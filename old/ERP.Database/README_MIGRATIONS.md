# Aplicar migrações SQL (PostgreSQL)

Este arquivo descreve como criar o banco PostgreSQL local e aplicar o script SQL inicial contido em `migrations/001_initial_schema.sql`.

Pré-requisitos

- PostgreSQL instalado no servidor/PC (psql disponível)
- Acesso a um usuário com permissão para criar banco e usuários, ou coordene com o DBA
- Arquivo `ERP.Database/migrations/001_initial_schema.sql` presente no repositório

1. Criar banco e usuário (opcional, como superuser/postgres)

```sh
# Conectar como postgres (ou outro superuser)
psql -U postgres

-- dentro do psql:
CREATE USER erpuser WITH PASSWORD 'erp_password';
CREATE DATABASE nina_erp OWNER erpuser;
GRANT ALL PRIVILEGES ON DATABASE nina_erp TO erpuser;
\q
```

2. Exportar variáveis de ambiente e executar o script

```sh
export DB_HOST=localhost
export DB_PORT=5432
export DB_NAME=nina_erp
export DB_USER=erpuser
export DB_PASS='erp_password'

# Tornar o script executável (uma vez)
chmod +x ERP.Database/apply_migrations.sh

# Executar
ERP.Database/apply_migrations.sh
```

3. Verificar tabelas

```sh
psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c "\dt"
```

4. Alternativa: aplicar o SQL manualmente

```sh
psql -h localhost -p 5432 -U erpuser -d nina_erp -f ERP.Database/migrations/001_initial_schema.sql
```

## EF Core Migrations (opcional)

Estas instruções são úteis se você quiser usar as migrations do Entity Framework Core em vez de aplicar o SQL direto.

Pré-requisitos:

- .NET SDK 8.0 instalado
- `dotnet-ef` instalado globalmente ou como ferramenta local
- O projeto `ERP.API` deve poder compilar e referenciar `ERP.Infrastructure`

### Instalando `dotnet-ef`

```sh
dotnet tool install --global dotnet-ef
```

### Gerar a migration inicial

```sh
cd ERP.API

dotnet ef migrations add InitialCreate --context ApplicationDbContext --output-dir "../ERP.Infrastructure/Migrations"
```

### Aplicar migration no banco

```sh
dotnet ef database update --context ApplicationDbContext
```

### Gerar script SQL idempotente a partir da migration

```sh
cd ERP.API

dotnet ef migrations script --idempotent -o "../ERP.Database/migrations/001_initial_schema.sql" --context ApplicationDbContext
```

### Script helper

Também existe um helper bash em `ERP.Database/ef_migrations.sh`:

```sh
chmod +x ERP.Database/ef_migrations.sh
ERP.Database/ef_migrations.sh add InitialCreate
ERP.Database/ef_migrations.sh update
ERP.Database/ef_migrations.sh script ../ERP.Database/migrations/001_initial_schema.sql
```

## Executar com Docker Compose

Se preferir um ambiente local com API + PostgreSQL em containers, use o `docker-compose.yml` no root do projeto.

```sh
docker compose up --build
```

A API ficará exposta em `http://localhost:7000` e o banco em `localhost:5432`.

O arquivo `.env.example` mostra as variáveis de ambiente que podem ser usadas para configurar as credenciais do PostgreSQL.
