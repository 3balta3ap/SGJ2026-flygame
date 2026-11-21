using UnityEngine;

namespace FlyJam
{
    /// Planner numbers for the courtship button order (S06, P03, E03). Sheet tab "Courtship".
    public class CourtshipBalance : BalanceTable
    {
        public override string SheetName => "Courtship";

        [Tooltip("How many buttons to press in order")]
        [SerializeField] int length = 4;
        [Tooltip("How many different buttons can appear (1-4). 4 = A/B/X/Y")]
        [SerializeField] int buttonKinds = 4;
        [Tooltip("Seconds to finish the whole order. 0 = no limit")]
        [SerializeField] float timeLimit = 0f;
        [Tooltip("Presses in the first moment are ignored (presses from before the event do not count)")]
        [SerializeField] float inputDelay = 0.3f;
        [Tooltip("Seconds the success / failure message stays")]
        [SerializeField] float resultShowSeconds = 1.2f;

        public int Length => Mathf.Clamp(length, 1, 8);
        public int ButtonKinds => Mathf.Clamp(buttonKinds, 1, 4);
        public float TimeLimit => Mathf.Max(0f, timeLimit);
        public float InputDelay => Mathf.Max(0f, inputDelay);
        public float ResultShowSeconds => Mathf.Max(0f, resultShowSeconds);
    }
}
