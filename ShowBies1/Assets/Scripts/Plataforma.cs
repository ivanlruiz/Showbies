using UnityEngine;

// Un solo criterio para "estamos en movil", para que todos los sistemas se
// pongan de acuerdo. Antes ConditionalShow miraba el define de compilacion
// (el target activo) y PlayerJS miraba la plataforma en runtime: en el editor
// con target Android, los joysticks se veian pero no respondian.
//
// En una build manda la plataforma real. En el editor manda el build target
// activo: con target Android, el editor se comporta como un telefono (los
// joysticks responden al mouse y el teclado se apaga), asi que el control
// tactil se puede probar sin dispositivo.
public static class Plataforma
{
    public static bool EsMovil
    {
        get
        {
#if UNITY_EDITOR
            var target = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
            return target == UnityEditor.BuildTarget.Android || target == UnityEditor.BuildTarget.iOS;
#else
            return Application.isMobilePlatform;
#endif
        }
    }
}
