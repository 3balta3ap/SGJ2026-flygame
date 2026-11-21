using UnityEngine;
using UnityEngine.UI;

namespace FlyJam
{
    public class PixelRenderer : MonoBehaviour
    {
        [SerializeField] Camera worldCamera;
        [SerializeField] RawImage output;              // Material = M_PixelArtPost
        [SerializeField, Range(90, 360)] int artHeight = 200;
        [SerializeField] bool supersample = true;
        [SerializeField] bool pixelOn = true;

        RenderTexture rt;
        int lastW, lastH, lastArtH;
        bool lastOn, lastSS;

        static readonly int ArtSizeId = Shader.PropertyToID("_ArtSize");
        static readonly int SSId = Shader.PropertyToID("_SS");

        void OnEnable() => Apply(true);

        void OnDisable()
        {
            Release();
            if (worldCamera != null) worldCamera.targetTexture = null;
        }

        void Update()
        {
            if (InputCompat.TogglePixelPressed()) pixelOn = !pixelOn;
            Apply(false);
        }

        void Apply(bool force)
        {
            bool changed = force || pixelOn != lastOn || supersample != lastSS || Screen.width != lastW
                        || Screen.height != lastH || artHeight != lastArtH;
            if (!changed) return;
            lastOn = pixelOn; lastSS = supersample; lastW = Screen.width; lastH = Screen.height; lastArtH = artHeight;

            Release();
            worldCamera.allowHDR = false;    // HDR intermediate textures lose the alpha (layer) channel
            worldCamera.allowMSAA = false;

            if (!pixelOn)
            {
                worldCamera.targetTexture = null;
                output.enabled = false;
                return;
            }

            int ss = supersample ? 2 : 1;
            int h = artHeight;
            int w = Mathf.Max(1, Mathf.RoundToInt(h * (float)Screen.width / Mathf.Max(1, Screen.height)));
            rt = new RenderTexture(w * ss, h * ss, 24, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Point,
                antiAliasing = 1,
                name = "PixelArtRT"
            };
            rt.Create();
            worldCamera.targetTexture = rt;
            output.texture = rt;
            output.enabled = true;
            if (output.material != null)
            {
                output.material.SetVector(ArtSizeId, new Vector4(w, h, 0f, 0f));
                output.material.SetFloat(SSId, ss);
            }
        }

        void Release()
        {
            if (rt == null) return;
            if (worldCamera != null && worldCamera.targetTexture == rt) worldCamera.targetTexture = null;
            rt.Release();
            Destroy(rt);
            rt = null;
        }
    }
}
