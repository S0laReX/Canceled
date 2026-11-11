# Controles locales FUNADOS 0.4.1

Fecha: 11/11/2026. Esta entrega continúa el bloque Dispositivos y control. Hay implementación y evidencia automática; no se completó la checklist entera ni se aceptó la primera entrega con personas y mandos físicos.

| ID | Implementación y evidencia | Falta para aceptación completa |
|---|---|---|
| SYS-022 | Teclado/ratón+mando y dos mandos con dispositivos exclusivos por puesto. Entrada enviada por Input System mueve únicamente al dueño. | Prueba con dispositivos físicos y distintos backends. |
| SYS-023 | Se conservan 2–5 participantes. Cinco mandos controlan cinco luchadores; sexto mando queda fuera. La regresión conserva los cinco kits y cuatro arenas. | Sesiones de 2–5 personas, mandos físicos y comodidad. |
| SYS-026 | Movimiento mediante cruceta o stick, combinado y normalizado. | Ajustes editables de sensibilidad/deadzones y vibración. El deadzone de stick sigue siendo el del Input System existente. |
| SYS-027 | Pausa al perder un mando reservado, bloqueo de reanudación, reconexión al mismo puesto y reemplazo explícito. Protege cuenta atrás, reloj, posición, porcentaje y stocks. | Ensayo físico de desconexión/reconexión USB/Bluetooth y cambio de identidad del dispositivo. |
| SYS-066 | Indica el puesto sin mando en HUD y pausa; instrucciones visibles y botón Continuar deshabilitado hasta recuperar dispositivos. | Revisión de legibilidad/accesibilidad con personas. |
| QA-445 | 26 pruebas virtuales de asignación, pérdida, reconexión, reemplazo, cupo y aislamiento. | Remapeo/conflictos no implementados; matriz física pendiente. |

## Resultado comprobado

Scripts: compilación sin errores. Build Windows 0.4.1: Succeeded, **0 errores, 485 avisos**, 179.098.963 bytes y 47,39 segundos. Compilaciones anteriores conservadas. No se evaluó cada aviso ni se midió rendimiento sostenido.

Prueba focal: **26/26**, cero fallos. Prueba completa: **109/109**, cero fallos. Se revisaron las capturas del menú y de la desconexión a 1280×720. El bloque de dispositivos crea mandos virtuales dentro de Input System, envía eventos y retira únicamente esos dispositivos; rechaza ejecutarse si encuentra mandos físicos. No sustituye pruebas con hardware real.

Build: `Builds/Funados/0.4.1/Windows/FUNADOS.exe`. Evidencia: `Evidence/Controls-0.4.1/{Focused,Full}/smoke-report.json`, capturas, player.log y build.txt. Los informes anteriores no se sobrescribieron. Los SHA256 de los 22 archivos visuales reutilizados no cambiaron.

## Decisiones

La asignación se fija al comenzar la ronda. Conectar un mando nuevo no toma un puesto activo ni convierte un bot en humano. Si falta un dispositivo, Start desde un mando libre reclama el primer puesto perdido; el botón de pausa permite asignar disponibles. Reanudar requiere una confirmación posterior. En desconexiones múltiples, cada puesto debe recuperarse. Los mandos sobrantes se ignoran también para pausar.

En modo solo, el mando inicial puede compartir P1 con teclado/ratón. Si se desconecta, se pausa y exige recuperarlo o volver al menú. El modo de entrada no se persiste todavía entre ejecuciones. El rig, los recursos y el balance permanecen como en la entrega anterior.

## Punto para continuar

**SYS-025: remapeo con detección de conflictos y controles mostrados según dispositivo.** Continuar luego con SYS-068/069 para guardado versionado y recuperable. El archivo `CONTINUAR.md` en la raíz contiene contexto, rutas, comportamiento, pruebas y pendientes de aceptación. CIERRE-455–458 siguen abiertos; la checklist original se mantiene sin marcar para no confundir pruebas automáticas con aceptación final.
