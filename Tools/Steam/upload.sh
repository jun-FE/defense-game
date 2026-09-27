#!/bin/bash
# Builds/Windows 폴더를 Steam에 업로드한다(SteamPipe). macOS용.
# 준비: Steamworks SDK를 받아 Tools/Steam/sdk 폴더에 압축을 푼다.
#       (https://partner.steamgames.com/downloads/list → steamworks_sdk_xxx.zip)
# 사용: ./upload.sh <AppId> <DepotId> <빌드용 Steam 계정> ["설명"]
# 업로드 후 Steamworks 사이트 > SteamPipe > Builds 에서 브랜치에 라이브로 설정한다.
set -e

if [ $# -lt 3 ]; then echo "사용법: $0 <AppId> <DepotId> <SteamUser> [설명]"; exit 1; fi
APP_ID="$1"; DEPOT_ID="$2"; STEAM_USER="$3"; DESC="${4:-build $(date '+%Y-%m-%d %H:%M')}"

HERE="$(cd "$(dirname "$0")" && pwd)"
STEAMCMD="$HERE/sdk/tools/ContentBuilder/builder_osx/steamcmd.sh"
CONTENT="$HERE/../../Builds/Windows"

[ -f "$STEAMCMD" ] || { echo "steamcmd를 찾을 수 없습니다: $STEAMCMD (Steamworks SDK를 Tools/Steam/sdk 에 풀어 주세요)"; exit 1; }
[ -f "$CONTENT/DefenseGame.exe" ] || { echo "빌드가 없습니다: $CONTENT (Unity 메뉴 Defense > Windows 빌드를 먼저 실행하세요)"; exit 1; }
CONTENT="$(cd "$CONTENT" && pwd)"

mkdir -p "$HERE/output"
SCRIPT="$HERE/output/app_build_$APP_ID.vdf"
sed -e "s|__APP_ID__|$APP_ID|" -e "s|__DEPOT_ID__|$DEPOT_ID|" -e "s|__DESC__|$DESC|" \
    -e "s|\"output\"|\"$HERE/output\"|" -e "s|\"../../Builds/Windows\"|\"$CONTENT\"|" \
    "$HERE/app_build.vdf" > "$SCRIPT"

chmod +x "$STEAMCMD" "$(dirname "$STEAMCMD")"/steamcmd 2>/dev/null || true
"$STEAMCMD" +login "$STEAM_USER" +run_app_build "$SCRIPT" +quit
