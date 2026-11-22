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
        _MoonColor ("Moon Color (a = visible)", Color) = (0.85, 0.9, 1, 1)
        _MoonSize ("Moon Size", Range(0.0005, 0.02)) = 0.0015
        _StarIntensity ("Night (0-1, set by DayNightCycle)", Range(0, 1)) = 0
        _StarDensity ("(old, unused)", Range(20, 300)) = 120
        _HorizonH ("Planet Horizon Height (set by DayNightCycle)", Range(-1, 0)) = 0

        [Header(Stars)]
        _StarGrid ("Small Star Grid (bigger = smaller, more)", Range(10, 200)) = 55
        _StarChance ("Small Star Chance", Range(0, 1)) = 0.25
        _StarSize ("Small Star Size", Range(0.05, 0.45)) = 0.22
        _BigStarGrid ("Big Star Grid", Range(4, 60)) = 14
        _BigStarChance ("Big Star Chance", Range(0, 1)) = 0.3
        _BigStarSize ("Big Star Core Size", Range(0.02, 0.3)) = 0.08
        _StarBrightness ("Star Brightness", Range(0, 3)) = 1.6

        [Header(Clouds)]
        _CloudCover ("Cloud Cover (0 = none)", Range(0, 1)) = 0.45
        _CloudScale ("Cloud Scale (bigger = smaller clouds)", Range(0.5, 8)) = 3.5
        _CloudSpeed ("Cloud Drift", Range(0, 0.2)) = 0.02
        _CloudSoft ("Cloud Edge Softness", Range(0.001, 0.2)) = 0.015
        _CloudLight ("Cloud Light", Color) = (1, 1, 1, 1)
        _CloudShade ("Cloud Shade", Color) = (0.8, 0.86, 0.96, 1)
        _CloudNight ("Cloud Night", Color) = (0.2, 0.23, 0.38, 1)
        _CloudAlpha ("Cloud Opacity", Range(0, 1)) = 0.95
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
            float _SunSize, _MoonSize, _StarIntensity, _StarDensity, _HorizonH;
            float _StarGrid, _StarChance, _StarSize, _BigStarGrid, _BigStarChance, _BigStarSize, _StarBrightness;
            float _CloudCover, _CloudScale, _CloudSpeed, _CloudSoft, _CloudAlpha;
            float4 _CloudLight, _CloudShade, _CloudNight;

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            float Hash31(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.zyx + 31.32);
                return frac((p.x + p.y) * p.z);
            }

            float Disc(float3 d, float3 center, float size)
            {
                float a = dot(d, normalize(center));
                return smoothstep(1.0 - size, 1.0 - size * 0.8, a);
            }

            float Noise3(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float n000 = Hash31(i);
                float n100 = Hash31(i + float3(1, 0, 0));
                float n010 = Hash31(i + float3(0, 1, 0));
                float n110 = Hash31(i + float3(1, 1, 0));
                float n001 = Hash31(i + float3(0, 0, 1));
                float n101 = Hash31(i + float3(1, 0, 1));
                float n011 = Hash31(i + float3(0, 1, 1));
                float n111 = Hash31(i + float3(1, 1, 1));
                float x00 = lerp(n000, n100, f.x);
                float x10 = lerp(n010, n110, f.x);
                float x01 = lerp(n001, n101, f.x);
                float x11 = lerp(n011, n111, f.x);
                return lerp(lerp(x00, x10, f.y), lerp(x01, x11, f.y), f.z);
            }

            float Fbm(float3 p)
            {
                float s = 0.0;
                float a = 0.5;
                for (int k = 0; k < 4; k++)
                {
                    s += a * Noise3(p);
                    p = p * 2.03 + 17.17;
                    a *= 0.5;
                }
                return s / 0.9375;
            }

            // One star per lucky grid cell, at a random point. Only points close to the sky sphere show (so sizes stay even).
            // Returns: x = in-sky distance from the star centre (cell units, 9 = none), y = twinkle 0..1, z = random
            float3 StarCell(float3 d, float grid, float chance, float jitter, float seed)
            {
                float3 g = d * grid;
                float3 cell = floor(g);
                float r = Hash31(cell + seed);
                if (r > chance) return float3(9, 0, 0);
                float3 jit = float3(Hash31(cell + seed + 1.7), Hash31(cell + seed + 2.9), Hash31(cell + seed + 4.3)) - 0.5;
                float3 q = g - (cell + 0.5 + jit * jitter);
                float off = dot(q, d);                     // how far the star point is from the sky sphere here
                if (abs(off) > 0.3) return float3(9, 0, 0);
                float3 t = q - off * d;                    // offset along the sky
                float tw = 0.65 + 0.35 * sin(_Time.y * (1.5 + r * 25.0) + r * 200.0);
                return float3(length(t), tw, r);
            }

            float SmallStars(float3 d)
            {
                float3 s = StarCell(d, _StarGrid, _StarChance, 1.0 - 2.0 * _StarSize, 0.0);
                return step(s.x, _StarSize) * s.y;
            }

            // Big stars: a dot plus thin arms along the world axes, cut by the sky = a 4-6 ray sparkle
            float BigStars(float3 d)
            {
                float3 g = d * _BigStarGrid;
                float3 cell = floor(g);
                float r = Hash31(cell + 50.0);
                if (r > _BigStarChance) return 0.0;
                float3 jit = float3(Hash31(cell + 51.7), Hash31(cell + 52.9), Hash31(cell + 54.3)) - 0.5;
                float3 q = g - (cell + 0.5 + jit * 0.4);
                float off = dot(q, d);
                if (abs(off) > 0.12) return 0.0;
                float3 t = q - off * d;
                float len = length(t);
                float tw = 0.55 + 0.45 * sin(_Time.y * (1.2 + r * 5.0) + r * 120.0);
                float core = step(len, _BigStarSize);
                float w = _BigStarSize * 0.35;
                float arm = (step(abs(t.x), w) + step(abs(t.y), w) + step(abs(t.z), w)) > 0.5 ? 1.0 : 0.0;
                arm *= step(len, _BigStarSize * (2.2 + 1.6 * tw));
                return max(core, arm * 0.85) * tw;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float h = dot(d, normalize(_SkyUp.xyz));   // Relative to local "up" at current planet position
                // The planet is small: seen from the camera its real horizon is far below the local horizontal (_HorizonH < 0).
                // hs = 0 at that horizon, 1 straight up, so the gradient, stars and clouds sit on the sky the player actually sees.
                h = (h - _HorizonH) / max(1.0 - _HorizonH, 0.001);

                float3 col = h >= 0
                    ? lerp(_HorizonColor.rgb, _TopColor.rgb, pow(saturate(h), 0.6))
                    : lerp(_HorizonColor.rgb, _BottomColor.rgb, saturate(-h * 1.5));

                col = lerp(col, _SunColor.rgb, Disc(d, _SunDir.xyz, _SunSize) * _SunColor.a);
                col = lerp(col, _MoonColor.rgb, Disc(d, _MoonDir.xyz, _MoonSize) * _MoonColor.a);

                float night = _StarIntensity;

                // Stars: night only, above the horizon
                if (night > 0.001)
                {
                    float above = saturate(h * 4.0 + 0.8);
                    float star = max(SmallStars(d) * 0.8, BigStars(d));
                    col += float3(1.0, 0.97, 0.88) * star * night * above * _StarBrightness;
                }

                // Clouds: noise on the world direction, slowly drifting. They pass by as the fly walks around the planet.
                if (_CloudCover > 0.001)
                {
                    float3 p = d * _CloudScale + float3(1.0, 0.3, 0.7) * (_Time.y * _CloudSpeed);
                    float n = Fbm(p);
                    float thr = lerp(0.68, 0.45, _CloudCover);
                    float mask = smoothstep(thr, thr + _CloudSoft, n);
                    float core = smoothstep(thr + 0.05, thr + 0.05 + _CloudSoft, n);   // lighter inside, shaded rim
                    float band = saturate(h * 6.0 + 0.6);                               // softer right at the planet edge
                    float3 lit = lerp(_CloudLight.rgb, _HorizonColor.rgb, 0.12);         // a touch of sunrise / sunset
                    float3 cloud = lerp(_CloudShade.rgb * lit, lit, core);
                    cloud = lerp(cloud, _CloudNight.rgb * lerp(0.75, 1.0, core), night);
                    col = lerp(col, cloud, mask * band * _CloudAlpha);
                }

                return fixed4(col, 0);
            }
            ENDCG
        }
    }
}
