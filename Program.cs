using System.Globalization;

namespace DetectorFuerzaBruta
{
    // Menú, estado de la aplicación y coordinación general.
    public static class Program
    {
        private static List<Registro> registros = new();
        private static List<Registro> registrosReferencia = new();

        public static void Main(string[] args)
        {
            Iniciar();
        }

        public static void Iniciar()
        {
            CargarReferencia();

            bool terminar = false;
            while (!terminar)
            {
                MostrarMenu();

                string? entrada = Console.ReadLine();
                if (!int.TryParse(entrada, out int opcion))
                {
                    Console.WriteLine("Opción inválida. Escriba un número del menú.");
                    Console.WriteLine();
                    continue;
                }

                terminar = EjecutarOpcion(opcion);
                Console.WriteLine();
            }
        }

        // Los registros de referencia considerados normales definen mu y sigma.
        private static void CargarReferencia()
        {
            registrosReferencia = LectorCSV.Leer(Configuracion.RutaReferencia);

            if (registrosReferencia.Count == 0)
            {
                Console.WriteLine($"Aviso: no se pudo cargar {Configuracion.RutaReferencia}.");
                Console.WriteLine("Sin registros de referencia no se puede calcular el umbral estadístico.");
                Console.WriteLine();
            }
        }

        public static void MostrarMenu()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("DETECTOR DE FUERZA BRUTA");
            Console.WriteLine("========================================");
            Console.WriteLine();
            Console.WriteLine("1. Iniciar monitoreo simulado");
            Console.WriteLine("2. Cargar registros CSV");
            Console.WriteLine("3. Consultar estadísticas");
            Console.WriteLine("4. Ver análisis temporal");
            Console.WriteLine("5. Comparar métodos numéricos");
            Console.WriteLine("6. Consultar alertas");
            Console.WriteLine("0. Salir");
            Console.WriteLine();
            Console.Write("Seleccione una opción: ");
        }

        // Ejecuta una opción del menú. Devuelve true si el programa debe terminar.
        public static bool EjecutarOpcion(int opcion)
        {
            switch (opcion)
            {
                case 1:
                    IniciarMonitoreoSimulado();
                    return false;
                case 2:
                    CargarCsv();
                    return false;
                case 3:
                    MostrarEstadisticas();
                    return false;
                case 4:
                    MostrarAnalisisTemporal();
                    return false;
                case 5:
                    CompararMetodos();
                    return false;
                case 6:
                    MostrarAlertas();
                    return false;
                case 0:
                    Console.WriteLine("Fin del programa.");
                    return true;
                default:
                    Console.WriteLine("Opción fuera de rango. Elija una opción del menú.");
                    return false;
            }
        }

        // Genera y procesa eventos de autenticación de forma continua.
        public static void IniciarMonitoreoSimulado()
        {
            if (!PuedeCalcularUmbral(out EstadisticaReferencia estadistica))
            {
                return;
            }

            ResultadoRaiz raiz = Detector.CalcularRaizUmbral();
            double umbral = Detector.UmbralEstadistico(raiz, estadistica);

            Console.WriteLine("========================================");
            Console.WriteLine("MONITOREO SIMULADO");
            Console.WriteLine("========================================");
            Console.WriteLine($"Umbral estadístico: {(int)Math.Ceiling(umbral)} intentos fallidos");
            Console.WriteLine("Procesando eventos de autenticación...");

            bool interactivo = !Console.IsInputRedirected;
            if (interactivo)
            {
                Console.WriteLine("Presione una tecla para detener el monitoreo.");
            }

            Console.WriteLine();

            List<Registro> eventos = Simulador.GenerarMonitoreo();
            var porIp = new Dictionary<string, List<Registro>>();
            var procesados = new List<Registro>();

            foreach (Registro evento in eventos)
            {
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
                MostrarBloqueActividad(alerta);

                if (interactivo)
                {
                    Thread.Sleep(Configuracion.PausaSimulacionMs);
                    if (Console.KeyAvailable)
                    {
                        Console.ReadKey(true);
                        break;
                    }
                }
            }

            registros.AddRange(procesados);
            Console.WriteLine($"Monitoreo finalizado. Eventos procesados: {procesados.Count}.");
            Console.WriteLine($"Los eventos simulados se agregaron al conjunto actual " +
                              $"({registros.Count} registros en total).");
        }

        public static void CargarCsv()
        {
            registros = LectorCSV.Leer(Configuracion.RutaCsv);

            if (registros.Count == 0)
            {
                Console.WriteLine("No se cargaron registros. Revise el archivo datos.csv.");
                return;
            }

            Console.WriteLine($"Registros cargados: {registros.Count}");
            Console.WriteLine($"Desde {registros[0].FechaHora:yyyy-MM-dd HH:mm:ss} " +
                              $"hasta {registros[^1].FechaHora:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();
            Console.WriteLine("REGISTROS LEÍDOS");
            Console.WriteLine(new string('-', 74));
            Console.WriteLine($"{"IP",-16}{"Usuario",-12}{"Fecha y hora",-21}{"Resultado",-10}");
            Console.WriteLine(new string('-', 74));

            foreach (Registro registro in registros)
            {
                string resultado = registro.Resultado == ResultadoIntento.Exitoso
                    ? "EXITOSO"
                    : "FALLIDO";

                Console.WriteLine(
                    $"{registro.DireccionIp,-16}" +
                    $"{registro.Usuario,-12}" +
                    $"{registro.FechaHora,-21:yyyy-MM-dd HH:mm:ss}" +
                    $"{resultado,-10}");
            }

            Console.WriteLine(new string('-', 74));
        }

        public static void MostrarEstadisticas()
        {
            if (!HayRegistros())
            {
                return;
            }

            Console.WriteLine("ESTADÍSTICAS POR IP");
            Console.WriteLine(new string('-', 78));
            Console.WriteLine($"{"IP",-16}{"Total",7}{"Fallidos",10}{"Exitosos",10}  {"Último intento",-20}");
            Console.WriteLine(new string('-', 78));

            foreach (ResumenIp resumen in Analizador.CalcularTodos(registros))
            {
                Console.WriteLine(
                    $"{resumen.DireccionIp,-16}" +
                    $"{resumen.TotalIntentos,7}" +
                    $"{resumen.IntentosFallidos,10}" +
                    $"{resumen.IntentosExitosos,10}  " +
                    $"{resumen.UltimoIntento:yyyy-MM-dd HH:mm:ss}");
            }

            Console.WriteLine(new string('-', 78));
        }

        public static void MostrarAnalisisTemporal()
        {
            if (!HayRegistros())
            {
                return;
            }

            Console.WriteLine("========================================");
            Console.WriteLine("ANÁLISIS TEMPORAL (DIFERENCIAS FINITAS)");
            Console.WriteLine("========================================");
            Console.WriteLine("h_i = t_i - t_(i-1)          v_i = 1 / h_i");
            Console.WriteLine("a_i = (v_i - v_(i-1)) / ((h_i + h_(i-1)) / 2)");
            Console.WriteLine($"Ventana móvil: últimos {Configuracion.TamanoVentana} intentos por IP");
            Console.WriteLine();

            Dictionary<string, List<Registro>> grupos = Analizador.AgruparPorIp(registros);

            foreach (string ip in grupos.Keys.OrderBy(k => k, StringComparer.Ordinal))
            {
                MostrarTablaTemporal(ip, grupos[ip]);
                Console.WriteLine();
            }
        }

        public static void MostrarTablaTemporal(string direccionIp, List<Registro> registrosIp)
        {
            ResultadoTemporal resultado = DiferenciasFinitas.Calcular(
                registrosIp,
                Configuracion.TamanoVentana);

            Console.WriteLine($"IP: {direccionIp}");
            Console.WriteLine($"  Intentos analizados: {resultado.IntentosAnalizados}   " +
                              $"Fallidos: {resultado.IntentosFallidos}");

            if (!resultado.HayDatos)
            {
                Console.WriteLine("  Se necesitan al menos dos intentos con fechas distintas.");

                if (resultado.IntervalosIgnorados > 0)
                {
                    Console.WriteLine($"  Intervalos descartados (h <= 0): {resultado.IntervalosIgnorados}");
                }

                return;
            }

            Console.WriteLine($"  {"t(s)",10}{"h(s)",10}{"v(int/s)",12}{"a",12}");

            for (int i = 0; i < resultado.Intervalos.Count; i++)
            {
                double t = (resultado.MarcasTiempo[i] - resultado.Inicio).TotalSeconds;
                string aceleracion = i >= 1
                    ? ConSigno6(resultado.Aceleraciones[i - 1])
                    : "-";

                Console.WriteLine(
                    $"  {Numero(t),10}{Numero(resultado.Intervalos[i]),10}" +
                    $"{Numero(resultado.Frecuencias[i]),12}{aceleracion,12}");
            }

            Console.WriteLine();
            Console.WriteLine($"  Frecuencia actual: {Numero2(resultado.FrecuenciaActual)} intentos/s");
            Console.WriteLine($"  Variación de frecuencia: {resultado.Variacion}");
            Console.WriteLine($"  Promedio de aceleración: {ConSigno6(resultado.PromedioAceleracion)}");

            if (resultado.IntervalosIgnorados > 0)
            {
                Console.WriteLine($"  Intervalos descartados (h <= 0): {resultado.IntervalosIgnorados}");
            }
        }

        public static void CompararMetodos()
        {
            Func<double, double> ecuacion = x =>
                NormalEstandar.Acumulada(x) - Configuracion.NivelConfianza;

            ResultadoRaiz biseccion = MetodoBiseccion.Calcular(
                ecuacion, -6.0, 6.0,
                Configuracion.Tolerancia,
                Configuracion.IteracionesMaximasBiseccion);

            ResultadoRaiz newton = MetodoNewton.Calcular(
                ecuacion, NormalEstandar.Densidad, 0.0,
                Configuracion.Tolerancia,
                Configuracion.IteracionesMaximasNewton);

            Console.WriteLine("COMPARACIÓN DE MÉTODOS NUMÉRICOS");
            Console.WriteLine("Ecuación: Phi(x) - 0.95 = 0");
            Console.WriteLine();
            Console.WriteLine($"{"Método",-14}{"Valor",12}{"Iteraciones",14}{"Error",12}{"Convergió",11}");
            Console.WriteLine(new string('-', 63));
            MostrarFilaComparacion("Bisección", biseccion);
            MostrarFilaComparacion("Newton", newton);
            Console.WriteLine(new string('-', 63));
            Console.WriteLine($"  Diferencia entre valores: " +
                              $"{Cientifico(Math.Abs(biseccion.Valor - newton.Valor))}");

            int diferencia = biseccion.Iteraciones - newton.Iteraciones;
            Console.WriteLine(diferencia >= 0
                ? $"  Newton-Raphson usó {diferencia} iteraciones menos."
                : $"  Newton-Raphson usó {Math.Abs(diferencia)} iteraciones más.");

            if (Analizador.CalcularEstadisticaReferencia(registrosReferencia) is EstadisticaReferencia estadistica)
            {
                double umbral = Detector.UmbralEstadistico(biseccion, estadistica);
                Console.WriteLine();
                Console.WriteLine($"  Umbral estadístico: T = mu + x*sigma = " +
                                  $"{Numero6(estadistica.Media)} + {Numero6(biseccion.Valor)} * " +
                                  $"{Numero6(estadistica.Desviacion)} = {Numero6(umbral)}");
                Console.WriteLine($"  Umbral entero (redondeado hacia arriba): {(int)Math.Ceiling(umbral)}");
            }
        }

        public static void MostrarAlertas()
        {
            if (!PuedeCalcularUmbral(out EstadisticaReferencia estadistica))
            {
                return;
            }

            ResultadoRaiz raiz = Detector.CalcularRaizUmbral();
            double umbral = Detector.UmbralEstadistico(raiz, estadistica);

            Console.WriteLine("========================================");
            Console.WriteLine("ALERTAS POR IP");
            Console.WriteLine("========================================");
            Console.WriteLine($"Umbral estadístico: T = mu + x*sigma = " +
                              $"{Numero6(estadistica.Media)} + {Numero6(raiz.Valor)} * " +
                              $"{Numero6(estadistica.Desviacion)} = {Numero6(umbral)} " +
                              $"-> {(int)Math.Ceiling(umbral)} intentos fallidos");
            Console.WriteLine($"Referencia: {estadistica.CantidadIp} IP consideradas normales");
            Console.WriteLine();

            if (registros.Count == 0)
            {
                Console.WriteLine("No hay registros cargados. Use la opción 2 o la opción 1.");
                Console.WriteLine();
            }
            else
            {
                Dictionary<string, List<Registro>> grupos = Analizador.AgruparPorIp(registros);

                foreach (string ip in grupos.Keys.OrderBy(k => k, StringComparer.Ordinal))
                {
                    ResultadoTemporal temporal = DiferenciasFinitas.Calcular(
                        grupos[ip],
                        Configuracion.TamanoVentana);
                    Alerta alerta = Detector.Evaluar(ip, temporal, umbral);
                    MostrarBloqueActividad(alerta, incluirMotivo: true);
                }
            }

            MostrarValidacion(umbral);
        }

        // Escenarios de prueba del informe: normal, sospechoso y acelerado,
        // con el conteo de verdaderos/falsos positivos y negativos.
        private static void MostrarValidacion(double umbral)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("VALIDACIÓN CON ESCENARIOS");
            Console.WriteLine("========================================");
            Console.WriteLine();
            Console.WriteLine($"{"Escenario",-12}{"IP",-16}{"Esperado",-30}{"Obtenido",-30}{"Correcto",8}");
            Console.WriteLine(new string('-', 96));

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

                bool acierta = alerta.Estado == caso.ResultadoEsperado;

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

                Console.WriteLine(
                    $"{caso.Escenario,-12}" +
                    $"{caso.DireccionIp,-16}" +
                    $"{caso.ResultadoEsperado,-30}" +
                    $"{alerta.Estado,-30}" +
                    $"{(acierta ? "Sí" : "No"),8}");
            }

            Console.WriteLine(new string('-', 96));
            Console.WriteLine($"Verdaderos positivos (VP): {vp}");
            Console.WriteLine($"Falsos positivos (FP):     {fp}");
            Console.WriteLine($"Verdaderos negativos (VN): {vn}");
            Console.WriteLine($"Falsos negativos (FN):     {fn}");
        }

        private static void MostrarBloqueActividad(Alerta alerta, bool incluirMotivo = false)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("ANÁLISIS DE ACTIVIDAD");
            Console.WriteLine("========================================");
            Console.WriteLine();
            Console.WriteLine($"IP: {alerta.DireccionIp}");
            Console.WriteLine();
            Console.WriteLine($"Intentos analizados: {alerta.IntentosAnalizados}");
            Console.WriteLine($"Intentos fallidos: {alerta.IntentosFallidos}");
            Console.WriteLine();
            Console.WriteLine($"Frecuencia actual: {Numero2(alerta.FrecuenciaActual)} intentos/s");
            Console.WriteLine($"Variación de frecuencia: {alerta.Variacion}");
            Console.WriteLine();
            Console.WriteLine($"Umbral estadístico: {alerta.Umbral}");
            Console.WriteLine();
            Console.WriteLine($"RESULTADO: {alerta.Estado}");

            if (incluirMotivo)
            {
                Console.WriteLine($"Motivo: {alerta.Motivo}");
            }

            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine();
        }

        private static void MostrarFilaComparacion(string nombre, ResultadoRaiz resultado)
        {
            Console.WriteLine(
                $"{nombre,-14}" +
                $"{Numero6(resultado.Valor),12}" +
                $"{resultado.Iteraciones,14}" +
                $"{Cientifico(resultado.Error),12}" +
                $"{(resultado.Convergio ? "Sí" : "No"),11}");
        }

        private static bool PuedeCalcularUmbral(out EstadisticaReferencia estadistica)
        {
            estadistica = new EstadisticaReferencia();
            EstadisticaReferencia? calculada =
                Analizador.CalcularEstadisticaReferencia(registrosReferencia);

            if (calculada is null)
            {
                Console.WriteLine($"No se pudo calcular el umbral estadístico: " +
                                  $"revise {Configuracion.RutaReferencia}.");
                return false;
            }

            estadistica = calculada;
            return true;
        }

        private static bool HayRegistros()
        {
            if (registros.Count > 0)
            {
                return true;
            }

            Console.WriteLine("No hay registros. Use la opción 2 para cargar el CSV " +
                              "o la opción 1 para el monitoreo simulado.");
            return false;
        }

        private static string Numero(double valor)
        {
            return valor.ToString("0.0000", CultureInfo.InvariantCulture);
        }

        private static string Numero2(double valor)
        {
            return valor.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static string Numero6(double valor)
        {
            return valor.ToString("0.000000", CultureInfo.InvariantCulture);
        }

        private static string ConSigno6(double valor)
        {
            return valor.ToString("+0.000000;-0.000000;0.000000", CultureInfo.InvariantCulture);
        }

        private static string Cientifico(double valor)
        {
            return valor.ToString("0.00e+0", CultureInfo.InvariantCulture);
        }
    }

}
