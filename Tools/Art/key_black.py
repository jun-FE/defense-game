"""검은 배경 이미지를 투명 PNG로 바꾼다(이미지 생성기·카카오톡 JPEG용).

사용: python3 Tools/Art/key_black.py <입력 파일 또는 폴더> <출력 폴더> [원본파일명=새이름 ...] [--solid=새이름,새이름]
  --solid: 인물·가구처럼 속이 꽉 찬 물체. 막힌 빈 곳을 크기와 상관없이 모두 불투명으로 메운다.
           (지정하지 않은 파일은 창문 구멍처럼 큰 빈 곳을 투명하게 남긴다.)
필요: pip install pillow numpy scipy

방식: 밝기(RGB 최댓값)가 CORE 이상인 부분을 몸통으로 보고 안쪽 구멍을 메운 뒤 불투명 처리하고,
그보다 어두운 가장자리는 검은 배경에서 '빛이 섞인 정도'만큼 반투명으로 되돌린다(빛 번짐 보존).
JPEG 압축 때문에 생긴 검정 근처 잡티(밝기 < NOISE)는 지운다.
"""
import sys, os, glob
import numpy as np
from PIL import Image
from scipy import ndimage as ndi

CORE = 40.0
NOISE = 10.0
MAX_HOLE = 400  # 이 픽셀 수 이하의 막힌 틈만 불투명으로 메운다


def key_black(path, solid=False):
    rgb = np.asarray(Image.open(path).convert('RGB')).astype(np.float32)
    m = rgb.max(axis=2)
    core = m > CORE
    core = ndi.binary_closing(core, iterations=2)
    # 몸통 안의 작은 어두운 틈만 메운다. 창문·나침반 안쪽처럼 큰 빈 곳은 투명하게 남긴다.
    holes = ndi.binary_fill_holes(core) & ~core
    hlab, hn = ndi.label(holes)
    if hn:
        hsizes = ndi.sum(holes, hlab, range(1, hn + 1))
        limit = float("inf") if solid else MAX_HOLE
        core |= np.isin(hlab, np.nonzero(hsizes <= limit)[0] + 1)
    # 작은 잡티 덩어리 제거
    lab, n = ndi.label(core)
    if n:
        sizes = ndi.sum(core, lab, range(1, n + 1))
        core = np.isin(lab, np.nonzero(sizes >= 30)[0] + 1)
    alpha = np.clip((m - NOISE) / (CORE - NOISE), 0, 1)
    alpha[core] = 1.0
    color = np.where(alpha[..., None] > 0, rgb / np.maximum(alpha[..., None], 1e-3), 0)
    rgba = np.dstack([np.clip(color, 0, 255), alpha * 255]).astype(np.uint8)
    # 투명한 여백을 잘라낸다(배치할 때 크기를 다루기 쉽고 텍스처도 작아진다).
    ys, xs = np.nonzero(rgba[..., 3] > 4)
    if len(xs):
        rgba = rgba[max(ys.min() - 2, 0):ys.max() + 3, max(xs.min() - 2, 0):xs.max() + 3]
    return Image.fromarray(rgba, 'RGBA')


def main(argv):
    if len(argv) < 2:
        print(__doc__)
        return 1
    src, out = argv[0], argv[1]
    solid = set()
    for a in argv[2:]:
        if a.startswith('--solid='):
            solid.update(x for x in a.split('=', 1)[1].split(',') if x)
    names = dict(a.split('=', 1) for a in argv[2:] if '=' in a and not a.startswith('--'))
    os.makedirs(out, exist_ok=True)
    files = sorted(glob.glob(os.path.join(src, '*'))) if os.path.isdir(src) else [src]
    for f in files:
        if not f.lower().endswith(('.png', '.jpg', '.jpeg')):
            continue
        base = os.path.basename(f)
        name = names.get(base, os.path.splitext(base)[0])
        key_black(f, solid=name in solid).save(os.path.join(out, name + '.png'))
        print(base, '->', name + '.png')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
