using System;
using System.Collections.Generic;
using UnityEngine;

// Lo que se cobra solo, sin que el jugador toque nada: el cierre de medianoche de las
// misiones (MisionesDiarias.CerrarElDia), el de la semana (DesafioSemanal.CerrarLaSemana) y
// el final de Halloween (EventoHalloween.CerrarSiTermino). Hasta la revision del 9/10
// (mejora 5) pasaba en silencio: el contador subia y nadie sabia por que, y el desafio de la
// semana es el premio mas grande del juego. Cada cierre lo anota aca y lo muestra el primero
// que puede: el cartel del menu (CartelDeCobros) o, si el cierre fue al empezar una partida,
// el aviso de la partida (AvisoDeMisiones). Queda solo en memoria: si la app se cierra antes
// de mostrarlo, se pierde el aviso, no las monedas.
public static class CobrosSolos
{
    public enum Origen { Misiones, Semanal, Halloween }

    public struct Cobro
    {
        public Origen origen;
        public double monedas;
        public bool sombrero;
    }

    private static readonly List<Cobro> pendientes = new List<Cobro>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetearEstadoCompartido()
    {
        pendientes.Clear();
    }

    public static bool Hay
    {
        get { return pendientes.Count > 0; }
    }

    // Dos cierres del mismo origen antes de mostrarlos (dos dias seguidos sin abrir el menu
    // no pasan, pero el de Halloween puede venir con monedas y con el sombrero) van juntos.
    public static void Anotar(Origen origen, double monedas, bool sombrero = false)
    {
        if (!(monedas > 0) || double.IsInfinity(monedas)) monedas = 0;
        if (monedas <= 0 && !sombrero) return;
        for (int i = 0; i < pendientes.Count; i++)
        {
            if (pendientes[i].origen != origen) continue;
            var cobro = pendientes[i];
            cobro.monedas += monedas;
            cobro.sombrero |= sombrero;
            pendientes[i] = cobro;
            return;
        }
        pendientes.Add(new Cobro { origen = origen, monedas = monedas, sombrero = sombrero });
    }

    // Los que hay, y la fila queda vacia: se muestran una sola vez.
    public static List<Cobro> Tomar()
    {
        var lista = new List<Cobro>(pendientes);
        pendientes.Clear();
        return lista;
    }

    // Un renglon por cobro: "MISIONES DEL DIA: +1.250 MONEDAS".
    public static string Renglon(Cobro cobro)
    {
        string monedas = FormatoNumeros.Compacto(Math.Floor(cobro.monedas + 1e-6));
        switch (cobro.origen)
        {
            case Origen.Misiones: return Textos.Formato("cobro_solo_misiones", monedas);
            case Origen.Semanal: return Textos.Formato("cobro_solo_semanal", monedas);
            default:
                if (!cobro.sombrero) return Textos.Formato("cobro_solo_halloween", monedas);
                if (cobro.monedas > 0) return Textos.Formato("cobro_solo_halloween_sombrero", monedas);
                return Textos.De("cobro_solo_sombrero");
        }
    }
}
