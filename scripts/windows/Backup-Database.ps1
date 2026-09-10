[CmdletBinding()]
param(
    [string]$ConfigPath = "$env:ProgramData\NinaERP\appsettings.Production.json",
    [string]$OutputDirectory = "$env:ProgramData\NinaERP\backups",
    [ValidateRange(1,3650)][int]$RetentionDays = 30
)
$ErrorActionPreference = "Stop"
if (-not (Test-Path $ConfigPath)) { throw "Configuração não encontrada: $ConfigPath" }
$config = Get-Content $ConfigPath -Raw | ConvertFrom-Json
$conn = $config.ConnectionStrings.DefaultConnection
$parts = @{}
$conn -split ';' | ForEach-Object { if ($_ -match '^([^=]+)=(.*)$') { $parts[$matches[1].Trim()] = $matches[2] } }
foreach ($required in @('Host','Port','Username','Password','Database')) {
    if (-not $parts.ContainsKey($required) -or [string]::IsNullOrWhiteSpace($parts[$required])) { throw "Connection string sem '$required'." }
}
$pgDump = Get-ChildItem "C:\Program Files\PostgreSQL\*\bin\pg_dump.exe" -ErrorAction SilentlyContinue | Sort-Object FullName -Descending | Select-Object -First 1
$pgRestore = Get-ChildItem "C:\Program Files\PostgreSQL\*\bin\pg_restore.exe" -ErrorAction SilentlyContinue | Sort-Object FullName -Descending | Select-Object -First 1
if (-not $pgDump -or -not $pgRestore) { throw "pg_dump.exe/pg_restore.exe não encontrados." }
New-Item $OutputDirectory -ItemType Directory -Force | Out-Null
$file = Join-Path $OutputDirectory ("ninaerp-{0:yyyyMMdd-HHmmss}.backup" -f (Get-Date))
$env:PGPASSWORD = $parts['Password']
try {
    & $pgDump.FullName --format=custom --no-owner --host $parts['Host'] --port $parts['Port'] --username $parts['Username'] --file $file $parts['Database']
    if ($LASTEXITCODE -ne 0) { throw "Falha no pg_dump (código $LASTEXITCODE)." }
    & $pgRestore.FullName --list $file | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "O arquivo gerado não passou na validação pg_restore --list." }
    $hash = (Get-FileHash -Path $file -Algorithm SHA256).Hash
    Set-Content -Path "$file.sha256" -Value "$hash  $(Split-Path $file -Leaf)" -Encoding ascii
    Get-ChildItem $OutputDirectory -Filter 'ninaerp-*.backup' -File |
        Where-Object LastWriteTime -lt (Get-Date).AddDays(-$RetentionDays) |
        ForEach-Object {
            Remove-Item $_.FullName -Force
            Remove-Item "$($_.FullName).sha256" -Force -ErrorAction SilentlyContinue
        }
    Write-Host "Backup validado: $file" -ForegroundColor Green
    Write-Host "SHA256: $hash" -ForegroundColor Green
}
finally {
    Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
}
