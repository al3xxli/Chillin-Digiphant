using DigiPhant;
using UnityEngine;
using UnityEngine.Serialization;

namespace ChillinDigiphant.Kart
{
    public enum KartCameraLayout { PoseTeam, MixedTeam }
    public sealed partial class KartRace
    {
        public KartCameraLayout cameraLayout = KartCameraLayout.MixedTeam;
        public bool literalBodyActions = true;
        [FormerlySerializedAs("invertFaceSteering")] public bool invertShoulderSteering = true;
        [Range(.06f,.24f)] public float jumpSensitivity = .10f;
        [Range(.20f,.65f)] public float duckDepth = .32f;
        [FormerlySerializedAs("faceTurnRange"), Range(.15f,.5f)] public float shoulderTurnRange = .28f;
        public readonly KartCameraInput cameraInput = new KartCameraInput();
        public bool MixedInputs => cameraLayout == KartCameraLayout.MixedTeam;
        public int BodyPerformer => MixedInputs ? 1 : ActionPerformer;
        public int SteeringPerformer => Mathf.Min(2,controller.performerCount);
        public string ShoulderSteeringHelp => invertShoulderSteering ? "left shoulder turns RIGHT / right turns LEFT" : "left shoulder turns LEFT / right turns RIGHT";
        public bool CameraStartPending { get; private set; }
        public string CameraStartLabel => CameraStartPending ? "CANCEL CAMERA START" : Phase == RacePhase.Practice ? "START WITH CAMERA" : "RESTART WITH CAMERA";
        public string CameraStartStatus => controller.CalibrationPending ? "Hold neutral for " + controller.CalibrationSecondsRemaining + " seconds." + (CameraStartPending ? " The race will start automatically." : "") :
            CameraStartPending && Preview && !Preview.HasLiveFrame ? Preview.Status :
            CameraStartPending ? "Waiting for the team. " + MovementStatus : !CameraConnected ? "Start with Camera opens the camera and guides you through the neutral pose." : MovementStatus;
        bool cameraHooks;
        void AttachCameraInput()
        {
            if(cameraHooks)return;
            cameraHooks=true;
            controller.FrameReceived+=ReceiveCameraInput;
            controller.TrackingReset+=cameraInput.Reset;
            controller.CalibrationOverride=CalibrateCameraInput;
            controller.kartMixedInputs=MixedInputs;
        }
        void DetachCameraInput()
        {
            if(!cameraHooks || !controller)return;
            cameraHooks=false;
            controller.FrameReceived-=ReceiveCameraInput;
            controller.TrackingReset-=cameraInput.Reset;
            controller.CalibrationOverride=null;controller.kartMixedInputs=false;
        }
        void ReceiveCameraInput(PoseFrame frame,float now)
        {
            cameraInput.handActions=MixedInputs;
            cameraInput.jumpThreshold=jumpSensitivity;cameraInput.duckThreshold=duckDepth;cameraInput.shoulderRange=shoulderTurnRange;
            cameraInput.Receive(frame,now,BodyPerformer,SteeringPerformer,ActionPerformer);
        }
        public bool CameraTeamReady(float now)
        {
            bool hands=controller.IsMovementVisible(1,Movement.LeftHandHeight,now) && controller.IsMovementVisible(1,Movement.RightHandHeight,now);
            bool steering=MixedInputs ? cameraInput.ShouldersVisible(now) :
                controller.IsMovementVisible(SteeringPerformer,Movement.LeftHandHeight,now) && controller.IsMovementVisible(SteeringPerformer,Movement.RightHandHeight,now);
            return hands && steering && cameraInput.CanCalibrate(now,!MixedInputs && literalBodyActions,MixedInputs);
        }
        bool CalibrateCameraInput(float now) => CameraTeamReady(now) && cameraInput.Calibrate(now,!MixedInputs && literalBodyActions,MixedInputs);
        public void StartWithCamera()
        {
            if (CameraStartPending) return;
            if (!CameraMode) SelectCamera();
            if (!CameraConnected) ConnectCamera();
            Paused = false;
            CameraStartPending = true;
            RefreshTracking(Time.realtimeSinceStartup);
            AdvanceCameraStart(Time.realtimeSinceStartup);
        }
        public void CancelCameraStart()
        {
            CameraStartPending = false;
            if (controller && controller.CalibrationPending) controller.CancelCalibrationCountdown();
        }
        public void AdvanceCameraStart(float now)
        {
            if (!CameraStartPending) return;
            if (!CameraMode || !CameraConnected) { CancelCameraStart(); return; }
            // Wait for the necessary roles before starting the ten-second neutral
            // countdown. P3's optional hand channel never blocks a camera start.
            if (!controller.IsCalibrated)
            {
                if (!controller.CalibrationPending && CameraTeamReady(now)) controller.BeginCalibrationCountdown(now);
                return;
            }
            if (!TrackingBlocked) { CameraStartPending = false; StartRace(); }
        }
        public Rect CameraStartButtonRect { get; private set; }
        void DrawCameraStartButton(bool inGame)
        {
            if (GUILayout.Button(CameraStartLabel, button, GUILayout.Height(40)))
            {
                if (CameraStartPending) CancelCameraStart(); else StartWithCamera();
            }
            if (!inGame && Event.current.type == EventType.Repaint)
            {
                Rect rect = GUILayoutUtility.GetLastRect();
                CameraStartButtonRect = new Rect(GUIUtility.GUIToScreenPoint(rect.position), rect.size);
            }
            GUILayout.Label(CameraStartStatus, note);
        }
        public void SetCameraLayout(KartCameraLayout layout)
        {
            CancelCameraStart();
            cameraLayout=layout;controller.kartMixedInputs=MixedInputs;
            cameraInput.handActions=MixedInputs;
            controller.InvalidateCalibration();locomotion.StopMotion();throwGesture.Reset();
            player.SetCrouch(false);RefreshTracking(Time.realtimeSinceStartup);
        }
        public void SetLiteralActions(bool enabled)
        {
            CancelCameraStart();
            literalBodyActions=enabled;controller.InvalidateCalibration();player.SetCrouch(false);
            if(enabled && !MixedInputs && controller.upperBodyOnly)controller.SetUpperBodyOnly(false);
        }
        string CameraActionStatus(float now)
        {
            if(MixedInputs)return "P1: LEFT hand drives / RIGHT hand brakes\nP"+ActionPerformer+": "+cameraInput.HandFeedback(now);
            string body=literalBodyActions ? !cameraInput.BodyVisible(now) ? "P"+BodyPerformer+": step back until shoulders, hips and BOTH feet are visible" :
                !cameraInput.Calibrated ? "P"+BodyPerformer+": stand tall, feet planted for neutral" : cameraInput.IsDucking(now) ? "DUCKING / stand up to release" : "P"+BodyPerformer+": jump to hop / squat or duck to lower" :
                "P"+ActionPerformer+": raise RIGHT to jump / lean RIGHT to duck";
            return body+"\nP"+ActionPerformer+": hold LEFT to throw";
        }
        void DrawCameraOptions()
        {
            if(!MixedInputs)
            {
                bool literal=GUILayout.Toggle(literalBodyActions,"Literal jump / duck (feet in view)",toggle);
                if(literal!=literalBodyActions)SetLiteralActions(literal);
            }
            GUILayout.Label(MixedInputs ? "P1 BODY: LEFT up drives / RIGHT up brakes\nP"+SteeringPerformer+" SHOULDERS: "+ShoulderSteeringHelp+"\nP"+ActionPerformer+" HAND: "+(controller.performerCount==3?"one hand in P3 section; ":"right hand; ")+"thumbs up (other fingers curled) to jump; fist to crouch; pinch + release to throw" :
                "P1: LEFT up accelerates / RIGHT up brakes\nP"+SteeringPerformer+": LEFT up steers left / RIGHT up steers right\nP"+ActionPerformer+": LEFT held throws\n"+(literalBodyActions?"P"+BodyPerformer+": physically jump and duck":"P"+ActionPerformer+": raise RIGHT to jump / lean RIGHT to duck"),note);
            GUILayout.Label(!MixedInputs && literalBodyActions ? "Neutral: stand tall with both feet planted and hands lowered; relax both shoulders." :
                MixedInputs ? "Neutral: sit or stand upright, P1 hands lowered, P2 shoulders relaxed. P3: relax fingers between thumbs-up jumps." : "Neutral: sit upright, hands lowered, shoulders relaxed. Lower RIGHT between jumps.",note);
            if(controller.performerCount<3 && MixedInputs)GUILayout.Label(controller.performerCount==1 ? "Solo combines body, shoulders and hand roles on P1." : "Two people: P1 body; P2 shoulders + hand actions.",note);
            if(MixedInputs)
            {
                bool invert=GUILayout.Toggle(invertShoulderSteering,"Invert shoulder steering",toggle);
                if(invert!=invertShoulderSteering){invertShoulderSteering=invert;cameraInput.ResetActions();}
                if(CameraMode)GUILayout.Label(cameraInput.BridgeStatus,note);
            }
            showGestureTuning=GUILayout.Toggle(showGestureTuning,"Gesture sensitivity",toggle);
            if(showGestureTuning)
            {
                if(!MixedInputs){GUILayout.Label("Jump height / smaller = easier",note);jumpSensitivity=GUILayout.HorizontalSlider(jumpSensitivity,.06f,.24f);}
                if(!MixedInputs){GUILayout.Label("Duck depth / smaller = easier",note);duckDepth=GUILayout.HorizontalSlider(duckDepth,.20f,.65f);}
                if(MixedInputs){GUILayout.Label("Shoulder shrug range / smaller = stronger steering",note);shoulderTurnRange=GUILayout.HorizontalSlider(shoulderTurnRange,.15f,.5f);}
            }
        }
        bool showGestureTuning;
    }
}
