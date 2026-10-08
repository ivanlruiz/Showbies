# Notas de la versión 1.5.0 (8) para Play

Las que van en "Novedades" de la versión de producción. Máximo 500 caracteres por idioma (contados abajo). Como la
1.4.0, la de Latinoamérica (es-419) no es la de España: allá se escribe "zombies" y se "juntan" las cosas.

## en-US
```
What's new in 1.5.0
• Halloween event, October 24 to November 9: pumpkins everywhere, zombies in costume and candy to collect
• Fill the candy track for coins and a pumpkin hat you keep forever
• New in OPTIONS: wear or hide your pumpkin hat
• Fixes and improvements
```

## es-ES
```
Novedades de la 1.5.0
• Evento de Halloween, del 24 de octubre al 9 de noviembre: calabazas por todas partes, zombis disfrazados y caramelos para recoger
• Llena la fila de caramelos y gana monedas y un sombrero de calabaza que es tuyo para siempre
• Nuevo en OPCIONES: ponte o quítate el sombrero de calabaza
• Arreglos y mejoras
```

## es-419
```
Novedades de la 1.5.0
• Evento de Halloween, del 24 de octubre al 9 de noviembre: calabazas por todas partes, zombies disfrazados y caramelos para juntar
• Llena la fila de caramelos y gana monedas y un sombrero de calabaza que es tuyo para siempre
• Nuevo en OPCIONES: ponte o quítate el sombrero de calabaza
• Arreglos y mejoras
```

## Antes de enviarla

- **Ivan prueba la APK** de la rama `halloween` (el evento forzado en el paquete `.prueba`).
- `bundleVersion` 1.5.0 y `AndroidBundleVersionCode` 8 en `ProjectSettings.asset` (el 7 ya está usado: lo dice
  `ultimo_aab.txt`), el AAB, y `halloween` pasa a `main` cuando Ivan lo confirme.
- **Tiene que estar publicada antes del 24/10**: el evento arranca solo ese día, y la tarjeta de contenido promocional
  exige que el evento esté en el juego en sus fechas. Mandarla a revisión a más tardar el 17/10 deja margen.
- **No cambia ninguna declaración**: el evento no junta datos nuevos (los caramelos y el sombrero van en el
  `progreso.json` del teléfono, como todo lo demás).
- **La ficha**: los títulos y las descripciones nuevas de `ficha_propuesta.md` pueden ir antes, por su cuenta; el
  bloque y la descripción corta de Halloween, el 24/10. La tarjeta del evento va aparte (Crecimiento > Contenido
  promocional), con `Builds/halloween/arte_tarjeta_1920x1080.png`.
