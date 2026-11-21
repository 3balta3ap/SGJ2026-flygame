using UnityEngine;

namespace FlyJam
{
    /// Planner numbers for the female (C03). Sheet tab "Mate".
    public class MateBalance : BalanceTable
    {
        public override string SheetName => "Mate";

        [Tooltip("Female speed = player max speed x this")]
        [SerializeField] float speedRatio = 0.5f;
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

        public float SpeedRatio => Mathf.Max(0f, speedRatio);
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
