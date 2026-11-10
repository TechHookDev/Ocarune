# Troubleshooting

Always start with `oot_log.txt` in the game folder: it holds the last run (the four before are `oot_log.1.txt` to `.4.txt`).
Its first line is the version.

| Problem | What to do |
|---|---|
| "That file is not the Ocarina of Time Switch 2 package" / "unsupported build" | You need the Switch 2 title `0400486024DEC000` **with its update** (v131072, build `e0f3ccfa`) |
| The installer says Python was not found | Install Python 3.10+ from python.org, tick *Add python.exe to PATH*, run Setup again. Not the Microsoft Store build |
| A download fails or "hash mismatch" | Retry. The installer refuses any file that does not match its pinned SHA-256; do not bypass it |
| Antivirus removed part of the build tools | False alarm on unsigned compilers. Restore the files from Windows Security protection history or exclude the installer's `cache` folder, then run Setup again |
| The build fails | Press **Show details** in the wizard and open an issue with the last 50 lines. Low disk space (20 GB needed) is the usual cause |
| Opens and closes, or the log says "device lost" | Update your graphics driver. Try *Robust buffers: On* and a lower Quality preset |
| Parts of the picture are missing, or a message about mesh shaders | The card or driver has no `VK_EXT_mesh_shader` (older than GTX 16 / RX 6000). Update the driver; older cards are not supported |
| Black screen at start | The first start prepares shaders and can take several minutes. If the log stops growing for 10 minutes, report it |
| Low fps | Check the FPS counter: lower **Resolution** to 67% or 50%, pick a Quality preset for your card, make sure **GPU device** is your dedicated GPU. Castle Town is CPU-heavy |
| Freezes in big scenes (0 fps) | Lower `RX_VK_VRAM_MB` in `oot_env.txt` (5120 suits 12 GB cards; use 3500 on 8 GB) |
| Black or purple squares on trees or sky | Keep `EDEN_NX2_GM_SPLIT=1` and `RX_VK_KEEP_FINALIZED_MB=2048` in `oot_env.txt`; report where it happens |
| Stutter the first time you enter an area | Normal: shaders are being prepared. The next visit is smooth |
| Picture too dark, bright or colourful | Picture tab: Brightness, Contrast, Saturation, Warmth |
| Wrong language | Edit the code in `language.txt` in the game folder |
| Controller not detected | Plug it in before starting; enable *Controller in background* if you alt-tab |

## Reporting a problem

[Open an issue](https://github.com/TechHookDev/Hylia/issues/new/choose) with the Hylia version, your GPU and driver,
where in the game it happens, your `oot_options.ini`, and `oot_log.txt`. **Never attach game files, keys or saves.**
