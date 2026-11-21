using System.Collections.Generic;
using UnityEngine;

namespace FlyJam
{
    /// In a pond the fly sinks a little (about a third of its body) and leaves faint pixel ripples while it moves.
    /// Put on the Fly root (the Mate variant inherits it). Needs VisualOffset on the same object. Speed is unchanged.
    [RequireComponent(typeof(VisualOffset))]
    public class PondSink : MonoBehaviour
    {
        [SerializeField] float sinkDepth = 0.15f;
        [SerializeField] float sinkSeconds = 0.15f;
        [SerializeField] float edgeMargin = 0.1f;
        [Header("Ripples")]
        [SerializeField] Material rippleMaterial;          // M_Particle. Empty = no ripples
        [SerializeField] float rippleInterval = 0.5f;
        [SerializeField] float rippleSeconds = 0.9f;
        [SerializeField] float rippleStartRadius = 0.25f;
        [SerializeField] float rippleEndRadius = 0.9f;
        [SerializeField] float rippleWidth = 0.035f;
        [SerializeField] Color rippleColor = new Color(0.85f, 0.95f, 1f, 0.35f);
        [SerializeField] int rippleSegments = 18;

        class Ripple { public LineRenderer line; public Vector3 center, up; public float t; public bool live; }

        VisualOffset offset;
        SphereWalker walker;
        float sink, rippleTimer;
        readonly List<Ripple> pool = new List<Ripple>();

        public bool InWater { get; private set; }

        void Awake()
        {
            offset = GetComponent<VisualOffset>();
            walker = GetComponent<SphereWalker>();
        }

        void Update()
        {
            InWater = WaterZone.AnyContains(transform.position, edgeMargin);
            float target = InWater ? -sinkDepth : 0f;
            sink = Mathf.MoveTowards(sink, target, sinkDepth / Mathf.Max(0.01f, sinkSeconds) * Time.deltaTime);
            offset.Set("pond", sink);

            bool moving = walker != null && walker.CurrentSpeed > 0.1f;
            if (InWater && moving && rippleMaterial != null && walker != null && walker.Planet != null)
            {
                rippleTimer -= Time.deltaTime;
                if (rippleTimer <= 0f)
                {
                    rippleTimer = rippleInterval;
                    var r = Get();
                    r.up = walker.Planet.UpAt(transform.position);
                    r.center = walker.Planet.SnapToSurface(transform.position, 0.03f);
                    r.t = 0f;
                }
            }
            UpdateRipples();
        }

        void UpdateRipples()
        {
            foreach (var r in pool)
            {
                if (!r.live) continue;
                r.t += Time.deltaTime / Mathf.Max(0.05f, rippleSeconds);
                if (r.t >= 1f) { r.live = false; r.line.enabled = false; continue; }
                float rad = Mathf.Lerp(rippleStartRadius, rippleEndRadius, r.t);
                Vector3 a = Vector3.Cross(r.up, Mathf.Abs(r.up.y) < 0.9f ? Vector3.up : Vector3.forward).normalized;
                Vector3 b = Vector3.Cross(r.up, a);
                for (int i = 0; i < rippleSegments; i++)
                {
                    float ang = Mathf.PI * 2f * i / rippleSegments;
                    r.line.SetPosition(i, r.center + (a * Mathf.Cos(ang) + b * Mathf.Sin(ang)) * rad);
                }
                var c = rippleColor;
                c.a *= 1f - r.t;
                r.line.startColor = c;
                r.line.endColor = c;
            }
        }

        Ripple Get()
        {
            foreach (var r in pool)
                if (!r.live) { r.live = true; r.line.enabled = true; return r; }
            var go = new GameObject("PondRipple");
            go.transform.SetParent(null, false);   // world space, not moving with the fly
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = true;
            line.positionCount = Mathf.Max(6, rippleSegments);
            line.widthMultiplier = rippleWidth;
            line.sharedMaterial = rippleMaterial;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            rippleSegments = line.positionCount;
            var rp = new Ripple { line = line, live = true };
            pool.Add(rp);
            return rp;
        }

        void OnDestroy()
        {
            foreach (var r in pool) if (r.line != null) Destroy(r.line.gameObject);
        }
    }
}
