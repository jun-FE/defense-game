"""메인(수선소) 화면 UI를 Unity 없이 그림으로 미리보기한다. MainUI.cs의 좌표·조각(ws_*)·9-slice 폭을 그대로 흉내 낸다.
사용: python3 Tools/Art/preview_main_ui.py <출력.png>
(글꼴은 시스템 한글 글꼴로 대신하므로 게임과 글자 모양은 다르다. 배치와 조각 확인용.)"""
import json
import os
import sys
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
sys.path.insert(0, os.path.join(ROOT, 'Tools/Art'))
import preview_layers  # noqa: E402

UI = os.path.join(ROOT, 'Assets/Art/Main/UI')
FONT = '/usr/share/fonts/truetype/wqy/wqy-zenhei.ttc'
W, H = 1920, 1080
Light, Muted, Ink, InkMuted, Gold, Brown = '#F3EBDD', '#B9AC9A', '#2E2219', '#6B5744', '#E0BE7C', '#8A5520'

borders = {}
for it in json.load(open(os.path.join(UI, 'ui_borders.json'), encoding='utf-8'))['items']:
    borders[it['name']] = (it.get('bx') or it['border'], it.get('by') or it['border'])
cache = {}


def img(name):
    if name not in cache:
        cache[name] = Image.open(os.path.join(UI, name + '.png')).convert('RGBA')
    return cache[name]


def frame(canvas, r, name):
    """9-slice: 모서리는 그대로, 변과 가운데만 늘린다(IMGUI GUIStyle.border와 같음)."""
    x, y, w, h = [round(v) for v in r]
    src = img(name)
    bx, by = borders.get(name, (0, 0))
    if bx == 0 and by == 0:
        canvas.alpha_composite(src.resize((w, h), Image.LANCZOS), (x, y))
        return
    bx, by = min(bx, w // 2, src.width // 2), min(by, h // 2, src.height // 2)
    sx = [0, bx, src.width - bx, src.width]
    sy = [0, by, src.height - by, src.height]
    dx = [0, bx, w - bx, w]
    dy = [0, by, h - by, h]
    for i in range(3):
        for j in range(3):
            sw, sh = sx[i + 1] - sx[i], sy[j + 1] - sy[j]
            tw, th = dx[i + 1] - dx[i], dy[j + 1] - dy[j]
            if sw <= 0 or sh <= 0 or tw <= 0 or th <= 0:
                continue
            piece = src.crop((sx[i], sy[j], sx[i + 1], sy[j + 1])).resize((tw, th), Image.LANCZOS)
            canvas.alpha_composite(piece, (x + dx[i], y + dy[j]))


def icon(canvas, r, name):
    """ScaleToFit: 비율 유지, 가운데."""
    x, y, w, h = r
    src = img(name)
    k = min(w / src.width, h / src.height)
    im = src.resize((max(1, round(src.width * k)), max(1, round(src.height * k))), Image.LANCZOS)
    canvas.alpha_composite(im, (round(x + (w - im.width) / 2), round(y + (h - im.height) / 2)))


def text(d, r, s, size, color, anchor='l', bold=False):
    font = ImageFont.truetype(FONT, size)
    x, y, w, h = r
    tw = d.textlength(s, font=font)
    tx = x if anchor == 'l' else x + (w - tw) / 2 if anchor == 'c' else x + w - tw
    ty = y + (h - size) / 2 - size * 0.1
    if bold:
        d.text((tx + 0.6, ty), s, font=font, fill=color)
    d.text((tx, ty), s, font=font, fill=color)


def main():
    canvas = preview_layers.compose().convert('RGBA')
    d = ImageDraw.Draw(canvas)

    # 로고와 부제
    icon(canvas, (30, 18, 470, 150), 'ws_logo')
    text(d, (60, 150, 700, 36), '잊힌 꿈도, 다시 꿰맬 수 있으니까.', 24, Light)

    # 해명도
    r = (48, 196, 420, 128)
    frame(canvas, r, 'ws_card_info')
    icon(canvas, (r[0] + 20, r[1] + 24, 80, 80), 'ws_moon_emblem')
    text(d, (r[0] + 116, r[1] + 14, 200, 40), '해명도', 28, Ink, bold=True)
    text(d, (r[0] + r[2] - 120, r[1] + 14, 96, 40), '0%', 24, Ink, 'r')
    frame(canvas, (r[0] + 116, r[1] + 58, r[2] - 140, 26), 'ws_gauge_track')
    text(d, (r[0] + 116, r[1] + 88, r[2] - 130, 30), '의뢰의 진실을 밝혀 획득', 18, InkMuted)

    # 재화 바, 설정·전체
    bar = (W - 660, 24, 470, 72)
    frame(canvas, bar, 'ws_bar')
    for (dx, dy, iw, ih, name) in [(34, 14, 34, 44, 'ws_icon_crystal'), (180, 16, 40, 40, 'ws_icon_coin'), (326, 14, 40, 44, 'ws_icon_key')]:
        ir = (bar[0] + dx, bar[1] + dy, iw, ih)
        icon(canvas, ir, name)
        text(d, (ir[0] + iw + 12, ir[1] + ih / 2 - 20, 100, 40), '0', 24, Light)
    for i, label in enumerate(['설정', '전체']):
        tr = (W - 176 + i * 78, 24, 70, 72)
        frame(canvas, tr, 'ws_tag')
        text(d, (tr[0], tr[1] + 14, tr[2], tr[3] - 14), label, 16, Ink, 'c')

    # 의뢰함
    r = (W - 470, 112, 440, 800)
    frame(canvas, r, 'ws_panel_list')
    icon(canvas, (r[0] + 30, r[1] + 12, 42, 32), 'ws_envelope_small')
    text(d, (r[0] + 82, r[1] + 4, 200, 48), '의뢰함', 28, Light, bold=True)
    text(d, (r[0] + r[2] - 140, r[1] + 4, 112, 48), '3 / 10', 20, Gold, 'r')
    for i, name in enumerate(['전체', '진행 중', '완료']):
        tr = (r[0] + 30 + i * 128, r[1] + 76, 120, 48)
        frame(canvas, tr, 'ws_tab_on' if i == 0 else 'ws_tab_off')
        text(d, tr, name, 18, Light if i == 0 else Ink, 'c')
    quests = [('잊힌 토끼의 꿈', '#인형  #상실  #어린 시절'), ('꺼지지 않는 불빛', '#촛불  #기다림  #어른'),
              ('비를 피하는 우산', '#우산  #그림자  #이별'), ('말하지 못한 편지', '#편지  #후회  #용서'),
              ('먼지 속의 멜로디', '#음악  #기억  #오르골')]
    y = r[1] + 142
    for i, (title, tags) in enumerate(quests):
        sel = i == 0
        ch = 112 if sel else 94
        cr = (r[0] + 26, y, r[2] - 52, ch)
        frame(canvas, cr, 'ws_card_on' if sel else 'ws_card')
        tx = cr[0] + 26
        if sel:
            th = (cr[0] + 18, cr[1] + 18, ch - 36, ch - 36)
            frame(canvas, th, 'ws_photo')
            tx = th[0] + th[2] + 18
        text(d, (tx, cr[1] + ch * 0.16, 260, 38), title, 24 if sel else 21, Ink, bold=sel)
        text(d, (tx, cr[1] + ch * 0.56, 260, 28), tags, 16, InkMuted)
        icon(canvas, (cr[0] + cr[2] - 54, cr[1] + ch / 2 - 16, 32, 32), 'ws_diamond_on' if sel else 'ws_diamond_off')
        y += ch + 12

    # 의뢰 상세(편지)
    r = (W - 960, 112, 470, 800)
    frame(canvas, r, 'ws_panel_letter')
    badge = (r[0] + r[2] / 2 - 70, r[1] + 34, 140, 36)
    frame(canvas, badge, 'ws_badge')
    text(d, badge, '진행 가능', 16, Light, 'c')
    text(d, (r[0] + 24, r[1] + 80, r[2] - 48, 50), '잊힌 토끼의 꿈', 32, Ink, 'c', bold=True)
    chips = ['#인형', '#상실', '#어린 시절']
    font = ImageFont.truetype(FONT, 14)
    widths = [d.textlength(c, font=font) + 28 for c in chips]
    x = r[0] + r[2] / 2 - (sum(widths) + 8 * (len(chips) - 1)) / 2
    for c, cw in zip(chips, widths):
        frame(canvas, (x, r[1] + 136, cw, 28), 'ws_chip')
        text(d, (x, r[1] + 136, cw, 28), c, 14, InkMuted, 'c')
        x += cw + 8
    frame(canvas, (r[0] + 40, r[1] + 182, r[2] - 80, 200), 'ws_photo')
    letter = ['"언니가 다시 안아줬으면 좋겠어요…"', '오래된 토끼 인형에게서 온 편지입니다. 낡았지만,',
              '여전히 누군가를 기다리는 꿈이 남아 있는 것 같아요.']
    for i, line in enumerate(letter):
        text(d, (r[0] + 40, r[1] + 398 + i * 30, r[2] - 80, 30), line, 19, Ink)
    y = r[1] + 538
    text(d, (r[0] + 40, y, 200, 30), '의뢰 보상', 20, Ink, bold=True)
    for i, (ic, name) in enumerate([('ws_reward_moon', '달빛 조각'), ('ws_reward_thread', '푸른 실타래'), ('ws_reward_note', '낡은 쪽지')]):
        sr = (r[0] + 40 + i * 98, y + 38, 82, 82)
        frame(canvas, sr, 'ws_slot')
        icon(canvas, (sr[0] + 20, sr[1] + 20, 42, 42), ic)
        text(d, (sr[0] - 8, sr[1] + 84, 98, 22), name, 13, InkMuted, 'c')
    br = (r[0] + 20, r[1] + r[3] - 104, r[2] - 40, 84)
    frame(canvas, br, 'ws_button')
    text(d, (br[0], br[1] - 5, br[2], br[3]), '의뢰 시작하기', 26, Light, 'c', bold=True)

    # 도하 쪽지
    r = (48, H - 262, 560, 112)
    frame(canvas, r, 'ws_bubble')
    text(d, (r[0] + 34, r[1] + 16, 300, 34), '야옹…', 22, Brown, bold=True)
    text(d, (r[0] + 34, r[1] + 52, 460, 44), '오늘은 어떤 꿈을 고쳐볼까?', 24, Ink)
    text(d, (r[0] + r[2] - 60, r[1] + 34, 40, 44), '›', 40, Brown, 'c')

    # 하단 탭
    names = ['수선소', '의뢰함', '도감', '가방', '기술', '지도']
    icons = ['ws_icon_workshop', 'ws_icon_envelope', 'ws_icon_codex', 'ws_icon_bag', 'ws_icon_mastery', 'ws_icon_map']
    bw, bh, gap = 168, 104, 10
    x0 = (W - len(names) * bw - (len(names) - 1) * gap) / 2
    y = H - bh - 6
    for i in range(len(names)):
        rr = (x0 + i * (bw + gap), y, bw, bh)
        active = i == 1
        tag = (rr[0] + bw / 2 - 56, rr[1] - 6, 112, 112)
        layer = Image.new('RGBA', canvas.size)
        icon(layer, tag, 'ws_nav_on' if active else 'ws_nav')
        icon(layer, (tag[0] + 56 - 24, tag[1] + 18, 48, 44), icons[i])
        if i in (2, 3, 5):  # 잠긴 탭은 반투명
            layer.putalpha(layer.getchannel('A').point(lambda v: int(v * 0.55)))
        canvas.alpha_composite(layer)
        text(d, (rr[0], tag[1] + 62, bw, 34), names[i], 20, Brown if active else Ink, 'c', bold=active)

    canvas.convert('RGB').save(sys.argv[1], quality=90)


main()
