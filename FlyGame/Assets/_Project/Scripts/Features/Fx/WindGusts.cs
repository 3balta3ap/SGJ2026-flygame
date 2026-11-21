using System.Collections.Generic;
using UnityEngine;

namespace FlyJam
{
    /// Now and then one or two wavy wind lines sweep across the view, and the pollen drifts with them for a moment.
    /// The lines live in the world just in front of the camera, so the pixel-art pass pixelates them.
    /// Wind direction: one main direction (screen space) that slowly swings back and forth.
    public class WindGusts : MonoBehaviour
    {
        [SerializeField] Material material;                     // M_Particle (soft alpha)
        [SerializeField] Camera worldCamera;                    // Auto-filled from GameContext if null
        [SerializeField] ParticleSystem pollen;                 // SYS_Fx/FX_Pollen (pushed by the gust). Optional
        [SerializeField] Vector2 interval = new Vector2(4f, 8f);
        [SerializeField] Vector2Int linesPerGust = new Vector2Int(1, 2);
        [SerializeField] float lifetime = 1.6f;
        [SerializeField] float lineLength = 4f;
        [SerializeField] float sweepWidth = 13f;               // distance the line head travels across the view
        [SerializeField] Vector2 depth = new Vector2(4f, 7f);  // distance in front of the camera
        [SerializeField] float verticalSpread = 1.6f;
        [SerializeField] float waveAmplitude = 0.18f;
        [SerializeField] float waves = 2.5f;
        [SerializeField] float width = 0.05f;
        [SerializeField] Color color = new Color(1f, 1f, 1f, 0.35f);
        [SerializeField] int points = 24;
        [SerializeField] float baseAngle = 0f;                 // degrees: 0 = left -> right on screen
        [SerializeField] float angleSwing = 25f;
        [SerializeField] float pollenPush = 2f;

        class Streak { public LineRenderer line; public float t, phase, dep, y, delay; public bool live; }

        readonly List<Streak> pool = new List<Streak>();
        float timer, gustLeft;
        Vector3 gustWorldDir;

        void Start()
        {
            if (worldCamera == null && GameContext.Current != null) worldCamera = GameContext.Current.WorldCamera;
            timer = Random.Range(interval.x, interval.y);
        }

        float Angle => baseAngle + Mathf.Sin(Time.time * 0.07f) * angleSwing;

        void Update()
        {
            if (worldCamera == null) return;
            float dt = Time.deltaTime;

            timer -= dt;
            if (timer <= 0f)
            {
                timer = Random.Range(interval.x, interval.y);
                int n = Random.Range(linesPerGust.x, linesPerGust.y + 1);
                for (int i = 0; i < n; i++)
                {
                    var s = Get();
                    s.t = 0f;
                    s.delay = i * 0.25f;
                    s.phase = Random.Range(0f, 6.28f);
                    s.dep = Random.Range(depth.x, depth.y);
                    s.y = Random.Range(-verticalSpread, verticalSpread);
                }
                gustLeft = lifetime;
                float a = Angle * Mathf.Deg2Rad;
                Transform cam = worldCamera.transform;
                gustWorldDir = (cam.right * Mathf.Cos(a) + cam.up * Mathf.Sin(a)).normalized;
            }

            PushPollen(dt);

            Transform c = worldCamera.transform;
            float ang = Angle * Mathf.Deg2Rad;
            Vector3 along = (c.right * Mathf.Cos(ang) + c.up * Mathf.Sin(ang)).normalized;
            Vector3 side = Vector3.Cross(c.forward, along).normalized;
            foreach (var s in pool)
            {
                if (!s.live) continue;
                if (s.delay > 0f) { s.delay -= dt; s.line.enabled = false; continue; }
                s.line.enabled = true;
                s.t += dt / Mathf.Max(0.05f, lifetime);
                if (s.t >= 1f) { s.live = false; s.line.enabled = false; continue; }

                Vector3 center = c.position + c.forward * s.dep + side * s.y;
                float head = Mathf.Lerp(-sweepWidth * 0.5f, sweepWidth * 0.5f + lineLength, s.t);
                for (int i = 0; i < points; i++)
                {
                    float k = (float)i / (points - 1);                       // 0 tail .. 1 head
                    float x = head - lineLength * (1f - k);
                    float wave = Mathf.Sin(k * waves * Mathf.PI * 2f + s.phase + s.t * 8f) * waveAmplitude * Mathf.Sin(k * Mathf.PI);
                    s.line.SetPosition(i, center + along * x + side * wave);
                }
                float fade = Mathf.Sin(s.t * Mathf.PI);                       // in and out
                var col = color;
                col.a *= fade;
                var tailCol = col;
                tailCol.a = 0f;
                s.line.startColor = tailCol;                                  // tail end transparent
                s.line.endColor = col;
            }
        }

        void PushPollen(float dt)
        {
            if (pollen == null) return;
            var f = pollen.forceOverLifetime;
            if (gustLeft > 0f)
            {
                gustLeft -= dt;
                float k = Mathf.Sin(Mathf.Clamp01(gustLeft / Mathf.Max(0.05f, lifetime)) * Mathf.PI) * pollenPush;
                f.enabled = true;
                f.space = ParticleSystemSimulationSpace.World;
                f.x = new ParticleSystem.MinMaxCurve(gustWorldDir.x * k);
                f.y = new ParticleSystem.MinMaxCurve(gustWorldDir.y * k);
                f.z = new ParticleSystem.MinMaxCurve(gustWorldDir.z * k);
            }
            else if (f.enabled) f.enabled = false;
        }

        Streak Get()
        {
            foreach (var s in pool)
                if (!s.live) { s.live = true; return s; }
            var go = new GameObject("WindStreak");
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = Mathf.Max(4, points);
            line.widthMultiplier = width;
            line.sharedMaterial = material;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
            points = line.positionCount;
            var st = new Streak { line = line, live = true };
            pool.Add(st);
            return st;
        }
    }
}
