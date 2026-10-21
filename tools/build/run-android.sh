#!/usr/bin/env bash
# Um comando só: gera o APK de desenvolvimento, instala no celular conectado por USB e abre o jogo.
# O Editor precisa estar FECHADO neste projeto. Rodar no seu terminal (ou no Terminal do app), não no shell restrito
# do Claude: lá o Gradle não consegue abrir a conexão local e o empacotamento falha.
# Uso: bash tools/build/run-android.sh            (gera, instala e abre)
#      bash tools/build/run-android.sh --no-build (só reinstala e abre o último APK)
set -u
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
APK="$ROOT/tools/build/out/MinersIdle-dev.apk"
PACKAGE="com.brunocarvalho.minersidle"
ADB="$(command -v adb || echo "C:/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe")"

if [ "$("$ADB" get-state 2>/dev/null)" != "device" ]; then
  echo "Nenhum celular pronto em 'adb devices'. Confira o cabo USB (com dados) e a depuração USB."
  exit 1
fi

if [ "${1:-}" != "--no-build" ]; then
  bash "$ROOT/tools/build/build-android.sh" || exit $?
fi

"$ADB" install -r "$APK" || exit $?
"$ADB" shell monkey -p "$PACKAGE" -c android.intent.category.LAUNCHER 1 >/dev/null || exit $?
echo "Miners Idle aberto no celular."
