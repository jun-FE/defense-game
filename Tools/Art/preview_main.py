"""main_layout.json 으로 메인 배경을 1920x1080 미리보기 이미지로 합성한다(Unity 없이 배치 확인용).
사용: python3 Tools/Art/preview_main.py <출력.png> [--ui]   (--ui: 임시 UI 자리 표시)"""
import json, sys
from PIL import Image, ImageDraw

def compose(layout_path='Assets/Art/Main/Depth/main_layout.json', art='Assets/Art/Main/Depth', ui=False):
    L = json.load(open(layout_path))
    W, H = L['canvasWidth'], L['canvasHeight']
    bg = L.get('background', '#000000')
    canvas = Image.new('RGBA', (W, H), bg)
    for layer in L['layers']:
        im = Image.open(f"{art}/{layer['name']}.png")
        scale = layer['width'] / im.width
        im = im.resize((round(im.width * scale), round(im.height * scale)), Image.LANCZOS)
        if layer.get('flipX'): im = im.transpose(Image.FLIP_LEFT_RIGHT)
        canvas.alpha_composite(im, (round(layer['x'] - im.width / 2), round(layer['y'] - im.height / 2)))
    if ui:
        d = ImageDraw.Draw(canvas, 'RGBA')
        d.rectangle([1520, 100, 1890, 900], fill=(20, 18, 40, 170), outline=(200, 170, 255, 255))   # 의뢰함
        d.rectangle([1080, 190, 1500, 900], fill=(230, 215, 180, 170))                               # 편지 카드
        d.rectangle([0, 980, 1920, 1080], fill=(20, 18, 40, 170))                                    # 탭 바
        d.rectangle([40, 30, 600, 150], outline=(255, 220, 150, 255))                                # 로고
        d.rectangle([60, 860, 640, 950], fill=(20, 18, 40, 170))                                     # 도하 말풍선
    return canvas

if __name__ == '__main__':
    compose(ui='--ui' in sys.argv).convert('RGB').save(sys.argv[1], quality=90)
