# QBASCONVERTER v3.1.8-pro (XPRESS HYBRID CORE)

Convertidor de audio/video con Dashboard Terminal Avanzado.

## Características
- Dashboard TUI con panel lateral
- Soporte NVENC_CUDA (NVIDIA) + CPU x264/x265
- Cola paralela configurable (1-8 activos)
- Sistema de perfiles JSON
- Reglas XPRESS ENGINE
- Métricas en tiempo real (GPU/CPU/RAM)
- Matriz de afinidad C1-C16
- Persistencia session.json con reanudación
- Panel Discos & Red (NTFS/SMB)
- Historial de conversiones

## Ejecución
```bash
python qbasconverter.py
```

## Compilar
```bash
pip install pyinstaller
pyinstaller build/qbasconverter.spec
```
