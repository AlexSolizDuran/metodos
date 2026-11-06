using DetectorFuerzaBruta;

namespace DetectorFuerzaBruta.Web.Servicios;

// Fila de la matriz de validación por escenarios.
public class FilaValidacion
{
    public string Escenario { get; init; } = string.Empty;
    public string DireccionIp { get; init; } = string.Empty;
    public string Esperado { get; init; } = string.Empty;
    public string Obtenido { get; init; } = string.Empty;
    public bool Correcto { get; init; }
    public bool EsperadoSospechoso { get; init; }
    public bool EsSospechosa { get; init; }
}

public class ResumenValidacion
{
    public int VerdaderosPositivos { get; init; }
    public int FalsosPositivos { get; init; }
    public int VerdaderosNegativos { get; init; }
    public int FalsosNegativos { get; init; }
}

public class ResultadoValidacion
{
    public bool Disponible { get; init; }
    public List<FilaValidacion> Filas { get; init; } = new();
    public ResumenValidacion Resumen { get; init; } = new();
}

// Estado compartido de la aplicación: registros, referencia y cálculos derivados.
public class DatosServicio
{
    private readonly HttpClient _http;

    public DatosServicio(HttpClient http)
    {
        _http = http;
    }

    public List<Registro> Registros { get; private set; } = new();
    public List<Registro> Referencia { get; private set; } = new();
    public List<string> AdvertenciasDatos { get; private set; } = new();
    public List<string> AdvertenciasReferencia { get; private set; } = new();
    public string? ErrorCarga { get; private set; }
    public string? Mensaje { get; private set; }
    public bool Inicializado { get; private set; }

    public event Action? Cambio;

    public bool HayDatos => Registros.Count > 0;

    public Dictionary<string, List<Registro>> Grupos => Analizador.AgruparPorIp(Registros);

    public IEnumerable<string> Ips =>
        Grupos.Keys.OrderBy(k => k, StringComparer.Ordinal);

    public List<ResumenIp> Resumenes => Analizador.CalcularTodos(Registros);

    public EstadisticaReferencia? Estadistica =>
        Analizador.CalcularEstadisticaReferencia(Referencia);

    public ResultadoRaiz RaizUmbral => Detector.CalcularRaizUmbral();

    public double? Umbral
    {
        get
        {
            EstadisticaReferencia? estadistica = Estadistica;
            return estadistica is null
                ? null
                : Detector.UmbralEstadistico(RaizUmbral, estadistica);
        }
    }

    public int? UmbralEntero =>
        Umbral is double umbral ? (int)Math.Ceiling(umbral) : null;

    public List<Alerta> CalcularAlertas()
    {
        if (Umbral is not double umbral)
        {
            return new List<Alerta>();
        }

        var alertas = new List<Alerta>();

        foreach (KeyValuePair<string, List<Registro>> grupo in Grupos
                     .OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            ResultadoTemporal temporal = DiferenciasFinitas.Calcular(
                grupo.Value,
                Configuracion.TamanoVentana);
            alertas.Add(Detector.Evaluar(grupo.Key, temporal, umbral));
        }

        return alertas;
    }

    public ResultadoValidacion CalcularValidacion()
    {
        if (Umbral is not double umbral)
        {
            return new ResultadoValidacion();
        }

        var filas = new List<FilaValidacion>();
        int vp = 0;
        int fp = 0;
        int vn = 0;
        int fn = 0;

        foreach (CasoValidacion caso in Simulador.CasosValidacion())
        {
            ResultadoTemporal temporal = DiferenciasFinitas.Calcular(
                caso.Registros,
                Configuracion.TamanoVentana);
            Alerta alerta = Detector.Evaluar(caso.DireccionIp, temporal, umbral);

            bool correcto = alerta.Estado == caso.ResultadoEsperado;

            if (caso.EsperadoSospechoso && alerta.EsSospechosa)
            {
                vp++;
            }
            else if (caso.EsperadoSospechoso)
            {
                fn++;
            }
            else if (alerta.EsSospechosa)
            {
                fp++;
            }
            else
            {
                vn++;
            }

            filas.Add(new FilaValidacion
            {
                Escenario = caso.Escenario,
                DireccionIp = caso.DireccionIp,
                Esperado = caso.ResultadoEsperado,
                Obtenido = alerta.Estado,
                Correcto = correcto,
                EsperadoSospechoso = caso.EsperadoSospechoso,
                EsSospechosa = alerta.EsSospechosa
            });
        }

        return new ResultadoValidacion
        {
            Disponible = true,
            Filas = filas,
            Resumen = new ResumenValidacion
            {
                VerdaderosPositivos = vp,
                FalsosPositivos = fp,
                VerdaderosNegativos = vn,
                FalsosNegativos = fn
            }
        };
    }

    public async Task InicializarAsync()
    {
        if (Inicializado)
        {
            return;
        }

        Inicializado = true;
        await CargarDemoAsync();
    }

    public async Task CargarDemoAsync()
    {
        try
        {
            string contenidoDatos = await _http.GetStringAsync("datos.csv");
            string contenidoReferencia = await _http.GetStringAsync("referencia.csv");

            ResultadoLectura datos = LectorCSV.LeerTexto(contenidoDatos);
            ResultadoLectura referencia = LectorCSV.LeerTexto(contenidoReferencia);

            Registros = datos.Registros;
            Referencia = referencia.Registros;
            AdvertenciasDatos = datos.Advertencias;
            AdvertenciasReferencia = referencia.Advertencias;
            ErrorCarga = null;
            Mensaje = $"Datos demo cargados: {Registros.Count} registros y " +
                      $"{Referencia.Count} registros de referencia.";
        }
        catch (Exception ex)
        {
            ErrorCarga = $"No se pudieron cargar los datos demo: {ex.Message}";
        }

        Notificar();
    }

    public void CargarDatos(string contenido)
    {
        ResultadoLectura resultado = LectorCSV.LeerTexto(contenido);
        Registros = resultado.Registros;
        AdvertenciasDatos = resultado.Advertencias;
        ErrorCarga = null;
        Mensaje = $"Se cargaron {Registros.Count} registros.";
        Notificar();
    }

    public void CargarReferencia(string contenido)
    {
        ResultadoLectura resultado = LectorCSV.LeerTexto(contenido);
        Referencia = resultado.Registros;
        AdvertenciasReferencia = resultado.Advertencias;
        ErrorCarga = null;
        Mensaje = $"Se cargaron {Referencia.Count} registros de referencia.";
        Notificar();
    }

    public void LimpiarDatos()
    {
        Registros = new List<Registro>();
        AdvertenciasDatos = new List<string>();
        Mensaje = "Se quitaron los registros cargados.";
        Notificar();
    }

    public void LimpiarReferencia()
    {
        Referencia = new List<Registro>();
        AdvertenciasReferencia = new List<string>();
        Mensaje = "Se quitaron los registros de referencia.";
        Notificar();
    }

    private void Notificar()
    {
        Cambio?.Invoke();
    }
}
