# Auditoría: inglés por defecto, con opción de español

Estado al 2026-09-17. Nada de esto está hecho todavía: es el inventario, la propuesta y lo que hay que decidir
antes de arrancar.

**Lo pedido:** el juego arranca **en inglés** la primera vez (y en cualquier build nueva), y el jugador puede
**cambiar a español** desde el juego. La elección queda guardada.

---

## 1. Resumen

| | |
|---|---|
| Textos distintos a traducir | **~90** (sale de 95 apariciones en escenas y prefabs + ~30 armados por código, con los repetidos juntos) |
| Dónde viven hoy | 5 escenas, 5 prefabs, 11 scripts y los 6 assets de mejoras |
| Textos que **no** se traducen | los del anuncio de prueba, el `MedidorBalance`, los logs, y los que son sólo símbolos (`SHOWBIES`, `COMBO`, `FPS`, `G`, `II`) |
| Formato de números | **hardcodeado en español** (`1.234`, `4,5 M`): en inglés tiene que ser `1,234` y `4.5M` |
| Riesgo de que no entre | **bajo**: el español suele ser más largo que el inglés, y los layouts ya están medidos para el español |
| Cosas que aparecieron de paso | el tutorial quedó afuera de la pasada de UI; `MM` (mil millones) en inglés es `B`; y el español mezcla voseo con tuteo |

---

## 2. Cómo lo propongo

### Por qué no el paquete de Unity (Localization)

Unity tiene un paquete oficial, y es el camino para un juego con muchos idiomas, audio doblado o textos que se
bajan de un servidor. Para ShowBies es demasiado: **depende de Addressables**, que agrega un paso a cada build y
peso a la APK, y trae un editor de tablas propio para ~90 textos en dos idiomas. Un sistema chico hace lo mismo
en tres archivos, y si algún día hacen falta diez idiomas, los textos ya van a estar separados y migrar es copiar
la tabla.

### Las piezas

| pieza | qué hace |
|---|---|
| `Resources/Textos.tsv` | **La tabla**: una fila por texto, columnas `id`, `en`, `es`. Se abre con cualquier planilla, se le puede pasar a un traductor, y sumar un idioma es agregar una columna. |
| `Idioma` (static) | El idioma actual, `Cambiar(idioma)` y un `Revision` que sube al cambiar (igual que `Progreso.Revision`: nadie se suscribe a nada, cada pantalla compara). **Arranca en inglés.** Se guarda en `PlayerPrefs["Idioma"]`, que es una preferencia del dispositivo y no progreso. |
| `Textos` (static) | `Textos.De("menu_jugar")` y `Textos.Formato("hud_oleada", n)`. Un id que falta se ve como `[menu_jugar]` y deja un aviso, así un olvido salta a la vista en vez de dejar un texto vacío. |
| `TextoTraducido` (componente) | Va al lado de cada `TMP_Text` fijo de escenas y prefabs, con su `id`. Escribe el texto al prenderse y cuando cambia el idioma. |
| Selector | Ver decisión 1. |

### Casos que necesitan cuidado

- **Plantillas con números.** Casi nada es una frase suelta: `"¡Te alcanza para {0} mejoras!"`,
  `"Oleada {0}\n<size=75%>Zombis {1}/{2}</size>"`. Lo que se traduce es la plantilla entera, con su rich text. La
  prueba de la tabla verifica que **cada idioma tenga los mismos `{0}`, `{1}`** que los demás: un `{1}` que falta
  en inglés tira una excepción en plena partida.
- **Singular y plural.** Ya hay dos casos resueltos a mano (`formatoAvisoUna` / `formatoAviso`). Se mantiene
  así, con dos ids: en inglés y en español alcanza con "1 o más de 1".
- **Los números.** `FormatoNumeros` arma los separadores a mano (a propósito: la cultura `es-AR` no está en todas
  las builds). Pasa a elegir punto y coma según el idioma: `1.234` / `1,234`, `4,5 M` / `4.5M`, `MM` / `B`.
- **Las mejoras.** El nombre y la unidad hoy son campos de cada `.asset`. No hace falta tocarlos: los ids salen
  del `id` de la mejora (`mejora_dano_bala_nombre`), que ya es fijo para siempre porque es la clave de los
  niveles guardados.
- **Los textos de relleno de las escenas** (`"Score:"`, `"Wave 1 "`, `"+0 monedas (total 0)"`) los pisa el código
  en el primer frame, pero ese frame se ve. Se vacían.
- **Las pruebas.** `PruebasMejoras` verifica hoy textos y números en español (`"1.234"`, los textos de las
  tarjetas). Van a fijar el idioma al empezar y sumar los casos en inglés.

---

## 3. Inventario, con la traducción propuesta

La columna de inglés es **una propuesta para que la revises**: el tono de los textos importa y lo decidís vos.
Donde marqué ⚠ hay algo para decidir (ver sección 5).

### Menú y modos

| id | español | inglés |
|---|---|---|
| `menu_jugar` | JUGAR | PLAY |
| `menu_mejoras` | MEJORAS | UPGRADES |
| `menu_modos` | MODOS DE JUEGO | GAME MODES |
| `menu_tutorial` | TUTORIAL | TUTORIAL |
| `menu_salir` | SALIR | QUIT |
| `modo_libre` | MODO LIBRE | FREE MODE ⚠ |
| `modo_oleadas` | OLEADAS | WAVES |
| `comun_volver` | VOLVER | BACK |

### Tienda

| id | español | inglés |
|---|---|---|
| `tienda_titulo` | MEJORAS | UPGRADES |
| `tienda_jugar` | ¡A JUGAR! | PLAY! |
| `tienda_pie_mejor_oleada` | Mejor oleada: {0} | Best wave: {0} |
| `tienda_pie_sin_oleadas` | ¡Jugá las oleadas para ganar monedas! | Play the waves to earn coins! |
| `tienda_pie_aplican` | Las mejoras se aplican al empezar cada partida | Upgrades apply at the start of each run |
| `tienda_sin_catalogo` | No se encontró el catálogo de mejoras | Upgrade catalog not found |
| `tienda_racha` | ¡x{0}! | x{0}! |
| `tarjeta_nivel` | NIVEL {0} | LEVEL {0} |
| `tarjeta_nivel_tope` | NIVEL {0}/{1} | LEVEL {0}/{1} |
| `tarjeta_max` | MÁX | MAX |
| `tarjeta_maximo` | ¡MÁXIMO! | MAXED! |
| `tarjeta_faltan` | faltan {0} | need {0} |

### Las seis mejoras

| mejora | nombre (es / en) | unidad (es / en) |
|---|---|---|
| `dano_bala` | DAÑO DE BALA / BULLET DAMAGE | daño por bala / damage per bullet |
| `cadencia` | CADENCIA / FIRE RATE | tiros por segundo / shots per second |
| `vida_maxima` | VIDA MÁXIMA / MAX HEALTH | vida al empezar / starting health |
| `iman` | IMÁN / MAGNET | metros de alcance / meters of reach |
| `botin` | BOTÍN / LOOT | monedas por zombi / coins per zombie |
| `furia` | FURIA / RAGE ⚠ | segundos de furia / seconds of rage |

### HUD y partida

| id | español | inglés |
|---|---|---|
| `hud_monedas` | MONEDAS | COINS |
| `hud_puntos` | PUNTOS | SCORE |
| `hud_nivel` | Nivel {0} | Level {0} |
| `hud_oleada` | Oleada {0} | Wave {0} |
| `hud_zombis` | Zombis {0}/{1} | Zombies {0}/{1} |
| `hud_reiniciar_pc` | R  REINICIAR | R  RESTART |
| `cartel_bono` | +{0} monedas por la oleada {1} | +{0} coins for wave {1} |
| `furia_boton` | FURIA | RAGE ⚠ |
| `furia_cartel` | ¡FURIA! | RAGE! ⚠ |

### Pausa

| id | español | inglés |
|---|---|---|
| `pausa_titulo` | PAUSA | PAUSED |
| `pausa_continuar` | CONTINUAR | RESUME |
| `pausa_reiniciar` | REINICIAR | RESTART |
| `pausa_menu` | MENÚ PRINCIPAL | MAIN MENU |

### Derrota

| id | español | inglés |
|---|---|---|
| `derrota_titulo` | PERDISTE | GAME OVER |
| `derrota_puntos` | PUNTOS | SCORE |
| `derrota_record` | RÉCORD | BEST |
| `derrota_record_nuevo` | ¡NUEVO RÉCORD! | NEW BEST! |
| `derrota_monedas` | +{0} MONEDAS | +{0} COINS |
| `derrota_total` | TOTAL {0} | TOTAL {0} |
| `derrota_otra_vez` | OTRA VEZ | PLAY AGAIN |
| `derrota_menu` | MENÚ | MENU |
| `aviso_compras_una` | ¡Te alcanza para una mejora! | You can afford an upgrade! |
| `aviso_compras_varias` | ¡Te alcanza para {0} mejoras! | You can afford {0} upgrades! |
| `aviso_descanso` | Llevás más de una hora jugando. Un descanso viene bien. | You've been playing for over an hour. Maybe take a break? |
| `oferta_duplicar` | VER VIDEO: +{0} MONEDAS | WATCH VIDEO: +{0} COINS |

### Revivir

| id | español | inglés |
|---|---|---|
| `revivir_titulo` | ¡HAS MUERTO! ⚠ | YOU DIED! |
| `revivir_bajada` | Mirá un video y seguí jugando | Watch a video to keep playing |
| `revivir_no` | NO, GRACIAS | NO, THANKS |

### Tutorial (cada paso tiene versión de PC y de teléfono)

| id | español | inglés |
|---|---|---|
| `tut_mover_pc` | Movete con W, A, S y D. | Move with W, A, S and D. |
| `tut_mover_movil` | Movete con el joystick de la izquierda. | Move with the left joystick. |
| `tut_disparar_pc` | Apuntá con el mouse y mantené el click izquierdo para disparar.\n¡Viene un zombi! | Aim with the mouse and hold left click to shoot.\nA zombie is coming! |
| `tut_disparar_movil` | Apuntá y dispará con el joystick de la derecha.\n¡Viene un zombi! | Aim and shoot with the right joystick.\nA zombie is coming! |
| `tut_granada_pc` | Cuando vengan varios juntos, mantené ESPACIO para ver dónde cae la granada (apuntás con el mouse) y soltalo para tirarla. | When several come at once, hold SPACE to see where the grenade lands (aim with the mouse) and let go to throw it. |
| `tut_granada_movil` | Cuando vengan varios juntos, arrastrá el botón G para apuntar la granada y soltalo para tirarla. | When several come at once, drag the G button to aim the grenade and let go to throw it. |
| `tut_cajas` | Cada tanto aparecen cajas: la de balas recarga el cargador y la de vida te cura.\nAgarrá una. | Crates show up now and then: ammo refills your magazine and health heals you.\nGrab one. |
| `tut_caja_arma` | La caja de arma mejora el arma: cargador más grande y dispara mucho más rápido.\nAgarrala. | The weapon crate upgrades your gun: a bigger magazine and a much faster fire rate.\nGrab it. |
| `tut_reloj` | Mirá el reloj arriba a la derecha: la cadencia mejorada dura unos segundos.\nEl cargador más grande, en cambio, queda para siempre. | See the timer at the top right? The faster fire rate only lasts a few seconds.\nThe bigger magazine is yours to keep. |
| `tut_fin_titulo` | ¡Eso es todo! Ya sabés jugar. | That's it! You know how to play. |
| `tut_fin_jugar` | Jugar | PLAY |
| `tut_fin_menu` | Menú | MENU |

### No se traducen

- **El anuncio de prueba** (`ProveedorFalso`): sólo existe en la APK de prueba, nunca llega a Play.
- **`MedidorBalance`**: sólo en el editor y en builds de desarrollo.
- **Los logs** (`Debug.Log`): son para nosotros.
- **Símbolos y nombres propios**: `SHOWBIES`, `COMBO x{0}`, `{0} FPS`, `G`, `II`, `(F)`.

---

## 4. Lo que apareció de paso

1. **El tutorial quedó afuera de la pasada de UI.** Todavía dice `Press "R" to Restart` y `Puntos:`, y sus botones
   finales ("Jugar" y "Menú") son texto suelto, sin el molde de los demás. Hay que hacerlo igual, y conviene
   hacerlo en la misma pasada.
2. **`MM` no existe en inglés.** En español `MM` es "mil millones"; en inglés es `B` (billion). Va en
   `FormatoNumeros`.
3. **El español mezcla voseo y tuteo.** Casi todo está en voseo rioplatense ("Mirá", "Jugá", "Llevás",
   "Movete"), pero la ventanita dice "¡HAS MUERTO!", que es de España. Ver decisión 3.

---

## 5. Lo que tenés que decidir

1. **Dónde va el selector.** Mi recomendación: **un botón chico en una esquina del menú principal que muestre el
   *otro* idioma escrito en ese idioma** — con el juego en inglés dice `ESPAÑOL`, en español dice `ENGLISH`. Es la
   convención por una razón: el que no entiende inglés tiene que poder encontrarlo en la primera pantalla sin
   leer nada en inglés, y "ESPAÑOL" lo reconoce cualquiera. No agrega un sexto botón grande (lo que no te gustó
   del VIDEOS). Las alternativas son banderitas (más lindo, pero una bandera es un país y no un idioma: ¿España o
   Argentina?) o una pantalla de opciones, que es más trabajo pero serviría también para volumen y para el
   interruptor de videos.
2. **La primera vez, ¿siempre inglés?** Vos dijiste que sí, y así lo planteo. La otra opción es mirar el idioma
   del teléfono solo en el primer arranque: un teléfono en español arranca en español y cualquier otro en inglés.
   Es un cambio de una línea y se puede decidir después.
3. **¿Voseo o español neutro?** El juego hoy habla como argentino. Si el público va a ser toda Latinoamérica y
   España, el neutro ("Mira", "Juega", "Te alcanza") llega mejor; si es sobre todo Argentina, el voseo tiene más
   personalidad. Sea cual sea, **"¡HAS MUERTO!" hay que cambiarlo**: con voseo sería "¡MORISTE!".
4. **Nombres en inglés** marcados con ⚠:
   - **FURIA → RAGE o FURY.** RAGE es lo que usa Idle Slayer, que fue la inspiración; FURY suena más a nombre de
     habilidad. Da lo mismo para el código.
   - **MODO LIBRE → FREE MODE o ENDLESS.** ENDLESS es como lo busca un jugador de habla inglesa; FREE MODE es
     como se llama en el código y en la ficha que ya escribimos.

---

## 6. Plan

Cinco partes, un commit cada una, en este orden porque cada una se apoya en la anterior:

1. **La base.** `Idioma`, `Textos`, `TextoTraducido`, la tabla con todos los ids de arriba, `FormatoNumeros` según
   el idioma, y las pruebas: que cada id tenga los dos idiomas, que los `{0}` coincidan entre idiomas, y los
   números en los dos formatos. Además un ítem **ShowBies > Idioma** en el editor, para probar sin jugar.
2. **Escenas y prefabs.** `TextoTraducido` en cada texto fijo (con un script de editor, no a mano), los textos de
   relleno vaciados, y **el tutorial vestido** como el resto.
3. **Lo que arma el código.** Los once scripts pasan a `Textos.Formato`, incluidas las mejoras.
4. **El selector** en el menú, guardado en `PlayerPrefs`, y que al cambiar se refresque todo lo que está en
   pantalla sin recargar.
5. **Verificación.** Fotos de las cinco pantallas en los dos idiomas (menú, tienda, partida, derrota, revivir) y
   medición de que ningún texto se salga de su caja, igual que con la derrota. Y CLAUDE.md: la sección de
   pantallas deja de decir "todo en español" y pasa a explicar cómo se agrega un texto.

Lo único que **no** puedo hacer sin vos es la decisión 1: sin saber dónde va el selector, las partes 1 a 3 se
pueden hacer igual, pero la 4 no.
