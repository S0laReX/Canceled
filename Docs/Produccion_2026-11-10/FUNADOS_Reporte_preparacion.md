# FUNADOS Informe de preparación

Fecha: 10 de noviembre de 2026. Dirección confirmada: lucha de plataformas **3D libre**, movimiento X/Z y altura Y. Don Promesio y Lola Viral son los nombres públicos elegidos para el diseño; los identificadores internos Don Decreto y Lola Loop se conservan.

## Entorno identificado

Proyecto real: `D:\Funados`. Unity del proyecto: **6000.5.7f1**; también existen 6000.5.5f1 y 6000.5.6f1. No se reinstaló ni actualizó Unity. VS Code **1.145.2**, Git **2.53.0.windows.1** y Unity CLI **1.0.0-beta.14** ya estaban instalados y se conservaron.

Windows 11 Home x64, versión 10.0.26300; AMD Ryzen 7 7840HS, 8 núcleos/16 hilos; aproximadamente 32 GB de RAM; NVIDIA RTX 4050 Laptop y Radeon 780M. No se usa la cifra de VRAM de WMI como medida fiable. Al inventariar: aproximadamente 26 GiB libres en C: y 593 GiB en D:. No son mínimos oficiales ni una medición de rendimiento del juego.

La beta existente es Cancel Brawl local 0.3: cinco kits, cuatro arenas, once modelos Meshy y rig generado por código. El nuevo GDD concentra calidad inicialmente en dos luchadores y Azotea Neón, conservando los demás contenidos.

## Protección del proyecto

Git existe en rama `main`, **sin commits**; el contenido estaba sin seguimiento. No se inventó identidad Git ni se publicó nada. Antes de abrir el Editor se creó `FUNADOS_respaldo_fuentes_2026-11-10.zip`, con **194 archivos** de Assets, Packages, ProjectSettings, .git y archivos raíz relevantes. Se comprobó el SHA256 de cada entrada contra su fuente; el manifiesto adjunto guarda las verificaciones y el hash del archivo ZIP.

Es un respaldo de fuentes y configuración. No duplica Library, Temp, Builds, Evidence ni los backups anteriores; esos directorios se conservaron en su ubicación. No se borró ningún archivo existente. La comparación posterior de los 194 archivos detectó cambios únicamente en `Packages/manifest.json` y `Packages/packages-lock.json`, gestionados por Package Manager. Los scripts, modelos, escenas, ajustes y metadatos originales conservan sus hashes.

## Instalaciones y comprobaciones

| Componente | Resultado |
|---|---|
| Blender | **4.5.16 LTS x64**, distribución ZIP portable oficial instalada en `C:\Users\SANTIAGO\Documents\Codex\Tools\blender-4.5.16-windows-x64`. Acceso en Inicio: **Blender 4.5 LTS FUNADOS**. Se conserva la carpeta incompleta anterior de Blender 4.2. |
| Procedencia Blender | Descarga de `https://download.blender.org/release/Blender4.5/blender-4.5.16-windows-x64.zip`; SHA256 **E258E90A1EBC14798AD65B3426BC5F11F5E0E013541C61CC7C3EE9BBF4CA0CE6**, coincidente con el archivo oficial de hashes. Firma del ejecutable válida, Blender Foundation. |
| Prueba Blender | Ejecutable devuelve 4.5.16 LTS; guardado de .blend, exportación FBX y reimportación de cubo de un metro correctos. La repetición fuera de la restricción inicial de archivos terminó sin el aviso de caché de extensiones. No se instalaron add-ons terceros. |
| Cinemachine | **3.1.7**, instalado desde registro oficial y confirmado mediante Package Manager. Disponible para trabajo de cámara; no sustituye la cámara de la beta automáticamente. |
| Animation Rigging | **1.4.1**, instalado y confirmado. Disponible para rigs de producción; no convierte el rig automático existente en un rig final. |
| ProBuilder | **6.1.4**, versión ofrecida por el registro del Editor real, instalada y confirmada. Disponible para blockout de escenarios. |
| URP y Shader Graph | **17.5.0**, ya presentes, conservados y verificados. |
| Input System | **1.20.0**, ya presente; Active Input Handling = Input System. Conservado. |
| AI Navigation | **2.0.14** preexistente, conservado. No se instaló Unity Behavior ni otro sistema de bots. |
| Unity agentes | CLI conectó con el proyecto y ejecutó consultas, instalaciones y scripts. Pipeline **0.9.1-exp.1** ya presente, sin actualizar. El plugin oficial `unity@unity-agent-plugin` **0.1.9-beta** está instalado y habilitado. También se detectó una copia preexistente `unity@openai-curated-remote` **0.1.8-beta**; no se añadió ninguna copia ni servidor MCP. Revisar su consolidación antes de ampliar integraciones. |
| Tweening | LitMotion y DOTween no se añadieron. Los efectos existentes tienen código propio; elegir una sola biblioteca cuando una transición o UI concreta lo requiera. Las alternativas y licencias están documentadas en la matriz técnica. |

WinGet no ejecutó dentro del acceso inicial restringido; la consulta autorizada funcionó. Su catálogo ofrecía Blender 4.5.10 LTS, mientras el servidor oficial tenía 4.5.16; se descargó y verificó esta última directamente. Los primeros accesos a hardware/Git/VS Code también necesitaron la vía autorizada; el inventario se completó sin reinstalarlos.

## Material y prueba de intercambio

Se añadió un directorio nuevo `Assets/FunadosProduction`, sin modificar referencias de la beta:

- `Rendering/FunadosToonStarter.shader`: shader original sencillo URP, tres bandas, color/textura, rim y niebla. Iluminación de arte fija; **no recibe sombras ni responde a todas las luces de escena**. Es referencia inicial de estilo, no el shader final de producción.
- `Rendering/FunadosToonStarter.mat`: material generado por el Editor, con .meta.
- `Validation/FUNADOS_MeterCube.fbx`: modelo de control exportado por Blender. Unity comprobó dimensiones de un metro.

La importación del shader no informó errores y creó el material. No se aplicó a los personajes ni se evaluó visualmente con GPU: el Editor de preparación se abrió sin gráficos. La aceptación artística, sombras, contornos y coste GPU siguen siendo trabajo de producción.

## Verificación técnica

El Editor abrió el proyecto y alcanzó estado ready; antes de las instalaciones no informó errores de compilación. Se confirmaron las tres dependencias nuevas y las versiones gráficas sin editar manualmente el manifiesto.

Se generó una **compilación Windows de desarrollo nueva** en la carpeta de trabajo, conservando `D:\Funados\Builds`. Resultado: **Succeeded, 0 errores, 485 avisos**, duración aproximada 111 segundos. Se observaron avisos de shaders de `com.unity.ai.inference`, incluyendo conversiones de tipos y operaciones potencialmente lentas. La consola tras build indicó 0 errores y 40 avisos agrupados. El número agrupado no equivale al total del informe de build. No se afirma que todos los avisos sean causados por el mismo componente.

La beta ya incluía AI Assistant **2.21.0-pre.2**, AI Inference **2.6.1** y Pipeline experimental. Se conservaron para evitar cambios ajenos. Sus avisos, tamaño e impacto deben evaluarse antes de publicar; no son requisitos nuevos de FUNADOS. No se añadió otro paquete experimental.

La prueba integrada de esta sesión terminó con **67 comprobaciones superadas, 0 fallos**. Cubrió reglas, cinco kits, cuatro arenas, modelos/rig y actividad de bots. Se ejecutó la compilación nueva con Direct3D 11 sobre la RTX 4050 (el Player informó 5920 MB de VRAM). Las capturas de la ventana oculta resultaron negras: por ello esta prueba acredita lógica en ejecución, **no validación visual ni rendimiento**. La revisión visible del Player sigue pendiente. El informe nuevo está en FUNADOS_Prueba_automatica.json; no se sustituyó la evidencia histórica.

No se probaron mandos físicos, comodidad, diversión, remapeo completo, rendimiento sostenido ni licencias originales de los once modelos Meshy. Una prueba automática no sustituye esas validaciones.

## Entregables y uso

- **FUNADOS_GDD.docx / .md**: documento principal de 28 páginas, trece arquetipos, kits, reglas 3D, controles, escenarios, pipeline, audio, accesibilidad, alcance, roadmap, riesgos y criterios verificables.
- **FUNADOS_Trello_Miro.docx / .md**: guía operativa de 10 páginas, listas, etiquetas, plantillas, Definition of Done, WIP, sprints y vínculos bidireccionales.
- **FUNADOS_Trello_tarjetas.csv**: 42 tarjetas preparadas; Trello no dispone de importador genérico completo CSV. La guía explica pegar títulos por lista y completar descripciones y aceptación.
- **FUNADOS_Miro_notas.csv**: 48 notas para ocho marcos. Copiar celdas de Texto por marco y pegarlas como notas; no promete reconstrucción automática de flechas o posiciones.
- **FUNADOS_Compatibilidad.md**: matriz y fuentes oficiales, alternativas, licencias y condiciones de adopción.
- Respaldo ZIP y manifiesto de integridad.

Los DOCX se comprobaron mediante extracción estructural sin unidades de contenido perdidas y exportación de Word 16 a PDF temporal: 28 y 10 páginas, sin páginas vacías; se revisó la maquetación de todas las páginas. El renderer empaquetado no dispone de LibreOffice en este equipo; se utilizó Word instalado, sin editar documentos del usuario. Los PDF e imágenes de revisión son temporales.

No se crearon tableros ni tarjetas en cuentas. Trello apareció disponible como plugin pero sin instalar/conectar; no se encontró una conexión utilizable a Miro. Se entregan instrucciones y datos locales sin subir el proyecto.

## Próximas acciones

1. Abrir la beta existente y realizar una partida con teclado/ratón y un mando, después con dos mandos. Registrar desconexión/reconexión, cámara, profundidad y recuperación.
2. Crear un primer commit local revisado de fuentes, ajustes, paquetes, .meta y documentación; evaluar Git LFS para arte grande antes de elegir remoto. El backup actual permite recuperar la base, pero Git sigue necesitando historial.
3. Ejecutar la primera sesión de 90 minutos de la guía Trello/Miro y elegir un objetivo de sprint basado en defectos observados.
4. Limpiar y riggear un personaje completo en Blender; probar hombros/axilas/manos, exportar FBX y revisar en cámara real antes de producir los otros doce.
5. Evaluar el material toon con luz/sombra y GPU; usar una sola solución final. Perfilar en build la arena y cinco participantes, sin asumir 60 FPS por el hardware.
6. Revisar procedencia de modelos Meshy, paquetes IA preexistentes y copias del plugin Unity antes de publicación. Online y ranking permanecen para investigación posterior al juego local estable.

