[CmdletBinding()]
param(
    [string]$Version = "10.0.401",
    [ValidateSet("x64","arm64")][string]$Architecture = "x64",
    [string]$InstallDir = "$env:ProgramData\NinaERP\dotnet-sdk",
    [string]$OfflineArchive,
    [string]$OfflineArchiveSha512,
    [switch]$PersistMachinePath
)
$ErrorActionPreference = "Stop"

function Test-NinaDotNet([string]$ExpectedVersion = $Version) {
    $cmd = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $cmd) { return $false }
    try {
        $parts = $ExpectedVersion.Split('.')
        if ($parts.Length -lt 3) { return $false }
        $expectedMajor = [int]$parts[0]
        $expectedMinor = [int]$parts[1]
        $expectedBand = [int]([math]::Floor(([int]$parts[2]) / 100) * 100)
        foreach ($line in (& dotnet --list-sdks 2>$null)) {
            if ($line -match '^(\d+)\.(\d+)\.(\d+)') {
                $major = [int]$Matches[1]; $minor = [int]$Matches[2]; $patch = [int]$Matches[3]
                $band = [int]([math]::Floor($patch / 100) * 100)
                if ($major -eq $expectedMajor -and $minor -eq $expectedMinor -and $band -eq $expectedBand -and $patch -ge [int]$parts[2]) { return $true }
            }
        }
        return $false
    } catch { return $false }
}

function Activate-InstallDir {
    $env:DOTNET_ROOT = $InstallDir
    if (($env:Path -split ';') -notcontains $InstallDir) { $env:Path = "$InstallDir;$env:Path" }
    if ($PersistMachinePath) {
        [Environment]::SetEnvironmentVariable("DOTNET_ROOT", $InstallDir, "Machine")
        $machinePath=[Environment]::GetEnvironmentVariable("Path","Machine")
        if (($machinePath -split ';') -notcontains $InstallDir) {
            [Environment]::SetEnvironmentVariable("Path", "$InstallDir;$machinePath", "Machine")
        }
    }
}

Write-Host "=== Nina ERP • Bootstrap .NET SDK $Version ===" -ForegroundColor Cyan
if (Test-NinaDotNet $Version) {
    Write-Host "SDK .NET 10 já disponível: $(dotnet --version)" -ForegroundColor Green
    exit 0
}

New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null

if ($OfflineArchive) {
    if (-not (Test-Path $OfflineArchive)) { throw "Arquivo offline não encontrado: $OfflineArchive" }
    if ($OfflineArchiveSha512) {
        $actualHash = (Get-FileHash -Path $OfflineArchive -Algorithm SHA512).Hash
        if (-not [string]::Equals($actualHash, $OfflineArchiveSha512.Trim(), [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "SHA-512 do SDK offline não confere. Esperado=$OfflineArchiveSha512; Atual=$actualHash"
        }
        Write-Host "SHA-512 do SDK offline validado." -ForegroundColor Green
    } else {
        Write-Warning "SDK offline sem SHA-512 informado. Para uma build de release, informe -OfflineArchiveSha512."
    }
    Write-Host "Instalando SDK pelo arquivo offline: $OfflineArchive"
    Remove-Item "$InstallDir\*" -Recurse -Force -ErrorAction SilentlyContinue
    Expand-Archive -Path $OfflineArchive -DestinationPath $InstallDir -Force
    Activate-InstallDir
    if (-not (Test-NinaDotNet $Version)) { throw "SDK offline foi extraído, mas dotnet 10 não ficou utilizável." }
    Write-Host "SDK .NET pronto: $(dotnet --version)" -ForegroundColor Green
    exit 0
}

# Primeiro tenta WinGet, que é o caminho preferencial em Windows 11/Server moderno.
$winget = Get-Command winget -ErrorAction SilentlyContinue
if ($winget) {
    Write-Host "Tentando Microsoft.DotNet.SDK.10 via WinGet..."
    & winget install -e --id Microsoft.DotNet.SDK.10 --accept-package-agreements --accept-source-agreements --silent
    if ($LASTEXITCODE -eq 0) {
        $dotnetDefault = Join-Path $env:ProgramFiles "dotnet"
        if (Test-Path $dotnetDefault) {
            $env:DOTNET_ROOT = $dotnetDefault
            if (($env:Path -split ';') -notcontains $dotnetDefault) { $env:Path = "$dotnetDefault;$env:Path" }
        }
        if (Test-NinaDotNet $Version) {
            Write-Host "SDK .NET pronto via WinGet: $(dotnet --version)" -ForegroundColor Green
            exit 0
        }
    }
    Write-Warning "WinGet não concluiu o bootstrap. Tentando instalador oficial portátil."
}

# Fallback oficial: dotnet-install.ps1 instala lado a lado e não exige MSI.
$installer = Join-Path $env:TEMP "ninaerp-dotnet-install.ps1"
try {
    Invoke-WebRequest -UseBasicParsing -Uri "https://dot.net/v1/dotnet-install.ps1" -OutFile $installer
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $installer `
        -Version $Version -Architecture $Architecture -InstallDir $InstallDir -NoPath
    if ($LASTEXITCODE -ne 0) { throw "dotnet-install.ps1 retornou código $LASTEXITCODE" }
    Activate-InstallDir
    if (-not (Test-NinaDotNet $Version)) { throw "dotnet-install concluiu, mas o SDK 10 não foi localizado." }
    Write-Host "SDK .NET pronto em $InstallDir: $(dotnet --version)" -ForegroundColor Green
} catch {
    throw @"
Não foi possível obter o SDK online. Use o pacote oficial Windows $Architecture do .NET SDK $Version
como arquivo ZIP e execute novamente:
  .\scripts\windows\Install-DotNetSdk.ps1 -OfflineArchive C:\caminho\dotnet-sdk-$Version-win-$Architecture.zip -OfflineArchiveSha512 <SHA512_OFICIAL> -PersistMachinePath
Erro original: $($_.Exception.Message)
"@
} finally {
    Remove-Item $installer -Force -ErrorAction SilentlyContinue
}
