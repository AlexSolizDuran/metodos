#!/usr/bin/env bash
set -euo pipefail

# Instala el SDK de .NET 8 de forma local para compilar Blazor WebAssembly en Vercel.
DOTNET_DIR="${PWD}/.dotnet"
export DOTNET_ROOT="${DOTNET_DIR}"
export PATH="${DOTNET_DIR}:${PATH}"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

if command -v dotnet >/dev/null 2>&1; then
  # En local ya está instalado; en Vercel normalmente no existe.
  DOTNET_BIN="$(command -v dotnet)"
else
  if [ ! -x "${DOTNET_DIR}/dotnet" ]; then
    curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
    bash /tmp/dotnet-install.sh --channel 8.0 --install-dir "${DOTNET_DIR}"
  fi

  DOTNET_BIN="${DOTNET_DIR}/dotnet"
fi

"${DOTNET_BIN}" publish Web/DetectorFuerzaBruta.Web.csproj \
  -c Release \
  -o Web/publish

# Vercel sirve los archivos directos; las variantes precomprimidas no se usan.
find Web/publish/wwwroot -type f \( -name '*.gz' -o -name '*.br' \) -delete
