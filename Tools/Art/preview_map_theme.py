"""칸 맵 테마(달빛 정원) 미리보기(Unity 없이). MapView의 테마 그리기 규칙을 흉내 낸다.
사용: python3 Tools/Art/preview_map_theme.py <출력.jpg> [맵 JSON] [--px=칸 픽셀]
탐색은 "출현 지점에서 목표까지 가운데 줄을 밝히고, 그 위아래 한 칸씩 더 밝힌" 상태로 가정한다.
결정 칸·벽은 그대로, 몬스터 경로 = 밝힌 칸의 최단 경로, 설치 가능 칸 = 밝힌 칸과 맞닿은 암흑 칸."""
import json
import math
import os
import sys
from collections import deque
import numpy as np
from PIL import Image, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
T = os.path.join(ROOT, 'Assets/Resources/Themes/MoonGarden')
args = [a for a in sys.argv[1:] if not a.startswith('--')]
opts = dict(a[2:].split('=', 1) for a in sys.argv[1:] if a.startswith('--') and '=' in a)
OUT = args[0]
MAP = args[1] if len(args) > 1 else os.path.join(ROOT, 'Assets/Resources/Maps/MAP_EXAMPLE_01.json')
PX = int(opts.get('px', 64))

m = json.load(open(MAP, encoding='utf-8'))
W, H, rows = m['width'], m['height'], m['rows']
sx, sy, gx, gy = m['spawnX'], m['spawnY'], m['goalX'], m['goalY']
cellt = [[rows[y][x] for x in range(W)] for y in range(H)]

opened = set()
for x in range(min(sx, gx), max(sx, gx) + 1):
    for dy in (-1, 0, 1):
        y = sy + dy
        if 0 <= y < H and cellt[y][x] != '#':
            opened.add((x, y))
opened.add((sx, sy)); opened.add((gx, gy))


def bfs():
    prev = {(sx, sy): None}
    q = deque([(sx, sy)])
    while q:
        c = q.popleft()
        if c == (gx, gy):
            break
        for dx, dy in ((0, -1), (1, 0), (0, 1), (-1, 0)):
            n = (c[0] + dx, c[1] + dy)
            if n in opened and n not in prev:
                prev[n] = c
                q.append(n)
    path, c = [], (gx, gy)
    while c is not None:
        path.append(c)
        c = prev.get(c)
    return path[::-1]


path = bfs()
onpath = set(path)


def ld(n):
    return Image.open(os.path.join(T, n + '.png')).convert('RGBA')


ground = ld('ground').resize((PX * 4, PX * 4), Image.LANCZOS)
img = Image.new('RGBA', (W * PX, H * PX), (8, 7, 20, 255))
for y in range(H):
    for x in range(W):
        wy = H - 1 - y
        tile = ground.crop(((x % 4) * PX, (3 - wy % 4) * PX, (x % 4 + 1) * PX, (4 - wy % 4) * PX))
        if (x, y) not in opened:
            tile = Image.fromarray((np.asarray(tile, float) * [0.7, 0.7, 0.78, 1]).astype('uint8'), 'RGBA')
        img.alpha_composite(tile, (x * PX, y * PX))

# 안개
fog = np.zeros((H * 8, W * 8))
for y in range(H):
    for x in range(W):
        if (x, y) not in opened:
            fog[y * 8:(y + 1) * 8, x * 8:(x + 1) * 8] = 1
fimg = Image.fromarray((fog * 255).astype('uint8')).filter(ImageFilter.BoxBlur(4)).filter(ImageFilter.BoxBlur(4)).resize((W * PX, H * PX), Image.BILINEAR)
a = np.asarray(fimg, float) / 255
a = a * a * (3 - 2 * a) * 232 / 255
layer = np.zeros((H * PX, W * PX, 4))
layer[..., 0], layer[..., 1], layer[..., 2], layer[..., 3] = 7 / 255, 6 / 255, 20 / 255, a
img.alpha_composite(Image.fromarray((layer * 255).astype('uint8'), 'RGBA'))

# 빛길
thread = ld('path_thread')
for i in range(1, len(path)):
    (x0, y0), (x1, y1) = path[i - 1], path[i]
    length = PX + PX * 0.12
    seg = thread.resize((int(length), int(PX * 0.55)), Image.LANCZOS)
    seg = Image.fromarray((np.asarray(seg, float) * [0.88, 0.74, 1, 0.85]).astype('uint8'), 'RGBA')
    ang = -math.degrees(math.atan2(y1 - y0, x1 - x0))
    seg = seg.rotate(ang, expand=True)
    cx, cy = (x0 + x1 + 1) / 2 * PX, (y0 + y1 + 1) / 2 * PX
    img.alpha_composite(seg, (int(cx - seg.width / 2), int(cy - seg.height / 2)))
for i in range(0, len(path), 3):
    x, y = path[i]
    r = PX * 0.09
    dot = Image.new('RGBA', img.size)
    from PIL import ImageDraw
    ImageDraw.Draw(dot).ellipse([(x + .5) * PX - r, (y + .5) * PX - r, (x + .5) * PX + r, (y + .5) * PX + r], fill=(255, 230, 255, 220))
    img.alpha_composite(dot)

# 설치 가능 칸
mark = ld('build_mark').resize((int(PX * 0.94), int(PX * 0.94)))
mark = Image.fromarray((np.asarray(mark, float) * [1, 0.84, 0.5, 0.8]).astype('uint8'), 'RGBA')
for y in range(H):
    for x in range(W):
        if (x, y) in opened or cellt[y][x] in 'cC':
            continue
        if any((x + dx, y + dy) in opened for dx, dy in ((0, 1), (1, 0), (0, -1), (-1, 0))):
            img.alpha_composite(mark, (int(x * PX + PX * 0.03), int(y * PX + PX * 0.03)))


def prop(name, x, y, width, tint=None):
    s = ld(name)
    if tint:
        s = Image.fromarray((np.asarray(s, float) * [tint[0], tint[1], tint[2], 1]).astype('uint8'), 'RGBA')
    w = int(PX * width)
    h = int(w * s.height / s.width)
    s = s.resize((w, h), Image.LANCZOS)
    img.alpha_composite(s, (int((x + 0.5) * PX - w / 2), int((y + 1) * PX - PX * 0.08 - h)))


walls = ['wall_bramble', 'wall_knot', 'wall_rock', 'wall_fence']
for y in range(H):
    for x in range(W):
        t = cellt[y][x]
        if t == '#':
            nb = [(x + dx, y + dy) for dx in (-1, 0, 1) for dy in (-1, 0, 1) if (dx or dy) and 0 <= x + dx < W and 0 <= y + dy < H]
            if not any(cellt[ny][nx] != '#' for nx, ny in nb):
                continue  # 안쪽 벽은 그리지 않음
            lit = any(n in opened for n in nb)
            prop(walls[((x * 73856093) ^ (y * 19349663)) % 4], x, y, 1.15, None if lit else (0.5, 0.5, 0.62))
        elif (x, y) in opened:
            pass
        elif t == 'c':
            prop('crystal_small', x, y, 0.6)
        elif t == 'C':
            prop('crystal_large', x, y, 0.85)
        if (x, y) in opened and (x, y) not in onpath and (x * 31 + y * 17) % 9 == 0:
            prop('sprout', x + 0.18, y + 0.1, 0.35)
prop('spawn_rift', sx, sy, 1.1)
prop('core_flower', gx, gy, 0.85)

img.convert('RGB').save(OUT, quality=88)
print('saved', OUT, img.size)
