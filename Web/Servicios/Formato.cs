using System.Globalization;

namespace DetectorFuerzaBruta.Web.Servicios;

// Formato numérico estable, independiente de la cultura del navegador.
public static class Formato
{
    public static string Numero(double valor, int decimales = 4)
    {
        if (decimales <= 0)
        {
            return valor.ToString("0", CultureInfo.InvariantCulture);
        }

        return valor.ToString("0." + new string('#', decimales),
            CultureInfo.InvariantCulture);
    }

    public static string ConSigno(double valor, int decimales = 6)
    {
        string patron = decimales <= 0
            ? "+0;-0;0"
            : "+0." + new string('0', decimales) + ";-0." + new string('0', decimales) + ";0." +
              new string('0', decimales);

        return valor.ToString(patron, CultureInfo.InvariantCulture);
    }

    public static string Cientifico(double valor)
    {
        return valor.ToString("0.00e+0", CultureInfo.InvariantCulture);
    }

    public static string Coordenada(double valor)
    {
        return valor.ToString("0.##", CultureInfo.InvariantCulture);
    }

    public static string Fecha(DateTime valor)
    {
        return valor.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }

    public static string Hora(DateTime valor)
    {
        return valor.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
    }

    public static string ClaseEstado(string estado)
    {
        return estado switch
        {
            "ACTIVIDAD SOSPECHOSA" => "badge badge-peligro",
            "INDICADOR TEMPORAL SOSPECHOSO" => "badge badge-aviso",
            _ => "badge badge-ok"
        };
    }

    public static string ClaseAlerta(string estado)
    {
        return estado switch
        {
            "ACTIVIDAD SOSPECHOSA" => "alerta alerta-peligro",
            "INDICADOR TEMPORAL SOSPECHOSO" => "alerta alerta-aviso",
            _ => "alerta alerta-ok"
        };
    }
}
