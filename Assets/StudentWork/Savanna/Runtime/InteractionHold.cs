namespace ChillinDigiphant.Savanna
{
    // A deliberate gesture must begin inside a zone. Holding it cannot repeat an action.
    public sealed class InteractionHold
    {
        public float Progress { get; private set; }
        bool armed;
        int previousZone = -1;
        public void Reset() { Progress = 0; armed = false; previousZone = -1; }
        public bool Step(int zone, bool valid, float signal, float dt, float duration)
        {
            if (zone != previousZone) { Progress = 0; armed = false; previousZone = zone; }
            if (!valid || zone < 0) { Progress = 0; armed = false; return false; }
            if (signal < .25f) { Progress = 0; armed = true; return false; }
            if (!armed || signal < .6f) { Progress = 0; return false; }
            Progress += System.Math.Max(0, System.Math.Min(dt, .1f)) / System.Math.Max(.1f, duration);
            if (Progress < 1) return false;
            Progress = 0; armed = false;
            return true;
        }
    }
}
