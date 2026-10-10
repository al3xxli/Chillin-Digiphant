using System.Collections.Generic;
using UnityEngine;

namespace ChillinDigiphant.Kart
{
    public sealed class KartBananaPeel : MonoBehaviour
    {
        public GameObject visual;
        public bool Available => Time.time >= availableAt;
        float availableAt;
        Collider trigger;
        void Awake() { trigger = GetComponent<Collider>(); }
        void Update()
        {
            trigger.enabled = Available;
            if (visual) visual.SetActive(Available);
        }
        void OnTriggerEnter(Collider other) => Touch(other);
        void OnTriggerStay(Collider other) => Touch(other);
        void Touch(Collider other)
        {
            var kart = other.attachedRigidbody ? other.attachedRigidbody.GetComponent<KartVehicle>() : null;
            if (!Available || !kart || !kart.race.CanDrive || kart.Finished || kart.Recovering) return;
            if (!kart.Hit(Vector3.zero, true)) return;
            availableAt = Time.time + 5;
            trigger.enabled = false;
            if (visual) visual.SetActive(false);
        }
        public void ResetPeel()
        {
            availableAt = 0;
            if (trigger) trigger.enabled = true;
            if (visual) visual.SetActive(true);
        }

        public static Mesh CreateMesh()
        {
            // Four curved, tapered flaps meeting at an upright center: an open peel.
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int flap = 0; flap < 4; flap++)
            {
                Quaternion turn = Quaternion.Euler(0, flap * 90 + 20, 0);
                int first = vertices.Count;
                for (int step = 0; step <= 5; step++)
                {
                    float t = step / 5f;
                    float width = Mathf.Sin(t * Mathf.PI) * .32f + .035f;
                    float y = Mathf.Lerp(.62f, .07f, Mathf.Sin(t * Mathf.PI * .5f)) + t * t * t * .12f;
                    vertices.Add(turn * new Vector3(-width, y, .06f + t * 1.05f));
                    vertices.Add(turn * new Vector3(width, y, .06f + t * 1.05f));
                    if (step == 0) continue;
                    int a = first + (step - 1) * 2;
                    triangles.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 });
                }
            }
            // Separate reverse-side vertices keep normals from cancelling each other.
            int frontCount = vertices.Count, indexCount = triangles.Count;
            vertices.AddRange(vertices.ToArray());
            for (int i = 0; i < indexCount; i += 3)
                triangles.AddRange(new[] { triangles[i + 2] + frontCount, triangles[i + 1] + frontCount, triangles[i] + frontCount });
            var mesh = new Mesh { name = "Open banana peel / runtime" };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }
    }
}
