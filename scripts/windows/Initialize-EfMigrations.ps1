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
$manifestPaths = New-Object System.Collections.Generic.HashSet[string]([System.StringComparer]::OrdinalIgnoreCase)
foreach ($line in Get-Content $manifest) {
    if ([string]::IsNullOrWhiteSpace($line)) { continue }
    $parts = $line -split '\s{2,}', 2
    if ($parts.Count -ne 2) { throw "Manifesto de schema inválido: $line" }
    $expected = $parts[0].Trim().ToUpperInvariant()
    $relativeRaw = $parts[1].Trim().Replace('\', '/')
    [void]$manifestPaths.Add($relativeRaw)
    $relative = $relativeRaw.Replace('/', '\')
    $file = Join-Path $Root $relative
    if (-not (Test-Path $file)) { throw "Arquivo do schema não encontrado: $relative" }
    $actual = (Get-FileHash $file -Algorithm SHA256).Hash.ToUpperInvariant()
    if ($actual -ne $expected) {
        throw "Gate bloqueado: $relative mudou sem atualização deliberada de migration/snapshot."
    }
}

$expectedSources = @(
    Get-ChildItem (Join-Path $Root 'src\NinaERP.Domain\Entities') -Filter '*.cs' -File |
        ForEach-Object { ('src/NinaERP.Domain/Entities/' + $_.Name) }
) + @('src/NinaERP.Infrastructure/Persistence/AppDbContext.cs')
foreach ($expectedSource in $expectedSources) {
    if (-not $manifestPaths.Contains($expectedSource)) {
        throw "Gate bloqueado: arquivo de modelo não coberto pelo manifesto: $expectedSource"
    }
}
if ($manifestPaths.Count -ne $expectedSources.Count) {
    throw "Gate bloqueado: manifesto contém fontes extras/obsoletas. Atualize schema-source.sha256 deliberadamente."
}
Write-Host "[EF] Manifesto SHA-256 do modelo validado e completo." -ForegroundColor Green

Write-Host "[EF] $($migrations.Count) arquivo(s) de migration e snapshot encontrados." -ForegroundColor Green
