#define AppName "Traz Base"
#define AppPublisher "Traz Base"
#define AppExeName "TrazBase.exe"
#define AppVersion "0.8.0"
#define AppFileVersion "0.8.0.0"

[Setup]
AppId={{6BB4D28C-AE7D-4627-8F01-CCEA7B3311B5}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\TrazBase
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
SetupIconFile=..\Imagens\icone\icone_supra.ico
UninstallDisplayIcon={app}\{#AppExeName}
OutputDir=.
OutputBaseFilename=TrazBase-Setup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
SetupLogging=yes
TimeStampsInUTC=yes
VersionInfoVersion={#AppFileVersion}
VersionInfoDescription=Instalador do Traz Base
VersionInfoProductName={#AppName}
VersionInfoProductVersion={#AppVersion}

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "Criar um atalho na área de trabalho"; GroupDescription: "Atalhos adicionais:"; Flags: unchecked

[Files]
Source: "Aplicativo\*"; DestDir: "{app}"; Excludes: "*.json,*.pdb,Imagens\*.svg,Imagens\icone\*.ico"; Flags: recursesubdirs createallsubdirs ignoreversion
Source: "Aplicativo\*.json"; DestDir: "{app}"; Flags: comparetimestamp nocompression
Source: "Aplicativo\*.pdb"; DestDir: "{app}"; Flags: comparetimestamp nocompression
Source: "Aplicativo\Imagens\*.svg"; DestDir: "{app}\Imagens"; Flags: comparetimestamp nocompression
Source: "Aplicativo\Imagens\icone\*.ico"; DestDir: "{app}\Imagens\icone"; Flags: comparetimestamp nocompression

[Icons]
Name: "{autoprograms}\Traz Base"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\Traz Base"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

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
