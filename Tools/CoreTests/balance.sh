#!/bin/bash
# 모의 플레이로 첫 의뢰(SampleContent.StageQ01) 밸런스를 잰다. Mono 필요. 인자: 반복 횟수(기본 200)
set -e
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
OUT="$(mktemp -d)"
mcs -langversion:7 -r:System.Numerics -r:System.Core -out:"$OUT/balance.exe" \
    "$ROOT"/Assets/Scripts/Battle/Core/*.cs "$ROOT/Tools/CoreTests/BalanceSim.cs"
mono "$OUT/balance.exe" "$@"
