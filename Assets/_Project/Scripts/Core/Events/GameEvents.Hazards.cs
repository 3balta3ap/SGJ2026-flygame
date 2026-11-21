using System;

namespace FlyJam
{
    public static partial class GameEvents
    {
        public static event Action PerfectDodge;
        public static event Action<float> ReactionMeasured;          // seconds

        public static void RaisePerfectDodge() => PerfectDodge?.Invoke();
        public static void RaiseReactionMeasured(float s) => ReactionMeasured?.Invoke(s);
    }
}
