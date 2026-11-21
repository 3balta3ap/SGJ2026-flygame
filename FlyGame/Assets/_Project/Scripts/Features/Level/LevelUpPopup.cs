using TMPro;
using UnityEngine;

namespace FlyJam
{
    /// "レベルアップ！" above the player's head on level-up: pops in, rises and fades.
    public class LevelUpPopup : MonoBehaviour
    {
        [SerializeField] TMP_Text text;
        [SerializeField] Camera worldCamera;      // Auto-filled from GameContext if null
        [SerializeField] SphereWalker player;     // Auto-filled from GameContext if null
        [SerializeField] float headHeight = 1.6f;
        [SerializeField] float showSeconds = 1.2f;
        [SerializeField] float risePixels = 70f;
        [SerializeField] Color color = new Color(1f, 0.85f, 0.3f, 1f);

        float t = -1f;

        void Start()
        {
            var ctx = GameContext.Current;
            if (ctx != null)
            {
                if (worldCamera == null) worldCamera = ctx.WorldCamera;
                if (player == null) player = ctx.Player;
            }
            if (text != null) text.enabled = false;
        }

        void OnEnable() => GameEvents.LevelChanged += OnLevel;
        void OnDisable() => GameEvents.LevelChanged -= OnLevel;

        void OnLevel(int level, int previous)
        {
            if (level <= previous || text == null) return;
            text.text = Loc.Get("level.up");
            t = 0f;
            text.enabled = true;
        }

        void LateUpdate()
        {
            if (t < 0f || text == null || worldCamera == null || player == null) return;
            t += Time.unscaledDeltaTime / Mathf.Max(0.05f, showSeconds);
            if (t >= 1f)
            {
                t = -1f;
                text.enabled = false;
                return;
            }
            Vector3 up = player.Planet.UpAt(player.transform.position);
            Vector3 vp = worldCamera.WorldToViewportPoint(player.transform.position + up * headHeight);
            float pop = t < 0.15f ? Mathf.Lerp(0.6f, 1.15f, t / 0.15f) : Mathf.Lerp(1.15f, 1f, Mathf.Clamp01((t - 0.15f) / 0.15f));
            text.rectTransform.position = new Vector3(vp.x * Screen.width, vp.y * Screen.height + risePixels * t, 0f);
            text.rectTransform.localScale = Vector3.one * pop;
            var c = color;
            c.a = t < 0.7f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.7f) / 0.3f);
            text.color = c;
        }
    }
}
