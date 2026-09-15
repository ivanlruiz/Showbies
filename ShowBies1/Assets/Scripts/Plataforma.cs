using UnityEngine;

// Un solo criterio para "estamos en movil", para que todos los sistemas se
// pongan de acuerdo. Antes ConditionalShow miraba el define de compilacion
// (el target activo) y PlayerJS miraba la plataforma en runtime: en el editor
// con target Android, los joysticks se veian pero no respondian.
//
// En una build manda la plataforma real. En el editor manda el build target
// activo: con target Android, el editor se comporta como un telefono (los
// joysticks responden al mouse y el teclado se apaga), asi que el control
// tactil se puede probar sin dispositivo. Para jugar con teclado y mouse sin
// cambiar el target (que reimporta todo), esta el menu ShowBies > Controles:
// una preferencia de esta maquina, que no va al repo.
public static class Plataforma
{
#if UNITY_EDITOR
    public const string ClaveTecladoEnElEditor = "ShowBies.TecladoYMouseEnElEditor";

    // Leida una vez por dominio: EsMovil se consulta en cada frame, y EditorPrefs
    // va al registro. El recargado de scripts al entrar en play la vuelve a leer.
    private static int tecladoEnElEditor = -1;

    public static void OlvidarPreferenciaDelEditor()
    {
        tecladoEnElEditor = -1;
    }
#endif

    public static bool EsMovil
    {
        get
        {
#if UNITY_EDITOR
            if (tecladoEnElEditor < 0)
                tecladoEnElEditor = UnityEditor.EditorPrefs.GetBool(ClaveTecladoEnElEditor, false) ? 1 : 0;
            if (tecladoEnElEditor == 1) return false;

            var target = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
            return target == UnityEditor.BuildTarget.Android || target == UnityEditor.BuildTarget.iOS;
#else
            return Application.isMobilePlatform;
#endif
        }
    }
}
