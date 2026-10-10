using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DigiPhant;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ChillinDigiphant.Savanna.Editor
{
    [InitializeOnLoad]
    public static class SavannaBuilder
    {
        public const string ScenePath = "Assets/StudentWork/Scenes/Savanna-Watering-Hole.unity";
        const string Folder = "Assets/StudentWork/Savanna/Generated";
        const string Request = ".tools/build-savanna";
        static readonly List<Transform> blockers = new List<Transform>();
        static readonly List<float> blockerRadii = new List<float>();
        static Transform root;
        static Mesh rockMesh, diskMesh, ringMesh;
        static Material sand, path, bank, water, waterLight, rock, bark, leaves, leafLight, dryGrass, greenGrass, fruit, teal, coral, gold;
        static System.Random random;
        static SavannaBuilder() { EditorApplication.delayCall += CheckRequest; }
        static void CheckRequest()
        {
            if (File.Exists(".tools/polish-savanna") && !EditorApplication.isPlayingOrWillChangePlaymode)
            { File.Delete(".tools/polish-savanna"); Polish(); }
            if (!File.Exists(Request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(Request);
            try { Build(); }
            catch (Exception e) { Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/savanna-build-error.txt", e.ToString()); Debug.LogException(e); }
        }
        [MenuItem("DigiPhant/Savanna/Create watering hole scene")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new Exception("Stop Play before creating a habitat.");
            if (File.Exists(ScenePath)) throw new Exception("Savanna scene already exists; open it to edit. Creation never overwrites it.");
            var scene = SceneManager.GetActiveScene();
            var controller = Object.FindFirstObjectByType<DigiPhantController>();
            if (!controller || controller.gameObject.scene != scene) throw new Exception("Open the elephant scene first.");
            var locomotion = controller.GetComponent<DigiPhantLocomotion>();
            if (!locomotion || !locomotion.travelRoot) throw new Exception("Elephant locomotion is missing.");
            string backup = "Assets/StudentWork/Scenes/Before-Savanna-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity";
            if (!EditorSceneManager.SaveScene(scene, backup, true)) throw new Exception("Could not preserve the current scene.");
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new Exception("Could not create habitat scene.");
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            random = new System.Random(74219); blockers.Clear(); blockerRadii.Clear();
            sand = Mat("Warm sand", "C6AC72"); path = Mat("Pale footpaths", "DAC08B"); bank = Mat("Wet clay", "857653");
            water = Mat("Jade water", "397E79", .65f); waterLight = Mat("Water glints", "9EC8B0", .5f, true);
            rock = Mat("Weathered stone", "9A9073"); bark = Mat("Acacia bark", "635644");
            leaves = Mat("Acacia olive", "62704B"); leafLight = Mat("Sunlit leaves", "87935D");
            dryGrass = Mat("Straw grass", "B19B5A"); greenGrass = Mat("Waterside reeds", "697C49");
            fruit = Mat("Wild fruit", "DE9A3B"); teal = Mat("Drink marker", "67C8B4", 0, true);
            coral = Mat("Play marker", "DA8967", 0, true); gold = Mat("Feed marker", "E5BE60", 0, true);
            rockMesh = SaveMesh(FacetedRock(), "Faceted stone"); diskMesh = SaveMesh(Disk(false), "Organic ground disk"); ringMesh = SaveMesh(Ring(), "Activity ring");
            root = new GameObject("Savanna habitat").transform;
            foreach (var go in scene.GetRootGameObjects())
                if (go.name == "Stage" || go.name.StartsWith("Stage distance grid")) go.SetActive(false);
            Shape("Savanna floor", diskMesh, new Vector3(0,-.025f,0), new Vector3(42,1,35), sand);
            Shape("Open clearing", diskMesh, new Vector3(0,-.012f,0), new Vector3(15,1,12), path);
            var horizon = Primitive("Ground to horizon", PrimitiveType.Plane, new Vector3(0,-.09f,0), new Vector3(30,1,30), sand);
            // Broad trails connect every activity; low vegetation leaves the routes readable.
            Trail(new Vector3(-1,0,0), new Vector3(-6,0,6), 2.2f);
            Trail(new Vector3(1,0,0), new Vector3(8,0,5), 2.2f);
            Trail(new Vector3(1,0,-1), new Vector3(8,0,-6), 2.5f);
            Vector3 pond = new Vector3(-9,0,8);
            Shape("Watering hole / sandy rim", diskMesh, pond + Vector3.up*.005f, new Vector3(7.1f,1,5.1f), path);
            Shape("Watering hole / damp shore", diskMesh, pond + Vector3.up*.018f, new Vector3(6.4f,1,4.5f), bank);
            Shape("Watering hole / shallow water", diskMesh, pond + Vector3.up*.03f, new Vector3(5.9f,1,4), water);
            // Water is a shallow wading area; all travel stays on the same ground plane.
            for (int i = 0; i < 19; i++)
            {
                float a = i / 19f * Mathf.PI * 2;
                if (a > 4.6f && a < 6.05f) continue; // accessible drinking shore
                Vector3 p = pond + new Vector3(Mathf.Cos(a)*6.3f, .12f, Mathf.Sin(a)*4.6f);
                Stone(p, Range(.5f,1.3f));
            }
            Tree(new Vector3(10,0,9), 1.1f, true);
            Tree(new Vector3(-15,0,15), 1.2f, true);
            Tree(new Vector3(17,0,1), .85f, true);
            Tree(new Vector3(-20,0,-2), 1.05f, true);
            Tree(new Vector3(1,0,20), 1.3f, true);
            Tree(new Vector3(21,0,17), 1.2f, true);
            Tree(new Vector3(-24,0,23), .9f, true);
            for (int i = 0; i < 10; i++) Tree(new Vector3(-38+i*8,0,Range(32,43)),Range(.8f,1.5f),false);
            for (int i = 0; i < 26; i++)
            {
                float a = Range(0,Mathf.PI*2), r = Range(23,30);
                Stone(new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r),Range(.7f,2.1f));
            }
            for (int i = 0; i < 12; i++)
                Shape("Distant kopje", rockMesh, new Vector3(-65+i*12,1,Range(50,66)),new Vector3(Range(10,18),Range(5,10),Range(8,15)),rock);
            Grass();
            var habitat = root.gameObject.AddComponent<SavannaHabitat>();
            habitat.controller = controller; habitat.locomotion = locomotion;
            habitat.obstacles = blockers.ToArray(); habitat.obstacleRadii = blockerRadii.ToArray();
            var drink = Zone("Watering hole", HabitatActivity.Drink,new Vector3(-4.5f,.05f,4.4f),teal,3.1f);
            var feed = Zone("Feeding tree", HabitatActivity.Feed,new Vector3(7,.05f,5.5f),gold,3.1f);
            var play = Zone("Play clearing", HabitatActivity.Play,new Vector3(7,.05f,-6),coral,3.1f);
            var basket = new GameObject("Fresh fruit / regrows after feeding").transform; basket.SetParent(root);
            for (int i = 0; i < 9; i++)
            {
                float a = i * 2.4f;
                var f = Shape("Wild marula fruit",rockMesh,new Vector3(7+Mathf.Cos(a)*.8f,.28f+(i%3)*.12f,5.5f+Mathf.Sin(a)*.65f),Vector3.one*.38f,fruit);
                f.transform.SetParent(basket);
            }
            feed.food = basket.gameObject;
            // An oversized enrichment ball responds to trunk nudges, rolls and slows down.
            var ball = Primitive("Enrichment ball",PrimitiveType.Sphere,new Vector3(7,.9f,-6),Vector3.one*1.8f,Mat("Ball terracotta","BD6747",.25f));
            habitat.ball = ball.transform; play.anchor = ball.transform;
            for (int i = 0; i < 3; i++)
            {
                var stripe = Shape("Ball seam",ringMesh,ball.transform.position,Vector3.one*.906f,Mat("Ball seams","F0C789"));
                stripe.transform.rotation = Quaternion.Euler(i*60,0,90); stripe.transform.SetParent(ball.transform,true);
            }
            habitat.zones = new[] { drink,feed,play };
            habitat.waterRipples = new Transform[3];
            for (int i = 0; i < 3; i++) habitat.waterRipples[i] = Shape("Water ripple " + (i+1),ringMesh,pond+new Vector3(2,.055f,-1),Vector3.one,waterLight).transform;
            var splashGO = new GameObject("Drinking splash"); splashGO.transform.SetParent(root); splashGO.transform.position = new Vector3(-5.5f,.15f,5.4f);
            var splash = splashGO.AddComponent<ParticleSystem>(); splash.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = splash.main; main.loop=false; main.duration=1.2f; main.startLifetime=.8f; main.startSpeed=3; main.startSize=.12f; main.gravityModifier=1; main.playOnAwake=false; main.startColor=new Color(.63f,.86f,.83f); main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission = splash.emission; emission.rateOverTime=35;
            var shape=splash.shape; shape.shapeType=ParticleSystemShapeType.Cone; shape.angle=25; shape.radius=.35f; shape.rotation=new Vector3(-90,0,0);
            splash.GetComponent<ParticleSystemRenderer>().sharedMaterial=Mat("Water droplets","A1DBD3",0,true); habitat.splash=splash;
            controller.showControls=false;
            // Open the original controls with Tab; enable the preview there when using tracking.
            controller.showCameraPreview=false;
            var preview=controller.GetComponent<DigiPhantCameraPreview>();
            if (!preview) preview=controller.gameObject.AddComponent<DigiPhantCameraPreview>();
            preview.autoStartBridge=false; preview.showPreview=false;
            locomotion.stageRadius=22; locomotion.followElephant=false; locomotion.StopMotion();
            var camera=locomotion.followCamera ? locomotion.followCamera : Camera.main;
            camera.transform.position=new Vector3(18.2f,17.55f,-21.05f); camera.transform.LookAt(new Vector3(0,0,3));
            camera.orthographic=true; camera.orthographicSize=20; camera.nearClipPlane=.1f; camera.farClipPlane=250;
            camera.rect = new Rect(0,0,1,1);
            camera.backgroundColor=new Color(.72f,.76f,.66f); camera.clearFlags=CameraClearFlags.SolidColor;
            foreach(var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if(light.type==LightType.Directional) { light.transform.rotation=Quaternion.Euler(43,-32,0); light.color=new Color(1,.88f,.7f); light.intensity=1.7f; light.shadows=LightShadows.Soft; }
            RenderSettings.ambientMode=AmbientMode.Trilight; RenderSettings.ambientSkyColor=new Color(.69f,.76f,.77f); RenderSettings.ambientEquatorColor=new Color(.55f,.52f,.4f); RenderSettings.ambientGroundColor=new Color(.3f,.26f,.2f);
            RenderSettings.fog=true; RenderSettings.fogMode=FogMode.Linear; RenderSettings.fogColor=new Color(.72f,.76f,.66f); RenderSettings.fogStartDistance=65; RenderSettings.fogEndDistance=145;
            if(SceneView.lastActiveSceneView) SceneView.lastActiveSceneView.LookAt(new Vector3(0,0,3),camera.transform.rotation,31,false,true);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            EditorBuildSettings.scenes=new[] {new EditorBuildSettingsScene(ScenePath,true)}.Concat(EditorBuildSettings.scenes.Where(s=>s.path!=ScenePath)).ToArray();
            Validate();
            File.WriteAllText("Logs/savanna-build.txt","SAVANNA_BUILD_OK\nScene: "+ScenePath+"\nPreserved scene: "+backup+"\n3 interaction areas; editable geometry; source elephant controls retained.");
            Debug.Log("SAVANNA_BUILD_OK: " + ScenePath);
        }
        [MenuItem("DigiPhant/Savanna/Validate habitat")]
        public static void Validate()
        {
            var h=Object.FindFirstObjectByType<SavannaHabitat>();
            if(!h || !h.controller || !h.locomotion || h.zones.Length!=3 || !h.ball || !h.splash) throw new Exception("Habitat references missing.");
            foreach(var z in h.zones) if(!z.anchor || !z.marker || z.radius<=0) throw new Exception("Incomplete interaction zone.");
            foreach(var renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                foreach(var m in renderer.sharedMaterials) if(!m || !m.shader || m.shader.name=="Hidden/InternalErrorShader") throw new Exception("Missing material: "+renderer.name);
            foreach(var go in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach(var t in go.GetComponentsInChildren<Transform>(true)) if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0) throw new Exception("Missing script: "+t.name);
            SavannaChecks.CheckHold();
            Debug.Log("SAVANNA_VALIDATION_OK: references, materials, interaction gate");
        }
        static void Polish()
        {
            if (SceneManager.GetActiveScene().path != ScenePath) return;
            foreach (string name in new[] { "Savanna grass mesh", "Shore reeds mesh" })
            {
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(Folder + "/" + name + ".asset");
                mesh.normals = Enumerable.Repeat(Vector3.up, mesh.vertexCount).ToArray(); EditorUtility.SetDirty(mesh);
            }
            var camera = Camera.main; camera.rect = new Rect(0,0,1,1);
            camera.transform.position = new Vector3(18.2f,17.55f,-21.05f);
            camera.transform.LookAt(new Vector3(0,0,3));
            var h = Object.FindFirstObjectByType<SavannaHabitat>();
            var preview = h.controller.GetComponent<DigiPhantCameraPreview>();
            if (!preview) preview = h.controller.gameObject.AddComponent<DigiPhantCameraPreview>();
            preview.autoStartBridge = false; preview.showPreview = false;
            if (SceneView.lastActiveSceneView)
            {
                SceneView.lastActiveSceneView.cameraMode = SceneView.GetBuiltinCameraMode(DrawCameraMode.Textured);
                SceneView.lastActiveSceneView.sceneLighting = true;
                SceneView.lastActiveSceneView.LookAt(new Vector3(0,0,3), camera.transform.rotation, 31, false, true);
            }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
            Debug.Log("SAVANNA_POLISH_OK");
        }
        static Material Mat(string name,string hex,float smooth=0,bool unlit=false)
        {
            string p=Folder+"/"+name+".mat"; var existing=AssetDatabase.LoadAssetAtPath<Material>(p); if(existing) return existing;
            var m=new Material(Shader.Find(unlit?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit")); m.name=name;
            ColorUtility.TryParseHtmlString("#"+hex,out Color c); m.SetColor("_BaseColor",c); m.SetFloat("_Smoothness",smooth); m.enableInstancing=true;
            AssetDatabase.CreateAsset(m,p); return m;
        }
        static Mesh SaveMesh(Mesh mesh,string name) { mesh.name=name; AssetDatabase.CreateAsset(mesh,Folder+"/"+name+".asset"); return mesh; }
        static GameObject Shape(string name,Mesh mesh,Vector3 p,Vector3 scale,Material mat)
        { var go=new GameObject(name); go.transform.SetParent(root); go.transform.position=p; go.transform.localScale=scale; go.AddComponent<MeshFilter>().sharedMesh=mesh; go.AddComponent<MeshRenderer>().sharedMaterial=mat; return go; }
        static GameObject Primitive(string name,PrimitiveType type,Vector3 p,Vector3 scale,Material mat)
        { var go=GameObject.CreatePrimitive(type); go.name=name; go.transform.SetParent(root); go.transform.position=p; go.transform.localScale=scale; go.GetComponent<Renderer>().sharedMaterial=mat; Object.DestroyImmediate(go.GetComponent<Collider>()); return go; }
        static void Trail(Vector3 a,Vector3 b,float width)
        { var go=Shape("Sandy trail",diskMesh,(a+b)*.5f+Vector3.up*.002f,new Vector3(width,1,Vector3.Distance(a,b)*.65f),path); go.transform.rotation=Quaternion.LookRotation(b-a); }
        static void Stone(Vector3 p,float size)
        { var go=Shape("Sun-worn boulder",rockMesh,p+Vector3.up*size*.24f,new Vector3(size, size*.6f,size*.8f),rock); go.transform.rotation=Quaternion.Euler(0,Range(0,360),Range(-10,10)); }
        static void Tree(Vector3 p,float size,bool block)
        {
            Transform old=root; var tree=new GameObject("Acacia / umbrella canopy").transform; tree.SetParent(root); tree.position=p; root=tree;
            Branch(p,p+new Vector3(.25f,4.4f*size,.15f),.42f*size);
            for(int i=0;i<5;i++)
            {
                float a=i*Mathf.PI*2/5; Vector3 end=p+new Vector3(Mathf.Cos(a)*2.5f*size,Range(5.3f,6.1f)*size,Mathf.Sin(a)*2*size);
                Branch(p+Vector3.up*3.2f*size,end,.17f*size);
                Shape("Layered foliage",rockMesh,end,new Vector3(2.6f,.8f,2.1f)*size,i%2==0?leaves:leafLight);
            }
            if(block) { blockers.Add(tree); blockerRadii.Add(1.8f*size); }
            root=old;
        }
        static void Branch(Vector3 a,Vector3 b,float r)
        { var go=Primitive("Branch",PrimitiveType.Cylinder,(a+b)*.5f,new Vector3(r,(b-a).magnitude*.5f,r),bark); go.transform.up=(b-a).normalized; }
        static HabitatZone Zone(string title,HabitatActivity activity,Vector3 p,Material mat,float radius)
        {
            var anchor=new GameObject(title+" / interaction center").transform; anchor.SetParent(root); anchor.position=p;
            var marker=Shape(title+" / approach circle",ringMesh,p,new Vector3(radius,1,radius),mat);
            return new HabitatZone {title=title,activity=activity,anchor=anchor,marker=marker.GetComponent<Renderer>(),color=mat.GetColor("_BaseColor"),radius=radius};
        }
        static float Range(float a,float b)=>Mathf.Lerp(a,b,(float)random.NextDouble());
        static Mesh Disk(bool unused)
        {
            const int n=64; var v=new Vector3[n+1]; var t=new int[n*3];
            for(int i=0;i<n;i++) { float a=i*2*Mathf.PI/n; float r=1+.035f*Mathf.Sin(a*5)+.025f*Mathf.Cos(a*9); v[i+1]=new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r); t[i*3]=0;t[i*3+1]=(i+1)%n+1;t[i*3+2]=i+1; }
            var m=new Mesh {vertices=v,triangles=t}; m.RecalculateNormals();m.RecalculateBounds();return m;
        }
        static Mesh Ring()
        {
            const int n=96; var v=new Vector3[n*2];var t=new int[n*6];
            for(int i=0;i<n;i++) { float a=i*2*Mathf.PI/n; var d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));v[i*2]=d*.972f;v[i*2+1]=d;int j=(i+1)%n*2;t[i*6]=i*2;t[i*6+1]=j;t[i*6+2]=i*2+1;t[i*6+3]=i*2+1;t[i*6+4]=j;t[i*6+5]=j+1; }
            var m=new Mesh {vertices=v,triangles=t};m.RecalculateNormals();m.RecalculateBounds();return m;
        }
        static Mesh FacetedRock()
        {
            var v=new List<Vector3>();var t=new List<int>();
            Vector3 Point(int lat,int lon) { float a=lon*2*Mathf.PI/10,b=lat*Mathf.PI/6;return new Vector3(Mathf.Sin(b)*Mathf.Cos(a),Mathf.Cos(b),Mathf.Sin(b)*Mathf.Sin(a)); }
            void Tri(Vector3 a,Vector3 b,Vector3 c) { int i=v.Count; v.Add(a);v.Add(b);v.Add(c);t.Add(i);t.Add(i+1);t.Add(i+2); }
            for(int j=0;j<6;j++)for(int i=0;i<10;i++) {var a=Point(j,i);var b=Point(j,i+1);var c=Point(j+1,i);var d=Point(j+1,i+1);Tri(a,b,c);Tri(b,d,c);}
            var m=new Mesh {vertices=v.ToArray(),triangles=t.ToArray()};m.RecalculateNormals();m.RecalculateBounds();return m;
        }
        static void Grass()
        {
            for(int pass=0;pass<2;pass++)
            {
                var v=new List<Vector3>();var tris=new List<int>();
                for(int i=0;i<(pass==0?1500:220);i++)
                {
                    float x=Range(-29,29),z=Range(-24,29);
                    if(pass==1) {float a=Range(0,Mathf.PI*2);x=-9+Mathf.Cos(a)*Range(6.3f,7.5f);z=8+Mathf.Sin(a)*Range(4.5f,5.5f);}
                    if(new Vector2(x,z).magnitude<10 || (Mathf.Pow((x+9)/6.3f,2)+Mathf.Pow((z-8)/4.5f,2)<1)) continue;
                    float h=Range(.25f,.7f)*(pass==1?1.6f:1);
                    for(int k=0;k<3;k++)
                    {float a=Range(0,Mathf.PI*2);var side=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*.11f;var p=new Vector3(x,.01f,z);int n=v.Count;v.Add(p-side);v.Add(p+side);v.Add(p+Vector3.up*h+side*.8f);tris.Add(n);tris.Add(n+2);tris.Add(n+1);tris.Add(n);tris.Add(n+1);tris.Add(n+2);}
                }
                var m=new Mesh {vertices=v.ToArray(),triangles=tris.ToArray(), normals=Enumerable.Repeat(Vector3.up,v.Count).ToArray()};m.RecalculateBounds();SaveMesh(m,pass==0?"Savanna grass mesh":"Shore reeds mesh");Shape(pass==0?"Golden grasses":"Shore reeds",m,Vector3.zero,Vector3.one,pass==0?dryGrass:greenGrass);
            }
        }
    }
}
