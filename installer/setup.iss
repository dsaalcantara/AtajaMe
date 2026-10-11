; Script de Inno Setup para AtajaMe
; Genera el instalador nativo AtajaMe_Setup.exe en la carpeta installer/output/
; Cumple con las directrices de AGENTS.md y los requisitos funcionales del MVP

#define MyAppName "AtajaMe"
; La versión se inyecta desde build.ps1 o GitHub Actions (/DMyAppVersion=X.Y.Z).
; Este valor solo se usa como respaldo si se compila el .iss manualmente.
#ifndef MyAppVersion
  #define MyAppVersion "1.1.0"
#endif
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


[Files]
Source: "..\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"; Check: ShouldCreateDesktopIcon

[Registry]
; Inicio automático en Windows en segundo plano / minimizado (RF-33)
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppName}"; ValueData: """{app}\{#MyAppExeName}"" --minimized"; Flags: uninsdeletevalue; Check: ShouldEnableStartup

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
var
  TasksCustomPage: TWizardPage;
  DesktopCheckBox: TNewCheckBox;
  StartupCheckBox: TNewCheckBox;

function ShouldCreateDesktopIcon(): Boolean;
begin
  Result := DesktopCheckBox.Checked;
end;

function ShouldEnableStartup(): Boolean;
begin
  Result := StartupCheckBox.Checked;
end;

procedure InitializeWizard();
var
  LblDesc: TNewStaticText;
  LblGroupIcons: TNewStaticText;
  LblGroupStartup: TNewStaticText;
  CurrentTop: Integer;
begin
  // Creamos la página personalizada de tareas usando controles independientes TNewCheckBox.
  // Esto sustituye al componente TNewCheckListBox de Inno Setup, cuyo dibujado interno
  // recorta el borde superior de las casillas temáticas en Windows 11 con escalado DPI.
  TasksCustomPage := CreateCustomPage(wpSelectDir,
    'Seleccione las Tareas Adicionales',
    '¿Qué tareas adicionales deben realizarse?');

  CurrentTop := ScaleY(8);

  LblDesc := TNewStaticText.Create(TasksCustomPage);
  LblDesc.Parent := TasksCustomPage.Surface;
  LblDesc.Left := 0;
  LblDesc.Top := CurrentTop;
  LblDesc.Width := TasksCustomPage.SurfaceWidth;
  LblDesc.WordWrap := True;
  LblDesc.Caption := 'Seleccione las tareas adicionales que desea que se realicen durante la instalación de AtajaMe y, a continuación, haga clic en Siguiente.';
  LblDesc.AdjustHeight;

  CurrentTop := CurrentTop + LblDesc.Height + ScaleY(20);

  LblGroupIcons := TNewStaticText.Create(TasksCustomPage);
  LblGroupIcons.Parent := TasksCustomPage.Surface;
  LblGroupIcons.Left := 0;
  LblGroupIcons.Top := CurrentTop;
  LblGroupIcons.Caption := 'Accesos directos adicionales:';
  LblGroupIcons.Font.Style := [fsBold];

  CurrentTop := CurrentTop + LblGroupIcons.Height + ScaleY(8);

  DesktopCheckBox := TNewCheckBox.Create(TasksCustomPage);
  DesktopCheckBox.Parent := TasksCustomPage.Surface;
  DesktopCheckBox.Left := ScaleX(10);
  DesktopCheckBox.Top := CurrentTop;
  DesktopCheckBox.Width := TasksCustomPage.SurfaceWidth - ScaleX(10);
  DesktopCheckBox.Height := ScaleY(24);
  DesktopCheckBox.Caption := 'Crear un acceso directo en el escritorio';
  DesktopCheckBox.Checked := False;

  CurrentTop := CurrentTop + DesktopCheckBox.Height + ScaleY(16);

  LblGroupStartup := TNewStaticText.Create(TasksCustomPage);
  LblGroupStartup.Parent := TasksCustomPage.Surface;
  LblGroupStartup.Left := 0;
  LblGroupStartup.Top := CurrentTop;
  LblGroupStartup.Caption := 'Inicio de Windows:';
  LblGroupStartup.Font.Style := [fsBold];

  CurrentTop := CurrentTop + LblGroupStartup.Height + ScaleY(8);

  StartupCheckBox := TNewCheckBox.Create(TasksCustomPage);
  StartupCheckBox.Parent := TasksCustomPage.Surface;
  StartupCheckBox.Left := ScaleX(10);
  StartupCheckBox.Top := CurrentTop;
  StartupCheckBox.Width := TasksCustomPage.SurfaceWidth - ScaleX(10);
  StartupCheckBox.Height := ScaleY(24);
  StartupCheckBox.Caption := 'Iniciar AtajaMe con Windows (en segundo plano)';
  StartupCheckBox.Checked := True;
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  // Omitimos la página wpSelectTasks original de Inno Setup
  if PageID = wpSelectTasks then
    Result := True
  else
    Result := False;
end;

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
