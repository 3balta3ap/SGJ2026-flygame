using UnityEngine;

namespace FlyJam
{
    /// Planner numbers for the round. Sheet tab "Session".
    public class SessionBalance : BalanceTable
    {
        public override string SheetName => "Session";

        [Tooltip("Seconds for one sky cycle (sunrise -> night). Repeats. Does not end the round")]
        [SerializeField] float dayLength = 60f;
        [Tooltip("Seconds between the end of play and the result panel")]
        [SerializeField] float resultDelay = 1.5f;

        public float DayLength => Mathf.Max(5f, dayLength);
        public float ResultDelay => Mathf.Max(0f, resultDelay);
    }
}
