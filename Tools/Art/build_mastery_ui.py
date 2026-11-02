"""기술(꿈 수틀) 화면 그림 만들기(Unity 없이): ArtSource/Mastery/{hoop,pieces}.png(Artlist 생성, 초록 배경)
→ Assets/Resources/Mastery/ms_*.png (투명 배경, 화면 크기에 맞춰 줄임)
사용: python3 Tools/Art/build_mastery_ui.py
pieces.png·nodes.png 조각 순서는 slice_ui_sheet.py 번호(위→아래, 왼→오른)와 같다. icons.png는 5×5 칸."""
import os
import subprocess
import sys
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SRC = os.path.join(ROOT, 'ArtSource/Mastery')
OUT = os.path.join(ROOT, 'Assets/Resources/Mastery')

# 예전 조각 시트(pieces.png)에서는 리본 버튼만 쓴다. 순서 = 시트 조각 번호('-'는 건너뜀)
PIECES = [
    ('-', 0), ('-', 0), ('-', 0), ('-', 0), ('-', 0), ('-', 0), ('-', 0), ('-', 0), ('-', 0),
    ('ms_ribbon', 480), ('ms_ribbon_off', 480),
]
# 매듭 틀(nodes.png, 메인 화면 그림체: 호두나무+황동 원형 틀). 가운데 메달은 안이 빈 리넨이라 도구 그림이 잘 보인다.
NODES = [
    ('ms_node_locked', 170), ('ms_node_open', 176), ('ms_node_on', 182),
    ('ms_key_locked', 230), ('ms_key_on', 240), ('ms_center', 300),
    ('ms_knot_on', 40), ('ms_knot_off', 32),
]
# 아이콘(icons.png, 5×5 칸, 메인 화면 아이콘 그림체). 위→아래, 왼→오른.
ICONS = ['mi_damage', 'mi_speed', 'mi_range', 'mi_armor', 'mi_reward',
         'mi_core', 'mi_move', 'mi_unknown', 'el_fire', 'el_poison',
         'el_lightning', 'el_frost', 'el_moon', 'el_prism', 'el_gold',
         'el_silver', 'el_thread', 'el_thorn', 'el_spring', 'el_magnet',
         'el_blast', 'el_sniper', 'mi_point', 'mi_lock', 'mi_star']
ICON_SIZE = 160


def slice_sheet(sheet, names, out):
    subprocess.run([sys.executable, os.path.join(ROOT, 'Tools/Art/slice_ui_sheet.py'), sheet, out, *names, '--gap=10'],
                   check=True, stdout=subprocess.DEVNULL)


def shrink(path, longest):
    img = Image.open(path)
    s = longest / max(img.size)
    if s < 1:
        img = img.resize((max(1, round(img.width * s)), max(1, round(img.height * s))), Image.LANCZOS)
    img.save(path)
    print(os.path.basename(path), img.size)


def slice_icons():
    """5×5 칸으로 나눠 칸마다 초록을 빼고 가장 큰 덩어리 둘레로 자른다(빛 번짐 포함)."""
    import numpy as np
    _src = open(os.path.join(ROOT, 'Tools/Art/slice_ui_sheet.py'), encoding='utf-8').read()
    ns = {}
    exec(_src[:_src.index('def main():')], ns)
    im = np.asarray(Image.open(os.path.join(SRC, 'icons.png')).convert('RGB'), float) / 255.0
    rgb, alpha = ns['key_green'](im)
    h, w = alpha.shape
    for i, name in enumerate(ICONS):
        r, c = divmod(i, 5)
        y0, y1, x0, x1 = r * h // 5, (r + 1) * h // 5, c * w // 5, (c + 1) * w // 5
        a = alpha[y0:y1, x0:x1].copy()
        # 칸 가장자리 4px은 옆 칸 번짐일 수 있어 지운다
        a[:4, :] = a[-4:, :] = 0
        a[:, :4] = a[:, -4:] = 0
        ys, xs = np.nonzero(a > 0.03)
        crop = np.dstack([rgb[y0:y1, x0:x1], a])[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
        img = Image.fromarray((crop * 255).round().astype(np.uint8), 'RGBA')
        s = ICON_SIZE / max(img.size)
        if s < 1:
            img = img.resize((max(1, round(img.width * s)), max(1, round(img.height * s))), Image.LANCZOS)
        img.save(os.path.join(OUT, name + '.png'))
    print('아이콘', len(ICONS), '개')


def main():
    os.makedirs(OUT, exist_ok=True)
    slice_sheet(os.path.join(SRC, 'pieces.png'), [n for n, _ in PIECES], OUT)
    for name, longest in PIECES:
        if name != '-':
            shrink(os.path.join(OUT, name + '.png'), longest)
    slice_sheet(os.path.join(SRC, 'nodes.png'), [n for n, _ in NODES], OUT)
    for name, longest in NODES:
        shrink(os.path.join(OUT, name + '.png'), longest)
    slice_icons()
    # 수틀: 한 덩어리라 조각 하나로 잘린다. 화면에서 지름 약 820px.
    slice_sheet(os.path.join(SRC, 'hoop.png'), ['ms_hoop'], OUT)
    shrink(os.path.join(OUT, 'ms_hoop.png'), 1200)


if __name__ == '__main__':
    main()
