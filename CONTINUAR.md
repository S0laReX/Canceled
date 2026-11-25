# Continuación de FUNADOS

**Trabajo en paralelo:** ver `PARALELO.md` (ramas abiertas en `origin`, reglas y orden de merge).

**Empezar por `CHECKLIST_2p5D.md`**, que ahora sigue las 63 tareas del **GDD 2.0** (`Docs/Produccion_2026-11-22/FUNADOS_GDD_2.0.docx`, vigente desde el 22/11/2026). Los apartados 0.5.1/0.5.0/0.4.1 siguientes son historial.

## Relevo perf/rendimiento: lectura en cámara lateral y fondo atenuado (sobre 0.7.4)

- Suite nueva `-read-smoke` (`BrawlReadChecks.cs`): 20 comprobaciones sobre 4 arenas, 11 aspectos y 88 casos. Queda fuera de `run_all` hasta que se resuelva el hallazgo abierto.
- Umbrales: el §9 del GDD es literal; el perfil viene del §11 y la altura a 720p del §10. Los de la propia suite son ΔE ≥ 10 y una separación ≥ 60 % en la silueta y en el borde.
- Cuenta atrás: `BrawlFighter.RespawnAt` ya pone el luchador de perfil. Antes, sin `Tick`, salía de frente. Resultado: 120/120.
- §9 sin atenuar. El fondo tenía más croma que los luchadores en tres arenas, y Torre lo cumplía por muy poco:

  | Arena | Croma del fondo | Croma de los luchadores |
  |---|---|---|
  | Azotea | 46,5 | 26,5 |
  | Estudio | 31,4 | 21,5 |
  | Plaza | 27,9 | 21,8 |
  | Torre | 24,0 | 24,3 |

- Solución: `BrawlBackgroundGrade.cs`, con el shader `Resources/FunadosDesaturate.shader`.
  - Al cargar la arena (`BeginRound`, una línea) copia en la GPU las texturas del escenario con menos saturación y las aplica con `MaterialPropertyBlock`. Los archivos de arte no cambian.
  - Saturación del albedo que se conserva: Azotea 0,40, Torre 0,85, Estudio 0,30 y Plaza 0,60. El neón pierde la mitad de esa reducción.
  - Cada factor es el mayor que deja la croma del fondo en ≤ 0,85 × la de los luchadores. La suite lo vuelve a comprobar con un barrido de calibración.
  - Coste: 6,8 ms, una sola vez por arena.
- Con el atenuado:

  | Arena | Croma del fondo | Croma de los luchadores |
  |---|---|---|
  | Azotea | 22,4 | 26,5 |
  | Torre | 20,5 | 24,3 |
  | Estudio | 17,3 | 21,3 |
  | Plaza | 17,7 | 21,8 |

  El contraste L* del fondo también queda por debajo del de los luchadores en las cuatro arenas.
- Con el atenuado, el resto de la suite:
  - Daltonismo: el peor caso es The Dapper Tycoon en Estudio con protanopía, con un 64 %. Victoria Vexley, que estaba en 59 %, ya pasa.
  - Separación: el peor caso es del 67 %.
  - Borde: el peor caso es del 67 %.
  - Altura mínima: 79 px.
  - Neón: 100 %.
- Opción «FONDO ATENUADO»: campo opcional `dimBackground` del archivo de opciones v4, activado por defecto. Todavía no tiene botón en el menú.
- **Abierto**: de perfil, Commander Vex y Smirking Tycoon se solapan 0,86, por encima del umbral de 0,80. Por eso Read da `passed=false`. No se corrige en esta rama.
- Regresión sobre 0.7.4: Audio 25/25, Ui 31/31, Music 34/34 y Perf 35/35. `-arena-stress` falló una de 24 comprobaciones en cada una de dos pasadas, y no fue la misma:
  - Primera pasada (`ArenaStress`): en Torre, P4 seguía en el aire 3 s después de reaparecer, sin estar en hitstun, en la partida 19.
  - Repetición (`ArenaStress-rerun`): en Azotea, un final por vidas dio los puestos 1,2,3,3,5. Dos luchadores eliminados en el mismo paso comparten puesto, y la prueba exige puestos distintos.
  - Ninguno de los dos depende del atenuado ni de la rotación de la cuenta atrás: no tocan la física ni los puestos.
  - Empate (decisión del padre): se usa el ranking de competición estándar. El juego ya lo hacía (`BrawlMatch`: los eliminados en el mismo paso reciben `survivors + 1`) y la pantalla de resultados ya ordena por puesto y muestra «#3» dos veces. Ahora `-arena-stress` lo acepta en finales por vidas y por tiempo: cada puesto es 1 más el número de luchadores por delante.
  - P4 en el aire en Torre, investigado con el código:
    - No es la protección: se agota en 2 s pase lo que pase y no toca la gravedad.
    - No hay plataforma de reaparición: se reaparece en el suelo, 0,12 m por encima.
    - Los golpes ponen al menos 0,035 s de pausa (`InHitstun`), y la prueba muestrea cada paso fijo, así que un golpe no se le escapa.
    - Queda el movimiento propio del bot: salto, doble salto tardío y recuperación (13,8 m/s, 0,55 s a ¼ de gravedad). Puede tenerlo unos 3,5 s en el aire (estimado), y a los 3 s estaba a 5,48 m, bajando de esa altura.
    - No se reprodujo en la repetición (160/160 aterrizajes en Torre).
    - Cambio en la prueba: «atascado» es ahora quedarse quieto (< 5 cm en 0,5 s) en el aire, o pasar más de 6 s sin aterrizar. Las esperas de más de 3 s se cuentan aparte (`longAirAfterRespawn`).
    - Sin corrida todavía: se validará con las 23 suites sobre 0.7.5. La corrida de las 23 suites sobre 0.7.5 irá en `Read-0.7.5`.

## Relevo 0.7.5 (música, voces, subtítulos y créditos)

- Merge de `claude/rendimiento-095c80` (3de3dd5). Versión 0.7.5. Recuento 51/63.
- 22 suites pasan (ver `Evidence/Production-0.7.5/suites.txt`). Se añade `run_all22` con `-music-smoke`.
- Conocido: el player de Arena se quedó colgado al cerrar una vez (pasó 46/46). Tirón de Azotea a ~14 s: 61 ms, sigue abierto.
- En curso en la misma rama (sin merge todavía): `-read-smoke` (lectura y GDD §9) con fondo atenuado por arena (BrawlBackgroundGrade, opción dimBackground), corrección de orientación en la cuenta atrás y empates 1,2,3,3,5 en arena-stress. Hallazgo abierto: perfiles de Vex y Tycoon solapados al 0,86.

## Relevo perf/rendimiento: música, voces, subtítulos y créditos (sobre 0.7.4)

- Archivos nuevos:
  - `BrawlMusic.cs`: cinco pistas de 8 compases sintetizadas por código, una para el menú y una por arena, con bucle sin salto y normalizadas a RMS 0,17.
  - `BrawlVoices.cs`: 15 frases originales (KO, super y victoria por personaje), en un balbuceo sintetizado con tono propio por personaje. Sin grabaciones, sin clonar voces y sin imitar a nadie.
  - `BrawlSubtitles.cs` y `BrawlCredits.cs`.
  - `BrawlMusicChecks.cs`: la suite `-music-smoke`, 34 comprobaciones.
- `BrawlAudio`: solo la pista del menú y los efectos se crean al arrancar (≈200 + 110 ms). Las pistas de arena y las frases se generan en hilos (`Task.Run`), y `BrawlAudio.Poll()`, desde `BrawlMatch.Update`, convierte una por frame en clip. Si hace falta una que aún no está, `MusicClip` y `Clip` la terminan en el momento.
- `BrawlAudio.Play("ko"/"super", personaje, jugador)` dice la frase y muestra el subtítulo. Prioridad en el bus de voces: victoria > KO > super.
- `BrawlAudio.SpeakerNames` usa `BrawlMatch.CharacterName`, así que los nombres son los de producción.
- HUD, solo líneas mínimas:
  - `BrawlHud.cs`: la rama del menú dibuja los créditos y el botón «VOLVER» (`credits-back`) cuando están abiertos. Hay una línea `BrawlSubtitles.Draw` después de las fases y un botón «CRÉDITOS» (`credits`) en el menú.
  - Pantalla tiene un tercer interruptor, «SUBTÍTULOS» (`display-subtitles`).
  - `BrawlHud.Navigation.cs`: atrás cierra los créditos.
- Opciones: el campo opcional `subtitles` (por defecto `true`) sigue en v4. Un archivo sin ese campo carga como «Loaded».
- Créditos: el texto sale de `Docs/Licencias/CREDITOS.md`, que ahora tiene un apartado «Audio», y la línea de Meshy está literal.
- Evidencia: `Evidence/Branch-perf-rendimiento/Music-0.7.4`, con las 22 suites (Music es la 22) en una corrida. En la primera pasada de `-perf-smoke`, Azotea falló una condición de juego: los bots no hicieron ningún KO en 60 s, aunque los fotogramas estuvieron bien. Se repitió con la máquina libre y pasaron 35 de 35. El tirón de Azotea volvió a salir a los ≈14 s de juego, de 52 ms en la primera pasada y de 63 ms en la segunda, frente a los 56 ms de 0.7.4. La música no lo empeora de forma clara, pero sigue abierto. Los fotogramas por segundo medios fueron 59,9–60 en las cuatro arenas. `music-smoke` pasó 34 de 34 y el soak corto, 6 de 6.
- Pendiente: sustituir música y voces por el audio final y que personas lo escuchen y lo valoren.

## Relevo 0.7.4 (animaciones de perfil)

- Juntada `claude/rendimiento-095c80` (4ac02d2): animaciones de perfil para los cinco kits (giro, frenada, voltereta, caída, reaparición) en `BrawlFighter.cs` y `BrawlFighterVisual.Model.cs`, más la suite `-anim-smoke` (`BrawlAnimChecks.cs`, 15 comprobaciones). Las poses son procedurales sobre el rig automático: hay que cambiarlas por clips cuando se decida D01.
- Evidencia: `Evidence/Production-0.7.4/suites.txt`, con las 21 suites en una sola corrida. El tirón de Azotea volvió a salir a los 14,7 s, esta vez de 56 ms: sigue abierto.
- Checklist **50 de 63**. Siguiente en paralelo: la sesión de rendimiento hace música por arena, voces, subtítulos y créditos, en archivos nuevos y con una sola línea de llamada en el HUD y en el menú.

## Relevo D04 cerrada (licencias, 25/11/2026)

- Juntada la rama `chore/licencias-d04`. Santiago confirmó tres cosas: las imágenes de referencia eran suyas, los 11 personajes los generó él en Meshy y acepta la atribución CC BY 4.0. Por eso `Tools/inventario_licencias.py` marca los 110 archivos de Meshy como `confirmado` (120/120 archivos de arte y 68/68 paquetes) y los CSV están regenerados.
- Créditos en `Docs/Licencias/CREDITOS.md` y en el README. La casilla SYS-017 está marcada y el checklist va en **49 de 63**. Solo documentación: sin cambios de código ni de versión (sigue la 0.7.3).
- Antes de vender: pedir a Meshy por escrito que aclare el uso comercial del plan Free (o regenerar con un plan de pago) y quitar `com.unity.ai.assistant` y `com.unity.pipeline`.

## Relevo perf/rendimiento: animaciones de perfil (sobre 0.7.3)

- `BrawlPose` lleva cinco relojes nuevos (`Braking`, `Turn`, `Flip`, `Tumble`, `Spawn`) que calcula `BrawlFighter.UpdatePresentationClocks` antes de animar: giro al cambiar de lado (solo en vista lateral), frenada al soltar una carrera, voltereta en el doble salto, giro del lanzamiento fuerte (`TumbleStrength` 0,4; termina en un múltiplo de 360°) y entrada al reaparecer con protección. `ClearPresentationClocks` en `RespawnAt`.
- `BrawlFighterVisual.ApplyBody` aplica giro y escala sobre «Modelo» con pivote a 0,9 m, compuesto con la posición, rotación y escala que tenía en `Awake`; en reposo no toca nada (los retratos colocan ese objeto ellos mismos).
- Suite `-anim-smoke` (`BrawlAnimChecks.cs`, registrada en `BrawlMatch` y `BrawlSideSmokeProbe`), 15 comprobaciones. Para mover a P1 en pruebas: `MakeBotForTest` + `AutomatedTest` y `Submit` en cada `WaitForFixedUpdate`; tras un golpe esperar 4 pasos fijos (el hitstop congela la pose).
- Build sobre 0.7.3 (`Evidence/Branch-perf-rendimiento/Anim-0.7.3`): las 19 suites sin fallos (side 405, side-controls 157, beta 208, controls 125, kits 91, arena 46, fx 33, power 32, ui 31, audio 25, arena-stress 25, moves 21, sfx 18, recovery 16, camera 15, hud 15, anim 15, toon 9, coyote 5), sesión de 5 min 6/6 (20 KO, 0 frames >100 ms) y perf 35/35: 60,0 FPS, 1 % bajo 57,1–58,8, frame máximo 24 ms, CPU p95 ≤3,5 ms, GPU p95 ≤6,1 ms. En esta pasada no salió el tirón de GPU de 80–200 ms de la Azotea; sigue abierto hasta perfilarlo.
- Pendiente: sustituir las poses por clips cuando se decida D01 (rig real); la hurtbox sigue siendo un rectángulo.

## Relevo 0.7.3 (efectos, sonido por capas y shader toon)

- Junta la rama `claude/rendimiento-095c80` (antes `perf/rendimiento`): `BrawlImpactFx` (impactos, KO y super con partículas reutilizadas y modo reducido), `BrawlSfxSynth` + `BrawlAudio.PlayHit` (golpes por capas y escalera de combo) y el shader `FUNADOS/Toon` con contorno para todos los luchadores.
- Suites nuevas: `-fx-smoke`, `-sfx-smoke`, `-toon-smoke`. La evidencia de la rama está en `Evidence/Branch-perf-rendimiento/`; la de la build 0.7.3 (20 suites) en `Evidence/Production-0.7.3/suites.txt`.
- Checklist 48/63: marcadas «Shader toon y contorno», «Efectos de impacto y supers» y «Efectos finales de golpes…».
- Medir rendimiento con la máquina libre: un runner roto de 0.7.2 dejó una build abierta que causó el frame de 311 ms de la primera sesión de 5 min; ya está cerrado y se borraron sus carpetas basura (`Production-0.7.2/-`, `/s`).
- Precalentamientos al arrancar: clips de audio (`BrawlAudio.Prewarm`), partículas y bloom (`BrawlImpactFx`, 3 frames invisibles), glifos de la fuente del HUD (`BrawlHud.WarmGlyphs`). Reparado el mojibake de `BrawlMatch.cs` que entró con 0.7.2.
- **Problema conocido (pendiente):** un tirón de GPU de 80–200 ms, una vez por proceso, a ~14 s de juego en la primera arena (Azotea) de `-perf-smoke`; 4 de 5 corridas pasaron de 100 ms. No depende del KO (salió con 0 KO, sin reapariciones). Solo aparece desde que se juntó rendimiento con el HUD 0.7.2; por separado no lo tenían. Perf ahora anota último KO, reapariciones y cámara en cada tirón. Siguiente paso: perfilar con un Development Build y Profiler conectado en ese momento.

## Relevo 0.7.2 (HUD de combate y retratos)

- `BrawlHud.Combat.cs`: tarjeta nueva por luchador según el GDD 2.0 §10 (retrato, forma y puesto, porcentaje grande con color de calor y animación al subir, vidas como formas, estado y barra de ESCÁNDALO). Combo junto a cualquier atacante; marcadores del borde con forma. Sigue en IMGUI: la decisión D07 (uGUI o UI Toolkit) sigue abierta y este HUD es la referencia de qué debe mostrar.
- `BrawlPortraits`: retratos generados del modelo real (capa 30, cámara ortográfica aparte, 256×256, caché por personaje y aspecto). Se usan en la selección, los rivales y el combate.
- Suite `-hud-smoke` (registrada en `BrawlMatch` y `BrawlSideSmokeProbe`): retratos, calor y contraste del porcentaje, animación y efectos reducidos, vidas, formas, combo de P3, marcador fuera de cámara y capturas a 1280×720.
- Checklist **45 de 63**. En paralelo, la sesión de rendimiento trabaja los efectos de impacto y supers con efectos reducidos (archivos de efectos, no de HUD).

## Relevo 0.7.1 (juntadas restauración, rendimiento, audio y UI)

- Juntadas en `main`, en el orden de `PARALELO.md`: `chore/restauracion-licencias`, `perf/rendimiento`, `feat/audio-buses` y `feat/ui-accesibilidad`. Checklist **43 de 63**.
- Conflictos resueltos: las opciones `-perf-smoke`, `-soak-session`, `-audio-smoke` y `-ui-smoke` van juntas en `BrawlMatch` y en `BrawlSideSmokeProbe`. Opciones: formato **v4** (audio) con `highContrast` y `colorBlindPalette` como campos opcionales (UI). Los archivos v1–v3 se migran, así que `-ui-smoke` ahora espera `MigratedLegacy` para v3.
- Evidencia en `Evidence/Production-0.7.1` (`suites.txt`): las 11 suites de siempre más audio, ui, perf, una sesión corta (`-soak-session -soak-seconds 120`) y arena-stress.
- Pendiente de personas o de Santiago: licencias de Meshy (D04), jugadores daltónicos reales, claridad del tutorial, efectos finales de audio y volver a medir el rendimiento cuando llegue el arte final.

## Relevo chore/licencias-d04

- Términos de Meshy consultados (fuentes en `Docs/Licencias/Procedencia_y_licencias.md`): Free = CC BY 4.0 con atribución, de pago = propiedad privada, comunidad = CC0, y cada modelo conserva los derechos de su plan. Las imágenes de referencia necesitan derechos en cualquier plan. Un artículo de marketplaces contradice el uso comercial del plan Free: aclararlo con Meshy antes de vender.
- `Docs/Licencias/CREDITOS.md` con la atribución «Model created with Meshy – CC BY 4.0 License», que cubre el peor caso. La casilla SYS-017 sigue abierta hasta que Santiago confirme las imágenes de referencia y el origen de los personajes, y acepte la atribución (D04). En el navegador integrado no había sesión de Meshy para ver el plan y los assets.
- Solo documentación; sin cambios de código, versión ni recuento.

## Relevo chore/restauracion-licencias

- **Restauración limpia (SYS-004/006, QA-441): pasa.** `Tools/restaurar_limpio.ps1 -Destino <carpeta nueva>` clona, ejecuta `git lfs pull`, comprueba los punteros, importa en batchmode sin `Library`, genera la build y ejecuta `-arena-smoke` y `-side-smoke`, con tiempos en `_restauracion/resumen.json`. Resultado sobre `main` 0.7.0: 40,5 min en total (LFS 562 MB en 62 s, importación 20 min, build 6 min, arena 46/0, side 375/0). Informe: `Docs/Restauracion/Restauracion_limpia_0.7.0.md`. No clonar más de una vez al mes sin motivo: es 1 GB de LFS gratuito al mes en GitHub y cada clon gasta unos 560 MB. El guion reutiliza un clon existente.
- **Licencias (SYS-017, D04): inventario hecho, casilla abierta.** `Docs/Licencias/Procedencia_y_licencias.md` + `inventario_activos.csv` (119 archivos con SHA256) + `inventario_paquetes.csv` (68 paquetes), regenerables con `python -I Tools/inventario_licencias.py --package-cache <proyecto>/Library/PackageCache`. Todo confirmado salvo los 110 archivos de Meshy (personajes, escenarios y conceptos), que dependen del plan de la cuenta al generarlos y de si los personajes salieron de la comunidad. Esa es la decisión D04 de Santiago; los pasos para cerrarla están al final del documento.
- Sin cambios en el código del juego ni en la versión. Tras importar, Unity reescribe 10 YAML/JSON con LF: con `core.autocrlf` Git los marca como modificados, sin cambios de contenido.


## Relevo perf/rendimiento (QA-435–440 y sesión de 30 minutos)

- `BrawlPerfSuite` (registrada en `BrawlMatch` y `BrawlSideSmokeProbe`): `-perf-smoke` (≈5 min) mide en la build, a velocidad real y con efectos completos, cinco bots por arena: FPS medio, 1 % bajo, tiempo de frame, CPU principal/render y GPU (mediana, p95, pico, con `FrameTimingManager`), memoria gestionada, nativa y del proceso, carga de arena, vuelta al menú y un censo de objetos tras cinco ciclos de arenas. `-soak-session` (≈32 min) encadena revanchas y vueltas al menú durante 30 min y comprueba bloqueos, errores, tirones y crecimiento de objetos y memoria (`-soak-seconds N` para una prueba corta; el límite del probe sube a 3300 s solo con esta opción). JSON en `perf-smoke.json` y `soak-session.json`.
- `ProjectSettings`: `enableFrameTimingStats: 1` para que la build de release mida CPU y GPU por frame (coste despreciable). Prueba: `BrawlFighter.MakeBotForTest` hace bot también a P1; durante estas suites se desactivan teclado y ratón (el probe lee la entrada en segundo plano y un Esc ajeno pausaba la partida; así se bloqueó una medición, `Perf-lockup-before-isolation`).
- Resultado en RTX 4050 Laptop (el equipo de referencia del GDD), máquina libre: 60,0 FPS, 1 % bajo ≥57,5, CPU p95 ≤4,1 ms y GPU p95 ≤5,1 ms (presupuesto 13,5 ms), picos 7,4/8,2 ms; carga 3–5 ms; 30 min: 20 partidas, 0 errores, 0 frames >100 ms, memoria plana. Hay margen de sobra: el cuello de botella no es el rendimiento.
- **Corregido**: el anillo y la flecha de cada luchador clonaban su material (`LineRenderer.material`) y lo perdían: 5 materiales por partida. Ahora `sharedMaterial` (`BrawlFighter.cs`, sin cambio visual).
- Umbrales propuestos (el GDD solo fija 60 FPS y 13,5 ms con mediana, p95 y picos): FPS medio ≥58, 1 % bajo ≥45, frame <100 ms, carga <2 s, menú <0,5 s, gestionada <256 MB, proceso <2 GB; en 30 min: ningún frame >250 ms, ≤5 GameObjects y ≤20 objetos de más en la misma arena, gestionada <16 MB y proceso <128 MB de crecimiento. Las partidas de la sesión duran 90 s de reloj para encadenar revanchas.
- Límites: los efectos son los actuales (sin el shader toon ni los VFX finales del GDD); volver a medir cuando lleguen el arte y las animaciones. El pico de 630 ms de CPU en `soak-session.json` es el frame del censo de objetos de la propia prueba, fuera de la partida.
- Suites en la rama (`Evidence/Branch-perf-rendimiento`): perf 35, soak 6, power 32, moves 21, kits 91, recovery 16, camera 15, coyote 5, arena 46, side-controls 127, side 375, controls 125, beta 208, arena-stress 25; cero fallos. Sin cambio de versión ni de recuento; al juntar, `-perf-smoke` y `-soak-session` van en la misma línea de opciones de `BrawlMatch` que `-audio-smoke`.

## Relevo perf/rendimiento: efectos de impacto y supers (Arte y animación)

- `BrawlImpactFx` (creado por `BrawlMatch` junto a la cámara; `BrawlImpactFx.Instance`): niveles `Light`, `Strong`, `Spike`, `Ko` y `Super`. `BrawlFighter.ApplyHits` elige el nivel (`ImpactTierFor`: lanzamiento hacia abajo = remate; fuerte, especial o empuje >45 % = fuerte) con el color del atacante; `LoseStock` dibuja el KO dentro de `PlayBounds`; el super al activarse; zonas y emisor en `BrawlHazards`. `BrawlFeedback.Burst` (saltos, esquivas, golpes al aire) ya no crea cubos: usa el sistema `Polvo`.
- Bloom: un `Volume` global de peso 0 que sube un instante en golpes fuertes, KO y supers; la cámara activa el posproceso (el perfil por defecto no tiene overrides, así que no cambia nada más). `GraphicsSettings` incluye el shader `Universal Render Pipeline/Particles/Unlit` (lo añade también `BetaProjectBuilder`). La textura lleva el degradado en el color además del alfa, para que el aditivo quede suave aunque la build no tenga la variante transparente.
- Efectos reducidos: anillo y estelas sí; destello, bloom y 2/3 de las chispas no. La pausa congela las partículas; volver al menú las borra.
- Build 0.7.1 con efectos, sonidos y toon (`Evidence/Branch-perf-rendimiento/Fx-0.7.1`): `-fx-smoke` 32/32, `-sfx-smoke` 18/18, `-toon-smoke` 9/9, `-perf-smoke` 35/35 (60,0 FPS, 1 % bajo ≥57,6, CPU p95 ≤3,5 ms, GPU p95 ≤6,0 ms, 0 KB/frame), sesión de 5 min 6/6, y las 14 suites anteriores sin fallos (side 405, side-controls 157, beta 208, controls 125, kits 91, arena 46, power 32, ui 31, audio 25, arena-stress 25, moves 21, recovery 16, camera 15, coyote 5). De las suites de regresión solo se guardan los informes (sin capturas).
- Rebase sobre `main` 0.7.2 (HUD y retratos): conflicto solo en la línea de opciones de `BrawlMatch` (ahora con `-hud-smoke` y las tres nuevas). En `Fx-0.7.2` las 18 suites pasan, `-hud-smoke` 14/14 incluido, y perf 35/35. La sesión de 5 min falló una vez por un frame de 311 ms que coincidió con un pico de CPU ajeno (`SoakShort`); repetida pasa 6/6 (`SoakShort-rerun`). El 1 % bajo en 0.7.2 queda en 54–59 FPS (frente a 58 en 0.7.1), por encima del umbral de 45: vigilarlo cuando llegue el arte final.
- **Toon** (`FUNADOS/Toon`, `BrawlFighterVisual.ToonShader`): material único de los luchadores con contorno de casco invertido (0,25 % de la pantalla). Incluido en «Always Included Shaders» por `BetaProjectBuilder`. `-toon-smoke` 9/9. Los escenarios no lo usan (prefabs de arte); hacerlo cuando se rehagan.
- Pendiente para arte: sustituir chispas y anillos por VFX dibujados. Mantener el modo reducido y que nada tape a un luchador.
- **Efectos de sonido por capas** (`BrawlSfxSynth`): `BrawlAudio.PlayHit(tier, comboStep, launch)` desde `BrawlFighter.ApplyHits`; nombres `hit-light`, `hit-heavy`, `hit-spike` (con `#n` para la escalera de combo), `dodge`, `launch`, `recovery` y `ko`; todos por el bus SFX. `-sfx-smoke` 18/18 con WAV para escuchar.
- Tras juntar `main` 0.7.1 en la rama: conflicto resuelto en la línea de opciones de `BrawlMatch` (`-audio-smoke`, `-ui-smoke`, `-fx-smoke`, `-sfx-smoke`, `-toon-smoke`). La rama se rehízo después como commits limpios sobre `origin/main` 0.7.1.

## Relevo feat/audio-buses (AUDIO-428 a 434)

- `BrawlAudio` (nuevo): buses Master, Música, SFX, Voces y UI. Una `AudioSource` por bus bajo la raíz de la partida (`Bus Música`, `Bus SFX`, …); volumen = `BusGain` × Master × bus. No se usa un asset `AudioMixer` porque sus grupos no se pueden crear desde código sin API interna; si llega audio final, basta asignar `outputAudioMixerGroup` a cada fuente.
- `BrawlFeedback.Sound` delega en `BrawlAudio.Play`: el bus sale del nombre (`ui-*` → UI, `voice-*` → Voces, el resto → SFX). El KO y el super añaden una frase provisional en Voces; una frase a la vez y el KO tiene prioridad. Pausa/reanudar suenan en UI. `BrawlFeedback.SfxVolume/MusicVolume/SetVolumes` siguen igual (son los buses SFX y Música).
- Opciones: formato **versión 4** (`master`, `voice`, `ui`); v1/v2/v3 migran. **Al juntar con `feat/ui-accesibilidad`**: si esa rama también sube el formato, usar la v5 encima de esta o fusionar ambas en la v4. Se ajustaron dos comprobaciones antiguas que fijaban la versión 3 (`BrawlDisplayChecks`) o no tenían los campos v4 (`BrawlBindingChecks`, archivo de versión futura).
- HUD (cambio mínimo en `BrawlHud.DrawOptions`): cuarta pestaña **AUDIO** (`tab-audio`, filas `audio-master/music/sfx/voice/ui`); las pestañas pasan a 76 px y el título OPCIONES se encoge si no cabe. GENERAL conserva Efectos y Música.
- `-audio-smoke` (registrado en `BrawlMatch` y `BrawlSideSmokeProbe`): 25/25, unos 35 s. Mide la salida real con `AudioListener.GetOutputData`, así que necesita un dispositivo de audio.
- Suites en la rama (`Evidence/Branch-feat-audio-buses`, build 0 errores): power 32, moves 21, kits 91, recovery 16, camera 15, coyote 5, arena 46, side-controls 127, side 375, controls 125, beta 208, audio 25; cero fallos. No se volvió a ejecutar `-arena-stress` (25 min; no toca audio). Copia del GDD marcada sin regenerar (se hace al juntar en `main`).

## Relevo feat/ui-accesibilidad (SYS-071 alto contraste y daltonismo, SYS-076 tutorial)

- Pestaña PANTALLA: botones ALTO CONTRASTE y DALTONISMO (`BrawlMatch.SetAccessibility`). Todos los colores del HUD salen de `BrawlHudPalette` (normal/alto contraste × normal/daltonismo); los colores de jugador también (`BrawlHudPalette.Player`). El HUD cambia al instante; los efectos del mundo, desde la ronda siguiente.
- `BrawlOptionsStore`: `highContrast` y `colorBlindPalette` como campos **opcionales**, sin subir la versión 3 (los archivos v1/v2/v3 cargan igual y quedan «Loaded»). Si `feat/audio-buses` sube a v4, el merge es sumar líneas; ojo con `stored.version == 3` en `BrawlDisplayChecks`.
- Prueba de daltonismo automática (protanopía/deuteranopía/tritanopía, Machado 2009, ΔE76): paleta habitual mínima 13,5; paleta de daltonismo 42,7. Contraste de texto ≥ 4,5:1 normal y ≥ 7:1 en alto contraste, también simulando daltonismo.
- Tutorial (`BrawlHud.Tutorial.cs`, clase `BrawlTutorial`) en el laboratorio, sin reloj: combo escalera y recuperación. Se abre desde el menú, el panel del laboratorio o la pausa; ocupa el sitio del panel del laboratorio mientras dura.
- Suite nueva `-ui-smoke` (31 comprobaciones; registrada en `BrawlMatch` y `BrawlSideSmokeProbe`, también al final de `-side-controls-smoke` y `-side-smoke`), capturas a 100/115/130 % y con ambas opciones.
- Build de la rama: 0 errores, 489 avisos (igual que `main` 0.7.0). Suites (`Evidence/Branch-feat-ui-accesibilidad`, resumen en `suites.txt`): ui 31, power 32, moves 21, kits 91, recovery 16, camera 15, coyote 5, arena 46, side-controls 157, side 405, controls 125, beta 208, arena-stress 25; cero fallos.
- Casillas SYS-071 y SYS-076 marcadas (sin tocar el recuento). **Con personas:** jugadores daltónicos reales y claridad de los textos del tutorial. No se regeneró `FUNADOS_GDD_2.0_checklist.docx` (se hace al juntar en `main`). Migración de framework D07 no tocada.



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
