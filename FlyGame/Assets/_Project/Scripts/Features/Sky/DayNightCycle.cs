using UnityEngine;
using UnityEngine.Rendering;

namespace FlyJam
{
    /// Computes sky appearance (gradient, sun, moon, stars) and time-of-day lighting, pushing values to ToonLighting.
    /// Without this component, the game still runs using ToonLighting's clear daytime defaults.
    public class DayNightCycle : MonoBehaviour
    {
        public static DayNightCycle Current { get; private set; }

        [SerializeField] GameSession session;            // Auto-filled from GameContext if null; loops with idleCycleSeconds if absent
        [SerializeField] float idleCycleSeconds = 20f;
        [SerializeField] Camera worldCamera;             // Auto-filled from GameContext if null
        [SerializeField] Planet planet;                  // Auto-filled from GameContext if null
        [SerializeField] Material skyMaterial;
        [SerializeField] bool useCodeDefaults = true;
        [SerializeField] Gradient topColor, horizonColor, lightColor, ambientColor;
        [SerializeField] AnimationCurve lightIntensity;
        [SerializeField, Range(0.5f, 0.95f)] float nightStart = 35f / 45f;

        [Header("Sun/Moon Arc (Camera-Facing Sky, Degrees)")]
        [SerializeField] float arcAzimuth = 28f;
        [SerializeField] float arcLowElevation = -36f;
        [SerializeField] float arcPeakElevation = -14f;

        /// Night start (0-1 of the day): from GameSession (sheet "Session".nightStart) when present, else this inspector value.
        float NightStart => session != null ? session.NightStart : nightStart;

        public float T { get; private set; }
        public float NightFactor { get; private set; }

        Material skyInstance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Current = null;

        void Awake()
        {
            Current = this;
            var ctx = GameContext.Current;
            if (ctx != null)
            {
                if (session == null) session = ctx.Session;
                if (worldCamera == null) worldCamera = ctx.WorldCamera;
                if (planet == null) planet = ctx.Planet;
            }
            if (useCodeDefaults || lightIntensity == null || lightIntensity.length == 0) BuildDefaults();
            skyInstance = new Material(skyMaterial);
            RenderSettings.skybox = skyInstance;
            RenderSettings.ambientMode = AmbientMode.Flat;
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        void LateUpdate()
        {
            if (worldCamera == null || planet == null) return;

            T = session != null ? session.DayProgress : Mathf.Repeat(Time.time / idleCycleSeconds, 1f);
            NightFactor = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(NightStart - 0.08f, NightStart + 0.04f, T));

            Vector3 up = planet.UpAt(worldCamera.transform.position);
            Vector3 fwd = Vector3.ProjectOnPlane(worldCamera.transform.forward, up).normalized;

            Vector3 sunDir = Arc(up, fwd, Mathf.InverseLerp(0f, NightStart, T));
            Vector3 moonDir = Arc(up, fwd, Mathf.InverseLerp(NightStart - 0.05f, 1f, T) * 0.7f);

            Color horizon = horizonColor.Evaluate(T);
            skyInstance.SetVector("_SkyUp", up);
            skyInstance.SetColor("_TopColor", topColor.Evaluate(T));
            skyInstance.SetColor("_HorizonColor", horizon);
            skyInstance.SetColor("_BottomColor", horizon * 0.85f);
            skyInstance.SetVector("_SunDir", sunDir);
            skyInstance.SetVector("_MoonDir", moonDir);
            skyInstance.SetColor("_SunColor", new Color(1f, 0.92f, 0.75f, 1f - NightFactor));
            skyInstance.SetColor("_MoonColor", new Color(0.85f, 0.9f, 1f, NightFactor));
            skyInstance.SetFloat("_StarIntensity", NightFactor);

            RenderSettings.ambientLight = ambientColor.Evaluate(T);

            if (ToonLighting.Current != null)
                ToonLighting.Current.SetTimeOfDay(lightColor.Evaluate(T), lightIntensity.Evaluate(T), horizon, NightFactor);
        }

        Vector3 Arc(Vector3 up, Vector3 fwd, float t)
        {
            float az = Mathf.Lerp(arcAzimuth, -arcAzimuth, t);
            float elev = Mathf.Lerp(arcLowElevation, arcPeakElevation, Mathf.Sin(t * Mathf.PI)) * Mathf.Deg2Rad;
            Vector3 flat = Quaternion.AngleAxis(az, up) * fwd;
            return (flat * Mathf.Cos(elev) + up * Mathf.Sin(elev)).normalized;
        }

        void BuildDefaults()
        {
            float[] t = { 0f, 5f / 45f, 25f / 45f, 30f / 45f, NightStart, 1f };
            topColor = G(t, new[] { C(0.30f, 0.32f, 0.58f), C(0.35f, 0.60f, 0.95f), C(0.35f, 0.60f, 0.95f), C(0.45f, 0.38f, 0.70f), C(0.07f, 0.08f, 0.22f), C(0.03f, 0.04f, 0.12f) });
            horizonColor = G(t, new[] { C(1.00f, 0.62f, 0.48f), C(0.75f, 0.88f, 1.00f), C(0.75f, 0.88f, 1.00f), C(1.00f, 0.55f, 0.30f), C(0.25f, 0.16f, 0.32f), C(0.07f, 0.08f, 0.20f) });
            lightColor = G(t, new[] { C(1.00f, 0.72f, 0.55f), C(1.00f, 0.97f, 0.90f), C(1.00f, 0.97f, 0.90f), C(1.00f, 0.62f, 0.38f), C(0.60f, 0.68f, 1.00f), C(0.60f, 0.68f, 1.00f) });
            ambientColor = G(t, new[] { C(0.45f, 0.40f, 0.45f), C(0.60f, 0.62f, 0.66f), C(0.60f, 0.62f, 0.66f), C(0.55f, 0.42f, 0.40f), C(0.26f, 0.28f, 0.42f), C(0.24f, 0.27f, 0.40f) });
            lightIntensity = new AnimationCurve(
                new Keyframe(t[0], 0.46f), new Keyframe(t[1], 0.85f), new Keyframe(t[2], 0.85f),
                new Keyframe(t[3], 0.62f), new Keyframe(t[4], 0.35f), new Keyframe(t[5], 0.35f));
        }

        static Color C(float r, float g, float b) => new Color(r, g, b, 1f);

        static Gradient G(float[] times, Color[] colors)
        {
            var ck = new GradientColorKey[times.Length];
            for (int i = 0; i < times.Length; i++) ck[i] = new GradientColorKey(colors[i], times[i]);
            var g = new Gradient();
            g.SetKeys(ck, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return g;
        }
    }
}
