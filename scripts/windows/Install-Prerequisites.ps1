[CmdletBinding()]
param(
    [switch]$DeveloperMachine,
    [switch]$InstallPostgreSql
)
$ErrorActionPreference = "Stop"

if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "Execute este script como Administrador."
}

Write-Host "=== Nina ERP • Dependências Windows ===" -ForegroundColor Cyan

function Has-Winget { return [bool](Get-Command winget -ErrorAction SilentlyContinue) }
function Install-WingetPackage([string]$Id) {
    if (-not (Has-Winget)) { throw "winget não encontrado." }
    winget install -e --id $Id --accept-package-agreements --accept-source-agreements --silent
    if ($LASTEXITCODE -ne 0) { throw "Falha ao instalar $Id pelo winget. Código: $LASTEXITCODE" }
}

# Runtime WebView2 é obrigatório para o shell WPF.
try {
    Write-Host "Verificando Microsoft Edge WebView2 Runtime..."
    $webview = Get-ItemProperty "HKLM:\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}" -ErrorAction SilentlyContinue
    if (-not $webview) {
        Install-WingetPackage "Microsoft.EdgeWebView2Runtime"
    } else {
        Write-Host "WebView2 já instalado." -ForegroundColor Green
    }
} catch {
    Write-Warning "Não foi possível instalar WebView2 automaticamente: $($_.Exception.Message)"
    Write-Warning "Use o instalador offline Evergreen em installer\NinaERP.Setup\prereqs."
}

$pgDump = Get-ChildItem "C:\Program Files\PostgreSQL\*\bin\pg_dump.exe" -ErrorAction SilentlyContinue |
    Sort-Object FullName -Descending | Select-Object -First 1

if ($pgDump) {
    Write-Host "PostgreSQL detectado: $($pgDump.FullName)" -ForegroundColor Green
} elseif ($InstallPostgreSql) {
    try {
        Write-Host "Instalando PostgreSQL 16 pelo WinGet..."
        Install-WingetPackage "PostgreSQL.PostgreSQL.16"
        Write-Host "PostgreSQL instalado. Abra o Server Manager para configurar a conexão." -ForegroundColor Green
    } catch {
        Write-Warning "A instalação automática do PostgreSQL falhou: $($_.Exception.Message)"
        Write-Warning "Instale PostgreSQL 16 x64 manualmente ou use o instalador offline EDB."
    }
} else {
    Write-Warning "PostgreSQL não encontrado. No servidor, execute novamente com -InstallPostgreSql ou instale PostgreSQL 16 x64 manualmente."
}

if ($DeveloperMachine) {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        Write-Host "Instalando .NET SDK 10..."
        Install-WingetPackage "Microsoft.DotNet.SDK.10"
    }
    if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
        Write-Host "Instalando Node.js LTS..."
        Install-WingetPackage "OpenJS.NodeJS.LTS"
    }
    Write-Host "Dependências de desenvolvimento verificadas." -ForegroundColor Green
}

Write-Host "Concluído." -ForegroundColor Green
