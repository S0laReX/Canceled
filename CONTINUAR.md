# Continuación de FUNADOS

**Trabajo en paralelo:** ver `PARALELO.md` (ramas abiertas en `origin`, reglas y orden de merge).

**Empezar por `CHECKLIST_2p5D.md`**, que ahora sigue las 63 tareas del **GDD 2.0** (`Docs/Produccion_2026-11-22/FUNADOS_GDD_2.0.docx`, vigente desde el 22/11/2026). Los apartados 0.5.1/0.5.0/0.4.1 siguientes son historial.

## Relevo chore/restauracion-licencias

- **Restauración limpia (SYS-004/006, QA-441): pasa.** `Tools/restaurar_limpio.ps1 -Destino <carpeta nueva>` clona, ejecuta `git lfs pull`, comprueba los punteros, importa en batchmode sin `Library`, genera la build y ejecuta `-arena-smoke` y `-side-smoke`, con tiempos en `_restauracion/resumen.json`. Resultado sobre `main` 0.7.0: 40,5 min en total (LFS 562 MB en 62 s, importación 20 min, build 6 min, arena 46/0, side 375/0). Informe: `Docs/Restauracion/Restauracion_limpia_0.7.0.md`. No clonar más de una vez al mes sin motivo: es 1 GB de LFS gratuito al mes en GitHub y cada clon gasta unos 560 MB. El guion reutiliza un clon existente.
- **Licencias (SYS-017, D04): inventario hecho, casilla abierta.** `Docs/Licencias/Procedencia_y_licencias.md` + `inventario_activos.csv` (119 archivos con SHA256) + `inventario_paquetes.csv` (68 paquetes), regenerables con `python -I Tools/inventario_licencias.py --package-cache <proyecto>/Library/PackageCache`. Todo confirmado salvo los 110 archivos de Meshy (personajes, escenarios y conceptos), que dependen del plan de la cuenta al generarlos y de si los personajes salieron de la comunidad. Esa es la decisión D04 de Santiago; los pasos para cerrarla están al final del documento.
- Sin cambios en el código del juego ni en la versión. Tras importar, Unity reescribe 10 YAML/JSON con LF: con `core.autocrlf` Git los marca como modificados, sin cambios de contenido.

## Relevo 0.7.0 (juntada feat/arena-ko-stress, ARENA-366)

- `BrawlArenaStress` (`-arena-stress`, registrado en `BrawlMatch` y `BrawlSideSmokeProbe`): 20 partidas de cinco bots por arena con auditoría de cada KO y reaparición, más comprobación de fugas. 25/25; unos 25 min.
- Observación: casi todas las partidas acaban por tiempo; revisar la agresividad de los bots o la duración y las vidas por defecto cuando se calibre con personas.
- Suites en la rama (`Evidence/Branch-feat-arena-ko-stress`): power 32, moves 21, kits 91, recovery 16, camera 15, coyote 5, arena 46, side-controls 127, side 375, controls 125, beta 208, arena-stress 25; cero fallos.
- Juntada en `main` como 0.7.0. Checklist **37 de 63**. Primera rama de `PARALELO.md` integrada; siguen abiertas `feat/audio-buses`, `feat/ui-accesibilidad`, `chore/restauracion-licencias` y `perf/rendimiento` (rebasarlas sobre `main`).

## Relevo 0.6.9 (distribuciones por arena, ARENA-350–367)

- `BrawlArena.SideLayouts`: Azotea ±4,8/2,6 (neutral) · Torre ±6,0/3,6 (vertical) · Estudio ±5,6/2,1 (atriles bajos) · Plaza ±10/2,2 (balcones sobre el vacío). Solo se mueve la instancia; prefabs y arte intactos.
- `BrawlArena.PlayBounds` → `BrawlCamera.FrameSide` encuadra siempre la distribución completa. La comprobación de cámara «juntos» ya no exige el zoom mínimo exacto (Plaza necesita más).
- `-arena-smoke` añade: plataformas visibles, subida con doble salto del kit de menor alcance (Coach), bajada al suelo o al vacío, y distribuciones distintas por pares.
- Suites 0.6.9: power 32, moves 21, kits 91, recovery 16, camera 15, coyote 5, arena 46, side-controls 127, side 375, controls 125, beta 208; cero fallos. Checklist **36 de 63**.
- **Siguiente técnico:** cien KO con reaparición y veinte partidas por arena (ARENA-366).

## Relevo 0.6.8 (límites y apariciones, ARENA-358/359)

- KO solo izquierda/derecha/abajo/arriba en 2.5D (retirado el criterio de profundidad). Apariciones validadas en las cuatro arenas.
- Corregido: plataformas atravesables gruesas en Torre, Estudio y Plaza (pared invisible para golpes/proyectiles bajo ellas) → losas de 0,3 m (`BrawlArena.ThinToTop`, solo en la instancia).
- Suites 0.6.8: power 32, moves 21, kits 91, recovery 16, camera 15, coyote 5, arena 33, side-controls 127, side 362, controls 125, beta 208; cero fallos. Checklist **35 de 63**.
- **Siguiente técnico:** distribuciones jugables distintas por arena (ARENA-350–367, propuestas del GDD §9), luego cien KO/veinte partidas por arena (ARENA-366).

## Relevo 0.6.7 (coyote time: 3 ticks)

- Decisión de Santiago tras medir: coyote time de 3 ticks (0,05 s), `BetaBalance.coyoteSeconds`. Sin él, pulsar en el borde ya gastaba el salto aéreo. Bloqueado tras salto, golpe, bajada de plataforma y aparición en el aire.
- Suites 0.6.7: power 32, moves 21, kits 91, recovery 16, camera 15, coyote 5, side-controls 127, side 330, controls 125, beta 208; cero fallos. Checklist **34 de 63**.
- **Siguiente:** revisar límites y puntos de aparición por arena (ARENA-358/359).

## Relevo 0.6.6 (cámara y avisos, SYS-036–040)

- `BrawlCamera.FrameSide` reserva la franja del HUD superior (`TopReserve`, calculada por el HUD) además de la inferior; zoom 5,5–16; rotación fija.
- `BrawlArena.DistanceToKoLimit` + marcador rosa con «!» a menos de 3 m de un límite (`BrawlHud.KoWarningDistance`).
- Suites 0.6.6: power 32, moves 21, kits 91, recovery 16, camera 15, side-controls 127, side 326, controls 125, beta 208; cero fallos. Checklist **33 de 63**.
- **Siguiente:** decidir coyote time (decisión de Santiago, con medición previa) y revisar límites y puntos de aparición por arena (ARENA-358/359).

## Relevo 0.6.5 (recuperación y bordes, SYS-030–035)

- `BrawlRecoveryChecks` (`-recovery-smoke`): recursos aéreos, restauración solo en suelo válido, recuperación por fase, movimientos rápidos sin atravesar, indicador de apoyo y alcance de vuelta medido por kit y borde.
- Alcance a nivel: Don 8,8 m · Lola 11,8 · Coach 8,3 (menor) · Cripto 9,4 · Tío 11,8; simétrico. Es generoso: candidato a calibración con personas.
- Suites 0.6.5: power 32, moves 21, kits 91, recovery 16, side-controls 127, side 312, controls 125, beta 208; cero fallos. Checklist **32 de 63**.
- **Siguiente:** cámara y avisos fuera de pantalla con cinco luchadores y alturas extremas (SYS-036–040); decidir coyote time.

## Relevo 0.6.4 (matriz de los cinco kits)

- `BrawlKitMatrixChecks` (`-kits-smoke`): especial, recuperación y super de los 5 kits en acierto, fallo, interrupción, pausa, plataforma y borde (90 casos).
- Corregido: interrumpir los supers de zona (Lola) o burbuja (Cripto) cancela la zona pendiente; la embestida de Coach se detiene al conectar.
- Suites 0.6.4: power 32, moves 21, kits 91, side-controls 127, side 297, controls 125, beta 208; cero fallos. Checklist **31 de 63**.
- **Siguiente:** recuperación desde ambos bordes con los cinco kits midiendo el alcance máximo (SYS-030–035); luego cámara y avisos fuera de pantalla (SYS-036–040).

## Relevo 0.6.3 (D03: reducción de hitstun)

- Decisión de Santiago: reducir hitstun. Golpes repetidos del mismo atacante al mismo rival dentro de 1,1 s: hitstun × 0,8 por golpe, mínimo 40 % (`BetaBalance.hitstunDecay` / `hitstunMinScale`). El combo escalera de 3 golpes sigue igual.
- Suites 0.6.3: power 32, moves 21, side-controls 127, side 207, controls 125, beta 208; cero fallos. Checklist **30 de 63**.
- **Siguiente (prioridad alta del GDD 2.0):** verificar especial, recuperación y super de cada kit en acierto, fallo, interrupción, pausa, plataforma y borde; después recuperación desde ambos bordes con los cinco kits (SYS-030–035).

## Relevo 0.6.2 (fórmula de lanzamiento y estudio de combos)

- Lanzamiento GDD 2.0 §5 en 2.5D: K = (B + G·P/100) / resistencia al ángulo del golpe (`BrawlLaunch`, datos `launchBase/launchGrowth/launchAngle`, interruptor `BetaBalance.angledLaunch`). Cambia el balance a % alto: ver `Evidence/Production-0.6.2/Moves/launch-compare.csv`.
- D03 medido con `BrawlComboStudy` (en `-moves-smoke` y `-side-smoke`): cadena verdadera máxima 3 golpes, el defensor escapa siempre. Falta la decisión de Santiago.
- Prueba de remapeo endurecida: leía una pulsación de un solo fotograma y fallaba de forma intermitente en la suite larga; ahora comprueba el estado sostenido.
- Suites 0.6.2: power 32, moves 19, side-controls 127, side 205, controls 125, beta 208; cero fallos. Checklist **29 de 63**.

## Relevo 0.6.1 (golpes como datos y cajas X/Y)

- SYS-011: `BetaBalance.asset` contiene `sideMoves` (ascendente básico/fuerte, aéreo neutral, remate básico/fuerte) y `kitTuning` (números de los cinco kits). `BrawlMoveData.Validate` revisa repetidos, faltantes, cajas vacías y rangos; si falla, se mantienen los valores incorporados. Los valores por defecto son los de la 0.5.1.
- SYS-042–048: en 2.5D el contacto cuerpo a cuerpo es caja de golpe contra hurtbox en X/Y, reflejada por orientación. Las cajas por defecto reproducen exactamente el alcance anterior, así que el balance no cambia. CombatLab dibuja ambas.
- Corregido: con una tecla muerta pendiente en Windows (p. ej. ` en teclado español) las etiquetas salían como «`W» o «À»; `BrawlBindings.CleanKeyName` lo limpia y conserva Ñ y AZERTY.
- Build 0.6.1: 0 errores, 485 avisos. Suites: power 32/32, moves 14/14 (nuevo `-moves-smoke`), side-controls 127/127, side 200/200, controls 125/125, beta 208/208. Checklist **28 de 63**.
- **Siguiente:** límite de combos (D03) medido en CombatLab y fórmula de lanzamiento con base/crecimiento/ángulo por golpe, ambos sobre los datos nuevos.

## Relevo 0.6.0 (fusión verificada)

La rama `claude/funados-implementation-continue-db5f40` llevaba en paralelo los controles 0.4.2–0.4.6 sobre la base 3D: remapeo con conflictos, opciones versionadas y recuperables, zonas muertas y vibración (corregida tras la prueba humana con un DualShock 4), impactos simultáneos imparciales (P0-03), navegación de menús con foco y texto/HUD escalables. En 0.6.0 se fusionó `main` (2.5D 0.5.1) en esa rama.

- Resoluciones: la entrada usa la tabla única de bindings con la regla lateral por ejes; `CombineHits` aplica la dirección lateral, el contador de combo y el hitstop de `main`, y conserva el signo de los remates descendentes. El HUD une foco/escalado con los nombres de arena de producción y el contador de combo. La evidencia va a `Evidence/Production-<versión>` (convención de `main`).
- Nuevo `-side-controls-smoke`: corre dispositivos, remapeo, ajustes de mando, menús y pantalla **en 2.5D**; `-side-smoke` también los incluye al final.
- Build 0.6.0: Succeeded, 0 errores, 485 avisos. Suites (sin mandos físicos): power **32/32**, side-controls **126/126**, side **186/186**, controls **124/124**, beta **207/207**. Única corrección durante la verificación: la prueba de navegación exigía el botón ENTRENAMIENTO también en el modo histórico, donde no existe.
- Checklist: **26 de 63** (20 del GDD 2.0 + P0-03, etiquetas de mando, remapeo, opciones recuperables, zonas muertas/vibración y navegación SYS-070). SYS-071 sigue abierta (faltan alto contraste y daltonismo). Copia marcada del GDD: `Docs/Produccion_2026-11-22/FUNADOS_GDD_2.0_checklist.docx` (regenerar con `marcar_checklist_gdd.py`).
- Nuevo `-isolate-physical-pads` (opcional): retira los mandos reales solo del proceso de prueba. Un DualShock emparejado por Bluetooth y apagado aparece en Windows como `BTHENUM` sin estar conectado.
- **Siguiente:** primera pendiente de prioridad alta del GDD 2.0: hitboxes/hurtboxes por movimiento (SYS-042–048) y golpes como datos editables (SYS-011); luego límite de combos (D03).

## Relevo 0.5.1

Se implementó un refugio central de ±1,4 m frente al súper de Tío Antena. Sus lados siguen siendo peligrosos y se puede escapar entrando al centro durante la preparación, saliendo del alcance o superando su altura. Los avisos de los cinco supers, fuerte y trayectoria de token se dibujan en X/Y; blanco indica refugio y rojo peligro. Pausa, interrupción y retorno al menú eliminan o congelan los avisos según corresponda. Se conservan los recursos originales.

Build: `Builds/Funados/0.5.1/Windows/FUNADOS.exe`. Evidencia: `Evidence/Production-0.5.1`; sus carpetas Initial y LabelFirst conservan las revisiones anteriores a la corrección visual final del rótulo REFUGIO. Consultar `verification.json` y los informes de las carpetas Focused, Side y Legacy para aceptar la build final. `-power-smoke` comprueba poderes; `-side-smoke` añade esos casos a la suite lateral. El informe histórico muestra versión 0.3.0 porque fuerza esas reglas; la identidad de su ejecutable se registra mediante hashes.

**Siguiente implementación: P0-03 / SYS-045, impactos simultáneos imparciales.** BrawlMatch simula luchadores secuencialmente; recibir un golpe puede cancelar el ataque del segundo antes de su turno. Añadir un caso que invierta orden y puestos, resolver contactos sin ventaja de índice y cubrir KO simultáneo, interrupciones, proyectiles y cargas sin doble daño. No empezar ocho personajes nuevos antes de resolver y aceptar este núcleo. Remapeo SYS-025 y opciones SYS-068/069 continúan después, como P1.

Las pruebas automáticas no aceptan balance, rig, claridad del combate con personas, rendimiento ni mandos físicos. CIERRE-455–458 siguen abiertos. No hay publicación remota.

Resultado de la build final 0.5.1: **32/32 focales, 88/88 laterales y 109/109 históricos, cero fallos**; la focal es un subconjunto de la lateral. Build con cero errores/cero advertencias; capturas revisadas, fuentes/ensamblado/informes vinculados por SHA256. Los 22 recursos de personajes conservan sus hashes y Git LFS fsck terminó OK. La escena principal queda abierta y detenida. Detalle de la entrega en `Docs/Produccion_2026-11-16/Poderes_0.5.1.md`.

## Historial 0.5.0

Proyecto: `D:\Funados`. Unity Hub + Editor **6000.5.7f1**, URP **17.5.0**, Input System **1.20.0**. Estado actual: **0.5.0, combate lateral 2.5D**, por petición y confirmación explícita de Santiago el 16/11/2026. Mantener los cinco kits, cuatro escenarios y once aspectos originales; no sustituir los recursos habituales. Don Promesio/Lola Viral conservan los IDs internos Don Decreto/Lola Loop.

## Prioridad vigente desde el 16 de noviembre

Leer primero `Docs/Produccion_2026-11-16/Combate_2p5D.md`. Santiago pidió replicar la dinámica del video https://www.youtube.com/shorts/pj4WF6dMzP4 y eligió combate lateral 2.5D: X horizontal, Y altura, Z bloqueado. **La dirección anterior de movimiento libre 3D queda reemplazada en producción.** No restaurarla por seguir el GDD o la checklist históricos.

La versión actual tiene cámara frontal ortográfica, plataformas atravesables, golpes ascendentes, aéreos neutrales y descendentes, salto tras básico confirmado, buffer de entradas, pausa de impacto, contador de golpes y poderes existentes en el plano lateral. Se conserva porcentaje/expulsiones; se añade KO superior a 20 m. A/D mueve, W/S+golpe elige dirección y S+Espacio baja de plataforma. Los mandos conservan asignación y recuperación de desconexiones.

Build actual: `Builds/Funados/0.5.0/Windows/FUNADOS.exe`. Menú del editor FUNADOS para abrir la escena principal y CombatLab. La evidencia nueva está en `Evidence/Side-0.5.0`. `-side-smoke` activa la prueba de las reglas nuevas; `-beta-smoke` fuerza las reglas históricas y ejecuta regresión 3D. Esas opciones son de prueba; el juego normal siempre arranca 2.5D en producción.

Resultado 2.5D vigente: **56 comprobaciones superadas, 0 fallos**, en Evidence/Side-0.5.0/Validated/side-report.json. Regresión histórica con el mismo ejecutable final: **109 comprobaciones superadas, 0 fallos**, en LegacyValidated/smoke-report.json. Se corrigió un fallo de velocidad de cruceta diagonal; la ejecución intermedia fallida permanece en la carpeta Final como historial, no como aceptación. Los 22 archivos visuales originales de personajes conservan sus hashes. El rig automático y el balance siguen siendo provisionales.

Primero aceptar y ajustar el nuevo combate con personas: realizar launcher → salto → aéreo → remate, probar recuperación, bordes y cuatro escenarios, medir lectura y ventanas. Aún no están calibrados como un producto final. Después retomar remapeo SYS-025 y guardado recuperable SYS-068/069. Revisar SYS-024/028/033/036/037 y SYS-041/043/046/047 bajo 2.5D; no exigir movimiento Z. Los ataques direccionales ya son alcance autorizado, aunque la checklist antigua SYS-061 los posponía.

Archivos nuevos o cambiados: BrawlFighter (ataques, buffer, plano, hitstop), BrawlFighterVisual.Model (poses direccionales), BrawlMatch (reglas laterales y entradas), BrawlCamera, BrawlArena (adaptación runtime de prefabs), BrawlHud y BrawlSideSmokeProbe. Los escenarios que ya estaban sin commit al comenzar quedaron preservados separadamente en `c2ad544`; la conversión no generó ni sustituyó ese arte. Respaldo previo: Builds/Backups/Before2p5D-2026-11-16.zip.

## Historial de 0.4.1 — referencia anterior

Los apartados siguientes conservan el estado de controles y evidencia de la entrega anterior; sus rutas y próximos pasos deben leerse subordinados a la prioridad 0.5.0 indicada arriba.

## Punto de la checklist

Se trabajó el bloque **Dispositivos y control**, especialmente **SYS-022, SYS-023 y SYS-027**. Hay implementación y prueba automática con mandos virtuales. **Falta validación con mandos físicos**, por lo que esas casillas no se declaran aceptadas completamente. SYS-026 avanzó únicamente en cruceta digital y normalización; sensibilidad y vibración siguen pendientes. SYS-066 avanzó con el aviso de desconexión por puesto. QA-445 tiene cobertura automática de desconexión/reconexión y mandos sobrantes; remapeo/conflictos sigue pendiente.

**Siguiente implementación: SYS-025, remapeo de controles con detección de conflictos y etiquetas reales por dispositivo.** Después SYS-068/SYS-069: guardar ajustes y controles mediante formato versionado, escritura recuperable y restauración ante archivos dañados. Hoy la entrada de gameplay se lee directamente de Keyboard/Gamepad; no existe todavía un sistema de bindings de gameplay editable. Reutilizar una única fuente de bindings para lectura y etiquetas al implementarlo, evitando una segunda tabla de teclas independiente.

La primera entrega local continúa abierta: CIERRE-455 a CIERRE-458 no están aceptados. El laboratorio y la regresión anterior existen; no confundir el número de comprobaciones con casillas completas. También están pendientes hitboxes/hurtboxes completas, resolución imparcial de impactos simultáneos (SYS-045), cámara/recuperación con personas, opciones robustas, arte final y rendimiento medido. El catálogo sigue en **5/13 kits y 4/8 arenas provisionales**. Online no está implementado y es posterior al local estable.

## Comportamiento entregado

- Modo teclado/ratón + mandos, o P1 con mando para jugar con dos mandos. El modo dos mandos exige dos dispositivos para versus y uno para entrenamiento.
- Asignación fija por ronda; reordenar Gamepad.all no cambia el dueño de un luchador. Los nuevos mandos no convierten bots en humanos durante la ronda.
- Al perder un mando reservado, se pausa antes de simular, también durante cuenta atrás. Se preservan posiciones, porcentaje, stocks y reloj. Se muestra el puesto perdido y se bloquea Continuar.
- El mismo objeto de dispositivo recupera su puesto al reconectar. Si el backend crea un dispositivo nuevo, Start en él reclama el primer puesto perdido; el botón Asignar mandos disponibles permite recuperar varios. Luego se exige confirmar para continuar. No se roban puestos activos.
- Los mandos sobrantes no controlan ni pausan la partida. Cruceta y stick se combinan con magnitud máxima 1.
- En modo solo, el primer mando presente al iniciar puede controlar P1 junto al teclado. Si se pierde, también pausa: recuperarlo o regresar al menú. No existe todavía un botón para abandonar ese mando y seguir solo con teclado durante la misma ronda.

## Archivos y pruebas

`Assets/CancelBrawlBeta/Runtime/BrawlDeviceSlots.cs` mantiene identidad y reservas. `BrawlMatch.cs` enruta entradas y pausa/reanuda. `BrawlHud.cs` presenta selección de dispositivo y recuperación. `BrawlDeviceChecks.cs` contiene pruebas optativas con dispositivos virtuales. No ejecuta nada durante juego normal.

Escenas: `Assets/FunadosProduction/Scenes/Funados.unity`, `CombatLab.unity`; beta original en `Assets/CancelBrawlBeta/Scenes/CancelBrawlBeta.unity`. Los escenarios se generan en runtime como en la beta. Menú del editor FUNADOS para abrir escenas y generar Windows.

Build actual: `Builds/Funados/0.4.1/Windows/FUNADOS.exe`. Se conservan las entregas anteriores. `FunadosProjectBuilder.BuildWindows` usa la versión de BrawlMatch.ProductionVersion para la carpeta y genera el informe en Evidence/Controls-0.4.1/build.txt; al cambiar de entrega, actualizar también la carpeta de evidencia.

Prueba focal: **26 comprobaciones superadas, 0 fallos**, en `Evidence/Controls-0.4.1/Focused/smoke-report.json`. Regresión completa: **109 comprobaciones superadas, 0 fallos**, en `Evidence/Controls-0.4.1/Full/smoke-report.json` (83 anteriores + 26 nuevas). Las capturas se guardan junto al informe. Se revisaron visualmente el menú y el aviso de desconexión. El informe nuevo identifica la versión de presentación 0.4.1. Build Windows: Succeeded, 0 errores, 485 avisos, 47,39 segundos. Los 22 archivos visuales del inventario anterior conservan sus SHA256.

Ejecutar desde la raíz con `-beta-smoke -evidence-path D:/Funados/Evidence/NuevaPrueba -force-d3d11 -screen-width 1280 -screen-height 720 -screen-fullscreen 0`. Para solo controles usar `-controls-smoke`. Ambas opciones cierran el Player al terminar. **Desconectar mandos físicos antes del bloque automático**: se niega a alterar dispositivos reales. Usa una carpeta nueva para no sobrescribir evidencia.

Para validación humana: teclado+mando y dos mandos; desconectar P1 y P2 en cuenta atrás y partida; comprobar que otro jugador no cambia de puesto, que el mismo/reemplazo recupera el puesto correcto, que no se gastan stocks y que resultado/revancha siguen funcionando. Repetir con dos bajas y un mando sobrante.

## Historial y límites

Base preservada: `d2bb25e`. Laboratorio/escenas: `0413722`. El commit siguiente a estos contiene controles 0.4.1; localizarlo con `git log -3 --oneline`. Solo commits locales; no publicar remoto sin la instrucción correspondiente. Git LFS contiene FBX/PNG/audio/Blender/ZIP y se conservaron GUID y .meta.

Checklist original: `Docs/Produccion_2026-11-11/FUNADOS_Checklist_maestra_implementacion.docx`. GDD: `Docs/Produccion_2026-11-10/FUNADOS_GDD.md`. Evidencia 0.4.0: Evidence/Production-2026-11-11. Las licencias originales Meshy, rig/animación final y 485 avisos de build siguen pendientes de evaluación. No se probó rendimiento sostenido ni hardware de mandos en esta sesión.
