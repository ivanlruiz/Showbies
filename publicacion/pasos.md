# Publicar ShowBies en Google Play

Estado al 2026-09-16. Lo tildado ya está hecho en el proyecto; lo demás es en la web de Play Console,
que es la parte que no puedo hacer yo.

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

La primera versión sale **sin anuncios**, y es a propósito: el proveedor real (AdMob) todavía no está integrado,
y **AdMob no sirve anuncios de verdad hasta que la app esté publicada y vinculada a su ficha**. O sea que el
orden natural es publicar → crear la cuenta de AdMob → vincular la app → integrar el SDK → actualizar.

Cuando llegue ese momento, además hay que:
- Volver a la sección **Anuncios** de Play Console y declarar que sí tiene.
- Rehacer **Seguridad de los datos** (AdMob recolecta identificadores y datos de uso).
- Agregar el **consentimiento (UMP)** para usuarios del Espacio Económico Europeo y Reino Unido.
- Publicar **app-ads.txt** si tenés dominio.
- **No subir Unity a 6000.3.17 o posterior**: el plugin de AdMob está roto ahí (issue #4212 del repo del
  plugin). Hoy estás en 6000.3.14, que está bien.
