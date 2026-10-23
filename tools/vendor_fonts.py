"""Descarga y vendoriza las fuentes de Google Fonts para uso OFFLINE.

Genera www/assets/fonts/fonts.css con rutas locales.
Solo se conservan los subsets latin y latin-ext (suficiente para español).
"""
import re
import sys
import urllib.request
from pathlib import Path

UA = ("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 "
      "(KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36")

FAMILIES = [
    ("Geist", "https://fonts.googleapis.com/css2?family=Geist:wght@100..900&display=swap"),
    ("Geist Mono", "https://fonts.googleapis.com/css2?family=Geist+Mono:wght@100..900&display=swap"),
    ("Inter", "https://fonts.googleapis.com/css2?family=Inter:wght@100..900&display=swap"),
    ("JetBrains Mono", "https://fonts.googleapis.com/css2?family=JetBrains+Mono:wght@100..800&display=swap"),
    ("Material Symbols Outlined", "https://fonts.googleapis.com/css2?family=Material+Symbols+Outlined:opsz,wght,FILL,GRAD@20..48,100..700,0..1,-50..200&display=swap"),
]

KEEP = {"latin", "latin-ext"}


def get(url: str) -> str:
    req = urllib.request.Request(url, headers={"User-Agent": UA})
    with urllib.request.urlopen(req, timeout=60) as r:
        return r.read().decode("utf-8")


def slug(name: str) -> str:
    return re.sub(r"[^a-z0-9]+", "-", name.lower()).strip("-")


def main() -> int:
    root = Path(sys.argv[1]) if len(sys.argv) > 1 else Path("www/assets/fonts")
    root.mkdir(parents=True, exist_ok=True)
    out_css: list[str] = [
        "/* Fuentes vendorizadas para uso offline. Generado por tools/vendor_fonts.py.",
        "   Inter y JetBrains Mono: SIL Open Font License 1.1.",
        "   Material Symbols Outlined: Apache License 2.0 (Google Material Design Icons). */",
        "",
    ]
    for name, url in FAMILIES:
        css = get(url)
        # Cada bloque va precedido de un comentario /* subset */
        blocks = re.findall(r"/\*\s*([a-z0-9\-]+)\s*\*/\s*(@font-face\s*\{.*?\})", css, re.S)
        if not blocks:
            # Las fuentes variables sin subconjuntos (Material Symbols) vienen
            # en un solo bloque con el comentario "fallback".
            blocks = [("fallback", b) for b in re.findall(r"(@font-face\s*\{.*?\})", css, re.S)]
        kept = 0
        for subset, block in blocks:
            if subset not in KEEP and subset != "fallback":
                continue
            m = re.search(r"url\((https://fonts\.gstatic\.com/[^)]+\.woff2)\)", block)
            if not m:
                continue
            src = m.group(1)
            ext = f"{slug(name)}-{subset}.woff2"
            dest = root / ext
            if not dest.exists():
                req = urllib.request.Request(src, headers={"User-Agent": UA})
                with urllib.request.urlopen(req, timeout=60) as r:
                    dest.write_bytes(r.read())
            block = block.replace(src, f"./{ext}")
            block = re.sub(r"font-display:\s*swap;", "font-display: block;", block)
            out_css.append(block)
            out_css.append("")
            kept += 1
        print(f"{name}: {kept} subsets -> {root}")

    (root / "fonts.css").write_text("\n".join(out_css), encoding="utf-8")
    print("OK ->", root / "fonts.css")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
