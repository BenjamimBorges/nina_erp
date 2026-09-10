# Relatório de validação desta revisão

Data da revisão: 2026-09-05/06 (America/Fortaleza).

## Executado com sucesso neste ambiente

### Frontend

`npm run build` foi executado após as alterações de estoque/PDV.

Resultado:

- TypeScript: OK.
- Vite production build: OK.
- 1652 módulos transformados.
- bundle JS final aproximado: 318 KB (97 KB gzip).
- CSS aproximado: 22.7 KB (4.7 KB gzip).

Os artefatos `frontend/dist` foram removidos novamente do pacote fonte porque devem ser gerados pelo pipeline.

### Higiene do repositório

O ZIP original continha `frontend/node_modules`, `frontend/dist` e diretórios `bin/obj` versionados. O `.gitignore` fornecido pelo usuário já expressava a intenção correta de ignorá-los; esta revisão remove esses artefatos da árvore entregue e amplia as exclusões para publish, secrets locais, backups e WebView2 user data.

### Revisão estática

Foram revisados os fluxos de:

- autenticação/JWT;
- CompanyId/multiempresa;
- departamento;
- produto/estoque;
- clientes/fornecedores;
- auditoria;
- venda/pagamento/cancelamento;
- Desktop;
- Server Manager;
- scripts Windows;
- instalador.

## Não foi possível executar neste ambiente

O container desta sessão **não possui o SDK .NET** e a rede do container não permitiu baixar o SDK. Portanto não houve como executar aqui:

```text
dotnet restore
dotnet build
dotnet test
dotnet publish
dotnet ef migrations add
dotnet ef migrations bundle
```

Essas etapas não são declaradas como “testadas” neste relatório. Elas estão automatizadas em `Build-FirstWindows.ps1` justamente para serem executadas na máquina Windows/VM que é o alvo da primeira build.

## Também depende de ambiente externo

Não foi possível afirmar validação real de:

- instalação Inno Setup em Windows limpo;
- criação/inicialização real do Windows Service;
- WebView2 runtime real;
- PostgreSQL Windows local;
- firewall Windows;
- LAN entre duas máquinas;
- backup/restore contra PostgreSQL real;
- SEFAZ/fiscal;
- TEF/adquirentes/PIX externos.

## Próxima execução obrigatória

Na VM Windows, rode:

```powershell
.\scripts\windows\Build-FirstWindows.ps1 -Runtime win-x64 -BuildInstaller
```

Não use `-SkipTests` na build candidata à primeira versão.
