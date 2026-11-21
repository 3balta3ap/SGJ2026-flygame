using UnityEngine;

namespace FlyJam
{
    /// Planner numbers for the mash battle with a male (C07, E05, P05). Sheet tab "Rival".
    public class RivalBalance : BalanceTable
    {
        public override string SheetName => "Rival";

        [Tooltip("Presses needed to break free")]
        [SerializeField] int targetPresses = 25;
        [Tooltip("Seconds to reach it")]
        [SerializeField] float timeLimit = 6f;
        [Tooltip("HP lost when time runs out, fraction of max HP (0.1 of 10 = -1)")]
        [SerializeField] float failDamageRatio = 0.1f;
        [Tooltip("Presses in the first moment are ignored (presses from before the battle do not count)")]
        [SerializeField] float inputDelay = 0.2f;
        [Tooltip("Seconds the result message stays")]
        [SerializeField] float resultShowSeconds = 1.0f;

        public int TargetPresses => Mathf.Max(1, targetPresses);
        public float TimeLimit => Mathf.Max(0.5f, timeLimit);
        public float FailDamageRatio => Mathf.Max(0f, failDamageRatio);
        public float InputDelay => Mathf.Max(0f, inputDelay);
        public float ResultShowSeconds => Mathf.Max(0f, resultShowSeconds);
    }
}
