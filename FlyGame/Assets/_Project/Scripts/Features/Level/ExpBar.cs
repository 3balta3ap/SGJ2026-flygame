using TMPro;
using UnityEngine;

namespace FlyJam
{
    /// Small EXP bar under the HP bar (A03): "Lv.3" on the left, "3/5" numbers on the right ("MAX" at the top level).
    public class ExpBar : MonoBehaviour
    {
        [SerializeField] TMP_Text levelText;     // "Lv.3"
        [SerializeField] RectTransform fill;     // stretched by anchorMax.x
        [SerializeField] TMP_Text valueText;     // "3/5"
        [SerializeField] float fillSharpness = 10f;

        float target, shown;
        int lastLevel = -1;

        void OnEnable()
        {
            GameEvents.ExpChanged += OnExp;
            Loc.Changed += Redraw;
        }

        void OnDisable()
        {
            GameEvents.ExpChanged -= OnExp;
            Loc.Changed -= Redraw;
        }

        int exp, need, level = 1;

        void OnExp(int e, int n, int l)
        {
            exp = e;
            need = n;
            level = l;
            target = n > 0 ? Mathf.Clamp01((float)e / n) : 1f;
            if (l != lastLevel)
            {
                // New level: the bar restarts from empty (or full at max level)
                shown = n > 0 ? 0f : 1f;
                lastLevel = l;
            }
            Redraw();
        }

        void Redraw()
        {
            if (levelText != null) levelText.text = Loc.Format("hud.lv", level);
            if (valueText != null) valueText.text = need > 0 ? exp + "/" + need : Loc.Get("hud.lvmax");
        }

        void Update()
        {
            shown = Mathf.Lerp(shown, target, 1f - Mathf.Exp(-fillSharpness * Time.unscaledDeltaTime));
            if (fill != null) fill.anchorMax = new Vector2(shown, fill.anchorMax.y);
        }
    }
}
