# nina_erp

Gestor para empresas

## Docker

O repositório já possui suporte inicial para execução do `ERP.API` em container juntamente com PostgreSQL.

Arquivos adicionados:

- `ERP.API/Dockerfile`
- `docker-compose.yml`
- `.dockerignore`
- `.env.example`

### Executar com Docker Compose

```sh
docker compose up --build
```

A API ficará disponível em `http://localhost:7000` e o PostgreSQL em `localhost:5432`.

### Observações

- O container foi criado para a API backend (`ERP.API`).
- O desktop WPF não é preparado para execução em container no Linux; ele deve ser compilado e executado localmente no Windows.
- O banco de dados pode ser populado usando `ERP.Database/migrations/001_initial_schema.sql` ou via EF Core migrations se preferir.

### Variáveis de ambiente

Você pode copiar `.env.example` para `.env` e adaptar credenciais antes de subir o compose.
