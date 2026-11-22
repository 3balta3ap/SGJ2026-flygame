using UnityEngine;

namespace FlyJam
{
    /// Planner numbers for the female (C03). Sheet tab "Mate".
    public class MateBalance : BalanceTable
    {
        public override string SheetName => "Mate";

        [Tooltip("Female speed (units per second). Player: Lv4 4.5, Lv5 4.8 at full HP")]
        [SerializeField] float speed = 4.4f;
        [Tooltip("Level at which the existing speech bubble and wandering leash become active")]
        [SerializeField] int shyUntilLevel = 3;
        static readonly Vector2 DefaultLv1 = new Vector2(10f, 15f);
        static readonly Vector2 DefaultLv5 = new Vector2(8f, 12f);
        static readonly Vector2 DefaultLv10 = new Vector2(5.5f, 8f);
        static readonly Vector2 DefaultLv15 = new Vector2(3f, 5f);

        [Tooltip("Lv.1 surface distances: X starts fleeing, Y releases")]
        [SerializeField] Vector2 fleeDistancesLv1 = DefaultLv1;
        [Tooltip("Lv.5 surface distances: X starts fleeing, Y releases")]
        [SerializeField] Vector2 fleeDistancesLv5 = DefaultLv5;
        [Tooltip("Lv.10 surface distances: X starts fleeing, Y releases")]
        [SerializeField] Vector2 fleeDistancesLv10 = DefaultLv10;
        [Tooltip("Lv.15 and above surface distances: X starts fleeing, Y releases")]
        [SerializeField] Vector2 fleeDistancesLv15 = DefaultLv15;
        [Tooltip("Seconds she keeps running after a failed courtship")]
        [SerializeField] float failFleeSeconds = 2f;
        [Tooltip("Roaming: seconds walking (min)")]
        [SerializeField] float walkTimeMin = 0.5f;
        [Tooltip("Roaming: seconds walking (max)")]
        [SerializeField] float walkTimeMax = 1.5f;
        [Tooltip("Roaming: seconds resting (min)")]
        [SerializeField] float pauseTimeMin = 3f;
        [Tooltip("Roaming: seconds resting (max)")]
        [SerializeField] float pauseTimeMax = 7f;
        [SerializeField, Min(0.1f)] float walkSpeedMin = 1f;
        [SerializeField, Min(0.1f)] float walkSpeedMax = 2f;
        [Tooltip("Roaming: random turn per walk, degrees")]
        [SerializeField] float wanderTurn = 70f;
        [Tooltip("If she is more than this many degrees around the planet from the player, her next walk heads back toward the player")]
        [SerializeField] float leashAngle = 100f;

        public float Speed => Mathf.Max(0f, speed);
        public int ShyUntilLevel => shyUntilLevel;
        public Vector2 FleeDistancesAtLevel(int level) =>
            InterpolateDistances(level, fleeDistancesLv1, fleeDistancesLv5, fleeDistancesLv10, fleeDistancesLv15);

        internal static Vector2 DefaultFleeDistancesAtLevel(int level) =>
            InterpolateDistances(level, DefaultLv1, DefaultLv5, DefaultLv10, DefaultLv15);

        static Vector2 InterpolateDistances(int level, Vector2 lv1, Vector2 lv5, Vector2 lv10, Vector2 lv15)
        {
            Vector2 distances;
            if (level <= 5) distances = Vector2.Lerp(lv1, lv5, Mathf.InverseLerp(1f, 5f, level));
            else if (level <= 10) distances = Vector2.Lerp(lv5, lv10, Mathf.InverseLerp(5f, 10f, level));
            else distances = Vector2.Lerp(lv10, lv15, Mathf.InverseLerp(10f, 15f, level));

            float start = Mathf.Max(0f, distances.x);
            return new Vector2(start, Mathf.Max(start + 0.5f, distances.y));
        }

        public float FailFleeSeconds => Mathf.Max(0f, failFleeSeconds);
        public float WalkTimeMin => Mathf.Max(0.1f, walkTimeMin);
        public float WalkTimeMax => Mathf.Max(WalkTimeMin, walkTimeMax);
        public float PauseTimeMin => Mathf.Max(0f, pauseTimeMin);
        public float PauseTimeMax => Mathf.Max(PauseTimeMin, pauseTimeMax);
        public float WalkSpeedMin => Mathf.Max(0.1f, walkSpeedMin);
        public float WalkSpeedMax => Mathf.Max(WalkSpeedMin, walkSpeedMax);
        public float WanderTurn => wanderTurn;
        public float LeashAngle => leashAngle;
    }
}
