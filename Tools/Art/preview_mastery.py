"""기술(꿈 수틀) 화면 미리보기(Unity 없이 배치 확인용): MasteryPanel.cs·Mastery.cs와 같은 좌표로 1920×1080 그림을 그린다.
사용: python3 Tools/Art/preview_mastery.py out.png
예시로 가운데 + 스탠드 줄 두 칸 + 공방 매듭 하나를 수놓은 상태를 그린다. 글자는 Unity와 조금 다르다."""
import math
import os
import sys
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
RES = os.path.join(ROOT, 'Assets/Resources')
FONT = '/usr/share/fonts/truetype/wqy/wqy-zenhei.ttc'
W, H = 1920, 1080
INK, MUTED, BROWN, LIGHT = (46, 33, 26), (107, 87, 69), (138, 85, 33), (243, 235, 222)
TOWERS = ['TW_LAMP', 'TW_SCISSORS', 'TW_SEWING', 'TW_TAPE', 'TW_PIN', 'TW_TAGBOW']
SIZE = {'center': 124, 'inner': 66, 'spoke': 74, 'weave': 60, 'key': 100}


def img(p):
    return Image.open(os.path.join(RES, p + '.png')).convert('RGBA')


def fit(c, im, cx, cy, size, alpha=1.0, grey=False):
    s = size / max(im.size)
    im = im.resize((max(1, int(im.width * s)), max(1, int(im.height * s))), Image.LANCZOS)
    if grey:
        im = Image.blend(im, Image.new('RGBA', im.size, (120, 115, 110, 0)), 0.45)
    if alpha < 1:
        a = im.split()[3].point(lambda v: int(v * alpha))
        im.putalpha(a)
    c.alpha_composite(im, (int(cx - im.width / 2), int(cy - im.height / 2)))


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
    hw = min(780, (H - 150) / aspect)
    total = hw + 50 + 470
    hx, hy = (W - total) / 2, 52
    hoop = img('Mastery/ms_hoop').resize((int(hw), int(hw * aspect)), Image.LANCZOS)
    c.alpha_composite(hoop, (int(hx), int(hy)))
    cx, cy, R = hx + hw * 0.4994, hy + hw * aspect * 0.537, hw * 0.46 * 0.98

    nodes = {'C': ('center', 0, 0, None)}
    for i, t in enumerate(TOWERS):
        a = -90 + i * 60
        nodes[f'I{i}'] = ('inner', .3, a, t)
        nodes[f'S{i}'] = ('spoke', .57, a, t)
        nodes[f'K{i}'] = ('key', .82, a, t)
        nodes[f'W{i}'] = ('weave', .57, a + 30, None)
    links = []
    for i in range(6):
        n = (i + 1) % 6
        links += [('C', f'I{i}'), (f'I{i}', f'I{n}'), (f'I{i}', f'S{i}'), (f'S{i}', f'K{i}'), (f'S{i}', f'W{i}'), (f'W{i}', f'S{n}')]
    learned = {'C', 'I0', 'S0', 'W0'}

    def pos(k, r=None):
        kind, rr, a, _ = nodes[k]
        rr = rr if r is None else r
        return cx + math.cos(math.radians(a)) * rr * R, cy + math.sin(math.radians(a)) * rr * R

    def stitch(p, q, pa, pb, lit, alpha):
        dx, dy = q[0] - p[0], q[1] - p[1]
        ln = math.hypot(dx, dy)
        step, size = (13, 16) if lit else (15, 13)
        knot = img('Mastery/ms_knot_on' if lit else 'Mastery/ms_knot_off')
        s = pa + step / 2
        while s < ln - pb:
            fit(c, knot, p[0] + dx / ln * s, p[1] + dy / ln * s, size, alpha)
            s += step

    for i in range(6):
        stitch(pos(f'K{i}'), pos(f'K{i}', 1.0), SIZE['key'] / 2, 0, False, .45)
        stitch(pos(f'K{i}'), pos(f'K{(i + 1) % 6}'), SIZE['key'] / 2, SIZE['key'] / 2, False, .45)
    for a, b in links:
        lit = a in learned and b in learned
        stitch(pos(a), pos(b), SIZE[nodes[a][0]] / 2, SIZE[nodes[b][0]] / 2, lit, 1 if (a in learned or b in learned) else .75)
    lv = {'inner': 1, 'spoke': 2, 'key': 3}
    icons = ['cx_icon_reward', 'cx_icon_reward', 'cx_icon_damage', 'cx_icon_core', 'cx_icon_reward', 'cx_icon_reward']
    for k, (kind, r, a, t) in nodes.items():
        x, y = pos(k)
        on = k in learned
        reach = any((l[0] == k and l[1] in learned) or (l[1] == k and l[0] in learned) for l in links)
        size = SIZE[kind]
        if kind == 'center':
            fit(c, img('Mastery/ms_center'), x, y, size)
            continue
        if kind == 'key':
            fit(c, img('Mastery/ms_key_on' if on else 'Mastery/ms_key_locked'), x, y, size * (1.16 if on else 1))
        else:
            name = 'ms_node_on' if on else 'ms_node_open' if reach else 'ms_node_locked'
            fit(c, img('Mastery/' + name), x, y, size * (1.09 if on else 1.06 if reach else 1))
        inner = f'Codex/Entries/{t}_{lv[kind]}' if t else 'Codex/' + icons[int(k[1])]
        fit(c, img(inner), x, y, size * (0.5 if kind == 'key' else 0.6), 1 if on else .9 if reach else .6, grey=not on)

    px, py = hx + hw + 50, hy + 70
    pw, ph = 470, min(720, hw * aspect - 90)
    slot = img('Codex/cx_slot').resize((pw, int(ph)), Image.LANCZOS)
    c.alpha_composite(slot, (int(px), int(py)))
    x, y = px + 40, py + 34
    text(d, (x, y + 20), '기술 · 꿈 수틀', 28, INK, bold=True)
    y += 44
    fit(c, img('Mastery/ms_point'), x + 17, y + 24, 48)
    text(d, (x + 44, y + 24), '실마리  0 / 3', 24, BROWN, bold=True)
    y += 50
    text(d, (x, y + 12), '기본 3개 + 의뢰를 깰 때마다 2개', 15, MUTED)
    y += 26
    fit(c, img('Codex/cx_divider'), px + pw / 2, y + 21, 380)
    y += 52
    fit(c, img('Mastery/ms_node_on'), x + 48, y + 48, 96 * 1.09)
    fit(c, img('Codex/Entries/TW_LAMP_2'), x + 48, y + 48, 58)
    text(d, (x + 112, y + 24), '넓은 갓', 26, INK, bold=True)
    text(d, (x + 112, y + 56), '도구 매듭 · 스탠드 조명', 16, MUTED)
    text(d, (x + 112, y + 80), '실마리 2개', 16, BROWN, bold=True)
    y += 112
    text(d, (x, y + 10), '사거리 +0.6칸.', 18, INK)
    text(d, (x, y + 42), '수놓았어요. 전투에 적용돼요.', 16, BROWN, bold=True)
    bx, by = px + pw / 2, py + ph - 170 + 52
    fit(c, img('Mastery/ms_ribbon_off'), bx, by, 300)
    text(d, (bx, by - 8), '수놓기', 24, LIGHT, 'mm', True)
    text(d, (x, py + ph - 49), '수놓은 매듭 3 / 24', 15, MUTED)
    text(d, (px + pw - 40, py + ph - 49), '모두 풀기', 16, BROWN, 'rm', True)
    fit(c, img('Codex/cx_seal'), px + pw - 26, py + 5, 90)
    text(d, (px + pw - 26, py + 5), '닫기', 17, LIGHT, 'mm', True)
    c.convert('RGB').save(out)


if __name__ == '__main__':
    main()
