# Settings reference

Open the menu with <kbd>Insert</kbd> or <kbd>F1</kbd> (Home, or Select + Start, on a controller). <kbd>Q</kbd>/<kbd>E</kbd>
or <kbd>Tab</kbd> switch tabs, <kbd>Up</kbd>/<kbd>Down</kbd> choose a row, <kbd>Left</kbd>/<kbd>Right</kbd> change it,
<kbd>Esc</kbd> closes. Choices are saved in `oot_options.ini` in the game folder. Rows marked *restart* apply at the next
start (**Apply and restart** in the System tab does it for you).

Defaults are what a new install gets (they live in `gpu/vk/vk_menu.h`, `struct Options`).

## Graphics

| Option | Default | Notes |
|---|---|---|
| Quality preset | Custom | Minimum (4GB), Low (6GB), Balanced (8GB), High (12GB), Ultra (16GB+) set resolution, upscaling, sharpness, frame generation and the video-memory limits together. Changing most other rows sets it back to Custom |
| Frame generation | 2x | Off, or 2x to 8x extra frames between the game's own. Adds a little input delay |
| Frame pacing | On | Shows frames at an even interval |
| Resolution | 75% | Auto (the game's dynamic resolution), 100%, 75%, 67%, 50% of the output size |
| Output resolution | 2560x1440 | The game's whole frame: 2048x1152, 2560x1440 or 3840x2160 (*restart*) |
| Upscaling | FSR | Off (plain copy) or FSR (EASU + RCAS) |
| Sharpness | 60% | RCAS strength, 0 to 100 |

## Picture

| Option | Default | Range |
|---|---|---|
| Brightness | 100% | 50 to 150 |
| Contrast | 100% | 50 to 150 |
| Saturation | 110% | 0 to 200 |
| Warmth | +5 | -50 (cool) to +50 (warm) |

## Gameplay

| Option | Default | Notes |
|---|---|---|
| FPS counter | On | Top right; with frame generation it also shows the game's own rate |
| Game speed | Real time | *Per frame* slows the game whenever it is under 60 fps |
| Text speed | Normal | 2x, 4x or instant dialogue |
| Cheats | | Heart protection, flight, teleport, items, time (opens its own page) |

## Controls

| Option | Default | Notes |
|---|---|---|
| Controller in background | Off | Controller keeps working in another window |
| Mouse camera | As the stick | *Native* turns the camera one to one |
| Mouse sensitivity | 1.00 | <kbd>-</kbd> and <kbd>=</kbd> change it in game |
| Invert mouse vertical / camera X / camera Y | Off | |
| Layout preset | Nintendo | Nintendo, Xbox / PC, Swap Face, Custom |
| Controls | | Remap every gamepad button and key |

Writing bindings by hand in `oot_options.ini`: `pad_<button>=<index>` (0 bottom face, 1 right, 2 left, 3 top, 4 Select,
5 Home, 6 Start, 7/8 stick presses, 9/10 shoulders, 11-14 D-pad, 15/16 triggers, -1 none) and
`key_<button>=<vk>,<vk>` (Windows virtual-key codes in decimal, 0 none). Buttons: `a b x y l r zl zr plus minus ls rs
left up right down`; keys also `move_up move_down move_left move_right cam_up cam_down cam_left cam_right`.

## Audio

Master volume (100%) and Mute when unfocused (Off).

## System

| Option | Default | Notes |
|---|---|---|
| Fullscreen | On | Borderless; <kbd>F11</kbd> too |
| GPU device | Auto | Pick a card on a PC with more than one (*restart*) |
| VSync | Auto | Auto, VSync, or Uncapped (*restart*) |
| Handheld profile | Off | Runs as the undocked console: lower resolution (*restart*) |
| Ultrawide | 16:9 | Camera at the monitor's shape (*restart*) |
| Robust buffers | On | A bad GPU read is dropped instead of crashing (*restart*) |

## Advanced switches: `oot_env.txt`

One `NAME=VALUE` per line, read at start. A fresh install writes:

```
EDEN_NX2_GM_SPLIT=1           # foliage drawn right (no floating trees or black blocks)
RX_VK_IMAGE_BLOCK_MB=32       # smaller image-memory blocks: less wasted video memory
RX_VK_VRAM_MB=5120            # texture memory cap: keeps big scenes from filling the card
RX_VK_KEEP_FINALIZED_MB=2048  # keep textures the GPU drew once instead of evicting them
```

(Put comments on their own lines in the real file.) Raise `RX_VK_VRAM_MB` on a 16 GB card, lower it on 8 GB.
`RX_VK_DEVICE=<n>` forces a GPU for one run; `HOS_OUTPUT_RES` and `HOS_TEXT_SPEED` override the menu. Delete a line to
go back to the default.
