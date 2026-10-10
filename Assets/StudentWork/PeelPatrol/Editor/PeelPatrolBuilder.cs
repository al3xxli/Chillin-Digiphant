using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ChillinDigiphant.Savanna;
using DigiPhant;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace ChillinDigiphant.PeelPatrol.Editor
{
    [InitializeOnLoad]
    public static class PeelPatrolBuilder
    {
        public const string ScenePath="Assets/StudentWork/Scenes/Peel-Patrol.unity";
        const string Folder="Assets/StudentWork/PeelPatrol/Generated";
        static Transform parent;
        static Material orange,yellow,brown,white,black,wood,leaf,warning;
        static Mesh ring;
        static PeelPatrolBuilder() { EditorApplication.delayCall+=ProcessRequest; }
        static void ProcessRequest()
        {
            if(File.Exists(".tools/repair-banana") && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                File.Delete(".tools/repair-banana");
                var existing=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/Banana body.asset");
                var fresh=BananaMesh();existing.Clear();existing.vertices=fresh.vertices;existing.triangles=fresh.triangles;existing.RecalculateNormals();existing.RecalculateBounds();Object.DestroyImmediate(fresh);
                EditorUtility.SetDirty(existing);AssetDatabase.SaveAssets();Debug.Log("BANANA_MESH_REPAIRED");
            }
            if(!File.Exists(".tools/build-peel-patrol") || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(".tools/build-peel-patrol");
            try { Build(); }
            catch(Exception e) { Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/peel-build-error.txt",e.ToString());Debug.LogException(e); }
        }
        [MenuItem("DigiPhant/Peel Patrol/Create defense scene")]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play first.");
            if(File.Exists(ScenePath)) throw new Exception("Peel Patrol already exists. Open it to edit; the builder preserves existing work.");
            var habitat=Object.FindFirstObjectByType<SavannaHabitat>();
            if(!habitat) throw new Exception("Open the savanna scene first.");
            var scene=SceneManager.GetActiveScene();
            if(scene.isDirty)
            {
                string backup="Assets/StudentWork/Scenes/Before-Peel-Patrol-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity";
                if(!EditorSceneManager.SaveScene(scene,backup,true)) throw new Exception("Could not preserve unsaved scene edits.");
            }
            if(!EditorSceneManager.SaveScene(scene,ScenePath)) throw new Exception("Could not create defense scene.");
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            orange=Mat("Juicy orange","F78B23");yellow=Mat("Banana yellow","F5CF34");brown=Mat("Banana tips","57482E");
            white=Mat("Eyes white","FFF5D4");black=Mat("Eyes black","232E25");wood=Mat("Barricade wood","7D5F3B");leaf=Mat("Orange leaves","477847");warning=Mat("Approach trail","C28850",true);
            ring=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/StudentWork/Savanna/Generated/Activity ring.asset");
            parent=new GameObject("Peel Patrol / game and defense stations").transform;
            var game=parent.gameObject.AddComponent<PeelPatrolGame>();
            game.habitat=habitat;game.controller=habitat.controller;game.locomotion=habitat.locomotion;
            habitat.enabled=false;
            var controller=game.controller;
            controller.SetInputMode(InputMode.TestSliders);controller.showControls=false;
            var preview=controller.GetComponent<DigiPhantCameraPreview>();
            if(preview) { preview.autoStartBridge=false;preview.showPreview=false; }
            game.locomotion.enableLocomotion=true;game.locomotion.stageRadius=25;game.locomotion.walkSpeed=2.8f;game.locomotion.runSpeed=4.8f;game.locomotion.turnDegreesPerSecond=105;
            var trunk=controller.controls.FirstOrDefault(c=>c.label=="Trunk curl");
            if(trunk==null || trunk.bones.Length==0) throw new Exception("Trunk rig missing.");
            game.trunkTip=trunk.bones.Last();
            game.orangeStation=habitat.zones[1].anchor;game.cleanStation=habitat.zones[0].anchor;
            game.workshop=Point("Workshop / use zone",new Vector3(7,.05f,-6));
            game.waterRenderer=GameObject.Find("Watering hole / shallow water").GetComponent<Renderer>();
            habitat.ball.gameObject.SetActive(false);habitat.zones[1].food.SetActive(false);
            habitat.zones[2].marker.transform.position=game.workshop.position;
            game.orangeStation.name="Orange grove / reload zone";
            game.cleanStation.name="Water filter / clean zone";
            // A visible reusable supply pile. Reloading does not consume the entire grove.
            Box("Orange crate base",new Vector3(7,.18f,5.5f),new Vector3(2.4f,.35f,1.8f),wood);
            for(int i=0;i<4;i++)
            {
                float x=i%2==0?-1.15f:1.15f,z=i<2?-.8f:.8f;
                Box("Crate corner",new Vector3(7+x,.6f,5.5f+z),new Vector3(.12f,.85f,.12f),wood);
            }
            for(int i=0;i<12;i++)
            {
                Vector3 p=new Vector3(6.25f+(i%4)*.5f,.55f+(i/8)*.4f,5+(i/4%2)*.7f);
                var f=Primitive("Orange supply",PrimitiveType.Sphere,p,Vector3.one*.5f,orange);
                Primitive("Orange leaf",PrimitiveType.Sphere,p+Vector3.up*.27f,new Vector3(.2f,.04f,.09f),leaf);
            }
            for(int i=0;i<4;i++)
            {
                var log=Primitive("Workshop log",PrimitiveType.Cylinder,new Vector3(7+(i%2)*.45f,.25f+(i/2)*.43f,-6),new Vector3(.35f,1.5f,.35f),wood);
                log.transform.rotation=Quaternion.Euler(0,0,90);
            }
            Box("Workshop bench",new Vector3(8.4f,.9f,-5.1f),new Vector3(1.5f,.17f,1),wood);
            Box("Bench leg",new Vector3(8,.45f,-5.1f),new Vector3(.15f,.9f,.65f),wood);
            Box("Bench leg",new Vector3(8.8f,.45f,-5.1f),new Vector3(.15f,.9f,.65f),wood);
            Primitive("Water filter barrel",PrimitiveType.Cylinder,new Vector3(-3.3f,.65f,5.1f),new Vector3(.7f,.65f,.7f),leaf);
            game.lanes=new[] {
                Lane("East",new[]{new Vector3(22,0,5),new Vector3(6,0,0),new Vector3(-1,0,2),new Vector3(-4,0,6)}),
                Lane("North",new[]{new Vector3(-1,0,24),new Vector3(-5,0,16),new Vector3(-8,0,12)}),
                Lane("South",new[]{new Vector3(-17,0,-18),new Vector3(-14,0,-2),new Vector3(-12,0,4)})
            };
            game.bananaPrefab=CreateBananaPrefab();game.orangePrefab=CreateOrangePrefab();
            var target=new GameObject("Current throw target");target.transform.SetParent(parent);
            target.AddComponent<MeshFilter>().sharedMesh=ring;
            target.AddComponent<MeshRenderer>().sharedMaterial=Mat("Target highlight","FFF3BB",true);
            target.transform.localScale=new Vector3(.8f,1,.8f);target.SetActive(false);game.targetRing=target.transform;
            // Keep all three entry lanes in view while retaining the environment's camera angle.
            var camera=game.locomotion.followCamera;camera.orthographicSize=23;
            camera.transform.position=new Vector3(18.2f,21.5f,-24.5f);camera.transform.LookAt(new Vector3(-2,0,3));camera.rect=new Rect(0,0,1,1);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)}.Concat(EditorBuildSettings.scenes.Where(s=>s.path!=ScenePath)).ToArray();
            Validate();
            File.WriteAllText("Logs/peel-build.txt","PEEL_PATROL_BUILD_OK\n"+ScenePath+"\nThree banana lanes, oranges, barricades, water cleaning, three waves, win/loss/restart. Original savanna scene preserved.");
            Debug.Log("PEEL_PATROL_BUILD_OK");
        }
        static BananaLane Lane(string name,Vector3[] positions)
        {
            var path=positions.Select((p,i)=>Point(name+" / waypoint "+i,p)).ToArray();
            var barrier=new GameObject(name+" / barricade");barrier.transform.SetParent(parent);barrier.transform.position=positions[1];
            var previous=parent;parent=barrier.transform;
            for(int i=0;i<3;i++) Box("Wooden post",positions[1]+new Vector3((i-1)*1.1f,.65f,0),new Vector3(.23f,1.3f,.23f),wood);
            for(int i=0;i<2;i++) Box("Cross rail",positions[1]+new Vector3(0,.4f+i*.6f,0),new Vector3(3.2f,.23f,.18f),wood);
            parent=previous;barrier.transform.rotation=Quaternion.LookRotation((positions[1]-positions[0]).normalized);barrier.SetActive(false);
            for(int j=0;j<positions.Length-1;j++)
            {
                float distance=Vector3.Distance(positions[j],positions[j+1]);
                for(float t=1;t<distance;t+=1.5f)
                {var dot=Primitive(name+" / approach trail",PrimitiveType.Cylinder,Vector3.Lerp(positions[j],positions[j+1],t/distance)+Vector3.up*.015f,new Vector3(.23f,.012f,.23f),warning);}
            }
            for(int side=-1;side<=1;side+=2)
            {
                Primitive(name+" entry post",PrimitiveType.Cylinder,positions[0]+new Vector3(side*1.7f,1.1f,0),new Vector3(.13f,1.1f,.13f),wood);
                Box(name+" banana flag",positions[0]+new Vector3(side*1.7f,2,0),new Vector3(.85f,.65f,.06f),yellow);
            }
            return new BananaLane{title=name,path=path,barricade=barrier};
        }
        static BananaRaider CreateBananaPrefab()
        {
            Transform saved=parent;var root=new GameObject("Walking banana");parent=root.transform;
            var body=new GameObject("Curved banana body");body.transform.SetParent(parent);
            var mesh=BananaMesh();AssetDatabase.CreateAsset(mesh,Folder+"/Banana body.asset");
            body.AddComponent<MeshFilter>().sharedMesh=mesh;body.AddComponent<MeshRenderer>().sharedMaterial=yellow;
            Primitive("Lower banana tip",PrimitiveType.Sphere,new Vector3(0,.35f,0),new Vector3(.2f,.15f,.2f),brown).transform.SetParent(body.transform,true);
            Primitive("Upper banana tip",PrimitiveType.Sphere,new Vector3(0,2.15f,0),new Vector3(.16f,.22f,.16f),brown).transform.SetParent(body.transform,true);
            for(int side=-1;side<=1;side+=2)
            {
                var eye=Primitive("Goofy eye",PrimitiveType.Sphere,new Vector3(.46f+side*.135f,1.6f,.215f),new Vector3(.23f,.27f,.11f),white);eye.transform.SetParent(body.transform,true);
                var pupil=Primitive("Pupil",PrimitiveType.Sphere,new Vector3(.46f+side*.135f,1.58f,.277f),new Vector3(.1f,.15f,.05f),black);pupil.transform.SetParent(body.transform,true);
            }
            var left=Primitive("Left foot",PrimitiveType.Capsule,new Vector3(-.2f,.15f,.06f),new Vector3(.24f,.15f,.4f),brown).transform;
            var right=Primitive("Right foot",PrimitiveType.Capsule,new Vector3(.23f,.15f,.06f),new Vector3(.24f,.15f,.4f),brown).transform;
            var raider=root.AddComponent<BananaRaider>();raider.body=body.transform;raider.bodyRenderer=body.GetComponent<Renderer>();raider.leftFoot=left;raider.rightFoot=right;
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,Folder+"/Walking Banana.prefab");Object.DestroyImmediate(root);parent=saved;return prefab.GetComponent<BananaRaider>();
        }
        static OrangeFlight CreateOrangePrefab()
        {
            var root=GameObject.CreatePrimitive(PrimitiveType.Sphere);root.name="Thrown orange";Object.DestroyImmediate(root.GetComponent<Collider>());
            root.transform.localScale=Vector3.one*.45f;root.GetComponent<Renderer>().sharedMaterial=orange;root.AddComponent<OrangeFlight>();
            var saved=parent;parent=root.transform;
            var sprig=Primitive("Leaf",PrimitiveType.Sphere,new Vector3(.1f,.25f,0),new Vector3(.21f,.035f,.08f),leaf);
            parent=saved;
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,Folder+"/Thrown Orange.prefab");Object.DestroyImmediate(root);return prefab.GetComponent<OrangeFlight>();
        }
        static Mesh BananaMesh()
        {
            const int rows=13, sides=8;var v=new List<Vector3>();var tris=new List<int>();
            for(int j=0;j<rows;j++)
            {
                float t=j/(float)(rows-1),radius=.035f+Mathf.Pow(Mathf.Clamp01(Mathf.Sin(t*Mathf.PI)),.65f)*.23f;
                var center=new Vector3(Mathf.Sin(t*Mathf.PI)*.58f,.35f+t*1.8f,0);
                for(int i=0;i<sides;i++) {float a=i*Mathf.PI*2/sides;v.Add(center+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius));}
            }
            for(int j=0;j<rows-1;j++)for(int i=0;i<sides;i++)
            {int a=j*sides+i,b=j*sides+(i+1)%sides,c=a+sides,d=b+sides;tris.Add(a);tris.Add(c);tris.Add(b);tris.Add(b);tris.Add(c);tris.Add(d);}
            var m=new Mesh{name="Curved banana",vertices=v.ToArray(),triangles=tris.ToArray()};m.RecalculateNormals();m.RecalculateBounds();return m;
        }
        [MenuItem("DigiPhant/Peel Patrol/Validate defense scene")]
        public static void Validate()
        {
            var g=Object.FindFirstObjectByType<PeelPatrolGame>();
            if(!g || !g.controller || !g.locomotion || !g.trunkTip || !g.waterRenderer || !g.bananaPrefab || !g.orangePrefab || g.lanes.Length!=3) throw new Exception("Defense references are incomplete.");
            if(g.habitat.enabled) throw new Exception("Exploration inputs must not compete with game inputs.");
            foreach(var v in g.bananaPrefab.body.GetComponent<MeshFilter>().sharedMesh.vertices)
                if(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) || float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z)) throw new Exception("Banana mesh contains invalid vertices.");
            foreach(var lane in g.lanes) if(lane.path.Length<3 || lane.path.Any(p=>!p) || !lane.barricade) throw new Exception("Invalid enemy route.");
            foreach(var obj in SceneManager.GetActiveScene().GetRootGameObjects()) foreach(var t in obj.GetComponentsInChildren<Transform>(true))
                if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0) throw new Exception("Missing script: "+t.name);
            foreach(var renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)) foreach(var material in renderer.sharedMaterials)
                if(!material || !material.shader || material.shader.name=="Hidden/InternalErrorShader") throw new Exception("Missing shader: "+renderer.name);
            PeelPatrolChecks.CheckRules();Debug.Log("PEEL_PATROL_VALIDATION_OK");
        }
        static Transform Point(string name,Vector3 p) {var t=new GameObject(name).transform;t.SetParent(parent);t.position=p;return t;}
        static GameObject Box(string name,Vector3 p,Vector3 s,Material m)=>Primitive(name,PrimitiveType.Cube,p,s,m);
        static GameObject Primitive(string name,PrimitiveType type,Vector3 p,Vector3 s,Material mat)
        {var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent);g.transform.position=p;g.transform.localScale=s;g.GetComponent<Renderer>().sharedMaterial=mat;Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
        static Material Mat(string name,string hex,bool unlit=false)
        {var m=new Material(Shader.Find(unlit?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit")){name=name};ColorUtility.TryParseHtmlString("#"+hex,out Color c);m.SetColor("_BaseColor",c);m.SetFloat("_Smoothness",.16f);AssetDatabase.CreateAsset(m,Folder+"/"+name+".mat");return m;}
    }
}
