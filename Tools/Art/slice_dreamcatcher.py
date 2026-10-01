from PIL import Image
import numpy as np
from scipy import ndimage as ndi
import os, json, sys

# 사용: python3 Tools/Art/slice_dreamcatcher.py [시트 경로] [출력 폴더]
# 필요: pip install pillow numpy scipy
SRC = sys.argv[1] if len(sys.argv) > 1 else 'ArtSource/Towers/dreamcatcher_sheet.jpg'
OUT = sys.argv[2] if len(sys.argv) > 2 else 'Assets/Art/Towers/Dreamcatcher'
os.makedirs(OUT, exist_ok=True)
rgb = np.asarray(Image.open(SRC).convert('RGB')).astype(np.float32)
m = rgb.max(axis=2)

# --- alpha: solid core + soft glow (unblended from black) ---
CORE = 42.0
core = m > CORE
core = ndi.binary_fill_holes(ndi.binary_closing(core, iterations=2))
alpha = np.clip(m / CORE, 0, 1)
alpha[core] = 1.0
alpha[m < 6] = 0.0
color = np.where(alpha[..., None] > 0, rgb / np.maximum(alpha[..., None], 1e-3), 0)
color = np.clip(color, 0, 255)
rgba = np.dstack([color, alpha * 255]).astype(np.uint8)

lab, _ = ndi.label(ndi.binary_dilation(m > 40, iterations=6))

def main_component(box):
    x0, y0, x1, y1 = box
    sub = lab[y0:y1, x0:x1]
    ids, counts = np.unique(sub[sub > 0], return_counts=True)
    return ids[np.argmax(counts)]

def hanger(box):
    """Top tip of the main object (the hanging loop) -> anchor x, y."""
    x0, y0, x1, y1 = box
    cid = main_component(box)
    obj = (lab[y0:y1, x0:x1] == cid) & (m[y0:y1, x0:x1] > 80)
    ys, xs = np.nonzero(obj)
    top = ys.min()
    row_x = xs[ys <= top + 3]
    return x0 + row_x.mean(), y0 + top

def crop_anchored(box, anchor, half_w, up, down):
    ax, ay = anchor
    ax, ay = int(round(ax)), int(round(ay))
    canvas = np.zeros((up + down, half_w * 2, 4), np.uint8)
    x0, y0, x1, y1 = box
    # only copy pixels from inside this frame's region (avoid neighbours)
    for y in range(max(y0, ay - up), min(y1, ay + down)):
        cx0 = max(x0, ax - half_w); cx1 = min(x1, ax + half_w)
        canvas[y - (ay - up), cx0 - (ax - half_w):cx1 - (ax - half_w)] = rgba[y, cx0:cx1]
    return Image.fromarray(canvas, 'RGBA')

def tight(box):
    x0, y0, x1, y1 = box
    a = rgba[y0:y1, x0:x1]
    ys, xs = np.nonzero(a[..., 3] > 8)
    pad = 2
    return Image.fromarray(a[max(ys.min()-pad,0):ys.max()+pad+1, max(xs.min()-pad,0):xs.max()+pad+1], 'RGBA')

row1_cols = [0, 214, 402, 597, 803, 978, 1168, 1382]
row2_cols = [0, 175, 339, 503, 669, 848, 1026, 1382]
idle = [(row1_cols[i], 40, row1_cols[i+1], 362) for i in range(7)]
attack = [(row2_cols[i], 362, row2_cols[i+1], 640) for i in range(7)]
build = [(430, 640, 597, 880), (597, 640, 790, 880)]
gray = (790, 630, 995, 880)

# Ring centre sits ~D px below the hanger tip; centre the canvas on it so the default pivot works.
anchors = {('idle', i): hanger(b) for i, b in enumerate(idle)}
anchors.update({('attack', i): hanger(b) for i, b in enumerate(attack)})
anchors.update({('build', i): hanger(b) for i, b in enumerate(build)})
anchors[('gray', 0)] = hanger(gray)

D = 105          # hanger tip -> ring centre
HALF_W = 175     # wide enough for the burst frame
UP = D + 30      # canvas top above ring centre... measured from hanger
CANVAS_H = 2 * 200
up_from_hanger = 200 - D          # canvas centre = ring centre
down_from_hanger = CANVAS_H - up_from_hanger

meta = {}
for (kind, i), anc in anchors.items():
    box = {'idle': idle, 'attack': attack, 'build': build}.get(kind, [gray])[i]
    img = crop_anchored(box, anc, HALF_W, up_from_hanger, down_from_hanger)
    name = f'dreamcatcher_{kind}_{i}.png' if kind != 'gray' else 'dreamcatcher_disabled.png'
    img.save(f'{OUT}/{name}')  # disabled 프레임은 아이콘을 만든 뒤 지운다
    meta[name] = [round(anc[0]), round(anc[1])]

# projectile feather (ring forward): rotate so the ring points to +x
feather = tight((225, 645, 330, 780))
fa = np.asarray(feather)[..., 3] > 128
ys, xs = np.nonzero(fa)
# ring = topmost opaque pixels; body = centroid
ring = np.array([xs[ys <= ys.min() + 4].mean(), ys.min()])
body = np.array([xs.mean(), ys.mean()])
v = ring - body
angle = np.degrees(np.arctan2(-v[1], v[0]))   # image y down
feather.rotate(-angle, expand=True, resample=Image.BICUBIC).save(f'{OUT}/feather_projectile.png')

icons = {'icon_feather': (990, 690, 1080, 840), 'icon_crystal': (1090, 685, 1165, 830),
         'icon_star': (1170, 690, 1260, 830), 'icon_lantern': (1262, 690, 1350, 830)}
for n, b in icons.items():
    tight(b).save(f'{OUT}/{n}.png')

print('feather angle', angle)

# HUD button icons: tight crops of the idle frame and the grey (can't afford) version
def tight_img(img):
    a = np.asarray(img); ys, xs = np.nonzero(a[..., 3] > 8)
    return Image.fromarray(a[ys.min():ys.max()+1, xs.min():xs.max()+1], 'RGBA')
tight_img(Image.open(f'{OUT}/dreamcatcher_idle_0.png')).save(f'{OUT}/icon_tower.png')
tight_img(Image.open(f'{OUT}/dreamcatcher_disabled.png')).save(f'{OUT}/icon_tower_disabled.png')
os.remove(f'{OUT}/dreamcatcher_disabled.png')
print('done ->', OUT)
