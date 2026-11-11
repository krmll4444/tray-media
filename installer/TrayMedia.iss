; Tray Media — Inno Setup 6 installer
#define MyAppName "Tray Media"
#define MyAppVersion "1.0.0-beta.1"
#define MyAppPublisher "krmll4444"
#define MyAppURL "https://github.com/krmll4444/tray-media"
#define MyAppExeName "TrayMedia.exe"

#ifndef DistDir
  #define DistDir "..\dist\installer-payload"
#endif
#ifndef OutDir
  #define OutDir "..\dist"
#endif

[Setup]
AppId={{A7C3E9F1-4B2D-4E8A-9C1F-6D5B8A2E0F33}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=EULA.txt
InfoBeforeFile=
OutputDir={#OutDir}
OutputBaseFilename=TrayMedia-Setup-{#MyAppVersion}
SetupIconFile=..\TrayMedia.App\Assets\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
RestartApplications=no
DisableWelcomePage=no
DisableDirPage=no
AllowNoIcons=yes
VersionInfoVersion=1.0.0.1
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} Setup
VersionInfoCopyright=Copyright (c) 2026 {#MyAppPublisher}
VersionInfoProductName={#MyAppName}
VersionInfoProductTextVersion={#MyAppVersion}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
; Checked by default (no "unchecked" flag) — user can clear it.
Name: "autostart"; Description: "Launch {#MyAppName} when I sign in to Windows"; GroupDescription: "Startup options:"
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"; Flags: unchecked

[Files]
Source: "{#DistDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\NOTICE"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\PRIVACY.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
; Autostart — only when the task remains checked (preselected).
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "TrayMedia"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: autostart

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
var
  SettingsDir, SettingsFile: string;
begin
  if CurStep = ssPostInstall then
  begin
    if WizardIsTaskSelected('autostart') then
    begin
      SettingsDir := ExpandConstant('{localappdata}\TrayMedia');
      ForceDirectories(SettingsDir);
      SettingsFile := SettingsDir + '\settings.json';
      SaveStringToFile(SettingsFile,
        '{' + #13#10 +
        '  "Autostart": true,' + #13#10 +
        '  "PinToTray": true' + #13#10 +
        '}', False);
    end;
  end;
end;
