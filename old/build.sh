#!/bin/bash
set -euo pipefail

# Script para contornar problemas de rede no Docker build
# Recompila a imagem do ERP.API usando modo network host

echo "🔨 Compilando ERP.API com workaround de rede..."

cd "$(dirname "$0")" || exit 1

# Opção 1: Usar docker-compose.dev.yml
if [ "${1:-}" = "dev" ]; then
  echo "Usando docker-compose.dev.yml"
  docker compose -f docker-compose.dev.yml up --build
  exit 0
fi

# Opção 2: Build direto com network host
echo "Build direto com network host..."
docker build \
  --network host \
  -f ERP.API/Dockerfile \
  -t nina_erp-api:latest \
  .

echo "✅ Build concluído com sucesso!"
echo ""
echo "Para executar os containers:"
echo "  docker compose up --build"
echo ""
echo "Ou com workaround dev:"
echo "  bash build.sh dev"
