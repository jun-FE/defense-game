#!/bin/bash
# ArtSource/Main_depth 의 투명 PNG 원본(01~16)을 Assets/Art/Main/Depth 로 가져온다.
# 배치는 Assets/Art/Main/Depth/main_layout.json (레이어 이름 = 아래 오른쪽 이름).
set -e
cd "$(dirname "$0")/../.."
python3 Tools/Art/import_layers.py ArtSource/Main_depth Assets/Art/Main/Depth \
  "01_최후경_밤하늘_숲.png=bg_night_forest" "02_창문프레임_외부구조.png=wall_frame_a" "03_후경_건물구조.png=wall_frame_b" \
  "04_후경_선반_벽면.png=shelves" "05_행잉장식.png=hanging_decor" "06_벽면오브젝트.png=wall_decor" \
  "07_조명레이어.png=lamps_set" "08_카운터_뒷소품.png=shelves_decorated" "09_카운터_본체.png=workbench" \
  "10_타워_소품.png=toys_shelf" "11_주인공.png=ian" "12_고양이인형_도하.png=doha" \
  "13_전경_블러오브젝트.png=fg_floor" "14_전경_식물.png=fg_plants_left" "15_전경_좌측.png=fg_books_left" \
  "16_전경_우측.png=fg_crystal_right"
