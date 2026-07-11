[Setup]
AppName=GoodRP
AppVersion=1.0.0
AppPublisher=GoodRP
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

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
