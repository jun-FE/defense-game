"""4방향·단계별 PNG 프레임 타워 원본을 게임 폴더로 옮긴다(Unity 없이).
사용: python3 Tools/Art/import_tower_frames.py <원본 폴더> <영문 이름> [--ui-icon=이름]
원본 구조: level_1/up/frame_01.png … (level_N / up·down·left·right / frame_XX.png, 같은 캔버스 크기, 투명 배경)
결과(Assets/Art/Towers/<Name>/):
  <name>_<dir>_L<n>_<f>.png  프레임(f는 0부터, 캔버스 그대로)
  icon.png, icon_disabled.png  1단계 아래 방향 첫 프레임을 잘라 만든 192px 아이콘(흑백판 포함)
  projectile.png  투사체(얼음 조각, 오른쪽이 앞) — 없을 때만 만든다
--ui-icon을 주면 건설 고리 아이콘(Assets/Art/Battle/UI/<이름>.png, 높이 104px)도 만든다."""
import os, sys, glob
import numpy as np
from PIL import Image, ImageFilter

DIRS = ['up', 'down', 'left', 'right']


def crop_square(im, size):
    box = im.getchannel('A').getbbox()
    im = im.crop(box)
    s = max(im.size)
    canvas = Image.new('RGBA', (s, s), (0, 0, 0, 0))
    canvas.alpha_composite(im, ((s - im.width) // 2, (s - im.height) // 2))
    return canvas.resize((size, size), Image.LANCZOS)


def ice_shard(path):
    w, h = 96, 40
    y, x = np.mgrid[0:h, 0:w].astype(float)
    # 오른쪽 끝이 뾰족한 마름모 + 뒤로 흐려지는 꼬리
    cx, cy = w * 0.68, h / 2
    core = np.clip(1 - (np.abs(x - cx) / (w * 0.30) + np.abs(y - cy) / (h * 0.30)), 0, 1)
    tail = np.clip(1 - np.abs(y - cy) / (h * 0.16), 0, 1) * np.clip(x / (w * 0.68), 0, 1) ** 2 * (x < cx)
    a = np.clip(core * 1.6 + tail * 0.7, 0, 1)
    white = np.clip(core * 2.2 - 0.6, 0, 1)
    rgb = np.dstack([0.55 + 0.45 * white, 0.85 + 0.15 * white, np.ones_like(white)])
    img = Image.fromarray((np.dstack([rgb, a]) * 255).astype('uint8'), 'RGBA')
    glow = img.filter(ImageFilter.GaussianBlur(3))
    glow.alpha_composite(img)
    glow.save(path)


def main():
    src, name = sys.argv[1], sys.argv[2]
    ui_icon = next((a.split('=', 1)[1] for a in sys.argv if a.startswith('--ui-icon=')), None)
    lower = name.lower()
    out = f'Assets/Art/Towers/{name}'
    os.makedirs(out, exist_ok=True)
    levels = [p for p in glob.glob(os.path.join(src, 'level_*')) if os.path.isdir(p)]
    levels.sort(key=lambda p: int(p.rsplit('_', 1)[1]))
    count = 0
    for li, level in enumerate(levels, 1):
        for d in DIRS:
            frames = sorted(glob.glob(os.path.join(level, d, '*.png')))
            for fi, f in enumerate(frames):
                Image.open(f).convert('RGBA').save(f'{out}/{lower}_{d}_L{li}_{fi}.png')
                count += 1
    first = Image.open(sorted(glob.glob(os.path.join(levels[0], 'down', '*.png')))[0]).convert('RGBA')
    icon = crop_square(first, 192)
    icon.save(f'{out}/icon.png')
    gray = icon.convert('LA').convert('RGBA')
    gray.putalpha(icon.getchannel('A').point(lambda v: v * 0.6))
    gray.save(f'{out}/icon_disabled.png')
    if not os.path.exists(f'{out}/projectile.png'):
        ice_shard(f'{out}/projectile.png')
    if ui_icon:
        im = first.crop(first.getchannel('A').getbbox())
        im = im.resize((round(im.width * 104 / im.height), 104), Image.LANCZOS)
        im.save(f'Assets/Art/Battle/UI/{ui_icon}.png')
    print(f'{count} frames, {len(levels)} levels -> {out}')


main()
