#!/usr/bin/env bash
set -euo pipefail

# Helper para gerar e aplicar migrations EF Core usando o projeto ERP.API.
# Requer dotnet SDK e dotnet-ef.

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet não encontrado. Instale o .NET SDK antes de usar este script."
  exit 1
fi

if ! dotnet tool list -g | grep -q "dotnet-ef"; then
  echo "dotnet-ef não está instalado globalmente. Instale com: dotnet tool install --global dotnet-ef"
  exit 1
fi

cd "$(dirname "$0")/.." || exit 1
cd ERP.API || exit 1

if [ "$#" -lt 1 ]; then
  echo "Uso: $0 <comando> [args...]"
  echo "Comandos suportados: add, update, script" 
  echo "Ex: $0 add InitialCreate" 
  echo "    $0 update" 
  echo "    $0 script ../ERP.Database/migrations/001_initial_schema.sql" 
  exit 1
fi

cmd="$1"
shift

case "$cmd" in
  add)
    migrationName="$1"
    dotnet ef migrations add "$migrationName" --context ApplicationDbContext --output-dir "../ERP.Infrastructure/Migrations"
    ;;
  update)
    dotnet ef database update --context ApplicationDbContext
    ;;
  script)
    target="${1:-../ERP.Database/migrations/001_initial_schema.sql}"
    dotnet ef migrations script --idempotent -o "$target" --context ApplicationDbContext
    ;;
  *)
    echo "Comando desconhecido: $cmd"
    exit 1
    ;;
esac
