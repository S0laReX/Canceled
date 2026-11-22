"""Genera FUNADOS_GDD_2.0_checklist.docx: copia del GDD 2.0 con las casillas de la sección 15 actualizadas.

Uso: python -I marcar_checklist_gdd.py
Lee las tareas marcadas [x] en CHECKLIST_2p5D.md (misma redacción que el GDD) y cambia ☐ por ☒ en esas
líneas del documento. Solo toca el carácter de la casilla y el recuento; el original no se modifica.
"""
import os
import re
import sys
import zipfile

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SOURCE = os.path.join(HERE, 'FUNADOS_GDD_2.0.docx')
TARGET = os.path.join(HERE, 'FUNADOS_GDD_2.0_checklist.docx')
CHECKLIST = os.path.join(ROOT, 'CHECKLIST_2p5D.md')
EMPTY, DONE = '☐', '☒'


def checklist_items():
    done, total = [], 0
    for line in open(CHECKLIST, encoding='utf8'):
        m = re.match(r'- \[( |x)\] (?:⧗ )?(.+)$', line.rstrip())
        if not m:
            continue
        total += 1
        if m.group(1) == 'x':
            done.append(m.group(2).strip())
    return done, total


def paragraph_text(p):
    return ''.join(re.findall(r'<w:t[^>]*>([^<]*)</w:t>', p))


def main():
    sys.stdout.reconfigure(encoding='utf-8')
    done, total = checklist_items()
    with zipfile.ZipFile(SOURCE) as z:
        xml = z.read('word/document.xml').decode('utf8')
        files = {n: z.read(n) for n in z.namelist()}
    unmatched = set(done)
    marked = 0

    def fix(match):
        nonlocal marked
        p = match.group(0)
        text = paragraph_text(p)
        if not text.startswith((EMPTY, DONE)):
            return p
        body = text[1:].strip()
        if body in done:
            unmatched.discard(body)
            marked += 1
            return p.replace(EMPTY, DONE, 1)
        return p

    xml = re.sub(r'<w:p[ >].*?</w:p>', fix, xml, flags=re.S)
    xml = re.sub(r'De 63 tareas, \d+ están implementadas \(marcadas\) y \d+ pendientes\.',
                 f'De {total} tareas, {marked} están implementadas (marcadas) y {total - marked} pendientes.', xml)
    files['word/document.xml'] = xml.encode('utf8')
    with zipfile.ZipFile(TARGET, 'w', zipfile.ZIP_DEFLATED) as out:
        for name, data in files.items():
            out.writestr(name, data)
    print(f'marcadas {marked} de {total}; sin coincidencia en el GDD: {len(unmatched)}')
    for item in sorted(unmatched):
        print('  -', item)


if __name__ == '__main__':
    main()
