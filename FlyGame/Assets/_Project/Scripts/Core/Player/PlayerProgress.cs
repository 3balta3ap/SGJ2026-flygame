using UnityEngine;

namespace FlyJam
{
    /// Numbers the current level gives other features (one row of the "Level" sheet table).
    [System.Serializable]
    public struct LevelInfo
    {
        public int level;              // 1, 2, 3 ...
        public int expToNext;          // EXP needed for the next level. 0 or less = max level
        public float maxSpeed;         // player speed at full HP
        public float turnRate;         // degrees per second at full stick tilt
        public float maxHp;            // max HP at this level
        public float rareItemChance;   // 0-1: chance that a respawning item is a rare one (banana)
        public int courtLength;        // courtship buttons at this level
        public float courtTime;        // courtship time limit (seconds). 0 = none
    }

    /// Read-only snapshot of player growth, written by the Level feature.
    /// Other features read it instead of referencing the Level feature (no asmdef dependency).
    /// Without SYS_Level: Active = false and everyone uses their own sheet values.
    public static class PlayerProgress
    {
        public static bool Active { get; private set; }
        public static LevelInfo Info { get; private set; }
        public static int Level => Active ? Info.level : 1;
        /// Level-up fever: invulnerable, flying, no warnings, no courtship start.
        public static bool Fever { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Clear();

        public static void Set(LevelInfo info)
        {
            Info = info;
            Active = true;
        }

        public static void SetFever(bool on) => Fever = on;

        public static void Clear()
        {
            Active = false;
            Fever = false;
            Info = default;
        }
    }
}
