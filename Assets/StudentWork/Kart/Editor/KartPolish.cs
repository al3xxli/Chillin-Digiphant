using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ChillinDigiphant.Kart.Editor
{
    [InitializeOnLoad]
    public static class KartPolish
    {
        const string AssetsPath = KartBuilder.Folder + "/RacewayV2";
        static Transform scenery;
        static Material dark, white, cyan, gold, orange, pink;
        static double poll;
        static KartPolish() { EditorApplication.update += Poll; }
        static void Poll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < poll) return;
            poll = EditorApplication.timeSinceStartup + 1;
            if (!File.Exists(".tools/upgrade-kart")) return;
            File.Delete(".tools/upgrade-kart");
            try { Upgrade(); }
            catch (Exception e) { File.WriteAllText("Logs/Kart/upgrade-error.txt", e.ToString()); Debug.LogException(e); }
        }
        [MenuItem("DigiPhant/Kart/Upgrade to wide raceway")]
        public static void Upgrade()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != KartBuilder.ScenePath) throw new InvalidOperationException("Open the saved Digiphant-Kart scene first.");
            string backup = AssetDatabase.GenerateUniqueAssetPath("Assets/StudentWork/Scenes/Kart-BeforeWideRaceway-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity");
            if (!EditorSceneManager.SaveScene(scene, backup, true)) throw new IOException("Could not back up current scene.");
            var race = Object.FindAnyObjectByType<KartRace>(); var track = race.track;
            // The imported Savannah root and the existing control/rig hierarchy are deliberately independent.
            foreach (Transform child in track.transform.Cast<Transform>().ToArray())
                if (!child.GetComponent<KartVehicle>()) Object.DestroyImmediate(child.gameObject);
            scenery = new GameObject("Wide Rainbow Raceway / v2").transform; scenery.SetParent(track.transform, false);
            Directory.CreateDirectory(AssetsPath); AssetDatabase.Refresh();
            Material M(string n) => AssetDatabase.LoadAssetAtPath<Material>(KartBuilder.Folder + "/" + n + ".mat");
            dark = M("Midnight chassis"); white = M("Warm ivory"); cyan = M("Mint neon"); gold = M("Solar gold"); orange = M("Juicy orange"); pink = M("Electric pink");
            track.points = Route(); track.gates = Enumerable.Range(0, 18).Select(i => i * 12).ToArray(); track.roadWidth = 18;
            track.openEdges = new[] { new KartTrack.OpenEdge { first = 48, last = 65, side = 1 }, new KartTrack.OpenEdge { first = 108, last = 123, side = -1 }, new KartTrack.OpenEdge { first = 164, last = 183, side = 1 } };
            Road(track, M);
            race.refillBays = new[] { Bay(track, 36, -1), Bay(track, 144, 1) };
            for (int i = 0; i < race.racers.Length; i++)
            {
                var kart = race.racers[i];
                kart.maxSpeed = 22; kart.acceleration = 10; kart.brake = 19; kart.steeringDegrees = 72;
                kart.aiLane = i == 1 ? 4.7f : i == 2 ? -4.7f : 0; kart.aiPace = .82f + i * .025f;
                kart.GetComponent<Rigidbody>().solverIterations = 10;
                kart.GetComponent<Rigidbody>().solverVelocityIterations = 5;
                kart.GetComponent<Rigidbody>().angularDamping = 8;
                foreach (var collider in kart.GetComponents<Collider>()) if (collider.enabled && collider.sharedMaterial)
                { collider.sharedMaterial.bounciness = .06f; EditorUtility.SetDirty(collider.sharedMaterial); }
                var feedback=kart.GetComponent<KartFeedback>(); if(!feedback)feedback=kart.gameObject.AddComponent<KartFeedback>();
                if(!feedback.visualRoot)
                {
                    var children=kart.transform.Cast<Transform>().ToArray();
                    feedback.visualRoot=new GameObject("Vehicle visual suspension").transform;feedback.visualRoot.SetParent(kart.transform,false);
                    foreach(var child in children)child.SetParent(feedback.visualRoot,false);
                }
                int p = i < 2 ? 0 : track.Count - 6;
                kart.transform.SetPositionAndRotation(track.Point(p) + track.Right(p) * (i % 2 == 0 ? -4.7f : 4.7f) + Vector3.up * .2f,
                    Quaternion.LookRotation(Vector3.ProjectOnPlane(track.Forward(p), Vector3.up)));
            }
            var camera = race.locomotion.followCamera; camera.farClipPlane = 420;
            var follow = camera.GetComponent<KartFollowCamera>(); follow.offset = new Vector3(0, 8, -13);
            camera.transform.position = race.player.transform.position + race.player.transform.rotation * follow.offset;
            camera.transform.LookAt(race.player.transform.position + Vector3.up * 2);
            RenderSettings.fogStartDistance = 200; RenderSettings.fogEndDistance = 410;
            var random = new System.Random(20261008);
            for (int i = 0; i < 95; i++)
            {
                float a = (float)random.NextDouble() * Mathf.PI * 2, radius = 140 + (float)random.NextDouble() * 65;
                Shape("Savannah star", PrimitiveType.Sphere, new Vector3(-20 + Mathf.Sin(a) * radius, 30 + (float)random.NextDouble() * 70, Mathf.Cos(a) * radius), Vector3.one * (.2f + (float)random.NextDouble() * .4f), i % 3 == 0 ? gold : white);
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            KartBuilder.Validate();
            File.WriteAllText("Logs/Kart/upgrade.txt", "WIDE_RACEWAY_OK\nScene: " + scene.path + "\nBackup: " + backup + "\n18 metre road / broad bends / 3 marked open edges / 18 ordered checkpoints / no static obstacles in the racing line\n");
        }
        static Vector3[] Route()
        {
            // A non-intersecting circuit with generous corner radii. The instructor's scenery remains below.
            Vector3[] p = { new(0,12,-70), new(35,12,-70), new(65,13,-55), new(80,17,-25), new(80,22,15), new(62,24,48),
                new(30,23,65), new(0,20,62), new(-22,17,44), new(-45,15,42), new(-72,16,60), new(-100,18,50),
                new(-114,21,18), new(-108,23,-20), new(-84,20,-48), new(-55,16,-58), new(-35,12,-70), new(-15,12,-70) };
            var result = new List<Vector3>();
            for (int i = 0; i < p.Length; i++) for (int j = 0; j < 12; j++)
            {
                float t = j / 12f; Vector3 a = p[(i + p.Length - 1) % p.Length], b = p[i], c = p[(i + 1) % p.Length], d = p[(i + 2) % p.Length];
                result.Add(.5f * (2*b + (-a+c)*t + (2*a-5*b+4*c-d)*t*t + (-a+3*b-3*c+d)*t*t*t));
            }
            return result.ToArray();
        }
        static void Road(KartTrack t, Func<string, Material> material)
        {
            float half = t.roadWidth / 2;
            for (int lane = 0; lane < 6; lane++)
            {
                var v = new List<Vector3>(); var tris = new List<int>(); var uv = new List<Vector2>();
                for (int i = 0; i <= t.Count; i++)
                {
                    Vector3 p = t.points[i % t.Count], side = Vector3.Cross(Vector3.up, t.points[(i+1)%t.Count] - t.points[(i+t.Count-1)%t.Count]).normalized;
                    v.Add(p + side * (-half + lane*t.roadWidth/6)); v.Add(p + side * (-half + (lane+1)*t.roadWidth/6));
                    uv.Add(new Vector2(0,i)); uv.Add(new Vector2(1,i));
                    if (i < t.Count) { int n = i*2; tris.AddRange(new[] { n,n+2,n+1,n+1,n+2,n+3 }); }
                }
                var mesh = new Mesh { name = "Wide ribbon " + lane }; mesh.SetVertices(v); mesh.SetUVs(0,uv); mesh.SetTriangles(tris,0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
                string path = AssetsPath + "/Ribbon " + lane + ".asset"; var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (existing) { EditorUtility.CopySerialized(mesh, existing); Object.DestroyImmediate(mesh); mesh = existing; } else AssetDatabase.CreateAsset(mesh,path);
                var road = new GameObject("18m rainbow ribbon / " + lane); road.transform.SetParent(scenery,false); road.layer = KartRace.RoadLayer;
                road.AddComponent<MeshFilter>().sharedMesh = mesh; road.AddComponent<MeshRenderer>().sharedMaterial = material("Ribbon " + lane); road.AddComponent<MeshCollider>().sharedMesh = mesh;
            }
            for (int i = 0; i < t.Count; i++)
            {
                Vector3 a = t.Point(i), b = t.Point(i+1);
                foreach (int side in new[] {-1,1})
                {
                    bool open = t.IsOpenEdge(i,side);
                    Vector3 from = a + t.Right(i)*side*(half+.12f), to = b + t.Right(i+1)*side*(half+.12f);
                    if (!open)
                    {
                        var rail = Shape("Physical edge rail",PrimitiveType.Cube,(from+to)*.5f+Vector3.up*.65f,new Vector3(.32f,1.3f,Vector3.Distance(from,to)+.15f),i%12<6?cyan:pink,true);
                        rail.rotation = Quaternion.LookRotation(to-from);
                    }
                    else if (i%2 == 0)
                    {
                        var edge = Shape("OPEN EDGE / amber warning",PrimitiveType.Cube,(from+to)*.5f+Vector3.up*.035f,new Vector3(.45f,.07f,Vector3.Distance(from,to)*.75f),gold);
                        edge.rotation = Quaternion.LookRotation(to-from);
                    }
                }
                if (i%6 == 3) foreach (int sign in new[] {-1,1})
                {
                    var arrow = Shape("Racing direction",PrimitiveType.Cube,a+t.Right(i)*sign*.42f+Vector3.up*.06f,new Vector3(.2f,.04f,1.35f),white);
                    arrow.rotation = Quaternion.LookRotation(t.Forward(i))*Quaternion.Euler(0,-sign*40,0);
                }
                if (i%36 == 10) for (int lane=-1;lane<=1;lane++) Pickup(a+t.Right(i)*lane*5);
                if (i==30 || i==88 || i==155)
                {
                    var pad = Shape("Rainbow boost",PrimitiveType.Cube,a+Vector3.up*.04f,new Vector3(15,.08f,3),cyan);
                    pad.rotation = Quaternion.LookRotation(t.Forward(i)); var box = pad.gameObject.AddComponent<BoxCollider>(); box.isTrigger=true; box.size=new Vector3(1,30,1); box.center=Vector3.up*14;
                    pad.gameObject.AddComponent<KartBoostPad>();
                }
            }
            foreach (var edge in t.openEdges)
            {
                int i = edge.first-3; Vector3 p = t.Point(i)+t.Right(i)*edge.side*10+Vector3.up*3.8f;
                Label("! OPEN EDGE",p,Quaternion.LookRotation(t.Forward(i)),gold.color,.14f);
            }
            for (int g=0;g<t.gates.Length;g++)
            {
                int i=t.gates[g]; Vector3 p=t.Point(i); Quaternion yaw=Quaternion.LookRotation(t.Forward(i));
                foreach(int sign in new[] {-1,1}) Shape("Gate "+g+" beacon",PrimitiveType.Cylinder,p+t.Right(i)*sign*(half+.65f)+Vector3.up*2.6f,new Vector3(.18f,2.6f,.18f),g==0?gold:cyan);
                if(g!=0)continue;
                var header=Shape("START FINISH arch",PrimitiveType.Cube,p+Vector3.up*6.8f,new Vector3(20,1.2f,.5f),dark); header.rotation=yaw;
                Label("DIGIPHANT KART",p+Vector3.up*6.8f-t.Forward(i)*.3f,yaw,white.color,.17f);
                for(int col=0;col<24;col++)for(int row=0;row<2;row++)
                {
                    var tile=Shape("Checkered start",PrimitiveType.Cube,p+t.Right(i)*(-8.625f+col*.75f)+t.Forward(i)*(row*.75f-.375f)+Vector3.up*.025f,new Vector3(.75f,.04f,.75f),(col+row)%2==0?white:dark);tile.rotation=yaw;
                }
            }
        }
        static Transform Bay(KartTrack t,int i,int side)
        {
            Vector3 p=t.Point(i)+t.Right(i)*side*5.8f;
            var bay=Shape("REFILL BAY / right-hand action",PrimitiveType.Cube,p+Vector3.up*.06f,new Vector3(4.6f,.07f,7),dark);bay.rotation=Quaternion.LookRotation(t.Forward(i));
            Label("REFILL",p+t.Right(i)*side*4.8f+Vector3.up*3,Quaternion.LookRotation(t.Forward(i)),gold.color,.13f);
            for(int n=0;n<3;n++)Shape("Refill orange display",PrimitiveType.Sphere,p+t.Right(i)*side*4.5f+t.Forward(i)*(n-1)*.8f+Vector3.up*1.5f,Vector3.one*.65f,orange);
            return bay;
        }
        static void Pickup(Vector3 p)
        {
            var go=new GameObject("Orange item box / respawns");go.transform.SetParent(scenery);go.transform.position=p;
            var box=go.AddComponent<BoxCollider>();box.isTrigger=true;box.center=Vector3.up*1.1f;box.size=new Vector3(1.5f,2.3f,1.5f);
            var visual=Shape("Floating item diamond",PrimitiveType.Cube,p+Vector3.up*1.25f,Vector3.one*.9f,cyan);visual.SetParent(go.transform,true);visual.rotation=Quaternion.Euler(0,45,35);
            var fruit=Shape("Orange inside",PrimitiveType.Sphere,p+Vector3.up*1.25f,Vector3.one*.58f,orange);fruit.SetParent(visual,true);
            go.AddComponent<KartPickup>().visual=visual;
        }
        static Transform Shape(string name,PrimitiveType type,Vector3 p,Vector3 scale,Material mat,bool collision=false)
        {
            var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(scenery);g.transform.position=p;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=mat;
            if(!collision)Object.DestroyImmediate(g.GetComponent<Collider>());return g.transform;
        }
        static void Label(string text,Vector3 p,Quaternion rotation,Color color,float size)
        {
            var g=new GameObject(text);g.transform.SetParent(scenery);g.transform.SetPositionAndRotation(p,rotation);var label=g.AddComponent<TextMesh>();label.text=text;label.fontSize=40;label.characterSize=size;label.anchor=TextAnchor.MiddleCenter;label.color=color;
        }
    }
}
