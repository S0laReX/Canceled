<#
.SYNOPSIS
  Restaura FUNADOS desde Git/LFS en una carpeta limpia y comprueba que importa, compila y pasa las suites.

.DESCRIPTION
  Pasos (cada uno se cronometra y queda en <Destino>\_restauracion\resumen.json):
    1. Requisitos: git, git-lfs y el editor Unity fijado en ProjectSettings/ProjectVersion.txt.
    2. Clonar sin descargar LFS (GIT_LFS_SKIP_SMUDGE=1) y después `git lfs pull` (se mide aparte).
    3. Comprobar que ningún archivo de LFS quedó como puntero (cabecera "version https://git-lfs").
    4. Importar el proyecto en batchmode (Library nueva) y buscar errores de compilación en el log.
    5. Generar la build Windows con FunadosProjectBuilder.BuildWindows.
    6. Ejecutar las suites indicadas (por defecto -arena-smoke y -side-smoke) sobre esa build.

  Si la carpeta de destino ya es un clon, el paso 2 se salta (la cuota gratuita de LFS en GitHub es
  de 1 GB de descarga al mes y el LFS de FUNADOS pesa unos 541 MB: no clonar dos veces sin motivo).
  Con -Desde se puede reanudar en un paso concreto.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File Tools\restaurar_limpio.ps1 -Destino D:\FunadosRestore

.EXAMPLE
  # Reanudar en la build si la importación ya pasó
  powershell -ExecutionPolicy Bypass -File Tools\restaurar_limpio.ps1 -Destino D:\FunadosRestore -Desde build
#>
[CmdletBinding()]
param(
    [string]$Destino = 'D:\FunadosRestore',
    [string]$Repositorio = 'https://github.com/S0laReX/Canceled.git',
    [string]$Rama = 'main',
    [string]$Unity = '',
    [string[]]$Suites = @('arena-smoke', 'side-smoke'),
    [ValidateSet('clonar', 'punteros', 'importar', 'build', 'suites')]
    [string]$Desde = 'clonar'
)

$ErrorActionPreference = 'Stop'
$pasos = @('clonar', 'punteros', 'importar', 'build', 'suites')
$inicio = [array]::IndexOf($pasos, $Desde)
$trabajo = Join-Path $Destino '_restauracion'
$resumenPath = Join-Path $trabajo 'resumen.json'
$resumen = [ordered]@{}

function Save-Resumen {
    New-Item -ItemType Directory -Force $trabajo | Out-Null
    $resumen | ConvertTo-Json -Depth 6 | Out-File -Encoding utf8 $resumenPath
}

function Invoke-Paso([string]$nombre, [scriptblock]$accion) {
    if ([array]::IndexOf($pasos, $nombre) -lt $inicio) { Write-Host "== $nombre (saltado por -Desde)"; return }
    Write-Host "== $nombre"
    $reloj = [Diagnostics.Stopwatch]::StartNew()
    try {
        $detalle = & $accion
        $resumen[$nombre] = [ordered]@{ resultado = 'ok'; segundos = [math]::Round($reloj.Elapsed.TotalSeconds, 1); detalle = $detalle }
        Save-Resumen
        Write-Host ("   ok en {0:N1} s" -f $reloj.Elapsed.TotalSeconds)
    } catch {
        $resumen[$nombre] = [ordered]@{ resultado = 'fallo'; segundos = [math]::Round($reloj.Elapsed.TotalSeconds, 1); error = "$_" }
        Save-Resumen
        throw "Paso '$nombre' falló: $_"
    }
}

function Invoke-Nativo([string]$exe, [string[]]$argumentos) {
    # Start-Process -Wait espera a Unity hasta que sale de verdad y devuelve su código de salida.
    $p = Start-Process -FilePath $exe -ArgumentList $argumentos -PassThru -Wait -NoNewWindow
    return $p.ExitCode
}

# ---------- 1. requisitos ----------
foreach ($cmd in 'git', 'git-lfs') {
    if (-not (Get-Command $cmd -ErrorAction SilentlyContinue)) { throw "Falta $cmd en PATH." }
}
$resumen['requisitos'] = [ordered]@{
    git = (git --version); gitlfs = (git lfs version)
    maquina = $env:COMPUTERNAME; fecha = (Get-Date).ToString('s'); destino = $Destino; rama = $Rama
}

# ---------- 2. clonar ----------
Invoke-Paso 'clonar' {
    if (Test-Path (Join-Path $Destino '.git')) {
        Write-Host "   $Destino ya es un clon: no se vuelve a descargar."
        return [ordered]@{ reutilizado = $true; commit = (git -C $Destino rev-parse HEAD) }
    }
    if ((Test-Path $Destino) -and (Get-ChildItem $Destino -Force | Where-Object Name -ne '_restauracion')) {
        throw "$Destino existe y no está vacío."
    }
    $env:GIT_LFS_SKIP_SMUDGE = '1'
    $t = [Diagnostics.Stopwatch]::StartNew()
    git clone --branch $Rama --single-branch $Repositorio $Destino | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "git clone salió con $LASTEXITCODE" }
    $tClone = $t.Elapsed.TotalSeconds
    Remove-Item Env:GIT_LFS_SKIP_SMUDGE
    $t.Restart()
    git -C $Destino lfs pull | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "git lfs pull salió con $LASTEXITCODE" }
    $tLfs = $t.Elapsed.TotalSeconds
    $lfsBytes = (Get-ChildItem (Join-Path $Destino '.git\lfs\objects') -Recurse -File | Measure-Object Length -Sum).Sum
    $gitBytes = (Get-ChildItem (Join-Path $Destino '.git') -Recurse -File | Measure-Object Length -Sum).Sum - $lfsBytes
    [ordered]@{
        reutilizado = $false; commit = (git -C $Destino rev-parse HEAD)
        segundosClone = [math]::Round($tClone, 1); segundosLfs = [math]::Round($tLfs, 1)
        mbLfs = [math]::Round($lfsBytes / 1MB, 1); mbGitSinLfs = [math]::Round($gitBytes / 1MB, 1)
    }
}

# ---------- 3. punteros LFS ----------
Invoke-Paso 'punteros' {
    $lista = git -C $Destino lfs ls-files --name-only
    if ($LASTEXITCODE -ne 0) { throw "git lfs ls-files salió con $LASTEXITCODE" }
    $punteros = @()
    foreach ($rel in $lista) {
        $ruta = Join-Path $Destino $rel
        if (-not (Test-Path -LiteralPath $ruta)) { $punteros += "$rel (no existe)"; continue }
        $fs = [IO.File]::OpenRead($ruta)
        try { $buf = New-Object byte[] 40; $n = $fs.Read($buf, 0, 40) } finally { $fs.Dispose() }
        if ([Text.Encoding]::ASCII.GetString($buf, 0, $n).StartsWith('version https://git-lfs')) { $punteros += $rel }
    }
    # Unity no admite LFS por sí solo: cualquier extensión LFS fuera de .gitattributes quedaría sin descargar.
    $prev = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
    $fsck = (git -C $Destino lfs fsck 2>&1 | Out-String).Trim(); $ErrorActionPreference = $prev
    if ($punteros.Count -gt 0) { throw ("Quedan {0} punteros sin descargar: {1}" -f $punteros.Count, ($punteros | Select-Object -First 10) -join ', ') }
    [ordered]@{ archivosLfs = @($lista).Count; punteros = 0; fsck = $fsck }
}

# ---------- Unity ----------
if (-not $Unity) {
    $ver = (Get-Content (Join-Path $Destino 'ProjectSettings\ProjectVersion.txt') | Select-String '^m_EditorVersion:').ToString().Split(':')[1].Trim()
    $Unity = "C:\Program Files\Unity\Hub\Editor\$ver\Editor\Unity.exe"
    $resumen['requisitos']['unityVersion'] = $ver
}
if (-not (Test-Path $Unity)) { throw "No se encuentra Unity en $Unity (instalar la versión de ProjectVersion.txt con Unity Hub)." }
$resumen['requisitos']['unity'] = $Unity

function Test-LogUnity([string]$log) {
    $errores = @(Select-String -Path $log -Pattern 'error CS\d+|Scripts have compiler errors|Compilation failed|Aborting batchmode due to failure' | ForEach-Object { $_.Line.Trim() } | Select-Object -Unique)
    return , $errores
}

# ---------- 4. importar ----------
Invoke-Paso 'importar' {
    if (Test-Path (Join-Path $Destino 'Library')) { throw "Library ya existe: la importación no sería limpia (borrarla o usar -Desde build)." }
    $log = Join-Path $trabajo 'unity-import.log'
    $code = Invoke-Nativo $Unity @('-batchmode', '-quit', '-projectPath', "`"$Destino`"", '-logFile', "`"$log`"")
    $errores = Test-LogUnity $log
    $avisos = @(Select-String -Path $log -Pattern 'warning CS\d+' | ForEach-Object { $_.Line.Trim() } | Select-Object -Unique).Count
    if ($code -ne 0 -or $errores.Count -gt 0) { throw "Importación: salida $code, errores: $($errores -join ' | ')" }
    [ordered]@{ salida = $code; errores = 0; avisosCompilacionUnicos = $avisos; log = $log }
}

# ---------- 5. build ----------
$version = ((Get-Content (Join-Path $Destino 'Assets\CancelBrawlBeta\Runtime\BrawlMatch.cs') | Select-String 'ProductionVersion\s*=\s*"([^"]+)"').Matches[0].Groups[1].Value)
$exe = Join-Path $Destino "Builds\Funados\$version\Windows\FUNADOS.exe"
Invoke-Paso 'build' {
    $log = Join-Path $trabajo 'unity-build.log'
    $code = Invoke-Nativo $Unity @('-batchmode', '-quit', '-projectPath', "`"$Destino`"", '-executeMethod', 'CancelBrawl.Beta.Editor.FunadosProjectBuilder.BuildWindows', '-logFile', "`"$log`"")
    $errores = Test-LogUnity $log
    $ok = Select-String -Path $log -Pattern 'FUNADOS_BUILD_SUCCEEDED' -Quiet
    $info = Join-Path $Destino "Evidence\Production-$version\build.txt"
    $buildTxt = if (Test-Path $info) { (Get-Content $info) -join '; ' } else { '' }
    if ($code -ne 0 -or -not $ok -or -not (Test-Path $exe)) { throw "Build: salida $code, éxito=$ok, exe=$(Test-Path $exe), errores: $($errores -join ' | ')" }
    [ordered]@{ salida = $code; version = $version; exe = $exe; buildTxt = $buildTxt; mbCarpeta = [math]::Round(((Get-ChildItem (Split-Path $exe) -Recurse -File | Measure-Object Length -Sum).Sum / 1MB), 1) }
}

# ---------- 6. suites ----------
Invoke-Paso 'suites' {
    $res = [ordered]@{}
    $fallos = 0
    foreach ($s in $Suites) {
        $dir = Join-Path $trabajo "suite-$s"
        if (Test-Path $dir) { Remove-Item -Recurse -Force $dir }
        New-Item -ItemType Directory -Force $dir | Out-Null
        $t = [Diagnostics.Stopwatch]::StartNew()
        $code = Invoke-Nativo $exe @("-$s", '-isolate-physical-pads', '-evidence-path', "`"$dir`"", '-options-path', "`"$dir\player-options.json`"",
            '-force-d3d11', '-screen-width', '1280', '-screen-height', '720', '-screen-fullscreen', '0', '-logFile', "`"$dir\player.log`"")
        $rep = Get-ChildItem $dir -Filter '*report.json' | Select-Object -First 1
        $j = if ($rep) { Get-Content $rep.FullName -Raw | ConvertFrom-Json } else { $null }
        $pasa = ($code -eq 0) -and $j -and $j.passed
        if (-not $pasa) { $fallos++ }
        $res[$s] = [ordered]@{
            pasa = [bool]$pasa; salida = $code; segundos = [math]::Round($t.Elapsed.TotalSeconds, 1)
            comprobaciones = if ($j) { @($j.checks).Count } else { 0 }; fallos = if ($j) { @($j.failures).Count } else { -1 }
            informe = if ($rep) { $rep.FullName } else { '' }
        }
        Write-Host ("   {0}: {1} ({2} ok / {3} fallos)" -f $s, ($(if ($pasa) { 'PASA' } else { 'FALLA' })), $res[$s].comprobaciones, $res[$s].fallos)
    }
    if ($fallos -gt 0) { $resumen['suitesDetalle'] = $res; throw "$fallos suite(s) fallaron" }
    $res
}

Write-Host "Restauración completa. Resumen: $resumenPath"
