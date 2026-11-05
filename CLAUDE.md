# 악몽수선소 — 작업 인수인계 (Claude용)

새 세션은 이 파일을 먼저 읽는다. 기능·조작·데이터 설명은 `README.md`에 있다. 여기에는 **대화로 정해진 결정, 작업 방식, 진행 중인 일**만 적는다. 일이 진행되면 이 파일도 고쳐서 같이 커밋한다.

## 지금 상태와 다음 할 일 (최근 갱신 2026-11-06)
- 최근 작업: 프롤로그를 `ArtSource/Prologue/프롤로그_화면구성.xlsx` 기준으로 다시 만듦(꿈속 방 탐색 → 전투 → 문 → 깨어남 → 수선소), 모든 전투 도구 슬롯, 탐색 채굴 타격감. 자세한 건 맨 아래 날짜별 기록.
- **사용자 확인 대기**(Unity에서 아직 못 봄):
  1. 새로하기 → 탐색 → 전투 화면이 "타워 목록에 빈 항목" 오류 없이 열리는지(2026-11-05 두 번째 수정: SetupStage를 LockTowers보다 먼저).
  2. 탐색 걷기가 순간이동처럼 보이지 않는지(0.4초/칸, 화면 부드럽게 따라감). 여전하면 이안 그림 로드 실패(네모로 보임) 여부부터 확인.
  3. 채굴 손맛(어둠 2·작은 결정 3·큰 결정 4번 두드림, `ExploreUI.DigHits`/`DigInterval`). 일반 탐색 전체에 적용됨 — 너무 느리면 줄이기.
  4. 문·깨어남 장면(눈꺼풀 연출, chain·blanket 소리), 대사창·툴팁 모양.
- Unity CLI 연결됨(Mac). 에디터 확인이 필요한 일은 Mac에서 도는 세션(또는 Mac 폴더에 연결된 세션)에서 `unity status`부터. 아래 "Unity CLI 연결" 기록 참고.
- 작업 규칙: main에 바로 커밋·푸시, 커밋 작성자 `jun-FE <craftttime@gmail.com>`, 메시지 끝에 Co-Authored-By 줄. 커밋·문서에 모델 이름 금지. 키·토큰은 채팅에 붙이지 않게.
- Artlist 크레딧 약 5,600(2026-11-05 기준, 11일 충전). 생성 전 비용 먼저 알리고 승인받기.

## 사용자와 소통
- 사용자는 한국어로 대화한다. 답도 한국어, 쉬운 말로(개발 용어는 풀어서). Mac 사용, 목표는 Steam PC 출시.
- 그림·효과음은 사용자와 친구가 맡고(친구는 ChatGPT 사용), 코드·연결·변환은 Claude가 맡는다.
- 사용자가 올린 원본 그림은 GitHub 웹에서 `ArtSource/<분류>/<폴더>`로 올라온다(`git pull`로 받음). 폴더를 엉뚱한 곳에 올리면 `git mv`로 옮긴다.
- 크레딧·돈이 드는 생성은 항상 비용을 먼저 보여 주고 승인받은 뒤 실행한다.

## 게임 개요(확정된 것)
- Unity 6.3, 2D, UI는 IMGUI(`UIKit`, 1080p 가상 좌표). 흐름: 로비 → 메인(수선소, 의뢰함) → 이야기 → **탐색** → **전투** → 결과.
- **탐색형 디펜스**: 전부 암흑인 칸 맵을 이안이 목표(꿈의 중심)에서 출발해 한 칸씩 밝힌다. 결정 칸을 밝히면 재화. **밝힌 칸들의 최단 경로**가 몬스터 경로가 된다(같은 거리면 위·오른쪽·아래·왼쪽 순). 타워는 밝힌 칸과 맞닿은 암흑 칸에만 짓는다. 탐색 제한은 아직 없음(나중에 정함).
- 큰 결정과 작은 결정은 둘 다 1×1 칸, 그림만 다르다.
- 실패 조건은 꿈의 중심 HP 0 하나뿐. 악몽 침식도 100은 실패가 아니라 가장 강한 몬스터 단계.
- 용어: "공격속도"(공격 간격, 초).
- 맵 에디터(개발자용, 로비에서 F12): 맵 크기 선택, 휠 확대, 도구는 암흑·막힌 암흑·작은 결정·큰 결정·출현·목표뿐(길 그리기 없음).
- 전투 HUD: 72% 크기. 왼쪽 아래 **도구 슬롯 줄**(고르면 청록 설치 구역 + 녹색 미리보기, 2026-11-03부터 모든 전투), 그 위 미니맵. 드래그로 화면 이동, 휠 확대·축소. (예전 건설 고리는 없앰)
- 타워(2026-10-29 확정): **수선소 물품 6종** = 스탠드 조명(원거리 단일)·가위(근거리 여러 적, 3단계 연속 가위질)·재봉틀·줄자(감속)·바늘꽂이(근거리 표식 = 방어 감소)·수선표 활(원거리 표식 = 받는 피해 증가). 병정인형·스노우볼은 목록에서 빠짐(정의·그림은 남김).

## 논의 중(사용자 결정 대기)
- 스토리 비유 제안: 꿈은 천, 악몽은 엉킨 실, 이안이 등불로 꿰맨 "빛길"로 악몽이 온다, 재화는 "꿈실"(큰 결정은 "실마리"), 몽결정은 영구 재화로. → 사용자 답 없음.
- 프로토타입 목록(사용자 메모): 달빛 정원 맵 테마(경로·일반·설치 가능 타일, 시작·끝점, 배경), 몬스터(잡몹·탱커·빠른 적·평범한 적, 정예=패시브 1, 보스=액티브 1+패시브 1), 타격 이펙트·SFX·VFX, 디버프(방어 감소·이동 감소), 탐색 중 빛 지점 발견, 퀘스트 볼륨, 1순위 퀘스트 2개.
- 진행 중 의뢰 칸에 마지막 완료 의뢰를 보여 줄지.

## 그림 생성: Artlist (MCP 커넥터로 연결됨)
- 요금제 AI Starter(월 16,500크레딧, 매월 11일 충전). MCP 사용은 연간 무제한과 상관없이 크레딧 차감.
- 그림체 기준: 이안·도하·메인 화면 소품·병정인형·스노우볼(부드러운 칠 그림). 드림캐처는 픽셀 아트라 기준에서 제외. 눈·크리스마스 장식은 빼기.
- 기준 그림 업로드: `upload_image`(mimeType) → 받은 URL로 `curl -X PUT --data-binary` → `confirm_upload`. 이 환경에서 동작 확인됨.
- **결과 다운로드**에는 네트워크 허용이 필요: `cms-toolkit-artifacts.artlist.io`, `ai-toolkit-generations.imgix.net`, `mcp.artlist.io`. 막혀 있으면 사용자에게 환경 설정(네트워크 액세스 → 허용 도메인) 추가를 안내한다.
- 배경 제거 모델(Pixelcut Background Removal)이 있다. 효과음은 생성 불가, `search_sfx`로 기존 효과음 검색만(다운로드는 사용자가 링크로).

### 진행 중: 메인 화면 UI 다시 그리기
- 사용자 요청: 메인 화면(배경은 좋음)의 UI가 도형으로 대충 만든 것이라 디자인을 새로 해 달라.
- 2026-10-11: 현재 화면 스크린샷을 기준으로 Nano Banana 2.1(1K, 16:9)로 **전체 화면 시안 4장** 생성(300크레딧). Artlist 생성 기록에서 찾을 수 있다(`list_generations`, 모델 Nano Banana 2.1, 4장 묶음).
  - 프롬프트 요지: 배경·인물·배치는 그대로, UI만 빈티지 꿈 수선 공방 느낌으로. 낡은 양피지·리넨 패널과 손바느질 테두리, 황동·호두나무 틀, 바늘·실·초승달 장식, 보랏빛 꿈실 광채, 의뢰 카드는 밀랍 봉인 편지, 하단 탭 6개는 천 이름표 모양, 크리스마스·눈 없음.
- 2026-10-11 완료: 사용자 선택 **3번 배치 + 4번 타이틀 + 1번 하단 탭**. 시안은 `ArtSource/Main_UI/reference/시안1~4.png`.
  - 글자 없는 조각 시트 4장(패널·컨트롤·아이콘·로고, Nano Banana 2.1 2K, 500크레딧)을 초록 배경으로 생성 → `ArtSource/Main_UI/generated/` → `Tools/Art/build_workshop_ui.py`로 `Assets/Art/Main/UI/ws_*.png` 41개 + `ui_borders.json`(bx/by).
  - `MainUI.cs`가 ws_ 조각을 쓰도록 바꿈(밝은 양피지 위 글자는 잉크색). 전투·탐색은 예전 조각 유지.
  - Artlist 남은 크레딧 약 15,700. 이 환경은 Artlist 이미지 서버 허용됨(다운로드 가능).
- 다음 후보: Unity에서 실제 화면 확인 후 크기·여백 조정, 같은 방식으로 로비·전투 HUD·팝업 UI도 공방 디자인으로 통일.

### 2026-10-11: 전투 HUD 공방 디자인 + 재봉틀 타워
- 전투 HUD도 ws_ 조각으로(UISkin이 bx/by 테두리, 데님 버튼 `Button`, 탭 `Tab` 지원). 탐색 화면 버튼도 데님으로 바뀜.
- 재봉틀(TW_SEWING, 실타래 슬롯): 사용자 제공 1단계 픽셀 그림 + Artlist로 2·3단계, 단계별 대기·공격 프레임(8장, 600크레딧). 화려한 연출은 BattleFx(가산 머티리얼 BattleView.glowMaterial).
- Artlist 남은 크레딧 약 15,100.

### 2026-10-11: 칸 맵 테마(달빛 정원)
- 사용자 결정: 칠 그림, 바닥 위에서 내려다봄 + 오브젝트 살짝 앞에서, 첫 테마 달빛 정원.
- `MapTheme`(Resources/Themes/MoonGarden): 바닥 타일, 어둠은 코드 안개, 경로는 빛나는 꿈실, 벽 4종(가장자리만 그림), 결정·출현·꿈의 중심 오브젝트, 바느질 점선 설치 표시. 전투(MapView)와 탐색(ExploreUI) 모두 적용. 375크레딧.
- 남은 일 후보: 맵 에디터 미리보기도 테마로, 다른 테마(테마별 폴더).

### 2026-10-11: 탐색 화면 이안 + 밝히기 연출
- 이안 전신 시트(Nano Banana 2.1 2K, 125크레딧): 6칸×2줄로 나옴(앞·뒤·뒤·왼·왼·오) → `build_ian_sprites.py`가 앞/뒤(3번째)/왼(4번째)/오(6번째)만 사용.
- ExploreUI: 이안 이동 연출·랜턴 빛, 밝히기·결정·벽 부딪힘 효과(IMGUI 입자, 테마 fx_glow/spark/ring), 안개 서서히 걷힘, 화면 흔들림. Unity에서 확인 필요(효과 크기·세기 조정 후보).

### 2026-10-11: 달빛 정원 2단계(사용자 샘플에 맞춤) + 연출 다듬기
- 사용자 샘플(`ArtSource/Maps/MoonGarden/reference/sample_moon_garden.png`)이 기준. 밝힌 칸 돌길, 안 연 칸 잔디, 막힌 암흑 꽃밭, 꽃덤불·경계 띠·정원 소품, 맵 바깥 어두운 꽃밭(625크레딧, 샘플을 참고 그림으로 생성).
- 사용자 피드백 반영: 밝힐 때 화면 흔들림 제거 → 어둠이 먼지처럼 흩어짐, 금빛 고리를 실이 휘감기는 불규칙한 모양으로(재봉틀 공격도 같은 그림 + 회전), 화면 축소 시 검은 바탕 대신 꽃밭.
- 옛 벽 그림(가시덤불 등)·새싹·ground.png는 Resources에서 뺐다(원본은 ArtSource에 남음).

### 2026-10-11: 맵 바깥·어둠 다듬기(사용자 스크린샷 피드백)
- 버그 원인: ① 확대 배율 IMGUI에서 `GUIUtility.RotateAroundPivot`이 그림을 엉뚱한 곳에 그림(세로 경계 띠가 맵 밖에 떠 보임) → 탐색 화면은 회전 금지, 미리 돌린 그림·뒤집기 사용. ② 바닥 텍스처가 Clamp로 가져와져 맵 바깥이 줄무늬 → `MapTheme.Load`에서 Repeat로 강제(새 스크립트와 그림이 한 번에 들어오면 가져오기 설정이 늦게 적용됨).
- 탐색 어둠 0.8 → 0.94, 맵 바깥에도 같은 안개. 맵은 화면 전체에 그리고 테두리 레이어(비네트 + 위·아래 그라데이션)로 HUD 뒤를 가라앉힘. 전투도 같은 레이어 + 맵 가장자리 그라데이션.
- 다음 후보(사용자 결정): 화면 테두리를 그림 액자(수틀·나무 틀 등)로 꾸밀지.

### 2026-10-29: 수선소 물품 타워 6종
- 사용자 메모(스탠드 조명·가위·줄자·근거리 표식·원거리 표식(활))를 해석해 확정: 바늘꽂이 = 근거리 방어 감소, 수선표 활 = 원거리 받는 피해 증가. 사용자가 "이대로 진행"·2K 승인.
- 그림: 재봉틀 L1·L3를 기준 그림으로 Artlist 2K 45장(1단계 5 → 2·3단계 10 → 대기·공격 30). 5,625크레딧, 실패한 10장 묶음은 차감 없이 다시 생성. 남은 크레딧 약 8,350.
- 코어: `TowerLevelDef.HitCount`(여러 적), `ArmorShred`·`Vulnerability`·`MarkSec`(표식), `HitResult.HitIndex/HitTotal/Pinned/Tagged`. 검산 테스트 추가(가위 여러 적, 표식 걸림·풀림).
- 밸런스(봇은 첫 두 타워만 씀): 스탠드 조명만 90%, 가위 섞기 53%. 표식·감속을 쓰는 봇은 아직 없음 → 사람 플레이로 조정 필요.
- 새 세션 컨테이너에는 게임 저장소가 없을 수 있다: `git clone https://github.com/jun-FE/defense-game /home/user/defense-game`(공개 저장소, push도 됨). 파이썬은 `python3.13`(PIL·numpy·scipy), C# 검사는 `apt-get install mono-mcs`.

### 2026-10-29: 전투 음악·효과음·임시 몬스터
- 전투 배경음악: 사용자 제공 Endless Drift(m4a → ogg). `MusicPlayer.Play`.
- 효과음: Artlist 요금제(AI Suite)는 **효과음 다운로드 불가**(subscription_required). 사용자가 후보 전부 1번 선택 → README 표에 기록, 지금은 `Tools/Audio/make_temp_sfx.py` 합성 임시음. 같은 이름으로 바꾸면 됨.
- 임시 몬스터 "실뭉치 악몽"(Artlist 2장, 250크레딧). 적 3종 모두 같은 그림, 색·크기·걸음 속도로 구분. 빌더 20.
- Artlist 남은 크레딧 약 8,100.

### 2026-10-29: 첫 플레이 피드백 반영
- 스탠드 조명 이중 연출(그림 속 고정 방향 빛 + 코드 빛줄기) → 그림 속 빛 제거. 길 위 소품·덤불·경계 띠 정리(소품은 꽃밭 안쪽만). 체력바 다듬기(맞기 전 숨김). 경로 표시는 준비 시간에만.
- 이안을 픽셀 그림체 4방향×4장으로 다시 생성(125크레딧), 키 약 1칸, 0.26초 일정 속도로 이어 걷기. 남은 크레딧 약 7,975.

### 2026-10-29: 새로하기 프롤로그 틀
- 새로하기 → `SceneFlow.StartPrologue()` → Story 씬 → 끝나면 Main(`storyGoesToMain`). 스테이지 스토리는 그대로 끝나면 스테이지.
- 프롤로그는 글자 대본 `Assets/Resources/Stories/prologue.txt`를 `StoryScript.Parse`로 읽음(장면 카드·배경·색·초상화·표정 문법). 임시 대본, 사용자가 진짜 스토리·디자인을 나중에 줌. 아트리스트 안 씀.
- StoryLine에 선택 칸 `sceneTitle/sceneSubtitle/background/portrait` 추가. StoryPlayer: 페이드 인·아웃, 장면 카드, 배경 겹쳐 바꾸기, 좌(이안)/우 초상화, 그림 없으면 자리 표시. Unity에서 확인 필요.

### 2026-10-29: 도감(수선 기록부) + 악몽 3종 그림
- Artlist 6장(750크레딧): 펼친 책, 도감 조각 시트(칸·책갈피·액자·막대·도장·아이콘 8개), 실밥 그림자·누더기 곰(기본+걷기). 남은 크레딧 약 7,225.
- 적 이름 변경: 병정→실뭉치 악몽, 빠른 그림자→실밥 그림자, 무거운 봉제→누더기 곰. 빌더 22(적 그림·크기·이름 갱신).
- 도감: `CodexPanel`(메인 도감 탭), 글은 `CodexData`, 기록은 `CodexProgress`(전투 이벤트로 만남·처치 기록). 미리보기 `Tools/Art/preview_codex.py`. Unity에서 확인 필요.

### 2026-10-29: 기술(꿈 수틀) 거미줄 기술 나무
- Artlist 2장(250크레딧): 나무 수틀, 매듭 조각 시트. 남은 크레딧 약 6,975.
- `Mastery`(매듭 25개: 가운데 + 6줄 × 안쪽·가운데·바깥 + 살 사이 6, 실마리 = 3 + 클리어당 2), `MasteryPanel`(메인 기술 탭). 전투 시작 시 `Mastery.Apply`. Unity에서 확인 필요.

### 2026-11-02: 프롤로그(기획서) + 첫 수선 튜토리얼
- 사용자 기획서 `ArtSource/Story/프롤로그_스토리.docx`(이안 과거·반복 꿈·해진/도윤/유나·달빛정원). 1~4장 → `prologue.txt`, 5장 → 튜토리얼 판. 이안은 현실에서 휠체어, 꿈에서는 걸음. 도하는 강의하지 않고 상황만 알림(조작은 화면 안내).
- 새로하기 → 프롤로그 → `SceneFlow.StartTutorial`(Tutorial.Active, 스테이지 0 탐색) → `BattleTutorial.SetupStage`로 웨이브 교체(이길 수 없음) → `FinishTutorial` → `tutorial_end.txt` → 메인.
- 스토리 대본에 `[안내]`·`[편지]` 추가. 컴파일 검사 수정: 에디터 설정이 mcs 내부 오류(8개 튜플)로 실제로는 검사되지 않고 "통과"로 나오던 것 → 튜플을 클래스로, compile.sh가 내부 오류도 실패로 봄. Unity에서 확인 필요.

### 2026-11-02: 도구별 강화(속성 분기) · 도구 해금 · 도감 보상
- 사용자 요청: 강화가 단조로움 → 도구마다 기본 손질(늘 적용) + 속성 분기 3개(각성→심화), 한 도구엔 고른 속성 하나만. 모든 도구에 불·독 넣지 말 것.
- 전투 코어: TowerLevelDef에 Element·DotDps/DotSec·StunChance/StunSec·ChainCount/Range/Falloff·SplashRadius/Pct, 검산 "속성 강화".
- 해금: 시작은 스탠드만, 클리어로 가위·재봉틀(1), 줄자·바늘꽂이(2), 수선표 활(3). 실마리 = 2 + 도감 기록 보상(도구 2·악몽 1). 해금 방식은 내가 정한 기본값 → 사용자 확인 필요.
- 건설 고리 버튼 104→136, 실제 도구 그림. Unity에서 확인 필요.

### 2026-11-02: 글꼴·기술 화면 그림체 맞추기
- 글꼴: 고운바탕(OFL) Regular/Bold를 Resources/Fonts에, UIKit.FontFor/StyleFor로 모든 GUIStyle에 적용. 미리보기 스크립트도 같은 글꼴.
- Artlist 2장(250크레딧, 메인 아이콘·컨트롤 시트 참고): 매듭 틀(가운데 빈 리넨 메달), 아이콘 25개(mi_*, el_*). 남은 크레딧 약 6,725. 도감 수치 아이콘도 교체.
- 참고: 사용자가 본 이전 스크린샷은 내 미리보기 그림(리눅스 기본 글꼴)이었음 → 이제 미리보기도 게임 글꼴로 그린다.

## 작업 방식(검증)
- Unity를 실행할 수 없으므로 아래로 검증하고, 사용자에게 "Unity에서 직접 확인 필요"를 분명히 말한다.
  - 전체 스크립트 컴파일 검사: `bash Tools/StubCompile/compile.sh` (Unity API 빈 껍데기 `UnityStubs.cs` 사용, 없는 API는 스텁에 추가)
  - 전투 코어 검산 테스트: `bash Tools/CoreTests/run.sh`
  - 밸런스 모의 플레이: `bash Tools/CoreTests/balance.sh N [garden] [mix]`
  - 배경·조명 미리보기 이미지: `python3 Tools/Art/preview_layers.py out.png`
- 타워 프레임 변환: `python3 Tools/Art/import_tower_frames.py "ArtSource/Towers/<폴더>" <영문이름> --ui-icon=mock_tower_<이름>` (구조 `level_N/방향/frame_XX.png`).
- 씬·데이터는 `Assets/Scripts/Editor/DemoSceneBuilder.cs`가 만든다. 씬 구성을 바꾸면 `BuildVersion`을 올린다.
- 커밋: `git -c user.name="jun-FE" -c user.email="craftttime@gmail.com" commit`, `main`에 바로 push(실패 시 재시도).

### 2026-11-03: 프롤로그 다시 만들기(프롤로그_화면구성.xlsx) + 모든 전투 도구 슬롯
- 기획 `ArtSource/Prologue/프롤로그_화면구성.xlsx`(참고 그림 `reference/01~08`) 기준. docx 1~4장 대본(prologue.txt)·tutorial_end.txt·이길 수 없는 판은 없앰.
- 새로하기 → `SceneFlow.StartTutorial`: `MAP_DREAM_ROOM`을 `DreamRun.Begin(prologue: true)`(테마 `DreamRoom`, 어린 이안 `Characters/IanChild`) → Explore(ExploreTutorial: 검은 화면 대사 → WASD → 몽결정(기존 탐색 규칙) → 길 잇기) → Battle(BattleTutorial: 스탠드만, `BattleHUD.ManualStart` "전투 시작", 쉬운 2웨이브) → `ReturnToDreamDoor`(DoorMode, 출현 칸 = 문) → 문에 닿으면 `FinishTutorial` → `Stories/prologue_wake.txt`(06~08) → 메인.
- `DreamUI`: 대사창(둥근 남색, "화자 | 대사")·조작 툴팁(키캡)·알림. 코드로 만든 둥근 9-slice. `TutorialSteps`: Say/Guide, 툴팁은 다음 단계까지 유지, 오른쪽 위 건너뛰기.
- 스토리 대본 `[효과] 눈뜨기/흔들림/암전/초상화없음`, `[소리] 이름`. StoryLine.effect/sound.
- BattleHUD 건설 고리 → 왼쪽 아래 도구 슬롯 + 청록 설치 구역(`MapView.ShowBuildTiles`) + 녹색 미리보기, 미니맵은 슬롯 위로. 모든 전투.
- `MapTheme.Backdrop`(테마 폴더 `backdrop`) + `BackdropMapArea`: 방 그림 한 장 위에 칸. Explore·MapView 둘 다.
- Artlist 9장(1,125크레딧): 방 배경, 바닥·결정·소용돌이·수면등 시트, 문·깨어남 POV·앉은 장면, 어린 이안 픽셀 시트, 초상화 이안·도하·어린 이안. 빌더 `build_prologue_art.py`, `build_ian_sprites.py child`. 남은 크레딧 약 5,600.
- 효과음 chain·blanket 합성 추가. Unity에서 확인 필요.

### 2026-11-05: 프롤로그 전투 오류 수정 + 탐색 채굴 타격감
- "타워 목록에 빈 항목" 오류: `LockTowers`(빈 자리 만들기)가 `BattleTutorial.SetupStage`(검사 다시 돌림)보다 먼저라 새 게임(스탠드만 해금)에서 5개 오류 → SetupStage를 먼저, 잠금은 맨 뒤. 프롤로그 옆 칸 자동 수집은 뺌(기존 규칙).
- 탐색: 걷기 0.4초/칸, 화면은 pendingPan으로 부드럽게 따라감. 채굴: 어둠 2·작은 결정 3·큰 결정 4번 두드려 깸(금 3단계 텍스처 코드 생성, 빛 새기, 파편, 히트스톱 0.045초, 결정 조각이 몽결정 숫자로 날아감). 효과음 dig·dig_crystal·gem_in 합성. 화면 전체 흔들림은 여전히 없음. Unity에서 확인 필요.

### 2026-11-06: Unity CLI 연결(Mac)
- 사용자 Mac에 Unity CLI(Homebrew `unity-cli`) + `com.unity.pipeline` 0.10.0-exp.1 패키지 + `unity skill install claude-code`. Unity 6000.3.25f1.
- **Mac에서 도는 Claude Code만** 에디터를 조종할 수 있다(파이프라인 서버는 localhost 전용). 클라우드 세션은 여전히 StubCompile·CoreTests로만 확인.
- Mac 세션에서 할 수 있는 확인: `unity status`(연결), `unity command`/`unity eval`(에디터 조종: 콘솔 오류, 플레이 모드, 스크린샷), `unity logs`, `unity doctor`. MCP(`unity mcp configure`)는 쓰지 않음(문서 권장: 셸 명령이 더 빠름).
- AI Assistant 패키지(`com.unity.ai.assistant`)는 안 씀. 넣게 되면 2.13 이상이어야 CLI와 충돌 없음. 그 패키지 안 MCP 서버는 Unity가 지원 종료.
- Unity 6.8부터 CoreCLR(Mono 제거): 정적 변수가 플레이 모드 재시작 때 초기화되지 않음(Tutorial.Active·DreamRun·SceneFlow·각종 캐시). 올릴 때 Project Auditor로 검사하고 `[AutoStaticsCleanup]` 등으로 초기화. 지금(6.3)은 할 일 없음.

