#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"

URL="${URLS:-http://localhost:5080}"

echo "Iniciando Detector de Fuerza Bruta (Blazor WebAssembly)..."
echo "Abre ${URL} en el navegador."
echo

exec dotnet run --project Web/DetectorFuerzaBruta.Web.csproj --urls "${URL}"
