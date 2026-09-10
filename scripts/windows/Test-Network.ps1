[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$Server,
    [int]$Port = 7000
)
$ErrorActionPreference = "Stop"
$uri = "http://${Server}:${Port}/health"
Write-Host "Testando TCP ${Server}:${Port}..." -ForegroundColor Cyan
$tcp = Test-NetConnection -ComputerName $Server -Port $Port -WarningAction SilentlyContinue
if (-not $tcp.TcpTestSucceeded) { throw "A porta $Port não está acessível em $Server. Verifique serviço e Firewall do Windows." }
Write-Host "TCP OK. Testando health endpoint..." -ForegroundColor Cyan
$result = Invoke-RestMethod -Uri $uri -TimeoutSec 8
$result | Format-List
Write-Host "Cliente ↔ servidor OK." -ForegroundColor Green
