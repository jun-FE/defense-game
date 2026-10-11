"""메인(수선소) 화면 UI 조각 만들기: Artlist로 뽑은 시트(ArtSource/Main_UI/generated)를 잘라
Assets/Art/Main/UI/ws_*.png 로 저장하고, 9-slice 테두리 폭을 ui_borders.json에 넣는다.
사용: python3 Tools/Art/build_workshop_ui.py
시트 안 조각 순서는 slice_ui_sheet.py의 번호(위→아래, 왼→오른)와 같다. 미리보기로 번호 확인:
  python3 Tools/Art/slice_ui_sheet.py ArtSource/Main_UI/generated/sheet_controls.png /tmp/x --preview=/tmp/x.png
표의 값: (조각 번호, 저장 이름, 크기 배율, 가로 테두리, 세로 테두리). 테두리 0이면 통째로 늘린다.
배율은 1080p 화면에서 실제로 그려지는 크기에 맞춘 것(모서리 장식이 화면에서 이 픽셀 크기로 보인다)."""
import json
import os
import subprocess
import sys
import tempfile
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
GEN = os.path.join(ROOT, 'ArtSource/Main_UI/generated')
OUT = os.path.join(ROOT, 'Assets/Art/Main/UI')

SHEETS = {
    'sheet_panels.png': [
        (0, 'ws_card', 0.40, 36, 30),
        (1, 'ws_card_on', 0.40, 40, 34),
        (2, 'ws_panel_letter', 0.70, 150, 150),
        (3, 'ws_panel_list', 0.68, 60, 60),
        (4, 'ws_card_info', 0.50, 40, 40),
        (5, 'ws_bubble', 0.45, 60, 40),
        (6, 'ws_photo', 0.55, 40, 40),
    ],
    'sheet_controls.png': [
        (0, 'ws_tab_off', 0.30, 16, 14),
        (1, 'ws_tab_on', 0.30, 16, 14),
        (2, 'ws_bar', 0.45, 40, 36),
        (3, 'ws_tag', 0.26, 0, 0),
        (4, 'ws_button', 0.37, 74, 42),
        (6, 'ws_gauge_track', 0.20, 16, 12),
        (7, 'ws_gauge_fill', 0.20, 10, 6),
        (8, 'ws_button_hover', 0.37, 74, 42),
        (9, 'ws_chip', 0.20, 14, 12),
        (10, 'ws_badge', 0.25, 20, 16),
        (13, 'ws_slot', 0.25, 0, 0),
        (14, 'ws_nav', 0.48, 0, 0),
        (15, 'ws_nav_on', 0.48, 0, 0),
    ],
    'sheet_icons.png': [(i, n, 0.5, 0, 0) for i, n in enumerate([
        'ws_icon_workshop', 'ws_icon_envelope', 'ws_icon_codex', 'ws_icon_bag', 'ws_icon_mastery',
        'ws_icon_map', 'ws_icon_crystal', 'ws_icon_coin', 'ws_icon_key', 'ws_envelope_small',
        'ws_reward_moon', 'ws_reward_thread', 'ws_reward_note', 'ws_moon_emblem', 'ws_diamond_on',
        'ws_diamond_off', 'ws_icon_thimble', 'ws_icon_pincushion', 'ws_icon_tape', 'ws_icon_scissors'])],
}


def slice_sheet(sheet, tmp):
    subprocess.run([sys.executable, os.path.join(ROOT, 'Tools/Art/slice_ui_sheet.py'), os.path.join(GEN, sheet), tmp],
                   check=True, stdout=subprocess.DEVNULL)


def main():
    borders_path = os.path.join(OUT, 'ui_borders.json')
    data = json.load(open(borders_path, encoding='utf-8'))
    items = {it['name']: it for it in data['items']}

    for sheet, spec in SHEETS.items():
        with tempfile.TemporaryDirectory() as tmp:
            slice_sheet(sheet, tmp)
            for index, name, scale, bx, by in spec:
                im = Image.open(os.path.join(tmp, f'piece_{index:02d}.png'))
                size = (max(1, round(im.width * scale)), max(1, round(im.height * scale)))
                im.resize(size, Image.LANCZOS).save(os.path.join(OUT, name + '.png'))
                if bx or by:
                    items[name] = {'name': name, 'border': by, 'bx': bx, 'by': by}
                print(f'{name:22s} {size[0]}x{size[1]}  테두리 {bx}/{by}')

    # 로고: 시트 전체에서 초록을 빼고 글자 영역만 남긴다.
    with tempfile.TemporaryDirectory() as tmp:
        subprocess.run([sys.executable, os.path.join(ROOT, 'Tools/Art/slice_ui_sheet.py'), os.path.join(GEN, 'sheet_logo.png'), tmp,
                        '--gap=60'], check=True, stdout=subprocess.DEVNULL)
        im = Image.open(os.path.join(tmp, 'piece_00.png'))
        im.thumbnail((1000, 400), Image.LANCZOS)
        im.save(os.path.join(OUT, 'ws_logo.png'))
        print(f'ws_logo                {im.width}x{im.height}')

    data['items'] = list(items.values())
    with open(borders_path, 'w', encoding='utf-8') as f:
        json.dump(data, f, ensure_ascii=False, indent=1)
        f.write('\n')


main()
