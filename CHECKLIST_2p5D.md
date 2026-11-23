# Checklist activa de FUNADOS 2.5D (GDD 2.0)

Fuente: **GDD 2.0 (combate lateral 2.5D), sección 15** — `Docs/Produccion_2026-11-22/FUNADOS_GDD_2.0.docx`. Esta lista sigue sus 63 tareas con el mismo orden y redacción; las notas de evidencia van debajo de cada una. Estado de trabajo: **0.7.0** (base lateral 0.5.1 de `main` + controles 0.4.2–0.4.6 de la rama `claude/funados-implementation-continue-db5f40`). Última actualización: 22/11/2026. Editor Unity **6000.5.7f1**.

`[x]` = implementado con evidencia técnica (pruebas automáticas de la versión indicada), **no** aceptado con personas. `[ ]` = pendiente. **⧗** = implementado en 0.6.0, pero su verificación automática en 2.5D aún no se ha ejecutado: no se marca hasta que pasen las suites. No equiparar cantidad de pruebas con casillas cerradas.

Recuento: **37 de 63** marcadas (20 del GDD 2.0 + 17 desde 0.6.0). Ninguna en ⧗. Evidencia vigente 0.7.0 (`Evidence/Production-0.7.0`): beta 208/208; arena-stress 25/25 (`Evidence/Branch-feat-arena-ko-stress`).

## Instrucciones para el siguiente agente

1. Leer esta lista, el GDD 2.0 y `CONTINUAR.md`; comprobar `git status` y `git log -4 --oneline`.
2. Tras cada tarea: ejecutar las suites (ver «Rutas y comandos»), marcar aquí con su evidencia y regenerar la copia marcada del GDD con `python -I Docs/Produccion_2026-11-22/marcar_checklist_gdd.py` (`FUNADOS_GDD_2.0_checklist.docx`).
3. Elegir la primera tarea pendiente de prioridad alta, implementarla, verificarla, marcarla aquí y en la copia del GDD, y crear un commit. Remoto: `origin` (GitHub). Para trabajo en paralelo, seguir `PARALELO.md` (una rama por área, PR a `main`, sin push directo a `main`).

## Base del proyecto

- [x] Diseño lateral 2.5D confirmado (16/11/2026)
- [x] Cinco kits, once aspectos y cuatro escenarios conservados, con ID y GUID
- [x] Escenas Funados y CombatLab; beta y pruebas históricas preservadas
- [x] Git + Git LFS; editor y paquetes fijados (SYS-005/006/007/009)
- [x] Restaurar desde Git/LFS en un entorno limpio (SYS-004/006, QA-441)
  - 23/11/2026, rama `chore/restauracion-licencias` (`Tools/restaurar_limpio.ps1`, informe `Docs/Restauracion/Restauracion_limpia_0.7.0.md`, evidencia `Evidence/Branch-chore-restauracion-licencias`). Se clonó `main` 0.7.0 (`130a3c6`) desde GitHub en una carpeta nueva y se ejecutó `git lfs pull`: 562 MB en 62 s y 0 punteros en 1.403 archivos LFS. La importación desde cero tardó 20 min, con 0 errores. La build salió en 6 min (Succeeded, 489 avisos). Sobre esa build, `-arena-smoke` 46/0 y `-side-smoke` 375/0. Total: 40,5 min. Límite: mismo equipo, cuenta de Windows y Unity instalado; en una máquina virgen hay que instalar además Hub, el editor y Git LFS.
- [ ] Procedencia y licencias de modelos, escenarios, audio y dependencias (SYS-017, D04)
  - 23/11/2026: inventario en `Docs/Licencias/Procedencia_y_licencias.md` y los CSV, que se regeneran con `python -I Tools/inventario_licencias.py`. Quedan **confirmados** los 68 paquetes (Unity Companion / Package Distribution / MIT / ToS de Unity), el shader, el audio (sintetizado por código), las fuentes (las del motor), el cubo de validación, las capturas y la plantilla URP. **Pendientes del usuario (D04):** los 110 archivos de Meshy (22 de personajes, 56 de modelos y texturas de escenarios y 32 conceptos, estos con XMP de Meshy `nano-banana-pro` y su ContentID). Falta saber con qué plan de Meshy se generaron cada lote (Free = CC BY 4.0 con atribución), si los personajes los generó el usuario o salieron de la comunidad, y si las imágenes de referencia tenían permiso. Avisos: `com.unity.ai.assistant` (pre) y `com.unity.pipeline` (exp) son del Editor; conviene quitarlos antes de publicar.

## Movimiento, cámara y plano 2.5D

- [x] Movimiento X/Y con Z bloqueado, cámara lateral ortográfica y doble salto
- [x] Plataformas atravesables desde abajo y S + Espacio para bajar
- [x] KO por izquierda, derecha, abajo y arriba (20 m)
- [x] La cruceta elige dirección de golpe sin frenar el movimiento horizontal
  - 0.6.0 conserva la regla por ejes también con teclado remapeado y con la palanca tras zona muerta/curva (`BrawlMatch.ReadKeyboardButtons` / `ReadGamepad`).
- [x] Recuperación desde ambos bordes con los cinco kits (SYS-030 a 035)
  - 0.6.5 (`BrawlRecoveryChecks`, `-recovery-smoke`): SYS-030 salto, segundo salto al 94 %, sin tercer salto, caída acelerada y recursos aéreos consumidos una vez; SYS-031 aterrizar restaura y rozar pared o techo no; SYS-032 una recuperación por fase, vulnerable y con final; SYS-033 caída a máxima velocidad sobre suelo y plataforma y esquiva contra pared sin atravesar; SYS-034 indicador de apoyo sobre plataforma y suelo; SYS-035 sin pared invisible en el borde y alcance medido por búsqueda binaria, simétrico entre bordes. Alcance de vuelta desde fuera del borde (a nivel / 2 m abajo): Don Promesio 8,8/8,1 m, Lola Viral 11,8/11,1, Coach Tóxico 8,3/7,7 (el menor, ARENA-357), Cripto King 9,4/8,6, Tío Antena 11,8/11,8 (`recovery-reach.json`). **Para calibrar:** el alcance es muy generoso (Lola y Tío casi en el tope medido) y el indicador de apoyo se ve fino en cámara lateral.
- [x] Cámara y avisos fuera de pantalla con cinco luchadores y alturas extremas (SYS-036 a 040)
  - 0.6.6 (`BrawlCameraChecks`, `-camera-smoke`): la cámara lateral encuadra a todos (cabeza y marcador incluidos) **por debajo del HUD superior**, con zoom entre 5,5 y 16; antes podía dejar a un luchador alto bajo el reloj. Rotación fija: apuntar nunca cambia de referencia. Oclusores atenuados sin tocar el suelo jugable. Nuevo aviso de límite (SYS-039): a menos de 3 m de un límite de expulsión el marcador se vuelve rosa con «!», también en el borde de pantalla. Probado en las cuatro arenas con cinco luchadores separados, extremos (lateral y superior) y juntos (`camera-*.png`). La reserva de la franja inferior y otras proporciones ya se probaron en 0.4.5/0.6.0.
- [x] Decidir y, si se aprueba, añadir coyote time
  - 0.6.7: medido (`BrawlCoyoteStudy`, `coyote-study.json`): sin coyote, pulsar salto en el primer instante tras salir caminando del borde ya gastaba el salto aéreo (velocidad 9,3 en vez de 10,0 y sin segundo salto). **Decisión de Santiago (22/11/2026): 3 ticks (0,05 s)** (`BetaBalance.coyoteSeconds`). Una pulsación hasta 3 ticks tarde cuenta como salto de suelo y conserva el aéreo; 4 o más, no. Nunca tras un salto, un golpe, al bajar de una plataforma ni al aparecer en el aire.

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
- [x] Distribuciones jugables distintas por arena (ARENA-350 a 367)
  - 0.6.9 (`BrawlArena.SideLayouts`, solo la instancia 2.5D; prefabs y arte intactos). Las dos plataformas existentes de cada arena se reubican: **Azotea Influencer** ±4,8 m a 2,6 m (neutral, referencia de torneo y balance); **Torre Corporativa** ±6,0 m a 3,6 m (vertical, solo con doble salto, el KO superior llega antes); **Estudio de Debate** ±5,6 m a 2,1 m (atriles bajos, basta un salto, se pelea cerca del suelo y de los bordes); **Plaza Colonial** ±10 m a 2,2 m (balcones sobre el vacío, recuperaciones arriesgadas). La cámara encuadra siempre toda la distribución (`PlayBounds`), aunque las plataformas estén vacías. Verificado en `-arena-smoke` (46 comprobaciones): las plataformas están en su sitio y son finas; la cámara muestra ambas; Coach, el kit de menor alcance, sube a las dos con doble salto; al bajarse se llega al suelo principal (en Plaza, al vacío); cada par de arenas difiere en al menos 0,8 m. Los límites de KO siguen iguales en las cuatro (±22, −10, +20). Falta calibrar la dificultad con personas.
- [x] Revisar límites y puntos de aparición de cada arena (ARENA-358/359)
  - 0.6.8 (`BrawlArenaChecks`, `-arena-smoke`), en las cuatro arenas: KO exacto a izquierda/derecha (±22 m), abajo (−10) y arriba (+20), una vida por límite cruzado, más allá del encuadre lateral (±19); retirado el KO en profundidad que aún existía en el código. Cinco apariciones en el plano, separadas ≥2 m, sobre suelo real y sin solaparse con geometría; cinco luchadores empiezan apoyados; la reaparición evita trampas activas. **Fallo real corregido:** en Torre Corporativa, Estudio de Debate y Plaza Colonial las plataformas atravesables conservaban el grosor del prefab (el balcón de Plaza Colonial bajaba hasta 0,4 m) y actuaban como pared invisible para golpes y proyectiles de quien estaba debajo; ahora son losas de 0,3 m con la cara superior en 2,6 m (solo la instancia 2.5D, el prefab y el arte no cambian). Límites en `arena-limits.json`; son iguales en las cuatro arenas porque comparten geometría hasta que se diferencien (ARENA-350–367).
- [x] Cien KO con reaparición reales y veinte partidas por arena (ARENA-366)
  - 0.7.0, juntada desde la rama `feat/arena-ko-stress` (`BrawlArenaStress`, `-arena-stress`, 25 comprobaciones; evidencia en `Evidence/Branch-feat-arena-ko-stress/ArenaStress/arena-stress.json`). Por arena: 20 partidas completas de cinco luchadores (cinco kits rotando) jugadas por los bots con las reglas reales: golpes, retroceso, expulsión, vidas, reaparición, protección y resultados. KO por arena: Azotea 219 · Torre 217 · Estudio 221 · Plaza 201, todos atribuidos a un rival. Cada KO resta exactamente una vida; todas las reapariciones llegan a los 2 s (error máx. 0,017 s), dentro de los límites, en el plano y protegidas; se aterriza en suelo real (máx. 1,0 s en el aire) y nadie vuelve a caer mientras está protegido; sin posiciones inválidas; sin fugas (0 objetos y ≤0,3 MB tras 19 partidas). Para que la prueba sea corta se mantiene a los luchadores al 300 % y se simula a 16×. **Observación:** 78 de 80 partidas terminan por tiempo (300 s); con cinco jugadores y 3 vidas los bots casi nunca eliminan a todos. Es un dato para el balance y la IA, no un fallo.
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
- [x] Medir rendimiento en build con cinco luchadores (QA-435 a 440)
  - Rama `perf/rendimiento`: `BrawlPerfSuite` (`-perf-smoke`), build Windows con RTX 4050 Laptop (el equipo de referencia del GDD), D3D11 a 1280×720, velocidad real (timeScale 1, 60 Hz) y efectos completos: cinco bots por arena, 60 s de juego medido cada una, super cada 4 s y daño ≥130 % para que haya lanzamientos y KO. Las cuatro arenas: **60,0 FPS medios, 1 % bajo 57,5–59,2, frame máximo 17–30 ms; CPU principal mediana 2,7–3,0 ms, p95 3,8–4,1 ms, pico 7,4 ms; GPU mediana 2,1–2,8 ms, p95 3,9–5,1 ms, pico 8,2 ms** (objetivo GDD: <13,5 ms), simulación 60,0 Hz. Carga de arena con cinco luchadores 3–5 ms; vuelta al menú 16–21 ms. Memoria pico: gestionada 4–6,5 MB, nativa de Unity 197–199 MB, proceso 688–713 MB. Cinco ciclos por las cuatro arenas: 4169 objetos de Unity constantes. Máquina libre durante la medición (CPU media 5 %, ningún otro Unity ni FUNADOS). Umbrales propuestos donde el GDD no fija cifra: FPS medio ≥58, 1 % bajo ≥45, frame <100 ms, carga <2 s, menú <0,5 s. 35/35, `Evidence/Branch-perf-rendimiento/Perf/perf-smoke.json`.
- [x] Sesión de 30 minutos con revanchas sin bloqueos ni fugas
  - `-soak-session` (misma suite): 31,2 min a velocidad real, **20 partidas de cinco bots (13 revanchas encadenadas con REVANCHA y 6 vueltas al menú con cambio de arena y personaje), 88 KO, 0 bloqueos, 0 errores; 60,0 FPS medios, 1 % bajo 58,0, ningún frame >100 ms**; GameObjects +4 y objetos de Unity +13 como máximo frente a la primera revancha de la misma arena (avisos de super vivos al acabar, que se destruyen con su luchador); gestionada +2,0 MB y proceso +4 MB tras calentarse (estable en ~7,8 MB y ~696 MB). Partidas de 90 s de reloj para poder encadenar revanchas (con 300 s los bots acaban por tiempo). 6/6, `Evidence/Branch-perf-rendimiento/Soak/soak-session.json`.
  - Corregido gracias a esta prueba: el anillo y la flecha de cada luchador leían `LineRenderer.material`, que clona el material y no se destruía: **5 materiales perdidos por partida** (+72 objetos en 30 min, `Soak-before-fix`). Ahora usan `sharedMaterial`; sin cambio visual.
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
  - `-perf-smoke` (≈5 min): rendimiento con cinco bots por arena a velocidad real (`perf-smoke.json`). `-soak-session` (≈32 min): sesión de 30 min con revanchas (`soak-session.json`); `-soak-seconds N` acorta la sesión para probar. Medir con la máquina libre: otra build o un Unity compilando falsean las cifras.
- Script de conveniencia usado en esta sesión: ejecutar las cinco suites en orden y resumir sus informes.
- **Mandos físicos**: desconectarlos, o añadir `-isolate-physical-pads` (los retira solo del proceso de prueba). Sin esa opción las suites se niegan con un mando real. Un DualShock emparejado por Bluetooth y apagado aparece en Windows como `BTHENUM` sin estar conectado. Pueden correr con la ventana sin foco.
