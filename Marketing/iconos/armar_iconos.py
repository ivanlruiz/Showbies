"""Arma los iconos del juego a partir de una imagen cuadrada (la de Nano Banana Pro, 2048 x 2048).

    python armar_iconos.py base        -> Marketing/iconos/base/
    python armar_iconos.py halloween   -> Marketing/iconos/halloween/

Cada carpeta sale con lo que usa Unity (Assets/Sprites/Icono, con los mismos nombres: se copian
encima y conservan su guid) y lo que va a la ficha de Play:

- IconoApp.png      1024 x 1024, el cuadrado de siempre (legacy, y el de Windows).
- IconoRedondo.png  432 x 432, recortado en circulo con el resto transparente.
- IconoFondo.png    432 x 432, la capa de fondo del icono adaptativo de Android.
- IconoFrente.png   432 x 432, la capa de adelante: vacia, todo va en el fondo.
- play_512.png      512 x 512, el icono de la ficha de Play.

El adaptativo: el launcher recorta la capa al centro (72 de 108 dp, dos tercios) con la forma
que quiera. El dibujo va achicado al 72 % del lado y centrado, asi esa ventana muestra casi todo
el icono, y alrededor va el mismo icono agrandado y desenfocado, para que si algun launcher
muestra un poco mas no se vea un borde.
"""
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

AQUI = Path(__file__).resolve().parent
LADO_ADAPTATIVO = 432
DIBUJO_EN_EL_ADAPTATIVO = 0.72


def armar(fuente: Path, destino: Path):
    destino.mkdir(parents=True, exist_ok=True)
    icono = Image.open(fuente).convert("RGBA")
    if icono.width != icono.height:
        raise SystemExit(f"{fuente} no es cuadrada: {icono.size}")

    icono.resize((1024, 1024), Image.LANCZOS).save(destino / "IconoApp.png", optimize=True)
    icono.resize((512, 512), Image.LANCZOS).save(destino / "play_512.png", optimize=True)

    # El redondo: con la mascara a cuatro veces el tamanio, para que el borde salga suave.
    lado = LADO_ADAPTATIVO
    redondo = icono.resize((lado, lado), Image.LANCZOS)
    mascara = Image.new("L", (lado * 4, lado * 4), 0)
    ImageDraw.Draw(mascara).ellipse((0, 0, lado * 4 - 1, lado * 4 - 1), fill=255)
    redondo.putalpha(mascara.resize((lado, lado), Image.LANCZOS))
    redondo.save(destino / "IconoRedondo.png", optimize=True)

    # El fondo del adaptativo: el icono agrandado y desenfocado, y encima el icono achicado.
    fondo = icono.resize((lado, lado), Image.LANCZOS).filter(ImageFilter.GaussianBlur(18))
    chico = round(lado * DIBUJO_EN_EL_ADAPTATIVO)
    dibujo = icono.resize((chico, chico), Image.LANCZOS)
    # Que el dibujo se funda con el fondo en el borde: una mascara que se apaga en los ultimos px.
    borde = Image.new("L", (chico, chico), 0)
    margen = max(4, chico // 40)
    ImageDraw.Draw(borde).rectangle((margen, margen, chico - 1 - margen, chico - 1 - margen), fill=255)
    borde = borde.filter(ImageFilter.GaussianBlur(margen / 2))
    pos = (lado - chico) // 2
    fondo.paste(dibujo, (pos, pos), borde)
    fondo.convert("RGB").convert("RGBA").save(destino / "IconoFondo.png", optimize=True)

    Image.new("RGBA", (lado, lado), (0, 0, 0, 0)).save(destino / "IconoFrente.png", optimize=True)
    print("listo:", destino)


if __name__ == "__main__":
    cual = sys.argv[1] if len(sys.argv) > 1 else "base"
    armar(AQUI / f"icono_{cual}_2048.png", AQUI / cual)
