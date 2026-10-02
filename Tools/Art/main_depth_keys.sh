#!/bin/bash
# ArtSource/Main_depth 의 카카오톡 원본(검은 배경 JPEG)을 투명 PNG 레이어로 바꿔 Assets/Art/Main/Depth 에 넣는다.
# 원본 PNG(투명 배경)가 오면 이 단계 없이 바로 Assets/Art/Main/Depth 에 같은 이름으로 넣으면 된다.
set -e
cd "$(dirname "$0")/../.."
SRC=ArtSource/Main_depth
p() { echo "KakaoTalk_Photo_2026-10-02-$1.jpeg=$2"; }
python3 Tools/Art/key_black.py "$SRC" Assets/Art/Main/Depth \
  "$(p '14-35-49 001' bg_night_forest)" "$(p '14-35-49 002' wall_frame_a)" "$(p '14-35-50 003' wall_frame_b)" \
  "$(p '14-35-50 004' shelves)" "$(p '14-35-50 005' hanging_decor)" "$(p '14-35-50 006' wall_decor)" \
  "$(p '14-35-51 007' lamps_set)" "$(p '14-35-51 008' shelves_decorated)" "$(p '14-35-51 009' workbench)" \
  "$(p '14-35-51 010' toys_shelf)" "$(p '14-35-51 011' ian)" "$(p '14-35-51 012' doha)" \
  "$(p '14-35-52 013' fg_floor)" "$(p '14-35-52 014' fg_plants_left)" "$(p '14-35-52 015' fg_books_left)" \
  "$(p '14-35-52 016' fg_crystal_right)" \
  --solid=workbench,ian,doha,toys_shelf,shelves,fg_books_left,fg_crystal_right
