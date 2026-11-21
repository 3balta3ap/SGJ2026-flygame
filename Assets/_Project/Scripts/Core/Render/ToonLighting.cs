using UnityEngine;

namespace FlyJam
{
    /// Toon 셰이더·픽셀 후처리가 쓰는 전역 값(_Fly*)을 매 프레임 넣는다.
    /// 하늘 시스템이 없으면 아래 기본값(맑은 낮)으로 동작한다. 하늘 시스템은 SetTimeOfDay로 값만 넘긴다.
    [DefaultExecutionOrder(1000)]
    public class ToonLighting : MonoBehaviour
    {
        public static ToonLighting Current { get; private set; }

        [SerializeField] Planet planet;
        [SerializeField] Light keyLight;   // Main Camera 자식 Directional Light (방향 + 색·밝기 표시용)

        [Header("기본값 (하늘 시스템이 없을 때)")]
        [SerializeField] Color tint = new Color(1.00f, 0.97f, 0.90f);
        [SerializeField] float exposure = 0.85f;
        [SerializeField] Color fogColor = new Color(0.75f, 0.88f, 1.00f);

        [Header("안개")]
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

        /// 하늘 시스템이 매 프레임 호출한다.
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
