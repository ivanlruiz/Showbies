using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
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
            ProbarAcumuladorDeDisparo(informe);
            ProbarDanoAlJugador(informe);

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
                    ProbarProximoObjetivo(informe);
                    ProbarBestiario(informe);
                    if (completo)
                    {
                        ProbarGetters(informe, catalogo);
                        ProbarCompras(informe, catalogo, temporales);
                        ProbarComprasPosibles(informe);
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
        // ConfirmarSalir) y los que se arman con el id de una mejora.
        // Solo los ids escritos enteros: "mejora_" + id se prueba aparte, con el catalogo.
        var pedido = new System.Text.RegularExpressions.Regex(@"Textos\.(?:De|Formato)\(\s*""([a-z0-9_]+)""\s*[,)]|\.id\s*=\s*""([a-z0-9_]+)""\s*;");
        int enCodigo = 0, faltanEnCodigo = 0;
        foreach (string archivo in Directory.GetFiles(Path.Combine(Application.dataPath, "Scripts"), "*.cs", SearchOption.AllDirectories))
        {
            foreach (System.Text.RegularExpressions.Match m in pedido.Matches(File.ReadAllText(archivo)))
            {
                string id = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value;
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
                foreach (string parte in new[] { "_nombre", "_unidad", "_simbolo" })
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
    }

    // 4. Escalado por oleada: la cuenta de Escalado.PorOleada, con los crecimientos que
    // tenian las oleadas al escribirla (vida 1,15, daño 1,07, monedas 1,05).
    // El tema de la interfaz: que el claro devuelva lo de siempre, que el oscuro cambie
    // por rol y que lo que se escribe encima se lea. El contraste se mide y no se mira:
    // un color oscuro de mas en la paleta se vería en el telefono y no en el editor.
    static void ProbarTema(Informe inf)
    {
        int antes = Tema.Revision;
        Tema.UsarParaPruebas(false);
        inf.Verdadero("tema: arranca en claro", !Tema.Oscuro);

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

        // Fijar solo mueve Revision cuando cambia de verdad.
        int revision = Tema.Revision;
        Tema.Fijar(true);
        inf.Igual("tema: fijar lo mismo no cambia la revision", revision, Tema.Revision);
        Tema.Fijar(false);
        inf.Verdadero("tema: fijar el otro sube la revision", Tema.Revision > revision);
        inf.Verdadero("tema: quedo en claro", !Tema.Oscuro);

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
        public int veces;

        public string Nombre { get { return "prueba"; } }
        public void Inicializar() { }
        public bool Listo(string lugar) { return hayVideo; }

        public void Mostrar(string lugar, System.Action<ResultadoAnuncio> alTerminar)
        {
            veces++;
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
        inf.Cerca("misiones: numeros redondos", 1250, MisionesDiarias.Redondo(1234), 1e-9);
        inf.Cerca("misiones: premio facil sin oleadas", 15, MisionesDiarias.Monto(0, 0), 1e-9);
        inf.Cerca("misiones: premio dificil con oleada 10", 1300, MisionesDiarias.Monto(2, 10), 1e-9);
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
        inf.Cerca("cofre: con oleada 10 paga mas", 940, MisionesDiarias.MontoCofre(10), 1e-9);

        int diaGuardado = estado.dia;
        Progreso.UsarCarpetaDePruebas(CarpetaProgreso);
        inf.Verdadero("misiones: se relee la cobrada", MisionesDiarias.DeHoy.Count == 3 && MisionesDiarias.DeHoy[0].cobrada);
        inf.Igual("misiones: se releen las oleadas del dia", 5, Progreso.Misiones.oleadasDelDia);

        // Un reloj atrasado no las cambia: el dia guardado es mayor que hoy.
        Progreso.Misiones.dia = diaGuardado + 1;
        MisionesDiarias.Asegurar();
        inf.Verdadero("misiones: con el reloj atrasado siguen las mismas", MisionesDiarias.DeHoy[0].cobrada);

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
            double jugando = cuestan * MisionesDiarias.MonedasPorPartida(m);
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
        inf.Verdadero("misiones: el primer dia no regala (menos de 500 en total)",
                      MisionesDiarias.Monto(0, 0) + MisionesDiarias.Monto(1, 0)
                      + MisionesDiarias.Monto(2, 0) + MisionesDiarias.MontoCofre(0) < 500);
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
        inf.Cerca("diaria: monto con mejor oleada 10", 800, RecompensaDiaria.Monto(3, 10), 1e-9);

        EmpezarCaso("{\"version\":3,\"monedas\":10,\"mejorOleada\":5}", null);
        inf.Igual("diaria: sin campos, dia 0", 0, Progreso.DiaUltimaRecompensa);
        inf.Cerca("diaria: cobro dia 1 con oleada 5", 225, RecompensaDiaria.CobrarEl(20260917), 1e-9);
        inf.Cerca("diaria: monedas tras cobrar", 235, Progreso.Monedas, 1e-9);
        inf.Cerca("diaria: no cuenta como partida", 0, Progreso.MonedasDeLaPartida, 1e-9);
        inf.Cerca("diaria: el mismo dia no paga", 0, RecompensaDiaria.CobrarEl(20260917), 1e-9);
        inf.Cerca("diaria: al otro dia paga el 2", 375, RecompensaDiaria.CobrarEl(20260918), 1e-9);
        Progreso.UsarCarpetaDePruebas(CarpetaProgreso);
        inf.Igual("diaria: se relee el dia", 20260918, Progreso.DiaUltimaRecompensa);
        inf.Igual("diaria: se relee la racha", 2, Progreso.RachaRecompensa);
        inf.Cerca("diaria: se releen las monedas", 610, Progreso.Monedas, 1e-9);
        inf.Cerca("diaria: dia 3 paga", 600, RecompensaDiaria.CobrarEl(20260919), 1e-9);
        inf.Cerca("diaria: queda para duplicar lo cobrado", 600, RecompensaDiaria.ParaDuplicar, 1e-9);
        inf.Cerca("diaria: el video paga lo mismo otra vez", 600, RecompensaDiaria.CobrarDuplicado(), 1e-9);
        inf.Cerca("diaria: el video no paga dos veces", 0, RecompensaDiaria.CobrarDuplicado(), 1e-9);
        inf.Igual("diaria: el video no toca la racha", 3, Progreso.RachaRecompensa);
        inf.Cerca("diaria: monedas tras el video", 1810, Progreso.Monedas, 1e-9);
        inf.Cerca("diaria: sin cobro no hay video", 0, RecompensaDiaria.CobrarDuplicado(), 1e-9);
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
