"""Panel lateral de navegación"""
from qbas.ui import Colors, colorize

class Sidebar:
    def __init__(self):
        self.sections = [
            ("ENG_CORE", "v3.1.8", False),
            ("Pipelines", "", False),
            ("Batch Queue", "(ACTIVO)", True),
            ("Codec Studio", "", False),
            ("Telemetry", "", False),
            ("Execution Log", "", False),
            ("Discos & Red", "", False),
        ]
        self.active = 2

    def render(self, hardware, width: int = 22):
        lines = []
        lines.append(colorize("╔" + "═" * (width-2) + "╗", Colors.GRAY))
        for i, (name, extra, active) in enumerate(self.sections):
            if i == self.active:
                marker = colorize("▌", Colors.BG_BLUE + Colors.WHITE)
                txt = colorize(f" {name} {extra}", Colors.WHITE)
                lines.append(marker + txt)
            else:
                marker = colorize("▌", Colors.CYAN_DARK)
                txt = colorize(f" {name} {extra}", Colors.GRAY_LIGHT)
                lines.append(marker + txt)
        lines.append(colorize("╠" + "═" * (width-2) + "╣", Colors.GRAY))
        # discos
        for d in hardware.disks[:4]:
            name = d['name'][:12]
            barw = 10
            pct = d['percent']
            filled = int(barw * pct / 100)
            bar = "▓" * filled + "░" * (barw - filled)
            c = Colors.GREEN if pct < 70 else (Colors.YELLOW if pct < 90 else Colors.RED)
            lines.append(f" {name:<12} [{colorize(bar,c)}] {pct}%")
        lines.append(colorize("╚" + "═" * (width-2) + "╝", Colors.GRAY))
        return "\n".join(lines)
