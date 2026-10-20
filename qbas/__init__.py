"""QBASCONVERTER v3.1.8-pro - XPRESS HYBRID CORE"""
__version__ = "3.1.8-pro"
__engine__ = "XPRESS_HYBRID_CORE"
__author__ = "QBASWING"

from . import hardware, ui, presets, convert, automation, queue, telemetry, session, history, sidebar, cli

__all__ = [
    "hardware", "ui", "presets", "convert", "automation", "queue",
    "telemetry", "session", "history", "sidebar", "cli"
]
