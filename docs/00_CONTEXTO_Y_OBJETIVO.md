# 00 · Contexto y objetivo

## 1. Propósito del proyecto

Desarrollar una aplicación de escritorio para Windows, conceptualmente
similar a PhraseExpress, orientada inicialmente a uso personal y, de
forma ocasional, compartida con algún compañero.

La aplicación permitirá definir códigos cortos o atajos de texto que, al
escribirse en aplicaciones compatibles, se sustituyan por contenidos más
largos.

Ejemplo:

-   Código: `bd`
-   Expansión: `buenos días`

El producto debe poder instalarse en un equipo Windows mediante un
instalador ejecutable que incluya o gestione los componentes necesarios
para su funcionamiento.

## 2. Necesidad que resuelve

El usuario necesita reutilizar de forma rápida textos frecuentes en
distintos contextos, evitando escribir repetidamente contenidos
habituales.

Los atajos podrán utilizarse para distintos ámbitos, por ejemplo:

-   correo electrónico;
-   textos de empresa;
-   proyectos concretos;
-   respuestas frecuentes;
-   contenidos personales o profesionales reutilizables.

## 3. Principios funcionales de partida

La aplicación deberá permitir:

1.  Crear, editar, eliminar, organizar, activar y desactivar atajos.
2.  Detectar códigos escritos por el usuario y sustituirlos por el
    contenido configurado.
3.  Funcionar en distintas aplicaciones de Windows, sujeto a las
    limitaciones técnicas de cada aplicación.
4.  Organizar los atajos mediante una estructura comprensible.
5.  Definir reglas sencillas sobre cuándo y dónde se ejecuta un atajo.
6.  Mantener una base local de atajos.
7.  Sincronizar la información entre distintos ordenadores mediante una
    ubicación compartida o sincronizada.
8.  Disponer de mecanismos básicos de importación, exportación, copia de
    seguridad y restauración.
9.  Mantener la solución simple para el MVP y evitar capacidades
    avanzadas que no aporten valor inmediato.

## 4. Alcance inicial

El MVP será exclusivamente para Windows.

Se priorizan:

-   simplicidad de uso;
-   funcionamiento local;
-   instalación sencilla;
-   texto plano;
-   expansión fiable de atajos;
-   variables dinámicas sencillas;
-   organización por carpetas;
-   configuración suficiente para evitar expansiones no deseadas;
-   sincronización sencilla entre equipos;
-   almacenamiento local con SQLite.

Quedan para evoluciones posteriores, entre otras posibilidades:

-   texto enriquecido;
-   etiquetas además de carpetas;
-   métodos de activación configurables por atajo;
-   ámbitos de aplicación configurables individualmente;
-   variables calculadas o condicionales;
-   capacidades de edición más avanzadas;
-   mecanismos de sincronización alternativos;
-   internacionalización efectiva de la interfaz.

## 5. Contexto de uso

El producto no se plantea inicialmente como un servicio comercial ni
como una plataforma multiusuario.

El escenario principal es:

-   un usuario;
-   uno o varios ordenadores Windows;
-   ejecución en segundo plano;
-   configuración local;
-   posibilidad de compartir excepcionalmente la aplicación con otros
    usuarios;
-   sin telemetría ni recopilación de datos de uso.

## 6. Sincronización

El objetivo inicial es que distintos ordenadores puedan utilizar una
misma configuración mediante una carpeta sincronizada, por ejemplo una
carpeta gestionada por Google Drive u otra solución equivalente.

La aplicación deberá seguir funcionando mediante una copia local cuando
el recurso sincronizado no esté disponible y deberá detectar los
conflictos de sincronización en lugar de sobrescribirlos
silenciosamente.

La arquitectura definitiva de este mecanismo queda pendiente de la fase
de arquitectura técnica.

## 7. Criterios de privacidad

La aplicación no deberá guardar el contenido concreto que haya sido
expandido en otras aplicaciones.

No se prevé recopilar telemetría ni métricas de uso.

Los campos sensibles, como campos de contraseña cuando puedan
identificarse de forma fiable, no deberán admitir expansión de atajos.

## 8. Enfoque de desarrollo

El proyecto se desarrollará por fases:

1.  contexto y objetivo;
2.  decisiones funcionales;
3.  especificación funcional del MVP;
4.  arquitectura técnica;
5.  plan de construcción;
6.  implementación y pruebas.

La definición funcional debe preceder a la elección definitiva de la
arquitectura.

## 9. Hipótesis técnica inicial

Durante la conversación inicial se planteó C# con .NET como candidato
natural por tratarse de una aplicación de escritorio específica para
Windows.

Esta propuesta no constituye todavía una decisión arquitectónica
cerrada. Deberá validarse durante la fase de arquitectura técnica frente
a los requisitos de:

-   captura global de teclado;
-   expansión de texto;
-   ejecución en segundo plano;
-   bandeja del sistema;
-   SQLite;
-   sincronización;
-   instalador;
-   actualizaciones;
-   mantenimiento futuro.

## 10. Fuente de autoridad

Para la construcción del producto, el orden de autoridad será:

1.  especificación funcional vigente del MVP;
2.  decisiones funcionales consolidadas;
3.  contexto y objetivo del proyecto;
4.  conversación original, únicamente como fuente de trazabilidad.

Cuando una decisión posterior sustituya a otra anterior, prevalece la
decisión posterior consolidada.
