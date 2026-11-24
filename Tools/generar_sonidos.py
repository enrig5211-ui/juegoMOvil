#!/usr/bin/env python3
"""Genera los sonidos originales del juego (sin descargas): noche, música de tensión,
helicóptero, radio, alarma, bengala, esposas y sonidos de la selección.
Uso: python3 Tools/generar_sonidos.py  ->  Assets/Audio/Generado/*.wav (22 050 Hz, mono, 16 bits)
"""
import math, os, random, struct, wave

SR = 22050
OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "Audio", "Generado")
random.seed(20261107)

def silence(seconds): return [0.0] * int(seconds * SR)
def noise(n): return [random.uniform(-1, 1) for _ in range(n)]

def lowpass(x, cutoff):
    a = 1 - math.exp(-2 * math.pi * cutoff / SR); y = []; v = 0.0
    for s in x: v += a * (s - v); y.append(v)
    return y

def highpass(x, cutoff):
    low = lowpass(x, cutoff); return [s - l for s, l in zip(x, low)]

def bandpass(x, lo, hi): return lowpass(highpass(x, lo), hi)

def add(dst, src, at=0, gain=1.0, wrap=False):
    n = len(dst)
    for i, s in enumerate(src):
        j = at + i
        if wrap: j %= n
        elif j >= n: break
        dst[j] += s * gain

def tone(freq, seconds, harmonics=(1.0,), decay=0.0, attack=0.005):
    n = int(seconds * SR); out = []
    for i in range(n):
        t = i / SR
        env = min(1.0, t / attack) if attack > 0 else 1.0
        if decay > 0: env *= math.exp(-t / decay)
        out.append(env * sum(a * math.sin(2 * math.pi * freq * (k + 1) * t) for k, a in enumerate(harmonics)))
    return out

def square(freq, seconds, decay=0.0):
    return tone(freq, seconds, harmonics=(1.0, 0, 1 / 3, 0, 1 / 5), decay=decay)

def fade(x, fade_in, fade_out):
    n = len(x); a = int(fade_in * SR); b = int(fade_out * SR)
    for i in range(min(a, n)): x[i] *= i / a
    for i in range(min(b, n)): x[n - 1 - i] *= i / b
    return x

def loop_crossfade(x, seconds):
    """Hace que el final empalme con el principio: el sobrante se mezcla al inicio."""
    k = int(seconds * SR); body = x[:-k]; tail = x[-k:]
    for i in range(k):
        w = i / k; body[i] = body[i] * w + tail[i] * (1 - w)
    return body

def normalize(x, peak=0.9):
    m = max(abs(s) for s in x) or 1.0
    return [s * peak / m for s in x]

def write(name, x, peak=0.9):
    x = normalize(x, peak); os.makedirs(OUT, exist_ok=True)
    with wave.open(os.path.join(OUT, name + ".wav"), "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1, min(1, s)) * 32767)) for s in x))
    print(f"{name}.wav  {len(x) / SR:.2f} s")

def night(seconds=16):
    total = silence(seconds + 1)
    wind = lowpass(lowpass(noise(len(total)), 350), 350)
    for i, s in enumerate(wind):
        total[i] += s * 0.6 * (0.6 + 0.4 * math.sin(2 * math.pi * i / SR / (seconds + 1)))
    loop = loop_crossfade(total, 1.0)
    for cricket in range(4):
        pitch = random.uniform(4300, 5200); gain = random.uniform(0.18, 0.3); t = random.uniform(0, 1)
        while t < seconds:
            for p in range(random.choice((3, 4))):
                add(loop, tone(pitch, 0.012, decay=0.006, attack=0.001), int((t + p * 0.028) * SR), gain, wrap=True)
            t += random.uniform(0.6, 1.1)
    return loop

def tension(seconds=32):
    n = int(seconds * SR); out = [0.0] * n
    for i in range(n):
        t = i / SR; swell = 0.75 + 0.25 * math.sin(2 * math.pi * t / 16)
        out[i] = swell * (0.35 * math.sin(2 * math.pi * 55 * t) + 0.22 * math.sin(2 * math.pi * 82.5 * t) + 0.08 * math.sin(2 * math.pi * 110 * t + 0.6 * math.sin(2 * math.pi * 0.25 * t)))
    air = loop_crossfade(lowpass(noise(n + SR), 900), 1.0); add(out, air, 0, 0.05)  # ciclos enteros y fundido: bucle sin clic
    for beat in range(seconds):          # latido: lub-dub cada segundo
        add(out, tone(48, 0.18, decay=0.06), int(beat * SR), 0.55, wrap=True)
        add(out, tone(42, 0.15, decay=0.05), int((beat + 0.24) * SR), 0.35, wrap=True)
    notes = [110.0, 130.81, 164.81, 174.61, 164.81, 130.81, 110.0, 98.0]
    for k, f in enumerate(notes):        # motivo grave en la menor, una nota cada 4 s
        add(out, tone(f, 2.5, harmonics=(1.0, 0.35, 0.12), decay=0.8), int(k * 4 * SR), 0.28, wrap=True)
    for k in range(4):                   # nota alta inquietante cada 8 s
        pad = tone(880 * (1 if k % 2 == 0 else 0.944), 3.0, attack=1.2, decay=1.2)
        add(out, pad, int((k * 8 + 2) * SR), 0.05, wrap=True)
    return out

def helicopter(seconds=6.0):
    n = int(seconds * SR); body = lowpass(noise(n), 500); out = []
    for i in range(n):
        t = i / SR; blade = (0.5 + 0.5 * math.cos(2 * math.pi * 11 * t)) ** 4
        out.append(body[i] * (0.35 + 1.6 * blade) + 0.05 * math.sin(2 * math.pi * 1150 * t))
    return fade(out, 1.6, 2.2)

def radio():
    out = silence(1.05)
    add(out, [s * 0.7 for s in bandpass(noise(int(0.14 * SR)), 1800, 4200)], 0)
    add(out, tone(1250, 0.09), int(0.15 * SR), 0.5)
    chatter = bandpass(noise(int(0.5 * SR)), 300, 2800)
    chatter = [s * (0.5 + 0.5 * math.sin(2 * math.pi * 6.5 * i / SR + math.sin(2 * math.pi * 1.7 * i / SR))) for i, s in enumerate(chatter)]
    add(out, chatter, int(0.28 * SR), 0.9)
    add(out, tone(1000, 0.08), int(0.85 * SR), 0.45)
    return fade(out, 0.004, 0.03)

def alarm():
    out = silence(1.25)
    for k in range(6): add(out, square(950 if k % 2 == 0 else 760, 0.16, decay=0.3), int(k * 0.2 * SR), 0.6)
    return out

def flare(seconds=2.0):
    total = [s * 0.35 for s in highpass(noise(int((seconds + 0.3) * SR)), 2200)]
    loop = loop_crossfade(total, 0.3)
    for _ in range(int(seconds * 35)):
        add(loop, [s for s in highpass(noise(int(0.004 * SR)), 1500)], random.randrange(len(loop)), random.uniform(0.3, 0.8), wrap=True)
    return loop

def cuffs():
    out = silence(0.45)
    for k, gain in enumerate((0.5, 0.55, 0.6, 1.0)):
        at = int((0.04 + k * 0.08) * SR)
        add(out, bandpass(noise(int(0.012 * SR)), 2500, 6000), at, gain)
        add(out, tone(2600 + k * 120, 0.06, decay=0.015, attack=0.001), at, gain * 0.5)
    return out

def select_move():
    out = square(660, 0.035, decay=0.03); add(out, [], 0)
    return out + square(990, 0.035, decay=0.03)

def select_confirm():
    out = silence(0.55)
    for k, f in enumerate((523.25, 659.25, 783.99, 1046.5)): add(out, square(f, 0.2, decay=0.08), int(k * 0.075 * SR), 0.6)
    return fade(out, 0.002, 0.08)


# ---------- Explosivos y destrucción ----------
def explosion(seconds=2.2):
    n = int(seconds * SR); out = silence(seconds)
    body = lowpass(noise(n), 900); rumble = lowpass(noise(n), 140)
    for i in range(n):
        t = i / SR; env = math.exp(-t / 0.35); low = math.exp(-t / 0.9)
        out[i] = body[i] * env * 1.4 + rumble[i] * low * 3.5 + math.sin(2 * math.pi * (55 - 25 * t) * t) * low * 0.5
    crack = highpass(noise(int(0.03 * SR)), 1800); add(out, crack, 0, 0.8)
    return fade(out, 0.001, 0.4)

def whistle(seconds=1.6):
    n = int(seconds * SR); out = []; phase = 0.0
    for i in range(n):
        t = i / SR; f = 1500 - 900 * (t / seconds) ** 1.4; phase += 2 * math.pi * f / SR
        out.append(math.sin(phase) * (0.25 + 0.75 * t / seconds))
    return fade(out, 0.15, 0.05)

def scan_ping():
    out = silence(1.3)
    for k, f in enumerate((1318.5, 1760.0)):
        add(out, tone(f, 0.5, harmonics=(1.0, 0.3), decay=0.18, attack=0.002), int(k * 0.16 * SR), 0.6)
    sweep = []; phase = 0.0
    for i in range(int(0.6 * SR)):
        t = i / SR; f = 400 + 1600 * t; phase += 2 * math.pi * f / SR; sweep.append(math.sin(phase) * math.exp(-t / 0.3) * 0.3)
    add(out, sweep, int(0.4 * SR))
    return fade(out, 0.002, 0.2)

def build():
    out = silence(0.8)
    for k in range(3):
        thud = lowpass(noise(int(0.12 * SR)), 300)
        thud = [s * math.exp(-i / SR / 0.05) for i, s in enumerate(thud)]
        add(out, thud, int(k * 0.18 * SR), 1.0 - k * 0.15)
        add(out, bandpass(noise(int(0.08 * SR)), 800, 3000), int(k * 0.18 * SR), 0.15)
    return out

def fire_loop(seconds=6):
    n = int(seconds * SR); out = [s * 0.35 for s in lowpass(noise(n), 500)]
    for _ in range(int(seconds * 40)):
        pop = highpass(noise(int(0.004 * SR)), 2000); add(out, pop, random.randrange(n), random.uniform(0.2, 0.9), wrap=True)
    return loop_crossfade(out, 0.3)

def bounce():
    out = silence(0.45)
    for k, gain in enumerate((1.0, 0.5, 0.25)):
        add(out, tone(2100 + k * 90, 0.07, harmonics=(1.0, 0.5, 0.25), decay=0.02, attack=0.001), int(k * 0.11 * SR), gain)
    return out

def crash(seconds=0.9):
    n = int(seconds * SR); out = silence(seconds)
    for k in range(9):
        at = int(random.uniform(0, 0.35) * SR); length = int(random.uniform(0.03, 0.09) * SR)
        snap = bandpass(noise(length), 300, 2500); snap = [s * math.exp(-i / SR / 0.03) for i, s in enumerate(snap)]
        add(out, snap, at, random.uniform(0.4, 1.0))
    add(out, [s * math.exp(-i / SR / 0.25) for i, s in enumerate(lowpass(noise(n), 200))], 0, 1.5)
    return fade(out, 0.001, 0.2)


# ---------- Vehículos ----------
def engine_loop(seconds=4.0):
    """Motor diésel en ralentí: pulsos graves a 28 Hz con ruido filtrado."""
    n = int(seconds * SR); rumble = lowpass(noise(n), 180); out = []
    for i in range(n):
        t = i / SR; pulse = 0.55 + 0.45 * math.sin(2 * math.pi * 28 * t) ** 2
        out.append(rumble[i] * 2.2 * pulse + 0.25 * math.sin(2 * math.pi * 56 * t))
    return loop_crossfade(out, 0.25)

def plane_loop(seconds=4.0):
    """Avioneta de hélice: zumbido a 95 Hz con armónicos y aire."""
    n = int(seconds * SR); air = bandpass(noise(n), 300, 1800); out = []
    for i in range(n):
        t = i / SR; buzz = sum(math.sin(2 * math.pi * 95 * k * t) / k for k in (1, 2, 3, 4))
        out.append(buzz * 0.35 * (0.8 + 0.2 * math.sin(2 * math.pi * 6 * t)) + air[i] * 0.6)
    return loop_crossfade(out, 0.25)


def horn():
    """Bocina de camioneta: dos tonos desafinados, dos toques."""
    out = silence(0.95)
    for at, length in ((0.0, 0.22), (0.32, 0.55)):
        n = int(length * SR); blast = []
        for i in range(n):
            t = i / SR; env = min(1.0, t / 0.01) * min(1.0, (length - t) / 0.03)
            blast.append(env * (math.sin(2 * math.pi * 410 * t) + 0.8 * math.sin(2 * math.pi * 517 * t) + 0.3 * math.sin(2 * math.pi * 820 * t)))
        add(out, blast, int(at * SR), 0.6)
    return out

def skid(seconds=0.9):
    """Derrape: chirrido de goma con ruido de grava."""
    n = int(seconds * SR); squeal = []; phase = 0.0
    for i in range(n):
        t = i / SR; f = 1350 - 350 * t / seconds + 60 * math.sin(2 * math.pi * 23 * t); phase += 2 * math.pi * f / SR
        squeal.append(math.sin(phase) * (1 - t / seconds) * 0.5)
    gravel = [s * (1 - i / n) for i, s in enumerate(bandpass(noise(n), 500, 4000))]
    add(squeal, gravel, 0, 0.9)
    return fade(squeal, 0.01, 0.15)

if __name__ == "__main__":
    write("noche_grillos", night(), 0.6)
    write("musica_tension", tension(), 0.8)
    write("helicoptero", helicopter())
    write("radio", radio())
    write("alarma", alarm(), 0.8)
    write("bengala", flare(), 0.6)
    write("esposas", cuffs())
    write("seleccion_mover", select_move(), 0.6)
    write("seleccion_confirmar", select_confirm(), 0.7)
    write("explosion", explosion())
    write("silbido", whistle(), 0.7)
    write("rastreo", scan_ping(), 0.7)
    write("muralla", build(), 0.8)
    write("fuego", fire_loop(), 0.6)
    write("granada_rebote", bounce(), 0.6)
    write("destruccion", crash(), 0.8)
    write("motor", engine_loop(), 0.7)
    write("avion", plane_loop(), 0.7)
    write("bocina", horn(), 0.7)
    write("derrape", skid(), 0.7)
