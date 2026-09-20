# 01 · Decisiones funcionales

## 1. Objeto del documento

Este documento consolida las decisiones tomadas durante la definición
inicial de la aplicación.

Las decisiones originales contienen redundancias, reformulaciones y
decisiones posteriores que modifican otras anteriores. Por ello se
distingue entre:

-   **MVP**: comportamiento previsto para la primera versión funcional.
-   **Futuro**: capacidad expresamente prevista como evolución.
-   **Sustituida o armonizada**: decisión cuyo contenido fue modificado
    posteriormente.
-   **Descartada**: capacidad que se decidió no incorporar.

Este documento mantiene la numeración utilizada durante la conversación
siempre que resulta posible. Algunas preguntas posteriores fueron
eliminadas por redundantes, por lo que la numeración original no
representa necesariamente 71 requisitos independientes.

------------------------------------------------------------------------

## Decisiones 1--27

### D01 · Tipo de contenido a expandir

-   **MVP:** texto plano y contenido dinámico sencillo.
-   **Futuro:** texto con formato y capacidades dinámicas más completas.
-   **Estado:** vigente y posteriormente reforzada por D64.

### D02 · Método de activación

-   **MVP:** activación mediante delimitador, por ejemplo espacio o
    tabulador.
-   **Futuro:** método configurable individualmente por atajo.
-   **Estado:** armonizada posteriormente con D68.

### D03 · Organización

-   **Decisión inicial:** carpetas + etiquetas.
-   **MVP consolidado:** solo carpetas.
-   **Futuro:** carpetas + etiquetas.
-   **Estado:** modificada posteriormente por D28.

### D04 · Ámbito de aplicación

-   **MVP:** disponible por defecto en las aplicaciones, con posibilidad
    de exclusiones.
-   **Futuro:** ámbito configurable individualmente por atajo.

### D05 · Coincidencia con texto normal

-   **MVP/objetivo:** reglas sencillas configurables por atajo.
-   Debe poder exigirse, por ejemplo, que el código esté aislado como
    palabra o cumpla una condición sencilla.
-   **Descartado:** patrones avanzados y expresiones regulares.
-   **Estado:** armonizada con D68.

### D06 · Mayúsculas y minúsculas

-   Configurable individualmente por atajo.
-   No se incluye transformación automática del estilo del resultado.

### D07 · Conflictos entre atajos

-   No se permiten conflictos.
-   El conflicto deberá resolverse antes de guardar el atajo.

### D08 · Duplicados exactos

-   No permitir dos atajos con el mismo código.
-   **Nota de trazabilidad:** fue una decisión asumida al continuar la
    conversación, no una confirmación expresa independiente.

### D09 · Edición del contenido

-   **MVP:** editor con variables insertables.
-   **Futuro:** editor más completo, preparado para contenido
    enriquecido.

### D10 · Variables dinámicas

-   **MVP:** variables automáticas sencillas y variables solicitadas al
    usuario.
-   Ejemplos: fecha, hora y valores introducidos al ejecutar.
-   **Futuro:** variables calculadas o condicionales.

### D11 · Solicitud de datos

-   Cuando un atajo necesite información, mostrar una ventana flotante
    compacta junto al cursor.

### D12 · Sincronización entre equipos

-   **MVP:** mecanismo sencillo basado en una ubicación sincronizada,
    por ejemplo una carpeta de Google Drive.
-   **Futuro:** varios mecanismos de sincronización configurables.
-   La implementación concreta queda pendiente de arquitectura.

### D13 · Almacenamiento e intercambio

-   Decisión inicial: SQLite como almacenamiento principal y JSON para
    importación/exportación.
-   **Estado:** parcialmente sustituida posteriormente: SQLite se
    reafirma como almacenamiento interno en D26 y el formato de
    intercambio se simplifica a CSV en D40.

### D14 · Copias de seguridad

-   Copia de seguridad automática periódica.

### D15 · Frecuencia de las copias

-   Configurable por tiempo.
-   La activación por número de cambios queda fuera del MVP.

### D16 · Ubicación de las copias

-   Carpeta definida por el usuario.

### D17 · Retención de copias

-   Configurable entre cantidad de copias y antigüedad.
-   Se planteó 10 copias como valor por defecto, sin convertirlo en
    requisito inamovible.

### D18 · Restauración

-   **MVP:** revisar la copia antes de aplicarla.
-   **Futuro:** posibilidades adicionales de restauración.

### D19 · Inicio con Windows

-   Durante la instalación se preguntará si la aplicación debe iniciarse
    con Windows.
-   La opción deberá poder modificarse posteriormente desde ajustes.
-   **Estado:** armonizada con D52, donde se eligió iniciar con el
    sistema como comportamiento deseado.

### D20 · Actualizaciones

-   Avisar cuando exista una nueva versión y pedir confirmación.
-   **Estado:** posteriormente simplificada por D50: aviso de nueva
    versión con instalación manual.

### D21 · Registro técnico

-   Decisión inicial: registrar únicamente errores técnicos.
-   **Estado:** sustituida por D51, donde se decidió no mantener
    registros persistentes para simplificar el producto.

### D22 · Privacidad del contenido expandido

-   Nunca guardar el contenido que haya sido expandido/escrito en otras
    aplicaciones.

### D23 · Conflictos de sincronización

-   Detectar el conflicto y solicitar resolución.
-   No sobrescribir silenciosamente.

### D24 · Recurso sincronizado no disponible

-   Trabajar con copia local y avisar al usuario.

### D25 · Portapapeles

-   No utilizar el portapapeles como mecanismo alternativo de expansión.
-   Si una expansión falla, no se conservará una copia temporal para
    reintentos.

### D26 · Base de datos local

-   SQLite desde el inicio.
-   PostgreSQL se descartó para el escenario actual por no existir
    necesidad de servidor o multiusuario.

### D27 · Importación inicial

-   Importación básica y puntual desde un formato genérico.
-   No construir importadores específicos complejos para otras
    aplicaciones en el MVP.
-   El formato real de exportación de PhraseExpress podrá analizarse
    posteriormente si se necesita una migración inicial.

------------------------------------------------------------------------

## Decisiones 28--45

### D28 · Organización del MVP

-   Solo carpetas.
-   Sustituye para el MVP la parte de etiquetas prevista en D03.

### D29 · Jerarquía de carpetas

-   Carpetas y subcarpetas con un máximo de tres niveles.

### D30 · Orden dentro de carpetas

-   Orden manual.
-   Si el usuario no establece un orden, utilizar orden alfabético por
    defecto.

### D31 · Búsqueda

-   Buscar por código, nombre y contenido.
-   Permitir filtros por carpeta.

### D32 · Favoritos y recientes

-   Incluir favoritos y lista de atajos recientes.

### D33 · Recientes

-   Cantidad de elementos recientes configurable.

### D34 · Eliminación

-   Mover los atajos eliminados a una papelera.
-   Permitir restaurarlos.

### D35 · Vaciado de papelera

-   Purga automática configurable.

### D36 · Guardado al editar

-   Guardar directamente los cambios.
-   No incorporar historial de versiones por atajo.

### D37 · Desactivación

-   Permitir activar o desactivar individualmente cada atajo.
-   No se requiere inicialmente activar/desactivar carpetas completas.

### D38 · Duplicación

-   Permitir duplicar un atajo.
-   Antes de guardar la copia será obligatorio cambiar su código.

### D39 · Exportación

-   Permitir exportar:
    -   todos los atajos;
    -   un atajo individual;
    -   una carpeta.

### D40 · Formato de exportación

-   Solo CSV para simplificar.
-   **Estado:** sustituye el JSON previsto inicialmente como formato de
    intercambio en D13.

### D41 · Conflictos durante importación

-   Resolver los conflictos caso por caso.
-   La pregunta inicial sobre vista previa se consideró parcialmente
    redundante con otras decisiones.

### D42 · Estructura de carpetas en exportación

-   Incluir siempre la estructura de carpetas.

### D43 · Creación masiva

-   Pregunta eliminada por redundante con la importación CSV.
-   No constituye un requisito independiente.

### D44 · Compatibilidad de los datos

-   Mantener compatibilidad de los datos entre versiones de la propia
    aplicación.

### D45 · Compartir atajos entre usuarios

-   No previsto como funcionalidad específica del MVP.
-   El producto no dispondrá inicialmente de enlaces compartibles ni
    repositorio común.

------------------------------------------------------------------------

## Decisiones 46--71

### D46 · Sistemas operativos

-   Solo Windows en esta fase.

### D47 · Idiomas

-   Interfaz inicialmente en un único idioma.
-   La aplicación deberá quedar preparada técnicamente para futuras
    traducciones.

### D48 · Accesibilidad básica

-   Tamaño de fuente ajustable.
-   Atajos de teclado para operar la propia aplicación.

### D49 · Versiones de Windows

-   Objetivo: soportar la versión vigente de Windows y hasta dos
    versiones anteriores, sujeto a validación técnica durante
    arquitectura y pruebas.

### D50 · Actualizaciones de la aplicación

-   Avisar de que existe una nueva versión.
-   La instalación de la actualización será manual.
-   **Estado:** concreta y simplifica D20.

### D51 · Registros

-   No mantener registros persistentes de uso ni de errores en el MVP.
-   Los errores deberán poder comunicarse al usuario cuando ocurran.
-   **Estado:** sustituye D21.

### D52 · Ejecución

-   Iniciar con Windows.
-   La aplicación funcionará en segundo plano.
-   Debe armonizarse con D19 para permitir configurar este
    comportamiento.

### D53 · Notificaciones

-   Mostrar notificaciones únicamente para errores críticos.

### D54 · Copias de seguridad

-   Pregunta eliminada por redundante.
-   Aplican D14--D18.

### D55 · Cierre de la ventana

-   Al cerrar la ventana principal, mantener la aplicación en la bandeja
    del sistema.

### D56 · Telemetría

-   No recopilar métricas ni datos de uso.

### D57 · Portapapeles

-   Pregunta eliminada por redundante.
-   Aplica D25: no utilizar el portapapeles como mecanismo alternativo
    de expansión.

### D58 · Fallo de un atajo

-   Si una expansión falla, mostrar un aviso discreto en ese momento.
-   No generar una notificación persistente salvo que el fallo alcance
    la consideración de error crítico.

### D59 · Apariencia

-   Seguir la configuración de tema del sistema operativo, incluyendo
    claro/oscuro.

### D60 · Restauración tras cierre inesperado

-   Pregunta eliminada.
-   No implementar guardado continuo de estado para recuperación tras un
    cierre inesperado.
-   Al volver a iniciar, la aplicación abre normalmente.

### D61 · Primer uso

-   Mostrar una pantalla introductoria breve.

### D62 · Ayuda

-   Incluir una sección básica de ayuda dentro de la aplicación.

### D63 · Aplicación de cambios

-   Los cambios realizados en los atajos se aplican inmediatamente.

### D64 · Texto enriquecido

-   **MVP:** solo texto plano.
-   **Futuro:** posibilidad de mantener formato/texto enriquecido.
-   Refuerza y concreta D01.

### D65 · Tamaño de los atajos

-   Establecer un límite interno razonable para el contenido.
-   El valor concreto se decidirá técnicamente; no debe introducir una
    limitación artificial para el uso normal.

### D66 · Campos incompatibles

-   Si no se puede realizar la expansión en un campo, mostrar un aviso
    sencillo.
-   No intentar métodos alternativos complejos en el MVP.

### D67 · Distribuciones de teclado

-   Sin soporte específico para distribuciones no estándar.
-   Utilizar el comportamiento estándar proporcionado por Windows.

### D68 · Disparador de expansión

-   **MVP:** expansión mediante delimitador.
-   **Futuro:** configurable por atajo.
-   Armoniza D02 y D05.

### D69 · Campos sensibles

-   Bloquear la expansión en campos sensibles, como contraseñas, cuando
    puedan identificarse de forma fiable.

### D70 · Pérdida de foco

-   Si cambia el foco durante una expansión, cancelar silenciosamente la
    operación.

### D71 · Versionado interno de la base de datos

-   No mantener historial funcional de versiones de los atajos dentro de
    la base de datos.
-   Las copias de seguridad cubren la recuperación de datos.
-   Esta decisión no impide utilizar un esquema técnico de migraciones
    de base de datos si la arquitectura lo requiere.

------------------------------------------------------------------------

## Decisiones consolidadas principales del MVP

Como síntesis, el MVP queda orientado a:

1.  Aplicación de escritorio exclusivamente para Windows.
2.  Instalación local y ejecución en segundo plano.
3.  Inicio con Windows, configurable.
4.  Expansión de texto plano mediante códigos cortos y delimitador.
5.  Variables dinámicas sencillas.
6.  Ventana flotante para solicitar valores cuando sea necesario.
7.  Organización mediante carpetas y subcarpetas de hasta tres niveles.
8.  Búsqueda por código, nombre y contenido.
9.  Favoritos y recientes.
10. Activación/desactivación individual de atajos.
11. Ámbito general con exclusiones.
12. SQLite como almacenamiento local.
13. Sincronización sencilla mediante ubicación sincronizada, con copia
    local y detección de conflictos.
14. Importación/exportación CSV.
15. Copias de seguridad automáticas periódicas en carpeta elegida por el
    usuario.
16. Papelera y restauración.
17. Sin telemetría ni almacenamiento del contenido expandido.
18. Sin historial interno de versiones de los atajos.
19. Avisos discretos ante fallos y notificación solo para errores
    críticos.
20. Tema visual según Windows.
21. Interfaz preparada para futura traducción.
22. Actualizaciones notificadas, con instalación manual.

## Capacidades expresamente pospuestas

-   texto enriquecido;
-   etiquetas;
-   activación configurable por atajo;
-   ámbito de aplicación individual por atajo;
-   variables calculadas o condicionales;
-   múltiples mecanismos de sincronización;
-   importadores específicos de aplicaciones de terceros;
-   capacidades avanzadas de restauración;
-   soporte específico para distribuciones de teclado no estándar.

## Capacidades descartadas en esta fase

-   expresiones regulares o patrones avanzados;
-   PostgreSQL para el escenario actual;
-   telemetría;
-   historial de contenido expandido;
-   repositorio compartido entre usuarios;
-   recuperación continua de estado tras cierres inesperados;
-   historial funcional de versiones por atajo.
