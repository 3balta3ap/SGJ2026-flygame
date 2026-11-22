using UnityEngine;
using UnityEngine.UI;

namespace FlyJam
{
    /// World camera -> low-res render texture (artHeight tall, x2 when supersampling) -> pixel-art post (M_PixelArtPost)
    /// -> art-size texture -> shown full screen by a RawImage with point filtering.
    /// The post shader runs once per ART pixel (e.g. 480x270), not once per screen pixel: on a Retina / web canvas
    /// that is about 30x less work for the same picture.
    public class PixelRenderer : MonoBehaviour
    {
        [SerializeField] Camera worldCamera;
        [SerializeField] RawImage output;              // Material in the scene = M_PixelArtPost (used for the blit)
        [SerializeField, Range(90, 360)] int artHeight = 200;
        [SerializeField] bool supersample = true;
        [SerializeField] bool pixelOn = true;

        RenderTexture rt, postRT;
        Material postMat;
        int lastW, lastH, lastArtH;
        bool lastOn, lastSS;

        static readonly int ArtSizeId = Shader.PropertyToID("_ArtSize");
        static readonly int SSId = Shader.PropertyToID("_SS");

        void Awake()
        {
            if (output != null && output.material != null && output.material != output.defaultMaterial)
                postMat = output.material;               // keep the post material; the RawImage itself draws plainly
            if (output != null) output.material = null;
        }

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

        /// Runs the pixel-art post at art resolution on the last rendered world frame (works with URP and built-in;
        /// one frame of delay, not noticeable). Must not use Camera.onPostRender: URP never calls it.
        void LateUpdate()
        {
            if (rt == null || postRT == null || !rt.IsCreated()) return;
            if (postMat != null) Graphics.Blit(rt, postRT, postMat);
            else Graphics.Blit(rt, postRT);
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
            postRT = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Point,
                antiAliasing = 1,
                name = "PixelPostRT"
            };
            postRT.Create();
            worldCamera.targetTexture = rt;
            output.texture = postRT;
            output.enabled = true;
            if (postMat != null)
            {
                postMat.SetVector(ArtSizeId, new Vector4(w, h, 0f, 0f));
                postMat.SetFloat(SSId, ss);
            }
        }

        void Release()
        {
            if (rt != null)
            {
                if (worldCamera != null && worldCamera.targetTexture == rt) worldCamera.targetTexture = null;
                rt.Release();
                Destroy(rt);
                rt = null;
            }
            if (postRT != null)
            {
                postRT.Release();
                Destroy(postRT);
                postRT = null;
            }
        }
    }
}
