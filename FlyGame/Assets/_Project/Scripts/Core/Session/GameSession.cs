using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace FlyJam
{
    /// Core round lifecycle only: clock, pause holders, end state.
    /// The round ends when a feature calls End (courtship -> "Love") or on GameEvents.PlayerDied ("Hit" / "Starved").
    /// No scene change here: the result panel (UI) listens to RoundEnded.
    public class GameSession : MonoBehaviour
    {
        [SerializeField] SphereWalker player;          // Auto-filled from GameContext if null
        [SerializeField] SessionBalance balance;
        [Tooltip("Used only when 'balance' is empty")]
        [FormerlySerializedAs("timeLimit")]
        [SerializeField] float dayLength = 45f;
        [Tooltip("Used only when 'balance' is empty")]
        [SerializeField] float resultDelay = 2.0f;
        [Tooltip("Used only when 'balance' is empty")]
        [SerializeField] float nightStart = 0.778f;

        readonly HashSet<object> pausers = new HashSet<object>();

        public float Elapsed { get; private set; }
        public float DayLength => balance != null ? balance.DayLength : Mathf.Max(5f, dayLength);
        public float ResultDelay => balance != null ? balance.ResultDelay : resultDelay;
        public float NightStart => balance != null ? balance.NightStart : nightStart;
        public float Remaining => Mathf.Max(0f, DayLength - Elapsed);
        /// 0 on the first day, 1 on the second ...
        public int DayIndex => Mathf.FloorToInt(Elapsed / DayLength);
        /// True from NightStart to the end of each day.
        public bool IsNight => DayProgress >= NightStart;
        /// 0 -> 1 over the first day, then stays 1.
        public float Normalized => Mathf.Clamp01(Elapsed / DayLength);
        /// 0 -> 1 every day, repeating (sky colour).
        public float DayProgress => Mathf.Repeat(Elapsed / DayLength, 1f);
        public bool Ended { get; private set; }
        /// True while anything holds a pause: courtship, mash battle, pause menu.
        /// Freezes the clock, sky, enemies, apples and HP.
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
            TimeControl.ResetAll();
            GameEvents.RaiseRoundStarted();
        }

        bool wasNight;

        void Update()
        {
            if (Ended || IsPaused) return;
            Elapsed += Time.deltaTime;
            bool night = IsNight;
            if (night != wasNight)
            {
                wasNight = night;
                GameEvents.RaiseNightChanged(night, DayIndex);
            }
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

        /// Ends the round once. 'reason' picks the result title (text key "result.<reason lower case>").
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
            GameEvents.RaiseRoundEnded(cleared);   // features record stats here; ResultPanel shows after ResultDelay
        }
    }
}
