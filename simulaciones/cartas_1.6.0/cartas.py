# Simulacion del balance de las cartas de la 1.6.0 (etapa 4, 13/10).
#
# La pregunta: las cartas de la partida (CARTAS.md) hacen al jugador mas fuerte dentro de cada
# partida y corren el muro de las oleadas, que la 1.5.2 dejo entre la 50 y la 60 para que el
# renacer de la 1.8.0 tenga sentido. Aca se suma a la economia de curva.py (se juega, se cobran
# las monedas y se compra entre partidas) lo que trae la 1.6.0:
#
#   - el PODER de la partida: cada zombi da su experiencia (los .asset), pasar del PODER L al
#     siguiente cuesta round(12 x 1,15^L) (PoderDeLaPartida.Costo), y cada subida es una
#     eleccion de 1 de 3 cartas sorteadas entre las que se pueden usar, con la dorada (x2) despues
#     de cada jefe;
#   - el efecto de cada carta, traducido al modelo 1D (ver el README: que es exacto y que no);
#   - el arma de la mano por tramo del dano sin la furia (Proyectiles.cs, GunController.cs): su
#     alcance, la velocidad de la bala, su cadencia compensada, los perdigones de la escopeta y
#     la onda de energia que atraviesa dos zombis;
#   - el auto-apuntado del telefono: una fraccion del tiempo a mano (cadencia entera y la punteria
#     del perfil) y el resto en automatico (media cadencia, punteria fija y hasta 16 m), y los
#     dos solo contra lo que se ve en la pantalla (la camara de WaveMode).
#
# La revision del 13/10 (dos revisores contra el codigo C#; ver el README) le agrego: el alcance
# de lo que se ve segun el rumbo de cada zombi (a mano, en automatico, el dron y los ataques del
# jefe), el cargador que no se llena entre oleadas, el techo de 60 en PC, los zarpazos del jefe y
# el ritmo y los saltos de sus ataques, el revivir (como variante), el tunel de las balas rapidas
# (como variante), el error de punteria como un angulo (los perdigones y el disparo doble), la
# cura del vampiro en las oleadas faciles y el ancho de la bala del juego.
#
# Despues, la decision de Gemini (13/10, CARTAS.md), como variantes ('gemini' y sus piezas): un
# tercer tramo de la curva desde la 31, la moneda de los zombis x2 en valor, la carta de cadencia
# sin costo de balas por los tiros de mas y el rifle a cadencia x1. Lo que no las usa da lo mismo
# que antes con las mismas semillas.
#
# El modelo de base es el de curva.py y se importa de ahi (los zombis, las mejoras, la tienda,
# los perfiles, el iman y lo que pegan segun la velocidad); lo que cambia es la partida, que se
# reescribe aca. Como en curva.py: los numeros absolutos no son confiables, la comparacion entre
# variantes corridas con las mismas semillas si.
#
# Uso: python cartas.py [clave=valor ...]
#   modo=curva (las variantes) | xp (la tabla de cartas por oleada) | pesos (cada carta sola, con
#   las mejoras congeladas) | escopeta (las primeras partidas con y sin ella) | balas (las balas de
#   las oleadas faciles contra el paso a paso)
#   partidas=250 semillas=5 perfiles=bajo,medio,alto variantes=todas hilos=12 fotos=1 salida=...
# Lo que se corrio y lo que encontro esta en el README.
import heapq
import json
import math
import os
import random
import statistics
import sys
import time
from multiprocessing import Pool

AQUI = os.path.dirname(os.path.abspath(__file__))
sys.dont_write_bytecode = True   # que importar curva.py no le deje un __pycache__ en su carpeta
sys.path.insert(0, os.path.join(AQUI, '..', 'curva_1.5.2'))
import curva  # noqa: E402  (la economia de la 1.5.2: no se modifica)

DT = curva.DT
TECHO_VIVOS = curva.TECHO_VIVOS          # el del telefono (WaveManager.maxZombisVivosMovil)
TECHO_PC = 60                            # WaveManager.maxZombisVivos: en PC no se baja a 35
# La horda que encierra al jugador (factor_velocidad de curva.py): se mide contra 35 vivos en
# los dos, asi en PC, con hasta 60, la horda pesa mas (con min(1, ...) como tope).
HORDA_DE_REFERENCIA = 35.0
APARICION = curva.APARICION
NACEN_A = curva.NACEN_A
# Hasta donde se juega: una partida que pasa la 100 dura mas de dos horas, y ahi ya no importa
# donde estaria el muro.
MAX_OLEADAS = 100
MAX_SEGUNDOS_OLEADA = curva.MAX_SEGUNDOS_OLEADA
DESCANSO = 3.0
# Lo que tarda el jugador en elegir una carta, con el juego congelado: no cuenta como tiempo de
# juego, pero si en los minutos que dura la partida de verdad.
SEGUNDOS_POR_ELECCION = 3.0

# --- La curva de la 1.5.2 (WaveMode.unity y Escalado.PorOleada) ---------------------------------
# La oleada 11 ya vale la 10 por el tardio (en curva.py el tardio empezaba en la 12: un 3 %).
CRECE_VIDA, CRECE_VIDA_TARDIO = 1.11, 1.08
CRECE_GOLPE, CRECE_GOLPE_TARDIO = 1.07, 1.04
SEGUNDO_TRAMO = 11
CRECE_MONEDAS = 1.08


def por_oleada(crece, oleada, desde=0, tardio=0.0):
    """Escalado.PorOleada: con 'desde' y 'tardio', en dos tramos."""
    if desde <= 1 or tardio <= 0 or oleada < desde:
        return crece ** max(0, oleada - 1)
    return crece ** (desde - 2) * tardio ** (oleada - desde + 1)


# El tercer tramo (la decision de Gemini del 13/10): desde la 31, la vida x1,15 y el golpe x1,08.
# Con la misma convencion que Escalado.PorOleada (que todavia no tiene tres tramos: esto la define
# para el juego): la 31 ya vale la 30 por el tercero, asi que hasta la 30 es la de dos tramos y la
# oleada n >= 31 vale crece^(desde-2) x tardio^(desde3-desde) x tercero^(n-desde3+1).
TERCER_TRAMO = 31
CRECE_VIDA_TERCERO, CRECE_GOLPE_TERCERO = 1.15, 1.08


def por_oleada3(crece, oleada, desde, tardio, desde3, tercero):
    """Escalado.PorOleada en tres tramos; sin el tercero es la de dos."""
    if desde3 <= 1 or tercero <= 0 or oleada < desde3:
        return por_oleada(crece, oleada, desde, tardio)
    return por_oleada(crece, desde3 - 1, desde, tardio) * tercero ** (oleada - desde3 + 1)


# Las mejoras de la tienda, como las eligio la 1.5.2: danio 1 + nivel, cadencia 6 + nivel.
MEJORAS = dict(dano_base=1, cad_base=6, hitos=False)

# --- Los perfiles ----------------------------------------------------------------------------
# Los de curva.py, mas la fraccion del tiempo que apuntan a mano en el telefono (el resto tira
# el automatico).
MANUAL = {'bajo': 0.2, 'medio': 0.5, 'alto': 0.8}
PUNTERIA_AUTOMATICA = 0.9   # el automatico apunta al mas cercano: falla solo lo que se mueve
PUNTERIA_DEL_DRON = 0.85


def perfil_de(nombre):
    p = dict(curva.PERFILES[nombre])
    p['manual'] = MANUAL[nombre]
    p['nombre'] = nombre
    return p


# --- Los zombis ------------------------------------------------------------------------------
# curva.TIPOS y curva.JEFE (hp, danio, velocidad, monedas, desde, peso), con su experiencia (los
# .asset de Assets/Zombies) y su radio en el mundo (el CapsuleCollider, 0,72, por la escala de
# la raiz del prefab: 0,5 el normal, 0,4 el rapido, 0,3 el FASTER, 1 el tanque y 2 el jefe).
EXPERIENCIA = {'normal': 1, 'rapido': 1, 'faster': 1, 'tanque': 3, 'jefe': 15}
EXPERIENCIA_TESORO = 5
RADIO = {'normal': 0.36, 'rapido': 0.29, 'faster': 0.22, 'tanque': 0.76, 'jefe': 1.44}
# El BoxCollider de la bala: un cubo de 1 en el prefab de escala 0,128 (Bullet.prefab), que Vestir
# deja del mismo tamanio en el mundo con cualquier arma y con la critica: medio ancho 0,064.
RADIO_BALA = 0.064
# A distancia 0 el zombi esta pegado: el abanico se mide desde aca (la suma de los radios).
DISTANCIA_PEGADO = 0.9

# --- Lo que se ve (la camara de WaveMode.unity) -------------------------------------------------
# La camara sigue al jugador a 12 m del piso y 3,9 m al sur, mirando 70 grados hacia abajo, con
# 60 grados de campo vertical. Lo que se ve de un zombi depende del rumbo por el que viene: al
# norte ~9 m, al sur ~5,6, a los costados ~12 (16:9) o ~14,6 (20:9). Cada zombi saca su rumbo al
# azar al aparecer, y con el, hasta donde se lo ve: su centro (WorldToViewportPoint del transform,
# a la mitad de su capsula: 0,5 m el normal, 0,4 el rapido, 0,3 el FASTER, 1 el tanque y 2 el
# jefe), con el margen de quien mira: el automatico (PlayerJS.MargenEnPantalla, 0,03), a mano y
# el dron (lo que esta en la pantalla, sin margen) y el jefe para atacar (JefePatrones,
# margenEnPantalla 0,08 x 0,1).
CAM_ALTO, CAM_ATRAS, CAM_PITCH, CAM_FOV = 12.0, 3.9, 70.0, 60.0
ALTURA_DEL_CENTRO = {'normal': 0.5, 'rapido': 0.4, 'faster': 0.3, 'tanque': 1.0, 'jefe': 2.0}
ASPECTO = {'telefono': 20.0 / 9.0, 'pc': 16.0 / 9.0}   # 20:9, el telefono mas comun (2400 x 1080)
MARGENES = {'auto': (0.03, 0.03), 'ver': (0.0, 0.0), 'jefe': (0.08, 0.1)}
RUMBOS = 360
SIN_PANTALLA = 1e9


def se_ve(x, z, h, aspecto, mx, my):
    """WorldToViewportPoint del punto (x, h, z), con el jugador en (0, 0) del piso."""
    p = math.radians(CAM_PITCH)
    vy, vz = h - CAM_ALTO, z + CAM_ATRAS
    yc = vy * math.cos(p) + vz * math.sin(p)
    zc = -vy * math.sin(p) + vz * math.cos(p)
    if zc <= 0:
        return False
    t = math.tan(math.radians(CAM_FOV / 2))
    px = 0.5 + 0.5 * x / (zc * t * aspecto)
    py = 0.5 + 0.5 * yc / (zc * t)
    return mx < px < 1 - mx and my < py < 1 - my


def radio_visible(rumbo, h, aspecto, mx, my):
    """Hasta donde se ve hacia ese rumbo (0 el norte): lo que se ve del piso es un trapecio que
    contiene al jugador, asi que alcanza con buscar el borde."""
    sx, cz = math.sin(rumbo), math.cos(rumbo)
    lo, hi = 0.0, 40.0
    for _ in range(32):
        mid = 0.5 * (lo + hi)
        if se_ve(mid * sx, mid * cz, h, aspecto, mx, my):
            lo = mid
        else:
            hi = mid
    return lo


RADIOS = {}
for _tipo, _h in ALTURA_DEL_CENTRO.items():
    for _pantalla, _aspecto in ASPECTO.items():
        for _margen, (_mx, _my) in MARGENES.items():
            if _margen == 'jefe' and _tipo != 'jefe':
                continue
            RADIOS[(_tipo, _pantalla, _margen)] = [radio_visible(2 * math.pi * i / RUMBOS, _h, _aspecto, _mx, _my)
                                                   for i in range(RUMBOS)]
_MEDIAS = {}


def alcance_medio_tipo(tipo, pantalla, margen, tope):
    """La media, sobre los rumbos, de lo que se ve topeado con el alcance del arma."""
    clave = (tipo, pantalla, margen, round(tope, 3))
    m = _MEDIAS.get(clave)
    if m is None:
        if pantalla is None:
            m = tope
        else:
            rs = RADIOS[(tipo, pantalla, margen)]
            m = sum(min(tope, r) for r in rs) / len(rs)
        _MEDIAS[clave] = m
    return m


class Zombi:
    __slots__ = ('tipo', 'hp', 'hp_max', 'dano', 'vel', 'mmin', 'mmax', 'd', 'reloj', 'vivo', 'radio',
                 'xp', 'hielo_hasta', 'lentitud', 'quema_ticks', 'quema_tick', 'quema_prox',
                 'ataque', 'ataques', 'r_mano', 'r_auto', 'r_jefe', 'patron_hasta', 'sin_invocar',
                 'toca_salto')

    def __init__(self, tp, mv, md, d, pantalla=None, rumbo=0):
        self.tipo = tp[0]
        self.hp = self.hp_max = tp[1] * mv
        self.dano = tp[2] * md
        self.vel = tp[3]
        self.mmin, self.mmax = tp[4], tp[5]
        self.d = d
        self.reloj = 0.0
        self.vivo = True
        self.radio = RADIO[self.tipo]
        self.xp = EXPERIENCIA[self.tipo]
        self.hielo_hasta = -1.0
        self.lentitud = 0.0
        self.quema_ticks = 0
        self.quema_tick = 0.0
        self.quema_prox = 0.0
        self.ataque = 0.0
        self.ataques = 0
        self.patron_hasta = -1.0
        self.sin_invocar = 0
        self.toca_salto = True
        if pantalla is None:
            self.r_mano = self.r_auto = self.r_jefe = SIN_PANTALLA
        else:
            self.r_mano = RADIOS[(self.tipo, pantalla, 'ver')][rumbo]
            self.r_auto = RADIOS[(self.tipo, pantalla, 'auto')][rumbo]
            self.r_jefe = RADIOS[('jefe', pantalla, 'jefe')][rumbo] if self.tipo == 'jefe' else SIN_PANTALLA


# --- Las armas de la mano (Proyectiles.cs) ----------------------------------------------------
PISTOLA, REVOLVER, ESCOPETA, RIFLE, ENERGIA = range(5)
NOMBRE_ARMA = ['pistola', 'revolver', 'escopeta', 'rifle', 'energia']
VEL_ARMA = [15, 25, 12, 35, 12]
VIDA_ARMA = [1.5, 1.0, 0.6, 0.8, 1.8]
ALCANCE_ARMA = [v * l for v, l in zip(VEL_ARMA, VIDA_ARMA)]   # 22,5  25  7,2  28  21,6
CADENCIA_ARMA = [1.0, 0.7, 0.7, 1.2, 1.0]
PERFORA_ARMA = [0, 0, 0, 0, 2]                                 # la de energia, sin perder danio
ANGULOS_ESCOPETA = [-12.0, -4.0, 4.0, 12.0]
PARTE_PERDIGON = 1.0 / 3.0
ANGULO_DOBLE_ESCOPETA = 20.0
ANGULO_POR_CARTA = 8.0
ABANICO_DOBLE = 8.0
DESDE_DANO = [0.0, 2.0, 4.0, 8.0, 16.0]                        # BulletController.DesdeDano
TECHO_TIROS = 120.0
ALCANCE_AUTOMATICO = 16.0
CADENCIA_AUTOMATICA = 0.5
ALCANCE_DRON = 12.0
VEL_DRON = 15.0                                                # la bala de la pistola
# Como curva.py, para la variante de calibracion.
ALCANCE_CURVA = curva.ALCANCE
VEL_CURVA = curva.VEL_BALA
# Las oleadas faciles gastan balas (el cargador ya no se llena al terminarlas): con la escopeta
# de lejos entran los dos perdigones del medio, dos tercios del tiro.
EFICACIA_ESCOPETA_LEJOS = 2.0 / 3.0

# El error de punteria es un angulo: la punteria del perfil es la probabilidad de que la bala del
# medio le pegue al blanco (como siempre, sin mirar la distancia), y con eso se sortea cuanto se
# desvio el tiro, asi cada perdigon y cada bala del disparo doble prueban con su propio angulo.
# El desvio es normal, con el sigma que da esa punteria contra un normal a 8 m.
DISTANCIA_DE_REFERENCIA = 8.0
NORMAL = statistics.NormalDist()


def sigma_de(punteria):
    alfa = math.atan((RADIO['normal'] + RADIO_BALA) / DISTANCIA_DE_REFERENCIA)
    return alfa / NORMAL.inv_cdf((1 + punteria) / 2)


# El jefe (JefePatrones): ataca a menos de 15 m y si se lo ve; el proximo ataque es 5 s despues
# de terminar el patron (x0,65 en furia). Cada patron lo frena (no camina ni tira zarpazos):
# el salto, 0,9 s de aviso, el vuelo y 1,3 s aturdido; la carga, 0,9 de aviso, 1 s embistiendo a
# 16 m/s y 1,3 aturdido; invocar, 0,8. El salto lo deja donde estaba el jugador (que lo esquivo
# saliendo del anillo de 4,5 m o salio despedido 6 m): a ~5 m. La carga lo pasa de largo.
JEFE_ALCANCE = 15.0
JEFE_CADA = 5.0
JEFE_PATRON = {'salto': 2.8, 'carga': 3.2, 'invocar': 0.8}
JEFE_TRAS_EL_SALTO = 5.0
JEFE_LARGO_DE_LA_CARGA = 16.0
JEFE_ZARPAZO = 0.8

# El revivir (PlayerHealth, OfertaDeRevivir, ConfigAnuncios): una vez por partida, con 180 s
# jugados en total contando la partida en curso, la partida de al menos 30 s y un video; vuelve
# con la vida llena, al menos 500 balas, 2,5 s sin recibir danio y los zombis a menos de 7 m
# (salvo el jefe) fuera, sin monedas ni experiencia.
REVIVIR_DESDE = 180.0
REVIVIR_PARTIDA = 30.0
REVIVIR_BALAS = 500
REVIVIR_DESPEJE = 7.0
REVIVIR_GRACIA = 2.5

# El tunel: la bala no tiene Rigidbody (se mueve con Translate en Update) y los zombis tienen
# deteccion discreta, asi que la fisica ve la bala una vez por cuadro (o por paso de fisica, a
# 50 Hz, si el telefono va a mas). Una bala que de un cuadro al siguiente avanza mas que el
# zombi atraviesa sin pegar. Solo como variante: hay que medirlo en el telefono.
FISICA_HZ = 50.0


def tramo(dano):
    t = 0
    for i in range(1, len(DESDE_DANO)):
        if dano + 1e-4 >= DESDE_DANO[i]:
            t = i
    return t


def horda(n):
    """Una bala que no va al blanco elegido le pega a otro de la horda (curva.py)."""
    return min(0.6, n / 30.0)


def al_costado(d, radio):
    """La explosiva en 1D: de los que estan a la misma distancia (+-radio), cuantos caen del
    mismo lado. Pegados al jugador hacen un anillo de ~6 m: 1,5 m cubren casi la mitad; a la
    distancia la horda se abre."""
    return min(0.6, 0.27 * radio / max(1.0, d))


# --- Las cartas (Assets/Cartas, PoderDeLaPartida) ----------------------------------------------
# id: (tope, familia, requisito). Los valores de cada una estan en Partida.recalcular.
CARTAS = {
    'dano': (4, 'dano', None),
    'cadencia': (4, 'dano', None),
    'critico': (3, 'dano', None),
    'vida': (3, 'supervivencia', None),
    'velocidad': (2, 'supervivencia', None),
    'iman': (2, 'utilidad', None),
    'granada_rapida': (2, 'utilidad', 'granada'),
    'furia_larga': (2, 'utilidad', 'furia'),
    'perforante': (2, 'reglas', None),
    'disparo_doble': (2, 'reglas', None),
    'hacia_atras': (1, 'reglas', None),
    'explosiva': (2, 'reglas', None),
    'hielo': (2, 'reglas', None),
    'quemadura': (2, 'reglas', None),
    'dron': (2, 'reglas', None),
    'vampiro': (2, 'supervivencia', None),
    'escudo': (1, 'supervivencia', None),
}
ORDEN_CARTAS = sorted(CARTAS)          # Sortear ordena por id antes de sortear
CURACION = 'curacion'
PRIMER_COSTO, CRECE_COSTO = 12, 1.15


def costo(poder, crece=CRECE_COSTO):
    """PoderDeLaPartida.Costo: round(12 x 1,15^L). Math.Round de C# redondea al par, como round."""
    return int(round(PRIMER_COSTO * crece ** max(0, poder)))


# Lo que prefiere cada politica dentro de cada familia.
PREFERENCIA = {
    'dano': ['dano', 'cadencia', 'critico'],
    'supervivencia': ['vida', 'escudo', 'vampiro', 'velocidad'],
    'reglas': ['perforante', 'disparo_doble', 'explosiva', 'dron', 'hielo', 'hacia_atras', 'quemadura'],
    'utilidad': ['granada_rapida', 'furia_larga', 'iman'],
}
ROTACION = ['dano', 'supervivencia', 'reglas']
BONO_DORADA = 25


def familia_util(p, carta):
    """La familia con que la mira el jugador: un critico con el critico ya al 100 % no sirve."""
    if carta == 'critico' and p.crit >= 1.0:
        return 'utilidad'
    return CARTAS[carta][1]


def puntaje_dano_primero(p, carta):
    fam = familia_util(p, carta)
    base = {'dano': 100, 'reglas': 50, 'supervivencia': 30, 'utilidad': 10}[fam]
    lista = PREFERENCIA[CARTAS[carta][1]]
    return base - lista.index(carta)


def puntaje_equilibrada(p, carta):
    fam = familia_util(p, carta)
    quiero = ROTACION[p.elecciones % len(ROTACION)]
    if fam == 'utilidad':
        base = 10
    else:
        # La familia que toca, despues la siguiente en la rueda y despues la otra.
        lejos = (ROTACION.index(fam) - ROTACION.index(quiero)) % len(ROTACION)
        base = (100, 60, 40)[lejos]
    lista = PREFERENCIA[CARTAS[carta][1]]
    return base - lista.index(carta)


def elegir(p, ofrecidas, dorada):
    """El indice de la carta que elige la politica de la variante."""
    if ofrecidas[0] == CURACION:
        return 0
    politica = p.var['politica']
    if politica == 'azar':
        return p.rng.randrange(len(ofrecidas))
    puntaje = puntaje_dano_primero if politica == 'dano' else puntaje_equilibrada
    mejor, mejor_i = None, 0
    for i, c in enumerate(ofrecidas):
        s = puntaje(p, c) + (BONO_DORADA if i == dorada else 0)
        if mejor is None or s > mejor:
            mejor, mejor_i = s, i
    return mejor_i


# --- La partida --------------------------------------------------------------------------------

class Partida:
    def __init__(self, niv, perfil, var, rng, segundos_previos=0.0):
        self.rng = rng
        self.perfil = perfil
        self.var = var
        self.st = curva.estadisticas(niv, MEJORAS)
        self.con_granada = niv['granada'] > 0
        self.con_furia = niv['furia'] > 0
        self.manual = 1.0 if not var['auto'] else perfil['manual']
        self.ciclo_manual = int(round(10 * self.manual))
        self.cura_caja = round(40 * self.st['mult_vida'])
        # Lo de la revision del 13/10; la calibracion (como_curva) vuelve a lo de curva.py.
        self.como_curva = bool(var.get('como_curva'))
        self.pantalla = None if self.como_curva else var['pantalla']
        self.techo = var['techo']
        self.sigma_mano = sigma_de(perfil['punteria'])
        self.sigma_auto = sigma_de(PUNTERIA_AUTOMATICA)
        self.fps = var.get('fps')
        self.segundos_previos = segundos_previos
        self.revivio = False
        self.gracia_hasta = -1.0
        self.inicio_partida = 0.0
        self._tunel = {}
        self.aparicion = var['aparicion']
        # Cuanto se pasa sin balas con un zombi al alcance (los pasos de 0,1 s).
        self.pasos_con_blanco = 0
        self.pasos_sin_balas = 0
        # La decision de Gemini (13/10): los tiros de mas de la carta de cadencia no gastan balas
        # (cada tiro cuesta 1 / el multiplicador de cadencia de las cartas, con un acumulador), y
        # cada moneda que sueltan los zombis vale 'valor_moneda' (el bono de la oleada no cambia).
        self.cadencia_gratis = bool(var.get('cadencia_gratis'))
        self.deuda_balas = 0.0
        self.valor_moneda = var.get('valor_moneda', 1.0)
        # Lo que va pasando en cada oleada que se termina: los minutos de partida hasta ahi
        # (con las elecciones de cartas) y las monedas juntadas.
        self.min_por_oleada = []
        self.monedas_por_oleada = []

        self.veces = dict(var.get('fijas', {}))
        self.poder = 0
        self.experiencia = 0
        self.pendientes = 0
        self.dorada = False
        self.elecciones = 0
        self.doradas = 0
        self.bajas_vampiro = 0
        self.oleada = 0
        self.poder_por_oleada = []
        self.dron_prox = []

        self.t = 0.0
        self.paso = 0
        self.monedas = 0.0
        self.balas = 500
        self.max_balas = 500
        self.potencia = 1.0
        self.potencia_hasta = -1.0
        self.granada_lista = 0.0
        self.furia_lista = 0.0
        self.furia_hasta = -1.0
        self.escudo_listo = 0.0
        self.prox_caja = {'balas': 8.0, 'vida': 8.0, 'arma': 20.0}
        self.en_vuelo = []
        self.seq = 0
        self.vivos = []
        self.golpeado = False
        self.mm = 1.0
        self.cerca = []
        self.recalcular()
        self.arma_inicio = self.arma
        self.vida = float(self.vida_max)

    # --- Lo que dejan las cartas ---------------------------------------------------------------

    def recalcular(self):
        v = self.veces.get
        st = self.st
        var = self.var
        self.m_dano = var['valor_dano'] ** v('dano', 0)
        self.m_cad = var['valor_cadencia'] ** v('cadencia', 0)
        self.crit = min(1.0, st['crit'] + var['valor_critico'] * v('critico', 0))
        self.vida_max = max(1, round(st['vida'] * var['valor_vida'] ** v('vida', 0)))
        self.vel_jugador = curva.VEL_JUGADOR * (1 + 0.1 * v('velocidad', 0))
        self.frac_monedas = curva.fraccion_monedas(self.perfil, st['iman'] + 2.0 * v('iman', 0))
        self.recarga_granada = 5.0 * 0.7 ** v('granada_rapida', 0)
        self.dur_furia = 6.0 + 2.0 * v('furia_larga', 0)
        self.perf_carta = v('perforante', 0)
        self.extra = v('disparo_doble', 0)
        self.atras = v('hacia_atras', 0)
        n = v('explosiva', 0)
        self.expl_frac = 0.3 * n
        self.expl_radio = 1.5 + 0.5 * (n - 1) if n else 0.0
        n = v('hielo', 0)
        self.hielo = 1 - 0.6 ** n if n else 0.0
        self.quema_p = min(1.0, 0.25 * v('quemadura', 0))
        self.drones = v('dron', 0)
        while len(self.dron_prox) < self.drones:
            self.dron_prox.append(self.t + 0.5)
        self.vampiro = 0.05 * v('vampiro', 0)
        self.escudo = v('escudo', 0) > 0

        # El arma de la mano: el tramo del danio de la mejora por las cartas, sin la furia. Sin
        # armas, todo es la pistola; sin escopeta, ese tramo tira como el revolver.
        a = tramo(st['dano'] * self.m_dano) if self.var['armas'] else PISTOLA
        if self.var.get('sin_escopeta') and a == ESCOPETA:
            a = REVOLVER
        self.arma = a
        if self.var.get('como_curva'):
            self.alcance_arma, self.vel_bala = ALCANCE_CURVA, VEL_CURVA
        else:
            self.alcance_arma, self.vel_bala = ALCANCE_ARMA[a], VEL_ARMA[a]
        self.cadencia_arma = CADENCIA_ARMA[a]
        if a == RIFLE and self.var.get('cadencia_rifle') is not None:
            # La decision de Gemini: el rifle a cadencia x1 (y la compensacion con ella).
            self.cadencia_arma = self.var['cadencia_rifle']
        self.perf_libres = PERFORA_ARMA[a]
        # Las balas de cada tiro: (angulo con signo, en radianes, y parte del tiro).
        # GunController.Disparar: cada perdigon y cada bala del disparo doble, con su angulo.
        if a == ESCOPETA:
            patron = [(math.radians(g), PARTE_PERDIGON) for g in ANGULOS_ESCOPETA]
            for k in range(self.extra):
                g = math.radians(ANGULO_DOBLE_ESCOPETA + k * ANGULO_POR_CARTA)
                patron += [(-g, PARTE_PERDIGON), (g, PARTE_PERDIGON)]
        else:
            patron = [(math.radians(-self.extra * 0.5 * ABANICO_DOBLE + i * ABANICO_DOBLE), 1.0)
                      for i in range(self.extra + 1)]
        self.patron = patron
        self.abanico = len(patron) > 1

    def tope(self, carta):
        if carta == 'dano':
            return self.var['tope_dano']
        return CARTAS[carta][0]

    def disponible(self, carta):
        tope, _, req = CARTAS[carta]
        if carta in self.var.get('excluir', ()):
            return False
        if self.veces.get(carta, 0) >= self.tope(carta):
            return False
        if req == 'granada' and not self.con_granada:
            return False
        if req == 'furia' and not self.con_furia:
            return False
        return True

    def sumar_experiencia(self, xp):
        if not self.var['cartas']:
            return
        self.experiencia += xp
        crece = self.var['crece_costo']
        while self.experiencia >= costo(self.poder, crece):
            self.experiencia -= costo(self.poder, crece)
            self.poder += 1
            self.pendientes += 1

    def resolver_elecciones(self):
        while self.pendientes > 0:
            self.pendientes -= 1
            candidatas = [c for c in ORDEN_CARTAS if self.disponible(c)]
            ofrecidas = self.rng.sample(candidatas, min(3, len(candidatas))) if candidatas else [CURACION]
            dorada = self.rng.randrange(len(ofrecidas)) if self.dorada and ofrecidas[0] != CURACION else -1
            i = elegir(self, ofrecidas, dorada)
            self.aplicar(ofrecidas[i], i == dorada)

    def aplicar(self, carta, es_dorada):
        self.elecciones += 1
        if es_dorada:
            # PoderDeLaPartida.Elegir: la dorada se gasta solo si se elige.
            self.dorada = False
            self.doradas += 1
        if carta == CURACION:
            self.vida = float(self.vida_max)
            return
        self.veces[carta] = min(self.tope(carta), self.veces.get(carta, 0) + (2 if es_dorada else 1))
        self.recalcular()
        if carta == 'vida':
            self.vida = float(self.vida_max)

    # --- Los golpes --------------------------------------------------------------------------

    def matar(self, z):
        z.vivo = False
        rng = self.rng
        pal = rng.randint(z.mmin, z.mmax)
        frac = self.frac_monedas
        self.monedas += sum(1 for _ in range(pal) if rng.random() < frac) * self.mm
        if self.var['cartas']:
            self.sumar_experiencia(z.xp)
            if z.tipo == 'jefe':
                self.dorada = True
        if self.vampiro > 0:
            self.bajas_vampiro += 1
            if self.bajas_vampiro >= 15:
                self.bajas_vampiro = 0
                self.vida = min(float(self.vida_max), self.vida + max(1, round(self.vida_max * self.vampiro)))
        if self.pendientes:
            self.resolver_elecciones()

    def pegar(self, z, dano, quema):
        """BulletController.Pegar: el danio, el hielo, la quemadura y la explosion."""
        if not z.vivo:
            return
        z.hp -= dano
        if z.hp <= 0.01:
            self.matar(z)
        else:
            t = self.t
            if self.hielo > 0:
                z.lentitud = max(z.lentitud, self.hielo) if t < z.hielo_hasta else self.hielo
                z.hielo_hasta = t + 2.0
            if quema > 0:
                tick = quema * 0.5
                if z.quema_ticks <= 0:
                    z.quema_tick = tick
                    z.quema_prox = t + 0.5
                else:
                    z.quema_tick = max(z.quema_tick, tick)
                z.quema_ticks = 6
        if self.expl_frac > 0:
            r = self.expl_radio
            p = al_costado(z.d, r)
            danio = dano * self.expl_frac
            rng = self.rng
            desde, hasta = z.d - r, z.d + r
            for o in self.vivos:
                if desde <= o.d <= hasta and o is not z and o.vivo and rng.random() < p:
                    o.hp -= danio
                    if o.hp <= 0.01:
                        self.matar(o)

    def reaprovechar(self):
        """La bala que llega a un muerto le pega al mas cercano, a veces (curva.py)."""
        if self.rng.random() >= self.perfil['reaprovecha']:
            return None
        z = None
        for o in self.cerca:
            if o.vivo and (z is None or o.d < z.d):
                z = o
        return z

    def llegar(self, z, dano, n, quema, libres, carta):
        """Llegan juntas las n balas de un paso que iban al mismo blanco (se suman para que la
        simulacion corra: cada efecto se tira una vez con el danio de todas, que en promedio es lo
        mismo). Si el blanco ya murio, pueden pegarle a otro; si lo matan, las que sobran tambien.
        Despues atraviesan lo que les toque: la de energia sin perder danio, las de la carta a la
        mitad."""
        rng = self.rng
        if not z.vivo:
            z = self.reaprovechar()
            if z is None:
                return
        antes = z.hp
        self.pegar(z, dano, quema)
        if libres + carta <= 0:
            if n > 1 and not z.vivo:
                una = dano / n
                sobran = n - max(1, math.ceil((antes - 0.01) / una))
                if sobran > 0:
                    otro = self.reaprovechar()
                    if otro is not None:
                        self.pegar(otro, una * sobran, quema)
            return
        tocados = [z]
        cerca = self.cerca
        while libres + carta > 0:
            cand = [o for o in cerca if o.vivo and o not in tocados]
            if not cand or rng.random() >= horda(len(cand)):
                break
            if libres > 0:
                libres -= 1
            else:
                carta -= 1
                dano *= 0.5
            detras = [o for o in cand if o.d >= z.d]
            o = rng.choice(detras or cand)
            tocados.append(o)
            self.pegar(o, dano, quema)

    def recibir(self, dano):
        """PlayerHealth.TakeDamage: el escudo para el golpe entero y no cuenta como golpe."""
        if self.t < self.gracia_hasta:
            return
        if self.escudo and self.t >= self.escudo_listo:
            self.escudo_listo = self.t + 15.0
            for z in self.vivos:
                if z.vivo and z.tipo != 'jefe' and z.d <= 1.8:
                    z.d += 0.5
            return
        self.vida -= dano
        self.golpeado = True

    # --- Las cajas, como en curva.py ---------------------------------------------------------

    def cajas_hasta(self, t):
        for tipo in ('balas', 'vida', 'arma'):
            while self.prox_caja[tipo] <= t:
                if self.rng.random() < self.perfil['cajas']:
                    if tipo == 'vida':
                        self.vida = min(float(self.vida_max), self.vida + self.cura_caja)
                    elif tipo == 'balas':
                        self.balas = self.max_balas
                        self.potencia, self.potencia_hasta = 1.5, self.prox_caja[tipo] + 10
                    else:
                        self.max_balas = max(self.max_balas, 1000)
                        self.balas = self.max_balas
                        self.potencia, self.potencia_hasta = 3.0, self.prox_caja[tipo] + 10
                self.prox_caja[tipo] += 8.0 if tipo != 'arma' else 20.0

    def avanzar(self, segundos):
        self.t += segundos
        self.cajas_hasta(self.t)

    # --- Lo que se mide antes de jugar una oleada paso a paso -----------------------------------

    def punteria_media(self):
        f = self.manual
        return f * self.perfil['punteria'] + (1 - f) * PUNTERIA_AUTOMATICA

    def cadencia_media(self):
        f = self.manual
        return f + (1 - f) * CADENCIA_AUTOMATICA

    def alcance_medio(self, tipo='normal'):
        """Hasta donde se le tira a ese tipo, en promedio sobre los rumbos: lo que se ve,
        topeado con lo que llega el arma (y con 16 m en automatico)."""
        if self.como_curva:
            return self.alcance_arma
        f = self.manual
        a_mano = alcance_medio_tipo(tipo, self.pantalla, 'ver', self.alcance_arma)
        if f >= 1.0:
            return a_mano
        a_auto = alcance_medio_tipo(tipo, self.pantalla, 'auto', min(ALCANCE_AUTOMATICO, self.alcance_arma))
        return f * a_mano + (1 - f) * a_auto

    def error_angular(self, sigma, alfa, apunto):
        """El desvio del tiro: normal, condicionado a que la bala del medio le pegue (|e| <= alfa)
        o no (|e| > alfa), asi la probabilidad de la bala del medio sigue siendo la punteria."""
        rng = self.rng
        c = NORMAL.cdf(alfa / sigma)
        if apunto:
            u = 0.5 + rng.random() * (c - 0.5)
        else:
            u = c + rng.random() * (1.0 - c)
        u = min(max(u, 0.5), 1.0 - 1e-12)
        e = sigma * NORMAL.inv_cdf(u)
        return e if rng.random() < 0.5 else -e

    def no_atraviesa(self, tipo, vel_zombi, vel_bala):
        """La probabilidad de que la bala no pase de largo (solo con la variante del tunel): la
        ventana es lo que mide el zombi mas la bala, el avance lo que se acercan por muestra."""
        clave = (tipo, vel_bala)
        p = self._tunel.get(clave)
        if p is None:
            ventana = 2 * (RADIO[tipo] + RADIO_BALA)
            avance = (vel_bala + vel_zombi) / min(self.fps, FISICA_HZ)
            p = self._tunel[clave] = min(1.0, ventana / avance)
        return p

    def balas_facil(self, n, mv, cantidad):
        """Las balas que gasta una oleada facil: mientras hay un zombi al alcance se tira, y se le
        sigue tirando hasta que llega la bala que lo mata (las que siguen en el aire se pierden o
        le pegan a otro, como en el paso a paso). Los zombis entran al alcance cada 0,35 s: si
        el que entro todavia ocupa el arma cuando entra el siguiente, se tira sin parar."""
        hab = curva.habilitados(n)
        total = sum(t[7] for t in hab)
        sin_techo = self.st['tiros'] * self.m_cad
        tps = min(TECHO_TIROS, sin_techo * self.cadencia_arma)
        compensacion = min(TECHO_TIROS, sin_techo) / max(1e-9, tps)
        tps *= self.cadencia_media()
        dano = self.st['dano'] * self.m_dano * compensacion * (1 + self.crit)
        if self.arma == ESCOPETA:
            dano *= EFICACIA_ESCOPETA_LEJOS
        p = self.punteria_media()
        ocupado = 0.0
        for t in hab:
            golpes = max(1, math.ceil(t[1] * mv / dano - 1e-9))
            vuelo = self.alcance_medio(t[0]) / (self.vel_bala + t[3])
            ocupado += t[7] / total * min(self.aparicion, golpes / p / tps + vuelo)
        # Las que llegan a un muerto y le pegan al que viene (reaprovecha) lo matan antes y
        # dejan huecos sin tirar: calibrado contra el paso a paso (+-10 %, validar_balas en el README).
        tiros = tps * cantidad * ocupado / (1 + 0.5 * (self.perfil['reaprovecha'] - 0.3))
        if self.cadencia_gratis:
            # Cada tiro cuesta 1 / el multiplicador de cadencia de las cartas.
            return tiros / self.m_cad
        return tiros

    def dps(self):
        """El danio por segundo sin cajas ni furia (la compensacion lo hace igual con cualquier
        arma), y el de los drones."""
        tiros = min(TECHO_TIROS, self.st['tiros'] * self.m_cad) * self.cadencia_media()
        dano = self.st['dano'] * self.m_dano
        return tiros * dano * (1 + self.crit), self.drones * dano

    def facil(self, n, mv):
        """Si la oleada se resuelve sin paso a paso: todos mueren antes de llegar. La presion de
        curva.py (los que llegan sobre los que se matan) por debajo de 0,6, achicado con el
        alcance (con la escopeta, 7 m, los zombis llegan aunque se maten rapido), y cada tipo se
        muere antes de cruzar el alcance."""
        hab = curva.habilitados(n)
        total = sum(t[7] for t in hab)
        dps_arma, dps_dron = self.dps()
        dano_tiro = self.st['dano'] * self.m_dano * (1 + self.crit)
        necesario = sum(t[7] / total * max(dano_tiro, t[1] * mv) for t in hab)
        rho = (1.0 / self.aparicion) * necesario / max(1e-9, dps_arma + dps_dron)
        alcance = self.alcance_medio()
        if rho >= 0.6 * min(1.0, alcance / ALCANCE_CURVA):
            return False, rho
        if self.como_curva:
            return True, rho
        punteria = self.punteria_media()
        if self.fps:
            punteria *= sum(t[7] / total * self.no_atraviesa(t[0], t[3], self.vel_bala) for t in hab)
        dps_real = dps_arma * punteria + dps_dron
        for t in hab:
            if t[1] * mv / max(1e-9, dps_real) >= self.alcance_medio(t[0]) / t[3]:
                return False, rho
        return True, rho

    # --- La partida --------------------------------------------------------------------------

    def jugar(self):
        rng = self.rng
        var = self.var
        for n in range(1, MAX_OLEADAS + 1):
            self.oleada = n
            if var.get('como_curva'):
                mv = curva.dos_tramos(CRECE_VIDA, 11, CRECE_VIDA_TARDIO, n)
                md = curva.dos_tramos(CRECE_GOLPE, 11, CRECE_GOLPE_TARDIO, n)
            elif var.get('tercer_tramo'):
                mv = por_oleada3(CRECE_VIDA, n, SEGUNDO_TRAMO, var['vida_tardia'], var['tercer_tramo'],
                                 var['vida_tercera'])
                md = por_oleada3(CRECE_GOLPE, n, SEGUNDO_TRAMO, var['golpe_tardio'], var['tercer_tramo'],
                                 var['golpe_tercero'])
            else:
                mv = por_oleada(CRECE_VIDA, n, SEGUNDO_TRAMO, var['vida_tardia'])
                md = por_oleada(CRECE_GOLPE, n, SEGUNDO_TRAMO, var['golpe_tardio'])
            self.mm = CRECE_MONEDAS ** (n - 1) * self.st['botin']
            if self.valor_moneda != 1.0:
                # La moneda de los zombis vale el doble (el multiplicadorMonedas del WaveManager).
                self.mm *= self.valor_moneda
            cantidad = var['zombis_base'] + var['zombis_por_oleada'] * n
            jefe = n % 10 == 0
            self.avanzar(DESCANSO)

            facil, _ = (False, None) if jefe else self.facil(n, mv)
            # El cargador no se llena al terminar la oleada (en el juego solo lo llenan las cajas
            # y el revivir): la facil gasta lo suyo, y si no alcanza se juega paso a paso.
            gasto = 0.0
            if facil and not self.como_curva:
                gasto = self.balas_facil(n, mv, cantidad)
                if gasto > self.balas:
                    facil = False
            self.golpeado = False
            if facil:
                frac = self.frac_monedas
                for _ in range(cantidad):
                    tp = curva.sortear_tipo(n, rng)
                    pal = rng.randint(tp[4], tp[5])
                    self.monedas += sum(1 for _ in range(pal) if rng.random() < frac) * self.mm
                    if var['cartas']:
                        self.sumar_experiencia(EXPERIENCIA[tp[0]])
                        if self.pendientes:
                            self.resolver_elecciones()
                            frac = self.frac_monedas
                    if self.vampiro > 0:
                        # Como en matar: cura cada 15 muertes en cualquier oleada.
                        self.bajas_vampiro += 1
                        if self.bajas_vampiro >= 15:
                            self.bajas_vampiro = 0
                            self.vida = min(float(self.vida_max),
                                            self.vida + max(1, round(self.vida_max * self.vampiro)))
                self.golpeado = rng.random() >= self.perfil['perfecta']
                dur = cantidad * self.aparicion + 2.0
                if self.como_curva:
                    self.avanzar(dur)
                    self.balas = self.max_balas
                else:
                    # Se gasta de a segundos, con las cajas que salen en el medio.
                    trozos = max(1, int(dur))
                    for _ in range(trozos):
                        self.balas = max(0, int(round(self.balas - gasto / trozos)))
                        self.avanzar(dur / trozos)
            else:
                if not self.jugar_oleada(n, cantidad, jefe, mv, md):
                    return self.resultado(n - 1, n)
            if var.get('recarga'):
                # Candidata: el cargador se llena al terminar cada oleada (hoy no: solo las cajas).
                self.balas = self.max_balas
            self.monedas += 4 * n * (1 if self.golpeado else 2)
            if n >= 3 and rng.random() < 0.25 and rng.random() < self.perfil['tesoro']:
                pal = rng.randint(14, 20)
                self.monedas += sum(1 for _ in range(pal) if rng.random() < self.frac_monedas) * self.mm
                if var['cartas']:
                    self.sumar_experiencia(EXPERIENCIA_TESORO)
                    self.resolver_elecciones()
            self.poder_por_oleada.append(self.poder)
            self.min_por_oleada.append((self.t + self.elecciones * SEGUNDOS_POR_ELECCION) / 60.0)
            self.monedas_por_oleada.append(self.monedas)
        return self.resultado(MAX_OLEADAS, None)

    def resultado(self, oleada, murio_en):
        return dict(oleada=oleada, monedas=self.monedas, segundos=self.t, elecciones=self.elecciones,
                    doradas=self.doradas, murio_en=murio_en, arma_inicio=self.arma_inicio, arma_fin=self.arma,
                    revivio=self.revivio,
                    sin_balas=self.pasos_sin_balas / max(1, self.pasos_con_blanco),
                    veces=dict(self.veces), poder_por_oleada=self.poder_por_oleada,
                    min_por_oleada=self.min_por_oleada, monedas_por_oleada=self.monedas_por_oleada)

    def nuevo_zombi(self, tp, mv, md, d):
        """Un zombi con su rumbo al azar, que dice hasta donde se lo ve."""
        if self.pantalla is None:
            return Zombi(tp, mv, md, d)
        return Zombi(tp, mv, md, d, self.pantalla, self.rng.randrange(RUMBOS))

    def revivir(self):
        """OfertaDeRevivir: una vez por partida, si hay video y el jugador lo acepta (la
        probabilidad de la variante: 1 es el techo, siempre que se puede)."""
        prob = self.var.get('revivir', 0.0)
        if self.revivio or prob <= 0:
            return False
        if self.segundos_previos + self.t < REVIVIR_DESDE or self.t < REVIVIR_PARTIDA:
            return False
        if self.rng.random() >= prob:
            return False
        self.revivio = True
        self.vida = float(self.vida_max)
        self.balas = max(self.balas, min(REVIVIR_BALAS, self.max_balas))
        for z in self.vivos:
            if not z.vivo:
                continue
            if z.tipo == 'jefe':
                z.ataque = max(z.ataque, self.t + REVIVIR_GRACIA)
            elif z.d <= REVIVIR_DESPEJE:
                # Fuera del mapa sin puntos, monedas ni experiencia; la oleada lo cuenta muerto.
                z.vivo = False
        self.vivos = [z for z in self.vivos if z.vivo]
        self.gracia_hasta = self.t + REVIVIR_GRACIA
        return True

    def atacar_jefe(self, jefe_z, t, mv, md, vivos):
        """Un patron del jefe (JefePatrones): en furia, cada tres ataques invoca si hay lugar;
        si no, salto y carga alternados, empezando por el salto."""
        perfil = self.perfil
        furioso = jefe_z.hp < 0.5 * jefe_z.hp_max
        if self.como_curva:
            # Como curva.py: un ataque cada 5 s, sin patron que lo frene.
            jefe_z.ataque = t + 5.0 * (0.65 if furioso else 1.0)
            jefe_z.ataques += 1
            if furioso and jefe_z.ataques % 3 == 0:
                for _ in range(6):
                    if len(vivos) < self.techo:
                        vivos.append(self.nuevo_zombi(curva.TIPOS[0], mv, md, max(2.0, jefe_z.d)))
            elif self.rng.random() >= perfil['esquiva_jefe']:
                self.recibir(jefe_z.dano * (1.5 if jefe_z.ataques % 2 == 1 else 2.5))
            return
        toca_invocar = furioso and jefe_z.sin_invocar >= 2
        if toca_invocar and len(vivos) < self.techo:
            patron = 'invocar'
            jefe_z.sin_invocar = 0
            for _ in range(6):
                if len(vivos) < self.techo:
                    vivos.append(self.nuevo_zombi(curva.TIPOS[0], mv, md, max(2.0, jefe_z.d)))
        else:
            # Sin lugar para invocar, esa vez carga.
            patron = 'carga' if toca_invocar or not jefe_z.toca_salto else 'salto'
            jefe_z.toca_salto = patron != 'salto'
            jefe_z.sin_invocar += 1
            if self.rng.random() >= perfil['esquiva_jefe']:
                self.recibir(jefe_z.dano * (1.5 if patron == 'salto' else 2.5))
            if patron == 'salto':
                jefe_z.d = min(jefe_z.d, JEFE_TRAS_EL_SALTO)
            else:
                jefe_z.d = abs(jefe_z.d - JEFE_LARGO_DE_LA_CARGA)
        jefe_z.ataques += 1
        jefe_z.patron_hasta = t + JEFE_PATRON[patron]
        jefe_z.ataque = jefe_z.patron_hasta + JEFE_CADA * (0.65 if furioso else 1.0)

    def jugar_oleada(self, n, cantidad, jefe, mv, md):
        """Paso a paso. Devuelve False si el jugador muere (y no revive)."""
        rng = self.rng
        perfil = self.perfil
        techo = self.techo
        como_curva = self.como_curva
        cola = []
        if jefe:
            cola.append(curva.JEFE)
        for _ in range(cantidad):
            cola.append(curva.sortear_tipo(n, rng))
        cola.reverse()
        self.vivos = []
        self.en_vuelo = []
        prox_aparicion = self.t
        acumulador = 0.0
        inicio = self.t
        jefe_z = None
        fps = self.fps

        while cola or self.vivos:
            t = self.t
            self.cajas_hasta(t)
            if t - inicio > MAX_SEGUNDOS_OLEADA:
                return False
            vivos = self.vivos
            if cola and t >= prox_aparicion and len(vivos) < techo:
                tp = cola.pop()
                z = self.nuevo_zombi(tp, mv, md, NACEN_A)
                vivos.append(z)
                if tp[0] == 'jefe':
                    jefe_z = z
                    z.ataque = t + 3.0
                    prox_aparicion = t + 1.5
                else:
                    prox_aparicion = t + self.aparicion

            # Caminan hacia el jugador; el hielo los frena (al jefe tambien, pero no sus ataques).
            # El jefe no camina mientras hace un patron.
            for z in vivos:
                if z.d > 0 and t >= z.patron_hasta:
                    v = z.vel * (1 - z.lentitud) if t < z.hielo_hasta else z.vel
                    z.d = max(0.0, z.d - v * DT)

            # La furia: apenas se pone feo.
            if self.con_furia and t >= self.furia_lista and (len(vivos) >= 8 or (jefe_z is not None and jefe_z.vivo)):
                self.furia_hasta = t + self.dur_furia
                self.furia_lista = t + 120.0
            furia = 2.0 if t < self.furia_hasta else 1.0

            # El arma. A mano (una fraccion de cada segundo, segun el perfil): toda la cadencia y la
            # punteria del perfil. En automatico: media cadencia, punteria fija y hasta 16 m. Las
            # dos, hasta donde llega el arma y solo contra lo que se ve (el rumbo de cada zombi).
            manual = (self.paso % 10) < self.ciclo_manual
            self.paso += 1
            alcance = self.alcance_arma
            if manual:
                en_alcance = [z for z in vivos if z.d <= alcance and z.d <= z.r_mano]
            else:
                alcance = min(ALCANCE_AUTOMATICO, alcance)
                en_alcance = [z for z in vivos if z.d <= alcance and z.d <= z.r_auto]
            # Los que estan al alcance en este paso: a los que puede pasar una bala que sigue.
            self.cerca = en_alcance
            if en_alcance:
                self.pasos_con_blanco += 1
                if self.balas <= 0:
                    self.pasos_sin_balas += 1
            if en_alcance and self.balas > 0:
                pot = self.potencia if t < self.potencia_hasta else 1.0
                sin_techo = self.st['tiros'] * pot * furia * self.m_cad
                tps = min(TECHO_TIROS, sin_techo)
                tps_arma = min(TECHO_TIROS, sin_techo * self.cadencia_arma)
                compensacion = tps / tps_arma
                if not manual:
                    tps_arma *= CADENCIA_AUTOMATICA
                acumulador += tps_arma * DT
                objetivo = min(en_alcance, key=lambda z: z.d)
                d_ef = max(objetivo.d, DISTANCIA_PEGADO)
                punteria = perfil['punteria'] if manual else PUNTERIA_AUTOMATICA
                abanico = self.abanico
                if abanico:
                    sigma = self.sigma_mano if manual else self.sigma_auto
                    alfa = math.atan((objetivo.radio + RADIO_BALA) / d_ef)
                e = 0.0
                pasa = 1.0 if not fps else self.no_atraviesa(objetivo.tipo, objetivo.vel, self.vel_bala)
                dano_tiro = self.st['dano'] * furia * self.m_dano * compensacion
                vel_bala = self.vel_bala
                n_horda = horda(len(en_alcance))
                quema_p = self.quema_p
                paquetes = {}
                cadencia_gratis = self.cadencia_gratis
                costo_tiro = 1.0 / self.m_cad
                while acumulador >= 1 and self.balas > 0:
                    acumulador -= 1
                    if cadencia_gratis:
                        # Los tiros de mas de la carta de cadencia no gastan: cada tiro suma
                        # 1 / m_cad a la deuda, y la bala se descuenta cuando llega a una entera.
                        self.deuda_balas += costo_tiro
                        if self.deuda_balas >= 1.0 - 1e-9:
                            self.deuda_balas -= 1.0
                            self.balas -= 1
                    else:
                        self.balas -= 1
                    dano = dano_tiro * (2.0 if (self.crit >= 1.0 or rng.random() < self.crit) else 1.0)
                    apunto = rng.random() < punteria
                    if abanico:
                        e = self.error_angular(sigma, alfa, apunto)
                    balas = [(angulo, dano * parte) for angulo, parte in self.patron]
                    # La de atras: con el tiro entero (la escopeta tira uno gordo), contra la horda.
                    balas += [(None, dano)] * self.atras
                    for angulo, d_bala in balas:
                        blanco = None
                        if angulo is not None and (abs(angulo + e) <= alfa if abanico else apunto):
                            if pasa >= 1.0 or rng.random() < pasa:
                                blanco = objetivo
                        if blanco is None:
                            # La que no va al blanco (o lo atraveso) le puede pegar a otro.
                            if rng.random() >= n_horda:
                                continue
                            blanco = en_alcance[rng.randrange(len(en_alcance))]
                        pq = paquetes.get(blanco)
                        if pq is None:
                            paquetes[blanco] = pq = [d_bala, 1, 0.0]
                        else:
                            pq[0] += d_bala
                            pq[1] += 1
                        # La quemadura se sortea bala por bala; quema la mas fuerte.
                        if quema_p > 0 and rng.random() < quema_p:
                            pq[2] = max(pq[2], d_bala * 0.1)
                for blanco, (d_paquete, n_balas, quema) in paquetes.items():
                    self.lanzar(blanco, d_paquete, vel_bala, n_balas, quema)
            else:
                acumulador = 0.0

            # Los drones: una bala de la pistola por segundo, al mas cercano a 12 m que se ve, con
            # el danio entero (sin critico ni compensacion), aunque el jugador no tenga balas.
            if self.drones:
                for i in range(self.drones):
                    if t < self.dron_prox[i]:
                        continue
                    self.dron_prox[i] = t + 1.0
                    cerca = [z for z in vivos if z.d <= ALCANCE_DRON and z.d <= z.r_mano]
                    if not cerca:
                        continue
                    blanco = min(cerca, key=lambda z: z.d)
                    if rng.random() >= PUNTERIA_DEL_DRON or (
                            fps and rng.random() >= self.no_atraviesa(blanco.tipo, blanco.vel, VEL_DRON)):
                        if rng.random() >= horda(len(cerca)):
                            continue
                        blanco = cerca[rng.randrange(len(cerca))]
                    d_dron = self.st['dano'] * furia * self.m_dano
                    quema = d_dron * 0.1 if self.quema_p > 0 and rng.random() < self.quema_p else 0.0
                    self.lanzar(blanco, d_dron, VEL_DRON, 1, quema, libres=0)

            # Las balas que llegan.
            en_vuelo = self.en_vuelo
            while en_vuelo and en_vuelo[0][0] <= t:
                _, _, z, dano, n_balas, quema, libres, carta = heapq.heappop(en_vuelo)
                self.llegar(z, dano, n_balas, quema, libres, carta)

            # La granada, como en curva.py, con la recarga de las cartas.
            if self.con_granada and t >= self.granada_lista:
                cerca = [z for z in vivos if z.vivo and z.d <= 5]
                duros = any(z.tipo in ('tanque', 'jefe') and z.d <= 6 for z in cerca)
                if len(cerca) >= perfil['umbral_granada'] or duros:
                    self.granada_lista = t + self.recarga_granada
                    for z in cerca:
                        if z.vivo and rng.random() < perfil['granada']:
                            z.hp -= 10 * mv
                            if z.hp <= 0.01:
                                self.matar(z)

            # Los ataques del jefe: a menos de 15 m y si se lo ve, cuando termino el patron de
            # antes y pasaron sus 5 s (x0,65 en furia).
            if (jefe_z is not None and jefe_z.vivo and t >= jefe_z.ataque and t >= jefe_z.patron_hasta
                    and jefe_z.d <= JEFE_ALCANCE and jefe_z.d <= jefe_z.r_jefe):
                self.atacar_jefe(jefe_z, t, mv, md, vivos)

            # La quemadura (un tick cada medio segundo, seis en total), y los que estan pegados
            # pegan cada 0,8 s, segun cuanto les escapa el jugador. El jefe tambien, con su golpe,
            # salvo mientras hace un patron (EnemyController.puedeZarpar).
            horda_cerca = 0.6 * (len(vivos) / HORDA_DE_REFERENCIA) ** 1.5
            for z in vivos:
                if z.quema_ticks > 0 and z.vivo and t >= z.quema_prox:
                    z.quema_ticks -= 1
                    z.quema_prox += 0.5
                    z.hp -= z.quema_tick
                    if z.hp <= 0.01:
                        self.matar(z)
                if z.vivo and z.d <= 0 and t >= z.reloj and t >= z.patron_hasta and (
                        z.tipo != 'jefe' or not como_curva):
                    z.reloj = t + JEFE_ZARPAZO
                    v = z.vel * (1 - z.lentitud) if t < z.hielo_hasta else z.vel
                    f = min(1.0, (v / self.vel_jugador) ** 2 + horda_cerca)
                    if rng.random() < perfil['exposicion'] * f:
                        self.recibir(z.dano)
            self.vivos = [z for z in vivos if z.vivo]

            if self.vida <= 0 and not self.revivir():
                return False
            self.t += DT
        return True

    def lanzar(self, blanco, dano, vel, n, quema, libres=None):
        """Las n balas de este paso que van al mismo blanco: llegan cuando cruzan la distancia."""
        self.seq += 1
        heapq.heappush(self.en_vuelo, (self.t + blanco.d / vel, self.seq, blanco, dano, n, quema,
                                       self.perf_libres if libres is None else libres, self.perf_carta))


# --- Las variantes -----------------------------------------------------------------------------

BASE = dict(cartas=True, politica='equilibrada', valor_dano=1.25, tope_dano=4, valor_cadencia=1.2, valor_critico=0.1,
            valor_vida=1.2, crece_costo=CRECE_COSTO, armas=True, auto=True,
            vida_tardia=CRECE_VIDA_TARDIO, golpe_tardio=CRECE_GOLPE_TARDIO,
            pantalla='telefono', techo=TECHO_VIVOS, revivir=0.0, fps=None,
            aparicion=APARICION, zombis_base=10, zombis_por_oleada=4)
# La PC: siempre a mano, la pantalla 16:9 y el techo de 60 vivos.
PC = dict(auto=False, pantalla='pc', techo=TECHO_PC)


def variante(nombre, **cambios):
    v = dict(BASE)
    v.update(cambios)
    v['nombre'] = nombre
    return v


VARIANTES = {
    'base': variante('1.5.2 sin cartas', cartas=False, politica=None),
    'c_dano': variante('1.6.0, danio primero', politica='dano'),
    'c_equil': variante('1.6.0, equilibrada', politica='equilibrada'),
    'c_azar': variante('1.6.0, al azar', politica='azar'),
    'd120_dano': variante('danio x1,20, danio primero', politica='dano', valor_dano=1.20),
    'd120_equil': variante('danio x1,20, equilibrada', valor_dano=1.20),
    'd115_dano': variante('danio x1,15 tope 3, danio primero', politica='dano', valor_dano=1.15, tope_dano=3),
    'd115_equil': variante('danio x1,15 tope 3, equilibrada', valor_dano=1.15, tope_dano=3),
    'sin_armas': variante('sin armas (todo pistola), equilibrada', armas=False),
    'sin_escopeta': variante('sin escopeta (revolver), equilibrada', sin_escopeta=True),
    'base_sin_armas': variante('1.5.2 sin cartas, sin armas (todo pistola)', cartas=False, politica=None, armas=False),
    'base_sin_escopeta': variante('1.5.2 sin cartas, sin escopeta (revolver)', cartas=False, politica=None, sin_escopeta=True),
    'pc': variante('PC (a mano, 16:9, techo 60), equilibrada', **PC),
    'pc_base': variante('PC (a mano, 16:9, techo 60) sin cartas', cartas=False, politica=None, **PC),
    'tel_manual': variante('telefono sin auto-apuntado, equilibrada', auto=False),
    'tel_manual_base': variante('telefono sin auto-apuntado, sin cartas', auto=False, cartas=False, politica=None),
    # El revivir con un video, siempre que se puede (el techo: en el juego depende de que haya
    # video y de que el jugador lo quiera ver).
    'base_revivir': variante('1.5.2 sin cartas, revive siempre', cartas=False, politica=None, revivir=1.0),
    'c_equil_revivir': variante('1.6.0 equilibrada, revive siempre', revivir=1.0),
    # El tunel de las balas rapidas, con el telefono a 30 FPS (sin medir: ver el README).
    'base_30fps': variante('1.5.2 sin cartas, tunel a 30 FPS', cartas=False, politica=None, fps=30),
    'c_equil_30fps': variante('1.6.0 equilibrada, tunel a 30 FPS', fps=30),
    'sin_escopeta_30fps': variante('sin escopeta (revolver), equilibrada, tunel a 30 FPS', sin_escopeta=True, fps=30),
    'calibracion': variante('calibracion: como curva.py', cartas=False, politica=None, armas=False,
                            auto=False, como_curva=True, pantalla=None),
    # Candidatas para devolver el muro a la 50-60 con partidas de 25-30 minutos (no las pidio nadie:
    # son para comparar), todas con la equilibrada.
    'suaves': variante('candidata: numeros a la mitad (danio x1,12, cadencia y vida x1,1, critico +5 %)',
                       valor_dano=1.12, valor_cadencia=1.1, valor_vida=1.1, valor_critico=0.05),
    'costo125': variante('candidata: el PODER crece x1,25 (menos cartas)', crece_costo=1.25),
    'curva112': variante('candidata: vida x1,12 y golpe x1,05 desde la 11', vida_tardia=1.12, golpe_tardio=1.05),
    'recarga': variante('candidata: el cargador se llena al terminar cada oleada', recarga=True),
    'base_recarga': variante('1.5.2 sin cartas, con el cargador lleno al terminar cada oleada', cartas=False, politica=None,
                             recarga=True),
    'olas_cortas': variante('candidata: oleadas de 10 + 3n, uno cada 0,25 s', zombis_por_oleada=3, aparicion=0.25),
    'combo_a': variante('candidata A: PODER x1,25, vida x1,12 y golpe x1,05, 10 + 3n cada 0,25 s', crece_costo=1.25,
                        vida_tardia=1.12, golpe_tardio=1.05, zombis_por_oleada=3, aparicion=0.25),
    'combo_b': variante('candidata B: la A con el cargador lleno en cada oleada', crece_costo=1.25, vida_tardia=1.12,
                        golpe_tardio=1.05, zombis_por_oleada=3, aparicion=0.25, recarga=True),
    'combo_c': variante('candidata C: la B con uno cada 0,20 s', crece_costo=1.25, vida_tardia=1.12, golpe_tardio=1.05,
                        zombis_por_oleada=3, aparicion=0.20, recarga=True),
    'combo_d': variante('candidata D: PODER x1,25, vida x1,14 y golpe x1,06, 10 + 2n cada 0,25 s, cargador lleno',
                        crece_costo=1.25, vida_tardia=1.14, golpe_tardio=1.06, zombis_por_oleada=2, aparicion=0.25,
                        recarga=True),
    'combo_e': variante('candidata E: el PODER como esta, vida x1,15 y golpe x1,07, 10 + 2n cada 0,25 s, cargador lleno',
                        vida_tardia=1.15, golpe_tardio=1.07, zombis_por_oleada=2, aparicion=0.25, recarga=True),
}
# Lo que decidio Gemini (13/10, CARTAS.md): oleadas de 10 + 2n, uno cada 0,20 s, la moneda de los
# zombis x2 en valor; la curva de la 1.5.2 hasta la 30 y un tercer tramo desde la 31 (vida x1,15,
# golpe x1,08); el cargador lleno al terminar cada oleada, los tiros de mas de la carta de cadencia
# sin costo de balas y el rifle a cadencia x1; el PODER como esta (12 x 1,15^L).
GEMINI = dict(zombis_por_oleada=2, aparicion=0.20, valor_moneda=2.0,
              tercer_tramo=TERCER_TRAMO, vida_tercera=CRECE_VIDA_TERCERO, golpe_tercero=CRECE_GOLPE_TERCERO,
              recarga=True, cadencia_gratis=True, cadencia_rifle=1.0)
VARIANTES.update({
    'gemini': variante('gemini: la decision del 13/10, equilibrada', **GEMINI),
    'gemini_base': variante('gemini sin cartas', cartas=False, politica=None, **GEMINI),
    # Para ver cuanto pesa cada pieza (de a una, sacada de la decision): el tercer tramo, la
    # cadencia sin costo y la moneda x2; y la decision con la curva de la candidata E (vida x1,15 y
    # golpe x1,07 desde la 11, sin tercer tramo).
    'gemini_sin_tercero': variante('gemini sin el tercer tramo, equilibrada', **dict(GEMINI, tercer_tramo=0)),
    'gemini_cadencia_con_costo': variante('gemini con la cadencia gastando balas como hoy, equilibrada',
                                          **dict(GEMINI, cadencia_gratis=False)),
    'gemini_moneda_x1': variante('gemini con la moneda x1, equilibrada', **dict(GEMINI, valor_moneda=1.0)),
    'gemini_curva_e': variante('gemini con la curva de la E (x1,15 y x1,07 desde la 11, sin tercer tramo), equilibrada',
                               **dict(GEMINI, tercer_tramo=0, vida_tardia=1.15, golpe_tardio=1.07)),
    # Con el muro de la decision en la 69-82 y no en la 50-55 (resultados_gemini.md), cuanto hay que
    # mover el tercer tramo para llegar: mas temprano, mas fuerte, y con la cadencia gastando balas.
    'gemini_t21': variante('gemini con el tercer tramo desde la 21 (x1,15 y x1,08), equilibrada',
                           **dict(GEMINI, tercer_tramo=21)),
    'gemini_t26_fuerte': variante('gemini con el tercer tramo desde la 26 (x1,20 y x1,10), equilibrada',
                                  **dict(GEMINI, tercer_tramo=26, vida_tercera=1.20, golpe_tercero=1.10)),
    'gemini_t31_fuerte': variante('gemini con el tercer tramo desde la 31 (x1,25 y x1,12), equilibrada',
                                  **dict(GEMINI, vida_tercera=1.25, golpe_tercero=1.12)),
    'gemini_t21_costo': variante('gemini con el tercer tramo desde la 21 y la cadencia gastando balas, equilibrada',
                                 **dict(GEMINI, tercer_tramo=21, cadencia_gratis=False)),
    # Lo que cerro Argon (13/10, CARTAS.md): el tercer tramo desde la 26 con x1,20 y x1,10, y la moneda
    # x1,4 en vez de x2. Lo demas, como la decision de Gemini.
    'final_1_6_0': variante('1.6.0 final: tercer tramo desde la 26 (x1,20 y x1,10), moneda x1,4, equilibrada',
                            **dict(GEMINI, tercer_tramo=26, vida_tercera=1.20, golpe_tercero=1.10, valor_moneda=1.4)),
    'final_1_6_0_base': variante('1.6.0 final sin cartas', cartas=False, politica=None,
                                 **dict(GEMINI, tercer_tramo=26, vida_tercera=1.20, golpe_tercero=1.10, valor_moneda=1.4)),
})


# --- La campania: partida tras partida, comprando entre una y otra ----------------------------

def campana(args):
    clave, var, perfil_nombre, semilla, partidas = args
    rng = random.Random(semilla)
    perfil = perfil_de(perfil_nombre)
    niv = {m: 0 for m in curva.ORDEN}
    banco = 0.0
    filas = []
    fotos = {}
    poder_suma = [0] * (MAX_OLEADAS + 1)
    poder_cuenta = [0] * (MAX_OLEADAS + 1)
    # Los minutos de partida al terminar cada oleada, de todas las partidas que la terminan.
    minutos = [[] for _ in range(MAX_OLEADAS + 1)]
    oro = [[] for _ in range(MAX_OLEADAS + 1)]
    segundos = 0.0
    for p in range(1, partidas + 1):
        if p in (10, 30) or p == partidas:
            fotos[p] = dict(niv)
        r = Partida(niv, perfil, var, rng, segundos).jugar()
        segundos += r['segundos']
        banco = curva.comprar(niv, banco + r['monedas'], MEJORAS, perfil)
        for i, poder in enumerate(r.pop('poder_por_oleada')):
            poder_suma[i + 1] += poder
            poder_cuenta[i + 1] += 1
        for i, m in enumerate(r.pop('min_por_oleada')):
            minutos[i + 1].append(m)
        for i, m in enumerate(r.pop('monedas_por_oleada')):
            oro[i + 1].append(m)
        r['partida'] = p
        r['dano'] = niv['dano']
        filas.append(r)
    return dict(clave=clave, perfil=perfil_nombre, semilla=semilla, filas=filas, fotos=fotos,
                poder=[s / c if c else None for s, c in zip(poder_suma, poder_cuenta)], minutos=minutos, oro=oro)


# --- Los resumenes ----------------------------------------------------------------------------

def mediana(xs, f='%.0f'):
    xs = [x for x in xs if x is not None]
    return f % statistics.median(xs) if xs else '—'


def mejor_hasta(filas, p):
    return max(f['oleada'] for f in filas[:p])


def primera_en(filas, meta):
    for f in filas:
        if f['oleada'] >= meta:
            return f['partida']
    return None


def ultimas(camps, k):
    return [f for c in camps for f in c['filas'][-k:]]


def tabla_principal(res, claves, perfil, partidas):
    lineas = ['', '### Perfil %s' % perfil, '',
              '| variante | mejor p10 | mejor p30 | muro (ult. 20) | mejor al final | llega a 40 | a 50 | a 60 | a 80 '
              '| al tope (100) | min. en el muro | cartas por partida | % muertes en jefe | arma en el muro '
              '| sin balas con blanco | revivio |',
              '|---|' + '---|' * 15]
    for clave in claves:
        camps = [c for c in res if c['clave'] == clave and c['perfil'] == perfil]
        if not camps:
            continue
        ult = ultimas(camps, 20)
        celdas = [mediana([mejor_hasta(c['filas'], p) for c in camps if len(c['filas']) >= p]) for p in (10, 30)]
        celdas.append(mediana([f['oleada'] for f in ult]))
        celdas.append(mediana([max(f['oleada'] for f in c['filas']) for c in camps]))
        for meta in (40, 50, 60, 80):
            vals = [primera_en(c['filas'], meta) for c in camps]
            ok = [v for v in vals if v is not None]
            celdas.append('%d' % statistics.median(ok) if len(ok) * 2 >= len(vals) else '—')
        celdas.append('%.0f %%' % (100.0 * sum(1 for f in ult if f['oleada'] >= MAX_OLEADAS) / len(ult)))
        celdas.append(mediana([(f['segundos'] + f['elecciones'] * SEGUNDOS_POR_ELECCION) / 60 for f in ult], '%.1f'))
        celdas.append(mediana([f['elecciones'] for f in ult]))
        muertes = [f for c in camps for f in c['filas'][-100:] if f['murio_en']]
        en_jefe = 100.0 * sum(1 for f in muertes if f['murio_en'] % 10 == 0) / max(1, len(muertes))
        celdas.append('%.0f %%' % en_jefe)
        armas = [f['arma_fin'] for f in ult]
        celdas.append(NOMBRE_ARMA[statistics.median_low(armas)])
        celdas.append('%.0f %%' % (100 * statistics.mean(f['sin_balas'] for f in ult)))
        celdas.append('%.0f %%' % (100.0 * sum(1 for f in ult if f['revivio']) / len(ult)) if VARIANTES[clave]['revivir'] else '—')
        lineas.append('| %s | %s |' % (VARIANTES[clave]['nombre'], ' | '.join(celdas)))
    return lineas


def tabla_arranque(res, claves, perfil):
    lineas = ['', '### Perfil %s: las primeras 12 partidas (oleada a la que se llega, mediana; entre parentesis '
              'el arma con que se arranca)' % perfil, '',
              '| variante | ' + ' | '.join('p%d' % i for i in range(1, 13)) + ' |', '|---|' + '---|' * 12]
    for clave in claves:
        camps = [c for c in res if c['clave'] == clave and c['perfil'] == perfil]
        if not camps:
            continue
        celdas = []
        for i in range(12):
            ol = statistics.median(c['filas'][i]['oleada'] for c in camps)
            arma = statistics.median_low(c['filas'][i]['arma_inicio'] for c in camps)
            celdas.append('%d (%s)' % (ol, NOMBRE_ARMA[arma][:3]))
        lineas.append('| %s | %s |' % (VARIANTES[clave]['nombre'], ' | '.join(celdas)))
    return lineas


def minutos_de(f):
    """Lo que dura una partida de verdad: el tiempo de juego mas el de elegir las cartas."""
    return (f['segundos'] + f['elecciones'] * SEGUNDOS_POR_ELECCION) / 60.0


def oro_por_minuto(filas):
    """Las monedas de esas partidas sobre sus minutos, juntas (no la media de los cocientes)."""
    minutos = sum(minutos_de(f) for f in filas)
    return sum(f['monedas'] for f in filas) / minutos if minutos > 0 else 0.0


def tabla_oro(res, claves, perfil):
    """El oro por minuto a la partida 10 (y alrededor), y en el muro."""
    lineas = ['', '### Perfil %s' % perfil, '',
              '| variante | oleada en la p10 | monedas en la p10 | min. de la p10 | oro/min en la p10 | oro/min p8-p12 '
              '| oro/min en el muro (ult. 20) | monedas por partida en el muro |',
              '|---|' + '---|' * 7]
    for clave in claves:
        camps = [c for c in res if c['clave'] == clave and c['perfil'] == perfil]
        if not camps or any(len(c['filas']) < 12 for c in camps):
            continue
        p10 = [c['filas'][9] for c in camps]
        ventana = [f for c in camps for f in c['filas'][7:12]]
        ult = ultimas(camps, 20)
        celdas = [mediana([f['oleada'] for f in p10]), mediana([f['monedas'] for f in p10]),
                  mediana([minutos_de(f) for f in p10], '%.1f'),
                  '%.0f' % oro_por_minuto(p10), '%.0f' % oro_por_minuto(ventana), '%.0f' % oro_por_minuto(ult),
                  mediana([f['monedas'] for f in ult])]
        lineas.append('| %s | %s |' % (VARIANTES[clave]['nombre'], ' | '.join(celdas)))
    return lineas


OLEADAS_TABLA_MINUTOS = [10, 20, 30, 40, 50, 55, 60, 70]


def tabla_minutos(res, claves, perfil):
    """Los minutos de partida (con las elecciones) y el oro juntado al terminar cada oleada, de las
    partidas que la terminan (la mediana de todas las de todas las semillas)."""
    lineas = ['', '### Perfil %s' % perfil, '',
              '| variante | ' + ' | '.join('min. hasta la %d' % o for o in OLEADAS_TABLA_MINUTOS) + ' | '
              + ' | '.join('oro hasta la %d' % o for o in (10, 30, 50)) + ' |',
              '|---|' + '---|' * (len(OLEADAS_TABLA_MINUTOS) + 3)]
    for clave in claves:
        camps = [c for c in res if c['clave'] == clave and c['perfil'] == perfil]
        if not camps:
            continue
        celdas = []
        for o in OLEADAS_TABLA_MINUTOS:
            vals = [m for c in camps for m in c['minutos'][o]]
            celdas.append(mediana(vals, '%.1f') if len(vals) >= 10 else '—')
        for o in (10, 30, 50):
            vals = [m for c in camps for m in c['oro'][o]]
            celdas.append(mediana(vals) if len(vals) >= 10 else '—')
        lineas.append('| %s | %s |' % (VARIANTES[clave]['nombre'], ' | '.join(celdas)))
    return lineas


OLEADAS_TABLA_XP = [1, 2, 3, 4, 5, 6, 8, 10, 15, 20, 30, 40, 50, 60]


def tabla_cartas_por_oleada(res, claves, perfiles):
    lineas = ['', '### Cartas elegidas al terminar cada oleada (promedio de todas las partidas que la terminan)', '',
              '| variante | perfil | ' + ' | '.join(str(o) for o in OLEADAS_TABLA_XP) + ' |',
              '|---|---|' + '---|' * len(OLEADAS_TABLA_XP)]
    for clave in claves:
        if not VARIANTES[clave]['cartas']:
            continue
        for perfil in perfiles:
            camps = [c for c in res if c['clave'] == clave and c['perfil'] == perfil]
            if not camps:
                continue
            celdas = []
            for o in OLEADAS_TABLA_XP:
                vals = [c['poder'][o] for c in camps if c['poder'][o] is not None]
                celdas.append('%.1f' % statistics.mean(vals) if vals else '—')
            lineas.append('| %s | %s | %s |' % (VARIANTES[clave]['nombre'], perfil, ' | '.join(celdas)))
    return lineas


def tabla_cartas_elegidas(res, claves, perfil):
    ids = ORDEN_CARTAS
    lineas = ['', '### Perfil %s: cuantas veces tiene cada carta al morir en el muro (promedio de las ultimas 20 partidas)' % perfil, '',
              '| variante | ' + ' | '.join(ids) + ' | doradas |', '|---|' + '---|' * (len(ids) + 1)]
    for clave in claves:
        if not VARIANTES[clave]['cartas']:
            continue
        camps = [c for c in res if c['clave'] == clave and c['perfil'] == perfil]
        if not camps:
            continue
        ult = ultimas(camps, 20)
        celdas = ['%.1f' % statistics.mean(f['veces'].get(i, 0) for f in ult) for i in ids]
        celdas.append('%.1f' % statistics.mean(f['doradas'] for f in ult))
        lineas.append('| %s | %s |' % (VARIANTES[clave]['nombre'], ' | '.join(celdas)))
    return lineas


# --- Cuanto pesa cada carta: sola y al tope, con las mejoras congeladas ------------------------

def jugar_fijo(args):
    """Partidas con las mejoras de una foto, sin comprar: la carta 'fija' al tope desde el
    principio y ninguna otra (o ninguna, o todas)."""
    nombre, niv, perfil_nombre, semilla, partidas, fijas, recarga = args
    rng = random.Random(semilla)
    perfil = perfil_de(perfil_nombre)
    var = variante('fijo', cartas=False, politica=None, fijas=fijas, recarga=recarga)
    oleadas = [Partida(niv, perfil, var, rng).jugar()['oleada'] for _ in range(partidas)]
    return nombre, perfil_nombre, semilla, oleadas


def modo_pesos(op, hilos, salida):
    """Lee las fotos de las mejoras que dejo el modo curva (fotos.json) y juega cada carta sola."""
    with open(os.path.join(AQUI, 'fotos.json'), encoding='utf-8') as f:
        fotos = json.load(f)
    perfiles = op.get('perfiles', 'medio').split(',')
    partidas = int(op.get('partidas', 60))
    semillas = int(op.get('semillas', 3))
    momentos = op.get('momentos', '10,final').split(',')
    origen = op.get('origen', 'base')
    # recarga=1: el cargador lleno al terminar cada oleada (la candidata 'recarga'), para ver si el
    # signo de una carta depende de las balas. cartas=a,b: solo esas cartas, sola y todas menos ella.
    recarga = op.get('recarga', '0') == '1'
    solo = [c for c in op.get('cartas', '').split(',') if c]
    todas = {c: CARTAS[c][0] for c in ORDEN_CARTAS}
    configs = [('ninguna', {}), ('todas', todas),
               ('las de numeros (danio, cadencia, critico, vida)', {'dano': 4, 'cadencia': 4, 'critico': 3, 'vida': 3}),
               ('las de reglas (perforante a dron)', {c: CARTAS[c][0] for c in ORDEN_CARTAS if CARTAS[c][1] == 'reglas'})]
    for c in ORDEN_CARTAS:
        if solo and c not in solo:
            continue
        configs.append(('sola: ' + c, {c: CARTAS[c][0]}))
        configs.append(('todas menos: ' + c, {k: n for k, n in todas.items() if k != c}))
    trabajos = []
    for perfil in perfiles:
        for momento in momentos:
            niv = fotos[origen][perfil][momento]
            for nombre, fijas in configs:
                for s in range(semillas):
                    trabajos.append(((nombre, momento), niv, perfil, 5000 + s, partidas, fijas, recarga))
    inicio = time.time()
    with Pool(hilos) as pool:
        res = pool.map(jugar_fijo, trabajos)
    lineas = ['# Cuanto pesa cada carta', '',
              'Las mejoras congeladas en la foto de la variante "%s" (semilla 1000) a la partida 10, 30 y al final; '
              'cada carta sola y al tope desde el principio de la partida, sin elegir ninguna otra. %d partidas x %d '
              'semillas por casilla, %.0f s.' % (VARIANTES[origen]['nombre'], partidas, semillas, time.time() - inicio)]
    def media(nombre, momento, perfil):
        return statistics.mean(o for r in res if r[0] == (nombre, momento) and r[1] == perfil for o in r[3])

    for perfil in perfiles:
        lineas += ['', '### Perfil %s' % perfil, '',
                   'La oleada media a la que se llega. "sola": la carta al tope y ninguna otra, y entre parentesis '
                   'cuanto suma contra ninguna. "todas menos": todas al tope salvo esa, y entre parentesis cuanto se '
                   'pierde contra todas. Ordenadas por lo que suma sola.', '',
                   '| | ' + ' | '.join('foto %s' % m for m in momentos) + ' |', '|---|' + '---|' * len(momentos)]
        ref = {m: (media('ninguna', m, perfil), media('todas', m, perfil)) for m in momentos}
        for nombre in ('ninguna', 'todas', configs[2][0], configs[3][0]):
            lineas.append('| %s | %s |' % (nombre, ' | '.join(
                '%.1f (%+.1f)' % (media(nombre, m, perfil), media(nombre, m, perfil) - ref[m][0]) for m in momentos)))
        lineas += ['', '| carta | ' + ' | '.join('sola, foto %s' % m for m in momentos) + ' | '
                   + ' | '.join('todas menos, foto %s' % m for m in momentos) + ' |',
                   '|---|' + '---|' * (2 * len(momentos))]
        filas = []
        for c in ORDEN_CARTAS:
            if solo and c not in solo:
                continue
            sola = [media('sola: ' + c, m, perfil) - ref[m][0] for m in momentos]
            menos = [media('todas menos: ' + c, m, perfil) - ref[m][1] for m in momentos]
            filas.append((sum(sola), c, sola, menos))
        filas.sort(key=lambda x: -x[0])
        for _, c, sola, menos in filas:
            lineas.append('| %s | %s | %s |' % (c, ' | '.join('%+.1f' % x for x in sola), ' | '.join('%+.1f' % x for x in menos)))
    texto = '\n'.join(lineas)
    print(texto)
    with open(salida, 'w', encoding='utf-8') as f:
        f.write(texto + '\n')


# --- La escopeta: con las mejoras del principio congeladas, con y sin ella ----------------------

# Las compras de las primeras partidas: con daño 2 y 3 se arranca con el revolver y las cartas de
# daño llevan a la escopeta en la misma partida (2 x 1,25^2 no llega, 3 x 1,25^2 = 4,7 si); con 4 a
# 7 se arranca con ella.
ARRANQUES_ESCOPETA = {
    'danio 3 (revolver; la escopeta con 2 cartas de danio)': dict(dano=2, cadencia=2, criticos=0, vida=2, iman=1, botin=0, granada=0, furia=0),
    'danio 4 (escopeta)': dict(dano=3, cadencia=3, criticos=1, vida=3, iman=2, botin=0, granada=0, furia=0),
    'danio 6 (escopeta)': dict(dano=5, cadencia=5, criticos=2, vida=5, iman=3, botin=1, granada=1, furia=0),
}
VARIANTES_ESCOPETA = ['base', 'base_sin_escopeta', 'base_sin_armas', 'c_equil', 'sin_escopeta', 'sin_armas',
                      'c_equil_30fps', 'sin_escopeta_30fps']


def jugar_arranque(args):
    nombre, clave, niv, perfil_nombre, semilla, partidas = args
    rng = random.Random(semilla)
    perfil = perfil_de(perfil_nombre)
    filas = [Partida(niv, perfil, VARIANTES[clave], rng).jugar() for _ in range(partidas)]
    return nombre, clave, perfil_nombre, [(f['oleada'], f['arma_fin']) for f in filas]


def modo_escopeta(op, hilos, salida):
    perfiles = op.get('perfiles', 'bajo,medio').split(',')
    partidas = int(op.get('partidas', 100))
    semillas = int(op.get('semillas', 4))
    trabajos = [(nombre, clave, niv, perfil, 7000 + s, partidas)
                for nombre, niv in ARRANQUES_ESCOPETA.items() for clave in VARIANTES_ESCOPETA
                for perfil in perfiles for s in range(semillas)]
    inicio = time.time()
    with Pool(hilos) as pool:
        res = pool.map(jugar_arranque, trabajos)
    lineas = ['# La escopeta al principio', '',
              'Las mejoras congeladas como en las primeras partidas (sin comprar entre partida y partida); %d partidas x '
              '%d semillas por casilla, %.0f s. La oleada media a la que se llega (entre parentesis, el error de la '
              'media) y con que arma se murio.' % (partidas, semillas, time.time() - inicio)]
    for perfil in perfiles:
        lineas += ['', '### Perfil %s' % perfil, '',
                   '| compras | ' + ' | '.join(VARIANTES[c]['nombre'] for c in VARIANTES_ESCOPETA) + ' |',
                   '|---|' + '---|' * len(VARIANTES_ESCOPETA)]
        for nombre in ARRANQUES_ESCOPETA:
            celdas = []
            for clave in VARIANTES_ESCOPETA:
                datos = [x for r in res if r[0] == nombre and r[1] == clave and r[2] == perfil for x in r[3]]
                oleadas = [o for o, _ in datos]
                media = statistics.mean(oleadas)
                error = statistics.stdev(oleadas) / math.sqrt(len(oleadas))
                arma = NOMBRE_ARMA[statistics.median_low([a for _, a in datos])]
                celdas.append('%.1f (%.1f) %s' % (media, error, arma))
            lineas.append('| %s | %s |' % (nombre, ' | '.join(celdas)))
    texto = '\n'.join(lineas)
    print(texto)
    with open(salida, 'w', encoding='utf-8') as f:
        f.write(texto + '\n')


# --- Las balas de las oleadas faciles: lo estimado contra el paso a paso -------------------------

def modo_balas(salida):
    """Juega paso a paso las oleadas que el modelo resolveria sin paso a paso (con balas y vida
    de sobra y sin cajas) y compara las balas que gasta con las que estima balas_facil."""
    with open(os.path.join(AQUI, 'fotos.json'), encoding='utf-8') as f:
        fotos = json.load(f)
    lineas = ['# Las balas de las oleadas faciles', '',
              'Con las mejoras de la foto de la "1.5.2 sin cartas" (semilla 1000) a la partida 10 y al final, y con '
              'cartas: daño x2, cadencia x2 y un disparo doble. Cada oleada que el modelo da por facil se juega paso a '
              'paso con balas y vida de sobra y sin cajas, y se comparan las balas que gasta con las que estima '
              '`balas_facil` (tres partidas hasta la primera oleada que no es facil).', '',
              '| perfil | foto | cartas | oleadas | estimado / paso a paso (total) | por oleada (mediana) |',
              '|---|---|---|---|---|---|']
    for perfil_n in ('bajo', 'medio', 'alto'):
        for momento in ('10', 'final'):
            niv = fotos['base'][perfil_n][momento]
            perfil = perfil_de(perfil_n)
            perfil['cajas'] = 0.0
            for clave in ('base', 'c_equil'):
                rng = random.Random(5)
                est, real = [], []
                for _ in range(3):
                    p = Partida(niv, perfil, VARIANTES[clave], rng)
                    if clave == 'c_equil':
                        p.veces.update(dano=2, cadencia=2, disparo_doble=1)
                    p.recalcular()
                    for n in range(1, MAX_OLEADAS):
                        if n % 10 == 0:
                            continue
                        mv = por_oleada(CRECE_VIDA, n, SEGUNDO_TRAMO, CRECE_VIDA_TARDIO)
                        md = por_oleada(CRECE_GOLPE, n, SEGUNDO_TRAMO, CRECE_GOLPE_TARDIO)
                        if not p.facil(n, mv)[0]:
                            break
                        cantidad = 10 + 4 * n
                        est.append(p.balas_facil(n, mv, cantidad))
                        p.balas = p.max_balas = 10 ** 9
                        p.vida = 1e12
                        p.jugar_oleada(n, cantidad, False, mv, md)
                        real.append(10 ** 9 - p.balas)
                if est:
                    lineas.append('| %s | %s | %s | %d | %.2f | %.2f |' % (
                        perfil_n, momento, 'si' if clave == 'c_equil' else 'no', len(est), sum(est) / sum(real),
                        statistics.median(e / r for e, r in zip(est, real) if r)))
    texto = '\n'.join(lineas)
    print(texto)
    with open(salida, 'w', encoding='utf-8') as f:
        f.write(texto + '\n')


# --- La tabla de la experiencia: cuantas cartas salen hasta cada oleada --------------------------

def modo_xp(salida, casos=((4, CRECE_COSTO),)):
    """Las cartas que salen hasta cada oleada con la mezcla de WaveMode, matando a todos (sin
    tesoro ni invocados), para compararla con la tabla de CARTAS.md (1, 2, 3, 6, 12, 16, 19, 24, 28,
    31, 33 hasta la 1, 2, 3, 5, 10, 15, 20, 30, 40, 50 y 60). Cada caso: (zombis por oleada, lo que
    crece el costo del PODER); las oleadas son de 10 + k n."""
    columnas = []
    for por_oleada, crece in casos:
        acumulada, poder, gastado = 0.0, 0, 0
        cartas = {}
        for n in range(1, MAX_OLEADAS + 1):
            hab = curva.habilitados(n)
            total = sum(t[7] for t in hab)
            acumulada += ((10 + por_oleada * n) * sum(t[7] / total * EXPERIENCIA[t[0]] for t in hab)
                          + (EXPERIENCIA['jefe'] if n % 10 == 0 else 0))
            while acumulada - gastado >= costo(poder, crece):
                gastado += costo(poder, crece)
                poder += 1
            cartas[n] = (acumulada, poder)
        columnas.append(cartas)
    lineas = ['# Cartas por oleada (lo esperado, sin azar)', '',
              'Con la mezcla de WaveMode y matando a todos, sin tesoro ni invocados. CARTAS.md: 1, 2, 3, 6, 12, 16, 19, '
              '24, 28, 31 y 33 hasta la 1, 2, 3, 5, 10, 15, 20, 30, 40, 50 y 60. La experiencia acumulada es la de las '
              'oleadas de hoy (10 + 4n).', '',
              '| oleada | experiencia acumulada | ' + ' | '.join('cartas, 10 + %dn, PODER x%.2f' % c for c in casos) + ' |',
              '|---|---|' + '---|' * len(casos)]
    for n in list(range(1, 11)) + [15, 20, 30, 40, 50, 60, 70, 80, 90, 100]:
        lineas.append('| %d | %.0f | %s |' % (n, columnas[0][n][0], ' | '.join(str(c[n][1]) for c in columnas)))
    texto = '\n'.join(lineas)
    print(texto)
    with open(salida, 'w', encoding='utf-8') as f:
        f.write(texto + '\n')


def main():
    sys.stdout.reconfigure(encoding='utf-8')
    op = dict(a.split('=', 1) for a in sys.argv[1:] if '=' in a)
    modo = op.get('modo', 'curva')
    hilos = min(12, int(op.get('hilos', 12)))
    salida = op.get('salida', os.path.join(AQUI, 'resultados_%s.md' % modo))
    if modo == 'xp':
        casos = [(int(c.split(':')[0]), float(c.split(':')[1]))
                 for c in op.get('casos', '4:1.15,4:1.25,3:1.25,2:1.25,2:1.15').split(',')]
        return modo_xp(salida, casos)
    if modo == 'pesos':
        return modo_pesos(op, hilos, salida)
    if modo == 'escopeta':
        return modo_escopeta(op, hilos, salida)
    if modo == 'balas':
        return modo_balas(salida)

    partidas = int(op.get('partidas', 250))
    semillas = int(op.get('semillas', 5))
    perfiles = op.get('perfiles', 'bajo,medio,alto').split(',')
    claves = list(VARIANTES) if op.get('variantes', 'todas') == 'todas' else op['variantes'].split(',')
    inicio = time.time()
    trabajos = [(c, VARIANTES[c], p, 1000 + s, partidas) for c in claves for p in perfiles for s in range(semillas)]
    # Las mas largas primero, para que no quede una sola corriendo al final.
    trabajos.sort(key=lambda w: {'alto': 0, 'medio': 1, 'bajo': 2}.get(w[2], 3))
    with Pool(hilos) as pool:
        res = pool.map(campana, trabajos, chunksize=1)

    # Las fotos de las mejoras (semilla 1000) para el modo pesos.
    fotos = {}
    for c in res:
        if c['semilla'] == 1000:
            de_este = fotos.setdefault(c['clave'], {}).setdefault(c['perfil'], {})
            for p, niv in c['fotos'].items():
                de_este['final' if p == partidas else str(p)] = niv
                if p in (10, 30):
                    de_este[str(p)] = niv
    if op.get('fotos', '1') == '1':
        with open(os.path.join(AQUI, 'fotos.json'), 'w', encoding='utf-8') as f:
            json.dump(fotos, f, indent=1, sort_keys=True)

    lineas = ['# Simulacion de las cartas de la 1.6.0', '',
              'partidas %d, semillas %d, perfiles %s, %.0f s' % (partidas, semillas, ','.join(perfiles), time.time() - inicio),
              '',
              'mejor pN: la mejor oleada completada hasta la partida N (mediana de las semillas). muro: la oleada '
              'mediana de las ultimas 20 partidas de cada semilla. llega a N: la partida en que se completa por primera '
              'vez la oleada N. min. en el muro: lo que dura una de esas partidas, con %.0f s por eleccion de carta. '
              '%% muertes en jefe: de las ultimas 100 partidas de cada semilla.' % SEGUNDOS_POR_ELECCION]
    lineas += ['', '## El muro']
    for perfil in perfiles:
        lineas += tabla_principal(res, claves, perfil, partidas)
    lineas += ['', '## El arranque']
    for perfil in perfiles:
        lineas += tabla_arranque(res, claves, perfil)
    lineas += ['', '## El oro por minuto', '',
               'Las monedas de la partida (las juntadas, el bono de cada oleada y el tesoro) sobre sus minutos (con '
               '%.0f s por eleccion de carta), sumadas las de todas las semillas.' % SEGUNDOS_POR_ELECCION]
    for perfil in perfiles:
        lineas += tabla_oro(res, claves, perfil)
    lineas += ['', '## Los minutos y el oro hasta cada oleada', '',
               'Al terminar la oleada N, en las partidas que la terminan (mediana de todas, de todas las semillas; '
               '"—" con menos de 10).']
    for perfil in perfiles:
        lineas += tabla_minutos(res, claves, perfil)
    lineas += ['', '## Las cartas']
    lineas += tabla_cartas_por_oleada(res, [c for c in claves if c in ('c_equil', 'c_dano', 'costo125', 'suaves',
                                                                       'gemini')], perfiles)
    for perfil in perfiles:
        lineas += tabla_cartas_elegidas(res, claves, perfil)
    texto = '\n'.join(lineas)
    print(texto)
    with open(salida, 'w', encoding='utf-8') as f:
        f.write(texto + '\n')


if __name__ == '__main__':
    main()
