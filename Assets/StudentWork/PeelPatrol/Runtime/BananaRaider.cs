using UnityEngine;

namespace ChillinDigiphant.PeelPatrol
{
    public sealed class BananaRaider : MonoBehaviour
    {
        public Transform body, leftFoot, rightFoot;
        public Renderer bodyRenderer;
        public int Health { get; private set; }
        public int Lane { get; private set; }
        public bool Alive => Health > 0;
        public Vector3 AimPoint => bodyRenderer ? bodyRenderer.bounds.center : transform.position+Vector3.up;
        public float RemainingDistance
        {
            get
            {
                float distance = Vector3.Distance(transform.position, route[waypoint]);
                for (int i = waypoint; i < route.Length - 1; i++) distance += Vector3.Distance(route[i], route[i + 1]);
                return distance;
            }
        }
        Vector3[] route;
        int waypoint;
        float speed, stride, hitFlash;
        Vector3 bodyRest;
        MaterialPropertyBlock tint;
        Color color;

        public void Initialise(Vector3[] points, int lane, int wave)
        {
            route = points; Lane = lane; Health = wave >= 3 ? 2 : 1;
            speed = .7f + wave * .12f; waypoint = 1; transform.position = route[0];
            bodyRest = body.localPosition; color = Health > 1 ? new Color(.67f,.82f,.24f) : new Color(1,.78f,.13f);
            tint = new MaterialPropertyBlock(); ApplyColor(color);
            transform.localScale *= 1.7f * (Health > 1 ? 1.2f : 1f);
        }
        public bool Hit()
        {
            if (!Alive) return false;
            Health--; hitFlash = .15f; ApplyColor(Color.white);
            return Health == 0;
        }
        // True means the raider reached the water. Barricades block it at waypoint one.
        public bool Step(float dt, bool blocked)
        {
            if (!Alive) return false;
            stride += dt * (blocked ? 3 : 7);
            body.localPosition = bodyRest + Vector3.up * Mathf.Abs(Mathf.Sin(stride)) * .1f;
            body.localRotation = Quaternion.Euler(0,0,Mathf.Sin(stride) * 7);
            leftFoot.localRotation = Quaternion.Euler(Mathf.Sin(stride)*28,0,0);
            rightFoot.localRotation = Quaternion.Euler(-Mathf.Sin(stride)*28,0,0);
            if (hitFlash > 0) { hitFlash -= dt; if (hitFlash <= 0) ApplyColor(color); }
            if (blocked) return false;
            Vector3 delta = route[waypoint] - transform.position; delta.y = 0;
            if (delta.sqrMagnitude > .001f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(delta), dt * 7);
            transform.position = Vector3.MoveTowards(transform.position,route[waypoint],speed * dt);
            if (Vector3.Distance(transform.position,route[waypoint]) < .1f)
            { if (waypoint == route.Length-1) return true; waypoint++; }
            return false;
        }
        public bool AtBarricade(Vector3 position) => waypoint <= 1 && Vector3.Distance(transform.position,position) < 1.1f;
        void ApplyColor(Color value) { tint.SetColor("_BaseColor",value); bodyRenderer.SetPropertyBlock(tint); }
    }
}
