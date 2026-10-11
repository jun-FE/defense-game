"""초록 배경(크로마키) 위에 그린 UI 조각 시트를 투명 배경 조각 PNG로 자른다(Unity 없이).
사용: python3 Tools/Art/slice_ui_sheet.py <시트.png> <출력 폴더> [이름1 이름2 ...] [--min=면적] [--gap=픽셀] [--preview=미리보기.png]
- 배경의 순수한 초록(#00FF00 근처)을 투명으로 바꾸고, 가장자리에 묻은 초록빛을 걷어 낸다(디스필).
- 서로 떨어진 덩어리를 하나씩 찾아 위→아래, 왼→오른 순서(줄 단위)로 번호를 매긴다.
- 이름을 주면 그 순서대로 저장하고(이름이 '-'이면 건너뜀), 안 주면 piece_00.png …으로 저장한다.
- --preview를 주면 번호를 매긴 미리보기 이미지를 저장한다(이름 순서를 맞출 때 확인용)."""
import os
import sys
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage


def key_green(rgb):
    """초록 배경(약 0, 0.98, 0)과 섞인 픽셀을 원래 색과 투명도로 되돌린다.
    배경은 빨강·파랑이 0이므로, 초록이 센 픽셀의 투명도는 빨강·파랑 중 큰 값으로 어림한다(빛 번짐도 자연스럽게)."""
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    spill = g - np.maximum(r, b)              # 초록이 다른 색보다 얼마나 센가
    keyed = spill > 0.02
    alpha = np.where(keyed, np.clip((np.maximum(r, b) - 0.15) / 0.8, 0.0, 1.0), 1.0)
    bg = np.array([0.0, 0.98, 0.0])
    a = np.maximum(alpha, 1e-3)[..., None]
    fg = np.clip((rgb - (1.0 - alpha)[..., None] * bg) / a, 0.0, 1.0)
    hi = np.maximum(fg[..., 0], fg[..., 2])
    lo = np.minimum(fg[..., 0], fg[..., 2])
    # 남은 초록빛은 빼되, 너무 빼서 분홍빛이 되지 않게(올 풀린 천 가장자리) 아래쪽도 막는다
    fg[..., 1] = np.clip(fg[..., 1], lo * 0.9, hi + 0.08)
    out = np.where(keyed[..., None], fg, rgb)
    return out, alpha


def main():
    args = [a for a in sys.argv[1:] if not a.startswith('--')]
    opts = dict(a[2:].split('=', 1) for a in sys.argv[1:] if a.startswith('--') and '=' in a)
    src, out_dir, names = args[0], args[1], args[2:]
    min_area = int(opts.get('min', 1500))
    gap = int(opts.get('gap', 6))
    os.makedirs(out_dir, exist_ok=True)

    im = np.asarray(Image.open(src).convert('RGB'), float) / 255.0
    rgb, alpha = key_green(im)
    solid = alpha > 0.5
    # 빛 번짐처럼 살짝 떨어진 부분은 같은 조각으로 묶는다
    grown = ndimage.binary_dilation(solid, iterations=gap)
    labels, count = ndimage.label(grown)
    boxes = ndimage.find_objects(labels)
    pieces = []
    for i, sl in enumerate(boxes):
        mask = (labels[sl] == i + 1) & (alpha[sl] > 0.02)
        if mask.sum() < min_area:
            continue
        pieces.append((sl, i + 1))

    # 줄 단위 정렬: 세로 중심이 비슷하면 같은 줄로 보고 왼쪽부터
    def center(p):
        sl = p[0]
        return ((sl[0].start + sl[0].stop) / 2, (sl[1].start + sl[1].stop) / 2, sl[0].stop - sl[0].start)
    pieces.sort(key=lambda p: center(p)[0])
    rows, current = [], []
    for p in pieces:
        cy, cx, h = center(p)
        if current and cy - center(current[0])[0] > max(40, 0.35 * center(current[0])[2]):
            rows.append(current)
            current = []
        current.append(p)
    if current:
        rows.append(current)
    ordered = [p for row in rows for p in sorted(row, key=lambda p: center(p)[1])]

    rgba = np.dstack([rgb, alpha])
    saved = []
    for idx, (sl, lab) in enumerate(ordered):
        name = names[idx] if idx < len(names) else f'piece_{idx:02d}'
        if name == '-':
            continue
        y0, y1, x0, x1 = sl[0].start, sl[0].stop, sl[1].start, sl[1].stop
        crop = rgba[y0:y1, x0:x1].copy()
        crop[..., 3] *= (labels[y0:y1, x0:x1] == lab)
        ys, xs = np.nonzero(crop[..., 3] > 0.01)
        crop = crop[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
        Image.fromarray((np.clip(crop, 0, 1) * 255).astype('uint8'), 'RGBA').save(os.path.join(out_dir, name + '.png'))
        saved.append((idx, name, (x0, y0, x1, y1)))
    print(f'{len(ordered)}개 조각 중 {len(saved)}개 저장 -> {out_dir}')
    for idx, name, box in saved:
        print(f'  {idx:2d} {name} {box}')

    if 'preview' in opts:
        prev = Image.open(src).convert('RGB')
        d = ImageDraw.Draw(prev)
        for idx, (sl, lab) in enumerate(ordered):
            box = (sl[1].start, sl[0].start, sl[1].stop, sl[0].stop)
            d.rectangle(box, outline=(255, 0, 0), width=6)
            d.text((box[0] + 10, box[1] + 10), str(idx), fill=(255, 0, 0))
        prev.thumbnail((1400, 1400))
        prev.save(opts['preview'])


main()
