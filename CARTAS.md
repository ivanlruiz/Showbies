# Cartas de la partida (diseño para la 1.6.0)

Matar zombis da experiencia; al llenar la barra se sube un nivel de **PODER**, el juego se congela y aparecen **tres
cartas**. Se elige una, se aplica en el acto y dura toda la partida. Es el corazón de la 1.6.0: en la revisión de
Gemini del 10/10 salió como lo que más retiene ("una partida más, a ver si me toca la build"), por encima de todo lo
demás. Lo pidió también Jeff en Discord el 8/10.

Este documento reemplaza al del 9/10, que daba las cartas cada 3 oleadas: Ivan eligió el plan de Gemini, por
experiencia. Las seis decisiones las tomó Gemini a pedido de Ivan (10/10): están al final.

## Qué es

- **La tienda es lo permanente y las cartas son la partida.** Una carta nunca toca `Progreso` como mejora: multiplica
  encima de lo que dejó `AplicarMejoras`, como hoy la caja de balas y la furia multiplican aparte en el arma. Morir o
  REINICIAR las pierde.
- **Las cartas cambian cómo se juega, no solo los números**: además de daño y cadencia, balas que atraviesan, que
  rebotan, que salen de a dos, que congelan o que saltan de un zombi a otro, un dron que dispara solo. Es lo que pidió
  Gemini, y lo que hace que dos partidas no sean iguales.
- **En los dos modos**: oleadas y libre. En el libre le da a la partida un objetivo que hoy no tiene.

## La experiencia y el PODER

- **Cada zombi da experiencia al morir**, en el mismo bloque de `DanoZombi` que suma los puntos (la regla de siempre:
  nada se suma desde otro lado). Normal, rápido y veloz 1; tanque 3; tesoro 5; jefe 15; los invocados por el jefe, 1.
  **Va en un campo `experiencia` de cada `Enemy`** (con 1 si no se carga, como `puntos`): tocar el balance no
  recompila, y la prueba de lógica arma la tabla de la curva leyendo los assets.
- **El tutorial no da experiencia ni abre cartas** (sus zombis mueren por `DanoZombi`), y **los bancos en play tampoco**:
  `RespaldoDelBanco.SigueArmado` prende `SinCartasAutomaticas` en cada cuadro, como `WaveManager.SinTesoroAutomatico`.
  Sin esto, cualquier banco que mata zombis quedaría congelado con tres cartas en pantalla. Va primero en la etapa 1.
- **Cómo llega al jugador** (decisión 1): directo al matar, con una chispa que vuela del zombi al jugador, o con
  cristales en el piso que hay que juntar, como las monedas. Recomiendo **directo**: las monedas ya son el "ir a
  juntar", los cristales compartirían su techo de 150/80 en escena (con 35 zombis el piso se llena) y la experiencia
  perdida por no pasar cerca frustra justo al que está aprendiendo.
- **El nombre**: el nivel de la partida se llama **PODER** ("¡PODER 4!"), porque ya existen el NIVEL del jugador (la
  cuenta) y el "Nivel N" del modo libre (decisión 2).
- **La barra**: fina, con "PODER N" chico al costado, **donde entre sin pisar nada, medido en 16:9, 20:9 y 21:9** (la
  prueba de lógica ya mide esas franjas). Arriba al centro es la franja más llena del HUD (el botón de pausa del
  teléfono, el cartel de la oleada, la barra del jefe, el del capítulo); los candidatos son encima de la vida, abajo
  al centro, o pegada al bloque de arriba a la izquierda. Se elige con maquetas.

### La curva

Pasar del PODER L al siguiente cuesta `round(8 × 1,15^L)` de experiencia: la primera carta a los 8 zombis (unos 20 s
de la primera oleada), y después cada vez más espaciadas. Con la mezcla de zombis de WaveMode:

| hasta la oleada | 1 | 2 | 3 | 5 | 10 | 15 | 20 | 30 | 40 | 50 | 60 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| cartas | 1 | 3 | 5 | 8 | 14 | 19 | 22 | 27 | 31 | 34 | 36 |

Gemini pedía 12-15 hasta la 10. Al principio es una carta cada uno o dos minutos; en las oleadas altas, una cada dos o
tres oleadas. En el libre la experiencia sale igual, de los zombis que se matan.

## La elección

- **El juego se congela** como con la oferta de revivir (`MenuPausa.JuegoCongelado` suma la elección): no corren los
  zombis, las cajas, la furia ni los relojes del jefe. La pausa no se abre encima y Escape no la cierra: hay que
  elegir.
- **Tres tarjetas de carbón neón** que entran girando una tras otra, con el jingle del cartel (`ConstructorUI`). Cada
  una: su dibujo, el nombre, lo que hace ("DAÑO ×1,25") y cuántas veces se tiene ("2/4"). Tocar una la hace saltar, las
  otras se van, y la elegida vuela a una fila de íconos chicos del HUD.
- **Si se sube más de un nivel de golpe** (una granada que mata a seis tanques), salen dos elecciones seguidas.
- **La elección mira lo mismo que la derrota**: nunca se abre con el jugador muerto (`PlayerHealth.EstaMuerto`), con la
  oferta de revivir o la derrota en pantalla, ni con la pausa abierta; **queda pendiente** y se abre en cuanto se
  puede (al revivir, al reanudar). Así nunca hay dos cosas congelando y descongelando el `timeScale` (el caso: la
  granada que llena la barra en el mismo paso de física en que un zarpazo mata al jugador). Sumada a
  `MenuPausa.OtroLoCongela`, la pausa de impacto de `Efectos` ya no le devuelve el `timeScale` a 1 por detrás.
- **El sorteo**: solo cartas que no llegaron a su tope y que se pueden usar (las de granada y furia, si están
  compradas). Con todo al tope sale **CURACIÓN** (vida entera), que no tiene tope.
- **Después de cada jefe, una de las tres sale dorada y vale el doble** (decisión 5): con el marco que brilla mucho y
  un sonido propio, épico (pedido de Gemini).
- **Sin cartas de monedas**: la jugada óptima sería elegirlas siempre, y llegar más lejos ya da más monedas.

## Las cartas

Cada una con su tope. Las de números (las del diseño anterior) y las que cambian las reglas (las de Gemini):

| carta | qué hace por vez | tope | dónde se engancha |
|---|---|---|---|
| Daño | daño ×1,25 | 4 | `GunController`, un multiplicador aparte como `FijarFuria` |
| Cadencia | tiros/s ×1,2 | 4 | `GunController`, respeta `maxTirosPorSegundo` |
| Crítico | +10 % de crítico | 3 | `GunController.EsCritico` |
| Vida | vida máxima ×1,2 y cura entera | 3 | `PlayerHealth` |
| Velocidad | +10 % | 2 | `PlayerController.multiplicadorVelocidad` (se multiplica con la furia) |
| Imán | +2 m (sin imán comprado, da 2 m) | 2 | `Moneda.FijarRadioIman` |
| Granada rápida | recarga −30 % | 2 | `PlayerController.granadaCooldown` (si está comprada) |
| Furia larga | la furia dura +2 s | 2 | `Furia` (si está comprada) |
| **Perforante** | la bala sigue después de pegar, con la mitad del daño, 1 zombi más | 2 | `BulletController.OnCollisionEnter` (hoy vuelve al pool al pegar) |
| **Disparo doble** | +1 bala por tiro, en abanico de 8° | 2 | `GunController`, al sacar cada bala del pool |
| **Hacia atrás** | una bala más por la espalda | 1 | `GunController` |
| **Explosiva** | daño en área chico al pegar (1,5 m, 30 % del daño) | 2 | `BulletController`, con las chispas de `Efectos` y el dedup por componente de la granada |
| **Hielo** | el zombi que recibe un tiro va 40 % más lento 2 s, teñido de celeste | 2 | `EnemyController`, un multiplicador de velocidad con su reloj |
| **Quemadura** | 1 de cada 4 balas quema al zombi: 10 % del daño base por segundo durante 3 s | 2 | `EnemyController`, un reloj y un daño por segundo, como el hielo |
| **Dron** | una esfera que orbita al jugador y dispara sola al más cercano, 1 tiro/s | 2 | componente nuevo en el jugador, con las balas del pool |
| **Vampiro** | cada 15 muertes cura el 5 % | 2 | `DanoZombi`, en el bloque de muerte |
| **Escudo** | bloquea un golpe cada 15 s, con un anillo que se ve, y al romperse empuja medio metro a los zombis pegados | 1 | `PlayerHealth.TakeDamage`, y el empuje como el del jefe a los invocados |
| Curación | vida entera | — | solo cuando todo lo demás está al tope |

Son 38 elecciones antes de que salga Curación: alcanzan hasta cerca de la oleada 50 de la tabla de arriba. Los números son
de partida y se ajustan con la simulación (ver El balance).

**Atravesar, rebotar, explotar y saltar tocan el conteo de muertes**: un zombi no puede morir dos veces ni sumar
puntos dobles (la guarda `estaMuerto` de `DanoZombi` ya lo cubre, y el daño en área deduplica por componente, como la
granada). La bala que sigue viva no vuelve al pool hasta terminar. **Y no le puede pegar dos veces al mismo zombi**:
cada zombi tiene tres colliders (la cápsula y dos cubos), y una bala que sigue entra en uno y después en otro; hoy no
pasa porque vuelve al pool en el primero. La bala que perfora, rebota o salta se acuerda de los `EnemyController` que ya
tocó, no de los colliders.

**Disparo doble y Hacia atrás gastan una sola bala del cargador por tiro**: si gastaran una por bala, con 500 de
arranque la carta sería un castigo. **El hielo frena la persecución, no los patrones del jefe**: el jefe se mueve solo
(`IMovimientoPropio`) y su carga va a 16 m/s; congelado camina más lento hacia el jugador, pero salta y embiste igual.

## Lo que se guarda (y la trampa de salir)

La partida de oleadas se retoma (`Progreso.GuardarOleadaEnCurso`), y MEJORAS de la pausa sale de la partida y vuelve.
Si las cartas no se guardaran, salir a comprar costaría todas las cartas. Entonces:

- **Con la partida en curso se guardan** las cartas elegidas (ids y veces), el PODER, la experiencia, las elecciones
  pendientes, la semilla y si toca la dorada, y se olvidan donde se olvida la partida (morir, REINICIAR). Retomar
  arranca la oleada de cero, pero con las cartas.
- **Se guarda una foto al empezar cada oleada**, con los puntos (`WaveManager`, `Progreso.GuardarPoderEnCurso`), y no en
  cada elección como decía la primera versión de este documento: retomar arranca la oleada de cero, con todos sus
  zombis, y si se guardara lo ganado a mitad de oleada, esos zombis darían su experiencia dos veces (salir por MEJORAS
  y volver sería una forma de sacar cartas gratis). **Lo de mitad de oleada no se pierde**: al jugarla de nuevo se
  vuelve a ganar, y con la misma semilla y el mismo PODER salen las mismas tres cartas.
- **Lo de la partida es `static`** (lo leen el arma, el jugador y las balas): va al reset de `SubsystemRegistration`,
  o la segunda partida del editor arrancaría con las cartas de la primera.
- **Las tres que se ofrecen salen de una semilla de la partida y del PODER**: salir y volver no las cambia.
- **Una elección pendiente se guarda** si quedó de la oleada anterior: vuelve al retomar.
- **La versión del JSON pasa a 8**, con su migración (un build viejo lo abre en solo lectura, como siempre).
- El libre no se retoma: ahí no se guarda nada.

## El arma que cambia con el daño

Gemini pidió "desbloqueos que se vean, no solo números". Desde la 1.5.0 la bala cambia de color y tamaño en cinco
tramos del daño de cada tiro (blanca, amarilla, naranja, magenta, violeta: `BulletController.Tramo`). **La pistola de la
mano cambia con el mismo tramo**: una pistola, un revólver, una escopeta corta, un rifle y un arma de energía, con
formas simples como la pistola de hoy (`ConstructorArmas`), cada una con su color de detalle y su fogonazo del color de
la bala. Al subir de tramo en la partida (una carta de daño, la furia), un destello y el sonido del cartel.

Primero, **maquetas** de las cinco armas en la mano del jugador, con la cámara del juego, para que Ivan elija.

## El balance

Las cartas hacen al jugador más fuerte dentro de la partida y mueven el muro: Gemini eligió la curva de la 1.5.2
(×1,08 y ×1,04 desde la 11) pensando en eso. Antes de publicar, la simulación de la curva (`simulaciones/curva_1.5.2`)
suma un jugador que elige cartas, para ver dónde queda el muro y si las cartas de daño tienen que ser más chicas.
Las partidas más largas mueven también lo que se mide con la mejor oleada (las misiones, la diaria, el bestiario,
Halloween), pero eso se mide siempre de menos y no se rompe.

## Rendimiento

Las explosiones, los rayos y los drones salen de pools y de `Efectos`, nunca de `Instantiate` por tiro. Con 35 zombis
en el teléfono se mide con el contador de FPS (el peor cuadro y los tirones), en el Redmi.

## Pruebas

- **Prueba de lógica**:
  - la curva y los topes;
  - el sorteo: sin cartas topeadas ni que no se puedan usar, y la misma semilla da las mismas tres;
  - lo que aplica cada carta sobre el arma y el jugador, y que se apile bien con la caja de balas y la furia;
  - el guardado al retomar y la migración a v8;
  - que la barra no pise la pausa ni la barra del jefe.
- **Banco en play**: subir de PODER, elegir, salir con MEJORAS, volver y ver que la carta sigue; morir y ver que se
  fue. Y uno que tire con perforante, rebote, explosiva y eléctrica y cuente que ningún zombi muera dos veces ni sume
  puntos dobles, y que `ZombisVivos` no se despegue.

## Las etapas

| etapa | qué | cuánto |
|---|---|---|
| 1 | Primero el interruptor de los bancos y el tutorial; después la experiencia (en los assets), el PODER, la barra, la elección y las 8 cartas de números, con el guardado (v8) y las pruebas | 3-4 días |
| 2 | Las 9 cartas que cambian las reglas, con su banco, y los íconos (**hecha el 12/10**) | 4-5 días |
| 3 | El arma que cambia con el daño: maquetas, y después el constructor (**hecha el 12/10**: el cañón, la luz, el fogonazo y el sonido de cada una; ver CLAUDE.md) | 1-2 días |
| 4 | El balance con la simulación, el Redmi, la APK de prueba y el AAB | 1-2 días |

Cada etapa termina con la prueba de lógica en TODO OK y una APK de prueba para el teléfono.

## Lo que se decidió

Las seis las decidió Gemini, a pedido de Ivan (10/10), todas como se recomendaron:

1. **La experiencia llega directa al matar**, con una chispa que vuela del zombi al jugador. Sin cristales.
2. **El nivel de la partida se llama PODER.**
3. **La curva**: la primera carta a los 8 zombis y cada nivel ×1,15 (14 cartas hasta la 10).
4. **También en el modo libre.**
5. **La carta dorada después de cada jefe**, que brille mucho y suene épica.
6. **Sin vídeo para cambiar las cartas en la 1.6.0.** Gemini propone sumar el cambio en la 1.7.0, pagado con monedas de
   la partida o, más adelante, con gemas.

Y un cambio suyo: **el escudo, al romperse, empuja medio metro a los zombis pegados**. Bloquear un golpe cada 15 s,
solo, no sirve en la oleada 40 con cinco zombis encima.

### La revisión de la etapa 1 (12/10)

Con la etapa 1 andando, Gemini revisó todo a pedido de Ivan:

- **El guardado al empezar la oleada, aprobado** (ver Lo que se guarda).
- **Las cartas que cambian las reglas son nueve**, pensadas para el Redmi Note 8: **sin Rebote** (buscar el próximo
  blanco y rebotar por cada bala que choca es lo que más cuesta en un teléfono de gama baja), **la Eléctrica pasa a ser
  Quemadura** (un contador en el zombi, sin buscar a nadie) y **Perforante con tope 2**. Ya están en la tabla.
- **Los textos de la tarjeta, cortos y sin repetir el nombre**: "DAÑO" y abajo "+25 %", no "+25 % de daño".
- **La dorada dice "¡BONO DE JEFE!"** arriba del panel, para que se entienda por qué salió.
- **La barra del PODER va fina, a todo el ancho, pegada al borde de abajo** (arriba está lleno: las monedas, las balas,
  el botón de pausa, la barra del jefe y los carteles; y el centro tiene que quedar libre para la acción). Se muestra
  con maquetas antes de cambiarla.
- **La fila de las cartas que se tienen, chica**: con 30 cartas ensucia; o las últimas 5 y un "ver build" en la pausa.
- **El 0,6 s sin elegir al abrir, aprobado.**

| carta | nombre (es / en) | en la tarjeta | color | familia |
|---|---|---|---|---|
| Daño | Daño / Damage | +25 % | naranja | estadísticas |
| Cadencia | Cadencia / Fire Rate | +20 % | amarillo | estadísticas |
| Crítico | Crítico / Critical | +10 % | rojo | estadísticas |
| Vida | Vida / Max Health | +20 % y cura | verde lima | supervivencia |
| Velocidad | Movimiento / Speed | +10 % | verde claro | supervivencia |
| Imán | Imán / Magnet | +2 m | azul | utilidad |
| Granada rápida | Granada / Grenade | recarga −30 % | naranja oscuro | táctica |
| Furia larga | Furia / Rage | +2 s | rojo oscuro | táctica |
| Perforante | Perforante / Piercing | atraviesa 1 zombi | violeta | alteración |
| Disparo doble | Múltiple / Multishot | +1 bala | violeta | alteración |
| Hacia atrás | Retaguardia / Rear Shot | +1 bala atrás | violeta | alteración |
| Explosiva | Explosiva / Explosive | área 30 % | cian | elemental |
| Hielo | Congelación / Frost | −40 % 2 s | cian | elemental |
| Quemadura | Quemadura / Burn | daño 3 s | cian | elemental |
| Dron | Dron / Drone | fuego de apoyo | rosa | aliados |
| Vampiro | Vampirismo / Vampire | cura 5 % cada 15 | rojo sangre | supervivencia |
| Escudo | Escudo / Shield | 1 golpe cada 15 s | azul claro | supervivencia |
| Curación | Curación / Full Heal | vida entera | blanco | comodín |

**Los íconos los generó Gemini** (Nano Banana, con las capturas de la ficha y los íconos del menú de referencia): una
hoja de 6 × 3, blancos sobre negro, en el orden de la tabla. Cada uno va en el círculo de la tarjeta y en la fila del
HUD, teñido con el color de su carta.
