# Arquitetura Nina ERP

## Decisão principal

A interface oficial do Nina ERP passa a ser o **React**. O WPF não duplica telas: ele funciona como um shell desktop usando WebView2.

Isso evita manter duas UIs independentes, reduz bugs de paridade e permite que a mesma tela seja usada:

- no computador servidor;
- em estações clientes da LAN;
- no executável Windows;
- futuramente em navegador controlado/PWA, se fizer sentido.

## Fluxo

```text
┌──────────────────────────────────┐
│ NinaERP.Desktop                  │
│ WPF + WebView2                   │
│ URL configurável do servidor     │
└──────────────┬───────────────────┘
               │ HTTP
               ▼
┌──────────────────────────────────┐
│ NinaERP.API                      │
│ ASP.NET Core                     │
│ /api/* + React static files      │
│ Windows Service                  │
└──────────────┬───────────────────┘
               │
               ▼
┌──────────────────────────────────┐
│ Application                      │
│ MediatR / CQRS / Validation      │
└──────────────┬───────────────────┘
               │
               ▼
┌──────────────────────────────────┐
│ Infrastructure                   │
│ EF Core / Npgsql / Repositories  │
└──────────────┬───────────────────┘
               │
               ▼
┌──────────────────────────────────┐
│ PostgreSQL                       │
└──────────────────────────────────┘
```

## Camadas

### Domain

Não conhece API, EF, WPF ou React. Contém entidades e enums centrais.

### Contracts

DTOs de entrada/saída. O cliente nunca deve definir `CompanyId` ou `UserId` em operações autenticadas; esses valores vêm do JWT e são montados no servidor.

### Application

Casos de uso com MediatR. Regras que precisam de repositórios ficam nos handlers. FluentValidation valida comandos antes da execução.

### Infrastructure

Implementa persistência e segurança técnica: `AppDbContext`, Npgsql, repositórios, JWT.

### API

Responsável por autenticação/autorização, CORS, rate limiting, auditoria HTTP, tratamento uniforme de erros, health check e hospedagem do frontend compilado.

### Desktop

Responsável somente pela experiência desktop: janela, URL do servidor, WebView2, estado offline e recarga. Não contém regra de negócio.

### Server Manager

É a ferramenta administrativa do servidor Windows. Centraliza tarefas que não devem ficar no cliente comum:

- conexão PostgreSQL;
- criação/verificação do banco;
- `efbundle`;
- bootstrap do primeiro admin;
- configuração da API;
- instalação/restart do Windows Service;
- backup e restore.

## Multiempresa

O isolamento é feito por `CompanyId`.

Regras adotadas:

1. o `CompanyId` confiável vem do JWT;
2. controllers sobrescrevem/constroem comandos com esse ID;
3. handlers verificam que a entidade recuperada pertence à empresa;
4. queries filtram por empresa;
5. IDs de outra empresa devem resultar em `404`, evitando inclusive revelar existência do registro.

Isso deve ser repetido em todos os módulos futuros.

## Estoque

Quantidade usa `decimal(18,4)`, não inteiro. É necessário para KG, MT, LT e outras unidades fracionáveis.

A baixa de venda acontece no **fechamento**, não na abertura. Todas as alterações de produto e movimentos são persistidos no mesmo `SaveChanges`, portanto ficam na mesma transação do DbContext.

`Product.Version` é token de concorrência otimista. Duas estações tentando alterar o mesmo saldo ao mesmo tempo não devem silenciosamente sobrescrever uma à outra; um conflito retorna HTTP 409.

## Venda

Estados atuais:

```text
Open -> Completed -> Cancelled
  └───────────────> Cancelled
```

- `Open`: itens registrados, estoque ainda não baixado.
- `Completed`: pagamentos validados e estoque baixado.
- `Cancelled`: venda invalidada; se já estava concluída, estoque é recomposto.

O cancelamento atual é operacional. Reversão real de cartão/PIX/TEF dependerá dos provedores de pagamento e deve ser implementada no estágio comercial.

## Windows

A API é publicada **self-contained**, então o computador de produção não depende da instalação global do runtime .NET para executar o servidor.

O serviço é instalado como `NinaERP.Server`, inicialização automática e política de restart em falha.

## Banco escalável

PostgreSQL permanece a escolha principal. A evolução recomendada é vertical primeiro, depois réplicas/leitura/cache/eventos somente quando métricas reais justificarem. Não introduzir microserviços no primeiro release comercial.

Para escala futura, preservar desde já:

- IDs UUID;
- índices por `CompanyId` + campos de consulta;
- consultas paginadas;
- operações idempotentes onde integrações externas existirem;
- outbox para eventos externos no estágio distribuído;
- nenhuma regra de negócio dependente do WPF.
