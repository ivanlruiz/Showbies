# Plan a partir de la revisión de Gemini (armado el 10/10 para arrancar el 11/10)

Ivan le pasó a Gemini Pro el juego entero (descripción, capturas y el repo) y le gustó su plan. Este documento es ese
plan adaptado a ShowBies:

- **qué** hacemos de cada punto,
- **cómo** se hace acá (los archivos y sistemas que ya existen),
- **qué no tomamos** de Gemini y por qué,
- **cuánto lleva** y en qué versión sale,
- **qué tiene que decidir o hacer Ivan.**

Gemini no conoce las trampas de `CLAUDE.md`. Algunas de sus recetas (`PlayerPrefs`, Unity IAP, Shader Graph) no van
en este proyecto, y abajo dice por qué.

## Ya en marcha (hoy, sin commitear)

Los tirones del Redmi Note 8:

- **Hecho:**
  - **Interpolación del Rigidbody del jugador** (`Jugador.prefab`). El cuerpo se mueve con la física, a 50 Hz, y la
    pantalla va a otro ritmo; sin interpolar, el jugador (y con él toda la pantalla) avanzaba a saltitos.
  - **`CamaraJugador` en `LateUpdate` con orden -100.** Va antes de las flechas del borde, las barras y los halos, que
    se ubican contra ella.
  - **Máximo de paso de física de 0,333 a 0,1 s** (`TimeManager.asset`). Después de un tirón, Unity ya no encadena
    hasta 16 pasos de física para ponerse al día.
- **Falta:**
  - **Guardado en un hilo aparte**, solo el del principio de cada oleada y el del nivel del libre. Pausar, perder el
    foco, morir y lo demás siguen guardando en el momento.
  - **"Peor cuadro" en el contador de FPS:** "47 FPS · peor 120 ms · 3 saltos". Con eso Ivan puede decir si lo que
    siente es un temblor parejo (lo arreglado) o saltos sueltos, por ejemplo cuando aparece un tipo de zombi nuevo o
    en la primera explosión. Si son saltos, el siguiente paso es precalentar los zombis y los shaders.
- **Antes de commitear:** correr la prueba de lógica y el banco **Derrota encima de la partida**, que mide la cámara al
  morir.

---

## 1. El arranque flojo y el auto-apuntado

### 1a. El arranque flojo

**Gemini dice:** daño de 1 a 5, cadencia de 4 a 6, y que los zombis de la oleada 1 mueran de 2 tiros.

**En ShowBies** la base sale de los assets de las mejoras (`DanoBala` 1, `Cadencia` 4, `valorBase`). No hay un
`PlayerStats`. Arrancar flojo fue decisión de Ivan en la fase 4, después de jugarlo; no fue un descuido.

Opciones:

- **A (recomendada): que el poder lo den las cartas (punto 3) y subir solo la cadencia base de 4 a 6.**
  - Con las cartas por experiencia, el primer nivel de la partida llega a los ~20 s y el jugador se siente fuerte sin
    tocar la tienda.
  - La cadencia a 6 se nota en el acto y no toca la economía de los premios: `Economia.BalasPorPartida` no depende de
    la cadencia.
- **B: daño base 2 y cadencia 6** (el normal muere de 3 tiros en la oleada 1).
  - Cada compra de daño pasa de duplicar (1 → 2) a sumar un 50 % (2 → 3), y se lee menos.
  - Hay que rehacer los textos de las tarjetas, las pruebas y `BalasPorPartida`, que supone 5 balas por zombi.
- **C: lo de Gemini literal (daño 5).** La primera compra de daño sería +20 %: la tienda deja de sentirse al
  principio. No lo haría.

Cualquiera que se elija, antes de tocar se simula la curva de las primeras 10 partidas (monedas, compras y oleada a
la que se llega).

### 1b. Auto-apuntado (`AUTO-APUNTAR`)

- **Dónde va:** un interruptor en OPCIONES (el del menú) y en la pausa. Es una preferencia del teléfono, así que va en
  `PlayerPrefs`, como `MostrarFps`.
- **Cómo:** si está prendido y el pulgar no está en el joystick derecho, `PlayerJS` apunta al zombi vivo más cercano
  dentro de un radio que se ve en pantalla y dispara (`FijarDisparo`). El cuerpo ya gira hacia donde se apunta.
  - Tocar el joystick derecho sigue mandando: no se esconde, como dice Gemini. Se apunta a mano cuando uno quiere, por
    ejemplo al jefe o al tesoro.
  - Hace falta una **lista de zombis vivos**: hoy hay solo un contador (`ZombisVivos`) y la lista de jefes. Se lleva en
    `OnEnable` y `DejarDeContar`, como el contador. No se usa `OverlapSphere` cada cuadro.
- **Lo que toca de rebote:**
  - La granada sin arrastrar va hacia donde se venía apuntando (`ApuntoHaceMenosDe`), y con auto, hacia el blanco.
  - El paso "dispara" del tutorial se completaría solo; cambia su texto.
  - El zombi del tesoro y el jefe tienen que poder ser blanco.
- **En PC:** el mouse apunta solo, así que es solo para el teléfono.
- **¿Prendido de fábrica?** Decisión de Ivan. Gemini apunta al público casual: prendido para el que instala, y se
  apaga en OPCIONES.
- **Verificar:** banco nuevo en play (el que dispara solo, el joystick que lo pisa, la granada) y la prueba de lógica.

**Esfuerzo:** 1 día. **Versión:** 1.5.2.

---

## 2. El muro de la 45 y el renacer

### 2a. Suavizar la curva

**Gemini dice:** a partir de la oleada 20-25, la vida deja de crecer ×1,11 y pasa a ×1,03, o a crecer lineal.

**En ShowBies:**

- El daño comprable crece más o menos lineal con la oleada (es el logaritmo de unas monedas que crecen exponencial).
- La vida crece ×1,11 por oleada. Esa es la pared.
- Una vida que crezca ×1,03-1,05 desde la 25 la aleja mucho. El riesgo es que el juego se vuelva fácil para siempre
  si las monedas (×1,08) crecen más rápido que la vida.

**Cómo:** `WaveManager` con un segundo tramo (`crecimientoVidaTardio`, desde `oleadaDelSegundoTramo`). Antes, una
**simulación** como la del 19/9 (aquella llevó el muro de la 35 a la 45-48): oleada máxima alcanzable por cantidad de
partidas, con 1,03, 1,04 y 1,05. La que deje una curva "cada vez más lenta pero sin pared" gana.

**Esfuerzo:** medio día con la simulación. **Versión:** 1.5.2 o 1.6.0.

### 2b. El renacer

**Gemini dice:** un `PrestigeManager`, una moneda en `PlayerPrefs` y un botón al morir después de la oleada 30.

**En ShowBies:**

- **La moneda va en `Progreso`** (versión 8 del JSON, con su migración), no en `PlayerPrefs`: ahí está el respaldo, el
  `.anterior` y la regla de no pisar un progreso más nuevo.
- **Ya está pensada desde la fase 4:** "cerebros", calculados con `monedasGanadasJugando`. Ese contador existe para
  esto: los premios y los videos no cuentan.
- **Dónde:** una pestaña o botón RENACER en la tienda, que aparece desde la oleada 30. No en la derrota, que ya tiene
  la oferta de video y el próximo objetivo.
- **Qué reinicia:** las monedas y los niveles de las mejoras.
- **Qué se queda:**
  - La furia: está escrito que es permanente.
  - La granada. Decidir; recomiendo que se quede.
  - La mejor oleada, los logros, el nivel, el bestiario y el modo libre desbloqueado.
  - La mejor oleada sigue marcando los premios de `Economia`, así la vuelta arranca con premios grandes.
- **La tienda de cerebros:** pocas mejoras, multiplicativas:
  - daño ×(1 + 0,5 × nivel), la fórmula de Gemini;
  - monedas +20 % por nivel;
  - quizás una que haga empezar con algo comprado.
- **Antes de programar:** simular cuántos cerebros da una vuelta a la 30, la 40 y la 50, y cuántas vueltas lleva pasar
  la 60.

**Esfuerzo:** ~1 semana con la simulación y la UI. **Versión:** 1.8.0. Nadie llega todavía a la 45; va último.

---

## 3. Las cartas por experiencia (1.6.0, lo grande)

**Gemini dice:** cristales de experiencia que sueltan los zombis; al llenar la barra, pausa y 1 de 3 cartas; y cartas
que cambien las reglas, no solo números. Es un rediseño de `CARTAS.md`, que las daba cada 3 oleadas. Se reescribe ese
documento primero.

**En ShowBies:**

- **Los cristales:**
  - Son `Moneda`s con otro modelo, como los caramelos de Halloween. El pool es por prefab, el imán los trae y el
    vuelo y el piso ya están hechos.
  - Ojo con el techo de 150/80 monedas en escena, que es compartido: uno por zombi, con el valor escalado.
- **El nivel de la partida necesita otro nombre.** Ya existen el NIVEL del jugador y el "Nivel N" del modo libre.
  Propuesta: **PODER** ("¡PODER 4!") con una barra fina arriba. Decide Ivan.
- **La curva:** el primer nivel a los ~20 s y unas 12-15 cartas hasta la oleada 10. Se simula con los zombis por
  oleada del `WaveManager`.
- **La elección:**
  - Congela el juego como la oferta de revivir (`MenuPausa.JuegoCongelado`).
  - Tres tarjetas de carbón neón que entran girando, con `ConstructorUI`. Sin reloj; Escape no la cierra.
  - Si hay dos niveles seguidos, dos elecciones seguidas.
- **Retomar una oleada:** hoy arranca la oleada de cero con los puntos. Las cartas y el PODER se guardan en
  `Progreso.GuardarOleadaEnCurso` (versión 8); si no, retomar las pierde.
- **Las cartas** son ScriptableObjects en `Assets/Cartas/Resources`, como las mejoras, cada una con su tope.
  - **Las de números**, de `CARTAS.md`: daño, cadencia, crítico, vida, velocidad, imán, granada rápida y furia larga.
  - **Las que cambian las reglas** (las que pide Gemini):

    | carta | qué hace |
    |---|---|
    | Perforante | la bala sigue después de pegar, con la mitad de daño para el siguiente |
    | Rebote | la bala busca al zombi más cercano |
    | Disparo doble | dos balas en abanico |
    | Hacia atrás | una bala más por la espalda |
    | Explosiva | daño en área chico al pegar, con las chispas de `Efectos` |
    | Hielo | ralentiza 2 s; el zombi se tiñe de celeste |
    | Eléctrica | salta a un segundo zombi, con una línea como la del jefe |
    | Dron | una esfera que orbita y dispara sola |
    | Vampiro | cura al matar |
    | Escudo | bloquea un golpe cada tanto |

  - **Raras después del jefe:** una dorada que vale doble.
  - **Sin cartas de monedas**, como estaba decidido.
- **El modo libre:** también da cartas. Encaja con su "Nivel N" por tiempo, o se cambia por el PODER.
- **Rendimiento:** explosiones, rayos y drones salen de pools y de `Efectos`. Con 35 zombis en el teléfono se mide
  con el contador nuevo.
- **Verificar:** la prueba de lógica (la curva, los topes, el guardado al retomar) y un banco en play que elige cartas
  y mide su efecto.

**Esfuerzo:** 1-2 semanas. **Versión:** 1.6.0.

---

## 4. Menos ruido en la interfaz

### 4a. Mostrar las cosas de a poco

**Gemini dice:** esconder el bestiario y los logros hasta el nivel 5 (con `SetActive` y `PlayerPrefs`).

**En ShowBies:**

- Se esconden **el trofeo del bestiario, la lista de logros** (la ventana del nivel queda, porque el nivel sí sirve) y
  **la fila del desafío semanal**, hasta el **nivel 5 del jugador** (`NivelJugador`, en `Progreso`).
- **Los contadores siguen sumando**: al aparecer no se perdió nada, y las estrellas y medallas ganadas esperan para
  cobrarse.
- Al desbloquearse:
  - en la partida, `AvisoDeMisiones` muestra "¡NUEVO: BESTIARIO!";
  - en el menú, el botón entra con "¡NUEVO!", como el del modo libre.
- **Los avisos en la partida** (estrellas, logros) tampoco salen antes del nivel 5.
- **Verificar:** la prueba de lógica (lo que se ve con nivel 1, 4 y 5) y fotos del menú.

**Esfuerzo:** medio día. **Versión:** 1.5.2.

### 4b. La fuente

**Gemini dice:** "unificar las fuentes" (es todo Bangers) y usar una fuente limpia para botones y descripciones,
dejando la estilizada para los títulos.

- La primera parte no aplica.
- La segunda es una idea válida: Bangers en textos chicos (las misiones, las tarjetas, el HUD) se lee mal. Además Ivan
  ya lo tenía anotado en `TAREAS.md` ("cambiar la fuente").
- **Cómo:**
  - **Primero, maquetas:** las mismas pantallas con 2 o 3 fuentes libres (OFL) con tildes y ñ, por ejemplo Lilita One,
    Rubik Bold o Nunito Black.
  - **Después,** un SDF dinámico con texturas múltiples, como Bangers, y un constructor que la ponga en los textos
    chicos. Bangers queda para los títulos, los números de daño y los carteles grandes.
- **Esfuerzo:** 1-2 días. Toca muchas escenas y prefabs.

### 4c. El botón de monedas cada 10 minutos

**Gemini dice:** sacarlo y poner un cofre diario.

- **El cofre diario ya existe:** es la recompensa diaria con racha y su x2 por video.
- **El botón lo pidió Ivan el 10/10**, y es el lugar del menú que más videos puede dar.
- **Recomiendo dejarlo** y esconderlo hasta el nivel 5, con los demás. Hoy aparece desde los 3 minutos jugados.

---

## 4d. El bate: un golpe cuerpo a cuerpo (1.7.0)

Ivan preguntó si se podía (12/10, con Nex Machina de referencia de jugabilidad), y Gemini lo diseñó en "Análisis de diseño
para ShowBies":

- **Una herramienta defensiva para sacarse la horda de encima, no una fuente de daño**: el botón de pánico para cuando
  te acorralan.
- **Un botón** un poco más chico que el de la granada, arriba o a la izquierda del joystick derecho; en PC, el clic
  derecho o Shift. **Nada de golpe automático de cerca**: le saca al jugador la decisión de cuándo usar su salvavidas.
- **Los números**: daño = el de la bala × 3 (escala solo con la mejora de daño y las cartas); un cono de 120° y 2,5 m
  delante del jugador; **empuja 3 m** a los normales, los rápidos y los veloces, **al tanque lo frena 1 s** sin empujarlo y
  **al jefe no le hace nada**; enfriamiento de 4 s.
- **Una mejora de la tienda**, de compra única, como la granada y la furia: 1.500 monedas. Una carta no, porque aparecer y
  desaparecer botones en medio de la partida confunde.
- **Con la escopeta** es la red de seguridad cuando la horda entra a 2 m; **con el auto-apuntado**, el bate abre un hueco
  y el automático sigue limpiando mientras el jugador toma distancia.
- **Que no frene al jugador**: el golpe va de la cintura para arriba (una máscara en el Animator, como la del brazo que
  dispara, `BrazoDerecho`) y las piernas siguen corriendo. El pack trae un solo ataque (`m_melee_combat_attack_A`), la
  espera y el correr de pelea, y el bate que el muñeco tenía en la mano.
- **Después de la 1.6.0**: Gemini lo pone en la 1.7.0, con las compras. Las cartas y las armas van primero.

## 5. Compras dentro del juego (1.7.0)

**Gemini dice:** Unity IAP, la compra guardada en `PlayerPrefs` y el intersticial envuelto en un `if`.

**En ShowBies:**

- **Play Billing por JNI**, en una androidlib como la de AdMob, sin el plugin Unity IAP. El plugin trae el
  administrador de dependencias que ya se peleó con Unity 6; la reseña, las actualizaciones y AdMob ya van así.
- **La compra se guarda en `Progreso` y se recupera de Play al abrir** (`queryPurchases`). Así sobrevive a reinstalar,
  y `PlayerPrefs` no.

**Los productos:**

| producto | tipo | precio | qué da |
|---|---|---|---|
| `quitar_anuncios` | no consumible | US$ 2,99 | saca **solo los automáticos**; los videos con premio quedan |
| `paquete_inicio` | no consumible, se ofrece las primeras 48 h desde la instalación | US$ 1,99 | quitar anuncios + monedas + algo cosmético |

- **Las monedas del paquete de inicio:** Gemini dice 15.000. En la oleada 0 eso compra media tienda; se calibra con
  `Economia` (por ejemplo, 5 partidas de monedas a la mejor oleada, con un piso).
- **El cosmético:** un color de bala exclusivo o un sombrero, como el de calabaza.
- **Dónde se ofrece:**
  - un botón QUITAR ANUNCIOS en OPCIONES;
  - el paquete, en la tienda y en la derrota durante las 48 h, con su cuenta atrás;
  - nunca en la partida.
- **Lo que hace Ivan en Play Console (conviene arrancarlo ya, porque Google tarda en verificar):**
  - el **perfil de pagos** (cuenta de comerciante) y los **datos fiscales**;
  - crear los dos productos con sus precios;
  - actualizar **Seguridad de los datos** (historial de compras);
  - sumar **testers de licencias** para probar sin pagar.
- **Verificar:** la prueba de lógica con un proveedor falso (comprado, restaurado, cancelado y pendiente), como la de
  los anuncios, y una compra de prueba en el teléfono.

**Esfuerzo:** ~3 días de código.

---

## 6. Lo visual y la ficha

### 6a. Rim light en los zombis

**Gemini dice:** Shader Graph con Fresnel. Eso es de URP; ShowBies usa el render Built-in.

- **Cómo:** el borde va en `ShowBies/PielDeZombi`: un término de fresnel sumado a la emisión, en la misma pasada (sin
  contorno de 2 pasadas, que duplica el skinning).
- **Costo:** unas pocas cuentas por píxel.
- **Maquetas:** con `FotosDeLaHorda` (de noche, en los tres capítulos, con la calidad del teléfono), probando el borde
  del color de la piel, blanco o celeste. La prueba mide el contraste contra el piso.

**Esfuerzo:** medio día. **Versión:** 1.5.2.

### 6b. El héroe

**Gemini dice:** cambiarle el material a uno brillante (blanco o amarillo).

- **El problema:** el modelo usa una textura atlas compartida (`TT_demo.mat`, que no se toca). Hay que hacer una copia
  de la textura con la remera repintada (PIL sobre la isla de UV de la remera) y un material propio.
- **Cada color choca con algo:**
  - el amarillo, con las monedas, el tesoro y las balas;
  - el blanco, con el destello del golpe;
  - el naranja, con las calabazas.
- **Maquetas:** 3 colores, con un **anillo neón bajo el héroe** y sin él. El anillo es lo más seguro.

**Esfuerzo:** 1 día. **Versión:** 1.5.2.

### 6c. Sombras falsas

No está en este mensaje de Gemini; salió en la revisión de arte. Un disco oscuro bajo cada personaje **en lugar de las
sombras reales** en Android: se ve igual o mejor y le saca al Redmi la pasada de sombras (Halloween suma ~900 piezas
que proyectan). Va con las maquetas de 6a.

**Versión:** 1.5.2.

### 6d. La ficha

- **Las capturas con textos cortos ya están** (`Builds/ficha_final_corta`, español e inglés). Se pueden acortar más,
  a 1 o 2 palabras ("¡SOBREVIVE!", "¡JEFES ÉPICOS!"). Se suben con el OK de Ivan.
- **Falta el gráfico destacado** de 1024 × 500: Nano Banana Pro con el logo. Argon lo vio el 14/10: rehacerlo, uno
  solo permanente, el héroe y la horda en la ciudad de noche y solo el logo del juego (pedido y detalle en
  `publicacion/grafico_de_funciones.md`).
- **El texto de la ficha:** reescribirlo para búsquedas ("zombie shooter offline", "survivor", "horde"), en
  `publicacion/ficha.md`, y cargarlo con su OK.
- **Clips de 15-20 s** (furia, jefe, horda muriendo en cadena) para TikTok y Shorts, con un banco que graba como
  `GrabarDisparo`. Los publica Ivan.

---

## 7. El camino de nivel (1.8.0, con el renacer)

Ivan preguntó (13/10) si se podía hacer algo con el nivel del jugador, "tipo pase de batalla". Lo decidió Argon (Gemini en
el razonamiento máximo, en el chat del análisis de diseño), en dos vueltas: la segunda corrigió tres premios que chocaban
con lo que el juego ya decidió.

- **No un pase por temporadas**: obliga a hacer premios nuevos cada mes, reiniciar el nivel frustra, y cuando una
  temporada termina sin otra lista el juego parece abandonado. **Un camino permanente de 50 niveles**, como el Trophy
  Road de Brawl Stars: se programa una vez. No se reinicia nunca, tampoco al renacer: es el progreso de la cuenta.
- **La curva queda** (100L − 75: el 50 cerca de la partida 71). Del 51 en adelante, un casillero sin fin que paga el
  15 % de una partida por nivel, como hoy.
- **Los niveles comunes** (los 40 que no son múltiplos de 5) pagan el 15 % de siempre. **Los hitos** (cada 5) pagan el
  triple y **un cosmético permanente**, con **x2 de las monedas por video**; quien compró quitar anuncios o el paquete de
  inicio de la 1.7.0 cobra el x2 sin video. No hay un pase pago aparte.
- **Conviven**: los logros quedan como están (sus niveles de golpe pasan a ser el atajo a los cosméticos), y los eventos
  siguen siendo filas cortas de 5 premios en su botón.
- **Cuatro ranuras, una cosa por ranura**: cabeza, aura, cuerpo del arma y remera. **La cabeza es una sola**: el sombrero de
  calabaza pasa a ser una opción más (`AccesorioCabeza`: ninguno, calabaza, aureola, cuernos, corona), y apilar
  atravesaría polígonos. El sombrero ganado sale como una casilla de "Evento" al principio del camino, y el interruptor
  de OPCIONES se va.
- **Ningún cosmético toca lo que se lee jugando**: el color de cada tipo de zombi ni el de la bala (el tramo del daño).
  Por eso no van las hordas teñidas ni una estela de color que Argon proponía primero.

| nivel | premio (más el triple de monedas) | ranura |
|---|---|---|
| 5 | el menú completo (lo que hoy destapa `Desbloqueo.Todo`) | — |
| 10 | aura celeste | aura |
| 15 | remera amarillo flúor | remera |
| 20 | aureola de neón | cabeza |
| 25 | arma de oro: el cuerpo dorado, con la luz del tramo intacta | arma |
| 30 | aura violeta | aura |
| 35 | cuernos de neón (dos conos `Unlit`) | cabeza |
| 40 | arma blanca, con la luz del tramo intacta | arma |
| 45 | marco dorado de la píldora del nivel | menú |
| 50 | corona de neón y aura dorada | cabeza y aura |

- **En el menú, sin otro botón**: la píldora del nivel (arriba a la izquierda) abre el camino, una fila horizontal centrada
  en el nivel actual, hecha como la de Halloween; late y lleva la insignia roja cuando hay premios por cobrar. En la ventana
  de los logros, el cobro del nivel pasa a ser "VER CAMINO DE NIVEL". En la casilla de un cosmético ya cobrado, COBRAR
  pasa a ser USAR / EN USO: no hay inventario aparte.
- **Va en la 1.8.0, con el renacer**: el camino es lo que sigue subiendo cuando el renacer devuelve las monedas y las
  mejoras a cero.
- **Para decidir al hacerlo**: Argon pone el renacer en el hito del 30, y este plan lo destapaba al llegar a la oleada 30.

## Orden propuesto

| cuándo | qué | versión |
|---|---|---|
| **Día 1 (11/10)** | terminar los tirones (guardado, peor cuadro), auto-apuntar, esconder hasta el nivel 5, maquetas del héroe, el rim light y las sombras → APK de prueba para el Redmi | 1.5.2 |
| Día 2 | lo que Ivan elija de las maquetas, la curva suavizada (con simulación), el arranque (1a) → AAB | 1.5.2 |
| Semana 1-2 | reescribir `CARTAS.md` y hacer las cartas por experiencia | 1.6.0 |
| En paralelo | Ivan arranca el perfil de pagos; gráfico destacado; clips | — |
| Después | compras | 1.7.0 |
| Después | renacer (con simulación) y el camino de nivel (7) | 1.8.0 |
| Cuando toque | la fuente secundaria (maquetas primero) | cualquiera |
| **Después del 9/11** | volver al ícono de base (va en la versión que salga en esas fechas) | — |

## Lo que tiene que decidir Ivan

| # | decisión | lo que recomiendo |
|---|---|---|
| 1 | El arranque (1a): A, B o C | **A**: las cartas y la cadencia base en 6 |
| 2 | AUTO-APUNTAR prendido de fábrica | sí, para el que instala; se apaga en OPCIONES |
| 3 | Cartas por experiencia o cada 3 oleadas | **por experiencia** |
| 4 | El nombre del nivel de la partida | **PODER** |
| 5 | Qué se esconde hasta el nivel 5 | bestiario, lista de logros, desafío semanal y el botón de 10 minutos |
| 6 | El intersticial: desde la 3.ª o la 5.ª partida | lo que prefieras: la 5.ª cuida al nuevo, la 3.ª da más anuncios |
| 7 | Precios: quitar anuncios y paquete de inicio | **2,99 y 1,99**, con las monedas calibradas |
| 8 | La moneda del renacer y si la granada se queda | **cerebros**; la granada se queda |
| 9 | La fuente para textos chicos | ver maquetas primero |
| 10 | Color del héroe | ver maquetas primero |
