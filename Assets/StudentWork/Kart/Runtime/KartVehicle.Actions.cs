using UnityEngine;
namespace ChillinDigiphant.Kart
{
    public sealed partial class KartVehicle
    {
        public bool Crouching { get; private set; }
        public float CrouchAmount { get; private set; }
        public int JumpCount { get; private set; }
        public int ObstacleContacts { get; private set; }
        public bool Jumping => !Grounded && Time.time<jumpedAt+1.3f;
        bool crouchRequested;
        float jumpedAt=-100, jumpRequestedUntil=-100;
        CapsuleCollider standingCollider;
        BoxCollider duckCollider;
        readonly Collider[] ceilings=new Collider[16];
        void InitializeActions()
        {
            standingCollider=GetComponent<CapsuleCollider>();
            duckCollider=gameObject.AddComponent<BoxCollider>();duckCollider.size=new Vector3(2.5f,1.4f,4.2f);duckCollider.center=Vector3.up*.75f;
            duckCollider.sharedMaterial=standingCollider.sharedMaterial;duckCollider.enabled=false;
        }
        void ResetActions()
        {
            Crouching=crouchRequested=false;CrouchAmount=0;jumpedAt=jumpRequestedUntil=-100;JumpCount=ObstacleContacts=0;
            if(standingCollider)standingCollider.enabled=true;if(duckCollider)duckCollider.enabled=false;
        }
        public void SetCrouch(bool held) { crouchRequested=held; }
        public void RequestJump()
        {
            if(race.CanDrive)jumpRequestedUntil=Time.time+.20f;
        }
        void ApplyJumpRequest()
        {
            if(!race.CanDrive){jumpRequestedUntil=-100;return;}
            if(Time.time<=jumpRequestedUntil && Jump())jumpRequestedUntil=-100;
        }
        public bool Jump()
        {
            // A real jump starts with a dip. Apply the released crouch immediately
            // so the previous physics frame's low collider cannot swallow takeoff.
            // UpdateCrouch still keeps us low if an overhead branch blocks standing.
            if(Crouching && !crouchRequested)UpdateCrouch(0);
            if(!race.CanDrive || Finished || Recovering || !Grounded || Crouching || StunRemaining>0 || Time.time-jumpedAt<.8f)return false;
            Vector3 v=Body.linearVelocity;
            // Preserve the upward travel on an incline; otherwise the uphill log takes away
            // most of the hop's clearance before the rear axle reaches it.
            float uphill=Mathf.Max(0,race.track.Forward(NearestPoint).y*Mathf.Max(0,Speed));
            v.y=10.5f+Mathf.Max(uphill,v.y);Body.linearVelocity=v;jumpedAt=Time.time;Grounded=false;JumpCount++;
            if(isPlayer)race.Say("HOP! / clear the log");return true;
        }
        void UpdateCrouch(float dt)
        {
            bool blocked=false;
            if(Crouching && !crouchRequested)
            {
                int count=Physics.OverlapBoxNonAlloc(Body.position+Vector3.up*1.95f,new Vector3(1.25f,.65f,2.1f),ceilings,Body.rotation,~(1<<KartRace.RoadLayer),QueryTriggerInteraction.Ignore);
                for(int i=0;i<count;i++) if(ceilings[i].attachedRigidbody!=Body && ceilings[i].GetComponentInParent<KartObstacle>())blocked=true;
            }
            Crouching=(crouchRequested && race.CanDrive && !Finished)||blocked;
            CrouchAmount=Mathf.MoveTowards(CrouchAmount,Crouching?1:0,dt*8);
            if(standingCollider)standingCollider.enabled=!Crouching;
            if(duckCollider)duckCollider.enabled=Crouching;
        }
        void ReadCourseForAI(ref float lane,ref float drive)
        {
            SetCrouch(false);
            foreach(var obstacle in race.obstacles)
            {
                if(!obstacle || !obstacle.InApproach(Body.position,32))continue;
                float ahead=obstacle.Ahead(Body.position);
                if(obstacle.kind==KartObstacleKind.Duck && ahead<18)SetCrouch(true);
                else if((obstacle.kind==KartObstacleKind.Jump || obstacle.kind==KartObstacleKind.Banana) && ahead>0)
                {
                    if(ahead<22)drive=Mathf.Min(drive,.8f);
                    if(ahead<Mathf.Clamp(Mathf.Abs(Speed)*.44f,5.5f,11))Jump();
                }
                else if(obstacle.kind==KartObstacleKind.Slalom && ahead>-3)
                {
                    // Override passing only through a hazard. An 8m opening still fits two karts.
                    lane=obstacle.passingLane+(aiLane>=0?1.6f:-1.6f);
                    drive=Mathf.Min(drive,.65f);
                    break;
                }
            }
        }
    }
}
