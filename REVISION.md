# Revision de ShowBies con diez agentes

Hecha el 22/9/2026. Diez agentes leyeron el codigo por dimension (progreso, economia de
premios, zombis, jugador, anuncios, interfaz, rendimiento, escenas, idiomas y producto), y
cada hallazgo paso por un verificador aparte que trato de refutarlo leyendo el codigo.

**De 47 hallazgos, 32 sobrevivieron la verificacion y 15 se descartaron.**
48 agentes, 7,2 millones de tokens, 24 minutos.

---

# Revisión de ShowBies — informe

## Si sólo vas a hacer una cosa hoy

**ARREGLADO (22/9).** Ya están el revivir y los tres exploits de economía (el tope de vídeos, la
recompensa diaria y los objetivos de misiones), cada uno con su prueba de regresión en
**ShowBies > Pruebas > Logica de mejoras**, verificada volviendo a poner el bug. Lo que sigue es el informe
como salió.

Arreglá **el revivir**: cerrar el vídeo (o que la red falle al mostrarlo) manda a la pantalla de derrota en el acto, sin devolver la ventanita ni los segundos que quedaban. Es lo único de esta lista que le rompe la partida al jugador en el momento en que le pedís que mire un anuncio, contradice la regla escrita de "cerrar el vídeo antes no castiga" y el arreglo es separar un callback. Con el proveedor en Nulo hoy no se ve en Play, pero la APK de prueba ya lo tiene y la red real lo va a agravar.

Si te sobra un rato después, las dos líneas de `health = Mathf.Max(0, ...)` y el tope de vídeos por día (que hoy es 3 por lugar, o sea 9) son arreglos de minutos.

---

## BUGS

### 1. Cerrar el vídeo del revivir manda a la derrota en el acto — **ARREGLADO**
**`Assets/Scripts/Anuncios/OfertaDeRevivir.cs:276`** — media

El callback "sin premio" que se le pasa a `ServicioAnuncios.Mostrar` es `Rechazar`, el mismo método del botón NO, GRACIAS: llama a `PlayerHealth.Terminar()` y carga la derrota. Así `Cerrado`, `NoDisponible` y una `FallaAlMostrar` no premiable terminan la partida. Además el reloj queda congelado por `esperandoVideo`, así que los segundos que le quedaban al jugador se pierden sin correr.

**Se dispara:** con el proveedor Falso (el de la APK de prueba), morir con la oferta disponible, tocar el vídeo en el segundo 1 y tocar SALTEAR. Con red real es peor: `Listo` puede dar true y el `Mostrar` fallar, así que el jugador toca "ver vídeo para revivir", no ve nada y aparece en la derrota.

**Arreglo:** separar el callback sin premio de `Rechazar`: `esperandoVideo = false`, volver a poner `interactable` los dos botones, correr `desde += (Time.unscaledTime - momentoEnQueSeAcepto)` para que la cuenta atrás siga donde quedó, y llamar a `Rechazar` sólo si el tiempo restante ya es ≤ 0.

**Contradice CLAUDE.md:** la sección de Anuncios dice "cerrar el vídeo antes no castiga". Es cierto para los topes, pero este lugar sí castiga: pierde la ventana de decisión. Los otros dos (`OfertaDeDuplicar`, `VentanaRecompensaDiaria`) sí cumplen.

---

### 2. El tope de 3 vídeos por día se aplica por lugar, no en total — **ARREGLADO**
**`Assets/Scripts/Anuncios/ServicioAnuncios.cs:144`** — media

`PuedeOfrecer` compara `config.vecesPorDia` (3) contra `Progreso.UsosDeHoy(lugar)`, que cuenta sólo ese lugar. El tope real es 3 revivir + 3 duplicar_derrota + 3 regalo_x2 = hasta 9 vídeos por día. `vecesPorPartida = 1` sólo limita revivir+duplicar dentro de una partida, no por día.

**Se dispara:** revivir con vídeo en tres partidas seguidas y después morir sin revivir en otras tres y cobrar el x2: los tres x2 se siguen ofreciendo porque `UsosDeHoy("duplicar_derrota")` arranca en 0.

**Arreglo:** un `Progreso.UsosDeHoyEnTotal()` que recorra `datos.anuncios.usos` y pasarlo a `PuedeOfrecerConDatos` en vez de `UsosDeHoy(lugar)`. Dejar el por-lugar por si algún día hace falta, y sumar un caso a `PruebasMejoras` que verifique que gastar el tope en un lugar cierra los otros.

**Contradice CLAUDE.md:** "hasta 3 veces por día" está escrito como regla global, y es la que hace que la monetización no se sienta agresiva.

---

### 3. La vida del HUD se queda en negativo durante la oferta de revivir
**`Assets/Scripts/Jugador/PlayerHealth.cs:157`** — baja

`health -= dano;` no clampea a 0. Cuando el golpe que mata saca más de lo que quedaba (la carga del jefe pega `daño` × multiplicador × 2,5), `health` queda negativo, y si hay vídeo la partida no termina: `Update` sigue corriendo con `timeScale` 0 y el número grande de abajo muestra "-17" en rojo durante los 10 s en que le pedís que decida.

**Se dispara:** morir de un golpe en oleadas altas con el proveedor en Falso y las condiciones de `revivir` cumplidas.

**Arreglo:** `health = Mathf.Max(0, health - dano);`. El `if (health > 0)` de abajo sigue igual.

---

### 4. La misión de granadas se cumple sin matar un zombi y cuesta menos de la mitad de lo que paga — **ARREGLADO**
**`Assets/Scripts/Progreso/MisionesDiarias.cs:164`** — media

`Progreso.ContarGranada()` cuenta cada lanzamiento, pegue o no, y el único freno es `granadaCooldown` = 5 s, que no escala. El objetivo difícil es `Redondo(2,5 · m · 1,2)`: 120 granadas en la oleada 40 = 600 s de reloj, contra una partida que llega ahí que dura como mínimo 1.400 s. Coste real: 0,43 partidas contra las 2,5 que se le cobran al premio. Desde la oleada ~10, tirar granadas al aire rinde más monedas por segundo que jugar bien (83,9 vs 24,3 en m=40).

**Se dispara:** con la granada comprada, entrar a oleadas y lanzarla en cuanto se recarga, sin apuntar a nada.

**Arreglo:** que el objetivo salga de la duración estimada de la partida y no del número de oleada: `partidas * Economia.ZombisPorPartida(m) * 0,35 / granadaCooldown`. Alternativas: no darla como difícil, o contar sólo las granadas que dañan a algún zombi.

Nota: `granadaDisponibleEn` es un campo de instancia que arranca en 0 mientras `Time.time` no se reinicia al cargar escena, así que morir o reiniciar regala una granada inmediata.

---

### 5. La misión de críticos está desacoplada de lo que cuesta — **ARREGLADO**
**`Assets/Scripts/Progreso/MisionesDiarias.cs:165`** — media

El objetivo es `partidas * 60 * (1 + m/10)`: crece ×3,75 en toda la curva, mientras el premio crece ×315. `ContarCritico()` se llama por bala crítica, y las balas por partida crecen con la vida total (exponencial) dividida por el daño comprable (logarítmico). El pago por crítico pasa de 0,8 monedas (m=3) a 67 (m=40). Y no mira el nivel de la mejora: con nivel 1 (5 %) y mejor oleada 3 salen ~7 críticos por partida contra un objetivo de 200.

**Se dispara:** el lado caro, con críticos recién comprados. El lado barato, retomando la partida guardada ("CONTINUE WAVE N") en la oleada 39-40: una sola oleada son ~1.500 impactos contra un objetivo de 750. (Desde cero no se cumple en dos oleadas, como decía el reporte original: hacen falta unas 20.)

**Arreglo:** atarlo a `Economia.ZombisPorPartida(m)` por balas-por-zombi y por la probabilidad de crítico de `CatalogoMejoras`, en vez de `60 * (1 + m/10)`. Y sumar a `ProbarPremiosDeMisiones` una prueba que compare el coste estimado de **cada** tipo, en partidas, contra `Partidas(dificultad)`: hoy sólo verifica que el objetivo crezca con la oleada, y por eso ni granadas ni críticos se ven.

---

### 6. La recompensa diaria es la única que resuelve el monto al cobrarla — **ARREGLADO**
**`Assets/Scripts/Progreso/RecompensaDiaria.cs:102`** — baja

`CobrarEl` usa `Monto(racha, Progreso.MejorOleada)` con la marca del momento del cobro. Misiones y desafío semanal congelan `mejorOleadaAlArmar` justamente para que postergar no sea la jugada óptima. Acá sí se puede postergar: el atrás de Android (y Escape en PC) cierra la ventana sin cobrar y `VentanaRecompensaDiaria.Start` la vuelve a abrir la próxima vez que se carga el menú ese día.

**Se dispara:** con la diaria disponible y mejorOleada 20, cerrar sin cobrar, jugar hasta la 25, volver al menú: de 8.650 a 15.700 monedas.

**Arreglo:** anotar la mejor oleada la primera vez que la recompensa queda disponible ese día (campo al lado de `diaRecompensa`/`rachaRecompensa`, centinela −1) y pagar con esa. Es el mismo arreglo que ya está anotado en TAREAS para el bestiario: quedan los cuatro sistemas con la misma regla.

---

### 7. La misión diaria del jefe dice "Derrota a un jefe" pero pide 2 o más
**`Assets/Scripts/Progreso/MisionesDiarias.cs:296`** — media

`Descripcion` es el único caso que ignora `mision.objetivo`: devuelve `Textos.De("mision_jefe")` (fila 51: "Defeat a boss" / "Derrota a un jefe", singular, sin `{0}`). Pero `Objetivo` devuelve `Max(1, Round(2,5 · m / 10))` con `m ≥ 9`: mínimo 2, y 10 en la oleada 40. El `Math.Max(1, …)` es letra muerta.

**Se dispara:** siempre que sale esa misión. La fila muestra "Derrota a un jefe" con el contador "0 / 2", y en la derrota `ProximoObjetivo` arma "PRÓXIMO: Derrota a un jefe 1/2".

**Arreglo:** cambiar la fila `mision_jefe` a plantilla con `{0}` ("Defeat {0} bosses" / "Derrota {0} jefes", como `semanal_jefes`, que ya existe y está bien) y usar `Textos.Formato("mision_jefe", n)`.

---

### 8. En móvil el jugador nunca reproduce la animación de disparo
**`Assets/Scripts/Jugador/PlayerJS.cs:68`** — media

El parámetro `"shoot"` del Animator sólo se escribe en `PlayerController.HandleShooting` (líneas 198 y 204), y ese método no corre nunca en móvil (`PlayerController.Update` hace `if (Plataforma.EsMovil) return;` en la 86). `PlayerJS.UpdateShootJoystick` prende `thegun.isFiring` pero no toca el Animator. El controller `TT_demo_male_A` sí tiene el parámetro, el estado `m_pistol_shoot` y sus transiciones.

**Se dispara:** con el target en Android, mover el joystick de disparo: salen balas y el arma rota, pero el muñeco se queda en `m_pistol_idle_A`.

**Arreglo:** sacar esas dos líneas de `HandleShooting` a un `PlayerController.FijarDisparo(bool)` que prenda `theGun.isFiring` y `trans.anim.SetBool("shoot", …)` juntos, y que lo llamen las dos plataformas.

Detalle: en el controller la transición a `m_pistol_shoot` sale sólo de `m_pistol_idle_A`, así que en PC tampoco se ve corriendo. Si querés que se vea siempre, hace falta además la transición desde `m_pistol_run`.

---

### 9. En PC, apretar disparo sin balas y agarrar una caja sin soltar no dispara
**`Assets/Scripts/Jugador/PlayerController.cs:196`** — baja

`theGun.isFiring` sólo se prende en el frame del `GetMouseButtonDown` y sólo si `cantBalas > 0`. En móvil no pasa: `PlayerJS` reevalúa `thegun.isFiring = player.cantBalas > 0` en cada frame. Las dos plataformas se comportan distinto.

**Se dispara:** vaciar el cargador manteniendo click, soltar, volver a apretar y mantener con 0 balas, pisar una caja PUBalas: el contador salta a 500 y no sale ninguna bala hasta soltar.

**Arreglo:** sacar la condición `cantBalas > 0` del `GetMouseButtonDown` y dejar que `GunController.Update` decida (ya chequea `isFiring && player.cantBalas > 0`). Si se unifica con el arreglo de la animación, sale gratis.

---

### 10. El cierre de medianoche paga las misiones al precio de la oleada 0
**`Assets/Scripts/Progreso/MisionesDiarias.cs:108`** — baja

`CerrarElDia` usa `Math.Max(0, estado.mejorOleadaAlArmar)`, y el centinela −1 sólo se completa en la rama de "mismo día" de `Asegurar` (línea 87), que corre **después** de `CerrarElDia` (línea 91). Si el progreso trae misiones de un build anterior al campo y la primera vez que corre `Asegurar` ya cambió el día, las cumplidas sin cobrar se auto-cobran al mínimo de toda la curva, y no avisa en pantalla.

**Se dispara:** progreso del 19/9 (el `EstadoMisiones` de entonces no tenía el campo), con la difícil cumplida y sin cobrar y mejorOleada 45: paga ~160 monedas en vez de ~77.600.

**Arreglo:** completar el centinela antes de cerrar: al principio de `Asegurar`, `if (estado.dia != 0 && estado.mejorOleadaAlArmar < 0) estado.mejorOleadaAlArmar = Progreso.MejorOleada;`, y recién después decidir si hay que cerrar. `DesafioSemanal` tiene el mismo patrón pero no está afectado.

Alcance real: sólo archivos escritos en una ventana de ~un día de builds de desarrollo, y el estado se auto-repara al siguiente `Asegurar`. Es barato dejarlo cerrado igual.

---

### 11. En Android los faroles del cementerio y la ciudad no alumbran
**`Assets/Editor/ConstructorEscenarios.cs`** (bloque de faroles del cementerio, líneas 101-116, y `Farol()` en 294) — media

Las 10 luces de los decorados (4 + 6) son Point con `renderMode` en Auto (`m_RenderMode: 0` en los dos prefabs). El proyecto renderiza en forward y Android usa Medium, con `pixelLightCount: 1`; la direccional siempre se lleva ese lugar, así que las 10 puntuales caen a luz por vértice. El piso es el Plane built-in (121 vértices) escalado ×10: un vértice cada 10 m, donde la luz por vértice no se ve. En el teléfono, en los capítulos de noche el suelo bajo cada farol queda igual de oscuro; lo único que se ve son los cuadros emisivos.

**Se dispara:** armar la APK y jugar la oleada 11 o la 21. En el editor con target Windows (Ultra, `pixelLightCount: 4`) se ve bien — es exactamente la trampa que ya está escrita en CLAUDE.md.

**Arreglo (elegí uno):** (a) subir `pixelLightCount` de Medium a 2-3 y medir FPS con 35 zombis; (b) pintar el charco de luz con un quad emisivo plano bajo cada farol, que es gratis y se ve igual en las dos calidades; (c) `renderMode = ForceVertex` y subir la teselación del piso. En cualquier caso, probarlo en el teléfono.

**Contradice CLAUDE.md:** la sección de capítulos describe los faroles como que alumbran ("cuatro faroles con luz cálida", "faroles con luz naranja"). En Android, hoy, no. Y el trabajo de limitar a 6 luces (`MaxLuces`) no compra nada, porque igual no se dibujan por píxel.

---

### 12. El cartel de misión cumplida y el de capítulo se pisan
**`Assets/Scripts/UI/AvisoDeMisiones.cs:127`** — baja

`AvisoDeMisiones.Mostrar` pone su cartel en `(0, 300)` y `CapitulosDeEscenario.MostrarCartel` (línea 355) el suyo en `(0, 385)`, los dos anclados al centro del mismo Canvas (el del HUD, fileID 1970546277 en WaveMode). Los centros están a 85 px y los dos bloques suman bastante más: se solapan ~40-46 px, o sea "LA CIUDAD" cae encima de "¡MISIÓN CUMPLIDA!". No depende de la resolución.

**Se dispara:** completar la oleada 10, 20 o 30 con la misión de "completa N oleadas" a medias. `MisionesDiarias.RegistrarOleada` corre en el mismo frame que el `OleadaActual++`, así que la coincidencia es esperable.

**Arreglo:** un único renglón para avisos transitorios: bajar el de misión a y ≈ 180-200, o que `AvisoDeMisiones` espere a que `CapitulosDeEscenario` no tenga cartel antes de sacar el suyo de `pendientes` (la cola ya existe). De paso revisar la franja de `BarraDelJefe` (`desdeArriba` 100 + margen 42), que en 16:9 queda entre 319 y 398 y también la pisa el cartel de misión.

---

### 13. El globo del idioma (y sus tres copias) es el único botón de vidrio del menú sin `PintarConTema`
**`Assets/Escenas/Menu.unity:7334`** — baja

De los cinco Image con el color de vidrio (0.06, 0.12, 0.05, 0.45), cuatro llevan `PintarConTema` con rol `Vidrio` y el del globo (`AreaSeguraMenu/BotonIdioma/Visual/Fondo`) no. Como el engranaje, misiones y bestiario son copias del globo hechas con `Instantiate`, los cuatro botones redondos de las esquinas heredan la falta: con el tema oscuro quedan discos verde oscuro sobre el fondo nocturno (contraste ~1,03 contra el cielo), sólo se ve el icono.

**Se dispara:** Menú > engranaje > MODO OSCURO, sin cerrar la ventana: se ve cómo QUIT/MEJORAS/VOLVER cambian y los cuatro de la esquina no.

**Arreglo:** agregarle `PintarConTema` con rol `Vidrio` al `Visual/Fondo` de `BotonIdioma` en Menu.unity (el `colorClaro` se completa solo). Las tres copias lo heredan sin tocar código.

**Contradice CLAUDE.md:** la sección de Tema dice que los botones de vidrio llevan `PintarConTema` y que lo exento es "lo que tiene color propio". El globo no tiene color propio: es un olvido.

---

### 14. El valor siguiente de la tarjeta de mejora no sigue el tema
**`Assets/Scripts/Tienda/TarjetaMejora.cs:218`** — baja (bajada de media: no rompe contraste)

`Refrescar()` escribe `valorSiguiente.color = colorValorSiguiente` en cada refresco, con el verde del prefab (0.18, 0.6, 0.12), que es bit a bit el mismo `colorClaro` que la Flecha lleva en su `PintarConTema` con rol `Acento`. `ValorSiguiente` es el único texto del prefab sin `PintarConTema`, y aunque se lo pusieran el script lo pisaría. En oscuro la flecha pasa a verde claro (8,58:1) y el número de al lado se queda en el verde oscuro (3,93:1): dos verdes que nacieron iguales y se separan.

**Se dispara:** MODO OSCURO > MEJORAS, cualquier tarjeta que no esté en tope.

**Arreglo:** `valorSiguiente.color = Tema.Elegir(colorValorSiguiente, RolDeTema.Acento);`. Opcional: un caso en `ProbarTema` que compruebe que `colorValorSiguiente` y el `colorClaro` de Flecha siguen siendo el mismo color.

Nota: no falla `ProbarTema` (que sólo mide rol contra rol) y a 56 px cuenta como texto grande, umbral 3,0, que 3,93 pasa. Es consistencia visual, no accesibilidad.

---

### 15. Al terminar de invocar, el jefe pega un salto de rotación
**`Assets/Scripts/Zombi/JefePatrones.cs:261`** — baja

`Terminar()` siempre escribe `transform.rotation = Quaternion.Euler(0f, rumboAlAturdirse, 0f)`, pero ese campo sólo se carga en `Aturdir()`, que es el final de la **carga**. `Terminar()` también se llama desde `AvisandoInvocar` (línea 206), donde el campo conserva el rumbo del final de la embestida anterior (~9 s antes). El error dura hasta el `FixedUpdate` siguiente, donde `EnemyController` vuelve a hacer `LookAt`: uno o dos frames.

**Se dispara:** oleada 10 de WaveMode, en el frame exacto en que salen los invocados. Se nota más cuanto más giró el jugador alrededor del jefe entre la carga y la invocación.

**Arreglo:** mover ese `transform.rotation` al caso `Aturdido` del `Update` (que es donde hace falta deshacer el balanceo en Z), o directamente no tocar la rotación al terminar: la persecución ya la reencara. De paso, reiniciar `rumboAlAturdirse` y `rotacionAlAturdirse` en `OnEnable`.

(La parte de "con el pool arrastra el valor de la aparición anterior" no es alcanzable: `OnEnable` pone `tocaCarga = true` y sólo `Terminar()` lo invierte.)

---

### 16. `Economia.ZombisPorPartida` le faltan 2m zombis
**`Assets/Scripts/Progreso/Economia.cs:17`** — baja

El comentario dice "la oleada n saca 10 + 4n, y esto es la suma hasta m". Esa suma es Σ(10+4n) para n=1..m = **2m² + 12m**, pero la función devuelve `10.0 * m + 2.0 * m * m` = 2m² + 10m. Con m=3: real 54, devuelto 48. Con m=45: 4.590 vs 4.500.

**Arreglo:** `return 12.0 * m + 2.0 * m * m;`. Y un caso en `PruebasMejoras` que compare la función contra la suma hecha a mano de `zombisBase + zombisPorOleada * n`, para que no se desincronice si tocás esos dos campos del WaveManager.

Impacto real: mucho menor de lo que parece. La vara es aproximada a propósito y su aproximación dominante es otra (`MonedasPorPartida` toma `1,08^(m/2)` en vez de sumar oleada por oleada, lo que en la oleada 45 subestima ~3×), todos los consumidores la usan de forma auto-consistente y `Redondo` se come parte. Es un desajuste comentario/constante, no un sesgo con síntoma jugable.

---

## MEJORAS

### 1. `Progreso.Leer` se traga cualquier excepción sin loguear
**`Assets/Scripts/Progreso/Progreso.cs:733`** — media

`Leer` es literalmente `catch (Exception) { return null; }` — ni captura la excepción — y devuelve el mismo null para "no existe", "no se pudo leer" y "JSON roto". `Cargar` usa ese null para tres caminos distintos y la rama que termina en `leidos = new Datos { version = VersionActual }` no escribe nada, mientras todas las demás ramas del archivo sí avisan (Guardar, Respaldar, versión futura, carpeta de pruebas). El único evento que le cuesta al jugador todo su progreso es el único que no deja una línea en logcat.

**Se dispara:** truncar `progreso.json` y abrir el juego: arranca de cero y la consola queda vacía.

**Arreglo:** capturar la excepción en `Leer` y `Debug.LogWarning` con ruta y mensaje; y un `LogWarning` en `Cargar` cuando se arranca de cero habiendo encontrado archivo, distinguiéndolo de "no había archivo". Una línea cada uno.

Atenuante: el peor caso está parcialmente cubierto, porque antes del primer `Guardar` se hace `Respaldar(ruta, ruta + ".roto")`. La pérdida total sólo ocurre si ya existía un `.roto` previo (`Respaldar` tiene `if (!File.Exists(destino))` a propósito). El hueco de diagnóstico sigue entero.

---

### 2. Las chispas de la boca del arma dependen del AudioSource
**`Assets/Scripts/Armas/GunController.cs:234`** — baja

`Efectos.Disparo(firePoint.position)` está dentro del `if (Time.time >= proximoSonido && AudioSource != null && AudioSource.clip != null)`, que es el techo del **sonido**. Dos consecuencias: si el AudioSource falta o queda sin clip no hay chispas (y no está en `Gun.prefab`: las tres escenas lo agregan a mano); y las chispas quedan topeadas a 25/s aunque se disparen hasta 120 balas por segundo.

**Arreglo:** mover `Efectos.Disparo(...)` al cuerpo de `Disparar()`. Si querés un techo para las chispas con cadencia alta, que sea uno propio.

---

### 3. La barra del jefe aloca un string por frame
**`Assets/Scripts/UI/BarraDelJefe.cs:143`** — baja

`Update` hace `nombre.text = Nombre(actual)` en cada frame con jefe vivo, y `Nombre` termina en `zombi.enemyType.name`: el getter `UnityEngine.Object.name` arma un string managed nuevo en cada llamada. Son ~40 bytes por frame durante toda la pelea del jefe, que es el momento con más zombis, balas y números flotantes. `EnemyController` ya lo evita con `nombreTipo` cacheado en `Awake`, con ese comentario exacto.

**Arreglo:** `Elegir()` ya detecta cuándo cambia el jefe: escribir `nombre.text` ahí y guardarlo en un campo. O exponer el `nombreTipo` que `EnemyController` ya cachea.

---

### 4. Las corrutinas que generan crean un `WaitForSeconds` por vuelta
**`Assets/Scripts/Zombi/WaveManager.cs:145`**, `GeneradorZombis.cs:157`, `PowerUp.cs:34` — baja

Los tres bucles alocan un `WaitForSeconds` por iteración con un intervalo que nunca cambia (verificado: nadie escribe `intervaloEntreApariciones` en runtime, y los cinco de `GeneradorZombis` entran por valor). En `GeneradorZombis` los `continue` del techo de población y del jefe vivo están **después** del yield, así que aloca aunque no spawnee nada.

**Arreglo:** crear la espera una vez antes del `while`. En `GeneradorZombis.spawnEnemy` y `PowerUp.spawnPU` alcanza una variable local; en `WaveManager`, un campo armado en `Start`. Lo mismo en `CapitulosDeEscenario.cs:194`, aunque ese corre una vez por capítulo.

Los propios comentarios de esas funciones dicen "un while hace lo mismo sin alocar": el `new` quedó adentro del while por olvido.

---

### 5. El cementerio (605 objetos) se instancia a los 6 s de la oleada 1
**`Assets/Scripts/Escenario/CapitulosDeEscenario.cs:170`** — media

`PrepararElSiguiente` se lanza en **todo** cambio de capítulo, incluido el primero (en la oleada 1 `capitulo` pasa de −1 a 0), así que seis segundos después hace `Instantiate` de Cementerio.prefab: 605 GameObjects con 456 MeshRenderer en un frame, más el recorrido de jerarquía. Toda partida de oleadas —incluidas las que mueren en la 2 o la 3, que son la mayoría— paga ese tirón mientras el jugador pelea la primera oleada, en el teléfono, y arrastra el `Cementerio(Clone)` apagado toda la partida. (El prefab tiene `m_IsActive: 1`, así que se instancia activo y recién después se apaga.)

**Arreglo:** atar la preparación al final del capítulo: esperar a que `oleadas.OleadaActual` llegue a `(capitulo + 1) * oleadasPorCapitulo - 2` (oleada 9 para el cementerio, 19 para la ciudad) en vez de un `WaitForSeconds(6f)` desde el principio. Dos oleadas de aire sobran.

CLAUDE.md documenta `PrepararElSiguiente` como decisión tomada, pero el motivo escrito es evitar el tirón **en el frame del cambio**; en el capítulo 1 no hay cambio por diez oleadas, así que ese motivo no cubre este caso.

---

## SUGERENCIAS

### 1. Morir en oleadas te devuelve a la oleada 1
**`Assets/Scripts/Jugador/PlayerHealth.cs:190`** — alta

Al morir, `Terminar()` llama a `Progreso.OlvidarOleadaEnCurso()` y OTRA VEZ arranca en la 1. Volver a la oleada 25 son ≥9,6 minutos, a la 35 ~17,6 y a la 40 ~22,4, contando **sólo** el tiempo en que los zombis aparecen. Nada de eso es contenido nuevo. Y el atajo existe pero es invisible: salir al menú **sí** conserva la oleada y el botón dice "SIGUE EN LA OLEADA N", o sea que la jugada óptima es salir justo antes de morir. Es el candidato número uno a desinstalación en la segunda o tercera sesión, porque el castigo crece con lo que el jugador invirtió.

**Arreglo:** punto de control cada 10 oleadas, que ya coincide con los capítulos: al morir, guardar el último múltiplo de 10 alcanzado (11, 21, 31…). Alternativa más visible: un selector "EMPEZAR EN LA OLEADA N" con los capítulos desbloqueados. Lo mínimo, si querés dejar la regla: un renglón en la pausa que diga que salir conserva la oleada, para que dejar de jugar no sea una trampa.

---

### 2. La pantalla de derrota no dice a qué oleada llegaste
**`Assets/Scripts/UI/highscoretext.cs:18`** — alta

La derrota muestra GAME OVER, monedas, SCORE y BEST (puntos). La oleada no aparece en ningún lado: en `Perdiste.unity` no hay ningún `TextoTraducido` de oleada ni existe el id. Pero la oleada es la métrica de todo el resto del juego — desbloquea el modo libre, define los capítulos, escala misiones, bestiario, semanal y diaria, decide la reseña — y sólo se le muestra al jugador en una línea chica del pie de la tienda. El número que contaría y le contaría a un amigo no está en el momento en que se lo entregás; en su lugar hay un "SCORE 1.480" que el juego nunca explicó.

**Arreglo:** en oleadas, que el número grande sea "OLEADA 17" y BEST la mejor oleada; los puntos bajan a un renglón chico. Dos ids nuevos y leer `Progreso.MejorOleada` en vez de PlayerPrefs. De paso deja lista la función de compartir que ya está en TAREAS: "Llegué a la oleada 21" se comparte, "1.480 puntos" no.

---

### 3. Las tres primeras capturas de la ficha de Play son menús
**`Builds/ficha/v5/1_premio_diario.png`** — alta

Las 7 capturas de la v5 empiezan con recompensa diaria, menú y tienda. En Play la primera es la del resultado de búsqueda y las dos primeras son lo único que se ve sin deslizar: hoy lo primero que alguien ve de un twin-stick shooter es un cartel de "DAILY REWARD / 5-DAY STREAK", que además es el lenguaje visual de los juegos que la gente desinstala por pesados. Y en esa captura el logo sale medio tragado por la niebla (`TituloEnLaNiebla`), que fuera de contexto se lee como error de render. La buena ya existe: `4_endless` (horda alrededor, COMBO x67, monedas, crítico en rojo).

**Arreglo:** reordenar en Play Console sin tocar el juego: `4_endless`, `5_furia`, `7_jefe`, y después tienda, menú y premio diario. Con un titular corto encima de cada una (ARRASA CON LA HORDA / DESATA LA FURIA / DERROTA AL JEFE), que es lo estándar en la categoría. Media hora y afecta directo a cuánta gente instala.

---

### 4. No hay notificaciones locales
**`Packages/manifest.json`** — alta

Hay tres sistemas construidos enteros para que el jugador vuelva —racha de 7 días que se corta si se saltea uno, misiones que cambian a medianoche, desafío de lunes a domingo— y ninguna forma de avisarle: no está `com.unity.mobile.notifications` y no hay una línea de notificaciones en todo `Assets/Scripts`. La racha de 7 días, la mecánica de retención más cara de construir de las que están hechas, la va a completar sólo quien ya abriría el juego solo. Es la mejor relación impacto/esfuerzo que queda: el trabajo pesado ya está hecho, falta el aviso.

**Arreglo:** agregar el paquete y programar al cerrar la app una notificación local para el día siguiente ("Tu recompensa del día 4 te espera: +1.440 monedas"), cancelándola al cobrar. Es local: no recopila ni transmite nada, así que no cambia el formulario de Seguridad de los datos ni la línea de "no necesita internet". Lo único que suma es POST_NOTIFICATIONS, que en Android 13+ se pide en runtime; conviene pedirlo después de la primera partida terminada, no al abrir.

---

### 5. Quedarse sin balas no avisa de ninguna manera
**`Assets/Scripts/Armas/Balas.cs:25`** — media

Con `cantBalas` en 0 el arma se apaga sin sonido, sin clic en vacío, sin temblor y sin flash, y el HUD sigue escribiendo "0 /500" con el mismo color y tamaño. No es raro: el cargador es de 500 y con la cadencia al tope (20 tiros/s) se vacía en 25 s, mientras una oleada de la 20 tarda 31 s sólo en terminar de aparecer. El jugador rodeado que deja de disparar sin explicación lo lee como que el juego se trabó: es el tipo de momento que termina en una reseña de una estrella que dice "se cuelga".

**Arreglo:** tres cosas chicas con el jugo que ya existe: el número del HUD en rojo y latiendo por debajo del 20 %, un clic seco más una sacudida del número al querer disparar en vacío (`Sonidos.Tocar` + `BotonJugoso.Sacudir`), y si se puede una flecha al borde hacia la caja de balas más cercana. Lo más barato de esta lista y evita el peor malentendido posible.

---

### 6. La granada y la furia aparecen como botones mudos
**`Assets/Scripts/UI/JoystickGranada.cs:37`** — media

Al comprar la granada (250) o la furia (5.000, el objeto más caro), el botón simplemente se hace visible y nadie dice qué hace ni cómo se usa. La granada es lo peor: la "G" parece un botón normal pero es un joystick (arrastrar apunta, soltar tira, volver al centro cancela), y el único texto que lo explica es `tut_granada_movil`, que vive en el Tutorial, al que el jugador nuevo nunca entra porque `MainMenu.TocarJugar` lo manda derecho a la oleada 1.

**Arreglo:** reusar `GuiaPrimeraPartida`: en la primera partida después de comprar cada una, un dedo fantasma sobre el botón con un cartel de una línea ("ARRASTRA PARA APUNTAR" / "ÚSALA CUANDO ESTÉS RODEADO"), que se va al usarlo. Se decide con `Progreso.Nivel("granada")`/`"furia"` más el contador de usos que ya está en las estadísticas de por vida: no hace falta marca nueva.

---

### 7. Dos capturas muestran un campo vacío con el contador de FPS
**`Builds/ficha/v5/6_oleada_10.png`** — media

`6_oleada_10.png` está sacada durante el descanso entre oleadas: campo verde liso, el jugador solo, "ZOMBIES 0/51". La captura que debería mostrar la oleada más grande no muestra un zombi. `7_jefe.png` tiene al jefe sobre el mismo campo vacío y sin la barra grande (es del 18/9; `BarraDelJefe` es del 20/9). En las dos se lee "60 FPS": `ContadorFps` está con `m_IsActive: 1` en las dos escenas, o sea que lo ve cualquier jugador en release. Y ninguna de las siete muestra el cementerio, la ciudad, la barra del jefe, las misiones, el bestiario ni el modo oscuro: la ficha sigue vendiendo un juego de un solo escenario verde justo cuando el argumento más fuerte pasó a ser que el escenario cambia cada 10 oleadas.

**Arreglo:** al armar el AAB de la 6, sacar la tanda de nuevo (jefe con barra, oleada llena, cementerio, ciudad, misiones, modo oscuro). Y antes, esconder el contador de FPS: `Debug.isDebugBuild` como ya hace `MedidorBalance`, o un interruptor en OPCIONES.

---

### 8. Tres tratamientos distintos del nombre: icono, banner y juego
**`Assets/Sprites/Icono/IconoApp.png`** — media

El icono es un render crudo de la cabeza del zombi de ToonyTinyPeople sobre un degradado naranja: sin marca, sin arma, y con un modelo del Asset Store que aparece en decenas de juegos. El banner escribe SHOWBIES en Bangers amarillo plano. Y el juego usa el bueno: `LogoShowBies.png` (el de `Marketing/logo.py`, con arco, extrusión, contorno doble, bisel y el mordisco en la H), el único de los tres que se lee como marca y que está hecho para leerse en chico. En la grilla de Play el icono es lo único que se ve a 48 px: es el activo con más peso de la ficha y el único que no recibió trabajo de diseño.

**Arreglo:** poner el logo de `logo.py` en el banner (está hecho para eso) y rehacer el icono con una silueta propia que funcione a 48 px: el zombi bien recortado con el fogonazo o la moneda, contorno grueso, el mismo dorado/naranja del logo, para que icono, banner y menú se lean como la misma cosa. Sin texto en el icono.

---

### 9. "Completa N oleadas" se farmea repitiendo la oleada 1
**`Assets/Scripts/Progreso/MisionesDiarias.cs:202`** — baja

Desde el arreglo del 20/9, `oleadasDelDia` (y `oleadasDeLaSemana`) cuenta una oleada completada, sea cual sea. Eso tapó el agujero de retomar una partida avanzada y abrió otro: REINICIAR y la R llaman a `OlvidarPartidaSiEsOleadas`, que hace arrancar de la 1. Completar la oleada 1 son 14 zombis, ~25 s con el cartel y la recarga de escena. Con la mejor oleada 40 la difícil pide 100 oleadas: 42 min de repetir la 1 contra ≥75 min reales; el semanal pide 600 (4,2 h contra ~9 h). No es enorme (2-3×) pero es repetible, mecánico y paga el premio más alto del día.

**Arreglo:** ponderar la oleada por lo que costó: sumar `oleada / max(1, mejorOleada)` acotado a 1 en vez de 1 fijo, o contar sólo las que llegan a alguna fracción de la mejor marca. Sigue sin premiar retomar una guardada y deja de premiar repetir la más barata.

---

### 10. El botón MODO LIBRE bloqueado no dice por dónde va el jugador
**`Assets/Scripts/UI/BotonModoLibre.cs:58`** — baja

Bloqueado dice "LLEGA A LA OLEADA 12" sin decir en cuál va. Como la mejor oleada tampoco se ve en el menú ni en la derrota (sugerencia 2), alguien que va por la 9 no sabe si le falta una tarde o una semana, y el desbloqueo es el objetivo de mediano plazo más importante: es la mitad de lo que promete la ficha ("DOS FORMAS DE SOBREVIVIR").

**Arreglo:** sumar el avance al mismo texto ("LLEGA A LA OLEADA 12 · VAS POR LA 9") o una barrita 9/12 debajo. Es un `Textos.Formato` con `Progreso.MejorOleada + 1` en el `Pintar()` que ya existe.

---

## Dónde la documentación quedó vieja

- **`CLAUDE.md` § Anuncios — "cerrar el vídeo antes no castiga"**: cierto para los topes, falso para el revivir (bug 1). O se arregla el código o se acota la frase.
- **`CLAUDE.md` § Anuncios — "hasta 3 veces por día"**: hoy son 3 por lugar, o sea hasta 9 (bug 2).
- **`CLAUDE.md` § Capítulos — los faroles "con luz cálida" / "con luz naranja"**: en Android no alumbran el piso (bug 11). Y la nota de que Android corre Medium y el editor Ultra está escrita justo al lado, sin conectarse con esto.
- **`CLAUDE.md` § Tema — "los botones de vidrio llevan `PintarConTema`"**: el globo y sus tres copias no (bug 13).
- **`CLAUDE.md` § Misiones — "derrotar N jefes"**: el cálculo dice N, el texto en pantalla dice "un jefe" (bug 7). La documentación tiene razón y el texto no.
- **`Economia.cs:17`**: el comentario describe una suma que la constante no calcula (bug 16); `CLAUDE.md:546` repite la misma descripción.
- **`CLAUDE.md` § Capítulos — `PrepararElSiguiente`**: está documentado el porqué (no instanciar en el frame del cambio), pero no cubre el caso del capítulo 1, donde no hay cambio por diez oleadas (mejora 5).