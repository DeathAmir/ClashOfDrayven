# LuaJIT integration

ClashOfDrayven pins the complete upstream LuaJIT source tree as the `third_party/LuaJIT` Git submodule.

- Upstream: `https://github.com/LuaJIT/LuaJIT`
- Pinned commit: `27f169c6c64175896d86e14f0d23c8d85c119c2c`
- Android ABIs: `arm64-v8a`, `armeabi-v7a`, `x86_64`
- Final native library: `libIrAutoX.so`
- Game Lua is compiled to architecture-appropriate LuaJIT bytecode during the Android build and embedded into the native library; raw `.lua`/`.luac` files are rejected by validation.

Clone with:

```bash
git clone --recurse-submodules https://github.com/DeathAmir/ClashOfDrayven.git
```

For an existing checkout:

```bash
git submodule update --init --recursive
```

`scripts/build_luajit_android.sh` verifies that the submodule is exactly at the pinned SHA before compiling the host bytecode tool and the three Android static runtimes.
