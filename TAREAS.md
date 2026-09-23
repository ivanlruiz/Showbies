# Tareas pendientes de ShowBies

Lista de Ivan del 16/9/2026, para después de subir la prueba cerrada. Todavía no se empezó ninguna.

## Visual

- [ ] **Cambiar la fuente de los textos.** Hoy todo usa Bangers. Elegir la nueva y reemplazarla en todas las
      pantallas (menú, HUD, tienda, derrota, revivir, tutorial y números flotantes).
- [x] **Cambiar el fondo** del menú: zombis cruzando sobre el piso de la partida y monedas cayendo.
- [x] **Paleta de colores más clara** en todo el juego: pasto de día, cielo celeste y paneles crema.

## Menú

- [x] **Sacar las monedas del menú principal:** que se vean solo en la tienda de mejoras.
- [x] **Reacomodar los botones del menú.** PLAY abajo a la derecha, y al tocarlo que pregunte qué modo jugar:
      modo libre, oleadas o tutorial. El tutorial deja de ser un botón aparte del menú.

## Juego

- [x] **Más zombis por oleada y que aparezcan más rápido** (`zombisBase`, `zombisPorOleada` e
      `intervaloEntreApariciones` del `WaveManager` de WaveMode). Ojo con el techo de zombis vivos en el celular (35).
- [ ] **Mapas diferentes.** Hoy el modo libre y las oleadas usan el mismo piso.

## Sonido

- [x] **Volumen de efectos y de música por separado,** con un lugar en la UI para cambiarlo (hoy no hay pantalla
      de opciones). La música de partida se sacó; definir si vuelve con su propio volumen.

## Mejoras

- [ ] **Power-ups en la tienda:** que las cajas (balas, vida y arma) se puedan mejorar con monedas, por ejemplo más
      duración, más efecto o que aparezcan más seguido.

## Retención (para que vuelvan cada día)

- [x] **Recompensa diaria:** monedas por entrar, que crecen si entrás varios días seguidos.
- [x] **Misiones:** 3 por día del tipo "matá 200 zombis", "llegá a la oleada 10" o "usá la furia 3 veces". Dan monedas y
      le ponen un objetivo a cada partida.
- [ ] **Logros:** hitos permanentes (primer jefe, 10.000 zombis, crítico al 100 %). Se pueden conectar con Google Play
      Games.

## Pendientes de la auditoría (18/9/2026)

Lo que encontró la auditoría de 10 agentes y quedó para después de la versión 5. Lo de antes de subir ya está hecho.

**Antes de integrar AdMob** (hoy no pasan porque la versión de Play sale sin anuncios):

- [x] La pausa de impacto descongela el juego con la oferta de revivir abierta (`Efectos.cs`).
- [x] Revivir saca al jefe y a todo lo que está a 7 m, y la oleada los cuenta como muertos (`EnemyController.DespejarAlrededor`).
- [x] Cerrar un video bloquea la oferta 60 s, y el tope "por partida" sigue contando en el menú (`ServicioAnuncios`).
- [x] Revivir después de caer por el kill-Z gasta el video y el jugador vuelve a morir a los 2,5 s.
- [x] Si Android mata la app durante el video de revivir, se pierde el récord de esa partida.

**A mediano plazo:**

- [ ] **Repo público:** reparte assets del Asset Store (ToonyTinyPeople, Joystick Pack, Blood decal pack, texturas) y además
      sirve la política de privacidad de Play. Ya se mudó la política a `ivanlruiz/showbies-privacidad` y el link nuevo
      está guardado en Play Console sin enviar. Falta: mandarlo a revisión cuando se apruebe la versión 5, y cuando Play
      muestre el link nuevo, pasar este repo a privado.
- [ ] **Muro de balance entre las oleadas 35 y 39** (parcheado el 19/9, falta la solución de fondo): el daño sube de a 1 con precio ×1,45 y la vida de los zombis ×1,15
      por oleada.
- [x] **Política de privacidad:** dice que el progreso nunca sale del teléfono, pero el Auto Backup de Android lo sube.
      Corregida en la versión del 19/9.
- [x] Adelantar la fecha del teléfono cobra varias recompensas diarias seguidas (RelojConfiable, 19/9: hace falta
      reiniciar el teléfono para hacerlo).

**Chicos:** hechos el 19/9 (empatar el récord, zombis pegados al aparecer, granada a los pies, morir al terminar la
oleada, el libre sin guardar, versión del progreso, monedas con sombra, sonidos sin precargar, la APK que no se
instalaba encima de la de Play, `Materials.meta` vacío y CLAUDE.md desactualizado).

## Ronda de ideas (19/9/2026)

Seis agentes, uno por ángulo. En orden de prioridad:

1. [x] **Parche del muro de las oleadas 35-39, sin código** (hecho: vida 1,11 y monedas 1,08; el jefe quedó igual) (simulado: hoy la 35 pide ~440 partidas y la 40 no se alcanza).
   En el WaveManager de WaveMode: `crecimientoVida` 1,15 → 1,11, `crecimientoMonedas` 1,05 → 1,08 y un jefe más blando
   (`crecimientoVidaJefe` 1,10 o BOSS.asset de 500 a 300). Lleva el muro a la 45-48. Validar jugando de la 30 a la 45.
   Simulación: `scratchpad/ideas_balance/sim.py` de la sesión (copiarla al repo si se usa).
2. [x] **Primer camino del jugador nuevo:** la primera vez, PLAY entra directo a la oleada 1 con pulgares fantasma sobre los
   joysticks; después, primera compra guiada (una mano sobre la tarjeta de daño). Postergar la recompensa diaria hasta
   la primera partida terminada. El tutorial viejo enseña la granada, que al principio no se tiene.
3. [x] **Contadores de por vida** en `Progreso` (matados por tipo, jefes, granadas, furias, monedas ganadas jugando):
   base de misiones, logros y renacer. Subir `VersionActual`.
4. [x] **Pedido de reseña in-app** (Google Play In-App Review) después del primer jefe; nunca en la derrota, sin premio.
   Tiene que estar en el AAB que sale a producción.
5. [x] **Jugo barato:** combo que toca notas que suben y carteles en x10/x25/x50 (`ContadorCombo`); escalera de monedas
   (las agarradas seguidas suben de grado en vez de sonar al azar, `Moneda`).

Después:
- Balance de fondo: daño ×1,5 cada 5 niveles (`Mejora` con hitos) con vida 1,12 y monedas 1,09; subir topes de
  cadencia y críticos; renacer desde la oleada 30.
- Combate: zombi hinchado que explota al morir (abre el enganche de comportamiento por zombi), oleadas con evento
  (ESTAMPIDA, FIEBRE DEL ORO, NIEBLA), jefe con ataques con aviso, zombi embestidor, la caja de arma da un modo de
  disparo (escopeta, perforante, rebote).
- Jugo: vibración en Android con interruptor, multi-kill con pausa de impacto, el jefe como evento (entrada y muerte).
- Opciones: joystick flotante, zurdos, tamaño de sticks, regulador de temblor y destellos; asistencia de puntería en móvil.
- Retención: ~~misiones diarias~~ (hechas el 19/9, con el cofre por las 3), ~~próximo objetivo en la derrota~~ (hecho el 19/9), ~~bestiario~~ (hecho el 19/9), desafío semanal.
- Crecimiento: compartir el récord; red real con LevelPlay (confirmar antes lo del plugin de AdMob); cuando entren los
  anuncios, corregir en la ficha "no recopila datos" y "sin internet"; lanzamiento escalonado y A/B del icono.

## Ideas de Archero (19/9/2026)

1. **Elegir 1 de 3 al terminar cada oleada** (el próximo feature grande, para la versión 7). En el descanso entre oleadas,
   tres cartas; se elige una, dura solo esa partida y se acumula. Disparo (doble, abanico, atraviesa, rebota, hacia atrás),
   elementales (fuego con daño por segundo, hielo que frena, rayo que salta), supervivencia (+vida, curarse al matar,
   escudo de un golpe) y monedas (botín, imán). No compite con la tienda: la tienda es lo permanente, esto la partida.
   Ayuda con el muro de las oleadas altas. Lo más trabajoso: las balas que atraviesan y rebotan (sobre el pool actual).
2. **Cartas raras y combinaciones**: una dorada de vez en cuando y combos secretos (fuego + rebote = balas de lava).
3. **Disparar solo al quedarse quieto** como opción de control, para jugar con un pulgar.
4. **El ángel del jefe**: después de cada jefe, curarse entero o una carta extra.
5. [x] **Capítulos con mapas** (hecho el 19/9: el cementerio de noche en las oleadas 11-20): cada 10 oleadas cambia el escenario (ya pedido en Visual como mapas diferentes).

No copiar: la energía que limita partidas ni el equipo con cofres y rarezas (monetización agresiva).

## Para mañana (anotado el 19/9 a la noche)

**Estado (al 20/9):** `main` está en `eea0eb5`; `idiomas` tiene además el cementerio (`2bc5a0c`), esta nota y el
**modo oscuro** (`9aba98a`), sin pushear. La versión 5 sigue en revisión en Play. Nada de esto está probado en un
teléfono.

**Primero, en este orden:**
1. [x] Los dos que rompían algo están arreglados (20/9): el jefe en el fondo del menú y el balance de las misiones.
2. [ ] Armar la APK de prueba (sale como "ShowBies (prueba)", al lado de la de Play) y probar en el teléfono: guía del
   jugador nuevo (pulgares en los joysticks), misiones y cofre, bestiario, próximo objetivo, jefe con patrones,
   cementerio (rendimiento al entrar a la oleada 11), combo que suena, escalera de monedas, SALIR que pregunta y el
   **modo oscuro** (el interruptor en OPCIONES, el menú de noche y que todo se lea al sol).
3. [ ] Pasar todo a `main`.
4. [ ] Cuando Google apruebe la 5: publicarla, mandar a revisión el link nuevo de la política y, cuando la ficha lo
   muestre, pasar el repo a privado.
5. [ ] Armar el AAB de la 6 (subir `AndroidBundleVersionCode` a 6 y `bundleVersion`) y mandarla.

**Revisión del jefe y lo nuevo** (un agente, 19/9; nada arreglado todavía):

- [x] **Media — el jefe rompe el fondo del menú.** `JefePatrones` tenía `[RequireComponent(EnemyController)]` y
  `FondoMenu` no podía borrar el EnemyController del BOSS: el jefe del fondo quedaba invisible y tiraba excepciones.
  **Arreglado el 20/9** sacando el RequireComponent (queda anotado en las trampas de CLAUDE.md).
- [x] **Media — las invocaciones ignoran el techo de zombis vivos.** **Arreglado el 20/9**: el generador fija
  `EnemyController.FijarTecho` y el jefe invoca `min(los suyos, maxInvocadosVivos, LugarParaZombis)`; en el modo libre
  no sale otro jefe mientras haya uno vivo.
- [x] **Media — premios de misiones al revés.** Eran fijos (150/300/600, cofre 800) contra objetivos que escalan.
  **Arreglado el 20/9**: el premio es una fracción (0,4/0,5/0,6) de lo que dan las partidas que cuesta el objetivo, y
  el cofre la mitad de las tres. El primer día pasa de 1.850 a 350 monedas, y el día entero paga ~2/3 de lo que da
  jugarlo en toda la curva (medido en las pruebas). **No** se hizo lo de esconder las misiones hasta la primera
  partida terminada: sin jugar no se cobra nada, y el objetivo de la función es justamente darle un objetivo a la
  primera partida.
- [x] El bestiario, con la misma vara. **Hecho el 20/9**: cada estrella paga la mitad de lo que dejan esas muertes, así
  que el tanque paga más que el caminante por el mismo escalón y la primera estrella baja de 200 a 110.
- [x] **Media — "Completa la oleada N" se cumple retomando.** **Arreglado el 20/9**: ahora cuenta cuántas oleadas se
  completaron hoy ("Completa 5 oleadas") y no a cuál se llegó.
- [x] **Media — a medianoche se pierden las misiones cumplidas sin cobrar.** **Arreglado el 20/9**: al cambiar el día
  se cobran solas antes de armar las nuevas, cofre incluido.
- [x] Baja — revivir al lado del jefe no daba aire. **Arreglado el 20/9** con `JefePatrones.Postergar` desde
  `PlayerHealth.Revivir` (2,5 s o los segundos de gracia, lo que sea más).
- [x] Baja — la línea de aviso del jefe quedaba medio bajo el piso. **Arreglado el 20/9**: `TransformZ` con el objeto
  rotado −90° en X; se ve plana sobre el piso, comprobado en el editor.
- [x] Baja — `RelojConfiable` hacía llamadas JNI de más. **Arreglado el 20/9**: `boot_count` una vez por sesión, sin
  reintentos si una lectura falla, y `Progreso.HoraConfiable` no toca JNI si no hay marca guardada.
- [x] Baja — pitch hasta 4. **Comprobado el 20/9 en el editor: Unity no lo corta** (acepta 4 y 5 por código; el 3 es
  el tope del slider del inspector). No hay nada que arreglar.
- [x] Baja — el cementerio se instanciaba entero cada noche. **Arreglado el 20/9**: se arma una sola vez y después
  se prende y se apaga. Como al combinarlo las piezas ya no se mueven solas, la primera noche sale cada lápida del
  piso y las siguientes sale el cementerio entero.
- [x] Baja — la cuenta regresiva de misiones. **Arreglado el 20/9**: usa `Progreso.AhoraConfiable()` y sólo arma el
  texto cuando cambia el minuto.
- [x] Baja — `Sprite.Create` sin `Destroy`. **Arreglado el 20/9** en las cuatro; la guía de la primera partida usa un
  solo sprite para los dos pulgares.

**Mejoras que propuso:**
- Invocación segura del jefe (lo de arriba, más validar que cada punto del anillo esté dentro del mapa).
- Carga que castiga y premia: un golpe propio de la carga (×2 o ×3, una vez), frenar en seco al chocar y dejar al jefe
  aturdido un instante (la ventana para pegarle).
- [x] Barra de vida del jefe grande arriba al centro, con su nombre y la muesca de la mitad. **Hecha el 20/9**
  (pedido de Ivan, "como en Minecraft"): `BarraDelJefe`, en el canvas del prefab MenuPausa.
- Postergar el ataque al revivir y la línea plana (S).
- Premios de misiones proporcionales al objetivo (lo de arriba).

## La derrota encima de la partida (23/9)

- [x] **Morir ya no cambia de escena** (pedido de Ivan): la pantalla de la derrota sale enseguida encima de la partida,
  que sigue andando, y detrás el mundo se va a gris en 5 s. Salió en dos vueltas con él: primero dejaba cinco segundos
  de gris sin pantalla y el fondo celeste tapaba el gris, y además congelaba el juego. Detalle en CLAUDE.md, La
  derrota encima de la partida; lo verifica el banco `PruebaDerrota` (22 chequeos).
- [x] **La horda festeja cuando el jugador muere** (elegido por Ivan). Se abre a los costados de la pantalla —el
  centro lo tapa la derrota, medido con la cámara— y salta con el puño en alto en oleadas, rugiendo las primeras tres.
  Del primer intento, festejando alrededor del cuerpo con saltitos, no se veía nada: todo quedaba detrás del texto.
- [x] **Los textos chicos de la derrota quedaban sobre la horda**: el cuerpo está en el centro de la pantalla y los
  zombis se amontonaban justo detrás de PUNTOS, RÉCORD y "te faltan N para…". **Resuelto con el festejo**: la horda
  se abre a los costados, más allá del renglón más ancho, y detrás de los textos queda sólo el cuerpo.
- [ ] Al rechazar el revivir, el velo oscuro de la ventanita se va de golpe y el mundo gris se aclara un poco antes de
  la derrota. Con el proveedor en `Nulo` (la versión de Play) no pasa; con la APK de prueba sí.
- [ ] Mirar en el teléfono cuánto gasta la derrota: antes era una escena de pura UI y ahora la partida sigue andando
  detrás, con el techo de zombis y el blit del gris, mientras el jugador decide.

## Animaciones de los zombis (22/9)

- [x] **Los zombis te pegan y se mueren con su animación.** Hasta hoy el `Zombi.controller` tenía **un solo estado**
  (correr en loop, sin transiciones) y un parámetro que no usaba nadie: los cinco zombis corrían para siempre, te
  pegaban corriendo y se morían corriendo, aunque `Z_attack_A` y `Z_death_A` venían en el pack, en el proyecto, sin
  usar. Ahora el controller lo arma **ShowBies > Animaciones > Armar el controller de los zombis**, y vive en
  `Assets/Animaciones/` (se movió con `MoveAsset`, que conserva el guid, así que los cinco prefabs no se tocaron).
  Lo difícil fue la muerte: el zombi volvía al pool en el mismo frame, así que hubo que separar "morir" de "apagarse"
  (`DejarDeContar` + `Morir`) para que el cadáver se quede 1,4 s desplomándose sin contar en `ZombisVivos` ni en la
  oleada. Dos bancos en play lo verifican: el golpe (99 % de los frames en Atacar después de pegar) y la muerte
  (`ZombisVivos` sin despegarse ni un frame, cadáveres que se van solos, oleada que sigue avanzando).
- [x] **El jefe tiene pose propia en cada patrón.** **Hecho el 22/9**, procedural sobre el hijo del modelo y en
  `LateUpdate` (después del Animator): se agazapa avisando la carga, va echado adelante embistiendo, se tambalea
  aturdido y se arquea hacia atrás al invocar. Lo que faltaba era justamente el aturdimiento: son 1,3 s en los que
  el jefe está indefenso y no lo decía nada. **ShowBies > Pruebas > Grabar al jefe** lo saca al lado del jugador para
  verlo sin llegar a la oleada 10.
- [x] **El tanque y el jefe caminan de verdad, y los cadáveres caen hacia donde les pegaste.** **Hecho el 22/9**: el
  estado de andar pasó a ser un blend de `Z_walk_rm` a `Z_run_rm` con el float `Ritmo` que fija cada prefab, y los
  lentos van en 0. Con el ciclo de correr a `Paso` 0,45 el tanque se veía como alguien corriendo en cámara lenta, no
  como algo pesado. Y `DanoZombi` recibe la dirección del golpe (la bala su forward, la granada del centro hacia
  afuera), así el cadáver sale despedido hacia allá en vez de caer siempre igual.
- [ ] Sigue sin usar `Z_idle_A` (quieto), que no tiene dónde ir mientras los zombis vayan siempre derecho al jugador.
- [x] **El daño del zombi entraba antes de que el brazo conectara** (lo vio Ivan el 23/9). **Arreglado el 23/9**: el
  zarpazo arranca al tocar, adelantado en el clip para dejar 0,2 s de anticipación, y el daño entra en el cuadro del
  impacto (0,37 s dentro de `Z_attack_A`, medido) si el jugador sigue al alcance del brazo. La embestida del jefe
  sigue pegando al chocar. El banco del golpe lo verifica: 207 de 207 golpes en el impacto, y con el bug de vuelta 0 de
  171.

## Revisión del 22/9 (diez agentes, en `REVISION.md`)

47 hallazgos, 32 sobrevivieron al verificador. El informe completo con archivo y línea está en `REVISION.md`.

- [x] **Cerrar el vídeo del revivir mandaba a la derrota en el acto.** **Arreglado el 22/9**: el callback de "sin
  premio" ya no es `Rechazar` sino `SinPremio`, que devuelve la ventanita con su tiempo. Verificado en play, y
  verificado también que la prueba detecta el bug (revirtiendo el arreglo a propósito da `escena=Perdiste`).
- [x] **Los tres exploits de economía.** **Arreglados el 22/9**, los tres con su prueba de regresión, y las tres
  verificadas volviendo a poner el bug (con los tres de vuelta, la suite da 4 fallas; sin ellos, 704 OK):
  - El tope de 3 vídeos por día se contaba **por lugar**, así que eran 3 de revivir + 3 del x2 de la derrota + 3
    del x2 de la diaria: nueve. `PuedeOfrecer` ahora pasa `Progreso.UsosDeHoyEnTotal()`.
  - La **recompensa diaria** resolvía el monto al cobrarla con la mejor oleada del momento, así que dejar la ventana
    sin cobrar, jugar hasta mejorar la marca y recién ahí tocar COBRAR era la jugada óptima: en la prueba, 8.400
    monedas en vez de 1.100. Ahora se congela con `Progreso.OleadaDeLaRecompensa(hoy)`, como las misiones y el
    desafío semanal.
  - Las misiones de **granadas y críticos** salían del número de oleada y no de lo que cuesta cumplirlas: la de
    granadas costaba 0,43 partidas con el premio de 2,5 (desde la oleada ~10, tirarlas al aire rendía más monedas
    por segundo que jugar bien) y la de críticos, 0,26 —y encima pedía 200 críticos a quien recién los compraba y le
    salían 7 por partida—. Ahora salen de `Economia.SegundosPorPartida` y `Economia.BalasPorPartida`, dos varas
    nuevas al lado de las que ya estaban.
  - La prueba que faltaba y que los habría visto: **comparar el costo de cada objetivo, en partidas, contra
    `Partidas(dificultad)`**. La que había sólo miraba que el objetivo creciera con la oleada, y los dos crecían.
- [x] **Los cuatro que seguían por tamaño.** **Arreglados el 23/9**, cada uno con su prueba, verificada volviendo a
  poner el bug:
  - La vida del HUD quedaba en negativo durante el ¡HAS MUERTO!: ya no baja de cero. El banco de la derrota lo mira con
    un golpe que se pasa (con el bug, el HUD decía −999920).
  - La misión del jefe decía "Derrota a un jefe" y pedía 2 o más: ahora dice cuántos. La prueba de lógica mira que el
    texto de cada misión diga cuánto pide.
  - En el teléfono el muñeco no disparaba nunca, porque la animación sólo la prendía el camino de PC. Ahora las dos
    plataformas pasan por `PlayerController.FijarDisparo`, y de paso en PC apretar sin balas y agarrar una caja sin
    soltar ya dispara (el 9 de la revisión). Banco nuevo, **Disparo con el joystick**: con el bug, 4 fallas de 8.
  - Los faroles no alumbraban el piso en Android: ahora cada uno pinta un charco de luz en el piso y su luz va sólo por
    vértice, igual en el editor que en el teléfono. **Fotos de los faroles** lo mide renderizando con la calidad del
    teléfono: bajo cada farol del cementerio el piso pasó de +0,005 a +0,12 de brillo.
  - Y uno que salió probando: al morir, el cuerpo seguía corriendo (y disparando) en el lugar detrás de la derrota,
    porque nadie apagaba la animación. Lo apaga `PlayerController.Soltar`, y lo mira el banco de la derrota.
- [ ] Lo que queda en `REVISION.md`: el cierre de medianoche que paga al precio de la oleada 0 (10), los carteles de
  misión y de capítulo que se pisan (12), el globo del idioma sin `PintarConTema` (13), el valor siguiente de la tarjeta
  en modo oscuro (14), el salto de rotación del jefe al terminar de invocar (15) y la suma de `ZombisPorPartida` (16).
- [ ] **Disparar corriendo no se ve.** El controller del muñeco (`TT_demo_male_A`, del pack) pasa a disparar sólo desde
  quieto, y de disparar no vuelve a correr hasta soltar: en el teléfono, donde se corre y se dispara a la vez, casi no se
  ve. Para que se vea siempre, una capa del torso y los brazos con el disparo (el avatar es humanoide, así que la máscara
  sale sola). Ojo al mirarlo: en el teléfono el cuerpo mira hacia donde camina y lo que gira hacia donde se apunta es el
  arma (`Gun`, un cubo chico), y el muñeco tiene prendido un bate en la mano (`w_baseballbat`).
- [ ] **El jugador no se cae al morir**: queda parado detrás de la derrota mientras la horda festeja. El pack trae
  `m_death_A`.
- [ ] Ver los faroles en el teléfono: están medidos con la calidad de Android, pero en el editor.

## Revisión de lo del 20/9 (dos agentes, esa misma tarde)

Arreglado en el momento: las misiones de furia, granadas y jefe tenían objetivo fijo y premio creciente (se cobraba el
premio de la oleada 45 tirando 25 granadas); el premio se calculaba con la mejor oleada del momento de cobrar, así que
convenía no cobrar nunca; en modo oscuro había texto casi blanco sobre la fila verde de una misión cobrada, sobre el
casillero dorado de la diaria y sobre el círculo del bestiario; los controles de la ventana de opciones eran blancos
sobre la crema; la barra del jefe se congelaba un segundo al cambiar de jefe; la cuenta regresiva hacía una llamada JNI
por frame; la ciudad salía con 16 luces en vez de 6; el decorado se instanciaba entero en el frame del cambio de
capítulo; la niebla quedaba prendida al volver al día; el reloj se apagaba para siempre con una falla puntual.

Lo que quedó anotado, de mayor a menor:

- [x] **La recompensa diaria quedó en la escala vieja.** **Arreglado el 20/9**: paga las partidas de `PartidasPorDia`
  con la vara de `Economia` (que ahora es de las tres), con los montos viejos de piso para que el arranque no cambie. En
  la oleada 45 el primer día pasa de 825 a 12.700.
- [ ] **Las estrellas del bestiario conviene no cobrarlas nunca**: el premio se resuelve al cobrar y crece con la mejor
  oleada, mientras los escalones son muertes absolutas. Arreglo: anotar la marca al ganar la estrella y pagar con esa,
  como ahora hacen las misiones.
- [ ] `Bestiario.MonedasPorTipo` copia a mano el promedio de `monedasMin/monedasMax` de los `.asset`. Si se toca el
  balance de un zombi, el premio miente en silencio: una prueba que cargue los `Enemy` con `AssetDatabase` y compare.
- [x] Quedaban `Sprite.Create` sin `Destroy` en seis pantallas: ~8 objetos por vuelta al menú. **Arreglado el 20/9**:
  cada una guarda sus sprites y los destruye con las texturas. El globo del idioma usa uno solo para el fondo y la
  sombra, que son el mismo dibujo.
- [ ] El campo del JSON pasó de `mejorOleadaDelDia` a `oleadasDelDia` sin subir la versión: al actualizar se pierde, en
  silencio, el avance de la misión de oleadas del día en curso.
- [ ] `PintarConTema.OnValidate` toma el color del `Graphic` como "el claro" cada vez que carga la escena en el editor:
  si alguien deja un color pisado, se sobrescribe el de siempre sin avisar.
- [x] La ventana de la diaria no se rearmaba al cambiar el tema, a diferencia de misiones y bestiario. **Arreglado el
  20/9**: se rearma en `Abrir`, y para poder armarla dos veces los dibujos se hacen una sola vez en `Start`.
- [x] El `Interruptor` no reflejaba un cambio hecho desde afuera. **Arreglado el 20/9**: en vez de una foto del valor
  recibe de dónde leerlo, y si no coincide corre la perilla sin volver a avisar.
- [x] La barra del jefe: el "Marco" quedaba entero fuera del rect de su propia raíz. **Arreglado el 20/9**: los hijos
  cuelgan del techo y la raíz mide lo que ocupan. Medido en play, el nombre y el marco quedan en los mismos píxeles
  que antes (−142…−194 y −195…−221), así que en pantalla no se movió nada.

- [x] **El desafío semanal.** **Hecho el 20/9**: `DesafioSemanal`, uno por semana de lunes a domingo, que cuesta
  quince partidas y paga la mitad de lo que dan esas quince (la vara de `Economia`, como todo lo demás). Se ve en una
  fila dorada arriba de las tres diarias, con los días que faltan; el progreso pasó a versión 5.

- [ ] **Co-op online.** Investigado el 21/9, está en `COOP.md`: se puede, el hosting es barato
  (~3.800 partidas de 4 jugadores gratis por mes en Relay), pero son 2-3 meses y hay que reescribir el 19 % del
  código — sobre todo los trece lugares que congelan el mundo con `Time.timeScale`, que en co-op no se puede.
  **El próximo paso es la prueba de rendimiento (fase 0, 1-2 días)**: 35 cápsulas con `NetworkTransform` entre dos
  teléfonos, a ver si el aparato aguanta. Esa medición decide todo y no caduca.

**Después:** el diseño de las cartas de 1 de 3 por oleada (ideas de Archero). El tercer capítulo
(la ciudad de noche, oleadas 21-30) se hizo el 20/9: `CapitulosDeEscenario` pasó a una lista de escenarios, así sumar un
cuarto es un elemento más en el array y su prefab.
