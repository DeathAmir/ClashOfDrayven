#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PIN="27f169c6c64175896d86e14f0d23c8d85c119c2c"
SOURCE="$ROOT/third_party/LuaJIT"
NDK="${ANDROID_NDK_ROOT:-${ANDROID_NDK_HOME:-$ANDROID_HOME/ndk/27.0.12077973}}"
BIN="$NDK/toolchains/llvm/prebuilt/linux-x86_64/bin"
WORK="$ROOT/build/luajit"
VENDOR="$ROOT/android/app/src/main/cpp/vendor/luajit"
LUA="$ROOT/android/app/src/main/lua/gameplay.lua"
GEN="$ROOT/android/app/src/main/cpp/generated/LuaGameplay.generated.hpp"

if [[ ! -f "$SOURCE/src/Makefile" ]]; then
  echo "Pinned LuaJIT submodule is missing. Clone with --recurse-submodules or run: git submodule update --init --recursive" >&2
  exit 2
fi
ACTUAL="$(git -C "$SOURCE" rev-parse HEAD 2>/dev/null || true)"
if [[ "$ACTUAL" != "$PIN" ]]; then
  echo "LuaJIT submodule mismatch: expected $PIN, got ${ACTUAL:-unknown}" >&2
  exit 3
fi

rm -rf "$WORK" "$VENDOR"
mkdir -p "$WORK" "$VENDOR/include"
cp -a "$SOURCE" "$WORK/host"

make -C "$WORK/host" -j2
export LUA_PATH="$WORK/host/src/?.lua;$WORK/host/src/jit/?.lua;;"
"$WORK/host/src/luajit" -bWd "$LUA" "$WORK/gameplay32.luac"
"$WORK/host/src/luajit" -bXd "$LUA" "$WORK/gameplay64.luac"
unset LUA_PATH
python "$ROOT/scripts/embed_lua_bytecode.py" --bytecode32 "$WORK/gameplay32.luac" --bytecode64 "$WORK/gameplay64.luac" --out "$GEN"

cp "$WORK/host/src/"*.h "$VENDOR/include/"

build_one() {
  local abi="$1" cc="$2" cross="$3" hostcc="$4"
  local src="$WORK/$abi"
  cp -a "$WORK/host" "$src"
  make -C "$src" clean >/dev/null
  make -C "$src" -j2 \
    HOST_CC="$hostcc" \
    CROSS="$BIN/$cross" \
    STATIC_CC="$BIN/$cc -fPIC" \
    DYNAMIC_CC="$BIN/$cc -fPIC" \
    TARGET_LD="$BIN/$cc" \
    TARGET_AR="$BIN/llvm-ar rcus" \
    TARGET_STRIP="$BIN/llvm-strip" \
    TARGET_SYS=Linux
  mkdir -p "$VENDOR/$abi"
  cp "$src/src/libluajit.a" "$VENDOR/$abi/libluajit.a"
  "$BIN/llvm-strip" -S "$VENDOR/$abi/libluajit.a" || true
  echo "LuaJIT ready: $abi $(stat -c%s "$VENDOR/$abi/libluajit.a") bytes"
}

build_one arm64-v8a aarch64-linux-android24-clang aarch64-linux-android- gcc
build_one armeabi-v7a armv7a-linux-androideabi24-clang arm-linux-androideabi- "gcc -m32"
build_one x86_64 x86_64-linux-android24-clang x86_64-linux-android- gcc

echo "LuaJIT source pinned in third_party/LuaJIT at $PIN; bytecode and static runtimes embedded into DrayvenEngine inputs."
