; PdfWordStudio - Inno Setup 安装脚本
; 适用于 Windows 10/11 x86/x64 (自包含发布)

#define MyAppName "PDF 转 Word"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "PdfWordStudio"
#define MyAppExeName "PdfWordStudio.exe"

[Setup]
AppId={{8B9F3C2A-1D5E-4A7B-9C6D-3E2F1A0B5C8D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\PdfWordStudio
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=Output
OutputBaseFilename=PdfWordStudio_Setup_{#MyAppVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
DisableProgramGroupPage=no
PrivilegesRequiredOverridesAllowed=dialog
SetupIconFile=..\src\PdfWordStudio\Resources\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
ShowLanguageDialog=no
LanguageDetectionMethod=none

; 最低 Windows 10
MinVersion=10.0.10240

; x64 优先，兼容 x86
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "chinesesimplified"; MessagesFile: "Languages\ChineseSimplified.isl"

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式(&D)"; GroupDescription: "快捷方式："; Flags: checkedonce

[Files]
; x64 自包含发布
Source: "..\build\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: Is64BitInstallMode
; x86 自包含发布
Source: "..\build\win-x86\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: not Is64BitInstallMode

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\卸载 {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "运行 {#MyAppName}"; Flags: postinstall nowait skipifsilent shellexec

[UninstallRun]
Filename: "{app}\{#MyAppExeName}"; RunOnceId: "KillApp"; Flags: nowait skipifdoesntexist

[Code]
// 安装前检查 .NET 10 Desktop Runtime（如果是非自包含版本）
// 自包含版本不需要此项检查
function InitializeSetup: Boolean;
begin
  Result := True;
end;
