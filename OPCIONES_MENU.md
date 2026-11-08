# Opciones del menú — Qué hace cada opción

> Guía de cada opción del menú de consola del proyecto **DetectorFuerzaBruta**.
> El menú se muestra en `Program.cs:51-66` y se ejecuta en `Program.cs:69-98`.

## Menú general

```text
========================================
DETECTOR DE FUERZA BRUTA
========================================

1. Iniciar monitoreo simulado
2. Cargar registros CSV
3. Consultar estadísticas
4. Ver análisis temporal
5. Comparar métodos numéricos
6. Consultar alertas
0. Salir

Seleccione una opción:
```

| Opción | Acción | Necesita `datos.csv` cargado | Necesita `referencia.csv` | Función principal |
|---|---|---|---|---|
| `1` | Iniciar monitoreo simulado | No, genera datos en memoria | Sí, para el umbral | `Program.cs:101 IniciarMonitoreoSimulado()` |
| `2` | Cargar registros CSV | No, lo carga | Sí, se carga solo al iniciar | `Program.cs:163 CargarCsv()` |
| `3` | Consultar estadísticas | **Sí** | No | `Program.cs:198 MostrarEstadisticas()` |
| `4` | Ver análisis temporal | **Sí** | No | `Program.cs:223 MostrarAnalisisTemporal()` |
| `5` | Comparar métodos numéricos | No | Sí, para mostrar el umbral | `Program.cs:294 CompararMetodos()` |
| `6` | Consultar alertas | Sí para la parte real, no para la validación | **Sí, obligatorio** | `Program.cs:336 MostrarAlertas()` + `Program.cs:380 MostrarValidacion()` |
| `0` | Salir | No | No | `Program.cs:91 return true` |

> **Idea clave:** `referencia.csv` NO se analiza como ataque. Solo sirve para calcular `mu` y `sigma` y de ahí el **umbral estadístico**. Los ataques que se analizan vienen de `datos.csv` (opción 2) o de los eventos simulados (opción 1).

Flujo recomendado:

```text
2 (cargar datos.csv) -> 3 (estadísticas) -> 4 (análisis temporal) -> 5 (comparar métodos) -> 6 (alertas y validación)
```

Si entras a `3`, `4` o `6` sin haber hecho `2` ni `1`, el programa responde:

> `No hay registros. Use la opción 2 para cargar el CSV o la opción 1 para el monitoreo simulado.`

---

## Opción 1 — Iniciar monitoreo simulado

**Qué hace:** Genera eventos de autenticación en memoria, los procesa uno por uno en orden cronológico y muestra el `ANÁLISIS DE ACTIVIDAD` después de **cada evento**. Al terminar, los eventos se **suman** a los registros actuales (`registros.AddRange`), para que después puedas usarlos en las opciones 3, 4 y 6.

**Requisitos:**

- Necesita `referencia.csv` para calcular el umbral. Si no se puede cargar, avisa y no inicia el monitoreo (`Program.cs:103`).

**Qué datos genera:** 21 eventos (`Program.cs:562-569`, clase `Simulador`):

| IP simulada | Tipo | Intentos |
|---|---|---|
| `192.168.1.10` | Normal: pocos fallos, cada 60 s | 6 (2 fallidos + 4 exitosos) |
| `192.168.1.30` | Sospechoso: muchos fallos, intervalos de 10 s → 0.5 s | 10 (9 fallidos + 1 exitoso) |
| `192.168.1.20` | Acelerado: intervalos 4 s, 2 s, 1 s, 0.5 s | 5 (4 fallidos + 1 exitoso) |

> Ojo: estas IP **coinciden** con las de `datos.csv`, pero se generan en código con `Simulador.CrearNormal / CrearSospechoso / CrearAcelerado`, no se leen del CSV. Si ya habías cargado `datos.csv` con la opción 2, al correr la opción 1 los eventos simulados se **agregan** a esos, no los reemplazan.

**Qué imprime:** Por cada evento un bloque como este:

```text
========================================
ANÁLISIS DE ACTIVIDAD
========================================

IP: 192.168.1.30

Intentos analizados: X
Intentos fallidos: Y

Frecuencia actual: Z intentos/s
Variación de frecuencia: Positiva / Negativa / Sin variación

Umbral estadístico: 5

RESULTADO: ACTIVIDAD NORMAL / ACTIVIDAD SOSPECHOSA / INDICADOR TEMPORAL SOSPECHOSO
```

**Detalle extra:** Si la entrada es interactiva, hace una pausa de 350 ms entre eventos (`Configuracion.PausaSimulacionMs` en `Program.cs:544`) y permite salir con una tecla.

---

## Opción 2 — Cargar registros CSV

**Qué hace:** Lee el archivo `datos.csv` (`Configuracion.RutaCsv = "datos.csv"` en `Program.cs:531`) con `LectorCSV.Leer` y lo deja en memoria en la lista `registros`. Es la opción que prepara los datos para el resto.

**Formato esperado de `datos.csv`:**

```csv
IP,Usuario,FechaHora,Resultado
192.168.1.10,Carlos,2026-10-31 10:00:01,EXITOSO
192.168.1.10,Carlos,2026-10-31 10:02:01,FALLIDO
```

- `FechaHora`: `yyyy-MM-dd HH:mm:ss` (también acepta milisegundos).
- `Resultado`: `EXITOSO` o `FALLIDO`.
- El encabezado y las líneas vacías se ignoran.

**Qué imprime:** Cantidad de registros leídos, rango de fechas y la tabla completa de `REGISTROS LEÍDOS`.

```text
Registros cargados: 26
Desde 2026-10-31 10:00:01 hasta 2026-10-31 10:04:50

REGISTROS LEÍDOS
----------------------------------------------------------------------------------
IP              Usuario     Fecha y hora         Resultado
----------------------------------------------------------------------------------
...
```

Si el archivo no existe o está vacío, dice `No se cargaron registros. Revise el archivo datos.csv.` y no cambia el estado actual.

---

## Opción 3 — Consultar estadísticas

**Qué hace:** Muestra una tabla de conteos por IP con `Analizador.CalcularTodos()` (`Analizador.cs:65`). No calcula probabilidades ni umbral, solo cuenta.

**Requisito:** Necesita registros cargados (opción `2` o `1`). Si no los hay, te pide hacer la opción 2 primero.

**Qué imprime:**

```text
ESTADÍSTICAS POR IP
------------------------------------------------------------------------------
IP                Total  Fallidos  Exitosos  Último intento
------------------------------------------------------------------------------
192.168.1.10          5         4         1  2026-10-31 10:17:01
192.168.1.20         10         9         1  2026-10-31 10:02:16
192.168.1.30          6         6         0  2026-10-31 10:03:28
192.168.1.40          5         3         2  2026-10-31 10:06:00
------------------------------------------------------------------------------
```

**Columnas:**

- `Total`: todos los intentos de esa IP.
- `Fallidos`: con `Resultado = FALLIDO`.
- `Exitosos`: con `Resultado = EXITOSO`.
- `Último intento`: fecha más reciente de esa IP.

---

## Opción 4 — Ver análisis temporal

**Qué hace:** Aplica **diferencias finitas** a cada IP para saber si los fallos se están acelerando. Es la Fase 1 del proyecto.

> No mira solo el total: mira el **tiempo entre intentos**. Si los intervalos se acortan, la frecuencia sube.

**Requisito:** Necesita registros cargados (opción `2` o `1`).

**Fórmulas usadas** (`DiferenciasFinitas.cs:68-70`):

```text
h_i = t_i - t_(i-1)                              intervalo en segundos
v_i = 1 / h_i                                    frecuencia (intentos/s)
a_i = (v_i - v_(i-1)) / ((h_i + h_(i-1)) / 2)    aceleración
```

**Reglas:**

- Solo usa los **últimos 10 intentos** por IP (ventana móvil, `Configuracion.TamanoVentana = 10`).
- Si dos intentos tienen la misma hora (`h_i <= 0`), ese intervalo se descarta y se cuenta como ignorado, para no dividir entre cero.
- La tendencia es **creciente** si el promedio de las aceleraciones es positivo (`PromedioAceleracion > 1e-12`).

**Qué imprime:** Una tabla por IP con `t(s)`, `h(s)`, `v(int/s)`, `a`, más la frecuencia actual y la variación.

```text
========================================
ANÁLISIS TEMPORAL (DIFERENCIAS FINITAS)
========================================

IP: 192.168.1.30
  Intentos analizados: 6   Fallidos: 6

       t(s)     h(s)    v(int/s)           a
     10.0000   10.0000      0.1000           -
     15.0000    5.0000      0.2000     +0.0133
     ...

  Frecuencia actual: 2.00 intentos/s
  Variación de frecuencia: Positiva
  Promedio de aceleración: +0.441429
```

---

## Opción 5 — Comparar métodos numéricos

**Qué hace:** Compara dos métodos para resolver la **misma ecuación** y obtener el percentil 95 de la normal estándar. Es la prueba de que Bisección y Newton-Raphson dan el mismo resultado.

**Ecuación común:**

```text
Phi(x) - 0.95 = 0
```

Donde `Phi(x)` es la acumulada de la normal estándar (aproximada con la función error de Abramowitz y Stegun 7.1.26, ver `Detector.cs:110-120`).

**Métodos comparados** (`Program.cs:294-333`):

| Método | Intervalo / inicio | Iteraciones esperadas aprox. | Error |
|---|---|---|---|
| **Bisección** | `[-6, 6]` | 23-24 | `7.15e-7` |
| **Newton-Raphson** | `x0 = 0`, derivada `phi(x)` (densidad) | 6 | `2.08e-8` |

**Qué imprime:**

```text
COMPARACIÓN DE MÉTODOS NUMÉRICOS
Ecuación: Phi(x) - 0.95 = 0

Método           Valor  Iteraciones      Error  Convergió
---------------------------------------------------------------
Bisección        1.644853            24    7.15e-7          Sí
Newton           1.644853             6    2.08e-8          Sí
---------------------------------------------------------------
  Diferencia entre valores: 2.58e-7
  Newton-Raphson usó 18 iteraciones menos.

  Umbral estadístico: T = mu + x*sigma = 3.000000 + 1.644853 * 0.894427 = 4.471201
  Umbral entero (redondeado hacia arriba): 5
```

**Requisitos:** No necesita `datos.csv`. Necesita `referencia.csv` solo para mostrar el valor final del umbral (`mu` y `sigma`). Si `referencia.csv` falta, compara los métodos igual pero omite la línea del umbral.

**Sobre el umbral (`T = mu + x·sigma`):**

1. De `referencia.csv` se cuentan los fallidos por IP y se calcula la media `mu` y la desviación muestral `sigma` (`Analizador.cs:81-106`).
2. Con los datos de ejemplo: `mu = 3.0`, `sigma ≈ 0.894427`, `x ≈ 1.644853`.
3. `T = 3.0 + 1.644853 * 0.894427 ≈ 4.471201` → umbral entero **`5` fallidos**.

---

## Opción 6 — Consultar alertas

**Qué hace:** Clasifica cada IP como **normal** o **sospechosa** usando dos criterios:

| Condición | Estado final |
|---|---|
| `fallidos >= umbral` (5) | `ACTIVIDAD SOSPECHOSA` |
| `tendencia creciente` y `fallidos < umbral` | `INDICADOR TEMPORAL SOSPECHOSO` |
| Ninguna condición | `ACTIVIDAD NORMAL` |

La lógica está en `Detector.cs:40-88` (`Detector.Evaluar`).

**Requisitos:**

- Necesita `referencia.csv` para el umbral. Sin él, responde `No se pudo calcular el umbral estadístico` y no continúa.
- Para la parte de IP reales necesita registros cargados (opción `2` o `1`).

**Qué imprime (según tengas o no datos cargados):**

La opción 6 muestra **dos secciones**, y ahí está la confusión más común:

### Parte A — Alertas reales (solo si hay registros cargados)

```text
========================================
ALERTAS POR IP
========================================
Umbral estadístico: T = mu + x*sigma = ... -> 5 intentos fallidos
Referencia: 6 IP consideradas normales

IP: 192.168.1.10
  Intentos: 5   Fallidos: 4   Frecuencia actual: 0.00 int/s
  RESULTADO: ACTIVIDAD NORMAL
  Motivo: sin indicadores de sospecha

IP: 192.168.1.30
  RESULTADO: ACTIVIDAD SOSPECHOSA
  ...
```

Estas IP **sí vienen de `datos.csv`** (o de la opción 1 si la corriste antes).

> Si no cargaste ningún CSV, esta parte se salta y verás directamente:
>
> `No hay registros cargados. Use la opción 2 o la opción 1.`

### Parte B — Validación con escenarios (siempre se muestra)

```text
========================================
VALIDACIÓN CON ESCENARIOS
========================================
Escenario   IP              Esperado                      Obtenido                      Correcto
------------------------------------------------------------------------------------------------
Normal      192.168.2.11    ACTIVIDAD NORMAL              ACTIVIDAD NORMAL                    Sí
...
Sospechoso  192.168.2.31    ACTIVIDAD SOSPECHOSA          ACTIVIDAD SOSPECHOSA                Sí
...
Acelerado   192.168.2.51    INDICADOR TEMPORAL SOSPECHOSO INDICADOR TEMPORAL SOSPECHOSO       Sí
...
Verdaderos positivos (VP): 6
Falsos positivos (FP):     0
Verdaderos negativos (VN): 4
Falsos negativos (FN):     0
```

**¿Por qué aparecen `192.168.2.11`, `.31`, `.51`? No están en ningún CSV.**

Porque **no vienen del CSV**. Son casos sintéticos generados en código en `Program.cs:558-612`, clase `Simulador.CasosValidacion()`:

- `192.168.2.11` a `.14` → escenario `Normal` (5 intentos cada 60 s, 2 fallidos).
- `192.168.2.31` a `.33` → escenario `Sospechoso` (10 intentos, intervalos 10 s → 0.5 s, 9 fallidos).
- `192.168.2.51` a `.53` → escenario `Acelerado` (5 intentos, intervalos 4 s, 2 s, 1 s, 0.5 s, 4 fallidos).

Los registros se crean en memoria con `Simulador.CrearNormal / CrearSospechoso / CrearAcelerado` y luego se pasan por `DiferenciasFinitas.Calcular` y `Detector.Evaluar`, igual que si fueran reales. Se compara `alerta.Estado` vs `caso.ResultadoEsperado` y se cuenta VP / FP / VN / FN.

**Por qué todo sale "Sí":** es una prueba interna. Los escenarios están hechos para que el detector los clasifique bien. Es útil para verificar que el código funciona, **no** es una medición de precisión sobre datos reales.

**Nota:** Con los archivos incluidos en el repo, el resultado esperado es `VP = 6, FP = 0, VN = 4, FN = 0` (ver `README.md:131`).

---

## Opción 0 — Salir

**Qué hace:** Imprime `Fin del programa.` y devuelve `true` para salir del bucle principal (`Program.cs:91-93`). No guarda ni borra nada; los registros quedan solo en memoria.

---

## Resumen rápido por objetivo

| Quiero... | Hago |
|---|---|
| Ver mis datos CSV | `2` |
| Contar fallidos por IP | `2` → `3` |
| Ver si los fallos se aceleran | `2` → `4` |
| Ver Bisección vs Newton y el umbral | `5` |
| Ver clasificación normal/sospechosa de mis datos | `2` → `6` (mira la **primera** sección `ALERTAS POR IP`) |
| Ver la prueba con IP `192.168.2.x` | `6` (mira la **segunda** sección `VALIDACIÓN CON ESCENARIOS`) |
| Probar sin CSV | `1` → `6` |

## Relación con las fases del informe

| Fase del informe | Opción del menú que la muestra |
|---|---|
| Fase 1 — Diferencias finitas | `4` (y también se usa dentro de `1` y `6`) |
| Fase 2 — Bisección | `5` |
| Fase 3A — Newton-Raphson | `5` |
| Fase 3B — Alertas | `6` (parte A) |
| Validación con escenarios | `6` (parte B) |
