using UnityEngine;

namespace FlyJam
{
    /// Male fly at night: eyes glow red and pulse, its halo turns red. Shows "the males got stronger" without words.
    /// Strength follows the night (ToonLighting.NightFactor). Put on Enemy_Male; eyes = EyeL/EyeR renderers, halo = Halo renderer.
    public class NightGlowEyes : MonoBehaviour
    {
        [SerializeField] Renderer[] eyes;
        [SerializeField] Renderer halo;
        [SerializeField] Color glowColor = new Color(1f, 0.12f, 0.08f, 1f);
        [SerializeField] Color haloNightColor = new Color(1f, 0.25f, 0.2f, 1f);
        [SerializeField] float pulseSpeed = 5f;
        [Range(0f, 1f)] [SerializeField] float pulseDepth = 0.35f;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int EmissiveId = Shader.PropertyToID("_Emissive");
        static readonly int HaloColorId = Shader.PropertyToID("_Color");

        MaterialPropertyBlock mpb;
        Color[] baseColors;
        Color haloBase = Color.white;
        float phase;

        void Start()
        {
            mpb = new MaterialPropertyBlock();
            phase = Random.Range(0f, 6.28f);
            baseColors = new Color[eyes != null ? eyes.Length : 0];
            for (int i = 0; i < baseColors.Length; i++)
            {
                var m = eyes[i] != null ? eyes[i].sharedMaterial : null;   // after ToonifyOnSpawn (Awake)
                baseColors[i] = m != null && m.HasProperty(BaseColorId) ? m.GetColor(BaseColorId) : Color.black;
            }
            if (halo != null && halo.sharedMaterial != null && halo.sharedMaterial.HasProperty(HaloColorId))
                haloBase = halo.sharedMaterial.GetColor(HaloColorId);
        }

        void LateUpdate()
        {
            if (mpb == null) return;
            float n = ToonLighting.Current != null ? ToonLighting.Current.NightFactor : 0f;
            float pulse = 1f - pulseDepth * (0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed + phase));
            float k = n * pulse;

            for (int i = 0; i < baseColors.Length; i++)
            {
                var r = eyes[i];
                if (r == null) continue;
                r.GetPropertyBlock(mpb);
                mpb.SetColor(BaseColorId, Color.Lerp(baseColors[i], glowColor, k));
                mpb.SetFloat(EmissiveId, k > 0.05f ? 1f : 0f);
                r.SetPropertyBlock(mpb);
            }

            if (halo != null)
            {
                halo.GetPropertyBlock(mpb);   // NightHalo sets _Intensity on the same block
                mpb.SetColor(HaloColorId, Color.Lerp(haloBase, haloNightColor, n));
                halo.SetPropertyBlock(mpb);
            }
        }
    }
}
