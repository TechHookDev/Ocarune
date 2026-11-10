<h1 align="center">
  Hylia
  <br>
  <sub>Ocarina of Time, native on your PC</sub>
</h1>

<p align="center">
  <b>The Legend of Zelda: Ocarina of Time (Nintendo Switch 2) running as a real Windows program: no emulator,
  native 2K, frame generation and FSR upscaling, tuned out of the box.</b>
</p>

<p align="center">
  <a href="#why-hylia">Why Hylia</a> ·
  <a href="#requirements">Requirements</a> ·
  <a href="#install">Install</a> ·
  <a href="#out-of-the-box-settings">Settings</a> ·
  <a href="#controls">Controls</a> ·
  <a href="docs/troubleshooting.md">Troubleshooting</a> ·
  <a href="#how-it-works">How it works</a> ·
  <a href="#credits">Credits</a>
</p>

> [!IMPORTANT]
> This repository contains **no game, no keys and no executable**, and it does not tell you how to get them.
> You need your own copy of the game (a `.nup` package, or its extracted `exefs` and `romfs`). Everything Hylia makes
> comes from your copy, is made on your PC and stays on it. Never share those files.

## Why Hylia

Hylia is a **static recompilation**. Your copy of the game is translated to C once, compiled into a Windows program, the
console's system software is reimplemented, and the Switch 2 graphics calls are replayed on Vulkan. Nothing is emulated
at run time, so the game runs at the speed of your hardware.

- **Native 2K by default.** The game's whole frame is rendered at 2560x1440; 4K is one setting away.
- **Smooth.** Frame generation (2x by default) with even frame pacing, aiming at a steady 60 fps or more.
- **Sharp.** FSR upscaling (EASU + RCAS) to your window, with adjustable sharpness.
- **Your picture, your way.** Brightness, contrast, saturation and warmth, live in the menu.
- **Keyboard, mouse and controller.** Native mouse camera, full remapping, Nintendo or Xbox layouts.
- **A real in-game menu** (press <kbd>Insert</kbd> or <kbd>F1</kbd>) with quality presets for 4 GB to 16 GB cards.
- **A proper installer.** `Setup.exe` downloads the open build tools, checks each one against a known SHA-256,
  builds the game from your file and creates the shortcut.
- **Mods.** Drop RomFS replacements into a `mods` folder.

## Requirements

| | |
|---|---|
| OS | Windows 10 or 11, 64-bit |
| GPU | Vulkan 1.3 with **mesh shader** support (NVIDIA GTX 16 / RTX series, AMD RX 6000 and newer, recent Intel Arc). 8 GB of video memory or more is recommended |
| CPU / RAM | A modern 6-core CPU and 16 GB of RAM or more. The build compiles for 10 to 45 minutes |
| Disk | About 20 GB free while building; the game itself is a few GB |
| Python | [Python 3.10 or newer from python.org](https://www.python.org/downloads/) with *Add python.exe to PATH* ticked (not the Microsoft Store build) |
| Internet | Only for the one-time download of the build tools |
| Game | Your own Ocarina of Time Switch 2 `.nup`: title `0400486024DEC000`, update version 131072, build `e0f3ccfa` |

## Install

### The easy way: the setup wizard

1. Download `Hylia-Setup.zip` from the [latest release](https://github.com/TechHookDev/Hylia/releases/latest) and
   unzip it anywhere.
2. Run **`Setup.exe`**.
3. **Game file:** pick your `.nup`. The wizard checks it is the right version before it starts.
4. **Install to:** keep `C:\Games\OoT` or choose another folder.
5. Press **Install** and let it work. It shows four stages: build tools, Python, game check, build. The build is the
   long one (roughly 10 to 30 minutes). **Show details** shows the live log.
6. When it says **You're ready**, leave *Start the game now* ticked.

The first start prepares the game's shaders once and can take several minutes. After that it starts fast.

### From the command line

```powershell
powershell -ExecutionPolicy Bypass -File installer\install.ps1 -Nup "D:\game.nup" -Out "C:\Games\OoT"
```

The script downloads and verifies the tools, creates a private Python environment, checks your game and builds it.
Add `-NoBuild` to set everything up without the long build. To drive the build yourself, see
[docs/install.md](docs/install.md).

### Updating

Download the newest release and run the wizard again on the same folder. Your saves (`user\`), your settings
(`oot_options.ini`, `oot_env.txt`) and your mods are kept. See [docs/install.md](docs/install.md#updating).

## Out-of-the-box settings

Hylia ships with the settings we tuned for the best result on a mid-to-high-end card (developed on an RTX 5070). You do
not have to touch anything:

| Setting | Default | What it does |
|---|---|---|
| Output resolution | **2560x1440** | The game's whole frame, HUD included |
| Resolution (3D) | **75%** | Share of the output the 3D is drawn at, then upscaled |
| Upscaling | **FSR**, sharpness **60%** | Sharper picture at a lower render cost |
| Frame generation | **2x**, frame pacing **On** | Smooth, even frames |
| Fullscreen | **On** | Borderless at your monitor's size (<kbd>F11</kbd>) |
| FPS counter | **On** | Top right; also shows the game's own rate |
| Saturation / Warmth | **110% / +5** | A touch more colour and warmth than the console |
| Brightness / Contrast | 100% / 100% | Neutral |
| Robust buffers | **On** | A bad GPU read is dropped instead of crashing the GPU |
| Texture memory cap | **5 GB** | Keeps big scenes (Castle Town) from filling the card |
| Foliage fix | **On** | Trees and grass drawn right, no black blocks |

Everything is changed live in the menu. Full reference: [docs/settings.md](docs/settings.md). On a 4 GB to 8 GB card,
pick a **Quality preset** in the Graphics tab.

## Controls

Press <kbd>Insert</kbd> or <kbd>F1</kbd> (Home or Select + Start on a controller) for the menu; <kbd>Esc</kbd> closes it.

| | Keyboard and mouse | Controller |
|---|---|---|
| Move | <kbd>W</kbd> <kbd>A</kbd> <kbd>S</kbd> <kbd>D</kbd> | Left stick |
| Camera | Arrow keys or <kbd>I</kbd> <kbd>J</kbd> <kbd>K</kbd> <kbd>L</kbd>, or the mouse | Right stick |
| Menu | <kbd>Insert</kbd> / <kbd>F1</kbd> | Home, or Select + Start |
| Fullscreen | <kbd>F11</kbd> | |

All buttons can be remapped in **Controls → Controls**. Layouts: Nintendo, Xbox / PC, Swap Face Buttons.

## Troubleshooting

The log of your last run is `oot_log.txt` in the game folder. The common problems (black screen, "device lost",
missing mesh shaders, antivirus, low fps) are in [docs/troubleshooting.md](docs/troubleshooting.md).

## How it works

1. `nx2/lift.py` translates the game's AArch64 code to C.
2. `nx2/build.py` compiles that C together with `host/`, which plays the part of the console's operating system.
3. `host/nvn_native.c` records the game's NVN2 graphics calls and `gpu/vk/` replays them on Vulkan.
4. Switch 2 shaders are lowered and translated to SPIR-V by Eden's shader recompiler.
5. A post pass applies upscaling, frame generation and the picture controls.

Developer docs: [docs/development.md](docs/development.md) and [docs/design.md](docs/design.md).
Contributing: [CONTRIBUTING.md](CONTRIBUTING.md).

## Credits

Hylia is inspired by, and builds on the open work of, many people:

- The static-recompilation approach and the **a64-kit** tools come from the open TOTKPort / mkw-recomp line of work
  by **NotRexed** (see `third_party/a64-kit`).
- **[Eden](https://git.eden-emu.dev/eden-emu/eden)**: the shader recompiler and Switch 2 shader work. **suyu**: the
  graphics decoders and the AArch64 translator. **Mesa (NAK)**: Ampere instruction encoding reference.
- **EvilOOT, Orchid and Lichii**: the culling, fog, exposure and compare-exchange fixes for Ocarina of Time.
- **[mstorsjo/llvm-mingw](https://github.com/mstorsjo/llvm-mingw)**: the Windows compiler the installer downloads.
- **[kinnay/zbic](https://github.com/kinnay/zbic)**, **Opus**, **SDL** and **AMD FidelityFX (EASU/RCAS)**:
  decompression, audio, input and upscaling.

Full third-party list: [NOTICE.md](NOTICE.md).

## License

Hylia is free software under the [GNU General Public License v3.0 or later](LICENSE). If you share a build, share its
source with it. Third-party parts keep their own notices in their folders.

Hylia is an unofficial fan project, not affiliated with or endorsed by Nintendo. The Legend of Zelda and Ocarina of
Time are trademarks of Nintendo. Use it only with a copy of the game you own.
