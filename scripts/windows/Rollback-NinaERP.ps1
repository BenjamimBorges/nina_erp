[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param(
  [Parameter(Mandatory=$true)][string]$RollbackDirectory,
  [string]$InstallRoot="$env:ProgramFiles\NinaERP",
  [string]$DatabaseBackup,
  [string]$ConfigPath="$env:ProgramData\NinaERP\appsettings.Production.json"
)
$ErrorActionPreference='Stop'
if(-not(Test-Path $RollbackDirectory)){throw "Snapshot de rollback não encontrado: $RollbackDirectory"}
if(-not $PSCmdlet.ShouldProcess($InstallRoot,'Restaurar versão anterior do Nina ERP')){return}
Stop-Service NinaERP.Server -ErrorAction SilentlyContinue
try{
  Remove-Item "$InstallRoot\*" -Recurse -Force -ErrorAction SilentlyContinue
  Copy-Item "$RollbackDirectory\*" $InstallRoot -Recurse -Force
  if($DatabaseBackup){
    & (Join-Path $PSScriptRoot 'Restore-Database.ps1') -BackupFile $DatabaseBackup -ConfigPath $ConfigPath -Confirm:$false
  }
}
finally{Start-Service NinaERP.Server -ErrorAction SilentlyContinue}
Write-Host 'Rollback concluído. Execute Smoke-Test.ps1 e conciliação antes da reabertura.' -ForegroundColor Yellow
