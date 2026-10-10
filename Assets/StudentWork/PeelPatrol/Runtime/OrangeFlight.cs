using UnityEngine;

namespace ChillinDigiphant.PeelPatrol
{
    public sealed class OrangeFlight : MonoBehaviour
    {
        public BananaRaider Target { get; private set; }
        Vector3 origin, destination;
        float elapsed, duration;
        public void Launch(Vector3 start, BananaRaider target)
        {
            Launch(start, target.AimPoint);
            Target = target;
        }
        public void Launch(Vector3 start, Vector3 end)
        {
            origin = start; Target = null; destination = end; elapsed = 0;
            duration = .28f + Vector3.Distance(start,destination) * .026f;
            transform.position = start;
        }
        public bool Step(float dt)
        {
            elapsed += dt;
            if (Target && Target.Alive) destination = Target.AimPoint;
            float t = Mathf.Clamp01(elapsed/duration);
            transform.position = Vector3.Lerp(origin,destination,t) + Vector3.up * (Mathf.Sin(t*Mathf.PI)*2.4f);
            transform.Rotate(new Vector3(210,100,80)*dt);
            return t >= 1;
        }
    }
}
