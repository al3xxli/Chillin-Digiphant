using System;
using System.IO;
using DigiPhant;
using UnityEditor;
using UnityEngine;
using ChillinDigiphant.PeelPatrol.Editor;
using Object = UnityEngine.Object;

namespace ChillinDigiphant.Kart.Editor
{
    [InitializeOnLoad]
    public static class KartCameraStartChecks
    {
        static KartRace race;
        static PeelPatrolMediaPipeWindow dock;
        static int stage = -1;
        static double at, began;
        static string report;
        static KartCameraStartChecks() { EditorApplication.update += Poll; }
        static void Next(int value) { stage = value; at = EditorApplication.timeSinceStartup; }
        static void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (!EditorApplication.isPlaying)
            {
                stage = -1;
                if (File.Exists(".tools/test-kart-camera-start") && !EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.EnterPlaymode();
                return;
            }
            if (File.Exists(".tools/test-kart-camera-start"))
            {
                File.Delete(".tools/test-kart-camera-start"); race = Object.FindAnyObjectByType<KartRace>();
                race.SelectKeys(); race.ResetRace(); race.SetCameraLayout(KartCameraLayout.MixedTeam); race.SetLiteralActions(true); race.UsePerformers(3); race.SelectCamera();
                race.showMediaPipePanel = true;
                dock = EditorWindow.GetWindow<PeelPatrolMediaPipeWindow>(); dock.Show(); dock.Focus(); dock.Repaint();
                report = "Unity camera start: actual dock button events, synthetic role packets, real calibration and race countdowns\n";
                began = EditorApplication.timeSinceStartup; Next(0);
            }
            if (stage < 0) return;
            try
            {
                if (EditorApplication.timeSinceStartup - began > 35) throw new Exception("Camera start check timed out at " + stage);
                Step();
            }
            catch (Exception e) { File.WriteAllText("Logs/Kart/camera-start-error.txt", report + e); stage = -1; Debug.LogException(e); }
        }
        static void ClickStart()
        {
            Vector2 point = race.CameraStartButtonRect.center - dock.position.position;
            dock.SendEvent(new Event { type = EventType.MouseDown, button = 0, mousePosition = point });
            dock.SendEvent(new Event { type = EventType.MouseUp, button = 0, mousePosition = point });
            // This suite injects packets; prevent competing webcam input.
            race.Preview.autoStartBridge = false; race.Preview.StopPreview();
        }
        static void Frame(bool shrug = true, bool feet = true)
        {
            var body = new BodyMotionFrame { hipY = .55f, shoulderY = .3f, leftFootY = .9f, rightFootY = .9f, scale = .25f, confidence = 1 };
            var packet = new PoseFrame { version = 1, performerCount = 3, previewFormat = 2, mirrorPreview = race.Preview.mirrorPreview, upperBodyOnly = race.controller.upperBodyOnly,
                kartStatus = "Camera start test", people = new[] {
                    new PerformerFrame { slot = 1, values = new float[6], confidence = new[] { 1f,1f,1f,1f,1f,1f }, handForward = new float[2], handForwardConfidence = new float[2], body = feet ? body : null },
                    new PerformerFrame { slot = 2, values = new float[6], confidence = new float[6], handForward = new float[2], handForwardConfidence = new float[2], shoulders = shrug ? new ShoulderMotionFrame { turn = 0, confidence = 1 } : null }
                } };
            if (!race.controller.AcceptPacket(JsonUtility.ToJson(packet), Time.realtimeSinceStartup)) throw new Exception("Camera packet was rejected");
        }
        static void Require(bool value, string message)
        { if (!value) throw new Exception(message); report += "PASS " + message + "\n"; File.WriteAllText("Logs/Kart/camera-start-checks.txt", report); }
        static void Step()
        {
            double elapsed = EditorApplication.timeSinceStartup - at;
            if (stage == 0)
            {
                if (elapsed < .6) return;
                Require(race.CameraStartLabel == "START WITH CAMERA" && race.CameraStartButtonRect.width > 100, "Camera start is visible before connection or calibration");
                ScreenCapture.CaptureScreenshot("Logs/Kart/camera-start-before.png"); ClickStart();
                Require(race.CameraMode && race.CameraConnected && race.CameraStartPending, "Clicking the visible button opens Camera mode and queues a camera race"); Next(1);
            }
            else if (stage == 1)
            {
                if (elapsed < .5) return;
                Require(race.Phase == RacePhase.Practice && !race.controller.CalibrationPending && race.TrackingBlocked, "Missing team waits without starting the race or forcing Keys");
                Frame(shrug:false); Next(2);
            }
            else if (stage == 2)
            {
                Frame(shrug:false); if (elapsed < .4) return;
                Require(!race.controller.CalibrationPending && race.MovementStatus.Contains("P2"), "Missing shoulders has actionable P2 guidance"); Next(3);
            }
            else if (stage == 3)
            {
                Frame(feet:false); if (elapsed < .4) return;
                Require(race.controller.CalibrationPending, "P1 feet are optional for team calibration"); Next(4);
            }
            else if (stage == 4)
            {
                Frame(); if (elapsed < .4) return;
                Require(race.controller.CalibrationPending && race.CameraStartPending && Time.timeScale == 0, "Visible required roles begin neutral calibration while gameplay stays held");
                Require(race.CameraStartStatus.Contains("automatically"), "Calibration explains that the race will start automatically"); Next(5);
            }
            else if (stage == 5)
            {
                Frame(); if (race.Phase != RacePhase.Countdown) return;
                Require(race.controller.IsCalibrated && !race.CameraStartPending && !race.TrackingBlocked && race.CameraMode,
                    "Neutral completion enters the race countdown automatically with no P3 torso or hand required");
                ScreenCapture.CaptureScreenshot("Logs/Kart/camera-start-countdown.png"); Next(6);
            }
            else if (stage == 6)
            {
                Frame(); if (elapsed < 3.4) return;
                Require(race.IsRacing && race.Elapsed > 0 && race.CanDrive, "Camera countdown reaches Racing and advances the race clock"); Next(7);
            }
            else if (stage == 7)
            {
                if (elapsed < .65) return;
                Require(race.TrackingBlocked && race.IsRacing && !race.CameraStartPending, "Tracking loss holds the existing race without resetting or switching controls");
                race.ResetRace(); race.controller.InvalidateCalibration(); race.StartWithCamera(); race.Preview.autoStartBridge = false; race.Preview.StopPreview();
                race.CancelCameraStart(); Frame(); race.AdvanceCameraStart(Time.realtimeSinceStartup);
                Require(!race.CameraStartPending && !race.controller.CalibrationPending, "Cancel prevents a delayed automatic start");
                race.StartWithCamera(); race.SelectKeys();
                Require(!race.CameraStartPending && !race.controller.CalibrationPending && !race.TrackingBlocked, "Choosing Keys cancels camera setup and leaves solo play available");
                race.ResetRace(); race.SelectCamera(); race.ConnectCamera();
                report += "KART_CAMERA_START_VALIDATION_OK\nLive team calibration and gestures remain physical checks.\n";
                File.WriteAllText("Logs/Kart/camera-start-checks.txt", report); stage = -1;
                ScreenCapture.CaptureScreenshot("Logs/Kart/camera-start-ready.png");
            }
        }
    }
}
