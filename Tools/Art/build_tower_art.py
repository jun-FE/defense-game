"""수선소 물품 타워 그림 만들기(Unity 없이): ArtSource/Towers/<한글 폴더>/L{1,2,3}_{base,idle,attack}.png
→ Assets/Art/Towers/<영문>/ 의 게임용 프레임·아이콘, 건설 고리 아이콘(Assets/Art/Battle/UI/mock_tower_<영문>.png).
사용: python3 Tools/Art/build_tower_art.py [영문 이름 ...]   (안 주면 전부)
- 재봉틀(build_sewing_tower.py)과 같은 규칙: 초록 배경 제거, 맨 아래·가로 가운데를 기준 그림(base)에 맞춰 정렬, 256px 캔버스(맨 아래 = 아래 6%).
- 단계별로 idle0 = 기본, idle1 = 대기 동작, attack0 = 공격 동작. 대기·공격 그림은 받침 모양이 기본 그림과 겹치도록 맞추고(match_shift),
  캔버스 가장자리에서 잘린 빛은 부드럽게 흐린다(soften_edges).
- 원본이 없는 단계·동작은 건너뛰고(있는 그림으로 대신), 1단계 기본 그림이 없으면 그 타워 전체를 건너뛴다."""
import os
import sys
import types
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
TOWERS = {
    'lamp': ('스탠드조명', 'Lamp'),
    'scissors': ('가위', 'Scissors'),
    'tape': ('줄자', 'Tape'),
    'pin': ('바늘꽂이', 'Pin'),
    'tagbow': ('수선표활', 'TagBow'),
}

# 재봉틀 스크립트의 함수(key, legs_anchor, shift, save)를 그대로 쓴다
src = open(os.path.join(ROOT, 'Tools/Art/build_sewing_tower.py'), encoding='utf-8').read()
lib = types.ModuleType('sewing_lib')
lib.__dict__['__file__'] = os.path.join(ROOT, 'Tools/Art/build_sewing_tower.py')
exec(src[:src.index('def main():')], lib.__dict__)
slicer_src = open(os.path.join(ROOT, 'Tools/Art/slice_ui_sheet.py'), encoding='utf-8').read()
slicer = types.ModuleType('slice_ui_sheet_lib')
exec(slicer_src[:slicer_src.index('def main():')], slicer.__dict__)
sys.modules['slice_ui_sheet_lib'] = slicer
WORK = lib.WORK


def anchor(rgba):
    """기준점: 불투명 픽셀 맨 아래 줄(받침 바닥)과, 전체 모양의 가로 가운데.
    (재봉틀은 다리 가운데를 썼지만, 줄자처럼 옆으로 늘어진 부분이 있으면 아래쪽만 보면 치우친다.)"""
    import numpy as np
    solid = rgba[..., 3] > 0.6
    ys, xs = np.nonzero(solid)
    return (xs.min() + xs.max()) / 2.0, ys.max()


def match_shift(ref, frame):
    """대기·공격 그림을 기본 그림에 맞추는 이동량(px). 받침이 있는 아래쪽 45% 영역의 모양(알파)이 가장 잘 겹치는 위치를 찾는다.
    (빛줄기·줄자처럼 크게 바뀌는 부분 때문에 맨 아래·가운데 기준이 흔들리는 것을 피한다.)"""
    import numpy as np
    from scipy import signal
    k = 8
    def mask(rgba):
        a = rgba[::k, ::k, 3] > 0.6
        h = a.shape[0]
        m = np.zeros_like(a, dtype=float)
        m[int(h * 0.55):] = a[int(h * 0.55):]
        return m - m.mean()
    r, f = mask(ref), mask(frame)
    corr = signal.fftconvolve(r, f[::-1, ::-1], mode='same')
    cy, cx = np.unravel_index(np.argmax(corr), corr.shape)
    h, w = r.shape
    return (cx - w // 2) * k, (cy - h // 2) * k


def soften_edges(rgba, margin=0.08):
    """캔버스 가장자리에서 잘린 빛(공격 빛줄기 등)이 딱 끊겨 보이지 않게 가장자리 쪽 알파를 부드럽게 줄인다."""
    import numpy as np
    h, w = rgba.shape[:2]
    y, x = np.mgrid[0:h, 0:w]
    d = np.minimum(np.minimum(x, w - 1 - x), np.minimum(y, h - 1 - y)) / (min(h, w) * margin)
    out = rgba.copy()
    out[..., 3] *= np.clip(d, 0, 1) ** 1.5
    return out


def build(name):
    folder, out_name = TOWERS[name]
    src_dir = os.path.join(ROOT, 'ArtSource/Towers', folder)
    out_dir = os.path.join(ROOT, 'Assets/Art/Towers', out_name)
    if not os.path.exists(os.path.join(src_dir, 'L1_base.png')):
        print(f'{name}: L1_base.png 없음, 건너뜀')
        return
    os.makedirs(out_dir, exist_ok=True)
    last = None
    for level in (1, 2, 3):
        base_path = os.path.join(src_dir, f'L{level}_base.png')
        if not os.path.exists(base_path):
            base_path = last
        base = lib.key(base_path)
        bx, by = anchor(base)
        base_dx, base_dy = WORK / 2 - bx, WORK * 0.94 - by
        for kind, out in (('base', 'idle0'), ('idle', 'idle1'), ('attack', 'attack0')):
            path = os.path.join(src_dir, f'L{level}_{kind}.png')
            if kind == 'base' or not os.path.exists(path):
                frame = lib.shift(base, base_dx, base_dy)
            else:
                rgba = lib.key(path)
                mx, my = match_shift(base, rgba)
                frame = lib.shift(rgba, base_dx + mx, base_dy + my)
            frame = soften_edges(frame)
            lib.save(frame, os.path.join(out_dir, f'{name}_L{level}_{out}.png'))
        last = base_path
        print(f'{name} {level}단계: 기준점 ({bx:.0f},{by:.0f})')

    icon = Image.open(os.path.join(out_dir, f'{name}_L1_idle0.png'))
    icon = icon.crop(icon.getchannel('A').getbbox())
    s = max(icon.size)
    sq = Image.new('RGBA', (s, s), (0, 0, 0, 0))
    sq.alpha_composite(icon, ((s - icon.width) // 2, (s - icon.height) // 2))
    sq192 = sq.resize((192, 192), Image.LANCZOS)
    sq192.save(os.path.join(out_dir, 'icon.png'))
    gray = sq192.convert('LA').convert('RGBA')
    gray.putalpha(sq192.getchannel('A').point(lambda v: v * 0.6))
    gray.save(os.path.join(out_dir, 'icon_disabled.png'))
    ui = icon.resize((max(1, round(icon.width * 104 / icon.height)), 104), Image.LANCZOS)
    ui.save(os.path.join(ROOT, 'Assets/Art/Battle/UI', f'mock_tower_{name}.png'))


def fx_slash():
    """가위질 칼날 빛: 가로로 긴 초승달 모양(가운데 굵고 양끝 뾰족, 살짝 휨). 재봉틀 빛 조각 폴더에 함께 둔다."""
    import numpy as np
    w, h = 256, 64
    y, x = np.mgrid[0:h, 0:w]
    u = x / (w - 1) * 2 - 1                    # -1 ~ 1
    center = (h - 1) / 2 + (u * u - 0.3) * h * 0.22   # 살짝 휜 중심선
    half = (1 - u * u) ** 0.8 * h * 0.22           # 가운데 굵고 끝 뾰족
    d = np.abs(y - center) / np.maximum(half, 1e-3)
    core = np.clip(1 - d, 0, 1) ** 0.7
    glow = np.clip(1 - np.abs(y - center) / (half * 2.4 + 1e-3), 0, 1) ** 2 * 0.45
    a = np.clip(np.maximum(core, glow), 0, 1)
    img = np.dstack([np.ones((h, w, 3)), a])
    Image.fromarray((img * 255).astype('uint8'), 'RGBA').save(os.path.join(ROOT, 'Assets/Art/Towers/Sewing/fx_slash.png'))
    print('fx_slash.png')


names = sys.argv[1:] or list(TOWERS)
for n in names:
    build(n)
fx_slash()
print('완료')
