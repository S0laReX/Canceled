# Tamaño de texto, HUD y proporciones de pantalla — FUNADOS 0.4.5

Fecha: 16/11/2026. Continúa la 0.4.4. Hay implementación y evidencia automática. Ninguna casilla se declara aceptada; falta la revisión con personas y con pantallas reales.

| ID | Implementación y evidencia | Falta para aceptación completa |
|---|---|---|
| SYS-071 | Nuevo cálculo único `BrawlHudLayout`, compartido por el HUD, la cámara y la detección de clics del laboratorio. El lienzo de diseño 1600×900 ya no deja bandas: en pantallas más anchas añade espacio lateral y en más altas añade altura. El título, el reloj y el panel del laboratorio quedan arriba; las tarjetas de jugador y la ayuda, abajo; menú, pausa y resultados, centrados. La cámara reserva **exactamente** la franja de las tarjetas, que se pinta de borde a borde. Pestaña **PANTALLA** en OPCIONES: tamaño de texto y tamaño del HUD (100, 115 y 130 %). Las tarjetas y la ayuda crecen solo mientras quepan (con cinco jugadores en 16:9 ya ocupan todo el ancho). Los estilos pequeños nunca bajan de un tamaño mínimo en píxeles reales según la resolución. Cada etiqueta y botón ajusta su fuente para caber en su rectángulo, sin bajar del 70 %: el texto grande crece donde hay sitio y nunca se corta ni invade otro elemento. | Probar con personas a distancia de sofá, en monitores reales 4:3/16:10/21:9 y a pantalla completa. |
| SYS-068 | Formato de opciones **versión 3** con `textScale` y `hudScale`. Un archivo v2 se migra conservando los ajustes de mando; la v2 queda como `.bak`. | Igual que en entregas anteriores. |

## Resultado comprobado

Build Windows 0.4.5: Succeeded, **0 errores, 485 avisos**, 179.181.811 bytes. Prueba focal: **123/123**. Prueba completa: **206/206** (191 anteriores + 15 nuevas), sin fallos, sobre la build final.

Las comprobaciones de diseño recorren 5 tamaños (1600×900, 1280×720, 960×720, 1280×800, 2560×1080), de 1 a 5 jugadores y 3 tamaños de HUD. Las tarjetas y la ayuda siempre caben. El texto más pequeño queda en **12 px reales o más en 16:9 y 21:9 desde 720p**, y en **10,5 px o más** en 4:3 y 16:10 pequeños. Límite honesto: a 960×720 queda en unos 11 px, porque crecer más rompería los paneles densos. La prueba cambia de verdad la ventana a 960×720, 1280×800 y 1280×548, y captura menú, partida con cinco jugadores y pausa con texto y HUD al 130 %. En cada tamaño comprueba el texto mínimo, la franja de la cámara y la navegación del menú. Se revisaron las capturas. La primera versión cortaba nombres, partía pestañas y montaba etiquetas con el texto grande; eso se corrigió con el ajuste por rectángulo antes de la evidencia final. También se comprobó que los clics sobre el panel del laboratorio se reconocen con el HUD agrandado.

Evidencia: `Evidence/Controls-0.4.5/{Focused,Full}`, incluidas las capturas `display-<tamaño>-{menu,match,pause}.png`, `build.txt` y `verification.json`.

## Punto para continuar

1. Prueba humana con mandos físicos y de legibilidad (lista en `CONTINUAR.md`).
2. SYS-072: completar las opciones de reducción (destellos, sacudida y movimiento del fondo por separado).
3. Hurtboxes anatómicas, cámara con personas, arte final y rendimiento medido (CIERRE-455 a 458).
