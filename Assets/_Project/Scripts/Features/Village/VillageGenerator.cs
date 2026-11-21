using System.Collections.Generic;
using UnityEngine;

namespace FlyJam
{
    [System.Serializable]
    public class PropRule
    {
        public string label = "Tree";
        public GameObject prefab;
        public PropKind kind = PropKind.Decor;
        public int count = 10;
        public float targetHeight = 3f;
        public Vector2 heightJitter = new Vector2(0.85f, 1.15f);
        [Tooltip("When enabled, measures collision radius from the model's actual horizontal width (recommended for Rock, Bush)")]
        public bool autoFootprint = false;
        [Tooltip("When autoFootprint is enabled: collision radius = half-width * this scale")]
        public float footprintScale = 0.85f;
        [Tooltip("Manual radius when autoFootprint is disabled (at targetHeight). For trees, use trunk radius")]
        public float footprintRadius = 0.5f;
        public float minSpacing = 1.2f;
    }

    public class VillageGenerator : MonoBehaviour
    {
        [SerializeField] Planet planet;
        [SerializeField] Transform container;
        [SerializeField] int seed = 20261121;
        [SerializeField] bool randomSeedEachRound = false;
        [SerializeField] PlanetProp[] landmarks;
        [SerializeField] Transform[] keepClear;
        [SerializeField] float keepClearRadius = 3f;
        [SerializeField] PropRule[] rules;
        [SerializeField] int attemptsPerObject = 30;
        [Tooltip("Base extra depth to sink all generated props into the surface")]
        [SerializeField] float baseSink = 0.02f;

        struct Placed { public Vector3 pos; public float radius; }

        void Awake()
        {
            if (planet == null && GameContext.Current != null) planet = GameContext.Current.Planet;
            Generate();
            if (Application.isPlaying && container != null) ToonMaterials.Apply(container, 1f);
        }

        [ContextMenu("Generate Now")]
        public void Generate()
        {
            if (planet == null || container == null || rules == null) return;
            Clear();

            int s = (randomSeedEachRound && Application.isPlaying) ? System.Environment.TickCount : seed;
            var rng = new System.Random(s);
            var placed = new List<Placed>();

            if (landmarks != null)
                foreach (var lm in landmarks)
                    if (lm != null) placed.Add(new Placed { pos = lm.transform.position, radius = lm.Radius });

            foreach (var rule in rules)
            {
                if (rule == null || rule.prefab == null) continue;
                int made = 0;
                for (int a = 0; a < rule.count * attemptsPerObject && made < rule.count; a++)
                {
                    Vector3 dir = RandomDirection(rng);
                    Vector3 surface = planet.SurfacePoint(dir);
                    if (InKeepClear(surface)) continue;

                    float h = rule.targetHeight * Mathf.Lerp(rule.heightJitter.x, rule.heightJitter.y, (float)rng.NextDouble());
                    float yaw = (float)rng.NextDouble() * 360f;

                    GameObject go = Instantiate(rule.prefab, container);
                    go.name = rule.label;
                    go.transform.localRotation = rule.prefab.transform.localRotation;
                    go.transform.localScale = rule.prefab.transform.localScale;

                    // Measure bounds and pivot offset before tilting (in prefab's upright orientation)
                    Bounds b0 = MeasureBounds(go);
                    float k = b0.size.y > 1e-4f ? h / b0.size.y : 1f;
                    float halfWidth = Mathf.Max(b0.extents.x, b0.extents.z) * k;
                    float bottomBelowPivot = (go.transform.position.y - b0.min.y) * k;   // Positive if pivot is above the bottom

                    float r = rule.autoFootprint
                        ? halfWidth * rule.footprintScale
                        : rule.footprintRadius * (h / Mathf.Max(0.01f, rule.targetHeight));

                    if (!IsFree(surface, r, rule.minSpacing, placed))
                    {
                        Discard(go);
                        continue;
                    }

                    float sinkWidth = rule.autoFootprint ? halfWidth : r;   // Trees sink based on trunk radius
                    float offset = bottomBelowPivot - baseSink - CurvatureSink(planet.Radius, sinkWidth);

                    go.transform.localScale = rule.prefab.transform.localScale * k;
                    go.transform.position = planet.SurfacePoint(dir, offset);
                    go.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir)
                                          * Quaternion.Euler(0f, yaw, 0f)
                                          * rule.prefab.transform.localRotation;

                    var prop = go.GetComponent<PlanetProp>();
                    if (prop == null) prop = go.AddComponent<PlanetProp>();
                    prop.Configure(rule.kind, r);

                    placed.Add(new Placed { pos = surface, radius = r });
                    made++;
                }
            }
        }

        bool InKeepClear(Vector3 pos)
        {
            var zones = KeepClearZone.All;
            for (int i = 0; i < zones.Count; i++)
            {
                var z = zones[i];
                if (z != null && Vector3.Distance(pos, planet.SnapToSurface(z.transform.position)) < Mathf.Max(keepClearRadius, z.Radius))
                    return true;
            }
            if (keepClear == null) return false;
            foreach (var t in keepClear)
                if (t != null && Vector3.Distance(pos, planet.SnapToSurface(t.position)) < keepClearRadius)
                    return true;
            return false;
        }

        static bool IsFree(Vector3 pos, float r, float minSpacing, List<Placed> placed)
        {
            foreach (var p in placed)
                if (Vector3.Distance(pos, p.pos) < Mathf.Max(minSpacing, r + p.radius))
                    return false;
            return true;
        }

        /// Gap height at the edge when placing a flat base of half-width w onto a sphere of radius R.
        static float CurvatureSink(float R, float w)
        {
            w = Mathf.Min(w, R * 0.9f);
            return R - Mathf.Sqrt(R * R - w * w);
        }

        void Clear()
        {
            for (int i = container.childCount - 1; i >= 0; i--)
                Discard(container.GetChild(i).gameObject);
        }

        static void Discard(GameObject go)
        {
            if (Application.isPlaying) { go.SetActive(false); Destroy(go); }
            else DestroyImmediate(go);
        }

        static Vector3 RandomDirection(System.Random rng)
        {
            float z = (float)(rng.NextDouble() * 2.0 - 1.0);
            float t = (float)(rng.NextDouble() * System.Math.PI * 2.0);
            float r = Mathf.Sqrt(1f - z * z);
            return new Vector3(r * Mathf.Cos(t), z, r * Mathf.Sin(t));
        }

        static Bounds MeasureBounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }
    }
}
