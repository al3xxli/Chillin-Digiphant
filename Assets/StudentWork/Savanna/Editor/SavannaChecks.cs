using System;
using System.IO;
using DigiPhant;
using UnityEditor;
using UnityEngine;

namespace ChillinDigiphant.Savanna.Editor
{
    [InitializeOnLoad]
    public static class SavannaChecks
    {
        static double playStarted;
        static SavannaChecks()
        {
            EditorApplication.playModeStateChanged += state =>
            { if (state == PlayModeStateChange.EnteredPlayMode) playStarted = EditorApplication.timeSinceStartup; };
            EditorApplication.update += Update;
        }
        static void Assert(bool value, string message) { if (!value) throw new Exception(message); }
        public static void CheckHold()
        {
            var g = new InteractionHold();
            for (int i=0;i<20;i++) Assert(!g.Step(0,true,1,.1f,.8f),"Entering while holding must not activate.");
            g.Step(0,true,0,.1f,.8f);
            for(int i=0;i<3;i++) Assert(!g.Step(0,true,1,.1f,.8f),"Hold completed too soon.");
            g.Step(0,false,1,.1f,.8f);
            for(int i=0;i<20;i++) Assert(!g.Step(0,true,1,.1f,.8f),"Tracking loss must require release.");
            g.Step(0,true,0,.1f,.8f); int count=0;
            for(int i=0;i<30;i++) if(g.Step(0,true,1,.1f,.8f)) count++;
            Assert(count==1,"Held gesture must fire exactly once.");
            g.Step(0,true,0,.1f,.8f); g.Step(0,true,1,.1f,.8f);
            for(int i=0;i<20;i++) Assert(!g.Step(1,true,1,.1f,.8f),"Switching zones must require a fresh gesture.");
            Assert(!g.Step(-1,true,0,.1f,.8f),"Outside zones cannot activate.");
        }
        [MenuItem("DigiPhant/Savanna/Run Play mode interaction checks")]
        public static void Run()
        {
            SavannaBuilder.Validate();
            SessionState.SetBool("SavannaChecksRequested",true);
            if(!EditorApplication.isPlaying) EditorApplication.EnterPlaymode();
            else playStarted=EditorApplication.timeSinceStartup;
        }
        static void Update()
        {
            if(!EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(".tools/test-savanna"))
            { File.Delete(".tools/test-savanna"); try { Run(); } catch(Exception e) { Fail(e); } }
            if(!EditorApplication.isPlaying || !SessionState.GetBool("SavannaChecksRequested",false) || EditorApplication.timeSinceStartup-playStarted<2) return;
            SessionState.SetBool("SavannaChecksRequested",false);
            try
            {
                var h=UnityEngine.Object.FindFirstObjectByType<SavannaHabitat>();
                Assert(h!=null,"Habitat missing in Play mode.");
                h.controller.SetInputMode(InputMode.TestSliders); h.ResetHabitat();
                var travel=h.locomotion.travelRoot; Vector3 start=travel.position;
                h.locomotion.testForward=.55f;
                for(int i=0;i<20;i++) h.locomotion.Evaluate(Time.realtimeSinceStartup,.05f);
                Assert(Vector3.Distance(start,travel.position)>.3f,"Elephant cannot move.");
                h.locomotion.StopMotion();
                float now=Time.time;
                travel.position=new Vector3(0,start.y,-18);
                h.TickInteraction(now,.1f,true,0);
                for(int j=0;j<20;j++) h.TickInteraction(now+j*.1f,.1f,true,1);
                Assert(h.TotalInteractions==0,"Action outside a zone fired.");
                for(int i=0;i<3;i++)
                {
                    now+=4;
                    Vector3 pos=h.zones[i].anchor.position; pos.y=start.y; travel.position=pos;
                    h.TickInteraction(now,.1f,true,0);
                    for(int j=0;j<10;j++) h.TickInteraction(now+j*.1f,.1f,true,1);
                    Assert(h.zones[i].uses==1,"Zone failed to activate: "+h.zones[i].title);
                }
                Assert(!h.zones[1].food.activeSelf,"Food must disappear after feeding.");
                Vector3 oldBall=h.ball.position;
                for(int i=0;i<15;i++) h.TickEffects(now+i*.1f,.1f);
                Assert(Vector3.Distance(oldBall,h.ball.position)>1,"Trunk nudge failed to move ball.");
                h.TickInteraction(now+20,.1f,true,0);
                Assert(h.zones[1].food.activeSelf,"Food did not regrow.");
                Assert(h.TotalInteractions==3,"Unexpected repeated activation.");
                h.ResetHabitat();
                Assert(h.TotalInteractions==0 && Vector3.Distance(travel.position,start)<.01f,"Reset failed.");
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/savanna-checks.txt","SAVANNA_PLAY_CHECKS_OK\nHold / release, tracking-loss reset, zone switching, out-of-range rejection, actual locomotion, all three actions, fruit depletion and regrowth, ball rolling, habitat reset.\nCamera gestures still require live performer testing.");
                Debug.Log("SAVANNA_PLAY_CHECKS_OK");
            }
            catch(Exception e) { Fail(e); }
            finally { EditorApplication.ExitPlaymode(); }
        }
        static void Fail(Exception e) { Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/savanna-checks-error.txt",e.ToString()); Debug.LogException(e); }
    }
}
