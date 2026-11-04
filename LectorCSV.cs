using System.Globalization;

namespace DetectorFuerzaBruta;

// Lee y valida el archivo CSV de registros de autenticación.
public static class LectorCSV
{
    // Formatos de fecha aceptados: con o sin fracción de segundo.
    private static readonly string[] FormatosFecha =
    {
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-dd HH:mm:ss.fff"
    };

    // Lee un CSV con el formato IP,Usuario,FechaHora,Resultado.
    // Las líneas mal formadas se informan y se omiten.
    public static List<Registro> Leer(string ruta)
    {
        var registros = new List<Registro>();

        if (!File.Exists(ruta))
        {
            Console.WriteLine($"No se encontró el archivo: {ruta}");
            return registros;
        }

        string[] lineas = File.ReadAllLines(ruta);
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

            Registro? registro = ConvertirLinea(linea, i + 1);
            if (registro is not null)
            {
                registros.Add(registro);
            }
        }

        registros.Sort((a, b) => a.FechaHora.CompareTo(b.FechaHora));
        return registros;
    }

    // Convierte una línea del CSV en un registro. Devuelve null si la línea es inválida.
    public static Registro? ConvertirLinea(string linea, int numeroLinea)
    {
        string[] partes = linea.Split(',');
        if (partes.Length != 4)
        {
            Console.WriteLine($"Línea {numeroLinea} ignorada: se esperaban 4 columnas.");
            return null;
        }

        string direccionIp = partes[0].Trim();
        string usuario = partes[1].Trim();
        string textoFecha = partes[2].Trim();
        string textoResultado = partes[3].Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(direccionIp) || string.IsNullOrWhiteSpace(usuario))
        {
            Console.WriteLine($"Línea {numeroLinea} ignorada: IP o usuario vacío.");
            return null;
        }

        if (!DateTime.TryParseExact(
                textoFecha,
                FormatosFecha,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime fechaHora))
        {
            Console.WriteLine($"Línea {numeroLinea} ignorada: fecha inválida ({textoFecha}).");
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
            Console.WriteLine($"Línea {numeroLinea} ignorada: resultado desconocido ({textoResultado}).");
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
}
