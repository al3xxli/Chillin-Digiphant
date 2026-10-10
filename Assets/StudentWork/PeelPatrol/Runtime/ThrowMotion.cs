using System;

namespace ChillinDigiphant.PeelPatrol
{
    // Hold a confidently tracked left hand to throw at one-second intervals.
    public sealed class ThrowMotion
    {
        float hold, cooldown;
        public bool IsFiring { get; private set; }
        public float Cooldown { get { return cooldown; } }
        public float Progress { get { return Math.Min(1,hold/.2f); } }
        public void Reset() { IsFiring=false;hold=cooldown=0; }
        public void ConfirmShot() { cooldown=1; }
        public bool Step(float height,bool valid,bool stationPriority,float dt)
        {
            if(!valid || stationPriority || dt<=0 || dt>.2f) { Reset();return false; }
            dt=Math.Min(dt,.1f); // Match the bounded gameplay clock; never catch up in bursts.
            cooldown=Math.Max(0,cooldown-dt);
            if(height<.25f) { hold=0;IsFiring=false;return false; }
            if(height<.6f) { hold=0;IsFiring=false;return false; }
            hold=Math.Min(.2f,hold+dt);IsFiring=hold>=.2f;
            if(!IsFiring || cooldown>.0001f) return false;
            return true;
        }
    }
}
