using System;
using UnityEngine;

namespace ChillinDigiphant.Kart
{
    public enum KartSound { Countdown, Go, Lap, FinalLap, Finish, Hit, Slip, Jump, Land, Duck, Stand, Pickup, Full, Windup, Throw, TargetHit, Boost, Bump, Recover, Brake, Pause, Resume }

    // Installed by KartRace, so the delivered scene and its existing GUIDs need no rewiring.
    [DefaultExecutionOrder(300)]
    public sealed class KartAudio : MonoBehaviour
    {
        [Range(0, 1)] public float musicVolume = .48f;
        [Range(0, 1)] public float effectsVolume = .72f;
        public bool muted;
        public float MusicBpm => (finalMix > .5f ? 160 : 136) * normal.pitch;
        public bool FinalArrangement => finalStarted;
        public bool Suspended => suspended;
        public bool AssetsReady { get; private set; }
        public string Caption { get; private set; } = "";
        public float CaptionRemaining => Mathf.Max(0, captionUntil - Time.unscaledTime);
        public string LapBanner { get; private set; } = "";
        public float BannerRemaining => Mathf.Max(0, bannerUntil - Time.unscaledTime);
        public KartSound LastCue { get; private set; }
        public int CueCount(KartSound sound) => counts[(int)sound];
        public AudioSource MusicSource => normal;
        public AudioSource ActiveMusicSource => finalStarted ? final : normal;
        KartRace race;
        AudioSource normal, final, engine, skid, ui;
        AudioSource[] voices;
        AudioClip[] clips;
        int[] counts;
        float[] cueAt;
        float finalMix, musicGain, duckGain = 1, duckUntil, captionUntil, bannerUntil;
        int voice, countdownBeat;
        bool suspended, finalStarted, wasGrounded, jumped, braking;
        RacePhase previousPhase;

        public void Initialize(KartRace owner)
        {
            race = owner;
            normal = Source("Rainbow Savannah / 136 BPM", true, 128);
            final = Source("Rainbow Savannah / final lap / 160 BPM", true, 128);
            engine = Source("Kart motor", true, 180); skid = Source("Steering friction", true, 190);
            ui = Source("Pause feedback", false, 30);
            voices = new AudioSource[8];
            for (int i = 0; i < voices.Length; i++) voices[i] = Source("Action cue " + i, false, 40);
            var names = Enum.GetNames(typeof(KartSound));
            clips = new AudioClip[names.Length]; counts = new int[names.Length]; cueAt = new float[names.Length];
            AssetsReady = true;
            for (int i = 0; i < names.Length; i++)
            {
                clips[i] = LoadClip(names[i]); cueAt[i] = -100;
                if (!clips[i]) AssetsReady = false;
            }
            normal.clip = LoadClip("RainbowSavannah");
            final.clip = LoadClip("RainbowSavannahFinal");
            engine.clip = LoadClip("Engine"); skid.clip = LoadClip("Skid");
            AssetsReady &= normal.clip && final.clip && engine.clip && skid.clip;
            if (!AssetsReady) Debug.LogError("Kart audio assets are missing from Resources/KartAudio.", this);
            ResetSound();
        }
        static AudioClip LoadClip(string name)
        {
            var clip = Resources.Load<AudioClip>("KartAudio/" + name);
            // Explicitly warm the short cues too: default imports may defer decoding until first play.
            if (clip) clip.LoadAudioData();
            return clip;
        }
        AudioSource Source(string label, bool loop, int priority)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false; source.loop = loop; source.spatialBlend = 0;
            source.dopplerLevel = 0; source.priority = priority; source.volume = 0;
            return source;
        }
        public void ResetSound()
        {
            if (!normal) return;
            foreach (var source in voices) source.Stop(); ui.Stop();
            finalMix = musicGain = 0; finalStarted = suspended = jumped = braking = false;
            duckGain = 1;
            Caption = LapBanner = ""; captionUntil = bannerUntil = duckUntil = 0;
            countdownBeat = 0; previousPhase = RacePhase.Practice; wasGrounded = false;
            Array.Clear(counts, 0, counts.Length);
            for (int i = 0; i < cueAt.Length; i++) cueAt[i] = -100;
            foreach (var source in new[] { normal, final, engine, skid })
            { source.Stop(); source.volume = 0; source.pitch = 1; if (source.clip) source.Play(); }
        }
        public void Play(KartSound sound, string caption = "")
        {
            if (!race || !enabled || !race.enabled || clips == null) return;
            bool menu = sound == KartSound.Pause || sound == KartSound.Resume;
            if (!menu && (race.Paused || race.TrackingBlocked)) return;
            int index = (int)sound;
            float delay = sound == KartSound.Bump ? .5f : sound == KartSound.Full ? 1 : .12f;
            if (Time.unscaledTime - cueAt[index] < delay) return;
            cueAt[index] = Time.unscaledTime; counts[index]++; LastCue = sound;
            if (clips[index] && !muted)
            {
                var source = menu ? ui : voices[voice++ % voices.Length];
                source.Stop(); source.clip = clips[index]; source.volume = effectsVolume;
                source.Play();
            }
            if (caption.Length > 0) { Caption = caption; captionUntil = Time.unscaledTime + 1.2f; }
            if (sound == KartSound.Lap || sound == KartSound.FinalLap || sound == KartSound.Finish || sound == KartSound.Hit || sound == KartSound.Slip)
                duckUntil = Time.unscaledTime + .9f;
        }
        public void CrossFinishLine()
        {
            bool finished = race.player.CompletedLaps >= race.totalLaps;
            bool last = !finished && race.player.CompletedLaps == race.totalLaps - 1;
            LapBanner = finished ? "FINISH!" : last ? "FINAL LAP!" : "LAP " + race.player.CompletedLaps + " COMPLETE";
            bannerUntil = Time.unscaledTime + 3;
            if (last && !finalStarted)
            {
                // Seek the faster performance to the same musical phrase before crossfading.
                if (final.clip && normal.clip) final.timeSamples = Mathf.Min(final.clip.samples - 1,
                    Mathf.RoundToInt((float)normal.timeSamples / normal.clip.samples * final.clip.samples));
                finalStarted = true;
            }
            Play(finished ? KartSound.Finish : last ? KartSound.FinalLap : KartSound.Lap);
        }
        void SetSuspended(bool value)
        {
            if (value == suspended) return;
            suspended = value;
            foreach (var source in new[] { normal, final, engine, skid })
                if (value) source.Pause(); else source.UnPause();
            foreach (var source in voices) if (value) source.Pause(); else source.UnPause();
        }
        void LateUpdate()
        {
            if (!race || !normal) return;
            SetSuspended(race.Paused || race.TrackingBlocked || !race.enabled);
            foreach (var source in voices) source.volume = muted ? 0 : effectsVolume;
            ui.volume = muted ? 0 : effectsVolume;
            if (suspended) return;
            if (race.Phase == RacePhase.Countdown)
            {
                int beat = Mathf.CeilToInt(race.Countdown);
                if (beat != countdownBeat) { countdownBeat = beat; Play(KartSound.Countdown); }
            }
            if (race.Phase == RacePhase.Racing && previousPhase == RacePhase.Countdown) Play(KartSound.Go, "GO!");
            previousPhase = race.Phase;
            float dt = Time.unscaledDeltaTime;
            // Lap two warms up slightly; the dedicated final arrangement adds drums without transposing the melody.
            float lapFraction = Mathf.Repeat(race.player.Progress / race.track.gates.Length, 1);
            float pace = race.Phase == RacePhase.Racing ? (finalStarted ? 1 + .045f * Mathf.SmoothStep(0, 1, lapFraction) : 1 + .025f * race.player.CompletedLaps) : 1;
            normal.pitch = final.pitch = Mathf.MoveTowards(normal.pitch, pace, dt * .025f);
            finalMix = Mathf.MoveTowards(finalMix, finalStarted ? 1 : 0, dt / 1.25f);
            float target = race.Phase == RacePhase.Finished ? 0 : race.Phase == RacePhase.Practice ? .3f : race.Phase == RacePhase.Countdown ? .5f : 1;
            musicGain = Mathf.MoveTowards(musicGain, target, dt * .9f);
            duckGain = Mathf.MoveTowards(duckGain, Time.unscaledTime < duckUntil ? .38f : 1, dt * 4);
            float music = muted ? 0 : musicVolume * musicGain * duckGain;
            normal.volume = music * Mathf.Cos(finalMix * Mathf.PI * .5f);
            final.volume = music * Mathf.Sin(finalMix * Mathf.PI * .5f);
            var kart = race.player;
            float speed = Mathf.Clamp01(Mathf.Abs(kart.Speed) / kart.maxSpeed);
            engine.pitch = Mathf.Lerp(engine.pitch, .7f + speed * .95f + Mathf.Abs(kart.Throttle) * .22f, 1 - Mathf.Exp(-5 * dt));
            bool driving = race.CanDrive && !kart.Finished && !kart.Recovering;
            engine.volume = muted || !driving ? 0 : effectsVolume * (.025f + speed * .10f) * (kart.StunRemaining > 0 ? .3f : 1);
            skid.volume = muted || !driving || !kart.Grounded ? 0 : effectsVolume * .065f * speed * Mathf.InverseLerp(.35f, .9f, Mathf.Abs(kart.Steering));
            bool brake = driving && kart.Throttle < -.25f && kart.Speed > 2;
            if (brake && !braking) Play(KartSound.Brake, "BRAKING"); braking = brake;
            if (jumped && kart.Grounded && !wasGrounded) { Play(KartSound.Land, "LANDED"); jumped = false; }
            if (kart.Jumping) jumped = true;
            if (kart.Recovering) jumped = false;
            wasGrounded = kart.Grounded;
        }
        void OnDisable()
        {
            if (!normal) return;
            foreach (var source in new[] { normal, final, engine, skid, ui }) source.Stop();
            foreach (var source in voices) source.Stop();
        }
        void OnEnable() { if (normal) ResetSound(); }
    }
}
