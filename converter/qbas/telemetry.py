"""Métricas en tiempo real"""
import time
import os
from typing import Dict, Any

class Telemetry:
    def __init__(self):
        self.start_time = time.time()
        self.frame_count = 0
        self.last_update = time.time()
        self.pid = os.getpid()

    def uptime_seconds(self) -> int:
        return int(time.time() - self.start_time)

    def uptime_str(self) -> str:
        s = self.uptime_seconds()
        h, r = divmod(s, 3600)
        m, sec = divmod(r, 60)
        return f"{h:02d}:{m:02d}:{sec:02d}"

    def tick(self):
        self.frame_count += 1

    def metrics(self, hardware, queue) -> Dict[str, Any]:
        qs = queue.get_summary() if queue else {}
        return {
            "pid": self.pid,
            "uptime": self.uptime_str(),
            "uptime_s": self.uptime_seconds(),
            "gpu_load": hardware.gpu_load,
            "gpu_temp": hardware.gpu_temp,
            "gpu_name": hardware.gpu_name,
            "ram_used": hardware.ram_used_mb,
            "ram_total": hardware.ram_total_mb,
            "ram_pct": round((hardware.ram_used_mb / hardware.ram_total_mb * 100), 1) if hardware.ram_total_mb > 0 else 0,
            "cores": hardware.cores_logical,
            "ffmpeg": hardware.ffmpeg_version,
            "queue": qs,
            "completed": qs.get("completed", 0),
            "failed": qs.get("failed", 0),
            "running": qs.get("running", 0)
        }
