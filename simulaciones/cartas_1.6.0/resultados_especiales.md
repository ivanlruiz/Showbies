# Los zombis con habilidad (18/10)

El juego de hoy (`poder_28_112`: la V3 de Argon con el PODER 28 x 1,12^L) contra el mismo juego con los cinco
especiales de `ZOMBIS.md` en la mezcla de WaveMode (`especiales`), con +1 de PODER a cada uno (`especiales_poder`), con
sus numeros y sin sus habilidades (`especiales_sin_habilidades`), de a uno (`solo_*`) y con lo que esta puesto a ojo
movido (cuanto se esquivan sus avisos, y los zombis en cuatro chorros en vez de por rumbos parejos). Todas con las
mismas semillas; con especiales, todo paso a paso desde la oleada 12 (`actual_paso_a_paso` es hoy con esa regla, y da
partida por partida lo mismo que `poder_28_112`). Como se modela cada uno, en el README ("Los zombis con habilidad").

## En corto

El muro (la mediana de las ultimas 20 partidas de cada semilla) y, entre parentesis, los minutos de una partida ahi:

| | bajo | medio | alto |
|---|---|---|---|
| hoy, sin especiales (`poder_28_112`) | 39 (16,2) | 59 (30,2) | 69 (38,6) |
| **con los cinco (`especiales`)** | **39** (16,6) | **59** (31,1) | **69** (40,1) |
| con los cinco y +1 de PODER (`especiales_poder`) | 43 (18,9) | 59 (30,7) | 70 (41,0) |
| con los cinco, sin sus habilidades (solo sus numeros) | 42 (18,1) | 59 (31,0) | 69 (39,3) |
| solo el escupidor / saltador / chaman / blindado / hinchado | 39 / 39 / 39 / 40 / 39 | 59 en los cinco | 69 / 69 / 69 / 69 / 70 |
| con los cinco, esquivando el 90 % de sus avisos | 46 (20,6) | 59 (31,0) | 69 (39,8) |
| con los cinco, esquivando la mitad que al jefe | 39 (15,7) | 54 (26,7) | 63 (33,8) |
| con los cinco, en cuatro chorros | 39 (16,5) | 59 (30,4) | 69 (39,8) |
| con los cinco y +1 de PODER, esquivando la mitad | 39 (15,8) | 56 (28,3) | 65 (34,8) |

Con 10 semillas (la tercera corrida; la diferencia es la de la media de las ultimas 20 partidas, semilla por semilla,
contra hoy, con su error):

| | bajo | medio | alto |
|---|---|---|---|
| hoy: muro, media de las ultimas 20, las que terminan antes de la 35 | 39, 41,7, 5 % | 59, 58,1, 0 % | 69, 68,8, 0 % |
| con los cinco | 39, 41,5, 2 % (-0,2 +-0,4) | 59, 58,1 (0,0 +-0,3) | 69, 68,8 (0,0 +-0,3) |
| con los cinco y +1 de PODER | 43,5, 43,2, 2 % (+1,5 +-0,3) | 59, 58,7 (+0,5 +-0,4) | 69,5, 69,8 (+1,0 +-0,4) |
| con los cinco, esquivando la mitad | 39, 37,7, 18 % (-4,0 +-0,2) | 54, 53,5 (-4,6 +-0,3) | 63, 62,7 (-6,1 +-0,4) |
| con los cinco y +1 de PODER, esquivando la mitad | 39, 38,2, 12 % (-3,5 +-0,4) | 56, 54,4 (-3,7 +-0,4) | 65, 63,5 (-5,2 +-0,4) |

- **El muro no se mueve: 39 / 59 / 69 con los cinco**, y con 10 semillas la media de las ultimas 20 partidas tampoco
  (-0,2, 0,0 y 0,0, con un error de 0,3-0,4). Los tres muros son
  jefes (el bajo muere en el de la 40, el medio en el de la 60 y el alto en el de la 70), y los especiales no cambian
  cuanto aguanta el jugador al jefe.
- **Lo que cuestan sus habilidades lo devuelve lo que pagan**: sin habilidades (solo sus numeros: mas vida, mas monedas
  y mas PODER) el bajo llega a la 42; con ellas, a la 39. El oro por minuto en el muro sube un 16, 20 y 21 % (1.930 ->
  2.243, 2.962 -> 3.540, 3.919 -> 4.734) y el oro por partida un 17, 22 y 27 %, y la partida dura 0,4-1,5 minutos mas.
- **Ninguno solo mueve el muro mas de una oleada.** El que mas pega es el escupidor: solo, su charco es el 18-24 % del danio
  que recibe el jugador (con los cinco: charco 19 %, salto 7 % y explosion 12 % en el bajo). El escudo del blindado
  casi no para nada (1-3 % de las balas comunes que le van): la carta de perforante, que lo anula, ya suele estar en
  el mazo cuando sale.
- **La regla de Argon no salta**: el bajo queda en la 39 (media de las ultimas 20, 41,5), por encima de la 35, tambien
  con los avisos esquivados la mitad (39; media 37,7, con el 18 % de las partidas del muro terminando antes de la 35).
- **Lo que puede darlo vuelta es cuanto se esquivan los avisos** (el charco, la franja del saltador y el circulo del
  hinchado), que esta puesto a ojo como lo que se esquiva al jefe: esquivando la mitad, el medio y el alto pierden 5 y 6
  oleadas (54 y 63), y +1 de PODER les devuelve una o dos.
## Primera corrida: hoy, con los cinco, con +1 de PODER y sin habilidades (100 partidas x 5 semillas)

partidas 100, semillas 5, perfiles bajo,medio,alto, 718 s

mejor pN: la mejor oleada completada hasta la partida N (mediana de las semillas). muro: la oleada mediana de las ultimas 20 partidas de cada semilla. llega a N: la partida en que se completa por primera vez la oleada N. min. en el muro: lo que dura una de esas partidas, con 3 s por eleccion de carta. % muertes en jefe: de las ultimas 100 partidas de cada semilla.

### El muro

#### Perfil bajo

| variante | mejor p10 | mejor p30 | muro (ult. 20) | mejor al final | llega a 40 | a 50 | a 60 | a 80 | al tope (100) | min. en el muro | cartas por partida | % muertes en jefe | arma en el muro | sin balas con blanco | revivio |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| V3 con el PODER 28 x 1,12^L | 8 | 37 | 39 | 49 | 43 | — | — | — | 0 % | 16.2 | 21 | 40 % | energia | 0 % | — |
| hoy sin especiales, paso a paso desde la 12 | 8 | 37 | 39 | 49 | 43 | — | — | — | 0 % | 16.2 | 21 | 40 % | energia | 0 % | — |
| chequeo: el sorteo al aparecer sin especiales (tiene que dar lo de arriba) | 8 | 37 | 39 | 49 | 43 | — | — | — | 0 % | 16.2 | 21 | 40 % | energia | 0 % | — |
| hoy con los cinco especiales | 8 | 38 | 39 | 49 | 40 | — | — | — | 0 % | 16.6 | 22 | 45 % | energia | 0 % | — |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 8 | 38 | 43 | 49 | 44 | — | — | — | 0 % | 18.9 | 24 | 43 % | energia | 0 % | — |
| con los especiales sin habilidades (solo sus numeros) | 8 | 39 | 42 | 49 | 33 | — | — | — | 0 % | 18.1 | 23 | 43 % | energia | 0 % | — |

#### Perfil medio

| variante | mejor p10 | mejor p30 | muro (ult. 20) | mejor al final | llega a 40 | a 50 | a 60 | a 80 | al tope (100) | min. en el muro | cartas por partida | % muertes en jefe | arma en el muro | sin balas con blanco | revivio |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| V3 con el PODER 28 x 1,12^L | 48 | 59 | 59 | 65 | 10 | 13 | 33 | — | 0 % | 30.2 | 27 | 60 % | energia | 0 % | — |
| hoy sin especiales, paso a paso desde la 12 | 48 | 59 | 59 | 65 | 10 | 13 | 33 | — | 0 % | 30.2 | 27 | 60 % | energia | 0 % | — |
| chequeo: el sorteo al aparecer sin especiales (tiene que dar lo de arriba) | 48 | 59 | 59 | 65 | 10 | 13 | 33 | — | 0 % | 30.2 | 27 | 60 % | energia | 0 % | — |
| hoy con los cinco especiales | 48 | 61 | 59 | 65 | 10 | 12 | 24 | — | 0 % | 31.1 | 28 | 58 % | energia | 0 % | — |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 49 | 61 | 59 | 64 | 9 | 11 | 19 | — | 0 % | 30.7 | 29 | 63 % | energia | 0 % | — |
| con los especiales sin habilidades (solo sus numeros) | 47 | 61 | 59 | 66 | 9 | 12 | 24 | — | 0 % | 31.0 | 28 | 59 % | energia | 0 % | — |

#### Perfil alto

| variante | mejor p10 | mejor p30 | muro (ult. 20) | mejor al final | llega a 40 | a 50 | a 60 | a 80 | al tope (100) | min. en el muro | cartas por partida | % muertes en jefe | arma en el muro | sin balas con blanco | revivio |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| V3 con el PODER 28 x 1,12^L | 66 | 73 | 69 | 76 | 4 | 5 | 7 | — | 0 % | 38.6 | 29 | 38 % | energia | 0 % | — |
| hoy sin especiales, paso a paso desde la 12 | 66 | 73 | 69 | 76 | 4 | 5 | 7 | — | 0 % | 38.6 | 29 | 38 % | energia | 0 % | — |
| chequeo: el sorteo al aparecer sin especiales (tiene que dar lo de arriba) | 66 | 73 | 69 | 76 | 4 | 5 | 7 | — | 0 % | 38.6 | 29 | 38 % | energia | 0 % | — |
| hoy con los cinco especiales | 63 | 71 | 69 | 75 | 5 | 6 | 8 | — | 0 % | 40.1 | 30 | 44 % | energia | 0 % | — |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 68 | 73 | 70 | 76 | 5 | 5 | 7 | — | 0 % | 41.0 | 32 | 46 % | energia | 0 % | — |
| con los especiales sin habilidades (solo sus numeros) | 66 | 75 | 69 | 76 | 5 | 6 | 8 | — | 0 % | 39.3 | 30 | 43 % | energia | 0 % | — |

### El arranque

#### Perfil bajo: las primeras 12 partidas (oleada a la que se llega, mediana; entre parentesis el arma con que se arranca)

| variante | p1 | p2 | p3 | p4 | p5 | p6 | p7 | p8 | p9 | p10 | p11 | p12 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| V3 con el PODER 28 x 1,12^L | 1 (pis) | 2 (pis) | 2 (rev) | 2 (rev) | 3 (rev) | 3 (rev) | 5 (esc) | 6 (esc) | 7 (esc) | 7 (esc) | 9 (esc) | 9 (esc) |
| hoy sin especiales, paso a paso desde la 12 | 1 (pis) | 2 (pis) | 2 (rev) | 2 (rev) | 3 (rev) | 3 (rev) | 5 (esc) | 6 (esc) | 7 (esc) | 7 (esc) | 9 (esc) | 9 (esc) |
| chequeo: el sorteo al aparecer sin especiales (tiene que dar lo de arriba) | 1 (pis) | 2 (pis) | 2 (rev) | 2 (rev) | 3 (rev) | 3 (rev) | 5 (esc) | 6 (esc) | 7 (esc) | 7 (esc) | 9 (esc) | 9 (esc) |
| hoy con los cinco especiales | 1 (pis) | 2 (pis) | 2 (rev) | 2 (rev) | 3 (rev) | 3 (rev) | 5 (esc) | 6 (esc) | 7 (esc) | 7 (esc) | 10 (esc) | 9 (esc) |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 1 (pis) | 2 (pis) | 2 (rev) | 2 (rev) | 3 (rev) | 3 (rev) | 5 (esc) | 6 (esc) | 7 (esc) | 7 (esc) | 10 (esc) | 9 (esc) |
| con los especiales sin habilidades (solo sus numeros) | 1 (pis) | 2 (pis) | 2 (rev) | 2 (rev) | 3 (rev) | 3 (rev) | 5 (esc) | 6 (esc) | 7 (esc) | 7 (esc) | 10 (esc) | 9 (esc) |

#### Perfil medio: las primeras 12 partidas (oleada a la que se llega, mediana; entre parentesis el arma con que se arranca)

| variante | p1 | p2 | p3 | p4 | p5 | p6 | p7 | p8 | p9 | p10 | p11 | p12 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| V3 con el PODER 28 x 1,12^L | 5 (pis) | 6 (rev) | 9 (esc) | 15 (esc) | 24 (esc) | 28 (rif) | 29 (rif) | 37 (rif) | 39 (rif) | 48 (rif) | 39 (rif) | 49 (ene) |
| hoy sin especiales, paso a paso desde la 12 | 5 (pis) | 6 (rev) | 9 (esc) | 15 (esc) | 24 (esc) | 28 (rif) | 29 (rif) | 37 (rif) | 39 (rif) | 48 (rif) | 39 (rif) | 49 (ene) |
| chequeo: el sorteo al aparecer sin especiales (tiene que dar lo de arriba) | 5 (pis) | 6 (rev) | 9 (esc) | 15 (esc) | 24 (esc) | 28 (rif) | 29 (rif) | 37 (rif) | 39 (rif) | 48 (rif) | 39 (rif) | 49 (ene) |
| hoy con los cinco especiales | 5 (pis) | 6 (rev) | 9 (esc) | 16 (esc) | 19 (esc) | 27 (rif) | 29 (rif) | 32 (rif) | 39 (rif) | 47 (rif) | 48 (ene) | 49 (ene) |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 5 (pis) | 6 (rev) | 9 (esc) | 14 (esc) | 26 (esc) | 31 (rif) | 39 (rif) | 39 (rif) | 47 (rif) | 49 (ene) | 49 (ene) | 49 (ene) |
| con los especiales sin habilidades (solo sus numeros) | 5 (pis) | 6 (rev) | 9 (esc) | 16 (esc) | 22 (esc) | 27 (rif) | 29 (rif) | 38 (rif) | 39 (rif) | 47 (rif) | 49 (ene) | 51 (ene) |

#### Perfil alto: las primeras 12 partidas (oleada a la que se llega, mediana; entre parentesis el arma con que se arranca)

| variante | p1 | p2 | p3 | p4 | p5 | p6 | p7 | p8 | p9 | p10 | p11 | p12 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| V3 con el PODER 28 x 1,12^L | 6 (pis) | 16 (esc) | 30 (esc) | 45 (rif) | 57 (rif) | 58 (ene) | 59 (ene) | 66 (ene) | 64 (ene) | 64 (ene) | 65 (ene) | 64 (ene) |
| hoy sin especiales, paso a paso desde la 12 | 6 (pis) | 16 (esc) | 30 (esc) | 45 (rif) | 57 (rif) | 58 (ene) | 59 (ene) | 66 (ene) | 64 (ene) | 64 (ene) | 65 (ene) | 64 (ene) |
| chequeo: el sorteo al aparecer sin especiales (tiene que dar lo de arriba) | 6 (pis) | 16 (esc) | 30 (esc) | 45 (rif) | 57 (rif) | 58 (ene) | 59 (ene) | 66 (ene) | 64 (ene) | 64 (ene) | 65 (ene) | 64 (ene) |
| hoy con los cinco especiales | 6 (pis) | 17 (esc) | 29 (esc) | 39 (rif) | 49 (rif) | 58 (rif) | 59 (ene) | 63 (ene) | 62 (ene) | 59 (ene) | 68 (ene) | 62 (ene) |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 6 (pis) | 17 (esc) | 29 (esc) | 39 (rif) | 54 (rif) | 59 (ene) | 59 (ene) | 63 (ene) | 68 (ene) | 59 (ene) | 69 (ene) | 59 (ene) |
| con los especiales sin habilidades (solo sus numeros) | 6 (pis) | 13 (esc) | 29 (esc) | 39 (rif) | 49 (rif) | 57 (rif) | 59 (ene) | 59 (ene) | 59 (ene) | 65 (ene) | 66 (ene) | 69 (ene) |

### El oro por minuto

Las monedas de la partida (las juntadas, el bono de cada oleada y el tesoro) sobre sus minutos (con 3 s por eleccion de carta), sumadas las de todas las semillas.

#### Perfil bajo

| variante | oleada en la p10 | monedas en la p10 | min. de la p10 | oro/min en la p10 | oro/min p8-p12 | oro/min en el muro (ult. 20) | monedas por partida en el muro |
|---|---|---|---|---|---|---|---|
| V3 con el PODER 28 x 1,12^L | 7 | 309 | 2.6 | 134 | 141 | 1930 | 28970 |
| hoy sin especiales, paso a paso desde la 12 | 7 | 309 | 2.6 | 134 | 141 | 1930 | 28970 |
| chequeo: el sorteo al aparecer sin especiales (tiene que dar lo de arriba) | 7 | 309 | 2.6 | 134 | 141 | 1930 | 28970 |
| hoy con los cinco especiales | 7 | 309 | 2.6 | 134 | 159 | 2243 | 33925 |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 7 | 309 | 2.6 | 134 | 157 | 2306 | 43296 |
| con los especiales sin habilidades (solo sus numeros) | 7 | 309 | 2.6 | 134 | 174 | 2458 | 41548 |

#### Perfil medio

| variante | oleada en la p10 | monedas en la p10 | min. de la p10 | oro/min en la p10 | oro/min p8-p12 | oro/min en el muro (ult. 20) | monedas por partida en el muro |
|---|---|---|---|---|---|---|---|
| V3 con el PODER 28 x 1,12^L | 48 | 33572 | 21.4 | 1620 | 1496 | 2962 | 90758 |
| hoy sin especiales, paso a paso desde la 12 | 48 | 33572 | 21.4 | 1620 | 1496 | 2962 | 90758 |
| chequeo: el sorteo al aparecer sin especiales (tiene que dar lo de arriba) | 48 | 33572 | 21.4 | 1620 | 1496 | 2962 | 90758 |
| hoy con los cinco especiales | 47 | 38987 | 20.8 | 1783 | 1842 | 3540 | 110690 |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 49 | 50372 | 23.2 | 2141 | 2062 | 3571 | 110609 |
| con los especiales sin habilidades (solo sus numeros) | 47 | 42768 | 20.4 | 1884 | 1933 | 3774 | 116213 |

#### Perfil alto

| variante | oleada en la p10 | monedas en la p10 | min. de la p10 | oro/min en la p10 | oro/min p8-p12 | oro/min en el muro (ult. 20) | monedas por partida en el muro |
|---|---|---|---|---|---|---|---|
| V3 con el PODER 28 x 1,12^L | 64 | 111487 | 35.2 | 3076 | 3227 | 3919 | 150631 |
| hoy sin especiales, paso a paso desde la 12 | 64 | 111487 | 35.2 | 3076 | 3227 | 3919 | 150631 |
| chequeo: el sorteo al aparecer sin especiales (tiene que dar lo de arriba) | 64 | 111487 | 35.2 | 3076 | 3227 | 3919 | 150631 |
| hoy con los cinco especiales | 59 | 116876 | 30.3 | 3892 | 3829 | 4734 | 190685 |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 59 | 115024 | 30.3 | 3900 | 4029 | 4804 | 194808 |
| con los especiales sin habilidades (solo sus numeros) | 65 | 154182 | 37.5 | 4157 | 4171 | 5080 | 195530 |

### Los minutos y el oro hasta cada oleada

Al terminar la oleada N, en las partidas que la terminan (mediana de todas, de todas las semillas; "—" con menos de 10).

#### Perfil bajo

| variante | min. hasta la 10 | min. hasta la 20 | min. hasta la 30 | min. hasta la 40 | min. hasta la 50 | min. hasta la 55 | min. hasta la 60 | min. hasta la 70 | oro hasta la 10 | oro hasta la 30 | oro hasta la 50 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| V3 con el PODER 28 x 1,12^L | 2.9 | 6.4 | 10.8 | 16.2 | — | — | — | — | 1608 | 14452 | — |
| hoy sin especiales, paso a paso desde la 12 | 2.9 | 6.4 | 10.8 | 16.2 | — | — | — | — | 1608 | 14452 | — |
| chequeo: el sorteo al aparecer sin especiales (tiene que dar lo de arriba) | 2.9 | 6.4 | 10.8 | 16.2 | — | — | — | — | 1608 | 14452 | — |
| hoy con los cinco especiales | 2.9 | 6.5 | 11.0 | 16.6 | — | — | — | — | 1611 | 16222 | — |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 2.9 | 6.5 | 11.0 | 16.5 | — | — | — | — | 1631 | 16226 | — |
| con los especiales sin habilidades (solo sus numeros) | 2.9 | 6.4 | 10.9 | 16.3 | — | — | — | — | 1621 | 16666 | — |

#### Perfil medio

| variante | min. hasta la 10 | min. hasta la 20 | min. hasta la 30 | min. hasta la 40 | min. hasta la 50 | min. hasta la 55 | min. hasta la 60 | min. hasta la 70 | oro hasta la 10 | oro hasta la 30 | oro hasta la 50 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| V3 con el PODER 28 x 1,12^L | 2.7 | 6.1 | 10.3 | 15.3 | 22.2 | 26.3 | 31.8 | — | 1701 | 15126 | 54901 |
| hoy sin especiales, paso a paso desde la 12 | 2.7 | 6.1 | 10.3 | 15.3 | 22.2 | 26.3 | 31.8 | — | 1701 | 15126 | 54901 |
| chequeo: el sorteo al aparecer sin especiales (tiene que dar lo de arriba) | 2.7 | 6.1 | 10.3 | 15.3 | 22.2 | 26.3 | 31.8 | — | 1701 | 15126 | 54901 |
| hoy con los cinco especiales | 2.7 | 6.1 | 10.3 | 15.4 | 22.4 | 26.7 | 32.2 | — | 1697 | 17212 | 65872 |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 2.7 | 6.1 | 10.3 | 15.3 | 22.2 | 26.5 | 32.1 | — | 1701 | 17195 | 65928 |
| con los especiales sin habilidades (solo sus numeros) | 2.7 | 6.1 | 10.3 | 15.3 | 22.2 | 26.4 | 31.8 | — | 1701 | 17439 | 67894 |

#### Perfil alto

| variante | min. hasta la 10 | min. hasta la 20 | min. hasta la 30 | min. hasta la 40 | min. hasta la 50 | min. hasta la 55 | min. hasta la 60 | min. hasta la 70 | oro hasta la 10 | oro hasta la 30 | oro hasta la 50 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| V3 con el PODER 28 x 1,12^L | 2.7 | 6.0 | 9.9 | 14.6 | 20.8 | 24.8 | 29.8 | 40.5 | 1749 | 15349 | 55899 |
| hoy sin especiales, paso a paso desde la 12 | 2.7 | 6.0 | 9.9 | 14.6 | 20.8 | 24.8 | 29.8 | 40.5 | 1749 | 15349 | 55899 |
| chequeo: el sorteo al aparecer sin especiales (tiene que dar lo de arriba) | 2.7 | 6.0 | 9.9 | 14.6 | 20.8 | 24.8 | 29.8 | 40.5 | 1749 | 15349 | 55899 |
| hoy con los cinco especiales | 2.7 | 6.0 | 10.0 | 14.7 | 20.9 | 25.0 | 29.9 | 41.1 | 1741 | 17758 | 68687 |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 2.7 | 6.0 | 10.0 | 14.8 | 20.9 | 25.0 | 29.9 | 41.0 | 1743 | 17764 | 68861 |
| con los especiales sin habilidades (solo sus numeros) | 2.7 | 6.0 | 10.0 | 14.7 | 20.9 | 24.9 | 29.9 | 40.5 | 1740 | 17831 | 69586 |

### Las cartas

#### Cartas elegidas al terminar cada oleada (promedio de todas las partidas que la terminan)

| variante | perfil | 1 | 2 | 3 | 4 | 5 | 6 | 8 | 10 | 15 | 20 | 30 | 40 | 50 | 60 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|

#### Perfil bajo: cuantas veces tiene cada carta al morir en el muro (promedio de las ultimas 20 partidas)

| variante | cadencia | critico | dano | disparo_doble | dron | escudo | explosiva | furia_larga | granada_rapida | hacia_atras | hielo | iman | perforante | quemadura | vampiro | velocidad | vida | doradas |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| V3 con el PODER 28 x 1,12^L | 2.5 | 0.0 | 2.7 | 1.7 | 1.6 | 0.9 | 1.7 | 0.1 | 0.3 | 0.7 | 1.5 | 0.1 | 1.9 | 1.5 | 1.8 | 1.6 | 2.4 | 3.1 |
| hoy sin especiales, paso a paso desde la 12 | 2.5 | 0.0 | 2.7 | 1.7 | 1.6 | 0.9 | 1.7 | 0.1 | 0.3 | 0.7 | 1.5 | 0.1 | 1.9 | 1.5 | 1.8 | 1.6 | 2.4 | 3.1 |
| chequeo: el sorteo al aparecer sin especiales (tiene que dar lo de arriba) | 2.5 | 0.0 | 2.7 | 1.7 | 1.6 | 0.9 | 1.7 | 0.1 | 0.3 | 0.7 | 1.5 | 0.1 | 1.9 | 1.5 | 1.8 | 1.6 | 2.4 | 3.1 |
| hoy con los cinco especiales | 2.7 | 0.0 | 2.8 | 1.9 | 1.8 | 0.9 | 1.8 | 0.2 | 0.3 | 0.9 | 1.6 | 0.1 | 1.9 | 1.5 | 1.7 | 1.7 | 2.6 | 3.2 |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 2.9 | 0.2 | 3.1 | 1.8 | 1.9 | 1.0 | 1.8 | 0.3 | 0.5 | 0.8 | 1.6 | 0.1 | 1.9 | 1.4 | 1.7 | 1.8 | 2.8 | 3.4 |
| con los especiales sin habilidades (solo sus numeros) | 2.4 | 0.1 | 3.0 | 1.9 | 1.8 | 0.9 | 1.8 | 0.2 | 0.4 | 0.9 | 1.6 | 0.1 | 1.9 | 1.5 | 1.9 | 1.8 | 2.6 | 3.4 |

#### Perfil medio: cuantas veces tiene cada carta al morir en el muro (promedio de las ultimas 20 partidas)

| variante | cadencia | critico | dano | disparo_doble | dron | escudo | explosiva | furia_larga | granada_rapida | hacia_atras | hielo | iman | perforante | quemadura | vampiro | velocidad | vida | doradas |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| V3 con el PODER 28 x 1,12^L | 3.5 | 0.3 | 3.5 | 2.0 | 1.9 | 1.0 | 1.9 | 0.4 | 0.8 | 0.9 | 1.9 | 0.4 | 2.0 | 1.8 | 1.9 | 1.9 | 2.9 | 4.6 |
| hoy sin especiales, paso a paso desde la 12 | 3.5 | 0.3 | 3.5 | 2.0 | 1.9 | 1.0 | 1.9 | 0.4 | 0.8 | 0.9 | 1.9 | 0.4 | 2.0 | 1.8 | 1.9 | 1.9 | 2.9 | 4.6 |
| chequeo: el sorteo al aparecer sin especiales (tiene que dar lo de arriba) | 3.5 | 0.3 | 3.5 | 2.0 | 1.9 | 1.0 | 1.9 | 0.4 | 0.8 | 0.9 | 1.9 | 0.4 | 2.0 | 1.8 | 1.9 | 1.9 | 2.9 | 4.6 |
| hoy con los cinco especiales | 3.6 | 0.4 | 3.8 | 2.0 | 1.9 | 1.0 | 2.0 | 0.5 | 1.0 | 1.0 | 1.9 | 0.5 | 2.0 | 1.9 | 2.0 | 2.0 | 2.8 | 4.5 |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 3.8 | 0.5 | 3.9 | 2.0 | 2.0 | 1.0 | 2.0 | 0.9 | 1.2 | 1.0 | 1.9 | 0.5 | 2.0 | 2.0 | 2.0 | 2.0 | 2.9 | 4.8 |
| con los especiales sin habilidades (solo sus numeros) | 3.7 | 0.4 | 3.8 | 2.0 | 2.0 | 1.0 | 2.0 | 0.8 | 1.1 | 0.9 | 1.9 | 0.5 | 2.0 | 1.9 | 2.0 | 1.9 | 3.0 | 4.8 |

#### Perfil alto: cuantas veces tiene cada carta al morir en el muro (promedio de las ultimas 20 partidas)

| variante | cadencia | critico | dano | disparo_doble | dron | escudo | explosiva | furia_larga | granada_rapida | hacia_atras | hielo | iman | perforante | quemadura | vampiro | velocidad | vida | doradas |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| V3 con el PODER 28 x 1,12^L | 3.8 | 0.8 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 0.9 | 1.3 | 1.0 | 2.0 | 0.8 | 2.0 | 1.9 | 2.0 | 2.0 | 3.0 | 5.7 |
| hoy sin especiales, paso a paso desde la 12 | 3.8 | 0.8 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 0.9 | 1.3 | 1.0 | 2.0 | 0.8 | 2.0 | 1.9 | 2.0 | 2.0 | 3.0 | 5.7 |
| chequeo: el sorteo al aparecer sin especiales (tiene que dar lo de arriba) | 3.8 | 0.8 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 0.9 | 1.3 | 1.0 | 2.0 | 0.8 | 2.0 | 1.9 | 2.0 | 2.0 | 3.0 | 5.7 |
| hoy con los cinco especiales | 4.0 | 1.1 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 1.3 | 1.4 | 1.0 | 2.0 | 0.9 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 5.9 |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 4.0 | 1.5 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 1.5 | 1.7 | 1.0 | 2.0 | 1.2 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 6.0 |
| con los especiales sin habilidades (solo sus numeros) | 3.9 | 1.4 | 3.9 | 2.0 | 2.0 | 1.0 | 2.0 | 1.4 | 1.6 | 1.0 | 2.0 | 0.9 | 2.0 | 2.0 | 2.0 | 2.0 | 2.9 | 5.9 |

### En que partida se llega a cada oleada

La partida en que se completa por primera vez la oleada (mediana de las semillas; "—" si no llega la mitad).

#### Perfil bajo

| variante | llega a 10 | llega a 20 | llega a 30 | llega a 40 | llega a 50 | llega a 60 |
|---|---|---|---|---|---|---|
| V3 con el PODER 28 x 1,12^L | 11 | 19 | 27 | 43 | — | — |
| hoy sin especiales, paso a paso desde la 12 | 11 | 19 | 27 | 43 | — | — |
| chequeo: el sorteo al aparecer sin especiales (tiene que dar lo de arriba) | 11 | 19 | 27 | 43 | — | — |
| hoy con los cinco especiales | 11 | 19 | 25 | 40 | — | — |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 11 | 18 | 25 | 44 | — | — |
| con los especiales sin habilidades (solo sus numeros) | 11 | 17 | 25 | 33 | — | — |

#### Perfil medio

| variante | llega a 10 | llega a 20 | llega a 30 | llega a 40 | llega a 50 | llega a 60 |
|---|---|---|---|---|---|---|
| V3 con el PODER 28 x 1,12^L | 4 | 5 | 7 | 10 | 13 | 33 |
| hoy sin especiales, paso a paso desde la 12 | 4 | 5 | 7 | 10 | 13 | 33 |
| chequeo: el sorteo al aparecer sin especiales (tiene que dar lo de arriba) | 4 | 5 | 7 | 10 | 13 | 33 |
| hoy con los cinco especiales | 4 | 6 | 8 | 10 | 12 | 24 |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 4 | 5 | 6 | 9 | 11 | 19 |
| con los especiales sin habilidades (solo sus numeros) | 4 | 5 | 8 | 9 | 12 | 24 |

#### Perfil alto

| variante | llega a 10 | llega a 20 | llega a 30 | llega a 40 | llega a 50 | llega a 60 |
|---|---|---|---|---|---|---|
| V3 con el PODER 28 x 1,12^L | 2 | 3 | 3 | 4 | 5 | 7 |
| hoy sin especiales, paso a paso desde la 12 | 2 | 3 | 3 | 4 | 5 | 7 |
| chequeo: el sorteo al aparecer sin especiales (tiene que dar lo de arriba) | 2 | 3 | 3 | 4 | 5 | 7 |
| hoy con los cinco especiales | 2 | 3 | 4 | 5 | 6 | 8 |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 2 | 3 | 4 | 5 | 5 | 7 |
| con los especiales sin habilidades (solo sus numeros) | 2 | 3 | 4 | 5 | 6 | 8 |

### El muro de cada semilla

La mediana de las ultimas 20 partidas de cada semilla, su media, y la mediana de todas juntas (la del muro de arriba).

#### Perfil bajo

| variante | semilla 1000 | semilla 1001 | semilla 1002 | semilla 1003 | semilla 1004 | media | todas juntas |
|---|---|---|---|---|---|---|---|
| V3 con el PODER 28 x 1,12^L | 39 | 39 | 42 | 39 | 41 | 40.0 | 39 |
| hoy sin especiales, paso a paso desde la 12 | 39 | 39 | 42 | 39 | 41 | 40.0 | 39 |
| chequeo: el sorteo al aparecer sin especiales (tiene que dar lo de arriba) | 39 | 39 | 42 | 39 | 41 | 40.0 | 39 |
| hoy con los cinco especiales | 39 | 39 | 42 | 39 | 42.5 | 40.3 | 39 |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 40 | 39 | 46 | 42.5 | 46 | 42.7 | 43 |
| con los especiales sin habilidades (solo sus numeros) | 46 | 41 | 40.5 | 41.5 | 43.5 | 42.5 | 42 |

#### Perfil medio

| variante | semilla 1000 | semilla 1001 | semilla 1002 | semilla 1003 | semilla 1004 | media | todas juntas |
|---|---|---|---|---|---|---|---|
| V3 con el PODER 28 x 1,12^L | 59 | 59 | 59 | 59 | 59 | 59.0 | 59 |
| hoy sin especiales, paso a paso desde la 12 | 59 | 59 | 59 | 59 | 59 | 59.0 | 59 |
| chequeo: el sorteo al aparecer sin especiales (tiene que dar lo de arriba) | 59 | 59 | 59 | 59 | 59 | 59.0 | 59 |
| hoy con los cinco especiales | 59 | 59 | 59 | 59 | 59 | 59.0 | 59 |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 59 | 59 | 59 | 59 | 59 | 59.0 | 59 |
| con los especiales sin habilidades (solo sus numeros) | 59 | 59.5 | 59 | 59 | 59 | 59.1 | 59 |

#### Perfil alto

| variante | semilla 1000 | semilla 1001 | semilla 1002 | semilla 1003 | semilla 1004 | media | todas juntas |
|---|---|---|---|---|---|---|---|
| V3 con el PODER 28 x 1,12^L | 69 | 69 | 69 | 69 | 69 | 69.0 | 69 |
| hoy sin especiales, paso a paso desde la 12 | 69 | 69 | 69 | 69 | 69 | 69.0 | 69 |
| chequeo: el sorteo al aparecer sin especiales (tiene que dar lo de arriba) | 69 | 69 | 69 | 69 | 69 | 69.0 | 69 |
| hoy con los cinco especiales | 69 | 69 | 69.5 | 69 | 69.5 | 69.2 | 69 |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 69 | 70 | 72 | 71.5 | 69 | 70.3 | 70 |
| con los especiales sin habilidades (solo sus numeros) | 69 | 71.5 | 69 | 69 | 73 | 70.3 | 69 |

### Los zombis con habilidad

En las ultimas 20 partidas de cada semilla: de donde sale el danio que recibe el jugador (el reparto), cuantos especiales se matan, que parte de las muertes es la explosion del hinchado, los pulsos del chaman, los saltos y los escupitajos, y que parte de las balas comunes que van a un blindado para el escudo.

#### Perfil bajo

| variante | danio recibido: zarpazos / jefe / charco / salto / explosion | especiales muertos por partida | muertes por la explosion | pulsos del chaman por partida (potenciados por pulso) | saltos por partida (pegan) | escupitajos por partida (golpes de charco por escupitajo) | bala a blindados parada por el escudo |
|---|---|---|---|---|---|---|---|
| hoy sin especiales, paso a paso desde la 12 | 80 % / 20 % / 0 % / 0 % / 0 % | 0.0 | 0.0 % | 0.0 (0.0) | 0.0 (0 %) | 0.0 (0.00) | — |
| chequeo: el sorteo al aparecer sin especiales (tiene que dar lo de arriba) | 80 % / 20 % / 0 % / 0 % / 0 % | 0.0 | 0.0 % | 0.0 (0.0) | 0.0 (0 %) | 0.0 (0.00) | — |
| hoy con los cinco especiales | 44 % / 19 % / 19 % / 7 % / 12 % | 303.6 | 0.7 % | 49.7 (1.3) | 49.8 (41 %) | 169.6 (0.63) | 3 % |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 45 % / 17 % / 20 % / 7 % / 11 % | 325.1 | 0.8 % | 58.5 (1.3) | 56.6 (39 %) | 188.5 (0.64) | 3 % |
| con los especiales sin habilidades (solo sus numeros) | 78 % / 22 % / 0 % / 0 % / 0 % | 365.3 | 0.0 % | 0.0 (0.0) | 0.0 (0 %) | 0.0 (0.00) | — |

#### Perfil medio

| variante | danio recibido: zarpazos / jefe / charco / salto / explosion | especiales muertos por partida | muertes por la explosion | pulsos del chaman por partida (potenciados por pulso) | saltos por partida (pegan) | escupitajos por partida (golpes de charco por escupitajo) | bala a blindados parada por el escudo |
|---|---|---|---|---|---|---|---|
| hoy sin especiales, paso a paso desde la 12 | 71 % / 29 % / 0 % / 0 % / 0 % | 0.0 | 0.0 % | 0.0 (0.0) | 0.0 (0 %) | 0.0 (0.00) | — |
| chequeo: el sorteo al aparecer sin especiales (tiene que dar lo de arriba) | 71 % / 29 % / 0 % / 0 % / 0 % | 0.0 | 0.0 % | 0.0 (0.0) | 0.0 (0 %) | 0.0 (0.00) | — |
| hoy con los cinco especiales | 40 % / 24 % / 18 % / 8 % / 9 % | 663.6 | 0.7 % | 132.7 (1.3) | 100.2 (21 %) | 229.3 (0.24) | 1 % |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 38 % / 24 % / 20 % / 9 % / 9 % | 664.0 | 0.6 % | 126.7 (1.3) | 97.8 (20 %) | 217.4 (0.25) | 1 % |
| con los especiales sin habilidades (solo sus numeros) | 74 % / 26 % / 0 % / 0 % / 0 % | 743.8 | 0.0 % | 0.0 (0.0) | 0.0 (0 %) | 0.0 (0.00) | — |

#### Perfil alto

| variante | danio recibido: zarpazos / jefe / charco / salto / explosion | especiales muertos por partida | muertes por la explosion | pulsos del chaman por partida (potenciados por pulso) | saltos por partida (pegan) | escupitajos por partida (golpes de charco por escupitajo) | bala a blindados parada por el escudo |
|---|---|---|---|---|---|---|---|
| hoy sin especiales, paso a paso desde la 12 | 83 % / 17 % / 0 % / 0 % / 0 % | 0.0 | 0.0 % | 0.0 (0.0) | 0.0 (0 %) | 0.0 (0.00) | — |
| chequeo: el sorteo al aparecer sin especiales (tiene que dar lo de arriba) | 83 % / 17 % / 0 % / 0 % / 0 % | 0.0 | 0.0 % | 0.0 (0.0) | 0.0 (0 %) | 0.0 (0.00) | — |
| hoy con los cinco especiales | 61 % / 15 % / 14 % / 5 % / 6 % | 954.0 | 0.5 % | 186.5 (1.4) | 115.6 (8 %) | 260.2 (0.09) | 1 % |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 62 % / 13 % / 15 % / 5 % / 5 % | 973.0 | 0.5 % | 192.0 (1.4) | 116.2 (8 %) | 260.5 (0.09) | 1 % |
| con los especiales sin habilidades (solo sus numeros) | 79 % / 21 % / 0 % / 0 % / 0 % | 1052.6 | 0.0 % | 0.0 (0.0) | 0.0 (0 %) | 0.0 (0.00) | — |

## Segunda corrida: de a uno, y lo puesto a ojo (100 partidas x 5 semillas)

partidas 100, semillas 5, perfiles bajo,medio,alto, 1170 s

mejor pN: la mejor oleada completada hasta la partida N (mediana de las semillas). muro: la oleada mediana de las ultimas 20 partidas de cada semilla. llega a N: la partida en que se completa por primera vez la oleada N. min. en el muro: lo que dura una de esas partidas, con 3 s por eleccion de carta. % muertes en jefe: de las ultimas 100 partidas de cada semilla.

### El muro

#### Perfil bajo

| variante | mejor p10 | mejor p30 | muro (ult. 20) | mejor al final | llega a 40 | a 50 | a 60 | a 80 | al tope (100) | min. en el muro | cartas por partida | % muertes en jefe | arma en el muro | sin balas con blanco | revivio |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| solo el escupidor | 8 | 35 | 39 | 49 | 37 | — | — | — | 0 % | 16.2 | 21 | 39 % | energia | 0 % | — |
| solo el saltador | 8 | 32 | 39 | 49 | 40 | — | — | — | 0 % | 15.8 | 21 | 40 % | energia | 0 % | — |
| solo el chaman | 8 | 37 | 39 | 49 | 42 | — | — | — | 0 % | 16.2 | 21 | 38 % | energia | 0 % | — |
| solo el blindado | 8 | 36 | 40 | 50 | 41 | 89 | — | — | 0 % | 16.5 | 21 | 35 % | energia | 0 % | — |
| solo el hinchado | 8 | 36 | 39 | 49 | 42 | — | — | — | 0 % | 16.5 | 21 | 43 % | energia | 0 % | — |
| con los especiales, esquivando el 90 % de sus avisos | 8 | 39 | 46 | 52 | 31 | 69 | — | — | 0 % | 20.6 | 24 | 43 % | energia | 0 % | — |
| con los especiales, esquivando la mitad que al jefe | 8 | 29 | 39 | 45 | 53 | — | — | — | 0 % | 15.7 | 21 | 34 % | energia | 0 % | — |
| con los especiales, los zombis en cuatro chorros | 8 | 38 | 39 | 49 | 39 | — | — | — | 0 % | 16.5 | 22 | 42 % | energia | 0 % | — |
| con los especiales y +1 de PODER, esquivando la mitad | 8 | 31 | 39 | 45 | 61 | — | — | — | 0 % | 15.8 | 22 | 35 % | energia | 0 % | — |

#### Perfil medio

| variante | mejor p10 | mejor p30 | muro (ult. 20) | mejor al final | llega a 40 | a 50 | a 60 | a 80 | al tope (100) | min. en el muro | cartas por partida | % muertes en jefe | arma en el muro | sin balas con blanco | revivio |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| solo el escupidor | 49 | 61 | 59 | 65 | 10 | 11 | 30 | — | 0 % | 30.6 | 27 | 61 % | energia | 0 % | — |
| solo el saltador | 49 | 59 | 59 | 64 | 10 | 13 | 34 | — | 0 % | 29.9 | 27 | 62 % | energia | 0 % | — |
| solo el chaman | 43 | 59 | 59 | 65 | 9 | 12 | 33 | — | 0 % | 30.3 | 27 | 57 % | energia | 0 % | — |
| solo el blindado | 48 | 60 | 59 | 64 | 8 | 13 | 26 | — | 0 % | 30.4 | 27 | 56 % | energia | 0 % | — |
| solo el hinchado | 49 | 60 | 59 | 65 | 9 | 13 | 30 | — | 0 % | 30.7 | 27 | 64 % | energia | 0 % | — |
| con los especiales, esquivando el 90 % de sus avisos | 49 | 60 | 59 | 65 | 8 | 11 | 29 | — | 0 % | 31.0 | 28 | 63 % | energia | 0 % | — |
| con los especiales, esquivando la mitad que al jefe | 39 | 55 | 54 | 59 | 12 | 15 | — | — | 0 % | 26.7 | 26 | 44 % | energia | 0 % | — |
| con los especiales, los zombis en cuatro chorros | 49 | 61 | 59 | 64 | 10 | 11 | 29 | — | 0 % | 30.4 | 28 | 60 % | energia | 0 % | — |
| con los especiales y +1 de PODER, esquivando la mitad | 39 | 58 | 56 | 60 | 11 | 15 | 59 | — | 0 % | 28.3 | 28 | 46 % | energia | 0 % | — |

#### Perfil alto

| variante | mejor p10 | mejor p30 | muro (ult. 20) | mejor al final | llega a 40 | a 50 | a 60 | a 80 | al tope (100) | min. en el muro | cartas por partida | % muertes en jefe | arma en el muro | sin balas con blanco | revivio |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| solo el escupidor | 68 | 72 | 69 | 75 | 5 | 6 | 7 | — | 0 % | 39.6 | 29 | 39 % | energia | 0 % | — |
| solo el saltador | 68 | 73 | 69 | 76 | 5 | 6 | 7 | — | 0 % | 38.6 | 29 | 41 % | energia | 0 % | — |
| solo el chaman | 68 | 72 | 69 | 75 | 5 | 5 | 7 | — | 0 % | 39.5 | 29 | 38 % | energia | 0 % | — |
| solo el blindado | 70 | 73 | 69 | 76 | 5 | 5 | 8 | — | 0 % | 39.2 | 29 | 37 % | energia | 0 % | — |
| solo el hinchado | 69 | 74 | 70 | 77 | 5 | 6 | 7 | — | 0 % | 41.3 | 30 | 43 % | energia | 0 % | — |
| con los especiales, esquivando el 90 % de sus avisos | 67 | 72 | 69 | 76 | 4 | 5 | 8 | — | 0 % | 39.8 | 30 | 48 % | energia | 0 % | — |
| con los especiales, esquivando la mitad que al jefe | 59 | 67 | 63 | 69 | 5 | 5 | 11 | — | 0 % | 33.8 | 29 | 34 % | energia | 0 % | — |
| con los especiales, los zombis en cuatro chorros | 67 | 72 | 69 | 75 | 4 | 5 | 7 | — | 0 % | 39.8 | 30 | 44 % | energia | 0 % | — |
| con los especiales y +1 de PODER, esquivando la mitad | 59 | 66 | 65 | 69 | 5 | 5 | 11 | — | 0 % | 34.8 | 30 | 32 % | energia | 0 % | — |

### El arranque

#### Perfil bajo: las primeras 12 partidas (oleada a la que se llega, mediana; entre parentesis el arma con que se arranca)

| variante | p1 | p2 | p3 | p4 | p5 | p6 | p7 | p8 | p9 | p10 | p11 | p12 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| solo el escupidor | 1 (pis) | 2 (pis) | 2 (rev) | 2 (rev) | 3 (rev) | 3 (rev) | 5 (esc) | 6 (esc) | 7 (esc) | 7 (esc) | 10 (esc) | 9 (esc) |
| solo el saltador | 1 (pis) | 2 (pis) | 2 (rev) | 2 (rev) | 3 (rev) | 3 (rev) | 5 (esc) | 6 (esc) | 7 (esc) | 7 (esc) | 9 (esc) | 9 (esc) |
| solo el chaman | 1 (pis) | 2 (pis) | 2 (rev) | 2 (rev) | 3 (rev) | 3 (rev) | 5 (esc) | 6 (esc) | 7 (esc) | 7 (esc) | 9 (esc) | 9 (esc) |
| solo el blindado | 1 (pis) | 2 (pis) | 2 (rev) | 2 (rev) | 3 (rev) | 3 (rev) | 5 (esc) | 6 (esc) | 7 (esc) | 7 (esc) | 9 (esc) | 9 (esc) |
| solo el hinchado | 1 (pis) | 2 (pis) | 2 (rev) | 2 (rev) | 3 (rev) | 3 (rev) | 5 (esc) | 6 (esc) | 7 (esc) | 7 (esc) | 9 (esc) | 9 (esc) |
| con los especiales, esquivando el 90 % de sus avisos | 1 (pis) | 2 (pis) | 2 (rev) | 2 (rev) | 3 (rev) | 3 (rev) | 5 (esc) | 6 (esc) | 7 (esc) | 7 (esc) | 10 (esc) | 9 (esc) |
| con los especiales, esquivando la mitad que al jefe | 1 (pis) | 2 (pis) | 2 (rev) | 2 (rev) | 3 (rev) | 3 (rev) | 5 (esc) | 6 (esc) | 7 (esc) | 7 (esc) | 10 (esc) | 9 (esc) |
| con los especiales, los zombis en cuatro chorros | 1 (pis) | 2 (pis) | 2 (rev) | 2 (rev) | 3 (rev) | 3 (rev) | 5 (esc) | 6 (esc) | 7 (esc) | 7 (esc) | 10 (esc) | 9 (esc) |
| con los especiales y +1 de PODER, esquivando la mitad | 1 (pis) | 2 (pis) | 2 (rev) | 2 (rev) | 3 (rev) | 3 (rev) | 5 (esc) | 6 (esc) | 7 (esc) | 7 (esc) | 10 (esc) | 9 (esc) |

#### Perfil medio: las primeras 12 partidas (oleada a la que se llega, mediana; entre parentesis el arma con que se arranca)

| variante | p1 | p2 | p3 | p4 | p5 | p6 | p7 | p8 | p9 | p10 | p11 | p12 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| solo el escupidor | 5 (pis) | 6 (rev) | 9 (esc) | 15 (esc) | 25 (esc) | 27 (rif) | 29 (rif) | 39 (rif) | 39 (rif) | 49 (rif) | 49 (ene) | 53 (ene) |
| solo el saltador | 5 (pis) | 6 (rev) | 9 (esc) | 15 (esc) | 19 (esc) | 27 (rif) | 29 (rif) | 38 (rif) | 38 (rif) | 47 (rif) | 39 (ene) | 49 (ene) |
| solo el chaman | 5 (pis) | 6 (rev) | 9 (esc) | 15 (esc) | 22 (esc) | 24 (rif) | 29 (rif) | 29 (rif) | 41 (rif) | 39 (rif) | 49 (rif) | 49 (ene) |
| solo el blindado | 5 (pis) | 6 (rev) | 9 (esc) | 15 (esc) | 22 (esc) | 32 (rif) | 29 (rif) | 41 (rif) | 39 (rif) | 48 (rif) | 49 (ene) | 45 (ene) |
| solo el hinchado | 5 (pis) | 6 (rev) | 9 (esc) | 15 (esc) | 26 (esc) | 29 (rif) | 29 (rif) | 34 (rif) | 46 (rif) | 49 (rif) | 49 (ene) | 49 (ene) |
| con los especiales, esquivando el 90 % de sus avisos | 5 (pis) | 6 (rev) | 9 (esc) | 16 (esc) | 21 (esc) | 31 (rif) | 30 (rif) | 43 (rif) | 48 (rif) | 39 (ene) | 49 (ene) | 49 (ene) |
| con los especiales, esquivando la mitad que al jefe | 5 (pis) | 6 (rev) | 9 (esc) | 16 (esc) | 19 (esc) | 21 (rif) | 29 (rif) | 33 (rif) | 34 (rif) | 31 (rif) | 39 (rif) | 45 (ene) |
| con los especiales, los zombis en cuatro chorros | 5 (pis) | 6 (rev) | 9 (esc) | 16 (esc) | 20 (esc) | 29 (rif) | 29 (rif) | 39 (rif) | 39 (rif) | 49 (rif) | 49 (ene) | 49 (ene) |
| con los especiales y +1 de PODER, esquivando la mitad | 5 (pis) | 6 (rev) | 9 (esc) | 16 (esc) | 19 (esc) | 26 (rif) | 29 (rif) | 39 (rif) | 39 (rif) | 39 (rif) | 43 (rif) | 48 (ene) |

#### Perfil alto: las primeras 12 partidas (oleada a la que se llega, mediana; entre parentesis el arma con que se arranca)

| variante | p1 | p2 | p3 | p4 | p5 | p6 | p7 | p8 | p9 | p10 | p11 | p12 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| solo el escupidor | 6 (pis) | 14 (esc) | 29 (esc) | 39 (rif) | 49 (rif) | 59 (rif) | 60 (ene) | 65 (ene) | 65 (ene) | 68 (ene) | 63 (ene) | 66 (ene) |
| solo el saltador | 6 (pis) | 14 (esc) | 29 (esc) | 39 (rif) | 49 (rif) | 59 (rif) | 61 (ene) | 65 (ene) | 59 (ene) | 64 (ene) | 59 (ene) | 66 (ene) |
| solo el chaman | 6 (pis) | 16 (esc) | 28 (esc) | 34 (rif) | 53 (rif) | 57 (rif) | 60 (ene) | 59 (ene) | 60 (ene) | 68 (ene) | 64 (ene) | 65 (ene) |
| solo el blindado | 6 (pis) | 16 (esc) | 30 (esc) | 39 (rif) | 53 (rif) | 49 (rif) | 59 (ene) | 67 (ene) | 65 (ene) | 69 (ene) | 64 (ene) | 67 (ene) |
| solo el hinchado | 6 (pis) | 16 (esc) | 31 (esc) | 39 (rif) | 48 (rif) | 59 (rif) | 65 (ene) | 65 (ene) | 59 (ene) | 68 (ene) | 68 (ene) | 63 (ene) |
| con los especiales, esquivando el 90 % de sus avisos | 6 (pis) | 16 (esc) | 32 (esc) | 43 (rif) | 55 (rif) | 59 (ene) | 59 (ene) | 64 (ene) | 59 (ene) | 64 (ene) | 67 (ene) | 65 (ene) |
| con los especiales, esquivando la mitad que al jefe | 6 (pis) | 14 (esc) | 26 (esc) | 38 (rif) | 50 (rif) | 49 (rif) | 58 (ene) | 57 (ene) | 59 (ene) | 59 (ene) | 60 (ene) | 57 (ene) |
| con los especiales, los zombis en cuatro chorros | 6 (pis) | 17 (esc) | 34 (esc) | 48 (rif) | 50 (rif) | 59 (ene) | 65 (ene) | 64 (ene) | 66 (ene) | 65 (ene) | 59 (ene) | 65 (ene) |
| con los especiales y +1 de PODER, esquivando la mitad | 6 (pis) | 14 (esc) | 29 (esc) | 37 (rif) | 51 (rif) | 52 (rif) | 57 (ene) | 58 (ene) | 58 (ene) | 58 (ene) | 62 (ene) | 59 (ene) |

### El oro por minuto

Las monedas de la partida (las juntadas, el bono de cada oleada y el tesoro) sobre sus minutos (con 3 s por eleccion de carta), sumadas las de todas las semillas.

#### Perfil bajo

| variante | oleada en la p10 | monedas en la p10 | min. de la p10 | oro/min en la p10 | oro/min p8-p12 | oro/min en el muro (ult. 20) | monedas por partida en el muro |
|---|---|---|---|---|---|---|---|
| solo el escupidor | 7 | 309 | 2.6 | 134 | 162 | 1913 | 29532 |
| solo el saltador | 7 | 309 | 2.6 | 134 | 141 | 1997 | 29855 |
| solo el chaman | 7 | 309 | 2.6 | 134 | 141 | 1998 | 29787 |
| solo el blindado | 7 | 309 | 2.6 | 134 | 141 | 2037 | 31813 |
| solo el hinchado | 7 | 309 | 2.6 | 134 | 141 | 2047 | 30424 |
| con los especiales, esquivando el 90 % de sus avisos | 7 | 309 | 2.6 | 134 | 171 | 2386 | 50028 |
| con los especiales, esquivando la mitad que al jefe | 7 | 309 | 2.6 | 134 | 158 | 1977 | 31980 |
| con los especiales, los zombis en cuatro chorros | 7 | 309 | 2.6 | 134 | 159 | 2215 | 33536 |
| con los especiales y +1 de PODER, esquivando la mitad | 7 | 309 | 2.6 | 134 | 157 | 2004 | 32357 |

#### Perfil medio

| variante | oleada en la p10 | monedas en la p10 | min. de la p10 | oro/min en la p10 | oro/min p8-p12 | oro/min en el muro (ult. 20) | monedas por partida en el muro |
|---|---|---|---|---|---|---|---|
| solo el escupidor | 49 | 40106 | 22.7 | 1635 | 1600 | 3025 | 92967 |
| solo el saltador | 47 | 37163 | 20.8 | 1650 | 1600 | 3121 | 95155 |
| solo el chaman | 39 | 21358 | 15.6 | 1415 | 1535 | 3058 | 94483 |
| solo el blindado | 48 | 40196 | 21.2 | 1704 | 1620 | 3132 | 96532 |
| solo el hinchado | 49 | 36332 | 22.9 | 1753 | 1733 | 3164 | 97697 |
| con los especiales, esquivando el 90 % de sus avisos | 39 | 27143 | 16.2 | 1848 | 1901 | 3567 | 110547 |
| con los especiales, esquivando la mitad que al jefe | 31 | 13053 | 12.3 | 1355 | 1492 | 3220 | 84139 |
| con los especiales, los zombis en cuatro chorros | 49 | 44604 | 23.6 | 1918 | 1929 | 3472 | 108298 |
| con los especiales y +1 de PODER, esquivando la mitad | 39 | 25192 | 16.0 | 1557 | 1585 | 3333 | 94465 |

#### Perfil alto

| variante | oleada en la p10 | monedas en la p10 | min. de la p10 | oro/min en la p10 | oro/min p8-p12 | oro/min en el muro (ult. 20) | monedas por partida en el muro |
|---|---|---|---|---|---|---|---|
| solo el escupidor | 68 | 143901 | 38.4 | 3587 | 3364 | 4060 | 160616 |
| solo el saltador | 64 | 122880 | 35.7 | 3488 | 3444 | 4175 | 161235 |
| solo el chaman | 68 | 146906 | 39.2 | 3539 | 3310 | 4111 | 162479 |
| solo el blindado | 69 | 154207 | 39.8 | 3730 | 3582 | 4236 | 163439 |
| solo el hinchado | 68 | 147107 | 39.7 | 3571 | 3484 | 4329 | 170101 |
| con los especiales, esquivando el 90 % de sus avisos | 64 | 140549 | 34.9 | 4039 | 3978 | 4745 | 188472 |
| con los especiales, esquivando la mitad que al jefe | 59 | 101839 | 29.8 | 3407 | 3373 | 4194 | 142127 |
| con los especiales, los zombis en cuatro chorros | 65 | 152936 | 37.3 | 4012 | 3948 | 4732 | 189276 |
| con los especiales y +1 de PODER, esquivando la mitad | 58 | 98768 | 28.8 | 3337 | 3437 | 4291 | 151608 |

### Los minutos y el oro hasta cada oleada

Al terminar la oleada N, en las partidas que la terminan (mediana de todas, de todas las semillas; "—" con menos de 10).

#### Perfil bajo

| variante | min. hasta la 10 | min. hasta la 20 | min. hasta la 30 | min. hasta la 40 | min. hasta la 50 | min. hasta la 55 | min. hasta la 60 | min. hasta la 70 | oro hasta la 10 | oro hasta la 30 | oro hasta la 50 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| solo el escupidor | 2.9 | 6.5 | 10.9 | 16.6 | — | — | — | — | 1620 | 14992 | — |
| solo el saltador | 2.9 | 6.4 | 10.8 | 16.2 | — | — | — | — | 1608 | 14581 | — |
| solo el chaman | 2.9 | 6.5 | 10.8 | 16.3 | — | — | — | — | 1614 | 14574 | — |
| solo el blindado | 2.9 | 6.5 | 10.9 | 16.4 | — | — | — | — | 1616 | 15017 | — |
| solo el hinchado | 2.8 | 6.4 | 10.8 | 16.3 | — | — | — | — | 1618 | 14844 | — |
| con los especiales, esquivando el 90 % de sus avisos | 2.9 | 6.5 | 10.9 | 16.7 | 25.0 | — | — | — | 1632 | 16473 | 63727 |
| con los especiales, esquivando la mitad que al jefe | 2.9 | 6.5 | 11.0 | 16.6 | — | — | — | — | 1617 | 16152 | — |
| con los especiales, los zombis en cuatro chorros | 2.9 | 6.5 | 11.0 | 16.5 | — | — | — | — | 1624 | 16235 | — |
| con los especiales y +1 de PODER, esquivando la mitad | 2.9 | 6.5 | 11.0 | 16.5 | — | — | — | — | 1606 | 16133 | — |

#### Perfil medio

| variante | min. hasta la 10 | min. hasta la 20 | min. hasta la 30 | min. hasta la 40 | min. hasta la 50 | min. hasta la 55 | min. hasta la 60 | min. hasta la 70 | oro hasta la 10 | oro hasta la 30 | oro hasta la 50 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| solo el escupidor | 2.7 | 6.1 | 10.2 | 15.3 | 22.2 | 26.4 | 31.8 | — | 1704 | 15907 | 56640 |
| solo el saltador | 2.7 | 6.1 | 10.2 | 15.2 | 22.0 | 26.1 | 31.5 | — | 1698 | 15332 | 57533 |
| solo el chaman | 2.7 | 6.1 | 10.2 | 15.2 | 22.1 | 26.3 | 31.8 | — | 1698 | 15248 | 57582 |
| solo el blindado | 2.7 | 6.1 | 10.3 | 15.3 | 22.3 | 26.4 | 31.9 | — | 1697 | 15743 | 58434 |
| solo el hinchado | 2.7 | 6.1 | 10.3 | 15.3 | 22.2 | 26.4 | 31.9 | — | 1701 | 15597 | 58037 |
| con los especiales, esquivando el 90 % de sus avisos | 2.7 | 6.1 | 10.3 | 15.3 | 22.3 | 26.5 | 32.0 | — | 1703 | 17302 | 66272 |
| con los especiales, esquivando la mitad que al jefe | 2.7 | 6.1 | 10.3 | 15.4 | 22.4 | 26.6 | — | — | 1690 | 16947 | 65293 |
| con los especiales, los zombis en cuatro chorros | 2.7 | 6.1 | 10.3 | 15.4 | 22.4 | 26.6 | 32.2 | — | 1695 | 17192 | 65802 |
| con los especiales y +1 de PODER, esquivando la mitad | 2.7 | 6.1 | 10.3 | 15.3 | 22.2 | 26.4 | — | — | 1690 | 16992 | 65315 |

#### Perfil alto

| variante | min. hasta la 10 | min. hasta la 20 | min. hasta la 30 | min. hasta la 40 | min. hasta la 50 | min. hasta la 55 | min. hasta la 60 | min. hasta la 70 | oro hasta la 10 | oro hasta la 30 | oro hasta la 50 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| solo el escupidor | 2.7 | 5.9 | 9.9 | 14.6 | 20.8 | 24.8 | 29.8 | 40.7 | 1744 | 16452 | 58983 |
| solo el saltador | 2.7 | 6.0 | 9.9 | 14.6 | 20.8 | 24.8 | 29.8 | 40.2 | 1742 | 15546 | 58934 |
| solo el chaman | 2.7 | 6.0 | 9.9 | 14.6 | 20.9 | 24.9 | 29.9 | 40.8 | 1741 | 15495 | 59002 |
| solo el blindado | 2.7 | 6.0 | 9.9 | 14.7 | 21.0 | 25.0 | 30.0 | 40.6 | 1738 | 16040 | 59559 |
| solo el hinchado | 2.7 | 6.0 | 9.9 | 14.7 | 20.9 | 25.0 | 29.9 | 40.5 | 1751 | 15850 | 59282 |
| con los especiales, esquivando el 90 % de sus avisos | 2.7 | 6.0 | 10.0 | 14.7 | 21.0 | 25.1 | 30.0 | 41.2 | 1750 | 17773 | 68638 |
| con los especiales, esquivando la mitad que al jefe | 2.7 | 6.0 | 10.0 | 14.7 | 21.0 | 25.2 | 30.1 | — | 1744 | 17697 | 67894 |
| con los especiales, los zombis en cuatro chorros | 2.7 | 6.0 | 10.0 | 14.7 | 21.0 | 25.1 | 29.9 | 41.2 | 1739 | 17802 | 68806 |
| con los especiales y +1 de PODER, esquivando la mitad | 2.7 | 6.0 | 10.0 | 14.8 | 21.0 | 25.1 | 29.9 | — | 1748 | 17736 | 68011 |

### Las cartas

#### Cartas elegidas al terminar cada oleada (promedio de todas las partidas que la terminan)

| variante | perfil | 1 | 2 | 3 | 4 | 5 | 6 | 8 | 10 | 15 | 20 | 30 | 40 | 50 | 60 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|

#### Perfil bajo: cuantas veces tiene cada carta al morir en el muro (promedio de las ultimas 20 partidas)

| variante | cadencia | critico | dano | disparo_doble | dron | escudo | explosiva | furia_larga | granada_rapida | hacia_atras | hielo | iman | perforante | quemadura | vampiro | velocidad | vida | doradas |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| solo el escupidor | 2.2 | 0.1 | 2.8 | 1.7 | 1.7 | 0.9 | 1.6 | 0.1 | 0.3 | 0.8 | 1.5 | 0.1 | 1.9 | 1.4 | 1.7 | 1.7 | 2.5 | 3.1 |
| solo el saltador | 2.4 | 0.1 | 2.6 | 1.8 | 1.7 | 0.9 | 1.6 | 0.1 | 0.3 | 0.8 | 1.6 | 0.1 | 1.9 | 1.5 | 1.8 | 1.7 | 2.5 | 3.1 |
| solo el chaman | 2.6 | 0.1 | 2.6 | 1.8 | 1.7 | 0.9 | 1.8 | 0.1 | 0.2 | 0.8 | 1.5 | 0.0 | 1.8 | 1.5 | 1.7 | 1.7 | 2.6 | 3.1 |
| solo el blindado | 2.3 | 0.1 | 2.5 | 1.7 | 1.7 | 0.9 | 1.7 | 0.2 | 0.4 | 0.9 | 1.6 | 0.1 | 1.9 | 1.6 | 1.7 | 1.8 | 2.5 | 3.2 |
| solo el hinchado | 2.5 | 0.1 | 2.9 | 1.7 | 1.7 | 0.9 | 1.7 | 0.1 | 0.3 | 0.8 | 1.6 | 0.1 | 1.9 | 1.5 | 1.6 | 1.7 | 2.4 | 3.2 |
| con los especiales, esquivando el 90 % de sus avisos | 2.8 | 0.1 | 3.2 | 1.8 | 1.8 | 0.9 | 1.8 | 0.2 | 0.5 | 0.9 | 1.7 | 0.1 | 1.9 | 1.6 | 1.7 | 1.7 | 2.7 | 3.5 |
| con los especiales, esquivando la mitad que al jefe | 2.2 | 0.0 | 2.4 | 1.8 | 1.7 | 0.9 | 1.7 | 0.1 | 0.3 | 0.7 | 1.6 | 0.0 | 1.8 | 1.3 | 1.6 | 1.7 | 2.5 | 2.8 |
| con los especiales, los zombis en cuatro chorros | 2.6 | 0.1 | 2.7 | 1.8 | 1.7 | 0.9 | 1.8 | 0.2 | 0.2 | 0.9 | 1.6 | 0.0 | 1.9 | 1.5 | 1.8 | 1.6 | 2.5 | 3.1 |
| con los especiales y +1 de PODER, esquivando la mitad | 2.6 | 0.2 | 2.8 | 1.8 | 1.6 | 1.0 | 1.7 | 0.1 | 0.2 | 0.8 | 1.4 | 0.0 | 1.9 | 1.4 | 1.6 | 1.8 | 2.3 | 2.8 |

#### Perfil medio: cuantas veces tiene cada carta al morir en el muro (promedio de las ultimas 20 partidas)

| variante | cadencia | critico | dano | disparo_doble | dron | escudo | explosiva | furia_larga | granada_rapida | hacia_atras | hielo | iman | perforante | quemadura | vampiro | velocidad | vida | doradas |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| solo el escupidor | 3.6 | 0.4 | 3.5 | 2.0 | 2.0 | 1.0 | 1.9 | 0.5 | 0.8 | 1.0 | 1.9 | 0.3 | 2.0 | 1.9 | 1.9 | 1.9 | 2.9 | 4.7 |
| solo el saltador | 3.3 | 0.3 | 3.7 | 2.0 | 1.9 | 1.0 | 1.9 | 0.5 | 1.0 | 1.0 | 1.9 | 0.3 | 2.0 | 1.8 | 2.0 | 1.9 | 2.9 | 4.7 |
| solo el chaman | 3.5 | 0.3 | 3.6 | 1.9 | 2.0 | 1.0 | 1.9 | 0.4 | 0.8 | 0.9 | 1.8 | 0.3 | 2.0 | 1.8 | 2.0 | 1.9 | 2.8 | 4.5 |
| solo el blindado | 3.6 | 0.3 | 3.7 | 2.0 | 1.9 | 1.0 | 1.9 | 0.4 | 0.8 | 1.0 | 1.9 | 0.4 | 2.0 | 1.8 | 1.9 | 1.8 | 2.8 | 4.6 |
| solo el hinchado | 3.5 | 0.3 | 3.5 | 2.0 | 2.0 | 1.0 | 2.0 | 0.6 | 0.8 | 1.0 | 1.9 | 0.5 | 2.0 | 1.9 | 1.9 | 2.0 | 2.9 | 4.8 |
| con los especiales, esquivando el 90 % de sus avisos | 3.7 | 0.4 | 3.8 | 2.0 | 2.0 | 1.0 | 2.0 | 0.6 | 1.0 | 1.0 | 1.9 | 0.5 | 2.0 | 1.9 | 1.9 | 1.9 | 2.9 | 4.7 |
| con los especiales, esquivando la mitad que al jefe | 3.5 | 0.1 | 3.6 | 1.9 | 1.9 | 1.0 | 2.0 | 0.5 | 0.8 | 0.9 | 1.8 | 0.2 | 2.0 | 1.9 | 1.9 | 2.0 | 2.7 | 4.3 |
| con los especiales, los zombis en cuatro chorros | 3.6 | 0.4 | 3.7 | 1.9 | 2.0 | 1.0 | 2.0 | 0.5 | 0.8 | 0.9 | 1.9 | 0.4 | 2.0 | 1.9 | 1.9 | 2.0 | 2.9 | 4.6 |
| con los especiales y +1 de PODER, esquivando la mitad | 3.6 | 0.3 | 3.7 | 2.0 | 1.9 | 1.0 | 2.0 | 0.7 | 1.0 | 1.0 | 1.9 | 0.3 | 2.0 | 1.9 | 2.0 | 1.9 | 2.9 | 4.4 |

#### Perfil alto: cuantas veces tiene cada carta al morir en el muro (promedio de las ultimas 20 partidas)

| variante | cadencia | critico | dano | disparo_doble | dron | escudo | explosiva | furia_larga | granada_rapida | hacia_atras | hielo | iman | perforante | quemadura | vampiro | velocidad | vida | doradas |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| solo el escupidor | 3.9 | 1.0 | 3.9 | 2.0 | 2.0 | 1.0 | 2.0 | 1.1 | 1.3 | 1.0 | 2.0 | 0.6 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 5.8 |
| solo el saltador | 3.9 | 0.9 | 3.9 | 2.0 | 2.0 | 1.0 | 2.0 | 1.1 | 1.4 | 1.0 | 2.0 | 0.7 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 5.5 |
| solo el chaman | 3.9 | 0.8 | 3.9 | 2.0 | 2.0 | 1.0 | 2.0 | 1.2 | 1.4 | 1.0 | 2.0 | 0.6 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 5.7 |
| solo el blindado | 4.0 | 0.9 | 3.9 | 2.0 | 2.0 | 1.0 | 2.0 | 1.2 | 1.3 | 1.0 | 2.0 | 0.6 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 5.7 |
| solo el hinchado | 3.9 | 1.1 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 1.2 | 1.4 | 1.0 | 2.0 | 0.8 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 5.9 |
| con los especiales, esquivando el 90 % de sus avisos | 3.9 | 1.0 | 3.9 | 2.0 | 2.0 | 1.0 | 2.0 | 1.3 | 1.5 | 1.0 | 2.0 | 1.2 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 6.1 |
| con los especiales, esquivando la mitad que al jefe | 3.8 | 0.6 | 3.9 | 2.0 | 2.0 | 1.0 | 2.0 | 1.1 | 1.0 | 1.0 | 2.0 | 0.5 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 5.1 |
| con los especiales, los zombis en cuatro chorros | 4.0 | 1.0 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 1.2 | 1.5 | 1.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 5.8 |
| con los especiales y +1 de PODER, esquivando la mitad | 3.9 | 1.0 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 1.4 | 1.4 | 1.0 | 2.0 | 0.8 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 5.4 |

### En que partida se llega a cada oleada

La partida en que se completa por primera vez la oleada (mediana de las semillas; "—" si no llega la mitad).

#### Perfil bajo

| variante | llega a 10 | llega a 20 | llega a 30 | llega a 40 | llega a 50 | llega a 60 |
|---|---|---|---|---|---|---|
| solo el escupidor | 11 | 17 | 26 | 37 | — | — |
| solo el saltador | 11 | 19 | 27 | 40 | — | — |
| solo el chaman | 11 | 18 | 29 | 42 | — | — |
| solo el blindado | 11 | 19 | 29 | 41 | 89 | — |
| solo el hinchado | 11 | 19 | 26 | 42 | — | — |
| con los especiales, esquivando el 90 % de sus avisos | 11 | 18 | 24 | 31 | 69 | — |
| con los especiales, esquivando la mitad que al jefe | 11 | 21 | 32 | 53 | — | — |
| con los especiales, los zombis en cuatro chorros | 11 | 20 | 26 | 39 | — | — |
| con los especiales y +1 de PODER, esquivando la mitad | 11 | 20 | 27 | 61 | — | — |

#### Perfil medio

| variante | llega a 10 | llega a 20 | llega a 30 | llega a 40 | llega a 50 | llega a 60 |
|---|---|---|---|---|---|---|
| solo el escupidor | 4 | 5 | 8 | 10 | 11 | 30 |
| solo el saltador | 4 | 6 | 7 | 10 | 13 | 34 |
| solo el chaman | 4 | 5 | 8 | 9 | 12 | 33 |
| solo el blindado | 4 | 5 | 6 | 8 | 13 | 26 |
| solo el hinchado | 4 | 5 | 8 | 9 | 13 | 30 |
| con los especiales, esquivando el 90 % de sus avisos | 4 | 5 | 6 | 8 | 11 | 29 |
| con los especiales, esquivando la mitad que al jefe | 4 | 6 | 8 | 12 | 15 | — |
| con los especiales, los zombis en cuatro chorros | 4 | 5 | 8 | 10 | 11 | 29 |
| con los especiales y +1 de PODER, esquivando la mitad | 4 | 6 | 7 | 11 | 15 | 59 |

#### Perfil alto

| variante | llega a 10 | llega a 20 | llega a 30 | llega a 40 | llega a 50 | llega a 60 |
|---|---|---|---|---|---|---|
| solo el escupidor | 2 | 3 | 4 | 5 | 6 | 7 |
| solo el saltador | 2 | 3 | 4 | 5 | 6 | 7 |
| solo el chaman | 2 | 3 | 4 | 5 | 5 | 7 |
| solo el blindado | 2 | 3 | 3 | 5 | 5 | 8 |
| solo el hinchado | 2 | 3 | 3 | 5 | 6 | 7 |
| con los especiales, esquivando el 90 % de sus avisos | 2 | 3 | 3 | 4 | 5 | 8 |
| con los especiales, esquivando la mitad que al jefe | 2 | 3 | 4 | 5 | 5 | 11 |
| con los especiales, los zombis en cuatro chorros | 2 | 3 | 3 | 4 | 5 | 7 |
| con los especiales y +1 de PODER, esquivando la mitad | 2 | 3 | 4 | 5 | 5 | 11 |

### El muro de cada semilla

La mediana de las ultimas 20 partidas de cada semilla, su media, y la mediana de todas juntas (la del muro de arriba).

#### Perfil bajo

| variante | semilla 1000 | semilla 1001 | semilla 1002 | semilla 1003 | semilla 1004 | media | todas juntas |
|---|---|---|---|---|---|---|---|
| solo el escupidor | 40 | 39 | 39 | 39 | 40 | 39.4 | 39 |
| solo el saltador | 39 | 39 | 39 | 39 | 39 | 39.0 | 39 |
| solo el chaman | 39 | 40 | 39.5 | 39 | 39.5 | 39.4 | 39 |
| solo el blindado | 44 | 39.5 | 40.5 | 40 | 39 | 40.6 | 40 |
| solo el hinchado | 39 | 39 | 44.5 | 39.5 | 39 | 40.2 | 39 |
| con los especiales, esquivando el 90 % de sus avisos | 40 | 45 | 49 | 44.5 | 41.5 | 44.0 | 46 |
| con los especiales, esquivando la mitad que al jefe | 38.5 | 39 | 39 | 38 | 39 | 38.7 | 39 |
| con los especiales, los zombis en cuatro chorros | 39 | 41 | 40 | 41 | 39 | 40.0 | 39 |
| con los especiales y +1 de PODER, esquivando la mitad | 39 | 39 | 39 | 38.5 | 39 | 38.9 | 39 |

#### Perfil medio

| variante | semilla 1000 | semilla 1001 | semilla 1002 | semilla 1003 | semilla 1004 | media | todas juntas |
|---|---|---|---|---|---|---|---|
| solo el escupidor | 59 | 59 | 59 | 59 | 59 | 59.0 | 59 |
| solo el saltador | 59 | 59 | 59 | 59 | 59 | 59.0 | 59 |
| solo el chaman | 59 | 58 | 59 | 59 | 59 | 58.8 | 59 |
| solo el blindado | 59 | 59 | 59 | 59 | 59 | 59.0 | 59 |
| solo el hinchado | 59 | 59 | 59 | 59 | 59 | 59.0 | 59 |
| con los especiales, esquivando el 90 % de sus avisos | 59 | 59 | 59 | 59 | 59 | 59.0 | 59 |
| con los especiales, esquivando la mitad que al jefe | 54 | 56 | 55.5 | 54.5 | 49.5 | 53.9 | 54 |
| con los especiales, los zombis en cuatro chorros | 59 | 59 | 59 | 58 | 59 | 58.8 | 59 |
| con los especiales y +1 de PODER, esquivando la mitad | 56 | 57 | 56 | 56.5 | 56 | 56.3 | 56 |

#### Perfil alto

| variante | semilla 1000 | semilla 1001 | semilla 1002 | semilla 1003 | semilla 1004 | media | todas juntas |
|---|---|---|---|---|---|---|---|
| solo el escupidor | 69 | 69 | 69.5 | 69 | 69 | 69.1 | 69 |
| solo el saltador | 69 | 69 | 68.5 | 69 | 69 | 68.9 | 69 |
| solo el chaman | 69 | 67 | 71 | 69 | 70.5 | 69.3 | 69 |
| solo el blindado | 70.5 | 69.5 | 68 | 69 | 69 | 69.2 | 69 |
| solo el hinchado | 70 | 69 | 69.5 | 70.5 | 72.5 | 70.3 | 70 |
| con los especiales, esquivando el 90 % de sus avisos | 69 | 69 | 67.5 | 69 | 70 | 68.9 | 69 |
| con los especiales, esquivando la mitad que al jefe | 64 | 63 | 63 | 63.5 | 62.5 | 63.2 | 63 |
| con los especiales, los zombis en cuatro chorros | 69.5 | 69 | 69 | 69 | 69 | 69.1 | 69 |
| con los especiales y +1 de PODER, esquivando la mitad | 63.5 | 64.5 | 66 | 63.5 | 63.5 | 64.2 | 65 |

### Los zombis con habilidad

En las ultimas 20 partidas de cada semilla: de donde sale el danio que recibe el jugador (el reparto), cuantos especiales se matan, que parte de las muertes es la explosion del hinchado, los pulsos del chaman, los saltos y los escupitajos, y que parte de las balas comunes que van a un blindado para el escudo.

#### Perfil bajo

| variante | danio recibido: zarpazos / jefe / charco / salto / explosion | especiales muertos por partida | muertes por la explosion | pulsos del chaman por partida (potenciados por pulso) | saltos por partida (pegan) | escupitajos por partida (golpes de charco por escupitajo) | bala a blindados parada por el escudo |
|---|---|---|---|---|---|---|---|
| solo el escupidor | 60 % / 19 % / 21 % / 0 % / 0 % | 76.9 | 0.0 % | 0.0 (0.0) | 0.0 (0 %) | 178.3 (0.65) | — |
| solo el saltador | 67 % / 20 % / 0 % / 13 % / 0 % | 89.9 | 0.0 % | 0.0 (0.0) | 75.3 (40 %) | 0.0 (0.00) | — |
| solo el chaman | 77 % / 23 % / 0 % / 0 % / 0 % | 33.3 | 0.0 % | 67.2 (1.4) | 0.0 (0 %) | 0.0 (0.00) | — |
| solo el blindado | 82 % / 18 % / 0 % / 0 % / 0 % | 85.1 | 0.0 % | 0.0 (0.0) | 0.0 (0 %) | 0.0 (0.00) | 1 % |
| solo el hinchado | 61 % / 21 % / 0 % / 0 % / 18 % | 79.0 | 1.1 % | 0.0 (0.0) | 0.0 (0 %) | 0.0 (0.00) | — |
| con los especiales, esquivando el 90 % de sus avisos | 65 % / 25 % / 5 % / 2 % / 3 % | 348.2 | 0.9 % | 64.7 (1.3) | 62.7 (9 %) | 203.1 (0.11) | 3 % |
| con los especiales, esquivando la mitad que al jefe | 28 % / 12 % / 40 % / 7 % / 13 % | 236.5 | 0.6 % | 32.2 (1.3) | 34.6 (68 %) | 136.6 (2.03) | 4 % |
| con los especiales, los zombis en cuatro chorros | 46 % / 18 % / 18 % / 7 % / 11 % | 298.6 | 0.8 % | 52.5 (1.6) | 50.2 (41 %) | 169.1 (0.65) | 2 % |
| con los especiales y +1 de PODER, esquivando la mitad | 26 % / 15 % / 39 % / 8 % / 13 % | 243.5 | 0.6 % | 32.0 (1.3) | 35.3 (70 %) | 131.6 (2.05) | 4 % |

#### Perfil medio

| variante | danio recibido: zarpazos / jefe / charco / salto / explosion | especiales muertos por partida | muertes por la explosion | pulsos del chaman por partida (potenciados por pulso) | saltos por partida (pegan) | escupitajos por partida (golpes de charco por escupitajo) | bala a blindados parada por el escudo |
|---|---|---|---|---|---|---|---|
| solo el escupidor | 56 % / 20 % / 24 % / 0 % / 0 % | 151.2 | 0.0 % | 0.0 (0.0) | 0.0 (0 %) | 264.5 (0.25) | — |
| solo el saltador | 59 % / 25 % / 0 % / 16 % / 0 % | 228.9 | 0.0 % | 0.0 (0.0) | 163.4 (20 %) | 0.0 (0.00) | — |
| solo el chaman | 69 % / 31 % / 0 % / 0 % / 0 % | 88.7 | 0.0 % | 164.5 (1.4) | 0.0 (0 %) | 0.0 (0.00) | — |
| solo el blindado | 69 % / 31 % / 0 % / 0 % / 0 % | 185.1 | 0.0 % | 0.0 (0.0) | 0.0 (0 %) | 0.0 (0.00) | 1 % |
| solo el hinchado | 54 % / 28 % / 0 % / 0 % / 17 % | 182.3 | 0.9 % | 0.0 (0.0) | 0.0 (0 %) | 0.0 (0.00) | — |
| con los especiales, esquivando el 90 % de sus avisos | 52 % / 27 % / 11 % / 4 % / 6 % | 661.9 | 0.7 % | 131.0 (1.3) | 101.8 (10 %) | 227.9 (0.11) | 2 % |
| con los especiales, esquivando la mitad que al jefe | 13 % / 10 % / 47 % / 14 % / 16 % | 556.6 | 0.5 % | 97.8 (1.3) | 78.9 (59 %) | 172.5 (1.41) | 3 % |
| con los especiales, los zombis en cuatro chorros | 44 % / 22 % / 17 % / 8 % / 9 % | 629.9 | 0.7 % | 135.4 (1.7) | 96.6 (20 %) | 212.5 (0.25) | 1 % |
| con los especiales y +1 de PODER, esquivando la mitad | 11 % / 10 % / 50 % / 15 % / 15 % | 590.6 | 0.5 % | 102.0 (1.3) | 82.0 (60 %) | 173.1 (1.44) | 1 % |

#### Perfil alto

| variante | danio recibido: zarpazos / jefe / charco / salto / explosion | especiales muertos por partida | muertes por la explosion | pulsos del chaman por partida (potenciados por pulso) | saltos por partida (pegan) | escupitajos por partida (golpes de charco por escupitajo) | bala a blindados parada por el escudo |
|---|---|---|---|---|---|---|---|
| solo el escupidor | 70 % / 13 % / 18 % / 0 % / 0 % | 222.0 | 0.0 % | 0.0 (0.0) | 0.0 (0 %) | 296.5 (0.08) | — |
| solo el saltador | 74 % / 16 % / 0 % / 10 % / 0 % | 340.6 | 0.0 % | 0.0 (0.0) | 191.8 (8 %) | 0.0 (0.00) | — |
| solo el chaman | 88 % / 12 % / 0 % / 0 % / 0 % | 139.2 | 0.0 % | 243.3 (1.5) | 0.0 (0 %) | 0.0 (0.00) | — |
| solo el blindado | 87 % / 13 % / 0 % / 0 % / 0 % | 274.7 | 0.0 % | 0.0 (0.0) | 0.0 (0 %) | 0.0 (0.00) | 0 % |
| solo el hinchado | 72 % / 16 % / 0 % / 0 % / 11 % | 272.4 | 0.7 % | 0.0 (0.0) | 0.0 (0 %) | 0.0 (0.00) | — |
| con los especiales, esquivando el 90 % de sus avisos | 59 % / 12 % / 17 % / 6 % / 6 % | 956.6 | 0.5 % | 189.7 (1.4) | 115.8 (10 %) | 259.0 (0.11) | 1 % |
| con los especiales, esquivando la mitad que al jefe | 9 % / 6 % / 53 % / 17 % / 15 % | 814.1 | 0.4 % | 125.7 (1.3) | 83.8 (54 %) | 163.4 (1.15) | 0 % |
| con los especiales, los zombis en cuatro chorros | 59 % / 17 % / 14 % / 4 % / 5 % | 951.8 | 0.6 % | 204.2 (1.8) | 115.0 (8 %) | 253.3 (0.09) | 1 % |
| con los especiales y +1 de PODER, esquivando la mitad | 8 % / 5 % / 56 % / 17 % / 14 % | 836.2 | 0.4 % | 125.6 (1.3) | 81.8 (54 %) | 166.3 (1.14) | 0 % |

## Tercera corrida: lo principal con 10 semillas (100 partidas x 10 semillas)

partidas 100, semillas 10, perfiles bajo,medio,alto, 1194 s

mejor pN: la mejor oleada completada hasta la partida N (mediana de las semillas). muro: la oleada mediana de las ultimas 20 partidas de cada semilla. llega a N: la partida en que se completa por primera vez la oleada N. min. en el muro: lo que dura una de esas partidas, con 3 s por eleccion de carta. % muertes en jefe: de las ultimas 100 partidas de cada semilla.

### El muro

#### Perfil bajo

| variante | mejor p10 | mejor p30 | muro (ult. 20) | mejor al final | llega a 40 | a 50 | a 60 | a 80 | al tope (100) | min. en el muro | cartas por partida | % muertes en jefe | arma en el muro | sin balas con blanco | revivio |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| V3 con el PODER 28 x 1,12^L | 8 | 38 | 39 | 49 | 43 | — | — | — | 0 % | 16.3 | 21 | 39 % | energia | 0 % | — |
| hoy con los cinco especiales | 8 | 37 | 39 | 49 | 40 | — | — | — | 0 % | 16.5 | 22 | 42 % | energia | 0 % | — |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 8 | 38 | 44 | 49 | 40 | — | — | — | 0 % | 19.0 | 24 | 43 % | energia | 0 % | — |
| con los especiales, esquivando la mitad que al jefe | 8 | 30 | 39 | 45 | 56 | — | — | — | 0 % | 15.8 | 21 | 34 % | energia | 0 % | — |
| con los especiales y +1 de PODER, esquivando la mitad | 8 | 32 | 39 | 45 | 53 | — | — | — | 0 % | 15.8 | 22 | 35 % | energia | 0 % | — |

#### Perfil medio

| variante | mejor p10 | mejor p30 | muro (ult. 20) | mejor al final | llega a 40 | a 50 | a 60 | a 80 | al tope (100) | min. en el muro | cartas por partida | % muertes en jefe | arma en el muro | sin balas con blanco | revivio |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| V3 con el PODER 28 x 1,12^L | 44 | 60 | 59 | 64 | 10 | 13 | 24 | — | 0 % | 30.2 | 27 | 60 % | energia | 0 % | — |
| hoy con los cinco especiales | 48 | 61 | 59 | 64 | 9 | 12 | 25 | — | 0 % | 30.8 | 28 | 59 % | energia | 0 % | — |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 49 | 61 | 59 | 64 | 9 | 11 | 29 | — | 0 % | 30.8 | 29 | 60 % | energia | 0 % | — |
| con los especiales, esquivando la mitad que al jefe | 39 | 56 | 54 | 59 | 11 | 15 | — | — | 0 % | 26.8 | 26 | 42 % | energia | 0 % | — |
| con los especiales y +1 de PODER, esquivando la mitad | 39 | 57 | 56 | 60 | 11 | 15 | 62 | — | 0 % | 27.9 | 28 | 45 % | energia | 0 % | — |

#### Perfil alto

| variante | mejor p10 | mejor p30 | muro (ult. 20) | mejor al final | llega a 40 | a 50 | a 60 | a 80 | al tope (100) | min. en el muro | cartas por partida | % muertes en jefe | arma en el muro | sin balas con blanco | revivio |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| V3 con el PODER 28 x 1,12^L | 66 | 73 | 69 | 76 | 4 | 6 | 7 | — | 0 % | 38.8 | 29 | 36 % | energia | 0 % | — |
| hoy con los cinco especiales | 64 | 72 | 69 | 75 | 5 | 6 | 7 | — | 0 % | 40.1 | 30 | 44 % | energia | 0 % | — |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 68 | 73 | 70 | 76 | 4 | 5 | 6 | — | 0 % | 41.2 | 32 | 44 % | energia | 0 % | — |
| con los especiales, esquivando la mitad que al jefe | 60 | 66 | 63 | 69 | 5 | 6 | 10 | — | 0 % | 33.7 | 29 | 33 % | energia | 0 % | — |
| con los especiales y +1 de PODER, esquivando la mitad | 59 | 66 | 65 | 69 | 4 | 6 | 11 | — | 0 % | 35.0 | 30 | 32 % | energia | 0 % | — |

### El arranque

#### Perfil bajo: las primeras 12 partidas (oleada a la que se llega, mediana; entre parentesis el arma con que se arranca)

| variante | p1 | p2 | p3 | p4 | p5 | p6 | p7 | p8 | p9 | p10 | p11 | p12 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| V3 con el PODER 28 x 1,12^L | 1 (pis) | 2 (pis) | 2 (rev) | 3 (rev) | 3 (rev) | 4 (rev) | 5 (esc) | 6 (esc) | 7 (esc) | 7 (esc) | 9 (esc) | 9 (esc) |
| hoy con los cinco especiales | 1 (pis) | 2 (pis) | 2 (rev) | 3 (rev) | 3 (rev) | 4 (rev) | 5 (esc) | 6 (esc) | 7 (esc) | 7 (esc) | 9 (esc) | 9 (esc) |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 1 (pis) | 2 (pis) | 2 (rev) | 3 (rev) | 3 (rev) | 4 (rev) | 5 (esc) | 6 (esc) | 7 (esc) | 7 (esc) | 9 (esc) | 9 (esc) |
| con los especiales, esquivando la mitad que al jefe | 1 (pis) | 2 (pis) | 2 (rev) | 3 (rev) | 3 (rev) | 4 (rev) | 5 (esc) | 6 (esc) | 7 (esc) | 7 (esc) | 9 (esc) | 9 (esc) |
| con los especiales y +1 de PODER, esquivando la mitad | 1 (pis) | 2 (pis) | 2 (rev) | 3 (rev) | 3 (rev) | 4 (rev) | 5 (esc) | 6 (esc) | 7 (esc) | 7 (esc) | 9 (esc) | 9 (esc) |

#### Perfil medio: las primeras 12 partidas (oleada a la que se llega, mediana; entre parentesis el arma con que se arranca)

| variante | p1 | p2 | p3 | p4 | p5 | p6 | p7 | p8 | p9 | p10 | p11 | p12 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| V3 con el PODER 28 x 1,12^L | 4 (pis) | 6 (rev) | 9 (esc) | 13 (esc) | 22 (esc) | 25 (rif) | 29 (rif) | 33 (rif) | 38 (rif) | 43 (rif) | 44 (rif) | 49 (ene) |
| hoy con los cinco especiales | 4 (pis) | 6 (rev) | 9 (esc) | 16 (esc) | 19 (esc) | 28 (rif) | 30 (rif) | 33 (rif) | 39 (rif) | 47 (rif) | 48 (ene) | 49 (ene) |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 4 (pis) | 6 (rev) | 9 (esc) | 14 (esc) | 25 (esc) | 31 (rif) | 39 (rif) | 38 (rif) | 48 (rif) | 49 (ene) | 49 (ene) | 50 (ene) |
| con los especiales, esquivando la mitad que al jefe | 4 (pis) | 6 (rev) | 9 (esc) | 15 (esc) | 19 (esc) | 21 (rif) | 28 (rif) | 31 (rif) | 34 (rif) | 36 (rif) | 40 (rif) | 44 (ene) |
| con los especiales y +1 de PODER, esquivando la mitad | 4 (pis) | 6 (rev) | 9 (esc) | 14 (esc) | 19 (esc) | 25 (rif) | 29 (rif) | 31 (rif) | 30 (rif) | 39 (rif) | 43 (rif) | 48 (ene) |

#### Perfil alto: las primeras 12 partidas (oleada a la que se llega, mediana; entre parentesis el arma con que se arranca)

| variante | p1 | p2 | p3 | p4 | p5 | p6 | p7 | p8 | p9 | p10 | p11 | p12 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| V3 con el PODER 28 x 1,12^L | 7 (pis) | 15 (esc) | 30 (esc) | 43 (rif) | 49 (rif) | 58 (rif) | 59 (ene) | 63 (ene) | 60 (ene) | 61 (ene) | 65 (ene) | 64 (ene) |
| hoy con los cinco especiales | 7 (pis) | 16 (esc) | 29 (esc) | 39 (rif) | 49 (rif) | 59 (rif) | 59 (ene) | 63 (ene) | 61 (ene) | 59 (ene) | 64 (ene) | 65 (ene) |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 7 (pis) | 15 (esc) | 29 (esc) | 47 (rif) | 54 (rif) | 60 (ene) | 60 (ene) | 64 (ene) | 67 (ene) | 61 (ene) | 67 (ene) | 66 (ene) |
| con los especiales, esquivando la mitad que al jefe | 7 (pis) | 16 (esc) | 28 (esc) | 39 (rif) | 48 (rif) | 49 (rif) | 55 (ene) | 56 (ene) | 59 (ene) | 59 (ene) | 60 (ene) | 58 (ene) |
| con los especiales y +1 de PODER, esquivando la mitad | 7 (pis) | 15 (esc) | 29 (esc) | 41 (rif) | 48 (rif) | 52 (rif) | 56 (ene) | 58 (ene) | 58 (ene) | 58 (ene) | 61 (ene) | 59 (ene) |

### El oro por minuto

Las monedas de la partida (las juntadas, el bono de cada oleada y el tesoro) sobre sus minutos (con 3 s por eleccion de carta), sumadas las de todas las semillas.

#### Perfil bajo

| variante | oleada en la p10 | monedas en la p10 | min. de la p10 | oro/min en la p10 | oro/min p8-p12 | oro/min en el muro (ult. 20) | monedas por partida en el muro |
|---|---|---|---|---|---|---|---|
| V3 con el PODER 28 x 1,12^L | 7 | 305 | 2.5 | 146 | 158 | 1950 | 29092 |
| hoy con los cinco especiales | 7 | 305 | 2.5 | 141 | 166 | 2230 | 33659 |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 7 | 305 | 2.5 | 141 | 159 | 2317 | 43811 |
| con los especiales, esquivando la mitad que al jefe | 7 | 305 | 2.5 | 148 | 159 | 1988 | 31984 |
| con los especiales y +1 de PODER, esquivando la mitad | 7 | 305 | 2.5 | 145 | 162 | 2025 | 32424 |

#### Perfil medio

| variante | oleada en la p10 | monedas en la p10 | min. de la p10 | oro/min en la p10 | oro/min p8-p12 | oro/min en el muro (ult. 20) | monedas por partida en el muro |
|---|---|---|---|---|---|---|---|
| V3 con el PODER 28 x 1,12^L | 44 | 27252 | 18.5 | 1556 | 1484 | 2953 | 90511 |
| hoy con los cinco especiales | 48 | 40011 | 21.7 | 1789 | 1839 | 3544 | 110508 |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 49 | 49572 | 23.7 | 2085 | 2061 | 3589 | 111228 |
| con los especiales, esquivando la mitad que al jefe | 36 | 17778 | 14.8 | 1336 | 1453 | 3217 | 85098 |
| con los especiales y +1 de PODER, esquivando la mitad | 39 | 23612 | 15.9 | 1435 | 1536 | 3310 | 92700 |

#### Perfil alto

| variante | oleada en la p10 | monedas en la p10 | min. de la p10 | oro/min en la p10 | oro/min p8-p12 | oro/min en el muro (ult. 20) | monedas por partida en el muro |
|---|---|---|---|---|---|---|---|
| V3 con el PODER 28 x 1,12^L | 62 | 96325 | 33.4 | 3067 | 3157 | 3930 | 152187 |
| hoy con los cinco especiales | 59 | 114362 | 30.8 | 3912 | 3863 | 4737 | 190685 |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 62 | 128138 | 32.6 | 3997 | 4087 | 4828 | 194792 |
| con los especiales, esquivando la mitad que al jefe | 59 | 102793 | 30.0 | 3424 | 3397 | 4202 | 141985 |
| con los especiales y +1 de PODER, esquivando la mitad | 58 | 103362 | 29.4 | 3460 | 3440 | 4305 | 152029 |

### Los minutos y el oro hasta cada oleada

Al terminar la oleada N, en las partidas que la terminan (mediana de todas, de todas las semillas; "—" con menos de 10).

#### Perfil bajo

| variante | min. hasta la 10 | min. hasta la 20 | min. hasta la 30 | min. hasta la 40 | min. hasta la 50 | min. hasta la 55 | min. hasta la 60 | min. hasta la 70 | oro hasta la 10 | oro hasta la 30 | oro hasta la 50 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| V3 con el PODER 28 x 1,12^L | 2.9 | 6.4 | 10.8 | 16.3 | — | — | — | — | 1609 | 14449 | — |
| hoy con los cinco especiales | 2.9 | 6.5 | 11.0 | 16.6 | — | — | — | — | 1611 | 16222 | — |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 2.8 | 6.5 | 11.0 | 16.6 | — | — | — | — | 1628 | 16227 | — |
| con los especiales, esquivando la mitad que al jefe | 2.9 | 6.5 | 11.0 | 16.6 | — | — | — | — | 1607 | 16148 | — |
| con los especiales y +1 de PODER, esquivando la mitad | 2.9 | 6.5 | 11.0 | 16.4 | — | — | — | — | 1610 | 16136 | — |

#### Perfil medio

| variante | min. hasta la 10 | min. hasta la 20 | min. hasta la 30 | min. hasta la 40 | min. hasta la 50 | min. hasta la 55 | min. hasta la 60 | min. hasta la 70 | oro hasta la 10 | oro hasta la 30 | oro hasta la 50 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| V3 con el PODER 28 x 1,12^L | 2.7 | 6.1 | 10.3 | 15.3 | 22.2 | 26.2 | 31.8 | — | 1701 | 15126 | 54916 |
| hoy con los cinco especiales | 2.7 | 6.1 | 10.3 | 15.4 | 22.4 | 26.6 | 32.1 | — | 1698 | 17225 | 65939 |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 2.7 | 6.1 | 10.3 | 15.3 | 22.2 | 26.5 | 31.9 | — | 1701 | 17209 | 65983 |
| con los especiales, esquivando la mitad que al jefe | 2.7 | 6.1 | 10.3 | 15.4 | 22.4 | 26.6 | — | — | 1695 | 16959 | 65274 |
| con los especiales y +1 de PODER, esquivando la mitad | 2.7 | 6.1 | 10.3 | 15.4 | 22.3 | 26.4 | — | — | 1694 | 16992 | 65339 |

#### Perfil alto

| variante | min. hasta la 10 | min. hasta la 20 | min. hasta la 30 | min. hasta la 40 | min. hasta la 50 | min. hasta la 55 | min. hasta la 60 | min. hasta la 70 | oro hasta la 10 | oro hasta la 30 | oro hasta la 50 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| V3 con el PODER 28 x 1,12^L | 2.7 | 6.0 | 9.9 | 14.6 | 20.8 | 24.8 | 29.8 | 40.3 | 1746 | 15349 | 55894 |
| hoy con los cinco especiales | 2.7 | 6.0 | 10.0 | 14.7 | 20.9 | 25.0 | 30.0 | 41.1 | 1742 | 17775 | 68712 |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 2.7 | 6.0 | 10.0 | 14.7 | 20.9 | 25.0 | 29.9 | 41.0 | 1748 | 17780 | 68867 |
| con los especiales, esquivando la mitad que al jefe | 2.7 | 6.0 | 10.0 | 14.7 | 21.0 | 25.1 | 30.0 | — | 1740 | 17693 | 67888 |
| con los especiales y +1 de PODER, esquivando la mitad | 2.7 | 6.0 | 10.0 | 14.8 | 21.0 | 25.0 | 30.0 | — | 1745 | 17743 | 68066 |

### Las cartas

#### Cartas elegidas al terminar cada oleada (promedio de todas las partidas que la terminan)

| variante | perfil | 1 | 2 | 3 | 4 | 5 | 6 | 8 | 10 | 15 | 20 | 30 | 40 | 50 | 60 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|

#### Perfil bajo: cuantas veces tiene cada carta al morir en el muro (promedio de las ultimas 20 partidas)

| variante | cadencia | critico | dano | disparo_doble | dron | escudo | explosiva | furia_larga | granada_rapida | hacia_atras | hielo | iman | perforante | quemadura | vampiro | velocidad | vida | doradas |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| V3 con el PODER 28 x 1,12^L | 2.5 | 0.1 | 2.8 | 1.7 | 1.7 | 0.9 | 1.7 | 0.1 | 0.3 | 0.8 | 1.6 | 0.1 | 1.9 | 1.4 | 1.7 | 1.6 | 2.5 | 3.1 |
| hoy con los cinco especiales | 2.6 | 0.0 | 2.9 | 1.8 | 1.8 | 0.9 | 1.8 | 0.2 | 0.3 | 0.9 | 1.5 | 0.1 | 1.8 | 1.5 | 1.8 | 1.7 | 2.5 | 3.1 |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 2.8 | 0.2 | 3.1 | 1.9 | 1.8 | 1.0 | 1.8 | 0.2 | 0.5 | 0.9 | 1.6 | 0.1 | 1.9 | 1.6 | 1.8 | 1.8 | 2.8 | 3.4 |
| con los especiales, esquivando la mitad que al jefe | 2.3 | 0.0 | 2.5 | 1.8 | 1.6 | 0.9 | 1.7 | 0.1 | 0.3 | 0.8 | 1.6 | 0.1 | 1.8 | 1.3 | 1.6 | 1.7 | 2.5 | 2.9 |
| con los especiales y +1 de PODER, esquivando la mitad | 2.6 | 0.1 | 2.9 | 1.8 | 1.6 | 0.9 | 1.7 | 0.1 | 0.3 | 0.8 | 1.5 | 0.0 | 1.9 | 1.4 | 1.7 | 1.7 | 2.4 | 2.9 |

#### Perfil medio: cuantas veces tiene cada carta al morir en el muro (promedio de las ultimas 20 partidas)

| variante | cadencia | critico | dano | disparo_doble | dron | escudo | explosiva | furia_larga | granada_rapida | hacia_atras | hielo | iman | perforante | quemadura | vampiro | velocidad | vida | doradas |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| V3 con el PODER 28 x 1,12^L | 3.4 | 0.3 | 3.6 | 1.9 | 1.9 | 1.0 | 1.9 | 0.4 | 0.8 | 1.0 | 1.9 | 0.3 | 2.0 | 1.8 | 1.9 | 1.9 | 2.9 | 4.5 |
| hoy con los cinco especiales | 3.6 | 0.4 | 3.8 | 2.0 | 1.9 | 1.0 | 2.0 | 0.6 | 1.0 | 1.0 | 1.9 | 0.4 | 2.0 | 1.9 | 1.9 | 2.0 | 2.9 | 4.6 |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 3.8 | 0.5 | 3.9 | 2.0 | 2.0 | 1.0 | 2.0 | 0.9 | 1.2 | 1.0 | 2.0 | 0.5 | 2.0 | 2.0 | 2.0 | 2.0 | 2.9 | 4.8 |
| con los especiales, esquivando la mitad que al jefe | 3.4 | 0.2 | 3.6 | 1.9 | 1.9 | 1.0 | 1.9 | 0.5 | 0.8 | 0.9 | 1.8 | 0.2 | 2.0 | 1.8 | 1.9 | 2.0 | 2.8 | 4.3 |
| con los especiales y +1 de PODER, esquivando la mitad | 3.5 | 0.3 | 3.7 | 2.0 | 2.0 | 1.0 | 2.0 | 0.7 | 1.0 | 1.0 | 1.9 | 0.4 | 2.0 | 1.9 | 2.0 | 1.9 | 2.9 | 4.4 |

#### Perfil alto: cuantas veces tiene cada carta al morir en el muro (promedio de las ultimas 20 partidas)

| variante | cadencia | critico | dano | disparo_doble | dron | escudo | explosiva | furia_larga | granada_rapida | hacia_atras | hielo | iman | perforante | quemadura | vampiro | velocidad | vida | doradas |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| V3 con el PODER 28 x 1,12^L | 3.8 | 0.8 | 3.9 | 2.0 | 2.0 | 1.0 | 2.0 | 0.9 | 1.3 | 1.0 | 2.0 | 0.8 | 2.0 | 1.9 | 2.0 | 2.0 | 2.9 | 5.7 |
| hoy con los cinco especiales | 3.9 | 1.1 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 1.4 | 1.5 | 1.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 5.9 |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 4.0 | 1.5 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 1.5 | 1.7 | 1.0 | 2.0 | 1.2 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 6.0 |
| con los especiales, esquivando la mitad que al jefe | 3.8 | 0.7 | 3.9 | 2.0 | 2.0 | 1.0 | 2.0 | 0.9 | 1.1 | 1.0 | 2.0 | 0.5 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 5.1 |
| con los especiales y +1 de PODER, esquivando la mitad | 3.9 | 0.9 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 1.3 | 1.5 | 1.0 | 2.0 | 0.9 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 5.4 |

### En que partida se llega a cada oleada

La partida en que se completa por primera vez la oleada (mediana de las semillas; "—" si no llega la mitad).

#### Perfil bajo

| variante | llega a 10 | llega a 20 | llega a 30 | llega a 40 | llega a 50 | llega a 60 |
|---|---|---|---|---|---|---|
| V3 con el PODER 28 x 1,12^L | 11 | 19 | 27 | 43 | — | — |
| hoy con los cinco especiales | 11 | 18 | 25 | 40 | — | — |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 11 | 18 | 25 | 40 | — | — |
| con los especiales, esquivando la mitad que al jefe | 11 | 20 | 30 | 56 | — | — |
| con los especiales y +1 de PODER, esquivando la mitad | 11 | 18 | 28 | 53 | — | — |

#### Perfil medio

| variante | llega a 10 | llega a 20 | llega a 30 | llega a 40 | llega a 50 | llega a 60 |
|---|---|---|---|---|---|---|
| V3 con el PODER 28 x 1,12^L | 4 | 5 | 8 | 10 | 13 | 24 |
| hoy con los cinco especiales | 4 | 5 | 7 | 9 | 12 | 25 |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 4 | 5 | 6 | 9 | 11 | 29 |
| con los especiales, esquivando la mitad que al jefe | 4 | 6 | 8 | 11 | 15 | — |
| con los especiales y +1 de PODER, esquivando la mitad | 4 | 6 | 8 | 11 | 15 | 62 |

#### Perfil alto

| variante | llega a 10 | llega a 20 | llega a 30 | llega a 40 | llega a 50 | llega a 60 |
|---|---|---|---|---|---|---|
| V3 con el PODER 28 x 1,12^L | 2 | 3 | 3 | 4 | 6 | 7 |
| hoy con los cinco especiales | 2 | 3 | 4 | 5 | 6 | 7 |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 2 | 3 | 4 | 4 | 5 | 6 |
| con los especiales, esquivando la mitad que al jefe | 2 | 3 | 4 | 5 | 6 | 10 |
| con los especiales y +1 de PODER, esquivando la mitad | 2 | 3 | 4 | 4 | 6 | 11 |

### El muro de cada semilla

La mediana de las ultimas 20 partidas de cada semilla, su media, y la mediana de todas juntas (la del muro de arriba); y, de las ultimas 20 de todas, la media, cuantas terminan antes de la 35 y cuantas mueren en la oleada de un jefe.

#### Perfil bajo

| variante | semilla 1000 | semilla 1001 | semilla 1002 | semilla 1003 | semilla 1004 | semilla 1005 | semilla 1006 | semilla 1007 | semilla 1008 | semilla 1009 | media | todas juntas | media de las ultimas 20 | terminan antes de la 35 | mueren en un jefe |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| V3 con el PODER 28 x 1,12^L | 39 | 39 | 42 | 39 | 41 | 39 | 42 | 40 | 39 | 42 | 40.2 | 39 | 41.7 | 5 % | 51 % |
| hoy con los cinco especiales | 39 | 39 | 42 | 39 | 42.5 | 39 | 39 | 39 | 41 | 39 | 39.9 | 39 | 41.5 | 2 % | 50 % |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 40 | 39 | 46 | 42.5 | 46 | 46.5 | 46.5 | 41 | 40.5 | 42.5 | 43.0 | 44 | 43.2 | 2 % | 47 % |
| con los especiales, esquivando la mitad que al jefe | 38.5 | 39 | 39 | 38 | 39 | 39 | 39 | 39 | 38.5 | 39 | 38.8 | 39 | 37.7 | 18 % | 43 % |
| con los especiales y +1 de PODER, esquivando la mitad | 39 | 39 | 39 | 38.5 | 39 | 39 | 39 | 39 | 39 | 38.5 | 38.9 | 39 | 38.2 | 12 % | 49 % |

#### Perfil medio

| variante | semilla 1000 | semilla 1001 | semilla 1002 | semilla 1003 | semilla 1004 | semilla 1005 | semilla 1006 | semilla 1007 | semilla 1008 | semilla 1009 | media | todas juntas | media de las ultimas 20 | terminan antes de la 35 | mueren en un jefe |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| V3 con el PODER 28 x 1,12^L | 59 | 59 | 59 | 59 | 59 | 59 | 59 | 59 | 59 | 59 | 59.0 | 59 | 58.1 | 0 % | 68 % |
| hoy con los cinco especiales | 59 | 59 | 59 | 59 | 59 | 59 | 59 | 59 | 59 | 59 | 59.0 | 59 | 58.1 | 0 % | 66 % |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 59 | 59 | 59 | 59 | 59 | 59 | 59 | 59 | 60.5 | 59 | 59.1 | 59 | 58.7 | 0 % | 61 % |
| con los especiales, esquivando la mitad que al jefe | 54 | 56 | 55.5 | 54.5 | 49.5 | 55.5 | 50 | 54.5 | 55 | 52.5 | 53.7 | 54 | 53.5 | 0 % | 38 % |
| con los especiales y +1 de PODER, esquivando la mitad | 56 | 57 | 56 | 56.5 | 56 | 57 | 56 | 51 | 55 | 53.5 | 55.4 | 56 | 54.4 | 0 % | 47 % |

#### Perfil alto

| variante | semilla 1000 | semilla 1001 | semilla 1002 | semilla 1003 | semilla 1004 | semilla 1005 | semilla 1006 | semilla 1007 | semilla 1008 | semilla 1009 | media | todas juntas | media de las ultimas 20 | terminan antes de la 35 | mueren en un jefe |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| V3 con el PODER 28 x 1,12^L | 69 | 69 | 69 | 69 | 69 | 69 | 69 | 69.5 | 69.5 | 69 | 69.1 | 69 | 68.8 | 0 % | 37 % |
| hoy con los cinco especiales | 69 | 69 | 69.5 | 69 | 69.5 | 69 | 69.5 | 69.5 | 69 | 70 | 69.3 | 69 | 68.8 | 0 % | 42 % |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 69 | 70 | 72 | 71.5 | 69 | 69 | 71 | 70 | 69 | 69 | 70.0 | 70 | 69.8 | 0 % | 40 % |
| con los especiales, esquivando la mitad que al jefe | 64 | 63 | 63 | 63.5 | 62.5 | 62.5 | 62.5 | 64.5 | 65.5 | 60.5 | 63.1 | 63 | 62.7 | 0 % | 32 % |
| con los especiales y +1 de PODER, esquivando la mitad | 63.5 | 64.5 | 66 | 63.5 | 63.5 | 65 | 64 | 65 | 64.5 | 65 | 64.5 | 65 | 63.5 | 0 % | 28 % |

### Los zombis con habilidad

En las ultimas 20 partidas de cada semilla: de donde sale el danio que recibe el jugador (el reparto), cuantos especiales se matan, que parte de las muertes es la explosion del hinchado, los pulsos del chaman, los saltos y los escupitajos, y que parte de las balas comunes que van a un blindado para el escudo.

#### Perfil bajo

| variante | danio recibido: zarpazos / jefe / charco / salto / explosion | especiales muertos por partida | reventones por partida (muertes por la explosion) | pulsos del chaman por partida (potenciados por pulso) | saltos por partida (pegan) | escupitajos por partida (golpes de charco por escupitajo) | bala a blindados parada por el escudo |
|---|---|---|---|---|---|---|---|
| hoy con los cinco especiales | 45 % / 19 % / 18 % / 7 % / 11 % | 300.3 | 60.8 (0.8 %) | 48.2 (1.3) | 49.3 (41 %) | 167.3 (0.63) | 3 % |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 44 % / 17 % / 20 % / 7 % / 11 % | 328.0 | 67.3 (0.8 %) | 58.5 (1.3) | 56.9 (40 %) | 189.0 (0.65) | 3 % |
| con los especiales, esquivando la mitad que al jefe | 27 % / 12 % / 40 % / 8 % / 13 % | 239.4 | 46.4 (0.6 %) | 32.7 (1.3) | 35.5 (69 %) | 134.8 (2.03) | 5 % |
| con los especiales y +1 de PODER, esquivando la mitad | 26 % / 14 % / 39 % / 8 % / 13 % | 248.4 | 48.8 (0.6 %) | 33.4 (1.3) | 36.8 (70 %) | 135.2 (2.03) | 4 % |

#### Perfil medio

| variante | danio recibido: zarpazos / jefe / charco / salto / explosion | especiales muertos por partida | reventones por partida (muertes por la explosion) | pulsos del chaman por partida (potenciados por pulso) | saltos por partida (pegan) | escupitajos por partida (golpes de charco por escupitajo) | bala a blindados parada por el escudo |
|---|---|---|---|---|---|---|---|
| hoy con los cinco especiales | 40 % / 24 % / 18 % / 8 % / 9 % | 657.9 | 138.9 (0.7 %) | 129.9 (1.3) | 98.2 (20 %) | 222.4 (0.24) | 1 % |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 40 % / 22 % / 20 % / 9 % / 10 % | 671.7 | 141.3 (0.6 %) | 129.8 (1.3) | 100.2 (20 %) | 221.9 (0.25) | 1 % |
| con los especiales, esquivando la mitad que al jefe | 13 % / 10 % / 47 % / 14 % / 16 % | 557.2 | 115.3 (0.5 %) | 96.4 (1.3) | 77.1 (60 %) | 169.3 (1.42) | 2 % |
| con los especiales y +1 de PODER, esquivando la mitad | 10 % / 11 % / 49 % / 14 % / 15 % | 580.6 | 120.6 (0.5 %) | 98.5 (1.3) | 79.4 (60 %) | 169.0 (1.44) | 1 % |

#### Perfil alto

| variante | danio recibido: zarpazos / jefe / charco / salto / explosion | especiales muertos por partida | reventones por partida (muertes por la explosion) | pulsos del chaman por partida (potenciados por pulso) | saltos por partida (pegan) | escupitajos por partida (golpes de charco por escupitajo) | bala a blindados parada por el escudo |
|---|---|---|---|---|---|---|---|
| hoy con los cinco especiales | 61 % / 14 % / 14 % / 5 % / 6 % | 954.4 | 201.0 (0.5 %) | 185.5 (1.4) | 114.1 (8 %) | 256.9 (0.09) | 1 % |
| con los especiales y +1 de PODER cada uno (3, 3, 4, 3, 3) | 60 % / 14 % / 15 % / 5 % / 6 % | 979.7 | 206.3 (0.5 %) | 193.2 (1.4) | 117.8 (8 %) | 264.9 (0.09) | 0 % |
| con los especiales, esquivando la mitad que al jefe | 9 % / 6 % / 53 % / 17 % / 15 % | 815.5 | 168.8 (0.4 %) | 124.8 (1.3) | 83.3 (54 %) | 163.3 (1.15) | 0 % |
| con los especiales y +1 de PODER, esquivando la mitad | 8 % / 5 % / 56 % / 17 % / 14 % | 841.5 | 174.4 (0.4 %) | 127.5 (1.3) | 82.6 (54 %) | 166.7 (1.15) | 1 % |
