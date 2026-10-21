#!/usr/bin/env bash
# Roda os testes do projeto em modo batch. O Editor precisa estar FECHADO neste projeto.
# Uso: bash tools/ci/run-tests.sh [EditMode|PlayMode|all]   (padrão: all)
# Saída: tools/ci/out/<plataforma>.xml e Logs/tests-<plataforma>.log.
set -u
UNITY="${UNITY_EXE:-C:/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Unity.exe}"
ROOT="$(cd "$(dirname "$0")/../.." && pwd -W 2>/dev/null || pwd)"
WHICH="${1:-all}"
mkdir -p "$ROOT/Logs" "$ROOT/tools/ci/out"

run() {
  local platform="$1"
  "$UNITY" -batchmode -nographics -projectPath "$ROOT" -runTests -testPlatform "$platform" \
    -testResults "$ROOT/tools/ci/out/$platform.xml" -logFile "$ROOT/Logs/tests-$platform.log"
  local code=$?
  echo "== $platform: código $code"
  grep -o 'result="[A-Za-z]*" total="[0-9]*" passed="[0-9]*" failed="[0-9]*"' "$ROOT/tools/ci/out/$platform.xml" 2>/dev/null | head -1
  return $code
}

status=0
if [ "$WHICH" = "all" ] || [ "$WHICH" = "EditMode" ]; then run EditMode || status=1; fi
if [ "$WHICH" = "all" ] || [ "$WHICH" = "PlayMode" ]; then run PlayMode || status=1; fi
exit $status
