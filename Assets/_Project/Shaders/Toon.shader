Shader "FlyJam/Toon"
{
    Properties
    {
        _BaseColor ("Color", Color) = (1, 1, 1, 1)
        _BaseMap ("Texture", 2D) = "white" {}
        _Layer ("Outline Layer (0.5 ground, 1 object)", Float) = 1
        _Bands ("Light Bands (0 = smooth)", Float) = 5
        _Emissive ("Emissive", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull [_Cull]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _BaseMap;
            float4 _BaseMap_ST;
            float4 _BaseColor;
            float _Layer, _Bands, _Emissive;

            // DayNightCycle이 Shader.SetGlobal*로 넣는 값
            float4 _FlyKeyDir;
            float4 _FlyTint;
            float _FlyExposure;
            float4 _FlyPlanetCenter;
            float4 _FlyFogRange;     // x = 시작 거리, y = 끝 거리, z = 최대 세기

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 wn : TEXCOORD1; float3 wp : TEXCOORD2; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.wn = UnityObjectToWorldNormal(v.normal);
                o.wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 albedo = tex2D(_BaseMap, i.uv).rgb * _BaseColor.rgb;
                float3 n = normalize(i.wn);
                float3 up = normalize(i.wp - _FlyPlanetCenter.xyz);
                float3 toCam = _WorldSpaceCameraPos - i.wp;
                float dist = length(toCam);
                toCam /= max(dist, 1e-4);

                float ndl = dot(n, normalize(_FlyKeyDir.xyz));
                float light = lerp(0.36, 0.58, dot(n, up) * 0.5 + 0.5) + 0.62 * max(ndl, 0.0) + 0.22 * max(dot(n, toCam), 0.0);
                if (_Bands > 0.5) light = (floor(light * _Bands) + 0.5) / _Bands;

                float3 col = albedo * light * _FlyExposure * _FlyTint.rgb;
                col *= ndl >= 0.0 ? float3(1.04, 1.0, 0.95) : float3(0.92, 0.95, 1.05);

                // 안개는 여기서 섞지 않는다. 세기만 계산해서 알파에 담고, 후처리가 팔레트 다음에 섞는다.
                float fog = 0.0;
                if (_Emissive > 0.5) col = albedo * 1.15;
                else fog = saturate(_FlyFogRange.z * smoothstep(_FlyFogRange.x, _FlyFogRange.y, dist));

                // 알파 = 층 − 0.2 × 안개  (물체 1.0~0.8, 땅 0.5~0.3)
                return fixed4(saturate(col), _Layer - 0.2 * fog);
            }
            ENDCG
        }
    }
    Fallback Off
}
