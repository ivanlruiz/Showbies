# Los zombis nuevos (diseño de Argon, 18/10)

Ivan, después de la ronda de ideas (PLAN.md 8): "de la 1.7.5 me gustó el nuevo zombi, habría que generar distintos tipos
de zombi con habilidades diferentes". Se le pidió el elenco a Argon (Gemini en el razonamiento máximo, en el chat del
análisis de diseño) con las restricciones reales del juego:

- **El mismo modelo para todos** (`TT_demo_zombie`) y **sin clips nuevos**: caminar, correr, el zarpazo y morir, más
  poses por código (como el jefe) y piezas colgadas de un hueso (como los disfraces de Halloween).
- **De noche y desde arriba un zombi mide unos 40 píxeles**, y casi todos los colores ya tienen dueño (los cinco tipos,
  el tesoro, el hielo, la quemadura, los drones y el bate, el destello): cada tipo nuevo se lee por su silueta, una pieza,
  una luz y lo que hace.
- **Techo de 35 vivos en el teléfono.**

## El elenco

Referencia: el normal (vida 5, golpe 1, velocidad 5, 1 punto, 1-3 monedas, 1 de PODER). Pesos de hoy: normal 1 desde la
1, rápido 0,35 desde la 3, tanque 0,1 desde la 6, veloz 0,25 desde la 9.

| zombi | rol | desde | peso | máx. vivos | vida / golpe / velocidad | puntos / monedas / PODER |
|---|---|---|---|---|---|---|
| **Escupidor** (Spitter) | artillería: niega zonas y saca del auto-apuntado | 12 | 0,15 | 3 (2 desde la 31) | 1,8× / 0,5× (el charco) / 0,95× | 8 / 3× / 2 |
| **Saltador** (Leaper) | emboscada en línea recta | 15 a 20, y desde 31 | 0,15 | 4 | 1,4× / 1,2× / 1,1× | 10 / 2,5× / 2 |
| **Chamán** (Totem Bearer) | apoyo de la horda, desde atrás | 18 a 20, y desde 31 | 0,10 | 2 | 2,2× / 1× / 0,9× | 15 / 4× / 3 |
| **Blindado** (Riot Shielder) | bloqueo de frente contra la ráfaga | 21 | 0,15 | 3 | 3,2× / 1,4× / 0,75× | 12 / 3× / 2 |
| **Hinchado** (Bloater) | bomba que camina (y que sirve contra la horda) | 24 | 0,14 | 3 | 2,5× / 2× (la explosión) / 0,85× | 10 / 3× / 2 |

**Cada capítulo estrena los suyos** (Ivan le preguntó a Argon si convenía, y eligió su reparto el 18/10; los números de
arriba ya son esos):

| capítulo | oleadas | zombis | jefe |
|---|---|---|---|
| la pradera, "la escuela" | 1 a 10 | los comunes: normal (1), rápido (3), tanque (6) y veloz (9), y el tesoro | el Bruto (rojo cobre, embestidas dobles) |
| el cementerio, "terreno maldito" | 11 a 20 | estrena el escupidor (12), el saltador (15) y el chamán (18) | el Invocador (verde espectral: salta y llama anillos de rápidos) |
| la ciudad, "asedio urbano" | 21 a 30 | estrena el blindado (21) y el hinchado (24); el escupidor sigue, el saltador y el chamán descansan | el Titán violeta (el de hoy, con los tres ataques) |
| "horda total" | desde 31 | los cinco juntos, con sus pesos y el tope de 8 especiales | — |

- **Por qué** (Argon): cambiar de escenario se siente distinto al jugar y no solo en el piso, presenta las mecánicas de a
  poco (como un tutorial que no se ve), y va con empezar desde el capítulo de la 1.7.0, que puede mostrar los zombis de
  cada zona. En la ciudad descansan el saltador y el chamán para aprender el escudo y la explosión sin cinco avisos
  distintos en el piso a la vez.
- **En el código**: un tipo que descansa va dos veces en la mezcla de WaveMode, con su `hastaOleada` (15 a 20 y desde
  31). Los tres jefes son la 1.8.0 (PLAN.md 8).

- **Además del tope de cada uno, como mucho 8 especiales vivos a la vez** (de los 35 del teléfono): siempre quedan al
  menos 27 comunes haciendo bulto para las cartas de área.
- **Ninguno invoca** (no se come el cupo ni los FPS).
- **En el bestiario, estrellas más baratas**: 25 / 250 / 2.500 muertes (salen poco y tarde).
- **El muro no se mueve** (48 / 59 / 69): tienen el doble de vida en promedio, pero pagan 2-3 de PODER y 2,5-4 veces las
  monedas, así que el oro y las cartas por punto de vida quedan igual; y la explosión del hinchado contra los suyos
  compensa al blindado y al chamán en las oleadas 30 a 60.
  **Sin simular** (auditoría del 18/10): con el PODER de hoy (28 × 1,12^L) el muro del bajo es la 39, no la 48, así que
  los tres muros caen desde la 31, donde los especiales son el 11-19 % de lo que sale; por punto de vida dan +16 % de
  monedas y +7 % de experiencia, pero el bono del chamán y el escupidor fuera del auto-apuntado empujan para el otro
  lado. Neto en el medio, entre −1,5 y +1 oleada: hay que correr `simulaciones/cartas_1.6.0` con los especiales.
  **Argon (18/10): correrla cuando estén el blindado y el hinchado**, y si el jugador bajo cae por debajo de la 35, darle
  +1 de PODER a los cinco especiales (de 2 a 3, y el chamán de 3 a 4).

### 1. El escupidor (Spitter)

**Estrena en el cementerio, en la oleada 12.** Ivan primero lo quería desde la ciudad, y después eligió el reparto por
capítulo de Argon. **Se planta adentro de la pantalla y lejos del HUD de arriba**: al norte del jugador la pantalla
apaisada es corta (a 9,5 m quedaba debajo del botón de pausa), así que si a esa distancia no se lo ve, rodea al jugador
hacia los costados, donde la pantalla es más ancha, hasta que se lo vea (como mucho 3 s; si no, se acerca).

Castiga quedarse quieto o girar siempre en el mismo círculo, y obliga a usar el joystick: trabaja afuera de los 7,5 m del
auto-apuntado.

- **Cómo se lee:** piel amarillo ácido (#D4FF00), el torso inclinado hacia adelante (+20°), escala (0,9, 0,9, 1,15). En la
  nuca y la espalda, dos esferas amarillas que brillan solas (los sacos de ácido), que laten y se inflan un 40 % antes de
  escupir.
- **Distancia:** camina hasta quedar a **9,5 m** del jugador y se planta. Si el jugador se le acerca a menos de **4,5 m**,
  retrocede despacio, a 0,6 de su velocidad.
- **Cadencia y aviso:** escupe **cada 3,5 s**. Antes se frena y se infla **0,65 s**, y marca en el piso un anillo amarillo
  ácido de **2,2 m de diámetro** donde está el jugador en ese momento.
- **El escupitajo:** un globo amarillo en arco bajo, a **11 m/s** (unos 0,85 s para los 9,5 m).
- **El charco:** al caer deja un disco de ácido de 2,2 m de diámetro durante **4 s**; como mucho **3 charcos** en el mapa
  (el cuarto borra el más viejo). Da **0,25 s de gracia** y después pega **la mitad del golpe de un normal cada 0,5 s**
  mientras el jugador lo pise.
- **El auto-apuntado** lo ignora (se planta a 9,5 m): hay que estirar el tiro con el joystick o acercarse. **Al final de
  la oleada** (ya salieron todos y quedan 3 vivos o menos, o solo escupidores) se acerca a **6,5 m**, adentro del
  auto-apuntado: si no, el que juega con el automático no terminaba la oleada sin salir a cazarlo (Argon, después de la
  auditoría).
- **El charco cuenta como golpe** para la oleada perfecta y el logro INTOCABLE, **pero no gasta el escudo** de la carta
  ni sacude la cámara: solo el borde rojo y un chisporroteo corto (Argon).
- **El bate:** es liviano (lo empuja 3 m), y si le pega mientras infla los sacos, le corta el escupitajo.
- **El hielo:** lo frena y estira el tiempo entre escupitajos de 3,5 s a 5 s.

### 2. El blindado (Riot Shielder)

Se pone adelante de la fila y absorbe las balas comunes: premia el bate, la perforación y los explosivos.

- **Cómo se lee:** piel gris pizarra (#4A525D), hombros anchos (1,2, 1,05, 1). Delante del pecho, una placa negra
  (#1A1D24) de 75 × 90 cm con el borde de arriba ámbar brillante (#FFB300), que desde arriba dibuja una barra gruesa
  delante del zombi, y un casco cuadrado.
- **Habilidad:** camina a 0,75 de velocidad mirando siempre al jugador, girando a **120°/s** como mucho. Con el escudo,
  todo tiro común que le entra de frente (un arco de 90° adelante) pega un **80 % menos** y no lo empuja: suena metálico y
  saca chispas blancas en lugar de sangre.
- **Qué es "tiro común"** (Argon, después de la auditoría): la pistola, el revólver, los perdigones de la escopeta, el
  rifle, las balas de más del disparo doble, la de hacia atrás y los drones (el ángulo se mide desde donde salió el tiro,
  no desde el jugador). **Ignoran el escudo:** el bate (que además lo rompe), la granada, el arma de energía, la explosión
  del hinchado, la quemadura, el área de la explosiva y **toda bala si el jugador tiene al menos una perforante**.
- **Es pesado, como el tanque:** no se aparta del jugador, y el chamán se refugia detrás de él.
- **Aviso:** a 1,5 m abre el brazo del escudo hacia el costado 0,5 s antes del zarpazo, y deja el pecho descubierto.
- **El bate** (su contra): un batazo le arranca el escudo para siempre (se apaga la placa), lo frena 1 s y lo deja como
  uno común.
- **La perforante, la explosiva, la quemadura, la granada y el arma de energía** ignoran el escudo.
- **El hielo** baja su giro de 120°/s a 50°/s: se lo puede rodear para pegarle por la espalda.
- **El auto-apuntado**, si es el más cercano, gasta balas contra la placa al 20 % hasta que el jugador use el bate o
  apunte a otro.
- **Cómo se arma** (Argon, 18/10, con la hoja de Nano Banana en `Marketing/zombis/blindado_hoja_nano_banana.png`): escala
  (1,20, 1,05, 1,05), piel #4A525D, ropa azul policial oscuro #1E2631 y borde de luz gris azulado frío. Un casco (cubo de
  36 × 22 × 38 cm en la cabeza, a +8 cm, #1A1D24) con la visera ámbar sin luz (#FFB300, 38 × 8 × 6 cm a +4 cm y +18 cm
  adelante). El escudo cuelga de un pivote hijo del torso, 38 cm adelante del pecho (no de la mano: los brazos se
  balancean): la placa de 75 × 90 × 8 cm #1A1D24, el borde de arriba ámbar (78 × 10 × 10 cm a +42 cm) y una mirilla ámbar
  (50 × 14 × 9 cm a +18 cm). Para abrir la guardia, el pivote gira 65° hacia la izquierda. Desde la cámara, la barra
  ámbar gruesa y recta delante de la cabeza lo hace inconfundible.

### 3. El saltador (Leaper)

Castiga caminar derecho sin mirar los avisos del piso: un esquive corto, como la embestida del jefe pero rápido.

- **Cómo se lee:** piel azul noche (#282A6B), flaco y largo (0,85, 1,15, 1,15), inclinado 30° hacia adelante, con dos
  crestas triangulares hacia atrás en la cabeza y ojos grandes blanco amarillento que brillan solos.
- **Habilidad:** camina a 1,1 de velocidad hasta quedar a **6 m** del jugador.
- **Aviso:** se frena en seco, se agacha (la escala en Y a 0,6) **0,6 s** y marca en el piso una franja color crema de
  **0,6 m × 5,5 m** apuntando al jugador.
- **El salto:** sale por esa línea a **14 m/s durante 0,4 s**, con la pose del zarpazo congelada; si toca al jugador en el
  camino pega **1,2 veces el golpe**. Al terminar se queda **0,8 s** quieto, recuperándose. Recarga: **5 s**.
- **El auto-apuntado** le tira mientras se agacha (está adentro de los 7,5 m), pero a media cadencia no llega a matarlo;
  con el pulgar en el joystick (cadencia entera) sí.
- **El bate**, mientras se agacha o en el aire, le corta el salto y lo empuja 3 m.
- **El hielo** acorta el salto a 3,2 m y duplica el tiempo que queda quieto al caer (1,6 s).

### 4. El hinchado (Bloater)

Una amenaza de cerca para el jugador, pero también un barril explosivo a favor si se lo mata o se lo batea adentro de la
horda.

- **Cómo se lee:** piel marrón óxido (#6B4235), la panza muy ancha (1,35, 1, 1,35), bamboleándose al caminar. En la espalda,
  un cilindro atravesado verde esmeralda (#00FF66) que brilla solo y parpadea lento al caminar y rapidísimo antes de
  estallar.
- **Habilidad:** no tira zarpazos. Cuando llega a **1,6 m** del jugador, o cuando su vida llega a 0, se clava en el piso.
- **Aviso:** se infla **0,75 s** (la escala ×1,3) y marca en el piso un círculo verde esmeralda de **2,6 m de radio**.
- **La explosión:** al terminar estalla y le pega **2 veces el golpe** al jugador si sigue adentro, y a todos los zombis
  dentro de los 2,6 m les saca el **35 % de su vida máxima** y los empuja 2,5 m.
- **El bate** (la jugada estrella): si se le pega mientras está inflado, se lo patea **4 m** hacia atrás, al medio de la
  horda, y les explota a los demás.
- **El auto-apuntado**, si lo mata a 7,5 m, lo hace explotar lejos y limpia gratis a los que venían pegados a él.
- **El hielo** estira la cuenta antes de explotar de 0,75 s a 1,1 s.
- **Contra el jefe** la explosión le saca el **4 %** de su vida máxima, no el 35 % (Argon, después de la auditoría).
- **Respeta el −25 % del chamán**: al potenciado le saca el 26,25 %.
- **Lo que mata paga entero**: puntos, monedas, PODER, misiones, bestiario, combo y vampiro, como si lo hubiera matado el
  jugador.
- **En cadena:** un hinchado alcanzado por otra explosión (que lo deja en 0, o que lo pilla con menos del 35 %) entra en
  su cuenta de 0,75 s y estalla también. El que ya se está inflando ignora las otras explosiones (para que no haya
  bucles).

### 5. El chamán (Totem Bearer)

Obliga a elegir blanco con el joystick en vez de dispararle siempre al de adelante.

- **Cómo se lee:** piel hueso pálido (#E6DFD3), alto y flaco (0,9, 1,25, 0,9). Sobre la cabeza, un tótem en forma de T con
  un cubo rubí (#FF1744) girando en diagonal, y un aro fino rubí en los pies.
- **Habilidad:** camina hasta quedar a **7 m** del jugador (si hay tanques o blindados, se queda detrás de ellos).
- **Aviso y pulso:** cada **4 s** se frena 0,5 s, se arquea hacia atrás prendiendo el cristal y potencia a los **4 aliados
  más cercanos** (a menos de 4,5 m) durante **3 s**: **+25 % de velocidad y −25 % de daño recibido**, con un cubito rubí
  girando sobre la cabeza de cada uno mientras dura (sin partículas ni líneas, por los FPS).
- **El auto-apuntado** se traba con los potenciados de adelante: hay que apuntarle a mano (o tener la perforante). Al
  morir el chamán, se apaga el bono de todos.
- **El bate:** es liviano (lo empuja 3 m) y le corta el pulso.
- **El hielo:** congelado no puede dar pulsos.

## El orden (de Argon)

1. **Escupidor**: resuelve la negación de zona y el abuso del auto-apuntado.
2. **Hinchado**: reusa el círculo de aviso del piso y le da un uso espectacular al bate.
3. **Saltador**: reusa la franja de aviso de la embestida del jefe.
4. **Blindado**: una pieza delante y un producto escalar para saber si la bala entra de frente.
5. **Chamán**: el último, porque mira hasta 4 vecinos cada 4 s.

**Hechos (18/10):** el escupidor, el saltador y el chamán, en el orden de los capítulos que eligió Ivan (los tres del
cementerio). Siguen el blindado (21) y el hinchado (24).

Lo que se decidió al hacer el chamán, que Argon no dijo:

- **No potencia a los que se mueven por su cuenta** (el jefe, el tesoro y los otros con habilidad, otros chamanes
  incluidos): su movimiento no pasa por la persecución de siempre, y el bono de velocidad les quedaría a medias.
- **"Detrás de los tanques"**: si hay un pesado vivo a menos de 5 m del chamán, se pone 1,6 m detrás de él (visto desde
  el jugador), mientras ese lugar quede entre 5,5 y 9 m del jugador; más cerca, el tanque lo llevaría a sus pies, y ahí
  se queda donde está. El blindado, si es pesado, va a contar solo.
- **El cristal y las marcas miden 22-24 cm**, no 14: a 40 px un cubo de 14 cm era un punto.

## Lo que decidió Argon después de la auditoría (18/10)

La auditoría de los tres primeros dejó preguntas de diseño, y se las llevó a Argon:

1. **El bono del chamán no pasa de 11 m/s** (el jugador corre a 11,5 y siempre se tiene que poder escapar corriendo
   derecho; con ×1,25 el veloz llegaba a 13,1). El normal y el tanque llevan el +25 % entero. Hecho.
2. **El charco del escupidor** cuenta para la perfecta y el INTOCABLE, no gasta el escudo y no sacude. Hecho.
3. **El escupidor** se acerca a 6,5 m al final de la oleada, y como mucho son 2 desde la 31. Hecho. La simulación, con
   el blindado y el hinchado (ver arriba).
4. **El blindado y el hinchado**: lo de sus secciones (qué atraviesa el escudo, el 4 % al jefe, la cadena).
5. **Cómo se arma el blindado**: en su sección, con la hoja de Nano Banana.

## Lo que hay que resolver en el código (de Claude)

- **Hecho:** el tope por tipo y el de especiales (`TipoEnOleada.maxVivos` y `especial`, `WaveManager.Elegible` y
  `EspecialesVivos`, `EnemyController.VivosDe`), y el bate que empuja a los que se mueven por su cuenta salvo al jefe y
  al tesoro (`EnemyController.RecibirBatazo`).
- **Falta: cada tipo entra al bestiario** (con las estrellas a 25 / 250 / 2.500) y a la tabla de textos. `NivelJugador.PuntosPorTipo`
  no hace falta: es solo para migrar progresos de antes de la v6.
- **Falta: el modo libre** tiene una corrutina por tipo (`GeneradorZombis`): los nuevos van primero en las oleadas.
- **Para el blindado y el hinchado** (de la auditoría del 18/10): un origen del daño en `DanoZombi` (el escudo tiene que
  saber qué le entra de frente), una persecución con giro limitado reusable (120°/s), su masa por encima de 10 (si no, el
  bate lo empuja y el chamán no se refugia detrás), y un gancho antes de morir (el hinchado se infla con vida 0 y todavía
  se lo puede patear). Y sacar a una base común lo que ya repiten los tres hechos (buscar el modelo, el aviso en el piso,
  quedarse quieto, el corte con el bate y la derrota, la pose), antes de copiarlo dos veces más.
