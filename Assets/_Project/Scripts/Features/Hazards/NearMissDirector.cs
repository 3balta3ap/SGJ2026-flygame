using TMPro;
using UnityEngine;

namespace FlyJam
{
    /// 굴러오는 장애물이 아슬아슬하게 다가오면 슬로모션 + "SPACE!" 표시. 그 사이에 회피하면 PERFECT.
    public class NearMissDirector : MonoBehaviour
    {
        public const string PerfectKey = "dodge.perfect";

        [SerializeField] SphereWalker player;      // 비우면 GameContext에서
        [SerializeField] GameSession session;      // 비우면 GameContext에서
        [SerializeField] GameObject promptUI;      // "SPACE!"
        [SerializeField] TMP_Text perfectText;     // "PERFECT!"
        [SerializeField] float triggerDistance = 1.8f;
        [SerializeField] float slowScale = 0.3f;
        [SerializeField] float windowRealtime = 0.5f;
        [SerializeField] float cooldown = 4f;

        float cd, windowEnd, perfectUntil;
        bool inWindow;

        public int PerfectCount { get; private set; }

        void OnEnable()
        {
            GameEvents.PlayerDodged += OnDodged;
            GameEvents.RoundEnded += OnRoundEnded;
        }

        void OnDisable()
        {
            GameEvents.PlayerDodged -= OnDodged;
            GameEvents.RoundEnded -= OnRoundEnded;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }

        void Start()
        {
            var ctx = GameContext.Current;
            if (ctx != null)
            {
                if (player == null) player = ctx.Player;
                if (session == null) session = ctx.Session;
            }
            if (promptUI != null) promptUI.SetActive(false);
            if (perfectText != null) perfectText.enabled = false;
        }

        void Update()
        {
            if (player == null || session == null) return;
            if (cd > 0f) cd -= Time.unscaledDeltaTime;

            if (inWindow && (Time.unscaledTime >= windowEnd || session.Ended || session.IsPaused))
                CloseWindow();

            if (!inWindow && cd <= 0f && !session.Ended && !session.IsPaused && player.State != FlyState.Dodging)
            {
                var all = RollingObstacle.Active;
                for (int i = 0; i < all.Count; i++)
                {
                    Vector3 to = player.transform.position - all[i].Position;
                    if (to.magnitude < triggerDistance && Vector3.Dot(all[i].Direction, to.normalized) > 0.6f)
                    {
                        OpenWindow();
                        break;
                    }
                }
            }

            if (perfectText != null) perfectText.enabled = Time.unscaledTime < perfectUntil;
        }

        void OpenWindow()
        {
            inWindow = true;
            windowEnd = Time.unscaledTime + windowRealtime;
            Time.timeScale = slowScale;
            Time.fixedDeltaTime = 0.02f * slowScale;
            if (promptUI != null) promptUI.SetActive(true);
        }

        void CloseWindow()
        {
            inWindow = false;
            cd = cooldown;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            if (promptUI != null) promptUI.SetActive(false);
        }

        void OnDodged(Vector3 pos, Vector3 dir)
        {
            if (!inWindow) return;
            PerfectCount++;
            perfectUntil = Time.unscaledTime + 0.8f;
            GameEvents.RaisePerfectDodge();
            CloseWindow();
        }

        void OnRoundEnded(bool cleared) => SessionResult.SetStat(PerfectKey, PerfectCount);
    }
}
