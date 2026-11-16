# Cambio de dirección FUNADOS 0.5.0

El 16/11/2026 Santiago solicitó replicar la dinámica del video https://www.youtube.com/shorts/pj4WF6dMzP4 y confirmó explícitamente **combate lateral 2.5D**. Esta decisión reemplaza el requisito de movimiento libre X/Z del GDD y de la checklist anteriores para producción. Los documentos originales se conservan como historial; no deben usarse para reintroducir profundidad jugable.

## Análisis de la referencia

El video se titula “The PERFECT Mario Ladder Combo”, de @TheSaviorNinja, y muestra un combate de Smash con Mario y Roy. Se observaron cámara lateral, personajes sobre plataformas, porcentajes de daño, recuperación desde el borde y golpes consecutivos que elevan al rival en el aire. La prioridad de esta entrega es el ritmo de golpe, persecución mediante salto y encadenado aéreo, conservando los personajes y poderes de FUNADOS. El video no proporciona datos por frame ni reglas completas; no se presentan nuestros valores como equivalentes exactos a los del juego de referencia.

## Implementación

- Producción se juega en X/Y, con Z=0. W/S y el eje vertical del mando seleccionan ataques; no desplazan al luchador en profundidad. La cámara es ortográfica y frontal, con encuadre de altura y separación.
- Básico ascendente: 4% de daño, preparación 0,06 s, activo 0,12 s y recuperación 0,13 s; poco empuje lateral y elevación de 8,8 antes de la contribución por daño. Fuerte ascendente: 10% y elevación 12.
- Básico aéreo neutral: 5%, preparación 0,05 s, activo 0,12 s y recuperación 0,15 s. Aéreo descendente: 7% básico/12% fuerte y lanzamiento inicial vertical -11, modificado por daño acumulado.
- Un básico confirmado admite salto para perseguir. El golpe fallado mantiene su recuperación. Buffer de básico/fuerte de 0,14 s y salto de 0,12 s; pausa de impacto individual de 0,035 s. Se mantienen doble salto, recuperación y esquiva.
- El contador enlaza impactos al mismo rival con menos de 1,1 s entre ellos. Muestra confirmación; no garantiza un combo inescapable ni concede puntos extra.
- Los poderes y supers existentes permanecen disponibles. La ráfaga de Lola se abre verticalmente, no en profundidad. El knockback se mantiene en X/Y.
- Cuatro escenarios de arte preexistentes adaptados en runtime: suelo principal, dos plataformas a 2,6 m, aparición de cinco participantes en el plano y KO lateral, inferior y superior (20 m). Las plataformas se atraviesan al subir y se puede bajar con S+Espacio. Se desactivan las colisiones de estructura trasera y se oculta el suelo decorativo que aparentaba continuar la plataforma.
- Los prefabs, texturas y modelos fuente se conservan. Las animaciones del rig automático incluyen una pose ascendente y una descendente; siguen siendo provisionales.

## Preservación y regresión

Antes de editar se creó `Builds/Backups/Before2p5D-2026-11-16.zip`, con Assets, Packages y ProjectSettings tal como estaban. SHA256: `7D369BF9AF847991167AE83151CAC9B2020BEC462A4894553662987B34F838E4`. Los escenarios y cambios locales encontrados al empezar se registraron separadamente en `c2ad544`; no fueron generados por esta conversión.

El ejecutable de producción arranca con reglas 2.5D. Las opciones optativas `-beta-smoke` y `-controls-smoke` fuerzan reglas históricas para comprobar regresiones; no modifican el proyecto ni habilitan una opción 3D en el menú normal. `-side-smoke` prueba el nuevo diseño. El balance histórico y las reglas antiguas quedan disponibles para esa comparación.

## Aceptación y continuación

Primera ejecución de la nueva prueba: **54 comprobaciones correctas, 0 fallos** (Evidence/Side-0.5.0/Run1). Tras revisar las capturas se ocultó el suelo trasero que confundía los bordes y se añadió comprobación de combate de bots. La evidencia de la build final se guardará en carpetas separadas dentro de Evidence/Side-0.5.0.

La ejecución intermedia `Final` encontró un fallo adicional: leer la cruceta como Vector2 normalizado reducía X al orientar el golpe hacia arriba. Se corrigió leyendo los botones de cruceta y limitando X/Y por separado en producción, también al combinar entradas. El ensayo de esa corrección permanece en la prueba automática. La carpeta `Final` conserva la evidencia de ese fallo para trazabilidad; no es el informe de aceptación. Las reglas históricas pasaron sus 109 comprobaciones antes de esta corrección exclusivamente lateral.

**Aceptación automática 2.5D: 56 comprobaciones superadas, 0 fallos**, en `Evidence/Side-0.5.0/Validated/side-report.json`, con el ejecutable definitivo. Incluye la corrección de cruceta, los dos poderes piloto y super, combo aéreo, remate descendente, plataformas desde abajo/aterrizaje/caída, cuatro escenarios con cinco participantes, cámara, KO superior, reaparición, pausa, mandos virtuales y combate con bots. Se revisaron capturas del combo y la partida normal. No sustituye mandos físicos, balance con personas ni medición de rendimiento.

La build incremental final fue Succeeded, 0 errores y 0 avisos, 264.934.211 bytes y 25,19 segundos; se guardó el informe en Evidence/Side-0.5.0/build.txt. La primera compilación de esta entrega informó 485 avisos. El resultado incremental con caché no acredita que se hayan eliminado los avisos de shaders de la compilación completa.

**Regresión histórica con el mismo ejecutable definitivo: 109 comprobaciones superadas, 0 fallos**, en `Evidence/Side-0.5.0/LegacyValidated/smoke-report.json`. Se conservan los cinco kits, cuatro arenas históricas, reglas de partida, entrenamiento y dispositivos. La propiedad version de ese informe es 0.3.0 porque las reglas históricas fuerzan la presentación beta; la build de producción es 0.5.0. Los dos informes de aceptación son Validated y LegacyValidated. Las pruebas se ejecutaron a 1280×720 con Direct3D 11 en RTX 4050 Laptop. No se evaluó rendimiento sostenido ni mandos físicos.

Esta entrega no cierra la checklist anterior íntegra. SYS-024/028/033/036/037, SYS-041/043/046/047 y las fichas de poderes necesitan reaceptación bajo 2.5D; el eje Z deja de ser una condición de aceptación. Se incorpora el límite superior de KO y se comprueban paso/caída de plataformas. Los golpes direccionales son ahora alcance autorizado por Santiago, reemplazando su aplazamiento anterior en SYS-061.

Pendiente: playtest con personas, ajuste de velocidad y ventanas de combo, poses/rig finales, calibración de hitboxes por movimiento, simultaneidad de impactos, mandos físicos, rendimiento sostenido y remapeo/guardado de opciones. Las reglas de protección y estadísticas existentes se conservan. El siguiente agente debe partir de este documento y de CONTINUAR.md.
