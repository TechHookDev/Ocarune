# Architecture

Ocarune turns the Switch 2 *Ocarina of Time* into a native Windows program, once, on your PC. Nothing is emulated at run
time.

```
 your .nup  ->  nx2/nup.py        unpack the package (exefs + romfs)
            ->  nx2/nso2.py       decompress the game's modules
            ->  nx2/image.py      lay them out in memory, relocate, bind imports
            ->  nx2/lift.py       AArch64 -> C, one function at a time (third_party/a64-kit)
            ->  nx2/build.py      compile with clang (llvm-mingw) + host/ into OcarinaOfTime.exe and oot_vk.dll
```

## The pieces

- **The lift** (`nx2/`, `third_party/a64-kit`): every guest function becomes a C function; loops get poll points so
  threads can be scheduled. `nx2/insn.py` covers the Switch 2 instruction forms, `lift_fixes.py` and `oot_patches.py`
  hold the game-specific fixes. `nx2/test_insn.py` and `test_leaf.py` compare lifted code against Unicorn.
- **The runtime** (`host/`): replaces the console's operating system at the `nn::` boundary: threads, memory, SVCs,
  file system, input, audio, window. The rest of the SDK runs as lifted code. Memory is one large reservation with the
  image at `0x80000000`.
- **NVN2** (`host/nvn_native.c`): the game's graphics API implemented in place over guest memory. Command buffers
  record an op stream (`gpu/render/rx_nvn.h`).
- **The Vulkan back end** (`gpu/vk/`, built as `oot_vk.dll`): replays the op stream on Vulkan 1.3. Switch 2 shader
  programs go through Eden's recompiler to SPIR-V; task/mesh programs use `VK_EXT_mesh_shader`. A WebGPU renderer
  (`gpu/render/`) stays in the exe as a fallback when Vulkan is missing.
- **Post pass** (`gpu/vk/shaders/`, `vk_framegen.cpp`): FSR upscaling (EASU + RCAS), frame generation, the picture
  controls. **Menu** (`vk_menu.cpp`): the in-game settings; defaults in `struct Options` in `vk_menu.h`.
- **The installer** (`installer/`): `install.ps1` fetches and verifies the tools and runs `nx2/make_game.py`;
  `wizard/Setup.cs` is its window.

## Rules that keep it correct

1. Nothing the replay reads comes from live guest memory; it reads submit-time captures (`gpu/vk/vk_memory.cpp`).
2. Cross-queue events are honoured: each submission replays as far as its waits allow (`vk_replay.cpp`).
3. Texture and sampler registrations are applied in order with the submits.
4. GPU timestamps drive the game's dynamic resolution, so the fake GPU clock must be scaled to the PC.
5. The CPU saturates first. Thread counts are measured, never raised by default (`HOS_VCORES`, `HOS_THREAD_STATS=1`,
   `HOS_SAMPLE` with `scripts/prof.py`).
6. Nothing derived from the game is committed: `work/`, `*.bin`, `*.nup`, keys and builds are git-ignored.

## Testing

`python -I tests/run_all.py` runs the whole suite (static checks, unit tests, and the Vulkan tests when a device and
compiler are present; the rest are skipped with the reason shown). `nx2/test_dll.py`, `test_insn.py` and `test_leaf.py`
need a full build and Unicorn.
