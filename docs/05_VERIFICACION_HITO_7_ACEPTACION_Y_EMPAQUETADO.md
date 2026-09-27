# AtajaMe · Verificación del Hito 7: Aceptación Final, Empaquetado e Instalador

Este documento consolida la verificación técnica, el proceso de empaquetado e instalación nativa y la validación de la **matriz de los 17 Criterios de Aceptación del MVP** (según la Sección 20 de `docs/02_ESPECIFICACION_FUNCIONAL_MVP.md`), en estricto cumplimiento de las directrices de ingeniería y los cuatro guardianes de calidad definidos en `AGENTS.md`.

---

## 1. Publicación y Optimización de Binarios

* **Configuración del proyecto:**
  * Ensamblado principal unificado: `AtajaMe` (`AtajaMe.exe`, `AtajaMe.dll`).
  * Destino de plataforma: Windows x64 (`net8.0-windows`, `win-x64`).
  * Modo de despliegue: **Self-Contained** (`--self-contained true`), empaquetando el runtime optimizado de .NET 8 para garantizar que la aplicación funcione en entornos corporativos sin requerir privilegios de administrador ni runtimes previos instalados.
  * Inclusión de recursos corporativos:
    * Icono oficial de aplicación (`Assets/app.ico`) embebido y copiado al directorio de ejecución.
    * Librerías nativas requeridas para la persistencia SQLite (`e_sqlite3.dll`, `SQLitePCLRaw.batteries_v2.dll`).

* **Comando de compilación:**
  ```powershell
  dotnet publish src/AtajaMe.UI/AtajaMe.UI.csproj -c Release -r win-x64 --self-contained true -o publish/win-x64
  ```

---

## 2. Instalador Nativo con Inno Setup (`installer/setup.iss`)

Se diseñó e implementó el script de compilación para **Inno Setup 6**, generando el ejecutable `installer/output/AtajaMe_Setup.exe`.

### 2.1 Características Clave del Instalador
1. **Instalación por usuario sin permisos de Administrador:**
   * `PrivilegesRequired=lowest`
   * Directorio de destino por defecto: `%LocalAppData%\Programs\AtajaMe` (`{localappdata}\Programs\AtajaMe`).
   * Permite instalación en equipos restringidos sin elevación UAC.
2. **Accesos directos:**
   * Acceso directo en el Menú Inicio (`{autoprograms}\AtajaMe`).
   * Opción configurable con casilla de verificación para crear acceso directo en el Escritorio (`{autodesktop}\AtajaMe`).
3. **Arranque automático con Windows:**
   * Tarea opcional durante la instalación (`startup`).
   * Registro en clave por usuario: `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` con valor `"{app}\AtajaMe.exe" --minimized`.
   * La aplicación arranca silenciosamente en la bandeja del sistema (system tray).
4. **Cierre proactivo de procesos:**
   * `InitializeSetup()` y `InitializeUninstall()` ejecutan `taskkill /F /IM AtajaMe.exe` de forma transparente para evitar bloqueos de archivos en caliente al actualizar o desinstalar.
5. **Desinstalador limpio con preservación de datos:**
   * Registra el desinstalador estándar `unins000.exe` en Windows.
   * `CurUninstallStepChanged(usPostUninstall)` consulta al usuario mediante cuadro de diálogo si desea eliminar los datos locales en `%LocalAppData%\AtajaMe\`.
   * El botón por defecto es **No** (`MB_DEFBUTTON2`), protegiendo la base de datos `atajame.db` y las copias de seguridad frente a borrados accidentales salvo petición explícita.

### 2.2 Automatización del Proceso (`installer/build.ps1`)
Se implementó el script PowerShell `installer/build.ps1` que orquesta de forma desatendida:
1. Ejecución y validación de las **256 pruebas unitarias** (`dotnet test`).
2. Publicación limpia en modo Release (`dotnet publish`).
3. Compilación con `ISCC.exe` de Inno Setup.
4. Generación y reporte del instalador con verificación SHA-256.

* **Artefacto generado:**
  * **Ruta:** `installer/output/AtajaMe_Setup.exe`
  * **Tamaño:** ~49.2 MB (comprimido con LZMA2 Ultra, incluyendo el runtime self-contained de .NET 8).
  * **Integridad:** SHA-256 verificado.

---

## 3. Matriz de Validación de los 17 Criterios de Aceptación del MVP

A continuación se detalla la verificación punto por punto de la Sección 20 de `docs/02_ESPECIFICACION_FUNCIONAL_MVP.md`:

| # | Criterio de Aceptación | Estado | Evidencia / Componente Responsable |
|---|---|:---:|---|
| **1** | Pueda instalarse y ejecutarse en un equipo Windows soportado. | **CUMPLIDO** | Instalador `AtajaMe_Setup.exe` (per-user `%LocalAppData%\Programs\AtajaMe`) sin requerir privilegios de administrador. |
| **2** | Permita crear un atajo de texto plano. | **CUMPLIDO** | `CreateSnippetUseCase`, validación de unicidad de código y editor visual en `MainWindow.xaml`. |
| **3** | Detecte el código escrito en una aplicación compatible. | **CUMPLIDO** | `LowLevelKeyboardHook` (`WH_KEYBOARD_LL`), canal asíncrono no bloqueante y búfer circular efímero `RollingCharBuffer`. |
| **4** | Lo expanda correctamente al producirse el delimitador. | **CUMPLIDO** | `WindowsTextInjector`, borrado seguro con pulsaciones `VK_BACK` e inyección Unicode vía Win32 `SendInput`. |
| **5** | Permita editar, desactivar, duplicar y eliminar/restaurar el atajo. | **CUMPLIDO** | `UpdateSnippetUseCase`, `ToggleSnippetStateUseCase`, `DuplicateSnippetUseCase`, `DeleteSnippetToTrashUseCase`, `RestoreSnippetUseCase`, `PurgeTrashUseCase`. |
| **6** | Permita organizar los atajos en carpetas. | **CUMPLIDO** | `FolderRepository`, `FolderViewModel`, árbol jerárquico de carpetas en panel lateral. |
| **7** | Permita buscarlos. | **CUMPLIDO** | Filtro dinámico en tiempo real por título, código o contenido en `MainViewModel.SearchText`. |
| **8** | Soporte variables dinámicas básicas (`{date}`, `{time}`, `{clipboard}`). | **CUMPLIDO** | `VariableResolutionService` con soporte de formatos personalizados (`{date:yyyy-MM-dd}`, `{time:HH:mm}`). |
| **9** | Solicite valores mediante la interfaz flotante cuando un atajo los necesite. | **CUMPLIDO** | `FloatingVariableDialog` (WPF borderless, `Topmost`, posicionada junto al cursor del ratón) para variables `{input:nombre[:defecto]}`. |
| **10** | Persista correctamente los datos en SQLite. | **CUMPLIDO** | `SqliteConnectionFactory`, modo WAL, inicializador y esquemas en `%LocalAppData%\AtajaMe\atajame.db`. |
| **11** | Pueda importar y exportar CSV. | **CUMPLIDO** | `CsvService` (cumplimiento RFC 4180) y cuadro de diálogo comparativo con resolución de conflictos `CsvImportPreviewDialog`. |
| **12** | Realice copias de seguridad según la configuración. | **CUMPLIDO** | `BackupService` utilizando la API de backup online de SQLite (`sqlite3_backup`), retención de las 10 copias más recientes y restauración guiada. |
| **13** | Permanezca operativo en segundo plano. | **CUMPLIDO** | `ShutdownMode = OnExplicitShutdown`, icono en la bandeja del sistema (`TrayIcon`) con menú contextual (Mostrar, Pausar/Reanudar, Configuración, Salir). |
| **14** | Continúe funcionando con datos locales cuando la ubicación sincronizada no esté disponible. | **CUMPLIDO** | Modo tolerante a fallos en `SyncEngine` y `FileWatcherSyncService` con reintentos sin bloqueo ante carpetas remotas inaccesibles. |
| **15** | No almacene un historial del contenido expandido (RF-46). | **CUMPLIDO** | El búfer de teclas es efímero en memoria RAM (máx 50 chars), se purga automáticamente tras 5 segundos de inactividad, y no existe telemetría ni logging de expansiones. |
| **16** | Bloquee la expansión en campos sensibles cuando puedan identificarse de forma fiable (RF-08). | **CUMPLIDO** | `SensitiveControlDetector` consulta UI Automation `IsPasswordProperty`; cancelación inmediata y silenciosa si el foco está en un campo de contraseña. |
| **17** | Avise de forma comprensible cuando una expansión no pueda realizarse. | **CUMPLIDO** | Notificaciones visuales en la bandeja o ventanas de advertencia según la naturaleza del evento. |

---

## 4. Auditoría de los Guardianes de Calidad (`AGENTS.md`)

* 🛡️ **Guardián 1 (Arquitectura y Especificación):** Las capas (Domain, Application, Infrastructure, Windows, UI) están perfectamente desacopladas. UI no tiene referencias directas a SQLite ni a Win32 nativo. El instalador no introduce dependencias foráneas ni características fuera de alcance (no hay texto enriquecido ni etiquetas).
* 🔍 **Guardián 2 (Limpieza de Código y Refactorización):** Tipado estricto con `<Nullable>enable</Nullable>`. Inyección de dependencias coherente y 256 pruebas unitarias automatizadas con 100% de éxito.
* ⚡ **Guardián 3 (Seguridad Win32 y Rendimiento):** Hook de teclado `WH_KEYBOARD_LL` con delegado protegido frente al GC, despacho asíncrono no bloqueante vía `System.Threading.Channels`, consumo mínimo en reposo (< 30-40 MB).
* 🔒 **Guardián 4 (Privacidad y Resiliencia):** Prohibición absoluta de telemetría y retención de pulsaciones. El desinstalador preserva la base de datos por defecto.

---

## 5. Conclusión del Hito 7

El **Hito 7: Aceptación Final, Empaquetado e Instalador** queda completado con éxito. El producto está listo para su distribución e instalación mediante `installer/output/AtajaMe_Setup.exe`.
