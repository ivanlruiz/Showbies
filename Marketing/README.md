# Marketing

Lo que no es el juego pero lo muestra: el logo y las piezas para la ficha de Play, YouTube
y TikTok. Vive fuera de `ShowBies1/` a propósito, para que Unity no lo importe.

## `logo_toxico/` — el logo de ShowBies (desde el 23/10)

El logo es **"Tóxico Arcade"**: SHOW en crema con dos agujeros de bala y BIES en verde tóxico, con ojos adentro de la B,
sobre un bloque oscuro con un borde de neón que va del rosa al celeste. Lo diseñó Argon y lo dibujó Nano Banana (Gemini)
el 23/10, después de que a Ivan no le gustaran ni el dorado de abajo ni dos pruebas de neón; Ivan lo eligió entre dos.

- `original_nano_banana.jpg`: lo que bajó de Gemini a tamaño completo (2752 × 1536), sobre el fondo liso `#0b0618`.
  Trae en las esquinas un fantasma casi invisible de la captura que se le pasó de referencia.
- `recortar_logo.py`: lo deja con transparencia en `ShowBies1/Assets/Sprites/UI/LogoShowBies.png`, a 1721 × 532
  como el de antes (el quad del menú no cambia), y escribe `vista_sobre_blanco.png` para revisar el recorte.

```bash
cd Marketing/logo_toxico
python recortar_logo.py
```

El recorte no es por color: el bloque oscuro de detrás de las letras es casi del color del fondo y quedaría agujereado.
Lo que se aparta mucho del fondo (letras y borde de neón) es sólido, junto con todo lo que ese borde encierra; afuera, el
resplandor sale de cuánto se aparta del fondo, con el color despremultiplicado para que no ensucie el cielo del menú, y
lejos del logo no queda nada (así se va el fantasma).

## `logo.py` — el logo dorado de antes

**No lo corras sin querer**: escribe `LogoShowBies.png` y pisa el logo de ahora. Queda por si se quiere volver al dorado.

El logo está **dibujado en código**, no es una imagen hecha a mano: se corre el script y
sale. Así se le cambia el arco, el grosor del contorno o los colores tocando un parámetro,
y se vuelve a generar en cualquier tamaño sin perder calidad.

```bash
cd Marketing
python logo.py
```

Escribe dos cosas:

| qué | dónde |
|---|---|
| el logo del juego, con transparencia | `ShowBies1/Assets/Sprites/UI/LogoShowBies.png` |
| dos vistas previas para mirarlo | `Marketing/vista_placa.png` y `vista_franja.png` (gitignoreadas) |

El PNG del juego **está versionado**: es el que usa el menú (ver abajo). Si se regenera,
hay que dejar que Unity lo reimporte y revisar que el importador siga en `npotScale = None`
— con la configuración por defecto lo lleva a la potencia de dos más cercana (2048x512) y
el logo sale estirado a lo ancho.

### Cómo está hecho

Por capas, de atrás hacia adelante, que es el tratamiento de los logos de juegos casuales:

1. **arco** — las letras se acomodan sobre una curva y se inclinan con ella
2. **extrusión** — la silueta repetida hacia abajo arma el volumen
3. **contornos** — uno grueso casi negro y otro marrón adentro, que es lo que hace que lea
   en chico, que es todo en un teléfono
4. **cara** — degradado amarillo arriba a naranja abajo
5. **bisel** — una banda clara pegada al borde de arriba y una oscura abajo
6. **brillo** — una franja diagonal suave sobre la cara
7. **mordisco** — le falta un pedazo a una letra, que es el guiño al género

Todo se dibuja al triple de tamaño y se achica al final: los contornos se hacen dilatando
máscaras y en el tamaño final eso quedaría escalonado.

**El mordisco va en la H y no en cualquier letra.** Mordida, la O se leía como una U y la E
como una F rota. La H tiene dos palos y un travesaño, así que le falta un pedazo y sigue
siendo una H. Si se cambia de letra, mirar que la palabra siga diciendo lo mismo.

El goteo (`con_gotas`) sigue en el código pero está apagado: se probó y no gustó.

### Dónde se usa el logo

- **El menú del juego**: `FondoMenu/Titulo` es un quad con `Materiales/LogoMenu.mat`
  (shader `ShowBies/LogoEnLaNiebla`), que `TituloEnLaNiebla` esconde a medias en la niebla.
  Antes era el nombre escrito con un TextMeshPro 3D.
- **Los vídeos**: la placa final del tráiler y la franja fija de la versión vertical.

## Los vídeos

Los scripts que arman el tráiler y la versión vertical **no están acá**: dependen de los
cuadros grabados en el editor, que son cientos de megas y se tiran al terminar. Si hay que
rehacerlos, se vuelven a grabar: una sesión de play con `Time.captureFramerate` fijo y
`AudioRenderer` para el sonido, y después ffmpeg junta todo.

## `kingball/` — la miniatura de YouTube de Kingball

Kingball es el tema de Ivan que suena en el menú. Para su video de YouTube (fondo #0b0618, KINGBALL en Arial Bold
rosa #ff2bd6 con la sombra celeste #00e5ff corrida y la onda del audio) Argon diseñó tres miniaturas de 1280 × 720
(19/10, `pedidos_argon.md`, con los pedidos para Nano Banana): **A**, el zombi con anteojos de neón y auriculares (la
que recomienda: a 168 × 94 px, como se ve en el teléfono, es la que más se lee); **B**, el héroe contra la horda sobre
un ecualizador; **C**, el héroe con un teclado colgado y zombis detrás.

Nano Banana hace la imagen sin letras, con la mitad izquierda libre (`fondo_*_nano_banana.png`), y
`miniatura_kingball.py` le pone encima el logo del juego, una píldora OST, KINGBALL con la misma letra y los mismos
colores del video (con un halo oscuro detrás) y, en la A, una tira de la onda del tema (ocho segundos desde el golpe,
leídos del WAV del proyecto de FL Studio). Los comandos de las tres están al principio del script; también saca la
vista a 168 px de cada una (gitignoreada). La esquina de abajo a la derecha queda para la duración de YouTube.

**Después Ivan pidió que la hiciera Argon entera, sin la palabra KINGBALL** (YouTube ya muestra el título debajo):
`miniatura_argon.jpg`, de la dirección A, con el logo y la píldora OST dibujados por Nano Banana
(`argon_entera_nano_banana.png`, a 1024 × 572, llevada a 1280 × 720 con el recorte de `fondo` del script).
