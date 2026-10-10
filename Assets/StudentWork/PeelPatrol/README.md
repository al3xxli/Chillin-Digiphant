# Peel Patrol

Protect the savanna watering hole from walking bananas. Open
`Assets/StudentWork/Scenes/Peel-Patrol.unity` and press Play. The original savanna
and a `Before-Playtest-...` copy are preserved.

## Physical controls (default)

The instructor's [DigiPhant controls](https://github.com/kommanderpi/studentstarter/blob/main/DigiPhant/README.md)
provide the local Python launcher, camera/skeleton preview, performer assignment,
body modes, calibration and sliders. Peel Patrol now uses those components in a
persistent Game-view sidebar and a dockable **DigiPhant > MediaPipe Controls** tab.

1. Open **DigiPhant > MediaPipe Controls** and choose **Enter Play Mode**, or press
   Unity's Play button and use the Game sidebar.
2. Choose **3 / Team** and **Seated / upper body**, the standard game settings.
   Upper-body mode works standing or seated; feet do not need to be in frame.
3. Choose **Start camera**. No terminal is needed. Camera 0 is the default; stop the
   camera before changing its index if the wrong device is selected.
4. Stand one person in each outlined third: **P1 left / P2 center / P3 right**,
   **as shown in the camera preview**. Keep your torso near the center of your labeled
   zone. Check that raising your own left hand raises the hand at image-left.
   If needed, use **Flip camera image (mirror adjustment)** below the picture.
   This visibly flips the image immediately and remembers the choice for that camera.
   Follow the neutral poses below, then choose **Set neutral pose** and hold them
   through the entire ten-second countdown. Start gestures only after it ends.
   Changing group size, body mode or positions requires fresh calibration.
5. Test movement, throwing and grove reloading in **Practice movements / no waves**,
   then choose **Start defense waves**.
   The camera panel stays usable while playing. **Tab** hides/shows the Game sidebar;
   the dockable tab can also control the game independently.

### Neutral poses and gestures

Everyone faces the camera, with **both hands lowered comfortably beside the body**,
shoulders relaxed and wrists visible. Hold this pose for the full ten-second neutral
countdown. Frame the camera low enough to see the lowered hands. All hand names mean
**your own anatomical left/right**. The fixed **L/R** zone headers show where your
hands should appear once the preview behaves like a mirror.

| Person / position | Raise LEFT hand | Raise RIGHT hand | Both hands lowered |
| --- | --- | --- | --- |
| **P1 / left third** | Move forward | Move backward | Stop travel |
| **P2 / center third** | Turn left | Turn right | Stop turning |
| **P3 / right third** | Hold up to throw one orange per second | Reload at the orange grove; use the workshop/filter when inside those stations | Stop throwing / release station action |

Raise toward chest/shoulder height. P1 and P2 are continuous controls: hold the hand
up to keep moving/turning, and lower it to stop. **P1 can drive while P2 turns**;
for example, both raise their left hands to drive along a left curve. P2 can also
turn while P1 is stopped. Forward/backward and turns follow the elephant's facing.
P1/P2 use the difference between the two positive hand raises: equal raises cancel;
if heights differ, the higher hand wins proportionally. Lowering below neutral never
commands the opposite direction. Use one hand at a time per person for clear input.

P3 holds the left raise: the first throw follows a short 0.2-second deliberate hold,
then throwing repeats **once per second** until the hand lowers or the basket empties.
Lowering stops automatic fire; rapidly tapping cannot bypass the one-second cooldown.
The right raise (0.35 seconds) still uses a station once: lower the right hand between
station uses. A deliberate right-hand raise inside a station takes priority over
throwing. Outside stations, the left hand can throw even if the right wrist is hidden.
Tracking recovery needs a fresh stable 0.2-second left-hand hold; lowering first is
not required. Each action uses its own calibrated hand height.
No swing, forward reach, or body lean is needed. Reloading still requires visiting
the grove, and carried oranges can be thrown anywhere.

Unity applies a horizontal flip to the actual camera image by default, like a
selfie preview. Your own left hand should appear on the left of your image, and
your own right on the right. Use the flip checkbox if your webcam already mirrors
its output. Changing it clears calibration: set a new neutral pose afterward.
Python sends raw image and skeleton pixels; Unity flips them together exactly once.
Pose inference uses the original camera frame and retains anatomical hand channels.
Zone assignments follow the final displayed orientation. Unity draws readable
labels after the flip. Older preview streams are rejected with a restart message.
The preview has fixed boundary lines at **one-third** and **two-thirds** of its width.
The guides are **thick bright red**, with a red border and large **P1 / P2 / P3**
labels, with **L** on the screen-left and **R** on the screen-right inside each zone.
These mark your anatomical hands in this mirrored view. Unity draws them directly over the preview in **3 / Team** mode, including
while waiting for the camera; they do not depend on Python drawing the overlay.
The guides follow the actual camera image inside any letterboxing. Stop Play and
let Unity reload scripts to load a changed interface; restart the camera to load
Python assignment changes as well.
Assignment uses the detected shoulder midpoint in Upper body mode (hip midpoint in
Full body mode), within that same mirrored camera view. Each third assigns independently,
even at startup: it does not need all three people detected simultaneously. Hands
may reach across a line without changing roles; keep your torso inside your own zone.
Two detections in one third hold only that role, with **ONE PERSON ONLY** shown.
Empty zones show **STEP INTO VIEW**; a single eligible pose shows **TRACKED**.
These are assignment guides over the existing full-frame detector, not three separate
camera crops. They help with positioning but cannot recover a pose the model misses.
Keep your places while playing and recalibrate after swapping places. **Reassign
people** clears calibration but the thirds remain fixed.

### Confirm the controls before playing

After this update, **stop Play**, let scripts reload, enter Play again, **restart the
camera**, and recalibrate. Choose Upper body explicitly if an already-open scene
still has Full body selected. Simply entering Play does not open the webcam.

1. Check the preview labels: the player in the preview's left third should be **P1**,
   center **P2**, and right third **P3**. Raising your own left hand should move
   the wrist labeled **L**. Keep all three players' shoulders visible.
2. With **Practice movements / no waves** enabled, check **MOVEMENT** in the panel.
   P1 raising the left hand should produce positive **travel**; neutral should read
   near zero. P1 right hand should produce negative travel. P2 left hand produces
   negative **steering**, right hand positive. Test P1 forward and P2 turning
   together, then lower both hands to stop.
3. Check **ACTIONS**. If it asks for recalibration, have P3 show both hands in the
   hands-down neutral pose and recalibrate everyone. The new gestures use hand
   height only; forward-hand depth is no longer required. People being detected
   does not by itself mean their controls are calibrated.
4. Test P3 in practice: hold the left hand up and check the orange count drops
   once per second. P1 can drive and P2 can turn at the same time. Visit the grove
   and raise P3's right hand to reload. Practice does not start waves. Choose
   **Start defense waves** when ready; workshop/filter use becomes available then.

**MOVEMENT** reports the specific reason travel is held: calibration, either
driver's wrists/shoulders, pause, shop, or practice being disabled. Losing these
movement inputs holds movement and waves after a **0.25-second** grace period for
dropped or low-confidence observations. Repeated bad observations cannot extend
that grace period. A stopped camera stream still expires at the receiver's existing
tracking timeout. **ACTIONS** separately reports missing P3 data: it immediately
holds only the action whose wrist is unavailable; P1/P2 driving can continue even if P3's whole pose
temporarily disappears. No missing action pose is replayed. Feet used for optional
animation do not block game calibration.

### If tracking flickers

Restart the camera after updating the bridge. Keep **3 / Team** and **Upper body**
selected. Stand one torso near the center of each outlined third, then calibrate in
neutral. The preview shows tracking status for each zone separately. If you have
swapped roles, use **Reassign people** and recalibrate.

For the next physical test, face the camera with heads, shoulders and wrists in
frame. Leave visible space between people and keep arms out of each other's body
outlines. Try even front lighting and avoid a bright window behind you. Move the
camera back only far enough to fit everyone without making the people tiny.
Google's [model card](https://storage.googleapis.com/mediapipe-assets/Model%20Card%20BlazePose%20GHUM%203D.pdf)
describes sensitivity to lighting, motion, face position/scale/orientation and
overlap. It does not identify striped shirts specifically. To investigate clothing,
compare a plain layer versus the striped shirt in the same position and lighting;
this is a diagnostic experiment, not an established requirement.

**Full body** is an alternative that also needs visible hips. Both modes use the
same left/right hand raises. The standard instructions use **Upper body** throughout.

For other group sizes, P1 handles travel, P2 handles steering (P1 in Solo), and the
last selected performer handles actions. Solo/duo share hand inputs across jobs and
are useful for setup checks; three people keep the jobs separate. Live meters show
each person's left and right hand heights relative to the lowered neutral pose.

At a station, lower the right hand after entering, then raise it and hold briefly.
Watch the station progress indicator. Entering while already holding the hand up
cannot trigger the station. Occluded hands, tracking loss, pause, calibration and
station changes require releasing the right-hand gesture before the next station use.
A held left hand can resume throwing after a fresh stable observation.

Implementation thresholds: calibrated hand height 0.6 triggers after a 0.2-second
initial throw hold or 0.35-second station hold; release is below 0.25. Holding the
left raise repeats at one-second intervals, with no catch-up bursts after gaps.
A right raise of at least 0.6 inside a station takes priority over throwing. These normalized body-relative values need physical
playtesting. Travel is positive left rise minus positive right rise; steering is
positive right rise minus positive left rise, followed by the driver's dead zone.

Pausing or opening the upgrade shop holds the round. Opening the MediaPipe panel
does not pause it. **Stop camera**, switching to sliders/keyboard, or stopping Play
releases the bridge Unity launched. The existing receiver
validates packets; the game reads its calibrated movement channels. No WASD input
is read for travel in MediaPipe mode.

## Oranges and stations

Your basket starts with **6 oranges** and carries **10**. Oranges appear on the
back of the elephant and in the large HUD inventory; both update as you spend or
reload. Loading is a deliberate station action, not merely standing by the crate.
You retain the basket contents when you leave.

A compact **136 x 34** floating indicator shows the loaded count and a short state
(ready, auto, empty, cooldown or tracking hold). Its lower edge sits six UI pixels
above the projected bounds of the elephant **and the carried oranges**. The larger
inventory and ten markers remain in the corner HUD. The compact indicator hides
when there is no room above the elephant instead of covering its body at the edge
of the camera view.

A throw consumes one orange anywhere. A highlighted banana within 20 units receives
an assisted arcing throw; without a target lock the orange flies forward and lands.
The HUD distinguishes a locked target, a forward toss, and an empty basket. Incoming
shots count toward target selection to avoid over-targeting an already covered enemy.

| Zone | Action |
| --- | --- |
| Orange grove / crate | Refill to 10 oranges, free |
| Workshop / log pile | Spend 4 oranges to build or fully repair the weakest barricade |
| Water filter / barrel | Spend 3 oranges to restore 25 water cleanliness, capped at 100 |

Oranges are shared between throwing, construction and cleaning. Each barricade has
40 durability. Bananas queue with about 3 units between them instead of overlapping
behind the front attacker.

## Waves, coins and rides

Survive three waves of 6, 9 and 12 bananas. There are **18 seconds** to prepare before
each wave. Arrivals are **4.5 / 4.1 / 3.7 seconds** apart across alternating lanes;
walking speed is **0.82 / 0.94 / 1.06 units per second**. All three entry points have
moved roughly 15 units farther out, especially the eastern/bottom-right approach.
Offscreen incoming bananas have lane indicators. Third-wave green bananas need two hits.
Bananas now appear **70% larger** across all waves; third-wave bananas retain their
additional size boost. Speed, health and spawn timing are unchanged. Assisted
oranges aim at the enlarged banana body's center.
Every banana reaching the pond removes 12 cleanliness. Zero cleanliness loses the game.

Each defeated banana earns **3 coins**; clearing a wave earns **8 coins**. Choose
**Upgrades** to buy the next ride. The simulation waits while you choose.

| Upgrade order | Cost | Speed above the original elephant |
| --- | --- | --- |
| Skateboard | 12 coins | +30% |
| Unicycle | 20 coins | +65% |
| Jetpack | 28 coins | +105% |

The current ride appears on the elephant. Speed multipliers apply to forward, running
and backward movement for both keyboard and MediaPipe. Each new ride replaces the
previous one; bonuses are not compounded. The jetpack is a ground-speed boost with
visible rockets/exhaust; airborne movement is not implemented. A new game resets
coins and equipment. Coins persist between waves within the same game.

## Optional sliders and keyboard tests

Choose **Test sliders** in the MediaPipe panel for travel/steering and optional
body-part sliders. Their values persist until adjusted or reset. Practice supports
movement, posing, throwing and grove reloads without waves; building and cleaning
are enabled during defense rounds.
Choose **Keyboard** for temporary keyboard testing without performers.

- **WASD / arrows:** travel and steering. **Shift:** run (not throw).
- **Space:** throw oranges, with a 0.55-second cooldown.
- **Hold E in a zone:** use the station. Release after entering, then hold.
- **U:** upgrades. **P / Escape:** pause. Losing focus also pauses the round.
- **R:** reset. **Enter:** start. **Tab:** show/hide the MediaPipe sidebar.

## Validation

**DigiPhant > Peel Patrol > Run Play mode checks** runs scene/rule checks and simulated
Play-mode scenarios, then exits Play and writes `Logs/peel-checks.txt`.

Coverage includes carried ammo/reloading away from stations, free and assisted throws,
visible inventory, banana spacing, barricades, cleaning, pause, a real unopposed loss,
three-wave victory, coin rewards, every ride's purchase/speed/visibility, shop pause,
and replay cleanup. Synthetic MediaPipe packets exercise calibration, P1/P2 motion,
P3 throw/reload, held gestures, confidence loss, timeout, and recovery without phantom
input. Panel checks cover practice without waves, slider retention, viewport space,
running waves with controls visible, all 1-4 performer roles, body-mode changes,
calibration cancellation and disconnect invalidation. These checks do not substitute
for live group calibration or human difficulty testing.

Game and panel behavior is under `Assets/StudentWork/PeelPatrol`. Imported runtime
adaptations expose calibration time remaining, validate/calibrate the added hand
channels, and embed the shared preview in this interface. The launcher honors the
selected seated mode and uses the student entry point for Peel Patrol. The original
reference scene and starter clone remain available.

`Tracking/bridge_runtime.py` is a student copy of the instructor bridge with optional
left/right forward-hand channels. `hand_features.py` measures wrist depth relative to
the corresponding shoulder and normalizes by shoulder width, following the
[MediaPipe landmark depth convention](https://github.com/google-ai-edge/mediapipe/blob/master/docs/solutions/pose.md).
The six original movement channels remain available; the Peel Patrol wrapper adjusts
the lean sign to match the player's own left/right. The receiver validates and
calibrates the additional channels independently of movement readiness. Each action
hand is calibrated independently: left for throwing and right for station use.
Optional forward-hand channels remain supported but are not needed by these gestures.

For the October 5 movement/orientation update, 12 student Python tests and eight
instructor bridge/preview regression tests passed. Runtime and Editor assemblies
compiled against Unity's installed references. Updated Play-mode scenarios are
compiled but have not been executed; live three-person movement still needs testing.

The subsequent detection-dropout update passes **19 student Python tests** and the
same **eight instructor regressions**, with runtime and Editor compilation passing.
Assignment tests cover alternating missing people, single survivors, extra and
duplicate detections, ambiguous lanes, reset and recovery. Unity scenario checks
also cover bounded movement grace, but have not been executed in Play mode.

The fixed-third update replaces the moving-lane assignment tests: **15 student
Python tests** and **eight instructor regressions** pass, with runtime and Editor
compilation passing. Tests cover independent startup, exact boundaries, missing
players, duplicate detections within a zone, and divider visibility after JPEG
encoding. The encoded 320 x 240 guide was visually inspected on a synthetic image.
Live camera detection, Unity display and three-person movement remain unverified.


The two-hand control update compiles runtime/Editor assemblies and passes executable
C# raise/release, one-shot, conflict, dropout and station-entry checks. Updated Unity
scenario checks cover forward-plus-turn and reverse-plus-turn inputs, but have not
been run in Play mode. Live three-person controls still need physical verification.
An isolated executable using the actual driver/mapping methods with simulated
input also passes simultaneous forward/reverse plus left/right steering, neutral,
equal-raise cancellation, below-neutral rejection and group-mapping checks.

The visibility/automatic-fire update compiles runtime and Editor assemblies. Its
executable C# checks pass one-second repeat spacing at 10/20/30/60 Hz, lowered-hand
stop, rapid re-raise cooldown, tracking/conflict/frame-gap recovery and one-shot
station use. The updated Unity scenario checks include three throws during a
three-second held gesture but have not been run in Play mode. Live floating-panel
placement, enemy readability and physical P3 control remain unverified.


The compact-indicator/throwing update passes runtime/Editor compilation, 15 Python
checks and executable C# gesture/cadence/recovery checks. The compressed L/R camera
labels were visually inspected. Updated Unity scenarios cover moving, turning and
throwing together with P3's idle right wrist hidden, plus practice throwing/reload;
these scenarios have not been executed in Play mode. Live indicator clearance and
three-person combined control remain unverified.


The display now flips in Unity, using raw preview stream version 2. Seventeen
student Python checks pass, including unflipped source pixels, live orientation
commands, matching zone assignments and unchanged anatomical hand channels.
Runtime and Editor compilation pass. These checks do not verify the live Unity
picture or physical gestures. Restart Play and the camera to load this update,
check the picture using your left hand, and recalibrate.
