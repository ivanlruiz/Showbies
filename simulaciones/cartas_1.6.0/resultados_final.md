# Simulacion de las cartas de la 1.6.0

partidas 100, semillas 5, perfiles bajo,medio,alto, 228 s

mejor pN: la mejor oleada completada hasta la partida N (mediana de las semillas). muro: la oleada mediana de las ultimas 20 partidas de cada semilla. llega a N: la partida en que se completa por primera vez la oleada N. min. en el muro: lo que dura una de esas partidas, con 3 s por eleccion de carta. % muertes en jefe: de las ultimas 100 partidas de cada semilla.

## El muro

### Perfil bajo

| variante | mejor p10 | mejor p30 | muro (ult. 20) | mejor al final | llega a 40 | a 50 | a 60 | a 80 | al tope (100) | min. en el muro | cartas por partida | % muertes en jefe | arma en el muro | sin balas con blanco | revivio |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 11 | 33 | 28 | 45 | 56 | — | — | — | 0 % | 15.1 | 0 | 16 % | energia | 27 % | — |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 55 | 58 | 57 | 59 | 6 | 8 | — | — | 0 % | 26.2 | 28 | 21 % | energia | 0 % | — |
| 1.6.0 final sin cartas | 22 | 34 | 33 | 35 | — | — | — | — | 0 % | 11.3 | 0 | 8 % | energia | 0 % | — |

### Perfil medio

| variante | mejor p10 | mejor p30 | muro (ult. 20) | mejor al final | llega a 40 | a 50 | a 60 | a 80 | al tope (100) | min. en el muro | cartas por partida | % muertes en jefe | arma en el muro | sin balas con blanco | revivio |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 47 | 57 | 56 | 59 | 9 | 13 | — | — | 0 % | 52.0 | 0 | 37 % | energia | 2 % | — |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 61 | 63 | 59 | 64 | 3 | 4 | 7 | — | 0 % | 26.5 | 29 | 67 % | energia | 0 % | — |
| 1.6.0 final sin cartas | 34 | 38 | 37 | 39 | — | — | — | — | 0 % | 11.8 | 0 | 5 % | energia | 0 % | — |

### Perfil alto

| variante | mejor p10 | mejor p30 | muro (ult. 20) | mejor al final | llega a 40 | a 50 | a 60 | a 80 | al tope (100) | min. en el muro | cartas por partida | % muertes en jefe | arma en el muro | sin balas con blanco | revivio |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 67 | 69 | 69 | 71 | 5 | 5 | 8 | — | 0 % | 76.0 | 0 | 58 % | energia | 0 % | — |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 69 | 70 | 67 | 71 | 2 | 3 | 4 | — | 0 % | 33.4 | 30 | 36 % | energia | 0 % | — |
| 1.6.0 final sin cartas | 40 | 41 | 41 | 44 | 9 | — | — | — | 0 % | 13.0 | 0 | 38 % | energia | 0 % | — |

## El arranque

### Perfil bajo: las primeras 12 partidas (oleada a la que se llega, mediana; entre parentesis el arma con que se arranca)

| variante | p1 | p2 | p3 | p4 | p5 | p6 | p7 | p8 | p9 | p10 | p11 | p12 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 1 (pis) | 2 (pis) | 2 (rev) | 2 (rev) | 3 (rev) | 5 (esc) | 6 (esc) | 9 (esc) | 9 (esc) | 11 (rif) | 19 (rif) | 19 (rif) |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 3 (pis) | 9 (rev) | 9 (esc) | 31 (esc) | 39 (rif) | 49 (ene) | 49 (ene) | 49 (ene) | 49 (ene) | 55 (ene) | 56 (ene) | 55 (ene) |
| 1.6.0 final sin cartas | 2 (pis) | 3 (rev) | 4 (rev) | 6 (esc) | 8 (esc) | 13 (esc) | 15 (rif) | 17 (rif) | 19 (rif) | 22 (rif) | 26 (rif) | 27 (rif) |

### Perfil medio: las primeras 12 partidas (oleada a la que se llega, mediana; entre parentesis el arma con que se arranca)

| variante | p1 | p2 | p3 | p4 | p5 | p6 | p7 | p8 | p9 | p10 | p11 | p12 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 3 (pis) | 3 (rev) | 7 (esc) | 9 (esc) | 19 (esc) | 26 (rif) | 29 (rif) | 39 (rif) | 47 (ene) | 40 (ene) | 49 (ene) | 49 (ene) |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 9 (pis) | 29 (esc) | 49 (rif) | 56 (ene) | 59 (ene) | 58 (ene) | 59 (ene) | 59 (ene) | 58 (ene) | 59 (ene) | 59 (ene) | 59 (ene) |
| 1.6.0 final sin cartas | 4 (pis) | 6 (rev) | 13 (esc) | 19 (esc) | 22 (rif) | 28 (rif) | 29 (rif) | 32 (ene) | 33 (ene) | 34 (ene) | 34 (ene) | 34 (ene) |

### Perfil alto: las primeras 12 partidas (oleada a la que se llega, mediana; entre parentesis el arma con que se arranca)

| variante | p1 | p2 | p3 | p4 | p5 | p6 | p7 | p8 | p9 | p10 | p11 | p12 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 5 (pis) | 12 (esc) | 29 (esc) | 39 (rif) | 55 (ene) | 59 (ene) | 59 (ene) | 62 (ene) | 66 (ene) | 59 (ene) | 65 (ene) | 67 (ene) |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 17 (pis) | 45 (rif) | 59 (ene) | 63 (ene) | 61 (ene) | 67 (ene) | 63 (ene) | 64 (ene) | 63 (ene) | 68 (ene) | 64 (ene) | 59 (ene) |
| 1.6.0 final sin cartas | 6 (pis) | 14 (esc) | 28 (rif) | 33 (rif) | 37 (ene) | 38 (ene) | 37 (ene) | 39 (ene) | 39 (ene) | 38 (ene) | 39 (ene) | 39 (ene) |

## El oro por minuto

Las monedas de la partida (las juntadas, el bono de cada oleada y el tesoro) sobre sus minutos (con 3 s por eleccion de carta), sumadas las de todas las semillas.

### Perfil bajo

| variante | oleada en la p10 | monedas en la p10 | min. de la p10 | oro/min en la p10 | oro/min p8-p12 | oro/min en el muro (ult. 20) | monedas por partida en el muro |
|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 11 | 2012 | 5.3 | 524 | 830 | 4745 | 51897 |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 55 | 733185 | 26.4 | 26950 | 26263 | 32447 | 859313 |
| 1.6.0 final sin cartas | 22 | 12780 | 7.0 | 1884 | 2135 | 7016 | 78340 |

### Perfil medio

| variante | oleada en la p10 | monedas en la p10 | min. de la p10 | oro/min en la p10 | oro/min p8-p12 | oro/min en el muro (ult. 20) | monedas por partida en el muro |
|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 40 | 195821 | 28.1 | 9238 | 10520 | 20809 | 1108401 |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 59 | 1072145 | 26.8 | 42010 | 40790 | 43190 | 1110508 |
| 1.6.0 final sin cartas | 34 | 75640 | 10.8 | 6854 | 6481 | 10529 | 123866 |

### Perfil alto

| variante | oleada en la p10 | monedas en la p10 | min. de la p10 | oro/min en la p10 | oro/min p8-p12 | oro/min en el muro (ult. 20) | monedas por partida en el muro |
|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 59 | 1502065 | 56.7 | 33953 | 37368 | 47410 | 3706336 |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 68 | 2577321 | 34.7 | 68801 | 60732 | 65863 | 2268997 |
| 1.6.0 final sin cartas | 38 | 137959 | 11.3 | 12294 | 12402 | 14412 | 185090 |

## Los minutos y el oro hasta cada oleada

Al terminar la oleada N, en las partidas que la terminan (mediana de todas, de todas las semillas; "—" con menos de 10).

### Perfil bajo

| variante | min. hasta la 10 | min. hasta la 20 | min. hasta la 30 | min. hasta la 40 | min. hasta la 50 | min. hasta la 55 | min. hasta la 60 | min. hasta la 70 | oro hasta la 10 | oro hasta la 30 | oro hasta la 50 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 2.9 | 8.3 | 16.4 | 28.2 | — | — | — | — | 3188 | 65343 | — |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 2.9 | 6.0 | 9.5 | 14.0 | 20.3 | 24.5 | — | — | 3146 | 52977 | 431123 |
| 1.6.0 final sin cartas | 2.4 | 5.6 | 9.6 | — | — | — | — | — | 3138 | 53419 | — |

### Perfil medio

| variante | min. hasta la 10 | min. hasta la 20 | min. hasta la 30 | min. hasta la 40 | min. hasta la 50 | min. hasta la 55 | min. hasta la 60 | min. hasta la 70 | oro hasta la 10 | oro hasta la 30 | oro hasta la 50 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 2.8 | 8.0 | 15.8 | 26.4 | 40.8 | 49.0 | — | — | 3323 | 66746 | 574592 |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 2.2 | 4.8 | 8.0 | 12.3 | 18.0 | 21.8 | 27.9 | — | 3145 | 53458 | 436296 |
| 1.6.0 final sin cartas | 1.7 | 4.4 | 8.2 | — | — | — | — | — | 3111 | 54115 | — |

### Perfil alto

| variante | min. hasta la 10 | min. hasta la 20 | min. hasta la 30 | min. hasta la 40 | min. hasta la 50 | min. hasta la 55 | min. hasta la 60 | min. hasta la 70 | oro hasta la 10 | oro hasta la 30 | oro hasta la 50 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 2.8 | 8.0 | 15.6 | 26.0 | 40.4 | 48.5 | 57.4 | — | 3429 | 67968 | 581530 |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 2.2 | 4.8 | 8.0 | 12.1 | 17.5 | 21.1 | 26.4 | 38.4 | 3220 | 54654 | 441902 |
| 1.6.0 final sin cartas | 1.7 | 4.0 | 7.2 | 12.1 | — | — | — | — | 3233 | 54785 | — |

## Las cartas

### Cartas elegidas al terminar cada oleada (promedio de todas las partidas que la terminan)

| variante | perfil | 1 | 2 | 3 | 4 | 5 | 6 | 8 | 10 | 15 | 20 | 30 | 40 | 50 | 60 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|

### Perfil bajo: cuantas veces tiene cada carta al morir en el muro (promedio de las ultimas 20 partidas)

| variante | cadencia | critico | dano | disparo_doble | dron | escudo | explosiva | furia_larga | granada_rapida | hacia_atras | hielo | iman | perforante | quemadura | vampiro | velocidad | vida | doradas |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 3.7 | 0.5 | 3.8 | 1.9 | 2.0 | 1.0 | 2.0 | 0.7 | 0.9 | 1.0 | 2.0 | 0.6 | 2.0 | 1.9 | 2.0 | 2.0 | 2.9 | 4.2 |

### Perfil medio: cuantas veces tiene cada carta al morir en el muro (promedio de las ultimas 20 partidas)

| variante | cadencia | critico | dano | disparo_doble | dron | escudo | explosiva | furia_larga | granada_rapida | hacia_atras | hielo | iman | perforante | quemadura | vampiro | velocidad | vida | doradas |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 3.9 | 0.7 | 3.9 | 2.0 | 2.0 | 1.0 | 2.0 | 0.7 | 1.3 | 1.0 | 2.0 | 0.6 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 4.8 |

### Perfil alto: cuantas veces tiene cada carta al morir en el muro (promedio de las ultimas 20 partidas)

| variante | cadencia | critico | dano | disparo_doble | dron | escudo | explosiva | furia_larga | granada_rapida | hacia_atras | hielo | iman | perforante | quemadura | vampiro | velocidad | vida | doradas |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 3.9 | 1.0 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 1.2 | 1.3 | 1.0 | 2.0 | 0.9 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 5.4 |
