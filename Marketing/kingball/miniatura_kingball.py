# La miniatura de YouTube de Kingball (de Argon, 19/10): la imagen de Nano Banana (sin letras, con la mitad
# izquierda libre) y encima, en código, el logo de ShowBies con la píldora OST, KINGBALL en Arial Bold rosa con la
# sombra celeste corrida (como el video) y una tira de la onda del audio abajo a la izquierda. La esquina de abajo a la
# derecha queda libre: ahí YouTube pone la duración.
# Uso: python miniatura_kingball.py <fondo> <salida> [ancho del título] [--sin-onda] [--oscurecer x0,y0,x1,y1]
# Las tres de Argon (pedidos_argon.md):
#   python miniatura_kingball.py fondo_a_nano_banana.png miniatura_a.jpg 600 --oscurecer 480,285,730,425
#   python miniatura_kingball.py fondo_b_nano_banana.png miniatura_b.jpg 500 --sin-onda
#   python miniatura_kingball.py fondo_c_nano_banana.png miniatura_c.jpg 560 --sin-onda
import sys, wave
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ANCHO, ALTO = 1280, 720
ROSA = (0xff, 0x2b, 0xd6)
CELESTE = (0x00, 0xe5, 0xff)
NOCHE = (0x0b, 0x06, 0x18)
LOGO = r"A:\GitHub\Showbies\ShowBies1\Assets\Sprites\UI\LogoShowBies.png"
WAV = r"A:\Image-Line\FL Studio\Projects\Kingball\Kingball.wav"
ARIAL = r"C:\Windows\Fonts\arialbd.ttf"


def fondo(ruta):
    im = Image.open(ruta).convert("RGB")
    # Recortar al 16:9 del centro y llevar a 1280 x 720.
    w, h = im.size
    if w / h > ANCHO / ALTO:
        nw = int(h * ANCHO / ALTO)
        im = im.crop(((w - nw) // 2, 0, (w - nw) // 2 + nw, h))
    else:
        nh = int(w * ALTO / ANCHO)
        im = im.crop((0, (h - nh) // 2, w, (h - nh) // 2 + nh))
    return im.resize((ANCHO, ALTO), Image.LANCZOS).convert("RGBA")


def envolvente(columnas, desde_s=29.3, hasta_s=37.3):
    w = wave.open(WAV)
    fr, n, ch, ancho = w.getframerate(), w.getnframes(), w.getnchannels(), w.getsampwidth()
    w.setpos(int(desde_s * fr))
    crudo = w.readframes(int((hasta_s - desde_s) * fr))
    w.close()
    b = np.frombuffer(crudo, dtype=np.uint8).reshape(-1, ancho)
    # 24 bits con signo, little endian.
    v = (b[:, 0].astype(np.int32) | (b[:, 1].astype(np.int32) << 8) | (b[:, 2].astype(np.int32) << 16))
    v = np.where(v >= 1 << 23, v - (1 << 24), v).astype(np.float32) / (1 << 23)
    v = v.reshape(-1, ch)
    # Ocho segundos desde el golpe (29,3 s), como la onda del video: el celeste es el RMS de cada columna (el cuerpo) y
    # el rosa, el pico (los golpes de la batería asoman por detrás).
    mono = (v[:, 0] + v[:, 1]) * 0.5
    trozos = np.array_split(mono, columnas)
    pico = np.array([np.abs(t).max() for t in trozos])
    rms = np.sqrt(np.array([(t ** 2).mean() for t in trozos]))
    m = pico.max()
    rosa = np.clip(pico / m, 0, 1) ** 1.3
    celeste = np.clip(rms / m * 1.9, 0, 1) ** 1.3
    return rosa, celeste


def onda(capa, x0, x1, ymedio, alto):
    columnas = x1 - x0
    rosa, celeste = envolvente(columnas)
    capa_onda = Image.new("RGBA", capa.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(capa_onda)
    for valores, color in ((rosa, ROSA), (celeste, CELESTE)):
        arriba = [(x0 + i, ymedio - valores[i] * alto * 0.5) for i in range(columnas)]
        abajo = [(x0 + i, ymedio + valores[i] * alto * 0.5) for i in reversed(range(columnas))]
        d.polygon(arriba + abajo, fill=color + (255,))
    # Se apaga hacia la derecha, para no meterse en la imagen.
    mascara = Image.new("L", capa.size, 255)
    dm = ImageDraw.Draw(mascara)
    for i in range(columnas):
        apagado = min(1.0, (columnas - i) / (columnas * 0.3))
        dm.line([(x0 + i, 0), (x0 + i, capa.size[1])], fill=int(255 * apagado))
    alfa = Image.fromarray((np.asarray(capa_onda.getchannel("A"), dtype=np.float32) * np.asarray(mascara, dtype=np.float32) / 255).astype(np.uint8))
    capa_onda.putalpha(alfa)
    capa.alpha_composite(capa_onda)


def titulo(capa, texto, x, y_centro, ancho_objetivo):
    tam = 200
    while True:
        f = ImageFont.truetype(ARIAL, tam)
        l, t, r, b = f.getbbox(texto)
        if r - l <= ancho_objetivo or tam <= 40:
            break
        tam -= 2
    alto = b - t
    y = y_centro - alto // 2 - t
    sombra = max(4, tam // 22)
    borde = max(3, tam // 34)
    d = ImageDraw.Draw(capa)
    # Un halo oscuro para despegarlo de lo que haya detrás, la sombra celeste corrida y el rosa encima.
    halo = Image.new("RGBA", capa.size, (0, 0, 0, 0))
    ImageDraw.Draw(halo).text((x, y), texto, font=f, fill=NOCHE + (200,), stroke_width=borde * 4, stroke_fill=NOCHE + (200,))
    capa.alpha_composite(halo.filter(ImageFilter.GaussianBlur(14)))
    d.text((x + sombra, y + sombra), texto, font=f, fill=CELESTE, stroke_width=borde, stroke_fill=NOCHE)
    d.text((x, y), texto, font=f, fill=ROSA, stroke_width=borde, stroke_fill=NOCHE)
    return tam


def pildora(capa, texto, x, y, alto):
    f = ImageFont.truetype(ARIAL, int(alto * 0.62))
    l, t, r, b = f.getbbox(texto)
    ancho = (r - l) + alto
    d = ImageDraw.Draw(capa)
    d.rounded_rectangle([x, y, x + ancho, y + alto], radius=alto // 2, fill=CELESTE)
    d.text((x + alto // 2 - l, y + (alto - (b - t)) // 2 - t), texto, font=f, fill=NOCHE)
    return ancho


# El retoque de Argon en la A: lo que brilla detrás del final del título, un 60 % más oscuro y con los bordes suaves,
# para que las dos L no se empasten con el pulso rosa del fondo.
def oscurecer(im, caja):
    mascara = Image.new("L", im.size, 0)
    ImageDraw.Draw(mascara).rectangle(caja, fill=255)
    m = np.asarray(mascara.filter(ImageFilter.GaussianBlur(28)), dtype=np.float32)[..., None] / 255.0
    a = np.asarray(im, dtype=np.float32)
    a[..., :3] *= 1.0 - 0.6 * m
    return Image.fromarray(a.clip(0, 255).astype(np.uint8), "RGBA")


def armar(ruta_fondo, salida, ancho_titulo=600, con_onda=True, caja_oscura=None):
    im = fondo(ruta_fondo)
    if caja_oscura:
        im = oscurecer(im, caja_oscura)
    capa = Image.new("RGBA", im.size, (0, 0, 0, 0))

    logo = Image.open(LOGO).convert("RGBA")
    ancho_logo = int(ANCHO * 0.25)
    logo = logo.resize((ancho_logo, int(logo.height * ancho_logo / logo.width)), Image.LANCZOS)
    capa.alpha_composite(logo, (44, 36))
    pildora(capa, "OST", 44 + ancho_logo + 16, 36 + logo.height // 2 - 22, 44)

    titulo(capa, "KINGBALL", 44, 352, ancho_titulo)
    # La B ya trae su ecualizador en el piso.
    if con_onda:
        onda(capa, 44, 640, 610, 110)

    im.alpha_composite(capa)
    im.convert("RGB").save(salida, quality=95)
    # Como se ve en el teléfono, para revisar que se lea.
    im.convert("RGB").resize((168, 94), Image.LANCZOS).save(salida.rsplit(".", 1)[0] + "_168.png")


if __name__ == "__main__":
    ancho = int(sys.argv[3]) if len(sys.argv) > 3 and sys.argv[3].isdigit() else 600
    caja = None
    if "--oscurecer" in sys.argv:
        caja = [int(v) for v in sys.argv[sys.argv.index("--oscurecer") + 1].split(",")]
    armar(sys.argv[1], sys.argv[2], ancho, "--sin-onda" not in sys.argv, caja)
