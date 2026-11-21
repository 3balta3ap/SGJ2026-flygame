using UnityEngine;

namespace FlyJam
{
    /// Planner numbers for HP (S02-S04). Sheet tab "Health".
    /// All damage and healing are fractions of the current max HP. Max HP itself comes from the "Level" table when
    /// the Level feature is on (it grows with each level); maxHp here is the fallback without it.
    public class HealthBalance : BalanceTable
    {
        public override string SheetName => "Health";

        [Tooltip("Max HP when the Level feature is off (with it: the Level table's maxHp)")]
        [SerializeField] float maxHp = 10f;
        [Tooltip("HP at the start of a round")]
        [SerializeField] float startHp = 5f;
        [Tooltip("HP lost per second while exploring (not during events or pause). 0 = off")]
        [SerializeField] float drainPerSecond = 0.1f;
        [Tooltip("Damage (fraction of max HP) for obstacles without their own value")]
        [SerializeField] float obstacleDamageRatio = 0.2f;
        [Tooltip("Speed at 0 HP as a fraction of full-HP speed (1 = HP never changes speed). 0.85 = up to 15% slower")]
        [SerializeField] float speedAtZeroHp = 0.85f;
        [Tooltip("Seconds of invulnerability (blinking) after a hit")]
        [SerializeField] float invulnerableTime = 1.5f;
        [Tooltip("Blink speed while invulnerable: seconds per on/off step")]
        [SerializeField] float blinkInterval = 0.08f;
        [Tooltip("At or below this HP ratio the bar turns yellow (0-1)")]
        [SerializeField] float midHpRatio = 0.5f;
        [Tooltip("At or below this HP ratio the bar turns orange and blinks, plus the 'hungry' message (0-1)")]
        [SerializeField] float lowHpRatio = 0.2f;

        public float MaxHp => Mathf.Max(1f, maxHp);
        public float StartHp => Mathf.Clamp(startHp, 0.1f, MaxHp);
        public float DrainPerSecond => Mathf.Max(0f, drainPerSecond);
        public float ObstacleDamageRatio => Mathf.Max(0f, obstacleDamageRatio);
        public float SpeedAtZeroHp => Mathf.Clamp01(speedAtZeroHp);
        public float InvulnerableTime => Mathf.Max(0f, invulnerableTime);
        public float BlinkInterval => Mathf.Max(0.02f, blinkInterval);
        public float MidHpRatio => Mathf.Clamp01(midHpRatio);
        public float LowHpRatio => Mathf.Clamp01(lowHpRatio);
    }
}
