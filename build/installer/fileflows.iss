[Setup]       
#define MyAppSetupName 'FileFlows'
#define MyAppCopyright 'Copyright © 2020 John Andrews'
#define MyAppName "FileFlows"
#define MyAppVersion "0.0.0.0"
#define MyAppPublisher "John Andrews"
#define MyAppURL "https://www.fileflows.com/"
#define MyAppExeName "FileFlows.exe"


AppName={#MyAppSetupName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppSetupName} {#MyAppVersion}
AppCopyright={#MyAppCopyright}
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
OutputBaseFilename={#MyAppSetupName}-{#MyAppVersion}
DefaultGroupName={#MyAppSetupName}
DefaultDirName={autopf}\{#MyAppSetupName}
UninstallDisplayIcon={app}\FileFlows.exe
SourceDir=src
OutputDir={#SourcePath}\bin
;SetupIconFile=C:\Users\john\src\FileFlows\FileFlows\FileFlows Icon.ico
AllowNoIcons=yes

PrivilegesRequired=admin

// remove next line if you only deploy 32-bit binaries and dependencies
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: en; MessagesFile: "compiler:Default.isl"
Name: nl; MessagesFile: "compiler:Languages\Dutch.isl"
Name: de; MessagesFile: "compiler:Languages\German.isl"

[Dirs]
Name: "{app}"; 
Name: "{app}\Logs"; Permissions: everyone-full     
Name: "{app}\Data"; Permissions: everyone-full
Name: "{app}\Plugins"; Permissions: everyone-full

[Files]
Source: "C:\Users\john\src\FileFlows\FileFlows\deploy\FileFlows\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs   
Source: "C:\utils\ffmpeg\ffmpeg.exe"; DestDir: "{app}\tools"; Flags: ignoreversion recursesubdirs createallsubdirs
  
[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: {sys}\taskkill.exe; Parameters: "/f /im FileFlows.exe"; Flags: skipifdoesntexist runhidden
Filename: {sys}\taskkill.exe; Parameters: "/f /im FileFlows.Server.exe"; Flags: skipifdoesntexist runhidden

[UninstallDelete]
Type: filesandordirs; Name: "{app}\Logs"
Type: filesandordirs; Name: "{app}\Data"    
