[CmdletBinding(SupportsShouldProcess=$true,ConfirmImpact='High')]
param(
  [Parameter(Mandatory=$true)][string]$PackageZip,
  [string]$PackageChecksumFile,
  [string]$InstallRoot = "$env:ProgramFiles\NinaERP",
  [string]$ConfigPath = "$env:ProgramData\NinaERP\appsettings.Production.json",
  [Parameter(Mandatory=$true)][string]$ExpectedSignerThumbprint,
  [switch]$ApplyDatabaseMigration
)
$ErrorActionPreference='Stop'
$Root=Resolve-Path (Join-Path $PSScriptRoot '..\..')
if(-not(Test-Path $PackageZip)){throw "Pacote ausente: $PackageZip"}
if([string]::IsNullOrWhiteSpace($PackageChecksumFile)){$PackageChecksumFile="$PackageZip.sha256"}
if(-not(Test-Path $PackageChecksumFile)){throw "Checksum do pacote ausente: $PackageChecksumFile"}
$expected=((Get-Content $PackageChecksumFile -Raw).Trim() -split '\s+')[0].ToUpperInvariant()
$actual=(Get-FileHash $PackageZip -Algorithm SHA256).Hash.ToUpperInvariant()
if($actual -ne $expected){throw 'Checksum SHA-256 do pacote inválido.'}
$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'; $rollback="$env:ProgramData\NinaERP\rollback\$stamp"; $staging="$env:TEMP\NinaERP-update-$stamp"
New-Item $rollback,$staging -ItemType Directory -Force | Out-Null
Expand-Archive -Path $PackageZip -DestinationPath $staging -Force
$manifestPath=Join-Path $staging 'release-manifest.json'
if(-not(Test-Path $manifestPath)){throw 'Pacote sem release-manifest.json.'}
$manifest=Get-Content $manifestPath -Raw | ConvertFrom-Json
foreach($item in $manifest.files){
  $file=Join-Path $staging ([string]$item.path).Replace('/','\')
  if(-not(Test-Path $file)){throw "Arquivo do manifesto ausente: $($item.path)"}
  if((Get-FileHash $file -Algorithm SHA256).Hash.ToUpperInvariant() -ne ([string]$item.sha256).ToUpperInvariant()){throw "Integridade interna inválida: $($item.path)"}
}
$expectedSigner=($ExpectedSignerThumbprint -replace '\s','').ToUpperInvariant()
if($expectedSigner -notmatch '^[0-9A-F]{40,64}$'){throw 'Thumbprint de assinatura inválido.'}
$signedTargets=Get-ChildItem $staging -Recurse -File | Where-Object { $_.Extension -in @('.exe','.msi') }
if(-not $signedTargets){throw 'Pacote não contém EXE/MSI para validação Authenticode.'}
foreach($file in $signedTargets){
  $signature=Get-AuthenticodeSignature $file.FullName
  if($signature.Status -ne 'Valid'){throw "Assinatura Authenticode inválida em $($file.Name): $($signature.Status)"}
  $actualSigner=([string]$signature.SignerCertificate.Thumbprint -replace '\s','').ToUpperInvariant()
  if($actualSigner -ne $expectedSigner){throw "Assinante inesperado em $($file.Name)."}
}
if(Test-Path $InstallRoot){Copy-Item "$InstallRoot\*" $rollback -Recurse -Force}
& (Join-Path $Root 'scripts\windows\Backup-Database.ps1') -ConfigPath $ConfigPath
if(-not $PSCmdlet.ShouldProcess($InstallRoot,"Atualizar Nina ERP para $($manifest.version)")){return}
Stop-Service NinaERP.Server -ErrorAction SilentlyContinue
try{
  if($ApplyDatabaseMigration){
    $bundle=Get-ChildItem $staging -Filter efbundle.exe -Recurse | Select-Object -First 1
    if(-not $bundle){throw 'efbundle.exe ausente; migration abortada.'}
    $config=Get-Content $ConfigPath -Raw | ConvertFrom-Json
    & $bundle.FullName --connection $config.ConnectionStrings.DefaultConnection
    if($LASTEXITCODE -ne 0){throw 'EF migration bundle falhou; utilize rollback e backup.'}
  }
  New-Item $InstallRoot -ItemType Directory -Force | Out-Null
  Remove-Item "$InstallRoot\*" -Recurse -Force -ErrorAction SilentlyContinue
  Copy-Item "$staging\*" $InstallRoot -Recurse -Force -Exclude 'release-manifest.json','release-manifest.sha256'
}
catch{ Write-Error "Atualização falhou. Snapshot anterior: $rollback. Banco não é restaurado automaticamente. Erro: $_"; throw }
finally{ Remove-Item $staging -Recurse -Force -ErrorAction SilentlyContinue; Start-Service NinaERP.Server -ErrorAction SilentlyContinue }
Write-Host "Atualização aplicada. Execute Smoke-Test.ps1. Rollback: $rollback" -ForegroundColor Green
