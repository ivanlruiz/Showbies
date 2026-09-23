using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Arma el Animator Controller de los zombis con los clips que ya trae
// ToonyTinyPeople (y, mas abajo, el del jugador: ArmarJugador). Hasta el 22/9 el controller tenia UN solo estado (Z_run_rm en
// loop, sin transiciones) y un parametro "runZM" que no usaba nadie: los cinco
// zombis corrian para siempre, te pegaban corriendo y se morian corriendo, aunque
// Z_attack_A y Z_death_A estaban en el proyecto sin usar desde el principio.
//
// Se arma desde el menu y no a mano para poder volver a correrlo: los .controller
// son YAML grande y editarlos a mano es como editar una escena a mano.
//
// El controller vive en Assets/Animaciones/ y no en la carpeta del pack, por lo
// mismo que la fuente Bangers se saco de los ejemplos de TextMesh Pro: lo que es
// del juego no va en una carpeta de terceros, que una reimportacion del pack pisa.
// Se mueve con AssetDatabase.MoveAsset, que conserva el guid, asi que los cinco
// prefabs (que lo pisan con un override de m_Controller) siguen apuntando solos.
public static class ConstructorAnimaciones
{
    const string Carpeta = "Assets/Animaciones";
    const string RutaEnElPack = "Assets/ToonyTinyPeople/TT_demo/animation/zombie/Zombi.controller";
    public const string RutaControlador = Carpeta + "/Zombi.controller";

    const string ClipsZombi = "Assets/ToonyTinyPeople/TT_demo/animation/zombie/";

    // Los nombres los comparte EnemyController: si cambian, cambian en los dos lados.
    public const string ParametroPaso = "Paso";
    public const string ParametroRitmo = "Ritmo";
    public const string GatilloMorir = "Morir";
    // El punto del clip de atacar en el festejo (ver el estado Festejar).
    public const string ParametroFestejo = "Festejo";

    // Ver el estado Morir, mas abajo.
    const float VelocidadDeLaMuerte = 1.35f;

    [MenuItem("ShowBies/Animaciones/Armar el controller de los zombis")]
    public static void Armar()
    {
        if (!AssetDatabase.IsValidFolder(Carpeta))
            AssetDatabase.CreateFolder("Assets", "Animaciones");

        // Sacarlo del pack la primera vez. MoveAsset conserva el guid: los prefabs
        // no se enteran.
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaControlador) == null &&
            AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaEnElPack) != null)
        {
            string error = AssetDatabase.MoveAsset(RutaEnElPack, RutaControlador);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError("No se pudo mover el controller de los zombis: " + error);
                return;
            }
            Debug.Log("Zombi.controller salio de la carpeta del pack a " + Carpeta + " (mismo guid).");
        }

        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaControlador);
        if (ctrl == null)
        {
            Debug.LogError("No esta el controller de los zombis en " + RutaControlador);
            return;
        }

        AnimationClip correr = Clip("Z_run_rm"), caminar = Clip("Z_walk_rm");
        AnimationClip atacar = Clip("Z_attack_A"), morir = Clip("Z_death_A");
        if (correr == null || caminar == null || atacar == null || morir == null)
        {
            Debug.LogError("Faltan clips del zombi en " + ClipsZombi);
            return;
        }

        // De cero: asi correrlo de nuevo da siempre lo mismo, sin estados viejos
        // colgados ni transiciones duplicadas.
        var capas = ctrl.layers;
        capas[0].defaultWeight = 1f;
        ctrl.layers = capas;

        var maquina = ctrl.layers[0].stateMachine;
        foreach (var t in new List<AnimatorStateTransition>(maquina.anyStateTransitions))
            maquina.RemoveAnyStateTransition(t);
        foreach (var s in new List<ChildAnimatorState>(maquina.states))
            maquina.RemoveState(s.state);
        foreach (var p in new List<AnimatorControllerParameter>(ctrl.parameters))
            ctrl.RemoveParameter(p);

        // El blend tree es un sub-asset del .controller y quitar su estado no lo
        // borra: sin esto, cada vez que se corre esta herramienta queda uno colgado
        // adentro del archivo.
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(RutaControlador))
            if (o is BlendTree) AssetDatabase.RemoveObjectFromAsset(o);

        // "Paso" es lo que antes era animador.speed: cada tipo camina a su ritmo (el
        // tanque pesado, el FASTER frenetico). Va como multiplicador del estado de
        // correr y no del Animator entero, porque con el Animator entero el jefe
        // (0,3) tardaba cuatro segundos y medio en morirse y el tanque pegaba en
        // camara lenta. Atacar y morir van siempre a velocidad 1.
        ctrl.AddParameter(ParametroPaso, AnimatorControllerParameterType.Float);
        // 0 es caminar y 1 correr. Arranca en 1: lo que hacian todos hasta ahora.
        ctrl.AddParameter(ParametroRitmo, AnimatorControllerParameterType.Float);
        var parametros = ctrl.parameters;
        parametros[0].defaultFloat = 1f;
        parametros[1].defaultFloat = 1f;
        ctrl.parameters = parametros;
        ctrl.AddParameter(GatilloMorir, AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter(ParametroFestejo, AnimatorControllerParameterType.Float);

        // Andar es un blend de caminar a correr, y no dos estados con su transicion:
        // asi el volver-de-atacar y el sale-de-cualquier-estado-a-morir siguen siendo
        // uno solo, y el tipo que va entre los dos ritmos se mezcla en vez de saltar.
        //
        // Antes era un unico Z_run_rm con el Paso bajado, y el tanque (0,45) y el
        // jefe (0,3) se veian como alguien corriendo en camara lenta, no como algo
        // pesado: el ciclo de correr tiene los dos pies en el aire y a esa velocidad
        // eso se lee como que el video va lento.
        BlendTree mezcla;
        var eAndar = ctrl.CreateBlendTreeInController("Andar", out mezcla, 0);
        mezcla.blendParameter = ParametroRitmo;
        mezcla.useAutomaticThresholds = false;
        mezcla.AddChild(caminar, 0f);
        mezcla.AddChild(correr, 1f);
        eAndar.speedParameterActive = true;
        eAndar.speedParameter = ParametroPaso;
        maquina.defaultState = eAndar;

        // CreateBlendTreeInController agrega un parametro "Blend" suyo si el arbol no
        // tenia otro; ya no lo usa nadie.
        foreach (var p in new List<AnimatorControllerParameter>(ctrl.parameters))
            if (p.name == "Blend") ctrl.RemoveParameter(p);

        // Lo deja donde caiga; que quede alineado con los otros dos.
        var estados = maquina.states;
        for (int i = 0; i < estados.Length; i++)
            if (estados[i].state == eAndar) estados[i].position = new Vector3(260, 0, 0);
        maquina.states = estados;

        var eAtacar = maquina.AddState("Atacar", new Vector3(520, 90, 0));
        eAtacar.motion = atacar;

        var eMorir = maquina.AddState("Morir", new Vector3(520, -90, 0));
        eMorir.motion = morir;
        // El clip son 1,83 s de desplome y el ultimo tercio es el cuerpo
        // acomodandose en el piso: acelerado entra entero en los 1,4 s que
        // EnemyController deja al cadaver, que es lo que se le puede pedir a un
        // telefono cuando una granada mata a diez de una.
        eMorir.speed = VelocidadDeLaMuerte;

        // El festejo, cuando el jugador muere: el clip de atacar con el tiempo manejado
        // a mano desde EnemyController (Motion Time), para quedarse en el tramo en que
        // la mano derecha sube por encima de la cabeza y bajar al hombro, como un puño
        // en alto. Tampoco se entra por transicion: lo arranca el codigo.
        var eFestejar = maquina.AddState("Festejar", new Vector3(780, 0, 0));
        eFestejar.motion = atacar;
        eFestejar.timeParameterActive = true;
        eFestejar.timeParameter = ParametroFestejo;

        // A Atacar no se entra por una transicion: lo arranca EnemyController con
        // CrossFadeInFixedTime, adelantado dentro del clip para que la mano llegue
        // adelante justo cuando entra el daño. Con un gatillo el clip empezaba
        // siempre en su cuadro 0, y el zarpazo conectaba 0,37 s despues del daño.
        // Por lo mismo no hay parametro Atacar.

        // Vuelve a andar antes de que termine del todo: el ultimo tramo del clip es
        // el brazo bajando y se mezcla bien con el paso.
        var volverAAndar = eAtacar.AddTransition(eAndar);
        volverAAndar.hasExitTime = true;
        volverAAndar.exitTime = 0.82f;
        volverAAndar.hasFixedDuration = true;
        volverAAndar.duration = 0.15f;

        // Morir no vuelve de ningun lado: el cadaver se apaga por su cuenta. Y no
        // puede interrumpirse a si mismo, o dos balas en el mismo paso de fisica lo
        // harian empezar dos veces.
        var aMorir = maquina.AddAnyStateTransition(eMorir);
        aMorir.AddCondition(AnimatorConditionMode.If, 0f, GatilloMorir);
        aMorir.hasExitTime = false;
        aMorir.hasFixedDuration = true;
        aMorir.duration = 0.05f;
        aMorir.canTransitionToSelf = false;

        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();

        Debug.Log(string.Format(
            "Controller de los zombis armado en {0}: Andar (caminar {1:0.00} s <-> correr {2:0.00} s por {3}, a x{4}) / Atacar ({5:0.00} s) / Morir ({6:0.00} s a x{7} = {8:0.00} s).",
            RutaControlador, caminar.length, correr.length, ParametroRitmo, ParametroPaso,
            atacar.length, morir.length, VelocidadDeLaMuerte, morir.length / VelocidadDeLaMuerte));
    }

    static AnimationClip Clip(string nombre)
    {
        return ClipEn(ClipsZombi, nombre);
    }

    static AnimationClip ClipEn(string carpeta, string nombre)
    {
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(carpeta + nombre + ".FBX"))
        {
            var clip = o as AnimationClip;
            if (clip != null && !clip.name.StartsWith("__preview__")) return clip;
        }
        return null;
    }

    // --- El jugador -------------------------------------------------------------------

    const string RutaJugadorEnElPack = "Assets/ToonyTinyPeople/TT_demo/animation/male/TT_demo_male_A.controller";
    public const string RutaControladorJugador = Carpeta + "/Jugador.controller";
    public const string RutaMascaraBrazo = Carpeta + "/BrazoDerecho.mask";
    const string ClipsJugador = "Assets/ToonyTinyPeople/TT_demo/animation/male/";

    // Los nombres los comparte PlayerController (vienen del controller del pack).
    public const string ParametroCorrer = "run";
    public const string ParametroDisparar = "shoot";
    public const string ParametroMuerto = "muerto";
    public const string CapaDisparo = "Disparo";

    // Arma el controller del jugador, pedido de Ivan: que se vea que dispara tambien
    // corriendo. El del pack pasaba a disparar solo desde quieto, y de disparar no volvia a
    // correr hasta soltar: en el telefono, donde se corre y se dispara a la vez, casi no se
    // veia, y el muñeco se deslizaba en la pose de disparo.
    //
    // Ahora son dos capas. La de abajo es el cuerpo: quieto o corriendo. La de arriba es
    // solo el brazo derecho (la mascara BrazoDerecho), que apunta la pistola mientras se
    // dispara, corra o no. El tiron de cada tiro lo pone ArmaEnLaMano, despues del Animator.
    //
    // Como el de los zombis, sale de la carpeta del pack con MoveAsset, que conserva el
    // guid: las tres escenas de juego, que lo tienen en el Animator del modelo, siguen
    // apuntando solas.
    [MenuItem("ShowBies/Animaciones/Armar el controller del jugador")]
    public static void ArmarJugador()
    {
        if (!AssetDatabase.IsValidFolder(Carpeta))
            AssetDatabase.CreateFolder("Assets", "Animaciones");

        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaControladorJugador) == null &&
            AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaJugadorEnElPack) != null)
        {
            string error = AssetDatabase.MoveAsset(RutaJugadorEnElPack, RutaControladorJugador);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError("No se pudo mover el controller del jugador: " + error);
                return;
            }
            Debug.Log("TT_demo_male_A.controller salio de la carpeta del pack a " + RutaControladorJugador + " (mismo guid).");
        }

        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaControladorJugador);
        if (ctrl == null)
        {
            Debug.LogError("No esta el controller del jugador en " + RutaControladorJugador);
            return;
        }

        AnimationClip quieto = ClipEn(ClipsJugador, "m_pistol_idle_A");
        AnimationClip correr = ClipEn(ClipsJugador, "m_pistol_run");
        AnimationClip disparar = ClipEn(ClipsJugador, "m_pistol_shoot");
        AnimationClip morir = ClipEn(ClipsJugador, "m_death_A");
        if (quieto == null || correr == null || disparar == null || morir == null)
        {
            Debug.LogError("Faltan clips del jugador en " + ClipsJugador);
            return;
        }

        // La mascara: el brazo derecho y sus dedos, nada mas. Asi las piernas, el torso y
        // el otro brazo siguen corriendo mientras la mano apunta.
        var mascara = AssetDatabase.LoadAssetAtPath<AvatarMask>(RutaMascaraBrazo);
        if (mascara == null)
        {
            mascara = new AvatarMask();
            AssetDatabase.CreateAsset(mascara, RutaMascaraBrazo);
        }
        for (var parte = AvatarMaskBodyPart.Root; parte < AvatarMaskBodyPart.LastBodyPart; parte++)
            mascara.SetHumanoidBodyPartActive(parte, parte == AvatarMaskBodyPart.RightArm || parte == AvatarMaskBodyPart.RightFingers);
        EditorUtility.SetDirty(mascara);

        // De cero, como el de los zombis.
        while (ctrl.layers.Length > 1) ctrl.RemoveLayer(ctrl.layers.Length - 1);
        var capas = ctrl.layers;
        capas[0].defaultWeight = 1f;
        ctrl.layers = capas;
        var cuerpo = ctrl.layers[0].stateMachine;
        foreach (var t in new List<AnimatorStateTransition>(cuerpo.anyStateTransitions))
            cuerpo.RemoveAnyStateTransition(t);
        foreach (var s in new List<ChildAnimatorState>(cuerpo.states))
            cuerpo.RemoveState(s.state);
        foreach (var p in new List<AnimatorControllerParameter>(ctrl.parameters))
            ctrl.RemoveParameter(p);

        ctrl.AddParameter(ParametroCorrer, AnimatorControllerParameterType.Bool);
        ctrl.AddParameter(ParametroDisparar, AnimatorControllerParameterType.Bool);
        ctrl.AddParameter(ParametroMuerto, AnimatorControllerParameterType.Bool);

        // El cuerpo. Sin tiempo de salida en ninguna de las dos: el del pack esperaba al 63 %
        // del paso para frenar, y el muñeco seguia corriendo en el lugar un rato.
        var eQuieto = cuerpo.AddState("Quieto", new Vector3(260, 0, 0));
        eQuieto.motion = quieto;
        var eCorrer = cuerpo.AddState("Correr", new Vector3(520, 0, 0));
        eCorrer.motion = correr;
        cuerpo.defaultState = eQuieto;
        Transicion(eQuieto, eCorrer, ParametroCorrer, true, 0.1f);
        Transicion(eCorrer, eQuieto, ParametroCorrer, false, 0.15f);

        // Al morir se desploma (pedido de Ivan: antes quedaba parado detras de la derrota
        // mientras la horda festejaba). Es un bool y no un gatillo porque revivir lo
        // levanta. El clip trae el movimiento horneado en la pose: cae en el lugar, sin
        // mover al muñeco. No puede empezar de nuevo sobre si mismo.
        var eMorir = cuerpo.AddState("Morir", new Vector3(390, -110, 0));
        eMorir.motion = morir;
        var aMorir = cuerpo.AddAnyStateTransition(eMorir);
        aMorir.AddCondition(AnimatorConditionMode.If, 0f, ParametroMuerto);
        aMorir.hasExitTime = false;
        aMorir.hasFixedDuration = true;
        aMorir.duration = 0.1f;
        aMorir.canTransitionToSelf = false;
        Transicion(eMorir, eQuieto, ParametroMuerto, false, 0.25f);

        // El brazo. "Nada" no tiene clip: deja pasar lo de abajo.
        ctrl.AddLayer(CapaDisparo);
        capas = ctrl.layers;
        capas[1].defaultWeight = 1f;
        capas[1].avatarMask = mascara;
        capas[1].blendingMode = AnimatorLayerBlendingMode.Override;
        ctrl.layers = capas;
        var brazo = ctrl.layers[1].stateMachine;
        var eNada = brazo.AddState("Nada", new Vector3(260, 0, 0));
        var eApuntar = brazo.AddState("Apuntar", new Vector3(520, 0, 0));
        eApuntar.motion = disparar;
        brazo.defaultState = eNada;
        // Entra pasado el tiron que trae el clip al principio: el de cada tiro lo pone
        // ArmaEnLaMano, y el del clip salia una sola vez por rafaga.
        var aApuntar = Transicion(eNada, eApuntar, ParametroDisparar, true, 0.08f);
        aApuntar.offset = 0.2f;
        Transicion(eApuntar, eNada, ParametroDisparar, false, 0.2f);

        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();
        Debug.Log("Controller del jugador armado en " + RutaControladorJugador + ": Quieto <-> Correr, y el brazo derecho apunta con " + ParametroDisparar + ".");
    }

    static AnimatorStateTransition Transicion(AnimatorState desde, AnimatorState hasta, string parametro, bool prendido, float duracion)
    {
        var t = desde.AddTransition(hasta);
        t.AddCondition(prendido ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parametro);
        t.hasExitTime = false;
        t.hasFixedDuration = true;
        t.duration = duracion;
        return t;
    }
}
