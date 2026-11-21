using UnityEngine;
using UnityEngine.UI;

namespace FlyJam
{
    /// HP bar view. 'fill' is stretched by its right anchor (anchorMax.x = HP ratio).
    /// At or below HealthBalance.lowHpRatio the fill turns red and blinks.
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] HealthBalance balance;
        [SerializeField] RectTransform fill;
        [SerializeField] Image fillImage;
        [SerializeField] Color normalColor = new Color(0.49f, 0.85f, 0.34f, 1f);
        [SerializeField] Color lowColor = new Color(1f, 0.35f, 0.35f, 1f);
        [SerializeField] Color lowDimColor = new Color(0.55f, 0.15f, 0.15f, 1f);
        [SerializeField] float lowBlinkPeriod = 0.5f;

        float ratio = 1f;

        void OnEnable() => GameEvents.HealthChanged += OnChanged;
        void OnDisable() => GameEvents.HealthChanged -= OnChanged;

        void OnChanged(float current, float max)
        {
            ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            if (fill != null) fill.anchorMax = new Vector2(ratio, fill.anchorMax.y);
        }

        void Update()
        {
            if (fillImage == null) return;
            float low = balance != null ? balance.LowHpRatio : 0.3f;
            if (ratio > low) { fillImage.color = normalColor; return; }
            bool on = Mathf.Repeat(Time.unscaledTime, lowBlinkPeriod) < lowBlinkPeriod * 0.5f;
            fillImage.color = on ? lowColor : lowDimColor;
        }
    }
}
