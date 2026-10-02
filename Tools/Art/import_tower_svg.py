#!/usr/bin/env python3
"""타워 4방향·3단계 SVG 프레임 → 게임용 PNG.

원본 구조(기획 전달): <원본폴더>/<방향>/L<단계>/<방향>_L<단계>_F<프레임>.svg
  방향 up/down/left/right, 단계 1~3, 프레임 01~06, 캔버스 400×400, 받침 기준점 (200,370).
결과: Assets/Art/Towers/<이름>/<이름>_<방향>_L<단계>_<0~5>.png, icon.png, icon_disabled.png

시트에서 잘라낸 프레임 가장자리에 옆 칸의 창끝 조각이 묻어 있는 경우가 있어서,
캔버스 가장자리에 닿은 작은 조각(본체와 떨어진 것)은 지운다. 본체·이펙트는 건드리지 않는다.

사용: python3 Tools/Art/import_tower_svg.py ArtSource/Towers/병정인형_4방향_3단계_공격모션_SVG Soldier soldier
필요: rsvg-convert, Pillow, numpy, scipy
"""
import os, subprocess, sys, tempfile, unicodedata
import numpy as np
from PIL import Image, ImageEnhance
from scipy import ndimage

DIRS = ['up', 'down', 'left', 'right']
SIZE = 400


def find_dir(path):
    """맥에서 올린 한글 폴더 이름(NFD)도 찾는다."""
    if os.path.isdir(path):
        return path
    parent, name = os.path.split(path.rstrip('/'))
    for entry in os.listdir(parent or '.'):
        if unicodedata.normalize('NFC', entry) == unicodedata.normalize('NFC', name):
            return os.path.join(parent, entry)
    sys.exit(f'폴더를 찾을 수 없습니다: {path}')


def render(svg):
    with tempfile.NamedTemporaryFile(suffix='.png') as tmp:
        subprocess.run(['rsvg-convert', '-w', str(SIZE), '-h', str(SIZE), svg, '-o', tmp.name], check=True)
        return Image.open(tmp.name).convert('RGBA')


def clean_edge_fragments(im):
    a = np.array(im)
    mask = a[:, :, 3] > 8
    labels, count = ndimage.label(mask, structure=np.ones((3, 3)))
    if count <= 1:
        return im, 0
    sizes = ndimage.sum(mask, labels, range(1, count + 1))
    biggest = sizes.max()
    edge = set(np.unique(np.concatenate([labels[0], labels[-1], labels[:, 0], labels[:, -1]]))) - {0}
    removed = 0
    for lab in edge:
        if sizes[lab - 1] < biggest * 0.15:
            a[labels == lab, 3] = 0
            removed += 1
    return Image.fromarray(a), removed


def make_icon(frame, out_dir):
    box = frame.getbbox()
    crop = frame.crop(box)
    side = max(crop.size) + 16
    icon = Image.new('RGBA', (side, side))
    icon.paste(crop, ((side - crop.width) // 2, (side - crop.height) // 2))
    icon = icon.resize((192, 192), Image.LANCZOS)
    icon.save(os.path.join(out_dir, 'icon.png'))
    gray = ImageEnhance.Brightness(ImageEnhance.Color(icon).enhance(0.0)).enhance(0.55)
    gray.putalpha(icon.getchannel('A').point(lambda v: v * 0.8))
    gray.save(os.path.join(out_dir, 'icon_disabled.png'))


def main():
    if len(sys.argv) < 4:
        sys.exit(__doc__)
    src, folder, prefix = find_dir(sys.argv[1]), sys.argv[2], sys.argv[3]
    out_dir = os.path.join('Assets/Art/Towers', folder)
    os.makedirs(out_dir, exist_ok=True)
    total_removed = 0
    for d in DIRS:
        for level in (1, 2, 3):
            for f in range(1, 7):
                svg = os.path.join(src, d, f'L{level}', f'{d}_L{level}_F{f:02d}.svg')
                if not os.path.exists(svg):
                    sys.exit(f'없는 프레임: {svg}')
                im, removed = clean_edge_fragments(render(svg))
                if removed:
                    print(f'  {d} L{level} F{f}: 가장자리 조각 {removed}개 제거')
                total_removed += removed
                im.save(os.path.join(out_dir, f'{prefix}_{d}_L{level}_{f - 1}.png'), optimize=True)
    make_icon(Image.open(os.path.join(out_dir, f'{prefix}_down_L1_0.png')), out_dir)
    print(f'완료: {out_dir} (프레임 72장, 아이콘 2장, 제거한 조각 {total_removed}개)')


if __name__ == '__main__':
    main()
