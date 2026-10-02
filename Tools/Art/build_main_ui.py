"""메인 UI 벡터 팩(ArtSource/Main_UI/*.svg)의 도형·색·그라데이션을 그대로 써서
게임에서 쓰기 좋은 조각 PNG(Assets/Art/Main/UI)를 만든다.

- 원본 SVG에는 예시 글자("잊힌 토끼의 꿈", "의뢰 시작하기" 등)가 박혀 있어서 그대로는 쓸 수 없다.
  글자를 뺀 빈 틀, 상태별 버튼, 아이콘을 따로 그린다.
- 늘어나는 틀(패널·카드·버튼)은 9-slice로 쓰므로 작은 크기로 그리고, 게임에서 모서리 폭(border)만큼은 늘이지 않는다.
- 색·모양을 바꾸려면 원본 SVG를 고친 뒤 이 스크립트의 해당 조각도 같은 값으로 고치고 다시 실행한다.

사용: python3 Tools/Art/build_main_ui.py   (rsvg-convert 필요: brew install librsvg)
결과: Assets/Art/Main/UI/*.png + ui_borders.json(9-slice 모서리 폭, 픽셀)
"""
import json, os, subprocess, tempfile

OUT = 'Assets/Art/Main/UI'
SCALE = 2  # 아이콘은 2배로 그린다. 늘어나는 틀(9-slice)은 디자인 크기 그대로(1배) 그려야 모서리가 시안과 같은 두께로 보인다.

DEFS = '''<defs>
<linearGradient id="g_indigo" x1="0" x2="1" y1="0" y2="1"><stop offset="0%" stop-color="#24213A"/><stop offset="100%" stop-color="#17162A"/></linearGradient>
<linearGradient id="g_paper" x1="0" x2="0" y1="0" y2="1"><stop offset="0%" stop-color="#E8DCC5"/><stop offset="100%" stop-color="#D8C6A7"/></linearGradient>
<filter id="softGlow" x="-50%" y="-50%" width="200%" height="200%"><feGaussianBlur stdDeviation="4" result="b"/><feMerge><feMergeNode in="b"/><feMergeNode in="SourceGraphic"/></feMerge></filter>
</defs>'''

GOLD, GOLD_LIGHT, LAVENDER = '#C59A59', '#E0BE7C', '#B7A9FF'

# 이름: (폭, 높이, SVG 본문, 9-slice 모서리 폭 또는 None)
PIECES = {
    # ── 틀 (9-slice) ──
    'panel_indigo': (128, 128, f'<rect x="1.5" y="1.5" width="125" height="125" rx="24" fill="url(#g_indigo)" stroke="{GOLD}" stroke-width="3"/>', 30),
    'panel_paper':  (128, 128, f'<rect x="2" y="2" width="124" height="124" rx="22" fill="url(#g_paper)" stroke="{GOLD}" stroke-width="4"/>', 30),
    'card_dark':    (96, 96,   '<rect x="1" y="1" width="94" height="94" rx="14" fill="#2A2840" stroke="#5C547D" stroke-width="2"/>', 18),
    'card_dark_hover': (96, 96, '<rect x="1" y="1" width="94" height="94" rx="14" fill="#34314F" stroke="#8A7BFF" stroke-width="2"/>', 18),
    'card_paper':   (96, 96,   f'<rect x="1.5" y="1.5" width="93" height="93" rx="14" fill="url(#g_paper)" stroke="{GOLD_LIGHT}" stroke-width="3"/>', 18),
    'tab_on':       (64, 44,   '<rect x="1" y="1" width="62" height="42" rx="10" fill="#514B86" stroke="#8A7BFF" stroke-width="2"/>', 12),
    'tab_off':      (64, 44,   '<rect x="0" y="0" width="64" height="44" rx="10" fill="#211F37"/>', 12),
    'button_primary': (200, 96, f'''<rect x="10" y="4" width="180" height="88" rx="22" fill="#514B86" stroke="{GOLD_LIGHT}" stroke-width="4"/>
        <path d="M26 14 L40 4 L50 14 M150 14 L160 4 L174 14 M26 82 L40 92 L50 82 M150 82 L160 92 L174 82" stroke="{GOLD_LIGHT}" stroke-width="3" fill="none"/>''', 56),
    'button_primary_hover': (200, 96, f'''<rect x="10" y="4" width="180" height="88" rx="22" fill="#6A62A8" stroke="#F3D79C" stroke-width="4"/>
        <path d="M26 14 L40 4 L50 14 M150 14 L160 4 L174 14 M26 82 L40 92 L50 82 M150 82 L160 92 L174 82" stroke="#F3D79C" stroke-width="3" fill="none"/>''', 56),
    'bar_indigo':   (96, 64,   f'<rect x="1.5" y="1.5" width="93" height="61" rx="18" fill="url(#g_indigo)" stroke="{GOLD}" stroke-width="3"/>', 22),
    'chip_tag':     (48, 28,   '<rect x="0" y="0" width="48" height="28" rx="14" fill="#D3C4AA"/>', 14),
    'badge':        (60, 38,   '<rect x="0" y="0" width="60" height="38" rx="19" fill="#514B86"/>', 19),
    'gauge_track':  (64, 24,   f'<rect x="1" y="1" width="62" height="22" rx="11" fill="#11101D" stroke="{GOLD}" stroke-width="2"/>', 12),
    'gauge_fill':   (32, 16,   f'<rect x="0" y="0" width="32" height="16" rx="8" fill="{GOLD_LIGHT}"/>', 8),
    'slot_reward':  (96, 96,   f'<rect x="2" y="2" width="92" height="92" rx="18" fill="#24213A" stroke="{GOLD}" stroke-width="3"/>', 22),
    'line_gold':    (64, 4,    f'<rect x="0" y="0" width="64" height="4" fill="{GOLD}" opacity="0.7"/>', 2),
    # ── 하단 탭 (늘이지 않음) ──
    'nav_button':   (180, 112, '<path d="M2 76 C38 76 42 2 90 2 C138 2 142 76 178 76 L178 110 L2 110 Z" fill="url(#g_indigo)" stroke="#C59A59" stroke-width="3"/>', None),
    'nav_button_on': (180, 112, '<path d="M2 76 C38 76 42 2 90 2 C138 2 142 76 178 76 L178 110 L2 110 Z" fill="#33305A" stroke="#E0BE7C" stroke-width="4"/>', None),
    'nav_emblem':   (56, 56,   '<circle cx="28" cy="28" r="26" fill="#211F37" stroke="#E0BE7C" stroke-width="2"/>', None),
    # ── 아이콘 ──
    'icon_workshop': (40, 40,  f'<path d="M6 32 Q20 2 34 32 Z" fill="{LAVENDER}"/>', None),
    'icon_crystal': (34, 52,   f'<polygon points="17,2 32,26 17,50 2,26" fill="{LAVENDER}" stroke="{GOLD_LIGHT}" stroke-width="2"/>', None),
    'icon_coin':    (40, 40,   f'<circle cx="20" cy="20" r="18" fill="#5B4A2F" stroke="{GOLD_LIGHT}" stroke-width="2"/>', None),
    'icon_key':     (64, 40,   f'<circle cx="18" cy="20" r="15" fill="none" stroke="{GOLD_LIGHT}" stroke-width="6"/><rect x="32" y="15" width="30" height="10" rx="5" fill="{GOLD_LIGHT}"/>', None),
    'icon_moon':    (76, 76,   f'<path d="M38 4 A34 34 0 1 0 66 58 A27 27 0 1 1 38 4Z" fill="{LAVENDER}" opacity="0.95"/>', None),
    'moon_emblem':  (84, 84,   f'<circle cx="42" cy="42" r="40" fill="#211F37" stroke="{GOLD_LIGHT}" stroke-width="3"/><path d="M42 8 A34 34 0 1 0 70 62 A27 27 0 1 1 42 8Z" fill="{LAVENDER}" opacity="0.95"/>', None),
    'reward_moon':  (64, 64,   f'<path d="M30 6 A24 24 0 1 0 46 46 A18 18 0 1 1 30 6Z" fill="{LAVENDER}"/>', None),
    'reward_thread': (64, 64,  '<circle cx="32" cy="32" r="18" fill="#50749D"/><path d="M15 26 H49 M15 38 H49" stroke="#F3EBDD" stroke-width="2"/>', None),
    'reward_note':  (64, 64,   '<rect x="11" y="9" width="42" height="46" fill="#E8DCC5" transform="rotate(-8 32 32)"/>', None),
    'diamond_on':   (40, 40,   f'<polygon points="20,4 36,20 20,36 4,20" fill="#776AD9" stroke="{GOLD}" stroke-width="2"/>', None),
    'diamond_off':  (36, 36,   '<polygon points="18,4 32,18 18,32 4,18" fill="none" stroke="#776AD9" stroke-width="2"/>', None),
    'envelope_small': (48, 34, f'<rect x="2" y="2" width="44" height="30" rx="4" fill="#E8DCC5" stroke="{GOLD}" stroke-width="2"/><path d="M4 6 L24 19 L44 6" fill="none" stroke="#2A2330" stroke-width="2"/>', None),
    'photo_placeholder': (476, 248, f'<rect x="1" y="1" width="474" height="246" rx="16" fill="#B4A18A" stroke="{GOLD}" stroke-width="2"/><path d="M16 228 C114 168, 162 278, 254 210 S394 172,460 230" fill="none" stroke="#8E7B69" stroke-width="6" opacity="0.6"/>', None),
    'thumb_placeholder': (82, 82, '<rect x="0" y="0" width="82" height="82" rx="8" fill="#9B8A72"/><circle cx="41" cy="41" r="24" fill="#C8B59A"/>', None),
}

# 원본 SVG를 그대로 쓰는 것(글자 없음)
AS_IS = ['icon_envelope', 'icon_codex', 'icon_bag', 'icon_mastery', 'icon_map',
         'lobby_dreamcatcher', 'lobby_hanging_moon_star', 'ornament_corner']


def render(svg_text, path, scale=SCALE):
    with tempfile.NamedTemporaryFile('w', suffix='.svg', delete=False) as f:
        f.write(svg_text)
        tmp = f.name
    subprocess.run(['rsvg-convert', '-z', str(scale), tmp, '-o', path], check=True)
    os.unlink(tmp)


def main():
    os.makedirs(OUT, exist_ok=True)
    borders = {}
    for name, (w, h, body, border) in PIECES.items():
        svg = f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}">{DEFS}{body}</svg>'
        render(svg, f'{OUT}/{name}.png', scale=1 if border is not None else SCALE)
        if border is not None:
            borders[name] = border
    for name in AS_IS:
        with open(f'ArtSource/Main_UI/{name}.svg', encoding='utf-8') as f:
            render(f.read(), f'{OUT}/{name}.png', scale=1)
    # Unity JsonUtility가 읽을 수 있게 목록 형태로 저장
    with open(f'{OUT}/ui_borders.json', 'w') as f:
        json.dump({'items': [{'name': k, 'border': v} for k, v in borders.items()]}, f, indent=1)
    print(f'{len(PIECES) + len(AS_IS)}개 조각 → {OUT}')


if __name__ == '__main__':
    main()
