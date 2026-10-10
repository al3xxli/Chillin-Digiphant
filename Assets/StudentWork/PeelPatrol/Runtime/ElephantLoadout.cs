using UnityEngine;

namespace ChillinDigiphant.PeelPatrol
{
    public sealed class ElephantLoadout : MonoBehaviour
    {
        public GameObject[] oranges;
        public GameObject[] rides;
        public Transform[] wheels, flames;
        public PeelPatrolGame game;
        public int VisibleOranges { get; private set; }
        public void Display(int ammo,int upgrade)
        {
            VisibleOranges=Mathf.Clamp(ammo,0,oranges.Length);
            for(int i=0;i<oranges.Length;i++) oranges[i].SetActive(i<VisibleOranges);
            for(int i=0;i<rides.Length;i++) rides[i].SetActive(upgrade==i+1);
        }
        void LateUpdate()
        {
            if(!game || !game.Round.Active || game.SimulationPaused) return;
            foreach(var wheel in wheels) if(wheel.gameObject.activeInHierarchy)
                wheel.Rotate(Vector3.up,game.locomotion.CurrentSpeed*Time.deltaTime*90,Space.Self);
            foreach(var flame in flames)
                flame.localScale=new Vector3(.22f,.4f+Mathf.Abs(game.locomotion.CurrentSpeed)*.08f+Mathf.Sin(Time.time*17)*.05f,.22f);
        }
    }
}
