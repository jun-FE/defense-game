"""기술(꿈 수틀) 화면 그림 만들기(Unity 없이): ArtSource/Mastery/{hoop,pieces}.png(Artlist 생성, 초록 배경)
→ Assets/Resources/Mastery/ms_*.png (투명 배경, 화면 크기에 맞춰 줄임)
사용: python3 Tools/Art/build_mastery_ui.py
pieces.png 조각 순서는 slice_ui_sheet.py 번호(위→아래, 왼→오른)와 같다."""
import os
import subprocess
import sys
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SRC = os.path.join(ROOT, 'ArtSource/Mastery')
OUT = os.path.join(ROOT, 'Assets/Resources/Mastery')

# (저장 이름, 긴 변 길이). 순서 = 시트 조각 번호
PIECES = [
    ('ms_node_locked', 160), ('ms_node_open', 170), ('ms_node_on', 170),
    ('ms_key_locked', 220), ('ms_key_on', 240), ('ms_center', 260),
    ('ms_knot_on', 40), ('ms_knot_off', 32), ('ms_point', 96),
    ('ms_ribbon', 480), ('ms_ribbon_off', 480),
]


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


def main():
    os.makedirs(OUT, exist_ok=True)
    slice_sheet(os.path.join(SRC, 'pieces.png'), [n for n, _ in PIECES], OUT)
    for name, longest in PIECES:
        shrink(os.path.join(OUT, name + '.png'), longest)
    # 수틀: 한 덩어리라 조각 하나로 잘린다. 화면에서 지름 약 820px.
    slice_sheet(os.path.join(SRC, 'hoop.png'), ['ms_hoop'], OUT)
    shrink(os.path.join(OUT, 'ms_hoop.png'), 1200)


if __name__ == '__main__':
    main()
