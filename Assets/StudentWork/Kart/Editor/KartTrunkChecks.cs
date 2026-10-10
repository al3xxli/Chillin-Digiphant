using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ChillinDigiphant.Kart.Editor
{
    [InitializeOnLoad]
    public static class KartTrunkChecks
    {
        static int stage = -1, releases, hitCount;
        static float at, pausedTime;
        static double started;
        static KartRace game;
        static KartVehicle target;
        static string report;
        static Vector3 restTip;
        static KartTrunkChecks() { EditorApplication.update += Poll; }
        static void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (File.Exists(".tools/test-kart-trunk") && !EditorApplication.isPlayingOrWillChangePlaymode && !File.Exists(".tools/refresh-kart"))
            {
                File.Delete(".tools/test-kart-trunk");
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                if (scene.isDirty) EditorSceneManager.SaveScene(scene, AssetDatabase.GenerateUniqueAssetPath("Assets/StudentWork/Scenes/Kart-BeforeTrunkChecks.unity"), true);
                EditorSceneManager.OpenScene(KartBuilder.ScenePath);
                SessionState.SetBool("KartTrunkChecksPending", true);
                var view = EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView")); view.Show(); view.Focus();
                EditorApplication.EnterPlaymode();
            }
            if (!EditorApplication.isPlaying) stage = -1;
            else if (SessionState.GetBool("KartTrunkChecksPending", false))
            {
                SessionState.SetBool("KartTrunkChecksPending", false);
                game = Object.FindAnyObjectByType<KartRace>(); game.SetMode(1); game.ResetRace();
                report = "Trunk throw: real imported bones, Unity physics, rendered rear camera.\n";
                started = EditorApplication.timeSinceStartup; Next(0);
            }
            if (stage >= 0)
            {
                try
                {
                    if (EditorApplication.timeSinceStartup - started > 25) throw new System.Exception("Trunk checks timed out at stage " + stage);
                    Step();
                }
                catch (System.Exception error)
                {
                    Directory.CreateDirectory("Logs/Kart"); File.WriteAllText("Logs/Kart/trunk-error.txt", report + error);
                    Debug.LogException(error); stage = -1;
                }
            }
            if (!File.Exists(".tools/audit-kart-trunk")) return;
            File.Delete(".tools/audit-kart-trunk");
            var race = Object.FindAnyObjectByType<KartRace>();
            var output = new StringBuilder();
            output.AppendLine("Playing=" + Application.isPlaying + " root=" + race.player.name + " scale=" + race.player.transform.lossyScale);
            foreach (var t in race.player.GetComponentsInChildren<Transform>().Where(t => t.name.Contains("Trunk") || t.name.Contains("Head")))
                output.AppendLine(t.name + " parent=" + t.parent.name + " playerLocal=" + race.player.transform.InverseTransformPoint(t.position).ToString("F3") + " local=" + t.localPosition.ToString("F3") + " right=" + race.player.transform.InverseTransformDirection(t.right).ToString("F3") + " up=" + race.player.transform.InverseTransformDirection(t.up).ToString("F3") + " forward=" + race.player.transform.InverseTransformDirection(t.forward).ToString("F3") + " children=" + t.childCount);
            foreach (var r in race.player.GetComponentsInChildren<SkinnedMeshRenderer>())
                output.AppendLine("SKIN " + r.name + " bounds=" + r.bounds + " local=" + r.localBounds + " scale=" + r.transform.lossyScale);
            output.AppendLine("camera=" + race.locomotion.followCamera.transform.position + " offset=" + race.locomotion.followCamera.GetComponent<KartFollowCamera>().offset);
            Directory.CreateDirectory("Logs/Kart"); File.WriteAllText("Logs/Kart/trunk-rig.txt", output.ToString());
        }
        [MenuItem("DigiPhant/Kart/Validate player trunk throws")]
        static void Request() { Directory.CreateDirectory(".tools"); File.WriteAllText(".tools/test-kart-trunk", ""); }
        static void Next(int next) { stage = next; at = Time.realtimeSinceStartup; }
        static void Check(bool ok, string message)
        {
            if (!ok) throw new System.Exception(message);
            report += "PASS " + message + "\n";
            Directory.CreateDirectory("Logs/Kart"); File.WriteAllText("Logs/Kart/trunk-checks.txt", report);
        }
        static void Step()
        {
            float elapsed = Time.realtimeSinceStartup - at;
            var trunk = game.PlayerTrunk;
            if (stage == 0)
            {
                if (elapsed < 1.2f) return;
                Check(trunk && trunk.Tip && !trunk.Busy, "Player uses the existing seven-bone elephant trunk and native tip");
                Check(!game.RequestThrow() && !trunk.Busy, "An empty basket cannot start a trunk throw");
                restTip = trunk.Tip.position;
                game.player.Collect(3);
                target = game.racers[1]; target.enabled = false;
                target.Body.position = game.player.Body.position + game.player.transform.forward * 10;
                target.Body.rotation = game.player.Body.rotation;
                target.Body.linearVelocity = Vector3.zero;
                target.transform.SetPositionAndRotation(target.Body.position, target.Body.rotation); Physics.SyncTransforms(); hitCount = target.HitCount;
                Check(game.RequestThrow() && trunk.HoldingOrange && game.player.Ammo == 2, "Throw starts a visible held orange and reserves exactly one item");
                Check(!game.RequestThrow() && Object.FindObjectsByType<KartProjectile>().Length == 0, "Wind-up cannot launch early or accept a duplicate throw");
                Next(1);
            }
            else if (stage == 1)
            {
                if (trunk.AnimationTime < .38f) return;
                Check(trunk.HoldingOrange && Vector3.Distance(restTip, trunk.Tip.position) > 1.5f, "Native trunk lifts the held orange before release");
                Vector3 view = game.locomotion.followCamera.WorldToViewportPoint(trunk.GripPosition);
                Check(view.z > 0 && view.x > .08f && view.x < .92f && view.y > .12f && view.y < .9f, "Raised orange is inside the rear camera framing");
                ScreenCapture.CaptureScreenshot("Logs/Kart/trunk-windup.png"); Next(2);
            }
            else if (stage == 2)
            {
                if (trunk.AnimationTime < .54f) return;
                ScreenCapture.CaptureScreenshot("Logs/Kart/trunk-flick.png"); Next(3);
            }
            else if (stage == 3)
            {
                if (trunk.ReleaseCount == 0) return;
                Check(trunk.LastProjectile && !trunk.HoldingOrange && trunk.LastProjectile.owner == game.player, "The orange becomes a player-owned physical projectile at trunk release");
                float travel = trunk.LastProjectile.GetComponent<Rigidbody>().linearVelocity.magnitude * Time.deltaTime * 3 + .2f;
                Check(Vector3.Distance(trunk.LastProjectile.transform.position, trunk.LastReleasePosition) < travel, "Projectile begins at the animated trunk grip");
                Vector3 oldOrigin = game.player.Body.position + game.player.transform.forward * 3.4f + Vector3.up * 1.6f;
                Check(Vector3.Distance(trunk.LastReleasePosition, oldOrigin) > 1, "Player no longer launches from the old fixed front position");
                CheckForwardRelease();
                ScreenCapture.CaptureScreenshot("Logs/Kart/trunk-release.png"); Next(31);
            }
            else if (stage == 31)
            {
                if (elapsed < .12f) return;
                ScreenCapture.CaptureScreenshot("Logs/Kart/trunk-flight.png"); Next(4);
            }
            else if (stage == 4)
            {
                if (elapsed < .9f) return;
                Check(target.HitCount > hitCount, "Trunk-launched orange physically hits a kart ahead");
                Check(!trunk.Busy && trunk.ReleaseCount == 1, "One animation releases exactly one orange and returns control to the rig");
                game.ResetRace(); game.SetMode(1); game.player.Collect(3);
                Check(game.RequestThrow(), "A new throw can start after reset"); Next(5);
            }
            else if (stage == 5)
            {
                if (elapsed < .18f) return;
                pausedTime = trunk.AnimationTime; releases = trunk.ReleaseCount;
                game.TogglePause(); Next(6);
            }
            else if (stage == 6)
            {
                if (elapsed < .35f) return;
                Check(Mathf.Abs(trunk.AnimationTime - pausedTime) < .025f && trunk.ReleaseCount == releases, "Pause freezes wind-up without firing an orange");
                game.TogglePause(); game.SelectKeys();
                Check(!trunk.Busy && game.player.Ammo == 3, "Switching controls cancels wind-up and returns the reserved orange"); Next(7);
            }
            else if (stage == 7)
            {
                if (elapsed < .8f) return;
                Check(trunk.ReleaseCount == releases && Object.FindObjectsByType<KartProjectile>().Length == 0, "A cancelled throw cannot fire later");
                game.SetMode(1); game.player.Collect(3); Check(game.RequestThrow(), "Another throw can begin after cancellation");
                game.ResetRace(); Next(8);
            }
            else if (stage == 8)
            {
                if (elapsed < 1.2f) return;
                Check(!trunk.Busy && game.player.Ammo == 0 && trunk.ReleaseCount == releases, "Race reset clears held oranges without phantom launches or ammo refunds");
                target.Collect(3);
                Vector3 expected = target.Body.position + target.transform.forward * 3.4f + Vector3.up * 1.6f;
                Check(target.Throw() && target.Ammo == 2, "Bots still throw immediately with their existing cooldown");
                var shot = Object.FindObjectsByType<KartProjectile>().Single(p => p.owner == target);
                Check(Vector3.Distance(shot.transform.position, expected) < .001f && !target.GetComponent<KartTrunkThrow>(), "Bot launch position and lack of trunk animation remain unchanged");
                target.enabled = true; game.ResetRace();
                game.player.Body.rotation *= Quaternion.Euler(0, 90, 0);
                game.player.transform.SetPositionAndRotation(game.player.Body.position, game.player.Body.rotation);
                Physics.SyncTransforms(); game.locomotion.followCamera.GetComponent<KartFollowCamera>().Snap(); Next(9);
            }
            else if (stage == 9)
            {
                if (elapsed < .3f) return;
                game.player.Collect(3); releases = trunk.ReleaseCount;
                Check(game.RequestThrow(), "Trunk throw starts after the kart changes heading"); Next(10);
            }
            else if (stage == 10)
            {
                if (trunk.ReleaseCount == releases) return;
                CheckForwardRelease();
                game.SelectKeys(); game.ResetRace();
                report += "KART_TRUNK_VALIDATION_OK\n"; File.WriteAllText("Logs/Kart/trunk-checks.txt", report); stage = -1;
            }
        }
        static void CheckForwardRelease()
        {
            var first = game.controller.controls.First(c => c.label == "Trunk curl").bones[0];
            Vector3 offset = game.player.transform.InverseTransformVector(game.PlayerTrunk.LastReleasePosition - first.position);
            Check(offset.z > .8f && offset.y > .65f && offset.x > .2f,
                "Trunk flick stays forward, raised and beside the head at kart yaw " + game.player.transform.eulerAngles.y.ToString("F0"));
        }
    }
}
