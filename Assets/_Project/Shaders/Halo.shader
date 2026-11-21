Shader "FlyJam/Halo"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.5, 0.7, 1)
        _Intensity ("Intensity", Range(0, 3)) = 0
        _RimPower ("Rim Power", Range(0.5, 8)) = 2.5
    }
    SubShader
    {
        Tags { "Queue"="Transparent+10" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One One ZWrite Off Cull Back
        ColorMask RGB

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Color;
            float _Intensity, _RimPower;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; float3 n : TEXCOORD0; float3 v : TEXCOORD1; };

            v2f vert (appdata i)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(i.vertex);
                o.n = UnityObjectToWorldNormal(i.normal);
                o.v = WorldSpaceViewDir(i.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float rim = pow(1.0 - saturate(dot(normalize(i.n), normalize(i.v))), _RimPower);
                return fixed4(_Color.rgb * rim * _Intensity, 1);
            }
            ENDCG
        }
    }
}
