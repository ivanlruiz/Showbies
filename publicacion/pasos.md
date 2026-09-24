# Publicar ShowBies en Google Play

Estado al 2026-09-16. Lo tildado ya está hecho en el proyecto; lo demás es en la web de Play Console,
que es la parte que no puedo hacer yo.

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
      Salió ESRB E10+, ClassInd 10 y GRAC 12+.
- [x] **Política de privacidad**: `https://ivanlruiz.github.io/showbies-privacidad/`, desde el repo público
      `ivanlruiz/showbies-privacidad` (`index.html` y `privacidad.html`, copias de `privacidad.html` de acá: al
      cambiar uno hay que actualizar los otros). Se mudó ahí el 19/9 para poder pasar este repo a privado. El link
      nuevo está **guardado sin enviar** en Play Console, para no reiniciar la revisión de la versión 5. El viejo
      (`ivanlruiz.github.io/Showbies/privacidad.html`, rama `gh-pages`) sigue vivo hasta que Play muestre el nuevo.
- [ ] **Seguridad de los datos**: respondida (no recopila ni comparte) pero sin enviar; el guardado final no
      se aplicó desde la extensión.
- [x] **Ficha de Play Store** (borrador): textos en inglés (principal), español de España y de Latinoamérica
      (`ficha.md`); icono 512, banner 1024x500 y 4 capturas 1920x1080 en inglés, en `Builds/ficha/` (fuera de git).

- [x] **Prueba cerrada (Alpha)**: 177 países y notas de la versión en los tres idiomas. El AAB (versionCode 4,
      1.1.0) salió de `main`; se sube a mano porque pesa 32 MB. Faltan los testers (12 como mínimo) y enviar a revisión.
- [x] **Versión 5 (1.2.0)** enviada a revisión el 18/9/2026, también de `main`: menú con fondo vivo, botones en
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
      (gitignoreado). **Hacé una copia fuera de la computadora hoy mismo.** Si se pierde, no se puede volver a
      actualizar la app publicada nunca más; sólo se salva si activás Play App Signing al subir la primera vez,
      que es lo que conviene hacer.
- [x] **Target API 36**, obligatorio para apps nuevas desde el 31/8/2026. Estaba en "automático", que depende de
      qué SDK tenga instalado la máquina.
- [x] **64 bits** (ARM64 + IL2CPP), que Play exige.
- [x] **versionCode 4** y versión **1.1.0**. Cada subida a Play necesita un versionCode mayor que el anterior:
      si rechazan un AAB y subís otro, hay que volver a subirlo.
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
`Build > Android AAB (release)` en Unity genera `Builds/ShowBies.aab` firmado con tu keystore. Al subirlo por
primera vez, **activá Play App Signing** (es el seguro contra perder el keystero).

## Anuncios

**AdMob ya está creado** (16/9/2026, cuenta de Play Console). No son secretos: van en el código.

| qué | ID |
|---|---|
| App ShowBies (Android, sin tienda vinculada todavía) | `ca-app-pub-5295383586829735~6982656335` |
| Bonificado `revivir` (recompensa 1 "revivir") | `ca-app-pub-5295383586829735/1512416812` |
| Bonificado `duplicar_derrota` (recompensa 1 "monedas_x2") | `ca-app-pub-5295383586829735/8640931940` |
| Bonificado `regalo_x2` (recompensa 1 "monedas_x2", el video de la recompensa diaria) | `ca-app-pub-5295383586829735/6299120897` |

Los nombres de los bloques son los de `LugarAnuncio`. Mientras la app no esté vinculada a su ficha publicada, AdMob
limita los anuncios: para probar se usan los bloques de prueba de Google. Al publicar en producción, vincular la
tienda desde **Configuración de la app → Añadir tienda** (`com.ivanruiz.showbies`).

La primera versión sale **sin anuncios**, y es a propósito: el proveedor real (AdMob) todavía no está integrado,
y **AdMob no sirve anuncios de verdad hasta que la app esté publicada y vinculada a su ficha**. O sea que el
orden natural es publicar → crear la cuenta de AdMob → vincular la app → integrar el SDK → actualizar.

Cuando llegue ese momento, además hay que (casi todo lo sumó la auditoría del 24/9):
- **Declarar los anuncios antes de subir el AAB, no después.** El SDK agrega por su cuenta el permiso `AD_ID` al
  manifiesto y, con la declaración en "no", Play bloquea la versión. El orden: la política nueva publicada, con
  fecha → rehacer **Seguridad de los datos** (AdMob recopila y comparte identificadores del dispositivo, ubicación
  aproximada, interacciones y diagnósticos, para publicidad, analíticas y prevención de fraude) → **Anuncios: sí**
  e **ID de publicidad: sí** → recién ahí subir. Y sacar de la ficha (`ficha.md`) el "no recopila tus datos".
- **Tope de clasificación**: `MaxAdContentRating` en PG (T como mucho) en la `RequestConfiguration`, antes del
  primer pedido, y en AdMob bloquear las categorías sensibles (juegos de azar, citas, alcohol, sexualidad, dinero
  fácil). La app es E10+ con público desde los 13, y sin tocar nada AdMob sirve hasta anuncios para adultos.
- **Consentimiento (UMP)** para el Espacio Económico Europeo, el Reino Unido y Suiza, más el mensaje de los estados
  de EE. UU. en AdMob. El SDK no pide anuncios hasta que UMP termina, así que `Inicializar` tiene que poder
  esperarlo. Mostrá el formulario en el menú desde la segunda partida terminada (antes no hay videos), no al abrir
  la app por primera vez, y sumá a OPCIONES un botón **PRIVACIDAD** que aparezca cuando UMP lo pida, para poder
  retirarlo.
- **Ids de bloque y de prueba en `ConfigAnuncios`**: un campo por lugar con los de la tabla de arriba, y la APK de
  prueba (el paquete `.prueba`) siempre con el bloque de prueba de Google, `ca-app-pub-3940256099942544/5224354917`,
  con tu teléfono registrado como dispositivo de prueba. Mirar y tocar anuncios reales propios es tráfico no
  válido, y AdMob cierra cuentas por eso. El AAB se tiene que negar a salir con un id de prueba.
- **app-ads.txt, sin dominio pago**: un repo público `ivanlruiz/ivanlruiz.github.io` con GitHub Pages y
  `app-ads.txt` en la raíz, con la línea `google.com, pub-5295383586829735, DIRECT, f08c47fec0942fa0`. El sitio
  web de la ficha tiene que caer en `ivanlruiz.github.io` (la URL de la política sirve). AdMob lo verifica recién
  después de vincular la app a la ficha, y tarda hasta 24 h.
- **Inicializar y precargar al arrancar**: `ServicioAnuncios.Arrancar` ya se llama al abrir la app, así que es en
  el `Inicializar` del proveedor real donde se arranca el SDK y se piden los videos. Cada video de AdMob sirve una vez y
  vence a la hora: hay que pedir otro después de mostrarlo y al vencer. Ni `Inicializar` ni `Listo` pueden tirar
  excepciones: `Listo` se pregunta en el golpe que mata al jugador.
- **Volver a preguntar `Listo` mientras las ofertas están abiertas**: hoy cada lugar pregunta una sola vez (la
  derrota al abrirse, el revivir al morir, la diaria al cobrar), porque el proveedor falso siempre está listo. Con
  la red real, un video que termina de cargar un segundo tarde no aparece, y el revivir, al volver de un video
  cerrado, habilita el botón sin preguntar.
- **Un solo resultado final por video**: el proveedor avisa una vez, al cerrarse el anuncio, con `Recompensado` si
  el premio llegó en cualquier momento (AdMob lo manda antes del cierre, y avisar ahí reanudaría el juego detrás
  del anuncio). Si el SDK puede no avisar nunca (volver a la app desde el ícono con el video abierto), hace falta
  un vigía que lo resuelva como cerrado al volver.
- **No subir Unity a 6000.3.17 o posterior**: el plugin de AdMob está roto ahí (issue #4212 del repo del
  plugin). Hoy estás en 6000.3.14, que está bien.
