using UnityEngine;

namespace FlyJam
{
    /// Planner numbers for the round. Sheet tab "Session".
    public class SessionBalance : BalanceTable
    {
        public override string SheetName => "Session";

        [Tooltip("Seconds for one sky cycle (sunrise -> night). Repeats. Does not end the round")]
        [SerializeField] float dayLength = 80f;
        [Tooltip("Night starts at this point of the day (0-1). 0.778 of 80 s = 62 s")]
        [SerializeField] float nightStart = 0.778f;
        [Tooltip("Seconds between the end of play and the result panel")]
        [SerializeField] float resultDelay = 1.5f;

        public float DayLength => Mathf.Max(5f, dayLength);
        public float NightStart => Mathf.Clamp(nightStart, 0.5f, 0.95f);
        public float ResultDelay => Mathf.Max(0f, resultDelay);
    }
}
