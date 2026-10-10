# DigiPhant Kart verification — October 8, 2026

Verified in the open Unity **6000.6.3f1** editor with **URP 17.6.0**.
Playable scene: `Assets/StudentWork/Scenes/Digiphant-Kart.unity`.

## Results

- The supplied Savannah bundle, deterministic generator, scene-copy, scene
  injection, unique-backup, and save/reload checks passed.
- All **50 imported SavannahCourse files**, plus the root folder metadata,
  match the separate source checkout byte for byte. Source commit:
  `4e6dab5271c618d1c0bfb2022c41c0d37cd8d69f`.
- The serialized controller's original hand/bone mappings and camera-preview
  settings match the pre-change snapshot. Its `upperBodyOnly` setting now defaults
  to false for literal jump/duck. Locomotion settings differ only in the earlier
  `externalMotion` flag. Optional body/face/hand channels, calibration hooks and
  the kart bridge command extend the original six-channel protocol. The starter
  checkout, existing pose model, dependencies and project settings are preserved,
  apart from the earlier kart entry in Build Settings. Face and hand bundles are
  new, separate files under `Tracking/models`.
- Unity scene validation passed for rig references, materials, four Rigidbody
  karts, standing/crouched collision shapes, 18 item boxes, five obstacles, and
  18 ordered checkpoints. Defense/habitat game systems are inactive.
- Real Play-mode physics checks passed for acceleration, ground contact,
  steering, camera follow, trigger pickups, inventory/cooldown, projectile hits,
  kart-to-kart collisions, barrier collisions, and fall recovery.
- Navigation checks reject a finish-line shortcut and reverse checkpoint
  crossing. Ordered checkpoints complete exactly three laps. Replay clears
  the previous race and inventory.
- The current obstacle scene passed **23 core gameplay checks** in addition to
  the 21 new pickup/obstacle checks. Its full-circuit smoke drive took 45.60 seconds.
- The previous widening pass passed **24 core Play-mode checks** and an automated
  44.56-second lap before adding obstacles. Its **14 racing-polish checks** verified:
  an overtaking kart gained an 8.6m lead without contact; a side collision moved
  the receiving kart 2.24m; an orange immediately removed over 40% of speed;
  animated wobble, orbiting stars and a juice burst appeared; the slowdown and
  animation expired and acceleration resumed. Simultaneous hits respected immunity.
- Input smoothing ramped steering without an immediate full-lock jump. A guarded
  edge stopped outward movement; an unguarded edge allowed a real gravity-driven
  fall, followed by recovery to the last checkpoint without extra progress.
- The obstacle update passed **21 additional checks** in actual Unity physics.
  Empty, partially full, and full baskets all consume item boxes; visuals and
  triggers disappear together; another kart cannot claim a consumed box; boxes
  respawn after five seconds. Gray refill platforms and their displays are absent.
- A non-jumping kart collided with the log. A jumping kart physically cleared it
  without contact and landed. An upright kart hit the low canopy; ducking shortened
  its collision shape and lowered its visible driver. Releasing duck under the
  canopy held it low until standing was safe. The marked slalom opening was passable.
- **All four karts completed three obstacle laps in 128.2 seconds**, with **zero
  checkpoint recoveries** and **three jumps each**. Two racers made three obstacle
  contacts each and continued without being stranded. They navigated all
  ordered gates while items, orange throwing, boosts, collisions and passing ran.
  This is an editor simulation result, not a human driving session or a
  standalone-build performance benchmark.
- Synthetic MediaPipe packets passed the unchanged protocol and neutral-pose
  calibration. P1 forward and P2 steering work simultaneously; P3 LEFT remains
  available with its RIGHT wrist missing. Expired input stops driving. Rig
  evaluation leaves kart translation to the Rigidbody.
- Additional synthetic packets verified a RIGHT-hand rise triggers one jump,
  a held hand does not repeat it, and a calibrated RIGHT lean ducks. Driving and
  LEFT-hand throwing are unchanged; RIGHT-hand jumping replaces the removed refill action.
- The student tracking Python suite passes **25 tests**, including eight new
  body, face, hand-size normalization, occlusion and hand-ownership checks.
- Game-view screenshots were inspected for the wide racing ribbon, follow camera,
  sidebar, rivals, inventory, race HUD, minimap, orange-hit animation, open-edge fall,
  physical log jumping and the low-canopy duck animation.

Detailed local evidence: `Logs/Kart/build.txt`, `upgrade.txt`, `play-checks.txt`,
`quality-checks.txt`, `quality-race-progress.txt`, `preservation.json`,
`game-view.png`, `orange-hit.png` and `open-edge-fall.png`. Obstacle evidence:
`obstacle-build.txt`, `obstacle-checks.txt`, `obstacle-race-progress.txt`,
`jump-log.png` and `duck-canopy.png` in the same folder.
Earlier development failures are retained in separately named log files.

## Mixed camera controls

- The camera gesture suite passed **34 checks** covering calibration, a single foot lift being rejected,
  one jump per takeoff, landing/rearming, duck hysteresis and release, pinch-release
  firing once, tracking-loss resets, invalid optional data, and head steering.
- Actual Unity Rigidbody checks use synthetic camera packets to verify physical
  jump, shortened duck collider, takeoff from a preparatory squat, real orange
  spawning, simultaneous pose acceleration and face steering, and stale-input hold.
  These tests exercise the game integration, not real performer recognition.
- The existing **21 pickup/obstacle checks** passed again. All four AI karts
  completed three obstacle laps in **129.7 seconds**, with no checkpoint recovery;
  the log includes the preserved hand-rise and lean fallback controls.
- Installed official face and hand model bundles performed real inference on
  MediaPipe's public `portrait.jpg` and `right_hands.jpg` test images. Production
  role cropping and ownership checks passed with supplied role assignments;
  missing assignments produced no controls. Models detected a 478-landmark face
  and two 21-landmark hands. Model hashes are recorded.
- A 15-second local webcam smoke check delivered **280 pose packets and 102
  previews**, averaging **21.03 packets/second** after startup. There was sparse
  body/face visibility and no detected hand in this unattended check; it is not
  a three-person tracking or gesture-accuracy benchmark. Only counters were saved.
- Current evidence: `Logs/Kart/input-checks.txt`, `tracking-checks.txt`,
  `model-checks.json`, `live-bridge-checks.json`, `input-setup.txt`, and
  `mixed-controls.png`. Original camera preview orientation tests remain passing.

## Backups and preserved work

- Untouched original: `Assets/StudentWork/Scenes/Peel-Patrol.unity`.
- First pre-change snapshot:
  `Assets/StudentWork/Scenes/Before-Kart-20261008-095348.unity`.
- Snapshot used during the successful build:
  `Assets/StudentWork/Scenes/Before-Kart-20261008-095619.unity`.
- Supplied injector's backup:
  `Assets/StudentWork/Scenes/Digiphant-Kart_BeforeSavannah 1.unity`.
- Snapshot immediately before widening:
  `Assets/StudentWork/Scenes/Kart-BeforeWideRaceway-20261008-105925.unity`.
- Snapshot immediately before removing refill bays and adding obstacles:
  `Assets/StudentWork/Scenes/Kart-BeforeObstacles-20261008-131156.unity`.
- Snapshot immediately before saving mixed camera roles:
  `Assets/StudentWork/Scenes/Kart-BeforeMixedControls.unity`.
  Controller and bridge source snapshots also exist in
  `.tools/BeforeMixedControls-20261008/`.
- Original ribbon meshes are retained. The wider road has separate assets in
  `Assets/StudentWork/Kart/Generated/RacewayV2/`.
- Original starter checkout: `DigiPhantStarter/`, preserved with its local changes.
- Course source checkout: `SavannahCourseSource/`, outside `Assets`.

The old 25-unit radial travel bound remains serialized in the locomotion
component. The kart's physical movement intentionally bypasses it so the full
custom circuit is reachable. Original scenes retain the old locomotion behavior.

## Physical check still required

Shoulder steering / jump reliability update (2026-10-08): 37 Python regression
tests pass, including anatomical shrug direction, equal-shoulder neutrality,
no dependency on face/hands/hips/feet for P2, preview orientation and aspect ratio,
and ankle lifts with hidden toes. Real P3 open-hand/fist inference still passes
after removing the face model. 62 Unity input checks and 12 camera-start checks pass. The input checks cover shoulder calibration and
brief dropout, hip sway, brief ankle loss, single-frame spike rejection, one-shot
lifts, in-flight acceleration, forward momentum, and the existing fist/trunk flow.
The moving jump measured 8.79 m/s before takeoff and 10.91 m/s in flight,
with 4.67 m of travel; the short landing buffer also passed.
Current evidence: `Logs/Kart/input-checks.txt`, `camera-start-checks.txt`, and
`live-shrug-jump-check.txt`. The restarted live bridge and Unity preview received P1 ankle and P2 shoulder
channels; observed P2 shoulder values included both negative and positive values.
Live transport is separate from a user's confirmation of comfortable shoulder
steering and consistent moving jumps.

Foot-lift / fist mapping (2026-10-08): 32 Python tests pass. Real Hand Landmarker
inference on MediaPipe's public open-hand and fist images correctly distinguishes
the gestures in both preview orientations with no torso supplied. Fist measurement
uses 3D finger geometry, so mirror and rotation preserve the measurement. Sources:
https://storage.googleapis.com/mediapipe-assets/fist.jpg and
https://developers.google.com/edge/mediapipe/solutions/vision/hand_landmarker/python.
46 Unity input checks pass, covering one-shot left/right foot lifts, lowering to
rearm, whole-body motion and foot jitter rejection, held-fist crouch, fist-release
throw suppression, missing data, real Rigidbody jumps and low collider changes,
and unchanged pinch-to-trunk throwing. See `Logs/Kart/input-checks.txt` and
`Logs/Kart/hand-only-model-checks.json`. Physical comfort and full-team use still
need live testing.

Camera start flow (2026-10-08): 12 Unity Play mode checks pass. The actual
MediaPipe dock button receives mouse events before connection/calibration;
synthetic P1/P2 packets then exercise missing-role guidance, the real ten-second
neutral countdown, automatic race countdown, and an advancing race clock.
Tracking loss preserves the race, and Cancel or Keys prevents a delayed start.
See `Logs/Kart/camera-start-checks.txt` and `camera-start-ready.png`. This verifies
the UI and game flow, not a physical team's calibration performance.

Hand-only and preview update (2026-10-08): 29 Python unit tests pass, including
P3 with no pose, both mirror orientations, competing hands, and neighbouring
wrists. Real Hand Landmarker inference recognizes a public test hand composited
into P3 in both mirror orientations without any body observation
(`Logs/Kart/hand-only-model-checks.json`). Unity's input checks pass with zero P3
pose confidence, including an actual trunk projectile, brief frame loss, longer
loss cancellation, and pre-calibration pinch feedback. Actual EditorWindow
mouse events shrink/enlarge both preview dimensions and verify saved size
(`Logs/Kart/preview-checks.txt`). These automated inputs do not establish physical
pinch comfort or a full-team camera play session.

The user subsequently confirmed that the hand segmenter works with only a hand
in the section. Overlapping blue pose dots were then suppressed in P3's displayed
third, preserving only the gold hand outline there. Physical pinch-to-projectile
and simultaneous team play remain distinct from that detection confirmation.

Live capture and packet transport were checked. Physical foot-lift/fist recognition,
comfortable sensitivity, anatomical shoulder steering, mirror orientation with a real
gesture, and simultaneous three-person performance still need human testing.
Press Play, choose **Camera**, then **Start with Camera**, and check the picture
with a LEFT-hand raise. Keep P1's feet in view. During the ten-second calibration,
stand tall with hands lowered and shoulders relaxed. Try P1 lifting either foot, P2 shrugging either shoulder, and
P3 showing one hand, making/opening a fist, and pinching/releasing thumb and index while driving
together. Adjust Gesture sensitivity or Invert shoulder steering if needed. Simple
body, seated actions, Keys, mirror adjustment and recording remain available.
