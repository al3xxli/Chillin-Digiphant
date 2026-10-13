using System;
using System.IO;
using System.Linq;
using DigiPhant;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace ChillinDigiphant.Kart.Editor
{
    [InitializeOnLoad] public static class KartInputChecks
    {
        static int stage=-1, jumps;
        static float at, heading, takeoffSpeed;
        static Vector3 takeoffPosition;
        static double started;
        static string report;
        static KartRace race;
        static KartInputChecks(){EditorApplication.update+=Update;}
        static void Require(bool condition,string description)
        {if(!condition)throw new Exception(description);report+="PASS "+description+"\n";}
        static void Next(int value){stage=value;at=Time.realtimeSinceStartup;}
        static void Update()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating)return;
            if(File.Exists(".tools/test-kart-inputs")&&!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                File.Delete(".tools/test-kart-inputs");
                var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                if(scene.isDirty)EditorSceneManager.SaveScene(scene,AssetDatabase.GenerateUniqueAssetPath("Assets/StudentWork/Scenes/Kart-BeforeInputChecks.unity"),true);
                EditorSceneManager.OpenScene(KartBuilder.ScenePath);Application.runInBackground=true;
                SessionState.SetBool("KartInputChecksPending",true);EditorApplication.EnterPlaymode();
            }
            if(!EditorApplication.isPlaying){stage=-1;return;}
            if(SessionState.GetBool("KartInputChecksPending",false))
            {SessionState.SetBool("KartInputChecksPending",false);report="Camera gesture sequences and actual Unity Rigidbody integration\n";started=EditorApplication.timeSinceStartup;Next(0);}
            if(stage<0)return;
            try{if(EditorApplication.timeSinceStartup-started>40)throw new Exception("Input checks timeout");Step();}
            catch(Exception e){File.WriteAllText("Logs/Kart/input-error.txt",report+e);Debug.LogException(e);stage=-1;}
        }
        static BodyMotionFrame Body(float lift=0,float drop=0,bool rightFoot=false,float rise=0) => new BodyMotionFrame
        {hipY=.55f-rise*.25f+drop*.25f,shoulderY=.3f-rise*.25f+drop*.25f,
            leftFootY=.9f-rise*.25f-(rightFoot?0:lift*.25f),rightFootY=.9f-rise*.25f-(rightFoot?lift*.25f:0),scale=.25f,confidence=1};
        static PoseFrame Packet(BodyMotionFrame body=null,float shrug=0,float pinch=1,float fist=0,bool pinchAllowed=true,bool thumbsUp=false)
        {
            var p=new PoseFrame {version=1,performerCount=3,kartStatus="Synthetic test",people=Enumerable.Range(1,3).Select(i=>new PerformerFrame
                {slot=i,values=new float[6],confidence=Enumerable.Repeat(1f,6).ToArray(),handForward=new float[2],handForwardConfidence=new[]{1f,1f}}).ToArray()};
            p.people[0].body=body??Body();p.people[1].shoulders=new ShoulderMotionFrame{turn=shrug,confidence=1};p.people[2].hand=new HandMotionFrame{pinch=pinch,fist=fist,pinchAllowed=pinchAllowed,thumbsUp=thumbsUp,confidence=1};
            p.people[2].confidence=new float[6]; // Independent hand-only role, no torso/pose confidence.
            return p;
        }
        static void Feed(KartCameraInput input,PoseFrame p,float now)=>input.Receive(p,now,1,2,3);
        static KartCameraInput Ready(float time=1)
        {
            var input=new KartCameraInput();Feed(input,Packet(),time);Require(input.Calibrate(time,false,true),"Shoulders calibrate without requiring P1 feet");
            Feed(input,Packet(),time+.05f);Feed(input,Packet(),time+.20f);return input;
        }
        static void Sequences()
        {
            Require(JsonUtility.FromJson<HandMotionFrame>("{\"thumbsUp\":true}").thumbsUp,"The bridge thumbsUp JSON field reaches Unity");
            Require(!JsonUtility.FromJson<HandMotionFrame>("{\"peace\":true}").thumbsUp,"Old peace packets cannot trigger the replacement jump gesture");
            var input=Ready();Feed(input,Packet(Body(.2f)),1.22f);Feed(input,Packet(Body(.2f)),1.32f);
            Require(!input.TakeJump(1.32f),"P1 left foot no longer triggers a jump");
            Feed(input,Packet(Body(.2f,rightFoot:true)),1.36f);Feed(input,Packet(Body(.2f,rightFoot:true)),1.46f);
            Require(!input.TakeJump(1.46f),"P1 right foot no longer triggers a jump");
            Feed(input,Packet(thumbsUp:true),1.5f);Require(!input.TakeJump(1.5f),"One thumbs-up frame cannot jump");
            Feed(input,Packet(thumbsUp:true),1.6f);
            Require(input.TakeJump(1.6f)&&!input.TakeJump(1.6f)&&!input.TakeThrow(1.6f),"P3 thumbs up jumps exactly once without throwing");
            Feed(input,Packet(thumbsUp:true),1.7f);Feed(input,Packet(thumbsUp:true),1.8f);
            Require(!input.TakeJump(1.8f),"Holding a thumbs up cannot repeat the jump");
            Feed(input,Packet(fist:1,pinch:.2f,pinchAllowed:false),2f);Require(input.IsDucking(2f),"P3 fist engages crouch independently of body lowering");
            Feed(input,Packet(fist:.55f,pinch:.2f,pinchAllowed:false),2.08f);Require(input.IsDucking(2.08f),"Fist hysteresis holds through small finger jitter");
            Feed(input,Packet(),2.16f);Require(!input.IsDucking(2.16f)&&!input.TakeThrow(2.16f),"Opening a fist stands up without throwing");
            Feed(input,Packet(fist:1,pinchAllowed:false),2.23f);Require(!input.IsDucking(3f),"Stale hand frames cannot hold crouch");
            Feed(input,Packet(),3f);Feed(input,Packet(pinch:.2f),3.08f);Require(!input.TakeThrow(3.08f),"Closing pinch prepares an item without firing");
            Feed(input,Packet(pinch:.2f),3.2f);Feed(input,Packet(),3.28f);
            Require(input.TakeThrow(3.28f)&&!input.TakeThrow(3.28f),"Pinch release throws once");
            Feed(input,Packet(),3.35f);Require(!input.TakeThrow(3.35f),"Open hand does not auto-fire");
            Feed(input,Packet(pinch:.2f),3.4f);var missing=Packet();missing.people[2].hand=null;Feed(input,missing,3.48f);Feed(input,Packet(),3.55f);
            Require(input.TakeThrow(3.55f),"One dropped camera frame preserves the pinch release");
            Feed(input,Packet(shrug:-.35f),3.63f);Require(input.Steering<-.2f,"Left shoulder shrug gives smoothed left steering");
            Feed(input,Packet(shrug:.35f),3.73f);Feed(input,Packet(shrug:.35f),3.83f);Require(input.Steering>.2f,"Right shoulder shrug gives right steering");
            missing=Packet();missing.people[1].shoulders=null;Feed(input,missing,3.9f);
            Require(input.ShouldersVisible(3.9f),"One dropped shoulder frame preserves steering");
            Feed(input,missing,4.1f);Require(!input.ShouldersVisible(4.1f)&&input.Steering==0,"Long shoulder loss clears steering");
            missing=Packet();missing.people[0].body.hipY=float.NaN;Feed(input,missing,4.2f);
            Require(!input.BodyReady(4.2f)&&!input.IsDucking(4.2f),"Malformed optional body data cannot trigger actions");
            input.Reset();Require(!input.Calibrated&&!input.ShouldersVisible(4.3f)&&!input.TakeJump(4.3f),"Reassignment clears neutral and all gesture latches");
            var airborne=new KartCameraInput();Feed(airborne,Packet(thumbsUp:true),1);Feed(airborne,Packet(thumbsUp:true),1.1f);
            Require(!airborne.TakeJump(1.1f),"Uncalibrated observations cannot jump");
            input=Ready(10);Feed(input,Packet(Body(drop:.4f)),10.25f);
            Require(!input.IsDucking(10.25f)&&!input.TakeJump(10.25f),"P1 squatting cannot crouch or jump");
            input=Ready(11);Feed(input,Packet(Body(rise:.3f)),11.25f);
            Require(!input.TakeJump(11.25f),"P1 whole-body movement cannot jump");
            input=Ready(12);Feed(input,Packet(Body(.04f)),12.25f);Feed(input,Packet(Body(.04f)),12.30f);
            Require(!input.TakeJump(12.30f),"Small foot jitter stays below the jump threshold");
            input=Ready(13);missing=Packet(fist:1,pinchAllowed:false);missing.people[0].body=null;Feed(input,missing,13.25f);
            Require(input.IsDucking(13.25f),"P3 fist does not depend on P1 feet remaining visible");
            input=Ready(14);Feed(input,Packet(pinch:.2f),14.25f);Feed(input,Packet(pinch:.2f,fist:.3f,pinchAllowed:false),14.33f);Feed(input,Packet(),14.42f);
            Require(!input.TakeThrow(14.42f),"Curling spare fingers cancels a pinch before it can become an accidental fist-release throw");
            input=Ready(15);missing=Packet(thumbsUp:true);missing.people[0].body=null;
            Feed(input,missing,15.22f);Feed(input,missing,15.32f);
            Require(input.TakeJump(15.32f),"P3 thumbs up jump works without P1 hips or feet");
            Feed(input,Packet(),15.4f);Feed(input,Packet(thumbsUp:true),15.45f);Feed(input,Packet(thumbsUp:true),15.55f);
            Require(!input.TakeJump(15.55f),"Brief thumbs-up flicker does not rearm a held jump");
            Feed(input,Packet(),15.6f);Feed(input,Packet(),15.74f);
            Feed(input,Packet(thumbsUp:true),15.78f);Feed(input,Packet(thumbsUp:true),15.88f);
            Require(input.TakeJump(15.88f),"Relaxing fingers rearms the next thumbs up jump");
            input=Ready(16);Feed(input,Packet(thumbsUp:true),16.22f);missing=Packet();missing.people[2].hand=null;
            Feed(input,missing,16.26f);Feed(input,Packet(thumbsUp:true),16.32f);
            Require(!input.TakeJump(16.32f),"Missing hand frames do not count toward the thumbs up dwell");
            Feed(input,Packet(thumbsUp:true),16.42f);Require(input.TakeJump(16.42f),"Stable thumbs up after a brief dropout still jumps");
            Feed(input,missing,16.48f);Feed(input,Packet(thumbsUp:true),16.55f);Feed(input,Packet(thumbsUp:true),16.65f);
            Require(!input.TakeJump(16.65f),"Brief hand loss cannot repeat a held thumbs up jump");
            Feed(input,missing,17f);Feed(input,Packet(thumbsUp:true),17.1f);Feed(input,Packet(thumbsUp:true),17.2f);
            Require(!input.TakeJump(17.2f),"Long hand loss requires a release before a new thumbs up jump");
            input=Ready(18);Feed(input,Packet(pinch:.2f),18.22f);Feed(input,Packet(thumbsUp:true),18.3f);Feed(input,Packet(thumbsUp:true),18.4f);
            Require(input.TakeJump(18.4f)&&!input.TakeThrow(18.4f),"Pinch to thumbs up cancels the pending throw and jumps");
            Feed(input,Packet(),18.5f);Require(!input.TakeThrow(18.5f),"Thumbs up release cannot throw an orange");
            input=new KartCameraInput();Feed(input,Packet(shrug:.12f),19);input.Calibrate(19,true,true);
            Feed(input,Packet(shrug:.12f),19.1f);Require(Mathf.Abs(input.Steering)<.001f,"Relaxed uneven shoulders calibrate to straight driving");
            Feed(input,Packet(shrug:-.23f),19.2f);Require(input.Steering<-.2f,"Left shrug steers relative to the calibrated shoulder posture");
            input=Ready(20);for(float t=20.3f;t<23.5f;t+=.1f)Feed(input,Packet(pinch:.2f),t);
            Feed(input,Packet(),23.55f);Require(input.TakeThrow(23.55f),"A continuously visible pinch can be held while aiming, then released to throw");
            input=Ready(30);Feed(input,Packet(pinch:.2f),30.3f);missing=Packet();missing.people[2].hand=null;
            Feed(input,missing,30.6f);Feed(input,Packet(),30.7f);
            Require(!input.TakeThrow(30.7f),"Long hand loss then open reacquisition cannot throw");
            input=Ready(35);missing=Packet(thumbsUp:true);missing.people[0].hand=missing.people[2].hand;missing.people[2].hand=null;
            Feed(input,missing,35.22f);Feed(input,missing,35.32f);
            Require(!input.TakeJump(35.32f),"A thumbs up from P1 cannot trigger P3's jump");
            input=Ready(36);missing=Packet(thumbsUp:true);missing.people[2].hand.confidence=.2f;
            Feed(input,missing,36.22f);Feed(input,missing,36.32f);
            Require(!input.TakeJump(36.32f),"Low-confidence thumbs up frames cannot jump");
            input=Ready(37);Feed(input,Packet(fist:1,thumbsUp:true),37.22f);Feed(input,Packet(fist:1,thumbsUp:true),37.32f);
            Require(!input.IsDucking(37.32f)&&input.TakeJump(37.32f),"Thumbs up overrides curled-finger fist input and jumps without crouching");
            input.ResetActions();Feed(input,Packet(thumbsUp:true),37.42f);Feed(input,Packet(thumbsUp:true),37.52f);
            Require(!input.TakeJump(37.52f),"Resetting while thumbs up is held requires relaxing the fingers");
            input=Ready(40);
            Feed(input,Packet(),40.55f);Feed(input,Packet(),40.90f);
            Feed(input,Packet(thumbsUp:true),41.25f);Feed(input,Packet(thumbsUp:true),41.60f);
            Require(input.TakeJump(41.60f),"Thumbs up jump works with real camera samples below four FPS");
            Feed(input,Packet(thumbsUp:true),41.95f);Feed(input,Packet(thumbsUp:true),42.30f);
            Require(!input.TakeJump(42.30f),"Slow camera samples cannot repeat a held thumbs up jump");
            Feed(input,Packet(),42.65f);Feed(input,Packet(),43f);
            Feed(input,Packet(thumbsUp:true),43.35f);Feed(input,Packet(thumbsUp:true),43.70f);
            Require(input.TakeJump(43.70f),"Opening then repeating thumbs up rearms at low camera FPS");
            input=Ready(45);missing=Packet();missing.people[2].hand=null;
            Feed(input,missing,45.6f);Feed(input,Packet(thumbsUp:true),46f);Feed(input,Packet(thumbsUp:true),46.1f);
            Require(input.TakeJump(46.1f),"An observed release stays armed when the hand leaves and reenters its zone");
            input=new KartCameraInput();missing=Packet();missing.people[0].body=null;Feed(input,missing,38);
            Require(input.Calibrate(38,false,true),"Neutral can be saved with no P1 body channel");
            input=new KartCameraInput();Feed(input,Packet(pinch:.2f),40);
            Require(input.HandFeedback(40).Contains("pinched")&&!input.TakeThrow(40),"Hand-only pinch feedback works before calibration without firing");
        }
        static void CameraFrame(float lift=0,float fist=0,float shrug=0,float pinch=1,float drive=0,bool thumbsUp=false)
        {
            var p=Packet(Body(lift),shrug,pinch,fist,fist<.45f && !thumbsUp,thumbsUp);p.upperBodyOnly=race.controller.upperBodyOnly;p.previewFormat=2;p.mirrorPreview=race.Preview.mirrorPreview;
            // Shoulder performer does not need visible wrists in mixed mode.
            p.people[1].confidence[0]=p.people[1].confidence[1]=0;
            p.people[0].values[0]=drive;
            p.people[0].body=null; // All hand actions and team calibration work with feet cropped out.
            RequirePacket(race.controller.AcceptPacket(JsonUtility.ToJson(p),Time.realtimeSinceStartup));
        }
        static void RequirePacket(bool accepted){if(!accepted)throw new Exception("Optional packet rejected");}
        static void Step()
        {
            float elapsed=Time.realtimeSinceStartup-at;
            if(stage==0)
            {
                if(elapsed<1)return;
                Sequences();race=Object.FindAnyObjectByType<KartRace>();race.SetMode(1);race.ResetRace();
                foreach(var r in race.racers.Skip(1))r.gameObject.SetActive(false);
                race.SetCameraLayout(KartCameraLayout.MixedTeam);race.SetLiteralActions(true);race.UsePerformers(3);race.SetMode(0);
                CameraFrame();Require(race.controller.CalibrateAt(Time.realtimeSinceStartup),"Mixed role calibration works with P2 wrists hidden");
                race.RefreshTracking(Time.realtimeSinceStartup);Require(!race.TrackingBlocked,"Body plus shoulders can drive without P2 hand signals");
                jumps=race.player.JumpCount;Next(1);
            }
            else if(stage==1)
            {
                CameraFrame();if(elapsed<.4f)return;Next(2);
            }
            else if(stage==2)
            {
                CameraFrame(thumbsUp:true,drive:1);if(elapsed<.25f)return;
                Require(race.player.JumpCount==jumps+1 && race.player.Body.linearVelocity.y>0,"P3 thumbs up launches the actual Rigidbody upward");
                Require(race.player.Speed>.20f,"Accelerating during a jump from low speed produces forward movement in the air");Next(3);
            }
            else if(stage==3)
            {
                CameraFrame();if(elapsed<1.1f)return;Next(4);
            }
            else if(stage==4)
            {
                CameraFrame(fist:1,pinch:.2f);if(elapsed<.25f)return;
                Require(race.player.Crouching && !race.player.GetComponent<CapsuleCollider>().enabled,"P3 fist switches the actual kart to the low collision shape");Next(5);
            }
            else if(stage==5)
            {
                CameraFrame(thumbsUp:true);if(elapsed<.25f)return;
                Require(!race.player.Crouching && race.player.JumpCount==jumps+2,"Changing P3 fist to thumbs up restores clearance and launches the kart");race.player.Collect(3);Next(6);
            }
            else if(stage==6)
            {
                CameraFrame(pinch:elapsed<.12f?1:.2f);if(elapsed<.32f)return;Next(7);
            }
            else if(stage==7)
            {
                CameraFrame();if(elapsed<.8f)return;
                Require(race.player.Ammo==2 && race.PlayerTrunk.LastProjectile && Object.FindObjectsByType<KartProjectile>(FindObjectsSortMode.None).Length==1,"Right pinch release animates the trunk and launches one real orange from its tip");heading=race.player.Body.rotation.eulerAngles.y;Next(8);
            }
            else if(stage==8)
            {
                CameraFrame(shrug:-.35f,drive:1);if(elapsed<1)return;
                Require(race.player.Steering*(race.invertShoulderSteering?-1:1)<-.4f && race.player.Throttle>.5f && Mathf.Abs(Mathf.DeltaAngle(heading,race.player.Body.rotation.eulerAngles.y))>3,
                    "Pose acceleration and shoulder steering drive and turn the real kart together");Next(9);
            }
            else if(stage==9)
            {
                if(elapsed<.65f)return;race.RefreshTracking(Time.realtimeSinceStartup);
                Require(race.TrackingBlocked,"Stale camera data holds the game");
                race.SetMode(1);race.ResetRace();race.SetMode(0);CameraFrame();race.controller.CalibrateAt(Time.realtimeSinceStartup);Next(11);
            }
            else if(stage==11)
            {
                CameraFrame(drive:1);if(elapsed<1.3f)return;
                Require(race.player.Speed>4,"Kart is moving before the momentum jump check");
                takeoffSpeed=race.player.Speed;takeoffPosition=race.player.Body.position;jumps=race.player.JumpCount;Next(12);
            }
            else if(stage==12)
            {
                CameraFrame(thumbsUp:true,drive:1);if(elapsed<.40f)return;
                Require(race.player.JumpCount==jumps+1 && race.player.Jumping,"A thumbs up while driving launches one moving jump");
                Require(race.player.Speed>=takeoffSpeed*.90f && Vector3.Distance(takeoffPosition,race.player.Body.position)>takeoffSpeed*.30f,"Moving jump retains forward momentum and travels down the course");
                report+="Measured moving jump: takeoff="+takeoffSpeed.ToString("F2")+" m/s; in-flight="+race.player.Speed.ToString("F2")+" m/s; travel="+Vector3.Distance(takeoffPosition,race.player.Body.position).ToString("F2")+" m\n";
                takeoffSpeed=race.player.Speed;Next(13);
            }
            else if(stage==13)
            {
                CameraFrame();if(elapsed<.16f)return;
                Require(race.player.Speed>=takeoffSpeed*.90f,"Lowering the driving hand briefly during flight does not brake the jump");
                race.SetMode(2);race.ResetRace();
                Require(!race.TrackingBlocked,"Keyboard mode remains available without tracking");
                race.SetMode(1);race.player.Body.position+=Vector3.up*1.8f;race.player.Body.linearVelocity=Vector3.down*4;
                race.player.transform.position=race.player.Body.position;Physics.SyncTransforms();jumps=race.player.JumpCount;Next(14);
            }
            else if(stage==14)
            {
                if(elapsed<.04f)return;
                Require(!race.player.Grounded,"Buffer check begins just above the ground");race.player.RequestJump();Next(15);
            }
            else if(stage==15)
            {
                if(elapsed<.20f)return;
                Require(race.player.JumpCount==jumps+1 && race.player.Body.linearVelocity.y>0,"Jump requested just before landing survives until the next grounded physics step");
                race.ResetRace();
                var canopy=race.obstacles.First(o=>o.kind==KartObstacleKind.Duck);
                race.testCrouch=true;race.player.SetCrouch(true);
                race.player.Body.position=canopy.transform.TransformPoint(new Vector3(0,.15f,0));
                race.player.Body.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(canopy.transform.forward,Vector3.up));
                race.player.transform.SetPositionAndRotation(race.player.Body.position,race.player.Body.rotation);
                race.player.Body.linearVelocity=Vector3.zero;Physics.SyncTransforms();Next(10);
            }
            else if(stage==10)
            {
                if(elapsed<.3f)return;
                Require(race.player.Crouching,"Kart can occupy the canopy's low clearance while ducking");
                race.testCrouch=false;race.player.SetCrouch(false);
                Require(!race.player.Jump() && race.player.Crouching,"A jump cannot expand the collider into an overhead canopy");
                race.SetMode(2);race.ResetRace();foreach(var r in race.racers)r.gameObject.SetActive(true);
                report+="KART_INPUT_VALIDATION_OK\nPhysical performers must still test camera framing and comfortable sensitivity.\n";
                File.WriteAllText("Logs/Kart/input-checks.txt",report);Debug.Log("KART_INPUT_VALIDATION_OK");stage=-1;
                ScreenCapture.CaptureScreenshot("Logs/Kart/mixed-controls.png");
            }
        }
    }
}
