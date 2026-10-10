# Agent installation and scene setup

1. Locate the root containing Assets/Packages/ProjectSettings. Inspect OS, architecture, Git changes, scenes, and editor state. Read README.md and INSTALL.md.
2. Match Unity 6000.6.3f1, URP 17.6.0, existing input settings and package pins. Run Setup-DigiPhant.ps1 on Windows x64, or manual INSTALL steps elsewhere. Restore starter commit 5adcbea1f7b311b8bb15c17b3dc4742ae8fff3af and verify elephant checksums/metadata.
3. Preserve the adapted Assets/DigiPhant. Keep Tracking and DigiPhantStarter beside Assets. Python must be DigiPhantStarter/Tracking/.venv/Scripts/python.exe on Windows or .venv/bin/python elsewhere. Install pinned dependencies and hand model.
4. Add this root in Unity Hub. Wait for import/compilation. Open Assets/StudentWork/Scenes/Digiphant-Kart.unity: it already contains the rig, race, player/rivals, course, obstacles, pickups, follow camera, tracking and recorder. It is first in Build Settings. Do not regenerate it as an installation step.
5. Run DigiPhant > Kart > Validate scene, test Keys in Game view, then perform actual camera tests using the README framing/calibration/role mapping.

## Integrating into another scene

Prefer duplicating the working kart scene through Unity into Assets/StudentWork/Scenes and adapting that copy. For another project, make a recoverable copy, import required Assets with every .meta, restore Elephant, and reconcile packages/pipeline/input. Avoid duplicate DigiPhant classes and do not blindly replace global settings. Launcher paths are relative to Application.dataPath/../.

Inspect KartBuilder.cs, KartInputSetup.cs, and the serialized scene before manual wiring. Preserve DigiPhantController's nine bone controls and valid targets, DigiPhantLocomotion, DigiPhantCameraPreview, DigiPhantRecording, KartRace/KartCameraInput, KartVehicle, KartTrack, and KartFollowCamera references. Kart owns physics travel via locomotion externalMotion; vendor Elephant/Animator/CharacterController ownership must not conflict. Preserve URP materials, generated road, colliders, ordered checkpoints, pickups, and trunk rig. The scenery-only Savannah prefab does not constitute the race game.

For puppet-only work, duplicate Assets/DigiPhant/Scenes/DigiPhant.unity. Create New Template Scene refuses an existing reference: preserve it. Add Locomotion To Open Scene is an integration tool, not a full kart installer. Inspect generator source before invoking on saved copies.

## Acceptance and handoff

Check imports/tests, Unity Console, missing scripts/references/materials, validation, and keyboard actions. Confirm one bridge/camera session and localhost UDP 5055–5057. Test asymmetric mirror orientation, neutral countdown, P1 hand driving and foot lifts, reversed P2 shoulder steering, and P3 fist/pinch-release together. Test tracking recovery and save/reload. Stop processes you start. Report checks actually performed and remaining target-hardware/performer work. Editor auto-launch does not establish standalone build support.

Suggested prompt: Read AGENTS.md and AGENT_SETUP.md, restore this project in the required Unity version, open the saved kart scene, preserve metadata/controls, validate available checks, and document remaining physical tests. For scene integration, work on a saved copy and preserve existing work.
