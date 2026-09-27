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
3. 처음 열 때는 패키지와 에셋을 가져오느라 몇 분 걸립니다. 열리면 `Assets/Scenes/Main` 데모 씬이 자동으로 만들어집니다.
4. 상단의 ▶ Play 버튼을 누릅니다.
5. (VS Code) Unity 메뉴 **Settings(Preferences) > External Tools > External Script Editor**에서 Visual Studio Code를 고르면, 스크립트를 더블클릭할 때 VS Code로 열립니다.

## 2. 데모 게임 조작

- 하단 버튼이나 `1`, `2` 키로 타워를 고르고, 길 옆의 어두운 칸을 클릭해 짓습니다.
- `Space`를 누르면 다음 웨이브를 바로 시작하고, 남은 시간만큼 보너스 골드를 받습니다.
- 오른쪽 위 `x1`/`x2` 버튼으로 게임 속도를 바꿉니다.
- 10웨이브를 막으면 승리, 라이프가 0이 되면 패배입니다. 5웨이브마다 보스가 나옵니다.

## 3. 프로젝트 구조

```
Assets/
  Scripts/
    Core/      GameManager(골드·라이프·승패), PathRoute(적 이동 경로)
    Enemies/   Enemy, WaveSpawner(웨이브 생성·난이도)
    Towers/    Tower, Projectile, BuildSlot, BuildManager
    UI/        HUD (프로토타입용 화면 UI와 입력)
    Steam/     SteamManager (Steam API 초기화)
    Editor/    DemoSceneBuilder(데모 씬 생성), BuildMenu(Windows 빌드)
  Art/ Prefabs/ Scenes/   ← 처음 열 때 자동 생성
Setup/         개발 환경 설치 스크립트 (install-mac.sh, Install.bat)
Tools/Steam/   Steam 업로드 스크립트
```

- 밸런스는 Inspector에서 조정합니다.
  - 타워: `Assets/Prefabs/BasicTower`, `CannonTower`의 사거리, 공격 속도, 피해량, 비용
  - 웨이브: 씬의 `Game` 오브젝트에 있는 `WaveSpawner`
- 아트 교체: `Assets/Art`의 흰색 도형을 실제 스프라이트로 바꾸거나, 프리팹의 SpriteRenderer에 새 스프라이트를 넣으면 됩니다.
- 데모 씬을 처음 상태로 되돌리려면 메뉴 **Defense > 데모 씬 다시 만들기**를 실행합니다. 프리팹도 덮어씁니다.

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
- [ ] 여러 스테이지와 스테이지 선택 화면
- [ ] HUD를 uGUI 또는 UI Toolkit으로 교체, 입력을 Input System 패키지로 교체
- [ ] 렌더링을 URP 2D로 전환 (조명·후처리가 필요해질 때)
- [ ] Steam 도전과제, 클라우드 세이브
- [ ] 한국어·영어 현지화 (Localization 패키지)
