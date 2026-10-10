using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

namespace ChillinDigiphant.Kart.Editor
{
    // Exercises the real Update -> Input System -> Rigidbody path without requiring a webcam.
    [InitializeOnLoad]
    public static class KartKeyboardChecks
    {
        static int stage = -1;
        static float at;
        static double started;
        static KartRace race;
        static Keyboard keyboard;
        static Vector3 position;
        static Quaternion rotation;
        static string report;
        static bool pausedBeforeSwitch;
        static KartKeyboardChecks() { EditorApplication.update += Poll; }

        [MenuItem("DigiPhant/Kart/Validate keyboard play without camera")]
        static void Request() { Directory.CreateDirectory(".tools"); File.WriteAllText(".tools/test-kart-keys", ""); }
        static void Require(bool ok, string description)
        {
            if (!ok) throw new Exception(description);
            report += "PASS " + description + "\n";
            File.WriteAllText("Logs/Kart/keyboard-checks.txt", report);
        }
        static void Next(int next, params Key[] held)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(held));
            stage = next; at = Time.realtimeSinceStartup;
        }
        static void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (File.Exists(".tools/test-kart-keys") && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                File.Delete(".tools/test-kart-keys");
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                if (scene.isDirty) EditorSceneManager.SaveScene(scene, AssetDatabase.GenerateUniqueAssetPath("Assets/StudentWork/Scenes/Kart-BeforeKeyboardChecks.unity"), true);
                EditorSceneManager.OpenScene(KartBuilder.ScenePath);
                SessionState.SetBool("KartKeyboardChecksPending", true);
                EditorApplication.EnterPlaymode();
            }
            if (!EditorApplication.isPlaying) { Cleanup(); return; }
            if (SessionState.GetBool("KartKeyboardChecksPending", false))
            {
                SessionState.SetBool("KartKeyboardChecksPending", false);
                Directory.CreateDirectory("Logs/Kart");
                report = "Unity Play mode: virtual keyboard events, real game input and physics.\n";
                started = EditorApplication.timeSinceStartup;
                race = Object.FindAnyObjectByType<KartRace>();
                keyboard = InputSystem.AddDevice<Keyboard>("Kart keyboard validation");
                keyboard.MakeCurrent();
                race.ResetRace();
                race.SetMode(0);
                Require(race.TrackingBlocked && Time.timeScale == 0, "Missing camera tracking holds Camera mode");
                var dock = EditorWindow.GetWindow<ChillinDigiphant.PeelPatrol.Editor.PeelPatrolMediaPipeWindow>();
                dock.Show(); dock.Focus();
                race.SelectKeys();
                Next(0);
            }
            if (stage < 0) return;
            try
            {
                if (EditorApplication.timeSinceStartup - started > 35) throw new Exception("Keyboard validation timed out at stage " + stage);
                Step();
            }
            catch (Exception e)
            {
                File.WriteAllText("Logs/Kart/keyboard-error.txt", report + "Stage " + stage + "\n" + e);
                Debug.LogException(e); Cleanup();
            }
        }
        static void Step()
        {
            float elapsed = Time.realtimeSinceStartup - at;
            if (elapsed < .25f) return;
            if (stage == 0)
            {
                Require(!race.CameraMode && race.keyboardTest && !race.CameraConnected && !race.TrackingBlocked && race.CanDrive && Time.timeScale == 1,
                    "Keys releases camera hold immediately, with no camera or calibration");
                Require(EditorWindow.focusedWindow && EditorWindow.focusedWindow.GetType().Name == "GameView", "Selecting Keys from the dock focuses Game view");
                Require(!race.Preview.autoStartBridge && !race.Preview.showPreview, "Keys stops the camera preview and automatic bridge startup");
                ScreenCapture.CaptureScreenshot("Logs/Kart/keys-controls.png");
                position = race.player.Body.position;
                Next(1, Key.W);
            }
            else if (stage == 1)
            {
                if (elapsed < 1.2f) return;
                Require(race.player.Throttle > .5f && race.player.Speed > 1 && Vector3.Distance(position, race.player.Body.position) > .5f, "W drives the real kart in camera-free practice");
                rotation = race.player.Body.rotation;
                Next(2, Key.W, Key.D);
            }
            else if (stage == 2)
            {
                if (elapsed < .7f) return;
                Require(race.player.Steering > .2f && Quaternion.Angle(rotation, race.player.Body.rotation) > 2, "W + D accelerates and steers together");
                race.ResetRace(); Next(3, Key.DownArrow);
            }
            else if (stage == 3)
            {
                if (elapsed < 1) return;
                Require(race.player.Throttle < -.5f && race.player.Speed < -.5f, "Down arrow reverses without camera input");
                race.ResetRace(); Next(4);
            }
            else if (stage == 4)
            {
                if (!race.player.Grounded) return;
                race.player.Collect(3); Next(41);
            }
            else if (stage == 41)
            {
                ScreenCapture.CaptureScreenshot("Logs/Kart/orange-ready.png"); Next(42);
            }
            else if (stage == 42)
            {
                Next(5, Key.Space, Key.E);
            }
            else if (stage == 5)
            {
                Require(race.player.JumpCount == 1 && race.player.Body.linearVelocity.y > 0, "E produces a physical jump");
                Require(race.PlayerTrunk.HoldingOrange && race.player.Ammo == 2, "Space starts the trunk wind-up and reserves one orange");
                Next(51);
            }
            else if (stage == 51)
            {
                if (elapsed < .45f) return;
                Require(race.PlayerTrunk.LastProjectile && race.player.Ammo == 2, "The moving trunk releases a physical orange after wind-up");
                Next(6);
            }
            else if (stage == 6)
            {
                if (!race.player.Grounded) return;
                Next(7, Key.C);
            }
            else if (stage == 7)
            {
                Require(race.player.Crouching, "Holding C ducks"); Next(8);
            }
            else if (stage == 8)
            {
                Require(!race.player.Crouching && Mathf.Abs(race.player.Throttle) < .05f, "Releasing keys clears duck and throttle");
                Next(9, Key.P);
            }
            else if (stage == 9)
            {
                Require(race.Paused && Time.timeScale == 0, "P pauses keyboard play");
                pausedBeforeSwitch = race.Paused; race.SelectCamera(); race.SelectKeys();
                Require(race.Paused == pausedBeforeSwitch, "Changing input mode preserves an intentional pause");
                Next(10);
            }
            else if (stage == 10) Next(11, Key.P);
            else if (stage == 11)
            {
                Require(!race.Paused && Time.timeScale == 1, "P resumes keyboard play"); Next(12);
            }
            else if (stage == 12) Next(13, Key.Enter);
            else if (stage == 13)
            {
                Require(race.Phase == RacePhase.Countdown && !race.TrackingBlocked, "Enter starts countdown without a camera"); Next(14);
            }
            else if (stage == 14)
            {
                if (elapsed < 3.2f) return;
                Require(race.Phase == RacePhase.Racing && race.CanDrive, "Camera-free countdown reaches a playable race");
                race.SelectCamera();
                Require(race.TrackingBlocked && !race.CanDrive, "Camera mode still requires team tracking");
                ScreenCapture.CaptureScreenshot("Logs/Kart/camera-controls.png"); Next(15);
            }
            else if (stage == 15)
            {
                race.SelectKeys(); race.ResetRace();
                Require(!race.TrackingBlocked && race.CanDrive && race.player.Ammo == 0, "Switching back to Keys restores clean solo practice");
                report += "KART_KEYBOARD_VALIDATION_OK\nPhysical keypresses and live performers were not tested by this automated check.\n";
                File.WriteAllText("Logs/Kart/keyboard-checks.txt", report);
                ScreenCapture.CaptureScreenshot("Logs/Kart/keys-controls.png"); Cleanup();
            }
        }
        static void Cleanup()
        {
            stage = -1;
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            keyboard = null;
        }
    }
}
