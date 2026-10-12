# Los disparos de las armas que cambian con el daño (1.6.0, etapa 3 de las cartas; ver
# ArmaEnLaMano y GunController.SonidoDelArma). La pistola sigue con shot.mp3; las otras cuatro
# se sintetizan aca, con numpy y sin samples, como los demas sonidos provisorios del juego:
#
#   revolver  "BAM":   un estallido seco con golpe grave, mas pesado que la pistola.
#   escopeta  "BOOM":  mas ancho y largo, con el chisporroteo de los perdigones.
#   rifle     "trrr":  un clack metalico y un chasquido corto; la cadencia alta lo encadena.
#   energia   "zap":   un barrido de tono que cae, sin polvora.
#
# De la segunda revision de Gemini (12/10): el revolver y la escopeta llevan debajo un bombo
# grave (le faltaba peso al uno y golpe a la otra), saturado para que sus armonicos se oigan
# en el parlante de un telefono, que no da los graves; y el rifle, un clack de metal al
# principio, para que suene a arma de fuego rapida y no a chasquido electronico.
#
# El volumen se iguala al de shot.mp3 como lo importa Unity (normalizado): la misma energia en
# los primeros 50 ms, con el pico por debajo de -1 dBFS; el revolver y la escopeta un poco por
# encima, que son mas grandes, y el rifle un poco por debajo, que se repite mucho. Lo grave casi
# no sale por el parlante de un telefono, asi que cada uno lleva tambien cuerpo en los medios.
#
#   python disparos.py            escribe los .wav en ShowBies1/Assets/otros/Resources/Disparos
#
# Se vuelve a correr para cambiarlos (la semilla es fija: sale siempre lo mismo).

import os
import wave

import numpy as np

SR = 44100
AQUI = os.path.dirname(os.path.abspath(__file__))
SALIDA = os.path.join(AQUI, '..', 'ShowBies1', 'Assets', 'otros', 'Resources', 'Disparos')
# Lo que mide shot.mp3 normalizado en sus primeros 50 ms (RMS, dBFS).
ENERGIA_DE_LA_PISTOLA = -12.9

rng = np.random.default_rng(1610)


def t_de(segundos):
    return np.arange(int(SR * segundos)) / SR


def caida(t, tau, ataque=0.0015):
    env = np.exp(-t / tau)
    return env * np.clip(t / ataque, 0, 1)


def ruido(n):
    return rng.uniform(-1, 1, n)


def pasa_bajos(x, corte):
    a = np.exp(-2 * np.pi * corte / SR)
    y = np.empty_like(x)
    acc = 0.0
    for i, v in enumerate(x):
        acc = (1 - a) * v + a * acc
        y[i] = acc
    return y


def pasa_altos(x, corte):
    return x - pasa_bajos(x, corte)


def barrido(t, desde, hasta, tau):
    # Un seno cuya frecuencia cae de "desde" a "hasta" exponencialmente.
    f = hasta + (desde - hasta) * np.exp(-t / tau)
    return np.sin(2 * np.pi * np.cumsum(f) / SR)


def bombo(t, desde, hasta, tau_tono, tau):
    # Un bombo: un seno que cae de tono, con un ataque seco y saturado (los armonicos son lo
    # que el parlante de un telefono deja oir de un grave).
    golpe = barrido(t, desde, hasta, tau_tono) * caida(t, tau, 0.001)
    return np.tanh(golpe * 2.5) / np.tanh(2.5)


def clack(t):
    # Metal contra metal: parciales inarmonicos que se apagan en milisegundos, con un clic.
    sonido = np.zeros(len(t))
    for f, a in ((2700, 1.0), (4150, 0.7), (5900, 0.5), (7300, 0.35)):
        sonido += a * np.sin(2 * np.pi * f * t + rng.uniform(0, 2 * np.pi))
    return sonido * caida(t, 0.006, 0.0003) + pasa_altos(ruido(len(t)), 3000) * caida(t, 0.002, 0.0002)


def revolver():
    t = t_de(0.26)
    n = len(t)
    chasquido = pasa_altos(ruido(n), 2500) * caida(t, 0.004)
    cuerpo = pasa_bajos(ruido(n), 2600) * caida(t, 0.045)
    golpe = barrido(t, 180, 75, 0.05) * caida(t, 0.07) * 0.9
    medio = barrido(t, 420, 260, 0.03) * caida(t, 0.03) * 0.35
    cola = pasa_bajos(ruido(n), 700) * caida(t, 0.09) * 0.5
    grave = bombo(t, 130, 48, 0.03, 0.09) * 1.3
    return chasquido * 0.8 + cuerpo * 1.2 + golpe + medio + cola + grave


def escopeta():
    t = t_de(0.36)
    n = len(t)
    chasquido = pasa_altos(ruido(n), 2000) * caida(t, 0.006)
    cuerpo = pasa_bajos(ruido(n), 1700) * caida(t, 0.085)
    golpe = barrido(t, 140, 50, 0.07) * caida(t, 0.11)
    medio = barrido(t, 330, 190, 0.05) * caida(t, 0.05) * 0.4
    cola = pasa_bajos(ruido(n), 450) * caida(t, 0.14) * 0.7
    # Los perdigones: chasquidos sueltos en los primeros 120 ms.
    perdigones = np.zeros(n)
    for _ in range(14):
        i = int(rng.uniform(0.004, 0.12) * SR)
        largo = int(0.006 * SR)
        k = np.arange(min(largo, n - i))
        perdigones[i:i + len(k)] += ruido(len(k)) * np.exp(-k / (0.0015 * SR)) * rng.uniform(0.3, 0.7)
    perdigones = pasa_altos(perdigones, 1200)
    grave = bombo(t, 110, 40, 0.04, 0.15) * 1.5
    return chasquido + cuerpo * 1.3 + golpe + medio + cola + perdigones + grave


def rifle():
    t = t_de(0.085)
    n = len(t)
    chasquido = pasa_altos(ruido(n), 1800) * caida(t, 0.009, 0.0005)
    cuerpo = pasa_bajos(ruido(n), 3500) * caida(t, 0.018)
    golpe = barrido(t, 300, 150, 0.015) * caida(t, 0.02) * 0.6
    return chasquido * 1.3 + cuerpo + golpe + clack(t) * 0.9


def energia():
    t = t_de(0.17)
    # El barrido, con un vaiven rapido que le da el zumbido electrico, y la quinta encima.
    vaiven = 1 + 0.12 * np.sin(2 * np.pi * 55 * t)
    f = (260 + 2100 * np.exp(-t / 0.035)) * vaiven
    fase = 2 * np.pi * np.cumsum(f) / SR
    tono = np.sign(np.sin(fase)) * 0.35 + np.sin(fase) * 0.65
    quinta = np.sin(fase * 1.5) * 0.3
    chispa = pasa_altos(ruido(len(t)), 4000) * caida(t, 0.01) * 0.35
    return (tono + quinta) * caida(t, 0.05, 0.002) + chispa


def igualar(x, energia=ENERGIA_DE_LA_PISTOLA):
    x = x - x.mean()
    # Las colas llegan a cero de verdad: un fundido de 5 ms al final.
    fin = int(0.005 * SR)
    x[-fin:] *= np.linspace(1, 0, fin)
    inicio = x[:int(0.05 * SR)]
    rms = np.sqrt((inicio ** 2).mean())
    x = x * (10 ** (energia / 20) / rms)
    pico = np.abs(x).max()
    if pico > 0.89:
        # Lo que se pasa se recorta suave en vez de bajar todo.
        x = np.tanh(x / 0.89) * 0.89
    return x


def escribir(nombre, x):
    os.makedirs(SALIDA, exist_ok=True)
    ruta = os.path.join(SALIDA, nombre + '.wav')
    datos = (np.clip(x, -1, 1) * 32767).astype('<i2')
    with wave.open(ruta, 'wb') as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(datos.tobytes())
    inicio = x[:int(0.05 * SR)]
    print('%-9s %.3f s  pico %5.1f dBFS  50 ms %5.1f dB' % (
        nombre, len(x) / SR, 20 * np.log10(np.abs(x).max()), 20 * np.log10(np.sqrt((inicio ** 2).mean()))))


if __name__ == '__main__':
    escribir('Revolver', igualar(revolver(), -11.5))
    escribir('Escopeta', igualar(escopeta(), -11.0))
    escribir('Rifle', igualar(rifle(), -13.5))
    escribir('Energia', igualar(energia()))
