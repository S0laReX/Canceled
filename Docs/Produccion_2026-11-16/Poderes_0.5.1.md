# FUNADOS 0.5.1 — poderes y continuidad lateral

La conversión a X/Y dejaba los sectores seguros originales de Tío Antena fuera del plano jugable. Ahora su súper protege una franja central de ±1,4 m; los lados permanecen peligrosos hasta el alcance del ataque y se mantiene su límite de altura. Un rival a 3 m puede entrar al centro durante la preparación. También puede salir del alcance o colocarse por encima del volumen.

Los avisos de los cinco supers muestran el volumen lateral: rectángulos para barridos y zona de Lola, círculo para la burbuja de Cripto. El contorno blanco y el rótulo REFUGIO identifican la protección central; rojo indica peligro. El fuerte y la trayectoria inicial del token también se anuncian en X/Y. Los avisos siguen a su dueño sin heredar su giro, se congelan con pausa/hitstop y se limpian al cancelar o abandonar la sesión. Se conservó la rama histórica 3D para regresión.

## Verificación y límites

La prueba focal `-power-smoke` contiene 32 casos: cinco supers, consumo de barra/impacto único, centro y bordes del refugio, peligro a ambos lados, salida horizontal y por altura, entrada caminando, pausa/reanudación, interrupción, limpieza, efectos reducidos, tres especiales adicionales y orientación del fuerte. La suite `-side-smoke` incorpora esos casos y pasa de 56 a 88. `-beta-smoke` mantiene 109 casos históricos; su informe indica versión 0.3.0 por las reglas que fuerza.

Los resultados definitivos y hashes están en `Evidence/Production-0.5.1/verification.json`; los informes actuales son `Focused/side-report.json`, `Side/side-report.json` y `Legacy/smoke-report.json`. Las carpetas Initial y LabelFirst preservan iteraciones anteriores: se corrigió primero un rótulo ausente y después su solapamiento con P1. No usar sus capturas como presentación final.

La primera reconstrucción limpia generó 485 advertencias de shaders de dependencias Sentis/ConvGeneric; están contabilizadas en `build-initial.txt`. Las dos builds incrementales siguientes terminaron con cero advertencias y cero errores. No se alteraron paquetes para ocultarlas. La build entregada está descrita en `build.txt`.

Unity inicialmente importó BrawlSideTelegraph como MonoScript pero lo omitió de CompilationPipeline. Se diagnosticó la lista de fuentes, se reconstruyó la caché y se volvió a registrar mediante AssetDatabase.MoveAsset, restaurando el nombre y conservando su GUID. La compilación posterior terminó sin errores. No hay una dependencia del nombre temporal ni hay que borrar Library para continuar.

Los 22 archivos FBX/PNG del inventario original de personajes conservan sus SHA256; Git LFS pasó fsck. No se añadieron sustituciones de arte. Las capturas verifican presentación básica; balance con personas, rig, claridad visual en todas las arenas y relaciones de aspecto, mandos físicos y rendimiento siguen pendientes.

## Próxima tarea

Empezar por **P0-03 / SYS-045** en `CHECKLIST_2p5D.md`: resolver impactos simultáneos sin ventaja del orden de Fighters. El bucle secuencial está en BrawlMatch.FixedUpdate; ReceiveHit cancela el ataque antes de que otro luchador tenga su turno. Reproducir con ataques simultáneos, invertir orden/puestos y cubrir interrupción, KO, proyectiles y cargas sin daño duplicado. Después retomar remapeo y opciones recuperables.

El juego completo sigue abierto. Esta entrega corrige poderes y avisos; no cierra CIERRE-455–458 ni el catálogo de trece personajes/ocho arenas.
