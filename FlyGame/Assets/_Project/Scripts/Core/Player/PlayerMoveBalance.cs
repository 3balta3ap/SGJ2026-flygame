using UnityEngine;

namespace FlyJam
{
    /// Planner numbers for player movement. Sheet tab "Player".
    /// With the Level feature (SYS_Level) on, speed and turn rate come from the "Level" table instead;
    /// these two are the fallback when it is off. Health scales speed slightly (Health.speedAtZeroHp).
    public class PlayerMoveBalance : BalanceTable
    {
        public override string SheetName => "Player";

        [Tooltip("Fallback speed when the Level feature is off (units per second)")]
        [SerializeField] float maxSpeed = 3.6f;
        [Tooltip("Fallback turn speed at full stick tilt when the Level feature is off, degrees per second")]
        [SerializeField] float turnRate = 70f;
        [Tooltip("Stick tilt ignored below this (0-1)")]
        [SerializeField] float steerDeadZone = 0.15f;

        public float MaxSpeed => Mathf.Max(0.1f, maxSpeed);
        public float TurnRate => Mathf.Max(1f, turnRate);
        public float SteerDeadZone => Mathf.Clamp01(steerDeadZone);
    }
}
