[CmdletBinding()]
param(
    [string]$Server = "127.0.0.1",
    [int]$Port = 7000,
    [Parameter(Mandatory=$true)][string]$Username,
    [Parameter(Mandatory=$true)][string]$Password
)
$ErrorActionPreference = "Stop"
$BaseUrl = "http://${Server}:$Port"

Write-Host "=== Nina ERP • Smoke Test ===" -ForegroundColor Cyan
Write-Host "Servidor: $BaseUrl"

Write-Host "[1/5] TCP..."
$tcp = Test-NetConnection -ComputerName $Server -Port $Port -WarningAction SilentlyContinue
if (-not $tcp.TcpTestSucceeded) { throw "A porta $Port não está acessível em $Server." }

Write-Host "[2/5] Health..."
$health = Invoke-RestMethod -Uri "$BaseUrl/health" -Method Get -TimeoutSec 10
if ($health.status -ne "healthy") { throw "Health check não retornou healthy. Resposta: $($health | ConvertTo-Json -Compress)" }

Write-Host "[3/5] Login..."
$body = @{ username = $Username; password = $Password } | ConvertTo-Json
$login = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method Post -ContentType "application/json" -Body $body -TimeoutSec 10
if (-not $login.success -or -not $login.data.token) { throw "Login não retornou token." }
$headers = @{ Authorization = "Bearer $($login.data.token)" }

Write-Host "[4/5] Identidade e dados básicos..."
$me = Invoke-RestMethod -Uri "$BaseUrl/api/auth/me" -Headers $headers -Method Get -TimeoutSec 10
$products = Invoke-RestMethod -Uri "$BaseUrl/api/products" -Headers $headers -Method Get -TimeoutSec 10
$dashboard = Invoke-RestMethod -Uri "$BaseUrl/api/dashboard" -Headers $headers -Method Get -TimeoutSec 10
if (-not $me.success -or -not $products.success -or -not $dashboard.success) { throw "Uma API autenticada falhou." }

Write-Host "[5/5] Resultado..."
Write-Host "Usuário: $($login.data.fullName) / $($login.data.role)" -ForegroundColor Green
Write-Host "Produtos retornados: $($products.data.Count)" -ForegroundColor Green
Write-Host "Smoke test concluído com sucesso." -ForegroundColor Green
