using UnityEngine;
namespace ChillinDigiphant.Kart
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class KartProjectile : MonoBehaviour
    {
        public KartVehicle owner;
        float age;
        int bounces;
        bool consumed;
        void Update() { age += Time.deltaTime; if (age > 5) Destroy(gameObject); }
        void OnCollisionEnter(Collision collision)
        {
            if (consumed) return;
            var victim = collision.rigidbody ? collision.rigidbody.GetComponent<KartVehicle>() : null;
            if (victim && victim != owner)
            {
                consumed = true;
                bool hit = victim.Hit((victim.transform.position - transform.position).normalized * 4);
                if(hit && owner.isPlayer) owner.race.Say("DIRECT HIT / " + victim.racerName + " slowed!");
                Destroy(gameObject);
            }
            else if (++bounces > 3) Destroy(gameObject);
        }
    }
}
