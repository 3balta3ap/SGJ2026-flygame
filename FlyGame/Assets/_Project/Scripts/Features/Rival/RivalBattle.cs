using System.Collections;
using TMPro;
using UnityEngine;

namespace FlyJam
{
    /// Mash battle with a male (C07, E05, P05, A05).
    /// Starts on GameEvents.RivalEncounter (raised by the male enemy on contact). The world pauses (session.Pause):
    /// no movement, no enemies, no HP drain. Each new press of the action button counts once (holding does not repeat).
    /// Reach targetPresses within timeLimit -> free. Otherwise HP -failDamageRatio, then back to exploring if HP is left.
    public class RivalBattle : MonoBehaviour
    {
        [SerializeField] RivalBalance balance;
        [SerializeField] GameSession session;                 // Auto-filled from GameContext if null
        [SerializeField] SphereWalker player;                 // Auto-filled from GameContext if null
        [SerializeField] PlanetFollowCamera followCamera;     // Auto-filled from GameContext if null
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text titleText;                  // "オスと連打バトル！" -> result
        [SerializeField] TMP_Text promptText;                 // "Ⓐを連打！"
        [SerializeField] TMP_Text countText;                  // "12／25"
        [SerializeField] RectTransform gaugeFill;             // stretched by anchorMax.x = count / target
        [SerializeField] RectTransform timerFill;             // stretched by anchorMax.x = time left / limit

        public bool Running { get; private set; }

        void Start()
        {
            var ctx = GameContext.Current;
            if (ctx != null)
            {
                if (session == null) session = ctx.Session;
                if (player == null) player = ctx.Player;
                if (followCamera == null) followCamera = ctx.FollowCamera;
            }
            if (panel != null) panel.SetActive(false);
        }

        void OnEnable() => GameEvents.RivalEncounter += OnEncounter;
        void OnDisable() => GameEvents.RivalEncounter -= OnEncounter;

        void OnEncounter(IObstacle male)
        {
            if (Running || session == null || player == null) return;
            if (session.Ended || session.IsPaused || player.State == FlyState.Disabled) return;
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            Running = true;
            int target = balance != null ? balance.TargetPresses : 25;
            float limit = balance != null ? balance.TimeLimit : 6f;
            float wait = balance != null ? balance.InputDelay : 0.2f;
            float left = limit;
            int count = 0;

            session.Pause(this);
            player.SetInputEnabled(false);
            if (followCamera != null) followCamera.SuppressLookOut = true;
            panel.SetActive(true);
            titleText.text = Loc.Get("rival.title");
            Refresh(count, target, left, limit);
            GameEvents.RaiseRivalBattleStarted();

            bool won;
            while (true)
            {
                yield return null;
                Refresh(count, target, left, limit);
                if (TimeControl.InputBlocked) continue;       // pause menu open
                float dt = Time.deltaTime;
                if (wait > 0f) { wait -= dt; continue; }

                left -= dt;
                if (InputCompat.ActionPressed())
                {
                    count++;
                    GameEvents.RaiseRivalPressed(count, target);
                }
                if (count >= target) { won = true; break; }
                if (left <= 0f) { won = false; break; }
            }

            left = Mathf.Max(0f, left);
            Refresh(count, target, left, limit);
            titleText.text = Loc.Get(won ? "rival.win" : "rival.lose");
            promptText.text = "";

            float show = balance != null ? balance.ResultShowSeconds : 1f;
            while (show > 0f)
            {
                yield return null;
                if (!TimeControl.Paused) show -= Time.deltaTime;
            }

            panel.SetActive(false);
            if (followCamera != null) followCamera.SuppressLookOut = false;
            session.Resume(this);
            player.SetInputEnabled(true);
            Running = false;
            GameEvents.RaiseRivalBattleEnded(won);
            if (!won) GameEvents.RaiseDamageRequested(balance != null ? balance.FailDamageRatio : 0.1f, "Hit");
        }

        void Refresh(int count, int target, float left, float limit)
        {
            if (promptText != null && count < target) promptText.text = Loc.Format("rival.prompt", InputCompat.ActionLabel);
            if (countText != null) countText.text = Loc.Format("rival.count", count, target);
            if (gaugeFill != null) gaugeFill.anchorMax = new Vector2(Mathf.Clamp01((float)count / target), gaugeFill.anchorMax.y);
            if (timerFill != null) timerFill.anchorMax = new Vector2(Mathf.Clamp01(left / limit), timerFill.anchorMax.y);
        }
    }
}
