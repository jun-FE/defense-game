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
- **게임 화면**
  - 하단 버튼이나 `1`, `2` 키로 타워를 고르고, 길 옆의 어두운 칸을 클릭해 짓습니다.
  - `Space`: 다음 웨이브를 바로 시작하고, 남은 시간만큼 보너스 골드를 받습니다.
  - `x1`/`x2`: 게임 속도
  - `Esc`: 일시정지 (계속하기 / 다시 하기 / 스테이지 선택 / 메인 메뉴)
  - 모든 웨이브를 막으면 승리, 라이프가 0이 되면 패배입니다. 5웨이브마다 보스가 나옵니다.
  - 승리하면 다음 스테이지로 바로 넘어갈 수 있습니다.

## 스테이지와 스토리 고치기

- **스테이지 목록**: `Assets/Resources/StageDatabase`를 선택하고 Inspector에서 순서를 바꾸거나 추가합니다.
- **스테이지 설정**: `Assets/Data/Stage_1-1` 등에서 이름, 시작 골드, 라이프, 웨이브 수, 적 체력 배율, 시작 전 스토리를 정합니다.
- **스토리 대사**: `Assets/Data/Story_1-1_Intro` 등을 선택하고 Inspector의 `Lines`에서 말하는 사람(`Speaker`)과 대사(`Text`)를 고칩니다. `Speaker`를 비우면 나레이션(기울임체)으로 나옵니다.
- **새로 만들기**: Project 창에서 우클릭 → **Create → Defense → Story** 또는 **Stage**를 고릅니다. 만든 스테이지는 StageDatabase에 추가하세요.
- 지금은 모든 스테이지가 같은 맵을 씁니다. 스테이지별 맵은 다음 단계에서 붙일 수 있습니다.

## 3. 프로젝트 구조

```
Assets/
  Scripts/
    Core/      GameManager(골드·라이프·승패·일시정지), SceneFlow(화면 전환), Progress(클리어 기록),
               GameSettings(설정 저장), PathRoute(적 이동 경로)
    Data/      StageData, StoryData, StageDatabase (ScriptableObject)
    Story/     StoryPlayer (대사 출력, 클릭으로 넘기기, 스킵)
    Enemies/   Enemy, WaveSpawner(웨이브 생성·난이도)
    Towers/    Tower, Projectile, BuildSlot, BuildManager
    UI/        TitleMenu(메인 메뉴), HUD(게임 화면), UIKit(공통 스타일)
    Steam/     SteamManager (Steam API 초기화)
    Editor/    DemoSceneBuilder(데모 게임 생성), BuildMenu(Windows/macOS 빌드)
  Art/ Prefabs/ Scenes/ Data/ Resources/   ← 처음 열 때 자동 생성
Setup/         개발 환경 설치 스크립트 (install-mac.sh, Install.bat)
Tools/Steam/   Steam 업로드 스크립트
```

- 밸런스는 Inspector에서 조정합니다.
  - 타워: `Assets/Prefabs/BasicTower`, `CannonTower`의 사거리, 공격 속도, 피해량, 비용
  - 웨이브: 씬의 `Game` 오브젝트에 있는 `WaveSpawner`
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
