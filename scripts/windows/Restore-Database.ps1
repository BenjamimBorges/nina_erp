[CmdletBinding(SupportsShouldProcess=$true, ConfirmImpact='High')]
param(
    [Parameter(Mandatory=$true)][string]$BackupFile,
    [string]$ConfigPath = "$env:ProgramData\NinaERP\appsettings.Production.json",
    [switch]$SkipChecksum
)
$ErrorActionPreference = "Stop"
if (-not (Test-Path $BackupFile)) { throw "Backup não encontrado: $BackupFile" }
if (-not (Test-Path $ConfigPath)) { throw "Configuração não encontrada: $ConfigPath" }
$checksumFile = "$BackupFile.sha256"
if (-not $SkipChecksum) {
    if (-not (Test-Path $checksumFile)) { throw "Checksum ausente: $checksumFile. Use -SkipChecksum somente após validação manual." }
    $expected = ((Get-Content $checksumFile -Raw).Trim() -split '\s+')[0].ToUpperInvariant()
    $actual = (Get-FileHash -Path $BackupFile -Algorithm SHA256).Hash.ToUpperInvariant()
    if ($actual -ne $expected) { throw "Checksum SHA256 inválido. A restauração foi abortada." }
}
$config = Get-Content $ConfigPath -Raw | ConvertFrom-Json
$conn = $config.ConnectionStrings.DefaultConnection
$parts = @{}
$conn -split ';' | ForEach-Object { if ($_ -match '^([^=]+)=(.*)$') { $parts[$matches[1].Trim()] = $matches[2] } }
$pgRestore = Get-ChildItem "C:\Program Files\PostgreSQL\*\bin\pg_restore.exe" -ErrorAction SilentlyContinue | Sort-Object FullName -Descending | Select-Object -First 1
if (-not $pgRestore) { throw "pg_restore.exe não encontrado." }
& $pgRestore.FullName --list $BackupFile | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Backup inválido segundo pg_restore --list." }
if (-not $PSCmdlet.ShouldProcess($parts['Database'], "Restaurar backup $BackupFile")) { return }
Stop-Service NinaERP.Server -ErrorAction SilentlyContinue
try {
    $env:PGPASSWORD = $parts['Password']
    & $pgRestore.FullName --clean --if-exists --no-owner --host $parts['Host'] --port $parts['Port'] --username $parts['Username'] --dbname $parts['Database'] $BackupFile
    if ($LASTEXITCODE -ne 0) { throw "Falha no pg_restore (código $LASTEXITCODE)." }
}
finally {
    Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
    Start-Service NinaERP.Server -ErrorAction SilentlyContinue
}
Write-Host "Restauração concluída. Execute Smoke-Test.ps1 e conciliação antes de liberar o ambiente." -ForegroundColor Green
