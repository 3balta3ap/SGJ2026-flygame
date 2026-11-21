using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlyJam
{
    /// HUD top-left (A03): HP gauge, "7/12" numbers, "+2"/"-2" change popup, hungry message.
    /// - The gauge gets longer with max HP (pixelsPerHp per point): growth you can see at each level-up.
    /// - Damage: the green part drops at once, the lost part stays red (chip) for chipDelay, then shrinks smoothly.
    /// - Healing: the green part fills up over healSeconds.
    /// - Colour: green above midHpRatio, yellow at or below it, orange blinking at or below lowHpRatio (+ message).
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] HealthBalance balance;
        [Tooltip("The HealthBar root: its width = labelWidth + max HP x pixelsPerHp")]
        [SerializeField] RectTransform barRoot;
        [SerializeField] RectTransform fill;            // green part, stretched by anchorMax.x
        [SerializeField] Image fillImage;
        [SerializeField] RectTransform chip;            // red part behind fill, stretched by anchorMax.x
        [SerializeField] Image chipImage;
        [SerializeField] TMP_Text valueText;            // "5/10"
        [SerializeField] TMP_Text deltaText;            // "+2" / "-2"
        [SerializeField] GameObject lowHpMessage;       // LocalizedText "hud.lowhp" ("おなかがペコペコ…")

        [Header("Size")]
        [SerializeField] float pixelsPerHp = 30f;
        [SerializeField] float labelWidth = 104f;       // space for the "体力" label left of the frame
        [SerializeField] float growSeconds = 0.35f;

        [Header("Motion")]
        [SerializeField] float chipDelay = 0.4f;
        [SerializeField] float chipSharpness = 5f;
        [SerializeField] float healSeconds = 0.3f;
        [SerializeField] float deltaShowSeconds = 1.2f;
        [SerializeField] float deltaMin = 0.05f;        // smaller changes (per-frame drain) are not shown and do not leave a chip

        [Header("Colours")]
        [SerializeField] Color fullColor = new Color(0.40f, 0.78f, 0.36f, 1f);
        [SerializeField] Color midColor = new Color(1.00f, 0.84f, 0.25f, 1f);
        [SerializeField] Color lowColor = new Color(1.00f, 0.55f, 0.15f, 1f);
        [SerializeField] Color lowDimColor = new Color(0.62f, 0.30f, 0.08f, 1f);
        [SerializeField] Color chipColor = new Color(0.90f, 0.20f, 0.18f, 1f);
        [SerializeField] Color healColor = new Color(0.25f, 0.6f, 0.3f, 1f);
        [SerializeField] Color damageColor = new Color(0.8f, 0.2f, 0.2f, 1f);
        [SerializeField] float lowBlinkPeriod = 0.6f;

        float target = 1f, shown = 1f, chipRatio = 1f, chipHold, healRate;
        float maxHp = 10f, width = -1f, widthFrom, widthTo, growT = 1f;
        float last = -1f, deltaLeft;

        public float ShownRatio => shown;
        public float ChipRatio => chipRatio;

        void OnEnable() => GameEvents.HealthChanged += OnChanged;
        void OnDisable() => GameEvents.HealthChanged -= OnChanged;

        void Start()
        {
            if (deltaText != null) deltaText.gameObject.SetActive(false);
            if (chipImage != null) chipImage.color = chipColor;
        }

        void OnChanged(float current, float max)
        {
            float newTarget = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            bool first = last < 0f;
            float d = first ? 0f : current - last;

            if (first) { shown = chipRatio = newTarget; }
            else if (Mathf.Abs(max - maxHp) > 0.01f)
            {
                // Max HP changed (level-up): re-express the gauge in the new scale, no chip
                shown = chipRatio = newTarget;
            }
            else if (d <= -deltaMin)
            {
                chipRatio = Mathf.Max(chipRatio, shown);   // lost part stays red
                shown = newTarget;
                chipHold = chipDelay;
            }
            else if (d >= deltaMin)
            {
                healRate = Mathf.Max(0.01f, (newTarget - shown) / Mathf.Max(0.01f, healSeconds));
            }
            else if (newTarget < shown) shown = newTarget;   // slow drain: follow directly
            target = newTarget;

            if (Mathf.Abs(max - maxHp) > 0.01f || width < 0f) SetMax(max, first);
            if (valueText != null) valueText.text = Mathf.CeilToInt(current - 0.0001f) + "/" + Mathf.RoundToInt(max);

            if (!first && Mathf.Abs(d) >= deltaMin && deltaText != null)
            {
                string n = d.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
                deltaText.text = d > 0f ? "+" + n : n;
                deltaText.color = d > 0f ? healColor : damageColor;
                deltaText.gameObject.SetActive(true);
                deltaLeft = deltaShowSeconds;
            }
            last = current;
        }

        void SetMax(float max, bool instant)
        {
            maxHp = max;
            float w = labelWidth + max * pixelsPerHp;
            if (barRoot == null) return;
            if (instant || width < 0f)
            {
                width = w;
                growT = 1f;
                barRoot.sizeDelta = new Vector2(w, barRoot.sizeDelta.y);
                return;
            }
            widthFrom = barRoot.sizeDelta.x;
            widthTo = w;
            growT = 0f;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;

            if (shown < target) shown = Mathf.MoveTowards(shown, target, healRate * dt);
            else if (shown > target) shown = target;

            if (chipHold > 0f) chipHold -= dt;
            else chipRatio = Mathf.Lerp(chipRatio, shown, 1f - Mathf.Exp(-chipSharpness * dt));
            if (chipRatio < shown || chipRatio - shown < 0.002f) chipRatio = shown;

            if (fill != null) fill.anchorMax = new Vector2(shown, fill.anchorMax.y);
            if (chip != null) chip.anchorMax = new Vector2(chipRatio, chip.anchorMax.y);

            if (growT < 1f && barRoot != null)
            {
                growT = Mathf.Min(1f, growT + dt / Mathf.Max(0.01f, growSeconds));
                float w = Mathf.Lerp(widthFrom, widthTo, Mathf.SmoothStep(0f, 1f, growT));
                barRoot.sizeDelta = new Vector2(w, barRoot.sizeDelta.y);
                width = w;
            }

            float low = balance != null ? balance.LowHpRatio : 0.2f;
            float mid = balance != null ? balance.MidHpRatio : 0.5f;
            bool isLow = target <= low;
            if (lowHpMessage != null && lowHpMessage.activeSelf != isLow) lowHpMessage.SetActive(isLow);

            if (fillImage != null)
            {
                if (isLow) fillImage.color = Mathf.Repeat(Time.unscaledTime, lowBlinkPeriod) < lowBlinkPeriod * 0.5f ? lowColor : lowDimColor;
                else fillImage.color = target <= mid ? midColor : fullColor;
            }

            if (deltaLeft > 0f)
            {
                deltaLeft -= dt;
                if (deltaLeft <= 0f && deltaText != null) deltaText.gameObject.SetActive(false);
            }
        }
    }
}
