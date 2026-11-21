using UnityEngine;

namespace FlyJam
{
    /// Planner numbers for HP. Sheet tab "Health".
    public class HealthBalance : BalanceTable
    {
        public override string SheetName => "Health";

        [Tooltip("Max and starting HP")]
        [SerializeField] float maxHp = 100f;
        [Tooltip("HP lost per second over time (hunger). 0 = off")]
        [SerializeField] float drainPerSecond = 1.5f;
        [Tooltip("HP lost when hit by an obstacle without its own damage value (acorns)")]
        [SerializeField] float obstacleDamage = 30f;
        [Tooltip("Seconds of invulnerability (blinking) after a hit")]
        [SerializeField] float invulnerableTime = 1.5f;
        [Tooltip("Blink speed while invulnerable: seconds per on/off step")]
        [SerializeField] float blinkInterval = 0.08f;
        [Tooltip("HP bar turns red and blinks at or below this ratio (0-1)")]
        [SerializeField] float lowHpRatio = 0.3f;

        public float MaxHp => Mathf.Max(1f, maxHp);
        public float DrainPerSecond => Mathf.Max(0f, drainPerSecond);
        public float ObstacleDamage => Mathf.Max(0f, obstacleDamage);
        public float InvulnerableTime => Mathf.Max(0f, invulnerableTime);
        public float BlinkInterval => Mathf.Max(0.02f, blinkInterval);
        public float LowHpRatio => Mathf.Clamp01(lowHpRatio);
    }
}
