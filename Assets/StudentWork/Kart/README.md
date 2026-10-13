# DigiPhant Kart / Rainbow Savannah

Open `Assets/StudentWork/Scenes/Digiphant-Kart.unity` in Unity 6000.6.3f1.
Press Play. Choose **Camera** for the team activity or **Keys** for solo play and
gameplay troubleshooting. Keys stops the camera, requires no calibration, and
focuses Unity's Game view when selected from the docked controls. Start in practice,
collect orange boxes, and press Enter or Start Race
for a three-lap race against three elephant karts. The game scene is first in
Build Settings. The original Peel Patrol scene and its source scripts are archived
and preserved; the kart scene deactivates its defense and habitat systems.

## Assignment elements

| Element | Implementation |
| --- | --- |
| Picking up elements | Floating item boxes disappear immediately when any kart touches them, even with a full three-orange basket. They cannot be claimed twice and respawn after five seconds. Gray refill platforms and their floating displays have been removed. |
| Throwing elements | The player lifts an orange with the elephant's native trunk, then flicks it forward. The orange stays attached to the moving tip until release, then becomes a physical bouncing projectile. Space, mixed-camera pinch/release, and Simple body hand controls all use this animation. Bots keep their immediate forward throws. A hit cuts speed, limits speed for 1.15 seconds, and animates the kart with wobble, orbiting stars, and a juice burst. |
| Environmental and kart collisions | An 18m-wide MeshCollider road has passing space, rounded equal-mass Rigidbody karts, and physical rails. Three marked sections omit one rail. A physical fallen log, low acacia canopy, and three alternating bush/stone barriers add jump, duck, and slalom challenges. |
| Physics | Contact impulses transfer momentum between karts; lateral grip relaxes briefly after a bump. Actual gravity carries karts off open edges before checkpoint recovery and a short rejoin delay. |
| Acceleration | Smoothed throttle, gradual acceleration, coasting, braking/reverse, 22m/s top speed, and temporary boost pads. |
| Steering | Camera mappings drive smoothed Rigidbody steering with a small neutral dead zone and reduced sensitivity at speed. Steering remains available during orange hits. Keys offers independent solo keyboard control. |
| Course navigation | Luminous direction arrows, minimap, 18 ordered directional checkpoints, three laps, race position, timer, countdown, finish, and replay. |
| Camera movement | The existing camera follows the physical elephant with smoothing, turning, a speed-sensitive field of view, and road obstruction checks. |

## Camera roles and natural actions

Player throws animate the existing seven-joint trunk above the head, visible from
the rear kart camera. A small orange icon floats above the player's head whenever
a throw is ready; it hides with an empty basket, during a throw/cooldown, or while
play is held. A throw reserves one orange immediately, releases from the
actual tip after 0.62 seconds, and returns to normal rig animation by 0.96 seconds.
The one-second throw cooldown remains. Pause freezes the motion; changing controls,
getting hit, or recovering cancels an unreleased throw and returns its orange.
Reset clears the motion and inventory. Bots retain their original launch position,
velocity, immediate release, and cooldown.

Three-person screen zones remain **P1 / P2 / P3**, from screen left to right.
The default **Body + shoulders + hand** layout gives each person a different role.
Camera mode offers teams of two, three, or four. Camera selection, mirror,
alternative gesture layouts, sensitivity, meters, and recording are under
**Camera settings & diagnostics**. Keys shows only keyboard help and game controls;
the Sliders option has been removed from the kart UI.
Select Camera, then **Start with Camera** at the top of the controls or in the
central prompt. This opens the camera, waits for P1's driving inputs and
P2's steering input, and begins the ten-second neutral countdown. Sit or stand upright
with P1 hands down and P2 shoulders relaxed. The race countdown starts
automatically when neutral is saved. P3's hand does not block starting.
Missing-input guidance stays visible while waiting; Cancel Camera Start, Reset,
Pause, Stop, or switching to Keys cancels the pending start. You can still use
Start Camera and Set Neutral separately to calibrate for practice.

| Performer | MediaPipe input | Action |
| --- | --- | --- |
| P1, left zone | Pose landmarks | LEFT hand accelerates; RIGHT hand brakes/reverses. |
| P2, center zone | Pose shoulders | Raise your LEFT shoulder to turn right, RIGHT shoulder to turn left. Relax both to straighten; raising both together stays straight. No face, wrist, or foot input is required for steering. |
| P3, right zone | Hand Landmarker | Show a peace sign (index and middle extended, ring and little curled) to jump once; relax fingers before repeating. Hold one hand in a fist to crouch; open it to stand. Pinch thumb and index with the other fingers extended, then release to throw one orange. No body or shoulders needed in this zone. |

P1 needs shoulders, wrists, and hips visible in Full body tracking; feet are optional.
Seated / upper body tracking needs only shoulders and wrists. P1 body or
foot movements no longer trigger jumps in this layout.
P2 keeps both shoulders in the center section; sit or stand with shoulders relaxed for neutral.
P3 can show just a hand in the right-hand section. Keep the wrist inside that
section; a small overlap at the guide keeps fingers from being cropped off.
Two hands in P3 are rejected until only one remains. A gold finger outline and
an open/pinched/fist/peace label show detection even before calibration or while driving is
held. Brief missed frames up to 0.25 seconds preserve the pinch; longer loss
requires opening the hand and starting a fresh pinch. Peace signs must stay
visible for 0.08 seconds; relax fingers for 0.12 seconds before the next jump.
Holding a peace sign cannot repeat jumps; after hand tracking is lost for 0.25
seconds, relax fingers before trying again. A peace sign cancels any pending throw. Three-person roles follow the existing screen zones,
including when the preview is flipped. Ambiguous roles do not take control.

A jump requested up to 0.20 seconds before landing is buffered. Hops preserve
horizontal velocity, and holding acceleration continues gentle forward drive in
the air. Releasing acceleration in flight does not apply brakes.
P3's fist holds crouch, with finger-curl hysteresis to resist jitter. Opening the
fist releases crouch without throwing. A throw requires a separate pinch with
the spare fingers extended; curling those fingers cancels a prepared throw.
P3 hand actions stay independent of P1's foot tracking. Losing the hand for more than
0.25 seconds releases crouch. Under an overhead obstacle, collision clearance
still keeps the kart low until it can safely stand.
Shoulder steering has a calibrated neutral dead zone and smoothing. A brief
missed shoulder frame is tolerated; loss beyond 0.25 seconds holds driving;
missing P3 hand data disables hand actions without blocking driving. Reassignment, changing
layout, or flipping the camera requires new calibration. Gesture sensitivity
adjusts shoulder shrug range; steering can be inverted.

For two people, P2 combines shoulder steering and hand actions. Solo combines all three
modalities on P1. Four-person mode uses P1 body, P2 shoulders, and P4 hand actions.
These shared-role layouts still require the assigned torso and right wrist.
Pose and hand inference is local. Shoulder steering reuses the pose model, so
the face model is no longer loaded. The hand model loads only in the mixed layout.

Drag the camera preview's bottom-right grip to resize its width and height.
The image keeps its proportions inside the frame. Sizes are remembered separately
for the Game sidebar and MediaPipe dock. The Game sidebar widens with its preview;
the dock preview fits within its panel (widen the Unity panel for more room).

## Existing controls and seated fallback

**Simple body** (under Camera settings & diagnostics) keeps the original hand driving and throwing. With Literal jump /
duck enabled, its last performer physically jumps and ducks. Select **Seated**
or turn off Literal jump / duck to use the earlier accessible substitutions:

| Performer | Anatomical hand | Action |
| --- | --- | --- |
| P1 | LEFT raised | Accelerate forward |
| P1 | RIGHT raised | Brake, then reverse |
| P2 (P1 in Solo) | LEFT / RIGHT raised | Steer left / right |
| P3 (last selected performer) | LEFT held up | Throw repeatedly at one-second intervals |
| P3 (last selected performer) | RIGHT raised | Jump once; lower the hand before the next jump |
| P3 (last selected performer) | Lean RIGHT | Hold a low crouch; return upright to stand when clear |

Moving, steering, throwing, and obstacle actions work concurrently. In Simple body,
throwing requires the LEFT wrist; a missing RIGHT wrist does not itself block it.
Missing movement tracking holds the simulation until movement input is valid again.
Calibration, mirror adjustment,
camera selection, reassignment, live meters, and recording
remain in the sidebar and **DigiPhant > MediaPipe Controls** dock.

The kart UI uses Keys for camera-free solo play. Team size and camera layout stay
configured when switching to Keys, so switching back restores the team setup.

Keyboard: **W/S** or Up/Down accelerate/reverse; **A/D** or Left/Right steer;
**Shift** uses full throttle; **Space** throws; **E/J** jumps; hold **C/Ctrl** to duck;
**Enter** starts/restarts; **P/Escape** pauses; **R** returns to practice;
**Backspace** recovers to the last checkpoint; **Tab** shows/hides the sidebar.

## Savannah obstacles on Rainbow Road

### Lap progression

Practice and lap one use the original course. Completing the human player's
first lap adds one log, one canopy, and two banana crossings (six peels).
Completing the second lap adds another log, another canopy, and two more banana
crossings: lap three has three logs, three canopies, and twelve peels total.
Faster rivals cannot trigger the progression. No extra slalom or steering
sections are added. New obstacles wait until all nearby racers are at least
22 metres away before activating, so colliders cannot appear under a kart.

Banana crossings have yellow JUMP / BANANAS warning signs. Their low triggers
can be cleared with the existing jump action. Contact uses the existing hit
wobble and orbiting stars, reduces speed for 1.15 seconds, and prevents throws
during recovery. There is no additional steering input or forced steering
reversal. The used peel disappears for five seconds; brief hit immunity prevents
stacked impacts. Pausing also pauses recovery and respawn timers.

These additions are created from existing log/canopy assets and a procedural
peel mesh at runtime. The serialized first-lap scene and supplied course assets
remain intact. Reset/restart hides later challenges and resets the peels.
Bots read the active additions and jump or crouch using their existing actions.

Orange storage is capped at three. The HUD now displays three filled/empty
fruit slots alongside the count. A pickup adds one, a trunk throw reserves one,
and an interrupted unreleased throw refunds it up to capacity. A full basket
still consumes a contacted box. Reset empties the basket.

- **Fallen log:** jump from the amber approach stripes. The hop uses Rigidbody
  velocity and accounts for uphill travel; driving straight into the log collides.
- **Low acacia canopy:** duck before the cyan clearance markers. The driver lowers
  visibly and the physical collision shape becomes shorter. Releasing duck under
  the branch keeps the kart low until there is room to stand.
- **Bush and stone slalom:** follow RIGHT, LEFT, RIGHT signs through the alternating
  eight-metre openings. Each opening retains room for two karts side by side.

Advance signs and a HUD prompt announce each obstacle. Rivals use the same physical
jump and crouch actions and choose the open slalom lanes. The starter's octagonal
timber, canopy meshes, bark, foliage, and stone materials are referenced directly;
its original scenery-only package remains unchanged.

## Instructor course and preservation

The separate `SavannahCourseSource/` checkout was downloaded from main at
`4e6dab5271c618d1c0bfb2022c41c0d37cd8d69f`. `Assets/SavannahCourse/` was imported
with its complete metadata. The supplied **Inject Course into Open Scene** tool
was invoked on the saved kart scene copied from the active Peel Patrol scene.
Its original layout 4, seed 20261005, meshes, materials, prefab, and validation
tools remain intact. The custom rainbow ribbon is now a broad, elevated circuit
around the original scenery, with generous bends and an 18m road (formerly 8m).
The start is on a long straight with separated grid rows. AI karts slow for bends,
choose free passing lanes, and brake for traffic. A last-checkpoint recovery is
available for falls or sustained obstruction, and never grants a checkpoint or lap.
Amber edge dashes and OPEN EDGE signs warn of the three unguarded stretches.

Custom racing objects live outside the reserved Savannah root. The static package
does not provide physics; all interactive additions are in `StudentWork/Kart`.
The supplied setup's static-only restriction is superseded for these additions
by the requested kart game. URP 17.6.0, tracking scripts/models, Python environment,
UDP ports, bone mappings, and the bridge's camera mirror implementation are retained.

`DigiPhantLocomotion.externalMotion` preserves gesture reading and rig animation
while handing travel to `KartVehicle`. The old finite `stageRadius` is still
serialized, but is intentionally bypassed by the kart's physical movement so the
whole circuit is reachable. Its original behavior remains the default in old scenes.

The exact pre-build and injector backup paths are recorded in
`Logs/Kart/build.txt`. The first pre-change snapshot is
`Assets/StudentWork/Scenes/Before-Kart-20261008-095348.unity`.
The source `Assets/StudentWork/Scenes/Peel-Patrol.unity` was not overwritten.
The pre-widening scene is
`Assets/StudentWork/Scenes/Kart-BeforeWideRaceway-20261008-105925.unity`.
Its old ribbon meshes are retained; the wide road uses separate `Generated/RacewayV2` assets.
The pre-obstacle scene is
`Assets/StudentWork/Scenes/Kart-BeforeObstacles-20261008-131156.unity`.

## Verification and testing

**DigiPhant > Kart > Validate race audio** runs Play-mode checks for imported
audio, countdown/GO, successful and rejected jumps, landing, held/released crouch,
actual trunk release, pickups, impacts/immunity, boosts, player-only lap music,
final-lap tempo, pause/resume, finish fade and reset/mute. Reports are written to
`Logs/Kart/audio-checks.txt` (errors: `audio-error.txt`). Listening on the target
speakers and testing real team gestures remain separate from these checks.

`Runtime/KartAudio.cs` attaches automatically without changing the saved scene.
The original 32-bar **Rainbow Savannah** score has 136 and 160 BPM performances,
phrase-aligned final-lap crossfade, gentle lap/progress acceleration, and music
ducking under lap/hit fanfares. Both music and effects are 2D so direction/camera
distance cannot hide feedback. All sounds ship in `Resources/KartAudio`.
`Tools/Audio/generate_kart_audio.py` reproduces the WAVs with Python/NumPy.
Music/effects sliders and mute are in the existing controls sidebar. Muting
preserves captions. Pause/tracking holds suspend voices without restarting the
score; reset/restart clears the final arrangement. Rivals never trigger your
action sounds or lap fanfares.

**DigiPhant > Kart > Validate lap progression and banana peels** opens the saved
kart scene and runs Play-mode checks for human/rival progression, safe activation,
three-orange collection and actual trunk releases, banana collision/recovery,
pause, peel respawn, added log/canopy clearance, finish, and reset/replay.
It preserves a dirty scene as a backup before opening the kart scene and exits
Play afterward. Results are in `Logs/Kart/lap-checks.txt`; failures are in
`lap-error.txt`. This is automated physics validation, not a physical gesture test.

**DigiPhant > Kart > Validate player trunk throws** checks the real rig motion,
held orange, delayed tip release, projectile collision, interruption handling,
and unchanged bot firing. Results: `Logs/Kart/trunk-checks.txt`; rear-camera images:
`trunk-windup.png`, `trunk-flick.png`, `trunk-release.png`, and `trunk-flight.png`.

**DigiPhant > Kart > Validate keyboard play without camera** exercises virtual
keyboard events through the real Unity input and physics loop. It checks driving,
steering, reverse, jump, throw, duck, pause, countdown, switching modes, and Game
view focus after using the dock. Results: `Logs/Kart/keyboard-checks.txt`.
Rendered panels: `Logs/Kart/keys-controls.png` and `camera-controls.png`.
This check does not substitute for physical keypress or live camera testing.

Use **DigiPhant > Kart > Validate scene** for references, materials, colliders,
rig mappings, item setup, and absence of active defense systems.
Use **DigiPhant > Kart > Run Play mode validation** for the real physics checks
and an automated drive around the complete circuit. Reports are written under
`Logs/Kart/`. These checks temporarily control the running game; stop and re-enter
Play afterward for a clean user session.

Use **DigiPhant > Kart > Validate pickups and obstacles in Play mode** with Unity
in Edit mode for item consumption/respawn, real jump/duck clearance, slalom,
synthetic gesture checks, and a complete four-kart three-lap obstacle race.
Evidence is saved in `Logs/Kart/obstacle-checks.txt` and `obstacle-race-progress.txt`.

The racing-polish regression in `Editor/KartQualityChecks.cs` additionally checks
clean overtaking, real sideways displacement, visible hit animation and timed
slowdown, steering smoothing, guarded versus open edges, fall recovery, and a
four-kart three-lap drive. To run it from the project root with Unity in Edit mode,
create an empty `.tools/quality-kart` file. It reloads the saved kart scene and
temporarily drives all four karts, then returns to keyboard practice. Results are
in `Logs/Kart/quality-checks.txt` and `quality-race-progress.txt`.

The instructor's bundle, deterministic generator, scene-copy, injection,
backup, and save/reload checks passed during import. The student bridge's
37 Python regression tests passed. The camera input suite runs by creating
`.tools/test-kart-inputs` with Unity in Edit mode; results are saved in
`Logs/Kart/input-checks.txt`. It tests body/shoulder/hand events and actual Rigidbody
actions using synthetic packets. See [VALIDATION.md](VALIDATION.md) for evidence
from model inference and live camera transport. Physical performer gestures and
mirror orientation still need a human check: raise the left hand, calibrate,
then try P1 acceleration/braking, P2 shoulder steering, and P3 peace-sign jumps, fist crouch,
and pinch-release throws together.
