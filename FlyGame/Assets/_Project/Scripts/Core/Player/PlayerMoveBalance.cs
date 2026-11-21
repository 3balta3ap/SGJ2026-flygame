using UnityEngine;

namespace FlyJam
{
    /// Planner numbers for player movement. Sheet tab "Player".
    /// Actual speed = maxSpeed x HP ratio (set by Health through SphereWalker.SpeedMultiplier).
    public class PlayerMoveBalance : BalanceTable
    {
        public override string SheetName => "Player";

        [Tooltip("Speed at full HP (units per second). Enemies and the female use this as 'player max speed'")]
        [SerializeField] float maxSpeed = 6f;
        [Tooltip("Turn speed at full stick tilt, degrees per second. Lower = wider arcs")]
        [SerializeField] float turnRate = 75f;
        [Tooltip("Stick tilt ignored below this (0-1)")]
        [SerializeField] float steerDeadZone = 0.15f;

        public float MaxSpeed => Mathf.Max(0.1f, maxSpeed);
        public float TurnRate => Mathf.Max(1f, turnRate);
        public float SteerDeadZone => Mathf.Clamp01(steerDeadZone);
    }
}
