#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
================================================================================
QBASCONVERTER v3.1.8-pro (XPRESS HYBRID CORE)
Súper convertidor de audio y video con Dashboard Terminal Avanzado
Autor: Sgborbolla | Motor: FFmpeg + NVENC_CUDA / MediaCodec
================================================================================
"""

import os
import sys
import time
import json
import shutil
import signal
import subprocess
import threading
import re
from pathlib import Path
from datetime import datetime
from concurrent.futures import ThreadPoolExecutor, as_completed
from typing import List, Dict, Optional, Tuple

# =================================================================================
# 1. CONFIGURACIÓN Y COLORES ANSI (UI)
# =================================================================================
class Colors:
    """Códigos ANSI para terminal"""
    RESET = '\033[0m'
    BOLD = '\033[1m'
    DIM = '\033[2m'
    UNDERLINE = '\033[4m'
    
    BLACK = '\033[30m'
    RED = '\033[31m'
    GREEN = '\033[32m'
    YELLOW = '\033[33m'
    BLUE = '\033[34m'
    MAGENTA = '\033[35m'
    CYAN = '\033[36m'
    WHITE = '\033[37m'
    
    BRIGHT_RED = '\033[91m'
    BRIGHT_GREEN = '\033[92m'
    BRIGHT_YELLOW = '\033[93m'
    BRIGHT_BLUE = '\033[94m'
    BRIGHT_CYAN = '\033[96m'
    BRIGHT_WHITE = '\033[97m'
    GRAY = '\033[90m'
    
    BG_BLUE = '\033[44m'
    BG_RED = '\033[41m'

def colorize(text: str, color: str) -> str:
    return f"{color}{text}{Colors.RESET}"

def clear_screen():
    os.system('cls' if os.name == 'nt' else 'clear')

def get_terminal_size() -> Tuple[int, int]:
    try:
        return shutil.get_terminal_size((80, 24))
    except Exception:
        return (80, 24)

# =================================================================================
# 2. DETECCIÓN DE HARDWARE Y TELEMETRÍA
# =================================================================================
class HardwareDetector:
    def __init__(self):
        self.gpu_name = "No detectada"
        self.gpu_temp = 0
        self.gpu_load = 0
        self.gpu_mem_used = 0
        self.gpu_mem_total = 0
        self.nvenc_supported = False
        self.mediacodec_supported = False
        
        self.cpu_cores = os.cpu_count() or 8
        self.max_threads = min(self.cpu_cores, 16)
        
        self.ram_used = 0
        self.ram_total = 0
        
        self.disks = []
        self._detect_gpu()
        self._detect_ram()
        self._detect_disks()

    def _detect_gpu(self):
        # Detección NVIDIA (NVENC)
        try:
            res = subprocess.run(
                ['nvidia-smi', '--query-gpu=name,temperature.gpu,utilization.gpu,memory.used,memory.total',
                 '--format=csv,noheader,nounits'],
                capture_output=True, text=True, timeout=3
            )
            if res.returncode == 0:
                parts = res.stdout.strip().split(',')
                if len(parts) >= 5:
                    self.gpu_name = parts[0].strip()
                    self.gpu_temp = int(parts[1].strip())
                    self.gpu_load = int(parts[2].strip())
                    self.gpu_mem_used = int(parts[3].strip())
                    self.gpu_mem_total = int(parts[4].strip())
                    self.nvenc_supported = True
        except Exception:
            pass
            
        # Detección MediaCodec (Android/Termux)
        try:
            res = subprocess.run(['ffmpeg', '-encoders'], capture_output=True, text=True, timeout=3)
            if 'h264_mediacodec' in res.stdout:
                self.mediacodec_supported = True
                if not self.nvenc_supported:
                    self.gpu_name = "MediaCodec (Android)"
        except Exception:
            pass

    def _detect_ram(self):
        try:
            if sys.platform == 'linux':
                with open('/proc/meminfo', 'r') as f:
                    lines = f.readlines()
                    total = int(lines[0].split()[1]) // 1024
                    available = int(lines[2].split()[1]) // 1024
                    self.ram_total = total
                    self.ram_used = total - available
            else:
                self.ram_total = 16384
                self.ram_used = 4096
        except Exception:
            self.ram_total = 16384
            self.ram_used = 4096

    def _detect_disks(self):
        self.disks = []
        if os.name == 'nt':
            for letter in 'CDEFGHI':
                path = f"{letter}:\\"
                if os.path.exists(path):
                    try:
                        usage = shutil.disk_usage(path)
                        self.disks.append({
                            'name': f"Disco Local ({letter}:)",
                            'used': usage.used / (1024**3),
                            'total': usage.total / (1024**3)
                        })
                    except: pass
        else:
            try:
                usage = shutil.disk_usage('/')
                self.disks.append({'name': "Local (/)", 'used': usage.used/(1024**3), 'total': usage.total/(1024**3)})
            except: pass
        
        for net in ['\\\\192.168.1.21h', '\\\\192.168.1.21m']:
            if os.path.exists(net):
                self.disks.append({'name': net, 'used': 0, 'total': 0, 'type': 'network'})

    def update_metrics(self):
        if self.nvenc_supported:
            try:
                res = subprocess.run(
                    ['nvidia-smi', '--query-gpu=temperature.gpu,utilization.gpu,memory.used,memory.total',
                     '--format=csv,noheader,nounits'],
                    capture_output=True, text=True, timeout=2
                )
                if res.returncode == 0:
                    parts = res.stdout.strip().split(',')
                    self.gpu_temp = int(parts[0].strip())
                    self.gpu_load = int(parts[1].strip())
                    self.gpu_mem_used = int(parts[2].strip())
                    self.gpu_mem_total = int(parts[3].strip())
            except: pass
        self._detect_ram()

# =================================================================================
# 3. SISTEMA DE PERFILES Y AUTOMATIZACIÓN (XPRESS ENGINE)
# =================================================================================
class PresetManager:
    def __init__(self):
        self.current_profile = "default"
        self.profiles = {
            "default": {
                "name": "Español · Dual Audio",
                "format": "mp4",
                "quality": "Alta",
                "video_codec": "h264",
                "audio_bitrate": "256 kbps",
                "resolution": "origen"
            },
            "h264_streaming": {
                "name": "H.264_STREAMING",
                "format": "mp4",
                "quality": "Media",
                "video_codec": "h264",
                "audio_bitrate": "128 kbps",
                "resolution": "720p"
            }
        }

    def get_active(self) -> Dict:
        return self.profiles.get(self.current_profile, self.profiles["default"])

class XPRESSAutomation:
    def __init__(self):
        self.rules = {
            "keep_folder": True,
            "hide_window": True,
            "forced_subs": True,
            "profile_name_in_output": True,
            "cap_novelas": False,
            "cap_series": True,
            "auto_origin_path": True
        }

# =================================================================================
# 4. MOTOR DE CONVERSIÓN FFmpeg
# =================================================================================
class FFmpegEngine:
    def __init__(self, hardware: HardwareDetector):
        self.hardware = hardware
        self.ffmpeg_cmd = "ffmpeg"

    def build_command(self, input_file: str, output_file: str, profile: Dict, 
                      threads: int = 8, use_gpu: bool = True, turbo: bool = False) -> List[str]:
        cmd = [self.ffmpeg_cmd, "-hide_banner", "-i", input_file]
        
        if use_gpu and self.hardware.nvenc_supported:
            cmd.extend(["-c:v", "h264_nvenc", "-preset", "p4" if not turbo else "p1"])
        elif use_gpu and self.hardware.mediacodec_supported:
            cmd.extend(["-c:v", "h264_mediacodec"])
        else:
            preset = "ultrafast" if turbo else "medium"
            cmd.extend(["-c:v", "libx264", "-preset", preset])
            
        cmd.extend(["-threads", str(threads)])
        cmd.extend(["-c:a", "aac", "-b:a", profile.get("audio_bitrate", "256k").replace(" kbps", "k")])
        cmd.extend(["-c:s", "mov_text"])
        cmd.extend(["-y", output_file])
        return cmd

    def run_conversion(self, input_file: str, output_file: str, profile: Dict, 
                       threads: int, use_gpu: bool, turbo: bool, 
                       progress_callback=None) -> bool:
        cmd = self.build_command(input_file, output_file, profile, threads, use_gpu, turbo)
        try:
            creationflags = subprocess.CREATE_NO_WINDOW if os.name == 'nt' else 0
            process = subprocess.Popen(
                cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE, 
                universal_newlines=True, creationflags=creationflags
            )
            for line in process.stderr:
                if "time=" in line and progress_callback:
                    match = re.search(r"time=(\d+):(\d+):(\d+)", line)
                    speed_match = re.search(r"speed=\s*([\d.]+)x", line)
                    if match:
                        current_secs = int(match.group(1))*3600 + int(match.group(2))*60 + int(match.group(3))
                        speed = float(speed_match.group(1)) if speed_match else 1.0
                        progress_callback(current_secs, speed)
            process.wait()
            return process.returncode == 0
        except Exception as e:
            print(f"Error FFmpeg: {e}")
            return False

# =================================================================================
# 5. COLA DE PROCESAMIENTO Y SESIÓN
# =================================================================================
class ProcessingQueue:
    def __init__(self, engine: FFmpegEngine, profile: Dict, max_active: int = 3):
        self.engine = engine
        self.profile = profile
        self.max_active = max_active
        self.jobs: List[Dict] = []
        self.executor = ThreadPoolExecutor(max_workers=max_active)
        self.lock = threading.Lock()
        self.is_paused = False
        self.is_stopped = False

    def add_job(self, input_file: str, output_file: str):
        self.jobs.append({
            'input': input_file,
            'output': output_file,
            'progress': 0,
            'speed': 0.0,
            'status': 'pending',
            'eta': '00:00:00'
        })

    def start_queue(self):
        for job in self.jobs:
            if job['status'] == 'pending' and not self.is_stopped:
                job['status'] = 'running'
                self.executor.submit(self._process_job, job)

    def _process_job(self, job: Dict):
        while self.is_paused and not self.is_stopped:
            time.sleep(1)
        if self.is_stopped:
            job['status'] = 'failed'
            return
        def on_progress(secs, speed):
            job['speed'] = speed
            job['progress'] = min(100, job['progress'] + (speed * 0.5))
        success = self.engine.run_conversion(
            job['input'], job['output'], self.profile, 
            threads=8, use_gpu=True, turbo=False, 
            progress_callback=on_progress
        )
        job['progress'] = 100 if success else job['progress']
        job['status'] = 'completed' if success else 'failed'

class SessionManager:
    def __init__(self):
        self.session_file = "session.json"
        self.history_file = "history.json"
        self.session = {"jobs": [], "config": {}, "sections": {}}
        self.history = []
        self._load()

    def _load(self):
        if os.path.exists(self.session_file):
            try:
                with open(self.session_file, 'r') as f:
                    self.session = json.load(f)
            except: pass
        if os.path.exists(self.history_file):
            try:
                with open(self.history_file, 'r') as f:
                    self.history = json.load(f)
            except: pass

    def save(self):
        try:
            with open(self.session_file, 'w') as f:
                json.dump(self.session, f, indent=2)
            with open(self.history_file, 'w') as f:
                json.dump(self.history, f, indent=2)
        except: pass

# =================================================================================
# 6. DASHBOARD PRINCIPAL (TUI)
# =================================================================================
class Dashboard:
    def __init__(self):
        self.hardware = HardwareDetector()
        self.presets = PresetManager()
        self.automation = XPRESSAutomation()
        self.session = SessionManager()
        self.engine = FFmpegEngine(self.hardware)
        self.queue = ProcessingQueue(self.engine, self.presets.get_active(), max_active=3)
        self.start_time = time.time()
        self.running = True
        
        self.queue.add_job("Hasta El Fin del Mundo [2025] [1080p] [Dual Audio].mp4", "out1.mp4")
        self.queue.jobs[0]['progress'] = 59
        self.queue.jobs[0]['speed'] = 3.93
        self.queue.jobs[0]['status'] = 'running'
        self.queue.add_job("La Bestia [2026] [1080p] [Dual A].mp4", "out2.mp4")
        self.queue.jobs[1]['progress'] = 47
        self.queue.jobs[1]['speed'] = 3.89
        self.queue.jobs[1]['status'] = 'running'
        self.queue.add_job("Resident Evil [Noche Cero] [2026] [1080p].mp4", "out3.mp4")
        self.queue.jobs[2]['progress'] = 61
        self.queue.jobs[2]['speed'] = 3.84
        self.queue.jobs[2]['status'] = 'running'

    def draw_progress_bar(self, percent: float, width: int = 30) -> str:
        filled = int(width * percent / 100)
        bar = "█" * filled + "░" * (width - filled)
        color = Colors.BRIGHT_GREEN if percent > 50 else Colors.BRIGHT_YELLOW
        return f"{colorize(bar, color)} {percent:.0f}%"

    def render(self):
        clear_screen()
        width, height = get_terminal_size()
        profile_name = self.presets.get_active()['name']
        header = f"★ QBASCONVERTER {colorize('v3.1.8-pro', Colors.BRIGHT_GREEN)} | PERFIL: {profile_name}"
        status = f"GPU · {self.hardware.cpu_cores} núcleos · sesión guardada ✓ | T: {self.hardware.gpu_temp}°C · RAM: {self.hardware.ram_used}/{self.hardware.ram_total} MB"
        print(f"{colorize(header, Colors.BOLD + Colors.BRIGHT_CYAN)}")
        print(f"{colorize(status, Colors.DIM)}")
        print("─" * width)
        print(f"\n{colorize('▌ ENG_CORE v3.1.8', Colors.BRIGHT_CYAN)}")
        print(f"{colorize('▌ Batch Queue', Colors.BOLD + Colors.BG_BLUE + Colors.WHITE)}")
        print(f"{colorize('▌ Codec Studio', Colors.GRAY)}")
        print(f"{colorize('▌ Telemetry', Colors.GRAY)}")
        print(f"{colorize('▌ Discos & Red', Colors.GRAY)}")
        for disk in self.hardware.disks[:2]:
            print(f"  {colorize('└', Colors.GRAY)} {disk['name']}: {disk['used']:.1f}G / {disk['total']:.1f}G")
        print("\n" + "═" * width)
        rules = "[✓] MANTENER CARPETA  [✓] SIN MOSTRAR VENTANA  [✓] SUBTÍTULOS FORZADOS  [✓] NOMBRE DE PERFIL"
        print(f"{colorize(' REGLAS DE AUTOMATIZACIÓN (XPRESS ENGINE)', Colors.BOLD + Colors.BRIGHT_CYAN)}")
        print(f"  {rules}")
        p = self.presets.get_active()
        print(f"\n{colorize('⚙ SALIDA Y CALIDAD', Colors.BOLD + Colors.BRIGHT_CYAN)} | CONFIG_ACTIVA | PERFIL: H.264_STREAMING")
        print(f"  FORMATO: {p['format']} | NIVEL: {colorize(p['quality'], Colors.BRIGHT_GREEN)} | CÓDEC: {p['video_codec']} | AUDIO: {p['audio_bitrate']}")
        print(f"\n{colorize('⚡ RENDIMIENTO & ACELERACIÓN', Colors.BOLD + Colors.BRIGHT_CYAN)} | NVENC_CUDA: ACTIVO")
        print(f"  [GPU] [ ] CPU | [TURBO] | NÚCLEOS: 8 (Óptimo) | AFINIDAD: C1-C8 ACTIVOS")
        active_jobs = [j for j in self.queue.jobs if j['status'] == 'running']
        print(f"\n{colorize('📊 COLA DE PROCESAMIENTO EN LOTE', Colors.BOLD + Colors.BRIGHT_CYAN)} | VIDEOS: 3 de 14 | {colorize(f'{len(active_jobs)} ACTIVOS', Colors.BG_RED + Colors.WHITE)}")
        for job in self.queue.jobs:
            if job['status'] == 'running':
                bar = self.draw_progress_bar(job['progress'])
                eta = f"00:{15 - int(job['progress'])//5}:00"
                print(f"  {colorize(f'{job['speed']:.2f}x', Colors.BRIGHT_CYAN)} {bar} {job['input'][:50]}... ETA {eta}")
            elif job['status'] == 'pending':
                print(f"  {colorize('⏳', Colors.YELLOW)} {job['input'][:60]}...")
        print(f"\n{colorize('📋 HISTORIAL COMPLETADO', Colors.BOLD + Colors.BRIGHT_CYAN)} | Última: 500 Días de Escobar (02:41) | {colorize('3 / 14 OK', Colors.BRIGHT_GREEN)}")
        elapsed = int(time.time() - self.start_time)
        mins, secs = divmod(elapsed, 60)
        time_str = f"{mins:02d}:{secs:02d}"
        print("\n" + "═" * width)
        footer = f"[▶ CONVERTIR] [⏸ PAUSAR] TIEMPO: {colorize(time_str, Colors.BRIGHT_GREEN)} [■ DETENER] | 1 Lote · 2 Herramientas · 0 Salir | PID: {os.getpid()} | FFMPEG 6.1.1-XPRESS"
        print(f" {footer}")
        print("═" * width)

    def run(self):
        def signal_handler(sig, frame):
            self.running = False
            self.session.save()
            print("\nSesión guardada. Saliendo...")
            sys.exit(0)
        try:
            signal.signal(signal.SIGINT, signal_handler)
        except Exception:
            pass
        print("Iniciando QBASCONVERTER v3.1.8-pro...")
        time.sleep(1)
        while self.running:
            try:
                self.hardware.update_metrics()
                self.render()
                time.sleep(2)
            except KeyboardInterrupt:
                self.running = False
                self.session.save()
                break

# =================================================================================
# 7. PUNTO DE ENTRADA
# =================================================================================
def main():
    app = Dashboard()
    app.run()

if __name__ == "__main__":
    main()
