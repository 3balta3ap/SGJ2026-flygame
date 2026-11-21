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
                sb.AppendLine($"Your best dodge reaction: {best:0.00}s");
                sb.AppendLine("Fruit fly (lab data): about 0.2-0.3s");
            }
            if (SessionResult.TryGetStat("dodge.perfect", out float perfect))
                sb.AppendLine($"PERFECT dodges: {perfect:0}");
            sb.AppendLine();
            sb.Append("SPACE: Retry   ESC: Title");
            detailText.text = sb.ToString();

            if (factText != null) factText.text = FlyFacts.Pick();
        }

        static string TitleFor(string reason, bool cleared)
        {
            switch (reason)
            {
                case "Love": return "LOVE!";
                case "Hit": return "OUCH!";
                case "TimeUp": return "TIME UP";
                default: return cleared ? "CLEAR!" : "FAILED";
            }
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
