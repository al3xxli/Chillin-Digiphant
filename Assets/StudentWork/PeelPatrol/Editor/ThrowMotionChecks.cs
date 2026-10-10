using System;
using ChillinDigiphant.Savanna;

namespace ChillinDigiphant.PeelPatrol.Editor
{
    public static class ThrowMotionChecks
    {
        static void Check(bool value,string message) { if(!value) throw new Exception(message); }
        static bool Step(ThrowMotion motion,float height,bool valid,bool priority,float dt)
        { bool fired=motion.Step(height,valid,priority,dt);if(fired)motion.ConfirmShot();return fired; }
        static int Hold(ThrowMotion motion,float height,int frames=20)
        { int shots=0;for(int i=0;i<frames;i++)if(Step(motion,height,true,false,.05f))shots++;return shots; }
        public static void Run()
        {
            var motion=new ThrowMotion();
            Check(Hold(motion,1)==1,"A deliberate held hand must work without a hidden release requirement.");
            foreach(float dt in new[]{.05f,1f/30,1f/60,.1f})
            {
                motion.Reset();Step(motion,0,true,false,dt);
                float time=0,last=-10;int shots=0;
                for(int i=0;i<(int)(4/dt);i++)
                {
                    time+=dt;
                    if(!Step(motion,1,true,false,dt))continue;
                    if(shots>0)Check(time-last>=.999f && time-last<=1+dt+.001f,"Held fire did not keep one-second spacing.");
                    last=time;shots++;
                }
                Check(shots==4 && motion.IsFiring,"Four seconds of raised hand must fire four oranges.");
                Step(motion,0,true,false,dt);
                Check(!motion.IsFiring,"Lowering did not stop automatic fire.");
                Check(Hold(motion,0)==0,"Lowered hand fired.");
            }
            motion.Reset();Step(motion,0,true,false,.05f);
            Check(Hold(motion,1,4)==1,"Initial deliberate raise did not fire.");
            Step(motion,0,true,false,.05f);
            Check(Hold(motion,1,4)==0,"Rapid lowering/raising bypassed the one-second interval.");
            motion.Reset();Step(motion,0,true,false,.05f);
            Check(!Step(motion,1,true,false,.05f),"One-frame spike fired.");
            Step(motion,0,true,false,.05f);
            Check(Hold(motion,.4f)==0,"Small neutral jitter fired.");
            Step(motion,1,false,false,.05f);
            Check(!Step(motion,1,true,false,.05f),"Recovery fired without a fresh stable hold.");
            Check(Hold(motion,1)==1,"Stable tracking recovery did not resume held fire.");
            Step(motion,0,true,false,.05f);Step(motion,1,true,true,.05f);
            Check(!Step(motion,1,true,false,.05f),"Station priority ended with an instant shot.");
            Check(Hold(motion,1)==1,"Held throw did not resume after station priority.");
            Step(motion,0,true,false,.05f);Step(motion,1,true,false,.4f);
            Check(!Step(motion,1,true,false,.05f),"Frame gap caused an instant shot.");
            Check(Hold(motion,1)==1,"Stable hold after frame gap did not resume.");
            motion.Reset();
            for(int i=0;i<4;i++)motion.Step(1,true,false,.05f);
            Check(motion.Step(1,true,false,.05f),"Rejected shot incorrectly started the repeat cooldown.");
            motion.ConfirmShot();
            Check(!motion.Step(1,true,false,.05f),"Confirmed shot did not start cooldown.");
            var station=new InteractionHold();
            station.Step(0,true,0,.05f,.35f);
            int uses=0;for(int i=0;i<30;i++)if(station.Step(0,true,1,.05f,.35f))uses++;
            Check(uses==1,"Right raise must use station once.");
            for(int i=0;i<30;i++)Check(!station.Step(1,true,1,.05f,.35f),"Entering a new zone with hand up used it.");
            station.Step(1,true,0,.05f,.35f);
            uses=0;for(int i=0;i<30;i++)if(station.Step(1,true,1,.05f,.35f))uses++;
            Check(uses==1,"Lowering inside a station did not rearm.");
        }
    }
}
