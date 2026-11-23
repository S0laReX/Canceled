# Trabajo en paralelo (ramas de FUNADOS)

Remoto: `origin` = `https://github.com/S0laReX/Canceled.git`. Rama integradora: **`main`** (0.6.9 al abrir las ramas; checklist 36/63).

## Estado

Las cinco ramas están juntadas en `main`: `feat/arena-ko-stress` en 0.7.0 y las otras cuatro en 0.7.1 (checklist 43/63). Para una tanda nueva, abrir ramas desde `main` 0.7.1 con las mismas reglas.

## Ramas de la primera tanda (juntadas)

Cada rama sale de `main` 0.6.9 y cubre tareas de `CHECKLIST_2p5D.md` que no se pisan entre sí. Un agente por rama.

| Rama | Tareas del GDD 2.0 §15 | Archivos principales (dueño) |
|---|---|---|
| `feat/arena-ko-stress` | Cien KO con reaparición reales y veinte partidas por arena (ARENA-366) | `BrawlArenaChecks.cs` o una suite nueva (`-arena-stress`), `BrawlSideSmokeProbe.cs` (solo añadir su opción) |
| `feat/audio-buses` | Buses Master, Música, SFX, Voces y UI (AUDIO-428 a 434) | Mezclador y código de audio nuevos; opciones de volumen en `BrawlOptionsStore` (solo campos nuevos) |
| `feat/ui-accesibilidad` | Texto y HUD escalables, alto contraste y prueba de daltonismo (SYS-071); tutorial de combo y recuperación (SYS-076) | `BrawlHud*.cs`, `BrawlHudLayout.cs` |
| `chore/restauracion-licencias` | Restaurar desde Git/LFS en un entorno limpio (SYS-004/006, QA-441); procedencia y licencias (SYS-017, D04) | Solo documentación y scripts en `Docs/` y `Tools/` (sin código del juego) |
| `perf/rendimiento` | Medir rendimiento en build con cinco luchadores (QA-435 a 440); sesión de 30 minutos con revanchas | Suite nueva de rendimiento (`-perf-smoke`), `BrawlSideSmokeProbe.cs` (solo añadir su opción) |

Fuera de las ramas (necesitan personas o una decisión de Santiago): calibración con personas, mandos físicos, aceptación de kits, framework de interfaz (D07), rig (D01), arte y animación.

## Reglas para no chocar

1. Empezar con `git fetch origin && git switch <rama> && git pull`. Trabajar solo en los archivos de la tabla; si hace falta tocar otro, cambio mínimo y explicarlo en el PR.
2. **No subir la versión** (`BrawlMatch.ProductionVersion`, `ProjectSettings` `bundleVersion`) en la rama: la versión se asigna al juntar en `main`.
3. En `CHECKLIST_2p5D.md` marcar solo las casillas propias, con su nota de evidencia; **no** editar la línea de «Recuento»: se recalcula en `main`. En `CONTINUAR.md` añadir un apartado «Relevo <rama>» en lugar de numerar versiones.
4. Evidencia en `Evidence/Branch-<rama>/` (no en `Production-*`). Las suites existentes deben seguir pasando antes de abrir el PR.
5. No hacer push a `main` directamente ni forzar el push (`--force`). Commits pequeños con `Co-Authored-By` cuando corresponda.
6. Al terminar: rebase sobre `origin/main` (o merge de `main` en la rama), volver a ejecutar las suites y abrir un PR a `main`.

## Juntar en `main`

Orden sugerido: `chore/restauracion-licencias` → `feat/arena-ko-stress` → `perf/rendimiento` → `feat/audio-buses` → `feat/ui-accesibilidad`. Tras cada merge: subir la versión (0.7.x), recalcular el recuento de la checklist, regenerar `FUNADOS_GDD_2.0_checklist.docx` con `python -I Docs/Produccion_2026-11-22/marcar_checklist_gdd.py` y volver a ejecutar las 11 suites sobre la build.
