using System;

namespace ChillinDigiphant.PeelPatrol
{
    public enum RoundPhase { Ready, Preparing, Wave, Won, Lost }

    // Gameplay state is independent of rendering, keyboard and camera input.
    public sealed class DefenseRound
    {
        public const int Capacity = 10;
        public const int MaxWater = 100;
        public const int LastWave = 3;
        public RoundPhase Phase { get; private set; } = RoundPhase.Ready;
        public int Water { get; private set; } = MaxWater;
        public int Ammo { get; private set; } = 6;
        public int Wave { get; private set; }
        public int Defeated { get; private set; }
        public int Spawned { get; private set; }
        public int Coins { get; private set; }
        public int Upgrade { get; private set; }
        public string Ride => new[] { "On foot", "Skateboard", "Unicycle", "Jetpack" }[Upgrade];
        public float SpeedMultiplier => new[] { 1f, 1.3f, 1.65f, 2.05f }[Upgrade];
        public int NextUpgradeCost => Upgrade < 3 ? new[] { 12, 20, 28 }[Upgrade] : 0;
        public string NextRide => Upgrade < 3 ? new[] { "Skateboard", "Unicycle", "Jetpack" }[Upgrade] : "Fully upgraded";
        public int WaveSize => 3 + Wave * 3;
        public float Countdown { get; private set; }
        float spawnClock;
        public bool Active => Phase == RoundPhase.Preparing || Phase == RoundPhase.Wave;

        public void Reset()
        {
            Phase = RoundPhase.Ready; Water = MaxWater; Ammo = 6; Wave = Defeated = Spawned = 0;
            Countdown = spawnClock = 0;
            Coins = Upgrade = 0;
        }
        public void Start()
        { if (Phase == RoundPhase.Ready) { Phase = RoundPhase.Preparing; Countdown = 18; } }
        // Returns one spawn request. Time steps are bounded; paused callers do not tick.
        public bool Tick(float dt, int livingEnemies)
        {
            dt = Math.Max(0, Math.Min(dt, .1f));
            if (Phase == RoundPhase.Preparing)
            {
                Countdown -= dt;
                if (Countdown <= 0) { Wave++; Spawned = 0; spawnClock = 0; Phase = RoundPhase.Wave; }
                return false;
            }
            if (Phase != RoundPhase.Wave) return false;
            if (Spawned < WaveSize)
            {
                spawnClock -= dt;
                if (spawnClock <= 0) { Spawned++; spawnClock = 4.9f - Wave * .4f; return true; }
            }
            else if (livingEnemies == 0)
            {
                Coins += 8;
                if (Wave == LastWave) Phase = RoundPhase.Won;
                else { Phase = RoundPhase.Preparing; Countdown = 18; }
            }
            return false;
        }
        public bool Spend(int amount,bool allowPractice=false)
        { if (!(Active || allowPractice && Phase==RoundPhase.Ready) || amount < 1 || Ammo < amount) return false; Ammo -= amount; return true; }
        public bool Refill(bool allowPractice=false)
        { if (!(Active || allowPractice && Phase==RoundPhase.Ready) || Ammo == Capacity) return false; Ammo = Capacity; return true; }
        public bool CleanWater()
        { if (Water == MaxWater || !Spend(3)) return false; Water = Math.Min(MaxWater, Water + 25); return true; }
        public void Contaminate(int amount)
        {
            if (!Active || amount <= 0) return;
            Water = Math.Max(0, Water - amount);
            if (Water == 0) Phase = RoundPhase.Lost;
        }
        public void RegisterDefeat() { if (Active) { Defeated++; Coins += 3; } }
        public bool BuyUpgrade()
        {
            if (!Active || Upgrade >= 3 || Coins < NextUpgradeCost) return false;
            Coins -= NextUpgradeCost; Upgrade++; return true;
        }
    }
}
