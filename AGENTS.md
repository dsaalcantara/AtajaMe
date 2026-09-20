# AtajaMe · Directrices de Ingeniería y Guardianes de Calidad (AGENTS.md)

Este documento define las reglas de ingeniería, estándares de calidad y roles de supervisión que rigen el desarrollo de **AtajaMe**. Cualquier agente o asistente que intervenga en este repositorio debe cumplir estrictamente estas directrices.

---

## 1. Orden de Autoridad del Proyecto

Para resolver cualquier duda o ambigüedad, el orden de prevalencia es:
1. `docs/02_ESPECIFICACION_FUNCIONAL_MVP.md` (especificación formal del MVP).
2. `docs/01_DECISIONES_FUNCIONALES.md` (decisiones funcionales consolidadas).
3. `docs/00_CONTEXTO_Y_OBJETIVO.md` (visión y alcance general).
4. `docs/03_ARQUITECTURA_TECNICA.md` (diseño técnico del sistema).

---

## 2. Principios de Arquitectura e Ingeniería

### 2.1 Clean Architecture y Separación de Responsabilidades
El código debe estructurarse en capas desacopladas e independientes:
* **Domain (`src/AtajaMe.Domain`)**: Entidades de negocio (`Snippet`, `Folder`, `Variable`), objetos de valor y contratos de interfaces. Sin dependencias externas ni referencias a librerías de UI o APIs de Windows.
* **Application / Core (`src/AtajaMe.Application`)**: Casos de uso y orquestación (expansión de atajos, resolución de variables dinámicas, exportación/importación CSV, copias de seguridad, detección de conflictos de sincronización).
* **Infrastructure (`src/AtajaMe.Infrastructure`)**: Persistencia con SQLite, repositorio de datos, sistema de archivos local y almacenamiento de backups.
* **Windows Services (`src/AtajaMe.Windows`)**: Interacción de bajo nivel con el sistema operativo (hook global de teclado `WH_KEYBOARD_LL`, simulación de entrada con `SendInput`, detección de campos sensibles con UI Automation).
* **UI / Presentation (`src/AtajaMe.UI`)**: Interfaz gráfica WPF / MVVM, icono en bandeja de sistema (*system tray*), ventana flotante para variables interactivas y pantallas de configuración.

### 2.2 Privacidad y Protección de Datos (RF-08 y RF-46)
* **Búfer de teclas efímero**: El búfer para detectar códigos debe ser estrictamente circular y en memoria (máximo 50 caracteres). Se purga automáticamente tras detectar un delimitador, pérdida de foco o tras 5 segundos de inactividad.
* **Prohibición de persistencia de pulsaciones**: Bajo ninguna circunstancia se registrará en disco, logs ni base de datos el contenido tecleado por el usuario ni el contenido expandido.
* **Campos sensibles y contraseñas**: Antes de expandir cualquier atajo, se debe consultar el tipo de control con foco (UI Automation / `IsPassword`). Si es un campo sensible, la expansión se cancela de forma inmediata y silenciosa.
* **Cero telemetría**: No se incorporarán dependencias ni llamadas a servicios externos de recopilación de métricas.

### 2.3 Fiabilidad y Rendimiento Win32
* **Procesamiento de hooks sin bloqueo**: La función de devolución de llamada (*hook callback*) debe limitarse a encolar el evento en un canal asíncrono no bloqueante (`System.Threading.Channels.Channel<T>`) y devolver el control al sistema en menos de 1 milisegundo para no degradar la respuesta del teclado en Windows.
* **Gestión rigurosa de memoria y recursos**: Todo recurso no administrado (manejadores de Win32, hooks, conexiones SQLite) debe implementar el patrón `IDisposable` y liberarse limpiamente al cerrar la aplicación.
* **Tipado estricto y Nullable**: Habilitar `<Nullable>enable</Nullable>` en todos los proyectos de .NET. Cero tolerancias con advertencias de compilador (*warnings as errors*).

---

## 3. Guardianes de Calidad (Roles de Agentes)

En cada iteración o cambio de código, se debe verificar el cumplimiento de estos 4 roles:

### 🛡️ Guardián 1: Arquitectura y Especificación (Architecture & Spec Guard)
* **Función**: Verifica que el código desarrollado satisfaga los requisitos del MVP sin introducir funcionalidades pospuestas (como texto enriquecido o etiquetas) ni violar los límites de las capas.
* **Regla clave**: La capa de UI jamás interactúa directamente con SQLite ni con la API Win32; todo fluye a través de los servicios de aplicación e interfaces del dominio.

### 🔍 Guardián 2: Limpieza de Código y Refactorización (Clean Code & Refactor Guard)
* **Función**: Examina la legibilidad, principios SOLID, inyección de dependencias y patrones de diseño en C#.
* **Regla clave**: Métodos cortos y con responsabilidad única, nombres descriptivos, sin métodos de más de 40 líneas ni clases monolíticas. Proponer refactorización proactiva cuando se detecte duplicidad.

### ⚡ Guardián 3: Seguridad Win32 y Rendimiento (Win32 Safety & Performance Guard)
* **Función**: Audita las llamadas a la API de Windows, el consumo de CPU y la huella de memoria.
* **Regla clave**: Asegurar que la aplicación en segundo plano mantenga un consumo mínimo de recursos (< 30-40 MB de RAM en reposo) y que las llamadas a P/Invoke sean seguras.

### 🔒 Guardián 4: Privacidad y Resiliencia (Privacy & Resilience Guard)
* **Función**: Garantiza que no existan fugas de datos de teclado y que la sincronización externa no cause bloqueos en caliente en SQLite.
* **Regla clave**: Si el archivo compartido no está accesible, la aplicación debe seguir funcionando con la base de datos local sin arrojar excepciones no controladas.

---

## 4. Política de Pruebas Automatizadas

* Cada componente de lógica de negocio en `src/AtajaMe.Application` y `src/AtajaMe.Domain` debe contar con pruebas unitarias en `tests/AtajaMe.Tests`.
* Pruebas obligatorias para:
  1. Detección y coincidencia de códigos y delimitadores.
  2. Sustitución de variables de fecha/hora.
  3. Algoritmo de detección de conflictos en sincronización.
  4. Serialización y deserialización CSV (importación/exportación).
  5. Políticas de retención de copias de seguridad y purga de papelera.
