# Simulacion de las cartas de la 1.6.0

partidas 100, semillas 5, perfiles bajo,medio,alto, 355 s

mejor pN: la mejor oleada completada hasta la partida N (mediana de las semillas). muro: la oleada mediana de las ultimas 20 partidas de cada semilla. llega a N: la partida en que se completa por primera vez la oleada N. min. en el muro: lo que dura una de esas partidas, con 3 s por eleccion de carta. % muertes en jefe: de las ultimas 100 partidas de cada semilla.

## El muro

### Perfil bajo

| variante | mejor p10 | mejor p30 | muro (ult. 20) | mejor al final | llega a 40 | a 50 | a 60 | a 80 | al tope (100) | min. en el muro | cartas por partida | % muertes en jefe | arma en el muro | sin balas con blanco | revivio |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 11 | 33 | 28 | 45 | 56 | — | — | — | 0 % | 15.1 | 0 | 16 % | energia | 27 % | — |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 55 | 58 | 57 | 59 | 6 | 8 | — | — | 0 % | 26.2 | 28 | 21 % | energia | 0 % | — |
| arranque de Argon (14/10), equilibrada | 23 | 47 | 43 | 49 | 17 | — | — | — | 0 % | 17.6 | 20 | 36 % | energia | 0 % | — |
| V2 de Argon (14/10), equilibrada | 4 | 37 | 49 | 54 | 34 | 49 | — | — | 0 % | 22.2 | 27 | 56 % | energia | 0 % | — |
| V2 de Argon sin cartas | 5 | 11 | 19 | 22 | — | — | — | — | 0 % | 6.1 | 0 | 25 % | energia | 0 % | — |

### Perfil medio

| variante | mejor p10 | mejor p30 | muro (ult. 20) | mejor al final | llega a 40 | a 50 | a 60 | a 80 | al tope (100) | min. en el muro | cartas por partida | % muertes en jefe | arma en el muro | sin balas con blanco | revivio |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 47 | 57 | 56 | 59 | 9 | 13 | — | — | 0 % | 52.0 | 0 | 37 % | energia | 2 % | — |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 61 | 63 | 59 | 64 | 3 | 4 | 7 | — | 0 % | 26.5 | 29 | 67 % | energia | 0 % | — |
| arranque de Argon (14/10), equilibrada | 49 | 58 | 52 | 58 | 6 | 13 | — | — | 0 % | 23.5 | 22 | 36 % | energia | 1 % | — |
| V2 de Argon (14/10), equilibrada | 49 | 64 | 59 | 67 | 10 | 13 | 18 | — | 0 % | 30.4 | 30 | 59 % | energia | 0 % | — |
| V2 de Argon sin cartas | 16 | 28 | 29 | 32 | — | — | — | — | 0 % | 9.6 | 0 | 46 % | energia | 0 % | — |

### Perfil alto

| variante | mejor p10 | mejor p30 | muro (ult. 20) | mejor al final | llega a 40 | a 50 | a 60 | a 80 | al tope (100) | min. en el muro | cartas por partida | % muertes en jefe | arma en el muro | sin balas con blanco | revivio |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 67 | 69 | 69 | 71 | 5 | 5 | 8 | — | 0 % | 76.0 | 0 | 58 % | energia | 0 % | — |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 69 | 70 | 67 | 71 | 2 | 3 | 4 | — | 0 % | 33.4 | 30 | 36 % | energia | 0 % | — |
| arranque de Argon (14/10), equilibrada | 59 | 62 | 57 | 65 | 3 | 4 | 13 | — | 0 % | 26.7 | 22 | 26 % | energia | 2 % | — |
| V2 de Argon (14/10), equilibrada | 71 | 74 | 72 | 77 | 5 | 6 | 6 | — | 0 % | 41.8 | 33 | 33 % | energia | 0 % | — |
| V2 de Argon sin cartas | 28 | 37 | 38 | 39 | — | — | — | — | 0 % | 13.3 | 0 | 22 % | energia | 0 % | — |

## El arranque

### Perfil bajo: las primeras 12 partidas (oleada a la que se llega, mediana; entre parentesis el arma con que se arranca)

| variante | p1 | p2 | p3 | p4 | p5 | p6 | p7 | p8 | p9 | p10 | p11 | p12 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 1 (pis) | 2 (pis) | 2 (rev) | 2 (rev) | 3 (rev) | 5 (esc) | 6 (esc) | 9 (esc) | 9 (esc) | 11 (rif) | 19 (rif) | 19 (rif) |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 3 (pis) | 9 (rev) | 9 (esc) | 31 (esc) | 39 (rif) | 49 (ene) | 49 (ene) | 49 (ene) | 49 (ene) | 55 (ene) | 56 (ene) | 55 (ene) |
| arranque de Argon (14/10), equilibrada | 1 (pis) | 2 (pis) | 2 (rev) | 2 (rev) | 4 (rev) | 4 (esc) | 7 (esc) | 12 (esc) | 17 (rif) | 23 (rif) | 31 (rif) | 34 (rif) |
| V2 de Argon (14/10), equilibrada | 1 (pis) | 1 (pis) | 1 (pis) | 2 (rev) | 2 (rev) | 2 (rev) | 2 (rev) | 2 (rev) | 2 (rev) | 4 (esc) | 4 (esc) | 5 (esc) |
| V2 de Argon sin cartas | 1 (pis) | 1 (pis) | 1 (pis) | 2 (rev) | 2 (rev) | 2 (rev) | 2 (rev) | 2 (rev) | 4 (esc) | 4 (esc) | 5 (esc) | 4 (esc) |

### Perfil medio: las primeras 12 partidas (oleada a la que se llega, mediana; entre parentesis el arma con que se arranca)

| variante | p1 | p2 | p3 | p4 | p5 | p6 | p7 | p8 | p9 | p10 | p11 | p12 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 3 (pis) | 3 (rev) | 7 (esc) | 9 (esc) | 19 (esc) | 26 (rif) | 29 (rif) | 39 (rif) | 47 (ene) | 40 (ene) | 49 (ene) | 49 (ene) |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 9 (pis) | 29 (esc) | 49 (rif) | 56 (ene) | 59 (ene) | 58 (ene) | 59 (ene) | 59 (ene) | 58 (ene) | 59 (ene) | 59 (ene) | 59 (ene) |
| arranque de Argon (14/10), equilibrada | 3 (pis) | 9 (rev) | 24 (esc) | 37 (rif) | 39 (rif) | 49 (ene) | 49 (ene) | 44 (ene) | 49 (ene) | 48 (ene) | 49 (ene) | 48 (ene) |
| V2 de Argon (14/10), equilibrada | 2 (pis) | 3 (pis) | 7 (rev) | 8 (esc) | 12 (esc) | 18 (esc) | 27 (rif) | 29 (rif) | 39 (rif) | 49 (rif) | 49 (ene) | 49 (ene) |
| V2 de Argon sin cartas | 3 (pis) | 4 (rev) | 5 (rev) | 6 (esc) | 9 (esc) | 9 (esc) | 11 (esc) | 15 (esc) | 14 (rif) | 15 (rif) | 15 (rif) | 17 (rif) |

### Perfil alto: las primeras 12 partidas (oleada a la que se llega, mediana; entre parentesis el arma con que se arranca)

| variante | p1 | p2 | p3 | p4 | p5 | p6 | p7 | p8 | p9 | p10 | p11 | p12 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 5 (pis) | 12 (esc) | 29 (esc) | 39 (rif) | 55 (ene) | 59 (ene) | 59 (ene) | 62 (ene) | 66 (ene) | 59 (ene) | 65 (ene) | 67 (ene) |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 17 (pis) | 45 (rif) | 59 (ene) | 63 (ene) | 61 (ene) | 67 (ene) | 63 (ene) | 64 (ene) | 63 (ene) | 68 (ene) | 64 (ene) | 59 (ene) |
| arranque de Argon (14/10), equilibrada | 8 (pis) | 26 (esc) | 40 (rif) | 51 (ene) | 47 (ene) | 51 (ene) | 52 (ene) | 49 (ene) | 53 (ene) | 53 (ene) | 55 (ene) | 51 (ene) |
| V2 de Argon (14/10), equilibrada | 6 (pis) | 12 (rev) | 27 (esc) | 39 (rif) | 49 (rif) | 64 (ene) | 65 (ene) | 67 (ene) | 68 (ene) | 69 (ene) | 69 (ene) | 71 (ene) |
| V2 de Argon sin cartas | 5 (pis) | 8 (rev) | 12 (esc) | 17 (esc) | 19 (rif) | 22 (rif) | 23 (rif) | 24 (rif) | 27 (rif) | 28 (rif) | 28 (rif) | 29 (rif) |

## El oro por minuto

Las monedas de la partida (las juntadas, el bono de cada oleada y el tesoro) sobre sus minutos (con 3 s por eleccion de carta), sumadas las de todas las semillas.

### Perfil bajo

| variante | oleada en la p10 | monedas en la p10 | min. de la p10 | oro/min en la p10 | oro/min p8-p12 | oro/min en el muro (ult. 20) | monedas por partida en el muro |
|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 11 | 2012 | 5.3 | 524 | 830 | 4745 | 51897 |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 55 | 733185 | 26.4 | 26950 | 26263 | 32447 | 859313 |
| arranque de Argon (14/10), equilibrada | 23 | 12626 | 9.0 | 2010 | 3665 | 13809 | 229355 |
| V2 de Argon (14/10), equilibrada | 4 | 95 | 1.5 | 59 | 60 | 3307 | 75637 |
| V2 de Argon sin cartas | 4 | 94 | 1.5 | 62 | 63 | 807 | 4855 |

### Perfil medio

| variante | oleada en la p10 | monedas en la p10 | min. de la p10 | oro/min en la p10 | oro/min p8-p12 | oro/min en el muro (ult. 20) | monedas por partida en el muro |
|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 40 | 195821 | 28.1 | 9238 | 10520 | 20809 | 1108401 |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 59 | 1072145 | 26.8 | 42010 | 40790 | 43190 | 1110508 |
| arranque de Argon (14/10), equilibrada | 48 | 362424 | 20.4 | 18854 | 18198 | 24508 | 534883 |
| V2 de Argon (14/10), equilibrada | 49 | 56568 | 21.7 | 2196 | 2354 | 5738 | 162235 |
| V2 de Argon sin cartas | 15 | 1711 | 4.8 | 364 | 363 | 1656 | 15639 |

### Perfil alto

| variante | oleada en la p10 | monedas en la p10 | min. de la p10 | oro/min en la p10 | oro/min p8-p12 | oro/min en el muro (ult. 20) | monedas por partida en el muro |
|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 59 | 1502065 | 56.7 | 33953 | 37368 | 47410 | 3706336 |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 68 | 2577321 | 34.7 | 68801 | 60732 | 65863 | 2268997 |
| arranque de Argon (14/10), equilibrada | 53 | 648418 | 23.3 | 29845 | 27612 | 34108 | 905783 |
| V2 de Argon (14/10), equilibrada | 69 | 306259 | 38.0 | 7779 | 7518 | 9044 | 372610 |
| V2 de Argon sin cartas | 28 | 9119 | 8.8 | 1019 | 1028 | 2548 | 33945 |

## Los minutos y el oro hasta cada oleada

Al terminar la oleada N, en las partidas que la terminan (mediana de todas, de todas las semillas; "—" con menos de 10).

### Perfil bajo

| variante | min. hasta la 10 | min. hasta la 20 | min. hasta la 30 | min. hasta la 40 | min. hasta la 50 | min. hasta la 55 | min. hasta la 60 | min. hasta la 70 | oro hasta la 10 | oro hasta la 30 | oro hasta la 50 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 2.9 | 8.3 | 16.4 | 28.2 | — | — | — | — | 3188 | 65343 | — |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 2.9 | 6.0 | 9.5 | 14.0 | 20.3 | 24.5 | — | — | 3146 | 52977 | 431123 |
| arranque de Argon (14/10), equilibrada | 2.9 | 6.3 | 10.4 | 15.3 | — | — | — | — | 3147 | 53289 | — |
| V2 de Argon (14/10), equilibrada | 2.9 | 6.4 | 10.7 | 15.8 | 23.2 | — | — | — | 1405 | 16112 | 79003 |
| V2 de Argon sin cartas | 2.8 | 6.1 | — | — | — | — | — | — | 1072 | — | — |

### Perfil medio

| variante | min. hasta la 10 | min. hasta la 20 | min. hasta la 30 | min. hasta la 40 | min. hasta la 50 | min. hasta la 55 | min. hasta la 60 | min. hasta la 70 | oro hasta la 10 | oro hasta la 30 | oro hasta la 50 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 2.8 | 8.0 | 15.8 | 26.4 | 40.8 | 49.0 | — | — | 3323 | 66746 | 574592 |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 2.2 | 4.8 | 8.0 | 12.3 | 18.0 | 21.8 | 27.9 | — | 3145 | 53458 | 436296 |
| arranque de Argon (14/10), equilibrada | 2.8 | 6.1 | 10.0 | 14.7 | 21.4 | 25.4 | — | — | 3229 | 54358 | 437534 |
| V2 de Argon (14/10), equilibrada | 2.8 | 6.2 | 10.2 | 15.0 | 21.6 | 25.6 | 30.9 | — | 1468 | 16592 | 81089 |
| V2 de Argon sin cartas | 2.5 | 5.7 | 10.1 | — | — | — | — | — | 1380 | 16096 | — |

### Perfil alto

| variante | min. hasta la 10 | min. hasta la 20 | min. hasta la 30 | min. hasta la 40 | min. hasta la 50 | min. hasta la 55 | min. hasta la 60 | min. hasta la 70 | oro hasta la 10 | oro hasta la 30 | oro hasta la 50 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 2.8 | 8.0 | 15.6 | 26.0 | 40.4 | 48.5 | 57.4 | — | 3429 | 67968 | 581530 |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 2.2 | 4.8 | 8.0 | 12.1 | 17.5 | 21.1 | 26.4 | 38.4 | 3220 | 54654 | 441902 |
| arranque de Argon (14/10), equilibrada | 2.8 | 6.0 | 9.8 | 14.4 | 20.4 | 24.4 | 29.9 | — | 3293 | 55213 | 443414 |
| V2 de Argon (14/10), equilibrada | 2.7 | 6.0 | 10.0 | 14.6 | 20.5 | 24.4 | 29.3 | 39.9 | 1507 | 16874 | 82402 |
| V2 de Argon sin cartas | 2.4 | 5.4 | 9.3 | — | — | — | — | — | 1478 | 16779 | — |

## Las cartas

### Cartas elegidas al terminar cada oleada (promedio de todas las partidas que la terminan)

| variante | perfil | 1 | 2 | 3 | 4 | 5 | 6 | 8 | 10 | 15 | 20 | 30 | 40 | 50 | 60 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|

### Perfil bajo: cuantas veces tiene cada carta al morir en el muro (promedio de las ultimas 20 partidas)

| variante | cadencia | critico | dano | disparo_doble | dron | escudo | explosiva | furia_larga | granada_rapida | hacia_atras | hielo | iman | perforante | quemadura | vampiro | velocidad | vida | doradas |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 3.7 | 0.5 | 3.8 | 1.9 | 2.0 | 1.0 | 2.0 | 0.7 | 0.9 | 1.0 | 2.0 | 0.6 | 2.0 | 1.9 | 2.0 | 2.0 | 2.9 | 4.2 |
| arranque de Argon (14/10), equilibrada | 2.1 | 0.0 | 2.5 | 1.6 | 1.6 | 0.9 | 1.6 | 0.1 | 0.2 | 0.8 | 1.4 | 0.0 | 1.6 | 1.2 | 1.6 | 1.6 | 2.5 | 3.0 |
| V2 de Argon (14/10), equilibrada | 3.4 | 0.1 | 3.4 | 1.9 | 1.8 | 1.0 | 1.9 | 0.5 | 0.7 | 1.0 | 1.9 | 0.2 | 2.0 | 1.8 | 1.9 | 1.9 | 2.8 | 3.8 |

### Perfil medio: cuantas veces tiene cada carta al morir en el muro (promedio de las ultimas 20 partidas)

| variante | cadencia | critico | dano | disparo_doble | dron | escudo | explosiva | furia_larga | granada_rapida | hacia_atras | hielo | iman | perforante | quemadura | vampiro | velocidad | vida | doradas |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 3.9 | 0.7 | 3.9 | 2.0 | 2.0 | 1.0 | 2.0 | 0.7 | 1.3 | 1.0 | 2.0 | 0.6 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 4.8 |
| arranque de Argon (14/10), equilibrada | 2.6 | 0.1 | 2.8 | 1.7 | 1.6 | 0.9 | 1.8 | 0.1 | 0.3 | 0.9 | 1.5 | 0.1 | 1.8 | 1.4 | 1.8 | 1.6 | 2.5 | 3.7 |
| V2 de Argon (14/10), equilibrada | 4.0 | 0.8 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 1.3 | 1.6 | 1.0 | 2.0 | 0.8 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 5.2 |

### Perfil alto: cuantas veces tiene cada carta al morir en el muro (promedio de las ultimas 20 partidas)

| variante | cadencia | critico | dano | disparo_doble | dron | escudo | explosiva | furia_larga | granada_rapida | hacia_atras | hielo | iman | perforante | quemadura | vampiro | velocidad | vida | doradas |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 3.9 | 1.0 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 1.2 | 1.3 | 1.0 | 2.0 | 0.9 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 5.4 |
| arranque de Argon (14/10), equilibrada | 2.9 | 0.1 | 2.9 | 1.8 | 1.8 | 0.9 | 1.8 | 0.2 | 0.3 | 0.9 | 1.7 | 0.1 | 1.8 | 1.5 | 1.7 | 1.8 | 2.4 | 4.0 |
| V2 de Argon (14/10), equilibrada | 4.0 | 2.4 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 1.8 | 1.9 | 1.0 | 2.0 | 1.4 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 6.4 |
