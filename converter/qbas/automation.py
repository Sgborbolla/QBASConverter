"""XPRESS ENGINE - Reglas de automatización"""
import os
import re
from pathlib import Path
from typing import Dict, Any

class XpressEngine:
    def __init__(self):
        self.rules = {
            "mantener_carpeta": True,
            "sin_mostrar_ventana": True,
            "subtitulos_forzados": True,
            "incluir_nombre_perfil": True,
            "auto_detectar_tipo": True,
            "usar_ruta_origen": True,
            "remux_si_posible": True,
            "crear_subcarpeta_convertidos": True
        }
        self.subcarpeta = "convertidos"
        self.patterns = {
            "novela": re.compile(r'#\s*cap\s*novela', re.I),
            "serie": re.compile(r'#\s*cap\s*serie', re.I)
        }

    def aplicar_reglas(self, input_path: str, profile: Dict[str, Any], output_dir: str = None) -> Dict[str, Any]:
        in_path = Path(input_path)
        result = {}
        if self.rules["usar_ruta_origen"] and not output_dir:
            if self.rules["crear_subcarpeta_convertidos"]:
                out_dir = in_path.parent / self.subcarpeta
            else:
                out_dir = in_path.parent
        else:
            out_dir = Path(output_dir) if output_dir else in_path.parent
        out_dir.mkdir(parents=True, exist_ok=True)
        base_name = in_path.stem
        ext = profile.get("format", "mp4")
        if self.rules["incluir_nombre_perfil"]:
            perfil = profile.get("name", "").replace(" ", "_").replace("·", "-")
            if perfil:
                base_name = f"{base_name}_[{perfil}]"
        if self.rules["auto_detectar_tipo"]:
            try:
                with open(in_path.with_suffix('.txt'), 'r', encoding='utf-8', errors='ignore') as f:
                    txt = f.read()
                    if self.patterns["novela"].search(txt):
                        result["tipo_contenido"] = "NOVELAS"
                    elif self.patterns["serie"].search(txt):
                        result["tipo_contenido"] = "SERIES"
            except:
                pass
        result["output_dir"] = str(out_dir)
        result["output_file"] = str(out_dir / f"{base_name}.{ext}")
        result["mantener_carpeta"] = self.rules["mantener_carpeta"]
        result["subtitulos_forzados"] = self.rules["subtitulos_forzados"]
        return result

    def toggle(self, rule: str):
        if rule in self.rules:
            self.rules[rule] = not self.rules[rule]
            return self.rules[rule]
        return None

    def get_estado(self):
        activos = sum(1 for v in self.rules.values() if v)
        return f"{activos}/{len(self.rules)} ACTIVAS"
