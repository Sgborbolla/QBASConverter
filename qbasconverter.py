#!/usr/bin/env python3
"""
QBASCONVERTER v3.1.8-pro (XPRESS HYBRID CORE)
Punto de entrada principal
"""
import os
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT))

from qbas.cli import Dashboard

def main():
    print("Iniciando QBASCONVERTER v3.1.8-pro (XPRESS HYBRID CORE)...")
    app = Dashboard()
    app.run()

if __name__ == "__main__":
    main()
