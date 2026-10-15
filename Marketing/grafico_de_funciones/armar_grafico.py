# Arma el grafico de funciones de la ficha de Play (1024 x 500) a partir del fondo de Nano Banana Pro
# y el logo del juego. Ver publicacion/grafico_de_funciones.md.
#
# Nano Banana deja al jefe pegado al borde de arriba aunque se le pida aire, y editando no aleja la
# camara. En vez de recortar la escena a 2:1, se achica y se apoya sobre una copia propia muy borrosa
# y mas oscura que llena lo que falta, con los bordes desvanecidos: los bordes de la escena son
# niebla y asfalto oscuros, y se lee como desenfoque. El logo va arriba a la izquierda, en el rincon
# que la escena deja oscuro, con una sombra suave.
#
#   python armar_grafico.py            -> grafico_de_funciones_1024x500.png al lado de este archivo
import os
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter

AQUI = os.path.dirname(os.path.abspath(__file__))
FONDO = os.path.join(AQUI, 'fondo_nano_banana.jpg')
LOGO = os.path.join(AQUI, '..', '..', 'ShowBies1', 'Assets', 'Sprites', 'UI', 'LogoShowBies.png')
SALIDA = os.path.join(AQUI, 'grafico_de_funciones_1024x500.png')

W, H = 1024, 500
ANCHO_ESCENA, ESCENA_X, ESCENA_Y = 880, 72, 14   # la escena achicada y donde va
BORDE = 46                                       # lo que se desvanece la escena en cada borde
ANCHO_LOGO, LOGO_X, LOGO_Y = 340, 36, 24


def armar():
    img = Image.open(FONDO).convert('RGB')
    w, h = img.size

    cubre = max(W / w, H / h)
    fondo = img.resize((round(w * cubre), round(h * cubre)), Image.LANCZOS)
    fx, fy = (fondo.width - W) // 2, (fondo.height - H) // 2
    fondo = fondo.crop((fx, fy, fx + W, fy + H)).filter(ImageFilter.GaussianBlur(28))
    fondo = ImageEnhance.Brightness(fondo).enhance(0.8)

    escala = ANCHO_ESCENA / w
    fw, fh = round(w * escala), round(h * escala)
    escena = img.resize((fw, fh), Image.LANCZOS)
    mascara = Image.new('L', (fw, fh), 0)
    ImageDraw.Draw(mascara).rectangle((BORDE, BORDE, fw - BORDE, fh - BORDE), fill=255)
    mascara = mascara.filter(ImageFilter.GaussianBlur(BORDE / 2))
    lienzo = fondo.copy()
    lienzo.paste(escena, (ESCENA_X, ESCENA_Y), mascara)

    logo = Image.open(LOGO).convert('RGBA')
    lg = logo.resize((ANCHO_LOGO, round(ANCHO_LOGO * logo.height / logo.width)), Image.LANCZOS)
    sombra = Image.new('RGBA', (lg.width + 40, lg.height + 40), (0, 0, 0, 0))
    sombra.paste(Image.new('RGBA', lg.size, (0, 0, 0, 255)), (20, 20), lg.split()[3].point(lambda v: int(v * 0.55)))
    sombra = sombra.filter(ImageFilter.GaussianBlur(10))
    base = lienzo.convert('RGBA')
    base.alpha_composite(sombra, (LOGO_X - 20, LOGO_Y - 14))
    base.alpha_composite(lg, (LOGO_X, LOGO_Y))
    base.convert('RGB').save(SALIDA)
    print(SALIDA, base.size)


if __name__ == '__main__':
    armar()
