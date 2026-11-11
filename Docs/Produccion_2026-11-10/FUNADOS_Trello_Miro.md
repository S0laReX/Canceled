# FUNADOS — Sistema de producción en Trello y Miro

Versión 1.0 · 10 de noviembre de 2026 · Guía operativa editable

## 1. Resultado y reglas de trabajo

Este sistema convierte el GDD en trabajo semanal verificable. Trello registra compromisos, responsables, bloqueos y evidencias. Miro explica decisiones, relaciones y referencias visuales. El GDD conserva las reglas aprobadas del juego; Git conserva sus implementaciones y versiones. Una tarjeta cerrada debe apuntar a la evidencia que demuestra su resultado.

**Base existente que se debe preservar:** la inspección del proyecto `D:/Funados` identifica Unity 6000.5.7f1 y un README de **Cancel Brawl beta 0.3**, con movimiento libre 3D, cinco personajes (Don Decreto, Lola Loop, Coach Tóxico, Cripto King y Tío Antena) y cuatro arenas (Azotea Neón, Mall del Ego, Campus en Llamas y Núcleo Cripto). Estos datos describen lo encontrado, no una validación de que todas las funciones pasen sus pruebas. El primer trabajo es probar esa base, registrar su estado y decidir qué se reutiliza.

**Dirección confirmada por el usuario:** juego de lucha de plataformas **3D con movimiento libre**, con desplazamiento sobre X/Z y altura Y, para Windows, modelos cartoon-anime y sombreado cel. La entrega enfocada valida dos jugadores locales, dos luchadores y un escenario; los candidatos editoriales son **Don Promesio** y **Lola Viral**, aprovechando los assets equivalentes Don Decreto y Lola Loop. **Azotea Neón** es candidata de reutilización para la arena piloto; **El Gran Debate** queda como propuesta de escenario futuro. FND-005 registra estas decisiones y confirma la selección de assets, sin volver a abrir la orientación 3D. No renombrar carpetas ni assets automáticamente para cambiar nombres visibles. Se descarta la conversión lateral 2.5D. Los trece arquetipos pertenecen a la visión de contenido; ampliar el roster, publicar online y añadir campañas no son requisitos de esa entrega. Si el GDD cambia una cifra o un alcance, actualizar las tarjetas afectadas antes de comprometer el siguiente sprint.

Los materiales adjuntos son preparación local. No equivalen a tableros creados, tarjetas importadas ni cuentas conectadas. No se incluyen contraseñas, tokens, enlaces ficticios ni importadores de terceros.

### Archivos de trabajo

| Archivo | Uso |
|---|---|
| `FUNADOS_Trello_tarjetas.csv` | 42 tarjetas de ejemplo con identificador, estado inicial, prioridad, hito, descripción, aceptación, dependencias y evidencia esperada |
| `FUNADOS_Miro_notas.csv` | 48 notas para distribuir en ocho marcos; cada fila contiene el texto de una nota |
| `FUNADOS_Trello_Miro.md` | Reglas del sistema, plantillas y procedimientos de preparación |

No usar el CSV como lista de tareas obligatorias que deban terminarse en el primer sprint. Es un backlog inicial para ordenar, recortar y estimar con evidencia.

## 2. Preparar Trello

Crear un tablero privado llamado **FUNADOS — Producción**, dentro del espacio de trabajo elegido. Antes de invitar colaboradores, confirmar quién necesita ver el concepto, las referencias y los archivos. No depende de campos personalizados, automatizaciones de pago ni Power-Ups.

### Listas de izquierda a derecha

| Lista | Qué contiene | Regla de salida |
|---|---|---|
| 00 — Guía y plantillas | Objetivo del producto, GDD, enlaces, Definition of Done y tarjetas modelo | Se mantiene estable; no cuenta como trabajo en curso |
| 01 — Ideas y después | Roster futuro, online, modos adicionales y propuestas sin validar | Solo entra al backlog tras justificar valor y coste |
| 02 — Backlog priorizado | Trabajo entendido y ordenado por valor/dependencia | Tiene aceptación, tamaño y dependencias explícitas |
| 03 — Sprint comprometido | Trabajo que cabe en la capacidad de las dos semanas | Cumple Definition of Ready y comienza cuando hay hueco |
| 04 — En curso | Trabajo que se está ejecutando ahora | Produce una versión o evidencia revisable |
| 05 — Validación | Trabajo terminado por su autor y pendiente de comprobar en una build | Pasa aceptación y Definition of Done |
| 06 — Bloqueado | Tarjetas con impedimento concreto | Tiene causa, acción de desbloqueo y próximo control |
| 07 — Hecho | Resultados aceptados y enlazados | No se reabre sin registrar motivo; defectos nuevos son tarjetas nuevas |

Al acabar un hito, archivar sus tarjetas terminadas desde Trello cuando ya se haya guardado el resumen y la evidencia. Archivar conserva la trazabilidad; no borrar registros del proyecto.

### Etiquetas y convenciones

Asignar **una etiqueta de área** y, cuando haga falta, una etiqueta de riesgo. La prioridad se escribe en el título para que sea visible sin depender del color.

| Nombre | Color sugerido | Ejemplo |
|---|---|---|
| Diseño | Amarillo | Reglas de stocks, identidad del roster |
| Gameplay | Azul | Movimiento, combate, cámara |
| Arte 3D | Morado | Modelado, rig, materiales, escenario |
| Audio | Verde | Mezcla, señales y música |
| UX y accesibilidad | Rosa | Menús, remapeo, legibilidad |
| QA y rendimiento | Naranja | Perfiles, regresiones y sesiones |
| Producción | Gris | Backups, documentación, releases |
| Riesgo / bloqueo | Rojo | Licencia incierta, incompatibilidad, dependencia |

Formato de título: **`FND-012 [P0] Resolver knockback y control de lanzamiento`**. P0 bloquea el siguiente gate o una build utilizable; P1 mejora la experiencia comprometida; P2 es opcional y se corta primero. No usar rojo como única señal: escribir también `BLOQUEO:` en la descripción y mover la tarjeta a la lista correspondiente.

No hacen falta etiquetas separadas para cada personaje, sprint y estado. Escribir esos datos en la descripción y usar el ID para localizar todas las referencias.

### Definition of Ready: antes de entrar a un sprint

- La tarjeta expresa un resultado observable y su valor para el jugador o la producción.
- Hay una persona responsable, un criterio de aceptación verificable y una dependencia identificada o la palabra «ninguna».
- Incluye estimación pequeña. Si supera dos jornadas de trabajo efectivo, dividir por resultado: movimiento horizontal, salto y recuperación pueden ser tarjetas distintas.
- Tiene referencia al GDD o a una decisión de Miro y especifica plataforma/dispositivo cuando afecta a input o rendimiento.
- Las dudas de versión, licencia, permisos o acceso se resuelven antes de comprometer integración.

### Definition of Done: para mover a Hecho

- Se cumplen **todos** los criterios de aceptación propios de la tarjeta.
- La función se ha probado en una build de Windows cuando afecta a la experiencia del jugador; anotar build y dispositivos.
- No introduce errores nuevos de compilación ni bloqueos conocidos en el recorrido afectado.
- El cambio está guardado en Git o, si es un documento/asset externo, en una ubicación respaldada y versionada.
- Existe evidencia adecuada: vídeo corto, captura, informe de pruebas, perfil o documento revisado. No se exige vídeo para un simple ajuste documental.
- Se actualizan las reglas o instrucciones que el cambio modifica; el GDD no contradice la build.
- Los assets incorporados tienen autor/origen/licencia registrados y cumplen el presupuesto acordado o una excepción documentada.
- Se verifica accesibilidad aplicable: remapeo, texto legible, señales además del color, sensibilidad visual y sonido.
- Se añade enlace al commit, build o entrega; se describe cualquier limitación aceptada y se crea seguimiento si lo necesita.

Para un desarrollador individual: hacer una revisión propia separada de la implementación, volver a la tarjeta con la build cerrada y recorrer su aceptación. Para cerrar un hito de jugabilidad, añadir al menos una sesión con otra persona. No marcar «aceptado» solo por haberlo programado.

## 3. Importar el material sin servicios adicionales

### Trello: crear tarjetas por lotes, después enriquecerlas

Trello documenta que no dispone de un importador genérico; permite pegar una columna de títulos para crear varias tarjetas. Por ello, el CSV entregado es una fuente estructurada para copiar campos, no un archivo de importación nativa completa. Abrirlo como UTF-8 y separado por comas en Excel, LibreOffice o una hoja ya disponible. Filtrar `Lista`, copiar solo `Titulo` sin encabezado, pegar en «Añadir tarjeta» de esa lista y elegir crear una tarjeta por línea. Probar primero con dos títulos y comprobar el resultado. [Procedimiento oficial de importación](https://support.atlassian.com/trello/docs/importing-data-into-trello/).

Después de crear títulos:

1. Abrir cada tarjeta y copiar `Descripcion`, `Aceptacion`, `Dependencias` y `Evidencia` a su descripción utilizando la plantilla de abajo.
2. Aplicar la etiqueta de `Area`, asignar responsable y conservar `Hito`, `Prioridad` y `Estimacion` como texto. La estimación del CSV es tamaño relativo inicial, no horas contratadas.
3. Convertir cada criterio de `Aceptacion`, separado por ` | `, en un ítem de checklist. Mantener la aceptación legible también en la descripción si resulta más sencillo.
4. Pegar las URL reales al crearlas. Dejar vacío un vínculo que todavía no existe.
5. No asignar fechas a todo el backlog. Fijar vencimientos solo a compromisos reales del sprint o a eventos acordados.
6. Verificar cinco tarjetas: ID único, lista correcta, aceptación íntegra, dependencia válida y ausencia de duplicados. Repetir esta comprobación al cambiar el método de importación.

Para usar una plantilla, crear una tarjeta modelo, abrir su menú y convertirla en plantilla mediante la opción correspondiente. También se puede copiar una tarjeta y sus checklists. Mantener las plantillas en «00 — Guía y plantillas», claramente tituladas. [Plantillas oficiales](https://support.atlassian.com/trello/docs/creating-template-cards/), [copiar tarjetas y checklists](https://support.atlassian.com/trello/docs/copying-cards-lists-or-boards).

No se instala un importador CSV ni se sube el backlog a otra empresa. Una futura integración mediante API requiere autorización y manejo seguro de credenciales; no es necesaria para empezar.

### Miro: del CSV a notas editables

Abrir `FUNADOS_Miro_notas.csv` en una hoja de cálculo. Filtrar por `Marco`, copiar **solo las celdas de `Texto`**, sin encabezado, y pegar sobre el lienzo del marco correspondiente. Elegir «Paste as sticky notes» si se ofrece. Cada celda se convierte en una nota editable. Hacer una prueba de dos notas antes del lote. La guía oficial permite este flujo desde hojas de cálculo; aquí se usan lotes de seis notas por marco. [Notas adhesivas de Miro](https://help.miro.com/hc/en-us/articles/360017572054-Sticky-notes).

No arrastrar el CSV esperando que reconstruya automáticamente marcos, flechas, posiciones o colores. Las columnas `Orden`, `Color` y `Tarjeta` son instrucciones de montaje manual. Si Miro pega texto simple, copiar las celdas desde una hoja en lugar de copiar el archivo crudo; alternativamente, utilizar el modo de creación masiva de notas de Miro con una nota por línea.

## 4. Plantillas listas para copiar

### Plantilla de entrega

```text
ID:
Resultado para el jugador o para producción:
Responsable:
Área / prioridad / hito:
Referencia GDD:
Referencia Miro:
Tamaño: S (hasta media jornada), M (hasta una jornada), L (hasta dos jornadas)
Dependencias:

ALCANCE
Qué se entrega:
Qué queda para una tarjeta posterior:

ACEPTACIÓN
- Dado [estado], cuando [acción], entonces [resultado observable].
- Dado [caso límite], entonces [respuesta esperada].

VALIDACIÓN
Build / commit:
Dispositivo y entorno:
Pasos ejecutados:
Resultado y evidencia:
Limitaciones aceptadas:

CIERRE
Definition of Done revisada:
Documentación actualizada:
Seguimientos:
```

### Plantilla de defecto

```text
ID y título: FND-XXX [P0/P1/P2] [fallo observable]
Build / commit y fecha:
Plataforma / mando / número de jugadores:
Frecuencia: siempre, N de M intentos o intermitente
Pasos mínimos para reproducir:
Esperado:
Observado:
Vídeo / captura / registro:
Impacto: bloquea jugar, altera reglas, reduce claridad o detalle cosmético
Primera versión conocida con el fallo:
Solución temporal, si existe:
Aceptación: pasos originales y caso próximo pasan en build indicada
```

### Plantilla de experimento o decisión

```text
Pregunta única:
Hipótesis:
Alternativas A y B:
Tiempo máximo: 0,5 a 1 jornada
Prueba y participantes:
Métrica y umbral decidido antes de observar:
Resultado:
Decisión y razón:
Referencia al marco de Miro / regla del GDD:
Qué tarea concreta nace o se cancela:
```

### Plantilla de asset 3D

```text
Uso: personaje / escenario / prop / efecto
Silhouette e identidad visual:
Referencia aprobada:
Origen, autor, herramienta y licencia:
Archivos fuente y formato de intercambio:
Escala / orientación / pivote acordados:
Presupuesto: triángulos, materiales, texturas y huesos
Rig, animaciones y deformaciones que comprobar:
Importación Unity y material URP:
Aceptación: lectura en cámara de combate, contacto con suelo,
deformación, colisiones separadas del detalle visual y rendimiento
Evidencia y ubicación versionada:
```

## 5. Sprints y ritmo sostenible

Trabajar en ciclos de dos semanas. El sprint tiene un objetivo jugable, no una colección de paquetes instalados. Primer objetivo sugerido: «dos mandos controlan dos cápsulas y una puede sacar a la otra del escenario». El siguiente: «se puede terminar, reiniciar y repetir una partida local sin reiniciar la aplicación».

### Capacidad y límites de trabajo

1. Anotar horas disponibles reales de las dos semanas. Restar compromisos personales, aprendizaje, documentación y revisión. Comprometer como máximo alrededor del 70% de la capacidad efectiva restante; reservar el resto para fallos y dudas.
2. Estimar con S/M/L y recalibrar tras dos sprints. No convertir puntos en una velocidad prometida. El CSV usa esas tallas para detectar tarjetas grandes, no para pronosticar una fecha de lanzamiento.
3. Una persona mantiene como máximo **una tarjeta En curso** y **una en Validación**. Un equipo pequeño puede aumentar En curso a una por persona; Validación no debe acumular más de dos. Bloqueado no sirve para iniciar trabajo ilimitado.
4. Si una tarea sigue bloqueada una jornada, crear o ejecutar una acción de desbloqueo, reducirla o renegociar el sprint. Registrar la causa y la próxima revisión en la tarjeta.
5. No añadir un personaje, plugin o modo nuevo a mitad del sprint sin retirar trabajo equivalente y anotar la decisión.

### Cadencia propuesta

| Momento | Duración | Acción y resultado |
|---|---|---|
| Inicio de sprint | 45–60 min | Revisar gate, escoger objetivo, dependencias y capacidad; dejar 3–6 entregas pequeñas listas |
| Inicio de cada sesión | 5 min | Abrir Trello; elegir la única tarjeta activa y escribir el resultado esperado del día |
| Final de cada sesión | 5–10 min | Guardar y respaldar; comentar avance, bloqueo y evidencia; actualizar lista |
| Mitad de semana | 20–30 min | Probar la build y revisar legibilidad, control y errores; ajustar prioridades |
| Fin de primera semana | 30 min | Revisar dependencias y alcance; recortar a tiempo si el objetivo está en riesgo |
| Fin de sprint | 45–60 min | Demostración de build, aceptación, métricas y decisiones; nada se acepta solo con capturas del editor |
| Después de la demo | 20 min | Retrospectiva: conservar una práctica, corregir un problema y elegir una mejora concreta |

La bitácora semanal tiene seis líneas: build jugada, objetivo, entregas aceptadas, problema principal, decisión y siguiente prueba. Las reuniones se pueden reemplazar por una nota individual cuando trabajas solo; la evidencia sigue siendo obligatoria.

### Secuencia sugerida por gates

| Hito | Resultado | Gate para continuar |
|---|---|---|
| H0 — Base protegida y decisiones registradas | Entorno identificado, copia recuperable, beta existente evaluada y selección de reutilización documentada | Restauración y apertura comprobadas; 3D libre y nombres editoriales registrados; no migración lateral |
| H1 — Combate validado | Dos luchadores seleccionados, movimiento elegido, recuperación, daño y eliminación | Partida local repetible; controles y reglas observables; reutilización de sistemas que ya pasan |
| H2 — Partida completa | Stocks, menú local, selección, pausa, rematch, entrenamiento mínimo | Recorrido completo en build Windows con dos dispositivos |
| H3 — Corte visual | Dos identidades, un escenario, rig/animación y material cel coherentes | Siluetas y ataques se leen; assets licenciados y dentro de presupuesto |
| H4 — MVP probado | Balance inicial, accesibilidad base, audio, perfiles y pruebas externas | Criterios MVP del GDD y cero bloqueos críticos conocidos |
| H5 — Viabilidad posterior | Decisión de expansión y prueba técnica online aislada | No comprometer roster completo ni online comercial sin coste y validación |

Estos hitos son dependencias, no semanas de calendario. Dos sprints pueden terminar H1 o hacer falta más. Para una sola persona, revisar fechas después de una vertical slice real. Priorizar FND-001 a FND-014 en el orden de sus dependencias; el backlog no exige cerrar catorce tarjetas en un sprint.

## 6. Tarjetas de muestra y trazabilidad

El CSV contiene las descripciones completas. Tres ejemplos muestran el nivel esperado:

**FND-012 [P0] Resolver knockback 3D y trayectorias.** Entrega: daño acumulado modifica un lanzamiento reproducible con azimut horizontal X/Z y elevación Y definidos en datos. Aceptación: repetir el mismo ataque a 0%, 50% y 100% aumenta la velocidad de lanzamiento según fórmula; el peso relativo diferencia a ambos luchadores; probar ocho direcciones horizontales y distintas alturas; no produce velocidades no válidas. Evidencia: vídeo del caso y tabla de parámetros. Dependencias: FND-010 y FND-011. La influencia direccional es una candidata posterior y no se exige para cerrar esta tarjeta.

**FND-023 [P1] Validar silueta de Don Promesio y Lola Viral.** Entrega: evaluar primero los diseños equivalentes de Don Decreto y Lola Loop que existen; cambiar identidad visible mediante datos, sin renombrar carpetas ni assets de la beta. Aceptación: diseños originales reconocibles sin color, sin retratos de personas reales; distinguirlos desde los ángulos de la cámara 3D a la escala de juego; identificar frente/espalda y dirección del ataque; evitar accesorios que tapen hurtboxes; documentar fuentes/licencias. Evidencia: lámina de siluetas y captura con cámara de combate. La animación y el modelo final pertenecen a otras tarjetas.

**FND-036 [P0] Ejecutar sesión externa del MVP.** Entrega: prueba guiada por preguntas con al menos dos personas ajenas a la implementación. Aceptación: registrar dispositivo y build, observar si pueden iniciar una partida, recuperar una caída, explicar stocks y hacer rematch; separar observación de sugerencia; convertir bloqueos en tarjetas. Evidencia: notas anonimizadas y decisión de gate. No subir grabaciones de participantes a cuentas compartidas sin su consentimiento.

## 7. Preparar Miro como mapa del juego

Crear un tablero privado **FUNADOS — Diseño y decisiones**. Un solo tablero con marcos sirve para comenzar. Si crece demasiado, separar Arte y Pruebas en tableros adicionales cuando el plan y el equipo lo permitan; no se presupone un límite concreto de plan. Las funciones disponibles se revisan en la cuenta antes de depender de ellas.

### Ocho marcos y orden de lectura

| Marco | Contenido | Pregunta que responde |
|---|---|---|
| M01 — Visión y límites | Beta existente, pitch, pilares, 3D libre confirmado, MVP, identidad y reglas de sátira | ¿Qué preservamos y qué probamos primero? |
| M02 — Loops y partida | Acción de segundos, ronda, revancha y práctica | ¿Qué decide y aprende el jugador? |
| M03 — Sistemas y datos | Input, simulación, combate, presentación, audio, UI | ¿Qué sistema depende de cuál? |
| M04 — Personajes y balance | Matriz de roles, roster futuro y fichas de los dos primeros | ¿Por qué se juegan distinto y cómo se les responde? |
| M05 — Escenario y arte | Greybox, distancias, cámara, referencias y pipeline | ¿Cómo se ve sin perder claridad? |
| M06 — Flujo de pantallas | Inicio, local, dispositivos, selección, combate, resultados | ¿Se puede jugar una sesión completa sin ayuda? |
| M07 — Roadmap y riesgos | Hitos, incertidumbres, coste y decisiones | ¿Qué prueba desbloquea la siguiente inversión? |
| M08 — Playtests y decisiones | Preguntas, resultados, cambios y evidencia | ¿Qué aprendimos y qué cambia? |

Nombrar cada grupo con su ID. Mantener títulos grandes, tipografía legible y espacio entre clusters. Color sugerido: amarillo = hipótesis, azul = regla aprobada, verde = evidencia, rosa = riesgo. Escribir también la categoría, porque el color no es suficiente.

### Mapas concretos que dibujar

**Loop de combate:** observar posición → elegir aproximación/defensa → ejecutar ataque → confirmar contacto y knockback → perseguir o recuperar → volver a posición. La eliminación conduce a respawn y cambio de stocks; al agotar stocks pasa a resultado y revancha. Dibujar flechas y marcar «lectura», «riesgo» y «respuesta» junto a cada decisión.

**Loop de mejora:** jugar → identificar una situación fallida → practicar una respuesta en entrenamiento → probarla en partida → observar resultado. Los cosméticos recompensan expresión; no alteran daño, alcance ni movilidad.

**Mapa de sistemas propuesto:** dispositivos → acciones de input → comandos por tick → motor de movimiento/estados → consulta de golpes → daño/knockback/stocks/escándalo → estado de partida. Desde el estado salen flechas a cámara, animación, VFX, audio y HUD. Comparar este mapa con el código real antes de abrir tareas de implementación. Anotar qué datos se configuran y qué sistema es su propietario. Mantener movimiento libre sobre X/Z y altura Y; representar orientación del personaje, vectores de ataque y límites volumétricos de eliminación. La influencia direccional queda como hipótesis posterior. No dibujar online como componente ya implementado; ponerlo en un recuadro de investigación futura.

**Mapa del escenario:** dibujar vista superior X/Z y vista de altura Y del suelo principal, plataformas, volúmenes de eliminación, posiciones de aparición, cámara máxima y márgenes seguros del HUD. Comparar a escala alcance del ataque, salto, recuperación y distancia al borde en las cuatro direcciones horizontales. Probar oclusión y lectura de profundidad desde todos los ángulos previstos. Cualquier prop 3D que parezca sólido debe comunicar si es jugable o fondo.

**Flujo de pantallas:** Inicio → Menú → Local → Conectar dispositivos → Elegir luchadores → Elegir escenario/reglas → Cuenta atrás → Combate → Resultados → Revancha / Cambiar luchadores / Menú. Pausa conduce a Reanudar, Opciones y Salir con confirmación apropiada. Una desconexión muestra una indicación que permite reconectar o salir. Opciones y remapeo deben poder abrirse antes de competir.

**Referencias visuales:** hacer columnas de silueta, proporciones, materiales, luz, VFX, UI y antiobjetivos. Cada imagen lleva URL de origen, autor cuando se conozca, razón específica para incluirla y condición de uso. «Referencia» no significa permiso para incorporar la imagen al juego. Separar concept art generado, propuesta y diseño aprobado; registrar herramienta/fecha/licencia de las salidas IA.

### Ficha de personaje en Miro

Identidad y sátira; silueta; rol; rango y movilidad; básico/cadena corta; fuerte; especial principal; recuperación aérea; super con barra de escándalo; vulnerabilidad; dos respuestas del rival; animaciones y props necesarios; riesgos de legibilidad 3D; prueba; tarjeta Trello. Este kit reutiliza el esquema de la beta y no obliga a cuatro especiales direccionales propios de otro sistema de combate. Preparar fichas de los trece arquetipos en baja fidelidad, pero terminar solo los dos escogidos para la vertical slice. El voto por popularidad visual no sustituye probar su contraste jugable.

## 8. Vínculo bidireccional con Trello

### Método base: enlaces, sin integración adicional

1. En Trello, obtener la URL real de la tarjeta desde sus opciones de compartir o desde la tarjeta abierta.
2. En Miro, crear una nota junto a la decisión con su ID `FND-XXX` y pegar esa URL como enlace. El ID permite recuperar la relación aunque cambie el título.
3. En Miro, seleccionar el marco o el objeto relevante, abrir su menú de tres puntos y copiar el enlace. Pegar la URL en `Referencia Miro` de la tarjeta y, si conviene, como adjunto. [Enlaces a marcos](https://help.miro.com/hc/en-us/articles/360018261813-Frames), [vínculos internos y externos](https://help.miro.com/hc/en-us/articles/360017572354-Internal-and-external-linking).
4. La tarjeta conserva estado y aceptación; Miro conserva el razonamiento y la imagen. Revisar la relación en la demo y al cambiar una decisión.
5. Comprobar permisos con una cuenta colaboradora autorizada. Un enlace no concede acceso automáticamente. Enlazar archivos del repositorio remoto o adjuntar la entrega según permisos; una ruta local de Windows no funcionará como enlace compartido en Miro.

Los enlaces manuales son bidireccionales en navegación, pero **no sincronizan** estado, texto ni fechas. No mantener dos backlogs editables independientes.

### Opción posterior: integración oficial Trello en Miro

Miro ofrece una integración para importar y editar tarjetas Trello con sincronización. Solo configurarla si las cuentas y permisos del equipo lo permiten y se decide usarla: desde el tablero de Miro, abrir Tools → Media & Integrations, buscar Trello y conectar siguiendo la autorización. Puede necesitar habilitación por el administrador y un periodo de sincronización. Importar después las tarjetas seleccionadas; probar una modificación en una tarjeta de prueba antes de depender del flujo. No está configurada por estos materiales. [Guía oficial de Trello en Miro](https://help.miro.com/hc/en-us/articles/30634130703378-Get-started-with-Trello).

## 9. Riesgos que deben tener dueño

| Riesgo | Señal temprana | Respuesta y gate |
|---|---|---|
| Rehacer una beta existente sin evaluarla | Tarjetas que reconstruyen algo que ya funciona | Probar primero, registrar reutilización y conservar la base 3D confirmada |
| Trece personajes antes de validar combate | Modelos nuevos mientras el rematch aún falla | Concentrar validación en dos y llevar expansión a Ideas; conservar personajes existentes |
| Plugins sin necesidad | Dos sistemas de input, tween o sombreado resuelven lo mismo | Exigir caso de uso, compatibilidad y prueba antes de añadir |
| Arte IA con mala topología/licencia | Deforma mal o no se puede documentar origen | Mantener proxy, corregir en Blender y bloquear incorporación final |
| Efectos ocultan ataques | Playtest no distingue inicio o dirección | Reducir VFX, probar siluetas y anotar evidencia |
| Online consume el MVP | Rediseño constante antes de sesión local estable | Arquitectura con datos separados y prueba futura acotada |
| Bucle de prototipo interminable | No hay build que pueda jugar alguien externo | Gate de recorrido completo y test cada sprint |
| Trabajo invisible | «Casi terminado» durante varios días | Dividir entregas y exigir evidencia pequeña verificable |
| Escenario domina rendimiento | CPU/GPU sin margen en cuatro luchadores proxy | Perfilar temprano; reducir transparencias/materiales y probar worst case |
| Sátira se vuelve ataque personal | Referencias reconocibles no autorizadas | Personajes originales, revisión de tono y registro de referencias |

Cada riesgo significativo se convierte en tarjeta con responsable y revisión por hito. No basta un tablero de riesgos que nadie vuelve a mirar.

## 10. Primera sesión de preparación: 90 minutos

1. **0–15 min:** leer la inspección de la beta y el alcance propuesto del GDD, crear tablero Trello y listas; poner el enlace del documento y la Definition of Done en Guía.
2. **15–35 min:** crear etiquetas y tres plantillas; pegar los títulos de tarjetas de base y backlog; enriquecer solo las primeras cinco que realmente vayas a usar.
3. **35–55 min:** crear los ocho marcos Miro y pegar seis notas por marco desde el CSV. Dibujar el loop de combate y el flujo de pantallas.
4. **55–70 min:** conectar FND-001, FND-005 y FND-012 a sus marcos mediante enlaces reales. Revisar que no haya permisos abiertos accidentalmente.
5. **70–85 min:** calcular capacidad de dos semanas, elegir un objetivo verificable y mover pocas tarjetas listas a Sprint comprometido.
6. **85–90 min:** escribir la bitácora inicial: versión de entorno, objetivo, evidencia que probará el éxito, bloqueo principal y próxima sesión.

Al terminar, deberías poder responder en menos de un minuto: qué se juega en la próxima build, qué tarjeta está activa, qué impide cerrar el hito y dónde está la evidencia de lo ya aceptado.
