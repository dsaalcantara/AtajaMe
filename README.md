# AtajaMe ⚡

> **Expansor de texto de escritorio para Windows, ligero, local y centrado en la privacidad.**

AtajaMe permite definir códigos cortos que se sustituyen automáticamente por contenidos de texto más largos mientras escribes en cualquier aplicación de Windows (correo, procesadores de texto, navegadores, chat, etc.).

---

## 🎯 Propósito y Características Principales

* **Expansión ágil y fiable**: Expande texto plano tras escribir un código y pulsar un delimitador (espacio, tabulador, etc.).
* **Variables dinámicas**:
  * Automáticas (fecha y hora del sistema).
  * Interactivas mediante una ventana emergente compacta (*pop-up*) junto al cursor para solicitar datos en tiempo real.
* **Organización clara**: Estructura de carpetas jerárquicas (hasta 3 niveles) con orden alfabético o manual.
* **Búsqueda instantánea**: Búsqueda integrada por código, nombre y contenido.
* **Privacidad estricta y Local-First**:
  * Persistencia en base de datos local SQLite.
  * **Cero telemetría**: No envía métricas a servidores externos.
  * **Protección de datos**: No almacena el texto expandido ni actúa sobre campos de contraseña.
* **Sincronización sin complicaciones**: Diseñado para sincronizarse entre ordenadores mediante carpetas en la nube (Google Drive, OneDrive, etc.) con detección de conflictos y copia local siempre operativa.
* **Importación y Exportación**: Copias de seguridad periódicas y formato de intercambio estándar CSV.
* **Integración con Windows**: Ejecución en segundo plano, residente en la bandeja del sistema (*system tray*) y compatibilidad con tema claro/oscuro de Windows.

---

## 📂 Estado del Proyecto y Documentación

El desarrollo de AtajaMe sigue un riguroso enfoque de ingeniería por fases:

1. [x] **Fase 1 · Contexto y Objetivo**: [`docs/00_CONTEXTO_Y_OBJETIVO.md`](docs/00_CONTEXTO_Y_OBJETIVO.md)
2. [x] **Fase 2 · Decisiones Funcionales**: [`docs/01_DECISIONES_FUNCIONALES.md`](docs/01_DECISIONES_FUNCIONALES.md)
3. [x] **Fase 3 · Especificación Funcional MVP**: [`docs/02_ESPECIFICACION_FUNCIONAL_MVP.md`](docs/02_ESPECIFICACION_FUNCIONAL_MVP.md)
4. [ ] **Fase 4 · Arquitectura Técnica**: `docs/03_ARQUITECTURA_TECNICA.md` *(en curso)*
5. [ ] **Fase 5 · Plan de Construcción**: `docs/04_PLAN_DE_CONSTRUCCION.md`
6. [ ] **Fase 6 · Implementación y Pruebas**

---

## 🛠️ Estructura del Repositorio

```text
AtajaMe/
├── docs/       # Especificación y documentación de diseño del producto
├── src/        # Código fuente de la aplicación (en construcción)
├── tests/      # Batería de pruebas automatizadas
├── .gitignore  # Filtro de archivos para .NET, SQLite y entorno Windows
└── README.md   # Información general del proyecto
```

---

## 📜 Licencia

Proyecto de uso personal y colaborativo. Todos los derechos reservados.
