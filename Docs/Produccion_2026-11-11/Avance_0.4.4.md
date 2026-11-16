# Menús navegables y volúmenes del laboratorio — FUNADOS 0.4.4

Fecha: 16/11/2026. Continúa la 0.4.3. Hay implementación y evidencia automática con dispositivos virtuales. Ninguna casilla se declara aceptada; la prueba con mandos físicos y personas sigue prevista al final.

| ID | Implementación y evidencia | Falta para aceptación completa |
|---|---|---|
| SYS-070 | Todos los controles del menú, la pausa y los resultados se registran con un id al dibujarse: luchadores, canchas, número de rivales, rivales, modo de entrada, ENTRAR, entrenamiento, todas las celdas de CONTROLES, restaurar, pestañas y filas de OPCIONES. Las flechas, las teclas de movimiento asignadas, la cruceta y la palanca mueven el foco al control más cercano en esa dirección; la palanca repite al mantenerla. Enter, Espacio o A activan. Izquierda/derecha ajustan los controles con valor (volúmenes, ajustes de mando, aspecto de cada rival). B reanuda desde la pausa. El foco es un **borde blanco** y el ratón lo mueve también. Cada pantalla abre en su acción principal (ENTRAR, CONTINUAR o ASIGNAR MANDOS, REVANCHA), así que Enter sigue iniciando la partida. En la pausa solo navegan los mandos de la ronda; los sobrantes no. Start sigue siendo atajo para jugar desde el menú y los resultados. | Recorrido completo con personas y mando físico; revisar que el orden espacial resulte natural en todas las pantallas. |
| SYS-065 (ampliado) | El laboratorio dibuja también proyectiles (esfera de impacto alrededor del proyectil), zonas de super (cilindro o esfera), el emisor de Tío Antena (ventana cilíndrica) y la onda de Coach Tóxico. Amarillo = aún no golpea, rojo = activo. El objetivo muestra una cruz en la raíz (cuerpo a cuerpo, emisor) y otra en el pecho (proyectiles y burbuja). Las constantes de impacto pasan a ser públicas y las comparten el juego y el visualizador: `BrawlProjectile.HitDistance/ChestHeight`, `BrawlZone.Tolerance/BelowTolerance`, `BrawlTrap.BelowReach/AboveReach` y la tolerancia de la onda. La opción VOLÚMENES oculta todo. | Revisión con personas; no sustituye hurtboxes anatómicas, que siguen pendientes. |

## Resultado comprobado

Build Windows 0.4.4: Succeeded, **0 errores, 485 avisos**, 179.168.659 bytes. Prueba focal: **108/108**. Prueba completa: **191/191** (169 anteriores + 22 nuevas), sin fallos, sobre la build final. Una prueba usa un proyectil inmóvil a `HitDistance + 0,1` del pecho, que no golpea, y otro a `HitDistance − 0,1`, que sí: demuestra que el volumen dibujado es el real. Se revisaron `menu-focus.png` (foco sobre una cancha) y `lab-hazards.png` (zona y emisor dibujados). No hay cambios en el arte.

Evidencia: `Evidence/Controls-0.4.4/{Focused,Full}`, `build.txt` y `verification.json`.

## Punto para continuar

1. Prueba humana con mandos físicos de todo el bloque de controles y menús (lista en `CONTINUAR.md`).
2. SYS-071: tamaño de texto y HUD ajustables, legibilidad a 720p y en otras relaciones de aspecto.
3. Hurtboxes anatómicas, cámara con personas, arte final y rendimiento medido (CIERRE-455 a 458).
