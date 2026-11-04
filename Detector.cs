namespace DetectorFuerzaBruta;

// Alerta generada para una dirección IP.
public class Alerta
{
    public string DireccionIp { get; set; } = string.Empty;
    public int IntentosAnalizados { get; set; }
    public int IntentosFallidos { get; set; }
    public double FrecuenciaActual { get; set; }
    public string Variacion { get; set; } = "Sin datos";
    public bool TendenciaCreciente { get; set; }
    public int Umbral { get; set; }
    public string Estado { get; set; } = "ACTIVIDAD NORMAL";
    public string Motivo { get; set; } = string.Empty;

    public bool EsSospechosa => Estado != "ACTIVIDAD NORMAL";
}

// Calcula el umbral estadístico y aplica los criterios de detección.
public static class Detector
{
    // Resuelve Phi(x) - NivelConfianza = 0 con el método de bisección.
    public static ResultadoRaiz CalcularRaizUmbral()
    {
        return MetodoBiseccion.Calcular(
            x => NormalEstandar.Acumulada(x) - Configuracion.NivelConfianza,
            -6.0,
            6.0,
            Configuracion.Tolerancia,
            Configuracion.IteracionesMaximasBiseccion);
    }

    // T = mu + x * sigma.
    public static double UmbralEstadistico(ResultadoRaiz raiz, EstadisticaReferencia estadistica)
    {
        return estadistica.Media + raiz.Valor * estadistica.Desviacion;
    }

    // Clasifica el comportamiento de una IP a partir de su ventana de intentos.
    public static Alerta Evaluar(
        string direccionIp,
        ResultadoTemporal temporal,
        double umbralEstadistico)
    {
        int umbralEntero = (int)Math.Ceiling(umbralEstadistico);

        var alerta = new Alerta
        {
            DireccionIp = direccionIp,
            IntentosAnalizados = temporal.IntentosAnalizados,
            IntentosFallidos = temporal.IntentosFallidos,
            FrecuenciaActual = temporal.FrecuenciaActual,
            Variacion = temporal.Variacion,
            TendenciaCreciente = temporal.TendenciaCreciente,
            Umbral = umbralEntero
        };

        var motivos = new List<string>();

        if (alerta.IntentosFallidos >= umbralEntero)
        {
            motivos.Add("supera el umbral de intentos fallidos");
        }

        if (temporal.TendenciaCreciente)
        {
            motivos.Add("la frecuencia de intentos está aumentando");
        }

        if (alerta.IntentosFallidos >= umbralEntero)
        {
            alerta.Estado = "ACTIVIDAD SOSPECHOSA";
        }
        else if (temporal.TendenciaCreciente)
        {
            alerta.Estado = "INDICADOR TEMPORAL SOSPECHOSO";
        }
        else
        {
            alerta.Estado = "ACTIVIDAD NORMAL";
        }

        alerta.Motivo = motivos.Count > 0
            ? string.Join("; ", motivos)
            : "sin indicadores de sospecha";

        return alerta;
    }
}

// Distribución normal estándar: Phi(x), phi(x) y la función error.
// Se usa la aproximación de Abramowitz y Stegun 7.1.26 para erf,
// con un error máximo de 1.5e-7.
public static class NormalEstandar
{
    // Phi(x) = 0.5 * (1 + erf(x / sqrt(2))).
    public static double Acumulada(double x)
    {
        return 0.5 * (1.0 + FuncionError(x / Math.Sqrt(2.0)));
    }

    // phi(x) = exp(-x^2 / 2) / sqrt(2*pi).
    // Es también la derivada de Phi(x) y se usa en Newton-Raphson.
    public static double Densidad(double x)
    {
        return Math.Exp(-x * x / 2.0) / Math.Sqrt(2.0 * Math.PI);
    }

    // Aproximación polinómica de la función error.
    public static double FuncionError(double x)
    {
        double signo = x < 0 ? -1.0 : 1.0;
        double valor = Math.Abs(x);
        double t = 1.0 / (1.0 + 0.3275911 * valor);

        double polinomio = (((((1.061405429 * t - 1.453152027) * t) + 1.421413741) * t
                              - 0.284496736) * t + 0.254829592) * t;

        return signo * (1.0 - polinomio * Math.Exp(-valor * valor));
    }
}
