using System;
using System.Collections.Generic;
using System.Linq;
using ChillinDigiphant.Savanna;
using DigiPhant;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChillinDigiphant.PeelPatrol
{
    [Serializable] public class BananaLane
    {
        public string title;
        public Transform[] path;
        public GameObject barricade;
        [NonSerialized] public float durability;
    }
    [DefaultExecutionOrder(150)]
    public sealed partial class PeelPatrolGame : MonoBehaviour
    {
        public DigiPhantController controller;
        public DigiPhantLocomotion locomotion;
        public SavannaHabitat habitat;
        public Transform trunkTip, orangeStation, workshop, cleanStation;
        public Renderer waterRenderer;
        public BananaRaider bananaPrefab;
        public OrangeFlight orangePrefab;
        public BananaLane[] lanes;
        public Transform targetRing;
        public float throwRange = 18;
        public float stationRadius = 3.1f;
        public ElephantLoadout loadout;
        public DefenseRound Round { get; } = new DefenseRound();
        public bool Paused { get; private set; }
        public int EnemyCount => enemies.Count;
        public IReadOnlyList<BananaRaider> Raiders => enemies;
        public int ProjectileCount => projectiles.Count;
        public int ActiveStation { get; private set; } = -1;
        public float StationProgress => stationHold.Progress;
        public bool CameraMode => controller.inputMode == InputMode.Camera;
        public bool TrackingBlocked { get; private set; }
        public bool ActionsBlocked { get; private set; }
        public bool ThrowBlocked { get; private set; }
        public bool GameplayEnabled => Round.Active || (Round.Phase==RoundPhase.Ready && PracticeMode);
        public string MovementStatus { get; private set; } = "Waiting for calibration";
        public string ActionStatus { get; private set; } = "Waiting for calibration";
        public bool ShopOpen { get; private set; }
        public bool SimulationPaused => Paused || ShopOpen || (CameraMode && TrackingBlocked);
        public bool PracticeMode { get; private set; } = true;
        public bool KeyboardTest { get; private set; } = true;
        public bool CameraConnected => cameraConnected;
        public int ActionPerformer => controller.performerCount;
        public bool showMediaPipePanel = true;
        public PeelPatrolMediaPipePanel ControlPanel { get; private set; }
        public float SidebarPixels => showMediaPipePanel ? Mathf.Min(360*Mathf.Clamp(Screen.height/850f,.6f,1.8f),Screen.width*.32f) : 0;
        readonly List<BananaRaider> enemies = new List<BananaRaider>();
        readonly List<OrangeFlight> projectiles = new List<OrangeFlight>();
        readonly List<(Transform item,float life)> bursts = new List<(Transform,float)>();
        readonly InteractionHold stationHold = new InteractionHold();
        readonly ThrowMotion throwGesture = new ThrowMotion();
        public float ThrowProgress => throwGesture.Progress;
        public bool AutoThrowing => CameraMode && GameplayEnabled && !SimulationPaused && !ThrowBlocked && Round.Ammo>0 && throwGesture.IsFiring;
        public float NextThrowSeconds => Mathf.Max(shotCooldown,CameraMode?throwGesture.Cooldown:0);
        Transform statusHead;
        SkinnedMeshRenderer[] statusMeshes;
        Renderer[] statusCargo;
        MaterialPropertyBlock waterTint;
        Color cleanColor;
        float shotCooldown, pulse, flash, messageTime, clock;
        string message = "";
        BoneControl trunk;
        bool cameraConnected;
        float baseWalk, baseRun, baseBackward;
        [NonSerialized] GUIStyle heading, label, small, big, button, smallButton;
        public BananaRaider Target { get; private set; }

        void OnEnable() { heading=label=small=big=button=smallButton=null; }

        void Awake()
        {
            waterTint = new MaterialPropertyBlock();
            cleanColor = waterRenderer.sharedMaterial.GetColor("_BaseColor");
            trunk = controller.controls.FirstOrDefault(c=>c.label=="Trunk curl");
            var head=controller.controls.FirstOrDefault(c=>c.label=="Head turn");
            statusHead=head?.bones?.FirstOrDefault(b=>b);
            statusMeshes=locomotion.elephantAnimator.GetComponentsInChildren<SkinnedMeshRenderer>();
            statusCargo=loadout?loadout.oranges.SelectMany(o=>o.GetComponentsInChildren<Renderer>()).ToArray():Array.Empty<Renderer>();
            baseWalk=locomotion.walkSpeed; baseRun=locomotion.runSpeed; baseBackward=locomotion.backwardSpeed;
            ControlPanel=GetComponent<PeelPatrolMediaPipePanel>();
            if(!ControlPanel) ControlPanel=gameObject.AddComponent<PeelPatrolMediaPipePanel>();
            ControlPanel.game=this;
            controller.showControls=false;
            var preview=controller.GetComponent<DigiPhantCameraPreview>();
            if(preview) preview.embeddedInterface=true;
            ConfigureHandControls();
        }
        void Start() { ResetRound(); }
        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.enterKey.wasPressedThisFrame)
                { if (Round.Phase == RoundPhase.Won || Round.Phase == RoundPhase.Lost) ResetRound(); StartRound(); }
                if (keyboard.rKey.wasPressedThisFrame) ResetRound();
                if (keyboard.pKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame) TogglePause();
                if (keyboard.tabKey.wasPressedThisFrame) showMediaPipePanel=!showMediaPipePanel;
                if (keyboard.uKey.wasPressedThisFrame && Round.Active) ToggleShop();
            }
            if(CameraMode) { controller.PollCamera(Time.realtimeSinceStartup); RefreshTracking(Time.realtimeSinceStartup); }
            UpdatePreview();
            bool active = (Round.Active || (Round.Phase==RoundPhase.Ready && PracticeMode)) && !SimulationPaused;
            locomotion.enableLocomotion = active;
            if (!active) { locomotion.StopMotion(); return; }
            if (controller.inputMode == InputMode.TestSliders && KeyboardTest && keyboard != null)
            {
                float forward = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1:0) - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1:0);
                float turn = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1:0) - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1:0);
                locomotion.testForward = forward * (keyboard.leftShiftKey.isPressed ? 1 : .75f);
                locomotion.testSteering = turn;
                if (keyboard.spaceKey.isPressed) RequestThrow();
                pulse = Mathf.Max(0,pulse-Time.deltaTime);
                if (trunk != null) trunk.testValue = Mathf.Clamp01(pulse * 5);
            }
        }
        void LateUpdate()
        {
            ApplyGameViewport();
            if(PracticeMode && !SimulationPaused) AvoidTrees();
            if (!GameplayEnabled || SimulationPaused) { if (targetRing) targetRing.gameObject.SetActive(false); return; }
            AvoidTrees();
            var keys = Keyboard.current;
            if(CameraMode) StepCameraInput(Time.realtimeSinceStartup,Time.deltaTime);
            else if(KeyboardTest) StepStation(keys != null && keys.eKey.isPressed,Time.deltaTime);
            Tick(Time.deltaTime);
        }
        void UpdatePreview()
        {
            var preview = controller.GetComponent<DigiPhantCameraPreview>();
            if(preview)
            {
                // The Connect button starts the bridge; simply entering Play does not open the webcam.
                preview.embeddedInterface=true;
                preview.showPreview = cameraConnected;
            }
        }
        public void SetControlMode(InputMode mode)
        {
            controller.SetInputMode(mode); TrackingBlocked=mode==InputMode.Camera;
            if(mode!=InputMode.Camera)
            {
                var preview=controller.GetComponent<DigiPhantCameraPreview>();
                if(preview) { preview.autoStartBridge=false; preview.StopPreview(); }
                cameraConnected=false;
            }
            controller.showControls=false; KeyboardTest=true; ResetGestures();
        }
        public void SelectSliders() { SetControlMode(InputMode.TestSliders); KeyboardTest=false; }
        public void SetPractice(bool value) { PracticeMode=value && Round.Phase==RoundPhase.Ready; ResetGestures(); }
        public void UsePerformers(int count)
        {
            controller.SetPerformerCount(count);
            ConfigureHandControls();
            RefreshTracking(Time.realtimeSinceStartup); ResetGestures();
        }
        public void ConfigureHandControls()
        {
            MovementSource Hand(int person,Movement hand,float sign) => new MovementSource{performer=person,movement=hand,weight=sign,positiveOnly=true};
            int turnPerson=Mathf.Min(2,controller.performerCount);
            locomotion.forward.combination=locomotion.steering.combination=SignalCombination.Sum;
            locomotion.forward.sources=new[]{Hand(1,Movement.LeftHandHeight,1),Hand(1,Movement.RightHandHeight,-1)};
            locomotion.steering.sources=new[]{Hand(turnPerson,Movement.LeftHandHeight,-1),Hand(turnPerson,Movement.RightHandHeight,1)};
        }
        public void UseUpperBody(bool value)
        { controller.SetUpperBodyOnly(value); RefreshTracking(Time.realtimeSinceStartup); ResetGestures(); }
        public void BeginNeutralPose()
        { controller.BeginCalibrationCountdown(Time.realtimeSinceStartup); RefreshTracking(Time.realtimeSinceStartup); ResetGestures(); }
        public void ReassignPeople()
        { controller.ReassignPeople(); RefreshTracking(Time.realtimeSinceStartup); ResetGestures(); }
        public void ApplyGameViewport()
        {
            var camera=locomotion.followCamera;
            if(camera) {float left=SidebarPixels/Mathf.Max(1,Screen.width);camera.rect=new Rect(left,0,1-left,1);}
        }
        public void ConnectCamera()
        {
            SetControlMode(InputMode.Camera);
            var preview=controller.GetComponent<DigiPhantCameraPreview>();
            if(preview) { preview.autoStartBridge=true; preview.BeginPreview(); cameraConnected=true; }
            showMediaPipePanel=true;
        }
        public void DisconnectCamera()
        {
            var preview=controller.GetComponent<DigiPhantCameraPreview>();
            if(preview) { preview.autoStartBridge=false;preview.StopPreview();preview.showPreview=false; }
            cameraConnected=false;controller.InvalidateCalibration();RefreshTracking(Time.realtimeSinceStartup);ResetGestures();
        }
        bool ReadActionHand(Movement hand,float now,out float value)
        {
            bool visible=controller.TryReadMovement(ActionPerformer,hand,now,out value);
            return visible && controller.HandHeightCalibrated(ActionPerformer,hand);
        }
        public void RefreshTracking(float now)
        {
            bool travel=locomotion.forward.TryRead(controller,now,out float speed);
            bool steer=locomotion.steering.TryRead(controller,now,out float turn);
            TrackingBlocked=CameraMode && !(travel && steer);
            bool left=ReadActionHand(Movement.LeftHandHeight,now,out _),right=ReadActionHand(Movement.RightHandHeight,now,out _);
            ThrowBlocked=CameraMode && !left;
            ActionsBlocked=CameraMode && !(left || right);
            if(!controller.IsCalibrated) MovementStatus=controller.CalibrationPending?"Hold neutral pose until the countdown ends":"Set neutral pose first; visible people are not yet calibrated";
            else if(!travel) MovementStatus="P1: show BOTH wrists and shoulders (also hips in Full body)";
            else if(!steer) MovementStatus="P"+Mathf.Min(2,controller.performerCount)+": show BOTH wrists and shoulders (also hips in Full body)";
            else if(Paused) MovementStatus="Movement ready / round paused: press Resume";
            else if(ShopOpen) MovementStatus="Movement ready / close the upgrade shop to move";
            else if(Round.Phase==RoundPhase.Ready && !PracticeMode) MovementStatus="Enable Practice movements or start defense waves";
            else MovementStatus="Movement ready / travel "+speed.ToString("+0.00;-0.00;0.00")+" / steering "+turn.ToString("+0.00;-0.00;0.00");
            if(!controller.IsCalibrated) ActionStatus="Set neutral pose first";
            else if(!controller.IsTracked(ActionPerformer,now)) ActionStatus="P"+ActionPerformer+": not tracked";
            else if(!left) ActionStatus="Throw held: show/calibrate P"+ActionPerformer+" LEFT wrist; reload can work independently";
            else if(!right) ActionStatus="Throw ready / RIGHT wrist unavailable for reload";
            else ActionStatus="Hold LEFT to throw 1/sec; raise RIGHT at a station to reload/use";
            if(TrackingBlocked) ResetGestures();
            else if(ActionsBlocked) { stationHold.Reset();throwGesture.Reset(); }
        }
        public void StepCameraInput(float now,float dt)
        {
            RefreshTracking(now);
            if(!GameplayEnabled || SimulationPaused) { ResetGestures(); return; }
            if(ActionsBlocked) { stationHold.Reset();throwGesture.Reset(); return; }
            bool left=ReadActionHand(Movement.LeftHandHeight,now,out float toss);
            bool right=ReadActionHand(Movement.RightHandHeight,now,out float use);
            ActiveStation=GetStation(locomotion.travelRoot.position);
            // Only a deliberate right raise inside a station takes priority.
            bool usingStation=right && use>=.6f && ActiveStation>=0;
            if(dt<=0 || dt>.2f) { stationHold.Reset();throwGesture.Reset();return; }
            if(throwGesture.Step(toss,left,usingStation,dt) && Round.Ammo>0 && RequestThrow()) throwGesture.ConfirmShot();
            if(stationHold.Step(ActiveStation,right,use,dt,.35f)) UseStation(ActiveStation);
        }
        void ResetGestures() { locomotion.StopMotion(); stationHold.Reset(); throwGesture.Reset(); }
        public void StartRound()
        {
            if(CameraMode) RefreshTracking(Time.realtimeSinceStartup);
            if(CameraMode && TrackingBlocked) { Say(MovementStatus); return; }
            Round.Start(); PracticeMode=false; Paused=false; ResetGestures();
        }
        public void TogglePause() { if(Round.Active) { Paused=!Paused; ResetGestures(); } }
        public void ToggleShop() { if(Round.Active) { ShopOpen=!ShopOpen; ResetGestures(); } }
        public bool BuyUpgrade()
        {
            if(!Round.BuyUpgrade()) return false;
            ApplyLoadout(); Say(Round.Ride+" equipped! Speed +"+Mathf.RoundToInt((Round.SpeedMultiplier-1)*100)+"%."); return true;
        }
        void ApplyLoadout()
        {
            locomotion.walkSpeed=baseWalk*Round.SpeedMultiplier;
            locomotion.runSpeed=baseRun*Round.SpeedMultiplier;
            locomotion.backwardSpeed=baseBackward*Round.SpeedMultiplier;
            if(loadout) loadout.Display(Round.Ammo,Round.Upgrade);
        }
        void OnApplicationFocus(bool focused)
        { if(!focused && Round.Active) { Paused=true; ResetGestures(); } }
        public void ResetRound()
        {
            foreach(var e in enemies) if(e) Destroy(e.gameObject);
            foreach(var p in projectiles) if(p) Destroy(p.gameObject);
            foreach(var b in bursts) if(b.item) Destroy(b.item.gameObject);
            enemies.Clear(); projectiles.Clear(); bursts.Clear();
            Round.Reset(); PracticeMode=true; Paused=ShopOpen=false; ResetGestures(); Target=null; ActiveStation=-1;
            shotCooldown=pulse=clock=flash=messageTime=0;
            foreach(var lane in lanes) { lane.durability=0; lane.barricade.SetActive(false); }
            locomotion.ResetPosition(); controller.ResetControls(); controller.showControls=false;
            if(targetRing) targetRing.gameObject.SetActive(false);
            UpdateWater();
            ApplyLoadout(); RefreshTracking(Time.realtimeSinceStartup);
        }
        public bool RequestThrow()
        {
            if(!GameplayEnabled || SimulationPaused || shotCooldown>.0001f) return false;
            Target=FindTarget();
            if(!Round.Spend(1,PracticeMode)) { Say("Out of oranges! Reload at the orange grove."); return false; }
            var shot=Instantiate(orangePrefab,transform);
            Vector3 origin=trunkTip ? trunkTip.position : locomotion.travelRoot.position+Vector3.up*2;
            if(Target) shot.Launch(origin,Target);
            else shot.Launch(origin,locomotion.travelRoot.position+locomotion.travelRoot.forward*throwRange+Vector3.up*.2f);
            projectiles.Add(shot); shotCooldown=1; pulse=.35f; ApplyLoadout();
            return true;
        }
        public BananaRaider FindTarget()
        {
            BananaRaider best=null; float nearest=throwRange*throwRange;
            foreach(var e in enemies)
            {
                if(!e || !e.Alive) continue;
                // Do not spend another orange on a raider already covered by incoming shots.
                int incoming=projectiles.Count(p=>p && p.Target==e);
                if(incoming>=e.Health) continue;
                float distance=(e.transform.position-locomotion.travelRoot.position).sqrMagnitude;
                if(distance<nearest) { nearest=distance; best=e; }
            }
            return best;
        }
        public int GetStation(Vector3 position)
        {
            Transform[] stations={orangeStation,workshop,cleanStation};
            for(int i=0;i<stations.Length;i++)
            { var d=position-stations[i].position; d.y=0; if(d.sqrMagnitude<=stationRadius*stationRadius) return i; }
            return -1;
        }
        public void StepStation(bool held,float dt)
        {
            if(!GameplayEnabled || SimulationPaused) { stationHold.Reset(); return; }
            ActiveStation=GetStation(locomotion.travelRoot.position);
            if(stationHold.Step(ActiveStation,true,held?1:0,dt,.75f)) UseStation(ActiveStation);
        }
        public bool UseStation(int index)
        {
            if(!GameplayEnabled || SimulationPaused || index<0 || GetStation(locomotion.travelRoot.position)!=index) return false;
            if(index==0)
            {
                if(!Round.Refill(PracticeMode)) { Say("Your trunk basket is full."); return false; }
                ApplyLoadout(); Say("Basket full: 10 oranges. Carry them anywhere!"); return true;
            }
            if(!Round.Active) { Say("Start defense waves to build or clean; practice supports throwing and reloading.");return false; }
            if(index==1)
            {
                var lane=lanes.OrderBy(l=>l.durability).First();
                if(lane.durability>=40) { Say("All three barricades are at full strength."); return false; }
                if(!Round.Spend(4)) { Say("A barricade costs 4 oranges. Visit the grove first."); return false; }
                lane.durability=40; lane.barricade.SetActive(true); ApplyLoadout(); Say(lane.title+" barricade ready!"); return true;
            }
            if(!Round.CleanWater()) { Say(Round.Water==100?"The water is already clean.":"Cleaning needs 3 oranges."); return false; }
            if(habitat.splash) habitat.splash.Play(); Say("Fresh water! Cleanliness restored by 25."); UpdateWater(); ApplyLoadout(); return true;
        }
        public BananaRaider SpawnRaider(int lane,int wave)
        {
            var e=Instantiate(bananaPrefab,transform);
            e.name="Walking banana / "+lanes[lane].title;
            e.Initialise(lanes[lane].path.Select(t=>t.position).ToArray(),lane,wave); enemies.Add(e); return e;
        }
        public void Tick(float delta)
        {
            if(!GameplayEnabled || SimulationPaused) return;
            float dt=Mathf.Clamp(delta,0,.1f); clock+=dt; shotCooldown=Mathf.Max(0,shotCooldown-dt); messageTime-=dt; flash=Mathf.Max(0,flash-dt);
            var previousPhase=Round.Phase;
            if(Round.Tick(dt,enemies.Count)) SpawnRaider((Round.Spawned+Round.Wave-2)%lanes.Length,Round.Wave);
            if(previousPhase==RoundPhase.Wave && Round.Phase!=RoundPhase.Wave) Say("Wave cleared! +8 coins. Visit Upgrades to add some speed.");
            for(int i=projectiles.Count-1;i>=0;i--)
            {
                var shot=projectiles[i];
                if(!shot.Step(dt)) continue;
                var target=shot.Target;
                if(target && target.Alive)
                {
                    Burst(target.AimPoint);
                    if(target.Hit()) { enemies.Remove(target); Round.RegisterDefeat(); Say("Banana stopped! +3 coins"); Destroy(target.gameObject); }
                }
                else Burst(shot.transform.position);
                projectiles.RemoveAt(i); Destroy(shot.gameObject);
            }
            for(int i=enemies.Count-1;i>=0;i--)
            {
                var enemy=enemies[i]; var lane=lanes[enemy.Lane];
                bool blocked=lane.durability>0 && enemy.AtBarricade(lane.path[1].position);
                if(blocked)
                {
                    lane.durability=Mathf.Max(0,lane.durability-dt*6);
                    if(lane.durability==0) { lane.barricade.SetActive(false); Say(lane.title+" barricade broke!"); }
                }
                bool queued=enemies.Any(other=>other && other!=enemy && other.Lane==enemy.Lane &&
                    other.RemainingDistance<enemy.RemainingDistance && enemy.RemainingDistance-other.RemainingDistance<3);
                if(enemy.Step(dt,blocked || queued))
                {
                    Round.Contaminate(12); flash=.4f; UpdateWater();
                    enemies.RemoveAt(i); Destroy(enemy.gameObject);
                    if(!Round.Active) break;
                }
            }
            for(int i=bursts.Count-1;i>=0;i--)
            {
                var b=bursts[i]; b.life-=dt;
                if(b.life<=0) { Destroy(b.item.gameObject); bursts.RemoveAt(i); }
                else { b.item.position+=b.item.up*dt*2; b.item.localScale*=Mathf.Exp(-dt*4); bursts[i]=b; }
            }
            Target=FindTarget();
            if(targetRing)
            {
                targetRing.gameObject.SetActive(Target && Round.Active);
                if(Target) targetRing.position=new Vector3(Target.transform.position.x,.08f,Target.transform.position.z);
            }
            if(!GameplayEnabled) { locomotion.StopMotion(); locomotion.enableLocomotion=false; }
            if(habitat.waterRipples!=null)
                for(int i=0;i<habitat.waterRipples.Length;i++) habitat.waterRipples[i].localScale=Vector3.one*(.4f+Mathf.Repeat(clock*.18f+i*.33f,1)*1.7f);
        }
        void Burst(Vector3 position)
        {
            for(int i=0;i<5;i++)
            {
                var item=Instantiate(orangePrefab.gameObject,position,Quaternion.Euler(i*61,0,i*72),transform);
                Destroy(item.GetComponent<OrangeFlight>()); item.transform.localScale=Vector3.one*.17f;
                bursts.Add((item.transform,.45f));
            }
        }
        void UpdateWater()
        {
            waterTint.SetColor("_BaseColor",Color.Lerp(new Color(.47f,.42f,.19f),cleanColor,Round.Water/100f));
            waterRenderer.SetPropertyBlock(waterTint);
        }
        void AvoidTrees()
        {
            var position=locomotion.travelRoot.position;
            for(int i=0;i<habitat.obstacles.Length;i++)
            {
                var d=position-habitat.obstacles[i].position; d.y=0; float r=habitat.obstacleRadii[i];
                if(d.sqrMagnitude>=r*r) continue;
                if(d.sqrMagnitude<.001f) d=Vector3.back;
                var p=habitat.obstacles[i].position+d.normalized*r; position.x=p.x; position.z=p.z;
            }
            locomotion.travelRoot.position=position;
        }
        void Say(string text) { message=text; messageTime=2.4f; }

        void WorldLabel(Vector3 point,string text,float scale)
        {
            if(!Camera.main) return;
            var p=Camera.main.WorldToScreenPoint(point+Vector3.up*.3f); if(p.z<=0) return;
            var r=new Rect(p.x/scale-108,(Screen.height-p.y)/scale+18,216,25);
            Panel(r); var s=new GUIStyle(small){alignment=TextAnchor.MiddleCenter,fontSize=11}; GUI.Label(r,text,s);
        }
        static void Panel(Rect r) { GUI.color=new Color(.075f,.12f,.10f,.94f);GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=new Color(1,.96f,.83f); }
        static void Bar(Rect r,float value,Color color) { GUI.color=new Color(.22f,.28f,.23f);GUI.DrawTexture(r,Texture2D.whiteTexture);r.width*=Mathf.Clamp01(value);GUI.color=color;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white; }
    }
}
