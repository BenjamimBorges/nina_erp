[CmdletBinding()]
param([int]$Port = 7000)
$ErrorActionPreference = "Stop"
$name = "Nina ERP API ($Port)"
$existing = Get-NetFirewallRule -DisplayName $name -ErrorAction SilentlyContinue
if ($existing) { Remove-NetFirewallRule -DisplayName $name }
New-NetFirewallRule -DisplayName $name -Direction Inbound -Action Allow -Protocol TCP -LocalPort $Port -Profile Domain,Private | Out-Null
Write-Host "Regra criada para TCP $Port nos perfis Domain/Private." -ForegroundColor Green
