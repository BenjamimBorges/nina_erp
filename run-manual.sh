#!/bin/bash
set -euo pipefail

# Script alternativo para executar ERP sem usar docker-compose networks
# Útil quando há restrições de rede do daemon Docker

echo "🐳 Iniciando containers manualmente (sem docker-compose networks)..."

cd "$(dirname "$0")" || exit 1

# Verificar se os containers já existem e remover
echo "Limpando containers anteriores..."
docker rm -f nina_erp-db 2>/dev/null || true
docker rm -f nina_erp-api 2>/dev/null || true

# Variáveis de ambiente
POSTGRES_DB=${POSTGRES_DB:-nina_erp}
POSTGRES_USER=${POSTGRES_USER:-erpuser}
POSTGRES_PASSWORD=${POSTGRES_PASSWORD:-erp_password}
API_PORT=${API_PORT:-7000}

# 1. Iniciar PostgreSQL em host network
echo "📦 Iniciando PostgreSQL..."
docker run -d \
  --name nina_erp-db \
  --network host \
  -e POSTGRES_DB="$POSTGRES_DB" \
  -e POSTGRES_USER="$POSTGRES_USER" \
  -e POSTGRES_PASSWORD="$POSTGRES_PASSWORD" \
  -v nina_erp_db_data:/var/lib/postgresql/data \
  postgres:16

# Aguardar PostgreSQL ficar pronto
echo "⏳ Aguardando PostgreSQL inicializar..."
sleep 5
for i in {1..30}; do
  if docker exec nina_erp-db pg_isready -U "$POSTGRES_USER" > /dev/null 2>&1; then
    echo "✅ PostgreSQL pronto!"
    break
  fi
  echo "Tentativa $i de 30..."
  sleep 2
done

# 2. Iniciar API em host network
echo "🚀 Iniciando API..."
docker run -d \
  --name nina_erp-api \
  --network host \
  -e ASPNETCORE_URLS="http://+:$API_PORT" \
  -e "ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=$POSTGRES_DB;Username=$POSTGRES_USER;Password=$POSTGRES_PASSWORD" \
  nina_erp-api:latest

echo ""
echo "✅ Containers iniciados com sucesso!"
echo ""
echo "📊 Status:"
docker ps -a --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" | grep nina_erp
echo ""
echo "🌐 Endpoints disponíveis:"
echo "  API:              http://localhost:$API_PORT"
echo "  Swagger UI:       http://localhost:$API_PORT/swagger/ui"
echo "  PostgreSQL:       localhost:5432"
echo ""
echo "📝 Logs:"
echo "  API:     docker logs -f nina_erp-api"
echo "  Database: docker logs -f nina_erp-db"
echo ""
echo "⛔ Para parar:"
echo "  docker stop nina_erp-api nina_erp-db"
echo "  docker rm nina_erp-api nina_erp-db"
