"""Sistema de perfiles y presets de calidad"""
import json
import os
from pathlib import Path
from typing import Dict, Any

class PresetManager:
    def __init__(self):
        self.root = Path(__file__).resolve().parent.parent
        self.profiles_dir = self.root / "profiles"
        self.profiles_dir.mkdir(exist_ok=True)
        self.current_profile = "default"
        self.profiles: Dict[str, Dict[str, Any]] = {
            "default": {
                "name": "Español · Dual Audio",
                "format": "mp4",
                "quality": "Alta",
                "video_codec": "h264",
                "video_preset_cpu": "medium",
                "video_preset_gpu": "p5",
                "crf": 18,
                "audio_bitrate": "256k",
                "audio_codec": "aac",
                "resolution": "origen",
                "fps": "origen",
                "dual_audio": True,
                "subtitles": "copy"
            },
            "h264_streaming": {
                "name": "H.264_STREAMING",
                "format": "mp4",
                "quality": "Media",
                "video_codec": "h264",
                "video_preset_cpu": "veryfast",
                "video_preset_gpu": "p4",
                "crf": 23,
                "audio_bitrate": "128k",
                "audio_codec": "aac",
                "resolution": "720p",
                "fps": "origen",
                "dual_audio": True,
                "subtitles": "copy"
            },
            "hevc_high": {
                "name": "HEVC ALTA CALIDAD",
                "format": "mkv",
                "quality": "Alta",
                "video_codec": "hevc",
                "video_preset_cpu": "medium",
                "video_preset_gpu": "p5",
                "crf": 20,
                "audio_bitrate": "256k",
                "audio_codec": "aac",
                "resolution": "origen",
                "fps": "origen",
                "dual_audio": True,
                "subtitles": "copy"
            }
        }
        self._load_profiles()

    def _load_profiles(self):
        try:
            for name in list(self.profiles.keys()):
                path = self.profiles_dir / f"{name}.json"
                if path.exists():
                    with open(path, 'r', encoding='utf-8') as f:
                        data = json.load(f)
                        if isinstance(data, dict):
                            self.profiles[name].update(data)
            for path in self.profiles_dir.glob("*.json"):
                name = path.stem
                if name not in self.profiles:
                    with open(path, 'r', encoding='utf-8') as f:
                        data = json.load(f)
                        if isinstance(data, dict):
                            self.profiles[name] = data
        except Exception:
            pass

    def save_profiles(self):
        try:
            for name, data in self.profiles.items():
                path = self.profiles_dir / f"{name}.json"
                with open(path, 'w', encoding='utf-8') as f:
                    json.dump(data, f, indent=2, ensure_ascii=False)
        except Exception:
            pass

    def get_active(self) -> Dict[str, Any]:
        return self.profiles.get(self.current_profile, self.profiles["default"]).copy()

    def list_profiles(self):
        return [(k, v.get("name", k)) for k, v in self.profiles.items()]

    def set_active(self, name: str):
        if name in self.profiles:
            self.current_profile = name
