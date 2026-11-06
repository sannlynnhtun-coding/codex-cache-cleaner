#ifndef PublishDir
  #define PublishDir "..\portable"
#endif

[Setup]
AppId={{C3DD3636-2126-49B6-AE3E-3B2508DD723A}
AppName=Codex Cache Care
AppVersion={#GetStringFileInfo(PublishDir + "\CodexCacheCare.exe", "ProductVersion")}
AppPublisher=Sann Lynn Htun
AppPublisherURL=https://github.com/sannlynnhtun-coding/codex-cache-cleaner
DefaultDirName={localappdata}\Programs\Codex Cache Care
PrivilegesRequired=lowest
ArchitecturesAllowed=x64os
ArchitecturesInstallIn64BitMode=x64os
MinVersion=10.0.19045
OutputDir=..\portable
OutputBaseFilename=CodexCacheCare-Setup-x64
SetupIconFile=..\CodexCacheCare\Assets\Codex.ico
UninstallDisplayIcon={app}\CodexCacheCare.exe
Compression=lzma2/fast
SolidCompression=yes
WizardStyle=modern
DisableProgramGroupPage=yes

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "CodexCacheCare-Setup-x64.exe"

[Icons]
Name: "{autoprograms}\Codex Cache Care"; Filename: "{app}\CodexCacheCare.exe"; WorkingDir: "{app}"

[Run]
Filename: "{app}\CodexCacheCare.exe"; Description: "Launch Codex Cache Care"; Flags: nowait postinstall skipifsilent
