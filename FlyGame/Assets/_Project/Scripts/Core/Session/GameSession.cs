using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace FlyJam
{
    /// Core round lifecycle only: clock, pause, end state, and transition to the Result scene.
    /// The round ends when a feature calls End (e.g., courtship -> "Love") or on GameEvents.PlayerDied.
    /// There is no time-up: 'dayLength' only drives Normalized (sky colour, hazard difficulty).
    public class GameSession : MonoBehaviour
    {
        [SerializeField] SphereWalker player;          // Auto-filled from GameContext if null
        [Tooltip("Seconds for Normalized to go 0 -> 1 (sky, difficulty). Does not end the round.")]
        [FormerlySerializedAs("timeLimit")]
        [SerializeField] float dayLength = 45f;
        [SerializeField] float resultDelay = 2.0f;

        readonly HashSet<object> pausers = new HashSet<object>();

        public float Elapsed { get; private set; }
        public float Remaining => Mathf.Max(0f, dayLength - Elapsed);
        public float Normalized => Mathf.Clamp01(Elapsed / dayLength);
        public bool Ended { get; private set; }
        /// True while any system holds a pause (e.g., courtship sequence). Freezes clock, sky progression, hazard spawning and HP.
        public bool IsPaused => pausers.Count > 0;
        public SphereWalker Player => player;

        void Awake()
        {
            if (player == null && GameContext.Current != null) player = GameContext.Current.Player;
        }

        void OnEnable() => GameEvents.PlayerDied += OnPlayerDied;
        void OnDisable() => GameEvents.PlayerDied -= OnPlayerDied;

        void Start()
        {
            Time.timeScale = 1f;
            GameEvents.RaiseRoundStarted();
        }

        void Update()
        {
            if (Ended || IsPaused) return;
            Elapsed += Time.deltaTime;
        }

        public void Pause(object by)
        {
            if (by != null) pausers.Add(by);
        }

        public void Resume(object by)
        {
            if (by != null) pausers.Remove(by);
        }

        void OnPlayerDied(string reason)
        {
            if (Ended) return;
            End(false, string.IsNullOrEmpty(reason) ? "Hit" : reason);
        }

        /// Ends the round. 'reason' selects the result screen title (text key "result.<reason lower case>").
        public void End(bool cleared, string reason)
        {
            if (Ended) return;
            Ended = true;
            pausers.Clear();
            if (player != null)
            {
                player.SetInputEnabled(false);
                if (!cleared) player.SetState(FlyState.Disabled);
            }

            SessionResult.Begin(cleared, Elapsed, reason);
            GameEvents.RaiseRoundEnded(cleared);   // Each feature records its own stats to SessionResult here
            StartCoroutine(GoToResultAfterDelay());
        }

        IEnumerator GoToResultAfterDelay()
        {
            yield return new WaitForSeconds(resultDelay);
            SceneFlow.GoToResult();
        }
    }
}
