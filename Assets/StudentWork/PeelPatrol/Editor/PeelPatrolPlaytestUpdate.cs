using System;
using System.IO;
using System.Linq;
using DigiPhant;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace ChillinDigiphant.PeelPatrol.Editor
{
    [InitializeOnLoad]
    public static class PeelPatrolPlaytestUpdate
    {
        static double poll;
        static PeelPatrolPlaytestUpdate() { EditorApplication.update+=Update; }
        static void Update()
        {
            if(EditorApplication.timeSinceStartup<poll || EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
            poll=EditorApplication.timeSinceStartup+1;
            if(!File.Exists(".tools/update-peel-patrol")) return;
            File.Delete(".tools/update-peel-patrol");
            try { Apply(); }
            catch(Exception e) { File.WriteAllText("Logs/peel-update-error.txt",e.ToString());Debug.LogException(e); }
        }
        [MenuItem("DigiPhant/Peel Patrol/Apply playtest update")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play mode first.");
            var scene=SceneManager.GetActiveScene();
            if(scene.path!=PeelPatrolBuilder.ScenePath) throw new Exception("Open Peel-Patrol before updating.");
            string backup="Assets/StudentWork/Scenes/Before-Playtest-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity";
            if(!EditorSceneManager.SaveScene(scene,backup,true)) throw new Exception("Could not preserve the previous scene.");
            var g=Object.FindFirstObjectByType<PeelPatrolGame>();
            var starts=new[]{new Vector3(36,0,9.4f),new Vector3(2.5f,0,38),new Vector3(-20,0,-33)};
            var trail=Mat("Approach trail");var wood=Mat("Barricade wood");var yellow=Mat("Banana yellow");
            for(int i=0;i<g.lanes.Length;i++)
            {
                var lane=g.lanes[i];Vector3 shift=starts[i]-lane.path[0].position;
                foreach(var t in g.GetComponentsInChildren<Transform>(true).ToArray())
                {
                    if(t.name==lane.title+" entry post" || t.name==lane.title+" banana flag") t.position+=shift;
                    if(t.name==lane.title+" / approach trail") Object.DestroyImmediate(t.gameObject);
                }
                lane.path[0].position=starts[i];
                for(int j=0;j<lane.path.Length-1;j++)
                {
                    var a=lane.path[j].position;var b=lane.path[j+1].position;float distance=Vector3.Distance(a,b);
                    for(float d=1;d<distance;d+=2)
                        Shape(lane.title+" / approach trail",g.transform,PrimitiveType.Cylinder,Vector3.Lerp(a,b,d/distance)+Vector3.up*.015f,new Vector3(.23f,.012f,.23f),trail);
                }
            }
            g.controller.SetPerformerCount(3);
            g.controller.SetUpperBodyOnly(true);
            g.ConfigureHandControls();
            g.controller.SetInputMode(InputMode.Camera);g.controller.showControls=false;
            var preview=g.controller.GetComponent<DigiPhantCameraPreview>();
            if(preview) {preview.autoStartBridge=false;preview.showPreview=false;}
            g.locomotion.walkSpeed=2.8f;g.locomotion.runSpeed=4.8f;g.locomotion.backwardSpeed=1.8f;
            g.throwRange=20;
            BuildLoadout(g,wood,yellow);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            PeelPatrolBuilder.Validate();
            File.WriteAllText("Logs/peel-update.txt","PEEL_PLAYTEST_UPDATE_OK\n"+backup+"\nRemote spawns; wider spacing; coins and rides; visible carried oranges; MediaPipe default with three roles.");
            if(File.Exists("Logs/peel-update-error.txt")) File.Delete("Logs/peel-update-error.txt");
            Debug.Log("PEEL_PLAYTEST_UPDATE_OK");
        }
        static void BuildLoadout(PeelPatrolGame g,Material wood,Material yellow)
        {
            if(g.loadout) Object.DestroyImmediate(g.loadout.gameObject);
            var root=new GameObject("Elephant / basket and rides").transform;root.SetParent(g.locomotion.travelRoot,false);
            var loadout=root.gameObject.AddComponent<ElephantLoadout>();g.loadout=loadout;loadout.game=g;
            var skins=g.locomotion.elephantAnimator.GetComponentsInChildren<SkinnedMeshRenderer>();
            Bounds bounds=new Bounds(Vector3.zero,Vector3.zero);
            foreach(var skin in skins)
            {
                var b=skin.bounds;
                for(int i=0;i<8;i++) bounds.Encapsulate(root.InverseTransformPoint(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))));
            }
            float top=bounds.max.y,width=Mathf.Clamp(bounds.size.x*.75f,1.1f,2.8f);
            float z=-.35f,height=top*.88f;
            Shape("Saddle basket",root,PrimitiveType.Cube,new Vector3(0,height,z),new Vector3(width,.2f,1.25f),wood);
            for(int side=-1;side<=1;side+=2)
            {
                Shape("Basket side",root,PrimitiveType.Cube,new Vector3(side*width*.5f,height+.15f,z),new Vector3(.08f,.4f,1.25f),wood);
                Shape("Basket end",root,PrimitiveType.Cube,new Vector3(0,height+.15f,z+side*.6f),new Vector3(width,.4f,.08f),wood);
            }
            var orange=Mat("Juicy orange");var dark=Mat("Eyes black");var green=Mat("Orange leaves");
            loadout.oranges=new GameObject[10];
            for(int i=0;i<10;i++)
                loadout.oranges[i]=Shape("Carried orange "+(i+1),root,PrimitiveType.Sphere,new Vector3((i%5-2)*width/5,height+.4f,z+(i/5-.5f)*.55f),Vector3.one*(width/5*.95f),orange).gameObject;
            var board=new GameObject("Skateboard / speed +30%").transform;board.SetParent(root,false);
            Shape("Green deck",board,PrimitiveType.Cube,new Vector3(0,.22f,0),new Vector3(width*1.35f,.14f,3.4f),green);
            var wheels=new System.Collections.Generic.List<Transform>();
            for(int i=0;i<4;i++)
            {
                var wheel=Shape("Skate wheel",board,PrimitiveType.Cylinder,new Vector3((i%2-.5f)*width*1.4f,.17f,(i/2-.5f)*2.4f),new Vector3(.4f,.15f,.4f),yellow);
                wheel.localRotation=Quaternion.Euler(0,0,90);wheels.Add(wheel);
            }
            var uni=new GameObject("Unicycle / speed +65%").transform;uni.SetParent(root,false);
            var tire=Shape("One enormous wheel",uni,PrimitiveType.Cylinder,new Vector3(0,.65f,-.1f),new Vector3(1.3f,.3f,1.3f),dark);tire.localRotation=Quaternion.Euler(0,0,90);wheels.Add(tire);
            for(int side=-1;side<=1;side+=2)
            {
                var hub=Shape("Golden wheel hub",uni,PrimitiveType.Cylinder,new Vector3(side*.32f,.65f,-.1f),new Vector3(.3f,.025f,.3f),yellow);
                hub.localRotation=Quaternion.Euler(0,0,90);hub.SetParent(tire,true);
                for(int spoke=0;spoke<3;spoke++)
                {
                    var bar=Shape("Wheel spoke",uni,PrimitiveType.Cube,new Vector3(side*.31f,.65f,-.1f),new Vector3(.04f,1.08f,.07f),yellow);
                    bar.localRotation=Quaternion.Euler(spoke*60,0,0);bar.SetParent(tire,true);
                }
            }
            Shape("Unicycle seat post",uni,PrimitiveType.Cylinder,new Vector3(0,1.25f,-.1f),new Vector3(.15f,.65f,.15f),yellow);
            Shape("Unicycle saddle",uni,PrimitiveType.Cube,new Vector3(0,1.85f,-.1f),new Vector3(1.1f,.2f,.75f),green);
            for(int side=-1;side<=1;side+=2)
                Shape("Pedal",uni,PrimitiveType.Cube,new Vector3(side*.65f,.62f,-.1f),new Vector3(.6f,.14f,.4f),yellow);
            var jet=new GameObject("Jetpack / speed +105%").transform;jet.SetParent(root,false);
            var flames=new System.Collections.Generic.List<Transform>();
            for(int side=-1;side<=1;side+=2)
            {
                Shape("Rocket tank",jet,PrimitiveType.Capsule,new Vector3(side*width*.65f,top*.68f,-.7f),new Vector3(.62f,.9f,.62f),green);
                Shape("Rocket nozzle",jet,PrimitiveType.Cylinder,new Vector3(side*width*.65f,top*.68f-.9f,-.7f),new Vector3(.4f,.15f,.4f),dark);
                flames.Add(Shape("Orange exhaust",jet,PrimitiveType.Capsule,new Vector3(side*width*.65f,top*.68f-1.35f,-.7f),new Vector3(.22f,.4f,.22f),orange));
            }
            loadout.rides=new[]{board.gameObject,uni.gameObject,jet.gameObject};loadout.wheels=wheels.ToArray();loadout.flames=flames.ToArray();loadout.Display(6,0);
            File.WriteAllText("Logs/peel-loadout-bounds.txt",bounds.ToString());
        }
        static Material Mat(string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/StudentWork/PeelPatrol/Generated/"+name+".mat");
        static Transform Shape(string name,Transform parent,PrimitiveType type,Vector3 position,Vector3 scale,Material mat)
        {
            var o=GameObject.CreatePrimitive(type);o.name=name;o.transform.SetParent(parent,false);o.transform.localPosition=position;o.transform.localScale=scale;
            o.GetComponent<Renderer>().sharedMaterial=mat;Object.DestroyImmediate(o.GetComponent<Collider>());return o.transform;
        }
    }
}
