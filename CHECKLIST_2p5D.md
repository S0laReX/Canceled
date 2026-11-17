# Checklist activa de FUNADOS 2.5D

Esta es la lista para continuar el proyecto con otro agente. Estado de trabajo: **0.5.1**, sucesora de 0.5.0. Fecha de actualización: 16/11/2026. Editor: **Unity 6000.5.7f1**, proyecto `D:\Funados`.

**Dirección obligatoria confirmada por Santiago:** personajes 3D con combate lateral X/Y y Z bloqueado. Mantener golpes, poderes, combos aéreos, porcentaje y expulsiones. Los requisitos históricos de movimiento libre X/Z, rodeos en profundidad y sectores seguros inaccesibles quedan reemplazados. No cambiar esto por seguir el GDD antiguo.

`[x]` indica trabajo implementado con evidencia técnica o una decisión ya confirmada; no sustituye la aceptación humana indicada en otra casilla. `[ ]` sigue pendiente. No equiparar cantidad de pruebas con cantidad de casillas cerradas. Conservar la checklist DOCX original como historial.

## Instrucciones para el siguiente agente

1. Leer esta lista y `CONTINUAR.md`; comprobar `git status` y `git log -4 --oneline`. Conservar cambios ajenos.
2. Consultar las evidencias y verificar que corresponden a los hashes de las fuentes actuales. No repetir pruebas generales si el código no cambió y la evidencia sigue vigente.
3. Elegir la primera tarea pendiente de prioridad alta. Implementarla y comprobar sus casos de aceptación antes de ampliar contenido.
4. Guardar evidencia en una carpeta nueva, actualizar esta checklist y CONTINUAR.md, crear commit local y comunicar qué falta. No publicar un remoto sin instrucción.

## Base conservada

- [x] Confirmar diseño lateral 2.5D con Santiago (16/11/2026).
- [x] Conservar cinco kits, once aspectos originales y cuatro escenarios de producción; mantener IDs y GUID.
- [x] Mantener escenas Funados y CombatLab; preservar beta y pruebas históricas.
- [x] Versionar con Git y Git LFS; editor y dependencias fijados (SYS-005/006/007/009).
- [x] Conservar respaldo anterior a 2.5D y registrar escenarios preexistentes separadamente (`c2ad544`).
- [ ] Probar restauración desde Git/LFS en entorno limpio y verificar todas las fuentes de arte (SYS-004/006, QA-441).
- [ ] Cerrar procedencia y licencias de modelos, escenarios, audio y dependencias (SYS-017).

## Combate y poderes — prioridad alta

- [x] Movimiento X/Y, cámara lateral, profundidad bloqueada, doble salto y plataformas atravesables: evidencia 0.5.0, 56 comprobaciones.
- [x] Golpe ascendente, aéreo neutral, descendente, salto tras impacto confirmado, buffer y pausa de impacto iniciales: evidencia 0.5.0.
- [x] **P0-01 · Corregir super de Tío Antena (SYS-050, CH07), implementación técnica 0.5.1.** Refugio central hasta ±1,4 m, peligro exterior hasta alcance+0,3, límite vertical y aviso blanco coincidentes. Pruebas de izquierda/derecha, bordes del refugio, entrada caminando durante preparación, altura, fallo, interrupción, pausa y limpieza superadas. Balance con personas sigue pendiente abajo.
- [x] **P0-02 · Avisos de poderes en XY (SYS-043/073, FX-418), implementación técnica 0.5.1.** Cinco supers con alcance/altura, fuerte y trayectoria de token. Rojo=peligro, blanco=refugio; rótulo REFUGIO. Efectos reducidos, orientación, pausa, cancelación y retorno al menú comprobados. No equivale a arte final ni aceptación de todas las cámaras.
- [x] Ampliar prueba 2.5D a los cinco supers y los tres especiales que faltaban; conservar las pruebas previas de los dos pilotos. La suite lateral pasa de 56 a 88 casos; la focal contiene 32.
- [ ] **P0-03 · Impactos simultáneos imparciales (SYS-045).** Pendiente. Hoy BrawlMatch llama Fighter.Tick secuencialmente y recibir daño cancela el ataque antes del turno siguiente. Reproducir dos ataques activos simultáneos, resolver contactos sin favorecer P1 y demostrar el mismo resultado al invertir orden/puestos. Cubrir interrupciones, KO simultáneo, proyectiles y cargas sin duplicar daño.
- [ ] Separar y revisar hitboxes/hurtboxes y avisos por movimiento, también durante altura, borde y espejo (SYS-042/043/044/048).
- [ ] Llevar los valores laterales provisionales a datos editables por movimiento/kit y validar rangos; hoy los AttackSpec direccionales están en BrawlFighter (SYS-011).
- [ ] Calibrar preparación/activo/recuperación, hitstun, knockback, límites de combo y vías de escape con personas (SYS-041/046/047/049/050).
- [ ] Verificar especial, recuperación y super de cada kit desde acierto, fallo, interrupción, pausa, plataforma y borde. No aceptar un personaje por tener modelo o código.

## Movimiento, cámara y arenas

- [x] Plataforma principal, dos apoyos atravesables, S+Espacio para bajar y KO superior a 20 m en cuatro escenarios: prueba automática 0.5.0.
- [x] Cruceta vertical selecciona golpe sin reducir la velocidad horizontal; regresión 0.5.0.
- [ ] Medir recuperación desde ambos bordes con los cinco kits, especialmente el de menor alcance (SYS-030/031/032/033/035, ARENA-357).
- [ ] Probar cámara y avisos fuera de pantalla con cinco luchadores separados en X/Y, relaciones de aspecto distintas y alturas extremas (SYS-036–040).
- [ ] Diferenciar distribuciones jugables: actualmente los cuatro escenarios comparten base lateral y dos plataformas a 2,6 m (ARENA-350–367).
- [ ] Revisar límites izquierda/derecha/arriba/abajo y spawns; retirar criterios de KO frontal/trasero del contrato nuevo (ARENA-358/359).
- [ ] Ejecutar cien KO/reapariciones reales y veinte partidas por escenario. Los cien retornos sintéticos de entrenamiento no equivalen a esto (ARENA-366).

## Controles, opciones e interfaz

- [x] Asignación estable de mandos, teclado+mando, dos mandos, 2–5 jugadores y reconexión: implementación y prueba virtual 0.4.1/0.5.0 (SYS-022/023/027).
- [ ] Aceptar lo anterior con mandos físicos USB/Bluetooth, dos bajas y mando reemplazado, cuenta atrás y partida. Conservar stocks/porcentaje/reloj (QA-445).
- [ ] **P1-01 · Remapeo y detección de conflictos (SYS-025).** Una fuente de bindings para lectura y etiquetas; cancelar reasignación, restaurar valores, separar dispositivos y probar ambos jugadores.
- [ ] **P1-02 · Opciones recuperables (SYS-068/069).** Formato versionado, escritura segura, archivo truncado/dañado y retorno a valores seguros; incluir modo de entrada y bindings.
- [ ] Sensibilidad, deadzones editables y vibración desactivable (SYS-026/072).
- [ ] Navegación completa de menús con teclado y mando, foco visible, texto/HUD escalables y tutorial de combo/recuperación sin tiempo límite (SYS-070/071/076).
- [ ] Unificar instrucciones laterales y avisos de peligro/refugio en tutorial y controles. Evitar que el usuario necesite leer código o depuración.

## Arte, audio y contenido

- [x] Reutilizar los personajes y escenarios habituales; sin sustituciones de arte en la conversión.
- [ ] Sustituir el rig automático provisional por un rig revisado, poses de ataque claras y clips sin deformaciones graves. Revisar frente/perfil/hitstun, caída y KO (ART/ANIM, CIERRE-455).
- [ ] Revisar lectura de efectos, neón, contraste y accesorios en cámara lateral. Las líneas de depuración del laboratorio no son el arte final.
- [ ] Completar mezcla Master/Música/SFX/Voces/UI, sonidos finales, subtítulos y créditos/licencias (AUDIO-428–434).
- [ ] Aceptar individualmente los cinco kits existentes antes de producir los ocho restantes. Faltan Vera Cláusula, Gloria Bis, El Moderador, Gala Causa, Don Monopolio, Rolo Directo, Don Íñigo del Ayer y Null.
- [ ] Diseñar y aceptar las cuatro arenas restantes del catálogo; revisar nombres/IDs frente al catálogo de escenarios de arte ya integrado (CIERRE-460/461).

## Calidad y cierre

- [ ] Medir rendimiento en build: CPU/GPU, percentiles, memoria, cinco participantes y efectos máximos. No declarar 60 FPS por una captura (QA-435–440).
- [ ] Sesión de treinta minutos con revanchas, opciones, pausa y reconexión sin bloqueos ni crecimiento sostenido de entidades.
- [ ] Aceptar Don Promesio y Lola Viral, una arena lateral, partida con dos dispositivos, resultados/revancha y accesibilidad: **CIERRE-455–458 siguen abiertos**.
- [x] Conservar regresión histórica de cinco kits/cuatro arenas: 109 comprobaciones de 0.5.0 (CIERRE-459 técnico).
- [x] Verificación técnica de 0.5.1: 32 pruebas focales de poderes, 88 de la suite 2.5D completa y 109 de regresión histórica; cero fallos. Capturas inspeccionadas y hashes registrados en `Evidence/Production-0.5.1/verification.json`. Las 32 focales son un subconjunto de las 88, no casos únicos adicionales.
- [ ] Juego local completo de trece personajes/ocho arenas, publicación, distribución y soporte: pendientes (CIERRE-460–467).
- [ ] Online/ranking/equipos/plataformas móviles: investigación posterior con decisión propia; no son requisito de esta corrección.

## Rutas y comandos para continuar

- Escenas: `Assets/FunadosProduction/Scenes/Funados.unity` y `CombatLab.unity`.
- Fuentes de esta tarea: `BrawlFighter.cs`, `BrawlSideTelegraph.cs`, `BrawlHazards.cs`, `BrawlSidePowerChecks.cs` y `BrawlSideSmokeProbe.cs`, en `Assets/CancelBrawlBeta/Runtime`.
- Generar: menú Unity **FUNADOS → Generar Windows**. La carpeta de build es `Builds/Funados/<versión>/Windows/FUNADOS.exe`.
- Evidencia nueva: `Evidence/Production-0.5.1`. Mantener carpetas nuevas para cada repetición.
- Prueba focal: `-power-smoke -evidence-path D:/Funados/Evidence/Production-0.5.1/Focused`.
- Prueba completa 2.5D: `-side-smoke -evidence-path D:/Funados/Evidence/Production-0.5.1/Side`.
- Regresión histórica: `-beta-smoke -evidence-path D:/Funados/Evidence/Production-0.5.1/Legacy`. Fuerza reglas 3D únicamente para comparación; no es el diseño actual.
- Añadir a esas ejecuciones `-force-d3d11 -screen-width 1280 -screen-height 720 -screen-fullscreen 0 -logFile <carpeta>/player.log`. Conservar los archivos junto al ejecutable.
- Desconectar mandos físicos para las suites que crean mandos virtuales. Nunca alterar los dispositivos reales para que una prueba pase.

## Registro de esta sesión

- Base: commit `8e457e8`, versión 0.5.0; árbol de trabajo limpio al empezar.
- Cerrados técnicamente P0-01/P0-02 y ampliación de pruebas de poderes. Build final 0.5.1: cero errores/cero advertencias; pruebas focal 32/32, lateral 88/88, histórica 109/109. Captura final de REFUGIO legible sin solaparse con P1. Recursos originales 22/22 sin cambios y Git LFS fsck OK. Sin mandos físicos ni aceptación humana de balance.
- Resuelto el registro de BrawlSideTelegraph en Unity mediante reimportación/MoveAsset y restauración de su nombre, conservando GUID. No hay bloqueo de compilación pendiente. La reconstrucción limpia inicial dejó 485 advertencias de shaders de paquetes; las builds incrementales posteriores dejaron cero. Detalle en `Docs/Produccion_2026-11-16/Poderes_0.5.1.md`.
- Evidencia final: carpetas Focused, Side y Legacy; Initial y LabelFirst conservan iteraciones previas. `verification.json` vincula fuentes, ensamblado, ejecutable e informes mediante SHA256.
- **Punto exacto de relevo: P0-03 / SYS-045, impactos simultáneos imparciales.** Empezar en `BrawlMatch.FixedUpdate` y `BrawlFighter.ReceiveHit`. Unity queda en la escena Funados, detenida. Si una tarea posterior queda incompleta, describir el bloqueo exacto y no marcarla.
