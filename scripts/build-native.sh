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

resolve_preset_from_rid() {
  case "$1" in
    win-x64) echo "windows-x64" ;;
    win-arm64) echo "windows-arm64" ;;
    linux-x64) echo "linux-x64" ;;
    linux-arm64) echo "linux-arm64" ;;
    osx-x64) echo "macos-x64" ;;
    osx-arm64) echo "macos-arm64" ;;
    "") echo "" ;;
    *) return 1 ;;
  esac
}

resolve_rid_from_preset() {
  case "$1" in
    windows-x64) echo "win-x64" ;;
    windows-arm64) echo "win-arm64" ;;
    linux-x64) echo "linux-x64" ;;
    linux-arm64) echo "linux-arm64" ;;
    macos-x64) echo "osx-x64" ;;
    macos-arm64) echo "osx-arm64" ;;
    "") echo "" ;;
    *) return 1 ;;
  esac
}

if [[ -z "$PRESET" ]]; then
  if [[ -z "$RID" ]]; then
    echo "RID か Preset のどちらかは必須です。" >&2
    exit 1
  fi
  if ! PRESET="$(resolve_preset_from_rid "$RID")" || [[ -z "$PRESET" ]]; then
    echo "未知の RID '$RID' です。scripts/build-native.sh を更新してください。" >&2
    exit 1
  fi
else
  if ! RID="$(resolve_rid_from_preset "$PRESET")" || [[ -z "$RID" ]]; then
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

echo "Native binaries are available under $REPO_ROOT/artifacts/native/$RID"

popd > /dev/null
