# FUNADOS — Stack 3D y compatibilidad

Fecha de consulta: 10 de noviembre de 2026. Este documento registra decisiones técnicas y fuentes; el reporte de preparación del equipo registra qué quedó instalado y probado.

## 1. Decisión de base

FUNADOS debe mantener **movimiento libre en 3D**, conforme a la decisión del usuario y a la beta existente: desplazamiento por el suelo en X/Z y altura/salto en Y. El combate, la recuperación y las expulsiones pueden ocurrir en diferentes direcciones del escenario. Usar URP, entrada por acciones y una cámara que preserve la lectura de todos los participantes. No bloquear el eje Z ni convertir el movimiento en desplazamiento lateral 2.5D. La simulación de combate se mantiene separada de animaciones y efectos visuales.

Preservar el controlador y las colisiones 3D de la beta mientras se revisa su funcionamiento. No sustituirlos por Rigidbody2D, Collider2D ni un controlador comprado. Un único sistema debe ser dueño del movimiento de cada luchador: evitar que Rigidbody, CharacterController, root motion y cambios directos de Transform compitan. Cualquier cambio posterior de controlador requiere una prueba de regresión del desplazamiento libre, salto, plataforma móvil y knockback vectorial.

El inventario de esta sesión identificó el proyecto **`D:/Funados`**, configurado para **Unity 6000.5.7f1**, URP **17.5** e Input System **1.20.0**. También detectó editores 6000.5.5f1 y 6000.5.6f1 y un Unity CLI existente. Se conserva la versión del proyecto; el archivo oficial de descargas incluye parches posteriores, cuya existencia no autoriza una migración automática. No reinstalar Unity ni VS Code. [Archivo oficial de Unity](https://unity.com/releases/editor/archive).

El manifiesto observado incluye además AI Navigation **2.0.14**, AI Assistant **2.21.0-pre.2** y Pipeline **0.9.1-exp.1**. Estos dos últimos identificadores son prerelease/experimental ya presentes. No se añaden como recomendación de producción, ni se retiran sin revisar referencias y consecuencias. Su presencia debe quedar en el registro de riesgos; conservar un proyecto y evitar nuevas dependencias experimentales son decisiones compatibles. Las pruebas del reporte de preparación determinan qué componentes se pudieron verificar realmente.

Unity 6000.5 aparece como versión Legacy en la documentación consultada; esto no la convierte en beta ni justifica un cambio automático de Editor. Antes de publicar, revisar parches de seguridad y soporte, y evaluar una actualización en una copia con pruebas. No rebajar un proyecto 6.5 a 6.3 para coincidir con un tutorial.

## 2. Matriz para Unity 6000.5

Las versiones siguientes son las publicadas en la documentación 6000.5 al consultar, no una orden de actualizar dependencias existentes. La selección final debe coincidir con el registro que ofrece el Editor real y quedar fijada en `manifest.json` y `packages-lock.json`. Input System 1.20.0 ya instalado no necesita actualizarse a 1.20.1 para preparar el MVP; URP 17.5 existente se conserva.

| Herramienta | Selección para FUNADOS | Compatibilidad y control |
|---|---|---|
| URP, `com.unity.render-pipelines.universal` | Base gráfica para proyecto nuevo; migración separada si el existente usa otro pipeline | Paquete core ligado al Editor. Elegir su versión correspondiente, sin mezclar URP, Core y Shader Graph de otras ramas. [URP 6000.5](https://docs.unity.com/en-us/engine/6000.5/manual/packages-list/packages-all/pack-core/com-unity-render-pipelines-universal). |
| Shader Graph, `com.unity.shadergraph` | Usar el que acompaña a URP | Es core y admite URP. No instalar una versión independiente de la rama gráfica. [Shader Graph 6000.5](https://docs.unity.com/en-us/engine/6000.5/manual/packages-list/packages-all/pack-core/com-unity-shadergraph). |
| Input System, `com.unity.inputsystem` | Prioridad alta: gamepads, teclado, unión de jugadores y reasignación | Documentación: **1.20.1**, released. Configurar acciones y esquemas; comprobar `Active Input Handling` antes de cambiarlo. [Input System 6000.5](https://docs.unity.com/en-us/engine/6000.5/manual/packages-list/packages-all/pack-safe/com-unity-inputsystem). |
| Cinemachine, `com.unity.cinemachine` | **3.1.7 instalado y comprobado**; cámara de arena 3D, seguimiento de grupo, altura y límites de zoom | La documentación lista 3.1.7, released. Mantener visibles separación horizontal y vertical, bordes y luchadores sin oclusiones. Si existe Cinemachine 2, no migrar sin revisar componentes y escenas: pasar a 3 requiere trabajo. [Cinemachine 6000.5](https://docs.unity.com/en-us/engine/6000.5/manual/packages-list/packages-all/pack-safe/com-unity-cinemachine). |
| Animation Rigging, `com.unity.animation.rigging` | **1.4.1 instalado y comprobado**; manos, miradas, agarres y ajuste de pies | La documentación lista 1.4.1, released. Probar la pose final con el Animator al configurar cada rig; instalarlo no equivale a modificar todos los personajes. [Animation Rigging 6000.5](https://docs.unity.com/en-us/engine/6000.5/manual/packages-list/packages-all/pack-safe/com-unity-animation-rigging). |
| ProBuilder, `com.unity.probuilder` | **6.1.4 seleccionado desde el registro real del Editor** para blockout; ver reporte final de importación | La documentación web 6000.5 todavía lista 6.1.3 y 6.1.2, mientras el registro consultado en el Editor ofrece 6.1.4. Se usa la selección verificada del registro local y se comprueba la resolución, sin imponer la versión antigua de esta página. [ProBuilder 6000.5](https://docs.unity.com/en-us/engine/6000.5/manual/packages-list/packages-all/pack-safe/com-unity-probuilder). |
| AI Navigation | Ya existe 2.0.14: conservar; reutilizar cuando se implemente un bot terrestre 3D | NavMesh puede servir para rutas sobre superficies de la arena 3D. Los saltos entre plataformas, plataformas móviles, recuperación aérea y expulsiones requieren lógica propia y enlaces validados; un agente NavMesh no debe anular el knockback. El MVP local entre personas no requiere configurar navegación adicional. [AI Navigation](https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/index.html). |
| Unity Behavior | Posponer | Añadir cuando un bot necesite un grafo visual y el mantenimiento lo justifique. El MVP local entre personas no depende de este paquete. Su disponibilidad no es una necesidad de diseño. [Unity Behavior](https://docs.unity3d.com/Packages/com.unity.behavior@1.0/manual/index.html). |
| DOTS/Entities, ML Agents, netcode, servicios cloud | Fuera de la instalación inicial | No son requisitos del combate local; aumentan superficie de integración y pruebas. Reevaluar por una necesidad medible. |

Los paquetes oficiales anteriores no requieren comprar un paquete Asset Store adicional; el uso del Editor sigue sujeto a la licencia de Unity aplicable. Un paquete experimental no debe entrar a producción por comodidad: Unity explica que no lo soporta y no garantiza su evolución. [Estados experimentales](https://docs.unity.com/en-us/engine/6000.3/manual/packages-list/packages-all/pack-exp).

## 3. Toon shading sin dependencia dudosa

Decisión recomendada: un material propio sencillo compatible con la versión URP del proyecto. Empezar con Shader Graph URP Unlit, color base, dos o tres bandas obtenidas a partir de una normal y una dirección de luz de arte, rim light moderado y paleta controlada. Ese primer shader simula la iluminación; no debe presentarse como si recibiera automáticamente sombras y todas las luces de URP.

Cuando el aspecto esté aprobado, implementar o integrar iluminación principal y sombras mediante una solución específica para esa versión de URP, con prueba de renderizado y compilación de Player. El outline de malla invertida puede duplicar geometría y fallar en bordes, escalas o transparencias: mantenerlo opcional y medirlo. No añadir un Renderer Feature copiado de otra versión de URP sin revisar Render Graph y pases.

Criterios: ningún material rosa; piel, pelo y ropa legibles en cámara lejana; dos personajes distinguibles sobre escenario claro y oscuro; sombra estable; contorno sin ocultar hurtboxes; tiempo GPU dentro del presupuesto. Un paquete Built-in, HDRP o un asset que solo diga “toon” no prueba compatibilidad con URP. [Compatibilidad de shaders por pipeline](https://docs.unity.com/en-us/engine/6000.3/manual/materials-and-shaders/built-in/shader-built-in).

## 4. Elegir una sola biblioteca de tweens

| Opción | Verificado | Decisión |
|---|---|---|
| DOTween Free | Descarga oficial **1.3.045**, 12 de octubre de 2026; mínimo actual Unity 2022.3. Uso comercial permitido por su licencia propia; no confundir con Pro ni asumir MIT. Requiere ejecutar Setup en su Utility Panel y seleccionar módulos. | Conveniente para un proyecto pequeño que busca pocos paquetes adicionales; conservarlo si ya existe. [Descarga](https://dotween.demigiant.com/download), [licencia](https://dotween.demigiant.com/license.php), [configuración](https://dotween.demigiant.com/getstarted.php). |
| LitMotion | Release consultada **2.0.2**; licencia MIT. Requisitos documentados: Unity 2021.3+, Burst 1.6+, Collections 1.5.1+, Mathematics 1.0+. | Alternativa cuando esas dependencias ya forman parte del proyecto o se quiere un paquete UPM con versión fijada. No instalar ambos para el mismo trabajo. [Instalación](https://github.com/annulusgames/LitMotion/blob/main/docs/articles/en/installation.md), [releases](https://github.com/annulusgames/LitMotion/releases), [licencia](https://github.com/annulusgames/LitMotion/blob/main/LICENSE). |

**Decisión efectiva de preparación: posponer ambas bibliotecas.** La beta ya contiene efectos propios y no se identificó una necesidad concreta que justifique otra dependencia. Nunca usar tweens para calcular el knockback, el desplazamiento de combate o los frames activos: pertenecen a la simulación. Una futura biblioteca se limitará a presentación, transiciones y feedback cancelable.

## 5. Blender: instalación y compatibilidad

La página oficial consultada ofrece **Blender 5.2.4 LTS**, publicado el 10 de noviembre de 2026. Blender **4.5.16 LTS** también figura mantenido; la línea 4.2 ya aparece en LTS anteriores. El catálogo WinGet de este equipo presentó versiones distintas: Blender genérico 5.3.0, Blender LTS 4.5 en 4.5.10 y LTS 4.2 en 4.2.16. Se debe registrar la versión efectivamente adquirida y probada, sin equiparar el índice web con una instalación local. Para coexistencia conservadora se prefiere **4.5 LTS**, con descarga oficial, firma/hash verificables y prueba de ejecución. No elegir 5.3 únicamente porque WinGet la muestre como la más nueva. Conservar versiones y archivos previos, y abrir copias de los `.blend` al cambiar de versión. [Descarga oficial](https://www.blender.org/download/), [LTS mantenidas](https://www.blender.org/download/lts/).

Requisitos publicados para Windows: sistema de 64 bits, CPU de cuatro núcleos con SSE4.2, RAM 8 GB y GPU con 2 GB de VRAM y OpenGL 4.3; la página detalla extensiones necesarias y soporte Vulkan. Recomendado: ocho núcleos, RAM 32 GB y VRAM 8 GB. Son requisitos del programa, no una promesa de fluidez con cualquier modelo. Verificar CPU/GPU/controlador del equipo y que Blender abre y renderiza. [Requisitos oficiales](https://www.blender.org/download/requirements/).

Vía de adquisición: instalador o ZIP enlazado desde blender.org y su infraestructura oficial. El ZIP portable sirve para coexistencia reversible y evita reemplazar asociaciones existentes. No descargar desde agregadores ni usar builds alpha/beta para producción. La instalación solo se da por comprobada cuando el ejecutable devuelve la versión y supera una prueba mínima; guardar procedencia y hash del archivo descargado.

**Resultado efectivo:** Blender **4.5.16 LTS** portable quedó en `C:/Users/SANTIAGO/Documents/Codex/Tools/blender-4.5.16-windows-x64`, con acceso directo en Inicio. La preparación verificó el SHA-256 oficial del ZIP (`E258E90A1EBC14798AD65B3426BC5F11F5E0E013541C61CC7C3EE9BBF4CA0CE6`), firma Authenticode válida de Blender Foundation, respuesta de versión y guardado `.blend` / exportación FBX / reimportación de un cubo de un metro. Esto acredita el flujo básico y conserva la instalación previa; la aceptación de rigs de personajes requiere sus propias pruebas.

## 6. Meshy → Blender → Unity

Meshy es opcional: permite acelerar un primer volumen o una referencia, pero no garantiza topología lista para animación, estilo consistente, colisiones correctas ni derechos sobre imágenes de terceros.

El Help Center indica que los modelos del plan Free usan **CC BY 4.0**, permiten uso comercial y requieren atribución a Meshy. Los planes de pago tienen reglas diferentes. Registrar plan, fecha, prompt, modelo, enlace de origen, licencia y modificaciones de cada asset; revisar también los derechos del material de entrada. Los créditos, descargas y privacidad del plan no deben interpretarse como ilimitados. [Licencia comercial Meshy](https://help.meshy.ai/en/articles/9992001-can-i-use-meshy-assets-commercially-license-copyright-explained), [plan Free](https://help.meshy.ai/en/articles/15696428-what-is-included-on-the-free-plan).

Flujo de aceptación de asset:

1. Diseñar silueta y vistas frontal, lateral y posterior antes de generar. El movimiento libre exige lectura desde cualquier orientación, no solo de perfil. Probar un personaje y un prop, no producir los trece de inmediato.
2. En Blender: limpiar piezas internas, corregir normales, simetría deliberada, retopología en hombros/codos/rodillas, UV, materiales limitados, rig y pesos. Aplicar escala con cuidado antes de riggear.
3. Conservar el `.blend` fuente fuera de `Assets`; exportar **FBX explícito** de geometría, armature y clips. Usar el preset acordado de unidades/ejes, selección de objetos y horneado de animación; validar con una regla de un metro, un giro y un salto.
4. En Unity: comprobar escala/orientación, configurar Humanoid o Generic según rig, revisar Avatar y clips, reconstruir materiales URP. Los materiales nodales de Blender no se trasladan como un toon shader equivalente.
5. Revisar deformación extrema, pies, manos, pelo, transparencias, bounds, LOD y consumo real. Crear hitboxes/hurtboxes sencillas en el sistema de combate, no usar toda la malla visual como collider.
6. Reexportar y confirmar que GUID, prefab y referencias se conservan antes de repetir la producción.

Unity recomienda FBX en producción: importar `.blend` directamente ejecuta el programa de modelado en segundo plano y depende de su instalación. La exportación explícita reduce esa dependencia entre equipos y versiones. [Formatos de modelado y Blender en Unity 6000.5](https://docs.unity.com/en-us/engine/6000.5/manual/assets-and-media/asset-types/models/creating-dccassets/howto-import-objects-from3dapps).

## 7. Unity CLI y plugin oficial para Codex

Hay soporte oficial para Windows. En este equipo se identificó **Unity CLI 1.0.0-beta.14**. El CLI puede instalarse con `winget install Unity.CLI` o con el instalador oficial, pero **no corresponde reinstalarlo si el existente funciona**. Primero consultar versión, editores, proyectos y estado; una versión beta posterior disponible no obliga a actualizar este entorno para la tarea. Si una función imprescindible exige actualización, verificarla y proceder desde la misma vía, evitando dos ejecutables en PATH. [Unity CLI oficial](https://docs.unity.com/en-us/unity-cli/use-unity-cli).

El plugin de agentes de Unity admite Unity 6.0+. Es un conjunto de habilidades para el agente, distinto de un paquete Asset Store. El entorno de esta sesión ya muestra habilidades del plugin oficial; comprobar que esté instalado y habilitado antes de añadir otra copia. La comprobación oficial en Codex usa `codex plugin list`. [Plugin Unity para Codex](https://docs.unity.com/en-us/ai/unity-plugin/codex), [requisitos del plugin](https://docs.unity.com/en-us/ai/unity-plugin/about-unity-plugin).

Para controlar el Editor desde shell, las instrucciones del plugin prefieren el CLI. El paquete de proyecto `com.unity.pipeline` permite comunicación con Unity 6.0+ y **ya aparece en FUNADOS como 0.9.1-exp.1**: verificarlo sin añadir otra copia. Su estado experimental exige no tratarlo como dependencia de runtime del juego ni como prueba automática de estabilidad. Confirmar errores de compilación y estado listo. No desplegar otro servidor MCP para cubrir la misma capacidad. Esto no exige contratar los generadores IA de Unity ni habilitar créditos de pago.

## 8. Puertas de verificación antes de cambiar un proyecto

1. Identificar ruta real, `ProjectVersion.txt`, `manifest.json`, lockfile, pipeline activo, procesos Editor abiertos y estado Git. No confundir carpeta de documentos con proyecto Unity.
2. Respaldar `Assets`, `Packages` y `ProjectSettings`, incluyendo `.meta`, y cualquier fuente externa necesaria. Guardar cambios ajenos y registrar una base recuperable. Un commit no sustituye backup de archivos ignorados importantes.
3. Añadir solo dependencias justificadas. Usar Package Manager/CLI compatible; no modificar `Library/PackageCache` ni borrar lockfiles para “arreglar” dependencias.
4. Esperar resolución e importación; comprobar versiones instaladas, consola sin errores nuevos y materiales correctos. Un manifiesto escrito sin resolver no equivale a instalación confirmada.
5. Probar unión de dos gamepads y reconexión; movimiento X/Z en cuatro direcciones y diagonales sin ventaja de velocidad; salto Y, orientación, knockback vectorial, bordes en todos los lados y recuperación. Revisar cámara con luchadores separados en profundidad y altura, oclusiones, rig y un FBX. Compilar una escena mínima en Windows y abrir el Player. Registrar resultados y limitaciones.
6. Si se necesita IL2CPP para Windows, verificar herramientas C++ y Windows SDK. La documentación 6000.5 exige VS 2019 con C++ Tools o posterior y SDK 10.0.19041+; VS Code es el editor de código y no sustituye ese compilador. Puede posponerse si la prueba usa Mono. [Requisitos Unity 6.5](https://docs.unity.com/en-us/engine/6000.5/manual/get-started/install-and-upgrade/getting-started-installing-unity/system-requirements).

Unity 6.5 para Windows x64 pide Windows 10 21H1 build 19043 o posterior, CPU SSE2 y GPU capaz de DX10/DX11/DX12 o Vulkan con controladores del fabricante. Recomienda al menos 8 GB de RAM; para Blender y Unity simultáneos, 16–32 GB es un objetivo práctico de producción, sujeto a medición en este equipo. La apertura del Editor y una prueba real siguen siendo necesarias. [Requisitos Unity 6.5](https://docs.unity.com/en-us/engine/6000.5/manual/get-started/install-and-upgrade/getting-started-installing-unity/system-requirements).
