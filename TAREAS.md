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
