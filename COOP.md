# ¿Se puede hacer ShowBies en co-op online?

Investigación pedida el 21/9/2026. La pregunta era: cuánto trabajo es de verdad, qué habría
que reescribir, cuánto cuesta el hosting, y si el teléfono aguanta 35 zombis sincronizados.

## La respuesta corta

**Se puede, pero es otro proyecto, y el costo no es la plata: es el tiempo y el riesgo.**

- El **hosting es barato**: con las cuentas de abajo, unas **3.800 partidas de 4 jugadores
  por mes entran en el plan gratuito** de Unity Relay, y pasado eso cada partida sale menos
  de medio centavo de dólar.
- Lo caro es **reescribir el núcleo del juego**. Los archivos que habría que rehacer suman
  **3.249 líneas de 17.315**, el 19 % del código, y son justo los que sostienen todo:
  `EnemyController`, `PlayerHealth`, `WaveManager`, `Efectos`, `Moneda`, `OfertaDeRevivir`.
- Hay **una pregunta que esta investigación no puede contestar**: si el teléfono aguanta.
  Eso se mide, no se deduce, y está propuesto abajo como una prueba de uno o dos días.

## De qué tamaño estamos hablando

| | |
|---|---|
| scripts del juego | 111 archivos, 17.315 líneas |
| lo que habría que rehacer de fondo | 3.249 líneas (19 %) |
| paquetes de red instalados hoy | ninguno (sólo `com.unity.multiplayer.center`, que es la ventana de onboarding y no hace nada) |
| zombis vivos a la vez | 60 en PC, **35 en móvil** |
| monedas en escena | 150 en PC, 80 en móvil |
| balas por segundo | hasta 120 con la cadencia al tope |

## Lo que habría que reescribir

### 1. El juego está construido para un jugador, no por descuido sino por diseño

No es cosa de cambiar un par de variables. Los tres cachés que sostienen el rendimiento
asumen que hay **un** jugador:

- `EnemyController.jugadorCache` — el jugador se busca **una vez** y lo comparten los 35
  zombis. Existe justamente porque buscarlo por zombi era costo cuadrático. En co-op cada
  zombi tiene que elegir a quién persigue, y esa elección la decide el servidor y hay que
  sincronizarla.
- `Moneda.jugador` — lo mismo para las monedas, que vuelan hacia "el jugador". En co-op hay
  que decidir de quién es cada moneda: ¿el que la toca, o se reparten? Eso es una decisión
  de diseño, no técnica.
- `PlayerHealth.instance` (9 usos) y `Puntaje.instance` (11 usos) — hay un solo marcador y
  una sola barra de vida en todo el código.

### 2. El problema más hondo: trece lugares congelan el mundo entero

Esto es lo que más me preocupa y lo que menos se ve de afuera. **Trece lugares llaman a
`Time.timeScale`** para parar el universo:

| dónde | por qué |
|---|---|
| `OfertaDeRevivir` (4 lugares) | el "¡HAS MUERTO!" congela todo mientras decidís si mirás el vídeo |
| `Efectos` (3) | la pausa de impacto de cada muerte grande |
| `MenuPausa` (2) | la pausa |
| `TiendaMejoras` (2), `BotonModoLibre` (1) | restauran al cambiar de escena |

**En co-op no podés parar el universo porque uno murió y está mirando un anuncio.** Y
tampoco podés congelar a los otros tres porque vos mataste un tanque. Esto no se arregla
con un `if`: hay que rediseñar el revivir (¿esperás al muerto? ¿lo revive un compañero?
¿reaparece solo?), sacar la pausa de impacto o hacerla local a cada pantalla, y decidir qué
significa "pausa" cuando hay cuatro personas.

La pausa de impacto es, además, la mitad del jugo del juego. Sacarla cambia cómo se siente.

### 3. La economía y el progreso son de una sola persona

- `Progreso` es un JSON local con las monedas y los niveles de mejora. En co-op: ¿las
  monedas de la partida son de quien las agarra? ¿Las mejoras compradas de cada uno se
  aplican en la partida compartida? Un jugador con daño 20 y otro con daño 1 en la misma
  oleada no funciona.
- `WaveManager` cierra la oleada mirando `PlayerHealth.instance.EstaMuerto`. Con cuatro
  jugadores, ¿la oleada termina cuando mueren todos? ¿Se completa si quedó uno vivo?
- El bono de la oleada y el récord son de un jugador.

### 4. El jefe

`JefePatrones` marca la carga hacia el jugador y lo aturde al chocar. Con cuatro objetivos,
hay que elegir a quién carga y sincronizar el aviso, porque el aviso rojo en el piso es lo
que hace que la carga sea justa. Un aviso que llega tarde por lag es una muerte injusta.

## Lo que tiene que viajar por la red

La parte que no se puede evitar: **los zombis no se pueden simular en cada teléfono por
separado.** Tienen Rigidbody y chocan entre ellos, así que dos simulaciones divergen en
segundos. O se sincroniza la posición, o hay que rehacer el movimiento sin física.

Cuentas con los supuestos a la vista:

- 4 jugadores, uno hace de anfitrión, todo por Relay.
- 35 zombis vivos (el techo de móvil).
- `NetworkTransform` al tick por defecto de 30 Hz, con compresión de media precisión:
  **~20 bytes por zombi por actualización**, contando cabeceras.

```
35 zombis x 20 bytes x 30 Hz          =  21 KB/s hacia cada cliente
x 3 clientes + la entrada de cada uno =  ~70 KB/s por partida
una partida de 10 minutos             =  ~42 MB
```

Las balas (hasta 120/s), las monedas (150) y los números de daño **no** viajan: se resuelven
en el servidor y cada cliente los dibuja por su cuenta. Si viajaran, esto no cerraría.

## Cuánto cuesta el hosting

Precios de Unity Gaming Services al 21/9/2026:

| servicio | gratis | después |
|---|---|---|
| **Relay** | 50 CCU promedio mensual, y 3 GiB por CCU hasta 150 GiB/mes | $0,16 por CCU extra; $0,09/GiB en US+EU, $0,16 en Asia+Australia |
| **Lobby** (juntar jugadores) | 10 GiB/mes por región | $0,09/GiB US+EU |
| **Cloud Save** | 5 GiB, 1.000.000 lecturas y escrituras/mes | $0,50/GiB, $0,025 por 10.000 escrituras |
| **Leaderboards** | gratis por ahora | — |

Con los 42 MB por partida de arriba:

```
150 GiB gratis / 42 MB  =  ~3.800 partidas de 4 jugadores por mes
                        =  ~128 partidas por día, gratis
pasado eso: 1 GiB = 24 partidas = $0,09  ->  menos de medio centavo por partida
```

**El hosting no es el problema.** Para el tamaño que va a tener este juego al principio,
entra gratis, y aunque explote es calderilla comparado con el trabajo.

Ojo con un dato: Unity **discontinuó Multiplay Game Server Hosting el 31/3/2026**. O sea que
no hay servidor dedicado barato: el anfitrión es el teléfono de un jugador. Eso significa
que **si el anfitrión se va, la partida se cae**, salvo que se implemente migración, que es
más trabajo.

## La pregunta que no puedo contestar sin medir

**¿El teléfono aguanta?** Hoy el juego corre a 30-60 FPS en el teléfono con 35 zombis, y eso
ya costó trabajo (escala de resolución 0,75, calidad Medium, Animators en Cull, pools por
todos lados). Encima de eso habría que sumar deserializar 35 transforms 30 veces por segundo
— que en las pruebas de la comunidad aparece como lo más caro de Netcode for GameObjects, y
además aloca memoria.

Puede que entre. Puede que haya que bajar a 20 zombis, y ahí el juego es otro.

**Esto se mide en uno o dos días** y antes de comprometerse con nada: una escena vacía, 35
cápsulas con `NetworkTransform` moviéndose, dos teléfonos conectados por Relay, y mirar el
contador de FPS que el juego ya tiene. Si a 35 zombis el teléfono va a 45 FPS, la cosa es
viable. Si va a 20, la respuesta es no y te ahorraste meses.

## Cuánto trabajo es

Estimar esto es adivinar, así que va por partes y con el criterio a la vista:

| fase | qué | tamaño |
|---|---|---|
| 0 | La prueba de rendimiento de arriba | 1-2 días |
| 1 | Sacar los supuestos de un jugador (los 3 cachés, las 2 instancias) | ~1 semana |
| 2 | Rediseñar revivir, pausa y pausa de impacto sin congelar el mundo | 1-2 semanas, y es **decisión de diseño** antes que código |
| 3 | Netcode: zombis con autoridad del servidor, disparos, daño | 3-4 semanas |
| 4 | Lobby, unirse, que se caiga el anfitrión, reconectar | 1-2 semanas |
| 5 | Economía compartida: monedas, mejoras, oleada, récord | 1-2 semanas |
| 6 | Probar con gente de verdad, lag, tramposos | sin fondo |

**Entre dos y tres meses de trabajo concentrado**, con la fase 6 abierta. Y el juego todavía
no está publicado.

## Alternativas mucho más baratas que dan sensación de online

1. **Tabla de récords global + guardado en la nube.** Días, no meses. Le pega al juego
   porque es un cazapuntos. Cloud Save además resuelve el "cambié de teléfono y perdí todo",
   que es un problema real hoy. **El obstáculo no es técnico sino los tramposos**: el
   progreso es un JSON local que cualquiera edita, así que una tabla con puntajes que manda
   el cliente se llena de basura. O se valida del lado del servidor, o se asume que es para
   divertirse.
2. **El desafío semanal, compartido.** Ya existe, ya funciona, y está calculado con la misma
   vara para todos. Hacerlo global es casi natural y da la sensación de que hay más gente.
3. **Fantasmas asíncronos**: ver por dónde iba otro jugador en la misma oleada. Nada de
   tiempo real, sólo datos guardados.

## Recomendación

**Hacer la fase 0 —la prueba de rendimiento— y nada más, por ahora.** Uno o dos días
contestan la única pregunta que decide todo, y el resultado no caduca.

Meter co-op en tiempo real antes de publicar la v1 es la forma clásica de no publicarla
nunca. El juego es bueno de a uno; conviene sacarlo, ver si le gusta a alguien, y decidir el
co-op con gente jugando en vez de con una corazonada.

---

Fuentes de precios y límites: [UGS Pricing](https://unity.com/products/gaming-services/pricing),
[Unity Support — costo de multiplayer](https://support.unity.com/hc/en-us/articles/30303074122772-How-much-is-the-cost-of-Multiplayer-services),
[NetworkTransform (NGO 2.7)](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.7/manual/components/helper/networktransform.html),
[NGO: rendimiento y alocaciones](https://discussions.unity.com/t/netcode-for-gameobjects-performance-and-allocations/1559396).
