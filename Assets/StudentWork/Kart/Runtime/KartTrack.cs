using UnityEngine;

namespace ChillinDigiphant.Kart
{
    public sealed class KartTrack : MonoBehaviour
    {
        public Vector3[] points;
        public int[] gates;
        public float roadWidth = 8;
        [System.Serializable] public struct OpenEdge { public int first, last, side; }
        public OpenEdge[] openEdges = new OpenEdge[0];
        public bool IsOpenEdge(int index, int side)
        {
            index = (index % Count + Count) % Count;
            foreach (var edge in openEdges) if (edge.side == side && index >= edge.first && index <= edge.last) return true;
            return false;
        }
        public int Count => points.Length;
        public Vector3 Point(int i) => transform.TransformPoint(points[(i % Count + Count) % Count]);
        public Vector3 Forward(int i) => (Point(i + 1) - Point(i - 1)).normalized;
        public Vector3 Right(int i) => Vector3.Cross(Vector3.up, Forward(i)).normalized;
        public int Nearest(Vector3 position)
        {
            int best = 0; float distance = float.MaxValue;
            for (int i = 0; i < Count; i++)
            {
                float d = (Point(i) - position).sqrMagnitude;
                if (d < distance) { distance = d; best = i; }
            }
            return best;
        }
        public Vector3 Ahead(int index, float distance)
        {
            for (int n = 0; n < Count; n++)
            {
                Vector3 a = Point(index), b = Point(index + 1);
                float length = Vector3.Distance(a, b);
                if (distance <= length) return Vector3.Lerp(a, b, distance / Mathf.Max(.001f, length));
                distance -= length; index++;
            }
            return Point(index);
        }
        public Vector3 LookAhead(Vector3 position, float distance)
        {
            int segment = 0; float fraction = 0, best = float.MaxValue;
            for (int i = 0; i < Count; i++)
            {
                Vector3 a = Point(i), delta = Point(i + 1) - a;
                float t = Mathf.Clamp01(Vector3.Dot(position - a, delta) / Mathf.Max(.0001f, delta.sqrMagnitude));
                float d = (position - (a + delta * t)).sqrMagnitude;
                if (d < best) { best = d; segment = i; fraction = t; }
            }
            float remainder = Vector3.Distance(Point(segment), Point(segment + 1)) * (1 - fraction);
            if (distance < remainder) return Vector3.Lerp(Point(segment), Point(segment + 1), fraction + (1 - fraction) * distance / remainder);
            return Ahead(segment + 1, distance - remainder);
        }
        public bool CrossedGate(int gate, Vector3 from, Vector3 to)
        {
            int i = gates[gate]; Vector3 normal = Forward(i), center = Point(i);
            float a = Vector3.Dot(from - center, normal), b = Vector3.Dot(to - center, normal);
            if (a > 0 || b <= 0 || b - a < .0001f) return false;
            Vector3 crossing = Vector3.Lerp(from, to, -a / (b - a)) - center;
            return Mathf.Abs(Vector3.Dot(crossing, Right(i))) < roadWidth * .5f + .5f && Mathf.Abs(crossing.y) < 4;
        }
    }
}
