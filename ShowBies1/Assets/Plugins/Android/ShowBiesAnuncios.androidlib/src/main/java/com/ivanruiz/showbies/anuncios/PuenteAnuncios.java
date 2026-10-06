package com.ivanruiz.showbies.anuncios;

import android.app.Activity;
import android.os.Handler;
import android.os.Looper;
import android.provider.Settings;
import android.util.Log;

import com.google.android.gms.ads.AdError;
import com.google.android.gms.ads.AdRequest;
import com.google.android.gms.ads.FullScreenContentCallback;
import com.google.android.gms.ads.LoadAdError;
import com.google.android.gms.ads.MobileAds;
import com.google.android.gms.ads.RequestConfiguration;
import com.google.android.gms.ads.rewarded.RewardedAd;
import com.google.android.gms.ads.rewarded.RewardedAdLoadCallback;
import com.google.android.ump.ConsentDebugSettings;
import com.google.android.ump.ConsentInformation;
import com.google.android.ump.ConsentRequestParameters;
import com.google.android.ump.UserMessagingPlatform;

import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.util.HashMap;
import java.util.Locale;
import java.util.Map;

// El puente entre el juego y AdMob (los videos con recompensa) y UMP (el consentimiento de
// Europa), sin el plugin de Unity: del lado de C# lo usa ProveedorAdMob, por JNI, como la
// reseña de Play (PedidoDeResena).
//
// Todo el estado vive en el hilo principal de Android: lo que llega de Unity se pasa ahi con
// el Handler, y lo que llega del SDK tambien (el SDK clasico pide que todo se le llame desde
// ese hilo). Lo que vuelve al juego son avisos de texto por OyenteAnuncios: (lugar, evento,
// dato). C# los encola y los atiende en su propio hilo.
//
// La regla de oro: cada pedido de mostrar termina en UN aviso "terminado", pase lo que pase
// (no habia anuncio, se cerro, fallo o algo revento). Si no, el juego quedaria esperando
// para siempre, mudo y con el revivir congelado. Del otro lado igual hay un vigia por si el
// SDK no avisa nunca.
//
// Los avisos:
//   cargado / no_cargado (dato: el codigo) / vencido   el anuncio de un lugar
//   abierto, ganado                                    el que esta en pantalla
//   terminado (recompensado, cerrado, falla, no_disponible)
//   consentimiento (requerido, obtenido, no_requerido, desconocido)
//   privacidad (requerida, no)
//   consentimiento_cerrado                             se cerro el cartel de UMP (o no hacia falta)
//   puede_pedir (si, no)                               si UMP deja pedir anuncios
//   sdk_listo, error (dato: el mensaje)
public final class PuenteAnuncios {
    private static final String ETIQUETA = "ShowBiesAnuncios";

    // Un anuncio cargado vence a la hora (lo dice Google): se tira un poco antes y se pide otro.
    private static final long VIDA_DE_UN_ANUNCIO_MS = 55L * 60L * 1000L;
    // Lo que se espera para volver a pedir uno que no cargo, cada vez mas.
    private static final long[] ESPERAS_MS = {10000L, 30000L, 60000L, 120000L, 300000L};

    private static final Handler principal = new Handler(Looper.getMainLooper());

    private static Activity actividad;
    private static OyenteAnuncios oyente;
    private static String clasificacion = RequestConfiguration.MAX_AD_CONTENT_RATING_T;
    private static boolean simularEuropa;

    private static final Map<String, String> bloques = new HashMap<>();
    private static final Map<String, RewardedAd> cargados = new HashMap<>();
    private static final Map<String, Boolean> cargando = new HashMap<>();
    private static final Map<String, Integer> fallos = new HashMap<>();
    private static final Map<String, Runnable> vencimientos = new HashMap<>();

    private static boolean sdkPedido;   // ya se llamo a MobileAds.initialize
    private static boolean sdkListo;    // y termino
    private static String enPantalla;   // el lugar del anuncio que se esta mostrando, o null
    private static boolean ganado;      // si el de enPantalla ya dio el premio

    private PuenteAnuncios() {
    }

    // --- lo que llama el juego ---------------------------------------------------------

    // Al abrir la app (ServicioAnuncios.Arrancar). Pregunta a UMP sin mostrar nada; si se
    // pueden pedir anuncios (fuera de Europa, o con el consentimiento ya dado), arranca el
    // SDK y carga un video por lugar. El cartel de UMP lo pide el juego aparte, cuando le
    // conviene (pedirConsentimiento).
    public static void iniciar(final Activity act, final OyenteAnuncios o, final String[] lugares,
                               final String[] ids, final String clasif, final boolean europa) {
        enPrincipal(() -> {
            actividad = act;
            oyente = o;
            bloques.clear();
            if (lugares != null && ids != null) {
                for (int i = 0; i < lugares.length && i < ids.length; i++) {
                    if (lugares[i] != null && ids[i] != null && !ids[i].isEmpty()) bloques.put(lugares[i], ids[i]);
                }
            }
            clasificacion = clasificacionValida(clasif);
            simularEuropa = europa;
            actualizarConsentimiento();
        });
    }

    public static void mostrar(final Activity act, final String lugar) {
        enPrincipal(() -> {
            try {
                mostrarYa(act, lugar);
            } catch (Throwable t) {
                avisar(lugar, "error", "mostrar: " + t);
                if (lugar != null && lugar.equals(enPantalla)) terminar(lugar, "no_disponible");
                else avisar(lugar, "terminado", "no_disponible");
            }
        });
    }

    // El vigia de C# dio por cerrado el que se estaba mostrando (el SDK no aviso): se
    // olvida, asi el proximo pedido no choca con uno que ya no esta.
    public static void olvidarElQueSeMuestra() {
        enPrincipal(() -> {
            if (enPantalla == null) return;
            String lugar = enPantalla;
            enPantalla = null;
            ganado = false;
            cargar(lugar);
        });
    }

    // El cartel de consentimiento, solo si UMP dice que hace falta. Al cerrarse (o en el
    // acto, si no hacia falta) avisa consentimiento_cerrado.
    public static void pedirConsentimiento(final Activity act) {
        enPrincipal(() -> {
            if (act != null) actividad = act;
            try {
                UserMessagingPlatform.loadAndShowConsentFormIfRequired(actividad, error -> enPrincipal(() -> {
                    if (error != null) avisar("", "error", "formulario: " + error.getMessage());
                    alCerrarElCartel();
                }));
            } catch (Throwable t) {
                avisar("", "error", "formulario: " + t);
                alCerrarElCartel();
            }
        });
    }

    // El cartel para cambiar lo que se eligio (el boton PRIVACIDAD de las opciones).
    public static void mostrarPrivacidad(final Activity act) {
        enPrincipal(() -> {
            if (act != null) actividad = act;
            try {
                UserMessagingPlatform.showPrivacyOptionsForm(actividad, error -> enPrincipal(() -> {
                    if (error != null) avisar("", "error", "privacidad: " + error.getMessage());
                    alCerrarElCartel();
                }));
            } catch (Throwable t) {
                avisar("", "error", "privacidad: " + t);
                alCerrarElCartel();
            }
        });
    }

    // --- el consentimiento -------------------------------------------------------------

    private static void actualizarConsentimiento() {
        try {
            ConsentInformation info = UserMessagingPlatform.getConsentInformation(actividad);
            ConsentRequestParameters.Builder parametros = new ConsentRequestParameters.Builder();
            if (simularEuropa) {
                // Solo en la APK de prueba: para ver el cartel desde un telefono de aca.
                ConsentDebugSettings.Builder depuracion = new ConsentDebugSettings.Builder(actividad)
                        .setDebugGeography(ConsentDebugSettings.DebugGeography.DEBUG_GEOGRAPHY_EEA);
                String id = idDeEsteTelefono();
                if (!id.isEmpty()) depuracion.addTestDeviceHashedId(id);
                parametros.setConsentDebugSettings(depuracion.build());
            }
            info.requestConsentInfoUpdate(actividad, parametros.build(),
                    () -> enPrincipal(() -> {
                        informarConsentimiento();
                        iniciarSdkSiSePuede();
                    }),
                    error -> enPrincipal(() -> {
                        avisar("", "error", "consentimiento: " + (error != null ? error.getMessage() : ""));
                        informarConsentimiento();
                        iniciarSdkSiSePuede();
                    }));
            // Con lo que se eligio en una sesion anterior se puede arrancar ya, sin esperar la
            // respuesta: es lo que recomienda Google.
            iniciarSdkSiSePuede();
        } catch (Throwable t) {
            // Sin UMP no se sabe si hace falta el consentimiento: no se piden anuncios.
            avisar("", "error", "consentimiento: " + t);
        }
    }

    private static void alCerrarElCartel() {
        informarConsentimiento();
        avisar("", "consentimiento_cerrado", "");
        iniciarSdkSiSePuede();
    }

    private static void informarConsentimiento() {
        try {
            ConsentInformation info = UserMessagingPlatform.getConsentInformation(actividad);
            String estado;
            switch (info.getConsentStatus()) {
                case ConsentInformation.ConsentStatus.REQUIRED:
                    estado = "requerido";
                    break;
                case ConsentInformation.ConsentStatus.OBTAINED:
                    estado = "obtenido";
                    break;
                case ConsentInformation.ConsentStatus.NOT_REQUIRED:
                    estado = "no_requerido";
                    break;
                default:
                    estado = "desconocido";
                    break;
            }
            avisar("", "consentimiento", estado);
            boolean privacidad = info.getPrivacyOptionsRequirementStatus()
                    == ConsentInformation.PrivacyOptionsRequirementStatus.REQUIRED;
            avisar("", "privacidad", privacidad ? "requerida" : "no");
            // Si con esto se pueden pedir anuncios: sin consentimiento en Europa, no.
            avisar("", "puede_pedir", info.canRequestAds() ? "si" : "no");
        } catch (Throwable t) {
            avisar("", "error", "estado del consentimiento: " + t);
        }
    }

    // El id con que UMP reconoce a este telefono como de prueba: el MD5 del ANDROID_ID, en
    // mayusculas, el mismo que UMP escribe en el logcat. Solo con simularEuropa, o sea en la
    // APK de prueba.
    private static String idDeEsteTelefono() {
        try {
            String id = Settings.Secure.getString(actividad.getContentResolver(), Settings.Secure.ANDROID_ID);
            if (id == null) return "";
            byte[] hash = MessageDigest.getInstance("MD5").digest(id.getBytes(StandardCharsets.UTF_8));
            StringBuilder texto = new StringBuilder();
            for (byte b : hash) texto.append(String.format(Locale.US, "%02X", b));
            return texto.toString();
        } catch (Throwable t) {
            return "";
        }
    }

    // --- el SDK y los videos -----------------------------------------------------------

    private static void iniciarSdkSiSePuede() {
        if (sdkPedido || actividad == null) return;
        boolean puede;
        try {
            puede = UserMessagingPlatform.getConsentInformation(actividad).canRequestAds();
        } catch (Throwable t) {
            puede = false;
        }
        if (!puede) return;
        sdkPedido = true;

        // Antes de arrancar: el SDK puede pedir anuncios apenas arranca.
        try {
            MobileAds.setRequestConfiguration(MobileAds.getRequestConfiguration().toBuilder()
                    .setMaxAdContentRating(clasificacion)
                    .build());
        } catch (Throwable t) {
            avisar("", "error", "configuracion: " + t);
        }

        final Activity a = actividad;
        // Google lo arranca fuera del hilo principal: tarda.
        new Thread(() -> {
            try {
                MobileAds.initialize(a, estado -> enPrincipal(() -> {
                    sdkListo = true;
                    avisar("", "sdk_listo", "");
                    for (String lugar : bloques.keySet()) cargar(lugar);
                }));
            } catch (Throwable t) {
                enPrincipal(() -> {
                    sdkPedido = false;
                    avisar("", "error", "initialize: " + t);
                });
            }
        }, ETIQUETA).start();
    }

    private static void cargar(final String lugar) {
        if (!sdkListo || actividad == null || lugar == null) return;
        if (cargados.get(lugar) != null || Boolean.TRUE.equals(cargando.get(lugar))) return;
        if (lugar.equals(enPantalla)) return;
        String bloque = bloques.get(lugar);
        if (bloque == null) return;

        cargando.put(lugar, true);
        try {
            RewardedAd.load(actividad, bloque, new AdRequest.Builder().build(), new RewardedAdLoadCallback() {
                @Override
                public void onAdLoaded(RewardedAd anuncio) {
                    enPrincipal(() -> {
                        cargando.put(lugar, false);
                        fallos.put(lugar, 0);
                        cargados.put(lugar, anuncio);
                        programarVencimiento(lugar);
                        avisar(lugar, "cargado", "");
                    });
                }

                @Override
                public void onAdFailedToLoad(LoadAdError error) {
                    enPrincipal(() -> {
                        cargando.put(lugar, false);
                        avisar(lugar, "no_cargado", error != null ? String.valueOf(error.getCode()) : "");
                        reintentar(lugar);
                    });
                }
            });
        } catch (Throwable t) {
            cargando.put(lugar, false);
            avisar(lugar, "no_cargado", t.toString());
            reintentar(lugar);
        }
    }

    private static void reintentar(final String lugar) {
        Integer previos = fallos.get(lugar);
        int n = previos != null ? previos + 1 : 1;
        fallos.put(lugar, n);
        long espera = ESPERAS_MS[Math.min(n, ESPERAS_MS.length) - 1];
        principal.postDelayed(() -> correr(() -> cargar(lugar)), espera);
    }

    private static void programarVencimiento(final String lugar) {
        cancelarVencimiento(lugar);
        Runnable vencer = () -> correr(() -> {
            vencimientos.remove(lugar);
            if (cargados.remove(lugar) != null) {
                avisar(lugar, "vencido", "");
                cargar(lugar);
            }
        });
        vencimientos.put(lugar, vencer);
        principal.postDelayed(vencer, VIDA_DE_UN_ANUNCIO_MS);
    }

    private static void cancelarVencimiento(String lugar) {
        Runnable vencer = vencimientos.remove(lugar);
        if (vencer != null) principal.removeCallbacks(vencer);
    }

    private static void mostrarYa(Activity act, final String lugar) {
        if (act != null) actividad = act;
        if (lugar == null || actividad == null || enPantalla != null) {
            avisar(lugar, "terminado", "no_disponible");
            return;
        }
        RewardedAd anuncio = cargados.remove(lugar);
        if (anuncio == null) {
            avisar(lugar, "terminado", "no_disponible");
            cargar(lugar);
            return;
        }
        cancelarVencimiento(lugar);
        enPantalla = lugar;
        ganado = false;

        anuncio.setFullScreenContentCallback(new FullScreenContentCallback() {
            @Override
            public void onAdShowedFullScreenContent() {
                enPrincipal(() -> avisar(lugar, "abierto", ""));
            }

            @Override
            public void onAdDismissedFullScreenContent() {
                enPrincipal(() -> terminar(lugar, ganado ? "recompensado" : "cerrado"));
            }

            @Override
            public void onAdFailedToShowFullScreenContent(AdError error) {
                enPrincipal(() -> {
                    avisar(lugar, "error", "mostrar: " + (error != null ? error.getMessage() : ""));
                    terminar(lugar, ganado ? "recompensado" : "falla");
                });
            }
        });
        // El premio llega antes del cierre: se anota y se avisa recien al cerrarse (terminar).
        anuncio.show(actividad, recompensa -> enPrincipal(() -> {
            if (lugar.equals(enPantalla)) {
                ganado = true;
                avisar(lugar, "ganado", "");
            }
        }));
    }

    // Un solo aviso por pedido: el segundo (un cierre que llega despues de una falla, o
    // despues de que el vigia de C# lo olvido) no encuentra a nadie en pantalla.
    private static void terminar(String lugar, String resultado) {
        if (lugar == null || !lugar.equals(enPantalla)) return;
        enPantalla = null;
        ganado = false;
        avisar(lugar, "terminado", resultado);
        // El siguiente, para la proxima oferta: cada anuncio sirve una sola vez.
        cargar(lugar);
    }

    // --- ayudas ------------------------------------------------------------------------

    private static String clasificacionValida(String clasif) {
        if (RequestConfiguration.MAX_AD_CONTENT_RATING_G.equals(clasif)) return RequestConfiguration.MAX_AD_CONTENT_RATING_G;
        if (RequestConfiguration.MAX_AD_CONTENT_RATING_PG.equals(clasif)) return RequestConfiguration.MAX_AD_CONTENT_RATING_PG;
        if (RequestConfiguration.MAX_AD_CONTENT_RATING_MA.equals(clasif)) return RequestConfiguration.MAX_AD_CONTENT_RATING_MA;
        return RequestConfiguration.MAX_AD_CONTENT_RATING_T;
    }

    private static void avisar(String lugar, String evento, String dato) {
        String l = lugar != null ? lugar : "";
        String d = dato != null ? dato : "";
        Log.d(ETIQUETA, evento + " [" + l + "] " + d);
        OyenteAnuncios o = oyente;
        if (o == null) return;
        try {
            o.alEvento(l, evento, d);
        } catch (Throwable t) {
            Log.w(ETIQUETA, "no se pudo avisar al juego: " + t);
        }
    }

    private static void enPrincipal(Runnable r) {
        if (Looper.myLooper() == Looper.getMainLooper()) correr(r);
        else principal.post(() -> correr(r));
    }

    private static void correr(Runnable r) {
        try {
            r.run();
        } catch (Throwable t) {
            Log.e(ETIQUETA, "error en el puente de anuncios", t);
            avisar("", "error", t.toString());
        }
    }
}
