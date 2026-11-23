# Restauración desde Git/LFS en un entorno limpio (SYS-004/006, QA-441)

Fecha: 23/11/2026 (00:56 a 01:37, hora local) · Equipo `SANTIAGOPC` · Repositorio `https://github.com/S0laReX/Canceled.git`, rama `main`, commit `130a3c6` (0.7.0).

**Resultado: PASA.** Se clonó a una carpeta nueva fuera del repo de trabajo (`D:\FunadosRestore`). No quedó ningún puntero LFS sin descargar. Unity importó el proyecto desde cero (sin `Library`) sin errores, la build salió y `-arena-smoke` y `-side-smoke` pasan sobre esa build.

## Cómo repetirlo

```
powershell -ExecutionPolicy Bypass -File Tools\restaurar_limpio.ps1 -Destino D:\FunadosRestore
```

El guion [`Tools/restaurar_limpio.ps1`](../../Tools/restaurar_limpio.ps1) hace estos pasos y cronometra cada uno en `<Destino>\_restauracion\resumen.json`:

1. **Requisitos.** Comprueba git, git-lfs y el Unity de `ProjectSettings/ProjectVersion.txt`, que debe estar instalado con Unity Hub.
2. **Clonar.** Clona con `GIT_LFS_SKIP_SMUDGE=1` y después ejecuta `git lfs pull`, medido aparte. Si la carpeta ya es un clon, **no vuelve a descargar**: GitHub gratuito da 1 GB de LFS al mes y cada clon gasta unos 560 MB.
3. **Punteros.** Lee la cabecera de los 1.403 archivos de `git lfs ls-files` y ejecuta `git lfs fsck`.
4. **Importar.** Ejecuta Unity en batchmode con `-quit` sobre una `Library` inexistente. Busca en el log `error CS`, «Scripts have compiler errors» y «Compilation failed».
5. **Build.** `-executeMethod CancelBrawl.Beta.Editor.FunadosProjectBuilder.BuildWindows`; comprueba la salida, `FUNADOS_BUILD_SUCCEEDED` y que existe el `.exe`.
6. **Suites.** Ejecuta cada suite (por defecto `arena-smoke` y `side-smoke`) con `-isolate-physical-pads`, a 1280×720 en ventana y con D3D11. Para pasar, la salida debe ser 0 y `side-report.json` debe tener `passed=true`.

Con `-Desde importar|build|suites` se reanuda en un paso; con `-Suites` se eligen otras suites.

## Tiempos y resultados

| Paso | Tiempo | Resultado |
|---|---|---|
| `git clone` (sin LFS) | 5,6 s | OK. 3.275 archivos de trabajo |
| `git lfs pull` | 62,3 s | OK. **562,1 MB** en `.git/lfs/objects` |
| Comprobar punteros | 4,4 s | **0 punteros** en 1.403 archivos LFS; `Git LFS fsck OK` |
| Importación limpia (Unity 6000.5.7f1) | 1.225 s (20 min 25 s) | Salida 0, **0 errores**, 4 avisos de compilación únicos (CS0618 `FindObjectsByType` en `BrawlArenaStress.cs`). Paquetes resueltos en 61,9 s (68 registrados); primera actualización de assets en 304 s |
| Build Windows 0.7.0 | 350,6 s | `result=Succeeded; errors=0; warnings=489`; 265 MB (carpeta de 253 MB) |
| `-arena-smoke` | 125,4 s | **PASA: 46 comprobaciones, 0 fallos** |
| `-side-smoke` | 655,7 s | **PASA: 375 comprobaciones, 0 fallos** |
| **Total** | **2.431 s (40,5 min)** | |

Los recuentos coinciden con la evidencia de `main` 0.7.0 (arena 46, side 375). Evidencia copiada en `Evidence/Branch-chore-restauracion-licencias/` (`resumen.json`, `build.txt` y los dos `side-report.json`). Las capturas y los logs completos quedaron en el clon.

## Observaciones

- **Avisos de licencia en el log**: tres `[Licensing::Client] Error: Code 404` y un `Access token is unavailable`. Son consultas de entitlements que no se encuentran; después aparece `Successfully resolved entitlement details` y la importación sigue. No son errores del proyecto, y el guion no los cuenta.
- **El clon queda «modificado» después de importar**: 10 archivos YAML/JSON de `Assets/Settings`, `Assets/CancelBrawlBeta/Data` y `ProjectSettings`, más `Evidence/Production-0.7.0/build.txt`. `git diff` (que respeta `core.autocrlf`) solo muestra el cambio de `build.txt`. En los demás, Unity reescribió el archivo con LF y Git los marca por el final de línea, sin cambios de contenido.
- La build gasta los 489 avisos ya conocidos (485 en 0.4.1); no se revisaron aquí.
- La salida de los Player en consola mezcla líneas de arranque (`memorysetup-*`). Son inofensivas.

## Alcance y límites

«Limpio» aquí significa: carpeta nueva, clon nuevo de GitHub, sin `Library`, `Temp`, `Builds` ni `Evidence` locales, y con todos los binarios bajados de LFS. **Se usó el mismo equipo y la misma cuenta de Windows**, con el mismo Unity instalado y su licencia, y con la configuración global de git. Unity también pudo usar su caché global de paquetes (`%LOCALAPPDATA%\Unity\cache`), así que no se midió una descarga de paquetes desde cero. Una máquina virgen añadiría la instalación de Unity Hub, del editor 6000.5.7f1 con el módulo Windows y de Git LFS, más esa descarga de paquetes. No hace falta nada más del repositorio de trabajo: el clon no dependió de `D:\Funados`.
