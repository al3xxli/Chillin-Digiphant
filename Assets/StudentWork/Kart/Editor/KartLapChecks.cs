using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ChillinDigiphant.Kart.Editor
{
    [InitializeOnLoad]
    public static class KartLapChecks
    {
        const string Pending = "KartLapChecks.Pending";
        static KartRace race;
        static KartObstacle obstacle;
        static KartBananaPeel peel;
        static int stage = -1, hits, contacts, releases, throws;
        static float at, stunned;
        static double started, realAt;
        static string report;
        static KartObstacle[] originals;
        static KartObstacle[] generated;
        static KartObstacle[] physicalChecks;
        static int physicalIndex;
        static KartLapChecks() { EditorApplication.update += Poll; }

        [MenuItem("DigiPhant/Kart/Validate lap progression and banana peels")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.isDirty) EditorSceneManager.SaveScene(scene, AssetDatabase.GenerateUniqueAssetPath("Assets/StudentWork/Scenes/Kart-BeforeLapChecks.unity"), true);
            Directory.CreateDirectory("Logs/Kart");
            if (File.Exists("Logs/Kart/lap-error.txt")) File.Delete("Logs/Kart/lap-error.txt");
            EditorSceneManager.OpenScene(KartBuilder.ScenePath);
            KartBuilder.Validate();
            EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView")).Show();
            SessionState.SetBool(Pending, true);
            SessionState.SetFloat("KartLapChecks.Started", (float)EditorApplication.timeSinceStartup);
            EditorApplication.EnterPlaymode();
        }
        static void Require(bool pass, string message)
        {
            if (!pass) throw new Exception(message);
            report += "PASS " + message + "\n";
            File.WriteAllText("Logs/Kart/lap-checks.txt", report);
        }
        static void Next(int value) { stage = value; at = Time.time; realAt = EditorApplication.timeSinceStartup; }
        static void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (!EditorApplication.isPlaying) return;
            if (SessionState.GetBool(Pending, false))
            {
                SessionState.SetBool(Pending, false);
                report = "Lap progression / real Unity triggers, physics, trunk throws and reset\n";
                started = EditorApplication.timeSinceStartup;
                Next(0);
            }
            if (stage < 0) return;
            try
            {
                if (EditorApplication.timeSinceStartup - started > 150) throw new Exception("Timed out at stage " + stage);
                Step();
            }
            catch (Exception error)
            {
                string state = race ? "\nPosition=" + race.player.Body.position + " velocity=" + race.player.Body.linearVelocity + " grounded=" + race.player.Grounded + " crouch=" + race.player.Crouching + " contacts=" + race.player.ObstacleContacts + "/" + contacts + " hits=" + race.player.HitCount + "/" + hits + " ahead=" + (obstacle ? obstacle.Ahead(race.player.Body.position) : 0) : "";
                if (race && obstacle) Capture("lap-failure", obstacle.transform.position);
                File.WriteAllText("Logs/Kart/lap-error.txt", report + "\nStage=" + stage + state + "\n" + error);
                Debug.LogException(error); Finish(1);
            }
        }
        static void Place(Vector3 position, Quaternion rotation)
        {
            race.locomotion.StopMotion(); race.testCrouch = false;
            race.player.SetInput(0, 0);
            race.player.Body.position = position; race.player.Body.rotation = rotation;
            race.player.Body.linearVelocity = race.player.Body.angularVelocity = Vector3.zero;
            race.player.transform.SetPositionAndRotation(position, rotation);
            Physics.SyncTransforms();
            race.locomotion.followCamera.GetComponent<KartFollowCamera>().Snap();
        }
        static void CompleteLap(KartVehicle kart)
        {
            for (int n = 0; n < race.track.gates.Length; n++)
            {
                int sample = race.track.gates[kart.NextGate];
                Vector3 center = race.track.Point(sample), forward = race.track.Forward(sample);
                kart.CheckNavigation(center - forward, center + forward);
            }
        }
        static void Approach(KartObstacle target)
        {
            obstacle = target;
            Place(target.transform.TransformPoint(new Vector3(0, .18f, -8)), Quaternion.LookRotation(Vector3.ProjectOnPlane(target.transform.forward, Vector3.up)));
            race.testCrouch = target.kind == KartObstacleKind.Duck;
        }
        static void Drive()
        {
            race.locomotion.testForward = .75f;
            race.player.Body.linearVelocity = race.player.Body.rotation * Vector3.forward * 16;
        }
        static void Step()
        {
            float elapsed = Time.time - at;
            if (stage == 0)
            {
                // The serialized scene starts in Camera mode, which can freeze scaled time.
                if (EditorApplication.timeSinceStartup - realAt < .5) return;
                race = Object.FindAnyObjectByType<KartRace>(); race.SetMode(1); race.ResetRace();
                originals = race.obstacles.ToArray();
                generated = race.track.GetComponentsInChildren<KartObstacle>(true).Except(originals).ToArray();
                Require(originals.Length == 5 && race.CourseLap == 1 && generated.Length == 8 && generated.All(o => !o.gameObject.activeSelf), "Practice and lap one preserve all five original obstacles; later hazards are hidden");
                Require(generated.Count(o => o.kind == KartObstacleKind.Slalom) == 0, "No new steering or slalom obstacles");
                foreach (var rival in race.racers.Skip(1)) rival.gameObject.SetActive(false);
                race.StartRace(); Next(1);
            }
            else if (stage == 1)
            {
                if (!race.IsRacing) return;
                CompleteLap(race.racers[1]);
                Require(race.CourseLap == 1 && race.obstacles.Length == 5, "Rival lap completion cannot escalate the human team's course");
                var waiting = generated.First(o => o.trackSample == 58);
                var rival = race.racers[1]; rival.gameObject.SetActive(true); rival.Body.position = waiting.transform.position;
                rival.transform.position = waiting.transform.position; Physics.SyncTransforms();
                CompleteLap(race.player);
                Require(race.CourseLap == 2 && !waiting.gameObject.activeSelf, "Human lap two activates progression but defers a collider occupied by a rival");
                rival.gameObject.SetActive(false); Next(2);
            }
            else if (stage == 2)
            {
                if (elapsed < .1f) return;
                Require(race.obstacles.Length == 9 && race.obstacles.Count(o => o.kind == KartObstacleKind.Jump) == 2 && race.obstacles.Count(o => o.kind == KartObstacleKind.Duck) == 2 && Object.FindObjectsByType<KartBananaPeel>(FindObjectsSortMode.None).Length == 6, "Lap two has two logs, two canopies, original slalom and six banana peels");
                CompleteLap(race.player); Next(3);
            }
            else if (stage == 3)
            {
                if (elapsed < .1f) return;
                Require(race.CourseLap == 3 && race.obstacles.Length == 13 && race.obstacles.Count(o => o.kind == KartObstacleKind.Jump) == 3 && race.obstacles.Count(o => o.kind == KartObstacleKind.Duck) == 3 && Object.FindObjectsByType<KartBananaPeel>(FindObjectsSortMode.None).Length == 12, "Lap three keeps lap two additions and adds another log, canopy and six peels");
                Require(race.player.Collect(1) && race.player.Collect(1) && race.player.Collect(1) && race.player.Ammo == 3 && !race.player.Collect() && !race.player.Collect(-1), "Three pickups fill storage; overflow and negative collection cannot change it");
                releases = race.PlayerTrunk.ReleaseCount; throws = 0; Next(4);
            }
            else if (stage == 4)
            {
                if (!race.player.CanThrow) return;
                Require(race.RequestThrow() && race.player.Ammo == 2 - throws, "Trunk throw " + (throws + 1) + " consumes exactly one stored orange");
                throws++; Next(5);
            }
            else if (stage == 5)
            {
                if (elapsed < 1.1f) return;
                Require(race.PlayerTrunk.ReleaseCount == releases + throws, "Stored orange " + throws + " releases as an actual trunk projectile");
                if (throws < 3) { Next(4); return; }
                Require(race.player.Ammo == 0 && !race.RequestThrow(), "All three stored oranges can be thrown, and an empty basket cannot throw");
                race.player.Collect(3);
                Require(race.RequestThrow() && race.player.Ammo == 2, "A new throw reserves one orange before a banana interruption");
                peel = Object.FindObjectsByType<KartBananaPeel>(FindObjectsSortMode.None).First(p => p.GetComponentInParent<KartObstacle>().trackSample == 115 && Mathf.Abs(p.transform.localPosition.x) < 1);
                hits = race.player.HitCount;
                Place(peel.transform.position + Vector3.up * .15f, peel.transform.rotation);
                Next(6);
            }
            else if (stage == 6)
            {
                if (elapsed < .15f) return;
                Require(race.player.HitCount == hits + 1 && race.player.BananaHit && race.player.StunRemaining > .5f && race.player.GetComponent<KartFeedback>().HitVisualActive, "Driving over a peel triggers one dizzy crash with stars and a short stun");
                Require(!peel.Available && !peel.GetComponent<Collider>().enabled && !race.RequestThrow() && race.player.Ammo == 3, "Peel is consumed, dizzy throwing is blocked, and stored oranges are preserved");
                Capture("banana-slip", peel.transform.position);
                ScreenCapture.CaptureScreenshot("Logs/Kart/orange-storage.png");
                race.TogglePause(); stunned = race.player.StunRemaining; Next(7);
            }
            else if (stage == 7)
            {
                if (EditorApplication.timeSinceStartup - realAt < .4) return;
                Require(Mathf.Abs(stunned - race.player.StunRemaining) < .06f, "Pause freezes the dizzy recovery timer");
                race.TogglePause();
                Place(race.track.Point(2) + Vector3.up * .2f, Quaternion.LookRotation(race.track.Forward(2)));
                Next(8);
            }
            else if (stage == 8)
            {
                if (elapsed < 1.3f) return;
                Require(race.player.StunRemaining == 0 && !race.player.GetComponent<KartFeedback>().HitVisualActive && race.player.CanThrow, "Dizzy animation clears and normal throwing resumes automatically");
                Next(9);
            }
            else if (stage == 9)
            {
                if (elapsed < 4.1f) return;
                Require(peel.Available && peel.GetComponent<Collider>().enabled, "Consumed banana peel returns after five seconds");
                physicalChecks = generated.Where(o => o.kind == KartObstacleKind.Jump || o.kind == KartObstacleKind.Duck || o.kind == KartObstacleKind.Banana).ToArray();
                // Check the adjusted downhill canopy still requires ducking.
                Approach(generated.First(o => o.trackSample == 181)); race.testCrouch = false; Next(14);
            }
            else if (stage == 14)
            {
                if (elapsed < .35f) return;
                contacts = race.player.ObstacleContacts; Drive(); Next(15);
            }
            else if (stage == 15)
            {
                if (elapsed < 1.2f) return;
                Require(race.player.ObstacleContacts > contacts, "Lap-three canopy still blocks an upright kart");
                physicalIndex = 0; Approach(physicalChecks[physicalIndex]); Next(10);
            }
            else if (stage == 10)
            {
                if (elapsed < .35f) return;
                hits = race.player.HitCount; contacts = race.player.ObstacleContacts;
                if (obstacle.kind == KartObstacleKind.Duck) Require(race.player.Crouching, "Crouch is established before approaching added canopy " + obstacle.trackSample);
                Drive();
                if (obstacle.kind != KartObstacleKind.Duck) Require(race.player.Jump(), "Jump accepted at added " + obstacle.kind + " sample " + obstacle.trackSample);
                Next(11);
            }
            else if (stage == 11)
            {
                if (elapsed < 1.2f) return;
                Require(obstacle.Ahead(race.player.Body.position) < -3 && race.player.ObstacleContacts == contacts && race.player.HitCount == hits, "Physical " + (obstacle.kind == KartObstacleKind.Duck ? "duck" : "jump") + " clears added " + obstacle.kind + " at sample " + obstacle.trackSample);
                Capture("lap-obstacle-" + obstacle.trackSample, obstacle.transform.position);
                physicalIndex++;
                if (physicalIndex < physicalChecks.Length) { Approach(physicalChecks[physicalIndex]); Next(10); return; }
                CompleteLap(race.player);
                Require(race.player.Finished && race.CourseLap == 3, "Finish stays at lap three without spawning a fourth set");
                race.ResetRace();
                Require(race.CourseLap == 1 && race.obstacles.SequenceEqual(originals) && generated.All(o => !o.gameObject.activeSelf) && race.player.Ammo == 0 && race.player.StunRemaining == 0, "Reset restores original course, empty storage and cleared dizziness");
                race.StartRace(); Next(12);
            }
            else if (stage == 12)
            {
                if (!race.IsRacing) return;
                CompleteLap(race.player); Next(13);
            }
            else if (stage == 13)
            {
                if (elapsed < .2f) return;
                Require(race.obstacles.Length == 9 && race.track.GetComponentsInChildren<KartObstacle>(true).Length == originals.Length + generated.Length, "Replay reuses the same later-lap objects without duplication");
                report += "KART_LAP_VALIDATION_OK\n";
                File.WriteAllText("Logs/Kart/lap-checks.txt", report); Finish(0);
            }
        }
        static void Capture(string name, Vector3 target)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            var camera = race.locomotion.followCamera;
            Vector3 position = camera.transform.position; Quaternion rotation = camera.transform.rotation;
            Rect rect = camera.rect; var previousTarget = camera.targetTexture; var previousActive = RenderTexture.active;
            var texture = new RenderTexture(1200, 800, 24); var pixels = new Texture2D(1200, 800, TextureFormat.RGB24, false);
            camera.transform.position = target - race.track.Forward(race.track.Nearest(target)) * 10 + Vector3.up * 7;
            camera.transform.LookAt(target + Vector3.up);
            camera.rect = new Rect(0, 0, 1, 1); camera.targetTexture = texture; camera.Render();
            RenderTexture.active = texture; pixels.ReadPixels(new Rect(0, 0, 1200, 800), 0, 0); pixels.Apply();
            File.WriteAllBytes("Logs/Kart/" + name + ".png", pixels.EncodeToPNG());
            camera.targetTexture = previousTarget; RenderTexture.active = previousActive; camera.rect = rect;
            camera.transform.SetPositionAndRotation(position, rotation);
            Object.DestroyImmediate(pixels); Object.DestroyImmediate(texture);
        }
        static void Finish(int code)
        {
            stage = -1;
            if (Application.isBatchMode || Environment.GetCommandLineArgs().Contains("-digiphantLapChecksExit")) EditorApplication.Exit(code);
            else { if (race) { race.ResetRace(); race.SelectKeys(); } EditorApplication.ExitPlaymode(); }
        }
    }
}
