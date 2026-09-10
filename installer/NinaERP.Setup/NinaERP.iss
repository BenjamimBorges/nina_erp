#define MyAppName "Nina ERP"
#define MyAppVersion "0.1.0"
#define MyAppPublisher "Nina ERP"
#define Artifacts "..\..\artifacts\windows"

[Setup]
AppId={{C871792D-C188-4B35-9FE1-2D938AA3D492}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\NinaERP
DefaultGroupName=Nina ERP
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=output
OutputBaseFilename=NinaERP-Setup-{#MyAppVersion}-win-x64
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\Desktop\NinaERP.Desktop.exe

[Dirs]
Name: "{commonappdata}\NinaERP"
Name: "{commonappdata}\NinaERP\backups"

[Files]
Source: "{#Artifacts}\server\*"; DestDir: "{app}\Server"; Flags: recursesubdirs createallsubdirs ignoreversion
Source: "{#Artifacts}\desktop\*"; DestDir: "{app}\Desktop"; Flags: recursesubdirs createallsubdirs ignoreversion
Source: "{#Artifacts}\manager\*"; DestDir: "{app}\Manager"; Flags: recursesubdirs createallsubdirs ignoreversion
Source: "prereqs\MicrosoftEdgeWebView2RuntimeInstallerX64.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall ignoreversion skipifsourcedoesntexist

[Icons]
Name: "{group}\Nina ERP"; Filename: "{app}\Desktop\NinaERP.Desktop.exe"
Name: "{group}\Nina ERP Server Manager"; Filename: "{app}\Manager\NinaERP.ServerManager.exe"
Name: "{autodesktop}\Nina ERP"; Filename: "{app}\Desktop\NinaERP.Desktop.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Criar atalho na área de trabalho"; GroupDescription: "Atalhos:"

[Run]
Filename: "{tmp}\MicrosoftEdgeWebView2RuntimeInstallerX64.exe"; Parameters: "/silent /install"; StatusMsg: "Instalando WebView2 Runtime..."; Flags: waituntilterminated skipifdoesntexist
Filename: "{sys}\sc.exe"; Parameters: "create NinaERP.Server binPath= ""{app}\Server\NinaERP.API.exe"" start= auto DisplayName= ""Nina ERP Server"""; Flags: runhidden waituntilterminated; Check: not ServiceExists('NinaERP.Server')
Filename: "{sys}\sc.exe"; Parameters: "failure NinaERP.Server reset= 86400 actions= restart/5000/restart/15000/restart/60000"; Flags: runhidden waituntilterminated
Filename: "{app}\Manager\NinaERP.ServerManager.exe"; Description: "Configurar e inicializar o servidor agora"; Flags: postinstall nowait skipifsilent

[UninstallRun]
Filename: "{sys}\sc.exe"; Parameters: "stop NinaERP.Server"; Flags: runhidden waituntilterminated; RunOnceId: "StopNinaERPServer"
Filename: "{sys}\sc.exe"; Parameters: "delete NinaERP.Server"; Flags: runhidden waituntilterminated; RunOnceId: "DeleteNinaERPServer"

[Code]
function ServiceExists(const ServiceName: string): Boolean;
var
  ResultCode: Integer;
begin
  Exec(ExpandConstant('{sys}\sc.exe'), 'query ' + ServiceName, '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := ResultCode = 0;
end;
