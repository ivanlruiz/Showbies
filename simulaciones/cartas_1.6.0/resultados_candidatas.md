# Simulacion de las cartas de la 1.6.0

partidas 100, semillas 5, perfiles bajo,medio,alto, 2437 s; las candidatas D y E y la "1.5.2 sin cartas, con el cargador lleno" se corrieron aparte con las mismas semillas y se sumaron a cada tabla

mejor pN: la mejor oleada completada hasta la partida N (mediana de las semillas). muro: la oleada mediana de las ultimas 20 partidas de cada semilla. llega a N: la partida en que se completa por primera vez la oleada N. min. en el muro: lo que dura una de esas partidas, con 3 s por eleccion de carta. % muertes en jefe: de las ultimas 100 partidas de cada semilla.

## El muro

### Perfil bajo

| variante | mejor p10 | mejor p30 | muro (ult. 20) | mejor al final | llega a 40 | a 50 | a 60 | a 80 | al tope (100) | min. en el muro | cartas por partida | % muertes en jefe | arma en el muro | sin balas con blanco | revivio |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| candidata: numeros a la mitad (danio x1,12, cadencia y vida x1,1, critico +5 %) | 45 | 55 | 49 | 59 | 8 | 18 | — | — | 0 % | 44.1 | 31 | 14 % | energia | 25 % | — |
| candidata: el PODER crece x1,25 (menos cartas) | 44 | 49 | 42 | 60 | 8 | 33 | 59 | — | 0 % | 32.8 | 20 | 14 % | energia | 29 % | — |
| candidata: vida x1,12 y golpe x1,05 desde la 11 | 37 | 46 | 41 | 50 | 12 | 36 | — | — | 0 % | 32.1 | 28 | 14 % | energia | 33 % | — |
| candidata: el cargador se llena al terminar cada oleada | 56 | 61 | 60 | 66 | 5 | 7 | 21 | — | 0 % | 62.7 | 33 | 11 % | energia | 21 % | — |
| candidata: oleadas de 10 + 3n, uno cada 0,25 s | 45 | 55 | 51 | 61 | 8 | 15 | 74 | — | 0 % | 30.1 | 29 | 13 % | energia | 40 % | — |
| candidata A: PODER x1,25, vida x1,12 y golpe x1,05, 10 + 3n cada 0,25 s | 39 | 43 | 32 | 52 | 13 | 48 | — | — | 0 % | 14.1 | 17 | 12 % | energia | 30 % | — |
| candidata B: la A con el cargador lleno en cada oleada | 51 | 67 | 52 | 68 | 7 | 9 | 19 | — | 0 % | 31.4 | 21 | 21 % | energia | 7 % | — |
| candidata C: la B con uno cada 0,20 s | 59 | 67 | 53 | 69 | 8 | 10 | 23 | — | 0 % | 29.6 | 21 | 20 % | energia | 4 % | — |
| candidata D: PODER x1,25, vida x1,14 y golpe x1,06, 10 + 2n cada 0,25 s, cargador lleno | 49 | 57 | 53 | 59 | 8 | 11 | — | — | 0 % | 27.0 | 19 | 36 % | energia | 3 % | — |
| candidata E: el PODER como esta, vida x1,15 y golpe x1,07, 10 + 2n cada 0,25 s, cargador lleno | 55 | 59 | 53 | 59 | 7 | 9 | — | — | 0 % | 26.5 | 28 | 30 % | energia | 7 % | — |
| 1.5.2 sin cartas, con el cargador lleno al terminar cada oleada | 17 | 47 | 48 | 49 | 18 | — | — | — | 0 % | 39.5 | 0 | 46 % | energia | 2 % | — |

### Perfil medio

| variante | mejor p10 | mejor p30 | muro (ult. 20) | mejor al final | llega a 40 | a 50 | a 60 | a 80 | al tope (100) | min. en el muro | cartas por partida | % muertes en jefe | arma en el muro | sin balas con blanco | revivio |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| candidata: numeros a la mitad (danio x1,12, cadencia y vida x1,1, critico +5 %) | 84 | 90 | 84 | 96 | 3 | 4 | 4 | 5 | 0 % | 114.1 | 38 | 16 % | energia | 7 % | — |
| candidata: el PODER crece x1,25 (menos cartas) | 72 | 87 | 70 | 91 | 3 | 4 | 5 | 15 | 0 % | 80.7 | 24 | 13 % | energia | 8 % | — |
| candidata: vida x1,12 y golpe x1,05 desde la 11 | 69 | 73 | 69 | 79 | 3 | 4 | 6 | — | 0 % | 80.3 | 35 | 33 % | energia | 17 % | — |
| candidata: el cargador se llena al terminar cada oleada | 88 | 91 | 88 | 97 | 3 | 3 | 3 | 4 | 1 % | 124.8 | 39 | 17 % | energia | 13 % | — |
| candidata: oleadas de 10 + 3n, uno cada 0,25 s | 83 | 85 | 80 | 92 | 3 | 3 | 4 | 8 | 0 % | 65.0 | 36 | 17 % | energia | 16 % | — |
| candidata A: PODER x1,25, vida x1,12 y golpe x1,05, 10 + 3n cada 0,25 s | 60 | 69 | 55 | 72 | 4 | 5 | 10 | — | 0 % | 33.8 | 21 | 12 % | energia | 9 % | — |
| candidata B: la A con el cargador lleno en cada oleada | 69 | 69 | 59 | 76 | 4 | 4 | 8 | — | 0 % | 38.3 | 22 | 18 % | energia | 6 % | — |
| candidata C: la B con uno cada 0,20 s | 66 | 73 | 59 | 76 | 4 | 5 | 7 | — | 0 % | 33.8 | 22 | 17 % | energia | 6 % | — |
| candidata D: PODER x1,25, vida x1,14 y golpe x1,06, 10 + 2n cada 0,25 s, cargador lleno | 58 | 65 | 59 | 68 | 5 | 6 | 14 | — | 0 % | 29.7 | 20 | 27 % | energia | 3 % | — |
| candidata E: el PODER como esta, vida x1,15 y golpe x1,07, 10 + 2n cada 0,25 s, cargador lleno | 59 | 64 | 55 | 66 | 3 | 5 | 13 | — | 0 % | 26.9 | 28 | 23 % | energia | 6 % | — |
| 1.5.2 sin cartas, con el cargador lleno al terminar cada oleada | 49 | 58 | 58 | 60 | 9 | 12 | 85 | — | 0 % | 55.1 | 0 | 42 % | energia | 1 % | — |

### Perfil alto

| variante | mejor p10 | mejor p30 | muro (ult. 20) | mejor al final | llega a 40 | a 50 | a 60 | a 80 | al tope (100) | min. en el muro | cartas por partida | % muertes en jefe | arma en el muro | sin balas con blanco | revivio |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| candidata: numeros a la mitad (danio x1,12, cadencia y vida x1,1, critico +5 %) | 100 | 100 | 100 | 100 | 2 | 2 | 2 | 3 | 73 % | 155.9 | 40 | 93 % | energia | 4 % | — |
| candidata: el PODER crece x1,25 (menos cartas) | 100 | 100 | 100 | 100 | 2 | 3 | 3 | 4 | 61 % | 154.6 | 27 | 26 % | energia | 6 % | — |
| candidata: vida x1,12 y golpe x1,05 desde la 11 | 90 | 97 | 89 | 99 | 2 | 2 | 3 | 4 | 0 % | 129.2 | 39 | 74 % | energia | 12 % | — |
| candidata: el cargador se llena al terminar cada oleada | 100 | 100 | 100 | 100 | 2 | 2 | 2 | 3 | 94 % | 155.9 | 40 | 93 % | energia | 8 % | — |
| candidata: oleadas de 10 + 3n, uno cada 0,25 s | 100 | 100 | 100 | 100 | 2 | 2 | 2 | 2 | 89 % | 94.6 | 38 | 79 % | energia | 10 % | — |
| candidata A: PODER x1,25, vida x1,12 y golpe x1,05, 10 + 3n cada 0,25 s | 79 | 89 | 77 | 92 | 3 | 3 | 4 | 11 | 0 % | 62.2 | 24 | 30 % | energia | 7 % | — |
| candidata B: la A con el cargador lleno en cada oleada | 79 | 89 | 77 | 93 | 3 | 3 | 4 | 13 | 0 % | 60.6 | 24 | 27 % | energia | 7 % | — |
| candidata C: la B con uno cada 0,20 s | 78 | 86 | 78 | 92 | 3 | 3 | 4 | 18 | 0 % | 53.7 | 24 | 22 % | energia | 6 % | — |
| candidata D: PODER x1,25, vida x1,14 y golpe x1,06, 10 + 2n cada 0,25 s, cargador lleno | 69 | 77 | 68 | 79 | 3 | 4 | 6 | — | 0 % | 38.7 | 21 | 30 % | energia | 4 % | — |
| candidata E: el PODER como esta, vida x1,15 y golpe x1,07, 10 + 2n cada 0,25 s, cargador lleno | 69 | 75 | 69 | 78 | 2 | 3 | 5 | — | 0 % | 38.9 | 31 | 32 % | energia | 8 % | — |
| 1.5.2 sin cartas, con el cargador lleno al terminar cada oleada | 66 | 69 | 69 | 70 | 5 | 5 | 7 | — | 0 % | 75.2 | 0 | 57 % | energia | 0 % | — |

## El arranque

### Perfil bajo: las primeras 12 partidas (oleada a la que se llega, mediana; entre parentesis el arma con que se arranca)

| variante | p1 | p2 | p3 | p4 | p5 | p6 | p7 | p8 | p9 | p10 | p11 | p12 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| candidata: numeros a la mitad (danio x1,12, cadencia y vida x1,1, critico +5 %) | 2 (pis) | 2 (pis) | 3 (rev) | 4 (rev) | 9 (esc) | 27 (esc) | 31 (rif) | 35 (rif) | 37 (ene) | 45 (ene) | 44 (ene) | 46 (ene) |
| candidata: el PODER crece x1,25 (menos cartas) | 2 (pis) | 3 (rev) | 3 (rev) | 9 (rev) | 17 (esc) | 29 (rif) | 30 (rif) | 23 (ene) | 30 (ene) | 31 (ene) | 30 (ene) | 37 (ene) |
| candidata: vida x1,12 y golpe x1,05 desde la 11 | 2 (pis) | 2 (rev) | 9 (rev) | 19 (esc) | 19 (rif) | 30 (rif) | 29 (rif) | 34 (ene) | 26 (ene) | 31 (ene) | 37 (ene) | 37 (ene) |
| candidata: el cargador se llena al terminar cada oleada | 2 (pis) | 2 (rev) | 9 (rev) | 39 (esc) | 43 (rif) | 49 (ene) | 51 (ene) | 51 (ene) | 51 (ene) | 54 (ene) | 54 (ene) | 53 (ene) |
| candidata: oleadas de 10 + 3n, uno cada 0,25 s | 2 (pis) | 3 (rev) | 6 (rev) | 10 (esc) | 19 (esc) | 34 (rif) | 33 (ene) | 37 (ene) | 37 (ene) | 45 (ene) | 42 (ene) | 40 (ene) |
| candidata A: PODER x1,25, vida x1,12 y golpe x1,05, 10 + 3n cada 0,25 s | 2 (pis) | 2 (rev) | 5 (rev) | 12 (esc) | 19 (esc) | 27 (rif) | 29 (rif) | 32 (rif) | 29 (ene) | 23 (ene) | 38 (ene) | 24 (ene) |
| candidata B: la A con el cargador lleno en cada oleada | 2 (pis) | 2 (rev) | 5 (rev) | 12 (esc) | 19 (esc) | 39 (rif) | 49 (rif) | 43 (ene) | 51 (ene) | 49 (ene) | 59 (ene) | 49 (ene) |
| candidata C: la B con uno cada 0,20 s | 2 (pis) | 2 (rev) | 5 (rev) | 9 (esc) | 20 (esc) | 27 (rif) | 39 (rif) | 49 (ene) | 48 (ene) | 59 (ene) | 46 (ene) | 48 (ene) |
| candidata D: PODER x1,25, vida x1,14 y golpe x1,06, 10 + 2n cada 0,25 s, cargador lleno | 2 (pis) | 2 (pis) | 3 (rev) | 9 (rev) | 12 (esc) | 27 (esc) | 39 (rif) | 44 (rif) | 49 (ene) | 49 (ene) | 49 (ene) | 54 (ene) |
| candidata E: el PODER como esta, vida x1,15 y golpe x1,07, 10 + 2n cada 0,25 s, cargador lleno | 2 (pis) | 4 (pis) | 9 (rev) | 18 (esc) | 29 (rif) | 39 (rif) | 48 (ene) | 49 (ene) | 49 (ene) | 49 (ene) | 49 (ene) | 51 (ene) |
| 1.5.2 sin cartas, con el cargador lleno al terminar cada oleada | 1 (pis) | 2 (pis) | 2 (rev) | 2 (rev) | 3 (rev) | 5 (esc) | 6 (esc) | 9 (esc) | 9 (esc) | 17 (rif) | 19 (rif) | 22 (rif) |

### Perfil medio: las primeras 12 partidas (oleada a la que se llega, mediana; entre parentesis el arma con que se arranca)

| variante | p1 | p2 | p3 | p4 | p5 | p6 | p7 | p8 | p9 | p10 | p11 | p12 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| candidata: numeros a la mitad (danio x1,12, cadencia y vida x1,1, critico +5 %) | 7 (pis) | 39 (esc) | 49 (rif) | 72 (ene) | 80 (ene) | 77 (ene) | 80 (ene) | 77 (ene) | 83 (ene) | 78 (ene) | 80 (ene) | 83 (ene) |
| candidata: el PODER crece x1,25 (menos cartas) | 4 (pis) | 10 (rev) | 46 (esc) | 57 (ene) | 61 (ene) | 67 (ene) | 63 (ene) | 64 (ene) | 63 (ene) | 65 (ene) | 71 (ene) | 65 (ene) |
| candidata: vida x1,12 y golpe x1,05 desde la 11 | 5 (pis) | 28 (esc) | 43 (rif) | 57 (ene) | 59 (ene) | 59 (ene) | 64 (ene) | 66 (ene) | 63 (ene) | 65 (ene) | 65 (ene) | 68 (ene) |
| candidata: el cargador se llena al terminar cada oleada | 5 (pis) | 39 (esc) | 69 (rif) | 80 (ene) | 80 (ene) | 82 (ene) | 79 (ene) | 76 (ene) | 80 (ene) | 83 (ene) | 80 (ene) | 81 (ene) |
| candidata: oleadas de 10 + 3n, uno cada 0,25 s | 5 (pis) | 35 (rev) | 55 (rif) | 70 (ene) | 69 (ene) | 75 (ene) | 75 (ene) | 79 (ene) | 78 (ene) | 73 (ene) | 76 (ene) | 81 (ene) |
| candidata A: PODER x1,25, vida x1,12 y golpe x1,05, 10 + 3n cada 0,25 s | 5 (pis) | 17 (esc) | 39 (rif) | 41 (rif) | 53 (ene) | 58 (ene) | 53 (ene) | 49 (ene) | 58 (ene) | 57 (ene) | 47 (ene) | 51 (ene) |
| candidata B: la A con el cargador lleno en cada oleada | 5 (pis) | 17 (esc) | 36 (rif) | 54 (rif) | 52 (ene) | 57 (ene) | 58 (ene) | 57 (ene) | 62 (ene) | 54 (ene) | 54 (ene) | 56 (ene) |
| candidata C: la B con uno cada 0,20 s | 4 (pis) | 11 (rev) | 29 (esc) | 47 (rif) | 59 (ene) | 55 (ene) | 56 (ene) | 61 (ene) | 51 (ene) | 52 (ene) | 63 (ene) | 50 (ene) |
| candidata D: PODER x1,25, vida x1,14 y golpe x1,06, 10 + 2n cada 0,25 s, cargador lleno | 6 (pis) | 16 (esc) | 34 (esc) | 39 (rif) | 49 (ene) | 50 (ene) | 49 (ene) | 57 (ene) | 58 (ene) | 57 (ene) | 54 (ene) | 56 (ene) |
| candidata E: el PODER como esta, vida x1,15 y golpe x1,07, 10 + 2n cada 0,25 s, cargador lleno | 5 (pis) | 23 (rev) | 41 (rif) | 49 (rif) | 50 (ene) | 49 (ene) | 51 (ene) | 54 (ene) | 54 (ene) | 53 (ene) | 54 (ene) | 51 (ene) |
| 1.5.2 sin cartas, con el cargador lleno al terminar cada oleada | 3 (pis) | 3 (rev) | 7 (esc) | 9 (esc) | 19 (esc) | 26 (rif) | 29 (rif) | 39 (rif) | 49 (ene) | 49 (ene) | 49 (ene) | 49 (ene) |

### Perfil alto: las primeras 12 partidas (oleada a la que se llega, mediana; entre parentesis el arma con que se arranca)

| variante | p1 | p2 | p3 | p4 | p5 | p6 | p7 | p8 | p9 | p10 | p11 | p12 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| candidata: numeros a la mitad (danio x1,12, cadencia y vida x1,1, critico +5 %) | 13 (pis) | 79 (esc) | 99 (ene) | 99 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) |
| candidata: el PODER crece x1,25 (menos cartas) | 6 (pis) | 47 (esc) | 77 (ene) | 90 (ene) | 96 (ene) | 92 (ene) | 100 (ene) | 92 (ene) | 96 (ene) | 99 (ene) | 100 (ene) | 100 (ene) |
| candidata: vida x1,12 y golpe x1,05 desde la 11 | 9 (pis) | 59 (esc) | 79 (ene) | 84 (ene) | 79 (ene) | 89 (ene) | 89 (ene) | 79 (ene) | 89 (ene) | 89 (ene) | 79 (ene) | 89 (ene) |
| candidata: el cargador se llena al terminar cada oleada | 9 (pis) | 79 (esc) | 100 (ene) | 99 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) |
| candidata: oleadas de 10 + 3n, uno cada 0,25 s | 29 (pis) | 82 (rif) | 100 (ene) | 100 (ene) | 99 (ene) | 99 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) | 100 (ene) |
| candidata A: PODER x1,25, vida x1,12 y golpe x1,05, 10 + 3n cada 0,25 s | 7 (pis) | 25 (esc) | 59 (rif) | 68 (ene) | 69 (ene) | 77 (ene) | 71 (ene) | 74 (ene) | 74 (ene) | 78 (ene) | 82 (ene) | 77 (ene) |
| candidata B: la A con el cargador lleno en cada oleada | 7 (pis) | 25 (esc) | 58 (rif) | 69 (ene) | 72 (ene) | 77 (ene) | 69 (ene) | 74 (ene) | 73 (ene) | 69 (ene) | 77 (ene) | 76 (ene) |
| candidata C: la B con uno cada 0,20 s | 11 (pis) | 34 (esc) | 55 (rif) | 62 (ene) | 62 (ene) | 70 (ene) | 73 (ene) | 75 (ene) | 70 (ene) | 71 (ene) | 70 (ene) | 77 (ene) |
| candidata D: PODER x1,25, vida x1,14 y golpe x1,06, 10 + 2n cada 0,25 s, cargador lleno | 9 (pis) | 30 (esc) | 47 (rif) | 58 (ene) | 57 (ene) | 62 (ene) | 69 (ene) | 60 (ene) | 67 (ene) | 59 (ene) | 64 (ene) | 67 (ene) |
| candidata E: el PODER como esta, vida x1,15 y golpe x1,07, 10 + 2n cada 0,25 s, cargador lleno | 16 (pis) | 48 (esc) | 53 (ene) | 57 (ene) | 62 (ene) | 59 (ene) | 65 (ene) | 64 (ene) | 66 (ene) | 69 (ene) | 68 (ene) | 65 (ene) |
| 1.5.2 sin cartas, con el cargador lleno al terminar cada oleada | 5 (pis) | 12 (esc) | 29 (esc) | 39 (rif) | 55 (ene) | 59 (ene) | 59 (ene) | 59 (ene) | 63 (ene) | 66 (ene) | 65 (ene) | 67 (ene) |

## Las cartas

### Cartas elegidas al terminar cada oleada (promedio de todas las partidas que la terminan)

| variante | perfil | 1 | 2 | 3 | 4 | 5 | 6 | 8 | 10 | 15 | 20 | 30 | 40 | 50 | 60 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| candidata: numeros a la mitad (danio x1,12, cadencia y vida x1,1, critico +5 %) | bajo | 1.0 | 2.0 | 3.0 | 4.1 | 6.0 | 7.0 | 9.4 | 12.0 | 16.0 | 19.0 | 24.0 | 28.0 | 31.0 | 33.0 |
| candidata: numeros a la mitad (danio x1,12, cadencia y vida x1,1, critico +5 %) | medio | 1.0 | 2.0 | 3.0 | 4.3 | 6.0 | 7.0 | 9.5 | 12.0 | 16.0 | 19.0 | 24.0 | 28.0 | 31.0 | 33.0 |
| candidata: numeros a la mitad (danio x1,12, cadencia y vida x1,1, critico +5 %) | alto | 1.0 | 2.0 | 3.0 | 4.3 | 6.0 | 7.0 | 9.5 | 12.0 | 16.0 | 19.0 | 24.0 | 28.0 | 31.0 | 33.0 |
| candidata: el PODER crece x1,25 (menos cartas) | bajo | 1.0 | 2.0 | 3.0 | 4.0 | 5.0 | 6.0 | 7.8 | 9.0 | 12.0 | 14.0 | 17.0 | 20.0 | 21.0 | 23.0 |
| candidata: el PODER crece x1,25 (menos cartas) | medio | 1.0 | 2.0 | 3.0 | 4.0 | 5.0 | 6.0 | 7.8 | 9.0 | 12.0 | 14.0 | 17.0 | 20.0 | 21.0 | 23.0 |
| candidata: el PODER crece x1,25 (menos cartas) | alto | 1.0 | 2.0 | 3.0 | 4.0 | 5.0 | 6.0 | 7.9 | 9.0 | 12.0 | 14.0 | 17.0 | 20.0 | 21.0 | 23.0 |

### Perfil bajo: cuantas veces tiene cada carta al morir en el muro (promedio de las ultimas 20 partidas)

| variante | cadencia | critico | dano | disparo_doble | dron | escudo | explosiva | furia_larga | granada_rapida | hacia_atras | hielo | iman | perforante | quemadura | vampiro | velocidad | vida | doradas |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| candidata: numeros a la mitad (danio x1,12, cadencia y vida x1,1, critico +5 %) | 3.9 | 0.9 | 4.0 | 2.0 | 1.9 | 1.0 | 2.0 | 1.2 | 1.4 | 1.0 | 2.0 | 0.8 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 4.0 |
| candidata: el PODER crece x1,25 (menos cartas) | 2.0 | 0.1 | 2.2 | 1.8 | 1.4 | 0.9 | 1.5 | 0.1 | 0.2 | 0.8 | 1.4 | 0.1 | 1.7 | 1.2 | 1.6 | 1.6 | 2.3 | 2.9 |
| candidata: vida x1,12 y golpe x1,05 desde la 11 | 3.6 | 0.3 | 3.6 | 2.0 | 1.8 | 1.0 | 1.9 | 0.6 | 0.9 | 0.9 | 1.9 | 0.2 | 2.0 | 1.9 | 1.9 | 1.9 | 2.8 | 3.0 |
| candidata: el cargador se llena al terminar cada oleada | 4.0 | 2.2 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 1.9 | 1.9 | 1.0 | 2.0 | 1.6 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 5.3 |
| candidata: oleadas de 10 + 3n, uno cada 0,25 s | 3.8 | 0.5 | 3.8 | 2.0 | 1.8 | 1.0 | 1.9 | 0.8 | 1.1 | 1.0 | 1.9 | 0.6 | 2.0 | 1.9 | 1.9 | 1.9 | 2.9 | 4.0 |
| candidata A: PODER x1,25, vida x1,12 y golpe x1,05, 10 + 3n cada 0,25 s | 1.8 | 0.0 | 1.9 | 1.4 | 1.2 | 0.9 | 1.3 | 0.1 | 0.1 | 0.7 | 1.1 | 0.1 | 1.7 | 1.1 | 1.4 | 1.2 | 2.0 | 2.3 |
| candidata B: la A con el cargador lleno en cada oleada | 2.6 | 0.2 | 2.8 | 1.7 | 1.6 | 0.9 | 1.7 | 0.1 | 0.1 | 0.9 | 1.6 | 0.0 | 1.7 | 1.3 | 1.8 | 1.6 | 2.4 | 3.7 |
| candidata C: la B con uno cada 0,20 s | 2.5 | 0.1 | 2.6 | 1.7 | 1.6 | 0.9 | 1.8 | 0.2 | 0.2 | 0.9 | 1.6 | 0.1 | 1.8 | 1.5 | 1.7 | 1.6 | 2.4 | 3.9 |
| candidata D: PODER x1,25, vida x1,14 y golpe x1,06, 10 + 2n cada 0,25 s, cargador lleno | 2.2 | 0.0 | 2.5 | 1.6 | 1.5 | 0.9 | 1.7 | 0.1 | 0.2 | 0.8 | 1.3 | 0.1 | 1.7 | 1.3 | 1.6 | 1.6 | 2.4 | 3.6 |
| candidata E: el PODER como esta, vida x1,15 y golpe x1,07, 10 + 2n cada 0,25 s, cargador lleno | 3.7 | 0.3 | 3.7 | 2.0 | 1.9 | 1.0 | 1.9 | 0.7 | 0.8 | 0.9 | 1.9 | 0.3 | 2.0 | 1.9 | 1.9 | 1.9 | 2.9 | 4.1 |

### Perfil medio: cuantas veces tiene cada carta al morir en el muro (promedio de las ultimas 20 partidas)

| variante | cadencia | critico | dano | disparo_doble | dron | escudo | explosiva | furia_larga | granada_rapida | hacia_atras | hielo | iman | perforante | quemadura | vampiro | velocidad | vida | doradas |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| candidata: numeros a la mitad (danio x1,12, cadencia y vida x1,1, critico +5 %) | 4.0 | 3.0 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 5.9 |
| candidata: el PODER crece x1,25 (menos cartas) | 3.3 | 0.2 | 3.2 | 1.9 | 1.8 | 1.0 | 1.9 | 0.4 | 0.3 | 0.9 | 1.8 | 0.1 | 1.9 | 1.8 | 1.8 | 1.8 | 2.8 | 4.7 |
| candidata: vida x1,12 y golpe x1,05 desde la 11 | 4.0 | 2.8 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 1.0 | 2.0 | 1.9 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 5.8 |
| candidata: el cargador se llena al terminar cada oleada | 4.0 | 3.0 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 6.2 |
| candidata: oleadas de 10 + 3n, uno cada 0,25 s | 4.0 | 2.9 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 1.0 | 2.0 | 1.9 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 6.7 |
| candidata A: PODER x1,25, vida x1,12 y golpe x1,05, 10 + 3n cada 0,25 s | 2.6 | 0.1 | 2.6 | 1.7 | 1.7 | 0.9 | 1.7 | 0.2 | 0.2 | 0.9 | 1.5 | 0.1 | 1.8 | 1.6 | 1.7 | 1.6 | 2.5 | 3.7 |
| candidata B: la A con el cargador lleno en cada oleada | 2.7 | 0.1 | 2.8 | 1.7 | 1.6 | 0.9 | 1.8 | 0.2 | 0.3 | 0.9 | 1.6 | 0.1 | 1.8 | 1.4 | 1.8 | 1.7 | 2.5 | 4.0 |
| candidata C: la B con uno cada 0,20 s | 2.7 | 0.1 | 2.9 | 1.8 | 1.7 | 1.0 | 1.7 | 0.2 | 0.4 | 0.8 | 1.6 | 0.1 | 1.9 | 1.4 | 1.8 | 1.6 | 2.5 | 4.0 |
| candidata D: PODER x1,25, vida x1,14 y golpe x1,06, 10 + 2n cada 0,25 s, cargador lleno | 2.4 | 0.1 | 2.6 | 1.6 | 1.6 | 0.9 | 1.7 | 0.1 | 0.2 | 0.8 | 1.4 | 0.0 | 1.8 | 1.4 | 1.7 | 1.6 | 2.4 | 3.9 |
| candidata E: el PODER como esta, vida x1,15 y golpe x1,07, 10 + 2n cada 0,25 s, cargador lleno | 3.7 | 0.5 | 3.8 | 2.0 | 2.0 | 1.0 | 2.0 | 0.7 | 0.8 | 1.0 | 2.0 | 0.4 | 2.0 | 1.9 | 1.9 | 1.9 | 2.9 | 4.2 |

### Perfil alto: cuantas veces tiene cada carta al morir en el muro (promedio de las ultimas 20 partidas)

| variante | cadencia | critico | dano | disparo_doble | dron | escudo | explosiva | furia_larga | granada_rapida | hacia_atras | hielo | iman | perforante | quemadura | vampiro | velocidad | vida | doradas |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| candidata: numeros a la mitad (danio x1,12, cadencia y vida x1,1, critico +5 %) | 4.0 | 3.0 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 6.0 |
| candidata: el PODER crece x1,25 (menos cartas) | 3.6 | 0.7 | 3.8 | 2.0 | 2.0 | 1.0 | 2.0 | 0.6 | 0.6 | 1.0 | 1.9 | 0.6 | 2.0 | 1.9 | 1.9 | 1.9 | 2.9 | 6.2 |
| candidata: vida x1,12 y golpe x1,05 desde la 11 | 4.0 | 3.0 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 5.9 |
| candidata: el cargador se llena al terminar cada oleada | 4.0 | 3.0 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 5.9 |
| candidata: oleadas de 10 + 3n, uno cada 0,25 s | 4.0 | 3.0 | 4.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 1.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 6.7 |
| candidata A: PODER x1,25, vida x1,12 y golpe x1,05, 10 + 3n cada 0,25 s | 3.2 | 0.2 | 3.3 | 1.9 | 1.8 | 1.0 | 1.9 | 0.3 | 0.4 | 0.9 | 1.8 | 0.3 | 1.9 | 1.7 | 1.7 | 1.8 | 2.8 | 5.0 |
| candidata B: la A con el cargador lleno en cada oleada | 3.1 | 0.2 | 3.3 | 1.8 | 1.8 | 1.0 | 1.8 | 0.3 | 0.3 | 0.9 | 1.8 | 0.2 | 1.9 | 1.7 | 1.9 | 1.9 | 2.6 | 5.1 |
| candidata C: la B con uno cada 0,20 s | 3.0 | 0.2 | 3.3 | 1.9 | 1.8 | 1.0 | 1.9 | 0.3 | 0.3 | 0.8 | 1.7 | 0.2 | 1.9 | 1.8 | 1.9 | 1.8 | 2.8 | 5.0 |
| candidata D: PODER x1,25, vida x1,14 y golpe x1,06, 10 + 2n cada 0,25 s, cargador lleno | 2.5 | 0.1 | 2.6 | 1.9 | 1.7 | 0.9 | 1.8 | 0.2 | 0.3 | 0.9 | 1.7 | 0.1 | 1.8 | 1.4 | 1.7 | 1.7 | 2.5 | 4.3 |
| candidata E: el PODER como esta, vida x1,15 y golpe x1,07, 10 + 2n cada 0,25 s, cargador lleno | 4.0 | 1.2 | 3.9 | 2.0 | 2.0 | 1.0 | 2.0 | 1.4 | 1.4 | 1.0 | 2.0 | 1.2 | 2.0 | 2.0 | 2.0 | 2.0 | 3.0 | 5.6 |

