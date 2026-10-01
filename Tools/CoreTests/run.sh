#!/bin/bash
# 전투 코어(Assets/Scripts/Battle/Core)를 Unity 없이 컴파일해 검산 테스트를 돌린다. Mono 필요.
set -e
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
OUT="$(mktemp -d)"
mcs -langversion:7 -nowarn:1998 -r:System.Numerics -out:"$OUT/coretests.exe" \
    "$ROOT"/Assets/Scripts/Battle/Core/*.cs "$ROOT/Tools/CoreTests/RunCoreTests.cs"
mono "$OUT/coretests.exe"
