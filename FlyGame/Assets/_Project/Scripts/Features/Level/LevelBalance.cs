using UnityEngine;

namespace FlyJam
{
    /// Planner numbers for levels. Sheet tabs "Level" (key/value: EXP and fever) and "LevelTable" (one row per level).
    /// Each row: level, expToNext (0 = max level), maxSpeed, turnRate, maxHp, rareItemChance, courtLength, courtTime.
    public class LevelBalance : BalanceTable
    {
        public override string SheetName => "Level";

        [Tooltip("EXP for a successful dodge (回避!). Items give the EXP written in ItemTypes")]
        [SerializeField] int dodgeExp = 3;
        [Tooltip("Level-up fever: seconds of flying + invulnerability")]
        [SerializeField] float feverSeconds = 3f;
        [Tooltip("Fever: speed x this")]
        [SerializeField] float feverSpeedMul = 1.2f;
        [Tooltip("Fever: how high the fly floats above the ground")]
        [SerializeField] float feverLift = 1.0f;
        [Tooltip("Fever: up/down swing while floating")]
        [SerializeField] float feverBobHeight = 0.25f;
        [Tooltip("Fever: up/down swing speed (radians per second; 9 = about 1.4 swings per second)")]
        [SerializeField] float feverBobSpeed = 9f;
        [Tooltip("Fever: seconds to rise at the start and land at the end")]
        [SerializeField] float feverEaseSeconds = 0.3f;

        [SheetRows("LevelTable")]
        [SerializeField] LevelInfo[] levels =
        {
            new LevelInfo { level = 1,  expToNext = 5,  maxSpeed = 3.6f, turnRate = 70f,  maxHp = 10f, rareItemChance = 0.05f, courtLength = 8, courtTime = 2.5f },
            new LevelInfo { level = 2,  expToNext = 7,  maxSpeed = 3.9f, turnRate = 80f,  maxHp = 11f, rareItemChance = 0.08f, courtLength = 8, courtTime = 2.5f },
            new LevelInfo { level = 3,  expToNext = 9,  maxSpeed = 4.2f, turnRate = 90f,  maxHp = 12f, rareItemChance = 0.12f, courtLength = 8, courtTime = 3f },
            new LevelInfo { level = 4,  expToNext = 11,  maxSpeed = 4.5f, turnRate = 100f, maxHp = 13f, rareItemChance = 0.15f, courtLength = 7, courtTime = 3.5f },
            new LevelInfo { level = 5,  expToNext = 13,  maxSpeed = 4.8f, turnRate = 110f, maxHp = 14f, rareItemChance = 0.18f, courtLength = 6, courtTime = 5f },
            new LevelInfo { level = 6,  expToNext = 16,  maxSpeed = 5.1f, turnRate = 120f, maxHp = 15f, rareItemChance = 0.21f, courtLength = 5, courtTime = 6f },
            new LevelInfo { level = 7,  expToNext = 19, maxSpeed = 5.4f, turnRate = 130f, maxHp = 16f, rareItemChance = 0.24f, courtLength = 4, courtTime = 6f },
            new LevelInfo { level = 8,  expToNext = 22, maxSpeed = 5.7f, turnRate = 140f, maxHp = 17f, rareItemChance = 0.27f, courtLength = 4, courtTime = 0f },
            new LevelInfo { level = 9,  expToNext = 25, maxSpeed = 6.0f, turnRate = 150f, maxHp = 18f, rareItemChance = 0.30f, courtLength = 4, courtTime = 0f },
            new LevelInfo { level = 10, expToNext = 29,  maxSpeed = 6.3f, turnRate = 160f, maxHp = 19f, rareItemChance = 0.33f, courtLength = 4, courtTime = 0f },
            new LevelInfo { level = 11, expToNext = 33, maxSpeed = 6.45f, turnRate = 165f, maxHp = 20f, rareItemChance = 0.35f, courtLength = 4, courtTime = 0f },
            new LevelInfo { level = 12, expToNext = 37, maxSpeed = 6.60f, turnRate = 170f, maxHp = 21f, rareItemChance = 0.37f, courtLength = 4, courtTime = 0f },
            new LevelInfo { level = 13, expToNext = 41, maxSpeed = 6.75f, turnRate = 175f, maxHp = 22f, rareItemChance = 0.39f, courtLength = 4, courtTime = 0f },
            new LevelInfo { level = 14, expToNext = 45, maxSpeed = 6.90f, turnRate = 180f, maxHp = 23f, rareItemChance = 0.41f, courtLength = 4, courtTime = 0f },
            new LevelInfo { level = 15, expToNext = 0, maxSpeed = 7.05f, turnRate = 185f, maxHp = 24f, rareItemChance = 0.43f, courtLength = 4, courtTime = 0f },
        };

        public int DodgeExp => Mathf.Max(0, dodgeExp);
        public float FeverSeconds => Mathf.Max(0f, feverSeconds);
        public float FeverSpeedMul => Mathf.Max(0.1f, feverSpeedMul);
        public float FeverLift => feverLift;
        public float FeverBobHeight => feverBobHeight;
        public float FeverBobSpeed => feverBobSpeed;
        public float FeverEaseSeconds => Mathf.Max(0.01f, feverEaseSeconds);
        public LevelInfo[] Levels => levels;

        /// Highest level in the table (at least 1).
        public int MaxLevel
        {
            get
            {
                int m = 1;
                if (levels != null) foreach (var r in levels) if (r.level > m) m = r.level;
                return m;
            }
        }

        /// The row for this level. Missing row = the closest lower level's row (so a short table still works).
        public LevelInfo Row(int level)
        {
            LevelInfo best = new LevelInfo { level = 1, expToNext = 5, maxSpeed = 3.6f, turnRate = 70f, maxHp = 10f, courtLength = 4 };
            int bestLevel = int.MinValue;
            if (levels != null)
                foreach (var r in levels)
                    if (r.level <= level && r.level > bestLevel) { best = r; bestLevel = r.level; }
            best.level = level;
            if (level >= MaxLevel) best.expToNext = 0;
            return best;
        }
    }
}
