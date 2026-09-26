# Pendientes de la auditoría del 24/9

La auditoría tuvo 20 frentes. En cada uno, un agente `buscar:` salió a buscar errores, riesgos y mejoras, y un
`mejoras:` revisó las mejoras contra el código. **La ronda `refutar:`** (un escéptico que intenta tumbar cada error
antes de arreglarlo) pasó al principio solo por los frentes de calidad y de diseño, por pedido de Ivan, y **el 26/9
por los quince hallazgos que quedaban** (ver la sección 1). **Lo chico y seguro ya está aplicado**, y quien lo aplicó
lo verificó leyendo el código (los commits "Aplicar ... de la auditoria" y los que siguen). Este archivo se llamaba
`AUDITORIA.md`. Acá queda:

1. **La ronda `refutar:` del 26/9**: el veredicto de cada hallazgo que estaba sin verificar, y adónde fue a parar.
2. **Verificados, para después**: confirmados leyendo el código, pero grandes, con algo para decidir o que esperaban
   la fase 2 (`PlayerHealth.cs` y el constructor de neón, que ya están en main).
3. **Para decidir**: mejoras y cambios de diseño que valen la pena pero cambian el juego o una decisión de Ivan.
4. **Para mirar en Unity**: lo que se aplicó sin poder probarlo.
5. **Antes de integrar la red de anuncios**: ver `publicacion/pasos.md`.
6. **Descartados**: lo que se refutó (no pasa hoy), con la recomendación para cuando cambie algo.

---

## 1. La ronda `refutar:` del 26/9

Quince escépticos, uno por hallazgo, intentaron tumbar cada uno leyendo el código y los YAML de HEAD (7276891), sin
Unity, y recalcularon los números con los valores serializados. Quedaron 2 refutados, 6 confirmados y 7 parciales
(pasan, pero distinto o menos grave de lo que se había anotado). Lo confirmado pasó a la sección 2, lo que pide una
decisión de Ivan a la 3 y lo refutado a la 6. Lo marcado "sin medir" sale de un modelo: medirlo antes de arreglar.
La ronda encontró además cosas que no estaban anotadas: el decorado que se arma en plena pelea, los shaders que se
compilan en el primer golpe, los niveles baratos de los probadores, dos pruebas circulares y una frase falsa en la
política de privacidad publicada.

| hallazgo | veredicto | gravedad | sección |
|---|---|---|---|
| Los jefes se farmean | confirmado | media (era alta) | 2 |
| La migración a v6 | parcial: solo pasa con archivos v4 y v5 | baja (era media) | 2 y 3 |
| Las monedas de logro sin cobrar | confirmado | baja (era media) | 2 |
| "Gana N monedas" | parcial: la causa es otra | baja (era media) | 2 |
| La misión de críticos | parcial: el arreglo propuesto regala | baja | 2 |
| El bestiario sin congelar | confirmado | baja | 2 |
| Cerrar la app en ¡HAS MUERTO! | parcial: hoy solo en la APK de prueba | baja hoy, media con anuncios | 2 |
| Morir por el kill-Z | refutado | — | 6 |
| El kill-Z en el tutorial | refutado | — | 6 |
| El récord al reiniciar | parcial: el arreglo propuesto rompe las oleadas | baja | 3 |
| Las balas atraviesan al FASTER | parcial: no es cosa de los 30 FPS | baja a 60 FPS, media a 20-30 | 2 |
| El empuje de la bala | confirmado, y más fuerte | media | 3 |
| La cámara a 50 Hz | confirmado | media | 2 |
| Los pools en plena pelea | parcial: el tirón es otro | baja | 2 |
| `progreso.json` en el almacenamiento externo | confirmado | baja | 2 |

## 2. Verificados, para después

- **Una fecha guardada en el futuro bloquea la diaria, las misiones, el semanal y los vídeos hasta esa fecha** (media).
  Es real, pero el arreglo propuesto (topar las fechas guardadas en mañana) reabre la trampa del reloj atrasado:
  atrasar el reloj, abrir, volver a la fecha real, y se cobra todo otra vez, sin límite. **Versión segura**: topar solo
  con la hora automática de Android prendida (`Settings.Global` `auto_time`, por JNI en `RelojConfiable`). Pide un
  teléfono para probarlo.
- **`PlayerHealth.instance` no está en ningún reset** (fase 2). **Arreglo**: `instance = null` en su reset y
  `OnDestroy { if (instance == this) instance = null; }`.
- **La caja de vida se consume con la vida llena** (fase 2). **Arreglo**: no tomarla con la vida llena (salvo en el
  tutorial) y tomarla con `OnTriggerStay` cuando baje.
- **Prueba de `ColorDeVida`**: es estática "para probarla" y ninguna prueba la llama (esperar a los colores de la fase
  2).
- **Tienda**: la rueda del mouse casi no mueve la fila (`m_ScrollSensitivity: 1` en el prefab; hay que decidir la
  dirección, en uGUI la rueda hacia arriba la lleva a la derecha); en 21:9 la flecha de la guía, en lo más bajo del
  rebote, se mete unos 10 px en el pie; y el toque que frena la fila ya no compra, pero el botón igual suena y se
  aprieta (`BotonJugoso`).
- **Ciudad**: las cajas (hasta 30 s) y monedas (hasta 20 s) que ya estaban en el piso cuando sale la ciudad quedan
  tapadas por los edificios, y los autos no cuentan como tapadores (1,8 % del área).
- **En PC la granada comprada no tiene botón ni aviso**: nada dice que se tira con ESPACIO, y en la recarga no hay
  respuesta. **Arreglo**: `showOnPC: 1` en el `ConditionalShow` de `BotonGranada` de las tres escenas (en Unity, junto
  con la fase 2 del HUD) y, en `JoystickGranada`, en PC la etiqueta "GRANADA (ESPACIO)" y el botón sin raycasts (el clic
  ya dispara el arma).
- **La diaria del día 1 con OTRA VEZ**: ahora la tienda que se abre desde la derrota espera a la diaria, pero OTRA VEZ
  no pasa por el menú, así que quien solo juega así no la ve. Opcional: una insignia en MENÚ de la derrota con lo que
  espera en el menú (después de la fase 2, que rehace la derrota).
- **El daño al jugador según cuánto entró** (fase 2): `Efectos.DanioJugador(fraccion)` ya está (un golpe grande suena
  más fuerte y grave, sacude más y no espera la ventana de 0,4 s), pero `PlayerHealth` todavía llama a la versión sin
  fracción. Falta una línea en `PlayerHealth.TakeDamage`:
  `if (health > 0) Efectos.DanioJugador((float)dano / maxHealth);`.
- **El limitador de sonido no llega a todo**: una granada que no mata queda en +2 dBFS durante 1,3 ms (golpe más
  explosión suman lo mismo que la muerte del jefe, que no satura) y con un golpe al jugador en el cuadro siguiente,
  en +3,2. Si en el teléfono sigue crujiendo, el paso siguiente es un limitador de verdad sobre la salida
  (`OnAudioFilterRead` en el `AudioListener`) o bajar la explosión.
- **Sonido, lo que quedó afuera**: `AvisoDeMisiones` toca su copia del jingle encima del cartel de la oleada (hay que
  esperar ~1 s después del último cartel); un tono distinto por caja (hoy las tres tocan `pop.mp3`); un clic al
  intentar la granada en recarga; y un disparo sintetizado propio si el de ahora cansa.

### De la ronda `refutar:`

- **Los jefes de las misiones y del semanal se farmean** (media). `EnemyController.cs:874` suma a `jefesMatados` todo
  zombi con `EsJefe`, sin mirar el modo, y lo leen `MisionesDiarias.cs:208` y `DesafioSemanal.cs:124`. En el libre
  sale un jefe cada 30 s (`ShowBies1.unity:1330-1337`) con 500 de vida en el nivel 1, y REINICIAR o la R lo vuelven al
  nivel 1: con mejor oleada 40, los 10 jefes de la misión (cotizada en 59 min, paga 51.450) salen en ~5 min, y los 60
  del semanal (cotizado en 5,9 h, paga 257.300) en ~30 min (en móvil, con el techo de 35 zombis, quizá uno cada ~67
  s). Retomar la oleada 10 (pausa → MENÚ → OLEADAS) vuelve a sacar su jefe, en un ciclo de ~20 s. Pega solo cuando
  sale ese tipo: la difícil, ~1 de cada 5 días, y el semanal, 1 de cada 4 semanas. **Arreglo**, más simple que el de
  la auditoría: sumar `jefesMatados` en `WaveManager` al completar una oleada con jefe, al lado de
  `RegistrarOleadaCompletada`, y no al morir el zombi. Cierra el libre y el retomar sin campo nuevo ni subir la
  versión (solo lo leen las misiones y el semanal). Costo: no cuenta un jefe matado en una oleada en la que después se
  muere. Ajustar `PruebasMejoras.cs` (~3072-3105, 3254-3276 y 3574). Las estrellas del jefe en el bestiario siguen
  contando muertes: se cobran una sola vez, y farmearlas solo las adelanta.
- **Dos premios pagan con la oleada del cobro y no con la de cuando se ganaron** (baja):
  - **Las monedas de logro.** `Logros.Revisar` anota la moneda con su experiencia congelada (`Logros.cs:199-200`),
    pero la experiencia entra recién en `Cobrar` (`:212-218`), y los niveles que cruza crean su `PremioDeNivel` con la
    mejor oleada de ese momento (`NivelJugador.cs:126-130`). Guardar un bronce de la oleada 30 a la 45 paga ~2,7 veces
    (unas 4.900 monedas más el bronce y 19.300 el oro, entre el 0,2 y el 0,6 % de lo que se gana en ese tramo);
    guardar las 36 hasta la 50, ~394.000 (un 31 % más de premios de nivel). Rompe una regla escrita: "guardarla para
    cobrarla más arriba no rinde". **Arreglo**: sumar la experiencia en `Revisar`, al anotar la moneda, y dejar COBRAR
    como festejo. El `min(MejorOleada, oleadaAlGanar)` de la auditoría deja entre el 25 y el 100 % del hueco, porque
    los niveles que suben los zombis mientras la experiencia espera también se cruzan más tarde. Cambia las pruebas de
    logros (`PruebasMejoras.cs:3962-3969`) y **va junto con el arreglo de la migración**: si no, el primer `Revisar`
    de un progreso migrado sube de golpe todos esos niveles.
  - **Las estrellas del bestiario.** `Bestiario.cs:109` cobra con la mejor oleada de ahora, y la tarjeta muestra ese
    monto (`VentanaBestiario.cs:187` y `:220`). La 2ª estrella del caminante pasa de 1.450 a 4.650 guardándola de la
    oleada 10 a la 40 (3,17 veces), y las 15, guardadas hasta la 40-50, dan de 49.000 a 269.000 más (del 4 al 7 % de
    lo ganado jugando). Guardada más de 18 oleadas rompe la regla "el premio no pasa lo que dejan esas muertes", que
    la prueba (`PruebasMejoras.cs:3674`) solo mira con la misma oleada. **Arreglo**: una lista {tipo, estrella,
    oleada} anotada al ver la estrella (desde `AvisoDeMisiones.AnotarEstrellas` y `VentanaBestiario`, como
    `Logros.Revisar`), sin subir la versión: un JSON viejo deja la lista vacía y congela al ver. El +N de la tarjeta
    tiene que usar la misma oleada.
- **La migración a v6** (baja). El mecanismo de la auditoría es real (`MigrarAlNivel` deja `nivelPremiado` en el nivel
  migrado, y el primer `Revisar` anota los logros ya cumplidos con ese nivel alto, que al cobrarlos cruzan ~17 niveles
  con premio), pero solo con archivos v4 y v5, que hay solo en las APK de prueba y el editor de Ivan: la prueba
  cerrada salió con la 1.2.0 (5), del commit 16d8a38, con `VersionActual = 3`, y después no hubo otro AAB. A un v3 la
  migración le da 0 de experiencia, y en la oleada 35 cobra ~6.600 monedas de logros. **Lo que sí le toca**: arranca
  en el nivel 1 con la mejor oleada alta, y cada nivel barato paga como esa oleada. Uno de la 35 sube del nivel 3 al
  14 en su primera partida y cobra ~49.600 monedas (2,2 veces lo que deja esa partida), ~370.000 en 40 partidas.
  Decidir (sección 3). El arreglo para v4 y v5 no puede ir adentro de `MigrarAlNivel` (`Logros` usa
  `Progreso.Jugador`, que llama a `Cargar()` con `datos` en null: recursión): una marca, y un paso después de `datos =
  leidos`.
- **"Gana N monedas" y la misión de críticos se miden con varas que no son las del juego** (baja):
  - **Las monedas.** `MonedasPorPartida` toma el multiplicador de la oleada a mitad de camino (1,08^(m/2)), y la media
    real, pesada por zombis, es casi el doble (8,99 contra 4,66 en la oleada 40); además la mezcla suelta 2,41 monedas
    por zombi, no 2. El bono pesa poco (el 4 % en la 40). Juntándolas todas, una partida da 1,2 (m = 5), 1,5 (10), 1,7
    (20) y 2,5 veces (40) lo que dice el modelo, y 6 veces con el botín al máximo en la 40: el semanal de monedas
    cuesta 6,1 partidas sin botín y 2,5 con botín 15, en vez de 15. No regala (la misión paga igual el 40-60 % de lo
    ganado al cumplirla), pero en los jugadores avanzados el semanal deja de durar una semana. **Arreglo**: una vara
    "de más" solo para el objetivo de monedas: la suma exacta por oleada con la mezcla de WaveMode (en constantes que
    una prueba compare con la escena), el botín comprado y un factor de cobro. Medir cuánto se junta de verdad: las
    monedas vencen a los 20 s y hay techo.
  - **Los críticos.** `BalasPorPartida` (`Economia.cs:58-63`) no mira ni el daño ni los críticos. Con la mejor oleada
    quieta, comprar críticos o daño encarece la misión (la media al 100 % cuesta 1,85-1,94 partidas en vez de 1); con
    la oleada de frontera se cancela. Dividir por `1 + p` corrige solo p, y si `BalasPorPartida` estuviera bien
    calibrada regalaría (la difícil al 100 % costaría ~1,25 partidas y pagaría 1,5). **Arreglo**: calcular los golpes
    con el daño real, `p·Σvida/(D·(1+p))`, o primero medir los críticos por partida contra el objetivo.
  - **Las dos pruebas son circulares** (`PruebasMejoras.cs:3432` y `:3436`): calculan el costo con la misma fórmula
    que el objetivo, así que nunca pueden ver esto. Tienen que medir contra la vara nueva.
- **Cerrar la app en ¡HAS MUERTO! conserva la oleada en curso** (baja hoy, media con anuncios). Pasa como decía la
  auditoría, pero solo con la oferta de revivir: hoy en la APK de prueba, que fuerza el proveedor Falso. La build de
  Play va en Nulo, y sin oferta `Terminar` corre en el mismo cuadro del golpe. No gasta ningún tope (un uso se anota
  solo si el vídeo premia) y la cuenta atrás se congela en recientes: es ilimitado. **Arreglo** (el de la auditoría,
  con cuidados): olvidar con `WaveManager.OlvidarPartidaSiEsOleadas()` (solo actúa en WaveMode, y el libre también
  ofrece revivir), anotar antes `OleadaEnCurso` y `PuntosEnCurso` para restaurarlos y guardar en `Revivir`, y hacerlo
  después de que `Ofrecer` devuelva verdadero. Costo que se acepta: si Android mata la app durante el vídeo, aunque se
  haya visto entero, se pierde la partida. Hacerlo antes de integrar la red de anuncios.
- **Las balas pueden atravesar al FASTER** (baja a 60 FPS, media a 20-30). No alcanza con el salto de la bala sola
  (0,37 m contra una ventana de contacto de ~0,6 m): lo que la supera es sumarle lo que avanza el zombi entre pasos de
  física (0,24 m). Tiros al cuerpo que lo cruzan sin tocarlo (modelo, sin medir): 0-2 % a 60 FPS, 0-5 % a 30 y 16-20 %
  a 20; contando los que rozan, 6-8, 11-15 y 23-26 %. Al rápido, 2 % a 60 y 9-12 % a 20. **Arreglo**: el barrido de la
  auditoría, pero en `FixedUpdate` y estirado ~0,24 m hacia atrás (en `Update` no cubre lo que avanza el zombi con 2 o
  3 pasos por cuadro), con `QueryTriggerInteraction.Ignore`, una máscara sin Player ni Bala y aceptando solo
  `EnemyController` (hoy las balas atraviesan las paredes, y un barrido las chocaría). `SphereCastNonAlloc` devuelve
  con distancia 0 lo que ya se superpone y no ordena. Quita también el empuje de la bala (sección 3). Para medirlo,
  `PruebaDisparo` necesita fijar los FPS y sacar zombis: a 20, 30 y 60.
- **`progreso.json` está en el almacenamiento externo** (baja). En Android, `persistentDataPath` es
  `getExternalFilesDir` (`/storage/emulated/0/Android/data/com.ivanruiz.showbies/files/`, siempre con minSdk 25). Se
  edita con un explorador de archivos en Android 7.1-10, con el truco del selector de carpetas en 11-12 y desde la PC
  (USB, adb) en 13 o más. No tiene datos personales: "privado" es una frase inexacta, no una fuga, y el interno
  tampoco frenaría las trampas. **Lo urgente es el texto de la política**, que vive en tres lugares
  (`publicacion/privacidad.html`, el repo `showbies-privacidad` y la rama `gh-pages`, que es la enlazada en Play):
  sacar "privado", y la de `gh-pages` dice además "never leaves your phone", que es falso con la copia de seguridad
  automática de Android (prendida: la APK no declara `allowBackup`). La mudanza al interno puede esperar a las tablas
  o la nube. Si se hace: por JNI, moviendo todas las copias (`.tmp`, `.anterior`, `.roto` y los `.bak`), sin migrar si
  hubo `NoSePudoLeer` o una versión futura, borrando el externo después de verificar el interno, y para siempre
  (restaurar una copia vieja de Android lo devuelve al externo).

### Rendimiento: hacerlo en Unity y medirlo en el teléfono

Aprobado por el revisor, pero son materiales, prefabs o ajustes del proyecto, y conviene ver el antes y el después con
el Profiler o el Frame Debugger (oleada 10+, 35 zombis):

- **El contacto de los zombis** (lo aprobaron dos revisores): cada zombi recibe un `OnCollisionStay` por paso de física
  por el piso y por cada vecino, solo para encontrar al jugador. **Arreglo**: que lo avise el jugador
  (`PlayerController.OnCollisionEnter/Stay` → `zombi.TocarAlJugador()`) y sacar los de `EnemyController`. Toca el
  camino del golpe: correr **Golpe animado**, **Derrota encima de la partida** y **Grabar al jefe** antes y después.
- **Las balas**: sin sombra (`Bullet.prefab`) y con GPU Instancing (`Bullet.mat`): hoy cada bala son dos draw calls, y
  con la caja de arma y la furia hay cien en pantalla.
- **Los zombis**: GPU Instancing en `TT_demo.mat` y las cuatro `Zombi*Piel.mat` (la cabeza es un draw call aparte),
  mipmaps en las manchas de sangre, y probar el Dynamic Batching en Android (quedárselo solo si baja el tiempo de
  cuadro).
- **El piso**: sin Specular Highlights ni Reflections en los tres materiales (por el inspector: tocar el float del
  YAML no prende la keyword) y sin proyectar sombra. Mostrarle a Ivan el antes y el después.
- **Android**: Blit Type en Auto y la pre-rotación de Vulkan prendida; probar en dos o tres teléfonos, girándolos en
  plena partida.
- **Los Animators de los zombis** (Humanoid, ~27 huesos como GameObjects): si pesan más de ~2 ms por cuadro, Optimize
  Game Objects exponiendo `HEAD_CONTAINER`.
- **Detrás de la tienda abierta** la cámara ya no dibuja, pero `FondoMenu` sigue sacando zombis y `MonedasDelFondo`
  sigue rearmando el canvas del menú en cada cuadro. Pausarlos con la tienda abierta si el Profiler lo muestra.
- **La cámara repite posición 1 de cada 6 cuadros** (media; confirmado en la ronda `refutar:`). Física a 1/50
  (`TimeManager.asset:6`), `m_Interpolate: 0` en el jugador y en los cinco zombis, el jugador se mueve con
  `linearVelocity` en `FixedUpdate` y `CamaraJugador` copia su posición en `Update` sin suavizar: a 60 FPS el piso
  avanza 24, 24, 24, 24, 24 y 0 px, un tirón diez veces por segundo. **Arreglo recomendado**: `Fixed Timestep` a 1/60,
  una línea, liso a 60 y a 30 FPS. Suma un 20 % a la física (medirlo en el teléfono con 35 zombis) y
  `recuperacionDelAplastado` termina un 20 % antes. Interpolar arregla además los monitores de 90 a 144 Hz (en Windows
  va con vSync), pero pisa las escrituras al Transform de los zombis (el `LookAt`, el festejo, la subida al aparecer,
  el deslizamiento del cadáver) y atrasa la puntería.
- **El decorado del capítulo siguiente se arma en plena pelea** (encontrado en la ronda `refutar:`).
  `PrepararElSiguiente` espera 6 s desde que empieza el capítulo: en la oleada 1 cae a los ~3 s de pelea. Y `Armar`
  instancia el prefab prendido y lo apaga después (`CapitulosDeEscenario.cs:290-291`): 733 objetos el cementerio y
  1.218 la ciudad, con sus luces y sus textos. En el editor costaba 5-13 ms. **Arreglo**: `Object.InstantiateAsync`
  (Unity 6) debajo de un padre apagado, que reparte el trabajo en varios cuadros; o, más simple, armarlo durante un
  cartel de oleada, sin zombis vivos. Es la mejora 5 de `REVISION.md`.
- **Los pools se llenan en plena pelea** (baja; parcial en la ronda `refutar:`). Ninguno se precalienta, pero lo que
  crean es chico y llega repartido (un zombi cada 0,35 s, fuera de la vista). Lo que podría notarse: el primer zombi
  de cada tipo, las invocaciones del jefe (hasta 6 `Aparecer` en un cuadro si no hay normales en la pila) y **los
  shaders**, que es lo nuevo: sin `ShaderVariantCollection` ni shaders precargados, el destello, los números de daño,
  las barras y las partículas se compilan en el primer golpe o la primera muerte, sobre todo en la primera partida
  después de instalar. Un objeto apagado no calienta shaders, así que precalentar los pools no lo arregla. Medir con
  el Profiler (Development Build) los primeros 15 s de oleadas y los primeros 35 s del libre, en la primera partida
  después de instalar y en la segunda.

## 3. Para decidir

### Balance (lo que más pesa)

- **El muro es un precipicio y lo causan los topes de cadencia (16) y críticos (8)** (alta). Simulado en seis
  calibraciones: con las dos mejoras al tope (hacia la oleada 33-40) solo queda el daño a +1 por nivel y ×1,45 de
  precio; la 30 cuesta ~1 partida por oleada y la 35, ~150. **Propuesta**: hitos multiplicativos solo para el daño
  (`Mejora.hitoCada` 5 y `hitoMultiplicador` 1,5, sin migrar el progreso), con la tarjeta avisando el próximo hito.
  Subir los topes solo corre el muro 3-5 oleadas. Ya está en TAREAS ("muro de balance"); esto es la causa medida.
- **La oleada del jefe es un salto de vida**: la 10 tiene 3,4× la vida de la 9, y es el primer logro. **Propuesta**:
  vida del BOSS 500 → 300 (sin código), o un `crecimientoVidaJefe` propio.
- **El modo libre no es granja: paga entre un cuarto y la mitad que las oleadas por minuto**, y al desbloquearlo en la
  12 su nivel 1 ya pesa como la oleada 12. **Propuesta**: alinear sus crecimientos con el parche del 19/9 (vida 1,11 y
  monedas 1,08 en `ShowBies1.unity`) y una prueba que compare las dos escenas.
- **El oro de SUPERVIVIENTE (completar la 50) queda detrás del muro** (45-48): bajarlo a 40-45 hasta que exista el
  renacer.
- **Con el techo de monedas lleno, la que se va para hacer lugar se va con su valor** (decidido así al aplicar el techo
  duro): pasárselo a la nueva rescataba lo que vence sin que nadie lo junte, un 10-30 % más de lo cobrado con el techo
  lleno. Si se prefiere que no se pierda nada, es una línea en `Moneda.Soltar`.
- **Los niveles baratos de los probadores** (ver la migración a v6 en la sección 2): un progreso v3 de la oleada 35
  arranca en el nivel 1 y cobra ~49.600 monedas en su primera partida y ~370.000 en 40 partidas. **Propuesta**:
  sembrarle la experiencia desde la mejor oleada, con `nivelPremiado` en ese nivel, como pretende la migración de los
  v4 y v5. O regalárselo por probar el juego.

### Diseño y retención

- **Punto de control por capítulo en las oleadas** (alta). Morir en la 38 vuelve a la 1 (~20 min de oleadas que no
  exigen nada), y pausa → MENÚ → OLEADAS es un revivir gratis que le va a ganar al video de revivir. Propuesta: al
  morir con la oleada > 10, guardar el principio del capítulo. Contradice "se olvida al morir" y toca `PlayerHealth`.
- **Notificaciones locales** (alta): la racha, las misiones, el semanal y los premios de nivel no avisan. Falta
  `com.unity.mobile.notifications`. Ya estaba en `REVISION.md`.
- **Guardado en la nube** (media): perder el progreso al cambiar de teléfono es causa típica de reseñas de una
  estrella. Saved Games de Play Games por JNI, como `PedidoDeResena`, junto con los logros de Play Games.
- **Contenido de combate después de la oleada 10** (media): enemigos y jefes nuevos por capítulo.
- **Cartas 1 de 3 por oleada, versión mínima** (media).
- **Misiones siempre iguales al principio** (media): sin granada, furia ni críticos, las tres son matar, oleadas y
  monedas. Propuesta: "mata N de un tipo" (rápidos, tanques) con los pesos de WaveMode.
- **La mejora de cadencia vacía el cargador más rápido** (media): el jugador nuevo se queda sin balas hacia la 4-5.
- **Oleadas con evento** (baja) y **tablas de Google Play Games** (media).
- **El aturdimiento del jefe no castiga nada en un shooter a distancia**: que reciba daño ×1,5 mientras está aturdido
  (sin usar el camino de los críticos, que inflaría la misión).
- **Las cajas nacen en cualquier punto del mapa**: sortearlas en un anillo de 10-22 m alrededor del jugador.
- **La moneda no muestra lo que vale**: un "+N" junto al contador del HUD mientras dura la escalera.
- **Detrás de la derrota los generadores siguen sacando zombis, jefes y cajas**: esperar mientras el jugador esté
  muerto (no cortar: puede revivir). ¿Es parte del festejo o sobra?
- **PLAY y SALIR quedan a 20 unidades en 20:9 y 21:9**: angostar MEJORAS y SALIR a 560, o esconder SALIR en el
  teléfono (el atrás ya pregunta si salir).
- **Los segundos de gracia después de revivir no se ven**: que el modelo parpadee (toca `PlayerHealth`).
- **¿Una partida abandonada cuenta para el récord?** (baja; parcial en la ronda `refutar:`). En el libre la partida se
  pierde sin compararla con MENÚ, REINICIAR, la R y cerrando la app; en oleadas, solo con REINICIAR y la R (MENÚ la
  guarda para retomarla). El arreglo de la auditoría empeora las oleadas: guardar el récord en `IrAlMenu` haría que al
  retomar no salga NEW BEST. Si se decide que sí cuenta: `GuardarRecord` en `MenuPausa.Pausar` y en la R para el libre
  (sus puntos solo suben), y en oleadas dentro de `OlvidarPartidaSiEsOleadas`. El tutorial no escribe récord.

### Anuncios

- **Un 3-2-1 al volver del video de revivir**: hoy el juego arranca en el acto y la gracia se gasta buscando los
  joysticks después de tocar la X del anuncio.
- **La oferta del x2 en la derrota tapa el dato que más la vende**: "VER VÍDEO: +60 MONEDAS · ¡1 MEJORA MÁS!".
- **Más lugares de video**: monedas en la tarjeta de la tienda que dice "faltan N", cambiar una misión, x2 del cofre.
  La palanca real es el tope diario (no tocarlo sin datos).
- **Medir el embudo de los videos**: ofrecido, aceptado, premiado, cerrado, no disponible y falla, por lugar.

### Combate

- **Una caja de balas durante la de arma baja la cadencia de x3 a x1,5**. `PotenciarCadencia` pisa sin comparar, y
  CLAUDE.md lo documenta así. Propuesta: conservar la mejora activa más fuerte (la caja igual recarga).
- **Zona muerta en el joystick de disparo**: cualquier roce dispara hacia un lado al azar. Probar en el teléfono.
- **En PC las balas no pasan por la mira**: el rayo del mouse corta Y=0 y la bala sale de la mano.
- **Balas a 11 m/s**, más lentas que el FASTER: 22-25 m/s con `lifeTime` 1 s, pero junto con el barrido.
- **El empuje de la bala** (media; confirmado en la ronda `refutar:`, y más fuerte de lo que se creía). La bala es un
  collider sin Rigidbody que aparece adentro del zombi, y PhysX lo saca empujándolo
  (`m_DefaultMaxDepenetrationVelocity` sin tope): cada bala lo corre ~10-14 cm y le hace perder su paso. Con fuego
  sostenido (modelo, sin medir): el normal va al 85 % de su velocidad con 4 tiros/s, al 50-63 % con 12 y al ~10 % con
  20; el tanque retrocede con 20; el jefe persiguiendo va al 22-39 % con 8, y su carga cubre el 72-75 % de la línea
  con 12. En el tanque y el jefe depende de los FPS. El juego se balanceó con esto sin saberlo, y el barrido de las
  balas (sección 2) o `IsTrigger` lo quitan. **Decidir**: sacarlo (y rebalancear), o dejarlo a propósito y parejo, en
  `DanoZombi`, quizá con resistencia por tipo. Si la bala pasa a trigger, su `OnCollisionEnter` pasa a
  `OnTriggerEnter`.

### Textos y plataforma

- **"Coge"/"Cógela"**: el CLAUDE.md fija español de España con "coger", pero hay una ficha de Play para
  Latinoamérica, donde "coger" es grosero. Propuesta: "recoge". Es decisión de Ivan.
- **El primer arranque es siempre en inglés** aunque hay dos fichas en español: arrancar en español si el teléfono
  está en español (sin guardarlo). Decisión documentada; hacerlo junto con lo de "coger".
- **Solo ARM64**: quedan afuera los Android de 32 bits. Mirar primero en Play Console cuántos se excluyen.
- **Más código muerto de la tarjeta de la tienda**: `borde`, `franja`, `icono`, `barraNivel`, `estampa` y sus hijos
  apagados. Pide correr `ConstructorTienda` en Unity.

## 4. Para mirar en Unity

Todo lo de la auditoría se aplicó sin Unity: compila (con el chequeo de referencias) pero nada corrió.

- **ShowBies > Pruebas > Logica de mejoras**: hay pruebas nuevas en casi todos los frentes.
- **El jefe**: el paso de las piernas al embestir salió 2,2 (medido del clip: su raíz avanza 2 m por ciclo) y los otros
  zombis patinan entre 1,5 y 6 veces; si se ve frenético al lado de ellos, se sube `velocidadDelClipDeCorrer`. Y
  **solo ataca con el jefe en pantalla** (para que se lea el aviso): con la cámara de WaveMode eso es ~4,7 m detrás
  del jugador, ~6 m delante y 9-11,5 m al costado, contra los 15 m de radio de antes; se ajusta con
  `margenEnPantalla`. Mirarlo con **Grabar al jefe**.
- **La tienda**: el toque que frena la fila es un `MonoBehaviour` anidado agregado con `AddComponent`
  (`TarjetaMejora.ToqueQueFrena`), como el cartel del anuncio falso; si molesta, va a su propio archivo.
- **El techo de monedas duro**: que no se note la moneda vieja que se va cuando cae la lluvia del jefe.
- **El menú en 20:9**: las ventanas de misiones y logros se ven un 8 % más chicas, para que entre el halo.
- **720p nativo en el teléfono** (antes quedaba en 540): medir los FPS en la oleada 10+ con 35 zombis; si no llega a
  60, bajar `AltoMinimoMovil`.
- **El tutorial**: tirar la granada al grupo y afuera (el paso cambia después de la explosión, los muertos se caen y
  los que quedan se van con partículas); en el paso 4 agarrar la caja de balas (sigue "Cógela" hasta agarrar la de
  arma); y correr contra una pared antes de los pasos 3, 4 y 5 (todo tiene que nacer del lado de adentro).
- **La diaria antes que la tienda**: derrota → MEJORAS con la diaria disponible, cobrando, con vídeo y cerrándola con
  el atrás. La tienda (y la flecha de la primera compra) tienen que llegar después; sin diaria, abre como siempre.
- **El desbloqueo del libre**: completar la oleada 11 (con **ShowBies > Progreso**) y ver el aviso en la partida y el
  "¡NUEVO!" en el panel de modos, también cambiando de idioma.
- **La horda**: el festejo con una muerte contra la pared oeste (**Derrota encima de la partida**), las barras de vida
  cruzadas en una horda apretada, y las manchas de sangre, que ahora van a 0,2 m (para no quedar debajo de las
  veredas de la ciudad) y tapan unos 20 cm de lo que las pisa durante sus 2 s.
- **Las medallas de logros**: cada familia con su símbolo de una o dos letras, que entre en el círculo.
- **La build**: ahora se niega con un paquete, un nombre o un orden de escenas que no son los de Play, y por
  línea de comandos sale con código 1 si falla. Hacer una APK y un AAB para ver que sigan saliendo.
- **El sonido, en el teléfono**:
  - que las granadas no crujan sobre un grupo ni sobre un tanque (la que mata suena ~2,6 dB más baja: ver si se
    siente débil), y que la horda densa y los arpegios de la tienda no se sientan apagados (bajan ~1 y ~3 dB);
  - los clics de todo el menú y de la pausa, y el control de EFECTOS, que ahora suena al arrastrarlo;
  - el disparo, que subió ~27 dB (estaba casi mudo): que no canse a 20 tiros por segundo;
  - la muerte del jugador (golpe grave, temblor y borde rojo), las notas de furia lista y de fin de la furia, la
    muerte del jefe (que ya no se pierde), un solo jingle al pasar a la oleada 11 y la escalera de la barra del nivel;
  - la música del menú, que ahora arranca unos cuadros tarde: medir cuánto tarda `LoadScene(0)` antes y después.
- **El menú y la tienda**: que abrir y cerrar la tienda no parpadee ni deje un cuadro de color liso (con VOLVER, el
  atrás, ¡A JUGAR! y MEJORAS desde la derrota), y que el menú se vea igual sin HDR (el título en la niebla, la
  noche). En el Profiler, durante una caja de arma, `TextMeshPro.GenerateTextMesh` ya no tendría que aparecer por
  cada número de daño que se desvanece.

## 5. Antes de integrar la red de anuncios

La lista está en `publicacion/pasos.md`: clasificación máxima de contenido, consentimiento (UMP) con botón de
privacidad, ids de bloque y de prueba, la declaración del ID de publicidad, app-ads.txt, inicializar y precargar al
arrancar, volver a preguntar `Listo` mientras las ofertas están abiertas, un vigía por si el SDK no avisa nunca, y que
el proveedor dé un solo resultado final.

## 6. Descartados

- **El premio se pierde si el aviso de premio llega después del de cierre** (anuncios). Hoy ningún proveedor manda
  dos resultados distintos. Queda como contrato del proveedor real (ver la lista de `pasos.md`).
- **Sin timeout alrededor del proveedor** (anuncios). Con Nulo y Falso no puede colgarse; el try/catch ya está; el
  vencimiento se decide con el SDK a la vista.
- **Con una red real, `Listo` da falso justo después de cerrar un video**. Hoy Falso siempre está listo; queda en la
  lista de `pasos.md`.
- **El modo libre da el doble de experiencia por minuto**. Es cierto, pero no da ventaja económica.
- **La derrota suena con `pedo.mp3`**. Refutado.
- **Morir por el kill-Z** y **el kill-Z en el tutorial** (refutados en la ronda `refutar:`, por dos escépticos). El
  jugador tiene congelada la posición Y en `Jugador.prefab` (`m_Constraints: 116`, desde noviembre de 2022) y ninguna
  escena ni script lo cambia; además `FixedUpdate` le pone la velocidad vertical en 0 y las paredes (1,17 m) son más
  gruesas que lo que avanza por paso (0,39 m con la furia). El kill-Z del jugador es código muerto. Si algún día se
  saca ese congelado: `Caer()` antes de `Terminar` y, en el tutorial, devolverlo al inicio. Queda algo chico: una
  línea en la prueba de lógica que verifique el 116, y corregir el comentario de `PlayerHealth.cs:51-52` y el
  CLAUDE.md ("Caer al vacío no se revive", y que el kill-Z "no es decorativo", que para el jugador sí lo es).
