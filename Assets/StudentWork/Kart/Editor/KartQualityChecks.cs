using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace ChillinDigiphant.Kart.Editor
{
    // Regression scenarios use real FixedUpdate, collision contacts and simulated time, never teleport across a lap.
    [InitializeOnLoad]
    public static class KartQualityChecks
    {
        static KartRace race;
        static int stage=-1, contacts, hitCount;
        static float at, beforeSpeed, minSpeed, maxSteerStep, previousSteer, fallY;
        static double started;
        static Vector3 targetStart, axis;
        static string report;
        static KartQualityChecks(){EditorApplication.update+=Update;}
        static void Require(bool condition,string message)
        {
            if(!condition)throw new Exception(message);
            report+="PASS "+message+"\n";File.WriteAllText("Logs/Kart/quality-checks.txt",report);
        }
        static void Next(int n){stage=n;at=Time.time;}
        static void Move(KartVehicle kart,int sample,float lane=0)
        {
            kart.Body.position=race.track.Point(sample)+race.track.Right(sample)*lane+Vector3.up*.18f;
            kart.Body.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(race.track.Forward(sample),Vector3.up));
            kart.transform.SetPositionAndRotation(kart.Body.position,kart.Body.rotation);kart.Body.linearVelocity=kart.Body.angularVelocity=Vector3.zero;
            Physics.SyncTransforms();
        }
        static void Update()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            if(File.Exists(".tools/quality-kart")&&!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                File.Delete(".tools/quality-kart");
                var current=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                if(current.isDirty)EditorSceneManager.SaveScene(current,AssetDatabase.GenerateUniqueAssetPath("Assets/StudentWork/Scenes/Kart-BeforeQualityChecks.unity"),true);
                EditorSceneManager.OpenScene(KartBuilder.ScenePath);KartBuilder.Validate();
                SessionState.SetBool("KartQualityPending",true);Application.runInBackground=true;
                EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView")).Focus();EditorApplication.EnterPlaymode();
            }
            if(!EditorApplication.isPlaying){stage=-1;return;}
            if(SessionState.GetBool("KartQualityPending",false))
            {
                SessionState.SetBool("KartQualityPending",false);report="Wide raceway / actual Unity physics regression\n";started=EditorApplication.timeSinceStartup;Next(0);
            }
            if(stage<0)return;
            try
            {
                if(EditorApplication.timeSinceStartup-started>330)throw new Exception("Quality run exceeded 330 seconds.");
                Step();
            }
            catch(Exception e){stage=-1;File.WriteAllText("Logs/Kart/quality-error.txt",report+e);Debug.LogException(e);}
        }
        static void Step()
        {
            if(stage==0)
            {
                if(EditorApplication.timeSinceStartup-started<1)return;
                race=Object.FindAnyObjectByType<KartRace>();race.SetMode(1);race.ResetRace();
                Require(race.track.roadWidth>=18,"18m racing ribbon has room for four 3.2m-wide karts and passing clearance");
                Require(race.track.openEdges.Length==3,"Three deliberate open-edge sections are recorded in the track");
                race.racers[2].gameObject.SetActive(false);race.racers[3].gameObject.SetActive(false);
                var other=race.racers[1];other.isPlayer=true;
                Move(race.player,0,-4.5f);Move(other,3,4.5f);other.SetInput(.35f,0);race.locomotion.testForward=1;
                contacts=race.player.CollisionCount;axis=race.track.Forward(0);Next(1);
            }
            else if(stage==1)
            {
                if(Time.time-at<3.1f)return;
                float lead=Vector3.Dot(race.player.Body.position-race.racers[1].Body.position,axis);
                Require(lead>3&&race.player.Grounded&&race.player.CollisionCount==contacts,"A faster kart overtakes a slower kart in parallel lanes without contact (lead "+lead.ToString("F1")+"m)");
                race.locomotion.StopMotion();race.racers[1].SetInput(0,0);Move(race.player,3,-4);Move(race.racers[1],3,0);
                race.player.Body.rotation=Quaternion.LookRotation(race.track.Right(3));race.player.Body.linearVelocity=race.track.Right(3)*10;
                targetStart=race.racers[1].Body.position;axis=race.track.Right(3);contacts=race.player.CollisionCount;Next(2);
            }
            else if(stage==2)
            {
                if(Time.time-at<.75f)return;
                float shift=Vector3.Dot(race.racers[1].Body.position-targetStart,axis);
                Require(race.player.CollisionCount>contacts&&shift>.35f,"Side impact physically pushes the receiving kart (displacement "+shift.ToString("F2")+"m)");
                race.racers[1].gameObject.SetActive(false);Move(race.player,5);race.locomotion.testForward=1;
                race.player.Body.linearVelocity=race.player.Body.rotation*Vector3.forward*18;beforeSpeed=race.player.Speed;hitCount=race.player.HitCount;
                Require(race.player.Hit(Vector3.zero),"Orange hit is accepted outside its immunity window");minSpeed=race.player.Speed;
                Require(minSpeed<beforeSpeed*.6f,"Orange hit immediately removes more than 40 percent of speed");
                Require(!race.player.Hit(Vector3.zero),"Brief immunity prevents repeated stun locking from simultaneous oranges");Next(3);
            }
            else if(stage==3)
            {
                minSpeed=Mathf.Min(minSpeed,race.player.Speed);
                if(Time.time-at<.25f)return;
                var feedback=race.player.GetComponent<KartFeedback>();
                Require(feedback.HitVisualActive&&Quaternion.Angle(feedback.visualRoot.localRotation,Quaternion.identity)>1&&Object.FindAnyObjectByType<KartImpactBurst>(),"Orange impact creates a visible animated wobble, orbiting stars and juice burst");
                ScreenCapture.CaptureScreenshot("Logs/Kart/orange-hit.png");Next(4);
            }
            else if(stage==4)
            {
                if(Time.time-at<1.25f)return;
                Require(race.player.StunRemaining==0&&!race.player.GetComponent<KartFeedback>().HitVisualActive&&race.player.Speed>minSpeed+2,"Hit animation and slowdown expire; held throttle accelerates normally again");
                Move(race.player,3);race.locomotion.testForward=.6f;race.locomotion.testSteering=1;previousSteer=race.player.Steering;maxSteerStep=0;Next(5);
            }
            else if(stage==5)
            {
                maxSteerStep=Mathf.Max(maxSteerStep,Mathf.Abs(race.player.Steering-previousSteer));previousSteer=race.player.Steering;
                if(Time.time-at<.22f)return;
                Require(race.player.Steering>.5f&&maxSteerStep<.7f,"Steering ramps to held input without an instantaneous full-lock jump");
                race.locomotion.StopMotion();Move(race.player,3,6);race.player.Body.rotation=Quaternion.LookRotation(race.track.Right(3));race.player.Body.linearVelocity=race.track.Right(3)*12;contacts=race.player.CollisionCount;Next(6);
            }
            else if(stage==6)
            {
                if(Time.time-at<.7f)return;
                Require(race.player.CollisionCount>contacts&&race.player.Body.position.y>race.track.Point(3).y-1,"Guarded section stops an outward impact at the physical rail");
                Move(race.player,56,7);race.player.Body.rotation=Quaternion.LookRotation(race.track.Right(56));race.player.Body.linearVelocity=race.track.Right(56)*12;
                fallY=race.player.Body.position.y;Next(7);
            }
            else if(stage==7)
            {
                if(Time.time-at<.8f)return;
                Require(!race.player.Grounded&&race.player.Body.position.y<fallY-1.5f&&race.player.Recoveries==0,"The same outward movement crosses an unguarded edge and genuinely falls under gravity");
                ScreenCapture.CaptureScreenshot("Logs/Kart/open-edge-fall.png");Next(8);
            }
            else if(stage==8)
            {
                if(Time.time-at<1.5f)return;
                Require(race.player.Recoveries==1&&race.player.Body.position.y>10,"A fall returns to the last passed checkpoint without awarding progress");
                foreach(var kart in race.racers){kart.gameObject.SetActive(true);kart.enabled=true;kart.isPlayer=false;}
                race.player.aiLane=-4.7f;race.player.aiPace=.8f;race.totalLaps=3;race.StartRace();Next(9);
            }
            else if(stage==9)
            {
                File.WriteAllText("Logs/Kart/quality-race-progress.txt","Elapsed "+race.Elapsed.ToString("F2")+"\n"+string.Join("\n",race.racers.Select(k=>k.racerName+" laps="+k.CompletedLaps+" gate="+k.NextGate+" point="+k.NearestPoint+" speed="+k.Speed.ToString("F1")+" recoveries="+k.Recoveries+" pos="+k.Body.position)));
                if(!race.racers.All(k=>k.Finished))
                {
                    if(race.Elapsed>240)throw new Exception("Four-kart three-lap race did not finish within 240 seconds.");return;
                }
                Require(race.racers.All(k=>k.Recoveries==0),"All four karts physically complete three laps with zero falls or checkpoint recoveries ("+race.Elapsed.ToString("F1")+"s)");
                report+="KART_QUALITY_VALIDATION_OK\n";File.WriteAllText("Logs/Kart/quality-checks.txt",report);
                race.player.isPlayer=true;race.SetMode(2);race.ResetRace();ScreenCapture.CaptureScreenshot("Logs/Kart/game-view.png");stage=-1;
            }
        }
    }
}
