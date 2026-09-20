---
name: win32-csharp-standards
description: Estándares de ingeniería para desarrollo C# / .NET en Windows con hooks Win32 de bajo nivel, Clean Architecture, SQLite local y cumplimiento estricto de privacidad.
---

# Guía de Estándares de Ingeniería · AtajaMe

Esta skill contiene las pautas técnicas y patrones de diseño para la implementación de AtajaMe.

## 1. Patrones para Hooks Globales Win32 (`WH_KEYBOARD_LL`)

* **Delegado estático**: Mantener siempre una referencia persistente al delegado del hook (`LowLevelKeyboardProc`) en un campo estático o instancia de ciclo de vida singleton con `GC.KeepAlive` para evitar que el recolector de basura (*Garbage Collector*) lo elimine prematuramente y provoque un fallo de acceso (*AccessViolationException*).
* **Canal asíncrono no bloqueante**:
  ```csharp
  // En el callback del hook:
  _channel.Writer.TryWrite(keyEvent);
  return CallNextHookEx(_hookId, nCode, wParam, lParam);
  ```
  Toda la lógica de comparación de atajos, resolución de variables e invocación de sustitución debe correr en un hilo de fondo consumidor del canal (`ChannelReader<KeyEvent>`), jamás en el hilo del hook.
* **Simulación con SendInput**:
  * Enviar retrocesos (`VK_BACK`) equivalentes a la longitud del código + delimitador.
  * Inyectar el texto usando `KEYEVENTF_UNICODE` (`wScan = char`, `wVk = 0`) para garantizar compatibilidad total con caracteres acentuados, eñes y símbolos sin depender de la distribución del teclado.

## 2. Persistencia y Sincronización con SQLite

* **Modo WAL (Write-Ahead Logging)**: Configurar SQLite con `PRAGMA journal_mode=WAL;` y `PRAGMA synchronous=NORMAL;` para maximizar concurrencia y velocidad.
* **Aislamiento de la BD local**:
  * La base de datos activa `atajame.db` reside en `%LocalAppData%\AtajaMe\`.
  * La carpeta sincronizada (Google Drive/OneDrive) **nunca** alberga la base de datos abierta directamente.
  * La sincronización intercambia paquetes de sincronización versionados (por ejemplo, archivos `.json` o `.dat` con suma de verificación SHA-256 y marca temporal UTC).
  * Si se detectan cambios remotos más recientes que los locales sobre el mismo atajo, se genera un evento de conflicto que el usuario puede resolver.

## 3. Seguridad de Campos Sensibles

* Utilizar `UIAutomationClient` / `AutomationElement.FocusedElement` para inspeccionar la propiedad `AutomationElement.IsPasswordProperty`.
* Si `IsPassword == true`, el procesador de atajos desactiva la captura inmediatamente hasta que el foco cambie a un control no protegido.
