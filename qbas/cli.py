"""Dashboard principal TUI - QBASCONVERTER v3.1.8-pro"""
import time
import os
from pathlib import Path

from qbas.ui import Colors, colorize, draw_box, draw_progress_bar, clear_screen, print_header, strip_ansi
from qbas.hardware import Hardware
from qbas.presets import PresetManager
from qbas.convert import Converter
from qbas.queue import ProcessingQueue
from qbas.session import SessionManager
from qbas.automation import XpressEngine
from qbas.telemetry import Telemetry
from qbas.history import History
from qbas.sidebar import Sidebar

class Dashboard:
    def __init__(self):
        self.hardware = Hardware()
        self.presets = PresetManager()
        self.session = SessionManager()
        self.automation = XpressEngine()
        self.converter = Converter(self.hardware)
        self.queue = ProcessingQueue(self.converter, max_active=3)
        self.telemetry = Telemetry()
        self.history = History()
        self.sidebar = Sidebar()
        self.running = True
        self.show_help = False
        self.use_gpu = self.hardware.nvenc_supported
        self.turbo = True
        self.threads = min(self.hardware.cores_logical, 16)
        self.max_active = 3
        self.core_affinity = self.hardware.core_affinity[:]
        
        # preguntar reanudar sesión
        if self.session.has_pending_session():
            clear_screen()
            print(colorize("╔══════════════════════════════════════════════════════════════╗", Colors.CYAN))
            print(colorize("║", Colors.CYAN) + colorize("  SE DETECTÓ UNA SESIÓN ANTERIOR CON TRABAJOS PENDIENTES", Colors.BOLD + Colors.YELLOW) + colorize("  ║", Colors.CYAN))
            print(colorize("╚══════════════════════════════════════════════════════════════╝", Colors.CYAN))
            print(f"\nTrabajos guardados: {len(self.session.get_pending_jobs())}")
            try:
                resp = input(f"\n¿Continuar sesión anterior? [{colorize('S',Colors.GREEN)}/{colorize('n',Colors.RED)}]: ").strip().lower()
                if resp == 's' or resp == 'y' or resp == '':
                    print(colorize("[*] Restaurando sesión...", Colors.CYAN))
                    time.sleep(1)
                else:
                    self.session.clear_pending_jobs()
                    print(colorize("[*] Iniciando sesión limpia...", Colors.GRAY))
                    time.sleep(1)
            except:
                pass

    def draw_layout(self):
        clear_screen()
        m = self.telemetry.metrics(self.hardware, self.queue)
        print_header("v3.1.8-pro", self.presets.get_active()['name'], 
                     self.hardware.gpu_name[:30], self.hardware.cores_logical,
                     self.hardware.gpu_temp, self.hardware.get_ram_str(), m['uptime'])
        
        # Layout 2 columnas
        sidebar_txt = self.sidebar.render(self.hardware, width=24)
        sidebar_lines = sidebar_txt.split('\n')
        
        # Main content
        main_lines = []
        # REGLAS XPRESS
        reglas = f"[{'✓' if self.automation.rules['mantener_carpeta'] else ' '}] MANTENER CARPETA   [{'✓' if self.automation.rules['sin_mostrar_ventana'] else ' '}] SIN MOSTRAR VENTANA   [{'✓' if self.automation.rules['subtitulos_forzados'] else ' '}] SUBTÍTULOS FORZADOS   [{'✓' if self.automation.rules['incluir_nombre_perfil'] else ' '}] NOMBRE DE PERFIL"
        main_lines.append(draw_box_str("REGLAS DE AUTOMATIZACIÓN (XPRESS ENGINE)", reglas, 74))
        main_lines.append("")
        # SALIDA Y CALIDAD
        prof = self.presets.get_active()
        salida = f"FORMATO: {colorize(prof['format'].upper(), Colors.CYAN)}   |   CALIDAD: {colorize(prof['quality'], Colors.GREEN)}   |   VÍDEO: {prof['video_codec'].upper()}   |   AUDIO: {prof['audio_bitrate']}   |   PERFIL: {prof['name']}"
        main_lines.append(draw_box_str("SALIDA Y CALIDAD", salida, 74))
        main_lines.append("")
        # AVANZADO
        avanzado = f"CÓDEC: {prof['video_codec'].upper()}   RESOLUCIÓN: {prof['resolution'].upper()}   FPS: {prof['fps'].upper()}   BITRATE AUDIO: {prof['audio_bitrate']}   SUBTÍTULOS: {prof['subtitles'].upper()}"
        main_lines.append(draw_box_str("AVANZADO // MATRIZ DE TRANSCODIFICACIÓN", avanzado, 74))
        main_lines.append("")
        # RENDIMIENTO
        gpu_str = "NVENC_CUDA" if self.use_gpu and self.hardware.nvenc_supported else "SOFTWARE"
        rend = f"ACELERACIÓN: {colorize(gpu_str, Colors.GREEN if self.use_gpu else Colors.YELLOW)}   |   TURBO: {colorize('ACTIVO' if self.turbo else 'INACTIVO', Colors.GREEN if self.turbo else Colors.GRAY)}   |   NÚCLEOS: {colorize(str(self.threads), Colors.CYAN)}   |   ACTIVOS: {colorize(str(self.max_active), Colors.CYAN)}   |   MATRIZ: C1-C{min(16, self.hardware.cores_logical)}"
        main_lines.append(draw_box_str("RENDIMIENTO & ACELERACIÓN", rend, 74))
        main_lines.append("")
        # COLA
        qview = self.queue.get_queue_view(5)
        qcontent = ""
        for j in qview:
            bar = draw_progress_bar(j['progress'], 38, f"| {j['speed']}x" if j['speed'] > 0 else "")
            status_c = Colors.GREEN if j['status']=='completed' else (Colors.CYAN if j['status']=='running' else Colors.GRAY)
            qcontent += f"▶ {colorize(j['name'], status_c)}  {bar}  ETA {j['eta']//60:02d}:{j['eta']%60:02d}\n"
        if not qcontent:
            qcontent = f"{colorize('Sin trabajos en cola. Añade archivos para empezar.', Colors.GRAY)}"
        qs = self.queue.get_summary()
        main_lines.append(draw_box_str(f"COLA DE PROCESAMIENTO EN LOTE ({qs['running']} ACTIVOS)", qcontent.rstrip(), 74))
        main_lines.append("")
        # HISTORIAL
        h = self.history.stats()
        hist = f"COMPLETADOS: {colorize(str(h['ok']), Colors.GREEN)} OK   |   FALLOS: {colorize(str(h['fail']), Colors.RED)}   |   TOTAL: {h['total']}   |   ÚLTIMA: {h['last']['name'][:40] if h['last'] else colorize('N/A', Colors.GRAY)}"
        main_lines.append(draw_box_str("HISTORIAL COMPLETADO", hist, 74))
        
        # Render side by side
        print()
        maxh = max(len(sidebar_lines), len(main_lines))
        for i in range(maxh):
            sl = sidebar_lines[i] if i < len(sidebar_lines) else " " * 24
            ml = main_lines[i] if i < len(main_lines) else ""
            print(f"{sl} {ml}")
        
        # Footer
        print()
        elapsed = self.telemetry.uptime_str()
        footer = f"[{colorize('▶ CONVERTIR', Colors.GREEN)}] [{colorize('⏸ PAUSAR', Colors.YELLOW)}] [{colorize('■ DETENER', Colors.RED)}]   TIEMPO SESIÓN: {colorize(elapsed, Colors.GREEN)}   GPU: {m['gpu_load']}% {m['gpu_temp']}°C   Lote:{qs['total']}   Herramientas:2   {colorize('0 SALIR', Colors.GRAY)}"
        print(colorize("╔" + "═" * 98 + "╗", Colors.GRAY))
        print(colorize("║", Colors.GRAY) + f" {footer:<96} " + colorize("║", Colors.GRAY))
        print(colorize("╚" + "═" * 98 + "╝", Colors.GRAY))
        print(f"\n{colorize('[ESPACIO/ENTER] Navegar  |  [Q] Salir  |  [S] Guardar sesión  |  [R] Refrescar', Colors.DIM)}")

    def run(self):
        try:
            while self.running:
                self.hardware.update()
                self.telemetry.tick()
                self.draw_layout()
                time.sleep(2)
        except KeyboardInterrupt:
            self.running = False
            self.session.save()
            self.session.save_history()
            clear_screen()
            print(colorize("\n[✓] Sesión guardada correctamente.", Colors.GREEN))
            print(colorize("[*] Hasta pronto - QBASCONVERTER v3.1.8-pro\n", Colors.CYAN))

def draw_box_str(title, content, width=74):
    inner = width - 4
    lines = []
    lines.append(colorize("╔" + "═" * (width - 2) + "╗", Colors.GRAY))
    title_s = strip_ansi(title)
    lines.append(colorize("║ ", Colors.GRAY) + colorize(title, Colors.BOLD + Colors.CYAN) + " " * max(0, inner - len(title_s)) + colorize(" ║", Colors.GRAY))
    lines.append(colorize("╠" + "═" * (width - 2) + "╣", Colors.GRAY))
    for line in content.split('\n'):
        line_s = strip_ansi(line)
        lines.append(colorize("║ ", Colors.GRAY) + line + " " * max(0, inner - len(line_s)) + colorize(" ║", Colors.GRAY))
    lines.append(colorize("╚" + "═" * (width - 2) + "╝", Colors.GRAY))
    return "\n".join(lines)
