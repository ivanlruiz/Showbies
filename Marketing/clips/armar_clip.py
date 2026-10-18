# Arma los clips verticales para TikTok, Shorts y Reels a partir de las tomas que graba GrabarClips
# (Builds/clips/<toma>/f0000.jpg..., sonido.wav). Pega las tomas, pone el texto de cada momento
# (en ingles y en espanol, como las capturas de la ficha: Bangers en mayusculas, relleno blanco o
# amarillo, trazo negro grueso y brillo de neon), el cierre de los ultimos 3 s (el video sigue de
# fondo oscurecido, el logo grande y "FREE ON GOOGLE PLAY") y el audio: Kingball, el tema de Ivan, y los
# efectos del juego (ver VOLUMEN_MUSICA). Lo que decidio Argon esta en publicacion/clips.md. Arma tambien
# una version limpia, sin textos ni cierre, para decorarla con otra herramienta.
#
#   python armar_clip.py clip1        -> Builds/clips/salida/clip1_en.mp4, clip1_es.mp4 y clip1_limpio.mp4
#                                        (y clip2 a clip4)
import os
import subprocess
import sys
import wave

from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageFont

AQUI = os.path.dirname(os.path.abspath(__file__))
RAIZ = os.path.normpath(os.path.join(AQUI, '..', '..'))
TOMAS = os.path.join(RAIZ, 'Builds', 'clips')
SALIDA = os.path.join(TOMAS, 'salida')
FUENTE = os.path.join(RAIZ, 'ShowBies1', 'Assets', 'Fuentes', 'Bangers.ttf')
LOGO = os.path.join(RAIZ, 'ShowBies1', 'Assets', 'Sprites', 'UI', 'LogoShowBies.png')
MUSICA = os.path.join(RAIZ, 'ShowBies1', 'Assets', 'otros', 'Kingball.ogg')

W, H, FPS = 1080, 1920, 30
# Zonas seguras (Argon): el texto entre el 12 y el 22 % desde arriba; nada en el 25 % de abajo ni en
# el 15 % de la derecha (los botones de la red). El texto se centra en lo que queda.
X_IZQ, X_DER = 60, int(W * 0.85) - 20
Y_TEXTO = int(H * 0.13)
CIERRE = 3.0
# La musica arriba y los efectos un poco abajo (Ivan, 18/10: con 0,6 y 1 los efectos tapaban la musica;
# medidos, los efectos andan en -19 dB de media y Kingball en -16). El limitador evita que la suma recorte.
VOLUMEN_MUSICA = 0.9
VOLUMEN_EFECTOS = 0.75

AMARILLO = (255, 214, 64)
BLANCO = (255, 255, 255)
MAGENTA = (255, 60, 200)
CIAN = (40, 220, 255)
NARANJA = (255, 140, 40)
VIOLETA = (175, 95, 255)

# Cada clip: las tomas (carpeta, primer cuadro, cuantos cuadros), los textos de cada tramo en los dos
# idiomas (desde, hasta, texto, color, brillo, y opcionalmente la altura en fraccion de la pantalla y
# el tamano) y el cierre. musica_desde pone el golpe de Kingball (a los 29,4 s) en el momento fuerte.
CLIPS = {
    'clip1': {
        'tomas': [('clip1_debil', 0, 120), ('clip1_fuerte', 0, 330)],
        # El golpe de Kingball cae a los 29,5 s: arrancando en 25,5, cae justo en el corte al fuerte.
        'musica_desde': 25.5,
        'textos': {
            'en': [(0.0, 4.0, 'LEVEL 1 ROOKIE', BLANCO, MAGENTA),
                   (4.0, 12.0, 'MAX UPGRADES\n+ FULL CARDS', AMARILLO, MAGENTA)],
            'es': [(0.0, 4.0, 'NIVEL 1: NOVATO', BLANCO, MAGENTA),
                   (4.0, 12.0, 'MEJORAS\nAL MÁXIMO', AMARILLO, MAGENTA)],
        },
        'cierre': {
            'en': ('FREE ON GOOGLE PLAY', 'Search: ShowBies'),
            'es': ('GRATIS EN GOOGLE PLAY', 'Buscalo como: ShowBies'),
        },
    },
    # El jefe salta (0,7-2 s), carga (3,7-5 s), queda aturdido y la furia lo castiga hasta que revienta
    # (10 s); despues, las monedas. El golpe de la musica, con la furia.
    'clip2': {
        'tomas': [('clip2_jefe', 0, 413)],
        'musica_desde': 24.4,
        'textos': {
            'en': [(0.0, 4.95, 'WAIT FOR\nTHE MISTAKE...', BLANCO, MAGENTA),
                   (4.95, 10.0, 'RAGE MODE:\n2X DAMAGE', AMARILLO, MAGENTA)],
            'es': [(0.0, 4.95, 'ESPERA\nEL ERROR...', BLANCO, MAGENTA),
                   (4.95, 10.0, '¡MODO FURIA:\nDAÑO X2!', AMARILLO, MAGENTA)],
        },
        'cierre': {
            'en': ('FREE ON GOOGLE PLAY', 'Search: ShowBies'),
            'es': ('GRATIS EN GOOGLE PLAY', 'Buscalo como: ShowBies'),
        },
    },
    # El tesoro sale a los 0,2 s y huye; la granada (3 s), la furia (4 s) y revienta a los 6,8 s. Va
    # justo arriba del heroe, a un 20 % del borde de arriba: el texto, mas arriba y mas chico.
    'clip3': {
        'tomas': [('clip3_tesoro', 0, 325)],
        'musica_desde': 25.4,
        'textos': {
            'en': [(0.2, 3.2, '12 SECONDS BEFORE\nHE ESCAPES', AMARILLO, MAGENTA, 0.075, 96),
                   (3.2, 6.8, "DON'T LET\nHIM GO!", BLANCO, MAGENTA, 0.075, 96)],
            'es': [(0.2, 3.2, '12 SEGUNDOS ANTES\nDE QUE ESCAPE', AMARILLO, MAGENTA, 0.075, 96),
                   (3.2, 6.8, '¡QUE NO\nSE ESCAPE!', BLANCO, MAGENTA, 0.075, 96)],
        },
        'cierre': {
            'en': ('FREE ON GOOGLE PLAY', 'Search: ShowBies'),
            'es': ('GRATIS EN GOOGLE PLAY', 'Buscalo como: ShowBies'),
        },
    },
    # Cuatro cortes de 3 s con el sombrero de calabaza: el nombre de cada arma en el color de su bala.
    # El de energia dura 6 s: los ultimos 3 son el fondo del cierre. El golpe, con la de energia.
    'clip4': {
        'tomas': [('clip4_revolver', 0, 90), ('clip4_escopeta', 0, 90), ('clip4_rifle', 0, 90), ('clip4_energia', 0, 180)],
        'musica_desde': 20.4,
        'textos': {
            'en': [(0.0, 9.0, 'WHICH WEAPON\nDO YOU PICK?', BLANCO, MAGENTA),
                   (9.0, 12.0, 'HALLOWEEN EVENT\nLIVE NOW', NARANJA, MAGENTA),
                   (0.0, 3.0, 'REVOLVER', AMARILLO, MAGENTA, 0.265, 104),
                   (3.0, 6.0, 'SHOTGUN', NARANJA, MAGENTA, 0.265, 104),
                   (6.0, 9.0, 'RIFLE', MAGENTA, VIOLETA, 0.265, 104),
                   (9.0, 12.0, 'ENERGY GUN', VIOLETA, MAGENTA, 0.265, 104)],
            'es': [(0.0, 9.0, '¿QUÉ ARMA\nELEGÍS?', BLANCO, MAGENTA),
                   (9.0, 12.0, 'EVENTO DE\nHALLOWEEN ACTIVO', NARANJA, MAGENTA),
                   (0.0, 3.0, 'REVÓLVER', AMARILLO, MAGENTA, 0.265, 104),
                   (3.0, 6.0, 'ESCOPETA', NARANJA, MAGENTA, 0.265, 104),
                   (6.0, 9.0, 'RIFLE', MAGENTA, VIOLETA, 0.265, 104),
                   (9.0, 12.0, 'ARMA DE ENERGÍA', VIOLETA, MAGENTA, 0.265, 104)],
        },
        'cierre': {
            'en': ('FREE ON GOOGLE PLAY', 'Search: ShowBies'),
            'es': ('GRATIS EN GOOGLE PLAY', 'Buscalo como: ShowBies'),
        },
    },
}


def fuente(tam):
    return ImageFont.truetype(FUENTE, tam)


_capas = {}


def capa_de_texto(texto, y, tam, relleno, brillo, ancho_max):
    """La capa transparente con el texto, armada una vez por texto (el brillo es caro)."""
    clave = (texto, y, tam, relleno, brillo, ancho_max)
    if clave not in _capas:
        capa = Image.new('RGBA', (W, H), (0, 0, 0, 0))
        texto_con_neon(capa, texto, y, tam, relleno, brillo, ancho_max)
        _capas[clave] = capa
    return _capas[clave]


def texto_con_neon(lienzo, texto, y, tam, relleno, brillo, ancho_max):
    """Dibuja el texto centrado entre X_IZQ y X_DER, achicandolo si no entra, con trazo y brillo."""
    lineas = texto.split('\n')
    while True:
        f = fuente(tam)
        anchos = [f.getbbox(l, stroke_width=max(4, tam // 10))[2] for l in lineas]
        if max(anchos) <= ancho_max or tam <= 40:
            break
        tam -= 4
    trazo = max(4, tam // 10)
    alto_linea = int(tam * 1.02)
    capa = Image.new('RGBA', lienzo.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(capa)
    cx = (X_IZQ + X_DER) // 2
    for i, l in enumerate(lineas):
        w = f.getbbox(l, stroke_width=trazo)[2]
        d.text((cx - w // 2, y + i * alto_linea), l, font=f, fill=brillo + (255,), stroke_width=trazo + 10, stroke_fill=brillo + (255,))
    halo = capa.filter(ImageFilter.GaussianBlur(18))
    lienzo.alpha_composite(halo)
    lienzo.alpha_composite(halo)
    capa = Image.new('RGBA', lienzo.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(capa)
    for i, l in enumerate(lineas):
        w = f.getbbox(l, stroke_width=trazo)[2]
        d.text((cx - w // 2, y + i * alto_linea), l, font=f, fill=relleno + (255,), stroke_width=trazo, stroke_fill=(0, 0, 0, 255))
    lienzo.alpha_composite(capa)


_franja = []


def franja_del_cierre():
    """Una franja oscura y difusa detras del logo y del texto del cierre: con la accion de fondo, los
    numeros de dano competian con FREE ON GOOGLE PLAY."""
    if not _franja:
        capa = Image.new('RGBA', (W, H), (0, 0, 0, 0))
        ImageDraw.Draw(capa).rectangle((0, int(H * 0.19), W, int(H * 0.52)), fill=(8, 4, 20, 150))
        _franja.append(capa.filter(ImageFilter.GaussianBlur(60)))
    return _franja[0]


def cuadros_del_clip(clip):
    for carpeta, desde, cuantos in clip['tomas']:
        for i in range(desde, desde + cuantos):
            yield os.path.join(TOMAS, carpeta, 'f%04d.jpg' % i)


def armar_video(nombre, idioma):
    clip = CLIPS[nombre]
    rutas = list(cuadros_del_clip(clip))
    total = len(rutas) / FPS
    logo = Image.open(LOGO).convert('RGBA')
    ancho_logo = int(W * 0.78)
    logo = logo.resize((ancho_logo, round(ancho_logo * logo.height / logo.width)), Image.LANCZOS)
    carpeta = os.path.join(SALIDA, nombre + '_' + idioma)
    os.makedirs(carpeta, exist_ok=True)
    limpio = idioma == 'limpio'
    for n, ruta in enumerate(rutas):
        t = n / FPS
        img = Image.open(ruta).convert('RGB')
        if limpio:
            img.save(os.path.join(carpeta, 'f%04d.jpg' % n), quality=93)
            continue
        en_cierre = t >= total - CIERRE
        if en_cierre:
            # El video sigue de fondo, oscurecido un 25 % (sin placa negra: la gente pasa de largo).
            img = ImageEnhance.Brightness(img).enhance(0.75)
        lienzo = img.convert('RGBA')
        if en_cierre:
            lienzo.alpha_composite(franja_del_cierre())
        for tramo in clip['textos'][idioma]:
            desde, hasta, texto, relleno, brillo = tramo[:5]
            y = int(H * tramo[5]) if len(tramo) > 5 else Y_TEXTO
            tam = tramo[6] if len(tramo) > 6 else 120
            if desde <= t < hasta and not en_cierre:
                lienzo.alpha_composite(capa_de_texto(texto, y, tam, relleno, brillo, X_DER - X_IZQ))
        if en_cierre:
            # El logo entra con un salto en un cuarto de segundo y queda.
            entra = min(1.0, (t - (total - CIERRE)) / 0.25)
            escala = round(0.85 + 0.15 * entra, 3)
            lg = logo.resize((int(logo.width * escala), int(logo.height * escala)), Image.LANCZOS)
            sombra = Image.new('RGBA', (lg.width + 80, lg.height + 80), (0, 0, 0, 0))
            sombra.paste(Image.new('RGBA', lg.size, (0, 0, 0, 255)), (40, 40), lg.split()[3].point(lambda v: int(v * 0.6)))
            sombra = sombra.filter(ImageFilter.GaussianBlur(20))
            x = (W - lg.width) // 2
            y = int(H * 0.30) - lg.height // 2
            lienzo.alpha_composite(sombra, (x - 40, y - 28))
            lienzo.alpha_composite(lg, (x, y))
            grande, chico = clip['cierre'][idioma]
            base = int(H * 0.30) + logo.height // 2
            lienzo.alpha_composite(capa_de_texto(grande, base + 40, 104, AMARILLO, MAGENTA, W - 2 * X_IZQ))
            lienzo.alpha_composite(capa_de_texto(chico, base + 175, 72, BLANCO, CIAN, W - 2 * X_IZQ))
        lienzo.convert('RGB').save(os.path.join(carpeta, 'f%04d.jpg' % n), quality=93)
    return carpeta, total


def armar_sonido(nombre):
    """Los efectos de cada toma, cortados como los cuadros, en un solo WAV."""
    clip = CLIPS[nombre]
    salida = os.path.join(SALIDA, nombre + '_efectos.wav')
    datos, params = b'', None
    for carpeta, desde, cuantos in clip['tomas']:
        ruta = os.path.join(TOMAS, carpeta, 'sonido.wav')
        if not os.path.exists(ruta):
            return None
        with wave.open(ruta, 'rb') as w:
            p = w.getparams()
            params = params or p
            por_cuadro = p.framerate / FPS
            w.setpos(int(desde * por_cuadro))
            datos += w.readframes(int(cuantos * por_cuadro))
    with wave.open(salida, 'wb') as w:
        w.setparams(params)
        w.writeframes(datos)
    return salida


def main():
    nombre = sys.argv[1] if len(sys.argv) > 1 else 'clip1'
    os.makedirs(SALIDA, exist_ok=True)
    efectos = armar_sonido(nombre)
    for idioma in ('en', 'es', 'limpio'):
        carpeta, total = armar_video(nombre, idioma)
        mp4 = os.path.join(SALIDA, '%s_%s.mp4' % (nombre, idioma))
        cmd = ['ffmpeg', '-y', '-loglevel', 'error', '-framerate', str(FPS), '-i', os.path.join(carpeta, 'f%04d.jpg'),
               '-ss', str(CLIPS[nombre]['musica_desde']), '-i', MUSICA]
        if efectos:
            cmd += ['-i', efectos,
                    '-filter_complex', '[1:a]volume=%.2f,atrim=0:%.3f[m];[2:a]volume=%.2f[e];[m][e]amix=inputs=2:duration=first:normalize=0,alimiter=limit=0.95:level=disabled,afade=t=out:st=%.3f:d=0.5[a]' % (VOLUMEN_MUSICA, total, VOLUMEN_EFECTOS, total - 0.5),
                    '-map', '0:v', '-map', '[a]']
        else:
            cmd += ['-filter_complex', '[1:a]volume=%.2f,atrim=0:%.3f,afade=t=out:st=%.3f:d=0.5[a]' % (VOLUMEN_MUSICA, total, total - 0.5),
                    '-map', '0:v', '-map', '[a]']
        cmd += ['-c:v', 'libx264', '-pix_fmt', 'yuv420p', '-crf', '18', '-preset', 'slow', '-c:a', 'aac', '-b:a', '192k',
                '-t', '%.3f' % total, '-movflags', '+faststart', mp4]
        subprocess.run(cmd, check=True)
        print(mp4, '%.1f s' % total, 'con efectos' if efectos else 'sin efectos')


if __name__ == '__main__':
    main()
