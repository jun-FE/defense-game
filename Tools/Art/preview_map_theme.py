"""칸 맵 테마(달빛 정원) 미리보기(Unity 없이). MapView·ExploreUI의 테마 그리기 규칙을 흉내 낸다.
사용: python3 Tools/Art/preview_map_theme.py <출력.jpg> [맵 JSON] [--px=칸 픽셀] [--mode=battle|explore] [--margin=바깥 칸 수]
탐색은 "출현 지점에서 목표까지 가운데 줄을 밝히고, 그 위아래 한 칸씩 더 밝힌" 상태로 가정한다.
- 밝힌 칸 = 돌길, 안 연 칸 = 잔디, 막힌 암흑 = 꽃밭(가장자리는 꽃덤불 조각 + 가끔 정원 소품).
- 돌길과 잔디 사이에는 꽃 경계 띠. 어둠 안개는 전투에서 옅게(밤 느낌), 탐색에서 짙게.
- 맵 바깥은 어두운 꽃밭이 이어진다."""
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
MODE = opts.get('mode', 'battle')
M = int(opts.get('margin', 3))
FOG = {'battle': 0.34, 'explore': 0.94}[MODE]
NIGHT = (9, 10, 23)

m = json.load(open(MAP, encoding='utf-8'))
W, H, rows = m['width'], m['height'], m['rows']
sx, sy, gx, gy = m['spawnX'], m['spawnY'], m['goalX'], m['goalY']
cellt = [[rows[y][x] for x in range(W)] for y in range(H)]
wall = lambda x, y: not (0 <= x < W and 0 <= y < H) or cellt[y][x] == '#'

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
def tint(im, t):
    return Image.fromarray(np.clip(np.asarray(im, float) * [t[0], t[1], t[2], t[3] if len(t) > 3 else 1], 0, 255).astype('uint8'), 'RGBA')


ld = lambda n: Image.open(os.path.join(T, n + '.png')).convert('RGBA')
h32 = lambda x, y: ((x * 73856093) ^ (y * 19349663)) & 0x7fffffff


TILES = {'stone': 4, 'grass': 4, 'flower': 8}
SHADE = {'stone': (0.95, 0.95, 1.0), 'grass': (1.0, 1.05, 1.0), 'flower': (0.78, 0.78, 0.86)}
grounds = {k: tint(ld('ground_' + k).resize((PX * n, PX * n), Image.LANCZOS), SHADE[k]) for k, n in TILES.items()}
img = Image.new('RGBA', ((W + 2 * M) * PX, (H + 2 * M) * PX), NIGHT + (255,))
O = M * PX


def piece(kind, x, y):
    wy = H - 1 - y
    g, n = grounds[kind], TILES[kind]
    return g.crop(((x % n) * PX, (n - 1 - wy % n) * PX, (x % n + 1) * PX, (n - wy % n) * PX))


# 바깥 꽃밭(어둡게)
for y in range(-M, H + M):
    for x in range(-M, W + M):
        if 0 <= x < W and 0 <= y < H:
            continue
        if MODE == 'battle':
            img.alpha_composite(tint(piece('flower', x, y), (0.3, 0.3, 0.4)), (O + x * PX, O + y * PX))
        else:  # 탐색: 꽃밭 위에 맵 안과 같은 안개
            t = np.asarray(piece('flower', x, y), float)
            t[..., :3] = t[..., :3] * (1 - FOG) + np.array([10, 12, 34]) * FOG
            img.alpha_composite(Image.fromarray(t.astype('uint8'), 'RGBA'), (O + x * PX, O + y * PX))
for y in range(H):
    for x in range(W):
        kind = 'flower' if cellt[y][x] == '#' else 'stone' if (x, y) in opened else 'grass'
        img.alpha_composite(piece(kind, x, y), (O + x * PX, O + y * PX))


def put(im, cx, cy, angle=0):
    if angle:
        im = im.rotate(angle, expand=True, resample=Image.BICUBIC)
    img.alpha_composite(im, (int(O + cx - im.width / 2), int(O + cy - im.height / 2)))


# 꽃밭 가장자리 덤불
tufts = [ld(f'flower_tuft_{i}') for i in range(4)]
for y in range(H):
    for x in range(W):
        if cellt[y][x] != '#':
            continue
        if not any(not wall(x + dx, y + dy) for dx in (-1, 0, 1) for dy in (-1, 0, 1)):
            continue
        k = h32(x, y)
        size = PX * (1.3 + (k >> 4) % 5 * 0.05)
        t = tint(tufts[k % 4].resize((int(size), int(size)), Image.LANCZOS), SHADE['flower'])
        if (k >> 8) % 2:
            t = t.transpose(Image.FLIP_LEFT_RIGHT)
        put(t, (x + .5 + ((k >> 10) % 5 - 2) * 0.05) * PX, (y + .5 + ((k >> 13) % 5 - 2) * 0.05) * PX, (k >> 16) % 4 * 90)

# 경계 띠(돌길 ↔ 잔디)
borders = [ld(f'border_{i}') for i in range(4)]
for (x, y) in opened:
    for dx, dy in ((0, -1), (1, 0), (0, 1), (-1, 0)):
        n = (x + dx, y + dy)
        if n in opened or wall(*n):
            continue
        b = borders[h32(x * 2 + dx, y * 2 + dy) % 4]
        b = b.resize((int(PX * 1.06), int(PX * 1.06 * b.height / b.width)), Image.LANCZOS)
        cx, cy = (x + .5 + dx * .5) * PX, (y + .5 + dy * .5) * PX
        put(b, cx, cy, 0 if dy else 90)

# 어둠 안개
fog = np.zeros((H * 8, W * 8))
for y in range(H):
    for x in range(W):
        if (x, y) not in opened:
            fog[y * 8:(y + 1) * 8, x * 8:(x + 1) * 8] = 1
fimg = Image.fromarray((fog * 255).astype('uint8')).filter(ImageFilter.BoxBlur(4)).filter(ImageFilter.BoxBlur(4)).resize((W * PX, H * PX), Image.BILINEAR)
a = np.asarray(fimg, float) / 255
a = a * a * (3 - 2 * a) * FOG
layer = np.zeros((H * PX, W * PX, 4))
layer[..., 0], layer[..., 1], layer[..., 2], layer[..., 3] = 10 / 255, 12 / 255, 34 / 255, a
img.alpha_composite(Image.fromarray((layer * 255).astype('uint8'), 'RGBA'), (O, O))

# 맵 가장자리 그라데이션(1.5칸, 바깥 어둠으로 녹임)
fw = int(PX * 1.5)
ramp = np.linspace(1, 0, fw)
ramp = ramp * ramp * (3 - 2 * ramp) * (0.5 if MODE == 'battle' else 0.35)
edge = np.zeros((H * PX, W * PX))
yy = np.arange(H * PX)[:, None]
xx = np.arange(W * PX)[None, :]
for d in (yy, H * PX - 1 - yy, xx, W * PX - 1 - xx):
    edge = np.maximum(edge, np.where(d < fw, ramp[np.clip(d, 0, fw - 1)], 0))
el = np.zeros((H * PX, W * PX, 4))
el[..., 0], el[..., 1], el[..., 2], el[..., 3] = NIGHT[0] / 255, NIGHT[1] / 255, NIGHT[2] / 255, edge
img.alpha_composite(Image.fromarray((el * 255).astype('uint8'), 'RGBA'), (O, O))

# 빛길(전투만, 은은하게)
if MODE == 'battle':
    thread = ld('path_thread')
    for i in range(1, len(path)):
        (x0, y0), (x1, y1) = path[i - 1], path[i]
        seg = tint(thread.resize((int(PX * 1.12), int(PX * 0.4)), Image.LANCZOS), (0.95, 0.85, 1, 0.5))
        put(seg, (x0 + x1 + 1) / 2 * PX, (y0 + y1 + 1) / 2 * PX, -math.degrees(math.atan2(y1 - y0, x1 - x0)))


def prop(name, x, y, width, t=None):
    s = ld(name)
    if t:
        s = tint(s, t)
    w = int(PX * width)
    h = int(w * s.height / s.width)
    s = s.resize((w, h), Image.LANCZOS)
    img.alpha_composite(s, (int(O + (x + 0.5) * PX - w / 2), int(O + (y + 1) * PX - PX * 0.08 - h)))


garden = ['garden_lantern_post', 'garden_hydrangea', 'garden_spools', 'garden_urn', 'garden_lilies', 'garden_lantern', 'garden_pillar', 'garden_button', 'garden_chest']
widths = [0.55, 0.95, 0.85, 0.75, 0.85, 0.55, 0.6, 0.8, 0.85]
for y in range(H):
    for x in range(W):
        t = cellt[y][x]
        if t == '#':
            if not any(not wall(x + dx, y + dy) for dx, dy in ((0, 1), (1, 0), (0, -1), (-1, 0))) or h32(x, y) % 3:
                continue
            lit = any((x + dx, y + dy) in opened for dx in (-1, 0, 1) for dy in (-1, 0, 1))
            k = h32(y + 7, x + 3) % len(garden)
            prop(garden[k], x, y, widths[k], None if lit or MODE == 'battle' else (0.55, 0.55, 0.7, 1))
        elif (x, y) in opened:
            pass
        elif t == 'c':
            prop('crystal_small', x, y, 0.6)
        elif t == 'C':
            prop('crystal_large', x, y, 0.85)
prop('spawn_rift', sx, sy, 1.1)
prop('core_flower', gx, gy, 0.85)

# 화면 테두리 레이어: 비네트 + 위·아래 그라데이션
Wt, Ht = img.size
yy, xx = np.mgrid[0:Ht, 0:Wt]
dx, dy = (xx + .5) / Wt * 2 - 1, (yy + .5) / Ht * 2 - 1
d = np.sqrt(dx * dx * 0.8 + dy * dy * 1.1)
a = np.clip((d - 0.62) / 0.6, 0, 1) ** 2 * 235 / 255
for band, frac in ((yy / Ht, 0.14), ((Ht - 1 - yy) / Ht, 0.21)):
    r = np.clip(1 - band / frac, 0, 1)
    a = np.maximum(a, r * r * (3 - 2 * r) * 0.92)
fl = np.zeros((Ht, Wt, 4))
fl[..., 0], fl[..., 1], fl[..., 2], fl[..., 3] = NIGHT[0] / 255, NIGHT[1] / 255, NIGHT[2] / 255, a
img.alpha_composite(Image.fromarray((fl * 255).astype('uint8'), 'RGBA'))

img.convert('RGB').save(OUT, quality=88)
print('saved', OUT, img.size)
