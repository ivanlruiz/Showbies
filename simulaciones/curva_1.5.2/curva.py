# Simulacion de la curva de las oleadas para la 1.5.2 (10/10).
#
# La pregunta: el muro de las oleadas altas. Gemini propuso que la vida de los zombis deje de
# crecer x1,11 por oleada desde la 20-25 (una "cola" mas suave); la superauditoria del 29/9 habia
# medido que el muro lo causan los topes de la cadencia (16) y los criticos (8), y propuso hitos de
# danio (x1,5 cada 5 niveles). Aca se simulan las dos familias con la economia de hoy, partida por
# partida: se juega, se cobran las monedas y se compra entre partidas.
#
# El modelo es el de la fase 4 (Builds/simulacion_fase4/economia.py) puesto al dia: los zombis
# caminan hacia el jugador (distancia en 1D), el jugador tira al mas cercano dentro del alcance,
# los que tocan pegan cada 0,8 s segun cuanto esquive el jugador, y las monedas se juntan con una
# fraccion que depende del perfil y del iman. Las oleadas faciles (presion baja) se resuelven sin
# paso a paso, para que alcance a correr cientos de partidas.
#
# Lo que se puso al dia (CLAUDE.md, WaveMode.unity, los .asset):
#   10 + 4n zombis, uno cada 0,35 s, techo de 35 vivos (el telefono), FASTER a 10,5, monedas x1,08,
#   bono 4n (x2 si la oleada sale sin un golpe), criticos 0/5/10/20/30/50/75/90/100 %, furia (6 s
#   cada 120 s, cadencia y danio x2), granada comprada (10 x la vida de la oleada), el jefe con sus
#   ataques cada 5 s (x0,65 en furia) que se esquivan, sus invocaciones y el zombi del tesoro.
#
# Uso: python curva.py [clave=valor ...]
#   partidas=400 semillas=3 perfiles=medio variantes=todas extra=1.0 hilos=12 salida=resultados.md
import math
import random
import statistics
import sys
import time
from multiprocessing import Pool

DT = 0.1
TECHO_VIVOS = 35
APARICION = 0.35
ALCANCE = 22.0
VEL_BALA = 25.0
NACEN_A = 35.0
MAX_OLEADAS = 120
MAX_SEGUNDOS_OLEADA = 900.0

# nombre, hp, danio, velocidad, monedas min, max, desde, peso
TIPOS = [
    ('normal', 5, 1, 5.0, 1, 3, 1, 1.0),
    ('rapido', 3, 2, 9.0, 1, 3, 3, 0.35),
    ('tanque', 25, 1, 3.0, 5, 8, 6, 0.10),
    ('faster', 3, 1, 10.5, 2, 4, 9, 0.25),
]
JEFE = ('jefe', 500, 10, 2.0, 30, 40)

CRITICOS = [0, .05, .10, .20, .30, .50, .75, .90, 1.0]
# precio inicial, crecimiento, tope
PRECIOS = {
    'dano': (40, 1.45, None), 'cadencia': (50, 1.45, 16), 'criticos': (150, 1.8, 8),
    'vida': (40, 1.45, None), 'iman': (30, 1.5, 13), 'botin': (120, 1.55, 15),
    'granada': (250, 1.0, 1), 'furia': (5000, 1.0, 1),
}
ORDEN = ['dano', 'cadencia', 'criticos', 'vida', 'iman', 'botin', 'granada', 'furia']

PERFILES = {
    'bajo': dict(punteria=0.55, exposicion=0.90, monedas_sin_iman=0.35, cajas=0.15, granada=0.5,
                 umbral_granada=4, reaprovecha=0.3, esquiva_jefe=0.6, tesoro=0.3, perfecta=0.25),
    'medio': dict(punteria=0.70, exposicion=0.65, monedas_sin_iman=0.55, cajas=0.35, granada=0.75,
                  umbral_granada=3, reaprovecha=0.5, esquiva_jefe=0.8, tesoro=0.5, perfecta=0.5),
    'alto': dict(punteria=0.85, exposicion=0.40, monedas_sin_iman=0.75, cajas=0.55, granada=0.95,
                 umbral_granada=2, reaprovecha=0.7, esquiva_jefe=0.92, tesoro=0.7, perfecta=0.7),
}

# Las variantes. vida_k: desde que oleada la vida crece vida_g en vez de 1,11. dano_k/dano_g lo
# mismo con el golpe de los zombis (1,07). hitos: el danio de la bala x1,5 cada 5 niveles.
# cad_base / dano_base: el arranque (decision 1 del PLAN): cadencia base 6 = 6 + 1 por nivel.
BASE = dict(vida_k=None, vida_g=1.11, dano_k=None, dano_g=1.07, hitos=False, cad_base=4, dano_base=1)


def variante(nombre, **cambios):
    v = dict(BASE)
    v.update(cambios)
    v['nombre'] = nombre
    return v


VARIANTES = {
    'base': variante('hoy: vida x1,11 siempre'),
    'c20_103': variante('cola desde la 20, x1,03', vida_k=20, vida_g=1.03),
    'c20_105': variante('cola desde la 20, x1,05', vida_k=20, vida_g=1.05),
    'c25_103': variante('cola desde la 25, x1,03', vida_k=25, vida_g=1.03),
    'c25_104': variante('cola desde la 25, x1,04', vida_k=25, vida_g=1.04),
    'c25_105': variante('cola desde la 25, x1,05', vida_k=25, vida_g=1.05),
    'c25_106': variante('cola desde la 25, x1,06', vida_k=25, vida_g=1.06),
    'hitos': variante('hitos de danio x1,5 cada 5, vida x1,11', hitos=True),
    'c25_105_hitos': variante('cola 25 x1,05 + hitos', vida_k=25, vida_g=1.05, hitos=True),
    'c25_105_d103': variante('cola 25 x1,05 + golpe x1,03 desde la 25', vida_k=25, vida_g=1.05, dano_k=25, dano_g=1.03),
}

# Lo que eligio Ivan (10/10): la cola desde la oleada 11, despues del primer jefe, y la
# cadencia base 6. Faltaba cuanto: estas son las candidatas, todas con cadencia 6.
DESDE_LA_11 = {
    'hoy_c6': variante('hoy, con cadencia 6', cad_base=6),
    'c25_v105_d103_c6': variante('desde la 25: vida x1,05, golpe x1,03', vida_k=25, vida_g=1.05, dano_k=25, dano_g=1.03, cad_base=6),
    'c11_v109_d105': variante('desde la 11: vida x1,09, golpe x1,05', vida_k=11, vida_g=1.09, dano_k=11, dano_g=1.05, cad_base=6),
    'c11_v108_d105': variante('desde la 11: vida x1,08, golpe x1,05', vida_k=11, vida_g=1.08, dano_k=11, dano_g=1.05, cad_base=6),
    'c11_v108_d104': variante('desde la 11: vida x1,08, golpe x1,04', vida_k=11, vida_g=1.08, dano_k=11, dano_g=1.04, cad_base=6),
    'c11_v107_d104': variante('desde la 11: vida x1,07, golpe x1,04', vida_k=11, vida_g=1.07, dano_k=11, dano_g=1.04, cad_base=6),
    'c11_v107_d103': variante('desde la 11: vida x1,07, golpe x1,03', vida_k=11, vida_g=1.07, dano_k=11, dano_g=1.03, cad_base=6),
    'c11_v106_d103': variante('desde la 11: vida x1,06, golpe x1,03', vida_k=11, vida_g=1.06, dano_k=11, dano_g=1.03, cad_base=6),
    'c11_v105_d103': variante('desde la 11: vida x1,05, golpe x1,03', vida_k=11, vida_g=1.05, dano_k=11, dano_g=1.03, cad_base=6),
}
VARIANTES.update(DESDE_LA_11)

ARRANQUES = {
    'hoy': variante('hoy: danio 1, cadencia 4'),
    'cad6': variante('cadencia base 6', cad_base=6),
    'd2_cad6': variante('danio base 2, cadencia 6', cad_base=6, dano_base=2),
}


def dos_tramos(base, k, g, n):
    if k is None or n <= k:
        return base ** (n - 1)
    return base ** (k - 1) * g ** (n - k)


def precio(m, nivel):
    p, g, _ = PRECIOS[m]
    return math.floor(p * g ** nivel + 0.5 + 1e-9)


def en_tope(m, nivel):
    t = PRECIOS[m][2]
    return t is not None and nivel >= t


def estadisticas(niv, var):
    ld = niv['dano']
    dano = (var['dano_base'] + ld) * (1.5 ** (ld // 5) if var['hitos'] else 1.0)
    tiros = var['cad_base'] + min(niv['cadencia'], 16)
    crit = CRITICOS[min(niv['criticos'], 8)]
    mult_vida = 1 + 0.25 * niv['vida']
    vida = round(80 * mult_vida)
    li = min(niv['iman'], 13)
    iman = 0.0 if li == 0 else 2 * (1 + 0.25 * (li - 1))
    botin = 1 + 0.1 * min(niv['botin'], 15)
    return dict(dano=dano, tiros=tiros, crit=crit, vida=vida, mult_vida=mult_vida, iman=iman, botin=botin,
                granada=niv['granada'] > 0, furia=niv['furia'] > 0)


VEL_JUGADOR = 11.5


def factor_velocidad(v, vivos):
    # El jugador corre a 11,5 y les escapa a todos: a un lento casi no lo deja pegar mientras
    # tenga lugar para correr, y a uno rapido (9-10,5) le cuesta mas. Lo que lo encierra es la
    # horda: con el techo de vivos lleno, hasta los lentos alcanzan.
    return min(1.0, (v / VEL_JUGADOR) ** 2 + 0.6 * (vivos / TECHO_VIVOS) ** 1.5)


def fraccion_monedas(perfil, iman):
    f0 = perfil['monedas_sin_iman']
    if iman <= 0:
        return f0
    return 1 - (1 - f0) * math.exp(-(iman - 0.8) / 2.5)


def habilitados(n):
    return [t for t in TIPOS if n >= t[6]]


def sortear_tipo(n, rng):
    hab = habilitados(n)
    total = sum(t[7] for t in hab)
    r = rng.random() * total
    for t in hab:
        r -= t[7]
        if r <= 0:
            return t
    return hab[-1]


def presion(n, st, var):
    """Zombis que llegan por segundo sobre zombis que se matan por segundo, sin cajas ni furia."""
    mv = dos_tramos(1.11, var['vida_k'], var['vida_g'], n)
    hab = habilitados(n)
    total = sum(t[7] for t in hab)
    dano_medio = st['dano'] * (1 + st['crit'])
    tiros = sum(t[7] / total * max(1.0, t[1] * mv / dano_medio) for t in hab)
    return (1.0 / APARICION) * tiros / max(1e-9, st['tiros'])


def jugar_partida(niv, perfil, var, rng, diag=None):
    st = estadisticas(niv, var)
    frac = fraccion_monedas(perfil, st['iman'])
    e = dict(vida=float(st['vida']), balas=500, max_balas=500, potencia=1.0, potencia_hasta=-1.0,
             monedas=0.0, t=0.0, granada_lista=0.0, furia_lista=0.0, furia_hasta=-1.0)
    prox = {'balas': 8.0, 'vida': 8.0, 'arma': 20.0}
    intervalos = {'balas': 8.0, 'vida': 8.0, 'arma': 20.0}
    cura = round(40 * st['mult_vida'])

    def cajas_hasta(t):
        for tipo in ('balas', 'vida', 'arma'):
            while prox[tipo] <= t:
                if rng.random() < perfil['cajas']:
                    if tipo == 'vida':
                        e['vida'] = min(st['vida'], e['vida'] + cura)
                    elif tipo == 'balas':
                        e['balas'] = e['max_balas']
                        e['potencia'], e['potencia_hasta'] = 1.5, prox[tipo] + 10
                    else:
                        e['max_balas'] = max(e['max_balas'], 1000)
                        e['balas'] = e['max_balas']
                        e['potencia'], e['potencia_hasta'] = 3.0, prox[tipo] + 10
                prox[tipo] += intervalos[tipo]

    def avanzar(segundos):
        e['t'] += segundos
        cajas_hasta(e['t'])

    for n in range(1, MAX_OLEADAS + 1):
        mv = dos_tramos(1.11, var['vida_k'], var['vida_g'], n)
        md = dos_tramos(1.07, var['dano_k'], var['dano_g'], n)
        mm = 1.08 ** (n - 1) * st['botin']
        cantidad = 10 + 4 * n
        jefe = n % 10 == 0
        avanzar(3.0)

        rho = presion(n, st, var)
        if diag is not None:
            diag[n] = (rho, st['vida'] / (2 * md))
        golpeado = False
        if not jefe and rho < 0.6:
            # Oleada facil: todos mueren antes de llegar. Monedas esperadas, sin golpes salvo mala suerte.
            for _ in range(cantidad):
                tp = sortear_tipo(n, rng)
                pal = rng.randint(tp[4], tp[5])
                e['monedas'] += sum(1 for _ in range(pal) if rng.random() < frac) * mm
            golpeado = rng.random() >= perfil['perfecta']
            avanzar(cantidad * APARICION + 2.0)
            e['balas'] = e['max_balas']
        else:
            r = jugar_oleada(n, cantidad, jefe, mv, md, mm, st, perfil, frac, e, rng, cajas_hasta)
            if r is None:
                return n - 1, e['monedas'], e['t']
            golpeado = r
        e['monedas'] += 4 * n * (1 if golpeado else 2)
        if n >= 3 and rng.random() < 0.25 and rng.random() < perfil['tesoro']:
            pal = rng.randint(14, 20)
            e['monedas'] += sum(1 for _ in range(pal) if rng.random() < frac) * mm
    return MAX_OLEADAS, e['monedas'], e['t']


def jugar_oleada(n, cantidad, jefe, mv, md, mm, st, perfil, frac, e, rng, cajas_hasta):
    """Paso a paso. Devuelve None si el jugador muere, o si recibio algun golpe."""
    cola = []
    if jefe:
        cola.append(('jefe',) + JEFE[1:])
    for _ in range(cantidad):
        cola.append(sortear_tipo(n, rng))
    golpeado = False
    vivos = []
    pendientes = []
    prox_aparicion = e['t']
    acumulador = 0.0
    inicio = e['t']
    jefe_z = None

    def matar(z):
        z['vivo'] = False
        pal = rng.randint(z['mmin'], z['mmax'])
        e['monedas'] += sum(1 for _ in range(pal) if rng.random() < frac) * mm

    while cola or vivos:
        t = e['t']
        cajas_hasta(t)
        if t - inicio > MAX_SEGUNDOS_OLEADA:
            return None
        if cola and t >= prox_aparicion and len(vivos) < TECHO_VIVOS:
            tp = cola.pop(0)
            z = {'tipo': tp[0], 'hp': tp[1] * mv, 'hp_max': tp[1] * mv, 'dano': tp[2] * md, 'vel': tp[3],
                 'mmin': tp[4], 'mmax': tp[5], 'd': NACEN_A, 'reloj': 0.0, 'vivo': True}
            vivos.append(z)
            if tp[0] == 'jefe':
                jefe_z = z
                z['ataque'] = t + 3.0
                z['ataques'] = 0
                prox_aparicion = t + 1.5
            else:
                prox_aparicion = t + APARICION

        for z in vivos:
            if z['d'] > 0:
                z['d'] = max(0.0, z['d'] - z['vel'] * DT)

        # La furia: apenas se pone feo.
        if st['furia'] and t >= e['furia_lista'] and (len(vivos) >= 8 or (jefe_z and jefe_z['vivo'])):
            e['furia_hasta'] = t + 6.0
            e['furia_lista'] = t + 120.0
        en_furia = t < e['furia_hasta']

        en_alcance = [z for z in vivos if z['d'] <= ALCANCE]
        if en_alcance and e['balas'] > 0:
            pot = e['potencia'] if t < e['potencia_hasta'] else 1.0
            cad = min(120.0, st['tiros'] * pot * (2.0 if en_furia else 1.0))
            acumulador += cad * DT
            objetivo = min(en_alcance, key=lambda z: z['d'])
            while acumulador >= 1 and e['balas'] > 0:
                acumulador -= 1
                e['balas'] -= 1
                blanco = None
                if rng.random() < perfil['punteria']:
                    blanco = objetivo
                elif rng.random() < min(0.6, len(en_alcance) / 30.0):
                    # La que falla en una horda le pega a otro.
                    blanco = en_alcance[rng.randrange(len(en_alcance))]
                if blanco is not None:
                    dano = st['dano'] * (2.0 if en_furia else 1.0) * (2.0 if rng.random() < st['crit'] else 1.0)
                    pendientes.append((t + blanco['d'] / VEL_BALA, blanco, dano))
        else:
            acumulador = 0.0

        if pendientes:
            quedan = []
            for llega, z, dano in pendientes:
                if llega > t:
                    quedan.append((llega, z, dano))
                    continue
                blanco = z
                if not z['vivo']:
                    otros = [o for o in vivos if o['vivo'] and o['d'] <= ALCANCE]
                    if not otros or rng.random() >= perfil['reaprovecha']:
                        continue
                    blanco = min(otros, key=lambda o: o['d'])
                blanco['hp'] -= dano
                if blanco['hp'] <= 0.01 and blanco['vivo']:
                    matar(blanco)
            pendientes = quedan

        if st['granada'] and t >= e['granada_lista']:
            cerca = [z for z in vivos if z['vivo'] and z['d'] <= 5]
            duros = any(z['tipo'] in ('tanque', 'jefe') and z['d'] <= 6 for z in cerca)
            if len(cerca) >= perfil['umbral_granada'] or duros:
                e['granada_lista'] = t + 5.0
                for z in cerca:
                    if rng.random() < perfil['granada']:
                        z['hp'] -= 10 * mv
                        if z['hp'] <= 0.01 and z['vivo']:
                            matar(z)

        # Los ataques del jefe: salto y carga alternados cada 5 s (x0,65 en furia), y en furia,
        # uno de cada tres invoca 6 normales.
        if jefe_z is not None and jefe_z['vivo'] and jefe_z['d'] <= 15 and t >= jefe_z['ataque']:
            furioso = jefe_z['hp'] < 0.5 * jefe_z['hp_max']
            jefe_z['ataque'] = t + 5.0 * (0.65 if furioso else 1.0)
            jefe_z['ataques'] += 1
            if furioso and jefe_z['ataques'] % 3 == 0:
                normal = TIPOS[0]
                for _ in range(6):
                    if len(vivos) < TECHO_VIVOS:
                        vivos.append({'tipo': 'normal', 'hp': normal[1] * mv, 'hp_max': normal[1] * mv,
                                      'dano': normal[2] * md, 'vel': normal[3], 'mmin': normal[4],
                                      'mmax': normal[5], 'd': max(2.0, jefe_z['d']), 'reloj': 0.0, 'vivo': True})
            elif rng.random() >= perfil['esquiva_jefe']:
                golpe = 1.5 if jefe_z['ataques'] % 2 == 1 else 2.5
                e['vida'] -= jefe_z['dano'] * golpe
                golpeado = True

        for z in vivos:
            if z['vivo'] and z['d'] <= 0 and t >= z['reloj'] and z['tipo'] != 'jefe':
                z['reloj'] = t + 0.8
                if rng.random() < perfil['exposicion'] * factor_velocidad(z['vel'], len(vivos)):
                    e['vida'] -= z['dano']
                    golpeado = True
        vivos = [z for z in vivos if z['vivo']]

        if e['vida'] <= 0:
            return None
        e['t'] += DT
    return golpeado


def ganancia(m, niv, var, perfil):
    st = estadisticas(niv, var)
    if m == 'granada':
        return 0.30
    if m == 'furia':
        return 0.20
    prueba = dict(niv)
    prueba[m] += 1
    s2 = estadisticas(prueba, var)
    if m == 'dano':
        return s2['dano'] / st['dano'] - 1
    if m == 'cadencia':
        return s2['tiros'] / st['tiros'] - 1
    if m == 'criticos':
        return (1 + s2['crit']) / (1 + st['crit']) - 1
    if m == 'vida':
        return 0.6 * (s2['vida'] / st['vida'] - 1)
    if m == 'iman':
        return fraccion_monedas(perfil, s2['iman']) / fraccion_monedas(perfil, st['iman']) - 1
    return 0.8 * (s2['botin'] / st['botin'] - 1)


def comprar(niv, banco, var, perfil):
    while True:
        candidatas = [m for m in ORDEN if not en_tope(m, niv[m])]
        if not candidatas:
            break
        elegida = max(candidatas, key=lambda m: ganancia(m, niv, var, perfil) / precio(m, niv[m]))
        p = precio(elegida, niv[elegida])
        if p > math.floor(banco + 1e-6):
            break
        banco -= p
        niv[elegida] += 1
    return banco


def campana(args):
    clave, var, perfil_nombre, semilla, partidas, extra = args
    rng = random.Random(semilla)
    perfil = PERFILES[perfil_nombre]
    niv = {m: 0 for m in ORDEN}
    banco = 0.0
    filas = []
    for p in range(1, partidas + 1):
        diag = {}
        ol, gan, dur = jugar_partida(niv, perfil, var, rng, diag)
        banco = comprar(niv, banco + gan * extra, var, perfil)
        sig = diag.get(ol + 1, (None, None))
        filas.append(dict(partida=p, oleada=ol, monedas=gan, segundos=dur, niveles=dict(niv),
                          presion=sig[0], golpes=sig[1]))
    return clave, perfil_nombre, semilla, filas


def primera_partida_en(filas, meta):
    for f in filas:
        if f['oleada'] >= meta:
            return f['partida']
    return None


def resumen_curva(resultados, claves, perfiles, metas, partidas):
    lineas = []
    for perfil in perfiles:
        lineas.append('')
        lineas.append('### Perfil %s: en que partida se llega por primera vez a cada oleada (mediana de las semillas)' % perfil)
        lineas.append('')
        lineas.append('| variante | ' + ' | '.join(str(m) for m in metas) + ' | oleada en la partida %d |' % partidas)
        lineas.append('|---|' + '---|' * len(metas) + '---|')
        for clave in claves:
            camps = [r[3] for r in resultados if r[0] == clave and r[1] == perfil]
            if not camps:
                continue
            celdas = []
            for meta in metas:
                vals = [primera_partida_en(c, meta) for c in camps]
                ok = [v for v in vals if v is not None]
                if len(ok) * 2 < len(vals):
                    celdas.append('—')
                else:
                    celdas.append(str(int(statistics.median(ok))))
            finales = [max(f['oleada'] for f in c[-10:]) for c in camps]
            lineas.append('| %s | %s | %d |' % (VARIANTES.get(clave, ARRANQUES.get(clave))['nombre'], ' | '.join(celdas),
                                                  int(statistics.median(finales))))
    return lineas


def resumen_muro(resultados, claves, perfil):
    """Lo que frena al final: la presion (zombis que llegan / que se matan) y los golpes que aguanta."""
    lineas = ['', '### Perfil %s: que frena en las ultimas 20 partidas' % perfil, '',
              '| variante | oleada | presion en la siguiente | golpes del rapido que aguanta | muertes en oleada de jefe (ultimas 100) | niveles (D C Cr V I B) |',
              '|---|---|---|---|---|---|']
    for clave in claves:
        camps = [r[3] for r in resultados if r[0] == clave and r[1] == perfil]
        if not camps:
            continue
        ultimas = [f for c in camps for f in c[-20:]]
        ol = statistics.median(f['oleada'] for f in ultimas)
        pres = [f['presion'] for f in ultimas if f['presion'] is not None]
        gol = [f['golpes'] for f in ultimas if f['golpes'] is not None]
        niv = camps[0][-1]['niveles']
        muertes = [f['oleada'] + 1 for c in camps for f in c[-100:] if f['oleada'] < MAX_OLEADAS]
        en_jefe = 100.0 * sum(1 for o in muertes if o % 10 == 0) / max(1, len(muertes))
        lineas.append('| %s | %d | %s | %s | %.0f %% | %d %d %d %d %d %d |' % (
            VARIANTES[clave]['nombre'], ol,
            '%.2f' % statistics.median(pres) if pres else '—',
            '%.0f' % statistics.median(gol) if gol else '—', en_jefe,
            niv['dano'], niv['cadencia'], niv['criticos'], niv['vida'], niv['iman'], niv['botin']))
    return lineas


def resumen_arranque(resultados, claves, perfiles):
    lineas = []
    for perfil in perfiles:
        lineas += ['', '### Perfil %s: las primeras 10 partidas (oleada a la que se llega, mediana)' % perfil, '',
                   '| arranque | ' + ' | '.join('p%d' % i for i in range(1, 11)) + ' | compras despues de la 1.ª |',
                   '|---|' + '---|' * 10 + '---|']
        for clave in claves:
            camps = [r[3] for r in resultados if r[0] == clave and r[1] == perfil]
            if not camps:
                continue
            ols = [int(statistics.median(c[i]['oleada'] for c in camps)) for i in range(10)]
            compras1 = statistics.median(sum(c[0]['niveles'].values()) for c in camps)
            lineas.append('| %s | %s | %.0f |' % (ARRANQUES[clave]['nombre'], ' | '.join(str(o) for o in ols), compras1))
    return lineas


def main():
    sys.stdout.reconfigure(encoding='utf-8')
    op = dict(a.split('=', 1) for a in sys.argv[1:] if '=' in a)
    partidas = int(op.get('partidas', 400))
    semillas = int(op.get('semillas', 3))
    perfiles = op.get('perfiles', 'medio').split(',')
    extra = float(op.get('extra', 1.0))
    hilos = int(op.get('hilos', 12))
    modo = op.get('modo', 'curva')
    salida = op.get('salida', 'resultados_%s.md' % modo)
    inicio = time.time()

    if modo == 'arranque':
        claves = list(ARRANQUES)
        tabla = ARRANQUES
        partidas = min(partidas, 10)
    else:
        claves = list(VARIANTES) if op.get('variantes', 'todas') == 'todas' else op['variantes'].split(',')
        tabla = VARIANTES
    trabajos = [(c, tabla[c], p, 1000 + s, partidas, extra) for c in claves for p in perfiles for s in range(semillas)]
    with Pool(hilos) as pool:
        resultados = pool.map(campana, trabajos)

    lineas = ['# Simulacion de la curva (%s)' % modo, '',
              'partidas %d, semillas %d, perfiles %s, ingreso extra x%.1f (la diaria, las misiones, los niveles y el x2 del video), %.0f s' % (
                  partidas, semillas, ','.join(perfiles), extra, time.time() - inicio)]
    if modo == 'arranque':
        lineas += resumen_arranque(resultados, claves, perfiles)
    else:
        metas = [10, 15, 20, 25, 30, 35, 40, 45, 50, 55, 60, 70, 80]
        lineas += resumen_curva(resultados, claves, perfiles, metas, partidas)
        for perfil in perfiles:
            lineas += resumen_muro(resultados, claves, perfil)
    texto = '\n'.join(lineas)
    print(texto)
    with open(salida, 'w', encoding='utf-8') as f:
        f.write(texto + '\n')


if __name__ == '__main__':
    main()
