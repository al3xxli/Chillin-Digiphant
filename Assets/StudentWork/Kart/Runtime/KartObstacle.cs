using UnityEngine;
namespace ChillinDigiphant.Kart
{
    public enum KartObstacleKind { Jump, Duck, Slalom, Banana }
    public sealed class KartObstacle : MonoBehaviour
    {
        public KartObstacleKind kind;
        public int trackSample;
        public float passingLane;
        public float Ahead(Vector3 position) => Vector3.Dot(transform.position-position,transform.forward);
        public bool InApproach(Vector3 position,float distance=30)
        {
            Vector3 local=transform.InverseTransformPoint(position);
            return isActiveAndEnabled && local.z<=4 && local.z>=-distance && Mathf.Abs(local.x)<13 && Mathf.Abs(local.y)<6;
        }
        public string Prompt => kind==KartObstacleKind.Banana ? "JUMP / BANANA PEELS" : kind==KartObstacleKind.Jump ? "JUMP / FALLEN LOG" : kind==KartObstacleKind.Duck ? "DUCK / LOW ACACIA CANOPY" : passingLane>0 ? "STEER RIGHT / KEEP TO THE OPEN LANE" : "STEER LEFT / KEEP TO THE OPEN LANE";
    }
}
