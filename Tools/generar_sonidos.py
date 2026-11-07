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
