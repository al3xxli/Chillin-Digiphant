using UnityEngine;
namespace ChillinDigiphant.Kart
{
    public sealed class KartImpactBurst : MonoBehaviour
    {
        readonly Transform[] pieces=new Transform[12];
        readonly Vector3[] velocity=new Vector3[12];
        float age;
        public static void Spawn(Vector3 position,Material material)
        {
            var go=new GameObject("Orange impact / juice burst");go.transform.position=position;
            var burst=go.AddComponent<KartImpactBurst>();
            for(int i=0;i<burst.pieces.Length;i++)
            {
                var piece=GameObject.CreatePrimitive(PrimitiveType.Sphere);Destroy(piece.GetComponent<Collider>());piece.transform.SetParent(go.transform,false);
                piece.transform.localScale=Vector3.one*(i%2==0?.3f:.18f);piece.GetComponent<Renderer>().sharedMaterial=material;
                float a=i*Mathf.PI*2/burst.pieces.Length;
                burst.pieces[i]=piece.transform;burst.velocity[i]=new Vector3(Mathf.Sin(a)*5,2+i%3,Mathf.Cos(a)*5);
            }
        }
        void Update()
        {
            age+=Time.deltaTime;
            for(int i=0;i<pieces.Length;i++)
            {
                velocity[i]+=Vector3.down*8*Time.deltaTime;pieces[i].localPosition+=velocity[i]*Time.deltaTime;
                pieces[i].localScale=Vector3.one*(i%2==0?.3f:.18f)*Mathf.Clamp01(1-age/.6f);
            }
            if(age>.6f)Destroy(gameObject);
        }
    }
}
