using System.Collections.Generic;
using UnityEngine;

namespace FlyJam
{
    /// Dash effect: glowing rings slide over the fly from head to tail, 4 times, fast - like the air around a falling meteor.
    /// Each ring is a circle around the body axis (the body is inside the ring), orange turning red as it fades.
    /// Drawn with LineRenderers in the world, so the pixel-art pass pixelates them like everything else.
    public class DashRingFx : MonoBehaviour
    {
        [SerializeField] Material material;                 // M_ParticleAdd (additive glow)
        [SerializeField] SphereWalker player;               // Auto-filled from GameContext if null
        [SerializeField] int rings = 4;
        [SerializeField] float ringInterval = 0.06f;        // seconds between ring starts
        [SerializeField] float ringTravelSeconds = 0.14f;   // head -> tail time of one ring
        [SerializeField] float headZ = 0.55f;               // local z (fly space) where a ring starts
        [SerializeField] float tailZ = -0.55f;              // local z where it ends
        [SerializeField] float centerY = 0.36f;             // local height of the body axis
        [SerializeField] float radius = 0.45f;
        [SerializeField] float endRadiusScale = 1.15f;
        [SerializeField] float width = 0.06f;
        [SerializeField] int segments = 20;
        [SerializeField] Color startColor = new Color(1f, 0.62f, 0.25f, 0.6f);
        [SerializeField] Color endColor = new Color(1f, 0.18f, 0.1f, 0f);

        class Ring { public LineRenderer line; public float t; public bool live; }

        readonly List<Ring> pool = new List<Ring>();
        float spawnTimer;
        int toSpawn;

        void Start()
        {
            if (player == null && GameContext.Current != null) player = GameContext.Current.Player;
        }

        void OnEnable() => GameEvents.PlayerDodged += OnDash;
        void OnDisable() => GameEvents.PlayerDodged -= OnDash;

        void OnDash(Vector3 pos, Vector3 dir)
        {
            toSpawn = Mathf.Max(1, rings);
            spawnTimer = 0f;
        }

        void Update()
        {
            if (player == null) return;
            float dt = Time.deltaTime;

            if (toSpawn > 0)
            {
                spawnTimer -= dt;
                if (spawnTimer <= 0f)
                {
                    Get().t = 0f;
                    toSpawn--;
                    spawnTimer = ringInterval;
                }
            }

            Transform pt = player.transform;
            foreach (var r in pool)
            {
                if (!r.live) continue;
                r.t += dt / Mathf.Max(0.01f, ringTravelSeconds);
                if (r.t >= 1f) { r.live = false; r.line.enabled = false; continue; }
                float z = Mathf.Lerp(headZ, tailZ, r.t);
                float rad = radius * Mathf.Lerp(1f, endRadiusScale, r.t);
                for (int i = 0; i < segments; i++)
                {
                    float a = Mathf.PI * 2f * i / segments;
                    Vector3 local = new Vector3(Mathf.Cos(a) * rad, centerY + Mathf.Sin(a) * rad, z);
                    r.line.SetPosition(i, pt.TransformPoint(local));
                }
                Color c = Color.Lerp(startColor, endColor, r.t);
                r.line.startColor = c;
                r.line.endColor = c;
            }
        }

        Ring Get()
        {
            foreach (var r in pool)
                if (!r.live) { r.live = true; r.line.enabled = true; return r; }
            var go = new GameObject("DashRing");
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = true;
            line.positionCount = Mathf.Max(6, segments);
            line.widthMultiplier = width;
            line.sharedMaterial = material;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            segments = line.positionCount;
            var ring = new Ring { line = line, live = true };
            pool.Add(ring);
            return ring;
        }
    }
}
