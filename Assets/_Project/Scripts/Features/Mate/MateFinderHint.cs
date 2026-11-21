using UnityEngine;
using UnityEngine.UI;

namespace FlyJam
{
    /// Displays a heart indicator toward the mate when the player stops moving.
    public class MateFinderHint : MonoBehaviour
    {
        [SerializeField] Camera worldCamera;        // Auto-filled from GameContext if null
        [SerializeField] SphereWalker player;       // Auto-filled from GameContext if null
        [SerializeField] Transform mate;            // Auto-filled from MateAI.Current if null
        [SerializeField] GameSession session;       // Auto-filled from GameContext if null
        [SerializeField] RawImage icon;
        [SerializeField] float showDelay = 0.35f;
        [SerializeField] float fadeSpeed = 6f;
        [SerializeField] float edgeMargin = 0.06f;
        [SerializeField] Vector2 sizeNearFar = new Vector2(64f, 36f);
        [SerializeField] Vector2 distanceNearFar = new Vector2(3f, 20f);

        float alpha;

        void Awake()
        {
            icon.texture = PixelHeart.Get();
            icon.raycastTarget = false;
            SetAlpha(0f);
        }

        void Start()
        {
            var ctx = GameContext.Current;
            if (ctx == null) return;
            if (worldCamera == null) worldCamera = ctx.WorldCamera;
            if (player == null) player = ctx.Player;
            if (session == null) session = ctx.Session;
        }

        void LateUpdate()
        {
            if (mate == null && MateAI.Current != null) mate = MateAI.Current.transform;
            if (worldCamera == null || player == null || session == null || mate == null)
            {
                SetAlpha(0f);
                return;
            }

            bool want = !session.Ended && !session.IsPaused
                        && player.State == FlyState.Stopped && player.TimeInState >= showDelay;
            alpha = Mathf.MoveTowards(alpha, want ? 1f : 0f, fadeSpeed * Time.unscaledDeltaTime);
            SetAlpha(alpha);
            if (alpha <= 0f) return;

            Vector3 up = player.Planet.UpAt(mate.position);
            Vector3 vp = worldCamera.WorldToViewportPoint(mate.position + up * 0.6f);
            Vector2 p = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
            if (vp.z < 0f) p = -p;

            float limit = 0.5f - edgeMargin;
            bool inside = vp.z > 0f && Mathf.Abs(p.x) <= limit && Mathf.Abs(p.y) <= limit;
            if (!inside)
            {
                float m = Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y));
                p = m < 1e-4f ? new Vector2(0f, -limit) : p / m * limit;
            }
            icon.rectTransform.position = new Vector3((p.x + 0.5f) * Screen.width, (p.y + 0.5f) * Screen.height, 0f);

            float dist = Vector3.Distance(player.transform.position, mate.position);
            float size = Mathf.Lerp(sizeNearFar.x, sizeNearFar.y, Mathf.InverseLerp(distanceNearFar.x, distanceNearFar.y, dist));
            size *= 1f + 0.12f * Mathf.Sin(Time.unscaledTime * 8f);
            icon.rectTransform.sizeDelta = new Vector2(size, size * 6f / 7f);
        }

        void SetAlpha(float a)
        {
            var c = icon.color;
            c.a = a;
            icon.color = c;
            icon.enabled = a > 0.001f;
        }
    }
}
