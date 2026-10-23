"""Historial de completados"""
import json
from pathlib import Path
from typing import List, Dict, Any

class History:
    def __init__(self):
        self.root = Path(__file__).resolve().parent.parent
        self.file = self.root / "history.json"
        self.items: List[Dict[str, Any]] = []
        self._load()

    def _load(self):
        try:
            if self.file.exists():
                with open(self.file, 'r', encoding='utf-8') as f:
                    data = json.load(f)
                    if isinstance(data, list):
                        self.items = data[-200:]
        except Exception:
            pass

    def add(self, name: str, status: str = "OK", duration_s: int = 0):
        import time
        entry = {
            "name": name,
            "status": status,
            "duration_s": duration_s,
            "timestamp": int(time.time())
        }
        self.items.append(entry)
        self._save()

    def _save(self):
        try:
            with open(self.file, 'w', encoding='utf-8') as f:
                json.dump(self.items, f, indent=2, ensure_ascii=False)
        except Exception:
            pass

    def stats(self):
        ok = sum(1 for x in self.items if x.get("status") == "OK")
        fail = sum(1 for x in self.items if x.get("status") != "OK")
        last = self.items[-1] if self.items else None
        return {"ok": ok, "fail": fail, "total": len(self.items), "last": last}

    def recent(self, n: int = 5):
        return list(reversed(self.items[-n:]))
