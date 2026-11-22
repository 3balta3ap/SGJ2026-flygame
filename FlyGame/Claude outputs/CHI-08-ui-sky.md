# CHI/08-ui-sky: 체력바 이미지·타이틀 이미지·하늘(구름·별)·바람 삭제

| 항목 | 값 |
|---|---|
| 브랜치 | 없음. 사람이 지금 브랜치(`dev`)에 바로 반영한다 |
| 선행 | **CHI/07(모델 교체)이 agy에서 완전히 끝난 뒤** 실행(같은 유니티를 쓰므로 동시에 돌리지 않는다) |
| 기준 | 2026-11-22 그릴 세션 결정 (1장) |
| 권장 커밋 메시지 | `Apply HP bar and title art, add sky clouds and bigger stars, remove wind gusts` |
| 건드리지 않는 것 | 구애 입력(XABY 그대로), `HealthBar.cs` 로직, `DayNightCycle.cs`, CHI/07에서 바꾼 생물 프리팹 |

> ## ⚠️ 에이전트 실행 모드: **끝까지 무정지**
> 사람은 결과만 본다. 멈추지 말고 1~4단계를 연달아 진행하고, 끝에 6장 형식으로 한 번 보고한다.
> 애매하면 1장 결정에 가장 맞게 직접 정하고 "판단 기록"에 적는다. 막힌 항목은 "미완료"에 적고 다음으로 간다.
> 규칙: 스펙 코드는 원문 그대로, **git 명령 금지**, 스펙 밖 파일·패키지 추가 금지, 끝나면 Console 에러 0 + 모든 프리팹·씬 저장.
> `AssetDatabase.DeleteAsset`은 MCP에서 막혀 있다(AGENTS.md 6장) → 파일 삭제는 셸 + `AssetDatabase.Refresh()`.

---

## 1. 확정된 결정

| 분류 | 결정 |
|---|---|
| 구애 입력 | **기각**(XABY 그대로). 이번엔 손대지 않는다 |
| 체력바 | **구성은 유지**: 왼쪽 "体力" 글자, 오른쪽 "5/10" 숫자와 "+2" 팝업, 레벨업마다 길어짐, 아래 EXP 막대와 배고픔 문구. **그림만 교체**: 사람이 넣은 `Art/HPBackGroundBar.png`(주황 알약 = 빈 칸 색)를 바닥에, 초록 채움·잔상은 그 알약 모양으로 잘려서 그 위에, `Art/HPEmptyBar.png`(사과 + 주황 테두리)를 맨 위에. 사과와 오른쪽 둥근 끝은 늘어나지 않고 가운데만 늘어남(9-slice). 그림 비율(610×120)을 살리려고 막대 높이만 40 → **72**(그림의 0.6배) |
| 체력바 색 | 배경이 주황이라 묻히는 색만 바꿈: 20% 이하 = **빨강 깜빡임**(전엔 주황), 데미지 잔상 = **흰색**(전엔 빨강). 초록(50% 초과)·노랑(50% 이하) 그대로 |
| 타이틀 | `TitleText`(텍스트 제목) **삭제**. `TitleScreen`이 이미지를 끄던 코드 삭제 → 씬의 `TitleImage`에 넣은 그림이 그대로 나온다. 크기 1260×570(원본 비율), 위치 (0,190). 겹치지 않게 `GoalText` (0,-85), `PlayButton` (0,-200) |
| 구름 | **스카이박스 셰이더에 평면 구름**(3D 아님). 방향 기반 노이즈라 파리가 행성을 돌면 구름이 지나간다. 천천히 흘러감. 두 톤(밝은 속 + 그늘진 가장자리), 노을엔 하늘색이 살짝 섞이고 밤엔 어두운 남색. 픽셀 후처리로 자연히 픽셀화 |
| 별 | 셰이더 별을 **크고 많게**: 작은 별(픽셀 2~3개 크기 점) + 밝은 큰 별(6갈래로 반짝임) 몇 개. 반짝임, 밤에만(`DayNightCycle`이 넣는 밤 정도 = `_StarIntensity`), 지평선 위만. 구름이 별을 가린다 |
| 바람 | `WindGusts` 컴포넌트와 스크립트 **삭제**. 꽃가루는 밀리지 않고 그냥 떠다님 |

---

## 2. 파일 변경 목록

경로는 `FlyGame/Assets/_Project/` 기준.

| 파일 | 구분 |
|---|---|
| `Shaders/GradientSky.shader` | 교체 (3-2) |
| `Scripts/UI/TitleScreen.cs` | 교체 (3-3) |
| `Scripts/Features/Fx/WindGusts.cs` (+`.meta`) | **삭제** (3-1 뒤) |
| `Art/HPEmptyBar.png`, `Art/HPBackGroundBar.png`, `Art/TitleImage.png` | 임포트 설정만 (Sprite, 앞의 둘은 9-slice 테두리) |
| `Prefabs/SYS_Fx.prefab` | `WindGusts` 제거 |
| `Prefabs/SYS_Health.prefab` | 체력바 그림·높이·색 |
| `Prefabs/SYS_Level.prefab` | `ExpBar` 위치 (체력바가 높아진 만큼 아래로) |
| `Scenes/Title.unity` | `TitleText` 삭제, `TitleImage`·`GoalText`·`PlayButton` 배치 |
| `FlyGame/AGENTS.md` | 5장 |

---

## 3. 단계 1 — 바람 제거 + 코드·셰이더

### 3-1. `SYS_Fx`에서 `WindGusts` 떼기 (`Unity_RunCommand`, **스크립트를 지우기 전에**)

```csharp
using UnityEditor;
using UnityEngine;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        const string path = "Assets/_Project/Prefabs/SYS_Fx.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        int removed = 0;
        foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
            if (mb != null && mb.GetType().Name == "WindGusts") { Object.DestroyImmediate(mb); removed++; }
        foreach (var ps in root.GetComponentsInChildren<ParticleSystem>(true))
            if (ps.name == "FX_Pollen") { var f = ps.forceOverLifetime; f.enabled = false; }   // pollen just floats
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log("[CHI08] WindGusts removed: " + removed + " (expected 1)");
    }
}
```
그다음 셸로 `FlyGame/Assets/_Project/Scripts/Features/Fx/WindGusts.cs`와 `WindGusts.cs.meta`를 삭제한다(삭제가 막히면 "미완료"에 적고 계속. 컴포넌트는 이미 떼었으므로 게임엔 영향 없음).

### 3-2. `Shaders/GradientSky.shader` (교체)

```hlsl
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
        _StarIntensity ("Night (0-1, set by DayNightCycle)", Range(0, 1)) = 0
        _StarDensity ("(old, unused)", Range(20, 300)) = 120

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
            float _SunSize, _MoonSize, _StarIntensity, _StarDensity;
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

                float3 col = h >= 0
                    ? lerp(_HorizonColor.rgb, _TopColor.rgb, pow(saturate(h), 0.6))
                    : lerp(_HorizonColor.rgb, _BottomColor.rgb, saturate(-h * 1.5));

                col = lerp(col, _SunColor.rgb, Disc(d, _SunDir.xyz, _SunSize) * _SunColor.a);
                col = lerp(col, _MoonColor.rgb, Disc(d, _MoonDir.xyz, _MoonSize) * _MoonColor.a);

                float night = _StarIntensity;

                // Stars: night only, above the horizon
                if (night > 0.001)
                {
                    float above = saturate(h * 3.0 + 0.6);
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
                    float band = saturate(h * 5.0 + 0.25);                              // fade out just below the horizon
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
```
- 미리보기(같은 계산을 파이썬으로 옮겨 270px로 렌더링): `FlyGame/Claude outputs/CHI-08-sky-preview.png`(위 낮, 아래 밤, 방향 3개). 구름은 화면의 약 20~25%.
- 머티리얼(하늘 머티리얼 에셋)은 건드리지 않는다. 새 속성은 셰이더 기본값을 쓴다. `DayNightCycle`이 지금처럼 `_StarIntensity`(밤 정도)를 넣는다.
- 알파 0 = 하늘 레이어(픽셀 후처리가 하늘로 인식) 유지.

### 3-3. `Scripts/UI/TitleScreen.cs` (교체)

```csharp
using UnityEngine;
using UnityEngine.EventSystems;

namespace FlyJam
{
    /// Title (S01, A06). Buttons: あそぶ (StartGame) and the language button (ToggleLanguage), driven by MenuNavigator.
    /// The title logo is a plain UI Image in the scene (Canvas/TitleImage): this component never hides or changes it.
    public class TitleScreen : MonoBehaviour
    {
        void Start() => TimeControl.ResetAll();

        public void StartGame() => SceneFlow.GoToGame();

        public void ToggleLanguage()
        {
            Loc.Toggle();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
```

`AssetDatabase.Refresh()` → 컴파일 → **Console 에러 0**(셰이더 에러 포함. 셰이더 에러가 있으면 고치고 무엇을 고쳤는지 기록).

---

## 4. 단계 2 — 이미지·프리팹·씬 (`Unity_RunCommand`)

```csharp
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

internal class CommandScript : IRunCommand
{
    const string Art = "Assets/_Project/Art/";
    const float Scale = 0.6f;                                        // art 120 px tall -> bar 72 units
    static readonly Vector4 Border = new Vector4(118, 0, 64, 0);     // left = apple, right = round cap (L, B, R, T)

    public void Execute(ExecutionResult result)
    {
        Sprite empty = SpriteAt(Art + "HPEmptyBar.png", Border);
        Sprite back = SpriteAt(Art + "HPBackGroundBar.png", Border);
        Sprite logo = SpriteAt(Art + "TitleImage.png", Vector4.zero);
        if (empty == null || back == null || logo == null) return;
        Health(empty, back);
        Level();
        Title(logo);
        AssetDatabase.SaveAssets();
        Debug.Log("[CHI08] done");
    }

    static Sprite SpriteAt(string path, Vector4 border)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) { Debug.LogError("[CHI08] NOT FOUND " + path); return null; }
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = 100;
        imp.spriteBorder = border;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.wrapMode = TextureWrapMode.Clamp;
        imp.filterMode = FilterMode.Bilinear;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.SaveAndReimport();
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s == null) Debug.LogError("[CHI08] not a sprite: " + path);
        else Debug.Log($"[CHI08] sprite {path} border {s.border}");
        return s;
    }

    static void Sliced(Image img, Sprite s)
    {
        img.sprite = s;
        img.type = Image.Type.Sliced;
        img.fillCenter = true;
        img.pixelsPerUnitMultiplier = 1f / Scale;   // borders drawn at 0.6x: apple 71 units, cap 38 units
        img.color = Color.white;
        img.raycastTarget = false;
    }

    static void Stretch(RectTransform rt, float left, float bottom, float right, float top)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
        rt.localScale = Vector3.one;
    }

    static RectTransform Child(RectTransform parent, string name, params System.Type[] comps)
    {
        var t = parent.Find(name) as RectTransform;
        if (t != null) return t;
        var types = new System.Type[comps.Length + 1];
        types[0] = typeof(RectTransform);
        comps.CopyTo(types, 1);
        var go = new GameObject(name, types);
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    // HP bar: Frame = orange pill (empty part) / Clip = same pill as a mask -> Inner (Chip, Fill) / Outline = apple + border on top.
    // Layout kept: Label left (104), ValueText + DeltaText right, width grows with max HP (HealthBar.cs, unchanged).
    static void Health(Sprite empty, Sprite back)
    {
        const string path = "Assets/_Project/Prefabs/SYS_Health.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var bar = root.transform.Find("Canvas/HealthBar") as RectTransform;
            var frame = bar.Find("Frame") as RectTransform;
            var inner = (frame.Find("Inner") ?? frame.Find("Clip/Inner")) as RectTransform;

            bar.sizeDelta = new Vector2(bar.sizeDelta.x, 120f * Scale);   // 40 -> 72

            Sliced(frame.GetComponent<Image>(), back);

            var clip = Child(frame, "Clip", typeof(Image), typeof(Mask));
            Stretch(clip, 0, 0, 0, 0);
            Sliced(clip.GetComponent<Image>(), back);
            clip.GetComponent<Mask>().showMaskGraphic = false;
            clip.SetSiblingIndex(0);

            inner.SetParent(clip, false);
            Stretch(inner, 66f, 0f, 18f, 0f);   // starts under the apple's right edge, ends at the pill's right end (mask rounds it)

            var outline = Child(frame, "Outline", typeof(Image));
            Stretch(outline, 0, 0, 0, 0);
            Sliced(outline.GetComponent<Image>(), empty);
            outline.SetAsLastSibling();

            var so = new SerializedObject(bar.GetComponent<FlyJam.HealthBar>());
            so.FindProperty("lowColor").colorValue = new Color(0.92f, 0.2f, 0.16f, 1f);
            so.FindProperty("lowDimColor").colorValue = new Color(0.55f, 0.1f, 0.08f, 1f);
            so.FindProperty("chipColor").colorValue = new Color(1f, 1f, 1f, 0.9f);
            so.ApplyModifiedPropertiesWithoutUndo();
            inner.Find("Chip").GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.9f);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Debug.Log("[CHI08] SYS_Health updated");
        }
        catch (System.Exception e) { Debug.LogError("[CHI08] SYS_Health: " + e); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    // EXP bar sits under the HP bar: move it down by the 32 units the HP bar grew
    static void Level()
    {
        const string path = "Assets/_Project/Prefabs/SYS_Level.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var exp = root.transform.Find("Canvas/ExpBar") as RectTransform;
            exp.anchoredPosition = new Vector2(32f, -106f);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Debug.Log("[CHI08] SYS_Level updated");
        }
        catch (System.Exception e) { Debug.LogError("[CHI08] SYS_Level: " + e); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static void Title(Sprite logo)
    {
        const string path = "Assets/_Project/Scenes/Title.unity";
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        Transform canvas = null;
        foreach (var go in scene.GetRootGameObjects()) if (go.name == "Canvas") canvas = go.transform;
        if (canvas == null) { Debug.LogError("[CHI08] Title: NOT FOUND Canvas"); return; }

        var text = canvas.Find("TitleText");
        if (text != null) Object.DestroyImmediate(text.gameObject);

        var img = canvas.Find("TitleImage").GetComponent<Image>();
        img.gameObject.SetActive(true);
        img.enabled = true;
        img.sprite = logo;
        img.type = Image.Type.Simple;
        img.preserveAspect = true;
        img.raycastTarget = false;
        img.color = Color.white;
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, 190f);
        rt.sizeDelta = new Vector2(1260f, 570f);

        ((RectTransform)canvas.Find("GoalText")).anchoredPosition = new Vector2(0f, -85f);
        ((RectTransform)canvas.Find("PlayButton")).anchoredPosition = new Vector2(0f, -200f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[CHI08] Title updated");
    }
}
```
기대 로그: `WindGusts removed: 1`, `sprite ... border (118.0, 0.0, 64.0, 0.0)` ×2, `sprite ...TitleImage.png border (0.0, 0.0, 0.0, 0.0)`, `SYS_Health updated`, `SYS_Level updated`, `Title updated`, `done`.

### 4-1. 확인 (에이전트)

| # | 확인 | 기대 |
|---|---|---|
| 1 | `SYS_Health/Canvas/HealthBar` 계층 | `Label`, `Frame`(주황 알약) → `Clip`(Mask, 그림 숨김) → `Inner` → `Chip`, `Fill` / `Frame/Outline`(사과 + 테두리, 맨 마지막 형제). 높이 72 |
| 2 | `HealthBar` 컴포넌트 참조 | fill, fillImage, chip, chipImage, barRoot 모두 그대로 연결(누락 0) |
| 3 | `Title.unity` | `TitleText` 없음, `TitleImage` 켜짐 + 스프라이트 `TitleImage`, `TitleScreen` 컴포넌트에 Missing 없음 |
| 4 | `SYS_Fx` | `WindGusts` 없음, Missing Script 없음. 프로젝트 검색 `WindGusts` → 코드 0건 |
| 5 | UI 캡처(AGENTS 6장의 임시 ScreenSpaceCamera 방법) | `SYS_Health`(체력바: 사과·주황 테두리·초록 채움), `Title`(로고·목표 문구·あそぶ 버튼이 안 겹침) → 보고서 첨부. **저장하지 말고** 다시 연다 |
| 6 | Console | 에러 0 (셰이더 포함) |

---

## 5. AGENTS.md 수정

`FlyGame/AGENTS.md`:
- 2.3절 `SYS_Fx` 줄에서 `` `WindGusts`(바람 줄기), `` 부분을 지운다.
- 2.3절 목록 끝에 추가:
  ```markdown
  - **하늘** (CHI/08): `GradientSky.shader`가 그라데이션·해·달 + **별**(작은 점 + 반짝이는 큰 별, 밤에만 = `_StarIntensity`) + **구름**(방향 노이즈, 흘러감, 밤엔 남색)을 그린다. 양·크기·속도는 하늘 머티리얼의 Stars/Clouds 속성(`_CloudCover` 0이면 구름 없음).
  - **체력바 그림** (CHI/08): `Art/HPBackGroundBar.png`(빈 칸 = `Frame`), `Clip`(같은 모양 Mask) 안의 `Fill`·`Chip`, `Art/HPEmptyBar.png`(사과 + 테두리 = `Outline`). 9-slice 테두리 (118,0,64,0), 0.6배(높이 72). 길이는 `HealthBar.cs`가 최대 체력으로 정함.
  - **타이틀**: 로고는 `Title.unity`의 `Canvas/TitleImage`에 직접 넣는다(`TitleScreen`은 손대지 않음).
  ```
- 3절 표 CHI 줄 끝에 추가: `08 체력바·타이틀 이미지, 하늘 구름·별, 바람 삭제.`

---

## 6. 보고 형식 (에이전트)

```
## CHI/08 보고
1. 만든/바꾼/지운 파일 (2장 표와 대조)
2. 로그 (3-1, 4장)
3. 스펙과 다르게 한 것 / 판단 기록
4. 미완료와 이유
5. 4-1 표 1~6 결과 + UI 캡처
6. Console 최종 상태
```

---

## 7. 체크포인트 (사람, Play)

| # | 확인 | 성공이면 |
|---|---|---|
| 1 | 타이틀 | BUZZ LIFE 로고가 보이고 텍스트 제목 없음. 목표 문구·あそぶ 버튼과 안 겹침. Play를 여러 번 눌러도 로고가 안 꺼짐 |
| 2 | 체력바 | 왼쪽 "体力" + 사과 아이콘 + 주황 테두리 안에 초록 채움, 오른쪽 "5/10". 레벨업 때 막대가 길어져도 사과·오른쪽 끝 모양 그대로 |
| 3 | 데미지 / 회복 | 깎인 부분이 흰색으로 잠깐 남았다가 사라짐 / 초록이 차오름. 빈 부분은 주황 |
| 4 | 50% 이하 / 20% 이하 | 노랑 / 빨강 깜빡임 + 「おなかがペコペコ…」(문구가 EXP 막대와 안 겹침) |
| 5 | 낮 하늘 | 흰 구름이 떠 있고 천천히 흐름. 파리가 돌면 구름이 지나감. 노을 땐 살짝 물듦 |
| 6 | 밤 하늘 (1:02 무렵) | 구름이 남색으로 어두워지고, 별(작은 점 + 반짝이는 큰 별)이 확실히 보임 |
| 7 | 바람 | 하얀 바람 줄기 없음. 꽃가루는 그냥 떠다님 |
| 8 | Console | 에러 0 |

튜닝(코드 수정 없이): 하늘 머티리얼(`DayNightCycle`의 skyMaterial)에서 `_CloudCover`(구름 양), `_CloudScale`(크기), `_CloudSpeed`(속도), `_StarChance`·`_StarSize`·`_StarBrightness`(별).

---

## 8. 커밋 (사람)

```bash
cd ~/Developer/sgj2026-flygame
git add FlyGame/Assets/_Project/Shaders/GradientSky.shader FlyGame/Assets/_Project/Scripts/UI/TitleScreen.cs \
        FlyGame/Assets/_Project/Scripts/Features/Fx \
        FlyGame/Assets/_Project/Art/HPEmptyBar.png FlyGame/Assets/_Project/Art/HPEmptyBar.png.meta \
        FlyGame/Assets/_Project/Art/HPBackGroundBar.png FlyGame/Assets/_Project/Art/HPBackGroundBar.png.meta \
        FlyGame/Assets/_Project/Art/TitleImage.png FlyGame/Assets/_Project/Art/TitleImage.png.meta \
        FlyGame/Assets/_Project/Prefabs/SYS_Fx.prefab FlyGame/Assets/_Project/Prefabs/SYS_Health.prefab \
        FlyGame/Assets/_Project/Prefabs/SYS_Level.prefab FlyGame/Assets/_Project/Scenes/Title.unity FlyGame/AGENTS.md
git status      # WindGusts.cs가 deleted로 보여야 함
git commit -m "Apply HP bar and title art, add sky clouds and bigger stars, remove wind gusts"
git push
```
(CHI/07 변경을 아직 커밋하지 않았다면 CHI-07 스펙 11장의 `git add`를 먼저 하고 커밋해도 되고, 두 묶음을 한 번에 커밋해도 된다.)
