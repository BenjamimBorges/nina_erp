[CmdletBinding()]
param(
    [string]$Server = "127.0.0.1",
    [int]$Port = 7000,
    [Parameter(Mandatory=$true)][string]$CompanyCnpj,
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

Write-Host "[3/5] Login com sessão HttpOnly..."
$body = @{ companyCnpj = $CompanyCnpj; username = $Username; password = $Password } | ConvertTo-Json
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$login = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method Post -ContentType "application/json" -Headers @{ "X-Requested-With" = "XMLHttpRequest" } -Body $body -WebSession $session -TimeoutSec 10
if (-not $login.success) { throw "Login falhou." }
if ($null -ne $login.data.token) { throw "Falha de segurança: login expôs JWT no corpo JSON." }
if (-not $session.Cookies.GetCookies($BaseUrl)['ninaerp_session']) { throw "Cookie de sessão não foi recebido." }

Write-Host "[4/5] Identidade e dados básicos..."
$me = Invoke-RestMethod -Uri "$BaseUrl/api/auth/me" -WebSession $session -Method Get -TimeoutSec 10
$products = Invoke-RestMethod -Uri "$BaseUrl/api/products" -WebSession $session -Method Get -TimeoutSec 10
$dashboard = Invoke-RestMethod -Uri "$BaseUrl/api/dashboard" -WebSession $session -Method Get -TimeoutSec 10
if (-not $me.success -or -not $products.success -or -not $dashboard.success) { throw "Uma API autenticada falhou." }

Write-Host "[5/5] Logout..."
Invoke-RestMethod -Uri "$BaseUrl/api/auth/logout" -WebSession $session -Headers @{ "X-Requested-With" = "XMLHttpRequest" } -Method Post -TimeoutSec 10 | Out-Null
try {
    Invoke-RestMethod -Uri "$BaseUrl/api/auth/me" -WebSession $session -Method Get -TimeoutSec 10 | Out-Null
    throw "Sessão permaneceu autenticada após logout."
} catch {
    if ($_.Exception.Response -and [int]$_.Exception.Response.StatusCode -ne 401) { throw }
}

Write-Host "Usuário: $($login.data.fullName) / $($login.data.role)" -ForegroundColor Green
Write-Host "Produtos retornados: $($products.data.Count)" -ForegroundColor Green
Write-Host "Smoke test concluído com sucesso." -ForegroundColor Green
