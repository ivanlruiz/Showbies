# Superauditoría del 29/9: informe

Ivan pidió una superauditoría completa de ShowBies. Esto es lo que salió, con el veredicto de cada hallazgo después de
que un escéptico intentara tumbarlo. Todo se hizo en solo lectura: nadie tocó el repo, ni abrió Unity, ni cambió git.

## Qué se miró

- **El código de HEAD** (`main` = `d17c32c`), leído sin Unity. Muchos valores viven en los `.unity`, `.prefab` y
  `.asset`: los números salen de lo serializado, no del valor por defecto del `.cs`.
- **26 frentes**, un agente cada uno. Por sistema: combate y disparo; zombis y horda; el jefe; oleadas y generación;
  jugador, vida, revivir y derrota; progreso y guardado; economía y premios; tienda y mejoras; anuncios; menú y
  ventanas; HUD y pantallas de partida; tutorial y primera vez; idiomas y textos; tema neón y UI armada en código;
  escenarios y noche; sonido y jugo. Transversales: estado estático y resets; tiempo, pausa y congelado; ciclo de vida
  de Android; rendimiento y memoria; build, Android y Play; trampas; pruebas; CLAUDE.md contra el código; herramientas
  de editor y bancos; código muerto y calidad.
- **Con más cuidado, lo que nunca se había auditado**: los siete commits de `e6556d9..HEAD`, del 25 al 27/9:
  `0a4cc87` (la partida en carbón neón y el segundo atlas de la fuente), `139698a` (la prueba de la nota del combo),
  `7276891` (el mundo de noche con neón y las píldoras redondas), `4c5a444` y `4127cb8` (pendientes), `8219044` (el
  festejo callado y lo que salió al probar la nube) y `d17c32c` (cinco bancos en play y `RespaldoDelBanco`).
- **Lo ya conocido no se repite**: cada frente leyó `pendientes.md` entero, incluidos los Descartados y la ronda
  `refutar:` del 26/9, y solo lo volvió a nombrar con evidencia nueva ("amplía").

**Cómo se hizo.** (1) **Buscar**: los 26 frentes, con su detalle en `buscar_<frente>.md`. (2) **Juntar**: un juez unió
duplicados, sacó lo ya conocido y fijó la gravedad con un solo criterio (`unicos.md`). (3) **Refutar**: un escéptico
por hallazgo de gravedad media y dos, con miradas distintas, para el único alto (`refutar_Hxx_*.md`); con dos miradas
gana la más desfavorable al hallazgo, siempre que traiga evidencia concreta. Los bajos no se refutaron. (4)
**Sintetizar**: este informe y `para_pendientes.md`. Todo está en `Builds/auditoria_29_9/`.

## Las cuentas

| paso | cuánto |
|---|---|
| hallazgos en bruto | 162 |
| únicos, después de unir 38 duplicados | 123 (1 alto, 22 medios y 100 bajos, según el juez) |
| ya conocidos, sacados | 1 (las balas que atraviesan al FASTER: `rendimiento#6` suma un motivo al barrido ya propuesto) |
| refutados en la ronda | 23 (los 22 medios y el alto) |
| miradas de escépticos | 24 (el alto tuvo dos): 16 confirmado, 7 parcial y 1 refutado |
| **veredicto por hallazgo** | **15 confirmados, 7 parciales y 1 refutado** |
| bajos sin refutar | 100 |
| medios sin refutar por el techo | 0 |

**Gravedad final de los 23 refutados**: ninguno alto, 13 medios, 9 bajos y 1 refutado. El único alto (H01) quedó en
media: la mirada del impacto trajo evidencia concreta de que todavía no está en Play.

| veredicto | media | baja | refutado |
|---|---|---|---|
| confirmado (15) | H03, H05, H06, H11, H13, H16, H17, H18, H20, H22, H23 | H12, H14, H15, H21 | |
| parcial (7) | H01, H10 | H02, H04, H07, H08, H09 | |
| refutado (1) | | | H19 |

Cambiaron de gravedad: H01 (alta → media), H02, H04, H07, H08, H09, H12, H14, H15 y H21 (media → baja).

## Lo más grave

1. **H01 + H05, la pausa**: el halo de neón de REINICIAR se lleva el borde de abajo de CONTINUAR, y REINICIAR borra la
   partida de oleadas sin preguntar. Vino con el neón del 25/9 y todavía no está en Play: es un bloqueante del
   próximo AAB.
2. **H06**: la tienda que se abre sola después de la diaria gasta 150 monedas en CRÍTICOS con un toque que era para la
   diaria, justo el día 1, y rompe la guía de la primera compra.
3. **H03**: desde cualquier reinicio del teléfono, adelantar el reloj da misiones y semanal nuevos, y el día se puede
   sortear.
4. **H17**: de noche la bala es un oliva sucio y la caja de vida queda más oscura que el piso.
5. **H18**: el jugador entra en los edificios de la ciudad y desaparece; los zombis los cruzan y aparecen encima.
6. **H16**: los zombis patinan (las piernas cubren del 16 al 36 % de lo que avanzan), el tanque también.
7. **H10**: con el techo de zombis lleno, la invocación del jefe hace el aviso entero y no sale nadie.
8. **H13**: en el teléfono, el botón de pausa tapa el cartel del capítulo, el nombre del jefe y el panel del tutorial.
9. **H22 + H23**: un banco cortado a mano secuestra el próximo Play y borra la oleada en curso del editor, y los
   bancos
   pisan escenas sin guardar.
10. **H11 + H20**: al jefe se lo mata sin que ataque, y en la ciudad las veredas tapan la línea de su carga.

## Lo que vino del tramo que nunca se había auditado

- **`0a4cc87`, el neón de la partida**: H01 (los halos que roban toques), H13 (el disco de la pausa pasó a 0,92 con
  anillo y ahora tapa), H21 (el panel del tutorial pasó a casi negro), la G de la granada (H24) y lo armado en código
  que quedó sin neón (H39).
- **`7276891`, la noche y las píldoras**: H17 (la luz de relleno solo para los personajes), `PonerLaNoche` sin guarda
  de escenas (H23), el menú sin la noche del cementerio (H103) y la niebla que no tapa el borde (H112).
- **`d17c32c`, los bancos**: H22, H23, H92, H96, H99 y H100.
- **`8219044`** borró de pendientes que los zombis patinan, sin resolverlo (H16).
- **De antes, que la auditoría del 24/9 no vio**: H12 (la viñeta no cambia desde `8b212f2`, del 14/9) y H14 (el verde
  es de `725d1db`, del 24/9; `7276891` solo volvió a guardar el menú).

## Por dónde empezar (sugerencia)

- **Antes del próximo AAB**: H01 y H05 juntos (con «El récord de una partida abandonada», que va en la misma llamada),
  H07 (versionCode 6 y la guarda), H08 (el formulario y la línea de la política, junto con el arreglo de "privado"),
  la guarda de H09, H82 y H83 (capturas nuevas sin el contador de FPS) y H114 (`pasos.md`).
- **Chicos y seguros**: H15 (una línea), H20 (una constante), H21 (un vector), H14, H12, H22 y H23 (el andamiaje de
  los bancos, que conviene hacer antes de volver a correr bancos).
- **Para decidir**: H03, H04, H08, H11, H18 y el paso del tanque (H16).
- **Para medir**: la lista del final.

---

## Confirmados

### Media

#### H03 — El reloj confiable solo se ancla al cobrar la diaria: desde cualquier reinicio del teléfono, adelantar el reloj da misiones y semanal nuevos

- **Veredicto**: confirmado. **Gravedad final**: media (igual). Confianza alta. Detalle: `refutar_H03_codigo.md`.
- **Evidencia**: la marca (`relojUtc`, `relojMs`, `relojArranques`) solo se escribe en `RegistrarRecompensaDiaria`
  (`Progreso.cs:353-357`), que solo llama `RecompensaDiaria.CobrarEl` (`:113`). `HoraConfiable` devuelve el reloj
  crudo en tres casos: sin marca (`:592`, nunca se cobró), si falla la lectura (`:596`) y si la marca es de otro
  arranque (`RelojConfiable.cs:53-54`). `MisionesDiarias.Asegurar` (`:78-102`) y `DesafioSemanal.Asegurar` (`:52-73`)
  solo comparan con el día guardado. El atrás cierra la diaria sin cobrar (`BotonAtrasMenu.cs:36-38`), y las
  misiones no necesitan el menú: `AvisoDeMisiones.Start` llama a `Asegurar` (`:61`) y `CerrarElDia`
  (`MisionesDiarias.cs:108-126`) cobra lo cumplido y el cofre. La "deuda" no lo frena: con la hora automática de
  vuelta, `Asegurar` sale temprano (`:91`) y las misiones del día futuro siguen valiendo.
- **Lo que sumó el escéptico**: quien nunca cobró la diaria no necesita ningún reinicio; y el día se puede **sortear**
  (`new Random(dia)`, `MisionesDiarias.cs:132`): abandonar un día no cuesta nada, así que se salta hasta que la
  difícil sea barata (la de monedas, o la de jefes, que ya se farmea). Lo que lo achica: los topes de vídeo hoy no
  pesan (proveedor Nulo, y la separación de 60 s va con `realtimeSinceStartup`), cuesta la diaria (0,25 a 2
  partidas-modelo por día) y el cambio de zona horaria da un solo salto.
- **Números** (modelo, sin medir): un juego de misiones paga 3,24 partidas-modelo y cuesta 2,5. En la oleada 40:
  misiones 111.150, semanal 257.300, diaria del día 7 68.600, y una partida real ≈85.800. Ganancia del tramposo sobre
  el legítimo, sin sorteo / con sorteo:

  | mejor oleada | 30 min/día | 1 h/día | 2 h/día | 4 h/día |
  |---|---|---|---|---|
  | 10 | +60 % / +96 % | +83 % / +124 % | +98 % / +141 % | +106 % / +151 % |
  | 20 | +7 % / +38 % | +40 % / +80 % | +65 % / +113 % | +82 % / +134 % |
  | 40 | −22 % / +36 % | −15 % / +26 % | +13 % / +67 % | +36 % / +100 % |

  El "+50 a +100 %" del hallazgo vale para la franja media; es de más para el avanzado que no sortea y de menos para
  el que sortea; quien juega poco pierde.
- **Arreglo recomendado**: anclar la marca en el primer `HoraConfiable` de cada arranque (sin marca, o de otro
  arranque) y no reanclar hacia adelante en el mismo (si se reanclara con la hora que tolera +2 h, cada guardado
  correría 1 h 59). Prueba: mismo arranque, +1 h 59, anclar, +1 h 59 otra vez, y el día no avanza. Se guarda bien
  (`Guardar` no tiene marca de sucio y `VigiaAplicacion` guarda al ir a segundo plano). **No lo cierra**: lo pasa a
  "un reinicio por salto" (y mata el sorteo, un reinicio por intento), pero un reinicio por hora de juego no frena al
  que ya cambia la fecha. **Decisión de Ivan**: si esa fricción alcanza, que es la postura escrita ("fricción, no un
  candado"). Tiene que salir junto con H70, porque anclar en el primer uso de cada arranque agranda el caso del
  teléfono que arranca con la hora mal, y con «Una fecha guardada en el futuro bloquea la diaria...». `auto_time`
  como alivio, sin verificar en modo avión.

#### H05 — REINICIAR de la pausa y la tecla R borran la partida de oleadas sin preguntar

- **Veredicto**: confirmado. **Gravedad final**: media mientras H01 siga abierto; con H01 arreglado, baja. Confianza
  alta. Detalle: `refutar_H05_codigo.md`.
- **Evidencia**: `MenuPausa.Reiniciar` (`MenuPausa.cs:89-94`) y la R (`RestartScene.cs:22-27`) llaman a
  `WaveManager.OlvidarPartidaSiEsOleadas` (`WaveManager.cs:28-33`), que pone la oleada y los puntos en curso en 0
  (`Progreso.cs:267-274`) y guarda. Aunque no guardara, no habría vuelta: al recargar, la primera vuelta de
  `WaveManager` guarda la oleada 1 (`:121-125`). La única guarda de la R es el vídeo o el ¡HAS MUERTO!, así que anda
  con la pausa abierta. En `MenuPausa.prefab`, `BotonReiniciar` (`:140`) está en y −90, entre CONTINUAR (60) y MENÚ
  (−240), y WaveMode no lo pisa. La F de la furia es la tecla de al lado (`Jugador.prefab:225`).
- **Lo que corrigió el escéptico**: la R es solo de PC (el objeto `RR` de `WaveMode.unity:1041-1073` lleva
  `ConditionalShow` solo para PC, y el HUD la anuncia con «R  REINICIAR»); en el teléfono el único camino es tocar
  REINICIAR sin querer, que es lo que agrava H01. La pérdida es la de una muerte (`PlayerHealth.Terminar` también
  olvida). Ivan pidió confirmación para SALIR, que no pierde nada.
- **Números**: rehacer las oleadas 1..N−1 (10 + 4n zombis, 0,35 s entre apariciones, 3 s de descanso): 950 zombis y
  6,5 min hasta la 20, 2.030 y 13,3 min hasta la 30, 2.720 y 17,6 min hasta la 35, 3.510 y 22,5 min hasta la 40
  (`Economia.SegundosPorPartida` da 14,1 y 23,5 min): 13 a 23 min, no "15 a 20". No es tiempo muerto: rehacerlas paga
  del 40 al 47 % de lo que paga por minuto la oleada de frontera. En la pausa, los botones miden ~8,5 mm de alto y el
  hueco entre ellos ~2,1 mm.
- **Arreglo recomendado**: confirmar solo si hay algo que perder (WaveMode y `Progreso.OleadaEnCurso > 1`); en el
  libre y el tutorial REINICIAR sigue inmediato. `ConfirmarSalir` no se reusa tal cual (copia `SelectorIdioma.panel`,
  que solo existe en el menú): una ventana propia con `ConstructorUI.VentanaNeon` en el canvas de la pausa, en tiempo
  sin escalar, con los textos en la tabla («¿EMPEZAR DE CERO? Perderás la oleada {0}») y SEGUIR grande y verde.
  **Trampa**: `MenuPausa.Update` convierte Escape (también el atrás de Android) en Reanudar; la ventana tiene que
  tomar Escape primero y `MenuPausa` no reanudar mientras esté abierta. La R abre la misma confirmación (o se
  mantiene ~0,6 s); no bloquearla en la pausa. Poner la confirmación antes de la misma llamada donde va
  `GuardarRecord` («El récord de una partida abandonada») y hacer los dos juntos. Prueba: con `OleadaEnCurso > 1`,
  REINICIAR no olvida sin confirmar y Escape con la ventana abierta no reanuda.

#### H06 — La tienda que se abre sola después de la diaria compra con los toques que eran para la diaria

- **Veredicto**: confirmado. **Gravedad final**: media (igual). Confianza alta. Detalle: `refutar_H06_codigo.md`.
- **Evidencia**: `TiendaMejoras.Abrir` (`:261-315`) solo pone `grupoPanel.alpha` en 0, y el fundido de `Update`
  (`:427-433`) sube el alfa sin tocar `interactable` ni `blocksRaycasts`. Los `CanvasGroup` están en 1/1
  (`Tienda.prefab:1665-1666`, `TarjetaMejora.prefab:1928-1929`) y Menu no los pisa; en uGUI el alfa no filtra
  raycasts. El botón de compra recibe el toque en `Visual/Fondo` (320x118). `IntentarComprar` no mira el tiempo;
  `ToqueQueFrena` no actúa porque `AcomodarFila` hace `StopMovement`; la guía no tapa nada. `enTienda` va daño,
  cadencia, críticos, vida: la fila abre en 0 con DAÑO a la vista. El día 1 la diaria paga 150 (el piso) y CRÍTICOS
  nivel 0 cuesta 150: después de cobrar siempre alcanza, y la flecha de la guía salta a ¡A JUGAR!
  (`GuiaPrimeraCompra.cs:130` y `:142`) porque `NuncaCompro` pasa a falso.
- **Lo que corrigió el escéptico**: hace falta un toque **nuevo**. El velo de la diaria tapa todo hasta que suelta
  `Ocupada`, y un toque que empezó sobre el velo no hace clic al soltarse sobre la tienda; los toques de los 1,55 s de
  espera los absorbe la ventana. Compra sin querer un toque que empieza entre ~1,55 y ~1,9-2,0 s después de COBRAR,
  en la zona de COBRAR: el que toca para cerrar el cartel de cobrado. El camino de VOLVER o del vídeo pide
  `PuedeOfrecer(regalo_x2)`: solo en la APK de prueba. El camino hermano (MEJORAS de la derrota sin diaria) es
  marginal: su centro cae debajo de la franja de compra salvo en 4:3.
- **Números**: el menú es 1920x1080 con match 0 y la tienda con match 0,5. En 16:9, COBRAR tapa el 97 % del botón de
  CRÍTICOS y el centro de VOLVER cae en VIDA; en 20:9, el 82 % (VIDA, 49 %); en 21:9, el 79 %; en 4:3, el 67 % de
  CRÍTICOS y el 43 % de CADENCIA. La tienda abre a ~1,55 s del COBRAR (1,3 s de espera y 0,25 de salida), el panel se
  funde en 0,15 s y la fila entera entra en 0,72 s (no ~0,5).
- **Arreglo recomendado**: no con `blocksRaycasts = false` (deja pasar los toques al canvas "Main Menu" de abajo), ni
  con `interactable` o una guarda de tiempo en `IntentarComprar` (el `Button` decide al soltar y no sabe cuándo bajó
  el dedo). Decidirlo al apoyar: en `ToqueQueFrena.OnPointerDown`, `eventData.eligibleForClick = false` si
  `FilaEnMovimiento` o si la tarjeta todavía entra (exponer `entrando`, o `tiempoAbierta` < ~0,45 s). Hereda lo ya
  anotado: el botón igual suena y se aprieta. Prueba: `PruebaDiaria` cobra con `onClick.Invoke()` (`:352`) y no lo
  ve; el caso nuevo arma un `PointerEventData` en el centro de COBRAR y, a ~0,05 s de `Abrir`, hace
  `EventSystem.RaycastAll` y `pointerDown`, `pointerUp` y `pointerClick`: `Progreso.Nivel("criticos")` sigue en 0; de
  control, el mismo toque a ~1 s compra.

#### H11 — Al jefe se lo puede matar desde fuera de su ventana de ataque, sin que ataque nunca

- **Veredicto**: confirmado. **Gravedad final**: media (igual). Confianza alta. Detalle: `refutar_H11_codigo.md`.
- **Evidencia**: `JefePatrones.Update:352` arranca un patrón solo si toca y `CercaDelJugador()`, que pide estar a 15 m
  o menos **y** `EnPantalla()` con el pivote (`:455-478`). La carga y la invocación pasan por ahí; la furia no fuerza
  ataques, `WaveManager` no reubica al jefe y el zarpazo es solo por contacto. `margenEnPantalla` no está serializado
  en `ZombiBOSS.prefab:197-224` (que todavía guarda un `anchoLinea` que ya no existe): vale (0,08; 0,1). La cámara, en
  las tres escenas, en (0, 12, −3,9), 70° y 60° vertical. Las balas: `velocidadBala` 11 (`WaveMode.unity:694-695`) ×
  `lifeTime` 2 (`Bullet.prefab:112`) = 22 m, y atraviesan paredes. El jefe camina a 2 m/s y el jugador a 15. El
  comentario de `:468-472` llama "lo normal al escaparle" al jefe a 7-15 m por debajo y da por hecho que "ataca apenas
  entra". La banda de 15-22 m, donde nunca atacó, es anterior; la ventana del 25/9 (`c429e59`) bajó el borde a 4,7-6
  m.
- **Números**: la ventana del pivote va de −4,73 a +6,00 m (±9,23 m al costado en 16:9 y ±11,54 en 20:9); alguna
  parte de la cápsula se ve de −6,01 a +10,40 m. El jefe de la oleada 10 tiene 1.279 de vida: con 20, 40 u 80 de
  daño por segundo muere en 64, 32 o 16 s. Qué parte del disco de 1,5 a 22 m cubre el ataque: hoy 13 % (16:9) y 16 %
  (20:9); antes del 25/9, 46 %; la de hoy o a menos de 8 m, 16-19 %; a menos de 10 m, 21-24 %; el cuerpo entero sin
  margen, 27-30 %.
- **Arreglo recomendado**, **para decidir jugando** (dentro de «El jefe en el teléfono», que amplía): medir la ventana
  con el cuerpo o "atacar siempre dentro de 8 m" no cierran la banda de 15-22 m. La cierran que camine más rápido
  fuera de cuadro, o dejar que invoque fuera de cuadro (sin el rugido si no se lo ve) y atar a la pantalla solo la
  carga, que es la que necesita que se lea la pose. Además, `PruebasMejoras.cs:2369-2374` debería medir qué fracción
  del disco de 22 m queda cubierta, no cuatro puntos sueltos.

#### H13 — El botón de pausa del teléfono se monta sobre lo que va arriba al centro

- **Veredicto**: confirmado. **Gravedad final**: media (por el cartel del capítulo; el jefe y el tutorial solos serían
  baja). Confianza alta. Detalle: `refutar_H13_codigo.md`, con reconstrucciones en `refutar_H13_capitulo_*.png` y
  `refutar_H13_jefe.png`.
- **Evidencia**: el disco (130x130 en −24: de 24 a 154 u desde arriba, x ±65) tiene `ConditionalShow` solo para
  Android (`MenuPausa.prefab:2104-2105`) y nada lo esconde. Los dos canvas escalan igual (match 0,5) y la pausa va
  encima (orden 10). (1) El cartel del capítulo (`CapitulosDeEscenario.cs:485-495`, centro en +385 del canvas del HUD,
  84 pt) sale en el descanso de las oleadas 11, 21 y 31 durante 3,2 s y **también al retomar** en la 11 o más (`:179`
  está fuera del if/else). (2) La barra del jefe: `6b8ea40` subió `desdeArriba` a 170 solo en el `.cs`, con el
  tooltip «Debajo del boton de pausa», y el prefab sigue en 100 (`MenuPausa.prefab:2364`); `a20151c` lo midió en play
  con ese 100. (3) `PanelInstruccion` del tutorial (`Tutorial.unity:726-727`, en y −230, 170 de alto). Desde `0a4cc87`
  el disco pasó de un gris al 45 % sin anillo a alfa 0,92 con el anillo celeste: desde ahí tapa de verdad.
  `ProbarAvisosSinPisarse` (`PruebasMejoras.cs:1740`) nunca lo compara con el botón.
- **Números**: el cartel mide 142,9 u; las mayúsculas de la línea 1 caen en 95-158 (16:9), 64-127 (18:9), 38-101
  (20:9) y 27-89 (21:9): entera dentro de 24-154 en todos los teléfonos (solo se salvan 16:10 y 4:3). En 20:9, de
  «CAPÍTULO 2» se tapan la Í (95 %), la T y la U (100 %) y la L (90 %). El jefe: letras en 151-183 (con el 170 del
  código empezarían en ~221); se tapan ~3 u en x ±20: queda pegado. En el libre sale un jefe cada 30 s. El tutorial:
  el borde del panel en 145-149 y el del anillo en 150,6-154, se tocan en cualquier proporción. Entre el cartel de la
  oleada con bono y el botón quedan 131 u en 16:9, 74 en 20:9 y 63 en 21:9: el capítulo (143) no entra en ninguno.
- **Arreglo recomendado**: capítulo: un `CanvasGroup` en `BotonPausa` que baje el alfa a ~0,25 mientras dura el cartel
  (tocable igual), o «CAPÍTULO N · NOMBRE» como primer renglón del cartel de la oleada (que entre con el bono). Jefe:
  `desdeArriba` ~140 en `MenuPausa.prefab:2364` y el mismo valor por defecto en el `.cs`. Tutorial:
  `PanelInstruccion` en y −260/−265 (con 150 de alto en −230 no alcanza) y mirar el combo del tutorial al bajarlo.
  Prueba: que `ProbarAvisosSinPisarse` lea del prefab los rects de `AreaSegura/BotonPausa` y de su halo, en vez de un
  "24-178" escrito a mano, y los compare con el capítulo, `barra.TechoDesdeArriba` y el panel del tutorial; habría
  agarrado el 100.

#### H16 — Los zombis patinan: sus piernas van de 3 a 6 veces más lento que lo que avanzan

- **Veredicto**: confirmado, y el tanque también. **Gravedad final**: media (igual). Confianza alta. Detalle:
  `refutar_H16_codigo.md`.
- **Evidencia**: parseando los FBX, `Z_run_rm` avanza 2,000 m en 0,667 s (3 m/s a escala 1) y `Z_walk_rm` 1,000 m en
  1,0 s. El retargeting humanoide no cambia la cuenta (la T-pose es la misma en los cuatro). Los clips no llevan root
  motion y el zombi se mueve con `linearVelocity = forward × enemyType.velocidad` (`EnemyController.cs:805-807`). El
  estado Andar usa `Paso` como velocidad. `Zombi.prefab` y `ZombiRapido.prefab` no serializan `velocidadDeAnimacion`
  (vale 1), el FASTER tiene 2,5 (`ZombiFASTER.prefab:149`), el tanque 0,8 con `Ritmo` 0 y el jefe 0,55. La misma
  cuenta
  (`JefePatrones.PasoParaCorrer`) calibra la carga del jefe, que se grabó el 27/9 sin patinar.
- **Lo que sumó el escéptico**: **el tanque también patina**, y es el más visible (grande, lento, mucho tiempo en
  pantalla); el jefe persiguiendo, un poco. **El fondo del menú patina igual** (`FondoMenu.cs:91-94` y `:242-243` leen
  `velocidadDeAnimacion` por reflexión). **Ya se sabía y se perdió**: `e09c0d6` anotó que "los otros zombis patinan
  entre 1,5 y 6 veces" y `8219044` lo borró el 27/9 al reescribir el ítem del jefe. CLAUDE.md dice que
  `velocidadDeAnimacion` ajusta el paso a lo que camina cada uno, y es falso. Atenuante: bajo fuego el empuje de PhysX
  baja la velocidad real, pero el que se acerca sin recibir tiros va a velocidad plena.
- **Números**:

  | zombi | `Paso` que pide | que tiene | los pies cubren | ciclos/s hoy → completo |
  |---|---|---|---|---|
  | normal (5 m/s, modelo a 0,6) | 2,78 | 1 | 36 % | 1,5 → 4,2 |
  | rápido (9 m/s, 0,48) | 6,25 | 1 | 16 % | 1,5 → 9,4 |
  | FASTER (12 m/s, 0,36) | 11,1 | 2,5 | 22,5 % | 3,75 → 16,7 |
  | tanque (3 m/s, caminando, 1,2) | 2,5 | 0,8 | 32 % | |
  | jefe persiguiendo | 0,83 | 0,55 | 66 % | |

  En pantalla (~74 px/m a 1080), el piso se desliza bajo el pie a ~235 px/s en el normal y ~560 en el rápido. Con el
  arreglo completo, el FASTER haría 3,6 cuadros por ciclo a 60 FPS y 1,8 a 30: estroboscopio.
- **Arreglo recomendado**: una función pura `EnemyController.PasoPara(velocidad, ritmo, escalaDelModelo)` con el
  avance de cada clip (3 m/s correr, 1 m/s caminar) y un **tope en `Paso` 3-4** (no 4-5, por los teléfonos a 30 FPS),
  usada en `OnEnable` y en `FondoMenu`; o serializar el `Paso` en los prefabs (normal 2,78, rápido 4, FASTER 4). Con
  tope 4 el normal queda completo, el rápido cubre el 64 % y el FASTER el 36 %; el FASTER no tiene arreglo completo
  sin tocar su escala o su velocidad. **El tanque es una decisión de Ivan**: caminando pide 2,5 (una marcha rápida,
  contra el "pesado" que se buscó), corriendo pediría 0,83 (la cámara lenta que se rechazó); o aceptarlo, o un
  término medio con `Ritmo` 0,3-0,5, o bajarle la velocidad. Además, una prueba como la del jefe
  (`PruebasMejoras.cs:2316-2333`), verlo con Grabar animaciones y corregir la frase de CLAUDE.md.

#### H17 — De noche las balas, las cajas y la granada quedan fuera de la luz de relleno y se ven oscuras

- **Veredicto**: confirmado. **Gravedad final**: media (igual). Confianza alta. Detalle: `refutar_H17_codigo.md` y
  `refutar_H17_contraste.py`.
- **Evidencia**: el `Relleno` es direccional, por vértice y con `m_CullingMask 256` (solo la capa 8):
  `WaveMode.unity:3036-3083`, igual en `ShowBies1.unity:3910-3948` y `Tutorial.unity:4936-4974`; lo demás queda con la
  luna (×0,5) y el ambiente (0,15; 0,19; 0,30). `PonerEnLaCapa` solo se llama desde `ArmaEnLaMano:82`,
  `PlayerHealth:81`, `Moneda:209` y `EnemyController:441`, y saltea los renderers con collider (`Personajes.cs:20`):
  `Bullet.prefab` (capa 7), `PUBalas`, `PUVida`, `PUArma` (capa 0) y `Granada` tienen el collider en la raíz. No hay
  emisión: `Bullet.mat` es Standard (1; 0,879; 0) con `_EmissionColor` en 0 y sin keywords; `PUVida.mat` rojo y
  `balas.mat` crema, Standard; `PUArma` y `Granada`, el Default-Material. Los faroles con luz de verdad son 4, de 7,2
  m: un ~8 % del mapa. En el teléfono se ve igual que en el editor (la única luz por píxel es la luna). Nada dice que
  sea a propósito: `7276891` solo habla de los personajes, y CLAUDE.md pide que "una caja" que tenga que leerse de
  noche vaya a la capa 8.
- **Números**: el modelo de luz del escéptico reproduce cuatro capturas con 3 niveles de error o menos. La bala de
  noche, de arriba, da (79, 86, 13), no el (92, 99, 0) del hallazgo. Contra el piso de noche (23, 51, 48):

  | qué | de día | de noche |
  |---|---|---|
  | bala | 2,52:1 | 1,69:1 (L* 34) |
  | caja de vida (79, 8, 13) | 1,35:1 | 1,14:1, **L* 15, más oscura que el piso** |
  | caja de arma y granada | 2,89:1 | 2,21:1 |
  | caja de balas | 2,81:1 | 2,03:1 |

  Con el gris de poca vida en 0,5, la diferencia de color de la caja de vida baja a ΔE 26. Con `Unlit/Color` la bala
  quedaría en (255, 224, 0), 10,3:1.
- **Arreglo recomendado**: la bala **por el material**, no por la capa (la capa 7 no choca con Player y eso no se
  toca): Standard con emisión amarilla (la keyword `_EMISSION` en `m_ShaderKeywords`: poner solo `_EmissionColor` en
  el YAML no la prende) o `Unlit/Color`, que pierde la sombra, lo mismo que pide «Las balas» de rendimiento: hacer los
  dos juntos. `Bullet.mat` también tiñe el brillo de `PUBalas`. Las cajas y la granada: **la raíz entera a la capa 8
  en el prefab** (la fila de la 8 en `DynamicsManager.asset` es igual a la de la 0, nadie usa `IgnoreLayerCollision`,
  los raycasts de `Moneda:253` y `JefePatrones:607` usan `~0` e ignoran triggers). La variante del hallazgo, el
  collider en un hijo, **rompe el agarre** (`CompareTag`, `SetActive(false)` y `Destroy` sobre `other.gameObject`).
  Si se hace con emisión, `PUArma` y `Granada` necesitan un material propio. Verificarlo con una foto con la calidad
  del teléfono, antes y después, y otra con el gris de poca vida en 0,5.

#### H18 — El jugador y los zombis entran en los edificios de la ciudad y desaparecen bajo el techo

- **Veredicto**: confirmado. **Gravedad final**: media (igual). Confianza alta. Detalle: `refutar_H18_codigo.md`.
- **Evidencia**: `ConstructorEscenarios.Local` (`:906`) y `Pieza` (`:861`) borran el collider; `Ciudad.prefab` tiene
  12
  grupos `Edificio` y ningún collider, y ningún script agrega uno. Las paredes invisibles están en ±49,2-49,7 y los
  edificios van de 30,5 a 41,5 m. `Pared.mat` es Standard opaco, no hay shader de silueta, y la cámara (a 12 m, 70°)
  queda siempre afuera de la caja: una caja convexa opaca tapa todo lo que tiene adentro. La ciudad es
  `escenarios[2]` de `Capitulos` (oleadas 21-30, 51-60...). `Tapado` solo lo leen `PowerUp`, `Moneda` y las pruebas.
- **Lo que corrigió y sumó el escéptico**: los zombis **no nacen** adentro (los `spawnPoints` son (0, 35), (35, 0),
  (0, −35) y (−35, 0), en la calle), pero los **cruzan** en línea recta: con el jugador en (36, 19) y un zombi desde
  (35, 0), atraviesa el edificio de (36, 12), aparece a 1,5 m y pega a los 0,2-0,3 s (en la pradera se vería 1,2 s
  antes). Además: en la oleada 30 los invocados del jefe pueden nacer adentro (`PuntoDelAnillo` no mira `Tapado`), el
  jefe puede avisar la carga desde adentro (`EnPantalla` no mira la oclusión), los números de daño (shader overlay)
  se ven a través del techo, y si el jugador muere adentro la cámara corrida muestra un cuerpo que no se ve.
- **Números**: 12 cubos de 11 × 11 m y 3,70-6,37 m de alto; huellas 1.452 m², el 14,9 % de los 9.773 m² entre
  paredes (el 15,2 % de las posiciones del centro del jugador, el 12 % entero adentro; con la franja que tapa el
  techo, el 17,4 %). El jugador mide ~2 m, menos que el edificio más bajo.
- **Arreglo recomendado**: **para el jugador** (paso claro): un `BoxCollider` por edificio en la capa 9 (libre) que
  choque solo con la 6; las balas (colliders sin Rigidbody) y los zombis (capa 0) pasan igual, y el raycast de las
  monedas (`Moneda.cs:253`) lo toma como pared; de paso `RadioLibre` deja de poner invocados adentro. Tres ajustes:
  correr al jugador a mano fuera de la huella antes de prender los colliders (sin la franja de `Tapado`, con 0,72 m
  en X y 0,5 m en Z de margen: PhysX lo expulsaría hasta 5,5 m en un paso), prenderlo en `Aterrizar` (cubre la
  salida animada y el retomar), y pruebas de la capa, la matriz y la huella. **Para decidir**, porque eso deja a los
  zombis cruzando escondidos y al jugador frenado contra la pared: (a) sacar los edificios fuera de las paredes (|x| o
  |z| > 50), que cierra todo y deja `Tapado` vacío; o (b) desvanecer el techo del edificio que tiene a alguien adentro,
  que choca con el static batching.

#### H20 — En la ciudad, las veredas tapan la línea de la carga y el anillo de la invocación del jefe

- **Veredicto**: confirmado, y la granada también. **Gravedad final**: media (igual). Confianza alta. Detalle:
  `refutar_H20_codigo.md` y `refutar_H20_montecarlo.py`.
- **Evidencia**: `ZombiBOSS.prefab:221` usa Sprites-Default (el mismo material de la mancha de sangre), en la cola
  transparente, sin escribir profundidad pero con el ZTest de siempre; el código solo cambia colores. Las 16 losas
  de vereda (14 × 0,14 × 14, a y 0,07) y los 64 cordones (0,18) son Standard opacos. La cinta queda plana, toda a y
  0,06 (`JefePatrones.cs:633` la línea, `:696` el anillo). Nada saca al jefe de las manzanas. El jefe de la 30 (y de
  la 60 y la 90) pelea en la ciudad entera: la oleada no pasa a 31 hasta que muere. El proyecto ya subió la mancha y
  los charcos a 0,2 por esto mismo.
- **Lo que sumó el escéptico**: **el anillo de la granada tiene lo mismo**: `Granade.DibujarAnillo` (`Granade.cs:117`)
  lo pone a y 0,05 con el mismo material (`Granada.prefab:229`), para el radio de la explosión (`:104`) y el anillo de
  puntería (`PlayerController.cs:320`), en las diez oleadas de la ciudad. Y un resto: `anchoLinea` 1.4
  (`ZombiBOSS.prefab:203`) ya no existe en el código.
- **Números**: la cinta mide 2,88 × 17,88 m y el anillo 0,35 m de ancho, de 7 a 3,5 m de radio. Las veredas cubren el
  34,1 % del piso (42 % dentro de ±44 m, 51 % alrededor del cruce). Modelo sin medir, 6.000 cargas por caso: con el
  jugador en todo el mapa, el 86,5 % de las cargas pierde más de 0,5 m², el 36 % pierde la mitad y el tramo donde está
  parado el jugador queda tapado el 43 %; cerca del centro, 89,6 %, 42 % y 49-50 %. El anillo con radio 7 tiene
  algún tramo tapado el 98,5 % de las veces.
- **Arreglo recomendado**: una constante que la prueba pueda leer, `public const float AlturaDelAviso =
  ManchaDeSangre.AlturaSobreElPiso;` (0,2), usada en `:633` y `:696` y sumada al chequeo de
  `PruebasMejoras.cs:1321-1340` (que ya calcula el techo de las manzanas, 0,18); lo mismo en `Granade.DibujarAnillo`.
  Costo, el ya aceptado con la mancha: la cinta pinta los 20 cm de abajo de lo que la pisa. No pelea con los charcos
  (ninguno escribe profundidad). Descartado `ZTest Always` (la dibujaría encima de los personajes). Para
  reproducirlo en el editor: `oleadaEnCurso` 30 en `progreso.json` y OLEADAS.

#### H22 — Un banco en play cortado a mano queda armado y secuestra la próxima partida del editor, sin respaldo

- **Veredicto**: confirmado, y más amplio. **Gravedad final**: media (solo editor, pero destruye en silencio el
  progreso del editor y los informes). Confianza alta. Detalle: `refutar_H22_codigo.md`.
- **Evidencia**: ningún código limpia las claves `ShowBies.*` de `SessionState` fuera de `PruebaDiaria.cs:830-855`;
  salvo Diaria, ningún banco reacciona a salir de play (`PruebaTienda.cs:189-196` solo mira `.olvidarProgreso`). Con
  `m_EnterPlayModeOptionsEnabled: 0` (`EditorSettings.asset:26`) cada Play recarga el dominio: los estáticos vuelven
  a su valor inicial y `.empezo`, `.listo` y `.desde` siguen. `RespaldoDelBanco.cs:111-114` y `:155` devuelve el
  progreso y borra la marca al volver a edición: el banco colgado corre sin respaldo. Con el proveedor Nulo,
  `TakeDamage` lleva a `Terminar` en el mismo cuadro. Derrota, confirmado entero: la vida al 10 %, cura 1,8 s, lo
  mata,
  `TerminarPartida`, `OlvidarOleadaEnCurso` y `Guardar` sobre el progreso real, y el OTRA VEZ de los 6,8 s deja la
  oleada 1 guardada; informe HAY FALLAS.
- **Lo que corrigió y sumó el escéptico**: MuerteAnimada mata zombis solo si el Play nuevo llega antes de 55 s;
  GolpeAnimado también corta el Play en el primer cuadro (no estaba nombrado); `GrabarDisparo.cs:56` tampoco se deja
  volver a arrancar; PruebaDisparo corre casi una prueba nueva. **Nuevo, una cascada**: un banco colgado que termina
  en el primer cuadro corta el Play del siguiente banco, al que `RespaldoDelBanco` también le borra la marca.
- **Números**: con el editor abierto hace minutos, Tienda (más de 120 s), Tutorial (300), ModoLibre (600 o 30) y
  MenuYTienda (240 o 40) terminan en el primer cuadro; GolpeAnimado con 45 s o más; MuerteAnimada mata un zombi cada
  0,35 s hasta los 55 s (volviendo a Play a los 20 s, unos 100 en 35 s); Derrota, con más de 40 s, lo mata apenas
  termina la cura. Las 12 claves coinciden con "ShowBies." más el nombre que se le pasa a `Guardar`.
- **Arreglo recomendado**: la regla "**un banco sin respaldo no corre**": `RespaldoDelBanco.EsDe(banco)` (verdadero si
  la marca existe y lleva ese nombre) y, en cada `Tick`, después de `isPlaying`, si no es suyo, `SetBool(Clave,
  false)`, `LogWarning` y salir. Durante el play la marca existe, porque `Guardar` la escribe en edición y `Restaurar`
  no corre en play. Cubre el Stop, la cascada, el error de compilación (que no pasa por `EnteredEditMode`) y
  cualquier camino futuro. La otra forma: limpiar `Clave`, `.empezo`, `.desde` y `.listo` adentro de `Restaurar`, sin
  tocar `.olvidarProgreso`. Además, un `LogWarning` en los `Arrancar` que hoy se niegan callados (Disparo,
  MenuYTienda, ModoLibre y GrabarDisparo) y la guarda de play de `PruebaTienda.cs:202` en Derrota, GolpeAnimado,
  MuerteAnimada, GrabarJefe y GrabarAnimaciones (llamados en play prenden `runInBackground` sin devolverlo, y los dos
  Grabar borran su carpeta antes del `OpenScene`).

#### H23 — Los doce bancos y «Poner la noche» abren escenas con `OpenScene(Single)` sin mirar si hay cambios sin guardar

- **Veredicto**: confirmado. **Gravedad final**: media (solo editor, pero pierde trabajo en silencio). Confianza alta
  en
  lo central. Detalle: `refutar_H23_codigo.md`.
- **Evidencia**: en `Assets/Editor`, `isDirty` solo aparece en `ConstructorNeon.cs:47` y `:164`; no hay
  `SaveCurrentModifiedScenesIfUserWantsTo`, `SaveOpenScenes`, `GetSceneManagerSetup` ni `RestoreSceneManagerSetup`, y
  `RespaldoDelBanco.cs` no nombra ninguna escena. Camino: ShowBies1 sucia → Tutorial (play) → `PruebaTutorial.cs:175`
  abre Tutorial en Single, que cierra ShowBies1 sin preguntar → el cambio se pierde. `PonerLaNoche`
  (`ConstructorEscenarios.cs:922-1022`) solo tiene la guarda de play: abre las tres escenas de juego en Single
  (`:938-940`),
  las relee de disco, las guarda (`:1017-1018`) y termina en el menú (`:1020`).
- **Lo que corrigió y sumó el escéptico**: la documentación de Unity **no** dice que `OpenScene` no pregunte (solo
  "Closes all current open Scenes and loads a Scene"); lo dan por hecho `ConstructorNeon` y la comunidad.
  `ConstructorNeon` no sirve de molde: `VestirMenu` perdona justo al menú sucio (`:47`) y después lo reabre de disco
  (`:53`) y lo guarda; las dos guardas miran solo la escena activa. **Lo más filoso**: un agente toca WaveMode por
  unity-mcp sin guardar y corre un banco para verificarlo; el banco la recarga de disco, prueba la versión vieja,
  escribe TODO OK y el cambio desaparece: una verificación falsa. Varios bancos tocan cosas antes del `OpenScene`
  (`PruebaTienda.cs:213-214` reinicia el progreso, los Grabar y `PruebaDerrota.Grabar` borran su carpeta), así que la
  guarda tiene que ir primera. `FotosDeLosFaroles` y `LeerEscena` abren en Additive y están bien.
- **Números**: 12 archivos con `OpenScene` en Single, 14 entradas de menú; `PonerLaNoche` hace 4 `OpenScene` y 3
  `SaveScene`. Nunca auditados: los 5 bancos de `d17c32c` y `PonerLaNoche` (`7276891`).
- **Arreglo recomendado**, sin ventanas: un ayudante común (`EscenasSinGuardar.Hay(quien)`) que recorra todas las
  escenas cargadas y, si alguna está sucia, dé `LogError` con la lista y cómo salir sin clics
  (`EditorSceneManager.SaveOpenScenes()` o reabrirla), llamado como primera instrucción de las 14 entradas de los
  bancos, de `PonerLaNoche` y de las dos de `ConstructorNeon` (en lugar de su guarda, sin la excepción del menú). En
  `PonerLaNoche` y `VestirPartida`, volver a lo que estaba abierto con `GetSceneManagerSetup` /
  `RestoreSceneManagerSetup`. Mejor en el andamiaje común de H22. Para que no vuelva: un chequeo en la prueba de
  lógica
  que falle si un `.cs` de `Assets/Editor` llama a `OpenScene(` sin `Additive` ni el ayudante.

### Baja

#### H12 — El borde rojo del daño va dentro del área segura y se corta en recto del lado de la cámara del teléfono

- **Veredicto**: confirmado. **Gravedad final**: baja (era media): solo visual, dura 0,4 s y queda en la orilla de un
  lado. Confianza alta. Detalle: `refutar_H12_codigo.md`.
- **Evidencia**: `androidRenderOutsideSafeArea: 1` (`ProjectSettings.asset:71`, desde 2022), pantalla completa y solo
  horizontal. `VinetaDanio` cuelga de `CanvasHelper` en las tres escenas (`WaveMode.unity:418-445`; ShowBies1 y
  Tutorial con `m_Father 1052123399`), con anclas 0-1 y sin máscara; `CanvasHelper.cs:16-29` ajusta las anclas a
  `safeArea`. El `SetResolution` a 0,75 no los desfasa. `VinetaDanio.cs:126` no existe (el archivo tiene 83 líneas).
  La viñeta no cambia desde `8b212f2` (14/9): no viene del neón, la auditoría del 24/9 no la vio. La viñeta va detrás
  del HUD (es el primer hermano), no encima como dice la clase. El cartel de la oleada (bajo `CanvasHelper/Textos`) y
  la barra del jefe se centran en el área segura; el del capítulo (`CapitulosDeEscenario.cs:485`) y el de misión
  (`AvisoDeMisiones.cs:201`) en el canvas raíz.
- **Números**: alfa del rojo en el corte: 0,382 a media altura, 0,522 a un cuarto, 0,637 a un octavo y 0,697 en la
  esquina; con la furia, 0,134 y 0,244; a los 0,2 s de un golpe, 0,19. La franja cortada, sin medir: 3,3-5,8 % del
  ancho en 2400x1080, 5,0-8,8 % en 1600x720 y 2,6-4,5 % en 3120x1440; el desfase del cartel, 27-94 u.
- **Arreglo recomendado**: junto con «El borde rojo del golpe que mata se corta enseguida»: la viñeta en su propio
  objeto raíz, con un Canvas overlay sin área segura, detrás del HUD, y que `EsconderElHud` la deje desvanecerse. Lo
  mínimo, sin tocar las escenas: en `VinetaDanio.Awake`, `SetParent` al `rootCanvas`, `SetAsFirstSibling()`, anclas
  0-1 y offsets 0. Opcional: los carteles en un mismo marco.

#### H14 — MODO LIBRE bloqueado: píldora gris del tema viejo con el halo verde de «jugar» y el texto verde oscuro

- **Veredicto**: confirmado. **Gravedad final**: baja (era media): cosmético, el botón tiembla y se explica solo y el
  texto pasa el 3:1 de texto grande. Confianza alta. Detalle: `refutar_H14_codigo.md`.
- **Evidencia**: en `Menu.unity`, `GameModesMenu/FreeMode`: `colorBloqueado` serializado (0,45; 0,45; 0,52) en
  `:4771`, el `Fondo` verde neón en `:262`, la `Sombra` (`NeonPildora`) (0,224; 1; 0,533; 0,5) en `:6129`, el icono en
  `:2055` y el texto en `VerdeTexto`. Los tres verdes los puso `ConstructorNeon.VestirBoton` (`725d1db`, vuelto a
  guardar en `7276891`). `BotonModoLibre.Pintar` (`:43-67`) solo cambia `fondo.color` (`:50`) y el texto; `PintarHalo`
  solo se llama desde `ConfirmarSalir` y `VentanaMisiones`. Se ve bloqueado con `MejorOleada < 11`, en cada PLAY salvo
  la primera vez. La captura `Builds/libre_boton_5_bloqueado_en.png` es de HEAD. El banco solo compara el fondo
  (`PruebaModoLibre.cs:739` y `:849`). Precedente: la tienda resuelve "todavía no se puede" con `ApagadoOscuro`,
  `TextoSuaveClaro` y el halo apagado (`TarjetaMejora.cs:474-490`). Lo menor también es cierto: el idioma elegido es
  amarillo (`SelectorIdioma.cs:130`) con el halo celeste de `ConstructorNeon.cs:87-92`.
- **Números**: texto e icono sobre el gris, 3,70:1 (el proyecto exige 4,5 a los botones); con el arreglo, 5,73:1.
  Saturación del gris 0,135 (halo celeste); la de `ApagadoOscuro`, 0,39 (halo casi negro a 0,5).
- **Arreglo recomendado**: tres trampas al del hallazgo. (1) `colorBloqueado` está serializado: pasar
  `Menu.unity:4771` a (0.086, 0.102, 0.141, 1) y seguir pintando con el campo, o cambiar el banco, que si no falla.
  (2) `PintarHalo(boton, colorBloqueado)` con `ApagadoOscuro` da un halo casi negro: **apagar el halo** (alfa 0 en la
  `Sombra`), como la tarjeta sin monedas. (3) `BotonModoLibre` no tiene referencia al icono: `fondo.transform.parent
  .Find("Icono")` o un campo. Texto e icono en `TextoSuaveClaro`; al desbloquear, los colores leídos en `Awake`.
  Sumar al banco la `Sombra`, el texto y un contraste de 4,5 o más, y en `SelectorIdioma.Pintar` el halo de cada
  botón. Corregir "lo pinta gris" en CLAUDE.md.

#### H15 — En PC el jugador corre un 41 % más rápido en diagonal

- **Veredicto**: confirmado. **Gravedad final**: baja (era media): solo PC, y lo que se distribuye es Android.
  Confianza alta. Detalle: `refutar_H15_codigo.md`.
- **Evidencia**: `HandleMovement` (`PlayerController.cs:156-157`) arma `moveInput` con dos `GetAxis` (cada uno llega a
  1, `InputManager.asset:9-39`) y lo multiplica por `moveSpeed × multiplicadorVelocidad` sin topar; `FixedUpdate`
  (`:110-113`) pisa `linearVelocity`. `moveSpeed` vale 10 en el prefab y 15 en las tres escenas
  (`WaveMode.unity:554-555`); la furia, 1,3. Igual desde 2022. En el teléfono el Joystick Pack normaliza
  (`Joystick.cs:79-88`) y los `snap` están en falso. **La reproducción con F1 no sirve**: `MedidorBalance` no muestra
  velocidad. Contra una pared penetra 0,39 m por paso, no 0,55: el kill-Z no se reabre. Lo que sí pesa: probar en el
  editor con teclado da un juego más fácil que el del teléfono.
- **Números**: 15 m/s derecho y 21,21 en diagonal (+41,4 %); con la furia, 19,5 y 27,58. Contra el FASTER (12), la
  ventaja pasa de 3 a 9,2 m/s; contra el rápido, de 6 a 12,2; contra el normal, de 10 a 16,2.
- **Arreglo recomendado**: `moveInput = Vector3.ClampMagnitude(moveInput, 1f);` antes de calcular `moveVelocity`.
  Conserva la rampa de `GetAxis` (con `normalized` se perdería y `run` se prendería con cualquier tecla), no cambia el
  umbral de `run` y topa también los ejes del gamepad. No rompe ninguna prueba.

#### H21 — La caja de arma del tutorial nace detrás del panel de instrucciones

- **Veredicto**: confirmado. **Gravedad final**: baja (era media): el tutorial es opcional (la primera vez PLAY va
  directo a la oleada 1) y se destapa con menos de un segundo de movimiento. Confianza alta. Detalle:
  `refutar_H21_codigo.md`.
- **Evidencia**: `TutorialManager.cs:174` la hace nacer en jugador + (0, 0, 4), a y 0,5; `PUArma` es un cubo de 0,4.
  La cámara de `Tutorial.unity:853-854`, en (0, 12, −3,9), 70° y 60° vertical. `PanelInstruccion` (`:726-727`) va de
  145 a 315 u desde arriba, con relleno al 90 % y el halo de 40 u, y está prendido en el paso Arma; antes de `0a4cc87`
  era negro al 60 %. En `Builds/tutorial_caja_arma.png` solo asoma la cara de abajo del cubo, en las filas 315-317,
  bajo la línea celeste. `PruebaTutorial.cs:534` solo mira `activeSelf`. Pasa en todo el mapa con z ≤ 40.
- **Números**: la caja cae en 278,3-318,0 px de 1080 en cualquier proporción; el relleno del panel llega a 315 (16:9),
  352 (20:9) y 361 (21:9), más 40 u de halo. Tapada al 92 % en 16:9 y al 100 % de 18:9 a 21:9. Se destapa con 0,62 m
  al norte en 16:9 o 1,22 m en 20:9, unos 3 m al sur, o 10-11 m de costado.
- **Arreglo recomendado**: al sur, `new Vector3(0f, 0f, -2.5f)`: cae en 738-776 px, del lado contrario a la marcha, y
  no la tapan ni el panel movido de H13 ni el texto de la vida. **No `(0, 0, 2)`**: con el panel bajado a −265 (H13)
  el relleno llega a 391-401 px y el halo a 436-447, así que en 21:9 vuelve el problema y en 20:9 queda al límite; y
  queda en el camino de quien viene de las cajas del paso 4. El desplazamiento como constante pública y un chequeo en
  Lógica de mejoras que proyecte la caja con la cámara y el panel leídos de disco en 16:9, 20:9 y 21:9, con el
  jugador lejos de la pared norte (`PruebaTutorial` corre en la proporción del Game view). Decidirlo junto con el
  panel
  de H13.

---

## Parciales

### Media

#### H01 — Los halos de neón roban toques: justo debajo de CONTINUAR, en la pausa, se REINICIA la partida

- **Veredicto**: parcial (dos miradas: la del código lo confirmó en alta; la del impacto lo dejó parcial en media, con
  evidencia concreta, y esa gana). **Gravedad final**: media (era alta). Sería alta si se mide que el dedo se desvía
  2 mm o más, o que el 2 % o más de los toques a CONTINUAR caen por debajo de su borde. **Es un bloqueante del próximo
  AAB.** Detalle: `refutar_H01_codigo.md` y `refutar_H01_impacto.md`.
- **Evidencia** (confirmada dos veces resolviendo los `RectTransform` del YAML en HEAD, `e6556d9`, `0a4cc87^` y
  `0a4cc87`): `ConstructorUI.HaloDeBoton` (`ConstructorUI.cs:60-73`) estira la `Sombra` ±34 u con `NeonPildora` y no
  toca `raycastTarget`. En disco hay 15 con `m_RaycastTarget: 1`: `MenuPausa.prefab:946`, `:1866` y `:1941`; diez en
  `Menu.unity` (338, 1018, 1573, 1648, 2145, 2700, 2940, 5928, 6130 y 7310); `Tutorial.unity:1548` y `:1774`. Las de
  Perdiste y la tienda están en 0, y `ConstructorUI.Boton` pone `false`. Ninguna escena lo pisa y ningún script lo
  cambia. El `GraphicRaycaster` prueba contra todo el rect (`alphaHitTestMinimumThreshold` en 0) y ordena por
  profundidad: la `Sombra` de REINICIAR va después y gana. El toque sube al `Button` de `BotonReiniciar`, cuyo
  `onClick` es `MenuPausa.Reiniciar` (`:140`): olvida la oleada y recarga, y la escena recargada guarda la oleada 1.
- **Lo que pesó en contra (la mirada del impacto)**: (1) **hoy no lo tiene nadie en Play**: la prueba cerrada es la
  1.2.0 (5), de `16d8a38` (18/9), y `0a4cc87` (25/9) no es su ancestro; HEAD sigue en 1.2.0 y 5, así que no hubo otro
  AAB. Solo está en las APK de Ivan del 27/9. (2) **Solo la pausa cuesta algo**: en el menú todo se deshace con un
  toque (SALIR abre ¿SALIR?, el panel de modos carga otro modo y OLEADAS retoma, en el idioma se vuelve a elegir); en
  PC es casi nulo; en el libre se pierde una partida que no era retomable, y en el tutorial, nada. En el teléfono se
  reanuda con CONTINUAR o con el atrás (el botón de pausa queda tapado por el `Panel`). (3) **La zona muerta
  publicada era de 22 u** (1,5 mm), porque REINICIAR ya empezaba en −30 por su `Fondo`: el arreglo devuelve ese
  umbral. El costo por vez es el de una muerte: se pierde la partida retomable y se conservan monedas, mejoras y
  mejor oleada; parece un bug (se recarga en la oleada 1).
- **Lo que sumaron los escépticos**: en el panel de modos, TUTORIAL se lleva el hueco y 4 u de arriba de MODO LIBRE, y
  OLEADAS el otro hueco y 4 u de abajo; en la ventana del idioma, VOLVER se lleva 9 u de ESPAÑOL (solo en x ±184) y
  ESPAÑOL, 14 u de abajo de INGLÉS; en `PanelFinal` del tutorial solo se reparte el hueco. Amplía «PLAY y SALIR quedan
  a
  20 unidades en 20:9 y 21:9»: el halo de SALIR ya se lleva toques que caen adentro del rect de JUGAR (x 370-384, y
  −244..−172 en 20:9, sobre la punta redonda visible de la píldora). **Perdiste ya vive con el arreglo A** (sus halos
  en 0) y nadie lo anotó.
- **Números**: en la pausa (1920x1080, match 0,5), CONTINUAR va de 0 a 120, REINICIAR de −150 a −30 y MENÚ de −300 a
  −180; el halo de REINICIAR llega a +4 y se queda con 34 u: 30 de hueco (2,1 mm) y 4 adentro de CONTINUAR (0,28 mm,
  no 2,4: los 2,4 mm son toda la franja). En un 6,5" de 2400x1080, 1 u ≈ 0,070 mm; el umbral de REINICIAR bajo el
  centro de CONTINUAR es hoy de 3,93 mm (6,31 con el arreglo o en la versión publicada). Rehacer la partida: 0,7 min
  en la oleada 5, 2 en la 10, 13,3 en la 30 y 22,4 en la 40 (14-23 min, no 15-20), y rehacer el camino paga del 40 al
  70 % de la frontera. Probabilidad de que un toque a CONTINUAR caiga en REINICIAR (supuestos: error normal con desvío
  σ y sesgo b hacia abajo), hoy → con el arreglo:

  | σ | b 0 | b 0,5 mm | b 1 mm | b 1,5 mm |
  |---|---|---|---|---|
  | 1,0 mm | 0,004 % → ~0 | 0,03 % → ~0 | 0,17 % → ~0 | 0,76 % → 0,0001 % |
  | 1,5 mm | 0,44 % → 0,001 % | 1,1 % → 0,005 % | 2,5 % → 0,02 % | 5,3 % → 0,07 % |
  | 2,0 mm | 2,5 % → 0,08 % | 4,3 % → 0,18 % | 7,2 % → 0,40 % | 11 % → 0,81 % |
  | 2,5 mm | 5,8 % → 0,58 % | 8,5 % → 1,0 % | 12 % → 1,7 % | 17 % → 2,7 % |

  El arreglo lo baja entre 50 y 200 veces en el rango del medio. Con 1,1 % y una pausa por partida, en 50 partidas le
  pasa al menos una vez al 43 % de los jugadores.
- **Arreglo recomendado**: **B**, `sombra.raycastPadding = new Vector4(34f, 34f, 34f, 34f)` en
  `ConstructorUI.HaloDeBoton`: el padding positivo achica el área tocable (lo usa `GraphicRaycaster.cs:326`) y queda
  un
  área quieta igual al rect del botón, que no pisa al vecino y no pierde los bordes al apretarse. Se aplica en las 15
  líneas `m_RaycastPadding` de las sombras (3 en la pausa, 10 en el menú y 2 en el tutorial, ninguna de una instancia)
  y en el constructor, para que Vestir el menú y Vestir la partida no las devuelvan a 0. **Confirmar el signo en
  Unity**
  (el rectángulo verde al seleccionar la imagen). La **A** del hallazgo (`raycastTarget = false`) es correcta pero
  deja
  como único blanco el `Fondo`, dentro de `Visual`, que `BotonJugoso` achica a 0,9 al apretar (18/s, también en la
  pausa): un toque en los 5 u de arriba o abajo, o en los 23 u de las puntas, puede apretar sin hacer clic. Prueba que
  lea de disco: toda `NeonPildora` con `m_RaycastTarget: 0` o con padding de 34 o más por lado ("ninguna es
  raycastTarget" rechazaría B). Revisar el diff: no desaparece ningún `--- !u!`. Para un área más generosa, un padding
  menor: 19 en la pausa (hueco de 30) y 24 o más en el menú (hueco de 20). Va con H05 y con «El récord de una partida
  abandonada».

#### H10 — Con el techo de zombis lleno, el jefe hace el aviso entero de la invocación y no sale nadie (o sale uno)

- **Veredicto**: parcial: el mecanismo es exacto, pero la frecuencia es otra y el arreglo propuesto no alcanza.
  **Gravedad final**: media (igual). Confianza media. Detalle: `refutar_H10_codigo.md`.
- **Evidencia**: `JefePatrones.Empezar` (`:407-428`) elige solo por `tocaCarga` y paga el aviso entero (anillo,
  zarpazos cortados, rugido con pitch 0,55, temblor). A los 0,8 s (`:395-402`) llama a `Invocar` y siempre a
  `Terminar`, que da vuelta `tocaCarga` (`:510`). `Invocar` (`:545-551`) recorta n = min(4 (+2 en furia), 8 −
  `InvocadosVivos`, `LugarParaZombis`); con 0 vuelve antes de `Efectos.Invocado`: el aviso termina en silencio y el
  HUD
  no suma. `LugarParaZombis` = techo − `ZombisVivos` (`EnemyController.cs:226-229`), y cuenta al jefe y a sus
  invocados. Techo 60 (35 en móvil) en `WaveMode.unity:2097-2098` y `ShowBies1.unity:1332-1333`. Con n = 1 sale
  siempre al +X. El camino de `maxInvocadosVivos` da 0 aunque haya lugar: la tercera invocación sin furia, y en furia
  6, 2 y 0; pasa también en PC en la oleada 10.
- **Lo que corrigió el escéptico**: (1) "WaveManager llena cada lugar libre en un cuadro" es falso: saca uno y espera
  0,35 s (`WaveManager.cs:154`); en el libre, cada 0,25 s. La razón es que el relleno llega antes de que termine el
  aviso de 0,8 s. (2) "En el libre casi siempre" es exagerado: sale vacía el 60-90 % de las veces cuando el nivel le
  gana al jugador (la segunda mitad de cada partida del libre); con un jugador que le gana al nivel, sale entera la
  mitad o más. En PC el techo de 60 solo se llena si el jugador se atrasa. (3) La oleada 10 del teléfono es angosta:
  el primer ataque es la carga y la primera invocación cae a ~21-24 s, cuando ya salieron los 50. Donde pega de verdad
  es en el teléfono desde la 20 (90 contra 35). (4) Mirar el lugar en `Empezar` no alcanza: el generador lo ocupa en
  0,25-0,35 s; con el techo saturado, el 54-93 % de las invocaciones que igual empiezan sale vacía.
- **Números** (modelo sin medir; se queda corto): libre, teléfono: nivel 1 con 30 de daño por segundo, 14 % vacías;
  nivel 5 con 30, 62 % vacías; nivel 10 con 60, 58 % vacías. Libre, PC: hasta el nivel 5, 100 % enteras. Oleadas, en
  el teléfono: la 10 sale 3-4-4 o 4-4-4; la 20 con 15-30 de daño por segundo, 0-0-0. Ciclo del jefe ≈14 s (10,5 en
  furia).
- **Arreglo recomendado**: **reservar el lugar en `Empezar`**: un `static int EnemyController.Reservados` que
  `LugarParaZombis` reste y que miren los dos generadores (`WaveManager.cs:148` y `GeneradorZombis.cs:169` pasan a
  `ZombisVivos + Reservados >= techo`). Se reserva min(cantidad + extra, `maxInvocadosVivos` − `InvocadosVivos()`,
  `LugarParaZombis`) y se suelta en `Invocar`, `VolverAPerseguir` y `OnDisable`; va en el reset de
  `SubsystemRegistration`. Si da 0, carga en vez de invocar, sin dar vuelta `tocaCarga`. La otra opción, que la
  invocación pase el techo hasta `maxInvocadosVivos` (35 → 43 en el teléfono, +23 %), se decide con la medición de FPS
  pendiente. Aparte, un desfase al azar en los ángulos para uno a tres invocados.

### Baja

#### H02 — La furia arranca lista en cada partida: la misión de furia y el logro FURIOSO se cumplen reiniciando

- **Veredicto**: parcial: el mecanismo funciona de punta a punta, pero adelanta y no multiplica. **Gravedad final**:
  baja (era media). Confianza alta. Detalle: `refutar_H02_codigo.md`.
- **Evidencia**: `activadaEn` arranca en `float.NegativeInfinity` (`Furia.cs:43`), campo del componente de
  `Jugador.prefab:208-225` (enfriamiento 120 en `:221`); `Restante` da 0 y `Lista` es verdadera desde el primer
  cuadro, a propósito (`BotonFuria.cs:34-36`). `Activar` (`:99-109`) llama a `Progreso.ContarFuria`
  (`Progreso.cs:419-423`), contador de por vida que leen la misión (`MisionesDiarias.cs:205` y `:217`) y FURIOSO
  (`Logros.cs:67`). Ni `Activar` ni `ContarFuria` miran el modo, el tiempo, los zombis o la oleada. Los caminos:
  REINICIAR, la R (solo se corta con vídeo o ¡HAS MUERTO!) y pausa → MENÚ → OLEADAS; en el tutorial, la F y la R en
  PC.
- **Lo que no se sostiene**: el cofre no se le atribuye (pide las tres misiones); FURIOSO se adelanta, no se
  multiplica (llega igual jugando, en ~8,3 partidas), y farmearlo temprano puede pagar menos; la granada ya se cotiza
  a 5 s tirándola al aire: la vuelta rinde ×2,5 solo si dura 2 s; en WaveMode REINICIAR y la R borran la oleada: el
  camino barato es el libre o entre carreras; no toca el semanal (no tiene furia). **Amplía «Las dos pruebas son
  circulares»**: `PruebasMejoras.cs:3434` tiene la misma forma; si modelara la furia gratis de cada partida fallaría
  en la oleada 10 (0,6 contra el umbral de 0,75 de `:3445`).
- **Números**: en la oleada 40 los objetivos de furia son 5 / 12 / 29 y pagan 5.500 / 17.150 / 51.450 (el cofre,
  37.050, pide las tres). Las 29 vueltas tardan 1 min a 2 s por vuelta, 2,4 min a 5 s y 7,3 min a 15 s (sin medir),
  contra ~55 min jugando. La misión de furia sale el 67 % de los días con solo la furia comprada (~14.200 monedas
  esperadas por día en la 40) y el 47 % con las tres compras (~10.600).
- **Arreglo recomendado**, del lado del contador: un `static float` con los segundos jugados en tiempo escalado desde
  la última furia contada (sube en `Furia.Update`, así la pausa lo congela), en 0 en `SubsystemRegistration` (así
  reiniciar la app no regala una); `Progreso.ContarFuria` cuenta solo si pasaron `enfriamiento` segundos y los
  descuenta. La furia sigue arrancando lista, la vuelta deja de rendir y el objetivo mide exactamente N×120 s, sin
  tocar `Objetivo` ni la prueba. Costo: la furia gratis del principio no cuenta para la misión ni para FURIOSO, y el
  bronce pide 120 s de partida. **Se descarta** el static en tiempo real del hallazgo: contradice que la pausa congele
  el enfriamiento y le cambia el juego al honesto (si muere y toca OTRA VEZ antes de 120 s, empieza con la furia en
  recarga); eso lo tendría que decidir Ivan. "Contar solo si completó una oleada" no sirve en el libre. Si se conserva
  la furia gratis que cuenta, arreglar `PruebasMejoras.cs:3434` para que la modele.

#### H04 — Retomar hace opcional la muerte en oleadas: cada oleada se reintenta con vida llena y las mejoras recién compradas

- **Veredicto**: parcial: no hay ninguna guarda, pero las monedas y los logros están mal pesados. **Gravedad final**:
  baja (era media). Confianza alta. **Pide una decisión de Ivan.** Detalle: `refutar_H04_codigo.md`.
- **Evidencia**: `WaveManager.cs:124-125` guarda la oleada y los puntos al empezar cada oleada. `MenuPausa.IrAlMenu`
  (`:96-100`) no olvida nada, y al perder el foco `Pausar` guarda (`:54-79`); solo olvidan morir
  (`PlayerHealth.cs:211`) y REINICIAR o la R. Retomar (`WaveManager.cs:104-113`) crea un `PlayerHealth` nuevo: vida
  llena, `GolpesRecibidos` en 0, `yaRevivio` falso y 500 balas (`WaveMode.unity:546-547`); `AplicarMejoras.Awake` y
  el botín aplican lo que se compró en el medio. No hay contador de retomas.
- **Lo que corrigió y sumó el escéptico**: (1) **En monedas no es granja**: ×1,2-1,6 por minuto con la duración de
  `Economia` y ~10 s por ciclo (el hallazgo decía ×1,0-1,2, sin el jefe); nunca llega al doble. (2) INTOCABLE y el
  bronce y la plata de SUPERVIVIENTE se **adelantan** (valen ~1 partida de monedas en total); lo que cambia de
  naturaleza es el oro de SUPERVIVIENTE (la 50, detrás del muro), `MejorOleada` y `HighScore_3`: alcanza con pasar
  cada oleada una vez, con vida llena. Hoy son solo locales. (3) La cadena `puntosEnCurso` → récord no suma nada: todo
  está en el mismo JSON editable. (4) Con el proveedor Nulo el golpe que mata olvida la oleada en el mismo cuadro: hay
  que pausar antes (la carga del jefe pega 90, 178 y 350 en las oleadas 20, 30 y 40). **Nuevo**: retomar también
  devuelve el revivir por vídeo (acotado por el tope de 3 por día), y quedan falsos `WaveManager.cs:76-77` ("no se
  puede comprar en medio de la partida"), `AplicarMejoras.cs:8-10` y la frase de CLAUDE.md sobre cuándo se aplican
  las mejoras. Amplía «Punto de control por capítulo en las oleadas», cuyo arreglo (guardar el capítulo al morir) deja
  MENÚ → OLEADAS igual.
- **Números**: farmeo, monedas por segundo contra la partida normal: muriendo en la 40, ×1,34-1,67 (tF 0,5),
  ×1,07-1,37 (0,8) y ×0,97-1,26 (1,2); en la 45, ×1,47-1,79; en la 30, ×0,89-1,41. Rejugar la 1-19 son 390 s; un
  reintento cuesta ~10 s más lo que tarde el primer golpe. INTOCABLE entera, ~4.700 monedas con mejor oleada 20.
- **Arreglo recomendado**, **para decidir** (todas las opciones cambian el pedido de `d6005e6`, el que cierra en la 24
  para dormir): la (b) del hallazgo castiga ese caso. La propuesta del escéptico, sin subir la versión: (1) para
  INTOCABLE, un bool "la oleada en curso ya recibió un golpe" guardado junto a `oleadaEnCurso` (se prende en
  `TakeDamage` y lo escribe el `Guardar` de `Pausar`); (2) para la cura gratis, guardar la vida y las balas al empezar
  cada oleada y retomar con eso, topado al máximo nuevo; (3) para las tablas y el oro, una sola retoma por oleada (un
  contador que `GuardarOleadaEnCurso` pone en 0): la segunda vuelve al principio del capítulo. La (c), congelar las
  mejoras, es la más cara y deja la tienda mostrando compras que no se aplican. Si se deja la compra a mitad de
  partida, corregir los dos comentarios y CLAUDE.md.

#### H07 — El versionCode 5 ya se usó y el AAB no lo revisa: la próxima subida la rechaza Play Console

- **Veredicto**: parcial: el mecanismo es tal cual, pero ya estaba en TAREAS y el rechazo llega al subir. **Gravedad
  final**: baja (era media). Confianza alta. Detalle: `refutar_H07_codigo.md`.
- **Evidencia**: `ProjectSettings.asset:178` (`AndroidBundleVersionCode: 5`) y `:147` (1.2.0), iguales en `main`,
  `idiomas` y la rama de la nube. `16d8a38` (18/9) subió de 4 a 5, y el manifiesto del `Builds/ShowBies.aab` de ese
  día dice versionCode 5; según `pasos.md:33` se mandó a revisión. Nada lo pisa (`mainTemplate.gradle:34` usa
  `**VERSIONCODE**`) y no hay `IPreprocessBuild`: `bundleVersionCode` solo aparece en `ConstructorAndroid.cs:332`,
  después de `BuildPlayer`, para anotarlo. Las pruebas del build (`PruebasMejoras.cs:1410-1442`) no lo miran.
  `TAREAS.md:130` ya dice «Armar el AAB de la 6 (subir AndroidBundleVersionCode a 6 y bundleVersion)». **Nuevo**:
  `publicacion/pasos.md:55`, en «Lo que ya está resuelto», todavía dice «versionCode 4 y versión 1.1.0», contra su
  línea 33 y el repo. El `build_result` del 27/9 con el 5 es de la APK de prueba: solo prueba que el repo no se movió.
- **Números**: historia del versionCode: 1 (2022) → 3 (2023) → 4 (16/9) → 5 (18/9). El AAB del 18/9 pesa 35.093.522
  bytes. Guardas de la build: seis, ninguna del versionCode.
- **Arreglo recomendado**: subir a 6 justo antes de armar (el 1.3.0 es cosmético). El "último subido" a mano tiene un
  agujero (si nadie lo actualiza, pasa la guarda y Play lo rechaza igual). Mejor: (1) `publicacion/ultimo_aab.txt`
  versionado, que arranca en 5; (2) una función pura `ConstructorAndroid.ProblemaDeVersionCode(actual, ultimo)`;
  (3) en `ArmarAab`, al lado de `ProblemaDelAab` y antes de tocar el keystore, `Fallar` con ese motivo; (4) en
  `Construir`, con un AAB exitoso, escribir el versionCode en el archivo (cada AAB armado cuenta como usado; los
  saltos se permiten); (5) casos en `PruebasMejoras.cs:1410-1442` (5 contra 5 y 4 contra 5 no salen, 6 contra 5 sí);
  (6) corregir `pasos.md:55` y CLAUDE.md.

#### H08 — La próxima subida es la primera con la librería de reseñas de Play: revisar Seguridad de los datos y la política de privacidad

- **Veredicto**: parcial: los hechos son ciertos, la lectura queda abierta. **Gravedad final**: baja (era media).
  Confianza media. **Pide una decisión de Ivan** (el formulario). Detalle: `refutar_H08_codigo.md`.
- **Evidencia**: `16d8a38` (1.2.0/5, 18/9) es ancestro de `a0b1b76` (la reseña, 19/9), y `mainTemplate.gradle` no
  existe en `16d8a38`. El dex del AAB subido tiene 0 `ReviewManagerFactory` y ningún `play/core/review`; la APK del
  27/9 trae `review_client=2.0.1`, 42 referencias a `play/core/review` y, de rebote, `play-services-basement` y
  `tasks`. La plantilla está prendida (`ProjectSettings.asset:264`) y `PedidoDeResena` está prendido en el menú
  (`Menu.unity:5563-5573`). La guía de Google (Data safety de In-App Review) dice lo que citaba el hallazgo;
  `pasos.md:26` y `:85`, `privacidad.html:32-33` y `ficha.md:53` y `:100` dicen "no recopila".
- **Lo que pesó en contra**: la lectura queda abierta (la ayuda del formulario cuenta lo que transmiten las
  librerías, pero la guía de Play Core dice que el dato lo maneja la Play Store en su propio proceso y el juego nunca
  lo ve, y Google deja la decisión al desarrollador); nada de lo que Play cruza solo cambia (0 `uses-permission` en
  los
  dos manifiestos: ni INTERNET, ni AD_ID); "el único que podría terminar en rechazo" no tiene regla ni caso conocido;
  y los probadores ya pueden mandar su opinión privada desde la ficha. No está en pendientes (solo el vecino de
  "privado"); H114 toca el mismo formulario.
- **Arreglo recomendado**, sin código y antes del próximo AAB: (1) decidir el formulario con las dos páginas de Google
  a la vista; lo recomendado es "no recopila", la lectura habitual y sin permisos nuevos, con el porqué anotado en
  `pasos.md`; si se declara "Otro contenido generado por el usuario" (Funcionalidad, no compartido), sacar "doesn't
  collect your data" / "no recopila tus datos" de `ficha.md:53` y `:100`. (2) Una línea en la política con el molde
  del párrafo de Analytics ("el juego puede mostrar la ventana de valoración de Google Play; lo que escribas ahí lo
  maneja Google según sus políticas"), en los tres lugares (`publicacion/privacidad.html`, el repo
  `showbies-privacidad` y `gh-pages`), con fecha nueva y junto con el arreglo de "privado" / "never leaves your
  phone". (3) Actualizar `pasos.md:26` y `:85` junto con H114. El SDK Index de la consola va a listar
  `play-services-basement` y `tasks`.

#### H09 — Dónde vive progreso.json lo decide un desplegable: pasarlo a Force Internal deja a todos sin progreso

- **Veredicto**: parcial: el mecanismo se confirma con fuente primaria, pero no destruye y hoy solo hay probadores.
  **Gravedad final**: baja (era media). Confianza alta. Detalle: `refutar_H09_codigo.md`.
- **Evidencia**: `ProjectSettings.asset:182` dice `AndroidPreferredDataLocation: 1` desde `266f6f6` (la migración a
  Unity 6); el enum de `UnityEditor.CoreModule.dll` 6000.3.14f1 es PreferExternal = 1 y ForceInternal = 2, y el
  `boot.config` de la APK del 27/9 dice 1. La caída al interno está documentada ("Prefer external, if possible. Use
  internal otherwise"; en el Inspector el ajuste se llama **Storage Location**). `Progreso.Ruta()` (`:1099-1101`) no
  tiene alternativa, y en `Cargar` (`:826-885`), con la carpeta vacía, `Leer` da `NoExiste` en los tres candidatos
  (`:915`): `soloLectura` falso, un `Datos` nuevo y el primer `Guardar` escribe en la carpeta nueva. Nada lo vigila
  (`ProbarCalidadDeAndroid`, `PruebasMejoras.cs:1401-1443`, ni `ConstructorAndroid`).
- **Lo que pesó en contra**: el pendiente «`progreso.json` está en el almacenamiento externo» ya propone la mudanza
  por
  JNI, no el desplegable a secas; no destruye (`getExternalFilesDir` sobrevive a las actualizaciones y volver a
  PreferExternal recupera todo, menos lo jugado entretanto); y "todos" hoy son los probadores (versionCode 4 y 5) y la
  APK de Ivan. La caída con el externo sin montar es cierta pero improbable con minSdk 25 (sin medir). **Amplía** ese
  pendiente: una sesión caída al interno deja un `progreso.json` casi vacío; si la mudanza toma "el interno existe"
  como "ya se mudó", dejaría huérfano el verdadero.
- **Arreglo recomendado**: (1) una guarda ya, con una constante que declare el código (`Progreso.UbicacionEsperada =
  PreferExternal`), en `ProbarCalidadDeAndroid` (`inf.Igual(..., esperada, PlayerSettings.Android.
  preferredDataLocation)`) y en `ConstructorAndroid` (un `ProblemaDeUbicacion()` que niegue la APK y el AAB); cuando
  entre la mudanza, se cambia la constante y la misma guarda protege la vuelta. (2) La mudanza con una marca propia en
  `PlayerPrefs` (`ProgresoMudado`), no con la existencia del archivo; con archivos en las dos carpetas, elegir por
  contenido (`segundosJugados` o `partidasTerminadas` mayor, o la fecha), y para todas las copias (principal, `.tmp`,
  `.anterior`, `.roto` y `.bak`). (3) Si se va a mudar, antes de producción.

---

## Refutado

#### H19 — Si Unity 6 acota el pitch a 3, las escaleras y los arpegios repiten la nota de arriba

- **Veredicto**: refutado. Confianza alta. Detalle: `refutar_H19_codigo.md`.
- **Lo que lo tumba**: se leyó el motor que se publica (6000.3.14f1, Android IL2CPP Release, arm64-v8a y x86_64, con
  los símbolos de `libunity.sym.so` y el `llvm-objdump` del NDK). Ningún paso acota el pitch a 3: `set_pitch` llega a
  `SetPitch_Injected` sin tocar el valor; `AudioSource_CUSTOM_SetPitch` solo descarta infinito y NaN;
  `AudioSource::SetPitch` descarta un negativo con clip comprimido y guarda el valor crudo en `m_Pitch`, que pasa
  igual de crudo a cada canal; `GetPitch` lo devuelve tal cual (`s.pitch = 4f; Debug.Log(s.pitch)` imprime 4);
  `PlayOneShot` y `Play(double)` vuelven a llamar a `SetPitch(m_Pitch)` sin acotar; y FMOD solo corta la frecuencia
  en 1.000.000 Hz. El único [−3, 3] del módulo es `AudioSource::CheckConsistency`, la validación de lo serializado y
  del inspector, que nadie llama directo: la frase de la documentación de 6000.3 describe eso, y su propio ejemplo usa
  `startingPitch = 4`.
- **Números**: 19, 21, 23 y 24 semitonos dan pitch 2,997, 3,364, 3,775 y 4,000; `moneda.wav` y `combo.wav` (44.100
  Hz) quedan en 132.180 a 176.400 Hz, contra el techo de FMOD de 1.000.000 Hz (pitch ≈22,7, unos +54 semitonos). Todo
  suena distinto donde el hallazgo decía que se repetía (la escalera, el combo, el arpegio del hito, los ticks de la
  derrota, la barra del nivel y el cofre).
- **Queda**: ningún arreglo. Como mucho, un comentario en `Sonidos.PitchDe` que diga que el setter acepta más de 3 y
  que FMOD corta en 1e6 Hz, para que nadie "corrija" las tablas.

---

## Sin refutar: los 100 hallazgos bajos

No se refutaron (la ronda solo pasó por los medios y el alto) y no quedó ningún medio sin refutar. Van con el
veredicto "sin refutar" y gravedad baja; la evidencia y el arreglo son los del juez. La reproducción de cada uno está
en
`unicos.md`. Los que dicen "Amplía" nombran el pendiente al que se suman.

### Partida, HUD y tutorial (H24 a H30)

#### H24 — Dos etiquetas del HUD no se leen en uno de sus estados: FURIA mientras dura (1,3:1) y la G de la granada recargando (2,2:1) (baja, bug; sin refutar)

- **Archivos**: Scripts/UI/BotonFuria.cs:102-106, :130; Prefabs/UI/BotonFuria.prefab:580, :587;
  Editor/ConstructorNeon.cs:214, :293-296; Editor/PruebasMejoras.cs:1919; Escenas/WaveMode.unity
  (BotonGranada/Etiqueta)
- **Evidencia**: Mientras dura, el fondo de la furia es ConstructorUI.Amarillo y la etiqueta (FURIA, o FURIA (F) en
  PC) sigue blanca, con el material liso de Bangers y sin contorno: 1,30:1 (3,48:1 sobre el rojo de lista), cuando el
  resto del neón lleva texto oscuro (13,9:1). 0a4cc87 pasó la G de la granada de blanca a NaranjaTexto, y la sombra de
  Recarga (negro al 55 %) va debajo: mientras recarga es oscuro sobre naranja oscurecido, 2,2:1 (antes ~7:1).
  ProbarPartidaNeon solo mira colorListo.
- **Arreglo**: Teñir la etiqueta de la furia según el estado: AmarilloTexto mientras dura, un rojo oscuro sobre el
  rojo de lista y blanco sobre el casi negro (o darle el material de contorno del HUD); la G blanca mientras recarga
  (o siempre). Sumar los dos estados, con el color compuesto, a ProbarPartidaNeon.

#### H25 — La cola de avisos de misión se sigue vaciando con el juego congelado: en la pausa se pierden y suenan todos juntos al volver; detrás de la derrota suenan y sacuden la cámara sin cartel (baja, bug; sin refutar)

- **Archivos**: Scripts/UI/AvisoDeMisiones.cs:75-85, :173, :182-194, :215-216; UI/DerrotaEnLaPartida.cs:126-134;
  Jugo/Sonidos.cs:29-31; Camara/CamaraJugador.cs:141
- **Evidencia**: Update corta solo la revisión con MenuPausa.JuegoCongelado (:173); la cola se sigue vaciando (:183) y
  cada cartel (2,6 s) se vence con unscaledTime. Bajo la pausa se gastan detrás del panel negro al 80 %; cada Mostrar
  toca el jingle por una fuente que AudioListener.pause retiene y suma 0,15 de trauma que la cámara no descarga en
  pausa: al tocar CONTINUAR arrancan N jingles en fase (tres ya pasan el techo del limitador) y la cámara tiembla
  sola. «Modo libre desbloqueado» se marca visto al encolarlo y no vuelve. Sobre la derrota, EsconderElHud apagó ese
  canvas: suena «¡misión cumplida!» y el mundo tiembla detrás de GAME OVER sin cartel (la muerte de un jefe encola
  varios a la vez). Con ¡HAS MUERTO! el cartel sale detrás de la ventanita.
- **Arreglo**: Con MenuPausa.JuegoCongelado no sacar nada de la cola y congelar la edad del cartel (unscaledDeltaTime
  topeado, sumado solo con el juego andando); con DerrotaEnLaPartida.Activa, vaciar la cola en silencio, o mostrar
  esos avisos en la derrota.
- **Nota**: Distinto de «Sonido, lo que quedó afuera» (el jingle del aviso encima del cartel de la oleada).

#### H26 — Los carteles del capítulo, de la guía de monedas y de la furia se vencen detrás de la pausa (baja, bug; sin refutar)

- **Archivos**: Scripts/Escenario/CapitulosDeEscenario.cs:179, :507-519; Tutorial/GuiaPrimeraPartida.cs:126, :171;
  UI/BotonFuria.cs:76-83
- **Evidencia**: Los tres miden su edad con Time.unscaledTime. El del capítulo (3,2 s, WaveMode.unity:2885) sale en el
  descanso de las oleadas 11, 21 y 31, justo cuando se pausa: al volver ya no está, mientras el fundido
  (Time.deltaTime) sigue a mitad. «¡COGE LAS MONEDAS!» (5 s), la guía de quien recién instala, salta de edad al
  reanudar y se va en el primer cuadro. En una partida retomada en la 11 o más, el cuadro que instancia el decorado
  entero se come la entrada del cartel del capítulo (sin medir). CLAUDE.md afirma que la pausa lo congela todo sin
  tocar nada más.
- **Arreglo**: Llevar la edad de esos carteles con un reloj propio (unscaledDeltaTime topeado) que no avance con
  MenuPausa.Pausado: la pausa de impacto no los congela y la del menú sí.

#### H27 — En PC, el panel final del tutorial se toca con la mira y el clic dispara (baja, bug; sin refutar)

- **Archivos**: Scripts/UI/CursorMira.cs:22-23; Tutorial/TutorialManager.cs:177-183; Jugador/PlayerController.cs:201
- **Evidencia**: CursorMira vuelve a la flecha solo con MenuPausa.JuegoCongelado. El paso Fin prende panelFinal (JUGAR
  y MENÚ) sin congelar nada: los botones se apuntan con la mira, y como el arma lee el mouse sin mirar la UI, el clic
  tira una ráfaga antes de cargar la escena.
- **Arreglo**: En Paso.Fin, apagar el arma (theGun.enabled = false) o el control del jugador, y volver a la flecha
  (por ejemplo, con un estado estático de CursorMira que el tutorial prenda).

#### H28 — Los zombis del tutorial pueden nacer a la vista (baja, bug; sin refutar)

- **Archivos**: Scripts/Tutorial/TutorialManager.cs:142, :152; Escenas/Tutorial.unity:401 (distanciaSpawnZombi 14)
- **Evidencia**: El zombi del paso 2 y el grupo del paso 3 nacen a 14 m en una dirección al azar, sin mirar si se ven.
  Con la cámara del juego ese punto cae dentro de la pantalla en 62 de 360 grados en 16:9 (17 %) y en 140 de 360 en
  20:9 (39 %), hacia los costados; la niebla empieza a 16 m y no los disimula. En los generadores esto se arregló el
  25/9 con SeVeriaAlAparecer.
- **Arreglo**: Sortear la dirección hasta que EnemyController.SeVeriaAlAparecer dé falso (mirándolo después de acotar
  con PuntoDelMapa), o hacerlos nacer a 18-20 m.

#### H29 — El tutorial dice que el cargador grande queda «para siempre», y dura la partida (baja, bug; sin refutar)

- **Archivos**: Assets/Idioma/Resources/Textos.txt:226 (tut_reloj); Scripts/Jugador/PlayerController.cs:29-30,
  :139-151
- **Evidencia**: tut_reloj dice «The bigger magazine is yours to keep» / «se queda para siempre». PUArma sube maxBalas
  a 1000 solo en el jugador de esa escena, y la partida siguiente (también la que carga el tutorial al terminar)
  vuelve a 500. Quien recién aprende puede entender que es una mejora permanente.
- **Arreglo**: «…lasts for the rest of the run» / «…se queda hasta el final de la partida».

#### H30 — La derrota con la oferta del x2: el próximo objetivo no se entera del cobro y queda dentro del halo del botón del vídeo (baja, bug; sin refutar)

- **Archivos**: Scripts/UI/ProximoObjetivo.cs:19, :31-40, :77-105, :117, :126; Anuncios/OfertaDeDuplicar.cs:97-109;
  Progreso/Progreso.cs:555-562; Tienda/BotonMejoras.cs:67-68; Escenas/Perdiste.unity:156-172 (OfertaVideo y −100,
  820x64, Sombra +34)
- **Evidencia**: Dos cosas en el mismo renglón, las dos solo con vídeo (hoy la APK de prueba; Play va en Nulo). (1)
  Elegir corre una sola vez, en Start. El x2 cobra por CobrarPremio y sube Revision: BotonMejoras vuelve a decir «¡Te
  alcanza para N mejoras!», pero el objetivo sigue con «TE FALTAN N MONEDAS PARA X» con X ya comprable, en la misma
  pantalla. (2) La píldora del x2 va de −132 a −68 y su halo de neón (naranja, alfa 0,5) baja hasta −166; las letras
  del objetivo van de −166 a −142 (el texto, de −175 a −135): el renglón queda dentro del resplandor.
- **Arreglo**: (1) Guardar Progreso.Revision al elegir y, si cambia, volver a Elegir y reescribir el texto y la barra.
  (2) Mirarlo en una captura de la derrota con Falso; si molesta, bajar ProximoObjetivo ~20 u o achicar el halo del
  x2.
- **Nota**: Un hallazgo con dos partes (lógica y posición), unidas porque hud#7 las vio juntas y pasan en la misma
  pantalla y el mismo caso.

### Tienda y menú (H31 a H40)

#### H31 — En PC, Espacio o Enter vuelven a comprar la última tarjeta tocada, y A/D mueven la selección (baja, bug; sin refutar)

- **Archivos**: Prefabs/UI/TarjetaMejora.prefab:2451-2452 (m_Navigation m_Mode 3);
  ProjectSettings/InputManager.asset:265-271 (Submit); Escenas/Menu.unity:1969;
  Scripts/Tienda/TarjetaMejora.cs:169-176; comparar con BotonFuria.prefab:282-283 (m_Mode 0)
- **Evidencia**: Selectable.OnPointerDown selecciona el botón si la navegación no es None, y el StandaloneInputModule
  manda Submit (Enter o Espacio) al seleccionado. Después de comprar con clic, cada Espacio o Enter llama otra vez a
  IntentarComprar, y el eje Horizontal (A/D, flechas) pasa la selección a la tarjeta de al lado. Espacio es la granada
  en la partida. El botón de la furia ya tiene la navegación en None por lo mismo.
- **Arreglo**: navigation.mode = None en el botón de la tarjeta, puesto por ConstructorTienda (y, si se quiere, en ¡A
  JUGAR! y VOLVER).

#### H32 — La granada y la furia se muestran como niveles, y la furia no dice qué hace (baja, bug; sin refutar)

- **Archivos**: Scripts/Tienda/TarjetaMejora.cs:200-203; Tienda/TiendaMejoras.cs:638, :651-668; Mejoras/Furia.asset y
  Granada.asset; Idioma/Resources/Textos.txt:151, :153, :168-171; comparar con UI/ProximoObjetivo.cs:97-101
- **Evidencia**: Con tope 1, la tarjeta dice «NIVEL 0/1», «0 → 1 granada cada 5 s» y «0 → 6 segundos de furia», y al
  comprarlas festeja «¡MÁXIMO!» y queda en MÁX. La derrota ya se corrigió para esto (objetivo_desbloqueo); la tienda
  no. La furia, que cuesta 5.000, no dice en ningún lado que da daño y cadencia x2 y velocidad x1,3.
- **Arreglo**: Para nivelMaximo == 1: sin NIVEL 0/1, el valor como bloqueada → desbloqueada, «¡DESBLOQUEADA!» al
  comprar, y una unidad de la furia que la explique.

#### H33 — Precio y monedas se truncan al mismo número compacto (157K, 1,2 M) y la tarjeta queda gris sin que se entienda (baja, bug; sin refutar)

- **Archivos**: Scripts/UI/FormatoNumeros.cs:26-38; Tienda/TarjetaMejora.cs:241-242, :262; UI/ContadorMonedas.cs:152
- **Evidencia**: Compacto trunca de 100K a 999K («157K» para 157.890) y desde el millón trunca a un decimal, mientras
  el estado de la tarjeta usa los valores reales. Con 157.200 monedas el contador y el precio dicen los dos 157K y la
  tarjeta está gris («faltan 690»); con 1.250.000 y un precio de 1.290.000, los dos dicen «1,2 M». Solo el daño y la
  vida pasan de 100K (desde el nivel ~22, en el muro; el daño cuesta ≈2,8 M en el nivel 30).
- **Arreglo**: Tres cifras significativas en Compacto desde el millón (1,29 M / 12,9 M / 129 M) y un decimal en el
  precio de la tarjeta hasta el millón, o al menos el precio redondeado hacia arriba y el saldo hacia abajo. Casos
  nuevos en ProbarFormatoNumeros.

#### H34 — «Mejor oleada: N» en la tienda es la completada, una menos que la alcanzada (baja, bug; sin refutar)

- **Archivos**: Scripts/Tienda/TiendaMejoras.cs:409-412; Idioma/Resources/Textos.txt:23, :147
- **Evidencia**: El pie muestra Progreso.MejorOleada, que es la última oleada completada: quien murió en la 12 lee
  «Mejor oleada: 11», mientras el libre dice «LLEGA A LA OLEADA 12» y el HUD mostró «Oleada 12».
- **Arreglo**: Mostrar MejorOleada + 1 como «Oleada más alta», o cambiar el texto a «Best wave cleared: {0}» / «Mejor
  oleada superada: {0}».

#### H35 — SEGUIR JUGANDO (¿SALIR?) no es una píldora: copia de VOLVER agrandada sin volver a redondear (baja, bug; sin refutar)

- **Archivos**: Scripts/UI/ConfirmarSalir.cs:72, :90; Escenas/Menu.unity:944, :1663
- **Evidencia**: Copia el VOLVER del idioma (300x80, redondeado para ese alto: Fondo con multiplicador 3,175 y Sombra
  con 0,797) y lo pasa a 440x100 sin recalcular: las puntas quedan de radio 40 en 50 de medio alto (un rectángulo
  redondeado; harían falta 2,54) y el halo sigue escalado para 148 de alto en vez de 168 (0,702). Es la trampa del
  Sliced que arregló 7276891; ProbarPildorasRedondas no lo ve porque se arma en Start.
- **Arreglo**: Después del sizeDelta, ConstructorUI.RedondearPildora sobre Visual/Fondo, y HaloDeBoton (o
  RedondearPildora) sobre la Sombra.

#### H36 — Tocar el COFRE y cerrar las misiones enseguida no lo abre: se abre solo al volver a entrar (baja, bug; sin refutar)

- **Archivos**: Scripts/UI/VentanaMisiones.cs:207, :247, :253
- **Evidencia**: TocarCofre pone abriendo = 0 y el cobro espera 0,6 s dentro de Update, pero después del «if
  (!Abierta) return». Cerrar no toca abriendo: si se cierra antes, el cofre no se cobra y se abre por sorpresa (sin el
  temblor) al reabrir la ventana. No se pierden monedas.
- **Arreglo**: Atender abriendo antes del return (cobrarlo aunque se cierre la ventana), o ponerlo en −1 en Cerrar.

#### H37 — Al volver por MEJORAS, la barra del nivel se llena escondida detrás de la tienda y suenan notas sin nada en pantalla (baja, bug; sin refutar)

- **Archivos**: Scripts/UI/VentanaLogros.cs:302, :381; Tienda/TiendaMejoras.cs:275
- **Evidencia**: TiendaMejoras.Abrir apaga extrasMenu (AreaSeguraMenu), donde está la píldora del nivel, pero
  VentanaLogros.Update sigue llenando la barra y toca una nota por nivel cruzado. experienciaMostrada es estática, así
  que al cerrar la tienda la barra ya está llena y el llenado no se ve nunca. Es el circuito que enseña la guía
  (MEJORAS, comprar, ¡A JUGAR!), y se sube ~1 nivel por partida.
- **Arreglo**: No avanzar experienciaMostrada mientras la tienda esté abierta (y quizá mientras
  VentanaRecompensaDiaria.Ocupada): la barra se llena al cerrarla.

#### H38 — El aviso de misión pisa los botones de furia y granada en 16:9, 16:10 y 4:3 (baja, bug; sin refutar)

- **Archivos**: Scripts/UI/AvisoDeMisiones.cs:37-39, :197-212; Editor/PruebasMejoras.cs:1767-1779
- **Evidencia**: Los detalles de logro miden 700-810 a 39,6 pt y «¡MODO LIBRE DESBLOQUEADO!» mide 703 a 72 pt. En 16:9
  el título llega a x 1312 y cruza el anillo de la furia (1256..1400, y 326..470), y un detalle de 710 cruza el anillo
  de la granada; en 4:3 el título entero cae sobre la furia. Se dibuja encima porque el aviso es el último hijo del
  canvas. La prueba solo mira 16:9 y 20:9 contra los carteles y la vida.
- **Arreglo**: Limitar el ancho del aviso a lo libre a la izquierda de los botones (o autoajustar el tamaño) y sumar
  los botones a la prueba, en 16:10 y 4:3.

#### H39 — Lo que se arma en código quedó sin neón: el cartel del capítulo, la guía de la primera partida, la barra del jefe (con puntas de elipse), los volúmenes y el VOLVER de la diaria (baja, calidad; sin refutar)

- **Archivos**: Scripts/Escenario/CapitulosDeEscenario.cs:492-496; Tutorial/GuiaPrimeraPartida.cs:114-117, :167;
  UI/BarraDelJefe.cs:95-100, :130; UI/ConstructorUI.cs:152-157; UI/SliderVolumen.cs:15;
  UI/VentanaRecompensaDiaria.cs:369; Editor/ConstructorNeon.cs:319-325; Escenas/WaveMode.unity:2883, :5271-5276
- **Evidencia**: VestirEscenaDeJuego solo cambia AvisoDeMisiones.materialContorno. CapitulosDeEscenario y
  GuiaPrimeraPartida siguen con Bangers SDF - Outline (f629c6e4) mientras el HUD pasó a Neon HUD (6962ae4a): el cartel
  «CAPÍTULO N» sale con el dorado viejo al lado del de la oleada, que lleva halo, y la guía, lo primero que ve un
  jugador nuevo, con blanco y un (1; 0,85; 0,3) que no es el amarillo neón de las monedas. La barra del jefe tiene
  Outline, el rojo viejo y Redondear(…, 8): los bordes de la Pildora quedan en 15,9 px, y en 26 de alto Unity achica
  solo el eje vertical a 13, puntas de 15,9x13 (la trampa del Sliced que resuelve RedondearPildora; la prueba no la ve
  porque se arma en código). Los volúmenes tienen el relleno dorado viejo y barra cuadrada; el VOLVER de la diaria usa
  el vidrio claro escrito a mano (solo se ve con vídeo). CLAUDE.md dice que el HUD es de neón.
- **Arreglo**: Que VestirPartida cablee materialContorno (Neon HUD o Neon) y los colores en CapitulosDeEscenario,
  GuiaPrimeraPartida y BarraDelJefe, con los colores de ConstructorUI; RedondearPildora en el marco, el relleno y el
  golpe de BarraDelJefe; ColorRelleno = ConstructorUI.Amarillo en los volúmenes; Tema.Elegir(…, RolDeTema.Vidrio) en
  la diaria. Sumar materialContorno a ProbarPartidaNeon.

#### H40 — La recompensa diaria no sale al volver a la app en un día nuevo (baja, bug; sin refutar)

- **Archivos**: Scripts/UI/VentanaRecompensaDiaria.cs:97-131, :242-247 (solo VigiaAplicacion.cs:73-82,
  MenuPausa.cs:54-62 y OfertaDeRevivir.cs:309-319 miran pausa o foco)
- **Evidencia**: Todo se decide en Start (racha, diaDeLaVentana, pendiente) y Update solo abre si pendiente. La
  actividad es singleTask: tocar el ícono con el proceso vivo trae el menú de ayer sin recargarlo, y la diaria recién
  sale al volver de una partida.
- **Arreglo**: Con OnApplicationPause(false) o un chequeo cada pocos segundos: si no está abierta y
  Progreso.DiaDeHoy() != diaDeLaVentana, volver a correr la lógica de Start (sin rehacer las texturas), respetando
  PrimeraVez y la tienda abierta.

### Controles, derrota, jefe y horda (H41 a H50)

#### H41 — Al perder el foco, la granada que se estaba apuntando se tira sola (baja, bug; sin refutar)

- **Archivos**: com.unity.ugui StandaloneInputModule.cs:166-244 y EventSystem.cs:464-469 (PackageCache);
  Scripts/UI/JoystickGranada.cs:71-80; Jugador/PlayerController.cs:291-293; UI/MenuPausa.cs:54-57
- **Evidencia**: El uGUI de Unity 6 manda pointerUp a los punteros que arrastraban cuando la app pierde el foco.
  JoystickGranada tira en OnPointerUp, y GranadaLista solo lo frena si MenuPausa ya pausó, pero el orden de
  OnApplicationFocus entre objetos no está definido. Además, un joystick apretado sin llegar a arrastrar no se suelta
  y sigue con su entrada al volver.
- **Arreglo**: En JoystickGranada.OnPointerUp, no tirar si !Application.isFocused o con JuegoCongelado; en
  MenuPausa.Pausar, reiniciar los dos FixedJoystick con OnPointerUp(null) y el estado apretado de la granada.
- **Nota**: Confianza baja (depende del orden de OnApplicationFocus).

#### H42 — La derrota no entra en un monitor 32:9 (Windows) (baja, bug; sin refutar)

- **Archivos**: Escenas/Perdiste.unity:1168-1173 (match 0; botones en y −290 con 140 de alto, título en 265, aviso de
  descanso en 360); ProjectSettings/ProjectSettings.asset:111
- **Evidencia**: Con match 0 el canvas mide 1920 × alto/ancho de alto: en 32:9 son 540 u (±270). Los botones (de −360
  a −220) quedan dos tercios afuera, y el título y el aviso, cortados. Se sale con Escape. En Android no pasa (21:9
  deja ±411).
- **Arreglo**: Match 0,5 en Perdiste, como la tienda y la pausa (revisando 16:9 y 21:9), o topear la relación de
  aspecto en Windows, si la build de Windows se va a distribuir.

#### H43 — La embestida pega a quien la toca al arrancar, aunque esté fuera de la cinta roja (baja, bug; sin refutar)

- **Archivos**: Scripts/Zombi/EnemyController.cs:1037-1042, :1062-1066; Zombi/JefePatrones.cs:357-384
- **Evidencia**: EmpezarEmbestida pone proximoGolpe = 0 y, con golpeaAlChocar, Golpear pega ×2,5 con cualquier
  contacto, sin mirar la dirección. Si el jugador está en contacto al costado o detrás cuando termina el aviso, el
  primer paso de física reporta el contacto: ~46 de daño en la 10, y la carga se corta a los 0,32 m. Contradice «no
  pisar lo rojo es no comerse el golpe».
- **Arreglo**: En Golpear con golpeaAlChocar, contar el choque solo si el jugador está delante en la dirección de la
  carga y dentro del ancho del cuerpo; si no, ignorarlo sin gastar el intervalo.

#### H44 — Con la derrota, el jefe camina a festejar congelado en la pose de su patrón (baja, bug; sin refutar)

- **Archivos**: Scripts/Zombi/JefePatrones.cs:231, :331-335, :442-453; Zombi/EnemyController.cs:1250
- **Evidencia**: LateUpdate sale si DerrotaEnLaPartida.Activa, antes de suavizar hacia cero, y VolverAPerseguir no
  resetea la inclinación, la altura, el balanceo ni el transform del modelo; EnemyController escribe el modelo recién
  al festejar. Si la carga mata al jugador, el jefe va hasta su lugar echado 24° hacia adelante (o agazapado, arqueado
  o torcido), al lado del cuerpo.
- **Arreglo**: En VolverAPerseguir, o en LateUpdate con la derrota activa, seguir suavizando la pose a cero mientras
  no esté festejando.

#### H45 — El jefe también cae bajo el techo de cadáveres y en el teléfono puede desaparecer de golpe (baja, bug; sin refutar)

- **Archivos**: Scripts/Zombi/EnemyController.cs:118-119, :599-606
- **Evidencia**: Morir llama a Devolver sin animación si cadaveres ≥ techo (5 en móvil), y el jefe no tiene excepción.
  Con sus invocados (12,8 de vida en la 10) cayendo con las mismas balas, o con una granada, es fácil tener 5
  cadáveres cuando muere: el clímax de la oleada es un jefe de 4 m que se esfuma en un cuadro.
- **Arreglo**: En Morir, dejar pasar al jefe aunque el techo esté lleno, mirando EsJefe antes de DejarDeContar (que lo
  pone en falso).

#### H46 — El zombi normal flota 10 cm sobre el piso (el mismo error que se arregló en los otros tres) (baja, bug; sin refutar)

- **Archivos**: Prefabs/Personajes/Zombi.prefab:258 (escala 0,5), :433 (y −0,794); ZombiRapido.prefab:432 (y −0,92);
  commit db09d6f
- **Evidencia**: La cápsula apoya su fondo 0,5 m bajo el pivote, y el origen del modelo (los pies) queda a 0,794 × 0,5
  = 0,397 m: flota 0,103 m. db09d6f bajó a −1 al tanque, al FASTER y al jefe porque «a −0,79 el jefe flotaba 0,4 m»
  (0,206 × 2), lo que confirma que los pies están en el origen. El normal quedó en −0,794 y el rápido flota ~3 cm.
  Afecta al zombi más común, a los invocados del jefe y al cadáver acostado.
- **Arreglo**: m_LocalPosition.y del modelo en −1 en Zombi.prefab (y en ZombiRapido.prefab si su origen también está
  en los pies), y corregir la frase de CLAUDE.md. La barra de vida mide los renderers y se ajusta sola.

#### H47 — Los invocados del jefe caminan con las piernas sincronizadas (baja, calidad; sin refutar)

- **Archivos**: Scripts/Zombi/EnemyController.cs:513; Zombi/JefePatrones.cs:558-576
- **Evidencia**: Cada aparición arranca el ciclo de andar en el cuadro 0 (Play(idAndar, 0, 0f)). La invocación saca 4
  (6 en furia) en el mismo cuadro y con el mismo Paso, así que marchan al unísono. El festejo evita esto a propósito
  con un desfase por zombi.
- **Arreglo**: animador.Play(idAndar, 0, Random.value).

#### H48 — El jefe muestra dos barras de vida: la flotante chica y la de arriba (baja, calidad; sin refutar)

- **Archivos**: Scripts/Zombi/EnemyController.cs:845-848, :976-980; Zombi/BarraDeVida.cs:31-60
- **Evidencia**: MostrarBarraDeVida corre para cualquier zombi y BarraDeVida no mira EsJefe. La flotante del jefe es
  la de 1,2 m de los chicos, sobre una cabeza a ~4,5 m; su altura se mide una sola vez con el primer golpe (puede
  tocar con el jefe agazapado o arqueado) y se reusa en las apariciones siguientes. BarraDelJefe ya muestra la vida
  arriba.
- **Arreglo**: Si no es a propósito, no crear la barra flotante cuando EsJefe.

#### H49 — Una excepción en el bloque de muerte deja un zombi inmortal que traba la oleada (baja, riesgo; sin refutar)

- **Archivos**: Scripts/Zombi/EnemyController.cs:829, :850-873 (estaMuerto en :852, Morir en :872), :1025
- **Evidencia**: estaMuerto se pone primero y Morir va último, con ocho llamadas a otros sistemas en el medio (mancha,
  partículas, Puntaje, NivelJugador, Moneda.Soltar, Progreso.ContarMuerte, Efectos). Si alguna tira, el zombi queda
  con estaMuerto y enUso: no se lo puede dañar ni pega, pero sigue en ZombisVivos y en SigueVivo, y la oleada no
  termina nunca (salida: pausa → menú → retomar). Hoy no hay un disparador concreto.
- **Arreglo**: Envolver los efectos en try/finally con Morir(empuje) en el finally (o try/catch con
  Debug.LogException): salir de la cuenta y volver al pool pasa siempre.

#### H50 — Verificar que el Paso y el Ritmo no se pierdan cuando un tanque o un FASTER vuelve del pool (baja, riesgo; sin refutar)

- **Archivos**: Scripts/Zombi/EnemyController.cs:507-514; Animaciones/Zombi.controller:149-160 (por defecto 1 y 1);
  ToonyTinyPeople/TT_demo_zombie.prefab:115 (KeepAnimatorControllerStateOnDisable 0)
- **Evidencia**: Al apagarse, el Animator vuelve a los valores por defecto (Paso 1, Ritmo 1). El zombi reusado los
  fija en el OnEnable de la raíz mientras el Animator se prende en un hijo durante la misma activación; si se
  reiniciara después, el tanque reusado correría y el FASTER iría a Paso 1. Ningún banco lo mira (corren en las
  oleadas 2-6, sin tanques ni FASTER). Lo más probable es que esté bien.
- **Arreglo**: Verificarlo en play: un tanque que vuelve del pool tiene que dar animator.GetFloat(«Ritmo») = 0. Si
  falla, fijar los parámetros en el primer FixedUpdate de la aparición.
- **Nota**: Confianza baja: es una verificación, no un error visto.

### Escenarios (H51 a H53)

#### H51 — Decorados: autos sobre el vacío, faroles que atraviesan autos, tumbas encimadas y una cerca que el jugador cruza (baja, bug; sin refutar)

- **Archivos**: Assets/Editor/ConstructorEscenarios.cs:172, :256-267, :279, :449-458; Escenas/WaveMode.unity:3009
- **Evidencia**: 6 de los 22 autos quedan en ±51,2 (la calle ±48 más 3,2) y el piso llega solo a ±50: enteros sobre el
  vacío, a 2-3 m del jugador contra la pared. Los faroles de (−3,8, 20,2) y (27,8, 44,2) atraviesan un auto, y el
  primero de esos autos está en medio del cruce, a 5 cm de otro. En el cementerio hay tres pares de tumbas encimadas,
  uno a 7 m del centro. La cerca y la reja van a 48 m y las paredes a 49,2-49,7: el jugador (radio 0,25) la cruza ~1
  m.
- **Arreglo**: Ciudad: no poner autos del lado de afuera de las calles ±48 y descartar los que queden a menos de ~1,5
  m de un farol o de otro auto. Cementerio: LejosDeTodos también para las tumbas sueltas. Cerca y reja a ~49,3. Volver
  a correr los constructores y la prueba de lógica.

#### H52 — En la ciudad el jugador y los zombis se hunden 14 cm en las veredas (baja, bug; sin refutar)

- **Archivos**: Assets/Editor/ConstructorEscenarios.cs:467-479
- **Evidencia**: Cada manzana es una losa de 0,14 m con un cordón de 0,18 m, sin collider: 16 manzanas de 14x14 m, un
  tercio del mapa. Los personajes caminan en y 0, así que sobre la vereda se les esconden los pies y los tobillos. Las
  manchas y los charcos ya se subieron a 0,2 m por la misma causa.
- **Arreglo**: Bajar la vereda a ~0,04 m y el cordón a ~0,06 (los charcos pueden volver a bajar; ver también H20).
  Colliders no: los zombis se trabarían en el cordón.

#### H53 — Las cajas nacen en filas de Z por la sobrecarga int de Random.Range (baja, calidad; sin refutar)

- **Archivos**: Scripts/PowerUps/PowerUp.cs:67
- **Evidencia**: Random.Range(-45, 45) tiene los dos argumentos int: devuelve un entero de −45 a 44, así que la Z
  nunca cae entre 44 y 45 y todas las cajas quedan en filas a un metro (verificado en el código). La X sí es float.
  Random.Range(0.5f, 0.5f) no sortea nada.
- **Arreglo**: Random.Range(-45f, 45f), y 0.5f directo en la Y.

### Textos e idiomas (H54 a H60)

#### H54 — El cartel de la oleada pasa a mayúsculas con la cultura del teléfono: en turco o azerí sale COİNS con otra fuente (baja, bug; sin refutar)

- **Archivos**: Escenas/WaveMode.unity:2750 (CartelOleada, m_fontStyle 17); com.unity.ugui 2.0.0
  Runtime/TMP/TextMeshProUGUI.cs:1905 y TMP_Text.cs:4109 (char.ToUpper); Idioma/Resources/Textos.txt:187;
  Scripts/Zombi/WaveManager.cs:217-220
- **Evidencia**: CartelOleada es el único TMP de juego con el bit UpperCase, y TMP convierte con char.ToUpper, que usa
  CurrentCulture: en tr y az, «i» pasa a «İ» (U+0130). La cmap de Bangers.ttf no tiene U+0130, así que cae en
  LiberationSans o sale como glifo faltante. El coins de cartel_bono en inglés tiene una i.
- **Arreglo**: Escribir cartel_oleada y cartel_bono ya en mayúsculas en la tabla y pasar m_fontStyle de 17 a 1 (desde
  ConstructorNeon.VestirPartida). Sumar a la prueba de lógica un chequeo que rechace el bit 16 en escenas y prefabs
  (los 7 botones del menú que lo llevan ya tienen sus textos en mayúsculas).

#### H55 — «Level» / «Nivel» nombra dos cosas distintas en la misma pantalla del modo libre (baja, calidad; sin refutar)

- **Archivos**: Idioma/Resources/Textos.txt:91 (aviso_nivel), :178 (hud_nivel); Scripts/Zombi/GeneradorZombis.cs:111;
  UI/AvisoDeMisiones.cs:115
- **Evidencia**: En el HUD del libre, «Nivel 4» es la dificultad (sube cada 45 s con su jingle); en la misma partida
  sale «¡NIVEL 13!» del nivel del jugador, con «+N monedas te esperan en el menú». Además, la tienda dice «NIVEL 3/16»
  y el logro dice «nivel … del modo libre».
- **Arreglo**: Solo tabla: aviso_nivel → «PLAYER LEVEL {0}!» / «¡NIVEL DE JUGADOR {0}!» (entra en los 1200 del aviso),
  o «¡SUBES AL NIVEL {0}!».

#### H56 — «faltan 1» en la tarjeta de la tienda (baja, bug; sin refutar)

- **Archivos**: Scripts/Tienda/TarjetaMejora.cs:262; Idioma/Resources/Textos.txt:155 (tarjeta_faltan)
- **Evidencia**: Usa «faltan {0}» con Max(1, costo − monedas), sin singular. Es el mismo error que ya se arregló con
  derrota_monedas_una y objetivo_*_una.
- **Arreglo**: Una fila tarjeta_falta_una («need {0}» / «falta {0}») elegida cuando la resta da 1.

#### H57 — El porcentaje se escribe 30% en la tienda y 30 % en los logros (baja, calidad; sin refutar)

- **Archivos**: Scripts/Progreso/Mejora.cs:107 (FormatoValor.Porcentaje); Idioma/Resources/Textos.txt:105
  (logro_critico)
- **Evidencia**: La tarjeta de críticos agrega «%» pegado en los dos idiomas; en español el logro escribe «{0} %» con
  espacio, que es la norma de la RAE. El mismo número se ve de dos formas según la pantalla.
- **Arreglo**: Que el sufijo salga de FormatoNumeros según el idioma: «%» en inglés y « %» con espacio duro U+00A0
  (Bangers lo tiene) en español.

#### H58 — El cartel de neón ZOMBIS de la ciudad está en español en un juego que arranca en inglés (baja, calidad; sin refutar)

- **Archivos**: Assets/Editor/ConstructorEscenarios.cs:364; Prefabs/Escenarios/Ciudad.prefab
- **Evidencia**: Es un TMP 3D horneado en el prefab, sin traducción a propósito. Los otros once carteles (BAR, 24H,
  MOTEL…) valen en los dos idiomas; ZOMBIS a un angloparlante le parece una falta de ortografía.
- **Arreglo**: Cambiarlo por una palabra que sirva en los dos idiomas (BRAINS, RIP, GRILL…) y volver a correr Armar
  ciudad.

#### H59 — Redacción: tres filas en inglés poco naturales y una en español desparejada (baja, calidad; sin refutar)

- **Archivos**: Idioma/Resources/Textos.txt:105 (logro_critico), :107 (logro_millonario), :148
  (tienda_pie_sin_oleadas), :225 (tut_caja_arma, es)
- **Evidencia**: «Play the waves to earn coins!», «Earn {0} coins playing» y «Get {0}% critical chance» no suenan
  naturales en inglés. En español, «mejora tu arma: cargador más grande y dispara mucho más rápido» mezcla un
  sustantivo con un verbo y repite «arma».
- **Arreglo**: «Play Waves mode to earn coins!», «Earn {0} coins by playing», «Reach {0}% critical chance» y «te da un
  cargador más grande y mucha más cadencia» (medidas: entran en sus cajas).

#### H60 — IconoDeBoton mide el texto con su propio margen: el icono queda ~4 veces más lejos y el grupo corrido (baja, bug; sin refutar)

- **Archivos**: Scripts/UI/IconoDeBoton.cs:37-38;
  Library/PackageCache/com.unity.ugui@8ccc29d23a79/Runtime/TMP/TMP_Text.cs:4836
- **Evidencia**: Pone margin.x = icono + separación y enseguida usa GetPreferredValues, que en este TMP suma
  m_margin.x al ancho. El hueco real queda en separación + (icono + separación)/2: JUGAR 56 u en vez de 13,6, MEJORAS
  40 en vez de 9,6, y el grupo queda lugar/4 a la izquierda (−21 u en JUGAR). Se ve en
  Builds/menu_tienda/0_referencia_menu.png. Ivan aprobó el aspecto actual.
- **Arreglo**: Restar texto.margin.x + margin.z al ancho preferido; si se quiere el hueco de hoy, subir separacion en
  las escenas.

### Sonido (H61 a H64)

#### H61 — Conectar o desconectar auriculares Bluetooth corta la música del menú hasta volver a cargarlo (baja, bug; sin refutar)

- **Archivos**: Scripts/Jugo/FuenteConVolumen.cs:33; Escenas/Menu.unity (AudioSource del GameObject 1452757527)
- **Evidencia**: En Android, cambiar la salida de audio reinicia el motor de Unity y se detiene lo que suena (el staff
  de Unity lo reconoce como limitación en 2022.3 y recomienda AudioSettings.OnAudioConfigurationChanged). Nadie en el
  proyecto escucha ese aviso, y la música del menú (MainMenu.mp3, PlayOnAwake y Loop) es la única fuente en loop con
  clip. No verificado en 6000.3.
- **Arreglo**: En FuenteConVolumen, suscribirse en OnEnable a AudioSettings.OnAudioConfigurationChanged y, si
  deviceWasChanged y la fuente es un loop con clip que estaba sonando, darle Play otra vez. Probarlo en el teléfono.

#### H62 — Al revivir, la explosión y el cartel salen juntos por la fuente neutra y pasan la escala (baja, bug; sin refutar)

- **Archivos**: Scripts/Jugador/PlayerHealth.cs:292; Jugo/Sonidos.cs:173, :297
- **Evidencia**: Revivir toca Efectos.Explosion (1,0, pitch 1) y, si despejó zombis, CartelOleada (0,8, pitch 1): los
  dos van por la fuente neutra. El limitador baja solo el cartel (a 0,78), porque la explosión ya salió y no se puede
  bajar. Mezclando los clips reales alineados, el pico llega a 1,22 (+1,8 dBFS) durante 7,7 ms, más que la granada del
  pendiente. Es un modelo con los clips reales, no una medición; una vez por partida y solo con la oferta de revivir
  (hoy, la APK de prueba).
- **Arreglo**: Tocar el cartel del revivir con un poco de variación de tono (va a una fuente propia y baja parejo) o
  bajarle el volumen. El arreglo de fondo es el del pendiente: un limitador sobre la salida.
- **Nota**: Amplía «El limitador de sonido no llega a todo»: otro caso, el del revivir.

#### H63 — El control MÚSICA de la pausa no cambia nada que se oiga (baja, calidad; sin refutar)

- **Archivos**: Scripts/UI/VolumenEnPausa.cs:34; Prefabs/Jugo/Efectos.prefab (AudioSource de música sin clip)
- **Evidencia**: La pausa arma el control de MÚSICA, pero en la partida no hay música (decisión de Ivan: la fuente de
  Efectos no tiene clip) y en pausa AudioListener.pause calla todo menos la interfaz. Quien lo mueve no oye nada
  distinto; el valor recién se nota en el menú.
- **Arreglo**: Sacar MÚSICA de la pausa y dejar EFECTOS (que ya suena al moverlo), o tocar ahí una muestra de la
  música del menú. Lo decide Ivan.

#### H64 — El buffer de audio está en «mejor rendimiento» (1024): el sonido puede llegar tarde respecto de lo que se ve (baja, riesgo; sin refutar)

- **Archivos**: ShowBies1/ProjectSettings/AudioManager.asset:12
- **Evidencia**: m_DSPBufferSize y m_RequestedDSPBufferSize están en 1024 desde 2022 (Best performance; Good latency
  es 512). A 48 kHz son 21 ms por buffer y Android encola varios: el golpe, la muerte y la nota de la moneda podrían
  salir 50-100 ms después del destello o del brillo. Estimación, sin medir.
- **Arreglo**: Medir primero en el teléfono (filmar un disparo contra un zombi y contar cuadros entre el destello y el
  golpe). Si molesta, probar 512 y cuidar que no aparezcan cortes en gama baja.
- **Nota**: Confianza baja; sin medir.

### Economía, progreso y guardado (H65 a H70)

#### H65 — «Completa N oleadas» (y «mata N» en el libre) se cumplen en un tercio de lo cotizado repitiendo las primeras oleadas (baja, exploit; sin refutar)

- **Archivos**: Scripts/Progreso/MisionesDiarias.cs:162, :294-297; Progreso/DesafioSemanal.cs:101, :168-171;
  Progreso/Economia.cs:49-53; UI/MenuPausa.cs:89-93; RestartScene.cs
- **Evidencia**: El objetivo es partidas × m oleadas, cotizado a 35,2 s por oleada en m = 40 (1.408 s / 40), pero
  cuenta cualquier oleada completada. Las oleadas 1-5 en ciclo con REINICIAR tardan ~10,7 s cada una: la difícil (100
  oleadas, paga 51.450) sale en ~19 min en vez de 59, y el semanal (600, paga 257.300) en ~1,9 h en vez de 5,9.
  Matando en el libre en nivel 1-3 salen ~5,9 zombis/s contra 2,6/s de la vara. No rinde más que jugar (~45 contra ~61
  monedas/s con las tasas reales del 26/9).
- **Arreglo**: Pesar cada oleada completada por (10 + 4n) / el promedio de la vara en RegistrarOleada, y contar para
  «mata N» solo las muertes de WaveMode (o pesar las del libre). Que la prueba de costos mida contra el camino más
  barato y no contra la misma vara (ver H94).
- **Nota**: Amplía «"Gana N monedas" y la misión de críticos se miden con varas que no son las del juego»: lo mismo
  pasa con las de oleadas y de matar. Baja porque no rinde más que jugar bien.

#### H66 — El objetivo de jefes cuenta m/10 jefes por partida y no floor(m/10): con el arreglo previsto cuesta hasta el doble (baja, bug; sin refutar)

- **Archivos**: Scripts/Progreso/MisionesDiarias.cs:141, :181; Progreso/DesafioSemanal.cs:90, :102;
  Editor/PruebasMejoras.cs:3437, :3443
- **Evidencia**: Una partida hasta la oleada m mata floor(m/10) jefes (jefeCadaOleadas 10), pero los dos objetivos
  usan m/10: en la 19 la misión pide 5 jefes, que son 5 partidas cuando se pagan 2,5, y el semanal pide 28 cuando se
  pagan 15. Con mejor oleada 9 aparecen 2 y 14 jefes para alguien que todavía no mató ninguno. La prueba usa la misma
  fracción y a propósito no mira los objetivos caros. Hoy lo tapa el farmeo de jefes; el arreglo decidido (contar en
  WaveManager al completar la oleada) lo deja a la vista.
- **Arreglo**: Math.Max(1, Math.Floor(m / 10.0)) jefes por partida en los dos Objetivo y en la prueba, y la puerta en
  mejorOleada ≥ 10. Hacerlo junto con el arreglo de los jefes farmeables.
- **Nota**: Amplía «Los jefes de las misiones y del semanal se farmean»: el arreglo decidido tiene que ir con este.

#### H67 — IMPARABLE (combo x25/x50/x100) se regala en el modo libre (baja, exploit; sin refutar)

- **Archivos**: Scripts/UI/ContadorCombo.cs:81-86; Jugo/Efectos.cs:146; Escenas/ShowBies1.unity:1327-1331;
  Zombi/WaveManager.cs:226; Escenas/WaveMode.unity:2092-2093; Progreso/Logros.cs:63
- **Evidencia**: El combo se corta con 1,5 s sin muertes, y en oleadas los 3 s de descanso lo cortan en cada oleada:
  x50 pide la 10 y x100 la 20-23 matando todo sin huecos. En el libre (abierto desde la 12) sale un normal cada 0,25 s
  de 5 de vida en el nivel 1 (~350 por minuto en PC), y x100 sale en uno o dos minutos; con la R se vuelve al nivel 1.
  Vale poco en monedas (~1.500 una vez), pero el oro pensado como hito del final lo va a tener todo el mundo cuando
  los logros vayan a Play Games.
- **Arreglo**: Que IMPARABLE cuente solo el combo de las oleadas (RegistrarCombo solo si hay WaveManager), o metas
  propias para el libre. Decide Ivan.

#### H68 — Guardar falla en silencio: con el almacenamiento lleno, toda la sesión queda solo en memoria (baja, riesgo; sin refutar)

- **Archivos**: Scripts/Progreso/Progreso.cs:797-817
- **Evidencia**: Cualquier excepción al escribir el .tmp (disco lleno, E/S) termina en un LogWarning; Guardar no
  devuelve nada y nadie cuenta las fallas. El principal no se toca (bien), pero las compras y cobros que «guardan en
  el acto» y las monedas de la sesión viven solo en memoria: al cerrar la app el jugador vuelve al último guardado
  bueno sin que nada le haya avisado.
- **Arreglo**: Que Guardar devuelva si pudo y lleve un contador de fallas seguidas; con dos o más, un aviso en el menú
  («no se pudo guardar: libera espacio», con fila nueva en Textos.txt), y reintentar en el próximo punto seguro.
- **Nota**: Pierde progreso, pero en un caso raro: por el criterio, raro → baja, igual que H69.

#### H69 — Cargar prefiere un principal viejo a un .tmp entero y más nuevo (baja, bug; sin refutar)

- **Archivos**: Scripts/Progreso/Progreso.cs:803-812, :834-841; Editor/PruebasMejoras.cs:2496-2526
- **Evidencia**: Cargar prueba principal, .tmp y .anterior en ese orden, pero un .tmp legible es siempre igual o más
  nuevo que el principal: un Guardar exitoso lo consume (:812), así que solo queda si falló o se cortó entre el fsync
  (:803) y el renombre (:809-812). Pasa si File.Delete o File.Move tiran (en Windows, con el archivo tomado por otro
  proceso) o con un corte en esa ventana: al abrir se carga el principal y el .tmp se pisa, y se pierde ese guardado
  (puede ser una compra; si el bloqueo dura toda la sesión, la sesión). ProbarGuardado no cubre principal sano con
  .tmp sano.
- **Arreglo**: Si el principal y el .tmp parsean los dos, tomar el .tmp. Sumar ese caso a ProbarGuardado.
- **Nota**: Pierde progreso, pero en una ventana de milisegundos o con un bloqueo raro: baja, igual que H68.

#### H70 — El reloj confiable puede quedar atrasado para un jugador legítimo hasta reiniciar el teléfono (baja, riesgo; sin refutar)

- **Archivos**: Scripts/Progreso/RelojConfiable.cs:50-58; Progreso/Progreso.cs:349-358, :587-598
- **Evidencia**: Confiable usa la marca más el tiempo real si coincide BOOT_COUNT y el reloj va más de 2 h adelante.
  (1) Si al cobrar la diaria el reloj estaba más de 2 h atrasado (hora automática apagada y mal puesta, o un teléfono
  que arranca con la hora mal y la corrige con la red) y el día igual era nuevo, la marca queda con esa hora, y al
  corregirse el reloj en el mismo arranque Confiable se queda atrasado. (2) La marca viaja en progreso.json: tras una
  restauración en otro teléfono o un restablecimiento de fábrica, si el número de arranque coincide, el día vuelve
  semanas atrás. Mientras tanto no hay diaria, misiones, semanal ni topes de vídeo nuevos, y la cuenta de «nuevas en»
  lo muestra. Es raro.
- **Arreglo**: Guardar con la marca un id de instalación (o ANDROID_ID) y descartarla si no coincide; no anclar con un
  reloj que va atrás de la marca anterior más el tiempo real; y confiar en el reloj si auto_time está prendido, junto
  con el pendiente de la fecha en el futuro y con H03. Si no, documentarlo junto a la fricción del reinicio.
- **Nota**: Confianza baja. Diseñarlo junto con H03 y con «Una fecha guardada en el futuro bloquea la diaria, las
  misiones, el semanal y los vídeos hasta esa fecha».

### Anuncios (para cuando se integre la red) (H71 a H76)

#### H71 — Con una red de anuncios real, el x2 de la derrota y de la diaria se pierde si Android mata el proceso durante el vídeo (baja, riesgo; sin refutar)

- **Archivos**: Scripts/Progreso/Progreso.cs:157-161, :555-563; Progreso/RecompensaDiaria.cs:80, :120-127;
  Anuncios/ServicioAnuncios.cs:185-186
- **Evidencia**: Lo que se duplica vive en memoria (MonedasDeLaPartida, partidaDuplicada, paraDuplicar) y el premio
  llega por el callback del SDK. Con AdMob o LevelPlay el vídeo es otra actividad; en teléfonos de 2-3 GB Android
  puede matar el proceso. El jugador ve el vídeo entero y el juego se reabre desde el menú con lo guardado en :185: no
  pierde progreso, pero se queda sin el x2 y sin forma de reclamarlo. Hoy no pasa (Nulo en Play; el Falso es un canvas
  del juego).
- **Arreglo**: Al integrar la red, anotar en el progreso un «premio en curso» (lugar, monto, partida) antes de
  Mostrar; si al reabrir sigue sin resolverse, ofrecerlo de nuevo o darlo con la regla de fallasPremiadasPorDia.
  Anotarlo en publicacion/pasos.md.
- **Nota**: Amplía «Cerrar la app en ¡HAS MUERTO! conserva la oleada en curso» (misma situación, Android matando la
  app durante el vídeo, del lado del x2). Va con «Antes de integrar la red de anuncios».

#### H72 — Los botones de la derrota siguen andando mientras se pide el vídeo del x2: premio perdido y la partida siguiente sin revivir (baja, riesgo; sin refutar)

- **Archivos**: Scripts/MenuPerdiste.cs:61, :71, :80, :91; Anuncios/OfertaDeDuplicar.cs:87-89, :97;
  Progreso/Progreso.cs:558, :643
- **Evidencia**: Retry, Menu y AbrirMejoras no miran MostrandoAnuncio (solo el Escape). Con una red real, tocar OTRA
  VEZ antes de que aparezca el anuncio recarga la escena y EmpezarPartida deja MonedasDeLaPartida en 0; al terminar el
  vídeo, Resolver gasta un uso del día y sube VideosDeLaPartida de la partida nueva, y DuplicarMonedasDeLaPartida da
  falso: el vídeo visto sin premio y la partida nueva ya no ofrece revivir. Con Falso no pasa, porque el cartel tapa
  la pantalla en el mismo cuadro.
- **Arreglo**: En los tres métodos (o en Salir), «if (ServicioAnuncios.MostrandoAnuncio) return;», como ya hacen la R
  y el Escape.

#### H73 — El x2 de la diaria se pregunta una sola vez: si la separación de 60 s lo frena en ese momento, se pierde el día (baja, bug; sin refutar)

- **Archivos**: Scripts/UI/VentanaRecompensaDiaria.cs:183, :221-224; Anuncios/ServicioAnuncios.cs:150, :274
- **Evidencia**: Cobrar pregunta PuedeOfrecer(DuplicarRegalo) una sola vez; si da falso, la ventana se va sola a los
  1,3 s y la diaria no vuelve ese día. PuedeOfrecer incluye la separación global de 60 s desde el último vídeo
  premiado: si se vio el x2 de la derrota pasada la medianoche y se entra al menú con la diaria nueva, no aparece
  VÍDEO: +N MÁS, que en el día 7 vale hasta 2 partidas. Raro: casi todos ven la diaria al abrir la app.
- **Arreglo**: Si lo único que falta es la separación, mostrar la oferta y habilitarla cuando se cumpla, o volver a
  preguntar cada ~0,5 s mientras la ventana está abierta (lo mismo que pasos.md pide para Listo).
- **Nota**: Amplía «Antes de integrar la red de anuncios» (volver a preguntar Listo mientras las ofertas están
  abiertas): pasa ya con el código de hoy, por la separación de 60 s.

#### H74 — El atrás que cierra un vídeo real podría llegar a Unity: solo lo filtra el revivir (baja, riesgo; sin refutar)

- **Archivos**: Scripts/Anuncios/OfertaDeRevivir.cs:249-257, :378 (ignorarAtrasHasta); MenuPerdiste.cs:87-93;
  UI/BotonAtrasMenu.cs:28-40; Anuncios/ServicioAnuncios.cs:236-250
- **Evidencia**: OfertaDeRevivir ignora el Escape 0,4 s después de volver de un vídeo porque podría llegar también a
  la actividad de Unity. MenuPerdiste solo mira MostrandoAnuncio, que Resolver apaga en el Update de VigiaAplicacion
  (en un orden no fijado), y BotonAtrasMenu no mira nada. Con la red real, cerrar el x2 con el atrás podría sacar al
  jugador de la derrota (perdiendo la oferta que debía volver si fue sin premio) o cerrar la diaria. No está
  confirmado que Android entregue ese atrás; hoy no pasa (con el Falso el atrás no hace nada y el AAB va en Nulo).
- **Arreglo**: Un ServicioAnuncios.AtrasIgnoradoHasta que ponga Resolver y que lean MenuPerdiste, BotonAtrasMenu y
  OfertaDeRevivir. Sumarlo a «Antes de integrar la red de anuncios» en publicacion/pasos.md (no está).
- **Nota**: Confianza baja. Va con «Antes de integrar la red de anuncios».

#### H75 — Integrar AdMob choca con el proyecto y pasos.md no lo dice: App ID en el manifiesto, EDM4U y callbacks abstractos (baja, riesgo; sin refutar)

- **Archivos**: publicacion/pasos.md (Anuncios); ShowBies1/Assets/Plugins/Android/mainTemplate.gradle:1-12;
  CLAUDE.md:1150-1152
- **Evidencia**: Sin la meta-data com.google.android.gms.ads.APPLICATION_ID, el SDK cierra la app al abrir
  (IllegalStateException de MobileAdsInitProvider), y en Plugins/Android no hay manifiesto propio. El plugin oficial
  de Unity trae EDM4U, que el proyecto evitó con la reseña porque se pelea con Unity 6. El camino de la reseña (JNI
  con AndroidJavaProxy) no alcanza: AndroidJavaProxy solo implementa interfaces, y RewardedAdLoadCallback y
  FullScreenContentCallback son clases abstractas.
- **Arreglo**: Sumar a pasos.md un paso sobre cómo entra el SDK: o el plugin, con EDM4U probado en 6000.3.14 y la
  plantilla propia, o un puente Java (play-services-ads en mainTemplate.gradle y un .java con los dos callbacks). En
  los dos casos, el App ID en el manifiesto y una prueba que lo verifique en la build con proveedor Real.
- **Nota**: Amplía «Antes de integrar la red de anuncios» (sección 5 / publicacion/pasos.md). Baja por el criterio:
  hoy no le pasa a nadie; es para no perder tiempo al integrar. Confianza alta.

#### H76 — La APK de prueba fuerza Falso siempre: con el proveedor Real, el SDK no se probaría nunca en el teléfono (baja, riesgo; sin refutar)

- **Archivos**: Assets/Editor/ConstructorAndroid.cs:76-78, :207-212; Scripts/Anuncios/ProveedorFalso.cs:26-29,
  :147-151
- **Evidencia**: BuildApk llama FijarProveedorDeAnuncios(Falso) sin condición, y el AAB solo se niega con Falso.
  pasos.md pide que la APK .prueba use los bloques de prueba de Google, lo que contradice el código. Además,
  ProveedorFalso.Listo siempre da verdadero y el vídeo solo termina en Recompensado o Cerrado: en el teléfono no se
  pueden probar NoDisponible, FallaAlMostrar ni una carga tardía.
- **Arreglo**: Forzar Falso solo si el asset está en Nulo (o un menú «APK con anuncios de prueba» que deje Real con
  los ids de prueba), y darle a ProveedorFalso un modo «sin vídeo» o «falla» configurable.
- **Nota**: Amplía «Antes de integrar la red de anuncios» (pasos.md: ids de bloque y de prueba).

### Estado, build y Play (H77 a H83)

#### H77 — FondoMenu y CapitulosDeEscenario limpian RenderSettings en OnDestroy como si fuera de la aplicación (es por escena) (baja, riesgo; sin refutar)

- **Archivos**: Scripts/UI/FondoMenu.cs:118-119, :159, :227-233; Escenario/CapitulosDeEscenario.cs:104-126, :131-136;
  Editor/ConstructorEscenarios.cs:1033-1049; Scripts/Puntaje.cs:28-38
- **Evidencia**: Los dos OnDestroy escriben RenderSettings.fog = false y ambientLight (el del menú, (0,5, 0,53, 0,56),
  contra (0,15, 0,19, 0,30) de la noche), con el comentario «son de la aplicación» (y CLAUDE.md: «Al descargarse la
  escena la niebla se apaga»); el propio FondoMenu.cs:118-119 dice lo contrario. RenderSettings es por escena: la
  escritura cae en la escena activa en ese momento, y una carga Single ya trae los de la nueva. Además
  CapitulosDeEscenario.Start lee la noche de la pradera de RenderSettings en vez de tenerla guardada. escenarios#2
  temía que, si el OnDestroy de la escena vieja corre después del Awake de la nueva con la nueva ya activa, el libre y
  el tutorial desde el menú arranquen con ~3 veces más ambiente y la pradera de las oleadas lo guarde como su noche.
  Hoy eso no pasa: Puntaje.Awake hace «else Destroy(gameObject)» sin DontDestroyOnLoad, y si la escena vieja siguiera
  viva en el Awake de la nueva, REINICIAR y OTRA VEZ romperían el puntaje; nunca se vio. Queda como riesgo si alguna
  carga pasa a LoadSceneAsync(Single).
- **Arreglo**: Sacar las escrituras de niebla y ambiente de los dos OnDestroy (dejar loTapado.Clear y los Destroy), y
  que PonerLaNoche escriba también escenarios[0], como ya hace con el 1 y el 2, en vez de leer RenderSettings en Start
  (leerlo en Awake no sirve). Corregir el comentario y la frase de CLAUDE.md. Para confirmar que hoy es inerte:
  Debug.Log(RenderSettings.ambientLight) en el primer Update de ShowBies1 entrando desde el menú debe dar (0,15, 0,19,
  0,30).
- **Nota**: escenarios#2 lo dio media (podía pasar hoy) y estaticos#1 baja (latente). Se resolvió por estaticos#1
  (inerte hoy con LoadScene síncrono): el singleton de Puntaje lo indica (si la escena vieja siguiera viva en el Awake
  de la nueva, REINICIAR dejaría sin puntaje), pero no se verificó en Unity. La verificación de una línea sigue
  valiendo.

#### H78 — Las ventanas del menú apagan su Abierta estático solo si el panel sigue vivo (baja, riesgo; sin refutar)

- **Archivos**: Scripts/UI/VentanaMisiones.cs:138-141, :435; UI/VentanaBestiario.cs:112-114;
  UI/VentanaLogros.cs:143-145; UI/VentanaRecompensaDiaria.cs:126-128; Resena/PedidoDeResena.cs:102-103;
  UI/BotonAtrasMenu.cs:48-64
- **Evidencia**: En los cuatro OnDestroy: «if (panel != null) Abierta = false;». El panel es un hijo armado en código,
  y al descargarse la escena Unity no garantiza el orden de OnDestroy entre padre e hijo. Si el hijo ya está
  destruido, Abierta queda en verdadero en la próxima visita al menú: PedidoDeResena no pide la reseña y el primer
  atrás se gasta en cerrar una ventana que no está. Hoy no hay camino: cada ventana tapa los toques con un Image a
  pantalla completa.
- **Arreglo**: Abierta = false sin condición (cada ventana vive una sola vez en el menú).

#### H79 — android:installLocation=preferExternal (baja, riesgo; sin refutar)

- **Archivos**: ShowBies1/ProjectSettings/ProjectSettings.asset:181 (AndroidPreferredInstallLocation: 1)
- **Evidencia**: Es el valor por defecto de Unity. En teléfonos con la SD adoptada como interna (gama baja de 32 GB)
  la app se instala ahí: si la tarjeta se saca o falla la app no abre, y carga y guarda (con fsync) más lento. Nada
  del juego lo necesita.
- **Arreglo**: Player Settings > Android > Install Location en Automatic o Force Internal, y verificar el manifiesto
  de la APK siguiente.
- **Nota**: Es otro ajuste que H09 (Install Location, no Preferred Data Location): cambiarlo no mueve progreso.json.

#### H80 — Un paquete preview del editor (ai.assistant) mete tres DLL de runtime en el juego (baja, riesgo; sin refutar)

- **Archivos**: ShowBies1/Packages/manifest.json:3; Library/.../assets/bin/Data/ScriptingAssemblies.json y
  RuntimeInitializeOnLoads.json; ProjectSettings/ProjectSettings.asset:777-778
- **Evidencia**: com.unity.ai.assistant 2.18.0-pre.2 tiene asmdefs de runtime: en la build entran
  Unity.AI.MCP.Runtime.dll, Unity.AI.Tracing.dll (que corre ConsoleSink.CaptureMainThreadId al arrancar) y
  Newtonsoft.Json.dll. Hoy es inofensivo (no hay permiso INTERNET), pero una actualización del paquete puede sumar red
  o Resources, como pasó con Sentis. Quedaron además los defines SENTIS_ANALYTICS_ENABLED y APP_UI_EDITOR_ONLY.
- **Arreglo**: Fijar la versión del paquete, que ConstructorAndroid anote o vigile la lista de ensamblados de la
  build, y borrar los dos defines.

#### H81 — CalidadDeAndroid lee QualitySettings del disco y no lo que Unity tiene cargado (baja, riesgo; sin refutar)

- **Archivos**: Assets/Editor/CalidadDeAndroid.cs:17-41; Assets/Editor/ConstructorAndroid.cs:258, :305
- **Evidencia**: La build usa la calidad en memoria, pero el chequeo lee el archivo. Si la trampa de QualitySettings
  se «arregla» revirtiendo el archivo con git con el editor abierto y Unity no lo recarga (no verificado), el chequeo
  pasa, el AAB sale fuera de Medium y el SaveAssets del finally vuelve a escribir el archivo sin el bloque.
- **Arreglo**: Leer también m_PerPlatformDefaultQuality en memoria (new
  SerializedObject(QualitySettings.GetQualitySettings())) y exigir Medium en los dos.
- **Nota**: Confianza baja.

#### H82 — Las capturas y el banner de la ficha de Play son de antes de la noche y del neón (baja, politica; sin refutar)

- **Archivos**: Builds/ficha/v5/hoja.png; publicacion/pasos.md:36-40; commits 0a4cc87 y 7276891 (25/9)
- **Evidencia**: Las 7 capturas subidas el 18/9 muestran el mundo de día, las ventanas crema y la tienda con letras,
  franja y sello MAXED!. HEAD es de noche, de carbón neón, y la tienda no tiene nada de eso. Que Play rechace la
  versión es poco probable, pero la ficha deja de representar la app.
- **Arreglo**: Rehacer capturas y banner con el HUD del teléfono y la calidad Medium (sin el contador de FPS, ver
  H83), y mandarlos en el mismo envío que el próximo AAB.
- **Nota**: Baja por el criterio: no es un error del juego ni un rechazo probable; es una tarea del próximo envío.

#### H83 — El contador de FPS se ve en la versión de Play (y en las capturas de la ficha) (baja, calidad; sin refutar)

- **Archivos**: Scripts/UI/ContadorFps.cs:15-25; UI/MedidorBalance.cs:38; CLAUDE.md:1210, :1321
- **Evidencia**: ContadorFps escribe «N FPS» sin mirar Debug.isDebugBuild (MedidorBalance sí lo mira): lo ve todo
  jugador, y en hoja.png se lee «60 FPS». CLAUDE.md lo describe como parte del HUD y prevé un interruptor: es para
  decidir, no un error.
- **Arreglo**: Mostrarlo solo en el editor y en builds de desarrollo, o detrás del Interruptor de OPCIONES, apagado
  por defecto.

### Rendimiento (sin medir) (H84 a H91)

#### H84 — Progreso.Guardar hace fsync y dos renombres en el hilo principal en momentos de acción (sin medir) (baja, rendimiento; sin refutar)

- **Archivos**: Scripts/Progreso/Progreso.cs:711-730, :790-818; Zombi/GeneradorZombis.cs:118-131;
  Zombi/WaveManager.cs:122-125; Progreso/MisionesDiarias.cs:119, :124; Progreso/DesafioSemanal.cs:82;
  UI/MenuPausa.cs:78; Anuncios/VigiaAplicacion.cs:75; Escenas/ShowBies1.unity:1336 (segundosPorNivel 45)
- **Evidencia**: Cada Guardar hace ToJson con prettyPrint (~4,2 KB), Flush(true) y hasta tres operaciones de
  directorio, todo sincrónico en el hilo principal y en el almacenamiento externo de Android (de pocos ms a más de 100
  ms en eMMC de gama baja; el cuadro siguiente además recupera varios pasos de física). Se llama cada 45 s en plena
  pelea del libre (al subir de nivel, el único guardado a mitad de la pelea), al empezar cada oleada (con el cartel y
  sin zombis), en cada toque de una racha de compras en la tienda, hasta 5 veces en el mismo cuadro al cerrar el día y
  la semana (también en partida si se cruza la medianoche) y dos veces al pasar a segundo plano.
- **Arreglo**: Medirlo primero en el teléfono (Development Build, un marker del Profiler alrededor de Guardar, modo
  libre al subir de nivel). Si pesa: serializar en el hilo principal y escribir + fsync + renombres en un hilo aparte,
  de a uno y con un candado; o guardar el libre cada varios niveles o en un cuadro sin pelea; en CerrarElDia y
  CerrarLaSemana, un solo Guardar al final; en la tienda, con una demora corta después de la última compra. Los de
  pausa, cierre, muerte y antes del vídeo quedan sincrónicos.
- **Nota**: Sin medir. Baja por el criterio (rendimiento sin medir); rendimiento#1 lo daba media. Cuatro frentes
  llegaron solos a lo mismo: medirlo es lo primero.

#### H85 — El Juntar de la primera salida del decorado cae con los primeros zombis de las oleadas 11 y 21 (baja, rendimiento; sin refutar)

- **Archivos**: Scripts/Escenario/CapitulosDeEscenario.cs:185-194, :298, :306-335, :363-372; Zombi/WaveManager.cs:123,
  :132; Escenas/WaveMode.unity:2096, :2877
- **Evidencia**: La primera vez cada pieza sale sola: localPosition de ~530 (cementerio) o ~990 (ciudad) transforms
  por cuadro durante ~1,9 s, y al aterrizar la última (≈3,15 s desde el cambio de oleada) Juntar hace
  StaticBatchingUtility.Combine de 528/986 renderers (733 y 1.218 objetos) en un cuadro. El primer zombi de la oleada
  sale a los 3,0 s y el segundo a 3,35 s. Una vez por capítulo y sesión; los decorados crecieron con el neón. Sin
  medir.
- **Arreglo**: Combinar en Armar (apagado, durante un cartel) y usar siempre la rama combinada (sale el decorado
  entero), o al menos dejar el Juntar para el próximo momento sin zombis vivos, o que el fundido termine antes de los
  3 s. Medirlo con el Profiler junto con el pendiente.
- **Nota**: Amplía «El decorado del capítulo siguiente se arma en plena pelea»: además del Armar, el Juntar de la
  primera salida cae con los primeros zombis.

#### H86 — El HUD rearma textos en cada cuadro: el contador de balas mientras se dispara, los colores de monedas y combo, y los indicadores radiales (baja, rendimiento; sin refutar)

- **Archivos**: Scripts/Armas/Balas.cs:25-28; UI/ContadorMonedas.cs:181; UI/ContadorCombo.cs:115;
  UI/BarraDelJefe.cs:151; UI/IndicadorMejoraCadencia.cs:34; UI/BotonFuria.cs:120; UI/IndicadorRecargaGranada.cs:17
- **Evidencia**: El contador de balas escribe solo cuando cambia, pero con la caja de arma o la furia cambia en todos
  los cuadros: string.Format con boxing más el parseo del rich text y la malla de TMP (y no pasa por FormatoNumeros:
  muestra «1000/1000»). Cambiar el color de un TMP rehace la malla (lo documenta NumeroFlotante.cs:84-89):
  ContadorMonedas lo hace en cada cuadro del salto (casi siempre con la escalera de monedas) y ContadorCombo en el
  desvanecido. BarraDelJefe aloca enemyType.name por cuadro. Los tres indicadores radiales cambian fillAmount en cada
  cuadro durante la caja (10 s) y los enfriamientos (120 s y 5 s) y ensucian el canvas del HUD.
- **Arreglo**: texto.SetText(plantilla, cantBalas, maxBalas), que no aloca (y a 10-15 Hz); color por vértices o
  CanvasRenderer.SetColor; el nombre del jefe solo al cambiar de jefe; los indicadores radiales en un sub-canvas
  propio. Medirlo con el Profiler junto con el resto de rendimiento.

#### H87 — El aplastado del golpe escala la raíz física del zombi en cada paso (baja, rendimiento; sin refutar)

- **Archivos**: Scripts/Zombi/EnemyController.cs:930-936, :965-973, :1272-1277;
  Prefabs/Personajes/ZombiBOSS.prefab:156-157
- **Evidencia**: GolpeVisual escala la raíz (1,15/0,85/1,15) y FixedUpdate la recupera en ~6 pasos (10 el jefe): los
  tres colliders y el Rigidbody se re-escalan sin parar bajo fuego. Por geometría la cápsula sube 7,5 cm (normal), 15
  (tanque) y 30 (jefe) y se ensancha hasta 22 cm; al recuperarse se mete en el piso y PhysX la saca de golpe, y los
  pies del modelo del jefe suben ~30 cm por golpe. Costo de PhysX y efecto visual sin medir.
- **Arreglo**: Aplastar el hijo modelo en LateUpdate, como ya hacen JefePatrones y el festejo, y no la raíz. Correr
  Golpe animado y Grabar al jefe antes y después.

#### H88 — 22 materiales del decorado en Standard con brillos y reflejos (no solo los tres pisos) (baja, rendimiento; sin refutar)

- **Archivos**: Assets/Escenarios/Ciudad/Vereda.mat (y Cordon, Pared, Ventana*, Vidrio, Poste, Rueda, Auto*,
  Contenedor, Lapida, Hierro, Madera, Mata, Tallo)
- **Evidencia**: Todos con m_Shader fileID 46 y _SpecularHighlights 1 / _GlossyReflections 1. Vereda son 16 losas de
  14x14 m (3.136 m², un tercio del mapa de la ciudad y donde se juega): buena parte de la pantalla con el shader más
  caro del pipeline integrado.
- **Arreglo**: Apagar Specular Highlights y Reflections (en ConstructorEscenarios, o por el inspector: tocar el float
  del YAML no prende la keyword), o pasar las piezas a Mobile/Diffuse. Mostrarle a Ivan el antes y el después (el
  brillo «mojado» de la ciudad).
- **Nota**: Amplía «El piso: sin Specular Highlights ni Reflections en los tres materiales»: son 22 materiales más.

#### H89 — Maximum Allowed Timestep 0,333: cada tirón se paga con hasta 16 pasos de física (baja, rendimiento; sin refutar)

- **Archivos**: ShowBies1/ProjectSettings/TimeManager.asset (Maximum Allowed Timestep 0.33333334, Fixed Timestep 0.02)
- **Evidencia**: Es el valor por defecto de Unity. Tras un cuadro largo (un shader nuevo, el Instantiate o el Combine
  del decorado, el fsync del guardado) el siguiente corre todos los pasos atrasados, hasta 16, cada uno con los
  FixedUpdate y OnCollisionStay de 35 zombis: alarga el tirón. No cambia el régimen normal.
- **Arreglo**: Bajarlo a 0,1 (5 pasos como mucho; también topea Time.deltaTime, así las balas no saltan 0,9 m en un
  cuadro). Medir en el teléfono que la pelea no cambie; si se toca el Fixed Timestep (pendiente de la cámara), hacerlo
  junto.

#### H90 — El GPU Instancing de los zombis solo juntaría las cabezas (baja, rendimiento; sin refutar)

- **Archivos**: ToonyTinyPeople/TT_demo/prefabs/zombiRapido.prefab:804, :903, :911;
  ProjectSettings/ProjectSettings.asset:431-433
- **Evidencia**: El cuerpo es SkinnedMeshRenderer y la cabeza MeshRenderer: el GPU Instancing no aplica a los cuerpos,
  y las cabezas pueden partirse por luces por vértice y SH. Android tiene el Dynamic Batching apagado: dos sprites por
  barra de vida, hasta 40 números TMP 3D y las manchas son ~100 draw calls chicas con fuego alto.
- **Arreglo**: Medir esperando solo una draw call menos por zombi (la cabeza); con el Frame Debugger contar barras,
  números y manchas antes de probar el Dynamic Batching.
- **Nota**: Amplía «Los zombis: GPU Instancing en TT_demo.mat y las cuatro Zombi*Piel.mat...»: baja lo que se puede
  esperar de ese arreglo.

#### H91 — El tutorial busca la granada en toda la escena en cada cuadro (baja, rendimiento; sin refutar)

- **Archivos**: Scripts/Tutorial/TutorialManager.cs:86-94
- **Evidencia**: En el paso de la granada, mientras no se tiró, FindFirstObjectByType<Granade>() en cada Update, sobre
  una escena con los 809 objetos de la pradera; dura lo que tarde el jugador en entender el botón.
- **Arreglo**: Anotar estadisticas.granadasTiradas al entrar al paso y compararlo, o que ThrowGranade avise.

### Pruebas y herramientas del editor (H92 a H100)

#### H92 — Los bancos de la horda juegan la oleada guardada en el editor: Muerte animada falla sola en múltiplos de 10 y desde la ~35 (baja, bug; sin refutar)

- **Archivos**: Assets/Editor/PruebaMuerteAnimada.cs:28-30, :100; Scripts/Zombi/WaveManager.cs:104-113;
  Escenas/WaveMode.unity:2092-2096
- **Evidencia**: Solo ModoLibre, Tienda y MenuYTienda arman un progreso conocido; Muerte, Golpe, Disparo y Derrota
  retoman Progreso.OleadaEnCurso. MuerteAnimada no mata al jefe (la oleada lo espera) y el jugador no dispara: en una
  oleada 10/20/30 la oleada no termina nunca. Además el último zombi de la oleada n sale a los 3 + 0,35·(9 + 4n) s y
  el banco mata hasta los 55 s: desde n ≈ 33-35 tampoco termina. La falla dice «la oleada no avanzó: los muertos no se
  cuentan», justo la regresión que vigila. PruebaDerrota se arregló el 27/9 por la misma causa; las otras no.
- **Arreglo**: Después de RespaldoDelBanco.Guardar, fijar el progreso como PruebaModoLibre.PrepararProgreso
  (ReiniciarTodo, GuardarOleadaEnCurso con una oleada fija, Guardar) en los cuatro bancos de la horda.
- **Nota**: Baja: es una falla falsa de un banco del editor, no un error del juego.

#### H93 — El revivir y el x2 de la derrota no los recorre ningún banco (baja, riesgo; sin refutar)

- **Archivos**: Assets/Editor/PruebasMejoras.cs:1502-1517 (solo BalasAlRevivir); PruebaDerrota (no recorre el
  revivir); PruebaDiaria.cs:103-117 (proveedor inyectable)
- **Evidencia**: De OfertaDeRevivir (440 líneas) solo se prueba una función estática, y OfertaDeDuplicar no aparece en
  ningún archivo de Assets/Editor. Nada prueba el timeScale en 0, SinPremio con el reloj corrido, el despeje sin
  monedas, Postergar al jefe, la gracia ni el NoSeCobro de la derrota. Es la monetización que toca la partida, y
  pendientes.md ya pide cambiarla antes de integrar la red; hoy solo se prueba en el teléfono.
- **Arreglo**: Un banco «Revivir (play)» con el proveedor del banco de PruebaDiaria: cerrar sin premio (la partida no
  termina y el reloj no se vence), premiar (vida, despeje sin puntos, el jefe no, 500 balas, gracia), rechazar, y el
  x2 de la derrota cerrado y premiado.
- **Nota**: Baja por el criterio (cobertura), pero conviene tenerlo antes del arreglo de «Cerrar la app en ¡HAS
  MUERTO!...» y de integrar la red.

#### H94 — Las siete pruebas de «cuesta lo que paga» son circulares, y los 120 s y 5 s están escritos a mano tres veces (baja, calidad; sin refutar)

- **Archivos**: Assets/Editor/PruebasMejoras.cs:1469-1492, :3417-3452; Scripts/Progreso/MisionesDiarias.cs:151-184;
  Progreso/Economia.cs:43; Prefabs/Personajes/Jugador.prefab:173, :221
- **Evidencia**: Matar, Oleada, Furia, Granadas y Jefe calculan el costo con la misma fórmula del objetivo, invertida,
  igual que monedas y críticos: solo puede fallar el redondeo, que el 0,75 absorbe. Las proporciones de premio
  (misiones :3365-3384, semanal :3511, bestiario :3676, diaria :4087-4092) dividen fracción·partidas·vara por
  partidas·vara: prueban constantes, no «toda la curva». La furia cada 120 s y la granada cada 5 están en Economia,
  MisionesDiarias y la prueba, y la verdad está en el prefab (enfriamiento 120, granadaCooldown 5): si se rebalancean
  ahí, nada falla.
- **Arreglo**: Comparar el 120 y el 5 de MisionesDiarias con Furia.enfriamiento y granadaCooldown del prefab, al lado
  de ProbarPuntoDeLaGranada, que ya lo lee; y medir contra la vara «de más» del pendiente cuando exista (y contra el
  camino más barato, ver H02 y H65).
- **Nota**: Amplía «Las dos pruebas son circulares» (dentro de «"Gana N monedas"...»): son siete, y las constantes no
  se comparan con el prefab.

#### H95 — El modo libre no lo juega ningún banco, y nadie prueba que desbloqueado lleve al libre (baja, riesgo; sin refutar)

- **Archivos**: Assets/Editor/PruebasMejoras.cs:2585-2590; PruebaModoLibre.cs:495-498; PruebaMenuYTienda.cs:1039
- **Evidencia**: GeneradorZombis (escalado por tiempo, un jefe a la vez, no subir de nivel muerto) solo lo toca
  MedirPartida, que se corre a mano. La prueba de lógica prueba EscenaPara sin progreso y con las oleadas, nunca «con
  la 11 completada da el libre»; PruebaModoLibre solo toca el botón bloqueado y PruebaMenuYTienda acepta cualquier
  modo tras ¡A JUGAR!. Un EscenaPara que devolviera siempre las oleadas pasaría todas las pruebas.
- **Arreglo**: El caso positivo en la lógica (RegistrarOleadaCompletada(11) en la carpeta de pruebas) y, en
  PruebaModoLibre, tocar el botón desbloqueado y mirar que cargue la escena 1.

#### H96 — Tres bancos, si el camino real falla, hacen la acción por atrás y pasan igual (baja, calidad; sin refutar)

- **Archivos**: Assets/Editor/PruebaTienda.cs:621-635, :922-943; PruebaTutorial.cs:515-522, :1000;
  PruebaMenuYTienda.cs:499-519
- **Evidencia**: PruebaTienda: si el raycast no cae en el botón, toca el botón directo, y esa caída no entra en ok[].
  PruebaTutorial: si caminar no agarra la caja, llama a OnTriggerEnter por reflexión y acepta cualquiera de los dos.
  PruebaMenuYTienda: si MEJORAS no abre, llama a tienda.Abrir() y solo lo anota. Algo que tape el botón de comprar, o
  una caja que el trigger no agarra, pasarían.
- **Arreglo**: Que el atajo cuente como falla, o como un OK con aviso que no sume al TODO OK.

#### H97 — El arreglo del 27/9 en JefePatrones.Empezar (cortar el zarpazo, dibujar la línea antes) no lo cubre ninguna prueba (baja, calidad; sin refutar)

- **Archivos**: Scripts/Zombi/JefePatrones.cs:407-427; Assets/Editor/PruebasMejoras.cs:2242-2387
- **Evidencia**: 8219044 hizo que Empezar llame a zombi.CortarZarpazo() y dibuje la línea o el anillo antes de
  prenderlos. ProbarPatronesDelJefe llega por reflexión a Aturdir, Terminar, Postergar y EmpezarEmbestida, pero no a
  Empezar: si se saca el CortarZarpazo no falla nada; solo lo vio la grabación del jefe revisada a ojo.
- **Arreglo**: En la vista previa: golpeEnCurso en true, invocar Empezar y mirar que se corte el zarpazo y que
  puedeZarpar quede en falso.

#### H98 — HerramientasProgreso: «Reiniciar todo» abre un modal, y los atajos dicen que guardaron aunque el progreso esté en solo lectura (baja, politica; sin refutar)

- **Archivos**: Assets/Editor/HerramientasProgreso.cs:54-63, :78-79, :118-120; Scripts/Progreso/Progreso.cs:756-764,
  :792, :857-878
- **Evidencia**: Es el único EditorUtility.DisplayDialog de Assets/Editor, y la regla de la casa (no pedirle clicks a
  Ivan en Unity) prohíbe los modales porque bloquean el editor y la sesión que lo maneja. Su texto dice que se pierden
  monedas, mejor oleada y niveles, pero ReiniciarTodo borra todo el JSON. Con el progreso en solo lectura (versión
  futura o archivo ilegible), Guardar vuelve sin escribir y la herramienta loguea «ahora hay N monedas en <ruta>»
  igual.
- **Arreglo**: Que el ítem copie progreso.json* a Library/ShowBies (o a un .bak), reinicie sin preguntar y deje la
  ruta de la copia en el log; que los atajos miren Progreso.SoloLectura y den LogError en vez del log de éxito.

#### H99 — Con dos copias de ShowBies abiertas, los respaldos de los bancos se pisan (baja, riesgo; sin refutar)

- **Archivos**: Assets/Editor/RespaldoDelBanco.cs:23, :85-108, :118-132; ProjectSettings/ProjectSettings.asset:15-16
- **Evidencia**: La copia va a Library/ de cada copia del proyecto, pero lo respaldado (persistentDataPath y los
  PlayerPrefs de IvRu/ShowBies) lo comparten todas: si A corre un banco y B corre otro en el medio, B respalda el
  estado de prueba de A y al final lo devuelve como si fuera el real. Hoy git worktree list muestra una sola copia.
  Borrar Library después de un cuelgue a mitad de un banco también se lleva la copia pendiente.
- **Arreglo**: Dejar la marca de «banco corriendo» junto al progreso (en persistentDataPath, con la ruta del proyecto)
  y que Guardar se niegue si encuentra la de otro proyecto.

#### H100 — La restauración de los bancos quedó duplicada, en parte sin efecto e incompleta (baja, calidad; sin refutar)

- **Archivos**: Assets/Editor/RespaldoDelBanco.cs:50-52, :147, :151-153; PruebaModoLibre.cs:747-758;
  PruebaDisparo.cs:86-97; PruebaTienda.cs:189-196, :979-980; PruebaDiaria.cs:841, :852-853; Scripts/Plataforma.cs:21
- **Evidencia**: RespaldoDelBanco ya devuelve el progreso, los PlayerPrefs, runInBackground y el teclado, y pone
  Progreso.datos en null; aun así DevolverPrefs y ModoTelefono/DevolverElTeclado lo repiten, y la reflexión que pone
  datos en null está copiada tres veces. El runInBackground = false de cada Terminar corre en play y, según el propio
  RespaldoDelBanco, «eso no queda»; PruebaDiaria.cs:841 lo pone en falso en el mismo EnteredEditMode en que el
  respaldo lo devuelve, en un orden no definido. Restaurar hace EditorPrefs.SetBool del teclado sin
  Plataforma.OlvidarPreferenciaDelEditor(): la caché tecladoEnElEditor (leída una vez por dominio) conserva lo del
  play hasta el próximo play. Tampoco devuelve el Idioma en memoria ni Progreso.soloLectura, ni sube
  Progreso.Revision.
- **Arreglo**: Borrar las copias; dejar en el respaldo lo que falta (Idioma.Cambiar y
  Plataforma.OlvidarPreferenciaDelEditor después de la línea 147) y sacar el runInBackground = false de cada Terminar.
  El SaveAssets en play solo donde haga falta (hipótesis de confianza baja: es lo que escribe el atlas de Bangers).

### Documentación (H101 a H114)

#### H101 — CLAUDE.md y MenuPausa.cs mandan cortar el input con Pausado; lo que corta de verdad es JuegoCongelado (baja, doc; sin refutar)

- **Archivos**: CLAUDE.md:181, :1566-1568, :1990 (paso 9 de «Para agregar una mecánica nueva»);
  Scripts/UI/MenuPausa.cs:12, :24-34; Jugador/PlayerController.cs:75, :293; Jugador/PlayerJS.cs:36;
  Jugador/Furia.cs:63, :92; Tutorial/GuiaPrimeraPartida.cs:126
- **Evidencia**: Tres lugares de CLAUDE.md, entre ellos la receta para una mecánica nueva, y la cabecera de MenuPausa
  dicen que quien lee input mira MenuPausa.Pausado. El código corta con JuegoCongelado (pausa, oferta de revivir o
  derrota), y CLAUDE.md:1117 y :1518 lo dicen bien. Desde el 23/9 la derrota corre a timeScale 1: siguiendo la receta,
  lo nuevo leería input con el ¡HAS MUERTO! abierto y detrás de la derrota.
- **Arreglo**: Cambiar las tres menciones y el comentario de MenuPausa.cs a MenuPausa.JuegoCongelado, y dejar Pausado
  solo para lo que depende del timeScale 0 del menú (el temblor, el combo, el audio).
- **Nota**: El primero de los doc: es la receta que siguen los agentes al sumar algo.

#### H102 — pedo.mp3 suena en cada derrota y la doc dice que el pedo no suena (baja, doc; sin refutar)

- **Archivos**: Escenas/Perdiste.unity:717-735 (AudioSource del GameObject Menu, m_PlayOnAwake 1, guid 400a6d27 =
  otros/pedo.mp3); Scripts/MenuPerdiste.cs:31-45; CLAUDE.md:68, :1805
- **Evidencia**: El AudioSource del objeto Menu de Perdiste está prendido, con PlayOnAwake, pedo.mp3, 2D y el objeto
  activo (verificado en el YAML). MenuPerdiste.Awake apaga cámaras, oído, luces y EventSystem de la escena aditiva,
  pero no sus AudioSource, y nadie pausa el audio: suena en cada derrota encima de la partida. Está así desde 97901db
  (3/11/2022, «sonidos»): casi seguro es a propósito. Pero CLAUDE.md solo dice que el pedo del Jugador «no suena
  nunca» (cierto: en las escenas de juego tiene PlayOnAwake 0) y el layout de otros lo lista sin uso. Con la horda
  festejando callada (27/9), es el único sonido propio de la derrota.
- **Arreglo**: Documentarlo como intencional en Jugo o en La derrota encima de la partida (o quitarlo si no lo es) y
  corregir el Descartado de pendientes.md.
- **Nota**: Amplía el Descartado «La derrota suena con pedo.mp3» («Refutado», sin motivo anotado en 2af1549): el YAML
  muestra que sí suena. No es un bug, es la doc.

#### H103 — El menú no usa la noche del cementerio desde el 25/9: cielo azul sobre la tierra violeta (baja, doc; sin refutar)

- **Archivos**: CLAUDE.md:1270-1272; Escenas/Menu.unity:2212-2216; Escenas/WaveMode.unity:2858-2862;
  Escenarios/Cementerio/PisoCementerio.mat:81; Scripts/UI/FondoMenu.cs:18-22
- **Evidencia**: 7276891 pasó el cementerio a violeta (cielo 0,05/0,03/0,09, luz 0,62/0,8/0,72, ambiente
  0,14/0,14/0,22). FondoMenu conserva la noche azul de antes (cielo 0,07/0,09/0,17, luz 0,55/0,66/1), los valores del
  cementerio en e6556d9: el menú pone cielo y niebla azules sobre PisoCementerio, ahora violeta. El comentario de
  FondoMenu dice «misma paleta que el capítulo 2» y «Las partidas siguen de día».
- **Arreglo**: Llevar la paleta violeta a los cinco campos de FondoMenu en Menu.unity, o corregir la frase de
  CLAUDE.md y el comentario.

#### H104 — «Un enemigo nuevo no pide tocar código» es falso desde el bestiario y la v6 (baja, doc; sin refutar)

- **Archivos**: CLAUDE.md:1972-1975; Scripts/Progreso/Bestiario.cs:19-34; UI/VentanaBestiario.cs:37-44;
  Progreso/NivelJugador.cs:32-43; Progreso/Logros.cs:71; Editor/PruebasMejoras.cs:3876-3891
- **Evidencia**: Bestiario.Tipos, MonedasPorTipo, coloresTipo y PuntosPorTipo son listas fijas de cinco tipos: un tipo
  nuevo no tiene tarjeta ni estrellas, no suma a COLECCIONISTA y migra con 1 punto. La prueba que «compara con los
  assets» recorre Bestiario.Tipos y no Assets/Zombies, así que no avisa.
- **Arreglo**: En el paso 1 de la receta, listar esos cuatro lugares y su nombre en la tabla; que la prueba recorra
  Assets/Zombies/*.asset.

#### H105 — La trampa de QualitySettings dice que nada lo delata; la build ya se niega (baja, doc; sin refutar)

- **Archivos**: CLAUDE.md:70, :1845-1851; Assets/Editor/ConstructorAndroid.cs:302-310; Editor/CalidadDeAndroid.cs;
  Editor/PruebasMejoras.cs:204
- **Evidencia**: CLAUDE.md dice que la build de Android saldría en el nivel por defecto «sin que nada lo delate», pero
  ConstructorAndroid corta la build si !CalidadDeAndroid.EstaEnMedium() y la Lógica de mejoras corre
  ProbarCalidadDeAndroid. CalidadDeAndroid no figura en el layout de Assets/Editor ni en las negativas de la sección
  Build de Android.
- **Arreglo**: Cambiar la frase (la build se niega y la prueba falla; revertir con git, con el cuidado de H81) y sumar
  CalidadDeAndroid al layout.

#### H106 — Frases viejas de anuncios y progreso en CLAUDE.md: «único momento», «hoy, el x2», «todavía no los muestra nada» (baja, doc; sin refutar)

- **Archivos**: CLAUDE.md:726, :731, :1019; Scripts/Progreso/RecompensaDiaria.cs:114; Progreso/MisionesDiarias.cs:247;
  Progreso/NivelJugador.cs:174; UI/VentanaBestiario.cs:191; Progreso/Logros.cs:61-72
- **Evidencia**: CLAUDE.md:1019 dice que «la derrota es el único momento» de los vídeos, cuando 1010-1012 listan
  revivir y la diaria. :726 dice que CobrarPremio es «hoy, el x2 de un vídeo», pero también lo usan la diaria, las
  misiones, el cofre, el semanal, el bestiario y los niveles. :731 dice que los contadores de por vida «todavía no los
  muestra nada», y los muestran el bestiario y los logros.
- **Arreglo**: Reescribir las tres frases con el estado actual.

#### H107 — «Mismo mapa que el libre» en las oleadas (baja, doc; sin refutar)

- **Archivos**: CLAUDE.md:18; Escenas/WaveMode.unity:2843-2876; Prefabs/Escenarios/Ciudad.prefab
- **Evidencia**: El área y los puntos de aparición son los mismos, pero el decorado cambia por capítulo (11-20
  cementerio, 21-30 ciudad). La nota sobre «Ciudad» sacada confunde ahora que existe un capítulo Ciudad.
- **Arreglo**: «La misma área; el decorado cambia cada 10 oleadas (ver Capítulos)».

#### H108 — Desactualizaciones chicas del layout y del texto de CLAUDE.md (baja, doc; sin refutar)

- **Archivos**: CLAUDE.md:51, :68, :70, :424, :492, :1758, :1963, :1983; ProjectSettings/TagManager.asset:7-8;
  Animaciones/Zombi.controller:10, :37, :63, :89; Editor/HerramientasProgreso.cs:21-28, :42;
  Scripts/Armas/BulletController.cs:117-120
- **Evidencia**: Faltan en el layout ProximoObjetivo (UI), CalidadDeAndroid (Editor) y combo.wav (otros). Las tags
  Zombi y Terreno tampoco se usan y no se mencionan. Dice «tres estados» y el controller tiene cuatro. El rótulo de
  los niveles de prueba omite granada y críticos 4. :424 dice que el zombi muerto «ya está apagado», pero el cadáver
  sigue 1,4 s prendido con los colliders apagados (mismo comentario en BulletController). «Los cuatro canvas» ya son
  más.
- **Arreglo**: Una pasada de texto sobre esas líneas (el comentario de MenuPerdiste.cs:8 va en H109).

#### H109 — Comentarios que describen la derrota vieja (jugador destruido, partida congelada) (baja, doc; sin refutar)

- **Archivos**: Scripts/Camara/CamaraJugador.cs:127-129; MenuPerdiste.cs:8-9, :52-53; Jugador/PlayerJS.cs:14-16;
  Jugo/FiltroBlancoYNegro.cs (Enganchar)
- **Evidencia**: CamaraJugador dice que PlayerHealth destruye al jugador y después carga Perdiste; ya no pasa ninguna
  de las dos. MenuPerdiste habla de una «partida congelada» y de que «quedó en timeScale 0», cuando la derrota lo pone
  en 1 (DerrotaEnLaPartida.cs:82). PlayerJS nombra Application.isMobilePlatform, y lo que decide es
  Plataforma.EsMovil. Engañan justo en el flujo que más cambió. FiltroBlancoYNegro.Enganchar no lo llama nadie (todo
  usa Tomar).
- **Arreglo**: Reescribir los tres comentarios según el flujo actual y borrar Enganchar.

#### H110 — VigiaAplicacion guarda al pasar a segundo plano, no al perder el foco, aunque su comentario y CLAUDE.md dicen que sí (baja, doc; sin refutar)

- **Archivos**: Scripts/Anuncios/VigiaAplicacion.cs:8-10, :73-82; CLAUDE.md (tabla de Anuncios y Persistencia)
- **Evidencia**: El código guarda solo en OnApplicationPause(true); OnApplicationFocus solo lleva la cuenta de la
  ausencia. En partida lo cubre MenuPausa.OnApplicationFocus, y en el menú y la derrota todo ya se guardó en el acto:
  no se pierde nada, lo que está mal es la descripción.
- **Arreglo**: Corregir el comentario y CLAUDE.md («al pasar a segundo plano»), o guardar también en
  OnApplicationFocus(false).

#### H111 — CLAUDE.md dice que la diaria espera a la primera partida terminada; también la abre la primera oleada completada (baja, doc; sin refutar)

- **Archivos**: Scripts/UI/VentanaRecompensaDiaria.cs:105; Tutorial/PrimeraVez.cs:22-25; CLAUDE.md (Primera vez,
  Recompensa diaria)
- **Evidencia**: La diaria corta con NoTerminoPartidas, que pide ninguna partida terminada y además ninguna oleada
  completada: quien completa la oleada 1 y sale por la pausa ve la diaria sin haber terminado ninguna partida. El
  comportamiento está bien; el documento dice otra cosa.
- **Arreglo**: «Desde la primera partida terminada o la primera oleada completada».

#### H112 — La niebla no se ve en la partida y el borde del mapa queda a la vista, aunque el código y CLAUDE.md digan que lo tapa (baja, doc; sin refutar)

- **Archivos**: Scripts/Escenario/CapitulosDeEscenario.cs:26, :231-234; Escenas/WaveMode.unity:21, :818, :845;
  CLAUDE.md (Capítulos)
- **Evidencia**: Cámara a 12 m, 70° y 60° de campo: el piso más lejano en pantalla está a 16,2 m de profundidad de
  vista, y la niebla lineal es 16-46 o 18-50: arriba queda entre 0,7 % y 0 % de niebla. Son falsos el tooltip («tapa
  el borde del mapa») y lo de «se cierra o se abre». Con el jugador en ±48 se ven 4 a 11 m de vacío más allá del piso
  de ±50.
- **Arreglo**: Corregir el tooltip, el comentario y CLAUDE.md. Si se quiere tapar el borde: piso a escala 20 (con el
  tiling de los materiales ×2) o una niebla que muerda (por ejemplo 10-24 m).

#### H113 — LeerEscena lee la escena abierta en memoria, no el disco, aunque el comentario y CLAUDE.md digan lo contrario (baja, doc; sin refutar)

- **Archivos**: Assets/Editor/PruebasMejoras.cs:1605-1622, :1861-1865; CLAUDE.md (trampa de las instancias de prefab)
- **Evidencia**: Si la escena ya está cargada en el editor, se lee esa, con sus cambios sin guardar y con los cambios
  a instancias de prefab que no se anotaron como override. El comentario de ProbarPartidaNeon y la trampa de CLAUDE.md
  («la prueba de lógica lee las escenas de disco, así que lo nota») lo dan por hecho: con WaveMode abierta (la dejan
  así los bancos) y un joystick recoloreado sin Anotar, pasaría igual.
- **Arreglo**: Si la escena está abierta y sucia, avisar o leerla en una escena de vista previa; corregir la frase de
  CLAUDE.md.

#### H114 — pasos.md no refleja el estado de Play, y el repo sigue público con gh-pages (baja, doc; sin refutar)

- **Archivos**: publicacion/pasos.md:21-26, :31, :55
- **Evidencia**: pasos.md sigue diciendo «versionCode 4 y 1.1.0» y deja Seguridad de los datos sin enviar, aunque la 5
  se mandó a revisión. El link nuevo de la política está «guardado sin enviar» y Play enlaza gh-pages; la API de
  GitHub da ivanlruiz/Showbies público con Pages. Si se privatiza (el motivo de la mudanza) antes de que Play muestre
  el link nuevo, el link de la política queda roto.
- **Arreglo**: Actualizar pasos.md al preparar el próximo envío (con H07 y H08) y dejar escrito el orden: primero el
  link nuevo en Play, después privatizar el repo.

### Código muerto y limpieza (H115 a H123)

#### H115 — MainMenu.PlayGame no lo llama nadie y CLAUDE.md lo da como camino al libre (baja, doc; sin refutar)

- **Archivos**: Scripts/MainMenu.cs:29-32; CLAUDE.md:93, :114; Scripts/UI/CurvasUI.cs:27; Scripts/Puntaje.cs:13;
  UI/SliderVolumen.cs:19, :73; Anuncios/LugarAnuncio.cs:15-16
- **Evidencia**: Los m_MethodName de MainMenu en Menu.unity son TocarJugar, GameModes, Tutorial y QuitGame; PlayGame
  no aparece ni se llama desde código. CLAUDE.md lo nombra en la tabla de índices y en la lista de caminos que pasan
  por ModoLibre.EscenaPara, que es donde mira quien sume un camino al libre. Otros restos sin uso:
  CurvasUI.SalidaElastica, Puntaje.enemy, SliderVolumen.slider (se escribe y nunca se lee) y
  LugarAnuncio.MonedasTienda / DuplicarBono.
- **Arreglo**: Borrar PlayGame y corregir las dos líneas de CLAUDE.md; borrar el resto o marcar las constantes de
  lugares como reservadas.

#### H116 — Anuncios: código muerto, comentarios viejos y el cartel del anuncio de prueba con voseo, fuera de la tabla y sin Bangers (baja, calidad; sin refutar)

- **Archivos**: Scripts/Anuncios/ServicioAnuncios.cs:54-64; Anuncios/OfertaDeDuplicar.cs:5-6;
  Anuncios/ProveedorFalso.cs:116-117, :122, :135-136; CLAUDE.md:1080
- **Evidencia**: HayProveedor y NombreDelProveedor no tienen llamadores, y su comentario habla de un interruptor del
  menú que ya no existe. OfertaDeDuplicar dice que es «el único lugar donde hoy entra un anuncio», y son tres.
  CLAUDE.md llama «semitransparente» a la ventanita del revivir, que desde 0a4cc87 tiene el fondo en alfa 0,92. El
  cartel de ProveedorFalso tiene «Terminó: tocá LISTO y cobrás», «SALTEAR» y «LISTO» escritos a mano, con voseo, en
  español aunque el juego esté en inglés y con la fuente por defecto. Solo la APK de prueba (el AAB se niega con
  Falso).
- **Arreglo**: Borrar las dos propiedades, corregir los dos comentarios y la frase de CLAUDE.md. En el cartel falso:
  «toca LISTO y cobra», «SALTAR» y la fuente del juego (pasarlo a la tabla es opcional).

#### H117 — FondoMenu: la rama del día parece muerta pero sostiene el piso de noche (baja, riesgo; sin refutar)

- **Archivos**: Scripts/UI/FondoMenu.cs:140-150, :163, :176-177, :224
- **Evidencia**: Con Tema.Oscuro fijo en verdadero, el menú arranca de noche y no vuelve: materialPiso, colorCielo e
  intensidadLuz del día no se ven nunca. Pero el piso solo se crea con if (materialPiso != null), y Aplicar le pone el
  de noche después: quien vacíe el material de día «porque está muerto» deja a los zombis del fondo caminando en el
  aire. El comentario de la línea 176 (el modo oscuro se toca en opciones) ya no vale.
- **Arreglo**: Crear el piso si hay materialPiso o materialPisoNoche y corregir el comentario; si el día no vuelve,
  sacar la rama entera (mezcla, objetivo, el fundido y los campos del día).

#### H118 — NewAudioMixer.mixer sin trackear: un mixer vacío que no usa nadie (baja, calidad; sin refutar)

- **Archivos**: Assets/NewAudioMixer.mixer, Assets/NewAudioMixer.mixer.meta
- **Evidencia**: Sin trackear desde el 15/9. Es el mixer por defecto (solo Master con Attenuation, sin parámetros
  expuestos); ningún YAML referencia su guid, ningún AudioSource tiene un output group y ningún script usa AudioMixer.
  El riesgo es que entre en un git add -A.
- **Arreglo**: Borrar los dos archivos. Si hace falta un limitador sobre la salida (pendiente del limitador), armar el
  mixer entonces, con sus grupos.

#### H119 — Assets propios sin ninguna referencia (ninguno entra en la build), dos de licencia desconocida (baja, calidad; sin refutar)

- **Archivos**: Escenarios/{Pradera,Cementerio,Ciudad}/Halo*, Charco*, Resplandor* (12 .mat); Materiales/Calle.mat,
  New Material.mat, Plano.mat; Sprites/120-1207602_*.jpg, 120-1207626_*.jpg, Showbies.png; Assets/Assets.index;
  Editor/ConstructorEscenarios.cs:640-651
- **Evidencia**: Un índice de guids con alcanzabilidad desde las 5 escenas del build, Resources y ProjectSettings deja
  20 archivos propios sin referencias. Los 12 materiales de brillo los fabrica NeonDe, que crea halo, halo suave,
  charco y resplandor de cada color aunque el escenario no los use, y cada Armar los vuelve a dejar. Calle.mat es de
  las calles que se sacaron (795c027). Los dos corazones .jpg vienen de un sitio de stickers, con licencia
  desconocida. Assets.index es un índice de Unity Search de 2023.
- **Arreglo**: Borrarlos, los corazones sobre todo. En NeonDe, crear cada material recién cuando se pide, o borrar al
  final de Armar los que quedaron sin referencia.

#### H120 — Overrides y claves serializadas de campos que ya no existen (baja, calidad; sin refutar)

- **Archivos**: Escenas/ShowBies1.unity:151, :159, :171, :187, :275, :323; Tutorial.unity (mismas líneas);
  WaveMode.unity:530, :538, :562, :650, :698, :2106-2169; Prefabs/Moneda.prefab:69;
  Prefabs/Personajes/ZombiBOSS.prefab:203
- **Evidencia**: Las instancias de Jugador en las tres escenas pisan campos borrados: PlayerController.speed, circulo,
  textoContBalas y explosion; GunController.textoContBalas; PlayerHealth.AudioSource (por ejemplo speed: 12 al lado
  del moveSpeed: 15 que sí vale). Moneda guarda «notas» y ZombiBOSS «anchoLinea», dos campos borrados. WaveMode tiene
  un GeneradorZombis apagado que nadie referencia, con una configuración vieja del libre (crecimientoVida 1,15). Las
  tags Zombi y Terreno no las usa nada.
- **Arreglo**: Remove Unused Overrides en las tres instancias de Jugador; reguardar Moneda y ZombiBOSS; borrar el
  GeneradorZombis de WaveMode y las dos tags; verificar los --- !u! contra HEAD.

#### H121 — Paquetes sin uso en el manifest (baja, calidad; sin refutar)

- **Archivos**: ShowBies1/Packages/manifest.json:4, :5, :8, :9, :11
- **Evidencia**: No hay NavMesh (com.unity.ai.navigation) ni PlayableDirector o Timeline (com.unity.timeline); tampoco
  se usan com.unity.collab-proxy (el repo es git) ni com.unity.multiplayer.center, y com.unity.ide.vscode está
  deprecado (test-framework se queda: lo pide ide.visualstudio). Suman compilación, importación y ruido; CLAUDE.md ya
  sacó otros paquetes por lo mismo.
- **Arreglo**: Sacarlos desde el Package Manager y comparar el tamaño del AAB y los warnings de la build antes y
  después.

#### H122 — Terceros sin uso: ~19 MB y 205 archivos fuera de la build (baja, calidad; sin refutar)

- **Archivos**: Assets/Thirdparty/Ciathyza/Gridbox Prototype Materials/ (demo y materiales HDRP/URP); Assets/TextMesh
  Pro/Documentation, Shaders/*.shadergraph; Assets/Joystick Pack/Examples, Documentaion.pdf;
  Assets/ToonyTinyPeople/TT_demo/*.tga, sample_scene
- **Evidencia**: Nada de esto se alcanza desde el build. La demo de Gridbox pesa 640 KB y sus materiales HDRP y URP se
  ven rosas en built-in (el juego usa una copia propia de prototype_512x512_green2). Del Joystick Pack solo se usa
  Fixed Joystick. Las tres .tga de ToonyTiny suman 8,5 MB. Todo pesa en cada clon y reimport.
- **Arreglo**: Borrar la demo y las carpetas HDRP/URP de Gridbox, los PDFs y los ejemplos del Joystick Pack; en
  ToonyTiny, solo lo que el índice confirme sin referencias (zombiRapido.FBX se queda como referencia de la trampa
  documentada).

#### H123 — Las cuatro ventanas del menú y los doce bancos están copiados, y hay archivos para partir (baja, calidad; sin refutar)

- **Archivos**: Scripts/UI/VentanaLogros.cs:206-239, :277-282; UI/VentanaMisiones.cs:187-228;
  UI/VentanaBestiario.cs:119-142; UI/VentanaRecompensaDiaria.cs:139-163, :234-240; Assets/Editor/PruebasMejoras.cs
  (4.578 líneas); Scripts/Zombi/EnemyController.cs:1115-1270
- **Evidencia**: Abrir (rearmar por Idioma o Tema.Revision, SetAsLastSibling, escala en 0), Cerrar y Festejar están
  casi letra por letra en las cuatro ventanas, y ya divergen (solo Logros sube el pixelDragThreshold). El andamiaje de
  los doce bancos también está copiado, y por eso solo PruebaDiaria maneja el corte (H22). PruebasMejoras es una sola
  clase de 4.578 líneas; el festejo de la derrota son unas 200 líneas de EnemyController (1.315).
- **Arreglo**: Una base VentanaDelMenu y un BancoEnPlay común (que resuelve H22 y H23 de una vez); PruebasMejoras en
  partial por subsistema; el festejo a un componente aparte.

---

## Ya conocido (sacado por el juez)

- `rendimiento#6`: «Las balas pueden atravesar al FASTER». Suma un motivo de rendimiento al barrido ya propuesto (que
  saca el collider de la bala), sin cambiar el arreglo.

---

## Lo que hay que medir en play o en el teléfono

Lo marcado "sin medir" en este informe sale de un modelo. Antes de arreglar, o para decidir, hace falta esto:

**En el teléfono**

- **H01, los toques de la pausa**: cuánto se desvía el dedo al tocar CONTINUAR (el desvío y el sesgo hacia abajo). Con
  2 mm o más, o con el 2 % o más de los toques por debajo de su borde, H01 pasa a alta. Después del arreglo, tocar
  justo debajo de CONTINUAR y que no pase nada.
- **H11 y H10, el jefe** (amplían «El jefe en el teléfono»): jugando, si se lo puede matar desde fuera de su ventana
  sin que ataque; y cuántas invocaciones salen vacías o con uno solo en el teléfono desde la oleada 20 y en el libre
  cuando el nivel le gana al jugador. Si se elige que la invocación pase el techo, los FPS con 43 zombis (junto con
  «720p nativo en el teléfono»).
- **H12, el borde rojo**: en un teléfono con cámara perforada o muesca, en horizontal, recibir un golpe y mirar el
  borde del lado de la cámara; con el arreglo, que llegue al borde real.
- **H03, el reloj**: si `auto_time` resincroniza con el modo avión, antes de usarlo como alivio.
- **Bajos**: H41 (el orden de `OnApplicationFocus` con la granada apuntada), H61 (conectar auriculares Bluetooth con
  la música del menú sonando), H64 (la latencia del audio: filmar un disparo contra un zombi y contar cuadros entre el
  destello y el golpe).

**En play, en el editor**

- **H01**: confirmar el signo de `raycastPadding` (el rectángulo verde al seleccionar la imagen) antes de aplicar el
  arreglo, y revisar el diff de las escenas.
- **H16, las piernas de la horda**: Grabar animaciones con la cámara del juego, antes y después, y mirar a 30 FPS que
  el rápido y el FASTER no se vean estroboscópicos con el tope.
- **H17 y H20, la noche**: una foto con la calidad del teléfono (como `FotosDeLosFaroles`) del chorro de balas y las
  tres cajas lejos de los faroles, antes y después, y otra con el gris de poca vida en 0,5; en la ciudad, la cinta y
  el anillo a 0,2 m encima del jefe, el jugador y los zombis (para reproducirlo: `oleadaEnCurso` 30 en
  `progreso.json` y OLEADAS).
- **H15, la diagonal**: loguear `rb.linearVelocity.magnitude` con W y con W+D (F1 no muestra velocidad).
- **H06, la tienda después de la diaria**: el caso nuevo de `PruebaDiaria`, con raycast de verdad.
- **H22 y H23**: después del andamiaje, cortar un banco a mano y volver a Play, y correr un banco con una escena
  sucia.
- **Bajos**: H50 (que un tanque o un FASTER que vuelve del pool dé `animator.GetFloat("Ritmo")` 0 y su `Paso`), H77
  (`Debug.Log(RenderSettings.ambientLight)` en el primer `Update` de ShowBies1 entrando desde el menú: tiene que dar
  (0,15, 0,19, 0,30)) y H93 (un banco del revivir y del x2 de la derrota).

**Con el Profiler (Development Build en el teléfono)**

- H84 (`Progreso.Guardar` al subir de nivel en el libre), H85 (el `Juntar` de la primera salida del decorado en las
  oleadas 11 y 21), H86 (el HUD que rearma textos), H87 (el aplastado del golpe), H88 (los 22 materiales del decorado,
  con el antes y el después para Ivan), H89 (el Maximum Allowed Timestep) y H90 (lo que de verdad junta el GPU
  Instancing de los zombis, con el Frame Debugger), junto con el resto de «Rendimiento: hacerlo en Unity y medirlo en
  el teléfono».

**Nota para cualquier banco**: después de correr bancos en play, mirar el `git status` de `ProjectSettings/`
(`QualitySettings.asset` pierde el Medium de Android) y, hasta que se arregle H22, no cortar un banco a mano.
