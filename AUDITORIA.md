# Auditoría del 24/9 — pendientes

Auditoría en 20 frentes: cada auditor buscó errores, riesgos y mejoras; una parte de los errores la revisó un
escéptico que intentó refutarlos (después Ivan pidió seguir sin escépticos: esos los verificó el que los aplicó,
leyendo el código). **Lo chico y seguro ya está aplicado** (ver los commits "Aplicar ... de la auditoria"). Acá queda
lo demás:

1. **Errores para arreglar después**: confirmados, pero grandes o en archivos de la fase 2 (que está sin commit en la
   PC de Ivan: `PlayerHealth.cs` y el constructor de neón).
2. **Para decidir**: mejoras y cambios de diseño que valen la pena pero cambian el juego o una decisión de Ivan.
3. **Antes de integrar la red de anuncios**: ver `publicacion/pasos.md`.
4. **Descartados**: lo que se refutó (no pasa hoy), con la recomendación para cuando cambie algo.

---

## 1. Errores para arreglar después

### Los más importantes

- **Los jefes de las misiones, del semanal y del bestiario se farmean** (alta). El modo libre saca un jefe cada 30 s
  con la vida del nivel 1 (y la R lo vuelve al nivel 1), y retomar una oleada de jefe (pausa → MENÚ → OLEADAS) lo
  vuelve a sacar aunque ya se lo haya matado. Con mejor oleada 40, la misión "derrota 10 jefes" (cotizada en 2,5
  partidas) sale en ~5 min, y el semanal de 60 jefes (~257.000 monedas, cotizado en 15 partidas) en ~30 min.
  **Arreglo**: un contador aparte de jefes de oleada (`EsJefeDeOleada` puesto en `WaveManager.Aparecer`) que lean
  `MisionesDiarias` y `DesafioSemanal`; y anotar con la oleada en curso si su jefe ya murió, para no volver a
  sacarlo al retomar. Es un campo nuevo en el progreso (subir la versión, como con los contadores de la v4).
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

### En archivos de la fase 2 (`PlayerHealth.cs`)

- **Cerrar la app en ¡HAS MUERTO! conserva la oleada en curso: es un revivir gratis y sin límite** (media). Con la
  oferta abierta, `Terminar` todavía no corrió y el archivo ya tiene la oleada guardada. **Arreglo**: olvidar la
  oleada y guardar justo antes de ofrecer revivir, y volver a guardarla en `Revivir`.
- **Morir por el kill-Z**: sin caída y con la cámara bajo el piso detrás de la derrota, y la horda festeja fuera del
  mapa. **Arreglo**: `Caer()` antes de `Terminar` y que la cámara no siga la Y bajo el piso.
- **Kill-Z en el tutorial**: sale GAME OVER, cuenta la partida (se pierde la guía de la primera partida) y
  `UltimoModo` queda en 4. **Arreglo**: en el tutorial, devolver al jugador a su posición inicial.
- **El récord solo se escribe al morir**: REINICIAR, la R y MENÚ tiran la partida sin compararla. **Arreglo**:
  `GuardarRecord` accesible y llamado antes de cargar en `MenuPausa.Reiniciar`, `IrAlMenu` y la R.
- **La caja de vida se consume con la vida llena**. **Arreglo**: no tomarla con la vida llena (salvo en el tutorial)
  y tomarla con `OnTriggerStay` cuando baje.
- **Prueba de `ColorDeVida`**: es estática "para probarla" y ninguna prueba la llama (esperar a los colores de la
  fase 2).

### Combate (sin verificar en Unity)

- **Las balas pueden atravesar al FASTER sin tocarlo** (media). La bala avanza por `Translate` y el choque es
  discreto: a 30 FPS salta ~0,37 m por cuadro contra un zombi de 0,44 m que viene a 12 m/s. **Arreglo**: barrido con
  `Physics.SphereCastNonAlloc` entre la posición anterior y la nueva, y sacar el collider de la bala. Medirlo antes
  con `PruebaDisparo` a 20 y 30 FPS.
- **Cada bala empuja al zombi que toca** (hipótesis). La bala no es trigger y aparece de golpe adentro: PhysX lo
  separa empujándolo, más a menos FPS. Con el barrido se va solo; si no, `IsTrigger` en `Bullet.prefab`.

### Otros

- **`progreso.json` probablemente vive en el almacenamiento externo de la app** y la política de privacidad dice
  "privado". En un Android 9 se edita con un explorador de archivos. **Primero medir**: loguear
  `Application.persistentDataPath` en la APK de prueba. Si es externo, migrar a interno una vez y ajustar la frase de
  `publicacion/privacidad.html` (y de la rama `gh-pages`, que es la publicada).
- **`'Gana N monedas'` se mide con un modelo que se queda corto** (misión y semanal, 1,3-2,6×, y 6× con el botín al
  máximo): con botín alto el semanal de monedas sale en ~2,5 partidas en vez de 15. **Arreglo**: una vara "de más"
  para los objetivos de monedas (la suma real por oleada con el botín comprado) y dejar `MonedasPorPartida` para los
  premios.
- **La misión de críticos es más cara cuanto más crítico se tiene** (baja): dividir por `1 + p`.
- **El bestiario es el único premio sin congelar** (baja): guardar la oleada al ganar cada estrella.

## 2. Para decidir

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

## 3. Antes de integrar la red de anuncios

La lista está en `publicacion/pasos.md`: clasificación máxima de contenido, consentimiento (UMP) con botón de
privacidad, ids de bloque y de prueba, la declaración del ID de publicidad, app-ads.txt, inicializar y precargar al
arrancar, volver a preguntar `Listo` mientras las ofertas están abiertas, y que el proveedor dé un solo resultado
final.

## 4. Descartados

- **El premio se pierde si el aviso de premio llega después del de cierre** (anuncios). Hoy ningún proveedor manda
  dos resultados distintos. Queda como contrato del proveedor real (ver la lista de `pasos.md`).
- **Sin timeout alrededor del proveedor** (anuncios). Con Nulo y Falso no puede colgarse; el try/catch es barato y va en esta
  ronda; el vencimiento se decide con el SDK a la vista.
- **Con una red real, `Listo` da falso justo después de cerrar un video**. Hoy Falso siempre está listo; queda en la
  lista de `pasos.md`.
- **El modo libre da el doble de experiencia por minuto**. Es cierto, pero no da ventaja económica.
- **Economía y Bestiario copian a mano el 1,08** (primera pasada): se refutó como error, pero la segunda pasada lo
  encontró como riesgo: va la constante con su prueba en esta ronda.
- **La derrota suena con `pedo.mp3`**. Refutado.
