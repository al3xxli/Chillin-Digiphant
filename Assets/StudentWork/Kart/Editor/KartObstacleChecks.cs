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
    [InitializeOnLoad]
    public static class KartObstacleChecks
    {
        static KartRace race;
        static KartPickup[] items;
        static KartObstacle obstacle;
        static int stage=-1,contacts,ammo;
        static float at,highest,groundY;
        static double started;
        static string report;
        static KartObstacleChecks(){EditorApplication.update+=Update;}
        [MenuItem("DigiPhant/Kart/Validate pickups and obstacles in Play mode")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play first.");
            Directory.CreateDirectory(".tools");File.WriteAllText(".tools/test-kart-obstacles","");
        }
        static void Require(bool ok,string message)
        {if(!ok)throw new Exception(message);report+="PASS "+message+"\n";File.WriteAllText("Logs/Kart/obstacle-checks.txt",report);}
        static void Next(int value){stage=value;at=Time.time;}
        static void Place(Vector3 p,Quaternion q)
        {
            race.player.Body.position=p;race.player.Body.rotation=q;race.player.Body.linearVelocity=race.player.Body.angularVelocity=Vector3.zero;
            race.player.transform.SetPositionAndRotation(p,q);Physics.SyncTransforms();race.locomotion.StopMotion();race.testCrouch=false;
            race.locomotion.followCamera.GetComponent<KartFollowCamera>().Snap();
        }
        static void Approach(KartObstacle target,float ahead,float lane=0)
        {
            obstacle=target;
            Place(target.transform.TransformPoint(new Vector3(lane,.18f,-ahead)),Quaternion.LookRotation(Vector3.ProjectOnPlane(target.transform.forward,Vector3.up)));
        }
        static void Drive(float speed=16)
        {race.locomotion.testForward=.75f;race.player.Body.linearVelocity=race.player.Body.rotation*Vector3.forward*speed;}
        static void Update()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating)return;
            if(File.Exists(".tools/test-kart-obstacles")&&!EditorApplication.isPlayingOrWillChangePlaymode && !File.Exists(".tools/refresh-kart"))
            {
                File.Delete(".tools/test-kart-obstacles");
                var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                if(scene.isDirty)EditorSceneManager.SaveScene(scene,AssetDatabase.GenerateUniqueAssetPath("Assets/StudentWork/Scenes/Kart-BeforeObstacleChecks.unity"),true);
                EditorSceneManager.OpenScene(KartBuilder.ScenePath);KartBuilder.Validate();Application.runInBackground=true;
                SessionState.SetBool("KartObstacleChecksPending",true);EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView")).Focus();EditorApplication.EnterPlaymode();
            }
            if(!EditorApplication.isPlaying){stage=-1;return;}
            if(SessionState.GetBool("KartObstacleChecksPending",false))
            {SessionState.SetBool("KartObstacleChecksPending",false);report="Actual Unity physics / pickup and obstacle regression\n";started=EditorApplication.timeSinceStartup;Next(0);}
            if(stage<0)return;
            try{if(EditorApplication.timeSinceStartup-started>350)throw new Exception("Obstacle checks exceeded 350 seconds.");Step();}
            catch(Exception e)
            {
                string state=race ? "\nStage="+stage+" position="+race.player.Body.position+" velocity="+race.player.Body.linearVelocity+" grounded="+race.player.Grounded+" contacts="+race.player.ObstacleContacts+" previousContacts="+contacts+" ahead="+(obstacle?obstacle.Ahead(race.player.Body.position):0)+"\n" : "";
                stage=-1;File.WriteAllText("Logs/Kart/obstacle-check-error.txt",report+state+e);Debug.LogException(e);
            }
        }
        static void Step()
        {
            float elapsed=Time.time-at;
            if(stage==0)
            {
                if(EditorApplication.timeSinceStartup-started<1)return;
                race=Object.FindAnyObjectByType<KartRace>();race.SetMode(1);race.ResetRace();
                Require(race.refillBays.Length==0&&!race.GetComponentsInChildren<Transform>().Any(t=>t.name.StartsWith("REFILL")||t.name=="Refill orange display"),"Gray refill platforms and unattainable refill displays are absent");
                Require(race.obstacles.Length==5,"One log, one low canopy, and three alternating slalom barriers are installed");
                foreach(var rival in race.racers.Skip(1))rival.gameObject.SetActive(false);
                items=Object.FindObjectsByType<KartPickup>();
                Place(items[0].transform.position+Vector3.up*.15f,Quaternion.LookRotation(race.track.Forward(race.track.Nearest(items[0].transform.position))));Next(1);
            }
            else if(stage==1)
            {
                if(elapsed<.2f)return;
                Require(race.player.Ammo==1&&!items[0].Available&&!items[0].visual.gameObject.activeSelf,"An empty basket receives one orange and the box immediately disappears");
                Place(items[1].transform.position+Vector3.up*.15f,race.player.Body.rotation);Next(2);
            }
            else if(stage==2)
            {
                if(elapsed<.2f)return;
                Require(race.player.Ammo==2&&!items[1].visual.gameObject.activeSelf,"A box also disappears when the driver already has an orange");
                race.player.Collect(3);Place(items[2].transform.position+Vector3.up*.15f,race.player.Body.rotation);Next(3);
            }
            else if(stage==3)
            {
                if(elapsed<.2f)return;
                Require(race.player.Ammo==3&&!items[2].Available&&!items[2].GetComponent<Collider>().enabled&&!items[2].visual.gameObject.activeSelf,"A full basket still consumes the box without exceeding capacity");
                Place(race.track.Point(0)+Vector3.up*.2f,Quaternion.LookRotation(race.track.Forward(0)));
                var rival=race.racers[1];rival.gameObject.SetActive(true);rival.isPlayer=true;rival.Body.position=items[2].transform.position+Vector3.up*.15f;rival.Body.linearVelocity=Vector3.zero;
                rival.transform.position=rival.Body.position;Physics.SyncTransforms();Next(4);
            }
            else if(stage==4)
            {
                if(elapsed<.3f)return;
                Require(race.racers[1].Ammo==0,"A second kart cannot claim a box that another kart already consumed");
                race.racers[1].gameObject.SetActive(false);Next(5);
            }
            else if(stage==5)
            {
                if(elapsed<5.1f)return;
                Require(items[2].Available&&items[2].visual.gameObject.activeSelf&&items[2].GetComponent<Collider>().enabled,"Consumed boxes respawn with their visual and trigger after five seconds");
                Approach(race.obstacles.First(o=>o.kind==KartObstacleKind.Jump),8);Next(6);
            }
            else if(stage==6)
            {
                if(elapsed<.3f)return;contacts=race.player.ObstacleContacts;Drive();Next(7);
            }
            else if(stage==7)
            {
                if(elapsed<.9f)return;
                Require(race.player.ObstacleContacts>contacts,"Driving into the log without jumping produces a physical obstacle collision");
                Approach(obstacle,8);Next(8);
            }
            else if(stage==8)
            {
                if(elapsed<.3f)return;contacts=race.player.ObstacleContacts;Drive();groundY=race.player.Body.position.y;highest=groundY;
                Require(race.player.Jump(),"Grounded kart accepts a jump impulse");Next(9);
            }
            else if(stage==9)
            {
                highest=Mathf.Max(highest,race.player.Body.position.y);
                if(elapsed<.35f)return;
                Require(highest>groundY+1.3f&&!race.player.Grounded,"Jump lifts the actual Rigidbody above the hurdle");ScreenCapture.CaptureScreenshot("Logs/Kart/jump-log.png");Next(10);
            }
            else if(stage==10)
            {
                if(elapsed<.8f)return;
                Require(obstacle.Ahead(race.player.Body.position)<-3&&race.player.Grounded&&race.player.ObstacleContacts==contacts,"Jump clears the log without contact and lands back on the track");
                Approach(race.obstacles.First(o=>o.kind==KartObstacleKind.Duck),8);Next(11);
            }
            else if(stage==11)
            {if(elapsed<.3f)return;contacts=race.player.ObstacleContacts;Drive();Next(12);}
            else if(stage==12)
            {
                if(elapsed<.9f)return;
                Require(race.player.ObstacleContacts>contacts,"The low canopy physically blocks an upright kart");
                Approach(obstacle,6);race.testCrouch=true;Next(13);
            }
            else if(stage==13)
            {
                if(elapsed<.3f)return;
                Require(race.player.Crouching&&race.player.GetComponent<KartFeedback>().driverRoot.localScale.y<.4f,"Duck reduces the collider height and visibly lowers the elephant");
                contacts=race.player.ObstacleContacts;Drive();Next(14);
            }
            else if(stage==14)
            {
                if(elapsed<.35f)return;
                race.locomotion.StopMotion();race.player.Body.linearVelocity=Vector3.zero;race.testCrouch=false;Next(15);
            }
            else if(stage==15)
            {
                if(elapsed<.2f)return;
                Require(race.player.Crouching,"Releasing duck under the canopy waits for safe standing clearance");ScreenCapture.CaptureScreenshot("Logs/Kart/duck-canopy.png");
                Drive(12);Next(16);
            }
            else if(stage==16)
            {
                if(elapsed<.9f)return;
                Require(obstacle.Ahead(race.player.Body.position)<-4&&!race.player.Crouching&&race.player.ObstacleContacts==contacts,"Crouched kart passes through the canopy and stands safely after clearing it");
                Approach(race.obstacles.First(o=>o.kind==KartObstacleKind.Slalom),9,5);Next(17);
            }
            else if(stage==17)
            {if(elapsed<.3f)return;contacts=race.player.ObstacleContacts;Drive(13);Next(18);}
            else if(stage==18)
            {
                if(elapsed<1.2f)return;
                Require(obstacle.Ahead(race.player.Body.position)<-3&&race.player.ObstacleContacts==contacts,"Steering into the marked opening passes the slalom barrier without collision");
                Place(race.track.Point(2)+Vector3.up*.2f,Quaternion.LookRotation(race.track.Forward(2)));Next(19);
            }
            else if(stage==19)
            {
                if(elapsed<.3f)return;CheckGesture();Next(20);
            }
            else if(stage==20)
            {
                if(elapsed<.2f)return;
                Require(race.player.Crouching,"A calibrated right lean reaches the physical crouch action");
                race.SetMode(1);foreach(var kart in race.racers){kart.gameObject.SetActive(true);kart.enabled=true;kart.isPlayer=false;}
                race.player.aiLane=-4.7f;race.player.aiPace=.8f;race.totalLaps=3;race.StartRace();Next(21);
            }
            else if(stage==21)
            {
                File.WriteAllText("Logs/Kart/obstacle-race-progress.txt","Elapsed "+race.Elapsed.ToString("F2")+"\n"+string.Join("\n",race.racers.Select(k=>k.racerName+" lap="+k.CompletedLaps+" gate="+k.NextGate+" point="+k.NearestPoint+" speed="+k.Speed.ToString("F1")+" jumps="+k.JumpCount+" duck="+k.Crouching+" recoveries="+k.Recoveries+" obstaclesHit="+k.ObstacleContacts+" position="+k.Body.position)));
                if(!race.racers.All(k=>k.Finished)){if(race.Elapsed>260)throw new Exception("Four-kart obstacle race exceeded 260 seconds.");return;}
                Require(race.racers.All(k=>k.Recoveries==0&&k.JumpCount>=3),"All four karts complete three laps including jumps, low canopy and slalom, without checkpoint recovery ("+race.Elapsed.ToString("F1")+"s)");
                report+="Obstacle contacts during race: "+string.Join(", ",race.racers.Select(k=>k.racerName+"="+k.ObstacleContacts))+"\nKART_OBSTACLE_VALIDATION_OK\n";
                File.WriteAllText("Logs/Kart/obstacle-checks.txt",report);race.player.isPlayer=true;race.totalLaps=3;race.SetMode(2);race.ResetRace();ScreenCapture.CaptureScreenshot("Logs/Kart/game-view.png");stage=-1;
            }
        }
        static void CheckGesture()
        {
            race.SetCameraLayout(KartCameraLayout.PoseTeam);race.SetLiteralActions(false);
            race.SetMode(0);var c=race.controller;float now=Time.realtimeSinceStartup;
            var frame=new PoseFrame{version=1,performerCount=c.performerCount,upperBodyOnly=c.upperBodyOnly,previewFormat=2,mirrorPreview=race.Preview.mirrorPreview,
                people=Enumerable.Range(1,c.performerCount).Select(i=>new PerformerFrame{slot=i,values=new float[6],confidence=Enumerable.Repeat(1f,6).ToArray(),handForward=new float[2],handForwardConfidence=new[]{1f,1f}}).ToArray()};
            Require(c.AcceptPacket(JsonUtility.ToJson(frame),now)&&c.CalibrateAt(now),"Existing MediaPipe packet and neutral calibration remain valid");
            race.RefreshTracking(now);race.ReadObstacleGestures(now);int jumps=race.player.JumpCount;
            frame.people[c.performerCount-1].values[1]=1;c.AcceptPacket(JsonUtility.ToJson(frame),now);race.ReadObstacleGestures(now);
            Require(race.player.JumpCount==jumps+1,"Right-hand rising gesture triggers a jump through the existing MediaPipe channel");
            race.ReadObstacleGestures(now);Require(race.player.JumpCount==jumps+1,"Holding the hand up does not repeatedly jump");
            frame.people[c.performerCount-1].values[4]=.6f;c.AcceptPacket(JsonUtility.ToJson(frame),now);race.ReadObstacleGestures(now);
        }
    }
}
