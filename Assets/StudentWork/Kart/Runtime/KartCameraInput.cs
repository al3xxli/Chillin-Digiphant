using System;
using DigiPhant;
using UnityEngine;

namespace ChillinDigiphant.Kart
{
    /// <summary>Camera-rate state machine. No dependency on frame rate or game time.</summary>
    public sealed class KartCameraInput
    {
        public bool Calibrated { get; private set; }
        public bool Duck { get; private set; }
        public float Steering { get; private set; }
        public float BodyRise { get; private set; }
        public float BodyDrop { get; private set; }
        public float Pinch { get; private set; }
        public bool Fist { get; private set; }
        public bool PinchAllowed { get; private set; }
        public bool ThumbsUp { get; private set; }
        public bool handActions = true;
        public string BridgeStatus { get; private set; } = "Waiting for camera";
        public string HandStatus { get; private set; } = "Waiting for hand tracking";
        const float HandGrace = .25f;
        const float ThumbsUpSampleGap = .75f;
        public float jumpThreshold = .10f, duckThreshold = .32f, shoulderRange = .28f;
        BodyMotionFrame body, neutral;
        float bodyAt=-100, shouldersAt=-100, handAt=-100, lastBody=-100, lastShoulders=-100;
        float shoulderNeutral, rawShoulders, previousRise, jumpAt=-100, throwAt=-100, closedAt=-100, groundAt=-100, thumbsUpAt=-100, thumbsUpReleasedAt=-100;
        bool jumpReady, pinchReady, pinched, thumbsUpReady;
        public bool BodyVisible(float now) => body != null && now >= bodyAt && now-bodyAt < .35f;
        public bool ShouldersVisible(float now) => now >= shouldersAt && now-shouldersAt < .25f;
        public bool HandVisible(float now) => now >= handAt && now-handAt < HandGrace;
        public bool BodyReady(float now) => Calibrated && neutral != null && BodyVisible(now);
        static bool Finite(float f) => !float.IsNaN(f) && !float.IsInfinity(f);
        static bool Unit(float f) => Finite(f) && f>=0 && f<=1;
        static bool Valid(BodyMotionFrame b) => b!=null && Unit(b.hipY) && Unit(b.shoulderY) && Unit(b.leftFootY) && Unit(b.rightFootY) &&
            Unit(b.confidence) && b.confidence>=.5f && Finite(b.scale) && b.scale>.035f && b.scale<.8f;
        public void Reset()
        {
            BridgeStatus="Waiting for camera";HandStatus="Waiting for hand tracking";Pinch=0;Fist=PinchAllowed=ThumbsUp=false;
            Calibrated=false; neutral=body=null; bodyAt=shouldersAt=handAt=lastBody=lastShoulders=-100;
            Steering=BodyRise=BodyDrop=0; ResetActions();
        }
        public void ResetActions()
        {
            Duck=jumpReady=pinchReady=pinched=thumbsUpReady=ThumbsUp=false;
            jumpAt=throwAt=closedAt=groundAt=thumbsUpAt=thumbsUpReleasedAt=-100;
        }
        public bool CanCalibrate(float now, bool requireBody, bool requireShoulders)
        {
            return !(requireBody && (!BodyVisible(now) || body.leftFootY-body.hipY<body.scale*.6f || body.rightFootY-body.hipY<body.scale*.6f)) &&
                !(requireShoulders && !ShouldersVisible(now));
        }
        public bool Calibrate(float now, bool requireBody, bool requireShoulders)
        {
            if (!CanCalibrate(now, requireBody, requireShoulders)) return false;
            neutral=BodyVisible(now)?body:null; shoulderNeutral=rawShoulders;
            Calibrated=true; Steering=previousRise=0; ResetActions();
            return true;
        }
        public void Receive(PoseFrame packet,float now,int bodySlot,int steeringSlot,int handSlot)
        {
            BridgeStatus=string.IsNullOrEmpty(packet.kartStatus)?"Pose bridge / shoulder and hand channels unavailable":packet.kartStatus;
            HandStatus=string.IsNullOrEmpty(packet.kartHandStatus)?"Show your RIGHT palm and shoulders":packet.kartHandStatus;
            BodyMotionFrame nextBody=null; ShoulderMotionFrame nextShoulders=null; HandMotionFrame nextHand=null;
            foreach(var p in packet.people)
            {
                if(p.slot==bodySlot)nextBody=p.body;
                if(p.slot==steeringSlot)nextShoulders=p.shoulders;
                if(p.slot==handSlot)nextHand=p.hand;
            }
            if(Valid(nextBody))
            {
                body=nextBody; bodyAt=now;
                if(!handActions && Calibrated && neutral!=null) ProcessBody(now);
                lastBody=now;
            }
            else
            {
                body=null;
                if(!handActions){jumpReady=false;groundAt=jumpAt=-100;Duck=false;}
            }
            if(nextShoulders!=null && Unit(nextShoulders.confidence) && nextShoulders.confidence>=.5f && Finite(nextShoulders.turn) && Mathf.Abs(nextShoulders.turn)<2)
            {
                rawShoulders=nextShoulders.turn;shouldersAt=now;
                float v=rawShoulders-shoulderNeutral;
                float target=Mathf.Sign(v)*Mathf.Clamp01((Mathf.Abs(v)-.045f)/Mathf.Max(.08f,shoulderRange-.045f));
                float dt=now-lastShoulders;
                Steering=!Calibrated?0:Mathf.Lerp(dt>.35f?0:Steering,target,1-Mathf.Exp(-Mathf.Min(dt,.1f)*12));
                lastShoulders=now;
            }
            else if(!ShouldersVisible(now)) { shouldersAt=-100;Steering=0; }
            if(nextHand!=null && Unit(nextHand.confidence) && nextHand.confidence>=.5f && Finite(nextHand.pinch) && nextHand.pinch>=0 && nextHand.pinch<10 && Unit(nextHand.fist))
            {
                bool gap=now-handAt>=HandGrace;
                bool thumbsUpGap=now-handAt>=ThumbsUpSampleGap;
                handAt=now;Pinch=nextHand.pinch;
                if(gap){pinchReady=pinched=Fist=false;closedAt=-100;if(handActions)jumpAt=-100;}
                // Camera inference can run below 4 FPS. Its next valid sample
                // must not erase the release that armed a thumbs-up jump.
                // A consumed gesture stays disarmed through any tracking gap.
                if(thumbsUpGap)thumbsUpAt=thumbsUpReleasedAt=-100;
                // Thumbs up also curls all four fingers: it must win over the
                // fist channel, including when transitioning out of crouch.
                ThumbsUp=nextHand.thumbsUp;
                Fist=!ThumbsUp && nextHand.fist>(Fist?.45f:.65f);
                PinchAllowed=nextHand.pinchAllowed && !ThumbsUp;
                if(handActions)
                {
                    Duck=Calibrated && Fist;
                    ProcessHandJump(now);
                }
                // Curling a fist can also bring thumb and index together. A fist
                // or thumbs up cancels the throw latch before release.
                if(Fist || !PinchAllowed){pinchReady=pinched=false;closedAt=throwAt=-100;}
                else if(Calibrated)
                {
                    if(Pinch>.65f)
                    {
                        if(pinched && now-closedAt>=.06f)throwAt=now;
                        pinched=false;pinchReady=true;closedAt=-100;
                    }
                    else if(Pinch<.30f && pinchReady){pinchReady=false;pinched=true;closedAt=now;}
                }
            }
            // A single dropped camera frame must not cancel a deliberate pinch.
            // Longer loss still requires a fresh open -> pinch -> release sequence.
            else
            {
                // Missing observations cannot count toward either dwell timer.
                thumbsUpAt=thumbsUpReleasedAt=-100;
                if (!HandVisible(now))
                {
                    handAt=-100;pinchReady=pinched=Fist=PinchAllowed=ThumbsUp=false;throwAt=-100;
                    if(handActions){Duck=false;jumpAt=-100;}
                }
            }
        }
        public string HandFeedback(float now)
        {
            if (!HandVisible(now)) return HandStatus;
            if (ThumbsUp) return thumbsUpReady ? "HAND: thumbs up / hold briefly to jump" : "HAND: thumbs up / open hand, then show thumbs up to jump";
            if (Fist) return "HAND: fist / crouch held / open to stand";
            if (!PinchAllowed) return "HAND: thumbs up to jump / fist to crouch / open fingers to throw";
            if (Pinch < .30f) return "HAND: pinched / open fingers to throw";
            if (Pinch > .65f) return "HAND: open / thumbs up to jump / pinch thumb + index to throw";
            return "HAND: detected / close thumb + index together";
        }
        void ProcessHandJump(float now)
        {
            if(!Calibrated){thumbsUpReady=false;thumbsUpAt=thumbsUpReleasedAt=-100;return;}
            if(!ThumbsUp)
            {
                thumbsUpAt=-100;
                if(thumbsUpReleasedAt<0)thumbsUpReleasedAt=now;
                if(now-thumbsUpReleasedAt>=.12f)thumbsUpReady=true;
                return;
            }
            thumbsUpReleasedAt=-100;
            if(!thumbsUpReady)return;
            if(thumbsUpAt<0)thumbsUpAt=now;
            else if(now-thumbsUpAt>=.08f){jumpAt=now;thumbsUpReady=false;thumbsUpAt=-100;}
        }
        void ProcessBody(float now)
        {
            float dt=now-lastBody, scale=neutral.scale;
            // A new/missing frame must establish neutral before another jump.
            if(dt>.35f || dt<=0){jumpReady=false;groundAt=-100;}
            BodyRise=(neutral.hipY-body.hipY)/scale;
            float shoulderDrop=(body.shoulderY-neutral.shoulderY)/scale;
            BodyDrop=Mathf.Max(-BodyRise,shoulderDrop);
            float left=(neutral.leftFootY-body.leftFootY)/scale,right=(neutral.rightFootY-body.rightFootY)/scale;
            float threshold=Duck?duckThreshold*.6f:duckThreshold;
            Duck=shoulderDrop>threshold && (-BodyRise>threshold*.45f || shoulderDrop>threshold*1.35f);
            bool settled=Mathf.Abs(left)<.065f && Mathf.Abs(right)<.065f && Mathf.Abs(BodyRise)<.12f && !Duck;
            if(settled)
            {
                if(groundAt<0)groundAt=now;
                if(now-groundAt>=.12f)jumpReady=true;
            }
            else groundAt=-100;
            float speed=dt>0 && dt<.35f?(BodyRise-previousRise)/dt:0;
            if(jumpReady && !Duck && left>jumpThreshold && right>jumpThreshold && BodyRise>jumpThreshold*.8f && speed>.25f)
            {jumpAt=now;jumpReady=false;}
            previousRise=BodyRise;
        }
        public bool TakeJump(float now){bool value=now>=jumpAt && now-jumpAt<.22f;jumpAt=-100;return value;}
        public bool TakeThrow(float now){bool value=now>=throwAt && now-throwAt<.22f;throwAt=-100;return value;}
        public bool IsDucking(float now) => Calibrated && Duck && (handActions ? HandVisible(now) : BodyReady(now));
    }
}
