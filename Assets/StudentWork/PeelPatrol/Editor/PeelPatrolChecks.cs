using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using DigiPhant;

namespace ChillinDigiphant.PeelPatrol.Editor
{
    [InitializeOnLoad]
    public static class PeelPatrolChecks
    {
        static double entered, nextPoll;
        static PeelPatrolChecks()
        {
            EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode) entered=EditorApplication.timeSinceStartup;};
            EditorApplication.update+=Update;
        }
        static void Check(bool value,string reason) { if(!value) throw new Exception(reason); }
        public static void CheckRules()
        {
            ThrowMotionChecks.Run();
            var r=new DefenseRound();
            Check(!r.Spend(1) && !r.Refill(),"Ready screen must not spend or reload.");
            r.Start();Check(r.Spend(6) && r.Ammo==0 && !r.Spend(1),"Ammo underflow.");
            Check(r.Refill() && r.Ammo==10 && !r.Refill(),"Refill must cap at ten.");
            Check(!r.CleanWater() && r.Ammo==10,"Full water must not consume oranges.");
            r.Contaminate(24);Check(r.CleanWater() && r.Water==100 && r.Ammo==7,"Cleaning cost or cap incorrect.");
            r.Contaminate(100);Check(r.Phase==RoundPhase.Lost,"Contaminated water must end the round.");
            Check(!r.Spend(1) && !r.Refill(),"Terminal game must reject actions.");
            r.Reset();r.Start();int spawned=0;
            for(int i=0;i<3000 && r.Active;i++) if(r.Tick(.1f,0)) spawned++;
            Check(r.Phase==RoundPhase.Won && r.Wave==3 && spawned==27,"Three-wave progression failed.");
            Check(r.Coins==24,"Wave bonuses must be awarded exactly once.");
            Check(!r.BuyUpgrade(),"Terminal round allowed a purchase.");
            r.Reset();Check(r.Phase==RoundPhase.Ready && r.Water==100 && r.Ammo==6 && r.Defeated==0,"Replay did not reset counters.");
            r.Start();Check(!r.BuyUpgrade(),"Upgrade purchased without coins.");
            for(int i=0;i<4;i++)r.RegisterDefeat();
            Check(r.Coins==12 && r.BuyUpgrade() && r.Coins==0 && r.Upgrade==1 && r.SpeedMultiplier==1.3f,"Skateboard economy failed.");
            for(int i=0;i<7;i++)r.RegisterDefeat();
            Check(r.BuyUpgrade() && r.Coins==1 && r.Upgrade==2,"Unicycle economy failed.");
            for(int i=0;i<9;i++)r.RegisterDefeat();
            Check(r.BuyUpgrade() && r.Coins==0 && r.Upgrade==3 && !r.BuyUpgrade(),"Jetpack economy/cap failed.");
            r.Reset();Check(r.Coins==0 && r.Upgrade==0 && r.SpeedMultiplier==1,"Replay retained upgrades.");
            r.Start();float time=0,last=-100;
            for(int i=0;i<600;i++) {time+=.1f;if(r.Tick(.1f,1)) {Check(time-last>=4.4f,"First-wave arrivals too close.");last=time;}}
            var wait=new DefenseRound();wait.Start();
            for(int i=0;i<700;i++) wait.Tick(.1f,1);
            Check(wait.Phase==RoundPhase.Wave && wait.Wave==1,"Wave cleared while an enemy remained.");
        }
        [MenuItem("DigiPhant/Peel Patrol/Run Play mode checks")]
        public static void Run()
        {
            PeelPatrolBuilder.Validate();SessionState.SetBool("PeelPatrolTests",true);
            if(!EditorApplication.isPlaying) EditorApplication.EnterPlaymode(); else entered=EditorApplication.timeSinceStartup;
        }
        static void Update()
        {
            if(EditorApplication.timeSinceStartup>=nextPoll)
            {
                nextPoll=EditorApplication.timeSinceStartup+1;
                if(EditorApplication.isPlaying && File.Exists(".tools/preview-peel-ride"))
                {
                    int tier=int.Parse(File.ReadAllText(".tools/preview-peel-ride"));File.Delete(".tools/preview-peel-ride");
                    var preview=UnityEngine.Object.FindFirstObjectByType<PeelPatrolGame>();
                    preview.SetControlMode(InputMode.TestSliders);preview.ResetRound();preview.StartRound();
                    for(int i=0;i<24;i++) preview.Round.RegisterDefeat();
                    for(int i=0;i<Mathf.Clamp(tier,0,3);i++)preview.BuyUpgrade();
                    preview.Round.Refill();preview.loadout.Display(10,preview.Round.Upgrade);
                    var camera=preview.locomotion.followCamera;camera.orthographicSize=7;
                    camera.transform.position=preview.locomotion.travelRoot.position+new Vector3(9,7,-10);
                    camera.transform.LookAt(preview.locomotion.travelRoot.position+Vector3.up*1.8f);
                    EditorApplication.isPaused=true;
                }
                if(!EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(".tools/test-peel-patrol"))
                {File.Delete(".tools/test-peel-patrol");try{Run();}catch(Exception e){Fail(e);}}
            }
            if(!EditorApplication.isPlaying || !SessionState.GetBool("PeelPatrolTests",false) || EditorApplication.timeSinceStartup-entered<2) return;
            SessionState.SetBool("PeelPatrolTests",false);
            try
            {
                var g=UnityEngine.Object.FindFirstObjectByType<PeelPatrolGame>();
                g.SetControlMode(InputMode.TestSliders);g.ResetRound();g.StartRound();
                Vector3 initial=g.locomotion.travelRoot.position;
                g.locomotion.enableLocomotion=true;g.locomotion.testForward=.75f;
                for(int i=0;i<10;i++) g.locomotion.Evaluate(Time.realtimeSinceStartup,.1f);
                Check(Vector3.Distance(initial,g.locomotion.travelRoot.position)>.5f,"Elephant movement failed.");g.locomotion.StopMotion();
                var raider=g.SpawnRaider(0,1);raider.transform.position=g.locomotion.travelRoot.position+Vector3.forward*5;
                Check(g.RequestThrow() && g.Round.Ammo==5,"Throw failed to consume exactly one orange.");
                Check(!g.RequestThrow(),"Throw cooldown failed.");
                for(int i=0;i<15;i++) g.Tick(.1f);
                Check(g.Round.Defeated==1 && g.EnemyCount==0 && g.ProjectileCount==0,"Orange failed to hit and remove a banana.");
                int ammo=g.Round.Ammo;Check(g.RequestThrow() && g.Round.Ammo==ammo-1,"A carried orange must toss forward with no target.");
                Check(g.loadout.VisibleOranges==g.Round.Ammo,"Carried oranges do not match inventory.");
                for(int i=0;i<15;i++)g.Tick(.1f);
                g.locomotion.travelRoot.position=initial;
                Check(!g.UseStation(0),"Remote refill was allowed.");
                g.locomotion.travelRoot.position=g.orangeStation.position;
                for(int i=0;i<20;i++) g.StepStation(true,.1f);
                Check(g.Round.Ammo==4,"Entering station with a held action should not trigger.");
                g.StepStation(false,.1f);for(int i=0;i<10;i++)g.StepStation(true,.1f);
                Check(g.Round.Ammo==10,"Deliberate zone refill failed.");
                g.locomotion.travelRoot.position=initial+Vector3.left*6;
                Check(g.GetStation(g.locomotion.travelRoot.position)==-1 && g.Round.Ammo==10 && g.RequestThrow() && g.Round.Ammo==9,"Reloaded oranges could not be carried away and thrown.");
                for(int i=0;i<15;i++)g.Tick(.1f);
                g.locomotion.travelRoot.position=g.orangeStation.position;g.UseStation(0);
                g.locomotion.travelRoot.position=g.workshop.position;Check(g.UseStation(1),"Barricade did not build.");
                Check(g.Round.Ammo==6 && g.lanes[0].durability==40 && g.lanes[0].barricade.activeSelf,"Barricade cost/state incorrect.");
                var blocked=g.SpawnRaider(0,1);blocked.transform.position=g.lanes[0].path[1].position;
                var before=blocked.transform.position;for(int i=0;i<15;i++)g.Tick(.1f);
                Check(Vector3.Distance(before,blocked.transform.position)<.1f && g.lanes[0].durability<40,"Barricade did not hold and take damage.");
                var follower=g.SpawnRaider(0,1);
                var direction=(g.lanes[0].path[0].position-g.lanes[0].path[1].position).normalized;
                follower.transform.position=before+direction*3.5f;
                for(int i=0;i<12;i++)g.Tick(.1f);
                Check(Vector3.Distance(follower.transform.position,blocked.transform.position)>2.8f,"Bananas overlapped behind a barricade.");
                g.TogglePause();float hp=g.lanes[0].durability;g.Tick(.1f);Check(g.lanes[0].durability==hp && !g.RequestThrow(),"Pause failed to freeze gameplay.");g.TogglePause();
                g.Round.Contaminate(36);g.locomotion.travelRoot.position=g.cleanStation.position;
                Check(g.UseStation(2) && g.Round.Water==89 && g.Round.Ammo==3,"Water cleaning failed.");
                g.ResetRound();g.StartRound();
                for(int i=0;i<9000 && g.Round.Active;i++)g.Tick(.1f);
                Check(g.Round.Phase==RoundPhase.Lost,"Unopposed walking bananas never contaminated the water.");
                g.ResetRound();g.StartRound();
                // Exercise all real waves and projectiles, supplying ammunition between attacks.
                for(int i=0;i<5000 && g.Round.Active;i++)
                {
                    if(g.Round.Ammo==0) {g.locomotion.travelRoot.position=g.orangeStation.position;g.UseStation(0);}
                    var target=g.FindTarget();
                    if(!target && g.EnemyCount>0)
                    {
                        // Destroy is deferred until frame end; inspect this round's live collection.
                        foreach(var e in g.Raiders) if(e.Alive) {g.locomotion.travelRoot.position=e.transform.position+Vector3.right*3;break;}
                    }
                    if(g.FindTarget())g.RequestThrow();g.Tick(.1f);
                }
                Check(g.Round.Phase==RoundPhase.Won,"Three-wave result: "+g.Round.Phase+", water="+g.Round.Water+", defeated="+g.Round.Defeated+", active="+g.EnemyCount);
                Check(g.Round.Coins==g.Round.Defeated*3+24,"Real kills/waves awarded incorrect coins.");
                g.ResetRound();g.StartRound();float baseSpeed=g.locomotion.walkSpeed;
                for(int upgrade=1;upgrade<=3;upgrade++)
                {
                    while(g.Round.Coins<g.Round.NextUpgradeCost) g.Round.RegisterDefeat();
                    g.ToggleShop();float countdown=g.Round.Countdown;g.Tick(.1f);
                    Check(g.Round.Countdown==countdown && !g.RequestThrow(),"Shop did not hold simulation.");
                    Check(g.BuyUpgrade() && g.loadout.rides[upgrade-1].activeSelf && Mathf.Approximately(g.locomotion.walkSpeed,baseSpeed*g.Round.SpeedMultiplier),"Ride visual or movement bonus failed.");
                    g.ToggleShop();
                }
                CheckCamera(g);
                CheckMediaPipePanel(g);
                g.ResetRound();Check(g.EnemyCount==0 && g.ProjectileCount==0 && g.Round.Water==100 && !g.lanes[0].barricade.activeSelf,"Replay left stale gameplay state.");
                Check(g.Round.Coins==0 && g.Round.Upgrade==0 && Mathf.Approximately(g.locomotion.walkSpeed,baseSpeed) && g.loadout.VisibleOranges==6,"Replay loadout was not reset.");
                Directory.CreateDirectory("Logs");File.WriteAllText("Logs/peel-checks.txt","PEEL_PATROL_PLAY_CHECKS_OK\nMovement; hits; free toss; carry/reload away from station; cargo display; spacing; barricades; pause; cleaning; loss; full three-wave victory; kill/wave coins; all purchases/speed/visuals; shop pause; replay.\nSynthetic MediaPipe packets: three roles, movement, steering, throwing, hold/release, station refill, confidence loss, tracking timeout, recovery without phantom actions. Live three-person calibration and human difficulty testing remain outstanding.");
                if(File.Exists("Logs/peel-checks-error.txt"))File.Delete("Logs/peel-checks-error.txt");
                File.AppendAllText("Logs/peel-checks.txt","\nMediaPipe panel: practice movement without waves, sliders retain values, sidebar does not pause the round, viewport reservation, all 1-4 performer mappings/calibration/throwing, body-mode invalidation, calibration cancellation, stop-camera invalidation.\n");
                Debug.Log("PEEL_PATROL_PLAY_CHECKS_OK");
            }
            catch(Exception e){Fail(e);}
            finally{EditorApplication.ExitPlaymode();}
        }
        static void CheckCamera(PeelPatrolGame g)
        {
            g.ResetRound();g.SetControlMode(InputMode.Camera);
            float now=Time.realtimeSinceStartup;
            PoseFrame Frame(float travel=0,float steer=0,float toss=0,float use=0,bool visible=true)
            {
                var people=new PerformerFrame[3];
                for(int i=0;i<3;i++)people[i]=new PerformerFrame{slot=i+1,values=new float[6],confidence=new[]{1f,1f,1f,1f,1f,1f},handForward=new[]{0f,0f},handForwardConfidence=new[]{1f,1f}};
                people[0].values[(int)Movement.LeftHandHeight]=Mathf.Max(0,travel);
                people[0].values[(int)Movement.RightHandHeight]=Mathf.Max(0,-travel);
                people[1].values[(int)Movement.LeftHandHeight]=Mathf.Max(0,-steer);
                people[1].values[(int)Movement.RightHandHeight]=Mathf.Max(0,steer);
                people[2].values[(int)Movement.LeftHandHeight]=toss;people[2].values[(int)Movement.RightHandHeight]=use;
                people[2].handForward=new[]{use,use};
                if(!visible) people[1].confidence[(int)Movement.RightHandHeight]=0;
                return new PoseFrame{version=1,performerCount=3,upperBodyOnly=g.controller.upperBodyOnly,people=people};
            }
            void Feed(PoseFrame frame) {Check(g.controller.AcceptPacket(JsonUtility.ToJson(frame),now),"Camera packet rejected.");g.RefreshTracking(now);}
            var legacy=Frame();legacy.people[2].handForward=legacy.people[2].handForwardConfidence=null;
            Feed(legacy);Check(g.controller.CalibrateAt(now),"Missing P3 depth must not block movement calibration.");
            g.RefreshTracking(now);
            Check(!g.TrackingBlocked && !g.ActionsBlocked,"Hand raises must not require optional hand-depth data.");
            var missingDriver=Frame();missingDriver.people[0].confidence[(int)Movement.LeftHandHeight]=0;
            g.controller.InvalidateCalibration();g.locomotion.enableLocomotion=false;Feed(missingDriver);
            Check(!g.controller.CalibrateAt(now),"Disabled locomotion bypassed missing travel input at calibration.");
            var hiddenFeet=Frame();foreach(var person in hiddenFeet.people){person.confidence[2]=person.confidence[3]=0;}
            Feed(hiddenFeet);Check(g.controller.CalibrateAt(now),"Cosmetic foot tracking blocked movement calibration.");
            var malformed=Frame();malformed.people[2].handForward=new[]{0f};
            Check(!g.controller.AcceptPacket(JsonUtility.ToJson(malformed),now),"Malformed hand channels were accepted.");
            Feed(Frame());Check(g.controller.CalibrateAt(now),"Three-person neutral calibration failed.");
            g.RefreshTracking(now);g.StartRound();Check(g.Round.Active && !g.SimulationPaused,"Calibrated MediaPipe round could not start.");
            Feed(Frame(travel:.8f,steer:.7f));
            g.locomotion.enableLocomotion=true;var position=g.locomotion.travelRoot.position;var rotation=g.locomotion.travelRoot.rotation;
            for(int i=0;i<10;i++)g.locomotion.Evaluate(now,.1f);
            Check(Vector3.Distance(position,g.locomotion.travelRoot.position)>.5f && Quaternion.Angle(rotation,g.locomotion.travelRoot.rotation)>10,"P1/P2 camera movement or steering failed.");
            Feed(Frame(travel:-.8f,steer:-.7f));
            Check(g.locomotion.forward.TryRead(g.controller,now,out float reverse) && reverse<0 &&
                g.locomotion.steering.TryRead(g.controller,now,out float leftTurn) && leftTurn<0,"Right-hand reverse and left-hand steering must work together.");
            Feed(Frame());g.StepCameraInput(now,.1f);Feed(Frame(toss:1));int ammo=g.Round.Ammo;
            for(int i=0;i<30;i++){g.StepCameraInput(now,.1f);g.Tick(.1f);}
            Check(g.Round.Ammo==ammo-3,"P3 held left hand must throw three oranges in three seconds.");
            Feed(Frame(visible:false));float countdown=g.Round.Countdown;
            Check(!g.TrackingBlocked,"A single dropped steering observation stopped movement.");
            now+=.1f;Feed(Frame(visible:false));
            Check(!g.TrackingBlocked,"Brief steering gap did not retain the last valid drive signal.");
            now+=.16f;Feed(Frame(visible:false));
            g.StepCameraInput(now,.1f);g.Tick(.1f);
            Check(g.TrackingBlocked && g.Round.Countdown==countdown && !g.RequestThrow(),"Continued tracking loss extended the movement grace period.");
            Feed(Frame(toss:1));for(int i=0;i<10;i++){g.StepCameraInput(now,.1f);g.Tick(.1f);}
            Check(g.Round.Ammo==ammo-4,"Stable held hand did not resume throwing after tracking recovery.");
            g.Tick(.1f);g.Tick(.1f);g.Tick(.1f);g.Tick(.1f);g.Tick(.1f);g.Tick(.1f);
            Feed(Frame());g.StepCameraInput(now,.1f);Feed(Frame(toss:1));for(int i=0;i<3;i++)g.StepCameraInput(now,.1f);
            Check(g.Round.Ammo==ammo-5,"Fresh gesture after recovery failed.");
            g.locomotion.travelRoot.position=g.orangeStation.position;
            Feed(Frame(use:1));for(int i=0;i<12;i++)g.StepCameraInput(now,.1f);
            Check(g.Round.Ammo==ammo-5,"Entering a zone with raised hand refilled without release.");
            Feed(Frame());g.StepCameraInput(now,.1f);Feed(Frame(use:1));for(int i=0;i<12;i++)g.StepCameraInput(now,.1f);
            Check(g.Round.Ammo==10 && g.loadout.VisibleOranges==10,"P3 right-hand raise reload failed.");
            var hiddenHand=Frame();hiddenHand.people[2].confidence[(int)Movement.LeftHandHeight]=0;
            hiddenHand.people[0].values[0]=.8f;
            Feed(hiddenHand);Check(!g.TrackingBlocked && g.ThrowBlocked,"Occluded P3 left wrist must block throwing only.");
            position=g.locomotion.travelRoot.position;g.locomotion.enableLocomotion=true;
            for(int i=0;i<10;i++){g.StepCameraInput(now,.1f);g.locomotion.Evaluate(now,.1f);}
            Check(Vector3.Distance(position,g.locomotion.travelRoot.position)>.5f,"P3 action hold froze P1 movement.");
            g.locomotion.travelRoot.position=g.orangeStation.position;g.UseStation(0);
            var movingThrow=Frame(travel:.8f,steer:.7f,toss:1);
            movingThrow.people[2].confidence[(int)Movement.RightHandHeight]=0;
            Feed(movingThrow);Check(!g.ThrowBlocked,"Idle right wrist blocked left-hand throw.");
            position=g.locomotion.travelRoot.position;rotation=g.locomotion.travelRoot.rotation;
            int movingAmmo=g.Round.Ammo;
            for(int i=0;i<25;i++){g.StepCameraInput(now,.1f);g.locomotion.Evaluate(now,.1f);g.Tick(.1f);}
            Check(g.Round.Ammo<movingAmmo && Vector3.Distance(position,g.locomotion.travelRoot.position)>.5f && Quaternion.Angle(rotation,g.locomotion.travelRoot.rotation)>10,
                "Moving, turning and throwing failed to run together.");
            g.RefreshTracking(now+g.controller.trackingTimeout+.1f);
            Check(g.TrackingBlocked,"Stale camera packets did not block the round.");
            g.SetControlMode(InputMode.TestSliders);
        }
        static void CheckMediaPipePanel(PeelPatrolGame g)
        {
            Check(g.ControlPanel && g.ControlPanel.Preview && g.ControlPanel.Preview.embeddedInterface,"MediaPipe interface missing or duplicated.");
            g.ResetRound();g.SelectSliders();g.UsePerformers(1);
            g.locomotion.testForward=.75f;g.controller.controls[0].testValue=.7f;
            Vector3 origin=g.locomotion.travelRoot.position;
            for(int i=0;i<10;i++)
            {
                g.SendMessage("Update");g.controller.ApplyControls(Time.realtimeSinceStartup,.1f);g.Tick(.1f);
            }
            Check(Vector3.Distance(origin,g.locomotion.travelRoot.position)>.5f && g.Round.Phase==RoundPhase.Ready && g.EnemyCount==0,"Practice movement failed or started waves.");
            Check(g.locomotion.testForward==.75f && g.controller.controls[0].testValue==.7f,"Keyboard input overwrote test sliders.");
            int practiceAmmo=g.Round.Ammo;
            Check(g.RequestThrow(),"Practice mode blocked throwing.");
            for(int i=0;i<12;i++)g.Tick(.1f);
            Check(g.Round.Ammo==practiceAmmo-1 && g.Round.Phase==RoundPhase.Ready && g.EnemyCount==0,"Practice throw started waves or failed to spend ammo.");
            g.locomotion.travelRoot.position=g.orangeStation.position;
            Check(g.UseStation(0) && g.Round.Ammo==10,"Practice reload failed.");
            g.showMediaPipePanel=true;g.StartRound();g.ApplyGameViewport();
            Check(!g.SimulationPaused && g.locomotion.followCamera.rect.x>0,"Visible sidebar blocked gameplay or covered the camera viewport.");
            float timer=g.Round.Countdown;g.Tick(.1f);Check(g.Round.Countdown<timer,"Round did not advance with sidebar open.");
            for(int count=1;count<=4;count++)
            {
                g.ResetRound();g.UsePerformers(count);g.SetControlMode(InputMode.Camera);
                Check(g.ActionPerformer==count && g.locomotion.steering.sources[0].performer==Mathf.Min(2,count),"Performer mapping did not adapt to group size.");
                float now=Time.realtimeSinceStartup;
                var people=new PerformerFrame[count];
                for(int i=0;i<count;i++)people[i]=new PerformerFrame{slot=i+1,values=new float[6],confidence=new[]{1f,1f,1f,1f,1f,1f},handForward=new[]{0f,0f},handForwardConfidence=new[]{1f,1f}};
                var frame=new PoseFrame{version=1,performerCount=count,upperBodyOnly=g.controller.upperBodyOnly,people=people};
                Check(g.controller.AcceptPacket(JsonUtility.ToJson(frame),now),"Group packet rejected: "+count);
                Check(g.controller.Status.StartsWith("Camera connected."),"Valid tracking did not clear the camera startup status.");
                Check(g.controller.CalibrateAt(now),"Group calibration failed: "+count);
                g.RefreshTracking(now);g.StartRound();g.StepCameraInput(now,.1f);
                people[count-1].values[(int)Movement.LeftHandHeight]=1;
                g.controller.AcceptPacket(JsonUtility.ToJson(frame),now);
                for(int i=0;i<3;i++)g.StepCameraInput(now,.1f);
                Check(g.Round.Active && g.Round.Ammo==5,"Group could not start/throw: "+count);
                g.UseUpperBody(!frame.upperBodyOnly);
                Check(g.TrackingBlocked && !g.controller.IsCalibrated && !g.controller.AcceptPacket(JsonUtility.ToJson(frame),now),"Framing change retained stale calibration or accepted wrong mode.");
                g.BeginNeutralPose();Check(g.controller.CalibrationPending && g.TrackingBlocked,"Countdown did not hold tracking.");
                g.controller.CancelCalibrationCountdown();Check(!g.controller.CalibrationPending && !g.controller.IsCalibrated,"Cancelled countdown retained calibration.");
                g.DisconnectCamera();Check(!g.CameraConnected && g.TrackingBlocked,"Stop camera did not hold input.");
            }
            g.UsePerformers(3);g.UseUpperBody(true);g.SetControlMode(InputMode.TestSliders);
        }
        static void Fail(Exception e){Directory.CreateDirectory("Logs");File.WriteAllText("Logs/peel-checks-error.txt",e.ToString());Debug.LogException(e);}
    }
}
