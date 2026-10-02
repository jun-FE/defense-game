# 메인(수선소) 화면 UI 이미지

`reference/lobby_mockup.jpg`(메인 화면 시안)를 실제 화면으로 만들기 위한 원본 이미지를 이 폴더에 넣어 주세요.
개발자가 여기서 정리해 `Assets/Art/Main/UI`로 가져갑니다(Unity가 원본을 직접 읽지 않도록 Assets 밖에 둡니다).
배경 레이어(뎁스)는 `ArtSource/Main_depth`, 첫 화면(로비)은 `ArtSource/Lobby_depth`에 넣습니다.

## 넣는 방법
- **GitHub 웹**: 저장소에서 `ArtSource/Main_UI` 폴더로 들어가 **Add file → Upload files**에 파일을 끌어다 놓고 **Commit changes**.
- 또는 채팅에 첨부해 주시면 개발자가 이 폴더에 넣습니다.

## 파일 규칙
- **PNG, 배경 투명**(배경 이미지만 JPG/PNG 상관없음).
- **해상도**: 화면 기준 1920×1080. 가능하면 2배(3840×2160 기준 크기)로 주시면 선명합니다.
- **한 요소 = 한 파일.** 시안처럼 합쳐진 한 장은 잘라 쓰기 어렵습니다. 글자(의뢰 제목, 버튼 문구 등)는 넣지 말고 비워 주세요. 글자는 게임에서 넣습니다.
- **늘어나는 프레임**(패널, 카드, 버튼, 말풍선)은 모서리 장식이 늘어나지 않도록 네 모서리를 여유 있게 그려 주세요(9-slice).
- **상태가 있는 요소**는 상태별로 따로: `_normal`, `_selected`, `_pressed`, `_locked`.
- 파일 이름은 아래 표의 이름을 써 주세요(영문 소문자·밑줄).

## 필요한 이미지

우선순위 1이 있어야 화면을 조립할 수 있고, 2·3은 없으면 임시 도형으로 먼저 만듭니다.

| 우선 | 파일 이름 | 내용 | 비고 |
| --- | --- | --- | --- |
| 1 | `bg_workshop.png` | 수선소 내부 배경 (인물 없이) | 1920×1080 이상. 오른쪽 의뢰함 자리는 너무 복잡하지 않게 |
| 1 | `char_ian.png` | 작업대에 기댄 이안 | 투명 PNG. 숨쉬기·눈 깜빡임을 넣으려면 눈 감은 버전 `char_ian_blink.png`도 |
| 1 | `char_doha.png` | 작업대 위 도하 | 투명 PNG. `char_doha_blink.png` 있으면 좋음 |
| 1 | `logo.png` | "악몽수선소" 로고 + 초승달·별 장식 | 부제 "잊힌 꿈도, 다시 꿰맬 수 있으니까."는 글자로 넣어도 되고 이미지에 포함해도 됨 |
| 1 | `panel_quest_board.png` | 오른쪽 의뢰함 패널 프레임 | 9-slice |
| 1 | `card_quest_normal.png`, `card_quest_selected.png`, `card_quest_locked.png` | 의뢰 목록 카드 (일반 / 선택 / 잠김) | 9-slice. 선택은 금색 테두리 |
| 1 | `paper_letter.png` | 가운데 양피지 편지 카드 | 9-slice. 클립·찢긴 가장자리 포함 |
| 1 | `btn_start_normal.png`, `btn_start_pressed.png` | "의뢰 시작하기" 버튼 | 글자 없이. 깃펜 장식은 `deco_quill.png`로 따로 |
| 1 | `tabbar.png` | 하단 탭 바 바탕 | 가로로 늘어나도 되게 |
| 1 | `tab_selected.png` | 선택된 탭 뒤 장식(보라 빛 테두리) | |
| 1 | `icon_tab_workshop.png`, `icon_tab_quests.png`, `icon_tab_codex.png`, `icon_tab_bag.png`, `icon_tab_skills.png`, `icon_tab_map.png` | 하단 탭 아이콘 6개 (수선소, 의뢰함, 도감, 가방, 기술, 지도) | 잠긴 탭은 게임에서 어둡게 처리 |
| 2 | `bar_currency.png` | 오른쪽 위 재화 바 프레임 | 9-slice |
| 2 | `icon_currency_star.png`, `icon_currency_crystal.png` | 재화 아이콘 (별 / 보라 결정) | 각각 어떤 재화인지 기획 확인 필요 |
| 2 | `icon_mail.png`, `icon_settings.png`, `icon_fullscreen.png` | 오른쪽 위 아이콘 | |
| 2 | `badge_new.png` | 빨간 알림 점 | 우편·탭 공통 |
| 2 | `tab_btn_normal.png`, `tab_btn_selected.png` | 의뢰함의 전체/진행 중/완료 탭 버튼 | 글자 없이 |
| 2 | `bubble_doha.png`, `portrait_doha.png` | 왼쪽 아래 도하 말풍선 프레임, 도하 초상화 | 말풍선은 9-slice, 화살표는 `icon_arrow_next.png` |
| 2 | `thumb_quest_*.png` | 의뢰 썸네일 (예: `thumb_quest_rabbit`, `thumb_quest_lamp`, `thumb_quest_umbrella`, `thumb_quest_letter`, `thumb_quest_gramophone`) | 정사각형 |
| 2 | `photo_quest_*.png` | 편지 카드 안 의뢰 사진 (예: `photo_quest_rabbit`) | |
| 2 | `slot_reward.png`, `icon_reward_*.png` | 보상 칸 프레임, 보상 아이콘 (달, 실패, 쪽지 등) | |
| 3 | `sign_board.png` | 왼쪽 나무 표지판 (문구 칸 4줄) | 글자 없이 |
| 3 | `deco_*.png` | 흔들리는 등불, 반짝이 등 움직일 장식 | 움직임을 넣고 싶은 것만 따로 |
| 3 | 폰트 파일 (`.ttf`/`.otf`) | 본문용 한글 폰트, 제목용 폰트 | 상업 이용 가능한 라이선스인지 확인 |
