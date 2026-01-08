; Moyu Windows Installer Script
; 使用 Inno Setup 编译此脚本生成安装程序
; 下载 Inno Setup: https://jrsoftware.org/isdl.php

#define MyAppName "摸鱼背单词"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Moyu Team"
#define MyAppURL "https://github.com/your-repo/moyu"
#define MyAppExeName "Moyu.exe"

[Setup]
; 基本信息
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\Moyu
DefaultGroupName={#MyAppName}
OutputDir=installer
OutputBaseFilename=MoyuSetup_{#MyAppVersion}
SetupIconFile=Resources\icon.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest

; 语言
[Languages]
Name: "chinesesimplified"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"

; 安装的文件
[Files]
Source: "publish\Moyu.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish\moyu.db"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish\*.dll"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: FileExists('publish\*.dll')
; 注意: 如果是单文件发布，只需要 Moyu.exe 和 moyu.db

; 创建快捷方式
[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\卸载 {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

; 可选任务
[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加选项:"; Flags: unchecked
Name: "startupicon"; Description: "开机自动启动"; GroupDescription: "附加选项:"; Flags: unchecked

; 注册表 - 开机启动
[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "Moyu"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: startupicon

; 安装完成后运行
[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "立即运行 {#MyAppName}"; Flags: nowait postinstall skipifsilent
