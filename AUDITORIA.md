# Auditoría del 24/9 — pendientes

Auditoría de 20 frentes: cada auditor buscó errores, riesgos y mejoras, cada error lo revisó un escéptico que intentó
refutarlo y cada mejora pasó por un revisor. **Lo chico y seguro ya está aplicado** (ver los commits "Aplicar ... de la
auditoria"). Acá queda lo demás, en tres listas:

1. **Errores para arreglar después**: confirmados, pero grandes, de diseño o en archivos de la fase 2 (que está sin
   commit en la PC de Ivan: `PlayerHealth.cs` y el constructor de neón).
2. **Mejoras grandes para decidir**: valen la pena, pero son proyectos o cambian una decisión de Ivan.
3. **Descartados**: lo que el escéptico refutó (no pasa hoy), con la recomendación para cuando cambie algo.

Se completa a medida que cierran las tandas.

---

## 1. Errores para arreglar después

### Anuncios

- **Cerrar la app en ¡HAS MUERTO! conserva la oleada en curso: es un revivir gratis y sin límite** (media, a
  verificar: el escéptico no terminó). `PlayerHealth.cs:182`. Con la oferta abierta, `Terminar` (que olvida la oleada)
  todavía no corrió y el archivo ya tiene la oleada guardada. Cerrar la app desde recientes y volver da CONTINUE
  WAVE N con la vida llena. Le compite de frente al video de revivir cuando haya red real. **Arreglo**: olvidar la
  oleada y guardar justo antes de ofrecer revivir, y volver a guardarla en `Revivir`. Toca `PlayerHealth` (fase 2).

### Jugador

- **Prueba de `PlayerHealth.ColorDeVida`**: es estática "para probarla" y ninguna prueba la llama. Casos: 1; 0,61;
  0,6 (amarillo); 0,31; 0,3 (rojo); 0. Esperar a la fase 2, que cambia esos colores.

### Combate (sin verificar todavía por el escéptico: los verifiqué leyendo el código)

- **Las balas pueden atravesar al FASTER sin tocarlo** (media). `BulletController.cs:86`. La bala se mueve por
  `Translate` y el choque es discreto: a 30 FPS la bala salta ~0,37 m por cuadro contra un zombi de 0,44 m de ancho
  que viene a 12 m/s. **Arreglo**: barrido con `Physics.SphereCastNonAlloc` entre la posición anterior y la nueva, y
  sacar el collider de la bala. Medirlo antes con `PruebaDisparo` a 20 y 30 FPS.
- **Cada bala empuja al zombi que toca** (media, hipótesis). La bala no es trigger y aparece de golpe adentro del
  zombi: PhysX lo empuja al separarlos, más a menos FPS. Con el barrido de arriba se va solo; si no, `IsTrigger` en
  `Bullet.prefab`.

## 2. Mejoras grandes para decidir

### Diseño y retención

- **Punto de control por capítulo en las oleadas** (alta). Hoy morir en la 38 vuelve a la 1 (~20 min de oleadas que
  no exigen nada), y pausa → MENÚ → OLEADAS es un revivir gratis y sin límite que le va a ganar al video de revivir.
  Propuesta: al morir con la oleada > 10, guardar el principio del capítulo (morir en la 38 guarda la 31). Contradice
  "se olvida al morir" (decisión de Ivan) y toca `PlayerHealth` (fase 2).
- **Notificaciones locales** (alta): la racha, las misiones, el semanal y los premios de nivel no avisan. Falta el
  paquete `com.unity.mobile.notifications`. Ya estaba en `REVISION.md` (sugerencia 4).
- **Contenido de combate después de la oleada 10** (media): atar enemigos y jefes nuevos a los capítulos.
- **Cartas 1 de 3 por oleada, versión mínima** (media): sin congelar el tiempo y guardadas con la oleada en curso.
- **La mejora de cadencia vacía el cargador más rápido** (media): el jugador nuevo se queda sin balas hacia la
  oleada 4-5. `AplicarMejoras.cs:53`.
- **Oleadas con evento** (baja): variedad casi gratis con las perillas que ya tiene el `WaveManager`.
- **Tablas de Google Play Games** (media): el modo libre no tiene para qué jugarse después del logro.

### Anuncios

- **Un 3-2-1 al volver del video de revivir** (baja): hoy el juego arranca en el acto y la gracia de 2,5 s se gasta
  buscando los joysticks después de tocar la X del anuncio. `OfertaDeRevivir.Volver`. Choca con el vestido de la fase 2.
- **Medir el embudo de los videos** (baja): ofrecido, aceptado, premiado, cerrado, no disponible y falla por lugar,
  en las estadísticas (versión 7 del progreso). Sin eso, los topes de `ConfigAnuncios` se ajustan a ojo.

### Combate

- **Una caja de balas durante la de arma baja la cadencia de x3 a x1,5**. `GunController.PotenciarCadencia` pisa sin
  comparar, y CLAUDE.md lo documenta así ("la última pisa a la anterior"). Propuesta: si hay una mejora activa más
  fuerte, conservarla (la caja igual recarga). Es cambiar una decisión documentada.
- **Zona muerta en el joystick de disparo**: cualquier roce dispara hacia un lado al azar, y cruzar el centro
  invierte la dirección un cuadro. Propuesta: debajo de ~0,15 seguir disparando hacia la última dirección. Hay que
  probarlo en el teléfono.
- **En PC las balas no pasan por la mira** (baja): el rayo del mouse corta el plano Y=0 y la bala sale de la mano, a
  otra altura y corrida. Propuesta: cortar el rayo a la altura de la boca y orientar el arma desde ahí.
- **Balas a 11 m/s** (media): más lentas que el FASTER y casi quietas corriendo con la furia. Propuesta: 22-25 m/s
  con `lifeTime` 1 s, pero junto con el barrido (si no, atraviesan más).

### Calidad

- **Más código muerto de la tarjeta de la tienda**: `borde`, `franja`, `icono`, `barraNivel`, `estampa` y sus hijos
  apagados en el prefab. Ya se sacó la letra (`simbolo`); lo demás pide correr `ConstructorTienda` en Unity.

## 3. Descartados (el escéptico los refutó)

- **El premio se pierde si el aviso de premio llega después del de cierre** (anuncios). Hoy ningún proveedor manda
  dos resultados distintos. *Para cuando se integre la red*: dejar escrito en `IProveedorAnuncios` que el proveedor
  da un solo resultado al cerrarse, Recompensado si el premio llegó en cualquier momento, y esperar 1-2 s después de
  `onAdClosed`.
- **Sin timeout ni try/catch alrededor del proveedor** (anuncios). Con Nulo y Falso no puede colgarse. *Para cuando se
  integre la red*: envolver `Proveedor.Mostrar` en try/catch que encole `NoDisponible`; el vencimiento decidirlo con
  el SDK a la vista.
- **Con una red real, `Listo` da falso justo después de cerrar un video** (anuncios). Hoy Falso siempre está listo.
  *Para cuando se integre la red*: volver a preguntar `PuedeOfrecer` cada 0,5 s mientras las ventanas están abiertas
  y esconder el botón del revivir en vez de dejarlo gris.
- **El modo libre da el doble de experiencia por minuto** (diseño). Es cierto, pero no da ventaja económica: una
  partida de oleadas deja bastante más.
- **Economía y Bestiario copian a mano el 1,08 de `crecimientoMonedas`** (calidad). Refutado.
- **La derrota suena con `pedo.mp3`** (calidad). Refutado.
