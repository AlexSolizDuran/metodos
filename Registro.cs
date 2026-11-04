namespace DetectorFuerzaBruta;

// Resultado de un intento de autenticación.
public enum ResultadoIntento
{
    Exitoso,
    Fallido
}

// Representa un evento de autenticación leído del CSV o generado por el simulador.
public class Registro
{
    public string DireccionIp { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public DateTime FechaHora { get; set; }
    public ResultadoIntento Resultado { get; set; }
}
