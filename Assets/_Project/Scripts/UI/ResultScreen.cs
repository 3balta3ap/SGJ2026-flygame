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
            titleText.text = SessionResult.Cleared ? "LOVE!" : (SessionResult.Reason == "Hit" ? "OUCH!" : "TIME UP");

            string best = SessionResult.BestReaction >= 0f ? $"{SessionResult.BestReaction:0.00}s" : "-";
            detailText.text =
                $"Your best dodge reaction: {best}\n" +
                "Fruit fly (lab data): about 0.2-0.3s\n" +
                $"PERFECT dodges: {SessionResult.PerfectCount}\n\n" +
                "SPACE: Retry   ESC: Title";

            if (factText != null) factText.text = FlyFacts.Pick();
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
