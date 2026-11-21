// Pixel pass ported from cobanov/pixelart-gaussiansplat (src/render/post.js, src/authoring/light.js)
// MIT License, Copyright (c) 2026 Mert Cobanov
// Changes: HLSL port, 3-level layers in alpha (sky / ground / object) with packed fog amount,
//          surface/sky palette split, OKLab hue-preserving match for surfaces,
//          sky passed through unquantised, fog applied after palette.
Shader "FlyJam/PixelArtPost"
{
    Properties
    {
        [PerRendererData] _MainTex ("Source", 2D) = "black" {}
        _ArtSize ("Art Size (set by script)", Vector) = (356, 200, 0, 0)
        _SS ("Supersample (set by script)", Float) = 2
        _Outline ("Outline", Float) = 1
        _SkyQuantise ("Sky Uses Palette (0 = smooth)", Float) = 0
        _SkyDither ("Sky Dither (only if Sky Uses Palette)", Range(0, 1)) = 0
        _SurfaceDither ("Surface Dither", Range(0, 1)) = 0
        _Quantise ("Use Palette", Float) = 1
        _FogAfterPalette ("Fog After Palette", Float) = 1
        _HuePenalty ("Hue Penalty", Range(0, 0.5)) = 0.12
        _LightnessWeight ("Lightness Weight", Range(0.5, 3)) = 1.4
        _DebugLayers ("Debug Layers", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Cull Off ZWrite Off ZTest Always Blend Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #define MAX_COLOURS 32

            sampler2D _MainTex;
            float4 _ArtSize;
            float _SS, _Outline, _SkyQuantise, _SkyDither, _SurfaceDither, _Quantise, _FogAfterPalette;
            float _HuePenalty, _LightnessWeight, _DebugLayers;

            float4 _Palette[MAX_COLOURS];      // sRGB 0..1 (first _SurfaceCount = surface colors, rest = sky-only)
            float4 _PaletteLab[MAX_COLOURS];   // OKLab
            float _PaletteSize;
            float _SurfaceCount;

            float4 _FlyFogColor;               // Global value from ToonLighting/DayNightCycle (horizon color)

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            static const float BAYER[16] = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };

            float Bayer(float2 p)
            {
                int i = (int)fmod(p.x, 4.0) + (int)fmod(p.y, 4.0) * 4;
                return (BAYER[i] + 0.5) / 16.0;
            }

            float3 ToGamma(float3 c)
            {
            #if !defined(UNITY_COLORSPACE_GAMMA)
                c = LinearToGammaSpace(max(c, 0.0));
            #endif
                return c;
            }

            // Single source texel: rgb = gamma (sRGB) space, a = layer + fog
            float4 Texel(float2 t)
            {
                float2 srcSize = _ArtSize.xy * _SS;
                float4 s = tex2Dlod(_MainTex, float4((t + 0.5) / srcSize, 0, 0));
                s.rgb = ToGamma(s.rgb);
                return s;
            }

            float2 SampleCoord(float2 cell, int k, int n)
            {
                return n == 1 ? cell : cell * 2.0 + float2(k % 2, k / 2);
            }

            // Unpack fog factor from alpha: object 1.0..0.8, ground 0.5..0.3
            float FogOf(float a)
            {
                return a > 0.75 ? saturate((1.0 - a) / 0.2) : saturate((0.5 - a) / 0.2);
            }

            int ArtLayer(float2 cell)
            {
                if (cell.x < 0.0 || cell.y < 0.0 || cell.x >= _ArtSize.x || cell.y >= _ArtSize.y) return 3;
                int n = _SS > 1.5 ? 4 : 1;
                int obj = 0, solid = 0;
                for (int k = 0; k < 4; k++)
                {
                    if (k < n)
                    {
                        float a = Texel(SampleCoord(cell, k, n)).a;
                        if (a > 0.75) obj++;
                        if (a > 0.25) solid++;
                    }
                }
                int need = n == 1 ? 1 : 2;
                return obj >= need ? 2 : (solid >= need ? 1 : 0);
            }

            float3 OkLab(float3 srgb)
            {
                float3 c = GammaToLinearSpace(max(srgb, 0.0));
                float3 lms = float3(
                    0.4122214708 * c.r + 0.5363325363 * c.g + 0.0514459929 * c.b,
                    0.2119034982 * c.r + 0.6806995451 * c.g + 0.1073969566 * c.b,
                    0.0883024619 * c.r + 0.2817188376 * c.g + 0.6299787005 * c.b);
                lms = pow(max(lms, 0.0) + 1e-9, float3(1.0 / 3.0, 1.0 / 3.0, 1.0 / 3.0));
                return float3(
                    0.2104542553 * lms.x + 0.7936177850 * lms.y - 0.0040720468 * lms.z,
                    1.9779984951 * lms.x - 2.4285922050 * lms.y + 0.4505937099 * lms.z,
                    0.0259040371 * lms.x + 0.7827717662 * lms.y - 0.8086757660 * lms.z);
            }

            float2 HueDir(float3 lab) { return lab.yz / (length(lab.yz) + 0.035); }

            int NearestSurface(float3 c)
            {
                float3 t = OkLab(c);
                float2 h = HueDir(t);
                float sat = smoothstep(0.025, 0.07, length(t.yz));
                float lit = smoothstep(0.06, 0.3, t.x);
                float pen = _HuePenalty * sat * lit;

                int best = 0;
                float bd = 1e9;
                [loop] for (int i = 0; i < MAX_COLOURS; i++)
                {
                    if (i >= (int)_SurfaceCount) break;
                    float3 p = _PaletteLab[i].xyz;
                    float3 e = t - p;
                    float2 dh = HueDir(p) - h;
                    float d = _LightnessWeight * e.x * e.x + e.y * e.y + e.z * e.z
                            + pen * dot(dh, dh)
                            + 8.0 * (1.0 - sat) * dot(p.yz, p.yz);
                    if (d < bd) { bd = d; best = i; }
                }
                return best;
            }

            float3 SnapDither(float3 c, float2 cell, float amount, int count)
            {
                float3 c1 = c, c2 = c;
                float d1 = 1e9, d2 = 1e9;
                [loop] for (int i = 0; i < MAX_COLOURS; i++)
                {
                    if (i >= count) break;
                    float3 p = _Palette[i].rgb;
                    float3 e = c - p;
                    float3 w = e * float3(0.30, 0.59, 0.11);
                    float d = dot(w, w) + 0.25 * dot(e, e);
                    if (d < d1) { d2 = d1; c2 = c1; d1 = d; c1 = p; }
                    else if (d < d2) { d2 = d; c2 = p; }
                }
                float3 axis = c2 - c1;
                float t = clamp(dot(c - c1, axis) / max(dot(axis, axis), 1e-5), 0.0, 0.5);
                return Bayer(cell) < t * amount ? c2 : c1;
            }

            float3 Ink(float3 body)
            {
                float3 tone = body * 0.64;
                tone = min(1.0, tone * max(1.0, 0.12 / max(dot(tone, float3(0.30, 0.55, 0.15)), 0.001)));
                return lerp(body, tone, 0.65);
            }

            fixed4 Out(float3 c)
            {
            #if !defined(UNITY_COLORSPACE_GAMMA)
                c = GammaToLinearSpace(c);
            #endif
                return fixed4(c, 1.0);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 cell = floor(i.uv * _ArtSize.xy);
                int n = _SS > 1.5 ? 4 : 1;

                float4 smp[4];
                int obj = 0, solid = 0;
                for (int k = 0; k < 4; k++)
                {
                    smp[k] = float4(0, 0, 0, 0);
                    if (k < n)
                    {
                        smp[k] = Texel(SampleCoord(cell, k, n));
                        if (smp[k].a > 0.75) obj++;
                        if (smp[k].a > 0.25) solid++;
                    }
                }
                int need = n == 1 ? 1 : 2;
                int layer = obj >= need ? 2 : (solid >= need ? 1 : 0);
                float minA = layer == 2 ? 0.75 : 0.25;

                if (_DebugLayers > 0.5)
                    return Out(layer == 2 ? float3(1, 0.3, 0.3) : (layer == 1 ? float3(0.3, 0.8, 0.3) : float3(0.3, 0.4, 1)));

                // ── Sky: output original color without palette quantization or dithering by default ──
                if (layer == 0)
                {
                    float3 sum = 0;
                    for (int s = 0; s < 4; s++) if (s < n) sum += smp[s].rgb;
                    float3 sky = sum / n;
                    if (_SkyQuantise > 0.5 && _Quantise > 0.5) sky = SnapDither(sky, cell, _SkyDither, (int)_PaletteSize);
                    return Out(sky);
                }

                // ── Ground & Objects ──
                bool edge = false;
                if (_Outline > 0.5)
                {
                    int lower = 0;
                    lower += ArtLayer(cell + float2(1, 0)) < layer ? 1 : 0;
                    lower += ArtLayer(cell - float2(1, 0)) < layer ? 1 : 0;
                    lower += ArtLayer(cell + float2(0, 1)) < layer ? 1 : 0;
                    lower += ArtLayer(cell - float2(0, 1)) < layer ? 1 : 0;
                    edge = lower > 0 && lower < 4;
                }

                // Average fog factor of samples belonging to this layer
                float fogSum = 0, fogCnt = 0;
                for (int f = 0; f < 4; f++)
                    if (f < n && smp[f].a > minA) { fogSum += FogOf(smp[f].a); fogCnt += 1; }
                float fog = fogSum / max(fogCnt, 1.0);

                float3 col;
                if (_Quantise > 0.5 && _SurfaceDither <= 0.0)
                {
                    int lab[4];
                    for (int j = 0; j < 4; j++)
                    {
                        lab[j] = -1;
                        if (j < n && smp[j].a > minA) lab[j] = NearestSurface(smp[j].rgb);
                    }
                    int best = -1, score = 0;
                    for (int a = 0; a < 4; a++)
                    {
                        if (lab[a] < 0) continue;
                        int cnt = 0;
                        for (int b = 0; b < 4; b++) if (lab[b] == lab[a]) cnt++;
                        if (cnt > score) { score = cnt; best = lab[a]; }
                    }
                    col = _Palette[max(best, 0)].rgb;
                    if (edge) col = _Palette[NearestSurface(Ink(col))].rgb;
                }
                else
                {
                    float3 sum = 0;
                    float cnt = 0;
                    for (int m = 0; m < 4; m++)
                        if (m < n && smp[m].a > minA) { sum += smp[m].rgb; cnt += 1; }
                    col = sum / max(cnt, 1.0);
                    if (edge) col = Ink(col);
                    if (_Quantise > 0.5)
                        col = edge ? _Palette[NearestSurface(col)].rgb
                                   : SnapDither(col, cell, _SurfaceDither, (int)_SurfaceCount);
                }

                // ── Blend fog smoothly after palette quantization ──
                if (_FogAfterPalette > 0.5) col = lerp(col, ToGamma(_FlyFogColor.rgb), fog);

                return Out(col);
            }
            ENDCG
        }
    }
}
