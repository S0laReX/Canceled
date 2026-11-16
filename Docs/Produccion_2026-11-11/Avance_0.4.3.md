# Ajustes de mando e impactos simultáneos — FUNADOS 0.4.3

Fecha: 16/11/2026. Continúa la 0.4.2 (remapeo y opciones guardadas). Hay implementación y evidencia automática con mandos virtuales. Ninguna casilla se declara aceptada: falta la prueba con mandos físicos y con personas, prevista al final.

| ID | Implementación y evidencia | Falta para aceptación completa |
|---|---|---|
| SYS-026 | Pestaña **MANDO** del panel OPCIONES, en el menú y en la pausa. Zona muerta de mover y de orientar (5–50 %, pasos de 5), respuesta de la palanca (0,5 fina – 1 lineal – 2 rápida) y vibración (0–100 %, pasos de 25, vista previa al cambiarla). La palanca se lee sin procesar: zona muerta radial, reescalado a 0–1 sin saltos y curva de respuesta. La cruceta sigue siendo una alternativa digital completa. | Ajustar valores por defecto con mandos físicos (deriva real, PlayStation/Xbox) y con personas. |
| SYS-072 (parcial) | La vibración se puede apagar o reducir. Solo vibra el mando dueño del luchador: golpe recibido (proporcional al empuje), pérdida de vida y super. Se detiene en pausa, menú, resultados, desconexión y al perder el foco. | Comprobar sensación y duración en hardware real. Movimiento del fondo sigue pendiente. |
| SYS-068 | Formato de opciones **versión 2** con los cuatro ajustes de mando. Un archivo versión 1 se migra conservando controles y volúmenes; la v1 queda como `.bak`. Un archivo v2 al que le faltan campos se trata como incompleto. | Igual que en 0.4.2: comprobar en la build final tras reinicios reales. |
| SYS-045 | Los impactos se resuelven por paso de simulación. Durante el paso se acumulan los golpes cuerpo a cuerpo, de proyectiles y de zonas; la vulnerabilidad se toma del estado al **inicio** del paso. Al final se aplican todos juntos por objetivo: el daño se suma, cada empuje usa el daño resultante, los empujes horizontales se suman (con el máximo de siempre) y la elevación y el aturdimiento toman el mayor. Un intercambio simultáneo golpea a ambos, sea quien sea P1. Un solo golpe da exactamente la fórmula anterior. | Pruebas con personas en partidas de 3–5 jugadores. La atribución del KO cuando dos golpes empatan en fuerza va al número de jugador menor. |

## Resultado comprobado

Build Windows 0.4.3: Succeeded, **0 errores, 485 avisos**, 179.147.811 bytes. Prueba focal (`-controls-smoke`): **86/86**. Prueba completa (`-beta-smoke`): **169/169** (146 anteriores + 23 nuevas), sin fallos. Las 83 comprobaciones de reglas, kits, arenas y laboratorio pasan con la nueva resolución de impactos. Se revisó la captura `pad-options.png`. No hay cambios en el arte.

Durante la sesión se encontró un problema real de las pruebas: si la ventana del juego perdía el foco durante los ~4 minutos de la prueba completa, el Input System deshabilitaba los mandos virtuales y fallaba el bloque de dispositivos. Las ejecuciones automáticas ahora ignoran el foco. El juego normal mantiene el comportamiento por defecto: deshabilitar y reiniciar los dispositivos al perder el foco. El mando PlayStation físico se desconectó durante la ejecución.

Evidencia: `Evidence/Controls-0.4.3/{Focused,Full}` (informes, capturas, player.log, archivos de opciones y `tuning-checks/`), `build.txt` y `verification.json`.

## Punto para continuar

1. Prueba humana con mandos físicos de todo el bloque de controles (lista en `CONTINUAR.md`).
2. Hitboxes/hurtboxes completas y visualizador de proyectiles y zonas en el laboratorio (pendiente de 0.4.0).
3. SYS-070/071: navegación completa de menús con teclado/mando y foco visible; tamaño de texto y HUD. Hoy los menús IMGUI dependen del ratón.
