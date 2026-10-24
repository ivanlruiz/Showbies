# Recorta el logo "Tóxico Arcade" (lo dibujó Argon con Nano Banana el 23/10, sobre un fondo liso #0b0618) y lo deja con
# transparencia en ShowBies1/Assets/Sprites/UI/LogoShowBies.png, que es lo que usan el menú, los clips, el gráfico de
# funciones y la miniatura de Kingball. Reemplaza al de logo.py (el dorado, que a Ivan no le gustaba).
#
#   cd Marketing/logo_toxico
#   python recortar_logo.py
#
# El problema es que el bloque oscuro de detrás de las letras es casi del color del fondo: cortando por color, quedaría
# agujereado. Por eso va en tres pasos:
#   1. lo que se aparta mucho del fondo (las letras y el borde de neón) es "sólido", y todo lo que ese borde encierra
#      (el bloque oscuro, los huecos de la O y la B) también;
#   2. afuera, el resplandor sale de cuánto se aparta del fondo, con el color despremultiplicado: sobre el cielo del menú,
#      que no es exactamente este fondo, no queda un halo sucio;
#   3. lejos del logo no queda nada: la imagen trae en las esquinas un fantasma de la captura que se le pasó de
#      referencia, que no se ve pero se colaría como manchas.
# Al final se recorta justo al resplandor y se completa con transparencia hasta la proporción del PNG de antes
# (1721 × 532), así el quad del menú no cambia.

import os
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

AQUI = os.path.dirname(os.path.abspath(__file__))
ORIGINAL = os.path.join(AQUI, 'original_nano_banana.jpg')
DESTINO = os.path.join(AQUI, '..', '..', 'ShowBies1', 'Assets', 'Sprites', 'UI', 'LogoShowBies.png')
VISTA_BLANCO = os.path.join(AQUI, 'vista_sobre_blanco.png')

FONDO = np.array([11.0, 8.0, 31.0])  # el #0b0618 que pidió Argon, medido en la imagen
UMBRAL_SOLIDO = 45.0                 # distancia al fondo desde la que es letra o borde
OPACO_DESDE = 220.0                  # el resplandor llega a opaco cuando un canal sube esto sobre el fondo: alto, para
                                     # que sea poco opaco y de su color de neón, y no opaco y oscuro
RUIDO = 6.0                          # lo que se aparta menos que esto es fondo (el JPEG tiene grano)
ALCANCE_DEL_BRILLO = 70              # px alrededor de lo sólido donde puede haber resplandor
ANCHO, ALTO = 1721, 532              # el PNG de antes


def rellenar_huecos(mascara):
    """Lo encerrado por la máscara pasa a ser máscara: se inunda el fondo desde afuera y lo que no se alcanzó es hueco."""
    h, w = mascara.shape
    img = Image.new('L', (w + 2, h + 2), 0)
    img.paste(Image.fromarray((mascara * 255).astype(np.uint8)), (1, 1))
    ImageDraw.floodfill(img, (0, 0), 128)
    a = np.asarray(img)[1:-1, 1:-1]
    return a != 128


def mancha_del_centro(mascara):
    """La mancha conectada que pasa por el medio de la imagen, que es el logo: el fantasma de las esquinas también pasa
    el umbral en algunos puntos, sueltos."""
    h, w = mascara.shape
    ys, xs = np.nonzero(mascara[h // 2 - 40:h // 2 + 40, :])
    i = int(np.argmin(abs(xs - w // 2)))
    semilla = (int(xs[i]), int(ys[i]) + h // 2 - 40)
    img = Image.fromarray((mascara * 255).astype(np.uint8)).copy()  # sin copiar, la imagen no se deja pintar
    ImageDraw.floodfill(img, semilla, 128)
    return np.asarray(img) == 128


def dilatar(mascara, radio):
    """Dilata a un cuarto del tamaño (el MaxFilter grande es lento) y vuelve."""
    h, w = mascara.shape
    chica = Image.fromarray((mascara * 255).astype(np.uint8)).resize((w // 4, h // 4), Image.BILINEAR)
    tam = 2 * (radio // 4) + 1
    chica = chica.filter(ImageFilter.MaxFilter(tam)).filter(ImageFilter.GaussianBlur(radio / 12))
    return np.asarray(chica.resize((w, h), Image.BILINEAR)).astype(float) / 255.0


def main():
    im = np.asarray(Image.open(ORIGINAL).convert('RGB')).astype(float)
    dif = im - FONDO
    distancia = np.sqrt((dif ** 2).sum(-1))

    # 1. lo sólido y lo que encierra
    fuerte = distancia > UMBRAL_SOLIDO
    # las letras están separadas por el bloque oscuro: para encontrar el logo entero se las junta un poco
    logo = mancha_del_centro(dilatar(fuerte, 24) > 0.5)
    solido = rellenar_huecos(fuerte & logo)
    cerca = dilatar(solido, ALCANCE_DEL_BRILLO)

    # 2. el resplandor: alfa por lo que sube el canal que más sube, color despremultiplicado
    exceso = np.clip(dif, 0, None).max(-1)
    alfa = np.clip((exceso - RUIDO) / (OPACO_DESDE - RUIDO), 0, 1)
    alfa = np.maximum(alfa, solido.astype(float))
    # 3. nada lejos del logo
    alfa = alfa * np.clip(cerca * 1.5, 0, 1)

    seguro = np.maximum(alfa, 1e-3)[..., None]
    color = (im - FONDO * (1 - alfa[..., None])) / seguro
    color = np.where(solido[..., None], im, color)
    color = np.clip(color, 0, 255)

    rgba = np.dstack([color, alfa * 255]).astype(np.uint8)
    logo = Image.fromarray(rgba, 'RGBA')

    # recorte justo al resplandor y lugar transparente hasta la proporción de antes
    caja = Image.fromarray((alfa > 0.02).astype(np.uint8) * 255).getbbox()
    logo = logo.crop(caja)
    w, h = logo.size
    escala = min(ANCHO / w, ALTO / h)
    logo = logo.resize((round(w * escala), round(h * escala)), Image.LANCZOS)
    lienzo = Image.new('RGBA', (ANCHO, ALTO), (0, 0, 0, 0))
    lienzo.paste(logo, ((ANCHO - logo.width) // 2, (ALTO - logo.height) // 2))
    lienzo.save(DESTINO)

    blanco = Image.new('RGBA', lienzo.size, (255, 255, 255, 255))
    blanco.alpha_composite(lienzo)
    blanco.convert('RGB').save(VISTA_BLANCO)
    print('recorte', caja, '->', logo.size, 'en', lienzo.size)


if __name__ == '__main__':
    main()
