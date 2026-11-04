namespace DetectorFuerzaBruta;

// Resultado de un método numérico de búsqueda de raíces.
public class ResultadoRaiz
{
    public double Valor { get; set; }
    public int Iteraciones { get; set; }
    public double Error { get; set; }
    public bool Convergio { get; set; }

    // Una entrada por iteración, para poder mostrar la tabla del método.
    public List<PasoRaiz> Pasos { get; } = new();
}

// Datos de una iteración. Bisección usa A, B, Punto y Función.
// Newton usa Punto, Función y Derivada.
public class PasoRaiz
{
    public int Iteracion { get; set; }
    public double A { get; set; }
    public double B { get; set; }
    public double Punto { get; set; }
    public double Funcion { get; set; }
    public double Derivada { get; set; }
    public double Error { get; set; }
}

// Método de bisección: parte el intervalo [a, b] a la mitad hasta
// que el error (b - a) / 2 sea menor que la tolerancia.
public static class MetodoBiseccion
{
    public static ResultadoRaiz Calcular(
        Func<double, double> f,
        double a,
        double b,
        double tolerancia,
        int maxIteraciones)
    {
        var resultado = new ResultadoRaiz();

        double fa = Evaluar(f, a);

        if (fa == 0.0)
        {
            resultado.Valor = a;
            resultado.Convergio = true;
            return resultado;
        }

        double fb = Evaluar(f, b);

        if (fb == 0.0)
        {
            resultado.Valor = b;
            resultado.Convergio = true;
            return resultado;
        }

        // Sin cambio de signo no se puede garantizar una raíz.
        if (fa * fb > 0)
        {
            resultado.Error = Math.Abs(b - a);
            resultado.Convergio = false;
            return resultado;
        }

        double error = Math.Abs(b - a) / 2.0;
        int iteracion = 0;

        while (iteracion < maxIteraciones && error > tolerancia)
        {
            double c = (a + b) / 2.0;
            double fc = Evaluar(f, c);
            double aAnterior = a;
            double bAnterior = b;

            if (fc == 0.0)
            {
                a = c;
                b = c;
            }
            else if (fa * fc < 0)
            {
                b = c;
            }
            else
            {
                a = c;
                fa = fc;
            }

            iteracion++;
            error = Math.Abs(b - a) / 2.0;

            resultado.Pasos.Add(new PasoRaiz
            {
                Iteracion = iteracion,
                A = aAnterior,
                B = bAnterior,
                Punto = c,
                Funcion = fc,
                Error = error
            });

            if (fc == 0.0)
            {
                break;
            }
        }

        resultado.Valor = (a + b) / 2.0;
        resultado.Iteraciones = iteracion;
        resultado.Error = error;
        resultado.Convergio = error <= tolerancia;

        return resultado;
    }

    // Evalúa la función en un punto.
    public static double Evaluar(Func<double, double> f, double x)
    {
        return f(x);
    }
}
