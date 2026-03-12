; Instalador FM26 Player Export v1.0.1

#define AppName      "FM26 Player Export"
#define AppVersion   "1.0.1"
#define AppPublisher "vintesetFM"
#define AppURL       "https://youtube.com/@vintesetFM"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisherURL={#AppURL}
DefaultDirName={code:GetFM26Path}
DisableDirPage=no
DirExistsWarning=no
OutputBaseFilename=FM26PlayerExport-v{#AppVersion}-Installer
OutputDir=.\dist
Compression=lzma2/ultra
SolidCompression=yes
WizardStyle=modern
DisableProgramGroupPage=yes
CreateAppDir=no
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]

Source: "build\winhttp.dll"; DestDir: "{app}"; Flags: uninsneveruninstall; Check: not BepInExAlreadyInstalled
Source: "build\doorstop_config.ini"; DestDir: "{app}"; Flags: uninsneveruninstall; Check: not BepInExAlreadyInstalled
Source: "build\BepInEx\*"; DestDir: "{app}\BepInEx"; Flags: recursesubdirs createallsubdirs uninsneveruninstall; Check: not BepInExAlreadyInstalled
Source: "build\plugin\FM26PlayerExport.dll"; DestDir: "{app}\BepInEx\plugins\FM26PlayerExport"; Flags: ignoreversion
Source: "TUTORIAL_INSTALACAO.txt"; DestDir: "{app}\BepInEx\plugins\FM26PlayerExport"; Flags: ignoreversion

[UninstallDelete]
Type: filesandordirs; Name: "{app}\BepInEx\plugins\FM26PlayerExport"

[Run]

; Adiciona excecao no Windows Defender automaticamente
Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -Command ""Add-MpPreference -ExclusionPath '{app}' -ErrorAction SilentlyContinue"""; Flags: runhidden waituntilterminated; StatusMsg: "Configurando excecao no Windows Defender..."

; Abre tutorial
Filename: "notepad.exe"; Parameters: "{app}\BepInEx\plugins\FM26PlayerExport\TUTORIAL_INSTALACAO.txt"; Description: "Abrir tutorial de uso"; Flags: postinstall skipifsilent nowait

[Code]

function GetSteamInstallPath(): String;
var
  Path: String;
begin
  Result := '';
  if RegQueryStringValue(HKLM, 'SOFTWARE\WOW6432Node\Valve\Steam', 'InstallPath', Path) then
    Result := Path
  else if RegQueryStringValue(HKLM, 'SOFTWARE\Valve\Steam', 'InstallPath', Path) then
    Result := Path
  else if RegQueryStringValue(HKCU, 'SOFTWARE\Valve\Steam', 'SteamPath', Path) then
    Result := Path;
end;

function FM26ExistsIn(Dir: String): Boolean;
begin
  Result := FileExists(Dir + '\Football Manager 26\fm.exe') or
            FileExists(Dir + '\Football Manager 26\Football Manager 26.exe');
end;

function FindFM26(): String;
var
  SteamPath: String;
  TestPath: String;
  i: Integer;
  CommonPaths: array[0..7] of String;
begin
  Result := '';
  SteamPath := GetSteamInstallPath();

  if SteamPath <> '' then
  begin
    TestPath := SteamPath + '\steamapps\common';
    if FM26ExistsIn(TestPath) then
    begin
      Result := TestPath + '\Football Manager 26';
      Exit;
    end;
  end;

  CommonPaths[0] := 'C:\Program Files (x86)\Steam\steamapps\common';
  CommonPaths[1] := 'C:\Program Files\Steam\steamapps\common';
  CommonPaths[2] := 'C:\SteamLibrary\steamapps\common';
  CommonPaths[3] := 'D:\Steam\steamapps\common';
  CommonPaths[4] := 'D:\SteamLibrary\steamapps\common';
  CommonPaths[5] := 'E:\Steam\steamapps\common';
  CommonPaths[6] := 'E:\SteamLibrary\steamapps\common';
  CommonPaths[7] := 'F:\SteamLibrary\steamapps\common';

  for i := 0 to 7 do
  begin
    if FM26ExistsIn(CommonPaths[i]) then
    begin
      Result := CommonPaths[i] + '\Football Manager 26';
      Exit;
    end;
  end;
end;

function GetFM26Path(Param: String): String;
var
  Found: String;
begin
  Found := FindFM26();
  if Found <> '' then
    Result := Found
  else
    Result := 'C:\Program Files (x86)\Steam\steamapps\common\Football Manager 26';
end;

function BepInExAlreadyInstalled(): Boolean;
begin
  Result := FileExists(WizardDirValue() + '\winhttp.dll') or
            DirExists(WizardDirValue() + '\BepInEx\core');
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  InstallDir: String;
begin
  Result := True;
  if CurPageID = wpSelectDir then
  begin
    InstallDir := WizardDirValue();
    if not (FileExists(InstallDir + '\fm.exe') or
            FileExists(InstallDir + '\Football Manager 26.exe')) then
    begin
      if MsgBox('A pasta selecionada nao parece ser a pasta do Football Manager 26.' + #13#10 +
                'Tem certeza que deseja continuar?',
                mbConfirmation, MB_YESNO) = IDNO then
        Result := False;
    end;
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';
  if not BepInExAlreadyInstalled() then
  begin
    MsgBox(
      'IMPORTANTE - LEIA ANTES DE CONTINUAR' + #13#10 + #13#10 +
      'O BepInEx sera instalado pela primeira vez.' + #13#10 + #13#10 +
      'Na PRIMEIRA vez que voce abrir o FM26 apos a instalacao:' + #13#10 + #13#10 +
      '1. Uma tela preta (console) vai aparecer automaticamente' + #13#10 +
      '2. Aguarde de 2 a 5 minutos enquanto os arquivos sao gerados' + #13#10 +
      '3. O jogo vai abrir normalmente em seguida' + #13#10 + #13#10 +
      'NAO feche a tela preta! Isso e normal e acontece apenas uma vez.' + #13#10 + #13#10 +
      'Se a tela preta NAO aparecer, veja o tutorial para instrucoes' + #13#10 +
      'de como adicionar excecao no antivirus.',
      mbInformation, MB_OK);
  end;
end;

procedure InitializeWizard();
begin
  WizardForm.DirEdit.Text := GetFM26Path('');
end;
