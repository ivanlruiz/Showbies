using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

// Solo en la APK de prueba (el paquete .prueba): una caja chica abajo a la izquierda del
// menu con el estado de los anuncios, para saber por que no aparece una oferta sin conectar
// el telefono a la PC. Muestra si AdMob arranco, que dijo el consentimiento, como quedo el
// video de cada lugar (con el codigo de error si no cargo), cuantos avisos llegaron de Java
// y los topes que miran las ofertas.
//
// La crea VigiaAplicacion; en el AAB y en el editor no existe. No es para el jugador: por
// eso va en IMGUI, sin la fuente del juego y sin pasar por la tabla de textos.
public class DiagnosticoAnuncios : MonoBehaviour
{
    private GUIStyle estilo;
    private string texto = "";
    private float proxima;

    private void Update()
    {
        if (Time.unscaledTime < proxima) return;
        proxima = Time.unscaledTime + 0.5f;
        texto = Armar();
    }

    private void OnGUI()
    {
        if (SceneManager.GetActiveScene().buildIndex != 0 || string.IsNullOrEmpty(texto)) return;
        if (estilo == null)
        {
            estilo = new GUIStyle(GUI.skin.box);
            estilo.alignment = TextAnchor.UpperLeft;
            estilo.wordWrap = true;
            estilo.fontSize = Mathf.Max(12, Screen.height / 42);
            estilo.normal.textColor = Color.white;
        }
        Rect segura = Screen.safeArea;
        float ancho = Screen.width * 0.45f;
        float alto = estilo.CalcHeight(new GUIContent(texto), ancho);
        // IMGUI cuenta desde arriba; el area segura, desde abajo.
        float y = Screen.height - segura.yMin - alto - 10f;
        GUI.Box(new Rect(segura.xMin + 10f, y, ancho, alto), texto, estilo);
    }

    private static string Armar()
    {
        var sb = new StringBuilder();
        sb.Append("ANUNCIOS (prueba): ").Append(ServicioAnuncios.NombreDelProveedor).Append('\n');

        var admob = ServicioAnuncios.ProveedorCreado as ProveedorAdMob;
        if (admob != null)
        {
            sb.Append("SDK: ").Append(admob.SdkListo ? "listo" : "sin arrancar")
              .Append(" | consentimiento: ").Append(admob.EstadoConsentimiento)
              .Append(" | puede pedir: ").Append(admob.PuedePedir).Append('\n');
            foreach (string lugar in ConfigAnuncios.LugaresConVideo)
                sb.Append(lugar).Append(": ").Append(admob.EstadoDe(lugar)).Append('\n');
            ConfigAnuncios cfg = ServicioAnuncios.ConfigEnUso;
            if (cfg != null && cfg.automaticos)
                sb.Append(LugarAnuncio.Automatico).Append(": ").Append(admob.EstadoDe(LugarAnuncio.Automatico))
                  .Append(" | partidas desde el ultimo ").Append(Progreso.PartidasTerminadas - Progreso.PartidaDelUltimoAutomatico)
                  .Append('/').Append(cfg.partidasEntreAutomaticos)
                  .Append(" (el primero desde la ").Append(cfg.partidasAntesDelPrimerAutomatico).Append(")\n");
            sb.Append("avisos de Java: ").Append(admob.AvisosRecibidos)
              .Append(" | ultimo: ").Append(admob.UltimoAviso).Append('\n');
            if (!string.IsNullOrEmpty(admob.UltimoError)) sb.Append("error: ").Append(admob.UltimoError).Append('\n');
        }
        if (!string.IsNullOrEmpty(PuenteAdMobAndroid.UltimoFallo))
            sb.Append("fallo JNI: ").Append(PuenteAdMobAndroid.UltimoFallo).Append('\n');

        ConfigAnuncios config = ServicioAnuncios.ConfigEnUso;
        if (config != null)
        {
            sb.Append("hoy ").Append(Progreso.UsosDeHoyEnTotal()).Append('/')
              .Append(config.vecesPorDia > 0 ? config.vecesPorDia.ToString() : "sin tope")
              .Append(" | partidas ").Append(Progreso.PartidasTerminadas).Append('/').Append(config.partidasTerminadasMinimas)
              .Append(" | jugado ").Append((int)Progreso.SegundosJugados).Append('/').Append((int)config.segundosJugadosMinimos)
              .Append(" s | videos ").Append(Progreso.OfrecerVideos ? "si" : "no");
        }
        return sb.ToString();
    }
}
