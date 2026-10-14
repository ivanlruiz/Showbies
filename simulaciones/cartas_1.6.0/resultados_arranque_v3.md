# Simulacion de las cartas de la 1.6.0

partidas 100, semillas 5, perfiles bajo,medio,alto, 424 s

mejor pN: la mejor oleada completada hasta la partida N (mediana de las semillas). muro: la oleada mediana de las ultimas 20 partidas de cada semilla. llega a N: la partida en que se completa por primera vez la oleada N. min. en el muro: lo que dura una de esas partidas, con 3 s por eleccion de carta. % muertes en jefe: de las ultimas 100 partidas de cada semilla.

## El muro

### Perfil bajo

| variante | mejor p10 | mejor p30 | muro (ult. 20) | mejor al final | llega a 40 | a 50 | a 60 | a 80 | al tope (100) | min. en el muro | cartas por partida | % muertes en jefe | arma en el muro | sin balas con blanco | revivio |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 11 | 33 | 28 | 45 | 56 | — | — | — | 0 % | 15.1 | 0 | 16 % | energia | 27 % | — |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 55 | 58 | 57 | 59 | 6 | 8 | — | — | 0 % | 26.2 | 28 | 21 % | energia | 0 % | — |
| V2 de Argon (14/10), equilibrada | 4 | 37 | 49 | 54 | 34 | 49 | — | — | 0 % | 22.2 | 27 | 56 % | energia | 0 % | — |
| V3 de Argon (14/10), equilibrada | 10 | 41 | 48 | 53 | 28 | 71 | — | — | 0 % | 21.5 | 25 | 49 % | energia | 0 % | — |
| V3 de Argon sin cartas | 6 | 15 | 21 | 23 | — | — | — | — | 0 % | 6.6 | 0 | 24 % | energia | 0 % | — |

### Perfil medio

| variante | mejor p10 | mejor p30 | muro (ult. 20) | mejor al final | llega a 40 | a 50 | a 60 | a 80 | al tope (100) | min. en el muro | cartas por partida | % muertes en jefe | arma en el muro | sin balas con blanco | revivio |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 47 | 57 | 56 | 59 | 9 | 13 | — | — | 0 % | 52.0 | 0 | 37 % | energia | 2 % | — |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 61 | 63 | 59 | 64 | 3 | 4 | 7 | — | 0 % | 26.5 | 29 | 67 % | energia | 0 % | — |
| V2 de Argon (14/10), equilibrada | 49 | 64 | 59 | 67 | 10 | 13 | 18 | — | 0 % | 30.4 | 30 | 59 % | energia | 0 % | — |
| V3 de Argon (14/10), equilibrada | 53 | 60 | 59 | 66 | 9 | 10 | 20 | — | 0 % | 29.8 | 28 | 63 % | energia | 0 % | — |
| V3 de Argon sin cartas | 19 | 27 | 29 | 31 | — | — | — | — | 0 % | 9.3 | 0 | 31 % | energia | 0 % | — |

### Perfil alto

| variante | mejor p10 | mejor p30 | muro (ult. 20) | mejor al final | llega a 40 | a 50 | a 60 | a 80 | al tope (100) | min. en el muro | cartas por partida | % muertes en jefe | arma en el muro | sin balas con blanco | revivio |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 67 | 69 | 69 | 71 | 5 | 5 | 8 | — | 0 % | 76.0 | 0 | 58 % | energia | 0 % | — |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 69 | 70 | 67 | 71 | 2 | 3 | 4 | — | 0 % | 33.4 | 30 | 36 % | energia | 0 % | — |
| V2 de Argon (14/10), equilibrada | 71 | 74 | 72 | 77 | 5 | 6 | 6 | — | 0 % | 41.8 | 33 | 33 % | energia | 0 % | — |
| V3 de Argon (14/10), equilibrada | 65 | 73 | 69 | 76 | 4 | 5 | 6 | — | 0 % | 39.7 | 30 | 37 % | energia | 0 % | — |
| V3 de Argon sin cartas | 29 | 36 | 37 | 39 | — | — | — | — | 0 % | 12.9 | 0 | 10 % | energia | 0 % | — |

## El arranque

### Perfil bajo: las primeras 12 partidas (oleada a la que se llega, mediana; entre parentesis el arma con que se arranca)

| variante | p1 | p2 | p3 | p4 | p5 | p6 | p7 | p8 | p9 | p10 | p11 | p12 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 1 (pis) | 2 (pis) | 2 (rev) | 2 (rev) | 3 (rev) | 5 (esc) | 6 (esc) | 9 (esc) | 9 (esc) | 11 (rif) | 19 (rif) | 19 (rif) |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 3 (pis) | 9 (rev) | 9 (esc) | 31 (esc) | 39 (rif) | 49 (ene) | 49 (ene) | 49 (ene) | 49 (ene) | 55 (ene) | 56 (ene) | 55 (ene) |
| V2 de Argon (14/10), equilibrada | 1 (pis) | 1 (pis) | 1 (pis) | 2 (rev) | 2 (rev) | 2 (rev) | 2 (rev) | 2 (rev) | 2 (rev) | 4 (esc) | 4 (esc) | 5 (esc) |
| V3 de Argon (14/10), equilibrada | 2 (pis) | 2 (pis) | 2 (rev) | 2 (rev) | 3 (rev) | 5 (esc) | 6 (esc) | 7 (esc) | 9 (esc) | 10 (esc) | 13 (esc) | 18 (rif) |
| V3 de Argon sin cartas | 1 (pis) | 2 (pis) | 2 (pis) | 2 (rev) | 2 (rev) | 3 (rev) | 5 (esc) | 5 (esc) | 5 (esc) | 6 (esc) | 6 (esc) | 9 (esc) |

### Perfil medio: las primeras 12 partidas (oleada a la que se llega, mediana; entre parentesis el arma con que se arranca)

| variante | p1 | p2 | p3 | p4 | p5 | p6 | p7 | p8 | p9 | p10 | p11 | p12 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 3 (pis) | 3 (rev) | 7 (esc) | 9 (esc) | 19 (esc) | 26 (rif) | 29 (rif) | 39 (rif) | 47 (ene) | 40 (ene) | 49 (ene) | 49 (ene) |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 9 (pis) | 29 (esc) | 49 (rif) | 56 (ene) | 59 (ene) | 58 (ene) | 59 (ene) | 59 (ene) | 58 (ene) | 59 (ene) | 59 (ene) | 59 (ene) |
| V2 de Argon (14/10), equilibrada | 2 (pis) | 3 (pis) | 7 (rev) | 8 (esc) | 12 (esc) | 18 (esc) | 27 (rif) | 29 (rif) | 39 (rif) | 49 (rif) | 49 (ene) | 49 (ene) |
| V3 de Argon (14/10), equilibrada | 3 (pis) | 6 (rev) | 8 (esc) | 14 (esc) | 23 (esc) | 29 (rif) | 31 (rif) | 39 (rif) | 37 (rif) | 53 (rif) | 49 (ene) | 49 (ene) |
| V3 de Argon sin cartas | 3 (pis) | 5 (rev) | 7 (esc) | 9 (esc) | 12 (esc) | 13 (esc) | 16 (rif) | 16 (rif) | 18 (rif) | 17 (rif) | 18 (rif) | 19 (rif) |

### Perfil alto: las primeras 12 partidas (oleada a la que se llega, mediana; entre parentesis el arma con que se arranca)

| variante | p1 | p2 | p3 | p4 | p5 | p6 | p7 | p8 | p9 | p10 | p11 | p12 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 5 (pis) | 12 (esc) | 29 (esc) | 39 (rif) | 55 (ene) | 59 (ene) | 59 (ene) | 62 (ene) | 66 (ene) | 59 (ene) | 65 (ene) | 67 (ene) |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 17 (pis) | 45 (rif) | 59 (ene) | 63 (ene) | 61 (ene) | 67 (ene) | 63 (ene) | 64 (ene) | 63 (ene) | 68 (ene) | 64 (ene) | 59 (ene) |
| V2 de Argon (14/10), equilibrada | 6 (pis) | 12 (rev) | 27 (esc) | 39 (rif) | 49 (rif) | 64 (ene) | 65 (ene) | 67 (ene) | 68 (ene) | 69 (ene) | 69 (ene) | 71 (ene) |
| V3 de Argon (14/10), equilibrada | 8 (pis) | 24 (esc) | 38 (rif) | 49 (rif) | 55 (rif) | 59 (ene) | 60 (ene) | 64 (ene) | 58 (ene) | 59 (ene) | 59 (ene) | 65 (ene) |
| V3 de Argon sin cartas | 5 (pis) | 9 (rev) | 13 (esc) | 20 (esc) | 22 (rif) | 25 (rif) | 25 (rif) | 27 (rif) | 28 (rif) | 28 (rif) | 28 (rif) | 28 (rif) |

## El oro por minuto

Las monedas de la partida (las juntadas, el bono de cada oleada y el tesoro) sobre sus minutos (con 3 s por eleccion de carta), sumadas las de todas las semillas.

### Perfil bajo

| variante | oleada en la p10 | monedas en la p10 | min. de la p10 | oro/min en la p10 | oro/min p8-p12 | oro/min en el muro (ult. 20) | monedas por partida en el muro |
|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 11 | 2012 | 5.3 | 524 | 830 | 4745 | 51897 |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 55 | 733185 | 26.4 | 26950 | 26263 | 32447 | 859313 |
| V2 de Argon (14/10), equilibrada | 4 | 95 | 1.5 | 59 | 60 | 3307 | 75637 |
| V3 de Argon (14/10), equilibrada | 10 | 647 | 4.1 | 187 | 217 | 2147 | 48068 |
| V3 de Argon sin cartas | 6 | 217 | 2.1 | 117 | 123 | 902 | 5979 |

### Perfil medio

| variante | oleada en la p10 | monedas en la p10 | min. de la p10 | oro/min en la p10 | oro/min p8-p12 | oro/min en el muro (ult. 20) | monedas por partida en el muro |
|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 40 | 195821 | 28.1 | 9238 | 10520 | 20809 | 1108401 |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 59 | 1072145 | 26.8 | 42010 | 40790 | 43190 | 1110508 |
| V2 de Argon (14/10), equilibrada | 49 | 56568 | 21.7 | 2196 | 2354 | 5738 | 162235 |
| V3 de Argon (14/10), equilibrada | 53 | 46227 | 26.9 | 1718 | 1693 | 3002 | 90124 |
| V3 de Argon sin cartas | 17 | 2712 | 5.4 | 498 | 490 | 1455 | 13608 |

### Perfil alto

| variante | oleada en la p10 | monedas en la p10 | min. de la p10 | oro/min en la p10 | oro/min p8-p12 | oro/min en el muro (ult. 20) | monedas por partida en el muro |
|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 59 | 1502065 | 56.7 | 33953 | 37368 | 47410 | 3706336 |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 68 | 2577321 | 34.7 | 68801 | 60732 | 65863 | 2268997 |
| V2 de Argon (14/10), equilibrada | 69 | 306259 | 38.0 | 7779 | 7518 | 9044 | 372610 |
| V3 de Argon (14/10), equilibrada | 59 | 90608 | 30.3 | 3053 | 3156 | 3947 | 155656 |
| V3 de Argon sin cartas | 28 | 9394 | 9.2 | 980 | 990 | 1950 | 25427 |

## Los minutos y el oro hasta cada oleada

Al terminar la oleada N, en las partidas que la terminan (mediana de todas, de todas las semillas; "—" con menos de 10).

### Perfil bajo

| variante | min. hasta la 10 | min. hasta la 20 | min. hasta la 30 | min. hasta la 40 | min. hasta la 50 | min. hasta la 55 | min. hasta la 60 | min. hasta la 70 | oro hasta la 10 | oro hasta la 30 | oro hasta la 50 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 2.9 | 8.3 | 16.4 | 28.2 | — | — | — | — | 3188 | 65343 | — |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 2.9 | 6.0 | 9.5 | 14.0 | 20.3 | 24.5 | — | — | 3146 | 52977 | 431123 |
| V2 de Argon (14/10), equilibrada | 2.9 | 6.4 | 10.7 | 15.8 | 23.2 | — | — | — | 1405 | 16112 | 79003 |
| V3 de Argon (14/10), equilibrada | 3.0 | 6.5 | 10.8 | 16.1 | 23.6 | — | — | — | 1636 | 14575 | 53196 |
| V3 de Argon sin cartas | 2.6 | 6.1 | — | — | — | — | — | — | 1324 | — | — |

### Perfil medio

| variante | min. hasta la 10 | min. hasta la 20 | min. hasta la 30 | min. hasta la 40 | min. hasta la 50 | min. hasta la 55 | min. hasta la 60 | min. hasta la 70 | oro hasta la 10 | oro hasta la 30 | oro hasta la 50 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 2.8 | 8.0 | 15.8 | 26.4 | 40.8 | 49.0 | — | — | 3323 | 66746 | 574592 |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 2.2 | 4.8 | 8.0 | 12.3 | 18.0 | 21.8 | 27.9 | — | 3145 | 53458 | 436296 |
| V2 de Argon (14/10), equilibrada | 2.8 | 6.2 | 10.2 | 15.0 | 21.6 | 25.6 | 30.9 | — | 1468 | 16592 | 81089 |
| V3 de Argon (14/10), equilibrada | 2.9 | 6.2 | 10.3 | 15.2 | 21.8 | 25.9 | 31.5 | — | 1701 | 15117 | 55007 |
| V3 de Argon sin cartas | 2.4 | 5.8 | 10.1 | — | — | — | — | — | 1600 | 14606 | — |

### Perfil alto

| variante | min. hasta la 10 | min. hasta la 20 | min. hasta la 30 | min. hasta la 40 | min. hasta la 50 | min. hasta la 55 | min. hasta la 60 | min. hasta la 70 | oro hasta la 10 | oro hasta la 30 | oro hasta la 50 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 2.8 | 8.0 | 15.6 | 26.0 | 40.4 | 48.5 | 57.4 | — | 3429 | 67968 | 581530 |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 2.2 | 4.8 | 8.0 | 12.1 | 17.5 | 21.1 | 26.4 | 38.4 | 3220 | 54654 | 441902 |
| V2 de Argon (14/10), equilibrada | 2.7 | 6.0 | 10.0 | 14.6 | 20.5 | 24.4 | 29.3 | 39.9 | 1507 | 16874 | 82402 |
| V3 de Argon (14/10), equilibrada | 2.8 | 6.1 | 10.0 | 14.7 | 20.8 | 24.7 | 29.8 | 40.2 | 1740 | 15358 | 55931 |
| V3 de Argon sin cartas | 2.4 | 5.4 | 9.5 | — | — | — | — | — | 1709 | 15225 | — |

## Las cartas

### Cartas elegidas al terminar cada oleada (promedio de todas las partidas que la terminan)

| variante | perfil | 1 | 2 | 3 | 4 | 5 | 6 | 8 | 10 | 15 | 20 | 30 | 40 | 50 | 60 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|

### Perfil bajo: cuantas veces tiene cada carta al morir en el muro (promedio de las ultimas 20 partidas)

| variante | cadencia | critico | dano | disparo_doble | dron | escudo | explosiva | furia_larga | granada_rapida | hacia_atras | hielo | iman | perforante | quemadura | vampiro | velocidad | vida | doradas |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 3.7 | 0.5 | 3.8 | 1.9 | 2.0 | 1.0 | 2.0 | 0.7 | 0.9 | 1.0 | 2.0 | 0.6 | 2.0 | 1.9 | 2.0 | 2.0 | 2.9 | 4.2 |
| V2 de Argon (14/10), equilibrada | 3.4 | 0.1 | 3.4 | 1.9 | 1.8 | 1.0 | 1.9 | 0.5 | 0.7 | 1.0 | 1.9 | 0.2 | 2.0 | 1.8 | 1.9 | 1.9 | 2.8 | 3.8 |
| V3 de Argon (14/10), equilibrada | 3.0 | 0.1 | 3.3 | 1.9 | 1.8 | 1.0 | 1.8 | 0.2 | 0.4 | 0.9 | 1.7 | 0.1 | 1.9 | 1.6 | 1.9 | 1.8 | 2.8 | 3.4 |

### Perfil medio: cuantas veces tiene cada carta al morir en el muro (promedio de las ultimas 20 partidas)

| variante | cadencia | critico | dano | disparo_doble | dron | escudo | explosiva | furia_larga | granada_rapida | hacia_atras | hielo | iman | perforante | quemadura | vampiro | velocidad | vida | doradas |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 3.9 | 0.7 | 3.9 | 2.0 | 2.0 | 1.0 | 2.0 | 0.7 | 1.3 | 1.0 | 2.0 | 0.6 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 4.8 |
| V2 de Argon (14/10), equilibrada | 4.0 | 0.8 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 1.3 | 1.6 | 1.0 | 2.0 | 0.8 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 5.2 |
| V3 de Argon (14/10), equilibrada | 3.8 | 0.4 | 3.8 | 2.0 | 2.0 | 1.0 | 1.9 | 0.5 | 0.8 | 1.0 | 1.9 | 0.3 | 2.0 | 1.9 | 2.0 | 1.9 | 2.9 | 4.5 |

### Perfil alto: cuantas veces tiene cada carta al morir en el muro (promedio de las ultimas 20 partidas)

| variante | cadencia | critico | dano | disparo_doble | dron | escudo | explosiva | furia_larga | granada_rapida | hacia_atras | hielo | iman | perforante | quemadura | vampiro | velocidad | vida | doradas |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 3.9 | 1.0 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 1.2 | 1.3 | 1.0 | 2.0 | 0.9 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 5.4 |
| V2 de Argon (14/10), equilibrada | 4.0 | 2.4 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 1.8 | 1.9 | 1.0 | 2.0 | 1.4 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 6.4 |
| V3 de Argon (14/10), equilibrada | 3.9 | 1.0 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 1.3 | 1.3 | 1.0 | 1.9 | 1.0 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 5.6 |
