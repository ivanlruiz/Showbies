# Simulacion de las cartas de la 1.6.0

partidas 100, semillas 5, perfiles bajo,medio,alto, 5272 s

mejor pN: la mejor oleada completada hasta la partida N (mediana de las semillas). muro: la oleada mediana de las ultimas 20 partidas de cada semilla. llega a N: la partida en que se completa por primera vez la oleada N. min. en el muro: lo que dura una de esas partidas, con 3 s por eleccion de carta. % muertes en jefe: de las ultimas 100 partidas de cada semilla.

## El muro

### Perfil bajo

| variante | mejor p10 | mejor p30 | muro (ult. 20) | mejor al final | llega a 40 | a 50 | a 60 | a 80 | al tope (100) | min. en el muro | cartas por partida | % muertes en jefe | arma en el muro | sin balas con blanco | revivio |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 11 | 33 | 28 | 45 | 56 | — | — | — | 0 % | 15.1 | 0 | 16 % | energia | 27 % | — |
| 1.6.0, danio primero | 44 | 58 | 55 | 63 | 10 | 12 | 36 | — | 0 % | 55.3 | 32 | 12 % | energia | 39 % | — |
| 1.6.0, equilibrada | 50 | 60 | 57 | 64 | 7 | 10 | 27 | — | 0 % | 58.2 | 33 | 14 % | energia | 34 % | — |
| 1.6.0, al azar | 46 | 56 | 49 | 63 | 7 | 15 | 53 | — | 0 % | 46.3 | 31 | 17 % | energia | 32 % | — |
| danio x1,20, danio primero | 42 | 57 | 54 | 63 | 9 | 19 | 48 | — | 0 % | 53.8 | 32 | 16 % | energia | 39 % | — |
| danio x1,20, equilibrada | 47 | 57 | 55 | 62 | 8 | 16 | 58 | — | 0 % | 55.0 | 32 | 12 % | energia | 36 % | — |
| danio x1,15 tope 3, danio primero | 47 | 55 | 52 | 61 | 8 | 15 | 71 | — | 0 % | 51.3 | 32 | 17 % | energia | 40 % | — |
| danio x1,15 tope 3, equilibrada | 48 | 57 | 54 | 60 | 7 | 11 | 68 | — | 0 % | 52.2 | 32 | 14 % | energia | 36 % | — |
| sin armas (todo pistola), equilibrada | 48 | 60 | 57 | 65 | 8 | 12 | 27 | — | 0 % | 58.2 | 33 | 13 % | pistola | 35 % | — |
| sin escopeta (revolver), equilibrada | 47 | 59 | 57 | 62 | 6 | 12 | 41 | — | 0 % | 58.0 | 33 | 16 % | energia | 35 % | — |
| 1.5.2 sin cartas, sin armas (todo pistola) | 9 | 38 | 28 | 40 | 61 | — | — | — | 0 % | 15.7 | 0 | 21 % | pistola | 27 % | — |
| 1.5.2 sin cartas, sin escopeta (revolver) | 9 | 38 | 29 | 41 | 72 | — | — | — | 0 % | 16.1 | 0 | 17 % | energia | 26 % | — |
| PC (a mano, 16:9, techo 60), equilibrada | 49 | 56 | 54 | 61 | 9 | 13 | 92 | — | 0 % | 55.7 | 32 | 16 % | energia | 55 % | — |
| PC (a mano, 16:9, techo 60) sin cartas | 17 | 29 | 17 | 33 | — | — | — | — | 0 % | 7.7 | 0 | 14 % | energia | 51 % | — |
| telefono sin auto-apuntado, equilibrada | 45 | 55 | 55 | 59 | 9 | 12 | — | — | 0 % | 56.0 | 32 | 13 % | energia | 55 % | — |
| telefono sin auto-apuntado, sin cartas | 19 | 29 | 17 | 31 | — | — | — | — | 0 % | 8.3 | 0 | 13 % | energia | 48 % | — |
| 1.5.2 sin cartas, revive siempre | 23 | 43 | 37 | 48 | 26 | — | — | — | 0 % | 24.7 | 0 | 19 % | energia | 20 % | 100 % |
| 1.6.0 equilibrada, revive siempre | 56 | 61 | 59 | 66 | 4 | 7 | 16 | — | 0 % | 62.1 | 33 | 14 % | energia | 36 % | 100 % |
| 1.5.2 sin cartas, tunel a 30 FPS | 12 | 37 | 29 | 42 | 71 | — | — | — | 0 % | 15.9 | 0 | 15 % | energia | 24 % | — |
| 1.6.0 equilibrada, tunel a 30 FPS | 47 | 58 | 56 | 63 | 8 | 11 | 47 | — | 0 % | 56.4 | 33 | 14 % | energia | 35 % | — |
| calibracion: como curva.py | 29 | 48 | 39 | 49 | 18 | — | — | — | 0 % | 25.3 | 0 | 82 % | pistola | 9 % | — |

### Perfil medio

| variante | mejor p10 | mejor p30 | muro (ult. 20) | mejor al final | llega a 40 | a 50 | a 60 | a 80 | al tope (100) | min. en el muro | cartas por partida | % muertes en jefe | arma en el muro | sin balas con blanco | revivio |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 47 | 57 | 56 | 59 | 9 | 13 | — | — | 0 % | 52.0 | 0 | 37 % | energia | 2 % | — |
| 1.6.0, danio primero | 87 | 91 | 87 | 95 | 2 | 3 | 3 | 5 | 0 % | 123.1 | 38 | 17 % | energia | 15 % | — |
| 1.6.0, equilibrada | 86 | 93 | 86 | 97 | 3 | 3 | 4 | 6 | 0 % | 121.7 | 38 | 15 % | energia | 15 % | — |
| 1.6.0, al azar | 86 | 90 | 86 | 95 | 3 | 3 | 3 | 6 | 0 % | 120.8 | 38 | 18 % | energia | 14 % | — |
| danio x1,20, danio primero | 85 | 91 | 88 | 96 | 2 | 3 | 3 | 5 | 0 % | 126.0 | 39 | 19 % | energia | 16 % | — |
| danio x1,20, equilibrada | 87 | 91 | 86 | 94 | 2 | 3 | 3 | 5 | 0 % | 120.3 | 38 | 17 % | energia | 15 % | — |
| danio x1,15 tope 3, danio primero | 86 | 90 | 85 | 93 | 3 | 3 | 3 | 5 | 0 % | 119.7 | 38 | 23 % | energia | 17 % | — |
| danio x1,15 tope 3, equilibrada | 84 | 91 | 85 | 96 | 3 | 3 | 3 | 5 | 0 % | 118.2 | 38 | 21 % | energia | 16 % | — |
| sin armas (todo pistola), equilibrada | 84 | 91 | 86 | 98 | 3 | 3 | 4 | 6 | 0 % | 122.2 | 38 | 18 % | pistola | 15 % | — |
| sin escopeta (revolver), equilibrada | 87 | 89 | 86 | 96 | 2 | 3 | 3 | 6 | 0 % | 121.0 | 38 | 19 % | energia | 16 % | — |
| 1.5.2 sin cartas, sin armas (todo pistola) | 39 | 55 | 53 | 59 | 11 | 19 | — | — | 0 % | 46.7 | 0 | 47 % | pistola | 2 % | — |
| 1.5.2 sin cartas, sin escopeta (revolver) | 48 | 58 | 55 | 59 | 9 | 17 | — | — | 0 % | 50.1 | 0 | 34 % | energia | 3 % | — |
| PC (a mano, 16:9, techo 60), equilibrada | 83 | 87 | 84 | 92 | 2 | 3 | 3 | 7 | 0 % | 117.3 | 38 | 19 % | energia | 25 % | — |
| PC (a mano, 16:9, techo 60) sin cartas | 49 | 58 | 53 | 59 | 7 | 12 | — | — | 0 % | 48.1 | 0 | 25 % | energia | 5 % | — |
| telefono sin auto-apuntado, equilibrada | 83 | 90 | 84 | 92 | 2 | 3 | 3 | 6 | 0 % | 116.9 | 38 | 17 % | energia | 26 % | — |
| telefono sin auto-apuntado, sin cartas | 49 | 58 | 50 | 59 | 7 | 12 | — | — | 0 % | 43.1 | 0 | 22 % | energia | 5 % | — |
| 1.5.2 sin cartas, revive siempre | 53 | 59 | 59 | 60 | 7 | 9 | 68 | — | 0 % | 56.8 | 0 | 48 % | energia | 2 % | 100 % |
| 1.6.0 equilibrada, revive siempre | 89 | 96 | 90 | 99 | 2 | 2 | 3 | 4 | 1 % | 131.7 | 39 | 17 % | energia | 16 % | 100 % |
| 1.5.2 sin cartas, tunel a 30 FPS | 47 | 58 | 56 | 59 | 9 | 17 | — | — | 0 % | 53.0 | 0 | 34 % | energia | 2 % | — |
| 1.6.0 equilibrada, tunel a 30 FPS | 87 | 92 | 88 | 95 | 3 | 3 | 3 | 5 | 0 % | 125.4 | 39 | 17 % | energia | 15 % | — |
| calibracion: como curva.py | 49 | 57 | 50 | 59 | 8 | 14 | — | — | 0 % | 38.7 | 0 | 53 % | pistola | 4 % | — |

### Perfil alto

| variante | mejor p10 | mejor p30 | muro (ult. 20) | mejor al final | llega a 40 | a 50 | a 60 | a 80 | al tope (100) | min. en el muro | cartas por partida | % muertes en jefe | arma en el muro | sin balas con blanco | revivio |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 67 | 69 | 69 | 71 | 5 | 5 | 8 | — | 0 % | 76.0 | 0 | 58 % | energia | 0 % | — |
| 1.6.0, danio primero | 100 | 100 | 100 | 100 | 1 | 2 | 2 | 2 | 97 % | 156.9 | 40 | 83 % | energia | 8 % | — |
| 1.6.0, equilibrada | 100 | 100 | 100 | 100 | 2 | 2 | 2 | 3 | 95 % | 156.3 | 40 | 87 % | energia | 8 % | — |
| 1.6.0, al azar | 100 | 100 | 100 | 100 | 2 | 2 | 2 | 3 | 93 % | 155.8 | 40 | 84 % | energia | 8 % | — |
| danio x1,20, danio primero | 100 | 100 | 100 | 100 | 1 | 2 | 2 | 2 | 99 % | 157.1 | 40 | 79 % | energia | 9 % | — |
| danio x1,20, equilibrada | 100 | 100 | 100 | 100 | 2 | 2 | 2 | 2 | 98 % | 156.4 | 40 | 88 % | energia | 9 % | — |
| danio x1,15 tope 3, danio primero | 100 | 100 | 100 | 100 | 2 | 2 | 2 | 2 | 84 % | 157.4 | 40 | 94 % | energia | 10 % | — |
| danio x1,15 tope 3, equilibrada | 100 | 100 | 100 | 100 | 2 | 2 | 2 | 2 | 89 % | 156.7 | 40 | 92 % | energia | 10 % | — |
| sin armas (todo pistola), equilibrada | 100 | 100 | 100 | 100 | 2 | 2 | 2 | 3 | 94 % | 156.3 | 40 | 88 % | pistola | 8 % | — |
| sin escopeta (revolver), equilibrada | 100 | 100 | 100 | 100 | 2 | 2 | 2 | 3 | 95 % | 156.3 | 40 | 85 % | energia | 8 % | — |
| 1.5.2 sin cartas, sin armas (todo pistola) | 63 | 69 | 68 | 69 | 5 | 5 | 10 | — | 0 % | 74.1 | 0 | 47 % | pistola | 0 % | — |
| 1.5.2 sin cartas, sin escopeta (revolver) | 67 | 69 | 69 | 73 | 5 | 5 | 7 | — | 0 % | 75.8 | 0 | 57 % | energia | 0 % | — |
| PC (a mano, 16:9, techo 60), equilibrada | 100 | 100 | 100 | 100 | 2 | 2 | 2 | 2 | 94 % | 156.6 | 40 | 89 % | energia | 10 % | — |
| PC (a mano, 16:9, techo 60) sin cartas | 67 | 69 | 69 | 72 | 4 | 5 | 7 | — | 0 % | 76.8 | 0 | 67 % | energia | 1 % | — |
| telefono sin auto-apuntado, equilibrada | 100 | 100 | 100 | 100 | 2 | 2 | 2 | 2 | 97 % | 156.5 | 40 | 89 % | energia | 11 % | — |
| telefono sin auto-apuntado, sin cartas | 68 | 69 | 69 | 72 | 4 | 5 | 7 | — | 0 % | 77.0 | 0 | 69 % | energia | 1 % | — |
| 1.5.2 sin cartas, revive siempre | 68 | 69 | 69 | 73 | 4 | 5 | 6 | — | 0 % | 76.8 | 0 | 67 % | energia | 0 % | 100 % |
| 1.6.0 equilibrada, revive siempre | 100 | 100 | 100 | 100 | 2 | 2 | 2 | 2 | 100 % | 156.2 | 40 | 42 % | energia | 8 % | 3 % |
| 1.5.2 sin cartas, tunel a 30 FPS | 67 | 69 | 69 | 70 | 5 | 5 | 8 | — | 0 % | 75.9 | 0 | 59 % | energia | 0 % | — |
| 1.6.0 equilibrada, tunel a 30 FPS | 100 | 100 | 100 | 100 | 2 | 2 | 2 | 2 | 98 % | 156.4 | 40 | 86 % | energia | 9 % | — |
| calibracion: como curva.py | 66 | 69 | 69 | 69 | 5 | 6 | 9 | — | 0 % | 71.9 | 0 | 63 % | pistola | 1 % | — |

## El arranque

### Perfil bajo: las primeras 12 partidas (oleada a la que se llega, mediana; entre parentesis el arma con que se arranca)

| variante | p1 | p2 | p3 | p4 | p5 | p6 | p7 | p8 | p9 | p10 | p11 | p12 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 1 (pis) | 2 (pis) | 2 (rev) | 2 (rev) | 3 (rev) | 5 (esc) | 6 (esc) | 9 (esc) | 9 (esc) | 11 (rif) | 19 (rif) | 19 (rif) |
| 1.6.0, danio primero | 2 (pis) | 2 (pis) | 4 (rev) | 9 (esc) | 16 (esc) | 32 (rif) | 34 (rif) | 32 (ene) | 37 (ene) | 44 (ene) | 43 (ene) | 44 (ene) |
| 1.6.0, equilibrada | 2 (pis) | 2 (rev) | 9 (rev) | 39 (esc) | 19 (rif) | 38 (ene) | 39 (ene) | 39 (ene) | 48 (ene) | 49 (ene) | 51 (ene) | 50 (ene) |
| 1.6.0, al azar | 2 (pis) | 2 (rev) | 8 (rev) | 19 (esc) | 34 (rif) | 32 (rif) | 39 (ene) | 35 (ene) | 39 (ene) | 42 (ene) | 37 (ene) | 31 (ene) |
| danio x1,20, danio primero | 2 (pis) | 2 (pis) | 2 (rev) | 9 (rev) | 32 (esc) | 29 (rif) | 35 (rif) | 35 (ene) | 32 (ene) | 39 (ene) | 29 (ene) | 40 (ene) |
| danio x1,20, equilibrada | 2 (pis) | 2 (pis) | 3 (rev) | 10 (rev) | 38 (esc) | 33 (rif) | 37 (rif) | 39 (ene) | 39 (ene) | 46 (ene) | 45 (ene) | 44 (ene) |
| danio x1,15 tope 3, danio primero | 2 (pis) | 2 (pis) | 3 (rev) | 9 (rev) | 26 (esc) | 34 (rif) | 30 (ene) | 40 (ene) | 40 (ene) | 40 (ene) | 45 (ene) | 44 (ene) |
| danio x1,15 tope 3, equilibrada | 2 (pis) | 2 (pis) | 3 (rev) | 10 (rev) | 19 (esc) | 39 (rif) | 40 (ene) | 42 (ene) | 32 (ene) | 45 (ene) | 50 (ene) | 45 (ene) |
| sin armas (todo pistola), equilibrada | 2 (pis) | 2 (pis) | 3 (pis) | 7 (pis) | 13 (pis) | 35 (pis) | 35 (pis) | 37 (pis) | 42 (pis) | 47 (pis) | 44 (pis) | 47 (pis) |
| sin escopeta (revolver), equilibrada | 2 (pis) | 2 (rev) | 9 (rev) | 31 (rev) | 29 (rif) | 39 (ene) | 36 (ene) | 36 (ene) | 45 (ene) | 47 (ene) | 45 (ene) | 50 (ene) |
| 1.5.2 sin cartas, sin armas (todo pistola) | 1 (pis) | 2 (pis) | 2 (pis) | 2 (pis) | 2 (pis) | 5 (pis) | 5 (pis) | 6 (pis) | 9 (pis) | 9 (pis) | 10 (pis) | 16 (pis) |
| 1.5.2 sin cartas, sin escopeta (revolver) | 1 (pis) | 2 (pis) | 2 (rev) | 2 (rev) | 3 (rev) | 4 (rev) | 5 (rev) | 5 (rev) | 9 (rev) | 9 (rev) | 12 (rif) | 19 (rif) |
| PC (a mano, 16:9, techo 60), equilibrada | 2 (pis) | 4 (rev) | 22 (esc) | 27 (rif) | 30 (rif) | 31 (rif) | 27 (ene) | 39 (ene) | 44 (ene) | 49 (ene) | 46 (ene) | 46 (ene) |
| PC (a mano, 16:9, techo 60) sin cartas | 2 (pis) | 2 (pis) | 2 (rev) | 4 (rev) | 8 (esc) | 9 (esc) | 16 (esc) | 16 (rif) | 17 (rif) | 10 (rif) | 17 (rif) | 16 (rif) |
| telefono sin auto-apuntado, equilibrada | 2 (pis) | 9 (rev) | 25 (esc) | 23 (rif) | 29 (rif) | 26 (rif) | 32 (rif) | 39 (ene) | 44 (ene) | 45 (ene) | 44 (ene) | 49 (ene) |
| telefono sin auto-apuntado, sin cartas | 2 (pis) | 2 (pis) | 3 (rev) | 5 (rev) | 8 (esc) | 9 (esc) | 17 (esc) | 16 (rif) | 19 (rif) | 17 (rif) | 9 (rif) | 11 (rif) |
| 1.5.2 sin cartas, revive siempre | 1 (pis) | 2 (pis) | 3 (rev) | 3 (rev) | 6 (esc) | 8 (esc) | 10 (esc) | 16 (esc) | 19 (rif) | 21 (rif) | 29 (rif) | 29 (rif) |
| 1.6.0 equilibrada, revive siempre | 2 (pis) | 2 (rev) | 10 (rev) | 44 (esc) | 40 (ene) | 49 (ene) | 48 (ene) | 52 (ene) | 54 (ene) | 53 (ene) | 53 (ene) | 58 (ene) |
| 1.5.2 sin cartas, tunel a 30 FPS | 1 (pis) | 2 (pis) | 2 (rev) | 2 (rev) | 3 (rev) | 5 (esc) | 6 (esc) | 8 (esc) | 9 (esc) | 12 (esc) | 9 (rif) | 16 (rif) |
| 1.6.0 equilibrada, tunel a 30 FPS | 2 (pis) | 2 (rev) | 2 (rev) | 4 (rev) | 29 (esc) | 34 (rif) | 34 (rif) | 39 (ene) | 45 (ene) | 47 (ene) | 47 (ene) | 49 (ene) |
| calibracion: como curva.py | 2 (pis) | 4 (pis) | 4 (pis) | 6 (pis) | 8 (pis) | 9 (pis) | 12 (pis) | 19 (pis) | 19 (pis) | 29 (pis) | 29 (pis) | 39 (pis) |

### Perfil medio: las primeras 12 partidas (oleada a la que se llega, mediana; entre parentesis el arma con que se arranca)

| variante | p1 | p2 | p3 | p4 | p5 | p6 | p7 | p8 | p9 | p10 | p11 | p12 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 3 (pis) | 3 (rev) | 7 (esc) | 9 (esc) | 19 (esc) | 26 (rif) | 29 (rif) | 39 (rif) | 47 (ene) | 40 (ene) | 49 (ene) | 49 (ene) |
| 1.6.0, danio primero | 7 (pis) | 49 (esc) | 69 (ene) | 74 (ene) | 78 (ene) | 79 (ene) | 80 (ene) | 80 (ene) | 82 (ene) | 80 (ene) | 82 (ene) | 81 (ene) |
| 1.6.0, equilibrada | 5 (pis) | 39 (esc) | 54 (rif) | 76 (ene) | 78 (ene) | 78 (ene) | 86 (ene) | 79 (ene) | 83 (ene) | 81 (ene) | 83 (ene) | 81 (ene) |
| 1.6.0, al azar | 4 (pis) | 16 (rev) | 60 (rif) | 69 (ene) | 77 (ene) | 79 (ene) | 78 (ene) | 83 (ene) | 77 (ene) | 85 (ene) | 83 (ene) | 82 (ene) |
| danio x1,20, danio primero | 7 (pis) | 49 (esc) | 71 (ene) | 74 (ene) | 79 (ene) | 75 (ene) | 79 (ene) | 84 (ene) | 81 (ene) | 83 (ene) | 80 (ene) | 80 (ene) |
| danio x1,20, equilibrada | 6 (pis) | 49 (esc) | 75 (ene) | 77 (ene) | 81 (ene) | 78 (ene) | 77 (ene) | 84 (ene) | 84 (ene) | 85 (ene) | 86 (ene) | 85 (ene) |
| danio x1,15 tope 3, danio primero | 7 (pis) | 39 (esc) | 63 (rif) | 73 (ene) | 79 (ene) | 80 (ene) | 81 (ene) | 81 (ene) | 79 (ene) | 83 (ene) | 80 (ene) | 82 (ene) |
| danio x1,15 tope 3, equilibrada | 6 (pis) | 39 (esc) | 64 (rif) | 74 (ene) | 80 (ene) | 80 (ene) | 83 (ene) | 77 (ene) | 79 (ene) | 79 (ene) | 80 (ene) | 82 (ene) |
| sin armas (todo pistola), equilibrada | 5 (pis) | 39 (pis) | 59 (pis) | 70 (pis) | 74 (pis) | 80 (pis) | 79 (pis) | 78 (pis) | 79 (pis) | 83 (pis) | 79 (pis) | 83 (pis) |
| sin escopeta (revolver), equilibrada | 5 (pis) | 44 (rev) | 68 (ene) | 76 (ene) | 79 (ene) | 79 (ene) | 83 (ene) | 83 (ene) | 84 (ene) | 81 (ene) | 81 (ene) | 79 (ene) |
| 1.5.2 sin cartas, sin armas (todo pistola) | 3 (pis) | 3 (pis) | 5 (pis) | 7 (pis) | 14 (pis) | 23 (pis) | 29 (pis) | 39 (pis) | 39 (pis) | 39 (pis) | 48 (pis) | 49 (pis) |
| 1.5.2 sin cartas, sin escopeta (revolver) | 3 (pis) | 3 (rev) | 6 (rev) | 9 (rev) | 17 (rev) | 26 (rif) | 29 (rif) | 39 (rif) | 48 (ene) | 44 (ene) | 49 (ene) | 49 (ene) |
| PC (a mano, 16:9, techo 60), equilibrada | 4 (pis) | 48 (rev) | 68 (ene) | 69 (ene) | 75 (ene) | 77 (ene) | 79 (ene) | 78 (ene) | 82 (ene) | 79 (ene) | 79 (ene) | 81 (ene) |
| PC (a mano, 16:9, techo 60) sin cartas | 3 (pis) | 5 (rev) | 9 (esc) | 19 (esc) | 29 (rif) | 38 (rif) | 40 (ene) | 47 (ene) | 49 (ene) | 46 (ene) | 47 (ene) | 49 (ene) |
| telefono sin auto-apuntado, equilibrada | 8 (pis) | 43 (esc) | 64 (ene) | 71 (ene) | 75 (ene) | 76 (ene) | 75 (ene) | 73 (ene) | 80 (ene) | 79 (ene) | 79 (ene) | 82 (ene) |
| telefono sin auto-apuntado, sin cartas | 3 (pis) | 6 (rev) | 9 (esc) | 19 (esc) | 27 (rif) | 39 (rif) | 44 (ene) | 41 (ene) | 49 (ene) | 49 (ene) | 46 (ene) | 48 (ene) |
| 1.5.2 sin cartas, revive siempre | 3 (pis) | 4 (rev) | 8 (esc) | 17 (esc) | 25 (rif) | 36 (rif) | 46 (ene) | 49 (ene) | 52 (ene) | 52 (ene) | 54 (ene) | 54 (ene) |
| 1.6.0 equilibrada, revive siempre | 5 (pis) | 51 (esc) | 70 (ene) | 82 (ene) | 85 (ene) | 82 (ene) | 82 (ene) | 86 (ene) | 89 (ene) | 88 (ene) | 83 (ene) | 89 (ene) |
| 1.5.2 sin cartas, tunel a 30 FPS | 2 (pis) | 3 (rev) | 4 (rev) | 9 (esc) | 20 (esc) | 23 (rif) | 29 (rif) | 39 (rif) | 44 (ene) | 43 (ene) | 49 (ene) | 49 (ene) |
| 1.6.0 equilibrada, tunel a 30 FPS | 6 (pis) | 39 (esc) | 69 (rif) | 78 (ene) | 80 (ene) | 81 (ene) | 82 (ene) | 80 (ene) | 84 (ene) | 84 (ene) | 85 (ene) | 86 (ene) |
| calibracion: como curva.py | 4 (pis) | 6 (pis) | 9 (pis) | 19 (pis) | 29 (pis) | 36 (pis) | 39 (pis) | 44 (pis) | 46 (pis) | 49 (pis) | 49 (pis) | 48 (pis) |

### Perfil alto: las primeras 12 partidas (oleada a la que se llega, mediana; entre parentesis el arma con que se arranca)

| variante | p1 | p2 | p3 | p4 | p5 | p6 | p7 | p8 | p9 | p10 | p11 | p12 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.5.2 sin cartas | 5 (pis) | 12 (esc) | 29 (esc) | 39 (rif) | 55 (ene) | 59 (ene) | 59 (ene) | 62 (ene) | 66 (ene) | 59 (ene) | 65 (ene) | 67 (ene) |
| 1.6.0, danio primero | 40 (pis) | 94 (rif) | 100 (ene) | 100 (ene) | 99 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) |
| 1.6.0, equilibrada | 9 (pis) | 79 (esc) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) |
| 1.6.0, al azar | 7 (pis) | 62 (esc) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) |
| danio x1,20, danio primero | 40 (pis) | 97 (rif) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) |
| danio x1,20, equilibrada | 37 (pis) | 85 (rif) | 99 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 99 (ene) | 100 (ene) |
| danio x1,15 tope 3, danio primero | 39 (pis) | 89 (rif) | 100 (ene) | 100 (ene) | 99 (ene) | 100 (ene) | 99 (ene) | 99 (ene) | 100 (ene) | 100 (ene) | 99 (ene) | 99 (ene) |
| danio x1,15 tope 3, equilibrada | 29 (pis) | 89 (rif) | 99 (ene) | 100 (ene) | 99 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 99 (ene) | 100 (ene) | 100 (ene) | 100 (ene) |
| sin armas (todo pistola), equilibrada | 9 (pis) | 69 (pis) | 100 (pis) | 100 (pis) | 100 (pis) | 100 (pis) | 100 (pis) | 100 (pis) | 100 (pis) | 100 (pis) | 100 (pis) | 100 (pis) |
| sin escopeta (revolver), equilibrada | 9 (pis) | 79 (rev) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) |
| 1.5.2 sin cartas, sin armas (todo pistola) | 5 (pis) | 9 (pis) | 25 (pis) | 39 (pis) | 55 (pis) | 59 (pis) | 59 (pis) | 59 (pis) | 59 (pis) | 62 (pis) | 59 (pis) | 59 (pis) |
| 1.5.2 sin cartas, sin escopeta (revolver) | 5 (pis) | 9 (rev) | 26 (rev) | 39 (rif) | 57 (ene) | 59 (ene) | 65 (ene) | 59 (ene) | 64 (ene) | 66 (ene) | 66 (ene) | 66 (ene) |
| PC (a mano, 16:9, techo 60), equilibrada | 39 (pis) | 89 (rif) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) |
| PC (a mano, 16:9, techo 60) sin cartas | 4 (pis) | 12 (esc) | 29 (esc) | 45 (rif) | 59 (ene) | 59 (ene) | 65 (ene) | 65 (ene) | 67 (ene) | 66 (ene) | 66 (ene) | 67 (ene) |
| telefono sin auto-apuntado, equilibrada | 29 (pis) | 80 (rif) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 99 (ene) | 100 (ene) |
| telefono sin auto-apuntado, sin cartas | 4 (pis) | 12 (esc) | 29 (esc) | 47 (rif) | 59 (ene) | 59 (ene) | 64 (ene) | 66 (ene) | 67 (ene) | 67 (ene) | 67 (ene) | 66 (ene) |
| 1.5.2 sin cartas, revive siempre | 5 (pis) | 13 (esc) | 34 (esc) | 49 (rif) | 59 (ene) | 63 (ene) | 65 (ene) | 68 (ene) | 67 (ene) | 67 (ene) | 68 (ene) | 68 (ene) |
| 1.6.0 equilibrada, revive siempre | 37 (pis) | 92 (rif) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) |
| 1.5.2 sin cartas, tunel a 30 FPS | 5 (pis) | 12 (esc) | 29 (esc) | 39 (rif) | 58 (ene) | 59 (ene) | 59 (ene) | 64 (ene) | 65 (ene) | 64 (ene) | 66 (ene) | 65 (ene) |
| 1.6.0 equilibrada, tunel a 30 FPS | 39 (pis) | 89 (rif) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) |
| calibracion: como curva.py | 5 (pis) | 9 (pis) | 26 (pis) | 39 (pis) | 49 (pis) | 59 (pis) | 59 (pis) | 59 (pis) | 63 (pis) | 66 (pis) | 65 (pis) | 62 (pis) |

## Las cartas

### Cartas elegidas al terminar cada oleada (promedio de todas las partidas que la terminan)

| variante | perfil | 1 | 2 | 3 | 4 | 5 | 6 | 8 | 10 | 15 | 20 | 30 | 40 | 50 | 60 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.6.0, danio primero | bajo | 1.0 | 2.0 | 3.0 | 4.1 | 6.0 | 7.0 | 9.4 | 12.0 | 16.0 | 19.0 | 24.0 | 28.0 | 31.0 | 33.0 |
| 1.6.0, danio primero | medio | 1.0 | 2.0 | 3.0 | 4.3 | 6.0 | 7.0 | 9.5 | 12.0 | 16.0 | 19.0 | 24.0 | 28.0 | 31.0 | 33.0 |
| 1.6.0, danio primero | alto | 1.0 | 2.0 | 3.0 | 4.4 | 6.0 | 7.0 | 9.6 | 12.0 | 16.0 | 19.0 | 24.0 | 28.0 | 31.0 | 33.0 |
| 1.6.0, equilibrada | bajo | 1.0 | 2.0 | 3.0 | 4.2 | 6.0 | 7.0 | 9.4 | 12.0 | 16.0 | 19.0 | 24.0 | 28.0 | 31.0 | 33.0 |
| 1.6.0, equilibrada | medio | 1.0 | 2.0 | 3.0 | 4.2 | 6.0 | 7.0 | 9.4 | 12.0 | 16.0 | 19.0 | 24.0 | 28.0 | 31.0 | 33.0 |
| 1.6.0, equilibrada | alto | 1.0 | 2.0 | 3.0 | 4.3 | 6.0 | 7.0 | 9.5 | 12.0 | 16.0 | 19.0 | 24.0 | 28.0 | 31.0 | 33.0 |

### Perfil bajo: cuantas veces tiene cada carta al morir en el muro (promedio de las ultimas 20 partidas)

| variante | cadencia | critico | dano | disparo_doble | dron | escudo | explosiva | furia_larga | granada_rapida | hacia_atras | hielo | iman | perforante | quemadura | vampiro | velocidad | vida | doradas |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.6.0, danio primero | 4.0 | 1.7 | 4.0 | 2.0 | 1.9 | 1.0 | 2.0 | 1.6 | 1.7 | 1.0 | 2.0 | 1.2 | 2.0 | 1.9 | 1.8 | 1.8 | 2.9 | 4.6 |
| 1.6.0, equilibrada | 4.0 | 1.6 | 4.0 | 2.0 | 1.9 | 1.0 | 2.0 | 1.8 | 1.8 | 1.0 | 2.0 | 1.2 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 4.8 |
| 1.6.0, al azar | 3.2 | 2.3 | 2.8 | 1.8 | 1.7 | 0.9 | 1.7 | 1.9 | 1.8 | 0.9 | 1.8 | 1.8 | 1.7 | 1.8 | 1.6 | 1.7 | 2.3 | 3.1 |
| danio x1,20, danio primero | 4.0 | 1.8 | 3.9 | 2.0 | 1.9 | 1.0 | 1.9 | 1.4 | 1.7 | 1.0 | 2.0 | 1.3 | 2.0 | 2.0 | 1.8 | 1.8 | 2.9 | 4.5 |
| danio x1,20, equilibrada | 4.0 | 1.5 | 3.9 | 2.0 | 1.9 | 1.0 | 2.0 | 1.6 | 1.7 | 1.0 | 2.0 | 1.1 | 2.0 | 2.0 | 2.0 | 2.0 | 2.9 | 4.5 |
| danio x1,15 tope 3, danio primero | 4.0 | 1.6 | 3.0 | 2.0 | 1.9 | 1.0 | 2.0 | 1.6 | 1.8 | 1.0 | 2.0 | 1.4 | 2.0 | 2.0 | 1.9 | 1.9 | 2.8 | 4.4 |
| danio x1,15 tope 3, equilibrada | 4.0 | 1.8 | 3.0 | 2.0 | 1.9 | 1.0 | 2.0 | 1.7 | 1.8 | 1.0 | 2.0 | 1.3 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 4.6 |
| sin armas (todo pistola), equilibrada | 4.0 | 1.9 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 1.7 | 1.8 | 1.0 | 2.0 | 1.2 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 4.9 |
| sin escopeta (revolver), equilibrada | 4.0 | 1.8 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 1.7 | 1.8 | 1.0 | 2.0 | 1.1 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 4.8 |
| PC (a mano, 16:9, techo 60), equilibrada | 3.9 | 1.4 | 4.0 | 2.0 | 1.9 | 1.0 | 2.0 | 1.5 | 1.7 | 1.0 | 2.0 | 1.0 | 2.0 | 2.0 | 1.9 | 2.0 | 2.9 | 4.5 |
| telefono sin auto-apuntado, equilibrada | 3.9 | 1.4 | 3.9 | 2.0 | 1.9 | 1.0 | 2.0 | 1.5 | 1.7 | 1.0 | 2.0 | 1.3 | 2.0 | 1.9 | 2.0 | 2.0 | 2.9 | 4.6 |
| 1.6.0 equilibrada, revive siempre | 4.0 | 1.9 | 4.0 | 2.0 | 1.9 | 1.0 | 2.0 | 1.8 | 1.9 | 1.0 | 2.0 | 1.4 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 5.1 |
| 1.6.0 equilibrada, tunel a 30 FPS | 3.9 | 1.7 | 4.0 | 2.0 | 1.8 | 1.0 | 2.0 | 1.4 | 1.7 | 1.0 | 2.0 | 1.2 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 4.6 |

### Perfil medio: cuantas veces tiene cada carta al morir en el muro (promedio de las ultimas 20 partidas)

| variante | cadencia | critico | dano | disparo_doble | dron | escudo | explosiva | furia_larga | granada_rapida | hacia_atras | hielo | iman | perforante | quemadura | vampiro | velocidad | vida | doradas |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.6.0, danio primero | 4.0 | 3.0 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 6.0 |
| 1.6.0, equilibrada | 4.0 | 3.0 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 6.0 |
| 1.6.0, al azar | 4.0 | 3.0 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 5.4 |
| danio x1,20, danio primero | 4.0 | 3.0 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 5.9 |
| danio x1,20, equilibrada | 4.0 | 3.0 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 6.0 |
| danio x1,15 tope 3, danio primero | 4.0 | 3.0 | 3.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 5.6 |
| danio x1,15 tope 3, equilibrada | 4.0 | 3.0 | 3.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 5.6 |
| sin armas (todo pistola), equilibrada | 4.0 | 3.0 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 6.0 |
| sin escopeta (revolver), equilibrada | 4.0 | 3.0 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 6.0 |
| PC (a mano, 16:9, techo 60), equilibrada | 4.0 | 3.0 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 6.0 |
| telefono sin auto-apuntado, equilibrada | 4.0 | 3.0 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 6.0 |
| 1.6.0 equilibrada, revive siempre | 4.0 | 3.0 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 6.0 |
| 1.6.0 equilibrada, tunel a 30 FPS | 4.0 | 3.0 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 6.0 |

### Perfil alto: cuantas veces tiene cada carta al morir en el muro (promedio de las ultimas 20 partidas)

| variante | cadencia | critico | dano | disparo_doble | dron | escudo | explosiva | furia_larga | granada_rapida | hacia_atras | hielo | iman | perforante | quemadura | vampiro | velocidad | vida | doradas |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1.6.0, danio primero | 4.0 | 3.0 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 6.0 |
| 1.6.0, equilibrada | 4.0 | 3.0 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 6.0 |
| 1.6.0, al azar | 4.0 | 3.0 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 5.6 |
| danio x1,20, danio primero | 4.0 | 3.0 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 6.0 |
| danio x1,20, equilibrada | 4.0 | 3.0 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 5.9 |
| danio x1,15 tope 3, danio primero | 4.0 | 3.0 | 3.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 5.5 |
| danio x1,15 tope 3, equilibrada | 4.0 | 3.0 | 3.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 5.6 |
| sin armas (todo pistola), equilibrada | 4.0 | 3.0 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 5.9 |
| sin escopeta (revolver), equilibrada | 4.0 | 3.0 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 6.0 |
| PC (a mano, 16:9, techo 60), equilibrada | 4.0 | 3.0 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 6.0 |
| telefono sin auto-apuntado, equilibrada | 4.0 | 3.0 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 6.0 |
| 1.6.0 equilibrada, revive siempre | 4.0 | 3.0 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 5.9 |
| 1.6.0 equilibrada, tunel a 30 FPS | 4.0 | 3.0 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 6.0 |
