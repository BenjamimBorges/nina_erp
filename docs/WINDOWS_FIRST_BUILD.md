# Primeira build Windows — roteiro de validação

## Objetivo

Provar que o Nina ERP pode sair de uma máquina de desenvolvimento e ser instalado do zero em um Windows limpo, com banco, API, desktop, rede e recuperação funcionando.

## VM recomendada

- Windows 11 Pro x64 atualizado.
- 4 vCPU.
- 8 GB RAM.
- 40 GB+ disco.
- adaptador de rede em modo que permita comunicação com uma segunda VM/máquina para o teste LAN.

## Preparar máquina de build

PowerShell como Administrador:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\scripts\windows\Install-Prerequisites.ps1 -DeveloperMachine -InstallPostgreSql
```

Também instale **Inno Setup 6** quando quiser gerar o `.exe` final.

## Build

```powershell
.\scripts\windows\Build-FirstWindows.ps1 -Runtime win-x64 -BuildInstaller
```

Se a pasta `src\NinaERP.Infrastructure\Persistence\Migrations` estava vazia, confirme que a migration foi criada. Revise-a e **commite os arquivos de migration**.

Rode novamente a build após o commit para provar reprodutibilidade.

## Instalação limpa

Copie apenas o setup para a VM limpa e execute como Administrador.

### PostgreSQL

O Nina ERP aceita banco local ou remoto. Para a primeira build, PostgreSQL local facilita o teste. Instale PostgreSQL 16+ e guarde a senha do usuário administrativo/ERP.

### Server Manager

Abra o Server Manager e preencha:

- Host: `localhost` no servidor local.
- Porta: `5432` por padrão.
- Banco: `nina_erp_retail`.
- Usuário e senha: credencial PostgreSQL dedicada.
- API: `7000` por padrão.

Use **Testar conexão** e depois **Inicializar servidor**.

A inicialização deve:

1. verificar/criar o database;
2. gravar `%ProgramData%\NinaERP\appsettings.Production.json`;
3. aplicar `efbundle.exe`;
4. solicitar empresa e administrador se `Users` estiver vazio;
5. instalar `NinaERP.Server`;
6. iniciar/reiniciar o serviço.

## Firewall

```powershell
.\scripts\windows\Configure-Firewall.ps1 -Port 7000
```

A regra deve ficar limitada aos perfis Private/Domain. Não exponha a porta do PostgreSQL para a rede se somente a API necessita acessá-lo localmente.

## Smoke test local

```powershell
.\scripts\windows\Smoke-Test.ps1 -Server 127.0.0.1 -Port 7000 -Username admin -Password 'SUA-SENHA'
```

Esse teste verifica:

- TCP;
- `/health` e conexão DB;
- login;
- JWT;
- `/api/auth/me`;
- produtos;
- dashboard.

## Teste cliente ↔ servidor

No servidor, descubra o IPv4:

```powershell
ipconfig
```

No cliente:

```powershell
.\scripts\windows\Test-Network.ps1 -Server 192.168.1.10 -Port 7000
```

Depois configure no NinaERP Desktop:

```text
http://192.168.1.10:7000
```

Confirme:

- login;
- listagem de produtos;
- ajuste de estoque;
- PDV;
- criação/finalização de venda;
- atualização da tela em outra estação.

## Concorrência

Abra duas estações e tente movimentar/vender o mesmo saldo simultaneamente. Um conflito de concorrência deve retornar 409 em vez de sobrescrever saldo silenciosamente.

## Backup

Faça um backup após cadastrar dados:

```powershell
.\scripts\windows\Backup-Database.ps1
```

Ou use o Server Manager.

## Restore destrutivo controlado

1. anote contagens/totais de produtos e vendas;
2. crie backup;
3. use uma base descartável ou VM snapshot;
4. altere/exclua dados de teste;
5. restaure o backup;
6. execute smoke test;
7. compare contagens/totais.

Não marque o item 10 como validado somente porque `pg_dump` gerou um arquivo.

## Critério final de aprovação

A build é aprovada apenas se passar: instalar → inicializar → reiniciar Windows → usar cliente LAN → vender → movimentar estoque → backup → restore → usar novamente.
