Shader "FlyJam/PixelDither"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Levels ("Color Levels", Range(2, 16)) = 5
        _Dither ("Dither Strength", Range(0, 1)) = 0.6
        _TexSize ("Texture Size (set by script)", Vector) = (320, 180, 0.003125, 0.005555)
        _PaletteTex ("Palette (optional, Nx1)", 2D) = "white" {}
        _PaletteSize ("Palette Size", Range(2, 32)) = 16
        _UsePalette ("Use Palette", Float) = 0
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
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _PaletteTex;
            float4 _TexSize;
            float _Levels, _Dither, _PaletteSize, _UsePalette;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            static const float bayer[16] = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };

            float3 NearestPalette(float3 c)
            {
                float best = 1e9;
                float3 result = c;
                for (int i = 0; i < 32; i++)
                {
                    if (i >= (int)_PaletteSize) break;
                    float3 p = tex2D(_PaletteTex, float2((i + 0.5) / _PaletteSize, 0.5)).rgb;
                    float d = dot(c - p, c - p);
                    if (d < best) { best = d; result = p; }
                }
                return result;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 px = floor(i.uv * _TexSize.xy);
                float2 uv = (px + 0.5) * _TexSize.zw;
                float3 c = tex2D(_MainTex, uv).rgb;

            #if !defined(UNITY_COLORSPACE_GAMMA)
                c = LinearToGammaSpace(c);
            #endif

                int idx = (int)(fmod(px.x, 4.0) + fmod(px.y, 4.0) * 4.0);
                float threshold = (bayer[idx] + 0.5) / 16.0 - 0.5;
                float stepSize = 1.0 / (_Levels - 1.0);
                c = saturate(c + threshold * stepSize * _Dither);

                if (_UsePalette > 0.5) c = NearestPalette(c);
                else c = round(c / stepSize) * stepSize;

            #if !defined(UNITY_COLORSPACE_GAMMA)
                c = GammaToLinearSpace(c);
            #endif
                return fixed4(c, 1);
            }
            ENDCG
        }
    }
}
