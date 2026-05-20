#!/bin/bash
set -e

# Script de inicialização do PostgreSQL que cria o banco e usuário corretamente
# Coloque este arquivo em /docker-entrypoint-initdb.d/ no container

echo "Criando usuário e banco de dados..."

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
  -- Já que estamos conectando como postgres (admin), podemos criar o usuário erpuser se não existir
  DO
  \$do\$
  BEGIN
     IF NOT EXISTS (SELECT FROM pg_user WHERE usename = 'erpuser') THEN
        CREATE USER erpuser WITH PASSWORD 'erp_password';
     END IF;
  END
  \$do\$;
  
  -- Garantir que erpuser tenha privilégios no banco nina_erp
  GRANT ALL PRIVILEGES ON DATABASE nina_erp TO erpuser;
EOSQL

echo "Inicialização do banco de dados concluída!"
