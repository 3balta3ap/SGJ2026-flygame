using UnityEngine;

namespace FlyJam
{
    /// Planner numbers for HP (S02, S03). Sheet tab "Health".
    public class HealthBalance : BalanceTable
    {
        public override string SheetName => "Health";

        [Tooltip("Max HP")]
        [SerializeField] float maxHp = 10f;
        [Tooltip("HP at the start of a round")]
        [SerializeField] float startHp = 5f;
        [Tooltip("HP lost per second while exploring (not during events or pause). 0 = off")]
        [SerializeField] float drainPerSecond = 0.1f;
        [Tooltip("Damage (fraction of max HP) for obstacles without their own value")]
        [SerializeField] float obstacleDamageRatio = 0.2f;
        [Tooltip("Speed never drops below this fraction of max speed (0 = exactly max speed x HP ratio)")]
        [SerializeField] float minSpeedRatio = 0.3f;
        [Tooltip("Seconds of invulnerability (blinking) after a hit")]
        [SerializeField] float invulnerableTime = 1.5f;
        [Tooltip("Blink speed while invulnerable: seconds per on/off step")]
        [SerializeField] float blinkInterval = 0.08f;
        [Tooltip("At or below this HP ratio: red bar + 'find an apple' message (0-1)")]
        [SerializeField] float lowHpRatio = 0.3f;

        public float MaxHp => Mathf.Max(0.1f, maxHp);
        public float StartHp => Mathf.Clamp(startHp, 0.01f, MaxHp);
        public float DrainPerSecond => Mathf.Max(0f, drainPerSecond);
        public float ObstacleDamageRatio => Mathf.Max(0f, obstacleDamageRatio);
        public float MinSpeedRatio => Mathf.Clamp01(minSpeedRatio);
        public float InvulnerableTime => Mathf.Max(0f, invulnerableTime);
        public float BlinkInterval => Mathf.Max(0.02f, blinkInterval);
        public float LowHpRatio => Mathf.Clamp01(lowHpRatio);
    }
}
