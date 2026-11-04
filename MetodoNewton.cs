namespace DetectorFuerzaBruta;

// Metodo de Newton-Raphson:
//   x_(n+1) = x_n - f(x_n) / f'(x_n)
// Necesita la funcion y su derivada.
public static class MetodoNewton
{
    public static ResultadoRaiz Calcular(
        Func<double, double> f,
        Func<double, double> derivada,
        double x0,
        double tolerancia,
        int maxIteraciones)
    {
        var resultado = new ResultadoRaiz();

        double x = x0;
        double error = double.MaxValue;
        int iteracion = 0;

        while (iteracion < maxIteraciones && error > tolerancia)
        {
            double fx = Evaluar(f, x);
            double dfx = Evaluar(derivada, x);

            // Si la derivada es cero no se puede dividir.
            if (Math.Abs(dfx) < 1e-12)
            {
                resultado.Convergio = false;
                break;
            }

            double siguiente = x - fx / dfx;
            iteracion++;
            error = Math.Abs(siguiente - x);

            resultado.Pasos.Add(new PasoRaiz
            {
                Iteracion = iteracion,
                Punto = x,
                Funcion = fx,
                Derivada = dfx,
                Error = error
            });

            x = siguiente;
        }

        resultado.Valor = x;
        resultado.Iteraciones = iteracion;
        resultado.Error = error;
        resultado.Convergio = error <= tolerancia;

        return resultado;
    }

    // Evalua la funcion (o su derivada) en un punto.
    public static double Evaluar(Func<double, double> f, double x)
    {
        return f(x);
    }
}
