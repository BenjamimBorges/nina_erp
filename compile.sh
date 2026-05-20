#!/bin/bash
set -euo pipefail

cd "$(dirname "$0")" || exit 1

echo "🏗️ Compilando solução ERP Nina..."
echo ""

# Restaurar dependências
echo "📦 Restaurando dependências (dotnet restore)..."
dotnet restore

echo ""
echo "🔨 Compilando projetos (dotnet build)..."
dotnet build

echo ""
echo "✅ Compilação concluída!"
echo ""
echo "📝 Próximos passos:"
echo ""
echo "1. Iniciar PostgreSQL:"
echo "   docker run -d --name nina_erp_db -e POSTGRES_DB=nina_erp -e POSTGRES_USER=erpuser -e POSTGRES_PASSWORD=erp_password -p 5432:5432 postgres:16"
echo ""
echo "2. Aplicar migrations:"
echo "   psql -U erpuser -d nina_erp -h localhost < ERP.Database/migrations/001_initial_schema.sql"
echo ""
echo "3. Executar API:"
echo "   cd ERP.API && dotnet run"
echo ""
echo "4. Em outro terminal, testar Swagger:"
echo "   xdg-open http://localhost:5000/swagger/ui"
echo ""
echo "5. Compilar Desktop (Windows/WPF):"
echo "   cd ERP.Desktop && dotnet run"
echo ""
