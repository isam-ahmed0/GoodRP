; Auto-version: pulled from the published exe (which gets its version from
; src/GoodRP.csproj <Version>). Build/publish BEFORE running iscc.
#define MyAppVersion GetFileVersion("..\publish\GoodRP.exe")

[Setup]
AppName=GoodRP
AppId=GoodRP
AppVersion={#MyAppVersion}
AppPublisher=GoodRP
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany=GoodRP
DefaultDirName={autopf}\GoodRP
DefaultGroupName=GoodRP
OutputDir=..\installer-output
OutputBaseFilename=GoodRP-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayName=GoodRP
CloseApplications=yes
RestartApplications=no
SetupIconFile=..\GoodRP.ico
WizardImageFile=goodrp-sidebar.bmp
WizardSmallImageFile=goodrp-small.bmp

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create desktop shortcut"; GroupDescription: "Additional shortcuts:"
Name: "startupicon"; Description: "Run at Windows startup"; GroupDescription: "Startup:"; Flags: unchecked

[Files]
Source: "..\publish\GoodRP.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\GoodRP"; Filename: "{app}\GoodRP.exe"
Name: "{group}\Uninstall GoodRP"; Filename: "{uninstallexe}"
Name: "{autodesktop}\GoodRP"; Filename: "{app}\GoodRP.exe"; Tasks: desktopicon
Name: "{userstartup}\GoodRP"; Filename: "{app}\GoodRP.exe"; Tasks: startupicon

[Run]
Filename: "{app}\GoodRP.exe"; Description: "Launch GoodRP now"; Flags: nowait postinstall skipifsilent

[Registry]
Root: HKCU; Subkey: "Software\GoodRP"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"; Flags: uninsdeletekey

; NOTE: User data is preserved on uninstall/update.
; Config lives in %APPDATA%\GoodRP (never touched by the installer).
; The install folder is NOT force-deleted, so any user files placed there
; (e.g. .grp scripts) survive. Inno Setup auto-removes only the files it
; installed (GoodRP.exe). Do NOT add a blanket [UninstallDelete] for {app}.

[Messages]
WelcomeLabel1=Welcome to the GoodRP Setup Wizard
WelcomeLabel2=GoodRP shows your currently playing music as a Discord Rich Presence.%n%nThis wizard will install GoodRP on your computer.
FinishedLabel=GoodRP has been installed.%n%nLaunch it from your Start Menu. Your existing settings are preserved.

[Code]
procedure InitializeWizard();
begin
  // GoodRP dark palette (Inno colors are $00BBGGRR)
  WizardForm.Color := $002E1A1A;
  if Assigned(WizardForm.WelcomeLabel1) then WizardForm.WelcomeLabel1.Font.Color := clWhite;
  if Assigned(WizardForm.WelcomeLabel2) then WizardForm.WelcomeLabel2.Font.Color := $00B0C0C0;
  if Assigned(WizardForm.FinishedHeadingLabel) then WizardForm.FinishedHeadingLabel.Font.Color := clWhite;
  if Assigned(WizardForm.FinishedLabel) then WizardForm.FinishedLabel.Font.Color := $00B0C0C0;
  if Assigned(WizardForm.PageNameLabel) then WizardForm.PageNameLabel.Font.Color := clWhite;
  if Assigned(WizardForm.PageDescriptionLabel) then WizardForm.PageDescriptionLabel.Font.Color := $00B0C0C0;
  if Assigned(WizardForm.MainPanel) then WizardForm.MainPanel.Color := $002E1A1A;
  if Assigned(WizardForm.InnerPage) then WizardForm.InnerPage.Color := $002E1A1A;
end;
