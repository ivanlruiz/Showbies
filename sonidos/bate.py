# Los sonidos del bate (1.7.0, ver Bate): se sintetizan aca, con numpy y sin samples, como los
# disparos de las armas (disparos.py, de donde salen las herramientas).
#
#   Silbido  "fiuu":  el bate cortando el aire, al apretar. Ruido en una banda que sube de tono
#                     mientras el golpe acelera, y se corta seco cuando conecta.
#   Golpe    "PLOC":  madera contra el zombi, solo si le pega a alguno: un bombo grave saturado
#                     (para que el parlante de un telefono deje oir el peso), la resonancia hueca
#                     de la madera y un chasquido al principio.
#
#   python bate.py            escribe los .wav en ShowBies1/Assets/otros/Resources/Bate
#
# Se vuelve a correr para cambiarlos (la semilla es fija: sale siempre lo mismo).

import os

import numpy as np

import disparos
from disparos import SR, t_de, caida, ruido, pasa_altos, pasa_bajos, barrido, bombo, igualar

disparos.SALIDA = os.path.join(disparos.AQUI, '..', 'ShowBies1', 'Assets', 'otros', 'Resources', 'Bate')
disparos.rng = np.random.default_rng(1700)


def banda(x, centro, q):
    # Un pasabanda de dos polos (state variable) con el centro que cambia muestra a muestra.
    y = np.empty_like(x)
    bajo = 0.0
    medio = 0.0
    for i, v in enumerate(x):
        f = 2 * np.sin(np.pi * min(centro[i], SR / 6) / SR)
        alto = v - bajo - medio / q
        medio += f * alto
        bajo += f * medio
        y[i] = medio
    return y


def silbido():
    t = t_de(0.2)
    n = len(t)
    # Sube mientras el bate acelera, con el pico a los 150 ms, y se corta en 30 ms.
    pico = 0.15
    subida = np.clip(t / pico, 0, 1) ** 2.2
    bajada = np.exp(-np.clip(t - pico, 0, None) / 0.012)
    env = subida * bajada
    centro = 380 + 1500 * np.clip(t / pico, 0, 1) ** 1.5
    aire = banda(ruido(n), centro, 3.5)
    cuerpo = banda(ruido(n), centro * 0.5, 2.0) * 0.6
    return (aire + cuerpo) * env


def golpe():
    t = t_de(0.22)
    n = len(t)
    chasquido = pasa_altos(ruido(n), 2200) * caida(t, 0.003, 0.0002) * 0.8
    madera = np.zeros(n)
    for f, a, tau in ((540, 1.0, 0.03), (1180, 0.6, 0.018), (1960, 0.35, 0.01)):
        madera += a * np.sin(2 * np.pi * f * t) * caida(t, tau, 0.0005)
    carne = pasa_bajos(ruido(n), 1400) * caida(t, 0.035) * 0.9
    grave = bombo(t, 170, 55, 0.025, 0.07) * 1.4
    golpecito = barrido(t, 320, 180, 0.02) * caida(t, 0.04) * 0.4
    return chasquido + madera * 0.8 + carne + grave + golpecito


def nivel(x, rms_db):
    # El silbido se mide entero y no en sus primeros 50 ms, que son casi silencio.
    x = x - x.mean()
    fin = int(0.004 * SR)
    x[-fin:] *= np.linspace(1, 0, fin)
    rms = np.sqrt((x ** 2).mean())
    x = x * (10 ** (rms_db / 20) / rms)
    pico = np.abs(x).max()
    if pico > 0.89:
        x = x * (0.89 / pico)
    return x


if __name__ == '__main__':
    disparos.escribir('Silbido', nivel(silbido(), -17.0))
    disparos.escribir('Golpe', igualar(golpe(), -11.0))
