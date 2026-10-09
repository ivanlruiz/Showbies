# Cartas de la partida (diseño para la 1.6.0)

Elegir 1 de 3 cartas entre oleadas, como en Archero. Lo pidió Jeff en Discord el 8/10 y ya estaba en `TAREAS.md`
(Ideas de Archero, 19/9). Esto es el diseño para que Ivan lo revise: **todavía no hay código**. Las preguntas que
deciden cosas están al final.

## Qué es

Al terminar ciertas oleadas el juego se congela (como la ventanita de revivir) y aparecen **tres cartas**. Se toca una,
se aplica en el acto y **dura toda la partida**, sumándose a las anteriores. Morir o REINICIAR las pierde.

- **La tienda es lo permanente y las cartas son la partida.** Una carta nunca toca `Progreso` como mejora: multiplica
  encima de lo que ya dejó `AplicarMejoras`, igual que hoy la caja de balas y la furia multiplican aparte en el arma.
- **Ayuda con dos cosas que se quejaron**: la oleada 5 sin mejoras se hace difícil (Jeff), y el muro de las oleadas
  altas (`CLAUDE.md`, Los zombis escalan con la oleada), donde el daño comprable ya no alcanza a la vida.
- **Solo en el modo oleadas** al principio. El libre podría darlas al subir de nivel, después.

## Cuándo salen

Propuesta: **al terminar cada 3 oleadas** (3, 6, 9...) y **después de cada jefe** (10, 20, 30...) una elección mejor
(ver Raras). Hasta la oleada 30 son 12 cartas. Cada oleada sería mucho: con oleadas de 30-60 s en el arranque, se
pasaría más tiempo eligiendo que jugando.

La elección va **antes del cartel de la oleada siguiente**, en el descanso (`WaveManager`, paso 1): el cartel y el
bono esperan a que se elija. Con el juego congelado no corren el descanso ni las cajas.

## Las cartas (unas 14 para empezar)

Cada una con su tope de veces. Solo salen las que se pueden usar (las de granada y furia, si están compradas).

| carta | efecto por vez | tope | dónde se engancha |
|---|---|---|---|
| Disparo doble | +1 bala por tiro, en abanico de 8° | 2 | `GunController` (tiros por cada tiro del acumulador) |
| Atravesar | la bala sigue después de pegar, 1 zombi más | 3 | `BulletController.OnCollisionEnter` (hoy vuelve al pool al pegar) |
| Rebote | la bala rebota hacia el zombi más cercano | 2 | `BulletController` (nuevo rumbo en vez de volver al pool) |
| Hacia atrás | una bala más por la espalda | 1 | `GunController` |
| Daño | daño ×1,25 | 4 | `GunController`, multiplicador aparte como `FijarFuria` |
| Cadencia | tiros/s ×1,2 | 4 | `GunController`, respeta `maxTirosPorSegundo` |
| Crítico | +10 % de crítico | 3 | `GunController.EsCritico` |
| Vida | vida máxima ×1,2 y cura entera | 3 | `PlayerHealth` |
| Vampiro | cada 20 muertes cura el 5 % | 2 | `EnemyController.DanoZombi`, en el bloque de muerte |
| Escudo | bloquea un golpe cada 15 s | 1 | `PlayerHealth.TakeDamage` |
| Velocidad | +10 % | 2 | `PlayerController.multiplicadorVelocidad` (lo usa la furia: se multiplican) |
| Imán | +2 m | 2 | `Moneda.FijarRadioIman` (sin imán comprado, da 2 m) |
| Granada rápida | recarga −30 % | 2 | `PlayerController.granadaCooldown` |
| Furia larga | la furia dura +2 s | 2 | `Furia` |

**Raras (después del jefe)**: una de las tres sale dorada y vale el doble, o se cura entero y se elige igual (el
"ángel del jefe" de las notas del 19/9). Las combinaciones secretas (fuego + rebote) quedan para después.

**Monedas no**: una carta de botín haría que la jugada óptima sea elegirla siempre, y las monedas ya escalan con la
oleada. Las cartas son para llegar más lejos, y llegar más lejos ya da más monedas.

## Cómo se ve

- Una ventana de carbón neón como la tienda (`ConstructorUI`), con tres tarjetas que entran girando, una tras otra,
  con el jingle del cartel. Cada tarjeta: icono, nombre, qué hace ("DAÑO ×1,25") y cuántas veces la tiene ("2/4").
- Tocar una la hace saltar, las otras dos se van, y la elegida vuela a una fila de iconos chiquitos del HUD (abajo
  del renglón de los FPS), que es lo que se lleva en la partida.
- Sin reloj: es una pausa. Escape / atrás no la cierra (hay que elegir). La pausa no se abre encima.
- Textos nuevos en `Textos.txt` (`carta_<id>_nombre`, `carta_<id>`), en inglés y español de España.

## Lo que se guarda (y la trampa de salir)

La partida de oleadas se retoma (`Progreso.GuardarOleadaEnCurso`), y la 1.5.0 suma **MEJORAS en la pausa**, que sale
de la partida y vuelve a la misma oleada. Si las cartas no se guardaran, salir a comprar costaría todas las cartas y
el botón nuevo no serviría. Entonces:

- **Las cartas elegidas se guardan con la partida en curso** (una lista de ids en el progreso, al lado de
  `oleadaEnCurso`), y se olvidan donde se olvida la partida (morir, REINICIAR).
- **Las tres que se ofrecen salen de una semilla de la partida y la oleada**: salir y volver a entrar no las cambia
  (si no, salir sería la forma de buscar las que uno quiere).
- **Una elección pendiente se guarda**: si se cierra la app con las cartas en pantalla, vuelven al retomar.
- La versión del JSON sube (un build viejo abre el archivo en solo lectura, como siempre).

## El balance

Las cartas hacen las partidas más largas, y eso mueve todo lo que se mide con la mejor oleada: la recompensa diaria,
las misiones, el bestiario y Halloween (`Economia`). No se rompe nada (todo se mide "de menos"), pero la curva de
dificultad de las oleadas altas hay que volver a mirarla con las cartas puestas: una simulación como la del 19/9, con
un jugador que elige bien. Puede que haya que subir `crecimientoVida` de las oleadas, o que las cartas de daño sean
más chicas. Se hace antes de publicar, no después.

## Pruebas

- Prueba de lógica: el sorteo (sin pasar topes, sin cartas que no se pueden usar, la misma semilla da las mismas tres),
  lo que aplica cada carta sobre el arma y el jugador (con los números de arriba, y que se apilen bien con la caja de
  balas y la furia), el guardado y la migración.
- Banco en play: completar la 3, elegir, salir con MEJORAS, volver y ver que la carta sigue; morir y ver que se fue.
- Atravesar y rebotar tocan el pool de balas y el conteo de muertes: un banco que cuente que ningún zombi muera dos
  veces ni sume puntos dobles.

## Lo que hay que decidir

1. **¿Cada cuántas oleadas?** Propuesta: cada 3 y después de cada jefe.
2. **¿Un vídeo para cambiar las tres cartas?** Sería un lugar nuevo de anuncios con premio, en una pausa real del
   juego, pero hoy la regla es que los vídeos con premio salen solo en la derrota y en el menú. Se puede abrir esa
   puerta o no.
3. **¿También en el modo libre?** Propuesta: no en la 1.6.0.
4. **¿Las cartas raras después del jefe?** Propuesta: sí, es poco trabajo y le da algo más a la oleada del jefe.

Estimación: una semana de trabajo, la mitad en atravesar/rebotar y en el balance.
