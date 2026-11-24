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

## Términos de Meshy (consultados el 24/11/2026)

Esto dicen las fuentes oficiales de Meshy:

| Caso | Licencia del resultado | Qué exige |
|---|---|---|
| Generado con el plan **Free** | **CC BY 4.0**: uso comercial permitido | Atribuir a Meshy, por ejemplo con «Model created with Meshy – CC BY 4.0 License» |
| Generado con un plan **de pago** | **Propiedad privada** del usuario, sin atribución | No haberlo publicado en la comunidad de Meshy |
| Modelo **publicado en la comunidad** de Meshy | **CC0** según los términos de servicio | CC0 no cubre personajes ni marcas de terceros |
| Cambio de plan | Cada modelo conserva los derechos del plan con el que se generó | — |
| **Imágenes de referencia** subidas | En cualquier plan, el uso comercial depende de tener derechos sobre la imagen de origen | — |

Fuentes:
- [Propiedad de los modelos generados](https://help.meshy.ai/en/articles/10137554-what-is-the-ownership-of-the-generated-models)
- [Uso comercial](https://help.meshy.ai/en/articles/16102098-can-i-use-meshy-assets-commercially)
- [Precios y planes](https://docs.meshy.ai/en/webapp/pricing)
- [Venta en marketplaces](https://help.meshy.ai/en/articles/16102951-can-you-publish-or-sell-meshy-models-on-marketplaces)

Un artículo del Help Center (el de marketplaces) contradice a los demás: dice que lo del plan Free es en general solo para uso personal. Lo prudente es pedir a Meshy que lo aclare antes de vender el juego con modelos del plan Free.

**Conclusión:** sea cual sea el plan, el uso actual (juego local, sin venta) está permitido. **Añadir la atribución cubre el peor caso** (plan Free), así que el texto de [`CREDITOS.md`](CREDITOS.md) ya está preparado. Lo que de verdad queda abierto son las imágenes de referencia y la contradicción para la venta comercial.

## Lo que falta para cerrar D04 (decisión del usuario)

Los 110 archivos de Meshy siguen como `pendiente-usuario` hasta que Santiago confirme lo siguiente:

1. **Imágenes de referencia.** Hay que confirmar que las imágenes subidas para imagen a imagen y para imagen a 3D eran propias o tenían permiso. Es lo único que ningún plan cubre. Los conceptos de escenarios son de imagen a imagen (`nano-banana-pro/edit`), así que partieron de alguna imagen.
2. **Origen de los personajes.** Nombres como `Commander_Vex`, `Neon_Rebel` o `Tin_Foil_Fred` no siguen la convención del proyecto. Hay que confirmar si los generó Santiago o si salieron de la comunidad (CC0). En ese segundo caso, también hay que confirmar que no imitan personajes ni marcas de terceros.
3. **Plan de cada lote.** Los personajes son de antes del 11/11/2026 y los escenarios y conceptos del 16/11/2026. Hoy la cuenta tiene 25 créditos permanentes y 0 mensuales, sin suscripción, pero eso no prueba qué plan había en noviembre. Se puede ver en meshy.ai → *Settings → Billing* y en *My Assets*. Mirarlo requiere iniciar sesión: en el navegador integrado no había sesión.
4. **Decisión D04:** aceptar la atribución (recomendado: `CREDITOS.md` ya está escrito) o regenerar o sustituir los modelos.

Cuando estén confirmados los puntos 1 y 2 y aceptada la atribución, hay que hacer tres cosas:
- en `Tools/inventario_licencias.py`, cambiar a `confirmado` el estado de `personaje`, `escenario-modelo` y `escenario-concepto`;
- regenerar los CSV;
- marcar la casilla.
