using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Arma el Animator Controller de los zombis con los clips que ya trae
// ToonyTinyPeople. Hasta el 22/9 el controller tenia UN solo estado (Z_run_rm en
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
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(ClipsZombi + nombre + ".FBX"))
        {
            var clip = o as AnimationClip;
            if (clip != null && !clip.name.StartsWith("__preview__")) return clip;
        }
        return null;
    }
}
