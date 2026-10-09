using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

// Banco del arrastre, en play: un zombi pegado al jugador no tiene que irse con el. Lo dijeron
// en Discord (9/10): "cuando te estas moviendo, hay una posibilidad de que te lleves un zombi para
// la direccion que estas yendo", y "no te lo podes sacar de encima, mas los rapidos azules". El
// jugador pesa 10 y los zombis livianos 1 (el normal) y 0,5 (el rapido y el veloz): corriendo
// contra uno lo atropellaba y lo llevaba por delante, porque el zombi volvia a caminar hacia el
// en cada paso de fisica y quedaba en su camino. Ivan eligio que se aparten de costado, cada uno
// con su peso: el liviano sale despedido, el tanque casi no se mueve.
//
// En WaveMode, con las oleadas paradas y el mapa despejado: el jugador en el centro, un zombi
// pegado de frente (un poco corrido, como pasa jugando) o de costado, y el joystick de moverse
// empujando hacia el este un segundo de juego. Se mide cuanto avanzo cada uno hacia el este,
// cuanto se corrio el zombi de costado (lo mas lejos de su linea: despues vuelve a perseguirlo),
// si el jugador lo paso y cuantos pasos de fisica se aparto (EnemyController.PasosApartado).
// Escribe Builds/prueba_arrastre.txt.
[InitializeOnLoad]
public static class PruebaArrastre
{
    const string Clave = "ShowBies.PruebaArrastre";
    const string Banco = "PruebaArrastre";
    const string Ruta = "../Builds/prueba_arrastre.txt";
    const string Escena = "Assets/Escenas/WaveMode.unity";
    const float Empujando = 1f;      // segundos de juego con el joystick apretado
    const double TopeTotal = 60.0;

    class Caso
    {
        public string zombi;
        public bool deFrente;
        // Lo medido.
        public float avanceZombi, costadoZombi, avanceJugador, velocidadZombi;
        public bool hecho, loPaso;
        public int apartado;
    }

    static readonly List<Caso> casos = new List<Caso>
    {
        new Caso { zombi = "Zombi", deFrente = true },
        new Caso { zombi = "ZombiFASTER", deFrente = true },
        new Caso { zombi = "ZombiTanque", deFrente = true },
        new Caso { zombi = "Zombi", deFrente = false },
        new Caso { zombi = "ZombiRapido", deFrente = false },
        new Caso { zombi = "ZombiTanque", deFrente = false },
    };

    enum Fase { Preparar, Esperar, Empujar, Limpiar }

    static int indice;
    static Fase fase;
    static float desde;
    static double inicio;
    static bool empezo, terminado;
    static EnemyController zombi;
    static Vector3 zombiAntes, jugadorAntes;
    static int apartadoAntes;
    static string error;

    static PruebaArrastre()
    {
        EditorApplication.update += Tick;
    }

    [MenuItem("ShowBies/Pruebas/Arrastre de zombis (play)")]
    // Publico para correrlo por codigo (ver la trampa del registro de menus).
    public static void Arrancar()
    {
        if (!RespaldoDelBanco.PuedeArrancar(Banco)) return;
        RespaldoDelBanco.Guardar(Banco);
        PruebaDisparo.ModoTelefono(Clave);
        PlayerSettings.runInBackground = true;
        Progreso.GuardarOleadaEnCurso(1, 0);
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
            indice = 0;
            fase = Fase.Preparar;
            desde = Time.time;
            inicio = EditorApplication.timeSinceStartup;
            foreach (var c in casos) c.hecho = false;
            return;
        }
        if (EditorApplication.timeSinceStartup - inicio > TopeTotal) { Terminar("se paso del tope de " + TopeTotal + " s"); return; }

        try
        {
            Avanzar();
        }
        catch (System.Exception e)
        {
            Terminar("excepcion: " + e);
        }
    }

    static void Avanzar()
    {
        var vida = PlayerHealth.instance;
        if (vida == null || EventSystem.current == null) return;
        vida.health = vida.maxHealth;
        var control = vida.GetComponent<PlayerController>();
        var joysticks = vida.GetComponent<PlayerJS>();
        var cuerpo = vida.GetComponent<Rigidbody>();
        if (control == null || joysticks == null || cuerpo == null) { Terminar("el jugador no tiene control, joysticks o cuerpo"); return; }

        switch (fase)
        {
            case Fase.Preparar:
            {
                if (Time.time - desde < 1f) return;
                // Sin oleadas, sin cajas y sin nadie en el mapa.
                var oleadas = Object.FindAnyObjectByType<WaveManager>();
                if (oleadas != null) { oleadas.StopAllCoroutines(); oleadas.enabled = false; }
                foreach (var cajas in Object.FindObjectsByType<PowerUp>(FindObjectsSortMode.None)) cajas.enabled = false;
                EnemyController.DespejarAlrededor(Vector3.zero, 500f);
                if (indice >= casos.Count) { Terminar(null); return; }

                var caso = casos[indice];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Personajes/" + caso.zombi + ".prefab");
                if (prefab == null) { Terminar("no esta el prefab " + caso.zombi); return; }
                PruebaDisparo.Soltar(joysticks.moveJoystick);
                Vector3 centro = new Vector3(0f, cuerpo.position.y, -6f);
                cuerpo.position = centro;
                cuerpo.linearVelocity = Vector3.zero;
                control.transform.position = centro;

                float radioJugador = RadioDe(vida.gameObject);
                float radioZombi = EnemyController.RadioDelCuerpo(prefab);
                float pegado = radioJugador + radioZombi - 0.03f;
                // De frente, un poco corrido hacia el norte (de frente justo no pasa jugando); de
                // costado, al norte.
                Vector3 donde = caso.deFrente ? new Vector3(pegado * 0.98f, 0f, pegado * 0.2f) : new Vector3(0f, 0f, pegado);
                zombi = EnemyController.Aparecer(prefab, new Vector3(centro.x, 0f, centro.z) + donde);
                if (zombi == null) { Terminar("no aparecio el " + caso.zombi); return; }
                caso.velocidadZombi = zombi.enemyType != null ? zombi.enemyType.velocidad : 0f;
                fase = Fase.Esperar;
                desde = Time.time;
                return;
            }

            case Fase.Esperar:
                // Que llegue a tocarlo.
                if (Time.time - desde < 0.15f) return;
                zombiAntes = zombi.transform.position;
                jugadorAntes = cuerpo.position;
                apartadoAntes = EnemyController.PasosApartado;
                casos[indice].costadoZombi = 0f;
                casos[indice].loPaso = false;
                PruebaDisparo.Apretar(joysticks.moveJoystick, Vector2.right);
                fase = Fase.Empujar;
                desde = Time.time;
                return;

            case Fase.Empujar:
            {
                PruebaDisparo.Apretar(joysticks.moveJoystick, Vector2.right);
                var caso = casos[indice];
                caso.costadoZombi = Mathf.Max(caso.costadoZombi, Mathf.Abs(zombi.transform.position.z - zombiAntes.z));
                if (zombi.transform.position.x < cuerpo.position.x) caso.loPaso = true;
                if (Time.time - desde < Empujando) return;
                Vector3 z = zombi.transform.position - zombiAntes;
                Vector3 j = cuerpo.position - jugadorAntes;
                caso.avanceZombi = z.x;
                caso.avanceJugador = j.x;
                caso.apartado = EnemyController.PasosApartado - apartadoAntes;
                caso.hecho = true;
                PruebaDisparo.Soltar(joysticks.moveJoystick);
                EnemyController.DespejarAlrededor(Vector3.zero, 500f);
                indice++;
                fase = Fase.Limpiar;
                desde = Time.time;
                return;
            }

            case Fase.Limpiar:
                if (Time.time - desde < 0.3f) return;
                fase = Fase.Preparar;
                desde = Time.time - 1f;
                return;
        }
    }

    static float RadioDe(GameObject go)
    {
        var capsula = go.GetComponent<CapsuleCollider>();
        if (capsula == null) return 0.5f;
        Vector3 e = go.transform.lossyScale;
        return capsula.radius * Mathf.Max(Mathf.Abs(e.x), Mathf.Abs(e.z));
    }

    static void Terminar(string motivo)
    {
        if (terminado) return;
        terminado = true;
        if (error == null) error = motivo;

        var inf = new StringBuilder();
        inf.AppendLine("Prueba del arrastre: el jugador corre " + Empujando + " s hacia el este con un zombi pegado (WaveMode, sin oleadas)");
        inf.AppendLine();
        if (error != null) inf.AppendLine("ERROR: " + error);
        var oks = new List<KeyValuePair<string, bool>>();
        foreach (var c in casos)
        {
            string donde = c.deFrente ? "de frente" : "de costado";
            inf.AppendLine(c.zombi + " " + donde + ": el zombi avanzo " + c.avanceZombi.ToString("0.00") + " m y se corrio hasta " + c.costadoZombi.ToString("0.00")
                           + " m de costado; el jugador avanzo " + c.avanceJugador.ToString("0.00") + " m (el zombi camina a " + c.velocidadZombi + "); lo paso "
                           + c.loPaso + "; pasos apartandose " + c.apartado);
            if (!c.hecho) { oks.Add(new KeyValuePair<string, bool>(c.zombi + " " + donde + ": se midio", false)); continue; }
            bool pesado = c.zombi == "ZombiTanque";
            if (c.deFrente && !pesado)
            {
                oks.Add(new KeyValuePair<string, bool>(c.zombi + " de frente: no se lo lleva por delante (el jugador lo pasa)", c.loPaso));
                oks.Add(new KeyValuePair<string, bool>(c.zombi + " de frente: sale de costado (mas de 0,5 m)", c.costadoZombi > 0.5f && c.apartado > 0));
                oks.Add(new KeyValuePair<string, bool>(c.zombi + " de frente: el jugador sigue corriendo (mas de 8 m)", c.avanceJugador > 8f));
            }
            else if (c.deFrente)
            {
                oks.Add(new KeyValuePair<string, bool>(c.zombi + " de frente: no se corre (no se aparta) y cede poco (menos de 2,5 m; antes, 12,7)", c.apartado == 0 && c.avanceZombi < 2.5f));
                oks.Add(new KeyValuePair<string, bool>(c.zombi + " de frente: el jugador lo rodea (lo pasa)", c.loPaso));
            }
            else
            {
                oks.Add(new KeyValuePair<string, bool>(c.zombi + " de costado: no lo arrastra (avanza a lo sumo lo que camina)", c.avanceZombi < c.velocidadZombi * Empujando + 0.5f));
            }
        }
        inf.AppendLine();
        bool todo = error == null;
        if (error == null) inf.AppendLine("OK  el banco llego hasta el final");
        else inf.AppendLine("FALLA  el banco llego hasta el final");
        foreach (var ok in oks)
        {
            inf.AppendLine((ok.Value ? "OK  " : "FALLA  ") + ok.Key);
            todo &= ok.Value;
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
