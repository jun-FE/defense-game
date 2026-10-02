# Defense Game

Unity 6.3 LTS로 만드는 2D 타워 디펜스 게임입니다. Steam 출시를 목표로 합니다.

## 1. 처음 설치 (한 번만)

이 폴더는 경로에 한글이나 공백이 없는 곳에 두는 걸 권장합니다. 예: `~/defense-game`, `C:\Dev\defense-game`

### macOS

**터미널**(Spotlight에서 `터미널` 검색)을 열고 아래 명령을 붙여넣은 뒤 엔터를 누릅니다.

```bash
bash ~/defense-game/Setup/install-mac.sh
```

- 폴더 위치가 다르면 경로를 바꿔 주세요.
- `Setup/Install.command`를 더블클릭해도 됩니다. "확인되지 않은 개발자" 경고가 뜨면 **우클릭 → 열기**를 누르세요.
- 중간에 Mac 로그인 비밀번호를 물어봅니다. 입력해도 화면에 안 보이는 게 정상입니다.

| 항목 | 용도 |
| --- | --- |
| Homebrew | 맥용 프로그램 설치 도구 (없을 때만 설치) |
| Git, Git LFS | 버전 관리, 이미지·사운드 같은 큰 파일 관리 |
| Unity Hub | Unity 에디터와 라이선스 관리 |
| Unity 6.3 LTS + Windows Build Support (Mono) | 게임 엔진과, 맥에서 Windows용 게임을 빌드하는 모듈 |
| VS Code + Unity 확장 + .NET SDK | C# 코드 편집과 디버깅 |
| Steam 클라이언트 | Steam 연동 테스트 |

옵션: `SKIP_VSCODE=1 bash ~/defense-game/Setup/install-mac.sh` (Rider를 쓸 때), `SKIP_STEAM=1`.

### Windows

`Setup\Install.bat`을 **더블클릭**합니다. 관리자 권한 요청이 뜨면 "예"를 누르세요.
Git, Git LFS, Unity Hub, Unity 6.3 LTS + Windows IL2CPP 모듈, Visual Studio 2022(Unity 워크로드), Steam을 설치합니다.
옵션: `Install.bat -SkipVisualStudio`, `-SkipSteam`.

### 설치가 끝나면 (공통)

이미 설치된 항목은 건너뜁니다. Unity 에디터를 받느라 **10~30분** 걸리고, 디스크는 약 20GB가 필요합니다.

1. Unity Hub에 로그인하고 **Personal(무료) 라이선스**를 활성화합니다.
2. Hub에서 **Projects > Add > Add project from disk**를 누르고 이 폴더를 선택합니다.
3. 처음 열 때는 패키지와 에셋을 가져오느라 몇 분 걸립니다. 열리면 타이틀·스토리·게임 씬이 자동으로 만들어지고 `Assets/Scenes/Title`이 열립니다.
4. 상단의 ▶ Play 버튼을 누릅니다.
5. (VS Code) Unity 메뉴 **Settings(Preferences) > External Tools > External Script Editor**에서 Visual Studio Code를 고르면, 스크립트를 더블클릭할 때 VS Code로 열립니다.

## 2. 게임 흐름과 조작

**타이틀(Title) → 스토리(Story) → 게임(Main)** 순서로 진행됩니다. 에디터에서는 `Assets/Scenes/Title`을 열고 ▶ Play를 누르세요.

- **타이틀 화면**
  - 시작하기: 1-1 스테이지를 시작합니다. 스테이지에 스토리가 있으면 스토리부터 나옵니다.
  - 스테이지 선택: 클리어한 스테이지의 다음 스테이지까지 열립니다.
  - 설정: 마스터 볼륨, 스토리 텍스트 속도, 전체 화면, 진행 초기화. 설정은 자동 저장됩니다.
  - 종료
- **스토리 화면**
  - 클릭, `Space`, `Enter`: 글자가 나오는 중이면 대사를 바로 완성하고, 다 나왔으면 다음 대사로 넘어갑니다.
  - 우상단 **스킵 ▶▶** 버튼이나 `Esc`: 스토리를 건너뛰고 바로 게임을 시작합니다.
- **전투 화면** (다방향 디펜스: 북쪽·동쪽에서 적이 꿈의 중심으로 온다)
  - 하단 타워 카드나 `1` 키로 타워를 고르고, 밝은 칸을 클릭해 짓습니다. 초록 칸은 건설 가능, 빨간 칸은 불가입니다. 우클릭·`Esc`로 취소합니다.
  - 지은 타워를 클릭하면 오른쪽에 정보와 **강화** 버튼이 나옵니다.
  - 웨이브 사이 준비 시간에 `Space`(바로 시작)로 웨이브를 당길 수 있습니다.
  - `x1`/`x2`/`x4` 속도, `Esc` 일시정지(계속 / 처음부터 / 중도 귀환 / 메인 메뉴).
  - 꿈의 중심 HP가 0이 되면 실패, 모든 웨이브를 막으면 성공입니다.
  - 개발용(에디터·개발 빌드): `F1` 재화 +100, `F2` 적 전부 처치.

## 수선소(로비) 화면

타이틀 **시작하기** → 수선소 → 의뢰함에서 편지 선택 → **의뢰 시작하기** → (스토리) → 전투 → 결과 화면 **수선소로** 순서입니다.

- **배경**: `Assets/Art/Lobby`의 레이어 13장을 `Assets/Art/Lobby/lobby_layout.json` 배치대로 실행 중에 조립합니다. 마우스를 움직이면 레이어 깊이(depth)에 따라 살짝 시차가 생기고, 이안·도하는 숨쉬듯 움직입니다.
  - 배치 고치기: JSON의 `x`, `y`(이미지 중심), `width`(화면에서의 폭)는 1920×1080 화면 기준 픽셀이라 포토샵 좌표처럼 읽으면 됩니다. 순서는 위에서 아래로 뒤→앞입니다.
  - Unity 없이 미리보기: `python3 Tools/Art/preview_lobby.py 미리보기.png` (`--ui`를 붙이면 UI 자리도 표시)
- **원본 이미지**: `ArtSource/Lobby`. 지금은 카카오톡으로 받은 검은 배경 JPEG이라 `bash Tools/Art/lobby_keys.sh`로 배경을 투명하게 바꿔 `Assets/Art/Lobby`에 넣었습니다. 투명 PNG 원본이 오면 같은 이름으로 `Assets/Art/Lobby`에 바로 덮어쓰면 됩니다.
- **UI**: 의뢰함·편지 카드·말풍선·탭 바는 UI 아트가 오기 전까지 임시로 그립니다(`Assets/Scripts/Lobby/LobbyUI.cs`). 필요한 UI 이미지 목록은 `ArtSource/Lobby/README.md`에 있습니다.
- **의뢰**: `Assets/Data/Quests`의 의뢰 에셋(제목, 태그, 편지, 보상 문구, 연결 스테이지)을 고치고, 목록 순서는 `Assets/Resources/QuestDatabase`에서 정합니다. `stageIndex`가 -1이면 "아직 도착하지 않은 의뢰"로 잠겨 보입니다. 앞 스테이지를 클리어하면 다음 의뢰가 열립니다.
- 탭: 수선소·의뢰함만 동작하고, 기술(마스터리)은 4주차, 도감·가방·지도는 잠금 표시입니다.

## 스테이지와 스토리 고치기

- **스테이지 목록**: `Assets/Resources/StageDatabase`를 선택하고 Inspector에서 순서를 바꾸거나 추가합니다.
- **스테이지 설정**: `Assets/Data/Stage_1-1` 등에서 이름, 시작 골드, 라이프, 웨이브 수, 적 체력 배율, 시작 전 스토리를 정합니다.
- **스토리 대사**: `Assets/Data/Story_1-1_Intro` 등을 선택하고 Inspector의 `Lines`에서 말하는 사람(`Speaker`)과 대사(`Text`)를 고칩니다. `Speaker`를 비우면 나레이션(기울임체)으로 나옵니다.
- **새로 만들기**: Project 창에서 우클릭 → **Create → Defense → Story** 또는 **Stage**를 고릅니다. 만든 스테이지는 StageDatabase에 추가하세요.
- 지금은 모든 스테이지가 같은 맵을 씁니다. 스테이지별 맵은 다음 단계에서 붙일 수 있습니다.

## 전투 데이터 고치기 (기획자용)

전투 수치는 코드가 아니라 `Assets/Data/Battle`의 데이터 에셋에서 고칩니다. 시스템 기획서 7장의 테이블과 같은 구조이며, ID도 같습니다.

| 에셋 | 기획서 테이블 | 고치는 것 |
| --- | --- | --- |
| `RULE_BASE` | GameRule | 방어력 상수, 공격속도 하한, 틱 간격, 이동 배율 하한·상한 |
| `EN_TOY`, `EN_RUSH`, `EN_HEAVY` | Enemy | HP, 방어력, 이동 속도, 누수 피해, 처치 재화, 색·크기 |
| `TW_LAMP` | Tower + TowerLevel | 설치 비용, 단계별 공격력·사거리·공격속도·치명타·강화 비용, 프리팹·아이콘 |
| `MAP_ROOM` | Map + SpawnPoint | 중심 위치, 출현 경로, 건설 구역, 카메라 범위 |
| `STG_Q01` | Stage + Wave + SpawnGroup | 시작 재화, 중심 HP, 지을 수 있는 타워, 웨이브별 준비 시간·생성 묶음 |

- **공격속도**는 "한 번 공격한 뒤 다음 공격까지 걸리는 초"입니다. 낮을수록 빠르고, 최소 0.2초입니다(기획서 `attack_sec`).
- 고친 뒤 메뉴 **Defense → 전투 데이터 검사**로 중복 ID, 없는 참조, 범위를 벗어난 값을 확인하세요. 오류가 있으면 전투 화면에도 표시되고 시작되지 않습니다.
- "데모 게임 다시 만들기"는 이미 있는 데이터 에셋을 덮어쓰지 않습니다.

## 전투 코어와 검산 테스트 (개발자용)

- 전투 규칙은 Unity와 분리된 순수 C#입니다(`Assets/Scripts/Battle/Core`, 네임스페이스 `Akmong.Battle`). 0.05초 고정 틱으로만 시간이 흐르고, 같은 시드면 결과가 같습니다.
- 화면 쪽(`Assets/Scripts/Battle/View`)은 전투 사건을 받아 그리기만 하고, 바꿀 때는 `TryBuild`, `TryUpgrade` 같은 명령만 씁니다.
- 시스템 기획서의 계산 사례(16피해, 7회 7초 처치, 잔액 76, 강화 실패 시 변화 없음 등)를 자동으로 검사합니다.
  - Unity: 메뉴 **Defense → 전투 검산 테스트 실행** (Console에 결과)
  - 터미널(Mono 필요): `bash Tools/CoreTests/run.sh`
- 밸런스 모의 플레이: `bash Tools/CoreTests/balance.sh` → 성공률, 잔여 중심 HP, 소요 시간을 기획 목표와 함께 보여줍니다.
- 첫 의뢰 웨이브는 기획서 샘플보다 적을 줄인 1차 밸런스입니다(기획서 샘플은 시작 재화 120으로 막기 어려운 스트레스 테스트). 기획서 샘플 그대로의 웨이브는 테스트 전용(`SampleContent.SpecSampleStage`)으로 남겨 두었습니다.

## 타워 아트

- **스탠드(TW_LAMP) = 드림캐처**: `Assets/Art/Towers/Dreamcatcher/`
  - `dreamcatcher_build_0~1`: 설치 연출 (마법진에서 나타남, 1회)
  - `dreamcatcher_idle_0~6`: 대기 (흔들림, 반복)
  - `dreamcatcher_attack_0~6`: 공격 (충전 → 깃털 폭발, 발사할 때마다 1회)
  - `feather_projectile`: 날아가는 깃털 (오른쪽이 앞, 비행 방향으로 자동 회전)
  - `icon_tower`, `icon_tower_disabled`: 하단 건설 버튼 아이콘 (골드 부족 시 회색)
  - `icon_feather/crystal/star/lantern`: 아직 안 쓰는 아이콘 (업그레이드 등에 사용 예정)
- 원본 시트: `ArtSource/Towers/dreamcatcher_sheet.jpg`. 다시 자르려면 프로젝트 폴더에서
  `python3 Tools/Art/slice_dreamcatcher.py`를 실행합니다(검은 배경을 투명하게 바꾸고 고리 중심을 기준으로 정렬).
- `Assets/Art/Towers/` 아래 PNG는 자동으로 스프라이트로 가져옵니다(타워 150 PPU, 투사체 220 PPU).
- 애니메이션 속도는 `Assets/Prefabs/Towers/TW_LAMP` 프리팹의 `Visual` 오브젝트 → `TowerVisual`에서 조정합니다.

## 3. 프로젝트 구조

```
Assets/
  Scripts/
    Battle/
      Core/    전투 규칙(순수 C#): BattleSession, BattleMath, DefinitionValidator, SampleContent, BattleSelfTest
      Data/    전투 데이터 에셋(Enemy, Tower, Map, Stage, GameRule)과 변환기
      View/    BattleController(고정 틱), MapView, BattleView, BattleHUD 등 화면
    Core/      SceneFlow(화면 전환), Progress(클리어 기록), GameSettings(설정 저장)
    Data/      StageData(스테이지 목록 항목), StoryData, StageDatabase
    Story/     StoryPlayer (대사 출력, 클릭으로 넘기기, 스킵)
    Lobby/     LobbyBackground(레이어 배경·시차), LobbyUI(의뢰함·편지·탭)
    Towers/    TowerVisual(타워 스프라이트 애니메이션)
    UI/        TitleMenu(메인 메뉴), SettingsPanel(설정), UIKit(공통 스타일)
    Steam/     SteamManager (Steam API 초기화)
    Editor/    DemoSceneBuilder(데모 게임 생성), BattleToolsMenu(검산·데이터 검사), BuildMenu(빌드)
  Art/ Prefabs/ Scenes/ Data/ Resources/   ← 처음 열 때 자동 생성
Setup/         개발 환경 설치 스크립트 (install-mac.sh, Install.bat)
Tools/Steam/   Steam 업로드 스크립트
Tools/CoreTests/ 전투 검산 테스트·밸런스 모의 플레이 (Unity 없이 실행)
```

- 밸런스는 `Assets/Data/Battle`의 데이터 에셋에서 조정합니다(아래 "전투 데이터 고치기").
- 아트 교체: `Assets/Art`의 흰색 도형을 실제 스프라이트로 바꾸거나, 프리팹의 SpriteRenderer에 새 스프라이트를 넣으면 됩니다.
- 씬과 프리팹을 처음 상태로 되돌리려면 메뉴 **Defense > 데모 게임 다시 만들기**를 실행합니다. 씬 3개와 프리팹은 덮어쓰지만, 스테이지·스토리 데이터(`Assets/Data`, `Assets/Resources`)는 건드리지 않습니다.

## 4. Steam 출시 흐름

1. [Steamworks](https://partner.steamgames.com/)에 가입합니다. 세금·은행 정보를 입력하고 Steam Direct 등록비 $100를 내면 **App ID**와 **Depot ID**를 받습니다.
2. `Assets/Scripts/Steam/SteamManager.cs`의 `AppId`와 프로젝트 루트의 `steam_appid.txt`를 받은 App ID로 바꿉니다.
   - 지금은 Valve 테스트용 앱인 `480`(Spacewar)으로 설정돼 있습니다.
   - Steam을 켠 채로 Play를 누르면 Console에 `[Steam] 초기화 성공`이 뜨는지 확인하세요.
3. Unity 메뉴 **Defense > Windows 빌드**를 실행하면 `Builds/Windows/`에 빌드가 만들어집니다.
   - Steam 유저 대부분이 Windows이므로 Windows 빌드가 기본입니다. 맥에서도 Windows 빌드를 만들 수 있습니다(Mono 백엔드).
   - **Defense > macOS 빌드**로 맥 버전도 만들 수 있습니다.
4. [Steamworks SDK](https://partner.steamgames.com/downloads/list)를 받아 `Tools/Steam/sdk/`에 압축을 풉니다.
5. 업로드합니다.
   ```bash
   # macOS (터미널)
   cd ~/defense-game/Tools/Steam
   ./upload.sh 1234560 1234561 내계정 "v0.1"
   ```
   ```powershell
   # Windows (PowerShell)
   cd Tools\Steam
   .\upload.ps1 -AppId 1234560 -DepotId 1234561 -SteamUser 내계정 -Desc "v0.1"
   ```
6. Steamworks 사이트의 **SteamPipe > Builds**에서 업로드한 빌드를 브랜치에 라이브로 설정합니다.
7. 스토어 페이지는 출시 최소 2주 전에 "곧 출시"로 공개해야 합니다. 위시리스트를 모으려면 몇 달 일찍 여는 게 좋습니다.

## 5. 다음 작업 제안

- [ ] 실제 아트와 사운드 적용
- [ ] 타워 업그레이드와 판매
- [ ] 적 종류 추가 (빠른 적, 비행 적, 방어력 있는 적)
- [ ] 스테이지별 맵
- [ ] 스토리 화면에 캐릭터 일러스트와 배경 이미지
- [ ] HUD를 uGUI 또는 UI Toolkit으로 교체, 입력을 Input System 패키지로 교체
- [ ] 렌더링을 URP 2D로 전환 (조명·후처리가 필요해질 때)
- [ ] Steam 도전과제, 클라우드 세이브
- [ ] 한국어·영어 현지화 (Localization 패키지)
