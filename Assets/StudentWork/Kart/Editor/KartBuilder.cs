using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DigiPhant;
using ChillinDigiphant.PeelPatrol;
using ChillinDigiphant.Savanna;
using StudentStarter.Savannah;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ChillinDigiphant.Kart.Editor
{
    [InitializeOnLoad]
    public static class KartBuilder
    {
        public const string ScenePath = "Assets/StudentWork/Scenes/Digiphant-Kart.unity";
        public const string Folder = "Assets/StudentWork/Kart/Generated";
        static double nextPoll;
        static Transform root;
        static bool retryFailedBuild;
        static Material dark, white, cyan, gold, orange, pink;
        static KartBuilder() { EditorApplication.update += Poll; }
        static void Poll()
        {
            if (EditorApplication.timeSinceStartup < nextPoll || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            nextPoll = EditorApplication.timeSinceStartup + 1;
            if (File.Exists(".tools/refresh-kart")) { File.Delete(".tools/refresh-kart"); AssetDatabase.Refresh(); return; }
            if (File.Exists(".tools/round-kart-colliders"))
            {
                File.Delete(".tools/round-kart-colliders"); var race = Object.FindAnyObjectByType<KartRace>();
                foreach (var kart in race.racers)
                {
                    var box = kart.GetComponent<BoxCollider>(); var material = box.sharedMaterial; box.enabled = false;
                    var capsule = kart.gameObject.AddComponent<CapsuleCollider>(); capsule.direction = 2; capsule.radius = 1.25f; capsule.height = 4.2f; capsule.center = new Vector3(0, 1.3f, 0); capsule.sharedMaterial = material;
                }
                EditorSceneManager.MarkSceneDirty(race.gameObject.scene); EditorSceneManager.SaveScene(race.gameObject.scene); return;
            }
            if (File.Exists(".tools/fix-kart-supports"))
            {
                File.Delete(".tools/fix-kart-supports"); var race = Object.FindAnyObjectByType<KartRace>();
                foreach (var collider in race.GetComponentsInChildren<Collider>()) if (collider.name == "Bridge support") collider.enabled = false;
                EditorSceneManager.MarkSceneDirty(race.gameObject.scene); EditorSceneManager.SaveScene(race.gameObject.scene); return;
            }
            if (File.Exists(".tools/rebase-kart-start"))
            {
                File.Delete(".tools/rebase-kart-start");
                var race = Object.FindAnyObjectByType<KartRace>(); var track = race.track;
                Vector3 oldStart = track.Point(0); Quaternion oldRotation = Quaternion.LookRotation(track.Forward(0));
                var markers = track.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("Gate ") && t.name.EndsWith(" beacon")).Select(t => (t, int.Parse(t.name.Split(' ')[1]), Vector3.Dot(t.position - track.Point(track.gates[int.Parse(t.name.Split(' ')[1])]), track.Right(track.gates[int.Parse(t.name.Split(' ')[1])])) < 0 ? -1 : 1)).ToArray();
                track.points = track.points.Skip(60).Concat(track.points.Take(60)).ToArray();
                Quaternion delta = Quaternion.LookRotation(track.Forward(0)) * Quaternion.Inverse(oldRotation);
                foreach (var t in track.GetComponentsInChildren<Transform>()) if (t.name == "START FINISH arch" || t.name == "DIGIPHANT KART" || t.name == "Checkered start") { t.position = track.Point(0) + delta * (t.position - oldStart); t.rotation = delta * t.rotation; }
                foreach (var marker in markers) marker.t.position = track.Point(track.gates[marker.Item2]) + track.Right(track.gates[marker.Item2]) * marker.Item3 * 4.6f + Vector3.up * 2;
                for (int i = 0; i < race.racers.Length; i++)
                {
                    int p = i < 2 ? 0 : track.Count - 8;
                    race.racers[i].transform.SetPositionAndRotation(track.Point(p) + track.Right(p) * (i % 2 == 0 ? -1.9f : 1.9f) + Vector3.up * .15f, Quaternion.LookRotation(Vector3.ProjectOnPlane(track.Forward(p), Vector3.up)));
                }
                var camera = race.locomotion.followCamera; camera.transform.position = race.player.transform.position + race.player.transform.rotation * camera.GetComponent<KartFollowCamera>().offset; camera.transform.LookAt(race.player.transform.position + Vector3.up * 2);
                EditorSceneManager.MarkSceneDirty(race.gameObject.scene); EditorSceneManager.SaveScene(race.gameObject.scene);
                File.WriteAllText("Logs/Kart/start-grid.txt", "Start line moved to the flat long straight. The course geometry is unchanged."); return;
            }
            if (File.Exists(".tools/polish-kart"))
            {
                File.Delete(".tools/polish-kart");
                var race = Object.FindAnyObjectByType<KartRace>();
                for (int i = 0; i < race.racers.Length; i++)
                {
                    int p = i < 2 ? 0 : race.track.Count - 8;
                    race.racers[i].transform.SetPositionAndRotation(race.track.Point(p) + race.track.Right(p) * (i % 2 == 0 ? -1.9f : 1.9f) + Vector3.up * .15f, Quaternion.LookRotation(Vector3.ProjectOnPlane(race.track.Forward(p), Vector3.up)));
                }
                if (!race.controller.GetComponent<DigiPhantRecording>()) race.controller.gameObject.AddComponent<DigiPhantRecording>();
                EditorSceneManager.MarkSceneDirty(race.gameObject.scene); EditorSceneManager.SaveScene(race.gameObject.scene);
                File.WriteAllText("Logs/Kart/polish.txt", "Separated the two starting rows by a full kart length; attached the existing performance recorder."); return;
            }
            if (File.Exists(".tools/retry-kart"))
            {
                string source = File.ReadAllText(".tools/retry-kart").Trim(); File.Delete(".tools/retry-kart");
                try
                {
                    if (!File.Exists("Logs/Kart/build-error.txt") || !source.StartsWith("Assets/StudentWork/Scenes/Before-Kart-")) throw new InvalidOperationException("Retry requires a failed generated build and its recorded pre-build snapshot.");
                    EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), AssetDatabase.GenerateUniqueAssetPath("Assets/StudentWork/Scenes/Kart-Incomplete-Snapshot.unity"), true);
                    EditorSceneManager.OpenScene(source); retryFailedBuild = true; Build();
                }
                catch (Exception e) { File.WriteAllText("Logs/Kart/build-error.txt", e.ToString()); Debug.LogException(e); }
                finally { retryFailedBuild = false; }
                return;
            }
            if (!File.Exists(".tools/build-kart")) return;
            File.Delete(".tools/build-kart");
            try { Build(); }
            catch (Exception e) { Directory.CreateDirectory("Logs/Kart"); File.WriteAllText("Logs/Kart/build-error.txt", e.ToString()); Debug.LogException(e); }
        }
        [MenuItem("DigiPhant/Kart/Create Rainbow Savannah from open scene")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first.");
            if (File.Exists(ScenePath) && !retryFailedBuild) throw new InvalidOperationException("Kart scene already exists; open it rather than overwriting it.");
            var scene = SceneManager.GetActiveScene();
            var controller = Object.FindFirstObjectByType<DigiPhantController>();
            var locomotion = controller ? controller.GetComponent<DigiPhantLocomotion>() : null;
            if (!controller || !locomotion || !locomotion.travelRoot || !locomotion.followCamera) throw new InvalidOperationException("Open the working elephant scene with controls, travel root and camera.");
            string sourcePath = scene.path;
            string backup = AssetDatabase.GenerateUniqueAssetPath("Assets/StudentWork/Scenes/Before-Kart-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity");
            if (!EditorSceneManager.SaveScene(scene, backup, true)) throw new IOException("Could not preserve the current scene.");
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Could not create kart scene copy.");
            // Invoke the supplied tool in the current saved scene, including its unique backup.
            SavannahCourseGenerator.InjectIntoOpenScene();
            string injectionBackup = SavannahCourseGenerator.LastBackupPath;
            SavannahCourseExport.ValidateBundle();
            SavannahCourseValidation.RunAll();
            scene = SceneManager.GetActiveScene();
            controller = Object.FindFirstObjectByType<DigiPhantController>(); locomotion = controller.GetComponent<DigiPhantLocomotion>();
            string preservedControls = EditorJsonUtility.ToJson(controller);
            foreach (var old in Object.FindObjectsByType<PeelPatrolGame>(FindObjectsSortMode.None)) old.gameObject.SetActive(false);
            foreach (var old in Object.FindObjectsByType<SavannaHabitat>(FindObjectsSortMode.None)) old.gameObject.SetActive(false);
            foreach (var old in Object.FindObjectsByType<ElephantLoadout>(FindObjectsSortMode.None)) { old.enabled = false; foreach (Transform child in old.transform) child.gameObject.SetActive(false); }
            foreach (var g in scene.GetRootGameObjects()) if (g.name == "Stage" || g.name.StartsWith("Stage distance grid")) g.SetActive(false);
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            dark = Mat("Midnight chassis", "141D39"); white = Mat("Warm ivory", "FFF5D9"); cyan = Mat("Mint neon", "56FFE0", .8f);
            gold = Mat("Solar gold", "FFD36A", .4f); orange = Mat("Juicy orange", "FF922E", .2f); pink = Mat("Electric pink", "FF579C", .55f);
            root = new GameObject("DIGIPHANT KART / Rainbow Savannah").transform;
            var course = scene.GetRootGameObjects().Single(g => g.name == SavannahCourseGenerator.RootName);
            root.SetPositionAndRotation(course.transform.position, course.transform.rotation);
            var track = root.gameObject.AddComponent<KartTrack>(); track.roadWidth = 8;
            track.points = MakeRoute(); track.gates = Enumerable.Range(0, 18).Select(i => i * 12).ToArray();
            BuildRoad(track); BuildScenery(track, course);
            var race = root.gameObject.AddComponent<KartRace>(); race.controller = controller; race.locomotion = locomotion; race.track = track; race.orangeMaterial = orange;
            locomotion.externalMotion = true; locomotion.followElephant = false; // stageRadius remains serialized unchanged, and is bypassed by physics.
            controller.showControls = false;
            var preview = controller.GetComponent<DigiPhantCameraPreview>(); if (preview) { preview.autoStartBridge = false; preview.showPreview = false; }
            var travel = locomotion.travelRoot;
            foreach (var collider in travel.GetComponentsInChildren<Collider>()) collider.enabled = false;
            var player = AddVehicle(travel.gameObject, race, true, "YOU / DigiPhant", cyan);
            race.player = player;
            race.racers = new KartVehicle[4]; race.racers[0] = player;
            for (int i = 1; i < 4; i++)
            {
                var g = new GameObject(new[] { "", "MANGO", "NOVA", "MINT" }[i]); g.transform.SetParent(root, false);
                var kart = AddVehicle(g, race, false, g.name, i == 1 ? orange : i == 2 ? pink : gold);
                kart.aiLane = (i - 2) * 1.5f; kart.aiPace = .67f + i * .035f;
                BuildRival(g.transform, i == 1 ? orange : i == 2 ? pink : gold);
                race.racers[i] = kart;
            }
            for (int i = 0; i < race.racers.Length; i++)
            {
                int p = i < 2 ? 0 : track.Count - 8;
                race.racers[i].transform.SetPositionAndRotation(track.Point(p) + track.Right(p) * (i % 2 == 0 ? -1.9f : 1.9f) + Vector3.up * .15f,
                    Quaternion.LookRotation(Vector3.ProjectOnPlane(track.Forward(p), Vector3.up)));
            }
            var camera = locomotion.followCamera; camera.orthographic = false; camera.fieldOfView = 60; camera.nearClipPlane = .2f; camera.farClipPlane = 240;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.04f, .05f, .14f);
            var follow = camera.gameObject.AddComponent<KartFollowCamera>(); follow.target = player;
            camera.transform.position = player.transform.position + player.transform.rotation * follow.offset;
            camera.transform.LookAt(player.transform.position + Vector3.up * 2);
            race.refillBays = new[] { Bay(track, 65), Bay(track, 158) };
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.5f, .53f, .7f);
            RenderSettings.fog = true; RenderSettings.fogColor = camera.backgroundColor; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 100; RenderSettings.fogEndDistance = 220;
            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) if (light.type == LightType.Directional) { light.color = new Color(.88f, .92f, 1); light.intensity = 1.4f; light.transform.rotation = Quaternion.Euler(40, -35, 0); }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }.Concat(EditorBuildSettings.scenes.Where(s => s.path != ScenePath)).ToArray();
            Validate();
            File.WriteAllText("Logs/Kart/build.txt", "KART_BUILD_OK\nSource: " + sourcePath + "\nCurrent scene snapshot: " + backup + "\nSupplied injector backup: " + injectionBackup + "\nScene: " + ScenePath + "\nStarter commit: 4e6dab5271c618d1c0bfb2022c41c0d37cd8d69f\nUnity 6000.6.3f1 / URP 17.6.0\nSupplied bundle and generator validations passed.\nOriginal stageRadius=" + locomotion.stageRadius + " preserved; kart Rigidbody owns travel without radial clamp.\n" + track.Count + " road samples; " + track.gates.Length + " ordered gates; 4 physical karts.\n");
            if (SceneView.lastActiveSceneView) SceneView.lastActiveSceneView.Frame(new Bounds(Vector3.zero, new Vector3(85, 30, 85)), false);
            Selection.activeGameObject = root.gameObject;
        }
        static Vector3[] MakeRoute()
        {
            // First eleven anchors follow layout 4; a raised outer return closes it into a circuit.
            var p = new[] { new Vector3(0, .22f, 0), new Vector3(0, .22f, 6), new Vector3(-14, .22f, 6), new Vector3(-19, .22f, 1),
                new Vector3(-19, .22f, -12), new Vector3(-14, .22f, -17), new Vector3(14, .22f, -17), new Vector3(19, .22f, -12),
                new Vector3(19, .22f, 13), new Vector3(14, .22f, 18), new Vector3(-16, .22f, 18), new Vector3(-29, 1, 22),
                new Vector3(-29, 4, 31), new Vector3(30, 7, 31), new Vector3(33, 9, -29), new Vector3(-9, 9, -29), new Vector3(-8, 9, -10), new Vector3(0, 4, -9) };
            var result = new List<Vector3>();
            for (int i = 0; i < p.Length; i++)
                for (int j = 0; j < 12; j++)
                {
                    float t = j / 12f; Vector3 a = p[(i + p.Length - 1) % p.Length], b = p[i], c = p[(i + 1) % p.Length], d = p[(i + 2) % p.Length];
                    result.Add(.5f * ((2 * b) + (-a + c) * t + (2 * a - 5 * b + 4 * c - d) * t * t + (-a + 3 * b - 3 * c + d) * t * t * t));
                }
            return result.Skip(60).Concat(result.Take(60)).ToArray();
        }
        static void BuildRoad(KartTrack track)
        {
            string[] hex = { "FD618E", "FFA65C", "F9DF76", "6CDEAA", "61BEF1", "B388F5" };
            for (int lane = 0; lane < 6; lane++)
            {
                var vertices = new List<Vector3>(); var triangles = new List<int>(); var uv = new List<Vector2>();
                for (int i = 0; i <= track.Count; i++)
                {
                    Vector3 p = track.points[i % track.Count], forward = track.points[(i + 1) % track.Count] - track.points[(i + track.Count - 1) % track.Count];
                    Vector3 side = Vector3.Cross(Vector3.up, forward).normalized;
                    vertices.Add(p + side * (-4 + lane * 8f / 6)); vertices.Add(p + side * (-4 + (lane + 1) * 8f / 6));
                    uv.Add(new Vector2(0, i)); uv.Add(new Vector2(1, i));
                    if (i < track.Count) { int n = i * 2; triangles.AddRange(new[] { n, n + 2, n + 1, n + 1, n + 2, n + 3 }); }
                }
                var mesh = new Mesh { name = "Rainbow lane " + lane }; mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
                string meshPath = Folder + "/Rainbow lane " + lane + ".asset";
                var existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (existingMesh) { Object.DestroyImmediate(mesh); mesh = existingMesh; } else AssetDatabase.CreateAsset(mesh, meshPath);
                var go = new GameObject("Rainbow ribbon / lane " + (lane + 1)); go.transform.SetParent(root, false); go.layer = KartRace.RoadLayer;
                go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = Mat("Ribbon " + lane, hex[lane], .18f);
                go.AddComponent<MeshCollider>().sharedMesh = mesh;
            }
            for (int i = 0; i < track.Count; i++)
            {
                Vector3 a = track.Point(i), b = track.Point(i + 1), side = track.Right(i), nextSide = track.Right(i + 1);
                foreach (float sign in new[] { -1f, 1f })
                {
                    Vector3 from = a + side * sign * 4.25f, to = b + nextSide * sign * 4.25f;
                    var rail = Shape("Physical edge rail", root, PrimitiveType.Cube, (from + to) * .5f + Vector3.up * .65f,
                        new Vector3(.3f, 1.3f, Vector3.Distance(from, to) + .2f), i % 12 < 6 ? cyan : pink, true);
                    rail.rotation = Quaternion.LookRotation(to - from);
                }
                if (i % 8 == 4) Arrow(track, i);
                if (i % 36 == 10)
                    for (int lane = -1; lane <= 1; lane++) Pickup(track.Point(i) + track.Right(i) * lane * 2.1f, lane == 0 ? gold : cyan);
                if (i == 63 || i == 142 || i == 184)
                {
                    var pad = Shape("Boost strip", root, PrimitiveType.Cube, a + Vector3.up * .035f, new Vector3(7, .07f, 2.5f), cyan, false);
                    pad.rotation = Quaternion.LookRotation(track.Forward(i)); var box = pad.gameObject.AddComponent<BoxCollider>(); box.isTrigger = true; box.size = new Vector3(1, 24, 1); box.center = Vector3.up * 10;
                    pad.gameObject.AddComponent<KartBoostPad>();
                }
                if (i > 120 && i % 12 == 0) Shape("Bridge support", root, PrimitiveType.Cylinder, a - Vector3.up * (a.y * .5f), new Vector3(.6f, Mathf.Max(.2f, a.y * .5f), .6f), dark, false);
            }
            for (int gate = 0; gate < track.gates.Length; gate++)
            {
                int i = track.gates[gate]; Vector3 p = track.Point(i), side = track.Right(i);
                foreach (int s in new[] { -1, 1 }) Shape("Gate " + gate + " beacon", root, PrimitiveType.Cylinder, p + side * s * 4.6f + Vector3.up * 2, new Vector3(.2f, 2, .2f), gate == 0 ? gold : cyan, false);
                if (gate == 0)
                {
                    var header = Shape("START FINISH arch", root, PrimitiveType.Cube, p + Vector3.up * 5.8f, new Vector3(10, 1, .5f), dark, false); header.rotation = Quaternion.LookRotation(track.Forward(i));
                    Label("DIGIPHANT KART", p + Vector3.up * 5.75f - track.Forward(i) * .3f, header.rotation, 28, .1f, white.color);
                    for (int col = 0; col < 16; col++) for (int row = 0; row < 2; row++)
                    {
                        var tile = Shape("Checkered start", root, PrimitiveType.Cube, p + side * (-3.75f + col * .5f) + track.Forward(i) * (row * .5f - .25f) + Vector3.up * .02f, new Vector3(.5f, .03f, .5f), (col + row) % 2 == 0 ? white : dark, false);
                        tile.rotation = Quaternion.LookRotation(track.Forward(i));
                    }
                }
            }
            // Physical chicanes leave a full kart-width passage.
            foreach (int i in new[] { 71, 99, 133 })
            {
                var barrel = Shape("Track obstacle / bumper", root, PrimitiveType.Cylinder, track.Point(i) + track.Right(i) * 2.5f + Vector3.up * .65f, new Vector3(1.2f, .65f, 1.2f), gold, true);
            }
        }
        static void Arrow(KartTrack track, int i)
        {
            Vector3 p = track.Point(i) + Vector3.up * .055f; Quaternion direction = Quaternion.LookRotation(track.Forward(i));
            for (int sign = -1; sign <= 1; sign += 2)
            {
                var a = Shape("Direction chevron", root, PrimitiveType.Cube, p + direction * new Vector3(sign * .3f, 0, -.1f), new Vector3(.18f, .035f, 1), white, false);
                a.rotation = direction * Quaternion.Euler(0, sign * -40, 0);
            }
        }
        static void Pickup(Vector3 p, Material mat)
        {
            var go = new GameObject("Orange item box / respawns"); go.transform.SetParent(root); go.transform.position = p;
            var c = go.AddComponent<BoxCollider>(); c.isTrigger = true; c.center = Vector3.up * 1.1f; c.size = new Vector3(1.5f, 2.3f, 1.5f);
            var visual = Shape("Floating item diamond", go.transform, PrimitiveType.Cube, p + Vector3.up * 1.25f, Vector3.one * .9f, mat, false); visual.rotation = Quaternion.Euler(0, 45, 35);
            var fruit = Shape("Orange inside", visual, PrimitiveType.Sphere, p + Vector3.up * 1.25f, Vector3.one * .58f, orange, false);
            fruit.SetParent(go.transform, true); // The orange remains clear above the rotating frame.
            fruit.SetParent(visual, true);
            go.AddComponent<KartPickup>().visual = visual;
        }
        static Transform Bay(KartTrack track, int i)
        {
            Vector3 p = track.Point(i); var pad = Shape("REFILL BAY / right-hand action", root, PrimitiveType.Cube, p + Vector3.up * .04f, new Vector3(7.5f, .05f, 5), dark, false); pad.rotation = Quaternion.LookRotation(track.Forward(i));
            Label("REFILL / RIGHT HAND", p + track.Right(i) * 4.9f + Vector3.up * 2.5f, Quaternion.LookRotation(track.Right(i)), 24, .08f, gold.color);
            for (int n = 0; n < 3; n++) Shape("Refill orange display", root, PrimitiveType.Sphere, p + track.Right(i) * 4.9f + track.Forward(i) * (n - 1) * .7f + Vector3.up * 1.2f, Vector3.one * .65f, orange, false);
            return pad;
        }
        static KartVehicle AddVehicle(GameObject go, KartRace race, bool player, string name, Material color)
        {
            var body = go.GetComponent<Rigidbody>(); if (!body) body = go.AddComponent<Rigidbody>(); body.mass = 180; body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            body.linearDamping = .12f; body.angularDamping = 6; body.centerOfMass = new Vector3(0, .45f, 0);
            var collider = go.AddComponent<CapsuleCollider>(); collider.direction = 2; collider.radius = 1.25f; collider.height = 4.2f; collider.center = new Vector3(0, 1.3f, 0);
            var physics = new PhysicsMaterial("Kart grip") { dynamicFriction = .05f, staticFriction = .05f, bounciness = .12f, frictionCombine = PhysicsMaterialCombine.Minimum };
            AssetDatabase.CreateAsset(physics, Folder + "/" + (player ? "Player" : name) + " grip.physicMaterial"); collider.sharedMaterial = physics;
            var vehicle = go.AddComponent<KartVehicle>(); vehicle.race = race; vehicle.isPlayer = player; vehicle.racerName = name;
            var frame = new GameObject("Kart chassis / physical ride").transform; frame.SetParent(go.transform, false);
            LocalShape("Chassis", frame, PrimitiveType.Cube, new Vector3(0, .6f, 0), new Vector3(2.9f, .55f, 4.6f), color);
            LocalShape("Front bumper", frame, PrimitiveType.Cube, new Vector3(0, .62f, 2.45f), new Vector3(3.2f, .32f, .25f), white);
            LocalShape("Rear bumper", frame, PrimitiveType.Cube, new Vector3(0, .62f, -2.4f), new Vector3(3.2f, .32f, .25f), dark);
            var wheels = new List<Transform>();
            foreach (float x in new[] { -1.55f, 1.55f }) foreach (float z in new[] { -1.5f, 1.5f })
            {
                var wheel = LocalShape("Wheel", frame, PrimitiveType.Cylinder, new Vector3(x, .5f, z), new Vector3(.9f, .22f, .9f), dark); wheel.localRotation = Quaternion.Euler(0, 0, 90); wheels.Add(wheel);
                var hub = LocalShape("Wheel hub", wheel, PrimitiveType.Cylinder, Vector3.up * (x < 0 ? .9f : -.9f), new Vector3(.6f, .08f, .6f), color);
            }
            vehicle.wheels = wheels.ToArray(); return vehicle;
        }
        static void BuildRival(Transform parent, Material color)
        {
            LocalShape("Rival body", parent, PrimitiveType.Sphere, new Vector3(0, 1.6f, -.3f), new Vector3(1.9f, 1.4f, 2.7f), color);
            LocalShape("Elephant head", parent, PrimitiveType.Sphere, new Vector3(0, 2.2f, .9f), new Vector3(1.45f, 1.45f, 1.5f), color);
            foreach (float sign in new[] { -1f, 1f })
            {
                LocalShape("Ear", parent, PrimitiveType.Sphere, new Vector3(sign * 1, 2.15f, .7f), new Vector3(.85f, 1.3f, .25f), color);
                LocalShape("Eye", parent, PrimitiveType.Sphere, new Vector3(sign * .5f, 2.4f, 1.5f), Vector3.one * .18f, dark);
            }
            LocalShape("Trunk", parent, PrimitiveType.Capsule, new Vector3(0, 1.65f, 1.75f), new Vector3(.4f, .65f, .4f), color);
        }
        static void BuildScenery(KartTrack track, GameObject course)
        {
            foreach (var renderer in course.GetComponentsInChildren<MeshRenderer>())
                if (renderer.name.Contains("trunk") || renderer.name.Contains("Trunk"))
                {
                    var g = new GameObject("Savannah tree collider"); g.transform.SetParent(root); g.transform.position = renderer.bounds.center;
                    var c = g.AddComponent<BoxCollider>(); c.size = renderer.bounds.size;
                }
            // Distant luminous stars are deterministic, independent of gameplay randomness.
            var random = new System.Random(20261005);
            for (int i = 0; i < 65; i++)
            {
                float a = (float)random.NextDouble() * Mathf.PI * 2, radius = 65 + (float)random.NextDouble() * 40;
                Shape("Savannah star", root, PrimitiveType.Sphere, new Vector3(Mathf.Sin(a) * radius, 18 + (float)random.NextDouble() * 45, Mathf.Cos(a) * radius), Vector3.one * (.15f + (float)random.NextDouble() * .3f), i % 3 == 0 ? gold : white, false);
            }
        }
        static Material Mat(string name, string hex, float glow = 0)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/" + name + ".mat"); if (existing) return existing;
            ColorUtility.TryParseHtmlString("#" + hex, out var color); var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            m.SetColor("_BaseColor", color); m.SetFloat("_Smoothness", .28f);
            if (glow > 0) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", color * glow); }
            AssetDatabase.CreateAsset(m, Folder + "/" + name + ".mat"); return m;
        }
        static Transform Shape(string name, Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Material material, bool collision)
        {
            var g = GameObject.CreatePrimitive(type); g.name = name; g.transform.SetParent(parent, true); g.transform.position = position; g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = material; if (!collision) Object.DestroyImmediate(g.GetComponent<Collider>()); return g.transform;
        }
        static Transform LocalShape(string name, Transform parent, PrimitiveType type, Vector3 p, Vector3 scale, Material material)
        {
            var result = Shape(name, parent, type, Vector3.zero, scale, material, false); result.localPosition = p; result.localRotation = Quaternion.identity; return result;
        }
        static void Label(string text, Vector3 position, Quaternion rotation, int size, float scale, Color color)
        {
            var g = new GameObject(text); g.transform.SetParent(root); g.transform.SetPositionAndRotation(position, rotation);
            var label = g.AddComponent<TextMesh>(); label.text = text; label.fontSize = size; label.characterSize = scale; label.anchor = TextAnchor.MiddleCenter; label.color = color;
        }
        [MenuItem("DigiPhant/Kart/Validate scene")]
        public static void Validate()
        {
            var race = Object.FindFirstObjectByType<KartRace>();
            if (!race || race.racers.Length != 4 || race.track.points.Length != 216 || race.track.gates.Length != 18) throw new Exception("Race setup incomplete.");
            if (!race.locomotion.externalMotion || race.controller.controls.Length != 9 || race.controller.controls.Any(c => c.bones.Any(b => !b))) throw new Exception("Controller or motion ownership invalid.");
            if (Object.FindObjectsByType<PeelPatrolGame>(FindObjectsSortMode.None).Length != 0 || Object.FindObjectsByType<SavannaHabitat>(FindObjectsSortMode.None).Length != 0) throw new Exception("Old game systems still active.");
            if (Object.FindObjectsByType<KartPickup>(FindObjectsSortMode.None).Length < 12) throw new Exception("Item boxes missing.");
            foreach (var kart in race.racers) if (!kart.GetComponent<Rigidbody>() || !kart.GetComponents<Collider>().Any(c => c.enabled && !c.isTrigger)) throw new Exception("Kart lacks physics.");
            foreach (var g in SceneManager.GetActiveScene().GetRootGameObjects()) foreach (var t in g.GetComponentsInChildren<Transform>(true)) if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) > 0) throw new Exception("Missing script: " + t.name);
            foreach (var renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)) foreach (var material in renderer.sharedMaterials) if (!material || !material.shader || material.shader.name == "Hidden/InternalErrorShader") throw new Exception("Missing material: " + renderer.name);
            Debug.Log("KART_SCENE_VALIDATION_OK / rig, 4 rigidbodies, colliders, items, gates, materials, no active defense");
        }
    }
}
