"""탐색 화면 이안 그림 만들기(Unity 없이): ArtSource/Characters/Ian/ian_pixel_sheet.png → Assets/Resources/Characters/Ian/ian_{방향}_{0~3}.png
사용: python3 Tools/Art/build_ian_sprites.py
- 시트(2026-10-29, 타워·몬스터와 같은 픽셀 그림체)는 초록 배경 4칸 × 4줄. 줄: 아래(앞)·왼쪽·오른쪽·위(뒤), 칸: 걷기 4장(0 = 서 있기에도 씀).
  (예전 칠 그림 시트 ian_sheet.png는 그림체가 달라 쓰지 않는다.)
- 초록 배경·발밑 그림자는 투명으로(slice_ui_sheet의 key_green), 랜턴 빛 번짐의 연두빛은 주황으로. 게임에서 그림자를 따로 그린다.
- 모든 그림을 같은 크기 캔버스에 발바닥 높이·머리 가로 중심을 맞춰 놓는다(걸을 때 덜컹거리지 않게)."""
import os
import sys
import types
import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SRC = os.path.join(ROOT, 'ArtSource/Characters/Ian/ian_pixel_sheet.png')
OUT = os.path.join(ROOT, 'Assets/Resources/Characters/Ian')
COLS, ROWS = 4, 4
ROW_OF = {'down': 0, 'left': 1, 'right': 2, 'up': 3}
FRAMES = 4
CANVAS = (300, 420)   # 저장 크기(가로, 세로), 발바닥은 아래 4% 위

src = open(os.path.join(ROOT, 'Tools/Art/slice_ui_sheet.py'), encoding='utf-8').read()
lib = types.ModuleType('slice_lib')
exec(src[:src.index('def main():')], lib.__dict__)


def cut(sheet, col, row):
    h, w = sheet.shape[:2]
    cw, ch = w / COLS, h / ROWS
    cell = sheet[int(row * ch):int((row + 1) * ch), int(col * cw):int((col + 1) * cw)]
    rgb, alpha = lib.key_green(cell)
    # 랜턴 빛 번짐(반투명)은 초록 위에 번져 연두빛이 남는다 → 따뜻한 주황으로
    soft = alpha < 0.9
    warm = np.maximum(rgb[..., 0], rgb[..., 2])
    rgb[..., 1] = np.where(soft, np.minimum(rgb[..., 1], warm * 0.78), rgb[..., 1])
    rgb[..., 2] = np.where(soft, np.minimum(rgb[..., 2], warm * 0.45), rgb[..., 2])
    # 가장 큰 덩어리(몸)와 붙은 것만 남긴다
    labels, n = ndimage.label(ndimage.binary_dilation(alpha > 0.5, iterations=6))
    sizes = ndimage.sum(alpha > 0.5, labels, range(1, n + 1))
    alpha = alpha * (labels == 1 + int(np.argmax(sizes)))
    return np.dstack([rgb, alpha])


def anchor(rgba):
    solid = rgba[..., 3] > 0.6
    ys, xs = np.nonzero(solid)
    top, bottom = ys.min(), ys.max()
    head = ys < top + (bottom - top) * 0.35
    return xs[head].mean(), bottom, bottom - top


def main():
    sheet = np.asarray(Image.open(SRC).convert('RGB'), float) / 255.0
    os.makedirs(OUT, exist_ok=True)
    frames = {(d, f): cut(sheet, f, r) for d, r in ROW_OF.items() for f in range(FRAMES)}
    tallest = max(anchor(fr)[2] for fr in frames.values())
    scale = CANVAS[1] * 0.9 / tallest
    for (d, f), fr in frames.items():
        cx, bottom, _ = anchor(fr)
        im = Image.fromarray((np.clip(fr, 0, 1) * 255).astype('uint8'), 'RGBA')
        im = im.resize((round(im.width * scale), round(im.height * scale)), Image.LANCZOS)
        canvas = Image.new('RGBA', CANVAS, (0, 0, 0, 0))
        canvas.alpha_composite(im, (round(CANVAS[0] / 2 - cx * scale), round(CANVAS[1] * 0.96 - bottom * scale)))
        canvas.save(os.path.join(OUT, f'ian_{d}_{f}.png'))
    print('완료 ->', OUT)


main()
