# 04 · Plan de Construcción

## 1. Identificación y Enfoque

* **Producto:** AtajaMe
* **Documento:** Plan de Construcción por Hitos Incrementales
* **Versión:** 1.0
* **Referencia:** `docs/02_ESPECIFICACION_FUNCIONAL_MVP.md` y `docs/03_ARQUITECTURA_TECNICA.md`.

El desarrollo se organiza en **7 hitos secuenciales**. Cada hito culmina con una batería de pruebas automatizadas y la validación por parte de los agentes guardianes de calidad definidos en `AGENTS.md`.

---

## 2. Mapa de Hitos

```
Hito 1: Solución y Dominio Puro (Domain Layer)
   │
   ▼
Hito 2: Persistencia SQLite y Casos de Uso Base (Infrastructure + Application)
   │
   ▼
Hito 3: Motor de Coincidencia, Delimitadores y Variables (Application Layer)
   │
   ▼
Hito 4: Servicios Win32 de Teclado, Inyección y Privacidad (Windows Layer)
   │
   ▼
Hito 5: Sincronización Desacoplada, CSV y Backups (Application + Infrastructure)
   │
   ▼
Hito 6: Interfaz Gráfica WPF, System Tray y Ventana Flotante (UI Layer)
   │
   ▼
Hito 7: Validación de Criterios de Aceptación, Empaquetado e Instalador
```

---

## 3. Detalle de Hitos

### 🏗️ Hito 1: Estructura de la Solución y Capa de Dominio
* **Objetivo:** Establecer el esqueleto del proyecto en .NET 8 con tipado estricto y las entidades base del dominio sin dependencias externas.
* **Entregables:**
  * Archivo de solución `AtajaMe.sln`.
  * Proyectos de biblioteca de clases .NET 8 con `<Nullable>enable</Nullable>`:
    * `src/AtajaMe.Domain`
    * `src/AtajaMe.Application`
    * `src/AtajaMe.Infrastructure`
    * `src/AtajaMe.Windows`
    * `src/AtajaMe.UI`
    * `tests/AtajaMe.Tests`
  * Entidades del Dominio:
    * `Snippet` (Id, Code, Name, Content, FolderId, CaseSensitive, IsActive, IsFavorite, IsInTrash, DeletedAt, UsageCount, LastUsedAt).
    * `Folder` (Id, Name, ParentId, SortOrder, validación de máx. 3 niveles).
    * `DynamicVariable` (identificación de tipo: `{date}`, `{time}`, `{input:nombre}`).
    * `ExcludedApplication`.
  * Interfaces de repositorio (`ISnippetRepository`, `IFolderRepository`).
* **Verificación:** Pruebas unitarias iniciales en `AtajaMe.Tests` validando reglas de dominio (ej. longitud máxima de código, no permitir nombres vacíos, límite de profundidad en carpetas).

---

### 💾 Hito 2: Persistencia SQLite y Casos de Uso de Biblioteca
* **Objetivo:** Implementar la base de datos local SQLite con modo WAL y la orquestación para crear, modificar, organizar y enviar a papelera los atajos.
* **Entregables:**
  * `SqliteDatabaseContext` con creación automática de tablas e índices (`Code` único).
  * `SnippetRepository` y `FolderRepository` con consultas parametrizadas optimizadas.
  * Casos de uso en `AtajaMe.Application`:
    * `CreateSnippetUseCase`, `UpdateSnippetUseCase`, `DuplicateSnippetUseCase`.
    * `DeleteSnippetToTrashUseCase`, `RestoreSnippetUseCase`, `PurgeTrashUseCase`.
    * `ManageFoldersUseCase` (crear, renombrar, reordenar).
    * `SearchSnippetsUseCase` (búsqueda por código, nombre y texto; favoritos y recientes).
* **Verificación:** Pruebas de integración sobre base de datos SQLite en memoria comprobando unicidad de códigos, relaciones jerárquicas y política de papelera.

---

### ⚡ Hito 3: Motor de Expansión, Delimitadores y Variables
* **Objetivo:** Desarrollar el núcleo de procesamiento de texto, análisis de delimitadores y sustitución de variables.
* **Entregables:**
  * `CircularKeyBuffer`: búfer en memoria de hasta 50 caracteres, hilo-seguro y con temporizador de purga por inactividad (5 segundos).
  * `SnippetExpansionEngine`:
    * Detección de delimitadores (espacio, tabulador, intro).
    * Búsqueda del atajo coincidente respetando o ignorando mayúsculas/minúsculas.
  * `VariableResolutionService`:
    * Sustitución de `{date}` y `{time}` con formato del sistema o configurable.
    * Extracción y preparación de variables interactivas `{input:nombre}`.
* **Verificación:** Pruebas unitarias cubriendo:
  * Coincidencia exacta y con delimitadores.
  * Comportamiento ante atajos desactivados o en papelera.
  * Sustitución correcta de fechas y horas.

---

### 🛡️ Hito 4: Capa Win32 (Hooks, SendInput y Seguridad)
* **Objetivo:** Conectar el motor de la aplicación con la API de Windows garantizando no bloquear el sistema y protegiendo datos sensibles.
* **Entregables:**
  * `LowLevelKeyboardHook`:
    * Instalación de `WH_KEYBOARD_LL` con puntero estático protegido frente al GC.
    * Encolado asíncrono no bloqueante en `Channel<RawKeyEvent>` (< 0.5 ms de respuesta).
  * `WindowsTextInjector`:
    * Envío de pulsaciones de retroceso `VK_BACK` para borrar el código.
    * Inyección de texto con `SendInput` y banderas `KEYEVENTF_UNICODE`.
  * `SensitiveControlDetector`:
    * Consulta mediante UI Automation a `IsPasswordProperty`.
    * Cancelación inmediata del procesamiento si el foco es un campo de contraseña (RF-08).
  * `FocusTracker`: Cancelación silenciosa si la ventana activa cambia durante el proceso (RF-10).
* **Verificación:** Pruebas unitarias con mocks de interfaces Win32 y pruebas funcionales de inyección.

---

### 🔄 Hito 5: Sincronización, CSV y Copias de Seguridad
* **Objetivo:** Construir los mecanismos de respaldo, importación/exportación y sincronización segura multi-equipo sin servidor central.
* **Entregables:**
  * `CsvService`:
    * Exportación e importación de atajos conservando la estructura de carpetas (RF-25 a RF-27).
    * Gestión de conflictos caso por caso durante la importación.
  * `BackupService`:
    * Copia de seguridad consistente programada con la API de backup de SQLite (RF-28 a RF-30).
    * Política de retención automática (conservar últimas 10 copias) (RF-31).
    * Función de vista previa y restauración (RF-32).
  * `SyncEngine` y `FileWatcherSyncService`:
    * Generación y consumo de `sync_manifest.json` en carpeta compartida (Google Drive/OneDrive).
    * Algoritmo de detección de conflictos locales vs. remotos (RF-24).
    * Modo tolerante a fallos cuando la carpeta remota no está disponible (RF-23).
* **Verificación:** Pruebas unitarias de detección de conflictos, serialización CSV y retención de copias de seguridad.

---

### 🖥️ Hito 6: Interfaz Gráfica WPF, Bandeja y Ventana Flotante
* **Objetivo:** Implementar la interfaz visual completa siguiendo el patrón MVVM y las directrices de apariencia de Windows.
* **Entregables:**
  * Configuración de ejecución en segundo plano (`ShutdownMode = OnExplicitShutdown`).
  * `TrayIcon`: icono en la bandeja del sistema con estados (activo/pausado) y menú contextual.
  * `MainWindow`:
    * Panel lateral con árbol de carpetas, favoritos, recientes y papelera.
    * Lista central de atajos con buscador dinámico.
    * Panel de edición con atajos de teclado, selector de variables y opciones (sensible a mayúsculas, activo).
  * `FloatingVariableDialog`:
    * Ventana emergente compacta sin bordes, posicionada junto al cursor para solicitar variables `{input:nombre}` (RF-12).
  * `SettingsDialog`: configuración de inicio con Windows, copias de seguridad, sincronización y tema (claro/oscuro según el sistema).
  * `ConflictResolutionDialog`: ventana comparativa para resolver conflictos de sincronización.
* **Verificación:** Pruebas de integración visual, accesibilidad (atajos de teclado internos) y cambio dinámico de tema claro/oscuro.

---

### 📦 Hito 7: Aceptación Final, Empaquetado e Instalador
* **Objetivo:** Verificar exhaustivamente los 17 criterios de aceptación del MVP y generar el instalador ejecutable final.
* **Entregables:**
  * Matriz de pruebas de aceptación (verificación de los puntos 1 a 17 de la sección 20 de `02_ESPECIFICACION_FUNCIONAL_MVP.md`).
  * Optimización de publicación: `dotnet publish -c Release -r win-x64 --self-contained false`.
  * Script de **Inno Setup** (`installer/setup.iss`):
    * Instalador `AtajaMe_Setup.exe`.
    * Instalación limpia por usuario en `%LocalAppData%\Programs\AtajaMe`.
    * Registro de inicio automático opcional en `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.
    * Desinstalador con opción de purga de datos.
* **Verificación:** Instalación de prueba en máquina limpia, ejecución en segundo plano, prueba de expansión en Notepad, Word, navegadores y verificación de desinstalación limpia.
