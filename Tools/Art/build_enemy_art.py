"""적 그림 만들기(Unity 없이): ArtSource/Enemies/<한글 폴더>/{base,walk}.png → Assets/Art/Enemies/<영문>/<영문>_{0,1}.png
사용: python3 Tools/Art/build_enemy_art.py
- 타워와 같은 규칙(build_tower_art.py): 초록 배경 제거, 걷기 그림을 기본 그림 몸통에 맞춰 정렬, 256px 캔버스(발바닥 = 아래 6%).
- 그림은 오른쪽으로 걷는 모습. 게임(EnemyView)이 왼쪽으로 갈 때 좌우를 뒤집는다."""
import os
import sys
import types

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
ENEMIES = {'tangle': ('실뭉치', 'Tangle')}

src = open(os.path.join(ROOT, 'Tools/Art/build_tower_art.py'), encoding='utf-8').read()
lib = types.ModuleType('tower_art')
lib.__dict__['__file__'] = os.path.join(ROOT, 'Tools/Art/build_tower_art.py')
exec(src[:src.index('def build(name):')], lib.__dict__)
sew = lib.lib
WORK = lib.WORK

for name, (folder, out_name) in ENEMIES.items():
    src_dir = os.path.join(ROOT, 'ArtSource/Enemies', folder)
    out_dir = os.path.join(ROOT, 'Assets/Art/Enemies', out_name)
    os.makedirs(out_dir, exist_ok=True)
    base = sew.key(os.path.join(src_dir, 'base.png'))
    bx, by = lib.anchor(base)
    dx, dy = WORK / 2 - bx, WORK * 0.94 - by
    sew.save(lib.soften_edges(sew.shift(base, dx, dy)), os.path.join(out_dir, f'{name}_0.png'))
    walk_path = os.path.join(src_dir, 'walk.png')
    if os.path.exists(walk_path):
        walk = sew.key(walk_path)
        # 걷기 그림은 다리가 움직이므로 아래쪽이 아니라 몸통 전체로 맞춘다
        import numpy as np
        from scipy import signal
        k = 8
        a = base[::k, ::k, 3] > 0.6
        b = walk[::k, ::k, 3] > 0.6
        corr = signal.fftconvolve(a - a.mean(), (b - b.mean())[::-1, ::-1], mode='same')
        cy, cx = np.unravel_index(np.argmax(corr), corr.shape)
        mx, my = (cx - a.shape[1] // 2) * k, (cy - a.shape[0] // 2) * k
        sew.save(lib.soften_edges(sew.shift(walk, dx + mx, dy + my)), os.path.join(out_dir, f'{name}_1.png'))
    print(name, '완료')
