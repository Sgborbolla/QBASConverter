/* QBAS Suite - arranque de la interfaz.
   Todo se habla con el servidor local por /api. El progreso en vivo llega
   por WebSocket a /api/ws. */

const $ = (sel) => document.querySelector(sel);

async function api(ruta, opciones) {
  const r = await fetch(ruta, opciones);
  if (!r.ok) throw new Error(r.status + " " + r.statusText);
  return r.json();
}

async function arrancar() {
  try {
    const v = await api("/api/version");
    $("#version").textContent = v.version;
    $("#estado").textContent = "en linea";
    $("#estado").className = "chip verde";
  } catch {
    $("#estado").textContent = "sin respuesta";
    $("#estado").className = "chip rojo";
    return;
  }

  try {
    const d = await api("/api/dispositivos");
    $("#unidades").textContent = d.length + " conectada(s)";
  } catch {
    $("#unidades").textContent = "—";
  }

  $("#nucleos").textContent = navigator.hardwareConcurrency + " (" + (navigator.hardwareConcurrency || 1) + " en uso)";

  conectar();
}

function conectar() {
  const url = (location.protocol === "https:" ? "wss://" : "ws://") + location.host + "/api/ws";
  const ws = new WebSocket(url);
  ws.onmessage = (ev) => {
    let msg;
    try { msg = JSON.parse(ev.data); } catch { return; }
    if (msg.tipo === "progreso") {
      const p = msg.datos;
      document.title = `QBAS Suite — ${Math.round(p.porcentaje)}%`;
    }
  };
  ws.onclose = () => setTimeout(conectar, 3000);
}

arrancar();
