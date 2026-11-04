namespace DetectorFuerzaBruta;

// Resultado del análisis temporal de una IP con diferencias finitas.
public class ResultadoTemporal
{
    // Marca de tiempo del segundo registro de cada intervalo válido.
    public List<DateTime> MarcasTiempo { get; } = new();

    // Intervalos h_i en segundos (solo h_i > 0).
    public List<double> Intervalos { get; } = new();

    // Frecuencias v_i = 1 / h_i en intentos por segundo.
    public List<double> Frecuencias { get; } = new();

    // Aceleraciones a_i de la frecuencia.
    public List<double> Aceleraciones { get; } = new();

    // Inicio de la ventana analizada.
    public DateTime Inicio { get; set; }

    // Intentos dentro de la ventana móvil.
    public int IntentosAnalizados { get; set; }

    // Intentos fallidos dentro de la ventana móvil.
    public int IntentosFallidos { get; set; }

    // Cantidad de intervalos descartados por tener h_i <= 0.
    public int IntervalosIgnorados { get; set; }

    public double PromedioAceleracion =>
        Aceleraciones.Count > 0 ? Aceleraciones.Average() : 0.0;

    public double FrecuenciaActual =>
        Frecuencias.Count > 0 ? Frecuencias[^1] : 0.0;

    public bool HayDatos => Intervalos.Count > 0;

    // Variación de la frecuencia según el promedio de las aceleraciones.
    public string Variacion
    {
        get
        {
            if (Aceleraciones.Count == 0)
            {
                return "Sin datos";
            }

            if (PromedioAceleracion > 1e-12)
            {
                return "Positiva";
            }

            if (PromedioAceleracion < -1e-12)
            {
                return "Negativa";
            }

            return "Sin variación";
        }
    }

    public bool TendenciaCreciente => Variacion == "Positiva";
}

// Método de diferencias finitas aplicado a los intervalos entre intentos
// de autenticación de una misma IP.
//
// h_i = t_i - t_(i-1)                       (segundos)
// v_i = 1 / h_i                              (intentos por segundo)
// a_i = (v_i - v_(i-1)) / ((h_i + h_(i-1)) / 2)
public static class DiferenciasFinitas
{
    // Calcula intervalos, frecuencias y aceleraciones sobre los últimos
    // intentos de una IP (ventana móvil).
    public static ResultadoTemporal Calcular(List<Registro> registros, int tamanoVentana)
    {
        var resultado = new ResultadoTemporal();

        List<Registro> ordenados = registros
            .OrderBy(r => r.FechaHora)
            .ToList();

        if (ordenados.Count > tamanoVentana)
        {
            ordenados = ordenados.Skip(ordenados.Count - tamanoVentana).ToList();
        }

        if (ordenados.Count == 0)
        {
            return resultado;
        }

        resultado.Inicio = ordenados[0].FechaHora;
        resultado.IntentosAnalizados = ordenados.Count;
        resultado.IntentosFallidos =
            ordenados.Count(r => r.Resultado == ResultadoIntento.Fallido);

        for (int i = 1; i < ordenados.Count; i++)
        {
            double h = (ordenados[i].FechaHora - ordenados[i - 1].FechaHora).TotalSeconds;

            // Si dos eventos comparten la marca de tiempo no se puede dividir entre cero.
            if (h <= 0)
            {
                resultado.IntervalosIgnorados++;
                continue;
            }

            resultado.MarcasTiempo.Add(ordenados[i].FechaHora);
            resultado.Intervalos.Add(h);
        }

        resultado.Frecuencias.AddRange(CalcularFrecuencias(resultado.Intervalos));
        resultado.Aceleraciones.AddRange(
            CalcularAceleraciones(resultado.Intervalos, resultado.Frecuencias));

        return resultado;
    }

    // v_i = 1 / h_i.
    public static List<double> CalcularFrecuencias(List<double> intervalos)
    {
        var frecuencias = new List<double>();

        foreach (double h in intervalos)
        {
            frecuencias.Add(1.0 / h);
        }

        return frecuencias;
    }

    // a_i = (v_i - v_(i-1)) / ((h_i + h_(i-1)) / 2).
    public static List<double> CalcularAceleraciones(
        List<double> intervalos,
        List<double> frecuencias)
    {
        var aceleraciones = new List<double>();

        for (int i = 1; i < intervalos.Count && i < frecuencias.Count; i++)
        {
            double denominador = (intervalos[i] + intervalos[i - 1]) / 2.0;
            if (denominador == 0)
            {
                continue;
            }

            aceleraciones.Add((frecuencias[i] - frecuencias[i - 1]) / denominador);
        }

        return aceleraciones;
    }
}
