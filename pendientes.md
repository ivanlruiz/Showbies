# Pendientes de la auditoría del 24/9

La auditoría tuvo 20 frentes. En cada uno, un agente `buscar:` salió a buscar errores, riesgos y mejoras, y un
`mejoras:` revisó las mejoras contra el código. **La ronda `refutar:`** (un escéptico que intenta tumbar cada error
antes de arreglarlo) **quedó para después**, por pedido de Ivan: solo pasaron por ella los frentes de calidad y de
diseño. **Lo chico y seguro ya está aplicado**, y quien lo aplicó lo verificó leyendo el código (los commits "Aplicar
... de la auditoria" y los que siguen). Este archivo se llamaba `AUDITORIA.md`. Acá queda:

1. **Sin verificar**: errores y riesgos que encontró un `buscar:` y que nadie refutó ni aplicó. Son la ronda
   `refutar:` que falta: antes de arreglar uno, confirmar que pasa.
2. **Verificados, para después**: confirmados leyendo el código, pero grandes, en archivos de la fase 2 (que está sin
   commit en la PC de Ivan: `PlayerHealth.cs` y el constructor de neón) o con algo para decidir.
3. **Para decidir**: mejoras y cambios de diseño que valen la pena pero cambian el juego o una decisión de Ivan.
4. **Para mirar en Unity**: lo que se aplicó sin poder probarlo.
5. **Antes de integrar la red de anuncios**: ver `publicacion/pasos.md`.
6. **Descartados**: lo que se refutó (no pasa hoy), con la recomendación para cuando cambie algo.

---

## 1. Sin verificar (la ronda `refutar:` que falta)

### Premios que se farmean

- **Los jefes de las misiones, del semanal y del bestiario se farmean** (alta; lo encontraron dos auditores). El modo
  libre saca un jefe cada 30 s con la vida del nivel 1 (y la R lo vuelve al nivel 1), y retomar una oleada de jefe
  (pausa → MENÚ → OLEADAS) lo vuelve a sacar aunque ya se lo haya matado. Con mejor oleada 40, la misión "derrota 10
  jefes" (cotizada en 2,5 partidas) sale en ~5 min, y el semanal de 60 jefes (~257.000 monedas, cotizado en 15
  partidas) en ~30 min. **Arreglo**: un contador aparte de jefes de oleada (`EsJefeDeOleada` puesto en
  `WaveManager.Aparecer`) que lean `MisionesDiarias` y `DesafioSemanal`; y anotar con la oleada en curso si su jefe ya
  murió, para no volver a sacarlo al retomar. Es un campo nuevo en el progreso (subir la versión, como con los
  contadores de la v4).
- **La migración a v6 devuelve de golpe los premios que quería evitar** (media). `MigrarAlNivel` no da premios por
  los niveles viejos, pero no marca los logros que ese progreso ya cumple: el primer `Logros.Revisar` los anota todos
  al nivel migrado (alto) y al cobrarlos cruzan ~17 niveles con premio. Un probador de la oleada 35 cobra ~76.000
  monedas al entrar; uno de la 45, más de 250.000. Le toca a todos los de la prueba cerrada con el próximo AAB.
  **Arreglo**: marcar en la migración que el progreso viene de v5 y, en el primer `Revisar` con esa marca, anotar las
  ganadas como cobradas, sumar su experiencia y subir `nivelPremiado` sin crear `PremioDeNivel`. O decidir que sí se
  premian y documentarlo.
- **Guardar las monedas de logro sin cobrar rinde más** (media). La experiencia se congela al ganar la moneda, pero
  los niveles que cruza al cobrarla pagan con la mejor oleada de ese momento: guardarla de la 30 a la 45 da ~3× las
  monedas. **Arreglo**: anotar la oleada al ganar (`oleadaAlGanar`, −1 de centinela) y pagar con
  `min(MejorOleada, oleadaAlGanar)`; o sumar la experiencia en el acto y dejar COBRAR solo como festejo.
- **`'Gana N monedas'` se mide con un modelo que se queda corto** (media; misión y semanal, 1,3-2,6×, y 6× con el botín
  al máximo): con botín alto el semanal de monedas sale en ~2,5 partidas en vez de 15. **Arreglo**: una vara "de más"
  para los objetivos de monedas (la suma real por oleada con el botín comprado) y dejar `MonedasPorPartida` para los
  premios.
- **La misión de críticos es más cara cuanto más crítico se tiene** (baja): dividir por `1 + p`.
- **El bestiario es el único premio sin congelar** (baja): guardar la oleada al ganar cada estrella.

### En `PlayerHealth.cs` (fase 2)

- **Cerrar la app en ¡HAS MUERTO! conserva la oleada en curso: es un revivir gratis y sin límite** (media; lo
  encontraron dos auditores). Con la oferta abierta, `Terminar` todavía no corrió y el archivo ya tiene la oleada
  guardada. **Arreglo**: olvidar la oleada y guardar justo antes de ofrecer revivir, y volver a guardarla en `Revivir`.
- **Morir por el kill-Z**: sin caída y con la cámara bajo el piso detrás de la derrota, y la horda festeja fuera del
  mapa. **Arreglo**: `Caer()` antes de `Terminar` y que la cámara no siga la Y bajo el piso.
- **Kill-Z en el tutorial**: sale GAME OVER, cuenta la partida (se pierde la guía de la primera partida) y
  `UltimoModo` queda en 4. **Arreglo**: en el tutorial, devolver al jugador a su posición inicial.
- **El récord solo se escribe al morir**: REINICIAR, la R y MENÚ tiran la partida sin compararla. **Arreglo**:
  `GuardarRecord` accesible y llamado antes de cargar en `MenuPausa.Reiniciar`, `IrAlMenu` y la R.

### Combate

- **Las balas pueden atravesar al FASTER sin tocarlo** (media). La bala avanza por `Translate` y el choque es
  discreto: a 30 FPS salta ~0,37 m por cuadro contra un zombi de 0,44 m que viene a 12 m/s. **Arreglo**: barrido con
  `Physics.SphereCastNonAlloc` entre la posición anterior y la nueva, y sacar el collider de la bala. Medirlo antes
  con `PruebaDisparo` a 20 y 30 FPS.
- **Cada bala empuja al zombi que toca** (hipótesis). La bala no es trigger y aparece de golpe adentro: PhysX lo
  separa empujándolo, más a menos FPS. Con el barrido se va solo; si no, `IsTrigger` en `Bullet.prefab`.

### Tutorial

- **Los zombis del paso de la granada desaparecen al tirarla y la granada explota sobre nada** (media). El paso
  termina en el cuadro en que existe un `Granade` (`TutorialManager.cs:67`) y el siguiente destruye a los zombis con un
  `Destroy` seco, 0,8 s antes de que la granada caiga. **Arreglo**: pasar de paso cuando la granada ya explotó, con
  ~1 s para ver los cadáveres, y sacar a los que sobrevivan con partículas.
- **Agarrar la caja de balas saltea la de arma** (media). El paso Arma se da por cumplido con `theGun.MejoraActiva`
  (`TutorialManager.cs:79`), que prende cualquier caja: con la de balas del paso anterior, el texto salta al del reloj
  ("el cargador más grande se queda para siempre", falso para esa caja) y la de arma se destruye sin tocarla.
  **Arreglo**: que el paso espere a que desaparezca su caja, como el de las cajas.
- **Cajas y zombis del tutorial nacen a distancias fijas del jugador sin mirar las paredes** (baja): pegado a la pared
  norte, la caja de arma nace del otro lado y el tutorial se traba. **Arreglo**: acotar el punto a ±44 en `Spawnear`.

### Sonido

- **Una granada que mata satura la salida** (media): la explosión suena casi a 0 dBFS y en el mismo cuadro se le suman
  el golpe y la muerte de cada zombi (+4 a +6,5 dBFS, medido mezclando los clips reales). **Arreglo**: un limitador
  simple en `Sonidos` (una carga que decae y escala lo nuevo) o bajar los eventos fuertes.
- **En el menú solo MEJORAS hace clic** (media): PLAY, los modos, SALIR y los botones de las ventanas armadas en código
  tienen `sonidoClick` vacío. La pausa tampoco suena (y con `AudioListener.pause` haría falta una fuente que lo
  ignore).
- **La muerte del tanque o del jefe puede quedarse sin sonido** (media): comparte con las muertes chicas la separación
  de 40 ms por clip. Lo mismo el crítico. **Arreglo**: que la muerte grande no pase por la separación.
- **`cartel.wav` suena dos veces al cambiar de capítulo** (baja): lo toca la oleada y un cuadro después el capítulo;
  en el teléfono quedan a más de 30 ms, un eco. Además anuncia 14 cosas distintas, el ¡HAS MUERTO! incluido.
- **La barra del nivel toca siempre la misma nota** al cruzar cada nivel (baja; `VentanaLogros.cs:345`): cuenta los
  niveles cruzados en ese cuadro, no desde que empezó a llenarse.

### Zombis

- **Festejo: los lugares detrás de las paredes no se alcanzan** (baja) y esos zombis corren en el lugar para siempre,
  de espaldas al cuerpo. **Arreglo**: si en ~0,5 s no se acercó, festejar donde está.
- **En la ciudad las manchas de sangre quedan tapadas por las veredas** (baja): van a 0,1 m y la vereda mide 0,14.
- **El cadáver del tanque sale despedido igual que el de un normal** (baja): `empujeAlMorir / Max(1, escala)` solo
  frena al jefe; el CLAUDE.md dice que el tanque casi no se mueve. **Arreglo**: dividir por la escala relativa al
  normal (0,5).
- **Un zombi que muere en el aire queda flotando** (baja): el cadáver pasa a kinematic donde está.
- **Barras de vida superpuestas** (baja): todos los fondos se dibujan antes que todos los rellenos. **Arreglo**: un
  `SortingGroup` por barra.
- **Oleada del jefe: el jefe y el primer zombi salen en el mismo cuadro** (baja; confianza baja), a veces en el mismo
  punto. **Arreglo**: esperar 1-2 s después del jefe.

### Rendimiento

- **La cámara repite posición 1 de cada 6 cuadros** (media): física a 50 Hz, juego a 60 FPS y nada interpolado; se ve
  como un tironeo del mundo aunque el contador diga 60. **El arreglo es para decidir**: `Fixed Timestep` a 1/60 (≈20 %
  más pasos de física) o interpolar los Rigidbody (hay que girar con `MoveRotation`). Medirlo en el teléfono.
- **Los pools se llenan en plena pelea** (baja): zombis, monedas, números, barras, partículas y letras de la fuente se
  crean cuando hacen falta. **Arreglo**: precalentarlos durante el cartel de la oleada 1.

### Otros

- **`progreso.json` probablemente vive en el almacenamiento externo de la app** y la política de privacidad dice
  "privado". En un Android 9 se edita con un explorador de archivos. **Primero medir**: loguear
  `Application.persistentDataPath` en la APK de prueba. Si es externo, migrar a interno una vez y ajustar la frase de
  `publicacion/privacidad.html` (y de la rama `gh-pages`, que es la publicada).

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
