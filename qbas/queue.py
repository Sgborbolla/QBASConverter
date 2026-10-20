"""Cola paralela con control de hilos/activos"""
import threading
import time
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
from typing import Dict, Any, List

class QueueJob:
    def __init__(self, input_file: str, output_file: str, profile: Dict[str, Any]):
        self.id = id(self)
        self.input_file = input_file
        self.output_file = output_file
        self.profile = profile
        self.status = "pending"
        self.progress = 0.0
        self.speed = 0.0
        self.eta_seconds = 0
        self.start_time = None
        self.end_time = None
        self.duration_total = 0.0
        self.error = None

class ProcessingQueue:
    def __init__(self, converter, max_active: int = 3):
        self.converter = converter
        self.max_active = max(1, min(max_active, 8))
        self.jobs: List[QueueJob] = []
        self.lock = threading.RLock()
        self.executor = ThreadPoolExecutor(max_workers=self.max_active)
        self.running = False
        self.paused = False
        self.completed = 0
        self.failed = 0
        self._stop_event = threading.Event()

    def add_job(self, input_file: str, output_file: str, profile: Dict[str, Any]):
        with self.lock:
            job = QueueJob(input_file, output_file, profile)
            self.jobs.append(job)
            return job

    def add_batch(self, files: List[str], out_dir: str, profile: Dict[str, Any], automation):
        added = []
        for f in files:
            res = automation.aplicar_reglas(f, profile)
            job = self.add_job(f, res["output_file"], profile)
            added.append(job)
        return added

    def start(self):
        if self.running and not self.paused:
            return
        self.running = True
        self.paused = False
        self._stop_event.clear()
        with self.lock:
            for job in self.jobs:
                if job.status == "pending" and not self._stop_event.is_set():
                    running = sum(1 for j in self.jobs if j.status == "running")
                    if running >= self.max_active:
                        break
                    self._submit_job(job)

    def _submit_job(self, job: QueueJob):
        job.status = "running"
        job.start_time = time.time()
        try:
            job.duration_total = self.converter.get_duration(job.input_file) or 0
        except:
            job.duration_total = 0
        self.executor.submit(self._process, job)

    def _process(self, job: QueueJob):
        def cb(pct):
            with self.lock:
                job.progress = pct
                if job.start_time and job.duration_total > 0 and pct > 0:
                    elapsed = time.time() - job.start_time
                    if pct >= 1 and elapsed > 0:
                        speed_est = job.duration_total / elapsed
                        job.speed = round(speed_est, 2)
                        remaining = job.duration_total * (100 - pct) / 100
                        job.eta_seconds = int(remaining / speed_est) if speed_est > 0 else 0
        try:
            ok = self.converter.run(job.input_file, job.output_file, job.profile, threads=8, use_gpu=True, turbo=True, progress_callback=cb)
            with self.lock:
                job.end_time = time.time()
                if ok:
                    job.status = "completed"
                    job.progress = 100.0
                    self.completed += 1
                else:
                    job.status = "failed"
                    self.failed += 1
            self._auto_next()
        except Exception as e:
            with self.lock:
                job.status = "failed"
                job.error = str(e)
                self.failed += 1
            self._auto_next()

    def _auto_next(self):
        if self._stop_event.is_set() or self.paused:
            return
        with self.lock:
            running = sum(1 for j in self.jobs if j.status == "running")
            if running < self.max_active:
                for job in self.jobs:
                    if job.status == "pending":
                        self._submit_job(job)
                        break

    def pause(self):
        self.paused = True
        with self.lock:
            for j in self.jobs:
                if j.status == "running":
                    j.status = "paused"

    def resume(self):
        self.paused = False
        self.start()

    def stop(self):
        self._stop_event.set()
        self.running = False
        self.paused = False
        with self.lock:
            for j in self.jobs:
                if j.status in ["pending", "running"]:
                    j.status = "stopped"

    def clear_completed(self):
        with self.lock:
            self.jobs = [j for j in self.jobs if j.status not in ["completed", "failed", "stopped"]]
            self.completed = 0
            self.failed = 0

    def get_summary(self):
        with self.lock:
            pending = sum(1 for j in self.jobs if j.status == "pending")
            running = sum(1 for j in self.jobs if j.status == "running")
            paused = sum(1 for j in self.jobs if j.status == "paused")
            completed = sum(1 for j in self.jobs if j.status == "completed")
            failed = sum(1 for j in self.jobs if j.status == "failed")
            return {"pending": pending, "running": running, "paused": paused, "completed": completed, "failed": failed, "total": len(self.jobs)}

    def get_queue_view(self, limit: int = 6):
        with self.lock:
            view = []
            for job in self.jobs[:limit]:
                name = Path(job.input_file).name
                if len(name) > 38:
                    name = name[:35] + "..."
                view.append({"name": name, "status": job.status, "progress": job.progress, "speed": job.speed, "eta": job.eta_seconds})
            return view
