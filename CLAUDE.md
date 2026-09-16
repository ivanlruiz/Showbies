# CLAUDE.md

Guía de arquitectura de **ShowBies** para agentes que trabajen en este repo.

## El juego

ShowBies es un twin-stick shooter 3D de zombis, top-down, **pensado para Android** (celular horizontal, con dos
joysticks y un botón de granada) que también se juega en Windows. En PC el jugador se mueve con WASD, apunta con
el mouse (raycast contra un plano en Y=0), dispara manteniendo click y apunta la granada manteniendo Espacio;
Escape pausa. Los zombis aparecen solos, van derecho hacia el jugador y le pegan mientras lo tocan. Matar suma
puntos y suelta monedas; morir guarda el récord y lleva a la pantalla de derrota.

Hay **dos modos**, los dos jugables desde el menú, y un tutorial:

- **Free mode** (`ShowBies1.unity`) — generación continua: cinco corrutinas paralelas, una por tipo de
  zombi, cada una con su intervalo. Sin final, pero se pone más difícil con el tiempo ("Nivel N" en el HUD).
- **Wave mode** (`WaveMode.unity`) — oleadas que terminan al matar a todos sus zombis, cada una más grande
  y con más tipos mezclados, y un jefe cada 10. Mismo mapa pero con las calles (`Ciudad`) encendidas.

Es un **incremental**: las monedas que se juntan en las partidas se gastan en la **tienda de mejoras** del menú
(daño de bala, cadencia, vida máxima, imán, botín y la furia), y las mejoras se aplican al empezar cada partida. Del otro lado,
los zombis se ponen más duros con cada oleada, y en el modo libre, con los minutos.

## Entorno

- **Unity 6000.3.14f1**, render built-in, 3D. `ProjectSettings/ProjectVersion.txt` es la fuente de verdad.
- El proyecto **nació en Unity 2020.3.26f1** y se subió a Unity 6. Buena parte de las rarezas del repo
  son cola de esa migración; si algo parece escrito para una API vieja, probablemente lo esté.
- Target principal: **Android** (ver Móvil). También compila para **Windows standalone**: 1920x1080, borderless
  (`FullScreenWindow`), ventana redimensionable.
- **Input Manager viejo** (`activeInputHandler: 0`): todo es `Input.GetAxis` / `Input.GetKey` /
  `Input.GetMouseButton`. No hay Input System.
- El proyecto Unity está en la subcarpeta **`ShowBies1/`**, no en la raíz del repo.
- No hay asmdefs: todo el código del juego vive en `Assembly-CSharp`. Los `.csproj` / `.sln` son
  generados y están gitignoreados.
- Assets de terceros en `Assets/`: **ToonyTinyPeople** (modelos y animaciones), **Joystick Pack**
  (`FixedJoystick`, para móvil), **Blood decal pack**, **Five Seamless Tileable Ground Textures**,
  **TextMesh Pro**.

## Layout del código

```
Assets/Scripts/Armas/       ← GunController, BulletController, Granade, Balas (UI), AudioArma
Assets/Scripts/Jugador/     ← PlayerController, PlayerHealth, PlayerJS (móvil), Transitions, Furia
Assets/Scripts/Zombi/       ← EnemyController, Enemy (ScriptableObject), GeneradorZombis, WaveManager, BarraDeVida, Escalado, ManchaDeSangre
Assets/Scripts/Camara/      ← CamaraJugador
Assets/Scripts/UI/          ← ConditionalShow, Score, highscoretext, ContadorFps, IndicadorMejoraCadencia, IndicadorRecargaGranada, JoystickGranada, MenuPausa, BotonAtrasMenu, ContadorMonedas, TextoMonedasPartida, FormatoNumeros, ContadorCombo, VinetaDanio, AparecerConRebote, BotonJugoso, CurvasUI, TexturasUI, MedidorBalance, BotonFuria, InterruptorVideos
Assets/Scripts/PowerUps/    ← PowerUp (el spawner), PickupCaducidad, Moneda (las que sueltan los zombis)
Assets/Scripts/Progreso/    ← Progreso (monedas, mejor oleada y niveles, en un JSON), Mejora, CatalogoMejoras, AplicarMejoras
Assets/Scripts/Tienda/      ← TiendaMejoras, TarjetaMejora, BotonMejoras, EfectosUI
Assets/Scripts/Anuncios/    ← ServicioAnuncios, ConfigAnuncios, IProveedorAnuncios, ProveedorFalso, ProveedorNulo, LugarAnuncio, OfertaDeDuplicar, VigiaAplicacion
Assets/Scripts/Jugo/        ← Efectos (golpes, muertes, explosiones, música), Sonidos, NumeroFlotante
Assets/Scripts/Tutorial/    ← TutorialManager
Assets/Scripts/*.cs         ← CanvasHelper, ConfiguracionRendimiento, MainMenu, MenuPerdiste, Plataforma, Puntaje, RestartScene
Assets/Escenas/             ← Menu, ShowBies1, Perdiste, WaveMode, Tutorial (+ Scenes/SampleScene, sin usar)
Assets/Prefabs/             ← Bullet, Gun, Granada, Moneda, power-ups, Jugo/ (Efectos, NumeroFlotante), Particulas/ (BrilloMoneda, Chispas), Personajes/, UI/ (MenuPausa, Tienda, TarjetaMejora, BotonFuria)
Assets/Zombies/*.asset      ← los cinco Enemy: stats POR TIPO, editables sin recompilar
Assets/Mejoras/             ← las seis Mejora (.asset) y Resources/CatalogoMejoras
Assets/Anuncios/            ← Resources/ConfigAnuncios: los numeros de los videos con recompensa
Assets/otros/               ← los audios: MainMenu.mp3, shot.mp3, pop.mp3 (cajas), pedo.mp3 y los sintetizados provisorios (moneda, golpe, muerte, explosion, danio, cartel y musica, en .wav)
Assets/Editor/              ← ConstructorAndroid (builds de Android), PruebasMejoras, HerramientasProgreso y ControlesEnElEditor (menú ShowBies)
```

**Código nuevo va en `Assets/Scripts/<Subsistema>/`**, nunca suelto en la raíz de `Assets/`.

## Escenas y build settings

| índice | escena | qué es |
|---|---|---|
| 0 | `Menu.unity` | menú principal + panel de modos + tienda de mejoras (prefab `Tienda`) |
| 1 | `ShowBies1.unity` | free mode |
| 2 | `Perdiste.unity` | pantalla de derrota |
| 3 | `WaveMode.unity` | wave mode |
| 4 | `Tutorial.unity` | tutorial jugable (opcional, desde el menú) |

**Los índices están hardcodeados en el código** (`MainMenu.PlayGame` → 1, `MainMenu.GameModes` → 3,
`MainMenu.Tutorial` → 4, `PlayerHealth` → 2, `MenuPerdiste.Menu` → 0, `TutorialManager.IrAJugar` → 1,
`TiendaMejoras.Jugar` → 1 o 3 y `TiendaMejoras.AbrirEnMenu` → 0, estos dos con las constantes `EscenaMenu`,
`EscenaModoLibre` y `EscenaOleadas`).
Reordenar Build Settings rompe la navegación en silencio.

### Tutorial

`Tutorial.unity` es una copia de ShowBies1 con `Spawners` apagados: los zombis y las cajas los pone
`TutorialManager` (`Assets/Scripts/Tutorial/`) cuando el paso los pide. Seis pasos — moverse,
disparar, granada, cajas de balas/vida, caja de arma (con su reloj de cadencia), fin — y **cada uno
se completa haciendo la acción**, no apretando "siguiente". Los textos salen de `Plataforma.EsMovil`
(teclado o joystick), el jugador es inmortal mientras dura, los pickups del tutorial no caducan, y al
terminar guarda `PlayerPrefs["TutorialCompletado"] = 1` por si algún día se quiere sugerir en la
primera partida. Para agregar un paso: un valor en el enum `Paso`, su texto en `Entrar` y su
condición de salida en `Update`.

`SampleScene.unity` no está en el build y no se usa. Es el único lugar donde queda `AudioArma`.

## Arquitectura: singletons y referencias de inspector

No hay sistema de eventos. Los managers son `public static X instance` asignados en `Awake()`:
`PlayerHealth.instance`, `Puntaje.instance`. El resto se conecta por campos públicos arrastrados en el
inspector (`PlayerController.theGun`, `EnemyController.enemyType`, etc.).

Lo que se comunica sin inspector usa búsquedas cacheadas:

- `EnemyController.jugadorCache` — el jugador se busca **una vez** y se comparte. Antes había un
  `FindObjectOfType` por zombi spawneado, que con zombis en escena era costo cuadrático.
- `EnemyController.ZombisVivos` — contador `static`, `OnEnable`/`OnDisable` (los zombis prendidos). Lo miran los dos
  generadores y el tutorial.
- `BulletController.pool` — la pila de balas dormidas.
- `EnemyController.pool` — los zombis muertos, apagados, en una pila por prefab (ver Generación de enemigos).
- `ManchaDeSangre.pool` — las manchas de sangre apagadas, también por prefab.
- `MenuPausa.Pausado` — si el juego está en pausa. Lo miran los que leen input.
- `CatalogoMejoras.Instancia` — sale de `Resources` (no se cablea en ninguna escena); si falta, las mejoras
  quedan neutras con un LogError.
- `Progreso.Revision` — un contador que sube con cada cambio del progreso. La tienda y los botones MEJORAS lo
  consultan en `Update` y se refrescan cuando cambia, en vez de suscribirse a un evento.
- `TiendaMejoras.AbrirAlCargarMenu` — la derrota lo prende para que el menú cargue con la tienda abierta.

**Todo lo `static` se resetea en un `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]`**, porque
sobrevive al cambio de escena y al "enter play mode" sin domain reload. Si agregás estado `static`,
sumalo a ese reset o vas a arrastrar basura entre partidas.

## Modelo de datos

```csharp
[CreateAssetMenu]                     // Assets/Scripts/Zombi/Enemy.cs
class Enemy : ScriptableObject {
    public int hp;                    // vida
    public int daño;                  // lo que le saca al jugador en cada golpe
    public int velocidad;
    public int puntos;                // cuánto suma MATARLO
    public int monedasMin;            // cuántas monedas suelta al morir: al azar entre min y max
    public int monedasMax;
}
```

Los cinco assets viven en `Assets/Zombies/`. Balance actual:

| zombi | hp | daño | velocidad | puntos | monedas | balas para matarlo |
|---|---|---|---|---|---|---|
| ZombiNormal | 5 | 1 | 5 | 1 | 1–3 | 5 |
| ZombiRapido | 3 | 2 | 9 | 2 | 1–3 | 3 |
| ZombiFASTER | 3 | 1 | 12 | 5 | 2–4 | 3 |
| ZombiTanque | 25 | 1 | 3 | 20 | 5–8 | 25 |
| ZombiBOSS | 500 | 10 | 2 | 100 | 30–40 | 500 |

La vida y el daño **de cada zombi** se calculan como float en `EnemyController`: el valor del `.asset` por
`multiplicadorVida` y `multiplicadorDano`, que pone quien lo hace aparecer antes de su primer golpe (ver Generación de
enemigos). Los `.asset` no cambian con la oleada, y la columna "balas para matarlo" vale para la oleada 1 sin
mejoras.

**Tocar el balance no requiere recompilar**: son valores de los `.asset`. Ojo que en puntos por bala el
BOSS es hoy el peor negocio del juego (100 puntos por 500 balas sin mejoras); está así a propósito hasta que se
decida el balance.

## Puntaje

`Puntaje.instance.contadorKill` es **puntos, no kills**, a pesar del nombre. El único lugar que lo
incrementa es `EnemyController.DanoZombi`, cuando el zombi efectivamente muere:

```csharp
Puntaje.instance.contadorKill += enemyType.puntos;
Puntaje.instance.UpdateKillCounterUI();
```

**No agregues puntos desde ningún otro lado.** Antes `BulletController` sumaba en `OnCollisionEnter`,
o sea por impacto: el BOSS daba 10000 puntos (100 balas × 100) y matarlo con granada daba 1. Hay
highscores guardados de esa época que son inalcanzables con el sistema actual.

## Pipeline de disparo

1. `PlayerController.HandleShooting` prende `theGun.isFiring` con el click, si `cantBalas > 0`.
2. `GunController.Update` calcula cuántos tiros tocan en el frame con `TirosDelFrame` y por cada uno pide una
   bala al pool: `BulletController.Obtener(bala, firePoint.position, firePoint.rotation)`.
3. `BulletController.Update` se mueve con `transform.Translate` y descuenta `lifeTime`.
4. Al vencer el tiempo o al chocar, la bala **se apaga y vuelve al pool**, no se destruye.

**Nunca hagas `Instantiate`/`Destroy` de balas directo.** El pool convierte 500 disparos en 49 objetos.

**Varias balas por frame.** `TirosDelFrame` es un acumulador (y es la misma función que usan las pruebas): tira
mientras el contador esté vencido, hasta `maxTirosPorFrame` (8), con un techo de `maxTirosPorSegundo` (120), y
cada bala se adelanta según su atraso para que el chorro salga escalonado. Antes el arma tiraba una bala por frame
como mucho: 20 por segundo a 60 FPS y 15 a 30 FPS, así que la cadencia dependía del teléfono.

**Soltar el disparo no recarga el arma.** Mientras no se dispara, `EnfriarSinDisparar` baja el contador hasta 0 y
ahí lo deja: después de una pausa más larga que el intervalo el primer tiro sale en el acto, pero tocar el disparo más
rápido que la cadencia no tira más balas. Antes el contador volvía a 0 al soltar, y con 4 tiros/s mover el joystick
de disparo a golpecitos disparaba mucho más que la mejora.

**La cadencia y el daño los fija la mejora, en tiros por segundo.** `AplicarMejoras` llama a
`FijarTirosPorSegundo` (4 de base, +1 por nivel) y `FijarDanoPorBala` (1 de base, +1 por nivel). Cada bala lleva su
`danoAplicado`; el `dañoDar` del prefab y el override `tiempoDisparo` de las escenas quedan como respaldo para una
escena sin `AplicarMejoras`. **Nunca escribas sobre `gun.bala`**: es el prefab, no una bala.

**Las cajas multiplican la cadencia por un rato.** `PotenciarCadencia(multiplicador)`: `PUBalas` ×1,5 y `PUArma` ×3
durante `duracionMejora` (10 s); la última pisa a la anterior y al vencer vuelve a la cadencia mejorada. Antes
fijaban un intervalo, y con la cadencia al tope una caja de balas empeoraba el arma. La munición no expira. La
furia multiplica aparte (`FijarFuria`), así una caja que llega durante la furia no la pisa.

**El sonido del disparo tiene techo.** `GunController` usa `PlayOneShot` y deja al menos
`intervaloMinimoSonido` (0.04 s) entre sonidos. Con `Play()` el mismo sonido se reiniciaba en cada tiro y,
con la cadencia mejorada, no llegaba a oírse.

La bala **no tiene Rigidbody**, sólo un `BoxCollider`: los eventos de colisión llegan porque el zombi
sí tiene Rigidbody. Por eso el pool no necesita resetear velocidades.

## Granada

`PlayerController.ThrowGranade` (Espacio en PC, botón G en móvil, con `granadaCooldown` de 5 s) instancia
`Granada.prefab` y llama a `Granade.Lanzar(destino)`.

- **Apuntar.** En PC, mantener Espacio marca en el piso dónde va a caer (bajo el mouse, entre
  `distanciaMinimaGranada` y `distanciaMaximaGranada`, 3 a 12 m) y soltarlo la tira. En móvil el botón G es un
  joystick (`JoystickGranada`): arrastrar desde donde se apoyó el dedo elige dirección y distancia, soltar la tira y
  volver al centro antes de soltar cancela. Un toque sin arrastrar la tira rápido a `distanciaGranadaMovil` (8 m)
  hacia donde se venía apuntando con el joystick de disparo en el último segundo (`PlayerJS.ApuntoHaceMenosDe`), o si
  no hacia donde mira el jugador. El anillo de "dónde cae" es una copia del indicador de la granada que
  `PlayerController` crea al arrancar. La cuenta del destino es `PlayerController.PuntoEnElPiso`, estática para
  poder probarla sin input.
- **Vuelo por trayectoria calculada, no por física.** Al lanzarla, el Rigidbody pasa a kinematic y el collider se
  apaga; `Update` la mueve en un arco de `alturaDelArco` durante `tiempoDeVuelo`. Así cae exacta sobre el anillo y
  no choca con el jugador del que sale ni con las balas que van en la misma dirección.
- **Explota** al pasar a `radioDeContacto` de un zombi en el aire, o `demoraAlCaer` (0,3 s) después de caer. Una
  granada instanciada sin `Lanzar` se comporta como antes: cae donde nace y explota con la mecha de 3 s.
- **Daño:** `damage` (10) × `multiplicadorVida` del zombi al que le pega, así escala con la oleada y no con la
  mejora de daño. Con 10 fijos, desde la oleada 6 ya no mataba ni a un normal.
- Mientras vuela, el hijo "Indicador" del prefab (un `LineRenderer`) se suelta y dibuja en el piso el radio de la
  explosión; se destruye con ella.
- El botón G muestra la recarga con `IndicadorRecargaGranada` y su hijo "Recarga" (Image Filled Radial360).
  **No tiene `Button`, y no hay que ponérselo:** su `onClick` se sumaría al joystick y cada toque tiraría dos veces.

## Furia

Un pico de poder que se compra **una sola vez** en la tienda (la mejora `furia`, 5.000 monedas, tope 1) y se activa
en la partida. `Furia` (`Assets/Scripts/Jugador/`, en la raíz de `Jugador.prefab`) lee la compra en su `Awake`, como
`AplicarMejoras`, y lleva los relojes con tiempo escalado: la pausa los congela.

- **Se activa** con el botón del HUD (`BotonFuria`, prefab `Prefabs/UI/BotonFuria` en ShowBies1 y WaveMode, arriba
  del botón de granada, visible en PC y en móvil) o con la **F** en PC. Sin comprarla el botón no aparece. En PC,
  hacer click en el botón también dispara (el arma lee el mouse sin mirar la UI): para eso está la F.
- **Dura** lo que dice el asset (`valorBase`, 6 s) y **se recarga en `enfriamiento`** (120 s), contados desde que se
  activa. Mientras dura: cadencia ×2 y daño ×2 por un multiplicador aparte del arma (`GunController.FijarFuria`, que
  se multiplica con la caja y respeta el techo de `maxTirosPorSegundo`) y velocidad ×1,3
  (`PlayerController.multiplicadorVelocidad`). Esos números están en el componente del prefab.
- **Jugo** (`Efectos.EmpezarFuria`): cartel "¡FURIA!" con rebote, chispas, temblor, una pausa de impacto corta, la
  música más aguda y el borde rojo latiendo mientras dura. El botón respira cuando está lista, vibra mientras dura y
  cuenta los segundos del enfriamiento.
- `GunController.DanoPorBala` sigue siendo el de la mejora (lo miran el medidor y las pruebas); lo que lleva cada bala
  es `DanoPorTiro`.
- **Es un desbloqueo permanente:** cuando entre el renacer de la fase 5, no tiene que reiniciarla.

## Generación de enemigos

Los dos generadores respetan el mismo techo, `maxZombisVivos` (60 por defecto, editable en el inspector),
consultando `EnemyController.ZombisVivos`:

- **`GeneradorZombis`** (free mode) — cinco corrutinas paralelas, una por tipo, cada una con un `while`
  infinito y su `WaitForSeconds`. Si se llegó al techo, saltea el spawn y sigue esperando. **Escala con el
  tiempo:** `NivelActual` sube uno cada `segundosPorNivel` (45 s de tiempo escalado, así la pausa lo congela) y
  cada zombi aparece con vida × `crecimientoVida`^(nivel−1), daño × `crecimientoDano`^(nivel−1) y monedas ×
  `crecimientoMonedas`^(nivel−1), los mismos crecimientos que las oleadas. `textoNivel` muestra "Nivel N" en el
  HUD y, al subir, rebota y suena el jingle del cartel. Sin esto el modo libre era una granja de monedas.
- **`WaveManager`** (wave mode) — **una sola** corrutina que corre toda la partida. Cada oleada:
  1. Muestra el cartel "Oleada N" (`cartelOleada`) durante `descansoEntreOleadas` (3 s). El HUD
     (`textoOleada`) muestra "Oleada N" y abajo "Zombis muertos/total" de esa oleada, jefe incluido; se
     actualiza en `Update` sólo cuando cambia, y los caídos por el kill-Z cuentan como muertos para que
     llegue al total justo cuando la oleada termina.
  2. Si la oleada es múltiplo de `jefeCadaOleadas` (10), saca un `jefe`.
  3. Saca `zombisBase + zombisPorOleada × oleada` zombis (6 + 2n), de a uno cada
     `intervaloEntreApariciones` (0,8 s), en un punto al azar de `spawnPoints`. El tipo sale por sorteo
     entre los `tipos` ya habilitados (`desdeOleada`), con `peso` relativo: normal desde la 1, rápido desde
     la 3, tanque desde la 6 y FASTER desde la 9. Si se llegó al techo, espera.
  4. **Termina cuando mueren todos los zombis que sacó**; los que caen por el kill-Z cuentan como muertos.

  La mezcla y el ritmo se configuran en el inspector del `WaveManager` de `WaveMode.unity`. Expone
  `OleadaActual` y los multiplicadores de la oleada actual.

**Los zombis salen de un pool.** Los generadores llaman a `EnemyController.Aparecer(prefab, posición)`, que prende uno
apagado de ese prefab o crea uno nuevo; al morir o caer por el kill-Z el zombi se apaga y vuelve a su pila
(`Devolver`). Lo que era de `Awake`/`Start`/`OnDestroy` se repartió: `Awake` arma lo fijo (rigidbody, escala base,
renderers del destello, animators) y cada aparición arranca en `OnEnable` (vida sin calcular, multiplicadores en 1,
escala base, relojes en cero, sube sobre el piso, cuenta en `ZombisVivos`) y termina en `OnDisable` (descuenta, apaga
la barra y deshace un destello a medias). **Un zombi muerto no queda en null**: para saber después si murió,
anotá su `NumeroDeAparicion` y preguntá `EnemyController.SigueVivo(zombi, número)`, como `WaveManager` y
`MedirPartida`. Por lo mismo, **apagar y prender un zombi vivo es matarlo y hacerlo aparecer de nuevo**: vuelve con
la vida sin calcular, los multiplicadores en 1 y otro número de aparición, y la oleada lo cuenta como muerto. Para
esconder uno, apagale los renderers. Uno que murió en el paso de física actual ya está apagado: las balas lo buscan
con `GetComponentInParent<EnemyController>(true)` para gastarse igual, y `DanoZombi` y `Golpear` lo ignoran. El
tutorial sigue haciendo `Instantiate`: esos zombis no tienen prefab de origen y al morir se destruyen como antes.

**Los zombis escalan con la oleada.** `WaveManager.Aparecer` pone `multiplicadorVida` = `crecimientoVida`^(o−1)
(1,15) y `multiplicadorDano` = `crecimientoDano`^(o−1) (1,07) apenas sale el zombi, antes de su primer golpe, jefe
incluido. La vida
crece más rápido que el daño a propósito: lo que frena es no llegar a matarlos, no que dos golpes liquiden al
jugador. `EnemyController` inicializa la vida perezosa (en el primer golpe o al consultarla, una vez por aparición) y muere con vida ≤ 0,01;
el daño al jugador acumula las fracciones (`PlayerHealth.AcumularDano`) y resta enteros.

Sin el techo son ~350 zombis en el primer minuto y sigue creciendo lineal.

Antes las oleadas eran por tiempo (salía la siguiente aunque quedaran zombis) y cada 5 oleadas el tipo de
zombi se reemplazaba en vez de sumarse: desde la oleada 20 sólo salían jefes.

**Barras de vida.** `EnemyController` crea una `BarraDeVida` con el primer golpe que no mata, así que los
zombis que mueren de un tiro nunca la muestran. Es un objeto aparte que sigue al zombi y mira a la cámara, no
un hijo: los zombis rotan hacia el jugador y tienen escalas distintas, y una barra hija heredaría las dos
cosas. Son dos `SpriteRenderer` sobre un sprite blanco hecho en código (estático, con su reset), no un Canvas
por zombi. La fracción sale de la vida con la que apareció el zombi (`vidaMaxima`), así que un escalado de vida
por oleada no la rompe mientras se aplique antes del primer golpe. Cuando el zombi muere la barra se apaga y queda
guardada con él: la aparición siguiente la prende con su primer golpe que no mata, sin crear otra (el pool es por
prefab, así que la altura sirve). Se destruye con el zombi.

## Monedas y progreso

Lo que el jugador conserva entre partidas vive en `Progreso` (`Assets/Scripts/Progreso/`): un JSON en
`Application.persistentDataPath/progreso.json` con las monedas, la mejor oleada completada, el nivel de cada
mejora y lo que necesitan los anuncios (versión 3: `mejoras` es una lista `{id, nivel}` porque `JsonUtility` no
guarda diccionarios, y desde la 3 se suman `partidasTerminadas`, `segundosJugados`, `ofrecerVideos` y los topes
del día). No usa PlayerPrefs a propósito: es estado estructurado.

- **Los zombis sueltan monedas y se cobran al agarrarlas.** Al morir, `DanoZombi` (en el mismo bloque que
  suma los puntos) suelta entre `monedasMin` y `monedasMax` monedas (`Moneda`, en `Assets/Prefabs/Moneda.prefab`)
  que valen `multiplicadorMonedas` cada una. Salen volando para los costados, caen despacio con un rebote y
  quedan girando en el piso; al acercarse el jugador vuelan solas hacia él (sin la mejora de imán hay que pasar a
  `distanciaDeCobro`, 0,8 m, que es pasarles por encima; con ella, al alcance del imán) y recién ahí se
  suman a `Progreso`, con un brillo y una nota de la escala de la bemol mayor sorteada con los pesos de
  `notas` (editables en el prefab; las del acorde, la bemol, do y mi bemol, salen más seguido). Las que nadie
  agarra desaparecen a los 20 s, parpadeando los últimos 3.
- **El único cobro directo es el bono de la oleada** (`WaveManager`, `bonoPorOleada × oleada`, que se anuncia
  en el cartel de la oleada siguiente). `bonoPorOleada` vale 4 en WaveMode, el doble del plan, porque las monedas
  que sueltan los zombis ya son ≈2× las que simuló; el botín no lo multiplica. **Al terminar la oleada las monedas
  se quedan donde cayeron**: no hay imán global (antes `Moneda.AtraerTodas` las traía todas), juntarlas es parte del
  juego y la mejora de imán es la que ayuda. Siguen desapareciendo a los 20 s.
- **`multiplicadorMonedas` y `monedaPrefab` los pone quien hace aparecer al zombi.** `WaveManager` usa
  `crecimientoMonedas^(oleada − 1)` (1,05) y `GeneradorZombis` 0,5 × el crecimiento de su nivel (el modo libre da
  la mitad y no tiene bono), los dos multiplicados por el botín de la mejora: con un multiplicador menor a 1 cada moneda sale con esa probabilidad y vale 1, porque una moneda de
  0,5 no mueve el contador al agarrarla. Un zombi sin `monedaPrefab`, como los del tutorial, no suelta nada.
- **Las monedas no tienen Rigidbody ni collider** y salen de un pool, con un techo de 150 en escena (80 en
  móvil): el vuelo es una parábola a mano y el cobro, una distancia al jugador. Si el techo no deja soltar
  todas, las que salen se reparten el valor de las que no. **Como no tienen collider, el vuelo no ve las paredes:**
  al salir, `FrenarAntesDeLasParedes` tira un raycast horizontal hasta lo máximo que puede recorrer (velocidad /
  frenado) y, si hay un collider fijo en el camino (sin Rigidbody y que no sea una bala), la frena para que caiga a
  `margenContraParedes` (0,3 m) de él. Sin eso caían detrás de las paredes invisibles del borde, y sin imán quedaban
  perdidas.
- **El modelo es el hijo `Modelo` del prefab** (hoy un cilindro dorado provisorio de 0,4 m, con el centro a 0,3 m del piso). Lo que gira es la raíz, de
  frente a la cámara: para cambiar el modelo se reemplaza el hijo, con la cara de la moneda mirando a +Z. El
  sonido es `Assets/otros/moneda.wav`, también provisorio: la bemol 5 y la bemol 6, la misma nota a una octava
  para que al llevarlo con el pitch a cualquier grado de la escala las dos queden en la bemol mayor. Si se
  cambia por otro sonido, `afinacion` (en semitonos) lleva su nota a la bemol; con dos notas distintas, en
  algún grado una de las dos se sale de la escala.
- **Se suman en memoria al agarrarlas y se guardan en disco en puntos seguros:** al completar cada oleada, al
  pausar (también pasa cuando la app pierde el foco, antes de que Android pueda matarla), al morir y al
  cerrar, y **cada compra guarda en el acto**. Se escribe un `.tmp` y después se copia; al cargar, si el
  principal falta o está roto, se prueba el `.tmp`. Un principal ilegible se copia a `progreso.json.roto`, y un
  JSON de versión menor se respalda (el archivo que se leyó) como `progreso.json.v<N>.bak` antes de migrarlo;
  ningún respaldo pisa uno anterior. **Un JSON de versión mayor** (de un build más nuevo: otra rama, o volver
  atrás una versión) se respalda como `progreso.json.v<N>.futuro.bak` y el progreso queda en solo lectura: se
  juega con los campos que el build entiende, pero `Guardar` no escribe, así un build viejo no borra lo que
  agregó el nuevo. Lo que se gane o compre en ese estado se pierde al cerrar. Sólo `ReiniciarTodo` (las
  herramientas de editor) lo pisa.
- `MonedasEnteras` (floor con 1e-6) es lo que se muestra y lo que se puede pagar. `Sumar`, `Comprar` y las
  funciones de depuración incrementan `Revision`.
- **`Sumar` es para lo que se gana jugando y `CobrarPremio` para todo lo demás** (hoy, el x2 de un video).
  Están separados a propósito: un premio no tiene que contar como monedas ganadas jugando cuando entre el
  renacer. Ver Anuncios.
- `PlayerHealth` llama a `Progreso.TerminarPartida(segundos)` al morir, en el mismo bloque que guarda: cuenta
  la partida y el tiempo jugado, que es lo que mira la oferta de video para no premiar una partida de dos
  segundos.
- `MonedasDeLaPartida` vuelve a cero al empezar cada partida (`PlayerHealth.Awake`) y lo muestra la pantalla
  de derrota (`TextoMonedasPartida`). El HUD de las escenas de juego muestra el total con `ContadorMonedas`.
  Los números para pantalla pasan por `FormatoNumeros.Compacto` (1.234, 123 K, 4,5 M).

## Mejoras y tienda

Cada mejora es un ScriptableObject `Mejora` en `Assets/Mejoras/`, y `Assets/Mejoras/Resources/CatalogoMejoras`
las junta (un campo tipado por mejora y `enTienda`, el orden de las tarjetas). **El catálogo tiene que quedar en
`Resources`**, y **un `id` no se renombra nunca**: es la clave de los niveles guardados.

| mejora | id | precio inicial | crecimiento | tope | efecto |
|---|---|---|---|---|---|
| Daño de bala | `dano_bala` | 40 | ×1,45 | — | 1 × (1 + nivel) por bala |
| Cadencia | `cadencia` | 50 | ×1,45 | 16 | 4 × (1 + 0,25 × nivel) tiros/s (de 4 a 20) |
| Vida máxima | `vida_maxima` | 40 | ×1,45 | — | 80 × (1 + 0,25 × nivel) |
| Imán | `iman` | 30 | ×1,5 | 13 | sin comprar no hay; 2 × (1 + 0,25 × (nivel − 1)) m (de 2 a 8) |
| Botín | `botin` | 120 | ×1,55 | 15 | monedas × (1 + 0,1 × nivel) |
| Furia | `furia` | 5.000 | — | 1 | desbloquea el botón de furia: 6 s de cadencia y daño ×2 (ver Furia) |

- **El jugador arranca flojo a propósito** (fase 4, pedido de Ivan después de jugar en el teléfono): dispara lento, pega
  1, tiene 80 de vida y no tiene imán (junta las monedas pasándoles por encima). Lo que lo hace fuerte son las compras, y por eso los primeros precios
  son bajos: la primera partida tiene que alcanzar para una o dos. El daño y la cadencia suben de a uno para que
  cada compra se lea en la tarjeta ("1 → 2") y en los números de daño.
- **`Mejora.arrancaEnCero`**: sin comprar vale 0 y el nivel 1 vale `valorBase`. El tope se aplica antes de correr el
  nivel, así un nivel guardado de más no pasa del máximo. Lo usa el imán: sin comprarlo no hay imán.
- **Precio** = `floor(precioInicial × crecimiento^nivel + 0,5 + 1e-9)`. El `+1e-9` no es decorativo: en double
  90 × 1,45 da 130,4999…, y el precio correcto es 131. Los textos redondean igual (`FormatoNumeros.ConDecimales`).
  Se balancean tocando los assets.
- **Se aplican al empezar la partida, no al comprar.** `AplicarMejoras` está en la raíz de `Jugador.prefab` y en
  su `Awake` fija daño por bala, tiros por segundo, vida máxima (con la vida llena), el multiplicador de cura y el
  alcance del imán (`Moneda.FijarRadioIman`, estático porque las monedas salen de un pool): fija valores, nunca
  multiplica los actuales, así reintentar no aplica dos veces. La caja de vida cura
  `curaPorPickup × multiplicador de vida` (40 × el multiplicador: sigue siendo la mitad de la vida máxima). El
  botín lo leen los dos generadores en su `Start`.
- **La tienda es un panel del menú**, no una escena: `Prefabs/UI/Tienda.prefab` instanciado en `Menu.unity`, con
  canvas propio (1920x1080, match 0,5, `sortingOrder` 5, área segura). Las tarjetas (`Prefabs/UI/TarjetaMejora`)
  se generan desde `enTienda`, con tres estados: comprable (verde, respira), sin monedas (gris, "faltan N", tocable
  para que tiemble) y en tope (dorada, "MÁX" y estampa). Comprar: monedas que vuelan al botón, arpegio en la bemol
  programado con `Sonidos.Programar`, estallido y temblor; compras seguidas suben el arpegio por I-IV-V-I'.
- **Navegación:** MEJORAS en el menú abre la tienda; VOLVER, Escape o el atrás de Android la cierran; ¡A JUGAR!
  carga `UltimoModo` (1 o 3; si no, 3). En la derrota, MEJORAS llama a `TiendaMejoras.AbrirEnMenu`, que carga el
  menú con la tienda abierta. Los botones MEJORAS (`BotonMejoras`) muestran una insignia con
  `CatalogoMejoras.ComprasPosibles()`: cuántas compras seguidas alcanzan de verdad, eligiendo siempre la más barata
  (con 50 monedas hay cuatro tarjetas verdes pero alcanza para una sola), y en la derrota "¡Te alcanza para N
  mejoras!".
- **Para agregar una mejora:** un asset `Mejora` con id nuevo → su campo y getter en `CatalogoMejoras` → aplicarla
  en `AplicarMejoras` o en quien la consume → sumarla a `enTienda` → casos en `PruebasMejoras`.

## Anuncios

Los videos con recompensa son la única monetización del juego y entran por **dos lugares**: **revivir** al morir
y, si no revivió, el **x2 de las monedas en la pantalla de derrota**. **Un solo video premiado por partida**
(`vecesPorPartida`), así que en la práctica es o uno o el otro. Todo lo demás (topes, proveedor, hilos) vive en
`Assets/Scripts/Anuncios/` y el juego no habla nunca con una red de anuncios.

**Las reglas que no se negocian**, porque son la diferencia entre un premio y una trampa:

- **Siempre opt-in y en una pausa natural.** Nunca durante la partida: la derrota es el único momento, y el
  jugador ya terminó de jugar.
- **El premio se dice exacto antes de mirar** ("VER VIDEO: +137 MONEDAS"), no como sorpresa.
- **Cerrar el video antes no castiga**: no hay premio, pero tampoco se gasta el tope del día ni pasa nada más.
- **Si no se puede ofrecer, el botón no existe**, no aparece en gris.
- **Se puede apagar**: `Progreso.OfrecerVideos` apaga todas las ofertas y el servicio lo respeta. Hoy no hay
  ningún botón que lo toque (hubo uno en el menú y a Ivan no le gustó): queda para cuando haya pantalla de
  opciones.
- **Un premio nunca es "monedas ganadas jugando".** Entra por `Progreso.CobrarPremio`, no por `Sumar`: cuando
  entre el renacer de la fase 5, lo que se cobró con videos no tiene que contar para los cerebros.

**Las piezas:**

| pieza | qué hace |
|---|---|
| `LugarAnuncio` | los nombres de los lugares, como strings. Se guardan en el JSON: **un lugar no se renombra nunca**. Hoy se usan `revivir` y `duplicar_derrota`. |
| `IProveedorAnuncios` | quién muestra el video: `Listo(lugar)` y `Mostrar(lugar, aviso)`. Cambiar de red es escribir otra clase. |
| `ProveedorNulo` | nunca tiene video: no se ofrece nada. Es el de Windows y el de "todavía no hay red". |
| `ProveedorFalso` | el de las pruebas: un cartel a pantalla completa armado por código, con una barra de 5 s y SALTEAR / LISTO. Prueba el circuito entero sin cuenta ni internet, y anda igual en el teléfono. |
| `ConfigAnuncios` | todos los números, en `Assets/Anuncios/Resources/ConfigAnuncios.asset`. Si falta, no se ofrece nada (con un LogError). |
| `ServicioAnuncios` | la puerta: `PuedeOfrecer(lugar)` y `Mostrar(lugar, alPremiar, alNoPremiar)`. |
| `VigiaAplicacion` | un objeto con `DontDestroyOnLoad` que se instala solo. Vacía los avisos de los videos en el hilo principal y **guarda el progreso cuando la app pierde el foco en cualquier escena** (antes eso lo hacía sólo `MenuPausa`, que no está ni en el menú ni en la derrota). |
| `OfertaDeDuplicar` | el botón de la derrota (objeto `OfertaVideo` en `Perdiste.unity`, componente en `Menu`). |
| `OfertaDeRevivir` | la ventanita de "¡HAS MUERTO!" (prefab `Prefabs/UI/OfertaRevivir` en ShowBies1 y WaveMode). |

**Cuándo se ofrece** (valores del asset): a partir de la 2ª partida terminada, con 180 s jugados en total, hasta
3 veces por día, 1 por partida y con 60 s entre un video y otro. El x2 pide además una partida de 90 s y 20
monedas; revivir, una partida de 30 s.
El día es un `aaaammdd` local guardado en el progreso, y **atrasar el reloj del teléfono no reinicia los topes**
(sólo cuenta un día mayor al guardado).

**El aviso del SDK llega desde cualquier hilo y a veces dos veces.** Por eso `ServicioAnuncios` numera cada
solicitud, encola el aviso con un candado y lo resuelve una sola vez en el `Update` de `VigiaAplicacion`. Antes
de irse a pantalla completa guarda el progreso y los `PlayerPrefs`: Android puede matar la app mientras se ve
el video. El audio del juego se pausa y se restaura como estaba.

**Un video que se rompe al mostrarse se premia igual, pero una vez por día** (`fallasPremiadasPorDia`): no es
culpa del jugador, pero cortar la red no puede ser la forma fácil de cobrar sin mirar nada.

**El botón ocupa el renglón del aviso "¡Te alcanza para N mejoras!"**, que se calla mientras la oferta está y
vuelve cuando se resuelve, con más monedas si el jugador cobró (`OfertaDeDuplicar.TapaElAviso`, que mira
`BotonMejoras`).

**A partir de una hora de sesión la derrota sugiere descansar** (`AvisoDescanso`, `minutosParaAvisoDeDescanso`).
No bloquea nada.

### Revivir

Cuando el jugador muere y hay un video, la partida **no termina**: `PlayerHealth` le pregunta a
`OfertaDeRevivir` y, si esta se hace cargo, el juego queda congelado (`Time.timeScale = 0`) con el jugador
muerto en el lugar donde cayó. La pantalla se va agrisando en 5 s mientras una ventanita muestra
"¡HAS MUERTO!" y un botón de video con un anillo que se cierra en 10 s. Recién cuando el jugador dice que no,
o se vence el reloj, se llama a `PlayerHealth.Terminar` (récord, `TerminarPartida`, escena de derrota).

- **Una sola vez por partida** (`PlayerHealth.yaRevivio`): con un revivir por video sin límite la partida no
  termina nunca y la tienda deja de tener sentido.
- **Volver no regala nada más que seguir jugando**: `EnemyController.DespejarAlrededor` saca del mapa a los
  zombis que estén a `radioDeDespeje` (7 m) **sin puntos, monedas ni mancha**, como el kill-Z, y el jugador
  vuelve con la vida llena y `segundosDeGracia` (2,5 s) sin recibir daño. Si esos zombis dieran monedas, el
  video sería la forma barata de cobrar una pantalla llena.
- **Que se venza el reloj es exactamente lo mismo que decir que no**, y el botón NO, GRACIAS está desde el
  primer segundo y se lee igual de bien que el otro.
- Mientras la ventana está abierta, `OfertaDeRevivir.Activa` es cierto y **`MenuPausa` no pausa**: reanudar
  desde el menú de pausa devolvería el `timeScale` a 1 con el jugador muerto. Si la escena se descarga con la
  oferta abierta, `OnDestroy` devuelve el `timeScale`.
- Los tres dibujos del botón (círculo, anillo y triángulo de play) los hace `TexturasUI` en código, así que no
  hay imágenes nuevas en el proyecto; el componente es dueño de esas texturas y las destruye.

**Todavía no hay red de anuncios de verdad.** `ConfigAnuncios.proveedor` está en `Falso` y `Real` no existe:
cuando se integre (AdMob o LevelPlay) es una clase nueva que implemente `IProveedorAnuncios` y un `case` en
`ServicioAnuncios`. Nada del juego se entera. Ojo con dos cosas al integrarla: el plugin de AdMob para Unity
está roto en Unity 6000.3.17 y posteriores (issue 4212 del repo), y AdMob sólo sirve anuncios de verdad cuando
la app ya está publicada y vinculada a su ficha de Play.

## Jugo

Lo que hace que cada acción se sienta vive en `Efectos` (`Assets/Scripts/Jugo/`), dentro del prefab
`Assets/Prefabs/Jugo/Efectos.prefab` puesto en ShowBies1, WaveMode y Tutorial. Quien produce un evento llama a
un método static (`Efectos.Golpe`, `Muerte`, `Explosion`, `DanioJugador`, `Caja`, `Disparo`, `CartelOleada`), que
**no hace nada si la escena no tiene el prefab**: el juego anda igual, plano. La excepción es `ParticulasDeMuerte`,
que sin `Efectos` instancia las partículas del zombi como antes.

- **Golpe a un zombi** (en `EnemyController.DanoZombi`, así cubre balas y granada): número de daño
  (`NumeroFlotante`, TextMeshPro 3D con Bangers y shader overlay, de un pool de 40), chispas y un tic. Si no
  muere, además destello blanco (sus renderers visibles pasan un instante al material `Destello`) y un aplastado
  de escala que se recupera en `FixedUpdate`. El material usa `Assets/Shaders/Destello.shader`, un color plano como
  `Unlit/Color` pero con pasada de sombra: con `Unlit/Color` el zombi dejaba de proyectar sombra mientras estaba blanco.
- **Muerte**: chispas y sonido. Desde `vidaParaMuerteGrande` (el tanque) suma temblor fuerte y una **pausa de
  impacto** (`timeScale` a 0,05 un instante); el jefe, más. Cada muerte cuenta para `ContadorCombo` en el HUD
  ("COMBO xN", con una ventana de 1,5 s entre muertes).
- **Granada**: temblor, estruendo, chispas y una pausa corta. **Daño al jugador**: temblor, borde rojo
  (`VinetaDanio`) y sonido, con 0,4 s mínimos entre dos, para que rodeado no quede prendido. **Cajas**: `pop.mp3`
  y chispas. **Disparo**: chispas en la boca del arma. **Cartel de oleada**: jingle en la bemol mayor y un rebote
  de escala (`AparecerConRebote`).
- **Temblor de cámara**: `CamaraJugador.Temblar(trauma)`. El trauma (0 a 1) se descarga solo y la sacudida crece
  con su cuadrado, así los golpes chicos casi no se notan. Usa tiempo sin escalar y se frena en la pausa del menú.
- **Sonidos**: `Sonidos.Tocar(clip, volumen, pitch, variación, separación mínima)`, un solo objeto con fuentes 2D
  que también usan las monedas y la tienda. Los de pitch 1 van a una fuente que nunca cambia de tono; los demás
  eligen una fuente libre según cuándo termina lo último que puso cada una (guardado con `dspTime`, porque
  `isPlaying` no sirve con `PlayOneShot`): reusar una que suena le cambia el tono a esa nota. `Sonidos.Programar`
  hace lo mismo con `PlayScheduled`, para los arpegios de la tienda.
- **UI**: `BotonJugoso` anima un hijo `Visual` del botón (apretar, rebotar, respirar, temblar), nunca la raíz, así
  el layout no se entera. `EfectosUI` (monedas que vuelan, estallidos, textos flotantes) va en un sub-Canvas con
  pools propios. Todo con tiempo sin escalar y delta topeado.
- **Chispas**: un solo `ParticleSystem` por escena (`Particulas/Chispas.prefab`) usado con `Emit`, como el brillo
  de las monedas.
- **Partículas de muerte**: cada zombi tiene las suyas (`deathParticles`, los `Particulas/Explosion*`, una ráfaga de
  50 de su color). `EnemyController` llama a `Efectos.ParticulasDeMuerte`, que arma **una copia de cada prefab por
  escena** y emite ahí la ráfaga en el lugar de la muerte, en vez de un `Instantiate` por muerte. La copia pasa a
  espacio mundo (en espacio local la posición del `Emit` se toma relativa a la copia y escalada con ella, y todas las
  ráfagas vivas se moverían con la copia), con la emisión propia apagada y el
  límite de velocidad multiplicado por la escala del prefab: en espacio local ese límite se compara en unidades del
  prefab escalado, y sin corregirlo las partículas de los prefabs chicos no frenaban. Así se comprobó, midiendo
  distancia, tamaño y velocidad contra el prefab original, que se ve igual. **Si un prefab de explosión deja de ser
  una sola ráfaga fija sin loop** (emisión continua, sistemas hijos, gravedad, ruido, fuerzas, colisiones, escala
  despareja) `CrearCopiaDeMuerte` no lo acepta y se instancia como antes; si le cambiás otra cosa, volvé a comparar.
- **Música de partida**: un loop de 32 s en la bemol mayor en el `AudioSource` del prefab (Vorbis, comprimida en
  memoria). La música y los sonidos nuevos están sintetizados y son provisorios.

## Persistencia

El récord, el último modo y el tutorial van por `PlayerPrefs`; las monedas y el progreso no (ver Monedas y
progreso):

| clave | quién escribe | quién lee |
|---|---|---|
| `"Score"` | `PlayerHealth` al morir | `Score` (pantalla de derrota) |
| `"HighScore_<buildIndex>"` | `PlayerHealth`, si superás el récord de ese modo | `highscoretext`, el del modo en `"UltimoModo"` |
| `"UltimoModo"` | `PlayerHealth`, el buildIndex de la escena | `MenuPerdiste.Retry`, `highscoretext`, `TiendaMejoras.Jugar` |
| `"TutorialCompletado"` | `TutorialManager`, al terminar el tutorial | nadie todavía |

Hay **un récord por modo** (`HighScore_1` el libre, `HighScore_3` las oleadas), y la clave la arma
`PlayerHealth.ClaveRecord`. La clave vieja `"HighScore"`, que compartían los dos modos, quedó sin uso.

`PlayerHealth.TakeDamage` llama a `PlayerPrefs.Save()` explícitamente. Si agregás una clave, escribila
en ese mismo bloque o se pierde cuando el juego no cierra bien.

`VigiaAplicacion` (ver Anuncios) guarda el progreso cuando la app pierde el foco **en cualquier escena**,
incluidos el menú y la derrota, donde no hay menú de pausa que lo haga.

`"UltimoModo"` es lo que hace que "Retry" vuelva al modo que estabas jugando y no siempre al primero.
La tecla R hace lo mismo por otro camino: recarga la escena activa.

## Pausa y botón atrás

`MenuPausa` (`Assets/Scripts/UI/`) vive en el prefab `Assets/Prefabs/UI/MenuPausa.prefab`, puesto en
ShowBies1, WaveMode y Tutorial: un canvas propio por encima del HUD (`sortingOrder` 10) con el botón de
pausa (sólo móvil, arriba al centro y dentro del safe area) y el panel Continuar / Reiniciar / Menú
principal. Se abre con ese botón, con Escape y **sola cuando la app pierde el foco** (una llamada, la
cortina de notificaciones, alt-tab). Esto último no corre en el editor, que pierde el foco con cada
click en otra ventana.

- Pausar es `Time.timeScale = 0` más `AudioListener.pause`. Todo el juego usa tiempo escalado (física,
  `WaitForSeconds`, los `Time.time` de la mejora de cadencia y de los pickups), así que se congela sin
  tocar nada más. **El input no se congela**: `PlayerController` y `PlayerJS` miran
  `MenuPausa.Pausado` antes de leerlo. Lo que agregues que lea input tiene que hacer lo mismo.
- `timeScale` y `AudioListener.pause` son globales y cruzan escenas. Los botones del panel los
  restauran antes de cargar otra escena, y `OnDestroy` también, por si la escena se descarga en pausa
  por otro camino (la R de `RestartScene`).
- **El botón atrás de Android llega como `KeyCode.Escape`**, también con el back predictivo activado
  (`androidPredictiveBackSupport: 1`, targetSdk 36): el player de Unity registra su propio
  `OnBackInvokedCallback` y reinyecta `KEYCODE_BACK` a la actividad. Deja de llegar si alguien pone
  `Input.backButtonLeavesApp = true`.
- Cada pantalla decide qué hace Escape: en juego pausa y reanuda (`MenuPausa`), en la derrota vuelve al
  menú (`MenuPerdiste`), y en el menú principal cierra primero la tienda, después el panel de modos o, en el
  principal, sale del juego sólo en móvil (`BotonAtrasMenu`, en el canvas "Main Menu", único lector de Escape del
  menú). `RestartScene` ya no cierra el
  juego con Escape: en PC, para salir está Quit.

## Móvil

Android es el objetivo principal (builds de APK y AAB, pausa con el botón atrás, joystick de granada), pero
**el código compila para las dos plataformas**, y eso es deliberado: no hay ningún `#if UNITY_ANDROID` en el código del juego. Quien decide es
**`Plataforma.EsMovil`** (`Assets/Scripts/Plataforma.cs`), el único criterio de "estamos en móvil"
que usan `PlayerJS`, `PlayerController` y `ConditionalShow`: en una build es la plataforma real; en
el editor es el **build target activo**. Consecuencia útil: con el target en Android, el editor se
comporta como un teléfono — los joysticks responden al mouse y el teclado se apaga — así que el
control táctil se prueba sin dispositivo. Con target Windows, jugás con teclado como siempre.
Antes había dos criterios distintos (defines de compilación vs. `Application.isMobilePlatform`) y
en el editor con target Android los joysticks se veían pero no respondían.

- `PlayerJS` lee los dos `FixedJoystick` del Canvas y llama a `PlayerController.Move(Vector2)`.
- `PlayerController.Update` **se corta enseguida en móvil** para no pelearse con el joystick por
  `moveVelocity` y por `isFiring`.
- `ConditionalShow` prende y apaga objetos por plataforma (`showOnAndroid` / `showOnPC`). Los joysticks
  ya están puestos en el Canvas de las dos escenas de juego con eso, igual que el **botón de granada**
  (`BotonGranada`, sólo Android), que es un joystick para apuntarla (ver la sección Granada).

**Para jugar con teclado y mouse en el editor sin cambiar el target** (que reimporta todo): **ShowBies > Controles >
Teclado y mouse en el editor**. Es una preferencia de la máquina (`EditorPrefs`), no del proyecto, y hace que
`Plataforma.EsMovil` dé falso en el editor aunque el target sea Android. Se aplica al entrar en play. Ojo que
también cambia los techos que dependen de la plataforma: 60 zombis vivos y 150 monedas en vez de 35 y 80.

**No vuelvas a meter un `#if UNITY_ANDROID` alrededor de una clase entera.** Ver la trampa de abajo.

### Rendimiento en móvil

La primera prueba en un teléfono dio bajos FPS. Lo que hay y por qué:

- `ConfiguracionRendimiento` (`Assets/Scripts/`) corre antes de la primera escena: pone
  `Application.targetFrameRate = 60` — **Unity en Android limita a 30 FPS por defecto** si nadie lo
  sube — y en móvil renderiza a `EscalaResolucionMovil` (0.75) de la resolución nativa. La UI no se
  entera porque los canvas escalan con la pantalla.
- Android usa el nivel de calidad **Medium**: sombras duras, 20 m, 1 cascada, resolución baja, sin
  AA ni anisotrópico, texturas a mitad de resolución (`globalTextureMipmapLimit = 1`; las del piso
  son 4K). El editor corre en Ultra, así que **lo que ves en el editor no es lo que ve el teléfono**.
- Las cámaras de las escenas de juego tienen HDR y MSAA apagados (sin post-proceso no aportan nada),
  y todo lo estático está marcado `BatchingStatic` (las 80 calles de WaveMode eran 80 draw calls).
- Los generadores usan `maxZombisVivosMovil` (35) en vez de 60 cuando `Plataforma.EsMovil`.
- Los Animators de los zombis están en **Cull Update Transforms**: fuera de pantalla no
  mueven huesos. El rápido tiene **dos** Animators: el que tiene el esqueleto usa el controller y el avatar del
  normal, sin root motion; el otro no tiene controller y no hace nada (ver la trampa del zombi invisible).
- Todos los zombis se ven con el modelo del zombi de ToonyTiny, que crece o se achica con la escala de la raíz. El
  normal, el tanque, el FASTER y el jefe lo tienen de hijo directo (`TT_demo_zombie.FBX`, escala 1,2; a y −0,79 el
  normal y a y −1 los otros tres, con los pies en el fondo de la cápsula: a −0,79 el jefe flotaba 0,4 m). El
  rápido es la excepción: su hijo es el prefab `zombiRapido` de ToonyTiny, a y −0,92, con la malla, el avatar y el
  controller de `TT_demo_zombie` pisados (ver la trampa del zombi invisible). Cada tipo tiñe el modelo con su
  material de `Assets/Materiales/`: el normal sin teñir, `ZombiRapidoPiel` lima, `ZombiTanquePiel` rojo,
  `ZombiFasterPiel` celeste y `ZombiJefePiel` violeta. **La cápsula y los dos cubos de cada prefab son sólo
  colliders**, con los renderers apagados: los cubos son las hitboxes y no se borran (con la cabeza grande y los
  brazos de la animación, el modelo cubre casi toda la cápsula). `EnemyController.velocidadDeAnimacion` ajusta el paso del modelo a lo que camina
  cada uno (1 el normal y el rápido, 0,45 el tanque, 2,5 el FASTER, 0,3 el jefe). Antes el tanque, el jefe y el
  FASTER eran la cápsula y los cubos a la vista.
- `ContadorFps` muestra los FPS en el HUD de las escenas de juego, para medir en el teléfono sin
  Profiler. "Anda lento" no se optimiza; "32 FPS con 35 zombis" sí.

### Build de Android

Dos entradas de menú en `Assets/Editor/ConstructorAndroid.cs`, ambas escriben el veredicto en
`Builds/build_result.txt` (raíz del repo, gitignoreada) y sirven por CLI con `-executeMethod`:

- **Build > Android APK** (`ConstructorAndroid.BuildApk`): `Builds/ShowBies.apk` firmado con el
  debug keystore, para probar en el teléfono. No pide nada.
- **Build > Android AAB (release)** (`ConstructorAndroid.BuildAab`): `Builds/ShowBies.aab` firmado
  con el keystore de release, que es lo que se sube a la Play Store. Lee ruta, alias y passwords
  de `ShowBies1/keystore.local` (gitignoreado; plantilla en `keystore.local.example`) y los limpia
  de `PlayerSettings` al terminar, así el keystore nunca queda configurado en `ProjectSettings` ni
  la build de APK se rompe por falta de password.

- Configuración: package `com.ivru.showbies` (cambiable hasta publicar, después queda fijo),
  IL2CPP + ARM64, minSdk 25, targetSdk automático, `bundleVersion` / `AndroidBundleVersionCode`
  en `ProjectSettings.asset` (el versionCode tiene que subir en cada subida a la Play Store).
- Orientación: rotación automática sólo entre los dos horizontales (`defaultScreenOrientation: 4`, sin
  portrait). Antes estaba fija en uno solo (`reverseLandscape` en el manifest) y no giraba con el
  teléfono al revés.
- **Keystores: `*.keystore`, `*.jks` y `keystore.local` están gitignoreados.** Había un
  `ShowBies1/user.keystore` de 2023 versionado, con password desconocida; se sacó del repo (queda
  en disco por si aparece la password). El de release se genera con `keytool` y se guarda con
  backup fuera del repo: si se pierde, no se puede actualizar la app publicada (salvo con Play App
  Signing, que conviene activar al subirla por primera vez).
- Los restos de Unity Mediation (discontinuado por Unity) ya se borraron; el paquete nunca estuvo
  en `manifest.json`. La red de anuncios de verdad todavía no está (ver Anuncios).
- **La APK fuerza el proveedor de anuncios Falso** mientras dura la build y después deja el asset como
  estaba, así el cartel de prueba llega siempre al teléfono. **El AAB se niega a construirse si el
  proveedor está en Falso**: un anuncio de prueba en la Play Store no.
- El `totalSize` del BuildReport miente: cuenta símbolos e intermedios (~430 MB); el APK real son
  ~32 MB. La carpeta `*_BurstDebugInformation_DoNotShip` que aparece al lado del APK no se
  distribuye.
- El primer switch de plataforma reimporta todos los assets (~10+ min con el proyecto en disco
  mecánico); después queda cacheado en `Library/`.

## Convenciones

- Los comentarios y los mensajes de commit van en **español**. Los commits: infinitivo imperativo, sin
  tildes en el asunto, con un cuerpo que explique el porqué y no sólo el qué. **Sin trailer de
  co-autoría.**
- Los nombres del código mezclan español e inglés sin criterio (`DanoZombi`, `dañoDar`, `theGun`,
  `spawnEnemy`). Seguí el idioma del archivo que estés editando.
- Los archivos están en **UTF-8**. Ver la trampa del encoding.

## Trampas conocidas

- **`#if UNITY_ANDROID` alrededor de una clase entera borra los datos de las escenas.** Si la clase
  compila vacía en Windows, Unity guarda las escenas según los campos que el script tiene *en ese
  momento*: al no tener ninguno, **borra las referencias serializadas**. Ya pasó con `PlayerJS`, que
  perdió el cableado de los dos joysticks en las dos escenas sin que nadie se enterara. Si necesitás
  código específico de plataforma, decidí en runtime.

- **Los archivos deben ser UTF-8, no CP1252.** Los identificadores con `ñ` (`daño`, `dañoDar`) son
  reales y están en el código. Un editor que guarde en Latin-1 los convierte en bytes inválidos que
  Roslyn lee como `U+FFFD`, y eso es **CS1056: Unexpected character**, no un warning. El proyecto
  entero estuvo sin compilar por esto. Además el nombre del campo `daño` tiene que coincidir con el
  `"da\xF1o"` serializado en los `.asset` de los zombis, o los stats se pierden.

- **Los `static` cruzan escenas.** `ZombisVivos`, `jugadorCache`, el pool de balas, el caché de sprites
  de sangre, el pool de monedas, el pool de zombis, el de manchas y los datos de `Progreso` sobreviven al `LoadScene` (los pools
  descartan las referencias a objetos que la escena ya destruyó). Están todos en el reset de `SubsystemRegistration`. Si te olvidás,
  el síntoma típico es un contador que queda alto y deja al generador tapado para siempre.

- **Una corrutina no sobrevive a la muerte de su GameObject.** `EnemyController` arrancaba una corrutina
  sobre el zombi para borrar la mancha de sangre y en la línea siguiente destruía al zombi: la corrutina
  nunca llegaba al `WaitForSeconds` y las manchas quedaban en la escena para siempre. Para limpiar algo
  después de destruir al que lo pidió, usá `Destroy(obj, segundos)`, que lo maneja el engine, o dale al objeto su
  propio reloj, como `ManchaDeSangre`.

- **Las partículas hijas con `stopAction = Destroy` se cortan si destruís al padre.** La explosión de la
  granada es hija del prefab. Hay que despegarla (`SetParent(null)`) antes de destruir la granada, y
  entonces se limpia sola.

- **Los índices de escena están hardcodeados.** Ver la tabla de arriba.

- **Las cajas no tienen techo de cantidad, sólo caducidad.** `PowerUp` spawnea cada 8 s (balas y vida) y cada
  20 s (arma): son los valores de las escenas, el código dice 3,5. Cada prefab lleva `PickupCaducidad`, que la
  destruye a los 30 s parpadeando los últimos 3; el tutorial la apaga para que esperen al jugador.

- **Lo que se repite en cada muerte no se crea ni se destruye.** El zombi vuelve a su pool, la mancha de sangre sale
  del de `ManchaDeSangre` (se apaga sola a los `duracionMancha` segundos, con tiempo escalado como el `Destroy`
  diferido de antes), las partículas de muerte salen de las copias de `Efectos`, y las monedas, los números de daño
  y las barras de vida también se reusan. Siguen siendo `Instantiate`/`Destroy` la granada (con su explosión, una
  cada 5 s como mucho), las cajas y los zombis del tutorial.

- **Las tags `ZombiNormal`, `ZombiBoss`, `ZombiFaster`, `ZombiRapido` y `ZombiTanque` siguen en el
  `TagManager` y en los prefabs, pero ya no las usa nadie.** Para detectar un zombi se pide el
  `EnemyController`: tener el componente es ser un zombi. No vuelvas a ramificar por tag. Las tags que
  sí se usan son las de los pickups —`PUBalas`, `PUVida`, `PUArma`, iguales al nombre de su prefab— y
  `Player`. (Antes eran `Balas`, `Vida` y `pwBalas`, cruzadas con lo que hacían; ya no existen.)

- **Los zombis tienen TRES colliders**: el capsule de la raíz y dos hitboxes hijas ("Cube"). Todo lo
  que resuelva un zombi desde un collider tiene que usar `GetComponentInParent`, no `GetComponent`
  (una bala que tocaba una hitbox hija rebotaba sin dañar). Y todo lo que dañe por área
  (`OverlapSphere`) tiene que deduplicar por componente, porque los tres colliders resuelven al mismo
  `EnemyController` y sin dedup el daño se multiplica por tres.

- **Los zombis pegan por intervalo, no por choque.** `EnemyController` pega al tocar al jugador y después
  cada `intervaloDeGolpe` (0,8 s) mientras siga en contacto (`OnCollisionEnter` y `OnCollisionStay` con un
  reloj por zombi). Antes pegaba sólo en `OnCollisionEnter`: un zombi pegado no volvía a dañar hasta
  separarse, y el daño dependía de cuánto temblara la física. El reloj también hace que los varios colliders
  del zombi y del jugador no cuenten el mismo toque más de una vez.

- **La muerte necesita guarda.** Dos golpes letales en el mismo paso de física llaman a `DanoZombi` (o
  `TakeDamage`) dos veces con la vida ya en cero: los eventos de colisión del paso se despachan aunque el zombi ya se
  haya apagado (o, con `Destroy`, aunque todavía no se haya ido), y sin el flag `estaMuerto`
  el bloque de muerte corre entero de nuevo — puntos dobles, dos manchas. Si agregás otra fuente de
  daño, no repitas la lógica de muerte: llamá a esos métodos, que ya están guardados.

- **Hay un kill-Z en Y = -20 para el jugador y en Y = -2 para los zombis**, y no es decorativo: el mapa tiene
  bordes por los que la física empuja cosas, y un zombi caído seguía contando en `ZombisVivos` — cada caído era un
  cupo del techo de población perdido para siempre. El de los zombis es más alto porque bajo el piso ya no se ven
  ni se les puede disparar. Si agregás entidades con Rigidbody que importen, dales su propio kill-Z.

- **Los zombis caminan en horizontal y la gravedad es de la física.** `EnemyController` mira al jugador a su propia
  altura y sólo pisa la velocidad horizontal. Antes miraba al centro del jugador y pisaba la velocidad entera en cada
  paso: la gravedad nunca actuaba, y un zombi que terminaba bajo el piso (que es un plano sin espesor) se quedaba
  ahí persiguiendo al jugador. Además los puntos de aparición de WaveMode están en Y = 0 y el pivote del zombi es el
  centro de su cápsula: `SubirSobreElPiso` lo levanta cada vez que aparece (`OnEnable`) para que no nazca medio
enterrado.

- **El "zombi invisible" era el rápido, que no tenía malla.** `ToonyTinyPeople/TT_demo/models/zombiRapido.FBX` es en
  realidad un glTF binario con extensión `.FBX`: Unity no le encuentra mallas y el prefab quedaba con el
  `SkinnedMeshRenderer` y la cabeza sin malla, y sin controller. Pegaba y chocaba (la cápsula está) pero no se veía.
  `Prefabs/Personajes/ZombiRapido.prefab` ahora pisa la malla, la cabeza, el avatar y el controller con los de
  `TT_demo_zombie.FBX` (mismos 15 huesos en el mismo orden). No uses ese FBX para nada nuevo. Para que no se
  confunda con el normal, el cuerpo y la cabeza usan `Materiales/ZombiRapidoPiel.mat`: la textura de TT_demo con
  `_Color` verde lima (Legacy Diffuse multiplica la textura por ese color).

- **Player (capa 6) y Bala (capa 7) no colisionan, y eso está en la matriz del proyecto.** Antes se
  seteaba con `Physics.IgnoreLayerCollision(6, 7)` en el `Start` de cada bala. No lo hagas por código.

- **El `AudioSource` con `pedo` que cuelga del Jugador no suena nunca**: quedó sin disparador cuando se limpió
  `PlayerHealth`. (`pop.mp3`, que antes tampoco usaba nadie, ahora suena al agarrar cajas.)

- **La pausa de impacto toca `Time.timeScale`, igual que el menú de pausa.** `Efectos` lo baja un instante y
  lo devuelve a 1 sólo si `MenuPausa.Pausado` es falso, y lo restaura si la escena se descarga en el medio. Lo que
  agregues que cambie `timeScale` tiene que respetar lo mismo, y lo que deba seguir andando durante la pausa de
  impacto (UI, temblor, sonidos) tiene que usar tiempo sin escalar. Para animaciones que arrancan al cargar una
  escena, topeá el delta: el primer frame dura mucho y se come la animación.

- **`CatalogoMejoras.asset` tiene que estar en `Resources`.** Si se mueve o pierde referencias, todas las mejoras
  quedan en los valores base (1 de daño, 4 tiros/s, 80 de vida, sin imán, botín ×1) y en la build no se ve
  ningún aviso: sólo un LogError y la prueba de lógica.

- **Menu y Perdiste tienen el canvas en match 0; la tienda y la pausa, en 0,5.** En 20:9 el menú mide 864 u de
  alto y en 21:9, 823: una fila de tarjetas de 560 u no entraba en el canvas del menú, por eso la tienda tiene el
  suyo.

- **Construir UI en el editor ensucia el atlas dinámico de Bangers** (`Bangers SDF.asset`) y el fallback de
  LiberationSans. Si aparecen modificados en git sin haber tocado fuentes, se restauran. Bangers no tiene `→`: la
  flecha de las tarjetas es un sprite.

- **Un `Image` sin sprite ignora `Image.Type.Filled`.** La barra del anuncio de prueba se veía llena desde el
  primer frame por eso; ahora mueve el ancho del `RectTransform`. Lo mismo vale para cualquier medidor que se
  arme por código con un rectángulo de color.

- **Al duplicar un botón del menú, no le cambies la transición a None.** Los botones del menú tienen un Image negro
  que la transición ColorTint deja invisible; con None aparece.

## Pruebas y medición

- **ShowBies > Pruebas > Logica de mejoras** (`PruebasMejoras.CorrerTodas`): precios, efectos, textos, escalado,
  acumuladores, guardado y migración (en una carpeta temporal), compras y **todo el circuito de los anuncios**
  (premio una sola vez aunque el SDK avise dos, cerrar sin castigo, topes del día, falla premiada, el x2
  completo), con un proveedor de mentira que se enchufa con `ServicioAnuncios.UsarParaPruebas`. No corre en
  play. Escribe `Builds/pruebas_mejoras.txt` y termina en `RESULTADO: TODO OK` o `N FALLAS`.
- **ShowBies > Pruebas > Medir partida (10 s)** (`PruebasMejoras.MedirPartida`), en play: dispara sin parar, mata
  a cada zombi después de registrar sus multiplicadores (así las oleadas avanzan) y compara con la tabla lo
  aplicado, los tiros por segundo por régimen (con y sin caja), la vida, el daño y las monedas de cada zombi. Escribe
  `Builds/medicion_mejoras.txt`.
- **ShowBies > Progreso > …** (`HerramientasProgreso`): sumar monedas, niveles de prueba (5, 10, 5, 6, 15 y la
  furia: daño, cadencia, vida, imán, botín, furia), niveles en cero, reiniciar. **Escriben el `progreso.json` real del editor**, igual
  que `MedirPartida`.
- **`MedidorBalance`**: F1 (o tres dedos) en partida muestra daño, tiros por segundo medidos contra esperados, vida,
  botín, imán, multiplicadores de la oleada o del nivel y monedas por zombi. Existe sólo en el editor y en builds de
  desarrollo; la APK de `ConstructorAndroid` no lo es.

## Para agregar una mecánica nueva

1. ¿Es un tipo de enemigo? Creá un `Enemy` nuevo en `Assets/Zombies/` (**con `puntos`, `monedasMin` y
   `monedasMax` cargados**; si no, valen 1, 1 y 3), un prefab con `EnemyController`, y sumalo al generador (en
   WaveMode, a `tipos` del `WaveManager`, con su `desdeOleada` y su `peso`). No hace falta tag ni tocar código. Si lo
   hacés aparecer desde código nuevo, con `EnemyController.Aparecer`, no con `Instantiate`.
2. ¿Spawnea objetos seguido? Pooleá desde el principio: mirá `BulletController.Obtener` / `Devolver`.
3. ¿Necesita estado global? `static` + reset en `SubsystemRegistration`.
4. ¿Suma puntos o monedas? Que salga de `DanoZombi` (`enemyType.puntos`, y las monedas que suelta), no de
   donde se produce el daño.
5. ¿Guarda algo entre partidas? Si es progreso (monedas, mejoras), va en `Progreso` y su JSON. Los
   `PlayerPrefs` quedan para el récord y el último modo, en el bloque de `PlayerHealth.TakeDamage`.
6. ¿Tiene UI? Los cuatro canvas usan `ScaleWithScreenSize`. Las escenas de juego tienen la referencia
   en 1080x1920 (vertical, herencia de móvil): parece un error pero con `match = 0.5` la escala sale de
   la raíz del producto ancho × alto, así que da lo mismo que 1920x1080.
7. ¿Toca una escena o un prefab? Verificá el diff: Unity re-hornea bastante al guardar, y desde Unity 6
   agrega un `SceneRoots` (`--- !u!1660057539`) a cada escena. Lo que hay que confirmar es que no
   desaparezca ningún objeto, comparando los `--- !u!` contra HEAD.
8. Archivo nuevo en `Assets/Scripts/<Subsistema>/`.
9. ¿Lee input? Cortalo con `MenuPausa.Pausado`: la pausa congela el tiempo escalado, no el input.
10. ¿Pasa algo que el jugador tiene que sentir? Sumale su método a `Efectos` (sonido, chispas, temblor) en vez de
    poner sonidos y partículas sueltos: el juego tiene que ser llamativo en cada acción.
11. ¿Se compra? Es una `Mejora` (ver Mejoras y tienda).
