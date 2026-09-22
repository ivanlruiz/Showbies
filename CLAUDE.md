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
  y con más tipos mezclados, y un jefe cada 10. Mismo mapa que el libre (las calles del centro, `Ciudad`, se sacaron).

**El modo libre se desbloquea** al llegar a la oleada 12 del modo oleadas (`ModoLibre`, ver Menú y modos).

Es un **incremental**: las monedas que se juntan en las partidas se gastan en la **tienda de mejoras** del menú
(daño de bala, cadencia, críticos, vida máxima, granada, imán, botín y la furia), y las mejoras se aplican al empezar cada partida. Del otro lado,
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
Assets/Scripts/Zombi/       ← EnemyController, Enemy (ScriptableObject), GeneradorZombis, WaveManager, BarraDeVida, Escalado, ManchaDeSangre, JefePatrones, IMovimientoPropio
Assets/Scripts/Camara/      ← CamaraJugador
Assets/Scripts/UI/          ← ConditionalShow, Score, highscoretext, ContadorFps, IndicadorMejoraCadencia, IndicadorRecargaGranada, JoystickGranada, MenuPausa, BotonAtrasMenu, ContadorMonedas, TextoMonedasPartida, FormatoNumeros, ContadorCombo, VinetaDanio, AparecerConRebote, BotonJugoso, CurvasUI, TexturasUI, MedidorBalance, BotonFuria, ConfirmarSalir, CursorMira, BotonModoLibre, BotonOleadas, FondoMenu, MonedasDelFondo, TituloEnLaNiebla, IconoDeBoton, OpcionesSonido, SliderVolumen, VolumenEnPausa, VentanaRecompensaDiaria, VentanaMisiones, AvisoDeMisiones, VentanaBestiario, BarraDelJefe, ConstructorUI, Tema, PintarConTema, Interruptor
Assets/Scripts/PowerUps/    ← PowerUp (el spawner), PickupCaducidad, Moneda (las que sueltan los zombis)
Assets/Scripts/Progreso/    ← Progreso (monedas, mejor oleada y niveles, en un JSON), Mejora, CatalogoMejoras, AplicarMejoras, ModoLibre, RecompensaDiaria, RelojConfiable, MisionesDiarias, DesafioSemanal, Bestiario, Economia
Assets/Scripts/Tienda/      ← TiendaMejoras, TarjetaMejora, BotonMejoras, EfectosUI, GuiaPrimeraCompra
Assets/Scripts/Resena/      ← PedidoDeResena (la reseña de Google Play)
Assets/Scripts/Anuncios/    ← ServicioAnuncios, ConfigAnuncios, IProveedorAnuncios, ProveedorFalso, ProveedorNulo, LugarAnuncio, OfertaDeDuplicar, VigiaAplicacion, OfertaDeRevivir
Assets/Scripts/Jugo/        ← Efectos (golpes, muertes, explosiones, música), Sonidos, NumeroFlotante, FiltroBlancoYNegro, Volumen, FuenteConVolumen
Assets/Scripts/Escenario/   ← CapitulosDeEscenario (los capítulos de las oleadas: pradera de día y cementerio de noche)
Assets/Scripts/Tutorial/    ← TutorialManager, PrimeraVez, GuiaPrimeraPartida
Assets/Scripts/Idioma/      ← Idioma, Textos, TextoTraducido, SelectorIdioma
Assets/Scripts/*.cs         ← CanvasHelper, ConfiguracionRendimiento, MainMenu, MenuPerdiste, Plataforma, Puntaje, RestartScene
Assets/Escenas/             ← Menu, ShowBies1, Perdiste, WaveMode, Tutorial (+ Scenes/SampleScene, sin usar)
Assets/Prefabs/             ← Bullet, Gun, Granada, Moneda, power-ups, Jugo/ (Efectos, NumeroFlotante), Particulas/ (BrilloMoneda, Chispas), Personajes/, UI/ (MenuPausa, Tienda, TarjetaMejora, BotonFuria)
Assets/Zombies/*.asset      ← los cinco Enemy: stats POR TIPO, editables sin recompilar
Assets/Mejoras/             ← las ocho Mejora (.asset) y Resources/CatalogoMejoras
Assets/Anuncios/            ← Resources/ConfigAnuncios: los numeros de los videos con recompensa
Assets/Idioma/              ← Resources/Textos.txt: todos los textos del juego, en ingles y espaniol
Assets/otros/               ← los audios: MainMenu.mp3, shot.mp3, pop.mp3 (cajas), pedo.mp3 y los sintetizados provisorios (moneda, golpe, muerte, explosion, danio, cartel y musica, en .wav)
Assets/Animaciones/         ← Zombi.controller: el Animator Controller de los cinco zombis (correr, atacar, morir)
Assets/Editor/              ← ConstructorEscenarios (arma el prefab del cementerio), ConstructorAnimaciones (arma el controller de los zombis), ConstructorAndroid (builds de Android), PruebasMejoras, PruebaGolpeAnimado y PruebaMuerteAnimada (bancos en play), GrabarAnimaciones, HerramientasProgreso, ControlesEnElEditor e IdiomaEnElEditor (menú ShowBies)
Assets/Shaders/             ← Destello (el golpe al zombi), BlancoYNegro (el revivir), LogoEnLaNiebla (el titulo del menú)
Assets/Sprites/UI/          ← los dibujos de la interfaz, y LogoShowBies.png, que lo genera Marketing/logo.py
```

Y **fuera del proyecto de Unity**, en la raíz del repo:

```
Marketing/                  ← logo.py: el logo del juego dibujado en código, y su README
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

**Los índices están hardcodeados en el código** (`MainMenu.PlayGame` y `BotonModoLibre.Jugar` → 1, `MainMenu.GameModes` → 3,
`MainMenu.Tutorial` → 4, `PlayerHealth` → 2, `MenuPerdiste.Menu` → 0, `TutorialManager.IrAJugar` → 1,
`TiendaMejoras.Jugar` → 1 o 3 y `TiendaMejoras.AbrirEnMenu` → 0, estos dos con las constantes `EscenaMenu`,
`EscenaModoLibre` y `EscenaOleadas`).
Reordenar Build Settings rompe la navegación en silencio.

### Menú y modos

**PLAY abre el panel de modos** (`GameModesMenu`: TUTORIAL, MODO LIBRE, OLEADAS y VOLVER); ya no hay botón GAME MODES.
La excepción es la primera vez (ver Primera vez): `MainMenu.TocarJugar` manda derecho a la oleada 1.
**PLAY está abajo a la derecha, cerca del pulgar** (pedido de Ivan), y en el panel de modos OLEADAS ocupa ese mismo lugar,
con el libre y el tutorial encima y VOLVER abajo a la izquierda. El menú no muestra monedas: solo la tienda. El modo
libre está **bloqueado hasta llegar a la oleada 12** (`ModoLibre.OleadaParaDesbloquear`; llegar a la 12 es haber
completado la 11, que es lo que guarda `Progreso.MejorOleada`). Bloqueado, `BotonModoLibre` lo pinta gris con
"REACH WAVE 12" abajo y tocarlo lo hace temblar. **Todo lo que carga el libre pasa por `ModoLibre.EscenaPara`**, que
manda a las oleadas si todavía no está: `MainMenu.PlayGame`, el final del tutorial, el ¡A JUGAR! de la tienda y el
OTRA VEZ de la derrota. Si agregás otro camino al libre, pasalo por ahí.

**`MainMenu` y `GameModesMenu` están estirados al canvas** (anclas 0,0 a 1,1). Antes eran rectángulos fijos de 1920x1080
centrados, y como el canvas escala por ancho, en un teléfono 20:9 (2400x1080, el más común) mide 864 de alto: PLAY,
OLEADAS y VOLVER quedaban cortados por abajo. Los botones de las esquinas tienen que colgar de algo que siga el borde
real de la pantalla.

**SALIR pregunta antes de cerrar** (pedido de Ivan: está en el medio del menú y un toque sin querer cerraba el juego).
`ConfirmarSalir` (raíz del canvas "Main Menu") copia al arrancar la ventana del idioma, como `OpcionesSonido`, más ancha
para tapar los botones del menú, y la arma como "¿SALIR DEL JUEGO?" con SEGUIR JUGANDO (verde, late) y SALIR (vidrio).
La abren `MainMenu.QuitGame` y el atrás de Android en el principal; el atrás con la ventana abierta la cierra. Si no se
pudo armar, las dos cosas cierran el juego como antes.

### Primera vez

Lo que ve alguien que recien instala (ronda de ideas del 19/9: el panel de modos, el tutorial opcional y una tienda de
ocho tarjetas sin guía eran demasiado). Lo decide `PrimeraVez` (`Assets/Scripts/Tutorial/`) mirando el progreso, sin
marcas aparte: `NuncaJugo` (ninguna partida terminada, ninguna oleada completada ni a medias), `NoTerminoPartidas` y
`NuncaCompro`.

- **PLAY va derecho a la oleada 1** (`MainMenu.TocarJugar`, que es el `onClick` del botón): después abre el panel de modos.
- **La primera partida trae una guía** (`GuiaPrimeraPartida`, objeto propio en WaveMode, armada en código): en el
  teléfono un pulgar fantasma sobre cada joystick con su cartel, que se va al usarlo; en PC un cartel con WASD y el clic.
  Con el primer zombi muerto, "¡COGE LAS MONEDAS!" hasta agarrar una. No frena nada. Sigue si se sale y se retoma
  (mira `NoTerminoPartidas`, porque `WaveManager` guarda la oleada en curso apenas empieza).
- **La primera compra está señalada** (`GuiaPrimeraCompra`, en la raíz del prefab `Tienda`): si nunca compró y le alcanza
  para el daño, la lista se desplaza hasta esa tarjeta y una flecha dorada la señala desde abajo; al comprar, la flecha
  pasa a ¡A JUGAR!. Es el triángulo de `TexturasUI.Play` girado.
- **La recompensa diaria espera a la primera partida terminada**: si no, se cobran 150 monedas y se compra antes de jugar.

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

**La granada se compra** (la mejora `granada`, 250 monedas, tope 1). Sin comprarla `PlayerController.GranadaDesbloqueada`
es falso (lo fija `AplicarMejoras`): `GranadaLista` da falso, Espacio no apunta y el botón G se esconde con un
`CanvasGroup` (`JoystickGranada.LateUpdate`, no apagando el objeto). El tutorial la prende en su `Start`, porque la
enseña. Una escena sin `AplicarMejoras` la tiene siempre.

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
- **Explota** al pasar a `radioDeContacto` de un zombi en el aire (recién después de alejarse `distanciaSegura`,
  1,5 m, del jugador: con un zombi pegado explotaba a los pies), o `demoraAlCaer` (0,3 s) después de caer. Una
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
- **Jugo** (`Efectos.EmpezarFuria`): cartel "¡FURIA!" con rebote, chispas, temblor, una pausa de impacto corta y el
  borde rojo latiendo mientras dura. El botón respira cuando está lista, vibra mientras dura y
  cuenta los segundos del enfriamiento.
- `GunController.DanoPorBala` sigue siendo el de la mejora (lo miran el medidor y las pruebas); lo que lleva cada bala
  es `DanoPorTiro`.
- **Es un desbloqueo permanente:** cuando entre el renacer de la fase 5, no tiene que reiniciarla.

## Generación de enemigos

Los dos generadores respetan el mismo techo, `maxZombisVivos` (60 por defecto, editable en el inspector),
consultando `EnemyController.ZombisVivos`:

- **`GeneradorZombis`** (free mode) — cinco corrutinas paralelas, una por tipo, cada una con un `while`
  infinito y su `WaitForSeconds`, en un punto al azar a más de `distanciaMinimaAlJugador` (8 m) del jugador. Si se
  llegó al techo, saltea el spawn y sigue esperando. Al subir de nivel guarda el progreso. **Escala con el
  tiempo:** `NivelActual` sube uno cada `segundosPorNivel` (45 s de tiempo escalado, así la pausa lo congela) y
  cada zombi aparece con vida × `crecimientoVida`^(nivel−1), daño × `crecimientoDano`^(nivel−1) y monedas ×
  `crecimientoMonedas`^(nivel−1) (1,15, 1,07 y 1,05: los que tenían las oleadas antes del parche del 19/9). `textoNivel` muestra "Nivel N" en el
  HUD y, al subir, rebota y suena el jingle del cartel. Sin esto el modo libre era una granja de monedas.
- **`WaveManager`** (wave mode) — **una sola** corrutina que corre toda la partida. Cada oleada:
  1. Muestra el cartel "Oleada N" (`cartelOleada`) durante `descansoEntreOleadas` (3 s). El HUD
     (`textoOleada`) muestra "Oleada N" y abajo "Zombis muertos/total" de esa oleada, jefe incluido; se
     actualiza en `Update` sólo cuando cambia, y los caídos por el kill-Z cuentan como muertos para que
     llegue al total justo cuando la oleada termina.
  2. Si la oleada es múltiplo de `jefeCadaOleadas` (10), saca un `jefe`.
  3. Saca `zombisBase + zombisPorOleada × oleada` zombis (10 + 4n: 14 en la 1, 50 en la 10; pedido de Ivan antes de
     la prueba cerrada, antes eran 6 + 2n), de a uno cada `intervaloEntreApariciones` (0,35 s, antes 0,8 s), en un punto al azar de `spawnPoints` a más de `distanciaMinimaAlJugador` (8 m) del jugador (si todos
     están cerca, el más lejano: antes nacían encima y pegaban en el acto). El tipo sale por sorteo
     entre los `tipos` ya habilitados (`desdeOleada`), con `peso` relativo: normal desde la 1, rápido desde
     la 3, tanque desde la 6 y FASTER desde la 9. Si se llegó al techo, espera.
  4. **Termina cuando mueren todos los zombis que sacó**; los que caen por el kill-Z cuentan como muertos. Si el
     jugador murió en el mismo paso, espera a ver si revive: si no, la partida termina en esa oleada y no se
     completa ni se guarda la siguiente.

  **La partida de oleadas se retoma** (pedido de Ivan): al empezar cada oleada, `WaveManager` guarda en el progreso la
  oleada y los puntos (`Progreso.GuardarOleadaEnCurso`) y escribe el archivo. Si se sale al menú o se cierra la app,
  la próxima vez arranca esa oleada desde cero (todos sus zombis, vida llena) con esos puntos, y el botón OLEADAS
  del menú avisa "CONTINUE WAVE N" (`BotonOleadas`). **Se olvida al morir** (`PlayerHealth.Terminar`) **y al
  reiniciar** (REINICIAR de la pausa y la R, con `WaveManager.OlvidarPartidaSiEsOleadas`): las dos cosas empiezan una
  partida nueva. Salir en mitad de una oleada que se estaba perdiendo la reinicia sin morir: es a propósito.

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

**El jefe tiene patrones propios** (`JefePatrones`, en el prefab ZombiBOSS; pedido de Ivan: antes era un zombi grande y
lento). Alterna dos ataques con aviso cuando el jugador está a menos de `distanciaParaAtacar` (15 m): la **carga** (se
frena, marca en el piso una línea roja hacia el jugador 0,9 s, ruge y embiste en línea recta a 16 m/s, pegando ×2,5
mientras embiste, y al terminar —haya chocado o no— **queda aturdido 1,3 s**, tambaleándose y sin atacar: esa es la
ventana para castigarlo, y es lo que hace que esquivar valga la pena) y la **invocación**
(se frena, un anillo rojo que se achica y aparecen 4 zombis normales con sus multiplicadores). A la mitad de su vida
entra en furia: ataca más seguido e invoca 6. Los invocados cuentan en la oleada y en el total del HUD
(`WaveManager.SumarALaOleada`). Para moverse por su cuenta usa `IMovimientoPropio`: `EnemyController` lo busca en su
`Awake` y en cada paso de física le pregunta primero; si devuelve verdadero, la persecución de siempre no corre ese paso.
Las líneas usan el material del indicador de la granada y el rugido es `explosion.wav` más grave, y van **planas
sobre el piso** (`LineAlignment.TransformZ` con el objeto rotado −90° en X): con la alineación de siempre, que mira a
la cámara, la cinta quedaba parada y medio enterrada.

**Las invocaciones respetan el techo de población.** El generador de cada escena lo fija al empezar
(`EnemyController.FijarTecho`, 60 o 35 en móvil) y el jefe saca `min(los suyos, maxInvocadosVivos,
EnemyController.LugarParaZombis)`: sin eso, en el modo libre se juntaban jefes invocando y el teléfono se trababa.
En el libre, además, **no sale otro jefe mientras haya uno vivo** (`GeneradorZombis` lo sigue con su número de
aparición). `EnemyController.Jefes` es la lista de los que hay, que se lleva desde el setter de `EsJefe` (se marca
después de aparecer, así que `OnEnable` no sirve) y la mira la barra de arriba.

**Al revivir, el jefe posterga su ataque** (`JefePatrones.Postergar`, desde `PlayerHealth.Revivir`): el jefe no se
despeja, y volver justo cuando terminaba de avisar la carga es morir de nuevo sin llegar a jugar.

**Los zombis escalan con la oleada.** `WaveManager.Aparecer` pone `multiplicadorVida` = `crecimientoVida`^(o−1)
(1,11) y `multiplicadorDano` = `crecimientoDano`^(o−1) (1,07) apenas sale el zombi, antes de su primer golpe, jefe
incluido. La vida
crece más rápido que el daño a propósito: lo que frena es no llegar a matarlos, no que dos golpes liquiden al
jugador. **La vida era 1,15 y las monedas 1,05** hasta el 19/9: una simulación mostró un muro en la oleada 35 (el daño
comprable crece con el logaritmo de las monedas y la vida, exponencial), y con 1,11 y 1,08 (Ivan lo probó en la 35 y
lo notó bien) se corre a la 45-48. La solución de fondo (hitos de daño multiplicativos, renacer) está en TAREAS. `EnemyController` inicializa la vida perezosa (en el primer golpe o al consultarla, una vez por aparición) y muere con vida ≤ 0,01;
el daño al jugador acumula las fracciones (`PlayerHealth.AcumularDano`) y resta enteros.

Sin el techo son ~350 zombis en el primer minuto y sigue creciendo lineal.

Antes las oleadas eran por tiempo (salía la siguiente aunque quedaran zombis) y cada 5 oleadas el tipo de
zombi se reemplazaba en vez de sumarse: desde la oleada 20 sólo salían jefes.

### Las animaciones de los zombis

Los cinco usan el mismo esqueleto (el modelo de ToonyTinyPeople, ver Rendimiento en móvil) y el mismo Animator
Controller, `Assets/Animaciones/Zombi.controller`, que **no se edita a mano**: lo arma **ShowBies > Animaciones >
Armar el controller de los zombis** (`ConstructorAnimaciones`), que se vuelve a correr para cambiarlo. Tiene tres
estados —**Correr** (`Z_run_rm`, en loop, el de entrada), **Atacar** (`Z_attack_A`) y **Morir** (`Z_death_A`)— y tres
parámetros: el float `Paso` y los gatillos `Atacar` y `Morir`. Los nombres los comparte `EnemyController`, como hashes.

Hasta el 22/9 el controller tenía **un solo estado** y un parámetro que no usaba nadie: los cinco zombis corrían para
siempre, te pegaban corriendo y se morían corriendo, aunque `Z_attack_A` y `Z_death_A` estaban en el proyecto desde el
principio, sin usar. El controller además vivía en la carpeta del pack; se movió con `AssetDatabase.MoveAsset`, que
conserva el guid, así que los cinco prefabs (que lo pisan con un override de `m_Controller`) siguieron apuntando solos.

- **El ritmo del paso es el parámetro `Paso`** y ya no `animador.speed`: es el multiplicador del estado de correr, y
  atacar y morir van siempre a 1. Con el Animator entero, el jefe (`velocidadDeAnimacion` 0,3) tardaba cuatro segundos
  y medio en morirse y el tanque (0,45) pegaba en cámara lenta.
- **Pegar** lo dispara `EnemyController.Golpear`, donde ya estaba el daño, así que cubre a los cinco y al jefe. El clip
  dura 1,33 s y `intervaloDeGolpe` es 0,8: pegado al jugador el zombi encadena golpes sin volver a correr, porque la
  transición desde AnyState se reinicia a sí misma.
- **Morir dejó de ser "apagar el objeto".** `EnemyController.Morir` saca al zombi de la cuenta en el acto
  (`DejarDeContar`: `ZombisVivos--`, deja de ser jefe, se esconde la barra) y **deja el GameObject prendido
  `duracionDeLaMuerte` (1,4 s)** mientras se desploma, con los colliders apagados y el Rigidbody kinematic; recién
  entonces llama a `Devolver`. El estado Morir va a velocidad 1,35 para que los 1,83 s del clip entren en esa ventana.
  **Para todo el resto del juego el cadáver ya no existe**: `Vivo` (que es `enUso`) da falso, la oleada lo cuenta
  muerto, las balas lo atraviesan y su lugar en el techo de población queda libre.
- **Hay techo de cadáveres** (`MaxCadaveres` 12, `MaxCadaveresMovil` 5): un cadáver es una malla con huesos
  animándose y cuesta lo mismo que un zombi vivo, así que una granada que mata a diez dejaría diez animándose encima de
  los que siguen saliendo. Pasado el techo el zombi se va de golpe, como antes.
- **Un jefe muerto no ataca**: `JefePatrones.Update` corre sobre el cadáver hasta que se va, y sin la guarda de `Vivo`
  seguía dibujando la línea de la carga e invocando un segundo y medio después de que la barra llegó a cero.
- **El kill-Z y el despeje del revivir no animan nada**: llaman a `Devolver` directo. El despeje saca a los zombis
  "sin puntos, monedas ni mancha", y una muerte en cámara ahí sería justo lo contrario.
- **Dos bancos en play lo verifican**, porque esto toca lo más fácil de romper en silencio del juego:
  **ShowBies > Pruebas > Golpe animado** mide en qué estado está el Animator en el medio segundo posterior a cada golpe
  (99 % en Atacar), y **Muerte animada** mata zombis por el mismo camino que una bala y comprueba que `ZombisVivos` no
  se despegue ni un solo frame de los que de verdad están vivos, que los cadáveres se vayan solos y que la oleada siga
  terminando y avanzando. Escriben `Builds/prueba_golpe.txt` y `Builds/prueba_muerte.txt`.

**Barras de vida.** `EnemyController` crea una `BarraDeVida` con el primer golpe que no mata, así que los
zombis que mueren de un tiro nunca la muestran. Es un objeto aparte que sigue al zombi y mira a la cámara, no
un hijo: los zombis rotan hacia el jugador y tienen escalas distintas, y una barra hija heredaría las dos
cosas. Son dos `SpriteRenderer` sobre un sprite blanco hecho en código (estático, con su reset), no un Canvas
por zombi. La fracción sale de la vida con la que apareció el zombi (`vidaMaxima`), así que un escalado de vida
por oleada no la rompe mientras se aplique antes del primer golpe. Cuando el zombi muere la barra se apaga y queda
guardada con él: la aparición siguiente la prende con su primer golpe que no mata, sin crear otra (el pool es por
prefab, así que la altura sirve). Se destruye con el zombi.

### Capítulos: la pradera, el cementerio y la ciudad

Pedido de Ivan: escenarios que cambien. Las oleadas van por **capítulos de 10** (`CapitulosDeEscenario`, objeto
`Capitulos` de WaveMode), que recorren la lista `escenarios` y vuelven a empezar: 1-10 la **pradera de día**, 11-20 el
**cementerio de noche**, 21-30 la **ciudad de noche**, 31-40 otra vez la pradera. El modo libre queda de día.

Cada escenario es un `EscenarioDeCapitulo`: el id de su nombre en la tabla, su decorado, su piso, el cielo, la luz (color,
intensidad y ángulo), la luz ambiente y la niebla. **El primero de la lista es lo que trae la escena**: sus colores y su
piso se leen en el `Start` en vez de cargarse a mano, así el capítulo 1 se ve igual que siempre. Para sumar un escenario
nuevo alcanza con un elemento más en el array, su prefab y su fila en la tabla de textos.

Al pasar de capítulo, en el descanso de la oleada: el cartel "CAPÍTULO 3 / LA CIUDAD" arriba de todo (al medio está el de
la oleada) con el jingle; el cielo, la luz, la luz ambiente y la niebla se funden de un escenario al otro en 2,5 s; a
mitad del fundido —que es lo más oscuro— cambia el piso, se va el decorado viejo y sale el nuevo. El que no tiene niebla
la manda lejísimos, así entrar o salir de la noche se ve como que se cierra o se abre, y no como un corte. Una partida
retomada en la 25 arranca directamente en la ciudad, sin fundido. Al descargarse la escena la niebla se apaga.

**Los decorados son prefabs hechos con formas simples**, sin colliders (los zombis van derecho al jugador y se
trabarían), y **cada uno se arma una sola vez por partida**: después se prende y se apaga. Cuando termina de salir se
junta con `StaticBatchingUtility` en pocos draw calls, y desde ahí las piezas ya no se mueven por separado, así que **la
primera vez sale cada pieza sola del piso y las siguientes sale el decorado entero**.

- `Prefabs/Escenarios/Cementerio`: lápidas, cruces, árboles pelados, la reja del borde y cuatro faroles con luz cálida,
  sobre tierra (`PisoCementerio.mat`, la textura Brown Stony repetida 45 veces).
- `Prefabs/Escenarios/Ciudad`: una cuadrícula de manzanas de 14 m con vereda y cordón, **corrida media manzana para que
  el cruce quede en el centro** (el jugador arranca en la calle: parado sobre una vereda lisa no se entendía que fuera
  una ciudad), las líneas blancas del medio de cada calle, edificios bajos sólo a más de 30 m (desde arriba, uno cerca
  taparía la partida), autos contra el cordón, contenedores, canteros y faroles con luz naranja en las cuatro esquinas
  del cruce. El piso es `PisoCiudad.mat` (Grey Stones repetida 70 veces).

El decorado del capítulo que viene **se arma apagado unos segundos después de entrar al anterior**
(`PrepararElSiguiente`): instanciar ochocientos objetos en el frame del cambio era un tirón justo en el momento del
evento, con el cartel y el fundido.

Ninguno se edita a mano: los arman **ShowBies > Escenarios > Armar cementerio** y **Armar ciudad**
(`ConstructorEscenarios`, con semilla fija), que se vuelven a correr para cambiarlos. La niebla funciona en la build
porque el menú la tiene guardada en su escena (ver El fondo del menú vivo).

## Monedas y progreso

Lo que el jugador conserva entre partidas vive en `Progreso` (`Assets/Scripts/Progreso/`): un JSON en
`Application.persistentDataPath/progreso.json` con las monedas, la mejor oleada completada, el nivel de cada
mejora y lo que necesitan los anuncios (versión 5: `mejoras` es una lista `{id, nivel}` porque `JsonUtility` no
guarda diccionarios, desde la 3 se suman `partidasTerminadas`, `segundosJugados`, `ofrecerVideos` y los topes
del día, desde la 4 los contadores de por vida y desde la 5 el desafío de la semana). No usa PlayerPrefs a propósito: es estado estructurado.

- **Los zombis sueltan monedas y se cobran al agarrarlas.** Al morir, `DanoZombi` (en el mismo bloque que
  suma los puntos) suelta entre `monedasMin` y `monedasMax` monedas (`Moneda`, en `Assets/Prefabs/Moneda.prefab`)
  que valen `multiplicadorMonedas` cada una. Salen volando para los costados, caen despacio con un rebote y
  quedan girando en el piso; al acercarse el jugador vuelan solas hacia él (sin la mejora de imán hay que pasar a
  `distanciaDeCobro`, 0,8 m, que es pasarles por encima; con ella, al alcance del imán) y recién ahí se
  suman a `Progreso`, con un brillo y una nota de la escala de la bemol mayor: **la escalera** (`Moneda.GradoSiguiente`),
  las agarradas seguidas (menos de `ventanaEscalera`, 0,45 s, entre una y otra) suben grado por grado dos octavas y
  siguen dando vueltas por la de arriba; cada octava completa brilla el triple. Si se corta, vuelve a la bemol. Una
  moneda cuyo sonido no pasa el techo de 50 ms no sube la escalera. (Antes cada una sorteaba una nota.) Las que nadie
  agarra desaparecen a los 20 s, parpadeando los últimos 3.
- **El único cobro directo es el bono de la oleada** (`WaveManager`, `bonoPorOleada × oleada`, que se anuncia
  en el cartel de la oleada siguiente). `bonoPorOleada` vale 4 en WaveMode, el doble del plan, porque las monedas
  que sueltan los zombis ya son ≈2× las que simuló; el botín no lo multiplica. **Al terminar la oleada las monedas
  se quedan donde cayeron**: no hay imán global (antes `Moneda.AtraerTodas` las traía todas), juntarlas es parte del
  juego y la mejora de imán es la que ayuda. Siguen desapareciendo a los 20 s.
- **`multiplicadorMonedas` y `monedaPrefab` los pone quien hace aparecer al zombi.** `WaveManager` usa
  `crecimientoMonedas^(oleada − 1)` (1,08) y `GeneradorZombis` 0,5 × el crecimiento de su nivel (el modo libre da
  la mitad y no tiene bono), los dos multiplicados por el botín de la mejora: con un multiplicador menor a 1 cada moneda sale con esa probabilidad y vale 1, porque una moneda de
  0,5 no mueve el contador al agarrarla. Un zombi sin `monedaPrefab`, como los del tutorial, no suelta nada.
- **Las monedas no tienen Rigidbody ni collider**, no proyectan ni reciben sombra (con el material instanciado,
  para que no sean un draw call cada una) y salen de un pool, con un techo de 150 en escena (80 en
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
- **Se suman en memoria al agarrarlas y se guardan en disco en puntos seguros:** al completar cada oleada (y al subir de nivel en el libre), al
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
- **Los contadores de por vida** (`estadisticas`, v4): zombis matados por tipo (con el nombre del asset `Enemy`,
  que por eso no se renombra), jefes, granadas, furias, críticos y `monedasGanadasJugando`, que suma solo lo que
  entra por `Sumar` (los premios no). Todavía no los muestra nada: son la base de misiones, logros y el renacer, y
  lo que no se cuenta se pierde. Se cuentan donde pasa cada cosa (`DanoZombi` en el bloque de muerte y al recibir un
  crítico, `ThrowGranade`, `Furia.Activar`), no suben `Revision` y se guardan en los mismos puntos que las monedas.
  La versión subió a 4 aunque migrar no pida nada, para que un build viejo abra el archivo en solo lectura y no los
  borre.
- `PlayerHealth` llama a `Progreso.TerminarPartida(segundos)` al morir, en el mismo bloque que guarda: cuenta
  la partida y el tiempo jugado, que es lo que mira la oferta de video para no premiar una partida de dos
  segundos.
- `MonedasDeLaPartida` vuelve a cero al empezar cada partida (`PlayerHealth.Awake`) y lo muestra la pantalla
  de derrota (`TextoMonedasPartida`). El HUD de las escenas de juego muestra el total con `ContadorMonedas`.
  Los números para pantalla pasan por `FormatoNumeros.Compacto`, que los escribe según el idioma (ver Idiomas).

## Recompensa diaria

Monedas por entrar una vez por día, que crecen con la racha (pedido de Ivan para que vuelvan cada día). La lógica es
`RecompensaDiaria` (`Assets/Scripts/Progreso/`) y la ventana, `VentanaRecompensaDiaria` (raíz del canvas "Main Menu"),
que se arma entera en código y aparece sola al abrir el menú si hoy hay algo para cobrar (desde la primera partida
terminada: ver Primera vez).

- **Racha:** sube si el último cobro fue ayer; si se saltó un día vuelve a 1. Paga 150, 250, 400, 600, 900, 1.300 y 2.000
  (`MonedasPorDia`), y del día 7 en adelante sigue pagando lo del 7 mientras no se corte.
- **Crece con la mejor oleada, con la vara de `Economia`**: paga las partidas que dice `PartidasPorDia` (de un cuarto
  de partida el primer día a dos el séptimo), y **los montos de arriba quedan como piso**. Así en el arranque —donde 150
  monedas son la primera mejora— paga lo mismo de siempre, y en la oleada 45 pasa de 825 monedas a unas 12.700 el primer
  día: con el +10 % lineal de antes quedaba en calderilla justo cuando más hace falta la razón para volver.
- **Paga con la mejor oleada del día, congelada** (`Progreso.OleadaDeLaRecompensa(hoy)`, campos
  `diaOleadaRecompensa` y `oleadaRecompensa`, que se anotan la primera vez que se pregunta ese día), igual que las
  misiones y el desafío semanal y por la misma razón: con la marca de ahora, dejar la ventana sin cobrar, jugar hasta
  mejorar la marca y recién ahí tocar COBRAR era la jugada óptima —en la prueba, 8.400 monedas en vez de 1.100— y
  encima pagaba la marca nueva por un día que ya venía corriendo. `RecompensaDiaria.OleadaDeHoy` es lo que lo lee, y
  el centinela del campo es −1 porque 0 es una marca válida.
- **Se guarda en el progreso** (`diaRecompensa` aaaammdd y `rachaRecompensa`, sin cambiar la versión) y entra por
  `CobrarPremio`: no son monedas ganadas jugando. **Atrasar el reloj no da otra**: sólo cuenta un día mayor al guardado
  (`Progreso.EsDiaNuevo`), como los topes de los anuncios. **Adelantarlo tampoco**, mientras no se reinicie el teléfono:
  `Progreso.DiaDeHoy` usa `RelojConfiable`, que al cobrar guarda una marca (hora, `SystemClock.elapsedRealtime` y el
  número de arranque) y, en el mismo arranque, si el reloj dice más de 2 h por encima del tiempo real, usa la hora de
  la marca más el tiempo real. Solo en Android; en PC vale el reloj.
- **El video va después de cobrar** (pedido de Ivan): se cobra con COBRAR y recién ahí, si `ServicioAnuncios.PuedeOfrecer`
  el lugar `regalo_x2`, la ventana ofrece VÍDEO: +N MÁS al lado de VOLVER (un botón de atrás, no un NO, GRACIAS). El video
  paga lo mismo otra vez (`RecompensaDiaria.CobrarDuplicado`, una sola vez por cobro y en memoria). Sin video, la ventana
  se va sola. Cerrar el video antes no castiga: vuelve la oferta si todavía se puede ofrecer.
- El atrás de Android la cierra sin cobrar (`BotonAtrasMenu`); vuelve a salir la próxima vez que se abre el menú ese día.
- **No se abre con la tienda abierta** (MEJORAS de la derrota carga el menú con la tienda encima, en otro canvas): se
  arma en `Start` y se abre en el primer `Update` con `TiendaMejoras.Abierta` en falso.
- Cada casillero se etiqueta con el mismo día de racha que usa su monto: desde el día 8 el casillero de hoy dice DÍA 8.
- Las pruebas cubren racha, corte, reloj atrasado, fin de mes y de año, bisiesto, montos y el cobro guardado.

## La vara: `Economia`

Los tres premios que no se ganan jugando —las misiones del día, las estrellas del bestiario y la recompensa diaria— se
miden contra **lo que deja jugar**, y esa cuenta vive en un solo lugar (`Economia`): `ZombisPorPartida` (los zombis que
se matan llegando a la oleada m, que son 10 + 4n por oleada) y `MonedasPorPartida` (esos zombis por las ~2 monedas que
suelta cada uno, con el multiplicador de la oleada a mitad de camino). Para los objetivos que no se cumplen matando
están además `SegundosPorPartida` (cuánto dura esa partida: un tercio de segundo largo por zombi más los 3 s de
descanso entre oleadas, ~1.400 s para una que llega a la 40) y `BalasPorPartida` (los zombis por lo que cuesta
matarlos, que **no** es constante: el daño comprable crece con el logaritmo de las monedas y la vida de los zombis,
exponencial, así que cada vez hacen falta más balas por zombi). Es **de menos a propósito**: no suma el bono de
cada oleada ni el botín, así un premio calculado con esto nunca se pasa de lo que da jugarlo. `Redondo` está ahí también,
para que todos los premios se lean igual (de a 5, de a 10 o de a 50).

Con montos fijos, un premio que servía en la oleada 5 regalaba en la 1 y era calderilla en la 40; hubo que arreglarlo
por separado en los tres sistemas antes de juntar la cuenta acá.

## Misiones del día

Tres por día (fácil, media y difícil, en verde, amarillo y rojo), pedido de Ivan para que cada partida tenga un objetivo.
La lógica es `MisionesDiarias` (`Assets/Scripts/Progreso/`), guardada en el progreso (`misiones`: el día, la mejor oleada
completada en el día y la lista).

- **Cambian a medianoche** con el día confiable (`Progreso.DiaDeHoy`), y el mismo día salen siempre las mismas: se sortean
  con el día de semilla (`Armar`). Un reloj atrasado no las cambia.
- **Tipos** (el nombre se guarda en el JSON: no se renombra): matar N zombis, completar N oleadas, ganar N monedas, usar
  la furia, tirar granadas y hacer críticos (estas tres solo si están compradas) y derrotar N jefes (solo la difícil, con
  la mejor oleada en 9). Tres tipos distintos por día. **Todos los objetivos se ajustan a la mejor oleada** (`Objetivo`,
  en números redondos), sin excepción: con la furia, las granadas y el jefe en números fijos, el premio —que sí escala—
  se cobraba tirando 25 granadas parado en un rincón, y en la oleada 45 eso pagaba más que una partida entera.
  **Y cada uno sale de lo que de verdad lo hace costar**, no del número de oleada: los que se cumplen matando, de
  `Economia.ZombisPorPartida`; los que se cumplen con el reloj (la furia cada 120 s, la granada cada 5), de
  `Economia.SegundosPorPartida`; y los críticos, de `Economia.BalasPorPartida` por la probabilidad que tenga comprada
  el jugador. Atados al número de oleada, cumplir la de granadas costaba 0,43 partidas con el premio de 2,5 (tirarlas
  al aire rendía más monedas por segundo que jugar bien desde la oleada ~10) y la de críticos, 0,26 —y además pedía
  200 críticos a quien recién los compraba y le salían 7 por partida—. **La prueba mide el costo de cada tipo, en
  partidas, contra `Partidas(dificultad)`**: verificar sólo que el objetivo crezca con la oleada dejaba pasar los dos,
  porque crecer crecían.
- **El avance sale de los contadores de por vida**: al armarlas se anota cuánto marcaba cada uno (`inicio`) y el avance es
  la diferencia. La de oleadas cuenta **cuántas se completaron hoy** (`oleadasDelDia`, que avisa `WaveManager` con
  `RegistrarOleada`), no a cuál se llegó: contando el número de la oleada, retomar una partida guardada en la 25
  cumplía de una las de "llega a la 15" y "llega a la 24".
- **Lo cumplido y sin cobrar no se pierde a medianoche**: al cambiar el día se cobra solo (`CerrarElDia`) antes de
  armar las nuevas, cofre incluido. No avisa en pantalla; las monedas aparecen en el contador.
- **Premio: una fracción de lo que dan las partidas que cuesta el objetivo** (0,4, 0,5 y 0,6 de 0,4, 1 y 2,5
  partidas), no un monto fijo. `MonedasPorPartida` estima lo que deja una partida que llega a la mejor oleada —los
  zombis que se matan por las ~2 monedas que suelta cada uno, con el multiplicador de la oleada a mitad de camino,
  sin el bono ni el botín— y es la misma cuenta con la que se arma el objetivo de "gana N monedas". Se cobra en el
  menú, por `CobrarPremio` (no cuenta como jugado). Eran 150, 300 y 600 fijos × (1 + 0,1 × oleada), y el primer día
  regalaban ~1.850 monedas contra ~300 de jugar: el jugador nuevo se saltaba la parte de arrancar flojo, que es el
  juego, y en las oleadas altas el premio no se notaba. Ahora el día entero paga ~2/3 de lo que da jugarlo, en toda
  la curva, y las pruebas lo miden en las oleadas 0, 3, 5, 10, 20 y 40.
- **El premio se congela con la mejor oleada del día en que se armaron** (`mejorOleadaAlArmar`, y `OleadaDeHoy` es lo
  que lo lee), no con la de ahora: el objetivo también quedó dimensionado con esa, y si no, guardar las misiones sin
  cobrar hasta mejorar la marca era la jugada óptima —y encima el cierre de medianoche las pagaba al precio más alto del
  día—. El centinela del campo es −1 y no 0, porque 0 es una marca válida (todavía no completó ninguna oleada).
- **El cofre del día**: con las tres cobradas se abre uno (`CobrarCofre`, `cofreCobrado` en el progreso, vuelve con las
  misiones nuevas) que paga la mitad de las tres juntas: es lo que se lleva quien vuelve a cerrar el día. En la ventana, al lado de VOLVER: gris con "COFRE 1/3"
  (tocarlo tiembla), dorado y latiendo cuando se puede abrir (cuenta en la insignia), y al tocarlo tiembla, estalla con
  el arpegio doble y queda verde. El ícono es `Sprites/UI/IconoCofre`, dibujado en blanco.
- **En el menú**: un botón redondo **arriba a la derecha** (pedido de Ivan), en espejo con el globo y el engranaje, con el
  portapapeles de Material (`Sprites/UI/IconoMisiones`) y una insignia contando las que hay para cobrar. Es una copia del
  globo hecha en código por `VentanaMisiones` (raíz del canvas "Main Menu"), que también arma la ventana: cada misión con
  su barra, el premio y COBRAR, y cuánto falta para las nuevas. El atrás de Android la cierra.
- **En la partida**: `AvisoDeMisiones` (objeto propio en ShowBies1 y WaveMode) muestra "¡MISIÓN CUMPLIDA!" con lo que pedía,
  un rebote y el jingle del cartel cuando se cumple una; las que ya estaban cumplidas al empezar no se repiten. Avisa
  también, en dorado, las estrellas del bestiario que se ganan jugando ("¡ESTRELLA! CAMINANTE x100"); si llegan dos a
  la vez, salen una después de otra.

## Desafío semanal

Uno solo, grande, que dura de lunes a domingo y paga bastante más que una misión del día (pedido de Ivan). Las
diarias dan una razón para entrar hoy; este da una para volver toda la semana, que es otra escala de tiempo. La
lógica es `DesafioSemanal` (`Assets/Scripts/Progreso/`), guardada en el progreso (`semanal`), y se ve en la fila
dorada de arriba de la ventana de misiones.

- **Cambia el lunes**, con el mismo día confiable que las misiones (`LunesDe(Progreso.DiaDeHoy())`), y el mismo
  lunes sale siempre el mismo: se sortea con el lunes de semilla. Un reloj atrasado no lo cambia, porque sólo
  cuenta un lunes mayor al guardado.
- **Tipos** (el nombre se guarda en el JSON: no se renombra): matar N zombis, completar N oleadas, ganar N monedas
  y derrotar N jefes (este último sólo con la mejor oleada en 9). El avance sale de los contadores de por vida,
  como las misiones; las oleadas se cuentan aparte (`oleadasDeLaSemana`, que avisa `WaveManager` con
  `RegistrarOleada`) porque no hay contador de por vida de oleadas.
- **Cuesta quince partidas y paga la mitad de lo que dan esas quince** (`PartidasQueCuesta`, `FraccionDelPremio`),
  con la vara de `Economia`, la misma de las misiones, el bestiario y la recompensa diaria. Una prueba verifica que
  pague más que la misión difícil del día —dura siete veces más— y que no pase de lo que cuesta.
- **El objetivo y el premio se congelan con la mejor oleada del lunes** (`mejorOleadaAlArmar`, centinela −1),
  igual que las misiones y por la misma razón: si no, guardarlo sin cobrar hasta mejorar la marca sería la jugada
  óptima.
- **Al cambiar de semana, lo cumplido sin cobrar se cobra solo** (`CerrarLaSemana`), como el cierre de medianoche
  de las misiones. Entra por `CobrarPremio`: no cuenta como monedas ganadas jugando.
- **En la ventana de misiones**: una fila dorada arriba de las tres del día, con la etiqueta DESAFÍO SEMANAL, su
  barra, el premio y los días que faltan ("TERMINA EN N DÍAS", y "¡ÚLTIMO DÍA!" el domingo). Cobrarlo salta y suena
  como una misión. Cuenta en la insignia del botón.

## Bestiario

Una tarjeta por tipo de zombi (`Bestiario`, `Assets/Scripts/Progreso/`) con tres estrellas: 100, 1.000 y 10.000 muertes de
ese tipo (el jefe: 1, 10 y 50), contadas con los contadores de por vida por el nombre del asset `Enemy`. Cada estrella se
cobra una vez y **paga la mitad de lo que dejan esas muertes** (`MonedasQueDeja`: lo que suelta ese tipo por el
multiplicador de la oleada, a mitad de camino, como en las misiones), por `CobrarPremio`. Eran 200, 1.000 y 5.000 fijos
× (1 + 0,1 × oleada): 100 caminantes pagaban lo mismo que 100 tanques, que cuestan tres veces más, las primeras
estrellas eran un regalo temprano y las últimas, calderilla. El jefe cuenta con un esfuerzo ×6, porque suelta 35
monedas pero cuesta 500 balas. Lo cobrado va en el progreso
(`estrellasCobradas`). En el menú, un trofeo redondo arriba a la derecha al lado de las misiones, con la insignia de las
estrellas por cobrar; la ventana (`VentanaBestiario`) muestra cada tipo con su color, cuántos lleva, sus estrellas (las
ganadas sin cobrar laten), la barra hasta la siguiente y el botón para cobrar. Los nombres (CAMINANTE, CORREDOR, VELOZ,
TANQUE, JEFE) salen de la tabla.

**Las ventanas del menú que se arman en código usan `ConstructorUI`** (rectángulos, textos, pildoras con el molde de
siempre, barras sin sprite, los botones redondos de las esquinas copiados del globo y la insignia copiada de MEJORAS).
Como escriben sus textos al armarse, **se vuelven a armar al abrirlas si cambió el idioma** (`Idioma.Revision`).

## Mejoras y tienda

Cada mejora es un ScriptableObject `Mejora` en `Assets/Mejoras/`, y `Assets/Mejoras/Resources/CatalogoMejoras`
las junta (un campo tipado por mejora y `enTienda`, el orden de las tarjetas). **El catálogo tiene que quedar en
`Resources`**, y **un `id` no se renombra nunca**: es la clave de los niveles guardados.

| mejora | id | precio inicial | crecimiento | tope | efecto |
|---|---|---|---|---|---|
| Daño de bala | `dano_bala` | 40 | ×1,45 | — | 1 × (1 + nivel) por bala |
| Cadencia | `cadencia` | 50 | ×1,45 | 16 | 4 × (1 + 0,25 × nivel) tiros/s (de 4 a 20) |
| Golpes críticos | `criticos` | 150 | ×1,8 | 8 | probabilidad de daño ×2 por bala: 0, 5, 10, 20, 30, 50, 75, 90 y 100 % (`valoresPorNivel`) |
| Vida máxima | `vida_maxima` | 40 | ×1,45 | — | 80 × (1 + 0,25 × nivel) |
| Granada | `granada` | 250 | — | 1 | desbloquea la granada (ver Granada) |
| Imán | `iman` | 30 | ×1,5 | 13 | sin comprar no hay; 2 × (1 + 0,25 × (nivel − 1)) m (de 2 a 8) |
| Botín | `botin` | 120 | ×1,55 | 15 | monedas × (1 + 0,1 × nivel) |
| Furia | `furia` | 5.000 | — | 1 | desbloquea el botón de furia: 6 s de cadencia y daño ×2 (ver Furia) |

- **El jugador arranca flojo a propósito** (fase 4, pedido de Ivan después de jugar en el teléfono): dispara lento, pega
  1, tiene 80 de vida y no tiene imán (junta las monedas pasándoles por encima). Lo que lo hace fuerte son las compras, y por eso los primeros precios
  son bajos: la primera partida tiene que alcanzar para una o dos. El daño y la cadencia suben de a uno para que
  cada compra se lea en la tarjeta ("1 → 2") y en los números de daño.
- **Golpes críticos**: cada bala sortea al salir (`GunController.EsCritico`, con el 100 % tratado aparte porque
  `Random.value` puede dar 1) y la crítica lleva `DanoPorTiro × multiplicadorCritico` (2). La bala marca `critico` y el
  número de daño sale rojo, más grande y con "!", con el doble de chispas y el golpe más agudo. La granada no tira
  críticos. **`Mejora.valoresPorNivel`**: si tiene valores, el nivel N vale el elemento N y la fórmula se ignora; el
  formato `Porcentaje` lo escribe "5%".
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
  menú con la tienda abierta. Los botones MEJORAS (`BotonMejoras`) no se mueven: lo que late y se bambolea es su insignia, con
  `CatalogoMejoras.ComprasPosibles()`: cuántas compras seguidas alcanzan de verdad, eligiendo siempre la más barata
  (con 50 monedas hay cuatro tarjetas verdes pero alcanza para una sola), y en la derrota "¡Te alcanza para N
  mejoras!".
- **Para agregar una mejora:** un asset `Mejora` con id nuevo → su campo y getter en `CatalogoMejoras` → aplicarla
  en `AplicarMejoras` o en quien la consume → sumarla a `enTienda` → `mejora_<id>_nombre`, `mejora_<id>_unidad` y
  `mejora_<id>_simbolo` (la letra grande de la tarjeta, la inicial en cada idioma) en la tabla de textos → casos en
  `PruebasMejoras`. El campo `simbolo` del asset quedó sólo para el inspector.

## Anuncios

Los videos con recompensa son la única monetización del juego y entran por **tres lugares**: **revivir** al morir,
si no revivió, el **x2 de las monedas en la pantalla de derrota**, y en el menú el **x2 de la recompensa diaria** (ver
Recompensa diaria). **Un solo video premiado por partida**
(`vecesPorPartida`), así que en la práctica es o uno o el otro. Todo lo demás (topes, proveedor, hilos) vive en
`Assets/Scripts/Anuncios/` y el juego no habla nunca con una red de anuncios.

**Las reglas que no se negocian**, porque son la diferencia entre un premio y una trampa:

- **Siempre opt-in y en una pausa natural.** Nunca durante la partida: la derrota es el único momento, y el
  jugador ya terminó de jugar.
- **El premio se dice exacto antes de mirar** ("VER VIDEO: +137 MONEDAS"), no como sorpresa.
- **Cerrar el video antes no castiga**: no hay premio, pero tampoco se gasta el tope del día ni la separación de
  60 s entre videos, así que la oferta sigue en pie. El tope por partida no se aplica al x2 de la diaria, que se cobra
  en el menú.
- **Si no se puede ofrecer, el botón no existe**, no aparece en gris.
- **Se puede apagar**: `Progreso.OfrecerVideos` apaga todas las ofertas y el servicio lo respeta. Hoy no hay
  ningún botón que lo toque (hubo uno en el menú y a Ivan no le gustó): queda para cuando haya pantalla de
  opciones.
- **Un premio nunca es "monedas ganadas jugando".** Entra por `Progreso.CobrarPremio`, no por `Sumar`: cuando
  entre el renacer de la fase 5, lo que se cobró con videos no tiene que contar para los cerebros.

**Las piezas:**

| pieza | qué hace |
|---|---|
| `LugarAnuncio` | los nombres de los lugares, como strings. Se guardan en el JSON: **un lugar no se renombra nunca**. Hoy se usan `revivir`, `duplicar_derrota` y `regalo_x2` (la recompensa diaria). |
| `IProveedorAnuncios` | quién muestra el video: `Listo(lugar)` y `Mostrar(lugar, aviso)`. Cambiar de red es escribir otra clase. |
| `ProveedorNulo` | nunca tiene video: no se ofrece nada. Es el de Windows y el de "todavía no hay red". |
| `ProveedorFalso` | el de las pruebas: un cartel a pantalla completa armado por código, con una barra de 5 s y SALTEAR / LISTO. Prueba el circuito entero sin cuenta ni internet, y anda igual en el teléfono. |
| `ConfigAnuncios` | todos los números, en `Assets/Anuncios/Resources/ConfigAnuncios.asset`. Si falta, no se ofrece nada (con un LogError). |
| `ServicioAnuncios` | la puerta: `PuedeOfrecer(lugar)` y `Mostrar(lugar, alPremiar, alNoPremiar)`. |
| `VigiaAplicacion` | un objeto con `DontDestroyOnLoad` que se instala solo. Vacía los avisos de los videos en el hilo principal y **guarda el progreso cuando la app pierde el foco en cualquier escena** (antes eso lo hacía sólo `MenuPausa`, que no está ni en el menú ni en la derrota). |
| `OfertaDeDuplicar` | el botón de la derrota (objeto `OfertaVideo` en `Perdiste.unity`, componente en `Menu`). |
| `OfertaDeRevivir` | la ventanita de "¡HAS MUERTO!" (prefab `Prefabs/UI/OfertaRevivir` en ShowBies1 y WaveMode). |

**Cuándo se ofrece** (valores del asset): a partir de la 2ª partida terminada, con 180 s jugados en total, hasta
3 veces por día, 1 por partida y con 60 s entre un video y otro. **El tope del día es global y se cuenta con
`Progreso.UsosDeHoyEnTotal()`, sumando los lugares**: contándolo por lugar (`UsosDeHoy(lugar)`), "3 por día" eran
3 de revivir más 3 del x2 de la derrota más 3 del x2 de la diaria, o sea nueve. Si sumás un lugar nuevo, no le des
su propio tope. El x2 pide además una partida de 90 s y 20
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
muerto en el lugar donde cayó. El mundo **se queda en blanco y negro** en 5 s mientras una ventanita chica y semitransparente
(620 x 340 sobre un canvas de 1920 x 1080) muestra "¡HAS MUERTO!" y un botón con una claqueta y un anillo que
se cierra en 10 s. Recién cuando el jugador dice que no,
o se vence el reloj, se llama a `PlayerHealth.Terminar` (récord, `TerminarPartida`, escena de derrota).

- **Una sola vez por partida** (`PlayerHealth.yaRevivio`): con un revivir por video sin límite la partida no
  termina nunca y la tienda deja de tener sentido.
- **Volver no regala nada más que seguir jugando**: `EnemyController.DespejarAlrededor` saca del mapa a los
  zombis que estén a `radioDeDespeje` (7 m) **sin puntos, monedas ni mancha**, como el kill-Z, y el jugador
  vuelve con la vida llena y `segundosDeGracia` (2,5 s) sin recibir daño. Si esos zombis dieran monedas, el
  video sería la forma barata de cobrar una pantalla llena. **El jefe no se despeja** (`EnemyController.EsJefe`, que
  marcan `WaveManager` y `GeneradorZombis`): la oleada lo contaría como muerto y revivir al lado del jefe lo borraría.
- **Caer al vacío no se revive**: el kill-Z del jugador llama directo a `Terminar`. Revivir lo dejaría 20 m bajo el piso
  y volvería a morir con el video gastado.
- **El récord se escribe al morir, antes de ofrecer revivir** (`PlayerHealth.GuardarRecord`, que sólo sube): si
  Android mata la app durante el video, `Terminar` no llega a correr.
- **Que se venza el reloj es exactamente lo mismo que decir que no**, y el botón NO, GRACIAS está desde el
  primer segundo y se lee igual de bien que el otro.
- **Cerrar el vídeo no termina la partida**, que es el caso que hay que mirar con más cuidado acá: el callback de
  "sin premio" es `SinPremio` y **no** `Rechazar`. Vuelve a la ventanita con los botones habilitados y, sobre todo,
  **corre el origen del reloj** (`desde += lo que duró el vídeo`): mientras se mira, `Update` corta con
  `esperandoVideo` pero `Time.unscaledTime` sigue, así que sin eso la cuenta atrás se vence en el acto al volver y
  la partida termina igual por otro camino. Lo mismo vale si el vídeo no se pudo mostrar. Antes los dos caminos
  llamaban a `Rechazar`, o sea que cerrar el anuncio mandaba derecho a la derrota.
- Mientras la ventana está abierta, `OfertaDeRevivir.Activa` es cierto y **`MenuPausa` no pausa**: reanudar
  desde el menú de pausa devolvería el `timeScale` a 1 con el jugador muerto. **`MenuPausa.JuegoCongelado`**
  (pausa u oferta abierta) es lo que miran el input (`PlayerController`, `PlayerJS`, la granada, la furia) y la pausa
  de impacto de `Efectos`: antes una explosión en el momento de morir devolvía el `timeScale` a 1 detrás del ¡HAS
  MUERTO!. Si la escena se descarga con la
  oferta abierta, `OnDestroy` devuelve el `timeScale`.
- Los tres dibujos del botón (círculo, anillo y claqueta) los hace `TexturasUI` en código, así que no hay
  imágenes nuevas en el proyecto; el componente es dueño de esas texturas y las destruye. La claqueta es el
  icono "movie" de Material Symbols redibujado a mano, resuelto con 4 x 4 muestras por píxel para que las
  diagonales salgan suaves.
- **El blanco y negro es `FiltroBlancoYNegro`** (`Assets/Scripts/Jugo/`), un image effect de los de siempre
  (`OnRenderImage` + `Graphics.Blit`) con `Assets/Shaders/BlancoYNegro.shader`. Se engancha a `Camera.main` en
  el momento y se suelta al terminar: es un blit de pantalla completa y no vale la pena tenerlo prendido toda
  la partida para usarlo diez segundos. El material sale de `Assets/Anuncios/Resources/BlancoYNegro.mat` y no
  de `Shader.Find`: **un shader que no usa ninguna escena no entra en la build** y en el teléfono se vería
  rosa. Como la UI en overlay no pasa por la cámara, la ventanita (y el HUD) quedan a color.

**Todavía no hay red de anuncios de verdad.** `ConfigAnuncios.proveedor` está en `Nulo` (la primera versión
para Play sale sin publicidad; la APK de prueba igual fuerza `Falso`) y `Real` no existe:
cuando se integre (AdMob o LevelPlay) es una clase nueva que implemente `IProveedorAnuncios` y un `case` en
`ServicioAnuncios`. Nada del juego se entera. Ojo con dos cosas al integrarla: el plugin de AdMob para Unity
está roto en Unity 6000.3.17 y posteriores (issue 4212 del repo), y AdMob sólo sirve anuncios de verdad cuando
la app ya está publicada y vinculada a su ficha de Play.

## Reseña de Google Play

`PedidoDeResena` (raíz del canvas "Main Menu") pide la ventanita nativa de reseña de Play (In-App Review) **en el menú,
con todo cerrado**, cuando la mejor oleada llegó a 10 y hay 3 partidas terminadas, y después como mucho cada 60 días
(`"ResenaPedidaEn"`, y atrasar el reloj no la vuelve a pedir). Nunca en la partida ni en la derrota, sin preguntar antes
"¿te gusta?" y sin premio: son reglas de Play. Google decide en silencio si la muestra, y fuera de una instalación desde
Play (una APK a mano) no muestra nada.

**Sin el plugin de Unity de Google:** la librería oficial (`com.google.android.play:review:2.0.1`) entra como dependencia
en `Assets/Plugins/Android/mainTemplate.gradle` (la plantilla propia está prendida en Player Settings) y se llama por JNI,
con un `AndroidJavaProxy` para el `OnCompleteListener`. El plugin trae el External Dependency Manager, que se pelea con
Unity 6. **Si se actualiza Unity, hay que volver a copiar su `mainTemplate.gradle` y sumarle la línea.** Solo en Android,
decidido en runtime; en el editor loguea "se pediría".

## Pantallas: idioma, fuente y botones

Todo lo que lee el jugador esta **con Bangers**, la fuente del juego, y **sale de la tabla de textos** (ver Idiomas):
no queda ningun texto con la fuente por defecto de Unity (LiberationSans), que era lo que hacia que el menu y la
derrota parecieran de dos juegos distintos. Si agregas un texto, ponele Bangers y dale una fila en la tabla.

**Todos los botones salen del mismo molde**, el de MEJORAS:

```
Boton            <- Button + BotonJugoso (la raiz recibe el toque y no se anima)
  Sombra         <- Image del mismo sprite, corrida 8 px hacia abajo
  Visual         <- lo que BotonJugoso aprieta, rebota y hace respirar
    Fondo        <- Image con el sprite y el color del boton
    Texto        <- TMP centrado, oscuro
```

El color dice que hace cada uno: **verde** lo que te devuelve al juego (PLAY, PLAY AGAIN, RESUME, ENDLESS),
**dorado** la tienda (UPGRADES) y el idioma elegido, **azul** lo que cambia de modo (RESTART),
**naranja** las oleadas, **gris** lo secundario (TUTORIAL, QUIT, MENU, BACK, NO THANKS).

**Todos los botones son pildoras con icono** (estilo elegido por Ivan): el mismo molde, pero `Fondo` y `Sombra`
usan `Sprites/UI/Pildora` en Sliced (un circulo con bordes de 127 px: Unity achica los bordes al alto del boton y
queda redondo en las puntas) y `Visual` suma un hijo `Icono` (`Sprites/UI/Icono*`, dibujados en blanco y teñidos con el
color del texto). `IconoDeBoton` lo pega a la izquierda del texto y centra los dos juntos, midiendo el texto cada
vez que cambia (idioma, CONTINUE WAVE N). Colores: fondo saturado con texto oscuro de su tono, y lo secundario
(QUIT, TUTORIAL, BACK, MENU) en vidrio oscuro translucido con texto blanco (el blanco no se veia sobre el pasto claro). Llevan icono los botones de accion (jugar,
reiniciar, mejoras, menu, volver, salir y los modos); los que ya dicen todo con su texto o los pinta el codigo (el
precio de las tarjetas, los idiomas, el video de la derrota, NO, GRACIAS) son solo pildora. Para uno nuevo: la forma de
`Pildora`, un `Icono` con `IconoDeBoton` y los colores de arriba.

**La paleta es clara, "pasto de dia"** (elegida por Ivan; es el **tema claro**, ver Tema claro y oscuro): cielo celeste (0,66; 0,86; 0,96) en las camaras, la niebla del
menu y el fondo de la derrota; pasto verde claro (`Materiales/PisoGrilla.png` con `prototype_512x512_green2`, que ya no es
metalico: con `_Metallic` 1 el piso casi no tomaba luz); luz ambiente plana y clara en las escenas; paneles crema (tienda y
ventana de idioma) y tarjetas blancas con texto oscuro. **Todo texto que va directo sobre el mundo o sobre un fondo claro lleva
contorno** con el material `Bangers SDF - Outline` (HUD, derrota, titulos): sin contorno, el blanco y el amarillo se pierden.
La pausa, el revivir y los
paneles del tutorial siguen oscuros a proposito: tapan la partida.

**El `ColorTint` del Button va en blanco.** Los botones viejos lo tenian casi negro para esconder un Image que
ya no existe; con el fondo nuevo, eso lo tenia todo de color negro. Apagar la transicion tampoco va (ver la
trampa).

**Jerarquia de cada pantalla**, que sigue lo que el jugador necesita de un vistazo:

- **Derrota**: GAME OVER, despues **las monedas de la partida** (grandes: es lo que te llevas), despues puntaje y
  record chicos, el renglon de la oferta de video o el aviso de compras, **el proximo objetivo** (`ProximoObjetivo`, armado
  en codigo en y = -155: la mision a medias o la mejora que todavia no alcanza con mas avance, con una barra que se llena)
  y abajo los tres botones, en y = -290 (se bajaron para hacerle lugar; en 21:9 terminan a 50 del borde). Si la partida
  fue record, el puntaje dice "NEW BEST!" y el texto del record se calla (`Score.HuboRecordNuevo`, que mira
  `PlayerHealth.RecordNuevo`: solo superarlo cuenta, un empate no).
- **Menu**: el nombre del juego arriba (en el fondo 3D, no en el canvas), UPGRADES y QUIT en el centro, PLAY grande abajo a la derecha, y el globo del
  idioma y el engranaje del sonido arriba a la izquierda, y las misiones arriba a la derecha. Sin monedas: se ven en la tienda.

- **HUD**: arriba a la izquierda, en orden de importancia, monedas, puntos y oleada o nivel; los FPS al final,
  chicos y translucidos. La vida, grande abajo al centro, **cambia de color** con lo que queda
  (`PlayerHealth.ColorDeVida`: verde arriba del 60 %, amarillo hasta el 30 %, rojo abajo).
- **La barra del jefe** (`BarraDelJefe`, pedido de Ivan: como la de los jefes de Minecraft) va arriba al centro
  mientras hay un jefe vivo, con su nombre, la muesca de la mitad (donde entra en furia, y ahí late en naranja) y una
  barra blanca detrás que baja despacio, para que cada bala se vea. Se arma en código y vive en el canvas del prefab
  `MenuPausa`, que está en las tres escenas de juego (como `CursorMira`), colgando del área segura y antes del panel
  de la pausa, que la tapa. Espera un segundo desde que el jefe aparece: la vida definitiva se la pone quien lo saca,
  con los multiplicadores de la oleada, y preguntarla antes la fijaría sin ellos.

**Con el modo oscuro el fondo del menu pasa a la noche** (ver Tema claro y oscuro).

**El fondo del menu esta vivo.** `FondoMenu` (objeto raiz `FondoMenu` de `Menu.unity`) acomoda la camara del menu
mirando un poco desde arriba, reusa su luz direccional y arma el piso de la partida con niebla del color del cielo,
para que no se vea donde termina. Por delante cruzan zombis de verdad (los cinco prefabs, con pesos en el
inspector): se instancian dentro de un padre apagado y se les borran scripts, colliders y rigidbodies antes de
prenderlos, asi **no cuentan en `ZombisVivos`** ni buscan al jugador. `MonedasDelFondo` (en la raiz del canvas
"Main Menu") deja caer monedas doradas girando justo encima de `BG`, que quedo con alfa 0. **El logo tambien es
parte del fondo:** `FondoMenu/Titulo` es un quad con `Materiales/LogoMenu.mat` detras de los zombis, que
`TituloEnLaNiebla` esconde a medias y vuelve a mostrar llevando la imagen hacia el color del cielo. Nunca lo tapa
entero: tiene que leerse en las capturas de la ficha. La niebla es a mano y no la de Unity, porque tiene que ir al
cielo de `FondoMenu` (que con el modo oscuro se hace de noche) y no al de la escena: la hace el shader
`ShowBies/LogoEnLaNiebla`, que mezcla la textura con `_ColorNiebla` segun `_Niebla` **sin tocar el alfa**, asi lo
que se esconde es el color y no la silueta. **Antes era el nombre escrito con un `TextMeshPro` 3D**, y ese camino
sigue en el componente por si `logo` esta vacio. **La niebla esta
guardada en la escena y no solo en el codigo:** el stripping de shaders mira la niebla de las escenas del build, y
prendida solo en runtime no tendria variantes en Android.

Las posiciones de la derrota y de la ventanita de revivir estan **medidas**, no puestas a ojo: cuando muevas algo
de esas pantallas, revisa que ningun par de elementos se pise, contando los que se prenden solos (la oferta de
video y el aviso de compras comparten renglon a proposito).

## Tema claro y oscuro

La interfaz tiene **dos temas**: el **claro** de siempre (paneles crema, tarjetas blancas, texto oscuro, el menú
de día) y el **oscuro** (pedido de Ivan), que se prende con **MODO OSCURO** en la ventana de opciones del engranaje
y queda guardado. Cambia el menú, la tienda, la derrota y todas las ventanas; **las partidas no se tocan**, que el
tema es de la interfaz y no del mundo.

- **El tema claro es lo que está guardado en cada escena y prefab**, no una paleta aparte: `PintarConTema` se
  acuerda del color que traía el objeto (`colorClaro`, que se completa solo al ponerle el componente) y lo devuelve
  tal cual. `Tema` sólo define la paleta del oscuro. Por eso sumar el modo oscuro no puede cambiar cómo se ve hoy
  el juego, y por eso un color nuevo del tema claro se toca en la escena, como siempre.
- **Cada color cumple un papel** (`RolDeTema`): `Panel` (el fondo de una ventana), `Tarjeta`, `Texto`, `TextoSuave`,
  `Hueco` (el relieve que separa una fila), `Surco` (el fondo de una barra), `Vidrio` (los botones secundarios),
  `Apagado`, `Fondo` (una pantalla entera, la derrota) y `Acento` (un verde que sobre claro es oscuro y sobre el
  oscuro no se leería). **Lo que tiene color propio no lleva papel**: el verde de jugar, el dorado de la tienda, el
  rojo de GAME OVER y el color de cada mejora significan algo y valen en los dos temas.
- **Un objeto de escena o prefab lleva `PintarConTema`** con su papel: la ventana del idioma (que copian la de
  opciones y la de salir), el fondo y el pie de la tienda, la tarjeta de mejora, el fondo y los textos de la
  derrota y los botones de vidrio. **Lo que se arma en código** pasa su color de siempre por `Tema.Elegir(claro,
  rol)`; si esa ventana no se vuelve a armar, se le pone el componente con `Tema.Pintar(grafico, rol, claro)` y se
  repinta sola. Las ventanas de misiones y bestiario se rearman al abrirlas si cambió `Tema.Revision`, igual que
  con `Idioma.Revision`.
- **Nadie se suscribe a nada**: `Tema.Revision` sube con cada cambio y quien pinta lo mira en su `Update`, como con
  `Progreso.Revision`. La preferencia va en `PlayerPrefs["TemaOscuro"]`, como el idioma y los volúmenes, porque es
  del dispositivo y no progreso. **Arranca en claro**, que es como salen las capturas de la ficha.
- **El menú se hace de noche** (`FondoMenu`): el cielo, la luz, la luz ambiente y la niebla se funden en 0,7 s con
  la misma paleta que el capítulo del cementerio y el pasto pasa a la tierra (`PisoCementerio.mat`). Se ve mientras
  se toca el interruptor, con la ventana de opciones abierta encima. El título 3D se esconde en la niebla hacia
  `FondoMenu.CieloActual` y no hacia un celeste fijo, o de noche quedaría como un halo claro.
- **El interruptor** (`Interruptor`) es una píldora con una perilla que se corre, armada en código como
  `SliderVolumen`, y se usa para cualquier sí/no. La ventana del engranaje pasó a llamarse OPCIONES: los dos
  volúmenes y el modo oscuro.
- **Un texto sobre un fondo de color fijo no lleva el papel `Texto`**: la fila de una misión cobrada (verde), el
  casillero de hoy de la diaria (dorado) y la inicial del bestiario (el círculo del color del zombi) no cambian con el
  tema, así que su texto va oscuro siempre. Con el papel puesto quedaban casi blancos sobre verde o sobre lima.
- **Lo que se arma en código sobre un panel recibe su color de quien lo crea** (`SliderVolumen.Crear` e
  `Interruptor.Crear` toman el color del texto y el del surco): el mismo control va sobre la ventana crema del menú y
  sobre el panel negro de la pausa, y ahí el blanco y el negro se dan vuelta.
- **Si agregás una pantalla**, mirá qué color cumple cada papel y ponele `PintarConTema` a lo que sea crema, blanco
  o texto oscuro. Lo que no lleva papel se queda igual en los dos temas, que casi siempre es lo que se quiere para
  un botón de color.

## Idiomas

El juego está **en inglés por defecto** y se puede pasar a **español de España**. Arranca en inglés la primera vez y
en cada instalación nueva, sin mirar el idioma del teléfono; el jugador lo cambia con el **globo** de arriba a la
izquierda del menú, que abre una ventana con un botón por idioma. La elección queda en `PlayerPrefs["Idioma"]`
(es una preferencia del dispositivo, no progreso).

**Todo texto que ve el jugador sale de la tabla** `Assets/Idioma/Resources/Textos.txt`: una fila por texto, con
`id`, `en` y `es` separados por TAB. Se abre con cualquier planilla. Nunca escribas un texto a mano en una escena
ni en el código.

- **Un texto fijo** de una escena o un prefab (un título, la etiqueta de un botón) lleva el componente
  `TextoTraducido` con su `id`: lo escribe al prenderse y cada vez que cambia el idioma, sin recargar la escena.
- **Un texto que arma el código** usa `Textos.De("id")` o `Textos.Formato("id", a, b)`. Escribí el id entero
  entre comillas: la prueba lo busca así en el código (y también `x.id = "id";`, cuando el código le cambia el id a
  un `TextoTraducido` que copió, como `OpcionesSonido` y `ConfirmarSalir`). La excepción son las mejoras, cuyo nombre y unidad salen
  de `mejora_<id>_nombre` y `mejora_<id>_unidad` (el `id` de la mejora ya es fijo para siempre); los campos
  `nombre` y `unidad` del asset quedaron sólo para el inspector.
- **Casi nada es una frase suelta**: son plantillas con `{0}`, `{1}` y rich text (`<size=55%>COINS</size>  {0}`).
  Se traduce la plantilla entera, formato incluido, y cada idioma tiene que tener **los mismos `{n}`** (uno que
  falta tira una excepción en plena partida). La prueba lo verifica. En `TMP_Text.SetText(formato, número)`, que
  no aloca, la plantilla de la tabla funciona igual.
- **Lo que se refresca solo al cambiar de idioma**: los `TextoTraducido`, la tienda (tarjetas y pie), los botones
  MEJORAS y los contadores de monedas. El idioma se cambia desde el menú, así que lo que se escribe una vez en
  partida (HUD, derrota) no necesita refrescarse. Si agregás algo al menú que arme texto por código, compará
  `Idioma.Revision` como hacen ellos.
- **Los números también dependen del idioma** (`FormatoNumeros`): `1,234` / `4.5M` / `2.3B` en inglés y
  `1.234` / `4,5 M` / `2,3 MM` en español. Los separadores se arman a mano: la cultura del sistema no está en
  todas las builds y la de un teléfono en otro idioma daría otra cosa.
- **El español es de España**: tuteo, "ratón", "coger", "vídeo" con tilde. Nada de voseo.
- **Un id que falta no rompe nada pero se ve**: sale `[id]` en pantalla y un aviso en la consola. Un texto vacío
  en un idioma cae al inglés.
- **Para sumar un idioma**: una columna con su código en la cabecera de la tabla, un valor en `Lengua` (en el
  orden de las columnas), su código y su nombre propio en `Idioma`, y los números en `FormatoNumeros`.
- **ShowBies > Idioma** cambia el idioma desde el editor; "Olvidar" deja el editor como alguien que abre el
  juego por primera vez.
- Se eligió este sistema y no el paquete Localization de Unity porque ese depende de Addressables: demasiado para
  dos idiomas y ~75 textos. Si algún día hacen falta muchos, los textos ya están separados en una tabla.

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
  ("COMBO xN", con una ventana de 1,5 s entre muertes), que **suena**: cada salto toca `combo.wav` (una marimba en la
  bemol, sintetizada) un grado más arriba, uno por frame como mucho, y al cruzar x10, x25, x50 y x100 muestra su
  palabra (¡ARRASANDO!, ¡MASACRE!, ¡IMPARABLE!, ¡LEGENDARIO!) un rato, con un arpegio, un temblor y un salto más
  grande. Solo efecto: no da monedas.
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
- **Sin música en la partida** (pedido de Ivan): el `AudioSource` del prefab quedó sin clip. `Assets/otros/musica.wav`
  (el loop de 32 s en la bemol mayor) sigue en el proyecto sin uso. Los sonidos nuevos están sintetizados y son provisorios.

## Volumen

El jugador elige **dos volúmenes**, efectos y música (`Volumen`, en `PlayerPrefs` "VolumenEfectos" y "VolumenMusica", de 0
a 1). Se cambian desde el **engranaje del menú** (a la derecha del globo) y desde el **menú de pausa**, con el mismo control
(`SliderVolumen`, armado en código).

- **Toda fuente de escena o prefab lleva `FuenteConVolumen`**: su volumen queda en el del inspector por el del jugador. Las que
  hacen loop son música (la del menú y la de `Efectos`), el resto efectos (el disparo). Si agregás un `AudioSource`, sumale el
  componente o no va a responder al control. Para bajar una fuente un rato se usa su `Atenuacion`, no `volume`: la tienda
  baja así la música del menú.
- **`Sonidos` multiplica por el volumen de efectos** al tocar y al programar, así golpes, monedas, explosiones y la tienda
  responden sin componente.
- **La ventana del menú no tiene objetos propios**: `OpcionesSonido` copia al arrancar el globo y la ventana del idioma
  (`SelectorIdioma`) y cambia los botones de idioma por los dos volúmenes y el **modo oscuro** (ver Tema claro y
  oscuro); por eso se llama OPCIONES y no SONIDO, aunque la clase conserve el nombre. `VolumenEnPausa` (raíz del prefab `MenuPausa`)
  los arma debajo de los botones de la pausa. El atrás de Android cierra la ventana de sonido primero.
- Se escribe a disco medio segundo después de soltar el control o al cerrarse la ventana, no en cada movimiento.

## Persistencia

El récord, el último modo y el tutorial van por `PlayerPrefs`; las monedas y el progreso no (ver Monedas y
progreso):

| clave | quién escribe | quién lee |
|---|---|---|
| `"Score"` | `PlayerHealth` al morir | `Score` (pantalla de derrota) |
| `"HighScore_<buildIndex>"` | `PlayerHealth`, si superás el récord de ese modo | `highscoretext`, el del modo en `"UltimoModo"` |
| `"UltimoModo"` | `PlayerHealth`, el buildIndex de la escena | `MenuPerdiste.Retry`, `highscoretext`, `TiendaMejoras.Jugar` |
| `"TutorialCompletado"` | `TutorialManager`, al terminar el tutorial | nadie todavía |
| `"VolumenEfectos"`, `"VolumenMusica"` | `SliderVolumen` (menú y pausa) | `Volumen`; sin nada guardado, 1 |
| `"Idioma"` | `SelectorIdioma` (el globo del menú), `"en"` o `"es"` | `Idioma`; sin nada guardado, inglés |
| `"TemaOscuro"` | el interruptor de la ventana de opciones, 0 o 1 | `Tema`; sin nada guardado, el tema claro |
| `"ResenaPedidaEn"` | `PedidoDeResena`, la fecha `yyyy-MM-dd` del último pedido | `PedidoDeResena` |

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
  menú (`MenuPerdiste`), y en el menú principal cierra primero la ventana de idioma, después la tienda, después el panel de modos o,
  en el principal, pregunta si salir del juego sólo en móvil (`ConfirmarSalir`, la misma ventana del botón SALIR; lo
  maneja `BotonAtrasMenu`, en el canvas "Main Menu", único lector de Escape del menú). `RestartScene` ya no cierra el
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

**En PC el puntero es una mira** mientras se juega (pedido de Ivan): `CursorMira`, en la raíz de `MenuPausa.prefab`, así
está en las tres escenas de juego. La textura es `Sprites/UI/Mira.png`, importada como **Cursor** (legible, RGBA32 y sin
mipmaps, que es lo que pide `Cursor.SetCursor`), con el centro, que es el punto que apunta, en (32, 32). Con el juego
congelado (`MenuPausa.JuegoCongelado`: la pausa o el ¡HAS MUERTO!) vuelve la flecha para tocar botones, y al apagarse
el componente también: el puntero es de toda la aplicación y cruza escenas, así el menú y la derrota no heredan la
mira. En el editor se ve con el target en Windows o con "Teclado y mouse en el editor".

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
  y todo lo estático está marcado `BatchingStatic`.
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
  cada uno (1 el normal y el rápido, 0,45 el tanque, 2,5 el FASTER, 0,3 el jefe), por el parámetro `Paso` del
  controller y no por `animador.speed` (ver Las animaciones de los zombis). Antes el tanque, el jefe y el
  FASTER eran la cápsula y los cubos a la vista.
- `ContadorFps` muestra los FPS en el HUD de las escenas de juego, para medir en el teléfono sin
  Profiler. "Anda lento" no se optimiza; "32 FPS con 35 zombis" sí.

### Build de Android

Dos entradas de menú en `Assets/Editor/ConstructorAndroid.cs`, ambas escriben el veredicto en
`Builds/build_result.txt` (raíz del repo, gitignoreada) y sirven por CLI con `-executeMethod`:

- **Build > Android APK** (`ConstructorAndroid.BuildApk`): `Builds/ShowBies.apk` firmado con el
  debug keystore, para probar en el teléfono. No pide nada. Sale con el paquete `com.ivanruiz.showbies.prueba` y el
  nombre "ShowBies (prueba)" (los restaura al terminar): la de Play está firmada con otra clave y Android no deja
  instalar una encima de la otra, así que conviven, cada una con su progreso.
- **Build > Android AAB (release)** (`ConstructorAndroid.BuildAab`): `Builds/ShowBies.aab` firmado
  con el keystore de release, que es lo que se sube a la Play Store. Lee ruta, alias y passwords
  de `ShowBies1/keystore.local` (gitignoreado; plantilla en `keystore.local.example`) y los limpia
  de `PlayerSettings` al terminar, así el keystore nunca queda configurado en `ProjectSettings` ni
  la build de APK se rompe por falta de password.

- Configuración: package `com.ivanruiz.showbies`, el que está registrado en la cuenta de Play Console (una app
  publicada no lo cambia nunca, y uno que no esté registrado en la cuenta no se puede subir),
  IL2CPP + ARM64, minSdk 25, **targetSdk 36** (fijo: Google lo exige a las apps nuevas desde el 31/8/2026, y en
  "automático" depende del SDK que tenga instalado la máquina), `bundleVersion` / `AndroidBundleVersionCode` en
  `ProjectSettings.asset` (el versionCode tiene que subir en cada subida a la Play Store).
- Orientación: rotación automática sólo entre los dos horizontales (`defaultScreenOrientation: 4`, sin
  portrait). Antes estaba fija en uno solo (`reverseLandscape` en el manifest) y no giraba con el
  teléfono al revés.
- **Keystores: `*.keystore`, `*.jks` y `keystore.local` están gitignoreados.** Había un
  `ShowBies1/user.keystore` de 2023 versionado, con password desconocida; se sacó del repo (queda
  en disco por si aparece la password). El de release se genera con `keytool` y se guarda con
  backup fuera del repo: si se pierde, no se puede actualizar la app publicada (salvo con Play App
  Signing, que conviene activar al subirla por primera vez).
- **El AAB lleva los símbolos nativos** (`UserBuildSettings.DebugSymbols`: `SymbolTable` dentro del bundle, y se
  restauran al terminar), así los crashes de Android vitals llegan con nombres de funciones.
- **No hay paquete Sentis** (`com.unity.ai.inference`): no lo usaba nadie y, por la carpeta Resources que trae, metía
  7,6 MB (el 23 % del AAB) y ~970 warnings de shaders en cada build. Se fue con burst, collections, app-ui y
  test-framework.performance, que sólo traía él (este último creaba un `Assets/Resources` vacío). Los ejemplos de
  TextMesh Pro tampoco van: estaban gitignoreados pero su `Resources` entraba igual en la build.
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

- **Ningún script de un zombi puede llevar `RequireComponent` de otro script suyo.** `FondoMenu` le borra los
  scripts a los zombis que cruzan por detrás del menú, y un `RequireComponent` no deja borrar el componente del que
  se depende: el `EnemyController` quedaba vivo sin Rigidbody y el jefe del fondo tiraba una excepción por
  aparición, con el zombi invisible. `JefePatrones` lo necesita y no lo declara a propósito.

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

- **La fuente del juego vive en `Assets/Fuentes/`**: `Bangers.ttf`, `Bangers SDF.asset` (dinámica: completa el atlas desde el
  .ttf, así que no se borra ninguno de los dos) y el material `Bangers SDF - Outline`. Antes estaban en
  `TextMesh Pro/Examples & Extras/`, que está gitignoreado: un clon del repo no tenía la fuente. Se movieron con
  `AssetDatabase.MoveAsset`, con los mismos guid. Nada del juego tiene que apuntar a esa carpeta de ejemplos.
- **Construir UI en el editor ensucia el atlas dinámico de Bangers** (`Bangers SDF.asset`) y el fallback de
  LiberationSans. Si aparecen modificados en git sin haber tocado fuentes, se restauran. Bangers no tiene `→`: la
  flecha de las tarjetas es un sprite.

- **Con la ventana de Unity en segundo plano, el juego en play NO corre** (`Run In Background` está apagado en
  ProjectSettings). No corren `Update` ni `OnRenderImage`, no se renderizan frames y
  `ScreenCapture.CaptureScreenshot` no escribe nada, pero el editor sí sigue vivo: una prueba manejada desde
  `EditorApplication.update` parece avanzar y todo lo del juego parece roto. Si hay que medir o fotografiar
  algo en play sin mirar la pantalla, prendé `PlayerSettings.runInBackground` y volvelo a apagar al terminar.

- **Un `Image` sin sprite ignora `Image.Type.Filled`.** La barra del anuncio de prueba se veía llena desde el
  primer frame por eso; ahora mueve el ancho del `RectTransform`. Lo mismo vale para cualquier medidor que se
  arme por código con un rectángulo de color.

- **La transición de un botón es ColorTint con el normal en blanco.** Los botones viejos tenían un Image negro
  que el ColorTint dejaba invisible, así que ponérselo en None lo hacía aparecer; hoy ese Image ya no está,
  pero el ColorTint sigue: si el normal no es blanco, tiñe el fondo de color del botón.

## Pruebas y medición

- **ShowBies > Pruebas > Logica de mejoras** (`PruebasMejoras.CorrerTodas`): precios, efectos, textos, escalado,
  acumuladores, guardado y migración (en una carpeta temporal), compras y **todo el circuito de los anuncios**
  (premio una sola vez aunque el SDK avise dos, cerrar sin castigo, topes del día, falla premiada, el x2
  completo), con un proveedor de mentira que se enchufa con `ServicioAnuncios.UsarParaPruebas`, y **los idiomas**
  (que cada texto tenga los dos idiomas y los mismos `{n}`, y que existan todos los ids que piden el código, las
  mejoras, los prefabs y las escenas) y **el tema** (que el claro devuelva el color de la escena, que cada papel del
  oscuro tenga su color y que lo que se escribe encima se lea: el contraste se mide con la fórmula de la WCAG, no se
  mira). Las pruebas fijan el idioma en español al empezar y lo devuelven al terminar. No corre en play. Escribe `Builds/pruebas_mejoras.txt` y termina en `RESULTADO: TODO OK` o `N FALLAS`.
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
6. ¿Tiene UI? Lo que sea crema, blanco o texto oscuro lleva `PintarConTema` con su papel, o pasa por
   `Tema.Elegir` si se arma en código (ver Tema claro y oscuro). Los cuatro canvas usan `ScaleWithScreenSize`. Las escenas de juego tienen la referencia
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
