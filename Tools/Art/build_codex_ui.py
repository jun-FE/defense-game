"""도감 화면 그림 만들기(Unity 없이): ArtSource/Codex/{book,pieces}.png(Artlist 생성, 초록 배경)
→ Assets/Resources/Codex/cx_*.png (투명 배경, 화면 크기에 맞춰 줄임)
사용: python3 Tools/Art/build_codex_ui.py
pieces.png 안 조각 위치는 아래 표(원본 픽셀 좌표 x0, y0, x1, y1)에 적혀 있다. 다시 생성하면 좌표도 다시 맞춘다."""
import os
import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
# slice_ui_sheet.py는 불러오면 바로 실행되므로 key_green 함수 부분만 가져온다
_src = open(os.path.join(ROOT, 'Tools/Art/slice_ui_sheet.py'), encoding='utf-8').read()
_ns = {}
exec(_src[:_src.index('def main():')], _ns)
key_green = _ns['key_green']

SRC = os.path.join(ROOT, 'ArtSource/Codex')
OUT = os.path.join(ROOT, 'Assets/Resources/Codex')

# (저장 이름, 상자, 저장할 긴 변 길이)
PIECES = [
    ('cx_slot', (51, 48, 571, 537), 256),
    ('cx_slot_on', (641, 32, 1202, 556), 256),
    ('cx_slot_lock', (1277, 48, 1799, 536), 256),
    ('cx_frame', (1901, 45, 2702, 826), 512),
    ('cx_mark_red', (82, 595, 291, 1011), 220),
    ('cx_mark_red_off', (362, 596, 570, 1010), 220),
    ('cx_mark_blue', (753, 596, 960, 1010), 220),
    ('cx_mark_blue_off', (1035, 597, 1241, 1009), 220),
    ('cx_bar_track', (1420, 874, 2359, 985), 470),
    ('cx_bar_fill', (1420, 1025, 2359, 1130), 470),
    ('cx_seal', (2435, 864, 2696, 1132), 160),
    ('cx_divider', (52, 1025, 1323, 1177), 640),
    ('cx_icon_damage', (69, 1187, 321, 1459), 128),
    ('cx_icon_speed', (421, 1190, 642, 1459), 128),
    ('cx_icon_range', (735, 1185, 980, 1468), 128),
    ('cx_icon_armor', (1098, 1189, 1333, 1461), 128),
    ('cx_icon_move', (1431, 1193, 1680, 1453), 128),
    ('cx_icon_core', (1776, 1186, 2010, 1463), 128),
    ('cx_icon_reward', (2110, 1193, 2359, 1460), 128),
    ('cx_icon_unknown', (2455, 1180, 2675, 1470), 256),
]


def keyed(path):
    im = np.asarray(Image.open(path).convert('RGB'), float) / 255.0
    rgb, alpha = key_green(im)
    return np.dstack([rgb, alpha])


def save(rgba, name, longest):
    ys, xs = np.nonzero(rgba[..., 3] > 0.02)
    rgba = rgba[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    img = Image.fromarray((rgba * 255).round().astype(np.uint8), 'RGBA')
    s = longest / max(img.size)
    if s < 1:
        img = img.resize((max(1, round(img.width * s)), max(1, round(img.height * s))), Image.LANCZOS)
    img.save(os.path.join(OUT, name + '.png'))
    print(name, img.size)


# 도감 그림: 게임에 쓰는 타워(단계별)·적 그림을 Resources로 복사한다(도감은 Resources에서 읽음).
TOWERS = {'TW_LAMP': 'Lamp/lamp', 'TW_SCISSORS': 'Scissors/scissors', 'TW_SEWING': 'Sewing/sewing',
          'TW_TAPE': 'Tape/tape', 'TW_PIN': 'Pin/pin', 'TW_TAGBOW': 'TagBow/tagbow'}
ENEMIES = {'EN_TOY': 'Tangle/tangle', 'EN_RUSH': 'Shade/shade', 'EN_HEAVY': 'Bear/bear'}


def copy_entries():
    out = os.path.join(OUT, 'Entries')
    os.makedirs(out, exist_ok=True)
    for tid, stem in TOWERS.items():
        for lv in (1, 2, 3):
            Image.open(os.path.join(ROOT, 'Assets/Art/Towers', f'{stem}_L{lv}_idle0.png')).save(os.path.join(out, f'{tid}_{lv}.png'))
    for eid, stem in ENEMIES.items():
        Image.open(os.path.join(ROOT, 'Assets/Art/Enemies', f'{stem}_0.png')).save(os.path.join(out, f'{eid}.png'))
    print('도감 그림', len(TOWERS) * 3 + len(ENEMIES), '장')


def main():
    os.makedirs(OUT, exist_ok=True)
    copy_entries()
    save(keyed(os.path.join(SRC, 'book.png')), 'cx_book', 1800)
    sheet = keyed(os.path.join(SRC, 'pieces.png'))
    for name, (x0, y0, x1, y1), longest in PIECES:
        save(sheet[y0:y1, x0:x1].copy(), name, longest)


if __name__ == '__main__':
    main()
