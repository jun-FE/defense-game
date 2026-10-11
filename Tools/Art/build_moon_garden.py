"""달빛 정원 맵 테마 만들기(Unity 없이): ArtSource/Maps/MoonGarden → Assets/Resources/Themes/MoonGarden
사용: python3 Tools/Art/build_moon_garden.py
- ground.png: 바닥 그림(ground_a.png)을 이음새 없이 반복되게 다듬어 512px(4×4칸, 칸당 128px)로.
  이음새 처리: 반 칸씩 밀린 복사본을 가운데가 투명한 가중치로 섞어, 원본 가장자리 경계가 보이지 않게 한다.
- 오브젝트(props.png, 3×3 초록 배경 시트) → 벽 4종·작은/큰 결정·출현 지점·꿈의 중심·새싹, 최대 256px.
- path_thread.png, build_mark.png는 이 폴더에 이미 있는 코드 생성 그림(빛나는 꿈실 띠, 바느질 점선 네모).
2단계(사용자 샘플 reference/sample_moon_garden.png에 맞춘 밝은 정원):
- ground_stone.png(밝힌 칸: 레이스 무늬 돌길), ground_grass.png(안 연 칸: 달빛 잔디) — 512px = 4×4칸, ground_flower.png(막힌 암흑: 꽃밭) — 1024px = 8×8칸.
- flower_tuft_0~3.png: 꽃밭 가장자리를 덮는 들쭉날쭉한 꽃덤불 조각(벽 칸 경계를 부드럽게).
- border_0~3.png: 돌길과 잔디 사이 경계(꽃·황동 기둥·실패·금사슬), 한 조각 = 칸 한 변.
- garden_*.png(props2.png 3×3 시트): 가로등, 수국 덤불, 실패 더미, 꽃 화분, 백합, 작은 랜턴, 돌기둥, 황동 단추, 벨벳 상자."""
import os
import subprocess
import sys
import tempfile
import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SRC = os.path.join(ROOT, 'ArtSource/Maps/MoonGarden')
OUT = os.path.join(ROOT, 'Assets/Resources/Themes/MoonGarden')
GARDEN = ['garden_lantern_post', 'garden_hydrangea', 'garden_spools', 'garden_urn', 'garden_lilies', 'garden_lantern', 'garden_pillar', 'garden_button', 'garden_chest']
PROPS = ['wall_bramble', 'wall_knot', 'wall_rock', 'wall_fence', 'crystal_small', 'crystal_large', 'spawn_rift', 'core_flower', 'sprout']
UNUSED = {'wall_bramble', 'wall_knot', 'wall_rock', 'wall_fence', 'sprout'}   # 1단계 그림(2단계 정원에서는 안 씀, 저장 안 함)


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


def lib():
    import types
    src = open(os.path.join(ROOT, 'Tools/Art/slice_ui_sheet.py'), encoding='utf-8').read()
    mod = types.ModuleType('slice_lib')
    exec(src[:src.index('def main():')], mod.__dict__)
    return mod


def tuft(img, seed):
    """꽃밭에서 잘라 낸 둥글고 들쭉날쭉한 덤불 조각(가장자리는 부드럽게 투명)."""
    rng = np.random.default_rng(seed)
    n = 512
    a = np.asarray(img, float)
    # 큰 흰 꽃이 덜 들어간 자리를 고른다(가장자리마다 같은 흰 꽃이 줄지어 보이지 않게)
    best = None
    for _ in range(40):
        x0, y0 = rng.integers(0, a.shape[1] - n), rng.integers(0, a.shape[0] - n)
        c = a[y0:y0 + n, x0:x0 + n]
        bright = (c.min(axis=2) > 200).mean()
        if best is None or bright < best[0]:
            best = (bright, c)
    crop = best[1]
    y, x = np.mgrid[0:n, 0:n] / (n - 1) * 2 - 1
    d = np.sqrt(x * x + y * y)
    ang = np.arctan2(y, x)
    edge = 0.78
    for k, amp in ((3, 0.05), (7, 0.05), (13, 0.035), (23, 0.02)):
        edge = edge + amp * np.sin(ang * k + rng.uniform(0, 6.28))
    alpha = np.clip((edge - d) / 0.12, 0, 1)
    # 잎 사이 어두운 곳은 먼저 투명해지게(덤불 윤곽이 잎 모양을 따라감)
    lum = crop.mean(axis=2) / 255
    alpha = np.clip(alpha * (0.6 + lum * 1.6) - (1 - alpha) * 0.2, 0, 1)
    out = np.dstack([crop, alpha * 255]).astype('uint8')
    return Image.fromarray(out, 'RGBA').resize((256, 256), Image.LANCZOS)


def garden_v2(slicer):
    # 꽃밭은 반복이 눈에 띄지 않게 8×8칸(1024px), 돌길·잔디는 4×4칸(512px). 칸당 128px.
    for src, name, tile, size in (('stone.png', 'ground_stone', False, 512), ('grass.png', 'ground_grass', True, 512), ('flowerbed.png', 'ground_flower', True, 1024)):
        im = Image.open(os.path.join(SRC, src)).convert('RGB')
        if tile:
            im = make_tileable(im)
        im.resize((size, size), Image.LANCZOS).save(os.path.join(OUT, name + '.png'))
        print(name, f'{size}x{size}')
    flower = make_tileable(Image.open(os.path.join(SRC, 'flowerbed.png')).convert('RGB'))
    for i in range(4):
        tuft(flower, 11 + i).save(os.path.join(OUT, f'flower_tuft_{i}.png'))
    print('flower_tuft_0~3')

    # 경계 띠: 초록 배경 제거 → 띠 높이만큼 잘라 → 칸 한 변 길이(띠 두께의 약 3배)로 4조각
    b = np.asarray(Image.open(os.path.join(SRC, 'border.png')).convert('RGB'), float) / 255.0
    rgb, alpha = slicer.key_green(b)
    rows = np.nonzero((alpha > 0.5).sum(axis=1) > b.shape[1] * 0.05)[0]
    y0, y1 = max(0, rows.min() - 4), min(b.shape[0], rows.max() + 5)
    strip = np.dstack([rgb, alpha])[y0:y1]
    h = strip.shape[0]
    seg = int(h * 2.9)
    cols = np.nonzero((alpha[y0:y1] > 0.5).sum(axis=0) > 0)[0]
    x_start, x_end = cols.min(), cols.max()
    count = 0
    for i in range(4):
        xs = x_start + i * seg
        if xs + seg > x_end:
            break
        piece = Image.fromarray((np.clip(strip[:, xs:xs + seg], 0, 1) * 255).astype('uint8'), 'RGBA')
        piece.resize((384, round(384 * h / seg)), Image.LANCZOS).save(os.path.join(OUT, f'border_{i}.png'))
        count += 1
    print(f'border_0~{count - 1} (띠 두께 {h}px, 조각 길이 {seg}px)')

    with tempfile.TemporaryDirectory() as tmp:
        subprocess.run([sys.executable, os.path.join(ROOT, 'Tools/Art/slice_ui_sheet.py'), os.path.join(SRC, 'props2.png'), tmp, '--gap=10', '--min=30000'],
                       check=True, stdout=subprocess.DEVNULL)
        for i, name in enumerate(GARDEN):
            im = Image.open(os.path.join(tmp, f'piece_{i:02d}.png'))
            im.thumbnail((256, 256), Image.LANCZOS)
            im.save(os.path.join(OUT, name + '.png'))
            print(f'{name:20s} {im.width}x{im.height}')


def main():
    os.makedirs(OUT, exist_ok=True)

    with tempfile.TemporaryDirectory() as tmp:
        subprocess.run([sys.executable, os.path.join(ROOT, 'Tools/Art/slice_ui_sheet.py'), os.path.join(SRC, 'props.png'), tmp, '--gap=10'],
                       check=True, stdout=subprocess.DEVNULL)
        for i, name in enumerate(PROPS):
            if name in UNUSED:
                continue
            im = Image.open(os.path.join(tmp, f'piece_{i:02d}.png'))
            im.thumbnail((256, 256), Image.LANCZOS)
            im.save(os.path.join(OUT, name + '.png'))
            print(f'{name:14s} {im.width}x{im.height}')

    garden_v2(lib())


main()
