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
    public static class KartAudioChecks
    {
        const string Pending = "KartAudioChecks.Pending";
        static KartRace race;
        static int stage = -1, samples, jumps, ducks, hits;
        static float at;
        static double started, realAt;
        static string report;
        static KartAudioChecks() { EditorApplication.update += Poll; }
        [MenuItem("DigiPhant/Kart/Validate race audio")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.isDirty) EditorSceneManager.SaveScene(scene, AssetDatabase.GenerateUniqueAssetPath("Assets/StudentWork/Scenes/Kart-BeforeAudioChecks.unity"), true);
            Directory.CreateDirectory("Logs/Kart");
            if (File.Exists("Logs/Kart/audio-error.txt")) File.Delete("Logs/Kart/audio-error.txt");
            EditorSceneManager.OpenScene(KartBuilder.ScenePath);
            KartBuilder.Validate();
            EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView")).Show();
            SessionState.SetBool(Pending, true);
            EditorApplication.EnterPlaymode();
        }
        static void Require(bool pass, string text)
        {
            if (!pass) throw new Exception(text);
            report += "PASS " + text + "\n"; File.WriteAllText("Logs/Kart/audio-checks.txt", report);
        }
        static void Next(int value) { stage = value; at = Time.time; realAt = EditorApplication.timeSinceStartup; }
        static void Lap(KartVehicle kart)
        {
            for (int n = 0; n < race.track.gates.Length; n++)
            {
                int sample = race.track.gates[kart.NextGate];
                Vector3 center = race.track.Point(sample), direction = race.track.Forward(sample);
                kart.CheckNavigation(center - direction, center + direction);
            }
        }
        static void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || !EditorApplication.isPlaying) return;
            if (SessionState.GetBool(Pending, false))
            { SessionState.SetBool(Pending, false); started = EditorApplication.timeSinceStartup; report = "Unity audio integration / actual race actions and playback state\n"; Next(0); }
            if (stage < 0) return;
            try
            {
                if (EditorApplication.timeSinceStartup - started > 60) throw new Exception("Audio checks timed out at " + stage);
                Step();
            }
            catch (Exception error)
            { File.WriteAllText("Logs/Kart/audio-error.txt", report + "\nStage " + stage + "\n" + error); Debug.LogException(error); Finish(1); }
        }
        static void Step()
        {
            float elapsed = Time.time - at;
            if (stage == 0)
            {
                race = Object.FindFirstObjectByType<KartRace>(); if (!race || !race.Audio) return;
                race.SetMode(1); race.ResetRace();
                Require(race.Audio.AssetsReady, "Both music performances, engine/skid loops and every cue import successfully");
                Require(race.Audio.MusicSource.loop && race.Audio.MusicSource.clip.length > 56 && !race.Audio.FinalArrangement, "Practice starts with the original normal looping track");
                Require(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(l => l.enabled) == 1, "Exactly one active listener in the delivered scene");
                foreach (var clip in Resources.LoadAll<AudioClip>("KartAudio"))
                    Require(clip.loadState == AudioDataLoadState.Loaded && clip.samples > 0, "Decoded audio data: " + clip.name);
                race.StartRace(); Next(1);
            }
            else if (stage == 1)
            {
                if (!race.IsRacing) return;
                // LateUpdate follows the race's Update; wait another frame for the GO cue.
                Next(2);
            }
            else if (stage == 2)
            {
                Require(race.Audio.CueCount(KartSound.Countdown) == 3 && race.Audio.CueCount(KartSound.Go) == 1, "Countdown gives exactly three beeps and one GO cue");
                Require(race.player.Collect(3) && race.Audio.CueCount(KartSound.Pickup) == 1, "A successful pickup emits its cue");
                Require(race.RequestThrow() && race.Audio.CueCount(KartSound.Windup) == 1 && race.Audio.CueCount(KartSound.Throw) == 0, "Trunk windup has a cue; release sound waits for the projectile");
                Next(3);
            }
            else if (stage == 3)
            {
                if (elapsed < 1.1f) return;
                Require(race.Audio.MusicSource.isPlaying && race.Audio.MusicSource.timeSamples > 0, "Music playback advances through the Unity audio engine");
                Require(race.PlayerTrunk.ReleaseCount > 0 && race.Audio.CueCount(KartSound.Throw) == 1, "The actual trunk projectile release emits one throw sound");
                if (!race.player.Grounded) return;
                jumps = race.Audio.CueCount(KartSound.Jump);
                Require(race.player.Jump() && race.Audio.CueCount(KartSound.Jump) == jumps + 1, "Successful jump emits one sound");
                Require(!race.player.Jump() && race.Audio.CueCount(KartSound.Jump) == jumps + 1, "Rejected airborne repeat has no extra sound"); Next(4);
            }
            else if (stage == 4)
            {
                if (elapsed < 1.4f || !race.player.Grounded) return;
                Require(race.Audio.CueCount(KartSound.Land) == 1, "Actual grounded transition after the hop emits one landing cue");
                race.testCrouch = true; Next(5);
            }
            else if (stage == 5)
            {
                if (elapsed < .25f) return;
                Require(race.player.Crouching && race.Audio.CueCount(KartSound.Duck) == 1, "Crouch collider change emits duck feedback");
                ducks = race.Audio.CueCount(KartSound.Duck); Next(6);
            }
            else if (stage == 6)
            {
                if (elapsed < .3f) return;
                Require(race.Audio.CueCount(KartSound.Duck) == ducks, "Holding crouch does not repeat the sound");
                race.testCrouch = false; Next(7);
            }
            else if (stage == 7)
            {
                if (elapsed < .25f) return;
                Require(!race.player.Crouching && race.Audio.CueCount(KartSound.Stand) == 1, "Standing when clear emits the release cue");
                Require(race.player.Hit(Vector3.zero) && race.Audio.CueCount(KartSound.Hit) == 1, "Accepted orange impact emits hit feedback");
                hits = race.Audio.CueCount(KartSound.Hit);
                Require(!race.player.Hit(Vector3.zero) && race.Audio.CueCount(KartSound.Hit) == hits, "Hit immunity prevents duplicate hit audio");
                race.player.Boost(); race.player.Boost();
                Require(race.Audio.CueCount(KartSound.Boost) == 1, "Overlapping boost-pad contacts give one boost cue");
                Lap(race.racers.First(v => v != race.player));
                Require(race.Audio.CueCount(KartSound.Lap) == 0 && !race.Audio.FinalArrangement, "Rivals cannot trigger player lap audio or music changes");
                Lap(race.player);
                Require(race.Audio.CueCount(KartSound.Lap) == 1 && race.Audio.LapBanner == "LAP 1 COMPLETE", "Crossing finish after all checkpoints emits lap cue and visible banner");
                Lap(race.player);
                Require(race.Audio.CueCount(KartSound.FinalLap) == 1 && race.Audio.FinalArrangement && race.Audio.LapBanner == "FINAL LAP!", "Final lap starts its own fanfare, banner and faster performance");
                ScreenCapture.CaptureScreenshot("Logs/Kart/audio-final-lap.png");
                Next(8);
            }
            else if (stage == 8)
            {
                if (elapsed < 1.5f) return;
                Require(race.Audio.MusicBpm >= 160, "After the crossfade final-lap tempo is at least 160 BPM");
                race.TogglePause(); Next(9);
            }
            else if (stage == 9)
            {
                if (EditorApplication.timeSinceStartup - realAt < .15) return;
                Require(race.Audio.Suspended, "Pause suspends music, motor and action voices");
                samples = race.Audio.ActiveMusicSource.timeSamples; Next(10);
            }
            else if (stage == 10)
            {
                if (EditorApplication.timeSinceStartup - realAt < .35) return;
                Require(Mathf.Abs(race.Audio.ActiveMusicSource.timeSamples - samples) < 256, "Active final-lap playback position stays fixed while paused");
                race.TogglePause(); Next(11);
            }
            else if (stage == 11)
            {
                if (elapsed < .25f) return;
                Require(!race.Audio.Suspended && race.Audio.CueCount(KartSound.Pause) == 1 && race.Audio.CueCount(KartSound.Resume) == 1, "Resume restores sound and both menu cues fire once");
                Require(race.Audio.ActiveMusicSource.isPlaying && race.Audio.ActiveMusicSource.timeSamples != samples, "Final-lap playback resumes from its held position");
                Require(race.player.Hit(Vector3.zero, true) && race.Audio.CueCount(KartSound.Slip) == 1, "Banana impact has a distinct slip sound");
                race.player.Recover();
                Require(race.Audio.CueCount(KartSound.Recover) == 1, "Recovery emits checkpoint feedback");
                Lap(race.player);
                Require(race.Phase == RacePhase.Finished && race.Audio.CueCount(KartSound.Finish) == 1 && race.Audio.LapBanner == "FINISH!", "Third finish crossing plays victory fanfare and finish banner");
                ScreenCapture.CaptureScreenshot("Logs/Kart/audio-finish.png");
                Next(12);
            }
            else if (stage == 12)
            {
                if (elapsed < 1.5f) return;
                Require(race.Audio.MusicSource.volume == 0, "Race music fades out behind the victory fanfare");
                race.Audio.muted = true; race.ResetRace(); Next(13);
            }
            else if (stage == 13)
            {
                if (elapsed < .1f) return;
                Require(!race.Audio.FinalArrangement && race.Audio.CueCount(KartSound.Finish) == 0 && race.Audio.LapBanner == "", "Replay resets arrangement, counters and banners");
                Require(race.Audio.MusicSource.volume == 0 && race.Audio.muted, "Mute persists through reset and silences the music");
                race.Audio.muted = false;
                race.SetMode(0); Next(14);
            }
            else if (stage == 14)
            {
                if (EditorApplication.timeSinceStartup - realAt < .15) return;
                Require(race.TrackingBlocked && race.Audio.Suspended, "Missing Camera driving input suspends audio");
                samples = race.Audio.ActiveMusicSource.timeSamples; Next(15);
            }
            else if (stage == 15)
            {
                if (EditorApplication.timeSinceStartup - realAt < .3) return;
                Require(Mathf.Abs(race.Audio.ActiveMusicSource.timeSamples - samples) < 256, "Tracking hold preserves music position");
                race.SetMode(1); Next(16);
            }
            else if (stage == 16)
            {
                if (elapsed < .2f) return;
                Require(!race.Audio.Suspended && race.Audio.ActiveMusicSource.isPlaying, "Restoring drive input resumes music");
                report += "KART_AUDIO_VALIDATION_OK\nDevice listening and real performer confirmation are separate checks.\n";
                File.WriteAllText("Logs/Kart/audio-checks.txt", report); Finish(0);
            }
        }
        static void Finish(int code)
        {
            stage = -1;
            if (Application.isBatchMode || Environment.GetCommandLineArgs().Contains("-digiphantAudioChecksExit")) EditorApplication.Exit(code);
            else { if (race) { race.ResetRace(); race.SelectKeys(); } EditorApplication.ExitPlaymode(); }
        }
    }
}
