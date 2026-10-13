# La decisión de Gemini (13/10) en la simulación

Lo que decidió Gemini para la 1.6.0 (`CARTAS.md`, "Lo que decidió Gemini"), puesto entero en `cartas.py` antes de
tocar el juego: oleadas de 10 + 2n, uno cada 0,20 s, cada moneda de los zombis vale el doble (el bono de la oleada no
cambia), la curva de la 1.5.2 hasta la 30 y un tercer tramo desde la 31 (vida ×1,15, golpe ×1,08), el cargador lleno al
terminar cada oleada, los tiros de más de la carta de cadencia sin costo de balas (cada tiro cuesta 1 / el multiplicador
de cadencia de las cartas) y el rifle a cadencia ×1; el PODER como está (12 × 1,15^L).

- `gemini`: todo eso, con cartas y la política equilibrada.
- `gemini_base`: lo mismo sin el PODER.
- De referencia, `base` (1.5.2 sin cartas) y `c_equil` (1.6.0 con cartas antes de la decisión).

100 partidas × 5 semillas por perfil, en el teléfono (20:9, 35 vivos, con auto-apuntado), sin revivir, con las mismas
semillas que `resultados_cartas.md`. **Las filas de `base` y `c_equil` salen idénticas a las de `resultados_cartas.md`**
(muro, minutos, cartas, muertes en el jefe y balas, en los tres perfiles), y antes se comparó partida por partida la
versión nueva de `cartas.py` con la de HEAD en diez variantes viejas (base, c_equil, recarga, combo_e, calibracion,
pc_base, base_30fps, c_equil_revivir, sin_escopeta, olas_cortas; 12 partidas, 2 semillas, 3 perfiles): las 60 casillas
dan lo mismo. Como siempre, los números absolutos no son confiables y la comparación entre variantes sí.

## El veredicto

| | bajo | medio | alto |
|---|---|---|---|
| **muro de `gemini`** (minutos de una partida ahí) | **69** (36) | **74** (39) | **82** (47) |
| lo que prometió Gemini | 50-55 | 50-55 | 50-55 |
| se aparta | **+14 a +19** | **+19 a +24** | **+27 a +32** |
| minutos hasta terminar la 50 (prometido: 20-25) | 18,7 | 17,0 | 16,8 |
| `gemini_base` (sin cartas) | 38 (14) | 43 (15) | 49 (16) |
| 1.5.2 sin cartas (`base`) | 28 (15) | 56 (52) | 69 (76) |
| 1.6.0 antes de la decisión (`c_equil`) | 57 (58) | 86 (122) | 100+ (156) |

- **El muro no coincide**: queda en la 69, la 74 y la 82, entre 14 y 32 oleadas más allá de la 50-55. Al medio, que es
  la referencia, le sobran unas 20. Contra la 1.6.0 de antes, la decisión entera le saca 12 oleadas al medio (86 → 74) y
  18 o más al alto (100+ → 82), y al bajo le suma 12 (57 → 69: el cargador lleno y la cadencia sin costo). **Las cartas
  siguen corriendo el muro unas 31 oleadas** (sin cartas, 38, 43 y 49; con cartas, 69, 74 y 82), lo mismo que antes de
  la decisión (+29, +30, +31).
- **El tiempo coincide, un poco de menos**: llegar al final de la 50 lleva 17-19 minutos (los 20-25 caen entre la 54 y la
  60). Pero como el muro queda más lejos, **una partida cerca del récord dura 36, 39 y 47 minutos**: menos de la mitad
  que con la 1.6.0 de antes (58, 122 y 156), y no los 20-25 que se buscaban.
- **Sin cartas la decisión endurece al medio y al alto** (43 contra 56, 49 contra 69) **y ayuda al bajo** (38 contra 28).
  El cargador lleno solo, sin cartas, daba 48, 58 y 69 (`base_recarga`, en `resultados_candidatas.md`): el resto de la
  decisión (el tercer tramo, las oleadas cortas) le saca 10 oleadas al bajo, 15 al medio y 20 al alto. Lo que se reparte
  entre una cosa y la otra no está separado sin cartas.
- **El muro del bajo es el jefe de la 70**: el 88 % de sus muertes en el muro son en un jefe, así que el 69 no es una
  pared pareja sino ese jefe, que mata a casi todos (el medio y el alto, 39 y 41 %).
- **Las cartas siguen saliendo una por oleada al principio**: 1, 2, 3 y 4 al terminar las oleadas 1 a 4 (en los tres
  perfiles), pero **la 5 no da carta** (4,2-4,4 al terminarla: la quinta llega en la 6; el 0,2-0,4 es el tesoro). Hasta la
  10 salen 9,6-9,7 (eran 12). Es la tabla sin azar de `resultados_xp.md` para 10 + 2n con el PODER ×1,15. En el muro se
  eligen 31-33 cartas por partida y casi todas quedan al tope.
- **El oro por minuto no queda igual: lo que queda igual es el oro por oleada.** Hasta la misma oleada se junta casi lo
  mismo que en la 1.5.2 (+30 % hasta la 10, +13 % hasta la 30, +7 % hasta la 50: 2 × (10 + 2n) monedas contra 10 + 4n),
  pero en un tercio del tiempo, así que **a la misma oleada el oro por minuto es 2,0 a 2,6 veces el de la 1.5.2** (medio,
  hasta la 30: 9.400 por minuto contra 4.200; hasta la 50: 36.300 contra 14.100). Partida por partida se suma que con la
  decisión se llega más lejos: a la partida 10, sin cartas, el bajo junta 5.000 por minuto (la 1.5.2, 520), el medio 16.600
  (9.200) y el alto 31.400 (34.000); con cartas, 93.000, 140.000 y 262.000. En el muro, el medio con cartas junta 161.000
  por minuto, casi lo de la 1.6.0 de antes (166.000).
- **La moneda ×2 casi no mueve el muro** (ver las piezas): con la moneda ×1 queda en 69, 73 y 80. Lo que cambia es el
  oro por partida (el doble), o sea lo rápido que se compra; el daño de la tienda crece con el logaritmo de las monedas.

## Cuánto pesa cada pieza

Corridas aparte con las mismas semillas, sacando de a una pieza de la decisión (tablas al final):

| | bajo | medio | alto |
|---|---|---|---|
| `gemini` | 69 (36) | 74 (39) | 82 (47) |
| sin el tercer tramo | 99 (60) | 100+ (58) | 100+ (56) |
| con la cadencia gastando balas, como hoy | 58 (26) | 60 (26) | 73 (37) |
| con la moneda ×1 | 69 (36) | 73 (38) | 80 (45) |
| con la curva de la candidata E (vida ×1,15 y golpe ×1,07 desde la 11, sin tercer tramo) | 59 (29) | 68 (36) | 76 (43) |

- **El tercer tramo le saca unas 30 oleadas** a la decisión: sin él, los tres pasan la 100.
- **La cadencia sin costo le suma 9 a 14** (bajo +11, medio +14, alto +9): con el cargador finito, la carta pasa de vaciar
  el cargador a duplicar el daño gratis. En el modelo los tres perfiles quedan con 0 % del tiempo sin balas; con la
  cadencia gastando, 6-8 %. **Depende de cuántas cajas de balas se juntan, que está puesto a ojo**, y de la causa no medida
  de que la carta reste (ver el README, 6): el número exacto no es confiable, el signo sí.
- **La moneda ×2, de 0 a 2.** El daño de la tienda crece con el logaritmo de las monedas.
- **Ninguna pieza sola lo deja en la 50-55**: con la cadencia como hoy, 58-73; con la curva de la E (que sin el resto de la
  decisión daba 53, 55 y 69), 59-76. Para llegar a la 50-55 con la cadencia sin costo hace falta una curva más dura que
  cualquiera de las simuladas (el tercer tramo antes o más fuerte), y conviene simularla antes de pasarla al juego.

## Para pasarlo al juego

- **`Escalado.PorOleada` no tiene tres tramos**: la simulación usa la convención de la de dos (la 31 ya vale la 30 por
  el tercero: `crece^(desde-2) × tardío^(desde3-desde) × tercero^(n-desde3+1)`).
- **`Economia` está escrita para 10 + 4n**: `ZombisPorPartida` es `12m + 2m²` (la prueba de lógica la compara con el
  `WaveManager`), que con 10 + 2n pasa a `11m + m²`, y `MonedasPorZombi` (2) tiene que pasar a 4 con la moneda ×2: si
  no, las misiones, el bestiario, la diaria y Halloween pagan la mitad. `SegundosPorPartida` cuenta 0,35 s por zombi,
  que con uno cada 0,20 s queda largo (y con él los objetivos de la furia y la granada).

# Las tablas

La salida de `python cartas.py partidas=100 semillas=5 perfiles=bajo,medio,alto hilos=12 fotos=0 variantes=base,c_equil,gemini,gemini_base salida=resultados_gemini.md`.

partidas 100, semillas 5, perfiles bajo,medio,alto, 590 s

mejor pN: la mejor oleada completada hasta la partida N (mediana de las semillas). muro: la oleada mediana de las ultimas 20 partidas de cada semilla. llega a N: la partida en que se completa por primera vez la oleada N. min. en el muro: lo que dura una de esas partidas, con 3 s por eleccion de carta. % muertes en jefe: de las ultimas 100 partidas de cada semilla.

## El muro

### Perfil bajo

| variante | mejor p10 | mejor p30 | muro (ult. 20) | mejor al final | llega a 40 | a 50 | a 60 | a 80 | al tope (100) | min. en el muro | cartas por partida | % muertes en jefe | arma en el muro | sin balas con blanco | revivio |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 11 | 33 | 28 | 45 | 56 | — | — | — | 0 % | 15.1 | 0 | 16 % | energia | 27 % | — |
| 1.6.0, equilibrada | 50 | 60 | 57 | 64 | 7 | 10 | 27 | — | 0 % | 58.2 | 33 | 14 % | energia | 34 % | — |
| gemini: la decision del 13/10, equilibrada | 69 | 69 | 69 | 71 | 4 | 5 | 6 | — | 0 % | 35.9 | 31 | 88 % | energia | 0 % | — |
| gemini sin cartas | 29 | 38 | 38 | 39 | — | — | — | — | 0 % | 13.6 | 0 | 21 % | energia | 0 % | — |

### Perfil medio

| variante | mejor p10 | mejor p30 | muro (ult. 20) | mejor al final | llega a 40 | a 50 | a 60 | a 80 | al tope (100) | min. en el muro | cartas por partida | % muertes en jefe | arma en el muro | sin balas con blanco | revivio |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 47 | 57 | 56 | 59 | 9 | 13 | — | — | 0 % | 52.0 | 0 | 37 % | energia | 2 % | — |
| 1.6.0, equilibrada | 86 | 93 | 86 | 97 | 3 | 3 | 4 | 6 | 0 % | 121.7 | 38 | 15 % | energia | 15 % | — |
| gemini: la decision del 13/10, equilibrada | 74 | 76 | 74 | 77 | 3 | 3 | 3 | — | 0 % | 39.0 | 32 | 39 % | energia | 0 % | — |
| gemini sin cartas | 39 | 43 | 43 | 45 | 14 | — | — | — | 0 % | 14.5 | 0 | 25 % | energia | 0 % | — |

### Perfil alto

| variante | mejor p10 | mejor p30 | muro (ult. 20) | mejor al final | llega a 40 | a 50 | a 60 | a 80 | al tope (100) | min. en el muro | cartas por partida | % muertes en jefe | arma en el muro | sin balas con blanco | revivio |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 67 | 69 | 69 | 71 | 5 | 5 | 8 | — | 0 % | 76.0 | 0 | 58 % | energia | 0 % | — |
| 1.6.0, equilibrada | 100 | 100 | 100 | 100 | 2 | 2 | 2 | 3 | 95 % | 156.3 | 40 | 87 % | energia | 8 % | — |
| gemini: la decision del 13/10, equilibrada | 82 | 84 | 82 | 86 | 2 | 2 | 3 | 4 | 0 % | 46.5 | 33 | 41 % | energia | 0 % | — |
| gemini sin cartas | 49 | 49 | 49 | 52 | 5 | 41 | — | — | 0 % | 16.3 | 0 | 26 % | energia | 0 % | — |

## El arranque

### Perfil bajo: las primeras 12 partidas (oleada a la que se llega, mediana; entre parentesis el arma con que se arranca)

| variante | p1 | p2 | p3 | p4 | p5 | p6 | p7 | p8 | p9 | p10 | p11 | p12 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 1 (pis) | 2 (pis) | 2 (rev) | 2 (rev) | 3 (rev) | 5 (esc) | 6 (esc) | 9 (esc) | 9 (esc) | 11 (rif) | 19 (rif) | 19 (rif) |
| 1.6.0, equilibrada | 2 (pis) | 2 (rev) | 9 (rev) | 39 (esc) | 19 (rif) | 38 (ene) | 39 (ene) | 39 (ene) | 48 (ene) | 49 (ene) | 51 (ene) | 50 (ene) |
| gemini: la decision del 13/10, equilibrada | 3 (pis) | 9 (rev) | 19 (esc) | 49 (rif) | 59 (ene) | 67 (ene) | 67 (ene) | 67 (ene) | 68 (ene) | 68 (ene) | 69 (ene) | 69 (ene) |
| gemini sin cartas | 2 (pis) | 3 (rev) | 6 (esc) | 8 (esc) | 11 (esc) | 14 (rif) | 18 (rif) | 19 (rif) | 26 (rif) | 29 (rif) | 32 (ene) | 34 (ene) |

### Perfil medio: las primeras 12 partidas (oleada a la que se llega, mediana; entre parentesis el arma con que se arranca)

| variante | p1 | p2 | p3 | p4 | p5 | p6 | p7 | p8 | p9 | p10 | p11 | p12 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 3 (pis) | 3 (rev) | 7 (esc) | 9 (esc) | 19 (esc) | 26 (rif) | 29 (rif) | 39 (rif) | 47 (ene) | 40 (ene) | 49 (ene) | 49 (ene) |
| 1.6.0, equilibrada | 5 (pis) | 39 (esc) | 54 (rif) | 76 (ene) | 78 (ene) | 78 (ene) | 86 (ene) | 79 (ene) | 83 (ene) | 81 (ene) | 83 (ene) | 81 (ene) |
| gemini: la decision del 13/10, equilibrada | 9 (pis) | 35 (esc) | 64 (rif) | 69 (ene) | 69 (ene) | 69 (ene) | 69 (ene) | 73 (ene) | 69 (ene) | 73 (ene) | 69 (ene) | 73 (ene) |
| gemini sin cartas | 4 (pis) | 6 (rev) | 13 (esc) | 21 (rif) | 28 (rif) | 33 (rif) | 38 (ene) | 38 (ene) | 39 (ene) | 39 (ene) | 39 (ene) | 39 (ene) |

### Perfil alto: las primeras 12 partidas (oleada a la que se llega, mediana; entre parentesis el arma con que se arranca)

| variante | p1 | p2 | p3 | p4 | p5 | p6 | p7 | p8 | p9 | p10 | p11 | p12 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 5 (pis) | 12 (esc) | 29 (esc) | 39 (rif) | 55 (ene) | 59 (ene) | 59 (ene) | 62 (ene) | 66 (ene) | 59 (ene) | 65 (ene) | 67 (ene) |
| 1.6.0, equilibrada | 9 (pis) | 79 (esc) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) |
| gemini: la decision del 13/10, equilibrada | 17 (pis) | 59 (rif) | 69 (ene) | 80 (ene) | 79 (ene) | 81 (ene) | 79 (ene) | 69 (ene) | 79 (ene) | 79 (ene) | 79 (ene) | 80 (ene) |
| gemini sin cartas | 6 (pis) | 15 (esc) | 30 (rif) | 39 (rif) | 44 (ene) | 44 (ene) | 46 (ene) | 47 (ene) | 47 (ene) | 48 (ene) | 45 (ene) | 46 (ene) |

## El oro por minuto

Las monedas de la partida (las juntadas, el bono de cada oleada y el tesoro) sobre sus minutos (con 3 s por eleccion de carta), sumadas las de todas las semillas.

### Perfil bajo

| variante | oleada en la p10 | monedas en la p10 | min. de la p10 | oro/min en la p10 | oro/min p8-p12 | oro/min en el muro (ult. 20) | monedas por partida en el muro |
|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 11 | 2012 | 5.3 | 524 | 830 | 4745 | 51897 |
| 1.6.0, equilibrada | 49 | 507524 | 45.1 | 11445 | 11930 | 21391 | 1215957 |
| gemini: la decision del 13/10, equilibrada | 68 | 3573684 | 35.5 | 93194 | 96262 | 107109 | 3871170 |
| gemini sin cartas | 29 | 47227 | 9.5 | 5009 | 5735 | 14273 | 196570 |

### Perfil medio

| variante | oleada en la p10 | monedas en la p10 | min. de la p10 | oro/min en la p10 | oro/min p8-p12 | oro/min en el muro (ult. 20) | monedas por partida en el muro |
|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 40 | 195821 | 28.1 | 9238 | 10520 | 20809 | 1108401 |
| 1.6.0, equilibrada | 81 | 11960687 | 107.2 | 120459 | 113596 | 165719 | 18727345 |
| gemini: la decision del 13/10, equilibrada | 73 | 5703703 | 38.4 | 140366 | 140564 | 160936 | 6589553 |
| gemini sin cartas | 39 | 214744 | 13.1 | 16618 | 15820 | 21724 | 318254 |

### Perfil alto

| variante | oleada en la p10 | monedas en la p10 | min. de la p10 | oro/min en la p10 | oro/min p8-p12 | oro/min en el muro (ult. 20) | monedas por partida en el muro |
|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 59 | 1502065 | 56.7 | 33953 | 37368 | 47410 | 3706336 |
| 1.6.0, equilibrada | 100 | 59886795 | 156.3 | 382907 | 383932 | 383746 | 60085053 |
| gemini: la decision del 13/10, equilibrada | 79 | 10244456 | 43.3 | 262010 | 234743 | 269640 | 12894195 |
| gemini sin cartas | 48 | 536654 | 16.5 | 31394 | 30038 | 34846 | 569928 |

## Los minutos y el oro hasta cada oleada

Al terminar la oleada N, en las partidas que la terminan (mediana de todas, de todas las semillas; "—" con menos de 10).

### Perfil bajo

| variante | min. hasta la 10 | min. hasta la 20 | min. hasta la 30 | min. hasta la 40 | min. hasta la 50 | min. hasta la 55 | min. hasta la 60 | min. hasta la 70 | oro hasta la 10 | oro hasta la 30 | oro hasta la 50 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 2.9 | 8.3 | 16.4 | 28.2 | — | — | — | — | 3188 | 65343 | — |
| 1.6.0, equilibrada | 3.6 | 9.6 | 18.5 | 30.5 | 45.2 | 53.6 | 63.0 | — | 3243 | 65566 | 565927 |
| gemini: la decision del 13/10, equilibrada | 2.9 | 6.0 | 9.3 | 13.4 | 18.7 | 22.5 | 26.9 | 37.7 | 4315 | 74066 | 609438 |
| gemini sin cartas | 2.4 | 5.5 | 9.5 | — | — | — | — | — | 4313 | 74831 | — |

### Perfil medio

| variante | min. hasta la 10 | min. hasta la 20 | min. hasta la 30 | min. hasta la 40 | min. hasta la 50 | min. hasta la 55 | min. hasta la 60 | min. hasta la 70 | oro hasta la 10 | oro hasta la 30 | oro hasta la 50 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 2.8 | 8.0 | 15.8 | 26.4 | 40.8 | 49.0 | — | — | 3323 | 66746 | 574592 |
| 1.6.0, equilibrada | 3.4 | 9.1 | 17.7 | 29.5 | 44.0 | 52.2 | 61.1 | 81.0 | 3333 | 66963 | 574462 |
| gemini: la decision del 13/10, equilibrada | 2.2 | 4.8 | 8.0 | 11.8 | 17.0 | 20.3 | 24.4 | 34.4 | 4329 | 75178 | 616465 |
| gemini sin cartas | 1.7 | 4.2 | 8.0 | 12.8 | — | — | — | — | 4335 | 75852 | — |

### Perfil alto

| variante | min. hasta la 10 | min. hasta la 20 | min. hasta la 30 | min. hasta la 40 | min. hasta la 50 | min. hasta la 55 | min. hasta la 60 | min. hasta la 70 | oro hasta la 10 | oro hasta la 30 | oro hasta la 50 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 2.8 | 8.0 | 15.6 | 26.0 | 40.4 | 48.5 | 57.4 | — | 3429 | 67968 | 581530 |
| 1.6.0, equilibrada | 3.4 | 9.0 | 17.6 | 29.4 | 43.9 | 52.0 | 60.9 | 80.6 | 3433 | 68137 | 582287 |
| gemini: la decision del 13/10, equilibrada | 2.2 | 4.8 | 8.0 | 11.8 | 16.8 | 20.0 | 24.0 | 33.6 | 4477 | 76803 | 625960 |
| gemini sin cartas | 1.7 | 4.0 | 7.0 | 11.4 | 17.7 | — | — | — | 4478 | 76634 | 630298 |

## Las cartas

### Cartas elegidas al terminar cada oleada (promedio de todas las partidas que la terminan)

| variante | perfil | 1 | 2 | 3 | 4 | 5 | 6 | 8 | 10 | 15 | 20 | 30 | 40 | 50 | 60 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.6.0, equilibrada | bajo | 1.0 | 2.0 | 3.0 | 4.2 | 6.0 | 7.0 | 9.4 | 12.0 | 16.0 | 19.0 | 24.0 | 28.0 | 31.0 | 33.0 |
| 1.6.0, equilibrada | medio | 1.0 | 2.0 | 3.0 | 4.2 | 6.0 | 7.0 | 9.4 | 12.0 | 16.0 | 19.0 | 24.0 | 28.0 | 31.0 | 33.0 |
| 1.6.0, equilibrada | alto | 1.0 | 2.0 | 3.0 | 4.3 | 6.0 | 7.0 | 9.5 | 12.0 | 16.0 | 19.0 | 24.0 | 28.0 | 31.0 | 33.0 |
| gemini: la decision del 13/10, equilibrada | bajo | 1.0 | 2.0 | 3.0 | 4.0 | 4.2 | 5.6 | 7.4 | 9.6 | 13.0 | 16.0 | 20.4 | 24.0 | 27.0 | 29.0 |
| gemini: la decision del 13/10, equilibrada | medio | 1.0 | 2.0 | 3.0 | 4.0 | 4.3 | 5.7 | 7.5 | 9.6 | 13.0 | 16.0 | 20.6 | 24.0 | 27.0 | 29.0 |
| gemini: la decision del 13/10, equilibrada | alto | 1.0 | 2.0 | 3.0 | 4.0 | 4.4 | 5.8 | 7.7 | 9.7 | 13.0 | 16.0 | 20.7 | 24.0 | 27.0 | 29.0 |

### Perfil bajo: cuantas veces tiene cada carta al morir en el muro (promedio de las ultimas 20 partidas)

| variante | cadencia | critico | dano | disparo_doble | dron | escudo | explosiva | furia_larga | granada_rapida | hacia_atras | hielo | iman | perforante | quemadura | vampiro | velocidad | vida | doradas |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.6.0, equilibrada | 4.0 | 1.6 | 4.0 | 2.0 | 1.9 | 1.0 | 2.0 | 1.8 | 1.8 | 1.0 | 2.0 | 1.2 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 4.8 |
| gemini: la decision del 13/10, equilibrada | 4.0 | 1.3 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 1.3 | 1.7 | 1.0 | 2.0 | 0.9 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 5.4 |

### Perfil medio: cuantas veces tiene cada carta al morir en el muro (promedio de las ultimas 20 partidas)

| variante | cadencia | critico | dano | disparo_doble | dron | escudo | explosiva | furia_larga | granada_rapida | hacia_atras | hielo | iman | perforante | quemadura | vampiro | velocidad | vida | doradas |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.6.0, equilibrada | 4.0 | 3.0 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 6.0 |
| gemini: la decision del 13/10, equilibrada | 4.0 | 1.8 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 1.6 | 1.9 | 1.0 | 2.0 | 1.4 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 6.2 |

### Perfil alto: cuantas veces tiene cada carta al morir en el muro (promedio de las ultimas 20 partidas)

| variante | cadencia | critico | dano | disparo_doble | dron | escudo | explosiva | furia_larga | granada_rapida | hacia_atras | hielo | iman | perforante | quemadura | vampiro | velocidad | vida | doradas |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.6.0, equilibrada | 4.0 | 3.0 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 6.0 |
| gemini: la decision del 13/10, equilibrada | 4.0 | 2.3 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 1.8 | 1.9 | 1.0 | 2.0 | 1.5 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 6.7 |

# Las piezas de la decisión

Corridas aparte con las mismas semillas (`variantes=gemini_sin_tercero,gemini_cadencia_con_costo,gemini_moneda_x1,gemini_curva_e`, 667 s): la decisión con una pieza cambiada por vez.

### El muro


#### Perfil bajo

| variante | mejor p10 | mejor p30 | muro (ult. 20) | mejor al final | llega a 40 | a 50 | a 60 | a 80 | al tope (100) | min. en el muro | cartas por partida | % muertes en jefe | arma en el muro | sin balas con blanco | revivio |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| gemini sin el tercer tramo, equilibrada | 100 | 100 | 99 | 100 | 4 | 4 | 4 | 5 | 49 % | 60.3 | 36 | 97 % | energia | 0 % | — |
| gemini con la cadencia gastando balas como hoy, equilibrada | 59 | 65 | 58 | 68 | 4 | 5 | 12 | — | 0 % | 26.1 | 29 | 19 % | energia | 6 % | — |
| gemini con la moneda x1, equilibrada | 68 | 69 | 69 | 70 | 4 | 5 | 6 | — | 0 % | 35.6 | 31 | 82 % | energia | 0 % | — |
| gemini con la curva de la E (x1,15 y x1,07 desde la 11, sin tercer tramo), equilibrada | 59 | 60 | 59 | 63 | 5 | 6 | 24 | — | 0 % | 29.4 | 29 | 83 % | energia | 0 % | — |

#### Perfil medio

| variante | mejor p10 | mejor p30 | muro (ult. 20) | mejor al final | llega a 40 | a 50 | a 60 | a 80 | al tope (100) | min. en el muro | cartas por partida | % muertes en jefe | arma en el muro | sin balas con blanco | revivio |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| gemini sin el tercer tramo, equilibrada | 100 | 100 | 100 | 100 | 2 | 3 | 3 | 3 | 98 % | 57.6 | 36 | 90 % | energia | 0 % | — |
| gemini con la cadencia gastando balas como hoy, equilibrada | 67 | 69 | 60 | 73 | 3 | 3 | 5 | — | 0 % | 25.9 | 29 | 19 % | energia | 7 % | — |
| gemini con la moneda x1, equilibrada | 73 | 75 | 73 | 77 | 3 | 3 | 4 | — | 0 % | 37.9 | 32 | 45 % | energia | 0 % | — |
| gemini con la curva de la E (x1,15 y x1,07 desde la 11, sin tercer tramo), equilibrada | 67 | 69 | 68 | 70 | 3 | 3 | 4 | — | 0 % | 35.5 | 31 | 44 % | energia | 0 % | — |

#### Perfil alto

| variante | mejor p10 | mejor p30 | muro (ult. 20) | mejor al final | llega a 40 | a 50 | a 60 | a 80 | al tope (100) | min. en el muro | cartas por partida | % muertes en jefe | arma en el muro | sin balas con blanco | revivio |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| gemini sin el tercer tramo, equilibrada | 100 | 100 | 100 | 100 | 2 | 2 | 2 | 3 | 98 % | 55.9 | 36 | 59 % | energia | 0 % | — |
| gemini con la cadencia gastando balas como hoy, equilibrada | 79 | 80 | 73 | 82 | 2 | 2 | 3 | 26 | 0 % | 37.2 | 32 | 24 % | energia | 8 % | — |
| gemini con la moneda x1, equilibrada | 80 | 84 | 80 | 85 | 2 | 2 | 3 | 9 | 0 % | 44.8 | 33 | 37 % | energia | 0 % | — |
| gemini con la curva de la E (x1,15 y x1,07 desde la 11, sin tercer tramo), equilibrada | 76 | 79 | 76 | 80 | 2 | 3 | 3 | 25 | 0 % | 43.2 | 32 | 44 % | energia | 0 % | — |

### El oro por minuto


Las monedas de la partida (las juntadas, el bono de cada oleada y el tesoro) sobre sus minutos (con 3 s por eleccion de carta), sumadas las de todas las semillas.

#### Perfil bajo

| variante | oleada en la p10 | monedas en la p10 | min. de la p10 | oro/min en la p10 | oro/min p8-p12 | oro/min en el muro (ult. 20) | monedas por partida en el muro |
|---|---|---|---|---|---|---|---|
| gemini sin el tercer tramo, equilibrada | 99 | 60640923 | 61.3 | 995278 | 948417 | 1005189 | 61451833 |
| gemini con la cadencia gastando balas como hoy, equilibrada | 55 | 1047681 | 24.3 | 44060 | 50276 | 56152 | 1405332 |
| gemini con la moneda x1, equilibrada | 68 | 1710048 | 35.8 | 45051 | 44631 | 52230 | 1929988 |
| gemini con la curva de la E (x1,15 y x1,07 desde la 11, sin tercer tramo), equilibrada | 59 | 1449484 | 29.0 | 47658 | 48261 | 54574 | 1538031 |

#### Perfil medio

| variante | oleada en la p10 | monedas en la p10 | min. de la p10 | oro/min en la p10 | oro/min p8-p12 | oro/min en el muro (ult. 20) | monedas por partida en el muro |
|---|---|---|---|---|---|---|---|
| gemini sin el tercer tramo, equilibrada | 100 | 62252384 | 58.4 | 1064849 | 1063312 | 1073436 | 62133214 |
| gemini con la cadencia gastando balas como hoy, equilibrada | 63 | 2269767 | 28.9 | 79338 | 74514 | 78453 | 1836744 |
| gemini con la moneda x1, equilibrada | 69 | 2087802 | 34.5 | 66367 | 67496 | 75230 | 2929155 |
| gemini con la curva de la E (x1,15 y x1,07 desde la 11, sin tercer tramo), equilibrada | 59 | 1623730 | 27.6 | 64710 | 71284 | 96830 | 3584886 |

#### Perfil alto

| variante | oleada en la p10 | monedas en la p10 | min. de la p10 | oro/min en la p10 | oro/min p8-p12 | oro/min en el muro (ult. 20) | monedas por partida en el muro |
|---|---|---|---|---|---|---|---|
| gemini sin el tercer tramo, equilibrada | 100 | 62673554 | 56.4 | 1103383 | 1096498 | 1113115 | 62487990 |
| gemini con la cadencia gastando balas como hoy, equilibrada | 69 | 3950019 | 32.8 | 113102 | 141837 | 168102 | 5809900 |
| gemini con la moneda x1, equilibrada | 80 | 5646696 | 45.2 | 126890 | 117128 | 131062 | 5532253 |
| gemini con la curva de la E (x1,15 y x1,07 desde la 11, sin tercer tramo), equilibrada | 74 | 6663050 | 42.1 | 153745 | 144680 | 169731 | 7572854 |

### Los minutos y el oro hasta cada oleada


Al terminar la oleada N, en las partidas que la terminan (mediana de todas, de todas las semillas; "—" con menos de 10).

#### Perfil bajo

| variante | min. hasta la 10 | min. hasta la 20 | min. hasta la 30 | min. hasta la 40 | min. hasta la 50 | min. hasta la 55 | min. hasta la 60 | min. hasta la 70 | oro hasta la 10 | oro hasta la 30 | oro hasta la 50 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| gemini sin el tercer tramo, equilibrada | 2.9 | 5.8 | 9.1 | 13.0 | 17.6 | 20.8 | 24.8 | 33.6 | 4321 | 73905 | 606263 |
| gemini con la cadencia gastando balas como hoy, equilibrada | 2.9 | 6.0 | 9.4 | 13.5 | 18.9 | 23.1 | 28.3 | — | 4308 | 74230 | 610164 |
| gemini con la moneda x1, equilibrada | 2.9 | 6.0 | 9.4 | 13.4 | 18.8 | 22.6 | 27.0 | — | 2365 | 38593 | 308776 |
| gemini con la curva de la E (x1,15 y x1,07 desde la 11, sin tercer tramo), equilibrada | 2.9 | 6.1 | 9.9 | 14.6 | 21.0 | 25.0 | 30.2 | — | 4299 | 74331 | 612190 |

#### Perfil medio

| variante | min. hasta la 10 | min. hasta la 20 | min. hasta la 30 | min. hasta la 40 | min. hasta la 50 | min. hasta la 55 | min. hasta la 60 | min. hasta la 70 | oro hasta la 10 | oro hasta la 30 | oro hasta la 50 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| gemini sin el tercer tramo, equilibrada | 2.2 | 4.8 | 8.0 | 11.8 | 16.3 | 18.7 | 22.6 | 31.3 | 4339 | 75065 | 615710 |
| gemini con la cadencia gastando balas como hoy, equilibrada | 2.2 | 4.8 | 8.0 | 11.9 | 17.1 | 20.5 | 24.8 | 35.0 | 4341 | 75187 | 617301 |
| gemini con la moneda x1, equilibrada | 2.2 | 4.8 | 8.0 | 11.9 | 17.0 | 20.3 | 24.5 | 34.6 | 2338 | 39022 | 312634 |
| gemini con la curva de la E (x1,15 y x1,07 desde la 11, sin tercer tramo), equilibrada | 2.2 | 4.8 | 8.2 | 12.8 | 18.4 | 22.3 | 27.4 | — | 4345 | 75216 | 620045 |

#### Perfil alto

| variante | min. hasta la 10 | min. hasta la 20 | min. hasta la 30 | min. hasta la 40 | min. hasta la 50 | min. hasta la 55 | min. hasta la 60 | min. hasta la 70 | oro hasta la 10 | oro hasta la 30 | oro hasta la 50 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| gemini sin el tercer tramo, equilibrada | 2.2 | 4.8 | 8.0 | 11.8 | 16.2 | 18.7 | 21.8 | 30.5 | 4459 | 76803 | 625896 |
| gemini con la cadencia gastando balas como hoy, equilibrada | 2.2 | 4.8 | 8.0 | 11.8 | 16.8 | 20.1 | 24.1 | 33.8 | 4485 | 76758 | 625373 |
| gemini con la moneda x1, equilibrada | 2.2 | 4.8 | 8.0 | 11.8 | 16.8 | 20.0 | 24.1 | 33.7 | 2431 | 40004 | 317370 |
| gemini con la curva de la E (x1,15 y x1,07 desde la 11, sin tercer tramo), equilibrada | 2.2 | 4.8 | 8.0 | 12.5 | 17.7 | 21.4 | 26.1 | 37.2 | 4472 | 76689 | 628397 |
