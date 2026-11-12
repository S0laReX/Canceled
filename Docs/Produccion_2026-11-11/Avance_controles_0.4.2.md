# Controles remapeables y opciones guardadas — FUNADOS 0.4.2

Fecha: 12/11/2026. Continúa el bloque Dispositivos y control a partir de 0.4.1 (`b49b358`). Hay implementación y evidencia automática. Ninguna casilla de la checklist se declara aceptada: faltan mandos físicos y pruebas con personas.

| ID | Implementación y evidencia | Falta para aceptación completa |
|---|---|---|
| SYS-024 | Acciones de movimiento X/Z, salto, básico, fuerte, especial, recuperación, esquiva, super y caída rápida en una única tabla `BrawlBindings`. Pausa fija en Esc/Start. | Mapear orientación por botón si el diseño lo pide; revisar con personas. |
| SYS-025 | El panel CONTROLES del menú y la pausa sale de la misma tabla que lee el juego. Muestra el nombre que da la distribución real del teclado y los botones reales del mando (Xbox: A/B/X/Y; PlayStation: Cruz/Círculo…; los genéricos usan nombres Xbox). Con un clic en una celda se captura la siguiente tecla, botón del ratón o botón del mando. Si el control ya pertenecía a otra acción, avisa en rosa e intercambia ambas asignaciones, así que nunca quedan duplicados ni acciones vacías. Esc y Start están reservados y cancelan la captura. Hay botones para restaurar teclado y mando. | Probar con mandos físicos de cada familia y distribuciones AZERTY/QWERTZ; legibilidad con personas. Una sola tecla por acción. |
| SYS-068 | Archivo `funados-options.json` en `Application.persistentDataPath` con `format` y `version` = 1. Guarda volúmenes, efectos reducidos, modo de entrada (mandos locales / P1 con mando) y controles de teclado y mando. La escritura pasa por un `.tmp` que se verifica antes de sustituir el archivo; el archivo válido anterior queda como `.bak`. Las opciones de 0.4.1 (PlayerPrefs) se importan una vez. | Comprobar el guardado en la build final tras reinicios reales de la máquina y cortes de energía. |
| SYS-069 | Un archivo vacío, truncado, incompleto, de otro formato o ilegible se aparta como `*.danado-<fecha>`. Se recupera la copia `.bak` o, sin copia, se cargan valores seguros. Los valores fuera de rango se corrigen. Una asignación duplicada, reservada o desconocida restaura los valores por defecto de ese dispositivo. Un `.tmp` huérfano se ignora. El panel OPCIONES avisa de la recuperación. | Revisión del mensaje con personas. |
| QA-445 | Además de las 26 pruebas de 0.4.1, se probaron con dispositivos virtuales el remapeo, los conflictos, las teclas reservadas y la captura. | Matriz con mandos físicos. |

## Resultado comprobado

Build Windows 0.4.2: Succeeded, **0 errores, 485 avisos**, 179.128.323 bytes. Son los mismos avisos que en 0.4.1. La primera generación en este worktree registró 4 errores de licencia del batchmode (token no disponible); las siguientes dieron 0 errores. Durante el desarrollo se corrigió un aviso nuevo (`Key.IMESelected` obsoleto).

Prueba focal (`-controls-smoke`): **63/63**, cero fallos. Prueba completa (`-beta-smoke`): **146/146**, cero fallos (109 anteriores + 37 nuevas). Ambas se ejecutaron sobre la build final con `-options-path` dentro de la carpeta de evidencia, de modo que no tocaron las opciones del jugador. Se revisó a 1280×720 la captura `controls-remap.png`, que muestra el aviso de conflicto. No hay cambios en `Assets/CancelBrawlBeta/Art`.

Evidencia: `Evidence/Controls-0.4.2/{Focused,Full}` con informe, capturas, player.log y los archivos de opciones generados por las pruebas (`options-checks/`). También `build.txt` y `verification.json` con SHA256 de fuentes y ensamblado.

## Decisiones

- Una sola tabla de bindings. La lectura del gameplay, las etiquetas del HUD, la barra de ayuda inferior, el remapeo y el guardado la usan; no existe una segunda lista de teclas.
- El conflicto se resuelve intercambiando, no rechazando, para que el jugador nunca deje una acción sin control. El aviso dice qué acción pasó a qué control.
- En el mando, el movimiento (palanca izquierda y cruceta) y la orientación (palanca derecha) son fijos; el ratón orienta en teclado. Se reservan Esc, Start y Select para que un remapeo nunca bloquee la pausa.
- Un perfil compartido por todos los mandos. Antes Shift y Ctrl funcionaban por los dos lados; ahora solo el izquierdo, que se puede remapear al derecho.
- Las pruebas automáticas solo escriben opciones si reciben `-options-path`; sin él no escriben el archivo del jugador.

## Punto para continuar

Validación física y humana de SYS-022/023/025/027. Después, SYS-026 (deadzones, sensibilidad y vibración editables, guardadas en el mismo archivo con `version` 2 y migración desde 1). CIERRE-455 a 458 siguen abiertos.
