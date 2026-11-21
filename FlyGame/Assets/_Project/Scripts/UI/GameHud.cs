using TMPro;
using UnityEngine;

namespace FlyJam
{
    /// Movement guide at the bottom centre (A03): "自動で進む　左スティックで曲がる" (keyboard: A・Dキー).
    /// Hides after the player has steered for a moment, or after maxShowSeconds.
    public class GameHud : MonoBehaviour
    {
        [SerializeField] GameObject guideRoot;
        [SerializeField] TMP_Text guideText;
        [SerializeField] float steerSecondsToHide = 0.6f;
        [SerializeField] float maxShowSeconds = 12f;

        float steered, shown;
        bool done;

        void Update()
        {
            if (done || guideRoot == null) return;
            if (guideText != null) guideText.text = Loc.Get(InputCompat.UsingPad ? "hud.guide.pad" : "hud.guide.kb");
            if (TimeControl.Paused) return;

            shown += Time.unscaledDeltaTime;
            if (Mathf.Abs(InputCompat.Steer()) > 0.3f) steered += Time.unscaledDeltaTime;
            if (steered >= steerSecondsToHide || shown >= maxShowSeconds)
            {
                done = true;
                guideRoot.SetActive(false);
            }
        }
    }
}
