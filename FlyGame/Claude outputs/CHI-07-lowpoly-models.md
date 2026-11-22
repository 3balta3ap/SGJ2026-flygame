# CHI/07-lowpoly-models: 로우폴리 생물 4종 모델(블렌더) → 프리팹 교체 + 적 모션

| 항목 | 값 |
|---|---|
| 브랜치 | `CHI/07-lowpoly-models` (`dev`에서 생성) |
| 기준 | 2026-11-22 그릴 세션 결정 (1장) |
| 권장 커밋 메시지 | `Replace primitive creatures with low-poly Blender models and add enemy motions` |
| 쓰는 MCP | **blender**(1단계), **unity-mcp**(2단계). 둘 다 `~/.gemini/config/mcp_config.json`에 등록됨 |
| 건드리지 않는 것 | `FlyVisual.cs`(코드 그대로), `RollingObstacle.cs`, 모든 `SYS_*` 프리팹, 씬(검증에서 깨진 참조를 되돌릴 때만 저장), 머티리얼 에셋, 도토리·사과·바나나 |

> ## ⚠️ 에이전트 실행 모드
> - **1단계(블렌더)가 끝나면 멈추고** 5장 체크포인트를 사람에게 보여 준 뒤 "계속"을 기다린다. 그 밖에는 멈추지 않고 끝까지 간다.
> - 스펙 코드는 원문 그대로 쓴다. 고쳤으면 무엇을 왜 고쳤는지 보고한다. **git 명령 금지.** 스펙 밖 파일·패키지 추가 금지. 끝나면 Console 에러 0, 모든 프리팹 저장.
> - 애매한 점은 1장 결정에 가장 맞는 쪽으로 직접 정하고 보고서 "판단 기록"에 적는다.

---

## 0. 사람: 실행 전 준비 (2분)

1. 브랜치:
   ```bash
   cd ~/Developer/sgj2026-flygame
   git switch dev && git pull
   git switch -c CHI/07-lowpoly-models
   ```
   (`FlyGame/ArtSource/build_creatures.py`는 이미 저장소 폴더에 들어 있다. 브랜치를 바꿔도 따라온다.)
2. **블렌더**를 켜고 3D 뷰포트 사이드바(N) → `MCP for Blender` 탭에 **Connected on localhost:9876**이 떠 있는지 확인. 지금 열린 블렌더 파일의 오브젝트는 스크립트가 **전부 지운다**(빈 장면이면 상관없음).
3. **유니티 에디터**를 연다(`FlyGame` 프로젝트).
4. `~/Developer/sgj2026-flygame`에서 agy 실행 → `/mcp`에서 `blender`, `unity-mcp` 둘 다 연결됐는지 확인. 명령·파일 쓰기는 자동 승인으로.

---

## 1. 확정된 결정 (2026-11-22 그릴 세션)

| 분류 | 결정 |
|---|---|
| 애니메이션 방식 | **관절별 단단한 부품 + 기존 코드.** 몸통은 한 메시, 다리·날개·더듬이는 피벗이 관절에 있는 별도 메시. 이름은 지금과 같음(`BodyRoot`, `LegL1`…`LegR3`, `WingL/R`, `AntennaL/R`). 회전 기본값은 모두 0(identity) → `FlyVisual`이 지금과 똑같이 움직인다. 리깅·Animator 없음 |
| 적 모션 | **새로 넣는다**(`CreatureMotion.cs` 하나): 수컷 = 걷는 다리(트라이포드)·몸 출렁·더듬이 흔들림, 거미 = 다리 8개 종종걸음·몸 출렁, 잠자리 = 날개 4개 항상 떨림(기존 몸 위아래 흔들림은 그대로). 멈춰 있을 때(맞고 정지)는 다리가 쉬고, 일시정지 중엔 같이 멈춤 |
| 디자인·크기 | 지금 프리미티브의 귀여운 비율(큰 머리, 큰 빨간 눈, 볼터치, 눈 하이라이트)과 **크기·위치 그대로**. 대시 링·충돌 반경·머리 위 UI 숫자 수정 없음. Flat 셰이딩, 텍스처 없음 |
| 색 | 블렌더 머티리얼 이름 = 기존 유니티 머티리얼 이름. 유니티에서는 **기존 머티리얼을 이름으로 연결**(새 머티리얼 없음). Toon 셰이더는 버텍스 컬러를 안 읽으므로 색 = 머티리얼 슬롯 |
| 플레이어 | 수컷 초파리 메시 `Mdl_FlyMale`(배 끝이 둥글고 검음 = 수컷의 특징). 눈썹(`Brows`)은 꺼 둠 |
| 라이벌 수컷 | 플레이어와 **같은 메시**, 몸·눈만 기존 적 머티리얼(`M_Enemy_MaleBody`, `M_Enemy_MaleEye`), **화난 눈썹 켬** |
| 암컷(목표) | 별도 메시 `Mdl_FlyFemale`: 7% 크게, 배가 길고 끝이 뾰족 + 줄무늬 3개, **속눈썹**(눈마다 3가닥), 머리 위 분홍 **리본**, 볼터치 조금 크게, 몸 색 `M_MateBody`. 부품 이름은 플레이어와 같음 |
| 거미·잠자리 | 거미 `Mdl_Spider`(머리가슴 + 큰 배, 등에 빨간 점 3개, 빨간 눈 4개, 송곳니, 관절 있는 다리 8개), 잠자리 `Mdl_Dragonfly`(초록 겹눈, 줄무늬 꼬리 7마디, 날개 4장, 접은 다리). 각각 지금 `Visual` 좌표계 그대로(발 높이 등) |
| 작업 흐름 | 블렌더 원본은 `Assets` 밖: `FlyGame/ArtSource/build_creatures.py`(만드는 스크립트 = 원본) → `creatures.blend` + FBX 4개(`Assets/_Project/Art/Models/`). 유니티에선 프리팹 `Visual` 아래 프리미티브를 지우고 FBX 인스턴스로 교체(백업은 git). FBX는 **언팩하지 않은 모델 인스턴스**로 넣으므로, 나중에 스크립트로 다시 내보내면 프리팹이 자동으로 바뀐다 |
| 범위 밖 | 도토리, 사과, 바나나, 마을 소품 |

미리보기(같은 스크립트를 Blender 5.3에서 실행해 렌더링): `FlyGame/Claude outputs/CHI-07-preview.png`

---

## 2. 구조

```
FBX (모델)                    프리팹                      움직이는 코드
Mdl_FlyMale                → Fly/Visual/Mdl_FlyMale       FlyVisual (그대로)      ← 플레이어
  BodyRoot                 → Enemy_Male/Visual/Mdl_FlyMale CreatureMotion (신규)  ← 라이벌 (머티리얼만 교체 + Brows 켬)
    Body (머리+가슴+배+볼+눈 하이라이트, 머티리얼 여러 개)
    EyeL, EyeR             ← NightGlowEyes(수컷 밤 눈)가 이 렌더러를 씀 → 몸통과 분리
    LegL1 LegR1 LegL2 LegR2 LegL3 LegR3   (피벗 = 엉덩이)
    WingL, WingR           (피벗 = 날개 뿌리)
    AntennaL, AntennaR     (피벗 = 더듬이 뿌리)
    Brows                  (수컷만)
Mdl_FlyFemale              → Mate/Visual/Mdl_FlyFemale    FlyVisual (그대로)      ← 암컷 (Fly의 Variant: 물려받은 Mdl_FlyMale은 끔)
Mdl_Spider / BodyRoot / Body, LegL1…LegR4                 → Enemy_Spider/Visual    CreatureMotion
Mdl_Dragonfly / BodyRoot / Body, WingFL WingFR WingBL WingBR → Enemy_Dragonfly/Visual CreatureMotion
```
- 각 적의 `Visual/Halo`(밤 후광, `NightHalo`가 씀)는 **지우지 않는다**.
- 좌표: 스크립트는 유니티 좌표(x 오른쪽, y 위, z 앞)로 쓰고 블렌더 좌표(앞 = −Y, 위 = +Z)로 바꿔서 만든다. FBX는 `Apply Transform`(bake_space_transform)으로 내보내므로 유니티에서 모든 부품의 회전이 0이어야 한다(6-2가 확인).
- 크기(유니티 단위): 플레이어 길이 약 0.96·높이 0.81, 암컷 길이 1.11, 거미 폭 0.88, 잠자리 날개폭 1.3.

---

## 3. 파일 변경 목록

| 파일 | 구분 |
|---|---|
| `FlyGame/ArtSource/build_creatures.py` | 이미 있음 (사람이 넣어 둠, 수정 금지) |
| `FlyGame/ArtSource/creatures.blend` | 신규 (1단계 스크립트가 저장) |
| `Assets/_Project/Art/Models/Mdl_FlyMale.fbx`, `Mdl_FlyFemale.fbx`, `Mdl_Spider.fbx`, `Mdl_Dragonfly.fbx` (+ `.meta`) | 신규 (1단계) |
| `Assets/_Project/Scripts/Features/Hazards/CreatureMotion.cs` | 신규 (6-1) |
| `Prefabs/Fly.prefab`, `Mate.prefab`, `Enemy_Male.prefab`, `Enemy_Spider.prefab`, `Enemy_Dragonfly.prefab` | 수정 (6-3) |
| `FlyGame/AGENTS.md` | 7장 |

경로의 `Assets/...`와 `Prefabs/...`는 `FlyGame/Assets/_Project/` 기준.

---

## 4. 1단계 — 블렌더 (blender MCP)

1. `get_scene_info`로 연결 확인.
2. `execute_blender_code`로 **아래 코드를 그대로** 실행(스크립트 파일을 읽어 실행만 한다. 스크립트 내용은 고치지 않는다):
   ```python
   FLYGAME_ROOT = "/Users/hyo/Developer/sgj2026-flygame/FlyGame"
   ns = {"FLYGAME_ROOT": FLYGAME_ROOT, "__name__": "__main__"}
   exec(open(FLYGAME_ROOT + "/ArtSource/build_creatures.py").read(), ns)
   print(ns["REPORT"])
   ```
   기대 출력(삼각형 수 정확히 일치):
   ```
   Mdl_FlyMale: 15 objects, 1348 tris -> .../Assets/_Project/Art/Models/Mdl_FlyMale.fbx
   Mdl_FlyFemale: 14 objects, 1548 tris -> ...
   Mdl_Spider: 10 objects, 956 tris -> ...
   Mdl_Dragonfly: 6 objects, 1084 tris -> ...
   ```
   - `bpy.ops.export_scene.fbx` 컨텍스트 오류가 나면: 같은 코드를 `with bpy.context.temp_override(window=bpy.context.window_manager.windows[0]):` 안에서 다시 실행.
   - MCP 실행 자체가 안 되면(타임아웃 등) 대신 셸로:
     ```bash
     "$HOME/Library/Application Support/Steam/steamapps/common/Blender/Blender.app/Contents/MacOS/Blender" -b \
       --python-expr 'FLYGAME_ROOT="/Users/hyo/Developer/sgj2026-flygame/FlyGame"; exec(open(FLYGAME_ROOT+"/ArtSource/build_creatures.py").read())'
     ```
     (블렌더 경로가 다르면 `mdfind -name Blender.app`으로 찾는다.)
3. 확인: `FlyGame/ArtSource/creatures.blend`, FBX 4개가 생겼는지. 블렌더 장면엔 4종이 x축으로 한 줄(1.6 간격)로 서 있다(미리보기 배치. FBX는 각자 원점 기준으로 이미 내보냄).
4. `look`으로 **여러 각도 시트**(앞·옆·위·뒤 위)를 찍는다. 거미·잠자리는 원점 아래로 발/몸이 내려가 있는 게 정상이다(유니티 `Visual` 좌표계 그대로라서).

### 5. 체크포인트 1 (사람) — 여기서 멈춤

에이전트는 4번 이미지와 출력 4줄을 보여 주고 기다린다. 사람이 확인할 것:
| # | 확인 | 기대 |
|---|---|---|
| 1 | 플레이어(왼쪽 첫째) | 큰 머리·큰 빨간 눈·흰 하이라이트·분홍 볼, 다리 6개, 날개 2장, 더듬이 2개, 배 끝이 둥글고 갈색. 눈 위 갈색 눈썹(유니티에서 플레이어는 꺼짐) |
| 2 | 암컷(둘째) | 조금 큼, 속눈썹, 머리 위 분홍 리본, 길고 뾰족한 배에 갈색 줄무늬 |
| 3 | 거미(셋째) | 다리 8개가 무릎에서 꺾임, 등에 빨간 점, 앞에 빨간 눈 |
| 4 | 잠자리(넷째) | 초록 겹눈, 날개 4장 수평, 줄무늬 긴 꼬리 |

고칠 점이 있으면 사람이 말한다 → **`build_creatures.py`의 숫자만** 고치고(유니티 좌표, 위 주석 참고) 4-2를 다시 실행. 괜찮으면 "계속" → 6장.

---

## 6. 2단계 — 유니티 (unity-mcp)

### 6-1. `Scripts/Features/Hazards/CreatureMotion.cs` (신규, `FlyJam.Hazards` 어셈블리)

```csharp
using UnityEngine;

namespace FlyJam
{
    /// Procedural motion for enemy creatures (they have no SphereWalker, so FlyVisual cannot drive them).
    /// Legs walk in two alternating groups while the enemy moves (6 legs = tripod gait like FlyVisual, 8 legs = spider),
    /// the body bobs with the steps, wings buzz (dragonfly: always), antennae sway.
    /// Legs rest while the enemy is stopped (RollingObstacle.IsMoving false); everything freezes while the session is paused.
    /// Pivot convention (same as FlyVisual): rest rotation = identity, x = side, y = up, z = forward.
    public class CreatureMotion : MonoBehaviour
    {
        [SerializeField] RollingObstacle body;              // empty = this object or a parent
        [Tooltip("Optional: moved up and down with the steps")]
        [SerializeField] Transform bodyRoot;
        [Tooltip("Front to back, left then right: L1, R1, L2, R2 ...")]
        [SerializeField] Transform[] legs = new Transform[0];
        [SerializeField] float stepsPerSecond = 5f;
        [SerializeField] float legSwingAngle = 20f;
        [SerializeField] float legLiftAngle = 12f;
        [SerializeField] float bodyBob = 0.015f;
        [Tooltip("Left then right, front pair first: L, R, L, R")]
        [SerializeField] Transform[] wings = new Transform[0];
        [Tooltip("Buzz even when not moving (dragonfly)")]
        [SerializeField] bool wingsAlways;
        [SerializeField] float wingFlapAngle = 28f;
        [SerializeField] float wingFlapFrequency = 14f;
        [SerializeField] Transform[] antennae = new Transform[0];
        [SerializeField] float antennaSway = 10f;
        [SerializeField] float blend = 12f;

        Quaternion[] legRest, wingRest, antRest;
        Vector3 bodyRest;
        float phase, clock;

        void Awake()
        {
            if (body == null) body = GetComponentInParent<RollingObstacle>();
            legRest = Rest(legs);
            wingRest = Rest(wings);
            antRest = Rest(antennae);
            if (bodyRoot != null) bodyRest = bodyRoot.localPosition;
            phase = Random.Range(0f, Mathf.PI * 2f);
            clock = Random.Range(0f, 10f);
        }

        static Quaternion[] Rest(Transform[] ts)
        {
            var r = new Quaternion[ts != null ? ts.Length : 0];
            for (int i = 0; i < r.Length; i++) r[i] = ts[i] != null ? ts[i].localRotation : Quaternion.identity;
            return r;
        }

        void LateUpdate()
        {
            var ctx = GameContext.Current;
            if (ctx != null && ctx.Session != null && ctx.Session.IsPaused) return;   // frozen with the world (E04)

            float dt = Time.deltaTime;
            float k = 1f - Mathf.Exp(-blend * dt);
            bool moving = body == null || body.IsMoving;
            clock += dt;
            if (moving) phase += stepsPerSecond * dt * Mathf.PI * 2f;

            for (int i = 0; i < legs.Length; i++)
            {
                if (legs[i] == null) continue;
                if (!moving)
                {
                    legs[i].localRotation = Quaternion.Slerp(legs[i].localRotation, legRest[i], k);
                    continue;
                }
                bool groupA = ((i / 2) + (i % 2)) % 2 == 0;     // L1, R2, L3 (, R4) step together
                float p = phase + (groupA ? 0f : Mathf.PI);
                float side = i % 2 == 0 ? 1f : -1f;             // left = +1 (same signs as FlyVisual)
                float swing = Mathf.Sin(p) * legSwingAngle * side;
                float lift = Mathf.Max(0f, Mathf.Cos(p)) * legLiftAngle * -side;
                legs[i].localRotation = legRest[i] * Quaternion.Euler(0f, swing, lift);
            }

            if (bodyRoot != null)
            {
                Vector3 target = moving ? bodyRest + Vector3.up * Mathf.Abs(Mathf.Sin(phase)) * bodyBob : bodyRest;
                bodyRoot.localPosition = moving ? target : Vector3.Lerp(bodyRoot.localPosition, target, k);
            }

            bool buzz = wingsAlways || moving;
            for (int i = 0; i < wings.Length; i++)
            {
                if (wings[i] == null) continue;
                if (!buzz)
                {
                    wings[i].localRotation = Quaternion.Slerp(wings[i].localRotation, wingRest[i], k);
                    continue;
                }
                float side = i % 2 == 0 ? 1f : -1f;
                float pair = (i / 2) % 2 == 0 ? 0f : Mathf.PI * 0.5f;   // back pair a quarter beat behind
                float flap = Mathf.Sin(clock * wingFlapFrequency * Mathf.PI * 2f + pair) * wingFlapAngle;
                wings[i].localRotation = wingRest[i] * Quaternion.Euler(0f, 0f, flap * side);
            }

            for (int i = 0; i < antennae.Length; i++)
            {
                if (antennae[i] == null) continue;
                float s = Mathf.Sin(clock * 3f + i * 1.7f) * antennaSway;
                antennae[i].localRotation = antRest[i] * Quaternion.Euler(s, 0f, s * 0.5f);
            }
        }
    }
}
```
컴파일 → Console 에러 0.

### 6-2. FBX 임포트 설정 + 확인 (`Unity_RunCommand`)

```csharp
using UnityEditor;
using UnityEngine;

internal class CommandScript : IRunCommand
{
    const string Dir = "Assets/_Project/Art/Models/";
    static readonly string[] Names = { "Mdl_FlyMale", "Mdl_FlyFemale", "Mdl_Spider", "Mdl_Dragonfly" };

    public void Execute(ExecutionResult result)
    {
        AssetDatabase.Refresh();
        foreach (var n in Names)
        {
            string path = Dir + n + ".fbx";
            var imp = AssetImporter.GetAtPath(path) as ModelImporter;
            if (imp == null) { Debug.LogError("[CHI07] NOT FOUND " + path); continue; }
            imp.globalScale = 1f;
            imp.useFileScale = true;
            imp.importCameras = false;
            imp.importLights = false;
            imp.importAnimation = false;
            imp.importBlendShapes = false;
            imp.animationType = ModelImporterAnimationType.None;
            imp.addCollider = false;
            imp.isReadable = false;
            imp.importNormals = ModelImporterNormals.Import;
            imp.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            imp.materialLocation = ModelImporterMaterialLocation.InPrefab;
            imp.SaveAndReimport();
            bool remapped = imp.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName, ModelImporterMaterialSearch.Everywhere);
            imp.SaveAndReimport();
            Debug.Log("[CHI07] imported " + n + " remap=" + remapped);
        }
        foreach (var n in Names) Report(n);
    }

    static void Report(string n)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Dir + n + ".fbx");
        if (asset == null) return;
        var go = Object.Instantiate(asset);
        go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        var rs = go.GetComponentsInChildren<Renderer>();
        Bounds b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        int rotated = 0;
        foreach (var t in go.GetComponentsInChildren<Transform>())
            if (t != go.transform && Quaternion.Angle(t.localRotation, Quaternion.identity) > 1f) rotated++;
        var eye = go.transform.Find("BodyRoot/EyeL");
        Debug.Log($"[CHI07] {n}: min {b.min:F2} max {b.max:F2}, rotated parts {rotated}" + (eye != null ? $", EyeL {eye.position:F2}" : ""));
        Object.DestroyImmediate(go);
    }
}
```
기대 로그(±0.02):
| 모델 | min | max | EyeL | rotated parts |
|---|---|---|---|---|
| Mdl_FlyMale | (-0.37, 0.00, -0.50) | (0.37, 0.81, 0.46) | (-0.16, 0.49, 0.32) | 0 |
| Mdl_FlyFemale | (-0.39, 0.00, -0.66) | (0.39, 0.87, 0.45) | (-0.17, 0.52, 0.34) | 0 |
| Mdl_Spider | (-0.44, -0.27, -0.48) | (0.44, 0.16, 0.45) | — | 0 |
| Mdl_Dragonfly | (-0.65, -0.16, -0.84) | (0.65, 0.12, 0.47) | — | 0 |

다르면(고친 내용은 판단 기록에):
- 크기가 100배 / 0.01배 → `imp.globalScale`을 0.01 / 100으로 바꿔 다시 실행.
- `rotated parts` > 0 이거나 EyeL이 (-0.16, **-0.32**, **0.49**)처럼 축이 바뀜 → `imp.bakeAxisConversion = true;`를 추가해 다시 실행.
- EyeL이 (**0.16**, 0.49, **-0.32**)(뒤를 봄) → 6-3의 `ModelYaw`를 180으로.
- `remap=False`는 괜찮다. 머티리얼은 6-3에서 이름으로 다시 연결한다.

### 6-3. 프리팹 교체 (`Unity_RunCommand`)

```csharp
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

internal class CommandScript : IRunCommand
{
    const string P = "Assets/_Project/Prefabs/";
    const string M = "Assets/_Project/Art/Models/";
    const float ModelYaw = 0f;   // 180 only if 6-2 said the models face backward

    public void Execute(ExecutionResult result)
    {
        var male = AssetDatabase.LoadAssetAtPath<GameObject>(M + "Mdl_FlyMale.fbx");
        var female = AssetDatabase.LoadAssetAtPath<GameObject>(M + "Mdl_FlyFemale.fbx");
        var spider = AssetDatabase.LoadAssetAtPath<GameObject>(M + "Mdl_Spider.fbx");
        var dragon = AssetDatabase.LoadAssetAtPath<GameObject>(M + "Mdl_Dragonfly.fbx");
        if (male == null || female == null || spider == null || dragon == null) { Debug.LogError("[CHI07] NOT FOUND model"); return; }

        // 1) Player: male mesh, brows off
        Edit("Fly.prefab", root =>
        {
            var visual = root.transform.Find("Visual");
            Clear(visual, null);
            var m = Place(male, visual, null);
            m.transform.Find("BodyRoot/Brows").gameObject.SetActive(false);
            WireFly(visual.GetComponent<FlyJam.FlyVisual>(), m.transform);
        });

        // 2) Female (variant of Fly): hide the inherited male model, add hers, point FlyVisual at her parts
        Edit("Mate.prefab", root =>
        {
            var visual = root.transform.Find("Visual");
            var inherited = visual.Find("Mdl_FlyMale");
            if (inherited != null) inherited.gameObject.SetActive(false);
            var again = visual.Find("Mdl_FlyFemale");
            if (again != null) Object.DestroyImmediate(again.gameObject);
            var f = Place(female, visual, null);
            WireFly(visual.GetComponent<FlyJam.FlyVisual>(), f.transform);
        });

        // 3) Rival male: player mesh, dark materials, angry brows, walking legs
        var rival = new Dictionary<string, string> { { "M_FlyBody", "M_Enemy_MaleBody" }, { "M_FlyEye", "M_Enemy_MaleEye" } };
        Edit("Enemy_Male.prefab", root =>
        {
            var visual = root.transform.Find("Visual");
            Clear(visual, "Halo");
            var br = Place(male, visual, rival).transform.Find("BodyRoot");
            br.Find("Brows").gameObject.SetActive(true);
            var eyes = new SerializedObject(root.GetComponent<FlyJam.NightGlowEyes>());
            SetArray(eyes.FindProperty("eyes"), br.Find("EyeL").GetComponent<Renderer>(), br.Find("EyeR").GetComponent<Renderer>());
            eyes.ApplyModifiedPropertiesWithoutUndo();
            var mo = Motion(root, br);
            SetArray(mo.FindProperty("legs"), Parts(br, "LegL1", "LegR1", "LegL2", "LegR2", "LegL3", "LegR3"));
            SetArray(mo.FindProperty("antennae"), Parts(br, "AntennaL", "AntennaR"));
            mo.FindProperty("stepsPerSecond").floatValue = 5f;
            mo.FindProperty("legSwingAngle").floatValue = 25f;
            mo.FindProperty("legLiftAngle").floatValue = 15f;
            mo.FindProperty("bodyBob").floatValue = 0.015f;
            mo.ApplyModifiedPropertiesWithoutUndo();
        });

        // 4) Spider: 8 scuttling legs
        Edit("Enemy_Spider.prefab", root =>
        {
            var visual = root.transform.Find("Visual");
            Clear(visual, "Halo");
            var br = Place(spider, visual, null).transform.Find("BodyRoot");
            var mo = Motion(root, br);
            SetArray(mo.FindProperty("legs"), Parts(br, "LegL1", "LegR1", "LegL2", "LegR2", "LegL3", "LegR3", "LegL4", "LegR4"));
            mo.FindProperty("stepsPerSecond").floatValue = 7f;
            mo.FindProperty("legSwingAngle").floatValue = 18f;
            mo.FindProperty("legLiftAngle").floatValue = 14f;
            mo.FindProperty("bodyBob").floatValue = 0.012f;
            mo.ApplyModifiedPropertiesWithoutUndo();
        });

        // 5) Dragonfly: 4 wings always buzzing (Visual bob stays in RollingObstacle)
        Edit("Enemy_Dragonfly.prefab", root =>
        {
            var visual = root.transform.Find("Visual");
            Clear(visual, "Halo");
            var br = Place(dragon, visual, null).transform.Find("BodyRoot");
            var mo = Motion(root, null);
            SetArray(mo.FindProperty("wings"), Parts(br, "WingFL", "WingFR", "WingBL", "WingBR"));
            mo.FindProperty("wingsAlways").boolValue = true;
            mo.FindProperty("wingFlapAngle").floatValue = 30f;
            mo.FindProperty("wingFlapFrequency").floatValue = 14f;
            mo.ApplyModifiedPropertiesWithoutUndo();
        });

        AssetDatabase.SaveAssets();
        Debug.Log("[CHI07] prefabs done");
    }

    static void Edit(string file, System.Action<GameObject> body)
    {
        string path = P + file;
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            body(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Debug.Log("[CHI07] saved " + file);
        }
        catch (System.Exception e) { Debug.LogError("[CHI07] " + file + ": " + e); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    /// Removes the old primitive model (every child of Visual except 'keep').
    static void Clear(Transform visual, string keep)
    {
        for (int i = visual.childCount - 1; i >= 0; i--)
        {
            var c = visual.GetChild(i);
            if (keep != null && c.name == keep) continue;
            Object.DestroyImmediate(c.gameObject);
        }
    }

    /// Model instance (kept as a model prefab instance: re-exported FBX updates it) + project materials by name.
    static GameObject Place(GameObject model, Transform parent, Dictionary<string, string> swap)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(model, parent);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.Euler(0f, ModelYaw, 0f);
        go.transform.localScale = Vector3.one;
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                string n = mats[i] != null ? mats[i].name : "";
                if (swap != null && swap.TryGetValue(n, out var to)) n = to;
                var pm = ProjectMat(n);
                if (pm != null) mats[i] = pm;
                else Debug.LogError("[CHI07] NOT FOUND material '" + n + "' on " + r.name);
            }
            r.sharedMaterials = mats;
        }
        return go;
    }

    static Material ProjectMat(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        foreach (var guid in AssetDatabase.FindAssets(name + " t:Material", new[] { "Assets/_Project/Materials" }))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(p) == name) return AssetDatabase.LoadAssetAtPath<Material>(p);
        }
        return null;
    }

    static void WireFly(FlyJam.FlyVisual fv, Transform model)
    {
        var br = model.Find("BodyRoot");
        var so = new SerializedObject(fv);
        so.FindProperty("bodyRoot").objectReferenceValue = br;
        SetArray(so.FindProperty("legs"), Parts(br, "LegL1", "LegR1", "LegL2", "LegR2", "LegL3", "LegR3"));
        so.FindProperty("wingL").objectReferenceValue = br.Find("WingL");
        so.FindProperty("wingR").objectReferenceValue = br.Find("WingR");
        so.FindProperty("antennaL").objectReferenceValue = br.Find("AntennaL");
        so.FindProperty("antennaR").objectReferenceValue = br.Find("AntennaR");
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static SerializedObject Motion(GameObject root, Transform bodyRoot)
    {
        var c = root.GetComponent<FlyJam.CreatureMotion>();
        if (c == null) c = root.AddComponent<FlyJam.CreatureMotion>();
        var so = new SerializedObject(c);
        so.FindProperty("bodyRoot").objectReferenceValue = bodyRoot;
        return so;
    }

    static Transform[] Parts(Transform br, params string[] names)
    {
        var a = new Transform[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            a[i] = br.Find(names[i]);
            if (a[i] == null) Debug.LogError("[CHI07] NOT FOUND part " + names[i]);
        }
        return a;
    }

    static void SetArray(SerializedProperty p, params Object[] items)
    {
        p.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
    }
}
```
기대 로그: `saved Fly.prefab`, `Mate.prefab`, `Enemy_Male.prefab`, `Enemy_Spider.prefab`, `Enemy_Dragonfly.prefab`, `prefabs done`. `NOT FOUND`가 하나라도 있으면 고치고 다시 실행(다시 실행해도 안전하다: 매번 지우고 새로 넣음).

### 6-4. 검증 (`Unity_RunCommand`)

프리팹과 씬의 `FlyVisual` 참조가 새 모델을 가리키는지 확인한다. 씬(또는 `SYS_Mate`)에 옛 부품을 가리키는 **오버라이드가 남아 깨졌으면** 그 컴포넌트 오버라이드만 되돌린다.

```csharp
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal class CommandScript : IRunCommand
{
    const string P = "Assets/_Project/Prefabs/";
    const string GameScene = "Assets/_Project/Scenes/Game.unity";

    public void Execute(ExecutionResult result)
    {
        int bad = 0;
        bad += CheckPrefab("Fly.prefab", "Mdl_FlyMale");
        bad += CheckPrefab("Mate.prefab", "Mdl_FlyFemale");
        bad += CheckEnemy("Enemy_Male.prefab", 6, 0, true);
        bad += CheckEnemy("Enemy_Spider.prefab", 8, 0, false);
        bad += CheckEnemy("Enemy_Dragonfly.prefab", 0, 4, false);

        // SYS_Mate (nested Mate) and Game.unity (player) may carry old overrides
        var sysMate = PrefabUtility.LoadPrefabContents(P + "SYS_Mate.prefab");
        bool fixedMate = false;
        foreach (var fv in sysMate.GetComponentsInChildren<FlyJam.FlyVisual>(true)) fixedMate |= FixIfBroken(fv, "SYS_Mate");
        if (fixedMate) PrefabUtility.SaveAsPrefabAsset(sysMate, P + "SYS_Mate.prefab");
        PrefabUtility.UnloadPrefabContents(sysMate);

        var scene = SceneManager.GetActiveScene();
        if (scene.path != GameScene) scene = EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);
        bool fixedScene = false;
        foreach (var go in scene.GetRootGameObjects())
            foreach (var fv in go.GetComponentsInChildren<FlyJam.FlyVisual>(true)) fixedScene |= FixIfBroken(fv, "Game.unity");
        if (fixedScene) EditorSceneManager.SaveScene(scene);

        Debug.Log(bad == 0 ? "[CHI07] check OK" : "[CHI07] check: " + bad + " problem(s)");
    }

    static bool Broken(FlyJam.FlyVisual fv)
    {
        var so = new SerializedObject(fv);
        if (so.FindProperty("bodyRoot").objectReferenceValue == null) return true;
        var legs = so.FindProperty("legs");
        if (legs.arraySize != 6) return true;
        for (int i = 0; i < 6; i++) if (legs.GetArrayElementAtIndex(i).objectReferenceValue == null) return true;
        foreach (var f in new[] { "wingL", "wingR", "antennaL", "antennaR" })
            if (so.FindProperty(f).objectReferenceValue == null) return true;
        return false;
    }

    static bool FixIfBroken(FlyJam.FlyVisual fv, string where)
    {
        if (!Broken(fv)) { Debug.Log($"[CHI07] {where}/{fv.name}: FlyVisual OK"); return false; }
        if (PrefabUtility.IsPartOfPrefabInstance(fv)) PrefabUtility.RevertObjectOverride(fv, InteractionMode.AutomatedAction);
        bool ok = !Broken(fv);
        if (ok) Debug.Log($"[CHI07] {where}/{fv.name}: reverted FlyVisual override -> OK");
        else Debug.LogError($"[CHI07] {where}/{fv.name}: FlyVisual still MISSING");
        return ok;
    }

    static int CheckPrefab(string file, string model)
    {
        var root = AssetDatabase.LoadAssetAtPath<GameObject>(P + file);
        var fv = root.GetComponentInChildren<FlyJam.FlyVisual>(true);
        var br = new SerializedObject(fv).FindProperty("bodyRoot").objectReferenceValue as Transform;
        bool ok = !Broken(fv) && br != null && br.parent != null && br.parent.name == model
                  && br.gameObject.activeSelf && br.parent.gameObject.activeSelf;
        if (!ok) Debug.LogError($"[CHI07] {file}: FlyVisual not on {model}");
        return ok ? 0 : 1;
    }

    static int CheckEnemy(string file, int legs, int wings, bool eyes)
    {
        var root = AssetDatabase.LoadAssetAtPath<GameObject>(P + file);
        int bad = 0;
        var mo = root.GetComponent<FlyJam.CreatureMotion>();
        if (mo == null) { Debug.LogError("[CHI07] " + file + ": no CreatureMotion"); return 1; }
        var so = new SerializedObject(mo);
        if (so.FindProperty("legs").arraySize != legs || so.FindProperty("wings").arraySize != wings) { Debug.LogError("[CHI07] " + file + ": wrong leg/wing count"); bad++; }
        if (root.transform.Find("Visual/Halo") == null) { Debug.LogError("[CHI07] " + file + ": Halo missing"); bad++; }
        if (eyes)
        {
            var e = new SerializedObject(root.GetComponent<FlyJam.NightGlowEyes>()).FindProperty("eyes");
            if (e.arraySize != 2 || e.GetArrayElementAtIndex(0).objectReferenceValue == null) { Debug.LogError("[CHI07] " + file + ": NightGlowEyes eyes"); bad++; }
        }
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            foreach (var m in r.sharedMaterials)
                if (m == null) { Debug.LogError("[CHI07] " + file + ": empty material on " + r.name); bad++; }
        return bad;
    }
}
```
기대: `check OK`. `reverted ... -> OK` 줄이 나오면 정상(옛 오버라이드를 치운 것)이고, 보고서에 적는다.

그다음 눈으로 확인(에이전트, 캡처 첨부): 각 프리팹을 프리팹 모드로 열어 Scene 뷰 캡처 5장(Fly, Mate, Enemy_Male, Enemy_Spider, Enemy_Dragonfly). 저장하지 말고 닫는다.

---

## 7. AGENTS.md 수정

`FlyGame/AGENTS.md`:
- **2.3절** 목록 끝에 추가:
  ```markdown
  - **생물 모델** (CHI/07): `Fly`·`Mate`·`Enemy_Male`·`Enemy_Spider`·`Enemy_Dragonfly`의 `Visual` 아래는 FBX 모델 인스턴스(`Art/Models/Mdl_*.fbx`, 언팩 안 함). 원본은 `FlyGame/ArtSource/build_creatures.py`(블렌더에서 실행 → `creatures.blend` + FBX 재생성 → 프리팹 자동 반영). 부품 이름·피벗이 곧 애니메이션 계약: 파리 = `BodyRoot`, `LegL1~LegR3`, `WingL/R`, `AntennaL/R`, `EyeL/R`(수컷 밤 눈), `Brows`(라이벌만 켬). 적 모션은 `Features/Hazards/CreatureMotion.cs`(다리·날개·더듬이·몸 출렁, 일시정지 시 정지). 머티리얼은 기존 `Materials/*.mat`을 이름으로 연결(Toon 셰이더는 버텍스 컬러를 안 씀).
  ```
- **3절 표** CHI 줄 끝에 추가: `07 로우폴리 생물 모델(블렌더 스크립트 → FBX), 적 모션(CreatureMotion).`

---

## 8. 보고 형식 (에이전트)

```
## CHI/07 보고
1. 1단계 출력 4줄 + look 이미지 (체크포인트 1에서 이미 보냈으면 "위와 같음")
2. 6-2 로그 4줄 (기대값과 비교) / 6-3 로그 / 6-4 로그
3. 만든/바꾼 파일 (3장 표와 대조)
4. 스펙과 다르게 한 것 / 판단 기록 (globalScale, bakeAxisConversion, ModelYaw를 바꿨으면 여기에)
5. 프리팹 캡처 5장
6. Console 최종 상태 (에러 0, 경고 목록)
```

---

## 9. 체크포인트 2 (사람, Play)

| # | 확인 | 성공이면 |
|---|---|---|
| 1 | 플레이어 | 새 모델, 걸을 때 다리 6개가 번갈아 움직이고 몸이 출렁, 더듬이 까딱. 대시·피버 때 날갯짓. 피격 시 깜빡임. 눈썹 없음 |
| 2 | 연못·피버 | 연못에서 몸이 살짝 잠기고, 피버 때 떠오름(전과 같음) |
| 3 | 암컷 | 리본·속눈썹·뾰족한 배. 걷기·멈춤(그루밍) 모션. 하트 마커 높이 자연스러움 |
| 4 | 라이벌 수컷 | 어두운 몸 + 화난 눈썹, 걸을 때 다리가 움직임. 밤에 눈이 빨갛게 맥동 |
| 5 | 거미 | 다리 8개가 빠르게 종종걸음. 맞고 멈춘 2초 동안은 다리 쉼 |
| 6 | 잠자리 | 날개 4장이 계속 떨리고, 몸 전체 위아래 흔들림 |
| 7 | 일시정지·구애·연타 배틀 중 | 적 다리·날개도 같이 멈춤 |
| 8 | 픽셀 셰이더(P키로 비교) | 모델이 덩어리로 깔끔하게 읽힘, 떨어진 조각 없음 |
| 9 | Console | 에러 0 |

---

## 10. 나중에 모양을 고치고 싶을 때

1. `FlyGame/ArtSource/build_creatures.py`에서 숫자만 고친다(유니티 좌표: x 오른쪽, y 위, z 앞. 부품 이름과 피벗 위치는 유지).
2. 블렌더에서 4장 2번 코드를 다시 실행 → FBX 4개가 덮어써진다.
3. 유니티로 돌아오면 자동 재임포트 → 프리팹의 모델 인스턴스가 바로 바뀐다(6-3을 다시 돌릴 필요 없음. 부품을 **추가/삭제**했을 때만 6-3 재실행).

---

## 11. 커밋 (사람)

```bash
cd ~/Developer/sgj2026-flygame
git add FlyGame/ArtSource/build_creatures.py FlyGame/ArtSource/creatures.blend \
        FlyGame/Assets/_Project/Art/Models FlyGame/Assets/_Project/Art/Models.meta \
        FlyGame/Assets/_Project/Scripts/Features/Hazards/CreatureMotion.cs FlyGame/Assets/_Project/Scripts/Features/Hazards/CreatureMotion.cs.meta \
        FlyGame/Assets/_Project/Prefabs FlyGame/AGENTS.md
git add FlyGame/Assets/_Project/Scenes/Game.unity 2>/dev/null   # 6-4가 씬을 고쳤을 때만 바뀌어 있음
git status      # creatures.blend1(블렌더 백업), 오디오·타이틀 이미지 같은 다른 작업 파일은 올라가지 않아야 함
git commit -m "Replace primitive creatures with low-poly Blender models and add enemy motions"
git push -u origin CHI/07-lowpoly-models
```
PR: `CHI/07-lowpoly-models` → `dev`.
