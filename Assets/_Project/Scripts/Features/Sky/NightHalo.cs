using UnityEngine;

namespace FlyJam
{
    /// Increases halo renderer (FlyJam/Halo) brightness as night falls. Stays daytime if the Sky feature is absent.
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
            float n = ToonLighting.Current != null ? ToonLighting.Current.NightFactor : 0f;
            halo.GetPropertyBlock(mpb);
            mpb.SetFloat(IntensityId, Mathf.Lerp(dayIntensity, nightIntensity, n));
            halo.SetPropertyBlock(mpb);
        }
    }
}
