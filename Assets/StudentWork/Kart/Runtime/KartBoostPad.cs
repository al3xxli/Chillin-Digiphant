using UnityEngine;
namespace ChillinDigiphant.Kart
{
    public sealed class KartBoostPad : MonoBehaviour
    {
        void OnTriggerEnter(Collider other)
        {
            var kart = other.attachedRigidbody ? other.attachedRigidbody.GetComponent<KartVehicle>() : null;
            if (kart && kart.race.CanDrive) kart.Boost();
        }
    }
}
