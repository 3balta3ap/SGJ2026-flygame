using UnityEngine;

namespace FlyJam
{
    /// Planner numbers for the dodge (P04). Sheet tab "Dodge".
    public class DodgeBalance : BalanceTable
    {
        public override string SheetName => "Dodge";

        [Tooltip("Dodge distance")]
        [SerializeField] float distance = 2.2f;
        [Tooltip("Dodge duration (seconds). Invulnerable during it")]
        [SerializeField] float duration = 0.22f;
        [Tooltip("Seconds before the next dodge")]
        [SerializeField] float cooldown = 0.4f;
        [Tooltip("Body turn added during a sideways dodge, degrees")]
        [SerializeField] float yawKick = 25f;
        [Tooltip("Stick tilt below this counts as neutral")]
        [SerializeField] float steerDeadZone = 0.3f;
        [Tooltip("Neutral stick: 0 = step aside off the enemy's path (recommended), 1 = move the same way the enemy is moving (literal spec)")]
        [SerializeField] int neutralMode = 0;

        public float Distance => Mathf.Max(0f, distance);
        public float Duration => Mathf.Max(0.05f, duration);
        public float Cooldown => Mathf.Max(0f, cooldown);
        public float YawKick => yawKick;
        public float SteerDeadZone => Mathf.Clamp01(steerDeadZone);
        public int NeutralMode => neutralMode;
    }
}
