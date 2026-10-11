"""달빛 정원 맵 테마 만들기(Unity 없이): ArtSource/Maps/MoonGarden → Assets/Resources/Themes/MoonGarden
사용: python3 Tools/Art/build_moon_garden.py
- ground.png: 바닥 그림(ground_a.png)을 이음새 없이 반복되게 다듬어 512px(4×4칸, 칸당 128px)로.
  이음새 처리: 반 칸씩 밀린 복사본을 가운데가 투명한 가중치로 섞어, 원본 가장자리 경계가 보이지 않게 한다.
- 오브젝트(props.png, 3×3 초록 배경 시트) → 벽 4종·작은/큰 결정·출현 지점·꿈의 중심·새싹, 최대 256px.
- path_thread.png, build_mark.png는 이 폴더에 이미 있는 코드 생성 그림(빛나는 꿈실 띠, 바느질 점선 네모)."""
import os
import subprocess
import sys
import tempfile
import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SRC = os.path.join(ROOT, 'ArtSource/Maps/MoonGarden')
OUT = os.path.join(ROOT, 'Assets/Resources/Themes/MoonGarden')
PROPS = ['wall_bramble', 'wall_knot', 'wall_rock', 'wall_fence', 'crystal_small', 'crystal_large', 'spawn_rift', 'core_flower', 'sprout']


def make_tileable(img):
    a = np.asarray(img.convert('RGB'), float)
    h, w = a.shape[:2]
    rolled = np.roll(np.roll(a, h // 2, axis=0), w // 2, axis=1)
    y, x = np.mgrid[0:h, 0:w]
    # 가장자리로 갈수록 0, 가운데 1 (원본 가장자리 = rolled의 가운데 → rolled로 덮음)
    wy = np.clip(np.minimum(y, h - 1 - y) / (h * 0.25), 0, 1)
    wx = np.clip(np.minimum(x, w - 1 - x) / (w * 0.25), 0, 1)
    wgt = (wy * wx)[..., None]
    wgt = wgt * wgt * (3 - 2 * wgt)  # 부드러운 곡선
    out = a * wgt + rolled * (1 - wgt)
    return Image.fromarray(np.clip(out, 0, 255).astype('uint8'), 'RGB')


def main():
    os.makedirs(OUT, exist_ok=True)
    ground = make_tileable(Image.open(os.path.join(SRC, 'ground_a.png')))
    ground.resize((512, 512), Image.LANCZOS).save(os.path.join(OUT, 'ground.png'))
    print('ground.png 512x512 (4x4칸)')

    with tempfile.TemporaryDirectory() as tmp:
        subprocess.run([sys.executable, os.path.join(ROOT, 'Tools/Art/slice_ui_sheet.py'), os.path.join(SRC, 'props.png'), tmp, '--gap=10'],
                       check=True, stdout=subprocess.DEVNULL)
        for i, name in enumerate(PROPS):
            im = Image.open(os.path.join(tmp, f'piece_{i:02d}.png'))
            im.thumbnail((256, 256), Image.LANCZOS)
            im.save(os.path.join(OUT, name + '.png'))
            print(f'{name:14s} {im.width}x{im.height}')


main()
