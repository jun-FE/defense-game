"""투명 PNG 원본 레이어를 게임용으로 가져온다: 투명한 여백을 잘라내고 영문 이름으로 저장.

사용: python3 Tools/Art/import_layers.py <원본 폴더> <출력 폴더> 원본파일=새이름 ... [--keep-canvas]
  --keep-canvas: 여백을 자르지 않는다(모든 층이 같은 캔버스 좌표를 공유할 때. 예: 로비)
"""
import os, sys, unicodedata
import numpy as np
from PIL import Image


def main(argv):
    if len(argv) < 3:
        print(__doc__)
        return 1
    src, out = argv[0], argv[1]
    keep = '--keep-canvas' in argv
    os.makedirs(out, exist_ok=True)
    # Mac에서 올린 한글 파일명은 자모가 분리된 형태(NFD)라 정규화해서 찾는다.
    files = {unicodedata.normalize('NFC', f): f for f in os.listdir(src)}
    for pair in argv[2:]:
        if pair.startswith('--'):
            continue
        name, new = pair.split('=', 1)
        real = files.get(unicodedata.normalize('NFC', name))
        if real is None:
            print(f'없음: {name}')
            return 1
        im = Image.open(os.path.join(src, real)).convert('RGBA')
        if not keep:
            a = np.asarray(im)[..., 3]
            ys, xs = np.nonzero(a > 4)
            if len(xs):
                im = im.crop((max(xs.min() - 2, 0), max(ys.min() - 2, 0), min(xs.max() + 3, im.width), min(ys.max() + 3, im.height)))
        im.save(os.path.join(out, new + '.png'), optimize=True)
        print(f'{name} -> {new}.png {im.size}')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
