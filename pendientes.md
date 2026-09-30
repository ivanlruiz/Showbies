# Pendientes de las auditorías (24/9 y superauditoría del 29/9)

La auditoría tuvo 20 frentes. En cada uno, un agente `buscar:` salió a buscar errores, riesgos y mejoras, y un
`mejoras:` revisó las mejoras contra el código. **La ronda `refutar:`** (un escéptico que intenta tumbar cada error
antes de arreglarlo) pasó al principio solo por los frentes de calidad y de diseño, por pedido de Ivan, y **el 26/9
por los quince hallazgos que quedaban** (ver la sección 1). **Lo chico y seguro ya está aplicado**, y quien lo aplicó
lo verificó leyendo el código (los commits "Aplicar ... de la auditoria" y los que siguen). Este archivo se llamaba
`AUDITORIA.md`. Acá queda:

1. **Las rondas `refutar:`**: la superauditoría del 29/9 y la ronda del 26/9, con el veredicto de cada hallazgo y
   adónde fue a parar.
2. **Verificados, para después**: confirmados leyendo el código, pero grandes, con algo para decidir o que esperaban
   la fase 2 (`PlayerHealth.cs` y el constructor de neón, que ya están en main).
3. **Para decidir**: mejoras y cambios de diseño que valen la pena pero cambian el juego o una decisión de Ivan.
4. **Para mirar en Unity**: lo que se aplicó sin poder probarlo.
5. **Antes de integrar la red de anuncios**: ver `publicacion/pasos.md`.
6. **Descartados**: lo que se refutó (no pasa hoy), con la recomendación para cuando cambie algo.
7. **Sin refutar**: los hallazgos bajos de la superauditoría del 29/9, una línea cada uno.

---

## 1. Las rondas `refutar:`

### La superauditoría del 29/9

La superauditoría del 29/9 tuvo 26 frentes (uno por sistema y los transversales: estado estático, tiempo y pausa,
Android, rendimiento, build y Play, trampas, pruebas, CLAUDE.md, herramientas y código muerto). Leyó HEAD (d17c32c)
sin Unity y miró con más cuidado lo que vino después de la auditoría del 24/9 (`e6556d9..HEAD`: el neón de la partida,
la noche, las píldoras, los arreglos del 27/9 y los bancos nuevos), que nunca se había auditado. De 162 hallazgos en
bruto quedaron 123 únicos; uno ya estaba anotado (las balas que atraviesan al FASTER). Los 23 de gravedad media o alta
pasaron por la ronda `refutar:` (dos escépticos para el único alto; con dos miradas, gana la más desfavorable si trae
evidencia concreta): 15 confirmados, 7 parciales y 1 refutado, y ninguno quedó alto. Los 100 bajos no se refutaron. Lo
marcado "sin medir" sale de un modelo: medirlo antes de arreglar. Lo nuevo del tramo sin auditar: los halos de neón
que roban toques (H01), el botón de pausa encima de los carteles (H13), la noche que apaga las balas y las cajas
(H17), la caja de arma del tutorial tapada (H21) y los bancos que se cuelgan o pisan escenas (H22, H23). H01, H05,
H06 y H17 se arreglaron el 29/9, probados con `PruebaTienda` y el banco nuevo `PruebaReiniciar`. Los cinco
que pedían una decisión (H03, H04, H08, H11 y H18) los decidió Ivan ese mismo día y pasaron a la sección 2; el paso
del tanque, que venía con H16, queda como está (sección 6).

| hallazgo | veredicto | gravedad | sección |
|---|---|---|---|
| H01: el halo de REINICIAR se lleva toques de CONTINUAR | parcial: todavía no está en Play | media (era alta) | arreglado el 29/9 |
| H02: la furia arranca lista en cada partida | parcial: adelanta, no multiplica | baja (era media) | 2 |
| H03: el reloj confiable solo se ancla al cobrar la diaria | confirmado | media | 2 |
| H04: retomar vuelve opcional la muerte en oleadas | parcial: en monedas no es granja | baja (era media) | 2 |
| H05: REINICIAR y la R sin confirmación | confirmado | media (baja con H01 arreglado) | arreglado el 29/9 |
| H06: la tienda compra con un toque de la diaria | confirmado | media | arreglado el 29/9 |
| H07: el versionCode 5 ya se usó | parcial: ya estaba en TAREAS | baja (era media) | 2 |
| H08: la librería de reseñas y Seguridad de los datos | parcial: la lectura queda abierta | baja (era media) | 2 |
| H09: Storage Location decide dónde vive `progreso.json` | parcial: no destruye, se revierte | baja (era media) | 2 |
| H10: la invocación del jefe sale vacía con el techo lleno | parcial: el arreglo propuesto no alcanza | media | 2 |
| H11: al jefe se lo mata sin que ataque | confirmado | media | 2 |
| H12: el borde rojo se corta en el área segura | confirmado | baja (era media) | 2 |
| H13: la pausa del teléfono tapa lo de arriba al centro | confirmado | media | 2 |
| H14: MODO LIBRE bloqueado con el halo verde | confirmado | baja (era media) | 2 |
| H15: en PC se corre un 41 % más rápido en diagonal | confirmado | baja (era media) | 2 |
| H16: los zombis patinan (el paso del tanque quedó como está, en la 6) | confirmado, y el tanque también | media | 2 |
| H17: de noche las balas y las cajas se ven oscuras | confirmado | media | arreglado el 29/9 |
| H18: los edificios de la ciudad tapan al jugador | confirmado | media | 2 |
| H19: el pitch acotado a 3 | refutado | — | 6 |
| H20: las veredas tapan la línea del jefe | confirmado, y la granada también | media | 2 |
| H21: la caja de arma del tutorial detrás del panel | confirmado | baja (era media) | 2 |
| H22: un banco cortado secuestra el próximo Play | confirmado | media | 2 |
| H23: los bancos abren escenas sin mirar si están sucias | confirmado | media | 2 |
| H24 a H123 (100 hallazgos bajos) | sin refutar | baja | sin refutar |

### La ronda del 26/9

Quince escépticos, uno por hallazgo, intentaron tumbar cada uno leyendo el código y los YAML de HEAD (7276891), sin
Unity, y recalcularon los números con los valores serializados. Quedaron 2 refutados, 6 confirmados y 7 parciales
(pasan, pero distinto o menos grave de lo que se había anotado). Lo confirmado pasó a la sección 2, con las tres
decisiones que tomó Ivan ese mismo día (el empuje de la bala, el récord de una partida abandonada y los niveles de los
probadores), y lo refutado a la 6. Lo marcado "sin medir" sale de un modelo: medirlo antes de arreglar. La ronda
encontró además cosas que no estaban anotadas: el decorado que se arma en plena pelea, los shaders que se compilan en
el primer golpe, los niveles baratos de los probadores, dos pruebas circulares y una frase falsa en la política de
privacidad publicada.

| hallazgo | veredicto | gravedad | sección |
|---|---|---|---|
| Los jefes se farmean | confirmado | media (era alta) | 2 |
| La migración a v6 | parcial: solo pasa con archivos v4 y v5 | baja (era media) | 2 |
| Las monedas de logro sin cobrar | confirmado | baja (era media) | 2 |
| "Gana N monedas" | parcial: la causa es otra | baja (era media) | 2 |
| La misión de críticos | parcial: el arreglo propuesto regala | baja | 2 |
| El bestiario sin congelar | confirmado | baja | 2 |
| Cerrar la app en ¡HAS MUERTO! | parcial: hoy solo en la APK de prueba | baja hoy, media con anuncios | 2 |
| Morir por el kill-Z | refutado | — | 6 |
| El kill-Z en el tutorial | refutado | — | 6 |
| El récord al reiniciar | parcial: el arreglo propuesto rompe las oleadas | baja | 2 |
| Las balas atraviesan al FASTER | parcial: no es cosa de los 30 FPS | baja a 60 FPS, media a 20-30 | 2 |
| El empuje de la bala | confirmado, y más fuerte | media | 2 |
| La cámara a 50 Hz | confirmado | media | 2 |
| Los pools en plena pelea | parcial: el tirón es otro | baja | 2 |
| `progreso.json` en el almacenamiento externo | confirmado | baja | 2 |

## 2. Verificados, para después

- **Una fecha guardada en el futuro bloquea la diaria, las misiones, el semanal y los vídeos hasta esa fecha** (media).
  Es real, pero el arreglo propuesto (topar las fechas guardadas en mañana) reabre la trampa del reloj atrasado:
  atrasar el reloj, abrir, volver a la fecha real, y se cobra todo otra vez, sin límite. **Versión segura**: topar solo
  con la hora automática de Android prendida (`Settings.Global` `auto_time`, por JNI en `RelojConfiable`). Pide un
  teléfono para probarlo. Con la hora de internet (H03, decidida el 29/9) se puede topar contra el día de internet,
  sin `auto_time`: hacerlo junto.
- **`PlayerHealth.instance` no está en ningún reset** (fase 2). **Arreglo**: `instance = null` en su reset y
  `OnDestroy { if (instance == this) instance = null; }`.
- **La caja de vida se consume con la vida llena** (fase 2). **Arreglo**: no tomarla con la vida llena (salvo en el
  tutorial) y tomarla con `OnTriggerStay` cuando baje.
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
- **El borde rojo del golpe que mata se corta enseguida** (baja; lo vio la revisión de la derrota del 27/9).
  `VinetaDanio` vive en el canvas del HUD, y `DerrotaEnLaPartida` apaga los canvas del juego apenas carga la derrota,
  unos 0,11 s después del golpe: el borde entero de `Efectos.MuerteJugador` casi no se ve, y con el proveedor Nulo
  todas las muertes van directo a la derrota. **Arreglo**: que la viñeta tenga su propio canvas y la derrota no lo
  apague hasta que se desvanezca (0,4 s).
- **Con el jefe encima del jugador, un invocado nace pegado a él** (baja; de la grabación del jefe del 27/9).
  `PuntoDelAnillo` esquiva las paredes pero no al jugador: con el jefe a 2 m y el anillo de 3,5 m, uno de los cuatro
  sale a 1,2 m. El anillo pasa por detrás del jugador durante el aviso, así que está avisado. Si molesta, que el punto
  que cae cerca del jugador se corra por el anillo.

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
  **Decidido el 26/9**: sembrarle la experiencia desde su mejor oleada, con `nivelPremiado` en ese nivel, así no cobra
  esos niveles (como pretende la migración de los v4 y v5). El arreglo para v4 y v5 no puede ir adentro de
  `MigrarAlNivel` (`Logros` usa `Progreso.Jugador`, que llama a `Cargar()` con `datos` en null: recursión): una marca,
  y un paso después de `datos = leidos`.
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
  con distancia 0 lo que ya se superpone y no ordena. Quita también el empuje de PhysX (ver el empuje, abajo). Para
  medirlo, `PruebaDisparo` necesita fijar los FPS y sacar zombis: a 20, 30 y 60.
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
- **El empuje de la bala** (media). La bala es un collider sin Rigidbody que aparece adentro del zombi, y PhysX lo
  saca empujándolo (`m_DefaultMaxDepenetrationVelocity` sin tope): cada bala lo corre ~10-14 cm y le hace perder su
  paso. Con fuego sostenido (modelo, sin medir): el normal va al 85 % de su velocidad con 4 tiros/s, al 50-63 % con 12
  y al ~10 % con 20; el tanque retrocede con 20; el jefe persiguiendo va al 22-39 % con 8, y su carga cubre el 72-75 %
  de la línea con 12. En el tanque y el jefe depende de los FPS, y el juego se balanceó con esto sin saberlo.
  **Decidido el 26/9: queda, pero a propósito y parejo**: en `DanoZombi`, igual a cualquier FPS y con resistencia por
  tipo (el tanque y el jefe, más pesados). Antes, medir el de ahora en play (el avance de un tanque y del jefe bajo
  fuego, a 60 y a 30 FPS) para que se sienta igual. Va con el barrido de las balas, que quita el empuje de PhysX; si
  en cambio la bala pasa a trigger, su `OnCollisionEnter` pasa a `OnTriggerEnter`.
- **El récord de una partida abandonada** (baja). En el libre la partida se pierde sin compararla con MENÚ, REINICIAR,
  la R y cerrando la app; en oleadas, solo con REINICIAR y la R (MENÚ la guarda para retomarla, y se compara al
  morir). **Decidido el 26/9: en el libre cuenta**: `GuardarRecord` en `MenuPausa.Pausar` y en la R (sus puntos solo
  suben, y perder el foco pausa, así que cubre cerrar la app). En oleadas, solo dentro de `OlvidarPartidaSiEsOleadas`
  (REINICIAR y la R). No en `IrAlMenu`, como decía la auditoría: al retomar no saldría NEW BEST. El tutorial no
  escribe récord. Desde el 29/9 REINICIAR y la R pasan por `MenuPausa.ReiniciarYa` (después de la confirmación, H05):
  el `GuardarRecord` del libre en la R va ahí, o en `PedirReiniciar`, que es por donde entra la R.

### De la superauditoría del 29/9

- **Con el techo de zombis lleno, la invocación del jefe hace el aviso entero y no sale nadie** (media; H10, parcial).
  `JefePatrones.Empezar` (`:407-428`) elige invocar sin mirar si hay lugar y paga el anillo, el rugido y 0,8 s quieto;
  `Invocar` (`:545-551`) recorta con `LugarParaZombis` y `maxInvocadosVivos` (8), con 0 vuelve en silencio, y
  `Terminar` da vuelta `tocaCarga` igual: la invocación se pierde. Con uno solo, sale siempre al +X. El camino de
  `maxInvocadosVivos` pasa también en PC en la oleada 10 (la tercera invocación sin furia; en furia salen 6, 2 y 0).
  Corrige al hallazgo: los generadores no llenan el lugar en un cuadro (uno cada 0,35 s en oleadas y cada 0,25 s en el
  libre), así que pega sobre todo en el teléfono desde la oleada 20 (90 zombis contra un techo de 35) y en el libre
  cuando el nivel le gana al jugador (60-90 % vacías, modelo sin medir); en la oleada 10 del teléfono casi siempre
  sale entera. **Arreglo**: reservar el lugar en `Empezar` con un `static int EnemyController.Reservados` que reste
  `LugarParaZombis` y que miren los dos generadores (`WaveManager.cs:148` y `GeneradorZombis.cs:169`), soltado en
  `Invocar`, `VolverAPerseguir` y `OnDisable` y puesto en el reset de `SubsystemRegistration`; si la reserva da 0,
  cargar sin dar vuelta `tocaCarga`. Mirar el lugar en `Empezar` sin reservarlo no alcanza: el generador lo ocupa
  antes de que termine el aviso. La otra opción, que la invocación pase el techo hasta `maxInvocadosVivos` (35 → 43 en
  el teléfono), se decide con la medición de «720p nativo en el teléfono». Aparte, un desfase al azar en los ángulos
  para uno a tres invocados.
- **El botón de pausa del teléfono tapa lo que va arriba al centro** (media; H13, confirmado). El disco (de 24 a 154 u
  desde arriba, x ±65, alfa 0,92 y con el anillo celeste desde `0a4cc87`) se dibuja encima de: (1) el cartel «CAPÍTULO
  N / NOMBRE» (`CapitulosDeEscenario.cs:485-495`, centro en +385), cuya primera línea cae entera en esa franja en
  16:9, 18:9, 20:9 y 21:9 (en 20:9 tapa la Í, la T, la U y la L de «CAPÍTULO 2»), también al retomar en la 11 o más
  (`:179` corre fuera del if/else); (2) el nombre del jefe: `6b8ea40` subió `desdeArriba` a 170 solo en el .cs, y
  `MenuPausa.prefab:2364` sigue en 100 (letras en 151-183 u); (3) `PanelInstruccion` del tutorial
  (`Tutorial.unity:726-727`, de 145 a 315 u), que toca el anillo en cualquier proporción. `ProbarAvisosSinPisarse`
  (`PruebasMejoras.cs:1740`) nunca mira el botón. **Arreglo**: el capítulo no entra más abajo en ningún teléfono
  cuando el cartel de la oleada lleva el bono; lo más simple es un `CanvasGroup` en `BotonPausa` que baje el alfa a
  ~0,25 mientras dura el cartel (tocable igual), o poner «CAPÍTULO N · NOMBRE» como primer renglón del cartel de la
  oleada. El jefe: `desdeArriba` ~140 en el prefab y el mismo valor en el .cs. El tutorial: `PanelInstruccion` en y
  −260/−265 (ver la caja de arma del tutorial, abajo). Que la prueba lea de `MenuPausa.prefab` los rects de
  `AreaSegura/BotonPausa` y de su halo, y los compare con el capítulo, la barra del jefe y el panel del tutorial.
- **Los zombis patinan: las piernas cubren entre el 16 y el 36 % de lo que avanzan** (media; H16, confirmado). Del
  FBX: `Z_run_rm` avanza 3 m/s a escala 1 y `Z_walk_rm`, 1 m/s; los prefabs no usan root motion y se mueven a
  `enemyType.velocidad` (`EnemyController.cs:805-807`). Con la cuenta de `JefePatrones.PasoParaCorrer`, que ya calibra
  la carga del jefe sin patinar, el normal pide `Paso` 2,78 y tiene 1 (36 %), el rápido 6,25 y 1 (16 %), el FASTER
  11,1 y 2,5 (22,5 %) y **el tanque**, en `Z_walk_rm`, 2,5 y 0,8 (32 %, el más visible); el jefe persiguiendo, 66 %.
  `FondoMenu.cs:91-94` copia el mismo valor, así que el fondo del menú patina igual. Estaba anotado (`e09c0d6`) y
  `8219044` lo borró al reescribir «El jefe en el teléfono»; CLAUDE.md dice que `velocidadDeAnimacion` ajusta el paso
  a lo que camina cada uno, y es falso. **Arreglo**: una función pura `EnemyController.PasoPara(velocidad, ritmo,
  escala)` con el avance de cada clip y un tope en `Paso` 3-4 (a 30 FPS el FASTER completo serían 1,8 cuadros por
  ciclo: estroboscopio), usada en `OnEnable` y en `FondoMenu`; o serializar el `Paso` en los prefabs (normal 2,78,
  rápido 4, FASTER 4). Con tope 4 el rápido cubre el 64 % y el FASTER el 36 %: el FASTER no se arregla entero sin
  tocar su escala o su velocidad. El tanque queda como está (decidido el 29/9): la función no lo toca. Una prueba
  como la del jefe (`PruebasMejoras.cs:2316-2333`) y verlo con Grabar animaciones. Corregir la frase de CLAUDE.md.
- **En la ciudad las veredas tapan la línea de la carga y el anillo de la invocación** (media; H20, confirmado). Se
  dibujan a y 0,06 (`JefePatrones.cs:633` y `:696`) con Sprites-Default (`ZombiBOSS.prefab:221`), que respeta la
  profundidad, y las 16 veredas opacas llegan a 0,14 (el cordón a 0,18) y cubren el 34 % del piso (el 51 % alrededor
  del cruce). Modelo sin medir: el 86-90 % de las cargas pierde algo, el 36-42 % pierde la mitad, y el tramo donde
  está parado el jugador queda tapado el 43-50 % de las veces. Pasa con el jefe de la 30 (y el de la 60 y la 90). **El
  anillo de la granada tiene lo mismo**, a y 0,05 (`Granade.cs:117`, y el de puntería, `PlayerController.cs:320`), en
  las diez oleadas de la ciudad. **Arreglo**: una constante `JefePatrones.AlturaDelAviso =
  ManchaDeSangre.AlturaSobreElPiso` (0,2) en las dos líneas, lo mismo en `Granade.DibujarAnillo`, y las dos alturas en
  el chequeo de `PruebasMejoras.cs:1321-1340`, que ya compara la mancha con la vereda. El costo es el que ya se aceptó
  con la mancha: la cinta pinta los 20 cm de abajo de lo que la pisa. De paso: `anchoLinea` 1.4
  (`ZombiBOSS.prefab:203`) ya no existe en el código.
- **Un banco en play cortado a mano queda armado y secuestra el próximo Play del editor, sin respaldo** (media; H22,
  confirmado). Cada banco guarda en `SessionState` que está corriendo (su clave, `.empezo`, `.desde` y `.listo`) y
  solo lo apaga al terminar; solo `PruebaDiaria` lo limpia al volver a edición (`:830-855`). `RespaldoDelBanco`
  (`:111-114` y `:155`) sí devuelve el progreso y borra su marca, así que en el Play siguiente el banco sigue sin
  respaldo y con los estáticos en cero (cada Play recarga el dominio). Derrota deja al jugador al 10 %, lo mata,
  cuenta una partida, olvida la oleada en curso del progreso real y pisa su informe; Tienda, Tutorial, ModoLibre,
  MenuYTienda y GolpeAnimado cortan el Play en el primer cuadro con un HAY FALLAS falso; MuerteAnimada mata zombis con
  puntos y monedas si el Play llega antes de los 55 s; los Grabar le apagan el control al jugador. Y en cascada: el
  banco colgado corta el Play del siguiente banco, al que `RespaldoDelBanco` también le borra la marca. **Arreglo**:
  la regla "un banco sin respaldo no corre": `RespaldoDelBanco.EsDe(banco)` y, en cada `Tick`, después de `isPlaying`,
  si la marca no es suya, apagar su clave, `LogWarning` y salir. Cubre el Stop, la cascada y el error de compilación
  (que no pasa por `EnteredEditMode`). La otra forma es limpiar las cuatro claves adentro de `Restaurar`. Además, un
  `LogWarning` en los `Arrancar` que hoy se niegan callados (Disparo, MenuYTienda, ModoLibre y GrabarDisparo) y la
  guarda de play de `PruebaTienda.cs:202` en Derrota, GolpeAnimado, MuerteAnimada, GrabarJefe y GrabarAnimaciones.
- **Los doce bancos y «Poner la noche» abren escenas en modo Single sin mirar si hay cambios sin guardar** (media;
  H23, confirmado). `isDirty` solo aparece en `ConstructorNeon.cs:47` y `:164`. Los bancos (`PruebaTutorial.cs:175`,
  `PruebaTienda.cs:216` y los demás) y `PonerLaNoche` (`ConstructorEscenarios.cs:938-940`, que además relee las tres
  escenas de juego, las guarda y termina en el menú) cierran lo abierto sin preguntar, y `RespaldoDelBanco` no mira
  escenas. Lo más filoso: un agente toca WaveMode por unity-mcp, corre un banco para verificarlo, el banco prueba la
  versión de disco, escribe TODO OK y el cambio desaparece. `ConstructorNeon` no sirve de molde: perdona justo al menú
  sucio y solo mira la escena activa. (La documentación de Unity no dice que `OpenScene` no pregunte; lo dan por hecho
  `ConstructorNeon` y la comunidad.) **Arreglo**, sin ventanas: un ayudante común (`EscenasSinGuardar.Hay(quien)`) que
  recorra todas las escenas cargadas y, si alguna está sucia, dé `LogError` con la lista y cómo salir sin clics
  (`EditorSceneManager.SaveOpenScenes()` o reabrirla), como **primera** instrucción de las 14 entradas de menú de los
  bancos (varios borran carpetas o reinician el progreso antes del `OpenScene`), de `PonerLaNoche` y de las dos de
  `ConstructorNeon`. `PonerLaNoche` y `VestirPartida` vuelven a lo que estaba abierto con `GetSceneManagerSetup` y
  `RestoreSceneManagerSetup`. Mejor en el mismo andamiaje que el de arriba. Y un chequeo en la prueba de lógica que
  falle si un `.cs` de `Assets/Editor` llama a `OpenScene(` sin `Additive` ni el ayudante.
- **Adelantar la fecha del teléfono cobra días: el reloj confiable solo se ancla al cobrar la diaria** (media; H03,
  confirmado). La marca (`relojUtc`, `relojMs` y `relojArranques`) solo la escribe `RegistrarRecompensaDiaria`
  (`Progreso.cs:353-357`), y `HoraConfiable` devuelve el reloj crudo sin marca, si falla la lectura o si la marca es de
  otro arranque (`:592`, `:596`; `RelojConfiable.cs:53-54`). Desde cualquier reinicio posterior al último cobro (y sin
  reinicio, si nunca se cobró), cerrando la diaria con el atrás, cada día adelantado cierra el día, arma misiones y
  semanal nuevos y cobra lo cumplido; las misiones ni siquiera piden el menú (`AvisoDeMisiones.Start`). Además el día
  se puede **sortear** (`new Random(dia)`, `MisionesDiarias.cs:132`) hasta que la difícil sea barata. Modelo sin
  medir: +60 a +140 % de ingreso en la oleada 10 con 30 min a 2 h por día; en la 40, de −22 % a +100 % sorteando.
  **Decidido el 29/9: la hora de internet, y sin conexión se espera** (Ivan eligió el candado y no la fricción de
  anclar el reloj en cada arranque, que pedía un reinicio por salto). El cambio de día de la diaria, las misiones y el
  semanal (con su cobro solo de lo cumplido) sale de la hora de un servidor; sin conexión, o si el servidor no
  contesta, el día no avanza hasta que vuelva: se juega igual y lo que se gana jugando cuenta, pero no se cobra la
  diaria ni cambian las misiones (tampoco salen las primeras). Mata también el sorteo, y anclar el reloj en cada
  arranque ya no hace falta: sin conexión el día no avanza. Al hacerlo: el servidor (la
  cabecera `Date` de una respuesta HTTPS, como `generate_204` de Google, o NTP), el permiso de internet (hoy
  `ForceInternetPermission: 0` y ningún script usa la red; `UnityWebRequest` lo agrega solo), una consulta por
  arranque y al volver del segundo plano, con esa hora más `SystemClock.elapsedRealtime` en el medio (como hace hoy la
  marca), el tiempo de espera y un aviso en la ventana de la diaria y en la de misiones para cuando falte la conexión
  (textos en la tabla). En PC, que no se distribuye, alcanza con el reloj, como hoy. Con esto H70 desaparece con
  conexión y «Una fecha guardada en el futuro» se puede topar contra el día de internet. La consulta manda la IP del
  teléfono al servidor de la hora: mirarlo junto con H08 antes del próximo AAB.
- **Al jefe se lo puede matar sin que ataque nunca** (media; H11, confirmado). Solo ataca a 15 m o menos **y** con el
  pivote en pantalla (`JefePatrones.cs:352` y `:455-478`; `margenEnPantalla` no está serializado y vale 0,08/0,1): la
  ventana va de 4,7 m detrás a 6 m delante (±9-11,5 m al costado), y las balas llegan a 22 m (11 m/s por 2 s,
  `WaveMode.unity:694-695` y `Bullet.prefab:112`), con el jefe a 2 m/s y el jugador a 15. El propio comentario
  (`:468-472`) llama "lo normal al escaparle" a tener al jefe a 7-15 m por debajo. El ataque cubre el 13-16 % del
  disco de 1,5 a 22 m (el 46 % antes del 25/9); el jefe de la 10 (1.279 de vida) muere en 16 a 64 s. **Decidido el
  29/9: invoca aunque no se lo vea.** La invocación deja de pedir pantalla y llega hasta pasado el alcance de las
  balas (22 m; hoy, 15), sin el rugido si está fuera de cuadro; la carga sigue atada a la pantalla, que es la que
  necesita que se lea la pose. Si le toca la carga y no se lo ve, invoca sin dar vuelta el turno de la carga. Va con
  H10 (la reserva de lugar), porque fuera de cuadro también respeta el techo. Que `PruebasMejoras.cs:2369-2374` mida
  qué fracción del disco de 1,5 a 22 m queda cubierta por algún ataque, y no cuatro puntos, y verlo jugando («El jefe
  en el teléfono», sección 4).
- **El jugador y los zombis entran en los edificios de la ciudad y desaparecen** (media; H18, confirmado). Los 12
  edificios (11 × 11 m, de 3,7 a 6,4 m de alto, centros en ±12/±36) no tienen collider (`ConstructorEscenarios.cs:861`
  y `:906`; `Ciudad.prefab`), están dentro de las paredes (el 15 % del área) y son opacos: con la cámara a 12 m, el
  jugador de 2 m queda tapado entero. Los zombis no nacen adentro (los `spawnPoints` están en la calle), pero los
  cruzan en línea recta: desde (35, 0) hacia un jugador en (36, 19), uno aparece a 1,5 m y pega a los 0,2-0,3 s.
  Además, en la oleada 30 los invocados del jefe pueden nacer adentro (`PuntoDelAnillo` no mira `Tapado`), el jefe
  puede avisar la carga desde adentro (`EnPantalla` no mira la oclusión), los números de daño se ven a través del
  techo y, si el jugador muere adentro, la cámara corrida muestra un cuerpo que no se ve. **Decidido el 29/9: el techo
  se transparenta** (Ivan prefirió mantener la ciudad como está antes que sacar los edificios fuera de las paredes).
  (1) Para el jugador, un `BoxCollider` por edificio en la capa 9 (libre) que choque solo con la 6: las balas y los
  zombis pasan igual, y el raycast de las monedas lo toma como pared. Prenderlo en `Aterrizar`, corriendo antes al
  jugador fuera de la huella (sin la franja de `Tapado`, con 0,72 y 0,5 m de margen: PhysX lo expulsaría hasta 5,5 m
  en un paso). (2) El edificio que tiene a alguien en lo que tapa (`Tapado`: la huella y la franja de atrás, donde
  también queda el jugador) se vuelve translúcido con un fundido corto, y vuelve al irse. Para eso los edificios
  quedan fuera del `StaticBatchingUtility` (juntados no se pueden cambiar de a uno; son 12) y con un material propio
  que pueda ser transparente. Los carteles de neón, que son TextMeshPro y no se juntan, se desvanecen con su edificio.
  Pruebas: la capa, la matriz, la huella y que el edificio se transparente con alguien en `Tapado`; y mirarlo con la
  cámara del juego (sección 4).
- **La furia arranca lista en cada partida y cuenta para la misión y FURIOSO reiniciando** (baja, era media; H02,
  parcial). `activadaEn` arranca en `float.NegativeInfinity` (`Furia.cs:43`; que arranque lista es a propósito,
  `BotonFuria.cs:34-36`) y `Activar` suma `Progreso.ContarFuria` (`Furia.cs:99-109`), que es de por vida. Con
  REINICIAR, la R o pausa → MENÚ → OLEADAS (en el libre o entre carreras: en WaveMode REINICIAR y la R borran la
  oleada), cada vuelta da una furia: la difícil de la oleada 40 (29 furias, cotizada en 58 min, paga 51.450) sale en 1
  a 7 min según lo que tarde la vuelta (sin medir). La misión de furia sale el 47-67 % de los días. Adelanta, no
  multiplica: el cofre pide las tres misiones, FURIOSO llega igual jugando (~3,3 h) y el semanal no tiene furia. Con
  la granada, la vuelta rinde ×2,5 solo si dura 2 s. Amplía «Las dos pruebas son circulares»: `PruebasMejoras.cs:3434`
  tiene la misma forma, y si modelara la furia gratis de cada partida fallaría en la oleada 10. **Arreglo**, del lado
  del contador: un `static float` con los segundos jugados (tiempo escalado, sumado en `Furia.Update`) desde la última
  furia contada, en 0 en `SubsystemRegistration`; `ContarFuria` cuenta solo si pasaron `enfriamiento` segundos, y los
  descuenta. La furia sigue arrancando lista y el objetivo mide exactamente N×120 s. No llevar el enfriamiento entre
  partidas con tiempo real: rompe la pausa y le cambia el juego al honesto (eso lo decidiría Ivan).
- **El versionCode 5 ya se usó y la build del AAB no lo revisa** (baja, era media; H07, parcial).
  `ProjectSettings.asset:178` dice 5 y `:147`, 1.2.0; el AAB del 18/9 (`16d8a38`) salió con el 5 (leído de su
  manifiesto) y Play no deja repetirlo. `ArmarAab` y `Construir` revisan seis cosas y el versionCode no: solo lo
  anotan (`ConstructorAndroid.cs:332`). `TAREAS.md:130` ya dice subir a 6, y el rechazo llega al subir, con el mensaje
  exacto. De paso, `publicacion/pasos.md:55` todavía dice «versionCode 4 y versión 1.1.0», contra su propia línea 33.
  **Arreglo**: subir a 6 antes de armar; `publicacion/ultimo_aab.txt` versionado (arranca en 5); una función pura
  `ConstructorAndroid.ProblemaDeVersionCode(actual, ultimo)` que `ArmarAab` mire antes de tocar el keystore; y que
  `Construir`, con un AAB exitoso, escriba el versionCode en ese archivo (así cada AAB armado cuenta como usado y
  nadie tiene que acordarse de actualizarlo). Casos en `PruebasMejoras.cs:1410-1442`, y corregir `pasos.md:55` y
  CLAUDE.md.
- **Dónde vive `progreso.json` lo decide un ajuste que nada vigila** (baja, era media; H09, parcial).
  `AndroidPreferredDataLocation: 1` (`ProjectSettings.asset:182`) es PreferExternal (el enum de 6000.3.14:
  PreferExternal = 1, ForceInternal = 2; el `boot.config` de la APK dice 1). En el Inspector se llama **Storage
  Location**. Pasarlo a Force Internal, el arreglo "obvio" de la política, hace que `Progreso.Ruta()` (`:1099-1101`)
  mire la carpeta interna, que `Leer` dé `NoExiste` en los tres candidatos y que el juego arranque de cero, sin
  `soloLectura`. No destruye (volver a PreferExternal lo recupera, menos lo jugado entretanto) y hoy solo hay
  probadores. Amplía «`progreso.json` está en el almacenamiento externo»: si Unity cae al interno con el externo sin
  montar (está documentado; improbable con minSdk 25), queda un `progreso.json` casi vacío en el interno, y una
  mudanza que tome "el interno existe" como "ya se mudó" dejaría huérfano el verdadero. **Arreglo**: una constante en
  el código (`Progreso.UbicacionEsperada`) y una guarda en `ProbarCalidadDeAndroid` y en `ConstructorAndroid` (un
  `ProblemaDeUbicacion` que niegue la APK y el AAB). La mudanza, si se hace, con una marca propia en `PlayerPrefs`
  (`ProgresoMudado`) y, si hay archivo en las dos carpetas, eligiendo por contenido; y antes de producción.
- **El borde rojo del daño se corta en recto del lado de la cámara del teléfono** (baja, era media; H12, confirmado).
  `androidRenderOutsideSafeArea: 1` (`ProjectSettings.asset:71`) y `VinetaDanio` cuelga de `CanvasHelper`, que se
  ajusta a `Screen.safeArea` (`CanvasHelper.cs:16-29`; `WaveMode.unity:418-445`, igual en ShowBies1 y Tutorial). Con
  cámara perforada o muesca, en horizontal, el rojo termina en una línea vertical a un 3-9 % del ancho, según el
  teléfono (sin medir), con 0,38 de opacidad a media altura y 0,70 en la esquina (con la furia, 0,13-0,24). Dura 0,4 s
  y, rodeado, titila cada 0,4 s. No viene del neón: la viñeta no cambia desde `8b212f2` (14/9). De paso, el cartel de
  la oleada se centra en el área segura y los del capítulo y de misión en el canvas entero (36-63 u de desfase en un
  2400x1080). **Arreglo**, junto con «El borde rojo del golpe que mata se corta enseguida»: la viñeta en su propio
  objeto raíz, con un Canvas overlay sin área segura y detrás del HUD, y que `EsconderElHud` la deje desvanecerse. Lo
  mínimo, sin tocar escenas: en `VinetaDanio.Awake`, `SetParent` al canvas raíz, `SetAsFirstSibling()` y anclas 0-1.
  Opcional: los tres carteles en un mismo marco.
- **MODO LIBRE bloqueado: una píldora gris con el halo y el texto verdes de "jugar"** (baja, era media; H14,
  confirmado). `BotonModoLibre.Pintar` solo cambia `fondo.color` (`:50`) al gris del tema viejo (`Menu.unity:4771`,
  0,45/0,45/0,52); la `Sombra` (`:6129`), el icono y el texto siguen con el verde que les puso `ConstructorNeon` (el
  texto a 3,70:1). Se ve en `Builds/libre_boton_5_bloqueado_en.png`, y el banco solo mira el fondo
  (`PruebaModoLibre.cs:739`). La tienda ya resuelve "todavía no se puede" con `ApagadoOscuro`, `TextoSuaveClaro` y el
  halo apagado. Lo menor: el idioma elegido es amarillo con el halo celeste. **Arreglo**: pasar `Menu.unity:4771` a
  `Tema.ApagadoOscuro` (0.086, 0.102, 0.141) y seguir pintando con el campo (si no, el banco falla); el texto y el
  icono en `TextoSuaveClaro` (5,73:1); **apagar el halo** (alfa 0 en la `Sombra`), porque `PintarHalo` con
  `ApagadoOscuro` da un halo casi negro. El icono se busca con `fondo.transform.parent.Find("Icono")`. Al desbloquear,
  devolver los colores leídos en `Awake`. En el banco, el color de la `Sombra`, el del texto y un contraste de 4,5 o
  más. En `SelectorIdioma.Pintar`, `PintarHalo` con el color de cada botón. Corregir "lo pinta gris" en CLAUDE.md.
- **En PC el jugador corre un 41 % más rápido en diagonal** (baja, era media; H15, confirmado). `HandleMovement`
  (`PlayerController.cs:156-157`) multiplica los dos `GetAxis` por `moveSpeed` (15 en las tres escenas) sin topar:
  21,2 m/s en diagonal y 27,6 con la furia, contra el FASTER a 12. En el teléfono no pasa: el Joystick Pack normaliza
  y los `snap` están apagados. Es baja porque solo se distribuye Android, pero probar con el teclado del editor da un
  juego más fácil que el del teléfono. La reproducción con F1 no sirve: `MedidorBalance` no muestra velocidad.
  **Arreglo**: `moveInput = Vector3.ClampMagnitude(moveInput, 1f);` antes de calcular `moveVelocity` (conserva la
  rampa de `GetAxis` y el umbral de `run`, y topa también los ejes del gamepad).
- **La caja de arma del tutorial nace detrás del panel de instrucciones** (baja, era media; H21, confirmado). Nace 4 m
  al norte del jugador (`TutorialManager.cs:174`) y, con la cámara del juego, cae entre 278 y 318 px de 1080;
  `PanelInstruccion` (`Tutorial.unity:726-727`) llega a 315 px en 16:9 y 352 en 20:9, más 40 u de halo, y desde
  `0a4cc87` es casi negro al 90 %: la caja queda tapada al 92 % en 16:9 y entera de 18:9 a 21:9 (se ve en
  `Builds/tutorial_caja_arma.png`). `PruebaTutorial.cs:534` solo mira `activeSelf`. Es baja porque el tutorial es
  opcional y se destapa con menos de un segundo de movimiento. **Arreglo**: hacerla nacer al sur, `new Vector3(0f, 0f,
  -2.5f)` (738-776 px, del lado contrario a la marcha: no la tapan ni el panel movido de H13 ni el texto de la vida).
  No en `(0, 0, 2)`: con el panel bajado a −265 vuelve el problema en 21:9, y queda en el camino de quien viene de las
  cajas del paso 4. El desplazamiento como constante pública y un chequeo en la prueba de lógica que la proyecte con
  la cámara y el panel leídos de disco en 16:9, 20:9 y 21:9 (`PruebaTutorial` corre en la proporción del Game view).
- **Retomar vuelve opcional la muerte en oleadas** (baja, era media; H04, parcial). MENÚ no olvida la oleada
  (`MenuPausa.cs:96-100`) y perder el foco pausa y guarda (`:54-79`): solo morir, REINICIAR y la R la olvidan. Retomar
  (`WaveManager.cs:104-125`) crea un jugador nuevo: vida llena, `GolpesRecibidos` en 0, 500 balas, el revivir por
  vídeo disponible otra vez y las mejoras compradas entretanto (`AplicarMejoras.Awake`). En monedas no es granja
  (×1,2-1,6 por minuto con ~10 s por ciclo, modelo sin medir). INTOCABLE y el bronce y la plata de SUPERVIVIENTE solo
  se adelantan (~1 partida de monedas); el oro de SUPERVIVIENTE, `MejorOleada` y `HighScore_3` dejan de pedir
  sobrevivir las oleadas seguidas (hoy son solo locales). Con el proveedor Nulo el golpe que mata olvida la oleada en
  el mismo cuadro: hay que pausar antes. **Decidido el 29/9: las tres partes**, sin subir la versión: (1) un bool "la
  oleada en curso ya recibió un golpe", guardado junto a `oleadaEnCurso`, para que retomarla no cuente como intacta;
  (2) guardar la vida y las balas al empezar cada oleada y retomar con eso, topado al máximo nuevo; (3) una sola
  retoma por oleada: la segunda vuelve al principio del capítulo, que es lo que hace que morir importe (con las dos
  primeras solas, reintentar sigue siendo gratis). Ninguna castiga al que cierra en la 24 para dormir, que es el
  pedido de `d6005e6`. Amplía «Punto de control por capítulo en las oleadas» (el revivir gratis de MENÚ). En la misma
  tanda, corregir `WaveManager.cs:76-77`, `AplicarMejoras.cs:8-10` y CLAUDE.md ("no se puede comprar en medio de la
  partida"), que ya son falsos.
- **La próxima subida es la primera con la librería de reseñas: Seguridad de los datos** (baja, era media; H08,
  parcial). La 1.2.0 (5) salió de `16d8a38` sin ella (el dex del AAB no tiene `play/core/review`); `a0b1b76` la sumó
  (`mainTemplate.gradle:11`, `review` 2.0.1, y de rebote `play-services-basement` y `tasks`), y la APK del 27/9 ya la
  trae. La guía de Google (Data safety de In-App Review) dice que la valoración y el texto se comparten con el
  desarrollador en una pista cerrada; el formulario, la política (`privacidad.html:32-33`) y la ficha (`ficha.md:53` y
  `:100`) dicen "no recopila". No hay permisos nuevos (0 `uses-permission` en los dos manifiestos). **Decidido el 29/9:
  declararla, por las dudas.** Antes del próximo AAB, con las dos páginas de Google a la vista: en el formulario,
  "Otro contenido generado por el usuario" (la valoración y el texto), por Funcionalidad y sin compartir (confirmarlo
  con la página al llenarlo); sacar "no recopila tus datos" de `ficha.md:53` y `:100`; y en la política, en los tres
  lugares (`publicacion/privacidad.html`, el repo `showbies-privacidad` y la rama `gh-pages`), una línea: "el juego
  puede mostrar la ventana de valoración de Google Play; lo que escribas ahí lo maneja Google según sus políticas",
  junto con el arreglo de "privado". Actualizar `pasos.md:26` y `:85` junto con H114. Con la hora de internet (H03),
  mirar también si la consulta de la hora cambia algo del formulario.

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
- **Detrás de la derrota los generadores siguen sacando zombis y jefes** (las cajas ya no: 27/9): esperar mientras el
  jugador esté muerto (no cortar: puede revivir). ¿Es parte del festejo o sobra?
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

Todo lo de la auditoría se aplicó sin Unity. **El 27/9 se corrió en el editor**: Lógica de mejoras (TODO OK, 1158),
los bancos en play de siempre (golpe, muerte, derrota y disparo: TODO OK) y cinco bancos nuevos para lo que no tenía
banco: la tienda (abre con DAÑO a la vista, el toque que frena no compra, la guía de la primera compra), la diaria
antes que la tienda (cobrando, con vídeo y con el atrás), el desbloqueo del libre (el aviso una sola vez y el ¡NUEVO!
en los dos idiomas), el tutorial (la granada al grupo y afuera, las cajas y lo que nace contra las paredes) y el menú
con la tienda (ningún cuadro liso al abrir y cerrar por los cuatro caminos, y el menú igual sin HDR): TODO OK, 89
chequeos. Los cinco quedaron como bancos fijos (ver Pruebas y medición en CLAUDE.md), y todos los bancos en play
devuelven ahora el progreso real al terminar (`RespaldoDelBanco`). Además se grabaron y revisaron cuadro a cuadro el
jefe y la derrota, y lo que salió de ahí se arregló (las cajas después de morir, el color del próximo objetivo, el
festejo cortado por el borde, la línea del aviso y el zarpazo que seguía en el aviso del jefe). Queda:

- **El jefe en el teléfono**: las piernas al embestir no patinan y los patrones se ven bien (grabación del 27/9), pero
  que **solo ataque con el jefe en pantalla** (~4,7 m detrás del jugador, ~6 m delante y 9-11,5 m al costado, contra
  los 15 m de radio de antes) no lo puede mostrar el banco, que mueve la cámara: ver jugando que no se vuelva fácil de
  evitar; se ajusta con `margenEnPantalla`.
- **El techo de monedas duro**: que no se note la moneda vieja que se va cuando cae la lluvia del jefe.
- **El menú en 20:9**: las ventanas de misiones y logros se ven un 8 % más chicas, para que entre el halo.
- **720p nativo en el teléfono** (antes quedaba en 540): medir los FPS en la oleada 10+ con 35 zombis; si no llega a
  60, bajar `AltoMinimoMovil`.
- **La horda**: las barras de vida cruzadas en una horda apretada, y las manchas de sangre, que ahora van a 0,2 m
  (para no quedar debajo de las veredas de la ciudad) y tapan unos 20 cm de lo que las pisa durante sus 2 s.
- **Las medallas de logros**: cada familia con su símbolo de una o dos letras, que entre en el círculo.
- **La build**: ahora se niega con un paquete, un nombre o un orden de escenas que no son los de Play, y por línea de
  comandos sale con código 1 si falla. La APK sale (27/9, tres veces); falta hacer un AAB.
- **El sonido, en el teléfono**:
  - que las granadas no crujan sobre un grupo ni sobre un tanque (la que mata suena ~2,6 dB más baja: ver si se
    siente débil), y que la horda densa y los arpegios de la tienda no se sientan apagados (bajan ~1 y ~3 dB);
  - los clics de todo el menú y de la pausa, y el control de EFECTOS, que ahora suena al arrastrarlo;
  - el disparo, que subió ~27 dB (estaba casi mudo): que no canse a 20 tiros por segundo;
  - la muerte del jugador (golpe grave, temblor y borde rojo), las notas de furia lista y de fin de la furia, la
    muerte del jefe (que ya no se pierde), un solo jingle al pasar a la oleada 11 y la escalera de la barra del nivel;
  - la música del menú, que ahora arranca unos cuadros tarde: medir cuánto tarda `LoadScene(0)` antes y después.
- **El menú y la tienda**: en el Profiler, durante una caja de arma, `TextMeshPro.GenerateTextMesh` ya no tendría que
  aparecer por cada número de daño que se desvanece.

### De la superauditoría del 29/9

- **El jefe en el teléfono** (amplía, con H10 y H11): además de ver que no se vuelva fácil de evitar y que invocar
  fuera de cuadro no se sienta injusto (los invocados llegan sin que se vea al jefe), contar cuántas
  invocaciones salen vacías o con uno solo en el teléfono desde la oleada 20 y en el libre cuando el nivel le gana al
  jugador; si se elige que la invocación pase el techo, medir los FPS con 43 zombis.
- **Las piernas de la horda** (H16): Grabar animaciones con la cámara del juego antes y después, y mirar a 30 FPS que
  el rápido y el FASTER no se vean estroboscópicos con el tope.
- **La noche** (H17, arreglado, y H20): la foto del chorro de balas y las tres cajas del 29/9
  (`Builds/noche_cajas_balas.png`, de `PruebaReiniciar`) es con la calidad del editor y cerca de dos faroles: falta una
  con la calidad del teléfono (como `FotosDeLosFaroles`) lejos de los faroles, y otra con el gris de poca vida en 0,5.
  El brillo de las cajas son cuadrados de partículas lisos, que ahora sí se ven: mirar si se quieren más lindos. En la
  ciudad, la cinta y el anillo a 0,2 m encima del jefe, el jugador y los zombis.
- **El borde rojo** (H12): en un teléfono con cámara perforada o muesca, en horizontal, recibir un golpe, mirar el
  borde del lado de la cámara y, con el arreglo, que llegue al borde real.
- **El reloj** (H03, con la hora de internet): en el teléfono, sin conexión (modo avión), que el día no avance aunque
  se adelante la fecha, y que al volver la conexión salgan la diaria y las misiones del día.
- **Los edificios de la ciudad** (H18): con la cámara del juego, que el techo se transparente a tiempo con un zombi, el
  jefe o el jugador en la huella o detrás, y los FPS de la ciudad en el teléfono con los edificios fuera del batching.
- **La diagonal en PC** (H15): loguear `rb.linearVelocity.magnitude` con W y con W+D (F1 no muestra velocidad).
- **De los bajos sin refutar, piden play o teléfono**: H41 (el orden de `OnApplicationFocus` con la granada apuntada),
  H50 (el `Paso` y el `Ritmo` de un tanque que vuelve del pool), H61 (auriculares Bluetooth y la música del menú), H64
  (la latencia del audio, filmando un disparo), H77 (la luz ambiente al entrar a ShowBies1 desde el menú), H84
  (`Progreso.Guardar` en el Profiler al subir de nivel en el libre), H85 a H90 (el `Juntar` del decorado, el HUD, el
  aplastado, los 22 materiales, el Maximum Allowed Timestep y el GPU Instancing de los zombis) y H93 (un banco del
  revivir y del x2).

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
- **Si Unity 6 acota el pitch a 3, las escaleras y los arpegios repiten la nota de arriba** (H19; refutado en la
  superauditoría del 29/9). Desensamblando `libunity` de 6000.3.14f1 (Android IL2CPP Release, arm64 y x86_64):
  `AudioSource::SetPitch` guarda el valor crudo y lo pasa a cada canal sin acotar, `GetPitch` lo devuelve tal cual y
  FMOD solo corta la frecuencia en 1.000.000 Hz (pitch ~22,7). El [−3, 3] de la documentación es `CheckConsistency`,
  la validación de lo serializado y del inspector. El juego pide como mucho 24 semitonos (pitch 4) y todas las notas
  suenan distintas. Queda algo chico: un comentario en `Sonidos.PitchDe` para que nadie "corrija" las tablas (verlo en
  el editor es opcional: `s.pitch = 4f; Debug.Log(s.pitch)` imprime 4).
- **El paso del tanque** (con H16; decidido el 29/9: queda como está). Camina con `Z_walk_rm` a `Paso` 0,8 y pediría
  2,5 para no patinar, que es una marcha rápida y no el paso pesado que se buscó; corriendo pediría 0,83, la cámara
  lenta que ya se había rechazado. Ivan lo prefirió así: el arreglo de H16 no toca el tanque. Si algún día se revisa,
  las otras opciones eran un término medio con `Ritmo` 0,3-0,5 (grabado con Grabar animaciones para elegir mirando) o
  bajarle la velocidad, que es balance.
- **Corregir un Descartado, «La derrota suena con `pedo.mp3`»** (H102, sin refutar): el YAML dice que sí suena
  (`Perdiste.unity:717-735`, el `AudioSource` del objeto `Menu` con `pedo.mp3` y `m_PlayOnAwake: 1`), y el descarte
  (`2af1549`) no anotó por qué. No es un error del juego: documentarlo como intencional en CLAUDE.md (o quitarlo si no
  lo es) y sacarlo de esta lista.

## 7. Sin refutar

Los 100 hallazgos bajos de la superauditoría del 29/9 (H24 a H123), que no pasaron por la ronda `refutar:`: antes
de arreglar uno, confirmar que pasa. El detalle de cada uno está en `auditorias/29-9/unicos.md`.

No quedaron medios sin refutar. Los 100 bajos, uno por renglón (el arreglo de cada uno está en `informe.md`):

- **H24** FURIA mientras dura (1,3:1) y la G recargando (2,2:1) no se leen (`BotonFuria.cs:102-106`).
- **H25** Los avisos de misión se vacían con el juego congelado: se pisan en la pausa y suenan tras la derrota.
- **H26** Los carteles del capítulo, de la guía de monedas y de la furia se vencen detrás de la pausa.
- **H27** En PC, tocar el panel final del tutorial con la mira dispara (`TutorialManager.cs:177-183`).
- **H28** Los zombis del tutorial pueden nacer a la vista, a 14 m (`TutorialManager.cs:142` y `:152`).
- **H29** El tutorial dice que el cargador grande queda «para siempre», y dura la partida (`Textos.txt:226`).
- **H30** Derrota con el x2: el próximo objetivo no se entera del cobro y cae dentro del halo del vídeo.
- **H31** En PC, Espacio o Enter vuelven a comprar la última tarjeta tocada (`TarjetaMejora.prefab:2451`).
- **H32** La granada y la furia se muestran como NIVEL 0/1, y la furia no dice qué hace (`TarjetaMejora.cs:200`).
- **H33** Precio y saldo se truncan al mismo número (157K, 1,2 M) y la tarjeta queda gris (`FormatoNumeros.cs:26`).
- **H34** «Mejor oleada: N» de la tienda es la completada, una menos que la alcanzada (`TiendaMejoras.cs:409`).
- **H35** SEGUIR JUGANDO de ¿SALIR? no es píldora: VOLVER agrandado sin redondear (`ConfirmarSalir.cs:72`).
- **H36** Tocar el COFRE y cerrar las misiones enseguida no lo abre hasta volver (`VentanaMisiones.cs:207`).
- **H37** Al volver por MEJORAS, la barra del nivel se llena tras la tienda y suena (`VentanaLogros.cs:302`).
- **H38** El aviso de misión pisa los botones de furia y granada en 16:9, 16:10 y 4:3 (`AvisoDeMisiones.cs:37`).
- **H39** Sin neón lo armado en código: capítulo, guía, barra del jefe, volúmenes y el VOLVER de la diaria.
- **H40** La diaria no sale al volver a la app en un día nuevo (`VentanaRecompensaDiaria.cs:97-131`).
- **H41** Al perder el foco, la granada que se apuntaba se tira sola (`JoystickGranada.cs:71-80`; sin confirmar).
- **H42** La derrota no entra en un monitor 32:9 de Windows (`Perdiste.unity:1168-1173`, match 0).
- **H43** La embestida pega a quien la toca al arrancar, fuera de la cinta roja (`EnemyController.cs:1037`).
- **H44** Con la derrota, el jefe va a festejar congelado en la pose de su patrón (`JefePatrones.cs:331-335`).
- **H45** El jefe cae bajo el techo de cadáveres y en el teléfono se va de golpe (`EnemyController.cs:599-606`).
- **H46** El zombi normal flota 10 cm: su modelo va en y −0,794 y no en −1 (`Zombi.prefab:433`).
- **H47** Los invocados del jefe caminan con las piernas sincronizadas (`EnemyController.cs:513`).
- **H48** El jefe muestra dos barras de vida, la flotante y la de arriba (`EnemyController.cs:845-848`).
- **H49** Una excepción en el bloque de muerte deja un zombi inmortal que traba la oleada (`EnemyController.cs:850`).
- **H50** Verificar en play que un tanque o un FASTER que vuelve del pool conserve `Paso` y `Ritmo`.
- **H51** Decorados: autos sobre el vacío, faroles en autos, tumbas encimadas y una cerca que el jugador cruza.
- **H52** En la ciudad el jugador y los zombis se hunden 14 cm en las veredas (`ConstructorEscenarios.cs:467`).
- **H53** Las cajas nacen en filas de Z por el `Random.Range` de enteros (`PowerUp.cs:67`).
- **H54** El cartel de la oleada pasa a mayúsculas con la cultura del teléfono: en turco sale COİNS.
- **H55** «Nivel» nombra el del jugador y el del libre en la misma pantalla (`Textos.txt:91` y `:178`).
- **H56** «faltan 1» en la tarjeta de la tienda (`TarjetaMejora.cs:262`).
- **H57** El porcentaje sale «30%» en la tienda y «30 %» en los logros (`Mejora.cs:107`, `Textos.txt:105`).
- **H58** El cartel de neón ZOMBIS de la ciudad está en español (`ConstructorEscenarios.cs:364`).
- **H59** Redacción: tres filas en inglés poco naturales y una en español desparejada (`Textos.txt:105`, `:225`).
- **H60** `IconoDeBoton` mide el texto con su margen y el icono queda ~4 veces más lejos (`IconoDeBoton.cs:37`).
- **H61** Conectar auriculares Bluetooth corta la música del menú hasta recargarlo (`FuenteConVolumen.cs:33`).
- **H62** Amplía «El limitador de sonido no llega a todo»: al revivir, la explosión y el cartel pasan la escala.
- **H63** El control MÚSICA de la pausa no cambia nada que se oiga (`VolumenEnPausa.cs:34`; decide Ivan).
- **H64** El buffer de audio en 1024 puede atrasar el sonido respecto de la imagen (`AudioManager.asset:12`).
- **H65** Amplía «"Gana N monedas"...»: «completa N oleadas» sale en un tercio repitiendo las primeras.
- **H66** Amplía «Los jefes de las misiones y del semanal se farmean»: cuenta m/10 jefes y no floor(m/10).
- **H67** IMPARABLE (combo x25/x50/x100) se regala en el modo libre (`ContadorCombo.cs:81-86`; decide Ivan).
- **H68** `Guardar` falla callado con el almacenamiento lleno: la sesión queda en memoria (`Progreso.cs:797`).
- **H69** `Cargar` prefiere un principal viejo a un `.tmp` entero y más nuevo (`Progreso.cs:834-841`).
- **H70** El reloj confiable puede quedar atrasado para un jugador legítimo hasta reiniciar; va con H03.
- **H71** Amplía «Cerrar la app en ¡HAS MUERTO!...»: con red real, el x2 se pierde si Android mata la app.
- **H72** Los botones de la derrota andan mientras se pide el vídeo del x2 (`MenuPerdiste.cs:61`, `:71`, `:80`).
- **H73** Amplía «Antes de integrar la red de anuncios»: el x2 de la diaria se pregunta una sola vez.
- **H74** Va con «Antes de integrar la red de anuncios»: el atrás que cierra un vídeo real llegaría a Unity.
- **H75** Amplía «Antes de integrar la red de anuncios»: AdMob pide App ID, EDM4U y callbacks (`pasos.md`).
- **H76** Amplía «Antes de integrar la red de anuncios»: la APK de prueba fuerza Falso y no probaría el SDK.
- **H77** `FondoMenu` y `CapitulosDeEscenario` limpian `RenderSettings` en `OnDestroy` (inerte hoy, sin verificar).
- **H78** Las ventanas del menú apagan su `Abierta` estático solo si el panel sigue vivo.
- **H79** `installLocation=preferExternal` (`ProjectSettings.asset:181`; no mueve `progreso.json`).
- **H80** El paquete preview ai.assistant mete tres DLL de runtime en el juego (`manifest.json:3`).
- **H81** `CalidadDeAndroid` lee `QualitySettings` del disco y no lo cargado en Unity (confianza baja).
- **H82** Las capturas y el banner de la ficha de Play son de antes de la noche y del neón.
- **H83** El contador de FPS se ve en la versión de Play y en las capturas (`ContadorFps.cs:15-25`).
- **H84** `Progreso.Guardar` hace fsync y dos renombres en el hilo principal en plena acción (sin medir).
- **H85** Amplía «El decorado del capítulo siguiente se arma en plena pelea»: el `Juntar` de la 11 y la 21.
- **H86** El HUD rearma textos en cada cuadro: las balas, los colores de monedas y combo, los radiales.
- **H87** El aplastado del golpe escala la raíz física del zombi en cada paso (`EnemyController.cs:930-936`).
- **H88** Amplía «El piso: sin Specular Highlights...»: son 22 materiales más del decorado.
- **H89** Maximum Allowed Timestep 0,333: un tirón se paga con hasta 16 pasos de física (`TimeManager.asset`).
- **H90** Amplía «Los zombis: GPU Instancing...»: solo juntaría las cabezas.
- **H91** El tutorial busca la granada en toda la escena en cada cuadro (`TutorialManager.cs:86-94`).
- **H92** Muerte animada juega la oleada guardada y falla sola en múltiplos de 10 y desde la ~35.
- **H93** El revivir y el x2 de la derrota no los recorre ningún banco.
- **H94** Amplía «Las dos pruebas son circulares»: son siete, y el 120 y el 5 no se comparan con el prefab.
- **H95** El libre no lo juega ningún banco, ni se prueba que desbloqueado lleve a la escena 1.
- **H96** Tienda, Tutorial y MenuYTienda hacen la acción por atrás si el camino real falla, y pasan igual.
- **H97** El arreglo del 27/9 en `JefePatrones.Empezar` no lo cubre ninguna prueba.
- **H98** «Reiniciar todo» abre un modal y los atajos dicen que guardaron en solo lectura (`HerramientasProgreso`).
- **H99** Con dos copias de ShowBies abiertas, los respaldos de los bancos se pisan (`RespaldoDelBanco.cs:23`).
- **H100** La restauración de los bancos quedó duplicada, en parte sin efecto e incompleta.
- **H101** CLAUDE.md manda cortar el input con `Pausado`; lo que corta es `JuegoCongelado` (paso 9 de la receta).
- **H102** Corrige el Descartado «La derrota suena con `pedo.mp3`»: suena (`Perdiste.unity:717-735`).
- **H103** El menú no usa la noche del cementerio desde el 25/9: cielo azul sobre la tierra violeta.
- **H104** «Un enemigo nuevo no pide tocar código» es falso desde el bestiario y la v6 (CLAUDE.md:1972).
- **H105** La trampa de QualitySettings dice que nada lo delata, y la build ya se niega (CLAUDE.md:1845).
- **H106** Frases viejas en CLAUDE.md: «único momento», «hoy, el x2», «todavía no los muestra nada».
- **H107** «Mismo mapa que el libre»: el decorado de las oleadas cambia cada 10 (CLAUDE.md:18).
- **H108** Desactualizaciones chicas del layout y del texto de CLAUDE.md.
- **H109** Comentarios de la derrota vieja: jugador destruido y partida congelada (`CamaraJugador.cs:127`).
- **H110** `VigiaAplicacion` guarda al ir a segundo plano, no al perder el foco como dicen el código y CLAUDE.md.
- **H111** La diaria también sale con la primera oleada completada, no solo con la primera partida terminada.
- **H112** La niebla no se ve en la partida y el borde del mapa queda a la vista, aunque la doc diga que lo tapa.
- **H113** `LeerEscena` lee la escena abierta en memoria, no el disco (`PruebasMejoras.cs:1605-1622`).
- **H114** `pasos.md` no refleja el estado de Play, y el repo sigue público con `gh-pages`.
- **H115** `MainMenu.PlayGame` no lo llama nadie y CLAUDE.md lo da como camino al libre.
- **H116** Anuncios: código muerto, comentarios viejos y el cartel del anuncio de prueba con voseo y sin Bangers.
- **H117** `FondoMenu`: la rama del día parece muerta pero sostiene el piso de noche (`FondoMenu.cs:140-150`).
- **H118** `Assets/NewAudioMixer.mixer`, sin trackear: un mixer vacío que no usa nadie.
- **H119** Assets propios sin referencias (12 .mat de halos, 3 materiales y 2 .jpg de licencia desconocida).
- **H120** Overrides y claves serializadas de campos que ya no existen (tres escenas, Moneda y ZombiBOSS).
- **H121** Cinco paquetes sin uso en el manifest (`manifest.json:4`, `:5`, `:8`, `:9` y `:11`).
- **H122** Terceros sin uso: ~19 MB y 205 archivos (Gridbox, docs de TMP, ejemplos del Joystick Pack).
- **H123** Las cuatro ventanas del menú y los doce bancos están copiados; `PruebasMejoras` tiene 4.578 líneas.
