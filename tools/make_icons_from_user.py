#!/usr/bin/env python3
import os, sys
from pathlib import Path
from PIL import Image

base = Path(r"C:\Users\FREEMAN\Documents\QBASConverter")
outdir = base / "build" / "iconos"
outdir.mkdir(parents=True, exist_ok=True)

# Usar las imágenes que me pasaste
imgs = [
    base / "Ícono QBASConverter.jpg",  # el que tiene nombre - el que más te gusta
    base / "1e192227d-a45d-49b2-bb6a-0b4396f87849.png",  # multicopiador a la derecha (más grande)
    base / "1533cd4d9-bba7-428b-8ad3-47e6f1e7d414.png",  # convertidor
]

for i, p in enumerate(imgs):
    if p.exists():
        print(f"Usando: {p.name}")
    else:
        print(f"NO ENCONTRADO: {p}")

# Icono para el multicopiador (más grande) - usar el segundo
ico_multi = outdir / "QBASFREEMAN.ico"
if (base / "1e192227d-a45d-49b2-bb6a-0b4396f87849.png").exists():
    img = Image.open(base / "1e192227d-a45d-49b2-bb6a-0b4396f87849.png").convert('RGBA')
else:
    img = Image.open(base / "Ícono QBASConverter.jpg").convert('RGBA')

sizes = [(16,16),(32,32),(48,48),(64,64),(128,128),(256,256)]
img.save(ico_multi, format='ICO', sizes=sizes)
print(f"Creado: {ico_multi}")

# Icono para el convertidor
ico_conv = outdir / "QBASFREEMAN CONVERTER.ico"
if (base / "1533cd4d9-bba7-428b-8ad3-47e6f1e7d414.png").exists():
    img2 = Image.open(base / "1533cd4d9-bba7-428b-8ad3-47e6f1e7d414.png").convert('RGBA')
else:
    img2 = Image.open(base / "Ícono QBASConverter.jpg").convert('RGBA')
img2.save(ico_conv, format='ICO', sizes=sizes)
print(f"Creado: {ico_conv}")

# Copiar a raíz para fácil acceso
import shutil
shutil.copy(ico_multi, base / "QBASFREEMAN.ico")
shutil.copy(ico_conv, base / "QBASFREEMAN CONVERTER.ico")
print("Copiados a raíz")
