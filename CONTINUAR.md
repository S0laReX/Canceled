# Continuación de FUNADOS

Proyecto: `D:\Funados`. Unity Hub + Editor **6000.5.7f1**, URP **17.5.0**, Input System **1.20.0**. Estado actual: **0.4.2**, controles remapeables y opciones guardadas con recuperación. Mantener los cinco kits, cuatro arenas y once aspectos originales; no sustituir los recursos habituales. Los nombres públicos Don Promesio/Lola Viral conservan los IDs internos Don Decreto/Lola Loop.

## Punto de la checklist

Bloque **Dispositivos y control**. En 0.4.1 se implementaron SYS-022, SYS-023 y SYS-027 (asignación y reconexión de mandos). En 0.4.2 se añaden **SYS-024/025** (tabla única de acciones, etiquetas reales por dispositivo y remapeo con detección de conflictos) y **SYS-068/069** (archivo de opciones versionado, escritura recuperable y valores seguros ante archivos dañados). Todo tiene prueba automática con dispositivos virtuales; **ninguna casilla está aceptada**, porque faltan mandos físicos y personas. QA-445 cubre ya desconexión, reconexión, mandos sobrantes, remapeo y teclas en conflicto, solo con dispositivos virtuales.

**Siguiente:**
1. Validación humana y física. Ver la lista de abajo.
2. **SYS-026**: deadzones, sensibilidad y vibración editables. Guardarlas en `BrawlOptionsData`, subiendo `BrawlOptionsStore.CurrentVersion` a 2, con migración desde la 1 y una prueba con un archivo v1.
3. Pendientes de la primera entrega local: CIERRE-455 a 458, hitboxes/hurtboxes completas, SYS-045, cámara con personas, arte final y rendimiento medido.

El catálogo sigue en **5/13 kits y 4/8 arenas provisionales**. Online no está implementado y es posterior al local estable.

## Comportamiento entregado

- Dispositivos (0.4.1): modo teclado/ratón + mandos, o P1 con mando. La asignación es fija por ronda. Al perder un mando se pausa, el mismo mando o uno de reemplazo recupera su puesto y los mandos sobrantes quedan fuera. Detalle en `Docs/Produccion_2026-11-11/Avance_controles_0.4.1.md`.
- Controles (0.4.2): panel CONTROLES en el menú y en la pausa. Un clic en una celda captura la siguiente tecla, botón del ratón o botón del mando. Si hay conflicto, avisa e intercambia. Esc y Start están reservados y cancelan. Hay botones para restaurar teclado y mando. Las etiquetas usan la distribución del teclado y la familia del mando (Xbox, PlayStation, Nintendo o genérico). La barra inferior del combate también sale de la tabla.
- Opciones (0.4.2): `funados-options.json` en `persistentDataPath`. Guarda volúmenes, efectos reducidos, modo de entrada y controles. Si el archivo está dañado, se aparta como `.danado-<fecha>` y se usa la copia `.bak` o los valores seguros. Las opciones antiguas de PlayerPrefs se importan una vez.
- Un perfil de botones compartido por todos los mandos y una tecla por acción. El movimiento y la orientación del mando no se remapean.

## Archivos y pruebas

- `BrawlBindings.cs`: tabla de acciones, validación, conflictos, captura y etiquetas.
- `BrawlOptionsStore.cs`: formato, escritura atómica, validación y recuperación.
- `BrawlMatch.cs`: lectura de entradas mediante bindings, captura de remapeo y carga/guardado de opciones.
- `BrawlHud.cs`: panel de controles y avisos.
- `BrawlDeviceSlots.cs` y `BrawlDeviceChecks.cs`: dispositivos (0.4.1).
- `BrawlBindingChecks.cs`: 37 pruebas nuevas.

Ninguna prueba se ejecuta durante el juego normal.

Escenas: `Assets/FunadosProduction/Scenes/Funados.unity`, `CombatLab.unity`; la beta original está en `Assets/CancelBrawlBeta/Scenes/CancelBrawlBeta.unity`.

Build: `Builds/Funados/0.4.2/Windows/FUNADOS.exe`. Se generó en el worktree de esta sesión; en `D:\Funados` hay que regenerarla con FUNADOS → Generar Windows. `FunadosProjectBuilder.BuildWindows` usa `BrawlMatch.ProductionVersion` para la carpeta y escribe `Evidence/Controls-0.4.2/build.txt`. **Al cambiar de entrega, actualizar ambas cosas.** Resultado: Succeeded, 0 errores, 485 avisos.

Pruebas: focal **63/63** y completa **146/146** (109 anteriores + 37), sin fallos, en `Evidence/Controls-0.4.2/{Focused,Full}`. Se ejecutan con:

`-beta-smoke` (o `-controls-smoke`) `-evidence-path <carpeta nueva> -options-path <carpeta nueva>\player-options.json -force-d3d11 -screen-width 1280 -screen-height 720 -screen-fullscreen 0`

Sin `-options-path`, las pruebas no escriben las opciones del jugador, pero las comprobaciones de opciones siguen usando archivos dentro de la evidencia. **Desconecta los mandos físicos antes**: los bloques de dispositivos y de remapeo se niegan a ejecutarse con mandos reales.

**Validación humana pendiente:**
- Teclado+mando y dos mandos físicos. Desconectar P1/P2 en cuenta atrás y en partida.
- Remapear salto/ataques en teclado y mando (Xbox y PlayStation si es posible), provocar un conflicto y comprobar que el aviso se entiende.
- Cerrar y abrir el juego para comprobar que se conservan controles, volúmenes y modo.
- Estropear a mano `%USERPROFILE%\AppData\LocalLow\<compañía>\FUNADOS\funados-options.json` y comprobar el aviso y la recuperación.
- Probar con una distribución AZERTY/QWERTZ.

## Historial y límites

Base: `d2bb25e`. Laboratorio/escenas: `0413722`. Controles 0.4.1: `b49b358`. El commit siguiente contiene 0.4.2. Solo commits locales; no publicar en remoto sin instrucción. Git LFS contiene FBX/PNG/audio/Blender/ZIP; se conservan GUID y .meta. En un worktree nuevo hay que ejecutar `git lfs checkout` antes de abrir Unity.

Checklist: `Docs/Produccion_2026-11-11/FUNADOS_Checklist_maestra_implementacion.docx`. GDD: `Docs/Produccion_2026-11-10/FUNADOS_GDD.md`. Siguen pendientes las licencias originales de Meshy, el rig/animación final y la evaluación de los 485 avisos. No se probó rendimiento sostenido ni hardware real de mandos.
