# Publicar ShowBies en Google Play

Estado al 2026-10-02 (mirado en Play Console ese día). Lo tildado ya está hecho; lo demás es en la web de Play
Console. La cuenta es "j'ntr", en el Gmail de la ficha (en el Chrome de Ivan, `play.google.com/console/u/2`): la
consola abre por defecto la del otro Gmail, "IvRu", que Google cerró por inactividad el 3/11/2024 y no tiene nada.

## Lo que ya está cargado en Play Console

La app **ShowBies** (`com.ivanruiz.showbies`) está creada como borrador en la cuenta personal "j'ntr".

- [x] **Datos de inicio de sesión**: no, no hay nada restringido.
- [x] **Aplicaciones gubernamentales**: no.
- [x] **Funciones financieras**: ninguna.
- [x] **Salud**: ninguna.
- [x] **Categoría**: Juego > Acción.
- [x] **Email de contacto** de la ficha: el Gmail de Ivan (se ve en la tienda).
- [x] **Anuncios**: no. **ID de publicidad**: no. La primera build sale sin AdMob y la prueba cerrada arranca
      así; cuando suba la build con anuncios hay que cambiar estas dos, la seguridad de los datos y la política.
- [x] **Contenido y audiencia objetivo**: 13-15, 16-17 y 18+.
- [x] **Clasificación de contenido** (IARC): violencia fantástica contra no humanos, sangre limitada, miedo.
      Salió ESRB E10+, ClassInd 10 y GRAC 12+. El cuestionario mandado con la 1.3.0 quedó activo el 6/10/2026
      ("Live Rating Notice" de IARC, Global Rating ID `5b6d86a1-864c-8c4c-88b0-3fa3d2cff6ee`, por si otra tienda lo
      pide). Si una actualización cambia alguna respuesta, hay que volver a llenarlo: mirarlo con la 1.4.0, aunque
      los anuncios van en otra declaración (Anuncios: sí).
- [x] **Política de privacidad**: `https://ivanlruiz.github.io/showbies-privacidad/`, desde el repo público
      `ivanlruiz/showbies-privacidad` (`index.html` y `privacidad.html`, copias de `privacidad.html` de acá: al
      cambiar uno hay que actualizar los otros). Se mudó ahí el 19/9 para poder pasar este repo a privado, pero **el
      repo `ivanlruiz/Showbies` sigue siendo público** (lo vio la revisión del 6/10; lo cambia Ivan en GitHub). El
      link nuevo se cargó el 19/9 y salió con la publicación del 21/9. El viejo
      (`ivanlruiz.github.io/Showbies/privacidad.html`, rama `gh-pages`) redirige al nuevo desde el 6/10. La versión
      con la sección de reseñas (para la 1.3.0) está en `privacidad.html` de acá y sin subir a `showbies-privacidad`.
- [x] **Seguridad de los datos**: no recopila ni comparte; completada el 16/9. La 1.3.0 (6) es la primera con la
      librería de reseñas: hay que declarar "Otro contenido generado por el usuario" en el mismo envío que el AAB
      (H08 en `pendientes.md`).
- [x] **Ficha de Play Store** (borrador): textos en inglés (principal), español de España y de Latinoamérica
      (`ficha.md`); icono 512, banner 1024x500 y 4 capturas 1920x1080 en inglés, en `Builds/ficha/` (fuera de git).

- [x] **Prueba cerrada (Alpha)**: 177 países y notas de la versión en los tres idiomas. El AAB (versionCode 4,
      1.1.0) salió de `main`; se sube a mano porque pesa 32 MB.
- [x] **Los 12 testers y los 14 días**: el 2/10 el panel tiene las tres tareas tildadas (versión de prueba cerrada
      publicada, 12 testers que aceptaron y 14 días con ellos) y habilita **Solicitar acceso a producción**. Sin
      fallos ni ANR en los últimos 28 días, sin problemas de políticas y sin comentarios de los testers.
- [x] **Solicitar acceso a producción**: enviada el 2/10/2026 a las 22:20 y **aprobada el 5/10/2026** (mail de
      Google: "Your app has been granted Google Play production access"). Que la aprueben no publica nada: habilita el
      canal de producción, y ahí se sube la versión que se elija (hoy en Play está la 1.2.0). Las respuestas, de Ivan,
      por si alguna vez hay que mandarla de nuevo:
      - Cómo se reclutó: una mezcla, amigos y familia con el enlace de la prueba y una comunidad de desarrolladores
        donde se prueban las apps entre ellos. Qué tan fácil: ni difícil ni fácil.
      - Qué hicieron: partidas de oleadas en sus teléfonos; con varios Ivan jugó al lado (se movían, compraban mejoras
        y volvían a jugar), otros escribieron por chat; partidas cortas y repetidas, como en producción.
      - Qué comentaron: en persona y por chat; que se divirtieron mucho, que es muy adictivo, y sugerencias de
        controles, interfaz y dificultad, que se fueron aplicando.
      - Público: de 13 en adelante, partidas cortas en el teléfono, shooters de acción y zombis, inglés y español.
        Qué lo destaca: dos joysticks desde arriba, incremental, jefe cada 10 que salta, embiste e invoca, capítulos
        de noche con neón, modo libre, misiones y logros. Descargas el primer año: entre 0 y 10.000.
      - Qué se cambió: botones grandes cerca del pulgar, confirmar salir y reiniciar, guía de la primera partida y la
        primera compra, jefe con ataques que se esquivan, rendimiento y arreglos. Por qué está listo: sin fallos ni
        ANR ni problemas de políticas, pruebas automáticas enteras y semanas jugándolo en el teléfono.
- [x] **Versión 5 (1.2.0)** enviada a revisión el 18/9/2026, también de `main`, y aprobada: está disponible para los
      testers en 178 de 178 países. Menú con fondo vivo, botones en
      píldora, paleta clara, recompensa diaria, más zombis por oleada, retomar la oleada, volumen y los arreglos de la
      auditoría. Sin Sentis el AAB bajó a unos 24 MB de descarga (35 MB con los símbolos nativos, que Play no reparte).
      La advertencia de "archivo de desofuscación" se ignora: el juego no ofusca Java (no usa R8).
- [x] **Ficha con capturas nuevas** (18/9/2026): 7 capturas 1920x1080 con la paleta clara y el HUD de teléfono
      (endless, furia, jefe, premio diario, tienda, menú, oleada 10), en teléfono y tablets de 7" y 10", en
      `Builds/ficha/v5/`. Se mandaron a revisión junto con la versión 5 y la descripción completa de los tres idiomas:
      Play no deja revisiones separadas, un envío nuevo reinicia la que está en curso.

En la consola, cuando la ventana es angosta, el botón **Guardar** de los formularios de dos pasos queda escondido
en el menú ⋮ de abajo a la derecha.

## Lo que ya está resuelto en el proyecto

- [x] **Keystore de release**: `showbies-release.keystore`, con ruta, alias y passwords en `ShowBies1/keystore.local`
      (gitignoreado). **Hacé una copia fuera de la computadora hoy mismo.** Es la **clave de subida**: con AAB, Play
      App Signing es obligatorio para las apps nuevas desde agosto de 2021, así que la clave que firma lo que se
      instala la guarda Google. Si perdés la tuya, la app no se pierde: generás otra con `keytool` y pedís en Play
      Console que te reseteen la clave de subida, pero hasta que Google lo aprueba no podés subir versiones.
- [x] **Target API 36**, obligatorio para apps nuevas desde el 31/8/2026. Estaba en "automático", que depende de
      qué SDK tenga instalado la máquina.
- [x] **64 bits** (ARM64 + IL2CPP), que Play exige.
- [x] **versionCode 6, versión 1.3.0**: subida el 6/10 (`eb25e5b`) para la primera de producción. Cada subida a Play
      necesita un versionCode mayor que el anterior: si rechazan un AAB y subís otro, hay que volver a subirlo. El
      versionCode del último AAB armado queda en `publicacion/ultimo_aab.txt`, que lo escribe la build, y la build
      del AAB se niega a salir con ese número o uno menor.
- [x] **1.3.0 a producción**: **enviada a revisión el 6/10/2026 a las 11:10**, desde `eb25e5b` (sin la R de
      reiniciar, `96d07b3`), después de que Ivan probara la APK en el teléfono. La política nueva se subió antes a
      `showbies-privacidad`. Un solo envío con 7 cambios: producción 6 (1.3.0) al 100 % con las notas de
      `notas_1.3.0.md` en en-US, es-419 y es-ES; los países (los 177 de la prueba cerrada más "el resto del mundo");
      la descripción de la ficha en los tres idiomas, que termina en "Free to play" / "Gratis"; y la seguridad de
      los datos: "Otro contenido generado por el usuario" (la reseña), recogido, no compartido, opcional, para la
      funcionalidad, cifrado en tránsito, sin cuentas ni inicio de sesión (la pregunta opcional de borrado quedó sin
      responder: las reseñas las borra cada uno desde su cuenta de Google). Google dice que la revisión suele tardar
      hasta 7 días; con la publicación gestionada apagada, al aprobarse sale sola. El AAB pesa 35,6 MB y lo subió
      Ivan arrastrándolo a la consola (la extensión sube hasta 10 MB). La declaración de recursos de IA de la ficha ya
      estaba en "Etiquetar recursos como creados o editados con IA" y no se tocó.
- [x] **Sin anuncios en esta versión** (`ConfigAnuncios.proveedor = Nulo`). Ver "Anuncios" abajo.
- [x] Orientación horizontal en las dos rotaciones, botón atrás de Android, icono de app.

## Antes de subir nada

1. **Probalo en el teléfono.** Desde la fase 4 no se probó nada en un dispositivo real, y desde entonces
   cambiaron los zombis, el pool, la furia, los anuncios y toda la UI. `Build > Android APK` y jugá dos o tres
   partidas completas, incluidas morir, pausar y comprar en la tienda.
2. [x] **Nombre del paquete: `com.ivanruiz.showbies`**, el mismo que tiene la app creada en Play Console. Estaba en
   `com.ivru.showbies` y se cambió para que coincidan. **No se puede cambiar después de publicar.**
3. **Elegí el nombre de desarrollador**, que es público y va en la ficha.

## En Play Console

### 1. Crear la app
Nombre, idioma principal (inglés, si el juego va a salir en inglés), app o juego, gratis o paga. **Gratis no se
puede pasar a paga después**; al revés sí.

### 2. Configuración de la app (el "panel de tareas")
- **Política de privacidad**: URL pública obligatoria, incluso sin recolectar datos. En `privacidad.html` tenés
  una lista para publicar en GitHub Pages (Settings > Pages del repo, o un repo nuevo `showbies-privacidad`).
- **Acceso a la app**: sin login, todo el contenido disponible.
- **Anuncios**: hoy **no** tiene. Si más adelante entra AdMob, hay que volver acá y cambiarlo.
- **Clasificación de contenido (IARC)**: un cuestionario. Es un juego de zombis con violencia fantástica;
  respondé con sinceridad (violencia leve, sin sangre realista, sin contenido sexual, sin apuestas) y sale una
  clasificación tipo PEGI 12 / ESRB Teen.
- **Público objetivo**: 13+ o 16+. **No marques "dirigido a menores de 13"**: te mete en Families Policy, con
  reglas de anuncios y de datos mucho más duras.
- **Seguridad de los datos**: ShowBies **no recolecta ni transmite nada** (el progreso es un archivo local).
  Respondé "no se recopilan datos". Cuando entren los anuncios hay que rehacer este formulario.
- **Ficha principal**: los textos están en `ficha.md`.
- **Gráficos**: icono 512x512, gráfico de funciones 1024x500, y **mínimo 2 capturas** (para juegos en
  horizontal: 16:9, entre 1080 y 3840 px de lado). Sacalas del teléfono o de la Game view.

### 3. Prueba cerrada (el paso largo)
Si tu cuenta de desarrollador es **personal y fue creada después del 13/11/2023**, Google exige antes de
producción:

- **12 testers como mínimo**, que hayan estado **opted in 14 días seguidos**.
- Recién ahí se habilita el acceso a producción.

Por eso: **creá la prueba cerrada y subí el AAB hoy**, aunque la ficha no esté terminada. Los 14 días corren en
paralelo a todo lo demás. Juntá 12 personas (amigos, familia, foros de gamedev) y que **acepten la invitación y
dejen la app instalada**; se hace con una lista de mails de Google o un grupo de Google.

### 4. Subir el AAB
`Build > Android AAB (release)` en Unity genera `Builds/ShowBies.aab` firmado con tu keystore, que es la clave de
subida. Play App Signing no hace falta activarlo: con AAB es obligatorio para las apps nuevas desde agosto de 2021, y
Google firma con su clave lo que se instala. Si perdés el keystore, pedís en Play Console que te reseteen la clave de
subida (por eso la copia: te ahorra ese trámite).

## Comunidad en Discord

Servidor **ShowBies**, creado el 4/10/2026 desde la cuenta de Discord "j'ntr" (la otra que hay en el Chrome de Ivan
no). **Invitación permanente: https://discord.gg/XsKgU7BBUd** (no vence, sin límite de usos, entra a #welcome y da el
rol Player). Es un servidor de Comunidad, todo en inglés (pedido de Ivan, que primero lo quería en español):

- **INFO**, solo lectura para los miembros: #welcome y #rules (con su mensaje fijado), #announcements y #patch-notes
  (canales de anuncios, se pueden seguir) y #dev, privado, donde llegan los avisos de Discord para moderadores.
- **COMMUNITY**: #general (ahí salen los "X se unió"), #screenshots-and-clips y #high-scores.
- **SHOWBIES**: #feedback y #bugs son foros con pautas y etiquetas (Fixed, en #bugs, solo la pone Ivan) y #ideas.
- **VOICE**: Lounge.
- Roles: Dev (Ivan), Tester (a mano, para los de la prueba cerrada) y Player (lo da la invitación).
- **AutoMod de Discord** prendido: spam, menciones masivas y palabras marcadas (lenguaje muy inapropiado, insultos y
  contenido sexual); bloquea el mensaje y avisa en #dev.
- **Carl-bot** (configurable en carl.gg con la misma cuenta), con solo seis permisos y sin Administrador: gestionar
  roles, ver canales, enviar mensajes, insertar enlaces, leer el historial y ver el registro de auditoría. Da Player a
  todo el que entra (para los que entren por otro link) y registra en #dev los mensajes borrados y editados, las
  entradas y salidas, los baneos y los cambios de roles. Su rol tiene que quedar justo encima de Player: más arriba
  podría repartir Tester o Dev, más abajo no puede dar Player.

Cuando se use para afuera (la ficha de Play, un botón en el juego, redes), el link es el de arriba.

## Anuncios

**AdMob ya está creado** (16/9/2026, cuenta de Play Console). No son secretos: van en el código.

| qué | ID |
|---|---|
| App ShowBies (Android, sin tienda vinculada todavía) | `ca-app-pub-5295383586829735~6982656335` |
| Bonificado `revivir` (recompensa 1 "revivir") | `ca-app-pub-5295383586829735/1512416812` |
| Bonificado `duplicar_derrota` (recompensa 1 "monedas_x2") | `ca-app-pub-5295383586829735/8640931940` |
| Bonificado `regalo_x2` (recompensa 1 "monedas_x2", el video de la recompensa diaria) | `ca-app-pub-5295383586829735/6299120897` |
| Intersticial `automatico` (los automáticos de la derrota; creado el 6/10) | `ca-app-pub-5295383586829735/6659600004` |

Los nombres de los bloques son los de `LugarAnuncio`. Mientras la app no esté vinculada a su ficha publicada, AdMob
limita los anuncios: para probar se usan los bloques de prueba de Google. Al publicar en producción, vincular la
tienda desde **Configuración de la app → Añadir tienda** (`com.ivanruiz.showbies`).

**La integración está hecha** (6/10/2026, rama `admob`, para la 1.4.0): sin el plugin de Unity, con una librería
propia (`Assets/Plugins/Android/ShowBiesAnuncios.androidlib`: el SDK clásico `play-services-ads` 25.5.0, UMP 4.0.0
y un puente en Java) y `ProveedorAdMob` del lado de C#. Lo de cómo funciona está en CLAUDE.md (Anuncios → AdMob).
Decisiones de Ivan del 6/10: **el cartel de consentimiento sale desde la segunda partida terminada**, en el menú, y
**los anuncios llegan hasta la clasificación T** (PG era la recomendada). La APK de prueba usa siempre el bloque de
prueba de Google y muestra en el menú una caja con el estado de los anuncios. Puede hacer como si el teléfono
estuviera en Europa, para ver el cartel y el botón PRIVACIDAD (`simularEuropaEnLaPrueba`), pero eso se prende recién
con el mensaje europeo creado en AdMob (paso 7): sin él, los anuncios de la prueba se quedan esperando. Desde el
6/10 el mensaje está publicado y la opción, prendida en el asset.

La rama `admob` **no va a `main` hasta la 1.4.0**: el SDK suma solo el permiso `AD_ID`, y un arreglo urgente de la
1.3.x armado con eso no pasaría la declaración de "sin anuncios" de Play.

**Para probarla:** compilar y correr la prueba de lógica, armar la APK y jugarla en el teléfono: los videos tienen que
decir "Test Ad", y el cartel de consentimiento salir en el menú desde la segunda partida, con PRIVACIDAD en
OPCIONES. El cartel y PRIVACIDAD aparecen recién cuando esté creado el mensaje europeo en AdMob (paso 7). La primera
APK (6/10) trajo el `SystemForegroundService` de WorkManager y el permiso `FOREGROUND_SERVICE` (los mete el SDK, issue
4092 del plugin): Play pide una declaración aparte por los servicios en primer plano, y los anuncios no los usan. Se
sacan con `tools:node="remove"` en el manifiesto de la librería, y en la APK siguiente ya no estaban.

**Probada en el teléfono de Ivan el 6/10: "funciona joya".** Los videos de prueba de Google salen en los tres lugares,
llega el premio y el juego sigue bien al cerrarlos. La primera APK no mostraba ofertas por la regla de los 180 s
jugados (el progreso de la `.prueba` era nuevo), no por un error: lo dijo la caja de diagnóstico del menú. Ese día
Ivan también sacó los topes de videos por día, por partida y la espera, y bajó el x2 de la derrota a 30 s y 10
monedas (ver CLAUDE.md, Anuncios).

**Para que salgan anuncios de verdad**, en este orden (todo esto es de afuera: se hace con Ivan, de a un paso):
1. La 1.3.0 publicada en producción.
2. En AdMob, **Configuración de la app → Añadir tienda** (`com.ivanruiz.showbies`).
3. **app-ads.txt sin dominio pago**: un repo público `ivanlruiz/ivanlruiz.github.io` con GitHub Pages y `app-ads.txt`
   en la raíz, con la línea `google.com, pub-5295383586829735, DIRECT, f08c47fec0942fa0`.
4. En Play Console, el **sitio web** de los datos de contacto de la ficha en `https://ivanlruiz.github.io/...` (la URL
   de la política sirve): AdMob lee ese campo, no el de la política. Después, en AdMob, "Verificar la app"; tarda
   hasta 24 h, y la revisión de la app 2 o 3 días, con anuncios limitados mientras tanto.
5. [x] En AdMob, el bloque **intersticial** `automatico` (6/10), con su id en `ConfigAnuncios` (`bloqueAutomatico`).
   Es el de los automáticos.
6. **Antes de subir el AAB**: la política nueva publicada, con fecha → rehacer **Seguridad de los datos** (AdMob
   recopila y comparte identificadores del dispositivo, ubicación aproximada, interacciones y diagnósticos, para
   publicidad, analíticas y prevención de fraude) → **Anuncios: sí** e **ID de publicidad: sí** → sacar de la ficha
   (`ficha.md`) el "no recopila tus datos".
7. [x] En AdMob (6/10, con permiso de Ivan en cada paso): **categorías sensibles bloqueadas** (citas, ganar dinero
   rápidamente, juegos de casino sociales, referencias al sexo y salud reproductiva y sexual; alcohol y apuestas ya
   venían bloqueadas) y **mensaje de consentimiento europeo publicado** en Privacidad y mensajes, para ShowBies, en
   inglés y español, con Consentir, **No consentir** (elegido por Ivan, para todos los países: lo pide la AEPD) y
   Gestionar opciones, y la política de privacidad cargada en la app. Falta, si se quiere, el de los estados de EE. UU.
8. La 1.4.0: `admob` a `main`, AAB y subida.

**Anuncios automáticos (intersticiales): hechos el 6/10**, cuando ya andaban los videos (lo había decidido Ivan).
Van con el mismo puente y estas reglas: solo al salir de la derrota (OTRA VEZ, MENÚ, MEJORAS o el atrás), desde la 3.ª
partida terminada, como mucho uno cada 3 partidas y nunca si en esa partida se miró un video con premio. Google
castiga mostrarlos al abrir la app, al salir o en medio de la partida. La APK de prueba usa el intersticial de prueba
de Google, y el de verdad (`automatico`, en la tabla de arriba) se creó el 6/10.

**Antes del 30/6/2027, pasar al SDK Next-Gen** (`com.google.android.libraries.ads.mobile.sdk:ads-mobile-sdk`): ese día
Google deja de dar soporte al clásico, y el 30/6/2028 lo apaga. Con el puente es un archivo Java y una línea de Gradle.

**Ya no hay que quedarse en Unity 6000.3.16 o menos**: lo del issue 4212 era del plugin. Al subir de versión, eso sí,
Unity 6000.3.17 pasa a AGP 9: hay que volver a copiar la plantilla de Gradle (como ya dice CLAUDE.md por la reseña) y
probar que la librería compila.
