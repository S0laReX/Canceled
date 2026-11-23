# Procedencia y licencias de FUNADOS (SYS-017, D04)

Fecha: 23/11/2026 · Rama `chore/restauracion-licencias` · Base `main` 0.7.0 (`130a3c6`).

Inventario completo, archivo por archivo con SHA256: [`inventario_activos.csv`](inventario_activos.csv) (119 archivos) y [`inventario_paquetes.csv`](inventario_paquetes.csv) (68 paquetes). Se regeneran con:

```
python -I Tools/inventario_licencias.py --package-cache <proyecto>/Library/PackageCache
```

El guion solo lee archivos. La procedencia sale del propio archivo: cabecera FBX (programa que lo exportó), metadatos XMP de los PNG (Meshy escribe ahí el sistema, la fecha y un `ContentID`) y el `LICENSE.md` de cada paquete en `Library/PackageCache`.

**Estados:** `confirmado` = el origen y la licencia se ven en el archivo o en el repositorio y permiten el uso actual · `desconocido` = no hay evidencia · `pendiente-usuario` = el origen está claro pero la licencia depende de datos de la cuenta que solo tiene el usuario (decisión **D04**).

## Resumen

| Grupo | Archivos | Origen detectado | Licencia | Estado |
|---|---|---|---|---|
| Personajes (11 aspectos: FBX + albedo 2048²) | 22 | Meshy, ya en la beta local 0.3 (`README_BETA.md`). FBX exportados con *Blender FBX IO 4.4.0*, el exportador que usa Meshy. Sin XMP ni ID de tarea | Términos de Meshy según el plan de la cuenta al generarlos, o del autor si salieron de la comunidad de Meshy | **pendiente-usuario** |
| Escenarios: modelos (4 arenas × 7 piezas, FBX + base color) | 56 | Meshy de imagen a 3D a partir del concepto de cada pieza (16/11/2026, commit `c2ad544`). Mismo exportador Blender 4.4.0 | Términos de Meshy según el plan | **pendiente-usuario** |
| Escenarios: conceptos (4 generales + 28 piezas) | 32 | Meshy de texto/imagen a imagen. Según el XMP, se generaron con `fal-ai/nano-banana-pro` (y `/edit` en las piezas) entre las 22:07 y las 22:49 UTC del 16/11/2026. Cada uno lleva su `ContentID` | Términos de Meshy según el plan; el modelo de imagen es de terceros servido por Meshy | **pendiente-usuario** |
| Escenarios: capturas `*_UnityPreview.png` | 4 | Capturas propias del Editor | Propio | confirmado |
| Cubo de validación `FUNADOS_MeterCube.fbx` | 1 | Hecho en Blender 4.5.16 LTS (descarga oficial con SHA256 verificado, `Docs/Produccion_2026-11-10/FUNADOS_Reporte_preparacion.md`) | Propio; Blender (GPL) no impone licencia a lo que exporta | confirmado |
| Shader `FunadosToonStarter.shader` + material | 1 | Escrito para el proyecto | Propio; incluye bibliotecas HLSL de URP (Unity Companion License) | confirmado |
| Audio | 0 archivos | No hay audio importado: `BrawlFeedback.cs` sintetiza efectos y el bucle con `AudioClip.Create` | Propio (código) | confirmado |
| Fuentes | 0 archivos | El HUD usa IMGUI con la fuente por defecto del motor (`GUI.skin`). No hay `.ttf`/`.otf` en `Assets` | Incluida en el runtime de Unity | confirmado |
| Plantilla URP (`TutorialInfo`, `Readme.asset`, `SampleScene`, `Settings/*`) | 1 binario | Plantilla Universal 3D de Unity | Contenido de plantilla de Unity. `SampleScene` no entra en la build | confirmado |
| Respaldos `Backups/*.zip` | 2 | Copias locales de código, ajustes y evidencia (sin arte de terceros) | Propio | confirmado |
| Paquetes (`Packages/manifest.json` + dependencias) | 68 | Registro de Unity y paquetes integrados | Ver la tabla siguiente | confirmado (con dos avisos) |

Total: **9 confirmados y 110 pendientes del usuario** entre los archivos de arte. El código C# es propio del proyecto.

## Paquetes de `Packages/manifest.json`

Licencia tomada del `LICENSE.md` de cada paquete en la importación limpia (`D:\FunadosRestore\Library\PackageCache`). Los 37 `com.unity.modules.*` son módulos del motor (licencia del Editor/Runtime de Unity).

| Paquete | Versión | Licencia | Avisos de terceros relevantes | Nota |
|---|---|---|---|---|
| com.unity.ai.assistant | 2.21.0-pre.2 | Términos de servicio de Unity | Markdig, Roslyn, Protobuf, HuggingFace Hub… (MIT/BSD/Apache) | **Preliminar**, solo del Editor. Usa servicios en la nube de Unity. Quitarlo o fijar uno estable antes de publicar |
| com.unity.ai.inference | 2.6.1 | Términos de servicio de Unity | ONNX (Apache 2.0), Protobuf (BSD-3), MSAGL (MIT) | Lo arrastra ai.assistant; el juego no lo usa |
| com.unity.ai.navigation | 2.0.14 | Unity Companion License | — | |
| com.unity.animation.rigging | 1.4.1 | Unity Companion License | — | |
| com.unity.cinemachine | 3.1.7 | Unity Companion License | Third Party Notices.md | |
| com.unity.collab-proxy | 2.13.5 | Unity Package Distribution License | zlib, LZ4, Antlr, Log4Net | Solo del Editor |
| com.unity.ide.rider | 3.0.38 | MIT | — | Solo del Editor |
| com.unity.ide.visualstudio | 2.0.26 | MIT | — | Solo del Editor |
| com.unity.inputsystem | 1.20.0 | Unity Companion License | — | |
| com.unity.multiplayer.center | 1.0.1 | Unity Package Distribution License | Third Party Notices.md | Solo del Editor |
| com.unity.pipeline | 0.9.1-exp.1 | Términos de servicio de Unity | Roslyn y System.* (MIT) | **Experimental**, para agentes del Editor. No entra en la build |
| com.unity.probuilder | 6.1.4 | Unity Companion License | Poly2Tri (BSD-3), csg.js, KdTree (MIT) | |
| com.unity.render-pipelines.universal | 17.5.0 | Unity Companion License | FXAA3_11 (licencia propia de NVIDIA, incluida) | |
| com.unity.test-framework | 1.7.0 | Unity Companion License | — | |
| com.unity.timeline | 1.8.12 | Unity Companion License | — | |
| com.unity.ugui | 2.5.0 | Unity Companion License | — | |
| com.unity.visualscripting | 1.9.12 | Unity Package Distribution License | DotNetZip (Ms-PL), FatCow Icons (CC BY 3.0)… | Los iconos con atribución son solo del Editor |
| Transitivos (burst, collections, mathematics, render-pipelines.core, shadergraph, splines, nuget.*, app-ui, searcher, settings-manager, ext.nunit, test-framework.performance, 2d.sprite, universal-config) | lock | Unity Companion / Package Distribution License | Burst: LLVM (Apache 2.0 + excepción LLVM), NCSA. App UI: fuente Inter (OFL 1.1), Phosphor Icons (MIT) | Detalle en el CSV |

Ningún paquete viene de Asset Store, Git ni carpeta local: todos son `registry` o `builtin` (`packages-lock.json`). Las licencias Unity Companion y Package Distribution permiten usar los paquetes en proyectos hechos con Unity, así que **los paquetes quedan confirmados**. Avisos sin bloqueo: `ai.assistant` (pre) y `pipeline` (exp) son herramientas del Editor y conviene quitarlas o estabilizarlas antes de una versión pública (lo mismo dice `Docs/Produccion_2026-11-10/FUNADOS_Compatibilidad.md`).

## Lo que falta para cerrar D04 (decisión del usuario)

Los 110 archivos pendientes son todos de Meshy. Hacen falta datos de la cuenta que no se pueden sacar del repositorio:

1. **Plan de Meshy con el que se generó cada lote**: personajes (antes del 11/11/2026, beta 0.3) y escenarios y conceptos (16/11/2026). Según el Help Center citado en `FUNADOS_Compatibilidad.md`, lo generado con el plan Free es **CC BY 4.0**: permite uso comercial pero exige atribuir a Meshy (créditos del juego). Los planes de pago tienen otras condiciones, que hay que revisar en los términos vigentes. La cuenta conectada hoy tiene 25 créditos permanentes y 0 mensuales, sin suscripción activa. Eso no dice qué plan había en noviembre.
2. **Personajes de la comunidad**: nombres como `Commander_Vex`, `Neon_Rebel` o `Tin_Foil_Fred` no siguen la convención del proyecto. Hay que confirmar que los generó el usuario y que no se bajaron de la galería pública de Meshy, porque en ese caso la licencia es la del autor. La API de Meshy solo devuelve las tareas creadas por API (hay una, de imagen a imagen, que no corresponde a ningún archivo del repo). Las tareas de la web hay que mirarlas en meshy.ai → *My Assets*.
3. **Imágenes de referencia** usadas en imagen a imagen y en imagen a 3D: hay que confirmar que eran propias o tenían permiso.
4. **Decisión D04**: aceptar CC BY 4.0 con atribución, pasar a un plan de pago y regenerar, o sustituir los modelos. Si se acepta la atribución, añadir el crédito «Modelos 3D y arte conceptual generados con Meshy (meshy.ai)» a los créditos y al README.

Cuando el usuario lo confirme, cambiar el estado en `Tools/inventario_licencias.py` (constante `MESHY_PENDING` y estado de `classify`), regenerar los CSV y marcar la casilla.
