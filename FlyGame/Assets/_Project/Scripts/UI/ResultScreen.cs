using System.Text;
using TMPro;
using UnityEngine;

namespace FlyJam
{
    public class ResultScreen : MonoBehaviour
    {
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text detailText;
        [SerializeField] TMP_Text factText;

        void Start()
        {
            Time.timeScale = 1f;
            titleText.text = TitleFor(SessionResult.Reason, SessionResult.Cleared);

            var sb = new StringBuilder();
            if (SessionResult.TryGetStat("reaction.best", out float best))
            {
                sb.AppendLine(Loc.Format("result.best", best));
                sb.AppendLine(Loc.Get("result.lab"));
            }
            if (SessionResult.TryGetStat("dodge.perfect", out float perfect))
                sb.AppendLine(Loc.Format("result.dodges", perfect));
            sb.AppendLine();
            sb.Append(Loc.Get("result.controls"));
            detailText.text = sb.ToString();

            if (factText != null) factText.text = FlyFacts.Pick();
        }

        /// Title = text key "result.<reason in lower case>" (Love -> result.love, Hit -> result.hit).
        /// A new end reason only needs a new row in the sheet's Text tab.
        static string TitleFor(string reason, bool cleared)
        {
            if (!string.IsNullOrEmpty(reason))
            {
                string key = "result." + reason.ToLowerInvariant();
                if (Loc.Has(key)) return Loc.Get(key);
            }
            return Loc.Get(cleared ? "result.clear" : "result.failed");
        }

        void Update()
        {
            if (InputCompat.ConfirmPressed()) Retry();
            else if (InputCompat.BackPressed()) ToTitle();
        }

        public void Retry() => SceneFlow.GoToGame();
        public void ToTitle() => SceneFlow.GoToTitle();
    }
}
