#!/bin/bash
# 더블클릭하면 GitHub에서 최신 버전을 받는다(git pull).
# 처음 한 번은 이 폴더를 저장소에 연결하고, 지금은 쓰지 않는 예전 스크립트를 정리한다.
# Unity를 끈 상태에서 실행하는 것을 권장한다.
cd "$(dirname "$0")/.." || exit 1
REPO="https://github.com/jun-FE/defense-game.git"

echo "== 악몽수선소 프로젝트 업데이트 =="
echo "폴더: $(pwd)"
echo

if [ ! -d .git ]; then
    echo "처음 실행: 이 폴더를 저장소에 연결합니다..."
    git init -q -b main && git remote add origin "$REPO" && git fetch -q origin \
        && git reset -q --hard origin/main && git branch -q -u origin/main \
        && git clean -fq -- '*.cs' '*.asmdef' \
        || { echo "연결에 실패했습니다. 화면을 캡처해서 보내 주세요."; read -n 1 -s -r -p "아무 키나 누르면 닫힙니다"; exit 1; }
    echo "연결 완료"
else
    if ! git pull --ff-only; then
        echo
        echo "업데이트에 실패했습니다. 이 폴더에서 코드 파일을 직접 고쳤다면 충돌일 수 있어요."
        echo "화면을 캡처해서 보내 주세요."
        read -n 1 -s -r -p "아무 키나 누르면 닫힙니다"
        exit 1
    fi
fi

echo
echo "최근 변경:"
git log --oneline -5
echo
echo "완료! Unity로 프로젝트를 열면 새 내용이 반영됩니다."
read -n 1 -s -r -p "아무 키나 누르면 닫힙니다"
