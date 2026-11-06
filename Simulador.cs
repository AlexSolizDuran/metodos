namespace DetectorFuerzaBruta;

// Constantes del sistema.
public static class Configuracion
{
    public const string RutaCsv = "datos.csv";
    public const string RutaReferencia = "referencia.csv";
    public const int TamanoVentana = 10;

    // Umbral estadístico (percentil 95 de la normal estándar).
    public const double NivelConfianza = 0.95;

    // Métodos numéricos.
    public const double Tolerancia = 0.000001;
    public const int IteracionesMaximasBiseccion = 100;
    public const int IteracionesMaximasNewton = 50;

    // Monitoreo simulado.
    public const int PausaSimulacionMs = 350;
}

// Caso de prueba usado en la validación por escenarios.
public class CasoValidacion
{
    public string Escenario { get; set; } = string.Empty;
    public string DireccionIp { get; set; } = string.Empty;
    public List<Registro> Registros { get; set; } = new();
    public string ResultadoEsperado { get; set; } = "ACTIVIDAD NORMAL";
    public bool EsperadoSospechoso => ResultadoEsperado != "ACTIVIDAD NORMAL";
}

// Genera los eventos simulados del monitoreo y de los escenarios de validación.
public static class Simulador
{
    private static readonly DateTime Inicio = new(2026, 10, 31, 10, 0, 0);

    public static List<Registro> GenerarMonitoreo()
    {
        var eventos = new List<Registro>();
        eventos.AddRange(CrearNormal("192.168.1.10", Inicio, 6, 2));
        eventos.AddRange(CrearSospechoso("192.168.1.30", Inicio));
        eventos.AddRange(CrearAcelerado("192.168.1.20", Inicio));
        return eventos.OrderBy(r => r.FechaHora).ToList();
    }

    public static List<CasoValidacion> CasosValidacion()
    {
        var casos = new List<CasoValidacion>();

        for (int i = 0; i < 4; i++)
        {
            string ip = $"192.168.2.{11 + i}";
            casos.Add(new CasoValidacion
            {
                Escenario = "Normal",
                DireccionIp = ip,
                Registros = CrearNormal(ip, Inicio, 5, 2),
                ResultadoEsperado = "ACTIVIDAD NORMAL"
            });
        }

        for (int i = 0; i < 3; i++)
        {
            string ip = $"192.168.2.{31 + i}";
            casos.Add(new CasoValidacion
            {
                Escenario = "Sospechoso",
                DireccionIp = ip,
                Registros = CrearSospechoso(ip, Inicio),
                ResultadoEsperado = "ACTIVIDAD SOSPECHOSA"
            });
        }

        for (int i = 0; i < 3; i++)
        {
            string ip = $"192.168.2.{51 + i}";
            casos.Add(new CasoValidacion
            {
                Escenario = "Acelerado",
                DireccionIp = ip,
                Registros = CrearAcelerado(ip, Inicio),
                ResultadoEsperado = "INDICADOR TEMPORAL SOSPECHOSO"
            });
        }

        return casos;
    }

    // Pocos fallos distribuidos en el tiempo, con intervalos constantes.
    private static List<Registro> CrearNormal(
        string direccionIp,
        DateTime inicio,
        int intentos,
        int fallidos)
    {
        var lista = new List<Registro>();

        for (int i = 0; i < intentos; i++)
        {
            lista.Add(new Registro
            {
                DireccionIp = direccionIp,
                Usuario = "usuario",
                FechaHora = inicio.AddSeconds(i * 60),
                Resultado = i < fallidos
                    ? ResultadoIntento.Fallido
                    : ResultadoIntento.Exitoso
            });
        }

        return lista;
    }

    // Muchos fallos en pocos segundos: diez intentos con el último a 0.5 s.
    private static List<Registro> CrearSospechoso(string direccionIp, DateTime inicio)
    {
        double[] intervalos = { 10.0, 7.0, 5.0, 3.5, 2.5, 1.8, 1.3, 0.9, 0.5 };
        var lista = new List<Registro>();
        DateTime actual = inicio;

        lista.Add(new Registro
        {
            DireccionIp = direccionIp,
            Usuario = "usuario",
            FechaHora = actual,
            Resultado = ResultadoIntento.Exitoso
        });

        foreach (double intervalo in intervalos)
        {
            actual = actual.AddSeconds(intervalo);
            lista.Add(new Registro
            {
                DireccionIp = direccionIp,
                Usuario = "usuario",
                FechaHora = actual,
                Resultado = ResultadoIntento.Fallido
            });
        }

        return lista;
    }

    // Los intervalos entre fallos disminuyen progresivamente.
    private static List<Registro> CrearAcelerado(string direccionIp, DateTime inicio)
    {
        double[] intervalos = { 4.0, 2.0, 1.0, 0.5 };
        var lista = new List<Registro>();
        DateTime actual = inicio;

        lista.Add(new Registro
        {
            DireccionIp = direccionIp,
            Usuario = "usuario",
            FechaHora = actual,
            Resultado = ResultadoIntento.Exitoso
        });

        foreach (double intervalo in intervalos)
        {
            actual = actual.AddSeconds(intervalo);
            lista.Add(new Registro
            {
                DireccionIp = direccionIp,
                Usuario = "usuario",
                FechaHora = actual,
                Resultado = ResultadoIntento.Fallido
            });
        }

        return lista;
    }
}
