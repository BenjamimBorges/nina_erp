# Nina ERP — Professional Edition

## Duas arquiteturas, um projeto

Este repositório contém **dois projetos paralelos**:

### 1. `ERP.*` — Arquitetura atual (operacional)
Pronta para rodar com `docker compose up --build`.

| Módulo | Status |
|---|---|
| Autenticação JWT | ✅ Implementado |
| BCrypt hash de senha | ✅ Implementado |
| CRUD Clients / Products / Stock | ✅ Implementado |
| CRUD Companies / Users | ✅ Implementado |
| [Authorize] em todos os endpoints | ✅ Implementado |
| Exception Handler global | ✅ Implementado |
| DbSeeder (admin inicial) | ✅ Implementado |
| Desktop WPF com Dashboard | ✅ Implementado |

**Login padrão:** `admin` / `Admin@123`

---

### 2. `src/NinaERP.*` — Clean Architecture (enterprise)
Nova arquitetura preparada para escalar. Ainda em migração.

```
src/
├── NinaERP.Domain          → Entidades, enums (sem dependências)
├── NinaERP.Application     → CQRS, MediatR, FluentValidation
├── NinaERP.Contracts       → ApiResponse<T>, AuthResponse
├── NinaERP.Infrastructure  → JWT, Serilog, DI
├── NinaERP.API             → Controllers com MediatR
└── NinaERP.Tests           → xUnit + FluentAssertions + NSubstitute
```

**Pacotes adicionados:** MediatR 12, FluentValidation 11, Serilog, xUnit, NSubstitute, FluentAssertions

---

## Como rodar (arquitetura atual)

```bash
docker compose up --build
# API → http://localhost:7000/swagger
```

## Como rodar testes (nova arquitetura)

```bash
cd src/NinaERP.Tests
dotnet test
```

## Próximos passos

- [ ] Migrar repositórios EF Core para `src/NinaERP.Infrastructure`
- [ ] Criar módulo Financeiro (CQRS)
- [ ] Criar módulo Vendas com handler `CreateSaleCommandHandler`
- [ ] Dashboard Web React/Next.js
- [ ] CI/CD GitHub Actions
- [ ] Redis para cache de sessão
- [ ] Hangfire para jobs em background
