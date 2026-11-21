using System;

namespace FlyJam
{
    public static partial class GameEvents
    {
        public static event Action<bool, int> NightChanged;   // isNight, day index (0 = first day). Raised by GameSession when it flips

        public static void RaiseNightChanged(bool night, int day) => NightChanged?.Invoke(night, day);
    }
}
