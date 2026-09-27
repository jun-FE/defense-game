#!/bin/bash
# Defense Game - macOS 개발 환경 원클릭 설치
# 설치 항목: Homebrew, Git, Git LFS, Unity Hub, Unity 6.3 LTS(+Windows 빌드 모듈),
#           VS Code(+Unity 확장, .NET SDK), Steam
# 이미 설치된 항목은 건너뛴다. 여러 번 실행해도 안전하다.
# 옵션(환경 변수): SKIP_VSCODE=1  SKIP_STEAM=1  UNITY_STREAM=6000.3

UNITY_STREAM="${UNITY_STREAM:-6000.3}"
STREAM_RE="$(printf '%s' "$UNITY_STREAM" | sed 's/\./\\./g')"   # 6000.3 -> 6000\.3 (grep용)
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
HUB_BIN="/Applications/Unity Hub.app/Contents/MacOS/Unity Hub"
EDITOR_ROOT="/Applications/Unity/Hub/Editor"
WARNINGS=""

step() { printf '\n\033[36m==> %s\033[0m\n' "$1"; }
ok()   { printf '    \033[32m%s\033[0m\n' "$1"; }
warn() { printf '    \033[33m%s\033[0m\n' "$1"; WARNINGS="${WARNINGS}  - $1"$'\n'; }

brew_formula() { # <formula> <표시 이름>
    if brew list --formula "$1" >/dev/null 2>&1; then ok "$2 - 이미 설치됨"; return; fi
    echo "    $2 설치 중..."
    if brew install "$1"; then ok "$2 - 설치 완료"; else warn "$2 설치 실패. 'brew install $1'로 다시 시도해 주세요."; fi
}

brew_cask() { # <cask> <표시 이름> [/Applications 안의 앱 이름]
    if { [ -n "$3" ] && [ -d "/Applications/$3" ]; } || brew list --cask "$1" >/dev/null 2>&1; then
        ok "$2 - 이미 설치됨"; return
    fi
    echo "    $2 설치 중... (몇 분 걸릴 수 있습니다)"
    if brew install --cask "$1"; then ok "$2 - 설치 완료"; else warn "$2 설치 실패. 'brew install --cask $1'로 다시 시도해 주세요."; fi
}

find_editor() { # 설치된 $UNITY_STREAM.x 중 최신 버전 이름
    [ -d "$EDITOR_ROOT" ] || return
    for v in $(ls -1 "$EDITOR_ROOT" | grep -E "^${STREAM_RE}\.[0-9]+f[0-9]+$" | sort -t. -k3,3nr); do
        if [ -d "$EDITOR_ROOT/$v/Unity.app" ]; then echo "$v"; return; fi
    done
}

load_brew() {
    for b in /opt/homebrew/bin/brew /usr/local/bin/brew; do
        if [ -x "$b" ]; then eval "$("$b" shellenv)"; return 0; fi
    done
    return 1
}

if [ "$(uname)" != "Darwin" ]; then echo "이 스크립트는 macOS 전용입니다. Windows는 Setup/Install.bat을 쓰세요."; exit 1; fi

printf '\033[36m================================================\n  Defense Game 개발 환경 설치 (macOS)\n================================================\033[0m\n'
echo "프로젝트 위치: $PROJECT_ROOT"
echo "중간에 Mac 로그인 비밀번호를 물어볼 수 있습니다. (입력해도 화면에 표시되지 않는 게 정상입니다)"

# 1. Homebrew
step "1/5 Homebrew (맥용 프로그램 설치 도구)"
command -v brew >/dev/null 2>&1 || load_brew
if command -v brew >/dev/null 2>&1; then
    ok "Homebrew - 이미 설치됨"
else
    echo "    Homebrew 설치 중... (Xcode 명령줄 도구도 함께 설치됩니다)"
    /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
    if ! load_brew; then
        printf '\033[31m    Homebrew 설치에 실패했습니다. https://brew.sh 를 참고해 직접 설치한 뒤 다시 실행해 주세요.\033[0m\n'
        exit 1
    fi
    # 새 터미널에서도 brew를 쓸 수 있게 등록
    BREW_BIN="$(command -v brew)"
    if ! grep -q "brew shellenv" "$HOME/.zprofile" 2>/dev/null; then
        echo "eval \"\$($BREW_BIN shellenv)\"" >> "$HOME/.zprofile"
    fi
    ok "Homebrew - 설치 완료"
fi

# 2. 기본 도구
step "2/5 Git / Git LFS / Unity Hub"
brew_formula git "Git"
brew_formula git-lfs "Git LFS"
git lfs install >/dev/null 2>&1 && ok "Git LFS 활성화"
brew_cask unity-hub "Unity Hub" "Unity Hub.app"

# 3. 코드 편집기 + Steam
step "3/5 VS Code / Steam"
if [ "${SKIP_VSCODE:-0}" = "1" ]; then
    ok "VS Code - 건너뜀"
else
    brew_cask visual-studio-code "VS Code" "Visual Studio Code.app"
    if command -v dotnet >/dev/null 2>&1; then ok ".NET SDK - 이미 설치됨"; else brew_cask dotnet-sdk ".NET SDK (C# 자동완성용)"; fi
    CODE_BIN="/Applications/Visual Studio Code.app/Contents/Resources/app/bin/code"
    if [ -x "$CODE_BIN" ]; then
        "$CODE_BIN" --install-extension visualstudiotoolsforunity.vstuc >/dev/null 2>&1 \
            && ok "VS Code Unity 확장 설치" || warn "VS Code Unity 확장 설치 실패. VS Code 확장 탭에서 'Unity'를 검색해 설치해 주세요."
    fi
fi
if [ "${SKIP_STEAM:-0}" = "1" ]; then ok "Steam - 건너뜀"; else brew_cask steam "Steam 클라이언트" "Steam.app"; fi

# 4. Unity 에디터
step "4/5 Unity $UNITY_STREAM LTS 에디터 (+ Windows 빌드 모듈)"
EDITOR_VERSION="$(find_editor)"
if [ -n "$EDITOR_VERSION" ]; then
    ok "Unity $EDITOR_VERSION - 이미 설치됨"
elif [ ! -x "$HUB_BIN" ]; then
    warn "Unity Hub를 찾을 수 없어 에디터를 설치하지 못했습니다."
else
    RELEASES="$("$HUB_BIN" -- --headless editors --releases 2>&1)"
    VERSION="$(echo "$RELEASES" | grep -oE "${STREAM_RE}\.[0-9]+f[0-9]+" | sort -u | sort -t. -k3,3n | tail -1)"
    if [ -z "$VERSION" ]; then
        warn "Unity Hub에서 $UNITY_STREAM 버전 목록을 가져오지 못했습니다. Hub > 설치 > 에디터 설치에서 'Unity 6.3 LTS'를 직접 설치해 주세요. (모듈: Windows Build Support (Mono))"
    else
        ARCH="$(uname -m)"; [ "$ARCH" = "arm64" ] || ARCH="x86_64"
        echo "    Unity $VERSION ($ARCH) 다운로드 및 설치 중... (10~30분, 용량 약 10GB)"
        "$HUB_BIN" -- --headless install --version "$VERSION" --architecture "$ARCH" --module windows-mono
        EDITOR_VERSION="$(find_editor)"
        if [ -n "$EDITOR_VERSION" ]; then ok "Unity $EDITOR_VERSION - 설치 완료"
        else warn "Unity $VERSION 설치를 확인하지 못했습니다. Unity Hub에서 설치 상태를 확인해 주세요."; fi
    fi
fi

# 5. 프로젝트 버전 맞추기
step "5/5 프로젝트 설정"
if [ -n "$EDITOR_VERSION" ]; then
    printf 'm_EditorVersion: %s\n' "$EDITOR_VERSION" > "$PROJECT_ROOT/ProjectSettings/ProjectVersion.txt"
    ok "프로젝트 에디터 버전을 $EDITOR_VERSION(으)로 설정"
fi

printf '\n\033[36m================================================\033[0m\n'
if [ -n "$WARNINGS" ]; then
    printf '\033[33m  완료 (확인 필요한 항목이 있습니다)\n%s\033[0m' "$WARNINGS"
else
    printf '\033[32m  설치 완료!\033[0m\n'
fi
printf '\033[36m================================================\033[0m\n'

printf '%s' "$PROJECT_ROOT" | pbcopy
cat <<MSG

다음 단계:
  1. Unity Hub에 로그인하고 무료 라이선스(Personal)를 활성화하세요. (처음 한 번)
  2. Hub의 [Projects] > [Add] > [Add project from disk]에서 아래 폴더를 선택하세요.
     $PROJECT_ROOT
     (경로가 클립보드에 복사돼 있습니다. 폴더 선택 창에서 Cmd+Shift+G 후 붙여넣기)
  3. 프로젝트가 열리면 데모 씬이 자동으로 만들어집니다. 상단의 Play 버튼을 누르세요.

MSG
[ -d "/Applications/Unity Hub.app" ] && open -a "Unity Hub"
