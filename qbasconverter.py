#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
QBASCONVERTER v3.1.8-pro (XPRESS HYBRID CORE)
GUI estilo XpressVideoConverter
"""
import tkinter as tk
from tkinter import ttk, filedialog, messagebox, scrolledtext
import os, sys, json, threading, subprocess, re, time
from pathlib import Path

CONFIG_FILE = "qbasconverter_config.json"
SESSION_FILE = "qbasconverter_session.json"

class App(tk.Tk):
    def __init__(self):
        super().__init__()
        self.title("QBASCONVERTER v3.1.8-pro | XPRESS HYBRID CORE")
        self.geometry("1080x720")
        self.minsize(1000, 680)
        self.configure(bg="#1e1e1e")
        
        self.files = []
        self.running = False
        self.stop_flag = False
        self.ffmpeg = self.find_ffmpeg()
        self.nvenc = self.has_nvenc()
        self.mediacodec = self.has_mediacodec()
        self.config = self.load_config()
        self.session = self.load_session()
        
        self._build()
        self._style()
        self.protocol("WM_DELETE_WINDOW", self.on_close)
        
    def find_ffmpeg(self):
        for c in ["ffmpeg","ffmpeg.exe","./ffmpeg","./ffmpeg.exe","codecs/ffmpeg.exe","./codecs/ffmpeg"]:
            try:
                r = subprocess.run([c,"-version"], stdout=subprocess.PIPE, stderr=subprocess.PIPE, timeout=2)
                if r.returncode==0: return c
            except: pass
        return None
    
    def has_nvenc(self):
        if not self.ffmpeg: return False
        try:
            r = subprocess.run([self.ffmpeg,"-encoders"], stdout=subprocess.PIPE, stderr=subprocess.PIPE, timeout=3)
            return b"h264_nvenc" in (r.stdout or b"")
        except: return False
    def has_mediacodec(self):
        if not self.ffmpeg: return False
        try:
            r = subprocess.run([self.ffmpeg,"-encoders"], stdout=subprocess.PIPE, stderr=subprocess.PIPE, timeout=3)
            out = r.stdout or b""
            return b"mediacodec" in out
        except: return False
    
    def load_config(self):
        try:
            if os.path.exists(CONFIG_FILE): return json.load(open(CONFIG_FILE,encoding="utf-8"))
        except: pass
        return {"threads":8,"max_active":3,"turbo":False,"gpu":True,"format":"mp4","quality":"Alta","outdir":str(Path.home()/ "Desktop"),"profile":"default"}
    def save_config(self):
        try: json.dump(self.config, open(CONFIG_FILE,"w",encoding="utf-8"), ensure_ascii=False, indent=2)
        except: pass
    def load_session(self):
        try:
            if os.path.exists(SESSION_FILE): return json.load(open(SESSION_FILE,encoding="utf-8"))
        except: pass
        return {"files":[]}
    def save_session(self):
        try:
            self.session["files"] = list(self.files)
            json.dump(self.session, open(SESSION_FILE,"w",encoding="utf-8"), ensure_ascii=False, indent=2)
        except: pass
    def on_close(self):
        self.save_config(); self.save_session(); self.destroy()
    
    def _style(self):
        try:
            self.style = ttk.Style(self)
            self.style.theme_use('clam')
        except: pass
    
    def _build(self):
        top = tk.Frame(self, bg="#2b2b2b", height=80); top.pack(fill="x"); top.pack_propagate(False)
        tk.Label(top, text="XpressVideoConverter | QBASCONVERTER", bg="#2b2b2b", fg="#00d7ff", font=("Segoe UI", 12, "bold")).pack(side="left", padx=16)
        tk.Label(top, text="v3.1.8-pro XPRESS HYBRID CORE", bg="#2b2b2b", fg="#9d9d9d", font=("Segoe UI", 9)).pack(side="left")
        self.info = tk.Label(top, text="", bg="#2b2b2b", fg="#50fa7b"); self.info.pack(side="right", padx=16); self._updinfo()
        # toolbar
        tb = tk.Frame(self, bg="#1e1e1e", height=40); tb.pack(fill="x")
        tk.Button(tb, text="Añadir Archivos", command=self.add_files, bg="#3c3f41", fg="white", relief="flat").pack(side="left", padx=6,pady=6)
        tk.Button(tb, text="Añadir Carpeta", command=self.add_folder, bg="#3c3f41", fg="white", relief="flat").pack(side="left", padx=6,pady=6)
        tk.Button(tb, text="Quitar", command=self.rem_sel, bg="#3c3f41", fg="white", relief="flat").pack(side="left", padx=6,pady=6)
        tk.Button(tb, text="Limpiar", command=self.clear_all, bg="#3c3f41", fg="white", relief="flat").pack(side="left", padx=6,pady=6)
        # body
        pw = ttk.Panedwindow(self, orient="horizontal"); pw.pack(fill="both", expand=True, padx=8,pady=(0,8))
        leftf = tk.Frame(pw, bg="#1e1e1e"); rightf = tk.Frame(pw, bg="#1e1e1e"); pw.add(leftf, weight=3); pw.add(rightf, weight=2)
        # left list
        cols = ("ruta",)
        self.tree = ttk.Treeview(leftf, columns=cols, show="headings", height=20)
        self.tree.heading("ruta", text="Lista de archivos a convertir")
        self.tree.column("ruta", width=800)
        self.tree.pack(fill="both", expand=True)
        sb = ttk.Scrollbar(leftf, orient="vertical", command=self.tree.yview); self.tree.configure(yscrollcommand=sb.set); sb.pack(side="right", fill="y")
        # right opts
        nb = ttk.Notebook(rightf); nb.pack(fill="both", expand=True)
        # general
        tab1 = ttk.Frame(nb); nb.add(tab1, text="General")
        ttk.Label(tab1, text="Formato de Salida").grid(row=0,column=0, sticky="w", padx=10,pady=6)
        self.fmt = ttk.Combobox(tab1, values=["mp4","mkv","webm","mov","avi","mp3","aac","m4a","flac","wav","ogg"], state="readonly", width=18)
        self.fmt.set(self.config.get("format","mp4")); self.fmt.grid(row=0,column=1, padx=10,pady=6)
        ttk.Label(tab1, text="Calidad").grid(row=1,column=0, sticky="w", padx=10,pady=6)
        self.qual = ttk.Combobox(tab1, values=["Baja","Media","Alta","Sin pérdida"], state="readonly", width=18)
        q = self.config.get("quality","Alta")
        mapq={"Baja":"Baja","Media":"Media","Alta":"Alta","Sin pérdida":"Sin pérdida"}; self.qual.set(q); self.qual.grid(row=1,column=1, padx=10,pady=6)
        ttk.Label(tab1, text="Perfil").grid(row=2,column=0, sticky="w", padx=10,pady=6)
        self.prof = ttk.Combobox(tab1, values=["Español·Dual Audio","H.264_STREAMING","Por defecto"], state="readonly", width=18)
        self.prof.current(0); self.prof.grid(row=2,column=1, padx=10,pady=6)
        ttk.Separator(tab1, orient="horizontal").grid(row=3,column=0,columnspan=2, sticky="ew", padx=10,pady=10)
        self.gpu_var = tk.BooleanVar(value=self.config.get("gpu",True) and (self.nvenc or self.mediacodec))
        ttk.Checkbutton(tab1, text="Aceleración GPU (NVENC/MediaCodec)", variable=self.gpu_var).grid(row=4,column=0,columnspan=2, sticky="w", padx=10)
        self.turbo_var = tk.BooleanVar(value=self.config.get("turbo",False))
        ttk.Checkbutton(tab1, text="Modo Turbo (ultrafast)", variable=self.turbo_var).grid(row=5,column=0,columnspan=2, sticky="w", padx=10)
        ttk.Label(tab1, text="Hilos CPU (1-32)").grid(row=6,column=0, sticky="w", padx=10,pady=6)
        self.threads = ttk.Spinbox(tab1, from_=1, to=32, width=16)
        self.threads.set(self.config.get("threads",8)); self.threads.grid(row=6,column=1, padx=10,pady=6)
        ttk.Label(tab1, text="Trabajos activos simultáneos").grid(row=7,column=0, sticky="w", padx=10,pady=6)
        self.act = ttk.Spinbox(tab1, from_=1, to=8, width=16)
        self.act.set(self.config.get("max_active",3)); self.act.grid(row=7,column=1, padx=10,pady=6)
        ttk.Label(tab1, text="Carpeta de salida").grid(row=8,column=0, sticky="nw", padx=10,pady=6)
        frm = tk.Frame(tab1); frm.grid(row=8,column=1, sticky="ew", padx=10,pady=6)
        self.outent = tk.Entry(frm); self.outent.insert(0,self.config.get("outdir",str(Path.home()/ "Desktop"))); self.outent.pack(side="left", fill="x", expand=True)
        ttk.Button(frm, text="...", command=self.sel_out, width=3).pack(side="left", padx=4)
        tab1.columnconfigure(1, weight=1)
        # cola
        tab2 = ttk.Frame(nb); nb.add(tab2, text="Cola")
        self.queue_txt = scrolledtext.ScrolledText(tab2, height=18, wrap="word"); self.queue_txt.pack(fill="both", expand=True, padx=6,pady=6)
        # log
        tab3 = ttk.Frame(nb); nb.add(tab3, text="Registro")
        self.log = scrolledtext.ScrolledText(tab3, height=18, wrap="word"); self.log.pack(fill="both", expand=True, padx=6,pady=6)
        # footer
        ft = tk.Frame(self, bg="#2b2b2b", height=56); ft.pack(fill="x")
        self.stat = tk.Label(ft, text="Listo. FFmpeg: " + ("OK" if self.ffmpeg else "NO ENCONTRADO"), bg="#2b2b2b", fg="#9d9d9d"); self.stat.pack(side="left", padx=16)
        self.prog = ttk.Progressbar(ft, mode="determinate"); self.prog.pack(side="right", padx=16, fill="x", expand=True, pady=14)
        self.btnconv = tk.Button(ft, text="Iniciar Conversión", command=self.start, bg="#2f9c4c", fg="white", relief="flat", font=("Segoe UI", 10,"bold"))
        self.btnconv.pack(side="right", padx=6, pady=10)
        self.btnstop = tk.Button(ft, text="Detener", command=self.stop, bg="#b71c1c", fg="white", relief="flat", state="disabled")
        self.btnstop.pack(side="right", padx=6, pady=10)
        # restore session
        for f in self.session.get("files", []):
            if os.path.exists(f):
                self.files.append(f); self.tree.insert("", "end", values=(f,))
    def _updinfo(self):
        ff = "OK" if self.ffmpeg else "NO"; nv = "SÍ" if self.nvenc else ("SÍ(MediaCodec)" if self.mediacodec else "NO")
        self.info.config(text=f"FFmpeg: {ff} | NVENC/MediaCodec: {nv}")
        self.after(4000, self._updinfo)
    
    def add_files(self):
        f = filedialog.askopenfilenames(filetypes=[("Multimedia","*.mp4 *.mkv *.avi *.mov *.webm *.wmv *.flv *.mp3 *.m4a *.aac *.flac *.wav *.ogg *.opus")])
        for x in f:
            if x not in self.files: self.files.append(x); self.tree.insert("", "end", values=(x,))
    def add_folder(self):
        d = filedialog.askdirectory(); 
        if not d: return
        ex = ('.mp4','.mkv','.avi','.mov','.webm','.wmv','.flv','.mp3','.m4a','.aac','.flac','.wav','.ogg','.opus')
        for r,_,fl in os.walk(d):
            for fn in fl:
                if fn.lower().endswith(ex):
                    p = os.path.join(r,fn)
                    if p not in self.files: self.files.append(p); self.tree.insert("", "end", values=(p,))
    def rem_sel(self):
        for i in reversed(self.tree.selection()):
            vals=self.tree.item(i,"values"); p=vals[0]
            self.tree.delete(i); self.files=[x for x in self.files if x!=p]
    def clear_all(self):
        for i in self.tree.get_children(): self.tree.delete(i)
        self.files.clear()
    def sel_out(self):
        d=filedialog.askdirectory(initialdir=self.outent.get()); 
        if d: self.outent.delete(0,"end"); self.outent.insert(0,d)
    
    def log_write(self, s):
        self.log.insert("end", s+"\n"); self.log.see("end")
    
    def start(self):
        if not self.files: messagebox.showwarning("Aviso","Añade archivos"); return
        if not self.ffmpeg: messagebox.showerror("Error","FFmpeg no encontrado"); return
        self.config["threads"]=int(self.threads.get()); self.config["max_active"]=int(self.act.get()); self.config["gpu"]=self.gpu_var.get(); self.config["turbo"]=self.turbo_var.get(); self.config["format"]=self.fmt.get(); self.config["quality"]=self.qual.get(); self.config["outdir"]=self.outent.get(); self.save_config()
        self.running=True; self.stop_flag=False
        self.btnconv.config(state="disabled"); self.btnstop.config(state="normal")
        threading.Thread(target=self.run_all, daemon=True).start()
    def stop(self): self.stop_flag=True
    
    def run_all(self):
        outdir=self.outent.get(); os.makedirs(outdir, exist_ok=True)
        fmt=self.fmt.get(); useg=self.gpu_var.get() and (self.nvenc or self.mediacodec); thr=int(self.threads.get())
        total=len(self.files)
        for i,f in enumerate(self.files):
            if self.stop_flag: break
            base=os.path.splitext(os.path.basename(f))[0]
            dst=os.path.join(outdir,f"{base}.{fmt}")
            cmd=[self.ffmpeg,"-hide_banner","-i",f]
            if fmt in ["mp4","mkv","webm","mov","avi"]:
                if useg and self.nvenc: cmd += ["-c:v","h264_nvenc","-preset","p5"]
                elif useg and self.mediacodec: cmd += ["-c:v","h264_mediacodec"]
                else: cmd += ["-c:v","libx264","-preset","medium","-crf","23"]
                cmd += ["-c:a","aac","-b:a","192k"]
            elif fmt=="mp3": cmd += ["-vn","-c:a","libmp3lame","-b:a","192k"]
            elif fmt in ["aac","m4a"]: cmd += ["-vn","-c:a","aac","-b:a","192k"]
            elif fmt=="flac": cmd += ["-vn","-c:a","flac"]
            elif fmt in ["ogg","opus"]: cmd += ["-vn","-c:a","libopus","-b:a","192k"]
            elif fmt=="wav": cmd += ["-vn","-c:a","pcm_s16le"]
            cmd += ["-threads",str(thr),"-y",dst]
            self.log_write(f"[{i+1}/{total}] Convertiendo: {os.path.basename(f)}")
            self.prog["value"]=int((i/total)*100); self.update_idletasks()
            try: subprocess.run(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
            except Exception as e: self.log_write(str(e))
        self.prog["value"]=100; self.running=False
        self.btnconv.config(state="normal"); self.btnstop.config(state="disabled")
        self.stat.config(text="Finalizado")

if __name__=="__main__":
    App().mainloop()
