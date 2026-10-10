# La curva de las oleadas (1.5.2, 10/10)

`curva.py` simula la economía de hoy partida por partida: se juega, se cobran las monedas y se compra entre partidas.
Sirve para comparar variantes de la curva y del arranque. **Los números absolutos no son confiables**: el modelo no ve
el mapa, y cuánto esquiva o junta cada perfil está puesto a ojo. Lo que sí sirve es la comparación entre variantes,
corridas con el mismo modelo y las mismas semillas.

```
python curva.py partidas=300 semillas=6 perfiles=bajo,medio,alto hilos=15 salida=resultados_curva.md
python curva.py partidas=300 semillas=6 perfiles=medio extra=1.5 salida=resultados_curva_extra15.md
python curva.py modo=arranque semillas=8 perfiles=bajo,medio,alto salida=resultados_arranque.md
```

## Lo que encontró

- **Hoy (vida ×1,11 siempre) el jugador medio se frena en la 40-45**, como decía la simulación del 19/9.
- **Lo que frena es el jefe**: entre el 60 y el 100 % de las muertes en el muro son en oleadas de jefe. Su vida crece
  ×1,11 por oleada y su golpe ×1,07 (la carga pega ×2,5), mientras la vida comprable crece lineal. En la 40, una carga
  saca unos 350 de una vida de 400-700. La presión de la horda común (zombis que llegan sobre zombis que se matan)
  queda por debajo de 1: el jugador los mata más rápido de lo que llegan.
- **La cola de vida de Gemini corre el muro**: ×1,04-1,05 desde la 25 lo lleva a la 55-60. Con ×1,03 casi no queda
  muro, y lo que mata pasa a ser **el golpe de los zombis**: el jugador termina aguantando 4-8 golpes de un rápido.
- **Suavizar también el golpe** (×1,03 desde la 25) ataca los dos muros: el jugador aguanta 27-31 golpes y las muertes
  en el jefe bajan al 41-60 %.
- **Los hitos de daño** (×1,5 cada 5 niveles, de la superauditoría del 29/9) rinden como una cola ×1,04, pero llevan
  código en la tienda. **Juntos con la cola**, el jugador se dispara sin techo.
- **El arranque**: con la cadencia base en 6 (6 + 1 por nivel, hasta 22), las primeras partidas llegan 2-3 oleadas más
  lejos. Con daño 2 y cadencia 6, bastante más; la primera compra de daño pasa a ser +50 % y no ×2.

Los resultados están en `resultados_*.md`.
