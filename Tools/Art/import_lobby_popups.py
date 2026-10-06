#!/usr/bin/env python3
"""로비 팝업 리소스 팩(ArtSource/Lobby_popups/{이어하기,새로하기,설정,종료}) → Assets/Art/Lobby/UI/Popups.

각 팩의 png/*.png를 '{영문키}_{파일명}.png'로 복사하고(팩마다 같은 이름이 있어서 앞에 붙임),
layout.json은 '{영문키}_layout.json'으로 복사한다. 글자는 PNG에 없고 게임 코드가 그린다.
또 이미지마다 실제 몸체 영역(불투명도 200 이상)을 재서 popup_bounds.json에 적는다.
버튼 '마우스 올림' 이미지처럼 빛 번짐 때문에 여백이 다른 그림도 몸체끼리 같은 자리에 맞춰 그리기 위함이다.
사용: python3 Tools/Art/import_lobby_popups.py
"""
import json, os, shutil, unicodedata
import numpy as np
from PIL import Image

SRC = 'ArtSource/Lobby_popups'
DST = 'Assets/Art/Lobby/UI/Popups'
KEYS = {'이어하기': 'continue', '새로하기': 'new', '설정': 'settings', '종료': 'quit'}


def main():
    os.makedirs(DST, exist_ok=True)
    count = 0
    for entry in os.listdir(SRC):
        key = KEYS.get(unicodedata.normalize('NFC', entry))
        if key is None:
            continue
        folder = os.path.join(SRC, entry)
        png = os.path.join(folder, 'png')
        for name in sorted(os.listdir(png)):
            if name.lower().endswith('.png'):
                shutil.copyfile(os.path.join(png, name), os.path.join(DST, f'{key}_{name}'))
                count += 1
        shutil.copyfile(os.path.join(folder, 'layout.json'), os.path.join(DST, f'{key}_layout.json'))
    items = []
    for name in sorted(os.listdir(DST)):
        if not name.endswith('.png'):
            continue
        im = Image.open(os.path.join(DST, name)).convert('RGBA')
        alpha = np.asarray(im)[..., 3]
        for threshold in (200, 40, 1):
            ys, xs = np.nonzero(alpha >= threshold)
            if len(xs):
                break
        x0, y0, x1, y1 = (int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1) if len(xs) else (0, 0, im.width, im.height)
        items.append({'name': name[:-4], 'texWidth': im.width, 'texHeight': im.height, 'x': x0, 'y': y0, 'width': x1 - x0, 'height': y1 - y0})
    with open(os.path.join(DST, 'popup_bounds.json'), 'w') as f:
        json.dump({'items': items}, f, ensure_ascii=False, indent=1)
    print(f'완료: {DST} (이미지 {count}장, 배치 4개, 몸체 영역 {len(items)}개)')


if __name__ == '__main__':
    main()
