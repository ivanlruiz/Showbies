using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Pruebas de las mejoras sin Test Runner y sin escena, desde el menu
// ShowBies/Pruebas o por RunCommand / -executeMethod:
//
//  - CorrerTodas: la logica pura (catalogo, precios, valores, escalado por
//    oleada, acumulador de disparo, daño al jugador, guardado, compras). Corre
//    en modo edicion y deja el veredicto en Builds/pruebas_mejoras.txt.
//  - MedirPartida: en modo play, dispara sin parar y mata a cada zombi al
//    aparecer para comparar lo que pasa en la partida con lo que dicen las
//    formulas. Deja el veredicto en Builds/medicion_mejoras.txt.
//
// Los dos escriben un archivo ademas de loguear, como ConstructorAndroid, para
// poder saber como termino una corrida lanzada sin supervision. Sin dialogos:
// se llaman desde afuera y un dialogo modal los colgaria.
public static class PruebasMejoras
{
    // El directorio de trabajo del editor es ShowBies1: ../Builds cae en la raiz
    // del repo, que esta gitignoreada.
    const string CarpetaSalida = "../Builds";
    const string RutaPruebas = CarpetaSalida + "/pruebas_mejoras.txt";
    const string RutaMedicion = CarpetaSalida + "/medicion_mejoras.txt";

    const double Tolerancia = 1e-4;

    static readonly CultureInfo Invariante = CultureInfo.InvariantCulture;

    [MenuItem("ShowBies/Pruebas/Logica de mejoras")]
    static void CorrerTodasDesdeMenu()
    {
        CorrerTodas();
    }

    [MenuItem("ShowBies/Pruebas/Medir partida (10 s)")]
    static void MedirDiezSegundos()
    {
        MedirPartida(10f);
    }

    // ------------------------------------------------------------------------
    // Informe: una linea "OK caso" o "FALLA: caso esperado X obtenido Y" por
    // chequeo y al final "RESULTADO: ...". Las fallas tambien van a la consola.
    // ------------------------------------------------------------------------
    private class Informe
    {
        private readonly StringBuilder texto = new StringBuilder();
        private readonly string prefijoLog;

        public int Fallas { get; private set; }

        public Informe(string titulo, string prefijoLog)
        {
            this.prefijoLog = prefijoLog;
            texto.AppendLine(titulo);
        }

        public void Linea(string linea)
        {
            texto.AppendLine(linea);
        }

        public void Ok(string caso)
        {
            texto.AppendLine("OK " + caso);
        }

        public void Falla(string motivo)
        {
            Fallas++;
            texto.AppendLine("FALLA: " + motivo);
            Debug.LogError(prefijoLog + "FALLA: " + motivo);
        }

        public void Falla(string caso, string esperado, string obtenido)
        {
            Falla(caso + " esperado " + esperado + " obtenido " + obtenido);
        }

        public bool Verdadero(string caso, bool condicion)
        {
            if (condicion) Ok(caso);
            else Falla(caso, "verdadero", "falso");
            return condicion;
        }

        public bool Igual(string caso, long esperado, long obtenido)
        {
            bool ok = esperado == obtenido;
            if (ok) Ok(caso);
            else Falla(caso, esperado.ToString(Invariante), obtenido.ToString(Invariante));
            return ok;
        }

        public bool Igual(string caso, string esperado, string obtenido)
        {
            bool ok = esperado == obtenido;
            if (ok) Ok(caso);
            else Falla(caso, Mostrar(esperado), Mostrar(obtenido));
            return ok;
        }

        // Con NaN la resta da NaN y la comparacion falso: cuenta como falla.
        public bool Cerca(string caso, double esperado, double obtenido, double tolerancia)
        {
            bool ok = Math.Abs(obtenido - esperado) <= tolerancia;
            if (ok) Ok(caso);
            else Falla(caso, Numero(esperado), Numero(obtenido));
            return ok;
        }

        public string Resultado(string sinFallas)
        {
            return Fallas == 0 ? sinFallas : Fallas + " FALLAS";
        }

        public bool Escribir(string ruta, string resultado)
        {
            texto.AppendLine("RESULTADO: " + resultado);
            try
            {
                Directory.CreateDirectory(CarpetaSalida);
                File.WriteAllText(ruta, texto.ToString());
            }
            catch (Exception e)
            {
                Debug.LogError(prefijoLog + "no se pudo escribir " + ruta + ": " + e.Message);
            }

            string resumen = prefijoLog + "RESULTADO: " + resultado + " (" + Path.GetFullPath(ruta) + ")";
            if (Fallas == 0) Debug.Log(resumen);
            else Debug.LogError(resumen);
            return Fallas == 0;
        }

        private static string Mostrar(string s)
        {
            return s == null ? "(null)" : "\"" + s + "\"";
        }
    }

    static string Numero(double valor)
    {
        return valor.ToString("R", Invariante);
    }

    static string Numero(double valor, string formato)
    {
        return valor.ToString(formato, Invariante);
    }

    // ========================================================================
    // Logica
    // ========================================================================

    public static bool CorrerTodas()
    {
        var informe = new Informe("PRUEBAS DE LOGICA DE MEJORAS", "PruebasMejoras: ");

        // En play el progreso cargado es el de la partida en curso: cambiarle la
        // carpeta a mitad de juego mezclaria lo de la prueba con lo jugado.
        if (EditorApplication.isPlaying)
        {
            informe.Falla("no se corre en modo play");
            informe.Escribir(RutaPruebas, informe.Resultado("TODO OK"));
            return false;
        }

        var temporales = new List<Mejora>();
        // Los textos y numeros esperados de abajo estan en espaniol; el idioma real
        // del editor se devuelve en el finally.
        Idioma.UsarParaPruebas(Lengua.Espanol);
        try
        {
            var catalogo = CatalogoMejoras.Instancia;
            bool completo = ProbarCatalogo(informe, catalogo);
            if (!completo) informe.Linea("(catalogo incompleto: se saltean las pruebas que dependen de los assets)");

            ProbarFormulaDePrecio(informe, temporales);
            ProbarArrancaEnCero(informe, temporales);
            if (completo)
            {
                ProbarPreciosDelCatalogo(informe, catalogo);
                ProbarValores(informe, catalogo);
            }
            ProbarFormatoNumeros(informe);
            ProbarIdiomas(informe);
            ProbarTema(informe);
            ProbarEscalado(informe);
            ProbarLugaresDelTutorial(informe);
            ProbarAcumuladorDeDisparo(informe);
            ProbarDanoAlJugador(informe);
            ProbarCajasYMonedas(informe);
            ProbarFaroles(informe);
            ProbarLaHorda(informe);
            ProbarCalidadDeAndroid(informe);
            ProbarPuntoDeLaGranada(informe);
            ProbarGrisDePocaVida(informe);
            ProbarSonidoDelJugo(informe);
            ProbarBalasAlRevivir(informe);
            ProbarZombisPorPartida(informe);
            ProbarCrecimientoDeMonedas(informe);
            ProbarAvisosSinPisarse(informe);
            ProbarTiendaTapaLaEscena(informe);
            ProbarVidriosDelMenu(informe);
            ProbarOrdenDeEscenas(informe);
            ProbarJefeAlTerminar(informe);
            ProbarPatronesDelJefe(informe);
            ProbarCurvaDeNivel(informe);
            ProbarRitmoDelNivel(informe);
            ProbarFamiliasDeLogros(informe);
            ProbarPuntosYMonedasDeLosZombis(informe);

            // Todo lo que toca Progreso va contra una carpeta temporal, y el
            // finally devuelve el progreso a persistentDataPath pase lo que pase.
            try
            {
                if (UsaLaCarpetaDePruebas(informe))
                {
                    ProbarGuardado(informe);
                    ProbarAnuncios(informe);
                    ProbarCircuitoDeAnuncios(informe);
                    ProbarRecompensaDiaria(informe);
                    ProbarEstadisticas(informe);
                    ProbarPrimeraVez(informe);
                    ProbarPedidoDeResena(informe);
                    ProbarRelojConfiable(informe);
                    ProbarJugoSonoro(informe);
                    ProbarMisiones(informe);
                    ProbarDesafioSemanal(informe);
                    ProbarProximoObjetivo(informe);
                    ProbarBestiario(informe);
                    ProbarNivelDelJugador(informe);
                    ProbarLogros(informe);
                    if (completo)
                    {
                        ProbarLogroDelCritico(informe);
                        ProbarGetters(informe, catalogo);
                        ProbarCompras(informe, catalogo, temporales);
                        ProbarComprasPosibles(informe);
                        ProbarTarjetaNeon(informe, catalogo);
                    }
                }
            }
            finally
            {
                Progreso.UsarCarpetaDePruebas(null);
            }
        }
        catch (Exception e)
        {
            informe.Falla("excepcion " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
        }
        finally
        {
            foreach (var m in temporales)
            {
                if (m != null) Object.DestroyImmediate(m);
            }
            Idioma.UsarParaPruebas(null);
            Tema.UsarParaPruebas(null);
        }

        return informe.Escribir(RutaPruebas, informe.Resultado("TODO OK"));
    }

    // 1. El catalogo existe, valida y tiene las ocho mejoras en orden.
    static bool ProbarCatalogo(Informe inf, CatalogoMejoras catalogo)
    {
        if (!inf.Verdadero("catalogo: existe en Resources/" + CatalogoMejoras.RutaEnResources, catalogo != null)) return false;

        var problemas = catalogo.Validar();
        inf.Igual("catalogo: Validar() sin problemas", "",
                  problemas == null ? "(null)" : string.Join(" | ", problemas));

        bool completo = true;
        completo &= ProbarId(inf, "danoBala", catalogo.danoBala, "dano_bala");
        completo &= ProbarId(inf, "cadencia", catalogo.cadencia, "cadencia");
        completo &= ProbarId(inf, "vidaMaxima", catalogo.vidaMaxima, "vida_maxima");
        completo &= ProbarId(inf, "iman", catalogo.iman, "iman");
        completo &= ProbarId(inf, "botin", catalogo.botin, "botin");
        completo &= ProbarId(inf, "furia", catalogo.furia, "furia");
        completo &= ProbarId(inf, "granada", catalogo.granada, "granada");
        completo &= ProbarId(inf, "criticos", catalogo.criticos, "criticos");

        string[] orden = { "dano_bala", "cadencia", "criticos", "vida_maxima", "granada", "iman", "botin", "furia" };
        int largo = catalogo.enTienda == null ? -1 : catalogo.enTienda.Length;
        inf.Igual("catalogo: enTienda tiene 8 mejoras", 8, largo);
        for (int i = 0; i < orden.Length; i++)
        {
            string obtenido = i < largo ? Id(catalogo.enTienda[i]) : "(falta)";
            inf.Igual("catalogo: enTienda[" + i + "]", orden[i], obtenido);
        }
        return completo;
    }

    static bool ProbarId(Informe inf, string campo, Mejora mejora, string id)
    {
        return inf.Igual("catalogo: id de " + campo, id, Id(mejora));
    }

    static string Id(Mejora mejora)
    {
        return mejora == null ? "(null)" : mejora.id;
    }

    // 2a. La formula de precio con mejoras temporales, para no depender de los
    // precios que tengan los assets.
    static void ProbarFormulaDePrecio(Informe inf, List<Mejora> temporales)
    {
        var m45 = CrearTemporal(temporales, "prueba_45", 45, 1.45, 0, CrecimientoEfecto.Aditivo, 0.15, 1, FormatoValor.UnDecimal);
        ChequearPrecios(inf, "formula de precio 45 x1,45", m45,
                        new double[] { 45, 65, 95, 137, 199, 288, 418, 606, 879, 1275, 1849 });
        inf.Cerca("formula de precio 45 x1,45: nivel negativo cuesta lo inicial", 45, m45.Precio(-1), 0);

        // 130,5 redondea para arriba: sin el +1e-9 un error de coma flotante lo
        // dejaba en 130.
        var m90 = CrearTemporal(temporales, "prueba_90", 90, 1.45, 0, CrecimientoEfecto.Aditivo, 0.15, 1, FormatoValor.UnDecimal);
        inf.Cerca("formula de precio 90 x1,45: Precio(1)", 131, m90.Precio(1), 0);

        var enorme = CrearTemporal(temporales, "prueba_enorme", 1e15, 10, 0, CrecimientoEfecto.Aditivo, 0.15, 1, FormatoValor.UnDecimal);
        inf.Cerca("formula de precio: topeado en PrecioMaximo", Mejora.PrecioMaximo, enorme.Precio(5), 0);
    }

    // 2a'. Una mejora que arranca en cero: sin comprar no rinde nada, el nivel 1
    // vale el valor base y el tope se aplica antes de correr el nivel.
    static void ProbarArrancaEnCero(Informe inf, List<Mejora> temporales)
    {
        var m = CrearTemporal(temporales, "prueba_cero", 30, 1.5, 4, CrecimientoEfecto.Aditivo, 0.25, 2, FormatoValor.UnDecimal);
        m.arrancaEnCero = true;
        inf.Cerca("arranca en cero: nivel -1 vale 0", 0, m.Valor(-1), Tolerancia);
        inf.Cerca("arranca en cero: nivel 0 vale 0", 0, m.Valor(0), Tolerancia);
        inf.Cerca("arranca en cero: nivel 1 vale la base", 2, m.Valor(1), Tolerancia);
        inf.Cerca("arranca en cero: nivel 2", 2.5, m.Valor(2), Tolerancia);
        inf.Cerca("arranca en cero: nivel 4 (tope)", 3.5, m.Valor(4), Tolerancia);
        inf.Cerca("arranca en cero: nivel 9 no pasa del tope", 3.5, m.Valor(9), Tolerancia);
        inf.Cerca("arranca en cero: el multiplicador no cambia", 1.25, m.Multiplicador(1), Tolerancia);
        inf.Igual("arranca en cero: texto nivel 0", "0", m.TextoValor(0));
    }

    // 2b. Los precios de los assets y sus topes. Arrancan baratos porque el
    // jugador arranca flojo: la primera partida tiene que alcanzar para comprar.
    static void ProbarPreciosDelCatalogo(Informe inf, CatalogoMejoras c)
    {
        ChequearPrecios(inf, "precio dano_bala", c.danoBala,
                        new double[] { 40, 58, 84, 122, 177, 256, 372, 539, 782, 1133, 1643 });
        ChequearPrecios(inf, "precio cadencia", c.cadencia,
                        new double[] { 50, 73, 105, 152, 221, 320, 465, 674, 977, 1417, 2054, 2979, 4319, 6263, 9081, 13167 });
        ChequearPrecios(inf, "precio vida_maxima", c.vidaMaxima,
                        new double[] { 40, 58, 84, 122, 177, 256, 372, 539, 782, 1133, 1643 });
        ChequearPrecios(inf, "precio iman", c.iman,
                        new double[] { 30, 45, 68, 101, 152, 228, 342, 513, 769, 1153, 1730, 2595, 3892 });
        ChequearPrecios(inf, "precio botin", c.botin,
                        new double[] { 120, 186, 288, 447, 693, 1074, 1664, 2579, 3998, 6197, 9605, 14888, 23076, 35768, 55440 });

        inf.Verdadero("tope cadencia: EnTope(15) es falso", !c.cadencia.EnTope(15));
        inf.Verdadero("tope cadencia: EnTope(16) es verdadero", c.cadencia.EnTope(16));
        inf.Verdadero("tope iman: EnTope(12) es falso", !c.iman.EnTope(12));
        inf.Verdadero("tope iman: EnTope(13) es verdadero", c.iman.EnTope(13));
        inf.Verdadero("tope botin: EnTope(14) es falso", !c.botin.EnTope(14));
        inf.Verdadero("tope botin: EnTope(15) es verdadero", c.botin.EnTope(15));
        inf.Verdadero("tope dano_bala: sin tope", !c.danoBala.TieneTope && !c.danoBala.EnTope(1000));
        inf.Verdadero("tope vida_maxima: sin tope", !c.vidaMaxima.TieneTope && !c.vidaMaxima.EnTope(1000));

        // La furia se compra una sola vez.
        ChequearPrecios(inf, "precio furia", c.furia, new double[] { 5000 });
        inf.Verdadero("tope furia: EnTope(0) es falso", !c.furia.EnTope(0));
        inf.Verdadero("tope furia: EnTope(1) es verdadero", c.furia.EnTope(1));

        // La granada tambien se compra una sola vez.
        ChequearPrecios(inf, "precio granada", c.granada, new double[] { 250 });
        inf.Verdadero("tope granada: EnTope(0) es falso", !c.granada.EnTope(0));
        inf.Verdadero("tope granada: EnTope(1) es verdadero", c.granada.EnTope(1));

        // Los criticos: ocho compras con los porcentajes de la tabla.
        ChequearPrecios(inf, "precio criticos", c.criticos, new double[] { 150, 270, 486, 875, 1575, 2834, 5102, 9183 });
        inf.Verdadero("tope criticos: EnTope(7) es falso", !c.criticos.EnTope(7));
        inf.Verdadero("tope criticos: EnTope(8) es verdadero", c.criticos.EnTope(8));
        double[] porcentajes = { 0, 5, 10, 20, 30, 50, 75, 90, 100 };
        for (int i = 0; i < porcentajes.Length; i++) ChequearValor(inf, "valor criticos", c.criticos, i, porcentajes[i]);
        ChequearValor(inf, "valor criticos pasado el tope", c.criticos, 12, 100);
        inf.Igual("texto criticos nivel 0", "0%", c.criticos.TextoValor(0));
        inf.Igual("texto criticos nivel 1", "5%", c.criticos.TextoValor(1));
        inf.Igual("texto criticos nivel 8", "100%", c.criticos.TextoValor(8));
        inf.Verdadero("critico: con 0 % nunca", !GunController.EsCritico(0f, 0f));
        inf.Verdadero("critico: con 5 % y sorteo 0,04 si", GunController.EsCritico(0.05f, 0.04f));
        inf.Verdadero("critico: con 5 % y sorteo 0,05 no", !GunController.EsCritico(0.05f, 0.05f));
        inf.Verdadero("critico: con 100 % aunque el sorteo de 1", GunController.EsCritico(1f, 1f));
    }

    static void ChequearPrecios(Informe inf, string nombre, Mejora mejora, double[] esperados)
    {
        for (int n = 0; n < esperados.Length; n++)
        {
            inf.Cerca(nombre + " nivel " + n, esperados[n], mejora.Precio(n), 0);
        }
    }

    // 3. Valores y textos de las mejoras de los assets.
    static void ProbarValores(Informe inf, CatalogoMejoras c)
    {
        ChequearValor(inf, "valor dano_bala", c.danoBala, 0, 1);
        ChequearValor(inf, "valor dano_bala", c.danoBala, 1, 2);
        ChequearValor(inf, "valor dano_bala", c.danoBala, 2, 3);
        ChequearValor(inf, "valor dano_bala", c.danoBala, 5, 6);
        ChequearValor(inf, "valor dano_bala", c.danoBala, 10, 11);
        ChequearValor(inf, "valor cadencia", c.cadencia, 0, 4);
        ChequearValor(inf, "valor cadencia", c.cadencia, 1, 5);
        ChequearValor(inf, "valor cadencia", c.cadencia, 5, 9);
        ChequearValor(inf, "valor cadencia", c.cadencia, 16, 20);
        ChequearValor(inf, "valor cadencia", c.cadencia, 17, 20);
        ChequearValor(inf, "valor vida_maxima", c.vidaMaxima, 0, 80);
        ChequearValor(inf, "valor vida_maxima", c.vidaMaxima, 1, 100);
        ChequearValor(inf, "valor vida_maxima", c.vidaMaxima, 5, 180);
        ChequearValor(inf, "valor vida_maxima", c.vidaMaxima, 10, 280);
        inf.Verdadero("iman arranca en cero", c.iman.arrancaEnCero);
        ChequearValor(inf, "valor iman", c.iman, 0, 0);
        ChequearValor(inf, "valor iman", c.iman, 1, 2);
        ChequearValor(inf, "valor iman", c.iman, 2, 2.5);
        ChequearValor(inf, "valor iman", c.iman, 13, 8);
        ChequearValor(inf, "valor iman", c.iman, 14, 8);
        ChequearValor(inf, "valor botin", c.botin, 1, 1.1);
        ChequearValor(inf, "valor botin", c.botin, 15, 2.5);
        ChequearValor(inf, "valor botin", c.botin, 16, 2.5);

        inf.Igual("texto dano_bala nivel 0", "1", c.danoBala.TextoValor(0));
        inf.Igual("texto dano_bala nivel 1", "2", c.danoBala.TextoValor(1));
        inf.Igual("texto dano_bala nivel 10", "11", c.danoBala.TextoValor(10));
        inf.Igual("texto cadencia nivel 0", "4", c.cadencia.TextoValor(0));
        inf.Igual("texto cadencia nivel 16", "20", c.cadencia.TextoValor(16));
        inf.Igual("texto vida_maxima nivel 0", "80", c.vidaMaxima.TextoValor(0));
        inf.Igual("texto iman nivel 0", "0", c.iman.TextoValor(0));
        inf.Igual("texto iman nivel 1", "2", c.iman.TextoValor(1));
        inf.Igual("texto iman nivel 2", "2,5", c.iman.TextoValor(2));
        inf.Igual("texto botin nivel 0", "×1", c.botin.TextoValor(0));
        inf.Igual("texto botin nivel 15", "×2,5", c.botin.TextoValor(15));

        // La furia: sin comprar no dura nada, comprada dura 6 s.
        inf.Verdadero("furia arranca en cero", c.furia.arrancaEnCero);
        ChequearValor(inf, "valor furia", c.furia, 0, 0);
        ChequearValor(inf, "valor furia", c.furia, 1, 6);
        ChequearValor(inf, "valor furia", c.furia, 2, 6);
        inf.Igual("texto furia nivel 0", "0", c.furia.TextoValor(0));
        inf.Igual("texto furia nivel 1", "6", c.furia.TextoValor(1));

        // El reloj de la furia y de su enfriamiento.
        inf.Cerca("furia: sin activar no falta nada", 0, Furia.Restante(10f, float.NegativeInfinity, 120f), 0);
        inf.Cerca("furia: recien activada falta todo el enfriamiento", 120, Furia.Restante(10f, 10f, 120f), Tolerancia);
        inf.Cerca("furia: a los 30 s faltan 90", 90, Furia.Restante(40f, 10f, 120f), Tolerancia);
        inf.Cerca("furia: a los 120 s esta lista", 0, Furia.Restante(130f, 10f, 120f), 0);
        inf.Cerca("furia: a los 6 s termino", 0, Furia.Restante(16f, 10f, 6f), 0);

        // La furia en el arma: multiplica encima de la mejora y de la caja, con el techo.
        var objeto = UnityEditor.EditorUtility.CreateGameObjectWithHideFlags("prueba_arma", HideFlags.HideAndDontSave);
        try
        {
            var arma = objeto.AddComponent<GunController>();
            arma.FijarTirosPorSegundo(10f);
            arma.FijarDanoPorBala(3f);
            arma.FijarFuria(2f, 2f);
            inf.Cerca("furia en el arma: tiros por segundo x2", 20, arma.TirosPorSegundo, Tolerancia);
            inf.Cerca("furia en el arma: daño por tiro x2", 6, arma.DanoPorTiro, Tolerancia);
            inf.Cerca("furia en el arma: el daño de la mejora no cambia", 3, arma.DanoPorBala, Tolerancia);
            arma.PotenciarCadencia(3f);
            inf.Cerca("furia con caja: se multiplican", 60, arma.TirosPorSegundo, Tolerancia);
            arma.FijarTirosPorSegundo(20f);
            inf.Cerca("furia con caja y cadencia al tope: techo de 120", 120, arma.TirosPorSegundo, Tolerancia);
            arma.FijarFuria(1f, 1f);
            inf.Cerca("furia apagada: vuelve la caja sola", 60, arma.TirosPorSegundo, Tolerancia);
            inf.Cerca("furia apagada: daño por tiro vuelve a la mejora", 3, arma.DanoPorTiro, Tolerancia);
        }
        finally
        {
            Object.DestroyImmediate(objeto);
        }
    }

    static void ChequearValor(Informe inf, string nombre, Mejora mejora, int nivel, double esperado)
    {
        inf.Cerca(nombre + " nivel " + nivel, esperado, mejora.Valor(nivel), Tolerancia);
    }

    static void ProbarFormatoNumeros(Informe inf)
    {
        inf.Igual("ConDecimales(5,75; 1)", "5,8", FormatoNumeros.ConDecimales(5.75, 1));
        inf.Igual("ConDecimales(20; 1)", "20", FormatoNumeros.ConDecimales(20, 1));
        inf.Igual("ConDecimales(21,6; 1)", "21,6", FormatoNumeros.ConDecimales(21.6, 1));
        inf.Igual("ConDecimales(8,745; 1)", "8,7", FormatoNumeros.ConDecimales(8.745, 1));
        inf.Igual("ConDecimales(229,99; 0)", "230", FormatoNumeros.ConDecimales(229.99, 0));
        inf.Igual("ConDecimales(1234,56; 1)", "1.235", FormatoNumeros.ConDecimales(1234.56, 1));
        inf.Igual("Compacto(1234) en espaniol", "1.234", FormatoNumeros.Compacto(1234));
        inf.Igual("Compacto(123456) en espaniol", "123 K", FormatoNumeros.Compacto(123456));
        inf.Igual("Compacto(4,5 M) en espaniol", "4,5 M", FormatoNumeros.Compacto(4500000));
        inf.Igual("Compacto(2,3 MM) en espaniol", "2,3 MM", FormatoNumeros.Compacto(2300000000));
        // Desde el billon (un millon de millones) va el B, y el ultimo escalon agrupa la
        // parte entera: antes 1,2345e15 salia "1234500 MM".
        inf.Igual("Compacto(999.999.999.999) en espaniol sigue en MM", "999,9 MM", FormatoNumeros.Compacto(999999999999));
        inf.Igual("Compacto(1 billon) en espaniol", "1 B", FormatoNumeros.Compacto(1e12));
        inf.Igual("Compacto(1,2 billones) en espaniol", "1,2 B", FormatoNumeros.Compacto(1.2345e12));
        inf.Igual("Compacto(1.234,5 billones) en espaniol, agrupado", "1.234,5 B", FormatoNumeros.Compacto(1.2345e15));

        // En ingles cambian los dos separadores y los sufijos: "1.234" en ingles se
        // lee "uno coma dos".
        Idioma.UsarParaPruebas(Lengua.Ingles);
        try
        {
            inf.Igual("ConDecimales(5,75; 1) en ingles", "5.8", FormatoNumeros.ConDecimales(5.75, 1));
            inf.Igual("ConDecimales(1234,56; 1) en ingles", "1,235", FormatoNumeros.ConDecimales(1234.56, 1));
            inf.Igual("Compacto(1234) en ingles", "1,234", FormatoNumeros.Compacto(1234));
            inf.Igual("Compacto(99999) en ingles", "99,999", FormatoNumeros.Compacto(99999));
            inf.Igual("Compacto(123456) en ingles", "123K", FormatoNumeros.Compacto(123456));
            inf.Igual("Compacto(4,5 M) en ingles", "4.5M", FormatoNumeros.Compacto(4500000));
            inf.Igual("Compacto(2,3 B) en ingles", "2.3B", FormatoNumeros.Compacto(2300000000));
            inf.Igual("Compacto(999.999.999.999) en ingles sigue en B", "999.9B", FormatoNumeros.Compacto(999999999999));
            inf.Igual("Compacto(1 T) en ingles", "1T", FormatoNumeros.Compacto(1e12));
            inf.Igual("Compacto(1,2 T) en ingles", "1.2T", FormatoNumeros.Compacto(1.2345e12));
            inf.Igual("Compacto(1234,5 T) en ingles, agrupado", "1,234.5T", FormatoNumeros.Compacto(1.2345e15));
        }
        finally
        {
            Idioma.UsarParaPruebas(Lengua.Espanol);
        }
    }

    // Los idiomas: el valor por defecto, la tabla de textos entera y que todo lo que
    // la usa (el codigo y los TextoTraducido de escenas y prefabs) pida ids que existen.
    static void ProbarIdiomas(Informe inf)
    {
        inf.Verdadero("idioma: por defecto es ingles", Idioma.PorDefecto == Lengua.Ingles);
        inf.Verdadero("idioma: sin nada guardado arranca en ingles", Idioma.DesdeCodigo("") == Lengua.Ingles);
        inf.Verdadero("idioma: null arranca en ingles", Idioma.DesdeCodigo(null) == Lengua.Ingles);
        inf.Verdadero("idioma: un codigo desconocido arranca en ingles", Idioma.DesdeCodigo("fr") == Lengua.Ingles);
        inf.Verdadero("idioma: \"es\" es espaniol", Idioma.DesdeCodigo("es") == Lengua.Espanol);
        inf.Verdadero("idioma: el codigo va y vuelve", Idioma.DesdeCodigo(Idioma.Codigo(Lengua.Espanol)) == Lengua.Espanol);

        int antes = Idioma.Revision;
        Idioma.Cambiar(Lengua.Ingles);
        inf.Verdadero("idioma: cambiar sube la revision", Idioma.Revision > antes);
        antes = Idioma.Revision;
        Idioma.Cambiar(Lengua.Ingles);
        inf.Igual("idioma: cambiar al mismo no sube la revision", antes, Idioma.Revision);
        Idioma.UsarParaPruebas(Lengua.Espanol);

        // La lectura, con una tabla armada: columnas en otro orden, comentarios,
        // saltos de linea escapados y un id repetido que no pisa al primero.
        var leida = new Dictionary<string, string[]>();
        var problemas = new List<string>();
        Textos.Leer("# comentario\nid\tes\ten\n\nhola\tHola\tHello\nsalto\tuno\\ndos\tone\\ntwo\nhola\tOtra\tOther\n", leida, problemas.Add);
        inf.Igual("textos: un id repetido se avisa", 1, problemas.Count);
        inf.Igual("textos: la lectura saltea comentarios y lineas vacias", 2, leida.Count);
        inf.Igual("textos: la columna se busca por su codigo", "Hello",
                  leida.ContainsKey("hola") ? leida["hola"][(int)Lengua.Ingles] : null);
        inf.Igual("textos: un id repetido no pisa al primero", "Hola",
                  leida.ContainsKey("hola") ? leida["hola"][(int)Lengua.Espanol] : null);
        inf.Igual("textos: \\n es un salto de linea", "uno\ndos",
                  leida.ContainsKey("salto") ? leida["salto"][(int)Lengua.Espanol] : null);

        // La tabla de verdad.
        Textos.Recargar();
        var ids = new List<string>(Textos.Ids);
        if (!inf.Verdadero("textos: la tabla existe y tiene textos (" + ids.Count + ")", ids.Count >= 60)) return;

        var marcador = new System.Text.RegularExpressions.Regex(@"\{(\d+)[^}]*\}");
        int incompletos = 0, desparejos = 0;
        foreach (string id in ids)
        {
            string en = Textos.Crudo(id, Lengua.Ingles);
            string es = Textos.Crudo(id, Lengua.Espanol);
            if (string.IsNullOrEmpty(en) || string.IsNullOrEmpty(es))
            {
                inf.Falla("textos: \"" + id + "\" no tiene los dos idiomas");
                incompletos++;
                continue;
            }

            // Un {1} que falta en un idioma tira una excepcion en plena partida.
            var enNumeros = new SortedSet<string>();
            foreach (System.Text.RegularExpressions.Match m in marcador.Matches(en)) enNumeros.Add(m.Groups[1].Value);
            var esNumeros = new SortedSet<string>();
            foreach (System.Text.RegularExpressions.Match m in marcador.Matches(es)) esNumeros.Add(m.Groups[1].Value);
            if (!enNumeros.SetEquals(esNumeros))
            {
                inf.Falla("textos: \"" + id + "\" no tiene los mismos {n} en los dos idiomas");
                desparejos++;
            }
        }
        inf.Igual("textos: todos tienen los dos idiomas", 0, incompletos);
        inf.Igual("textos: todos tienen los mismos {n} en los dos idiomas", 0, desparejos);

        var existentes = new HashSet<string>(ids);

        // Los ids que pide el codigo: Textos.De("..."), Textos.Formato("...", ...), los que
        // le pone a un TextoTraducido copiado (t.id = "...";, como OpcionesSonido y
        // ConfirmarSalir), los que se pasan a un control armado en codigo
        // (SliderVolumen.Crear(padre, "...", ...), Interruptor.Crear) y los que se arman con
        // el id de una mejora.
        // Solo los ids escritos enteros: "mejora_" + id se prueba aparte, con el catalogo.
        var pedido = new System.Text.RegularExpressions.Regex(@"Textos\.(?:De|Formato)\(\s*""([a-z0-9_]+)""\s*[,)]|\.id\s*=\s*""([a-z0-9_]+)""\s*;|\.Crear\(\s*\w+\s*,\s*""([a-z0-9_]+)""");
        int enCodigo = 0, faltanEnCodigo = 0;
        foreach (string archivo in Directory.GetFiles(Path.Combine(Application.dataPath, "Scripts"), "*.cs", SearchOption.AllDirectories))
        {
            foreach (System.Text.RegularExpressions.Match m in pedido.Matches(File.ReadAllText(archivo)))
            {
                string id = m.Groups[1].Success ? m.Groups[1].Value
                          : m.Groups[2].Success ? m.Groups[2].Value : m.Groups[3].Value;
                enCodigo++;
                if (existentes.Contains(id)) continue;
                inf.Falla("textos: " + Path.GetFileName(archivo) + " pide \"" + id + "\", que no esta en la tabla");
                faltanEnCodigo++;
            }
        }
        inf.Verdadero("textos: el codigo pide textos (" + enCodigo + ")", enCodigo > 0);
        inf.Igual("textos: todo lo que pide el codigo existe", 0, faltanEnCodigo);

        var catalogo = CatalogoMejoras.Instancia;
        if (catalogo != null)
        {
            int faltanMejoras = 0;
            foreach (var mejora in catalogo.enTienda)
            {
                if (mejora == null) continue;
                foreach (string parte in new[] { "_nombre", "_unidad" })
                {
                    string id = "mejora_" + mejora.id + parte;
                    if (existentes.Contains(id)) continue;
                    inf.Falla("textos: la mejora " + mejora.id + " no tiene \"" + id + "\"");
                    faltanMejoras++;
                }
            }
            inf.Igual("textos: cada mejora de la tienda tiene nombre y unidad", 0, faltanMejoras);
        }

        // Los TextoTraducido de los prefabs.
        int enPrefabs = 0, faltanEnPrefabs = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            if (prefab == null) continue;
            foreach (var traducido in prefab.GetComponentsInChildren<TextoTraducido>(true))
            {
                enPrefabs++;
                if (existentes.Contains(traducido.id)) continue;
                inf.Falla("textos: el prefab " + prefab.name + "/" + traducido.name + " pide \"" + traducido.id + "\", que no esta en la tabla");
                faltanEnPrefabs++;
            }
        }
        inf.Igual("textos: todo lo que piden los prefabs existe (" + enPrefabs + ")", 0, faltanEnPrefabs);

        // Los de las escenas se leen del archivo, sin abrirlas: cada TextoTraducido
        // queda como un bloque con el guid del script y su "id:".
        string guidScript = AssetDatabase.AssetPathToGUID("Assets/Scripts/Idioma/TextoTraducido.cs");
        int enEscenas = 0, faltanEnEscenas = 0;
        if (!string.IsNullOrEmpty(guidScript))
        {
            foreach (var escena in EditorBuildSettings.scenes)
            {
                string ruta = Path.Combine(Path.GetDirectoryName(Application.dataPath), escena.path);
                if (!File.Exists(ruta)) continue;
                string[] lineas = File.ReadAllLines(ruta);
                for (int i = 0; i < lineas.Length; i++)
                {
                    if (!lineas[i].Contains("guid: " + guidScript)) continue;
                    for (int j = i + 1; j < Math.Min(lineas.Length, i + 6); j++)
                    {
                        string linea = lineas[j].Trim();
                        if (!linea.StartsWith("id:")) continue;
                        string id = linea.Substring(3).Trim();
                        enEscenas++;
                        if (!existentes.Contains(id))
                        {
                            inf.Falla("textos: " + Path.GetFileName(escena.path) + " pide \"" + id + "\", que no esta en la tabla");
                            faltanEnEscenas++;
                        }
                        break;
                    }
                }
            }
        }
        inf.Igual("textos: todo lo que piden las escenas existe (" + enEscenas + ")", 0, faltanEnEscenas);

        // Los ids guardados en otros campos idTexto de escenas y prefabs: el nombre de cada
        // capitulo (CapitulosDeEscenario.escenarios[].idTexto) y la plantilla de un contador
        // de monedas (ContadorMonedas.idTexto, que vacio quiere decir "sin plantilla"). El
        // codigo los pasa como variable a Textos.De, asi que la busqueda de arriba no los ve.
        var conIdTexto = new List<string>();
        foreach (var escena in EditorBuildSettings.scenes) conIdTexto.Add(escena.path);
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
            conIdTexto.Add(AssetDatabase.GUIDToAssetPath(guid));
        int enCampos = 0, faltanEnCampos = 0;
        foreach (string relativa in conIdTexto)
        {
            string ruta = Path.Combine(Path.GetDirectoryName(Application.dataPath), relativa);
            if (!File.Exists(ruta)) continue;
            foreach (string cruda in File.ReadAllLines(ruta))
            {
                string linea = cruda.Trim();
                if (linea.StartsWith("- ")) linea = linea.Substring(2).TrimStart();
                if (!linea.StartsWith("idTexto:")) continue;
                string id = linea.Substring("idTexto:".Length).Trim();
                if (id.Length == 0) continue;
                enCampos++;
                if (existentes.Contains(id)) continue;
                inf.Falla("textos: " + Path.GetFileName(relativa) + " guarda idTexto \"" + id + "\", que no esta en la tabla");
                faltanEnCampos++;
            }
        }
        inf.Verdadero("textos: hay campos idTexto en las escenas (" + enCampos + ")", enCampos > 0);
        inf.Igual("textos: todo idTexto guardado existe", 0, faltanEnCampos);
    }

    // 4. Escalado por oleada: la cuenta de Escalado.PorOleada, con los crecimientos que
    // tenian las oleadas al escribirla (vida 1,15, daño 1,07, monedas 1,05).
    // El tema de la interfaz: que el claro devuelva lo de siempre, que el oscuro cambie
    // por rol y que lo que se escribe encima se lea. El contraste se mide y no se mira:
    // un color oscuro de mas en la paleta se vería en el telefono y no en el editor.
    static void ProbarTema(Informe inf)
    {
        int antes = Tema.Revision;
        Tema.UsarParaPruebas(null);
        inf.Verdadero("tema: el de siempre es el neon (el oscuro)", Tema.Oscuro);
        Tema.UsarParaPruebas(false);
        inf.Verdadero("tema: el claro de antes queda para las pruebas", !Tema.Oscuro);

        var propio = new Color(0.12f, 0.34f, 0.56f, 0.78f);
        inf.Verdadero("tema claro: Elegir devuelve el color de la escena",
                      Tema.Elegir(propio, RolDeTema.Panel) == propio);

        Tema.UsarParaPruebas(true);
        inf.Verdadero("tema oscuro: Elegir devuelve el del rol",
                      Tema.Elegir(propio, RolDeTema.Panel) == Tema.PanelOscuro);

        // Cada rol tiene su color: dos roles iguales serian un rol de mas.
        var roles = (RolDeTema[])Enum.GetValues(typeof(RolDeTema));
        int repetidos = 0;
        for (int i = 0; i < roles.Length; i++)
        {
            for (int j = i + 1; j < roles.Length; j++)
            {
                if (Tema.ColorOscuro(roles[i]) == Tema.ColorOscuro(roles[j])) repetidos++;
            }
        }
        inf.Igual("tema oscuro: ningun rol repite color", 0, repetidos);

        // Lo que se lee sobre cada fondo. 4,5 es el minimo de la WCAG para texto normal
        // y 3 para texto grande, que es lo que son los secundarios con Bangers.
        Contraste(inf, "texto sobre el panel", Tema.TextoClaro, Tema.PanelOscuro, 4.5);
        Contraste(inf, "texto sobre la tarjeta", Tema.TextoClaro, Tema.TarjetaOscura, 4.5);
        Contraste(inf, "texto sobre el fondo de la derrota", Tema.TextoClaro, Tema.FondoOscuro, 4.5);
        Contraste(inf, "texto suave sobre el panel", Tema.TextoSuaveClaro, Tema.PanelOscuro, 3.0);
        Contraste(inf, "texto suave sobre la tarjeta", Tema.TextoSuaveClaro, Tema.TarjetaOscura, 3.0);
        Contraste(inf, "acento sobre el fondo de la derrota", Tema.AcentoClaro, Tema.FondoOscuro, 3.0);

        // Lo mismo en claro, con los colores que traen las ventanas: la crema y el texto
        // oscuro de VentanaMisiones y la tarjeta de la tienda.
        var crema = new Color(1f, 0.96f, 0.86f, 1f);
        var textoOscuro = new Color(0.16f, 0.14f, 0.2f, 1f);
        Contraste(inf, "texto oscuro sobre la crema", textoOscuro, crema, 4.5);

        Tema.UsarParaPruebas(null);
        inf.Verdadero("tema: la revision avanza con cada cambio", Tema.Revision > antes);
    }

    static void Contraste(Informe inf, string caso, Color frente, Color fondo, double minimo)
    {
        double a = Luminancia(frente), b = Luminancia(fondo);
        double claro = Math.Max(a, b), oscuro = Math.Min(a, b);
        double razon = (claro + 0.05) / (oscuro + 0.05);
        if (razon >= minimo) inf.Ok("tema contraste: " + caso + " (" + razon.ToString("0.0", Invariante) + ":1)");
        else inf.Falla("tema contraste: " + caso, minimo.ToString("0.0", Invariante) + ":1",
                       razon.ToString("0.0", Invariante) + ":1");
    }

    // La luminancia relativa de la WCAG, con el canal linealizado como sRGB.
    static double Luminancia(Color c)
    {
        return 0.2126 * Canal(c.r) + 0.7152 * Canal(c.g) + 0.0722 * Canal(c.b);
    }

    static double Canal(double v)
    {
        return v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
    }

    // Donde nacen las cajas y los zombis del tutorial (TutorialManager.PuntoDelMapa, estatica
    // para probarla sin escena). Hasta el 25/9 se sumaba el desplazamiento al jugador sin mirar
    // las paredes invisibles, que dejan adentro unos 49 m desde el centro: una caja del otro
    // lado, que en el tutorial no caduca, trababa el paso, y un zombi caia al vacio y el paso
    // de disparar se daba por hecho.
    static void ProbarLugaresDelTutorial(Informe inf)
    {
        float limite = TutorialManager.LimiteDelMapa;
        // Mas el grupo de la granada (1,5 m) y la mitad de una caja, antes de las paredes.
        inf.Verdadero("tutorial: el area deja margen antes de las paredes", limite > 20f && limite + 2f < 49f);

        var j = new Vector3(3f, 0.5f, -2f);
        inf.Verdadero("tutorial: con lugar, donde se pidio y en el piso",
                      Cerca(TutorialManager.PuntoDelMapa(j, new Vector3(4f, 0f, 2f), limite), new Vector3(7f, 0f, 0f)));
        j = new Vector3(0f, 0.5f, 47f);
        inf.Verdadero("tutorial: pegado a la pared norte, la caja de arma va al sur a la misma distancia",
                      Cerca(TutorialManager.PuntoDelMapa(j, new Vector3(0f, 0f, 4f), limite), new Vector3(0f, 0f, 43f)));
        j = new Vector3(48f, 0.5f, -47f);
        inf.Verdadero("tutorial: en un rincon, el zombi viene de adentro a la misma distancia",
                      Cerca(TutorialManager.PuntoDelMapa(j, new Vector3(9f, 0f, -9f), limite), new Vector3(39f, 0f, -38f)));
        j = new Vector3(49f, 0.5f, 0f);
        inf.Verdadero("tutorial: con el jugador afuera del area, recortado al borde y mas lejos",
                      Cerca(TutorialManager.PuntoDelMapa(j, new Vector3(2f, 0f, 0f), limite), new Vector3(limite, 0f, 0f)));

        // Las dos cajas del paso 4, a 4 m a los costados de un punto 2 m adelante que se acota
        // dejandoles lugar: contra la pared de un costado, acotada cada una por su lado, las
        // dos caian en el mismo lugar.
        j = new Vector3(47f, 0.5f, 10f);
        Vector3 medio = TutorialManager.PuntoDelMapa(j, new Vector3(0f, 0f, 2f), limite - 4f);
        Vector3 derecha = medio + new Vector3(4f, 0f, 0f), izquierda = medio - new Vector3(4f, 0f, 0f);
        inf.Verdadero("tutorial: contra la pared este, las dos cajas adentro y separadas",
                      Mathf.Abs(derecha.x) <= limite && Mathf.Abs(izquierda.x) <= limite && (derecha - izquierda).magnitude > 7.9f);

        // Con el jugador en cualquier lugar entre las paredes y lo que pide cada paso (las
        // cajas a 2 y 4 m, el zombi a 14 m en cualquier direccion): siempre adentro y nunca
        // mas cerca del jugador de lo pedido, en ninguno de los dos ejes.
        var pedidos = new List<Vector3> { new Vector3(0f, 0f, 2f), new Vector3(0f, 0f, 4f),
                                          new Vector3(4f, 0f, 2f), new Vector3(-4f, 0f, 2f) };
        for (int grados = 0; grados < 360; grados += 15)
            pedidos.Add(Quaternion.Euler(0f, grados, 0f) * Vector3.forward * 14f);
        int casos = 0, afuera = 0, masCerca = 0;
        for (float x = -49f; x <= 49f; x += 1f)
        {
            for (float z = -49f; z <= 49f; z += 1f)
            {
                j = new Vector3(x, 0.5f, z);
                foreach (Vector3 d in pedidos)
                {
                    Vector3 p = TutorialManager.PuntoDelMapa(j, d, limite);
                    casos++;
                    if (Mathf.Abs(p.x) > limite + 1e-4f || Mathf.Abs(p.z) > limite + 1e-4f || p.y != 0f) afuera++;
                    if (Mathf.Abs(p.x - j.x) < Mathf.Abs(d.x) - 1e-3f || Mathf.Abs(p.z - j.z) < Mathf.Abs(d.z) - 1e-3f) masCerca++;
                }
            }
        }
        inf.Igual("tutorial: nada nace fuera del area (" + casos + " casos)", 0, afuera);
        inf.Igual("tutorial: nada nace mas cerca del jugador de lo pedido", 0, masCerca);
    }

    static void ProbarEscalado(Informe inf)
    {
        inf.Cerca("escalado: PorOleada(1,15; 1)", 1, Escalado.PorOleada(1.15f, 1), 1e-6);
        inf.Cerca("escalado: PorOleada(1,15; 0)", 1, Escalado.PorOleada(1.15f, 0), 1e-6);
        inf.Cerca("escalado: PorOleada(0; 5)", 1, Escalado.PorOleada(0f, 5), 1e-6);
        inf.Cerca("escalado: PorOleada(1,15; 10)", 3.5179, Escalado.PorOleada(1.15f, 10), 1e-3);
        inf.Cerca("escalado: PorOleada(1,07; 10)", 1.8385, Escalado.PorOleada(1.07f, 10), 1e-3);
        inf.Cerca("escalado: PorOleada(1,05; 10)", 1.5513, Escalado.PorOleada(1.05f, 10), 1e-3);
    }

    // 5. El acumulador de disparo: la cadencia promedio no depende de los FPS
    // (antes salia una bala por frame como mucho) y un frame largo no suelta
    // mas de maxTirosPorFrame.
    static void ProbarAcumuladorDeDisparo(Informe inf)
    {
        float[] cadencias = { 4f, 5f, 14f, 20f, 21.6f, 36f, 60f, 108f };
        int[] fps = { 60, 30, 24 };
        foreach (float cadencia in cadencias)
        {
            foreach (int f in fps)
            {
                double medido = SimularTiros(cadencia, 1f / f, 10f, 8);
                inf.Cerca("disparo: " + Numero(cadencia, "0.#") + " tiros/s a " + f + " FPS durante 10 s", cadencia, medido, 0.5);
            }
        }

        float contador = 0f;
        float intervalo = 1f / 20f;
        float atraso;
        for (int i = 0; i < 60; i++)
        {
            GunController.TirosDelFrame(ref contador, 1f / 60f, intervalo, 8, out atraso);
        }
        GunController.TirosDelFrame(ref contador, 0.5f, intervalo, 8, out atraso);
        int despuesDelFrameLargo = GunController.TirosDelFrame(ref contador, 1f / 60f, intervalo, 8, out atraso);
        inf.Igual("disparo: despues de un frame de 0,5 s salen 8 (el tope)", 8, despuesDelFrameLargo);
        // Salen los ultimos 8 tiros de la deuda, no los mas viejos: el primero va
        // atrasado entre 7 y 8 intervalos (0,35 a 0,40 s).
        inf.Cerca("disparo: el primero de ese frame va atrasado lo de los ultimos 8 tiros", 0.375, atraso, 0.03);

        // Sin tope el atraso conserva la fraccion: es lo que espacia parejo las
        // balas entre frames.
        contador = -0.005f;
        int sinTope = GunController.TirosDelFrame(ref contador, 1f / 60f, intervalo, 8, out atraso);
        inf.Igual("disparo: con contador -0,005 y sin tope tira 1", 1, sinTope);
        inf.Cerca("disparo: con contador -0,005 y sin tope va atrasado 0,005 s", 0.005, atraso, 1e-4);

        contador = 0f;
        int primero = GunController.TirosDelFrame(ref contador, 1f / 60f, intervalo, 8, out atraso);
        inf.Igual("disparo: primer llamado con contador 0 tira 1", 1, primero);
        inf.Cerca("disparo: primer llamado sin atraso", 0, atraso, 1e-6);

        // Soltar el disparo no recarga el arma: tocarlo mas rapido que la cadencia no
        // tira mas balas. Antes, a 4 tiros/s tocando 30 veces por segundo salian 30.
        const int frames = 600;   // 10 s a 60 FPS
        int tocando30 = TirosTocando(4f, 1f / 60f, frames, 1, 1);
        inf.Verdadero("disparo: a 4 tiros/s tocando 30 veces por segundo no pasa de 4 por segundo (" + tocando30 + " en 10 s)",
                      tocando30 >= 35 && tocando30 <= 41);
        int tocando12 = TirosTocando(4f, 1f / 60f, frames, 2, 3);
        inf.Verdadero("disparo: a 4 tiros/s tocando 12 veces por segundo no pasa de 4 por segundo (" + tocando12 + " en 10 s)",
                      tocando12 >= 35 && tocando12 <= 41);

        // Con pausas mas largas que el intervalo, cada toque sigue tirando en el acto:
        // 0,1 s apretado y 0,5 s suelto, una bala por toque.
        int toques = (frames + 35) / 36;
        inf.Igual("disparo: con pausas de 0,5 s cada toque tira una bala en el acto", toques, TirosTocando(4f, 1f / 60f, frames, 6, 30));
        inf.Cerca("disparo: sin disparar el contador baja hasta 0 y no pasa", 0, GunController.EnfriarSinDisparar(0.01f, 0.5f), 1e-6);
    }

    // Como GunController.Update con el disparo apretado framesApretado frames y
    // suelto framesSuelto, en ciclo.
    static int TirosTocando(float tirosPorSegundo, float dt, int frames, int framesApretado, int framesSuelto)
    {
        float contador = 0f;
        float atraso;
        int total = 0;
        int ciclo = framesApretado + framesSuelto;
        for (int i = 0; i < frames; i++)
        {
            if (i % ciclo < framesApretado) total += GunController.TirosDelFrame(ref contador, dt, 1f / tirosPorSegundo, 8, out atraso);
            else contador = GunController.EnfriarSinDisparar(contador, dt);
        }
        return total;
    }

    static double SimularTiros(float tirosPorSegundo, float dt, float segundos, int maximo)
    {
        float contador = 0f;
        float intervalo = 1f / tirosPorSegundo;
        int frames = Mathf.RoundToInt(segundos / dt);
        long total = 0;
        float atraso;
        for (int i = 0; i < frames; i++)
        {
            total += GunController.TirosDelFrame(ref contador, dt, intervalo, maximo, out atraso);
        }
        return total / (frames * (double)dt);
    }

    // Las cajas no nacen y las monedas no caen donde las tapa un edificio de la ciudad, y el
    // techo de monedas en escena es duro. Los edificios no tienen collider (los zombis se
    // trabarian): hasta la auditoria del 24/9 una de cada seis cajas nacia adentro de uno y
    // vencia sin que nadie la viera, y las monedas de un zombi que moria cruzandolo caian
    // adentro. Y con el techo lleno cada muerte sumaba una moneda de mas, mientras la lluvia
    // del jefe salia como una o dos iguales a las demas.
    static void ProbarCajasYMonedas(Informe inf)
    {
        // El techo: cuantas salen de una muerte y cuantas del piso se van para hacerles lugar.
        int lugar;
        inf.Igual("techo de monedas: con lugar salen todas", 3, Moneda.CuantasSalen(3, 10, 80, 10, out lugar));
        inf.Igual("techo de monedas: con lugar no se va ninguna del piso", 0, lugar);
        inf.Igual("techo de monedas: casi lleno salen las que entran", 2, Moneda.CuantasSalen(3, 78, 80, 10, out lugar));
        inf.Igual("techo de monedas: casi lleno no se va ninguna", 0, lugar);
        inf.Igual("techo de monedas: lleno sale una igual", 1, Moneda.CuantasSalen(3, 80, 80, 10, out lugar));
        inf.Igual("techo de monedas: lleno se va la mas vieja", 1, lugar);
        inf.Igual("techo de monedas: pasado del techo sale una", 1, Moneda.CuantasSalen(3, 85, 80, 10, out lugar));
        inf.Igual("techo de monedas: pasado del techo se va una y no crece", 1, lugar);
        inf.Igual("techo de monedas: la lluvia del jefe sale entera con el techo lleno", 35, Moneda.CuantasSalen(35, 80, 80, 10, out lugar));
        inf.Igual("techo de monedas: y le hacen lugar las 35 mas viejas", 35, lugar);
        inf.Igual("techo de monedas: con 60 en el piso la lluvia sale entera", 35, Moneda.CuantasSalen(35, 60, 80, 10, out lugar));
        inf.Igual("techo de monedas: y le hacen lugar 15", 15, lugar);
        bool nuncaPasa = true, siempreUna = true;
        for (int cantidad = 1; cantidad <= 45; cantidad++)
        {
            for (int enElPiso = 0; enElPiso <= 80; enElPiso++)
            {
                int salen = Moneda.CuantasSalen(cantidad, enElPiso, 80, 10, out lugar);
                if (enElPiso - lugar + salen > 80 || lugar > enElPiso) nuncaPasa = false;
                if (salen < 1 || salen > cantidad) siempreUna = false;
            }
        }
        inf.Verdadero("techo de monedas: con hasta 80 en el piso nunca pasa de 80", nuncaPasa);
        inf.Verdadero("techo de monedas: siempre sale al menos una y nunca mas de las que suelta", siempreUna);

        // Moneda no sabe quien las suelta: la lluvia se reconoce por la cantidad. La del jefe
        // tiene que llegar siempre (en el libre, en promedio: ahi cada moneda sale con
        // probabilidad 0,5 o mas) y la de ningun otro zombi.
        var objetoMoneda = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Moneda.prefab");
        var moneda = objetoMoneda != null ? objetoMoneda.GetComponent<Moneda>() : null;
        if (inf.Verdadero("techo de monedas: esta el prefab de la moneda", moneda != null))
        {
            foreach (string tipo in Bestiario.Tipos)
            {
                var enemigo = AssetDatabase.LoadAssetAtPath<Enemy>("Assets/Zombies/" + tipo + ".asset");
                if (!inf.Verdadero("techo de monedas: esta el asset de " + tipo, enemigo != null)) continue;
                if (tipo == Bestiario.Jefe)
                    inf.Verdadero("techo de monedas: el jefe suelta una lluvia, tambien en el libre", enemigo.monedasMin * 0.5 >= moneda.lluviaDesde);
                else
                    inf.Verdadero("techo de monedas: " + tipo + " no llega a una lluvia", enemigo.monedasMax < moneda.lluviaDesde);
            }
        }

        // Lo que tapan los edificios, con la camara de WaveMode: 70 grados, mirando hacia +z.
        Vector3 mirada = Quaternion.Euler(70f, 0f, 0f) * Vector3.forward;
        var tapado = new List<Rect>();
        var cementerio = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Escenarios/Cementerio.prefab");
        if (inf.Verdadero("edificios: esta el prefab del cementerio", cementerio != null))
        {
            CapitulosDeEscenario.LoQueTapanLosEdificios(cementerio, mirada, tapado);
            inf.Igual("edificios: el cementerio no tapa nada", 0, tapado.Count);
        }
        var ciudad = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Escenarios/Ciudad.prefab");
        if (!inf.Verdadero("edificios: esta el prefab de la ciudad", ciudad != null)) return;
        CapitulosDeEscenario.LoQueTapanLosEdificios(ciudad, mirada, tapado);
        var edificios = new List<Transform>();
        foreach (Transform t in ciudad.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "Edificio") edificios.Add(t);
        }
        inf.Verdadero("edificios: la ciudad tiene", edificios.Count > 0);
        inf.Igual("edificios: una zona tapada por edificio", edificios.Count, tapado.Count);

        var estadoDelAzar = UnityEngine.Random.state;
        try
        {
            CapitulosDeEscenario.UsarParaPruebas(tapado);
            bool centrosTapados = true;
            foreach (var edificio in edificios) centrosTapados &= CapitulosDeEscenario.Tapado(edificio.position);
            inf.Verdadero("edificios: el centro de cada uno esta tapado", centrosTapados);

            // Cada edificio es un cuadrado centrado en su grupo. Del lado de la camara (-z) tapa
            // solo su huella; del otro, tambien la franja que esconde el techo (mas de un metro
            // con 3,5 m de alto).
            bool haciaAtras = true, haciaLaCamara = true;
            foreach (var edificio in edificios)
            {
                Vector3 centro = edificio.position;
                Rect zona = default;
                foreach (var z in tapado)
                {
                    if (z.Contains(new Vector2(centro.x, centro.z))) zona = z;
                }
                float mitad = zona.width / 2f;
                if (Mathf.Abs(centro.z - mitad - zona.yMin) > 0.1f) haciaLaCamara = false;
                if (zona.yMax - (centro.z + mitad) < 1f) haciaAtras = false;
            }
            inf.Verdadero("edificios: tapan una franja detras del techo, del lado contrario a la camara", haciaAtras);
            inf.Verdadero("edificios: del lado de la camara tapan solo la huella", haciaLaCamara);

            // Las calles no quedan tapadas: el cruce del centro, donde arranca el jugador, y las
            // lineas del medio de las calles.
            bool callesLibres = true;
            for (int t = -45; t <= 45; t++)
            {
                foreach (float calle in new[] { -24f, 0f, 24f })
                {
                    if (CapitulosDeEscenario.Tapado(new Vector3(calle, 0f, t)) || CapitulosDeEscenario.Tapado(new Vector3(t, 0f, calle))) callesLibres = false;
                }
            }
            inf.Verdadero("edificios: las calles del medio quedan libres", callesLibres);

            // Las cajas: ninguna nace tapada y todas en el area de siempre.
            UnityEngine.Random.InitState(1234);
            int tapadas = 0, afuera = 0;
            for (int i = 0; i < 5000; i++)
            {
                Vector3 caja = PowerUp.PuntoDeAparicion();
                if (CapitulosDeEscenario.Tapado(caja, PowerUp.MargenContraLosEdificios)) tapadas++;
                if (caja.x < -48f || caja.x > 48f || caja.z < -45f || caja.z > 44f || !Mathf.Approximately(caja.y, 0.5f)) afuera++;
            }
            inf.Igual("edificios: ninguna caja nace tapada (de 5000)", 0, tapadas);
            inf.Igual("edificios: las cajas nacen en el area de siempre", 0, afuera);

            // Las monedas: las de un zombi que muere adentro salen por el borde mas cercano, y
            // las que vuelan hacia un edificio se frenan antes, como contra una pared.
            bool salenAfuera = true, frenanAntes = true, siguenDeLargo = true;
            foreach (var zona in tapado)
            {
                var adentro = new Vector3(zona.center.x, 1f, zona.center.y);
                Vector3 afueraDelEdificio = Moneda.SacarDeLoTapado(adentro, 0.3f);
                if (CapitulosDeEscenario.Tapado(afueraDelEdificio) || !CapitulosDeEscenario.Tapado(afueraDelEdificio, 0.31f) ||
                    !Mathf.Approximately(afueraDelEdificio.y, 1f)) salenAfuera = false;
                Vector3 haciaElEdificio = new Vector3(zona.center.x - afueraDelEdificio.x, 0f, zona.center.y - afueraDelEdificio.z).normalized;
                if (Mathf.Abs(Moneda.LibreHastaLoTapado(afueraDelEdificio, haciaElEdificio, 3f) - 0.3f) > 1e-3f) frenanAntes = false;
                if (Mathf.Abs(Moneda.LibreHastaLoTapado(afueraDelEdificio, -haciaElEdificio, 3f) - 3f) > 1e-3f) siguenDeLargo = false;
                var alSur = new Vector3(zona.center.x, 1f, zona.yMin - 5f);
                if (Mathf.Abs(Moneda.LibreHastaLoTapado(alSur, Vector3.forward, 10f) - 5f) > 1e-3f) frenanAntes = false;
            }
            inf.Verdadero("edificios: las monedas de un zombi que muere adentro salen al borde, a 0,3 m", salenAfuera);
            inf.Verdadero("edificios: una moneda que vuela hacia un edificio se frena en el borde", frenanAntes);
            inf.Verdadero("edificios: una que se aleja no se frena", siguenDeLargo);
            var libre = new Vector3(0f, 1f, 0f);
            inf.Verdadero("edificios: en la calle la moneda sale donde murio el zombi", Moneda.SacarDeLoTapado(libre, 0.3f) == libre);

            // Sin decorado (la pradera, el modo libre) no hay nada tapado.
            CapitulosDeEscenario.UsarParaPruebas(null);
            inf.Verdadero("edificios: sin decorado no hay nada tapado",
                          edificios.Count == 0 || !CapitulosDeEscenario.Tapado(edificios[0].position));
        }
        finally
        {
            CapitulosDeEscenario.UsarParaPruebas(null);
            UnityEngine.Random.state = estadoDelAzar;
        }
    }

    // 6. El daño con decimales de los zombis escalados se acumula y sale entero.
    static void ProbarDanoAlJugador(Informe inf)
    {
        int total;
        inf.Igual("dano al jugador: 10 golpes de 1,3108", "1,1,1,2,1,1,2,1,1,2", SecuenciaDeDano(1.3108f, 10, out total));
        inf.Igual("dano al jugador: 10 golpes de 1,3108 suman 13", 13, total);
        inf.Igual("dano al jugador: 6 golpes de 2,2898", "2,2,2,3,2,2", SecuenciaDeDano(2.2898f, 6, out total));
        SecuenciaDeDano(18.3846f, 5, out total);
        inf.Igual("dano al jugador: 5 golpes de 18,3846 suman 91", 91, total);

        float pendiente = 0f;
        inf.Igual("dano al jugador: cantidad 0", 0, PlayerHealth.AcumularDano(ref pendiente, 0f));
        inf.Igual("dano al jugador: cantidad negativa", 0, PlayerHealth.AcumularDano(ref pendiente, -3f));
        inf.Cerca("dano al jugador: sin daño no queda pendiente", 0, pendiente, 1e-6);
    }

    static string SecuenciaDeDano(float golpe, int veces, out int total)
    {
        float pendiente = 0f;
        total = 0;
        var partes = new string[veces];
        for (int i = 0; i < veces; i++)
        {
            int entero = PlayerHealth.AcumularDano(ref pendiente, golpe);
            total += entero;
            partes[i] = entero.ToString(Invariante);
        }
        return string.Join(",", partes);
    }

    // ------------------------------------------------------------------------
    // Progreso en una carpeta temporal
    // ------------------------------------------------------------------------

    [Serializable]
    private class NivelGuardado
    {
        public string id;
        public int nivel;
    }

    [Serializable]
    private class JsonGuardado
    {
        public int version;
        public double monedas;
        public int mejorOleada;
        public List<NivelGuardado> mejoras;
    }

    static string CarpetaProgreso
    {
        get { return Path.Combine(Application.temporaryCachePath, "pruebas_progreso"); }
    }

    // Cada caso arranca de cero: primero se apunta Progreso a la carpeta (que
    // guarda lo del caso anterior ahi mismo), despues se borra y se escriben los
    // archivos del caso. Devuelve la ruta del progreso.json de la prueba.
    static string EmpezarCaso(string principal, string temporal)
    {
        string carpeta = CarpetaProgreso;
        Progreso.UsarCarpetaDePruebas(carpeta);
        if (Directory.Exists(carpeta)) Directory.Delete(carpeta, true);
        Directory.CreateDirectory(carpeta);

        string ruta = Path.Combine(carpeta, "progreso.json");
        if (principal != null) File.WriteAllText(ruta, principal);
        if (temporal != null) File.WriteAllText(ruta + ".tmp", temporal);
        return ruta;
    }

    // Leer Monedas fuerza la carga: la Revision inicial se lee recien despues.
    static void EmpezarConMonedas(double monedas)
    {
        EmpezarCaso("{\"version\":" + Progreso.VersionActual + ",\"monedas\":" + Numero(monedas) + "}", null);
        _ = Progreso.Monedas;
        // Como partida nueva: los contadores que no se guardan (monedas de la
        // partida, videos vistos) arrancan en cero en cada caso.
        Progreso.EmpezarPartida();
    }

    // Si Progreso no escribiera en la carpeta de pruebas, lo que sigue pisaria el
    // progreso real del editor: sin esto no se corre nada.
    static bool UsaLaCarpetaDePruebas(Informe inf)
    {
        string ruta = EmpezarCaso(null, null);
        return inf.Igual("guardado: Progreso escribe en la carpeta de pruebas",
                         Path.GetFullPath(ruta), Path.GetFullPath(Progreso.RutaArchivo));
    }

    static string LeerSiExiste(string ruta)
    {
        return File.Exists(ruta) ? File.ReadAllText(ruta) : null;
    }

    static JsonGuardado LeerGuardado(string ruta)
    {
        try
        {
            return File.Exists(ruta) ? JsonUtility.FromJson<JsonGuardado>(File.ReadAllText(ruta)) : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    static int NivelGuardadoDe(JsonGuardado json, string id)
    {
        if (json == null || json.mejoras == null) return -1;
        foreach (var m in json.mejoras)
        {
            if (m != null && m.id == id) return m.nivel;
        }
        return -1;
    }

    // La horda, de la auditoria del 24/9:
    // - No aparece a la vista (EnemyController.SeVeriaAlAparecer): a los 8 m de distancia
    //   minima al jugador todavia se esta en cuadro. Con la camara de WaveMode, en 16:9 y 20:9.
    // - En el festejo, el que no se acerca a su lugar (detras de una pared invisible, adentro
    //   de un tanque) deja de ir: caminando hacia el no se traba, parado contra algo si.
    // - El cadaver sale despedido segun cuanto mas grande que el normal es, con la escala de
    //   cada prefab: el tanque salia igual que un normal.
    // - La mancha de sangre queda por encima de la vereda y el cordon de la ciudad, que la
    //   tapaban.
    static void ProbarLaHorda(Informe inf)
    {
        inf.Verdadero("horda: sin camara no se descarta ningun punto", !EnemyController.SeVeriaAlAparecer(null, Vector3.zero));

        Vector3 posicionCamara = Vector3.zero, jugador = Vector3.zero;
        Quaternion rotacionCamara = Quaternion.identity;
        float campoVisual = 0f, cerca = 0.3f, lejos = 1000f;
        LeerEscena("Assets/Escenas/WaveMode.unity", e =>
        {
            var seguidor = Buscar<CamaraJugador>(e);
            var control = Buscar<PlayerController>(e);
            var lente = seguidor != null ? seguidor.GetComponent<Camera>() : null;
            if (lente == null || control == null) return;
            posicionCamara = lente.transform.position;
            rotacionCamara = lente.transform.rotation;
            campoVisual = lente.fieldOfView;
            cerca = lente.nearClipPlane;
            lejos = lente.farClipPlane;
            jugador = control.transform.position;
        });
        if (inf.Verdadero("horda: esta la camara del juego en WaveMode", campoVisual > 0f))
        {
            var objeto = EditorUtility.CreateGameObjectWithHideFlags("prueba_camara_horda", HideFlags.HideAndDontSave, typeof(Camera));
            try
            {
                var camara = objeto.GetComponent<Camera>();
                camara.enabled = false;
                camara.transform.SetPositionAndRotation(posicionCamara, rotacionCamara);
                camara.fieldOfView = campoVisual;
                camara.nearClipPlane = cerca;
                camara.farClipPlane = lejos;
                foreach (var pantalla in new[] { new Vector2(16f, 9f), new Vector2(20f, 9f) })
                {
                    camara.aspect = pantalla.x / pantalla.y;
                    string en = " (" + pantalla.x + ":" + pantalla.y + ")";
                    Func<float, float, bool> seVe = (x, z) =>
                        EnemyController.SeVeriaAlAparecer(camara, new Vector3(jugador.x + x, 0f, jugador.z + z));
                    inf.Verdadero("horda: a 8 m por delante, la distancia minima, todavia se ve y no aparece ahi" + en, seVe(0f, 8f));
                    inf.Verdadero("horda: a 9 m al costado se ve y no aparece ahi" + en, seVe(9f, 0f));
                    inf.Verdadero("horda: a 20 m al costado no se ve" + en, !seVe(20f, 0f));
                    inf.Verdadero("horda: a 12 m por detras no se ve" + en, !seVe(0f, -12f));
                    inf.Verdadero("horda: a 16 m por delante no se ve" + en, !seVe(0f, 16f));
                }
            }
            finally
            {
                Object.DestroyImmediate(objeto);
            }
        }

        // El festejo, a paso de fisica: caminando a 2 m/s (el jefe, el mas lento) no se traba;
        // parado a 3 m de su lugar, deja de ir a los EsperaSinAcercarse.
        float masCerca = float.PositiveInfinity, desde = 0f;
        bool trabado = false;
        for (int paso = 0; paso <= 100 && !trabado; paso++)
            trabado = EnemyController.SinAcercarse(10f - 2f * 0.02f * paso, 0.02f * paso, ref masCerca, ref desde);
        inf.Verdadero("horda: festejo, caminando hacia su lugar no se traba", !trabado);
        masCerca = float.PositiveInfinity;
        desde = 0f;
        float trabadoEn = -1f;
        for (int paso = 0; paso <= 100 && trabadoEn < 0f; paso++)
            if (EnemyController.SinAcercarse(3f, 0.02f * paso, ref masCerca, ref desde)) trabadoEn = 0.02f * paso;
        inf.Cerca("horda: festejo, parado contra una pared deja de ir a su lugar al rato",
                  EnemyController.EsperaSinAcercarse, trabadoEn, 0.021);

        // El empujon del cadaver, con la escala de la raiz de cada prefab.
        var escalas = new Dictionary<string, float>();
        foreach (string nombre in new[] { "Zombi", "ZombiRapido", "ZombiFASTER", "ZombiTanque", "ZombiBOSS" })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Personajes/" + nombre + ".prefab");
            if (prefab != null) escalas[nombre] = prefab.transform.localScale.y;
        }
        if (inf.Igual("horda: estan los cinco prefabs de zombi", 5, escalas.Count))
        {
            inf.Cerca("horda: la vara del empujon es la escala del zombi normal", escalas["Zombi"], EnemyController.EscalaDelNormal, 1e-6);
            inf.Cerca("horda: el cadaver del normal sale con todo el empujon", 1, EnemyController.VelocidadDelEmpuje(1f, escalas["Zombi"]), 1e-6);
            inf.Cerca("horda: el del rapido, mas chico, como el normal", 1, EnemyController.VelocidadDelEmpuje(1f, escalas["ZombiRapido"]), 1e-6);
            inf.Cerca("horda: el del FASTER, mas chico, como el normal", 1, EnemyController.VelocidadDelEmpuje(1f, escalas["ZombiFASTER"]), 1e-6);
            inf.Verdadero("horda: el del tanque sale a la mitad o menos (salia como el normal)",
                          EnemyController.VelocidadDelEmpuje(1f, escalas["ZombiTanque"]) <= 0.5f + 1e-6f);
            inf.Verdadero("horda: el del jefe sale a un cuarto o menos",
                          EnemyController.VelocidadDelEmpuje(1f, escalas["ZombiBOSS"]) <= 0.25f + 1e-6f);
        }

        // La mancha, por encima de lo mas alto que se pisa en la ciudad: la vereda de cada
        // manzana y su cordon (cubos derechos, asi que el techo es el centro mas medio alto).
        var ciudad = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Escenarios/Ciudad.prefab");
        if (inf.Verdadero("horda: esta el prefab de la ciudad", ciudad != null))
        {
            float techo = float.NegativeInfinity;
            int piezas = 0;
            foreach (Transform manzana in ciudad.GetComponentsInChildren<Transform>(true))
            {
                if (manzana.name != "Manzana") continue;
                foreach (Transform pieza in manzana)
                {
                    piezas++;
                    techo = Mathf.Max(techo, pieza.position.y + Mathf.Abs(pieza.lossyScale.y) * 0.5f);
                }
            }
            inf.Verdadero("horda: la ciudad tiene manzanas con vereda", piezas > 0);
            inf.Verdadero("horda: la mancha de sangre queda por encima de la vereda y el cordon de la ciudad (" + Numero(techo, "0.00") + " m)",
                          piezas > 0 && ManchaDeSangre.AlturaSobreElPiso > techo);
        }
    }

    // Los faroles de noche alumbran el piso tambien en el telefono. Hasta el 23/9 sus luces
    // iban en Auto: en Android (calidad Medium, una sola luz por pixel, que se lleva la luna)
    // caian a luz por vertice, y el piso tiene un vertice cada diez metros, asi que no se
    // alumbraba nada; en el editor, en Ultra, se veian bien. Ahora el piso lo pinta un charco
    // de luz bajo cada farol y la luz va solo por vertice, que es lo mismo en las dos
    // calidades. Lo que se ve lo mide ShowBies > Escenarios > Fotos de los faroles.
    static void ProbarFaroles(Informe inf)
    {
        foreach (string ruta in new[] { "Assets/Prefabs/Escenarios/Cementerio.prefab", "Assets/Prefabs/Escenarios/Ciudad.prefab" })
        {
            string nombre = Path.GetFileNameWithoutExtension(ruta);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (!inf.Verdadero("faroles: esta el prefab " + nombre, prefab != null)) continue;

            int faroles = 0, conCharco = 0, lucesPorPixel = 0, charcosCorridos = 0;
            foreach (Transform farol in prefab.GetComponentsInChildren<Transform>(true))
            {
                if (farol.name != "Farol") continue;
                faroles++;
                Renderer charco = null;
                foreach (var render in farol.GetComponentsInChildren<Renderer>(true))
                {
                    var material = render.sharedMaterial;
                    if (material != null && material.shader != null && material.shader.name == "ShowBies/CharcoDeLuz") charco = render;
                }
                if (charco != null) conCharco++;

                foreach (var luz in farol.GetComponentsInChildren<Light>(true))
                {
                    if (luz.renderMode != LightRenderMode.ForceVertex) lucesPorPixel++;
                    if (charco == null) continue;
                    // En el piso (el cordon de la ciudad mide 0,18) y justo debajo de la luz.
                    Vector3 a = luz.transform.position, b = charco.transform.position;
                    if (new Vector2(a.x - b.x, a.z - b.z).magnitude > 0.3f || b.y < 0f || b.y > 0.25f) charcosCorridos++;
                }
            }
            inf.Verdadero("faroles: el " + nombre + " tiene faroles", faroles > 0);
            inf.Igual("faroles: cada farol del " + nombre + " tiene su charco de luz en el piso", faroles, conCharco);
            inf.Igual("faroles: las luces del " + nombre + " van solo por vertice (asi el editor ve lo del telefono)", 0, lucesPorPixel);
            inf.Igual("faroles: el charco del " + nombre + " esta en el piso, debajo de su luz", 0, charcosCorridos);
        }
    }

    // Android sale en Medium (ver Rendimiento en movil). Entrar y salir de play le borra el
    // bloque a QualitySettings.asset sin avisar; la build tambien lo controla
    // (ConstructorAndroid), pero esto lo avisa antes, justo despues de un banco en play.
    static void ProbarCalidadDeAndroid(Informe inf)
    {
        inf.Verdadero("calidad: hay un nivel Medium", CalidadDeAndroid.Medium >= 0);
        inf.Igual("calidad: Android sale en Medium (si falla, revertir ProjectSettings/QualitySettings.asset)",
                  CalidadDeAndroid.Medium, CalidadDeAndroid.Guardada());

        // Lo demas que la build revisa antes de compilar (ConstructorAndroid): el paquete y el
        // nombre, que la APK cambia y devuelve en un finally (si el editor se cae en el medio
        // quedan cambiados en disco), y las escenas, cuyos indices estan escritos en el codigo.
        string play = ConstructorAndroid.PaqueteDePlay;
        inf.Verdadero("build: el AAB sale con el paquete de Play y el nombre de siempre",
                      ConstructorAndroid.ProblemaDelAab(play, "ShowBies") == null);
        inf.Verdadero("build: el AAB no sale con el paquete que deja una APK a medias",
                      ConstructorAndroid.ProblemaDelAab(play + ".prueba", "ShowBies") != null);
        inf.Verdadero("build: el AAB no sale con otro paquete", ConstructorAndroid.ProblemaDelAab("com.ivanruiz.showbie", "ShowBies") != null);
        inf.Verdadero("build: el AAB no sale con el nombre de la APK de prueba",
                      ConstructorAndroid.ProblemaDelAab(play, "ShowBies (prueba)") != null);
        inf.Verdadero("build: la APK sale del paquete de Play", ConstructorAndroid.ProblemaDeLaApk(play, "ShowBies") == null);
        inf.Verdadero("build: la APK no arma un .prueba.prueba", ConstructorAndroid.ProblemaDeLaApk(play + ".prueba", "ShowBies") != null);
        inf.Verdadero("build: la APK no arma un nombre (prueba) (prueba)",
                      ConstructorAndroid.ProblemaDeLaApk(play, "ShowBies (prueba)") != null);

        var enOrden = new List<string>();
        foreach (string nombre in ConstructorAndroid.EscenasEnOrden) enOrden.Add("Assets/Escenas/" + nombre + ".unity");
        inf.Verdadero("build: las escenas del juego en su orden pasan", ConstructorAndroid.ProblemaDeEscenas(enOrden) == null);
        var cambiadas = new List<string>(enOrden);
        cambiadas[1] = enOrden[3];
        cambiadas[3] = enOrden[1];
        inf.Verdadero("build: con el libre y las oleadas cambiados de lugar no sale", ConstructorAndroid.ProblemaDeEscenas(cambiadas) != null);
        var sinUna = new List<string>(enOrden);
        sinUna.RemoveAt(sinUna.Count - 1);
        inf.Verdadero("build: sin el tutorial no sale", ConstructorAndroid.ProblemaDeEscenas(sinUna) != null);
        var conOtra = new List<string>(enOrden);
        conOtra.Insert(2, "Assets/Escenas/Prueba.unity");
        inf.Verdadero("build: con otra escena en el medio no sale", ConstructorAndroid.ProblemaDeEscenas(conOtra) != null);
        inf.Verdadero("build: sin escenas no sale", ConstructorAndroid.ProblemaDeEscenas(new List<string>()) != null);

        // Y lo que tiene hoy el proyecto pasa: si no, la build se va a negar.
        inf.Verdadero("build: el proyecto tiene el paquete y el nombre de Play (si falla, revertir ProjectSettings/ProjectSettings.asset)",
                      ConstructorAndroid.ProblemaDelAab(ConstructorAndroid.PaqueteAndroid, PlayerSettings.productName) == null);
        inf.Verdadero("build: las escenas prendidas del proyecto son las del juego, en su orden",
                      ConstructorAndroid.ProblemaDeEscenas(ConstructorAndroid.EscenasHabilitadas()) == null);
    }

    // Donde cae la granada: PlayerController.PuntoEnElPiso, estatica para probarla sin input.
    static void ProbarPuntoDeLaGranada(Informe inf)
    {
        var origen = new Vector3(2f, 1f, 3f);
        var adelante = new Vector3(0f, 0.5f, 1f);
        Vector3 p;

        p = PlayerController.PuntoEnElPiso(origen, origen + new Vector3(1f, 0f, 0f), adelante, 3f, 12f);
        inf.Verdadero("granada: apuntada a 1 m cae a la minima, 3 m en la misma direccion", Cerca(p, new Vector3(5f, 0f, 3f)));
        p = PlayerController.PuntoEnElPiso(origen, origen + new Vector3(0f, 0f, -20f), adelante, 3f, 12f);
        inf.Verdadero("granada: apuntada a 20 m cae a la maxima, 12 m", Cerca(p, new Vector3(2f, 0f, -9f)));
        p = PlayerController.PuntoEnElPiso(origen, origen + new Vector3(3.6f, 0f, 4.8f), adelante, 3f, 12f);
        inf.Verdadero("granada: apuntada a 6 m cae ahi", Cerca(p, new Vector3(5.6f, 0f, 7.8f)));
        p = PlayerController.PuntoEnElPiso(origen, origen + new Vector3(0f, 5f, 0f), adelante, 3f, 12f);
        inf.Verdadero("granada: apuntada encima del jugador usa adelante, aplanado, a la minima", Cerca(p, new Vector3(2f, 0f, 6f)));
        p = PlayerController.PuntoEnElPiso(origen, new Vector3(9f, 4f, 3f), adelante, 3f, 12f);
        inf.Cerca("granada: cae en el piso aunque el origen y lo apuntado esten en otra altura", 0, p.y, 1e-5);
        p = PlayerController.PuntoEnElPiso(origen, origen + new Vector3(0.5f, 0f, 0f), adelante, 8f, 8f);
        inf.Verdadero("granada: con minima = maxima, como el toque rapido del telefono, siempre a esa distancia", Cerca(p, new Vector3(10f, 0f, 3f)));

        // La tarjeta de la granada dice cada cuanto se tira ("granada cada 5 s") con el numero
        // escrito a mano en la tabla, y el de verdad es PlayerController.granadaCooldown, del
        // prefab del jugador: si se rebalancea ahi (o una escena se lo pisa) y no en el texto,
        // la tienda miente. Las escenas son las dos donde se usa la granada comprada.
        var prefabJugador = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Personajes/Jugador.prefab");
        var control = prefabJugador != null ? prefabJugador.GetComponent<PlayerController>() : null;
        if (!inf.Verdadero("granada: el prefab del jugador tiene su PlayerController", control != null)) return;
        float recarga = control.granadaCooldown;
        var numero = new System.Text.RegularExpressions.Regex(@"\d+(?:[.,]\d+)?");
        foreach (var lengua in new[] { Lengua.Ingles, Lengua.Espanol })
        {
            string texto = Textos.Crudo("mejora_granada_unidad", lengua) ?? "";
            var m = numero.Match(texto);
            double dice = m.Success ? double.Parse(m.Value.Replace(',', '.'), Invariante) : double.NaN;
            inf.Cerca("granada: \"" + texto + "\" (" + Idioma.Codigo(lengua) + ") dice el granadaCooldown del prefab del jugador",
                      recarga, dice, 1e-3);
        }
        foreach (string escena in new[] { "Assets/Escenas/ShowBies1.unity", "Assets/Escenas/WaveMode.unity" })
        {
            float enLaEscena = float.NaN;
            LeerEscena(escena, abierta =>
            {
                var jugador = Buscar<PlayerController>(abierta);
                if (jugador != null) enLaEscena = jugador.granadaCooldown;
            });
            inf.Cerca("granada: " + Path.GetFileNameWithoutExtension(escena) + " no le cambia el granadaCooldown al jugador",
                      recarga, enLaEscena, 1e-4);
        }
    }

    static bool Cerca(Vector3 a, Vector3 b)
    {
        return (a - b).sqrMagnitude < 1e-6f;
    }

    // Revivir sin balas no es volver a jugar: con cuantas vuelve (OfertaDeRevivir.BalasAlRevivir,
    // estatica para probarla sin escena).
    static void ProbarBalasAlRevivir(Informe inf)
    {
        inf.Igual("revivir: sin balas vuelve con el minimo", 500, OfertaDeRevivir.BalasAlRevivir(0, 500, 500));
        inf.Igual("revivir: con pocas balas vuelve con el minimo", 500, OfertaDeRevivir.BalasAlRevivir(30, 500, 500));
        inf.Igual("revivir: con mas balas que el minimo no se le quitan", 700, OfertaDeRevivir.BalasAlRevivir(700, 1000, 500));
        inf.Igual("revivir: con el cargador mejorado vuelve con el minimo, no lo llena como una caja", 500,
                  OfertaDeRevivir.BalasAlRevivir(0, 1000, 500));
        inf.Igual("revivir: nunca pasa del cargador", 300, OfertaDeRevivir.BalasAlRevivir(0, 300, 500));
        var oferta = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/OfertaRevivir.prefab");
        var revivir = oferta != null ? oferta.GetComponent<OfertaDeRevivir>() : null;
        var jugador = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Personajes/Jugador.prefab");
        var control = jugador != null ? jugador.GetComponent<PlayerController>() : null;
        inf.Verdadero("revivir: el prefab vuelve con balas, sin pasar del cargador de base",
                      revivir != null && control != null && revivir.balasMinimasAlRevivir > 0
                      && revivir.balasMinimasAlRevivir <= control.maxBalas);
    }

    // El sonido del jugo: el limitador de Sonidos (lo de siempre no cambia y una granada que
    // mata baja parejo), el daño al jugador segun cuanto entro, la escalera de la barra del
    // nivel, que haya un clic para los botones que no traen uno, la nota de los avisos de la
    // furia y como se importan el disparo y la musica del menu.
    static void ProbarSonidoDelJugo(Informe inf)
    {
        inf.Cerca("limitador: un tiro que mata (golpe y muerte) suena igual", 1, Sonidos.GananciaDelLimitador(0.35f + 0.55f, 0f), 1e-6);
        inf.Cerca("limitador: la muerte del jefe (golpe y muerte grande) suena igual", 1, Sonidos.GananciaDelLimitador(0.35f + 1f, 0f), 1e-6);
        inf.Cerca("limitador: una granada que mata baja parejo golpe, muerte y explosion", 1.4 / 1.9,
                  Sonidos.GananciaDelLimitador(0.35f + 0.55f + 1f, 0f), 1e-5);
        float conTodo = Sonidos.GananciaDelLimitador(1f, 20f);
        inf.Verdadero("limitador: con todo sonando baja mucho, pero nunca calla", conTodo > 0f && conTodo < 0.1f);
        inf.Cerca("limitador: lo que recien arranca pesa entero", 0.8, Sonidos.CargaQueQueda(0.8f, 0f, 0.1f), 1e-6);
        inf.Verdadero("limitador: el golpe deja de pesar enseguida", Sonidos.CargaQueQueda(1f, 0.1f, Sonidos.AtaqueDe(0.07f)) < 0.01f);
        inf.Verdadero("limitador: la explosion pesa mas tiempo que el golpe", Sonidos.AtaqueDe(1.3f) > 5f * Sonidos.AtaqueDe(0.07f));

        inf.Cerca("daño al jugador: sin saber cuanto, como un zarpazo comun", 0, Efectos.IntensidadDelDanio(0f), 1e-6);
        inf.Cerca("daño al jugador: un rasguño tampoco cambia", 0, Efectos.IntensidadDelDanio(0.03f), 1e-6);
        inf.Cerca("daño al jugador: un tercio de la vida es lo mas fuerte", 1, Efectos.IntensidadDelDanio(0.3f), 1e-6);
        inf.Cerca("daño al jugador: media vida no pasa de lo mas fuerte", 1, Efectos.IntensidadDelDanio(0.5f), 1e-6);

        inf.Igual("nivel: el primer nivel que cruza la barra suena una octava arriba", 12, VentanaLogros.SemitonosDelNivelCruzado(0));
        inf.Igual("nivel: el segundo, un grado mas", 14, VentanaLogros.SemitonosDelNivelCruzado(1));
        inf.Igual("nivel: el tercero sigue por la escala de la bemol", 16, VentanaLogros.SemitonosDelNivelCruzado(2));
        inf.Igual("nivel: el octavo llega a la octava de arriba", 24, VentanaLogros.SemitonosDelNivelCruzado(7));
        inf.Igual("nivel: pasada la octava vuelve a empezar", 12, VentanaLogros.SemitonosDelNivelCruzado(8));

        // Los botones sin sonidoClick usan el del primero que aparece con uno (MEJORAS, en el
        // menu) y, en una partida abierta directo, el golpe de Efectos.
        int conClic = 0;
        LeerEscena("Assets/Escenas/Menu.unity", escena =>
        {
            foreach (var raiz in escena.GetRootGameObjects())
            {
                foreach (var boton in raiz.GetComponentsInChildren<BotonJugoso>(true))
                {
                    if (boton.sonidoClick != null && boton.gameObject.activeInHierarchy) conClic++;
                }
            }
        });
        inf.Verdadero("clic: el menu tiene un boton prendido con clic, que se lo presta a los demas", conClic > 0);
        var prefabEfectos = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Jugo/Efectos.prefab");
        var efectos = prefabEfectos != null ? prefabEfectos.GetComponent<Efectos>() : null;
        inf.Verdadero("clic: en la partida queda el golpe de Efectos", efectos != null && efectos.golpe != null);

        // Los avisos de la furia son la nota del combo del HUD, en las escenas con furia.
        foreach (var ruta in new[] { "Assets/Escenas/WaveMode.unity", "Assets/Escenas/ShowBies1.unity" })
        {
            ContadorCombo combo = null;
            LeerEscena(ruta, escena => combo = Buscar<ContadorCombo>(escena));
            inf.Verdadero("furia: los avisos tienen la nota del combo en " + Path.GetFileNameWithoutExtension(ruta),
                          combo != null && combo.nota != null);
        }

        // El disparo viene a -27 dBFS, y el Normalize del importador solo actua al pasarlo
        // a mono: en mono sube ~27 dB. La musica del menu, comprimida y en segundo plano,
        // y en estereo (en mono, el Normalize le subiria el volumen).
        var disparo = AssetImporter.GetAtPath("Assets/otros/shot.mp3") as AudioImporter;
        inf.Verdadero("importacion: el disparo va en mono, que es lo que hace actuar su Normalize", disparo != null && disparo.forceToMono);
        var musica = AssetImporter.GetAtPath("Assets/otros/MainMenu.mp3") as AudioImporter;
        inf.Verdadero("importacion: la musica del menu no se descomprime entera al cargar el menu",
                      musica != null && musica.defaultSampleSettings.loadType != AudioClipLoadType.DecompressOnLoad && musica.loadInBackground);
        inf.Verdadero("importacion: la musica del menu sigue en estereo, con su volumen", musica != null && !musica.forceToMono);
    }

    // El gris de poca vida (pedido de Ivan): nada hasta un cuarto de la vida y de ahi, en
    // linea recta, hasta medio gris con la vida en cero. Al gris total llega recien al morir.
    static void ProbarGrisDePocaVida(Informe inf)
    {
        inf.Cerca("gris: con la vida llena, nada", 0, GrisDePocaVida.CantidadDeGris(1f, 0.25f, 0.5f), 1e-6);
        inf.Cerca("gris: justo con un cuarto, nada", 0, GrisDePocaVida.CantidadDeGris(0.25f, 0.25f, 0.5f), 1e-6);
        inf.Cerca("gris: con un octavo, un cuarto de gris", 0.25, GrisDePocaVida.CantidadDeGris(0.125f, 0.25f, 0.5f), 1e-6);
        inf.Cerca("gris: con la vida en cero, medio gris", 0.5, GrisDePocaVida.CantidadDeGris(0f, 0.25f, 0.5f), 1e-6);
        inf.Cerca("gris: con vida de menos no se pasa", 0.5, GrisDePocaVida.CantidadDeGris(-0.2f, 0.25f, 0.5f), 1e-6);
        var jugador = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Personajes/Jugador.prefab");
        var gris = jugador != null ? jugador.GetComponent<GrisDePocaVida>() : null;
        inf.Verdadero("gris: el jugador lo tiene, desde un cuarto de la vida y hasta medio gris",
                      gris != null && Mathf.Approximately(gris.desde, 0.25f) && Mathf.Approximately(gris.maximo, 0.5f));
    }

    // Abre una escena del juego para leerla, si no estaba abierta, y la cierra despues sin
    // guardar: las pruebas no escriben escenas.
    static void LeerEscena(string ruta, Action<Scene> leer)
    {
        Scene escena = SceneManager.GetSceneByPath(ruta);
        bool abrio = false;
        if (!escena.isLoaded)
        {
            escena = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Additive);
            abrio = true;
        }
        try
        {
            leer(escena);
        }
        finally
        {
            if (abrio) EditorSceneManager.CloseScene(escena, true);
        }
    }

    static T Buscar<T>(Scene escena) where T : Component
    {
        foreach (var raiz in escena.GetRootGameObjects())
        {
            var componente = raiz.GetComponentInChildren<T>(true);
            if (componente != null) return componente;
        }
        return null;
    }

    // El crecimiento de las monedas con que Economia y el Bestiario miden los premios, contra
    // el del WaveManager de WaveMode, que es el que paga de verdad. Esta copiado porque los
    // premios se calculan en el menu, sin escena: si se cambia en el inspector (TAREAS
    // propone 1,09) y no en Economia, las misiones, el cofre, el desafio, la diaria, el nivel
    // y las estrellas siguen midiendo con el viejo, y hasta el 24/9 ninguna prueba lo notaba.
    // Que el Bestiario lo use lo mira ProbarPuntosYMonedasDeLosZombis.
    static void ProbarCrecimientoDeMonedas(Informe inf)
    {
        float crecimiento = float.NaN;
        LeerEscena("Assets/Escenas/WaveMode.unity", escena =>
        {
            var oleadas = Buscar<WaveManager>(escena);
            if (oleadas != null) crecimiento = oleadas.crecimientoMonedas;
        });
        if (!inf.Verdadero("economia: WaveMode tiene su WaveManager, con el crecimiento de las monedas", !float.IsNaN(crecimiento))) return;

        // El campo es float: 1,08f no es exactamente 1,08, de ahi la tolerancia.
        inf.Cerca("economia: el crecimiento de las monedas es el del WaveManager de WaveMode",
                  crecimiento, Economia.CrecimientoMonedasOleadas, 1e-6);

        // Y MonedasPorPartida lo usa: de la oleada 20 a la 22, lo que deja cada zombi crece
        // exactamente eso, porque el multiplicador se toma a mitad de camino.
        double porZombi20 = Economia.MonedasPorPartida(20) / Economia.ZombisPorPartida(20);
        double porZombi22 = Economia.MonedasPorPartida(22) / Economia.ZombisPorPartida(22);
        inf.Cerca("economia: MonedasPorPartida crece con ese crecimiento",
                  Economia.CrecimientoMonedasOleadas, porZombi22 / porZombi20, 1e-9);
    }

    // Economia.ZombisPorPartida contra la suma de lo que saca de verdad el WaveManager de
    // WaveMode, oleada por oleada. Hasta el 23/9 le faltaban 2m zombis: el comentario decia
    // la suma y la cuenta era otra.
    static void ProbarZombisPorPartida(Informe inf)
    {
        int zombisBase = -1, zombisPorOleada = -1;
        LeerEscena("Assets/Escenas/WaveMode.unity", escena =>
        {
            var oleadas = Buscar<WaveManager>(escena);
            if (oleadas == null) return;
            zombisBase = oleadas.zombisBase;
            zombisPorOleada = oleadas.zombisPorOleada;
        });
        if (!inf.Verdadero("economia: WaveMode tiene su WaveManager", zombisBase >= 0)) return;

        bool iguales = true;
        string cual = "";
        foreach (int m in new[] { 3, 10, 25, 45 })
        {
            double suma = 0;
            for (int n = 1; n <= m; n++) suma += zombisBase + zombisPorOleada * n;
            if (Math.Abs(Economia.ZombisPorPartida(m) - suma) < 1e-9) continue;
            iguales = false;
            cual += m + ": " + Economia.ZombisPorPartida(m) + " en vez de " + suma + "; ";
        }
        inf.Verdadero("economia: ZombisPorPartida es la suma de lo que saca cada oleada" + (iguales ? "" : " (" + cual.Trim() + ")"), iguales);
    }

    // Con la tienda entera a la vista la camara del menu no dibuja nada (TiendaMejoras, con
    // su cullingMask en 0 desde que termina el fundido de entrada hasta que se cierra): eso
    // solo vale si la tienda tapa la pantalla entera por su cuenta. Sobre la tienda de
    // Menu.unity, con lo que la escena le pise: el fondo del panel prendido, liso, opaco y
    // estirado a todo el canvas, y el canvas overlay, que no pasa por la camara. Con un fondo
    // translucido detras se veria el color liso de la camara, y con un canvas de camara no se
    // veria la tienda.
    static void ProbarTiendaTapaLaEscena(Informe inf)
    {
        bool Estirado(RectTransform rt)
        {
            return rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one &&
                   rt.offsetMin == Vector2.zero && rt.offsetMax == Vector2.zero &&
                   rt.localScale == Vector3.one && rt.localRotation == Quaternion.identity;
        }

        bool hay = false, overlay = false, panelEntero = false, fondoEntero = false, liso = false;
        float alfa = float.NaN;
        LeerEscena("Assets/Escenas/Menu.unity", escena =>
        {
            var tienda = Buscar<TiendaMejoras>(escena);
            var panel = tienda != null && tienda.panel != null ? tienda.panel.transform as RectTransform : null;
            var fondo = panel != null ? panel.Find("Fondo") as RectTransform : null;
            var imagen = fondo != null ? fondo.GetComponent<UnityEngine.UI.Image>() : null;
            if (imagen == null) return;
            hay = true;

            // El de mas arriba: los canvas anidados dibujan como el suyo.
            var canvases = panel.GetComponentsInParent<Canvas>(true);
            var raiz = canvases.Length > 0 ? canvases[canvases.Length - 1] : null;
            overlay = raiz != null && raiz.renderMode == RenderMode.ScreenSpaceOverlay;
            panelEntero = raiz != null && panel.parent == raiz.transform && Estirado(panel);
            fondoEntero = Estirado(fondo);
            liso = fondo.gameObject.activeSelf && imagen.enabled && imagen.sprite == null;
            alfa = imagen.color.a;
        });
        if (!inf.Verdadero("tienda: el menu tiene la tienda, con su panel y el fondo del panel", hay)) return;
        inf.Verdadero("tienda: su canvas es overlay (no pasa por la camara del menu)", overlay);
        inf.Verdadero("tienda: el panel cuelga del canvas y lo ocupa entero", panelEntero);
        inf.Verdadero("tienda: el fondo del panel ocupa el panel entero", fondoEntero);
        inf.Verdadero("tienda: el fondo del panel esta prendido y es un color liso", liso);
        inf.Cerca("tienda: el fondo del panel es opaco", 1, alfa, 1e-3);
    }

    // Los avisos que pueden salir a la vez no se pisan: el de mision cumplida con el cartel
    // de la oleada ("completa N oleadas" se cumple al terminar una, que es cuando sale el de
    // la siguiente), con el del capitulo (en la 10, 20 y 30 tambien) y con la barra del
    // jefe, y ninguno con la vida, abajo. La franja de cada uno sale de su letra y del alto
    // de linea de la fuente, en 16:9 y en 20:9. Hasta el 23/9 el de mision iba en y = 300 y
    // se pisaba con el del capitulo y con la barra del jefe.
    static void ProbarAvisosSinPisarse(Informe inf)
    {
        var fuente = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Fuentes/Bangers SDF.asset");
        if (!inf.Verdadero("avisos: esta la fuente", fuente != null && fuente.faceInfo.pointSize > 0)) return;
        float linea = fuente.faceInfo.lineHeight / fuente.faceInfo.pointSize;

        float yOleada = float.NaN, letraOleada = 0f, letraVida = 0f;
        LeerEscena("Assets/Escenas/WaveMode.unity", escena =>
        {
            var oleadas = Buscar<WaveManager>(escena);
            if (oleadas != null && oleadas.cartelOleada != null)
            {
                var texto = oleadas.cartelOleada.GetComponentInChildren<TMPro.TMP_Text>(true);
                yOleada = ((RectTransform)oleadas.cartelOleada.transform).anchoredPosition.y;
                letraOleada = texto != null ? texto.fontSize : 0f;
            }
            var vida = Buscar<PlayerHealth>(escena);
            if (vida != null && vida.healthTMP != null)
                letraVida = vida.healthTMP.enableAutoSizing ? vida.healthTMP.fontSizeMax : vida.healthTMP.fontSize;
        });
        var pausa = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/MenuPausa.prefab");
        var barra = pausa != null ? pausa.GetComponentInChildren<BarraDelJefe>(true) : null;
        if (!inf.Verdadero("avisos: estan el cartel de la oleada, la vida y la barra del jefe",
                           !float.IsNaN(yOleada) && letraOleada > 0f && letraVida > 0f && barra != null)) return;

        Vector2 mision = Franja(AvisoDeMisiones.Altura, AvisoDeMisiones.Letra * (1f + AvisoDeMisiones.LetraDelDetalle) * linea);
        Vector2 capitulo = Franja(CapitulosDeEscenario.AlturaDelCartel, CapitulosDeEscenario.LetraDelCartel * (1f + CapitulosDeEscenario.LetraDelNombre) * linea);
        Vector2 oleada = Franja(yOleada, letraOleada * linea);

        foreach (var pantalla in new[] { new Vector2(16f, 9f), new Vector2(20f, 9f) })
        {
            float medioAlto = MedioAltoDelCanvas(pantalla);
            // La barra cuelga del borde de arriba; la vida va apoyada en el de abajo.
            var jefe = new Vector2(medioAlto - barra.PisoDesdeArriba, medioAlto - barra.TechoDesdeArriba);
            var vidaAbajo = new Vector2(-medioAlto, -medioAlto + letraVida * linea);
            string en = " (" + pantalla.x + ":" + pantalla.y + ")";
            inf.Verdadero("avisos: el de mision no pisa el cartel de la oleada" + en, !SePisan(mision, oleada));
            inf.Verdadero("avisos: el de mision no pisa el del capitulo" + en, !SePisan(mision, capitulo));
            inf.Verdadero("avisos: el de mision no pisa la barra del jefe" + en, !SePisan(mision, jefe));
            inf.Verdadero("avisos: el de mision no pisa la vida" + en, !SePisan(mision, vidaAbajo));
            inf.Verdadero("avisos: el del capitulo no pisa el de la oleada" + en, !SePisan(capitulo, oleada));
        }
    }

    // De donde a donde va un texto centrado en y, en unidades del canvas.
    static Vector2 Franja(float centro, float alto)
    {
        return new Vector2(centro - alto * 0.5f, centro + alto * 0.5f);
    }

    static bool SePisan(Vector2 a, Vector2 b)
    {
        return a.x < b.y && b.x < a.y;
    }

    // El medio alto de los canvas de las escenas de juego, en sus unidades, en una pantalla
    // de esa proporcion: con match 0,5 la escala es la raiz del ancho por el alto sobre el de
    // 1920 x 1080 (el del HUD tiene la referencia en vertical, que da lo mismo).
    static float MedioAltoDelCanvas(Vector2 proporcion)
    {
        float alto = 1080f, ancho = alto * proporcion.x / proporcion.y;
        float escala = Mathf.Sqrt(ancho * alto / (1920f * 1080f));
        return alto / escala * 0.5f;
    }

    // Los indices de escena estan escritos en el codigo, en literales (MainMenu.GameModes y
    // Tutorial, MenuPerdiste.Menu, MenuPausa, TutorialManager.IrAlMenu) y en constantes
    // (TiendaMejoras.EscenaMenu, EscenaModoLibre y EscenaOleadas, DerrotaEnLaPartida.
    // EscenaDerrota): reordenar Build Settings rompe la navegacion sin ningun aviso en la
    // build, y hasta el 24/9 ninguna prueba lo miraba. El orden es la tabla del CLAUDE.md.
    // Cuentan solo las escenas prendidas, que son las que numera SceneManager.LoadScene.
    static void ProbarOrdenDeEscenas(Informe inf)
    {
        string[] esperadas = { "Menu", "ShowBies1", "Perdiste", "WaveMode", "Tutorial" };
        var enElBuild = new List<string>();
        foreach (var escena in EditorBuildSettings.scenes)
        {
            if (escena != null && escena.enabled) enElBuild.Add(Path.GetFileNameWithoutExtension(escena.path));
        }
        for (int i = 0; i < esperadas.Length; i++)
        {
            inf.Igual("escenas: la " + i + " del build es " + esperadas[i], esperadas[i],
                      i < enElBuild.Count ? enElBuild[i] : "(no hay)");
        }
        inf.Igual("escenas: TiendaMejoras.EscenaMenu es el menu", 0, TiendaMejoras.EscenaMenu);
        inf.Igual("escenas: TiendaMejoras.EscenaModoLibre es el libre", 1, TiendaMejoras.EscenaModoLibre);
        inf.Igual("escenas: DerrotaEnLaPartida.EscenaDerrota es la derrota", 2, DerrotaEnLaPartida.EscenaDerrota);
        inf.Igual("escenas: TiendaMejoras.EscenaOleadas son las oleadas", 3, TiendaMejoras.EscenaOleadas);
    }

    // Los botones de vidrio del menu cambian con el tema: todo Image del menu con el color
    // de vidrio lleva PintarConTema con el papel Vidrio. Hasta el 23/9 el globo del idioma no
    // lo tenia, y sus tres copias (el engranaje, las misiones y el bestiario) tampoco: en
    // oscuro quedaban discos verde oscuro sobre la noche, y se leia solo el icono.
    static void ProbarVidriosDelMenu(Informe inf)
    {
        var vidrio = new Color(0.06f, 0.12f, 0.05f, 0.45f);
        int conVidrio = 0, sinPapel = 0;
        string cuales = "";
        LeerEscena("Assets/Escenas/Menu.unity", escena =>
        {
            foreach (var raiz in escena.GetRootGameObjects())
            {
                foreach (var imagen in raiz.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                {
                    Color c = imagen.color;
                    if (Mathf.Abs(c.r - vidrio.r) > 0.01f || Mathf.Abs(c.g - vidrio.g) > 0.01f ||
                        Mathf.Abs(c.b - vidrio.b) > 0.01f || Mathf.Abs(c.a - vidrio.a) > 0.01f) continue;
                    conVidrio++;
                    var papel = imagen.GetComponent<PintarConTema>();
                    if (papel != null && papel.rol == RolDeTema.Vidrio) continue;
                    sinPapel++;
                    cuales += imagen.transform.parent != null ? imagen.transform.parent.parent.name : imagen.name;
                    cuales += " ";
                }
            }
        });
        inf.Verdadero("tema: el menu tiene botones de vidrio", conVidrio > 0);
        inf.Verdadero("tema: todos los botones de vidrio del menu cambian con el tema" + (sinPapel == 0 ? "" : " (" + cuales.Trim() + ")"), sinPapel == 0);
    }

    // La tienda es de carbon neon (pedido de Ivan, 24/9; la viste ConstructorTienda): su
    // paleta es propia, igual en los dos temas, asi que ni la tienda ni la tarjeta llevan
    // PintarConTema, y el valor siguiente es el verde de la tarjeta en claro y en oscuro.
    // Lo que se lee, se lee: el contraste de cada texto contra lo que tiene detras. En una
    // escena de vista previa, para no tocar la abierta.
    static void ProbarTarjetaNeon(Informe inf, CatalogoMejoras c)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/TarjetaMejora.prefab");
        var prefabTienda = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Tienda.prefab");
        if (!inf.Verdadero("tarjeta: estan los prefabs", prefab != null && prefabTienda != null)) return;
        inf.Igual("tarjeta: sin PintarConTema, la paleta es propia", 0, prefab.GetComponentsInChildren<PintarConTema>(true).Length);
        inf.Igual("tienda: sin PintarConTema, la paleta es propia", 0, prefabTienda.GetComponentsInChildren<PintarConTema>(true).Length);

        var escena = EditorSceneManager.NewPreviewScene();
        try
        {
            var tarjeta = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, escena)).GetComponent<TarjetaMejora>();
            inf.Verdadero("tarjeta: tiene el borde de neon",
                          tarjeta.haloTarjeta != null && tarjeta.haloTarjeta.sprite != null && tarjeta.haloTarjeta.sprite.name == "NeonBorde");
            inf.Verdadero("tarjeta: el boton tiene su halo",
                          tarjeta.haloBoton != null && tarjeta.haloBoton.sprite != null && tarjeta.haloBoton.sprite.name == "NeonPildora");
            // El daño no tiene tope: siempre muestra el valor siguiente.
            foreach (bool oscuro in new[] { false, true })
            {
                Tema.UsarParaPruebas(oscuro);
                tarjeta.Configurar(c.danoBala, null, 0);
                inf.Verdadero("tarjeta: el valor siguiente es el verde de la tarjeta " + (oscuro ? "en oscuro" : "en claro"),
                              tarjeta.valorSiguiente != null && tarjeta.valorSiguiente.color == tarjeta.colorValorSiguiente);
            }

            var fondo = tarjeta.contenido.Find("Fondo").GetComponent<UnityEngine.UI.Image>().color;
            Contraste(inf, "tienda: el nombre sobre la tarjeta", tarjeta.nombre.color, fondo, 4.5);
            Contraste(inf, "tienda: el nivel sobre la tarjeta", tarjeta.nivel.color, fondo, 4.5);
            Contraste(inf, "tienda: la unidad sobre la tarjeta", tarjeta.descripcion.color, fondo, 4.5);
            Contraste(inf, "tienda: el valor siguiente sobre la tarjeta", tarjeta.colorValorSiguiente, fondo, 4.5);
            Contraste(inf, "tienda: el precio sobre el boton de comprar", tarjeta.colorPrecioComprable, tarjeta.colorComprable, 4.5);
            Contraste(inf, "tienda: el precio sobre el boton sin monedas", tarjeta.colorPrecioFalta, tarjeta.colorSinMonedas, 4.5);
            Contraste(inf, "tienda: MAX sobre el boton del tope", tarjeta.textoTope.color, tarjeta.colorTope, 4.5);
        }
        finally
        {
            Tema.UsarParaPruebas(null);
            EditorSceneManager.ClosePreviewScene(escena);
        }

        // Al abrir, la fila arranca al principio (abria centrada, con el daño afuera), salvo que
        // la primera tarjeta que se puede comprar no entre entera: entonces corre lo justo. Con
        // las medidas de 16:9: ocho tarjetas de 360 cada 380 desde 10 (3040) en una lista de 1872.
        inf.Cerca("tienda: al abrir con el daño comprable la fila arranca al principio", 0, TiendaMejoras.PosicionAlAbrir(3040f, 1872f, 370f), 1e-4);
        inf.Cerca("tienda: al abrir sin nada comprable la fila arranca al principio", 0, TiendaMejoras.PosicionAlAbrir(3040f, 1872f, float.NaN), 1e-4);
        inf.Cerca("tienda: con la granada como primera comprable corre lo justo", 28.0 / 1168.0, TiendaMejoras.PosicionAlAbrir(3040f, 1872f, 1890f), 1e-4);
        inf.Cerca("tienda: con el iman como primera comprable lo muestra entero", 408.0 / 1168.0, TiendaMejoras.PosicionAlAbrir(3040f, 1872f, 2270f), 1e-4);
        inf.Cerca("tienda: con la furia como primera comprable va al final", 1, TiendaMejoras.PosicionAlAbrir(3040f, 1872f, 3030f), 1e-4);
        inf.Cerca("tienda: una fila que entra entera no se corre", 0, TiendaMejoras.PosicionAlAbrir(1500f, 1872f, 1490f), 1e-4);

        // El toque que frena la fila no compra; con la fila casi quieta, o contra el borde al que
        // iba (en Clamped la velocidad sigue bajando sola con la fila ya quieta), si.
        float rapido = TiendaMejoras.VelocidadParaFrenar * 5f;
        inf.Verdadero("tienda: con la fila quieta el toque compra", !TiendaMejoras.SeEstaDeslizando(0f, 0.5f));
        inf.Verdadero("tienda: con la fila casi quieta el toque compra", !TiendaMejoras.SeEstaDeslizando(TiendaMejoras.VelocidadParaFrenar * 0.5f, 0.5f));
        inf.Verdadero("tienda: deslizandose hacia el final el toque la frena", TiendaMejoras.SeEstaDeslizando(-rapido, 0.5f));
        inf.Verdadero("tienda: deslizandose hacia el principio el toque la frena", TiendaMejoras.SeEstaDeslizando(rapido, 0.5f));
        inf.Verdadero("tienda: saliendo del principio el toque la frena", TiendaMejoras.SeEstaDeslizando(-rapido, 0f));
        inf.Verdadero("tienda: contra el principio el toque compra", !TiendaMejoras.SeEstaDeslizando(rapido, 0f));
        inf.Verdadero("tienda: contra el final el toque compra", !TiendaMejoras.SeEstaDeslizando(-rapido, 1f));

        // La flecha de la guia de la primera compra hacia A JUGAR no pisa las tarjetas. Iba
        // encima del boton, y en 20:9 y 21:9 (el pie queda a unos 80 de las tarjetas) su base
        // tapaba el pie de los botones de comprar; va a su izquierda, en el pie. Con las medidas
        // del prefab, sin area segura, en los extremos del rebote y con la sombra; el canvas de
        // la tienda es de 1920 x 1080 con match 0,5, como los de las escenas de juego. Las
        // tarjetas van centradas en la lista (la fila en la lista, y ellas en la fila).
        Rect EnElPadre(RectTransform rt, Rect padre)
        {
            Vector2 min = padre.min + Vector2.Scale(rt.anchorMin, padre.size) + rt.offsetMin;
            Vector2 max = padre.min + Vector2.Scale(rt.anchorMax, padre.size) + rt.offsetMax;
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
        var areaSegura = prefabTienda.transform.Find("Panel/AreaSegura");
        var lista = areaSegura != null ? areaSegura.Find("Tarjetas") as RectTransform : null;
        var pie = areaSegura != null ? areaSegura.Find("Pie") as RectTransform : null;
        var jugar = pie != null ? pie.Find("BotonJugar") as RectTransform : null;
        if (!inf.Verdadero("tienda: estan la lista, el pie y el boton de jugar", lista != null && jugar != null)) return;
        float medioAltoTarjeta = ((RectTransform)prefab.transform).sizeDelta.y * 0.5f;
        float mitadFlecha = GuiaPrimeraCompra.LadoFlecha * 0.5f;
        foreach (var pantalla in new[] { new Vector2(16f, 9f), new Vector2(20f, 9f), new Vector2(21f, 9f) })
        {
            float medioAlto = MedioAltoDelCanvas(pantalla);
            float medioAncho = medioAlto * pantalla.x / pantalla.y;
            var canvas = Rect.MinMaxRect(-medioAncho, -medioAlto, medioAncho, medioAlto);
            float pisoTarjetas = EnElPadre(lista, canvas).center.y - medioAltoTarjeta;
            Rect boton = EnElPadre(jugar, EnElPadre(pie, canvas));
            bool pisa = false, tapa = false, afuera = false;
            foreach (float distancia in new[] { GuiaPrimeraCompra.Separacion, GuiaPrimeraCompra.Separacion + GuiaPrimeraCompra.Rebote })
            {
                Vector2 centro = GuiaPrimeraCompra.CentroDeLaFlecha(boton, false, distancia);
                var flecha = Rect.MinMaxRect(centro.x - mitadFlecha, centro.y - mitadFlecha - GuiaPrimeraCompra.CaidaSombra,
                                             centro.x + mitadFlecha, centro.y + mitadFlecha);
                pisa |= flecha.yMax >= pisoTarjetas;
                tapa |= flecha.Overlaps(boton);
                afuera |= flecha.xMin < canvas.xMin || flecha.yMin < canvas.yMin;
            }
            string en = " (" + pantalla.x + ":" + pantalla.y + ")";
            inf.Verdadero("tienda: la flecha de A JUGAR no pisa las tarjetas" + en, !pisa);
            inf.Verdadero("tienda: la flecha de A JUGAR no tapa el boton" + en, !tapa);
            inf.Verdadero("tienda: la flecha de A JUGAR queda en la pantalla" + en, !afuera);
        }
    }

    // Los patrones del jefe (auditoria del 24/9), con el prefab en una escena de vista previa
    // (sin Awake: lo privado, por reflexion, y el EnemyController se le pone a mano):
    // - La linea de la carga tiene el ancho de su cuerpo: media 1,4 m fijos y la capsula
    //   barre casi 3, asi que el que se corria afuera de lo rojo se comia igual el golpe.
    // - Aturdido no tira zarpazos (es la ventana para castigarlo), y al volver a perseguir,
    //   o al postergar en plena embestida (el revivir), vuelven los zarpazos y el golpe normal.
    // - La embestida arranca sin el intervalo ni el zarpazo a medias de uno de antes: el
    //   primer choque no contaba (el jefe arrastraba al jugador) o la carga se cortaba sola.
    // - Embistiendo corre con el paso de lo que avanza: velocidadDelClipDeCorrer sale de
    //   Z_run_rm, que tiene que ser el clip de correr del controller.
    // - Solo ataca si se lo ve: con la camara de WaveMode, en 16:9 y en 20:9.
    static void ProbarPatronesDelJefe(Informe inf)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Personajes/ZombiBOSS.prefab");
        if (!inf.Verdadero("jefe: esta el prefab", prefab != null)) return;
        const System.Reflection.BindingFlags Privado = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var tipo = typeof(JefePatrones);
        var campoEstado = tipo.GetField("estado", Privado);
        var campoZombi = tipo.GetField("zombi", Privado);
        var aturdir = tipo.GetMethod("Aturdir", Privado);
        var terminar = tipo.GetMethod("Terminar", Privado);
        var estados = tipo.GetNestedType("Estado", System.Reflection.BindingFlags.NonPublic);
        var proximoGolpe = typeof(EnemyController).GetField("proximoGolpe", Privado);
        var golpeEnCurso = typeof(EnemyController).GetField("golpeEnCurso", Privado);
        if (!inf.Verdadero("jefe: se llega a sus patrones y a los golpes del zombi",
                           campoEstado != null && campoZombi != null && aturdir != null && terminar != null &&
                           estados != null && proximoGolpe != null && golpeEnCurso != null)) return;

        var escena = EditorSceneManager.NewPreviewScene();
        try
        {
            var jefe = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, escena)).GetComponent<JefePatrones>();
            var zombi = jefe.GetComponent<EnemyController>();
            campoZombi.SetValue(jefe, zombi);

            // La linea: tan ancha como la capsula, y las hitboxes caben adentro, a lo ancho y a
            // lo largo (asoman menos de 5 cm por delante de la capsula).
            var capsula = jefe.GetComponent<CapsuleCollider>();
            if (inf.Verdadero("jefe: tiene su capsula", capsula != null))
            {
                Transform raiz = jefe.transform;
                float radio, frente;
                JefePatrones.MedirElCuerpo(capsula, raiz.lossyScale, out radio, out frente);
                inf.Cerca("jefe: la linea de la carga tiene el ancho de la capsula",
                          2.0 * capsula.radius * Mathf.Abs(raiz.lossyScale.x), 2.0 * radio, 1e-4);
                float costado = 0f, adelante = float.NegativeInfinity;
                foreach (var caja in raiz.GetComponentsInChildren<BoxCollider>(true))
                {
                    for (int esquina = 0; esquina < 8; esquina++)
                    {
                        var signo = new Vector3((esquina & 1) == 0 ? -0.5f : 0.5f, (esquina & 2) == 0 ? -0.5f : 0.5f, (esquina & 4) == 0 ? -0.5f : 0.5f);
                        Vector3 enLaRaiz = raiz.InverseTransformPoint(caja.transform.TransformPoint(caja.center + Vector3.Scale(caja.size, signo)));
                        costado = Mathf.Max(costado, Mathf.Abs(enLaRaiz.x) * Mathf.Abs(raiz.lossyScale.x));
                        adelante = Mathf.Max(adelante, enLaRaiz.z * Mathf.Abs(raiz.lossyScale.z));
                    }
                }
                inf.Verdadero("jefe: las hitboxes caben a lo ancho de la linea de la carga", costado > 0f && costado <= radio + 1e-3f);
                inf.Verdadero("jefe: la linea llega hasta el frente de las hitboxes", adelante <= frente + 0.05f);
            }

            // Aturdido no ataca; volviendo a perseguir, si.
            zombi.puedeZarpar = true;
            aturdir.Invoke(jefe, new object[] { 10f });
            inf.Verdadero("jefe: aturdido no tira zarpazos", !zombi.puedeZarpar);
            inf.Verdadero("jefe: aturdido no pega con el cuerpo", !zombi.golpeaAlChocar);
            terminar.Invoke(jefe, new object[] { 11.3f });
            inf.Verdadero("jefe: al volver a perseguir vuelve a tirar zarpazos", zombi.puedeZarpar);

            // El revivir en plena embestida (Postergar): vuelve a perseguir con todo en su lugar.
            campoEstado.SetValue(jefe, Enum.Parse(estados, "Cargando"));
            zombi.golpeaAlChocar = true;
            zombi.multiplicadorGolpe = jefe.golpeDeLaCarga;
            zombi.puedeZarpar = false;
            jefe.Postergar(2.5f);
            inf.Verdadero("jefe: postergar en plena embestida devuelve los zarpazos y el golpe normal",
                          zombi.puedeZarpar && !zombi.golpeaAlChocar && zombi.multiplicadorGolpe == 1f);

            // La embestida arranca limpia.
            proximoGolpe.SetValue(zombi, 1e6f);
            golpeEnCurso.SetValue(zombi, true);
            zombi.EmpezarEmbestida();
            inf.Verdadero("jefe: la embestida no espera el intervalo de un zarpazo de antes", (float)proximoGolpe.GetValue(zombi) <= 0f);
            inf.Verdadero("jefe: la embestida corta el zarpazo a medias", !(bool)golpeEnCurso.GetValue(zombi));

            // El paso al embestir: la raiz de Z_run_rm avanza 2 m por ciclo (medido del FBX:
            // 78,74 pulgadas), asi que a escala 1 corre 2 m sobre lo que dura el clip.
            var controlador = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>("Assets/Animaciones/Zombi.controller");
            AnimationClip correr = null;
            if (controlador != null && controlador.layers.Length > 0)
            {
                foreach (var hijo in controlador.layers[0].stateMachine.states)
                {
                    if (hijo.state.name != "Andar" || !(hijo.state.motion is UnityEditor.Animations.BlendTree arbol)) continue;
                    foreach (var mezcla in arbol.children)
                        if (Mathf.Approximately(mezcla.threshold, 1f)) correr = mezcla.motion as AnimationClip;
                }
            }
            if (inf.Verdadero("jefe: el clip de correr del controller es Z_run_rm, el que se midio",
                              correr != null && AssetDatabase.GetAssetPath(correr).EndsWith("Z_run_rm.FBX")))
            {
                inf.Cerca("jefe: velocidadDelClipDeCorrer es lo que avanza Z_run_rm (2 m por ciclo)",
                          2.0 / correr.length, jefe.velocidadDelClipDeCorrer, 0.05);
            }

            // Solo ataca si se lo ve (su centro, con el margen), con la camara de WaveMode.
            Vector3 posicionCamara = Vector3.zero, jugador = Vector3.zero;
            Quaternion rotacionCamara = Quaternion.identity;
            float campoVisual = 0f, cerca = 0.3f, lejos = 1000f;
            LeerEscena("Assets/Escenas/WaveMode.unity", e =>
            {
                var seguidor = Buscar<CamaraJugador>(e);
                var control = Buscar<PlayerController>(e);
                var lente = seguidor != null ? seguidor.GetComponent<Camera>() : null;
                if (lente == null || control == null) return;
                posicionCamara = lente.transform.position;
                rotacionCamara = lente.transform.rotation;
                campoVisual = lente.fieldOfView;
                cerca = lente.nearClipPlane;
                lejos = lente.farClipPlane;
                jugador = control.transform.position;
            });
            if (inf.Verdadero("jefe: esta la camara del juego en WaveMode", campoVisual > 0f && capsula != null))
            {
                var objeto = UnityEditor.EditorUtility.CreateGameObjectWithHideFlags("prueba_camara_jefe", HideFlags.HideAndDontSave, typeof(Camera));
                try
                {
                    var camara = objeto.GetComponent<Camera>();
                    camara.enabled = false;
                    camara.transform.SetPositionAndRotation(posicionCamara, rotacionCamara);
                    camara.fieldOfView = campoVisual;
                    camara.nearClipPlane = cerca;
                    camara.farClipPlane = lejos;
                    // El centro del jefe parado en el piso: media capsula.
                    float alto = capsula.height * 0.5f * Mathf.Abs(jefe.transform.lossyScale.y);
                    foreach (var pantalla in new[] { new Vector2(16f, 9f), new Vector2(20f, 9f) })
                    {
                        camara.aspect = pantalla.x / pantalla.y;
                        string en = " (" + pantalla.x + ":" + pantalla.y + ")";
                        Func<float, float, bool> seVe = (x, z) => JefePatrones.DentroDelCuadro(
                            camara.WorldToViewportPoint(new Vector3(jugador.x + x, alto, jugador.z + z)), jefe.margenEnPantalla);
                        inf.Verdadero("jefe: a 3 m por delante del jugador se lo ve y ataca" + en, seVe(0f, 3f));
                        inf.Verdadero("jefe: a 5 m al costado del jugador se lo ve y ataca" + en, seVe(5f, 0f));
                        inf.Verdadero("jefe: a 12 m por detras, fuera de cuadro, no ataca" + en, !seVe(0f, -12f));
                        inf.Verdadero("jefe: a 12 m por delante, fuera de cuadro, no ataca" + en, !seVe(0f, 12f));
                    }
                }
                finally
                {
                    Object.DestroyImmediate(objeto);
                }
            }
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(escena);
        }
    }

    // Al terminar un patron el jefe no gira: la raiz la gira EnemyController, mirando al
    // jugador. Hasta el 23/9 Terminar le aplicaba un rumbo guardado al aturdirse, y al salir de
    // invocar el jefe pegaba un salto de giro hasta el paso de fisica siguiente. Y aturdido la
    // raiz no se tambalea, que el tambaleo es del modelo: hasta el 24/9 Mover la balanceaba en
    // Z, a paso de fisica y sin decaer, encima del del modelo. Los estados son privados: por
    // reflexion, con el prefab en una escena de vista previa (sin Awake, asi que el
    // EnemyController se le pone a mano).
    static void ProbarJefeAlTerminar(Informe inf)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Personajes/ZombiBOSS.prefab");
        if (!inf.Verdadero("jefe: esta el prefab", prefab != null)) return;
        const System.Reflection.BindingFlags Privado = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var tipo = typeof(JefePatrones);
        var campoEstado = tipo.GetField("estado", Privado);
        var campoZombi = tipo.GetField("zombi", Privado);
        var terminar = tipo.GetMethod("Terminar", Privado);
        var estados = tipo.GetNestedType("Estado", System.Reflection.BindingFlags.NonPublic);
        if (!inf.Verdadero("jefe: se llega a sus estados", campoEstado != null && campoZombi != null && terminar != null && estados != null)) return;

        var escena = EditorSceneManager.NewPreviewScene();
        try
        {
            var jefe = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, escena)).GetComponent<JefePatrones>();
            campoZombi.SetValue(jefe, jefe.GetComponent<EnemyController>());

            // Saliendo de invocar: mira a 30 grados.
            jefe.transform.rotation = Quaternion.Euler(0f, 30f, 0f);
            campoEstado.SetValue(jefe, Enum.Parse(estados, "AvisandoInvocar"));
            terminar.Invoke(jefe, new object[] { 10f });
            inf.Cerca("jefe: al terminar de invocar no gira", 30, jefe.transform.eulerAngles.y, 0.01);

            // Aturdido, con el rumbo de la carga: ni Mover ni Terminar tocan la raiz.
            var rumbo = Quaternion.Euler(0f, 90f, 0f);
            jefe.transform.rotation = rumbo;
            campoEstado.SetValue(jefe, Enum.Parse(estados, "Aturdido"));
            var cuerpo = jefe.GetComponent<Rigidbody>();
            bool loMueve = true;
            for (int paso = 0; paso < 5; paso++) loMueve &= jefe.Mover(cuerpo, jefe.transform);
            inf.Verdadero("jefe: aturdido lo mueve JefePatrones (quieto)", loMueve);
            inf.Verdadero("jefe: aturdido la raiz no se tambalea", Quaternion.Angle(jefe.transform.rotation, rumbo) < 0.01f);
            terminar.Invoke(jefe, new object[] { 10f });
            inf.Verdadero("jefe: al terminar el aturdimiento sigue con el rumbo de la carga",
                          Quaternion.Angle(jefe.transform.rotation, rumbo) < 0.01f);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(escena);
        }
    }

    // 7. Migracion desde v1, archivos rotos y normalizacion.
    static void ProbarGuardado(Informe inf)
    {
        // La oleada a medias: un JSON sin los campos la lee como 0, se guarda y se
        // relee, y olvidarla la deja en 0.
        string ruta0 = EmpezarCaso("{\"version\":3,\"monedas\":1}", null);
        inf.Igual("oleada en curso: sin campo vale 0", 0, Progreso.OleadaEnCurso);
        Progreso.GuardarOleadaEnCurso(15, 1234);
        Progreso.UsarCarpetaDePruebas(CarpetaProgreso);
        inf.Igual("oleada en curso: se relee la oleada", 15, Progreso.OleadaEnCurso);
        inf.Igual("oleada en curso: se releen los puntos", 1234, Progreso.PuntosEnCurso);
        Progreso.OlvidarOleadaEnCurso();
        Progreso.UsarCarpetaDePruebas(CarpetaProgreso);
        inf.Igual("oleada en curso: olvidada vale 0", 0, Progreso.OleadaEnCurso);
        inf.Igual("oleada en curso: olvidada sin puntos", 0, Progreso.PuntosEnCurso);
        inf.Verdadero("oleada en curso: existe el archivo", File.Exists(ruta0));

        // v1: se lee, se respalda tal cual y se guarda como v2.
        string v1 = "{\"version\":1,\"monedas\":123.5,\"mejorOleada\":7}";
        string ruta = EmpezarCaso(v1, null);
        inf.Cerca("guardado v1: monedas", 123.5, Progreso.Monedas, 1e-9);
        inf.Igual("guardado v1: mejor oleada", 7, Progreso.MejorOleada);
        inf.Igual("guardado v1: nivel de dano_bala", 0, Progreso.Nivel("dano_bala"));
        inf.Igual("guardado v1: progreso.json.v1.bak igual al original", v1, LeerSiExiste(ruta + ".v1.bak"));
        Progreso.Guardar();
        string guardado = LeerSiExiste(ruta);
        inf.Verdadero("guardado v1: el JSON guardado dice la version actual",
                      guardado != null && guardado.Contains("\"version\": " + Progreso.VersionActual));
        inf.Verdadero("guardado v1: el JSON guardado tiene \"mejoras\"", guardado != null && guardado.Contains("\"mejoras\""));

        // Version mas nueva que la del build (otra rama o volver atras): se lee lo
        // que se entiende, se respalda y no se escribe nada, ni guardando ni
        // comprando. Reiniciar desde las herramientas si lo pisa.
        string futuro = "{\"version\":99,\"monedas\":77,\"mejorOleada\":4,\"cerebros\":12," +
                        "\"mejoras\":[{\"id\":\"dano_bala\",\"nivel\":3}]}";
        ruta = EmpezarCaso(futuro, null);
        inf.Cerca("guardado futuro: monedas", 77, Progreso.Monedas, 1e-9);
        inf.Igual("guardado futuro: nivel de dano_bala", 3, Progreso.Nivel("dano_bala"));
        inf.Verdadero("guardado futuro: queda en solo lectura", Progreso.SoloLectura);
        inf.Igual("guardado futuro: progreso.json.v99.futuro.bak igual al original", futuro, LeerSiExiste(ruta + ".v99.futuro.bak"));
        Progreso.Sumar(5);
        Progreso.DepurarFijarNivel("cadencia", 2);
        Progreso.Guardar();
        inf.Igual("guardado futuro: Guardar no pisa el archivo", futuro, LeerSiExiste(ruta));
        inf.Verdadero("guardado futuro: no queda .tmp", !File.Exists(ruta + ".tmp"));
        Progreso.ReiniciarTodo();
        guardado = LeerSiExiste(ruta);
        inf.Verdadero("guardado futuro: Reiniciar escribe la version actual",
                      guardado != null && guardado.Contains("\"version\": " + Progreso.VersionActual));
        inf.Verdadero("guardado futuro: despues de Reiniciar ya no es solo lectura", !Progreso.SoloLectura);
        inf.Igual("guardado futuro: el .futuro.bak sigue despues de Reiniciar", futuro, LeerSiExiste(ruta + ".v99.futuro.bak"));

        // Sin version: vale 0 y se respalda como v0.
        ruta = EmpezarCaso("{\"monedas\":10}", null);
        inf.Cerca("guardado sin version: monedas", 10, Progreso.Monedas, 1e-9);
        inf.Verdadero("guardado sin version: existe progreso.json.v0.bak", File.Exists(ruta + ".v0.bak"));

        // Principal roto con .tmp sano: se carga el .tmp, el roto queda aparte y
        // el respaldo de version es del archivo que se leyo.
        string roto = "{\"monedas\": 5";
        string tmp = "{\"version\":1,\"monedas\":42}";
        ruta = EmpezarCaso(roto, tmp);
        inf.Cerca("guardado roto con .tmp: carga el .tmp", 42, Progreso.Monedas, 1e-9);
        inf.Igual("guardado roto con .tmp: progreso.json.roto es el principal roto", roto, LeerSiExiste(ruta + ".roto"));
        inf.Igual("guardado roto con .tmp: progreso.json.v1.bak es el .tmp", tmp, LeerSiExiste(ruta + ".v1.bak"));

        // Principal roto sin .tmp: arranca de cero y el roto no se pierde al guardar.
        ruta = EmpezarCaso(roto, null);
        inf.Cerca("guardado roto sin .tmp: arranca en 0", 0, Progreso.Monedas, 1e-9);
        inf.Igual("guardado roto sin .tmp: sin .bak", 0, Directory.GetFiles(CarpetaProgreso, "*.bak").Length);
        Progreso.Guardar();
        inf.Igual("guardado roto sin .tmp: el .roto sigue despues de Guardar", roto, LeerSiExiste(ruta + ".roto"));

        // Guardar escribe el .tmp, aparta el principal como .anterior y pone el .tmp en su
        // lugar: en disco queda siempre una copia entera. Un corte despues de apartar el
        // principal deja solo el .anterior, y se carga ese en vez de arrancar de cero.
        ruta = EmpezarCaso(null, null);
        File.WriteAllText(ruta + ".anterior", "{\"version\":" + Progreso.VersionActual + ",\"monedas\":77}");
        inf.Cerca("guardado cortado: sin principal ni .tmp carga el .anterior", 77, Progreso.Monedas, 1e-9);

        ruta = EmpezarCaso("{\"version\":" + Progreso.VersionActual + ",\"monedas\":10}", null);
        _ = Progreso.Monedas;
        Progreso.DepurarFijarMonedas(20);
        var principalGuardado = LeerGuardado(ruta);
        var anteriorGuardado = LeerGuardado(ruta + ".anterior");
        inf.Verdadero("guardado: el principal queda con lo nuevo", principalGuardado != null && Math.Abs(principalGuardado.monedas - 20) < 1e-9);
        inf.Verdadero("guardado: el .anterior queda con lo de antes", anteriorGuardado != null && Math.Abs(anteriorGuardado.monedas - 10) < 1e-9);
        inf.Verdadero("guardado: no queda .tmp", !File.Exists(ruta + ".tmp"));

        // Normalizacion: monedas negativas, ids repetidos, vacios y desconocidos.
        string sucio = "{\"version\":2,\"monedas\":-5,\"mejorOleada\":3,\"mejoras\":[" +
                       "{\"id\":\"dano_bala\",\"nivel\":2},{\"id\":\"dano_bala\",\"nivel\":4}," +
                       "{\"id\":\"\",\"nivel\":3},{\"id\":\"viejo\",\"nivel\":7},{\"id\":\"cadencia\",\"nivel\":-1}]}";
        ruta = EmpezarCaso(sucio, null);
        inf.Cerca("normalizacion: monedas negativas pasan a 0", 0, Progreso.Monedas, 1e-9);
        inf.Igual("normalizacion: dano_bala repetido se queda con el mayor", 4, Progreso.Nivel("dano_bala"));
        inf.Igual("normalizacion: nivel negativo pasa a 0", 0, Progreso.Nivel("cadencia"));
        inf.Igual("normalizacion: id desconocido se conserva", 7, Progreso.Nivel("viejo"));
        Progreso.Guardar();

        var json = LeerGuardado(ruta);
        inf.Verdadero("normalizacion: el JSON guardado se puede leer", json != null && json.mejoras != null);
        if (json == null || json.mejoras == null) return;

        int vacios = 0;
        int repetidos = 0;
        var vistos = new List<string>();
        foreach (var m in json.mejoras)
        {
            if (m == null || string.IsNullOrEmpty(m.id)) { vacios++; continue; }
            if (vistos.Contains(m.id)) repetidos++;
            else vistos.Add(m.id);
        }
        inf.Igual("normalizacion: el JSON guardado no tiene ids vacios", 0, vacios);
        inf.Igual("normalizacion: el JSON guardado no tiene ids repetidos", 0, repetidos);
        inf.Igual("normalizacion: el JSON guardado conserva viejo", 7, NivelGuardadoDe(json, "viejo"));
    }

    // Los getters que usa AplicarMejoras, sin mejoras y con los niveles de prueba.
    static void ProbarGetters(Informe inf, CatalogoMejoras c)
    {
        EmpezarCaso(null, null);
        inf.Cerca("getters nivel 0: DanoPorBala", 1, CatalogoMejoras.DanoPorBala, Tolerancia);
        inf.Cerca("getters nivel 0: TirosPorSegundo", 4, CatalogoMejoras.TirosPorSegundo, Tolerancia);
        inf.Igual("getters nivel 0: VidaMaxima", 80, CatalogoMejoras.VidaMaxima);
        inf.Cerca("getters nivel 0: MultiplicadorVida", 1, CatalogoMejoras.MultiplicadorVida, Tolerancia);
        inf.Cerca("getters nivel 0: RadioIman", 0, CatalogoMejoras.RadioIman, Tolerancia);
        inf.Cerca("getters nivel 0: MultiplicadorBotin", 1, CatalogoMejoras.MultiplicadorBotin, Tolerancia);
        inf.Verdadero("getters nivel 0: FuriaDesbloqueada es falso", !CatalogoMejoras.FuriaDesbloqueada);
        inf.Cerca("getters nivel 0: DuracionFuria", 0, CatalogoMejoras.DuracionFuria, Tolerancia);
        inf.Verdadero("getters nivel 0: GranadaDesbloqueada es falso", !CatalogoMejoras.GranadaDesbloqueada);
        inf.Cerca("getters nivel 0: ProbabilidadCritico", 0, CatalogoMejoras.ProbabilidadCritico, Tolerancia);
        Progreso.DepurarFijarNivel(c.criticos.id, 5);
        inf.Cerca("getters criticos 5: ProbabilidadCritico", 0.5f, CatalogoMejoras.ProbabilidadCritico, Tolerancia);
        Progreso.DepurarFijarNivel(c.criticos.id, 0);
        Progreso.DepurarFijarNivel(c.granada.id, 1);
        inf.Verdadero("getters granada 1: GranadaDesbloqueada", CatalogoMejoras.GranadaDesbloqueada);
        Progreso.DepurarFijarNivel(c.granada.id, 0);

        // Volumen: la cuenta de cada fuente y los topes.
        inf.Cerca("volumen: base por jugador", 0.13f, FuenteConVolumen.Calcular(0.26f, 0.5f, 1f), Tolerancia);
        inf.Cerca("volumen: con la tienda bajando la musica", 0.25f, FuenteConVolumen.Calcular(1f, 0.5f, 0.5f), Tolerancia);
        inf.Cerca("volumen: en cero no suena", 0f, FuenteConVolumen.Calcular(1f, 0f, 1f), Tolerancia);
        inf.Cerca("volumen: un valor de mas se topa", 1f, FuenteConVolumen.Calcular(1f, 3f, 1f), Tolerancia);

        // El modo libre se desbloquea al llegar a la oleada 12 (completar la 11).
        inf.Verdadero("modo libre: sin oleadas bloqueado", !ModoLibre.DesbloqueadoCon(0));
        inf.Verdadero("modo libre: completada la 10 bloqueado", !ModoLibre.DesbloqueadoCon(10));
        inf.Verdadero("modo libre: completada la 11 (llego a la 12) libre", ModoLibre.DesbloqueadoCon(11));
        inf.Verdadero("modo libre: completada la 30 libre", ModoLibre.DesbloqueadoCon(30));
        inf.Igual("modo libre: sin progreso lleva a las oleadas", TiendaMejoras.EscenaOleadas, ModoLibre.EscenaPara(TiendaMejoras.EscenaModoLibre));
        inf.Igual("modo libre: las oleadas no cambian", TiendaMejoras.EscenaOleadas, ModoLibre.EscenaPara(TiendaMejoras.EscenaOleadas));

        Progreso.DepurarFijarNivel(c.danoBala.id, 5);
        Progreso.DepurarFijarNivel(c.cadencia.id, 10);
        Progreso.DepurarFijarNivel(c.vidaMaxima.id, 5);
        Progreso.DepurarFijarNivel(c.iman.id, 6);
        Progreso.DepurarFijarNivel(c.botin.id, 15);
        inf.Cerca("getters 5/10/5/6/15: DanoPorBala", 6, CatalogoMejoras.DanoPorBala, Tolerancia);
        inf.Cerca("getters 5/10/5/6/15: TirosPorSegundo", 14, CatalogoMejoras.TirosPorSegundo, Tolerancia);
        inf.Igual("getters 5/10/5/6/15: VidaMaxima", 180, CatalogoMejoras.VidaMaxima);
        inf.Cerca("getters 5/10/5/6/15: MultiplicadorVida", 2.25, CatalogoMejoras.MultiplicadorVida, Tolerancia);
        inf.Cerca("getters 5/10/5/6/15: RadioIman", 4.5, CatalogoMejoras.RadioIman, Tolerancia);
        inf.Cerca("getters 5/10/5/6/15: MultiplicadorBotin", 2.5, CatalogoMejoras.MultiplicadorBotin, Tolerancia);

        Progreso.DepurarFijarNivel(c.furia.id, 1);
        inf.Verdadero("getters furia comprada: FuriaDesbloqueada", CatalogoMejoras.FuriaDesbloqueada);
        inf.Cerca("getters furia comprada: DuracionFuria", 6, CatalogoMejoras.DuracionFuria, Tolerancia);
    }

    // 8. Compras, con el daño de bala del catalogo (40 el nivel 0, 58 el 1).
    static void ProbarCompras(Informe inf, CatalogoMejoras c, List<Mejora> temporales)
    {
        Mejora dano = c.danoBala;
        const string Comprada = "Comprada";
        const string SinMonedas = "SinMonedas";

        // Justas.
        EmpezarConMonedas(40);
        int revision = Progreso.Revision;
        inf.Igual("compras con 40: primera", Comprada, Progreso.Comprar(dano).ToString());
        inf.Cerca("compras con 40: quedan 0", 0, Progreso.Monedas, 1e-9);
        inf.Igual("compras con 40: nivel 1", 1, Progreso.Nivel(dano.id));
        inf.Igual("compras con 40: la compra sube la Revision en 1", revision + 1, Progreso.Revision);
        inf.Igual("compras con 40: segunda", SinMonedas, Progreso.Comprar(dano).ToString());
        inf.Igual("compras con 40: el rechazo no sube la Revision", revision + 1, Progreso.Revision);

        // Un pelo abajo por coma flotante: alcanza y nunca queda negativo.
        EmpezarConMonedas(39.9999999);
        revision = Progreso.Revision;
        inf.Igual("compras con 39,9999999: primera", Comprada, Progreso.Comprar(dano).ToString());
        inf.Cerca("compras con 39,9999999: quedan 0", 0, Progreso.Monedas, 1e-9);
        inf.Verdadero("compras con 39,9999999: nunca negativo", Progreso.Monedas >= 0);
        inf.Igual("compras con 39,9999999: la compra sube la Revision en 1", revision + 1, Progreso.Revision);

        // Un centavo abajo: no alcanza.
        EmpezarConMonedas(39.99);
        revision = Progreso.Revision;
        inf.Igual("compras con 39,99: MonedasEnteras", 39, Progreso.MonedasEnteras);
        inf.Igual("compras con 39,99: primera", SinMonedas, Progreso.Comprar(dano).ToString());
        inf.Cerca("compras con 39,99: no descuenta", 39.99, Progreso.Monedas, 1e-9);
        inf.Igual("compras con 39,99: nivel 0", 0, Progreso.Nivel(dano.id));
        inf.Igual("compras con 39,99: el rechazo no sube la Revision", revision, Progreso.Revision);

        // Con resto, y la compra queda en disco en el acto.
        string ruta = Path.Combine(CarpetaProgreso, "progreso.json");
        EmpezarConMonedas(80.7);
        revision = Progreso.Revision;
        inf.Igual("compras con 80,7: primera", Comprada, Progreso.Comprar(dano).ToString());
        inf.Cerca("compras con 80,7: quedan 40,7", 40.7, Progreso.Monedas, 1e-9);
        inf.Igual("compras con 80,7: segunda (cuesta 58)", SinMonedas, Progreso.Comprar(dano).ToString());
        inf.Igual("compras con 80,7: Revision sube 1 en total", revision + 1, Progreso.Revision);
        var enDisco = LeerGuardado(ruta);
        inf.Igual("compras con 80,7: Comprar guarda el nivel en disco", 1, NivelGuardadoDe(enDisco, dano.id));
        inf.Cerca("compras con 80,7: Comprar guarda las monedas en disco", 40.7, enDisco != null ? enDisco.monedas : double.NaN, 1e-9);

        // Persistencia: volver a cargar la misma carpeta sin borrarla.
        Progreso.UsarCarpetaDePruebas(CarpetaProgreso);
        inf.Igual("compras persistencia: el nivel sigue al recargar", 1, Progreso.Nivel(dano.id));
        inf.Cerca("compras persistencia: las monedas siguen al recargar", 40.7, Progreso.Monedas, 1e-9);

        // Dos seguidas que dejan en cero.
        EmpezarConMonedas(98);
        revision = Progreso.Revision;
        inf.Igual("compras con 98: primera", Comprada, Progreso.Comprar(dano).ToString());
        inf.Igual("compras con 98: segunda", Comprada, Progreso.Comprar(dano).ToString());
        inf.Cerca("compras con 98: quedan 0", 0, Progreso.Monedas, 1e-9);
        inf.Igual("compras con 98: nivel 2", 2, Progreso.Nivel(dano.id));
        inf.Igual("compras con 98: dos compras suben la Revision en 2", revision + 2, Progreso.Revision);

        // Al tope no se cobra aunque sobren monedas.
        EmpezarConMonedas(1e6);
        Progreso.DepurarFijarNivel(c.cadencia.id, 16);
        revision = Progreso.Revision;
        inf.Igual("compras cadencia al tope: Estado", "EnTope", Progreso.Estado(c.cadencia).ToString());
        inf.Igual("compras cadencia al tope: Comprar", "EnTope", Progreso.Comprar(c.cadencia).ToString());
        inf.Cerca("compras cadencia al tope: no descuenta", 1e6, Progreso.Monedas, 1e-9);
        inf.Igual("compras cadencia al tope: nivel sigue en 16", 16, Progreso.Nivel(c.cadencia.id));
        inf.Igual("compras cadencia al tope: el rechazo no sube la Revision", revision, Progreso.Revision);

        // Un nivel guardado por encima del tope (un tope que bajo en un update)
        // cuenta como tope y el efecto no pasa del maximo.
        var conTope = CrearTemporal(temporales, "prueba_tope", 45, 1.45, 10, CrecimientoEfecto.Aditivo, 0.08, 1, FormatoValor.UnDecimal);
        EmpezarConMonedas(1e6);
        Progreso.DepurarFijarNivel(conTope.id, 15);
        inf.Igual("compras tope 10 con nivel 15: Estado", "EnTope", Progreso.Estado(conTope).ToString());
        inf.Igual("compras tope 10 con nivel 15: Comprar", "EnTope", Progreso.Comprar(conTope).ToString());
        inf.Cerca("compras tope 10 con nivel 15: Multiplicador(15)", 1.8, conTope.Multiplicador(15), 1e-9);

        // Invalidas.
        var sinId = CrearTemporal(temporales, "", 45, 1.45, 0, CrecimientoEfecto.Aditivo, 0.15, 1, FormatoValor.UnDecimal);
        EmpezarConMonedas(1e6);
        revision = Progreso.Revision;
        inf.Igual("compras invalidas: Estado(null)", "Invalida", Progreso.Estado(null).ToString());
        inf.Igual("compras invalidas: Comprar(null)", "Invalida", Progreso.Comprar(null).ToString());
        inf.Igual("compras invalidas: Estado sin id", "Invalida", Progreso.Estado(sinId).ToString());
        inf.Igual("compras invalidas: Comprar sin id", "Invalida", Progreso.Comprar(sinId).ToString());
        inf.Igual("compras invalidas: no suben la Revision", revision, Progreso.Revision);
        inf.Cerca("compras invalidas: no descuentan", 1e6, Progreso.Monedas, 1e-9);
    }

    // 9. Compras posibles encadenadas desde nivel 0, siempre la mas barata:
    // iman 30 + dano 40 + vida 40 + iman 45 + cadencia 50.
    // 12. Anuncios: la migracion a v3, los contadores del progreso, el premio que
    // se cobra una sola vez y las condiciones para ofrecer un video.
    static void ProbarAnuncios(Informe inf)
    {
        // --- migracion v2 -> v3 -------------------------------------------------
        string v2 = "{\"version\":2,\"monedas\":500,\"mejorOleada\":4}";
        string ruta = EmpezarCaso(v2, null);
        inf.Cerca("anuncios v2: monedas", 500, Progreso.Monedas, 1e-9);
        inf.Igual("anuncios v2: partidas terminadas arranca en 0", 0, Progreso.PartidasTerminadas);
        inf.Cerca("anuncios v2: segundos jugados arranca en 0", 0, Progreso.SegundosJugados, 1e-9);
        inf.Verdadero("anuncios v2: los videos arrancan ofrecidos", Progreso.OfrecerVideos);
        inf.Igual("anuncios v2: progreso.json.v2.bak igual al original", v2, LeerSiExiste(ruta + ".v2.bak"));
        Progreso.Guardar();
        inf.Igual("anuncios v2: se guarda como v" + Progreso.VersionActual,
                  Progreso.VersionActual, LeerGuardado(ruta) != null ? LeerGuardado(ruta).version : -1);

        // --- TerminarPartida ----------------------------------------------------
        EmpezarConMonedas(0);
        Progreso.TerminarPartida(30f);
        Progreso.TerminarPartida(12.5f);
        inf.Igual("anuncios: dos partidas terminadas", 2, Progreso.PartidasTerminadas);
        inf.Cerca("anuncios: segundos jugados sumados", 42.5, Progreso.SegundosJugados, 1e-3);
        inf.Cerca("anuncios: segundos de la ultima partida", 12.5, Progreso.SegundosDeLaUltimaPartida, 1e-3);

        Progreso.TerminarPartida(float.NaN);
        inf.Cerca("anuncios: un NaN no ensucia los segundos", 42.5, Progreso.SegundosJugados, 1e-3);
        Progreso.TerminarPartida(-5f);
        inf.Cerca("anuncios: un negativo no resta", 42.5, Progreso.SegundosJugados, 1e-3);
        inf.Igual("anuncios: pero las partidas si se cuentan", 4, Progreso.PartidasTerminadas);

        // --- el interruptor guarda en el acto -----------------------------------
        EmpezarConMonedas(0);
        ruta = Path.Combine(CarpetaProgreso, "progreso.json");
        Progreso.OfrecerVideos = false;
        inf.Verdadero("anuncios: el interruptor queda apagado", !(Progreso.OfrecerVideos));
        inf.Verdadero("anuncios: apagarlo guarda en el acto",
                      LeerSiExiste(ruta) != null && LeerSiExiste(ruta).Contains("\"ofrecerVideos\": false"));
        Progreso.OfrecerVideos = true;

        // --- premios ------------------------------------------------------------
        EmpezarConMonedas(100);
        Progreso.EmpezarPartida();
        Progreso.Sumar(50);
        long revision = Progreso.Revision;
        Progreso.CobrarPremio("prueba", 25, true);
        inf.Cerca("anuncios: el premio suma al total", 175, Progreso.Monedas, 1e-9);
        inf.Cerca("anuncios: el premio se ve en la partida", 75, Progreso.MonedasDeLaPartida, 1e-9);
        inf.Verdadero("anuncios: el premio sube la revision", Progreso.Revision > revision);

        Progreso.CobrarPremio("prueba", 0, true);
        Progreso.CobrarPremio("prueba", -10, true);
        inf.Cerca("anuncios: un premio de 0 o negativo no hace nada", 175, Progreso.Monedas, 1e-9);

        Progreso.CobrarPremio("fuera", 25, false);
        inf.Cerca("anuncios: un premio que no es de la partida suma al total", 200, Progreso.Monedas, 1e-9);
        inf.Cerca("anuncios: pero no al contador de la partida", 75, Progreso.MonedasDeLaPartida, 1e-9);

        // --- el x2 de la derrota ------------------------------------------------
        EmpezarConMonedas(0);
        Progreso.EmpezarPartida();
        Progreso.Sumar(40);
        inf.Verdadero("anuncios x2: se cobra", Progreso.DuplicarMonedasDeLaPartida());
        inf.Cerca("anuncios x2: la partida vale el doble", 80, Progreso.MonedasDeLaPartida, 1e-9);
        inf.Cerca("anuncios x2: el total tambien", 80, Progreso.Monedas, 1e-9);
        inf.Verdadero("anuncios x2: queda marcado", Progreso.YaSeDuplicoLaPartida);
        inf.Verdadero("anuncios x2: no se cobra dos veces", !(Progreso.DuplicarMonedasDeLaPartida()));
        inf.Cerca("anuncios x2: y no toco las monedas", 80, Progreso.Monedas, 1e-9);

        Progreso.EmpezarPartida();
        inf.Verdadero("anuncios x2: la partida siguiente vuelve a poder", !(Progreso.YaSeDuplicoLaPartida));
        inf.Verdadero("anuncios x2: pero sin monedas no hay premio", !(Progreso.DuplicarMonedasDeLaPartida()));

        // --- el dia de los topes -------------------------------------------------
        inf.Verdadero("anuncios dia: un dia mayor es nuevo", Progreso.EsDiaNuevo(20260101, 20260102));
        inf.Verdadero("anuncios dia: el mismo dia no", !(Progreso.EsDiaNuevo(20260102, 20260102)));
        inf.Verdadero("anuncios dia: atrasar el reloj no reinicia los topes", !(Progreso.EsDiaNuevo(20260102, 20250101)));
        inf.Verdadero("anuncios dia: sin dia guardado, el primero es nuevo", Progreso.EsDiaNuevo(0, 20260102));
        inf.Verdadero("anuncios dia: DiaDeHoy es aaaammdd", Progreso.DiaDeHoy() >= 20200101 && Progreso.DiaDeHoy() <= 21000101);

        // --- usos del dia --------------------------------------------------------
        EmpezarConMonedas(0);
        inf.Igual("anuncios usos: arranca en 0", 0, Progreso.UsosDeHoy(LugarAnuncio.DuplicarDerrota));
        Progreso.RegistrarUsoDeAnuncio(LugarAnuncio.DuplicarDerrota);
        Progreso.RegistrarUsoDeAnuncio(LugarAnuncio.DuplicarDerrota);
        inf.Igual("anuncios usos: cuenta los del lugar", 2, Progreso.UsosDeHoy(LugarAnuncio.DuplicarDerrota));
        inf.Igual("anuncios usos: otro lugar cuenta aparte", 0, Progreso.UsosDeHoy(LugarAnuncio.Revivir));
        inf.Igual("anuncios usos: las fallas premiadas arrancan en 0", 0, Progreso.FallasPremiadasHoy);
        Progreso.RegistrarFallaPremiada();
        inf.Igual("anuncios usos: se cuenta la falla premiada", 1, Progreso.FallasPremiadasHoy);

        // --- PuedeOfrecer --------------------------------------------------------
        var config = ScriptableObject.CreateInstance<ConfigAnuncios>();
        try
        {
            config.partidasTerminadasMinimas = 2;
            config.segundosJugadosMinimos = 180f;
            config.vecesPorDia = 3;
            config.vecesPorPartida = 1;
            config.segundosEntreAnuncios = 60f;

            inf.Verdadero("puede ofrecer: con todo en regla", ServicioAnuncios.PuedeOfrecerConDatos(config, true, false, 2, 200, 0, 999f, true, 0));
            inf.Verdadero("puede ofrecer: sin config, nunca", !(ServicioAnuncios.PuedeOfrecerConDatos(null, true, false, 9, 9999, 0, 999f, true, 0)));
            inf.Verdadero("puede ofrecer: con el interruptor apagado, nunca", !(ServicioAnuncios.PuedeOfrecerConDatos(config, false, false, 9, 9999, 0, 999f, true, 0)));
            inf.Verdadero("puede ofrecer: no durante otro video", !(ServicioAnuncios.PuedeOfrecerConDatos(config, true, true, 9, 9999, 0, 999f, true, 0)));
            inf.Verdadero("puede ofrecer: no sin video cargado", !(ServicioAnuncios.PuedeOfrecerConDatos(config, true, false, 9, 9999, 0, 999f, false, 0)));
            inf.Verdadero("puede ofrecer: no en la primera partida", !(ServicioAnuncios.PuedeOfrecerConDatos(config, true, false, 1, 9999, 0, 999f, true, 0)));
            inf.Verdadero("puede ofrecer: no con poco jugado", !(ServicioAnuncios.PuedeOfrecerConDatos(config, true, false, 9, 179, 0, 999f, true, 0)));
            inf.Verdadero("puede ofrecer: no con el tope del dia cumplido", !(ServicioAnuncios.PuedeOfrecerConDatos(config, true, false, 9, 9999, 3, 999f, true, 0)));
            inf.Verdadero("puede ofrecer: el ultimo uso del dia todavia se puede", ServicioAnuncios.PuedeOfrecerConDatos(config, true, false, 9, 9999, 2, 999f, true, 0));
            inf.Verdadero("puede ofrecer: no antes de los 60 s del anterior", !(ServicioAnuncios.PuedeOfrecerConDatos(config, true, false, 9, 9999, 0, 59.9f, true, 0)));
            inf.Verdadero("puede ofrecer: a los 60 s justos, si", ServicioAnuncios.PuedeOfrecerConDatos(config, true, false, 9, 9999, 0, 60f, true, 0));

            inf.Verdadero("puede ofrecer: no con un video ya visto en la partida", !(ServicioAnuncios.PuedeOfrecerConDatos(config, true, false, 9, 9999, 0, 999f, true, 1)));

            config.vecesPorPartida = 2;
            inf.Verdadero("puede ofrecer: con el tope por partida en 2, el segundo si", ServicioAnuncios.PuedeOfrecerConDatos(config, true, false, 9, 9999, 0, 999f, true, 1));
            config.vecesPorPartida = 1;

            config.vecesPorDia = 0;
            inf.Verdadero("puede ofrecer: con el tope en 0, nunca", !(ServicioAnuncios.PuedeOfrecerConDatos(config, true, false, 9, 9999, 0, 999f, true, 0)));
        }
        finally
        {
            Object.DestroyImmediate(config);
        }
    }

    // Un proveedor de mentira para las pruebas: devuelve el resultado que se le
    // pide, y si se le pide, avisa dos veces (los SDK de verdad lo hacen).
    class ProveedorDePrueba : IProveedorAnuncios
    {
        public ResultadoAnuncio resultado = ResultadoAnuncio.Recompensado;
        public bool hayVideo = true;
        public bool avisarDosVeces;
        public bool tirarAlMostrar;   // como un SDK que revienta al pedirle el video
        public int veces;

        public string Nombre { get { return "prueba"; } }
        public void Inicializar() { }
        public bool Listo(string lugar) { return hayVideo; }

        public void Mostrar(string lugar, System.Action<ResultadoAnuncio> alTerminar)
        {
            veces++;
            if (tirarAlMostrar) throw new System.InvalidOperationException("prueba: el SDK revento al mostrar");
            if (alTerminar == null) return;
            alTerminar(resultado);
            if (avisarDosVeces) alTerminar(resultado);
        }
    }

    // 13. El circuito del premio: lo que hay del otro lado son monedas, asi que se
    // prueba entero sin entrar en play.
    static void ProbarCircuitoDeAnuncios(Informe inf)
    {
        var proveedor = new ProveedorDePrueba();
        var config = ScriptableObject.CreateInstance<ConfigAnuncios>();
        config.partidasTerminadasMinimas = 0;
        config.segundosJugadosMinimos = 0f;
        config.vecesPorDia = 3;
        config.segundosEntreAnuncios = 60f;
        config.fallasPremiadasPorDia = 1;

        int premios = 0;
        int cierres = 0;
        System.Action alPremiar = () => premios++;
        System.Action alCerrar = () => cierres++;

        try
        {
            string lugar = LugarAnuncio.DuplicarDerrota;

            // --- se vio entero ---------------------------------------------------
            EmpezarConMonedas(0);
            ServicioAnuncios.UsarParaPruebas(proveedor, config);
            premios = cierres = 0;
            bool audioAntes = AudioListener.pause;

            inf.Verdadero("circuito: se puede ofrecer", ServicioAnuncios.PuedeOfrecer(lugar));
            inf.Verdadero("circuito: Mostrar arranca", ServicioAnuncios.Mostrar(lugar, alPremiar, alCerrar));
            inf.Verdadero("circuito: el premio no llega antes de atender los avisos", premios == 0);
            ServicioAnuncios.AtenderAvisos();
            inf.Igual("circuito: el premio llega una vez", 1, premios);
            inf.Igual("circuito: y no se llama al de sin premio", 0, cierres);
            inf.Igual("circuito: gasta un uso del dia", 1, Progreso.UsosDeHoy(lugar));
            inf.Verdadero("circuito: ya no esta mostrando", !ServicioAnuncios.MostrandoAnuncio);
            inf.Verdadero("circuito: el audio vuelve como estaba", AudioListener.pause == audioAntes);

            // El segundo enseguida no: hay que dejar pasar los 60 s.
            inf.Verdadero("circuito: no se ofrece otro enseguida", !ServicioAnuncios.PuedeOfrecer(lugar));
            inf.Verdadero("circuito: y Mostrar tampoco arranca",
                          !ServicioAnuncios.Mostrar(lugar, alPremiar, alCerrar));
            inf.Igual("circuito: sin premio de mas", 1, premios);

            // --- avisa dos veces -------------------------------------------------
            EmpezarConMonedas(0);
            ServicioAnuncios.UsarParaPruebas(proveedor, config);
            proveedor.avisarDosVeces = true;
            premios = cierres = 0;
            ServicioAnuncios.Mostrar(lugar, alPremiar, alCerrar);
            ServicioAnuncios.AtenderAvisos();
            inf.Igual("circuito: dos avisos del SDK, un solo premio", 1, premios);
            inf.Igual("circuito: y un solo uso del dia", 1, Progreso.UsosDeHoy(lugar));
            proveedor.avisarDosVeces = false;

            // --- lo cerro antes --------------------------------------------------
            EmpezarConMonedas(0);
            ServicioAnuncios.UsarParaPruebas(proveedor, config);
            proveedor.resultado = ResultadoAnuncio.Cerrado;
            premios = cierres = 0;
            ServicioAnuncios.Mostrar(lugar, alPremiar, alCerrar);
            ServicioAnuncios.AtenderAvisos();
            inf.Igual("circuito: cerrarlo no premia", 0, premios);
            inf.Igual("circuito: cerrarlo avisa que no hubo premio", 1, cierres);
            inf.Igual("circuito: y no gasta el tope del dia", 0, Progreso.UsosDeHoy(lugar));
            inf.Verdadero("circuito: cerrarlo deja la oferta en pie", ServicioAnuncios.PuedeOfrecer(lugar));

            // --- la diaria no cuenta el tope por partida -------------------------
            EmpezarConMonedas(0);
            ServicioAnuncios.UsarParaPruebas(proveedor, config);
            Progreso.RegistrarUsoDeAnuncio(LugarAnuncio.DuplicarDerrota);
            inf.Verdadero("circuito: un video en la partida corta la derrota", !ServicioAnuncios.PuedeOfrecer(LugarAnuncio.DuplicarDerrota));
            inf.Verdadero("circuito: pero no el x2 de la diaria", ServicioAnuncios.PuedeOfrecer(LugarAnuncio.DuplicarRegalo));

            // --- no habia video --------------------------------------------------
            EmpezarConMonedas(0);
            ServicioAnuncios.UsarParaPruebas(proveedor, config);
            proveedor.resultado = ResultadoAnuncio.NoDisponible;
            premios = cierres = 0;
            ServicioAnuncios.Mostrar(lugar, alPremiar, alCerrar);
            ServicioAnuncios.AtenderAvisos();
            inf.Igual("circuito: sin video no hay premio", 0, premios);
            inf.Igual("circuito: sin video no gasta tope", 0, Progreso.UsosDeHoy(lugar));

            // --- el proveedor revienta al pedirle el video ------------------------
            // Nadie iba a avisar como termino: el juego quedaba mudo, MostrandoAnuncio
            // prendido toda la sesion y el revivir congelado sin botones. Se resuelve como
            // si no hubiera habido video (deja un error en la consola: es el esperado).
            EmpezarConMonedas(0);
            ServicioAnuncios.UsarParaPruebas(proveedor, config);
            proveedor.resultado = ResultadoAnuncio.Recompensado;
            proveedor.tirarAlMostrar = true;
            premios = cierres = 0;
            bool audioAntesDeRomperse = AudioListener.pause;
            bool arranco = ServicioAnuncios.Mostrar(lugar, alPremiar, alCerrar);
            ServicioAnuncios.AtenderAvisos();
            proveedor.tirarAlMostrar = false;
            inf.Verdadero("circuito: si el proveedor revienta, Mostrar no tira la excepcion para arriba", arranco);
            inf.Igual("circuito: y no hay premio", 0, premios);
            inf.Igual("circuito: y se avisa que no hubo premio", 1, cierres);
            inf.Verdadero("circuito: y ya no esta mostrando", !ServicioAnuncios.MostrandoAnuncio);
            inf.Verdadero("circuito: y el audio vuelve como estaba", AudioListener.pause == audioAntesDeRomperse);
            inf.Igual("circuito: y no gasta el tope del dia", 0, Progreso.UsosDeHoy(lugar));

            // --- fallo al mostrarse ----------------------------------------------
            EmpezarConMonedas(0);
            ServicioAnuncios.UsarParaPruebas(proveedor, config);
            proveedor.resultado = ResultadoAnuncio.FallaAlMostrar;
            premios = cierres = 0;
            ServicioAnuncios.Mostrar(lugar, alPremiar, alCerrar);
            ServicioAnuncios.AtenderAvisos();
            inf.Igual("circuito: un video roto se premia igual la primera vez", 1, premios);
            inf.Igual("circuito: y queda anotado", 1, Progreso.FallasPremiadasHoy);

            Progreso.EmpezarPartida();
            ServicioAnuncios.UsarParaPruebas(proveedor, config);
            premios = cierres = 0;
            ServicioAnuncios.Mostrar(lugar, alPremiar, alCerrar);
            ServicioAnuncios.AtenderAvisos();
            inf.Igual("circuito: la segunda falla del dia ya no se premia", 0, premios);
            inf.Igual("circuito: y avisa que no hubo premio", 1, cierres);

            // --- tope del dia ----------------------------------------------------
            EmpezarConMonedas(0);
            proveedor.resultado = ResultadoAnuncio.Recompensado;
            premios = 0;
            for (int i = 0; i < 5; i++)
            {
                Progreso.EmpezarPartida();
                ServicioAnuncios.UsarParaPruebas(proveedor, config);
                ServicioAnuncios.Mostrar(lugar, alPremiar, alCerrar);
                ServicioAnuncios.AtenderAvisos();
            }
            inf.Igual("circuito: el tope del dia corta en 3", 3, premios);
            inf.Igual("circuito: y los usos quedan en 3", 3, Progreso.UsosDeHoy(lugar));

            // --- el tope del dia es global, no uno por lugar ----------------------
            // Se gastan los tres repartidos entre los tres lugares. Contandolos por
            // lugar, "3 videos por dia" eran 3 de revivir mas 3 del x2 de la derrota
            // mas 3 del x2 de la diaria: nueve.
            string[] lugares = { LugarAnuncio.Revivir, LugarAnuncio.DuplicarDerrota,
                                 LugarAnuncio.DuplicarRegalo };
            EmpezarConMonedas(0);
            premios = 0;
            foreach (string uno in lugares)
            {
                Progreso.EmpezarPartida();
                ServicioAnuncios.UsarParaPruebas(proveedor, config);
                ServicioAnuncios.Mostrar(uno, alPremiar, alCerrar);
                ServicioAnuncios.AtenderAvisos();
            }
            inf.Igual("circuito: tres videos repartidos entre los tres lugares", 3, premios);
            inf.Igual("circuito: cada lugar gasto uno solo", 1, Progreso.UsosDeHoy(LugarAnuncio.Revivir));
            inf.Igual("circuito: y el tope los suma a todos", 3, Progreso.UsosDeHoyEnTotal());

            bool algunoOfrece = false;
            foreach (string uno in lugares)
            {
                Progreso.EmpezarPartida();
                ServicioAnuncios.UsarParaPruebas(proveedor, config);
                if (ServicioAnuncios.PuedeOfrecer(uno)) algunoOfrece = true;
            }
            inf.Verdadero("circuito: gastado el tope entre lugares, ninguno ofrece un cuarto",
                          !algunoOfrece);

            // --- un solo video por partida ---------------------------------------
            EmpezarConMonedas(0);
            ServicioAnuncios.UsarParaPruebas(proveedor, config);
            premios = 0;
            ServicioAnuncios.Mostrar(lugar, alPremiar, alCerrar);
            ServicioAnuncios.AtenderAvisos();
            inf.Igual("circuito partida: el primer video se cobra", 1, premios);
            ServicioAnuncios.UsarParaPruebas(proveedor, config);
            inf.Verdadero("circuito partida: el segundo de la misma partida no se ofrece",
                          !ServicioAnuncios.PuedeOfrecer(lugar));
            inf.Verdadero("circuito partida: y no se muestra",
                          !ServicioAnuncios.Mostrar(lugar, alPremiar, alCerrar));
            Progreso.EmpezarPartida();
            ServicioAnuncios.UsarParaPruebas(proveedor, config);
            inf.Verdadero("circuito partida: en la partida siguiente si", ServicioAnuncios.PuedeOfrecer(lugar));

            // --- el interruptor del jugador --------------------------------------
            EmpezarConMonedas(0);
            ServicioAnuncios.UsarParaPruebas(proveedor, config);
            Progreso.OfrecerVideos = false;
            premios = 0;
            inf.Verdadero("circuito: con los videos apagados no se ofrece",
                          !ServicioAnuncios.PuedeOfrecer(lugar));
            inf.Verdadero("circuito: ni se muestra", !ServicioAnuncios.Mostrar(lugar, alPremiar, alCerrar));
            inf.Igual("circuito: y no hay premio", 0, premios);
            Progreso.OfrecerVideos = true;

            // --- el x2 completo, que es lo que ve el jugador ----------------------
            EmpezarConMonedas(1000);
            ServicioAnuncios.UsarParaPruebas(proveedor, config);
            Progreso.EmpezarPartida();
            Progreso.Sumar(120);
            ServicioAnuncios.Mostrar(lugar, () => Progreso.DuplicarMonedasDeLaPartida(), alCerrar);
            ServicioAnuncios.AtenderAvisos();
            inf.Cerca("circuito x2: la partida vale el doble", 240, Progreso.MonedasDeLaPartida, 1e-9);
            inf.Cerca("circuito x2: el total suma el premio", 1240, Progreso.Monedas, 1e-9);
        }
        finally
        {
            ServicioAnuncios.UsarParaPruebas(null, null);
            Object.DestroyImmediate(config);
        }
    }

    // La recompensa diaria: racha, corte, reloj atrasado, monto y cobro.
    // Los contadores de por vida: un JSON de la version 3 los lee en cero, se suman,
    // sobreviven a guardar y releer, y los premios no cuentan como monedas jugadas.
    static void ProbarEstadisticas(Informe inf)
    {
        string ruta = EmpezarCaso("{\"version\":3,\"monedas\":10,\"mejorOleada\":5}", null);
        inf.Igual("estadisticas: un JSON v3 arranca sin muertes", 0L, Progreso.MatadosEnTotal);
        inf.Cerca("estadisticas: y sin monedas jugadas", 0, Progreso.MonedasGanadasJugando, 1e-9);

        Progreso.ContarMuerte("ZombiNormal", false);
        Progreso.ContarMuerte("ZombiNormal", false);
        Progreso.ContarMuerte("ZombiBOSS", true);
        Progreso.ContarMuerte(null, false);
        Progreso.ContarGranada();
        Progreso.ContarFuria();
        Progreso.ContarCritico();
        Progreso.ContarCritico();
        Progreso.Sumar(12.5);
        Progreso.CobrarPremio("prueba", 100, false);

        inf.Igual("estadisticas: muertes por tipo", 2, Progreso.Matados("ZombiNormal"));
        inf.Igual("estadisticas: un tipo sin muertes", 0, Progreso.Matados("ZombiTanque"));
        inf.Igual("estadisticas: muertes en total (el tipo vacio cuenta)", 4L, Progreso.MatadosEnTotal);
        inf.Igual("estadisticas: jefes", 1, Progreso.JefesMatados);
        inf.Igual("estadisticas: granadas", 1, Progreso.GranadasTiradas);
        inf.Igual("estadisticas: furias", 1, Progreso.FuriasActivadas);
        inf.Igual("estadisticas: criticos", 2L, Progreso.Criticos);
        inf.Cerca("estadisticas: el premio no cuenta como jugado", 12.5, Progreso.MonedasGanadasJugando, 1e-9);

        Progreso.UsarCarpetaDePruebas(CarpetaProgreso);
        inf.Igual("estadisticas: se releen las muertes", 2, Progreso.Matados("ZombiNormal"));
        inf.Igual("estadisticas: se releen los jefes", 1, Progreso.JefesMatados);
        inf.Igual("estadisticas: se releen los criticos", 2L, Progreso.Criticos);
        inf.Cerca("estadisticas: se releen las monedas jugadas", 12.5, Progreso.MonedasGanadasJugando, 1e-9);
        inf.Igual("estadisticas: se guarda como v" + Progreso.VersionActual,
                  Progreso.VersionActual, LeerGuardado(ruta) != null ? LeerGuardado(ruta).version : -1);

        EmpezarCaso("{\"version\":4,\"monedas\":1,\"estadisticas\":{\"matados\":[" +
                    "{\"id\":\"ZombiRapido\",\"cantidad\":3},{\"id\":\"ZombiRapido\",\"cantidad\":7}," +
                    "{\"id\":\"\",\"cantidad\":9},{\"id\":\"ZombiTanque\",\"cantidad\":-4}]," +
                    "\"jefesMatados\":-2,\"criticos\":-1,\"monedasGanadasJugando\":-5}}", null);
        inf.Igual("estadisticas: repetido, gana el mayor", 7, Progreso.Matados("ZombiRapido"));
        inf.Igual("estadisticas: negativo queda en cero", 0, Progreso.Matados("ZombiTanque"));
        inf.Igual("estadisticas: sin id se descarta", 7L, Progreso.MatadosEnTotal);
        inf.Igual("estadisticas: jefes negativos en cero", 0, Progreso.JefesMatados);
        inf.Igual("estadisticas: criticos negativos en cero", 0L, Progreso.Criticos);
        inf.Cerca("estadisticas: monedas jugadas negativas en cero", 0, Progreso.MonedasGanadasJugando, 1e-9);
    }

    // Quien es "alguien que recien instala": lo deciden el progreso y las compras.
    static void ProbarPrimeraVez(Informe inf)
    {
        EmpezarCaso("{\"version\":4,\"monedas\":0}", null);
        inf.Verdadero("primera vez: progreso vacio, nunca jugo", PrimeraVez.NuncaJugo);
        inf.Verdadero("primera vez: progreso vacio, no termino partidas", PrimeraVez.NoTerminoPartidas);
        inf.Verdadero("primera vez: progreso vacio, nunca compro", PrimeraVez.NuncaCompro);

        EmpezarCaso("{\"version\":4,\"oleadaEnCurso\":3}", null);
        inf.Verdadero("primera vez: con una oleada a medias ya jugo", !PrimeraVez.NuncaJugo);
        inf.Verdadero("primera vez: pero la guia sigue (no termino partidas)", PrimeraVez.NoTerminoPartidas);

        // La 1 a medias es la unica que puede quedar sin ninguna completada, y retomarla es lo
        // mismo que empezarla: el que abandono su primera oleada sigue yendo derecho a jugar.
        EmpezarCaso("{\"version\":4,\"oleadaEnCurso\":1}", null);
        inf.Verdadero("primera vez: con la oleada 1 a medias sigue siendo la primera vez", PrimeraVez.NuncaJugo);

        EmpezarCaso("{\"version\":4,\"partidasTerminadas\":1}", null);
        inf.Verdadero("primera vez: con una partida terminada ya jugo", !PrimeraVez.NuncaJugo && !PrimeraVez.NoTerminoPartidas);

        EmpezarCaso("{\"version\":2,\"mejorOleada\":2}", null);
        inf.Verdadero("primera vez: un progreso viejo con oleadas ya jugo", !PrimeraVez.NuncaJugo);

        EmpezarCaso("{\"version\":4,\"mejoras\":[{\"id\":\"dano_bala\",\"nivel\":1}]}", null);
        inf.Verdadero("primera vez: con una mejora comprada ya compro", !PrimeraVez.NuncaCompro);
        EmpezarCaso("{\"version\":4,\"mejoras\":[{\"id\":\"dano_bala\",\"nivel\":0}]}", null);
        inf.Verdadero("primera vez: nivel cero no es una compra", PrimeraVez.NuncaCompro);
    }

    // Cuando se pide la reseña de Play: con la oleada 10 y 3 partidas, y despues cada 60 dias.
    static void ProbarPedidoDeResena(Informe inf)
    {
        var hoy = new DateTime(2026, 9, 19);
        inf.Verdadero("resena: oleada 10 y 3 partidas, nunca pedida",
                      PedidoDeResena.Corresponde(10, 3, "", hoy, 10, 3, 60));
        inf.Verdadero("resena: sin llegar a la oleada 10 no",
                      !PedidoDeResena.Corresponde(9, 30, "", hoy, 10, 3, 60));
        inf.Verdadero("resena: con pocas partidas no",
                      !PedidoDeResena.Corresponde(12, 2, "", hoy, 10, 3, 60));
        inf.Verdadero("resena: pedida hace 59 dias no",
                      !PedidoDeResena.Corresponde(12, 5, "2026-07-22", hoy, 10, 3, 60));
        inf.Verdadero("resena: pedida hace 60 dias si",
                      PedidoDeResena.Corresponde(12, 5, "2026-07-21", hoy, 10, 3, 60));
        inf.Verdadero("resena: reloj atrasado no la vuelve a pedir",
                      !PedidoDeResena.Corresponde(12, 5, "2026-12-01", hoy, 10, 3, 60));
        inf.Verdadero("resena: fecha guardada rota, se pide",
                      PedidoDeResena.Corresponde(12, 5, "ayer", hoy, 10, 3, 60));
    }

    // El reloj adelantado a mano: en el mismo arranque del telefono vale la hora de la
    // marca mas el tiempo real; en otro arranque, o sin marca, vale el reloj.
    static void ProbarRelojConfiable(Informe inf)
    {
        var marca = new DateTime(2026, 9, 19, 12, 0, 0, DateTimeKind.Utc);
        long ms = 5000000, diez = 10 * 60 * 1000;
        Func<DateTime, long, int, DateTime> reloj = (ahora, ahoraMs, arranques) =>
            RelojConfiable.Confiable(ahora, ahoraMs, arranques, marca.Ticks, ms, 7);

        inf.Verdadero("reloj: sin marca vale el reloj",
                      RelojConfiable.Confiable(marca.AddDays(1), ms, 7, 0, 0, 0) == marca.AddDays(1));
        inf.Verdadero("reloj: adelantado un dia a los 10 min, vale el tiempo real",
                      reloj(marca.AddDays(1), ms + diez, 7) == marca.AddMilliseconds(diez));
        inf.Verdadero("reloj: sin tocar, vale el reloj",
                      reloj(marca.AddMinutes(10), ms + diez, 7) == marca.AddMinutes(10));
        inf.Verdadero("reloj: una hora de mas se tolera (hora de verano)",
                      reloj(marca.AddMinutes(70), ms + diez, 7) == marca.AddMinutes(70));
        inf.Verdadero("reloj: al dia siguiente de verdad, vale el reloj",
                      reloj(marca.AddHours(20), ms + 20L * 3600 * 1000, 7) == marca.AddHours(20));
        inf.Verdadero("reloj: otro arranque, vale el reloj",
                      reloj(marca.AddDays(1), 1000, 8) == marca.AddDays(1));
        inf.Verdadero("reloj: contador que va para atras, vale el reloj",
                      reloj(marca.AddDays(1), ms - 1, 7) == marca.AddDays(1));
        inf.Verdadero("reloj: atrasado, vale el reloj (lo frena EsDiaNuevo)",
                      reloj(marca.AddDays(-3), ms + diez, 7) == marca.AddDays(-3));
    }

    // La escalera de las monedas y los hitos del combo.
    static void ProbarJugoSonoro(Informe inf)
    {
        inf.Igual("escalera: la primera moneda es la bemol", 0, Moneda.GradoSiguiente(-1, 0f, 0.45f));
        inf.Igual("escalera: seguida sube un grado", 4, Moneda.GradoSiguiente(3, 0.2f, 0.45f));
        inf.Igual("escalera: si se corta vuelve a empezar", 0, Moneda.GradoSiguiente(5, 0.6f, 0.45f));
        inf.Igual("escalera: arriba de todo sigue por la octava de arriba", 7, Moneda.GradoSiguiente(14, 0.1f, 0.45f));
        inf.Igual("escalera: el grado 7 es la octava", 12, Moneda.SemitonosDelGrado(7));
        inf.Igual("escalera: el ultimo, dos octavas", 24, Moneda.SemitonosDelGrado(14));

        int[] hitos = { 10, 25, 50, 100 };
        inf.Igual("combo: de 9 a 10 cruza el 10", 10, ContadorCombo.HitoCruzado(hitos, 9, 10));
        inf.Igual("combo: de 10 a 11 no cruza nada", 0, ContadorCombo.HitoCruzado(hitos, 10, 11));
        inf.Igual("combo: una granada de 8 a 27 cruza el 25", 25, ContadorCombo.HitoCruzado(hitos, 8, 27));
        inf.Igual("combo: sin hitos no cruza nada", 0, ContadorCombo.HitoCruzado(null, 0, 99));
        inf.Igual("combo: el primer salto es la bemol", 0, ContadorCombo.SemitonosDelSalto(0));
        inf.Igual("combo: los saltos de mas se quedan arriba", 24, ContadorCombo.SemitonosDelSalto(40));
    }

    // Las misiones del dia: como se arman, como avanzan con los contadores y como se cobran.
    static void ProbarMisiones(Informe inf)
    {
        var a = MisionesDiarias.Armar(20260919, 10, false, false, false);
        var b = MisionesDiarias.Armar(20260919, 10, false, false, false);
        inf.Igual("misiones: son tres", 3, a.Count);
        bool iguales = true, distintas = true;
        for (int i = 0; i < a.Count; i++)
        {
            if (a[i].tipo != b[i].tipo || a[i].objetivo != b[i].objetivo) iguales = false;
            for (int j = 0; j < i; j++) if (a[i].tipo == a[j].tipo) distintas = false;
        }
        inf.Verdadero("misiones: el mismo dia salen las mismas", iguales);
        inf.Verdadero("misiones: tres tipos distintos", distintas);

        bool sinCompras = true, jefeBien = true, hayFuria = false, objetivosBien = true;
        for (int dia = 20260101; dia < 20260131; dia++)
        {
            foreach (var m in MisionesDiarias.Armar(dia, 12, false, false, false))
            {
                if (m.tipo == MisionesDiarias.Furia || m.tipo == MisionesDiarias.Granadas || m.tipo == MisionesDiarias.Criticos) sinCompras = false;
                if (m.tipo == MisionesDiarias.Jefe && m.dificultad != 2) jefeBien = false;
                if (!(m.objetivo > 0)) objetivosBien = false;
            }
            foreach (var m in MisionesDiarias.Armar(dia, 5, true, true, true))
            {
                if (m.tipo == MisionesDiarias.Furia) hayFuria = true;
                if (m.tipo == MisionesDiarias.Jefe) jefeBien = false;
            }
        }
        inf.Verdadero("misiones: sin compras no piden furia, granada ni criticos", sinCompras);
        inf.Verdadero("misiones: el jefe solo en la dificil y con la oleada 9", jefeBien);
        inf.Verdadero("misiones: con la furia comprada alguna la pide", hayFuria);
        inf.Verdadero("misiones: todos los objetivos son positivos", objetivosBien);
        inf.Cerca("economia: numeros redondos", 1250, Economia.Redondo(1234), 1e-9);
        // Con la suma de zombis corregida (Economia.ZombisPorPartida, 23/9): eran 15 y 1300.
        inf.Cerca("misiones: premio facil sin oleadas", 20, MisionesDiarias.Monto(0, 0), 1e-9);
        inf.Cerca("misiones: premio dificil con oleada 10", 1400, MisionesDiarias.Monto(2, 10), 1e-9);
        ProbarPremiosDeMisiones(inf);

        // Con el progreso: una de matar 3, se matan 3, se cobra una sola vez y se guarda.
        EmpezarCaso("{\"version\":4,\"monedas\":0}", null);
        MisionesDiarias.Asegurar();
        inf.Igual("misiones: un progreso nuevo arma tres", 3, MisionesDiarias.DeHoy.Count);
        var estado = Progreso.Misiones;
        estado.lista = new List<MisionDelDia>
        {
            new MisionDelDia { tipo = MisionesDiarias.Matar, dificultad = 0, objetivo = 3, inicio = Progreso.MatadosEnTotal },
            new MisionDelDia { tipo = MisionesDiarias.Oleada, dificultad = 1, objetivo = 5 },
            new MisionDelDia { tipo = MisionesDiarias.Jefe, dificultad = 2, objetivo = 1, inicio = Progreso.JefesMatados },
        };
        inf.Igual("misiones: sin avance no hay nada para cobrar", 0, MisionesDiarias.PorCobrar);
        inf.Cerca("misiones: sin cumplir no paga", 0, MisionesDiarias.Cobrar(0), 1e-9);
        for (int i = 0; i < 3; i++) Progreso.ContarMuerte("ZombiNormal", false);
        // La de oleadas cuenta cuantas se completaron hoy, no a cual se llego: cuatro
        // oleadas no cumplen "completa 5", aunque la cuarta sea la numero 25.
        for (int i = 0; i < 4; i++) MisionesDiarias.RegistrarOleada(25);
        inf.Igual("misiones: matar 3 cumplida, la oleada no", 1, MisionesDiarias.PorCobrar);
        MisionesDiarias.RegistrarOleada(26);
        inf.Igual("misiones: con las cinco oleadas la cumple", 2, MisionesDiarias.PorCobrar);
        // Lo que paga es lo que muestra la ventana para esa dificultad y esa mejor oleada.
        double esperado = MisionesDiarias.Monto(0, Progreso.MejorOleada);
        inf.Cerca("misiones: cobrar paga lo que dice la ventana", esperado, MisionesDiarias.Cobrar(0), 1e-9);
        inf.Cerca("misiones: no paga dos veces", 0, MisionesDiarias.Cobrar(0), 1e-9);
        inf.Cerca("misiones: las monedas llegaron", esperado, Progreso.Monedas, 1e-9);
        inf.Cerca("misiones: no cuentan como jugadas", 0, Progreso.MonedasGanadasJugando, 1e-9);

        // El cofre: con las tres cobradas, una sola vez.
        inf.Verdadero("cofre: con una cobrada no se abre", !MisionesDiarias.CofreDisponible && MisionesDiarias.Cobradas == 1);
        inf.Cerca("cofre: cerrado no paga", 0, MisionesDiarias.CobrarCofre(), 1e-9);
        MisionesDiarias.Cobrar(1);
        Progreso.ContarMuerte("ZombiBOSS", true);
        MisionesDiarias.Cobrar(2);
        inf.Verdadero("cofre: con las tres cobradas se abre", MisionesDiarias.CofreDisponible);
        inf.Igual("cofre: cuenta en la insignia", 1, MisionesDiarias.PorCobrar);
        double esperadoCofre = MisionesDiarias.MontoCofre(Progreso.MejorOleada);
        inf.Cerca("cofre: paga lo que dice la ventana", esperadoCofre, MisionesDiarias.CobrarCofre(), 1e-9);
        inf.Cerca("cofre: no paga dos veces", 0, MisionesDiarias.CobrarCofre(), 1e-9);
        inf.Verdadero("cofre: queda cobrado", MisionesDiarias.CofreCobrado && MisionesDiarias.PorCobrar == 0);
        // Era 940 hasta que la suma de zombis se corrigio (23/9).
        inf.Cerca("cofre: con oleada 10 paga mas", 1000, MisionesDiarias.MontoCofre(10), 1e-9);

        int diaGuardado = estado.dia;
        Progreso.UsarCarpetaDePruebas(CarpetaProgreso);
        inf.Verdadero("misiones: se relee la cobrada", MisionesDiarias.DeHoy.Count == 3 && MisionesDiarias.DeHoy[0].cobrada);
        inf.Igual("misiones: se releen las oleadas del dia", 5, Progreso.Misiones.oleadasDelDia);

        // Un reloj atrasado no las cambia: el dia guardado es mayor que hoy.
        Progreso.Misiones.dia = diaGuardado + 1;
        MisionesDiarias.Asegurar();
        inf.Verdadero("misiones: con el reloj atrasado siguen las mismas", MisionesDiarias.DeHoy[0].cobrada);

        // El premio no cambia por mejorar la marca durante el dia: si no, guardar las
        // misiones sin cobrar hasta llegar mas lejos era la jugada optima.
        EmpezarCaso("{\"version\":4,\"monedas\":0}", null);
        MisionesDiarias.Asegurar();
        double premioAlArmar = MisionesDiarias.Monto(2, MisionesDiarias.OleadaDeHoy);
        Progreso.RegistrarOleadaCompletada(30);
        inf.Igual("misiones: la marca del dia no se mueve al mejorar la mejor oleada",
                  0, MisionesDiarias.OleadaDeHoy);
        inf.Cerca("misiones: el premio tampoco", premioAlArmar,
                  MisionesDiarias.Monto(2, MisionesDiarias.OleadaDeHoy), 1e-9);

        // A medianoche no se pierde lo que quedo cumplido sin cobrar: se cobra solo antes
        // de armar las del dia nuevo, cofre incluido.
        EmpezarCaso("{\"version\":4,\"monedas\":0}", null);
        MisionesDiarias.Asegurar();
        var ayer = Progreso.Misiones;
        ayer.dia = Progreso.DiaDeHoy() - 1;
        ayer.cofreCobrado = false;
        ayer.lista = new List<MisionDelDia>
        {
            new MisionDelDia { tipo = MisionesDiarias.Matar, dificultad = 0, objetivo = 1, inicio = Progreso.MatadosEnTotal },
            new MisionDelDia { tipo = MisionesDiarias.Matar, dificultad = 1, objetivo = 1, inicio = Progreso.MatadosEnTotal },
            new MisionDelDia { tipo = MisionesDiarias.Matar, dificultad = 2, objetivo = 1, inicio = Progreso.MatadosEnTotal },
        };
        Progreso.ContarMuerte("ZombiNormal", false);
        double esperadoDeAyer = MisionesDiarias.MontoCofre(Progreso.MejorOleada);
        for (int d = 0; d < MisionesDiarias.Cantidad; d++) esperadoDeAyer += MisionesDiarias.Monto(d, Progreso.MejorOleada);
        double antesDeMedianoche = Progreso.Monedas;
        MisionesDiarias.Asegurar();
        inf.Cerca("misiones: a medianoche se cobra lo cumplido y el cofre",
                  esperadoDeAyer, Progreso.Monedas - antesDeMedianoche, 1e-9);
        inf.Verdadero("misiones: y despues hay tres nuevas sin cobrar",
                      MisionesDiarias.DeHoy.Count == 3 && MisionesDiarias.Cobradas == 0 && !MisionesDiarias.CofreCobrado);

        // Un progreso de antes de la marca (sin el campo, que vale -1) que se abre con el dia
        // ya vencido: lo cumplido sin cobrar se paga con la mejor oleada, no con la 0. Hasta el
        // 23/9 la marca se completaba solo si seguia siendo el mismo dia, que se mira despues
        // del cierre: una dificil de la oleada 45 pagaba unas 160 monedas en vez de ~77.600.
        EmpezarCaso("{\"version\":4,\"monedas\":0}", null);
        Progreso.RegistrarOleadaCompletada(45);
        MisionesDiarias.Asegurar();
        var sinMarca = Progreso.Misiones;
        sinMarca.dia = Progreso.DiaDeHoy() - 1;
        sinMarca.mejorOleadaAlArmar = -1;
        sinMarca.cofreCobrado = true;
        sinMarca.lista = new List<MisionDelDia>
        {
            new MisionDelDia { tipo = MisionesDiarias.Matar, dificultad = 0, objetivo = 1, cobrada = true },
            new MisionDelDia { tipo = MisionesDiarias.Matar, dificultad = 1, objetivo = 1, cobrada = true },
            new MisionDelDia { tipo = MisionesDiarias.Matar, dificultad = 2, objetivo = 1, inicio = Progreso.MatadosEnTotal },
        };
        Progreso.ContarMuerte("ZombiNormal", false);
        double antesDelCierre = Progreso.Monedas;
        MisionesDiarias.Asegurar();
        inf.Cerca("misiones: sin la marca, a medianoche paga con la mejor oleada y no con la 0",
                  MisionesDiarias.Monto(2, 45), Progreso.Monedas - antesDelCierre, 1e-9);
    }

    // Los premios de las misiones salen de lo que cuesta cumplirlas, no de un monto fijo:
    // con los fijos, el primer dia regalaban ~1.850 monedas contra ~300 de jugar (seis
    // veces lo que costaba) y en las oleadas altas no se notaban. Se mide en toda la curva.
    static void ProbarPremiosDeMisiones(Informe inf)
    {
        double cuestan = 0;
        for (int d = 0; d < MisionesDiarias.Cantidad; d++) cuestan += MisionesDiarias.Partidas(d);

        bool proporcion = true, ordenado = true, crece = true, vale = true;
        double anterior = 0;
        foreach (int m in new[] { 0, 3, 5, 10, 20, 40 })
        {
            double total = MisionesDiarias.MontoCofre(m);
            for (int d = 0; d < MisionesDiarias.Cantidad; d++) total += MisionesDiarias.Monto(d, m);

            // Lo que dan las partidas que cuesta el dia entero. El premio es un extra de
            // eso: si lo pasara, convendria cobrar misiones antes que jugar.
            double jugando = cuestan * Economia.MonedasPorPartida(m);
            double parte = total / jugando;
            if (parte > 1.0) proporcion = false;
            // Y tiene que valer la pena: menos de un tercio no mueve a nadie.
            if (parte < 0.33) vale = false;
            if (!(MisionesDiarias.Monto(0, m) < MisionesDiarias.Monto(1, m)
                  && MisionesDiarias.Monto(1, m) < MisionesDiarias.Monto(2, m))) ordenado = false;
            if (m > 3 && total <= anterior) crece = false;
            anterior = total;
        }

        inf.Verdadero("misiones: el premio del dia no pasa lo que da jugarlo", proporcion);
        inf.Verdadero("misiones: el premio del dia es al menos un tercio de eso", vale);
        inf.Verdadero("misiones: la facil paga menos que la media y esta menos que la dificil", ordenado);
        inf.Verdadero("misiones: el premio crece con la mejor oleada", crece);
        // Ningun objetivo puede quedarse fijo mientras el premio crece con la oleada: con
        // 25 granadas fijas se cobraba en la oleada 45 el premio de la oleada 45 tirandolas
        // paradas en un rincon, mas de lo que da una partida entera.
        bool escalan = true;
        string elQueNoEscala = "";
        string[] tipos = { MisionesDiarias.Matar, MisionesDiarias.Oleada, MisionesDiarias.Monedas,
                           MisionesDiarias.Furia, MisionesDiarias.Granadas, MisionesDiarias.Criticos,
                           MisionesDiarias.Jefe };
        foreach (string tipo in tipos)
        {
            for (int d = 0; d < MisionesDiarias.Cantidad; d++)
            {
                if (MisionesDiarias.Objetivo(tipo, d, 40) > MisionesDiarias.Objetivo(tipo, d, 5)) continue;
                escalan = false;
                elQueNoEscala += tipo + "/" + d + " ";
            }
        }
        inf.Verdadero("misiones: todos los objetivos crecen con la oleada" +
                      (escalan ? "" : " (" + elQueNoEscala.Trim() + ")"), escalan);

        // Que crezca no alcanza: lo que importa es cuanto CUESTA cumplir cada objetivo
        // contra las partidas que se le cobran al premio. Sin esta cuenta, el objetivo de
        // granadas costaba 0,43 partidas con el premio de 2,5 (tirarlas al aire rendia mas
        // monedas por segundo que jugar bien desde la oleada ~10) y el de criticos, 0,26.
        // Los dos pasaban la prueba de arriba, porque crecer con la oleada crecian.
        //
        // El costo se estima en partidas para cada tipo: los que se cumplen matando salen
        // de los zombis de la partida, los que se cumplen con el reloj (la furia cada
        // 120 s, la granada cada 5) de lo que dura, y los criticos de las balas que se
        // disparan por la probabilidad que tenga comprada el jugador.
        double probCritico = Math.Max(0.05, CatalogoMejoras.ProbabilidadCritico);
        bool valenLoQuePagan = true;
        string elBarato = "";
        foreach (int m in new[] { 0, 3, 5, 10, 20, 40 })
        {
            int mm = Math.Max(3, m);
            for (int d = 0; d < MisionesDiarias.Cantidad; d++)
            {
                foreach (string tipo in tipos)
                {
                    double objetivo = MisionesDiarias.Objetivo(tipo, d, m);
                    double enPartidas;
                    switch (tipo)
                    {
                        case MisionesDiarias.Matar: enPartidas = objetivo / Economia.ZombisPorPartida(mm); break;
                        case MisionesDiarias.Monedas: enPartidas = objetivo / Economia.MonedasPorPartida(m); break;
                        case MisionesDiarias.Oleada: enPartidas = objetivo / mm; break;
                        case MisionesDiarias.Furia: enPartidas = objetivo * 120.0 / Economia.SegundosPorPartida(mm); break;
                        case MisionesDiarias.Granadas: enPartidas = objetivo * 5.0 / Economia.SegundosPorPartida(mm); break;
                        case MisionesDiarias.Criticos: enPartidas = objetivo / (Economia.BalasPorPartida(mm) * probCritico); break;
                        case MisionesDiarias.Jefe: enPartidas = objetivo / (mm / 10.0); break;
                        default: continue;
                    }
                    // El 0,75 es por los pisos y el redondeo, que mueven los objetivos
                    // chicos: el peor de los siete queda en 0,80 (el jefe en la oleada 5,
                    // donde pedir "un jefe" ya son 2 partidas contra 2,5 que se pagan).
                    // Pasarse para arriba no es un exploit sino una mision dura, asi que
                    // no se mira.
                    if (enPartidas >= MisionesDiarias.Partidas(d) * 0.75) continue;
                    valenLoQuePagan = false;
                    elBarato += tipo + "/" + d + "@" + m + " ";
                }
            }
        }
        inf.Verdadero("misiones: cumplir cada objetivo cuesta las partidas que se le pagan" +
                      (valenLoQuePagan ? "" : " (" + elBarato.Trim() + ")"), valenLoQuePagan);

        // Lo que se lee tiene que decir lo que se pide. La del jefe era un texto fijo, "Derrota
        // a un jefe", y pedia 2 o mas: la fila mostraba ese texto con "0 / 2" al lado, y la
        // derrota "PROXIMO: Derrota a un jefe 1/2". Con uno solo el texto puede ir sin numero.
        bool dicenElObjetivo = true;
        string elQueNoLoDice = "";
        foreach (int m in new[] { 9, 20, 40 })
        {
            for (int d = 0; d < MisionesDiarias.Cantidad; d++)
            {
                foreach (string tipo in tipos)
                {
                    var mision = new MisionDelDia { tipo = tipo, dificultad = d, objetivo = MisionesDiarias.Objetivo(tipo, d, m) };
                    if (mision.objetivo <= 1) continue;
                    if (MisionesDiarias.Descripcion(mision).Contains(FormatoNumeros.Compacto(mision.objetivo))) continue;
                    dicenElObjetivo = false;
                    elQueNoLoDice += tipo + "/" + d + "@" + m + " ";
                }
            }
        }
        inf.Verdadero("misiones: el texto de cada una dice cuanto pide" +
                      (dicenElObjetivo ? "" : " (" + elQueNoLoDice.Trim() + ")"), dicenElObjetivo);

        inf.Verdadero("misiones: el primer dia no regala (menos de 500 en total)",
                      MisionesDiarias.Monto(0, 0) + MisionesDiarias.Monto(1, 0)
                      + MisionesDiarias.Monto(2, 0) + MisionesDiarias.MontoCofre(0) < 500);
    }

    // El desafio de la semana: la semana que le toca, el objetivo, el premio y el cobro.
    static void ProbarDesafioSemanal(Informe inf)
    {
        // El lunes de la semana de cada dia (el 21/9/2026 es lunes).
        inf.Igual("semanal: el lunes es su propio lunes", 20260921, DesafioSemanal.LunesDe(20260921));
        inf.Igual("semanal: el miercoles cae en ese lunes", 20260921, DesafioSemanal.LunesDe(20260923));
        inf.Igual("semanal: el domingo tambien", 20260921, DesafioSemanal.LunesDe(20260927));
        inf.Igual("semanal: el lunes siguiente ya es otro", 20260928, DesafioSemanal.LunesDe(20260928));
        inf.Igual("semanal: cruza el fin de mes", 20260928, DesafioSemanal.LunesDe(20261002));
        inf.Igual("semanal: una fecha rota da 0", 0, DesafioSemanal.LunesDe(20260899));

        // El mismo lunes da siempre el mismo desafio, y sin la oleada 9 no pide jefes.
        bool estable = true, sinJefes = true;
        foreach (int lunes in new[] { 20260921, 20260928, 20261005, 20261012 })
        {
            if (DesafioSemanal.Elegir(lunes, 20) != DesafioSemanal.Elegir(lunes, 20)) estable = false;
            if (DesafioSemanal.Elegir(lunes, 3) == DesafioSemanal.Jefes) sinJefes = false;
        }
        inf.Verdadero("semanal: el mismo lunes da el mismo desafio", estable);
        inf.Verdadero("semanal: sin la oleada 9 no pide jefes", sinJefes);

        // El objetivo crece con la oleada y el premio no se pasa de lo que cuesta.
        bool crecen = true, proporcion = true, valeMasQueUnaDiaria = true;
        foreach (int m in new[] { 0, 5, 11, 20, 40 })
        {
            foreach (string tipo in new[] { DesafioSemanal.Matar, DesafioSemanal.Oleadas,
                                            DesafioSemanal.Monedas, DesafioSemanal.Jefes })
            {
                if (m > 5 && DesafioSemanal.Objetivo(tipo, m) <= DesafioSemanal.Objetivo(tipo, 5)) crecen = false;
            }
            double cuestan = DesafioSemanal.PartidasQueCuesta * Economia.MonedasPorPartida(m);
            if (DesafioSemanal.Monto(m) > cuestan) proporcion = false;
            // Tiene que pagar mas que la mision dificil del dia: dura toda la semana.
            if (DesafioSemanal.Monto(m) <= MisionesDiarias.Monto(2, m)) valeMasQueUnaDiaria = false;
        }
        inf.Verdadero("semanal: los objetivos crecen con la oleada", crecen);
        inf.Verdadero("semanal: el premio no pasa lo que cuesta", proporcion);
        inf.Verdadero("semanal: paga mas que la mision dificil del dia", valeMasQueUnaDiaria);

        // Con el progreso: se arma, se avanza, se cobra una sola vez.
        EmpezarCaso("{\"version\":" + Progreso.VersionActual + ",\"monedas\":0}", null);
        DesafioSemanal.Asegurar();
        var estado = Progreso.Semanal;
        inf.Verdadero("semanal: un progreso nuevo arma uno", estado.lunes > 0 && estado.objetivo > 0);
        inf.Verdadero("semanal: arranca sin cobrar y sin avance",
                      !estado.cobrado && DesafioSemanal.Avance < 0.001);

        // Se fuerza uno de matar tres zombis para probar el circuito.
        estado.tipo = DesafioSemanal.Matar;
        estado.objetivo = 3;
        estado.inicio = Progreso.MatadosEnTotal;
        inf.Cerca("semanal: sin cumplir no paga", 0, DesafioSemanal.Cobrar(), 1e-9);
        for (int i = 0; i < 3; i++) Progreso.ContarMuerte("ZombiNormal", false);
        inf.Verdadero("semanal: con las tres muertes queda cumplido", DesafioSemanal.Cumplido);
        inf.Verdadero("semanal: y cuenta para la insignia", DesafioSemanal.PorCobrar);
        double esperado = DesafioSemanal.MontoDeEstaSemana;
        inf.Cerca("semanal: cobra lo que dice la ventana", esperado, DesafioSemanal.Cobrar(), 1e-9);
        inf.Cerca("semanal: no paga dos veces", 0, DesafioSemanal.Cobrar(), 1e-9);
        inf.Cerca("semanal: las monedas llegaron", esperado, Progreso.Monedas, 1e-9);
        inf.Cerca("semanal: no cuenta como jugado", 0, Progreso.MonedasGanadasJugando, 1e-9);

        // Un reloj atrasado no lo cambia: el lunes guardado es mayor que el de hoy.
        int lunesGuardado = estado.lunes;
        estado.lunes = lunesGuardado + 7;
        DesafioSemanal.Asegurar();
        inf.Verdadero("semanal: con el reloj atrasado sigue el mismo", Progreso.Semanal.cobrado);

        // Al cambiar de semana, lo cumplido sin cobrar se cobra solo.
        estado.lunes = lunesGuardado - 7;
        estado.tipo = DesafioSemanal.Matar;
        estado.objetivo = 1;
        estado.inicio = 0;
        estado.cobrado = false;
        estado.mejorOleadaAlArmar = 0;
        double antes = Progreso.Monedas;
        double esperadoDeLaSemanaVieja = DesafioSemanal.Monto(0);
        DesafioSemanal.Asegurar();
        inf.Cerca("semanal: al cambiar de semana se cobra lo cumplido",
                  esperadoDeLaSemanaVieja, Progreso.Monedas - antes, 1e-9);
        inf.Verdadero("semanal: y queda uno nuevo sin cobrar",
                      Progreso.Semanal.lunes == lunesGuardado && !Progreso.Semanal.cobrado);
    }

    // El proximo objetivo de la derrota: gana el de mas avance, y lo que ya alcanza no cuenta.
    static void ProbarProximoObjetivo(Informe inf)
    {
        EmpezarCaso("{\"version\":4,\"monedas\":20}", null);
        MisionesDiarias.Asegurar();
        var estado = Progreso.Misiones;
        estado.lista = new List<MisionDelDia>
        {
            new MisionDelDia { tipo = MisionesDiarias.Matar, dificultad = 0, objetivo = 10, inicio = Progreso.MatadosEnTotal - 9 },
            new MisionDelDia { tipo = MisionesDiarias.Oleada, dificultad = 1, objetivo = 5 },
            new MisionDelDia { tipo = MisionesDiarias.Jefe, dificultad = 2, objetivo = 1, inicio = Progreso.JefesMatados },
        };
        string texto;
        float fraccion;
        inf.Verdadero("objetivo: hay uno", ProximoObjetivo.Elegir(out texto, out fraccion));
        inf.Cerca("objetivo: la mision al 90 % le gana a la mejora", 0.9, fraccion, 1e-4);
        inf.Verdadero("objetivo: dice la mision", texto != null && texto.Contains("10"));

        estado.lista[0].inicio = Progreso.MatadosEnTotal;   // la mision vuelve a 0 de 10
        inf.Verdadero("objetivo: sin misiones cerca, una mejora", ProximoObjetivo.Elegir(out texto, out fraccion));
        inf.Verdadero("objetivo: la mejora mas cerca, sin llegar", fraccion > 0f && fraccion < 1f);

        EmpezarCaso("{\"version\":4,\"monedas\":100000}", null);
        MisionesDiarias.Asegurar();
        Progreso.Misiones.lista.Clear();
        ProximoObjetivo.Elegir(out texto, out fraccion);
        inf.Verdadero("objetivo: con todo al alcance no elige una mejora que ya alcanza", fraccion < 1f);

        // Con una sola moneda de falta va en singular: "TE FALTAN 1 MONEDAS" no. Con las
        // monedas una por debajo del precio de una mejora, gana la mas cerca de las que no
        // alcanzan y le falta justo una: se prueba con cada una de la tienda, asi pasan las
        // que suben de nivel y las que se desbloquean (la granada y la furia).
        var catalogo = CatalogoMejoras.Instancia;
        if (catalogo != null && catalogo.enTienda != null)
        {
            string enPlural = "";
            double masBarata = double.MaxValue;
            foreach (var mejora in catalogo.enTienda)
            {
                if (mejora == null) continue;
                masBarata = Math.Min(masBarata, mejora.Precio(0));
                EmpezarConMonedas(mejora.Precio(0) - 1);
                ProximoObjetivo.Elegir(out texto, out fraccion);
                if (texto == null || !texto.StartsWith("TE FALTA 1 MONEDA PARA ")) enPlural += mejora.id + " ";
            }
            inf.Verdadero("objetivo: con una moneda de falta, en singular" + (enPlural.Length == 0 ? "" : " (" + enPlural.Trim() + ")"),
                          enPlural.Length == 0);
            EmpezarConMonedas(masBarata - 2);
            ProximoObjetivo.Elegir(out texto, out fraccion);
            inf.Verdadero("objetivo: con dos de falta, en plural", texto != null && texto.StartsWith("TE FALTAN 2 MONEDAS PARA "));
        }

        // Lo mismo con la experiencia, sin monedas para que gane el nivel.
        EmpezarConMonedas(0);
        NivelJugador.Sumar(NivelJugador.CostoDelNivel(1) - 1);
        ProximoObjetivo.Elegir(out texto, out fraccion);
        inf.Igual("objetivo: con un punto de experiencia de falta, en singular", "TE FALTA 1 XP PARA EL NIVEL 2", texto);
        EmpezarConMonedas(0);
        NivelJugador.Sumar(NivelJugador.CostoDelNivel(1) - 2);
        ProximoObjetivo.Elegir(out texto, out fraccion);
        inf.Igual("objetivo: con dos puntos de falta, en plural", "TE FALTAN 2 XP PARA EL NIVEL 2", texto);
    }

    // El bestiario: las estrellas por muertes de cada tipo, que se cobran una vez cada una.
    static void ProbarBestiario(Informe inf)
    {
        inf.Igual("bestiario: 99 normales, ninguna estrella", 0, Bestiario.AlcanzadasCon(Bestiario.Normal, 99));
        inf.Igual("bestiario: 100 normales, una", 1, Bestiario.AlcanzadasCon(Bestiario.Normal, 100));
        inf.Igual("bestiario: 10.000, las tres", 3, Bestiario.AlcanzadasCon(Bestiario.Tanque, 10000));
        inf.Igual("bestiario: un jefe ya es una estrella", 1, Bestiario.AlcanzadasCon(Bestiario.Jefe, 1));
        inf.Igual("bestiario: 50 jefes, las tres", 3, Bestiario.AlcanzadasCon(Bestiario.Jefe, 50));
        inf.Cerca("bestiario: premio de la primera del caminante sin oleadas", 110, Bestiario.Premio(Bestiario.Normal, 0, 0), 1e-9);
        inf.Cerca("bestiario: premio de la tercera del caminante con oleada 10", 14700, Bestiario.Premio(Bestiario.Normal, 2, 10), 1e-9);
        ProbarPremiosDelBestiario(inf);

        EmpezarCaso("{\"version\":4,\"monedas\":0,\"estadisticas\":{\"matados\":[{\"id\":\"ZombiNormal\",\"cantidad\":1500},{\"id\":\"ZombiBOSS\",\"cantidad\":1}]}}", null);
        inf.Igual("bestiario: dos normales y un jefe para cobrar", 3, Bestiario.PorCobrar);
        inf.Igual("bestiario: el siguiente de los normales es 10.000", 10000, Bestiario.Siguiente(Bestiario.Normal));
        // Lo que paga es lo que muestra la tarjeta para ese tipo y esa estrella.
        double primera = Bestiario.Premio(Bestiario.Normal, 0, Progreso.MejorOleada);
        double segunda = Bestiario.Premio(Bestiario.Normal, 1, Progreso.MejorOleada);
        inf.Cerca("bestiario: cobra la primera", primera, Bestiario.Cobrar(Bestiario.Normal), 1e-9);
        inf.Cerca("bestiario: despues la segunda", segunda, Bestiario.Cobrar(Bestiario.Normal), 1e-9);
        inf.Cerca("bestiario: la tercera no esta ganada", 0, Bestiario.Cobrar(Bestiario.Normal), 1e-9);
        inf.Cerca("bestiario: sin muertes no paga", 0, Bestiario.Cobrar(Bestiario.Tanque), 1e-9);
        inf.Cerca("bestiario: las monedas llegaron", primera + segunda, Progreso.Monedas, 1e-9);
        inf.Cerca("bestiario: no cuentan como jugadas", 0, Progreso.MonedasGanadasJugando, 1e-9);
        Progreso.UsarCarpetaDePruebas(CarpetaProgreso);
        inf.Igual("bestiario: se releen las cobradas", 2, Bestiario.Cobradas(Bestiario.Normal));
        inf.Igual("bestiario: queda el jefe para cobrar", 1, Bestiario.PorCobrar);
    }

    // Los premios del bestiario salen de lo que dejan las muertes del escalon, no de un
    // monto fijo: con los fijos, 100 caminantes pagaban lo mismo que 100 tanques y la
    // primera estrella era un regalo temprano.
    static void ProbarPremiosDelBestiario(Informe inf)
    {
        bool crecePorEscalon = true, elMasDuroPagaMas = true, proporcion = true, creceConLaOleada = true;
        foreach (int m in new[] { 0, 5, 10, 20, 40 })
        {
            foreach (string tipo in Bestiario.Tipos)
            {
                var escalones = Bestiario.Escalones(tipo);
                double anterior = 0;
                for (int e = 0; e < Bestiario.Estrellas; e++)
                {
                    double premio = Bestiario.Premio(tipo, e, m);
                    if (premio <= anterior) crecePorEscalon = false;
                    anterior = premio;

                    // Nunca mas que lo que dejan esas muertes: si no, conviene cobrar
                    // estrellas antes que jugar.
                    double dejan = escalones[e] * Bestiario.MonedasQueDeja(tipo, m);
                    if (premio > dejan) proporcion = false;
                }
            }

            // El tanque cuesta tres veces mas que el caminante y paga mas por el mismo
            // escalon de muertes; el jefe, mas todavia.
            if (Bestiario.Premio(Bestiario.Tanque, 0, m) <= Bestiario.Premio(Bestiario.Normal, 0, m)) elMasDuroPagaMas = false;
            if (Bestiario.Premio(Bestiario.Jefe, 0, m) <= 0) elMasDuroPagaMas = false;
            if (m > 0 && Bestiario.Premio(Bestiario.Normal, 0, m) <= Bestiario.Premio(Bestiario.Normal, 0, 0)) creceConLaOleada = false;
        }

        inf.Verdadero("bestiario: cada estrella paga mas que la anterior", crecePorEscalon);
        inf.Verdadero("bestiario: el zombi mas duro paga mas por el mismo escalon", elMasDuroPagaMas);
        inf.Verdadero("bestiario: el premio no pasa lo que dejan esas muertes", proporcion);
        inf.Verdadero("bestiario: el premio crece con la mejor oleada", creceConLaOleada);
    }

    // ------------------------------------------------------------------------
    // El nivel del jugador y los logros (NivelJugador, Logros)
    // ------------------------------------------------------------------------

    // La curva: lo que cuesta cada nivel, la experiencia con que se llega a cada uno y la
    // vuelta (NivelCon) justo en los bordes, que es donde una raiz en double se equivoca.
    static void ProbarCurvaDeNivel(Informe inf)
    {
        inf.Igual("nivel: el primero cuesta 25", 25, NivelJugador.CostoDelNivel(1));
        inf.Igual("nivel: el segundo, 125", 125, NivelJugador.CostoDelNivel(2));
        inf.Igual("nivel: el decimo, 925", 925, NivelJugador.CostoDelNivel(10));
        inf.Cerca("nivel: al 1 se llega con 0", 0, NivelJugador.ExperienciaDelNivel(1), 1e-9);
        inf.Cerca("nivel: al 2, con 25", 25, NivelJugador.ExperienciaDelNivel(2), 1e-9);
        inf.Cerca("nivel: al 10, con 3.825", 3825, NivelJugador.ExperienciaDelNivel(10), 1e-9);
        inf.Igual("nivel: sin experiencia, el 1", 1, NivelJugador.NivelCon(0));
        inf.Igual("nivel: con experiencia negativa, el 1", 1, NivelJugador.NivelCon(-5));
        inf.Igual("nivel: con NaN, el 1", 1, NivelJugador.NivelCon(double.NaN));

        bool bordes = true, sumaDeCostos = true;
        string cual = "";
        double acumulada = 0;
        for (int nivel = 1; nivel <= 3000; nivel++)
        {
            double experiencia = NivelJugador.ExperienciaDelNivel(nivel);
            if (Math.Abs(experiencia - acumulada) > 1e-6) sumaDeCostos = false;
            acumulada += NivelJugador.CostoDelNivel(nivel);
            bool justo = NivelJugador.NivelCon(experiencia) == nivel;
            bool antes = nivel == 1 || NivelJugador.NivelCon(experiencia - 0.001) == nivel - 1;
            if (justo && antes) continue;
            if (bordes) cual = " (falla el " + nivel + ")";
            bordes = false;
        }
        inf.Verdadero("nivel: la experiencia de cada nivel es la suma de los costos de antes", sumaDeCostos);
        inf.Verdadero("nivel: NivelCon acierta justo en el borde de los 3.000 primeros" + cual, bordes);

        // El premio: un 15 % de una partida con la mejor oleada, y el triple cada cinco.
        inf.Cerca("nivel: premio del 2 sin oleadas", 20, NivelJugador.Premio(2, 0), 1e-9);
        inf.Cerca("nivel: premio del 6 con la oleada 10", 140, NivelJugador.Premio(6, 10), 1e-9);
        inf.Cerca("nivel: el 5 es grande, el triple", 420, NivelJugador.Premio(5, 10), 1e-9);
        inf.Verdadero("nivel: el premio crece con la mejor oleada", NivelJugador.Premio(7, 40) > NivelJugador.Premio(7, 20));
    }

    // El ritmo: con la experiencia de jugar (los puntos) y una mejor oleada que crece como la
    // raiz de las partidas jugadas (3,3 por la raiz de k: la 10 en la partida 9, la 33 en la
    // 100), el nivel 10 cae cerca de la partida 10 y el 50 cerca de la 55, que es lo que dice
    // NivelJugador. Los puntos de cada oleada salen del WaveManager de WaveMode: su mezcla de
    // tipos, sus pesos, el jefe y los puntos del asset de cada uno.
    static void ProbarRitmoDelNivel(Informe inf)
    {
        var tipos = new List<float[]>();   // desde que oleada, peso, puntos
        int zombisBase = -1, zombisPorOleada = 0, cadaJefe = 0, puntosJefe = 0;
        LeerEscena("Assets/Escenas/WaveMode.unity", escena =>
        {
            var oleadas = Buscar<WaveManager>(escena);
            if (oleadas == null) return;
            zombisBase = oleadas.zombisBase;
            zombisPorOleada = oleadas.zombisPorOleada;
            cadaJefe = oleadas.jefeCadaOleadas;
            foreach (var tipo in oleadas.tipos)
            {
                var zombi = tipo.prefab != null ? tipo.prefab.GetComponent<EnemyController>() : null;
                if (zombi != null && zombi.enemyType != null) tipos.Add(new float[] { tipo.desdeOleada, tipo.peso, zombi.enemyType.puntos });
            }
            var jefe = oleadas.jefe != null ? oleadas.jefe.GetComponent<EnemyController>() : null;
            if (jefe != null && jefe.enemyType != null) puntosJefe = jefe.enemyType.puntos;
        });
        if (!inf.Verdadero("nivel: WaveMode tiene su WaveManager con tipos", zombisBase >= 0 && tipos.Count > 0)) return;

        Func<int, double> puntosDeOleada = n =>
        {
            double suma = 0, pesos = 0;
            foreach (var t in tipos)
            {
                if (n < t[0]) continue;
                suma += t[1] * t[2];
                pesos += t[1];
            }
            double medio = pesos > 0 ? suma / pesos : 1;
            return (zombisBase + zombisPorOleada * n) * medio + (cadaJefe > 0 && n % cadaJefe == 0 ? puntosJefe : 0);
        };

        double experiencia = 0;
        int del10 = -1, del50 = -1;
        for (int k = 1; k <= 400 && del50 < 0; k++)
        {
            int llega = (int)Math.Floor(3.3 * Math.Sqrt(k));
            for (int n = 1; n <= llega; n++) experiencia += puntosDeOleada(n);
            experiencia += 0.4 * puntosDeOleada(llega + 1);   // y muere a mitad de la siguiente
            int nivel = NivelJugador.NivelCon(experiencia);
            if (del10 < 0 && nivel >= 10) del10 = k;
            if (del50 < 0 && nivel >= 50) del50 = k;
        }
        inf.Verdadero("nivel: el 10 cae cerca de la partida 10 (en la " + del10 + ")", del10 >= 7 && del10 <= 14);
        inf.Verdadero("nivel: el 50 cae cerca de la partida 55 (en la " + del50 + ")", del50 >= 40 && del50 <= 75);
    }

    // Las familias de logros: doce, con ids distintos y tres metas que crecen, y los textos
    // de cada una en los dos idiomas: el nombre, el simbolo de la medalla, la descripcion con
    // su {0} y, si una meta es 1, su texto propio. Los ids de esos textos se arman en codigo y
    // la prueba de idiomas no los ve: por eso van aca.
    static void ProbarFamiliasDeLogros(Informe inf)
    {
        inf.Igual("logros: doce familias", 12, Logros.Familias.Length);
        var ids = new HashSet<string>();
        var lenguas = new[] { Lengua.Ingles, Lengua.Espanol };
        bool unicos = true, metas = true, textos = true;
        string falta = "";
        foreach (var familia in Logros.Familias)
        {
            if (!ids.Add(familia.id)) unicos = false;
            if (familia.metas == null || familia.metas.Length != Logros.Escalones || !(familia.metas[0] > 0))
            {
                metas = false;
                continue;
            }
            for (int e = 1; e < Logros.Escalones; e++) if (!(familia.metas[e] > familia.metas[e - 1])) metas = false;

            var necesarios = new List<string> { "logro_" + familia.id + "_nombre", "logro_" + familia.id, "logro_" + familia.id + "_simbolo" };
            foreach (double meta in familia.metas) if (meta == 1) necesarios.Add("logro_" + familia.id + "_uno");
            foreach (string id in necesarios)
            {
                foreach (var lengua in lenguas)
                {
                    if (!string.IsNullOrEmpty(Textos.Crudo(id, lengua))) continue;
                    textos = false;
                    falta += id + " ";
                }
            }
            foreach (var lengua in lenguas)
            {
                string plantilla = Textos.Crudo("logro_" + familia.id, lengua);
                if (plantilla == null || plantilla.Contains("{0}")) continue;
                textos = false;
                falta += "logro_" + familia.id + "(sin {0}) ";
            }
        }
        foreach (string id in new[] { "aviso_logro_bronce", "aviso_logro_plata", "aviso_logro_oro" })
        {
            foreach (var lengua in lenguas)
            {
                if (!string.IsNullOrEmpty(Textos.Crudo(id, lengua))) continue;
                textos = false;
                falta += id + " ";
            }
        }
        inf.Verdadero("logros: ids distintos", unicos);
        inf.Verdadero("logros: tres metas por familia, que crecen", metas);
        inf.Verdadero("logros: los textos de cada familia en los dos idiomas" + (textos ? "" : " (faltan " + falta.Trim() + ")"), textos);

        // El simbolo de la medalla (VentanaLogros): una o dos letras, que con tres no entran en
        // el circulo, y distinto dentro de cada idioma. Con la inicial del nombre salian tres C
        // en espaniol y tres E en ingles. Que la fila este lo mira el caso de arriba.
        foreach (var lengua in lenguas)
        {
            var simbolos = new HashSet<string>();
            string repetidos = "", malos = "";
            foreach (var familia in Logros.Familias)
            {
                string simbolo = Textos.Crudo("logro_" + familia.id + "_simbolo", lengua);
                if (string.IsNullOrEmpty(simbolo)) continue;
                if (!simbolos.Add(simbolo)) repetidos += simbolo + " ";
                bool letras = simbolo.Length <= 2;
                foreach (char c in simbolo) if (!char.IsLetter(c)) letras = false;
                if (!letras) malos += simbolo + " ";
            }
            string codigo = Idioma.Codigo(lengua);
            inf.Verdadero("logros: los simbolos de las medallas son distintos en " + codigo
                          + (repetidos.Length == 0 ? "" : " (se repite " + repetidos.Trim() + ")"), repetidos.Length == 0);
            inf.Verdadero("logros: cada simbolo es una o dos letras en " + codigo
                          + (malos.Length == 0 ? "" : " (" + malos.Trim() + ")"), malos.Length == 0);
        }
        inf.Igual("logros: la descripcion lleva la meta", "Llega a un combo x50", Logros.Descripcion(Logros.Imparable, 50));
        inf.Igual("logros: una meta de 1 no dice \"1 veces\"", "Usa la furia por primera vez", Logros.Descripcion(Logros.Furioso, 1));
        inf.Cerca("logros: el bronce vale un nivel del que se gano", NivelJugador.CostoDelNivel(7),
                  Logros.Experiencia(new LogroGanado { escalon = 0, nivelAlGanar = 7 }), 1e-9);
        inf.Cerca("logros: el oro, cuatro", 4.0 * NivelJugador.CostoDelNivel(7),
                  Logros.Experiencia(new LogroGanado { escalon = 2, nivelAlGanar = 7 }), 1e-9);
    }

    // Los puntos y las monedas de cada zombi estan copiados a mano en NivelJugador (la
    // experiencia de un progreso viejo) y en el Bestiario (su premio), porque hacen falta sin
    // escena. Si se toca el balance de un zombi y no esas copias, esto falla.
    static void ProbarPuntosYMonedasDeLosZombis(Informe inf)
    {
        foreach (string tipo in Bestiario.Tipos)
        {
            var enemigo = AssetDatabase.LoadAssetAtPath<Enemy>("Assets/Zombies/" + tipo + ".asset");
            if (!inf.Verdadero("zombis: esta el asset de " + tipo, enemigo != null)) continue;
            inf.Igual("zombis: los puntos de " + tipo + " en NivelJugador", enemigo.puntos, NivelJugador.PuntosPorTipo(tipo));
            // MonedasQueDeja con la oleada 0 es el promedio del asset por el crecimiento de las
            // monedas a la 1,5 (y el jefe, por 6). El crecimiento es el de Economia, que
            // ProbarCrecimientoDeMonedas compara con la escena: escrito a mano aca no notaba
            // que el Bestiario se quedara con uno viejo.
            double esperado = (enemigo.monedasMin + enemigo.monedasMax) * 0.5 * (tipo == Bestiario.Jefe ? 6.0 : 1.0)
                            * Math.Pow(Economia.CrecimientoMonedasOleadas, 1.5);
            inf.Cerca("zombis: las monedas de " + tipo + " en el Bestiario", esperado, Bestiario.MonedasQueDeja(tipo, 0), 1e-9);
        }
    }

    // La experiencia en el progreso: sube niveles, cada nivel deja un premio con la mejor
    // oleada de ese momento, se cobran de a uno y se guardan. Y la migracion de la v5.
    static void ProbarNivelDelJugador(Informe inf)
    {
        EmpezarCaso("{\"version\":" + Progreso.VersionActual + ",\"monedas\":0,\"mejorOleada\":10}", null);
        inf.Igual("nivel: arranca en el 1", 1, NivelJugador.Nivel);
        inf.Igual("nivel: 24 de experiencia no alcanzan", 0, NivelJugador.Sumar(24));
        inf.Igual("nivel: con 25 sube al 2", 1, NivelJugador.Sumar(1));
        inf.Igual("nivel: un premio esperando", 1, NivelJugador.PorCobrar);
        inf.Igual("nivel: 350 mas, dos niveles de una", 2, NivelJugador.Sumar(350));
        inf.Igual("nivel: ya en el 4", 4, NivelJugador.Nivel);
        inf.Igual("nivel: tres premios esperando", 3, NivelJugador.PorCobrar);

        // El premio queda con la oleada del momento de subir: mejorarla despues no lo sube.
        // Si no, la jugada optima seria no cobrarlo nunca.
        Progreso.RegistrarOleadaCompletada(40);
        double esperado = NivelJugador.Premio(2, 10);
        inf.Cerca("nivel: cobra primero el del 2, con la oleada de cuando subio", esperado, NivelJugador.Cobrar(), 1e-9);
        inf.Cerca("nivel: las monedas llegaron", esperado, Progreso.Monedas, 1e-9);
        inf.Cerca("nivel: no cuentan como jugadas", 0, Progreso.MonedasGanadasJugando, 1e-9);
        inf.Igual("nivel: quedan dos", 2, NivelJugador.PorCobrar);
        var pendiente = NivelJugador.Pendiente;
        inf.Igual("nivel: el que sigue es el del 3", 3, pendiente != null ? pendiente.nivel : -1);

        // Un nivel que se sube despues de mejorar la marca paga con la nueva.
        NivelJugador.Sumar(NivelJugador.ExperienciaDelNivel(5) - NivelJugador.Experiencia);
        var delCinco = NivelJugador.PendienteDe(5);
        inf.Igual("nivel: el del 5 quedo con la oleada 40", 40, delCinco != null ? delCinco.oleada : -1);

        // Se guarda: al releer, la experiencia y los premios siguen ahi.
        double experiencia = NivelJugador.Experiencia;
        Progreso.UsarCarpetaDePruebas(CarpetaProgreso);
        inf.Cerca("nivel: se relee la experiencia", experiencia, NivelJugador.Experiencia, 1e-9);
        inf.Igual("nivel: se releen los premios", 3, NivelJugador.PorCobrar);
        while (NivelJugador.Cobrar() > 0) { }
        inf.Igual("nivel: cobrados todos, no queda ninguno", 0, NivelJugador.PorCobrar);

        // La migracion: un progreso de antes (v5) arranca con la experiencia de sus muertes,
        // sin premio por los niveles de antes, y con la racha de ahora como la mejor.
        string ruta = EmpezarCaso("{\"version\":5,\"monedas\":0,\"rachaRecompensa\":4,\"estadisticas\":{\"matados\":[" +
                                  "{\"id\":\"ZombiNormal\",\"cantidad\":1500},{\"id\":\"ZombiTanque\",\"cantidad\":10}," +
                                  "{\"id\":\"ZombiBOSS\",\"cantidad\":2}]}}", null);
        inf.Cerca("nivel: la migracion suma los puntos de las muertes", 1500 + 200 + 200, NivelJugador.Experiencia, 1e-9);
        inf.Igual("nivel: con 1.900 es el 7", 7, NivelJugador.Nivel);
        inf.Igual("nivel: los niveles de antes no dejan premio", 0, NivelJugador.PorCobrar);
        inf.Igual("nivel: la mejor racha arranca con la de ahora", 4, Progreso.MejorRacha);
        Progreso.Guardar();
        var guardado = LeerGuardado(ruta);
        inf.Igual("nivel: se guarda como v" + Progreso.VersionActual, Progreso.VersionActual, guardado != null ? guardado.version : -1);
        NivelJugador.Sumar(NivelJugador.ExperienciaDelNivel(8) - NivelJugador.Experiencia);
        inf.Igual("nivel: el primero que se sube despues si deja premio", 1, NivelJugador.PorCobrar);
    }

    // Los logros en el progreso: se anotan al verlos con el nivel de ese momento, se cobran
    // de a uno y en orden, y lo que dan no cambia por cobrarlos mas tarde.
    static void ProbarLogros(Informe inf)
    {
        EmpezarCaso("{\"version\":" + Progreso.VersionActual + ",\"monedas\":0,\"mejorOleada\":25," +
                    "\"estadisticas\":{\"matados\":[{\"id\":\"ZombiNormal\",\"cantidad\":1500}]}}", null);
        int nivel = NivelJugador.Nivel;   // es v6: no migra, arranca sin experiencia
        var nuevas = Logros.Revisar();
        inf.Igual("logros: oleada 25 y 1.500 zombis, tres monedas nuevas", 3, nuevas.Count);
        inf.Igual("logros: superviviente con bronce y plata", 2, Logros.Ganadas(Logros.Superviviente));
        inf.Igual("logros: exterminador con bronce", 1, Logros.Ganadas(Logros.Exterminador));
        inf.Igual("logros: la segunda mirada no encuentra nada", 0, Logros.Revisar().Count);
        inf.Igual("logros: tres para cobrar", 3, Logros.PorCobrar);
        var plata = Logros.Ganado(Logros.Superviviente, 1);
        inf.Igual("logros: se anotan con el nivel de ahora", nivel, plata != null ? plata.nivelAlGanar : -1);

        // Subir de nivel antes de cobrar no cambia lo que dan: es la regla de todos los premios.
        NivelJugador.Sumar(NivelJugador.ExperienciaDelNivel(nivel + 10) - NivelJugador.Experiencia);
        double antes = NivelJugador.Experiencia;
        double costo = NivelJugador.CostoDelNivel(nivel);
        inf.Cerca("logros: cobra el bronce, lo que valia un nivel al ganarlo", costo, Logros.Cobrar(Logros.Superviviente), 1e-9);
        inf.Cerca("logros: despues la plata, dos", 2.0 * costo, Logros.Cobrar(Logros.Superviviente), 1e-9);
        inf.Cerca("logros: el oro no esta ganado", 0, Logros.Cobrar(Logros.Superviviente), 1e-9);
        inf.Cerca("logros: la experiencia llego al nivel", antes + 3.0 * costo, NivelJugador.Experiencia, 1e-9);
        inf.Igual("logros: queda el de exterminador", 1, Logros.PorCobrar);

        // Se guardan al cobrar: al releer siguen ganadas y cobradas.
        Progreso.UsarCarpetaDePruebas(CarpetaProgreso);
        inf.Igual("logros: se releen las ganadas", 2, Logros.Ganadas(Logros.Superviviente));
        inf.Igual("logros: y las cobradas", 2, Logros.Cobradas(Logros.Superviviente));

        // Los records solo suben.
        Progreso.RegistrarCombo(30);
        Progreso.RegistrarCombo(12);
        inf.Igual("logros: el mejor combo no baja", 30, Progreso.MejorCombo);
        Progreso.RegistrarGranada(6);
        Progreso.RegistrarGranada(2);
        inf.Igual("logros: la mejor granada no baja", 6, Progreso.MejorGranada);
        inf.Igual("logros: combo x30 y una granada de 6, dos bronces", 2, Logros.Revisar().Count);

        // Un archivo con repetidos o escalones que no existen se limpia al cargar.
        EmpezarCaso("{\"version\":" + Progreso.VersionActual + ",\"jugador\":{\"experiencia\":50,\"logros\":[" +
                    "{\"familia\":\"imparable\",\"escalon\":0,\"nivelAlGanar\":3}," +
                    "{\"familia\":\"imparable\",\"escalon\":0,\"nivelAlGanar\":9}," +
                    "{\"familia\":\"imparable\",\"escalon\":7,\"nivelAlGanar\":3}," +
                    "{\"familia\":\"\",\"escalon\":1,\"nivelAlGanar\":3}]}}", null);
        var logros = Progreso.Jugador.logros;
        inf.Igual("logros: al cargar quedan los validos, sin repetidos", 1, logros.Count);
        inf.Igual("logros: de los repetidos gana el primero", 3, logros.Count > 0 ? logros[0].nivelAlGanar : -1);

        // La derrota muestra el nivel siguiente cuando es lo que esta mas cerca.
        EmpezarCaso("{\"version\":" + Progreso.VersionActual + ",\"jugador\":{\"experiencia\":145}}", null);
        MisionesDiarias.Asegurar();
        Progreso.Misiones.lista.Clear();
        string texto;
        float fraccion;
        ProximoObjetivo.Elegir(out texto, out fraccion);
        inf.Igual("logros: la derrota dice lo que falta para el nivel 3", "TE FALTAN 5 XP PARA EL NIVEL 3", texto);
    }

    // Una moneda ganada queda aunque la marca baje: la del critico se gana comprando, y el
    // renacer va a devolver las mejoras a cero.
    static void ProbarLogroDelCritico(Informe inf)
    {
        EmpezarConMonedas(0);
        Progreso.DepurarFijarNivel("criticos", 4);
        Logros.Revisar();
        inf.Igual("logros: critico al 30 %, el bronce", 1, Logros.Ganadas(Logros.Critico));
        Progreso.DepurarFijarNivel("criticos", 8);
        Logros.Revisar();
        inf.Igual("logros: critico al 100 %, las tres", 3, Logros.Ganadas(Logros.Critico));
        Progreso.DepurarFijarNivel("criticos", 0);
        Logros.Revisar();
        inf.Igual("logros: con el critico en cero siguen ganadas", 3, Logros.Ganadas(Logros.Critico));
    }

    static void ProbarRecompensaDiaria(Informe inf)
    {
        inf.Igual("diaria: nunca cobrada, racha 1", 1, RecompensaDiaria.RachaParaHoy(20260917, 0, 0));
        inf.Igual("diaria: mismo dia, nada", 0, RecompensaDiaria.RachaParaHoy(20260917, 20260917, 3));
        inf.Igual("diaria: ayer, sube la racha", 4, RecompensaDiaria.RachaParaHoy(20260917, 20260916, 3));
        inf.Igual("diaria: salto un dia, vuelve a 1", 1, RecompensaDiaria.RachaParaHoy(20260917, 20260915, 3));
        inf.Igual("diaria: reloj atrasado, nada", 0, RecompensaDiaria.RachaParaHoy(20260910, 20260917, 3));
        inf.Igual("diaria: fin de mes", 6, RecompensaDiaria.RachaParaHoy(20261001, 20260930, 5));
        inf.Igual("diaria: fin de anio", 2, RecompensaDiaria.RachaParaHoy(20270101, 20261231, 1));
        inf.Igual("diaria: bisiesto", 2, RecompensaDiaria.RachaParaHoy(20280229, 20280228, 1));
        inf.Igual("diaria: fecha guardada rota, racha 1", 1, RecompensaDiaria.RachaParaHoy(20260917, 20260899, 4));
        inf.Igual("diaria: casillero del dia 9 es el 7", 7, RecompensaDiaria.Casillero(9));
        inf.Cerca("diaria: monto dia 1 sin oleadas", 150, RecompensaDiaria.Monto(1, 0), 1e-9);
        inf.Cerca("diaria: monto dia 7", 2000, RecompensaDiaria.Monto(7, 0), 1e-9);
        inf.Cerca("diaria: monto dia 12 queda en el 7", 2000, RecompensaDiaria.Monto(12, 0), 1e-9);
        // Era 440 hasta que la suma de zombis se corrigio (23/9).
        inf.Cerca("diaria: monto con mejor oleada 10", 470, RecompensaDiaria.Monto(3, 10), 1e-9);
        ProbarMontosDeLaDiaria(inf);

        EmpezarCaso("{\"version\":3,\"monedas\":10,\"mejorOleada\":5}", null);
        inf.Igual("diaria: sin campos, dia 0", 0, Progreso.DiaUltimaRecompensa);
        double esperadoDelDia1 = RecompensaDiaria.Monto(1, Progreso.MejorOleada);
        inf.Cerca("diaria: cobro dia 1 paga lo que dice la ventana", esperadoDelDia1, RecompensaDiaria.CobrarEl(20260917), 1e-9);
        inf.Cerca("diaria: monedas tras cobrar", 10 + esperadoDelDia1, Progreso.Monedas, 1e-9);
        inf.Cerca("diaria: no cuenta como partida", 0, Progreso.MonedasDeLaPartida, 1e-9);
        inf.Cerca("diaria: el mismo dia no paga", 0, RecompensaDiaria.CobrarEl(20260917), 1e-9);
        double esperadoDelDia2 = RecompensaDiaria.Monto(2, Progreso.MejorOleada);
        inf.Cerca("diaria: al otro dia paga el 2", esperadoDelDia2, RecompensaDiaria.CobrarEl(20260918), 1e-9);
        Progreso.UsarCarpetaDePruebas(CarpetaProgreso);
        inf.Igual("diaria: se relee el dia", 20260918, Progreso.DiaUltimaRecompensa);
        inf.Igual("diaria: se relee la racha", 2, Progreso.RachaRecompensa);
        inf.Cerca("diaria: se releen las monedas", 10 + esperadoDelDia1 + esperadoDelDia2, Progreso.Monedas, 1e-9);
        double esperadoDelDia3 = RecompensaDiaria.Monto(3, Progreso.MejorOleada);
        inf.Cerca("diaria: dia 3 paga", esperadoDelDia3, RecompensaDiaria.CobrarEl(20260919), 1e-9);
        inf.Cerca("diaria: queda para duplicar lo cobrado", esperadoDelDia3, RecompensaDiaria.ParaDuplicar, 1e-9);
        inf.Cerca("diaria: el video paga lo mismo otra vez", esperadoDelDia3, RecompensaDiaria.CobrarDuplicado(), 1e-9);
        inf.Cerca("diaria: el video no paga dos veces", 0, RecompensaDiaria.CobrarDuplicado(), 1e-9);
        inf.Igual("diaria: el video no toca la racha", 3, Progreso.RachaRecompensa);
        inf.Cerca("diaria: monedas tras el video",
                  10 + esperadoDelDia1 + esperadoDelDia2 + esperadoDelDia3 * 2, Progreso.Monedas, 1e-9);
        inf.Cerca("diaria: sin cobro no hay video", 0, RecompensaDiaria.CobrarDuplicado(), 1e-9);
    }

    // La diaria paga con la misma vara que las misiones y el bestiario: un monto fijo
    // servia en la oleada 5, regalaba en la 1 y era calderilla en la 40, justo cuando mas
    // hace falta la razon para volver. Los montos viejos quedaron como piso del arranque.
    static void ProbarMontosDeLaDiaria(Informe inf)
    {
        // En el arranque manda el piso: el primer dia sigue comprando la primera mejora.
        inf.Cerca("diaria: en el arranque paga el piso de siempre", 150, RecompensaDiaria.Monto(1, 0), 1e-9);
        inf.Cerca("diaria: y el dia 7 tambien", 2000, RecompensaDiaria.Monto(7, 0), 1e-9);

        bool creceConLaRacha = true, creceConLaOleada = true, seNota = true, noSePasa = true;
        double anteriorPorOleada = 0;
        foreach (int m in new[] { 0, 5, 11, 20, 30, 45 })
        {
            double anterior = 0;
            for (int racha = 1; racha <= RecompensaDiaria.DiasDelCiclo; racha++)
            {
                double monto = RecompensaDiaria.Monto(racha, m);
                if (monto < anterior) creceConLaRacha = false;
                anterior = monto;
            }
            // El dia 7 tiene que valer mas que una partida (si no, no mueve a nadie) y
            // menos que tres (si no, entrar paga mas que jugar).
            double partida = Economia.MonedasPorPartida(m);
            double dia7 = RecompensaDiaria.Monto(7, m);
            if (m >= 11 && dia7 < partida) seNota = false;
            // En el arranque manda el piso (2.000 contra una partida de 108) y eso es a
            // proposito: el tope vale desde que el premio lo decide lo que da jugar.
            if (m >= 11 && dia7 > partida * 3.0) noSePasa = false;
            if (m > 0 && RecompensaDiaria.Monto(7, m) < anteriorPorOleada) creceConLaOleada = false;
            anteriorPorOleada = RecompensaDiaria.Monto(7, m);
        }

        inf.Verdadero("diaria: el monto crece con la racha", creceConLaRacha);
        inf.Verdadero("diaria: y con la mejor oleada", creceConLaOleada);
        inf.Verdadero("diaria: el dia 7 vale al menos una partida", seNota);
        inf.Verdadero("diaria: pero nunca mas de tres", noSePasa);

        // Con que oleada paga hoy se congela la primera vez que se pregunta, como el
        // premio de las misiones: si no, dejar la ventana sin cobrar, jugar hasta mejorar
        // la marca y recien ahi tocar COBRAR era la jugada optima, y pagaba el premio de
        // la marca nueva por el dia que ya venia corriendo.
        int hoy = 20260922;
        EmpezarConMonedas(0);
        Progreso.RegistrarOleadaCompletada(20);
        inf.Igual("diaria: anota la marca la primera vez que se pregunta",
                  20, Progreso.OleadaDeLaRecompensa(hoy));

        Progreso.RegistrarOleadaCompletada(40);
        inf.Igual("diaria: mejorar la marca no sube lo que paga hoy",
                  20, Progreso.OleadaDeLaRecompensa(hoy));
        inf.Igual("diaria: pero el progreso si la registra", 40, Progreso.MejorOleada);

        // Y el cobro de verdad paga con la congelada, no con la de ahora.
        double cobrado = RecompensaDiaria.CobrarEl(hoy);
        inf.Cerca("diaria: y cobra con la congelada", RecompensaDiaria.Monto(1, 20), cobrado, 1e-9);
        inf.Verdadero("diaria: que no es lo que pagaria la marca nueva",
                      RecompensaDiaria.Monto(1, 40) > cobrado);
        inf.Cerca("diaria: las monedas que entraron son las cobradas", cobrado, Progreso.Monedas, 1e-9);

        inf.Igual("diaria: maniana ya paga con la marca nueva",
                  40, Progreso.OleadaDeLaRecompensa(hoy + 1));
    }

    static void ProbarComprasPosibles(Informe inf)
    {
        double[] monedas = { 0, 29, 30, 69, 70, 109, 110, 154, 155, 204, 205 };
        int[] esperadas = { 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5 };
        for (int i = 0; i < monedas.Length; i++)
        {
            EmpezarConMonedas(monedas[i]);
            inf.Igual("ComprasPosibles con " + Numero(monedas[i]) + " monedas", esperadas[i], CatalogoMejoras.ComprasPosibles());
        }
    }

    static Mejora CrearTemporal(List<Mejora> temporales, string id, double precioInicial, double crecimientoPrecio,
                                int nivelMaximo, CrecimientoEfecto crecimiento, double efectoPorNivel,
                                double valorBase, FormatoValor formato)
    {
        var mejora = ScriptableObject.CreateInstance<Mejora>();
        mejora.hideFlags = HideFlags.DontSave;
        mejora.name = "PruebaTemporal " + id;
        mejora.id = id;
        mejora.precioInicial = precioInicial;
        mejora.crecimientoPrecio = crecimientoPrecio;
        mejora.nivelMaximo = nivelMaximo;
        mejora.crecimiento = crecimiento;
        mejora.efectoPorNivel = efectoPorNivel;
        mejora.valorBase = valorBase;
        mejora.formato = formato;
        temporales.Add(mejora);
        return mejora;
    }

    // ========================================================================
    // Medicion en modo play
    // ========================================================================

    // Un tramo de la partida con la misma cadencia esperada: sin caja, con caja,
    // y cada nivel de cadencia. Se compara por tramo porque al vencer la caja la
    // cadencia cambia a mitad de la medicion.
    private class Regimen
    {
        public bool conCaja;
        public float decimos;       // Mathf.Round(TirosPorSegundo * 10): la clave
        public float esperado;      // TirosPorSegundo del arma al entrar al tramo
        public double duracion;
        public long tiros;
    }

    // Lo que tenia un tipo de zombi en una oleada (o nivel del modo libre).
    private class Par
    {
        public string tipo;
        public int oleada;
        public float vida, dano, monedas;
        public bool conEsperado;
        public double vidaEsperada, danoEsperado, monedasEsperadas;
    }

    private class Medicion
    {
        public Informe informe;
        public float segundos;
        public float inicio;
        public int escena;

        public PlayerController jugador;
        public GunController arma;
        public PlayerHealth vida;
        public WaveManager oleadas;
        public GeneradorZombis generador;
        public PlayerJS joysticks;
        public bool joysticksHabilitados;
        public int balasAntes;

        public float botin;
        public int muertesAlEmpezar;
        public double esperadasAlEmpezar;
        public double soltadasAlEmpezar;

        // Por numero de aparicion y no por componente: los zombis se reusan, y el
        // mismo EnemyController vuelve a salir como otro zombi.
        public readonly HashSet<int> procesados = new HashSet<int>();
        public readonly List<Par> pares = new List<Par>();
        public readonly List<Regimen> regimenes = new List<Regimen>();

        public bool hayAnterior;
        public bool cajaAnterior;
        public float decimosAnterior;
        public int tirosAnterior;
        public float tiempoAnterior;
    }

    private static Medicion medicion;

    // El reset de siempre para los static. Al entrar a play no hay medicion
    // posible todavia; esto evita arrastrar una colgada si no hay domain reload.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        EditorApplication.update -= Tick;
        medicion = null;
    }

    // Dispara sin parar, mantiene al jugador con vida y mata a cada zombi en el
    // tick en que aparece (asi las oleadas avanzan solas), y al cumplir los
    // segundos de juego escribe Builds/medicion_mejoras.txt.
    //
    // OJO: el bono de cada oleada y las monedas que caen al alcance del iman se
    // suman al progreso real del editor.
    public static void MedirPartida(float segundos)
    {
        var informe = new Informe("MEDICION DE MEJORAS", "PruebasMejoras.MedirPartida: ");

        if (!EditorApplication.isPlaying)
        {
            informe.Falla("MedirPartida solo corre en modo play");
            informe.Escribir(RutaMedicion, informe.Resultado("OK"));
            return;
        }
        if (medicion != null)
        {
            Debug.LogWarning("PruebasMejoras.MedirPartida: ya hay una medicion en curso");
            return;
        }

        var m = new Medicion();
        m.informe = informe;
        m.segundos = Mathf.Max(1f, segundos);
        m.jugador = Object.FindFirstObjectByType<PlayerController>();
        m.arma = m.jugador != null ? m.jugador.theGun : null;
        m.vida = PlayerHealth.instance;
        if (m.jugador == null || m.arma == null || m.vida == null)
        {
            informe.Falla("no hay jugador, arma o PlayerHealth en la escena");
            informe.Escribir(RutaMedicion, informe.Resultado("OK"));
            return;
        }

        // En WaveMode el generador libre puede estar en la escena apagado: manda
        // el que esta andando.
        var oleadas = Object.FindFirstObjectByType<WaveManager>();
        var generador = Object.FindFirstObjectByType<GeneradorZombis>();
        m.oleadas = oleadas != null && oleadas.isActiveAndEnabled ? oleadas : null;
        m.generador = m.oleadas == null && generador != null && generador.isActiveAndEnabled ? generador : null;
        m.joysticks = Object.FindFirstObjectByType<PlayerJS>();
        m.joysticksHabilitados = m.joysticks != null && m.joysticks.enabled;
        // Tick le pone cien mil balas: al terminar se le devuelven las que tenia.
        m.balasAntes = m.jugador.cantBalas;

        var escena = SceneManager.GetActiveScene();
        m.escena = escena.handle;
        m.inicio = Time.time;
        m.botin = CatalogoMejoras.MultiplicadorBotin;
        m.muertesAlEmpezar = EnemyController.MuertesConBotin;
        m.esperadasAlEmpezar = EnemyController.MonedasEsperadas;
        m.soltadasAlEmpezar = EnemyController.MonedasSoltadas;

        informe.Linea("escena: " + escena.name + " (build " + escena.buildIndex + "), " +
                      (m.oleadas != null ? "oleadas" : m.generador != null ? "modo libre" : "sin generador de zombis"));
        informe.Linea("segundos pedidos: " + Numero(m.segundos, "0.##"));
        FotoDeLoAplicado(m);

        medicion = m;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        Debug.Log("PruebasMejoras.MedirPartida: midiendo " + Numero(m.segundos, "0.##") + " s en " + escena.name);
    }

    static void FotoDeLoAplicado(Medicion m)
    {
        var inf = m.informe;
        var catalogo = CatalogoMejoras.Instancia;

        var niveles = new StringBuilder("niveles:");
        if (catalogo != null && catalogo.enTienda != null)
        {
            foreach (var mejora in catalogo.enTienda)
            {
                if (mejora == null) continue;
                niveles.Append(' ').Append(mejora.id).Append(' ').Append(Progreso.Nivel(mejora.id));
            }
        }
        else
        {
            niveles.Append(" (sin catalogo)");
        }
        inf.Linea(niveles.ToString());

        float dano = CatalogoMejoras.DanoPorBala;
        float tiros = CatalogoMejoras.TirosPorSegundo;
        int vidaMaxima = CatalogoMejoras.VidaMaxima;
        float multiplicadorVida = CatalogoMejoras.MultiplicadorVida;
        float radioIman = CatalogoMejoras.RadioIman;

        inf.Linea("catalogo: daño/bala " + Numero(dano, "0.0###") + ", tiros/s " + Numero(tiros, "0.0##") +
                  ", vida " + vidaMaxima + ", multiplicador de vida " + Numero(multiplicadorVida, "0.0##") +
                  ", iman " + Numero(radioIman, "0.0##") + " m, botin x" + Numero(m.botin, "0.0##"));
        inf.Linea("aplicado: daño/bala " + Numero(m.arma.DanoPorBala, "0.0###") +
                  ", tiros/s base " + Numero(m.arma.TirosPorSegundoBase, "0.0##") +
                  ", vida " + m.vida.health + "/" + m.vida.maxHealth +
                  ", cura por caja " + m.vida.CuraPorCaja +
                  ", iman " + Numero(Moneda.RadioImanDeLaPartida, "0.0##") + " m" +
                  ", botin x" + Numero(m.botin, "0.0##"));

        var aplicar = Object.FindFirstObjectByType<AplicarMejoras>();
        if (aplicar != null)
        {
            inf.Linea("AplicarMejoras: daño/bala " + Numero(aplicar.DanoPorBala, "0.0###") +
                      ", tiros/s " + Numero(aplicar.TirosPorSegundo, "0.0##") +
                      ", vida " + aplicar.VidaMaxima + ", multiplicador de cura " + Numero(aplicar.MultiplicadorCura, "0.0##"));
            inf.Cerca("aplicado: AplicarMejoras.DanoPorBala", dano, aplicar.DanoPorBala, 1e-3);
            inf.Cerca("aplicado: AplicarMejoras.TirosPorSegundo", tiros, aplicar.TirosPorSegundo, 1e-3);
            inf.Igual("aplicado: AplicarMejoras.VidaMaxima", vidaMaxima, aplicar.VidaMaxima);
            inf.Cerca("aplicado: AplicarMejoras.MultiplicadorCura", multiplicadorVida, aplicar.MultiplicadorCura, 1e-3);
            inf.Cerca("aplicado: AplicarMejoras.RadioIman", radioIman, aplicar.RadioIman, 1e-3);
        }
        else
        {
            inf.Falla("no hay AplicarMejoras en la escena (va en Jugador.prefab)");
        }

        // Lo que de verdad quedo en el arma y en la vida, por si algo lo piso
        // despues de AplicarMejoras.
        inf.Cerca("aplicado: GunController.DanoPorBala", dano, m.arma.DanoPorBala, 1e-3);
        inf.Cerca("aplicado: GunController.TirosPorSegundoBase", tiros, m.arma.TirosPorSegundoBase, 1e-3);
        inf.Igual("aplicado: PlayerHealth.maxHealth", vidaMaxima, m.vida.maxHealth);
        inf.Cerca("aplicado: PlayerHealth.MultiplicadorCura", multiplicadorVida, m.vida.MultiplicadorCura, 1e-3);
        inf.Cerca("aplicado: Moneda.RadioImanDeLaPartida", radioIman, Moneda.RadioImanDeLaPartida, 1e-3);

        // La furia: si Jugador.prefab pierde el componente, el boton del HUD no aparece nunca.
        var furia = m.jugador.GetComponent<Furia>();
        inf.Verdadero("aplicado: el jugador tiene Furia", furia != null);
        if (furia != null)
            inf.Verdadero("aplicado: Furia.Desbloqueada coincide con la compra", furia.Desbloqueada == CatalogoMejoras.FuriaDesbloqueada);
    }

    static void Tick()
    {
        var m = medicion;
        if (m == null)
        {
            EditorApplication.update -= Tick;
            return;
        }

        if (!EditorApplication.isPlaying)
        {
            Abortar(m, "se salio de modo play");
            return;
        }
        if (EditorApplication.isPaused) return;
        if (SceneManager.GetActiveScene().handle != m.escena)
        {
            Abortar(m, "cambio la escena (¿murio el jugador?)");
            return;
        }
        if (m.jugador == null || m.arma == null || m.vida == null)
        {
            Abortar(m, "se destruyo el jugador");
            return;
        }

        // 1. Disparar sin parar y no morir. PlayerJS se apaga porque con el target
        // en Android pondria isFiring en falso con el joystick suelto.
        if (m.joysticks != null) m.joysticks.enabled = false;
        m.arma.isFiring = true;
        m.jugador.cantBalas = 100000;
        m.vida.health = m.vida.maxHealth;

        // 2. Anotar y matar a los zombis nuevos.
        var zombis = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        foreach (var zombi in zombis)
        {
            if (zombi == null || zombi.enemyType == null || !m.procesados.Add(zombi.NumeroDeAparicion)) continue;
            RegistrarPar(m, zombi);
            zombi.DanoZombi(zombi.VidaActual + 1f);
        }

        // 3. Tiros por tramo: solo cuenta el intervalo entre dos ticks con la misma
        // cadencia, asi el tick en que vence una caja no mezcla los dos tramos.
        bool caja = m.arma.MejoraActiva;
        float decimos = Mathf.Round(m.arma.TirosPorSegundo * 10f);
        int tiros = m.arma.TirosDisparados;
        float ahora = Time.time;
        if (m.hayAnterior && caja == m.cajaAnterior && decimos == m.decimosAnterior)
        {
            var regimen = BuscarRegimen(m, caja, decimos);
            regimen.duracion += ahora - m.tiempoAnterior;
            regimen.tiros += tiros - m.tirosAnterior;
        }
        m.hayAnterior = true;
        m.cajaAnterior = caja;
        m.decimosAnterior = decimos;
        m.tirosAnterior = tiros;
        m.tiempoAnterior = ahora;

        if (ahora - m.inicio >= m.segundos) Terminar(m);
    }

    static Regimen BuscarRegimen(Medicion m, bool caja, float decimos)
    {
        foreach (var r in m.regimenes)
        {
            if (r.conCaja == caja && r.decimos == decimos) return r;
        }
        var nuevo = new Regimen { conCaja = caja, decimos = decimos, esperado = m.arma.TirosPorSegundo };
        m.regimenes.Add(nuevo);
        return nuevo;
    }

    // La oleada del par es la vigente en el tick en que se lo ve: el zombi
    // aparecio en este mismo frame, antes de su Start.
    static void RegistrarPar(Medicion m, EnemyController zombi)
    {
        int oleada = m.oleadas != null ? m.oleadas.OleadaActual
                   : m.generador != null ? m.generador.NivelActual
                   : 0;
        string tipo = zombi.enemyType.name;
        foreach (var p in m.pares)
        {
            if (p.tipo == tipo && p.oleada == oleada) return;
        }

        var par = new Par
        {
            tipo = tipo,
            oleada = oleada,
            vida = zombi.VidaMaxima,
            dano = zombi.DanoPorGolpe,
            monedas = zombi.multiplicadorMonedas,
        };

        if (m.oleadas != null)
        {
            par.conEsperado = true;
            par.vidaEsperada = zombi.enemyType.hp * Escalado.PorOleada(m.oleadas.crecimientoVida, oleada);
            par.danoEsperado = zombi.enemyType.daño * Escalado.PorOleada(m.oleadas.crecimientoDano, oleada);
            par.monedasEsperadas = Escalado.PorOleada(m.oleadas.crecimientoMonedas, oleada) * m.botin;
        }
        else if (m.generador != null)
        {
            par.conEsperado = true;
            par.vidaEsperada = zombi.enemyType.hp * Escalado.PorOleada(m.generador.crecimientoVida, oleada);
            par.danoEsperado = zombi.enemyType.daño * Escalado.PorOleada(m.generador.crecimientoDano, oleada);
            par.monedasEsperadas = m.generador.multiplicadorMonedas * Escalado.PorOleada(m.generador.crecimientoMonedas, oleada) * m.botin;
        }

        m.pares.Add(par);
    }

    static void Restaurar(Medicion m)
    {
        EditorApplication.update -= Tick;
        if (medicion == m) medicion = null;

        if (m.joysticks != null) m.joysticks.enabled = m.joysticksHabilitados;
        if (m.arma != null) m.arma.isFiring = false;
        // Con Min: si durante la medicion agarro una caja de arma, maxBalas cambio.
        if (m.jugador != null) m.jugador.cantBalas = Mathf.Min(m.balasAntes, m.jugador.maxBalas);
    }

    static void Abortar(Medicion m, string motivo)
    {
        Restaurar(m);
        EscribirMediciones(m);
        m.informe.Linea("ABORTADA: " + motivo);
        m.informe.Escribir(RutaMedicion, "ABORTADA");
    }

    static void Terminar(Medicion m)
    {
        Restaurar(m);
        m.informe.Linea("duracion: " + Numero(Time.time - m.inicio, "0.00") + " s de juego");
        EscribirMediciones(m);
        ChequearRegimenes(m);
        ChequearPares(m);
        m.informe.Escribir(RutaMedicion, m.informe.Resultado("OK"));
    }

    static void EscribirMediciones(Medicion m)
    {
        var inf = m.informe;

        inf.Linea("regimenes:");
        if (m.regimenes.Count == 0) inf.Linea("  (ninguno)");
        foreach (var r in m.regimenes)
        {
            double medido = r.duracion > 0 ? r.tiros / r.duracion : 0;
            inf.Linea("  " + NombreRegimen(r) + ": " + Numero(r.duracion, "0.00") + " s, " + r.tiros +
                      " tiros, medido " + Numero(medido, "0.00") + " | esperado " + Numero(r.esperado, "0.00"));
        }

        inf.Linea("zombis vistos:");
        if (m.pares.Count == 0) inf.Linea("  (ninguno)");
        foreach (var p in m.pares)
        {
            string linea = "  " + p.tipo + " " + (m.generador != null ? "nivel " : "oleada ") + p.oleada +
                           ": vida " + Numero(p.vida, "0.0###") + ", daño " + Numero(p.dano, "0.0###") +
                           ", monedas x" + Numero(p.monedas, "0.0###");
            if (p.conEsperado)
            {
                linea += "  (esperado vida " + Numero(p.vidaEsperada, "0.0###") + ", daño " + Numero(p.danoEsperado, "0.0###") +
                         ", monedas x" + Numero(p.monedasEsperadas, "0.0###") + ")";
            }
            inf.Linea(linea);
        }

        int muertes = EnemyController.MuertesConBotin - m.muertesAlEmpezar;
        double soltadas = EnemyController.MonedasSoltadas - m.soltadasAlEmpezar;
        double esperadas = EnemyController.MonedasEsperadas - m.esperadasAlEmpezar;
        string porZombi = muertes > 0
            ? " (por zombi: real " + Numero(soltadas / muertes, "0.000") + " | esperado " + Numero(esperadas / muertes, "0.000") + ")"
            : "";
        inf.Linea("monedas: soltadas " + Numero(soltadas, "0.##") + ", esperadas " + Numero(esperadas, "0.##") +
                  ", muertes con botin " + muertes + porZombi);
    }

    static string NombreRegimen(Regimen r)
    {
        return (r.conCaja ? "con caja" : "sin caja") + " a " + Numero(r.decimos / 10f, "0.0") + " tiros/s";
    }

    // Un tramo largo se mide bien; uno de 2 a 5 s tiene mas ruido por los ticks
    // del editor, y uno de menos de 2 s no dice nada.
    static void ChequearRegimenes(Medicion m)
    {
        foreach (var r in m.regimenes)
        {
            double medido = r.duracion > 0 ? r.tiros / r.duracion : 0;
            string caso = "regimen " + NombreRegimen(r) + " (" + Numero(r.duracion, "0.0") + " s)";
            if (r.duracion >= 5) m.informe.Cerca(caso, r.esperado, medido, 0.5);
            else if (r.duracion >= 2) m.informe.Cerca(caso, r.esperado, medido, 1.0);
            else m.informe.Linea("(" + caso + ": muy corto para comparar)");
        }
    }

    static void ChequearPares(Medicion m)
    {
        foreach (var p in m.pares)
        {
            if (!p.conEsperado) continue;
            string caso = "zombi " + p.tipo + " " + (m.generador != null ? "nivel " : "oleada ") + p.oleada;
            m.informe.Cerca(caso + ": VidaMaxima", p.vidaEsperada, p.vida, ToleranciaRelativa(p.vidaEsperada));
            m.informe.Cerca(caso + ": DanoPorGolpe", p.danoEsperado, p.dano, ToleranciaRelativa(p.danoEsperado));
            m.informe.Cerca(caso + ": multiplicadorMonedas", p.monedasEsperadas, p.monedas, ToleranciaRelativa(p.monedasEsperadas));
        }
    }

    static double ToleranciaRelativa(double valor)
    {
        return 1e-3 * Math.Max(1, Math.Abs(valor));
    }
}
