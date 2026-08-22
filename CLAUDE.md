# CLAUDE.md

Guía de arquitectura de **ShowBies** para agentes que trabajen en este repo.

## El juego

ShowBies es un twin-stick shooter 3D de zombis, top-down, para Windows. El jugador se mueve con WASD,
apunta con el mouse (raycast contra un plano en Y=0), dispara manteniendo click y tira granadas con
Espacio. Los zombis aparecen solos, van derecho hacia el jugador y le pegan por colisión. Matar suma
puntos, morir guarda el highscore y lleva a la pantalla de derrota.

Hay **dos modos**, los dos jugables desde el menú:

- **Free mode** (`ShowBies1.unity`) — generación continua: cinco corrutinas paralelas, una por tipo de
  zombi, cada una con su intervalo. Sin final.
- **Wave mode** (`WaveMode.unity`) — oleadas de 10 enemigos cada 2 s, con un tipo nuevo cada 5 oleadas.
  Mismo mapa pero con las calles (`Ciudad`) encendidas.

## Entorno

- **Unity 6000.3.14f1**, render built-in, 3D. `ProjectSettings/ProjectVersion.txt` es la fuente de verdad.
- El proyecto **nació en Unity 2020.3.26f1** y se subió a Unity 6. Buena parte de las rarezas del repo
  son cola de esa migración; si algo parece escrito para una API vieja, probablemente lo esté.
- Target real: **Windows standalone**, 1920x1080, borderless (`FullScreenWindow`), ventana redimensionable.
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
Assets/Scripts/Jugador/     ← PlayerController, PlayerHealth, PlayerJS (móvil), Transitions
Assets/Scripts/Zombi/       ← EnemyController, Enemy (ScriptableObject), GeneradorZombis, WaveManager
Assets/Scripts/Camara/      ← CamaraJugador
Assets/Scripts/UI/          ← ConditionalShow, Score, highscoretext
Assets/Scripts/PowerUps/    ← PowerUp (el spawner)
Assets/Scripts/*.cs         ← CanvasHelper, MainMenu, MenuPerdiste, Puntaje, RestartScene
Assets/Escenas/             ← Menu, ShowBies1, Perdiste, WaveMode (+ SampleScene, sin usar)
Assets/Prefabs/             ← Bullet, Gun, Granada, power-ups, Particulas/, Personajes/
Assets/Zombies/*.asset      ← los cinco Enemy: stats POR TIPO, editables sin recompilar
Assets/otros/               ← los 4 audios del juego
Assets/Editor/              ← MediationAdapterDependencies.xml (resto del intento de ads en Android)
```

**Código nuevo va en `Assets/Scripts/<Subsistema>/`**, nunca suelto en la raíz de `Assets/`.

## Escenas y build settings

| índice | escena | qué es |
|---|---|---|
| 0 | `Menu.unity` | menú principal + panel de modos |
| 1 | `ShowBies1.unity` | free mode |
| 2 | `Perdiste.unity` | pantalla de derrota |
| 3 | `WaveMode.unity` | wave mode |

**Los índices están hardcodeados en el código** (`MainMenu.PlayGame` → 1, `MainMenu.GameModes` → 3,
`PlayerHealth` → 2, `MenuPerdiste.Menu` → 0). Reordenar Build Settings rompe la navegación en silencio.

`SampleScene.unity` no está en el build y no se usa. Es el único lugar donde queda `AudioArma`.

## Arquitectura: singletons y referencias de inspector

No hay sistema de eventos. Los managers son `public static X instance` asignados en `Awake()`:
`PlayerHealth.instance`, `Puntaje.instance`. El resto se conecta por campos públicos arrastrados en el
inspector (`PlayerController.theGun`, `EnemyController.enemyType`, etc.).

Lo que se comunica sin inspector usa búsquedas cacheadas:

- `EnemyController.jugadorCache` — el jugador se busca **una vez** y se comparte. Antes había un
  `FindObjectOfType` por zombi spawneado, que con zombis en escena era costo cuadrático.
- `EnemyController.ZombisVivos` — contador `static`, `Awake`/`OnDestroy`. Lo miran los dos generadores.
- `BulletController.pool` — la pila de balas dormidas.

**Todo lo `static` se resetea en un `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]`**, porque
sobrevive al cambio de escena y al "enter play mode" sin domain reload. Si agregás estado `static`,
sumalo a ese reset o vas a arrastrar basura entre partidas.

## Modelo de datos

```csharp
[CreateAssetMenu]                     // Assets/Scripts/Zombi/Enemy.cs
class Enemy : ScriptableObject {
    public int hp;                    // vida
    public int daño;                  // lo que le saca al jugador por colisión
    public int velocidad;
    public int puntos;                // cuánto suma MATARLO
}
```

Los cinco assets viven en `Assets/Zombies/`. Balance actual:

| zombi | hp | daño | velocidad | puntos | balas para matarlo |
|---|---|---|---|---|---|
| ZombiNormal | 5 | 1 | 5 | 1 | 1 |
| ZombiRapido | 3 | 2 | 9 | 2 | 1 |
| ZombiFASTER | 3 | 1 | 12 | 5 | 1 |
| ZombiTanque | 25 | 1 | 3 | 20 | 5 |
| ZombiBOSS | 500 | 10 | 2 | 100 | 100 |

**Tocar el balance no requiere recompilar**: son valores de los `.asset`. Ojo que en puntos por bala el
BOSS es hoy el peor negocio del juego (100 puntos por 100 balas); está así a propósito hasta que se
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
2. `GunController.Update` cuenta `tiempoDisparo` y por cada disparo pide una bala al pool:
   `BulletController.Obtener(bala, firePoint.position, firePoint.rotation)`.
3. `BulletController.Update` se mueve con `transform.Translate` y descuenta `lifeTime`.
4. Al vencer el tiempo o al chocar, la bala **se apaga y vuelve al pool**, no se destruye.

**Nunca hagas `Instantiate`/`Destroy` de balas directo.** Con `tiempoDisparo = 0.04` son 25 balas por
segundo, y el power-up de arma lo baja a 0.01 (100 por segundo). El pool convierte 500 disparos en
49 objetos.

**La cadencia mejorada es temporal.** Los pickups no tocan `tiempoDisparo` directo: pasan por
`GunController.MejorarCadencia(valor)`, que aplica la mejora por `duracionMejora` segundos (10 por
defecto, editable en el inspector) y después vuelve a la cadencia con la que arrancó la escena. Un
pickup nuevo pisa al vigente y reinicia el reloj; la munición que dio el pickup no expira.

La bala **no tiene Rigidbody**, sólo un `BoxCollider`: los eventos de colisión llegan porque el zombi
sí tiene Rigidbody. Por eso el pool no necesita resetear velocidades.

## Generación de enemigos

Los dos generadores respetan el mismo techo, `maxZombisVivos` (60 por defecto, editable en el inspector),
consultando `EnemyController.ZombisVivos`:

- **`GeneradorZombis`** (free mode) — cinco corrutinas paralelas, una por tipo, cada una con un `while`
  infinito y su `WaitForSeconds`. Si se llegó al techo, saltea el spawn y sigue esperando.
- **`WaveManager`** (wave mode) — **una sola** corrutina que corre toda la partida. Elige el punto de
  spawn al azar entre `spawnPoints`. Si se llegó al techo, la oleada espera.

Sin el techo son ~350 zombis en el primer minuto y sigue creciendo lineal.

**El `WaveManager` no espera a que mates la oleada anterior**: las oleadas son por tiempo. El comentario
original decía lo contrario y el código nunca lo hizo. Cambiarlo es decisión de diseño.

## Persistencia

Todo por `PlayerPrefs`, con tres claves:

| clave | quién escribe | quién lee |
|---|---|---|
| `"Score"` | `PlayerHealth` al morir | `Score` (pantalla de derrota) |
| `"HighScore"` | `PlayerHealth`, si superás el récord | `highscoretext` |
| `"UltimoModo"` | `PlayerHealth`, el buildIndex de la escena | `MenuPerdiste.Retry` |

`PlayerHealth.TakeDamage` llama a `PlayerPrefs.Save()` explícitamente. Si agregás una clave, escribila
en ese mismo bloque o se pierde cuando el juego no cierra bien.

`"UltimoModo"` es lo que hace que "Retry" vuelva al modo que estabas jugando y no siempre al primero.
La tecla R hace lo mismo por otro camino: recarga la escena activa.

## Móvil

El puerto a Android está a medias pero **el código compila para las dos plataformas**, y eso es
deliberado: no hay ningún `#if UNITY_ANDROID` en el código del juego. Quien decide es
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
  (`BotonGranada`, sólo Android), cuyo `onClick` llama a `PlayerController.ThrowGranade` — público
  justamente por eso, y con el cooldown adentro, así que el botón no puede spamear.

**No vuelvas a meter un `#if UNITY_ANDROID` alrededor de una clase entera.** Ver la trampa de abajo.

### Build de Android

**Build > Android APK** (menú de `Assets/Editor/ConstructorAndroid.cs`) compila las escenas
habilitadas a `Builds/ShowBies.apk` en la raíz del repo (gitignoreada) y escribe el veredicto en
`Builds/build_result.txt`. También sirve por CLI con `-executeMethod ConstructorAndroid.BuildApk`.

- Configuración: package `com.ivru.showbies` (cambiable hasta publicar, después queda fijo),
  IL2CPP + ARM64, minSdk 25, targetSdk automático.
- El `user.keystore` de la raíz está configurado pero **apagado** (`useCustomKeystore = 0`): las
  builds de prueba firman con el debug keystore sin pedir nada. Para Play Store se reactiva con su
  contraseña, o mejor, Play App Signing.
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

- **Los `static` cruzan escenas.** `ZombisVivos`, `jugadorCache`, el pool de balas y el caché de sprites
  de sangre sobreviven al `LoadScene`. Están todos en el reset de `SubsystemRegistration`. Si te olvidás,
  el síntoma típico es un contador que queda alto y deja al generador tapado para siempre.

- **Una corrutina no sobrevive a la muerte de su GameObject.** `EnemyController` arrancaba una corrutina
  sobre el zombi para borrar la mancha de sangre y en la línea siguiente destruía al zombi: la corrutina
  nunca llegaba al `WaitForSeconds` y las manchas quedaban en la escena para siempre. Para limpiar algo
  después de destruir al que lo pidió, usá `Destroy(obj, segundos)`, que lo maneja el engine.

- **Las partículas hijas con `stopAction = Destroy` se cortan si destruís al padre.** La explosión de la
  granada es hija del prefab. Hay que despegarla (`SetParent(null)`) antes de destruir la granada, y
  entonces se limpia sola.

- **Los índices de escena están hardcodeados.** Ver la tabla de arriba.

- **Un power-up que no agarrás no se destruye nunca.** `PowerUp` spawnea cada 8 s (balas y vida) y cada
  20 s (arma), sin límite ni caducidad.

- **Todavía no hay pooling de zombis, manchas ni partículas.** Están un orden de magnitud por debajo de
  las balas, pero siguen siendo `Instantiate`/`Destroy`.

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

- **La muerte necesita guarda.** `Destroy` es diferido: dos golpes letales en el mismo paso de física
  llaman a `DanoZombi` (o `TakeDamage`) dos veces con la vida ya en cero, y sin el flag `estaMuerto`
  el bloque de muerte corre entero de nuevo — puntos dobles, dos manchas. Si agregás otra fuente de
  daño, no repitas la lógica de muerte: llamá a esos métodos, que ya están guardados.

- **Hay un kill-Z en Y = -20** para zombis y jugador, y no es decorativo: el mapa tiene bordes por los
  que la física empuja cosas, y un zombi caído seguía contando en `ZombisVivos` — cada caído era un
  cupo del techo de población perdido para siempre. Si agregás entidades con Rigidbody que importen,
  dales su propio kill-Z.

- **Player (capa 6) y Bala (capa 7) no colisionan, y eso está en la matriz del proyecto.** Antes se
  seteaba con `Physics.IgnoreLayerCollision(6, 7)` en el `Start` de cada bala. No lo hagas por código.

- **`pop.mp3` no lo usa nadie** y los power-ups se agarran en silencio. Igual el `AudioSource` con
  `pedo` que cuelga del Jugador: quedó sin disparador cuando se limpió `PlayerHealth`.

- **No hay música en las escenas de juego.** El único tema del proyecto es `MainMenu.mp3` y sólo suena
  en el menú.

## Para agregar una mecánica nueva

1. ¿Es un tipo de enemigo? Creá un `Enemy` nuevo en `Assets/Zombies/` (**con `puntos` cargado**, si no
   vale 1), un prefab con `EnemyController`, y sumalo al generador. No hace falta tag ni tocar código.
2. ¿Spawnea objetos seguido? Pooleá desde el principio: mirá `BulletController.Obtener` / `Devolver`.
3. ¿Necesita estado global? `static` + reset en `SubsystemRegistration`.
4. ¿Suma puntos? Que salga de `enemyType.puntos` en `DanoZombi`, no de donde se produce el daño.
5. ¿Guarda algo? `PlayerPrefs` en el bloque de `PlayerHealth.TakeDamage`, que ya llama a `Save()`.
6. ¿Tiene UI? Los cuatro canvas usan `ScaleWithScreenSize`. Las escenas de juego tienen la referencia
   en 1080x1920 (vertical, herencia de móvil): parece un error pero con `match = 0.5` la escala sale de
   la raíz del producto ancho × alto, así que da lo mismo que 1920x1080.
7. ¿Toca una escena o un prefab? Verificá el diff: Unity re-hornea bastante al guardar, y desde Unity 6
   agrega un `SceneRoots` (`--- !u!1660057539`) a cada escena. Lo que hay que confirmar es que no
   desaparezca ningún objeto, comparando los `--- !u!` contra HEAD.
8. Archivo nuevo en `Assets/Scripts/<Subsistema>/`.
