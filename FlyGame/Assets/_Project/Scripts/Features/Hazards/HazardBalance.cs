using UnityEngine;

namespace FlyJam
{
    /// Planner numbers for enemies. Sheet tabs: "Hazards" (spawner, key/value) and "HazardTypes" (one row per enemy type).
    public class HazardBalance : BalanceTable
    {
        public override string SheetName => "Hazards";

        [System.Serializable]
        public struct TypeRow
        {
            public string id;            // must match an id in ObstacleSpawner.prefabs (dragonfly, spider, male, acorn)
            public float weight;         // spawn chance weight. 0 = never spawns
            public float speedRatio;     // speed = player base move speed x this
            public bool chase;           // 1 = turns toward the player when close
            public float detectRadius;   // chase starts within this straight-line distance
            public float loseRadius;     // chase stops beyond this straight-line distance
            public float turnRate;       // degrees per second while chasing
            public float stopOnHit;      // seconds it stays put after hitting the player (harmless meanwhile). 0 = passes through
        }

        [Tooltip("Seconds before the first enemy appears")]
        [SerializeField] float firstDelay = 3f;
        [Tooltip("Seconds between spawns at the start of the day")]
        [SerializeField] float intervalStart = 3.5f;
        [Tooltip("Seconds between spawns at the end of the day")]
        [SerializeField] float intervalEnd = 1.4f;
        [Tooltip("Max enemies alive at once")]
        [SerializeField] int maxActive = 6;
        [Tooltip("Spawn point: degrees around the planet away from the player (min)")]
        [SerializeField] float spawnAngleMin = 35f;
        [Tooltip("Spawn point: degrees around the planet away from the player (max)")]
        [SerializeField] float spawnAngleMax = 60f;
        [Tooltip("Random aim error toward the player, degrees")]
        [SerializeField] float aimJitter = 15f;
        [Tooltip("Distance travelled before disappearing, x half the planet circumference")]
        [SerializeField] float travelRatio = 0.6f;

        [SheetRows("HazardTypes")]
        [SerializeField] TypeRow[] types =
        {
            new TypeRow { id = "dragonfly", weight = 40f, speedRatio = 0.7f, stopOnHit = 2f },
            new TypeRow { id = "spider",    weight = 40f, speedRatio = 0.7f, stopOnHit = 2f },
            new TypeRow { id = "male",      weight = 20f, speedRatio = 0.7f, chase = true,
                          detectRadius = 6f, loseRadius = 9f, turnRate = 120f, stopOnHit = 2f },
            new TypeRow { id = "acorn",     weight = 0f,  speedRatio = 1.0f },
        };

        public float FirstDelay => Mathf.Max(0f, firstDelay);
        public float IntervalStart => Mathf.Max(0.1f, intervalStart);
        public float IntervalEnd => Mathf.Max(0.1f, intervalEnd);
        public int MaxActive => Mathf.Max(0, maxActive);
        public float SpawnAngleMin => spawnAngleMin;
        public float SpawnAngleMax => Mathf.Max(spawnAngleMin, spawnAngleMax);
        public float AimJitter => aimJitter;
        public float TravelRatio => Mathf.Max(0.05f, travelRatio);
        public TypeRow[] Types => types;
    }
}
