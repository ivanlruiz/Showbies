# Las cartas de la 1.6.0 (etapa 4; revisada el 13/10)

`cartas.py` es la simulación de la economía de `../curva_1.5.2/curva.py` (se juega, se cobran las monedas y se compra
entre partidas) con lo que trae la 1.6.0: **el PODER y las cartas, las armas de la mano por tramo del daño y el
auto-apuntado del teléfono**. Importa `curva.py` y no lo toca: los zombis, la tienda, los perfiles, el imán, las cajas
y lo que pegan según la velocidad son los mismos; la partida se reescribe acá. Como en la 1.5.2, **los números absolutos
no son confiables y la comparación entre variantes sí**: todas corren con las mismas semillas.

**El 13/10 dos revisores compararon el modelo con el código C#** y encontraron trece diferencias; todas eran reales
(ver abajo qué se hizo con cada una), y con ellas se volvió a correr todo. Lo que más cambió: **el modelo ya no le da
balas gratis al jugador entre oleadas** y **solo se le tira a lo que se ve en la pantalla**. Los números de abajo son
los nuevos; los de la primera versión de este archivo quedaron viejos.

## Los zombis con habilidad (18/10)

**Después de esta corrida Argon cambió la regla del escudo** (ver `ZOMBIS.md`): la perforante ya no lo anula, sino que
cada carta suma un 30 % a lo que entra de frente (20, 50 y 80 %) y la placa se come una perforación. El modelo de abajo
tiene la regla de antes y no se volvió a correr: con la nueva, el blindado queda un poco más duro.

Argon pidió (`ZOMBIS.md`) correr esto con los cinco especiales ya en el juego (el escupidor, el saltador y el chamán del
cementerio, el blindado y el hinchado de la ciudad), con una regla: **"si el jugador bajo cae por debajo de la 35, +1 de
PODER a los cinco especiales"**. Todo en `resultados_especiales.md` (tres corridas, con las mismas semillas).

**Qué variante es el juego de hoy: `poder_28_112`**, la V3 de Argon del 14/10 (`ARGON_V3`: la vida y el golpe ×1,13 y
×1,09 hasta la 10, ×1,145 y ×1,085 hasta la 30 y ×1,12 y ×1,065 desde la 31; 10 + 2n zombis, uno cada 0,2 s; la moneda
×1 que crece ×1,03 por oleada y el bono de 2n; un cuarto del cargador por oleada y lleno después de un jefe; el
automático hasta 7,5 m con 0,25 s para enganchar al siguiente; daño ×1,20, cadencia ×1,15 sin costo de balas, el doble
al 70 % y el vampiro 3 % cada 20) con el PODER 28 × 1,12^L del 16/10 (`resultados_poder.md`). Está revisado contra el
`WaveManager` de `WaveMode.unity` y `PoderDeLaPartida.cs`. **Su muro es 39 / 59 / 69**: el 48 / 59 / 69 que dice
CLAUDE.md es el de `argon_v3`, con el PODER de antes del 16/10 (14 × 1,15^L); el 39 de `ZOMBIS.md` es el bueno. Lo que
el modelo no tiene del juego de hoy: el desvío de hasta 6° del automático va como su puntería (0,75), y el bate de la
1.7.0 no se compra (la tienda es la de `curva.py`).

| muro (minutos de una partida ahí) | bajo | medio | alto |
|---|---|---|---|
| hoy, sin especiales (`poder_28_112`) | 39 (16,2) | 59 (30,2) | 69 (38,6) |
| **con los cinco (`especiales`)** | **39** (16,6) | **59** (31,1) | **69** (40,1) |
| con los cinco y +1 de PODER (`especiales_poder`: 3, 3, 4, 3, 3) | 43 (18,9) | 59 (30,7) | 70 (41,0) |
| con los cinco, sin sus habilidades (solo sus números) | 42 (18,1) | 59 (31,0) | 69 (39,3) |
| solo el escupidor / saltador / chamán / blindado / hinchado | 39 / 39 / 39 / 40 / 39 | 59 los cinco | 69 / 69 / 69 / 69 / 70 |
| con los cinco, esquivando la mitad de sus avisos | 39 (15,7) | 54 (26,7) | 63 (33,8) |
| con los cinco, esquivando el 90 % | 46 (20,6) | 59 (31,0) | 69 (39,8) |
| partida en que llega a la 10 / 30 / 50: hoy | 11 / 27 / — | 4 / 7 / 13 | 2 / 3 / 5 |
| ídem con los cinco | 11 / 25 / — | 4 / 8 / 12 | 2 / 4 / 6 |
| oro por minuto en el muro: hoy → con los cinco | 1.930 → 2.243 | 2.962 → 3.540 | 3.919 → 4.734 |

- **El muro no se mueve: 39 / 59 / 69.** Con 10 semillas (la tercera corrida), la media de las últimas 20 partidas,
  semilla por semilla contra hoy, cambia −0,2, 0,0 y 0,0 (con un error de 0,3-0,4). Los tres muros son jefes: el bajo
  muere en el de la 40, el medio en el de la 60 y el alto en el de la 70 (todas las semillas del medio dan 59), y los
  especiales no cambian cuánto se le aguanta al jefe.
- **Lo que cuestan sus habilidades lo devuelve lo que pagan.** Con los cinco, el 38-40 % del daño que recibe el bajo y el
  medio sale de sus habilidades (el bajo: charco 19 %, salto 7 % y explosión 12 %), pero dejan un 16, 20 y 21 % más de
  oro por minuto en el muro (un 17, 22 y 27 % más por partida) y algo más de PODER, y la partida dura 0,4-1,5 minutos
  más. Sin las habilidades (solo sus números) el bajo llega a la 42: las habilidades le cuestan unas 3 oleadas y lo que
  pagan se las devuelve.
- **Ninguno solo mueve el muro más de una oleada.** El que más pega es el escupidor (solo, su charco es el 18-24 % del
  daño que recibe el jugador). En el modelo el chamán potencia a 1,3-1,4 zombis por pulso, la explosión del hinchado mata
  menos del 1 % de los zombis, y el escudo del blindado para el 1-3 % de las balas comunes que le van: sobre todo porque
  la carta de perforante, que lo anula para todas las balas, casi siempre ya está en el mazo cuando sale el blindado (en
  el muro se tiene 1,9-2 veces); además en el muro se tira con la de energía, que lo atraviesa, y pegado tiene la
  guardia abierta. **Para Argon**: con una sola carta de perforante el blindado es un tanque más.
- **La regla de Argon no salta**: con los cinco, el bajo queda en la 39 (media de las últimas 20: 41,5; las que terminan
  antes de la 35, el 2 %, contra el 5 % de hoy). Tampoco esquivando la mitad de los avisos: 39, con media 37,7 y el 18 % de
  las partidas del muro terminando antes de la 35. **+1 de PODER** (`especiales_poder`) le suma 1,5 oleadas de media al
  bajo (mediana 43,5 con 10 semillas), 0,5 al medio y 1 al alto: no hace falta por la regla. El muro es, como el 39 de
  referencia, el de las últimas 20 de 100 partidas, y el bajo todavía sube despacio (llega a la 40 en la partida 43):
  con más partidas su muro solo puede subir, así que el veredicto es conservador.
- **Lo que lo puede dar vuelta es cuánto se esquivan los avisos** (el charco, la franja del saltador y el círculo del
  hinchado), que está puesto a ojo como lo que el perfil le esquiva al jefe (0,6, 0,8 y 0,92). Esquivando la mitad, el
  medio y el alto pierden 5 y 6 oleadas (54 y 63; con +1 de PODER, 56 y 65), y el bajo, que ya muere en el jefe de la
  40, no (39). Esquivando el 90 %, el bajo sube a la 46. **Vale la pena mirar en el teléfono cuánto pisa el charco del
  escupidor el que juega poco**: es el 40-53 % del daño que se recibe si se esquiva la mitad.

**Cómo se modeló** (`jugar_oleada_esp`, que es `jugar_oleada` con lo de los especiales; lo de antes no cambió: las
variantes viejas dan partida por partida lo mismo: `poder_28_112` reproduce las 15 filas de `resultados_poder.md`, y otras
ocho variantes viejas, en una corrida corta, las mismas tablas que con el `cartas.py` de antes):

- **La mezcla**: los cinco con sus `.asset` (vida 9, 7, 11, 16 y 13; velocidad 4,75, 5,5, 4,5, 3,75 y 4,25; monedas
  5-7, 4-6, 7-9, 5-7 y 5-7; PODER 2, 2, 3, 2 y 2) y su lugar en `WaveManager.tipos` de WaveMode, en el orden de la escena:
  su tramo (el saltador de la 15 a la 20 y desde la 31, el chamán de la 18 a la 20 y desde la 31, el escupidor de la 12 a
  la 30 con tope 3 y desde la 31 con tope 2, el blindado desde la 21 y el hinchado desde la 24), su tope de vivos y el de
  8 especiales, contando cada uno una vez (`WaveManager.Elegible`). El tipo se sortea al aparecer, como
  `WaveManager.ElegirTipo`, con el mismo número al azar que tiraba `curva.sortear_tipo` al empezar la oleada: sin
  especiales sale exactamente lo mismo (`chequeo_mezcla`), así que la diferencia es de los especiales y no del azar.
- **Paso a paso desde la 12**: la cuenta de las oleadas fáciles no sabe de charcos ni de escudos. `actual_paso_a_paso`
  es hoy con esa regla, y da partida por partida lo mismo que `poder_28_112` (con la V3, desde la 12 no hay oleadas
  fáciles).
- **Las distancias entre zombis** (el pulso del chamán y la explosión del hinchado): cada zombi ya tenía un rumbo al azar
  (el de lo que se ve); su lugar en el piso es su distancia (los pegados, a 0,9 m) por ese rumbo. **Es de menos**: en el
  juego salen de cuatro puntos y llegan en chorros. `especiales_chorros` junta los rumbos en cuatro direcciones de ±15°
  (solo para estas distancias): el chamán pasa a 1,6-1,8 por pulso y el muro no cambia.
- **El escupidor**: camina hasta 9,5 m y se planta; desde ahí lo ven la mano y el dron (en el juego rodea al jugador hasta
  verse), pero el automático no llega (7,5 m) y la escopeta tampoco (7,2). Al final de la oleada (salieron todos y quedan
  3 o menos, o solo escupidores) se acerca a 6,5 m. Escupe 1 s después de plantarse y cada 3,5 s con el aviso de 0,65
  adentro; el globo tarda d / 11 s, y el charco pega la mitad de su golpe cada 0,5 s, con 0,25 s de gracia, hasta 8
  veces: el jugador lo pisa si no esquiva, y sigue pisándolo golpe a golpe con la misma probabilidad (0,67 golpes por
  escupitajo con 0,6 de esquivar). Cuenta para la perfecta y no gasta el escudo de la carta. No se modelan el tope de 3
  charcos en el mapa (de más para el jugador), que retroceda si se le acercan, ni el hielo.
- **El saltador**: a 6 m se agacha 0,6 s (quieto y sin zarpazos), salta 5,6 m (la distancia pasa a |d − 5,6|: pegado,
  atraviesa al jugador y cae del otro lado), pega 1,2 golpes si no se esquiva (lo para el escudo de la carta, como en el
  juego), se queda 0,8 s quieto y recarga 5 s desde que salta.
- **El chamán**: se planta a 7 m (a la vista); pulsa 1,5 s después de aparecer y cada 4 s con el aviso de 0,5 adentro,
  también acercándose si se lo ve y tiene a quién, y potencia 3 s a los 4 más cercanos a menos de 4,5 m de él: los comunes
  y el blindado (no el jefe ni los que se mueven por su cuenta), que corren ×1,25 sin pasar de 11 m/s y reciben ×0,75 de
  todo (balas, granada, explosiva, quemadura y la explosión del hinchado). Se apaga en todos si muere. No se modela que se
  ponga detrás de un pesado: en 1D, como se le tira al más cercano, ya queda para el final.
- **El blindado**: con la guardia cerrada, las balas comunes (las del arma de la mano, las del dron, el doble y la de
  atrás, no la de energía, y ninguna con una carta de perforante) pegan el 20 % de frente; de frente es el 90 % (gira a
  120°/s mirando al jugador: puesto a ojo), así que entra el 28 %. La guardia se abre a 1,5 m y se cierra a más de 2,3, y
  el zarpazo (×1,4) sale 0,5 s después de abrirla. La granada, la explosiva, la quemadura y el hinchado lo atraviesan, y
  la granada lo cuenta como duro. **Sin bate**: su contra no está, pero como la guardia se abre a 1,5 m, en 1D el bate
  solo le sacaría el escudo entre 1,5 y 2,5 m.
- **El hinchado**: no tira zarpazos; a 1,6 m o con la vida en 0 se clava 0,75 s (no le entra nada ni le tira nadie) y
  revienta: al jugador 2 golpes si está a menos de 2,6 m y no esquiva, y a los zombis a menos de 2,6 m el 35 % de su vida
  máxima (al jefe el 4 %; a los potenciados, ×0,75), con lo que mata pagado entero, el empujón de 2,5 m (la mitad y 0,5 s
  frenados los pesados; nada al jefe; en 1D, lo que el empujón tiene de alejarse del jugador) y la cadena con los otros
  hinchados. Su propia muerte paga entero.
- **Esquivar los avisos** (el charco, la franja y el círculo) es lo que el perfil le esquiva al jefe, o lo que diga
  `esquiva_especial` (un número para todos, o negativo para multiplicar el del perfil): es lo más a ojo, y lo que más
  mueve.
- **Lo que se cuenta** (`diag`): el daño que recibe el jugador por fuente, las muertes por la explosión, los pulsos y los
  potenciados, los saltos y los escupitajos, y cuánto de las balas que le van a un blindado para el escudo: la tabla "Los
  zombis con habilidad" de cada corrida. Desde el 18/10 todas las corridas sacan además en qué partida se llega a cada
  oleada, el muro de cada semilla y la media de las últimas 20 (más fina que la mediana, que se pega a los jefes), y
  `crudo=archivo.json` deja partida por partida.

**Lo que puede no estar en el juego**: en 1D el escupidor no obliga a moverse ni a meterse en la horda, el charco no
tapa lugar, el chamán y el hinchado tienen menos vecinos que en un piso de verdad (el uno empeora al jugador y el otro lo
ayuda), y el bate no existe. Ninguna de las variantes de arriba deja al bajo por debajo de la 35; lo que más pesa es
esquivar los avisos, y eso se puede medir en el teléfono.

## La decisión de Gemini (13/10), simulada

Gemini decidió sin simular (`CARTAS.md`, "Lo que decidió Gemini"): oleadas de 10 + 2n, uno cada 0,20 s, cada moneda de
los zombis ×2 en valor, la curva de la 1.5.2 hasta la 30 y un tercer tramo desde la 31 (vida ×1,15, golpe ×1,08), el
cargador lleno al terminar cada oleada, los tiros de más de la carta de cadencia sin costo de balas y el rifle a cadencia
×1, con el PODER como está. Prometía **el muro en la 50-55 y partidas de 20-25 minutos en la oleada 50**. Puesto entero
en el modelo (`gemini`, y `gemini_base` sin cartas; todo en `resultados_gemini.md`), 100 partidas × 5 semillas:

| | bajo | medio | alto |
|---|---|---|---|
| **`gemini`**: muro (minutos de una partida ahí) | **69** (36) | **74** (39) | **82** (47) |
| se aparta de la 50-55 | +14 a +19 | +19 a +24 | +27 a +32 |
| minutos hasta terminar la 50 | 18,7 | 17,0 | 16,8 |
| `gemini_base` (sin cartas) | 38 (14) | 43 (15) | 49 (16) |
| 1.5.2 sin cartas (`base`) | 28 (15) | 56 (52) | 69 (76) |
| 1.6.0 antes de la decisión (`c_equil`) | 57 (58) | 86 (122) | 100+ (156) |

- **El muro no coincide**: queda 14 a 32 oleadas más allá de lo prometido (al medio le sobran unas 20). Las cartas
  siguen corriendo el muro ~31 oleadas, como antes de la decisión. El del bajo es el jefe de la 70 (el 88 % de sus
  muertes en el muro son en un jefe).
- **El tiempo sí, un poco de menos**: la 50 se termina en 17-19 minutos (los 20-25 caen entre la 54 y la 60). Pero con
  el muro más lejos, **una partida cerca del récord dura 36-47 minutos**.
- **Una carta por oleada hasta la 4** (1, 2, 3, 4), **ninguna en la 5** (la quinta llega en la 6) y 9,6-9,7 hasta la 10
  (eran 12).
- **El oro por oleada queda igual, el oro por minuto no**: hasta la misma oleada se junta lo mismo que en la 1.5.2 (+30 %
  hasta la 10, +13 % hasta la 30, +7 % hasta la 50), en un tercio del tiempo: **a la misma oleada, 2,0-2,6 veces el oro
  por minuto de la 1.5.2**. A la partida 10, sin cartas: 5.000, 16.600 y 31.400 por minuto (1.5.2: 520, 9.200 y 34.000);
  con cartas, 93.000, 140.000 y 262.000.
- **Lo que pesa cada pieza** (sacando una por vez): sin el tercer tramo los tres pasan la 100 (le saca ~30 oleadas); la
  cadencia sin costo suma 9-14 (con la cadencia gastando como hoy: 58, 60 y 73); la moneda ×2, 0-2 (con ×1: 69, 73 y
  80). Con la curva de la candidata E en lugar de los tres tramos: 59, 68 y 76. **Ninguna pieza sola deja el muro en la
  50-55**: con la cadencia sin costo hace falta una curva más dura que las simuladas, y conviene simularla antes de
  pasarla al juego.
- **Al pasarlo al juego**: `Escalado.PorOleada` no tiene tres tramos (el modelo usa su convención: la 31 ya vale la 30
  por el tercero), y `Economia` está escrita para 10 + 4n y 2 monedas por zombi (`12m + 2m²`): con 10 + 2n y sin la
  moneda ×2 los premios pagarían la mitad, y `SegundosPorPartida` cuenta 0,35 s por zombi.

Las filas de `base` y `c_equil` salen idénticas a las de `resultados_cartas.md`, y la versión nueva de `cartas.py` da
partida por partida lo mismo que la de antes en diez variantes viejas: lo nuevo no cambió nada de lo que ya corría.

## El tercer tramo y lo que cerró Argon (13/10)

Con el muro de la decisión 14-32 oleadas más allá de lo prometido, se probó mover el tercer tramo
(`resultados_tercer_tramo.md`, la decisión entera con el tramo cambiado):

| tercer tramo | bajo | medio | alto |
|---|---|---|---|
| desde la 31, ×1,15 y ×1,08 (la decisión) | 69 (36) | 74 (39) | 82 (47) |
| desde la 21, ×1,15 y ×1,08 | 59 (29) | 69 (35) | 78 (42) |
| **desde la 26, ×1,20 y ×1,10** | **57 (27)** | **59 (26)** | **68 (35)** |
| desde la 31, ×1,25 y ×1,12 | 54 (24) | 58 (25) | 61 (29) |
| desde la 21, con la cadencia gastando balas | 56 (25) | 58 (24) | 69 (34) |

Argon (Gemini en el razonamiento máximo, `CARTAS.md`) eligió **desde la 26 con ×1,20 y ×1,10**, porque deja que el que
juega bien llegue más lejos (el de la 31 ×1,25 pega a los tres contra el mismo muro), con **la moneda ×1,4 y no ×2**, y lo
demás de la decisión. Es la variante `final_1_6_0` (`resultados_final.md`), la que está en el juego:

| | bajo | medio | alto |
|---|---|---|---|
| **`final_1_6_0`**: muro (minutos de una partida ahí) | **57** (26) | **59** (27) | **67** (33) |
| partida en que llega a la 50 | 8 | 4 | 3 |
| minutos hasta terminar la 50 | 20,3 | 18,0 | 17,5 |
| oro hasta terminar la 10 (1.5.2 sin cartas) | 3.146 (3.188) | 3.145 (3.323) | 3.220 (3.429) |
| `final_1_6_0_base` (sin cartas) | 33 (11) | 37 (12) | 41 (13) |

- **El oro del principio empata con la 1.5.2**: hasta la 10 se junta lo mismo (−1 a −6 %), en menos minutos.
- **La progresión se comprime**: el medio llega a la 50 en la partida 4 y al muro enseguida. Se aceptó: el muro es para
  el renacer de la 1.8.0.
- **Sin balas con un blanco a la vista: 0 %** en los tres (era 27 % para el bajo en la 1.5.2): el cargador lleno en cada
  oleada y la cadencia sin costo.
- **Al pasarlo al juego** se hizo lo que faltaba: `Escalado.PorOleada` de tres tramos (con la misma convención: la 26 ya
  vale la 25 por el tercero) y `Economia` con 10 + 2n y la moneda ×1,4 (`ZombisBase`, `ZombisPorOleada` y
  `MultiplicadorMonedasOleadas`, que la prueba de lógica compara con WaveMode).

## Lo que encontró

En corto:

- **Las cartas siguen corriendo el muro unas 30 oleadas**: el bajo de la 28 a la 57, el medio de la 56 a la 86 y el
  alto de la 69 a más de 100. Y lo corren enseguida: el medio llega a la 50 en la partida 3 (sin cartas, en la 13).
- **Los 25-30 minutos ya no se cumplían sin cartas** (el medio, 52 minutos en su muro de la 56); con cartas son dos
  horas. La partida dura lo que tardan en salir los zombis: 29 min hasta la 45, 35 hasta la 50, 49 hasta la 60.
- **La carta de daño ×1,25 con tope 4 no es el problema**: achicarla a ×1,15 con tope 3 le saca 1 oleada al medio y 3
  al bajo. Las de números juntas (daño, cadencia, crítico y vida) suman 0, porque **la cadencia resta 8-10 oleadas**:
  con el cargador finito, tirar más rápido es quedarse sin balas antes.
- **La escopeta no empeora al medio**: en la campaña da lo mismo (86 y 86), y sin cartas es mejor que el revólver.
  Solo si la partida arranca con el revólver y las cartas la llevan a la escopeta cuesta 2,6 oleadas (medio) y 3,3
  (bajo), y con el túnel de las balas a 30 FPS, casi nada.
- **Sale una carta por oleada hasta la 4** (1, 2, 3, 4), 12 hasta la 10, 28 hasta la 40; el mazo (38) se llena hacia la
  55-60, y el medio y el alto mueren en el muro con todas al tope.
- **Pesan el daño y el disparo doble solos (+7 cada una), y en el mazo entero la granada rápida, la vida y el
  vampiro** (sacarlas cuesta 12, 8 y 5 oleadas).
- **Lo nuevo: las balas.** Sin el cargador lleno entre oleadas, el bajo pasa un cuarto del tiempo (un tercio con
  cartas) con un zombi al alcance y sin balas, y eso, más que las cartas, es lo que lo frena: sin cartas, con el
  cargador lleno en cada oleada llega a la 48, y sin él a la 28.
- **Para volver a la 50-60 con partidas de ~30 minutos hace falta tocar la curva y el largo de las oleadas**, no las
  cartas: oleadas de 10 + 2n cada 0,25 s, la vida ×1,15 y el golpe ×1,07 desde la 11 y el cargador lleno al terminar
  cada oleada dejan el muro en la 53, 55 y 69 con partidas de 27, 27 y 39 minutos (la candidata E, abajo).

Todo con 100 partidas y 5 semillas por perfil (`resultados_cartas.md`), en el teléfono (20:9, techo de 35 vivos, con
auto-apuntado) salvo que diga PC. El **muro** es la oleada mediana de las últimas 20 partidas de cada semilla; como
casi siempre se muere en un jefe o cerca, **una diferencia de menos de 3-4 oleadas es ruido** (en el bajo, que muere
más disperso, hasta 5).

### 1. Dónde queda el muro, y cuánto dura la partida ahí

| | bajo | medio | alto |
|---|---|---|---|
| 1.5.2 sin cartas (con las armas y el auto-apuntado) | 28 (15 min) | 56 (52 min) | 69 (76 min) |
| 1.6.0, equilibrada | 57 (58 min) | 86 (122 min) | 100+ (156 min) |
| 1.6.0, daño primero | 55 (55 min) | 87 (123 min) | 100+ (157 min) |
| 1.6.0, al azar | 49 (46 min) | 86 (121 min) | 100+ (156 min) |
| PC (a mano, 16:9, techo 60) sin cartas | 17 (8 min) | 53 (48 min) | 69 (77 min) |
| PC con cartas, equilibrada | 54 (56 min) | 84 (117 min) | 100+ (157 min) |

(100+: la simulación corta en la 100, y ahí llegan el 93-97 % de las últimas partidas del alto con cartas.)

- **Las cartas corren el muro ~30 oleadas** en los tres perfiles: +29 el bajo, +30 el medio y +31 o más el alto. **La
  política casi no importa** (al azar, 8 menos el bajo, igual el medio): el mazo entero es lo que pesa.
- **Y lo corren en las primeras partidas**: con cartas, el medio pasa la 40 y la 50 en la partida 3 (sin cartas, en la
  9 y la 13), y el bajo pasa la 40 en la 7 (sin cartas, en la 56). Después la tienda casi no mueve nada: el medio llega a
  la 86 en la partida 10 y en la 100 sigue ahí. **La progresión de la tienda queda comprimida en 3-10 partidas.**
- **La partida dura lo que tardan en salir los zombis** (`10 + 4n` por oleada, uno cada 0,35 s, y 3 s de descanso):
  29 minutos hasta la 45, 35 hasta la 50, 49 hasta la 60, juegue como juegue; cerca del muro, más (ahí se mata más
  lento de lo que salen). **Los 25-30 minutos entran hasta la oleada 40-45**: el medio de la 1.5.2 ya jugaba 52 minutos
  en su muro.
- **Lo que mata en el muro** sin cartas es cada vez más el jefe (el 16, 37 y 58 % de las muertes del bajo, el medio y
  el alto); con cartas, el bajo y el medio mueren en las oleadas comunes (14 y 15 % en el jefe), y el alto, que llega a
  la 100, en los jefes de la 90 y la 100.
- **La `calibracion`** (sin nada de la 1.6.0 ni de la revisión, con la curva y las balas de `curva.py`) da 39, 50 y 69,
  como antes de la revisión: el modelo nuevo reproduce el viejo cuando se le apaga lo nuevo. Con las mismas semillas
  la simulación es determinista.

**El auto-apuntado, la PC y el revivir** (`resultados_cartas.md`):

| | bajo | medio | alto |
|---|---|---|---|
| teléfono, con auto-apuntado, sin cartas / con cartas | 28 / 57 | 56 / 86 | 69 / 100+ |
| teléfono sin auto-apuntado, sin cartas / con cartas | 17 / 55 | 50 / 84 | 69 / 100+ |
| PC (a mano, 16:9, techo 60), sin cartas / con cartas | 17 / 54 | 53 / 84 | 69 / 100+ |
| teléfono, revive siempre que puede, sin cartas / con cartas | 37 / 59 | 59 / 90 | 69 / 100+ |

- **El automático no le cuesta oleadas a nadie: le suma 11 al bajo y 6 al medio sin cartas** (con cartas, 2). Antes
  de la revisión decía lo contrario (que le costaba 5-8 al bajo), pero era porque las balas sobraban: con el cargador
  finito, tirar a media cadencia y con buena puntería (0,9 contra 0,55 del bajo) rinde más balas. **La puntería del
  automático está puesta a ojo**: si en el teléfono falla más, esto se achica.
- **La PC de verdad** (16:9, que ve un poco menos a los costados, y 60 vivos) **no sale más difícil que el teléfono a
  mano**: 17, 53 y 69 contra 17, 50 y 69. La horda más grande no se nota porque los que frenan son la vida de los
  zombis y las balas.
- **El revivir** (con un video cada vez que se puede: es el techo) le suma 9 oleadas al bajo sin cartas, 3 al medio y 0
  al alto; con cartas, 2-4. Las tablas de arriba son **sin revivir**.

### 2. ¿La carta de daño ×1,25 con tope 4 es demasiado?

**No: es la que más pesa sola (+7 oleadas al tope), pero achicarla casi no mueve el muro.**

| | bajo | medio | alto |
|---|---|---|---|
| ×1,25 tope 4 (daño primero / equilibrada) | 55 / 57 | 87 / 86 | 100+ (97 / 95 % al tope) |
| ×1,20 tope 4 | 54 / 55 | 88 / 86 | 100+ (99 / 98 %) |
| ×1,15 tope 3 | 52 / 54 | 85 / 85 | 100+ (84 / 89 %) |

Con ×1,15 y tope 3 (el daño de las cartas pasa de ×2,44 a ×1,52) el bajo pierde 3 oleadas, el medio 1-2 y el alto
sigue pasando la 100. **El problema no es la carta de daño: es el mazo entero** (todas juntas, +26 oleadas con las
mejoras congeladas; ver 5), y el muro se devuelve con la curva (ver las candidatas).

### 3. ¿La escopeta empeora al jugador medio?

**No.** Con la pantalla de por medio, el alcance largo del revólver y el rifle no sirve (nadie le tira a lo que no ve),
y la escopeta, que de cerca pega el 133 %, rinde igual o mejor.

- **En la campaña da lo mismo**: con cartas, el muro con escopeta y con el revólver en su lugar es 57 y 57 (bajo), 86 y
  86 (medio); sin cartas, 28 y 29, 56 y 55, 69 y 69 (`resultados_escopeta.md`).
- **Con las mejoras de las primeras partidas congeladas** (`resultados_escopeta_arranque.md`, 400 partidas por casilla;
  el error de cada media es de 0,0-0,7), la oleada media a la que se llega:

| compras | sin cartas: escopeta / revólver en su lugar / todo pistola | con cartas: ídem | con cartas y túnel a 30 FPS: escopeta / revólver |
|---|---|---|---|
| bajo, daño 3 (revólver; dos cartas de daño lo llevan a la escopeta) | 4,7 / 4,7 / 4,5 | **21,6** / 24,9 / 23,4 | 20,6 / 19,4 |
| bajo, daño 4 (escopeta) | **7,6** / 6,3 / 5,9 | 32,8 / 32,5 / 29,8 | 32,0 / 31,4 |
| bajo, daño 6 (escopeta) | **12,6** / 10,1 / 9,8 | 30,6 / 31,3 / 30,9 | 29,5 / 30,4 |
| medio, daño 3 | 6,7 / 6,7 / 6,8 | **45,1** / 47,7 / 46,6 | 44,9 / 45,4 |
| medio, daño 4 | **10,4** / 8,5 / 8,9 | 48,5 / 47,9 / 51,4 | 47,6 / 47,8 |
| medio, daño 6 | **20,4** / 18,6 / 18,7 | 49,6 / 50,6 / 55,3 | 49,3 / 48,5 |

- **Sin cartas la escopeta es mejor que el revólver**: +1,3 a +2,5 oleadas en el bajo y +1,8 a +1,9 en el medio.
- **Con cartas, solo cuesta cuando se arranca con el revólver** (daño 3) y las cartas de daño la traen a mitad de
  partida: −2,6 oleadas el medio y −3,3 el bajo. Con daño 4 o 6 da lo mismo (±1). **Con el túnel a 30 FPS** (ver 7) la
  diferencia casi desaparece (−0,5 el medio) o se da vuelta (+1,2 el bajo): las balas rápidas del revólver son las que
  más atraviesan.
- **Lo que sí aparece: el rifle gasta un 20 % más de balas por el mismo daño** (tira ×1,2 y cada bala pega ÷1,2), y con
  el cargador finito eso se paga: con cartas y daño 6, el medio llega a la 49,6 con las armas (casi todo con el rifle)
  y a la 55,3 todo con la pistola. En el muro no se nota, porque todos terminan con la de energía.
- Lo que el modelo no ve: tener que acercarse no cuesta más golpes que dejar llegar a la horda (en 1D es lo mismo), ni
  la escopeta le dispara peor al tesoro que huye.

### 4. Cuántas cartas salen

| hasta la oleada | 1 | 2 | 3 | 4 | 5 | 6 | 8 | 10 | 15 | 20 | 30 | 40 | 50 | 60 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| cartas (los tres perfiles) | 1,0 | 2,0 | 3,0 | 4,2 | 6,0 | 7,0 | 9,4 | 12,0 | 16,0 | 19,0 | 24,0 | 28,0 | 31,0 | 33,0 |

- **Al principio sale una por oleada**, como se quería: 1, 2, 3 y 4 hasta la 4 (el 0,2 de más es el tesoro). Después
  más: 6 hasta la 5 y 12 hasta la 10; desde la 10, una cada dos oleadas, y desde la 30, una cada dos o tres. Coincide
  exacto con la tabla de CARTAS.md (`resultados_xp.md`, sin azar).
- **En el muro se eligen 33 cartas por partida el bajo, 38 el medio y 40 el alto**, con 5-6 doradas. **El mazo se
  acaba**: las 17 cartas suman 38 veces (con la granada y la furia compradas), y con las doradas se llenan con unas 33
  elecciones, hacia la oleada 55-60. El medio y el alto mueren en el muro con todas al tope.
- **Si se achican las oleadas** (ver las candidatas) sale menos experiencia por oleada: con `10 + 2n` y el PODER como
  está siguen siendo 1, 2, 3 y 4 hasta la 4 (y 9 hasta la 10); con `10 + 2n` y el PODER ×1,25, la segunda carta llega
  recién en la oleada 3 (`resultados_xp.md`).

### 5. Qué cartas pesan más

Con las mejoras congeladas del medio sin cartas (`resultados_pesos.md`: la foto de la partida 10 y la del final, 90
partidas por casilla; las diferencias de menos de ~1,5 oleadas son ruido), cuánto suma cada carta sola y al tope, y
cuánto se pierde si es la única que falta de un mazo con todas al tope:

| | sola (p10 / final) | la que falta de todas (p10 / final) |
|---|---|---|
| todas | +25,6 / +26,6 | |
| las de números (daño, cadencia, crítico, vida) | **−0,1 / +0,4** | |
| las de reglas (perforante a dron) | +11,7 / +10,9 | |
| daño | **+7,4 / +7,3** | −1,9 / −1,3 |
| disparo doble | **+7,2 / +7,0** | −1,3 / +0,1 |
| granada rápida | +4,5 / +5,1 | **−13,3 / −12,3** |
| explosiva | +4,7 / +2,6 | +0,7 / +1,1 |
| vida | +3,6 / +3,3 | **−8,4 / −7,4** |
| vampiro | +2,8 / +2,7 | **−4,9 / −5,3** |
| hacia atrás | +2,3 / +2,9 | −0,1 / +1,0 |
| hielo | +2,5 / +2,4 | −2,0 / −0,9 |
| velocidad | −0,1 / +0,7 | −3,3 / −2,1 |
| escudo, dron, perforante, furia larga, quemadura | entre −1,2 y +1,3 | entre −2,3 y +1,2 |
| crítico, imán | 0 (el crítico de la tienda ya está al 100 %; el imán no cambia la oleada) | 0 |
| cadencia | **−10,1 / −7,9** | **+9,1 / +10,0** |

- **Solas, pesan el daño (+7) y el disparo doble (+7)**: el doble no divide el daño (como el código) y no gasta balas.
- **En el mazo entero, pesan la granada rápida, la vida y el vampiro**: con todo lo demás la horda ya no frena, y lo
  que mata es el desgaste; la granada es el único daño que no gasta balas. **La granada del modelo es generosa** (le
  pega a todos los que están a menos de 5 m, apilados en 1D): no confiar en el número exacto.
- **La cadencia resta 8-10 oleadas**, y sacarla de un mazo completo suma otro tanto: con el cargador de 500 (y las cajas
  como en `curva.py`), tirar más rápido es vaciarlo antes; el daño por bala no cambia. Por eso las cuatro de números
  juntas suman 0. **Depende de cuántas cajas de balas junta el jugador**, que está puesto a ojo (ver 6).
- Casi nada: el escudo, el dron (una bala por segundo contra 10-40 del jugador), la perforante (a la mitad, y la de
  energía ya atraviesa dos), la quemadura y la furia larga.

### 6. Las balas

En el juego el cargador (500, o 1.000 con la caja del arma) **solo se llena con las cajas de balas y al revivir**; el
modelo de antes lo llenaba después de cada oleada fácil. Sin eso (`sin balas con blanco` en `resultados_cartas.md`: la
parte de los pasos con un zombi al alcance en que el jugador no tiene balas, en las partidas del muro):

| | bajo | medio | alto |
|---|---|---|---|
| sin cartas | 27 % | 2 % | 0 % |
| con cartas (equilibrada) | 34 % | 15 % | 8 % |
| PC sin cartas (todo a mano, a cadencia entera) | 51 % | 5 % | 1 % |

- **El bajo pasa entre un cuarto y un tercio del tiempo sin balas**, y es lo que más lo frena: sin cartas, con el
  cargador lleno al terminar cada oleada llega a la 48, y sin eso a la 28 (`resultados_candidatas.md`); al medio le
  cambia 2 oleadas (58 y 56) y al alto nada. El modelo lo deja juntar el 15 % de las cajas de balas (una cada 8 s), el
  medio el 35 % y el alto el 55 %: **eso está puesto a ojo y es lo que más pesa en el bajo**.
- Con la cadencia de las mejoras al tope y las cartas (22 × 2,07 = 45 tiros/s a mano, la mitad en automático), 500
  balas duran 15-20 segundos de fuego; una oleada de la 40 tarda un minuto en salir.
- **La carta de cadencia sigue restando con el cargador lleno en cada oleada** (13/10, `modo=pesos recarga=1
  cartas=cadencia,dano`, medio): sola −7,9 / −7,8 (sin recarga, −10,1 / −7,9), y sacarla del mazo completo suma +9,7 /
  +8,4. El bajo, mezclado: +3,2 / −11,4. O sea que no depende de que el cargador se llene. **La causa no está medida**: lo más
  probable es cómo el modelo reparte las balas de un paso entre los zombis (las que sobran de uno que muere), y en el
  juego la bala atraviesa al cadáver y puede pegarle al de atrás. Antes de cambiar la carta conviene mirarlo en el
  teléfono.
- **Llenar el cargador al terminar cada oleada** (la candidata `recarga`): con las cartas como están suma poco (bajo 57 →
  60, medio 86 → 88), pero con una curva más dura (las candidatas A → B) le devuelve al bajo 20 oleadas (32 → 52) y al
  medio 4 (55 → 59): **acerca a los perfiles** sin mover al alto.
- Las balas que gastan las oleadas fáciles (que no se juegan paso a paso) salen de una cuenta calibrada contra el paso
  a paso: le erra por menos de un 8 % (`resultados_balas.md`).

### 7. El túnel de las balas rápidas

**En el juego la bala no tiene Rigidbody** (se mueve con `Translate` en `Update`) **y los zombis tienen detección
discreta**: la física ve la bala una vez por cuadro (o por paso de física, a 50 Hz, si el teléfono va a más). Una bala
que de un cuadro al siguiente avanza más que el ancho del zombi lo atraviesa sin pegarle. A 30 FPS, contra un zombi que
viene de frente, le pegarían el rifle al normal, al rápido y al FASTER ~0,6, 0,5 y 0,4 de las veces, el revólver ~0,8,
0,6 y 0,5, y la pistola ~1, 0,9 y 0,7; la escopeta y la de energía (12 m/s) casi siempre. **No está medido**: va como
variante (`*_30fps`). En el muro casi no cambia nada (con y sin cartas, ±2 oleadas: todos terminan con la de energía,
que es lenta), pero en el arranque le quita la ventaja al revólver (ver 3). **Vale la pena medirlo en el Redmi** (el
contador de FPS y un rifle contra un FASTER) y, si se confirma, arreglarlo en el juego: un raycast entre la posición de
antes y la de ahora en cada cuadro, o un Rigidbody kinematic con detección continua.

### Las candidatas

No las pidió nadie: son para ver qué hay que tocar para devolver el muro a la 50-60 con partidas de 25-30 minutos
(`resultados_candidatas.md`, todas con la equilibrada, 100 partidas y 5 semillas). El muro, y entre paréntesis los
minutos de una partida ahí:

| | bajo | medio | alto |
|---|---|---|---|
| 1.5.2 sin cartas (la referencia) | 28 (15) | 56 (52) | 69 (76) |
| 1.5.2 sin cartas, con el cargador lleno al terminar cada oleada | 48 (40) | 58 (55) | 69 (75) |
| 1.6.0 como está | 57 (58) | 86 (122) | 100+ (156) |
| números a la mitad (daño ×1,12, cadencia y vida ×1,1, crítico +5 %) | 49 (44) | 84 (114) | 100+ (156) |
| menos cartas (PODER ×1,25: 9 hasta la 10, 20 hasta la 40) | 42 (33) | 70 (81) | 100+ (155) |
| vida de los zombis ×1,12 y golpe ×1,05 desde la 11 | 41 (32) | 69 (80) | 89 (129) |
| el cargador lleno al terminar cada oleada | 60 (63) | 88 (125) | 100+ (156) |
| oleadas más cortas: 10 + 3n, uno cada 0,25 s | 51 (30) | 80 (65) | 100+ (95) |
| **A**: menos cartas, vida ×1,12 y golpe ×1,05, 10 + 3n cada 0,25 s | 32 (14) | 55 (34) | 77 (62) |
| **B**: la A con el cargador lleno | 52 (31) | 59 (38) | 77 (61) |
| **C**: la B con uno cada 0,20 s | 53 (30) | 59 (34) | 78 (54) |
| **D**: menos cartas, vida ×1,14 y golpe ×1,06, 10 + 2n cada 0,25 s, cargador lleno | 53 (27) | 59 (30) | 68 (39) |
| **E**: el PODER como está, vida ×1,15 y golpe ×1,07, 10 + 2n cada 0,25 s, cargador lleno | **53 (27)** | **55 (27)** | **69 (39)** |

- **Ninguna perilla sola alcanza**: achicar las cartas de números le saca 2 oleadas al medio, tener menos cartas 16 y
  endurecer la curva 17, y ninguna baja los minutos por debajo de 80. **Acortar las oleadas** es lo único que baja los
  minutos (122 → 65 en el medio), y sola baja poco el muro (86 → 80).
- **Los minutos en el muro van con lo que hay que matar**: para ~30 minutos en la 55-60 hacen falta oleadas de la mitad
  (10 + 2n), y entonces la curva tiene que ser más dura que la de la 1.5.2 para que el muro no suba.
- **El cargador lleno acerca a los perfiles**: sin él (A) el bajo se queda en la 32 con partidas de 14 minutos; con él
  (B a E), en la 52-53.
- **La E deja el ritmo de cartas como está** (una por oleada hasta la 4); la D, con el PODER ×1,25 y oleadas de 10 + 2n,
  da la segunda carta recién en la oleada 3. Con la E el medio llega a la 50 en la partida 5 y el muro está en la 55.

### Recomendaciones

Cada una con el número que la justifica. **Todo lo que dependa de las balas conviene confirmarlo antes en el teléfono**
(cuántas veces se queda sin balas el que juega poco en las oleadas 20-40).

1. **No achicar la carta de daño** (`Assets/Cartas`, daño ×1,25 tope 4): con ×1,15 y tope 3 el medio pierde 1 oleada
   (86 → 85) y el bajo 3 (57 → 54); el alto sigue pasando la 100. Es la carta que más se siente (+7 sola) y no es la
   que corre el muro.
2. **Cambiar la carta de cadencia**: al tope resta 8-10 oleadas sola y sacarla del mazo suma 9-10, porque vacía el
   cargador. Que sus tiros de más no gasten cargador (como el disparo doble, que suma +7 sin gastar), o reemplazarla.
   Con el cargador lleno en cada oleada pesaría menos.
3. **Llenar el cargador al terminar cada oleada** (`WaveManager`, al cerrar la oleada): sin cartas el bajo pasa de la
   28 a la 48; con la curva más dura, 20 oleadas también (candidata A → B: 32 → 52), y deja de estar un tercio del
   tiempo sin balas (30 % → 7 %). Al medio le suma 2-4 y al alto nada.
4. **Devolver el muro con la curva y el largo de las oleadas, no con las cartas** (la candidata E, en el `WaveManager`
   de `WaveMode.unity`): `zombisPorOleada` 4 → 2, `intervaloEntreApariciones` 0,35 → 0,25, la vida tardía 1,08 → 1,15
   y el golpe tardío 1,04 → 1,07, con el cargador de la 3. Muro en la 53, 55 y 69 (bajo, medio, alto) con partidas de
   27, 27 y 39 minutos, el ritmo de cartas intacto y los números de las cartas como están. Si no se quieren oleadas de
   la mitad, la **C** (10 + 3n cada 0,20 s, PODER ×1,25, vida ×1,12 y golpe ×1,05, cargador lleno) deja 53, 59 y 78 con
   30, 34 y 54 minutos. Ojo: con la mitad de zombis por oleada sale la mitad de monedas por oleada (`Economia` lo sigue
   sola, porque cuenta con los números del `WaveManager`), y la curva más dura que la de la 1.5.2 es justo lo que
   Gemini había suavizado: antes de las cartas no hacía falta.
5. **El rifle**: tira ×1,2 compensado en el daño, así gasta un 20 % más de balas por el mismo daño; con cartas y daño 6
   el medio llega 5,7 oleadas menos que todo con la pistola (49,6 contra 55,3). Bajarle la cadencia a ×1 (y la
   compensación con ella) lo deja igual en daño por segundo y sin ese costo.
6. **Medir el túnel de las balas en el Redmi** y, si se confirma, arreglarlo con un raycast (ver 7). En el modelo no
   mueve el muro, pero en el teléfono puede verse como balas que pasan a través de los zombis.
7. **Medir en el teléfono cuánto dura una partida en el muro de hoy**: el modelo dice 52 minutos para el medio sin
   cartas y dos horas con cartas; si es así, la meta de 25-30 minutos pide tocar el largo de las oleadas en cualquier
   caso (recomendación 4).

## Lo que se cambió con la revisión del 13/10

Las trece diferencias que encontraron los dos revisores, verificadas contra el código (archivo y línea), y qué se hizo:

| lo que encontraron | en el juego | qué se hizo |
|---|---|---|
| El automático le tiraba a todo a 16 m (dos revisores) | `PlayerJS.BuscarBlanco` descarta lo que no `SeVe` (centro en la pantalla, margen 0,03); la cámara de WaveMode (12 m, 70°, fov 60) ve ~9 m al norte, 5,6 al sur y 11,7 (16:9) o 14,6 (20:9) a los costados | **Aplicado**: cada zombi saca un rumbo al azar al aparecer y con él hasta dónde se lo ve (la cuenta de la cámara, con la altura de su centro); el automático le tira hasta eso, topeado con 16 m y el alcance del arma |
| A mano se tiraba hasta donde llega la bala (25 m el revólver, 28 el rifle) (dos revisores) | `Proyectiles.Alcance` es lo que recorre la bala; nadie apunta a lo que no ve | **Aplicado**: a mano, hasta lo que se ve (sin margen) topeado con el arma, en el teléfono 20:9 y en la PC 16:9. La ventaja de alcance del revólver y el rifle sobre la pistola **no existe en el juego**: todo lo que se ve está a menos de 21 m |
| El cargador se llenaba después de cada oleada fácil | `cantBalas` solo sube con las cajas (`PUBalas`, `PUArma`) y al revivir | **Aplicado**: la oleada fácil gasta sus balas (una cuenta calibrada contra el paso a paso, ±8 %), en tramos de un segundo con las cajas que salen en el medio; si no alcanzan, se juega paso a paso. El cargador lleno quedó como candidata (`recarga`) |
| Las variantes de PC corrían con el techo de 35 (dos revisores) | `WaveManager.maxZombisVivos` 60, 35 solo con `Plataforma.EsMovil` | **Aplicado**: la PC con 60 vivos y 16:9; la horda que encierra se sigue midiendo contra 35 (con 60 pesa más). Para aislar el automático, `tel_manual` y `tel_manual_base` (el teléfono sin auto-apuntado) |
| El jefe no pegaba al tocar | `EnemyController.Golpear`: el jefe tira zarpazos con su golpe salvo con `puedeZarpar` apagado | **Aplicado**, y de paso lo demás del jefe que no estaba: **ataca solo si se lo ve** (`JefePatrones.EnPantalla`, margen 0,08 × 0,1) a menos de 15 m; **el próximo ataque es 5 s después de terminar el patrón** (en el modelo eran 5 s entre ataques); **el patrón lo frena** (no camina ni tira zarpazos: el salto 2,8 s, la carga 3,2, invocar 0,8); **el salto lo deja a ~5 m** (donde estaba el jugador) y **la carga lo pasa de largo** (16 m); invoca cada tres ataques en furia si hay lugar, y si no, carga |
| No había revivir | `PlayerHealth`: una vez por partida, con 180 s jugados y un video | **Aplicado como variante** (`*_revivir`: el techo, revive siempre que puede, con la vida llena, ≥500 balas, 2,5 s de gracia y los zombis a menos de 7 m fuera, salvo el jefe). Las tablas principales son sin revivir |
| El túnel de las balas rápidas | `BulletController.Update` con `Translate` y sin Rigidbody; los zombis con `m_CollisionDetection: 0` | **Verificado en el código, no medido**: va como variante (`*_30fps`, la física ve la bala una vez por cuadro a 30 FPS); recomendado medirlo (ver 7) |
| El dron le tiraba a todo a 12 m | `CartasEnElJugador.MasCercano` exige `SeVe` | **Aplicado**: hasta lo que se ve (sin margen), topeado con 12 m. Se mide desde el jugador y no desde la órbita de 1,5 m |
| El tiro iba entero al blanco o entero afuera | `GunController`: cada perdigón y cada bala del doble con su ángulo | **Aplicado**: el desvío del tiro es un ángulo (normal, con el sigma que da la puntería del perfil contra un normal a 8 m), condicionado a que la bala del medio pegue o no; cada perdigón prueba con su propio ángulo |
| El vampiro no curaba en las oleadas fáciles | `EnemyController.DanoZombi` cura cada 15 muertes en cualquier oleada | **Aplicado** |
| La bala medía 0,05 de radio | el `BoxCollider` de la bala mide 0,128 m en el mundo con cualquier arma (`Bullet.prefab`, `Vestir`) | **Aplicado**: 0,064. La escopeta le mete los cuatro perdigones a un normal a menos de 1,99 m y los dos del medio a menos de 6,06 m |

Ninguna se descartó. Lo que se hizo distinto de lo que proponían: el alcance de lo que se ve va **por rumbo** y no con
una media de ~10 m (la media sale sola: 11,0 m en 20:9 con el tope de 16); el factor de la horda en PC se mide contra
35 y no contra 60 (35 vivos encierran igual en las dos); y el túnel no entra en las tablas principales hasta medirlo.

## Cómo se corre

Lo que se corrió, en este orden (con Python 3.9, sin nada instalado aparte; el 13/10, unas 3 h en total con 12
procesos):

```
python cartas.py partidas=100 semillas=5 perfiles=bajo,medio,alto hilos=12 salida=resultados_cartas.md variantes=base,c_dano,c_equil,c_azar,d120_dano,d120_equil,d115_dano,d115_equil,sin_armas,sin_escopeta,base_sin_armas,base_sin_escopeta,pc,pc_base,tel_manual,tel_manual_base,base_revivir,c_equil_revivir,base_30fps,c_equil_30fps,calibracion
python cartas.py modo=escopeta perfiles=bajo,medio partidas=100 semillas=4 hilos=3 salida=resultados_escopeta_arranque.md
python cartas.py partidas=100 semillas=5 perfiles=bajo,medio,alto hilos=9 fotos=0 salida=resultados_candidatas.md variantes=suaves,costo125,curva112,recarga,olas_cortas,combo_a,combo_b,combo_c
python cartas.py partidas=100 semillas=5 perfiles=bajo,medio,alto hilos=9 fotos=0 salida=<aparte> variantes=combo_d   (y combo_e y base_recarga: sus filas se sumaron a resultados_candidatas.md)
python cartas.py modo=pesos origen=base perfiles=medio momentos=10,final partidas=30 semillas=3 hilos=3 salida=resultados_pesos.md
python cartas.py modo=xp salida=resultados_xp.md
python cartas.py modo=balas salida=resultados_balas.md
```

La decisión de Gemini (13/10; 10 min la primera línea y 11 la segunda, con 12 procesos), las dos en `resultados_gemini.md`
(la segunda se corrió aparte, con las mismas semillas, y sus tablas se sumaron al final):

```
python cartas.py partidas=100 semillas=5 perfiles=bajo,medio,alto hilos=12 fotos=0 variantes=base,c_equil,gemini,gemini_base salida=resultados_gemini.md
python cartas.py partidas=100 semillas=5 perfiles=bajo,medio,alto hilos=12 fotos=0 salida=<aparte> variantes=gemini_sin_tercero,gemini_cadencia_con_costo,gemini_moneda_x1,gemini_curva_e
```

El tercer tramo (10 min) y lo que cerró Argon (4 min), con las mismas semillas:

```
python cartas.py partidas=100 semillas=5 perfiles=bajo,medio,alto hilos=12 fotos=0 variantes=gemini,gemini_t21,gemini_t26_fuerte,gemini_t31_fuerte,gemini_t21_costo salida=resultados_tercer_tramo.md
python cartas.py partidas=100 semillas=5 perfiles=bajo,medio,alto hilos=12 fotos=0 variantes=base,final_1_6_0,final_1_6_0_base salida=resultados_final.md
```

Los zombis con habilidad (18/10; 12, 20 y 20 min con 12 procesos), las tres en `resultados_especiales.md` (cada una se
corrió aparte, con las mismas semillas, y sus tablas se juntaron):

```
python cartas.py partidas=100 semillas=5 perfiles=bajo,medio,alto hilos=12 fotos=0 salida=<aparte> variantes=poder_28_112,actual_paso_a_paso,chequeo_mezcla,especiales,especiales_poder,especiales_sin_habilidades
python cartas.py partidas=100 semillas=5 perfiles=bajo,medio,alto hilos=12 fotos=0 salida=<aparte> variantes=solo_escupidor,solo_saltador,solo_chaman,solo_blindado,solo_hinchado,especiales_esquiva_09,especiales_esquiva_mitad,especiales_chorros,especiales_poder_esquiva_mitad
python cartas.py partidas=100 semillas=10 perfiles=bajo,medio,alto hilos=12 fotos=0 salida=<aparte> crudo=<aparte>.json variantes=poder_28_112,especiales,especiales_poder,especiales_esquiva_mitad,especiales_poder_esquiva_mitad
```

- Desde el 13/10 el `modo=curva` saca también **el oro por minuto** (a la partida 10, de la 8 a la 12 y en el muro: las
  monedas de esas partidas sobre sus minutos, con las elecciones) y **los minutos y el oro hasta terminar cada oleada**
  (la mediana de las partidas que la terminan).

- `modo=curva` (el de siempre; la primera línea tardó 88 min con 12 procesos): las variantes, partida por partida. Deja
  también `fotos.json`, los niveles de las mejoras de la semilla 1000 a la partida 10, 30 y al final de cada variante y
  perfil (con `fotos=0` no lo pisa).
- `modo=pesos` (50 min con 3 procesos): con las mejoras congeladas de una foto, cada carta sola al tope y todas menos
  una. Lee `fotos.json`, así que va después de la primera línea.
- `modo=escopeta` (61 min con 3 procesos): con las mejoras de las primeras partidas congeladas, con y sin escopeta.
- `modo=xp` (un segundo): cuántas cartas salen hasta cada oleada, sin azar, con `casos=4:1.15,...` (zombis por oleada
  y lo que crece el PODER).
- `modo=balas` (un minuto): las balas que estima una oleada fácil contra las que gasta jugada paso a paso.
- `resultados_escopeta.md` son las filas de las armas de `resultados_cartas.md`, juntas (la misma corrida).
- `variantes=` elige cuáles (sin él corren todas las del diccionario `VARIANTES`); `hilos` no pasa de 12. Con las
  mismas semillas la simulación es determinista.

**Se corrieron 100 partidas por semilla, no 200-300**: con cartas el muro llega entre la partida 3 y la 10 en los tres
perfiles, y después solo se arrastra; y una partida que pasa la oleada 85 dura más de dos horas de juego, que en la
simulación son varios segundos cada una.

## Las variantes

| clave | qué es |
|---|---|
| `base` | **1.5.2 sin cartas**: la curva de la 1.5.2 con las armas de la mano y el auto-apuntado a media cadencia, que ya están en la rama; es la 1.6.0 sin el PODER |
| `base_sin_armas`, `base_sin_escopeta` | la `base` con todo como la pistola, y con el tramo de la escopeta tirando como el revólver |
| `c_dano`, `c_equil`, `c_azar` | **1.6.0 con cartas**, con cada política de elección |
| `d120_*`, `d115_*` | la carta de daño ×1,20 (tope 4) y ×1,15 con tope 3, con "daño primero" y "equilibrada" |
| `sin_armas`, `sin_escopeta` | con cartas (equilibrada): todo como la pistola, y el tramo de la escopeta como el revólver |
| `pc`, `pc_base` | **la PC**: siempre a mano, 16:9 y el techo de 60 vivos, con cartas (equilibrada) y sin |
| `tel_manual`, `tel_manual_base` | el teléfono sin auto-apuntado (aísla el automático), con cartas y sin |
| `base_revivir`, `c_equil_revivir` | revive una vez por partida siempre que puede (el techo del revivir) |
| `base_30fps`, `c_equil_30fps`, `sin_escopeta_30fps` | con el túnel de las balas a 30 FPS |
| `calibracion` | sin cartas, sin armas, sin auto-apuntado, sin nada de la revisión y con la curva y las balas de `curva.py`: tiene que dar lo de `c11_v108_d104` |
| `suaves`, `costo125`, `curva112`, `recarga`, `olas_cortas`, `combo_a` a `combo_e` | las candidatas (ver arriba), todas con la equilibrada |
| `base_recarga` | la `base` con el cargador lleno al terminar cada oleada: aísla lo que pesan las balas sin cartas |
| `gemini` | **la decisión de Gemini del 13/10**, con cartas y la equilibrada: 10 + 2n cada 0,20 s, la moneda ×2 en valor (`valor_moneda`; el bono no cambia), el tercer tramo desde la 31 (vida ×1,15 y golpe ×1,08: `tercer_tramo`, `vida_tercera`, `golpe_tercero`), el cargador lleno al terminar cada oleada, la carta de cadencia sin costo (`cadencia_gratis`: cada tiro cuesta 1 / el multiplicador de cadencia de las cartas, con un acumulador, también en las oleadas fáciles) y el rifle a ×1 (`cadencia_rifle`) |
| `gemini_base` | la `gemini` sin el PODER |
| `gemini_sin_tercero`, `gemini_cadencia_con_costo`, `gemini_moneda_x1`, `gemini_curva_e` | la `gemini` con una pieza cambiada: sin el tercer tramo, con la cadencia gastando balas como hoy, con la moneda ×1, y con la curva de la candidata E (vida ×1,15 y golpe ×1,07 desde la 11) en lugar de los tres tramos |
| `gemini_t21`, `gemini_t26_fuerte`, `gemini_t31_fuerte`, `gemini_t21_costo` | la `gemini` con el tercer tramo cambiado: desde la 21 (×1,15 y ×1,08), desde la 26 (×1,20 y ×1,10), desde la 31 (×1,25 y ×1,12), y desde la 21 con la cadencia gastando balas |
| `final_1_6_0`, `final_1_6_0_base` | lo que cerró Argon el 13/10: la `gemini` con el tercer tramo desde la 26 (×1,20 y ×1,10) y la moneda ×1,4; con cartas (equilibrada) y sin |
| `argon_arranque`, `argon_v2`, `argon_v3` (y sus `_base`, sin cartas) | las tres versiones de Argon del 14/10 (`resultados_arranque*.md`): la tercera, `argon_v3`, es la que pasó al juego |
| `poder_24_115`, `poder_20_120`, `poder_28_112`, `poder_18_125` | la `argon_v3` con otro costo del PODER (16/10, `resultados_poder.md`). **`poder_28_112` es el juego de hoy** (sin especiales) |
| `actual_paso_a_paso` | `poder_28_112` jugando paso a paso desde la 12 (da lo mismo, partida por partida): el control de los especiales |
| `chequeo_mezcla` | con el sorteo de los especiales y ninguno en la mezcla: tiene que dar lo mismo que `actual_paso_a_paso` |
| `especiales`, `especiales_poder`, `especiales_sin_habilidades` | **el juego de hoy con los cinco especiales** (18/10); con +1 de PODER cada uno (3, 3, 4, 3, 3), y con sus números pero sin habilidades |
| `solo_escupidor`, `solo_saltador`, `solo_chaman`, `solo_blindado`, `solo_hinchado` | con uno solo de los cinco |
| `especiales_esquiva_09`, `especiales_esquiva_mitad`, `especiales_poder_esquiva_mitad`, `especiales_chorros` | con los cinco y lo puesto a ojo movido: los avisos esquivados el 90 % o la mitad de lo que se esquiva al jefe (la última, con +1 de PODER), y los zombis en cuatro chorros para las distancias entre ellos |

Las políticas:

- **Daño primero**: daño, cadencia y crítico apenas salen (el crítico no, si ya está al 100 %), después las de reglas
  (perforante, disparo doble, explosiva, dron, hielo, hacia atrás, quemadura), después las de supervivencia y al final
  las de utilidad (granada rápida, furia larga, imán).
- **Equilibrada**: rota entre daño, supervivencia (vida, escudo, vampiro, velocidad) y reglas: busca la familia que
  toca, si no sale la siguiente de la rueda, y las de utilidad al final.
- **Al azar**: cualquiera de las tres.

Las dos primeras toman la dorada si es de la familia que buscan o de la siguiente (le suman 25 puntos a una escala de
100, 60 y 40 por familia).

## Cómo se traduce cada cosa al modelo

El modelo es el de `curva.py`: los zombis caminan hacia el jugador en una sola dimensión (la distancia), el jugador le
tira al más cercano que alcanza, una bala que falla le pega a otro de la horda con `min(0,6, n/30)`, los que están
pegados pegan cada 0,8 s según cuánto los esquiva el jugador, y las oleadas fáciles se resuelven sin paso a paso.

**Lo que es exacto** (sale del código, sin geometría):

- **El PODER**: cada zombi da la experiencia de su `.asset` (1 el normal, el rápido y el FASTER, 3 el tanque, 15 el
  jefe, 5 el tesoro, 1 los invocados), y pasar del PODER L al siguiente cuesta `round(12 × 1,15^L)`. La tabla esperada
  (`resultados_xp.md`) da exacto la de CARTAS.md.
- **El sorteo**: tres distintas entre las que no llegaron al tope y se pueden usar (granada rápida y furia larga solo
  con la granada o la furia compradas), Curación solo sin ninguna otra. **La dorada**: después de un jefe, una de las
  tres al azar vale doble sin pasar del tope, y como en `PoderDeLaPartida.Elegir` **se gasta solo si se elige**.
- **Los números**: daño ×1,25 por vez, cadencia ×1,2 (con el techo de 120 tiros/s), crítico +10 %, vida ×1,2 y la
  llena, velocidad +10 % (entra en lo que el jugador les escapa a los zombis: `factor_velocidad` de `curva.py`), imán
  +2 m (en la fracción de monedas que se juntan), granada rápida (recarga 5 s × 0,7ⁿ), furia larga (+2 s).
- **El hielo**: cada zombi que recibe un tiro camina un 40 % más lento 2 s (64 % con dos). Al jefe lo frena al
  caminar, no en sus ataques.
- **La quemadura**: una de cada cuatro balas (dos con dos cartas) quema 10 % del daño de la bala por segundo, en seis
  ticks de 0,5 s; si ya se quemaba, gana la más fuerte y vuelven a ser seis.
- **El vampiro** (cada 15 muertes cura el 5 % del máximo, 10 % con dos, también en las oleadas fáciles) y **el escudo**
  (para un golpe entero cada 15 s, también la carga del jefe, y corre medio metro a los pegados).
- **El dron**: una bala de la pistola por segundo cada uno, al más cercano que se ve a menos de 12 m, con el daño
  entero del tiro (sin crítico ni compensación del arma), con las cartas de la bala y aunque el jugador no tenga
  balas. Apunta con 0,85.
- **Las armas** (`Proyectiles.cs`): el arma es el tramo del daño de la mejora por las cartas de daño, sin la furia
  (desde 2 revólver, desde 4 escopeta, desde 8 rifle, desde 16 energía). Cada una con su alcance (22,5, 25, 7,2, 28 y
  21,6 m), la velocidad de su bala y su cadencia (×1, 0,7, 0,7, 1,2 y 1), compensada en el daño de cada bala
  (`CompensacionDelArma`, con los dos techos de 120). La de energía atraviesa dos zombis sin perder daño.
- **El cargador**: 500 balas al empezar; la caja de balas lo llena y la del arma lo sube a 1.000 y lo llena. No se
  llena entre oleadas.
- **El jefe** (`JefePatrones`): ataca a menos de 15 m si se lo ve; alterna salto (×1,5) y carga (×2,5), empezando por
  el salto, que se esquivan con la probabilidad del perfil; el próximo ataque es 5 s después de terminar el patrón
  (×0,65 en furia); en furia, cada tres ataques invoca 6 normales si hay lugar. El patrón lo frena; el salto lo deja a
  5 m del jugador y la carga lo pasa 16 m. Pegado, tira zarpazos con su golpe cada 0,8 s, con el mismo factor que los
  demás.
- **El revivir** (solo en las variantes `*_revivir`): una vez por partida, con 180 s jugados en total y la partida de
  30 s o más, con la vida llena, ≥500 balas, 2,5 s sin recibir daño y los zombis a menos de 7 m fuera (el jefe no).

**Lo que es geometría, resuelta así**:

- **Lo que se ve**: la cámara de WaveMode a 12 m del piso y 3,9 m al sur del jugador, 70° hacia abajo y 60° de campo
  vertical. Cada zombi saca un rumbo al azar al aparecer, y con él hasta dónde se lo ve (su centro, a la altura de la
  mitad de su cápsula). El automático (margen 0,03), la mano y el dron (sin margen) y el jefe para atacar (0,08 × 0,1)
  miran cada uno lo suyo. En 20:9 se ve en promedio a 11,0 m a un normal con el tope de 16 (5,6 m al sur, 9,1 al norte
  y 14,6 a los costados); en 16:9, 10,3.
- **El abanico** (la escopeta y el disparo doble): el desvío del tiro es un ángulo; una bala a θ grados le pega al
  blanco si `|θ + desvío|` no pasa el ángulo que tapa el zombi (su radio más el de la bala, 0,064, sobre la distancia,
  midiendo desde 0,9 m cuando está pegado); si no, le pega a otro de la horda como cualquier bala que falla. Los radios
  son los de la cápsula por la escala del prefab: 0,36 m el normal, 0,29 el rápido, 0,22 el FASTER, 0,76 el tanque y
  1,44 el jefe. El disparo doble no divide el daño (como el código).
- **La perforante y la de energía**: después de pegar, la bala sigue y le pega a otro de los que están al alcance con
  la misma probabilidad de la horda (de preferencia uno que está detrás), la de energía con el daño entero y las de la
  carta a la mitad cada vez.
- **La explosiva** (30 % del daño en 1,5 m; 60 % en 2 m con dos): les pega a los que están a la misma distancia del
  jugador (± el radio), con probabilidad `min(0,6; 0,27 · radio / max(1, d))`. **Es generosa con las hordas densas**:
  en 1D todos los pegados están a distancia 0.
- **Hacia atrás**: una bala con el tiro entero (la escopeta tira uno gordo), que le pega a uno de la horda con la misma
  probabilidad. En 1D detrás hay tanta horda como adelante: **es generosa**; contra el jefe solo, nada.
- **El túnel** (solo en las variantes `*_30fps`): la bala que le pega al blanco lo atraviesa con probabilidad
  `1 − min(1, 2 (radio + 0,064) / ((v_bala + v_zombi) / min(FPS, 50)))`, y sigue como una que falla.

**El auto-apuntado** (`GunController.CadenciaDelAutomatico`): en cada segundo, una fracción va a mano (0,2 el bajo,
0,5 el medio, 0,8 el alto: toda la cadencia y la puntería del perfil) y el resto en automático (media cadencia,
puntería 0,9, porque apunta solo al más cercano que se ve, hasta 16 m o lo que llegue el arma). Es un ciclo fijo de 10
pasos de 0,1 s. **La puntería del automático y cuánto se usa el joystick están puestos a ojo.** El automático no hace
esquivar mejor, aunque en el juego deje las dos manos libres para moverse.

**Lo heredado de `curva.py`, que pesa en lo que sale**:

- **Las cajas**: el bajo junta el 15 % de las que salen (una de balas y una de vida cada 8 s, una del arma cada 20), el
  medio el 35 % y el alto el 55 %. Con el cargador que no se llena solo, **es lo que más pesa en el bajo**.
- El jefe se esquiva con la probabilidad del perfil; la granada, la furia, el tesoro y las monedas se juntan como en la
  1.5.2.

**Lo que se simplificó para que corra**: las balas de un paso (0,1 s) que van al mismo blanco llegan juntas, con el
daño sumado, y cada efecto (la explosión, la perforación, la quemadura) se sortea una vez con el daño de todas. Las que
sobran después de matar al blanco le pegan a otro con la probabilidad de `curva.py`. **Las partidas se cortan en la
oleada 100** (`MAX_OLEADAS`). En las tablas, "al tope" es el porcentaje de partidas que llegaron.

**La curva** es la del juego (`Escalado.PorOleada`, con la 11 que ya vale la 10 por el tardío); en `curva.py` el tardío
empezaba en la 12, un 3 % de diferencia. La `calibracion` usa la de `curva.py`. El tercer tramo de las variantes `gemini` sigue la misma
convención (la 31 ya vale la 30 por el tercero: `por_oleada3`), que el juego todavía no tiene.
