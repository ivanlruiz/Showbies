# El logo de SHOWBIES, dibujado en codigo.
#
# No es "poner la fuente con una sombra": es el tratamiento que usan los logos de juegos
# casuales, por capas, de atras hacia adelante:
#
#   1. arco          las letras se acomodan sobre una curva y se inclinan con ella
#   2. goteo         unas gotas colgando de algunas letras, que es el gesto del genero
#   3. extrusion     la silueta repetida hacia abajo arma el volumen
#   4. contornos     uno grueso casi negro y otro marron adentro, para que lea en chico
#   5. cara          degradado amarillo arriba a naranja abajo
#   6. bisel         una banda clara pegada al borde de arriba y una oscura abajo
#   7. brillo        una franja diagonal suave sobre la cara
#
# Todo se dibuja al triple de tamanio y se achica al final: los contornos se hacen
# dilatando mascaras, y en el tamanio final eso quedaria escalonado.
import numpy as np
import cv2
from PIL import Image, ImageDraw, ImageFont

import os
# La fuente del juego, relativa al repo: este script vive en Marketing/ y la fuente en
# el proyecto de Unity.
FUENTE = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                      '..', 'ShowBies1', 'Assets', 'Fuentes', 'Bangers.ttf')
SS = 3                                  # supermuestreo

# La paleta sale del juego: el dorado de las monedas y de los carteles.
AMARILLO = (255, 226, 96)
ORO = (255, 190, 40)
NARANJA = (238, 130, 24)
MARRON = (120, 54, 10)
BORDE = (32, 18, 8)
CREMA = (255, 248, 214)
# El verde del chorreton: dice "zombi" sin tocar la palabra, que sigue siendo la dorada
# del juego. Es el mismo lima de la piel del zombi rapido, un poco mas saturado.
VERDE = (190, 240, 70)
VERDE_OSCURO = (96, 152, 20)


def _mascara_texto(texto, tam, arco, inclinacion, separacion=0.0):
    """La silueta de las letras sobre un arco, en una imagen L mas grande que el texto.

    Bangers ya es condensada y, al girar cada letra con el arco, las de la derecha se
    tocaban: por eso el avance lleva una separacion extra.
    """
    f = ImageFont.truetype(FUENTE, tam)
    anchos = [f.getlength(c) + separacion * tam for c in texto]
    total = sum(anchos)
    alto = tam * 2
    margen = tam
    lienzo = Image.new('L', (int(total + margen * 2), int(alto + margen * 2)), 0)

    x = margen
    for i, c in enumerate(texto):
        # De -1 a 1 a lo largo de la palabra; la curva es una parabola.
        centro = (x + anchos[i] / 2 - margen) / max(1.0, total / 2) - 1.0
        dy = arco * (centro ** 2 - 0.35)
        giro = -inclinacion * centro

        letra = Image.new('L', (int(anchos[i] + tam), int(alto + tam)), 0)
        ImageDraw.Draw(letra).text((tam / 2, tam / 2), c, font=f, fill=255)
        if abs(giro) > 0.01:
            letra = letra.rotate(giro, resample=Image.BICUBIC,
                                 center=(letra.width / 2, letra.height / 2))
        lienzo.paste(letra, (int(x - tam / 2), int(margen + dy - tam / 2)), letra)
        x += anchos[i]

    return np.array(lienzo)


def _gotas(mascara, cuantas, largo_max, semilla):
    """Cuelga chorretones del borde de abajo de la silueta.

    Nacen del ancho que tiene la letra en ese punto (no de un grosor fijo), se van
    afinando y terminan en una gota. Con grosor constante y una bola al final parecian
    alfileres, no algo que chorrea.
    """
    az = np.random.RandomState(semilla)
    alto, ancho = mascara.shape
    lienzo = Image.fromarray(mascara)
    d = ImageDraw.Draw(lienzo)

    # El borde de abajo, columna por columna.
    fondo = {}
    for x in range(ancho):
        ys = np.nonzero(mascara[:, x] > 128)[0]
        if len(ys):
            fondo[x] = int(ys[-1])
    if not fondo:
        return mascara

    # Solo donde el trazo es ancho: de un borde fino no chorrea nada, y ademas hay que
    # estar en la parte baja de la letra y no en un hombro.
    candidatos = []
    for x in sorted(fondo):
        corrida = 0
        for k in range(1, int(largo_max)):
            if fondo.get(x - k) is not None and abs(fondo[x - k] - fondo[x]) < largo_max * 0.10:
                corrida += 1
            else:
                break
        for k in range(1, int(largo_max)):
            if fondo.get(x + k) is not None and abs(fondo[x + k] - fondo[x]) < largo_max * 0.10:
                corrida += 1
            else:
                break
        if corrida > largo_max * 0.30:
            candidatos.append((x, fondo[x], corrida))
    if not candidatos:
        return mascara

    # Repartidas: una por zona, para que no se amontonen.
    elegidas, ultimo = [], -10 ** 9
    az.shuffle(candidatos)
    for x, y, corrida in sorted(candidatos, key=lambda c: -c[2]):
        if all(abs(x - ex) > ancho / (cuantas + 2) for ex, _, _ in elegidas):
            elegidas.append((x, y, corrida))
        if len(elegidas) >= cuantas:
            break

    for x, y, corrida in elegidas:
        largo = az.uniform(0.60, 1.15) * largo_max
        w0 = min(corrida * 0.80, largo_max * 0.60)      # nace del ancho del trazo
        w1 = w0 * az.uniform(0.42, 0.56)                # y se afina
        pasos = max(8, int(largo))
        for i in range(pasos + 1):
            t = i / pasos
            # Cae mas rapido de lo que se afina: da la forma de chorreton.
            r = (w0 * (1 - t) + w1 * t) / 2
            d.ellipse([x - r, y + t * largo - r, x + r, y + t * largo + r], fill=255)
        r = w1 * 0.62                                    # la gota del final
        cy = y + largo + r * 0.35
        d.ellipse([x - r, cy - r, x + r, cy + r], fill=255)
    return np.array(lienzo)


def _mordisco(mascara, x, tam_rel=0.17, semilla=3):
    """Le come un bocado al borde de arriba de la silueta, en la columna x.

    Ojo con DONDE se muerde: hay letras que mordidas pasan a ser otra. La O quedo
    leyendose como U y la E como una F rota. Las seguras son las que tienen partes de
    sobra: a la H se le puede comer la punta de un palo y sigue siendo una H.
    """
    az = np.random.RandomState(semilla)
    alto, ancho = mascara.shape
    x = int(min(max(x, 0), ancho - 1))
    col = np.nonzero(mascara[:, x] > 128)[0]
    if len(col) == 0:
        return mascara
    y = int(col[0])

    ys = np.nonzero(mascara > 128)[0]
    r = tam_rel * (ys.max() - ys.min() + 1)

    quitar = Image.new('L', (ancho, alto), 0)
    d = ImageDraw.Draw(quitar)
    d.ellipse([x - r, y - r * 1.05, x + r, y + r * 0.95], fill=255)
    # Los dientes del borde del bocado.
    for k in range(4):
        ang = np.pi * (0.12 + 0.76 * k / 3.0)
        rr = r * az.uniform(0.26, 0.40)
        px = x - np.cos(ang) * r * 0.90
        py = y - r * 0.05 + np.sin(ang) * r * 0.90
        d.ellipse([px - rr, py - rr, px + rr, py + rr], fill=255)
    return cv2.subtract(mascara, np.array(quitar))


def _x_de_letra(texto, tam, separacion, indice, fraccion):
    """La columna que cae en `fraccion` del ancho de la letra `indice`."""
    f = ImageFont.truetype(FUENTE, tam)
    anchos = [f.getlength(c) + separacion * tam for c in texto]
    return int(tam + sum(anchos[:indice]) + anchos[indice] * fraccion)


def _dilatar(m, radio):
    if radio <= 0:
        return m
    k = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (radio * 2 + 1, radio * 2 + 1))
    return cv2.dilate(m, k)


def _erosionar(m, radio):
    if radio <= 0:
        return m
    k = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (radio * 2 + 1, radio * 2 + 1))
    return cv2.erode(m, k)


def _pintar(destino, mascara, color):
    """Compone un color plano usando la mascara como alfa (todo float 0..1)."""
    a = (mascara.astype(np.float32) / 255.0)[..., None]
    capa = np.zeros_like(destino)
    capa[..., :3] = np.array(color, np.float32) / 255.0
    capa[..., 3:] = 1.0
    return capa * a + destino * (1 - a)


def _degradado(alto, ancho, paradas):
    """Degradado vertical: [(posicion 0..1, color), ...]."""
    y = np.linspace(0, 1, alto, dtype=np.float32)
    salida = np.zeros((alto, ancho, 3), np.float32)
    ps = sorted(paradas)
    for i in range(len(ps) - 1):
        (p0, c0), (p1, c1) = ps[i], ps[i + 1]
        tramo = (y >= p0) & (y <= p1)
        if not tramo.any():
            continue
        t = ((y[tramo] - p0) / max(1e-6, p1 - p0))[:, None]
        col = np.array(c0, np.float32) * (1 - t) + np.array(c1, np.float32) * t
        salida[tramo] = (col / 255.0)[:, None, :]
    salida[y < ps[0][0]] = np.array(ps[0][1], np.float32) / 255.0
    salida[y > ps[-1][0]] = np.array(ps[-1][1], np.float32) / 255.0
    return salida


def logo(texto='SHOWBIES', tam=260, arco=0.26, inclinacion=4.5, separacion=0.045,
         profundidad=0.105, gotas=5, con_gotas=False, verde_en_las_gotas=True,
         con_mordisco=(1, 0.80, 0.22), semilla=7):
    """Devuelve el logo como RGBA uint8, recortado a lo que ocupa.

    El mordisco va en la H y no en cualquier letra: mordida, la O se leia como U y la E
    como una F rota. La H tiene dos palos y un travesanio, asi que le falta un pedazo y
    sigue siendo una H.
    """
    t = tam * SS
    letras = _mascara_texto(texto, t, arco * t, inclinacion, separacion)
    if con_gotas:
        m = _gotas(letras, gotas, int(t * 0.30), semilla)
        # Lo que agrego el goteo, por separado: la silueta entera comparte contorno y
        # volumen, pero la cara de las gotas se pinta de otro color.
        solo_gotas = cv2.subtract(m, letras)
    else:
        m = letras
        solo_gotas = np.zeros_like(m)

    if con_mordisco:
        # `con_mordisco` es (indice de letra, fraccion del ancho, tamanio).
        i, fr, tr = con_mordisco
        m = _mordisco(m, _x_de_letra(texto, t, separacion, i, fr), tr, semilla)

    # Sitio para los contornos y la extrusion.
    pad = int(t * 0.42)
    m = cv2.copyMakeBorder(m, pad, pad, pad, pad, cv2.BORDER_CONSTANT, value=0)
    solo_gotas = cv2.copyMakeBorder(solo_gotas, pad, pad, pad, pad, cv2.BORDER_CONSTANT, value=0)
    alto, ancho = m.shape

    grueso = max(1, int(t * 0.050))          # el contorno de afuera
    medio = max(1, int(t * 0.026))           # el de adentro
    hondo = max(1, int(t * profundidad))     # cuanto baja la extrusion

    borde_ext = _dilatar(m, grueso + medio)
    borde_int = _dilatar(m, medio)

    # La extrusion: la silueta con su contorno, repetida hacia abajo y un poco a la
    # derecha, que es lo que da el cuerpo.
    cuerpo = np.zeros_like(m)
    for i in range(hondo, 0, -1):
        dx, dy = int(i * 0.34), i
        despl = np.zeros_like(m)
        despl[max(0, dy):, max(0, dx):] = borde_ext[:alto - dy, :ancho - dx]
        cuerpo = np.maximum(cuerpo, despl)

    lienzo = np.zeros((alto, ancho, 4), np.float32)

    # 1. Sombra de apoyo, para que no flote sobre el fondo.
    sombra = cv2.GaussianBlur(_dilatar(m, grueso), (0, 0), t * 0.05)
    desp = np.zeros_like(sombra)
    dy = int(t * 0.10)
    desp[dy:, :] = sombra[:alto - dy, :]
    lienzo = _pintar(lienzo, (desp * 0.55).astype(np.uint8), (0, 0, 0))

    # 2. El costado de la extrusion, con su propio contorno.
    lienzo = _pintar(lienzo, cuerpo, BORDE)
    lienzo = _pintar(lienzo, _erosionar(cuerpo, grueso), MARRON)

    # 3. Los dos contornos de la cara.
    lienzo = _pintar(lienzo, borde_ext, BORDE)
    lienzo = _pintar(lienzo, borde_int, MARRON)

    # 4. La cara, con el degradado.
    grad = _degradado(alto, ancho, [(0.28, AMARILLO), (0.50, ORO), (0.80, NARANJA)])
    a = (m.astype(np.float32) / 255.0)[..., None]
    capa = np.concatenate([grad, np.ones((alto, ancho, 1), np.float32)], axis=2)
    lienzo = capa * a + lienzo * (1 - a)

    if verde_en_las_gotas and solo_gotas.max() > 0:
        # El verde toma el chorreton entero y sube un poco adentro de la letra, para que
        # se vea que sale de ahi y no que es una punta pegada. La mezcla se difumina.
        subir = int(t * 0.10)
        adentro_letra = np.zeros_like(solo_gotas)
        adentro_letra[:alto - subir, :] = solo_gotas[subir:, :]
        zona = np.maximum(solo_gotas, cv2.bitwise_and(adentro_letra, m))
        zona = cv2.GaussianBlur(zona, (0, 0), t * 0.010)
        gv = _degradado(alto, ancho, [(0.50, VERDE), (1.00, VERDE_OSCURO)])
        ag = ((zona.astype(np.float32) / 255.0) * (m.astype(np.float32) / 255.0))[..., None]
        capag = np.concatenate([gv, np.ones((alto, ancho, 1), np.float32)], axis=2)
        lienzo = capag * ag + lienzo * (1 - ag)

    # 5. Bisel: una banda clara contra el borde de arriba y una oscura contra el de abajo.
    adentro = _erosionar(m, medio)
    sub = int(t * 0.045)
    bajado = np.zeros_like(adentro)
    bajado[sub:, :] = adentro[:alto - sub, :]
    luz = cv2.subtract(adentro, bajado)
    luz = cv2.GaussianBlur(luz, (0, 0), t * 0.012)
    lienzo = _pintar(lienzo, (luz * 0.80).astype(np.uint8), CREMA)

    subido = np.zeros_like(adentro)
    subido[:alto - sub, :] = adentro[sub:, :]
    sombra_int = cv2.subtract(adentro, subido)
    sombra_int = cv2.GaussianBlur(sombra_int, (0, 0), t * 0.014)
    lienzo = _pintar(lienzo, (sombra_int * 0.42).astype(np.uint8), MARRON)

    # 6. Brillo: una franja diagonal por la parte de arriba de la cara.
    yy, xx = np.mgrid[0:alto, 0:ancho].astype(np.float32)
    banda = np.exp(-(((yy + xx * 0.22) / alto - 0.40) ** 2) / (2 * 0.030 ** 2))
    brillo = (banda * (m.astype(np.float32) / 255.0) * 255 * 0.30).astype(np.uint8)
    lienzo = _pintar(lienzo, brillo, (255, 255, 255))

    # A tamanio final y recortado a lo que se ve.
    salida = (np.clip(lienzo, 0, 1) * 255).astype(np.uint8)
    salida = cv2.resize(salida, (ancho // SS, alto // SS), interpolation=cv2.INTER_AREA)
    ys, xs = np.nonzero(salida[..., 3] > 2)
    if len(ys):
        salida = salida[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    return salida


def pegar(fondo_bgr, rgba, cx, cy, ancho_objetivo):
    """Pega el logo (RGBA) centrado en (cx, cy) escalado a ese ancho. Fondo BGR uint8."""
    escala = ancho_objetivo / rgba.shape[1]
    w = int(rgba.shape[1] * escala)
    h = int(rgba.shape[0] * escala)
    chico = cv2.resize(rgba, (w, h), interpolation=cv2.INTER_AREA)
    x0, y0 = int(cx - w / 2), int(cy - h / 2)
    x1, y1 = x0 + w, y0 + h

    rx0, ry0 = max(0, x0), max(0, y0)
    rx1, ry1 = min(fondo_bgr.shape[1], x1), min(fondo_bgr.shape[0], y1)
    if rx0 >= rx1 or ry0 >= ry1:
        return fondo_bgr
    rec = chico[ry0 - y0:ry1 - y0, rx0 - x0:rx1 - x0]
    a = (rec[..., 3:4].astype(np.float32) / 255.0)
    # El logo se dibuja en RGB y el video va en BGR.
    color = rec[..., :3][..., ::-1].astype(np.float32)
    destino = fondo_bgr[ry0:ry1, rx0:rx1].astype(np.float32)
    fondo_bgr[ry0:ry1, rx0:rx1] = (color * a + destino * (1 - a)).astype(np.uint8)
    return fondo_bgr


def bajada(texto, tam=64, espaciado=0.16, color=CREMA):
    """La linea chica de abajo del logo: separada y con su contorno oscuro.

    Va con mucho espaciado entre letras y peso liviano: debajo de un logo pesado, una
    segunda linea gruesa compite con el y no se lee ninguna de las dos.
    """
    t = tam * SS
    f = ImageFont.truetype(FUENTE, t)
    anchos = [f.getlength(c) + espaciado * t for c in texto]
    total = int(sum(anchos))
    margen = t
    lienzo = Image.new('L', (total + margen * 2, int(t * 2.2) + margen), 0)
    d = ImageDraw.Draw(lienzo)
    x = margen
    for i, c in enumerate(texto):
        d.text((x, margen / 2), c, font=f, fill=255)
        x += anchos[i]
    m = np.array(lienzo)

    grueso = max(1, int(t * 0.085))
    alto, ancho = m.shape
    salida = np.zeros((alto, ancho, 4), np.float32)
    salida = _pintar(salida, _dilatar(m, grueso), BORDE)
    salida = _pintar(salida, m, color)

    salida = (np.clip(salida, 0, 1) * 255).astype(np.uint8)
    salida = cv2.resize(salida, (ancho // SS, alto // SS), interpolation=cv2.INTER_AREA)
    ys, xs = np.nonzero(salida[..., 3] > 2)
    if len(ys):
        salida = salida[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    return salida


# --- lo que usan los videos -------------------------------------------------------

_cache = {}


def marca(ancho_logo, texto_bajada='PRÓXIMAMENTE EN ANDROID', tam_bajada=52):
    """El logo con su bajada, ya compuesto, a un ancho dado. Se cachea: es caro."""
    clave = (ancho_logo, texto_bajada, tam_bajada)
    if clave in _cache:
        return _cache[clave]

    l = logo()
    escala = ancho_logo / l.shape[1]
    l = cv2.resize(l, (ancho_logo, int(l.shape[0] * escala)), interpolation=cv2.INTER_AREA)
    b = bajada(texto_bajada, tam_bajada) if texto_bajada else None

    hueco = int(ancho_logo * 0.02)
    alto = l.shape[0] + (hueco + b.shape[0] if b is not None else 0)
    ancho = max(ancho_logo, b.shape[1] if b is not None else 0)
    out = np.zeros((alto, ancho, 4), np.uint8)

    x = (ancho - l.shape[1]) // 2
    out[0:l.shape[0], x:x + l.shape[1]] = l
    if b is not None:
        y = l.shape[0] + hueco
        xb = (ancho - b.shape[1]) // 2
        # Compuesto sobre lo que ya hay, que el goteo del logo puede llegar hasta aca.
        rec = out[y:y + b.shape[0], xb:xb + b.shape[1]].astype(np.float32)
        a = b[..., 3:4].astype(np.float32) / 255.0
        rec[..., :3] = b[..., :3] * a + rec[..., :3] * (1 - a)
        rec[..., 3:] = np.maximum(rec[..., 3:], b[..., 3:])
        out[y:y + b.shape[0], xb:xb + b.shape[1]] = rec.astype(np.uint8)

    _cache[clave] = out
    return out


if __name__ == '__main__':
    BASE = os.path.dirname(os.path.abspath(__file__))
    ASSETS = os.path.join(BASE, '..', 'ShowBies1', 'Assets')

    # 1. El logo para el juego: solo la palabra (la bajada "PROXIMAMENTE EN ANDROID" es
    #    de las piezas de marketing, no del menu), con fondo transparente y bien grande,
    #    que Unity despues lo achica segun la calidad.
    l = logo(tam=420)
    ruta = os.path.join(ASSETS, 'Sprites', 'UI', 'LogoShowBies.png')
    os.makedirs(os.path.dirname(ruta), exist_ok=True)
    cv2.imwrite(ruta, cv2.cvtColor(l, cv2.COLOR_RGBA2BGRA))
    print('logo del juego:', l.shape[1], 'x', l.shape[0], '->', os.path.relpath(ruta, BASE))

    # 2. Las vistas previas, para mirarlas: en grande como la placa final del trailer y
    #    en chico como la franja de la vertical, que es donde el mordisco podia perderse.
    placa = np.full((1080, 1920, 3), (15, 18, 20), np.uint8)
    pegar(placa, marca(1250, tam_bajada=64), 960, 540, 1250)
    cv2.imwrite(os.path.join(BASE, 'vista_placa.png'), placa)

    banda = np.full((480, 1080, 3), (15, 18, 20), np.uint8)
    pegar(banda, marca(int(1080 * 0.70)), 540, 240, int(1080 * 0.70))
    cv2.imwrite(os.path.join(BASE, 'vista_franja.png'), banda)
    print('vistas previas: vista_placa.png y vista_franja.png')
