; ¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T
;  Iron Vault ¡ª Inno Setup installer script (CI-safe version)
; ¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif
#ifndef SourceDir
  #error SourceDir must be passed on the command line: /DSourceDir="..."
#endif
#ifndef OutputDir
  #define OutputDir "."
#endif

#define AppName      "Iron Vault"
#define AppPublisher "nevermorewd"
#define AppURL       "https://github.com/nevermorewd/IronVault"
#define AppExeName   "IronVault.Desktop.exe"

[Setup]
AppId={{7A1E3C4D-2B6F-4D8E-9A3C-5F71B0E2D8A9}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}/issues
AppUpdatesURL={#AppURL}/releases

DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}

PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=commandline

OutputDir={#OutputDir}
OutputBaseFilename=IronVault-{#AppVersion}-setup

WizardStyle=modern

Compression=lzma2/ultra64
SolidCompression=yes
LZMAUseSeparateProcess=yes

ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible

UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#AppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; \
  GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; \
  Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; \
  Description: "{cm:LaunchProgram,{#AppName}}"; \
  Flags: nowait postinstall skipifsilent

; ¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T
;  Exit code mapping (CI / Microsoft Store compatible)
; ¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T¨T

[Code]
const
  UNINST_KEY = 'Software\Microsoft\Windows\CurrentVersion\Uninstall\' +
               '{7A1E3C4D-2B6F-4D8E-9A3C-5F71B0E2D8A9}_is1';

var
  GAlreadyInstalled: Boolean;
  GNeedsRestart: Boolean;   // Captured in NeedRestart() for use in exit-code mapping

// ©¤©¤ Called once at startup: detect whether an existing installation is present ©¤©¤
function InitializeSetup(): Boolean;
var
  S: String;
begin
  Result := True;
  GAlreadyInstalled :=
    RegQueryStringValue(HKLM, UNINST_KEY, 'UninstallString', S) or
    RegQueryStringValue(HKCU, UNINST_KEY, 'UninstallString', S);
  GNeedsRestart := False;
end;

// ©¤©¤ Called by the wizard after files are installed ©¤©¤
// Returning True here triggers the "Restart now?" prompt.
// We also cache the result so GetCustomSetupExitCode can read it safely.
function NeedRestart(): Boolean;
begin
  Result := False;          // Change to True if your app ever requires a reboot
  GNeedsRestart := Result;
end;

// ©¤©¤ CI / Microsoft Store compatible exit codes ©¤©¤
//   0    = clean first-time install
//   1638 = product already installed (another version present)
//   3010 = success, but a reboot is required
function GetCustomSetupExitCode: Integer;
begin
  if GNeedsRestart then
    Result := 3010
  else if GAlreadyInstalled then
    Result := 1638
  else
    Result := 0;
end;