namespace DetectorFuerzaBruta;

// Datos calculados de una dirección IP.
public class ResumenIp
{
    public string DireccionIp { get; set; } = string.Empty;
    public int TotalIntentos { get; set; }
    public int IntentosExitosos { get; set; }
    public int IntentosFallidos { get; set; }
    public DateTime UltimoIntento { get; set; }
}

// Media y desviación estándar de los intentos fallidos por IP
// en los registros de referencia considerados normales.
public class EstadisticaReferencia
{
    public double Media { get; set; }
    public double Desviacion { get; set; }
    public int CantidadIp { get; set; }
}

// Agrupa los registros por IP, calcula sus conteos y la estadística de referencia.
public static class Analizador
{
    // Agrupa los registros por dirección IP y los ordena por fecha.
    public static Dictionary<string, List<Registro>> AgruparPorIp(List<Registro> registros)
    {
        var grupos = new Dictionary<string, List<Registro>>();

        foreach (Registro registro in registros)
        {
            if (!grupos.TryGetValue(registro.DireccionIp, out List<Registro>? lista))
            {
                lista = new List<Registro>();
                grupos[registro.DireccionIp] = lista;
            }

            lista.Add(registro);
        }

        foreach (List<Registro> lista in grupos.Values)
        {
            lista.Sort((a, b) => a.FechaHora.CompareTo(b.FechaHora));
        }

        return grupos;
    }

    // Calcula los conteos de una IP a partir de sus registros.
    public static ResumenIp CalcularResumen(string direccionIp, List<Registro> registros)
    {
        return new ResumenIp
        {
            DireccionIp = direccionIp,
            TotalIntentos = registros.Count,
            IntentosExitosos = registros.Count(r => r.Resultado == ResultadoIntento.Exitoso),
            IntentosFallidos = registros.Count(r => r.Resultado == ResultadoIntento.Fallido),
            UltimoIntento = registros.Count > 0
                ? registros.Max(r => r.FechaHora)
                : DateTime.MinValue
        };
    }

    // Calcula el resumen de todas las IP, ordenado por dirección.
    public static List<ResumenIp> CalcularTodos(List<Registro> registros)
    {
        var resumenes = new List<ResumenIp>();

        foreach (KeyValuePair<string, List<Registro>> grupo in AgruparPorIp(registros))
        {
            resumenes.Add(CalcularResumen(grupo.Key, grupo.Value));
        }

        resumenes.Sort((a, b) => string.CompareOrdinal(a.DireccionIp, b.DireccionIp));
        return resumenes;
    }

    // Obtiene mu y sigma a partir de los intentos fallidos por IP
    // de los registros de referencia considerados normales.
    // Se usa la desviación estándar muestral (n - 1).
    public static EstadisticaReferencia? CalcularEstadisticaReferencia(
        List<Registro> registrosReferencia)
    {
        Dictionary<string, List<Registro>> grupos = AgruparPorIp(registrosReferencia);
        if (grupos.Count == 0)
        {
            return null;
        }

        var fallidosPorIp = grupos.Values
            .Select(lista => (double)lista.Count(r => r.Resultado == ResultadoIntento.Fallido))
            .ToList();

        double media = fallidosPorIp.Average();
        double sumaCuadrados = fallidosPorIp.Sum(x => (x - media) * (x - media));
        double desviacion = fallidosPorIp.Count > 1
            ? Math.Sqrt(sumaCuadrados / (fallidosPorIp.Count - 1))
            : 0.0;

        return new EstadisticaReferencia
        {
            Media = media,
            Desviacion = desviacion,
            CantidadIp = fallidosPorIp.Count
        };
    }
}
