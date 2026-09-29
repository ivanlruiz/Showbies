# Superauditoría del 29/9 — plan

Pedido de Ivan: "mandate una superauditoría". Eligió **Completa** y **ahora** (5 h al 36 %, semanal al 60 %).

## Cómo

Un workflow (`showbies-superauditoria`), todo de solo lectura: ningún agente edita el repo, usa Unity ni cambia git.
Cada agente escribe su detalle en esta carpeta y devuelve un resumen chico.

1. **Buscar** — 26 frentes, un agente cada uno. Cada uno lee `pendientes.md` entero y no repite lo que ya está ahí
   (salvo evidencia nueva). Detalle en `buscar_<frente>.md`.
2. **Juntar** — un juez junta todo, saca duplicados y lo ya conocido, y ordena por gravedad. Resultado en
   `unicos.md`.
3. **Refutar** — un escéptico por hallazgo medio y dos (con miradas distintas) por hallazgo alto; los bajos no se
   refutan (queda anotado cuántos). Detalle en `refutar_<id>.md`.
4. **Sintetizar** — un agente arma `informe.md` (todo) y `para_pendientes.md` (lo que va a `pendientes.md`).

## Frentes

Por sistema: combate y disparo; zombis y horda; el jefe; oleadas y generación; jugador, vida, revivir y derrota;
progreso y guardado; economía y premios; tienda y mejoras; anuncios; menú y ventanas; HUD y pantallas de partida;
tutorial y primera vez; idiomas y textos; tema neón y UI armada en código; escenarios y noche; sonido y jugo.

Transversales: estado estático y resets; tiempo, pausa y congelado; ciclo de vida de Android; rendimiento y memoria;
build, Android y Play; trampas; pruebas (cobertura y pruebas que no pueden fallar); CLAUDE.md contra el código;
herramientas de editor y bancos; código muerto y calidad.

## Después (lo hago yo, en serie)

- Leer `informe.md` y el resumen del workflow; actualizar `pendientes.md` con `para_pendientes.md`; commit y push
  (sin `Bangers SDF.asset` ni `NewAudioMixer`).
- Lo que pida play o el teléfono queda anotado para correr bancos después.
- Ofrecerle a Ivan el informe como página (como la de la auditoría de la nube).
- Si la conversación se compacta: este archivo, `journal.jsonl` del workflow y los archivos de esta carpeta alcanzan
  para seguir.

## La tanda

- Run: wf_03667eff-685 (tarea wkgv382fo), lanzada el 29/9 a las 20:0x.
- Script: C:\Users\ivanc\.claude\projects\A--GitHub-Showbies-ShowBies1\d099429b-e154-4067-96a6-0d85e60fd83a\workflows\scripts\showbies-superauditoria-wf_03667eff-685.js
- Registro de cada agente: C:\Users\ivanc\.claude\projects\A--GitHub-Showbies-ShowBies1\d099429b-e154-4067-96a6-0d85e60fd83a\subagents\workflows\wf_03667eff-685\journal.jsonl
- Retomar si se corta: Workflow con scriptPath y resumeFromRunId (lo terminado vuelve de la cache).
- 29/9: la PC se apagó en medio de Refutar (26 frentes, el juez y 13 escépticos ya terminados). Retomada con resumeFromRunId (tarea wn388hqr9): lo terminado vino de la cache.
