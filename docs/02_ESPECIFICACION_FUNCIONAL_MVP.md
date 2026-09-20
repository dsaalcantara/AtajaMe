# 02 · Especificación funcional del MVP

## 1. Identificación

**Producto:** AtajaMe\
**Documento:** Especificación funcional del MVP\
**Versión:** 0.1\
**Plataforma inicial:** Windows\
**Estado:** Base funcional para diseño de arquitectura y construcción

## 2. Propósito

AtajaMe será una aplicación de escritorio para Windows que permitirá
definir códigos cortos y sustituirlos por textos más largos mientras el
usuario escribe en otras aplicaciones compatibles.

Ejemplo:

-   código: `bd`
-   resultado: `buenos días`

El MVP debe resolver esta función de forma fiable, local y sencilla,
evitando incorporar capacidades avanzadas que no sean necesarias para
validar y utilizar el producto.

## 3. Principios del MVP

El MVP se regirá por estos principios:

1.  Priorizar fiabilidad y simplicidad sobre cantidad de funciones.
2.  Funcionar principalmente de forma local.
3.  No requerir un servidor de base de datos.
4.  No recopilar telemetría ni historial del contenido escrito por el
    usuario.
5.  Mantener separadas las funciones imprescindibles del MVP y las
    evoluciones futuras.
6.  No condicionar innecesariamente la arquitectura futura.
7.  Permitir que la aplicación permanezca disponible en segundo plano
    sin mantener abierta su ventana principal.

## 4. Usuarios y escenario de uso

### 4.1 Usuario objetivo inicial

El MVP está orientado a un usuario individual que utiliza uno o varios
ordenadores Windows.

No se requiere:

-   gestión de organizaciones;
-   cuentas de usuario;
-   roles;
-   permisos multiusuario;
-   repositorio colaborativo;
-   servicio central.

### 4.2 Escenario principal

1.  El usuario instala AtajaMe.
2.  La aplicación dispone de una biblioteca local de atajos.
3.  El usuario crea un código y el texto asociado.
4.  AtajaMe permanece activo en segundo plano.
5.  El usuario escribe el código en una aplicación compatible.
6.  Al introducir el delimitador correspondiente, AtajaMe sustituye el
    código por el contenido configurado.
7.  Los cambios realizados en la biblioteca se aplican inmediatamente.

## 5. Requisitos funcionales

### RF-01 · Gestión de atajos

El usuario podrá:

-   crear atajos;
-   editar atajos;
-   eliminar atajos;
-   restaurar atajos eliminados;
-   duplicar atajos;
-   activar o desactivar individualmente cada atajo.

Los cambios se guardarán directamente y serán efectivos inmediatamente.

### RF-02 · Código del atajo

Cada atajo tendrá un código identificativo utilizado para provocar su
expansión.

No se permitirán códigos duplicados.

Cuando exista un conflicto, la aplicación deberá impedir el guardado
hasta que se resuelva.

La sensibilidad a mayúsculas y minúsculas será configurable por atajo.

### RF-03 · Contenido

El MVP soportará texto plano.

El diseño deberá permitir evolucionar posteriormente hacia texto
enriquecido sin que esta capacidad forme parte del MVP.

Se establecerá técnicamente un límite razonable de longitud del
contenido. El valor deberá ser suficientemente amplio para no interferir
con el uso normal.

### RF-04 · Activación de la expansión

En el MVP, la expansión se producirá mediante un delimitador después del
código.

La definición técnica determinará qué delimitadores iniciales se
admiten, contemplando como mínimo mecanismos habituales como espacio o
tabulador.

La configuración individual del método de activación por atajo queda
fuera del MVP.

### RF-05 · Condiciones de coincidencia

El sistema deberá evitar expansiones accidentales dentro de palabras
normales.

Las reglas deberán ser sencillas y previsibles.

No se implementarán expresiones regulares ni patrones avanzados en el
MVP.

### RF-06 · Expansión

Cuando se detecte un código válido y se cumplan sus condiciones:

1.  AtajaMe identificará el código.
2.  Obtendrá el contenido correspondiente.
3.  Sustituirá el código introducido por dicho contenido.
4.  No almacenará posteriormente el contenido que haya sido escrito en
    la aplicación de destino.

El mecanismo técnico de sustitución se definirá durante la arquitectura.

### RF-07 · Aplicaciones de destino

El MVP estará disponible de forma general para las aplicaciones
compatibles de Windows.

El usuario podrá definir exclusiones para impedir la expansión en
determinadas aplicaciones.

La configuración individual del ámbito de aplicación para cada atajo
queda como evolución futura.

### RF-08 · Campos sensibles

AtajaMe no deberá expandir atajos en campos sensibles, como campos de
contraseña, cuando Windows o la aplicación de destino permitan
identificarlos de forma fiable.

### RF-09 · Campos incompatibles

Si AtajaMe no puede realizar una expansión en un campo determinado:

-   no intentará mecanismos alternativos complejos;
-   mostrará un aviso discreto;
-   no conservará el contenido en el portapapeles para reintentos.

### RF-10 · Pérdida de foco

Si durante una expansión cambia la ventana o el control que tiene el
foco, la operación se cancelará silenciosamente para evitar escribir
contenido en un destino incorrecto.

## 6. Variables dinámicas

### RF-11 · Variables automáticas

El MVP admitirá variables automáticas sencillas.

Como mínimo, el diseño deberá contemplar variables equivalentes a:

-   fecha;
-   hora.

El catálogo definitivo se concretará durante el diseño detallado.

### RF-12 · Variables solicitadas al usuario

Un atajo podrá contener valores que deban ser introducidos por el
usuario en el momento de ejecutarlo.

Cuando sea necesario, AtajaMe mostrará una ventana flotante compacta
próxima al punto de interacción para solicitar los datos antes de
completar la expansión.

Las variables calculadas y las reglas condicionales quedan fuera del
MVP.

## 7. Organización de la biblioteca

### RF-13 · Carpetas

Los atajos se organizarán mediante carpetas.

Se admitirán subcarpetas hasta un máximo de tres niveles.

Las etiquetas quedan fuera del MVP.

### RF-14 · Ordenación

Dentro de una carpeta:

-   el orden inicial será alfabético;
-   el usuario podrá establecer posteriormente un orden manual.

### RF-15 · Búsqueda

La aplicación permitirá buscar por:

-   código;
-   nombre;
-   contenido.

También permitirá filtrar los resultados por carpeta.

### RF-16 · Favoritos

El usuario podrá marcar atajos como favoritos.

### RF-17 · Recientes

La aplicación mostrará los atajos utilizados recientemente.

El usuario podrá configurar cuántos elementos aparecen en esta lista.

La información de recientes deberá limitarse a la identificación del
atajo utilizado; no deberá guardar el contenido final expandido.

## 8. Eliminación y duplicación

### RF-18 · Papelera

La eliminación de un atajo lo trasladará inicialmente a una papelera.

Desde ella podrá restaurarse.

La eliminación automática de elementos de la papelera será configurable.

### RF-19 · Duplicación

El usuario podrá duplicar un atajo existente.

La copia conservará la configuración correspondiente, pero deberá
recibir un código diferente antes de poder guardarse.

## 9. Persistencia

### RF-20 · Base de datos local

AtajaMe utilizará SQLite como almacenamiento local del MVP.

No se requiere PostgreSQL ni un servidor de base de datos.

El modelo de datos y el mecanismo técnico de migraciones se definirán en
la arquitectura.

La ausencia de historial funcional de versiones de los atajos no impide
utilizar migraciones técnicas del esquema de base de datos.

### RF-21 · Compatibilidad entre versiones

Las evoluciones de AtajaMe deberán contemplar la compatibilidad de los
datos entre versiones soportadas de la aplicación.

## 10. Sincronización entre equipos

### RF-22 · Objetivo de sincronización

El MVP deberá permitir utilizar la configuración en varios equipos
mediante una ubicación sincronizada elegida por el usuario, por ejemplo
una carpeta gestionada por Google Drive u otra solución equivalente.

La arquitectura determinará si se sincroniza directamente un archivo de
datos, un artefacto de intercambio u otro mecanismo seguro.

### RF-23 · Funcionamiento sin sincronización disponible

Si la ubicación sincronizada no está disponible:

-   AtajaMe seguirá funcionando con una copia local;
-   informará de la situación al usuario;
-   no bloqueará el uso normal de los atajos.

### RF-24 · Conflictos

Si se detectan modificaciones incompatibles realizadas desde distintos
equipos, AtajaMe no deberá sobrescribir silenciosamente la información.

El conflicto deberá ser identificado y presentado para su resolución.

La estrategia técnica concreta se definirá en arquitectura.

## 11. Importación y exportación

### RF-25 · Formato

El formato de intercambio del MVP será CSV.

### RF-26 · Importación

La aplicación permitirá una importación básica desde CSV.

Cuando existan conflictos, deberán resolverse caso por caso.

No se desarrollarán en el MVP importadores específicos para
PhraseExpress u otras aplicaciones.

Si fuese necesario realizar la migración inicial desde PhraseExpress, su
formato de exportación se analizará como tarea específica.

### RF-27 · Exportación

El usuario podrá exportar:

-   toda la biblioteca;
-   una carpeta;
-   un atajo individual.

Cuando corresponda, la exportación conservará la información necesaria
para reconstruir la estructura de carpetas.

## 12. Copias de seguridad

### RF-28 · Creación

AtajaMe realizará copias de seguridad automáticas periódicas.

### RF-29 · Frecuencia

La periodicidad será configurable por el usuario.

### RF-30 · Ubicación

El usuario elegirá la carpeta donde se almacenan las copias.

### RF-31 · Retención

La política de retención será configurable mediante cantidad de copias
y/o antigüedad.

Podrá definirse posteriormente un valor predeterminado adecuado; durante
la definición inicial se planteó conservar 10 copias como referencia, no
como requisito obligatorio.

### RF-32 · Restauración

Antes de aplicar una copia de seguridad, AtajaMe permitirá revisarla.

No se requiere en el MVP un sistema avanzado de restauración parcial o
historial continuo.

## 13. Ejecución en Windows

### RF-33 · Inicio

AtajaMe podrá iniciarse automáticamente con Windows.

Durante la instalación se ofrecerá esta opción y posteriormente podrá
modificarse desde los ajustes.

### RF-34 · Segundo plano

La aplicación permanecerá disponible en segundo plano para detectar los
atajos.

### RF-35 · Bandeja del sistema

Al cerrar la ventana principal, AtajaMe continuará ejecutándose desde la
bandeja del sistema.

Deberá existir una acción explícita para finalizar completamente la
aplicación.

### RF-36 · Windows soportado

El objetivo funcional es soportar la versión vigente de Windows y hasta
dos versiones anteriores.

La viabilidad concreta deberá validarse durante la selección tecnológica
y las pruebas de compatibilidad.

### RF-37 · Teclados

El MVP no incorporará tratamiento específico para distribuciones de
teclado no estándar.

Se utilizará el comportamiento estándar proporcionado por Windows.

## 14. Interfaz

### RF-38 · Apariencia

La aplicación seguirá la configuración de apariencia de Windows,
incluyendo modo claro y oscuro cuando esté disponible.

### RF-39 · Idioma

La primera versión utilizará un único idioma de interfaz.

La arquitectura de la interfaz deberá quedar preparada para
internacionalización futura.

### RF-40 · Accesibilidad básica

La aplicación permitirá:

-   ajustar el tamaño de fuente;
-   utilizar atajos de teclado para acciones relevantes de la propia
    aplicación.

### RF-41 · Primer inicio

En el primer uso se mostrará una pantalla introductoria breve.

No se requiere un tutorial guiado completo.

### RF-42 · Ayuda

La aplicación incluirá una sección básica de ayuda.

## 15. Avisos y errores

### RF-43 · Fallos de expansión

Un fallo ordinario durante la expansión generará un aviso discreto y no
persistente.

### RF-44 · Errores críticos

Los errores críticos podrán generar una notificación visible para que el
usuario conozca que existe un problema.

### RF-45 · Registros

El MVP no mantendrá registros persistentes de uso ni telemetría.

Tampoco se requiere inicialmente un sistema persistente de registro de
errores.

La arquitectura podrá utilizar mecanismos técnicos transitorios de
diagnóstico durante desarrollo y pruebas siempre que no se conviertan en
recopilación de datos de uso del producto.

## 16. Privacidad

### RF-46 · Contenido expandido

AtajaMe nunca almacenará un historial del contenido que haya sido
finalmente expandido en aplicaciones externas.

### RF-47 · Telemetría

No se recopilarán métricas de uso ni se enviarán datos de utilización a
servicios externos.

### RF-48 · Portapapeles

El portapapeles no se utilizará como mecanismo alternativo para
conservar o reintentar expansiones fallidas.

## 17. Actualizaciones

### RF-49 · Detección de nueva versión

AtajaMe podrá informar al usuario cuando exista una nueva versión
disponible.

### RF-50 · Instalación

La instalación de nuevas versiones será manual en el MVP.

No se requiere actualización automática silenciosa.

El mecanismo utilizado para comprobar nuevas versiones deberá definirse
durante la arquitectura y no deberá introducir telemetría innecesaria.

## 18. Fuera de alcance del MVP

Quedan expresamente fuera de la primera versión:

-   texto enriquecido;
-   etiquetas;
-   expresiones regulares;
-   patrones avanzados;
-   PostgreSQL;
-   servidor central;
-   cuentas y roles;
-   colaboración multiusuario;
-   repositorio común de atajos;
-   enlaces compartibles;
-   variables calculadas;
-   variables condicionales;
-   activadores completamente configurables por atajo;
-   ámbito de aplicación individual por atajo;
-   importadores específicos de terceros;
-   sincronización mediante API propia;
-   historial de contenido expandido;
-   telemetría;
-   historial funcional de versiones por atajo;
-   recuperación continua del estado tras un cierre inesperado;
-   soporte específico para distribuciones de teclado no estándar;
-   actualización automática silenciosa.

## 19. Evoluciones previstas

Sin constituir requisitos del MVP, la definición funcional deja abiertas
estas evoluciones:

1.  texto enriquecido;
2.  etiquetas;
3.  métodos de activación configurables por atajo;
4.  ámbito de ejecución configurable por atajo;
5.  variables calculadas y condicionales;
6.  mecanismos adicionales de sincronización;
7.  internacionalización efectiva;
8.  restauración más avanzada;
9.  importadores específicos cuando exista una necesidad real.

## 20. Criterios funcionales de aceptación del MVP

El MVP podrá considerarse funcionalmente válido cuando, como mínimo:

1.  pueda instalarse y ejecutarse en un equipo Windows soportado;
2.  permita crear un atajo de texto plano;
3.  detecte el código escrito en una aplicación compatible;
4.  lo expanda correctamente al producirse el delimitador;
5.  permita editar, desactivar, duplicar y eliminar/restaurar el atajo;
6.  permita organizar los atajos en carpetas;
7.  permita buscarlos;
8.  soporte variables dinámicas básicas;
9.  solicite valores mediante la interfaz flotante cuando un atajo los
    necesite;
10. persista correctamente los datos en SQLite;
11. pueda importar y exportar CSV;
12. realice copias de seguridad según la configuración;
13. permanezca operativo en segundo plano;
14. continúe funcionando con datos locales cuando la ubicación
    sincronizada no esté disponible;
15. no almacene un historial del contenido expandido;
16. bloquee la expansión en campos sensibles cuando puedan identificarse
    de forma fiable;
17. avise de forma comprensible cuando una expansión no pueda
    realizarse.

## 21. Cuestiones reservadas para arquitectura

Este documento no cierra todavía las siguientes decisiones técnicas:

-   lenguaje y plataforma definitivos;
-   C#/.NET u otra alternativa;
-   tecnología de interfaz gráfica;
-   mecanismo de captura global del teclado;
-   mecanismo de sustitución del texto en aplicaciones externas;
-   identificación de campos sensibles;
-   modelo físico de SQLite;
-   estrategia de migraciones del esquema;
-   mecanismo exacto de sincronización;
-   algoritmo de detección y resolución de conflictos;
-   empaquetado e instalador;
-   mecanismo de comprobación de nuevas versiones;
-   estructura interna del proyecto;
-   estrategia de pruebas.

Estas decisiones deberán tomarse en el documento de arquitectura
técnica, utilizando esta especificación y las decisiones funcionales
consolidadas como fuentes de autoridad.

## 22. Fuentes de autoridad

Para resolver cualquier ambigüedad durante arquitectura o construcción
se utilizará este orden:

1.  `02_ESPECIFICACION_FUNCIONAL_MVP.md`;
2.  `01_DECISIONES_FUNCIONALES.md`;
3.  `00_CONTEXTO_Y_OBJETIVO.md`;
4.  `99_CONVERSACION_ORIGINAL_FUENTE.md`.

Si una decisión funcional posterior modifica una anterior, prevalece la
versión consolidada reflejada en esta especificación.
