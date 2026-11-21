using UnityEngine;
using UnityEngine.UI;

namespace FlyJam
{
    /// Discovery mark (A03, C03): a heart above the female's head while she is on screen and not too far.
    /// (Class name kept from the prototype's direction hint so the SYS_Mate prefab link survives.)
    public class MateFinderHint : MonoBehaviour
    {
        [SerializeField] Camera worldCamera;        // Auto-filled from GameContext if null
        [SerializeField] SphereWalker player;       // Auto-filled from GameContext if null
        [SerializeField] Transform mate;            // Auto-filled from MateAI.Current if null
        [SerializeField] GameSession session;       // Auto-filled from GameContext if null
        [SerializeField] RawImage icon;
        [SerializeField] float headHeight = 0.9f;
        [SerializeField] float maxDistance = 16f;
        [SerializeField] float size = 44f;
        [SerializeField] float bobPixels = 6f;
        [SerializeField] float fadeSpeed = 8f;

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

            Vector3 up = player.Planet.UpAt(mate.position);
            Vector3 vp = worldCamera.WorldToViewportPoint(mate.position + up * headHeight);
            bool onScreen = vp.z > 0f && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;
            bool near = Vector3.Distance(player.transform.position, mate.position) <= maxDistance;
            bool want = onScreen && near && !session.Ended;

            alpha = Mathf.MoveTowards(alpha, want ? 1f : 0f, fadeSpeed * Time.unscaledDeltaTime);
            SetAlpha(alpha);
            if (alpha <= 0f) return;

            float bob = Mathf.Sin(Time.unscaledTime * 5f) * bobPixels;
            icon.rectTransform.position = new Vector3(vp.x * Screen.width, vp.y * Screen.height + bob, 0f);
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
