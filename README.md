# FUNADOS

Juego local de lucha de plataformas 3D: movimiento X/Z, salto Y, porcentaje de daño y expulsiones. Proyecto de producción iniciado sobre Cancel Brawl 0.3, conservando sus cinco kits, cuatro arenas y once aspectos originales. Entrega actual: 0.4.1, asignación estable y recuperación de mandos; incluye el laboratorio de 0.4.0.

Abre `D:\Funados` desde Unity Hub con **Unity 6000.5.7f1**. Las versiones de paquetes están fijadas en `Packages/manifest.json` y `packages-lock.json`; no aceptes una actualización de Editor para reproducir esta entrega.

Para continuar con otro agente, empezar por `CONTINUAR.md`: contiene los puntos de checklist, evidencia, archivos relevantes y siguiente implementación.

## Escenas y ejecución

- `Assets/FunadosProduction/Scenes/Funados.unity`: selección, versus local y acceso a entrenamiento. Pulsa Play y luego Enter para jugar. En el menú se pueden elegir los cinco kits, sus aspectos y las cuatro arenas.
- `Assets/FunadosProduction/Scenes/CombatLab.unity`: pulsa Play para entrar directamente al entrenamiento con Don Promesio y Lola Viral en Azotea Neón.
- `Assets/CancelBrawlBeta/Scenes/CancelBrawlBeta.unity`: escena beta conservada con sus nombres internos y reglas históricas. Su documentación sigue en `README_BETA.md`.

Menú del editor: **FUNADOS → Abrir juego local**, **Abrir CombatLab** y **Generar Windows**. La generación de escenas crea únicamente las que faltan; no sobrescribe escenas existentes. Guarda primero cualquier escena sin título modificada.

El ejecutable nuevo se genera en `Builds/Funados/0.4.1/Windows/FUNADOS.exe`. Conserva toda la carpeta Windows junto al ejecutable. Las compilaciones anteriores se mantienen aparte.

## Mandos locales

Conecta los mandos antes de iniciar. Activa **Mandos locales** y selecciona **P1: teclado y ratón** para teclado+mando, o **P1: mando** para dos mandos. Este último modo exige al menos dos mandos disponibles para versus y uno para entrenamiento. Los puestos adicionales sin mando son bots; el objetivo de entrenamiento permanece sin entradas. La cruceta también permite moverse, con diagonales normalizadas.

Cada mando queda reservado a un luchador durante la ronda. Si se pierde uno, la partida o cuenta atrás se pausa y muestra el puesto afectado. Reconectar el mismo dispositivo recupera su puesto; después hay que confirmar con **Continuar**, Esc o Start. Si el sistema reconoce el mando como uno nuevo, pulsa Start en él para reclamar el primer puesto perdido, o usa **Asignar mandos disponibles** en pausa. Repite para cada baja. La partida solo continúa cuando todos los puestos recuperan su mando. Los mandos sobrantes no controlan ni pausan la partida.

Los nuevos mandos no sustituyen bots a mitad de ronda. Regresa al menú para cambiar participantes. En modo solo, P1 conserva teclado/ratón y puede usar el primer mando que estaba conectado al comenzar; desconectarlo también pausa y permite recuperarlo o regresar al menú. La selección del modo de entrada todavía no se guarda entre ejecuciones.

## Entrenamiento

Usa los controles habituales: WASD para moverse, ratón para orientar, Espacio para saltar dos veces, clic izquierdo/derecho para básico/fuerte, E especial, R recuperación aérea, Shift esquiva, Q super, Ctrl caída acelerada y Esc pausa.

El objetivo no emite entradas; recibe daño y knockback normal. El laboratorio no agota tiempo ni stocks. Las caídas devuelven al luchador al punto de práctica. **−25/+25** modifica el daño inicial del objetivo entre 0 y 300; **Reiniciar** reconstruye la sesión, elimina proyectiles/zonas y restablece ese porcentaje. **Super listo** llena la barra de P1.

**Volúmenes** muestra el alcance horizontal, cono y tolerancia vertical del cuerpo a cuerpo usando los mismos datos del golpe: amarillo durante preparación/final y rojo durante la ventana activa. La cruz marca la raíz que el barrido consulta. La geometría sólida sigue bloqueando impactos. Los proyectiles, zonas, onda y sectores seguros conservan sus avisos existentes; sus volúmenes de colisión completos aún no tienen un visualizador adicional.

El entrenamiento respeta la arena, personajes y aspectos seleccionados en el menú. Volver al menú permite iniciar versus con el número de rivales elegido anteriormente. El modo no cambia el balance compartido ni los controles de la beta.

## Recursos y nombres

Se reutilizan los once modelos/texturas existentes de `Assets/CancelBrawlBeta/Art/Characters` mediante `Data/CharacterArt.asset`. No se generaron sustitutos. Don Decreto → Don Promesio y Lola Loop → Lola Viral son cambios de presentación en las escenas de producción; IDs, carpetas, GUID y kits permanecen estables. El rig automático sigue siendo temporal.

## Compilar y verificar

Genera Windows desde el menú FUNADOS. Para repetir las pruebas en una carpeta de evidencia nueva:

```powershell
$proof = 'D:\Funados\Evidence\MiPrueba'
New-Item -ItemType Directory -Force $proof | Out-Null
$game = Start-Process -FilePath 'D:\Funados\Builds\Funados\0.4.1\Windows\FUNADOS.exe' -WorkingDirectory 'D:\Funados' -ArgumentList "-beta-smoke -evidence-path $proof -force-d3d11 -screen-width 1280 -screen-height 720 -screen-fullscreen 0 -logFile $proof\player.log" -PassThru -Wait
$game.ExitCode
```

La prueba recorre los cinco kits, las cuatro arenas, reglas de partida, laboratorio y dispositivos; dura unos tres minutos y se cierra al terminar. Usa una ruta sin espacios en este comando o entrecomilla cada ruta en ArgumentList. Las capturas y `smoke-report.json` se guardan en esa carpeta. Desconecta mandos físicos antes de esta prueba: el último bloque crea dispositivos virtuales y se niega a manipular los reales. Para ejecutar solo ese bloque, cambia `-beta-smoke` por `-controls-smoke`. El informe identifica ahora la versión de presentación de la escena. Mandos físicos, pruebas con personas y aceptación del arte siguen pendientes.

## Git y restauración

Instala Git LFS antes de clonar/restaurar. Modelos, texturas, audio, Blender y ZIP usan LFS; sus `.meta` son texto versionado. Después de obtener el repositorio, ejecuta `git lfs pull` y abre el proyecto con la versión indicada. Unity regenera Library y Temp. No se versionan ejecutables ni cachés.

El primer commit conserva la base previa al desarrollo. El commit siguiente contiene el laboratorio y las escenas nuevas. La checklist original está en `Docs/Produccion_2026-11-11`; el registro de avance distingue implementación, evidencia y aceptación pendiente. El repositorio sigue siendo local; no se publica un remoto en esta entrega.
