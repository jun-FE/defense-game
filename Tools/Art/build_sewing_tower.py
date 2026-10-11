"""재봉틀 타워 그림 만들기(Unity 없이): ArtSource/Towers/재봉틀/L{1,2,3}_{base,idle,attack}.png
→ Assets/Art/Towers/Sewing/ 의 게임용 프레임·아이콘·연출 조각.
사용: python3 Tools/Art/build_sewing_tower.py
- 초록 배경은 투명으로(slice_ui_sheet.key_green), 원본에 투명이 있으면 그대로.
- 생성 그림은 장마다 몇 픽셀씩 어긋나므로, 받침(아래쪽 다리) 위치를 기준 그림(base)에 맞춰 정렬한다.
- 256px 캔버스로 줄인다(받침 바닥이 캔버스 아래쪽 약 6%).
- 연출 조각: 빛나는 바늘(투사체), 별 불티, 빛 고리, 부드러운 빛 점."""
import os
import sys
import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
sys.path.insert(0, os.path.join(ROOT, 'Tools/Art'))
SRC = os.path.join(ROOT, 'ArtSource/Towers/재봉틀')
OUT = os.path.join(ROOT, 'Assets/Art/Towers/Sewing')
UI_OUT = os.path.join(ROOT, 'Assets/Art/Battle/UI')
WORK = 1024
SIZE = 256


def key(path):
    from importlib import import_module
    im = Image.open(path).convert('RGBA')
    if im.size != (WORK, WORK):
        im = im.resize((WORK, WORK), Image.LANCZOS)
    a = np.asarray(im, float) / 255.0
    if a[..., 3].min() < 0.5:          # 원본에 투명이 있으면 그대로
        return a
    key_green = import_module('slice_ui_sheet_lib').key_green
    rgb, alpha = key_green(a[..., :3])
    return np.dstack([rgb, alpha])


def legs_anchor(rgba):
    """받침 기준점: 불투명 픽셀의 맨 아래 줄과, 아래쪽 15% 구간의 가로 중심."""
    solid = rgba[..., 3] > 0.6
    ys, xs = np.nonzero(solid)
    bottom = ys.max()
    band = ys > bottom - WORK * 0.15
    return (xs[band].min() + xs[band].max()) / 2.0, bottom


def shift(rgba, dx, dy):
    out = np.zeros_like(rgba)
    h, w = rgba.shape[:2]
    dx, dy = int(round(dx)), int(round(dy))
    xs0, xs1 = max(0, -dx), min(w, w - dx)
    ys0, ys1 = max(0, -dy), min(h, h - dy)
    out[ys0 + dy:ys1 + dy, xs0 + dx:xs1 + dx] = rgba[ys0:ys1, xs0:xs1]
    return out


def save(rgba, path, size=SIZE):
    im = Image.fromarray((np.clip(rgba, 0, 1) * 255).astype('uint8'), 'RGBA').resize((size, size), Image.LANCZOS)
    im.save(path)
    return im


def soft_disc(n, power):
    y, x = np.mgrid[0:n, 0:n] / (n - 1) * 2 - 1
    d = np.sqrt(x * x + y * y)
    return np.clip(1 - d, 0, 1) ** power


def thread_ring(n, seed=7):
    """반듯한 동그라미 대신 '꿈실이 휘감기며 퍼지는' 고리: 굵기가 바뀌고 끊긴 실 가닥 3개 + 끝이 가늘어짐 + 약한 번짐."""
    rng = np.random.default_rng(seed)
    y, x = np.mgrid[0:n, 0:n] / (n - 1) * 2 - 1
    d = np.sqrt(x * x + y * y)
    ang = np.arctan2(y, x)
    out = np.zeros((n, n))
    for k in range(3):
        start = rng.uniform(0, 2 * np.pi)
        span = rng.uniform(1.3, 1.75) * np.pi             # 가닥 하나가 도는 각도(한 바퀴가 안 됨 → 틈)
        rel = np.mod(ang - start, 2 * np.pi)
        u = np.clip(rel / span, 0, 1)                      # 가닥을 따라 0→1
        inside = rel < span
        # 반지름이 물결치고(실이 흔들림) 가닥마다 조금씩 다르다, 끝으로 갈수록 바깥으로 풀린다
        r = 0.72 + 0.05 * k + 0.04 * np.sin(ang * (3 + k) + start) + 0.07 * u
        width = (0.025 + 0.05 * np.sin(np.pi * u) ** 0.8) * (1.0 - 0.25 * k)  # 가운데 굵고 양끝 뾰족
        core = np.clip(1 - np.abs(d - r) / np.maximum(width, 1e-3), 0, 1) ** 1.3
        out = np.maximum(out, np.where(inside, core * (0.55 + 0.45 * np.sin(np.pi * u)), 0))
    # 짧은 바늘땀(점선) 몇 개를 바깥에
    for _ in range(9):
        a0 = rng.uniform(0, 2 * np.pi)
        rel = np.abs(np.angle(np.exp(1j * (ang - a0))))
        r0 = rng.uniform(0.86, 0.95)
        dash = np.clip(1 - np.abs(d - r0) / 0.022, 0, 1) * np.clip(1 - rel / 0.09, 0, 1)
        out = np.maximum(out, dash * 0.8)
    # 약한 번짐
    glow = np.clip(1 - np.abs(d - 0.76) / 0.2, 0, 1) ** 2 * 0.18
    a = np.clip(np.maximum(out, glow), 0, 1)
    return np.dstack([np.ones((n, n, 3)), a])


def fx_sprites():
    n = 64
    glow = soft_disc(n, 2.0)
    Image.fromarray((np.dstack([np.ones((n, n, 3)), glow]) * 255).astype('uint8'), 'RGBA').save(os.path.join(OUT, 'fx_glow.png'))

    Image.fromarray((thread_ring(256) * 255).astype('uint8'), 'RGBA').save(os.path.join(OUT, 'fx_ring.png'))

    n = 48
    y, x = np.mgrid[0:n, 0:n] / (n - 1) * 2 - 1
    star = np.clip(1 - (np.abs(x) ** 0.5 + np.abs(y) ** 0.5) / 1.1, 0, 1) ** 1.2 + soft_disc(n, 3.0) * 0.6
    Image.fromarray((np.dstack([np.ones((n, n, 3)), np.clip(star, 0, 1)]) * 255).astype('uint8'), 'RGBA').save(os.path.join(OUT, 'fx_spark.png'))

    # 빛나는 바늘(오른쪽이 앞): 픽셀 아트 느낌으로 작게 그려 키운다
    w, h = 24, 7
    px = np.zeros((h, w, 4))
    for i in range(3, w - 1):
        px[3, i] = (0.85, 0.88, 0.95, 1)          # 몸통(은빛)
        px[2, i] = (0.62, 0.66, 0.78, 1) if i < w - 4 else (0, 0, 0, 0)
    px[3, w - 1] = (1, 1, 1, 1)                   # 뾰족한 끝
    px[2, 4], px[2, 5] = (0.2, 0.2, 0.3, 1), (0.2, 0.2, 0.3, 1)   # 바늘귀
    for i in range(0, 4):
        px[4 if i % 2 else 3, i] = (0.78, 0.6, 1.0, 1)             # 꿰인 꿈실
    img = Image.fromarray((px * 255).astype('uint8'), 'RGBA').resize((w * 4, h * 4), Image.NEAREST)
    halo = soft_disc(img.height * 3, 2.0)
    canvas = Image.new('RGBA', (img.width + 16, img.height + 16), (0, 0, 0, 0))
    canvas.alpha_composite(img, (8, 8))
    canvas.save(os.path.join(OUT, 'fx_needle.png'))


def main():
    os.makedirs(OUT, exist_ok=True)
    # slice_ui_sheet.py는 실행용 스크립트라 key_green만 꺼내 모듈로 쓴다
    src = open(os.path.join(ROOT, 'Tools/Art/slice_ui_sheet.py'), encoding='utf-8').read()
    lib = src[:src.index('def main():')]
    import types
    mod = types.ModuleType('slice_ui_sheet_lib')
    exec(lib, mod.__dict__)
    sys.modules['slice_ui_sheet_lib'] = mod

    for level in (1, 2, 3):
        base = key(os.path.join(SRC, f'L{level}_base.png'))
        bx, by = legs_anchor(base)
        # 받침 바닥이 캔버스 아래 6%에 오고 가로 가운데가 되게 옮긴다
        base_dx, base_dy = WORK / 2 - bx, WORK * 0.94 - by
        frames = {}
        for kind in ('base', 'idle', 'attack'):
            rgba = base if kind == 'base' else key(os.path.join(SRC, f'L{level}_{kind}.png'))
            ax, ay = legs_anchor(rgba)
            frames[kind] = shift(rgba, base_dx + (bx - ax), base_dy + (by - ay))
        save(frames['base'], os.path.join(OUT, f'sewing_L{level}_idle0.png'))
        save(frames['idle'], os.path.join(OUT, f'sewing_L{level}_idle1.png'))
        save(frames['attack'], os.path.join(OUT, f'sewing_L{level}_attack0.png'))
        print(f'{level}단계: 기준점 ({bx:.0f},{by:.0f})')

    # 아이콘(1단계 기본 그림)
    icon = Image.open(os.path.join(OUT, 'sewing_L1_idle0.png'))
    icon = icon.crop(icon.getchannel('A').getbbox())
    s = max(icon.size)
    sq = Image.new('RGBA', (s, s), (0, 0, 0, 0))
    sq.alpha_composite(icon, ((s - icon.width) // 2, (s - icon.height) // 2))
    sq.resize((192, 192), Image.LANCZOS).save(os.path.join(OUT, 'icon.png'))
    gray = sq.resize((192, 192), Image.LANCZOS).convert('LA').convert('RGBA')
    gray.putalpha(sq.resize((192, 192), Image.LANCZOS).getchannel('A').point(lambda v: v * 0.6))
    gray.save(os.path.join(OUT, 'icon_disabled.png'))
    ui = icon.resize((round(icon.width * 104 / icon.height), 104), Image.LANCZOS)
    ui.save(os.path.join(UI_OUT, 'mock_tower_sewing.png'))

    fx_sprites()
    print('완료 ->', OUT)


main()
