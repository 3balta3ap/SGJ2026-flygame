using UnityEngine;

namespace FlyJam
{
    /// Planner numbers for enemies (C04-C08, E01, E02). Sheet tabs: "Hazards" (key/value) and "HazardTypes" (one row per enemy type).
    public class HazardBalance : BalanceTable
    {
        public override string SheetName => "Hazards";

        [System.Serializable]
        public struct TypeRow
        {
            public string id;            // must match an id in ObstacleSpawner.prefabs (dragonfly, spider, male, acorn)
            public int count;            // how many of this type circle the planet
            public float speedRatio;     // speed = player max speed x this
            public float damageRatio;    // HP lost on contact, fraction of max HP (ignored when rival = 1)
            public bool warn;            // 1 = approaching triggers the warning (red frame, dodge allowed)
            public bool rival;           // 1 = contact starts the mash battle instead of damage (male)
            public bool chase;           // 1 = turns toward the player when close
            public float detectRadius;   // chase starts within this straight-line distance
            public float loseRadius;     // chase stops beyond this straight-line distance
            public float turnRate;       // degrees per second while chasing
            public float stopOnHit;      // seconds it stays put after touching the player (harmless meanwhile). 0 = passes through
        }

        [Tooltip("Seconds before the first enemy appears")]
        [SerializeField] float firstDelay = 2f;
        [Tooltip("Enemies appear one by one, this many seconds apart, until every type reaches its count")]
        [SerializeField] float spawnInterval = 1.5f;
        [Tooltip("Spawn point: at least this many degrees around the planet away from the player")]
        [SerializeField] float spawnAngleMin = 50f;
        [Tooltip("Spawn point: at most this many degrees away (180 = the opposite side)")]
        [SerializeField] float spawnAngleMax = 170f;
        [Tooltip("Warning: an approaching enemy within this distance (straight line)")]
        [SerializeField] float warnRadius = 3f;
        [Tooltip("Warning: how directly it must be heading at the player (1 = exactly, 0 = sideways)")]
        [SerializeField] float warnApproachDot = 0.5f;
        [Tooltip("Game speed during a warning (slow motion). 1 = off")]
        [SerializeField] float slowScale = 0.5f;
        [Tooltip("Seconds after a dodge to wait for the enemy to pass before giving up on 'dodge success'")]
        [SerializeField] float successTimeout = 3f;

        [SheetRows("HazardTypes")]
        [SerializeField] TypeRow[] types =
        {
            new TypeRow { id = "dragonfly", count = 2, speedRatio = 0.7f, damageRatio = 0.2f, warn = true, stopOnHit = 2f },
            new TypeRow { id = "spider",    count = 2, speedRatio = 0.7f, damageRatio = 0.2f, warn = true, stopOnHit = 2f },
            new TypeRow { id = "male",      count = 1, speedRatio = 0.7f, damageRatio = 0f,   warn = true, rival = true,
                          chase = true, detectRadius = 6f, loseRadius = 9f, turnRate = 120f, stopOnHit = 2f },
            new TypeRow { id = "acorn",     count = 1, speedRatio = 0.7f, damageRatio = 0.1f, warn = true },
        };

        public float FirstDelay => Mathf.Max(0f, firstDelay);
        public float SpawnInterval => Mathf.Max(0.1f, spawnInterval);
        public float SpawnAngleMin => Mathf.Clamp(spawnAngleMin, 0f, 180f);
        public float SpawnAngleMax => Mathf.Clamp(Mathf.Max(spawnAngleMin, spawnAngleMax), 0f, 180f);
        public float WarnRadius => Mathf.Max(0.5f, warnRadius);
        public float WarnApproachDot => Mathf.Clamp(warnApproachDot, -1f, 1f);
        public float SlowScale => Mathf.Clamp(slowScale, 0.05f, 1f);
        public float SuccessTimeout => Mathf.Max(0.1f, successTimeout);
        public TypeRow[] Types => types;
    }
}
