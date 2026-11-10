# Development

Ocarune is a static recompilation of the Switch 2 *Ocarina of Time*. How it fits together: [design.md](design.md).
It builds on [a64-kit](../third_party/a64-kit) by NotRexed, [Eden](https://git.eden-emu.dev/eden-emu/eden)'s shader
recompiler and suyu's decoders (see [NOTICE.md](../NOTICE.md)).

**No game files are in this repository.** Everything made from your copy goes to `work/`, which git ignores.

## Set up

Easiest: run `installer/install.ps1` once (it downloads the verified tools into `installer/cache` and builds a Python
environment). To work by hand you need Python 3.10+ with `lz4 numpy capstone unicorn zstandard pillow`, llvm-mingw, and
the tool bundles the installer fetches (`cpm`, `third_party`, `wgpu-native`; see `installer/install.ps1`). Run
everything from the repository root with `python -I`.

## Commands

| Command | What |
|---|---|
| `python -I nx2/make_game.py --nup <file.nup> --out <folder>` | the whole pipeline: check, unpack, lift, build, install |
| `python -I nx2/make_game.py --update <folder>` | rebuild an installed game from the sources (keeps saves and settings) |
| `python -I nx2/make_game.py --check --nup <file.nup>` | only check the file is the right title, update and build |
| `python -I nx2/build.py --standalone --jobs 4` | compile `work/sa/build` (`ocarune.exe`, `oot_vk.dll`, installed as `OcarinaOfTime.exe`) |
| `python -I tests/run_all.py` | all tests |
| `python -I scripts/prof.py <stacks file>` | profile sampled with `HOS_SAMPLE=<thread>` and `HOS_SAMPLE_FILE=<file>` |

`OOT_DUMP` (an extracted dump folder) and `OOT_WORK` (the work folder) override the defaults.

## Layout

| Path | What |
|---|---|
| `nx2/` | the lift pipeline, the build driver and their tests |
| `host/` | the Horizon runtime and the native NVN2 layer `nvn_native.c` |
| `gpu/vk/` | the Vulkan back end (`oot_vk.dll`), with Eden's shader recompiler and sirit |
| `gpu/render/`, `gpu/third_party/` | the WebGPU fallback renderer, its op stream, suyu's decoders |
| `installer/` | `install.ps1`, `package.ps1`, the setup wizard |
| `scripts/` | profiling and run helpers |
| `third_party/` | a64-kit (unmodified), libopus, zbic (unpacking only) |

## Debug switches

Environment variables, or lines in `oot_env.txt`: `HOS_SAMPLE`, `HOS_THREAD_STATS=1`, `HOS_INPUT_SCRIPT` (scripted
input), `HOS_HEADLESS`, `RX_VK_VRAM_MB`, `RX_VK_DEVICE`. Crash offsets map to lifted functions with `nx2/dllmap.py`.

License: GPL-3.0-or-later.
