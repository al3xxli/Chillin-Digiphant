using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace ChillinDigiphant.Kart.Editor
{
    [InitializeOnLoad]
    public static class KartObstacleBuilder
    {
        const string RootName="Savannah racing challenges";
        const string Source="Assets/SavannahCourse/Generated/v4/";
        static Transform root;
        static KartRace race;
        static Material bark,leaves,sage,stone,sand,cyan,gold,white;
        static Mesh cylinder,canopy;
        static double nextPoll;
        static KartObstacleBuilder(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup<nextPoll)return;
            nextPoll=EditorApplication.timeSinceStartup+1;
            if(File.Exists(".tools/fix-kart-signs"))
            {
                File.Delete(".tools/fix-kart-signs");race=Object.FindAnyObjectByType<KartRace>();
                foreach(var obstacle in race.obstacles)foreach(Transform child in obstacle.transform)
                    if(child.name=="Advance warning post" || child.GetComponent<TextMesh>())PlaceSign(child,obstacle,child.localPosition.x<0?-1:1,child.GetComponent<TextMesh>()!=null);
                EditorSceneManager.MarkSceneDirty(race.gameObject.scene);EditorSceneManager.SaveScene(race.gameObject.scene);
                File.WriteAllText("Logs/Kart/sign-placement.txt","Warning signs placed relative to the actual approach road centerline, outside its 18m width. Physical obstacle geometry unchanged.\n");return;
            }
            if(!File.Exists(".tools/obstacles-kart"))return;File.Delete(".tools/obstacles-kart");
            try{Install();}catch(Exception e){File.WriteAllText("Logs/Kart/obstacle-build-error.txt",e.ToString());Debug.LogException(e);}
        }
        [MenuItem("DigiPhant/Kart/Install Savannah racing challenges")]
        public static void Install()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play first.");
            var scene=SceneManager.GetActiveScene();if(scene.path!=KartBuilder.ScenePath)throw new InvalidOperationException("Open Digiphant-Kart first.");
            string backup=AssetDatabase.GenerateUniqueAssetPath("Assets/StudentWork/Scenes/Kart-BeforeObstacles-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity");
            if(!EditorSceneManager.SaveScene(scene,backup,true))throw new IOException("Could not back up the current scene.");
            race=Object.FindAnyObjectByType<KartRace>();
            foreach(var t in race.track.GetComponentsInChildren<Transform>().Where(t=>t.name==RootName || t.name.StartsWith("REFILL") || t.name=="Refill orange display").ToArray())
                if(t)Object.DestroyImmediate(t.gameObject);
            race.refillBays=Array.Empty<Transform>();
            Material SourceMat(string n)=>AssetDatabase.LoadAssetAtPath<Material>(Source+n+".mat");
            Material KartMat(string n)=>AssetDatabase.LoadAssetAtPath<Material>(KartBuilder.Folder+"/"+n+".mat");
            bark=SourceMat("Bark");leaves=SourceMat("Olive foliage");sage=SourceMat("Sage foliage");stone=SourceMat("Basin stone");sand=SourceMat("Pale sand path");
            cyan=KartMat("Mint neon");gold=KartMat("Solar gold");white=KartMat("Warm ivory");
            cylinder=AssetDatabase.LoadAssetAtPath<Mesh>(Source+"Octagonal cylinder.asset");canopy=AssetDatabase.LoadAssetAtPath<Mesh>(Source+"Acacia canopy.asset");
            root=new GameObject(RootName).transform;root.SetParent(race.track.transform,false);
            var list=new List<KartObstacle>();
            var jump=Station(KartObstacleKind.Jump,36,"Fallen acacia log");list.Add(jump);
            var timber=MeshPart(jump.transform,"Physical fallen timber",cylinder,bark,new Vector3(0,.55f,0),new Vector3(1.1f,18,1.1f));timber.localRotation=Quaternion.Euler(0,0,90);
            foreach(int side in new[]{-1,1}){var end=MeshPart(jump.transform,"Cut log end",cylinder,sand,new Vector3(side*9.02f,.55f,0),new Vector3(1,.06f,1));end.localRotation=Quaternion.Euler(0,0,90);}
            Box(jump.transform,Vector3.up*.55f,new Vector3(18,1.1f,1.1f));
            for(int row=0;row<4;row++)Cube(jump.transform,"Jump approach stripes",new Vector3(0,.06f,-4-row*2),new Vector3(16,.035f,.25f),gold);
            Sign(jump,"JUMP",gold);
            var duck=Station(KartObstacleKind.Duck,78,"Low acacia canopy");list.Add(duck);
            foreach(int side in new[]{-1,1})
            {
                MeshPart(duck.transform,"Acacia trunk",cylinder,bark,new Vector3(side*9.5f,2,0),new Vector3(.7f,4,.7f));
                MeshPart(duck.transform,"Canopy crown",canopy,side<0?leaves:sage,new Vector3(side*4.5f,3.9f,0),new Vector3(11,2.1f,5));
            }
            Cube(duck.transform,"Low branch",new Vector3(0,2.4f,0),new Vector3(18,.5f,2),bark);
            Box(duck.transform,new Vector3(0,3.1f,0),new Vector3(18,1.9f,2)); // 2.15m clearance; crouched kart is 1.45m high.
            for(int i=-8;i<=8;i+=2)Cube(duck.transform,"Duck clearance marker",new Vector3(i,2.18f,-1.08f),new Vector3(.65f,.12f,.1f),cyan);
            Sign(duck,"DUCK",cyan);
            foreach(var entry in new[]{(132,1),(143,-1),(154,1)})
            {
                var obstacle=Station(KartObstacleKind.Slalom,entry.Item1,"Acacia and stone slalom");obstacle.passingLane=entry.Item2*4.9f;list.Add(obstacle);
                float blocked=-entry.Item2*4;
                Box(obstacle.transform,new Vector3(blocked,1.3f,0),new Vector3(10,2.6f,1.4f));
                for(int n=-2;n<=2;n++)
                {
                    MeshPart(obstacle.transform,"Stone obstacle",cylinder,stone,new Vector3(blocked+n*1.7f,.7f,0),new Vector3(2,1.4f,1.6f));
                    MeshPart(obstacle.transform,"Acacia bush",canopy,n%2==0?leaves:sage,new Vector3(blocked+n*1.7f,1.6f,0),new Vector3(2.2f,2.2f,2));
                }
                for(int i=0;i<3;i++)Cube(obstacle.transform,"Slalom arrow",new Vector3(blocked+(i-1)*2,2.15f,-.9f),new Vector3(.2f,1,.15f),gold).localRotation=Quaternion.Euler(0,0,entry.Item2*-40);
                Sign(obstacle,entry.Item2>0?"KEEP RIGHT":"KEEP LEFT",gold);
            }
            race.obstacles=list.ToArray();
            foreach(var kart in race.racers)
            {
                var feedback=kart.GetComponent<KartFeedback>();if(!feedback || !feedback.visualRoot)throw new Exception("Kart visual suspension is missing.");
                if(feedback.driverRoot)continue;
                var children=feedback.visualRoot.Cast<Transform>().Where(t=>t.name!="Kart chassis / physical ride").ToArray();
                feedback.driverRoot=new GameObject("Driver duck pivot").transform;feedback.driverRoot.SetParent(feedback.visualRoot,false);feedback.driverRoot.localPosition=Vector3.up*.75f;
                foreach(var child in children)child.SetParent(feedback.driverRoot,true);
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();KartBuilder.Validate();
            File.WriteAllText("Logs/Kart/obstacle-build.txt","KART_OBSTACLES_INSTALLED\nBackup: "+backup+"\nRefill platforms and floating refill displays removed.\n1 jump log; 1 low canopy; 3 alternating slalom barriers; advance signs; AI and player physical actions.\nSource meshes/materials referenced without modifying SavannahCourse.\n");
        }
        static KartObstacle Station(KartObstacleKind kind,int sample,string name)
        {
            var go=new GameObject(name);go.transform.SetParent(root);go.transform.SetPositionAndRotation(race.track.Point(sample),Quaternion.LookRotation(race.track.Forward(sample)));
            var obstacle=go.AddComponent<KartObstacle>();obstacle.kind=kind;obstacle.trackSample=sample;return obstacle;
        }
        static Transform MeshPart(Transform parent,string name,Mesh mesh,Material material,Vector3 p,Vector3 scale)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=scale;
            go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;return go.transform;
        }
        static Transform Cube(Transform parent,string name,Vector3 p,Vector3 scale,Material mat)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=scale;
            Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=mat;return go.transform;
        }
        static void Box(Transform parent,Vector3 center,Vector3 size){var box=parent.gameObject.AddComponent<BoxCollider>();box.center=center;box.size=size;}
        static void Sign(KartObstacle obstacle,string caption,Material color)
        {
            foreach(int side in new[]{-1,1})
            {
                var post=Cube(obstacle.transform,"Advance warning post",new Vector3(side*9.8f,2.5f,-19),new Vector3(.18f,5,.18f),color);PlaceSign(post,obstacle,side,false);
                var label=new GameObject(caption);label.transform.SetParent(obstacle.transform,false);label.transform.localPosition=new Vector3(side*9.8f,5.2f,-19);
                var text=label.AddComponent<TextMesh>();text.text=caption;text.fontSize=40;text.characterSize=.14f;text.anchor=TextAnchor.MiddleCenter;text.color=color.color;
                PlaceSign(label.transform,obstacle,side,true);
            }
        }
        static void PlaceSign(Transform part,KartObstacle obstacle,int side,bool label)
        {
            int sample=obstacle.trackSample-6;
            part.position=race.track.Point(sample)+race.track.Right(sample)*side*(race.track.roadWidth*.5f+1)+Vector3.up*(label?5.2f:2.5f);
            part.rotation=label?Quaternion.LookRotation(Vector3.ProjectOnPlane(race.track.Forward(sample),Vector3.up)):Quaternion.identity;
        }
    }
}
