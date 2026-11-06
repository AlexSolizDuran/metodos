#!/usr/bin/env bash
set -euo pipefail

# Instala el SDK de .NET 8 de forma local para compilar Blazor WebAssembly en Vercel.
DOTNET_DIR="${PWD}/.dotnet"
export DOTNET_ROOT="${DOTNET_DIR}"
export PATH="${DOTNET_DIR}:${PATH}"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

if [ ! -x "${DOTNET_DIR}/dotnet" ]; then
  curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
  bash /tmp/dotnet-install.sh --channel 8.0 --install-dir "${DOTNET_DIR}"
fi

"${DOTNET_DIR}/dotnet" publish Web/DetectorFuerzaBruta.Web.csproj \
  -c Release \
  -o Web/publish
