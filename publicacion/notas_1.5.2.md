# Notas de la versión 1.5.2 (12) para Play

Las que van en "Novedades" de la versión de producción. Máximo 500 caracteres por idioma (contados: en-US 376,
es-ES 439, es-419 442). Como siempre, la de Latinoamérica (es-419) escribe "zombies".

## en-US
```
What's new in 1.5.2
• Smoother on low-end phones
• Auto-aim on phones: just move and your hero shoots the nearest zombie (turn it off in Options)
• Stronger start: 6 shots per second from the first wave
• Fairer waves after the first boss: zombie health and hits grow slower
• Your hero wears white and glows, zombies have a bright outline, and new features unlock as you play
```

## es-ES
```
Novedades de la 1.5.2
• Más fluido en teléfonos de gama baja
• Auto-apuntado en el teléfono: muévete y tu héroe dispara al zombi más cercano (se apaga en Opciones)
• Arranque más fuerte: 6 tiros por segundo desde la primera oleada
• Oleadas más justas después del primer jefe: la vida y el golpe de los zombis crecen más despacio
• Tu héroe viste de blanco y brilla, los zombis tienen un borde de luz y lo nuevo aparece a medida que juegas
```

## es-419
```
Novedades de la 1.5.2
• Más fluido en teléfonos de gama baja
• Auto-apuntado en el teléfono: muévete y tu héroe dispara al zombie más cercano (se apaga en Opciones)
• Arranque más fuerte: 6 tiros por segundo desde la primera oleada
• Oleadas más justas después del primer jefe: la vida y el golpe de los zombies crecen más despacio
• Tu héroe viste de blanco y brilla, los zombies tienen un borde de luz y lo nuevo aparece a medida que juegas
```

## Lo que trae (para tener a mano, no va en Play)

Casi todo sale de la revisión de Gemini del 10/10 (`PLAN.md`), con lo que eligió Ivan:

- **Los tirones del Redmi Note 8**: el jugador interpola, la cámara va en `LateUpdate`, el paso de física máximo es 0,1 s
  y el guardado del principio de cada oleada va en otro hilo. OPCIONES > MOSTRAR FPS dice ahora el peor cuadro y los
  tirones.
- **Auto-apuntado** en el teléfono, prendido de fábrica (OPCIONES > AUTO-APUNTADO).
- **Cadencia base 6** (era 4), +1 por compra hasta 22.
- **La curva desde la oleada 11**: vida ×1,08 y golpe ×1,04 por oleada (eran ×1,11 y ×1,07); los números los eligió
  Gemini entre tres candidatas simuladas (`simulaciones/curva_1.5.2`).
- **El héroe**: remera blanca y un aura bajo los pies. **Los zombis**: un borde de luz de su color.
- **Sombras falsas en el teléfono** (un disco bajo cada personaje en vez de la pasada de sombras).
- **Lo que aparece de a poco**: las misiones del día en la oleada 5; el bestiario, los logros, Discord, el desafío
  semanal y el botón de monedas por video en el nivel 5. En el teléfono no hay botón SALIR.

## Antes de enviarla

- Sigue el ícono de Halloween (es el de la 1.5.1). Después del 9/11, volver al de base con otra versión y cambiar el de
  la ficha.
- Es la primera que trae el aviso de actualización dentro del juego (`ActualizacionDePlay`): quien la tenga se va a
  enterar de la próxima desde el menú. A quien tiene la 1.5.1 o antes, esta le llega como siempre, por Play.
