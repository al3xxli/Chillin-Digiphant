# Chillin-Digiphant

DigiPhant Kart / Rainbow Savannah is a Unity elephant racing game with local webcam tracking for team play and keyboard controls for solo play.

## Download and install

1. Install Git, Unity Hub, and **Unity 6000.6.3f1**. Packages restore on import; the project pins **URP 17.6.0** and **Input System 1.20.0**.
2. Clone `https://github.com/al3xxli/Chillin-Digiphant.git`, or download and extract the GitHub ZIP.
3. Before opening Unity, run the following from the folder containing Assets, Packages, and ProjectSettings, using Windows PowerShell:

   ```powershell
   ./Setup-DigiPhant.ps1
   ```

   If execution is blocked: `powershell -NoProfile -ExecutionPolicy Bypass -File ./Setup-DigiPhant.ps1`.
   Setup restores the excluded elephant asset from pinned starter commit `5adcbea1f7b311b8bb15c17b3dc4742ae8fff3af`, installs Python 3.12.14, MediaPipe 0.10.35, OpenCV contrib 4.14.0.94, and the hand model. Internet is required for setup and package restore; inference runs locally afterward.
4. In Unity Hub choose **Add project from disk**, selecting this repository root. Wait for import and compilation.
5. Open `Assets/StudentWork/Scenes/Digiphant-Kart.unity`, press Play, choose **Keys**, focus the Game view, and press Enter for the three-lap race.

Windows x64 is the automated setup path. See [INSTALL.md](INSTALL.md) for manual macOS/Linux setup; those systems require target-machine verification. This delivery runs in the Unity Editor. Standalone builds need a separately launched Python bridge.

## Technical and physical requirements

- A computer capable of running this Unity/URP project, keyboard, and screen visible to performers. No Arduino, wearables, or additional controllers required.
- Camera mode needs a webcam, OS camera permission, and space for the group. Close competing camera apps. Local UDP ports **5055, 5056, 5057** must be available; run one camera session at a time.
- Use even front lighting and a securely placed camera facing the group. Adjust distance using the preview, rather than a fixed measurement: P1 must fit shoulders, wrists, hips, and both feet into the left zone; P2 shoulders fit the center; P3 hand fits the right.
- Leave room for hand raises and comfortable foot lifts without contacting people or equipment. Keep one foot planted: the default layout does not require physically jumping. Seated alternatives are available.

## Team camera controls

Choose **Camera**, three performers, and **Body + shoulders + hand**. Arrange **P1 / P2 / P3 from left to right in the displayed preview**. Camera selection, mirror, sensitivity, and alternatives are under Camera settings & diagnostics. Check orientation with a raised left hand. Mirror/layout/reassignment changes require new neutral calibration.

Choose **Start with Camera**. Hold feet planted, hands down, and shoulders relaxed during the ten-second neutral countdown; the race starts afterward. For practice, use Start Camera and Set Neutral separately. Follow missing-input guidance while waiting.

| Role | Action | Result |
| --- | --- | --- |
| P1: body, left | Raise LEFT / RIGHT hand | Accelerate / brake then reverse |
| P1 | Lift either foot, lower it before repeating | Jump |
| P2: shoulders, center | Raise LEFT / RIGHT shoulder | Turn right / turn left (default reversed steering) |
| P2 | Relax shoulders | Straighten |
| P3: hand, right | Fist / open hand | Crouch / stand when clear |
| P3 | Pinch thumb and index with other fingers extended, then release | Throw an orange |

Collect orange boxes before throwing. P2 may sit or stand. In three-person mode P3 needs only **one hand**, with wrist/fingers visible inside the hand zone; two hands are rejected. Tracking loss may hold driving or disable the affected action. See the [kart guide](Assets/StudentWork/Kart/README.md) for two/four-person layouts and seated Simple body alternatives.

## Solo keyboard controls

**Keys** stops the camera and needs no calibration. Focus the Game view.

| Key | Action |
| --- | --- |
| W/S or Up/Down | Accelerate / reverse |
| A/D or Left/Right | Steer |
| Shift | Full throttle |
| Space | Throw |
| E or J | Jump |
| Hold C or Ctrl | Duck |
| Enter | Start / restart |
| P or Escape | Pause / resume |
| R | Practice |
| Backspace | Recover to last checkpoint |
| Tab | Toggle sidebar |

Jump over logs, duck under the canopy, and follow slalom arrows.

## Setup and repository contents

- [Installation and troubleshooting](INSTALL.md)
- [Agent scene installation/setup](AGENT_SETUP.md) and [working rules](AGENTS.md)
- [Detailed kart guide](Assets/StudentWork/Kart/README.md)
- [Historical integration report](DIGIPHANT_SETUP.md): current README/source supersede its older controls.

Git includes authored scenes/scripts, required generated course assets, Unity metadata, Packages, ProjectSettings, tracking source/tests, and setup instructions. Caches, builds, logs, recordings, environments, downloaded models/assets, duplicate checkouts, and backup scenes stay local. Required Elephant assets restore from the [student starter](https://github.com/kommanderpi/studentstarter). Retain original third-party terms; this repository grants no new license for supplied assets/packages.

After import run **DigiPhant > Kart > Validate scene**, test Keys, then test the actual performers together in Game view. Automated checks do not establish physical gesture comfort, framing, or mirror correctness. Earlier games remain optional scenes.
