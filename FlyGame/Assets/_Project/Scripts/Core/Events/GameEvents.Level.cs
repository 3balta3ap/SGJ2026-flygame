using System;

namespace FlyJam
{
    public static partial class GameEvents
    {
        public static event Action<int> ExpRequested;               // EXP to add (items, dodge success)
        public static event Action<int, int, int> ExpChanged;       // exp in this level, exp needed (0 = max level), level
        public static event Action<int, int> LevelChanged;          // new level, previous level (same value at round start)
        public static event Action<bool> FeverChanged;              // level-up fever on / off

        public static void RaiseExpRequested(int amount) => ExpRequested?.Invoke(amount);
        public static void RaiseExpChanged(int exp, int need, int level) => ExpChanged?.Invoke(exp, need, level);
        public static void RaiseLevelChanged(int level, int previous) => LevelChanged?.Invoke(level, previous);
        public static void RaiseFeverChanged(bool on) => FeverChanged?.Invoke(on);
    }
}
