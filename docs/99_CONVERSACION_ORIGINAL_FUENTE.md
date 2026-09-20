# 99 · Conversación original · Fuente de trazabilidad

**Proyecto:** AtajaMe  
**Objeto:** conservar en un único archivo la conversación de origen utilizada como fuente de trazabilidad del proyecto.  
**Alcance:** mensajes visibles entre usuario y asistente relativos a la idea original, definición funcional, decisiones 1–71, consolidación inicial y creación de la documentación 00–02.  
**Exclusiones:** no se incluyen mensajes internos del sistema, instrucciones técnicas de la plataforma, razonamiento interno ni llamadas a herramientas.

> Este documento no sustituye a la especificación funcional vigente. En caso de discrepancia, prevalece el orden de autoridad definido en `02_ESPECIFICACION_FUNCIONAL_MVP.md`.

---

## 1. Idea original

**Usuario:** Eh... A ver... Ya tengo una idea de... de una aplicación que quiero desarrollar.

**Asistente:** Vale.

**Usuario:** Sobre... Yo tengo mi ordenador. Quiero hacer una aplicación de escritorio, ¿vale? Para que yo pueda ejecutarla en cualquier ordenador cuando, cuando necesite. Así que al final le daré...

**Asistente:** Okey.

**Usuario:** ... entiendo un archivo... .exe que me instale todo lo que sea necesario para la aplicación en el ordenador.

**Asistente:** Vamos a ver cómo hacerlo.

**Usuario:** A ver.

**Asistente:** Vale.

**Usuario:** Lo que quiero es tener una aplicación similar a Phrase Express. En mi ordenador tengo una aplicación que es alemana, Phrase Express, que tiene...

**Asistente:** Okey.

**Usuario:** ... tú metes un código corto y eso te despliega en el campo de texto que esté escribiendo o donde sea un texto más largo. Es decir, escribe `bd` y te escribe donde sea "buenos días", ¿vale? Ese tipo de ajustes que luego puedan...

**Asistente:** Claro.

**Usuario:** ... personalizarse con dónde se ejecuta o que se pueda personalizar agrupando tipos de shortcuts para que sean temas de correo o temas de empresa, temas de un proyecto concreto.

**Asistente:** Ujum.

**Usuario:** Y que yo pueda sincronizar luego eso en un archivo que esté guardado en algún sitio, que luego pueda ejecutar la aplicación en otro ordenador y pueda tirar del mismo archivo, tipo Google Drive o algo similar.

**Asistente:** Ajá.

**Usuario:** ¿Cómo ves? ¿Aplicación posible para hacerlo?

**Asistente:** Es posible, pero hay que pensarlo bien, sobre todo en el tema de sincronización y cómo capturar esos atajos a nivel global. Yo en este punto no escribiría todavía código. Primero hay que definir el comportamiento exacto, porque eso te va a condicionar mucho la arquitectura final.

**Usuario:** Sí, sí, de acuerdo. Primero hay que definir qué vamos a hacer y cómo lo vamos a hacer, pero... se podría crear una aplicación de escritorio, ¿verdad? No siempre hay que hacer aplicaciones web o algo parecido, sino que puedo hacer una aplicación de escritorio.

**Asistente:** Sí. No tiene por qué ser una aplicación web. De hecho, en tu caso, para todo lo que necesitas, lo que más te encaja es una aplicación de escritorio nativa en Windows. Yo miraría C# con .NET y luego decidiríamos la interfaz. Pero se puede hacer justo como lo planteas.

**Usuario:** Vale. ¿Por qué no me vas haciendo preguntas una a una para ir definiendo cómo sería la aplicación? Porque sé que después me tendré que sentar frente al ordenador con Codex y empezar a desarrollar las fases con el front, con el back, con la base de datos, con Alembic, con Pydantic, con todas las herramientas que hacen falta para tener un funcionamiento más serio.

**Asistente:** Claro, vamos a ir paso a paso. Lo trabajamos como en el otro proyecto: una decisión por vez y todo trazado.

---

## 2. Decisiones funcionales 1–27

### Decisión 1 · Alcance de la primera versión

**Asistente:** Primera pregunta, alcance de la primera versión. ¿Qué quieres que sea capaz de expandir inicialmente? Opción A, solo texto plano. Opción B, texto con formato básico. C, texto dinámico con variables. O D, todo lo anterior.

**Usuario:** El ideal sería todo lo anterior... pero si es muy complicado, lo dejamos solamente en texto básico.

**Asistente:** Como objetivo funcional ideal sería la D, todo, pero el MVP lo acotamos a texto plano y dinámico sencillo para no bloquear.

### Decisión 2 · Cómo se activa la expansión

**Asistente:** Siguiente decisión, cómo se activa la expansión. Opciones. A. Inmediato al escribir el código. B. Con un delimitador como espacio o tab. C. Con una combinación de teclas. D. Que cada atajo lo configure. Yo partiría por la B.

**Usuario:** Repite las opciones, por favor.

**Asistente:** A. Inmediato al escribir el código. B. Con un delimitador como espacio o tab. C. Con una combinación de teclas. D. Que cada atajo lo configure.

**Usuario:** La aplicación que estoy utilizando ahora mismo tiene todas esas opciones y cada atajo lo puedes configurar por separado. Puedes configurar uno que sea inmediato, otro con una combinación de teclas y otro con otro método. Me gustaría todas esas opciones, aunque un primer desarrollo con la opción B sería bueno.

**Asistente:** Entonces lo registramos así: objetivo final D, que cada atajo pueda configurar su método. Para el primer desarrollo, opción B.

### Decisión 3 · Organización de atajos

**Asistente:** ¿Cómo quieres organizar los atajos? Opciones: A, en carpetas. B, en categorías simples. C, con etiquetas múltiples. D, carpetas más etiquetas. Yo te propondría D.

**Usuario:** Sí, la D.

**Asistente:** Perfecto, decisión 3 registrada: D.

### Decisión 4 · Ámbito de aplicación

**Asistente:** ¿Quieres que funcione en todas las aplicaciones siempre? ¿Solo en las que autorices? ¿En todas por defecto pero con exclusiones? ¿O que cada atajo sea configurable? Yo aquí también te diría que como objetivo final la D, pero para empezar la C.

**Usuario:** Sí, estoy conforme. La D como objetivo final y la C como inicial.

**Asistente:** Registrada la decisión 4.

### Decisión 5 · Coincidencia con texto normal

**Asistente:** ¿Qué pasa si el atajo coincide con texto normal? A. Siempre expande aunque forme parte de otra palabra. B. Solo expande si está aislada como palabra. C. Configurable por atajo. D. Eso y además patrones avanzados. Aquí yo iría con C como inicial y final para el MVP. Dejamos la D para más adelante.

**Usuario:** Explícame cómo sería la D, cómo serían esos patrones avanzados.

**Asistente:** La D implicaría que los atajos no solo sean cadenas fijas, sino reglas, en plan expresiones regulares. Por ejemplo, detectar una fecha con cierto formato o un código que cumpla un patrón. Es potente, pero sube mucho la complejidad y el riesgo de disparos inesperados. Por eso yo lo dejaría fuera del MVP y lo mantendría como capacidad avanzada futura.

**Usuario:** Esa última no me cuadra demasiado. Creo que complicaría la funcionalidad que estoy buscando. La otra opción, recuérdamela.

**Asistente:** La C: reglas claras por atajo, por ejemplo que `bd` solo expanda si va seguido de espacio o si está al inicio de una línea. Sin patrones complejos.

**Usuario:** Pero las otras eran que estuviera el texto solo o algo así, separado entre espacio.

**Asistente:** La opción B sería que solo expande si está aislado como palabra, es decir, separado por espacios o signos de puntuación. La C es lo mismo pero configurable por atajo.

**Usuario:** Venga, vale, la C.

**Asistente:** Queda registrada la decisión 5 como C.

### Decisión 6 · Mayúsculas y minúsculas

**Asistente:** Sensibilidad a mayúsculas y minúsculas. A. Que distinga siempre. B. Que no distinga nunca. C. Configurable por atajo. D. Configurable y además conserva estilo; por ejemplo, `bd` → `Buenos días`, `BD` → `BUENOS DÍAS`. Yo como objetivo final pondría D, pero C en primera versión.

**Usuario:** La C.

**Asistente:** Decisión 6: C.

### Decisión 7 · Conflictos entre atajos

**Asistente:** Si hay dos atajos que pueden coincidir: A, gana siempre el más corto. B, gana el más largo o específico. C, se prohíbe el conflicto al crear. D, se avisa pero se permite. Yo aquí te propongo D para que no te bloquees, pero se avisa de forma clara.

**Usuario:** Sí, sí, hay que avisar. No quiero que haya conflicto.

**Asistente:** Entonces la decisión 7 la ajustamos a C: prohibir conflicto y obligar a resolverlo al crear.

### Decisión 8 · Duplicados exactos

**Asistente:** Si intentas crear dos veces el mismo atajo: A, no se permite nunca. B, se permite si están en carpetas distintas. C, se permite si tienen ámbitos de aplicación distintos. D, se permite siempre. Yo aquí iría con la C.

**Usuario:** Repite las opciones, que no te he echado cuenta.

**Asistente:** A, no se permite nunca. B, se permite si están en carpetas distintas. C, se permite si tienen ámbitos de aplicación distintos. D, se permite siempre.

**Usuario:** La verdad es que no tengo ni idea.

**Asistente:** Para simplificar esta decisión ahora mismo, yo elegiría la A: nunca permitir duplicados. Es más simple y no estás buscando una lógica compleja.

### Decisión 9 · Edición del contenido

**Asistente:** ¿Qué prefieres? A, un campo de texto simple. B, editor con vista previa. C, editor con variables insertables. D, todo lo anterior preparando un editor enriquecido futuro. Yo aquí te propongo C para el MVP.

**Usuario:** Vale, objetivo final la D y el MVP la C.

**Asistente:** Decisión 9 registrada.

### Decisión 10 · Variables dinámicas iniciales

**Asistente:** A, solo variables automáticas como fecha, hora, portapapeles. B, solo variables rellenadas por ti al lanzar el atajo. C, ambas. D, lo anterior más variables calculadas o condicionales. Propongo C para el MVP y D más adelante.

**Usuario:** Correcto, estoy de acuerdo: C para el MVP y D para más adelante.

### Decisión 11 · Solicitud de datos al ejecutar

**Asistente:** Cuando un atajo requiere datos, ¿qué pasa? A, ventana pequeña con los campos y un botón insertar. B, ventana flotante compacta junto al cursor. C, abrir la aplicación principal. D, configurable por atajo. Yo iría con B para el MVP.

**Usuario:** Repíteme A y B.

**Asistente:** A, ventana pequeña con los campos y un botón insertar. B, ventana flotante compacta junto al cursor, o sea, que sale en la misma posición en la que estás escribiendo.

**Usuario:** Sí, la B.

**Asistente:** Decisión 11: B.

### Decisión 12 · Sincronización entre equipos

**Asistente:** A, un archivo único en una carpeta sincronizada, tipo Google Drive. B, sincronización directa a un servicio en nube vía API. C, base local más copia central automática. D, varias opciones configurables. Yo te propongo la A para el MVP y la D para más adelante.

**Usuario:** Sí, de acuerdo.

### Decisión 13 · Formato de almacenamiento

**Asistente:** A, JSON legible. B, SQLite. C, formato propio. D, base SQLite más exportar o importar en JSON. Yo te propongo D.

**Usuario:** Vale.

### Decisión 14 · Copias de seguridad

**Asistente:** ¿Quieres copia manual, automática periódica, automática antes de cambios, o una combinación de la periódica y antes de cambios? Con opción de restaurar versiones. Te propongo la opción D.

**Usuario:** Automática periódica.

**Asistente:** Entonces dejamos la decisión 14 en B: copia automática periódica.

### Decisión 15 · Frecuencia de copias

**Asistente:** A, diaria. B, semanal. C, configurable por ti. D, según número de cambios. Propongo C.

**Usuario:** Según número de cambios, ¿cómo sería eso?

**Asistente:** Por ejemplo, cada N modificaciones de tu biblioteca de atajos, se lanza una copia. Cada 10 cambios, por ejemplo. Es útil si haces muchos cambios seguidos, pero es menos intuitivo. Yo mantendría la opción C como opción inicial, configurable por tiempo y, si más adelante quieres la opción por número de cambios, la añadimos.

**Usuario:** Vale, pues me quedo con esa que dices tú ahora.

### Decisión 16 · Ubicación de las copias

**Asistente:** A, en carpeta local fija. B, en carpeta elegida por el usuario. C, en la misma carpeta sincronizada. D, configurable entre local y sincronizada. Yo te propongo D.

**Usuario:** En la carpeta definida por el usuario.

### Decisión 17 · Retención de copias

**Asistente:** Conservar todas, solo las últimas X, conservar por días, o configurable entre número y antigüedad. Te propongo D con un valor por defecto, por ejemplo, 10 copias.

**Usuario:** Vale.

### Decisión 18 · Restauración de copia

**Asistente:** A, sustituir toda la base por la copia. B, importar y revisar antes de aplicar. C, restauración parcial por elementos. D, todas las anteriores. Te propongo B para el MVP y D a futuro.

**Usuario:** Vale, si te parece bien así.

### Decisión 19 · Inicio de la aplicación

**Asistente:** A, solo manual cuando la abras. B, automático al iniciar Windows. C, preguntar durante la instalación. D, configurable desde dentro de la app. Aquí te propongo C durante la instalación y D luego en ajustes.

**Usuario:** Estoy de acuerdo.

### Decisión 20 · Actualizaciones

**Asistente:** A, manual. B, automáticas. C, avisar y pedir confirmación. D, configurable. Yo aquí sugiero la C.

**Usuario:** A ver, repite, que has cambiado la voz y me has despistado.

**Asistente:** La decisión 20 es sobre actualizaciones de la aplicación. A, manual. B, automáticas. C, avisar y pedir confirmación. D, configurable. Yo sugiero la C.

**Usuario:** ¿Actualizaciones automáticas de qué? ¿De la copia de seguridad?

**Asistente:** De la aplicación. Si hay cambios o mejoras y existe una nueva versión.

**Usuario:** Vale, pero esa nueva versión la lanzaré yo. Pensando en que esto lo voy a distribuir en algún sitio y un usuario final encuentra que el desarrollador ha lanzado una nueva versión y le pregunta si quiere instalarla o no. ¿A eso te refieres?

**Asistente:** Exactamente.

**Usuario:** ¿Y cuáles son las opciones?

**Asistente:** A, manual. B, automáticas. C, avisar y pedir confirmación. D, configurable.

**Usuario:** Pues la C.

### Decisión 21 · Logs de uso

**Asistente:** A, sin logs. B, logs solo de errores técnicos. C, logs de uso local sin datos sensibles. D, logs detallados configurables. Yo aquí propongo la B.

**Usuario:** ¿Para qué me puede servir a mí?

**Asistente:** Para diagnóstico si hay un fallo o un problema, sin necesidad de tener datos personales.

**Usuario:** No lo sé, la verdad. Esta opción no la tenía pensada. Si quieres proponer tú, me parece bien.

**Asistente:** Propongo la B como base: solo errores técnicos y nada de datos de uso. Si más tarde quieres más visibilidad, se puede ampliar.

**Usuario:** Vale.

### Decisión 22 · Privacidad en expansión

**Asistente:** A, nunca guardar contenido expandido. B, guardar contenido solo si lo autorizas. C, guardar todo para historial. D, configurable por atajo. Aquí propongo la A.

**Usuario:** Sí, estoy de acuerdo. Nunca guardar ese contenido.

### Decisión 23 · Conflictos de sincronización

**Asistente:** A, gana siempre la última modificación. B, se detecta conflicto y se pide resolver. C, se bloquea la edición simultánea. D, híbrido configurable. Yo propongo B.

**Usuario:** De acuerdo.

### Decisión 24 · Archivo sincronizado no disponible

**Asistente:** A, la app se bloquea. B, funciona en modo solo lectura. C, funciona con copia local y avisa. D, deja elegir cada vez. Aquí propongo la C.

**Usuario:** De acuerdo, la C.

### Decisión 25 · Portapapeles

**Asistente:** A, nunca usar portapapeles. B, usarlo solo temporalmente y borrar. C, guardar historial de portapapeles. D, configurable. Yo propongo la B.

**Usuario:** ¿Para qué se puede utilizar el portapapeles en esta aplicación?

**Asistente:** Si un campo no acepta la inserción directa de texto, la app puede usar el portapapeles para pegar y luego borrarlo. No guarda historial, es solo un mecanismo puntual.

**Usuario:** Pero si tengo algún tipo de problema con una aplicación porque no me deja usar lo que sea, guardarlo en el portapapeles no le veo demasiado sentido porque después lo vas a intentar otra vez. Entiendo que se va a generar el texto otra vez, ¿no?

**Asistente:** No guarda ese texto; simplemente sería un paso intermedio donde se guarda en el portapapeles, se pega y luego se borra. Pero si no te parece sensato, lo dejamos en la A.

**Usuario:** Creo que si intentas hacer un reemplazo de un código corto con un atajo en algún sitio y no funciona, vas a intentarlo otra vez. Es más fácil directamente al segundo intento volver a generar el texto y listo, ¿no?

**Asistente:** Entonces dejamos la opción A, nunca usar el portapapeles.

**Usuario:** Sí, yo creo que sí.

### Decisión 26 · Estructura de datos inicial

**Asistente:** A, un archivo plano JSON. B, base de datos SQLite desde el inicio. C, JSON al principio con migración prevista a SQLite. D, otra opción más compleja. Yo aquí propongo la C para avanzar rápido sin cerrar puertas.

**Usuario:** Repite esta pregunta, ¿de qué era?

**Asistente:** De la estructura de datos inicial.

**Usuario:** Yo ahora mismo utilizo una aplicación que funciona y creo que se puede exportar todo lo que tengo creado, pero no sé cuál es el formato. Si te refieres a utilizar un archivo como ese para importarlo en esta aplicación, no te sé decir qué formato utiliza. No sé si sería mejor dejarlo en stand-by hasta que yo sepa qué formato puedo darte. Y además sería solamente para el arranque; no quiero que esto sea parte de la aplicación como tal. Para la aplicación tendría que ser manual o que me busques algún estándar que este tipo de aplicaciones utilicen en la industria.

**Asistente:** Aquí hay dos cosas mezcladas. La decisión 26 es sobre el almacenamiento interno de nuestra aplicación y no depende del formato de PhraseExpress. Yo mantendría SQLite desde el inicio como almacenamiento principal y exportación o importación a JSON. Lo de importar desde PhraseExpress lo dejaría como otra decisión para más tarde.

**Usuario:** Yo normalmente utilizo PostgreSQL. No sé si sería en este caso mejor SQLite.

**Asistente:** En este caso mantendría SQLite para el MVP: es una aplicación de escritorio monousuario por equipo y simplifica mucho la instalación. Con PostgreSQL tendríamos que gestionar un servicio, conexiones y credenciales. Si en el futuro hubiese modo multiusuario real o backend central, se podría replantear.

**Usuario:** Vale, venga. SQLite entonces.

### Decisión 27 · Importación desde otras aplicaciones

**Usuario:** Vamos a continuar con el proyecto donde lo dejamos. Hazme un repaso para poder continuar con la siguiente pregunta, una a una.

**Asistente:** Estábamos definiendo una aplicación de atajos de texto. Cerramos reglas de detección, sensibilidad a mayúsculas configurable, prohibición de conflictos, editor con variables, sincronización por carpeta y SQLite desde el inicio. Dejamos pendiente la importación desde tu aplicación actual. La 27 sería sobre importación desde otras aplicaciones: A, no incorporar nada al producto y hacer una migración puntual. B, importar formatos genéricos tipo JSON o CSV. C, formatos genéricos más importadores específicos. D, sistema de importadores extensible. Para el MVP propondría B.

**Usuario:** Estábamos hablando de importaciones de aplicaciones similares, ¿no?

**Asistente:** Exactamente.

**Usuario:** En principio no me parece algo absolutamente imprescindible, más que por la primera vez. Se puede poner algo básico. ¿Qué me recomiendas?

**Asistente:** Para el MVP lo dejaría en algo sencillo: una importación básica y puntual desde un formato genérico, tipo CSV o JSON, sin más complicaciones.

**Usuario:** Vale, continúa.

---

## 3. Decisiones funcionales 28–71

### Decisión 28 · Organización del MVP

**Asistente:** Organización de atajos: A, lista única. B, carpetas. C, etiquetas. D, carpetas y etiquetas. Para el MVP propondría B, carpetas.

**Usuario:** Me suena que lo hemos hablado antes. Confírmalo.

**Asistente:** Salió de forma lateral y antes habíamos elegido carpetas más etiquetas como objetivo. Para no pisarlo: D como objetivo final y solo carpetas en el MVP.

**Usuario:** Yo diría que sí. Solo carpetas.

### Decisión 29 · Jerarquía de carpetas

**Asistente:** A, un solo nivel. B, carpetas y subcarpetas sin límite. C, carpetas y subcarpetas con límite de profundidad. D, sin jerarquía. Para el MVP recomendaría C, con máximo tres niveles.

**Usuario:** Sí, esa me parece bien.

### Decisión 30 · Orden de atajos

**Asistente:** A, alfabético fijo. B, manual arrastrando. C, varios criterios seleccionables. D, manual más criterios temporales. Yo iría con B, manual.

**Usuario:** Manual, pero si no se selecciona nada, que se vaya ordenando alfabéticamente.

**Asistente:** Orden manual, con orden alfabético por defecto.

### Decisión 31 · Búsqueda

**Asistente:** A, solo por código. B, código y nombre. C, también por contenido. D, todo lo anterior con filtros por carpeta. Te propongo D.

**Usuario:** La D.

### Decisión 32 · Favoritos y recientes

**Asistente:** A, no hay favoritos. B, marcar favoritos. C, favoritos y recientes. D, favoritos, recientes y zona de accesos configurables. Yo diría C.

**Usuario:** Pues la C.

**Usuario:** Espera, estaba hablando de la anterior y te había preguntado cuál era la opción C.

**Asistente:** Favoritos y recientes: puedes marcar algunos como favoritos y además tener una lista de los últimos usados.

**Usuario:** Vale, me parece bien. Continúa.

### Decisión 33 · Recientes

**Asistente:** A, últimos usados sin configurar. B, configurable en cantidad. C, configurable en cantidad y tiempo. D, solo en la sesión actual. Te propongo B.

**Usuario:** ¿Cuál es el B?

**Asistente:** Configurable en cantidad: eliges cuántos atajos recientes se muestran.

**Usuario:** Vale.

### Decisión 34 · Borrado

**Asistente:** A, borrado directo con confirmación. B, papelera con opción de restaurar. C, papelera con purga automática por días. D, configurable. Te propongo B.

**Usuario:** Sí.

### Decisión 35 · Vaciado de papelera

**Asistente:** A, manual. B, automático tras X días fijo. C, automático configurable. D, nunca borrar automáticamente. Te propongo C.

**Usuario:** Sí, la C.

### Decisión 36 · Edición de atajo existente

**Asistente:** A, guardar directamente. B, pedir confirmación. C, guardar directo y permitir deshacer el último cambio. D, historial de versiones por atajo. Yo recomendaría C.

**Usuario:** Repite la característica.

**Asistente:** La pregunta era cómo quieres que se comporte la app cuando edites un atajo ya existente.

**Usuario:** No creo que haga falta versiones ninguna.

**Asistente:** Descartamos D. A, guardar directamente. B, pedir confirmación. C, guardar directo y permitir deshacer el último cambio.

**Usuario:** Yo lo haría más simple. Lo guardaba directamente.

**Asistente:** Decisión 36: A.

### Decisión 37 · Desactivar temporalmente

**Asistente:** A, no se puede. B, activar o desactivar por atajo. C, por carpeta. D, ambas cosas. Yo recomendaría D.

**Usuario:** La D. La B.

**Asistente:** Queda la 37 con la opción B: activar o desactivar por atajo.

### Decisión 38 · Duplicar atajo

**Asistente:** A, no hay duplicado. B, duplicar y obligar a cambiar código. C, duplicar abriendo copia editable con código vacío. D, opciones avanzadas. Te propongo C.

**Usuario:** La B me parece bien, pero si tú crees que la C es mejor...

**Asistente:** Pensándolo bien, la B encaja mejor. Duplicas todo y solo obliga a cambiar el código antes de guardar.

### Decisión 39 · Exportación

**Asistente:** A, exportar todo. B, por atajo. C, por carpeta. D, todo lo anterior. Te sugiero D.

**Usuario:** Repite la pregunta.

**Asistente:** Cómo quieres poder exportar atajos: todo, uno individual, una carpeta o las tres posibilidades.

**Usuario:** Sí, la D.

### Decisión 40 · Formato de exportación

**Asistente:** A, solo JSON. B, solo CSV. C, ambos. D, JSON principal y CSV simplificado. Te sugiero D.

**Usuario:** Quiero hacerlo simple. Creo que un CSV es suficiente.

**Asistente:** Inicialmente interpreté mal tu respuesta, pero luego se corrigió: decisión 40, solo CSV.

### Decisión 41 · Conflictos en importación

**Asistente:** Importar desde JSON: A, directo. B, vista previa. C, vista previa y detección de conflictos. D, vista previa, conflictos y selección individual. Te sugiero C.

**Usuario:** Repite la pregunta.

**Asistente:** Cómo debería funcionar la importación desde un archivo: directa, con vista previa, con detección de conflictos o con selección individual.

**Usuario:** Me parece que esta pregunta la hemos hablado anteriormente.

**Asistente:** Parte ya estaba cubierta. Reformulo solo para conflictos durante una importación: A, cancelar todo. B, saltar los conflictivos. C, decidir uno a uno. D, sobrescribir todo. Te recomiendo C.

**Usuario:** Sí, corrige también la 40 a solo CSV.

**Asistente:** Queda la 40 en solo CSV y la 41 en C, decidir caso a caso si hay conflicto.

### Decisión 42 · Estructura de carpetas al exportar

**Asistente:** ¿Exportar incluye también la estructura de carpetas? A, nunca. B, siempre. C, opcional. D, depende del formato. Te diría C.

**Usuario:** Quiero hacerlo simple.

**Asistente:** Lo más simple sería B: siempre incluir la estructura.

### Decisión 43 · Creación masiva

**Asistente:** A, no se permite. B, pegando varias líneas. C, importando desde CSV. D, ambas. Si quieres mantenerlo simple, la dejaría fuera.

**Usuario:** ¿A qué te refieres?

**Asistente:** Crear muchos atajos de una sola vez, por ejemplo pegando una lista o importando un archivo preparado.

**Usuario:** Eso es como una opción de importación.

**Asistente:** Tienes razón. Eliminamos esta decisión por redundante con la importación CSV.

**Usuario:** Sí, mejor.

### Decisión 44 · Compatibilidad entre versiones

**Asistente:** A, solo usable en la misma app y versión. B, compatible entre versiones. C, compatible y documentada. D, compatible con otras herramientas. Te sugiero B.

**Usuario:** Sí, solo compatibilidad entre versiones.

### Decisión 45 · Compartir entre usuarios

**Asistente:** A, no prevista. B, exportar e importar archivo. C, enlace compartible. D, repositorio común. Por simplicidad, propongo A.

**Usuario:** Sí, es lo correcto.

### Decisión 46 · Otros sistemas operativos

**Asistente:** A, solo sistema operativo principal. B, multisistema desde el inicio. C, solo uno ahora pero preparando el camino. D, versiones separadas.

**Usuario:** ¿Te refieres a compatibilidad entre Windows y Mac, por ejemplo?

**Asistente:** Exacto.

**Usuario:** De momento, solo Windows.

**Asistente:** Decisión 46: A.

### Decisión 47 · Idiomas

**Asistente:** A, solo español. B, español e inglés. C, preparado para traducir pero solo un idioma ahora. D, multidioma desde el inicio. Yo haría solo español por ahora.

**Usuario:** Yo pondría la C, preparado para traducir, pero no ahora.

### Decisión 48 · Accesibilidad mínima

**Asistente:** A, sin consideraciones específicas. B, tamaño de fuente ajustable. C, atajos de teclado en la propia app. D, ambas cosas. Recomendaría D.

**Usuario:** La D.

### Decisión 49 · Requisitos de Windows

**Asistente:** A, última versión de Windows. B, soportar dos versiones atrás. C, soportar tres o más. D, sin definir. Propongo B.

**Usuario:** La B.

### Decisión 50 · Mecanismo de actualización

**Asistente:** A, actualizaciones manuales. B, aviso de nueva versión pero instalación manual. C, actualización automática opcional. D, actualización obligatoria. Para mantenerlo simple, te diría A.

**Usuario:** La B me parece interesante: aviso de una nueva versión, pero actualización manual.

### Decisión 51 · Registro de errores

**Asistente:** A, no guardar nada. B, guardar solo errores técnicos mínimos. C, errores más eventos de uso. D, todo lo posible. Te sugeriría B.

**Usuario:** Explícame la B.

**Asistente:** Guardar solo errores técnicos mínimos, por ejemplo fallos al arrancar o al cargar un archivo, sin datos de uso ni información sensible.

**Usuario:** Eso lo estás diciendo tú. Si fuera una aplicación que yo fuera a comercializar y tuviera mucho recorrido... pero en principio va a ser de uso puramente personal y en algún caso se lo compartiré a algún compañero, nada más allá de eso.

**Asistente:** Si es solo para uso personal y muy limitado, podemos simplificar y dejarlo en A, sin registros.

**Usuario:** Yo diría que sí, pero si tú crees que sería bueno para averiguar después qué está fallando, pues lo que tú digas. Pero que sea simple.

**Asistente:** Yo dejaría A, sin registros, y si en algún momento necesitas diagnosticar algo puntual, se replantea.

**Usuario:** Vale, opción A.

### Decisión 52 · Modo de ejecución

**Asistente:** A, siempre en segundo plano. B, iniciar con el sistema operativo. C, abrir solo bajo demanda. D, configurable. Por simplicidad propongo B.

**Usuario:** La B, siempre con el sistema.

### Decisión 53 · Notificaciones

**Asistente:** A, ninguna. B, solo errores críticos. C, errores y avisos útiles. D, totalmente configurable. Por mantenerlo simple, tiraría por A.

**Usuario:** Repite la pregunta.

**Asistente:** Cómo quieres gestionar las notificaciones: ninguna, solo errores críticos, errores y avisos útiles o configurable.

**Usuario:** Si falla me gustaría saber qué está fallando, qué problema hay.

**Asistente:** Entonces B, solo errores críticos.

**Usuario:** Okay.

### Decisión 54 · Copia de seguridad automática

**Asistente:** A, no. B, sí, en local. C, sí, en la nube. D, ambas.

**Usuario:** Creo que esto también lo miramos en preguntas anteriores. Revísalo.

**Asistente:** Correcto. Se elimina esta decisión como independiente y se mantienen las decisiones anteriores sobre copias de seguridad.

### Decisión 55 · Cierre de la ventana

**Asistente:** A, cerrar del todo. B, minimizar a la bandeja del sistema. C, preguntar siempre. D, configurable. Como hemos elegido que arranque con el sistema, recomendaría B.

**Usuario:** Sí.

### Decisión 56 · Privacidad de datos de uso

**Asistente:** A, no se recoge nada. B, métricas anónimas mínimas. C, métricas ampliadas. D, configurable. Elegiría A.

**Usuario:** Sí, la A.

### Decisión 57 · Permisos del portapapeles

**Asistente:** A, acceso siempre que la app esté activa. B, solo cuando se usa un atajo. C, preguntar la primera vez. D, configurable.

**Usuario:** Revisa esto porque creo que también hablamos algo parecido.

**Asistente:** Se parece al mecanismo alternativo de portapapeles. Reformulé la pregunta, pero finalmente quedó claro que era redundante con la decisión 25.

**Usuario:** Creo que hablamos de esto cuando discutimos si era coherente utilizar el portapapeles si fallaba o no. Dime qué decidimos.

**Asistente:** Esta decisión 57 es redundante. Se elimina y se mantiene la decisión anterior de no usar el portapapeles como mecanismo alternativo.

### Decisión 58 · Fallo al expandir

**Asistente:** Si un atajo falla al ejecutarse: A, no mostrar nada. B, mostrar aviso discreto. C, error detallado. D, configurable. Propongo B.

**Usuario:** Me vale, pero me suena a que lo hablamos anteriormente.

**Asistente:** Se parece a la decisión 53, pero aquí se refiere al fallo concreto durante la ejecución del atajo. Lo simplifico: mostrar un aviso discreto en el momento, sin notificación persistente.

**Usuario:** Vale.

### Decisión 59 · Tema visual

**Asistente:** A, solo modo claro. B, solo oscuro. C, claro y oscuro. D, configurable con sistema. Por simplicidad, diría A.

**Usuario:** Aquí no; aquí me gustaría configurarlo con el sistema.

**Asistente:** Decisión 59: D.

### Decisión 60 · Restauración tras fallo inesperado

**Asistente:** A, no restaurar nada. B, intentar restaurar automáticamente. C, preguntar al iniciar. D, siempre avisar y dejar elegir. Te sugiero C.

**Usuario:** ¿Cómo es eso de restaurar? ¿Qué implica?

**Asistente:** Si la aplicación se cierra de forma inesperada, al volver a abrirla te preguntaría si quieres recuperar el estado anterior.

**Usuario:** Pero ¿qué aporta? ¿Tiene que estar pendiente de cada movimiento y haciendo copia de seguridad continua o simplemente recupera la última copia?

**Asistente:** Para que esa restauración tenga sentido, la app tendría que guardar estado temporal de forma continua, lo que añade complejidad. Como ya decidimos guardar cambios directamente y tener copias aparte, propongo eliminar esta decisión y no añadir restauración tras fallo.

**Usuario:** Sí, creo que es lo más lógico.

### Decisión 61 · Ayuda inicial

**Asistente:** A, ninguna. B, pantalla introductoria breve. C, tutorial paso a paso. D, tutorial opcional más ayuda contextual. Por simplicidad diría A.

**Usuario:** La verdad es que la que me gusta es la B.

### Decisión 62 · Documentación en la app

**Asistente:** A, ninguna. B, sección de ayuda básica. C, documentación completa dentro de la app. D, documentación externa. Por simplicidad diría B.

**Usuario:** Sí, la B es la más simple.

### Decisión 63 · Aplicación de cambios

**Asistente:** A, aplicar cambios al instante. B, aplicar al reiniciar. C, preguntar si aplicar ahora o luego. D, configurable. Por simplicidad recomiendo A.

**Usuario:** Sí, lo mejor.

### Decisión 64 · Texto enriquecido

**Asistente:** A, solo texto plano. B, mantener formato siempre. C, elegir por atajo. D, configurable globalmente. Por simplicidad sugiero A.

**Usuario:** Me encaja y creo que hablamos de esto previamente. Mira qué decidimos.

**Asistente:** No había una decisión idéntica sobre mantener formato, aunque sí sobre tipo de contenido. Yo la cerraría como A, solo texto plano.

**Usuario:** No, yo quiero hacer primero un texto plano pero después tener posibilidad de mantener formato.

**Asistente:** Entonces: MVP solo texto plano, con previsión explícita de soportar texto enriquecido más adelante.

**Usuario:** Sí.

### Decisión 65 · Tamaño máximo del contenido

**Asistente:** A, sin límite práctico. B, límite razonable no configurable. C, límite configurable. D, límite bajo fijo. Elegiría B.

**Usuario:** Sí, un límite razonable interno.

### Decisión 66 · Campos donde no se puede pegar

**Asistente:** A, no hacer nada. B, mostrar aviso de que no se puede. C, intentar método alternativo. D, configurable. Por simplicidad propongo B.

**Usuario:** Sí, perfecto.

### Decisión 67 · Teclados no estándar

**Asistente:** A, sin soporte específico. B, compatibilidad básica con distribuciones comunes. C, soporte ampliado desde el inicio. D, configurable. Por simplicidad elegiría A.

**Usuario:** Sí. La verdad es que no entiendo qué es eso de teclados no estándar.

**Asistente:** Distribuciones menos comunes, como Dvorak, Colemak o personalizadas. Con A no hacemos nada especial; funciona con lo que el sistema soporte.

**Usuario:** Sí, sin complicarnos más, con el estándar de ahora.

### Decisión 68 · Activación cuando coincide con texto normal

**Asistente:** A, expandir siempre. B, expandir solo si hay delimitador como espacio o intro. C, expandir solo con tecla de confirmación. D, configurable. Recomiendo B.

**Usuario:** De nuevo creo que hablamos de esto, confírmalo.

**Asistente:** Se parece a decisiones anteriores. Mantendría B para evitar sorpresas.

**Usuario:** Me gusta la configurable, aunque para el MVP la opción B es suficiente.

**Asistente:** MVP B, delimitador; futuro configurable.

### Decisión 69 · Campos sensibles

**Asistente:** A, expandir siempre. B, bloquear siempre en esos campos. C, advertir y dejar elegir. D, configurable. Sugiero B.

**Usuario:** Sí, de acuerdo.

### Decisión 70 · Pérdida de foco

**Asistente:** A, cancelar silenciosamente. B, cancelar con aviso. C, intentar continuar. D, configurable. Yo iría con A.

**Usuario:** Sí.

### Decisión 71 · Versionado interno de la base de datos

**Asistente:** A, sin versionado interno. B, versionado simple automático. C, versionado con historial visible. D, configurable. Por simplicidad y teniendo ya copias, diría A, sin versionado interno.

**Usuario:** Okay. ¿Qué es lo siguiente?

**Asistente:** El siguiente paso es cerrar el bloque de las 71 decisiones, depurando redundancias y consolidando las que han cambiado. El entregable debería ser un documento maestro con todas las decisiones limpias, trazables y coherentes. Después, pasar a diseño funcional detallado o arquitectura del MVP.

---

## 4. Recuperación y consolidación de las decisiones iniciales

**Usuario:** Vamos a dar el siguiente paso.

**Asistente:** Al revisar el material disponible, detecté que no tenía acceso claro al tramo 1–27 y propuse recuperarlo antes de consolidar.

**Usuario:** Sí, claro, tendrá que buscarlo en el historial de esta conversación.

**Asistente:** Se aclaró que, aunque el usuario podía ver el historial completo, yo no tenía acceso fiable a todo el tramo desde el contexto cargado y pedí pegar las primeras decisiones.

**Usuario:** Tengo que hacerlo desde el ordenador porque desde aquí es imposible.

**Asistente:** Se acordó retomarlo cuando pudiera pegar esos fragmentos.

**Usuario:** Posteriormente fue pegando los fragmentos correspondientes a las decisiones 1–22 y se reconstruyeron de forma progresiva las decisiones faltantes.

**Asistente:** A medida que se recuperaron, se identificaron además varias decisiones posteriores que sustituían o matizaban otras anteriores; por ejemplo, organización por carpetas frente a carpetas+etiquetas en el MVP, CSV frente a JSON como formato de intercambio y eliminación de logs persistentes.

---

## 5. Contexto fundador aportado de nuevo

**Usuario:** Te voy a pasar también mi primera idea de la aplicación y sobre la que partimos con todas estas preguntas:

> Quiero hacer una aplicación de escritorio que pueda instalarse en distintos ordenadores, similar a PhraseExpress, donde un código corto se convierta en un texto largo; que pueda organizarse por temas, personalizar dónde se ejecuta y sincronizarse mediante un archivo en una ubicación como Google Drive.

**Asistente:** Esto fija el contexto fundador del proyecto: aplicación de escritorio para Windows, instalable mediante ejecutable, orientada inicialmente a uso personal y ocasionalmente compartida con algún compañero; función principal de expansión de códigos cortos; organización por temas; reglas de ámbito; sincronización entre equipos; y definición funcional previa a la arquitectura.

**Asistente:** También se mantuvo C# + .NET como hipótesis técnica inicial, no como arquitectura cerrada.

---

## 6. Creación del proyecto documental

**Usuario:** Me gustaría guardar esto en un proyecto para luego pasar a Codex. ¿Te parece?

**Asistente:** Sí. Se propuso separar documentación y código y crear una estructura inicial con `docs/`, `src/`, `tests/` y un `README.md`. Se indicó que los documentos funcionales debían depurarse antes de pasarlos a Codex para evitar requisitos contradictorios.

**Usuario:** De acuerdo, genera los dos primeros archivos md y ve creando las carpetas y el proyecto.

**Asistente:** Se generaron:

- `00_CONTEXTO_Y_OBJETIVO.md`
- `01_DECISIONES_FUNCIONALES.md`

junto con la estructura inicial del proyecto.

**Usuario:** TextExpander me parece muy ajeno. Busquemos un nombre en español.

**Asistente:** Se propusieron varios nombres en español relacionados con atajos, escritura y expansión de texto.

**Usuario:** AtajaMe, ¿qué te parece?

**Asistente:** Se consideró adecuado por ser corto, memorable y directamente relacionado con atajos. Se propuso usar `AtajaMe` como marca visual y evitar la tilde en ejecutables, repositorios, paquetes y rutas.

**Usuario:** Sí, me parece mejor AtajaMe. Comienza con el archivo 02. Y envíamelo para descargarlo y unirlo al resto del ZIP.

**Asistente:** Se generó `02_ESPECIFICACION_FUNCIONAL_MVP.md` como fuente funcional de autoridad para el desarrollo, con requisitos funcionales, alcance, fuera de alcance, criterios de aceptación y cuestiones reservadas para arquitectura.

---

## 7. Orden de autoridad resultante

A partir de la documentación generada, se estableció el siguiente orden de autoridad:

1. `02_ESPECIFICACION_FUNCIONAL_MVP.md`
2. `01_DECISIONES_FUNCIONALES.md`
3. `00_CONTEXTO_Y_OBJETIVO.md`
4. conversación original como fuente de trazabilidad

Este archivo conserva precisamente el cuarto nivel: la conversación original relevante para el proyecto.

