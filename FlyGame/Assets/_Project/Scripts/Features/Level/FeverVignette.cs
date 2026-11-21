using UnityEngine;
using UnityEngine.UI;

namespace FlyJam
{
    /// Soft yellow glow on the screen edges during the level-up fever: fades in, breathes brighter / darker, fades out.
    /// The texture is made in code at low resolution with point filtering and a few alpha steps, so it looks pixelated.
    [RequireComponent(typeof(RawImage))]
    public class FeverVignette : MonoBehaviour
    {
        [SerializeField] Color color = new Color(1f, 0.88f, 0.3f, 1f);
        [SerializeField] float alphaLow = 0.18f;
        [SerializeField] float alphaHigh = 0.45f;
        [SerializeField] float pulseSpeed = 6f;
        [SerializeField] float fadeSeconds = 0.3f;
        [SerializeField] int textureWidth = 64;
        [SerializeField] int textureHeight = 36;
        [Tooltip("Width of the glow band as a fraction of the screen")]
        [SerializeField] float band = 0.28f;
        [Tooltip("Alpha steps (pixel look). 4 = 4 visible rings")]
        [SerializeField] int steps = 4;

        RawImage image;
        bool on;
        float level;   // 0..1 fade

        void Awake()
        {
            image = GetComponent<RawImage>();
            image.raycastTarget = false;
            image.texture = Build();
            Apply(0f);
        }

        void OnEnable() => GameEvents.FeverChanged += OnFever;
        void OnDisable() => GameEvents.FeverChanged -= OnFever;
        void OnFever(bool v) => on = v;

        void Update()
        {
            level = Mathf.MoveTowards(level, on ? 1f : 0f, Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeSeconds));
            float pulse = Mathf.Lerp(alphaLow, alphaHigh, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pulseSpeed));
            Apply(level * pulse);
        }

        void Apply(float a)
        {
            var c = color;
            c.a = a;
            image.color = c;
            image.enabled = a > 0.001f;
        }

        Texture2D Build()
        {
            int w = Mathf.Max(8, textureWidth), h = Mathf.Max(8, textureHeight);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "FeverVignette"
            };
            int n = Mathf.Max(1, steps);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = (x + 0.5f) / w, v = (y + 0.5f) / h;
                    float edge = Mathf.Min(Mathf.Min(u, 1f - u) * w / h, Mathf.Min(v, 1f - v));   // distance to nearest edge, height units
                    float a = Mathf.Clamp01(1f - edge / Mathf.Max(0.01f, band));
                    a = Mathf.Ceil(a * a * n) / n;                                                   // stepped falloff
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            tex.Apply();
            return tex;
        }
    }
}
