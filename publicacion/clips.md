# Clips para TikTok, Shorts y Reels

El 15/10/2026 Ivan preguntó cómo hacer que juegue gente de afuera, y se arrancó por videos cortos. Los graba
`GrabarClips` (`ShowBies1/Assets/Editor/`) desde el juego, en vertical, con Unity en segundo plano; los publica Ivan. Qué
grabar lo decidió Argon (Gemini en Máx., chat "Análisis de diseño para ShowBies").

## Los cuatro clips de Argon, en orden de prioridad (15 s cada uno, 9:16)

Todos en la ciudad (oleadas 21 a 30) salvo el de las armas: el asfalto oscuro con las líneas amarillas y los charcos
magenta y cian es lo que mejor rinde en vertical y sin interfaz.

1. **"Nivel 1 contra nivel máximo"** (la fantasía de poder, lo que más convierte en incrementales y survivor-likes).
   - 0 a 4 s: oleada 25, pistola, cadencia mínima, sin cartas ni imán; la horda lo acorrala, el mundo se va a blanco y
     negro y muere a los 3,5 s. Texto: LEVEL 1 ROOKIE 💀 / NIVEL 1: NOVATO 💀.
   - 4 a 12 s, corte seco: la misma oleada con el arma de energía, la cadencia al tope, la furia y las cartas vistosas
     (disparo doble, perforante, explosiva, quemadura, hielo, dos drones); avanza por la calle barriendo la horda. Texto:
     MAX UPGRADES + FULL CARDS 🔥 / MEJORAS AL MÁXIMO 🔥.
   - 12 a 15 s: pasa sobre el tendal de monedas con el imán al máximo y se las chupa todas; el cierre.
2. **"Esquivá al jefe y contraatacá en furia"** (que el juego tiene jefes con mecánicas).
   - 0 a 4 s: oleada 30; el jefe salta, cae, marca la franja roja de la carga, el héroe la esquiva a último momento y
     el jefe queda aturdido. Texto: WAIT FOR THE MISTAKE... / ESPERA EL ERROR...
   - 4 a 11 s: furia, escopeta o rifle con disparo doble, crítico al 100 % y explosiva: lluvia de críticos. Texto: RAGE
     MODE: 2X DAMAGE ⚡ / ¡MODO FURIA: DAÑO X2! ⚡
   - 11 a 15 s: el jefe muere con la pausa de impacto, estalla la montaña de monedas y el imán las junta.
3. **"La codicia mata: cazando al zombi del tesoro"** (la lluvia de monedas retiene hasta el final).
   - 0 a 5 s: oleada 24; el tesoro huye entre tanques y zombis, el héroe con rifle, perforante e hielo se mete entre la
     horda. Texto: 12 SECONDS BEFORE HE ESCAPES 🏃‍♂️💰 / 12 SEGUNDOS ANTES DE QUE ESCAPE 💰
   - 5 a 10 s: granada que abre un hueco, furia, y lo remata antes de que se vaya. Texto: DON'T LET HIM GO! / ¡QUE NO SE
     ESCAPE!
   - 10 a 15 s: la lluvia del tesoro y la de la oleada, y el imán aspirando el río de oro.
4. **"Halloween: la escalada de armas"** (cuatro cortes de 3 s, aprovechando el evento hasta el 9/11): el héroe con el
   sombrero de calabaza; oleada 9 con revólver e hielo, 16 en el cementerio con escopeta, disparo doble y escudo, 24 en
   la ciudad con rifle, hacia atrás y perforante, y 28 con energía, drones, explosiva y furia limpiando la pantalla.
   Texto: WHICH WEAPON DO YOU PICK? 🔫🎃 / ¿QUÉ ARMA ELEGÍS? 🔫🎃, con REVOLVER → SHOTGUN → RIFLE → ENERGY abajo; al final,
   HALLOWEEN EVENT LIVE NOW / EVENTO DE HALLOWEEN ACTIVO.

## Dónde están

- **Clip 1** (16/10): `Builds/clips/salida/clip1_en.mp4` y `clip1_es.mp4`, regrabado con el editor abierto. La primera
  versión (15/10, en -batchmode) Ivan la vio "re bugeada": las balas se iban para otro lado (un bug del juego, ya
  arreglado: ver Pipeline de disparo en CLAUDE.md), el blanco a mano podía estar fuera del cuadro o encima del héroe y
  el zoom de 1,8 agrandaba de más los números y la onda de energía. El golpe de la versión nueva de Kingball sigue a los
  29,3 s: `musica_desde` 25,5 vale.
- **Clips 2, 3 y 4** (17/10): `clip2_*.mp4` (13,8 s), `clip3_*.mp4` (11 s) y `clip4_*.mp4` (15 s), en la misma carpeta,
  grabados con el editor abierto (`GrabarClips`, ShowBies > Clips). Lo que se hizo a mano para que entre en un video:
  - **El jefe** (oleada 30, sin la horda: la oleada se para y se despejan los demás): aparece a 6,5 m al norte y ataca
    cada 0,4 s en vez de 5; la carga dura 0,45 s en vez de 1 (de 16 m terminaba fuera del cuadro vertical) y, cuando la
    carga lo deja aturdido, queda aturdido hasta morir. El héroe esquiva leyendo el estado de `JefePatrones` (sale del
    anillo hacia atrás y de la línea de costado, a último momento) y recién ahí prende la furia y tira, desde 3,5 m al
    sur del jefe. La vida del jefe va de llena a cero en 5 s (la de la oleada 30, 22.500, aguanta más que el clip).
  - **El tesoro** (oleada 24, la calle del medio de sur a norte): sale 6 m al norte del héroe, que lo sigue desde 2,5 m
    al sur entre la horda tirándole a los zombis y no a él (ni a lo que está en su dirección); granada al grupo más grande,
    furia a los 4 s, y recién ahí le tira: su vida baja a mano en 2,8 s. La primera versión le tiraba desde el principio,
    se le apilaban los números de daño y no se lo veía escapar (Ivan, 18/10).
  - **Las armas** (oleadas 9, 16, 24 y 28, Halloween forzado y el sombrero): la horda lo rodea 10 s y empieza a tirar
    4 s antes de grabar; con 1,5 s, quedaba tapado.
  - Mientras se grababan apareció el bug de los zombis que se subían encima del jugador quieto y salían volando: se
    arregló (la altura congelada, ver CLAUDE.md) y el 3 se regrabó con el arreglo.

- **La mezcla** (18/10, pedido de Ivan): Kingball al 90 % y los efectos al 75 %, con un limitador (eran 60 y 100 %: los
  efectos tapaban la música). **Cada clip sale también limpio** (`clipN_limpio.mp4`, sin textos ni cierre, con el mismo
  audio), para decorarlo con otra herramienta.

## Formato

- **Dos versiones de cada clip**: inglés para YouTube Shorts e Instagram Reels (Shorts reparte por el idioma del video,
  no por el país desde donde se sube) y español para TikTok (que reparte primero por la SIM y la IP: Argentina y
  vecinos).
- **El texto**: en el tercio de arriba (del 12 al 22 % desde arriba, centrado), de 4 a 5 palabras a la vez, mayúsculas
  gruesas blancas o amarillas con trazo negro y brillo de neón, como las capturas de la ficha. **Nada en el 25 % de
  abajo** (la cuenta, la descripción, la música) **ni en el 15 % de la derecha** (los botones).
- **El cierre, en los últimos 3 s, sin placa negra** (la gente pasa al siguiente antes de leer): la lluvia de monedas
  sigue de fondo oscurecida un 25 %, el logo grande arriba al centro y abajo FREE ON GOOGLE PLAY / Search: ShowBies
  (GRATIS EN GOOGLE PLAY / Buscalo como: ShowBies).
- **El audio**: Kingball y los efectos del juego, sobre todo los disparos pesados, las explosiones y la escalera de notas
  de las monedas (Argon pedía la música al 60 % y los efectos al 100 %; Ivan la prefirió más alta: ver arriba).

## Cómo publicar

YouTube Shorts y TikTok a la vez, y Reels si hay cuenta (es el mismo MP4). En Shorts se puede fijar un comentario con el
link a Google Play. **Un video cada 2 días** (3 o 4 por semana), no los cuatro el mismo día: las redes prueban mejor cada
video con 48 h de aire. Con estos cuatro hay más de una semana para ver qué gancho trae las primeras descargas.
