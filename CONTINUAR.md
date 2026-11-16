# Continuación de FUNADOS

Proyecto: `D:\Funados`. Unity Hub + Editor **6000.5.7f1**, URP **17.5.0**, Input System **1.20.0**. Estado actual: **0.4.5**: tamaño de texto y HUD ajustables, y HUD adaptado a 4:3, 16:10 y 21:9. Incluye los menús navegables con foco visible y los volúmenes del laboratorio de 0.4.4, los ajustes de mando, vibración e impactos simultáneos de 0.4.3 y los controles remapeables y opciones guardadas de 0.4.2. Mantener los cinco kits, cuatro arenas y once aspectos originales; no sustituir los recursos habituales. Los nombres públicos Don Promesio/Lola Viral conservan los IDs internos Don Decreto/Lola Loop.

## Punto de la checklist

Bloque **Dispositivos y control**. En 0.4.1 se implementaron SYS-022, SYS-023 y SYS-027 (asignación y reconexión de mandos). En 0.4.2 se añaden **SYS-024/025** (tabla única de acciones, etiquetas reales por dispositivo y remapeo con detección de conflictos) y **SYS-068/069** (archivo de opciones versionado, escritura recuperable y valores seguros ante archivos dañados). En 0.4.3 se añaden **SYS-026** (zonas muertas, respuesta de palanca y vibración en la pestaña MANDO de OPCIONES, guardadas en el formato de opciones v2 con migración desde v1), vibración apagable (parte de SYS-072) y **SYS-045** (impactos acumulados por paso y aplicados juntos, sin ventaja por número de jugador). En 0.4.4 se añade **SYS-070** (foco visible y navegación completa de menú, pausa y resultados con teclado o mando) y el laboratorio amplía SYS-065 con volúmenes de proyectiles, zonas, emisor y onda. En 0.4.5 se añade **SYS-071**: diseño único `BrawlHudLayout` (lienzo extendido sin bandas, grupos anclados, franja de cámara exacta), pestaña PANTALLA con tamaño de texto/HUD, tamaño mínimo en píxeles reales y ajuste automático de cada texto a su rectángulo. Las opciones pasan a formato v3. Todo tiene prueba automática con dispositivos virtuales; **ninguna casilla está aceptada**, porque faltan mandos físicos y personas. QA-445 cubre ya desconexión, reconexión, mandos sobrantes, remapeo y teclas en conflicto, solo con dispositivos virtuales.

**Siguiente:**
1. Validación humana y física de todo el bloque de controles: el usuario pidió hacerla al final. Ver la lista de abajo.
2. SYS-072: reducción de destellos, sacudida y movimiento del fondo por separado (hoy hay un solo interruptor y la vibración aparte).
3. Hurtboxes anatómicas (hoy: raíz y pecho como referencias).
4. Pendientes de la primera entrega local: CIERRE-455 a 458, cámara con personas, arte final y rendimiento medido.

El catálogo sigue en **5/13 kits y 4/8 arenas provisionales**. Online no está implementado y es posterior al local estable.

## Comportamiento entregado

- Dispositivos (0.4.1): modo teclado/ratón + mandos, o P1 con mando. La asignación es fija por ronda. Al perder un mando se pausa, el mismo mando o uno de reemplazo recupera su puesto y los mandos sobrantes quedan fuera. Detalle en `Docs/Produccion_2026-11-11/Avance_controles_0.4.1.md`.
- Controles (0.4.2): panel CONTROLES en el menú y en la pausa. Un clic en una celda captura la siguiente tecla, botón del ratón o botón del mando. Si hay conflicto, avisa e intercambia. Esc y Start están reservados y cancelan. Hay botones para restaurar teclado y mando. Las etiquetas usan la distribución del teclado y la familia del mando (Xbox, PlayStation, Nintendo o genérico). La barra inferior del combate también sale de la tabla.
- Opciones (0.4.2): `funados-options.json` en `persistentDataPath`. Guarda volúmenes, efectos reducidos, modo de entrada y controles. Si el archivo está dañado, se aparta como `.danado-<fecha>` y se usa la copia `.bak` o los valores seguros. Las opciones antiguas de PlayerPrefs se importan una vez.
- Ajustes de mando (0.4.3): pestaña MANDO del panel OPCIONES. Zona muerta de mover y orientar, respuesta de palanca y vibración con vista previa. La cruceta no pasa por esos ajustes. Solo vibra el mando dueño (golpe, KO, super); se detiene en pausa, menú, resultados, desconexión y pérdida de foco.
- Impactos (0.4.3): `BrawlMatch` abre una ventana al inicio de cada paso (orden de ejecución -100) y `BrawlHitResolver` la cierra al final (orden 100). Los golpes del paso se aplican juntos con `BrawlFighter.CombineHits`, cuyo resultado no depende del orden. Fuera de un paso (llamadas directas de las pruebas) el golpe es inmediato.
- Menús (0.4.4): `BrawlHud.Navigation.cs`. Cada control llama a `Nav(id, rect, activar, ajustar)` al dibujarse. Las flechas, las teclas de movimiento, la cruceta o la palanca (con repetición) mueven el foco en el espacio; Enter, Espacio o A activan; ←/→ ajustan; B reanuda en la pausa. El foco es un borde blanco. Cada pantalla abre en ENTRAR, CONTINUAR o REVANCHA. En la pausa no navegan los mandos sobrantes. `BrawlMatch.Update` ya no inicia la partida con Enter/A: lo hace el control enfocado. Start sigue siendo atajo.
- Laboratorio (0.4.4): `BrawlHazardVolumes` dibuja proyectiles, zonas, emisor y onda con las mismas constantes públicas que usan sus comprobaciones de impacto.
- Pantalla (0.4.5): `BrawlHudLayout.Compute(ancho, alto, hudScale, tarjetas)` da la escala, el espacio extra, la escala de las tarjetas y de la ayuda, y la franja. El HUD dibuja con las matrices `Top`, `TopScaled(pivote)`, `Bottom`, `Hint` y `Centre`; la cámara toma `StripFraction` en `BrawlHud.LateUpdate`. `Label`/`Button` reducen su fuente hasta caber (`FitText`). Límite conocido: a 960×720 (4:3) el texto más pequeño queda en ~11 px.
- Un perfil de botones compartido por todos los mandos y una tecla por acción. El movimiento y la orientación del mando no se remapean.

## Archivos y pruebas

- `BrawlBindings.cs`: tabla de acciones, validación, conflictos, captura y etiquetas.
- `BrawlOptionsStore.cs`: formato, escritura atómica, validación y recuperación.
- `BrawlMatch.cs`: lectura de entradas mediante bindings, captura de remapeo y carga/guardado de opciones.
- `BrawlHud.cs`: panel de controles y avisos.
- `BrawlDeviceSlots.cs` y `BrawlDeviceChecks.cs`: dispositivos (0.4.1).
- `BrawlBindingChecks.cs`: 37 pruebas de remapeo y opciones (0.4.2).
- `BrawlInputTuning.cs`: forma de la palanca. `BrawlHitResolver.cs`: cierre de la ventana de impactos.
- `BrawlTuningChecks.cs`: 23 pruebas de ajustes de mando, vibración, migración v1→v2 e intercambios simultáneos (0.4.3).
- `BrawlMenuChecks.cs`: 22 pruebas de navegación de menús y volúmenes del laboratorio (0.4.4).
- `BrawlDisplayChecks.cs`: 15 pruebas de diseño por proporción, legibilidad, opciones v3 y capturas cambiando la ventana (0.4.5). Restaura el tamaño de la ventana al terminar.

Ninguna prueba se ejecuta durante el juego normal.

Escenas: `Assets/FunadosProduction/Scenes/Funados.unity`, `CombatLab.unity`; la beta original está en `Assets/CancelBrawlBeta/Scenes/CancelBrawlBeta.unity`.

Build: `Builds/Funados/0.4.5/Windows/FUNADOS.exe`. Se generó en el worktree de esta sesión; en `D:\Funados` hay que regenerarla con FUNADOS → Generar Windows. `FunadosProjectBuilder.BuildWindows` usa `BrawlMatch.ProductionVersion` para la carpeta y para `Evidence/Controls-<versión>/build.txt`. **Al cambiar de entrega basta con cambiar `ProductionVersion`.** Resultado: Succeeded, 0 errores, 485 avisos.

Pruebas: focal **123/123** y completa **206/206** (191 anteriores + 15), sin fallos, en `Evidence/Controls-0.4.5/{Focused,Full}`. Se ejecutan con:

`-beta-smoke` (o `-controls-smoke`) `-evidence-path <carpeta nueva> -options-path <carpeta nueva>\player-options.json -force-d3d11 -screen-width 1280 -screen-height 720 -screen-fullscreen 0`

Sin `-options-path`, las pruebas no escriben las opciones del jugador, pero las comprobaciones de opciones siguen usando archivos dentro de la evidencia. **Desconecta los mandos físicos antes** (en este equipo hay un mando PlayStation inalámbrico): los bloques con mandos virtuales se niegan a ejecutarse con mandos reales. Las pruebas ignoran el foco de ventana, así que se puede cambiar de ventana mientras corren.

**Validación humana pendiente:**
- Teclado+mando y dos mandos físicos. Desconectar P1/P2 en cuenta atrás y en partida.
- Remapear salto/ataques en teclado y mando (Xbox y PlayStation si es posible), provocar un conflicto y comprobar que el aviso se entiende.
- Cerrar y abrir el juego para comprobar que se conservan controles, volúmenes y modo.
- Estropear a mano `%USERPROFILE%\AppData\LocalLow\<compañía>\FUNADOS\funados-options.json` y comprobar el aviso y la recuperación.
- Probar con una distribución AZERTY/QWERTZ.
- Pestaña MANDO: subir la zona muerta hasta que la deriva no mueva al luchador; probar respuesta fina/rápida; sentir la vibración al recibir golpes, perder vida y lanzar super; comprobar que 0 % la apaga.
- Intercambio simultáneo con dos personas: dos básicos a la vez deben golpear a ambos.
- Recorrer menú, pausa y resultados **sin ratón**, solo con el mando PlayStation: elegir luchador y aspecto, cancha, rivales, remapear un botón, ajustar la vibración y empezar.
- En el laboratorio con VOLÚMENES: comprobar que proyectiles, zonas y emisor golpean dentro de lo dibujado.
- Pestaña PANTALLA: texto y HUD al 130 % desde el sofá; probar a pantalla completa y, si es posible, en un monitor de otra proporción.

## Historial y límites

Base: `d2bb25e`. Laboratorio/escenas: `0413722`. Controles 0.4.1: `b49b358`. Controles 0.4.2: `9065e0b`. Ajustes de mando 0.4.3: `6d0bb01`. Menús 0.4.4: `7c73c00`. El commit siguiente contiene 0.4.5. Solo commits locales; no publicar en remoto sin instrucción. Git LFS contiene FBX/PNG/audio/Blender/ZIP; se conservan GUID y .meta. En un worktree nuevo hay que ejecutar `git lfs checkout` antes de abrir Unity.

Checklist: `Docs/Produccion_2026-11-11/FUNADOS_Checklist_maestra_implementacion.docx`. GDD: `Docs/Produccion_2026-11-10/FUNADOS_GDD.md`. Siguen pendientes las licencias originales de Meshy, el rig/animación final y la evaluación de los 485 avisos. No se probó rendimiento sostenido ni hardware real de mandos.
