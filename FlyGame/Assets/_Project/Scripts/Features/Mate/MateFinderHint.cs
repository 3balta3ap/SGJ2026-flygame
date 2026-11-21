using UnityEngine;
using UnityEngine.UI;

namespace FlyJam
{
    /// Heart marker that always shows where the female is (A03, C03).
    /// On screen: above her head. Off screen (or behind the camera): pinned to the screen edge in her direction.
    /// Size grows as she gets closer (sizeFar at distanceFar or more -> sizeNear at distanceNear or less).
    /// Hidden during courtship / mash battle / pause (session paused) and after the round ends.
    /// The first time she is on screen and within seenDistance -> speech bubble "メスだ！".
    /// (Class name kept from the prototype so the SYS_Mate prefab link survives.)
    public class MateFinderHint : MonoBehaviour
    {
        [SerializeField] Camera worldCamera;        // Auto-filled from GameContext if null
        [SerializeField] SphereWalker player;       // Auto-filled from GameContext if null
        [SerializeField] Transform mate;            // Auto-filled from MateAI.Current if null
        [SerializeField] GameSession session;       // Auto-filled from GameContext if null
        [SerializeField] RawImage icon;
        [SerializeField] float headHeight = 0.9f;
        [Tooltip("Screen-edge margin for the off-screen marker (fraction of the screen)")]
        [SerializeField] float edgeMargin = 0.06f;
        [SerializeField] float sizeNear = 72f;
        [SerializeField] float sizeFar = 36f;
        [SerializeField] float distanceNear = 3f;
        [SerializeField] float distanceFar = 24f;
        [SerializeField] float bobPixels = 6f;
        [SerializeField] float fadeSpeed = 8f;
        [Tooltip("First sighting bubble: on screen and within this distance")]
        [SerializeField] float seenDistance = 16f;
        [SerializeField] string seenBubbleKey = "bubble.mate.seen";

        float alpha;
        bool seen;

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

            Vector3 up = player.Planet.UpAt(mate.position);
            Vector3 vp = worldCamera.WorldToViewportPoint(mate.position + up * headHeight);
            bool onScreen = vp.z > 0f && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;
            float dist = Vector3.Distance(player.transform.position, mate.position);

            if (!seen && onScreen && dist <= seenDistance && !session.IsPaused && !session.Ended)
            {
                seen = true;
                if (!string.IsNullOrEmpty(seenBubbleKey)) GameEvents.RaiseBubbleRequested(seenBubbleKey, 2);
            }

            bool want = !session.Ended && !session.IsPaused;
            alpha = Mathf.MoveTowards(alpha, want ? 1f : 0f, fadeSpeed * Time.unscaledDeltaTime);
            SetAlpha(alpha);
            if (alpha <= 0f) return;

            Vector2 pos;
            if (onScreen)
            {
                float bob = Mathf.Sin(Time.unscaledTime * 5f) * bobPixels;
                pos = new Vector2(vp.x * Screen.width, vp.y * Screen.height + bob);
            }
            else pos = EdgePoint(vp);

            float t = Mathf.InverseLerp(distanceFar, distanceNear, dist);   // 0 far .. 1 near
            float s = Mathf.Lerp(sizeFar, sizeNear, t);
            icon.rectTransform.position = new Vector3(pos.x, pos.y, 0f);
            icon.rectTransform.sizeDelta = new Vector2(s, s * 6f / 7f);
        }

        /// Off-screen: from the screen centre toward her, clamped to the screen rectangle minus the margin.
        Vector2 EdgePoint(Vector3 vp)
        {
            Vector2 d = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
            if (vp.z < 0f) d = -d;                       // behind the camera: the projection is mirrored
            if (d.sqrMagnitude < 1e-6f) d = Vector2.down;
            float half = 0.5f - edgeMargin;
            float k = half / Mathf.Max(Mathf.Abs(d.x), Mathf.Abs(d.y));
            Vector2 v = new Vector2(0.5f, 0.5f) + d * k;
            return new Vector2(v.x * Screen.width, v.y * Screen.height);
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
