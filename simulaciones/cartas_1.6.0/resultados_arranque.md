# Simulacion de las cartas de la 1.6.0

partidas 100, semillas 5, perfiles bajo,medio,alto, 462 s

mejor pN: la mejor oleada completada hasta la partida N (mediana de las semillas). muro: la oleada mediana de las ultimas 20 partidas de cada semilla. llega a N: la partida en que se completa por primera vez la oleada N. min. en el muro: lo que dura una de esas partidas, con 3 s por eleccion de carta. % muertes en jefe: de las ultimas 100 partidas de cada semilla.

## El muro

### Perfil bajo

| variante | mejor p10 | mejor p30 | muro (ult. 20) | mejor al final | llega a 40 | a 50 | a 60 | a 80 | al tope (100) | min. en el muro | cartas por partida | % muertes en jefe | arma en el muro | sin balas con blanco | revivio |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 11 | 33 | 28 | 45 | 56 | — | — | — | 0 % | 15.1 | 0 | 16 % | energia | 27 % | — |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 55 | 58 | 57 | 59 | 6 | 8 | — | — | 0 % | 26.2 | 28 | 21 % | energia | 0 % | — |
| arranque de Argon (14/10), equilibrada | 23 | 47 | 43 | 49 | 17 | — | — | — | 0 % | 17.6 | 20 | 36 % | energia | 0 % | — |
| arranque de Argon sin cartas | 9 | 19 | 24 | 27 | — | — | — | — | 0 % | 7.9 | 0 | 9 % | energia | 0 % | — |
| la final con el automatico de Argon, equilibrada | 49 | 55 | 53 | 56 | 8 | 13 | — | — | 0 % | 24.8 | 28 | 44 % | energia | 0 % | — |
| arranque de Argon con el automatico de hoy, equilibrada | 46 | 49 | 46 | 54 | 10 | 67 | — | — | 0 % | 18.6 | 20 | 37 % | energia | 2 % | — |

### Perfil medio

| variante | mejor p10 | mejor p30 | muro (ult. 20) | mejor al final | llega a 40 | a 50 | a 60 | a 80 | al tope (100) | min. en el muro | cartas por partida | % muertes en jefe | arma en el muro | sin balas con blanco | revivio |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 47 | 57 | 56 | 59 | 9 | 13 | — | — | 0 % | 52.0 | 0 | 37 % | energia | 2 % | — |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 61 | 63 | 59 | 64 | 3 | 4 | 7 | — | 0 % | 26.5 | 29 | 67 % | energia | 0 % | — |
| arranque de Argon (14/10), equilibrada | 49 | 58 | 52 | 58 | 6 | 13 | — | — | 0 % | 23.5 | 22 | 36 % | energia | 1 % | — |
| arranque de Argon sin cartas | 25 | 32 | 32 | 34 | — | — | — | — | 0 % | 10.9 | 0 | 22 % | energia | 0 % | — |
| la final con el automatico de Argon, equilibrada | 59 | 61 | 59 | 63 | 4 | 5 | 15 | — | 0 % | 29.4 | 29 | 66 % | energia | 0 % | — |
| arranque de Argon con el automatico de hoy, equilibrada | 51 | 58 | 49 | 59 | 4 | 6 | — | — | 0 % | 18.8 | 21 | 29 % | energia | 3 % | — |

### Perfil alto

| variante | mejor p10 | mejor p30 | muro (ult. 20) | mejor al final | llega a 40 | a 50 | a 60 | a 80 | al tope (100) | min. en el muro | cartas por partida | % muertes en jefe | arma en el muro | sin balas con blanco | revivio |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 67 | 69 | 69 | 71 | 5 | 5 | 8 | — | 0 % | 76.0 | 0 | 58 % | energia | 0 % | — |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 69 | 70 | 67 | 71 | 2 | 3 | 4 | — | 0 % | 33.4 | 30 | 36 % | energia | 0 % | — |
| arranque de Argon (14/10), equilibrada | 59 | 62 | 57 | 65 | 3 | 4 | 13 | — | 0 % | 26.7 | 22 | 26 % | energia | 2 % | — |
| arranque de Argon sin cartas | 35 | 39 | 38 | 40 | 77 | — | — | — | 0 % | 13.3 | 0 | 27 % | energia | 0 % | — |
| la final con el automatico de Argon, equilibrada | 66 | 69 | 66 | 70 | 2 | 3 | 5 | — | 0 % | 35.8 | 30 | 33 % | energia | 0 % | — |
| arranque de Argon con el automatico de hoy, equilibrada | 59 | 63 | 56 | 65 | 4 | 5 | 15 | — | 0 % | 23.0 | 22 | 23 % | energia | 4 % | — |

## El arranque

### Perfil bajo: las primeras 12 partidas (oleada a la que se llega, mediana; entre parentesis el arma con que se arranca)

| variante | p1 | p2 | p3 | p4 | p5 | p6 | p7 | p8 | p9 | p10 | p11 | p12 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 1 (pis) | 2 (pis) | 2 (rev) | 2 (rev) | 3 (rev) | 5 (esc) | 6 (esc) | 9 (esc) | 9 (esc) | 11 (rif) | 19 (rif) | 19 (rif) |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 3 (pis) | 9 (rev) | 9 (esc) | 31 (esc) | 39 (rif) | 49 (ene) | 49 (ene) | 49 (ene) | 49 (ene) | 55 (ene) | 56 (ene) | 55 (ene) |
| arranque de Argon (14/10), equilibrada | 1 (pis) | 2 (pis) | 2 (rev) | 2 (rev) | 4 (rev) | 4 (esc) | 7 (esc) | 12 (esc) | 17 (rif) | 23 (rif) | 31 (rif) | 34 (rif) |
| arranque de Argon sin cartas | 1 (pis) | 1 (pis) | 2 (pis) | 2 (rev) | 3 (rev) | 4 (esc) | 5 (esc) | 6 (esc) | 8 (esc) | 9 (esc) | 9 (esc) | 9 (rif) |
| la final con el automatico de Argon, equilibrada | 2 (pis) | 2 (rev) | 3 (rev) | 9 (rev) | 14 (esc) | 32 (esc) | 39 (rif) | 43 (ene) | 49 (ene) | 49 (ene) | 49 (ene) | 49 (ene) |
| arranque de Argon con el automatico de hoy, equilibrada | 2 (pis) | 4 (rev) | 6 (rev) | 9 (esc) | 16 (esc) | 26 (rif) | 39 (rif) | 38 (ene) | 39 (ene) | 41 (ene) | 43 (ene) | 39 (ene) |

### Perfil medio: las primeras 12 partidas (oleada a la que se llega, mediana; entre parentesis el arma con que se arranca)

| variante | p1 | p2 | p3 | p4 | p5 | p6 | p7 | p8 | p9 | p10 | p11 | p12 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 3 (pis) | 3 (rev) | 7 (esc) | 9 (esc) | 19 (esc) | 26 (rif) | 29 (rif) | 39 (rif) | 47 (ene) | 40 (ene) | 49 (ene) | 49 (ene) |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 9 (pis) | 29 (esc) | 49 (rif) | 56 (ene) | 59 (ene) | 58 (ene) | 59 (ene) | 59 (ene) | 58 (ene) | 59 (ene) | 59 (ene) | 59 (ene) |
| arranque de Argon (14/10), equilibrada | 3 (pis) | 9 (rev) | 24 (esc) | 37 (rif) | 39 (rif) | 49 (ene) | 49 (ene) | 44 (ene) | 49 (ene) | 48 (ene) | 49 (ene) | 48 (ene) |
| arranque de Argon sin cartas | 3 (pis) | 4 (rev) | 7 (esc) | 12 (esc) | 14 (esc) | 17 (rif) | 19 (rif) | 22 (rif) | 24 (rif) | 23 (rif) | 27 (rif) | 28 (ene) |
| la final con el automatico de Argon, equilibrada | 3 (pis) | 9 (rev) | 37 (esc) | 49 (rif) | 56 (ene) | 58 (ene) | 59 (ene) | 57 (ene) | 57 (ene) | 59 (ene) | 59 (ene) | 59 (ene) |
| arranque de Argon con el automatico de hoy, equilibrada | 6 (pis) | 17 (esc) | 32 (rif) | 42 (rif) | 49 (ene) | 45 (ene) | 48 (ene) | 49 (ene) | 49 (ene) | 49 (ene) | 54 (ene) | 48 (ene) |

### Perfil alto: las primeras 12 partidas (oleada a la que se llega, mediana; entre parentesis el arma con que se arranca)

| variante | p1 | p2 | p3 | p4 | p5 | p6 | p7 | p8 | p9 | p10 | p11 | p12 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 5 (pis) | 12 (esc) | 29 (esc) | 39 (rif) | 55 (ene) | 59 (ene) | 59 (ene) | 62 (ene) | 66 (ene) | 59 (ene) | 65 (ene) | 67 (ene) |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 17 (pis) | 45 (rif) | 59 (ene) | 63 (ene) | 61 (ene) | 67 (ene) | 63 (ene) | 64 (ene) | 63 (ene) | 68 (ene) | 64 (ene) | 59 (ene) |
| arranque de Argon (14/10), equilibrada | 8 (pis) | 26 (esc) | 40 (rif) | 51 (ene) | 47 (ene) | 51 (ene) | 52 (ene) | 49 (ene) | 53 (ene) | 53 (ene) | 55 (ene) | 51 (ene) |
| arranque de Argon sin cartas | 5 (pis) | 12 (esc) | 19 (esc) | 25 (rif) | 29 (rif) | 29 (rif) | 30 (ene) | 33 (ene) | 34 (ene) | 35 (ene) | 36 (ene) | 36 (ene) |
| la final con el automatico de Argon, equilibrada | 9 (pis) | 41 (esc) | 57 (rif) | 59 (ene) | 62 (ene) | 59 (ene) | 63 (ene) | 66 (ene) | 66 (ene) | 66 (ene) | 62 (ene) | 65 (ene) |
| arranque de Argon con el automatico de hoy, equilibrada | 9 (pis) | 28 (esc) | 39 (rif) | 49 (ene) | 56 (ene) | 51 (ene) | 54 (ene) | 52 (ene) | 58 (ene) | 59 (ene) | 56 (ene) | 54 (ene) |

## El oro por minuto

Las monedas de la partida (las juntadas, el bono de cada oleada y el tesoro) sobre sus minutos (con 3 s por eleccion de carta), sumadas las de todas las semillas.

### Perfil bajo

| variante | oleada en la p10 | monedas en la p10 | min. de la p10 | oro/min en la p10 | oro/min p8-p12 | oro/min en el muro (ult. 20) | monedas por partida en el muro |
|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 11 | 2012 | 5.3 | 524 | 830 | 4745 | 51897 |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 55 | 733185 | 26.4 | 26950 | 26263 | 32447 | 859313 |
| arranque de Argon (14/10), equilibrada | 23 | 12626 | 9.0 | 2010 | 3665 | 13809 | 229355 |
| arranque de Argon sin cartas | 9 | 878 | 2.7 | 324 | 327 | 3581 | 27724 |
| la final con el automatico de Argon, equilibrada | 49 | 396779 | 21.7 | 18818 | 17062 | 23639 | 606094 |
| arranque de Argon con el automatico de hoy, equilibrada | 41 | 181387 | 15.5 | 11396 | 10653 | 16842 | 315216 |

### Perfil medio

| variante | oleada en la p10 | monedas en la p10 | min. de la p10 | oro/min en la p10 | oro/min p8-p12 | oro/min en el muro (ult. 20) | monedas por partida en el muro |
|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 40 | 195821 | 28.1 | 9238 | 10520 | 20809 | 1108401 |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 59 | 1072145 | 26.8 | 42010 | 40790 | 43190 | 1110508 |
| arranque de Argon (14/10), equilibrada | 48 | 362424 | 20.4 | 18854 | 18198 | 24508 | 534883 |
| arranque de Argon sin cartas | 23 | 17786 | 7.3 | 2636 | 2823 | 6547 | 71101 |
| la final con el automatico de Argon, equilibrada | 59 | 1045455 | 29.3 | 35307 | 33610 | 37359 | 1097943 |
| arranque de Argon con el automatico de hoy, equilibrada | 49 | 409992 | 18.4 | 24376 | 23898 | 26312 | 436289 |

### Perfil alto

| variante | oleada en la p10 | monedas en la p10 | min. de la p10 | oro/min en la p10 | oro/min p8-p12 | oro/min en el muro (ult. 20) | monedas por partida en el muro |
|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 59 | 1502065 | 56.7 | 33953 | 37368 | 47410 | 3706336 |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 68 | 2577321 | 34.7 | 68801 | 60732 | 65863 | 2268997 |
| arranque de Argon (14/10), equilibrada | 53 | 648418 | 23.3 | 29845 | 27612 | 34108 | 905783 |
| arranque de Argon sin cartas | 35 | 90269 | 11.8 | 8144 | 7623 | 10608 | 142875 |
| la final con el automatico de Argon, equilibrada | 66 | 2032991 | 35.7 | 59522 | 55403 | 59955 | 2124941 |
| arranque de Argon con el automatico de hoy, equilibrada | 59 | 1073697 | 25.5 | 40651 | 34326 | 36489 | 824781 |

## Los minutos y el oro hasta cada oleada

Al terminar la oleada N, en las partidas que la terminan (mediana de todas, de todas las semillas; "—" con menos de 10).

### Perfil bajo

| variante | min. hasta la 10 | min. hasta la 20 | min. hasta la 30 | min. hasta la 40 | min. hasta la 50 | min. hasta la 55 | min. hasta la 60 | min. hasta la 70 | oro hasta la 10 | oro hasta la 30 | oro hasta la 50 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 2.9 | 8.3 | 16.4 | 28.2 | — | — | — | — | 3188 | 65343 | — |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 2.9 | 6.0 | 9.5 | 14.0 | 20.3 | 24.5 | — | — | 3146 | 52977 | 431123 |
| arranque de Argon (14/10), equilibrada | 2.9 | 6.3 | 10.4 | 15.3 | — | — | — | — | 3147 | 53289 | — |
| arranque de Argon sin cartas | 2.6 | 6.0 | — | — | — | — | — | — | 3000 | — | — |
| la final con el automatico de Argon, equilibrada | 3.0 | 6.4 | 10.4 | 15.2 | 22.0 | 26.0 | — | — | 3157 | 53353 | 430606 |
| arranque de Argon con el automatico de hoy, equilibrada | 2.8 | 6.0 | 9.9 | 14.7 | 21.7 | — | — | — | 3143 | 53138 | 432787 |

### Perfil medio

| variante | min. hasta la 10 | min. hasta la 20 | min. hasta la 30 | min. hasta la 40 | min. hasta la 50 | min. hasta la 55 | min. hasta la 60 | min. hasta la 70 | oro hasta la 10 | oro hasta la 30 | oro hasta la 50 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 2.8 | 8.0 | 15.8 | 26.4 | 40.8 | 49.0 | — | — | 3323 | 66746 | 574592 |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 2.2 | 4.8 | 8.0 | 12.3 | 18.0 | 21.8 | 27.9 | — | 3145 | 53458 | 436296 |
| arranque de Argon (14/10), equilibrada | 2.8 | 6.1 | 10.0 | 14.7 | 21.4 | 25.4 | — | — | 3229 | 54358 | 437534 |
| arranque de Argon sin cartas | 2.4 | 5.6 | 9.7 | — | — | — | — | — | 3204 | 54077 | — |
| la final con el automatico de Argon, equilibrada | 2.9 | 6.2 | 10.1 | 14.8 | 21.0 | 25.1 | 31.0 | — | 3234 | 54267 | 437719 |
| arranque de Argon con el automatico de hoy, equilibrada | 2.1 | 4.6 | 8.0 | 12.7 | 18.9 | 23.1 | — | — | 3140 | 53638 | 437191 |

### Perfil alto

| variante | min. hasta la 10 | min. hasta la 20 | min. hasta la 30 | min. hasta la 40 | min. hasta la 50 | min. hasta la 55 | min. hasta la 60 | min. hasta la 70 | oro hasta la 10 | oro hasta la 30 | oro hasta la 50 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 2.8 | 8.0 | 15.6 | 26.0 | 40.4 | 48.5 | 57.4 | — | 3429 | 67968 | 581530 |
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 2.2 | 4.8 | 8.0 | 12.1 | 17.5 | 21.1 | 26.4 | 38.4 | 3220 | 54654 | 441902 |
| arranque de Argon (14/10), equilibrada | 2.8 | 6.0 | 9.8 | 14.4 | 20.4 | 24.4 | 29.9 | — | 3293 | 55213 | 443414 |
| arranque de Argon sin cartas | 2.4 | 5.4 | 9.2 | — | — | — | — | — | 3297 | 55393 | — |
| la final con el automatico de Argon, equilibrada | 2.9 | 6.1 | 10.0 | 14.5 | 20.3 | 24.2 | 29.7 | 41.1 | 3293 | 55143 | 442703 |
| arranque de Argon con el automatico de hoy, equilibrada | 2.1 | 4.6 | 7.8 | 12.3 | 17.8 | 21.7 | 27.4 | — | 3250 | 54629 | 442866 |

## Las cartas

### Cartas elegidas al terminar cada oleada (promedio de todas las partidas que la terminan)

| variante | perfil | 1 | 2 | 3 | 4 | 5 | 6 | 8 | 10 | 15 | 20 | 30 | 40 | 50 | 60 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|

### Perfil bajo: cuantas veces tiene cada carta al morir en el muro (promedio de las ultimas 20 partidas)

| variante | cadencia | critico | dano | disparo_doble | dron | escudo | explosiva | furia_larga | granada_rapida | hacia_atras | hielo | iman | perforante | quemadura | vampiro | velocidad | vida | doradas |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 3.7 | 0.5 | 3.8 | 1.9 | 2.0 | 1.0 | 2.0 | 0.7 | 0.9 | 1.0 | 2.0 | 0.6 | 2.0 | 1.9 | 2.0 | 2.0 | 2.9 | 4.2 |
| arranque de Argon (14/10), equilibrada | 2.1 | 0.0 | 2.5 | 1.6 | 1.6 | 0.9 | 1.6 | 0.1 | 0.2 | 0.8 | 1.4 | 0.0 | 1.6 | 1.2 | 1.6 | 1.6 | 2.5 | 3.0 |
| la final con el automatico de Argon, equilibrada | 3.6 | 0.4 | 3.8 | 2.0 | 2.0 | 1.0 | 1.9 | 0.6 | 0.8 | 1.0 | 1.9 | 0.3 | 2.0 | 1.9 | 2.0 | 1.9 | 2.9 | 4.1 |
| arranque de Argon con el automatico de hoy, equilibrada | 2.2 | 0.1 | 2.5 | 1.8 | 1.6 | 0.9 | 1.6 | 0.1 | 0.3 | 0.8 | 1.4 | 0.1 | 1.8 | 1.3 | 1.6 | 1.6 | 2.4 | 3.2 |

### Perfil medio: cuantas veces tiene cada carta al morir en el muro (promedio de las ultimas 20 partidas)

| variante | cadencia | critico | dano | disparo_doble | dron | escudo | explosiva | furia_larga | granada_rapida | hacia_atras | hielo | iman | perforante | quemadura | vampiro | velocidad | vida | doradas |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 3.9 | 0.7 | 3.9 | 2.0 | 2.0 | 1.0 | 2.0 | 0.7 | 1.3 | 1.0 | 2.0 | 0.6 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 4.8 |
| arranque de Argon (14/10), equilibrada | 2.6 | 0.1 | 2.8 | 1.7 | 1.6 | 0.9 | 1.8 | 0.1 | 0.3 | 0.9 | 1.5 | 0.1 | 1.8 | 1.4 | 1.8 | 1.6 | 2.5 | 3.7 |
| la final con el automatico de Argon, equilibrada | 3.9 | 0.5 | 3.9 | 2.0 | 2.0 | 1.0 | 2.0 | 0.9 | 1.3 | 1.0 | 1.9 | 0.5 | 2.0 | 2.0 | 2.0 | 1.9 | 3.0 | 4.6 |
| arranque de Argon con el automatico de hoy, equilibrada | 2.4 | 0.1 | 2.8 | 1.7 | 1.6 | 0.9 | 1.7 | 0.1 | 0.3 | 0.9 | 1.5 | 0.1 | 1.9 | 1.4 | 1.7 | 1.8 | 2.4 | 3.6 |

### Perfil alto: cuantas veces tiene cada carta al morir en el muro (promedio de las ultimas 20 partidas)

| variante | cadencia | critico | dano | disparo_doble | dron | escudo | explosiva | furia_larga | granada_rapida | hacia_atras | hielo | iman | perforante | quemadura | vampiro | velocidad | vida | doradas |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada | 3.9 | 1.0 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 1.2 | 1.3 | 1.0 | 2.0 | 0.9 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 5.4 |
| arranque de Argon (14/10), equilibrada | 2.9 | 0.1 | 2.9 | 1.8 | 1.8 | 0.9 | 1.8 | 0.2 | 0.3 | 0.9 | 1.7 | 0.1 | 1.8 | 1.5 | 1.7 | 1.8 | 2.4 | 4.0 |
| la final con el automatico de Argon, equilibrada | 4.0 | 1.1 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 1.3 | 1.4 | 1.0 | 2.0 | 0.8 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 5.3 |
| arranque de Argon con el automatico de hoy, equilibrada | 2.8 | 0.1 | 3.0 | 1.9 | 1.7 | 1.0 | 1.8 | 0.2 | 0.3 | 0.8 | 1.6 | 0.2 | 1.9 | 1.4 | 1.7 | 1.7 | 2.4 | 3.8 |
