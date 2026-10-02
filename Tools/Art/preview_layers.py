"""레이어 배치 JSON으로 배경을 1920x1080 미리보기 이미지로 합성한다(Unity 없이 배치 확인용).
사용: python3 Tools/Art/preview_layers.py <출력.png> [--lobby] [--ui] [--brightness=1.0]
  기본은 메인(수선소), --lobby 는 로비(첫 화면, 메뉴 포함), --ui 는 메인의 임시 UI 자리 표시"""
import json, sys
import numpy as np
from PIL import Image, ImageDraw


def hex_rgb(h):
    h = h.lstrip('#')
    return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)], float) / 255.0


def glow(size, color, strength=0.85):
    r = np.linspace(-1, 1, size)
    d = np.sqrt(r[None, :] ** 2 + r[:, None] ** 2)
    a = np.clip(1 - d, 0, 1) ** 2.2 * strength
    return np.dstack([a * c for c in color])


def add_lights(canvas, L, brightness=1.0):
    """Unity의 LayeredBackground와 같은 방식: 레이어에 ambient×shade를 곱했고, 여기서 조명 빛을 더하고 가장자리를 어둡게."""
    img = np.asarray(canvas.convert('RGB'), float) / 255.0
    H, W = img.shape[:2]
    for light in L.get('lights', []):
        s = int(light['size'])
        # LayeredBackground.ApplyColors와 같은 공식: 배경이 밝을수록 빛 번짐은 약하게
        k = 0.85 * (1.2 + (0.7 - 1.2) * min(1, max(0, (brightness - 0.6) / 0.8)))
        g = glow(s, hex_rgb(light.get('color', '#ffb257')), strength=k)
        x0, y0 = int(light['x'] - s / 2), int(light['y'] - s / 2)
        xa, ya, xb, yb = max(0, x0), max(0, y0), min(W, x0 + s), min(H, y0 + s)
        img[ya:yb, xa:xb] += g[ya - y0:yb - y0, xa - x0:xb - x0]
    v = L.get('vignette', 0)
    if v:
        yy, xx = np.mgrid[0:H, 0:W]
        d = np.sqrt(((xx - W / 2) / (W / 2)) ** 2 + ((yy - H / 2) / (H / 2)) ** 2) / 1.414
        img *= (1 - v * np.clip((d - 0.35) / 0.65, 0, 1) ** 1.6)[..., None]
    return Image.fromarray((np.clip(img, 0, 1) * 255).astype('uint8'))

def compose(layout_path='Assets/Art/Main/Depth/main_layout.json', art='Assets/Art/Main/Depth', ui=False, brightness=1.0):
    L = json.load(open(layout_path))
    W, H = L['canvasWidth'], L['canvasHeight']
    bg = L.get('background', '#000000')
    canvas = Image.new('RGBA', (W, H), bg)
    for layer in L['layers']:
        im = Image.open(f"{art}/{layer['name']}.png")
        scale = layer['width'] / im.width
        im = im.resize((round(im.width * scale), round(im.height * scale)), Image.LANCZOS)
        if layer.get('flipX'): im = im.transpose(Image.FLIP_LEFT_RIGHT)
        tint = np.clip(hex_rgb(L.get('ambient', '#ffffff')) * layer.get('shade', 1.0) * brightness, 0, 1)
        if (tint < 1).any():
            a = np.asarray(im, float)
            a[..., :3] *= tint
            im = Image.fromarray(a.astype('uint8'), 'RGBA')
        canvas.alpha_composite(im, (round(layer['x'] - im.width / 2), round(layer['y'] - im.height / 2)))
    if L.get('lights') or L.get('vignette'):
        canvas = add_lights(canvas, L, brightness).convert('RGBA')
    for item in L.get('menu', []):
        im = Image.open(f"{art}/{item['name']}.png")
        im = im.resize((round(item['width']), round(item['height'])), Image.LANCZOS)
        canvas.alpha_composite(im, (round(item['x'] - im.width / 2), round(item['y'] - im.height / 2)))
    if ui:
        d = ImageDraw.Draw(canvas, 'RGBA')
        d.rectangle([1520, 100, 1890, 900], fill=(20, 18, 40, 170), outline=(200, 170, 255, 255))   # 의뢰함
        d.rectangle([1080, 190, 1500, 900], fill=(230, 215, 180, 170))                               # 편지 카드
        d.rectangle([0, 980, 1920, 1080], fill=(20, 18, 40, 170))                                    # 탭 바
        d.rectangle([40, 30, 600, 150], outline=(255, 220, 150, 255))                                # 로고
        d.rectangle([60, 860, 640, 950], fill=(20, 18, 40, 170))                                     # 도하 말풍선
    return canvas

if __name__ == '__main__':
    if '--lobby' in sys.argv:
        image = compose('Assets/Art/Lobby/lobby_layout.json', 'Assets/Art/Lobby')
    else:
        b = [float(a.split('=')[1]) for a in sys.argv if a.startswith('--brightness=')]
        image = compose(ui='--ui' in sys.argv, brightness=b[0] if b else 1.0)
    image.convert('RGB').save(sys.argv[1], quality=90)
