# Inicio de producción FUNADOS 0.4.0

Fecha: 11/11/2026. Responsable del proyecto: Santiago. Esta entrega inicia el desarrollo siguiendo la checklist maestra v1.0 y el GDD; no declara terminado el juego completo ni la vertical slice. Se concentra en herramientas de práctica y preservación de la base jugable.

Actualización posterior: la entrega 0.4.1 implementa asignación estable, modo dos mandos y recuperación de desconexiones. La 0.4.2 añade remapeo con conflictos (SYS-024/025) y opciones versionadas y recuperables (SYS-068/069); ver `Avance_controles_0.4.2.md`. La 0.4.3 añade ajustes de mando y vibración (SYS-026), opciones v2 e impactos simultáneos imparciales (SYS-045); ver `Avance_0.4.3.md`. La 0.4.4 añade menús navegables con foco visible (SYS-070) y volúmenes de proyectiles y zonas en el laboratorio; ver `Avance_0.4.4.md`. La 0.4.5 añade tamaño de texto/HUD y adaptación a otras proporciones (SYS-071); ver `Avance_0.4.5.md`. El estado actual y el punto de continuación están en `CONTINUAR.md`, en la raíz del repositorio. El resto de este documento conserva la evidencia de 0.4.0.

Decisión del 16/11/2026: Santiago reemplazó el movimiento libre 3D por **combate lateral 2.5D con golpes direccionales y combos aéreos**. La entrega 0.5.0 y los criterios que deben revalidarse están en `Docs/Produccion_2026-11-16/Combate_2p5D.md`. Los requisitos históricos X/Z quedan subordinados a esa decisión.

La checklist original se conserva sin marcar en esta carpeta. Las instrucciones de sus documentos se usan como requisitos de diseño y aceptación, dentro del trabajo local solicitado. No se modificaron Trello, Miro ni cuentas externas.

## Entrega implementada

| Checklist | Resultado de esta entrega | Estado de aceptación |
|---|---|---|
| SYS-005 | Primer commit de la base, identidad Git existente y Git LFS para arte/modelos/audio/fuentes Blender/ZIP. | Historial local creado; remoto pendiente por decisión del usuario. |
| SYS-006 | Editor 6000.5.7f1 y lock de paquetes conservados; README con apertura, build y restauración. | Comprobación local; falta restauración en un segundo equipo. |
| SYS-007 | Escenas Funados y CombatLab independientes; escena beta sin modificación. | Implementado; escenas generadas y ejecutadas por Unity. |
| SYS-008 | Compilación de scripts sin errores; reporte nuevo de build separado de evidencia histórica. | Evidencia en Evidence/Production-2026-11-11. |
| SYS-009 | Nombres públicos Don Promesio/Lola Viral solo en presentación de producción; IDs, GUID, datos y recursos originales conservados. | Verificado en menú y laboratorio. |
| SYS-017 | Inventario SHA256 de 22 archivos visuales reutilizados. | Parcial: derechos originales de Meshy siguen sin verificar. |
| SYS-065 | Entrenamiento sin límite de tiempo/stocks, objetivo sin entradas, daño inicial 0–300, reinicio, super listo y alcance cuerpo a cuerpo visible. | Implementado y verificado; falta ampliar inspección de proyectiles/zonas y prueba con personas. |
| SYS-059 | Pausa y vuelta a menú/versus desde entrenamiento. | Prueba automática específica además de regresión existente. |
| ST01-368 | Reutilización de Azotea Neón sin modificar geometría ni rutas existentes. | Escena ejecutada y revisada; medición y aceptación completa de la arena pendientes. |
| CIERRE-459 | Regresión de los cinco kits y cuatro arenas conservadas. | Resultado de esta sesión en el informe nuevo; no sustituye mandos físicos ni playtest. |

La visualización de melee consulta los mismos parámetros que el barrido ofensivo. La cruz marca la raíz usada como referencia vulnerable; no pretende representar una hurtbox anatómica nueva. Se conservan tolerancia horizontal adicional de 0,3 m, altura, cono y bloqueo por geometría de la beta. No se cambia el balance para acomodar la visualización.

Los cien retornos dobles del laboratorio son una prueba sintética del reinicio de práctica, invocada sobre la resolución de caídas; no acreditan cien ciclos completos de KO y reaparición del versus ni cierran H1.

## Evidencia

Build Windows: `Builds/Funados/Windows/FUNADOS.exe`. Reporte: `Evidence/Production-2026-11-11/build.txt`. La build histórica en Builds/Windows se conserva.

Captura del editor revisada: `Evidence/Production-2026-11-11/editor-lab.png`. Antes de capturar, se comprobó avance desde frame 495/simulación 8,37 s hasta frame 1357/simulación 23,40 s. Consola del editor sin errores de ejecución en esa revisión.

Regresión del ejecutable: `Evidence/Production-2026-11-11/Smoke/smoke-report.json`, capturas y player.log. La propiedad version del informe conserva la versión del motor de beta 0.3.0; la build y presentación de producción son 0.4.0. No mezclar este informe con Evidence/smoke-report.json, que es histórico.

Resultado nuevo: **83 comprobaciones superadas, 0 fallos**. Incluye las 67 de regresión anterior y 16 de entrenamiento. Se revisaron visualmente las capturas del menú, laboratorio y combate de cinco participantes; no resultaron negras. El Player usó Direct3D 11 en la NVIDIA RTX 4050 Laptop a 1280×720. La prueba no mide percentiles ni acredita 60 FPS sostenidos.

Compilación: Succeeded, 0 errores, 485 avisos, aproximadamente 110 segundos y 179.086.819 bytes según BuildReport. La preparación del 10/11 también registró 485 avisos. Coincidir en cantidad no prueba que cada aviso sea idéntico; siguen pendientes de evaluación los paquetes IA preexistentes.

## Límites y siguiente trabajo

Se reutilizan los once modelos/texturas de la beta, sin generar arte nuevo ni sustituir los recursos habituales. El rig automático permanece provisional; no se aceptan todavía las animaciones finales, accesorios ni procedencia publicable de los personajes.

Quedan pendientes unión y reconexión estable de mandos, remapeo con conflictos, opciones versionadas recuperables, mayor inspección de volúmenes y un rig de referencia revisado. La siguiente sesión debe empezar con teclado+mando y dos mandos físicos en Azotea Neón, registrando tres problemas observables antes de ampliar contenido. No se han aceptado los trece personajes ni las ocho arenas; online permanece posterior al local estable.
