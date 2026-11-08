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

    // Estado del monitoreo simulado (opción 1 de la versión de consola).
    public bool SimulacionEnCurso { get; private set; }
    public int SimulacionProcesados { get; private set; }
    public int SimulacionTotal { get; private set; }
    public Alerta? SimulacionActual { get; private set; }
    public List<Alerta> SimulacionHistorial { get; } = new();
    public string? SimulacionMensaje { get; private set; }
    public string? SimulacionError { get; private set; }

    public double SimulacionProgreso =>
        SimulacionTotal == 0
            ? 0.0
            : 100.0 * SimulacionProcesados / SimulacionTotal;

    private CancellationTokenSource? _simulacionCts;

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

    // Genera los 21 eventos del monitoreo simulado y los procesa uno por uno,
    // igual que la opción 1 de la versión de consola. Al finalizar (o al
    // detenerse), los eventos procesados se agregan al conjunto actual.
    public async Task EjecutarSimulacionAsync()
    {
        if (SimulacionEnCurso)
        {
            return;
        }

        if (Umbral is not double umbral)
        {
            SimulacionError = "No se puede simular sin registros de referencia. " +
                              "Carga referencia.csv para calcular el umbral.";
            SimulacionMensaje = null;
            Notificar();
            return;
        }

        SimulacionEnCurso = true;
        SimulacionProcesados = 0;
        SimulacionTotal = 0;
        SimulacionActual = null;
        SimulacionHistorial.Clear();
        SimulacionMensaje = null;
        SimulacionError = null;

        _simulacionCts = new CancellationTokenSource();
        CancellationToken token = _simulacionCts.Token;

        List<Registro> eventos = Simulador.GenerarMonitoreo();
        SimulacionTotal = eventos.Count;

        var porIp = new Dictionary<string, List<Registro>>(StringComparer.Ordinal);
        var procesados = new List<Registro>();

        Notificar();

        try
        {
            foreach (Registro evento in eventos)
            {
                token.ThrowIfCancellationRequested();

                if (!porIp.TryGetValue(evento.DireccionIp, out List<Registro>? lista))
                {
                    lista = new List<Registro>();
                    porIp[evento.DireccionIp] = lista;
                }

                lista.Add(evento);
                procesados.Add(evento);

                ResultadoTemporal temporal = DiferenciasFinitas.Calcular(
                    lista,
                    Configuracion.TamanoVentana);
                Alerta alerta = Detector.Evaluar(evento.DireccionIp, temporal, umbral);

                SimulacionActual = alerta;
                SimulacionHistorial.Add(alerta);
                SimulacionProcesados++;
                Notificar();

                await Task.Delay(Configuracion.PausaSimulacionMs, token);
            }

            AgregarRegistrosSimulados(procesados);
            SimulacionMensaje =
                $"Monitoreo finalizado. Eventos procesados: {procesados.Count}. " +
                $"Los eventos se agregaron al conjunto actual " +
                $"({Registros.Count} registros en total).";
        }
        catch (OperationCanceledException)
        {
            AgregarRegistrosSimulados(procesados);
            SimulacionMensaje =
                $"Monitoreo detenido. Eventos procesados: {procesados.Count}. " +
                $"Los eventos se agregaron al conjunto actual " +
                $"({Registros.Count} registros en total).";
        }
        finally
        {
            SimulacionEnCurso = false;
            _simulacionCts?.Dispose();
            _simulacionCts = null;
            Notificar();
        }
    }

    public void DetenerSimulacion()
    {
        _simulacionCts?.Cancel();
    }

    private void AgregarRegistrosSimulados(List<Registro> procesados)
    {
        if (procesados.Count == 0)
        {
            return;
        }

        Registros = Registros
            .Concat(procesados)
            .OrderBy(r => r.FechaHora)
            .ToList();
    }

    // Al cambiar los datos o la referencia, la simulación anterior deja de
    // representar el estado actual y se limpia de la vista.
    private void ReiniciarVistaSimulacion()
    {
        if (SimulacionEnCurso)
        {
            return;
        }

        SimulacionProcesados = 0;
        SimulacionTotal = 0;
        SimulacionActual = null;
        SimulacionHistorial.Clear();
        SimulacionMensaje = null;
        SimulacionError = null;
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
        if (SimulacionEnCurso)
        {
            return;
        }

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
            ReiniciarVistaSimulacion();
        }
        catch (Exception ex)
        {
            ErrorCarga = $"No se pudieron cargar los datos demo: {ex.Message}";
        }

        Notificar();
    }

    public void LimpiarDatos()
    {
        if (SimulacionEnCurso)
        {
            return;
        }

        Registros = new List<Registro>();
        AdvertenciasDatos = new List<string>();
        Mensaje = "Se quitaron los registros cargados.";
        ReiniciarVistaSimulacion();
        Notificar();
    }

    public void LimpiarReferencia()
    {
        if (SimulacionEnCurso)
        {
            return;
        }

        Referencia = new List<Registro>();
        AdvertenciasReferencia = new List<string>();
        Mensaje = "Se quitaron los registros de referencia.";
        ReiniciarVistaSimulacion();
        Notificar();
    }

    private void Notificar()
    {
        Cambio?.Invoke();
    }
}
