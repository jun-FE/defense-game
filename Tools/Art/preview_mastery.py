"""기술(꿈 수틀) 화면 미리보기(Unity 없이 배치 확인용): MasteryPanel.cs·Mastery.cs와 같은 좌표로 1920×1080 그림을 그린다.
사용: python3 Tools/Art/preview_mastery.py out.png
예시: 스탠드 조명(해금), 기본 손질 2개 + 불씨 등불 각성까지 수놓은 상태. 글자는 Unity와 조금 다르다."""
import math
import os
import sys
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
RES = os.path.join(ROOT, 'Assets/Resources')
FONT = '/usr/share/fonts/truetype/wqy/wqy-zenhei.ttc'
W, H = 1920, 1080
INK, MUTED, BROWN, LIGHT = (46, 33, 26), (107, 87, 69), (138, 85, 33), (243, 235, 222)
TOWERS = [('TW_LAMP', '스탠드 조명', '불씨 등불'), ('TW_SCISSORS', None, None), ('TW_SEWING', None, None),
          ('TW_TAPE', None, None), ('TW_PIN', None, None), ('TW_TAGBOW', None, None)]
BRANCHES = [('불씨 등불', (255, 128, 38)), ('달빛 등불', (204, 184, 255)), ('프리즘 등불', (255, 153, 242))]
SIZE = {'C': 132, 'B': 66, 'A': 80, 'D': 96}


def img(p):
    return Image.open(os.path.join(RES, p + '.png')).convert('RGBA')


def fit(c, im, cx, cy, size, alpha=1.0, tint=None):
    s = size / max(im.size)
    im = im.resize((max(1, int(im.width * s)), max(1, int(im.height * s))), Image.LANCZOS)
    if tint:
        r, g, b, a = im.split()
        r = r.point(lambda v: int(v * tint[0] / 255)); g = g.point(lambda v: int(v * tint[1] / 255)); b = b.point(lambda v: int(v * tint[2] / 255))
        im = Image.merge('RGBA', (r, g, b, a))
    if alpha < 1:
        im.putalpha(im.split()[3].point(lambda v: int(v * alpha)))
    c.alpha_composite(im, (int(cx - im.width / 2), int(cy - im.height / 2)))


def box(c, p, x, y, w, h):
    c.alpha_composite(img(p).resize((int(w), int(h)), Image.LANCZOS), (int(x), int(y)))


def text(d, xy, s, size, color, anchor='lm', bold=False):
    d.text(xy, s, font=ImageFont.truetype(FONT, size), fill=color, anchor=anchor, stroke_width=1 if bold else 0, stroke_fill=color)


def main():
    out = sys.argv[1]
    c = Image.new('RGBA', (W, H), (40, 30, 30, 255))
    bg = os.path.join(ROOT, 'ArtSource/Main_UI/reference/시안3.png')
    if os.path.exists(bg):
        c.alpha_composite(Image.open(bg).convert('RGBA').resize((W, H)))
    c.alpha_composite(Image.new('RGBA', (W, H), (0, 0, 0, 153)))
    d = ImageDraw.Draw(c)
    aspect = 1200 / 1109
    listW, panelW, gap = 250, 450, 28
    hw = min(700, (H - 170) / aspect)
    x0 = (W - (listW + gap + hw + gap + panelW)) / 2
    lx, ly, lh = x0, 96, H - 230
    hx, hy = lx + listW + gap, 46
    px, py, ph = hx + hw + gap, 96, H - 230

    # 도구 목록
    text(d, (lx, ly - 30), '수선 도구', 24, LIGHT, bold=True)
    rowH = min(104, (lh - 50) / 6)
    for i, (tid, name, sub) in enumerate(TOWERS):
        y = ly + i * (rowH + 10)
        box(c, 'Codex/cx_slot_on' if i == 0 else 'Codex/cx_slot' if name else 'Codex/cx_slot_lock', lx, y, listW, rowH)
        a = rowH - 20
        if name:
            fit(c, img(f'Codex/Entries/{tid}_1'), lx + 14 + a / 2, y + 10 + a / 2, a)
        else:
            fit(c, img('Codex/cx_icon_unknown'), lx + 14 + a / 2, y + 10 + a / 2, a * 0.6)
        text(d, (lx + 24 + a, y + 29), name or '???', 19, INK if name else LIGHT, bold=True)
        text(d, (lx + 24 + a, y + 57), sub or '잠김', 15, (150, 80, 30) if sub else (200, 195, 190))

    hoop = img('Mastery/ms_hoop').resize((int(hw), int(hw * aspect)), Image.LANCZOS)
    c.alpha_composite(hoop, (int(hx), int(hy)))
    cx, cy, R = hx + hw * 0.4994, hy + hw * aspect * 0.537, hw * 0.46 * 0.98

    def P(r, a):
        return cx + math.cos(math.radians(a)) * r * R, cy + math.sin(math.radians(a)) * r * R

    def stitch(p, q, pa, pb, lit, alpha, tint=None):
        dx, dy = q[0] - p[0], q[1] - p[1]
        ln = math.hypot(dx, dy)
        step, size = (13, 16) if lit else (15, 12)
        knot = img('Mastery/ms_knot_on' if lit else 'Mastery/ms_knot_off')
        s = pa + step / 2
        while s < ln - pb:
            fit(c, knot, p[0] + dx / ln * s, p[1] + dy / ln * s, size, alpha, tint)
            s += step

    for k in range(6):
        a = -90 + k * 60
        stitch(P(.12, a), P(1, a), 0, 0, False, .3)
        for ring in (.32, .6, .86):
            stitch(P(ring, a), P(ring, a + 60), 8, 8, False, .3)
    nodes = {'C': (0, 0)}
    for i in range(3):
        a = -90 + i * 120
        nodes[f'B{i}'] = (.32, a); nodes[f'A{i}'] = (.6, a); nodes[f'D{i}'] = (.86, a)
    links = [('C', f'B{i}') for i in range(3)] + [(f'B{i}', f'B{(i + 1) % 3}') for i in range(3)]
    links += [(f'B{i}', f'A{i}') for i in range(3)] + [(f'A{i}', f'D{i}') for i in range(3)]
    learned = {'C', 'B0', 'B1', 'A0'}
    for a_, b_ in links:
        lit = a_ in learned and b_ in learned
        br = int(b_[1]) if b_[0] in 'AD' else None
        tint = tuple(int(255 * .3 + v * .7) for v in BRANCHES[br][1]) if lit and br is not None else None
        stitch(P(*nodes[a_]), P(*nodes[b_]), SIZE[a_[0]] / 2, SIZE[b_[0]] / 2, lit, 1 if (a_ in learned or b_ in learned) else .7, tint)
    icons = ['Codex/cx_icon_damage', 'Codex/cx_icon_speed', 'Codex/cx_icon_range']
    for k, (r, a) in nodes.items():
        x, y = P(r, a)
        kind = k[0]
        on = k in learned
        reach = any((l[0] == k and l[1] in learned) or (l[1] == k and l[0] in learned) for l in links)
        size = SIZE[kind]
        if kind in 'AD':
            col = BRANCHES[int(k[1])][1]
            fit(c, img('Mastery/ms_knot_on'), x, y, size * 1.5, .5 if on else .18, col)
        if kind == 'C':
            fit(c, img('Mastery/ms_center'), x, y, size)
            fit(c, img('Codex/Entries/TW_LAMP_1'), x, y, size * .62)
            continue
        if kind == 'D':
            fit(c, img('Mastery/ms_key_on' if on else 'Mastery/ms_key_locked'), x, y, size)
        else:
            fit(c, img('Mastery/' + ('ms_node_on' if on else 'ms_node_open' if reach else 'ms_node_locked')), x, y, size * (1.09 if on else 1.06 if reach else 1))
        inner = icons[int(k[1])] if kind == 'B' else f"Codex/Entries/TW_LAMP_{2 if kind == 'A' else 3}"
        tint = None if kind == 'B' else tuple(int(127 + v / 2) for v in BRANCHES[int(k[1])][1])
        fit(c, img(inner), x, y, size * (.5 if kind in 'BD' else .62), 1 if on else .9 if reach else .6, tint)
    for i, (name, col) in enumerate(BRANCHES):
        x, y = P(.86, -90 + i * 120)
        below = 60 if 0 < -90 + i * 120 < 180 else -92
        text(d, (x, y + below + 15), name + (' ✦' if i == 0 else ''), 17, tuple(int(v * .75 + 64) for v in col), 'mm', True)

    box(c, 'Codex/cx_slot', px, py, panelW, ph)
    x, y = px + 36, py + 28
    text(d, (x, y + 18), '기술 · 꿈 수틀', 26, INK, bold=True)
    y += 40
    fit(c, img('Mastery/ms_point'), x + 15, y + 22, 44)
    text(d, (x + 40, y + 22), '실마리  3 / 7', 22, BROWN, bold=True)
    y += 44
    text(d, (x, y + 11), '도감에 받을 기록 보상이 1개 있어요', 14, (128, 30, 46), bold=True)
    y += 26
    fit(c, img('Codex/cx_divider'), px + panelW / 2, y + 19, 360)
    y += 46
    fit(c, img('Mastery/ms_knot_on'), x + 44, y + 44, 88 * 1.5, .55, BRANCHES[0][1])
    fit(c, img('Mastery/ms_node_on'), x + 44, y + 44, 88 * 1.09)
    fit(c, img('Codex/Entries/TW_LAMP_2'), x + 44, y + 44, 54, tint=(255, 191, 146))
    text(d, (x + 102, y + 20), '불씨 옮기기', 24, INK, bold=True)
    text(d, (x + 102, y + 49), '속성 각성 · 불씨 등불', 15, (150, 80, 30), bold=True)
    text(d, (x + 102, y + 71), '실마리 2개', 15, BROWN, bold=True)
    y += 98
    for i, line in enumerate(['빛줄기가 닿은 악몽에 불이 붙어요. 맞은 악몽이 3초', '동안 초당 8 화상 피해를 입어요(방어 무시).']):
        text(d, (x, y + 10 + i * 24), line, 17, INK)
    text(d, (x, y + 70), '수놓았어요.', 15, BROWN, bold=True)
    by = py + ph - 232
    text(d, (x, by + 12), '전투에 쓸 속성 (하나만)', 15, INK, bold=True)
    bw = (panelW - 72 - 16) / 3
    for i, (name, col) in enumerate(BRANCHES):
        cxp = x + i * (bw + 8)
        box(c, 'Codex/cx_slot_on' if i == 0 else 'Codex/cx_slot', cxp, by + 30, bw, 64)
        fit(c, img('Mastery/ms_knot_on'), cxp + 19, by + 62, 22, 1 if i == 0 else .5, col if i == 0 else (150, 148, 140))
        text(d, (cxp + 32, by + 52), name, 14, INK if i == 0 else MUTED, bold=i == 0)
        text(d, (cxp + 32, by + 72), '사용 중' if i == 0 else '배우지 않음', 12, MUTED)
    text(d, (px + panelW - 36, py + ph - 18), '이 도구 모두 풀기', 14, BROWN, 'rm', True)
    fit(c, img('Codex/cx_seal'), px + panelW - 26, py + 5, 90)
    text(d, (px + panelW - 26, py + 5), '닫기', 17, LIGHT, 'mm', True)
    c.convert('RGB').save(out)


if __name__ == '__main__':
    main()
