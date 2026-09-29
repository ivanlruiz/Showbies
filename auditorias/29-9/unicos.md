# Superauditoría del 29/9: hallazgos únicos

El juez juntó la lista en bruto de los 26 frentes (162 hallazgos, el detalle en `buscar_<frente>.md`): unió los
que tienen la misma causa, sacó lo que ya estaba en `pendientes.md` y revisó la gravedad con un solo criterio.

## Cuentas

- **123 hallazgos únicos**: 1 de gravedad alta, 22 media y 100 baja.
- **38 duplicados unidos**: 161 refs en los únicos menos 123 únicos (Σ|fuentes| − |únicos|).
- **1 ya conocido** (sacado; ver al final). 161 + 1 = 162, cada ref una sola vez.

## Criterio de gravedad

- **Alta**: perder progreso o monedas, traba sin salida, exploit que rompe la economía, rechazo de Play o un error
  que ve casi todo jugador.
- **Media**: error visible o frecuente, números mal.
- **Baja**: cosmético, raro o calidad.

Cómo se resolvieron los choques del criterio, igual para todos:

- **Raro pero pierde progreso → baja** (H68 y H69): pesa la probabilidad. **Frecuente y pierde progreso → alta**
  (H01: un toque errado sobre CONTINUAR, el botón más tocado de la pausa, borra la partida de oleadas retomable).
- **Rechazo de Play**: alta solo si es un rechazo en revisión. El versionCode repetido (H07) es un error de subida
  en el acto que se arregla en un minuto: media. La declaración de datos (H08) es la única que podría terminar en un
  rechazo en revisión, pero sin certeza de infracción: media.
- **Exploits con tope diario** (H02, como el pendiente de los jefes farmeables) o que multiplican lo que rinde jugar
  sin regalar monedas (H03): media, no alta.
- **Cosmético pero delante de todo jugador nuevo** (H14, el MODO LIBRE bloqueado, hasta la oleada 12): media, por
  «visible o frecuente»; el resto de lo cosmético, baja.
- **Solo editor**: media únicamente si destruye en silencio datos del desarrollador (H22, el progreso del editor y
  los informes; H23, escenas sin guardar). Las fallas falsas de un banco y la cobertura, baja.
- **Rendimiento sin medir, cobertura de pruebas, notas para integrar la red de anuncios y la ficha de Play**: baja
  (son calidad o riesgo futuro); dicen «sin medir» o a qué lista van.
- Los de gravedad media llevan en `nota` la confianza del frente, para la ronda `refutar:`.

Orden: alta, media y baja; dentro de cada una, lo que más le pasa al jugador primero, y al final lo del editor, la
documentación y el código muerto.

## Índice

| id | gravedad | tipo | hallazgo | fuentes |
|---|---|---|---|---|
| H01 | alta | bug | Los halos de neón roban toques: justo debajo de CONTINUAR, en la pausa, se REINICIA la partida | tema#1 |
| H02 | media | exploit | La furia arranca lista en cada partida: la misión de furia y el logro FURIOSO se cumplen reiniciando | combate#1, economia#1 |
| H03 | media | exploit | El reloj confiable solo se ancla al cobrar la diaria: desde cualquier reinicio del teléfono, adelantar el reloj da misiones, semanal y topes de vídeo nuevos sin límite | guardado#1 |
| H04 | media | riesgo | Retomar hace opcional la muerte en oleadas: cada oleada se reintenta con vida llena y las mejoras recién compradas, y así salen INTOCABLE, SUPERVIVIENTE y el récord | tienda#8, trampas#1 |
| H05 | media | riesgo | REINICIAR de la pausa y la tecla R borran la partida de oleadas sin preguntar | hud#2 |
| H06 | media | bug | La tienda que se abre sola después de la diaria compra con los toques que eran para la diaria | tienda#1 |
| H07 | media | riesgo | El versionCode 5 ya se usó y el AAB no lo revisa: la próxima subida la rechaza Play Console | build#1 |
| H08 | media | politica | La próxima subida es la primera con la librería de reseñas de Play: revisar Seguridad de los datos y la política de privacidad | build#3 |
| H09 | media | riesgo | Dónde vive progreso.json lo decide un desplegable (Preferred Data Location): pasarlo a Force Internal deja a todos sin progreso | build#2, android#8 |
| H10 | media | bug | Con el techo de zombis lleno, el jefe hace el aviso entero de la invocación y no sale nadie (o sale uno) | jefe#2, oleadas#1 |
| H11 | media | exploit | Al jefe se lo puede matar desde fuera de su ventana de ataque, sin que ataque nunca | jefe#3 |
| H12 | media | bug | El borde rojo del daño va dentro del área segura y se corta en recto del lado de la cámara del teléfono | hud#5, sonido#2, android#1 |
| H13 | media | bug | El botón de pausa del teléfono se monta sobre lo que va arriba al centro: el cartel del capítulo, el nombre del jefe y el panel del tutorial | hud#1, hud#3, tutorial#3 |
| H14 | media | bug | MODO LIBRE bloqueado: píldora gris del tema viejo con el halo verde de «jugar» y el texto verde oscuro | menu#1, tema#2 |
| H15 | media | bug | En PC el jugador corre un 41 % más rápido en diagonal | jugador#1 |
| H16 | media | bug | Los zombis chicos patinan: sus piernas van de 3 a 6 veces más lento que lo que avanzan | horda#1 |
| H17 | media | bug | De noche las balas, las cajas y la granada quedan fuera de la luz de relleno y se ven oscuras | combate#2 |
| H18 | media | bug | El jugador y los zombis entran en los edificios de la ciudad y desaparecen bajo el techo | escenarios#1 |
| H19 | media | bug | Si Unity 6 acota el pitch a 3, las escaleras y los arpegios repiten la nota de arriba | sonido#1 |
| H20 | media | bug | En la ciudad, las veredas tapan la línea de la carga y el anillo de la invocación del jefe | jefe#1 |
| H21 | media | bug | La caja de arma del tutorial nace detrás del panel de instrucciones | tutorial#1 |
| H22 | media | bug | Un banco en play cortado a mano queda armado y secuestra la próxima partida del editor, sin respaldo | pruebas#1, herramientas#1, muerto#1 |
| H23 | media | riesgo | Los doce bancos y «Poner la noche» abren escenas con OpenScene(Single) sin mirar si hay cambios sin guardar | escenarios#7, herramientas#2, muerto#2, pruebas#4 |
| H24 | baja | bug | Dos etiquetas del HUD no se leen en uno de sus estados: FURIA mientras dura (1,3:1) y la G de la granada recargando (2,2:1) | jugador#2, tema#3 |
| H25 | baja | bug | La cola de avisos de misión se sigue vaciando con el juego congelado: en la pausa se pierden y suenan todos juntos al volver; detrás de la derrota suenan y sacuden la cámara sin cartel | jugador#3, hud#4, tiempo#1 |
| H26 | baja | bug | Los carteles del capítulo, de la guía de monedas y de la furia se vencen detrás de la pausa | tiempo#2 |
| H27 | baja | bug | En PC, el panel final del tutorial se toca con la mira y el clic dispara | hud#8, tutorial#6 |
| H28 | baja | bug | Los zombis del tutorial pueden nacer a la vista | tutorial#2 |
| H29 | baja | bug | El tutorial dice que el cargador grande queda «para siempre», y dura la partida | tutorial#4, idiomas#4 |
| H30 | baja | bug | La derrota con la oferta del x2: el próximo objetivo no se entera del cobro y queda dentro del halo del botón del vídeo | tienda#2, hud#7, anuncios#7 |
| H31 | baja | bug | En PC, Espacio o Enter vuelven a comprar la última tarjeta tocada, y A/D mueven la selección | tienda#3 |
| H32 | baja | bug | La granada y la furia se muestran como niveles, y la furia no dice qué hace | tienda#4 |
| H33 | baja | bug | Precio y monedas se truncan al mismo número compacto (157K, 1,2 M) y la tarjeta queda gris sin que se entienda | tienda#5, idiomas#2 |
| H34 | baja | bug | «Mejor oleada: N» en la tienda es la completada, una menos que la alcanzada | tienda#6, idiomas#10 |
| H35 | baja | bug | SEGUIR JUGANDO (¿SALIR?) no es una píldora: copia de VOLVER agrandada sin volver a redondear | menu#2, tema#5 |
| H36 | baja | bug | Tocar el COFRE y cerrar las misiones enseguida no lo abre: se abre solo al volver a entrar | menu#3 |
| H37 | baja | bug | Al volver por MEJORAS, la barra del nivel se llena escondida detrás de la tienda y suenan notas sin nada en pantalla | menu#4 |
| H38 | baja | bug | El aviso de misión pisa los botones de furia y granada en 16:9, 16:10 y 4:3 | hud#6 |
| H39 | baja | calidad | Lo que se arma en código quedó sin neón: el cartel del capítulo, la guía de la primera partida, la barra del jefe (con puntas de elipse), los volúmenes y el VOLVER de la diaria | tema#6, tutorial#5, jefe#7, herramientas#4 |
| H40 | baja | bug | La recompensa diaria no sale al volver a la app en un día nuevo | android#2 |
| H41 | baja | bug | Al perder el foco, la granada que se estaba apuntando se tira sola | android#6 |
| H42 | baja | bug | La derrota no entra en un monitor 32:9 (Windows) | android#7 |
| H43 | baja | bug | La embestida pega a quien la toca al arrancar, aunque esté fuera de la cinta roja | jefe#4 |
| H44 | baja | bug | Con la derrota, el jefe camina a festejar congelado en la pose de su patrón | jefe#5 |
| H45 | baja | bug | El jefe también cae bajo el techo de cadáveres y en el teléfono puede desaparecer de golpe | jefe#6 |
| H46 | baja | bug | El zombi normal flota 10 cm sobre el piso (el mismo error que se arregló en los otros tres) | horda#2 |
| H47 | baja | calidad | Los invocados del jefe caminan con las piernas sincronizadas | horda#5 |
| H48 | baja | calidad | El jefe muestra dos barras de vida: la flotante chica y la de arriba | horda#6 |
| H49 | baja | riesgo | Una excepción en el bloque de muerte deja un zombi inmortal que traba la oleada | horda#3 |
| H50 | baja | riesgo | Verificar que el Paso y el Ritmo no se pierdan cuando un tanque o un FASTER vuelve del pool | horda#4 |
| H51 | baja | bug | Decorados: autos sobre el vacío, faroles que atraviesan autos, tumbas encimadas y una cerca que el jugador cruza | escenarios#3 |
| H52 | baja | bug | En la ciudad el jugador y los zombis se hunden 14 cm en las veredas | escenarios#6 |
| H53 | baja | calidad | Las cajas nacen en filas de Z por la sobrecarga int de Random.Range | combate#4 |
| H54 | baja | bug | El cartel de la oleada pasa a mayúsculas con la cultura del teléfono: en turco o azerí sale COİNS con otra fuente | idiomas#1 |
| H55 | baja | calidad | «Level» / «Nivel» nombra dos cosas distintas en la misma pantalla del modo libre | idiomas#3 |
| H56 | baja | bug | «faltan 1» en la tarjeta de la tienda | idiomas#7 |
| H57 | baja | calidad | El porcentaje se escribe 30% en la tienda y 30 % en los logros | idiomas#8 |
| H58 | baja | calidad | El cartel de neón ZOMBIS de la ciudad está en español en un juego que arranca en inglés | idiomas#5 |
| H59 | baja | calidad | Redacción: tres filas en inglés poco naturales y una en español desparejada | idiomas#9 |
| H60 | baja | bug | IconoDeBoton mide el texto con su propio margen: el icono queda ~4 veces más lejos y el grupo corrido | tema#4 |
| H61 | baja | bug | Conectar o desconectar auriculares Bluetooth corta la música del menú hasta volver a cargarlo | sonido#3 |
| H62 | baja | bug | Al revivir, la explosión y el cartel salen juntos por la fuente neutra y pasan la escala | sonido#4 |
| H63 | baja | calidad | El control MÚSICA de la pausa no cambia nada que se oiga | sonido#6 |
| H64 | baja | riesgo | El buffer de audio está en «mejor rendimiento» (1024): el sonido puede llegar tarde respecto de lo que se ve | sonido#5 |
| H65 | baja | exploit | «Completa N oleadas» (y «mata N» en el libre) se cumplen en un tercio de lo cotizado repitiendo las primeras oleadas | oleadas#3 |
| H66 | baja | bug | El objetivo de jefes cuenta m/10 jefes por partida y no floor(m/10): con el arreglo previsto cuesta hasta el doble | economia#2 |
| H67 | baja | exploit | IMPARABLE (combo x25/x50/x100) se regala en el modo libre | trampas#2 |
| H68 | baja | riesgo | Guardar falla en silencio: con el almacenamiento lleno, toda la sesión queda solo en memoria | guardado#3 |
| H69 | baja | bug | Cargar prefiere un principal viejo a un .tmp entero y más nuevo | guardado#2 |
| H70 | baja | riesgo | El reloj confiable puede quedar atrasado para un jugador legítimo hasta reiniciar el teléfono | guardado#6, android#5 |
| H71 | baja | riesgo | Con una red de anuncios real, el x2 de la derrota y de la diaria se pierde si Android mata el proceso durante el vídeo | guardado#4 |
| H72 | baja | riesgo | Los botones de la derrota siguen andando mientras se pide el vídeo del x2: premio perdido y la partida siguiente sin revivir | anuncios#3 |
| H73 | baja | bug | El x2 de la diaria se pregunta una sola vez: si la separación de 60 s lo frena en ese momento, se pierde el día | anuncios#4 |
| H74 | baja | riesgo | El atrás que cierra un vídeo real podría llegar a Unity: solo lo filtra el revivir | anuncios#6, android#4 |
| H75 | baja | riesgo | Integrar AdMob choca con el proyecto y pasos.md no lo dice: App ID en el manifiesto, EDM4U y callbacks abstractos | anuncios#1 |
| H76 | baja | riesgo | La APK de prueba fuerza Falso siempre: con el proveedor Real, el SDK no se probaría nunca en el teléfono | anuncios#2 |
| H77 | baja | riesgo | FondoMenu y CapitulosDeEscenario limpian RenderSettings en OnDestroy como si fuera de la aplicación (es por escena) | escenarios#2, estaticos#1 |
| H78 | baja | riesgo | Las ventanas del menú apagan su Abierta estático solo si el panel sigue vivo | estaticos#3 |
| H79 | baja | riesgo | android:installLocation=preferExternal | android#3 |
| H80 | baja | riesgo | Un paquete preview del editor (ai.assistant) mete tres DLL de runtime en el juego | build#6 |
| H81 | baja | riesgo | CalidadDeAndroid lee QualitySettings del disco y no lo que Unity tiene cargado | build#7 |
| H82 | baja | politica | Las capturas y el banner de la ficha de Play son de antes de la noche y del neón | build#4 |
| H83 | baja | calidad | El contador de FPS se ve en la versión de Play (y en las capturas de la ficha) | build#5 |
| H84 | baja | rendimiento | Progreso.Guardar hace fsync y dos renombres en el hilo principal en momentos de acción (sin medir) | oleadas#2, guardado#5, tienda#7, rendimiento#1 |
| H85 | baja | rendimiento | El Juntar de la primera salida del decorado cae con los primeros zombis de las oleadas 11 y 21 | escenarios#5, rendimiento#4 |
| H86 | baja | rendimiento | El HUD rearma textos en cada cuadro: el contador de balas mientras se dispara, los colores de monedas y combo, y los indicadores radiales | combate#3, rendimiento#7 |
| H87 | baja | rendimiento | El aplastado del golpe escala la raíz física del zombi en cada paso | rendimiento#2 |
| H88 | baja | rendimiento | 22 materiales del decorado en Standard con brillos y reflejos (no solo los tres pisos) | rendimiento#3 |
| H89 | baja | rendimiento | Maximum Allowed Timestep 0,333: cada tirón se paga con hasta 16 pasos de física | rendimiento#5 |
| H90 | baja | rendimiento | El GPU Instancing de los zombis solo juntaría las cabezas | rendimiento#8 |
| H91 | baja | rendimiento | El tutorial busca la granada en toda la escena en cada cuadro | rendimiento#9 |
| H92 | baja | bug | Los bancos de la horda juegan la oleada guardada en el editor: Muerte animada falla sola en múltiplos de 10 y desde la ~35 | pruebas#2 |
| H93 | baja | riesgo | El revivir y el x2 de la derrota no los recorre ningún banco | pruebas#3 |
| H94 | baja | calidad | Las siete pruebas de «cuesta lo que paga» son circulares, y los 120 s y 5 s están escritos a mano tres veces | pruebas#5 |
| H95 | baja | riesgo | El modo libre no lo juega ningún banco, y nadie prueba que desbloqueado lleve al libre | pruebas#6 |
| H96 | baja | calidad | Tres bancos, si el camino real falla, hacen la acción por atrás y pasan igual | pruebas#7 |
| H97 | baja | calidad | El arreglo del 27/9 en JefePatrones.Empezar (cortar el zarpazo, dibujar la línea antes) no lo cubre ninguna prueba | pruebas#9 |
| H98 | baja | politica | HerramientasProgreso: «Reiniciar todo» abre un modal, y los atajos dicen que guardaron aunque el progreso esté en solo lectura | herramientas#3 |
| H99 | baja | riesgo | Con dos copias de ShowBies abiertas, los respaldos de los bancos se pisan | herramientas#5 |
| H100 | baja | calidad | La restauración de los bancos quedó duplicada, en parte sin efecto e incompleta | muerto#3, estaticos#2 |
| H101 | baja | doc | CLAUDE.md y MenuPausa.cs mandan cortar el input con Pausado; lo que corta de verdad es JuegoCongelado | tiempo#3, claudemd#1 |
| H102 | baja | doc | pedo.mp3 suena en cada derrota y la doc dice que el pedo no suena | claudemd#7 |
| H103 | baja | doc | El menú no usa la noche del cementerio desde el 25/9: cielo azul sobre la tierra violeta | claudemd#4 |
| H104 | baja | doc | «Un enemigo nuevo no pide tocar código» es falso desde el bestiario y la v6 | claudemd#2 |
| H105 | baja | doc | La trampa de QualitySettings dice que nada lo delata; la build ya se niega | claudemd#3 |
| H106 | baja | doc | Frases viejas de anuncios y progreso en CLAUDE.md: «único momento», «hoy, el x2», «todavía no los muestra nada» | claudemd#5 |
| H107 | baja | doc | «Mismo mapa que el libre» en las oleadas | claudemd#6 |
| H108 | baja | doc | Desactualizaciones chicas del layout y del texto de CLAUDE.md | claudemd#8 |
| H109 | baja | doc | Comentarios que describen la derrota vieja (jugador destruido, partida congelada) | jugador#4 |
| H110 | baja | doc | VigiaAplicacion guarda al pasar a segundo plano, no al perder el foco, aunque su comentario y CLAUDE.md dicen que sí | guardado#7 |
| H111 | baja | doc | CLAUDE.md dice que la diaria espera a la primera partida terminada; también la abre la primera oleada completada | tutorial#7 |
| H112 | baja | doc | La niebla no se ve en la partida y el borde del mapa queda a la vista, aunque el código y CLAUDE.md digan que lo tapa | escenarios#4 |
| H113 | baja | doc | LeerEscena lee la escena abierta en memoria, no el disco, aunque el comentario y CLAUDE.md digan lo contrario | pruebas#8 |
| H114 | baja | doc | pasos.md no refleja el estado de Play, y el repo sigue público con gh-pages | build#8 |
| H115 | baja | doc | MainMenu.PlayGame no lo llama nadie y CLAUDE.md lo da como camino al libre | muerto#7 |
| H116 | baja | calidad | Anuncios: código muerto, comentarios viejos y el cartel del anuncio de prueba con voseo, fuera de la tabla y sin Bangers | anuncios#5, idiomas#6 |
| H117 | baja | riesgo | FondoMenu: la rama del día parece muerta pero sostiene el piso de noche | muerto#8 |
| H118 | baja | calidad | NewAudioMixer.mixer sin trackear: un mixer vacío que no usa nadie | muerto#4 |
| H119 | baja | calidad | Assets propios sin ninguna referencia (ninguno entra en la build), dos de licencia desconocida | muerto#5 |
| H120 | baja | calidad | Overrides y claves serializadas de campos que ya no existen | muerto#6 |
| H121 | baja | calidad | Paquetes sin uso en el manifest | muerto#9 |
| H122 | baja | calidad | Terceros sin uso: ~19 MB y 205 archivos fuera de la build | muerto#10 |
| H123 | baja | calidad | Las cuatro ventanas del menú y los doce bancos están copiados, y hay archivos para partir | muerto#11 |

---

## H01 — Los halos de neón roban toques: justo debajo de CONTINUAR, en la pausa, se REINICIA la partida (alta, bug)

- **Fuentes**: tema#1
- **Archivos**: ShowBies1/Assets/Scripts/UI/ConstructorUI.cs:60-73; Prefabs/UI/MenuPausa.prefab:946, :1866, :1941; Scripts/UI/MenuPausa.cs:89-94; Escenas/Menu.unity:1018, :2940, :5928 (y OLEADAS, TUTORIAL, MODO LIBRE, VOLVER y la ventana del idioma); Escenas/Tutorial.unity:1548, :1774

**Evidencia.** HaloDeBoton estira la Sombra ±34 u y le pone NeonPildora sin tocar raycastTarget, y las sombras de Menu, Tutorial y MenuPausa quedaron con m_RaycastTarget 1 (Perdiste, la tienda y lo armado con ConstructorUI.Boton están bien). La sombra es hija del botón, así que el toque sube a su Button, y donde dos halos se pisan gana el hermano que se dibuja después. En MenuPausa/Panel el orden es CONTINUAR (y 0..120), REINICIAR (−150..−30) y MENÚ, con 30 u de hueco: el halo de REINICIAR llega a y +4 y se queda con el hueco y con los 4 u de abajo de CONTINUAR (unos 2,4 mm en un 2400x1080). REINICIAR hace OlvidarPartidaSiEsOleadas y LoadScene sin preguntar. CONTINUAR es el botón que se toca al volver de cada pausa, también de las automáticas por perder el foco, y el dedo suele apoyar por debajo del objetivo. En e6556d9 la sombra de la pausa era el rect del botón corrido 8 u y el hueco no era de nadie: vino con 0a4cc87 (25/9, nunca auditado). En el menú, SALIR se lleva los 14 u de abajo de MEJORAS (abre ¿SALIR?) y, en 20:9 y 21:9, los 14 u de la izquierda de JUGAR; en el idioma, VOLVER se lleva 9 u de ESPAÑOL (cierra sin elegir).

**Arreglo.** sombra.raycastTarget = false en ConstructorUI.HaloDeBoton y, en RedondearBotones, para toda imagen NeonPildora; volver a correr Vestir el menú y Vestir la partida y revisar el diff. Sumar a ProbarPildorasRedondas (o ProbarPartidaNeon) que ninguna NeonPildora sea raycastTarget. Si se quiere un área de toque más generosa, que sea un rect invisible propio que no pise al vecino.

**Reproducción.** En la pausa (teléfono o PC), en WaveMode, tocar 1-2 mm por debajo de CONTINUAR: se recarga la escena y OLEADAS ya no dice CONTINUAR.

**Nota.** Alta por el criterio: pierde la partida de oleadas retomable (15-20 min en la oleada 30-40) con un toque errado sobre el botón más tocado de la pausa. Confianza alta en el mecanismo (YAML, orden de hermanos, cómo ordena el GraphicRaycaster); la frecuencia del toque errado, sin medir. Amplía «PLAY y SALIR quedan a 20 unidades en 20:9 y 21:9»: SALIR ya no solo está cerca, se lleva toques que caen sobre JUGAR. Ver H05 (REINICIAR sin confirmación), que agrava la consecuencia.

## H02 — La furia arranca lista en cada partida: la misión de furia y el logro FURIOSO se cumplen reiniciando (media, exploit)

- **Fuentes**: combate#1, economia#1
- **Archivos**: Scripts/Jugador/Furia.cs:43, :51-64, :99-109; UI/BotonFuria.cs:42-61; Progreso/MisionesDiarias.cs:168, :205; Progreso/Logros.cs:67; Jugador/PlayerController.cs:43, :306; UI/MenuPausa.cs:89-94; RestartScene.cs:23-27; Editor/PruebasMejoras.cs:3434; Prefabs/Personajes/Jugador.prefab:221

**Evidencia.** activadaEn arranca en float.NegativeInfinity en cada carga de escena, así que RestanteEnfriamiento da 0 y la furia está lista desde el primer cuadro, y Activar suma Progreso.ContarFuria (contador de por vida, sobrevive al LoadScene). La misión cotiza 120 s de juego por furia: en la oleada 40 la difícil pide 29 furias (58 min) y paga 51.450 (37.050 más el cofre). Con FURIA → pausa → REINICIAR (o la R, o pausa → MENÚ → OLEADAS, que conserva la oleada, o F+R en el tutorial) cada vuelta da una furia en 2-5 s: la misión sale en ~2,5 min y FURIOSO de oro (100 furias, más de 3 h) en ~8 min. Con granadaDisponibleEn pasa lo mismo, pero rinde solo ~2,5 veces más.

**Arreglo.** Llevar el enfriamiento que falta entre escenas: un static en tiempo real con su reset en SubsystemRegistration (o un campo en Progreso sin subir la versión) que Furia.Awake respete, y lo mismo para granadaDisponibleEn. Si no, contar para la misión y el logro solo las furias de una partida andando (por ejemplo, que después completó una oleada). En los dos casos, pasar el modelo del objetivo y de la prueba a (N−1)×120 s por partida, porque cada partida trae una furia gratis.

**Reproducción.** Con la furia comprada y la misión de furia del día: entrar al libre, tocar FURIA, pausa → REINICIAR, FURIA de nuevo (está lista), repetir. El contador de la misión sube uno por vuelta.

**Nota.** Confianza alta (los dos frentes llegaron solos a lo mismo). Media, como «Los jefes de las misiones y del semanal se farmean», que tiene la misma forma: la misión tiene tope diario, así que adelanta el premio del día, no lo multiplica; el logro es una sola vez.

## H03 — El reloj confiable solo se ancla al cobrar la diaria: desde cualquier reinicio del teléfono, adelantar el reloj da misiones, semanal y topes de vídeo nuevos sin límite (media, exploit)

- **Fuentes**: guardado#1
- **Archivos**: Scripts/Progreso/Progreso.cs:349-358 (único lugar que escribe la marca), :587-598, :660-668; Progreso/RelojConfiable.cs:53-54; Progreso/MisionesDiarias.cs:91-102; Progreso/DesafioSemanal.cs:57-73

**Evidencia.** La marca (relojUtc, relojMs, relojArranques) solo se escribe en RegistrarRecompensaDiaria. HoraConfiable la usa solo si es del mismo arranque; si no, devuelve el reloj crudo. Así que desde cualquier reinicio posterior al último cobro de la diaria (cerrando la ventana con el atrás), cada día adelantado cierra el día (CerrarElDia), arma tres misiones nuevas y el semanal, y vuelve a cero los topes de vídeo. Un juego de misiones paga 3,24 partidas-modelo y cuesta unas 2,5; rotando el día, del orden de +50 a +100 % de ingreso. CLAUDE.md y TAREAS lo documentan como «fricción: un reinicio por cobro», que vale para la diaria; para las misiones y el semanal alcanza un solo reinicio, y puede ser cualquiera de los que ya pasaron.

**Arreglo.** Anclar la marca también fuera de la diaria, pero solo si no hay marca o si es de otro arranque (relojArranques != arranques o relojMs > ms), en el primer HoraConfiable o Guardar de ese arranque. Dentro del mismo arranque no moverla hacia adelante con el reloj: Confiable acepta hasta +2 h, y reanclar en cada Guardar daría 1 h 59 por oleada (un día en unas 12 oleadas, sin reiniciar). Si se refresca, que sea con real = marcaUtc + (ms − marcaMs). Probarlo con Confiable, incluido: mismo arranque, +1 h 59, anclar, +1 h 59 otra vez, y el día no avanza.

**Reproducción.** Android: cobrar la diaria, reiniciar el teléfono, apagar la hora automática y adelantar 1 día. Abrir el juego y cerrar la diaria con el atrás: salen misiones nuevas y se cobran las cumplidas. Jugar hasta cumplirlas, adelantar otro día y repetir, siempre sin cobrar la diaria.

**Nota.** Confianza alta en el mecanismo; el +50-100 % es un modelo sin medir. Media y no alta: cada juego de misiones hay que jugarlo (~2,5 partidas), así que multiplica lo que rinde jugar, no regala monedas. No amplía «Una fecha guardada en el futuro bloquea la diaria...» (es el problema opuesto), pero conviene diseñar los dos arreglos juntos, y con H70.

## H04 — Retomar hace opcional la muerte en oleadas: cada oleada se reintenta con vida llena y las mejoras recién compradas, y así salen INTOCABLE, SUPERVIVIENTE y el récord (media, riesgo)

- **Fuentes**: tienda#8, trampas#1
- **Archivos**: Scripts/Zombi/WaveManager.cs:104-125, :126, :175-176; UI/MenuPausa.cs:54-79, :96-100; Jugador/PlayerHealth.cs:211; Progreso/AplicarMejoras.cs:29-59; Progreso/Logros.cs:61, :68; CLAUDE.md:410

**Evidencia.** MENÚ no olvida la oleada en curso, y cerrar la app pausa y guarda: pausa → MENÚ → OLEADAS (o cerrar desde recientes) vuelve a la misma oleada, sin límite, con vida llena, 500 balas y GolpesRecibidos en 0. Entre medio, las monedas de la partida se gastan en la tienda y AplicarMejoras.Awake las aplica al retomar: el ciclo morir → comprar → volver a la 1 deja de ser obligatorio. INTOCABLE (oleada sin golpes) y SUPERVIVIENTE (completar la 10/25/50) se sacan reintentando oleada por oleada, y HighScore_3 y MejorOleada, lo que iría a las tablas de Play Games, dejan de tener techo de habilidad; además puntosEnCurso sale del progreso.json editable y termina en el récord. En monedas el farmeo da solo ×1,0-1,2 por minuto (modelo). CLAUDE.md:410 dice que salir de una oleada que se perdía «es a propósito»; lo nuevo es que no tiene límite y lo que arrastra a los logros y al récord. El arreglo del pendiente (guardar el principio del capítulo al morir) deja MENÚ → OLEADAS igual.

**Arreglo.** A decidir con Ivan, porque todas las opciones cambian su pedido: (a) retomar al principio del capítulo y no en la oleada; (b) marcar la partida como retomada (campo nuevo, sin subir la versión) y que no cuente para INTOCABLE ni SUPERVIVIENTE ni mande puntaje u oleada a las tablas; (c) que retomar aplique las mejoras de cuando empezó la partida. Lo mínimo antes de conectar las tablas es (b), más la mudanza de progreso.json al interno (ya anotada).

**Reproducción.** Progreso en la oleada 20: al primer golpe, pausa → MENÚ → OLEADAS («CONTINUE WAVE 20»); vuelve la misma oleada con vida llena. Repetir hasta terminarla sin golpes: sale INTOCABLE.

**Nota.** Amplía «Punto de control por capítulo en las oleadas», que ya lo llama un revivir gratis: suma los logros, el récord y la compra a mitad de partida. Confianza alta en el mecanismo. Media por los logros y el récord, no por las monedas.

## H05 — REINICIAR de la pausa y la tecla R borran la partida de oleadas sin preguntar (media, riesgo)

- **Fuentes**: hud#2
- **Archivos**: Scripts/UI/MenuPausa.cs:89-94; RestartScene.cs:22-27; Zombi/WaveManager.cs:28-33; Jugador/Furia.cs:25

**Evidencia.** Un toque en REINICIAR (el botón del medio de la pausa) o la R llaman a OlvidarPartidaSiEsOleadas, que olvida la oleada en curso y lo guarda en disco. RestartScene solo mira MostrandoAnuncio y OfertaDeRevivir.Activa, así que la R anda también con la pausa abierta, y está justo encima de la F de la furia. Cerrar la app conserva la partida y REINICIAR la borra: son 15 a 20 minutos en la oleada 30-40. Ivan ya pidió confirmación para SALIR por lo mismo.

**Arreglo.** Pedir confirmación con una ventana como la de ConfirmarSalir («¿EMPEZAR DE CERO? Perderás la oleada N») cuando hay algo que perder. Para la R: mantenerla ~0,6 s o apretarla dos veces, y no aceptarla con MenuPausa.Pausado.

**Reproducción.** PC, WaveMode: llegar a la oleada 5 y apretar R (o pausa → REINICIAR): vuelve a la 1 y OLEADAS ya no dice CONTINUAR.

**Nota.** Confianza media (depende de cuánto se toque sin querer). Causa distinta de H01 (allí el toque cae en REINICIAR sin apuntarle; acá falta la confirmación), pero la misma ventana cierra la consecuencia de los dos. No es «El récord de una partida abandonada», que trata de la comparación con el récord.

## H06 — La tienda que se abre sola después de la diaria compra con los toques que eran para la diaria (media, bug)

- **Fuentes**: tienda#1
- **Archivos**: Scripts/Tienda/TiendaMejoras.cs:261-315, :417-421; Tienda/TarjetaMejora.cs:270-276, :392-397; Prefabs/UI/Tienda.prefab:1664-1666; Prefabs/UI/TarjetaMejora.prefab:1927-1929; UI/VentanaRecompensaDiaria.cs:48-49, :180-184, :360-371

**Evidencia.** Abrir deja el panel y las tarjetas con alfa 0 pero con el CanvasGroup interactable y blocksRaycasts en 1, y nada los apaga durante la entrada. Con la fila al principio, COBRAR (0,−208; 560x130) pisa casi entero el botón de CRÍTICOS (x −146..174, y −257..−139) y VOLVER (330,−208) pisa el de VIDA, en 16:9 y en 20:9. En el día 1 (150 de la diaria más lo de la primera partida) CRÍTICOS es comprable, y la guía de la primera compra se salta el daño.

**Arreglo.** Que el panel no acepte toques (grupoPanel.interactable o blocksRaycasts en falso) hasta fade ≥ 1 y el fin de la entrada de las tarjetas (~0,5 s), o que IntentarComprar ignore los toques con tiempoAbierta < 0,5. Sumar el caso a PruebaDiaria.

**Reproducción.** Con diaria disponible y 150-300 monedas: morir, MEJORAS en la derrota, COBRAR en la diaria y volver a tocar en el mismo lugar a los ~1,6 s (1,3 s de espera + 0,25 s de salida): se compra CRÍTICOS. Con vídeo (APK de prueba): COBRAR y doble toque en VOLVER: se compra VIDA.

**Nota.** Confianza media. Media porque gasta monedas en una mejora que no se eligió (no se devuelven) y rompe la guía de la primera compra justo en el día 1, que es el circuito que enseña.

## H07 — El versionCode 5 ya se usó y el AAB no lo revisa: la próxima subida la rechaza Play Console (media, riesgo)

- **Fuentes**: build#1
- **Archivos**: ShowBies1/ProjectSettings/ProjectSettings.asset:178; Assets/Editor/ConstructorAndroid.cs:200-260, :327-333; publicacion/pasos.md:33

**Evidencia.** AndroidBundleVersionCode es 5 y bundleVersion 1.2.0, y la 1.2.0 (5) ya se mandó a revisión el 18/9 (el build_result del 27/9 también dice 5). ArmarAab revisa keystore, anuncios, paquete, nombre, calidad y escenas, pero el versionCode solo lo anota después de armar.

**Arreglo.** Subir a 6 (y 1.3.0), y que ArmarAab se niegue con Fallar si bundleVersionCode no es mayor que un «último subido» guardado en el repo (una constante o un archivo en publicacion/).

**Reproducción.** Build > Android AAB (release) → subir Builds/ShowBies.aab a Play Console → «El código de versión 5 ya se usó».

**Nota.** Confianza alta. No es alta pese a «rechazo de Play»: Play Console rechaza la subida en el acto, sin revisión, y se arregla en un minuto. Lo que importa es no olvidarlo ni armar un AAB que se crea subible.

## H08 — La próxima subida es la primera con la librería de reseñas de Play: revisar Seguridad de los datos y la política de privacidad (media, politica)

- **Fuentes**: build#3
- **Archivos**: ShowBies1/Assets/Plugins/Android/mainTemplate.gradle:11; Assets/Scripts/Resena/PedidoDeResena.cs:139-174; publicacion/pasos.md:26, :85; publicacion/privacidad.html

**Evidencia.** La 1.2.0 (5) salió de 16d8a38 y la reseña entró después, en a0b1b76. La guía de Google para esa librería dice que se recopilan datos que escribe el usuario (la valoración y el texto), y que en una pista cerrada se comparten en privado con el desarrollador. El formulario de Seguridad de los datos dice «no recopila», la política no nombra ninguna librería de Google y la ficha dice «doesn't collect your data».

**Arreglo.** Decidir la declaración con la guía de Google a la vista y sumar a la política una línea sobre la ventana de valoración de Google Play, junto con el arreglo de «privado» en los tres lugares.

**Nota.** Confianza media: no afirma una infracción, depende de cómo se lea la guía de Google. Es el único hallazgo de la tanda que podría terminar en un rechazo en revisión. Va junto con el texto de la política de «progreso.json está en el almacenamiento externo».

## H09 — Dónde vive progreso.json lo decide un desplegable (Preferred Data Location): pasarlo a Force Internal deja a todos sin progreso (media, riesgo)

- **Fuentes**: build#2, android#8
- **Archivos**: ShowBies1/ProjectSettings/ProjectSettings.asset:182; Library/.../assets/bin/Data/boot.config (android-preferred-data-location=1); Assets/Scripts/Progreso/Progreso.cs:826-885, :1099-1101

**Evidencia.** AndroidPreferredDataLocation: 1 es PreferExternal (enum de UnityEditor.CoreModule 6000.3.14f1: PreferExternal=1, ForceInternal=2), y por eso persistentDataPath es getExternalFilesDir. Poner Force Internal, el arreglo obvio para el «privado» de la política, hace que Progreso mire la carpeta interna, no encuentre nada y arranque de cero (los PlayerPrefs siguen). Ni la build ni la prueba de lógica vigilan ese ajuste. Además, con PreferExternal Unity podría caer al interno si el externo no está montado: los tres candidatos darían NoExiste (no NoSePudoLeer, que es lo que activa soloLectura), la sesión arrancaría en cero y se guardaría aparte, y al volver el externo se perdería esa sesión.

**Arreglo.** Un chequeo en Construir y en PruebasMejoras que exija PreferExternal hasta que haya migración. La mudanza, cuando se haga: Force Internal más una migración al arrancar que lea la carpeta vieja por JNI, con los cuidados del pendiente. Confirmar en la documentación de Unity 6 la caída al interno.

**Reproducción.** Player Settings > Preferred Data Location: Force Internal → AAB → actualizar encima de la 1.x: el juego abre sin monedas ni mejoras, como la primera vez.

**Nota.** Amplía «progreso.json está en el almacenamiento externo»: el arreglo obvio del «privado» borraría el progreso de todos, y nada lo vigila. Confianza alta en el enum y el boot.config; baja en la caída al interno con el externo sin montar (android#8, sin fuente primaria).

## H10 — Con el techo de zombis lleno, el jefe hace el aviso entero de la invocación y no sale nadie (o sale uno) (media, bug)

- **Fuentes**: jefe#2, oleadas#1
- **Archivos**: Scripts/Zombi/JefePatrones.cs:395-402, :407-428, :545-551; Zombi/EnemyController.cs:226-229; Zombi/WaveManager.cs:146-155; Prefabs/Personajes/ZombiBOSS.prefab (cantidadInvocados 4, maxInvocadosVivos 8)

**Evidencia.** Empezar elige invocar sin mirar si hay lugar y paga el aviso entero: anillo, rugido, temblor, pose de 0,8 s y zarpazos cortados. Invocar recorta n con LugarParaZombis (techo 35 en móvil, 60 en PC) y maxInvocadosVivos, y con 0 vuelve sin sacar a nadie; después Terminar invierte tocaCarga igual, así que la invocación se pierde. Mientras la oleada tiene zombis por sacar, WaveManager llena cada lugar libre en un cuadro (en móvil, 35 contra 51 en la oleada 10; en PC, 60 contra 91 en la 20), y como las muertes entran antes de Update y los generadores rellenan después, sale 0 o 1 en vez de 4 (6 en furia). Con n=1 el único invocado sale siempre al +X.

**Arreglo.** En Empezar, si min(cantidad+extra, maxInvocadosVivos − vivos, LugarParaZombis) da 0, hacer la carga sin invertir tocaCarga (o postergar el ataque); con lugar para menos, repartir los ángulos con un desfase al azar. Otra opción: dejar que la invocación pase el techo hasta maxInvocadosVivos, que ya está acotado (un jefe por vez en el libre y WaveManager espera a que bajen).

**Reproducción.** Target Android (techo 35), retomar la oleada 10 sin matar a los normales: el jefe hace el anillo, ruge y no aparece nadie. En el libre pasado el arranque (techo saturado) pasa casi siempre.

**Nota.** Confianza alta en el mecanismo y media en la frecuencia (sin medir): en el libre con el techo saturado es casi siempre; en oleadas, en móvil en la 10/20/30 mientras quedan zombis por salir y en PC desde la 20. Distinto de «Con el jefe encima del jugador, un invocado nace pegado a él».

## H11 — Al jefe se lo puede matar desde fuera de su ventana de ataque, sin que ataque nunca (media, exploit)

- **Fuentes**: jefe#3
- **Archivos**: Scripts/Zombi/JefePatrones.cs:455-478; Escenas/WaveMode.unity:554-555, :694-695; Prefabs/Bullet.prefab:112; Zombies/ZombiBOSS.asset

**Evidencia.** Con la cámara en (0, 12, −3,9), 70° de inclinación y 60° de campo vertical, la ventana del pivote va de ~4,7 m detrás a ~6 m delante, igual en 16:9 y 20:9. Las balas llegan a 22 m (11 m/s × 2 s), el jefe camina a 2 m/s y el jugador a 15. Teniéndolo 7-20 m arriba o abajo en la pantalla se le dispara sin que cargue ni invoque; entre 6 y 10,4 m delante incluso se lo ve.

**Arreglo.** A decidir jugando: medir la ventana con el cuerpo entero y no con el pivote, que ataque siempre dentro de ~8 m, o que camine más rápido fuera de cuadro. Se ajusta con margenEnPantalla.

**Reproducción.** Oleada 10: retroceder en vertical (arriba o abajo en la pantalla) con el jefe a más de 7 m y dispararle: no hay ninguna carga ni invocación hasta que muere.

**Nota.** Amplía «El jefe en el teléfono» (sección 4, que pedía ver jugando que no se vuelva fácil de evitar): con las balas a 22 m y el jefe a 2 m/s, se lo mata sin que ataque. Confianza media (geometría de cámara calculada, no jugada).

## H12 — El borde rojo del daño va dentro del área segura y se corta en recto del lado de la cámara del teléfono (media, bug)

- **Fuentes**: hud#5, sonido#2, android#1
- **Archivos**: ShowBies1/ProjectSettings/ProjectSettings.asset:71 (androidRenderOutsideSafeArea 1); Escenas/WaveMode.unity:1531-1537 (VinetaDanio hija de CanvasHelper, :441-445), ShowBies1.unity:1732-1738, Tutorial.unity:3423; Scripts/CanvasHelper.cs:16-29; UI/VinetaDanio.cs:63-82, :126

**Evidencia.** En las tres escenas, VinetaDanio cuelga de CanvasHelper, que ajusta su rectángulo a Screen.safeArea, y el juego dibuja fuera del área segura. En horizontal, con cámara perforada o muesca, el rojo termina en una línea recta a unos 80-140 px (~4 %) del borde de ese lado, con ~0,4 de opacidad en el corte en el medio del costado (0,7 en las esquinas), y del otro lado llega al borde. Pasa en cada golpe, en el golpe que mata y en los latidos de la furia. El comentario de la clase dice que va «sobre todo el HUD». Por el mismo origen, CartelOleada (centrado en el área segura) queda corrido unas 45 u respecto de los carteles del capítulo y de misión (centrados en el canvas entero).

**Arreglo.** Colgar VinetaDanio directo del Canvas (anclas 0-1, fuera de CanvasHelper) en las tres escenas, desde Unity y no tocando m_Father en el YAML; tiene raycastTarget en falso y nunca necesitó el área segura. De paso, centrar los carteles en un mismo marco.

**Reproducción.** Teléfono con cámara perforada o muesca, en horizontal: recibir un golpe y mirar el borde del lado de la cámara. Sin recorte, o en PC, no pasa.

**Nota.** Confianza media (depende de qué deja afuera Screen.safeArea en cada teléfono; tres frentes llegaron a lo mismo). Media: se ve en cada golpe en la mayoría de los teléfonos con recorte. Distinto de «El borde rojo del golpe que mata se corta enseguida» (otra causa), pero conviene hacerlos juntos: si la viñeta pasa a su propio canvas, que no lleve área segura.

## H13 — El botón de pausa del teléfono se monta sobre lo que va arriba al centro: el cartel del capítulo, el nombre del jefe y el panel del tutorial (media, bug)

- **Fuentes**: hud#1, hud#3, tutorial#3
- **Archivos**: Prefabs/UI/MenuPausa.prefab:513, :1995, :2019, :2104-2105, :2364 (desdeArriba 100); Scripts/Escenario/CapitulosDeEscenario.cs:80, :485-495; UI/BarraDelJefe.cs:27, :40-56; Escenas/Tutorial.unity:726-727 (PanelInstruccion); Editor/PruebasMejoras.cs:1740-1780

**Evidencia.** El botón de pausa (solo en el teléfono, disco con alfa 0,92) va de 24 a 154 u desde arriba (178 con su halo), x ±65, en el canvas de orden 10. (1) El cartel «CAPÍTULO N / NOMBRE» va centrado en +385 del canvas del HUD: las mayúsculas de «CAPÍTULO 2» caen a 95-158 u desde arriba en 16:9 y a 38-101 en 20:9, y «EL CEMENTERIO» a 123-160 en 20:9: el disco tapa el medio de las dos líneas. (2) BarraDelJefe tiene serializado desdeArriba 100 en el prefab (el código vale 170, con el comentario «el aire contra el botón de pausa»): las letras de «JEFE» van de 151 a 183 y el anillo de neón (25/9) baja hasta ~151 con su brillo encima; en el libre sale un jefe cada ~30 s. (3) En el tutorial, PanelInstruccion empieza a 145 u con su borde de neón y se cruza con el anillo en cualquier teléfono (se ve en Builds/temp_partida/6_tutorial.png). ProbarAvisosSinPisarse nunca compara con la franja de la pausa.

**Arreglo.** Capítulo: no alcanza con bajar AlturaDelCartel (en 21:9 no queda franja libre entre el cartel de la oleada y la pausa): ponerlo como primera línea del cartel de la oleada, o atenuar el botón de pausa mientras dura. Jefe: desdeArriba ~140 en el prefab (letras desde ~191). Tutorial: PanelInstruccion en y −260/−265 o de 150 de alto. Sumar la franja 24-178 u desde arriba (x ±90) a la prueba de avisos.

**Reproducción.** Target Android, WaveMode, completar la oleada 10: en el descanso de la 11 el disco queda sobre «CAPÍTULO 2». Modo libre: el primer jefe sale con «JEFE» pegado al disco. Tutorial: la pausa apoyada sobre la línea celeste del panel.

**Nota.** Misma causa en los tres: la franja del botón de pausa no se reservó ni la mira la prueba. Confianza alta (medidas del YAML y captura). Media por el cartel del capítulo, que sale en cada cambio de capítulo; el del jefe y el del tutorial solos serían baja.

## H14 — MODO LIBRE bloqueado: píldora gris del tema viejo con el halo verde de «jugar» y el texto verde oscuro (media, bug)

- **Fuentes**: menu#1, tema#2
- **Archivos**: Scripts/UI/BotonModoLibre.cs:50; Escenas/Menu.unity:4771, :6129-6130; Editor/PruebaModoLibre.cs:739

**Evidencia.** Pintar() solo cambia fondo.color a colorBloqueado (0,45; 0,45; 0,52), un gris del tema claro. La Sombra (el halo de neón), el texto y el icono siguen con el verde que les puso ConstructorNeon: nadie llama a ConstructorUI.PintarHalo. Resultado: el único botón que no hace nada brilla con el color de «te devuelve al juego», con el texto a 3,7:1. Se ve en Builds/libre_boton_5_bloqueado_en.png y libre_boton_6_bloqueado_es.png; el banco solo mira el color del fondo. Algo parecido, menor: el idioma elegido es amarillo con halo celeste.

**Arreglo.** Bloqueado: fondo Tema.ApagadoOscuro, texto e icono Tema.TextoSuaveClaro y ConstructorUI.PintarHalo(boton, colorBloqueado) (ColorDeHalo ya da el celeste tenue del vidrio para un gris). Libre: volver a colorLibre, a los colores guardados en Awake y al halo verde. Sumar el color de la Sombra al chequeo de PruebaModoLibre.

**Reproducción.** Con la mejor oleada por debajo de 11: PLAY, y en el panel de modos MODO LIBRE sale gris con resplandor verde.

**Nota.** Confianza alta (captura del propio banco). Cosmético, pero media porque lo ve todo jugador desde su segunda partida hasta la oleada 12.

## H15 — En PC el jugador corre un 41 % más rápido en diagonal (media, bug)

- **Fuentes**: jugador#1
- **Archivos**: Scripts/Jugador/PlayerController.cs:156-157; ProjectSettings/InputManager.asset (Horizontal y Vertical); Escenas/WaveMode.unity:554-555 (moveSpeed 15)

**Evidencia.** HandleMovement arma moveInput con dos GetAxis y lo multiplica por moveSpeed sin normalizar ni topar: con W+D mide √2. Da 15 m/s derecho, 21,2 m/s en diagonal y 27,6 con la furia, contra zombis a 5, 9 y 12: en PC kitear en diagonal es lo óptimo. El teléfono no lo sufre porque el Joystick Pack ya topa a 1 (Joystick.cs:83-84).

**Arreglo.** En HandleMovement, moveInput = Vector3.ClampMagnitude(moveInput, 1f) antes de calcular moveVelocity (ClampMagnitude conserva la rampa de GetAxis).

**Reproducción.** En PC, o en el editor con «Teclado y mouse en el editor», comparar con F1 (MedidorBalance) o a ojo lo que avanza con W sola y con W+D.

**Nota.** Confianza alta (verificado en PlayerController.cs:156-157). No reabre el kill-Z refutado: son 0,55 m por paso contra paredes de 1,17 m.

## H16 — Los zombis chicos patinan: sus piernas van de 3 a 6 veces más lento que lo que avanzan (media, bug)

- **Fuentes**: horda#1
- **Archivos**: Scripts/Zombi/EnemyController.cs:510; Zombi/JefePatrones.cs:74, :650; Prefabs/Personajes/ZombiFASTER.prefab:149; Zombi.prefab y ZombiRapido.prefab (sin velocidadDeAnimacion serializada)

**Evidencia.** El proyecto mide que Z_run_rm avanza 3 m/s a escala 1 y, con PasoParaCorrer = v / (3 · escala), pone al jefe en Paso 2,2 para que «los pies no patinen». Con la misma cuenta: el normal (5 m/s, modelo a 0,6) pide Paso 2,78 y tiene 1; el rápido (9 m/s, 0,48) pide 6,25 y tiene 1; el FASTER (12 m/s, 0,36) pide 11,1 y tiene 2,5. Los pies cubren entre el 16 y el 36 % de lo que avanzan, y CLAUDE.md dice que velocidadDeAnimacion ajusta el paso a lo que camina cada uno.

**Arreglo.** Calcular el Paso en Awake con JefePatrones.PasoParaCorrer(enemyType.velocidad, 3, |modelo.lossyScale.z|) cuando ritmoDeAndar es 1, con un tope de ~4-5 para el rápido y el FASTER, o subir velocidadDeAnimacion en los prefabs. Revisarlo con Grabar animaciones y medir también el avance de Z_walk_rm para el tanque.

**Reproducción.** Oleada 3 o más: mirar un rápido acercándose; avanza 9 m/s con las piernas a ~1,5 ciclos por segundo.

**Nota.** Confianza media: la cuenta usa el avance medido de Z_run_rm y los valores serializados, no una grabación. Media: son los zombis de todas las partidas.

## H17 — De noche las balas, las cajas y la granada quedan fuera de la luz de relleno y se ven oscuras (media, bug)

- **Fuentes**: combate#2
- **Archivos**: Escenas/WaveMode.unity:3083 (Relleno, solo la capa 8); Scripts/Escenario/Personajes.cs:18-22; Prefabs/Bullet.prefab (capa 7); Materiales/Bullet.mat (Standard, sin emisión); Prefabs/PUBalas, PUVida, PUArma y Granada

**Evidencia.** El relleno alumbra solo la capa 8, y PonerEnLaCapa se saltea los renderers con collider en el mismo objeto: la bala, las cajas y la granada quedan con la luna (0,5) y el ambiente bajo. En gamma, la bala amarilla queda en ≈(92, 99, 0), un oliva oscuro sobre el piso verde azulado; la caja de vida, roja oscura, y la de arma, gris oscuro. Las monedas se pasaron a la capa 8 justamente por esto; las balas nunca se miraron de noche (las grabaciones del 27/9 no disparan).

**Arreglo.** Bullet.mat con emisión amarilla o Unlit/Color, sin tocar la física. En las cajas, emisión, o el collider en un hijo y el renderer en la capa 8. Verificarlo con una foto con la calidad del teléfono, como la de los faroles.

**Reproducción.** Cualquier partida desde el 25/9: disparar y mirar el chorro de balas y las cajas lejos de los faroles.

**Nota.** Confianza media: cuenta de color, no una foto. Media: se ve en cada disparo de cada partida desde el 25/9.

## H18 — El jugador y los zombis entran en los edificios de la ciudad y desaparecen bajo el techo (media, bug)

- **Fuentes**: escenarios#1
- **Archivos**: Assets/Editor/ConstructorEscenarios.cs:377-384, :482-487, :906; Prefabs/Escenarios/Ciudad.prefab (0 colliders)

**Evidencia.** Hay 12 edificios de 11x11 m y 3,7 a 6,4 m de alto, con centros en (±12, ±36), (±36, ±12) y (±36, ±36), dentro de las paredes y sin collider: cubren ~15 % del área jugable. Con la cámara a 12 m de alto y 3,9 m detrás, el cubo cerrado tapa entero al jugador que está adentro, y las balas salen de la pared. Un zombi que sale de adentro de un edificio pega sin haberse visto. Tapado resolvió las cajas y las monedas, no al jugador.

**Arreglo.** Un BoxCollider por edificio en una capa nueva (la 9) que choque solo con Player (6): los zombis y las balas siguen pasando, y el raycast de las monedas lo toma como pared. Prenderlo en Aterrizar, no mientras el edificio sale del piso; si el jugador está en la huella al cambiar de capítulo, correrlo al borde (como Moneda.SacarDeLoTapado).

**Reproducción.** Oleada 21 o más: caminar hacia (36, 36) o (−12, 36) y entrar al edificio: el jugador desaparece.

**Nota.** Confianza alta. Distinto de «Ciudad: las cajas (hasta 30 s) y monedas...»: acá lo tapado son el jugador y los zombis.

## H19 — Si Unity 6 acota el pitch a 3, las escaleras y los arpegios repiten la nota de arriba (media, bug)

- **Fuentes**: sonido#1
- **Archivos**: Scripts/PowerUps/Moneda.cs:68; UI/ContadorCombo.cs:43, :156; UI/TextoMonedasPartida.cs:26; UI/VentanaLogros.cs:357; UI/VentanaMisiones.cs:264; Jugo/Sonidos.cs:163, :169, :210, :274; Editor/PruebasMejoras.cs:1543, :3194, :3202

**Evidencia.** La referencia de AudioSource.pitch de 6000.3 dice que con un AudioClip el pitch se acota a [−3..3]; la de 2022.3 no lo decía. El setter es nativo, así que desde afuera no se puede confirmar. Varias tablas llegan a 21, 23 y 24 semitonos (pitch 3,36, 3,78 y 4), que acotados a 3 suenan como el de 19. En la escalera de monedas las últimas notas de cada vuelta saldrían iguales (y el brillo de la octava caería en la nota equivocada); en el combo, desde x15 siempre la misma nota, y el arpegio de cada hito terminaría en dos notas iguales; en la derrota, los últimos tres ticks del conteo con 14 monedas o más; también la última nota del cofre. Sonidos además calcula la fuente libre y el ataque del limitador con el tono sin acotar. Las tres pruebas dan por bueno el 24 porque miran la tabla, no lo que suena.

**Arreglo.** Verificar primero (abajo). Si se confirma: una constante PitchMaximo = 3 y ninguna tabla por encima de 19 semitonos, o un segundo clip una octava arriba (moneda y combo) para las notas que pasen; que PitchDe o Sonar acoten el tono para que libreEn y el ataque usen el real. Sumar una prueba que falle si alguna tabla pasa de 19 y reescribir las tres de PruebasMejoras.

**Reproducción.** En el editor: var s = gameObject.AddComponent<AudioSource>(); s.pitch = 4f; Debug.Log(s.pitch). Si imprime 3, pasa todo lo demás. Jugando: agarrar 13 o más monedas seguidas, o llegar a un combo x10 y escuchar el final del arpegio.

**Nota.** Confianza media: depende de una línea de la documentación que no se pudo comprobar sin Unity. Si se confirma, lo oye casi todo jugador (la escalera de monedas y el combo).

## H20 — En la ciudad, las veredas tapan la línea de la carga y el anillo de la invocación del jefe (media, bug)

- **Fuentes**: jefe#1
- **Archivos**: Scripts/Zombi/JefePatrones.cs:633, :696; Assets/Editor/ConstructorEscenarios.cs:471; Prefabs/Personajes/ZombiBOSS.prefab:221

**Evidencia.** La línea y el anillo se dibujan a y 0,06 con Sprites-Default (transparente, con ZTest). Las 16 veredas de Ciudad.prefab son cubos de 14x0,14x14 con el techo en 0,14 (el cordón llega a 0,18) y cubren ~34 % del área: tapan la cinta de 2,88x17,9 m donde cruza una manzana. La mancha de sangre y los charcos se subieron a 0,2 por esto mismo; la línea del jefe quedó afuera.

**Arreglo.** Dibujar la línea y el anillo a ManchaDeSangre.AlturaSobreElPiso (0,2) y sumar el caso a la prueba de lógica que ya compara la mancha con la vereda.

**Reproducción.** Oleada 30 (ciudad): dejar que el jefe cargue con una manzana en el camino; la cinta desaparece sobre la vereda y reaparece en la calle.

**Nota.** Confianza alta. Solo en la ciudad (el jefe de la 30, y el del libre no, que es la pradera), pero ahí la cinta es el aviso de por dónde viene la carga.

## H21 — La caja de arma del tutorial nace detrás del panel de instrucciones (media, bug)

- **Fuentes**: tutorial#1
- **Archivos**: Scripts/Tutorial/TutorialManager.cs:174; Escenas/Tutorial.unity:702, :726-727 (PanelInstruccion), :853-854 (cámara)

**Evidencia.** El paso 5 hace nacer la caja 4 m al norte del jugador. Con la cámara del juego (0, 12, −3,9), 70° y 60° de campo vertical, la caja cae entre 278 y 318 px de 1080; el panel llega hasta 315 px en 16:9 y 352 px en 20:9, más 40 u de halo. La captura del propio banco (Builds/tutorial_caja_arma.png, con «caja de arma en el piso: sí») no la muestra: solo asoma una franja de ~5 px bajo la línea celeste. Hasta 0a4cc87 el panel era negro al 60 % sobre pasto de día; ahora es casi negro al 90 % sobre la noche. El panel tapa además la franja de 3,7 a 6,8 m delante del jugador, donde caen el anillo de la granada y el grupo cuando viene del norte.

**Arreglo.** Hacerla nacer como las del paso 4, con new Vector3(0f, 0f, 2f) (queda en 403-442 px, visible en 16:9 y 20:9), o a un costado. Sumar a PruebaTutorial un chequeo con WorldToScreenPoint contra el rect del PanelInstruccion en 16:9 y 20:9 (hoy solo mira activeSelf).

**Reproducción.** Tutorial cerca del centro del mapa: agarrar una caja del paso 4 y buscar la caja de arma; en 20:9 queda entera debajo del panel.

**Nota.** Confianza alta (cuenta con la cámara del juego y la captura del banco). Media: lo ve todo el que hace el tutorial, y el paso no avanza hasta agarrar la caja.

## H22 — Un banco en play cortado a mano queda armado y secuestra la próxima partida del editor, sin respaldo (media, bug)

- **Fuentes**: pruebas#1, herramientas#1, muerto#1
- **Archivos**: Assets/Editor/RespaldoDelBanco.cs:111-114, :155; PruebaDiaria.cs:823-855 (el único que limpia); PruebaDerrota.cs:147-156, :193-247; PruebaTienda.cs:189-196, :222-236; PruebaTutorial.cs:181-196; PruebaMenuYTienda.cs:172, :273-285, :436-450; PruebaModoLibre.cs:165, :206-217, :299-303; PruebaDisparo.cs:71, :99-127; PruebaMuerteAnimada.cs:59-110; PruebaGolpeAnimado.cs:79-103; GrabarJefe.cs:68-96; GrabarDisparo.cs:56, :73-93; GrabarAnimaciones.cs:58-73

**Evidencia.** Cada banco guarda en SessionState que está corriendo (Clave, .empezo, .desde, .listo) y solo lo apaga en Terminar; el único que lo limpia al volver a edición es PruebaDiaria, y su comentario anticipa justo esto. RespaldoDelBanco, en cambio, devuelve el progreso y borra su marca al salir a mano: queda la bandera prendida sin respaldo pendiente (lo mismo si EnterPlaymode falla por un error de compilación). En el Play siguiente el banco sigue con sus estáticos en cero: Tienda, Tutorial, ModoLibre y MenuYTienda cortan el Play en el primer cuadro y pisan su informe con un HAY FALLAS falso; PruebaDerrota deja al jugador al 10 %, le hace TakeDamage(999999), cuenta una partida y olvida la oleada en curso del progreso real del editor; MuerteAnimada mata zombis y suma puntos y monedas reales; GrabarJefe y GrabarAnimaciones le apagan el control al jugador. Disparo, MenuYTienda y ModoLibre además no se pueden volver a arrancar, sin aviso.

**Arreglo.** Un andamiaje común: que RespaldoDelBanco (o un BancoEnPlay) reciba la Clave del banco y en EnteredEditMode apague Clave, .empezo, .desde y .listo antes de borrar su marca (las 12 claves coinciden con el nombre que se le pasa a Guardar), como PruebaDiaria.AlCambiarDeModo, y avise del corte por consola. Un LogWarning en los Arrancar que hoy hacen return en silencio, y la guarda de play de PruebaTienda.cs:202 en los bancos que no la tienen.

**Reproducción.** Correr ShowBies > Pruebas > Derrota encima de la partida, Stop a los 2-5 s y después Play a mano en WaveMode: el jugador muere solo, el progreso del editor pierde la oleada en curso y prueba_derrota.txt queda pisado.

**Nota.** Solo editor. Media, igual que H23, porque destruye en silencio datos del desarrollador (el progreso real del editor y los informes de los bancos). Confianza alta; tres frentes llegaron solos a lo mismo.

## H23 — Los doce bancos y «Poner la noche» abren escenas con OpenScene(Single) sin mirar si hay cambios sin guardar (media, riesgo)

- **Fuentes**: escenarios#7, herramientas#2, muerto#2, pruebas#4
- **Archivos**: Assets/Editor/PruebaTienda.cs:216; PruebaTutorial.cs:175; PruebaMenuYTienda.cs:187; PruebaModoLibre.cs:183; PruebaDiaria.cs:186; PruebaDerrota.cs:141; PruebaDisparo.cs:79; PruebaGolpeAnimado.cs:73; PruebaMuerteAnimada.cs:53; Grabar*.cs; ConstructorEscenarios.cs:938-940, :1020 (contra ConstructorNeon.cs:46-51, :164-168)

**Evidencia.** EditorSceneManager.OpenScene en modo Single reemplaza las escenas abiertas y, según la documentación de Unity, no pregunta por los cambios sin guardar. ConstructorNeon ya se niega si la escena activa está sucia («la escena abierta tiene cambios sin guardar»), pero los doce bancos y PonerLaNoche no lo miran. PonerLaNoche además reabre de disco las tres escenas de juego, las guarda y termina en Menu.unity, sea cual sea la que estaba abierta. Si se corre un banco (desde el menú o por unity-mcp) con una escena a medio editar, esos cambios se pierden en silencio. RespaldoDelBanco cuida el progreso, no las escenas.

**Arreglo.** Sin ventanas (regla de la casa, nada de SaveCurrentModifiedScenesIfUserWantsTo): si alguna escena cargada está isDirty, LogError y no arrancar, como ConstructorNeon, antes de RespaldoDelBanco.Guardar en los bancos y al principio de PonerLaNoche. Al terminar PonerLaNoche, volver a lo que estaba abierto con GetSceneManagerSetup / RestoreSceneManagerSetup. Ponerlo en el andamiaje común de H22.

**Reproducción.** Mover un objeto en ShowBies1.unity sin guardar y correr ShowBies > Pruebas > Tutorial (play) o ShowBies > Escenarios > Poner la noche: al volver, el cambio no está.

**Nota.** Solo editor. Media, igual que H22, porque pierde en silencio trabajo del desarrollador. Confianza media (comportamiento documentado de OpenScene, no probado). Cuatro frentes llegaron solos a lo mismo.

## H24 — Dos etiquetas del HUD no se leen en uno de sus estados: FURIA mientras dura (1,3:1) y la G de la granada recargando (2,2:1) (baja, bug)

- **Fuentes**: jugador#2, tema#3
- **Archivos**: Scripts/UI/BotonFuria.cs:102-106, :130; Prefabs/UI/BotonFuria.prefab:580, :587; Editor/ConstructorNeon.cs:214, :293-296; Editor/PruebasMejoras.cs:1919; Escenas/WaveMode.unity (BotonGranada/Etiqueta)

**Evidencia.** Mientras dura, el fondo de la furia es ConstructorUI.Amarillo y la etiqueta (FURIA, o FURIA (F) en PC) sigue blanca, con el material liso de Bangers y sin contorno: 1,30:1 (3,48:1 sobre el rojo de lista), cuando el resto del neón lleva texto oscuro (13,9:1). 0a4cc87 pasó la G de la granada de blanca a NaranjaTexto, y la sombra de Recarga (negro al 55 %) va debajo: mientras recarga es oscuro sobre naranja oscurecido, 2,2:1 (antes ~7:1). ProbarPartidaNeon solo mira colorListo.

**Arreglo.** Teñir la etiqueta de la furia según el estado: AmarilloTexto mientras dura, un rojo oscuro sobre el rojo de lista y blanco sobre el casi negro (o darle el material de contorno del HUD); la G blanca mientras recarga (o siempre). Sumar los dos estados, con el color compuesto, a ProbarPartidaNeon.

**Reproducción.** Con la furia comprada, activarla y mirar el botón; en el teléfono, tirar una granada y mirar la G durante los 5 s de recarga.

## H25 — La cola de avisos de misión se sigue vaciando con el juego congelado: en la pausa se pierden y suenan todos juntos al volver; detrás de la derrota suenan y sacuden la cámara sin cartel (baja, bug)

- **Fuentes**: jugador#3, hud#4, tiempo#1
- **Archivos**: Scripts/UI/AvisoDeMisiones.cs:75-85, :173, :182-194, :215-216; UI/DerrotaEnLaPartida.cs:126-134; Jugo/Sonidos.cs:29-31; Camara/CamaraJugador.cs:141

**Evidencia.** Update corta solo la revisión con MenuPausa.JuegoCongelado (:173); la cola se sigue vaciando (:183) y cada cartel (2,6 s) se vence con unscaledTime. Bajo la pausa se gastan detrás del panel negro al 80 %; cada Mostrar toca el jingle por una fuente que AudioListener.pause retiene y suma 0,15 de trauma que la cámara no descarga en pausa: al tocar CONTINUAR arrancan N jingles en fase (tres ya pasan el techo del limitador) y la cámara tiembla sola. «Modo libre desbloqueado» se marca visto al encolarlo y no vuelve. Sobre la derrota, EsconderElHud apagó ese canvas: suena «¡misión cumplida!» y el mundo tiembla detrás de GAME OVER sin cartel (la muerte de un jefe encola varios a la vez). Con ¡HAS MUERTO! el cartel sale detrás de la ventanita.

**Arreglo.** Con MenuPausa.JuegoCongelado no sacar nada de la cola y congelar la edad del cartel (unscaledDeltaTime topeado, sumado solo con el juego andando); con DerrotaEnLaPartida.Activa, vaciar la cola en silencio, o mostrar esos avisos en la derrota.

**Reproducción.** Terminar una oleada que complete una misión y suba un nivel, pausar en el descanso unos 6 s y reanudar: no se vio ningún cartel, suenan dos jingles juntos y la cámara tiembla.

**Nota.** Distinto de «Sonido, lo que quedó afuera» (el jingle del aviso encima del cartel de la oleada).

## H26 — Los carteles del capítulo, de la guía de monedas y de la furia se vencen detrás de la pausa (baja, bug)

- **Fuentes**: tiempo#2
- **Archivos**: Scripts/Escenario/CapitulosDeEscenario.cs:179, :507-519; Tutorial/GuiaPrimeraPartida.cs:126, :171; UI/BotonFuria.cs:76-83

**Evidencia.** Los tres miden su edad con Time.unscaledTime. El del capítulo (3,2 s, WaveMode.unity:2885) sale en el descanso de las oleadas 11, 21 y 31, justo cuando se pausa: al volver ya no está, mientras el fundido (Time.deltaTime) sigue a mitad. «¡COGE LAS MONEDAS!» (5 s), la guía de quien recién instala, salta de edad al reanudar y se va en el primer cuadro. En una partida retomada en la 11 o más, el cuadro que instancia el decorado entero se come la entrada del cartel del capítulo (sin medir). CLAUDE.md afirma que la pausa lo congela todo sin tocar nada más.

**Arreglo.** Llevar la edad de esos carteles con un reloj propio (unscaledDeltaTime topeado) que no avance con MenuPausa.Pausado: la pausa de impacto no los congela y la del menú sí.

**Reproducción.** Llegar a la oleada 11 y pausar apenas sale «CAPÍTULO 2»: al reanudar, el cartel ya no está.

## H27 — En PC, el panel final del tutorial se toca con la mira y el clic dispara (baja, bug)

- **Fuentes**: hud#8, tutorial#6
- **Archivos**: Scripts/UI/CursorMira.cs:22-23; Tutorial/TutorialManager.cs:177-183; Jugador/PlayerController.cs:201

**Evidencia.** CursorMira vuelve a la flecha solo con MenuPausa.JuegoCongelado. El paso Fin prende panelFinal (JUGAR y MENÚ) sin congelar nada: los botones se apuntan con la mira, y como el arma lee el mouse sin mirar la UI, el clic tira una ráfaga antes de cargar la escena.

**Arreglo.** En Paso.Fin, apagar el arma (theGun.enabled = false) o el control del jugador, y volver a la flecha (por ejemplo, con un estado estático de CursorMira que el tutorial prenda).

**Reproducción.** En PC, terminar el tutorial y hacer clic en JUGAR.

## H28 — Los zombis del tutorial pueden nacer a la vista (baja, bug)

- **Fuentes**: tutorial#2
- **Archivos**: Scripts/Tutorial/TutorialManager.cs:142, :152; Escenas/Tutorial.unity:401 (distanciaSpawnZombi 14)

**Evidencia.** El zombi del paso 2 y el grupo del paso 3 nacen a 14 m en una dirección al azar, sin mirar si se ven. Con la cámara del juego ese punto cae dentro de la pantalla en 62 de 360 grados en 16:9 (17 %) y en 140 de 360 en 20:9 (39 %), hacia los costados; la niebla empieza a 16 m y no los disimula. En los generadores esto se arregló el 25/9 con SeVeriaAlAparecer.

**Arreglo.** Sortear la dirección hasta que EnemyController.SeVeriaAlAparecer dé falso (mirándolo después de acotar con PuntoDelMapa), o hacerlos nacer a 18-20 m.

**Reproducción.** Es una cuenta, no algo observado: en un teléfono 20:9, repetir el tutorial y mirar los bordes cuando aparece el primer zombi.

## H29 — El tutorial dice que el cargador grande queda «para siempre», y dura la partida (baja, bug)

- **Fuentes**: tutorial#4, idiomas#4
- **Archivos**: Assets/Idioma/Resources/Textos.txt:226 (tut_reloj); Scripts/Jugador/PlayerController.cs:29-30, :139-151

**Evidencia.** tut_reloj dice «The bigger magazine is yours to keep» / «se queda para siempre». PUArma sube maxBalas a 1000 solo en el jugador de esa escena, y la partida siguiente (también la que carga el tutorial al terminar) vuelve a 500. Quien recién aprende puede entender que es una mejora permanente.

**Arreglo.** «…lasts for the rest of the run» / «…se queda hasta el final de la partida».

**Reproducción.** Tutorial, caja de arma: el texto promete el cargador para siempre, y en la partida siguiente vuelve a 500/500.

## H30 — La derrota con la oferta del x2: el próximo objetivo no se entera del cobro y queda dentro del halo del botón del vídeo (baja, bug)

- **Fuentes**: tienda#2, hud#7, anuncios#7
- **Archivos**: Scripts/UI/ProximoObjetivo.cs:19, :31-40, :77-105, :117, :126; Anuncios/OfertaDeDuplicar.cs:97-109; Progreso/Progreso.cs:555-562; Tienda/BotonMejoras.cs:67-68; Escenas/Perdiste.unity:156-172 (OfertaVideo y −100, 820x64, Sombra +34)

**Evidencia.** Dos cosas en el mismo renglón, las dos solo con vídeo (hoy la APK de prueba; Play va en Nulo). (1) Elegir corre una sola vez, en Start. El x2 cobra por CobrarPremio y sube Revision: BotonMejoras vuelve a decir «¡Te alcanza para N mejoras!», pero el objetivo sigue con «TE FALTAN N MONEDAS PARA X» con X ya comprable, en la misma pantalla. (2) La píldora del x2 va de −132 a −68 y su halo de neón (naranja, alfa 0,5) baja hasta −166; las letras del objetivo van de −166 a −142 (el texto, de −175 a −135): el renglón queda dentro del resplandor.

**Arreglo.** (1) Guardar Progreso.Revision al elegir y, si cambia, volver a Elegir y reescribir el texto y la barra. (2) Mirarlo en una captura de la derrota con Falso; si molesta, bajar ProximoObjetivo ~20 u o achicar el halo del x2.

**Reproducción.** APK de prueba: morir con 60 monedas de la partida, 70 en el bolsillo y una mejora de 100. La derrota dice «faltan 30»; ver el vídeo del x2: el aviso dice que alcanza y el objetivo sigue con «faltan 30».

**Nota.** Un hallazgo con dos partes (lógica y posición), unidas porque hud#7 las vio juntas y pasan en la misma pantalla y el mismo caso.

## H31 — En PC, Espacio o Enter vuelven a comprar la última tarjeta tocada, y A/D mueven la selección (baja, bug)

- **Fuentes**: tienda#3
- **Archivos**: Prefabs/UI/TarjetaMejora.prefab:2451-2452 (m_Navigation m_Mode 3); ProjectSettings/InputManager.asset:265-271 (Submit); Escenas/Menu.unity:1969; Scripts/Tienda/TarjetaMejora.cs:169-176; comparar con BotonFuria.prefab:282-283 (m_Mode 0)

**Evidencia.** Selectable.OnPointerDown selecciona el botón si la navegación no es None, y el StandaloneInputModule manda Submit (Enter o Espacio) al seleccionado. Después de comprar con clic, cada Espacio o Enter llama otra vez a IntentarComprar, y el eje Horizontal (A/D, flechas) pasa la selección a la tarjeta de al lado. Espacio es la granada en la partida. El botón de la furia ya tiene la navegación en None por lo mismo.

**Arreglo.** navigation.mode = None en el botón de la tarjeta, puesto por ConstructorTienda (y, si se quiere, en ¡A JUGAR! y VOLVER).

**Reproducción.** Windows, tienda con monedas para dos niveles de daño: clic en comprar daño y después Espacio: compra otro nivel. D y Espacio: compra cadencia.

## H32 — La granada y la furia se muestran como niveles, y la furia no dice qué hace (baja, bug)

- **Fuentes**: tienda#4
- **Archivos**: Scripts/Tienda/TarjetaMejora.cs:200-203; Tienda/TiendaMejoras.cs:638, :651-668; Mejoras/Furia.asset y Granada.asset; Idioma/Resources/Textos.txt:151, :153, :168-171; comparar con UI/ProximoObjetivo.cs:97-101

**Evidencia.** Con tope 1, la tarjeta dice «NIVEL 0/1», «0 → 1 granada cada 5 s» y «0 → 6 segundos de furia», y al comprarlas festeja «¡MÁXIMO!» y queda en MÁX. La derrota ya se corrigió para esto (objetivo_desbloqueo); la tienda no. La furia, que cuesta 5.000, no dice en ningún lado que da daño y cadencia x2 y velocidad x1,3.

**Arreglo.** Para nivelMaximo == 1: sin NIVEL 0/1, el valor como bloqueada → desbloqueada, «¡DESBLOQUEADA!» al comprar, y una unidad de la furia que la explique.

## H33 — Precio y monedas se truncan al mismo número compacto (157K, 1,2 M) y la tarjeta queda gris sin que se entienda (baja, bug)

- **Fuentes**: tienda#5, idiomas#2
- **Archivos**: Scripts/UI/FormatoNumeros.cs:26-38; Tienda/TarjetaMejora.cs:241-242, :262; UI/ContadorMonedas.cs:152

**Evidencia.** Compacto trunca de 100K a 999K («157K» para 157.890) y desde el millón trunca a un decimal, mientras el estado de la tarjeta usa los valores reales. Con 157.200 monedas el contador y el precio dicen los dos 157K y la tarjeta está gris («faltan 690»); con 1.250.000 y un precio de 1.290.000, los dos dicen «1,2 M». Solo el daño y la vida pasan de 100K (desde el nivel ~22, en el muro; el daño cuesta ≈2,8 M en el nivel 30).

**Arreglo.** Tres cifras significativas en Compacto desde el millón (1,29 M / 12,9 M / 129 M) y un decimal en el precio de la tarjeta hasta el millón, o al menos el precio redondeado hacia arriba y el saldo hacia abajo. Casos nuevos en ProbarFormatoNumeros.

**Reproducción.** Con 1.250.000 monedas y un precio de 1.290.000: el contador dice 1,2 M, el precio dice 1,2 M y la tarjeta está gris.

## H34 — «Mejor oleada: N» en la tienda es la completada, una menos que la alcanzada (baja, bug)

- **Fuentes**: tienda#6, idiomas#10
- **Archivos**: Scripts/Tienda/TiendaMejoras.cs:409-412; Idioma/Resources/Textos.txt:23, :147

**Evidencia.** El pie muestra Progreso.MejorOleada, que es la última oleada completada: quien murió en la 12 lee «Mejor oleada: 11», mientras el libre dice «LLEGA A LA OLEADA 12» y el HUD mostró «Oleada 12».

**Arreglo.** Mostrar MejorOleada + 1 como «Oleada más alta», o cambiar el texto a «Best wave cleared: {0}» / «Mejor oleada superada: {0}».

**Reproducción.** Morir en la oleada 12 por primera vez y abrir MEJORAS: «Mejor oleada: 11».

## H35 — SEGUIR JUGANDO (¿SALIR?) no es una píldora: copia de VOLVER agrandada sin volver a redondear (baja, bug)

- **Fuentes**: menu#2, tema#5
- **Archivos**: Scripts/UI/ConfirmarSalir.cs:72, :90; Escenas/Menu.unity:944, :1663

**Evidencia.** Copia el VOLVER del idioma (300x80, redondeado para ese alto: Fondo con multiplicador 3,175 y Sombra con 0,797) y lo pasa a 440x100 sin recalcular: las puntas quedan de radio 40 en 50 de medio alto (un rectángulo redondeado; harían falta 2,54) y el halo sigue escalado para 148 de alto en vez de 168 (0,702). Es la trampa del Sliced que arregló 7276891; ProbarPildorasRedondas no lo ve porque se arma en Start.

**Arreglo.** Después del sizeDelta, ConstructorUI.RedondearPildora sobre Visual/Fondo, y HaloDeBoton (o RedondearPildora) sobre la Sombra.

**Reproducción.** Menú → SALIR (o el atrás de Android): mirar las puntas y el halo de SEGUIR JUGANDO.

## H36 — Tocar el COFRE y cerrar las misiones enseguida no lo abre: se abre solo al volver a entrar (baja, bug)

- **Fuentes**: menu#3
- **Archivos**: Scripts/UI/VentanaMisiones.cs:207, :247, :253

**Evidencia.** TocarCofre pone abriendo = 0 y el cobro espera 0,6 s dentro de Update, pero después del «if (!Abierta) return». Cerrar no toca abriendo: si se cierra antes, el cofre no se cobra y se abre por sorpresa (sin el temblor) al reabrir la ventana. No se pierden monedas.

**Arreglo.** Atender abriendo antes del return (cobrarlo aunque se cierre la ventana), o ponerlo en −1 en Cerrar.

**Reproducción.** Con las tres misiones cobradas: tocar COFRE y, antes de 0,6 s, VOLVER o atrás. La insignia sigue contando el cofre, y al reabrir misiones se abre solo.

## H37 — Al volver por MEJORAS, la barra del nivel se llena escondida detrás de la tienda y suenan notas sin nada en pantalla (baja, bug)

- **Fuentes**: menu#4
- **Archivos**: Scripts/UI/VentanaLogros.cs:302, :381; Tienda/TiendaMejoras.cs:275

**Evidencia.** TiendaMejoras.Abrir apaga extrasMenu (AreaSeguraMenu), donde está la píldora del nivel, pero VentanaLogros.Update sigue llenando la barra y toca una nota por nivel cruzado. experienciaMostrada es estática, así que al cerrar la tienda la barra ya está llena y el llenado no se ve nunca. Es el circuito que enseña la guía (MEJORAS, comprar, ¡A JUGAR!), y se sube ~1 nivel por partida.

**Arreglo.** No avanzar experienciaMostrada mientras la tienda esté abierta (y quizá mientras VentanaRecompensaDiaria.Ocupada): la barra se llena al cerrarla.

**Reproducción.** Jugar una partida que dé un nivel, morir y tocar MEJORAS en la derrota: suena la nota del nivel sobre la tienda y al cerrarla la barra ya está llena.

## H38 — El aviso de misión pisa los botones de furia y granada en 16:9, 16:10 y 4:3 (baja, bug)

- **Fuentes**: hud#6
- **Archivos**: Scripts/UI/AvisoDeMisiones.cs:37-39, :197-212; Editor/PruebasMejoras.cs:1767-1779

**Evidencia.** Los detalles de logro miden 700-810 a 39,6 pt y «¡MODO LIBRE DESBLOQUEADO!» mide 703 a 72 pt. En 16:9 el título llega a x 1312 y cruza el anillo de la furia (1256..1400, y 326..470), y un detalle de 710 cruza el anillo de la granada; en 4:3 el título entero cae sobre la furia. Se dibuja encima porque el aviso es el último hijo del canvas. La prueba solo mira 16:9 y 20:9 contra los carteles y la vida.

**Arreglo.** Limitar el ancho del aviso a lo libre a la izquierda de los botones (o autoajustar el tamaño) y sumar los botones a la prueba, en 16:10 y 4:3.

**Reproducción.** Tablet 16:10 o 4:3 con la furia comprada: ganar un logro con un detalle largo.

## H39 — Lo que se arma en código quedó sin neón: el cartel del capítulo, la guía de la primera partida, la barra del jefe (con puntas de elipse), los volúmenes y el VOLVER de la diaria (baja, calidad)

- **Fuentes**: tema#6, tutorial#5, jefe#7, herramientas#4
- **Archivos**: Scripts/Escenario/CapitulosDeEscenario.cs:492-496; Tutorial/GuiaPrimeraPartida.cs:114-117, :167; UI/BarraDelJefe.cs:95-100, :130; UI/ConstructorUI.cs:152-157; UI/SliderVolumen.cs:15; UI/VentanaRecompensaDiaria.cs:369; Editor/ConstructorNeon.cs:319-325; Escenas/WaveMode.unity:2883, :5271-5276

**Evidencia.** VestirEscenaDeJuego solo cambia AvisoDeMisiones.materialContorno. CapitulosDeEscenario y GuiaPrimeraPartida siguen con Bangers SDF - Outline (f629c6e4) mientras el HUD pasó a Neon HUD (6962ae4a): el cartel «CAPÍTULO N» sale con el dorado viejo al lado del de la oleada, que lleva halo, y la guía, lo primero que ve un jugador nuevo, con blanco y un (1; 0,85; 0,3) que no es el amarillo neón de las monedas. La barra del jefe tiene Outline, el rojo viejo y Redondear(…, 8): los bordes de la Pildora quedan en 15,9 px, y en 26 de alto Unity achica solo el eje vertical a 13, puntas de 15,9x13 (la trampa del Sliced que resuelve RedondearPildora; la prueba no la ve porque se arma en código). Los volúmenes tienen el relleno dorado viejo y barra cuadrada; el VOLVER de la diaria usa el vidrio claro escrito a mano (solo se ve con vídeo). CLAUDE.md dice que el HUD es de neón.

**Arreglo.** Que VestirPartida cablee materialContorno (Neon HUD o Neon) y los colores en CapitulosDeEscenario, GuiaPrimeraPartida y BarraDelJefe, con los colores de ConstructorUI; RedondearPildora en el marco, el relleno y el golpe de BarraDelJefe; ColorRelleno = ConstructorUI.Amarillo en los volúmenes; Tema.Elegir(…, RolDeTema.Vidrio) en la diaria. Sumar materialContorno a ProbarPartidaNeon.

## H40 — La recompensa diaria no sale al volver a la app en un día nuevo (baja, bug)

- **Fuentes**: android#2
- **Archivos**: Scripts/UI/VentanaRecompensaDiaria.cs:97-131, :242-247 (solo VigiaAplicacion.cs:73-82, MenuPausa.cs:54-62 y OfertaDeRevivir.cs:309-319 miran pausa o foco)

**Evidencia.** Todo se decide en Start (racha, diaDeLaVentana, pendiente) y Update solo abre si pendiente. La actividad es singleTask: tocar el ícono con el proceso vivo trae el menú de ayer sin recargarlo, y la diaria recién sale al volver de una partida.

**Arreglo.** Con OnApplicationPause(false) o un chequeo cada pocos segundos: si no está abierta y Progreso.DiaDeHoy() != diaDeLaVentana, volver a correr la lógica de Start (sin rehacer las texturas), respetando PrimeraVez y la tienda abierta.

**Reproducción.** Dejar la app en el menú en segundo plano sin matarla, pasar la medianoche y volver: no aparece la ventana.

## H41 — Al perder el foco, la granada que se estaba apuntando se tira sola (baja, bug)

- **Fuentes**: android#6
- **Archivos**: com.unity.ugui StandaloneInputModule.cs:166-244 y EventSystem.cs:464-469 (PackageCache); Scripts/UI/JoystickGranada.cs:71-80; Jugador/PlayerController.cs:291-293; UI/MenuPausa.cs:54-57

**Evidencia.** El uGUI de Unity 6 manda pointerUp a los punteros que arrastraban cuando la app pierde el foco. JoystickGranada tira en OnPointerUp, y GranadaLista solo lo frena si MenuPausa ya pausó, pero el orden de OnApplicationFocus entre objetos no está definido. Además, un joystick apretado sin llegar a arrastrar no se suelta y sigue con su entrada al volver.

**Arreglo.** En JoystickGranada.OnPointerUp, no tirar si !Application.isFocused o con JuegoCongelado; en MenuPausa.Pausar, reiniciar los dos FixedJoystick con OnPointerUp(null) y el estado apretado de la granada.

**Reproducción.** Apuntar la granada con el botón G y bajar la cortina de notificaciones.

**Nota.** Confianza baja (depende del orden de OnApplicationFocus).

## H42 — La derrota no entra en un monitor 32:9 (Windows) (baja, bug)

- **Fuentes**: android#7
- **Archivos**: Escenas/Perdiste.unity:1168-1173 (match 0; botones en y −290 con 140 de alto, título en 265, aviso de descanso en 360); ProjectSettings/ProjectSettings.asset:111

**Evidencia.** Con match 0 el canvas mide 1920 × alto/ancho de alto: en 32:9 son 540 u (±270). Los botones (de −360 a −220) quedan dos tercios afuera, y el título y el aviso, cortados. Se sale con Escape. En Android no pasa (21:9 deja ±411).

**Arreglo.** Match 0,5 en Perdiste, como la tienda y la pausa (revisando 16:9 y 21:9), o topear la relación de aspecto en Windows, si la build de Windows se va a distribuir.

## H43 — La embestida pega a quien la toca al arrancar, aunque esté fuera de la cinta roja (baja, bug)

- **Fuentes**: jefe#4
- **Archivos**: Scripts/Zombi/EnemyController.cs:1037-1042, :1062-1066; Zombi/JefePatrones.cs:357-384

**Evidencia.** EmpezarEmbestida pone proximoGolpe = 0 y, con golpeaAlChocar, Golpear pega ×2,5 con cualquier contacto, sin mirar la dirección. Si el jugador está en contacto al costado o detrás cuando termina el aviso, el primer paso de física reporta el contacto: ~46 de daño en la 10, y la carga se corta a los 0,32 m. Contradice «no pisar lo rojo es no comerse el golpe».

**Arreglo.** En Golpear con golpeaAlChocar, contar el choque solo si el jugador está delante en la dirección de la carga y dentro del ancho del cuerpo; si no, ignorarlo sin gastar el intervalo.

**Reproducción.** Quedar en contacto con el jefe al costado o detrás mientras avisa la carga: al terminar el aviso entra el golpe ×2,5 y el jefe queda aturdido en el lugar.

## H44 — Con la derrota, el jefe camina a festejar congelado en la pose de su patrón (baja, bug)

- **Fuentes**: jefe#5
- **Archivos**: Scripts/Zombi/JefePatrones.cs:231, :331-335, :442-453; Zombi/EnemyController.cs:1250

**Evidencia.** LateUpdate sale si DerrotaEnLaPartida.Activa, antes de suavizar hacia cero, y VolverAPerseguir no resetea la inclinación, la altura, el balanceo ni el transform del modelo; EnemyController escribe el modelo recién al festejar. Si la carga mata al jugador, el jefe va hasta su lugar echado 24° hacia adelante (o agazapado, arqueado o torcido), al lado del cuerpo.

**Arreglo.** En VolverAPerseguir, o en LateUpdate con la derrota activa, seguir suavizando la pose a cero mientras no esté festejando.

**Reproducción.** Oleada 10 con proveedor Nulo: morir por la carga. Detrás de la derrota, el jefe va a su lugar inclinado y se endereza recién al festejar.

## H45 — El jefe también cae bajo el techo de cadáveres y en el teléfono puede desaparecer de golpe (baja, bug)

- **Fuentes**: jefe#6
- **Archivos**: Scripts/Zombi/EnemyController.cs:118-119, :599-606

**Evidencia.** Morir llama a Devolver sin animación si cadaveres ≥ techo (5 en móvil), y el jefe no tiene excepción. Con sus invocados (12,8 de vida en la 10) cayendo con las mismas balas, o con una granada, es fácil tener 5 cadáveres cuando muere: el clímax de la oleada es un jefe de 4 m que se esfuma en un cuadro.

**Arreglo.** En Morir, dejar pasar al jefe aunque el techo esté lleno, mirando EsJefe antes de DejarDeContar (que lo pone en falso).

**Reproducción.** Móvil, oleada 10: matar al jefe justo después de una granada sobre sus invocados; desaparece sin desplomarse.

## H46 — El zombi normal flota 10 cm sobre el piso (el mismo error que se arregló en los otros tres) (baja, bug)

- **Fuentes**: horda#2
- **Archivos**: Prefabs/Personajes/Zombi.prefab:258 (escala 0,5), :433 (y −0,794); ZombiRapido.prefab:432 (y −0,92); commit db09d6f

**Evidencia.** La cápsula apoya su fondo 0,5 m bajo el pivote, y el origen del modelo (los pies) queda a 0,794 × 0,5 = 0,397 m: flota 0,103 m. db09d6f bajó a −1 al tanque, al FASTER y al jefe porque «a −0,79 el jefe flotaba 0,4 m» (0,206 × 2), lo que confirma que los pies están en el origen. El normal quedó en −0,794 y el rápido flota ~3 cm. Afecta al zombi más común, a los invocados del jefe y al cadáver acostado.

**Arreglo.** m_LocalPosition.y del modelo en −1 en Zombi.prefab (y en ZombiRapido.prefab si su origen también está en los pies), y corregir la frase de CLAUDE.md. La barra de vida mide los renderers y se ajusta sola.

**Reproducción.** Oleada 1, un zombi normal de cerca: los pies separados de la sombra; se nota más en el cadáver acostado.

## H47 — Los invocados del jefe caminan con las piernas sincronizadas (baja, calidad)

- **Fuentes**: horda#5
- **Archivos**: Scripts/Zombi/EnemyController.cs:513; Zombi/JefePatrones.cs:558-576

**Evidencia.** Cada aparición arranca el ciclo de andar en el cuadro 0 (Play(idAndar, 0, 0f)). La invocación saca 4 (6 en furia) en el mismo cuadro y con el mismo Paso, así que marchan al unísono. El festejo evita esto a propósito con un desfase por zombi.

**Arreglo.** animador.Play(idAndar, 0, Random.value).

**Reproducción.** Oleada 10: mirar una invocación; los cuatro dan los pasos a la vez.

## H48 — El jefe muestra dos barras de vida: la flotante chica y la de arriba (baja, calidad)

- **Fuentes**: horda#6
- **Archivos**: Scripts/Zombi/EnemyController.cs:845-848, :976-980; Zombi/BarraDeVida.cs:31-60

**Evidencia.** MostrarBarraDeVida corre para cualquier zombi y BarraDeVida no mira EsJefe. La flotante del jefe es la de 1,2 m de los chicos, sobre una cabeza a ~4,5 m; su altura se mide una sola vez con el primer golpe (puede tocar con el jefe agazapado o arqueado) y se reusa en las apariciones siguientes. BarraDelJefe ya muestra la vida arriba.

**Arreglo.** Si no es a propósito, no crear la barra flotante cuando EsJefe.

**Reproducción.** Oleada 10: pegarle al jefe; salen la barra grande arriba y una chica sobre su cabeza.

## H49 — Una excepción en el bloque de muerte deja un zombi inmortal que traba la oleada (baja, riesgo)

- **Fuentes**: horda#3
- **Archivos**: Scripts/Zombi/EnemyController.cs:829, :850-873 (estaMuerto en :852, Morir en :872), :1025

**Evidencia.** estaMuerto se pone primero y Morir va último, con ocho llamadas a otros sistemas en el medio (mancha, partículas, Puntaje, NivelJugador, Moneda.Soltar, Progreso.ContarMuerte, Efectos). Si alguna tira, el zombi queda con estaMuerto y enUso: no se lo puede dañar ni pega, pero sigue en ZombisVivos y en SigueVivo, y la oleada no termina nunca (salida: pausa → menú → retomar). Hoy no hay un disparador concreto.

**Arreglo.** Envolver los efectos en try/finally con Morir(empuje) en el finally (o try/catch con Debug.LogException): salir de la cuenta y volver al pool pasa siempre.

**Reproducción.** Hoy no hay disparador. Para verlo, forzar una excepción en Moneda.Soltar: el zombi sigue caminando, no muere y la oleada queda en N−1/N.

## H50 — Verificar que el Paso y el Ritmo no se pierdan cuando un tanque o un FASTER vuelve del pool (baja, riesgo)

- **Fuentes**: horda#4
- **Archivos**: Scripts/Zombi/EnemyController.cs:507-514; Animaciones/Zombi.controller:149-160 (por defecto 1 y 1); ToonyTinyPeople/TT_demo_zombie.prefab:115 (KeepAnimatorControllerStateOnDisable 0)

**Evidencia.** Al apagarse, el Animator vuelve a los valores por defecto (Paso 1, Ritmo 1). El zombi reusado los fija en el OnEnable de la raíz mientras el Animator se prende en un hijo durante la misma activación; si se reiniciara después, el tanque reusado correría y el FASTER iría a Paso 1. Ningún banco lo mira (corren en las oleadas 2-6, sin tanques ni FASTER). Lo más probable es que esté bien.

**Arreglo.** Verificarlo en play: un tanque que vuelve del pool tiene que dar animator.GetFloat(«Ritmo») = 0. Si falla, fijar los parámetros en el primer FixedUpdate de la aparición.

**Reproducción.** Oleada 7 o más en el editor: matar un tanque, esperar otro del pool y leer GetFloat de Ritmo y Paso.

**Nota.** Confianza baja: es una verificación, no un error visto.

## H51 — Decorados: autos sobre el vacío, faroles que atraviesan autos, tumbas encimadas y una cerca que el jugador cruza (baja, bug)

- **Fuentes**: escenarios#3
- **Archivos**: Assets/Editor/ConstructorEscenarios.cs:172, :256-267, :279, :449-458; Escenas/WaveMode.unity:3009

**Evidencia.** 6 de los 22 autos quedan en ±51,2 (la calle ±48 más 3,2) y el piso llega solo a ±50: enteros sobre el vacío, a 2-3 m del jugador contra la pared. Los faroles de (−3,8, 20,2) y (27,8, 44,2) atraviesan un auto, y el primero de esos autos está en medio del cruce, a 5 cm de otro. En el cementerio hay tres pares de tumbas encimadas, uno a 7 m del centro. La cerca y la reja van a 48 m y las paredes a 49,2-49,7: el jugador (radio 0,25) la cruza ~1 m.

**Arreglo.** Ciudad: no poner autos del lado de afuera de las calles ±48 y descartar los que queden a menos de ~1,5 m de un farol o de otro auto. Cementerio: LejosDeTodos también para las tumbas sueltas. Cerca y reja a ~49,3. Volver a correr los constructores y la prueba de lógica.

**Reproducción.** Oleada 21 o más: ir a (−48, −32) o (13, −48) y se ve el auto sobre el vacío. En la pradera, caminar contra la pared: el jugador queda del lado de afuera de la cerca de neón.

## H52 — En la ciudad el jugador y los zombis se hunden 14 cm en las veredas (baja, bug)

- **Fuentes**: escenarios#6
- **Archivos**: Assets/Editor/ConstructorEscenarios.cs:467-479

**Evidencia.** Cada manzana es una losa de 0,14 m con un cordón de 0,18 m, sin collider: 16 manzanas de 14x14 m, un tercio del mapa. Los personajes caminan en y 0, así que sobre la vereda se les esconden los pies y los tobillos. Las manchas y los charcos ya se subieron a 0,2 m por la misma causa.

**Arreglo.** Bajar la vereda a ~0,04 m y el cordón a ~0,06 (los charcos pueden volver a bajar; ver también H20). Colliders no: los zombis se trabarían en el cordón.

## H53 — Las cajas nacen en filas de Z por la sobrecarga int de Random.Range (baja, calidad)

- **Fuentes**: combate#4
- **Archivos**: Scripts/PowerUps/PowerUp.cs:67

**Evidencia.** Random.Range(-45, 45) tiene los dos argumentos int: devuelve un entero de −45 a 44, así que la Z nunca cae entre 44 y 45 y todas las cajas quedan en filas a un metro (verificado en el código). La X sí es float. Random.Range(0.5f, 0.5f) no sortea nada.

**Arreglo.** Random.Range(-45f, 45f), y 0.5f directo en la Y.

## H54 — El cartel de la oleada pasa a mayúsculas con la cultura del teléfono: en turco o azerí sale COİNS con otra fuente (baja, bug)

- **Fuentes**: idiomas#1
- **Archivos**: Escenas/WaveMode.unity:2750 (CartelOleada, m_fontStyle 17); com.unity.ugui 2.0.0 Runtime/TMP/TextMeshProUGUI.cs:1905 y TMP_Text.cs:4109 (char.ToUpper); Idioma/Resources/Textos.txt:187; Scripts/Zombi/WaveManager.cs:217-220

**Evidencia.** CartelOleada es el único TMP de juego con el bit UpperCase, y TMP convierte con char.ToUpper, que usa CurrentCulture: en tr y az, «i» pasa a «İ» (U+0130). La cmap de Bangers.ttf no tiene U+0130, así que cae en LiberationSans o sale como glifo faltante. El coins de cartel_bono en inglés tiene una i.

**Arreglo.** Escribir cartel_oleada y cartel_bono ya en mayúsculas en la tabla y pasar m_fontStyle de 17 a 1 (desde ConstructorNeon.VestirPartida). Sumar a la prueba de lógica un chequeo que rechace el bit 16 en escenas y prefabs (los 7 botones del menú que lo llevan ya tienen sus textos en mayúsculas).

**Reproducción.** Teléfono en turco, juego en inglés, oleadas: desde el cartel de la oleada 2 se lee «+4 COİNS FOR WAVE 1», con la İ en otra fuente y sin halo.

## H55 — «Level» / «Nivel» nombra dos cosas distintas en la misma pantalla del modo libre (baja, calidad)

- **Fuentes**: idiomas#3
- **Archivos**: Idioma/Resources/Textos.txt:91 (aviso_nivel), :178 (hud_nivel); Scripts/Zombi/GeneradorZombis.cs:111; UI/AvisoDeMisiones.cs:115

**Evidencia.** En el HUD del libre, «Nivel 4» es la dificultad (sube cada 45 s con su jingle); en la misma partida sale «¡NIVEL 13!» del nivel del jugador, con «+N monedas te esperan en el menú». Además, la tienda dice «NIVEL 3/16» y el logro dice «nivel … del modo libre».

**Arreglo.** Solo tabla: aviso_nivel → «PLAYER LEVEL {0}!» / «¡NIVEL DE JUGADOR {0}!» (entra en los 1200 del aviso), o «¡SUBES AL NIVEL {0}!».

**Reproducción.** Jugar el libre hasta que suba el nivel del jugador: sale «¡NIVEL 13!» con el HUD en «Nivel 4».

## H56 — «faltan 1» en la tarjeta de la tienda (baja, bug)

- **Fuentes**: idiomas#7
- **Archivos**: Scripts/Tienda/TarjetaMejora.cs:262; Idioma/Resources/Textos.txt:155 (tarjeta_faltan)

**Evidencia.** Usa «faltan {0}» con Max(1, costo − monedas), sin singular. Es el mismo error que ya se arregló con derrota_monedas_una y objetivo_*_una.

**Arreglo.** Una fila tarjeta_falta_una («need {0}» / «falta {0}») elegida cuando la resta da 1.

**Reproducción.** Tener justo una moneda menos que el precio: la tarjeta dice «faltan 1».

## H57 — El porcentaje se escribe 30% en la tienda y 30 % en los logros (baja, calidad)

- **Fuentes**: idiomas#8
- **Archivos**: Scripts/Progreso/Mejora.cs:107 (FormatoValor.Porcentaje); Idioma/Resources/Textos.txt:105 (logro_critico)

**Evidencia.** La tarjeta de críticos agrega «%» pegado en los dos idiomas; en español el logro escribe «{0} %» con espacio, que es la norma de la RAE. El mismo número se ve de dos formas según la pantalla.

**Arreglo.** Que el sufijo salga de FormatoNumeros según el idioma: «%» en inglés y « %» con espacio duro U+00A0 (Bangers lo tiene) en español.

## H58 — El cartel de neón ZOMBIS de la ciudad está en español en un juego que arranca en inglés (baja, calidad)

- **Fuentes**: idiomas#5
- **Archivos**: Assets/Editor/ConstructorEscenarios.cs:364; Prefabs/Escenarios/Ciudad.prefab

**Evidencia.** Es un TMP 3D horneado en el prefab, sin traducción a propósito. Los otros once carteles (BAR, 24H, MOTEL…) valen en los dos idiomas; ZOMBIS a un angloparlante le parece una falta de ortografía.

**Arreglo.** Cambiarlo por una palabra que sirva en los dos idiomas (BRAINS, RIP, GRILL…) y volver a correr Armar ciudad.

## H59 — Redacción: tres filas en inglés poco naturales y una en español desparejada (baja, calidad)

- **Fuentes**: idiomas#9
- **Archivos**: Idioma/Resources/Textos.txt:105 (logro_critico), :107 (logro_millonario), :148 (tienda_pie_sin_oleadas), :225 (tut_caja_arma, es)

**Evidencia.** «Play the waves to earn coins!», «Earn {0} coins playing» y «Get {0}% critical chance» no suenan naturales en inglés. En español, «mejora tu arma: cargador más grande y dispara mucho más rápido» mezcla un sustantivo con un verbo y repite «arma».

**Arreglo.** «Play Waves mode to earn coins!», «Earn {0} coins by playing», «Reach {0}% critical chance» y «te da un cargador más grande y mucha más cadencia» (medidas: entran en sus cajas).

## H60 — IconoDeBoton mide el texto con su propio margen: el icono queda ~4 veces más lejos y el grupo corrido (baja, bug)

- **Fuentes**: tema#4
- **Archivos**: Scripts/UI/IconoDeBoton.cs:37-38; Library/PackageCache/com.unity.ugui@8ccc29d23a79/Runtime/TMP/TMP_Text.cs:4836

**Evidencia.** Pone margin.x = icono + separación y enseguida usa GetPreferredValues, que en este TMP suma m_margin.x al ancho. El hueco real queda en separación + (icono + separación)/2: JUGAR 56 u en vez de 13,6, MEJORAS 40 en vez de 9,6, y el grupo queda lugar/4 a la izquierda (−21 u en JUGAR). Se ve en Builds/menu_tienda/0_referencia_menu.png. Ivan aprobó el aspecto actual.

**Arreglo.** Restar texto.margin.x + margin.z al ancho preferido; si se quiere el hueco de hoy, subir separacion en las escenas.

## H61 — Conectar o desconectar auriculares Bluetooth corta la música del menú hasta volver a cargarlo (baja, bug)

- **Fuentes**: sonido#3
- **Archivos**: Scripts/Jugo/FuenteConVolumen.cs:33; Escenas/Menu.unity (AudioSource del GameObject 1452757527)

**Evidencia.** En Android, cambiar la salida de audio reinicia el motor de Unity y se detiene lo que suena (el staff de Unity lo reconoce como limitación en 2022.3 y recomienda AudioSettings.OnAudioConfigurationChanged). Nadie en el proyecto escucha ese aviso, y la música del menú (MainMenu.mp3, PlayOnAwake y Loop) es la única fuente en loop con clip. No verificado en 6000.3.

**Arreglo.** En FuenteConVolumen, suscribirse en OnEnable a AudioSettings.OnAudioConfigurationChanged y, si deviceWasChanged y la fuente es un loop con clip que estaba sonando, darle Play otra vez. Probarlo en el teléfono.

**Reproducción.** Con el menú abierto en el teléfono, conectar o desconectar auriculares Bluetooth: la música no vuelve hasta salir y entrar al menú.

## H62 — Al revivir, la explosión y el cartel salen juntos por la fuente neutra y pasan la escala (baja, bug)

- **Fuentes**: sonido#4
- **Archivos**: Scripts/Jugador/PlayerHealth.cs:292; Jugo/Sonidos.cs:173, :297

**Evidencia.** Revivir toca Efectos.Explosion (1,0, pitch 1) y, si despejó zombis, CartelOleada (0,8, pitch 1): los dos van por la fuente neutra. El limitador baja solo el cartel (a 0,78), porque la explosión ya salió y no se puede bajar. Mezclando los clips reales alineados, el pico llega a 1,22 (+1,8 dBFS) durante 7,7 ms, más que la granada del pendiente. Es un modelo con los clips reales, no una medición; una vez por partida y solo con la oferta de revivir (hoy, la APK de prueba).

**Arreglo.** Tocar el cartel del revivir con un poco de variación de tono (va a una fuente propia y baja parejo) o bajarle el volumen. El arreglo de fondo es el del pendiente: un limitador sobre la salida.

**Reproducción.** APK de prueba (proveedor Falso): morir con zombis alrededor, ver el vídeo y revivir.

**Nota.** Amplía «El limitador de sonido no llega a todo»: otro caso, el del revivir.

## H63 — El control MÚSICA de la pausa no cambia nada que se oiga (baja, calidad)

- **Fuentes**: sonido#6
- **Archivos**: Scripts/UI/VolumenEnPausa.cs:34; Prefabs/Jugo/Efectos.prefab (AudioSource de música sin clip)

**Evidencia.** La pausa arma el control de MÚSICA, pero en la partida no hay música (decisión de Ivan: la fuente de Efectos no tiene clip) y en pausa AudioListener.pause calla todo menos la interfaz. Quien lo mueve no oye nada distinto; el valor recién se nota en el menú.

**Arreglo.** Sacar MÚSICA de la pausa y dejar EFECTOS (que ya suena al moverlo), o tocar ahí una muestra de la música del menú. Lo decide Ivan.

**Reproducción.** En una partida, pausar y mover MÚSICA: no suena nada distinto.

## H64 — El buffer de audio está en «mejor rendimiento» (1024): el sonido puede llegar tarde respecto de lo que se ve (baja, riesgo)

- **Fuentes**: sonido#5
- **Archivos**: ShowBies1/ProjectSettings/AudioManager.asset:12

**Evidencia.** m_DSPBufferSize y m_RequestedDSPBufferSize están en 1024 desde 2022 (Best performance; Good latency es 512). A 48 kHz son 21 ms por buffer y Android encola varios: el golpe, la muerte y la nota de la moneda podrían salir 50-100 ms después del destello o del brillo. Estimación, sin medir.

**Arreglo.** Medir primero en el teléfono (filmar un disparo contra un zombi y contar cuadros entre el destello y el golpe). Si molesta, probar 512 y cuidar que no aparezcan cortes en gama baja.

**Nota.** Confianza baja; sin medir.

## H65 — «Completa N oleadas» (y «mata N» en el libre) se cumplen en un tercio de lo cotizado repitiendo las primeras oleadas (baja, exploit)

- **Fuentes**: oleadas#3
- **Archivos**: Scripts/Progreso/MisionesDiarias.cs:162, :294-297; Progreso/DesafioSemanal.cs:101, :168-171; Progreso/Economia.cs:49-53; UI/MenuPausa.cs:89-93; RestartScene.cs

**Evidencia.** El objetivo es partidas × m oleadas, cotizado a 35,2 s por oleada en m = 40 (1.408 s / 40), pero cuenta cualquier oleada completada. Las oleadas 1-5 en ciclo con REINICIAR tardan ~10,7 s cada una: la difícil (100 oleadas, paga 51.450) sale en ~19 min en vez de 59, y el semanal (600, paga 257.300) en ~1,9 h en vez de 5,9. Matando en el libre en nivel 1-3 salen ~5,9 zombis/s contra 2,6/s de la vara. No rinde más que jugar (~45 contra ~61 monedas/s con las tasas reales del 26/9).

**Arreglo.** Pesar cada oleada completada por (10 + 4n) / el promedio de la vara en RegistrarOleada, y contar para «mata N» solo las muertes de WaveMode (o pesar las del libre). Que la prueba de costos mida contra el camino más barato y no contra la misma vara (ver H94).

**Reproducción.** Mejor oleada 40, con la misión o el semanal de oleadas: jugar las oleadas 1-5, REINICIAR y repetir.

**Nota.** Amplía «"Gana N monedas" y la misión de críticos se miden con varas que no son las del juego»: lo mismo pasa con las de oleadas y de matar. Baja porque no rinde más que jugar bien.

## H66 — El objetivo de jefes cuenta m/10 jefes por partida y no floor(m/10): con el arreglo previsto cuesta hasta el doble (baja, bug)

- **Fuentes**: economia#2
- **Archivos**: Scripts/Progreso/MisionesDiarias.cs:141, :181; Progreso/DesafioSemanal.cs:90, :102; Editor/PruebasMejoras.cs:3437, :3443

**Evidencia.** Una partida hasta la oleada m mata floor(m/10) jefes (jefeCadaOleadas 10), pero los dos objetivos usan m/10: en la 19 la misión pide 5 jefes, que son 5 partidas cuando se pagan 2,5, y el semanal pide 28 cuando se pagan 15. Con mejor oleada 9 aparecen 2 y 14 jefes para alguien que todavía no mató ninguno. La prueba usa la misma fracción y a propósito no mira los objetivos caros. Hoy lo tapa el farmeo de jefes; el arreglo decidido (contar en WaveManager al completar la oleada) lo deja a la vista.

**Arreglo.** Math.Max(1, Math.Floor(m / 10.0)) jefes por partida en los dos Objetivo y en la prueba, y la puerta en mejorOleada ≥ 10. Hacerlo junto con el arreglo de los jefes farmeables.

**Nota.** Amplía «Los jefes de las misiones y del semanal se farmean»: el arreglo decidido tiene que ir con este.

## H67 — IMPARABLE (combo x25/x50/x100) se regala en el modo libre (baja, exploit)

- **Fuentes**: trampas#2
- **Archivos**: Scripts/UI/ContadorCombo.cs:81-86; Jugo/Efectos.cs:146; Escenas/ShowBies1.unity:1327-1331; Zombi/WaveManager.cs:226; Escenas/WaveMode.unity:2092-2093; Progreso/Logros.cs:63

**Evidencia.** El combo se corta con 1,5 s sin muertes, y en oleadas los 3 s de descanso lo cortan en cada oleada: x50 pide la 10 y x100 la 20-23 matando todo sin huecos. En el libre (abierto desde la 12) sale un normal cada 0,25 s de 5 de vida en el nivel 1 (~350 por minuto en PC), y x100 sale en uno o dos minutos; con la R se vuelve al nivel 1. Vale poco en monedas (~1.500 una vez), pero el oro pensado como hito del final lo va a tener todo el mundo cuando los logros vayan a Play Games.

**Arreglo.** Que IMPARABLE cuente solo el combo de las oleadas (RegistrarCombo solo si hay WaveManager), o metas propias para el libre. Decide Ivan.

**Reproducción.** Con el libre desbloqueado, entrar y disparar sin parar; mirar el récord de combo o los logros después de uno o dos minutos.

## H68 — Guardar falla en silencio: con el almacenamiento lleno, toda la sesión queda solo en memoria (baja, riesgo)

- **Fuentes**: guardado#3
- **Archivos**: Scripts/Progreso/Progreso.cs:797-817

**Evidencia.** Cualquier excepción al escribir el .tmp (disco lleno, E/S) termina en un LogWarning; Guardar no devuelve nada y nadie cuenta las fallas. El principal no se toca (bien), pero las compras y cobros que «guardan en el acto» y las monedas de la sesión viven solo en memoria: al cerrar la app el jugador vuelve al último guardado bueno sin que nada le haya avisado.

**Arreglo.** Que Guardar devuelva si pudo y lleve un contador de fallas seguidas; con dos o más, un aviso en el menú («no se pudo guardar: libera espacio», con fila nueva en Textos.txt), y reintentar en el próximo punto seguro.

**Reproducción.** Llenar el almacenamiento, jugar una partida, comprar algo y cerrar la app: al volver no está nada de eso.

**Nota.** Pierde progreso, pero en un caso raro: por el criterio, raro → baja, igual que H69.

## H69 — Cargar prefiere un principal viejo a un .tmp entero y más nuevo (baja, bug)

- **Fuentes**: guardado#2
- **Archivos**: Scripts/Progreso/Progreso.cs:803-812, :834-841; Editor/PruebasMejoras.cs:2496-2526

**Evidencia.** Cargar prueba principal, .tmp y .anterior en ese orden, pero un .tmp legible es siempre igual o más nuevo que el principal: un Guardar exitoso lo consume (:812), así que solo queda si falló o se cortó entre el fsync (:803) y el renombre (:809-812). Pasa si File.Delete o File.Move tiran (en Windows, con el archivo tomado por otro proceso) o con un corte en esa ventana: al abrir se carga el principal y el .tmp se pisa, y se pierde ese guardado (puede ser una compra; si el bloqueo dura toda la sesión, la sesión). ProbarGuardado no cubre principal sano con .tmp sano.

**Arreglo.** Si el principal y el .tmp parsean los dos, tomar el .tmp. Sumar ese caso a ProbarGuardado.

**Reproducción.** Dejar un progreso.json sano con 10 monedas y un progreso.json.tmp sano con 20, y abrir: Cargar toma 10.

**Nota.** Pierde progreso, pero en una ventana de milisegundos o con un bloqueo raro: baja, igual que H68.

## H70 — El reloj confiable puede quedar atrasado para un jugador legítimo hasta reiniciar el teléfono (baja, riesgo)

- **Fuentes**: guardado#6, android#5
- **Archivos**: Scripts/Progreso/RelojConfiable.cs:50-58; Progreso/Progreso.cs:349-358, :587-598

**Evidencia.** Confiable usa la marca más el tiempo real si coincide BOOT_COUNT y el reloj va más de 2 h adelante. (1) Si al cobrar la diaria el reloj estaba más de 2 h atrasado (hora automática apagada y mal puesta, o un teléfono que arranca con la hora mal y la corrige con la red) y el día igual era nuevo, la marca queda con esa hora, y al corregirse el reloj en el mismo arranque Confiable se queda atrasado. (2) La marca viaja en progreso.json: tras una restauración en otro teléfono o un restablecimiento de fábrica, si el número de arranque coincide, el día vuelve semanas atrás. Mientras tanto no hay diaria, misiones, semanal ni topes de vídeo nuevos, y la cuenta de «nuevas en» lo muestra. Es raro.

**Arreglo.** Guardar con la marca un id de instalación (o ANDROID_ID) y descartarla si no coincide; no anclar con un reloj que va atrás de la marca anterior más el tiempo real; y confiar en el reloj si auto_time está prendido, junto con el pendiente de la fecha en el futuro y con H03. Si no, documentarlo junto a la fricción del reinicio.

**Nota.** Confianza baja. Diseñarlo junto con H03 y con «Una fecha guardada en el futuro bloquea la diaria, las misiones, el semanal y los vídeos hasta esa fecha».

## H71 — Con una red de anuncios real, el x2 de la derrota y de la diaria se pierde si Android mata el proceso durante el vídeo (baja, riesgo)

- **Fuentes**: guardado#4
- **Archivos**: Scripts/Progreso/Progreso.cs:157-161, :555-563; Progreso/RecompensaDiaria.cs:80, :120-127; Anuncios/ServicioAnuncios.cs:185-186

**Evidencia.** Lo que se duplica vive en memoria (MonedasDeLaPartida, partidaDuplicada, paraDuplicar) y el premio llega por el callback del SDK. Con AdMob o LevelPlay el vídeo es otra actividad; en teléfonos de 2-3 GB Android puede matar el proceso. El jugador ve el vídeo entero y el juego se reabre desde el menú con lo guardado en :185: no pierde progreso, pero se queda sin el x2 y sin forma de reclamarlo. Hoy no pasa (Nulo en Play; el Falso es un canvas del juego).

**Arreglo.** Al integrar la red, anotar en el progreso un «premio en curso» (lugar, monto, partida) antes de Mostrar; si al reabrir sigue sin resolverse, ofrecerlo de nuevo o darlo con la regla de fallasPremiadasPorDia. Anotarlo en publicacion/pasos.md.

**Nota.** Amplía «Cerrar la app en ¡HAS MUERTO! conserva la oleada en curso» (misma situación, Android matando la app durante el vídeo, del lado del x2). Va con «Antes de integrar la red de anuncios».

## H72 — Los botones de la derrota siguen andando mientras se pide el vídeo del x2: premio perdido y la partida siguiente sin revivir (baja, riesgo)

- **Fuentes**: anuncios#3
- **Archivos**: Scripts/MenuPerdiste.cs:61, :71, :80, :91; Anuncios/OfertaDeDuplicar.cs:87-89, :97; Progreso/Progreso.cs:558, :643

**Evidencia.** Retry, Menu y AbrirMejoras no miran MostrandoAnuncio (solo el Escape). Con una red real, tocar OTRA VEZ antes de que aparezca el anuncio recarga la escena y EmpezarPartida deja MonedasDeLaPartida en 0; al terminar el vídeo, Resolver gasta un uso del día y sube VideosDeLaPartida de la partida nueva, y DuplicarMonedasDeLaPartida da falso: el vídeo visto sin premio y la partida nueva ya no ofrece revivir. Con Falso no pasa, porque el cartel tapa la pantalla en el mismo cuadro.

**Arreglo.** En los tres métodos (o en Salir), «if (ServicioAnuncios.MostrandoAnuncio) return;», como ya hacen la R y el Escape.

**Reproducción.** Con un proveedor que tarde en mostrar: morir con más de 20 monedas y 90 s de partida, tocar VER VÍDEO y enseguida OTRA VEZ, mirar el vídeo entero.

## H73 — El x2 de la diaria se pregunta una sola vez: si la separación de 60 s lo frena en ese momento, se pierde el día (baja, bug)

- **Fuentes**: anuncios#4
- **Archivos**: Scripts/UI/VentanaRecompensaDiaria.cs:183, :221-224; Anuncios/ServicioAnuncios.cs:150, :274

**Evidencia.** Cobrar pregunta PuedeOfrecer(DuplicarRegalo) una sola vez; si da falso, la ventana se va sola a los 1,3 s y la diaria no vuelve ese día. PuedeOfrecer incluye la separación global de 60 s desde el último vídeo premiado: si se vio el x2 de la derrota pasada la medianoche y se entra al menú con la diaria nueva, no aparece VÍDEO: +N MÁS, que en el día 7 vale hasta 2 partidas. Raro: casi todos ven la diaria al abrir la app.

**Arreglo.** Si lo único que falta es la separación, mostrar la oferta y habilitarla cuando se cumpla, o volver a preguntar cada ~0,5 s mientras la ventana está abierta (lo mismo que pasos.md pide para Listo).

**Nota.** Amplía «Antes de integrar la red de anuncios» (volver a preguntar Listo mientras las ofertas están abiertas): pasa ya con el código de hoy, por la separación de 60 s.

## H74 — El atrás que cierra un vídeo real podría llegar a Unity: solo lo filtra el revivir (baja, riesgo)

- **Fuentes**: anuncios#6, android#4
- **Archivos**: Scripts/Anuncios/OfertaDeRevivir.cs:249-257, :378 (ignorarAtrasHasta); MenuPerdiste.cs:87-93; UI/BotonAtrasMenu.cs:28-40; Anuncios/ServicioAnuncios.cs:236-250

**Evidencia.** OfertaDeRevivir ignora el Escape 0,4 s después de volver de un vídeo porque podría llegar también a la actividad de Unity. MenuPerdiste solo mira MostrandoAnuncio, que Resolver apaga en el Update de VigiaAplicacion (en un orden no fijado), y BotonAtrasMenu no mira nada. Con la red real, cerrar el x2 con el atrás podría sacar al jugador de la derrota (perdiendo la oferta que debía volver si fue sin premio) o cerrar la diaria. No está confirmado que Android entregue ese atrás; hoy no pasa (con el Falso el atrás no hace nada y el AAB va en Nulo).

**Arreglo.** Un ServicioAnuncios.AtrasIgnoradoHasta que ponga Resolver y que lean MenuPerdiste, BotonAtrasMenu y OfertaDeRevivir. Sumarlo a «Antes de integrar la red de anuncios» en publicacion/pasos.md (no está).

**Nota.** Confianza baja. Va con «Antes de integrar la red de anuncios».

## H75 — Integrar AdMob choca con el proyecto y pasos.md no lo dice: App ID en el manifiesto, EDM4U y callbacks abstractos (baja, riesgo)

- **Fuentes**: anuncios#1
- **Archivos**: publicacion/pasos.md (Anuncios); ShowBies1/Assets/Plugins/Android/mainTemplate.gradle:1-12; CLAUDE.md:1150-1152

**Evidencia.** Sin la meta-data com.google.android.gms.ads.APPLICATION_ID, el SDK cierra la app al abrir (IllegalStateException de MobileAdsInitProvider), y en Plugins/Android no hay manifiesto propio. El plugin oficial de Unity trae EDM4U, que el proyecto evitó con la reseña porque se pelea con Unity 6. El camino de la reseña (JNI con AndroidJavaProxy) no alcanza: AndroidJavaProxy solo implementa interfaces, y RewardedAdLoadCallback y FullScreenContentCallback son clases abstractas.

**Arreglo.** Sumar a pasos.md un paso sobre cómo entra el SDK: o el plugin, con EDM4U probado en 6000.3.14 y la plantilla propia, o un puente Java (play-services-ads en mainTemplate.gradle y un .java con los dos callbacks). En los dos casos, el App ID en el manifiesto y una prueba que lo verifique en la build con proveedor Real.

**Nota.** Amplía «Antes de integrar la red de anuncios» (sección 5 / publicacion/pasos.md). Baja por el criterio: hoy no le pasa a nadie; es para no perder tiempo al integrar. Confianza alta.

## H76 — La APK de prueba fuerza Falso siempre: con el proveedor Real, el SDK no se probaría nunca en el teléfono (baja, riesgo)

- **Fuentes**: anuncios#2
- **Archivos**: Assets/Editor/ConstructorAndroid.cs:76-78, :207-212; Scripts/Anuncios/ProveedorFalso.cs:26-29, :147-151

**Evidencia.** BuildApk llama FijarProveedorDeAnuncios(Falso) sin condición, y el AAB solo se niega con Falso. pasos.md pide que la APK .prueba use los bloques de prueba de Google, lo que contradice el código. Además, ProveedorFalso.Listo siempre da verdadero y el vídeo solo termina en Recompensado o Cerrado: en el teléfono no se pueden probar NoDisponible, FallaAlMostrar ni una carga tardía.

**Arreglo.** Forzar Falso solo si el asset está en Nulo (o un menú «APK con anuncios de prueba» que deje Real con los ids de prueba), y darle a ProveedorFalso un modo «sin vídeo» o «falla» configurable.

**Nota.** Amplía «Antes de integrar la red de anuncios» (pasos.md: ids de bloque y de prueba).

## H77 — FondoMenu y CapitulosDeEscenario limpian RenderSettings en OnDestroy como si fuera de la aplicación (es por escena) (baja, riesgo)

- **Fuentes**: escenarios#2, estaticos#1
- **Archivos**: Scripts/UI/FondoMenu.cs:118-119, :159, :227-233; Escenario/CapitulosDeEscenario.cs:104-126, :131-136; Editor/ConstructorEscenarios.cs:1033-1049; Scripts/Puntaje.cs:28-38

**Evidencia.** Los dos OnDestroy escriben RenderSettings.fog = false y ambientLight (el del menú, (0,5, 0,53, 0,56), contra (0,15, 0,19, 0,30) de la noche), con el comentario «son de la aplicación» (y CLAUDE.md: «Al descargarse la escena la niebla se apaga»); el propio FondoMenu.cs:118-119 dice lo contrario. RenderSettings es por escena: la escritura cae en la escena activa en ese momento, y una carga Single ya trae los de la nueva. Además CapitulosDeEscenario.Start lee la noche de la pradera de RenderSettings en vez de tenerla guardada. escenarios#2 temía que, si el OnDestroy de la escena vieja corre después del Awake de la nueva con la nueva ya activa, el libre y el tutorial desde el menú arranquen con ~3 veces más ambiente y la pradera de las oleadas lo guarde como su noche. Hoy eso no pasa: Puntaje.Awake hace «else Destroy(gameObject)» sin DontDestroyOnLoad, y si la escena vieja siguiera viva en el Awake de la nueva, REINICIAR y OTRA VEZ romperían el puntaje; nunca se vio. Queda como riesgo si alguna carga pasa a LoadSceneAsync(Single).

**Arreglo.** Sacar las escrituras de niebla y ambiente de los dos OnDestroy (dejar loTapado.Clear y los Destroy), y que PonerLaNoche escriba también escenarios[0], como ya hace con el 1 y el 2, en vez de leer RenderSettings en Start (leerlo en Awake no sirve). Corregir el comentario y la frase de CLAUDE.md. Para confirmar que hoy es inerte: Debug.Log(RenderSettings.ambientLight) en el primer Update de ShowBies1 entrando desde el menú debe dar (0,15, 0,19, 0,30).

**Nota.** escenarios#2 lo dio media (podía pasar hoy) y estaticos#1 baja (latente). Se resolvió por estaticos#1 (inerte hoy con LoadScene síncrono): el singleton de Puntaje lo indica (si la escena vieja siguiera viva en el Awake de la nueva, REINICIAR dejaría sin puntaje), pero no se verificó en Unity. La verificación de una línea sigue valiendo.

## H78 — Las ventanas del menú apagan su Abierta estático solo si el panel sigue vivo (baja, riesgo)

- **Fuentes**: estaticos#3
- **Archivos**: Scripts/UI/VentanaMisiones.cs:138-141, :435; UI/VentanaBestiario.cs:112-114; UI/VentanaLogros.cs:143-145; UI/VentanaRecompensaDiaria.cs:126-128; Resena/PedidoDeResena.cs:102-103; UI/BotonAtrasMenu.cs:48-64

**Evidencia.** En los cuatro OnDestroy: «if (panel != null) Abierta = false;». El panel es un hijo armado en código, y al descargarse la escena Unity no garantiza el orden de OnDestroy entre padre e hijo. Si el hijo ya está destruido, Abierta queda en verdadero en la próxima visita al menú: PedidoDeResena no pide la reseña y el primer atrás se gasta en cerrar una ventana que no está. Hoy no hay camino: cada ventana tapa los toques con un Image a pantalla completa.

**Arreglo.** Abierta = false sin condición (cada ventana vive una sola vez en el menú).

## H79 — android:installLocation=preferExternal (baja, riesgo)

- **Fuentes**: android#3
- **Archivos**: ShowBies1/ProjectSettings/ProjectSettings.asset:181 (AndroidPreferredInstallLocation: 1)

**Evidencia.** Es el valor por defecto de Unity. En teléfonos con la SD adoptada como interna (gama baja de 32 GB) la app se instala ahí: si la tarjeta se saca o falla la app no abre, y carga y guarda (con fsync) más lento. Nada del juego lo necesita.

**Arreglo.** Player Settings > Android > Install Location en Automatic o Force Internal, y verificar el manifiesto de la APK siguiente.

**Nota.** Es otro ajuste que H09 (Install Location, no Preferred Data Location): cambiarlo no mueve progreso.json.

## H80 — Un paquete preview del editor (ai.assistant) mete tres DLL de runtime en el juego (baja, riesgo)

- **Fuentes**: build#6
- **Archivos**: ShowBies1/Packages/manifest.json:3; Library/.../assets/bin/Data/ScriptingAssemblies.json y RuntimeInitializeOnLoads.json; ProjectSettings/ProjectSettings.asset:777-778

**Evidencia.** com.unity.ai.assistant 2.18.0-pre.2 tiene asmdefs de runtime: en la build entran Unity.AI.MCP.Runtime.dll, Unity.AI.Tracing.dll (que corre ConsoleSink.CaptureMainThreadId al arrancar) y Newtonsoft.Json.dll. Hoy es inofensivo (no hay permiso INTERNET), pero una actualización del paquete puede sumar red o Resources, como pasó con Sentis. Quedaron además los defines SENTIS_ANALYTICS_ENABLED y APP_UI_EDITOR_ONLY.

**Arreglo.** Fijar la versión del paquete, que ConstructorAndroid anote o vigile la lista de ensamblados de la build, y borrar los dos defines.

## H81 — CalidadDeAndroid lee QualitySettings del disco y no lo que Unity tiene cargado (baja, riesgo)

- **Fuentes**: build#7
- **Archivos**: Assets/Editor/CalidadDeAndroid.cs:17-41; Assets/Editor/ConstructorAndroid.cs:258, :305

**Evidencia.** La build usa la calidad en memoria, pero el chequeo lee el archivo. Si la trampa de QualitySettings se «arregla» revirtiendo el archivo con git con el editor abierto y Unity no lo recarga (no verificado), el chequeo pasa, el AAB sale fuera de Medium y el SaveAssets del finally vuelve a escribir el archivo sin el bloque.

**Arreglo.** Leer también m_PerPlatformDefaultQuality en memoria (new SerializedObject(QualitySettings.GetQualitySettings())) y exigir Medium en los dos.

**Nota.** Confianza baja.

## H82 — Las capturas y el banner de la ficha de Play son de antes de la noche y del neón (baja, politica)

- **Fuentes**: build#4
- **Archivos**: Builds/ficha/v5/hoja.png; publicacion/pasos.md:36-40; commits 0a4cc87 y 7276891 (25/9)

**Evidencia.** Las 7 capturas subidas el 18/9 muestran el mundo de día, las ventanas crema y la tienda con letras, franja y sello MAXED!. HEAD es de noche, de carbón neón, y la tienda no tiene nada de eso. Que Play rechace la versión es poco probable, pero la ficha deja de representar la app.

**Arreglo.** Rehacer capturas y banner con el HUD del teléfono y la calidad Medium (sin el contador de FPS, ver H83), y mandarlos en el mismo envío que el próximo AAB.

**Nota.** Baja por el criterio: no es un error del juego ni un rechazo probable; es una tarea del próximo envío.

## H83 — El contador de FPS se ve en la versión de Play (y en las capturas de la ficha) (baja, calidad)

- **Fuentes**: build#5
- **Archivos**: Scripts/UI/ContadorFps.cs:15-25; UI/MedidorBalance.cs:38; CLAUDE.md:1210, :1321

**Evidencia.** ContadorFps escribe «N FPS» sin mirar Debug.isDebugBuild (MedidorBalance sí lo mira): lo ve todo jugador, y en hoja.png se lee «60 FPS». CLAUDE.md lo describe como parte del HUD y prevé un interruptor: es para decidir, no un error.

**Arreglo.** Mostrarlo solo en el editor y en builds de desarrollo, o detrás del Interruptor de OPCIONES, apagado por defecto.

## H84 — Progreso.Guardar hace fsync y dos renombres en el hilo principal en momentos de acción (sin medir) (baja, rendimiento)

- **Fuentes**: oleadas#2, guardado#5, tienda#7, rendimiento#1
- **Archivos**: Scripts/Progreso/Progreso.cs:711-730, :790-818; Zombi/GeneradorZombis.cs:118-131; Zombi/WaveManager.cs:122-125; Progreso/MisionesDiarias.cs:119, :124; Progreso/DesafioSemanal.cs:82; UI/MenuPausa.cs:78; Anuncios/VigiaAplicacion.cs:75; Escenas/ShowBies1.unity:1336 (segundosPorNivel 45)

**Evidencia.** Cada Guardar hace ToJson con prettyPrint (~4,2 KB), Flush(true) y hasta tres operaciones de directorio, todo sincrónico en el hilo principal y en el almacenamiento externo de Android (de pocos ms a más de 100 ms en eMMC de gama baja; el cuadro siguiente además recupera varios pasos de física). Se llama cada 45 s en plena pelea del libre (al subir de nivel, el único guardado a mitad de la pelea), al empezar cada oleada (con el cartel y sin zombis), en cada toque de una racha de compras en la tienda, hasta 5 veces en el mismo cuadro al cerrar el día y la semana (también en partida si se cruza la medianoche) y dos veces al pasar a segundo plano.

**Arreglo.** Medirlo primero en el teléfono (Development Build, un marker del Profiler alrededor de Guardar, modo libre al subir de nivel). Si pesa: serializar en el hilo principal y escribir + fsync + renombres en un hilo aparte, de a uno y con un candado; o guardar el libre cada varios niveles o en un cuadro sin pelea; en CerrarElDia y CerrarLaSemana, un solo Guardar al final; en la tienda, con una demora corta después de la última compra. Los de pausa, cierre, muerte y antes del vídeo quedan sincrónicos.

**Reproducción.** Libre en un Android de gama baja con 35 zombis: mirar el cuadro del jingle de «Nivel N» cada 45 s.

**Nota.** Sin medir. Baja por el criterio (rendimiento sin medir); rendimiento#1 lo daba media. Cuatro frentes llegaron solos a lo mismo: medirlo es lo primero.

## H85 — El Juntar de la primera salida del decorado cae con los primeros zombis de las oleadas 11 y 21 (baja, rendimiento)

- **Fuentes**: escenarios#5, rendimiento#4
- **Archivos**: Scripts/Escenario/CapitulosDeEscenario.cs:185-194, :298, :306-335, :363-372; Zombi/WaveManager.cs:123, :132; Escenas/WaveMode.unity:2096, :2877

**Evidencia.** La primera vez cada pieza sale sola: localPosition de ~530 (cementerio) o ~990 (ciudad) transforms por cuadro durante ~1,9 s, y al aterrizar la última (≈3,15 s desde el cambio de oleada) Juntar hace StaticBatchingUtility.Combine de 528/986 renderers (733 y 1.218 objetos) en un cuadro. El primer zombi de la oleada sale a los 3,0 s y el segundo a 3,35 s. Una vez por capítulo y sesión; los decorados crecieron con el neón. Sin medir.

**Arreglo.** Combinar en Armar (apagado, durante un cartel) y usar siempre la rama combinada (sale el decorado entero), o al menos dejar el Juntar para el próximo momento sin zombis vivos, o que el fundido termine antes de los 3 s. Medirlo con el Profiler junto con el pendiente.

**Nota.** Amplía «El decorado del capítulo siguiente se arma en plena pelea»: además del Armar, el Juntar de la primera salida cae con los primeros zombis.

## H86 — El HUD rearma textos en cada cuadro: el contador de balas mientras se dispara, los colores de monedas y combo, y los indicadores radiales (baja, rendimiento)

- **Fuentes**: combate#3, rendimiento#7
- **Archivos**: Scripts/Armas/Balas.cs:25-28; UI/ContadorMonedas.cs:181; UI/ContadorCombo.cs:115; UI/BarraDelJefe.cs:151; UI/IndicadorMejoraCadencia.cs:34; UI/BotonFuria.cs:120; UI/IndicadorRecargaGranada.cs:17

**Evidencia.** El contador de balas escribe solo cuando cambia, pero con la caja de arma o la furia cambia en todos los cuadros: string.Format con boxing más el parseo del rich text y la malla de TMP (y no pasa por FormatoNumeros: muestra «1000/1000»). Cambiar el color de un TMP rehace la malla (lo documenta NumeroFlotante.cs:84-89): ContadorMonedas lo hace en cada cuadro del salto (casi siempre con la escalera de monedas) y ContadorCombo en el desvanecido. BarraDelJefe aloca enemyType.name por cuadro. Los tres indicadores radiales cambian fillAmount en cada cuadro durante la caja (10 s) y los enfriamientos (120 s y 5 s) y ensucian el canvas del HUD.

**Arreglo.** texto.SetText(plantilla, cantBalas, maxBalas), que no aloca (y a 10-15 Hz); color por vértices o CanvasRenderer.SetColor; el nombre del jefe solo al cambiar de jefe; los indicadores radiales en un sub-canvas propio. Medirlo con el Profiler junto con el resto de rendimiento.

## H87 — El aplastado del golpe escala la raíz física del zombi en cada paso (baja, rendimiento)

- **Fuentes**: rendimiento#2
- **Archivos**: Scripts/Zombi/EnemyController.cs:930-936, :965-973, :1272-1277; Prefabs/Personajes/ZombiBOSS.prefab:156-157

**Evidencia.** GolpeVisual escala la raíz (1,15/0,85/1,15) y FixedUpdate la recupera en ~6 pasos (10 el jefe): los tres colliders y el Rigidbody se re-escalan sin parar bajo fuego. Por geometría la cápsula sube 7,5 cm (normal), 15 (tanque) y 30 (jefe) y se ensancha hasta 22 cm; al recuperarse se mete en el piso y PhysX la saca de golpe, y los pies del modelo del jefe suben ~30 cm por golpe. Costo de PhysX y efecto visual sin medir.

**Arreglo.** Aplastar el hijo modelo en LateUpdate, como ya hacen JefePatrones y el festejo, y no la raíz. Correr Golpe animado y Grabar al jefe antes y después.

## H88 — 22 materiales del decorado en Standard con brillos y reflejos (no solo los tres pisos) (baja, rendimiento)

- **Fuentes**: rendimiento#3
- **Archivos**: Assets/Escenarios/Ciudad/Vereda.mat (y Cordon, Pared, Ventana*, Vidrio, Poste, Rueda, Auto*, Contenedor, Lapida, Hierro, Madera, Mata, Tallo)

**Evidencia.** Todos con m_Shader fileID 46 y _SpecularHighlights 1 / _GlossyReflections 1. Vereda son 16 losas de 14x14 m (3.136 m², un tercio del mapa de la ciudad y donde se juega): buena parte de la pantalla con el shader más caro del pipeline integrado.

**Arreglo.** Apagar Specular Highlights y Reflections (en ConstructorEscenarios, o por el inspector: tocar el float del YAML no prende la keyword), o pasar las piezas a Mobile/Diffuse. Mostrarle a Ivan el antes y el después (el brillo «mojado» de la ciudad).

**Nota.** Amplía «El piso: sin Specular Highlights ni Reflections en los tres materiales»: son 22 materiales más.

## H89 — Maximum Allowed Timestep 0,333: cada tirón se paga con hasta 16 pasos de física (baja, rendimiento)

- **Fuentes**: rendimiento#5
- **Archivos**: ShowBies1/ProjectSettings/TimeManager.asset (Maximum Allowed Timestep 0.33333334, Fixed Timestep 0.02)

**Evidencia.** Es el valor por defecto de Unity. Tras un cuadro largo (un shader nuevo, el Instantiate o el Combine del decorado, el fsync del guardado) el siguiente corre todos los pasos atrasados, hasta 16, cada uno con los FixedUpdate y OnCollisionStay de 35 zombis: alarga el tirón. No cambia el régimen normal.

**Arreglo.** Bajarlo a 0,1 (5 pasos como mucho; también topea Time.deltaTime, así las balas no saltan 0,9 m en un cuadro). Medir en el teléfono que la pelea no cambie; si se toca el Fixed Timestep (pendiente de la cámara), hacerlo junto.

## H90 — El GPU Instancing de los zombis solo juntaría las cabezas (baja, rendimiento)

- **Fuentes**: rendimiento#8
- **Archivos**: ToonyTinyPeople/TT_demo/prefabs/zombiRapido.prefab:804, :903, :911; ProjectSettings/ProjectSettings.asset:431-433

**Evidencia.** El cuerpo es SkinnedMeshRenderer y la cabeza MeshRenderer: el GPU Instancing no aplica a los cuerpos, y las cabezas pueden partirse por luces por vértice y SH. Android tiene el Dynamic Batching apagado: dos sprites por barra de vida, hasta 40 números TMP 3D y las manchas son ~100 draw calls chicas con fuego alto.

**Arreglo.** Medir esperando solo una draw call menos por zombi (la cabeza); con el Frame Debugger contar barras, números y manchas antes de probar el Dynamic Batching.

**Nota.** Amplía «Los zombis: GPU Instancing en TT_demo.mat y las cuatro Zombi*Piel.mat...»: baja lo que se puede esperar de ese arreglo.

## H91 — El tutorial busca la granada en toda la escena en cada cuadro (baja, rendimiento)

- **Fuentes**: rendimiento#9
- **Archivos**: Scripts/Tutorial/TutorialManager.cs:86-94

**Evidencia.** En el paso de la granada, mientras no se tiró, FindFirstObjectByType<Granade>() en cada Update, sobre una escena con los 809 objetos de la pradera; dura lo que tarde el jugador en entender el botón.

**Arreglo.** Anotar estadisticas.granadasTiradas al entrar al paso y compararlo, o que ThrowGranade avise.

## H92 — Los bancos de la horda juegan la oleada guardada en el editor: Muerte animada falla sola en múltiplos de 10 y desde la ~35 (baja, bug)

- **Fuentes**: pruebas#2
- **Archivos**: Assets/Editor/PruebaMuerteAnimada.cs:28-30, :100; Scripts/Zombi/WaveManager.cs:104-113; Escenas/WaveMode.unity:2092-2096

**Evidencia.** Solo ModoLibre, Tienda y MenuYTienda arman un progreso conocido; Muerte, Golpe, Disparo y Derrota retoman Progreso.OleadaEnCurso. MuerteAnimada no mata al jefe (la oleada lo espera) y el jugador no dispara: en una oleada 10/20/30 la oleada no termina nunca. Además el último zombi de la oleada n sale a los 3 + 0,35·(9 + 4n) s y el banco mata hasta los 55 s: desde n ≈ 33-35 tampoco termina. La falla dice «la oleada no avanzó: los muertos no se cuentan», justo la regresión que vigila. PruebaDerrota se arregló el 27/9 por la misma causa; las otras no.

**Arreglo.** Después de RespaldoDelBanco.Guardar, fijar el progreso como PruebaModoLibre.PrepararProgreso (ReiniciarTodo, GuardarOleadaEnCurso con una oleada fija, Guardar) en los cuatro bancos de la horda.

**Reproducción.** Dejar el editor con la oleada en curso en 10 (o 35) y correr «Muerte animada (play)»: prueba_muerte.txt termina en «la oleada no avanzó».

**Nota.** Baja: es una falla falsa de un banco del editor, no un error del juego.

## H93 — El revivir y el x2 de la derrota no los recorre ningún banco (baja, riesgo)

- **Fuentes**: pruebas#3
- **Archivos**: Assets/Editor/PruebasMejoras.cs:1502-1517 (solo BalasAlRevivir); PruebaDerrota (no recorre el revivir); PruebaDiaria.cs:103-117 (proveedor inyectable)

**Evidencia.** De OfertaDeRevivir (440 líneas) solo se prueba una función estática, y OfertaDeDuplicar no aparece en ningún archivo de Assets/Editor. Nada prueba el timeScale en 0, SinPremio con el reloj corrido, el despeje sin monedas, Postergar al jefe, la gracia ni el NoSeCobro de la derrota. Es la monetización que toca la partida, y pendientes.md ya pide cambiarla antes de integrar la red; hoy solo se prueba en el teléfono.

**Arreglo.** Un banco «Revivir (play)» con el proveedor del banco de PruebaDiaria: cerrar sin premio (la partida no termina y el reloj no se vence), premiar (vida, despeje sin puntos, el jefe no, 500 balas, gracia), rechazar, y el x2 de la derrota cerrado y premiado.

**Nota.** Baja por el criterio (cobertura), pero conviene tenerlo antes del arreglo de «Cerrar la app en ¡HAS MUERTO!...» y de integrar la red.

## H94 — Las siete pruebas de «cuesta lo que paga» son circulares, y los 120 s y 5 s están escritos a mano tres veces (baja, calidad)

- **Fuentes**: pruebas#5
- **Archivos**: Assets/Editor/PruebasMejoras.cs:1469-1492, :3417-3452; Scripts/Progreso/MisionesDiarias.cs:151-184; Progreso/Economia.cs:43; Prefabs/Personajes/Jugador.prefab:173, :221

**Evidencia.** Matar, Oleada, Furia, Granadas y Jefe calculan el costo con la misma fórmula del objetivo, invertida, igual que monedas y críticos: solo puede fallar el redondeo, que el 0,75 absorbe. Las proporciones de premio (misiones :3365-3384, semanal :3511, bestiario :3676, diaria :4087-4092) dividen fracción·partidas·vara por partidas·vara: prueban constantes, no «toda la curva». La furia cada 120 s y la granada cada 5 están en Economia, MisionesDiarias y la prueba, y la verdad está en el prefab (enfriamiento 120, granadaCooldown 5): si se rebalancean ahí, nada falla.

**Arreglo.** Comparar el 120 y el 5 de MisionesDiarias con Furia.enfriamiento y granadaCooldown del prefab, al lado de ProbarPuntoDeLaGranada, que ya lo lee; y medir contra la vara «de más» del pendiente cuando exista (y contra el camino más barato, ver H02 y H65).

**Nota.** Amplía «Las dos pruebas son circulares» (dentro de «"Gana N monedas"...»): son siete, y las constantes no se comparan con el prefab.

## H95 — El modo libre no lo juega ningún banco, y nadie prueba que desbloqueado lleve al libre (baja, riesgo)

- **Fuentes**: pruebas#6
- **Archivos**: Assets/Editor/PruebasMejoras.cs:2585-2590; PruebaModoLibre.cs:495-498; PruebaMenuYTienda.cs:1039

**Evidencia.** GeneradorZombis (escalado por tiempo, un jefe a la vez, no subir de nivel muerto) solo lo toca MedirPartida, que se corre a mano. La prueba de lógica prueba EscenaPara sin progreso y con las oleadas, nunca «con la 11 completada da el libre»; PruebaModoLibre solo toca el botón bloqueado y PruebaMenuYTienda acepta cualquier modo tras ¡A JUGAR!. Un EscenaPara que devolviera siempre las oleadas pasaría todas las pruebas.

**Arreglo.** El caso positivo en la lógica (RegistrarOleadaCompletada(11) en la carpeta de pruebas) y, en PruebaModoLibre, tocar el botón desbloqueado y mirar que cargue la escena 1.

**Reproducción.** Cambiar ModoLibre.EscenaPara para que devuelva siempre EscenaOleadas: Lógica de mejoras y los bancos siguen en TODO OK.

## H96 — Tres bancos, si el camino real falla, hacen la acción por atrás y pasan igual (baja, calidad)

- **Fuentes**: pruebas#7
- **Archivos**: Assets/Editor/PruebaTienda.cs:621-635, :922-943; PruebaTutorial.cs:515-522, :1000; PruebaMenuYTienda.cs:499-519

**Evidencia.** PruebaTienda: si el raycast no cae en el botón, toca el botón directo, y esa caída no entra en ok[]. PruebaTutorial: si caminar no agarra la caja, llama a OnTriggerEnter por reflexión y acepta cualquiera de los dos. PruebaMenuYTienda: si MEJORAS no abre, llama a tienda.Abrir() y solo lo anota. Algo que tape el botón de comprar, o una caja que el trigger no agarra, pasarían.

**Arreglo.** Que el atajo cuente como falla, o como un OK con aviso que no sume al TODO OK.

**Reproducción.** Poner raycastTarget en true a la flecha de GuiaPrimeraCompra encima del botón de DAÑO: PruebaTienda sigue diciendo que el toque compra.

## H97 — El arreglo del 27/9 en JefePatrones.Empezar (cortar el zarpazo, dibujar la línea antes) no lo cubre ninguna prueba (baja, calidad)

- **Fuentes**: pruebas#9
- **Archivos**: Scripts/Zombi/JefePatrones.cs:407-427; Assets/Editor/PruebasMejoras.cs:2242-2387

**Evidencia.** 8219044 hizo que Empezar llame a zombi.CortarZarpazo() y dibuje la línea o el anillo antes de prenderlos. ProbarPatronesDelJefe llega por reflexión a Aturdir, Terminar, Postergar y EmpezarEmbestida, pero no a Empezar: si se saca el CortarZarpazo no falla nada; solo lo vio la grabación del jefe revisada a ojo.

**Arreglo.** En la vista previa: golpeEnCurso en true, invocar Empezar y mirar que se corte el zarpazo y que puedeZarpar quede en falso.

## H98 — HerramientasProgreso: «Reiniciar todo» abre un modal, y los atajos dicen que guardaron aunque el progreso esté en solo lectura (baja, politica)

- **Fuentes**: herramientas#3
- **Archivos**: Assets/Editor/HerramientasProgreso.cs:54-63, :78-79, :118-120; Scripts/Progreso/Progreso.cs:756-764, :792, :857-878

**Evidencia.** Es el único EditorUtility.DisplayDialog de Assets/Editor, y la regla de la casa (no pedirle clicks a Ivan en Unity) prohíbe los modales porque bloquean el editor y la sesión que lo maneja. Su texto dice que se pierden monedas, mejor oleada y niveles, pero ReiniciarTodo borra todo el JSON. Con el progreso en solo lectura (versión futura o archivo ilegible), Guardar vuelve sin escribir y la herramienta loguea «ahora hay N monedas en <ruta>» igual.

**Arreglo.** Que el ítem copie progreso.json* a Library/ShowBies (o a un .bak), reinicie sin preguntar y deje la ruta de la copia en el log; que los atajos miren Progreso.SoloLectura y den LogError en vez del log de éxito.

## H99 — Con dos copias de ShowBies abiertas, los respaldos de los bancos se pisan (baja, riesgo)

- **Fuentes**: herramientas#5
- **Archivos**: Assets/Editor/RespaldoDelBanco.cs:23, :85-108, :118-132; ProjectSettings/ProjectSettings.asset:15-16

**Evidencia.** La copia va a Library/ de cada copia del proyecto, pero lo respaldado (persistentDataPath y los PlayerPrefs de IvRu/ShowBies) lo comparten todas: si A corre un banco y B corre otro en el medio, B respalda el estado de prueba de A y al final lo devuelve como si fuera el real. Hoy git worktree list muestra una sola copia. Borrar Library después de un cuelgue a mitad de un banco también se lleva la copia pendiente.

**Arreglo.** Dejar la marca de «banco corriendo» junto al progreso (en persistentDataPath, con la ruta del proyecto) y que Guardar se niegue si encuentra la de otro proyecto.

## H100 — La restauración de los bancos quedó duplicada, en parte sin efecto e incompleta (baja, calidad)

- **Fuentes**: muerto#3, estaticos#2
- **Archivos**: Assets/Editor/RespaldoDelBanco.cs:50-52, :147, :151-153; PruebaModoLibre.cs:747-758; PruebaDisparo.cs:86-97; PruebaTienda.cs:189-196, :979-980; PruebaDiaria.cs:841, :852-853; Scripts/Plataforma.cs:21

**Evidencia.** RespaldoDelBanco ya devuelve el progreso, los PlayerPrefs, runInBackground y el teclado, y pone Progreso.datos en null; aun así DevolverPrefs y ModoTelefono/DevolverElTeclado lo repiten, y la reflexión que pone datos en null está copiada tres veces. El runInBackground = false de cada Terminar corre en play y, según el propio RespaldoDelBanco, «eso no queda»; PruebaDiaria.cs:841 lo pone en falso en el mismo EnteredEditMode en que el respaldo lo devuelve, en un orden no definido. Restaurar hace EditorPrefs.SetBool del teclado sin Plataforma.OlvidarPreferenciaDelEditor(): la caché tecladoEnElEditor (leída una vez por dominio) conserva lo del play hasta el próximo play. Tampoco devuelve el Idioma en memoria ni Progreso.soloLectura, ni sube Progreso.Revision.

**Arreglo.** Borrar las copias; dejar en el respaldo lo que falta (Idioma.Cambiar y Plataforma.OlvidarPreferenciaDelEditor después de la línea 147) y sacar el runInBackground = false de cada Terminar. El SaveAssets en play solo donde haga falta (hipótesis de confianza baja: es lo que escribe el atlas de Bangers).

**Reproducción.** Con «Teclado y mouse en el editor» prendido, correr PruebaDisparo y salir de play a mano antes de que termine: en modo edición Plataforma.EsMovil queda en «teléfono» hasta el próximo play.

## H101 — CLAUDE.md y MenuPausa.cs mandan cortar el input con Pausado; lo que corta de verdad es JuegoCongelado (baja, doc)

- **Fuentes**: tiempo#3, claudemd#1
- **Archivos**: CLAUDE.md:181, :1566-1568, :1990 (paso 9 de «Para agregar una mecánica nueva»); Scripts/UI/MenuPausa.cs:12, :24-34; Jugador/PlayerController.cs:75, :293; Jugador/PlayerJS.cs:36; Jugador/Furia.cs:63, :92; Tutorial/GuiaPrimeraPartida.cs:126

**Evidencia.** Tres lugares de CLAUDE.md, entre ellos la receta para una mecánica nueva, y la cabecera de MenuPausa dicen que quien lee input mira MenuPausa.Pausado. El código corta con JuegoCongelado (pausa, oferta de revivir o derrota), y CLAUDE.md:1117 y :1518 lo dicen bien. Desde el 23/9 la derrota corre a timeScale 1: siguiendo la receta, lo nuevo leería input con el ¡HAS MUERTO! abierto y detrás de la derrota.

**Arreglo.** Cambiar las tres menciones y el comentario de MenuPausa.cs a MenuPausa.JuegoCongelado, y dejar Pausado solo para lo que depende del timeScale 0 del menú (el temblor, el combo, el audio).

**Nota.** El primero de los doc: es la receta que siguen los agentes al sumar algo.

## H102 — pedo.mp3 suena en cada derrota y la doc dice que el pedo no suena (baja, doc)

- **Fuentes**: claudemd#7
- **Archivos**: Escenas/Perdiste.unity:717-735 (AudioSource del GameObject Menu, m_PlayOnAwake 1, guid 400a6d27 = otros/pedo.mp3); Scripts/MenuPerdiste.cs:31-45; CLAUDE.md:68, :1805

**Evidencia.** El AudioSource del objeto Menu de Perdiste está prendido, con PlayOnAwake, pedo.mp3, 2D y el objeto activo (verificado en el YAML). MenuPerdiste.Awake apaga cámaras, oído, luces y EventSystem de la escena aditiva, pero no sus AudioSource, y nadie pausa el audio: suena en cada derrota encima de la partida. Está así desde 97901db (3/11/2022, «sonidos»): casi seguro es a propósito. Pero CLAUDE.md solo dice que el pedo del Jugador «no suena nunca» (cierto: en las escenas de juego tiene PlayOnAwake 0) y el layout de otros lo lista sin uso. Con la horda festejando callada (27/9), es el único sonido propio de la derrota.

**Arreglo.** Documentarlo como intencional en Jugo o en La derrota encima de la partida (o quitarlo si no lo es) y corregir el Descartado de pendientes.md.

**Reproducción.** Morir en WaveMode: al cargarse Perdiste en modo aditivo suena pedo.mp3.

**Nota.** Amplía el Descartado «La derrota suena con pedo.mp3» («Refutado», sin motivo anotado en 2af1549): el YAML muestra que sí suena. No es un bug, es la doc.

## H103 — El menú no usa la noche del cementerio desde el 25/9: cielo azul sobre la tierra violeta (baja, doc)

- **Fuentes**: claudemd#4
- **Archivos**: CLAUDE.md:1270-1272; Escenas/Menu.unity:2212-2216; Escenas/WaveMode.unity:2858-2862; Escenarios/Cementerio/PisoCementerio.mat:81; Scripts/UI/FondoMenu.cs:18-22

**Evidencia.** 7276891 pasó el cementerio a violeta (cielo 0,05/0,03/0,09, luz 0,62/0,8/0,72, ambiente 0,14/0,14/0,22). FondoMenu conserva la noche azul de antes (cielo 0,07/0,09/0,17, luz 0,55/0,66/1), los valores del cementerio en e6556d9: el menú pone cielo y niebla azules sobre PisoCementerio, ahora violeta. El comentario de FondoMenu dice «misma paleta que el capítulo 2» y «Las partidas siguen de día».

**Arreglo.** Llevar la paleta violeta a los cinco campos de FondoMenu en Menu.unity, o corregir la frase de CLAUDE.md y el comentario.

## H104 — «Un enemigo nuevo no pide tocar código» es falso desde el bestiario y la v6 (baja, doc)

- **Fuentes**: claudemd#2
- **Archivos**: CLAUDE.md:1972-1975; Scripts/Progreso/Bestiario.cs:19-34; UI/VentanaBestiario.cs:37-44; Progreso/NivelJugador.cs:32-43; Progreso/Logros.cs:71; Editor/PruebasMejoras.cs:3876-3891

**Evidencia.** Bestiario.Tipos, MonedasPorTipo, coloresTipo y PuntosPorTipo son listas fijas de cinco tipos: un tipo nuevo no tiene tarjeta ni estrellas, no suma a COLECCIONISTA y migra con 1 punto. La prueba que «compara con los assets» recorre Bestiario.Tipos y no Assets/Zombies, así que no avisa.

**Arreglo.** En el paso 1 de la receta, listar esos cuatro lugares y su nombre en la tabla; que la prueba recorra Assets/Zombies/*.asset.

## H105 — La trampa de QualitySettings dice que nada lo delata; la build ya se niega (baja, doc)

- **Fuentes**: claudemd#3
- **Archivos**: CLAUDE.md:70, :1845-1851; Assets/Editor/ConstructorAndroid.cs:302-310; Editor/CalidadDeAndroid.cs; Editor/PruebasMejoras.cs:204

**Evidencia.** CLAUDE.md dice que la build de Android saldría en el nivel por defecto «sin que nada lo delate», pero ConstructorAndroid corta la build si !CalidadDeAndroid.EstaEnMedium() y la Lógica de mejoras corre ProbarCalidadDeAndroid. CalidadDeAndroid no figura en el layout de Assets/Editor ni en las negativas de la sección Build de Android.

**Arreglo.** Cambiar la frase (la build se niega y la prueba falla; revertir con git, con el cuidado de H81) y sumar CalidadDeAndroid al layout.

## H106 — Frases viejas de anuncios y progreso en CLAUDE.md: «único momento», «hoy, el x2», «todavía no los muestra nada» (baja, doc)

- **Fuentes**: claudemd#5
- **Archivos**: CLAUDE.md:726, :731, :1019; Scripts/Progreso/RecompensaDiaria.cs:114; Progreso/MisionesDiarias.cs:247; Progreso/NivelJugador.cs:174; UI/VentanaBestiario.cs:191; Progreso/Logros.cs:61-72

**Evidencia.** CLAUDE.md:1019 dice que «la derrota es el único momento» de los vídeos, cuando 1010-1012 listan revivir y la diaria. :726 dice que CobrarPremio es «hoy, el x2 de un vídeo», pero también lo usan la diaria, las misiones, el cofre, el semanal, el bestiario y los niveles. :731 dice que los contadores de por vida «todavía no los muestra nada», y los muestran el bestiario y los logros.

**Arreglo.** Reescribir las tres frases con el estado actual.

## H107 — «Mismo mapa que el libre» en las oleadas (baja, doc)

- **Fuentes**: claudemd#6
- **Archivos**: CLAUDE.md:18; Escenas/WaveMode.unity:2843-2876; Prefabs/Escenarios/Ciudad.prefab

**Evidencia.** El área y los puntos de aparición son los mismos, pero el decorado cambia por capítulo (11-20 cementerio, 21-30 ciudad). La nota sobre «Ciudad» sacada confunde ahora que existe un capítulo Ciudad.

**Arreglo.** «La misma área; el decorado cambia cada 10 oleadas (ver Capítulos)».

## H108 — Desactualizaciones chicas del layout y del texto de CLAUDE.md (baja, doc)

- **Fuentes**: claudemd#8
- **Archivos**: CLAUDE.md:51, :68, :70, :424, :492, :1758, :1963, :1983; ProjectSettings/TagManager.asset:7-8; Animaciones/Zombi.controller:10, :37, :63, :89; Editor/HerramientasProgreso.cs:21-28, :42; Scripts/Armas/BulletController.cs:117-120

**Evidencia.** Faltan en el layout ProximoObjetivo (UI), CalidadDeAndroid (Editor) y combo.wav (otros). Las tags Zombi y Terreno tampoco se usan y no se mencionan. Dice «tres estados» y el controller tiene cuatro. El rótulo de los niveles de prueba omite granada y críticos 4. :424 dice que el zombi muerto «ya está apagado», pero el cadáver sigue 1,4 s prendido con los colliders apagados (mismo comentario en BulletController). «Los cuatro canvas» ya son más.

**Arreglo.** Una pasada de texto sobre esas líneas (el comentario de MenuPerdiste.cs:8 va en H109).

## H109 — Comentarios que describen la derrota vieja (jugador destruido, partida congelada) (baja, doc)

- **Fuentes**: jugador#4
- **Archivos**: Scripts/Camara/CamaraJugador.cs:127-129; MenuPerdiste.cs:8-9, :52-53; Jugador/PlayerJS.cs:14-16; Jugo/FiltroBlancoYNegro.cs (Enganchar)

**Evidencia.** CamaraJugador dice que PlayerHealth destruye al jugador y después carga Perdiste; ya no pasa ninguna de las dos. MenuPerdiste habla de una «partida congelada» y de que «quedó en timeScale 0», cuando la derrota lo pone en 1 (DerrotaEnLaPartida.cs:82). PlayerJS nombra Application.isMobilePlatform, y lo que decide es Plataforma.EsMovil. Engañan justo en el flujo que más cambió. FiltroBlancoYNegro.Enganchar no lo llama nadie (todo usa Tomar).

**Arreglo.** Reescribir los tres comentarios según el flujo actual y borrar Enganchar.

## H110 — VigiaAplicacion guarda al pasar a segundo plano, no al perder el foco, aunque su comentario y CLAUDE.md dicen que sí (baja, doc)

- **Fuentes**: guardado#7
- **Archivos**: Scripts/Anuncios/VigiaAplicacion.cs:8-10, :73-82; CLAUDE.md (tabla de Anuncios y Persistencia)

**Evidencia.** El código guarda solo en OnApplicationPause(true); OnApplicationFocus solo lleva la cuenta de la ausencia. En partida lo cubre MenuPausa.OnApplicationFocus, y en el menú y la derrota todo ya se guardó en el acto: no se pierde nada, lo que está mal es la descripción.

**Arreglo.** Corregir el comentario y CLAUDE.md («al pasar a segundo plano»), o guardar también en OnApplicationFocus(false).

## H111 — CLAUDE.md dice que la diaria espera a la primera partida terminada; también la abre la primera oleada completada (baja, doc)

- **Fuentes**: tutorial#7
- **Archivos**: Scripts/UI/VentanaRecompensaDiaria.cs:105; Tutorial/PrimeraVez.cs:22-25; CLAUDE.md (Primera vez, Recompensa diaria)

**Evidencia.** La diaria corta con NoTerminoPartidas, que pide ninguna partida terminada y además ninguna oleada completada: quien completa la oleada 1 y sale por la pausa ve la diaria sin haber terminado ninguna partida. El comportamiento está bien; el documento dice otra cosa.

**Arreglo.** «Desde la primera partida terminada o la primera oleada completada».

## H112 — La niebla no se ve en la partida y el borde del mapa queda a la vista, aunque el código y CLAUDE.md digan que lo tapa (baja, doc)

- **Fuentes**: escenarios#4
- **Archivos**: Scripts/Escenario/CapitulosDeEscenario.cs:26, :231-234; Escenas/WaveMode.unity:21, :818, :845; CLAUDE.md (Capítulos)

**Evidencia.** Cámara a 12 m, 70° y 60° de campo: el piso más lejano en pantalla está a 16,2 m de profundidad de vista, y la niebla lineal es 16-46 o 18-50: arriba queda entre 0,7 % y 0 % de niebla. Son falsos el tooltip («tapa el borde del mapa») y lo de «se cierra o se abre». Con el jugador en ±48 se ven 4 a 11 m de vacío más allá del piso de ±50.

**Arreglo.** Corregir el tooltip, el comentario y CLAUDE.md. Si se quiere tapar el borde: piso a escala 20 (con el tiling de los materiales ×2) o una niebla que muerda (por ejemplo 10-24 m).

## H113 — LeerEscena lee la escena abierta en memoria, no el disco, aunque el comentario y CLAUDE.md digan lo contrario (baja, doc)

- **Fuentes**: pruebas#8
- **Archivos**: Assets/Editor/PruebasMejoras.cs:1605-1622, :1861-1865; CLAUDE.md (trampa de las instancias de prefab)

**Evidencia.** Si la escena ya está cargada en el editor, se lee esa, con sus cambios sin guardar y con los cambios a instancias de prefab que no se anotaron como override. El comentario de ProbarPartidaNeon y la trampa de CLAUDE.md («la prueba de lógica lee las escenas de disco, así que lo nota») lo dan por hecho: con WaveMode abierta (la dejan así los bancos) y un joystick recoloreado sin Anotar, pasaría igual.

**Arreglo.** Si la escena está abierta y sucia, avisar o leerla en una escena de vista previa; corregir la frase de CLAUDE.md.

## H114 — pasos.md no refleja el estado de Play, y el repo sigue público con gh-pages (baja, doc)

- **Fuentes**: build#8
- **Archivos**: publicacion/pasos.md:21-26, :31, :55

**Evidencia.** pasos.md sigue diciendo «versionCode 4 y 1.1.0» y deja Seguridad de los datos sin enviar, aunque la 5 se mandó a revisión. El link nuevo de la política está «guardado sin enviar» y Play enlaza gh-pages; la API de GitHub da ivanlruiz/Showbies público con Pages. Si se privatiza (el motivo de la mudanza) antes de que Play muestre el link nuevo, el link de la política queda roto.

**Arreglo.** Actualizar pasos.md al preparar el próximo envío (con H07 y H08) y dejar escrito el orden: primero el link nuevo en Play, después privatizar el repo.

## H115 — MainMenu.PlayGame no lo llama nadie y CLAUDE.md lo da como camino al libre (baja, doc)

- **Fuentes**: muerto#7
- **Archivos**: Scripts/MainMenu.cs:29-32; CLAUDE.md:93, :114; Scripts/UI/CurvasUI.cs:27; Scripts/Puntaje.cs:13; UI/SliderVolumen.cs:19, :73; Anuncios/LugarAnuncio.cs:15-16

**Evidencia.** Los m_MethodName de MainMenu en Menu.unity son TocarJugar, GameModes, Tutorial y QuitGame; PlayGame no aparece ni se llama desde código. CLAUDE.md lo nombra en la tabla de índices y en la lista de caminos que pasan por ModoLibre.EscenaPara, que es donde mira quien sume un camino al libre. Otros restos sin uso: CurvasUI.SalidaElastica, Puntaje.enemy, SliderVolumen.slider (se escribe y nunca se lee) y LugarAnuncio.MonedasTienda / DuplicarBono.

**Arreglo.** Borrar PlayGame y corregir las dos líneas de CLAUDE.md; borrar el resto o marcar las constantes de lugares como reservadas.

## H116 — Anuncios: código muerto, comentarios viejos y el cartel del anuncio de prueba con voseo, fuera de la tabla y sin Bangers (baja, calidad)

- **Fuentes**: anuncios#5, idiomas#6
- **Archivos**: Scripts/Anuncios/ServicioAnuncios.cs:54-64; Anuncios/OfertaDeDuplicar.cs:5-6; Anuncios/ProveedorFalso.cs:116-117, :122, :135-136; CLAUDE.md:1080

**Evidencia.** HayProveedor y NombreDelProveedor no tienen llamadores, y su comentario habla de un interruptor del menú que ya no existe. OfertaDeDuplicar dice que es «el único lugar donde hoy entra un anuncio», y son tres. CLAUDE.md llama «semitransparente» a la ventanita del revivir, que desde 0a4cc87 tiene el fondo en alfa 0,92. El cartel de ProveedorFalso tiene «Terminó: tocá LISTO y cobrás», «SALTEAR» y «LISTO» escritos a mano, con voseo, en español aunque el juego esté en inglés y con la fuente por defecto. Solo la APK de prueba (el AAB se niega con Falso).

**Arreglo.** Borrar las dos propiedades, corregir los dos comentarios y la frase de CLAUDE.md. En el cartel falso: «toca LISTO y cobra», «SALTAR» y la fuente del juego (pasarlo a la tabla es opcional).

## H117 — FondoMenu: la rama del día parece muerta pero sostiene el piso de noche (baja, riesgo)

- **Fuentes**: muerto#8
- **Archivos**: Scripts/UI/FondoMenu.cs:140-150, :163, :176-177, :224

**Evidencia.** Con Tema.Oscuro fijo en verdadero, el menú arranca de noche y no vuelve: materialPiso, colorCielo e intensidadLuz del día no se ven nunca. Pero el piso solo se crea con if (materialPiso != null), y Aplicar le pone el de noche después: quien vacíe el material de día «porque está muerto» deja a los zombis del fondo caminando en el aire. El comentario de la línea 176 (el modo oscuro se toca en opciones) ya no vale.

**Arreglo.** Crear el piso si hay materialPiso o materialPisoNoche y corregir el comentario; si el día no vuelve, sacar la rama entera (mezcla, objetivo, el fundido y los campos del día).

## H118 — NewAudioMixer.mixer sin trackear: un mixer vacío que no usa nadie (baja, calidad)

- **Fuentes**: muerto#4
- **Archivos**: Assets/NewAudioMixer.mixer, Assets/NewAudioMixer.mixer.meta

**Evidencia.** Sin trackear desde el 15/9. Es el mixer por defecto (solo Master con Attenuation, sin parámetros expuestos); ningún YAML referencia su guid, ningún AudioSource tiene un output group y ningún script usa AudioMixer. El riesgo es que entre en un git add -A.

**Arreglo.** Borrar los dos archivos. Si hace falta un limitador sobre la salida (pendiente del limitador), armar el mixer entonces, con sus grupos.

## H119 — Assets propios sin ninguna referencia (ninguno entra en la build), dos de licencia desconocida (baja, calidad)

- **Fuentes**: muerto#5
- **Archivos**: Escenarios/{Pradera,Cementerio,Ciudad}/Halo*, Charco*, Resplandor* (12 .mat); Materiales/Calle.mat, New Material.mat, Plano.mat; Sprites/120-1207602_*.jpg, 120-1207626_*.jpg, Showbies.png; Assets/Assets.index; Editor/ConstructorEscenarios.cs:640-651

**Evidencia.** Un índice de guids con alcanzabilidad desde las 5 escenas del build, Resources y ProjectSettings deja 20 archivos propios sin referencias. Los 12 materiales de brillo los fabrica NeonDe, que crea halo, halo suave, charco y resplandor de cada color aunque el escenario no los use, y cada Armar los vuelve a dejar. Calle.mat es de las calles que se sacaron (795c027). Los dos corazones .jpg vienen de un sitio de stickers, con licencia desconocida. Assets.index es un índice de Unity Search de 2023.

**Arreglo.** Borrarlos, los corazones sobre todo. En NeonDe, crear cada material recién cuando se pide, o borrar al final de Armar los que quedaron sin referencia.

## H120 — Overrides y claves serializadas de campos que ya no existen (baja, calidad)

- **Fuentes**: muerto#6
- **Archivos**: Escenas/ShowBies1.unity:151, :159, :171, :187, :275, :323; Tutorial.unity (mismas líneas); WaveMode.unity:530, :538, :562, :650, :698, :2106-2169; Prefabs/Moneda.prefab:69; Prefabs/Personajes/ZombiBOSS.prefab:203

**Evidencia.** Las instancias de Jugador en las tres escenas pisan campos borrados: PlayerController.speed, circulo, textoContBalas y explosion; GunController.textoContBalas; PlayerHealth.AudioSource (por ejemplo speed: 12 al lado del moveSpeed: 15 que sí vale). Moneda guarda «notas» y ZombiBOSS «anchoLinea», dos campos borrados. WaveMode tiene un GeneradorZombis apagado que nadie referencia, con una configuración vieja del libre (crecimientoVida 1,15). Las tags Zombi y Terreno no las usa nada.

**Arreglo.** Remove Unused Overrides en las tres instancias de Jugador; reguardar Moneda y ZombiBOSS; borrar el GeneradorZombis de WaveMode y las dos tags; verificar los --- !u! contra HEAD.

## H121 — Paquetes sin uso en el manifest (baja, calidad)

- **Fuentes**: muerto#9
- **Archivos**: ShowBies1/Packages/manifest.json:4, :5, :8, :9, :11

**Evidencia.** No hay NavMesh (com.unity.ai.navigation) ni PlayableDirector o Timeline (com.unity.timeline); tampoco se usan com.unity.collab-proxy (el repo es git) ni com.unity.multiplayer.center, y com.unity.ide.vscode está deprecado (test-framework se queda: lo pide ide.visualstudio). Suman compilación, importación y ruido; CLAUDE.md ya sacó otros paquetes por lo mismo.

**Arreglo.** Sacarlos desde el Package Manager y comparar el tamaño del AAB y los warnings de la build antes y después.

## H122 — Terceros sin uso: ~19 MB y 205 archivos fuera de la build (baja, calidad)

- **Fuentes**: muerto#10
- **Archivos**: Assets/Thirdparty/Ciathyza/Gridbox Prototype Materials/ (demo y materiales HDRP/URP); Assets/TextMesh Pro/Documentation, Shaders/*.shadergraph; Assets/Joystick Pack/Examples, Documentaion.pdf; Assets/ToonyTinyPeople/TT_demo/*.tga, sample_scene

**Evidencia.** Nada de esto se alcanza desde el build. La demo de Gridbox pesa 640 KB y sus materiales HDRP y URP se ven rosas en built-in (el juego usa una copia propia de prototype_512x512_green2). Del Joystick Pack solo se usa Fixed Joystick. Las tres .tga de ToonyTiny suman 8,5 MB. Todo pesa en cada clon y reimport.

**Arreglo.** Borrar la demo y las carpetas HDRP/URP de Gridbox, los PDFs y los ejemplos del Joystick Pack; en ToonyTiny, solo lo que el índice confirme sin referencias (zombiRapido.FBX se queda como referencia de la trampa documentada).

## H123 — Las cuatro ventanas del menú y los doce bancos están copiados, y hay archivos para partir (baja, calidad)

- **Fuentes**: muerto#11
- **Archivos**: Scripts/UI/VentanaLogros.cs:206-239, :277-282; UI/VentanaMisiones.cs:187-228; UI/VentanaBestiario.cs:119-142; UI/VentanaRecompensaDiaria.cs:139-163, :234-240; Assets/Editor/PruebasMejoras.cs (4.578 líneas); Scripts/Zombi/EnemyController.cs:1115-1270

**Evidencia.** Abrir (rearmar por Idioma o Tema.Revision, SetAsLastSibling, escala en 0), Cerrar y Festejar están casi letra por letra en las cuatro ventanas, y ya divergen (solo Logros sube el pixelDragThreshold). El andamiaje de los doce bancos también está copiado, y por eso solo PruebaDiaria maneja el corte (H22). PruebasMejoras es una sola clase de 4.578 líneas; el festejo de la derrota son unas 200 líneas de EnemyController (1.315).

**Arreglo.** Una base VentanaDelMenu y un BancoEnPlay común (que resuelve H22 y H23 de una vez); PruebasMejoras en partial por subsistema; el festejo a un componente aparte.

---

## Ya conocidos (sacados)

- rendimiento#6: Las balas pueden atravesar al FASTER (suma un motivo de rendimiento al barrido ya propuesto, que saca el collider; no cambia el arreglo)
