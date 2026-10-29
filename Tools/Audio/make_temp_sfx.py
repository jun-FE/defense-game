"""임시 효과음 만들기(코드로 합성, 저작권 걱정 없음): Assets/Resources/Sfx/<이름>.wav
사용: python3 Tools/Audio/make_temp_sfx.py
Artlist 요금제가 효과음 다운로드를 지원하면 같은 이름의 파일로 바꿔 넣기만 하면 된다(목록·고른 후보는 README "효과음").
모두 44.1kHz 모노 16비트, 끝은 자연스럽게 사라지고 앞뒤 공백 없음."""
import os
import wave
import numpy as np

SR = 44100
OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))), 'Assets/Resources/Sfx')
rng = np.random.default_rng(3)


def t(sec):
    return np.arange(int(SR * sec)) / SR


def env(n, attack=0.005, decay=6.0):
    x = np.arange(n) / SR
    a = np.clip(x / max(attack, 1e-4), 0, 1)
    return a * np.exp(-x * decay)


def bell(freq, sec, decay=6.0, partials=((1, 1), (2.76, 0.4), (5.4, 0.2)), attack=0.002):
    x = t(sec)
    s = sum(a * np.sin(2 * np.pi * freq * m * x) * np.exp(-x * decay * (1 + 0.5 * m)) for m, a in partials)
    return s * np.clip(x / attack, 0, 1)


def noise(sec):
    return rng.standard_normal(int(SR * sec))


def lowpass(x, cutoff):
    # 한 극 저역 통과(간단)
    a = np.exp(-2 * np.pi * cutoff / SR)
    y = np.zeros_like(x)
    acc = 0.0
    for i, v in enumerate(x):
        acc = (1 - a) * v + a * acc
        y[i] = acc
    return y


def highpass(x, cutoff):
    return x - lowpass(x, cutoff)


def sweep(f0, f1, sec, shape='sin'):
    x = t(sec)
    f = f0 * (f1 / f0) ** (x / sec)
    ph = 2 * np.pi * np.cumsum(f) / SR
    return np.sin(ph)


def mix(*parts):
    n = max(len(p) for _, p in parts)
    out = np.zeros(n)
    for off, p in parts:
        i = int(off * SR)
        out[i:i + len(p)] += p[:max(0, n - i)]
    return out


def pluck(freq, sec, damp=0.996):
    """카플러스-스트롱 현(활시위 튕김)."""
    n = int(SR * sec)
    period = int(SR / freq)
    buf = rng.uniform(-1, 1, period)
    out = np.zeros(n)
    for i in range(n):
        out[i] = buf[i % period]
        buf[i % period] = damp * 0.5 * (buf[i % period] + buf[(i + 1) % period])
    return out


def save(name, x, gain=0.9):
    x = np.asarray(x, float)
    peak = np.max(np.abs(x)) or 1
    x = x / peak * gain
    fade = min(len(x), int(SR * 0.01))
    x[-fade:] *= np.linspace(1, 0, fade)
    data = (np.clip(x, -1, 1) * 32767).astype('<i2')
    with wave.open(os.path.join(OUT, name + '.wav'), 'wb') as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(data.tobytes())
    print(f'{name:16s} {len(x) / SR:.2f}초')


def sparkle(sec=0.7, base=1800, count=9, spread=0.35):
    parts = []
    for i in range(count):
        f = base * 2 ** (rng.uniform(0, 1.5))
        parts.append((rng.uniform(0, spread), bell(f, 0.35, decay=9) * rng.uniform(0.4, 1)))
    return mix(*parts)


os.makedirs(OUT, exist_ok=True)

# 탐색
save('reveal', mix((0, sparkle(0.8, 1600, 10, 0.3)), (0, bell(880, 0.6, 5) * 0.5), (0, lowpass(noise(0.4), 3000) * env(int(SR * 0.4), 0.02, 9) * 0.25)), 0.7)
save('crystal', mix((0, bell(1568, 0.6, 5)), (0.07, bell(2093, 0.7, 5)), (0.0, sparkle(0.5, 2400, 5, 0.2) * 0.4)), 0.8)
save('bump', mix((0, lowpass(noise(0.25), 900) * env(int(SR * 0.25), 0.003, 18)), (0, 0.6 * sweep(160, 70, 0.2) * env(int(SR * 0.2), 0.002, 20))), 0.8)
save('step', lowpass(highpass(noise(0.12), 300), 2500) * env(int(SR * 0.12), 0.004, 35), 0.5)

# 타워 공격
save('tw_lamp', mix((0, sweep(900, 2400, 0.25) * env(int(SR * 0.25), 0.005, 10) * 0.6), (0, highpass(noise(0.3), 2000) * env(int(SR * 0.3), 0.01, 14) * 0.4), (0.03, bell(1320, 0.4, 8) * 0.5)), 0.65)
save('tw_scissors', mix((0, highpass(noise(0.05), 3000) * env(int(SR * 0.05), 0.001, 80)), (0.06, highpass(noise(0.07), 2500) * env(int(SR * 0.07), 0.001, 60) * 1.2), (0.06, bell(3200, 0.15, 30) * 0.4)), 0.75)
clicks = [(i * 0.028, highpass(noise(0.012), 1500) * env(int(SR * 0.012), 0.0005, 300) * (0.6 + 0.4 * (i % 2))) for i in range(8)]
save('tw_sewing', mix(*clicks, (0, sweep(220, 260, 0.24) * env(int(SR * 0.24), 0.01, 8) * 0.25)), 0.6)
save('tw_tape', mix((0, lowpass(highpass(noise(0.22), 600), 6000) * env(int(SR * 0.22), 0.04, 10) * np.linspace(0.3, 1, int(SR * 0.22))), (0.2, highpass(noise(0.03), 2000) * env(int(SR * 0.03), 0.001, 90) * 1.5)), 0.65)
save('tw_pin', mix((0, bell(4200, 0.12, 35)), (0.015, highpass(noise(0.01), 4000) * 0.5)), 0.5)
save('tw_tagbow', mix((0, pluck(196, 0.4) * env(int(SR * 0.4), 0.001, 7)), (0, highpass(noise(0.08), 1500) * env(int(SR * 0.08), 0.002, 40) * 0.6)), 0.7)

# 전투 기타
save('hit', mix((0, lowpass(noise(0.12), 1800) * env(int(SR * 0.12), 0.001, 40)), (0, 0.7 * sweep(220, 110, 0.1) * env(int(SR * 0.1), 0.001, 40))), 0.55)
save('kill', mix((0, sweep(500, 1400, 0.18) * env(int(SR * 0.18), 0.002, 14) * 0.6), (0.05, sparkle(0.4, 1400, 5, 0.12) * 0.6), (0, lowpass(noise(0.25), 2500) * env(int(SR * 0.25), 0.01, 12) * 0.4)), 0.7)
save('core_hit', mix((0, sweep(110, 45, 0.7) * env(int(SR * 0.7), 0.003, 5)), (0, lowpass(noise(0.6), 400) * env(int(SR * 0.6), 0.005, 6) * 0.8), (0.02, bell(311, 0.8, 4) * 0.25)), 0.9)
save('build', mix((0, lowpass(noise(0.15), 1200) * env(int(SR * 0.15), 0.002, 25)), (0, sweep(180, 120, 0.12) * env(int(SR * 0.12), 0.002, 25) * 0.6), (0.05, bell(1047, 0.4, 7) * 0.4), (0.1, bell(1568, 0.4, 7) * 0.3)), 0.75)
save('upgrade', mix(*[(i * 0.06, bell(f, 0.5, 6)) for i, f in enumerate((784, 988, 1175, 1568))], (0.2, sparkle(0.5, 2000, 6, 0.25) * 0.4)), 0.75)
save('wave_start', mix((0, bell(392, 1.6, 2.2, ((1, 1), (2.0, 0.5), (3.01, 0.3), (4.2, 0.15)))), (0, sweep(60, 50, 1.2) * env(int(SR * 1.2), 0.05, 3) * 0.5), (0.05, sparkle(0.8, 1500, 6, 0.6) * 0.25)), 0.85)
save('victory', mix(*[(i * 0.13, bell(f, 0.9 if i == 4 else 0.5, 4 if i == 4 else 6)) for i, f in enumerate((523, 659, 784, 1047, 1319))], (0.55, sparkle(1.0, 1800, 10, 0.6) * 0.4)), 0.8)
save('defeat', mix(*[(i * 0.22, bell(f, 0.9, 3.5, ((1, 1), (2.0, 0.3)))) for i, f in enumerate((440, 415, 349, 294))], (0.6, lowpass(noise(0.8), 500) * env(int(SR * 0.8), 0.1, 4) * 0.3)), 0.75)

# UI
save('ui_click', mix((0, lowpass(highpass(noise(0.03), 800), 4000) * env(int(SR * 0.03), 0.0005, 120)), (0, bell(1800, 0.06, 60) * 0.5)), 0.5)
save('ui_open', lowpass(highpass(noise(0.25), 700), 5000) * env(int(SR * 0.25), 0.03, 12) * np.sin(np.linspace(0, np.pi, int(SR * 0.25))), 0.5)
print('완료 ->', OUT)
