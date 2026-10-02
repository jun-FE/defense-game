#!/bin/bash
# 더블클릭하면 ArtSource 폴더에 새로 넣은 원본 아트를 GitHub에 올린다.
# 웹 업로드(파일당 25MB 제한)로 안 올라가는 큰 파일용. git push는 파일당 100MB까지.
cd "$(dirname "$0")/.." || exit 1
pause() { read -n 1 -s -r -p "아무 키나 누르면 닫힙니다"; echo; }

echo "== 원본 아트 올리기 =="
echo "폴더: $(pwd)/ArtSource"
echo

if [ ! -d .git ]; then
    echo "먼저 Setup/Update.command를 한 번 실행해 주세요."; pause; exit 1
fi

git pull -q --ff-only || { echo "최신 버전 받기에 실패했습니다. 화면을 캡처해서 보내 주세요."; pause; exit 1; }

if [ -z "$(git status --porcelain -- ArtSource)" ]; then
    echo "ArtSource에 새 파일이 없습니다."
    echo "열리는 Finder 창(ArtSource)의 알맞은 폴더에 파일을 넣고 이 스크립트를 다시 실행하세요."
    open ArtSource
    pause; exit 0
fi

# 100MB 넘는 파일은 GitHub가 거부한다.
BIG=$(git status --porcelain -uall -- ArtSource | sed 's/^...//' | tr -d '"' | while IFS= read -r f; do
    [ -f "$f" ] && [ "$(stat -f%z "$f" 2>/dev/null || stat -c%s "$f")" -gt 99000000 ] && echo "  $f"; done)
if [ -n "$BIG" ]; then
    echo "100MB가 넘어서 올릴 수 없는 파일이 있습니다:"
    echo "$BIG"
    echo "PSD라면 레이어별 PNG로 내보내고, 영상이라면 프레임 PNG로 나눠 주세요."
    pause; exit 1
fi

echo "올릴 파일:"
git status --short -uall -- ArtSource
echo
git add -- ArtSource
git -c user.name="$(git config user.name || echo jun-FE)" -c user.email="$(git config user.email || echo craftttime@gmail.com)" \
    commit -q -m "원본 아트 추가 ($(date '+%Y-%m-%d %H:%M'))" || { echo "커밋에 실패했습니다."; pause; exit 1; }

echo "GitHub에 올리는 중... (처음이면 GitHub 아이디와 비밀번호 대신 토큰을 물어볼 수 있어요)"
if git push -q origin HEAD:main; then
    echo; echo "완료! Claude에게 '업로드 했어'라고 알려 주세요."
else
    git reset -q --soft HEAD~1
    echo
    echo "올리기에 실패했습니다. 파일은 그대로 있으니 화면을 캡처해서 보내 주세요."
fi
pause
