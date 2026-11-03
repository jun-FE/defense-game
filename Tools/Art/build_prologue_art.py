"""프롤로그 그림 만들기(Unity 없이): ArtSource/Prologue/generated/*.png → Assets/Resources/...
사용: python3 Tools/Art/build_prologue_art.py
- 대사 배경(1920 너비): door → Stories/Backgrounds/prologue_door, wake_pov → prologue_wake_pov, wake_sit → prologue_wake_sit
- 꿈속 방 배경: room → Themes/DreamRoom/backdrop (탐색·전투 화면 맨 뒤에 깔고 그 위에 칸을 그린다)
- 초상화(초록 배경 → 투명, 여백 잘라 세로 1100): portrait_ian → Stories/Portraits/이안, portrait_doha → 도하,
  portrait_ian_child → 이안_어린
- 칸·물건 시트 tiles(위 3개·아래 3개, 초록 배경): 밝은 바닥, 어두운 바닥, 작은 몽결정 / 큰 몽결정, 보라 소용돌이(악몽 입구), 수면등(심장)
  → ArtSource/Prologue/sliced/floor_lit, floor_dark, crystal_small, crystal_big, portal, core (테마 재료)
- 꿈속 방 테마 Themes/DreamRoom(탐색·전투 공용, MapTheme.Load("DreamRoom")):
  ground_stone(밝힌 칸) = 밝은 바닥 4×4, ground_grass(안 연 칸) = 어두운 바닥 4×4, ground_flower(막힌 칸·바깥) = 아주 어두운 바닥 8×8,
  crystal_small·crystal_large·spawn_rift(보라 소용돌이)·core_flower(수면등), backdrop(방 그림).
  덤불·경계 띠·정원 소품은 두지 않는다(방 안이라). 빛·불티·고리·실·설치 표시 그림은 MoonGarden 것을 복사.
꿈속 어린 이안 걷기 그림은 build_ian_sprites.py child 로 따로 만든다."""
import shutil
import os
import types
import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SRC = os.path.join(ROOT, 'ArtSource/Prologue/generated')
RES = os.path.join(ROOT, 'Assets/Resources')

src = open(os.path.join(ROOT, 'Tools/Art/slice_ui_sheet.py'), encoding='utf-8').read()
lib = types.ModuleType('slice_lib')
exec(src[:src.index('def main():')], lib.__dict__)

BACKGROUNDS = {'door': 'Stories/Backgrounds/prologue_door', 'wake_pov': 'Stories/Backgrounds/prologue_wake_pov',
               'wake_sit': 'Stories/Backgrounds/prologue_wake_sit', 'room': 'Themes/DreamRoom/backdrop'}
PORTRAITS = {'portrait_ian': 'Stories/Portraits/이안', 'portrait_doha': 'Stories/Portraits/도하',
             'portrait_ian_child': 'Stories/Portraits/이안_어린'}
TILES = [['floor_lit', 'floor_dark', 'crystal_small'], ['crystal_big', 'portal', 'core']]


def out(rel):
    path = os.path.join(RES, rel + '.png')
    os.makedirs(os.path.dirname(path), exist_ok=True)
    return path


SLICED = os.path.join(ROOT, 'ArtSource/Prologue/sliced')


def sliced(name):
    os.makedirs(SLICED, exist_ok=True)
    return os.path.join(SLICED, name + '.png')


def keyed(arr):
    rgb, alpha = lib.key_green(arr)
    return np.dstack([rgb, alpha])


def crop(rgba, pad=8, keep_largest=False):
    alpha = rgba[..., 3]
    if keep_largest:
        labels, n = ndimage.label(ndimage.binary_dilation(alpha > 0.5, iterations=4))
        if n > 1:
            sizes = ndimage.sum(alpha > 0.5, labels, range(1, n + 1))
            rgba = rgba.copy()
            rgba[..., 3] = alpha * (labels == 1 + int(np.argmax(sizes)))
            alpha = rgba[..., 3]
    ys, xs = np.nonzero(alpha > 0.02)
    y0, y1 = max(0, ys.min() - pad), min(rgba.shape[0], ys.max() + pad + 1)
    x0, x1 = max(0, xs.min() - pad), min(rgba.shape[1], xs.max() + pad + 1)
    return rgba[y0:y1, x0:x1]


def save(rgba, path, height=None, width=None):
    im = Image.fromarray((np.clip(rgba, 0, 1) * 255).astype('uint8'), 'RGBA')
    if height:
        im = im.resize((round(im.width * height / im.height), height), Image.LANCZOS)
    if width:
        im = im.resize((width, round(im.height * width / im.width)), Image.LANCZOS)
    im.save(path)


def main():
    for name, rel in BACKGROUNDS.items():
        im = Image.open(os.path.join(SRC, name + '.png')).convert('RGB')
        im.resize((1920, round(im.height * 1920 / im.width)), Image.LANCZOS).save(out(rel))
    for name, rel in PORTRAITS.items():
        arr = np.asarray(Image.open(os.path.join(SRC, name + '.png')).convert('RGB'), float) / 255.0
        save(crop(keyed(arr), keep_largest=True), out(rel), height=1100)
    # 시트의 물건 배치가 고르지 않아 칸으로 자르지 않고 덩어리(6개)를 찾아 위 줄·아래 줄, 왼쪽부터 순서로 이름을 붙인다.
    sheet = keyed(np.asarray(Image.open(os.path.join(SRC, 'tiles.png')).convert('RGB'), float) / 255.0)
    h = sheet.shape[0]
    labels, n = ndimage.label(ndimage.binary_dilation(sheet[..., 3] > 0.5, iterations=10))
    # 반짝이 같은 작은 조각은 버리고 가장 큰 6개만
    area = lambda b: (b[0].stop - b[0].start) * (b[1].stop - b[1].start)
    boxes = sorted(ndimage.find_objects(labels), key=area, reverse=True)[:6]
    rows = [sorted([b for b in boxes if ((b[0].start + b[0].stop) / 2 < h / 2) == top], key=lambda b: b[1].start) for top in (True, False)]
    for r, row in enumerate(rows):
        for c, b in enumerate(row):
            name = TILES[r][c]
            rgba = crop(sheet[b], pad=2 if name.startswith('floor') else 6, keep_largest=True)
            save(rgba, sliced(name), width=256 if name.startswith('floor') else None,
                 height=None if name.startswith('floor') else 256)
    build_room_theme()
    print('완료')


def tiled(tile, n, size, shade=1.0):
    """칸 그림을 n×n으로 깐 텍스처. 칸마다 뒤집기·돌리기로 반복이 덜 보이게."""
    cell = size // n
    base = tile.convert('RGB').resize((cell, cell), Image.LANCZOS)
    tex = Image.new('RGB', (cell * n, cell * n))
    rng = np.random.default_rng(7)
    for y in range(n):
        for x in range(n):
            t = base.transpose(Image.Transpose.ROTATE_180) if rng.random() < 0.5 else base
            if rng.random() < 0.5:
                t = t.transpose(Image.Transpose.FLIP_LEFT_RIGHT)
            tex.paste(t, (x * cell, y * cell))
    if shade != 1.0:
        tex = Image.fromarray((np.asarray(tex, float) * shade).astype('uint8'))
    return tex


def build_room_theme():
    dst = os.path.join(RES, 'Themes/DreamRoom')
    os.makedirs(dst, exist_ok=True)
    pro = SLICED
    tiled(Image.open(os.path.join(pro, 'floor_lit.png')), 4, 512).save(os.path.join(dst, 'ground_stone.png'))
    tiled(Image.open(os.path.join(pro, 'floor_dark.png')), 4, 512).save(os.path.join(dst, 'ground_grass.png'))
    tiled(Image.open(os.path.join(pro, 'floor_dark.png')), 8, 1024, 0.28).save(os.path.join(dst, 'ground_flower.png'))
    for src, name in [('crystal_small', 'crystal_small'), ('crystal_big', 'crystal_large'), ('portal', 'spawn_rift'),
                      ('core', 'core_flower')]:
        shutil.copyfile(os.path.join(pro, src + '.png'), os.path.join(dst, name + '.png'))
    garden = os.path.join(RES, 'Themes/MoonGarden')
    for name in ['fx_glow', 'fx_spark', 'fx_ring_0', 'fx_ring_1', 'fx_ring_2', 'path_thread', 'build_mark']:
        shutil.copyfile(os.path.join(garden, name + '.png'), os.path.join(dst, name + '.png'))


if __name__ == '__main__':
    main()
