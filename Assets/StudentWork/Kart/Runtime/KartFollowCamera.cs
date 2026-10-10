using UnityEngine;
namespace ChillinDigiphant.Kart
{
    [DefaultExecutionOrder(300)]
    public sealed class KartFollowCamera : MonoBehaviour
    {
        public KartVehicle target;
        public Vector3 offset = new Vector3(0, 7, -12);
        Vector3 velocity;
        bool snapped;
        Quaternion heading;
        public void Snap() { snapped = false; }
        void LateUpdate()
        {
            if (!target) return;
            Vector3 focus = target.transform.position + Vector3.up * 2;
            Quaternion yaw = Quaternion.Euler(0, target.transform.eulerAngles.y, 0);
            heading = snapped ? Quaternion.Slerp(heading,yaw,1-Mathf.Exp(-10*Time.deltaTime)) : yaw;
            Vector3 desired = target.transform.position + heading * offset;
            if (Physics.Linecast(focus, desired, out var hit, 1 << KartRace.RoadLayer, QueryTriggerInteraction.Ignore)) desired = hit.point + hit.normal * .6f;
            transform.position = snapped ? Vector3.SmoothDamp(transform.position, desired, ref velocity, .16f, Mathf.Infinity, Time.unscaledDeltaTime) : desired;
            Quaternion aim = Quaternion.LookRotation(focus + heading * Vector3.forward * 3 - transform.position);
            transform.rotation = snapped ? Quaternion.Slerp(transform.rotation,aim,1-Mathf.Exp(-12*Time.deltaTime)) : aim;
            var camera = GetComponent<Camera>();
            if (camera) camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, target.BoostRemaining > 0 ? 72 : Mathf.Lerp(60,66,Mathf.Clamp01(Mathf.Abs(target.Speed)/target.maxSpeed)), 1-Mathf.Exp(-3*Time.deltaTime));
            snapped = true;
        }
    }
}
