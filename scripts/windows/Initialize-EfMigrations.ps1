[CmdletBinding()]
param()
$ErrorActionPreference = "Stop"
$Root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $Root

Write-Host "[EF] Validando migrations versionadas..." -ForegroundColor Cyan
dotnet tool restore
if ($LASTEXITCODE -ne 0) { throw "Falha ao restaurar ferramentas .NET." }

$MigrationDir = Join-Path $Root "src\NinaERP.Infrastructure\Persistence\Migrations"
if (-not (Test-Path $MigrationDir)) {
    throw "Gate bloqueado: diretório de migrations EF não existe. Migrations devem ser versionadas no repositório; o build não cria migrations automaticamente."
}

$migrations = Get-ChildItem $MigrationDir -Filter "*.cs" -File | Where-Object { $_.Name -ne "AppDbContextModelSnapshot.cs" }
if (-not $migrations) {
    throw "Gate bloqueado: nenhuma migration EF versionada encontrada."
}

$snapshot = Join-Path $MigrationDir "AppDbContextModelSnapshot.cs"
if (-not (Test-Path $snapshot)) {
    throw "Gate bloqueado: AppDbContextModelSnapshot.cs ausente."
}


$manifest = Join-Path $MigrationDir "schema-source.sha256"
if (-not (Test-Path $manifest)) {
    throw "Gate bloqueado: manifesto schema-source.sha256 ausente."
}
foreach ($line in Get-Content $manifest) {
    if ([string]::IsNullOrWhiteSpace($line)) { continue }
    $parts = $line -split '\s{2,}', 2
    if ($parts.Count -ne 2) { throw "Manifesto de schema inválido: $line" }
    $expected = $parts[0].Trim().ToUpperInvariant()
    $relative = $parts[1].Trim().Replace('/', '\\')
    $file = Join-Path $Root $relative
    if (-not (Test-Path $file)) { throw "Arquivo do schema não encontrado: $relative" }
    $actual = (Get-FileHash $file -Algorithm SHA256).Hash.ToUpperInvariant()
    if ($actual -ne $expected) {
        throw "Gate bloqueado: $relative mudou sem atualização deliberada de migration/snapshot."
    }
}
Write-Host "[EF] Manifesto SHA-256 do modelo validado." -ForegroundColor Green

Write-Host "[EF] $($migrations.Count) arquivo(s) de migration e snapshot encontrados." -ForegroundColor Green
