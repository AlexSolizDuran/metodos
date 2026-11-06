using System.Globalization;

namespace DetectorFuerzaBruta;

// Resultado de leer un CSV: registros válidos y avisos de líneas omitidas.
public class ResultadoLectura
{
    public List<Registro> Registros { get; } = new();
    public List<string> Advertencias { get; } = new();
}

// Lee y valida el contenido CSV de registros de autenticación.
public static class LectorCSV
{
    // Formatos de fecha aceptados: con o sin fracción de segundo.
    private static readonly string[] FormatosFecha =
    {
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-dd HH:mm:ss.fff"
    };

    // Lee un CSV desde el disco. Conserva la salida de consola del programa original.
    public static List<Registro> Leer(string ruta)
    {
        if (!File.Exists(ruta))
        {
            Console.WriteLine($"No se encontró el archivo: {ruta}");
            return new List<Registro>();
        }

        ResultadoLectura resultado = LeerTexto(File.ReadAllText(ruta));

        foreach (string advertencia in resultado.Advertencias)
        {
            Console.WriteLine(advertencia);
        }

        return resultado.Registros;
    }

    // Lee un CSV desde texto. Usado por la interfaz web para procesar archivos subidos.
    public static ResultadoLectura LeerTexto(string contenido)
    {
        var resultado = new ResultadoLectura();

        if (string.IsNullOrWhiteSpace(contenido))
        {
            return resultado;
        }

        string[] lineas = contenido
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Split('\n');

        bool primeraLinea = true;

        for (int i = 0; i < lineas.Length; i++)
        {
            // TrimStart elimina el BOM que algunos editores agregan al inicio.
            string linea = lineas[i].Trim().TrimStart('\uFEFF');
            if (string.IsNullOrWhiteSpace(linea))
            {
                continue;
            }

            if (primeraLinea)
            {
                primeraLinea = false;

                if (linea.StartsWith("IP", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
            }

            Registro? registro = ConvertirLinea(linea, i + 1, resultado.Advertencias);
            if (registro is not null)
            {
                resultado.Registros.Add(registro);
            }
        }

        resultado.Registros.Sort((a, b) => a.FechaHora.CompareTo(b.FechaHora));
        return resultado;
    }

    // Convierte una línea del CSV en un registro. Devuelve null si la línea es inválida.
    public static Registro? ConvertirLinea(
        string linea,
        int numeroLinea,
        ICollection<string>? advertencias = null)
    {
        string[] partes = linea.Split(',');
        if (partes.Length != 4)
        {
            AgregarAdvertencia(advertencias,
                $"Línea {numeroLinea} ignorada: se esperaban 4 columnas.");
            return null;
        }

        string direccionIp = partes[0].Trim();
        string usuario = partes[1].Trim();
        string textoFecha = partes[2].Trim();
        string textoResultado = partes[3].Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(direccionIp) || string.IsNullOrWhiteSpace(usuario))
        {
            AgregarAdvertencia(advertencias,
                $"Línea {numeroLinea} ignorada: IP o usuario vacío.");
            return null;
        }

        if (!DateTime.TryParseExact(
                textoFecha,
                FormatosFecha,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime fechaHora))
        {
            AgregarAdvertencia(advertencias,
                $"Línea {numeroLinea} ignorada: fecha inválida ({textoFecha}).");
            return null;
        }

        ResultadoIntento resultado;
        if (textoResultado == "EXITOSO")
        {
            resultado = ResultadoIntento.Exitoso;
        }
        else if (textoResultado == "FALLIDO")
        {
            resultado = ResultadoIntento.Fallido;
        }
        else
        {
            AgregarAdvertencia(advertencias,
                $"Línea {numeroLinea} ignorada: resultado desconocido ({textoResultado}).");
            return null;
        }

        return new Registro
        {
            DireccionIp = direccionIp,
            Usuario = usuario,
            FechaHora = fechaHora,
            Resultado = resultado
        };
    }

    private static void AgregarAdvertencia(
        ICollection<string>? advertencias,
        string mensaje)
    {
        if (advertencias is null)
        {
            Console.WriteLine(mensaje);
            return;
        }

        advertencias.Add(mensaje);
    }
}
