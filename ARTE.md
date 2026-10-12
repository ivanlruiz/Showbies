# La dirección de arte

Lo que dijo Gemini como director de arte (chat "Revisión Artística para ShowBies"), con lo hecho y lo que falta. Las
restricciones no cambian: no hay artista (formas simples en código o imágenes de Nano Banana recortadas), render
built-in, el Redmi Note 8 en calidad Medium, hasta 35 zombis, la cámara de arriba a 70° (un zombi mide unos 40 px).

## La primera revisión (9-10/10)

Hecho: las sombras falsas en el teléfono, la remera blanca y el aura del héroe, el borde de luz de los zombis (por
vértice, en vez de un contorno de dos pasadas), las balas por tramos con su charco de luz y las cajas como dibujos.

## La segunda revisión (12/10)

Con el juego como estaba el 12/10 (capturas de la ficha, la horda, el cementerio, el jefe y la elección de cartas).
Lo que dijo que ya funciona: el aura, las sombras falsas y el borde de luz "resolvieron el problema de fondo" (el
jugador y la acción se leen al instante), y las balas de colores pintando el piso son el jugo arcade que pedía. De la
lista anterior siguen valiendo todas: el jefe "sigue viéndose como un modelo estirado" y las letras de las capturas de la
ficha, con contornos negros duros, compiten con el neón.

### El top 10 nuevo, por prioridad

| | mejora | qué | costo |
|---|---|---|---|
| 1 | Viñeta en el HUD | una imagen negra en los bordes con el centro transparente, alfa 40-50 % | nulo; enfoca el centro y disimula lo que aparece por los bordes |
| 2 | Silueta del jefe | un cubo o cilindro enorme (1,5 × 0,5 m, rojo emisor) en la mano, como un arma | bajo; escalado se ve barato, necesita algo que rompa la silueta |
| 3 | Daño al jefe | una barra roja oscura (#660000) detrás de la roja, que baja con un lerp de 0,5 s | bajo, código (hoy hay una blanca que baja despacio) |
| 4 | Feedback de poderes | al curarse o agarrar una caja, el material del héroe destella verde o amarillo 0,15 s | nulo |
| 5 | Letras de las capturas de la ficha | cambiar el contorno negro duro por un brillo magenta o celeste (#FF00FF / #00FFFF) | bajo; hoy "remite a WordArt" |
| 6 | Fondo de las cartas | un degradé radial del color de la carta al 10-15 % adentro | nulo |
| 7 | Jerarquía del texto de las cartas | la descripción gris (#AAAAAA) y un 20 % más chica que el título | nulo |
| 8 | Pulso del aura | la opacidad del aura del héroe de 0,4 a 0,8, ida y vuelta en 1 s | bajo |
| 9 | Contorno en los números de daño | un contorno negro suave de 1 px | nulo (TextMeshPro); los amarillos y rojos se empastan |
| 10 | Silueta del tanque | el modelo a 1,3 en X y Z (Y igual) y dos esferas oscuras (#222222) en los hombros | bajo |

### El arma que cambia con el daño (etapa 3 de las cartas)

**No modelar cinco armas**: a 15 px, desde arriba y disparando, un revólver y una pistola son la misma mancha. Lo que
se tiene que ver es lo que el arma emite:

- **El modelo**: siempre un bloque gris oscuro (#333333, que la segunda revisión pasó a #666666); solo cambia el largo del cañón: pistola 0,4 m, escopeta 0,6 m
  (y más ancha), rifle 1,0 m.
- **Un "LED" de neón**: un quad diminuto pegado al arma, `Unlit/Color`, del color del tramo del daño.
- **El fogonazo**, que es donde va el esfuerzo (un quad de 0,05 s frente al cañón): la pistola y el revólver, un
  círculo chico; la escopeta, un cono ancho de 0,1 s; el rifle, una estrella o cruz de cuatro puntas; el arma de
  energía, sin fogonazo: anillos que se expanden.
- **La cadencia y el sonido son el 80 % de la mejora que se siente**: pop… pop de la pistola, trrrr del rifle, ¡BAM! de
  la escopeta.

**Hecho el 12/10**, y ese mismo día Gemini revisó el resultado (las cinco de cerca, en la partida al doble, el aviso y
los sonidos): el fogonazo se distinguía jugando, pero el arma no (el gris oscuro se fundía con el piso, el LED era una
línea de un píxel), el revólver parecía "la pistola con la bala más grande" y a los sonidos les faltaba peso. Su top 10,
**hecho entero el 12/10** (ver El arma cambia con el daño en `CLAUDE.md`):

1. El LED tres veces más ancho: cubre el lomo de la corredera.
2. El LED late con cada tiro: ×2,5 y de vuelta en 0,05 s.
3. El arma gris medio (#666666) en vez de #333333.
4. El fogonazo del revólver, un óvalo a lo largo del cañón (1,5 de largo por 0,5 de ancho).
5. El aviso de arma nueva entra de un salto (0 → 1,3 → 1 en 0,2 s) con una pausa de impacto de 0,2 s.
6. Un bombo grave debajo del revólver y la escopeta.
7. La cruz del rifle en X (girada 45°).
8. Un clack de metal al principio del rifle.
9. Un núcleo brillante en el medio del anillo de la de energía.
10. Con la furia, el LED parpadea en rojo cada 0,2 s.

Queda de esa revisión, fuera del top 10: un zumbido que quede sonando después del zap de la de energía.

### Las balas de cada arma (12/10)

Ivan: "que no sean todos cubitos" (para la escopeta se imaginaba proyectiles muy chiquitos que recorren poca distancia).
Gemini, con las fotos de las balas de noche: cada arma tiene que cambiar también cómo dispara, no solo cómo se ve. **Hecho
el 12/10** (la tabla y los detalles, en Lo que tira cada arma de `CLAUDE.md`):

- La pistola, la de siempre; el revólver, más grande y rápido; la escopeta, cuatro perdigones chicos en abanico que
  llegan a 7 m; el rifle, una trazadora larga, muy rápida; la de energía, una onda ancha y lenta que atraviesa dos zombis.
- El revólver y la escopeta tiran más lento y el rifle más rápido, para que cada arma tenga su ritmo; el daño por bala lo
  compensa (lo confirmó Gemini: una compra nunca puede bajar el daño por segundo).
- La escopeta reparte el tiro a un tercio por perdigón (133 % con los cuatro adentro); las cartas, en cada perdigón; el
  disparo doble le suma dos a los costados y la de atrás tira uno gordo.
- El charco de luz: el rifle sin, la escopeta uno solo por tiro, más grande y tenue (el Redmi no aguanta el piso lleno de
  transparencias).
- La crítica, blanca, la mitad más grande y con estela de chispas (la roja se confundía con los tramos 4 y 5 y la sangre).
- Las medidas, en proporción a la bala de 0,09 m de hoy (Ivan las había pedido más chicas); el auto-apuntado, hasta los
  7 m de la escopeta.

### Las tarjetas de las cartas

Hecho el 12/10, junto con el 6 y el 7 de la lista: **sin el círculo relleno** (el fondo ya es oscuro y el círculo era
un agujero; parecían señales de tránsito), **el ícono de neón** del color de la carta, con su brillo, dentro de **un
anillo fino** del mismo color.
