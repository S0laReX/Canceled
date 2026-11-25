# FUNADOS

Juego local de lucha de plataformas **2.5D**: personajes 3D, movimiento lateral X, salto Y y profundidad bloqueada. Por instrucción del 16/11/2026, la referencia es el combo aéreo del video “The PERFECT Mario Ladder Combo”; la producción abandona el movimiento libre X/Z. Se conservan los cinco kits, cuatro escenarios de producción y once aspectos originales. Entrega actual: **0.5.1**, golpes direccionales, combos aéreos, poderes con avisos de alcance/altura y refugio central accesible frente al súper de Tío Antena.

Abre `D:\Funados` desde Unity Hub con **Unity 6000.5.7f1**. Las versiones de paquetes están fijadas en `Packages/manifest.json` y `packages-lock.json`; no aceptes una actualización de Editor para reproducir esta entrega.

Para continuar con otro agente, empezar por `CHECKLIST_2p5D.md` y `CONTINUAR.md`: contienen prioridades, criterios de aceptación, evidencia, archivos relevantes y siguiente implementación.

## Escenas y ejecución

- `Assets/FunadosProduction/Scenes/Funados.unity`: selección, versus local y acceso a entrenamiento. Pulsa Play y luego Enter para jugar. En el menú se pueden elegir los cinco kits, sus aspectos y las cuatro arenas.
- `Assets/FunadosProduction/Scenes/CombatLab.unity`: pulsa Play para entrar directamente al entrenamiento con Don Promesio y Lola Viral en Azotea Neón.
- `Assets/CancelBrawlBeta/Scenes/CancelBrawlBeta.unity`: escena beta conservada con sus nombres internos y reglas históricas. Su documentación sigue en `README_BETA.md`.

Menú del editor: **FUNADOS → Abrir juego local**, **Abrir CombatLab** y **Generar Windows**. La generación de escenas crea únicamente las que faltan; no sobrescribe escenas existentes. Guarda primero cualquier escena sin título modificada.

El ejecutable nuevo se genera en `Builds/Funados/0.5.1/Windows/FUNADOS.exe`. Conserva toda la carpeta Windows junto al ejecutable. Las compilaciones anteriores se mantienen aparte. El cambio de diseño y sus límites están en `Docs/Produccion_2026-11-16/Combate_2p5D.md`.

## Mandos locales

Conecta los mandos antes de iniciar. Activa **Mandos locales** y selecciona **P1: teclado y ratón** para teclado+mando, o **P1: mando** para dos mandos. Este último modo exige al menos dos mandos disponibles para versus y uno para entrenamiento. Los puestos adicionales sin mando son bots; el objetivo de entrenamiento permanece sin entradas. La cruceta también permite moverse, con diagonales normalizadas.

Cada mando queda reservado a un luchador durante la ronda. Si se pierde uno, la partida o cuenta atrás se pausa y muestra el puesto afectado. Reconectar el mismo dispositivo recupera su puesto; después hay que confirmar con **Continuar**, Esc o Start. Si el sistema reconoce el mando como uno nuevo, pulsa Start en él para reclamar el primer puesto perdido, o usa **Asignar mandos disponibles** en pausa. Repite para cada baja. La partida solo continúa cuando todos los puestos recuperan su mando. Los mandos sobrantes no controlan ni pausan la partida.

Los nuevos mandos no sustituyen bots a mitad de ronda. Regresa al menú para cambiar participantes. En modo solo, P1 conserva teclado/ratón y puede usar el primer mando que estaba conectado al comenzar; desconectarlo también pausa y permite recuperarlo o regresar al menú. La selección del modo de entrada todavía no se guarda entre ejecuciones.

## Entrenamiento

Usa **A/D** para moverte lateralmente, ratón para orientar izquierda/derecha, **W + clic** para un golpe ascendente y **S + clic en el aire** para un remate descendente. Espacio permite saltar dos veces, clic izquierdo/derecho ejecuta básico/fuerte, E especial, R recuperación aérea, Shift esquiva, Q super, S o Ctrl caída acelerada y Esc pausa. **S+Espacio** permite bajar de las plataformas elevadas. En mando, palanca/cruceta izquierda elige movimiento y dirección del ataque; también se puede orientar con la palanca derecha.

Para practicar un combo: acércate, mantén W y golpea con clic izquierdo; después del impacto pulsa Espacio y vuelve a golpear con W en el aire. El contador muestra impactos consecutivos sobre el mismo rival separados por menos de 1,1 segundos. El salto cancela un básico confirmado, mientras un golpe fallado conserva su recuperación. Hay una pequeña pausa de impacto y buffer de entrada. El remate descendente cambia el lanzamiento vertical hacia abajo. Los valores son provisionales y no reproducen cifras internas del juego de referencia.

El objetivo no emite entradas; recibe daño y knockback normal. El laboratorio no agota tiempo ni stocks. Las caídas devuelven al luchador al punto de práctica. **−25/+25** modifica el daño inicial del objetivo entre 0 y 300; **Reiniciar** reconstruye la sesión, elimina proyectiles/zonas y restablece ese porcentaje. **Super listo** llena la barra de P1.

**Volúmenes** muestra el alcance horizontal, cono y tolerancia vertical del cuerpo a cuerpo usando los mismos datos del golpe: amarillo durante preparación/final y rojo durante la ventana activa. La cruz marca la raíz que el barrido consulta. La geometría sólida sigue bloqueando impactos. Los proyectiles, zonas, onda y sectores seguros conservan sus avisos existentes; sus volúmenes de colisión completos aún no tienen un visualizador adicional.

El entrenamiento respeta la arena, personajes y aspectos seleccionados en el menú. Volver al menú permite iniciar versus con el número de rivales elegido anteriormente. El modo no cambia el balance compartido ni los controles de la beta.

## Recursos y nombres

Se reutilizan los once modelos/texturas existentes de `Assets/CancelBrawlBeta/Art/Characters` mediante `Data/CharacterArt.asset`. No se generaron sustitutos. Don Decreto → Don Promesio y Lola Loop → Lola Viral son cambios de presentación en las escenas de producción; IDs, carpetas, GUID y kits permanecen estables. El rig automático sigue siendo temporal.

**Créditos y licencias:** los modelos 3D de personajes y escenarios y el arte conceptual se crearon con Meshy (Model created with Meshy – CC BY 4.0 License). Los generó el autor con imágenes de referencia propias. La lista completa está en `Docs/Licencias/CREDITOS.md` y el inventario de procedencia en `Docs/Licencias/Procedencia_y_licencias.md`.

## Compilar y verificar

Genera Windows desde el menú FUNADOS. Para repetir las pruebas en una carpeta de evidencia nueva:

```powershell
$proof = 'D:\Funados\Evidence\MiPrueba'
New-Item -ItemType Directory -Force $proof | Out-Null
$game = Start-Process -FilePath 'D:\Funados\Builds\Funados\0.5.1\Windows\FUNADOS.exe' -WorkingDirectory 'D:\Funados' -ArgumentList "-side-smoke -evidence-path $proof -force-d3d11 -screen-width 1280 -screen-height 720 -screen-fullscreen 0 -logFile $proof\player.log" -PassThru -Wait
$game.ExitCode
```

`-side-smoke` prueba las reglas nuevas: combo escalera, remate, los cinco supers, especiales, avisos XY, refugio de Tío Antena, plataformas, cuatro escenarios, bots y dispositivos. Genera `side-report.json` y capturas. `-power-smoke` ejecuta solo las 32 comprobaciones de poderes y avisos. `-beta-smoke` activa exclusivamente las reglas históricas 3D para regresión y genera `smoke-report.json`; no representa el diseño actual de producción. `-controls-smoke` ejecuta solo dispositivos bajo esas reglas históricas. Usa una carpeta de evidencia nueva y sin espacios o entrecomilla las rutas. Desconecta mandos físicos antes de las pruebas: se crean dispositivos virtuales y el bloque se niega a manipular los reales. Mandos físicos, pruebas con personas y aceptación del arte siguen pendientes.

## Git y restauración

Instala Git LFS antes de clonar/restaurar. Modelos, texturas, audio, Blender y ZIP usan LFS; sus `.meta` son texto versionado. Después de obtener el repositorio, ejecuta `git lfs pull` y abre el proyecto con la versión indicada. Unity regenera Library y Temp. No se versionan ejecutables ni cachés.

El primer commit conserva la base previa al desarrollo. El commit siguiente contiene el laboratorio y las escenas nuevas. La checklist original está en `Docs/Produccion_2026-11-11`; el registro de avance distingue implementación, evidencia y aceptación pendiente. El repositorio sigue siendo local; no se publica un remoto en esta entrega.
