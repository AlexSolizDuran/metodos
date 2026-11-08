# Detector de Fuerza Bruta

Programa de consola en **C# / .NET 8** que analiza registros de autenticación para detectar posibles ataques de fuerza bruta. Combina estadística descriptiva con tres métodos numéricos: **diferencias finitas**, **bisección** y **Newton-Raphson**.

El sistema agrupa los intentos por dirección IP, analiza cómo evoluciona la frecuencia de autenticación en una ventana móvil y compara la cantidad de intentos fallidos contra un umbral calculado a partir de registros considerados normales.

## Características

- Carga de registros desde `datos.csv` o generación de un monitoreo simulado.
- Conteo de intentos totales, fallidos y exitosos por IP.
- Análisis temporal con ventana móvil de los últimos **10 intentos** por IP:
  - `h_i = t_i - t_(i-1)` (intervalo en segundos)
  - `v_i = 1 / h_i` (frecuencia en intentos/s)
  - `a_i = (v_i - v_(i-1)) / ((h_i + h_(i-1)) / 2)` (aceleración)
- Cálculo del umbral estadístico `T = μ + x·σ`, donde `x` se obtiene resolviendo `Φ(x) - 0.95 = 0`.
- Clasificación en tres estados: **actividad normal**, **actividad sospechosa** e **indicador temporal sospechoso**.
- Comparación de bisección y Newton-Raphson: valor, iteraciones, error y convergencia.
- Validación con escenarios normal, sospechoso y acelerado, incluyendo la matriz VP / FP / VN / FN.
- Control de intervalos inválidos (`h <= 0`) para evitar divisiones entre cero.

## Requisitos

- [.NET SDK 8.0](https://dotnet.microsoft.com/download) o superior.

Verificar la instalación:

```bash
dotnet --version
```

## Cómo ejecutar

```bash
git clone <URL-DEL-REPOSITORIO>
cd DetectorFuerzaBruta
dotnet run
```

También se puede compilar y ejecutar por separado:

```bash
dotnet build
dotnet run --no-build
```

El programa es interactivo: muestra un menú y espera el número de la opción.

## Interfaz web (Blazor WebAssembly)

El proyecto incluye una versión web con la misma lógica de análisis, pensada
para ejecutarse en el navegador y desplegarse en Vercel.

Ejecutar en local:

```bash
./run-local.sh
```

También se puede iniciar directamente:

```bash
dotnet run --project Web/DetectorFuerzaBruta.Web.csproj --urls http://localhost:5080
```

Abre <http://localhost:5080>.

La interfaz web permite:

- Cargar los datos demo incluidos (`datos.csv` y `referencia.csv`).
- Subir tus propios archivos CSV.
- Consultar estadísticas, análisis temporal, métodos numéricos y alertas.
- **Ejecutar el monitoreo simulado**: genera 21 eventos, los analiza uno por
  uno y los agrega al conjunto de datos actual.

### Desplegar en Vercel

La app usa **Blazor WebAssembly**, por lo que se publica como sitio estático.
`vercel-build.sh` instala el SDK de .NET 8 y genera `Web/publish/wwwroot`, que
es el directorio configurado en `vercel.json`.

```bash
npx vercel login
npx vercel --prod
```

> Blazor Server no es compatible con Vercel porque requiere un servidor .NET
> con conexión persistente. Por eso la versión web usa WebAssembly.

## Menú

| Opción | Acción |
|--------|--------|
| `1` | Iniciar monitoreo simulado |
| `2` | Cargar registros CSV |
| `3` | Consultar estadísticas |
| `4` | Ver análisis temporal |
| `5` | Comparar métodos numéricos |
| `6` | Consultar alertas |
| `0` | Salir |

Flujo recomendado: opción `2` para cargar los datos, luego `3`, `4` y `5` para revisar los análisis, y `6` para ver las alertas y la validación.

## Formato del CSV

```csv
IP,Usuario,FechaHora,Resultado
192.168.1.10,Carlos,2026-10-31 10:00:01,EXITOSO
192.168.1.10,Carlos,2026-10-31 10:02:01,FALLIDO
```

- **FechaHora**: `yyyy-MM-dd HH:mm:ss` (también se acepta con milisegundos, `yyyy-MM-dd HH:mm:ss.fff`).
- **Resultado**: `EXITOSO` o `FALLIDO`.
- Las líneas vacías y el encabezado se ignoran; las líneas inválidas se informan y se omiten.

Archivos incluidos:

| Archivo | Uso |
|---------|-----|
| `datos.csv` | Registros de autenticación que se analizan. |
| `referencia.csv` | IP consideradas normales; de aquí se obtienen `μ` y `σ`. |

## Cálculo del umbral

1. Se cuentan los intentos fallidos por IP en `referencia.csv`.
2. Se calcula la media `μ` y la desviación estándar muestral `σ`.
3. Se resuelve `Φ(x) - 0.95 = 0` con bisección (y se compara con Newton-Raphson).
4. Se obtiene `T = μ + x·σ` y se redondea hacia arriba para comparar con los intentos fallidos.

Con los datos de referencia incluidos:

```text
μ = 3.000000
σ = 0.894427
x = 1.644853   (percentil 95 de la normal estándar)
T = 4.471201   → umbral entero = 5 intentos fallidos
```

La función `Φ(x)` se aproxima con la función error de Abramowitz y Stegun 7.1.26 (error máximo aproximado `1.5e-7`), documentada en `Detector.cs`.

## Reglas de detección

| Condición | Estado |
|-----------|--------|
| Intentos fallidos en la ventana `>=` umbral | `ACTIVIDAD SOSPECHOSA` |
| Frecuencia en aumento y fallidos por debajo del umbral | `INDICADOR TEMPORAL SOSPECHOSO` |
| Sin ninguna de las condiciones anteriores | `ACTIVIDAD NORMAL` |

## Estructura del proyecto

```text
DetectorFuerzaBruta.csproj
Program.cs              Menú de consola y coordinación
Simulador.cs            Configuración, simulación y escenarios de validación
Registro.cs             Modelo de un intento de autenticación
LectorCSV.cs            Lectura y validación del CSV
Analizador.cs           Agrupación por IP, conteos y estadística de referencia
DiferenciasFinitas.cs   Ventana móvil, intervalos, frecuencias y aceleraciones
MetodoBiseccion.cs      Bisección y resultado de raíces
MetodoNewton.cs         Newton-Raphson
Detector.cs             Umbral, criterios, alertas y distribución normal
datos.csv               Datos de entrada
referencia.csv          Datos normales de referencia
run-local.sh            Arranca la interfaz web en local
vercel.json             Configuración de despliegue en Vercel
vercel-build.sh         Build de Blazor WebAssembly para Vercel
Web/                    Interfaz Blazor WebAssembly
```

## Resultados esperados

Con los archivos incluidos:

- **26** registros cargados desde `datos.csv`.
- Umbral estadístico: **5** intentos fallidos.
- Bisección: **23 iteraciones**, error `7.15e-7`.
- Newton-Raphson: **6 iteraciones**, error `2.08e-8`.
- Validación: **VP = 6, FP = 0, VN = 4, FN = 0**.

## Notas

- `referencia.csv` es obligatorio: sin él no se puede calcular el umbral.
- El monitoreo simulado procesa 21 eventos y los agrega al conjunto actual, de modo que las demás opciones puedan analizarlos.
- La validación por escenarios se muestra dentro de la opción `6`, ya que el menú no tiene una opción independiente para ella.
- El monitoreo simulado solo hace pausas y acepta una tecla para detenerse cuando la entrada es interactiva; si la entrada está redirigida, procesa todos los eventos sin pausa.
