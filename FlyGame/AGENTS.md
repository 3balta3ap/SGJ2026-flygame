# AGENTS.md — Little Fly Planet (FlyGame)

이 문서는 `~/MCP`에서 이관된 Unity 프로토타입 프로젝트를 새 세션이 바로 이어서 작업할 수 있도록 정리한 인수인계 가이드다.

---

## 1. 프로젝트 환경 요약

- **프로젝트 경로**: `~/Developer/sgj2026-flygame/FlyGame` (`~/MCP`는 더 이상 수정하지 않음)
- **Unity 버전**: `6000.6.0f1` (URP, `Assets/Settings/PC_RPAsset.asset`, `supportsHDR = false`)
- **입력/색공간/직렬화**: New Input System (`activeInputHandler: 1`), `Linear` ColorSpace, `Force Text` (`m_SerializationMode: 2`)
- **Enter Play Mode Options**: `3` (**Disable Domain Reload + Disable Scene Reload** 활성화 → 모든 `static` 싱글톤/레지스트리에 `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` 리셋 필수)
- **언어 정책**: UI 텍스트 및 코드/셰이더 주석은 모두 **영문** 유지 (다국적 팀 공유)

---

## 2. 프로젝트 구조

### 2.1 씬 흐름 (Build Settings)
- `[0] Assets/_Project/Scenes/Title.unity` → `[1] Game.unity` → `[2] Result.unity` (`SPACE` Retry / `ESC` Title)

### 2.2 어셈블리(`.asmdef`) 및 의존 방향
`Assets/_Project/Scripts/` 아래 11개 어셈블리(런타임 10 + 에디터 전용 `Editor/FlyJam.Editor.asmdef`: 참조 `FlyJam.Core`)로 분리되어 컴파일 단계에서 의존 방향을 강제한다.

- **Core**: `Core/FlyJam.Core.asmdef` → 참조: `Unity.InputSystem`, `UnityEngine.UI`
  - 하위 폴더: `Events/`, `Session/`, `World/`, `World/Obstacles/`, `Player/`, `Camera/`, `Render/`
  - **코어는 기능을 모른다.**
- **Features** (8개): `Features/{Backpedal, Courtship, Dodge, Fx, Hazards, Mate, Sky, Village}/FlyJam.<Name>.asmdef`
  - 기본 참조: `FlyJam.Core` (`Hazards`·`Courtship`은 `+ Unity.TextMeshPro`, `Mate`는 `+ UnityEngine.UI`)
  - **기능끼리 서로를 모른다.** 유일한 예외: `FlyJam.Courtship` → `FlyJam.Mate`
- **UI**: `UI/FlyJam.UI.asmdef` → 참조: `FlyJam.Core`, `Unity.TextMeshPro`

**고정된 코어 계약** (임의 변경 금지): `FlyState`, `IMoveInput`, `IObstacle`/`IMovingObstacle`, `GameEvents` 코어 파일, `SessionResult` 키-값 API, 알파 채널 층 규칙(하늘 `0` / 땅 `0.5` / 물체 `1.0`, 안개 `-0.2`), 셰이더 전역 변수(`_Fly*`).

### 2.3 `Game.unity` 계층 & `SYS_*` 프리팹 (9개)
`Game.unity`는 `[Core]` + 9개 기능 프리팹(`Assets/_Project/Prefabs/SYS_*.prefab`)으로 구성된다.

- **`[Core]`** (씬 직접 배치): `GameContext` (`ExecutionOrder -1000`), `GameSession`, `Planet`, `Fly` (`Fly.prefab`), `Main Camera` (`PlanetFollowCamera`, 자식 `Directional Light`), `ScreenCamera`, `PixelCanvas` (`sortingOrder -100`), `PixelRenderer` (`PixelRenderer`, `PixelPalette`, `ToonLighting`), `Toonify` (`ToonMaterialSwapper`: `Planet(0.5)`, `Fly(1)`).
- **`SYS_Village.prefab`**: `Village` (`VillageGenerator`, `Generated`는 비어 있음, `keepClear` 비움), `Pond` (`SurfacePlacer`, `SurfaceDisc`, `PlanetProp`, `ToonifyOnSpawn`), `BigTree` (`tree_oak.fbx` 중첩).
- **`SYS_Sky.prefab`**: `DayNight` (`DayNightCycle`, `M_Sky`). 끄면 `ToonLighting` 기본값(맑은 낮)으로 동작.
- **`SYS_Dodge.prefab`**: `DodgeAbility` (`ExecutionOrder -10`).
- **`SYS_Backpedal.prefab`**: `BackpedalFacing`.
- **`SYS_Hazards.prefab`**: `ObstacleSpawner`, `NearMissDirector`, `ReactionTracker`, 자체 `Canvas` (`sortingOrder 10`: `SpacePrompt`, `PerfectText`).
- **`SYS_Mate.prefab`**: 중첩 `Mate.prefab` (`Fly.prefab` Variant, `MateAI`/`MateGoal`/`MotionBlindVisibility`/`KeepClearZone`/`NightHalo`/`ToonifyOnSpawn`), 자체 `Canvas` (`sortingOrder 11`: `MateHint` + `MateFinderHint`).
- **`SYS_Courtship.prefab`**: `CourtshipFlow`, `CourtshipQTE`, 자체 `Canvas` (`sortingOrder 12`: `CourtshipPanel`).
- **`SYS_Fx.prefab`**: `FxDirector`, `AmbientParticles`, 파티클 7종(`FX_DodgeTrail`, `FX_HitBurst`, `FX_HeartBurst`, `FX_MatePop`, `FX_Perfect`, `FX_Pollen`, `FX_Fireflies`).
- **`SYS_UI.prefab`**: HUD `Canvas` (`sortingOrder 0`), `GameHud`, `TimerText`.

**원칙**: `SYS_*` 프리팹 내부에는 씬 객체 직접 참조가 **0개**다. 런타임에 `GameContext.Current`, `MateAI.Current`, `ToonLighting.Current`로 자동 해석한다. 어떤 `SYS_*`든 비활성화해도 Console 에러 0으로 플레이된다.

---

## 3. 구현 완료된 Phase 요약 (스펙 v1 ~ v8 + 후속/v9)

| 스펙 / Phase | 핵심 구현 내용 |
|---|---|
| **v1 (Phase 0~3)** | 작은 구체 행성 이동(`SphereWalker`, 물리 미사용 거리 계산), 3인칭 추적 카메라(`PlanetFollowCamera`), 씬 흐름(`Title→Game→Result`), 프리미티브 파리 모델 및 프로시저럴 보행/그루밍 애니메이션(`FlyVisual`). |
| **v2 (Phase 4~10)** | Kenney Nature Kit 마을 생성(`VillageGenerator`), 표면 충돌(`SurfaceCollision`), 짝 AI + 운동맹(`MotionBlindVisibility`) + 정지 시 줌아웃, 굴러오는 도토리(`RollingObstacle`) + 근접 슬로모션(`NearMissDirector`, `PERFECT`) + 반응속도(`ReactionTracker`), 4박자 구애 QTE(`CourtshipQTE`) + 결과 통계/초파리 상식(`FlyFacts`), 2등신 파리 리디자인, 픽셀 후처리, 45초 낮/밤 사이클(`DayNightCycle`) + `NightHalo`, 픽셀 파티클(`FxDirector`, `AmbientParticles`). |
| **v3 (Phase 11)** | 고해상도 구 메시(`PlanetMesh`), 곡면 연못(`SurfaceDisc`), 모델 실제 폭 기반 자동 footprint·곡률 묻기(`CurvatureSink`), 짝 리시(`leashAngle`) + 정지 시 하트 방향 힌트(`MateFinderHint`), 화면 내 해/달 궤적. |
| **v4 (Phase 12~13)** | 탱크식 조작(`W/S` 전후진, `A/D` 제자리 회전), FOV 40°, 레퍼런스 이식 픽셀 파이프라인(`FlyJam/Toon`, `FlyJam/PixelArtPost`, `FlyJam/Particle`, 3층 알파 `0 / 0.5 / 1`). |
| **v5 (Phase 14)** | OKLab + 색조 벌점 표면 팔레트 매칭, 표면용(24색, `#d8f0a0` 추가) vs 하늘 전용(`#9fd0f0`) 팔레트 분리, 밝기(낮 `0.85` / 밤 `0.35`)·안개(`14/30/0.35`) 하향으로 잎 하늘색 깨짐 수정. |
| **v6 (Phase 15)** | 하늘은 팔레트·디더링 없이 원색 그라데이션 출력, 안개 세기를 알파에 패킹(`layer - 0.2 * fog`)해 팔레트 확정 뒤 후처리에서 부드럽게 혼합, 꽃가루(초당 5, 0.12~0.18)·반딧불(초당 6, 0.1~0.14) 조정. |
| **v7 (Phase 16)** | `BackpedalFacing`으로 뒷걸음 시 `Fly/Visual`만 180° 회전(`backwardSpeedRatio = 0.7`)해 카메라 회전 없이 얼굴 노출. |
| **v8 (Phase 17 A~G)** | 죽은 코드 삭제, `GameEvents` partial 5파일 분리, `GameContext`·`ToonLighting`·`KeepClearZone`·`IMovingObstacle` 도입, `GameSession` 슬림화 + `CourtshipFlow`·`DodgeAbility` 분리, `SessionResult` 키-값화, 폴더 재배치 + `.asmdef` 10개, `SYS_*` 프리팹 9개 분리(제거 테스트 통과). |
| **후속 / v9 작업** | 프로젝트 50개 파일 한글 주석/헤더/툴팁/로그 전체 영문 변환, `ThirdParty/Silver/Silver.ttf`로 `Assets/_Project/Art/Fonts/Silver SDF.asset` 생성(`Dynamic`, `SDFAA`, `1024×1024`), `TMP Settings` 기본 폰트 교체, `Title`·`Result`·`SYS_UI`·`SYS_Hazards`·`SYS_Courtship` 전체 TMP 텍스트 `Silver SDF` 적용(크기 유지, `Bold` 해제), `Title` 우하단 크레딧(`Font: Silver by Poppy Works (CC BY 4.0)`) 추가. |

---

## 4. 작업 규칙 (스펙 v1 1장 + 팀 규칙)

1. **Phase 순서 준수**: 각 Phase/단계 끝의 체크포인트에서 멈추고, 변경 내역과 플레이 확인 항목을 보고한 뒤 사람 확인을 기다린다.
2. **스펙 코드 원문 우선**: 스펙 코드는 그대로 사용한다. 컴파일 오류·구조적 문제로 고쳐야 하면 **무엇을 왜 고쳤는지** 반드시 보고한다.
3. **범위 준수**: 요청받지 않은 기능·에셋·패키지를 추가하지 않는다. 판단이 애매하면 추측하지 말고 질문한다.
4. **종료 검증**: 작업 완료 시 **Console 에러 0개** 확인 및 씬·프리팹 저장. (git 커밋은 명시 요청 시에만)
5. **Play 확인은 사람 몫**: 체크포인트 보고에 "무엇을 눌러서 무엇이 보이면 성공인지"를 적는다.
6. **코딩/협업 규약**:
   - 모든 스크립트는 `namespace FlyJam` 안에 둔다. `FindObjectOfType` 류 금지(`GameContext`/인스펙터 참조 사용).
   - UI 텍스트와 코드/셰이더 주석은 영문으로 작성한다.
   - 파일 이동은 반드시 Unity 내부(`AssetDatabase.MoveAsset`)로 실행해 `.meta` GUID 연결을 유지한다.
   - 새 이벤트는 `Core/Events/GameEvents.<Module>.cs` 신규 partial 파일로 추가하고, 기능 수정은 자기 `SYS_*` 프리팹 안에서 처리한다.

---

## 5. 스펙과 다르게 고친 부분과 이유

- **Disable Domain Reload 정적 초기화 추가**: `EnterPlayModeOptions = 3` 환경에서 재플레이 시 파괴된 참조가 남는 문제를 막기 위해 `PlanetProp`, `ObstacleRegistry`, `RollingObstacle`, `GameContext`, `ToonLighting`, `KeepClearZone`, `DayNightCycle`, `MateAI`에 `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` 리셋 메서드를 추가했다.
- **`PlanetProp.Configure()` & `ObstacleHitDetector` 방어 코드**: Edit 모드에서 `VillageGenerator.Generate()` 실행 시 정적 `ObstacleRegistry`에 파괴될 오브젝트가 등록되던 문제를 막기 위해 `PlanetProp.Configure()`에 `!Application.isPlaying || !isActiveAndEnabled` 가드를 넣고, `ObstacleHitDetector.Update()`에 Unity `null` 체크를 추가했다.
- **`SurfaceDisc.cs` & `SurfacePlacer.cs` `GameContext` 폴백 (17-G)**: `Pond`와 `BigTree`를 `SYS_Village.prefab`으로 옮기며 인스펙터의 씬 `Planet` 참조를 비웠기 때문에, 런타임에 `planet == null`이면 `GameContext.Current.Planet`에서 채우도록 `Build()`/`Place()`에 각 1줄 추가했다.
- **`BackpedalFacing.OnDisable()` null 가드 (17-G 후속)**: `SYS_Backpedal`이 `[Core]` 아래 루트 프리팹으로 분리되면서 씬 종료/전환 시 `Fly/Visual`이 먼저 파괴되어 `MissingReferenceException`이 발생 → `if (flyVisual != null)` / `if (visualRoot != null)` 가드 추가.
- **`SYS_Village.prefab`의 `Generated` 컨테이너 비움**: Edit 모드 생성물 120개를 프리팹에 저장하면 YAML이 비대해지고 Edit 모드 `Generate()`에서 `DestroyImmediate` 예외가 발생하므로 비워 두었다(런타임 `Awake()`에서 생성 + `ToonMaterials.Apply`).

---

## 6. Unity MCP 주의점 & 자주 난 오류/해결법

- **`Unity_RunCommand` 제약**:
  - 반드시 `internal class CommandScript : IRunCommand` + `public void Execute(ExecutionResult result)` 형태 사용.
  - `System.Reflection`은 보안 정책으로 차단된다.
  - `AssetDatabase.DeleteAsset`, `EditorApplication.isPlaying = true`, 일부 `EditorApplication.ExecuteMenuItem`(예: `Window/General/Game`)은 `UNEXPECTED_ERROR: User interactions are not supported for MCP tool calls`로 차단된다. 프리팹 갱신은 `SaveAsPrefabAsset` 덮어쓰기, 파일 삭제는 씬 참조 제거 후 외부 삭제를 사용한다.
  - 단, `Assets/Create/TextMeshPro/Font Asset/SDF` 메뉴 실행은 동작했다.
- **도메인 리로드 공백**: `.cs`/`.asmdef` 생성·수정 직후 약 5~10초간 `Unity not detected (no fresh discovery files found)`가 뜰 수 있다. 연속 재시도하지 말고 `schedule`로 5~6초 대기 후 재호출한다.
- **`Image` 네임스페이스 충돌**: `Unity_RunCommand` 안에서 `Image`가 모호하므로 항상 `UnityEngine.UI.Image`, `UnityEngine.UI.RawImage`로 풀네임 사용한다.
- **`SaveAsPrefabAssetAndConnect` 반환값 주의**: 이 API는 씬 인스턴스가 아니라 **프리팹 에셋**을 반환한다. 씬 전용 오버라이드는 원본 씬 `GameObject`에 적용하고 `PrefabUtility.RecordPrefabInstancePropertyModifications`를 호출한다.
- **Unity 6000.6 API 변경**: `FindFirstObjectByType` 및 `FindObjectsByType(..., FindObjectsSortMode)`는 deprecated 경고 → `FindAnyObjectByType<T>()` / `FindObjectsByType<T>(FindObjectsInactive)` 사용.
- **UI 스크린샷 캡처 주의**: `Unity_Camera_Capture` / `Camera.Render()`는 `Screen Space - Overlay` 캔버스를 렌더링하지 않는다. 오프스크린 프리뷰가 필요하면 메모리상에서만 임시로 캔버스를 `ScreenSpaceCamera`로 메인 카메라에 붙여 캡처한 뒤 **저장하지 않고 씬을 재오픈**한다. (URP에서 별도 Base Camera를 `ClearFlags.Depth`로 띄우면 컬러가 단색으로 덮이므로 메인 카메라를 직접 사용)
- **TMP Essentials**: 미임포트 시 `TMP_Settings.defaultFontAsset` NRE 발생(현재는 임포트 및 `Silver SDF` 연결 완료).

---

## 7. 아직 남은 문제 / 확인 필요 항목

1. **URP GPU Resident Drawer × `PlanetMesh` 콘솔 에러 (미해결)**:
   - 증상: `Game.unity` 로드 또는 `PlanetMesh.Build()` 호출 시 `BatchDrawCommand ... MaterialID: ("M_Planet"), MeshID: ("<null>")` 에러 발생.
   - 원인: `Assets/Settings/PC_RPAsset.asset`의 `m_GPUResidentDrawerMode: 1`과 `PlanetMesh.Build()`의 런타임 `new Mesh()`(및 `Game.unity` 내장 메시 `fileID: 766514539`)가 충돌.
   - 권장 조치: `PC_RPAsset.asset`에서 **GPU Resident Drawer → Disabled** (`Mobile_RPAsset.asset`과 동일)로 끄거나, `PlanetHiRes`를 정식 `.asset` 메시로 저장해 연결.
2. **Smart Merge 로컬 Git 설정**:
   - `.gitattributes`에 `merge=unityyamlmerge` 규칙은 이미 들어가 있으나, 실제 동작하려면 각 팀원의 로컬 `.git/config`에 `UnityYAMLMerge` 드라이버 경로 등록이 필요하다.
3. **팀 저장소 정리 확인**:
   - 루트에 임시 파일로 보이는 `dummy.png`가 존재하며, v1 시절 미사용 프리팹(`Fruit.prefab`, `Thorn.prefab`) 유지 여부를 팀과 결정할 것.
   - 팀 저장소(`FlyGame`)에서 `Silver SDF` 폰트 및 9개 `SYS_*` 프리팹 최종 Play 테스트 확인 필요.

---

## 8. 본 개발 협업 규칙 (dev 이후)

- **브랜치**: `dev`에서 따고 `dev`로 머지한다. 이름은 `CHI/NN-기능-이름` (예: `CHI/03-player-health`). 계획은 `specs/CHI-00-plan.md`.
- **스펙**: 브랜치 하나 = 스펙 하나(`specs/CHI-NN-*.md`). 스펙 범위 밖의 파일은 건드리지 않는다.
- **git**: 에이전트는 branch/add/commit/push를 실행하지 않는다. 사람이 스펙의 영어 커밋 메시지로 커밋한다.
- **수치(밸런스)**: 기획자가 바꿀 값은 코드에 박지 않는다.
  1. 기능 폴더에 `BalanceTable`을 상속한 클래스를 만든다 (`SheetName` = 시트 탭 이름, 필드 이름 = 시트 key, `[Tooltip]` = 시트 note).
  2. 표 형태 값은 배열/List 필드에 `[SheetRows("탭이름")]`.
  3. 에셋은 `Assets/_Project/Data/Balance/<Feature>Balance.asset`. 기능 컴포넌트가 `[SerializeField]`로 참조한다(`SYS_*` 프리팹 안에서).
  4. 사람이 `FlyJam → Balance Sync`의 `Copy (TSV)`로 시트에 탭을 만들고 `Pull`로 받아 온다.
- **Balance Sync 툴**: `Scripts/Editor/Balance/`. 시트 전체를 xlsx로 받아(`/export?format=xlsx`) 탭 이름과 필드 이름으로 맞춘다. 시트에 없는 키는 현재 값을 유지한다. Vector2 필드는 시트에서 `이름.x`/`이름.y` 두 줄이다(구글 시트가 `1, 2`를 날짜로 바꾸기 때문).
