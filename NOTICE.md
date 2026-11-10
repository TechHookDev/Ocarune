# Third-party notices

Hylia is GPL-3.0-or-later. It includes or builds on the following; each keeps its own license and notices.

| Project | Used for | License |
|---|---|---|
| a64-kit (NotRexed) | AArch64 to C translation tools (`third_party/a64-kit`, unmodified) | see `third_party/a64-kit/NOTICE.TOTKPort.md` |
| Eden | shader recompiler, Switch 2 shader support | GPL-3.0 |
| suyu | graphics decoders, AArch64 translator | GPL-2.0-or-later |
| Sirit | SPIR-V emitter | see its folder |
| Vulkan Memory Allocator | GPU memory | MIT |
| SDL 2 / SDL 3 | window, input | zlib |
| Opus | audio | BSD-3-Clause |
| zbic (kinnay) | ZBIC decompression | see `third_party/zbic/LICENSE` |
| stb | font rasterising | public domain / MIT |
| AMD FidelityFX (EASU, RCAS) | upscaling shaders | MIT |
| llvm-mingw (mstorsjo) | downloaded by the installer, not redistributed | MIT / Apache-2.0 with LLVM exception |

Provenance of imported code is recorded in `host/PROVENANCE.md`, `gpu/PROVENANCE.md` and
`third_party/a64-kit/PROVENANCE.md`. Credit also goes to the EvilOOT, Orchid and Lichii projects for Ocarina of Time
fixes referenced in code comments.
