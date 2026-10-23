"""Persistencia session.json con reanudación"""
import json
import os
from pathlib import Path
from typing import Dict, Any, List

class SessionManager:
    def __init__(self):
        self.root = Path(__file__).resolve().parent.parent
        self.session_file = self.root / "session.json"
        self.history_file = self.root / "history.json"
        self.session: Dict[str, Any] = {
            "version": "3.1.8-pro",
            "active_profile": "default",
            "jobs": [],
            "queue_config": {"max_active": 3},
            "sections": {"regras": True, "salida": True, "avanzado": True, "rendimiento": True, "cola": True, "historial": True, "sidebar": True},
            "settings": {"use_gpu": True, "turbo": True, "threads": 8},
            "timestamp": 0
        }
        self.history: List[Dict[str, Any]] = []
        self._load()

    def _load(self):
        try:
            if self.session_file.exists():
                with open(self.session_file, 'r', encoding='utf-8') as f:
                    data = json.load(f)
                    if isinstance(data, dict):
                        self.session.update({k: v for k, v in data.items() if k in self.session or True})
        except Exception:
            pass
        try:
            if self.history_file.exists():
                with open(self.history_file, 'r', encoding='utf-8') as f:
                    data = json.load(f)
                    if isinstance(data, list):
                        self.history = data[-100:]
        except Exception:
            pass

    def save(self):
        try:
            import time
            self.session["timestamp"] = int(time.time())
            with open(self.session_file, 'w', encoding='utf-8') as f:
                json.dump(self.session, f, indent=2, ensure_ascii=False)
        except Exception:
            pass

    def save_history(self):
        try:
            with open(self.history_file, 'w', encoding='utf-8') as f:
                json.dump(self.history, f, indent=2, ensure_ascii=False)
        except Exception:
            pass

    def has_pending_session(self) -> bool:
        jobs = self.session.get("jobs", [])
        return len(jobs) > 0

    def get_pending_jobs(self):
        return self.session.get("jobs", [])

    def set_pending_jobs(self, jobs_list: List[Dict[str, Any]]):
        self.session["jobs"] = jobs_list
        self.save()

    def clear_pending_jobs(self):
        self.session["jobs"] = []
        self.save()

    def add_history_item(self, item: Dict[str, Any]):
        import time
        item["timestamp"] = int(time.time())
        self.history.append(item)
        self.save_history()
