# Marketing

Lo que no es el juego pero lo muestra: el logo y las piezas para la ficha de Play, YouTube
y TikTok. Vive fuera de `ShowBies1/` a propósito, para que Unity no lo importe.

## `logo.py` — el logo de ShowBies

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
