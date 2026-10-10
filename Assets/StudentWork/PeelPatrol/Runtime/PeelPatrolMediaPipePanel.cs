using System;
using DigiPhant;
using UnityEngine;

namespace ChillinDigiphant.PeelPatrol
{
    // Shared view for the Game sidebar and the dockable Unity editor tab.
    // Tracking, preview, calibration and rig ownership stay with the instructor's components.
    public sealed class PeelPatrolMediaPipePanel : MonoBehaviour
    {
        public PeelPatrolGame game;
        Vector2 gameScroll;
        bool showRig, showSignals;
        [NonSerialized] GUIStyle title, text, note, action, toggle, zoneLabel;
        public DigiPhantCameraPreview Preview => game.controller.GetComponent<DigiPhantCameraPreview>();
        void OnEnable() { title=text=note=action=toggle=zoneLabel=null; }
        void Styles()
        {
            if(title!=null) return;
            title=new GUIStyle(GUI.skin.label){fontSize=23,fontStyle=FontStyle.Bold};
            text=new GUIStyle(GUI.skin.label){fontSize=14,wordWrap=true};
            note=new GUIStyle(text){fontSize=12};
            action=new GUIStyle(GUI.skin.button){fontSize=13,wordWrap=true};
            toggle=new GUIStyle(GUI.skin.toggle){fontSize=13,wordWrap=true};
            zoneLabel=new GUIStyle(GUI.skin.label){fontSize=18,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};
            zoneLabel.normal.textColor=Color.white;
            foreach(var style in new[]{title,text,note,toggle}) style.normal.textColor=new Color(.96f,.96f,.88f);
        }
        void OnGUI()
        {
            if(!game || !game.showMediaPipePanel) return;
            float scale=Mathf.Clamp(Screen.height/850f,.6f,1.8f);
            var matrix=GUI.matrix;var color=GUI.color;
            GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
            float width=game.SidebarPixels/scale;
            GUI.color=new Color(.065f,.105f,.09f);GUI.DrawTexture(new Rect(0,0,width,Screen.height/scale),Texture2D.whiteTexture);GUI.color=Color.white;
            GUILayout.BeginArea(new Rect(12,14,width-24,Screen.height/scale-28));
            DrawContents(width-24,ref gameScroll);
            GUILayout.EndArea();GUI.matrix=matrix;GUI.color=color;
        }
        public void DrawContents(float width,ref Vector2 scroll)
        {
            if(!game) return;
            Styles();var c=game.controller;var preview=Preview;
            scroll=GUILayout.BeginScrollView(scroll);
            float content=Mathf.Max(120,width-30);
            bool compact=content<260;
            GUILayout.BeginVertical(GUILayout.Width(content));
            GUILayout.Label("MEDIAPIPE",title);
            GUILayout.Label("DigiPhant / camera and movement controls",note);
            GUILayout.Space(6);
            int mode=game.CameraMode?0:game.KeyboardTest?2:1;
            int chosen=GUILayout.Toolbar(mode,compact?new[]{"Camera","Sliders","Keys"}:new[]{"Camera","Test sliders","Keyboard"},action,GUILayout.Height(29));
            if(chosen!=mode)
            {
                if(chosen==0) game.SetControlMode(InputMode.Camera);
                else if(chosen==1) game.SelectSliders();
                else game.SetControlMode(InputMode.TestSliders);
            }
            if(preview)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Camera "+preview.cameraIndex+(compact?"":" (0 = default)"),note);
                GUI.enabled=!game.CameraConnected;
                if(GUILayout.Button("-",action,GUILayout.Width(28)))preview.cameraIndex=Mathf.Max(0,preview.cameraIndex-1);
                if(GUILayout.Button("+",action,GUILayout.Width(28)))preview.cameraIndex=Mathf.Min(8,preview.cameraIndex+1);
                GUI.enabled=true;GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                if(GUILayout.Button(game.CameraConnected?"Restart camera":"START CAMERA",action,GUILayout.Height(compact?44:32)))game.ConnectCamera();
                GUI.enabled=game.CameraConnected;
                if(GUILayout.Button("Stop camera",action,GUILayout.Height(compact?44:32)))game.DisconnectCamera();
                GUI.enabled=true;GUILayout.EndHorizontal();
                var picture=GUILayoutUtility.GetRect(content,Mathf.Min(content*.75f,178),GUILayout.ExpandWidth(true));
                GUI.DrawTexture(picture,Texture2D.blackTexture);
                if(preview.HasLiveFrame) preview.DrawPreview(picture);
                else GUI.Label(new Rect(picture.x+12,picture.y+18,picture.width-24,picture.height-25),game.CameraConnected?"Waiting for the camera...\nThe skeleton preview will appear here.":"1. Start camera\n2. Select your group and framing\n3. Set neutral pose\n4. Practice, then start waves",text);
                // Draw in Unity too: guides must not depend on a restarted Python bridge
                // or on JPEG compression preserving thin lines in a small preview.
                if(c.performerCount==3) DrawThirdsGuide(picture,preview.PreviewTexture);
                bool mirrored=GUILayout.Toggle(preview.mirrorPreview,"Flip camera image (mirror adjustment)",toggle);
                if(mirrored!=preview.mirrorPreview)preview.SetMirrorPreview(mirrored);
                GUILayout.Label("Raise your LEFT hand: it should move on the LEFT side of your image. Flip the checkbox if the camera already mirrors itself, then recalibrate.",note);
                GUILayout.Label(game.CameraConnected?preview.Status:"Camera is off. Test sliders work without it.",note);
            }
            GUILayout.Space(5);
            GUILayout.Label("Performers",text);
            int count=GUILayout.Toolbar(c.performerCount-1,compact?new[]{"1","2","3","4"}:new[]{"1 / Solo","2","3 / Team","4"},action,GUILayout.Height(28))+1;
            if(count!=c.performerCount)game.UsePerformers(count);
            if(count==3) GUILayout.Label("CAMERA ZONES: screen-left P1 / center P2 / screen-right P3. Stand one person in each outlined zone. The mirror checkbox flips the picture and updates zone assignment. Recalibrate after changing it or changing places.",note);
            bool upper=GUILayout.Toolbar(c.upperBodyOnly?1:0,compact?new[]{"Full body","Seated"}:new[]{"Full body","Seated / upper body"},action,GUILayout.Height(28))==1;
            if(upper!=c.upperBodyOnly)game.UseUpperBody(upper);
            GUILayout.Label(upper?"Upper body works standing or seated. Keep shoulders and hands visible; feet are not needed.":"Keep shoulders, hips and hands visible. Feet only animate the legs.",note);
            if(game.CameraMode)
            {
                string people="";
                for(int i=1;i<=c.performerCount;i++) people+=(i>1?"   ":"")+"P"+i+": "+(c.IsTracked(i,Time.realtimeSinceStartup)?"visible":"waiting");
                GUILayout.Label(people,note);
                GUI.enabled=game.CameraConnected;
                if(c.CalibrationPending)
                {
                    GUILayout.Label("Hold neutral pose: "+c.CalibrationSecondsRemaining+" seconds",text);
                    if(GUILayout.Button("Cancel countdown",action))c.CancelCalibrationCountdown();
                }
                else if(GUILayout.Button(c.IsCalibrated?"Recalibrate / 10 seconds":"SET NEUTRAL POSE / 10 seconds",action,GUILayout.Height(34)))game.BeginNeutralPose();
                if(GUILayout.Button("Reassign people",action))game.ReassignPeople();
                GUI.enabled=true;
                GUILayout.Label(game.CameraConnected?c.Status:"Start the camera to calibrate. Use Solo for one-person testing.",note);
                GUILayout.Label("MOVEMENT: "+game.MovementStatus,text);
                GUILayout.Label("ACTIONS: "+game.ActionStatus,note);
                GUILayout.Label("Neutral: BOTH hands lowered by your sides, wrists visible. P1: left up = forward, right up = reverse. P2: left up = left turn, right up = right turn. Move and turn together.",note);
            }
            GUILayout.Space(6);
            if(game.Round.Phase==RoundPhase.Ready)
            {
                bool practice=GUILayout.Toggle(game.PracticeMode,"Practice movements / no waves",toggle);
                if(practice!=game.PracticeMode)game.SetPractice(practice);
                GUI.enabled=!game.CameraMode || !game.TrackingBlocked;
                if(GUILayout.Button("START DEFENSE WAVES",action,GUILayout.Height(35)))game.StartRound();
                GUI.enabled=true;
            }
            else if(game.Round.Active)
            {
                GUILayout.Label(game.CameraMode && game.TrackingBlocked?"Waves held until tracking and calibration are ready.":game.Paused?"Round paused.":"Round running / controls stay available.",note);
                if(GUILayout.Button(game.Paused?"Resume round":"Pause round",action))game.TogglePause();
            }
            if(!game.CameraMode && !game.KeyboardTest)
            {
                GUILayout.Label("Travel / backward to forward",note);
                game.locomotion.testForward=GUILayout.HorizontalSlider(game.locomotion.testForward,-1,1);
                GUILayout.Label("Steering / left to right",note);
                game.locomotion.testSteering=GUILayout.HorizontalSlider(game.locomotion.testSteering,-1,1);
                if(GUILayout.Button("Stop moving",action))game.locomotion.StopMotion();
                showRig=GUILayout.Toggle(showRig,"Body-part test sliders",toggle);
                if(showRig)
                {
                    foreach(var control in c.controls)
                    {
                        GUILayout.Label(control.label+" / P"+control.performer,note);
                        control.testValue=GUILayout.HorizontalSlider(control.testValue,-1,1);
                    }
                    if(GUILayout.Button("Reset body sliders",action))c.ResetControls();
                }
                GUI.enabled=game.GameplayEnabled && !game.SimulationPaused;
                if(GUILayout.Button("Throw orange",action))game.RequestThrow();
                if(GUILayout.Button("Use current station",action))game.UseStation(game.GetStation(game.locomotion.travelRoot.position));
                GUI.enabled=true;
            }
            GUILayout.Space(5);
            GUILayout.Label("P1: LEFT up = forward / RIGHT up = backward\nP"+Mathf.Min(2,c.performerCount)+": LEFT up = turn left / RIGHT up = turn right\nP"+game.ActionPerformer+": hold LEFT up = auto throw (1/sec) / RIGHT up = reload at grove or use station.",note);
            GUILayout.Label("Calibrate with BOTH hands lowered and visible. P3: hold YOUR LEFT to throw; lower to stop. RIGHT reloads at the grove and takes priority at stations. Practice includes throwing and reloading. P1 and P2 can act at the same time.",note);
            if(game.CameraMode)
            {
                GUILayout.Label(game.Round.Ammo==0?"BASKET EMPTY / use the orange grove":game.AutoThrowing?"AUTO THROW / 1 orange per second":"Hold LEFT hand up to throw; lower to stop.",text);
                if(game.StationProgress>0) GUILayout.Label("Using station: "+Mathf.RoundToInt(game.StationProgress*100)+"% / keep RIGHT hand raised",text);
                if(game.CameraConnected && game.ThrowBlocked)
                    GUILayout.Label("Throwing needs YOUR LEFT wrist visible and calibrated. RIGHT tracking only affects reload. L/R above the preview mark your own sides.",note);
            }
            showSignals=GUILayout.Toggle(showSignals,"Live movement meters",toggle);
            if(showSignals)
            {
                Channel("Forward / left hand",1,Movement.LeftHandHeight,content);
                Channel("Reverse / right hand",1,Movement.RightHandHeight,content);
                Channel("Turn left / left hand",Mathf.Min(2,c.performerCount),Movement.LeftHandHeight,content);
                Channel("Turn right / right hand",Mathf.Min(2,c.performerCount),Movement.RightHandHeight,content);
                Channel("Throw / left hand",game.ActionPerformer,Movement.LeftHandHeight,content);
                Channel("Reload / right hand",game.ActionPerformer,Movement.RightHandHeight,content);
            }
            var recording=c.GetComponent<DigiPhantRecording>();
            if(recording) {GUILayout.Space(8);recording.DrawControls(content);}
            GUILayout.Space(8);
            game.showMediaPipePanel=GUILayout.Toggle(game.showMediaPipePanel,"Show sidebar in Game view (Tab)",toggle);
            GUILayout.Label("Dockable tab: DigiPhant > MediaPipe Controls\nStopping Play closes Unity's camera bridge.",note);
            GUILayout.EndVertical();GUILayout.EndScrollView();
        }
        void DrawThirdsGuide(Rect picture,Texture cameraImage)
        {
            Rect frame=picture;
            if(cameraImage && cameraImage.width>0 && cameraImage.height>0)
            {
                float fit=Mathf.Min(picture.width/cameraImage.width,picture.height/cameraImage.height);
                frame=new Rect(picture.center.x-cameraImage.width*fit*.5f,picture.center.y-cameraImage.height*fit*.5f,
                    cameraImage.width*fit,cameraImage.height*fit);
            }
            Color previous=GUI.color;
            void Fill(Rect rect,Color color) { GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture); }
            Color red=new Color(1,0,0,1);
            // Coordinates match ScaleToFit's actual image, including letterboxing.
            Fill(new Rect(frame.x,frame.y,frame.width,44),red);
            Fill(new Rect(frame.x,frame.y,4,frame.height),red);
            Fill(new Rect(frame.xMax-4,frame.y,4,frame.height),red);
            Fill(new Rect(frame.x,frame.yMax-4,frame.width,4),red);
            for(int split=1;split<=2;split++)
            {
                float x=frame.x+frame.width*split/3f;
                Fill(new Rect(x-5,frame.y,10,frame.height),Color.black);
                Fill(new Rect(x-3,frame.y,6,frame.height),red);
            }
            GUI.color=Color.white;
            for(int zone=0;zone<3;zone++)
            {
                float x=frame.x+zone*frame.width/3f,width=frame.width/3f;
                GUI.Label(new Rect(x,frame.y,width,22),"P"+(zone+1),zoneLabel);
                // Fixed targets for the desired mirror view, even with a pre-mirrored webcam.
                GUI.Label(new Rect(x+3,frame.y+21,width*.4f,22),"L",zoneLabel);
                GUI.Label(new Rect(x+width*.6f-3,frame.y+21,width*.4f,22),"R",zoneLabel);
            }
            GUI.color=previous;
        }
        void Channel(string label,int performer,Movement movement,float width)
        {
            bool valid=game.controller.TryReadMovement(performer,movement,Time.realtimeSinceStartup,out float value);
            GUILayout.Label(label+" / P"+performer+": "+(valid?value.ToString("+0.00;-0.00;0.00"):"waiting for calibration / tracking"),note);
            var bar=GUILayoutUtility.GetRect(width,8);var old=GUI.color;
            GUI.color=new Color(.2f,.27f,.23f);GUI.DrawTexture(bar,Texture2D.whiteTexture);
            if(valid)
            {
                float amount=Mathf.Clamp(value,-1,1)*bar.width*.5f;
                GUI.color=new Color(.35f,.85f,.7f);GUI.DrawTexture(new Rect(bar.center.x+Mathf.Min(0,amount),bar.y,Mathf.Abs(amount),bar.height),Texture2D.whiteTexture);
            }
            GUI.color=Color.white;GUI.DrawTexture(new Rect(bar.center.x,bar.y,1,bar.height),Texture2D.whiteTexture);GUI.color=old;
        }
    }
}
