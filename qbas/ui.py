"""Componentes visuales TUI para QBASCONVERTER"""
import os
import sys
from typing import List

class Colors:
    RESET = '\033[0m'
    BOLD = '\033[1m'
    DIM = '\033[2m'
    ITALIC = '\033[3m'
    UNDERLINE = '\033[4m'
    CYAN = '\033[96m'
    CYAN_DARK = '\033[36m'
    GREEN = '\033[92m'
    GREEN_DARK = '\033[32m'
    RED = '\033[91m'
    RED_DARK = '\033[31m'
    YELLOW = '\033[93m'
    YELLOW_DARK = '\033[33m'
    BLUE = '\033[94m'
    MAGENTA = '\033[95m'
    WHITE = '\033[97m'
    GRAY = '\033[90m'
    GRAY_LIGHT = '\033[37m'
    BG_DARK = '\033[48;5;235m'
    BG_CYAN = '\033[48;5;39m'
    BG_GREEN = '\033[48;5;46m'
    BG_RED = '\033[48;5;196m'
    BG_YELLOW = '\033[48;5;226m'

def colorize(text: str, color: str) -> str:
    if not sys.stdout.isatty():
        return text
    return f"{color}{text}{Colors.RESET}"

def clear_screen():
    os.system('cls' if os.name == 'nt' else 'clear')

def strip_ansi(text: str) -> str:
    import re
    ansi_escape = re.compile(r'\x1B(?:[@-Z\\-_]|\[[0-?]*[ -/]*[@-~])')
    return ansi_escape.sub('', text)

def draw_box(title: str, content: str, width: int = 100, color_title: str = Colors.BOLD + Colors.CYAN) -> None:
    inner = width - 4
    print(colorize("╔" + "═" * (width - 2) + "╗", Colors.GRAY))
    title_stripped = strip_ansi(title)
    title_pad = inner - len(title_stripped)
    print(colorize("║ ", Colors.GRAY) + colorize(title, color_title) + " " * max(0, title_pad) + colorize(" ║", Colors.GRAY))
    print(colorize("╠" + "═" * (width - 2) + "╣", Colors.GRAY))
    for line in content.split('\n'):
        if line.strip() == '':
            print(colorize("║ " + " " * inner + " ║", Colors.GRAY))
            continue
        line_stripped = strip_ansi(line)
        pad = inner - len(line_stripped)
        print(colorize("║ ", Colors.GRAY) + line + " " * max(0, pad) + colorize(" ║", Colors.GRAY))
    print(colorize("╚" + "═" * (width - 2) + "╝", Colors.GRAY))

def draw_progress_bar(percent: float, width: int = 45, show_speed: str = "") -> str:
    percent = max(0, min(100, percent))
    filled = int(width * percent / 100)
    bar = "█" * filled + "░" * (width - filled)
    if percent >= 90:
        color = Colors.GREEN
    elif percent >= 60:
        color = Colors.CYAN
    elif percent >= 30:
        color = Colors.YELLOW
    else:
        color = Colors.GRAY
    speed_part = f" {show_speed}" if show_speed else ""
    return f"[{colorize(bar, color)}] {colorize(f'{percent:5.1f}%', color)}{speed_part}"

def draw_hline(width: int = 100, char: str = "─") -> str:
    return char * width

def print_header(version: str, profile_name: str, gpu_name: str, cores: int, gpu_temp: int, ram_str: str, time_str: str = "") -> None:
    header_left = f"★ QBASCONVERTER {colorize(version, Colors.GREEN)}"
    header_mid = f"PERFIL: {colorize(profile_name, Colors.CYAN)}"
    header_right = f"GPU: {colorize(gpu_name[:25], Colors.YELLOW)} · {cores} NÚCLEOS · {gpu_temp}°C · RAM: {ram_str}"
    if time_str:
        header_right += f" · SESIÓN: {colorize(time_str, Colors.GREEN)}"
    print(colorize(header_left, Colors.BOLD + Colors.CYAN) + " │ " + header_mid + " │ " + header_right)
    print(colorize("═" * 100, Colors.GRAY))
