using System;
using System.Linq;
using ChillinDigiphant.PeelPatrol;
using ChillinDigiphant.Savanna;
using DigiPhant;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChillinDigiphant.Kart
{
    public enum RacePhase { Practice, Countdown, Racing, Finished }
    [DefaultExecutionOrder(-50)]
    public sealed partial class KartRace : MonoBehaviour
    {
        public const int RoadLayer = 8;
        public DigiPhantController controller;
        public DigiPhantLocomotion locomotion;
        public KartTrack track;
        public KartVehicle player;
        public KartVehicle[] racers;
        public Transform[] refillBays;
        public KartObstacle[] obstacles = Array.Empty<KartObstacle>();
        [HideInInspector] public bool testCrouch;
        public Material orangeMaterial;
        public int totalLaps = 3;
        public bool showMediaPipePanel = true;
        [HideInInspector] public bool keyboardTest = true;
        public RacePhase Phase { get; private set; }
        public bool Paused { get; private set; }
        public bool TrackingBlocked { get; private set; }
        public bool CameraConnected { get; private set; }
        public bool CameraMode => controller.inputMode == InputMode.Camera;
        public bool CanDrive => (Phase == RacePhase.Practice || Phase == RacePhase.Racing) && !Paused && !TrackingBlocked;
        public bool IsRacing => Phase == RacePhase.Racing;
        public int ActionPerformer => controller.performerCount;
        public float Elapsed { get; private set; }
        public float Countdown { get; private set; }
        public string MovementStatus { get; private set; } = "Ready";
        public string ActionStatus { get; private set; } = "Pick up orange boxes on the road";
        public string Message { get; private set; } = "Welcome to Rainbow Savannah / practice or start a race";
        public float SidebarPixels => showMediaPipePanel ? Mathf.Min((CameraMode ? Mathf.Max(320, GamePreview.size.x + 58) : 320) * UiScale, Screen.width * .55f) : 0;
        public float UiScale => Mathf.Clamp(Screen.height / 850f, .65f, 1.7f);
        public DigiPhantCameraPreview Preview => controller.GetComponent<DigiPhantCameraPreview>();
        public int Position => 1 + racers.Count(r => r != player && (r.Finished ? !player.Finished || r.FinishTime < player.FinishTime : !player.Finished && r.Progress > player.Progress));
        public float RefillProgress => refillHold.Progress;
        public float ThrowProgress => throwGesture.Progress;
        readonly ThrowMotion throwGesture = new ThrowMotion();
        readonly InteractionHold refillHold = new InteractionHold();
        Vector3[] gridPositions;
        Quaternion[] gridRotations;
        float messageUntil;
        public KartTrunkThrow PlayerTrunk { get; private set; }
        PhysicsMaterial orangePhysics;
        bool jumpArmed, gestureDuck;
        public KartObstacle NextObstacle
        {
            get
            {
                KartObstacle best=null;float nearest=36;
                foreach(var obstacle in obstacles)if(obstacle && obstacle.InApproach(player.Body.position,36))
                { float distance=obstacle.Ahead(player.Body.position);if(distance>-3 && distance<nearest){nearest=distance;best=obstacle;} }
                return best;
            }
        }
        void Awake()
        {
            // Old scenes may have saved slider mode. Interactive non-camera play always uses keys.
            keyboardTest = true;
            controller.showControls = false; locomotion.externalMotion = true; locomotion.followElephant = false;
            if (Preview) { Preview.embeddedInterface = true; Preview.autoStartBridge = false; Preview.showPreview = false; }
            gridPositions = racers.Select(r => r.transform.position).ToArray();
            gridRotations = racers.Select(r => r.transform.rotation).ToArray();
            PlayerTrunk = player.gameObject.AddComponent<KartTrunkThrow>();
            if (!PlayerTrunk.Initialize(player)) Debug.LogError("Player trunk rig is missing; trunk throws are unavailable.", player);
            orangeIndicatorHead = controller.controls.FirstOrDefault(c => c.label == "Head turn")?.bones?.FirstOrDefault();
            AttachCameraInput();
        }
        void Start() { ResetRace(); if (!CameraMode) SelectKeys(); }
        void OnDisable() { CancelCameraStart(); Time.timeScale = 1; DetachCameraInput();cameraInput.Reset(); if (PlayerTrunk) PlayerTrunk.Cancel(); }
        void OnDestroy() { if (orangePhysics) Destroy(orangePhysics); if (orangeReadyIcon) Destroy(orangeReadyIcon); }
        void Update()
        {
            var keys = Keyboard.current;
            if (keys != null)
            {
                if (keys.tabKey.wasPressedThisFrame) showMediaPipePanel = !showMediaPipePanel;
                if (keys.enterKey.wasPressedThisFrame) StartRace();
                if (keys.rKey.wasPressedThisFrame) ResetRace();
                if (keys.pKey.wasPressedThisFrame || keys.escapeKey.wasPressedThisFrame) TogglePause();
                if (keys.backspaceKey.wasPressedThisFrame) { player.Recover(); locomotion.followCamera.GetComponent<KartFollowCamera>().Snap(); }
            }
            float now = Time.realtimeSinceStartup;
            if (CameraMode) controller.PollCamera(now);
            RefreshTracking(now);
            AdvanceCameraStart(now);
            Time.timeScale = Paused || TrackingBlocked ? 0 : 1;
            locomotion.enableLocomotion = CanDrive;
            float forward = keyboardTest ? 0 : locomotion.testForward, turn = keyboardTest ? 0 : locomotion.testSteering;
            if (CameraMode)
            {
                locomotion.forward.TryRead(controller, now, out forward);
                locomotion.steering.TryRead(controller, now, out turn);
                if(MixedInputs)turn=cameraInput.ShouldersVisible(now)?cameraInput.Steering*(invertShoulderSteering?-1:1):0;
            }
            else if (keyboardTest && keys != null)
            {
                forward = ((keys.wKey.isPressed || keys.upArrowKey.isPressed) ? 1 : 0) - ((keys.sKey.isPressed || keys.downArrowKey.isPressed) ? 1 : 0);
                if (forward > 0 && !keys.leftShiftKey.isPressed) forward *= .8f;
                turn = ((keys.dKey.isPressed || keys.rightArrowKey.isPressed) ? 1 : 0) - ((keys.aKey.isPressed || keys.leftArrowKey.isPressed) ? 1 : 0);
                locomotion.testForward = forward; locomotion.testSteering = turn;
            }
            player.SetInput(CanDrive ? forward : 0, CanDrive ? turn : 0);
            if (CanDrive)
            {
                if (CameraMode)
                {
                    if(MixedInputs){if(cameraInput.TakeThrow(now))RequestThrow();}
                    else
                    {
                        bool left = ReadHand(Movement.LeftHandHeight, now, out float toss);
                        if (throwGesture.Step(toss, left, false, Time.deltaTime) && RequestThrow()) throwGesture.ConfirmShot();
                    }
                    ReadObstacleGestures(now);
                }
                else if (keyboardTest && keys != null)
                {
                    if (keys.spaceKey.isPressed) RequestThrow();
                    player.SetCrouch(keys.cKey.isPressed || keys.leftCtrlKey.isPressed);
                    if(keys.eKey.wasPressedThisFrame || keys.jKey.wasPressedThisFrame)player.RequestJump();
                }
                else player.SetCrouch(testCrouch);
            }
            else { throwGesture.Reset(); refillHold.Reset();jumpArmed=gestureDuck=false;cameraInput.ResetActions();player.SetCrouch(false); }
            if (!Paused && !TrackingBlocked)
            {
                if (Phase == RacePhase.Countdown) { Countdown -= Time.deltaTime; if (Countdown <= 0) { Phase = RacePhase.Racing; Say("GO! / follow the luminous arrows"); } }
                if (Phase == RacePhase.Racing) Elapsed += Time.deltaTime;
            }
            if (now > messageUntil) Message = "";
            var camera = locomotion.followCamera;
            if (camera) { float left = SidebarPixels / Mathf.Max(1, Screen.width); camera.rect = new Rect(left, 0, 1 - left, 1); }
        }
        public void RefreshTracking(float now)
        {
            if (!CameraMode)
            {
                TrackingBlocked = false;
                MovementStatus = "Solo keyboard play / no camera needed";
                ActionStatus = "SPACE throws / E jumps / hold C to duck";
                return;
            }
            bool travel = locomotion.forward.TryRead(controller, now, out _), steering = locomotion.steering.TryRead(controller, now, out _);
            if(MixedInputs)steering=controller.IsCalibrated && cameraInput.Calibrated && cameraInput.ShouldersVisible(now);
            TrackingBlocked = CameraMode && !(travel && steering);
            MovementStatus = !controller.IsCalibrated ? "Set neutral pose with BOTH hands lowered" : !travel ? "P1: show both wrists and shoulders" : !steering ? "P" + Mathf.Min(2, controller.performerCount) + ": show both wrists and shoulders" : "Movement ready / accelerate and steer together";
            bool left = ReadHand(Movement.LeftHandHeight, now, out _), right = ReadHand(Movement.RightHandHeight, now, out _);
            ActionStatus = !CameraMode ? "SPACE throws / E jumps / hold C to duck" : !left ? "Show and calibrate your LEFT wrist to throw" : !right ? "LEFT throw ready / show RIGHT wrist to jump" : "LEFT held = throw / raise RIGHT = jump / lean RIGHT = duck";
            if(CameraMode)
            {
                if(MixedInputs)
                {
                    bool driveVisible=controller.IsMovementVisible(1,Movement.LeftHandHeight,now) && controller.IsMovementVisible(1,Movement.RightHandHeight,now);
                    MovementStatus=!controller.IsCalibrated ? !driveVisible ? "P1: show both wrists and shoulders for neutral" :
                        !cameraInput.ShouldersVisible(now) ? "P"+SteeringPerformer+": show both shoulders for steering" :
                        literalBodyActions && !cameraInput.BodyVisible(now) ? "P"+BodyPerformer+": keep BOTH feet in view for neutral" :
                        "Set neutral: hands down, body upright, shoulders relaxed" : !travel ? "P1: show wrists and shoulders" :
                        !steering ? "P"+SteeringPerformer+": show both shoulders / steering lost" : "Body + shoulders driving ready";
                }
                ActionStatus=CameraActionStatus(now);
            }
        }
        public void ReadObstacleGestures(float now)
        {
            if(literalBodyActions)
            {
                player.SetCrouch(cameraInput.IsDucking(now));
                if(cameraInput.TakeJump(now))player.RequestJump();
                return;
            }
            bool right=ReadHand(Movement.RightHandHeight,now,out float height);
            if(!right)jumpArmed=false;
            else if(height<.25f)jumpArmed=true;
            else if(height>.65f && jumpArmed){jumpArmed=false;player.Jump();}
            bool lean=controller.TryReadMovement(ActionPerformer,Movement.Lean,now,out float tilt);
            gestureDuck=lean && tilt>(gestureDuck?.18f:.32f);
            player.SetCrouch(MixedInputs ? cameraInput.IsDucking(now) : gestureDuck);
        }
        bool ReadHand(Movement hand, float now, out float value) => controller.TryReadMovement(ActionPerformer, hand, now, out value) && controller.HandHeightCalibrated(ActionPerformer, hand);
        public void Say(string text) { Message = text; messageUntil = Time.realtimeSinceStartup + 3; }
        public void StartRace()
        {
            if (TrackingBlocked) { if (CameraMode) StartWithCamera(); else Say(MovementStatus); return; }
            ResetRace(); Phase = RacePhase.Countdown; Countdown = 3; Say("GET READY / three laps");
        }
        public void ResetRace()
        {
            CancelCameraStart();
            if (PlayerTrunk) PlayerTrunk.Cancel(false);
            Time.timeScale = 1; Paused = false; Phase = RacePhase.Practice; Elapsed = 0; Countdown = 0;
            for (int i = 0; i < racers.Length; i++) racers[i].ResetRace(gridPositions[i], gridRotations[i]);
            foreach (var item in FindObjectsByType<KartPickup>(FindObjectsSortMode.None)) item.ResetPickup();
            foreach (var shot in FindObjectsByType<KartProjectile>(FindObjectsSortMode.None)) Destroy(shot.gameObject);
            foreach (var effect in FindObjectsByType<KartImpactBurst>(FindObjectsSortMode.None)) Destroy(effect.gameObject);
            locomotion.StopMotion(); throwGesture.Reset(); refillHold.Reset();
            testCrouch=jumpArmed=gestureDuck=false;
            cameraInput.ResetActions();
            locomotion.followCamera.GetComponent<KartFollowCamera>().Snap();
            Say("PRACTICE / pick up items, or press ENTER to race");
        }
        public void OnFinish(KartVehicle vehicle)
        {
            if (vehicle != player || !player.isPlayer) return;
            Phase = RacePhase.Finished; Say("FINISH! / position " + Position + " of " + racers.Length);
        }
        public void TogglePause() { CancelCameraStart(); Paused = !Paused; throwGesture.Reset(); refillHold.Reset();cameraInput.ResetActions(); }
        public bool RequestThrow() => player.Throw();
        public int CurrentBay()
        {
            for (int i = 0; i < refillBays.Length; i++) if (Vector3.Distance(player.Body.position, refillBays[i].position) < 4.5f) return i;
            return -1;
        }
        public bool Refill()
        {
            if (CurrentBay() < 0 || !CanDrive) return false;
            bool filled = player.Collect(KartVehicle.Capacity); if (filled) Say("BASKET REFILLED / 3 oranges"); return filled;
        }
        public void Launch(KartVehicle owner)
        {
            Vector3 forward = owner.Body.rotation * Vector3.forward;
            CreateOrange(owner, owner.Body.position + forward * 3.4f + Vector3.up * 1.6f,
                forward * (21 + Mathf.Max(0, owner.Speed) * .5f) + Vector3.up * 3.2f);
        }
        public KartProjectile LaunchFromTrunk(KartVehicle owner, Vector3 grip)
        {
            Vector3 forward = owner.Body.rotation * Vector3.forward;
            float speed = 21 + Mathf.Max(0, owner.Speed) * .5f;
            // Aim back toward the driving line from the visible sideward trunk flick.
            Vector3 aim = owner.Body.position + forward * 12 + Vector3.up * 1.6f;
            float flight = Vector3.ProjectOnPlane(aim - grip, Vector3.up).magnitude / speed;
            Vector3 velocity = (aim - grip) / Mathf.Max(.1f, flight) - Physics.gravity * flight * .5f;
            return CreateOrange(owner, grip, velocity);
        }
        KartProjectile CreateOrange(KartVehicle owner, Vector3 origin, Vector3 velocity)
        {
            var shot = GameObject.CreatePrimitive(PrimitiveType.Sphere); shot.name = "Thrown orange / " + owner.racerName;
            shot.transform.localScale = Vector3.one * .65f;
            shot.transform.position = origin;
            shot.GetComponent<Renderer>().sharedMaterial = orangeMaterial;
            var body = shot.AddComponent<Rigidbody>(); body.mass = .8f; body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.linearVelocity = velocity;
            if (!orangePhysics) orangePhysics = new PhysicsMaterial("Orange bounce") { bounciness = .55f, dynamicFriction = .2f, staticFriction = .2f };
            shot.GetComponent<Collider>().sharedMaterial = orangePhysics;
            var projectile = shot.AddComponent<KartProjectile>(); projectile.owner = owner;
            foreach (var collider in owner.GetComponentsInChildren<Collider>()) Physics.IgnoreCollision(shot.GetComponent<Collider>(), collider);
            return projectile;
        }
        public void SetMode(int mode)
        {
            CancelCameraStart();
            if (PlayerTrunk) PlayerTrunk.Cancel();
            // 1 remains an internal simulation hook for the existing physics checks.
            // The player-facing selector exposes Camera (0) and Keys (2) only.
            controller.SetInputMode(mode == 0 ? InputMode.Camera : InputMode.TestSliders);
            keyboardTest = mode == 2; locomotion.StopMotion(); throwGesture.Reset(); refillHold.Reset();
            testCrouch=jumpArmed=gestureDuck=false;player.SetCrouch(false);
            player.SetInput(0, 0);
            if (mode != 0) DisconnectCamera(); RefreshTracking(Time.realtimeSinceStartup);
            Time.timeScale = Paused || TrackingBlocked ? 0 : 1;
        }
        public void SelectKeys()
        {
            SetMode(2);
            FocusKeyboardPlay();
        }
        public void SelectCamera()
        {
            if (controller.performerCount < 2) UsePerformers(3);
            SetMode(0);
        }
        public void FocusKeyboardPlay()
        {
            if (Event.current != null) GUIUtility.keyboardControl = 0;
#if UNITY_EDITOR
            // A docked EditorWindow otherwise owns keyboard focus after clicking Keys.
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (!this || !Application.isPlaying || CameraMode) return;
                var gameView = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
                var window = UnityEditor.EditorWindow.GetWindow(gameView);
                window.Show(); window.Focus();
            };
#endif
        }
        public void ConnectCamera()
        {
            SetMode(0); if (Preview) { Preview.embeddedInterface = true; Preview.showPreview = true; Preview.autoStartBridge = true; Preview.BeginPreview(); CameraConnected = true; }
        }
        public void DisconnectCamera()
        {
            CancelCameraStart();
            if (Preview) { Preview.autoStartBridge = false; Preview.StopPreview(); Preview.showPreview = false; }
            CameraConnected = false; controller.InvalidateCalibration();
        }
        public void UsePerformers(int count)
        {
            CancelCameraStart();
            controller.SetPerformerCount(count);
            MovementSource Hand(int p, Movement m, float sign) => new MovementSource { performer = p, movement = m, weight = sign, positiveOnly = true };
            locomotion.forward.combination = locomotion.steering.combination = SignalCombination.Sum;
            locomotion.forward.sources = new[] { Hand(1, Movement.LeftHandHeight, 1), Hand(1, Movement.RightHandHeight, -1) };
            locomotion.steering.sources = new[] { Hand(Mathf.Min(2, count), Movement.LeftHandHeight, -1), Hand(Mathf.Min(2, count), Movement.RightHandHeight, 1) };
        }
    }
}
