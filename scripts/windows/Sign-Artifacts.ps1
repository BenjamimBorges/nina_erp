[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$CertificateThumbprint,
    [string]$ArtifactsDirectory = "$(Resolve-Path (Join-Path $PSScriptRoot '..\..\artifacts\windows'))",
    [string]$TimestampUrl = "http://timestamp.digicert.com",
    [switch]$SkipTimestamp
)
$ErrorActionPreference='Stop'
$signtool=(Get-Command signtool.exe -ErrorAction SilentlyContinue)
if (-not $signtool) {
  $candidate=Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\x64\signtool.exe" -ErrorAction SilentlyContinue | Sort-Object FullName -Descending | Select-Object -First 1
  if($candidate){$signtool=$candidate}
}
if (-not $signtool) { throw 'signtool.exe não encontrado. Instale o Windows SDK.' }
$signtoolPath = if($signtool.Source){$signtool.Source}else{$signtool.FullName}
$cert=Get-ChildItem Cert:\CurrentUser\My,Cert:\LocalMachine\My -ErrorAction SilentlyContinue | Where-Object Thumbprint -eq ($CertificateThumbprint -replace '\s','').ToUpperInvariant() | Select-Object -First 1
if (-not $cert) { throw 'Certificado de assinatura não encontrado pelo thumbprint informado.' }
$targets=Get-ChildItem $ArtifactsDirectory -Recurse -File | Where-Object { $_.Extension -in @('.exe','.msi') }
if (-not $targets) { throw "Nenhum EXE/MSI encontrado em $ArtifactsDirectory" }
foreach($file in $targets){
  if($SkipTimestamp){
    & $signtoolPath sign /sha1 $cert.Thumbprint /fd SHA256 $file.FullName
  } else {
    & $signtoolPath sign /sha1 $cert.Thumbprint /fd SHA256 /tr $TimestampUrl /td SHA256 $file.FullName
  }
  if($LASTEXITCODE -ne 0){ throw "Falha ao assinar $($file.FullName)." }
  & $signtoolPath verify /pa /all $file.FullName | Out-Null
  if($LASTEXITCODE -ne 0){ throw "Assinatura inválida em $($file.FullName)." }
}
Write-Host "Assinatura verificada em $($targets.Count) artefato(s)." -ForegroundColor Green
