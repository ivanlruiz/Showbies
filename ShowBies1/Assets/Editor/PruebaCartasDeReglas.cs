using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Banco de las cartas que cambian las reglas (1.6.0, etapa 2), en play: WaveMode sin oleadas, el
// jugador quieto en el centro y zombis puestos a mano en la linea del tiro, quietos con el hielo
// al 100 % (asi la vida que pierden es solo la de la prueba). Cada carta por separado:
//   - perforante: el primero pierde el daño entero, el segundo la mitad y el tercero nada;
//   - explosiva: el tocado pierde el daño, el de al lado el 30 % y el de lejos nada;
//   - sin muertes dobles: perforante y explosiva juntas matan a tres, y los puntos son tres;
//   - disparo doble y hacia atras: las balas de mas, su abanico, y que no gasten cargador ni
//     cuenten en TirosDisparados;
//   - hielo: frena un 40 % y pinta, y a los 2 s vuelve;
//   - quemadura: la mitad de las balas quema, y la quemadura saca el 10 % por segundo, 3 s;
//   - vampiro: cura el 3 % a las 20 muertes, no antes (14/10; era el 5 % a las 15);
//   - escudo: el anillo, para un golpe sin contarlo y corre al zombi pegado; el siguiente entra;
//   - dron: dos drones que le tiran al que se ve.
// Y en cada cuadro, que ZombisVivos coincida con la lista de vivos. Los bancos no abren la
// eleccion de cartas (SinCartasAutomaticas): las cartas se ponen a mano (PoderDeLaPartida.Empezar).
// Escribe Builds/prueba_cartas_de_reglas.txt.
[InitializeOnLoad]
public static class PruebaCartasDeReglas
{
    const string Clave = "ShowBies.PruebaCartasDeReglas";
    const string Banco = "PruebaCartasDeReglas";
    const string Ruta = "../Builds/prueba_cartas_de_reglas.txt";
    const string Escena = "Assets/Escenas/WaveMode.unity";
    const string PrefabNormal = "Assets/Prefabs/Personajes/Zombi.prefab";
    const string PrefabTanque = "Assets/Prefabs/Personajes/ZombiTanque.prefab";
    const double TopeTotal = 150.0;

    enum Paso
    {
        Preparar, Perforante, Explosiva, SinDobles, Abanico, Hielo, Quemadura, Vampiro, Escudo, Dron, Fin
    }

    static Paso paso;
    static int sub;
    static float desde;
    static double inicio;
    static bool empezo, terminado;
    static string error;
    static readonly List<bool> oks = new List<bool>();
    static readonly List<string> ques = new List<string>();
    static readonly StringBuilder notas = new StringBuilder();
    static int cuadrosMirados, cuadrosConLaListaMal;

    static readonly List<EnemyController> puestos = new List<EnemyController>();
    static readonly List<float> vidas = new List<float>();
    static Vector3 centro, hacia;
    static int puntosAntes, valorAntes, tirosAntes, balasAntes, curasAntes, bloqueadosAntes, golpesAntes;
    static float marca, distanciaAntes;
    static Vector3 posA;

    static PruebaCartasDeReglas()
    {
        EditorApplication.update += Tick;
    }

    [MenuItem("ShowBies/Pruebas/Cartas de reglas (play)")]
    // Publico para correrlo por codigo (ver la trampa del registro de menus).
    public static void Arrancar()
    {
        if (!RespaldoDelBanco.PuedeArrancar(Banco)) return;
        RespaldoDelBanco.Guardar(Banco);
        // Sin auto-apuntado: tiraria solo a los zombis de la prueba.
        PlayerJS.FijarAutoApuntar(false);
        PlayerSettings.runInBackground = true;
        Progreso.GuardarOleadaEnCurso(1, 0);
        Progreso.GuardarPoderEnCurso(new EstadoPoder());
        Progreso.Guardar();
        EditorSceneManager.OpenScene(Escena);
        SessionState.SetBool(Clave, true);
        empezo = false;
        EditorApplication.EnterPlaymode();
    }

    static void Tick()
    {
        if (!SessionState.GetBool(Clave, false)) return;
        if (!EditorApplication.isPlaying) return;
        if (!RespaldoDelBanco.SigueArmado(Banco, Clave)) return;

        if (!empezo)
        {
            empezo = true;
            terminado = false;
            error = null;
            oks.Clear();
            ques.Clear();
            notas.Length = 0;
            cuadrosMirados = cuadrosConLaListaMal = 0;
            puestos.Clear();
            vidas.Clear();
            paso = Paso.Preparar;
            sub = 0;
            desde = Time.time;
            inicio = EditorApplication.timeSinceStartup;
            return;
        }
        if (EditorApplication.timeSinceStartup - inicio > TopeTotal) { Terminar("se paso del tope de " + TopeTotal + " s, en " + paso + "/" + sub); return; }
        try
        {
            Avanzar();
        }
        catch (System.Exception e)
        {
            Terminar("excepcion en " + paso + "/" + sub + ": " + e);
        }
    }

    static void Anotar(bool ok, string que)
    {
        oks.Add(ok);
        ques.Add(que);
    }

    static void Pasar(Paso siguiente)
    {
        paso = siguiente;
        sub = 0;
        desde = Time.time;
    }

    static void Sub()
    {
        sub++;
        desde = Time.time;
    }

    // Las cartas de la prueba, como si se hubieran elegido: "id", veces, "id", veces...
    static void Poner(params object[] cartas)
    {
        var estado = new EstadoPoder { semilla = 7 };
        for (int i = 0; i + 1 < cartas.Length; i += 2)
            estado.cartas.Add(new CartaElegida { id = (string)cartas[i], veces = (int)cartas[i + 1] });
        PoderDeLaPartida.Empezar(estado);
    }

    // Un zombi en la linea del tiro, a "x" metros del jugador y "z" de costado, quieto (el hielo
    // al 100 %), o caminando si se pide.
    static EnemyController PonerZombi(string prefab, float x, float z, bool quieto = true)
    {
        var molde = AssetDatabase.LoadAssetAtPath<GameObject>(prefab);
        var zombi = EnemyController.Aparecer(molde, centro + hacia * x + Vector3.Cross(Vector3.up, hacia) * z);
        if (zombi == null) throw new System.Exception("no aparecio " + prefab);
        if (quieto) zombi.Congelar(1f, 600f);
        puestos.Add(zombi);
        vidas.Add(zombi.VidaActual);
        return zombi;
    }

    static float Perdio(int i)
    {
        return vidas[i] - puestos[i].VidaActual;
    }

    static void Despejar()
    {
        EnemyController.DespejarAlrededor(Vector3.zero, 500f);
        puestos.Clear();
        vidas.Clear();
    }

    // Una bala del arma por el camino de siempre (con las cartas), derecho hacia los zombis.
    static BulletController Tirar(GunController arma, float dano)
    {
        arma.FijarDanoPorBala(dano);
        Vector3 salida = centro + hacia * 1.2f + Vector3.up * (arma.firePoint != null ? arma.firePoint.position.y - centro.y : 1f);
        return arma.SacarBala(salida, Quaternion.LookRotation(hacia), 0f, false);
    }

    static int Puntos()
    {
        return Puntaje.instance != null ? Puntaje.instance.contadorKill : 0;
    }

    static List<BulletController> BalasPrendidas()
    {
        return new List<BulletController>(Object.FindObjectsByType<BulletController>(FindObjectsSortMode.None));
    }

    static void Avanzar()
    {
        var vida = PlayerHealth.instance;
        if (vida == null) return;
        var control = vida.GetComponent<PlayerController>();
        var cuerpo = vida.GetComponent<Rigidbody>();
        var arma = control != null ? control.theGun : null;
        if (control == null || cuerpo == null || arma == null) { Terminar("el jugador no tiene control, cuerpo o arma"); return; }

        cuadrosMirados++;
        if (EnemyController.Vivos.Count != EnemyController.ZombisVivos) cuadrosConLaListaMal++;
        float t = Time.time - desde;
        if (paso != Paso.Vampiro && paso != Paso.Escudo) vida.health = vida.maxHealth;

        switch (paso)
        {
            case Paso.Preparar:
            {
                if (t < 1f) return;
                var oleadas = Object.FindAnyObjectByType<WaveManager>();
                if (oleadas != null) { oleadas.StopAllCoroutines(); oleadas.enabled = false; }
                foreach (var cajas in Object.FindObjectsByType<PowerUp>(FindObjectsSortMode.None)) cajas.enabled = false;
                EnemyController.DespejarAlrededor(Vector3.zero, 500f);
                centro = new Vector3(0f, cuerpo.position.y, -6f);
                cuerpo.position = centro;
                cuerpo.linearVelocity = Vector3.zero;
                control.transform.position = centro;
                hacia = Vector3.right;
                arma.FijarFuria(1f, 1f);
                Pasar(Paso.Perforante);
                return;
            }

            case Paso.Perforante:
                if (sub == 0)
                {
                    if (t < 0.5f) return;
                    Poner(PoderDeLaPartida.Perforante, 1);
                    PonerZombi(PrefabTanque, 7f, 0f);
                    PonerZombi(PrefabTanque, 9.5f, 0f);
                    PonerZombi(PrefabTanque, 12f, 0f);
                    Tirar(arma, 4f);
                    Sub();
                    return;
                }
                if (t < 1.2f) return;
                notas.AppendLine("Perforante: perdieron " + Perdio(0).ToString("0.##") + ", " + Perdio(1).ToString("0.##") + " y " + Perdio(2).ToString("0.##"));
                Anotar(Mathf.Abs(Perdio(0) - 4f) < 1e-3f, "perforante: el primero pierde el daño entero (4)");
                Anotar(Mathf.Abs(Perdio(1) - 2f) < 1e-3f, "perforante: el segundo, la mitad (2), una sola vez");
                Anotar(Perdio(2) < 1e-3f, "perforante: con una carta no llega al tercero");
                Despejar();
                Pasar(Paso.Explosiva);
                return;

            case Paso.Explosiva:
                if (sub == 0)
                {
                    if (t < 1.5f) return;   // que se vayan las balas de antes
                    Poner(PoderDeLaPartida.Explosiva, 1);
                    PonerZombi(PrefabTanque, 7f, 0f);
                    PonerZombi(PrefabTanque, 7f, 1.2f);
                    PonerZombi(PrefabTanque, 7f, 3f);
                    Tirar(arma, 10f);
                    Sub();
                    return;
                }
                if (t < 1.2f) return;
                notas.AppendLine("Explosiva: perdieron " + Perdio(0).ToString("0.##") + ", " + Perdio(1).ToString("0.##") + " y " + Perdio(2).ToString("0.##"));
                Anotar(Mathf.Abs(Perdio(0) - 10f) < 1e-3f, "explosiva: el tocado pierde el daño entero (10), no dos veces");
                Anotar(Mathf.Abs(Perdio(1) - 3f) < 1e-3f, "explosiva: el de al lado, el 30 % (3)");
                Anotar(Perdio(2) < 1e-3f, "explosiva: el de lejos, nada");
                Despejar();
                Pasar(Paso.SinDobles);
                return;

            case Paso.SinDobles:
                if (sub == 0)
                {
                    if (t < 1.5f) return;
                    Poner(PoderDeLaPartida.Perforante, 2, PoderDeLaPartida.Explosiva, 1);
                    puntosAntes = Puntos();
                    PonerZombi(PrefabNormal, 7f, 0f);
                    PonerZombi(PrefabNormal, 8.2f, 0f);
                    PonerZombi(PrefabNormal, 9.4f, 0f);
                    valorAntes = puestos[0].enemyType.puntos * 3;
                    Tirar(arma, 100f);
                    Sub();
                    return;
                }
                if (t < 1.2f) return;
                {
                    int muertos = 0;
                    foreach (var z in puestos) if (!z.Vivo) muertos++;
                    int sumados = Puntos() - puntosAntes;
                    notas.AppendLine("Sin muertes dobles: murieron " + muertos + " y se sumaron " + sumados + " puntos (esperados " + valorAntes + ")");
                    Anotar(muertos == 3, "perforante y explosiva juntas matan a los tres");
                    Anotar(sumados == valorAntes, "y cada uno suma sus puntos una sola vez");
                }
                Despejar();
                Pasar(Paso.Abanico);
                return;

            case Paso.Abanico:
            {
                if (t < 1.5f) return;
                var disparar = typeof(GunController).GetMethod("Disparar", BindingFlags.Instance | BindingFlags.NonPublic);
                if (disparar == null) { Terminar("no se encontro GunController.Disparar"); return; }
                control.cantBalas = Mathf.Max(control.cantBalas, 100);
                // Con la pistola: el abanico es de la carta, no del arma (con 4 de daño saldria la
                // escopeta, con sus perdigones).
                arma.FijarDanoPorBala(1f);
                Poner(PoderDeLaPartida.DisparoDoble, 1, PoderDeLaPartida.HaciaAtras, 1);
                var antes = new HashSet<BulletController>(BalasPrendidas());
                tirosAntes = arma.TirosDisparados;
                balasAntes = control.cantBalas;
                disparar.Invoke(arma, new object[] { 0f });
                var nuevas = BalasPrendidas().FindAll(b => !antes.Contains(b));
                Vector3 frente = arma.firePoint.forward;
                frente.y = 0f;
                var angulos = new List<float>();
                foreach (var b in nuevas)
                {
                    Vector3 f = b.transform.forward;
                    f.y = 0f;
                    // Para atras da -180 o 180 segun el redondeo: va primera siempre.
                    float girado = Vector3.SignedAngle(frente, f, Vector3.up);
                    angulos.Add(Mathf.Abs(girado) > 179f ? -180f : girado);
                }
                angulos.Sort();
                notas.AppendLine("Disparo doble y hacia atras: " + nuevas.Count + " balas, a " + string.Join(", ", angulos.ConvertAll(a => a.ToString("0.#"))) + " grados");
                Anotar(nuevas.Count == 3, "disparo doble y hacia atras: tres balas de un tiro");
                Anotar(angulos.Count == 3 && Mathf.Abs(Mathf.Abs(angulos[0]) - 180f) < 1f && Mathf.Abs(angulos[1] + 4f) < 0.5f && Mathf.Abs(angulos[2] - 4f) < 0.5f,
                       "las de adelante a ±4° y la otra para atras");
                Anotar(arma.TirosDisparados == tirosAntes + 1 && control.cantBalas == balasAntes - 1,
                       "las de mas no gastan cargador ni cuentan como tiros");
                Poner(PoderDeLaPartida.DisparoDoble, 2);
                antes = new HashSet<BulletController>(BalasPrendidas());
                disparar.Invoke(arma, new object[] { 0f });
                nuevas = BalasPrendidas().FindAll(b => !antes.Contains(b));
                angulos.Clear();
                foreach (var b in nuevas)
                {
                    Vector3 f = b.transform.forward;
                    f.y = 0f;
                    // Para atras da -180 o 180 segun el redondeo: va primera siempre.
                    float girado = Vector3.SignedAngle(frente, f, Vector3.up);
                    angulos.Add(Mathf.Abs(girado) > 179f ? -180f : girado);
                }
                angulos.Sort();
                Anotar(angulos.Count == 3 && Mathf.Abs(angulos[0] + 8f) < 0.5f && Mathf.Abs(angulos[1]) < 0.5f && Mathf.Abs(angulos[2] - 8f) < 0.5f,
                       "con dos, tres balas a -8°, 0° y 8° (" + string.Join(", ", angulos.ConvertAll(a => a.ToString("0.#"))) + ")");
                Pasar(Paso.Hielo);
                return;
            }

            case Paso.Hielo:
                if (sub == 0)
                {
                    if (t < 1.5f) return;
                    Poner(PoderDeLaPartida.Hielo, 1);
                    PonerZombi(PrefabNormal, 12f, 0f, false);
                    Tirar(arma, 1f);
                    Sub();
                    return;
                }
                if (sub == 1)
                {
                    if (!puestos[0].Congelado) { if (t > 2f) { Anotar(false, "hielo: la bala lo congela"); Despejar(); Pasar(Paso.Quemadura); } return; }
                    Anotar(Mathf.Abs(puestos[0].LentitudActual - 0.4f) < 1e-3f, "hielo: lo congela un 40 %");
                    Anotar(TienePropiedades(puestos[0]), "hielo: y lo pinta");
                    Sub();
                    return;
                }
                if (sub == 2)
                {
                    if (t < 0.3f) return;
                    posA = puestos[0].transform.position;
                    marca = Time.time;
                    Sub();
                    return;
                }
                if (sub == 3)
                {
                    if (t < 0.5f) return;
                    float v = Plano(puestos[0].transform.position - posA).magnitude / (Time.time - marca);
                    float esperado = puestos[0].enemyType.velocidad * 0.6f;
                    notas.AppendLine("Hielo: congelado camina a " + v.ToString("0.00") + " m/s (esperado " + esperado.ToString("0.00") + ")");
                    Anotar(Mathf.Abs(v - esperado) < 0.8f, "hielo: congelado camina al 60 %");
                    Sub();
                    return;
                }
                if (sub == 4)
                {
                    // A los 2 s del golpe se le va: un margen y se mide de nuevo.
                    if (t < 1.4f) return;
                    Anotar(!puestos[0].Congelado && !TienePropiedades(puestos[0]), "hielo: a los 2 s se le va, y el color tambien");
                    // Ya llego cerca del jugador, donde frena para pegar: se lo aleja para medir.
                    var lejos = centro + hacia * 12f;
                    lejos.y = puestos[0].transform.position.y;
                    puestos[0].GetComponent<Rigidbody>().position = lejos;
                    puestos[0].transform.position = lejos;
                    Sub();
                    return;
                }
                if (sub == 5)
                {
                    if (t < 0.2f) return;
                    posA = puestos[0].transform.position;
                    marca = Time.time;
                    Sub();
                    return;
                }
                if (t < 0.3f) return;
                {
                    float v = Plano(puestos[0].transform.position - posA).magnitude / (Time.time - marca);
                    notas.AppendLine("Hielo: despues camina a " + v.ToString("0.00") + " m/s");
                    Anotar(v > puestos[0].enemyType.velocidad * 0.8f, "hielo: y vuelve a caminar entero");
                }
                Despejar();
                Pasar(Paso.Quemadura);
                return;

            case Paso.Quemadura:
                if (sub == 0)
                {
                    if (t < 1.5f) return;
                    Poner(PoderDeLaPartida.Quemadura, 2);
                    valorAntes = 0;   // aca, las balas que se tiraron para prenderlo
                    // Al cielo: solo se cuenta a cuantas les toca quemar.
                    int quemadoras = 0;
                    arma.FijarDanoPorBala(1f);
                    for (int i = 0; i < 200; i++)
                        if (arma.SacarBala(centro + Vector3.up * 3f, Quaternion.LookRotation(-hacia), 0f, false).quemaduraPorSegundo > 0f) quemadoras++;
                    notas.AppendLine("Quemadura: con dos cartas quemaron " + quemadoras + " de 200 balas");
                    Anotar(quemadoras > 70 && quemadoras < 130, "quemadura: con dos, quema la mitad de las balas");
                    PonerZombi(PrefabTanque, 8f, 0f);
                    Sub();
                    return;
                }
                if (sub == 1)
                {
                    // De a una, hasta que una lo prenda: la bala tarda mas de medio segundo en
                    // llegar, y una que llegara despues de anotar la vida sumaria su golpe. La
                    // vida se anota en el cuadro en que se prende, antes del primer tick.
                    if (puestos[0].Quemandose)
                    {
                        vidas[0] = puestos[0].VidaActual;
                        Sub();
                        return;
                    }
                    if (t < 1.2f) return;
                    if (++valorAntes > 30) { Anotar(false, "quemadura: alguna bala lo prende"); Despejar(); Pasar(Paso.Vampiro); return; }
                    // De a 2: el tanque (25) aguanta hasta que una lo prenda.
                    Tirar(arma, 2f);
                    desde = Time.time;
                    return;
                }
                if (puestos[0].Quemandose && t < 4f) return;
                notas.AppendLine("Quemadura: mientras se quemo perdio " + Perdio(0).ToString("0.###"));
                Anotar(Mathf.Abs(Perdio(0) - 0.6f) < 1e-3f, "quemadura: saca el 10 % del daño por segundo durante 3 s (0,6 con balas de 2)");
                Despejar();
                Pasar(Paso.Vampiro);
                return;

            case Paso.Vampiro:
            {
                if (t < 1.5f) return;
                Poner(PoderDeLaPartida.Vampiro, 1);
                vida.health = vida.maxHealth / 2;
                int mitad = vida.health;
                curasAntes = vida.CurasDelVampiro;
                for (int i = 0; i < PoderDeLaPartida.BajasParaElVampiro - 1; i++)
                    PonerZombi(PrefabNormal, 9f + i * 0.2f, 4f).DanoZombi(1e9f);
                Anotar(vida.health == mitad && vida.CurasDelVampiro == curasAntes,
                       "vampiro: antes de " + PoderDeLaPartida.BajasParaElVampiro + " muertes no cura");
                PonerZombi(PrefabNormal, 9f, -4f).DanoZombi(1e9f);
                int esperada = Mathf.Min(vida.maxHealth, mitad + Mathf.Max(1, Mathf.RoundToInt(vida.maxHealth * 0.03f)));
                notas.AppendLine("Vampiro: de " + mitad + " a " + vida.health + " (esperado " + esperada + ")");
                Anotar(vida.health == esperada && vida.CurasDelVampiro == curasAntes + 1,
                       "vampiro: a las " + PoderDeLaPartida.BajasParaElVampiro + " cura el 3 % de la vida maxima");
                vida.health = vida.maxHealth;
                Despejar();
                Pasar(Paso.Escudo);
                return;
            }

            case Paso.Escudo:
                if (sub == 0)
                {
                    if (t < 1.5f) return;
                    vida.health = vida.maxHealth;
                    Poner(PoderDeLaPartida.Escudo, 1);
                    Sub();
                    return;
                }
                if (sub == 1)
                {
                    if (t < 0.3f) return;
                    var cartas = vida.GetComponent<CartasEnElJugador>();
                    Anotar(vida.EscudoListo && cartas != null && cartas.AnilloVisible, "escudo: listo, con el anillo en el piso");
                    vida.health = vida.maxHealth;
                    var pegado = PonerZombi(PrefabNormal, 0.9f, 0f, false);
                    distanciaAntes = Plano(pegado.transform.position - vida.transform.position).magnitude;
                    bloqueadosAntes = vida.GolpesBloqueados;
                    golpesAntes = vida.GolpesRecibidos;
                    vida.TakeDamage(10f);
                    float distanciaDespues = Plano(pegado.transform.position - vida.transform.position).magnitude;
                    notas.AppendLine("Escudo: el pegado paso de " + distanciaAntes.ToString("0.00") + " a " + distanciaDespues.ToString("0.00") + " m");
                    Anotar(vida.health == vida.maxHealth && vida.GolpesBloqueados == bloqueadosAntes + 1, "escudo: para el golpe entero");
                    Anotar(vida.GolpesRecibidos == golpesAntes, "escudo: y no cuenta como golpe (la oleada sigue perfecta)");
                    Anotar(distanciaDespues - distanciaAntes > 0.4f, "escudo: al romperse corre al zombi pegado");
                    Anotar(!vida.EscudoListo, "escudo: y queda recargando");
                    vida.TakeDamage(10f);
                    Anotar(vida.health == vida.maxHealth - 10, "escudo: el golpe siguiente entra");
                    Despejar();
                    Sub();
                    return;
                }
                if (t < 0.3f) return;
                {
                    var cartas = vida.GetComponent<CartasEnElJugador>();
                    Anotar(cartas != null && !cartas.AnilloVisible, "escudo: recargando no hay anillo");
                }
                vida.health = vida.maxHealth;
                Pasar(Paso.Dron);
                return;

            case Paso.Dron:
            {
                var cartas = vida.GetComponent<CartasEnElJugador>();
                if (cartas == null) { Terminar("el jugador no tiene CartasEnElJugador"); return; }
                if (sub == 0)
                {
                    if (t < 1.5f) return;
                    arma.FijarDanoPorBala(1f);
                    Poner(PoderDeLaPartida.Dron, 2);
                    PonerZombi(PrefabTanque, 6f, 0f);
                    tirosAntes = cartas.TirosDeLosDrones;
                    Sub();
                    return;
                }
                if (sub == 1)
                {
                    if (t < 0.3f) return;
                    Anotar(cartas.DronesPuestos == 2, "dron: con dos cartas hay dos drones");
                    Sub();
                    return;
                }
                if (sub == 2)
                {
                    if (t < 3f) return;
                    int tiros = cartas.TirosDeLosDrones - tirosAntes;
                    notas.AppendLine("Dron: " + tiros + " tiros en 3 s; el tanque perdio " + Perdio(0).ToString("0.##"));
                    Anotar(tiros >= 4, "dron: tiran solos, una vez por segundo cada uno");
                    Anotar(Perdio(0) >= 3f, "dron: y le pegan al que se ve");
                    Poner();
                    Despejar();
                    Sub();
                    return;
                }
                if (t < 0.3f) return;
                Anotar(cartas.DronesPuestos == 0, "dron: sin la carta no hay drones");
                Terminar(null);
                return;
            }
        }
    }

    static Vector3 Plano(Vector3 v)
    {
        v.y = 0f;
        return v;
    }

    static bool TienePropiedades(EnemyController zombi)
    {
        foreach (var r in zombi.GetComponentsInChildren<Renderer>())
            if (r.enabled && r.HasPropertyBlock()) return true;
        return false;
    }

    static void Terminar(string motivo)
    {
        if (terminado) return;
        terminado = true;
        if (error == null) error = motivo;
        Anotar(cuadrosMirados > 0 && cuadrosConLaListaMal == 0,
               "la lista de vivos coincide con la cuenta en cada cuadro (" + cuadrosMirados + " cuadros, " + cuadrosConLaListaMal + " mal)");

        var inf = new StringBuilder();
        inf.AppendLine("Prueba de las cartas de reglas (WaveMode sin oleadas, el jugador quieto en el centro)");
        inf.AppendLine();
        if (error != null) inf.AppendLine("ERROR: " + error);
        inf.Append(notas);
        inf.AppendLine();
        bool todo = error == null;
        for (int i = 0; i < oks.Count; i++)
        {
            inf.AppendLine((oks[i] ? "OK  " : "FALLA  ") + ques[i]);
            todo &= oks[i];
        }
        inf.AppendLine();
        inf.AppendLine("RESULTADO: " + (todo ? "TODO OK" : "HAY FALLAS"));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(Ruta)));
        File.WriteAllText(Ruta, inf.ToString());
        SessionState.SetBool(Clave, false);
        EditorApplication.ExitPlaymode();
        Debug.Log(inf.ToString());
    }
}
