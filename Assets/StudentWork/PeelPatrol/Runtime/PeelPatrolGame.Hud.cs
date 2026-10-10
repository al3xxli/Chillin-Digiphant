using DigiPhant;
using UnityEngine;

namespace ChillinDigiphant.PeelPatrol
{
    public sealed partial class PeelPatrolGame
    {
        GUIStyle elephantCount, elephantState;
        void OnGUI()
        {
            if(!controller) return;
            float scale=Mathf.Clamp(Screen.height/850f,.6f,1.8f);
            var old=GUI.matrix; GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
            float w=Screen.width/scale,h=Screen.height/scale;
            if(heading==null || smallButton==null)
            {
                heading=new GUIStyle(GUI.skin.label){fontSize=27,fontStyle=FontStyle.Bold};
                label=new GUIStyle(GUI.skin.label){fontSize=17,wordWrap=true};
                small=new GUIStyle(label){fontSize=13}; big=new GUIStyle(heading){fontSize=36};
                button=new GUIStyle(GUI.skin.button){fontSize=18,fontStyle=FontStyle.Bold};
                smallButton=new GUIStyle(GUI.skin.button){fontSize=13,wordWrap=true};
                elephantCount=new GUIStyle(label){fontSize=14,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};
                elephantState=new GUIStyle(small){fontSize=10,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};
            }
            float x=SidebarPixels/scale+24;
            Panel(new Rect(x,22,300,147));
            GUI.Label(new Rect(x+18,32,264,37),"PEEL PATROL",heading);
            GUI.Label(new Rect(x+18,75,268,25),"CLEAN WATER   "+Round.Water+" / 100",small);
            Bar(new Rect(x+18,106,264,10),Round.Water/100f,new Color(.33f,.82f,.7f));
            GUI.Label(new Rect(x+18,128,264,26),CameraMode?"MEDIAPIPE / "+controller.performerCount+" PERFORMERS":KeyboardTest?"KEYBOARD TEST MODE":"BODY AND MOVEMENT SLIDERS",small);
            if(GUI.Button(new Rect(x,179,230,30),showMediaPipePanel?"HIDE MEDIAPIPE PANEL (TAB)":"SHOW MEDIAPIPE PANEL (TAB)",smallButton))showMediaPipePanel=!showMediaPipePanel;
            Panel(new Rect(w-324,22,300,205));
            string wave=Round.Phase==RoundPhase.Ready?"PRACTICE / NO WAVES":Round.Phase==RoundPhase.Preparing?"NEXT WAVE IN "+Mathf.CeilToInt(Round.Countdown):"WAVE "+Round.Wave+" / 3";
            GUI.Label(new Rect(w-306,34,264,30),wave,label);
            GUI.Label(new Rect(w-306,69,264,35),"ORANGES  "+Round.Ammo+"/10",heading);
            for(int i=0;i<DefenseRound.Capacity;i++) Bar(new Rect(w-306+i*26,113,20,11),i<Round.Ammo?1:0,new Color(1,.56f,.13f));
            GUI.Label(new Rect(w-306,133,264,30),"COINS  "+Round.Coins+"   /   "+Round.Ride,label);
            GUI.Label(new Rect(w-306,171,264,38),"Stopped: "+Round.Defeated+"   Incoming: "+EnemyCount+"\nSpeed: "+Round.SpeedMultiplier.ToString("0.00")+"x",small);
            if(Round.Active)
            {
                if(GUI.Button(new Rect(w-324,237,171,36),ShopOpen?"CLOSE SHOP":"UPGRADES",button)) ToggleShop();
                if(GUI.Button(new Rect(w-143,237,119,36),Paused?"RESUME":"PAUSE",button)) TogglePause();
                WorldLabel(orangeStation.position,"ORANGE GROVE / refill to 10",scale);
                WorldLabel(workshop.position,"WORKSHOP / 4 oranges",scale);
                WorldLabel(cleanStation.position,"CLEAN WATER / 3 oranges",scale);
                foreach(var lane in lanes) if(lane.durability>0) WorldLabel(lane.path[1].position,"BARRICADE  "+Mathf.CeilToInt(lane.durability),scale);
                string use=CameraMode?"Hold P"+ActionPerformer+"'s RIGHT hand up":"Hold E";
                string hint=ActiveStation==0?use+" to refill (then carry oranges anywhere)":ActiveStation==1?use+" to build a barricade (4 oranges)":ActiveStation==2?use+" to clean water (3 oranges)":Round.Ammo==0?"BASKET EMPTY / visit the orange grove to reload":Target?"TARGET LOCKED / throw an orange!":"NO TARGET LOCK / throws go forward";
                if(AutoThrowing) hint="AUTO THROW / hold LEFT hand up for 1 orange per second";
                if(CameraMode && ThrowBlocked) hint="ACTIONS HELD / "+ActionStatus;
                if(CameraMode && TrackingBlocked) hint="MOVEMENT HELD / "+MovementStatus;
                float width=Mathf.Min(790,w-x-24); Panel(new Rect(x,h-117,width,93));
                GUI.Label(new Rect(x+17,h-107,width-34,33),messageTime>0?message:hint,label);
                GUI.Label(new Rect(x+17,h-67,width-34,34),CameraMode?"P1: L/R = forward/back   P"+Mathf.Min(2,controller.performerCount)+": L/R = turn left/right\nP"+ActionPerformer+": hold LEFT up = auto throw (1/sec) / RIGHT up = reload or use zone.":KeyboardTest?"WASD / arrows: move   Shift: run   Space: throw   E: use zone   U: upgrades   P: pause":"Use the movement sliders and action buttons in the MediaPipe panel.",small);
                if(StationProgress>0) Bar(new Rect(x,h-28,width,4),StationProgress,new Color(1,.64f,.2f));
                DrawIncoming(w,h,scale);
            }
            if(Round.Phase==RoundPhase.Ready && PracticeMode)WorldLabel(orangeStation.position,"ORANGE GROVE / raise RIGHT to reload",scale);
            DrawElephantStatus(scale);
            if(flash>0) { GUI.color=new Color(1,.34f,.22f,flash*.22f);GUI.DrawTexture(new Rect(0,0,w,h),Texture2D.whiteTexture);GUI.color=Color.white; }
            if(ShopOpen) DrawShop(x,w,h);
            else if(Round.Phase==RoundPhase.Won || Round.Phase==RoundPhase.Lost || Paused || (!showMediaPipePanel && (Round.Phase==RoundPhase.Ready || SimulationPaused))) DrawRoundPanel(x,w,h);
            GUI.color=Color.white; GUI.matrix=old;
        }
        void DrawElephantStatus(float scale)
        {
            var camera=locomotion.followCamera;
            if(!camera || !locomotion.travelRoot) return;
            Vector3 anchor=statusHead?statusHead.position:locomotion.travelRoot.position;
            var screen=camera.WorldToScreenPoint(anchor);
            if(screen.z<=0) return;
            float top=screen.y;
            void Include(Renderer renderer)
            {
                if(!renderer || !renderer.enabled || !renderer.gameObject.activeInHierarchy)return;
                var bounds=renderer.bounds;
                for(int i=0;i<8;i++)
                {
                    var corner=bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                    var projected=camera.WorldToScreenPoint(corner);
                    if(projected.z>0)top=Mathf.Max(top,projected.y);
                }
            }
            if(statusMeshes!=null)foreach(var mesh in statusMeshes)Include(mesh);
            if(statusCargo!=null)foreach(var orange in statusCargo)Include(orange);
            var area=camera.pixelRect;
            if(screen.x<area.xMin || screen.x>area.xMax)return;
            const float height=34;
            float width=Mathf.Min(136,area.width/scale-16);
            if(width<90)return;
            float y=(Screen.height-top)/scale-height-6;
            // Do not clamp down onto the elephant when it reaches the top of the view.
            if(y<(Screen.height-area.yMax)/scale+2)return;
            var box=new Rect(Mathf.Clamp(screen.x/scale-width*.5f,area.xMin/scale+8,area.xMax/scale-width-8),y,width,height);
            Panel(box);
            GUI.color=new Color(1,.58f,.12f);
            GUI.DrawTexture(new Rect(box.x,box.y,box.width,2),Texture2D.whiteTexture);
            GUI.Label(new Rect(box.x+4,box.y+2,box.width-8,18),Round.Ammo+" / "+DefenseRound.Capacity+" ORANGES",elephantCount);
            string status=!GameplayEnabled?"START WAVES":
                SimulationPaused?"PAUSED / TRACKING":
                Round.Ammo==0?"EMPTY / RELOAD":
                CameraMode && ThrowBlocked?"SHOW P3 LEFT HAND":
                StationProgress>0?"USING STATION":
                AutoThrowing?"AUTO / 1 PER SECOND":
                NextThrowSeconds>.01f?"NEXT IN "+NextThrowSeconds.ToString("0.0")+"s":"READY TO THROW";
            GUI.color=Round.Ammo==0?new Color(1,.45f,.3f):Color.white;
            GUI.Label(new Rect(box.x+3,box.y+20,box.width-6,12),status,elephantState);
            GUI.color=Color.white;
        }

        void DrawIncoming(float w,float h,float scale)
        {
            if(!Camera.main || SimulationPaused) return;
            for(int i=0;i<lanes.Length;i++)
            {
                BananaRaider first=null;
                foreach(var enemy in enemies) if(enemy.Lane==i && (!first || enemy.RemainingDistance<first.RemainingDistance)) first=enemy;
                if(!first) continue;
                var p=Camera.main.WorldToViewportPoint(first.transform.position);
                if(p.x>.03f && p.x<.97f && p.y>.03f && p.y<.97f && p.z>0) continue;
                float px=Mathf.Clamp(SidebarPixels/scale+p.x*(w-SidebarPixels/scale),SidebarPixels/scale+100,w-100),py=Mathf.Clamp((1-p.y)*h,290,h-158);
                var r=new Rect(px-80,py,160,26);Panel(r);GUI.Label(r,lanes[i].title.ToUpper()+" / INCOMING",small);
            }
        }
        void DrawShop(float x,float w,float h)
        {
            float cw=Mathf.Min(660,w-x-36),cx=x+(w-x-cw)*.5f,cy=(h-405)*.5f;
            Panel(new Rect(cx,cy,cw,405));
            GUI.Label(new Rect(cx+26,cy+22,cw-52,50),"RIDES FOR THE HERD",big);
            GUI.Label(new Rect(cx+28,cy+82,cw-56,53),"Coins: "+Round.Coins+"   /   +3 per banana, +8 per cleared wave\nThe round waits while you choose.",label);
            string[] names={"Skateboard", "Unicycle", "Jetpack"};
            string[] details={"+30% speed / 12 coins", "+65% speed / 20 coins", "+105% speed / 28 coins"};
            for(int i=0;i<3;i++)
            {
                GUI.Label(new Rect(cx+28,cy+150+i*48,cw-56,37),(Round.Upgrade==i+1?"EQUIPPED / ":Round.Upgrade>i+1?"OWNED / ":"")+names[i]+"     "+details[i],label);
            }
            GUI.enabled=Round.Active && Round.Upgrade<3 && Round.Coins>=Round.NextUpgradeCost;
            if(GUI.Button(new Rect(cx+28,cy+304,cw-56,40),Round.Upgrade==3?"FULLY UPGRADED":"BUY "+Round.NextRide.ToUpper()+" / "+Round.NextUpgradeCost+" COINS",button)) BuyUpgrade();
            GUI.enabled=true;
            if(GUI.Button(new Rect(cx+28,cy+352,cw-56,33),"BACK TO THE WATERING HOLE",button)) ToggleShop();
        }
        void DrawRoundPanel(float x,float w,float h)
        {
            bool terminal=Round.Phase==RoundPhase.Won || Round.Phase==RoundPhase.Lost;
            float cw=Mathf.Min(680,w-x-36),cx=x+(w-x-cw)*.5f,cy=(h-440)*.5f;
            Panel(new Rect(cx,cy,cw,440));
            string title=Round.Phase==RoundPhase.Ready?"BANANAS INCOMING!":Round.Phase==RoundPhase.Won?"WATER SAVED!":Round.Phase==RoundPhase.Lost?"THAT'S BANANAS.":TrackingBlocked&&CameraMode?"WAITING FOR THE HERD":"TAKE A BREATHER";
            GUI.Label(new Rect(cx+26,cy+20,cw-52,52),title,big);
            string detail=terminal?"Stopped "+Round.Defeated+" bananas. Earn coins for faster rides, reload at the grove, and carry your oranges around the savanna.":CameraMode?"P1 / Left up: forward. Right up: backward.\nP"+Mathf.Min(2,controller.performerCount)+" / Left up: turn left. Right up: turn right.\nP"+ActionPerformer+" / Hold left up: throw 1/sec. Right up: reload/use zone.":"WASD / arrows: travel and steer. Shift: run.\nSpace: throw. Hold E in a station to use it.\nOranges stay in your basket when you leave the grove.";
            GUI.Label(new Rect(cx+28,cy+88,cw-56,85),detail,label);
            if(!terminal)
            {
                if(GUI.Button(new Rect(cx+28,cy+183,(cw-66)*.55f,35),"MEDIAPIPE / CONNECT CAMERA",smallButton)) ConnectCamera();
                if(GUI.Button(new Rect(cx+cw*.57f,cy+183,cw*.43f-28,35),"KEYBOARD TEST",smallButton)) SetControlMode(InputMode.TestSliders);
                string status=CameraMode?(TrackingBlocked?"Round held: connect "+controller.performerCount+" performer(s) and set a neutral pose.":"Camera control roles are ready."):"Keyboard is an optional test control.";
                GUI.Label(new Rect(cx+28,cy+230,cw-56,48),status,small);
                if(CameraMode)
                {
                    GUI.enabled=cameraConnected;
                    if(GUI.Button(new Rect(cx+28,cy+280,cw-56,32),controller.CalibrationPending?"NEUTRAL POSE COUNTDOWN...":"SET NEUTRAL POSE / 10 SECOND COUNTDOWN",smallButton) && !controller.CalibrationPending)
                        BeginNeutralPose();
                    GUI.enabled=true;
                    GUI.Label(new Rect(cx+28,cy+319,cw-56,33),cameraConnected?controller.Status:"Connect the camera when your players are ready.",small);
                }
                else GUI.Label(new Rect(cx+28,cy+280,cw-56,66),"+3 coins per banana / +8 per cleared wave\nUpgrades pause the round. New games reset coins and rides.",small);
            }
            GUI.enabled=terminal || !CameraMode || !TrackingBlocked;
            if(GUI.Button(new Rect(cx+28,cy+374,cw-56,43),terminal?"PLAY AGAIN":Round.Phase==RoundPhase.Ready?"DEFEND THE WATERING HOLE":"RESUME",button))
            {
                if(terminal) ResetRound();
                else { controller.showControls=false; Paused=false; StartRound(); }
            }
            GUI.enabled=true;
        }
    }
}
