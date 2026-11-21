using UnityEngine;

namespace FlyJam
{
    /// Story lines that come from the world itself (other features raise their own bubbles with BubbleRequested):
    /// nightfall -> "夜は、なんだかこわく見える…" then "オスが強くなってる…！".
    public class BubbleTriggers : MonoBehaviour
    {
        [SerializeField] string[] nightKeys = { "bubble.night.1", "bubble.night.2" };
        [SerializeField] int nightPriority = 1;

        void OnEnable() => GameEvents.NightChanged += OnNight;
        void OnDisable() => GameEvents.NightChanged -= OnNight;

        void OnNight(bool night, int day)
        {
            if (!night || nightKeys == null) return;
            foreach (var k in nightKeys)
                if (!string.IsNullOrEmpty(k)) GameEvents.RaiseBubbleRequested(k, nightPriority);
        }
    }
}
