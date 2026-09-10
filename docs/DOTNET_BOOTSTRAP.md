# Nina ERP — bootstrap reproduzível do .NET 10

O repositório fixa o SDK em **10.0.401** por `global.json`, com `rollForward=latestPatch`. Os scripts aceitam somente SDK compatível com o mesmo **feature band 10.0.4xx** e versão igual ou superior à solicitada dentro desse band. Isso evita o falso positivo “há algum .NET 10 instalado”, que ainda pode ser recusado pelo resolver do `global.json`.

A aplicação publicada é self-contained; o SDK é necessário somente para restore, build, testes, migrations e geração do bundle EF.

## Estratégia de resolução

Há quatro caminhos, em ordem prática:

1. **CI autoritativo:** `.github/workflows/ci.yml` usa `actions/setup-dotnet@v4` e instala o SDK 10.0.401 no runner, depois executa build, testes, PostgreSQL e publish Windows.
2. **Windows online:** `Install-DotNetSdk.ps1` tenta um SDK compatível já instalado, depois WinGet e, por fim, `dotnet-install.ps1` oficial.
3. **Windows/Linux offline:** leve o arquivo binário oficial do SDK em mídia local e forneça-o ao bootstrap. Esta é a alternativa para redes restritas.
4. **Linux online:** `ensure-dotnet.sh` usa `dotnet-install.sh` oficial quando o SDK compatível não está disponível.

## Windows — online

Em PowerShell elevado:

```powershell
.\scripts\windows\Install-DotNetSdk.ps1 -PersistMachinePath
```

Depois:

```powershell
dotnet --info
.\scripts\windows\Build-FirstWindows.ps1 -Runtime win-x64 -BuildInstaller
```

A própria build pode tentar o bootstrap:

```powershell
.\scripts\windows\Build-FirstWindows.ps1 -BootstrapDotNet -Runtime win-x64 -BuildInstaller
```

## Windows — offline/rede restrita

Em uma máquina com internet, obtenha o ZIP oficial **.NET SDK 10.0.401 Windows x64** e o SHA-512 publicado pela Microsoft. Copie ambos para a VM e execute:

```powershell
.\scripts\windows\Install-DotNetSdk.ps1 `
  -OfflineArchive C:\Temp\dotnet-sdk-10.0.401-win-x64.zip `
  -OfflineArchiveSha512 '<SHA512_OFICIAL>' `
  -PersistMachinePath
```

O bootstrap valida o SHA-512 antes de extrair o SDK quando o hash é fornecido. Para build candidata/release, o hash deve ser informado.

## Linux — online

```bash
./scripts/dev/ensure-dotnet.sh
export DOTNET_ROOT="$HOME/.ninaerp/dotnet"
export PATH="$DOTNET_ROOT:$PATH"
dotnet --info
```

## Linux — offline

```bash
NINAERP_DOTNET_ARCHIVE=/mnt/offline/dotnet-sdk-10.0.401-linux-x64.tar.gz \
NINAERP_DOTNET_ARCHIVE_SHA512='<SHA512_OFICIAL>' \
  ./scripts/dev/ensure-dotnet.sh
```

## Gate autoritativo

Quando a máquina local não possui rede/SDK, **não se considera o backend compilado por inferência**. O workflow do GitHub é o executor de referência: .NET 10.0.401 + PostgreSQL 17 + migrations + testes unitários/integração + smoke da API + frontend + publish Windows. Um release só pode mudar `productionApproved` para `true` depois desses gates e da homologação fiscal/operacional.
