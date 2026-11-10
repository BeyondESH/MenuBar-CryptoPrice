#define MyAppName "MenuBar CryptoPrice"
#define MyAppVersion "1.1.1"
#define MyAppPublisher "BeyondESH"
#define MyAppURL "https://github.com/BeyondESH/MenuBar-CryptoPrice"
#define MyAppExeName "MenuBarCryptoPrice.exe"

[Setup]
AppId={{F708B73E-D6A9-4954-A7EA-D5234ACBBE31}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={localappdata}\Programs\MenuBarCryptoPrice
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=..\dist
OutputBaseFilename=MenuBarCryptoPrice-Setup-{#MyAppVersion}
SetupIconFile=..\src\MenuBarCryptoPrice\Assets\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.22000
CloseApplications=yes
RestartApplications=no
AppMutex=Local\MenuBarCryptoPrice.Mutex
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} 安装程序
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}
VersionInfoVersion={#MyAppVersion}.0
ShowLanguageDialog=no

[Languages]
Name: "chinesesimplified"; MessagesFile: "ChineseSimplified.isl"

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加任务："; Flags: unchecked

[Files]
Source: "..\dist\MenuBarCryptoPrice.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\settings\*"; DestDir: "{app}\settings"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb"

[Icons]
Name: "{group}\MenuBar CryptoPrice"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\MenuBar CryptoPrice 设置"; Filename: "{app}\settings\MenuBarCryptoPrice.Settings.WinUI.exe"
Name: "{group}\卸载 MenuBar CryptoPrice"; Filename: "{uninstallexe}"
Name: "{autodesktop}\MenuBar CryptoPrice"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "启动 {#MyAppName}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{sys}\taskkill.exe"; Parameters: "/IM MenuBarCryptoPrice.Settings.WinUI.exe /F"; Flags: runhidden waituntilterminated; RunOnceId: "KillSettings"
Filename: "{sys}\taskkill.exe"; Parameters: "/IM MenuBarCryptoPrice.exe /F"; Flags: runhidden waituntilterminated; RunOnceId: "KillHost"

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: none; ValueName: "MenuBarCryptoPrice"; Flags: uninsdeletevalue

[UninstallDelete]
Type: filesandordirs; Name: "{app}\settings"
