#!/usr/bin/env bash
# APK de desenvolvimento (Android) em modo batch. O Editor precisa estar FECHADO neste projeto.
# Saída: tools/build/out/MinersIdle-dev.apk (ignorada pelo git). Log: Logs/build.log.
# Instalar no aparelho: adb install -r tools/build/out/MinersIdle-dev.apk
set -u
UNITY="${UNITY_EXE:-C:/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Unity.exe}"
ROOT="$(cd "$(dirname "$0")/../.." && pwd -W 2>/dev/null || pwd)"
mkdir -p "$ROOT/Logs" "$ROOT/tools/build/out"

"$UNITY" -batchmode -nographics -projectPath "$ROOT" -buildTarget Android \
  -executeMethod MinersIdle.EditorTools.BuildTools.BuildAndroidDevBatch \
  -logFile "$ROOT/Logs/build.log"
code=$?

echo "Unity saiu com código $code"
grep -E "\[Build\]|error CS|BuildFailedException|Exception" "$ROOT/Logs/build.log" | head -20
exit $code
