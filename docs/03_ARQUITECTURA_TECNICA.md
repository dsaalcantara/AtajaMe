# 03 · Arquitectura Técnica

## 1. Identificación y Alcance

* **Producto:** AtajaMe
* **Documento:** Arquitectura Técnica del Sistema
* **Versión:** 1.0
* **Estado:** Aprobado para construcción
* **Prevalencia:** Este documento responde a los requisitos de `docs/02_ESPECIFICACION_FUNCIONAL_MVP.md` y a las decisiones consolidadas de `docs/01_DECISIONES_FUNCIONALES.md`.

---

## 2. Decisiones Tecnológicas Fundamentales

| Componente / Aspecto | Tecnología Seleccionada | Justificación Técnica |
| :--- | :--- | :--- |
| **Plataforma / Runtime** | **C# / .NET 8 (LTS)** | Soporte oficial a largo plazo, máximo rendimiento en Windows x64, tipado nulo estricto (`<Nullable>enable</Nullable>`) y librerías modernas de asincronía. |
| **Framework de UI** | **WPF (.NET 8) + CommunityToolkit.Mvvm** | Arquitectura MVVM desacoplada, soporte maduro para bandeja del sistema (*System Tray*), acceso directo a manejadores Win32 (`HWND`), consumo de memoria ultra bajo en reposo (< 30 MB) y compatibilidad total con Windows 10 y 11. |
| **Captura de Teclado** | **Win32 Hook `WH_KEYBOARD_LL` + `Channel<T>`** | Hook de bajo nivel procesado en milisegundos mediante un canal asíncrono no bloqueante, aislando el hilo del sistema de Windows. |
| **Inyección de Texto** | **Win32 `SendInput` (Unicode)** | Simulación nativa por eventos UTF-16 (`KEYEVENTF_UNICODE`), inmune a la distribución de teclado y respetando estrictamente el portapapeles del usuario. |
| **Seguridad de Foco** | **UI Automation (`AutomationElement.IsPasswordProperty`)** | Detección fiable y estándar de campos de contraseña en Windows para cancelar la expansión de forma silenciosa e inmediata. |
| **Persistencia Local** | **SQLite + Dapper / SQL Script Migrations** | Base de datos ligera, local-first, sin servidor, configurada en modo WAL (*Write-Ahead Logging*) para máxima concurrencia y velocidad instantánea. |
| **Sincronización** | **Intercambio desacoplado por Snapshots / JSON** | Evita bloqueos en caliente de SQLite en carpetas en la nube (Google Drive/OneDrive). Detección de cambios mediante `FileSystemWatcher` y resolución caso por caso de conflictos. |
| **Empaquetado** | **Inno Setup (Instalador .exe)** | Instalador limpio por usuario (`%LocalAppData%\Programs\AtajaMe`) que no requiere permisos de Administrador y gestiona el inicio automático con Windows. |

---

## 3. Arquitectura del Sistema (Clean Architecture)

La solución se estructura en capas concéntricas desacopladas mediante inyección de dependencias (`Microsoft.Extensions.DependencyInjection`).

```
                    ┌───────────────────────────────┐
                    │          AtajaMe.UI           │ (WPF / MVVM / Tray Icon)
                    └───────┬───────────────┬───────┘
                            │               │
                            ▼               ▼
        ┌────────────────────────┐    ┌────────────────────────┐
        │  AtajaMe.Infrastructure │    │    AtajaMe.Windows     │ (Win32 Hooks,
        │  (SQLite, Backup, Sync)│    │    SendInput, UI Auto) │
        └───────────┬────────────┘    └───────────┬────────────┘
                    │                             │
                    ▼                             ▼
        ┌──────────────────────────────────────────────────────┐
        │                 AtajaMe.Application                  │ (Casos de uso,
        │           (SnippetEngine, VariableParser)            │  Orquestación)
        └──────────────────────────┬───────────────────────────┘
                                   │
                                   ▼
        ┌──────────────────────────────────────────────────────┐
        │                    AtajaMe.Domain                    │ (Entidades puras,
        │               (Snippet, Folder, Enums)               │  Interfaces base)
        └──────────────────────────────────────────────────────┘
```

### 3.1 Proyectos de la Solución

1. **`AtajaMe.Domain`**:
   * Contiene las entidades puras del negocio: `Snippet`, `Folder`, `DynamicVariable`, `SyncConflict`.
   * Enums: `DelimiterType`, `CaseSensitivity`, `ConflictResolution`.
   * Interfaces del repositorio (`ISnippetRepository`, `IFolderRepository`).
   * Cero dependencias externas o del sistema operativo.
2. **`AtajaMe.Application`**:
   * Orquestación de casos de uso: creación, edición, borrado a papelera, purga y restauración de atajos.
   * `SnippetExpansionEngine`: Lógica de coincidencia de atajos y delimitadores.
   * `VariableResolutionService`: Sustitución de `{date}`, `{time}` y orquestación de variables interactivas `{input:nombre}`.
   * `CsvImportExportService`: Parseo y serialización de atajos en CSV.
   * `BackupService`: Ejecución programada y retención de copias de seguridad.
   * `SyncEngine`: Algoritmo de detección de conflictos y fusión de cambios.
3. **`AtajaMe.Windows`**:
   * Implementación de bajo nivel de interfaces de la aplicación:
     * `IKeyboardHook`: Instalación de `WH_KEYBOARD_LL`, canal de eventos y búfer circular.
     * `ITextInjector`: Envío de `VK_BACK` y cadenas Unicode vía `SendInput`.
     * `ISensitiveControlDetector`: Consulta a UI Automation para verificar `IsPassword`.
     * `IFocusTracker`: Verificación de ventana/control activo para abortar en caso de pérdida de foco.
4. **`AtajaMe.Infrastructure`**:
   * `SqliteDbContext` / Conexión SQLite con modo WAL y creación/migración de tablas.
   * Repositorios Dapper de persistencia.
   * `FileWatcherSyncService`: Monitor de cambios en la carpeta compartida seleccionada.
   * `DiskBackupStorage`: Almacenamiento y rotación de archivos de backup `.sqlite`.
5. **`AtajaMe.UI`**:
   * Aplicación WPF en segundo plano (`ShutdownMode = OnExplicitShutdown`).
   * `TrayIconService`: Icono en la barra de tareas con menú contextual (Abrir, Desactivar atajos, Salir).
   * Vistas y ViewModels:
     * `MainWindow` (Gestión de carpetas, lista de atajos, búsqueda, favoritos, papelera, editor).
     * `SettingsWindow` (Configuración de sincronización, copias de seguridad, exclusiones, inicio con Windows).
     * `InteractiveVariableWindow` (Ventana flotante compacta junto al cursor para solicitar variables).
     * `ConflictResolutionWindow` (Diálogo para resolver conflictos de sincronización).
6. **`AtajaMe.Tests`**:
   * Pruebas unitarias de casos de uso, motor de coincidencias, variables y serialización CSV.

---

## 4. Motor de Captura y Expansión de Texto

### 4.1 Flujo de Eventos de Teclado y Búfer Efímero

```
[Usuario teclea]
       │
       ▼
[WH_KEYBOARD_LL Hook] ──( < 0.5 ms )──> [Channel<RawKeyEvent>.TryWrite()]
       │                                              │
       ▼                                              ▼
[Retorno inmediato al OS]                      [Background Worker Task]
                                                      │
                                                      ▼
                                            [Actualizar Búfer Circular]
                                            (En memoria, máx. 50 caracteres)
                                                      │
                                                      ├── ¿Es Delimitador? (Espacio, Tab)
                                                      │           │
                                                      │           ├── NO ──> Continuar esperando
                                                      │           │
                                                      │           └── SÍ ──> Buscar coincidencia de código
                                                      │
                                                      ▼
                                       ¿Coincide con Snippet Activo?
                                                      │
                                                      ├── NO ──> Purgar búfer
                                                      │
                                                      └── SÍ ──> Comprobar Reglas de Seguridad:
                                                                 1. ¿App Excluida? ──> Cancelar
                                                                 2. ¿Campo Sensible / Password? ──> Cancelar
                                                                 3. ¿Mantiene el foco? ──> Cancelar si cambió
                                                                 4. ¿Requiere inputs interactivos?
                                                                      ├─ SÍ ─> Abrir Popup Flotante
                                                                      └─ NO ─> Proceder a Inyección
```

### 4.2 Inyección mediante `SendInput`
Para expandir un atajo (ej. código `bd` de 2 caracteres + 1 espacio):
1. **Borrado del disparador**:
   * Se calculan los caracteres a borrar: $N = \text{longitud}(c\acute{o}digo) + 1$.
   * Se envían $N$ pulsaciones de tecla `VK_BACK` mediante un único paquete `SendInput`.
2. **Inyección del texto de reemplazo**:
   * El texto resultante (tras sustituir variables) se convierte en un array de `INPUT` con `type = INPUT_KEYBOARD`, `dwFlags = KEYEVENTF_UNICODE`, `wScan = char` y `wVk = 0`.
   * Se envía a la aplicación de destino sin tocar el portapapeles de Windows (`Clipboard`).

### 4.3 Privacidad Estricta (RF-08 y RF-46)
* El búfer circular solo almacena caracteres de texto imprimibles recientes (hasta 50).
* Se borra automáticamente:
  * Tras cada delimitador no coincidente.
  * Tras detectar un cambio de ventana activa (`EVENT_SYSTEM_FOREGROUND`).
  * Tras un periodo de inactividad de 5 segundos.
* **Nunca** se guardan pulsaciones en disco, variables estáticas persistentes ni bases de datos.
* Si `UIAutomation.IsPasswordProperty == true`, el hook ignora de inmediato las teclas y limpia el búfer.

---

## 5. Modelo Físico de Datos (SQLite)

La base de datos local residirá en:  
`%LocalAppData%\AtajaMe\atajame.db`

### 5.1 Esquema de Tablas

```sql
-- Carpetas (Jerarquía de hasta 3 niveles)
CREATE TABLE Folders (
    Id TEXT PRIMARY KEY,               -- GUID v4
    ParentId TEXT NULL,                -- GUID de la carpeta padre
    Name TEXT NOT NULL,                -- Nombre visible
    SortOrder INTEGER NOT NULL DEFAULT 0,
    CreatedAt TEXT NOT NULL,           -- ISO8601 UTC
    UpdatedAt TEXT NOT NULL,           -- ISO8601 UTC
    FOREIGN KEY (ParentId) REFERENCES Folders(Id) ON DELETE CASCADE
);

-- Atajos de texto
CREATE TABLE Snippets (
    Id TEXT PRIMARY KEY,               -- GUID v4
    FolderId TEXT NULL,                -- Carpeta contenedora
    Code TEXT NOT NULL UNIQUE,         -- Código de disparo (ej. "bd")
    Name TEXT NOT NULL,                -- Nombre descriptivo
    Content TEXT NOT NULL,             -- Texto plano con tags de variable
    CaseSensitive INTEGER NOT NULL DEFAULT 0, -- 0: false, 1: true
    IsActive INTEGER NOT NULL DEFAULT 1,
    IsFavorite INTEGER NOT NULL DEFAULT 0,
    IsInTrash INTEGER NOT NULL DEFAULT 0,
    DeletedAt TEXT NULL,               -- Fecha en que se movió a papelera
    SortOrder INTEGER NOT NULL DEFAULT 0,
    UsageCount INTEGER NOT NULL DEFAULT 0,
    LastUsedAt TEXT NULL,              -- Para lista de recientes (solo metadato)
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    FOREIGN KEY (FolderId) REFERENCES Folders(Id) ON DELETE SET NULL
);

-- Aplicaciones excluidas
CREATE TABLE ExcludedApplications (
    Id TEXT PRIMARY KEY,
    ProcessName TEXT NOT NULL UNIQUE,  -- ej. "KeePass.exe", "cmd.exe"
    Description TEXT NULL,
    CreatedAt TEXT NOT NULL
);

-- Configuración general de la aplicación
CREATE TABLE AppSettings (
    Key TEXT PRIMARY KEY,
    Value TEXT NOT NULL
);

-- Metadatos de Sincronización
CREATE TABLE SyncMetadata (
    MachineId TEXT PRIMARY KEY,        -- GUID identificador único de este equipo
    LastSyncTimestamp TEXT NOT NULL,   -- Última fecha UTC procesada
    LastSyncHash TEXT NOT NULL         -- Hash SHA-256 del último paquete aplicado
);
```

### 5.2 Optimización y Rendimiento
* En la apertura de la conexión:
  ```sql
  PRAGMA journal_mode = WAL;
  PRAGMA synchronous = NORMAL;
  PRAGMA foreign_keys = ON;
  PRAGMA busy_timeout = 5000;
  ```

---

## 6. Sincronización Segura y Detección de Conflictos

### 6.1 Problema con SQLite en la Nube
Colocar directamente un archivo `.db` de SQLite en Google Drive o OneDrive bloquea el archivo (*file lock*) y genera archivos duplicados (*ej. atajame-DESKTOP-conflict.db*), pudiendo corromper la base de datos.

### 6.2 Solución: Paquete de Sincronización Desacoplado (*Sync Snapshot*)
1. La base de datos SQLite siempre es local e independiente en cada máquina.
2. En la carpeta compartida configurada por el usuario (ej. `G:\Mi unidad\AtajaMeSync\`), la aplicación interactúa con un archivo de sincronización:
   * `sync_manifest.json`: Contiene el listado de atajos y carpetas con sus identificadores (`Id`), marcas temporales (`UpdatedAt`) y firmas hash.
3. **Flujo de Exportación Local**:
   * Cuando el usuario crea, edita o elimina un atajo, la aplicación actualiza su SQLite local y escribe el nuevo estado en el archivo de sincronización compartido.
4. **Flujo de Detección e Importación Remota**:
   * Un `FileSystemWatcher` vigila la carpeta compartida.
   * Cuando detecta una modificación externa de `sync_manifest.json`, compara los registros:
     * Si un atajo remoto tiene `UpdatedAt` más reciente y no ha sido modificado localmente desde la última sincronización $\rightarrow$ Se actualiza la base de datos local automáticamente.
     * Si el atajo remoto y el local tienen modificaciones posteriores al último sync con contenidos distintos $\rightarrow$ Se genera un **Conflicto de Sincronización**.
5. **Resolución de Conflictos (RF-24)**:
   * Se notifica discretamente al usuario.
   * Se abre la ventana de resolución de conflictos, mostrando ambas versiones (local vs. remota) con fecha y contenido para que el usuario decida:
     * *Conservar versión local* (sobrescribe la remota).
     * *Conservar versión remota* (sobrescribe la local).
     * *Crear copia duplicada con código alternativo* (ej. `bd_remoto`).
6. **Resiliencia ante Ausencia de Red (RF-23)**:
   * Si la carpeta compartida no está accesible (disco desconectado o sin conexión), la aplicación continúa operando con su SQLite local de forma normal e inadvertida, emitiendo solo un estado informativo en el menú de la bandeja.

---

## 7. Copias de Seguridad y Retención (RF-28 a RF-32)

* **Generación**:
  * Servicio temporizado en segundo plano (cada $X$ horas / días según configuración).
  * Realiza un volcado consistente mediante la API de Backup de SQLite (`Microsoft.Data.Sqlite.SqliteConnection.BackupDatabase`).
  * Nomenclatura del archivo: `AtajaMe_Backup_YYYYMMDD_HHMMSS.db`.
* **Retención**:
  * Tras cada copia, el servicio inspecciona la carpeta destino.
  * Elimina las copias que superen el límite configurado (por defecto, se conservan las 10 copias más recientes).
* **Restauración**:
  * El usuario puede seleccionar un archivo de copia, visualizar un resumen previo (cantidad de atajos, carpetas) y confirmar la restauración completa sobre la BD local.

---

## 8. Interfaz de Usuario y Experiencia (WPF / MVVM)

### 8.1 Modos de Ejecución
* **Inicio con Windows**: Registro en `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run`.
* **Comportamiento en segundo plano**:
  * Al pulsar el botón de cerrar (`X`) en la ventana principal, la ventana se oculta (`Hide()`) y la aplicación permanece viva en la bandeja del sistema (*Tray Icon*).
  * El icono del System Tray dispone de menú contextual:
    * *Abrir AtajaMe* (abre la ventana principal).
    * *Pausar AtajaMe* (desactiva temporalmente el hook de teclado con icono en gris).
    * *Configuración*.
    * *Salir* (cierre total y liberación de recursos).

### 8.2 Ventana Emergente de Variables Interactivas (RF-12)
* Ventana WPF ligera (`WindowStyle="None"`, `AllowsTransparency="True"`, `Topmost="True"`).
* Posicionamiento inteligente:
  * Mediante Win32 `GetGUIThreadInfo` / `GetCaretPos` para situar el cuadro de texto justo bajo el cursor de escritura.
  * Si no es posible determinar la posición exacta del cursor, se sitúa junto al puntero del ratón.
* Campos solicitados en formulario vertical compacto con foco automático en el primer campo.
* Al pulsar `Enter`, devuelve los valores al motor de expansión y se cierra en menos de 100 ms.

---

## 9. Política de Empaquetado e Instalación

* **Compilación**:
  ```powershell
  dotnet publish src/AtajaMe.UI/AtajaMe.UI.csproj -c Release -r win-x64 --self-contained false -o publish/
  ```
* **Instalador con Inno Setup**:
  * Archivo instalador: `AtajaMe_Setup.exe`.
  * Instalación por usuario: `%LocalAppData%\Programs\AtajaMe` (permite instalar sin permisos de administrador de Windows).
  * Creación de accesos directos en Menú Inicio y opción de "Iniciar automáticamente con Windows".
  * Desinstalador limpio que consulta si se desean conservar los datos locales de atajos o purgarlos.
