using UnityEngine;

namespace FlyJam
{
    /// Pushes global shader values (_Fly*) used by the Toon shader and pixel post-process every frame.
    /// Without the Sky feature, falls back to the clear daytime defaults below. The Sky feature only pushes values via SetTimeOfDay.
    [DefaultExecutionOrder(1000)]
    public class ToonLighting : MonoBehaviour
    {
        public static ToonLighting Current { get; private set; }

        [SerializeField] Planet planet;
        [SerializeField] Light keyLight;   // Directional Light under Main Camera (for direction, color, and intensity)

        [Header("Defaults (When Sky Feature Is Absent)")]
        [SerializeField] Color tint = new Color(1.00f, 0.97f, 0.90f);
        [SerializeField] float exposure = 0.85f;
        [SerializeField] Color fogColor = new Color(0.75f, 0.88f, 1.00f);

        [Header("Fog")]
        [SerializeField] float fogStart = 14f;
        [SerializeField] float fogEnd = 30f;
        [SerializeField, Range(0f, 1f)] float fogMax = 0.35f;

        public float NightFactor { get; private set; }

        static readonly int KeyDirId = Shader.PropertyToID("_FlyKeyDir");
        static readonly int TintId = Shader.PropertyToID("_FlyTint");
        static readonly int ExposureId = Shader.PropertyToID("_FlyExposure");
        static readonly int CenterId = Shader.PropertyToID("_FlyPlanetCenter");
        static readonly int FogColorId = Shader.PropertyToID("_FlyFogColor");
        static readonly int FogRangeId = Shader.PropertyToID("_FlyFogRange");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Current = null;

        /// Called every frame by the Sky feature.
        public void SetTimeOfDay(Color newTint, float newExposure, Color newFogColor, float nightFactor)
        {
            tint = newTint;
            exposure = newExposure;
            fogColor = newFogColor;
            NightFactor = nightFactor;
        }

        void Awake()
        {
            Current = this;
            if (planet == null && GameContext.Current != null) planet = GameContext.Current.Planet;
            Push();
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        void LateUpdate() => Push();

        void Push()
        {
            Vector3 keyDir = keyLight != null ? -keyLight.transform.forward : new Vector3(0.45f, 0.8f, 0.4f).normalized;
            if (keyLight != null)
            {
                keyLight.color = tint;
                keyLight.intensity = exposure;
            }
            Shader.SetGlobalVector(KeyDirId, keyDir);
            Shader.SetGlobalColor(TintId, tint);
            Shader.SetGlobalFloat(ExposureId, exposure);
            Shader.SetGlobalVector(CenterId, planet != null ? planet.Center : Vector3.zero);
            Shader.SetGlobalColor(FogColorId, fogColor);
            Shader.SetGlobalVector(FogRangeId, new Vector4(fogStart, fogEnd, fogMax, 0f));
        }
    }
}
