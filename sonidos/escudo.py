# Los sonidos del escudo del blindado (ZombiBlindado, ZOMBIS.md): se sintetizan aca, con numpy y sin samples, como los
# del bate (bate.py) y los disparos (disparos.py, de donde salen las herramientas).
#
#   Tinc     "tinc":   una bala comun que pega de frente en el escudo. Una placa de metal: parciales inarmonicos agudos
#                      que se apagan rapido, con un clic. Corto y bajito: con la cadencia alta suena veinte veces por
#                      segundo (Efectos.GolpeAlEscudo lo separa).
#   Rotura   "CLANG":  el bate le arranca el escudo. El mismo metal mas grave y largo, un golpe sordo debajo y el
#                      ruido de la chapa que vuela.
#
#   python escudo.py          escribe los .wav en ShowBies1/Assets/otros/Resources/Escudo
#
# Se vuelve a correr para cambiarlos (la semilla es fija: sale siempre lo mismo).

import os

import numpy as np

import disparos
from disparos import SR, t_de, caida, ruido, pasa_altos, pasa_bajos, bombo, igualar

disparos.SALIDA = os.path.join(disparos.AQUI, '..', 'ShowBies1', 'Assets', 'otros', 'Resources', 'Escudo')
disparos.rng = np.random.default_rng(2100)


def metal(t, parciales, tau):
    sonido = np.zeros(len(t))
    for f, a, k in parciales:
        sonido += a * np.sin(2 * np.pi * f * t + disparos.rng.uniform(0, 2 * np.pi)) * caida(t, tau * k, 0.0003)
    return sonido


def tinc():
    t = t_de(0.16)
    n = len(t)
    placa = metal(t, ((1870, 1.0, 1.0), (2930, 0.75, 0.8), (4410, 0.5, 0.55), (6120, 0.3, 0.4)), 0.035)
    clic = pasa_altos(ruido(n), 3500) * caida(t, 0.0015, 0.0002) * 0.9
    return placa + clic


def rotura():
    t = t_de(0.6)
    n = len(t)
    placa = metal(t, ((610, 1.0, 1.0), (1037, 0.8, 0.85), (1590, 0.6, 0.7), (2380, 0.45, 0.5), (3510, 0.3, 0.35)), 0.16)
    golpe = bombo(t, 190, 60, 0.03, 0.08) * 1.2
    chapa = pasa_altos(ruido(n), 1800) * caida(t, 0.09, 0.001) * 0.5
    clic = pasa_altos(ruido(n), 3000) * caida(t, 0.003, 0.0002)
    return placa + golpe + chapa + clic


if __name__ == '__main__':
    disparos.escribir('Tinc', igualar(tinc(), -17.0))
    disparos.escribir('Rotura', igualar(rotura(), -11.0))
