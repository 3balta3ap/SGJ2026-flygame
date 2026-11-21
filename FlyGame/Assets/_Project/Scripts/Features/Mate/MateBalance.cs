using UnityEngine;

namespace FlyJam
{
    /// Planner numbers for the female (C03). Sheet tab "Mate".
    /// Before shyUntilLevel she is shy: she keeps far away (shyFleeRadius) so you cannot reach her early. The heart marker still shows where she is.
    public class MateBalance : BalanceTable
    {
        public override string SheetName => "Mate";

        [Tooltip("Female speed (units per second). Player: Lv4 4.5, Lv5 4.8 at full HP")]
        [SerializeField] float speed = 4.4f;
        [Tooltip("Below this player level she is shy (keeps far away). At this level she starts acting normally")]
        [SerializeField] int shyUntilLevel = 3;
        [Tooltip("Shy: she runs away when the player is this close (straight line). 18 = about a quarter of the planet")]
        [SerializeField] float shyFleeRadius = 18f;
        [Tooltip("Shy: she stops running when the player is this far (straight line)")]
        [SerializeField] float shyReleaseRadius = 20f;
        [Tooltip("She starts running away when the player is this close (straight line)")]
        [SerializeField] float fleeRadius = 5f;
        [Tooltip("She goes back to roaming when the player is this far (straight line). Keep it bigger than fleeRadius")]
        [SerializeField] float releaseRadius = 8f;
        [Tooltip("Seconds she keeps running after a failed courtship")]
        [SerializeField] float failFleeSeconds = 2f;
        [Tooltip("Roaming: seconds walking (min)")]
        [SerializeField] float walkTimeMin = 1.5f;
        [Tooltip("Roaming: seconds walking (max)")]
        [SerializeField] float walkTimeMax = 3f;
        [Tooltip("Roaming: seconds resting (min)")]
        [SerializeField] float pauseTimeMin = 0.4f;
        [Tooltip("Roaming: seconds resting (max)")]
        [SerializeField] float pauseTimeMax = 1.0f;
        [Tooltip("Roaming: random turn per walk, degrees")]
        [SerializeField] float wanderTurn = 70f;
        [Tooltip("If she is more than this many degrees around the planet from the player, her next walk heads back toward the player")]
        [SerializeField] float leashAngle = 100f;

        public float Speed => Mathf.Max(0f, speed);
        public int ShyUntilLevel => shyUntilLevel;
        public float ShyFleeRadius => Mathf.Max(0f, shyFleeRadius);
        public float ShyReleaseRadius => Mathf.Max(ShyFleeRadius + 0.5f, shyReleaseRadius);
        public float FleeRadius => Mathf.Max(0f, fleeRadius);
        public float ReleaseRadius => Mathf.Max(FleeRadius + 0.5f, releaseRadius);
        public float FailFleeSeconds => Mathf.Max(0f, failFleeSeconds);
        public float WalkTimeMin => Mathf.Max(0.1f, walkTimeMin);
        public float WalkTimeMax => Mathf.Max(WalkTimeMin, walkTimeMax);
        public float PauseTimeMin => Mathf.Max(0f, pauseTimeMin);
        public float PauseTimeMax => Mathf.Max(PauseTimeMin, pauseTimeMax);
        public float WanderTurn => wanderTurn;
        public float LeashAngle => leashAngle;
    }
}
