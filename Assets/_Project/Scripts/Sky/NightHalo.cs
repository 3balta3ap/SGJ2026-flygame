using UnityEngine;

namespace FlyJam
{
    /// 밤이 될수록 halo 렌더러(FlyJam/Halo)의 밝기를 올린다.
    public class NightHalo : MonoBehaviour
    {
        [SerializeField] Renderer halo;
        [SerializeField] float dayIntensity = 0f;
        [SerializeField] float nightIntensity = 1.4f;

        static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        MaterialPropertyBlock mpb;

        void Awake() => mpb = new MaterialPropertyBlock();

        void LateUpdate()
        {
            if (halo == null) return;
            float n = DayNightCycle.Current != null ? DayNightCycle.Current.NightFactor : 0f;
            halo.GetPropertyBlock(mpb);
            mpb.SetFloat(IntensityId, Mathf.Lerp(dayIntensity, nightIntensity, n));
            halo.SetPropertyBlock(mpb);
        }
    }
}
