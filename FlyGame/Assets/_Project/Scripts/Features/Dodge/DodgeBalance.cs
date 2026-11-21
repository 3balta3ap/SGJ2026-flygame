using UnityEngine;

namespace FlyJam
{
    /// Planner numbers for the dash (P04). Sheet tab "Dodge".
    public class DodgeBalance : BalanceTable
    {
        public override string SheetName => "Dodge";

        [Tooltip("Dash distance (straight ahead, the way the fly is facing)")]
        [SerializeField] float distance = 3f;
        [Tooltip("Dash duration (seconds). Invulnerable during it, so it can pass through an enemy")]
        [SerializeField] float duration = 0.3f;
        [Tooltip("Seconds before the next dash")]
        [SerializeField] float cooldown = 0.4f;

        public float Distance => Mathf.Max(0.1f, distance);
        public float Duration => Mathf.Max(0.05f, duration);
        public float Cooldown => Mathf.Max(0f, cooldown);
    }
}
