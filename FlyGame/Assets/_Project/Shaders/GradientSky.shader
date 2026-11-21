Shader "FlyJam/GradientSky"
{
    Properties
    {
        _TopColor ("Top", Color) = (0.35, 0.6, 0.95, 1)
        _HorizonColor ("Horizon", Color) = (0.75, 0.88, 1, 1)
        _BottomColor ("Bottom", Color) = (0.64, 0.75, 0.85, 1)
        _SkyUp ("Sky Up (world)", Vector) = (0, 1, 0, 0)
        _SunDir ("Sun Dir", Vector) = (0, 0.1, 1, 0)
        _SunColor ("Sun Color (a = visible)", Color) = (1, 0.92, 0.75, 1)
        _SunSize ("Sun Size", Range(0.0005, 0.02)) = 0.0025
        _MoonDir ("Moon Dir", Vector) = (0, 0.1, 1, 0)
        _MoonColor ("Moon Color (a = visible)", Color) = (0.85, 0.9, 1, 0)
        _MoonSize ("Moon Size", Range(0.0005, 0.02)) = 0.0015
        _StarIntensity ("Star Intensity", Range(0, 1)) = 0
        _StarDensity ("Star Density", Range(20, 300)) = 120
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _TopColor, _HorizonColor, _BottomColor, _SkyUp, _SunDir, _SunColor, _MoonDir, _MoonColor;
            float _SunSize, _MoonSize, _StarIntensity, _StarDensity;

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            float Hash13(float3 p) { return frac(sin(dot(p, float3(12.9898, 78.233, 37.719))) * 43758.5453); }

            float Disc(float3 d, float3 center, float size)
            {
                float a = dot(d, normalize(center));
                return smoothstep(1.0 - size, 1.0 - size * 0.8, a);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float h = dot(d, normalize(_SkyUp.xyz));   // Relative to local "up" at current planet position

                float3 col = h >= 0
                    ? lerp(_HorizonColor.rgb, _TopColor.rgb, pow(saturate(h), 0.6))
                    : lerp(_HorizonColor.rgb, _BottomColor.rgb, saturate(-h * 1.5));

                col = lerp(col, _SunColor.rgb, Disc(d, _SunDir.xyz, _SunSize) * _SunColor.a);
                col = lerp(col, _MoonColor.rgb, Disc(d, _MoonDir.xyz, _MoonSize) * _MoonColor.a);

                float n = Hash13(floor(d * _StarDensity));
                float star = step(0.992, n) * (0.6 + 0.4 * sin(_Time.y * 3.0 + n * 100.0));
                col += star * _StarIntensity * saturate(h * 3.0 + 0.6);

                return fixed4(col, 0);
            }
            ENDCG
        }
    }
}
