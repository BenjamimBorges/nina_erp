#!/usr/bin/env bash
set -euo pipefail

# Script para aplicar a migração SQL inicial no PostgreSQL usando psql.
# Variáveis de ambiente esperadas:
# DB_HOST (default: localhost)
# DB_PORT (default: 5432)
# DB_NAME (default: nina_erp)
# DB_USER (default: erpuser)
# DB_PASS (senha do usuário)

DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${DB_NAME:-nina_erp}"
DB_USER="${DB_USER:-erpuser}"
SQL_FILE="$(dirname "$0")/migrations/001_initial_schema.sql"

if [ ! -f "$SQL_FILE" ]; then
  echo "Arquivo SQL não encontrado: $SQL_FILE"
  exit 1
fi

if [ -z "${DB_PASS:-}" ]; then
  echo "Por segurança, exporte DB_PASS com a senha do usuário PostgreSQL e execute o script." 
  echo "Ex: export DB_PASS='minha_senha'"
  exit 1
fi

PGPASSWORD="$DB_PASS" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -v ON_ERROR_STOP=1 -f "$SQL_FILE"

echo "Migração aplicada com sucesso."