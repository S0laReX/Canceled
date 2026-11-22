# Checklist activa de FUNADOS 2.5D (GDD 2.0)

Fuente: **GDD 2.0 (combate lateral 2.5D), sección 15** — `Docs/Produccion_2026-11-22/FUNADOS_GDD_2.0.docx`. Esta lista sigue sus 63 tareas con el mismo orden y redacción; las notas de evidencia van debajo de cada una. Estado de trabajo: **0.6.4** (base lateral 0.5.1 de `main` + controles 0.4.2–0.4.6 de la rama `claude/funados-implementation-continue-db5f40`). Última actualización: 22/11/2026. Editor Unity **6000.5.7f1**.

`[x]` = implementado con evidencia técnica (pruebas automáticas de la versión indicada), **no** aceptado con personas. `[ ]` = pendiente. **⧗** = implementado en 0.6.0, pero su verificación automática en 2.5D aún no se ha ejecutado: no se marca hasta que pasen las suites. No equiparar cantidad de pruebas con casillas cerradas.

Recuento: **31 de 63** marcadas (20 del GDD 2.0 + 6 en 0.6.0 + 2 en 0.6.1 + 1 en 0.6.2 + 1 en 0.6.3 + 1 en 0.6.4). Ninguna en ⧗. Evidencia vigente 0.6.4 (`Evidence/Production-0.6.4`): power 32/32, moves 21/21, kits 91/91, side-controls 127/127, side 297/297, controls 125/125, beta 208/208.

## Instrucciones para el siguiente agente

1. Leer esta lista, el GDD 2.0 y `CONTINUAR.md`; comprobar `git status` y `git log -4 --oneline`.
2. Tras cada tarea: ejecutar las suites (ver «Rutas y comandos»), marcar aquí con su evidencia y regenerar la copia marcada del GDD con `python -I Docs/Produccion_2026-11-22/marcar_checklist_gdd.py` (`FUNADOS_GDD_2.0_checklist.docx`).
3. Elegir la primera tarea pendiente de prioridad alta, implementarla, verificarla, marcarla aquí y en la copia del GDD, y crear un commit local. No publicar un remoto sin instrucción.

## Base del proyecto

- [x] Diseño lateral 2.5D confirmado (16/11/2026)
- [x] Cinco kits, once aspectos y cuatro escenarios conservados, con ID y GUID
- [x] Escenas Funados y CombatLab; beta y pruebas históricas preservadas
- [x] Git + Git LFS; editor y paquetes fijados (SYS-005/006/007/009)
- [ ] Restaurar desde Git/LFS en un entorno limpio (SYS-004/006, QA-441)
  - Parcial: cada worktree nuevo se ha restaurado con `git lfs checkout` e importación completa de Library en batchmode (0.4.2 y 0.6.0). Falta una máquina/cuenta limpia.
- [ ] Procedencia y licencias de modelos, escenarios, audio y dependencias (SYS-017, D04)

## Movimiento, cámara y plano 2.5D

- [x] Movimiento X/Y con Z bloqueado, cámara lateral ortográfica y doble salto
- [x] Plataformas atravesables desde abajo y S + Espacio para bajar
- [x] KO por izquierda, derecha, abajo y arriba (20 m)
- [x] La cruceta elige dirección de golpe sin frenar el movimiento horizontal
  - 0.6.0 conserva la regla por ejes también con teclado remapeado y con la palanca tras zona muerta/curva (`BrawlMatch.ReadKeyboardButtons` / `ReadGamepad`).
- [ ] Recuperación desde ambos bordes con los cinco kits (SYS-030 a 035)
- [ ] Cámara y avisos fuera de pantalla con cinco luchadores y alturas extremas (SYS-036 a 040)
  - Parcial (0.4.5): la cámara reserva exactamente la franja del HUD en 4:3, 16:10 y 21:9 (`BrawlHudLayout`). Falta el caso lateral con cinco luchadores separados y alturas extremas.
- [ ] Decidir y, si se aprueba, añadir coyote time

## Combate

- [x] Golpes ascendente, aéreo neutral y remate descendente
- [x] Salto tras básico confirmado, buffer de entradas, hitstop y contador de combo
- [x] Modo Volúmenes en CombatLab para ver el alcance de los golpes
  - 0.4.4 añadió volúmenes de proyectiles, zonas, emisor y onda con las constantes reales de impacto.
- [x] P0-03 · Impactos simultáneos imparciales (SYS-045) — siguiente tarea
  - Implementado (0.4.3, integrado en 0.6.0): ventana de impactos por paso (`BrawlMatch` −100 → `BrawlHitResolver` +100), vulnerabilidad al inicio del paso, `BrawlFighter.CombineHits` independiente del orden. En 0.6.0 se añadió la regla lateral (empuje solo en X con signo, contador de combo y hitstop por atacante) y se corrigió que un remate descendente (elevación negativa) conserve su signo al combinarse. Verificado en 2.5D (side-controls y side): intercambio espejo a lo largo de X en ambos órdenes golpea a los dos, combinación invertida idéntica, golpes opuestos, un solo golpe = fórmula anterior, poderes 32/32 sin daño duplicado. Falta con personas y KO simultáneo de última vida en partida real.
- [x] Hitboxes y hurtboxes separadas por movimiento, en altura, borde y espejo (SYS-042 a 048)
  - 0.6.1: en 2.5D el cuerpo a cuerpo es un rectángulo de golpe por movimiento (X hacia donde mira, Y desde los pies) contra la hurtbox del rival (0,72 × 1,7 m), reflejado por orientación (`BrawlMoveData.Overlaps`). Golpes direccionales con caja propia en datos; los del kit la derivan de su alcance y altura. Valores por defecto equivalentes al barrido anterior (comprobado en 1.089 puntos por golpe). Pruebas: espejo geométrico y en partida, altura (ascendente/remate), ambos bordes con rival fuera del suelo, CombatLab dibuja la caja real y las hurtboxes (`lab-hitboxes.png`). Proyectiles, zonas y onda conservan sus volúmenes reales (0.4.4). Límite: la hurtbox es un rectángulo por luchador, no anatómica por animación; eso depende del rig real (D01).
- [x] Golpes y kits como datos editables con validación (SYS-011)
  - 0.6.1: `BetaBalance.asset` → `sideMoves` (5 golpes direccionales: tiempos, daño, empuje, elevación, caja) y `kitTuning` (valores de los 5 kits), con rangos en el Inspector y `BrawlMoveData.Validate` (repetidos, faltantes, cajas vacías, rangos, orden del roster). Datos no válidos no se aplican y se avisa en el log. Pruebas: cambiar daño o caja en datos cambia el golpe real. Los nombres, estilos y modelos siguen en `BrawlRoster` (no son balance).
- [x] Fórmula de lanzamiento con base, crecimiento y ángulo por golpe
  - 0.6.2: en 2.5D K = (B + G·P/100) / resistencia, lanzado al ángulo θ del golpe (`BrawlLaunch`). Los 5 golpes direccionales tienen `launchBase/launchGrowth/launchAngle` en datos; los del kit los derivan de su empuje/elevación editables. Por defecto cada golpe conserva su ángulo y fuerza de 0 %. **Cambio de balance a % alto** (tabla `Evidence/Production-0.6.2/Moves/launch-compare.csv`): ascendente a 100 % antes salía casi horizontal (11,6; 10,6) y ahora vertical (1,95; 15,6); el remate ya no se aplana (−15,5 frente a −9,2). La resistencia divide ahora también la vertical. Interruptor `angledLaunch` para volver a la fórmula histórica. Calibrar con personas.
- [x] Límite de combos: reducción de hitstun o tope (D03)
  - Medido en 0.6.2 (`BrawlComboStudy`, `combo-study.json`): atacante con combo escalera guionizado contra defensor que escapa en cuanto puede actuar. Cadena verdadera máxima: Lola 3/3/3 y Coach 2/3/3 a 0/50/100 %; escapa siempre. Sin infinito con este guion. **Decisión de Santiago (22/11/2026): reducir hitstun.** 0.6.3: cada golpe repetido del mismo atacante al mismo rival dentro de la ventana de combo (1,1 s) multiplica el hitstun por `hitstunDecay` (0,8) con suelo `hitstunMinScale` (40 %), editables y validados en `BetaBalance`. Pruebas: 100/80/64/51 % en partida y reinicio al pasar la ventana; el combo escalera de 3 golpes sigue funcionando (estudio re-medido igual). Ajuste fino de los valores: con personas (fila de calibración).
- [ ] Calibrar ventanas, hitstun, knockback y escapes con personas (SYS-041/046/047/049/050)

## Poderes y supers

- [x] Super de Tío Antena con refugio central de ±1,4 m (SYS-050)
- [x] Avisos de los cinco supers, del fuerte y del token en X/Y (SYS-043/073)
- [x] Pruebas de los cinco supers y los especiales (88 casos)
- [x] Verificar especial, recuperación y super de cada kit en todas las situaciones (acierto, fallo, interrupción, pausa, plataforma y borde)
  - 0.6.4: matriz `BrawlKitMatrixChecks` (`-kits-smoke`, también en `-side-smoke`): 5 kits × especial/super × 6 situaciones + recuperación × 6 = 90 casos, todos superados. Fallos reales encontrados y corregidos: (1) interrumpir el super de Lola Viral o Cripto King dejaba su zona/burbuja pendiente y estallaba igual → ahora se cancela (`BrawlZone.CancelFor`); (2) la embestida de Coach Tóxico seguía tras conectar y lo sacaba de la plataforma → ahora se detiene al conectar (fallar mantiene el avance completo). La recuperación se verifica volviendo desde ambos bordes desde cerca (0,6 m fuera); el alcance máximo por kit es la tarea SYS-030–035.
- [ ] Aceptar los cinco kits con personas

## Controles y dispositivos

- [x] Asignación estable de mandos, teclado + mando, dos mandos y 2 a 5 jugadores
- [x] Pausa y recuperación al desconectar un mando (con mandos virtuales)
- [x] Mandos de PlayStation funcionando por posición de botones (sin probar en físico)
  - Prueba física parcial (0.4.5/0.4.6, DualShock 4 v2 por USB, modo 3D): reconocido como `DualShock4GamepadHID`, barra de luz y vibración funcionando; vibración corregida tras la prueba humana.
- [ ] Probar con mandos físicos Xbox, DualShock 4 y DualSense, por USB y Bluetooth (QA-445)
  - Parcial: solo DualShock 4 por USB. Nota técnica: el Input System 1.20 no envía vibración al DualSense por Bluetooth (FIXME en `DualShockGamepadHID.cs`).
- [x] Etiquetas de botones según el mando conectado (✕ □ △ ○ o A B X Y)
  - Verificado en 2.5D: PlayStation (Cruz/Círculo/Cuadrado/Triángulo), Xbox (A/B/X/Y) y genérico, desde la misma tabla de bindings; panel CONTROLES y barra inferior. Se muestran como texto («Cruz»), no glifos. El tutorial aún no existe (SYS-076).
- [x] P1-01 · Remapeo con detección de conflictos (SYS-025)
  - Implementado (0.4.2): tabla única `BrawlBindings`, captura interactiva, intercambio con aviso, Esc/Start reservados, restaurar por dispositivo. En 0.6.0 las filas se rotulan para 2.5D («Arriba: dirección de golpe»). Verificado en 2.5D: asignación, conflicto con intercambio, reservados, captura con teclado y mando, el luchador usa el botón remapeado en partida.
- [x] P1-02 · Opciones y controles guardados de forma recuperable (SYS-068/069)
  - Implementado (0.4.2–0.4.5): `funados-options.json` versión 3, escritura temporal verificada + `.bak`, cuarentena de dañados, migración v1/v2, modo de entrada incluido. Pendiente además: la prueba humana de 0.4.5 no encontró archivo guardado; en 0.4.6 se añadió registro de carga/guardado y una reproducción en modo normal **sí** guardó. Verificado en 2.5D: guardado, reinicio, truncado/vacío/incompleto/ajeno, copia, temporal huérfano, migraciones v1→v3. Confirmar con persona el guardado en uso normal.
- [x] Deadzones, sensibilidad y vibración ajustables (SYS-026/072)
  - Implementado (0.4.3/0.4.6): pestaña MANDO; vibración con mínimo perceptible confirmada por persona con DualShock 4. Verificado en 2.5D con la regla por ejes. SYS-072 sigue abierta en sus demás partes (destellos/sacudida/fondo por separado).

## Escenarios

- [x] Cuatro arenas de producción con arte y adaptación lateral
- [ ] Distribuciones jugables distintas por arena (ARENA-350 a 367)
- [ ] Revisar límites y puntos de aparición de cada arena (ARENA-358/359)
- [ ] Cien KO con reaparición reales y veinte partidas por arena (ARENA-366)
- [ ] Diseñar las cuatro arenas restantes del catálogo (CIERRE-460/461)

## Interfaz y accesibilidad

- [x] Opción de reducir destellos y temblor, y volúmenes separados
- [ ] Decidir el framework de interfaz y migrar desde IMGUI (D07)
- [ ] HUD nuevo: porcentaje grande, vidas, escándalo, combo e indicadores fuera de pantalla
- [x] Navegación completa con teclado y mando, con foco visible (SYS-070)
  - Implementado (0.4.4) sobre IMGUI: foco espacial con borde blanco en menú, pausa y resultados; ✕/A confirma, ○/B vuelve de la pausa. Independiente de la decisión D07. Verificado en 2.5D: 22 casos de navegación, pausa, resultados y mando sobrante.
- [ ] Texto y HUD escalables, alto contraste y prueba de daltonismo (SYS-071)
  - Implementado (0.4.5): pestaña PANTALLA (texto/HUD 100–130 %), mínimo legible por resolución, ajuste de texto a su rectángulo, HUD sin bandas en 4:3/16:10/21:9. Texto/HUD escalables verificados en 2.5D (15 casos, capturas a 960×720, 1280×800 y 1280×548). **Faltan alto contraste y prueba de daltonismo**: la casilla sigue abierta.
- [ ] Tutorial de combo y recuperación sin límite de tiempo (SYS-076)
- [ ] Retratos de personajes en la selección

## Arte y animación

- [ ] Rig real para un personaje de referencia (Don Promesio) y decidir la herramienta (D01)
- [ ] Animaciones de perfil mínimas para los cinco kits (lista en la sección 11)
- [ ] Shader toon y contorno únicos en URP
- [ ] Efectos de impacto y supers con versión de efectos reducidos
- [ ] Revisar lectura del neón, el contraste y los accesorios en cámara lateral

## Audio

- [ ] Buses Master, Música, SFX, Voces y UI (AUDIO-428 a 434)
- [ ] Efectos finales de golpes, remate, esquiva, recuperación y KO
- [ ] Música por arena, voces de personajes, subtítulos y créditos

## Calidad y cierre

- [x] Regresión histórica de 109 casos conservada (CIERRE-459)
- [ ] Medir rendimiento en build con cinco luchadores (QA-435 a 440)
- [ ] Sesión de 30 minutos con revanchas sin bloqueos ni fugas
- [ ] Playtests con personas según el protocolo (combo escalera y recuperación)
- [ ] Aceptar Don Promesio, Lola Viral, una arena, dos dispositivos, resultados y accesibilidad (CIERRE-455 a 458)

## Expansión (después de la vertical slice)

- [ ] Ocho kits restantes, de dos en dos (D05)
- [ ] Juego local completo de 13 personajes y 8 arenas (CIERRE-460 a 467)
- [ ] Investigación online con puerta de viabilidad (D06)

## Rutas y comandos

- Escenas: `Assets/FunadosProduction/Scenes/Funados.unity` y `CombatLab.unity`.
- Generar: menú Unity **FUNADOS → Generar Windows** → `Builds/Funados/<versión>/Windows/FUNADOS.exe`; informe en `Evidence/Production-<versión>/build.txt`.
- Suites (añadir `-evidence-path <carpeta nueva> -options-path <carpeta nueva>\player-options.json -force-d3d11 -screen-width 1280 -screen-height 720 -screen-fullscreen 0 -logFile <carpeta>\player.log`):
  - `-power-smoke`: poderes y avisos 2.5D.
  - `-side-controls-smoke`: dispositivos, remapeo, ajustes de mando, menús y pantalla **en 2.5D** (nuevo en 0.6.0).
  - `-side-smoke`: reglas 2.5D completas + poderes + las suites de controles.
  - `-controls-smoke` / `-beta-smoke`: regresión histórica 3D (no es el diseño actual).
- Script de conveniencia usado en esta sesión: ejecutar las cinco suites en orden y resumir sus informes.
- **Mandos físicos**: desconectarlos, o añadir `-isolate-physical-pads` (los retira solo del proceso de prueba). Sin esa opción las suites se niegan con un mando real. Un DualShock emparejado por Bluetooth y apagado aparece en Windows como `BTHENUM` sin estar conectado. Pueden correr con la ventana sin foco.
