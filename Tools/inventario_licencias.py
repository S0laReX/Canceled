"""Inventario reproducible de procedencia y licencias de FUNADOS (SYS-017, D04).

Uso (desde la raíz del repositorio):
    python -I Tools/inventario_licencias.py [--package-cache <Library/PackageCache>]

Genera en Docs/Licencias/:
  - inventario_activos.csv  : cada modelo, textura, concepto, shader y binario de Assets/ y Backups/
                              con tamaño, SHA256, herramienta detectada en el propio archivo
                              (cabecera FBX, XMP de PNG) y estado de licencia.
  - inventario_paquetes.csv : cada paquete de Packages/packages-lock.json con versión, origen,
                              licencia leída de su LICENSE.md y avisos de terceros.

Solo lee archivos; no cambia nada del proyecto. Las decisiones de licencia (D04) son del usuario:
el estado "pendiente-usuario" significa que la evidencia del archivo no basta para confirmarla.
"""
import argparse
import csv
import hashlib
import json
import os
import re
import struct
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), os.pardir))
OUT = os.path.join(ROOT, "Docs", "Licencias")

MESHY_PENDING = ("Términos de Meshy según el plan de la cuenta al generar (Free: CC BY 4.0 con "
                 "atribución a Meshy; de pago: según sus términos) o del autor si se bajó de la comunidad")


def sha256(path):
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for block in iter(lambda: f.read(1 << 20), b""):
            h.update(block)
    return h.hexdigest().upper()


def fbx_tool(data):
    m = re.search(rb"Blender \(stable FBX IO\) - ([\w .]+?) - ", data)
    if m:
        return "Blender FBX IO " + m.group(1).decode("latin1").strip()
    if b"FBX SDK" in data:
        return "Autodesk FBX SDK"
    return "desconocida"


def png_xmp(data):
    """Devuelve (sistema, content_id, fecha) del XMP de Meshy si existe."""
    i = data.find(b"<x:xmpmeta")
    if i < 0:
        return None
    xmp = data[i:data.find(b"</x:xmpmeta>", i)].decode("utf-8", "replace")
    get = lambda k: (re.search(k + r'="([^"]*)"', xmp) or [None, ""])[1]
    return get("meshy:System"), get("meshy:ContentID"), get("xmp:CreateDate"), get("dc:publisher")


def png_size(data):
    if data[:8] != b"\x89PNG\r\n\x1a\n":
        return "no es PNG (" + data[:4].hex() + ")"
    w, h = struct.unpack(">II", data[16:24])
    return f"{w}x{h}"


def classify(rel, data):
    """(categoría, origen, herramienta, licencia, estado, nota)."""
    low = rel.lower().replace("\\", "/")
    ext = os.path.splitext(low)[1]
    if low.startswith("assets/funadosproduction/validation/"):
        return ("validación", "propio (cubo de 1 m para escala)", fbx_tool(data),
                "propio del proyecto", "confirmado", "")
    if low.startswith("assets/tutorialinfo/") or low.startswith("assets/scenes/samplescene"):
        return ("plantilla", "plantilla URP de Unity", "Unity", "Unity (contenido de plantilla)",
                "confirmado", "no se incluye en la build (no está en la lista de escenas)")
    if low.startswith("backups/"):
        return ("respaldo", "copia local previa (código, ajustes, evidencia)", "zip",
                "propio del proyecto", "confirmado", "no entra en la build")
    if ext == ".shader":
        return ("shader", "propio (FUNADOS Toon Starter URP)", "texto", "propio del proyecto",
                "confirmado", "usa include de URP (Unity Companion License)")
    if "/art/characters/" in low:
        tool = fbx_tool(data) if ext == ".fbx" else png_size(data)
        return ("personaje", "Meshy (preexistente en la beta local 0.3)", tool, MESHY_PENDING,
                "pendiente-usuario", "sin XMP ni ID de tarea de Meshy en el archivo")
    if "/environments/" in low:
        if "/concept/" in low and "unitypreview" in low:
            return ("escenario-captura", "captura propia del Editor Unity", png_size(data),
                    "propio del proyecto", "confirmado", "")
        if "/concept/" in low:
            x = png_xmp(data)
            if x:
                system, cid, date, pub = x
                return ("escenario-concepto", f"{pub} texto/imagen a imagen ({system})", png_size(data),
                        MESHY_PENDING, "pendiente-usuario", f"XMP Meshy ContentID={cid} fecha={date}")
            return ("escenario-concepto", "desconocido", png_size(data), "desconocida", "desconocido", "")
        tool = fbx_tool(data) if ext == ".fbx" else png_size(data)
        return ("escenario-modelo", "Meshy imagen a 3D desde el concepto de la pieza", tool, MESHY_PENDING,
                "pendiente-usuario", "exportado a FBX por la canalización de Meshy (Blender 4.4)")
    return ("otro", "desconocido", "", "desconocida", "desconocido", "")


def assets_rows():
    exts = (".fbx", ".png", ".jpg", ".tga", ".psd", ".blend", ".wav", ".ogg", ".mp3",
            ".ttf", ".otf", ".shader", ".shadergraph", ".hlsl", ".zip")
    rows = []
    for top in ("Assets", "Backups"):
        for dirpath, _, files in os.walk(os.path.join(ROOT, top)):
            for name in files:
                if not name.lower().endswith(exts):
                    continue
                path = os.path.join(dirpath, name)
                rel = os.path.relpath(path, ROOT).replace("\\", "/")
                with open(path, "rb") as f:
                    data = f.read()
                if data.startswith(b"version https://git-lfs"):
                    sys.exit(f"{rel} es un puntero LFS: ejecutar `git lfs pull` antes del inventario.")
                cat, origin, tool, lic, state, note = classify(rel, data)
                rows.append([rel, cat, len(data), sha256(path), origin, tool, lic, state, note])
    rows.sort()
    return rows


def license_of(pkg_dir):
    for name in ("LICENSE.md", "LICENSE", "LICENSE.txt"):
        p = os.path.join(pkg_dir, name)
        if os.path.exists(p):
            text = open(p, encoding="utf-8", errors="replace").read()
            for pattern, label in (
                (r"Unity Companion (Package )?License", "Unity Companion License"),
                (r"Unity Package Distribution License", "Unity Package Distribution License"),
                (r"Unity Terms of Service", "Unity Terms of Service"),
                (r"subject to, and made available under, the Unity", "Términos de Unity (ToS / servicios IA)"),
                (r"MIT License", "MIT"),
            ):
                if re.search(pattern, text):
                    return label
            return "ver " + name
    return ""


def third_party(pkg_dir):
    for name in os.listdir(pkg_dir):
        if name.lower().startswith("third party notices"):
            text = open(os.path.join(pkg_dir, name), encoding="utf-8", errors="replace").read()
            comps = re.findall(r"Component Name:\s*(.+)", text)
            lics = re.findall(r"License Type:\s*(.+)", text)
            pairs = [f"{c.strip()} ({l.strip()})" for c, l in zip(comps, lics)]
            return "; ".join(pairs[:8]) + (" …" if len(pairs) > 8 else "") if pairs else "ver " + name
    return ""


def package_rows(cache):
    lock = json.load(open(os.path.join(ROOT, "Packages", "packages-lock.json"), encoding="utf-8"))
    direct = json.load(open(os.path.join(ROOT, "Packages", "manifest.json"), encoding="utf-8"))["dependencies"]
    dirs = {}
    if cache and os.path.isdir(cache):
        for d in os.listdir(cache):
            dirs[d.split("@")[0]] = os.path.join(cache, d)
    rows = []
    for name, info in sorted(lock["dependencies"].items()):
        d = dirs.get(name)
        if name.startswith("com.unity.modules."):
            lic, tp, state = "módulo integrado del motor (licencia del Editor/Runtime de Unity)", "", "confirmado"
        else:
            lic = license_of(d) if d else ""
            tp = third_party(d) if d else ""
            state = "confirmado" if lic else "desconocido"
        if info.get("version", "").endswith(("-pre.2", "-exp.1")) or "-exp" in info.get("version", "") or "-pre" in info.get("version", ""):
            note = "preliminar/experimental: no apto para producción sin revisar"
        else:
            note = ""
        rows.append([name, info.get("version"), "directa" if name in direct else f"transitiva (prof. {info.get('depth')})",
                     info.get("source"), lic, tp, state, note])
    return rows


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--package-cache", default=os.path.join(ROOT, "Library", "PackageCache"))
    args = ap.parse_args()
    os.makedirs(OUT, exist_ok=True)
    a = assets_rows()
    with open(os.path.join(OUT, "inventario_activos.csv"), "w", newline="", encoding="utf-8") as f:
        w = csv.writer(f)
        w.writerow(["ruta", "categoria", "bytes", "sha256", "origen", "herramienta_o_formato", "licencia", "estado", "nota"])
        w.writerows(a)
    p = package_rows(args.package_cache)
    with open(os.path.join(OUT, "inventario_paquetes.csv"), "w", newline="", encoding="utf-8") as f:
        w = csv.writer(f)
        w.writerow(["paquete", "version", "dependencia", "fuente", "licencia", "avisos_terceros", "estado", "nota"])
        w.writerows(p)
    from collections import Counter
    print("activos:", len(a), dict(Counter(r[7] for r in a)))
    print("paquetes:", len(p), dict(Counter(r[6] for r in p)))


if __name__ == "__main__":
    main()
