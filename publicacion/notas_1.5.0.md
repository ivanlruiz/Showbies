# Notas de la versión 1.5.0 (10) para Play

Las que van en "Novedades" de la versión de producción. Máximo 500 caracteres por idioma (contados: en-US 429,
es-ES 469, es-419 471). Como la 1.4.0, la de Latinoamérica (es-419) no es la de España: allá se escribe "zombies".

## en-US
```
What's new in 1.5.0
• Halloween event until Nov 9: pumpkins, zombies in costume and candy that fills a prize track up to a pumpkin hat
• Treasure zombie: a rare golden zombie that runs away. Catch it and it rains coins!
• Perfect waves pay double, and your bullets grow and glow with your damage
• Arrows to boxes and the last zombies, and upgrades from the pause menu
• Buildings and obstacles now block the way, plus many fixes
```

## es-ES
```
Novedades de la 1.5.0
• Evento de Halloween hasta el 9 de noviembre: calabazas, zombis disfrazados y caramelos que llenan una fila de premios hasta un sombrero de calabaza
• Zombi del tesoro: uno dorado y raro que huye. ¡Atrápalo y llueven monedas!
• Las oleadas perfectas pagan el doble, y tus balas crecen y brillan con tu daño
• Flechas hacia las cajas y los últimos zombis, y mejoras desde la pausa
• Los edificios y los obstáculos cortan el paso, y muchos arreglos
```

## es-419
```
Novedades de la 1.5.0
• Evento de Halloween hasta el 9 de noviembre: calabazas, zombies disfrazados y caramelos que llenan una fila de premios hasta un sombrero de calabaza
• Zombi del tesoro: uno dorado y raro que huye. ¡Atrápalo y llueven monedas!
• Las oleadas perfectas pagan el doble, y tus balas crecen y brillan con tu daño
• Flechas hacia las cajas y los últimos zombies, y mejoras desde la pausa
• Los edificios y los obstáculos cortan el paso, y muchos arreglos
```

## Lo que trae (para tener a mano, no va en Play)

- **Halloween** desde que sale hasta el 9/11 (arranca el 9/10: Ivan lo quiso ya, no el 24/10): calabazas (82 en el
  mapa), zombis disfrazados, caramelos, la fila de cinco premios y el sombrero de calabaza; en la derrota, los caramelos
  de la partida y lo que falta para el premio.
- **El zombi del tesoro**, las **oleadas perfectas** (bono doble), el **final de oleada** festejado, los **avisos** de mejora
  y de récord, los **premios cobrados solos** avisados, el **desafío semanal cumplido** avisado, las **balas por tramos de
  daño** (más chicas, con luz en el piso) y las **flechas del borde**.
- **MEJORAS en la pausa** de las oleadas, **cajas dibujadas** (corazón, balas, rayo), **paredes** en los edificios y
  **obstáculos** con collider, zombis con **colores vivos**, el **arrastre** arreglado, el jugador a 11,5.
- La pradera **sin faroles**, y `androidx.activity` 1.9.3 y `androidx.fragment` 1.8.5 (Play avisó que la 1.4.0 usaba
  la 1.0.0 y la 1.1.0, obsoletas).
- Los 10 arreglos de la revisión del 9/10 (anuncios vencidos, el reloj atrasado en Halloween, la invocación del jefe en
  el cementerio y el resto: `auditorias/9-10/informe.md`).

## Antes de enviarla

- Ivan probó la APK el 9/10 y anda bien. `bundleVersion` 1.5.0 y `AndroidBundleVersionCode` **10** (el 8 se armó con
  Halloween el 24/10 y el 9 sin `androidx.fragment` nuevo; ninguno se subió); el AAB está en `Builds/ShowBies.aab`.
- **Halloween arranca con la actualización**: el evento corre del 9/10 al 9/11, así que en cuanto se publica ya está.
  Cuanto antes se mande a revisión, más días de evento.
- **No cambia ninguna declaración**: el evento y lo nuevo no juntan datos nuevos (todo va en el `progreso.json` del
  teléfono, como siempre), y los anuncios son los mismos.
- **La ficha**: los títulos y las descripciones nuevas de `ficha_propuesta.md` pueden ir antes, por su cuenta; el
  bloque y la descripción corta de Halloween, cuando se publica. La tarjeta del evento va aparte (Crecimiento > Contenido
  promocional), con `Builds/halloween/arte_tarjeta_1920x1080.png`.
- `halloween` pasa a `main` cuando Ivan lo confirme.
