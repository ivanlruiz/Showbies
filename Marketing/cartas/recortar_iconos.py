# Recorta los iconos de las cartas de la partida de la hoja que genero Gemini (Nano Banana, 12/10):
# 18 dibujos blancos sobre negro en una grilla de 6 x 3, en el orden de CARTAS.md. Cada uno sale a
# un PNG cuadrado, blanco con el alfa de su brillo (el juego lo tine con el color de la carta), en
# ShowBies1/Assets/Cartas/Iconos/IconoCarta_<id>.png.
#
# La grilla no es pareja: se separan las filas y las columnas por las franjas negras del medio, asi
# un dibujo que se corre de su celda (las rayitas del iman, la mecha de la bomba) no se corta.
#
# Uso: python recortar_iconos.py   (desde cualquier carpeta)

import os
from PIL import Image

AQUI = os.path.dirname(os.path.abspath(__file__))
HOJA = os.path.join(AQUI, "iconos_gemini.jpg")
SALIDA = os.path.join(AQUI, "..", "..", "ShowBies1", "Assets", "Cartas", "Iconos")

# En el orden de la hoja: de izquierda a derecha y de arriba abajo. Los ids son los de las cartas
# (PoderDeLaPartida); los de la etapa 2 son los que van a tener.
IDS = [
    "dano", "cadencia", "critico", "vida", "velocidad", "iman",
    "granada_rapida", "furia_larga", "perforante", "disparo_doble", "hacia_atras", "explosiva",
    "hielo", "quemadura", "dron", "vampiro", "escudo", "curacion",
]
FILAS, COLUMNAS = 3, 6
LADO = 256          # el PNG de cada icono
MARGEN = 14         # lo que queda libre alrededor del dibujo, en ese PNG
UMBRAL = 128        # lo que cuenta como dibujo al buscar las franjas negras
NEGRO, BLANCO = 40, 215   # del brillo al alfa: por debajo, transparente; por encima, opaco (la hoja es JPEG)


def tramos(perfil, cuantos):
    """Los `cuantos` tramos con dibujo de un perfil (cuanto dibujo hay en cada columna o fila),
    separados por las franjas sin nada mas anchas."""
    hay = [v > 0 for v in perfil]
    corridas = []
    i = 0
    while i < len(hay):
        if hay[i]:
            j = i
            while j < len(hay) and hay[j]:
                j += 1
            corridas.append([i, j])
            i = j
        else:
            i += 1
    # Se juntan las corridas por el hueco mas chico hasta que queden las que tienen que quedar.
    while len(corridas) > cuantos:
        huecos = [corridas[k + 1][0] - corridas[k][1] for k in range(len(corridas) - 1)]
        k = huecos.index(min(huecos))
        corridas[k][1] = corridas[k + 1][1]
        del corridas[k + 1]
    if len(corridas) != cuantos:
        raise SystemExit("se esperaban %d tramos y hay %d" % (cuantos, len(corridas)))
    return corridas


def main():
    hoja = Image.open(HOJA).convert("L")
    w, h = hoja.size
    px = hoja.load()
    mascara = [[px[x, y] > UMBRAL for x in range(w)] for y in range(h)]
    filas = tramos([sum(f) for f in mascara], FILAS)
    os.makedirs(SALIDA, exist_ok=True)
    n = 0
    for (y0, y1) in filas:
        perfil = [sum(mascara[y][x] for y in range(y0, y1)) for x in range(w)]
        for (x0, x1) in tramos(perfil, COLUMNAS):
            # La caja justa del dibujo dentro de su celda.
            ys = [y for y in range(y0, y1) if any(mascara[y][x0:x1])]
            caja = (x0, ys[0], x1, ys[-1] + 1)
            dibujo = hoja.crop(caja)
            escala = (LADO - 2 * MARGEN) / max(dibujo.size)
            dibujo = dibujo.resize((max(1, round(dibujo.width * escala)), max(1, round(dibujo.height * escala))), Image.LANCZOS)
            alfa = dibujo.point(lambda v: 0 if v <= NEGRO else 255 if v >= BLANCO else round((v - NEGRO) * 255 / (BLANCO - NEGRO)))
            icono = Image.new("RGBA", (LADO, LADO), (255, 255, 255, 0))
            blanco = Image.new("RGBA", dibujo.size, (255, 255, 255, 255))
            blanco.putalpha(alfa)
            icono.paste(blanco, ((LADO - dibujo.width) // 2, (LADO - dibujo.height) // 2))
            ruta = os.path.join(SALIDA, "IconoCarta_%s.png" % IDS[n])
            icono.save(ruta)
            print(IDS[n], caja)
            n += 1


if __name__ == "__main__":
    main()
