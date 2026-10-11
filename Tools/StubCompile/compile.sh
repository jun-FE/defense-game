#!/bin/bash
# Unity 없이 게임 스크립트 전체를 컴파일만 해 본다(오타·형식 오류 검사용). Mono(mcs) 필요.
# UnityStubs.cs는 Unity API의 빈 껍데기다. 실제 Unity에 있는 API인데 여기 없다고 나오면 스텁에 추가하면 된다.
# 두 설정을 검사한다: 에디터(Mac, Steamworks 포함)와 Windows 빌드(에디터 스크립트 제외).
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
OUT="$(mktemp -d)"
STUBS="$ROOT/Tools/StubCompile/UnityStubs.cs"
status=0
for defs in "-d:UNITY_EDITOR -d:STEAMWORKS_NET -d:UNITY_STANDALONE_OSX" "-d:UNITY_STANDALONE_WIN"; do
  FILES=$(find "$ROOT/Assets" -name '*.cs' | grep -v "/Editor/")
  [ "$defs" != "-d:UNITY_STANDALONE_WIN" ] && FILES="$FILES $(find "$ROOT/Assets" -path '*/Editor/*' -name '*.cs')"
  errors=$(mcs -langversion:7 -target:library -nowarn:0649,0414,0169 $defs -r:System.Core -r:System.Numerics \
       -out:"$OUT/out.dll" "$STUBS" $FILES 2>&1 | grep -E "error" | grep -v "Location of" | sort -u)
  if [ -n "$errors" ]; then echo "$errors"; echo "[$defs] 오류"; status=1; else echo "[$defs] 통과"; fi
done
exit $status
