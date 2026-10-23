"""Detección de hardware, GPU, CPU, discos y afinidad"""
import os
import platform
import shutil
import subprocess
from pathlib import Path
from typing import List, Dict, Any

class Hardware:
    def __init__(self):
        self.os_name = platform.system()
        self.cores_logical = os.cpu_count() or 8
        self.cores_physical = self.cores_logical
        self.gpu_name = "No detectada"
        self.gpu_vendor = "unknown"
        self.gpu_temp = 0
        self.gpu_load = 0
        self.gpu_memory_used = 0
        self.gpu_memory_total = 0
        self.ram_total_mb = 0
        self.ram_used_mb = 0
        self.ram_free_mb = 0
        self.disks = []
        self.network_paths = []
        self.nvenc_supported = False
        self.ffmpeg_version = "desconocida"
        self.core_affinity = list(range(1, min(self.cores_logical, 16) + 1))
        self._detect_all()

    def _detect_all(self):
        self._detect_cpu()
        self._detect_gpu()
        self._detect_ram()
        self._detect_disks()
        self._detect_network()
        self._detect_ffmpeg()

    def _detect_cpu(self):
        try:
            if self.os_name == "Linux":
                try:
                    with open('/proc/cpuinfo') as f:
                        content = f.read()
                        count = content.count('processor')
                        if count:
                            self.cores_logical = count
                except:
                    pass
        except:
            pass
        self.cores_physical = min(self.cores_logical, self.cores_logical // 2 if self.cores_logical > 1 else self.cores_logical)
        if self.cores_physical == 0:
            self.cores_physical = self.cores_logical

    def _detect_gpu(self):
        # NVIDIA via nvidia-smi
        try:
            res = subprocess.run(
                ['nvidia-smi', '--query-gpu=name,temperature.gpu,utilization.gpu,memory.used,memory.total', 
                 '--format=csv,noheader,nounits'],
                capture_output=True, text=True, timeout=3
            )
            if res.returncode == 0 and res.stdout.strip():
                for line in res.stdout.strip().splitlines():
                    parts = [p.strip() for p in line.split(',')]
                    if len(parts) >= 5:
                        self.gpu_name = parts[0]
                        self.gpu_vendor = "nvidia"
                        self.gpu_temp = int(parts[1]) if parts[1].isdigit() else 0
                        self.gpu_load = int(parts[2]) if parts[2].isdigit() else 0
                        self.gpu_memory_used = int(parts[3]) if parts[3].isdigit() else 0
                        self.gpu_memory_total = int(parts[4]) if parts[4].isdigit() else 0
                        self.nvenc_supported = True
                        return
        except Exception:
            pass
        # AMD
        try:
            res = subprocess.run(['rocm-smi', '--showtemp', '--showuse'], capture_output=True, text=True, timeout=2)
            if res.returncode == 0:
                self.gpu_vendor = "amd"
        except Exception:
            pass
        # Intel
        try:
            res = subprocess.run(['vainfo'], capture_output=True, text=True, timeout=2)
            if res.returncode == 0 and 'intel' in res.stdout.lower():
                self.gpu_vendor = "intel"
        except Exception:
            pass

    def _detect_ram(self):
        try:
            if self.os_name == "Linux":
                with open('/proc/meminfo') as f:
                    data = {}
                    for line in f:
                        if ':' in line:
                            k, v = line.split(':')
                            data[k.strip()] = int(v.strip().split()[0])
                    self.ram_total_mb = data.get('MemTotal', 0) // 1024
                    avail = data.get('MemAvailable', data.get('MemFree', 0))
                    self.ram_used_mb = self.ram_total_mb - (avail // 1024)
                    self.ram_free_mb = avail // 1024
            elif self.os_name == "Windows":
                try:
                    import psutil
                    vm = psutil.virtual_memory()
                    self.ram_total_mb = vm.total // (1024**2)
                    self.ram_used_mb = vm.used // (1024**2)
                    self.ram_free_mb = vm.free // (1024**2)
                except Exception:
                    pass
            elif self.os_name == "Darwin":
                try:
                    res = subprocess.run(['sysctl', '-n', 'hw.memsize'], capture_output=True, text=True)
                    total = int(res.stdout.strip())
                    self.ram_total_mb = total // (1024**2)
                    try:
                        import psutil
                        vm = psutil.virtual_memory()
                        self.ram_used_mb = vm.used // (1024**2)
                        self.ram_free_mb = vm.free // (1024**2)
                    except:
                        self.ram_used_mb = self.ram_total_mb // 4
                        self.ram_free_mb = self.ram_total_mb // 2
                except:
                    pass
        except Exception:
            if self.ram_total_mb == 0:
                self.ram_total_mb = 16384
                self.ram_used_mb = 4096
                self.ram_free_mb = 12288

    def _detect_disks(self):
        self.disks = []
        try:
            if self.os_name == "Windows":
                for letter in 'ABCDEFGHIJKLMNOPQRSTUVWXYZ':
                    path = f"{letter}:\\"
                    if os.path.exists(path):
                        try:
                            usage = shutil.disk_usage(path)
                            total_gb = usage.total / (1024**3)
                            used_gb = usage.used / (1024**3)
                            free_gb = usage.free / (1024**3)
                            self.disks.append({
                                'name': f"Local ({letter}:)",
                                'path': path,
                                'total_gb': round(total_gb, 1),
                                'used_gb': round(used_gb, 1),
                                'free_gb': round(free_gb, 1),
                                'percent': round((used_gb/total_gb)*100, 1) if total_gb > 0 else 0,
                                'type': 'NTFS'
                            })
                        except:
                            pass
            elif self.os_name == "Darwin":
                try:
                    import psutil
                    for part in psutil.disk_partitions(all=False):
                        if part.mountpoint and not part.mountpoint.startswith('/System/Volumes'):
                            try:
                                usage = shutil.disk_usage(part.mountpoint)
                                total_gb = usage.total / (1024**3)
                                used_gb = usage.used / (1024**3)
                                free_gb = usage.free / (1024**3)
                                self.disks.append({
                                    'name': part.device.split('/')[-1] or part.mountpoint,
                                    'path': part.mountpoint,
                                    'total_gb': round(total_gb, 1),
                                    'used_gb': round(used_gb, 1),
                                    'free_gb': round(free_gb, 1),
                                    'percent': round((used_gb/total_gb)*100, 1) if total_gb > 0 else 0,
                                    'type': part.fstype
                                })
                            except:
                                pass
                except:
                    pass
            else:  # Linux
                try:
                    import psutil
                    for part in psutil.disk_partitions(all=False):
                        if part.mountpoint in ['/', '/home', '/storage', '/sdcard'] or part.mountpoint.startswith('/media') or part.mountpoint.startswith('/mnt'):
                            try:
                                usage = shutil.disk_usage(part.mountpoint)
                                total_gb = usage.total / (1024**3)
                                used_gb = usage.used / (1024**3)
                                free_gb = usage.free / (1024**3)
                                name = part.mountpoint.split('/')[-1] or part.mountpoint
                                self.disks.append({
                                    'name': name.capitalize() if name else part.mountpoint,
                                    'path': part.mountpoint,
                                    'total_gb': round(total_gb, 1),
                                    'used_gb': round(used_gb, 1),
                                    'free_gb': round(free_gb, 1),
                                    'percent': round((used_gb/total_gb)*100, 1) if total_gb > 0 else 0,
                                    'type': part.fstype
                                })
                            except:
                                pass
                except:
                    try:
                        usage = shutil.disk_usage('/')
                        self.disks.append({'name': 'Root (/)', 'path': '/', 'total_gb': round(usage.total/(1024**3),1), 'used_gb': round(usage.used/(1024**3),1), 'free_gb': round(usage.free/(1024**3),1), 'percent': round((usage.used/usage.total)*100,1), 'type': 'ext4'})
                    except:
                        pass
        except Exception as e:
            pass

    def _detect_network(self):
        self.network_paths = []
        try:
            if self.os_name == "Windows":
                self.network_paths = [{'name': 'SMB/Red', 'path': '\\\\', 'status': 'Disponible'}]
            elif self.os_name in ["Linux", "Darwin"]:
                for p in ['/mnt', '/media', '/Volumes']:
                    if os.path.exists(p):
                        self.network_paths.append({'name': f'Red/{p[1:]}', 'path': p, 'status': 'Disponible'})
        except:
            pass

    def _detect_ffmpeg(self):
        try:
            res = subprocess.run(['ffmpeg', '-version'], capture_output=True, text=True, timeout=3)
            if res.returncode == 0:
                first = res.stdout.splitlines()[0]
                if 'ffmpeg' in first.lower():
                    self.ffmpeg_version = first.replace('ffmpeg version', '').strip().split(' ')[0]
        except Exception:
            self.ffmpeg_version = "no-encontrado"

    def update(self):
        self._detect_gpu()
        self._detect_ram()

    def get_ram_str(self):
        return f"{self.ram_used_mb}/{self.ram_total_mb} MB"

    def get_summary(self):
        return {
            'cores': self.cores_logical,
            'gpu': self.gpu_name,
            'gpu_load': self.gpu_load,
            'gpu_temp': self.gpu_temp,
            'nvenc': self.nvenc_supported,
            'ram': self.get_ram_str(),
            'ffmpeg': self.ffmpeg_version,
            'disks': len(self.disks)
        }
