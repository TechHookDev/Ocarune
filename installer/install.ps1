# Ocarina of Time (Switch 2) PC port: our own installer.
# Run from the folder that holds this script and the `src` folder (see package.ps1):
#   powershell -ExecutionPolicy Bypass -File install.ps1 -Nup "D:\game.nup" -Out "C:\Games\OoT"
# Needs: Windows 10/11 x64, internet (tools are downloaded once and SHA-256 checked), ~6 GB free for tools + build.
param(
    [Parameter(Mandatory)][string]$Nup,
    [string]$Out = "C:\Games\OoT",
    [string]$Src,
    [string]$Cache,
    [switch]$NoBuild   # fetch and set up everything, skip the long game build
)
$ErrorActionPreference = "Stop"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $Src) { $Src = Join-Path $here "src" }
if (-not $Cache) { $Cache = Join-Path $here "cache" }
$ProgressPreference = "SilentlyContinue"

# name, sha256, url, folder in the zip that becomes $Src\<name>  (re-hosted by this project, SHA-256 checked)
$B = "https://github.com/TechHookDev/Ocarune/releases/download/tools-v1"
$parts = @(
  @{n="cpm";         h="bfd2206346ad0c2f85a136c719668647193f3fa0385f30580d1b501eafd84bdf"; u="$B/Ocarune-cpm-bfd2206346ad.zip";                from="Ocarune\tools\cpm"; s="cpm"},
  @{n="third_party"; h="bc1fc03536519dd02913d70d681576f209ec9c12092006fe9b6ac0233cf8b302"; u="$B/Ocarune-third_party-bc1fc0353651.zip";     from="Ocarune\tools\third_party"; s="third_party\sdl2\SDL2.dll"},
  @{n="wgpu-native"; h="c61896fa48ae653013cc8fd3ba54d383e785e00ead2411ca4958e2b6a96a5163"; u="$B/Ocarune-wgpu-native-c61896fa48ae.zip"; from="Ocarune\tools\wgpu-native"; s="wgpu-native\include"},
  @{n="llvm-mingw";  h="552191b5754c49a5a20723276a6fc56541ea1922c4c50b7b669022333fc1b40f"; u="https://github.com/mstorsjo/llvm-mingw/releases/download/20261104/llvm-mingw-20261104-ucrt-x86_64.zip"; from="llvm-mingw-20261104-ucrt-x86_64"; s="llvm-mingw\bin"}
)

if (-not (Test-Path -LiteralPath $Nup)) { throw "Game file not found: $Nup" }
if (-not (Test-Path (Join-Path $Src "nx2\make_game.py"))) { throw "Source not found in $Src (run package.ps1 first, or pass -Src)" }
New-Item -ItemType Directory -Force $Cache | Out-Null

$i = 0
foreach ($p in $parts) {
    Write-Host "@@ $([int](5 + 15 * $i++)) Getting $($p.n)"
    $dest = Join-Path $Src $p.n
    # (third_party and cpm may already exist in a git checkout, without the downloaded binaries: merge into them)
    if (Test-Path (Join-Path $Src $p.s)) { Write-Host "have $($p.n)"; continue }
    $zip = Join-Path $Cache (Split-Path $p.u -Leaf)
    if (-not ((Test-Path $zip) -and (Get-FileHash $zip -Algorithm SHA256).Hash -eq $p.h)) {
        Write-Host "downloading $($p.n) ..."
        Invoke-WebRequest $p.u -OutFile $zip
    }
    if ((Get-FileHash $zip -Algorithm SHA256).Hash -ne $p.h) { Remove-Item $zip; throw "hash mismatch for $($p.n): refusing to use it" }
    $tmp = Join-Path $Cache ("x-" + $p.n)   # each archive in its own empty folder
    if (Test-Path $tmp) { Remove-Item -Recurse -Force $tmp }
    Expand-Archive $zip $tmp
    if (Test-Path $dest) { Copy-Item (Join-Path (Join-Path $tmp $p.from) "*") $dest -Recurse -Force } else { Move-Item (Join-Path $tmp $p.from) $dest }
    Remove-Item -Recurse -Force $tmp
}

Write-Host "@@ 65 Preparing Python"
# Python with the build's packages in its own venv (python -I can't see user-site packages)
$venv = Join-Path $Cache "venv"
$py = Join-Path $venv "Scripts\python.exe"
if (-not (Test-Path $py)) {
    $sys = Get-Command py -ErrorAction SilentlyContinue; if (-not $sys) { $sys = Get-Command python -ErrorAction SilentlyContinue }
    if (-not $sys) { throw "Python 3.10+ not found. Install it from python.org (tick 'Add to PATH') and run this again." }
    & $sys.Source $(if ($sys.Name -like "py*") { "-3" }) -m venv $venv
    & $py -m pip install --quiet lz4 numpy capstone "unicorn>=2.1,<2.2" zstandard pillow
}

Write-Host "@@ 72 Checking your game file"
$game = Join-Path $Src "nx2\make_game.py"
# the downloaded tool folders, as the build looks for them (without wgpu-native it would build with no renderer)
$env:WGPU_NATIVE = Join-Path $Src "wgpu-native"
$env:RX_CPM = Join-Path $Src "cpm"
Push-Location $Src
try {
    & $py -I $game --check --nup $Nup
    if ($LASTEXITCODE) { throw "That file is not the Ocarina of Time Switch 2 package this port needs." }
    if ($NoBuild) { Write-Host "Setup done. Build skipped (-NoBuild)."; return }
    Write-Host "@@ 78 Building the game (this takes a while)"
    & $py -I $game --nup $Nup --out $Out --work (Join-Path $Cache "work")
    if ($LASTEXITCODE) { throw "Build failed (see output above)." }
} finally { Pop-Location }
Write-Host "@@ 100 Done"
Write-Host "Done. Start $Out\OcarinaOfTime.exe  (Insert or F1 = settings)"
