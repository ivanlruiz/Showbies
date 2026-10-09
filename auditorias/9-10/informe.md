**En números: se encontraron 11 errores, 1 se descartó y quedan 10 (7 confirmados y 3 plausibles). No quedó ninguno sin verificar.**

# Revisión de ShowBies (rama halloween, 1.5.0)

Ivan: esto es lo que salió de la revisión de los 10 agentes. Primero los errores, de más grave a menos grave, después las 8 mejoras que más rinden por lo que cuestan y al final lo que haría primero. Ninguno de los errores rompe una partida. Los tres de gravedad media tocan la plata de los anuncios, la invocación del jefe en el cementerio y el evento de Halloween, que arranca en dos semanas.

---

## 1. Errores

### Gravedad media

**1. Se ofrecen y se muestran anuncios cargados hace horas** (confirmado)
`ShowBies1/Assets/Plugins/Android/ShowBiesAnuncios.androidlib/src/main/java/com/ivanruiz/showbies/anuncios/PuenteAnuncios.java:385`
- **Qué pasa:** alguien juega a la noche, bloquea el teléfono con el juego en segundo plano y lo abre a la mañana. El x2 de la diaria, el revivir o el automático salen como "listos" y muestran un anuncio cargado hace 8 o 10 horas, cuando la vida de un anuncio es una hora. Google no paga esa impresión, o el anuncio falla al mostrarse. Si falla, el primero del día regala el premio sin que se mire nada, y desde el segundo el botón se apaga.
- **Por qué:** el vencimiento usa `postDelayed`, y ese reloj no corre mientras el teléfono duerme. Al mostrar, nadie mira cuánto tiene el anuncio.
- **Arreglo:** guardar `SystemClock.elapsedRealtime()` en los dos `onAdLoaded`. En `mostrarYa` y `mostrarIntersticial`, si pasó la hora, descartarlo, avisar "vencido" y "terminado no_disponible" y volver a cargar. Lo mejor es sumar un `revisarVencidos()` que C# llame cuando la app recupera el foco, así ni siquiera se ofrece. El `postDelayed` queda de respaldo.

**2. En el cementerio, los zombis que invoca el jefe nacen adentro de su cuerpo** (confirmado)
`ShowBies1/Assets/Scripts/Zombi/JefePatrones.cs:846`
- **Qué pasa:** en la oleada 20, el jefe en furia parado entre las tumbas invoca, y alguno de los 6 zombis nace a menos de 1 m de su centro, adentro de la cápsula del jefe. La física lo escupe de golpe, casi siempre contra el piso. Los que caen por el kill-Z cuentan como muertos y no dan monedas. Se ve como zombis que salen disparados, o la invocación se pierde.
- **Por qué:** `RadioLibre` tira un rayo que choca con las lápidas y las cruces que el jefe pisa (`IgnoreCollision` no afecta a los rayos) y achica el radio a 0,1-0,8 m. Darlo vuelta no sirve, porque del otro lado también hay una tumba.
- **Arreglo:** en `RadioLibre`, saltear los colliders de `CapitulosDeEscenario.Chicos`. Además, como red de seguridad, que `PuntoDelAnillo` nunca devuelva un radio menor que `radioDelCuerpo + 0,5`. El banco de tumbas usa un jefe sin patrones, así que no lo detecta: conviene sumar el caso.

**3. Atrasar el reloj reabre Halloween cerrado y hasta reinicia una fila ya cobrada** (confirmado)
`ShowBies1/Assets/Scripts/Evento/EventoHalloween.cs:139`
- **Qué pasa:** pasado el 9/11, alguien pone la fecha del teléfono en el 5/11 y el evento vuelve entero: puede juntar caramelos y cobrar los hitos que le faltaban, sombrero incluido. Con la fecha en octubre del año anterior y después de vuelta en la de hoy, la fila del año en curso arranca de cero y se cobra otra vez, todas las veces que quiera. En la diaria, las misiones y el semanal esta trampa está cerrada. Acá no.
- **Arreglo:** en `Asegurar`, no arrancar nunca una edición menor que la guardada. En `CerrarSiTermino`, no cerrar por esa razón. En `Sumar` y `CobrarSiguiente`, salir si la edición está cerrada. Opcional: guardar el último día visto y usar el máximo, para que tampoco se reabra dentro de las fechas. Sumar los dos casos a la prueba de lógica.

### Gravedad baja, confirmados

**4. En el teléfono se sigue disparando mientras el salto del jefe te tira por el aire** (confirmado)
`ShowBies1/Assets/Scripts/Jugador/PlayerJS.cs:36`
- **Qué pasa:** el golpe del jefe te despide y, si tenés el pulgar en el joystick de disparo, el muñeco sigue girando y disparando en pleno vuelo, desde más arriba de lo normal. En PC eso no pasa, y el vuelo debería ser "sin control".
- **Arreglo:** en `PlayerJS.Update`, después de la guarda que ya está, agregar `if (player != null && player.EnElAire) { player.FijarDisparo(false); return; }`.

**5. PC: si soltás Espacio durante el vuelo, el anillo de la granada queda en el piso y la granada no sale** (confirmado)
`ShowBies1/Assets/Scripts/Jugador/PlayerController.cs:71`
- **Qué pasa:** estás apuntando la granada con Espacio y el salto del jefe te despide. Si soltás en el aire, no tira, y el anillo blanco queda dibujado en el piso hasta que vuelvas a apretar Espacio.
- **Arreglo:** en `Lanzar`, llamar a `OcultarPunteroGranada()` junto con `FijarDisparo(false)`. Si al aterrizar seguís apretando, el anillo vuelve solo.

**6. La reseña de Play o el cartel de consentimiento pueden salir encima de la ventana de Halloween** (confirmado)
`ShowBies1/Assets/Scripts/Resena/PedidoDeResena.cs:105`
- **Qué pasa:** durante el evento (o siempre en la APK de prueba), alguien con la oleada 10 y 3 partidas entra al menú y toca HALLOWEEN, que tiene el "!" para llamar la atención, antes de que pase 1,5 s. La hoja de reseña o el formulario de consentimiento sale encima de la ventana abierta, y eso rompe la regla de "en el menú, con todo cerrado".
- **Arreglo:** sumar `&& !VentanaHalloween.Abierta` a la condición `tranquilo`. Mirá también la mejora de la lista única de ventanas, al final, para que no vuelva a pasar con la próxima ventana.

**7. Sale el automático justo después de un video con premio que se cerró antes de terminar** (confirmado)
`ShowBies1/Assets/Scripts/Anuncios/ServicioAnuncios.cs:266`
- **Qué pasa:** desde la 3.ª partida, en la derrota, alguien mira 20 de los 30 s del video del x2 (o del revivir) y lo cierra. Toca MENÚ y le sale el intersticial a pantalla completa: dos anuncios seguidos, contra tu regla de que el que ya miró uno no se come otro.
- **Por qué:** solo cuentan los videos premiados. El cerrado antes de tiempo no cuenta. (La parte del reporte sobre los videos que fallan al mostrarse no vale: esos no se llegaron a ver.)
- **Arreglo:** un contador `VideosVistosDeLaPartida` en `Progreso`, que vuelva a 0 donde vuelve `VideosDeLaPartida` y suba con `Recompensado` o `Cerrado`. `PuedeMostrarAutomatico` mira ese. Sumar el caso a `PruebasMejoras`.

### Gravedad baja, plausibles

**8. En la ciudad, los zombis que estaban del otro lado de un edificio festejan detrás de él** (plausible)
`ShowBies1/Assets/Scripts/Zombi/EnemyController.cs:1543`
- **Qué pasa:** el jugador muere en la ciudad. Un zombi detrás de un edificio empieza a rodearlo, y como camina de costado la distancia en línea recta a su lugar no baja. A los 0,5 s cambia de lugar y a los 0,5 s siguientes "festeja donde está": detrás del edificio, fuera de cuadro. (Que el lugar caiga adentro de un auto y termine festejando donde está es lo que elegiste. Esto es otra cosa: un zombi que sí podía llegar.)
- **Depende de** dónde quede el zombi respecto del edificio. Lejos, la misma cuenta sí acerca. No se midió en play.
- **Arreglo:** cuando el `Rodeo` devuelve una esquina, medir el avance contra esa esquina, o reiniciar `masCercaDelLugar` y `seAcercoEn` cada vez que cambia.

**9. Con la horda apiñada, una moneda puede caer adentro de un auto o de un contenedor** (plausible)
`ShowBies1/Assets/Scripts/PowerUps/Moneda.cs:275`
- **Qué pasa:** muere un zombi en medio de la horda, al lado de un auto, y la moneda vuela y cae adentro del auto. Sin imán no se puede cobrar.
- **Por qué:** el rayo que frena la moneda tiene lugar para 8 choques, y los cubos y cápsulas de los zombis los llenan antes de llegar al auto.
- **Depende de** que el rayo cruce unos 3 zombis y que PhysX deje afuera justo el obstáculo. No se sabe cada cuánto pasa. Con imán no pasa nada.
- **Arreglo:** agrandar `golpesDeSalida` a 32. O mejor: recortar la distancia libre contra `CapitulosDeEscenario.Huellas` y `Redondos`, como ya se hace con los edificios, así no depende del buffer.

**10. La barra flotante del jefe puede quedar hundida en su cabeza, y el jefe siguiente la hereda** (plausible)
`ShowBies1/Assets/Scripts/Zombi/BarraDeVida.cs:41`
- **Qué pasa:** si el primer tiro que no lo mata le entra mientras se agacha para el salto o para la carga, la barra se mide en esa pose y queda hundida en la cabeza. Como el jefe sale del pool, el de la oleada 20 y el de la 30 heredan la misma altura. (El caso de la barra 4 m por arriba casi no se alcanza: en el aire el jefe es kinematic y las balas no le pegan.)
- **Depende de** que el primer tiro caiga justo en el agazape, según cuándo empieza a disparar el jugador.
- **Arreglo:** medir la altura de la cabeza una sola vez en `EnemyController.Awake`, con el modelo en su pose base, y pasársela a `BarraDeVida.Crear`.

---

## 2. Mejoras (las 8 mejores, de más a menos rendimiento por esfuerzo)

Junté las que se repetían y saqué las que ya están planeadas. La entrada del jefe estaba propuesta, pero "el jefe como evento (entrada y muerte)" ya está en TAREAS. Cada una tiene el qué, por qué suma y cuánto cuesta. El detalle técnico está en la lista estructurada.

1. **Que terminar una oleada sea un momento** (impacto alto, esfuerzo chico). Hoy la oleada termina en silencio: la última muerte de un caminante no tiene pausa y el bono se cobra sin que nadie lo vea. La propuesta: cámara lenta corta y temblor en la última muerte, "¡OLEADA N SUPERADA!" con rebote y el bono como "+N" con monedas volando al contador. Es el latido del modo principal (pasa 10 a 50 veces por partida) y el único hito sin jugo. Pensalo para que conviva con las cartas de la 1.6.0, que van en ese mismo descanso.
2. **Avisos de progreso en la partida: "¡TE ALCANZA PARA UNA MEJORA!" y "¡NUEVO RÉCORD!"** (alto, chico). Con la cola que ya tiene `AvisoDeMisiones`: en el descanso de las oleadas, avisar que ya alcanza para comprar ("PAUSA → MEJORAS"), poner la insignia de cuántas compras alcanzan en el MEJORAS de la pausa y festejar en el momento cuando se pasa la mejor oleada. Sin esto, el botón MEJORAS de la pausa que sumaste en la 1.5.0 casi no se va a usar.
3. **Pulir Halloween antes del 24/10** (medio, chico). (a) Mostrar en la derrota "+N CARAMELOS" y "TE FALTAN N PARA EL PREMIO": `CaramelosDeLaPartida` ya se calcula y nadie lo muestra. (b) Apagar las calabazas que caen adentro de edificios, autos o tumbas en la ciudad y el cementerio. Se van a ver en las capturas y en los videos justo durante el evento que promocionás en Play.
4. **Bono por oleada perfecta** (medio, chico). Si la oleada se terminó sin recibir un golpe, el bono se duplica, con "¡PERFECTA! +N" en dorado. La comparación de golpes ya existe para el logro INTOCABLE. Así esquivar el zarpazo, la carga y el salto del jefe paga dentro de la partida.
5. **Que los premios que se cobran solos se sientan** (medio, chico). Avisar en la partida "¡DESAFÍO SEMANAL CUMPLIDO!", que hoy es el premio más grande y pasa sin aviso. Y al abrir el menú, mostrar un cartel con lo que cobraron solos el cierre del día, el de la semana y el de Halloween. Hoy el contador sube y nadie sabe por qué.
6. **Que lo que se compra se vea en la partida** (alto, medio). Que la bala cambie de color y crezca por tramos de daño (blanca, amarilla, naranja, roja, violeta), que la crítica salga roja desde el arma, que el disparo suene más grave con el daño y que al empezar la partida salga "¡MEJORADO! DAÑO 2 → 3". Es el corazón de un incremental: comprar y sentirte más fuerte. Usá materiales compartidos, para no romper el batching.
7. **Flechas en el borde de la pantalla** (alto, medio). Un solo sistema para dos cosas: las cajas que no se ven (con su color, latiendo más rápido cuando están por vencer) y, cuando quedan 3 zombis o menos, o el jefe está fuera de cuadro, flechas hacia ellos. Hoy casi todas las cajas nacen y vencen sin que nadie las vea, y buscar al último corredor en la ciudad es tiempo muerto. REVISION.md ya pedía la flecha a la caja de balas: esto la generaliza.
8. **El zombi del tesoro** (alto, medio). Un zombi dorado, raro, que no ataca y huye. Si lo alcanzás, revienta en una lluvia de monedas, y si no, a los 12 s se escapa. Sale con un 25 % por oleada desde la 3 y la oleada no lo espera. Rompe la rutina de quedarse quieto disparando: es el duende del tesoro de Diablo, y es de lo que la gente habla. Se arma con piezas que ya tenés: `IMovimientoPropio`, la piel con brillo, el halo de las cajas y la lluvia de monedas del jefe. Ojo con las dos listas a mano: `NivelJugador.PuntosPorTipo` y `Bestiario.Tipos`.

Quedaron afuera, y valen para más adelante: los zombis élite, el imán que también atrae las cajas, que ¡A JUGAR! diga "SIGUE EN LA OLEADA N", las misiones a la vista en la pausa, una lista única de las ventanas abiertas del menú (para que no se repita el error 6), los FPS p95 y la basura por cuadro en la APK de prueba, la resolución adaptativa y sacarles a los brazos de los zombis el choque con la capa Default.

---

## 3. Lo que haría primero

1. **Halloween, antes de que arranque** (sale el 24/10 y la versión tiene que pasar la revisión de Play): arreglar el reloj atrasado (error 3) y la reseña encima de la ventana (error 6), y hacer la mejora 3 (caramelos en la derrota y calabazas fuera de los edificios).
2. **Lo que toca la plata y al jefe, que son arreglos cortos:** el vencimiento de los anuncios (error 1), el automático después de un video cerrado (error 7) y la invocación en el cementerio (error 2). Ya que estás con el jefe, los dos del vuelo (errores 4 y 5) son una línea cada uno.
3. **Las dos mejoras más baratas que más se van a notar:** el final de oleada como momento (mejora 1) y los avisos de "te alcanza para una mejora" y "nuevo récord" (mejora 2). Las dos usan cosas que ya existen y no tocan el guardado ni el balance.