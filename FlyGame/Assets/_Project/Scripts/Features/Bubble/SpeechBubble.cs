using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlyJam
{
    /// The player's speech bubble above its head (UI, pixel bubble, Silver font).
    /// GameEvents.BubbleRequested(key, priority): a higher priority replaces the one on screen; otherwise it waits in line.
    /// Hidden (and its timer paused) while the session is paused: courtship, mash battle, pause menu. Cleared at round end.
    public class SpeechBubble : MonoBehaviour
    {
        [SerializeField] RectTransform root;      // bubble root (moves above the head)
        [SerializeField] Image box;               // Image Type = Sliced (sprite made in code)
        [SerializeField] Image tail;              // below the box
        [SerializeField] TMP_Text text;
        [SerializeField] Camera worldCamera;      // Auto-filled from GameContext if null
        [SerializeField] SphereWalker player;     // Auto-filled from GameContext if null
        [SerializeField] GameSession session;     // Auto-filled from GameContext if null
        [SerializeField] float headHeight = 1.3f;
        [SerializeField] float showSeconds = 2.5f;
        [SerializeField] float gapSeconds = 0.25f;
        [SerializeField] float maxWidth = 560f;
        [SerializeField] Vector2 padding = new Vector2(40f, 26f);
        [Tooltip("Screen pixels per bubble texel (pixel size)")]
        [SerializeField] float pixelScale = 4f;
        [SerializeField] int maxQueue = 4;

        struct Item { public string key; public int priority; }

        readonly List<Item> queue = new List<Item>();
        Item current;
        float left, gap;
        bool showing;

        public bool Showing => showing;
        public string CurrentKey => showing ? current.key : null;

        void Awake()
        {
            if (box != null)
            {
                box.sprite = PixelBubbleArt.Box();
                box.type = Image.Type.Sliced;
                box.pixelsPerUnitMultiplier = 1f / Mathf.Max(0.01f, pixelScale);   // 1 texel = pixelScale canvas units
                box.raycastTarget = false;
            }
            if (tail != null)
            {
                tail.sprite = PixelBubbleArt.Tail();
                tail.raycastTarget = false;
                tail.rectTransform.sizeDelta = new Vector2(7f * pixelScale, 4f * pixelScale);
            }
            if (text != null) text.raycastTarget = false;
            SetVisible(false);
        }

        void Start()
        {
            var ctx = GameContext.Current;
            if (ctx == null) return;
            if (worldCamera == null) worldCamera = ctx.WorldCamera;
            if (player == null) player = ctx.Player;
            if (session == null) session = ctx.Session;
        }

        void OnEnable()
        {
            GameEvents.BubbleRequested += OnRequest;
            GameEvents.RoundEnded += OnRoundEnded;
            Loc.Changed += Relayout;
        }

        void OnDisable()
        {
            GameEvents.BubbleRequested -= OnRequest;
            GameEvents.RoundEnded -= OnRoundEnded;
            Loc.Changed -= Relayout;
        }

        void OnRoundEnded(bool cleared)
        {
            queue.Clear();
            showing = false;
            SetVisible(false);
        }

        void OnRequest(string key, int priority)
        {
            if (string.IsNullOrEmpty(key) || (session != null && session.Ended)) return;
            var item = new Item { key = key, priority = priority };
            if (!showing && gap <= 0f) { Show(item); return; }
            if (showing && priority > current.priority) { Show(item); return; }
            if (queue.Count >= maxQueue) return;
            // keep the line ordered: higher priority first, same priority in arrival order
            int at = queue.Count;
            for (int i = 0; i < queue.Count; i++) if (queue[i].priority < priority) { at = i; break; }
            queue.Insert(at, item);
        }

        void Show(Item item)
        {
            current = item;
            showing = true;
            left = showSeconds;
            Relayout();
            SetVisible(true);
        }

        void Relayout()
        {
            if (!showing || text == null || root == null) return;
            text.text = Loc.Get(current.key);
            Vector2 pref = text.GetPreferredValues(text.text, maxWidth - padding.x, 0f);
            float w = Mathf.Min(maxWidth, pref.x + padding.x);
            float h = pref.y + padding.y;
            root.sizeDelta = new Vector2(Snap(w), Snap(h));
        }

        float Snap(float v) => Mathf.Ceil(v / pixelScale) * pixelScale;

        void SetVisible(bool v)
        {
            if (box != null) box.enabled = v;
            if (tail != null) tail.enabled = v;
            if (text != null) text.enabled = v;
        }

        void LateUpdate()
        {
            bool paused = session != null && session.IsPaused;
            if (!showing)
            {
                if (gap > 0f && !paused) gap -= Time.deltaTime;
                if (gap <= 0f && queue.Count > 0 && !paused)
                {
                    var next = queue[0];
                    queue.RemoveAt(0);
                    Show(next);
                }
                return;
            }

            SetVisible(!paused);
            if (paused) return;

            left -= Time.deltaTime;
            if (left <= 0f)
            {
                showing = false;
                gap = gapSeconds;
                SetVisible(false);
                return;
            }

            if (worldCamera == null || player == null || root == null) return;
            Vector3 up = player.Planet.UpAt(player.transform.position);
            Vector3 vp = worldCamera.WorldToViewportPoint(player.transform.position + up * headHeight);
            if (vp.z <= 0f) { SetVisible(false); return; }
            float x = Mathf.Round(vp.x * Screen.width / pixelScale) * pixelScale;
            float y = Mathf.Round(vp.y * Screen.height / pixelScale) * pixelScale;
            root.position = new Vector3(x, y, 0f);
        }
    }
}
