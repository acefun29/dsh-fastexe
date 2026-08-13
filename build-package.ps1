# Assemble the full DeepSeekHarness portable package in the script's directory:
#   node/   portable Node.js LTS runtime
#   harness DeepSeek Harness source + deps + build output
#   dsh.exe launcher (compiled from launcher.cs)
# Prereqs: git, curl (built into Windows 10+), internet. Runs on PowerShell 5.1+.
$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
$nodeDir = Join-Path $here 'node'
$harnessDir = Join-Path $here 'harness'
$nodeExe = Join-Path $nodeDir 'node.exe'

$UPSTREAM = 'https://github.com/deepseek-ai/deepseek-harness.git'

function Step([string]$msg) { Write-Host "`n==> $msg" -ForegroundColor Cyan }

# ---- 1. Portable Node.js LTS ------------------------------------------------
if (-not (Test-Path $nodeExe)) {
    Step 'Downloading latest Node.js LTS (win-x64)'
    $index = Invoke-RestMethod 'https://nodejs.org/dist/index.json'
    $lts = $index | Where-Object { $_.lts } | Select-Object -First 1
    $ver = $lts.version
    Write-Host "Node $ver"
    $zip = Join-Path $env:TEMP "node-$ver.zip"
    Invoke-WebRequest "https://nodejs.org/dist/$ver/node-$ver-win-x64.zip" -OutFile $zip
    New-Item -ItemType Directory -Force -Path $nodeDir | Out-Null
    tar -xf $zip -C $nodeDir --strip-components=1
    if (-not (Test-Path $nodeExe)) { throw 'node.exe missing after extraction' }
    Remove-Item $zip -Force
} else {
    Write-Host "node/ exists, skipping download."
}

# ---- 2. Harness source ------------------------------------------------------
if (-not (Test-Path (Join-Path $harnessDir 'package.json'))) {
    Step 'Cloning DeepSeek Harness (shallow)'
    git clone --depth 1 $UPSTREAM $harnessDir
    if ($LASTEXITCODE -ne 0) { throw 'git clone failed' }
} else {
    Write-Host "harness/ exists, skipping clone."
}

# ---- 3. Dependencies + build -----------------------------------------------
Step 'Installing dependencies (pnpm, this takes several minutes)'
Push-Location $harnessDir
try {
    $env:PATH = "$nodeDir;$env:PATH"
    corepack pnpm install
    if ($LASTEXITCODE -ne 0) { throw 'pnpm install failed' }

    Step 'Building (lib + web frontend)'
    corepack pnpm run build
    if ($LASTEXITCODE -ne 0) { throw 'pnpm build failed' }
} finally {
    Pop-Location
}

# ---- 4. Launcher ------------------------------------------------------------
Step 'Compiling launcher dsh.exe'
& "$here\build-launcher.ps1"

Step 'Done.'
Write-Host "Portable package ready: $here (about 1.3 GB)"
Write-Host 'Double-click dsh.exe to start. See README.md for API-key setup.'
