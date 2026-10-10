param([switch]$SkipPython)
$ErrorActionPreference = 'Stop'
$starterCommit = '5adcbea1f7b311b8bb15c17b3dc4742ae8fff3af'
$starterUrl = 'https://github.com/kommanderpi/studentstarter.git'
Set-Location $PSScriptRoot
if (!(Test-Path Assets) -or !(Test-Path ProjectSettings)) { throw 'Run from a Unity project.' }

if (!(Test-Path DigiPhantStarter)) {
    git -c core.autocrlf=false clone $starterUrl DigiPhantStarter
    if ($LASTEXITCODE) { throw 'Starter clone failed.' }
    git -C DigiPhantStarter checkout --detach $starterCommit
    if ($LASTEXITCODE) { throw 'Starter checkout failed.' }
}
$actualCommit = git -C DigiPhantStarter rev-parse HEAD
if ($LASTEXITCODE -or $actualCommit -ne $starterCommit) {
    throw "Existing starter differs from required commit $starterCommit. Reconcile it before proceeding."
}

# Check source bytes. Accept only CRLF conversion when it reproduces the exact
# published SHA-256; source changes are never silently overwritten.
$manifest = Get-Content DigiPhantStarter/asset-checksums.json -Raw | ConvertFrom-Json
foreach ($entry in $manifest.PSObject.Properties) {
    $source = Join-Path 'DigiPhantStarter' $entry.Name
    if (!(Test-Path -LiteralPath $source)) { throw "Missing supplied file: $source" }
    if ((Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLower() -ne $entry.Value) {
        $bytes = [IO.File]::ReadAllBytes((Join-Path $PWD $source))
        $lf = [Text.Encoding]::UTF8.GetBytes([Text.Encoding]::UTF8.GetString($bytes).Replace("`r`n", "`n"))
        $sha = [Security.Cryptography.SHA256]::Create()
        try { $digest = [BitConverter]::ToString($sha.ComputeHash($lf)).Replace('-', '').ToLower() }
        finally { $sha.Dispose() }
        if ($digest -ne $entry.Value) { throw "Supplied file differs: $source" }
        [IO.File]::WriteAllBytes((Join-Path $PWD $source), $lf)
    }
}

foreach ($name in @('Elephant', 'DigiPhant')) {
    if (!(Test-Path "Assets/$name") -and !(Test-Path "Assets/$name.meta")) {
        Copy-Item -LiteralPath "DigiPhantStarter/$name" -Destination "Assets/$name" -Recurse
        Copy-Item -LiteralPath "DigiPhantStarter/$name.meta" -Destination "Assets/$name.meta"
    } elseif (!(Test-Path "Assets/$name") -or !(Test-Path "Assets/$name.meta")) {
        throw "Incomplete Assets/$name import. Reconcile existing files before restoring."
    } else {
        Write-Host "Keeping existing Assets/$name (including any student adaptations)."
    }
}
foreach ($entry in $manifest.PSObject.Properties) {
    if ($entry.Name -eq 'Elephant.meta' -or $entry.Name.StartsWith('Elephant/')) {
        $destination = Join-Path Assets $entry.Name
        if (!(Test-Path -LiteralPath $destination) -or
            (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash.ToLower() -ne $entry.Value) {
            throw "Shared asset differs: $destination. Preserve and document adaptations before proceeding."
        }
    }
}
Write-Host 'Starter model and shared elephant assets verified.'
if ($SkipPython) { return }

$python = 'DigiPhantStarter/Tracking/.venv/Scripts/python.exe'
if (!(Test-Path $python)) {
    if (Test-Path DigiPhantStarter/Tracking/.venv) { throw 'An incomplete virtual environment exists; inspect it first.' }
    $uv = '.tools/uv/uv.exe'
    if (!(Test-Path $uv)) {
        New-Item -ItemType Directory -Path .tools -Force | Out-Null
        $ProgressPreference = 'SilentlyContinue'
        Invoke-WebRequest -Uri 'https://github.com/astral-sh/uv/releases/download/0.12.21/uv-x86_64-pc-windows-msvc.zip' -OutFile .tools/uv.zip
        Expand-Archive -LiteralPath .tools/uv.zip -DestinationPath .tools/uv -Force
    }
    & $uv python install 3.12.14 --install-dir .tools/python --no-bin
    if ($LASTEXITCODE) { throw 'Python installation failed.' }
    & ./.tools/python/cpython-3.12.14-windows-x86_64-none/python.exe -m venv DigiPhantStarter/Tracking/.venv
    if ($LASTEXITCODE) { throw 'Virtual environment creation failed.' }
}
& $python -m pip install -r DigiPhantStarter/Tracking/requirements.txt
if ($LASTEXITCODE) { throw 'Dependency installation failed.' }
& $python -m pip check
if ($LASTEXITCODE) { throw 'Dependency check failed.' }
& $python -c "import sys,cv2,mediapipe; print(sys.version); print('MediaPipe',mediapipe.__version__,'OpenCV',cv2.__version__)"
if ($LASTEXITCODE) { throw 'Python imports failed.' }
& $python Tracking/kart_landmarkers.py
if ($LASTEXITCODE) { throw 'Kart face/hand model installation failed.' }
& $python -m unittest discover -s DigiPhantStarter/Tracking -p 'test_*.py'
if ($LASTEXITCODE) { throw 'Tracking/encoder tests failed.' }
& $python -m unittest discover -s Tracking -p 'test_*.py'
if ($LASTEXITCODE) { throw 'Student screen-order tracking tests failed.' }
Write-Host 'Setup complete. Open Assets/StudentWork/Scenes/Digiphant-Kart.unity in Unity 6000.6.3f1.'

