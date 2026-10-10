using UnityEngine;

namespace ChillinDigiphant.Kart
{
    // Additive animation above the existing rig leaves every MediaPipe bone intact.
    [DefaultExecutionOrder(250)]
    public sealed class KartFeedback : MonoBehaviour
    {
        public Transform visualRoot;
        public Transform driverRoot;
        public bool HitVisualActive => hitTime > 0;
        public float ImpactPulse => Mathf.Clamp01(hitTime/.6f);
        KartVehicle kart;
        Vector3 rest;
        Quaternion restRotation;
        Vector3 driverScale;
        Quaternion driverRotation;
        Transform[] stars;
        float hitTime, bump, bank;
        bool initialized;
        static Material starMaterial;
        void Awake()
        {
            kart=GetComponent<KartVehicle>();
            initialized=true;
            if(visualRoot) { rest=visualRoot.localPosition; restRotation=visualRoot.localRotation; }
            if(driverRoot){driverScale=driverRoot.localScale;driverRotation=driverRoot.localRotation;}
            if(!starMaterial)
            {
                starMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit")) { name="Hit stars / runtime", hideFlags=HideFlags.DontSave };
                starMaterial.SetColor("_BaseColor",new Color(1,.8f,.15f));starMaterial.EnableKeyword("_EMISSION");starMaterial.SetColor("_EmissionColor",new Color(1,.65f,.1f)*1.5f);
            }
            stars=new Transform[5];
            for(int i=0;i<stars.Length;i++)
            {
                var star=GameObject.CreatePrimitive(PrimitiveType.Cube);star.name="Orange hit / orbiting star";
                Destroy(star.GetComponent<Collider>());star.transform.SetParent(transform,false);star.transform.localScale=new Vector3(.34f,.34f,.13f);
                star.GetComponent<Renderer>().sharedMaterial=starMaterial;stars[i]=star.transform;star.SetActive(false);
            }
        }
        public void Impact()
        {
            hitTime=1.15f;
            KartImpactBurst.Spawn(transform.position+Vector3.up*1.8f,kart.race.orangeMaterial);
        }
        public void Bump(float strength) { bump=Mathf.Max(bump,strength); }
        public void ResetFeedback() { hitTime=bump=bank=0; }
        void LateUpdate()
        {
            if(!kart)return;
            hitTime=Mathf.Max(0,hitTime-Time.deltaTime);bump=Mathf.Max(0,bump-Time.deltaTime*3);
            bank=Mathf.Lerp(bank,-kart.Steering*Mathf.Clamp01(Mathf.Abs(kart.Speed)/15)*4,1-Mathf.Exp(-9*Time.deltaTime));
            float wobble=hitTime>0?Mathf.Sin((1.15f-hitTime)*32)*12*(hitTime/1.15f):0;
            if(visualRoot)
            {
                visualRoot.localRotation=restRotation*Quaternion.Euler(bump*Mathf.Sin(Time.time*35)*3,wobble,bank+wobble*.45f);
                visualRoot.localPosition=rest+Vector3.up*(hitTime>0?Mathf.Abs(Mathf.Sin((1.15f-hitTime)*22))*.16f:0);
            }
            if(driverRoot)
            {
                driverRoot.localScale=Vector3.Scale(driverScale,new Vector3(1,Mathf.Lerp(1,.25f,kart.CrouchAmount),1));
                driverRoot.localRotation=driverRotation*Quaternion.Euler(kart.CrouchAmount*8,0,0);
            }
            for(int i=0;i<stars.Length;i++)
            {
                stars[i].gameObject.SetActive(hitTime>0);
                if(hitTime<=0)continue;
                float a=Time.time*7+i*Mathf.PI*2/stars.Length;
                stars[i].localPosition=new Vector3(Mathf.Cos(a)*1.65f,3.9f+Mathf.Sin(a*2)*.18f,Mathf.Sin(a)*1.65f);
                stars[i].localRotation=Quaternion.Euler(0,Time.time*180,45);
            }
        }
        void OnDisable()
        {
            if(!initialized)return;
            if(visualRoot){visualRoot.localPosition=rest;visualRoot.localRotation=restRotation;}
            if(driverRoot){driverRoot.localScale=driverScale;driverRoot.localRotation=driverRotation;}
            if(stars!=null)foreach(var star in stars)if(star)star.gameObject.SetActive(false);
        }
    }
}
