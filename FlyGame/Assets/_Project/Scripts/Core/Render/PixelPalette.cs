using UnityEngine;

namespace FlyJam
{
    /// Uploads the surface palette and sky-only palette to the post-process material, including precomputed OKLab values.
    [ExecuteAlways]
    public class PixelPalette : MonoBehaviour
    {
        const int Max = 32;

        [SerializeField] Material postMaterial;   // M_PixelArtPost

        [Tooltip("Colors allowed for ground and object surfaces")]
        [SerializeField] string[] surfaceHex =
        {
            "#0a0a18", "#17152c", "#2b2740", "#4c4a60",   // Night / shadows
            "#807e92", "#b9b6c0", "#fff1c0",              // Neutrals / highlights
            "#1f4a38", "#2f6b3a", "#4f9a4a", "#a8d860", "#d8f0a0",   // Grass / foliage (+ bright lime highlight)
            "#4a2a26", "#7a4a2e", "#b8743a",              // Dirt / wood
            "#e8a860", "#c8372d", "#7a1f2c",              // Fly
            "#ff8fb3", "#c04f78",                         // Mate
            "#f08a2a", "#ffcf5a",                         // Sunset / glow
            "#2f4f8f", "#4f86c6",                         // Water
        };

        [Tooltip("Colors reserved for the sky only (surfaces cannot quantize to these)")]
        [SerializeField] string[] skyOnlyHex = { "#9fd0f0" };

        static readonly int PaletteId = Shader.PropertyToID("_Palette");
        static readonly int LabId = Shader.PropertyToID("_PaletteLab");
        static readonly int SizeId = Shader.PropertyToID("_PaletteSize");
        static readonly int SurfaceId = Shader.PropertyToID("_SurfaceCount");

        void OnEnable() => Push();
        void OnValidate() => Push();

        public void Push()
        {
            if (postMaterial == null) return;
            var rgb = new Vector4[Max];
            var lab = new Vector4[Max];
            int n = 0;
            int surface = Add(surfaceHex, rgb, lab, ref n);
            Add(skyOnlyHex, rgb, lab, ref n);

            postMaterial.SetVectorArray(PaletteId, rgb);
            postMaterial.SetVectorArray(LabId, lab);
            postMaterial.SetFloat(SizeId, n);
            postMaterial.SetFloat(SurfaceId, surface);
        }

        static int Add(string[] hexes, Vector4[] rgb, Vector4[] lab, ref int n)
        {
            int added = 0;
            if (hexes == null) return 0;
            foreach (var hex in hexes)
            {
                if (n >= Max) break;
                if (!ColorUtility.TryParseHtmlString(hex, out var c)) continue;
                rgb[n] = new Vector4(c.r, c.g, c.b, 1f);
                Vector3 l = OkLab(c);
                lab[n] = new Vector4(l.x, l.y, l.z, 0f);
                n++;
                added++;
            }
            return added;
        }

        // sRGB (gamma) -> OKLab. Matches OkLab() in the shader.
        static Vector3 OkLab(Color srgb)
        {
            float r = Mathf.GammaToLinearSpace(srgb.r);
            float g = Mathf.GammaToLinearSpace(srgb.g);
            float b = Mathf.GammaToLinearSpace(srgb.b);
            float l = Cbrt(0.4122214708f * r + 0.5363325363f * g + 0.0514459929f * b);
            float m = Cbrt(0.2119034982f * r + 0.6806995451f * g + 0.1073969566f * b);
            float s = Cbrt(0.0883024619f * r + 0.2817188376f * g + 0.6299787005f * b);
            return new Vector3(
                0.2104542553f * l + 0.7936177850f * m - 0.0040720468f * s,
                1.9779984951f * l - 2.4285922050f * m + 0.4505937099f * s,
                0.0259040371f * l + 0.7827717662f * m - 0.8086757660f * s);
        }

        static float Cbrt(float x) => Mathf.Pow(Mathf.Max(x, 0f) + 1e-9f, 1f / 3f);
    }
}
