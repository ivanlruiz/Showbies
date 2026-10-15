# Gráfico de funciones (1024 × 500): lo que dijo Argon

El 14/10/2026, a pedido de Ivan, le mostré a Argon (Gemini en Máx., chat "Análisis de diseño para ShowBies") el gráfico
de funciones publicado (`Builds/ficha/banner_1024x500.png`, de fines de septiembre), con el ícono de Halloween, la hoja
de las 8 capturas nuevas (`Builds/ficha_final_corta/hoja_es.jpg`) y el logo del juego (`LogoShowBies.png`).

## Veredicto: rehacerlo

- **Parece de otro juego**: el ícono es 3D con luz dramática y las capturas son de noche con neón magenta y cian; el
  banner tiene una grilla verde de prueba y zombis con luz plana.
- **El héroe no se ve**: en el medio hay una mancha gris tapada por los "12" y las monedas. No se entiende que es un
  twin-stick shooter.
- **No usa el logo del juego**: SHOWBIES en Bangers amarillo plano, teniendo `LogoShowBies.png`.
- **Le sobra texto**: Play escribe el título al lado; "ZOMBIE SHOOTER" y "SURVIVE • UPGRADE • REPEAT" repiten, achican y
  en el teléfono no se leen.

## Cómo tiene que ser

- **Cámara en 3/4 baja**, a la altura del hombro (como el arte de Brawl Stars o Survivor.io), sobre la calle de la ciudad
  de noche, con el neón magenta y cian de las capturas.
- **A la izquierda, el héroe** de remera blanca con el aura en los pies, disparando hacia la derecha: fogonazo grande,
  un par de balas brillantes y monedas saltando.
- **A la derecha, la horda** viniéndose encima: el jefe violeta atrás rugiendo, y adelante el zombi verde y el tanque rojo
  con los brazos estirados, con un borde de luz magenta, cian y naranja como el ícono. **Sin números de daño.**
- **El logo del juego arriba a la izquierda**, con un 10 % de margen. **Ningún otro texto**: un solo archivo para todos
  los idiomas, y queda limpio si Play le pone encima el botón del video.
- **Uno solo, permanente**: el ícono y la primera captura ya venden Halloween, y así no hay que cambiarlo el 10/11.
- **Híbrido**: el fondo con Nano Banana Pro, sin texto (con las hojas de modelo de `ReferenciasDeArte` del héroe, el
  zombi verde, el tanque y el jefe, y el ícono para la luz), y el logo pegado encima por código, como `armar_iconos.py`.

## El pedido para Nano Banana Pro (tal cual lo escribió Argon)

```
Generá un banner promocional horizontal (aspect ratio 2:1 / 16:9) para un juego móvil de acción, manteniendo exactamente el estilo 3D low-poly estilizado, cabezón y la iluminación vibrante del ícono de referencia adjunto.

Composición:

Cámara: Ángulo 3/4 dinámico a media altura, con mucha acción.

Fondo: Una calle de ciudad de noche, asfalto oscuro con líneas amarillas, iluminada por luces de neón magenta a la izquierda y azul cian a la derecha.

A la izquierda (mirando hacia la derecha): El héroe del juego (personaje low-poly cabezón con gorra oscura y remera blanca brillante, con un halo de luz blanca/cian en el piso bajo sus pies) disparando una pistola con detalles de neón hacia la derecha. Gran fogonazo naranja y amarillo en la boca del arma, dos proyectiles luminosos cruzando hacia la derecha y algunas monedas de oro brillantes saltando por el aire.

A la derecha (abalanzándose hacia el héroe): Una horda de zombis low-poly (respetando los modelos de referencia, con ojos redondos abiertos y dientes cuadrados visibles): adelante un zombi verde brillante y un zombi tanque rojo corpulento corriendo con los brazos extendidos; justo detrás de ellos, elevándose imponente, el Jefe zombi gigante de piel violeta rugiendo.

Iluminación y espacio libre: Contraluz (rim light) naranja, magenta y cian muy marcado recortando las siluetas de todos los personajes contra la noche. Importante: Dejá el cuadrante superior izquierdo con fondo nocturno oscuro y despejado (espacio negativo sobre el héroe) para poder colocar el logo del juego después.

Regla estricta: SIN TEXTO, sin letras, sin números flotando, sin barras de vida ni interfaz.
```

## La primera prueba no le gustó a Ivan (15/10)

Con el pedido de arriba y las hojas sin disfraz (`Builds/referencias/sin_disfraz`, ShowBies > Arte > Referencias sin
disfraz) salió una escena fiel a los personajes, y a Ivan no le gustó. Argon le vio que **parece otro género** (la
cámara al ras del piso y los personajes de perfil se leen como un juego de avance lateral, no como un twin-stick visto
desde arriba), que **la escena está tiesa** (el héroe de maniquí, balas que no le pegan a nadie, monedas donde no murió
nadie) y **el ruido de IA** (el haz de luz del anillo, los carteles con garabatos, el jefe y el anillo contra los
bordes). Propuso tres direcciones y **Ivan eligió la A, acorralado desde arriba**: cámara alta en 3/4 como el juego, el
héroe abajo a la izquierda disparando a una horda en semicírculo, el jefe asomando a la derecha, sin edificios, sin
anillo y sin monedas, y el rincón de arriba a la izquierda oscuro para el logo.

- **Nano Banana no respeta los márgenes que se le piden**: el jefe sale siempre pegado al borde de arriba, y editar
  ("alejá la cámara") devuelve la misma imagen. Se resuelve al componer: la escena se achica (×0,86) y se apoya sobre
  una copia propia muy borrosa y más oscura que llena lo que falta, con los bordes desvanecidos. Como los bordes son
  niebla y asfalto oscuros, se lee como desenfoque.
- **Con Argon no dibuja**: hay que pasar a 3.1 Pro y activar Crear imagen. Con el pedido largo a veces contesta en
  texto o se cuelga a los 3 minutos sin error; un segundo mensaje corto en el mismo chat ("generá la imagen ahora")
  la saca.

## El gráfico que quedó (15/10, Ivan: "ta bien")

`Marketing/grafico_de_funciones/`: el fondo de Nano Banana Pro en tamaño completo (`fondo_nano_banana.jpg`, 2752 ×
1536, del chat de Gemini "Image Generation Request Refusal"), `armar_grafico.py`, que lo achica sobre su propio fondo
borroso y le pega el logo, y el resultado, `grafico_de_funciones_1024x500.png`. Los tanques salieron del tamaño de un
zombi normal: se le pidió dos veces y no lo cambió.

## Para subirlo

- **Con el OK de Ivan**, en la ficha predeterminada (en-US). es-ES lo hereda (el 14/10 no tenía uno propio); es-419,
  mirarlo al subir.
- Va marcado como hecho con IA en la declaración de la ficha, como el ícono.
- **No mandarlo mientras haya otro envío de la ficha en revisión**: un envío nuevo reinicia el que está en curso. El del
  14/10 (ícono de las traducciones y capturas) tiene que aprobarse antes.
