using UnityEngine;

namespace ChillinDigiphant.Kart
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed partial class KartVehicle : MonoBehaviour
    {
        public KartRace race;
        public bool isPlayer;
        public string racerName;
        public float maxSpeed = 14, acceleration = 8, brake = 16, steeringDegrees = 85;
        public float aiLane, aiPace = .8f;
        public Transform[] wheels;
        public Rigidbody Body { get; private set; }
        public int CompletedLaps { get; private set; }
        public int NextGate { get; private set; } = 1;
        public bool Finished { get; private set; }
        public float FinishTime { get; private set; }
        public int Ammo { get; private set; }
        public bool CanThrow => race && race.CanDrive && !Finished && Ammo > 0 && Time.time - shotAt >= 1 && StunRemaining <= 0 &&
            (!isPlayer || this != race.player || race.PlayerTrunk && race.PlayerTrunk.Tip && !race.PlayerTrunk.Busy && !Recovering);
        public const int Capacity = 3;
        public float Speed => Body ? Vector3.Dot(Body.linearVelocity, transform.forward) : 0;
        public float StunRemaining { get; private set; }
        public bool BananaHit { get; private set; }
        public float BoostRemaining { get; private set; }
        public int CollisionCount { get; private set; }
        public int NearestPoint { get; private set; }
        public bool Grounded { get; private set; }
        public int Recoveries { get; private set; }
        public int HitCount { get; private set; }
        public bool Recovering => Time.time < recoveryUntil;
        public bool Airborne => !Grounded && Time.time - lastSafeTime > .3f;
        public float Throttle => throttle;
        public float Steering => steer;
        public string LastContact { get; private set; } = "none";
        public float Progress => CompletedLaps * race.track.gates.Length + (NextGate == 0 ? race.track.gates.Length : NextGate) - 1 + SegmentProgress();
        float throttle, steer, requestedThrottle, requestedSteer, lastSafeTime, shotAt = -100, recoveryUntil, hitImmuneUntil, looseGripUntil, stalled, reverseUntil, passingLane, passUntil, bestAIProgress, aiProgressAt;
        int lastGate;
        Vector3 previous;
        public void SetInput(float forward, float turning)
        {
            requestedThrottle = Mathf.Abs(forward) < .035f ? 0 : Mathf.Clamp(forward, -1, 1);
            requestedSteer = Mathf.Abs(turning) < .035f ? 0 : Mathf.Clamp(turning, -1, 1);
        }
        void Awake() { Body = GetComponent<Rigidbody>(); previous = transform.position; InitializeActions(); }
        public void ResetRace(Vector3 position, Quaternion rotation)
        {
            if (!Body) Body = GetComponent<Rigidbody>();
            Body.position = position; Body.rotation = rotation;
            Body.linearVelocity = Body.angularVelocity = Vector3.zero;
            CompletedLaps = 0; NextGate = 1; lastGate = 0; Finished = false; FinishTime = 0;
            Ammo = 0; StunRemaining = BoostRemaining = 0; BananaHit = false; previous = position;
            lastSafeTime = Time.time; recoveryUntil = Time.time; hitImmuneUntil = Time.time + 1; shotAt = -100;
            stalled = reverseUntil = 0;
            bestAIProgress = 0; aiProgressAt = Time.time;
            throttle = steer = 0; looseGripUntil = passUntil = 0; passingLane = aiLane; Recoveries = HitCount = 0;
            GetComponent<KartFeedback>()?.ResetFeedback();
            ResetActions();
            FinishCollision(false);
            SetInput(0, 0);
        }
        float SegmentProgress()
        {
            int a = race.track.gates[lastGate], b = race.track.gates[NextGate];
            int span = (b - a + race.track.Count) % race.track.Count;
            int p = (NearestPoint - a + race.track.Count) % race.track.Count;
            return p > span ? 0 : (float)p / Mathf.Max(1, span);
        }
        void FixedUpdate()
        {
            if (!race || !Body) return;
            float dt = Time.fixedDeltaTime;
            NearestPoint = race.track.Nearest(Body.position);
            // An upright chassis can ride on its front axle on a ramp; probe both axles as well as the center.
            RaycastHit ground;
            Grounded = Physics.Raycast(Body.position + Vector3.up * .5f, Vector3.down, out ground, 1.6f, 1 << KartRace.RoadLayer, QueryTriggerInteraction.Ignore);
            if (!Grounded) Grounded = Physics.Raycast(Body.position + transform.forward * 1.8f + Vector3.up * .5f, Vector3.down, out ground, 1.6f, 1 << KartRace.RoadLayer, QueryTriggerInteraction.Ignore);
            if (!Grounded) Grounded = Physics.Raycast(Body.position - transform.forward * 1.8f + Vector3.up * .5f, Vector3.down, out ground, 1.6f, 1 << KartRace.RoadLayer, QueryTriggerInteraction.Ignore);
            if(Time.time-jumpedAt<.32f)Grounded=false;
            if (Grounded) lastSafeTime = Time.time;
            if (Body.position.y < race.track.Point(NearestPoint).y - 11 || Time.time - lastSafeTime > 2.6f) Recover();
            StunRemaining = Mathf.Max(0, StunRemaining - dt); BoostRemaining = Mathf.Max(0, BoostRemaining - dt);
            ApplyJumpRequest();
            if (!race.CanDrive || Recovering) { UpdateCrouch(dt); throttle = steer = 0; if (Grounded) Body.linearVelocity *= Mathf.Exp(-8 * dt); return; }
            if (Finished) { Body.linearVelocity *= Mathf.Exp(-2 * dt); return; }
            if (!isPlayer)
            {
                if (!race.IsRacing) { Body.linearVelocity *= Mathf.Exp(-8 * dt); return; }
                if (Progress > bestAIProgress + .1f) { bestAIProgress = Progress; aiProgressAt = Time.time; }
                else if (Time.time - aiProgressAt > 12)
                {
                    // A collision deadlock must not strand a rival for the rest of the race.
                    // Recovery returns to the already-passed gate and never grants progress.
                    Recover();
                    NearestPoint = race.track.Nearest(Body.position); bestAIProgress = Progress; aiProgressAt = Time.time;
                }
                float lookDistance = 8 + Mathf.Abs(Speed) * .5f;
                Vector3 target = race.track.LookAhead(Body.position, lookDistance);
                int targetIndex = race.track.Nearest(target);
                Vector3 trackSide = race.track.Right(NearestPoint);
                float bend = Vector3.Angle(race.track.Forward(NearestPoint), race.track.Forward(NearestPoint + 6));
                float currentLane = Vector3.Dot(Body.position - race.track.Point(NearestPoint), trackSide);
                float desiredLane = Time.time < passUntil ? passingLane : aiLane;
                float trafficSpeed = 1;
                foreach (var other in race.racers)
                {
                    if (other == this || !other.gameObject.activeInHierarchy) continue;
                    Vector3 offset = other.Body.position - Body.position;
                    float ahead = Vector3.Dot(offset, race.track.Forward(NearestPoint));
                    float lateral = Vector3.Dot(offset, trackSide);
                    if (ahead < 0 || ahead > 24 || Mathf.Abs(lateral) > 3.5f || other.Speed > Speed + 1) continue;
                    // Pick and hold a free passing lane; brake if traffic occupies both alternatives.
                    float best = float.MaxValue; bool found = false;
                    foreach (float lane in new[] { -5f, 0f, 5f })
                    {
                        if (Mathf.Abs(lane - (currentLane + lateral)) < 3.7f) continue;
                        bool clear = true;
                        foreach (var neighbor in race.racers)
                        {
                            if (neighbor == this || !neighbor.gameObject.activeInHierarchy) continue;
                            Vector3 relative = neighbor.Body.position - Body.position;
                            if (Mathf.Abs(Vector3.Dot(relative, race.track.Forward(NearestPoint))) < 12 && Mathf.Abs(Vector3.Dot(relative, trackSide) + currentLane - lane) < 3.5f) clear = false;
                        }
                        if (clear && Mathf.Abs(lane-currentLane) < best) { best = Mathf.Abs(lane-currentLane); desiredLane = lane; found = true; }
                    }
                    if (found) { passingLane = desiredLane; passUntil = Time.time + 2.5f; }
                    if (ahead < 10 && (!found || Mathf.Abs(lateral) < 2.5f)) trafficSpeed = Mathf.Min(trafficSpeed, Mathf.Clamp01((ahead-4)/8));
                }
                float drive = aiPace * Mathf.Lerp(1, .52f, Mathf.Clamp01(bend/55)) * trafficSpeed;
                ReadCourseForAI(ref desiredLane,ref drive);
                target += race.track.Right(targetIndex) * Mathf.Clamp(desiredLane, -race.track.roadWidth*.5f+3, race.track.roadWidth*.5f-3);
                Vector3 delta = target - Body.position; delta.y = 0;
                float angle = Vector3.SignedAngle(transform.forward, delta, Vector3.up);
                if (Mathf.Abs(angle)>65) drive = .25f;
                stalled = Grounded && Mathf.Abs(Speed) < .6f && StunRemaining <= 0 ? stalled + dt : 0;
                if (stalled > 2) { reverseUntil = Time.time + 1; stalled = 0; }
                if (Time.time < reverseUntil) drive = -.65f;
                float yaw = 2 * Mathf.Max(5, Mathf.Abs(Speed)) * Mathf.Sin(angle * Mathf.Deg2Rad) / Mathf.Max(4, delta.magnitude) * Mathf.Rad2Deg;
                SetInput(drive, Mathf.Clamp(yaw / TurnRate(), -1, 1));
                if (Ammo > 0 && Time.time - shotAt > 4.5f)
                    foreach (var other in race.racers)
                        if (other != this && Vector3.Distance(other.transform.position, transform.position) < 18 && Vector3.Dot(transform.forward, (other.transform.position - transform.position).normalized) > .85f) { Throw(); break; }
            }
            UpdateCrouch(dt);
            throttle = Mathf.MoveTowards(throttle, requestedThrottle, (requestedThrottle < throttle ? 5 : 3) * dt);
            steer = Mathf.MoveTowards(steer, requestedSteer, (Mathf.Sign(requestedSteer) != Mathf.Sign(steer) ? 5 : 7) * dt);
            float input = throttle;
            if (Grounded)
            {
                Vector3 side = transform.right * Vector3.Dot(Body.linearVelocity, transform.right);
                Body.AddForce(-side * (Time.time < looseGripUntil ? 1.2f : 6), ForceMode.Acceleration);
                float targetSpeed = input * (input < 0 ? 5 : maxSpeed + (BoostRemaining > 0 ? 7 : 0)) * (StunRemaining > 0 ? .38f : 1);
                float force = Mathf.Abs(input)<.01f ? -Speed*.55f : Mathf.Clamp((targetSpeed - Speed) * 2.2f, -brake, acceleration);
                Vector3 driveDirection = Vector3.ProjectOnPlane(transform.forward, ground.normal).normalized;
                Body.AddForce(driveDirection * force, ForceMode.Acceleration);
                // Arcade drivetrain holds against the downhill component while the wheels contact the road.
                Body.AddForce(-Vector3.ProjectOnPlane(Physics.gravity + Vector3.down * 14, ground.normal), ForceMode.Acceleration);
                float turn = steer * TurnRate() * Mathf.Clamp01(Mathf.Abs(Speed)/2) * (Speed < -.3f ? -1 : 1) * (StunRemaining>0 ? .8f : 1);
                Body.MoveRotation(Body.rotation * Quaternion.Euler(0, turn * dt, 0));
            }
            else if(isPlayer && Jumping && input>0 && StunRemaining<=0)
            {
                // Foot lifting can coincide with acceleration. Continue gentle
                // forward drive in a hop; releasing throttle never brakes in air.
                float targetSpeed=input*(maxSpeed+(BoostRemaining>0?7:0));
                float force=Mathf.Clamp((targetSpeed-Speed)*2.2f,0,acceleration*.6f);
                Body.AddForce(Vector3.ProjectOnPlane(transform.forward,Vector3.up).normalized*force,ForceMode.Acceleration);
            }
            Body.AddForce(Vector3.down * 14, ForceMode.Acceleration);
            if (race.IsRacing) CheckNavigation(previous, Body.position);
            previous = Body.position;
            if (wheels != null) foreach (var wheel in wheels) if (wheel) wheel.Rotate(Vector3.up, Speed * dt * 100, Space.Self);
        }
        float TurnRate() => steeringDegrees * Mathf.Lerp(1, .55f, Mathf.Clamp01(Mathf.Abs(Speed)/maxSpeed));
        void FinishCollision(bool ignore)
        {
            foreach(var other in race.racers) if(other!=this)
                foreach(var a in GetComponents<Collider>()) foreach(var b in other.GetComponents<Collider>())
                    if(a.enabled && b.enabled)Physics.IgnoreCollision(a,b,ignore);
        }
        public void CheckNavigation(Vector3 from, Vector3 to)
        {
            if (Finished || !race.track.CrossedGate(NextGate, from, to)) return;
            lastGate = NextGate;
            if (NextGate == 0)
            {
                CompletedLaps++;
                if (this == race.player) race.Audio?.CrossFinishLine();
                if (CompletedLaps >= race.totalLaps) { Finished = true; FinishTime = race.Elapsed; FinishCollision(true); race.OnFinish(this); }
                else if (this == race.player) race.AdvanceCourse(this);
            }
            NextGate = (NextGate + 1) % race.track.gates.Length;
        }
        public void Recover()
        {
            int point = race.track.gates[lastGate];
            float lane = isPlayer ? -4.5f : aiLane;
            // Rejoin behind nearby traffic instead of spawning inside another kart.
            Vector3 rejoin = race.track.Point(point) + Vector3.up * .25f + race.track.Forward(point) * 1.5f + race.track.Right(point) * lane;
            for (int attempt=0; attempt<4; attempt++)
            {
                bool occupied=false;
                foreach(var other in race.racers) if(other!=this && other.gameObject.activeInHierarchy && (other.Body.position-rejoin).sqrMagnitude<36) occupied=true;
                if(!occupied)break; rejoin-=race.track.Forward(point)*6;
            }
            Body.position = rejoin;
            Body.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(race.track.Forward(point), Vector3.up));
            Body.linearVelocity = Body.angularVelocity = Vector3.zero;
            previous = Body.position; lastSafeTime = Time.time; recoveryUntil = Time.time + .65f; hitImmuneUntil=Time.time+2;
            Recoveries++; throttle=steer=0; StunRemaining=BoostRemaining=0; aiProgressAt=Time.time;
            if(isPlayer)race.locomotion.followCamera.GetComponent<KartFollowCamera>().Snap();
            if (isPlayer) race.Say("Back on track / checkpoint recovered");
            if (this == race.player) race.Audio?.Play(KartSound.Recover, "CHECKPOINT RECOVERED");
        }
        public bool Collect(int amount = 1)
        {
            if (!race.CanDrive || Finished || Ammo >= Capacity || amount <= 0) return false;
            int added = Mathf.Min(amount, Capacity - Ammo);
            Ammo += added;
            if (this == race.player) race.Audio?.Play(KartSound.Pickup, "ORANGE +" + added);
            if (isPlayer) race.Say("ORANGE +" + added + " / STORED " + Ammo + " / " + Capacity + " / " + (race.CameraMode ? race.MixedInputs ? "pinch + release" : "hold LEFT" : "SPACE") + " to throw");
            return true;
        }
        public bool Throw()
        {
            if (!CanThrow) return false;
            bool trunkThrow = isPlayer && this == race.player;
            if (trunkThrow && (!race.PlayerTrunk || !race.PlayerTrunk.TryBegin())) return false;
            Ammo--; shotAt = Time.time;
            if (trunkThrow) race.Audio?.Play(KartSound.Windup, "TRUNK READY");
            if (!trunkThrow) race.Launch(this);
            return true;
        }
        public void RefundTrunkThrow() { Ammo = Mathf.Min(Capacity, Ammo + 1); shotAt = -100; }
        public bool Hit(Vector3 impulse, bool banana = false)
        {
            if (Time.time < hitImmuneUntil || Finished) return false;
            HitCount++; StunRemaining = 1.15f; BananaHit = banana; BoostRemaining=0; hitImmuneUntil=Time.time+1.7f;
            Vector3 velocity=Body.linearVelocity; velocity.x*=.45f; velocity.z*=.45f; Body.linearVelocity=velocity;
            Body.AddForce(Vector3.ProjectOnPlane(impulse,Vector3.up)*.45f, ForceMode.VelocityChange);
            GetComponent<KartFeedback>()?.Impact();
            if (this == race.player) race.Audio?.Play(banana ? KartSound.Slip : KartSound.Hit, banana ? "BANANA SLIP!" : "ORANGE HIT!");
            if (isPlayer) race.Say(banana ? "BANANA SLIP / dizzy for a moment!" : "ORANGE HIT / slowing down!");
            return true;
        }
        public void Boost() { bool fresh = BoostRemaining < .2f; BoostRemaining = 1.8f; if (isPlayer) race.Say("RAINBOW BOOST!"); if (fresh && this == race.player) race.Audio?.Play(KartSound.Boost, "BOOST!"); }
        void OnCollisionEnter(Collision collision)
        {
            if (collision.collider.gameObject.layer == KartRace.RoadLayer) return;
            CollisionCount++; LastContact = collision.collider.name;
            if (this == race.player && collision.relativeVelocity.magnitude > 1.5f) race.Audio?.Play(KartSound.Bump, "BUMP!");
            var obstacle=collision.collider.GetComponentInParent<KartObstacle>();
            if(obstacle)
            {
                ObstacleContacts++;GetComponent<KartFeedback>()?.Bump(.8f);
                if(isPlayer)race.Say(obstacle.Prompt);
            }
            if(collision.rigidbody && collision.rigidbody.GetComponent<KartVehicle>())
            {
                // The equal-mass rigidbody solver transfers momentum. Briefly relax lateral grip so
                // the receiving kart can actually be pushed instead of cancelling the contact impulse.
                looseGripUntil=Time.time+.6f;
                GetComponent<KartFeedback>()?.Bump(Mathf.Clamp01(collision.relativeVelocity.magnitude/10));
            }
        }
    }
}
