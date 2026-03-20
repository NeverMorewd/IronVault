; ═══════════════════════════════════════════════════════════════════════════
;  Iron Vault — Inno Setup installer script
;
;  Called by the CI workflow (release.yml) with:
;    iscc /DAppVersion="v1.2.3" /DSourceDir="abs\path\to\publish\win-x64" setup.iss
;
;  Output: IronVault-<AppVersion>-setup.exe  (written to the OutputDir below)
; ═══════════════════════════════════════════════════════════════════════════

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
; ── Identity ──────────────────────────────────────────────────────────────
; AppId uniquely identifies this installer for Windows uninstall records.
; NEVER change this GUID after the first public release.
AppId={{7A1E3C4D-2B6F-4D8E-9A3C-5F71B0E2D8A9}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}/issues
AppUpdatesURL={#AppURL}/releases

; ── Install location ───────────────────────────────────────────────────────
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
; Allow non-admin installs (no UAC prompt for users with write access)
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=commandline

; ── Output ────────────────────────────────────────────────────────────────
OutputDir={#OutputDir}
OutputBaseFilename=IronVault-{#AppVersion}-setup

; ── Appearance ────────────────────────────────────────────────────────────
WizardStyle=modern
WizardSmallImageFile=

; ── Compression ───────────────────────────────────────────────────────────
Compression=lzma2/ultra64
SolidCompression=yes
LZMAUseSeparateProcess=yes

; ── Architecture ──────────────────────────────────────────────────────────
; NativeAOT win-x64 — 64-bit Windows only
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible

; ── Uninstall ─────────────────────────────────────────────────────────────
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#AppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; \
  GroupDescription: "{cm:AdditionalIcons}"

[Files]
; Copy all files from the NativeAOT publish output recursively
Source: "{#SourceDir}\*"; DestDir: "{app}"; \
  Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}";                       Filename: "{app}\{#AppExeName}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}";                 Filename: "{app}\{#AppExeName}"; \
  Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; \
  Description: "{cm:LaunchProgram,{#AppName}}"; \
  Flags: nowait postinstall skipifsilent

; ═══════════════════════════════════════════════════════════════════════════
;  Exit-code mapping for Microsoft Store "standard installer" requirements.
;
;  Inno Setup built-in codes (cannot be overridden by GetCustomSetupExitCode):
;    0  – success (or app already installed and re-install succeeded)
;    1  – setup initialisation failed / another instance already running
;    2  – user cancelled wizard BEFORE installation started
;    4  – fatal error during installation (e.g. disk full mid-copy)
;    5  – user cancelled DURING actual file installation
;
;  GetCustomSetupExitCode() lets us return custom codes when Inno would
;  otherwise return 0.  We use it to emit the Windows-standard 3010
;  (ERROR_SUCCESS_REBOOT_REQUIRED) so Microsoft can detect restart needs.
; ═══════════════════════════════════════════════════════════════════════════
[Code]
// Called by Inno Setup when the installer would exit with 0 (success).
// Return 3010 if the OS needs a reboot to finish applying the installation.
function GetCustomSetupExitCode: Integer;
begin
  if NeedRestart() then
    Result := 3010   // ERROR_SUCCESS_REBOOT_REQUIRED — standard Win32 code
  else
    Result := 0;
end;
