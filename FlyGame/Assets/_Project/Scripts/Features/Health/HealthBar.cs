using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlyJam
{
    /// HUD top-left (A03): HP gauge, "5/10" numbers, "+2"/"-2" change popup, low-HP message.
    /// The popup shows the real change (near the cap a 2-point apple may show "+1"); the slow drain is not shown.
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] HealthBalance balance;
        [SerializeField] RectTransform fill;            // stretched by anchorMax.x = HP ratio
        [SerializeField] Image fillImage;
        [SerializeField] TMP_Text valueText;            // "5/10"
        [SerializeField] TMP_Text deltaText;            // "+2" / "-2"
        [SerializeField] GameObject lowHpMessage;       // LocalizedText "hud.lowhp" ("りんごを探そう！")
        [SerializeField] Color normalColor = new Color(0.36f, 0.62f, 0.40f, 1f);
        [SerializeField] Color lowColor = new Color(0.85f, 0.25f, 0.22f, 1f);
        [SerializeField] Color lowDimColor = new Color(0.55f, 0.15f, 0.15f, 1f);
        [SerializeField] Color healColor = new Color(0.25f, 0.6f, 0.3f, 1f);
        [SerializeField] Color damageColor = new Color(0.8f, 0.2f, 0.2f, 1f);
        [SerializeField] float lowBlinkPeriod = 0.6f;
        [SerializeField] float deltaShowSeconds = 1.2f;
        [SerializeField] float deltaMin = 0.05f;        // smaller changes (per-frame drain) are not shown

        float ratio = 1f;
        float last = -1f;
        float deltaLeft;

        void OnEnable() => GameEvents.HealthChanged += OnChanged;
        void OnDisable() => GameEvents.HealthChanged -= OnChanged;

        void Start()
        {
            if (deltaText != null) deltaText.gameObject.SetActive(false);
        }

        void OnChanged(float current, float max)
        {
            ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            if (fill != null) fill.anchorMax = new Vector2(ratio, fill.anchorMax.y);
            if (valueText != null) valueText.text = Mathf.CeilToInt(current - 0.0001f) + "/" + Mathf.RoundToInt(max);

            if (last >= 0f)
            {
                float d = current - last;
                if (Mathf.Abs(d) >= deltaMin && deltaText != null)
                {
                    string n = d.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
                    deltaText.text = d > 0f ? "+" + n : n;
                    deltaText.color = d > 0f ? healColor : damageColor;
                    deltaText.gameObject.SetActive(true);
                    deltaLeft = deltaShowSeconds;
                }
            }
            last = current;
        }

        void Update()
        {
            float low = balance != null ? balance.LowHpRatio : 0.3f;
            bool isLow = ratio <= low;
            if (lowHpMessage != null && lowHpMessage.activeSelf != isLow) lowHpMessage.SetActive(isLow);

            if (fillImage != null)
            {
                if (!isLow) fillImage.color = normalColor;
                else fillImage.color = Mathf.Repeat(Time.unscaledTime, lowBlinkPeriod) < lowBlinkPeriod * 0.5f ? lowColor : lowDimColor;
            }

            if (deltaLeft > 0f)
            {
                deltaLeft -= Time.unscaledDeltaTime;
                if (deltaLeft <= 0f && deltaText != null) deltaText.gameObject.SetActive(false);
            }
        }
    }
}
