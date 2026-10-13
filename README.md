# Chillin-Digiphant

DigiPhant Kart / Rainbow Savannah is a Unity elephant racing game with local webcam tracking for team play and keyboard controls for solo play.

**New to Unity?** Follow the [step-by-step guide to downloading and playing DigiPhant Kart](SIMPLIFIED_GUIDE.md). No programming experience needed.

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
- Use even front lighting and a securely placed camera facing the group. Adjust distance using the preview, rather than a fixed measurement: P1 must fit shoulders, wrists, and hips into the left zone; P2 shoulders fit the center; P3 hand fits the right.
- Leave room for hand raises without contacting people or equipment. The default layout works seated or standing; feet need not be visible.

## Team camera controls

Choose **Camera**, three performers, and **Body + shoulders + hand**. Arrange **P1 / P2 / P3 from left to right in the displayed preview**. Camera selection, mirror, sensitivity, and alternatives are under Camera settings & diagnostics. Check orientation with a raised left hand. Mirror/layout/reassignment changes require new neutral calibration.

Choose **Start with Camera**. Sit or stand upright with P1 hands down and P2 shoulders relaxed during the ten-second neutral countdown; the race starts afterward. For practice, use Start Camera and Set Neutral separately. Follow missing-input guidance while waiting.

| Role | Action | Result |
| --- | --- | --- |
| P1: body, left | Raise LEFT / RIGHT hand | Accelerate / brake then reverse |
| P2: shoulders, center | Raise LEFT / RIGHT shoulder | Turn right / turn left (default reversed steering) |
| P2 | Relax shoulders | Straighten |
| P3: hand, right | Thumbs up: thumb pointing upward, other fingers curled; open the hand before repeating | Jump once |
| P3 | Fist / open hand | Crouch / stand when clear |
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

## Race music and sounds

An original **Rainbow Savannah** synth-and-bell track plays quietly in practice,
then builds during the race. Lap two slightly increases its pace. The final lap
crossfades into a faster **160 BPM** performance (normal: **136 BPM**) with extra
percussion and a high countervoice; it accelerates a little more as you approach
the finish. Each finish-line crossing has a distinct lap/final-lap/victory cue
and banner. The race music fades behind the victory fanfare.

Countdown/GO, jump/landing, duck/stand, orange pickup/full basket, trunk windup/
release, direct hits, orange impacts, banana slips, boosts, bumps, braking,
checkpoint recovery and pause/resume have sound feedback. Motor pitch follows
speed and throttle, and fast steering adds subtle friction. Action captions
accompany cues. Sounds use the same gameplay events in Camera and Keys mode.

The sidebar's **Audio** section has music volume, action-sound volume, and mute.
Pause or lost driving tracking suspends playback; reset returns to the normal
track. The bundled score and cues are original synthesized audio, inspired by
bright celestial arcade racing, with no Nintendo recordings or melodies.

## More challenges each lap

The course grows when **your team** completes a lap:

| Lap | Jump and duck challenges | Banana peels |
| --- | --- | --- |
| 1 / practice | Original log and low canopy | None |
| 2 | Two logs and two low canopies | Six peels across two marked crossings |
| 3 | Three logs and three low canopies | Twelve peels across four marked crossings |

The original steering/slalom sections stay the same. Jump over yellow banana
peels; touching one triggers the crash wobble and dizzy stars, with about a
one-second slowdown before normal play resumes. Used peels return after five
seconds. New hazards wait if a racer is too close to their spawn position.
Reset or restart returns to the original first-lap course.

The elephant stores **up to three oranges**. Each item box adds one until full;
each throw uses one. The three fruit slots and `ORANGES 0 / 3` counter show what
is stored, including during cooldown or dizziness. Full storage still consumes
a contacted item box, and restarting empties storage.

## Setup and repository contents

- [Installation and troubleshooting](INSTALL.md)
- [Agent scene installation/setup](AGENT_SETUP.md) and [working rules](AGENTS.md)
- [Detailed kart guide](Assets/StudentWork/Kart/README.md)
- [Historical integration report](DIGIPHANT_SETUP.md): current README/source supersede its older controls.

Git includes authored scenes/scripts, required generated course assets, Unity metadata, Packages, ProjectSettings, tracking source/tests, and setup instructions. Caches, builds, logs, recordings, environments, downloaded models/assets, duplicate checkouts, and backup scenes stay local. Required Elephant assets restore from the [student starter](https://github.com/kommanderpi/studentstarter). Retain original third-party terms; this repository grants no new license for supplied assets/packages.

After import run **DigiPhant > Kart > Validate scene**, test Keys, then test the actual performers together in Game view. Automated checks do not establish physical gesture comfort, framing, or mirror correctness. Earlier games remain optional scenes.

## Group Reflection

| Question                                                                                        | Answer                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      |
| ----------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| What models did you select? + Why did you select them?                                          | For this project, we used GPT Astra 6 on extra high mode to ensure that our process was as efficient and smooth as possible because connecting an LLM to a 3D scene requires a higher level of interpretation and processing to get right. Within our team, we also had the right plan to work with the model without any limits. This way we were able to build out exactly what we intended and received a well-done execution.<br><br>For the MediaPipe models we chose to use the heuristic pose landmarker such that we could fit three people within the same scene but delegate the sensing to be of full-body pose, face, and hand separately. We did not find a method for the camera to segment one view into three different models (pose + facemesh + hand model), and so the heuristic pose landmarker was our best option.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    |
| How do they work? | The LLM takes our written instructions and project code as context and generates responses based on patterns learned during training. We used it to help translate our game ideas into Unity scripts, explain errors, and revise the implementation as we tested it.<br><br>MediaPipe processes the webcam feed frame by frame and estimates landmarks, which are key points on the body or hand. Our code uses their relative positions to measure movements, such as a raised wrist, uneven shoulders, or fingers forming a fist or pinch. We assign these signals to each player using their position in the camera frame, then send the tracking data to Unity. Calibration establishes a neutral starting position, and rule-based checks turn the signals into actions like accelerating, steering, jumping, ducking, and throwing. This lets the players control the elephant together through their movements. |
| What technical challenges did you face when integrating specific MediaPipe tools?               | The biggest challenge we had was to calibrate the camera such that it could detect all three people within the same frame and assign the roles accurately and consistently. Sometimes it would only detect two of three people or start assigning roles before all three people were detected, and that caused a lot of complications.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      |
| How did you overcome those challenges? <br>                                                     | We ended with a strategy where we divided the frame into three sections. Each section had its own boundary, and as long as the person stayed within that boundary, the detector would be able to assign the correct role to them. And we made a rule where from left to right it would always be in the sequence of P1, P2, P3.<br><br>There is also a disconnection between the way people would move in real life versus how it would be displayed on the camera because of the way the laptop is pointing at us. If we raised our left hand, our left hand would show on the right side of the screen because the camera didn't process the mirroring, and so we actually had to code that into the camera function to make sure that when we were looking at the screen, it was exactly a mirror of our own actions.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    |
| What signals did you choose to operationalize?                                                  | We chose to make the movements as intuitive as possible because there is a reaction time required to interact with the elephant and the obstacles, and if we made the movements as intuitive as possible, then it would be easier on both the users and the MediaPipe control to recognize and process.<br><br>For player one, they would be standing and using both hands to control the forward and backwards motion of the elephant. So if they raised their left hand, the elephant would go forward. If they raised their right hand, the elephant would start going backwards. We also added a jump such that if they raised one foot off the ground, similar to a real jumping motion, then the elephant would jump.<br><br>For player two, we recognize that the steering motion is probably the hardest to get right within the game as it's sensitive and requires a higher level of precision. So for player 2, we simplified it down to shoulder movements that are aligned with the way a real driver would slightly tilt their body.<br><br>For player 3, their job was to duck under obstacles and to throw objects at other elements within the game, and so player 3 would use their hand to control the movement through recognized gestures, and it allowed for very quick movements and response times. |
| What parts of your setup did you push to GIT to make your Digiphant setup accessible to others? | We pushed the Unity scenes, scripts, required course assets, metadata, package settings, and tracking code to GitHub. We also included installation instructions, camera positioning and gesture controls, and agent setup guidance. Large third-party assets are restored through a setup script, while caches, recordings, and local environments are excluded.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           |
