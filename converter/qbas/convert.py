"""Motor FFmpeg con NVENC_CUDA y CPU"""
import subprocess
import os
import re
from pathlib import Path
from typing import Dict, Any, Callable, Optional

class Converter:
    def __init__(self, hardware):
        self.hardware = hardware
        self.ffmpeg_path = "ffmpeg"
        self.ffprobe_path = "ffprobe"
        self._check_ffmpeg()

    def _check_ffmpeg(self):
        for cmd in ["ffmpeg", "ffmpeg.exe"]:
            try:
                res = subprocess.run([cmd, "-version"], capture_output=True, text=True, timeout=2)
                if res.returncode == 0:
                    self.ffmpeg_path = cmd
                    break
            except:
                pass
        for cmd in ["ffprobe", "ffprobe.exe"]:
            try:
                res = subprocess.run([cmd, "-version"], capture_output=True, text=True, timeout=2)
                if res.returncode == 0:
                    self.ffprobe_path = cmd
                    break
            except:
                pass

    def get_duration(self, input_file: str) -> float:
        try:
            cmd = [self.ffprobe_path, "-v", "error", "-show_entries", "format=duration", "-of", "default=noprint_wrappers=1:nokey=1", input_file]
            res = subprocess.run(cmd, capture_output=True, text=True, timeout=10)
            if res.returncode == 0 and res.stdout.strip():
                return float(res.stdout.strip())
        except Exception:
            pass
        return 0.0

    def normalize_bitrate(self, br: str) -> str:
        if not br:
            return "256k"
        br = str(br).strip().lower()
        if br.endswith('kbps'):
            num = br.replace('kbps', '').strip()
            return f"{num}k"
        if br.endswith('k'):
            return br
        # try number
        m = re.findall(r'(\d+)', br)
        if m:
            return f"{m[0]}k"
        return "256k"

    def build_command(self, input_file: str, output_file: str, profile: Dict[str, Any], 
                     threads: int = 8, use_gpu: bool = True, turbo: bool = True) -> list:
        cmd = [self.ffmpeg_path, "-hide_banner", "-i", input_file]
        
        vcodec = profile.get("video_codec", "h264")
        if use_gpu and self.hardware.nvenc_supported and self.hardware.gpu_vendor == "nvidia":
            if vcodec == "hevc":
                cmd.extend(["-c:v", "hevc_nvenc"])
            elif vcodec == "h264":
                cmd.extend(["-c:v", "h264_nvenc"])
            else:
                cmd.extend(["-c:v", "h264_nvenc"])
            preset = profile.get("video_preset_gpu", "p4")
            cmd.extend(["-preset", preset])
            if turbo:
                cmd.extend(["-tune", "hq"])
        else:
            if vcodec == "hevc":
                cmd.extend(["-c:v", "libx265"])
            elif vcodec == "h264":
                cmd.extend(["-c:v", "libx264"])
            else:
                cmd.extend(["-c:v", "libx264"])
            preset = profile.get("video_preset_cpu", "medium")
            if turbo and preset in ["medium", "slow"]:
                preset = "veryfast"
            cmd.extend(["-preset", preset])
            crf = profile.get("crf", 23)
            cmd.extend(["-crf", str(crf)])
        
        threads = max(1, min(int(threads), 16))
        cmd.extend(["-threads", str(threads)])
        
        acodec = profile.get("audio_codec", "aac")
        abitrate = self.normalize_bitrate(profile.get("audio_bitrate", "256k"))
        if acodec == "copy":
            cmd.extend(["-c:a", "copy"])
        else:
            cmd.extend(["-c:a", "aac", "-b:a", abitrate])
        
        # subtitles
        sub = profile.get("subtitles", "copy")
        if sub == "copy":
            cmd.extend(["-c:s", "copy"])
        elif sub == "none":
            cmd.extend(["-sn"])
        
        cmd.extend(["-map", "0"])
        cmd.extend(["-movflags", "+faststart"])
        cmd.extend(["-y", output_file])
        return cmd

    def run(self, input_file: str, output_file: str, profile: Dict[str, Any], 
            threads: int = 8, use_gpu: bool = True, turbo: bool = True,
            progress_callback: Optional[Callable] = None) -> bool:
        try:
            Path(output_file).parent.mkdir(parents=True, exist_ok=True)
            cmd = self.build_command(input_file, output_file, profile, threads, use_gpu, turbo)
            duration = self.get_duration(input_file)
            process = subprocess.Popen(
                cmd, 
                stdout=subprocess.PIPE, 
                stderr=subprocess.PIPE,
                universal_newlines=True,
                bufsize=1
            )
            for line in process.stderr:
                if progress_callback and duration > 0 and 'time=' in line:
                    try:
                        m = re.search(r'time=(\d{2}):(\d{2}):(\d{2})\.(\d+)', line)
                        if m:
                            h, mi, s, ms = int(m.group(1)), int(m.group(2)), int(m.group(3)), int(m.group(4))
                            current = h*3600 + mi*60 + s + ms/100
                            pct = min(99.9, (current / duration) * 100)
                            progress_callback(pct)
                    except:
                        pass
            process.wait(timeout=3600)
            if progress_callback:
                progress_callback(100.0)
            return process.returncode == 0
        except subprocess.TimeoutExpired:
            try:
                process.kill()
            except:
                pass
            return False
        except Exception as e:
            print(f"[CONVERT] Error: {e}")
            return False
