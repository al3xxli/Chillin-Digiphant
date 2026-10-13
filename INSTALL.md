# Installation and recovery

Run commands from the repository root beside Assets, before opening Unity. Match ProjectSettings/ProjectVersion.txt and preserve package pins.

## Windows x64

Install Git, Unity Hub/6000.6.3f1, then run `./Setup-DigiPhant.ps1`. The script verifies the starter commit and elephant checksum manifest, preserves adapted student code, and rejects conflicting assets/environments rather than overwriting them. `./Setup-DigiPhant.ps1 -SkipPython` restores assets for keyboard-only play; full setup can run later.

## Manual macOS/Linux setup

These paths have not been verified on those platforms. Install Git, the same Unity editor, and compatible Python 3.12. From a fresh repository checkout:

```bash
git -c core.autocrlf=false clone https://github.com/kommanderpi/studentstarter.git DigiPhantStarter
git -C DigiPhantStarter checkout --detach 5adcbea1f7b311b8bb15c17b3dc4742ae8fff3af
cp -R DigiPhantStarter/Elephant Assets/Elephant
cp DigiPhantStarter/Elephant.meta Assets/Elephant.meta
python3.12 -m venv DigiPhantStarter/Tracking/.venv
DigiPhantStarter/Tracking/.venv/bin/python -m pip install -r DigiPhantStarter/Tracking/requirements.txt
DigiPhantStarter/Tracking/.venv/bin/python -m pip check
DigiPhantStarter/Tracking/.venv/bin/python Tracking/kart_landmarkers.py
DigiPhantStarter/Tracking/.venv/bin/python -m unittest discover -s Tracking -p 'test_*.py'
```

Preserve Elephant metadata and review the starter asset-checksums.json and terms. Do not copy starter DigiPhant over this repository's adapted Assets/DigiPhant. If assets already exist, inspect before copying. The launcher expects that exact environment path. Grant OS camera permission.

## Troubleshooting and verification

- Missing elephant: finish restoration and let Unity import; do not regenerate GUIDs.
- Setup failure: inspect the first error, network, pinned commit, and interpreter. Preserve incomplete environments for diagnosis before recreating.
- Missing hand model: run Tracking/kart_landmarkers.py with the installed environment Python.
- No camera: check index, OS permission, competing applications, and other bridge sessions using localhost 5055–5057.
- No movement: check role zones, P1 shoulders/wrists (plus hips in Full body tracking), P2 shoulders, and neutral calibration. Relax P3's fingers between thumbs-up jumps; open the hand before preparing a fresh pinch. Use Keys to isolate gameplay from tracking.
- Pink materials/compilation errors: match the editor and allow package restoration; inspect Console before changing pipeline.

Run pip check, Python tests, Unity compilation, and DigiPhant > Kart > Validate scene. Test keyboard driving, steering, pickup/throw, jump, crouch, race start, and pause in Game view. Then test real team gestures, calibration, mirror, simultaneous roles, and loss/recovery. A clean clone/import and physical test are necessary on each target setup.
