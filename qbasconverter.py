#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
QBASCONVERTER v3.1.8-pro (XPRESS HYBRID CORE)
Dashboard TUI con Rich
"""
import os, sys, time, json, re, signal, subprocess, threading
from pathlib import Path
from datetime import datetime
from concurrent.futures import ThreadPoolExecutor

try:
    from rich.console import Console
    from rich.layout import Layout
    from rich.panel import Panel
    from rich.table import Table
    from rich.progress import Progress, BarColumn, TextColumn, TaskProgressColumn, TimeRemainingColumn
    from rich.live import Live
    from rich.align import Align
    from rich.text import Text
    from rich.box import ROUNDED, SQUARE, DOUBLE
except ImportError:
    pass

class Hardware:
    def __init__(self):
        self.gpu_name = "NVIDIA RTX 3060"
        self.gpu_temp = 49
        self.gpu_load = 78
        self.nvenc = True
        self.mediacodec = False
        self.cores = os.cpu_count() or 8
        self.ram_used, self.ram_total = 3400, 32000
        self.disks = [
            {'name':'C:', 'used':25.45,'total':110},
            {'name':'D:', 'used':80.41,'total':7280},
            {'name':'F:', 'used':273.28,'total':5460},
        ]

class Presets:
    def __init__(self):
        self.current='default'
        self.profiles={'default':{'name':'Español · Dual Audio','format':'mp4','quality':'Alta','video_codec':'h264','audio_bitrate':'256 kbps','resolution':'origen'}}
    def get(self): return self.profiles[self.current]

class QueueJob:
    def __init__(self,name,out,spd,prog):
        self.name=name; self.out=out; self.speed=spd; self.progress=prog; self.status='running'

class Queue:
    def __init__(self):
        self.jobs=[
            QueueJob("Hasta El Fin del Mundo [2025] [1080p] [Dual Audio].mp4","out1",3.93,59),
            QueueJob("La Bestia [2026] [1080p] [Dual A].mp4","out2",3.89,47),
            QueueJob("Resident Evil [Noche Cero] [2026] [1080p].mp4","out3",3.84,61),
        ]
    def active(self): return [j for j in self.jobs if j.status=='running']
    def running(self): return len(self.active())

class App:
    def __init__(self):
        self.console=Console()
        self.hw=Hardware(); self.ps=Presets(); self.q=Queue()
        self.start=time.time()
        self.running=True
    def uptime(self):
        s=int(time.time()-self.start); h,s1=divmod(s,3600); m,s2=divmod(s1,60); return f"{h:02d}:{m:02d}:{s2:02d}"
    def build(self):
        l=Layout()
        l.split_column(Layout(name="header",size=4), Layout(name="body"), Layout(name="footer",size=4))
        # header
        h=Table(show_header=False, box=None, padding=0)
        h.add_row(f"[bold #00E5FF]★ QBASCONVERTER [#00FF88]v3.1.8-pro[/] | PERFIL: {self.ps.get()['name']}[/]", f"[dim]GPU · {self.hw.cores} núcleos · sesión guardada ✓ | T: {self.hw.gpu_temp}°C · RAM: {self.hw.ram_used}/{self.hw.ram_total} MB[/]")
        l["header"].update(Panel(h,style="#0D1117"))
        # body split
        l["body"].split_row(Layout(name="side",size=24), Layout(name="main"))
        # side
        st=Table(show_header=False, box=None)
        st.add_row("[#00E5FF]▌ ENG_CORE v3.1.8")
        st.add_row("[bold #FFFFFF on #4D9DE0]▌ Batch Queue[/]")
        st.add_row("[#8B949E]▌ Codec Studio")
        st.add_row("[#8B949E]▌ Telemetry")
        st.add_row("[#8B949E]▌ Execution Log")
        st.add_row("[#8B949E]▌ Discos & Red")
        for d in self.hw.disks[:3]:
            st.add_row(f"  [#8B949E]└ {d['name']} {d['used']:.1f}G/{d['total']:.1f}G")
        l["side"].update(Panel(st, title="Navegación", style="#0D1117"))
        # main
        m=Layout()
        m.split_column(Layout(name="r1",size=3), Layout(name="r2",size=3), Layout(name="r3",size=3), Layout(name="r4",size=5), Layout(name="r5",size=6), Layout(name="r6",size=3))
        # rules
        r1=Text("[✓] MANTENER CARPETA   [✓] SIN MOSTRAR VENTANA   [✓] SUBTÍTULOS FORZADOS   [✓] NOMBRE DE PERFIL", style="#E6EDF3")
        m["r1"].update(Panel(r1, title="REGLAS DE AUTOMATIZACIÓN (XPRESS ENGINE)", style="#0D1117", border_style="#30363D"))
        p=self.ps.get()
        r2=Text(f"FORMATO: {p['format']} | CALIDAD: [#00FF88]{p['quality']}[/] | CÓDEC: {p['video_codec']} | AUDIO: {p['audio_bitrate']} | PERFIL: {p['name']}", style="#E6EDF3")
        m["r2"].update(Panel(r2, title="SALIDA Y CALIDAD", style="#0D1117", border_style="#30363D"))
        r3=Text("ACELERACIÓN: [#00FF88]NVENC_CUDA[/]   |   TURBO: [#00FF88]ACTIVO[/]   |   NÚCLEOS: [#00E5FF]8[/] (Óptimo)   |   ACTIVOS: [#00E5FF]3[/]   |   MATRIZ: C1-C16", style="#E6EDF3")
        m["r3"].update(Panel(r3, title="RENDIMIENTO & ACELERACIÓN", style="#0D1117", border_style="#30363D"))
        tq=Table(show_header=False, box=None)
        for j in self.q.jobs:
            tq.add_row(f"[#00E5FF]{j.speed:.2f}x[/]", self.pg(j.progress), f"{j.name[:48]}", f"ETA 00:{max(0,15-int(j.progress)//5):02d}:00")
        m["r4"].update(Panel(tq, title=f"COLA DE PROCESAMIENTO EN LOTE ({self.q.running()} ACTIVOS)", style="#0D1117", border_style="#30363D"))
        hst=Text("[#00FF88]3/14 OK[/]   |   FALLOS: 0   |   TOTAL: 14   |   ÚLTIMA: 500 Días de Escobar (02:41)", style="#E6EDF3")
        m["r5"].update(Panel(hst, title="HISTORIAL COMPLETADO", style="#0D1117", border_style="#30363D"))
        m["r6"].update(Panel(Text("ENG_CORE | v3.1.8-pro | XPRESS HYBRID CORE", style="#8B949E"), style="#0D1117", border_style="#30363D"))
        l["main"].update(m)
        # footer
        f=Text(f"[▶ CONVERTIR] [⏸ PAUSAR] [■ DETENER]   TIEMPO: [#00FF88]{self.uptime()}[/]   GPU: {self.hw.gpu_load}% {self.hw.gpu_temp}°C   Lote: {len(self.q.jobs)}   PID: {os.getpid()}   FFMPEG 6.1.1-XPRESS", style="#E6EDF3")
        l["footer"].update(Panel(f, style="#0D1117"))
        return l
    def pg(self,p):
        f=int(30*p/100); b="█"*f+"░"*(30-f); c="#00FF88" if p>60 else "#FFD600" if p>30 else "#8B949E"; return f"[{c}]{b}[/] {p:.0f}%"
    def run(self):
        try:
            with Live(self.build(), console=self.console, refresh_per_second=1, screen=False) as live:
                while self.running:
                    time.sleep(1)
                    live.update(self.build())
        except KeyboardInterrupt:
            self.running=False

if __name__=="__main__":
    App().run()
