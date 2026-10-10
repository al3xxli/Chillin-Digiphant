# DigiPhant setup

## Unity-controlled mirror preview (2026-10-05)

This replaces the earlier Python-only mirror changes, which the user reported did
not correct their live preview. The Peel Patrol bridge now sends raw JPEG pixels
and raw skeleton coordinates (preview stream version 2). Unity applies the final
horizontal flip directly when drawing the picture. Native Unity text stays readable.
Older preview streams are rejected with a restart message instead of being labeled
as mirrored. The original reference scene keeps its existing preview behavior.

Stop Play, allow scripts to reload, then start the camera again. Below the picture,
**Flip camera image (mirror adjustment)** visibly flips the displayed image
immediately; the choice is saved per camera index. Raise your own left hand: it
should appear on the left of your image, under the fixed L guide. Adjust the checkbox
if your camera already mirrors its output. Set a fresh neutral pose afterward.
Changing orientation clears calibration and requests fresh player assignments.
The bridge assigns P1/P2/P3 using the final display coordinates and acknowledges
the orientation before Unity accepts its new pose data. Inference uses original
camera frames and retains anatomical hand channels.

Validation: 17 student Python tests pass, covering raw source pixels, live flip
commands, displayed zone assignments and anatomical hand inputs. Runtime and Editor
assemblies compile against the installed Unity references. Live camera appearance
and three-person physical controls still require a local playtest.

## Compact indicator, independent throwing, L/R guides (2026-10-05)

The floating indicator is reduced to 136 x 34 UI pixels. Its lower edge is placed
above the projected eight-corner bounds of the elephant meshes and active orange
cargo, rather than a world-height offset that could overlap a tilted camera view.
It hides at the top edge if there is no clear space instead of clamping onto the
model. Each red camera third now labels the player's anatomical L/R, in Unity and
in the Python JPEG overlay.

Throwing now reads/calibrates P3's left hand independently; a hidden idle right wrist
cannot block it. The hidden hands-down rearm requirement is removed: after a dropout,
a fresh stable 0.2-second held raise resumes firing. A deliberate right raise inside
a station takes priority; small incidental right-hand movement does not cancel throws.
The repeating gesture only starts its cooldown when a shot is accepted by the game,
so another cooldown cannot consume an attempted throw and postpone the next attempt.
Movement and steering still run concurrently with action processing.

Practice now supports actual projectiles, ammo spending, and orange-grove reloads
without advancing the waves. Building and cleaning remain round actions. The guide
and on-screen prompts reflect this. Runtime/Editor compilation, 15 Python checks,
and executable C# gesture checks pass. L/R JPEG labels were visually inspected;
Unity integration scenarios compile but were not run, and live UI/control needs testing.

## Enemy visibility, floating inventory, automatic fire (2026-10-05)

Bananas spawn at 1.7 times their previous scale, retaining the additional wave-three
size boost. Their speed, health and spawn timing are unchanged. Projectile targeting
and hit bursts follow the enlarged body's bounds center. A screen-space panel follows
above the elephant's head/mesh bounds, with current ammo, ten inventory markers and
ready/auto/empty/cooldown/station/pause/tracking status. It respects the game camera
viewport so it stays clear of the MediaPipe sidebar.

P3 now holds the left hand raised to throw once per second after the initial 0.2
second raise. Lowering stops it; tracking loss, both-hands conflict, pause and long
frame gaps clear the firing gesture. No catch-up shots are emitted. A one-second
throw cooldown also prevents rapid re-raise bypass. Right-hand station use remains
one action per raise, with release required between uses.

Runtime/Editor compilation and executable C# cadence/gesture checks pass, including
10/20/30/60 Hz inputs. Updated Unity scenarios compile but were not executed in Play
mode; live HUD placement, larger enemy visibility and physical control need testing.

## Both hands lowered / left and right raises (2026-10-05)

This supersedes the earlier lean, swing and forward-reach mappings below. Calibrate
with both hands lowered beside the body and wrists visible. P1 raises left for
forward, right for reverse. P2 raises left to turn left, right to turn right. Both
movement channels run together: the elephant can steer while driving. Each channel
sums opposing positive hand raises, without halving strength or interpreting a
below-neutral hand as an opposite command. Equal raises cancel on that driver's axis.
Mappings are applied on game startup/group changes and stored in the scene defaults.

P3 raises left for one throw (0.2-second hold) and right for station use (0.35 seconds):
reload at the grove, build at the workshop, clean at the filter. Lower between uses;
both action hands raised cancel and require release. Actions only need height data,
not depth/forward-hand fields. P3's neutral hand-height validity is recorded separately
so an occluded hand during calibration cannot become an unintended action baseline.
Movement/action tracking separation and brief driving dropout grace remain intact.

The on-screen hints and [canonical guide](Assets/StudentWork/PeelPatrol/README.md)
now match this scheme. Runtime and Editor compilation plus executable C# action-state
checks pass. Updated Unity scenarios include combined travel/steering and reverse;
Play-mode execution and live physical controls remain unverified.
An isolated executable using the actual driver/mapping methods with simulated input
passes both travel directions combined with both steering directions, hands-down
neutral, equal-raise cancellation and invalid-input/group-mapping checks.

## Bright-red guides in Unity (2026-10-05)

The shared Game/sidebar and dockable panel now draw thick red thirds directly in
Unity, with a red frame and large white P1/P2/P3 labels. They appear in 3 / Team
mode even without a live frame, and follow the ScaleToFit image rectangle when
letterboxed. The Python preview also uses thick red dividers. This removes the
Unity overlay's dependency on restarting the Python process or preserving thin
lines through JPEG compression. Reload Unity scripts; restart the camera to load
the latest assignment code. Runtime/Editor compilation and 15 student Python tests
pass. The encoded Python overlay was visually checked on a synthetic image; the
live Unity display has not been verified.

## Fixed thirds and visible boundaries (2026-10-05)

Peel Patrol's three-person mode now assigns by fixed selfie-preview thirds:
**left P1 / center P2 / right P3**. `Tracking/team_zones.py` supplies both assignment
and the visual guide. Boundary lines, role labels and per-zone TRACKED / STEP INTO
VIEW / ONE PERSON ONLY status are drawn after the selfie flip and before JPEG
encoding, so both Unity panels receive the same guides. Each zone can acquire its
player independently from startup, without first detecting all three together.
Two eligible detections in one zone hold only that role. Assignment uses shoulder
midpoint in upper-body mode and hip midpoint in full-body mode; hand crossing alone
does not change roles. The detector still uses the full camera image.

This supersedes the remembered-lane assignment described below. Restart the camera,
stand one torso near each zone's center, and recalibrate. Existing movement grace
and strict action validity remain unchanged. Fifteen student Python tests and eight
instructor regressions pass; runtime and Editor assemblies compile. The encoded
320 x 240 overlay was visually inspected on a synthetic image. Live Unity/camera
and physical three-person control still need testing.

## Detection dropout recovery (2026-10-05)

The earlier exact-three assignment rule amplified one lost pose into all roles being
unassigned. It is replaced by current screen order when three separated people are
visible, with remembered horizontal lanes for partial/extra detections. Only clear
matches are sent; ambiguous or missing lanes never receive fabricated poses.
Reassign clears lanes. Keep your places and recalibrate after swapping.

Peel Patrol locomotion retains the last confident driving input for at most 0.25
seconds after a dropped/low-confidence observation. Invalid samples do not refresh
that deadline. P3 actions and calibration require current confident inputs; action
poses are never replayed through gaps. The original reference scene retains strict
movement reads. The preview identifies which role is being reacquired.

Validation: 19 student Python tests and eight instructor bridge/preview regression
tests pass; runtime and Editor assemblies compile. Unity scenario checks now cover
the grace deadline but were not run in Play mode. Live camera detection and physical
three-person movement are still unverified. Restart the camera and use the current
[control/troubleshooting guide](Assets/StudentWork/PeelPatrol/README.md).

## Movement readiness and player orientation (2026-10-05)

The canonical neutral poses, startup checklist, and gestures are in the
[Peel Patrol control guide](Assets/StudentWork/PeelPatrol/README.md#physical-controls-default).
The standard setup is now **3 / Team**, **Seated / upper body**, standing or seated.
From the players' side facing the screen, stand **left P1, center P2, right P3**.
The student bridge shows a labeled **SELFIE** preview and assigns roles in that
same display space. Inference still receives the original camera frame: anatomical
left/right hand channels are preserved. Wrist overlays explicitly label L/R.
The student wrapper reverses the raw lean sign so the player's own left lean turns
the elephant left in both upper-body and full-body modes.

Game calibration now requires travel/steering inputs, without gating on optional
foot animation or P3's action hands. P3's hand calibration is recorded independently.
Missing P3 hand confidence holds actions without stopping P1/P2 driving. The later
dropout update above also preserves other roles when a whole person disappears.
The panel reports separate MOVEMENT
and ACTIONS readiness, including missing calibration, missing joints, an old bridge,
paused rounds, and disabled practice. Restart the camera and recalibrate after updating.

Validation: 12 student Python tests passed, including player-side assignment,
anatomical hand channels, and steering signs in both body modes. Eight instructor
bridge/preview regression tests also passed. Runtime and Editor
assemblies compiled against the installed Unity references. The Unity scenario checks
were extended for independent action readiness and calibration with hidden feet, but
were not executed in Play mode. Desktop access was unavailable during this update;
live camera orientation, movement, and three-person physical play remain unverified.

## P3 action gestures (2026-10-05)

P3 now raises the right hand to ready an orange, then makes a quick forward/downward
swing to throw. Raising alone does not fire. Slowly lowering cancels; readiness expires
after 2.5 seconds. Hold both hands forward for 0.75 seconds inside a station to use it.
Bring hands back before repeating. Two-hand reaches cancel readied throws. The HUD
and MediaPipe panel show readiness, station progress and calibrated reach values.

The student bridge now uses `Tracking/bridge_runtime.py`, adapted from the pinned
instructor source. It adds optional two-element `handForward` and
`handForwardConfidence` arrays to each performer; original six-channel packets remain
valid for reference scenes. Peel Patrol requires the added hand channels for action
calibration and actions; movement can be calibrated independently. Stop Play and restart the camera after upgrading, then recalibrate with
hands relaxed near the waist. The instructor clone is unchanged.

Validation: isolated compilation of runtime and Editor assemblies against Unity's
current references; executable C# throw/station state tests; eight student Python tests
for assignment and hand features. Live Unity Play-mode integration and physical
gesture tuning have not been verified for this gesture update.

## Three-player assignment update (2026-10-05)

Peel Patrol launches `Tracking/peel_patrol_bridge.py`, a versionable student wrapper
around the student bridge adaptation. For three people it assigns current horizontal
positions each frame: **left P1, center P2, right P3**, in the SELFIE preview
(orientation corrected in the movement update above).
There is no previous-position distance limit for a complete, separated group.
The original exact-three restriction is superseded by the dropout update above.
Three valid detections restore full assignment automatically. Recalibrate after changing
places. Other group sizes and the original reference scene retain the instructor tracker.
Restart the camera after this update to load the new Python entry point.

Five synthetic Python tests pass: shuffled detection order, large position changes,
crossings, dropout/reappearance, and unchanged other group modes. Live three-person
detection and movement still need playtesting; joints must pass the original visibility
checks before a detected pose is eligible for assignment.

## Interface integration record (2026-10-04)

Open `Assets/StudentWork/Scenes/Peel-Patrol.unity`, then **DigiPhant > MediaPipe
Controls**. The dockable tab has an **Enter Play Mode** button. The same controls
appear in the Game view's left sidebar during Play.

Select **1 / Solo** for a quick check or **3 / Team** for the planned game, choose
**Full body** or **Seated / upper body**, and click **Start camera**. The instructor's
Python bridge launches locally and sends its live skeleton preview to Unity; no
terminal or additional installation is required on this machine. Keep all selected
performers and required joints visible, then use **Set neutral pose** and hold still
for ten seconds. Hands around waist height leave room for positive and negative
movement. Practice travel and posing without enemies, then **Start defense waves**.

**Tab** toggles the Game sidebar without pausing the round. Camera controls remain
available in the dockable tab. **Stop camera**, switching to test sliders/keyboard,
or stopping Play closes Unity's owned camera bridge. Selecting another group size,
body mode or person assignment requires recalibration. Tracking loss holds travel
and waves; the panel shows performer visibility and optional calibrated signal meters.

P1's left hand controls travel. P2's lean steers (P1 in Solo). The last selected
performer raises then swings the right hand to throw and holds both hands forward in a station to use it.
Solo intentionally shares travel/station input and is useful for testing. Detailed
gameplay instructions are in `Assets/StudentWork/PeelPatrol/README.md`.

Source located: [instructor repository](https://github.com/kommanderpi/studentstarter),
especially `DigiPhant/README.md` and `DigiPhant/Runtime/DigiPhantController.cs`,
`DigiPhantCameraPreview.cs`, and `DigiPhantLocomotion.cs`. The supplied panel is a
runtime Game-view sidebar. Our dockable Editor tab is a student adaptation sharing
the same controls with Peel Patrol's persistent sidebar. The reference scene remains
available at `Assets/DigiPhant/Scenes/DigiPhant.unity`.

Implementation: `PeelPatrolMediaPipePanel` is attached automatically during Play;
`PeelPatrolMediaPipeWindow` supplies the Editor tab. The imported controller exposes
the remaining calibration seconds and clears a transient startup socket status when
a valid packet arrives. The imported preview can embed in the shared interface and
passes the selected upper-body mode when launching Python. Tracking algorithms,
packet validation, calibration, and rig movement still use the instructor's components.

Verified on this machine: Unity compilation, Peel Patrol Play-mode scenarios including
practice, retained sliders, visible-panel gameplay and all 1-4 performer mappings;
all 11 instructor Python tests; visible dockable/Game panels; camera-0 startup from
the UI, live preview with a P1 skeleton, and Stop Camera clearing the preview. The
Python recording fixtures needed an unrestricted test process because sandbox-created
temporary directories had incompatible Windows permissions; the source tests passed
unchanged. No performance recording was made. Live calibrated gesture control,
three-person play and difficulty tuning still need a human session.

## Original project setup

Setup date: 2026-10-01 (America/Vancouver).

## Actual environment and provenance

- Project root: `C:\Users\alias\Documents\GitHub\Projects\Unity\Chillin-Digiphant\Chillin-Digiphant`.
  The outer folder is the workspace; the inner folder contains the Unity project.
- Windows x64, OS build `10.0.26200`.
- Unity `6000.6.3f1` (`45d8eee7de74`), installed under
  `C:\Program Files\Unity\Hub\Editor\6000.6.3f1\Editor\Unity.exe`.
- Universal Render Pipeline `17.6.0`; existing quality-level pipeline assets retained.
  Input System `1.20.0`, with the existing new-input-system setting retained.
- CPython `3.12.14` x64, installed locally through uv `0.12.21` in
  `.tools/python/cpython-3.12.14-windows-x86_64-none`.
- Tracking environment: `DigiPhantStarter/Tracking/.venv`.
- Direct dependencies: MediaPipe `0.10.35`, opencv-contrib-python `4.14.0.94`
  (OpenCV runtime reports `4.14.0`). The starter's pins were retained.
- Starter: https://github.com/kommanderpi/studentstarter.git
- Starter commit: `5adcbea1f7b311b8bb15c17b3dc4742ae8fff3af`.
- Clone: `DigiPhantStarter`, beside `Assets`; tracking stays outside Unity assets.
- Imports: `Assets/Elephant`, `Assets/DigiPhant`, including original metadata.
- Camera index `0` captures 640 x 480 RGB frames. Windows enumerates an ASUS FHD
  webcam and an ASUS IR camera; the OpenCV index is the tested selection.

Python 3.12 was chosen because no usable Python was available on the normal
command path and this release has Windows-compatible dependency wheels. Runtime
installation is local to this project and does not require shell activation.

## What was integrated

The source scene was duplicated into
`Assets/StudentWork/Scenes/Chillin-Digiphant.unity` with a new scene GUID.
The supplied reference scene and existing `Assets/Scenes/SampleScene.unity` remain
available. The student scene uses the default three-person, full-body mapping and
starts in Test sliders mode. Camera preview and recording use the supplied
components and the conventional relative Python path.

`Assets/StudentWork/Editor/ProjectValidation.cs` runs the supplied validation and
checks the student scene's scripts, rig, locomotion, material/shader references,
and disabled vendor controller. Its setup method attaches preview/recording
components explicitly and puts the student scene first in build settings.

One imported validation-only fix was required in
`Assets/DigiPhant/Editor/DigiPhantSetup.cs`: the seated-mode test now selects
Camera mode after the preceding UDP test had switched back to Test sliders.
Without this, the supplied test injected camera data while testing slider input
and incorrectly failed. Runtime behavior and starter Python source were unchanged.

The vendor `Elephant` controller stays disabled. The locomotion PlayableGraph owns
gait animation, then the controller adds bounded gesture rotations. This prevents
keyboard input and the original Animator controller from fighting the rig driver.

`.gitignore` excludes the starter clone, shared elephant assets, environments,
`.tools`, caches, and recordings. The existing student repository remote was
preserved. No project changes were pushed and no instructor repository was changed
remotely. `Setup-DigiPhant.ps1` provides reconstruction without overwriting tracked
student adaptations.

Windows Git initially converted source text to CRLF. Files were normalized only
when doing so reproduced their published SHA-256 exactly. All **124** supplied
asset/model checksums matched before Unity import. Unity may subsequently update
importer metadata for this editor version.

## Data path and controls

Webcam -> Python/OpenCV -> local MediaPipe Pose Landmarker -> six body-relative
signals and confidence -> UDP JSON -> Unity calibration/mapping -> elephant rig.
All tracking processing and communication are local. The bundled model is
`DigiPhantStarter/Tracking/pose_landmarker_full.task`.

| Local port | Purpose |
| --- | --- |
| 5055 | Unity tracking receiver and command source |
| 5056 | Python bridge and configuration receiver |
| 5057 | Unity JPEG preview receiver |

The six signals are left/right hand height, left/right foot lift, lean, and arm
spread. Unity subtracts the saved neutral values, applies sensitivity and
inversion, clamps, smooths, and rotates the assigned bones. Default minimum
confidence is `0.5`, tracking timeout `0.5` seconds, and bone smoothing `8`.
Locomotion uses its own smoothing `6`, dead zone `0.12`, and a 15-unit stage radius.

| Performer | Full-body input | Response |
| --- | --- | --- |
| P1 | Left/right foot lift | Front legs |
| P1 | Left-hand height, lean | Forward/backward travel, steering |
| P2 | Left/right foot lift, lean | Rear legs, tail sway |
| P3 | Lean, right-hand height, hand separation | Head, trunk, ears |

For solo tests, select 1 person: all roles move to P1. Seated mode substitutes
hand raises for foot lifts and shoulder tilt for lean. Solo seated hand motions
therefore affect several controls at once. In this original reference workflow
(not Peel Patrol), the preview is unmirrored; left/right
body parts refer to the performer's own sides. Initial IDs are assigned left to
right and require the selected number of people to be visible.

## Start, calibrate, and recover

1. Open `Assets/StudentWork/Scenes/Chillin-Digiphant.unity` and press Play.
2. Allow camera access yourself if Windows requests it. The bridge launches
   automatically in the Editor; do not also launch another bridge on the same ports.
3. Test sliders can verify the puppet before using live tracking.
4. Select group size, Full body or Seated / upper body, then Camera.
5. For full body, keep hips and feet visible. For seated mode, keep shoulders and
   hands visible. Keep P1's left hand around waist/mid height for calibration so
   it has room to move up and down for travel.
6. Click Set neutral pose (10 seconds), get into position, and hold still.
   All assigned movements must be visible when the countdown ends.
7. Test one control at a time before collective movement.

Changing performer count or movement mode clears calibration. Reassign people
resets identity assignment; calibrate again afterwards. Tracking loss stops travel
and fades affected bone controls toward rest. Valid input can resume control using
the existing baseline; reassign and recalibrate after identity swaps or repositioning.
Return to starting position also requires fresh calibration in Camera mode.

Stop Play to stop the bridge owned by Unity. Retry restarts its camera preview.
For an independent test, run the following, then press Q in its preview to stop it
before returning to Unity:

```powershell
& ./DigiPhantStarter/Tracking/.venv/Scripts/python.exe ./DigiPhantStarter/Tracking/bridge.py --people 1 --upper-body-only --camera 0
```

Built players require a separately launched bridge. The automatic launcher and
recorder are Editor-only. Windows camera permissions must be managed by the user.

## Recording and verification

Select Camera, obtain a live preview, and calibrate first. Keep the Game view
visible, ideally 1280 x 720 or larger. Click Record performance, then Stop recording
and save. Output is `Recordings/<session>/performance.mp4`. Capture targets 10 fps
at up to 1280 x 720 and has no microphone audio. The encoder preserves elapsed
time by holding frames across capture gaps.

Check the beginning, end, and each performer's action before uploading to the
team's Drive folder. No recording or Drive upload is automatic. Frame JPEGs,
`frames.jsonl`, and `session.json` remain available for recovery:

```powershell
& ./DigiPhantStarter/Tracking/.venv/Scripts/python.exe ./DigiPhantStarter/Tracking/encode_recording.py --session 'Recordings/SESSION_FOLDER'
```

Verification results are recorded below after setup checks finish. Encoder unit
tests alone do not verify visible Unity Game view capture or a live performance.

## Reconstruct a fresh checkout

Run `./Setup-DigiPhant.ps1` from the Unity project root. It uses the recorded
starter commit, verifies source hashes, restores omitted elephant assets, keeps
existing DigiPhant adaptations, creates the local Python environment, installs
dependencies, and runs the supplied tests. Keep the `.tools/python` runtime while
using its virtual environment; moving the project requires recreating the venv.

Manual equivalent, for an already compatible Python interpreter:

```powershell
git -c core.autocrlf=false clone https://github.com/kommanderpi/studentstarter.git DigiPhantStarter
git -C DigiPhantStarter checkout --detach 5adcbea1f7b311b8bb15c17b3dc4742ae8fff3af
Copy-Item DigiPhantStarter/Elephant Assets/Elephant -Recurse
Copy-Item DigiPhantStarter/Elephant.meta Assets/Elephant.meta
python -m venv DigiPhantStarter/Tracking/.venv
& ./DigiPhantStarter/Tracking/.venv/Scripts/python.exe -m pip install -r DigiPhantStarter/Tracking/requirements.txt
& ./DigiPhantStarter/Tracking/.venv/Scripts/python.exe -m pip check
```

Only run those copy commands when their destinations are absent. The student
repository already includes `Assets/DigiPhant` and `Assets/StudentWork`; do not
replace them with starter reference copies. The Python source is unmodified.
On macOS/Linux, use a compatible Python interpreter, `cp -R` for the shared
asset folder, and `.venv/bin/python` for environment commands. That platform path
is supported by the supplied launcher but has not been tested on this machine.

For repeat Unity validation, close the project in the Editor first, then run:

```powershell
& 'C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Unity.exe' -batchmode -nographics -quit -projectPath $PWD.Path -executeMethod ChillinDigiphant.Editor.ProjectValidation.Validate -logFile Logs/revalidation.log
```

## Test results and remaining live checks

- PASS: all 124 supplied asset/model hashes before Unity import.
- PASS: Python dependency consistency and MediaPipe/OpenCV imports.
- PASS: all 11 supplied Python tests, including MP4 encode/decode, duration,
  frame holding, missing frames, performer matching, and JPEG transport.
- PASS: camera index 0 opens and reads a 640 x 480 color frame.
- PASS: Unity compilation and supplied rig, slider/reset, UDP, performer preset,
  seated calibration, stale tracking, gait, travel, turning, gesture composition,
  compound mapping, tracking loss, stage limit, and reset validations.
- PASS: student scene rig, scripts, material/shader references, locomotion, and
  disabled vendor controller; preview/recording components saved explicitly.
- PASS: 15-second actual webcam/MediaPipe bridge check produced 82 tracking
  packets and 58 decodable 320 x 240 previews, detected one performer, and
  confirmed seated mode configuration over UDP. The test bridge was stopped.
- PASS: a disposable local Git snapshot and fresh checkout contained 112 student
  files (556,606 bytes); the restore script recovered all 96 shared elephant
  files with exact supplied hashes, and all student file contents were preserved
  (13 files had only Git line-ending conversion). The bundled model was verified.
  This was an asset/source reconstruction check: Python was not reinstalled and
  Unity was not opened in that disposable checkout. Compilation and dependency
  validation were performed in the working project.
- GUI inspection was unavailable: the computer-use native pipe could not connect
  after retries. No visual Unity rendering, Play-mode auto-launch, or actual
  Game view recording is claimed from the headless checks.
- Remaining user checks: physical neutral calibration, intentional movement and
  tracking-loss recovery, three-person control, and a visible Game view recording
  containing the live camera and elephant. No performance video has been uploaded.

Local evidence (excluded from Git): `Logs/python-tests.log`,
`Logs/bridge-smoke.json`, `Logs/bridge-smoke.log`, `Logs/project-validation.log`,
`Logs/final-validation.log`, and `Logs/reconstruction.json`.
