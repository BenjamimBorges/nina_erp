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

### Troubleshooting Docker

**Erro: `failed to add the host <=> sandbox pair interfaces: operation not supported`**

Este erro ocorre em ambientes Docker com restrições de rede (WSL2, Docker-in-Docker, ambientes virtualizados, etc.).

**Soluções:**

1. **Build direto com host network (recomendado):**

   ```sh
   docker build --network host -f ERP.API/Dockerfile -t nina_erp-api:latest .
   docker compose up
   ```

2. **Usar script auxiliar:**

   ```sh
   chmod +x build.sh
   bash build.sh dev
   ```

3. **Usar compose alternativo:**

   ```sh
   docker compose -f docker-compose.dev.yml up --build
   ```

4. **Verificar/reiniciar Docker daemon:**
   ```sh
   sudo systemctl restart docker
   # ou em Docker Desktop: restart via GUI
   ```

Se os problemas persistirem, a compilação local (.NET SDK) será necessária como alternativa.
