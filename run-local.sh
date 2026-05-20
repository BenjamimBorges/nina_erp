#!/bin/bash
set -euo pipefail

cd "$(dirname "$0")" || exit 1

echo "🚀 Executando ERP Nina em desenvolvimento local..."
echo ""

# Verificar PostgreSQL
echo "🔍 Verificando PostgreSQL..."
if ! docker ps | grep -q nina_erp_db; then
    echo "⚠️  Contêiner PostgreSQL não está rodando"
    echo "Iniciando: docker run -d --name nina_erp_db ..."
    
    docker run -d \
        --name nina_erp_db \
        -e POSTGRES_DB=nina_erp \
        -e POSTGRES_USER=erpuser \
        -e POSTGRES_PASSWORD=erp_password \
        -p 5432:5432 \
        postgres:16
    
    echo "Aguardando PostgreSQL inicializar..."
    sleep 5
fi

echo "✅ PostgreSQL está rodando"
echo ""

# Restaurar e compilar se necessário
if [ ! -f "bin/Debug/net8.0/ERP.API.dll" ]; then
    echo "📦 Restaurando e compilando..."
    dotnet restore
    dotnet build
    echo ""
fi

echo "🔌 Iniciando API em https://localhost:5001"
echo "📊 Swagger disponível em: http://localhost:5000/swagger/ui"
echo "⛔ Para parar: Ctrl+C"
echo ""

cd ERP.API
ASPNETCORE_ENVIRONMENT=Development dotnet run

# Cleanup
echo ""
echo "🧹 Limpando containers..."
docker stop nina_erp_db 2>/dev/null || true
docker rm nina_erp_db 2>/dev/null || true
echo "✅ Pronto para próxima execução!"
