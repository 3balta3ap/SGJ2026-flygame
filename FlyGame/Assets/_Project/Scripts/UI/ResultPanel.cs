using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;

namespace FlyJam
{
    /// Game over / clear panel inside the Game scene (S04, S05, S06, A06). Replaces the old Result scene.
    /// Title  = text key "result.<reason>"      (result.love = 求愛成功！, result.hit / result.starved = 体力がなくなった！)
    /// Sub    = text key "result.<reason>.sub"  (メスのハエに出会えた / 赤りんごで体力を回復しよう)
    /// Below: dodge reaction stats and one FLY FACT line. Buttons: もういちど (Retry), タイトルに戻る (ToTitle).
    public class ResultPanel : MonoBehaviour
    {
        [SerializeField] GameSession session;        // Auto-filled from GameContext if null
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text subText;
        [SerializeField] TMP_Text statsText;
        [SerializeField] TMP_Text factText;

        void Start()
        {
            if (session == null && GameContext.Current != null) session = GameContext.Current.Session;
            if (panel != null) panel.SetActive(false);
        }

        void OnEnable() => GameEvents.RoundEnded += OnRoundEnded;
        void OnDisable() => GameEvents.RoundEnded -= OnRoundEnded;

        void OnRoundEnded(bool cleared) => StartCoroutine(Show());

        IEnumerator Show()
        {
            yield return new WaitForSecondsRealtime(session != null ? session.ResultDelay : 1.5f);
            Fill();
            if (panel != null) panel.SetActive(true);
        }

        void Fill()
        {
            string reason = string.IsNullOrEmpty(SessionResult.Reason) ? "" : SessionResult.Reason.ToLowerInvariant();
            string key = "result." + reason;
            bool known = reason.Length > 0 && Loc.Has(key);
            titleText.text = known ? Loc.Get(key) : Loc.Get(SessionResult.Cleared ? "result.clear" : "result.failed");
            if (subText != null) subText.text = known && Loc.Has(key + ".sub") ? Loc.Get(key + ".sub") : "";

            if (statsText != null)
            {
                var sb = new StringBuilder();
                if (SessionResult.TryGetStat("level", out float level) && level > 0f)
                    sb.AppendLine(Loc.Format("result.level", level));
                if (SessionResult.TryGetStat("reaction.best", out float best))
                {
                    sb.AppendLine(Loc.Format("result.best", best));
                    sb.AppendLine(Loc.Get("result.lab"));
                }
                if (SessionResult.TryGetStat("dodge.perfect", out float dodges) && dodges > 0f)
                    sb.AppendLine(Loc.Format("result.dodges", dodges));
                statsText.text = sb.ToString().TrimEnd();
            }
            if (factText != null) factText.text = FlyFacts.Pick();
        }

        public void Retry() => SceneFlow.GoToGame();
        public void ToTitle() => SceneFlow.GoToTitle();
    }
}
