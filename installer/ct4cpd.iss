; Inno Setup Script for ct4cpd (PowerToys Command Palette Plugin)
; SEE THE DOCUMENTATION FOR DETAILS ON CREATING INNO SETUP SCRIPT FILES!

#define MyAppName "ct4cpd"
#define MyAppVersion "1.1.0"
#define MyAppPublisher "A Lone Developer"
#define MyAppExeName "ct4cpd.exe"

[Setup]
AppId={{d2541718-87a7-4bbc-aec5-063cfa836158}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputBaseFilename=ct4cpd_Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin

[Languages]
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"

[Files]
; ビルド出力ファイル一式（不要な publish フォルダ・pdb・中間ファイルを除外）
Source: "..\ct4cpd\bin\x64\Release\net9.0-windows10.0.26100.0\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion; Excludes: "publish\*,*.pdb,*.build.appxrecipe"
; Assets フォルダを確実に同梱
Source: "..\ct4cpd\bin\x64\Release\net9.0-windows10.0.26100.0\win-x64\Assets\*"; DestDir: "{app}\Assets"; Flags: ignoreversion recursesubdirs createallsubdirs

[Run]
; インストール完了時に PowerToys Command Palette へ自動登録
Filename: "powershell.exe"; \
  Parameters: "-NoProfile -ExecutionPolicy Bypass -Command ""Get-AppxPackage *ct4cpd* | Remove-AppxPackage -ErrorAction SilentlyContinue; Add-AppxPackage -Register '{app}\AppxManifest.xml' -ForceApplicationShutdown"""; \
  StatusMsg: "PowerToys Command Palette にプラグインを登録しています..."; \
  Flags: runhidden waituntilterminated

[UninstallRun]
; アンインストール時に Command Palette の登録を解除
Filename: "powershell.exe"; \
  Parameters: "-NoProfile -ExecutionPolicy Bypass -Command ""Get-AppxPackage *ct4cpd* | Remove-AppxPackage -ErrorAction SilentlyContinue"""; \
  Flags: runhidden waituntilterminated; \
  RunOnceId: "RemoveCt4cpdAppx"
