#define AppName "SupraKit"
#define AppPublisher "SupraKit"
#define AppExeName "SupraKit.exe"
#define AppVersion "1.0.0"
#define AppFileVersion "1.0.0.0"

[Setup]
AppId={{6BB4D28C-AE7D-4627-8F01-CCEA7B3311B5}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\SupraKit
; Força a migração da pasta do Traz Base mesmo em atualizações (UsePreviousAppDir=yes usaria a antiga)
UsePreviousAppDir=no
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
SetupIconFile=..\Imagens\icone\icone_supra.ico
UninstallDisplayIcon={app}\{#AppExeName}
OutputDir=.
OutputBaseFilename=SupraKit-Setup
Compression=lzma2/ultra64
SolidCompression=yes
CloseApplications=force
WizardStyle=modern
SetupLogging=yes
TimeStampsInUTC=yes
VersionInfoVersion={#AppFileVersion}
VersionInfoDescription=Instalador do SupraKit
VersionInfoProductName={#AppName}
VersionInfoProductVersion={#AppVersion}

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "Criar um atalho na área de trabalho"; GroupDescription: "Atalhos adicionais:"; Flags: unchecked

[Files]
Source: "Aplicativo\*"; DestDir: "{app}"; Excludes: "*.json,*.pdb,Imagens\*.svg,Imagens\*.png,Imagens\icone\*.ico"; Flags: recursesubdirs createallsubdirs ignoreversion
Source: "Aplicativo\*.json"; DestDir: "{app}"; Flags: comparetimestamp nocompression
Source: "Aplicativo\*.pdb"; DestDir: "{app}"; Flags: comparetimestamp nocompression
Source: "Aplicativo\Imagens\*.svg"; DestDir: "{app}\Imagens"; Flags: comparetimestamp nocompression
Source: "Aplicativo\Imagens\*.png"; DestDir: "{app}\Imagens"; Flags: comparetimestamp nocompression
Source: "Aplicativo\Imagens\icone\*.ico"; DestDir: "{app}\Imagens\icone"; Flags: comparetimestamp nocompression

[InstallDelete]
; Remove a instalação antiga do Traz Base após a migração para a nova pasta
Type: filesandordirs; Name: "{autopf}\TrazBase"
; Remove os atalhos antigos: além de apontarem para o exe antigo, usam o AUMID antigo
; e fariam os toasts do Windows serem descartados. Inclui atalhos criados pelo Instalar.ps1.
Type: files; Name: "{autoprograms}\Traz Base.lnk"
Type: files; Name: "{autodesktop}\Traz Base.lnk"

[Icons]
Name: "{autoprograms}\SupraKit"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; AppUserModelID: "BrunoPasseti.SupraKit"
Name: "{autodesktop}\SupraKit"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; AppUserModelID: "BrunoPasseti.SupraKit"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Flags: nowait runasoriginaluser

[Code]
const
  UninstallKey = 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{6BB4D28C-AE7D-4627-8F01-CCEA7B3311B5}_is1';

function InitializeSetup(): Boolean;
var
  InstalledVersionText: String;
  InstalledVersion: Int64;
  SetupVersion: Int64;
begin
  Result := True;
  InstalledVersionText := '';
  if not RegQueryStringValue(HKLM, UninstallKey, 'DisplayVersion', InstalledVersionText) then
    RegQueryStringValue(HKCU, UninstallKey, 'DisplayVersion', InstalledVersionText);
  if InstalledVersionText <> '' then
  begin
    if StrToVersion(InstalledVersionText, InstalledVersion) and
       StrToVersion('{#AppVersion}', SetupVersion) and
       (ComparePackedVersion(SetupVersion, InstalledVersion) < 0) then
    begin
      MsgBox('A versão ' + InstalledVersionText + ' já está instalada. Este instalador contém a versão ' +
        '{#AppVersion}' + ', que não é mais recente.', mbInformation, MB_OK);
      Result := False;
    end;
  end;
end;
