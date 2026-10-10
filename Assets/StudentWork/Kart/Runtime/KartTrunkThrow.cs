using System.Linq;
using UnityEngine;

namespace ChillinDigiphant.Kart
{
    // Runs after the imported rig and kart feedback, before the rear follow camera.
    [DefaultExecutionOrder(275)]
    public sealed class KartTrunkThrow : MonoBehaviour
    {
        public const float ReleaseAt = .62f;
        public const float Duration = .96f;
        public bool Busy { get; private set; }
        public bool HoldingOrange => Busy && !released;
        public float AnimationTime { get; private set; }
        public Transform Tip { get; private set; }
        public Vector3 GripPosition => Tip ? Tip.position + (Tip.position - bones[bones.Length - 1].position).normalized * .23f : transform.position;
        public Vector3 LastReleasePosition { get; private set; }
        public int ReleaseCount { get; private set; }
        public KartProjectile LastProjectile { get; private set; }
        KartVehicle owner;
        Transform rig;
        Transform[] bones;
        Vector3[] points;
        float[] lengths;
        GameObject heldOrange;
        bool released;

        public bool Initialize(KartVehicle kart)
        {
            owner = kart;
            var control = kart.race.controller.controls.FirstOrDefault(c => c.label == "Trunk curl");
            if (control?.bones == null || control.bones.Length < 2 || control.bones.Any(b => !b)) return false;
            bones = control.bones;
            Tip = bones[bones.Length - 1].GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "elephant_Trunk7-nub_bone");
            if (!Tip) return false;
            // DigiPhant Controls is a stationary scene object; animate in the moving kart's frame.
            rig = kart.transform;
            points = new Vector3[bones.Length + 1]; lengths = new float[bones.Length];
            // The raised trunk can leave the imported mesh's resting bounds.
            foreach (var skin in kart.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var bounds = skin.localBounds; bounds.Expand(3); skin.localBounds = bounds;
            }
            heldOrange = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            heldOrange.name = "Orange held by trunk";
            heldOrange.transform.SetParent(transform, false);
            heldOrange.transform.localScale = Vector3.one * .65f;
            var collider = heldOrange.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
            heldOrange.GetComponent<Renderer>().sharedMaterial = kart.race.orangeMaterial;
            heldOrange.SetActive(false);
            return true;
        }
        public bool TryBegin()
        {
            if (Busy || !Tip || !owner || owner.Recovering) return false;
            Busy = true; released = false; AnimationTime = 0;
            heldOrange.transform.position = GripPosition;
            heldOrange.SetActive(true);
            return true;
        }
        public void Cancel(bool refund = true)
        {
            if (HoldingOrange && refund && owner) owner.RefundTrunkThrow();
            Busy = false; AnimationTime = 0;
            if (heldOrange) heldOrange.SetActive(false);
        }
        void LateUpdate()
        {
            if (!Busy) return;
            if (!owner || !owner.isPlayer || owner.Finished || owner.Recovering || owner.StunRemaining > 0)
            { Cancel(); return; }
            if (owner.race.CanDrive) AnimationTime += Time.deltaTime;
            // Evaluate the exact release pose even if a slow frame skips across the release instant.
            float poseTime = !released && AnimationTime >= ReleaseAt ? ReleaseAt : AnimationTime;
            Pose(poseTime);
            if (!released)
            {
                heldOrange.transform.position = GripPosition;
                if (AnimationTime >= ReleaseAt && owner.race.CanDrive)
                {
                    LastReleasePosition = GripPosition;
                    LastProjectile = owner.race.LaunchFromTrunk(owner, LastReleasePosition);
                    ReleaseCount++; released = true; heldOrange.SetActive(false);
                }
            }
            if (AnimationTime >= Duration) { Busy = false; heldOrange.SetActive(false); }
        }
        void Pose(float time)
        {
            for (int i = 0; i < bones.Length; i++) points[i] = rig.InverseTransformPoint(bones[i].position);
            points[bones.Length] = rig.InverseTransformPoint(Tip.position);
            float length = 0;
            for (int i = 0; i < bones.Length; i++) { lengths[i] = Vector3.Distance(points[i], points[i + 1]); length += lengths[i]; }
            Vector3 root = points[0];
            float flick = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.46f, ReleaseAt, time));
            Vector3 lift = root + new Vector3(.95f, 1.57f, -.23f);
            Vector3 release = root + new Vector3(.65f, 1.07f, 1.17f);
            Vector3 target = Vector3.Lerp(lift, release, flick);
            Vector3 endDirection = Vector3.Lerp(new Vector3(-.15f, .3f, -.25f), new Vector3(-.05f, -.25f, .8f), flick).normalized;
            target = root + Vector3.ClampMagnitude(target - root, length * .97f);
            Vector3 handle = root + new Vector3(1.15f, .4f, .25f);
            Vector3 endHandle = target - endDirection * .55f;
            // Seed a smooth sideward curve, then solve using the real rig segment lengths.
            for (int i = 1; i < points.Length; i++)
            {
                float t = (float)i / bones.Length, u = 1 - t;
                points[i] = u * u * u * root + 3 * u * u * t * handle + 3 * u * t * t * endHandle + t * t * t * target;
            }
            for (int iteration = 0; iteration < 16; iteration++)
            {
                points[bones.Length] = target;
                for (int i = bones.Length - 1; i >= 0; i--) points[i] = points[i + 1] + (points[i] - points[i + 1]).normalized * lengths[i];
                points[0] = root;
                for (int i = 0; i < bones.Length; i++) points[i + 1] = points[i] + (points[i + 1] - points[i]).normalized * lengths[i];
            }
            float weight = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0, .38f, time)) * (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.70f, Duration, time)));
            for (int i = 0; i < bones.Length; i++)
            {
                Transform next = i + 1 < bones.Length ? bones[i + 1] : Tip;
                Vector3 current = bones[i].parent.InverseTransformVector(next.position - bones[i].position);
                Vector3 desired = bones[i].parent.InverseTransformVector(rig.TransformVector(points[i + 1] - points[i]));
                Quaternion rotation = Quaternion.FromToRotation(current, desired) * bones[i].localRotation;
                bones[i].localRotation = Quaternion.Slerp(bones[i].localRotation, rotation, weight);
            }
        }
        void OnDisable() { Cancel(); }
    }
}
