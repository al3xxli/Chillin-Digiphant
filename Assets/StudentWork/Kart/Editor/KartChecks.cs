using System;
using System.IO;
using System.Linq;
using DigiPhant;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ChillinDigiphant.Kart.Editor
{
    [InitializeOnLoad]
    public static class KartChecks
    {
        static double nextPoll, stageAt;
        static int stage = -1, collisions;
        static Vector3 start;
        static Quaternion rotation;
        static string report = "";
        static KartRace game;
        static KartChecks() { EditorApplication.update += Update; }
        static void Require(bool ok, string message) { if (!ok) throw new Exception(message); report += "PASS " + message + "\n"; File.WriteAllText("Logs/Kart/play-checks.txt", report); }
        [MenuItem("DigiPhant/Kart/Run Play mode validation")]
        public static void Run()
        {
            Application.runInBackground = true;
            if (!EditorApplication.isPlaying)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                if (scene.isDirty) EditorSceneManager.SaveScene(scene, AssetDatabase.GenerateUniqueAssetPath("Assets/StudentWork/Scenes/Kart-BeforeValidation.unity"), true);
                EditorSceneManager.OpenScene(KartBuilder.ScenePath);
            }
            KartBuilder.Validate(); SessionState.SetBool("KartValidationPending", true);
            var view = EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView")); view.Show(); view.Focus();
            if (!EditorApplication.isPlaying) EditorApplication.EnterPlaymode();
        }
        static void Update()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (EditorApplication.timeSinceStartup >= nextPoll)
            {
                nextPoll = EditorApplication.timeSinceStartup + .5;
                if (File.Exists(".tools/test-kart")) { File.Delete(".tools/test-kart"); try { Run(); } catch (Exception e) { Fail(e); } }
                if (File.Exists(".tools/drive-kart")) { File.Delete(".tools/drive-kart"); SessionState.SetBool("KartDriveOnly", true); try { Run(); } catch (Exception e) { Fail(e); } }
                if (File.Exists(".tools/capture-kart") && EditorApplication.isPlaying)
                {
                    File.Delete(".tools/capture-kart"); ScreenCapture.CaptureScreenshot("Logs/Kart/game-view.png");
                }
                if (File.Exists(".tools/stop-kart-play") && EditorApplication.isPlaying) { File.Delete(".tools/stop-kart-play"); EditorApplication.ExitPlaymode(); }
            }
            if (!EditorApplication.isPlaying) { stage = -1; return; }
            if (SessionState.GetBool("KartValidationPending", false))
            {
                SessionState.SetBool("KartValidationPending", false); stage = 0; stageAt = EditorApplication.timeSinceStartup;
                report = SessionState.GetBool("KartDriveOnly", false) && File.Exists("Logs/Kart/play-checks.txt") ? File.ReadAllText("Logs/Kart/play-checks.txt") : "Unity Play-mode checks using real FixedUpdate and physics\n";
            }
            if (stage < 0 || EditorApplication.timeSinceStartup - stageAt < 1.5) return;
            try { Step(); } catch (Exception e) { Fail(e); }
        }
        static void Move(KartVehicle kart, int point, float lane = 0)
        {
            kart.Body.position = game.track.Point(point) + game.track.Right(point) * lane + Vector3.up * .15f;
            kart.Body.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(game.track.Forward(point), Vector3.up));
            kart.transform.SetPositionAndRotation(kart.Body.position, kart.Body.rotation);
            Physics.SyncTransforms();
            kart.Body.linearVelocity = kart.Body.angularVelocity = Vector3.zero;
        }
        static void Next() { stage++; stageAt = EditorApplication.timeSinceStartup; }
        static void Step()
        {
            if (stage == 0)
            {
                game = Object.FindAnyObjectByType<KartRace>(); Require(game != null, "Kart scene runs without a missing game component");
                CheckMediaPipeContract();
                game.SetMode(1); game.ResetRace();
                if (SessionState.GetBool("KartDriveOnly", false))
                {
                    SessionState.SetBool("KartDriveOnly", false); game.totalLaps = 1; game.player.isPlayer = false; game.player.aiLane = -1.2f; game.player.aiPace = .75f;
                    game.StartRace(); stage = 8; stageAt = EditorApplication.timeSinceStartup; return;
                }
                foreach (var rival in game.racers.Skip(1)) rival.gameObject.SetActive(false);
                Move(game.player, 1); start = game.player.Body.position; rotation = game.player.Body.rotation;
                game.locomotion.testForward = 1; Next();
            }
            else if (stage == 1)
            {
                Require(game.player.Grounded && game.player.Speed > 4 && Vector3.Distance(start, game.player.Body.position) > 2, "Acceleration and gravity keep the Rigidbody on the road");
                Require(Vector3.Distance(game.locomotion.followCamera.transform.position, game.player.transform.position) < 22, "Follow camera stays with the moving kart");
                game.locomotion.testSteering = .6f; Next();
            }
            else if (stage == 2)
            {
                Require(Quaternion.Angle(rotation, game.player.Body.rotation) > 10, "Steering turns the physical vehicle");
                game.locomotion.StopMotion();
                var item = Object.FindObjectsByType<KartPickup>().First();
                game.player.Body.position = item.transform.position + Vector3.up * .15f; game.player.Body.linearVelocity = Vector3.zero;
                Next();
            }
            else if (stage == 3)
            {
                Require(game.player.Ammo > 0, "Driving through a pickup trigger awards an orange");
                Require(game.refillBays.Length==0, "Obsolete refill bays are removed from the active race");
                Move(game.player, 1); var rival = game.racers[1]; rival.gameObject.SetActive(true); rival.enabled = false;
                rival.Body.position = game.player.Body.position + (game.player.Body.rotation * Vector3.forward) * 9; rival.Body.rotation = game.player.Body.rotation; rival.Body.linearVelocity = Vector3.zero;
                rival.transform.SetPositionAndRotation(rival.Body.position, rival.Body.rotation); Physics.SyncTransforms();
                int ammo = game.player.Ammo; Require(game.RequestThrow() && game.player.Ammo == ammo - 1 && !game.RequestThrow(), "Throw consumes one item and respects its one-second cooldown");
                Next();
            }
            else if (stage == 4)
            {
                Require(game.racers[1].StunRemaining > 0, "Thrown Rigidbody orange collides with another kart and applies a hit (target " + game.racers[1].Body.position + ", shooter " + game.player.Body.position + ")");
                Move(game.player, 1); game.racers[1].Body.position = game.player.Body.position + game.player.transform.forward * 5;
                collisions = game.player.CollisionCount; game.locomotion.testForward = 1; Next();
            }
            else if (stage == 5)
            {
                Require(game.player.CollisionCount > collisions, "Kart-to-kart collision is resolved by physics");
                game.racers[1].gameObject.SetActive(false); Move(game.player, 1); collisions = game.player.CollisionCount;
                game.player.Body.rotation = Quaternion.LookRotation(game.track.Right(1)); game.locomotion.testForward = 1; Next();
            }
            else if (stage == 6)
            {
                Require(game.player.CollisionCount > collisions, "Environment rail collision prevents driving through the boundary");
                game.locomotion.StopMotion(); game.player.Body.linearVelocity = Vector3.zero;
                game.player.Body.position = new Vector3(0, -30, 0); Next();
            }
            else if (stage == 7)
            {
                Require(game.player.Body.position.y > -2, "Falling below the course recovers to the last checkpoint");
                var p = game.player; var t = game.track; game.ResetRace();
                Vector3 line = t.Point(0), normal = t.Forward(0);
                p.CheckNavigation(line - normal, line + normal); Require(p.CompletedLaps == 0 && p.NextGate == 1, "Finish line alone cannot award a lap");
                line = t.Point(t.gates[1]); normal = t.Forward(t.gates[1]);
                p.CheckNavigation(line + normal, line - normal); Require(p.NextGate == 1, "Reverse checkpoint crossing is rejected");
                for (int lap = 0; lap < game.totalLaps; lap++) for (int g = 0; g < t.gates.Length; g++)
                {
                    int gate = p.NextGate; line = t.Point(t.gates[gate]); normal = t.Forward(t.gates[gate]); p.CheckNavigation(line - normal, line + normal);
                }
                Require(p.Finished && p.CompletedLaps == 3 && game.Phase == RacePhase.Finished, "Ordered checkpoints complete exactly three laps and finish the race");
                game.ResetRace(); Require(!p.Finished && p.CompletedLaps == 0 && p.Ammo == 0, "Replay resets laps, inventory and finished state");
                game.SetMode(0); game.RefreshTracking(Time.realtimeSinceStartup); Require(game.TrackingBlocked, "Uncalibrated camera input blocks movement");
                game.SetMode(1); Require(!game.TrackingBlocked, "Keyboard and sliders resume without camera calibration");
                foreach (var rival in game.racers.Skip(1)) { rival.gameObject.SetActive(true); rival.enabled = true; }
                game.totalLaps = 1; game.player.isPlayer = false; game.player.aiLane = -1.2f; game.player.aiPace = .75f;
                game.StartRace(); Next();
            }
            else if (stage == 8)
            {
                File.WriteAllText("Logs/Kart/race-progress.txt", "CanDrive=" + game.CanDrive + " elapsed=" + game.Elapsed + "\n" + string.Join("\n", game.racers.Select(k => k.racerName + " lap=" + k.CompletedLaps + " next=" + k.NextGate + " point=" + k.NearestPoint + " pos=" + k.transform.position + " speed=" + k.Speed.ToString("F1") + " grounded=" + k.Grounded + " throttle=" + k.Throttle + " steer=" + k.Steering + " contact=" + k.LastContact)));
                if (!game.player.Finished)
                {
                    if (game.Elapsed > 180) throw new Exception("AI full-circuit physical drive did not finish within 180 seconds; inspect race-progress.txt");
                    return;
                }
                Require(true, "AI physically completes the full circuit through every checkpoint, with collision recovery enabled");
                game.player.isPlayer = true; game.totalLaps = 3; game.SetMode(2); game.ResetRace();
                report += "KART_PLAY_VALIDATION_OK\nLive webcam gestures and mirror orientation require physical performers.\n";
                report = string.Join("\n", report.Split('\n').Distinct());
                File.WriteAllText("Logs/Kart/play-checks.txt", report); Debug.Log("KART_PLAY_VALIDATION_OK"); stage = -1;
                ScreenCapture.CaptureScreenshot("Logs/Kart/game-view.png");
            }
        }
        static void Fail(Exception e)
        {
            stage = -1; SessionState.SetBool("KartValidationPending", false); Time.timeScale = 1;
            File.WriteAllText("Logs/Kart/play-error.txt", report + e); Debug.LogException(e);
        }
        static void CheckMediaPipeContract()
        {
            game.SetCameraLayout(KartCameraLayout.PoseTeam);game.SetLiteralActions(false);
            game.SetMode(0); float now = Time.realtimeSinceStartup; var c = game.controller;
            var frame = new PoseFrame { version = 1, performerCount = c.performerCount, upperBodyOnly = c.upperBodyOnly,
                previewFormat = 2, mirrorPreview = game.Preview.mirrorPreview,
                people = Enumerable.Range(1, c.performerCount).Select(i => new PerformerFrame { slot = i, values = new float[6], confidence = Enumerable.Repeat(1f, 6).ToArray(), handForward = new float[2], handForwardConfidence = new[] { 1f, 1f } }).ToArray() };
            Require(c.AcceptPacket(JsonUtility.ToJson(frame), now) && c.CalibrateAt(now), "Synthetic MediaPipe packet and neutral calibration retain the existing protocol");
            frame.people[0].values[0] = 1;
            frame.people[Mathf.Min(2, c.performerCount) - 1].values[1] = 1;
            frame.people[c.performerCount - 1].values[0] = 1;
            frame.people[c.performerCount - 1].confidence[1] = 0;
            c.AcceptPacket(JsonUtility.ToJson(frame), now);
            Require(game.locomotion.forward.TryRead(c, now, out float travel) && travel > .5f && game.locomotion.steering.TryRead(c, now, out float steering) && steering > .5f,
                "P1 forward and P2 steering remain available simultaneously");
            Require(c.TryReadMovement(c.performerCount, Movement.LeftHandHeight, now, out float toss) && toss > .5f && !c.TryReadMovement(c.performerCount, Movement.RightHandHeight, now, out _),
                "P3 LEFT throw channel remains valid when only the RIGHT wrist is missing");
            Vector3 before = game.player.transform.position;
            game.locomotion.enableLocomotion = true;
            c.ApplyControls(now, .1f);
            Require(Vector3.Distance(before, game.player.transform.position) < .0001f, "Elephant rig evaluation leaves physical kart translation to the Rigidbody");
            Require(!game.locomotion.forward.TryRead(c, now + 3, out _), "Stale camera movement stops driving rather than reusing an old pose");
            game.SetMode(1);
        }
    }
}

