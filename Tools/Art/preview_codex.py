"""도감 화면 미리보기(Unity 없이 배치 확인용): CodexPanel.cs와 같은 좌표로 1920×1080 그림을 그린다.
사용: python3 Tools/Art/preview_codex.py out.png [tool|nightmare] [번호]
글자 모양·크기는 Unity와 조금 다르다(대략적인 배치만 확인)."""
import os
import sys
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
RES = os.path.join(ROOT, 'Assets/Resources/Codex')
FONT = '/usr/share/fonts/truetype/wqy/wqy-zenhei.ttc'
W, H = 1920, 1080
INK, MUTED, BROWN, LIGHT = (46, 33, 26), (107, 87, 69), (138, 85, 33), (243, 235, 222)

TOOLS = [('TW_LAMP', '스탠드 조명'), ('TW_SCISSORS', '가위'), ('TW_SEWING', '재봉틀'),
         ('TW_TAPE', '줄자'), ('TW_PIN', '바늘꽂이'), ('TW_TAGBOW', '수선표 활')]
NIGHTS = [('EN_TOY', '실뭉치 악몽'), ('EN_RUSH', '실밥 그림자'), ('EN_HEAVY', '누더기 곰')]


def img(name):
    return Image.open(os.path.join(RES, name + '.png')).convert('RGBA')


def fit(canvas, im, r):
    x, y, w, h = r
    s = min(w / im.width, h / im.height)
    im = im.resize((max(1, int(im.width * s)), max(1, int(im.height * s))), Image.LANCZOS)
    canvas.alpha_composite(im, (int(x + (w - im.width) / 2), int(y + (h - im.height) / 2)))


def text(d, r, s, size, color, anchor='mm', bold=False):
    f = ImageFont.truetype(FONT, size)
    x, y, w, h = r
    pos = {'mm': (x + w / 2, y + h / 2), 'lm': (x, y + h / 2), 'lt': (x, y)}[anchor]
    d.text(pos, s, font=f, fill=color, anchor=anchor, stroke_width=1 if bold else 0, stroke_fill=color)


def main():
    out = sys.argv[1]
    tab = sys.argv[2] if len(sys.argv) > 2 else 'tool'
    sel = int(sys.argv[3]) if len(sys.argv) > 3 else 0
    c = Image.new('RGBA', (W, H), (40, 30, 30, 255))
    bg = os.path.join(ROOT, 'ArtSource/Main_UI/reference/시안3.png')
    if os.path.exists(bg):
        c.alpha_composite(Image.open(bg).convert('RGBA').resize((W, H)))
    c.alpha_composite(Image.new('RGBA', (W, H), (0, 0, 0, 153)))
    d = ImageDraw.Draw(c)
    bw = min(1440, W - 120)
    bh = bw * 1004 / 1800
    bx, by = (W - bw) / 2, max(70, (H - 110 - bh) / 2 + 24)
    for i, (kind, name, label) in enumerate([('tool', 'cx_mark_red', '도구'), ('nightmare', 'cx_mark_blue', '악몽')]):
        on = tab == kind
        r = (bx + bw * 0.60 + i * 92, by - 96 + (0 if on else 26), 78, 160)
        fit(c, img(name if on else name + '_off'), r)
        text(d, (r[0], r[1] + 50, 78, 30), label[0], 22, LIGHT if on else MUTED, bold=True)
        text(d, (r[0], r[1] + 76, 78, 30), label[1], 22, LIGHT if on else MUTED, bold=True)
    fit(c, img('cx_book'), (bx, by, bw, bh))
    L = (bx + bw * 0.085, by + bh * 0.115, bw * 0.38, bh * 0.74)
    R = (bx + bw * 0.535, by + bh * 0.115, bw * 0.38, bh * 0.74)
    entries = TOOLS if tab == 'tool' else NIGHTS
    text(d, (L[0], L[1] + 4, L[2], 50), '수선 도구' if tab == 'tool' else '악몽 기록', 34, INK, bold=True)
    text(d, (L[0], L[1] + 52, L[2], 28), f'공방에서 쓰는 도구  6 / 6' if tab == 'tool' else '만난 악몽  2 / 3', 17, MUTED)
    fit(c, img('cx_divider'), (L[0] + L[2] / 2 - 200, L[1] + 82, 400, 45))
    slot = min(150, (L[2] - 40) / 3 - 14)
    gap = (L[2] - 3 * slot) / 4
    for i, (eid, name) in enumerate(entries):
        known = tab == 'tool' or i < 2
        r = (L[0] + gap + (i % 3) * (slot + gap), L[1] + 140 + (i // 3) * (slot + 52), slot, slot * 0.94)
        fit(c, img('cx_slot_lock' if not known else 'cx_slot_on' if i == sel else 'cx_slot'), r)
        if known:
            p = f'Entries/{eid}_1' if tab == 'tool' else f'Entries/{eid}'
            fit(c, img(p), (r[0] + r[2] * .16, r[1] + r[3] * .16, r[2] * .68, r[3] * .68))
        text(d, (r[0] - 10, r[1] + r[3] + 2, r[2] + 20, 30), name if known else '???', 18, BROWN if i == sel else INK, bold=i == sel)
    eid, name = entries[sel]
    fr = (R[0] + 6, R[1] + 6, 220, 215)
    fit(c, img('cx_frame'), fr)
    fit(c, img(f'Entries/{eid}_1' if tab == 'tool' else f'Entries/{eid}'), (fr[0] + 44, fr[1] + 43, 132, 129))
    tx = fr[0] + fr[2] + 18
    text(d, (tx, R[1] + 10, 300, 50), name, 34, INK, 'lm', True)
    text(d, (tx, R[1] + 58, 300, 28), '수선 도구 · 원거리 · 한 명 집중' if tab == 'tool' else '악몽 · 평범 · 가장 흔한 악몽', 17, MUTED, 'lm')
    if tab == 'tool':
        for lv in range(3):
            b = (tx + lv * 92, R[1] + 136, 84, 52)
            fit(c, img('cx_slot_on' if lv == 0 else 'cx_slot'), b)
            text(d, b, f'{lv + 1}단계', 17, INK)
    else:
        fit(c, img('cx_seal'), (R[0] + R[2] - 92, R[1] + 120, 84, 86))
    fit(c, img('cx_divider'), (R[0], fr[1] + fr[3] + 6, R[2], 40))
    y = fr[1] + fr[3] + 54
    lore = ['도하의 작업대를 밤새 비추던 낡은 스탠드. 이안의 랜턴에서', '불씨를 나눠 받은 뒤로, 빛줄기 끝에 닿은 악몽의 실을', '바삭하게 태워 버린다. 빛이 닿는 곳의 실은 다시 엉키지', '않는다는 것이 공방의 오래된 믿음이다.']
    for i, line in enumerate(lore):
        text(d, (R[0] + 10, y + i * 25, R[2], 25), line, 18, INK, 'lm')
    y += len(lore) * 25 + 12
    for icon, label, val, ratio in [('cx_icon_damage', '공격력', '20', .15), ('cx_icon_speed', '공격 간격', '1초', .4), ('cx_icon_range', '사거리', '4칸', .66)]:
        fit(c, img(icon), (R[0] + 10, y + 2, 30, 30))
        text(d, (R[0] + 48, y, 140, 34), label, 17, INK, 'lm', True)
        tr = (R[0] + 190, y + 9, R[2] - 300, 18)
        c.alpha_composite(img('cx_bar_track').resize((int(tr[2]), 18)), (int(tr[0]), int(tr[1])))
        c.alpha_composite(img('cx_bar_fill').resize((max(30, int((tr[2] - 4) * ratio)), 16)), (int(tr[0] + 2), int(tr[1] + 1)))
        text(d, (tr[0] + tr[2] + 10, y, 80, 34), val, 17, INK, 'lm')
        y += 36
    text(d, (R[0] + 10, y + 6, R[2], 30), '도하의 메모 — 가장 먼저 놓기 좋은 도구야.', 17, BROWN, 'lm')
    fit(c, img('cx_seal'), (bx + bw - 96, by - 22, 88, 90))
    text(d, (bx + bw - 96, by - 22, 88, 90), '닫기', 17, LIGHT, bold=True)
    c.convert('RGB').save(out)


if __name__ == '__main__':
    main()
