# Makes the shareable setup folder: the source (without downloaded tools) + install.ps1 + Setup.exe.
# Never includes the game, a .nup, or built files. GPL-3.0: the source must go along with any build you share.
# Build Setup.exe first:  see wizard\Setup.cs (csc line at the top).
param([string]$Src = (Join-Path $PSScriptRoot ".."), [string]$Out = (Join-Path $PSScriptRoot "Ocarune-Setup"))
$ErrorActionPreference = "Stop"
robocopy $Src (Join-Path $Out "src") /E /XD (Join-Path $Src "llvm-mingw") (Join-Path $Src "cpm") (Join-Path $Src "wgpu-native") (Join-Path $Src ".git") (Join-Path $Src "installer") (Join-Path $Src "work") /NFL /NDL /NJH /NJS | Out-Null
if ($LASTEXITCODE -ge 8) { throw "copy failed ($LASTEXITCODE)" }
Copy-Item (Join-Path $PSScriptRoot "install.ps1") $Out
Copy-Item (Join-Path $PSScriptRoot "wizard/Setup.exe") $Out
Copy-Item (Join-Path $PSScriptRoot "wizard/oot.ico") $Out
"Done: $Out  (zip it; they double-click Setup.exe)"
