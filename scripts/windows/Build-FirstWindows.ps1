[CmdletBinding()]
param(
    [ValidateSet("win-x64", "win-arm64")]
    [string]$Runtime = "win-x64",
    [switch]$SkipTests,
    [switch]$BuildInstaller,
    [switch]$BootstrapDotNet
)
$ErrorActionPreference = "Stop"
$Root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$Artifacts = Join-Path $Root "artifacts\windows"
Set-Location $Root

function Require-Command([string]$Name) {
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Dependência de build ausente: $Name"
    }
}

Write-Host "=== Nina ERP • Primeira Build Windows ===" -ForegroundColor Cyan
if ($BootstrapDotNet) {
    # O bootstrap também corrige uma instalação existente em feature band incompatível.
    & (Join-Path $PSScriptRoot "Install-DotNetSdk.ps1")
}
Require-Command dotnet
Require-Command node
Require-Command npm

$dotnetVersion = dotnet --version
Write-Host "SDK .NET: $dotnetVersion"
$requiredSdk = (Get-Content (Join-Path $Root "global.json") -Raw | ConvertFrom-Json).sdk.version
$requiredParts = $requiredSdk.Split('.')
$actualParts = $dotnetVersion.Split('.')
if ($requiredParts.Length -lt 3 -or $actualParts.Length -lt 3) { throw "Versão de SDK inválida. Requerido: $requiredSdk; detectado: $dotnetVersion" }
$requiredBand = [int]([math]::Floor(([int]$requiredParts[2]) / 100) * 100)
$actualBand = [int]([math]::Floor(([int]$actualParts[2]) / 100) * 100)
if ([int]$actualParts[0] -ne [int]$requiredParts[0] -or [int]$actualParts[1] -ne [int]$requiredParts[1] -or $actualBand -ne $requiredBand) {
    throw "SDK incompatível com global.json. Requer feature band $($requiredParts[0]).$($requiredParts[1]).${requiredBand}xx; detectado: $dotnetVersion"
}
Write-Host "Node: $(node --version)"

Remove-Item $Artifacts -Recurse -Force -ErrorAction SilentlyContinue
New-Item $Artifacts -ItemType Directory -Force | Out-Null

Write-Host "[1/7] Restaurando frontend..." -ForegroundColor Cyan
Push-Location "frontend"
npm ci
npm run build
Pop-Location

Write-Host "[2/7] Incorporando React à API..." -ForegroundColor Cyan
$wwwroot = Join-Path $Root "src\NinaERP.API\wwwroot"
Remove-Item $wwwroot -Recurse -Force -ErrorAction SilentlyContinue
New-Item $wwwroot -ItemType Directory -Force | Out-Null
Copy-Item "frontend\dist\*" $wwwroot -Recurse -Force

Write-Host "[3/7] Restaurando .NET e preparando EF Migrations..." -ForegroundColor Cyan
dotnet restore "NinaERP.sln"
& "scripts\windows\Initialize-EfMigrations.ps1"

if (-not $SkipTests) {
    Write-Host "[4/7] Compilando e executando testes..." -ForegroundColor Cyan
    dotnet build "NinaERP.sln" -c Release --no-restore
    dotnet test "src\NinaERP.Tests\NinaERP.Tests.csproj" -c Release --no-build --filter "Category!=Integration"
} else {
    Write-Host "[4/7] Testes ignorados por parâmetro." -ForegroundColor Yellow
}

Write-Host "[5/7] Gerando EF migration bundle..." -ForegroundColor Cyan
$previousEnvironment = $env:ASPNETCORE_ENVIRONMENT
$env:ASPNETCORE_ENVIRONMENT = "Production"
$serverOut = Join-Path $Artifacts "server"
New-Item $serverOut -ItemType Directory -Force | Out-Null
dotnet tool restore
dotnet ef migrations bundle `
    --project "src\NinaERP.Infrastructure\NinaERP.Infrastructure.csproj" `
    --startup-project "src\NinaERP.API\NinaERP.API.csproj" `
    --self-contained `
    --target-runtime $Runtime `
    --output (Join-Path $serverOut "efbundle.exe") `
    --force
$env:ASPNETCORE_ENVIRONMENT = $previousEnvironment

Write-Host "[6/7] Publicando API, Desktop e Server Manager self-contained..." -ForegroundColor Cyan
dotnet publish "src\NinaERP.API\NinaERP.API.csproj" -c Release -r $Runtime --self-contained true -o $serverOut /p:PublishSingleFile=false

dotnet publish "src\NinaERP.Desktop\NinaERP.Desktop.csproj" -c Release -r $Runtime --self-contained true -o (Join-Path $Artifacts "desktop") /p:PublishSingleFile=false

dotnet publish "src\NinaERP.ServerManager\NinaERP.ServerManager.csproj" -c Release -r $Runtime --self-contained true -o (Join-Path $Artifacts "manager") /p:PublishSingleFile=false

Write-Host "[7/7] Preparando instalador..." -ForegroundColor Cyan
if ($BuildInstaller) {
    $iscc = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if (-not $iscc) {
        throw "Inno Setup não encontrado. Instale o Inno Setup 6 e execute novamente com -BuildInstaller."
    }
    & $iscc.Source "installer\NinaERP.Setup\NinaERP.iss"
    if ($LASTEXITCODE -ne 0) { throw "Compilação do instalador Inno Setup falhou." }
    $installerOut = Join-Path $Artifacts "installer"
    New-Item $installerOut -ItemType Directory -Force | Out-Null
    Copy-Item "installer\NinaERP.Setup\output\*" $installerOut -Force
} else {
    Write-Host "Instalador não compilado. Use -BuildInstaller quando o Inno Setup estiver instalado." -ForegroundColor DarkGray
}

Write-Host "Build concluída em: $Artifacts" -ForegroundColor Green
