# Tareas pendientes de ShowBies

Lista de Ivan del 16/9/2026, para después de subir la prueba cerrada. Todavía no se empezó ninguna.

## Visual

- [ ] **Cambiar la fuente de los textos.** Hoy todo usa Bangers. Elegir la nueva y reemplazarla en todas las
      pantallas (menú, HUD, tienda, derrota, revivir, tutorial y números flotantes).
- [x] **Cambiar el fondo** del menú: zombis cruzando sobre el piso de la partida y monedas cayendo.
- [x] **Paleta de colores más clara** en todo el juego: pasto de día, cielo celeste y paneles crema.

## Menú

- [x] **Sacar las monedas del menú principal:** que se vean solo en la tienda de mejoras.
- [x] **Reacomodar los botones del menú.** PLAY abajo a la derecha, y al tocarlo que pregunte qué modo jugar:
      modo libre, oleadas o tutorial. El tutorial deja de ser un botón aparte del menú.

## Juego

- [x] **Más zombis por oleada y que aparezcan más rápido** (`zombisBase`, `zombisPorOleada` e
      `intervaloEntreApariciones` del `WaveManager` de WaveMode). Ojo con el techo de zombis vivos en el celular (35).
- [ ] **Mapas diferentes.** Hoy el modo libre y las oleadas usan el mismo piso.

## Sonido

- [x] **Volumen de efectos y de música por separado,** con un lugar en la UI para cambiarlo (hoy no hay pantalla
      de opciones). La música de partida se sacó; definir si vuelve con su propio volumen.

## Mejoras

- [ ] **Power-ups en la tienda:** que las cajas (balas, vida y arma) se puedan mejorar con monedas, por ejemplo más
      duración, más efecto o que aparezcan más seguido.

## Retención (para que vuelvan cada día)

- [x] **Recompensa diaria:** monedas por entrar, que crecen si entrás varios días seguidos.
- [ ] **Misiones:** 3 por día del tipo "matá 200 zombis", "llegá a la oleada 10" o "usá la furia 3 veces". Dan monedas y
      le ponen un objetivo a cada partida.
- [ ] **Logros:** hitos permanentes (primer jefe, 10.000 zombis, crítico al 100 %). Se pueden conectar con Google Play
      Games.
