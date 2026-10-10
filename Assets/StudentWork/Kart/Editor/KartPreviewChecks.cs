using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using ChillinDigiphant.PeelPatrol.Editor;
using Object = UnityEngine.Object;

namespace ChillinDigiphant.Kart.Editor
{
    [InitializeOnLoad]
    public static class KartPreviewChecks
    {
        static KartRace race;
        static PeelPatrolMediaPipeWindow dock;
        static int stage = -1, frames, handFrames, shoulderFrames, bodyFrames;
        static float minShrug, maxShrug;
        static double at, liveUntil, nextLiveStatus;
        static Vector2 before, point;
        static string report;
        static KartPreviewChecks() { EditorApplication.update += Poll; }
        static void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (!EditorApplication.isPlaying)
            {
                stage = -1;
                if ((File.Exists(".tools/test-kart-preview") || File.Exists(".tools/check-kart-camera")) && !File.Exists(".tools/refresh-kart") && !EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.EnterPlaymode();
                return;
            }
            if (File.Exists(".tools/test-kart-preview"))
            {
                File.Delete(".tools/test-kart-preview");
                race = Object.FindAnyObjectByType<KartRace>(); race.SelectCamera();
                dock = EditorWindow.GetWindow<PeelPatrolMediaPipeWindow>(); dock.Show(); dock.Focus(); dock.Repaint();
                report = "Unity EditorWindow mouse events through the actual camera preview GUI\n"; stage = 0; at = EditorApplication.timeSinceStartup;
            }
            if (File.Exists(".tools/check-kart-camera"))
            {
                File.Delete(".tools/check-kart-camera"); race = Object.FindAnyObjectByType<KartRace>();
                race.ConnectCamera(); frames = handFrames = shoulderFrames = bodyFrames = 0; minShrug = maxShrug = 0; liveUntil = EditorApplication.timeSinceStartup + 120;
                race.controller.FrameReceived -= ReadFrame; race.controller.FrameReceived += ReadFrame;
            }
            if (liveUntil > 0 && EditorApplication.timeSinceStartup > liveUntil)
            { if (race) race.controller.FrameReceived -= ReadFrame; liveUntil = 0; }
            if(liveUntil>0 && race && EditorApplication.timeSinceStartup>=nextLiveStatus)
            {
                nextLiveStatus=EditorApplication.timeSinceStartup+1;
                File.WriteAllText("Logs/Kart/live-camera-status.txt",race.Preview.Status+"\nEnabled="+race.Preview.isActiveAndEnabled+"; autoStart="+race.Preview.autoStartBridge+"; live="+race.Preview.HasLiveFrame+"; packets="+frames+"\n"+race.controller.Status+"\n");
            }
            if (stage < 0 || EditorApplication.timeSinceStartup - at < .5) return;
            at = EditorApplication.timeSinceStartup;
            try
            {
                if (stage == 0)
                {
                    before = race.LastPreviewRect.size;
                    Require(before.x >= 160 && before.y >= 100, "Preview has a visible resize handle");
                    point = race.LastPreviewHandle.center - dock.position.position;
                    Send(EventType.MouseDown, point); Send(EventType.MouseDrag, point - new Vector2(40, 40)); Send(EventType.MouseUp, point - new Vector2(40, 40));
                    dock.Repaint(); stage = 1;
                }
                else if (stage == 1)
                {
                    Require(Mathf.Abs(race.DockPreviewSize.x - Mathf.Max(160, before.x - 40)) < 2 && Mathf.Abs(race.DockPreviewSize.y - Mathf.Max(100, before.y - 40)) < 2,
                        "Dragging bottom-right changes both preview dimensions");
                    Require(Mathf.Abs(PlayerPrefs.GetFloat("Kart.CameraPreview.Dock.Width") - race.DockPreviewSize.x) < .1f,
                        "Releasing the handle saves the preview size");
                    point = race.LastPreviewHandle.center - dock.position.position;
                    Send(EventType.MouseDown, point); Send(EventType.MouseDrag, point + new Vector2(40, 40)); Send(EventType.MouseUp, point + new Vector2(40, 40));
                    dock.Repaint(); stage = 2;
                }
                else
                {
                    Require(Mathf.Abs(race.DockPreviewSize.x - before.x) < 2 && Mathf.Abs(race.DockPreviewSize.y - before.y) < 2,
                        "Dragging outward enlarges the preview again");
                    report += "KART_PREVIEW_VALIDATION_OK\n"; File.WriteAllText("Logs/Kart/preview-checks.txt", report); stage = -1;
                    race.showMediaPipePanel = true; ScreenCapture.CaptureScreenshot("Logs/Kart/resizable-preview.png");
                }
            }
            catch (Exception e) { File.WriteAllText("Logs/Kart/preview-error.txt", report + e + "\nSize=" + race.DockPreviewSize + " handle=" + point + " window=" + dock.position); stage = -1; Debug.LogException(e); }
        }
        static void Send(EventType type, Vector2 point) => dock.SendEvent(new Event { type = type, button = 0, mousePosition = point });
        static void Require(bool ok, string message) { if (!ok) throw new Exception(message); report += "PASS " + message + "\n"; }
        static void ReadFrame(DigiPhant.PoseFrame packet, float now)
        {
            frames++;
            foreach (var person in packet.people)
            {
                if (person.slot == 3 && person.hand != null) handFrames++;
                if (person.slot == 1 && person.body != null) bodyFrames++;
                if (person.slot == 2 && person.shoulders != null)
                { shoulderFrames++; minShrug=Mathf.Min(minShrug,person.shoulders.turn); maxShrug=Mathf.Max(maxShrug,person.shoulders.turn); }
            }
            File.WriteAllText("Logs/Kart/live-hand-check.txt", "Live packets=" + frames + "\nHand frames=" + handFrames + "\n" + packet.kartStatus + "\n" + packet.kartHandStatus + "\n" + race.cameraInput.HandFeedback(now) + "\n");
            File.WriteAllText("Logs/Kart/live-shrug-jump-check.txt", "Live packets="+frames+"\nP1 ankle frames="+bodyFrames+"\nP2 shoulder frames="+shoulderFrames+"\nShoulder range="+minShrug.ToString("F3")+" to "+maxShrug.ToString("F3")+"\nCalibrated="+race.cameraInput.Calibrated+"\nFoot lift="+race.cameraInput.LeftFootLift.ToString("F3")+"\nJump count="+race.player.JumpCount+"\nSpeed="+race.player.Speed.ToString("F2")+"\n"+packet.kartStatus+"\n");
        }
    }
}
