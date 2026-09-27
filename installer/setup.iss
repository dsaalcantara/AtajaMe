; Script de Inno Setup para AtajaMe
; Genera el instalador nativo AtajaMe_Setup.exe en la carpeta installer/output/
; Cumple con las directrices de AGENTS.md y los requisitos funcionales del MVP

#define MyAppName "AtajaMe"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "AtajaMe"
#define MyAppURL "https://github.com/dsaalcantara/AtajaMe"
#define MyAppExeName "AtajaMe.exe"

[Setup]
AppId={{A7A3A3E0-4D56-4740-B93E-4E7B442A9B10}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
; Instalación por usuario sin requerir privilegios de Administrador (evita bloqueos corporativos)
PrivilegesRequired=lowest
OutputDir=output
OutputBaseFilename=AtajaMe_Setup
SetupIconFile=..\src\AtajaMe.UI\Assets\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startup"; Description: "Iniciar AtajaMe automáticamente al iniciar sesión en Windows (en segundo plano)"; GroupDescription: "Inicio de Windows:"; Flags: checkedonce

[Files]
Source: "..\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
; Inicio automático en Windows en segundo plano / minimizado (RF-33)
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppName}"; ValueData: """{app}\{#MyAppExeName}"" --minimized"; Flags: uninsdeletevalue; Tasks: startup

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
function InitializeSetup(): Boolean;
var
  ErrorCode: Integer;
begin
  Result := True;
  // Detener AtajaMe si ya está en ejecución antes de actualizar/instalar
  Exec('taskkill.exe', '/F /IM ' + '{#MyAppExeName}', '', SW_HIDE, ewWaitUntilTerminated, ErrorCode);
end;

function InitializeUninstall(): Boolean;
var
  ErrorCode: Integer;
begin
  Result := True;
  // Detener AtajaMe si está en ejecución antes de proceder con la desinstalación
  Exec('taskkill.exe', '/F /IM ' + '{#MyAppExeName}', '', SW_HIDE, ewWaitUntilTerminated, ErrorCode);
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  DataDir: String;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    // Preservar por defecto la base de datos de atajos en %LocalAppData%\AtajaMe\
    // salvo que el usuario confirme explícitamente su eliminación
    DataDir := ExpandConstant('{localappdata}\{#MyAppName}');
    if DirExists(DataDir) then
    begin
      if MsgBox('¿Desea eliminar también la base de datos de atajos y la configuración de ' + '{#MyAppName}?' + #13#10 + #13#10 +
                'Ubicación: ' + DataDir + #13#10 + #13#10 +
                'Si planea reinstalar AtajaMe en el futuro, seleccione "No" para conservar sus datos.', 
                mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES then
      begin
        DelTree(DataDir, True, True, True);
      end;
    end;
  end;
end;
