# Installing Ocarune

## What the installer does

`Setup.exe` (a small WinForms program) runs `installer/install.ps1`, which:

1. Downloads the build tools once into a cache folder and checks each against a pinned SHA-256: the Vulkan headers and
   FFmpeg (`cpm`), SDL and the assembler helpers (`third_party`), the WebGPU fallback (`wgpu-native`) and
   [llvm-mingw](https://github.com/mstorsjo/llvm-mingw). Nothing is installed system-wide.
2. Creates a private Python virtual environment with `lz4 numpy capstone unicorn zstandard pillow`.
3. Checks your game with `nx2/make_game.py --check` (right title, update and build).
4. Builds the game into the folder you chose.

You need Python 3.10+ from python.org on your PATH (the Microsoft Store build does not work with `python -I`).

## Getting your game file

Ocarune needs **your own** dump of the Switch 2 *Ocarina of Time* update: title `0400486024DEC000`, version 131072,
build `e0f3ccfa`. Either the `.nup` package, or its extracted `exefs` and `romfs` folders. This project does not
provide it, explain how to get it, or accept it in issues.

## Command line

```powershell
# the full installer
powershell -ExecutionPolicy Bypass -File installer\install.ps1 -Nup "D:\game.nup" -Out "C:\Games\OoT"

# only check that your file is the right one
python -I nx2/make_game.py --check --nup "D:\game.nup"

# build with the script directly (tools already set up, see development.md)
python -I nx2/make_game.py --nup "D:\game.nup" --out "C:\Games\OoT" --language en-US
python -I nx2/make_game.py --romfs "D:\romfs" --exefs "D:\exefs" --out "C:\Games\OoT"
```

Useful options: `--jobs <n>` (how many files compile at once), `--log <file>` (also save the build output),
`--language <code>` (the game language; also editable later in `language.txt`).

## Packaging the setup wizard yourself

```powershell
cd installer\wizard
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe -nologo -target:winexe -win32icon:oot.ico -out:Setup.exe Setup.cs -r:System.Windows.Forms.dll -r:System.Drawing.dll -r:Microsoft.CSharp.dll
cd ..
powershell -File package.ps1     # makes installer\Ocarune-Setup (source + install.ps1 + Setup.exe)
```

Zip `Ocarune-Setup` to share it. It never contains the game, a `.nup` or any built files. Because Ocarune is GPL-3.0, the
source goes along with it.

## Where things go

| What | Where |
|---|---|
| The game | The folder you chose (default `C:\Games\OoT`), `OcarinaOfTime.exe` |
| Saves | `user\` in the game folder |
| Settings | `oot_options.ini` (the in-game menu writes it), `oot_env.txt` (advanced switches) |
| Last run log | `oot_log.txt` (the four runs before: `oot_log.1.txt` to `.4.txt`) |
| Mods | `mods\` in the game folder |
| Build cache | `cache\` beside the installer (about 4 GB; not needed to play) |

## Updating

Run the newest `Setup.exe` on the same folder. Saves, `oot_options.ini`, `oot_env.txt` and `mods` are kept; the game
and `image.bin` are rebuilt. The game files the first build read are kept in the game folder, so an update from a
clone can also run `python -I nx2/make_game.py --update "C:\Games\OoT"`.

## Uninstalling

Delete the game folder, the installer's `cache` folder and any shortcuts.
