#!/usr/bin/env bash
set -euo pipefail

PRESET=""
RID=""
CONFIG="Release"

usage() {
  cat <<'EOF'
Usage: scripts/build-native.sh [--preset <name>] [--rid <runtime identifier>] [--config <Configuration>]

Preset または RID のいずれかを必ず指定してください。
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --preset)
      PRESET="$2"
      shift 2
      ;;
    --rid)
      RID="$2"
      shift 2
      ;;
    --config)
      CONFIG="$2"
      shift 2
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      usage
      exit 1
      ;;
  esac
done

declare -A MAP
MAP["win-x64"]="windows-x64"
MAP["win-arm64"]="windows-arm64"
MAP["linux-x64"]="linux-x64"
MAP["linux-arm64"]="linux-arm64"
MAP["osx-x64"]="macos-x64"
MAP["osx-arm64"]="macos-arm64"

if [[ -z "$PRESET" ]]; then
  if [[ -z "$RID" ]]; then
    echo "RID か Preset のどちらかは必須です。" >&2
    exit 1
  fi
  if [[ -z "${MAP[$RID]+_}" ]]; then
    echo "未知の RID '$RID' です。scripts/build-native.sh を更新してください。" >&2
    exit 1
  fi
  PRESET="${MAP[$RID]}"
else
  for KEY in "${!MAP[@]}"; do
    if [[ "${MAP[$KEY]}" == "$PRESET" ]]; then
      RID="$KEY"
      break
    fi
  done
  if [[ -z "$RID" ]]; then
    echo "Preset '$PRESET' に対応する RID が定義されていません。" >&2
    exit 1
  fi
fi

BUILD_PRESET="${PRESET}-$(echo "$CONFIG" | tr '[:upper:]' '[:lower:]')"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
pushd "$REPO_ROOT/native" > /dev/null

echo "Configuring preset '$PRESET'..."
cmake --preset "$PRESET"

echo "Building preset '$BUILD_PRESET'..."
cmake --build --preset "$BUILD_PRESET"

echo "Installing artifacts for RID '$RID'..."
cmake --install --preset "$BUILD_PRESET"

echo "Native binaries are available under $(cd "$REPO_ROOT/artifacts/native/$RID" && pwd)"

popd > /dev/null
