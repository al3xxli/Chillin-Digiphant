using UnityEngine;

namespace ChillinDigiphant.Kart
{
    public sealed class KartPickup : MonoBehaviour
    {
        public Transform visual;
        public float respawnSeconds = 5;
        float availableAt;
        Collider trigger;
        Vector3 rest;
        public bool Available => Time.time >= availableAt;
        void Awake() { trigger = GetComponent<Collider>(); rest = visual.localPosition; }
        void Update()
        {
            bool available = Time.time >= availableAt;
            trigger.enabled = available; visual.gameObject.SetActive(available);
            if (available) { visual.Rotate(0, 75 * Time.deltaTime, 0); visual.localPosition = rest + Vector3.up * (Mathf.Sin(Time.time * 2.5f) * .18f); }
        }
        void OnTriggerEnter(Collider other) => TryCollect(other);
        void OnTriggerStay(Collider other) => TryCollect(other);
        void TryCollect(Collider other)
        {
            if (Time.time < availableAt) return;
            var kart = other.attachedRigidbody ? other.attachedRigidbody.GetComponent<KartVehicle>() : null;
            if (!kart || !kart.race.CanDrive || kart.Finished) return;
            // The world box is consumed on contact, independently of free inventory slots.
            // Disable immediately so two karts in the same physics step cannot claim it twice.
            availableAt = Time.time + respawnSeconds; trigger.enabled = false; visual.gameObject.SetActive(false);
            bool awarded = kart.Collect();
            if (!awarded && kart.isPlayer) kart.race.Say("ITEM BOX CLAIMED / basket already full");
            if (!awarded && kart==kart.race.player) kart.race.Audio?.Play(KartSound.Full,"BASKET FULL");
        }
        public void ResetPickup() { availableAt = 0; if(trigger)trigger.enabled=true; if(visual)visual.gameObject.SetActive(true); }
    }
}
