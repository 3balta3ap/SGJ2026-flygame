# CHI/05-planner-spec-all: 기획서(Excel·PDF) M1~M4 전체 구현 — 사운드 제외

| 항목 | 값 |
|---|---|
| 브랜치 | `CHI/05-planner-spec-all` (`dev`에서 생성. CHI/01~04가 머지된 `dev`) |
| 기준 문서 | 기획 Excel `FlyGame 仕様書`(시트 01~06, 이 스펙 3장에 ID별로 옮김), 기획 PDF. **Excel이 우선** |
| 권장 커밋 메시지 | `Implement planner spec M1-M4: auto-run, HP, apples, enemies, dodge, courtship, mash battle, menus` |
| 범위 | 기획서 S01~S07, P01~P06, C01~C08, E01~E05, A01~A06 (A07 사운드는 이벤트 목록만 제공) |
| 건드리지 않는 것 | `CourtshipFlow.cs`, `PlanetFollowCamera.cs`, `ObstacleHitDetector.cs`, `Loc.cs`, `TextTable.cs`, `LocalizedText.cs`, `FlyFacts.cs`, `SessionResult.cs`, Balance Sync 툴(`Scripts/Editor/Balance/*`), Render·Village·Sky(1줄 제외) 코드, 셰이더 |

> **에이전트 작업 규칙** (AGENTS.md 4장·6장·8장 그대로 적용)
> - 스펙의 코드는 **원문 그대로** 쓴다. 컴파일 오류 등으로 고쳤다면 **무엇을 왜** 고쳤는지 보고한다.
> - **git 명령은 하나도 실행하지 않는다** (branch, add, commit, stash, checkout 포함). 커밋은 사람이 한다.
> - 스펙에 없는 파일·패키지·기능을 추가하지 않는다. `FindObjectOfType` 류 금지.
> - 끝나면 **Console 에러 0개**, 모든 씬·프리팹 저장.
> - 이 스펙은 **한 번에 끝까지** 진행한다. 단계 사이에 사람 확인을 기다리지 않는다. 대신 각 단계 끝의 **자가 확인**을 통과해야 다음 단계로 간다.
> - 어떤 항목이 막히면 추측으로 메우지 말고, 그 항목을 보고서의 "미완료"에 적고 **서로 상관없는 다음 항목**은 계속한다.
> - Balance Sync의 **Pull 버튼은 누르지 않는다** (시트가 아직 옛 형식이라 새 값을 덮어쓴다. 11장 참고).

---

## 0. 이 문서 읽는 법

| 장 | 내용 | 누가 |
|---|---|---|
| 1 | 확정된 기획 결정 | 모두 |
| 2 | 구현 전략(설계) — **왜 이렇게 짰는지** | 모두. 코드가 이상해 보이면 먼저 여기 |
| 3 | 기획서 ID ↔ 구현 위치 표 | 모두 |
| 4 | 파일 변경 목록 | 에이전트 |
| 5 | **단계 1: 코드** (전체 파일) | 에이전트 |
| 6 | **단계 2: 에셋** (밸런스 에셋, 텍스트 표) | 에이전트 |
| 7 | **단계 3: 프리팹·씬** | 에이전트 |
| 8 | **단계 4: 자가 검증** | 에이전트 |
| 9 | AGENTS.md 수정 | 에이전트 |
| 10 | 보고 형식 | 에이전트 |
| 11 | 시트 작업 | 사람 |
| 12 | 체크포인트 (기획서 ID별) | 사람 |
| 13 | 사운드 담당용 이벤트 표 | 사운드 담당 |
| 14 | 튜닝 값 전체 | 기획 |
| 15 | 커밋 | 사람 |

용어
- **세션 일시정지**(`GameSession.Pause(by)`): 구애·연타 배틀·일시정지 메뉴 동안 시계, 체력 감소, 적, 스포너, 암컷, 사과 판정을 멈추는 것. 시간 배율(`Time.timeScale`)과는 별개다.
- **시간 배율**(`Time.timeScale`): 이제 `TimeControl`만 쓴다. 일시정지 메뉴 = 0, 경고 중 슬로모션 = 0.5, 평소 = 1.
- **액션 버튼**: 패드 오른쪽 면 버튼(Switch Ⓐ / Xbox Ⓑ / PS ○). 키보드는 Space(또는 Enter).
- **비율 데미지/회복**: 모든 체력 증감은 "최대 체력의 몇 %"로 적는다(최대 10이면 0.2 = 2).

---

## 1. 확정된 기획 결정

기획 Excel·PDF 리뷰 후 확정된 것. 이 스펙은 이것만 구현한다.

| 분류 | 결정 |
|---|---|
| 성장 | **레벨·경험치 없음**. 구애 난이도는 시트 값(`Courtship` 탭)으로만 조절 |
| 이동 | **오토런**: 입력이 없어도 앞으로 간다. 스틱 좌우 = 핸들(선회 속도). 최대 선회 75°/s → 큰 호를 그리며 돈다. 중립 = 직진. 뒷걸음(Backpedal) **삭제** |
| 속도 | 최대 속도 **6**(기획 예시는 10, 행성 반지름 12에 맞춰 6으로 결정). 실제 속도 = 6 × 현재 체력/최대 체력 (최저 30%) |
| 입력 | **게임패드 우선**, 키보드는 개발용 대체. 버튼 표기는 마지막에 쓴 기기에 맞춰 바뀐다(Switch/Xbox/PS/키보드) |
| 액션 버튼 | 오른쪽 면 버튼(Switch Ⓐ / Xbox Ⓑ / PS ○) / Space. 회피·연타·메뉴 결정에 공용 |
| 일시정지 | 패드 Start(+) / 키보드 Esc. 메뉴: つづける / タイトルに戻る |
| 체력 | 최대 10, 시작 5, 초당 0.1 감소. 적 접촉 = 최대의 20%(도토리 10%). 사과 = +20% |
| 사과 | 고정 지점 8곳, 먹으면 사라지고 그 지점에서 직선 20 이상 멀어지면 같은 곳에 다시 1개. 첫 사과는 시작 지점 바로 앞 |
| 암컷 | 행성을 돌아다니고, 플레이어가 5 안에 오면 도망, 약 8 밖이면 다시 배회. 속도 = 플레이어 최대 속도 × 0.5. **운동맹(motion-blind) 끔**. 하트 방향 안내 **삭제** → 화면 안에 보이면 머리 위 발견 마크 |
| 구애 | **버튼 순서 입력**(기본 4개, ABXY 4종). 틀리면 즉시 실패 → 암컷이 2초 도망 → 떨어졌다가 다시 닿으면 재도전. 시간 제한 기본 없음(시트로 켤 수 있음) |
| 적 | 잠자리 2·거미 2·수컷 1·도토리 1이 **행성을 계속 돈다**(사라지지 않는 고정 개체 수). 속도 = 플레이어 최대 속도 × 0.7 |
| 수컷 | 가까우면 쫓아온다. 닿으면 **연타 배틀**: 6초 안에 25번 → 성공이면 탐색 복귀, 실패면 체력 −1(10%) 후 탐색 복귀 |
| 도토리 | 적과 같은 취급(경고 대상), 접촉 10% |
| 경고·회피 | 다가오는 적이 있으면 화면 네 변에 빨간 테두리 + 위쪽 가운데 "Ⓐボタンで回避！" + 슬로모션. **회피는 경고 중에만** 된다. 회피 성공 = 그 적이 플레이어에게 닿지 않고 지나감 → "回避!" |
| 결과 | **Result 씬 삭제**. 게임 씬 안의 결과 패널(もういちど / タイトルに戻る). 통계 줄(최고 반응 속도·연구 데이터·회피 횟수)과 FLY FACT **유지** |
| HUD | 왼쪽 위 체력 숫자 + 증감 팝업(+2/−2), 저체력 "りんごを探そう！", 시작 시 아래 가운데 조작 안내, 암컷 발견 마크 |
| 유지 | 낮밤 순환(하루 반복, 라운드를 끝내지 않음), 픽셀 아트, 슬로모션, EN/JA 전환 |
| 사운드 | 이번 범위 밖(다른 팀원). 13장에 이벤트 목록만 |
| 아이템 | 타키자와 씨의 아이템 브랜치는 **쓰지 않는다**. 이 스펙의 `Features/Items`가 사과를 구현 |

---

## 2. 구현 전략

### 2-1. 큰 그림

```
                 ┌────────────── Core (FlyJam.Core) ───────────────────────────────┐
 패드/키보드 ──▶ │ InputCompat ──▶ KeyboardMoveInput ──▶ SphereWalker (AutoRun)   │
                 │   (기기 판별, 버튼 표기)                 ▲ SpeedMultiplier      │
                 │ TimeControl (timeScale 유일한 주인)       │                      │
                 │ GameSession (시계·세션 일시정지·종료) ── RoundEnded ──┐          │
                 │ GameEvents (partial, 기능 사이의 유일한 통로)          │          │
                 └──────────────────────────────────────────────────────│──────────┘
        이벤트만 주고받는다 (기능끼리 서로 모름)                         ▼
 Health ── HP→속도, PlayerDied ─────────────────────────────▶ GameSession.End ─▶ UI.ResultPanel
 Items ─── HealRequested, AppleEaten
 Hazards ─ 적 순환, WarningChanged, DodgeSucceeded, RivalEncounter(수컷 접촉)
 Dodge ─── WarningChanged가 있을 때만 PlayerDodged
 Rival ─── RivalEncounter → 연타 배틀(세션 일시정지) → DamageRequested(실패 시)
 Mate ──── 암컷 배회/도망, MateTouched
 Courtship ─ MateTouched → 버튼 순서(세션 일시정지) → 성공 시 GameSession.End(true,"Love")
 Fx ────── 이벤트 → 파티클     Sky ── GameSession.DayProgress → 하늘색
 UI ────── MenuNavigator, TitleScreen, PauseMenu, ResultPanel, GameHud
```

원칙(지금까지와 같음)
1. **기능끼리는 `GameEvents`로만 이야기한다.** 예외는 기존의 `Courtship → Mate` 하나뿐.
2. **기획자가 바꿀 숫자는 전부 `BalanceTable` 에셋**(→ 구글 시트 탭). 코드 안 숫자는 시트가 비었을 때의 기본값일 뿐이다.
3. **`SYS_*` 프리팹 안에는 씬 참조가 0개.** 런타임에 `GameContext.Current`, `MateAI.Current`로 찾는다.
4. 새 기능은 새 폴더 + 새 asmdef + 새 `SYS_*` 프리팹(`Items`, `Rival`, `Menu`). 끄면(프리팹 비활성) 그 기능만 빠지고 에러는 없다.

### 2-2. 시간과 일시정지 — 충돌을 막는 규칙

지금까지는 `GameSession`, `NearMissDirector`, `TitleScreen`, `ResultScreen`이 각자 `Time.timeScale`을 썼다. 일시정지 메뉴와 슬로모션이 겹치면 서로 덮어쓴다. 그래서:

- `Core/Session/TimeControl.cs`(정적 클래스)만 `Time.timeScale`을 쓴다.
  - `SetPaused(bool)`: 일시정지 메뉴. 켜져 있으면 무조건 0.
  - `SetSlow(owner, scale)`: 슬로모션 요청. 여러 개면 가장 느린 값. 1을 넘기면 요청 해제.
  - `ResetAll()`: 씬 전환 때마다(`SceneFlow`), 그리고 `GameSession.Start`, `TitleScreen.Start`.
  - `InputBlocked`: 일시정지 중 **그리고 일시정지를 닫은 바로 그 프레임**에 true. 메뉴를 닫은 버튼(액션 버튼)이 회피·연타·구애 입력으로 새지 않게 한다.
- 세션 일시정지(`GameSession.Pause/Resume`, 소유자 집합)는 "게임 규칙상 멈춤"이다. 일시정지 메뉴는 **둘 다** 건다(시간 0 + 세션 일시정지). 구애·연타는 **세션 일시정지만** 건다(UI 애니메이션과 입력 타이머는 계속 돌아야 하므로).
- 구애·연타의 남은 시간은 `Time.deltaTime`으로 줄인다 → 일시정지 메뉴(시간 0) 동안 자동으로 멈춘다(S07 "イベントの残り時間も保持").
- 정적 상태는 `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]`로 초기화한다(Domain Reload 꺼짐).

### 2-3. 입력 — `InputCompat` 하나로

- 기존 `InputCompat`(정적)을 확장한다. 모든 게임 코드는 Input System을 직접 읽지 않고 이것만 부른다.
- `Steer()` : 왼쪽 스틱 x / 십자키 / A·D / ←→ → −1~1.
- `ActionPressed()` : 패드 `buttonEast`(Switch Ⓐ, Xbox Ⓑ, PS ○) / Space / Enter. **이번 프레임에 새로 눌렸을 때만** true(`wasPressedThisFrame`) → 누르고 있어도 1번(P05).
- `PausePressed()` : 패드 Start(+) / Esc.
- `FaceButtonPressed(out FaceButton)` : 구애용 4버튼. `FaceButton`은 **물리 위치**(East/South/North/West). 키보드는 →/D=East, ↓/S=South, ↑/W=North, ←/A=West.
- `MenuMove()` : 메뉴 커서. 스틱은 0.5를 넘는 순간 1번만(프레임 캐시).
- `Layout` : 마지막으로 입력한 기기. 패드 이름으로 Switch/PlayStation을 판별하고 나머지는 Xbox. 키보드 아무 키 = Keyboard.
- `Label(FaceButton)` : 기기별 이름. Switch A/B/X/Y, Xbox B/A/Y/X, PS ○/×/△/□, 키보드 →/↓/↑/←. `ActionLabel` : Ⓐ / Ⓑ / ○ / SPACE.
- 옛 함수(`Move`, `ConfirmPressed`, `BackPressed`, `DodgePressed`, `TogglePixelPressed`)는 호환용으로 남긴다(`PixelRenderer`가 `TogglePixelPressed`를 씀).
- 화면 글자는 전부 이 표기에서 만든다 → 기기를 바꾸면 "Ⓐボタンで回避！"가 "○ボタンで回避！"로 바로 바뀐다(P06 확인 조건).

### 2-4. 이동 — 오토런과 체력-속도 고리

- `SphereWalker`에 `MoveMode.AutoRun` 추가(enum 끝에 추가 → 기존 값 Tank=0, Steer=1 유지. 암컷 프리팹의 오버라이드가 안 깨진다).
- AutoRun: 매 프레임 `앞으로 MoveSpeed × SpeedMultiplier`, 스틱 x만큼 `TurnRate`로 회전. 회전 반경 = 속도 / 각속도 = 6 / (75° = 1.31 rad) ≈ **4.6**(체력 절반이면 2.3). 행성 반지름 12에서 "급하게 꺾이지 않는 큰 호"(P01).
- 입력이 꺼지면(구애·연타·종료) 그 자리에 멈춘다.
- `MoveSpeed` = `PlayerMoveBalance.maxSpeed`(시트). 적·암컷은 이 값을 "플레이어 최대 속도"로 쓴다 → 플레이어가 느려져도 적 속도는 그대로.
- `PlayerHealth`가 매 프레임 `SpeedMultiplier = max(minSpeedRatio, HP/최대HP)`를 넣는다(S03).
- **설계된 순환**: 시작 HP 5 → 속도 3 = 암컷 속도 3 → 그대로는 못 따라잡는다 → 사과를 먹어 빨라져야(HP 7이면 4.2) 암컷을 잡는다. 기획 01 "赤リンゴで体力と速さを保ち、…メスに追いつく" 그대로다. 너무 어려우면 시트 `Mate.speedRatio`나 `Health.startHp`로 조절.

### 2-5. 적 → 경고 → 회피 → 결과

1. `ObstacleSpawner`: `firstDelay` 후 `spawnInterval`마다 **부족분이 가장 큰 종류**를 1마리씩 만든다. 각 종류가 `count`에 도달하면 더 만들지 않는다. 플레이어에게서 50~170° 떨어진 곳, 진행 방향은 랜덤. 이동 거리 제한 없음 → 행성을 영원히 돈다(대원 위를 직진).
2. `RollingObstacle`(이름 유지, 범용 적): `HazardTypes` 한 줄의 값(속도 비율, 데미지 비율, 경고 여부, 라이벌 여부, 추적, 정지 시간)을 받는다. `IDamageSource.DamageRatio`로 자기 데미지를 알려 준다. 세션 일시정지 중엔 멈춘다(E04).
3. `NearMissDirector`(이름 유지, 경고 담당): 매 프레임 **경고 대상** = `warn`이 켜진 적 중 `warnRadius` 안에서 **플레이어 쪽으로 오고 있는**(진행 방향·플레이어 방향 내적 > `warnApproachDot`) 것 중 가장 가까운 것(E01). 바뀌면 `WarningChanged(target)`을 1번 보내고, 테두리·안내 문구·슬로모션을 켜고 끈다. 세션 일시정지 중엔 대상 없음 → 구애·연타 중에는 경고가 사라진다(A05).
4. `DodgeAbility`: `WarningChanged`로 받은 대상이 있을 때만 액션 버튼에 반응(P04). 방향: 스틱 x를 기울인 쪽(파리 기준 좌우). 중립이면 `Dodge.neutralMode`:
   - **0(기본, 권장)**: 적의 진행선에서 **옆으로 비켜서기**.
   - 1(기획서 문구 그대로): 적이 **가는 방향**으로 이동.
   - 왜 기본이 0인가: 정면에서 오는 적의 진행 방향으로 피하면 **같은 직선 위에서 앞으로 밀려날 뿐**이라 적의 진로를 벗어나지 못하고 결국 맞는다. 1로 바꾸면 기획서 문구와 똑같이 동작하므로 기획이 시트에서 바로 비교할 수 있다.
5. 회피 성공(E02): 회피를 누른 순간의 대상을 기억 → 그 적이 플레이어에게 **닿지 않고** 위험 범위(warnRadius 안 + 접근 중)를 벗어나면 `DodgeSucceeded` + `PerfectDodge`(기존 파티클) + "回避!" 1번. 그 사이 그 적에게 맞으면 성공 취소 → 성공과 피격이 같은 회피에서 둘 다 나오지 않는다. `successTimeout`(3초) 안에 안 벗어나면 포기.
6. 접촉은 기존 `ObstacleHitDetector`가 `PlayerHitBy(src)`로 알린다 → `PlayerHealth`가 `src.DamageRatio × 최대HP`를 깎고 무적 1.5초(깜빡임). 적은 `stopOnHit`(2초) 동안 그 자리에 멈추고 무해 → 같은 적에 계속 닿아도 매 프레임 깎이지 않는다(C04).
7. 반응 속도(결과 통계): `ReactionTracker`를 **경고가 뜬 순간 → 회피 버튼**까지의 실제 시간(슬로모션 영향 없음)으로 바꾼다. 적이 사라지지 않게 되면서 예전 방식(적마다 1번)은 한 바퀴 뒤부터 측정이 안 되기 때문이다.

### 2-6. 대면 이벤트 공통 패턴 (구애·연타)

```
시작: session.Pause(this) → player.SetInputEnabled(false) → followCamera.SuppressLookOut = true → 패널 표시 → XxxStarted
진행: 매 프레임 표시 갱신 → TimeControl.InputBlocked면 건너뜀 → inputDelay 동안 입력 무시 → Time.deltaTime으로 남은 시간 감소
끝:  결과 문구 resultShowSeconds 동안 표시(일시정지 메뉴 중엔 안 줄어듦) → 패널 숨김 → session.Resume → 입력 복구 → XxxEnded
```
- `inputDelay`: 이벤트 직전에 누르고 있던/누른 입력을 세지 않는다(P05 "イベント前の入力を除く").
- **재발동 방지**(E04 "終了直後に同じイベントが自動で再開しない"):
  - 구애 실패 → 암컷이 `failFleeSeconds` 동안 강제로 도망 + `MateGoal`은 플레이어가 `rearmDistance`(2.5) 이상 떨어져야 다시 판정.
  - 연타 종료 → 수컷은 `stopOnHit`(2초) 동안 멈춤·무해, 플레이어는 무적 1.5초(0 데미지 접촉도 무적을 건다).
- 연타 실패 데미지는 세션을 재개한 **뒤에** `DamageRequested(0.1, "Hit")`로 보낸다 → 체력이 0이 되면 정상적으로 게임 오버(S04), 남으면 탐색 복귀(E05).

### 2-7. 결과 흐름 (Result 씬 삭제)

```
PlayerHealth: HP 0 → PlayerDied("Hit"|"Starved") (1번만)
CourtshipFlow: 성공 → session.End(true, "Love")
        ▼
GameSession.End: Ended=true, 세션 일시정지 해제, 플레이어 입력 끔, SessionResult.Begin(...), RoundEnded(cleared)
        ▼  (ReactionTracker·NearMissDirector가 통계 기록)
ResultPanel: WaitForSecondsRealtime(resultDelay 1.5) → 제목 result.<reason>, 부제 result.<reason>.sub, 통계, FLY FACT
        → もういちど = SceneFlow.GoToGame()(씬 다시 로드 = 위치·체력·사과·적 전부 초기화, S05) / タイトルに戻る
```
- `GameSession`은 더 이상 씬을 바꾸지 않는다. `SceneFlow.GoToResult` 삭제.
- 하루 길이(`Session.dayLength`)는 하늘색에만 쓰인다. 라운드는 시간으로 끝나지 않는다(체력 감소가 시간 제한 역할).

### 2-8. 메뉴 — `MenuNavigator` 하나로 타이틀·일시정지·결과 공통(P02)

- 버튼 배열을 받아 스틱/십자키/화살표로 커서를 옮기고, 액션 버튼으로 **선택된 버튼의 `onClick`만 1번** 실행한다.
- 메뉴가 열린 프레임의 입력은 무시(열 때 누른 버튼이 바로 선택을 실행하지 않게).
- 선택 표시: 배경색 + 글자색 + 크기 1.08배(A06 "選択中の項目が分かり").
- 마우스 클릭도 되게 각 씬에 EventSystem을 둔다. 버튼의 Navigation은 None, Transition은 None(색은 `MenuNavigator`가 칠함) → EventSystem의 자체 내비게이션과 충돌하지 않는다.

### 2-9. 해석이 필요했던 부분 (기획에 공유할 것)

| 기획서 | 해석 | 바꾸려면 |
|---|---|---|
| P04 중립 회피 "対象の敵の進行方向へ避ける" | 기본은 진행선에서 옆으로 비켜서기(0). 문구 그대로는 1 | 시트 `Dodge.neutralMode` |
| S03 최대 속도 예 10 | 6 (반지름 12 행성에서 10은 한 바퀴 7.5초) | 시트 `Player.maxSpeed` |
| C08 도토리 경고 대상 여부 "未決定" | 경고 대상(1) | 시트 `HazardTypes` acorn `warn` |
| E05 "体力−1として勝敗を再判定" | 실패 = 최대의 10%(=1) 깎고 탐색 복귀. 수컷이 2초 뒤 다시 쫓아와 닿으면 다시 배틀 | `Rival.failDamageRatio`, `HazardTypes` male `stopOnHit` |
| C02 "直線距離20以上離れたら同地点に1個再出現" | 지점(지면) 기준 직선 거리. 행성 지름이 24라 거의 반대편까지 가야 함 | `Items.respawnDistance` |
| E03 "ここは任せます" | 실패 시 암컷 2초 도망 + 2.5 이상 떨어졌다 다시 닿으면 재도전 | `Mate.failFleeSeconds` |
| A06 타이틀 견출 "タイトル" | 게임 제목(`title.name`) 그대로 + 아래 목표 한 줄(`title.goal`) | 시트 `Text` 탭 |
| 시작 속도 = 암컷 속도 | 2-4 참고. 의도된 순환으로 보고 그대로 | `Mate.speedRatio`, `Health.startHp` |

---

## 3. 기획서 ID ↔ 구현

| ID | 기획 내용(요약) | 구현 | 확인(12장) |
|---|---|---|---|
| S01 | 타이틀 → 탐색 시작 | `TitleScreen`(あそぶ) → `SceneFlow.GoToGame` → `GameSession.Start`(시간 초기화, `RoundStarted`) | 12-1 |
| S02 | 체력 시작 5/최대 10, 초당 0.1 감소, 범위 고정 | `HealthBalance`, `PlayerHealth`(Clamp, `Time.deltaTime`) | 12-2 |
| S03 | 속도 = 최대 × 현재/최대 | `PlayerHealth.ApplySpeed` → `SphereWalker.SpeedMultiplier`, `PlayerMoveBalance.maxSpeed` | 12-2 |
| S04 | 체력 0 → 이벤트 종료, 결과로 (1번만) | `PlayerHealth.IsDead` 1회 → `PlayerDied` → `GameSession.End` → `ResultPanel` | 12-9 |
| S05 | 결과에서 재도전 시 전부 초기화 | `ResultPanel.Retry` → 씬 재로드 + `TimeControl.ResetAll` | 12-9 |
| S06 | 구애 버튼 전부 정답 → 클리어 | `CourtshipQTE` + `ButtonSequence` → `CourtshipFlow` → `End(true,"Love")` | 12-6 |
| S07 | 일시정지, 이벤트 남은 시간 유지 | `PauseMenu` + `TimeControl.SetPaused` + 세션 일시정지 | 12-8 |
| P01 | 오토런, 큰 호 선회, 중립 직진 | `SphereWalker` `AutoRun` + `PlayerMoveBalance.turnRate` | 12-1 |
| P02 | 메뉴: 스틱 선택, 액션 결정 | `MenuNavigator` (타이틀·일시정지·결과) | 12-1, 12-8, 12-9 |
| P03 | 구애 순서 입력 | `CourtshipQTE`, `InputCompat.FaceButtonPressed` | 12-6 |
| P04 | 경고 중에만 회피, 스틱 방향/중립 | `DodgeAbility` + `DodgeBalance` | 12-5 |
| P05 | 연타: 누르기 시작마다 1회, 길게 = 1회 | `RivalBattle` + `InputCompat.ActionPressed` + `inputDelay` | 12-7 |
| P06 | 포즈 버튼, 기기별 버튼 표기 일치 | `InputCompat.PausePressed/Layout/Label/ActionLabel` | 12-8 |
| C01 | 카메라 뒤에서 추적 | 기존 `PlanetFollowCamera`(변경 없음) | 12-1 |
| C02 | 사과 +2, 사라짐, 거리 20 후 재출현 | `AppleSpawner`, `AppleVisual`, `ItemBalance` | 12-3 |
| C03 | 암컷 배회/5 안 도망/복귀, 0.5배 속도, 접촉 → 구애 | `MateAI`, `MateGoal`, `MateBalance` | 12-6 |
| C04 | 적 속도 0.7배, 접촉 시작 시만 데미지 | `HazardTypes.speedRatio`, 무적 + `stopOnHit` | 12-4 |
| C05/C06 | 잠자리·거미 순환, 20% | `HazardTypes` dragonfly/spider | 12-4 |
| C07 | 수컷 추적·해제, 접촉 → 연타 | `HazardTypes` male(`chase`, `rival`) → `RivalEncounter` | 12-7 |
| C08 | 도토리 10% | `HazardTypes` acorn | 12-4 |
| E01 | 다가오는 가장 가까운 적 = 경고 대상 | `NearMissDirector.FindThreat` | 12-5 |
| E02 | 회피 성공 1번, 피격과 중복 없음 | `NearMissDirector.UpdateSuccess` | 12-5 |
| E03 | 구애 진행, 실패 후 떨어졌다 재접촉 | `CourtshipQTE`, `MateAI.Flee`, `MateGoal.Rearm` | 12-6 |
| E04 | 이벤트 중 이동·적·감소 정지, 자동 재발동 없음 | 세션 일시정지를 모든 시스템이 확인 + 2-6 재발동 방지 | 12-6, 12-7 |
| E05 | 연타 25회/6초, 실패 −1 후 재판정 | `RivalBattle`, `RivalBalance` | 12-7 |
| A01 | 3D 오브젝트 | 기존 임시 모델 + `Apple.prefab`(Fruit 모델 재사용). 최종 아트는 프리팹 `Visual`만 교체 | 12-3 |
| A02 | 첫 사과는 시작 지점 근처 | 사과 지점 P0 = 시작 지점 25° 앞 | 12-3 |
| A03 | HUD: 체력·증감, 저체력 문구, 이동 안내, 발견 마크 | `HealthBar`, `GameHud`, `MateFinderHint` | 12-2, 12-6 |
| A04 | 경고 중 4변 빨간 테두리 + 위쪽 가운데 안내 | `SYS_Hazards` `WarningFrame`, `PromptRoot` | 12-5 |
| A05 | 연타 횟수/목표 + 게이지, 구애 입력 열·다음·정답 표시 | `SYS_Rival` 패널, `SYS_Courtship` 패널 | 12-6, 12-7 |
| A06 | あそぶ / つづける・タイトルに戻る / もういちど・タイトルに戻る | `Title.unity`, `SYS_Menu` | 12-1, 12-8, 12-9 |
| A07 | 사운드 | 범위 밖. 13장 이벤트 표 | — |

---

## 4. 파일 변경 목록

경로는 `FlyGame/Assets/_Project/` 기준. **신규** = 새 파일, **교체** = 파일 전체를 스펙 코드로 덮어쓰기, **수정** = 표시한 줄만, **삭제** = 파일과 `.meta`를 함께 지움.

### 4-1. 코드

| 파일 | 구분 | 요약 |
|---|---|---|
| `Scripts/Core/Events/GameEvents.Health.cs` | 교체 | `HealRequested`가 비율로, `DamageRequested` 추가 |
| `Scripts/Core/Events/GameEvents.Courtship.cs` | 교체 | `CourtshipStarted`, `CourtshipInput` 추가 |
| `Scripts/Core/Events/GameEvents.Items.cs` | 신규 | `AppleEaten` |
| `Scripts/Core/Events/GameEvents.Threat.cs` | 신규 | `WarningChanged`, `DodgeSucceeded` |
| `Scripts/Core/Events/GameEvents.Rival.cs` | 신규 | `RivalEncounter`, `RivalBattleStarted`, `RivalPressed`, `RivalBattleEnded` |
| `Scripts/Core/Events/GameEvents.Menu.cs` | 신규 | `MenuMoved`, `MenuSelected`, `GamePaused` |
| `Scripts/Core/Session/InputCompat.cs` | 교체 | 패드·기기 판별·버튼 표기 |
| `Scripts/Core/Session/TimeControl.cs` | 신규 | `Time.timeScale`의 유일한 주인 |
| `Scripts/Core/Session/SessionBalance.cs` | 신규 | 시트 `Session` |
| `Scripts/Core/Session/GameSession.cs` | 교체 | 씬 전환 제거, `DayProgress`, `PlayerDied`로 종료 |
| `Scripts/Core/Session/SceneFlow.cs` | 교체 | Title·Game만, 전환 시 `TimeControl.ResetAll` |
| `Scripts/Core/Player/PlayerMoveBalance.cs` | 신규 | 시트 `Player` |
| `Scripts/Core/Player/SphereWalker.cs` | 교체 | `MoveMode.AutoRun`, `balance` |
| `Scripts/Core/Player/KeyboardMoveInput.cs` | 교체 | (Steer, 1), 액션 버튼 |
| `Scripts/Core/World/Obstacles/IDamageSource.cs` | 교체 | `float DamageRatio` |
| `Scripts/Features/Health/FlyJam.Health.asmdef` | 교체 | + `Unity.TextMeshPro` |
| `Scripts/Features/Health/HealthBalance.cs` | 교체 | 최대 10·시작 5·비율 데미지·최저 속도 |
| `Scripts/Features/Health/PlayerHealth.cs` | 교체 | 속도 연동, 비율 데미지, `DamageRequested` |
| `Scripts/Features/Health/HealthBar.cs` | 교체 | 숫자·증감 팝업·저체력 문구 |
| `Scripts/Features/Items/FlyJam.Items.asmdef` | 신규 | |
| `Scripts/Features/Items/ItemBalance.cs` | 신규 | 시트 `Items` |
| `Scripts/Features/Items/AppleSpawner.cs` | 신규 | 고정 지점·먹기·재출현 |
| `Scripts/Features/Items/AppleVisual.cs` | 신규 | 둥실·회전 |
| `Scripts/Features/Mate/MateBalance.cs` | 신규 | 시트 `Mate` |
| `Scripts/Features/Mate/MateAI.cs` | 교체 | 배회/도망/복귀, 속도 = 플레이어 최대 × 비율 |
| `Scripts/Features/Mate/MateGoal.cs` | 교체 | 떨어져야 재판정, 일시정지 중 판정 안 함 |
| `Scripts/Features/Mate/MateFinderHint.cs` | 교체 | 방향 안내 → 머리 위 발견 마크 |
| `Scripts/Features/Courtship/FlyJam.Courtship.asmdef` | 교체 | + `UnityEngine.UI` |
| `Scripts/Features/Courtship/CourtshipBalance.cs` | 신규 | 시트 `Courtship` |
| `Scripts/Features/Courtship/ButtonSequence.cs` | 신규 | 순서 판정(순수 로직) |
| `Scripts/Features/Courtship/CourtshipQTE.cs` | 교체 | 리듬 → 버튼 순서 패널 (클래스 이름 유지) |
| `Scripts/Features/Hazards/HazardBalance.cs` | 교체 | 고정 개체 수, 데미지·경고·라이벌 열 |
| `Scripts/Features/Hazards/RollingObstacle.cs` | 교체 | `IDamageSource`, 무한 이동, 세션 일시정지, 라이벌 |
| `Scripts/Features/Hazards/ObstacleSpawner.cs` | 교체 | 부족한 종류부터 채우기 |
| `Scripts/Features/Hazards/NearMissDirector.cs` | 교체 | 경고 대상·테두리·안내·슬로·회피 성공 (클래스 이름 유지) |
| `Scripts/Features/Hazards/ReactionTracker.cs` | 교체 | 경고 → 회피 시간 |
| `Scripts/Features/Dodge/DodgeBalance.cs` | 신규 | 시트 `Dodge` |
| `Scripts/Features/Dodge/DodgeAbility.cs` | 교체 | 경고 중에만, 스틱 방향/중립 모드 |
| `Scripts/Features/Rival/FlyJam.Rival.asmdef` | 신규 | |
| `Scripts/Features/Rival/RivalBalance.cs` | 신규 | 시트 `Rival` |
| `Scripts/Features/Rival/RivalBattle.cs` | 신규 | 연타 배틀 |
| `Scripts/Features/Fx/FxDirector.cs` | 교체 | 사과 파티클 추가 |
| `Scripts/Features/Sky/DayNightCycle.cs` | 수정 | 60번째 줄 근처 1줄 |
| `Scripts/UI/MenuNavigator.cs` | 신규 | 패드·키보드 메뉴 |
| `Scripts/UI/TitleScreen.cs` | 교체 | 버튼 방식 |
| `Scripts/UI/PauseMenu.cs` | 신규 | 일시정지 |
| `Scripts/UI/ResultPanel.cs` | 신규 | 결과 패널 |
| `Scripts/UI/GameHud.cs` | 교체 | 타이머 → 이동 안내 |
| `Scripts/UI/ResultScreen.cs` | **삭제** | Result 씬 전용 |
| `Scripts/Features/Backpedal/` (폴더 전체) | **삭제** | 뒷걸음 기능 |

### 4-2. 에셋

| 에셋 | 구분 |
|---|---|
| `Data/Balance/PlayerMoveBalance.asset`, `SessionBalance.asset`, `ItemBalance.asset`, `MateBalance.asset`, `CourtshipBalance.asset`, `RivalBalance.asset`, `DodgeBalance.asset` | 신규 (코드 기본값) |
| `Data/Balance/HealthBalance.asset`, `HazardBalance.asset` | 새 기본값으로 초기화 |
| `Data/Balance/Resources/TextTable.asset` | 6장 TSV로 전체 교체 (42줄) |
| `Prefabs/Apple.prefab` | 신규 (Fruit 모델 재사용) |
| `Prefabs/SYS_Items.prefab`, `SYS_Rival.prefab`, `SYS_Menu.prefab` | 신규 |
| `Prefabs/SYS_Health`, `SYS_Hazards`, `SYS_Courtship`, `SYS_Mate`, `SYS_Dodge`, `SYS_UI`, `SYS_Village`, `Fly`, `Mate` | 수정 |
| `Prefabs/SYS_Backpedal.prefab` | **삭제** |
| `Scenes/Game.unity` | 수정 (프리팹 인스턴스 교체, `GameSession.balance`) |
| `Scenes/Title.unity` | 수정 (버튼 메뉴) |
| `Scenes/Result.unity` | **삭제** |
| `ProjectSettings/EditorBuildSettings.asset` | Title, Game 2개만 |
| `AGENTS.md` | 9장 |

### 4-3. 삭제 방법 (AGENTS.md 6장)
`AssetDatabase.DeleteAsset`은 MCP에서 막혀 있다. 셸로 파일과 `.meta`를 함께 지운 뒤 Unity에서 `AssetDatabase.Refresh()`. 두 번에 나눈다.

**(a) 코드 — 단계 1에서** (5-14):
```bash
cd FlyGame/Assets/_Project
rm -f Scripts/UI/ResultScreen.cs Scripts/UI/ResultScreen.cs.meta
rm -rf Scripts/Features/Backpedal Scripts/Features/Backpedal.meta
```
(이 시점에 `SYS_Backpedal.prefab`과 `Result.unity`는 "Missing Script"가 되지만 컴파일 에러는 아니다. 단계 3에서 지운다.)

**(b) 프리팹·씬 — 단계 3 끝에서** (7-15에서 `SYS_Backpedal` 인스턴스를 씬에서 빼고, 7-17에서 Build Settings의 Result를 뺀 **다음**):
```bash
cd FlyGame/Assets/_Project
rm -f Prefabs/SYS_Backpedal.prefab Prefabs/SYS_Backpedal.prefab.meta
rm -f Scenes/Result.unity Scenes/Result.unity.meta
```
셸에서 삭제가 막히면 지우지 말고 보고서 "미완료"에 적는다(사람이 Unity Project 창에서 지운다).

---

## 5. 단계 1 — 코드

**순서가 중요하다.** 아래 파일을 **모두** 쓰고(4-3의 코드 삭제 포함) 나서 한 번에 컴파일한다. 중간에 컴파일하면 옛 파일이 새 API를 못 찾아 에러가 나는 게 정상이다(예: `ResultScreen`이 `SceneFlow.GoToResult`를 찾음).

- 모든 `.cs`는 UTF-8. 일본어 문자열은 코드에 없다(전부 `TextTable`). 주석 안의 일본어는 설명용이다.
- `.asmdef`는 JSON 그대로.
- 기존 파일을 "교체"할 때 `.meta`는 건드리지 않는다(GUID 유지 → 프리팹의 컴포넌트 연결이 살아 있음). 클래스 이름을 바꾸지 않은 이유도 이것이다(`CourtshipQTE`, `NearMissDirector`, `RollingObstacle`).

**자가 확인 1**: Unity 컴파일 후 Console 에러 0. 경고 중 `[SerializeField]` 관련 "never assigned"는 무시. 에러가 있으면 단계 2로 가지 않는다.


### 5-1. Core — 이벤트

#### `Scripts/Core/Events/GameEvents.Health.cs` (교체)

```csharp
using System;

namespace FlyJam
{
    public static partial class GameEvents
    {
        public static event Action<IObstacle> PlayerHitBy;              // what touched the player. Raised right before PlayerHit
        public static event Action<float, float> HealthChanged;         // current HP, max HP
        public static event Action<string> PlayerDied;                  // reason: "Hit" | "Starved" (= result title key)
        public static event Action<float> HealRequested;                // fraction of max HP to add (apple = 0.2)
        public static event Action<float, string> DamageRequested;      // fraction of max HP to remove, reason (mash battle lost)

        public static void RaisePlayerHitBy(IObstacle source) => PlayerHitBy?.Invoke(source);
        public static void RaiseHealthChanged(float current, float max) => HealthChanged?.Invoke(current, max);
        public static void RaisePlayerDied(string reason) => PlayerDied?.Invoke(reason);
        public static void RaiseHealRequested(float ratio) => HealRequested?.Invoke(ratio);
        public static void RaiseDamageRequested(float ratio, string reason) => DamageRequested?.Invoke(ratio, reason);
    }
}
```

#### `Scripts/Core/Events/GameEvents.Courtship.cs` (교체)

```csharp
using System;
using UnityEngine;

namespace FlyJam
{
    public static partial class GameEvents
    {
        public static event Action CourtshipStarted;
        public static event Action<bool> CourtshipInput;               // true = correct button, false = wrong
        public static event Action<bool, Vector3> CourtshipFinished;   // success, mate position

        public static void RaiseCourtshipStarted() => CourtshipStarted?.Invoke();
        public static void RaiseCourtshipInput(bool correct) => CourtshipInput?.Invoke(correct);
        public static void RaiseCourtshipFinished(bool ok, Vector3 p) => CourtshipFinished?.Invoke(ok, p);
    }
}
```

#### `Scripts/Core/Events/GameEvents.Items.cs` (신규)

```csharp
using System;
using UnityEngine;

namespace FlyJam
{
    public static partial class GameEvents
    {
        public static event Action<Vector3> AppleEaten;   // where it was

        public static void RaiseAppleEaten(Vector3 p) => AppleEaten?.Invoke(p);
    }
}
```

#### `Scripts/Core/Events/GameEvents.Threat.cs` (신규)

```csharp
using System;

namespace FlyJam
{
    public static partial class GameEvents
    {
        public static event Action<IMovingObstacle> WarningChanged;   // the enemy you are warned about; null = warning over
        public static event Action DodgeSucceeded;                    // dodged during a warning and the enemy passed without touching

        public static void RaiseWarningChanged(IMovingObstacle target) => WarningChanged?.Invoke(target);
        public static void RaiseDodgeSucceeded() => DodgeSucceeded?.Invoke();
    }
}
```

#### `Scripts/Core/Events/GameEvents.Rival.cs` (신규)

```csharp
using System;

namespace FlyJam
{
    public static partial class GameEvents
    {
        public static event Action<IObstacle> RivalEncounter;     // a male touched the player -> mash battle
        public static event Action RivalBattleStarted;
        public static event Action<int, int> RivalPressed;        // count, target
        public static event Action<bool> RivalBattleEnded;        // true = escaped

        public static void RaiseRivalEncounter(IObstacle male) => RivalEncounter?.Invoke(male);
        public static void RaiseRivalBattleStarted() => RivalBattleStarted?.Invoke();
        public static void RaiseRivalPressed(int count, int target) => RivalPressed?.Invoke(count, target);
        public static void RaiseRivalBattleEnded(bool won) => RivalBattleEnded?.Invoke(won);
    }
}
```

#### `Scripts/Core/Events/GameEvents.Menu.cs` (신규)

```csharp
using System;

namespace FlyJam
{
    public static partial class GameEvents
    {
        public static event Action MenuMoved;            // cursor moved (menu SE)
        public static event Action MenuSelected;         // item chosen (decide SE)
        public static event Action<bool> GamePaused;     // true = pause menu opened

        public static void RaiseMenuMoved() => MenuMoved?.Invoke();
        public static void RaiseMenuSelected() => MenuSelected?.Invoke();
        public static void RaiseGamePaused(bool paused) => GamePaused?.Invoke(paused);
    }
}
```

### 5-2. Core — 입력·시간·세션

#### `Scripts/Core/Session/InputCompat.cs` (교체)

```csharp
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine;

namespace FlyJam
{
    /// Face buttons by position, so the same physical button works on every pad. East = the "action" button.
    public enum FaceButton { East, South, North, West }

    /// Which button names to show on screen. Follows the device the player touched last.
    public enum PadLayout { Keyboard, Switch, Xbox, PlayStation }

    /// The only place that reads keyboard / gamepad. Gameplay code asks "what was pressed", never which device.
    /// Action button = right face button (Switch A / Xbox B / PS ○) or Space / Enter.
    /// Pause = Start (Switch +) or Esc. Steering = left stick / D-pad / A-D / arrow keys.
    public static class InputCompat
    {
        const float StickDead = 0.2f;
        const float MenuOn = 0.6f;
        const float MenuOff = 0.3f;

        static PadLayout layout = PadLayout.Keyboard;
        static bool layoutInit;
        static int layoutFrame = -1;
        static int menuFrame = -1;
        static Vector2Int menuMove;
        static bool menuStickHeld;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            layout = PadLayout.Keyboard;
            layoutInit = false;
            layoutFrame = -1;
            menuFrame = -1;
            menuMove = Vector2Int.zero;
            menuStickHeld = false;
        }

        // ───────── Gameplay ─────────

        /// Steering -1 (left) .. +1 (right). Keys give a full turn, the stick is proportional.
        public static float Steer()
        {
            float v = 0f;
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k != null)
                v = (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1f : 0f)
                  - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1f : 0f);
            var p = Gamepad.current;
            if (p != null)
            {
                float s = p.leftStick.ReadValue().x;
                s = Mathf.Abs(s) < StickDead ? 0f : Mathf.Sign(s) * Mathf.InverseLerp(StickDead, 1f, Mathf.Abs(s));
                float d = p.dpad.ReadValue().x;
                if (Mathf.Abs(d) > Mathf.Abs(s)) s = d;
                if (Mathf.Abs(s) > Mathf.Abs(v)) v = s;
            }
#else
            v = Input.GetAxisRaw("Horizontal");
#endif
            return Mathf.Clamp(v, -1f, 1f);
        }

        /// Action button pressed this frame. Rising edge only: holding it down counts once.
        public static bool ActionPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            var p = Gamepad.current;
            return (k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame))
                || (p != null && p.buttonEast.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
#endif
        }

        public static bool PausePressed()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            var p = Gamepad.current;
            return (k != null && k.escapeKey.wasPressedThisFrame) || (p != null && p.startButton.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        /// One of the four face buttons pressed this frame (courtship input).
        /// Keyboard: arrow keys or WASD by position (→/D = East, ↓/S = South, ↑/W = North, ←/A = West).
        public static bool FaceButtonPressed(out FaceButton button)
        {
            button = FaceButton.East;
#if ENABLE_INPUT_SYSTEM
            var p = Gamepad.current;
            if (p != null)
            {
                if (p.buttonEast.wasPressedThisFrame) { button = FaceButton.East; return true; }
                if (p.buttonSouth.wasPressedThisFrame) { button = FaceButton.South; return true; }
                if (p.buttonNorth.wasPressedThisFrame) { button = FaceButton.North; return true; }
                if (p.buttonWest.wasPressedThisFrame) { button = FaceButton.West; return true; }
            }
            var k = Keyboard.current;
            if (k != null)
            {
                if (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame) { button = FaceButton.East; return true; }
                if (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame) { button = FaceButton.South; return true; }
                if (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame) { button = FaceButton.North; return true; }
                if (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame) { button = FaceButton.West; return true; }
            }
#endif
            return false;
        }

        // ───────── Menus ─────────

        /// Menu cursor step this frame: x -1/0/+1, y +1 = up / -1 = down. Same answer for every caller in a frame.
        public static Vector2Int MenuMove()
        {
            if (menuFrame == Time.frameCount) return menuMove;
            menuFrame = Time.frameCount;
            var m = Vector2Int.zero;
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k != null)
            {
                if (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame) m.y = 1;
                else if (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame) m.y = -1;
                else if (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame) m.x = -1;
                else if (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame) m.x = 1;
            }
            var p = Gamepad.current;
            if (p != null && m == Vector2Int.zero)
            {
                if (p.dpad.up.wasPressedThisFrame) m.y = 1;
                else if (p.dpad.down.wasPressedThisFrame) m.y = -1;
                else if (p.dpad.left.wasPressedThisFrame) m.x = -1;
                else if (p.dpad.right.wasPressedThisFrame) m.x = 1;
                else
                {
                    Vector2 s = p.leftStick.ReadValue();
                    if (!menuStickHeld && (Mathf.Abs(s.x) > MenuOn || Mathf.Abs(s.y) > MenuOn))
                    {
                        menuStickHeld = true;
                        if (Mathf.Abs(s.y) >= Mathf.Abs(s.x)) m.y = s.y > 0f ? 1 : -1;
                        else m.x = s.x > 0f ? 1 : -1;
                    }
                    else if (Mathf.Abs(s.x) < MenuOff && Mathf.Abs(s.y) < MenuOff) menuStickHeld = false;
                }
            }
#endif
            menuMove = m;
            return m;
        }

        // ───────── Button names on screen ─────────

        public static PadLayout Layout
        {
            get
            {
                UpdateLayout();
                return layout;
            }
        }

        public static bool UsingPad => Layout != PadLayout.Keyboard;

        /// Name of a face button for the current device (courtship slots).
        public static string Label(FaceButton b)
        {
            switch (Layout)
            {
                case PadLayout.Switch: return b == FaceButton.East ? "A" : b == FaceButton.South ? "B" : b == FaceButton.North ? "X" : "Y";
                case PadLayout.Xbox: return b == FaceButton.East ? "B" : b == FaceButton.South ? "A" : b == FaceButton.North ? "Y" : "X";
                case PadLayout.PlayStation: return b == FaceButton.East ? "○" : b == FaceButton.South ? "×" : b == FaceButton.North ? "△" : "□";
                default: return b == FaceButton.East ? "→" : b == FaceButton.South ? "↓" : b == FaceButton.North ? "↑" : "←";
            }
        }

        /// Name of the action button for prompts ("Ⓐボタンで回避！").
        public static string ActionLabel
        {
            get
            {
                switch (Layout)
                {
                    case PadLayout.Switch: return "Ⓐ";
                    case PadLayout.Xbox: return "Ⓑ";
                    case PadLayout.PlayStation: return "○";
                    default: return "SPACE";
                }
            }
        }

        static void UpdateLayout()
        {
            if (layoutFrame == Time.frameCount) return;
            layoutFrame = Time.frameCount;
#if ENABLE_INPUT_SYSTEM
            var p = Gamepad.current;
            if (!layoutInit)
            {
                layoutInit = true;
                if (p != null) layout = KindOf(p);
            }
            var k = Keyboard.current;
            if (k != null && k.anyKey.wasPressedThisFrame) layout = PadLayout.Keyboard;
            else if (p != null && PadTouched(p)) layout = KindOf(p);
#endif
        }

#if ENABLE_INPUT_SYSTEM
        static bool PadTouched(Gamepad p) =>
            p.buttonEast.wasPressedThisFrame || p.buttonSouth.wasPressedThisFrame ||
            p.buttonNorth.wasPressedThisFrame || p.buttonWest.wasPressedThisFrame ||
            p.startButton.wasPressedThisFrame || p.leftStick.ReadValue().sqrMagnitude > 0.25f ||
            p.dpad.ReadValue().sqrMagnitude > 0.25f;

        static PadLayout KindOf(Gamepad p)
        {
            string n = (p.GetType().Name + " " + p.displayName + " " + p.description.product).ToLowerInvariant();
            if (n.Contains("switch") || n.Contains("pro controller") || n.Contains("joy-con")) return PadLayout.Switch;
            if (n.Contains("dualshock") || n.Contains("dualsense") || n.Contains("playstation") || n.Contains("wireless controller"))
                return PadLayout.PlayStation;
            return PadLayout.Xbox;
        }
#endif

        // ───────── Old names (kept so existing calls still compile) ─────────

        public static Vector2 Move() => new Vector2(Steer(), 0f);
        public static bool ConfirmPressed() => ActionPressed();
        public static bool BackPressed() => PausePressed();
        public static bool DodgePressed() => ActionPressed();

        /// Toggle pixel-art post-processing on/off for comparison (P key).
        public static bool TogglePixelPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            return k != null && k.pKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.P);
#endif
        }
    }
}
```

#### `Scripts/Core/Session/TimeControl.cs` (신규)

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace FlyJam
{
    /// The only code that writes Time.timeScale.
    /// Pause menu -> 0. Slow motion -> the lowest requested scale. Otherwise 1.
    public static class TimeControl
    {
        static readonly Dictionary<object, float> slows = new Dictionary<object, float>();
        static bool paused;
        static int resumedFrame = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            slows.Clear();
            paused = false;
            resumedFrame = -1;
        }

        public static bool Paused => paused;

        /// True while paused and on the frame the pause was closed, so the button that closed it is not reused by gameplay.
        public static bool InputBlocked => paused || Time.frameCount == resumedFrame;

        public static void SetPaused(bool on)
        {
            if (paused && !on) resumedFrame = Time.frameCount;
            paused = on;
            Apply();
        }

        /// Ask for slow motion (scale below 1). Pass 1 to release your request.
        public static void SetSlow(object by, float scale)
        {
            if (by == null) return;
            if (scale >= 1f) slows.Remove(by);
            else slows[by] = Mathf.Max(0.05f, scale);
            Apply();
        }

        /// Normal speed, nothing paused. Called on every scene change.
        public static void ResetAll()
        {
            slows.Clear();
            paused = false;
            Apply();
        }

        static void Apply()
        {
            float s = 1f;
            foreach (var v in slows.Values) s = Mathf.Min(s, v);
            if (paused) s = 0f;
            Time.timeScale = s;
            Time.fixedDeltaTime = 0.02f * Mathf.Max(s, 0.05f);
        }
    }
}
```

#### `Scripts/Core/Session/SessionBalance.cs` (신규)

```csharp
using UnityEngine;

namespace FlyJam
{
    /// Planner numbers for the round. Sheet tab "Session".
    public class SessionBalance : BalanceTable
    {
        public override string SheetName => "Session";

        [Tooltip("Seconds for one sky cycle (sunrise -> night). Repeats. Does not end the round")]
        [SerializeField] float dayLength = 60f;
        [Tooltip("Seconds between the end of play and the result panel")]
        [SerializeField] float resultDelay = 1.5f;

        public float DayLength => Mathf.Max(5f, dayLength);
        public float ResultDelay => Mathf.Max(0f, resultDelay);
    }
}
```

#### `Scripts/Core/Session/GameSession.cs` (교체)

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace FlyJam
{
    /// Core round lifecycle only: clock, pause holders, end state.
    /// The round ends when a feature calls End (courtship -> "Love") or on GameEvents.PlayerDied ("Hit" / "Starved").
    /// No scene change here: the result panel (UI) listens to RoundEnded.
    public class GameSession : MonoBehaviour
    {
        [SerializeField] SphereWalker player;          // Auto-filled from GameContext if null
        [SerializeField] SessionBalance balance;
        [Tooltip("Used only when 'balance' is empty")]
        [FormerlySerializedAs("timeLimit")]
        [SerializeField] float dayLength = 45f;
        [Tooltip("Used only when 'balance' is empty")]
        [SerializeField] float resultDelay = 2.0f;

        readonly HashSet<object> pausers = new HashSet<object>();

        public float Elapsed { get; private set; }
        public float DayLength => balance != null ? balance.DayLength : Mathf.Max(5f, dayLength);
        public float ResultDelay => balance != null ? balance.ResultDelay : resultDelay;
        public float Remaining => Mathf.Max(0f, DayLength - Elapsed);
        /// 0 -> 1 over the first day, then stays 1.
        public float Normalized => Mathf.Clamp01(Elapsed / DayLength);
        /// 0 -> 1 every day, repeating (sky colour).
        public float DayProgress => Mathf.Repeat(Elapsed / DayLength, 1f);
        public bool Ended { get; private set; }
        /// True while anything holds a pause: courtship, mash battle, pause menu.
        /// Freezes the clock, sky, enemies, apples and HP.
        public bool IsPaused => pausers.Count > 0;
        public SphereWalker Player => player;

        void Awake()
        {
            if (player == null && GameContext.Current != null) player = GameContext.Current.Player;
        }

        void OnEnable() => GameEvents.PlayerDied += OnPlayerDied;
        void OnDisable() => GameEvents.PlayerDied -= OnPlayerDied;

        void Start()
        {
            TimeControl.ResetAll();
            GameEvents.RaiseRoundStarted();
        }

        void Update()
        {
            if (Ended || IsPaused) return;
            Elapsed += Time.deltaTime;
        }

        public void Pause(object by)
        {
            if (by != null) pausers.Add(by);
        }

        public void Resume(object by)
        {
            if (by != null) pausers.Remove(by);
        }

        void OnPlayerDied(string reason)
        {
            if (Ended) return;
            End(false, string.IsNullOrEmpty(reason) ? "Hit" : reason);
        }

        /// Ends the round once. 'reason' picks the result title (text key "result.<reason lower case>").
        public void End(bool cleared, string reason)
        {
            if (Ended) return;
            Ended = true;
            pausers.Clear();
            if (player != null)
            {
                player.SetInputEnabled(false);
                if (!cleared) player.SetState(FlyState.Disabled);
            }

            SessionResult.Begin(cleared, Elapsed, reason);
            GameEvents.RaiseRoundEnded(cleared);   // features record stats here; ResultPanel shows after ResultDelay
        }
    }
}
```

#### `Scripts/Core/Session/SceneFlow.cs` (교체)

```csharp
using UnityEngine.SceneManagement;

namespace FlyJam
{
    /// Two scenes: Title and Game. Results are a panel inside Game (no Result scene).
    public static class SceneFlow
    {
        public const string Title = "Title";
        public const string Game = "Game";

        public static void GoToTitle()
        {
            TimeControl.ResetAll();
            SceneManager.LoadScene(Title);
        }

        /// Also used for "retry": reloading the scene resets player, HP, enemies, apples and events (S05).
        public static void GoToGame()
        {
            TimeControl.ResetAll();
            SceneManager.LoadScene(Game);
        }
    }
}
```

### 5-3. Core — 플레이어·장애물

#### `Scripts/Core/Player/PlayerMoveBalance.cs` (신규)

```csharp
using UnityEngine;

namespace FlyJam
{
    /// Planner numbers for player movement. Sheet tab "Player".
    /// Actual speed = maxSpeed x HP ratio (set by Health through SphereWalker.SpeedMultiplier).
    public class PlayerMoveBalance : BalanceTable
    {
        public override string SheetName => "Player";

        [Tooltip("Speed at full HP (units per second). Enemies and the female use this as 'player max speed'")]
        [SerializeField] float maxSpeed = 6f;
        [Tooltip("Turn speed at full stick tilt, degrees per second. Lower = wider arcs")]
        [SerializeField] float turnRate = 75f;
        [Tooltip("Stick tilt ignored below this (0-1)")]
        [SerializeField] float steerDeadZone = 0.15f;

        public float MaxSpeed => Mathf.Max(0.1f, maxSpeed);
        public float TurnRate => Mathf.Max(1f, turnRate);
        public float SteerDeadZone => Mathf.Clamp01(steerDeadZone);
    }
}
```

#### `Scripts/Core/Player/SphereWalker.cs` (교체)

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FlyJam
{
    /// Core fly state. Fixed contract - features must not add values without coordinating with Core.
    public enum FlyState { Stopped, Walking, Dodging, Celebrating, Disabled }

    /// AutoRun = Player: always moves forward, input x steers (arc turns). Stops only while input is disabled (events, end).
    /// Tank    = old player mode: input y = forward/backward, x = turn in place.
    /// Steer   = Mate (AI): input (x, y) = desired local movement direction; body turns toward it.
    public enum MoveMode { Tank, Steer, AutoRun }

    /// Spherical surface locomotion. Abilities like Dodge temporarily take control via BeginOverride.
    public class SphereWalker : MonoBehaviour
    {
        [SerializeField] Planet planet;                     // Auto-filled from GameContext if null
        [SerializeField] MoveMode moveMode = MoveMode.Tank;
        [SerializeField] bool isPlayer = true;
        [Tooltip("Player: leave empty to use the values below. With a balance asset, maxSpeed/turnRate come from the sheet")]
        [SerializeField] PlayerMoveBalance balance;
        [SerializeField] float moveSpeed = 4f;
        [SerializeField] float backwardSpeedRatio = 0.7f;
        [SerializeField] float tankTurnSpeed = 150f;
        [SerializeField] float turnAnimSpeed = 1.5f;
        [SerializeField] float steerTurnSpeed = 540f;
        [SerializeField] float inputDeadZone = 0.1f;
        [SerializeField] float surfaceOffset = 0f;
        [SerializeField] float bodyRadius = 0.3f;

        public Planet Planet => planet;
        public bool IsPlayer => isPlayer;
        public IMoveInput Input => input;
        public bool InputEnabled => inputEnabled;
        public MoveMode Mode => moveMode;
        public FlyState State { get; private set; } = FlyState.Stopped;
        public float TimeInState { get; private set; }
        public float CurrentSpeed { get; private set; }
        public float SignedSpeed { get; private set; }
        /// Speed before SpeedMultiplier (= "player max speed" for enemies and the female).
        public float MoveSpeed => balance != null ? balance.MaxSpeed : moveSpeed;
        /// AutoRun turn speed at full tilt (degrees per second).
        public float TurnRate => balance != null ? balance.TurnRate : tankTurnSpeed;
        public float SpeedMultiplier { get; set; } = 1f;
        /// Whether an ability currently overrides movement (e.g., Dodge).
        public bool IsOverridden => overrides.Count > 0;
        /// Whether an ability currently grants invulnerability (ignores hazards during dodge).
        public bool IsInvulnerable => invulnerable.Count > 0;
        public event Action<FlyState> StateChanged;

        IMoveInput input;
        bool inputEnabled = true;
        bool scripted;
        readonly HashSet<object> overrides = new HashSet<object>();
        readonly HashSet<object> invulnerable = new HashSet<object>();

        void Awake()
        {
            if (planet == null && GameContext.Current != null) planet = GameContext.Current.Planet;
            input = GetComponent<IMoveInput>();
            if (input == null) Debug.LogWarning("[SphereWalker] Missing IMoveInput component.", this);
        }

        void Start()
        {
            transform.position = planet.SnapToSurface(transform.position, surfaceOffset);
            AlignToSurface();
        }

        void Update()
        {
            TimeInState += Time.deltaTime;
            if (scripted || IsOverridden) return;

            Vector3 up = planet.UpAt(transform.position);
            bool canControl = inputEnabled && input != null;
            Vector2 raw = canControl ? Vector2.ClampMagnitude(input.ReadMove(), 1f) : Vector2.zero;

            if (moveMode == MoveMode.AutoRun) UpdateAutoRun(raw, canControl, up);
            else if (moveMode == MoveMode.Tank) UpdateTank(raw, up);
            else UpdateSteer(raw, up);

            if (State == FlyState.Stopped || State == FlyState.Walking)
                SetState(Mathf.Abs(SignedSpeed) > 0.01f ? FlyState.Walking : FlyState.Stopped);
        }

        /// P01: always forward; x steers with a capped turn rate, so turns become wide arcs. Neutral = straight.
        void UpdateAutoRun(Vector2 raw, bool canControl, Vector3 up)
        {
            if (!canControl)
            {
                CurrentSpeed = 0f;
                SignedSpeed = 0f;
                AlignToSurface();
                return;
            }

            float dead = balance != null ? balance.SteerDeadZone : inputDeadZone;
            float steer = Mathf.Abs(raw.x) > dead ? raw.x : 0f;
            if (steer != 0f) TurnAround(steer * TurnRate * Time.deltaTime);

            float speed = MoveSpeed * SpeedMultiplier;
            Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, up).normalized;
            MoveBy(fwd * speed * Time.deltaTime);
            AlignToSurface();

            CurrentSpeed = speed;
            SignedSpeed = speed;
        }

        void UpdateTank(Vector2 raw, Vector3 up)
        {
            float turn = Mathf.Abs(raw.x) > inputDeadZone ? raw.x : 0f;
            float drive = Mathf.Abs(raw.y) > inputDeadZone ? raw.y : 0f;

            if (turn != 0f) TurnAround(turn * tankTurnSpeed * Time.deltaTime);

            float speed = drive * MoveSpeed * (drive < 0f ? backwardSpeedRatio : 1f) * SpeedMultiplier;
            if (speed != 0f)
            {
                Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, up).normalized;
                MoveBy(fwd * speed * Time.deltaTime);
            }
            AlignToSurface();

            CurrentSpeed = Mathf.Abs(speed);
            SignedSpeed = speed != 0f ? speed : Mathf.Abs(turn) * turnAnimSpeed;
        }

        void UpdateSteer(Vector2 raw, Vector3 up)
        {
            Vector3 move = Vector3.zero;
            if (raw.magnitude > inputDeadZone)
            {
                Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, up).normalized;
                Vector3 right = Vector3.Cross(up, fwd);
                move = fwd * raw.y + right * raw.x;
            }

            CurrentSpeed = move.magnitude * MoveSpeed * SpeedMultiplier;
            if (CurrentSpeed > 0f) MoveBy(move * MoveSpeed * SpeedMultiplier * Time.deltaTime);
            AlignToSurface();
            if (CurrentSpeed > 0f) TurnToward(move, steerTurnSpeed);
            SignedSpeed = CurrentSpeed;
        }

        // ── API for abilities (Dodge, etc.) ──

        public void BeginOverride(object by) { if (by != null) overrides.Add(by); }
        public void EndOverride(object by) { if (by != null) overrides.Remove(by); }

        public void SetInvulnerable(object by, bool on)
        {
            if (by == null) return;
            if (on) invulnerable.Add(by); else invulnerable.Remove(by);
        }

        /// Moves along the planet surface and resolves Blocker collisions.
        public void MoveBy(Vector3 delta)
        {
            Vector3 p = planet.SnapToSurface(transform.position + delta, surfaceOffset);
            transform.position = SurfaceCollision.ResolveBlockers(planet, p, bodyRadius, surfaceOffset);
        }

        /// Rotates in place around the local surface up axis (degrees).
        public void TurnAround(float degrees)
        {
            Vector3 up = planet.UpAt(transform.position);
            transform.rotation = Quaternion.AngleAxis(degrees, up) * transform.rotation;
        }

        public void AlignToSurface()
        {
            Vector3 up = planet.UpAt(transform.position);
            transform.rotation = Quaternion.FromToRotation(transform.up, up) * transform.rotation;
        }

        /// Allows an overriding ability to set speed values for animation and camera.
        public void SetMotion(float currentSpeed, float signedSpeed)
        {
            CurrentSpeed = currentSpeed;
            SignedSpeed = signedSpeed;
        }

        // ── Standard API ──

        void TurnToward(Vector3 dir, float degPerSec)
        {
            Vector3 up = planet.UpAt(transform.position);
            Vector3 d = Vector3.ProjectOnPlane(dir, up);
            if (d.sqrMagnitude < 1e-6f) return;
            Quaternion target = Quaternion.LookRotation(d.normalized, up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, degPerSec * Time.deltaTime);
        }

        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
            if (!enabled) { CurrentSpeed = 0f; SignedSpeed = 0f; }
        }

        public void SetState(FlyState s)
        {
            if (State == s) return;
            State = s;
            TimeInState = 0f;
            StateChanged?.Invoke(s);
            if (isPlayer) GameEvents.RaisePlayerStateChanged(s);
        }

        public void FaceTowards(Vector3 worldPoint)
        {
            Vector3 up = planet.UpAt(transform.position);
            Vector3 d = Vector3.ProjectOnPlane(worldPoint - transform.position, up);
            if (d.sqrMagnitude > 1e-6f) transform.rotation = Quaternion.LookRotation(d.normalized, up);
        }

        public void FlyTo(Vector3 worldPoint, float duration, float arcHeight)
        {
            StopAllCoroutines();
            StartCoroutine(FlyToRoutine(worldPoint, duration, arcHeight));
        }

        IEnumerator FlyToRoutine(Vector3 end, float duration, float arcHeight)
        {
            scripted = true;
            CurrentSpeed = 0f;
            SignedSpeed = 0f;
            Vector3 start = transform.position;
            for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.01f, duration))
            {
                Vector3 p = Vector3.Lerp(start, end, t);
                Vector3 up = planet.UpAt(p);
                transform.position = p + up * (arcHeight * Mathf.Sin(t * Mathf.PI));
                transform.rotation = Quaternion.FromToRotation(transform.up, up) * transform.rotation;
                yield return null;
            }
            transform.position = end;
            scripted = false;
        }
    }
}
```

#### `Scripts/Core/Player/KeyboardMoveInput.cs` (교체)

```csharp
using UnityEngine;

namespace FlyJam
{
    /// Player input for SphereWalker (keyboard + gamepad through InputCompat; the class name is kept for prefab links).
    /// x = steering. y = 1 (forward) - ignored by AutoRun, used only if the walker is switched back to Tank.
    public class KeyboardMoveInput : MonoBehaviour, IMoveInput
    {
        public Vector2 ReadMove() => new Vector2(InputCompat.Steer(), 1f);
        public bool ReadDodge() => InputCompat.ActionPressed();
    }
}
```

#### `Scripts/Core/World/Obstacles/IDamageSource.cs` (교체)

```csharp
namespace FlyJam
{
    /// Optional for an IObstacle: HP it takes on contact, as a fraction of max HP (0.2 = 20%).
    /// Return a negative value to use HealthBalance.obstacleDamageRatio instead. 0 = no damage (e.g., the male starts a mash battle).
    public interface IDamageSource
    {
        float DamageRatio { get; }
    }
}
```

### 5-4. Features/Health

#### `Scripts/Features/Health/FlyJam.Health.asmdef` (교체)

```json
{
    "name": "FlyJam.Health",
    "rootNamespace": "FlyJam",
    "references": [
        "FlyJam.Core",
        "UnityEngine.UI",
        "Unity.TextMeshPro"
    ],
    "autoReferenced": true
}
```

#### `Scripts/Features/Health/HealthBalance.cs` (교체)

```csharp
using UnityEngine;

namespace FlyJam
{
    /// Planner numbers for HP (S02, S03). Sheet tab "Health".
    public class HealthBalance : BalanceTable
    {
        public override string SheetName => "Health";

        [Tooltip("Max HP")]
        [SerializeField] float maxHp = 10f;
        [Tooltip("HP at the start of a round")]
        [SerializeField] float startHp = 5f;
        [Tooltip("HP lost per second while exploring (not during events or pause). 0 = off")]
        [SerializeField] float drainPerSecond = 0.1f;
        [Tooltip("Damage (fraction of max HP) for obstacles without their own value")]
        [SerializeField] float obstacleDamageRatio = 0.2f;
        [Tooltip("Speed never drops below this fraction of max speed (0 = exactly max speed x HP ratio)")]
        [SerializeField] float minSpeedRatio = 0.3f;
        [Tooltip("Seconds of invulnerability (blinking) after a hit")]
        [SerializeField] float invulnerableTime = 1.5f;
        [Tooltip("Blink speed while invulnerable: seconds per on/off step")]
        [SerializeField] float blinkInterval = 0.08f;
        [Tooltip("At or below this HP ratio: red bar + 'find an apple' message (0-1)")]
        [SerializeField] float lowHpRatio = 0.3f;

        public float MaxHp => Mathf.Max(0.1f, maxHp);
        public float StartHp => Mathf.Clamp(startHp, 0.01f, MaxHp);
        public float DrainPerSecond => Mathf.Max(0f, drainPerSecond);
        public float ObstacleDamageRatio => Mathf.Max(0f, obstacleDamageRatio);
        public float MinSpeedRatio => Mathf.Clamp01(minSpeedRatio);
        public float InvulnerableTime => Mathf.Max(0f, invulnerableTime);
        public float BlinkInterval => Mathf.Max(0.02f, blinkInterval);
        public float LowHpRatio => Mathf.Clamp01(lowHpRatio);
    }
}
```

#### `Scripts/Features/Health/PlayerHealth.cs` (교체)

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace FlyJam
{
    /// Player HP (S02-S04). Starts at startHp, drains over time, drops on contact, refills from apples.
    /// Also sets the player's speed: SpeedMultiplier = max(minSpeedRatio, HP / maxHP) (S03).
    /// HP 0 -> GameEvents.PlayerDied("Hit" or "Starved") once. Frozen while the session is paused or ended.
    public class PlayerHealth : MonoBehaviour
    {
        public const string ReasonHit = "Hit";
        public const string ReasonStarved = "Starved";

        [SerializeField] HealthBalance balance;
        [SerializeField] SphereWalker player;   // Auto-filled from GameContext if null
        [SerializeField] GameSession session;   // Auto-filled from GameContext if null

        public float Current { get; private set; }
        public float Max => balance != null ? balance.MaxHp : 10f;
        public float Ratio => Max > 0f ? Current / Max : 0f;
        public bool IsDead { get; private set; }

        readonly List<Renderer> blinkRenderers = new List<Renderer>();
        float invulnerableLeft;
        bool hidden;

        void Awake()
        {
            var ctx = GameContext.Current;
            if (player == null && ctx != null) player = ctx.Player;
            if (session == null && ctx != null) session = ctx.Session;
            if (player != null)
                foreach (var r in player.GetComponentsInChildren<Renderer>())
                    if (r.enabled) blinkRenderers.Add(r);
        }

        void OnEnable()
        {
            GameEvents.PlayerHitBy += OnHitBy;
            GameEvents.HealRequested += OnHeal;
            GameEvents.DamageRequested += OnDamageRequested;
        }

        void OnDisable()
        {
            GameEvents.PlayerHitBy -= OnHitBy;
            GameEvents.HealRequested -= OnHeal;
            GameEvents.DamageRequested -= OnDamageRequested;
            EndInvulnerable();
        }

        void Start()
        {
            Current = balance != null ? balance.StartHp : 5f;
            GameEvents.RaiseHealthChanged(Current, Max);
            ApplySpeed();
        }

        void Update()
        {
            if (IsDead || session == null || session.Ended) return;
            if (session.IsPaused) { SetHidden(false); return; }

            if (invulnerableLeft > 0f)
            {
                invulnerableLeft -= Time.deltaTime;
                if (invulnerableLeft <= 0f) EndInvulnerable();
                else
                {
                    float step = balance != null ? balance.BlinkInterval : 0.08f;
                    SetHidden(Mathf.Repeat(invulnerableLeft, step * 2f) < step);
                }
            }

            float drain = balance != null ? balance.DrainPerSecond : 0f;
            if (drain > 0f) TakeDamage(drain * Time.deltaTime, ReasonStarved);
            ApplySpeed();
        }

        void ApplySpeed()
        {
            if (player == null) return;
            float min = balance != null ? balance.MinSpeedRatio : 0.3f;
            player.SpeedMultiplier = Mathf.Max(min, Ratio);
        }

        void OnHitBy(IObstacle source)
        {
            if (IsDead || invulnerableLeft > 0f) return;
            if (session != null && (session.Ended || session.IsPaused)) return;

            float fallback = balance != null ? balance.ObstacleDamageRatio : 0.2f;
            float ratio = source is IDamageSource ds && ds.DamageRatio >= 0f ? ds.DamageRatio : fallback;
            TakeDamage(ratio * Max, ReasonHit);
            if (IsDead) return;

            // Also after a 0-damage contact (male -> mash battle): avoids an instant second contact afterwards
            invulnerableLeft = balance != null ? balance.InvulnerableTime : 1.5f;
            if (invulnerableLeft > 0f && player != null) player.SetInvulnerable(this, true);
        }

        void OnHeal(float ratio)
        {
            if (IsDead || ratio <= 0f) return;
            Current = Mathf.Min(Max, Current + ratio * Max);
            GameEvents.RaiseHealthChanged(Current, Max);
            ApplySpeed();
        }

        /// Not blocked by pause: the mash battle requests damage right after it resumes the session.
        void OnDamageRequested(float ratio, string reason)
        {
            if (IsDead || ratio <= 0f) return;
            TakeDamage(ratio * Max, string.IsNullOrEmpty(reason) ? ReasonHit : reason);
        }

        void TakeDamage(float amount, string reason)
        {
            if (IsDead || amount <= 0f) return;
            Current = Mathf.Max(0f, Current - amount);
            GameEvents.RaiseHealthChanged(Current, Max);
            ApplySpeed();
            if (Current > 0f) return;

            IsDead = true;
            EndInvulnerable();
            GameEvents.RaisePlayerDied(reason);
        }

        void EndInvulnerable()
        {
            invulnerableLeft = 0f;
            SetHidden(false);
            if (player != null) player.SetInvulnerable(this, false);
        }

        void SetHidden(bool hide)
        {
            if (hidden == hide) return;
            hidden = hide;
            foreach (var r in blinkRenderers)
                if (r != null) r.enabled = !hide;
        }
    }
}
```

#### `Scripts/Features/Health/HealthBar.cs` (교체)

```csharp
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlyJam
{
    /// HUD top-left (A03): HP gauge, "5/10" numbers, "+2"/"-2" change popup, low-HP message.
    /// The popup shows the real change (near the cap a 2-point apple may show "+1"); the slow drain is not shown.
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] HealthBalance balance;
        [SerializeField] RectTransform fill;            // stretched by anchorMax.x = HP ratio
        [SerializeField] Image fillImage;
        [SerializeField] TMP_Text valueText;            // "5/10"
        [SerializeField] TMP_Text deltaText;            // "+2" / "-2"
        [SerializeField] GameObject lowHpMessage;       // LocalizedText "hud.lowhp" ("りんごを探そう！")
        [SerializeField] Color normalColor = new Color(0.36f, 0.62f, 0.40f, 1f);
        [SerializeField] Color lowColor = new Color(0.85f, 0.25f, 0.22f, 1f);
        [SerializeField] Color lowDimColor = new Color(0.55f, 0.15f, 0.15f, 1f);
        [SerializeField] Color healColor = new Color(0.25f, 0.6f, 0.3f, 1f);
        [SerializeField] Color damageColor = new Color(0.8f, 0.2f, 0.2f, 1f);
        [SerializeField] float lowBlinkPeriod = 0.6f;
        [SerializeField] float deltaShowSeconds = 1.2f;
        [SerializeField] float deltaMin = 0.05f;        // smaller changes (per-frame drain) are not shown

        float ratio = 1f;
        float last = -1f;
        float deltaLeft;

        void OnEnable() => GameEvents.HealthChanged += OnChanged;
        void OnDisable() => GameEvents.HealthChanged -= OnChanged;

        void Start()
        {
            if (deltaText != null) deltaText.gameObject.SetActive(false);
        }

        void OnChanged(float current, float max)
        {
            ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            if (fill != null) fill.anchorMax = new Vector2(ratio, fill.anchorMax.y);
            if (valueText != null) valueText.text = Mathf.CeilToInt(current - 0.0001f) + "/" + Mathf.RoundToInt(max);

            if (last >= 0f)
            {
                float d = current - last;
                if (Mathf.Abs(d) >= deltaMin && deltaText != null)
                {
                    string n = d.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
                    deltaText.text = d > 0f ? "+" + n : n;
                    deltaText.color = d > 0f ? healColor : damageColor;
                    deltaText.gameObject.SetActive(true);
                    deltaLeft = deltaShowSeconds;
                }
            }
            last = current;
        }

        void Update()
        {
            float low = balance != null ? balance.LowHpRatio : 0.3f;
            bool isLow = ratio <= low;
            if (lowHpMessage != null && lowHpMessage.activeSelf != isLow) lowHpMessage.SetActive(isLow);

            if (fillImage != null)
            {
                if (!isLow) fillImage.color = normalColor;
                else fillImage.color = Mathf.Repeat(Time.unscaledTime, lowBlinkPeriod) < lowBlinkPeriod * 0.5f ? lowColor : lowDimColor;
            }

            if (deltaLeft > 0f)
            {
                deltaLeft -= Time.unscaledDeltaTime;
                if (deltaLeft <= 0f && deltaText != null) deltaText.gameObject.SetActive(false);
            }
        }
    }
}
```

### 5-5. Features/Items (신규 기능)

#### `Scripts/Features/Items/FlyJam.Items.asmdef` (신규)

```json
{
    "name": "FlyJam.Items",
    "rootNamespace": "FlyJam",
    "references": [
        "FlyJam.Core"
    ],
    "autoReferenced": true
}
```

#### `Scripts/Features/Items/ItemBalance.cs` (신규)

```csharp
using UnityEngine;

namespace FlyJam
{
    /// Planner numbers for red apples (C02). Sheet tab "Items".
    public class ItemBalance : BalanceTable
    {
        public override string SheetName => "Items";

        [Tooltip("HP restored per apple, as a fraction of max HP (0.2 of 10 = +2). Capped at max HP")]
        [SerializeField] float healRatio = 0.2f;
        [Tooltip("An eaten apple comes back at the same point once the player is this far (straight line) from that point")]
        [SerializeField] float respawnDistance = 20f;
        [Tooltip("Pick-up distance between the player and the apple")]
        [SerializeField] float pickupRadius = 1.0f;
        [Tooltip("Apple height above the ground")]
        [SerializeField] float hoverHeight = 0.6f;
        [Tooltip("Up/down float height")]
        [SerializeField] float bobHeight = 0.12f;
        [Tooltip("Up/down float speed")]
        [SerializeField] float bobSpeed = 2.5f;
        [Tooltip("Spin, degrees per second")]
        [SerializeField] float spinSpeed = 90f;

        public float HealRatio => Mathf.Max(0f, healRatio);
        public float RespawnDistance => Mathf.Max(0f, respawnDistance);
        public float PickupRadius => Mathf.Max(0.1f, pickupRadius);
        public float HoverHeight => hoverHeight;
        public float BobHeight => Mathf.Max(0f, bobHeight);
        public float BobSpeed => bobSpeed;
        public float SpinSpeed => spinSpeed;
    }
}
```

#### `Scripts/Features/Items/AppleSpawner.cs` (신규)

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace FlyJam
{
    /// Red apples at fixed points (C02, A02).
    /// Each child transform in 'points' is one apple spot (only its direction from the planet centre matters; it is snapped to the surface).
    /// Touch -> +HP (HealRequested), apple disappears. It comes back at the same spot once the player is respawnDistance away from it.
    /// Put the first point near the start so new players heal before meeting danger (A02).
    public class AppleSpawner : MonoBehaviour
    {
        [SerializeField] ItemBalance balance;
        [SerializeField] GameObject applePrefab;
        [SerializeField] Transform[] points;
        [SerializeField] Planet planet;             // Auto-filled from GameContext if null
        [SerializeField] SphereWalker player;       // Auto-filled from GameContext if null
        [SerializeField] GameSession session;       // Auto-filled from GameContext if null

        class Spot
        {
            public Vector3 ground;
            public GameObject apple;
            public bool waiting;
        }

        readonly List<Spot> spots = new List<Spot>();

        public int ActiveCount
        {
            get
            {
                int n = 0;
                foreach (var s in spots) if (s.apple != null && s.apple.activeSelf) n++;
                return n;
            }
        }

        void Start()
        {
            var ctx = GameContext.Current;
            if (ctx != null)
            {
                if (planet == null) planet = ctx.Planet;
                if (player == null) player = ctx.Player;
                if (session == null) session = ctx.Session;
            }
            if (planet == null || applePrefab == null || points == null) return;

            foreach (var p in points)
            {
                if (p == null) continue;
                Vector3 dir = p.position - planet.transform.position;
                if (dir.sqrMagnitude < 1e-6f) continue;
                Vector3 ground = planet.SurfacePoint(dir.normalized);
                Vector3 up = planet.UpAt(ground);
                float hover = balance != null ? balance.HoverHeight : 0.6f;
                var apple = Instantiate(applePrefab, ground + up * hover, Quaternion.FromToRotation(Vector3.up, up), transform);
                var look = apple.GetComponent<AppleVisual>();
                if (look != null) look.Init(balance);
                spots.Add(new Spot { ground = ground, apple = apple });
            }
        }

        void Update()
        {
            if (player == null || session == null || session.Ended) return;
            if (session.IsPaused || player.State == FlyState.Disabled) return;

            float pick = balance != null ? balance.PickupRadius : 1f;
            float back = balance != null ? balance.RespawnDistance : 20f;
            Vector3 pp = player.transform.position;

            foreach (var s in spots)
            {
                if (s.apple == null) continue;
                if (!s.waiting)
                {
                    if (Vector3.Distance(pp, s.apple.transform.position) <= pick) Take(s);
                }
                else if (Vector3.Distance(pp, s.ground) >= back)
                {
                    s.waiting = false;
                    s.apple.SetActive(true);
                }
            }
        }

        void Take(Spot s)
        {
            s.waiting = true;
            s.apple.SetActive(false);
            GameEvents.RaiseHealRequested(balance != null ? balance.HealRatio : 0.2f);
            GameEvents.RaiseAppleEaten(s.apple.transform.position);
        }
    }
}
```

#### `Scripts/Features/Items/AppleVisual.cs` (신규)

```csharp
using UnityEngine;

namespace FlyJam
{
    /// Floats the apple model up and down and spins it slowly. Put on the Apple prefab root; 'visual' = the model child.
    public class AppleVisual : MonoBehaviour
    {
        [SerializeField] Transform visual;

        ItemBalance balance;
        Vector3 basePos;
        float phase;

        public void Init(ItemBalance b) => balance = b;

        void Awake()
        {
            if (visual == null && transform.childCount > 0) visual = transform.GetChild(0);
            if (visual != null) basePos = visual.localPosition;
            phase = Random.Range(0f, 10f);
        }

        void Update()
        {
            if (visual == null) return;
            float bob = balance != null ? balance.BobHeight : 0.12f;
            float speed = balance != null ? balance.BobSpeed : 2.5f;
            float spin = balance != null ? balance.SpinSpeed : 90f;
            visual.localPosition = basePos + Vector3.up * (Mathf.Sin((Time.time + phase) * speed) * bob);
            visual.Rotate(Vector3.up, spin * Time.deltaTime, Space.Self);
        }
    }
}
```

### 5-6. Features/Mate

#### `Scripts/Features/Mate/MateBalance.cs` (신규)

```csharp
using UnityEngine;

namespace FlyJam
{
    /// Planner numbers for the female (C03). Sheet tab "Mate".
    public class MateBalance : BalanceTable
    {
        public override string SheetName => "Mate";

        [Tooltip("Female speed = player max speed x this")]
        [SerializeField] float speedRatio = 0.5f;
        [Tooltip("She starts running away when the player is this close (straight line)")]
        [SerializeField] float fleeRadius = 5f;
        [Tooltip("She goes back to roaming when the player is this far (straight line). Keep it bigger than fleeRadius")]
        [SerializeField] float releaseRadius = 8f;
        [Tooltip("Seconds she keeps running after a failed courtship")]
        [SerializeField] float failFleeSeconds = 2f;
        [Tooltip("Roaming: seconds walking (min)")]
        [SerializeField] float walkTimeMin = 1.5f;
        [Tooltip("Roaming: seconds walking (max)")]
        [SerializeField] float walkTimeMax = 3f;
        [Tooltip("Roaming: seconds resting (min)")]
        [SerializeField] float pauseTimeMin = 0.4f;
        [Tooltip("Roaming: seconds resting (max)")]
        [SerializeField] float pauseTimeMax = 1.0f;
        [Tooltip("Roaming: random turn per walk, degrees")]
        [SerializeField] float wanderTurn = 70f;
        [Tooltip("If she is more than this many degrees around the planet from the player, her next walk heads back toward the player")]
        [SerializeField] float leashAngle = 100f;

        public float SpeedRatio => Mathf.Max(0f, speedRatio);
        public float FleeRadius => Mathf.Max(0f, fleeRadius);
        public float ReleaseRadius => Mathf.Max(FleeRadius + 0.5f, releaseRadius);
        public float FailFleeSeconds => Mathf.Max(0f, failFleeSeconds);
        public float WalkTimeMin => Mathf.Max(0.1f, walkTimeMin);
        public float WalkTimeMax => Mathf.Max(WalkTimeMin, walkTimeMax);
        public float PauseTimeMin => Mathf.Max(0f, pauseTimeMin);
        public float PauseTimeMax => Mathf.Max(PauseTimeMin, pauseTimeMax);
        public float WanderTurn => wanderTurn;
        public float LeashAngle => leashAngle;
    }
}
```

#### `Scripts/Features/Mate/MateAI.cs` (교체)

```csharp
using UnityEngine;

namespace FlyJam
{
    /// Female movement (C03): roams the planet (walk / short rest); runs away while the player is within fleeRadius,
    /// goes back to roaming beyond releaseRadius. Speed = player max speed x speedRatio.
    /// Freeze() during courtship; Flee() after a failed courtship.
    public class MateAI : MonoBehaviour, IMoveInput
    {
        public static MateAI Current { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Current = null;

        void OnEnable() => Current = this;

        void OnDisable()
        {
            if (Current == this) Current = null;
        }

        [SerializeField] SphereWalker self;
        [SerializeField] Transform player;
        [SerializeField] MateBalance balance;
        [SerializeField] float leashJitter = 30f;

        enum Mode { Pause, Walk, Flee, Frozen }
        Mode mode = Mode.Pause;
        float timer, forcedFlee;
        Vector3 heading;
        SphereWalker playerWalker;

        public bool IsFleeing => mode == Mode.Flee;

        void Awake()
        {
            if (self == null) self = GetComponent<SphereWalker>();
            var ctx = GameContext.Current;
            if (ctx != null && ctx.Player != null)
            {
                playerWalker = ctx.Player;
                if (player == null) player = ctx.Player.transform;
            }
        }

        void Start()
        {
            heading = transform.forward;
            timer = Random.Range(PauseMin, PauseMax);
        }

        float PauseMin => balance != null ? balance.PauseTimeMin : 0.4f;
        float PauseMax => balance != null ? balance.PauseTimeMax : 1f;

        void Update()
        {
            if (mode == Mode.Frozen || self == null || player == null) return;
            ApplySpeed();
            if (SessionPaused()) return;   // E04: mash battle / pause menu - she waits too

            if (forcedFlee > 0f) forcedFlee -= Time.deltaTime;
            float dist = Vector3.Distance(transform.position, player.position);
            float fleeR = balance != null ? balance.FleeRadius : 5f;
            float releaseR = balance != null ? balance.ReleaseRadius : 8f;
            bool flee = forcedFlee > 0f || (mode == Mode.Flee ? dist < releaseR : dist <= fleeR);

            if (flee)
            {
                mode = Mode.Flee;
                return;
            }
            if (mode == Mode.Flee)
            {
                mode = Mode.Pause;   // back to roaming after a short rest
                timer = PauseMin;
            }

            timer -= Time.deltaTime;
            if (timer > 0f) return;

            if (mode == Mode.Pause)
            {
                mode = Mode.Walk;
                timer = Random.Range(balance != null ? balance.WalkTimeMin : 1.5f, balance != null ? balance.WalkTimeMax : 3f);

                Vector3 up = self.Planet.UpAt(transform.position);
                Vector3 playerUp = self.Planet.UpAt(player.position);
                Vector3 baseDir;
                float jitter;
                if (Vector3.Angle(up, playerUp) > (balance != null ? balance.LeashAngle : 100f))
                {
                    baseDir = Vector3.ProjectOnPlane(player.position - transform.position, up);
                    jitter = leashJitter;
                }
                else
                {
                    baseDir = Vector3.ProjectOnPlane(transform.forward, up);
                    jitter = balance != null ? balance.WanderTurn : 70f;
                }
                heading = Quaternion.AngleAxis(Random.Range(-jitter, jitter), up) * baseDir;
            }
            else
            {
                mode = Mode.Pause;
                timer = Random.Range(PauseMin, PauseMax);
            }
        }

        static bool SessionPaused()
        {
            var ctx = GameContext.Current;
            return ctx != null && ctx.Session != null && ctx.Session.IsPaused;
        }

        void ApplySpeed()
        {
            float playerMax = playerWalker != null ? playerWalker.MoveSpeed : 6f;
            float ratio = balance != null ? balance.SpeedRatio : 0.5f;
            self.SpeedMultiplier = playerMax * ratio / Mathf.Max(0.01f, self.MoveSpeed);
        }

        public Vector2 ReadMove()
        {
            if (mode == Mode.Pause || mode == Mode.Frozen || player == null || SessionPaused()) return Vector2.zero;
            Vector3 up = self.Planet.UpAt(transform.position);
            Vector3 desired = mode == Mode.Flee ? transform.position - player.position : heading;
            desired = Vector3.ProjectOnPlane(desired, up);
            if (desired.sqrMagnitude < 1e-6f) return Vector2.zero;
            desired.Normalize();
            heading = desired;

            Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, up).normalized;
            Vector3 right = Vector3.Cross(up, fwd);
            return new Vector2(Vector3.Dot(desired, right), Vector3.Dot(desired, fwd));
        }

        public bool ReadDodge() => false;

        public void Freeze(bool on)
        {
            mode = on ? Mode.Frozen : Mode.Pause;
            timer = Random.Range(PauseMin, PauseMax);
        }

        /// Run away for failFleeSeconds even if the player is not close (failed courtship).
        public void Flee()
        {
            forcedFlee = balance != null ? balance.FailFleeSeconds : 2f;
            mode = Mode.Flee;
        }
    }
}
```

#### `Scripts/Features/Mate/MateGoal.cs` (교체)

```csharp
using UnityEngine;

namespace FlyJam
{
    /// Raises MateTouched when the player reaches the female.
    /// After a courtship ends (Rearm), the player must first move rearmDistance away: no automatic restart (E03, E04).
    public class MateGoal : MonoBehaviour
    {
        [SerializeField] Transform player;
        [SerializeField] float reachDistance = 0.9f;
        [SerializeField] float rearmDistance = 2.5f;

        bool armed = true;
        bool waitForSeparation;
        GameSession session;

        void Awake()
        {
            var ctx = GameContext.Current;
            if (ctx != null)
            {
                if (player == null && ctx.Player != null) player = ctx.Player.transform;
                session = ctx.Session;
            }
        }

        void Update()
        {
            if (player == null) return;
            if (session != null && (session.Ended || session.IsPaused)) return;

            float d = Vector3.Distance(player.position, transform.position);
            if (waitForSeparation)
            {
                if (d > rearmDistance)
                {
                    waitForSeparation = false;
                    armed = true;
                }
                return;
            }
            if (armed && d <= reachDistance)
            {
                armed = false;
                GameEvents.RaiseMateTouched();
            }
        }

        public void Rearm()
        {
            armed = false;
            waitForSeparation = true;
        }
    }
}
```

#### `Scripts/Features/Mate/MateFinderHint.cs` (교체)

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace FlyJam
{
    /// Discovery mark (A03, C03): a heart above the female's head while she is on screen and not too far.
    /// (Class name kept from the prototype's direction hint so the SYS_Mate prefab link survives.)
    public class MateFinderHint : MonoBehaviour
    {
        [SerializeField] Camera worldCamera;        // Auto-filled from GameContext if null
        [SerializeField] SphereWalker player;       // Auto-filled from GameContext if null
        [SerializeField] Transform mate;            // Auto-filled from MateAI.Current if null
        [SerializeField] GameSession session;       // Auto-filled from GameContext if null
        [SerializeField] RawImage icon;
        [SerializeField] float headHeight = 0.9f;
        [SerializeField] float maxDistance = 16f;
        [SerializeField] float size = 44f;
        [SerializeField] float bobPixels = 6f;
        [SerializeField] float fadeSpeed = 8f;

        float alpha;

        void Awake()
        {
            icon.texture = PixelHeart.Get();
            icon.raycastTarget = false;
            SetAlpha(0f);
        }

        void Start()
        {
            var ctx = GameContext.Current;
            if (ctx == null) return;
            if (worldCamera == null) worldCamera = ctx.WorldCamera;
            if (player == null) player = ctx.Player;
            if (session == null) session = ctx.Session;
        }

        void LateUpdate()
        {
            if (mate == null && MateAI.Current != null) mate = MateAI.Current.transform;
            if (worldCamera == null || player == null || session == null || mate == null)
            {
                SetAlpha(0f);
                return;
            }

            Vector3 up = player.Planet.UpAt(mate.position);
            Vector3 vp = worldCamera.WorldToViewportPoint(mate.position + up * headHeight);
            bool onScreen = vp.z > 0f && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;
            bool near = Vector3.Distance(player.transform.position, mate.position) <= maxDistance;
            bool want = onScreen && near && !session.Ended;

            alpha = Mathf.MoveTowards(alpha, want ? 1f : 0f, fadeSpeed * Time.unscaledDeltaTime);
            SetAlpha(alpha);
            if (alpha <= 0f) return;

            float bob = Mathf.Sin(Time.unscaledTime * 5f) * bobPixels;
            icon.rectTransform.position = new Vector3(vp.x * Screen.width, vp.y * Screen.height + bob, 0f);
            icon.rectTransform.sizeDelta = new Vector2(size, size * 6f / 7f);
        }

        void SetAlpha(float a)
        {
            var c = icon.color;
            c.a = a;
            icon.color = c;
            icon.enabled = a > 0.001f;
        }
    }
}
```

### 5-7. Features/Courtship

#### `Scripts/Features/Courtship/FlyJam.Courtship.asmdef` (교체)

```json
{
    "name": "FlyJam.Courtship",
    "rootNamespace": "FlyJam",
    "references": [
        "FlyJam.Core",
        "FlyJam.Mate",
        "Unity.TextMeshPro",
        "UnityEngine.UI"
    ],
    "autoReferenced": true
}
```

#### `Scripts/Features/Courtship/CourtshipBalance.cs` (신규)

```csharp
using UnityEngine;

namespace FlyJam
{
    /// Planner numbers for the courtship button order (S06, P03, E03). Sheet tab "Courtship".
    public class CourtshipBalance : BalanceTable
    {
        public override string SheetName => "Courtship";

        [Tooltip("How many buttons to press in order")]
        [SerializeField] int length = 4;
        [Tooltip("How many different buttons can appear (1-4). 4 = A/B/X/Y")]
        [SerializeField] int buttonKinds = 4;
        [Tooltip("Seconds to finish the whole order. 0 = no limit")]
        [SerializeField] float timeLimit = 0f;
        [Tooltip("Presses in the first moment are ignored (presses from before the event do not count)")]
        [SerializeField] float inputDelay = 0.3f;
        [Tooltip("Seconds the success / failure message stays")]
        [SerializeField] float resultShowSeconds = 1.2f;

        public int Length => Mathf.Clamp(length, 1, 8);
        public int ButtonKinds => Mathf.Clamp(buttonKinds, 1, 4);
        public float TimeLimit => Mathf.Max(0f, timeLimit);
        public float InputDelay => Mathf.Max(0f, inputDelay);
        public float ResultShowSeconds => Mathf.Max(0f, resultShowSeconds);
    }
}
```

#### `Scripts/Features/Courtship/ButtonSequence.cs` (신규)

```csharp
using System.Collections.Generic;

namespace FlyJam
{
    /// A button order to enter (plain logic, no Unity objects; reusable later for a rival command battle).
    public sealed class ButtonSequence
    {
        public enum Step { Correct, Wrong, Completed }

        readonly FaceButton[] buttons;

        public int Index { get; private set; }
        public int Length => buttons.Length;
        public IReadOnlyList<FaceButton> Buttons => buttons;
        public bool IsComplete => Index >= buttons.Length;
        public FaceButton Next => buttons[IsComplete ? buttons.Length - 1 : Index];

        public ButtonSequence(FaceButton[] order)
        {
            buttons = order != null && order.Length > 0 ? order : new[] { FaceButton.East };
        }

        /// Random order. With 2+ kinds the same button never appears twice in a row (easier to read).
        public static ButtonSequence Random(int length, int kinds)
        {
            if (length < 1) length = 1;
            if (kinds < 1) kinds = 1;
            if (kinds > 4) kinds = 4;
            var order = new FaceButton[length];
            for (int i = 0; i < length; i++)
            {
                FaceButton b;
                do b = (FaceButton)UnityEngine.Random.Range(0, kinds);
                while (kinds > 1 && i > 0 && b == order[i - 1]);
                order[i] = b;
            }
            return new ButtonSequence(order);
        }

        public Step Submit(FaceButton pressed)
        {
            if (IsComplete) return Step.Completed;
            if (pressed != buttons[Index]) return Step.Wrong;
            Index++;
            return IsComplete ? Step.Completed : Step.Correct;
        }
    }
}
```

#### `Scripts/Features/Courtship/CourtshipQTE.cs` (교체)

```csharp
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlyJam
{
    /// Courtship panel (S06, P03, E03, A05): press the shown buttons in order.
    /// All correct -> success. One wrong button (or the time limit, if set) -> failure.
    /// Each correct press plays a rising note, so it still feels like the courtship song.
    /// (Class name kept from the prototype's rhythm QTE so CourtshipFlow and the prefab links stay valid.)
    public class CourtshipQTE : MonoBehaviour
    {
        [SerializeField] CourtshipBalance balance;
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text titleText;          // "順番にボタンを押そう！" -> result title
        [SerializeField] TMP_Text[] slots;            // button boxes left to right (8 boxes; extra ones are hidden)
        [SerializeField] GameObject[] slotFrames;     // highlight frame for the next button (same index as slots)
        [SerializeField] TMP_Text infoText;           // "1／4　次はX" -> failure reason
        [SerializeField] Image timerFill;             // optional, Image Type = Filled. Hidden when there is no limit
        [SerializeField] Color doneColor = new Color(0.55f, 0.35f, 0.55f, 1f);
        [SerializeField] Color nextColor = new Color(0.35f, 0.1f, 0.2f, 1f);
        [SerializeField] Color todoColor = new Color(0.45f, 0.45f, 0.45f, 1f);

        const string DoneMark = "♪";

        AudioSource audioSource;
        AudioClip wrongClip;

        public bool Running { get; private set; }

        void Awake()
        {
            if (panel != null) panel.SetActive(false);
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            wrongClip = MakeBeep(220f, 0.25f);
        }

        public void Begin(Action<bool> onDone)
        {
            if (Running) return;
            StartCoroutine(Run(onDone));
        }

        IEnumerator Run(Action<bool> onDone)
        {
            Running = true;
            int length = balance != null ? balance.Length : 4;
            int kinds = balance != null ? balance.ButtonKinds : 4;
            float limit = balance != null ? balance.TimeLimit : 0f;
            float wait = balance != null ? balance.InputDelay : 0.3f;
            float left = limit;

            var seq = ButtonSequence.Random(length, kinds);
            panel.SetActive(true);
            titleText.text = Loc.Get("court.title");
            if (timerFill != null) timerFill.gameObject.SetActive(limit > 0f);
            Refresh(seq);
            GameEvents.RaiseCourtshipStarted();

            bool success = false;
            string failKey = null;
            while (true)
            {
                yield return null;
                Refresh(seq);                                // button names follow the device in use
                if (TimeControl.InputBlocked) continue;      // pause menu open
                float dt = Time.deltaTime;
                if (wait > 0f) { wait -= dt; continue; }

                if (limit > 0f)
                {
                    left -= dt;
                    if (timerFill != null) timerFill.fillAmount = Mathf.Clamp01(left / limit);
                    if (left <= 0f) { failKey = "court.fail.time"; break; }
                }

                if (!InputCompat.FaceButtonPressed(out var pressed)) continue;
                var step = seq.Submit(pressed);
                if (step == ButtonSequence.Step.Wrong)
                {
                    GameEvents.RaiseCourtshipInput(false);
                    audioSource.PlayOneShot(wrongClip);
                    failKey = "court.fail.wrong";
                    break;
                }
                GameEvents.RaiseCourtshipInput(true);
                audioSource.PlayOneShot(MakeBeep(660f * Mathf.Pow(2f, seq.Index / 6f), 0.12f));
                if (step == ButtonSequence.Step.Completed)
                {
                    success = true;
                    break;
                }
            }

            Refresh(seq);
            if (timerFill != null) timerFill.gameObject.SetActive(false);
            titleText.text = Loc.Get(success ? "court.success" : "court.fail.title");
            infoText.text = success ? "" : Loc.Get(failKey);

            float show = balance != null ? balance.ResultShowSeconds : 1.2f;
            while (show > 0f)
            {
                yield return null;
                if (!TimeControl.Paused) show -= Time.deltaTime;
            }

            panel.SetActive(false);
            Running = false;
            onDone?.Invoke(success);
        }

        void Refresh(ButtonSequence seq)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                bool used = i < seq.Length;
                if (slots[i] != null)
                {
                    slots[i].transform.parent.gameObject.SetActive(used);
                    if (used)
                    {
                        bool done = i < seq.Index;
                        slots[i].text = done ? DoneMark : InputCompat.Label(seq.Buttons[i]);
                        slots[i].color = done ? doneColor : i == seq.Index ? nextColor : todoColor;
                    }
                }
                if (slotFrames != null && i < slotFrames.Length && slotFrames[i] != null)
                    slotFrames[i].SetActive(used && i == seq.Index && !seq.IsComplete);
            }
            if (!seq.IsComplete)
                infoText.text = Loc.Format("court.progress", seq.Index, seq.Length, InputCompat.Label(seq.Next));
        }

        static AudioClip MakeBeep(float freq, float duration)
        {
            const int sampleRate = 44100;
            int n = Mathf.Max(1, (int)(sampleRate * duration));
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float env = 1f - (float)i / n;
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * i / sampleRate) * 0.25f * env;
            }
            var clip = AudioClip.Create("beep", n, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
```

### 5-8. Features/Hazards

#### `Scripts/Features/Hazards/HazardBalance.cs` (교체)

```csharp
using UnityEngine;

namespace FlyJam
{
    /// Planner numbers for enemies (C04-C08, E01, E02). Sheet tabs: "Hazards" (key/value) and "HazardTypes" (one row per enemy type).
    public class HazardBalance : BalanceTable
    {
        public override string SheetName => "Hazards";

        [System.Serializable]
        public struct TypeRow
        {
            public string id;            // must match an id in ObstacleSpawner.prefabs (dragonfly, spider, male, acorn)
            public int count;            // how many of this type circle the planet
            public float speedRatio;     // speed = player max speed x this
            public float damageRatio;    // HP lost on contact, fraction of max HP (ignored when rival = 1)
            public bool warn;            // 1 = approaching triggers the warning (red frame, dodge allowed)
            public bool rival;           // 1 = contact starts the mash battle instead of damage (male)
            public bool chase;           // 1 = turns toward the player when close
            public float detectRadius;   // chase starts within this straight-line distance
            public float loseRadius;     // chase stops beyond this straight-line distance
            public float turnRate;       // degrees per second while chasing
            public float stopOnHit;      // seconds it stays put after touching the player (harmless meanwhile). 0 = passes through
        }

        [Tooltip("Seconds before the first enemy appears")]
        [SerializeField] float firstDelay = 2f;
        [Tooltip("Enemies appear one by one, this many seconds apart, until every type reaches its count")]
        [SerializeField] float spawnInterval = 1.5f;
        [Tooltip("Spawn point: at least this many degrees around the planet away from the player")]
        [SerializeField] float spawnAngleMin = 50f;
        [Tooltip("Spawn point: at most this many degrees away (180 = the opposite side)")]
        [SerializeField] float spawnAngleMax = 170f;
        [Tooltip("Warning: an approaching enemy within this distance (straight line)")]
        [SerializeField] float warnRadius = 3f;
        [Tooltip("Warning: how directly it must be heading at the player (1 = exactly, 0 = sideways)")]
        [SerializeField] float warnApproachDot = 0.5f;
        [Tooltip("Game speed during a warning (slow motion). 1 = off")]
        [SerializeField] float slowScale = 0.5f;
        [Tooltip("Seconds after a dodge to wait for the enemy to pass before giving up on 'dodge success'")]
        [SerializeField] float successTimeout = 3f;

        [SheetRows("HazardTypes")]
        [SerializeField] TypeRow[] types =
        {
            new TypeRow { id = "dragonfly", count = 2, speedRatio = 0.7f, damageRatio = 0.2f, warn = true, stopOnHit = 2f },
            new TypeRow { id = "spider",    count = 2, speedRatio = 0.7f, damageRatio = 0.2f, warn = true, stopOnHit = 2f },
            new TypeRow { id = "male",      count = 1, speedRatio = 0.7f, damageRatio = 0f,   warn = true, rival = true,
                          chase = true, detectRadius = 6f, loseRadius = 9f, turnRate = 120f, stopOnHit = 2f },
            new TypeRow { id = "acorn",     count = 1, speedRatio = 0.7f, damageRatio = 0.1f, warn = true },
        };

        public float FirstDelay => Mathf.Max(0f, firstDelay);
        public float SpawnInterval => Mathf.Max(0.1f, spawnInterval);
        public float SpawnAngleMin => Mathf.Clamp(spawnAngleMin, 0f, 180f);
        public float SpawnAngleMax => Mathf.Clamp(Mathf.Max(spawnAngleMin, spawnAngleMax), 0f, 180f);
        public float WarnRadius => Mathf.Max(0.5f, warnRadius);
        public float WarnApproachDot => Mathf.Clamp(warnApproachDot, -1f, 1f);
        public float SlowScale => Mathf.Clamp(slowScale, 0.05f, 1f);
        public float SuccessTimeout => Mathf.Max(0.1f, successTimeout);
        public TypeRow[] Types => types;
    }
}
```

#### `Scripts/Features/Hazards/RollingObstacle.cs` (교체)

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace FlyJam
{
    /// Enemy / obstacle that circles the planet along its heading (a great circle), passing through props like trees (C04-C08).
    /// Per prefab: the look (rolling ball, or a creature facing its path with an optional hover bob).
    /// Per type row (HazardBalance.TypeRow from ObstacleSpawner.Launch): damage, warning, chase, stop-on-hit, rival.
    /// Freezes while the session is paused (courtship, mash battle, pause menu) - E04.
    public class RollingObstacle : MonoBehaviour, IMovingObstacle, IDamageSource
    {
        static readonly List<RollingObstacle> active = new List<RollingObstacle>();
        static readonly List<RollingObstacle> alive = new List<RollingObstacle>();

        /// Dangerous right now (moving, can hit). Warning and reaction tracking use this.
        public static IReadOnlyList<RollingObstacle> Active => active;
        /// Every spawned instance, including ones stopped after a hit. The spawner counts these.
        public static IReadOnlyList<RollingObstacle> Alive => alive;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            active.Clear();
            alive.Clear();
        }

        [SerializeField] Transform visual;
        [SerializeField] float radius = 0.4f;
        [Tooltip("Spin the visual like a rolling ball (acorn). Off = a creature that faces its path")]
        [SerializeField] bool rollVisual = true;
        [Tooltip("Hover bob height of the visual (dragonfly). 0 = none")]
        [SerializeField] float bobHeight = 0f;
        [SerializeField] float bobSpeed = 3f;

        Planet planet;
        Transform target;
        HazardBalance.TypeRow row;
        bool hasRow;
        Vector3 dir, visualBase;
        float speed, travelled, maxTravel, stopLeft, bobPhase;
        bool chasing, dangerous;

        public Vector3 Position => transform.position;
        public float HitRadius => radius;
        public bool IsMoving => stopLeft <= 0f;
        public Vector3 Direction => dir;
        public bool IsChasing => chasing;
        public string TypeId => hasRow ? row.id : "";
        /// Approaching this one triggers the warning (E01).
        public bool Warns => !hasRow || row.warn;
        public bool IsRival => hasRow && row.rival;
        /// Fraction of max HP lost on contact. -1 = let Health use its default. 0 for the male (mash battle instead).
        public float DamageRatio => !hasRow ? -1f : row.rival ? 0f : row.damageRatio;

        /// Old signature (no type row): behaves like the original acorn.
        public void Launch(Planet p, Vector3 surfacePos, Vector3 direction, float spd, float maxDistance)
        {
            Launch(p, surfacePos, direction, spd, maxDistance, default, null);
            hasRow = false;
        }

        /// maxDistance <= 0: never disappears (keeps circling the planet).
        public void Launch(Planet p, Vector3 surfacePos, Vector3 direction, float spd, float maxDistance,
                           HazardBalance.TypeRow type, Transform chaseTarget)
        {
            planet = p;
            speed = spd;
            maxTravel = maxDistance;
            row = type;
            hasRow = true;
            target = chaseTarget;
            transform.position = p.SnapToSurface(surfacePos, radius);
            Vector3 up = p.UpAt(transform.position);
            dir = Vector3.ProjectOnPlane(direction, up).normalized;
            transform.rotation = Quaternion.LookRotation(dir, up);
        }

        void Awake()
        {
            if (visual != null) visualBase = visual.localPosition;
            bobPhase = Random.Range(0f, 10f);
        }

        void OnEnable()
        {
            alive.Add(this);
            SetDangerous(true);
            GameEvents.PlayerHitBy += OnPlayerHitBy;
        }

        void OnDisable()
        {
            alive.Remove(this);
            SetDangerous(false);
            GameEvents.PlayerHitBy -= OnPlayerHitBy;
        }

        void SetDangerous(bool on)
        {
            if (dangerous == on) return;
            dangerous = on;
            if (on)
            {
                active.Add(this);
                ObstacleRegistry.Register(this);
            }
            else
            {
                active.Remove(this);
                ObstacleRegistry.Unregister(this);
            }
        }

        void OnPlayerHitBy(IObstacle source)
        {
            if (!ReferenceEquals(source, this)) return;
            if (IsRival) GameEvents.RaiseRivalEncounter(this);   // male: mash battle (Rival feature)
            if (row.stopOnHit <= 0f) return;
            stopLeft = row.stopOnHit;
            chasing = false;
            SetDangerous(false);   // harmless while stopped: no hit, no warning
        }

        void Update()
        {
            if (planet == null) return;
            var ctx = GameContext.Current;
            if (ctx != null && ctx.Session != null && ctx.Session.IsPaused) return;   // E04
            Bob();

            if (stopLeft > 0f)
            {
                stopLeft -= Time.deltaTime;
                if (stopLeft <= 0f) SetDangerous(true);
                return;
            }

            Vector3 up = planet.UpAt(transform.position);
            if (row.chase && target != null) Steer(up);

            float step = speed * Time.deltaTime;
            transform.position = planet.SnapToSurface(transform.position + dir * step, radius);
            up = planet.UpAt(transform.position);
            dir = Vector3.ProjectOnPlane(dir, up).normalized;
            transform.rotation = Quaternion.LookRotation(dir, up);

            if (rollVisual && visual != null)
                visual.Rotate(Vector3.Cross(up, dir), step / radius * Mathf.Rad2Deg, Space.World);

            if (maxTravel > 0f)
            {
                travelled += step;
                if (travelled >= maxTravel) Destroy(gameObject);
            }
        }

        void Steer(Vector3 up)
        {
            float dist = Vector3.Distance(transform.position, target.position);
            if (!chasing && dist <= row.detectRadius) chasing = true;
            else if (chasing && dist > row.loseRadius) chasing = false;
            if (!chasing) return;

            Vector3 want = Vector3.ProjectOnPlane(target.position - transform.position, up);
            if (want.sqrMagnitude < 1e-6f) return;
            dir = Vector3.RotateTowards(dir, want.normalized, row.turnRate * Mathf.Deg2Rad * Time.deltaTime, 0f);
        }

        void Bob()
        {
            if (visual == null || bobHeight <= 0f) return;
            float y = Mathf.Sin((Time.time + bobPhase) * bobSpeed) * bobHeight;
            visual.localPosition = visualBase + Vector3.up * y;
        }
    }
}
```

#### `Scripts/Features/Hazards/ObstacleSpawner.cs` (교체)

```csharp
using UnityEngine;

namespace FlyJam
{
    /// Keeps a fixed population of enemies circling the planet (C04-C08).
    /// After firstDelay, one enemy appears every spawnInterval until each HazardTypes row reaches its 'count'.
    /// Each one starts 50-170 degrees away from the player with a random heading and never disappears.
    public class ObstacleSpawner : MonoBehaviour
    {
        [System.Serializable]
        public struct TypePrefab
        {
            public string id;
            public RollingObstacle prefab;
        }

        [SerializeField] Planet planet;             // Auto-filled from GameContext if null
        [SerializeField] SphereWalker player;       // Auto-filled from GameContext if null
        [SerializeField] GameSession session;       // Auto-filled from GameContext if null
        [SerializeField] HazardBalance balance;
        [SerializeField] TypePrefab[] prefabs;

        float timer;

        void Start()
        {
            var ctx = GameContext.Current;
            if (ctx != null)
            {
                if (planet == null) planet = ctx.Planet;
                if (player == null) player = ctx.Player;
                if (session == null) session = ctx.Session;
            }
            timer = balance != null ? balance.FirstDelay : 2f;

            if (balance != null)
                foreach (var t in balance.Types)
                    if (t.count > 0 && FindPrefab(t.id) == null)
                        Debug.LogWarning($"[ObstacleSpawner] no prefab for enemy type '{t.id}' - it will not spawn");
        }

        void Update()
        {
            if (planet == null || player == null || session == null || balance == null) return;
            if (session.Ended || session.IsPaused) return;
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = balance.SpawnInterval;

            if (PickShortType(out var row, out var prefab)) Spawn(row, prefab);
        }

        /// The type with the biggest shortage (count - alive). False when every type is full.
        bool PickShortType(out HazardBalance.TypeRow row, out RollingObstacle prefab)
        {
            row = default;
            prefab = null;
            int best = 0;
            foreach (var t in balance.Types)
            {
                var p = FindPrefab(t.id);
                if (p == null || t.count <= 0) continue;
                int shortage = t.count - AliveOf(t.id);
                if (shortage > best)
                {
                    best = shortage;
                    row = t;
                    prefab = p;
                }
            }
            return prefab != null;
        }

        static int AliveOf(string id)
        {
            int n = 0;
            var all = RollingObstacle.Alive;
            for (int i = 0; i < all.Count; i++)
                if (all[i] != null && string.Equals(all[i].TypeId, id, System.StringComparison.OrdinalIgnoreCase)) n++;
            return n;
        }

        void Spawn(HazardBalance.TypeRow row, RollingObstacle prefab)
        {
            Vector3 up = planet.UpAt(player.transform.position);
            Vector3 baseFwd = Vector3.ProjectOnPlane(player.transform.forward, up).normalized;
            Vector3 tangent = Quaternion.AngleAxis(Random.Range(0f, 360f), up) * baseFwd;
            Vector3 axis = Vector3.Cross(up, tangent);
            Vector3 spawnDir = Quaternion.AngleAxis(Random.Range(balance.SpawnAngleMin, balance.SpawnAngleMax), axis) * up;

            Vector3 spawnPos = planet.SurfacePoint(spawnDir);
            Vector3 sUp = planet.UpAt(spawnPos);
            Vector3 anyTangent = Vector3.ProjectOnPlane(Mathf.Abs(sUp.y) < 0.9f ? Vector3.up : Vector3.forward, sUp).normalized;
            Vector3 heading = Quaternion.AngleAxis(Random.Range(0f, 360f), sUp) * anyTangent;

            var o = Instantiate(prefab);
            o.Launch(planet, spawnPos, heading, player.MoveSpeed * row.speedRatio, 0f, row, player.transform);
        }

        RollingObstacle FindPrefab(string id)
        {
            if (prefabs == null || string.IsNullOrEmpty(id)) return null;
            foreach (var p in prefabs)
                if (p.prefab != null && string.Equals(p.id, id, System.StringComparison.OrdinalIgnoreCase)) return p.prefab;
            return null;
        }
    }
}
```

#### `Scripts/Features/Hazards/NearMissDirector.cs` (교체)

```csharp
using TMPro;
using UnityEngine;

namespace FlyJam
{
    /// Danger warning and dodge result (E01, E02, A04).
    /// Warning = the nearest enemy that is heading at the player within warnRadius. While warned:
    /// red frame on the screen edges, "Ⓐボタンで回避！" prompt, slow motion, and dodging is allowed (DodgeAbility listens to WarningChanged).
    /// Success = after a dodge, that enemy leaves the danger range without touching the player -> "回避!" once.
    /// (Class name kept from the prototype so the SYS_Hazards prefab link survives.)
    public class NearMissDirector : MonoBehaviour
    {
        public const string PerfectKey = "dodge.perfect";   // result stat: number of successful dodges

        [SerializeField] HazardBalance balance;
        [SerializeField] SphereWalker player;        // Auto-filled from GameContext if null
        [SerializeField] GameSession session;        // Auto-filled from GameContext if null
        [SerializeField] GameObject promptUI;        // top centre prompt root
        [SerializeField] TMP_Text promptText;        // "Ⓐボタンで回避！" (set from code: button name follows the device)
        [SerializeField] GameObject frameUI;         // red frame on the four screen edges
        [SerializeField] TMP_Text perfectText;       // "回避!" after a successful dodge
        [SerializeField] float successShowSeconds = 0.8f;

        RollingObstacle target, dodged;
        float dodgedTimer, perfectUntil;

        public int PerfectCount { get; private set; }
        public RollingObstacle Target => target;

        void OnEnable()
        {
            GameEvents.PlayerDodged += OnDodged;
            GameEvents.PlayerHitBy += OnHitBy;
            GameEvents.RoundEnded += OnRoundEnded;
        }

        void OnDisable()
        {
            GameEvents.PlayerDodged -= OnDodged;
            GameEvents.PlayerHitBy -= OnHitBy;
            GameEvents.RoundEnded -= OnRoundEnded;
            SetTarget(null);
            TimeControl.SetSlow(this, 1f);
        }

        void Start()
        {
            var ctx = GameContext.Current;
            if (ctx != null)
            {
                if (player == null) player = ctx.Player;
                if (session == null) session = ctx.Session;
            }
            if (promptUI != null) promptUI.SetActive(false);
            if (frameUI != null) frameUI.SetActive(false);
            if (perfectText != null) perfectText.enabled = false;
        }

        void Update()
        {
            bool live = player != null && session != null && balance != null && !session.Ended && !session.IsPaused
                        && player.InputEnabled && player.State != FlyState.Disabled;

            var next = live ? FindThreat() : null;
            if (next != target) SetTarget(next);
            if (target != null && promptText != null)
                promptText.text = Loc.Format("hazard.prompt", InputCompat.ActionLabel);

            if (live) UpdateSuccess();
            if (perfectText != null) perfectText.enabled = Time.unscaledTime < perfectUntil;
        }

        RollingObstacle FindThreat()
        {
            RollingObstacle best = null;
            float bestDist = balance.WarnRadius;
            var all = RollingObstacle.Active;
            for (int i = 0; i < all.Count; i++)
            {
                var o = all[i];
                if (o == null || !o.Warns) continue;
                float d = Vector3.Distance(player.transform.position, o.Position);
                if (d > bestDist || !Approaching(o)) continue;
                best = o;
                bestDist = d;
            }
            return best;
        }

        bool Approaching(RollingObstacle o)
        {
            Vector3 to = player.transform.position - o.Position;
            return to.sqrMagnitude > 1e-6f && Vector3.Dot(o.Direction, to.normalized) > balance.WarnApproachDot;
        }

        bool InDanger(RollingObstacle o)
        {
            if (o == null) return false;
            bool activeNow = false;
            var all = RollingObstacle.Active;
            for (int i = 0; i < all.Count; i++) if (all[i] == o) { activeNow = true; break; }
            return activeNow && Vector3.Distance(player.transform.position, o.Position) <= balance.WarnRadius && Approaching(o);
        }

        void SetTarget(RollingObstacle t)
        {
            target = t;
            GameEvents.RaiseWarningChanged(t);
            if (promptUI != null) promptUI.SetActive(t != null);
            if (frameUI != null) frameUI.SetActive(t != null);
            TimeControl.SetSlow(this, t != null && balance != null ? balance.SlowScale : 1f);
        }

        void OnDodged(Vector3 pos, Vector3 dir)
        {
            if (target == null) return;
            dodged = target;
            dodgedTimer = balance != null ? balance.SuccessTimeout : 3f;
        }

        void OnHitBy(IObstacle src)
        {
            if (dodged != null && ReferenceEquals(src, dodged)) dodged = null;   // touched it: no success
        }

        void UpdateSuccess()
        {
            if (dodged == null) return;
            dodgedTimer -= Time.deltaTime;
            if (!InDanger(dodged))
            {
                dodged = null;
                PerfectCount++;
                perfectUntil = Time.unscaledTime + successShowSeconds;
                GameEvents.RaiseDodgeSucceeded();
                GameEvents.RaisePerfectDodge();   // existing FX burst
            }
            else if (dodgedTimer <= 0f) dodged = null;
        }

        void OnRoundEnded(bool cleared) => SessionResult.SetStat(PerfectKey, PerfectCount);
    }
}
```

#### `Scripts/Features/Hazards/ReactionTracker.cs` (교체)

```csharp
using UnityEngine;

namespace FlyJam
{
    /// Reaction time for the result panel (FLY FACT comparison): seconds from the danger warning appearing
    /// (WarningChanged with a target) to the dodge press. Measured in real time, so slow motion does not shorten it.
    /// Enemies now circle forever, so it measures per warning, not per enemy.
    public class ReactionTracker : MonoBehaviour
    {
        public const string BestKey = "reaction.best";
        public const string AvgKey = "reaction.avg";
        public const string CountKey = "reaction.count";

        float warnedAt = -1f;   // < 0: no warning waiting for a dodge
        float sum;

        public int Count { get; private set; }
        public float Best { get; private set; } = -1f;
        public float Average => Count > 0 ? sum / Count : -1f;

        void OnEnable()
        {
            GameEvents.WarningChanged += OnWarning;
            GameEvents.PlayerDodged += OnDodged;
            GameEvents.RoundEnded += OnRoundEnded;
        }

        void OnDisable()
        {
            GameEvents.WarningChanged -= OnWarning;
            GameEvents.PlayerDodged -= OnDodged;
            GameEvents.RoundEnded -= OnRoundEnded;
        }

        void OnWarning(IMovingObstacle target)
        {
            if (target == null) { warnedAt = -1f; return; }
            if (warnedAt < 0f) warnedAt = Time.unscaledTime;   // a new target while still warned keeps the first time
        }

        void OnDodged(Vector3 pos, Vector3 dir)
        {
            if (warnedAt < 0f) return;
            float rt = Time.unscaledTime - warnedAt;
            warnedAt = -1f;   // one measurement per warning
            Count++;
            sum += rt;
            if (Best < 0f || rt < Best) Best = rt;
            GameEvents.RaiseReactionMeasured(rt);
        }

        void OnRoundEnded(bool cleared)
        {
            if (Count <= 0) return;
            SessionResult.SetStat(BestKey, Best);
            SessionResult.SetStat(AvgKey, Average);
            SessionResult.SetStat(CountKey, Count);
        }
    }
}
```

### 5-9. Features/Dodge

#### `Scripts/Features/Dodge/DodgeBalance.cs` (신규)

```csharp
using UnityEngine;

namespace FlyJam
{
    /// Planner numbers for the dodge (P04). Sheet tab "Dodge".
    public class DodgeBalance : BalanceTable
    {
        public override string SheetName => "Dodge";

        [Tooltip("Dodge distance")]
        [SerializeField] float distance = 2.2f;
        [Tooltip("Dodge duration (seconds). Invulnerable during it")]
        [SerializeField] float duration = 0.22f;
        [Tooltip("Seconds before the next dodge")]
        [SerializeField] float cooldown = 0.4f;
        [Tooltip("Body turn added during a sideways dodge, degrees")]
        [SerializeField] float yawKick = 25f;
        [Tooltip("Stick tilt below this counts as neutral")]
        [SerializeField] float steerDeadZone = 0.3f;
        [Tooltip("Neutral stick: 0 = step aside off the enemy's path (recommended), 1 = move the same way the enemy is moving (literal spec)")]
        [SerializeField] int neutralMode = 0;

        public float Distance => Mathf.Max(0f, distance);
        public float Duration => Mathf.Max(0.05f, duration);
        public float Cooldown => Mathf.Max(0f, cooldown);
        public float YawKick => yawKick;
        public float SteerDeadZone => Mathf.Clamp01(steerDeadZone);
        public int NeutralMode => neutralMode;
    }
}
```

#### `Scripts/Features/Dodge/DodgeAbility.cs` (교체)

```csharp
using UnityEngine;

namespace FlyJam
{
    /// Dodge (P04). Only while a warning is on (GameEvents.WarningChanged); the action button does nothing otherwise.
    /// Stick left/right = dodge that way (relative to the fly). Neutral = DodgeBalance.neutralMode.
    /// Temporarily overrides SphereWalker movement (BeginOverride) and makes the fly invulnerable for the dodge.
    [DefaultExecutionOrder(-10)]   // Reads input before SphereWalker.Update
    public class DodgeAbility : MonoBehaviour
    {
        [SerializeField] SphereWalker walker;            // Auto-filled from GameContext.Player if null
        [SerializeField] DodgeBalance balance;

        IMovingObstacle threat;
        float timer, cd, yawSign;
        Vector3 dir;

        public bool IsDodging => timer > 0f;

        void Start()
        {
            if (walker == null && GameContext.Current != null) walker = GameContext.Current.Player;
        }

        void OnEnable() => GameEvents.WarningChanged += OnWarning;

        void OnDisable()
        {
            GameEvents.WarningChanged -= OnWarning;
            Finish();
        }

        void OnWarning(IMovingObstacle t) => threat = t;

        void Update()
        {
            if (walker == null) return;
            if (cd > 0f) cd -= Time.deltaTime;
            if (timer > 0f) { Tick(); return; }

            if (!HasThreat() || cd > 0f || TimeControl.InputBlocked) return;
            if (!walker.InputEnabled || walker.Input == null || walker.IsOverridden) return;
            if (walker.State == FlyState.Celebrating || walker.State == FlyState.Disabled) return;
            if (!walker.Input.ReadDodge()) return;

            Begin(InputCompat.Steer());
        }

        bool HasThreat() => threat != null && !(threat is Object uo && uo == null);

        void Begin(float steer)
        {
            Transform t = walker.transform;
            Vector3 up = walker.Planet.UpAt(t.position);
            Vector3 fwd = Vector3.ProjectOnPlane(t.forward, up).normalized;
            Vector3 right = Vector3.Cross(up, fwd);
            float dead = balance != null ? balance.SteerDeadZone : 0.3f;

            if (Mathf.Abs(steer) > dead)
            {
                dir = right * Mathf.Sign(steer);
                yawSign = Mathf.Sign(steer);
            }
            else if (balance != null && balance.NeutralMode == 1)
            {
                Vector3 along = Vector3.ProjectOnPlane(threat.Direction, up);
                dir = along.sqrMagnitude > 1e-6f ? along.normalized : right;
                yawSign = 0f;
            }
            else
            {
                dir = AsideFromPath(t.position, up, right);
                yawSign = Mathf.Sign(Vector3.Dot(dir, right));
            }

            timer = Duration;
            cd = balance != null ? balance.Cooldown : 0.4f;
            walker.BeginOverride(this);
            walker.SetInvulnerable(this, true);
            walker.SetState(FlyState.Dodging);
            if (walker.IsPlayer) GameEvents.RaisePlayerDodged(t.position, dir);
            Tick();
        }

        float Duration => balance != null ? balance.Duration : 0.22f;

        void Tick()
        {
            float dt = Time.deltaTime;
            float spd = (balance != null ? balance.Distance : 2.2f) / Duration;
            walker.MoveBy(dir * spd * dt);
            walker.AlignToSurface();
            Vector3 up = walker.Planet.UpAt(walker.transform.position);
            dir = Vector3.ProjectOnPlane(dir, up).normalized;
            float kick = balance != null ? balance.YawKick : 25f;
            if (yawSign != 0f) walker.TurnAround(yawSign * kick * dt / Duration);
            walker.SetMotion(spd, spd);

            timer -= dt;
            if (timer <= 0f) Finish();
        }

        void Finish()
        {
            timer = 0f;
            if (walker == null) return;
            walker.EndOverride(this);
            walker.SetInvulnerable(this, false);
            if (walker.State == FlyState.Dodging) walker.SetState(FlyState.Stopped);
        }

        /// Sideways, away from the line the enemy is travelling on.
        Vector3 AsideFromPath(Vector3 pos, Vector3 up, Vector3 fallbackRight)
        {
            Vector3 side = Vector3.Cross(up, threat.Direction);
            if (Vector3.Dot(side, pos - threat.Position) < 0f) side = -side;
            side = Vector3.ProjectOnPlane(side, up);
            return side.sqrMagnitude > 1e-6f ? side.normalized : fallbackRight;
        }
    }
}
```

### 5-10. Features/Rival (신규 기능)

#### `Scripts/Features/Rival/FlyJam.Rival.asmdef` (신규)

```json
{
    "name": "FlyJam.Rival",
    "rootNamespace": "FlyJam",
    "references": [
        "FlyJam.Core",
        "Unity.TextMeshPro"
    ],
    "autoReferenced": true
}
```

#### `Scripts/Features/Rival/RivalBalance.cs` (신규)

```csharp
using UnityEngine;

namespace FlyJam
{
    /// Planner numbers for the mash battle with a male (C07, E05, P05). Sheet tab "Rival".
    public class RivalBalance : BalanceTable
    {
        public override string SheetName => "Rival";

        [Tooltip("Presses needed to break free")]
        [SerializeField] int targetPresses = 25;
        [Tooltip("Seconds to reach it")]
        [SerializeField] float timeLimit = 6f;
        [Tooltip("HP lost when time runs out, fraction of max HP (0.1 of 10 = -1)")]
        [SerializeField] float failDamageRatio = 0.1f;
        [Tooltip("Presses in the first moment are ignored (presses from before the battle do not count)")]
        [SerializeField] float inputDelay = 0.2f;
        [Tooltip("Seconds the result message stays")]
        [SerializeField] float resultShowSeconds = 1.0f;

        public int TargetPresses => Mathf.Max(1, targetPresses);
        public float TimeLimit => Mathf.Max(0.5f, timeLimit);
        public float FailDamageRatio => Mathf.Max(0f, failDamageRatio);
        public float InputDelay => Mathf.Max(0f, inputDelay);
        public float ResultShowSeconds => Mathf.Max(0f, resultShowSeconds);
    }
}
```

#### `Scripts/Features/Rival/RivalBattle.cs` (신규)

```csharp
using System.Collections;
using TMPro;
using UnityEngine;

namespace FlyJam
{
    /// Mash battle with a male (C07, E05, P05, A05).
    /// Starts on GameEvents.RivalEncounter (raised by the male enemy on contact). The world pauses (session.Pause):
    /// no movement, no enemies, no HP drain. Each new press of the action button counts once (holding does not repeat).
    /// Reach targetPresses within timeLimit -> free. Otherwise HP -failDamageRatio, then back to exploring if HP is left.
    public class RivalBattle : MonoBehaviour
    {
        [SerializeField] RivalBalance balance;
        [SerializeField] GameSession session;                 // Auto-filled from GameContext if null
        [SerializeField] SphereWalker player;                 // Auto-filled from GameContext if null
        [SerializeField] PlanetFollowCamera followCamera;     // Auto-filled from GameContext if null
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text titleText;                  // "オスと連打バトル！" -> result
        [SerializeField] TMP_Text promptText;                 // "Ⓐを連打！"
        [SerializeField] TMP_Text countText;                  // "12／25"
        [SerializeField] RectTransform gaugeFill;             // stretched by anchorMax.x = count / target
        [SerializeField] RectTransform timerFill;             // stretched by anchorMax.x = time left / limit

        public bool Running { get; private set; }

        void Start()
        {
            var ctx = GameContext.Current;
            if (ctx != null)
            {
                if (session == null) session = ctx.Session;
                if (player == null) player = ctx.Player;
                if (followCamera == null) followCamera = ctx.FollowCamera;
            }
            if (panel != null) panel.SetActive(false);
        }

        void OnEnable() => GameEvents.RivalEncounter += OnEncounter;
        void OnDisable() => GameEvents.RivalEncounter -= OnEncounter;

        void OnEncounter(IObstacle male)
        {
            if (Running || session == null || player == null) return;
            if (session.Ended || session.IsPaused || player.State == FlyState.Disabled) return;
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            Running = true;
            int target = balance != null ? balance.TargetPresses : 25;
            float limit = balance != null ? balance.TimeLimit : 6f;
            float wait = balance != null ? balance.InputDelay : 0.2f;
            float left = limit;
            int count = 0;

            session.Pause(this);
            player.SetInputEnabled(false);
            if (followCamera != null) followCamera.SuppressLookOut = true;
            panel.SetActive(true);
            titleText.text = Loc.Get("rival.title");
            Refresh(count, target, left, limit);
            GameEvents.RaiseRivalBattleStarted();

            bool won;
            while (true)
            {
                yield return null;
                Refresh(count, target, left, limit);
                if (TimeControl.InputBlocked) continue;       // pause menu open
                float dt = Time.deltaTime;
                if (wait > 0f) { wait -= dt; continue; }

                left -= dt;
                if (InputCompat.ActionPressed())
                {
                    count++;
                    GameEvents.RaiseRivalPressed(count, target);
                }
                if (count >= target) { won = true; break; }
                if (left <= 0f) { won = false; break; }
            }

            left = Mathf.Max(0f, left);
            Refresh(count, target, left, limit);
            titleText.text = Loc.Get(won ? "rival.win" : "rival.lose");
            promptText.text = "";

            float show = balance != null ? balance.ResultShowSeconds : 1f;
            while (show > 0f)
            {
                yield return null;
                if (!TimeControl.Paused) show -= Time.deltaTime;
            }

            panel.SetActive(false);
            if (followCamera != null) followCamera.SuppressLookOut = false;
            session.Resume(this);
            player.SetInputEnabled(true);
            Running = false;
            GameEvents.RaiseRivalBattleEnded(won);
            if (!won) GameEvents.RaiseDamageRequested(balance != null ? balance.FailDamageRatio : 0.1f, "Hit");
        }

        void Refresh(int count, int target, float left, float limit)
        {
            if (promptText != null && count < target) promptText.text = Loc.Format("rival.prompt", InputCompat.ActionLabel);
            if (countText != null) countText.text = Loc.Format("rival.count", count, target);
            if (gaugeFill != null) gaugeFill.anchorMax = new Vector2(Mathf.Clamp01((float)count / target), gaugeFill.anchorMax.y);
            if (timerFill != null) timerFill.anchorMax = new Vector2(Mathf.Clamp01(left / limit), timerFill.anchorMax.y);
        }
    }
}
```

### 5-11. Features/Fx

#### `Scripts/Features/Fx/FxDirector.cs` (교체)

```csharp
using UnityEngine;

namespace FlyJam
{
    /// One-shot particle effects for gameplay events. Listens to GameEvents only.
    public class FxDirector : MonoBehaviour
    {
        [SerializeField] Planet planet;
        [SerializeField] Transform player;
        [SerializeField] ParticleSystem dodgeTrail, hitBurst, heartBurst, matePop, perfectBurst;
        [Tooltip("Apple pickup burst. Empty = uses perfectBurst")]
        [SerializeField] ParticleSystem appleBurst;

        void Awake()
        {
            var ctx = GameContext.Current;
            if (ctx != null)
            {
                if (planet == null) planet = ctx.Planet;
                if (player == null && ctx.Player != null) player = ctx.Player.transform;
            }
            if (heartBurst == null) return;
            var mat = heartBurst.GetComponent<ParticleSystemRenderer>().material;
            var tex = PixelHeart.Get();
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
        }

        void OnEnable()
        {
            GameEvents.PlayerDodged += OnDodged;
            GameEvents.PlayerHit += OnHit;
            GameEvents.CourtshipFinished += OnCourtship;
            GameEvents.MateAppeared += OnMateAppeared;
            GameEvents.PerfectDodge += OnPerfect;
            GameEvents.AppleEaten += OnApple;
        }

        void OnDisable()
        {
            GameEvents.PlayerDodged -= OnDodged;
            GameEvents.PlayerHit -= OnHit;
            GameEvents.CourtshipFinished -= OnCourtship;
            GameEvents.MateAppeared -= OnMateAppeared;
            GameEvents.PerfectDodge -= OnPerfect;
            GameEvents.AppleEaten -= OnApple;
        }

        void OnDodged(Vector3 pos, Vector3 dir)
        {
            Vector3 up = planet.UpAt(pos);
            PlayAt(dodgeTrail, pos + up * 0.3f, Quaternion.LookRotation(-dir, up));
        }

        void OnHit() => PlayUp(hitBurst, player.position, 0.3f);

        void OnCourtship(bool ok, Vector3 pos)
        {
            if (ok) PlayUp(heartBurst, pos, 0.6f);
        }

        void OnMateAppeared(Vector3 pos) => PlayUp(matePop, pos, 0.3f);

        void OnPerfect() => PlayUp(perfectBurst, player.position, 0.4f);

        void OnApple(Vector3 pos) => PlayUp(appleBurst != null ? appleBurst : perfectBurst, pos, 0.4f);

        void PlayUp(ParticleSystem ps, Vector3 pos, float lift)
        {
            Vector3 up = planet.UpAt(pos);
            PlayAt(ps, pos + up * lift, Quaternion.FromToRotation(Vector3.up, up));
        }

        static void PlayAt(ParticleSystem ps, Vector3 pos, Quaternion rot)
        {
            if (ps == null) return;
            ps.transform.SetPositionAndRotation(pos, rot);
            ps.Play(true);
        }
    }
}
```

### 5-12. Features/Sky — `DayNightCycle.cs` (수정, 1줄)

`LateUpdate()` 안의 줄(60번째 근처):
```csharp
            T = session != null ? session.Normalized : Mathf.Repeat(Time.time / idleCycleSeconds, 1f);
```
을 아래로 바꾼다(하늘이 하루마다 반복. `Normalized`는 첫날 이후 1에 멈춘다):
```csharp
            T = session != null ? session.DayProgress : Mathf.Repeat(Time.time / idleCycleSeconds, 1f);
```
그 외 줄은 건드리지 않는다.

### 5-13. UI

#### `Scripts/UI/MenuNavigator.cs` (신규)

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace FlyJam
{
    /// Menu control for pad and keyboard (P02): left stick / D-pad / arrows / WASD move the cursor,
    /// the action button (Switch A / Xbox B / PS ○ / Space / Enter) runs the selected Button's OnClick.
    /// Works while the game is paused (no time used). Mouse clicks still work through the EventSystem if the scene has one.
    public class MenuNavigator : MonoBehaviour
    {
        [SerializeField] Button[] buttons;
        [SerializeField] float selectedScale = 1.08f;
        [SerializeField] Color selectedColor = new Color(0.12f, 0.29f, 0.33f, 1f);
        [SerializeField] Color normalColor = new Color(0.85f, 0.88f, 0.86f, 1f);
        [SerializeField] Color selectedTextColor = Color.white;
        [SerializeField] Color normalTextColor = new Color(0.2f, 0.2f, 0.2f, 1f);

        int index;
        int openedFrame;

        public int Index => index;

        void OnEnable()
        {
            openedFrame = Time.frameCount;
            index = 0;
            Apply();
        }

        void Update()
        {
            if (buttons == null || buttons.Length == 0) return;
            if (Time.frameCount == openedFrame) return;   // the press that opened this menu does not also choose an item

            var m = InputCompat.MenuMove();
            int step = m.y != 0 ? -m.y : m.x;              // up / left = previous, down / right = next
            if (step != 0)
            {
                index = (index + step + buttons.Length) % buttons.Length;
                Apply();
                GameEvents.RaiseMenuMoved();
            }

            if (InputCompat.ActionPressed() && buttons[index] != null && buttons[index].interactable)
            {
                GameEvents.RaiseMenuSelected();
                buttons[index].onClick.Invoke();
            }
        }

        /// Mouse hover can call this from an EventTrigger if wanted.
        public void Select(int i)
        {
            if (buttons == null || i < 0 || i >= buttons.Length) return;
            index = i;
            Apply();
        }

        void Apply()
        {
            if (buttons == null) return;
            for (int i = 0; i < buttons.Length; i++)
            {
                var b = buttons[i];
                if (b == null) continue;
                bool on = i == index;
                b.transform.localScale = Vector3.one * (on ? selectedScale : 1f);
                if (b.targetGraphic != null) b.targetGraphic.color = on ? selectedColor : normalColor;
                var label = b.GetComponentInChildren<TMPro.TMP_Text>();
                if (label != null) label.color = on ? selectedTextColor : normalTextColor;
            }
        }
    }
}
```

#### `Scripts/UI/TitleScreen.cs` (교체)

```csharp
using UnityEngine;
using UnityEngine.EventSystems;

namespace FlyJam
{
    /// Title (S01, A06). Buttons: あそぶ (StartGame) and the language button (ToggleLanguage), driven by MenuNavigator.
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

#### `Scripts/UI/PauseMenu.cs` (신규)

```csharp
using UnityEngine;

namespace FlyJam
{
    /// Pause (S07, P06): Start / + / Esc opens the menu. Everything stops (time scale 0 and session pause):
    /// HP, clock, position, enemies and the remaining time of a courtship or mash battle.
    /// Buttons: つづける (Resume), タイトルに戻る (ToTitle). Disabled after the round ends.
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] GameSession session;        // Auto-filled from GameContext if null
        [SerializeField] GameObject panel;

        public bool IsOpen { get; private set; }

        void Start()
        {
            if (session == null && GameContext.Current != null) session = GameContext.Current.Session;
            if (panel != null) panel.SetActive(false);
        }

        void Update()
        {
            if (session == null) return;
            if (session.Ended)
            {
                if (IsOpen) Resume();
                return;
            }
            if (!InputCompat.PausePressed()) return;
            if (IsOpen) Resume();
            else Open();
        }

        void Open()
        {
            IsOpen = true;
            if (panel != null) panel.SetActive(true);
            session.Pause(this);
            TimeControl.SetPaused(true);
            GameEvents.RaiseGamePaused(true);
        }

        public void Resume()
        {
            if (!IsOpen) return;
            IsOpen = false;
            if (panel != null) panel.SetActive(false);
            if (session != null) session.Resume(this);
            TimeControl.SetPaused(false);
            GameEvents.RaiseGamePaused(false);
        }

        public void ToTitle() => SceneFlow.GoToTitle();

        void OnDisable()
        {
            if (IsOpen) TimeControl.SetPaused(false);
        }
    }
}
```

#### `Scripts/UI/ResultPanel.cs` (신규)

```csharp
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;

namespace FlyJam
{
    /// Game over / clear panel inside the Game scene (S04, S05, S06, A06). Replaces the old Result scene.
    /// Title  = text key "result.<reason>"      (result.love = 求愛成功！, result.hit / result.starved = 体力がなくなった！)
    /// Sub    = text key "result.<reason>.sub"  (メスのハエに出会えた / 赤りんごで体力を回復しよう)
    /// Below: dodge reaction stats and one FLY FACT line. Buttons: もういちど (Retry), タイトルに戻る (ToTitle).
    public class ResultPanel : MonoBehaviour
    {
        [SerializeField] GameSession session;        // Auto-filled from GameContext if null
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text subText;
        [SerializeField] TMP_Text statsText;
        [SerializeField] TMP_Text factText;

        void Start()
        {
            if (session == null && GameContext.Current != null) session = GameContext.Current.Session;
            if (panel != null) panel.SetActive(false);
        }

        void OnEnable() => GameEvents.RoundEnded += OnRoundEnded;
        void OnDisable() => GameEvents.RoundEnded -= OnRoundEnded;

        void OnRoundEnded(bool cleared) => StartCoroutine(Show());

        IEnumerator Show()
        {
            yield return new WaitForSecondsRealtime(session != null ? session.ResultDelay : 1.5f);
            Fill();
            if (panel != null) panel.SetActive(true);
        }

        void Fill()
        {
            string reason = string.IsNullOrEmpty(SessionResult.Reason) ? "" : SessionResult.Reason.ToLowerInvariant();
            string key = "result." + reason;
            bool known = reason.Length > 0 && Loc.Has(key);
            titleText.text = known ? Loc.Get(key) : Loc.Get(SessionResult.Cleared ? "result.clear" : "result.failed");
            if (subText != null) subText.text = known && Loc.Has(key + ".sub") ? Loc.Get(key + ".sub") : "";

            if (statsText != null)
            {
                var sb = new StringBuilder();
                if (SessionResult.TryGetStat("reaction.best", out float best))
                {
                    sb.AppendLine(Loc.Format("result.best", best));
                    sb.AppendLine(Loc.Get("result.lab"));
                }
                if (SessionResult.TryGetStat("dodge.perfect", out float dodges) && dodges > 0f)
                    sb.AppendLine(Loc.Format("result.dodges", dodges));
                statsText.text = sb.ToString().TrimEnd();
            }
            if (factText != null) factText.text = FlyFacts.Pick();
        }

        public void Retry() => SceneFlow.GoToGame();
        public void ToTitle() => SceneFlow.GoToTitle();
    }
}
```

#### `Scripts/UI/GameHud.cs` (교체)

```csharp
using TMPro;
using UnityEngine;

namespace FlyJam
{
    /// Movement guide at the bottom centre (A03): "自動で進む　左スティックで曲がる" (keyboard: A・Dキー).
    /// Hides after the player has steered for a moment, or after maxShowSeconds.
    public class GameHud : MonoBehaviour
    {
        [SerializeField] GameObject guideRoot;
        [SerializeField] TMP_Text guideText;
        [SerializeField] float steerSecondsToHide = 0.6f;
        [SerializeField] float maxShowSeconds = 12f;

        float steered, shown;
        bool done;

        void Update()
        {
            if (done || guideRoot == null) return;
            if (guideText != null) guideText.text = Loc.Get(InputCompat.UsingPad ? "hud.guide.pad" : "hud.guide.kb");
            if (TimeControl.Paused) return;

            shown += Time.unscaledDeltaTime;
            if (Mathf.Abs(InputCompat.Steer()) > 0.3f) steered += Time.unscaledDeltaTime;
            if (steered >= steerSecondsToHide || shown >= maxShowSeconds)
            {
                done = true;
                guideRoot.SetActive(false);
            }
        }
    }
}
```

### 5-14. 삭제

4-3 (a)의 명령으로 `Scripts/UI/ResultScreen.cs`, `Scripts/Features/Backpedal/` 폴더를 지운다(프리팹·씬 삭제는 7장 끝, 4-3 (b)). 그다음 컴파일 → **자가 확인 1**.

---

## 6. 단계 2 — 에셋

### 6-1. 밸런스 에셋 만들기·초기화

`Unity_RunCommand`로 아래를 그대로 실행한다. 새 에셋 7개를 만들고, `HealthBalance`·`HazardBalance`는 **새 코드의 기본값으로 초기화**한다(옛 필드 `obstacleDamage`, `weight` 등이 남아 있으면 안 됨).

```csharp
using UnityEditor;
using UnityEngine;

internal class CommandScript : IRunCommand
{
    const string Dir = "Assets/_Project/Data/Balance/";

    public void Execute(ExecutionResult result)
    {
        Create<FlyJam.PlayerMoveBalance>(Dir + "PlayerMoveBalance.asset");
        Create<FlyJam.SessionBalance>(Dir + "SessionBalance.asset");
        Create<FlyJam.ItemBalance>(Dir + "ItemBalance.asset");
        Create<FlyJam.MateBalance>(Dir + "MateBalance.asset");
        Create<FlyJam.CourtshipBalance>(Dir + "CourtshipBalance.asset");
        Create<FlyJam.RivalBalance>(Dir + "RivalBalance.asset");
        Create<FlyJam.DodgeBalance>(Dir + "DodgeBalance.asset");
        ResetToDefaults<FlyJam.HealthBalance>(Dir + "HealthBalance.asset");
        ResetToDefaults<FlyJam.HazardBalance>(Dir + "HazardBalance.asset");
        AssetDatabase.SaveAssets();
    }

    static void Create<T>(string path) where T : ScriptableObject
    {
        if (AssetDatabase.LoadAssetAtPath<T>(path) != null) { Debug.Log("[CHI05] already exists: " + path); return; }
        AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<T>(), path);
        Debug.Log("[CHI05] created: " + path);
    }

    static void ResetToDefaults<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) { Create<T>(path); return; }
        string keepName = asset.name;
        var fresh = ScriptableObject.CreateInstance<T>();
        EditorUtility.CopySerialized(fresh, asset);
        asset.name = keepName;
        EditorUtility.SetDirty(asset);
        Object.DestroyImmediate(fresh);
        Debug.Log("[CHI05] reset to code defaults: " + path);
    }
}
```

확인: `HealthBalance` 인스펙터 = maxHp 10, startHp 5, drainPerSecond 0.1, obstacleDamageRatio 0.2, minSpeedRatio 0.3 … / `HazardBalance` → `Types` 4줄(dragonfly 2, spider 2, male 1, acorn 1). 값은 14장 표와 같아야 한다.

### 6-2. 텍스트 표 (`TextTable.asset`) 전체 교체

1. 아래 TSV를 **그대로** `FlyGame/Temp/CHI05-text.tsv`에 UTF-8로 저장한다(`Temp/`는 git 무시 폴더).
   - 칸 구분은 **탭 문자**다. 공백으로 바뀌면 안 된다. 저장 후 셸에서 확인: `awk -F'\t' 'NF!=3{print NR": "NF}' FlyGame/Temp/CHI05-text.tsv` → **출력 없음**이어야 한다.
   - `hud.guide.*`, `court.progress`의 큰 공백은 전각 공백(U+3000)이다. 그대로 복사한다.
2. `Unity_RunCommand`:

```csharp
using UnityEditor;
using UnityEngine;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var table = AssetDatabase.LoadAssetAtPath<FlyJam.TextTable>("Assets/_Project/Data/Balance/Resources/TextTable.asset");
        string tsv = System.IO.File.ReadAllText("Temp/CHI05-text.tsv", System.Text.Encoding.UTF8);
        int n = table.EditorImportTsv(tsv);
        AssetDatabase.SaveAssets();
        FlyJam.Loc.Reload();
        Debug.Log("[CHI05] TextTable rows: " + n + " (expected 42)");
    }
}
```
3. 로그가 **42**가 아니면 TSV의 탭이 깨진 것이다. 1번부터 다시.

```tsv
key	en	ja
title.name	LITTLE FLY PLANET	ちいさなハエのほし
title.goal	Find the female fly and win her heart!	メスのハエを見つけて、求愛しよう！
title.play	PLAY	あそぶ
title.language	日本語	English
hud.hp	HP	体力
hud.lowhp	Find an apple!	りんごを探そう！
hud.guide.pad	Auto-run   Left stick: turn	自動で進む　左スティックで曲がる
hud.guide.kb	Auto-run   A / D: turn	自動で進む　A・Dキーで曲がる
hazard.prompt	Press {0} to dodge!	{0}ボタンで回避！
hazard.dodge	DODGE!	回避!
court.title	Press the buttons in order!	順番にボタンを押そう！
court.progress	{0}/{1}   Next: {2}	{0}／{1}　次は{2}
court.success	Courtship success!	求愛成功！
court.fail.title	Courtship failed	求愛失敗
court.fail.wrong	Wrong button!	ボタンの順番が違った！
court.fail.time	Time up!	時間切れ！
rival.title	Mash battle with a male!	オスと連打バトル！
rival.prompt	Mash {0}!	{0}を連打！
rival.count	{0}/{1}	{0}／{1}
rival.win	You won!	連打成功！
rival.lose	Time up...	時間切れ…
pause.title	PAUSED	一時停止
pause.resume	Resume	つづける
menu.totitle	Back to title	タイトルに戻る
result.retry	Retry	もういちど
result.love	Courtship success!	求愛成功！
result.love.sub	You met the female fly.	メスのハエに出会えた
result.hit	Out of HP!	体力がなくなった！
result.hit.sub	Eat red apples to recover HP.	赤りんごで体力を回復しよう
result.starved	Out of HP!	体力がなくなった！
result.starved.sub	Eat red apples to recover HP.	赤りんごで体力を回復しよう
result.clear	CLEAR!	クリア!
result.failed	FAILED	しっぱい
result.best	Your best dodge reaction: {0:0.00}s	あなたの最速の回避反応: {0:0.00}秒
result.lab	Fruit fly (lab data): about 0.2-0.3s	ショウジョウバエ(実験データ): 約0.2〜0.3秒
result.dodges	DODGE! x {0:0}	回避! × {0:0}
fact.header	FLY FACT	ハエまめ知識
fact.1	Fruit flies dodge obstacles hundreds of times faster than today's robots.	ショウジョウバエは、今のロボットより何百倍も速く障害物をよける。
fact.2	Motion-blind fruit flies can't see things while they move. When an object stops, it seems to pop into view.	運動盲のショウジョウバエには、動いている物が見えない。物が止まると、急に見えるようになるらしい。
fact.3	Even without sight, a fruit fly stops when something is in front of it. Its antennae seem to feel the air flow.	目が見えなくても、ショウジョウバエは前に何かあると止まる。触角で空気の流れを感じているらしい。
fact.4	Blind fruit flies may also notice shadows with light receptors. (Research in progress)	目の見えないショウジョウバエも、光受容体で影に気づくのかもしれない。(研究中)
fact.5	A male fruit fly sings a courtship song to the female - sometimes hundreds of times.	オスのショウジョウバエは、メスに求愛の歌をうたう。ときには何百回も。
```

- 지워지는 옛 키: `title.press`, `court.sing`, `court.good`, `court.miss`, `court.ready`, `court.love`, `court.ranaway`, `result.timeup`, `result.controls`. 새 코드는 이 키를 쓰지 않는다.
- 글자는 모두 Silver 폰트에 있는 글자다(확인 완료: 全角 `！／　…×〜`, `Ⓐ Ⓑ ○ × △ □ ← → ↑ ↓ ♪` 포함). 폰트 에셋은 Dynamic이라 처음 쓰는 글자가 자동으로 아틀라스에 추가된다 → `Silver SDF.asset`이 바뀌어 보이는 것은 정상이고 **커밋하지 않는다**(15장).

**자가 확인 2**: Console 에러 0. 위 두 로그 확인.

---

## 7. 단계 3 — 프리팹·씬

### 7-0. 공통 규칙

- 프리팹 편집: `PrefabUtility.LoadPrefabContents(path)` → 수정 → `PrefabUtility.SaveAsPrefabAsset(root, path)` → `PrefabUtility.UnloadPrefabContents(root)`.
- 새 UI 객체: `new GameObject(name, typeof(RectTransform))` 후 `transform.SetParent(parent, false)`. RunCommand 안에서는 `UnityEngine.UI.Image`, `UnityEngine.UI.RawImage`, `UnityEngine.UI.Button`처럼 **풀네임**(AGENTS 6장).
- 텍스트: `TMPro.TextMeshProUGUI`, 폰트 `Assets/_Project/Art/Fonts/Silver SDF.asset`, Font Style Normal(Bold 끔), 가운데 정렬, `raycastTarget` 끔. 크기·색은 표대로.
- 텍스트를 `LocalizedText`로 표시하는 곳은 **TMP 컴포넌트를 먼저** 붙이고 `FlyJam.LocalizedText`를 붙인 뒤 `Key`를 넣는다(`LocalizedText`는 `[RequireComponent(typeof(TMP_Text))]`). 코드가 글자를 직접 쓰는 곳(안내 문구, 결과 제목 등)에는 **`LocalizedText`를 붙이지 않는다**(서로 덮어씀).
- 장식 Image는 `raycastTarget` 끔. 버튼 Image만 켬.
- 표기법: **앵커** (minX,minY)-(maxX,maxY) / **피벗** (x,y) / **위치** = anchoredPosition / **크기** = sizeDelta. "꽉 채움" = 앵커 (0,0)-(1,1), offsetMin = offsetMax = (0,0).
- 새 Canvas는 기존과 같게: Screen Space - Overlay, `CanvasScaler` Scale With Screen Size 1920×1080, Match 0.5, `GraphicRaycaster`.
- 색은 (r, g, b, a) 0~1.
- 오브젝트 참조 연결은 `SerializedObject` + `FindProperty("필드이름")` + `objectReferenceValue`(배열은 `arraySize` 후 `GetArrayElementAtIndex(i)`), 끝에 `ApplyModifiedPropertiesWithoutUndo()`. (리플렉션 금지)

### 7-1. `Fly.prefab` (플레이어)

루트의 `SphereWalker`:
| 필드 | 값 |
|---|---|
| moveMode | **AutoRun** (enum 값 2) |
| balance | `Data/Balance/PlayerMoveBalance.asset` |
| moveSpeed | 6 (에셋이 비었을 때의 대비값) |

나머지 컴포넌트(`KeyboardMoveInput`, `ObstacleHitDetector`, `FlyVisual`, `KeepClearZone`)는 그대로.

### 7-2. `Mate.prefab` (암컷, `Fly.prefab`의 Variant)

`Fly`에 `balance`를 넣었기 때문에 Variant가 그 값을 물려받는다. 암컷은 쓰면 안 된다.
| 컴포넌트 | 필드 | 값 |
|---|---|---|
| `SphereWalker` | balance | **None** (명시적으로 비워서 오버라이드로 저장) |
| `SphereWalker` | moveMode | Steer (지금 오버라이드 그대로, 바꾸지 않음) |
| `MateAI` | balance | `Data/Balance/MateBalance.asset` |
| `MotionBlindVisibility` | motionBlind | **false** |
| `MateGoal` | reachDistance / rearmDistance | 0.9 / 2.5 (기본값) |

`SYS_Mate.prefab` 안의 `Mate` 인스턴스에 `motionBlind`나 `balance` 오버라이드가 있으면 지워서(Revert) 위 값을 따르게 한다.

### 7-3. `Apple.prefab` (신규)

1. `Prefabs/Fruit.prefab`을 씬 없이 인스턴스로 만든다(`PrefabUtility.InstantiatePrefab`) → `PrefabUtility.UnpackPrefabInstance(..., PrefabUnpackMode.Completely, ...)`.
2. 이름을 `Visual`로 바꾸고, localPosition (0,0,0), localScale **(0.4, 0.4, 0.4)**.
3. 새 빈 오브젝트 `Apple`(위치 0, 회전 0, 스케일 1)을 만들고 `Visual`을 그 자식으로.
4. `Visual` 아래 모든 Collider 제거(판정은 거리 계산).
5. `Apple` 루트에 컴포넌트:
   - `FlyJam.AppleVisual` — visual = `Visual`
   - `FlyJam.ToonifyOnSpawn` — 설정값은 `Prefabs/Enemy_Spider.prefab`의 `ToonifyOnSpawn`과 같게
6. `Assets/_Project/Prefabs/Apple.prefab`으로 저장하고 임시 오브젝트는 지운다.
7. `Body`의 머티리얼(빨강)은 그대로. 모양은 나중에 아트가 `Visual`만 바꾼다.

### 7-4. `SYS_Items.prefab` (신규)

```
SYS_Items            (위치 0,0,0)  AppleSpawner
└ Points
  ├ P0 … P7          (빈 Transform, 아래 표 위치)
```
`AppleSpawner`: balance = `ItemBalance.asset`, applePrefab = `Apple.prefab`, points = P0~P7 (8칸, 순서대로). planet/player/session은 비움.

| 지점 | 위치 (월드, 행성 중심 원점) | 비고 |
|---|---|---|
| P0 | (0, 10.88, 5.07) | **시작 지점(0,12,0)에서 진행 방향(+Z)으로 25° 앞** — 첫 사과(A02) |
| P1 | (6.38, 6.55, -7.77) | |
| P2 | (-8.43, 2.18, -8.26) | |
| P3 | (11.99, 0, 0.4) | |
| P4 | (-8.96, -2.18, 7.68) | |
| P5 | (1.35, -4.36, -11.1) | |
| P6 | (5.85, -6.55, 8.18) | |
| P7 | (-8.06, -8.73, -1.7) | |

(지점끼리 최소 57° 떨어짐. 높이는 코드가 지면 + hoverHeight로 맞추므로 대략이면 된다.)

### 7-5. `SYS_Health.prefab`

`Canvas/HealthBar`(앵커 (0,1), 위치 (32,-28), 크기 (440,40)) 안:

| 오브젝트 | 작업 | 값 |
|---|---|---|
| `Label` | 수정 | 크기 (96, 0) — "体力" 두 글자가 들어가게. `LocalizedText` key `hud.hp` 유지 |
| `Frame` | 수정 | offsetMin (104, 0), offsetMax (0, 0) |
| `ValueText` | 신규 TMP | 앵커 (1,0.5)-(1,0.5), 피벗 (0,0.5), 위치 (14,0), 크기 (150,52), 글자 40, 흰색, **왼쪽 정렬**, 내용 "5/10" |
| `DeltaText` | 신규 TMP | 앵커 (1,0.5)-(1,0.5), 피벗 (0,0.5), 위치 (164,0), 크기 (120,52), 글자 40, 왼쪽 정렬, 내용 "+2", **비활성** |
| `LowHpMessage` | 신규 TMP | 앵커 (0,0)-(0,0), 피벗 (0,1), 위치 (0,-8), 크기 (640,52), 글자 40, 색 (1, 0.85, 0.3, 1), 왼쪽 정렬, `LocalizedText` key `hud.lowhp`, **비활성** |

`HealthBar` 컴포넌트: balance = `HealthBalance.asset`, fill = `Frame/Inner/Fill`(RectTransform), fillImage = `Fill`의 Image, valueText = `ValueText`, deltaText = `DeltaText`, lowHpMessage = `LowHpMessage`. 색 필드는 기본값.
`PlayerHealth`(루트): balance = `HealthBalance.asset` (그대로).

### 7-6. `SYS_Hazards.prefab`

`Canvas`(sortingOrder 10) 안을 아래 순서(위가 먼저 그려짐)로 만든다:

| 순서 | 오브젝트 | 작업 | 값 |
|---|---|---|---|
| 1 | `WarningFrame` | 신규, **비활성** | 꽉 채움, Image 없음 |
| | ├ `Top` | Image | 앵커 (0,1)-(1,1), 피벗 (0.5,1), 위치 (0,0), 크기 (0,28), 색 (0.92,0.12,0.12,0.85) |
| | ├ `Bottom` | Image | 앵커 (0,0)-(1,0), 피벗 (0.5,0), 위치 (0,0), 크기 (0,28), 같은 색 |
| | ├ `Left` | Image | 앵커 (0,0)-(0,1), 피벗 (0,0.5), 위치 (0,0), 크기 (28,0), 같은 색 |
| | └ `Right` | Image | 앵커 (1,0)-(1,1), 피벗 (1,0.5), 위치 (0,0), 크기 (28,0), 같은 색 |
| 2 | `PerfectText` | 그대로 | (`LocalizedText` key `hazard.dodge` 유지) |
| 3 | `PromptRoot` | 신규, **비활성** | 앵커 (0.5,1)-(0.5,1), 피벗 (0.5,1), 위치 (0,-150), 크기 (1000,130), Image 색 (0,0,0,0.55) |
| | └ `PromptText` | TMP | 꽉 채움, 글자 80, 흰색, 내용 "Ⓐボタンで回避！", **`LocalizedText` 없음** |
| — | `SpacePrompt` | **삭제** | |

- 위치 (0,-150)은 왼쪽 위 저체력 문구(y −78~−130)와 겹치지 않게 한 것이다(A04 "低体力表示と同時に出ても…読める").

컴포넌트(루트):
| 컴포넌트 | 필드 | 값 |
|---|---|---|
| `NearMissDirector` | balance | `HazardBalance.asset` |
| | promptUI / promptText | `PromptRoot` / `PromptText` |
| | frameUI / perfectText | `WarningFrame` / `PerfectText` |
| | successShowSeconds | 0.8 |
| `ObstacleSpawner` | balance, prefabs | 그대로 (`dragonfly`→`Enemy_Dragonfly`, `spider`→`Enemy_Spider`, `male`→`Enemy_Male`, `acorn`→`RollingObstacle`) |
| `ReactionTracker` | — | 필드 없음(옛 값은 저장 시 사라짐) |

적 프리팹(`Enemy_*`, `RollingObstacle`)은 수정하지 않는다.

### 7-7. `SYS_Dodge.prefab`

`DodgeAbility`: balance = `DodgeBalance.asset`. walker는 비움.

### 7-8. `SYS_Courtship.prefab`

`Canvas/CourtshipPanel`(비활성 유지)을 다시 구성한다. `JudgeText`, `BeatsText`는 **삭제**.

`CourtshipPanel`: 앵커 가운데, 크기 **(1180, 440)**, Image 색 (0.06,0.06,0.12,0.85) 유지.

| 오브젝트 | 값 |
|---|---|
| `TitleText` (TMP) | 앵커 (0.5,1)-(0.5,1), 피벗 (0.5,1), 위치 (0,-24), 크기 (1100,84), 글자 60, 흰색, 내용 "順番にボタンを押そう！" |
| `Slots` (빈 RectTransform) | 앵커 가운데, 위치 (0,10), 크기 (1100,140) |
| └ `Slot0` ~ `Slot7` | 앵커 가운데, 크기 (120,120), 위치 x = −437.5, −312.5, −187.5, −62.5, 62.5, 187.5, 312.5, 437.5 / y = 0 |
| &nbsp;&nbsp;&nbsp;├ `Frame` (Image, **비활성**) | 앵커 가운데, 크기 (132,132), 색 (1,0.82,0.25,1) — 다음에 누를 칸 표시 |
| &nbsp;&nbsp;&nbsp;├ `Box` (Image) | 앵커 가운데, 크기 (110,110), 색 (0.93,0.93,0.9,1) |
| &nbsp;&nbsp;&nbsp;└ `Label` (TMP) | 꽉 채움, 글자 72, 색 (0.45,0.45,0.45,1), 내용 "A" |
| `InfoText` (TMP) | 앵커 (0.5,0)-(0.5,0), 피벗 (0.5,0), 위치 (0,70), 크기 (1100,64), 글자 44, 색 (1,1,1,0.9), 내용 "0／4　次はA" |
| `TimerFill` (Image) | 앵커 (0.5,0)-(0.5,0), 피벗 (0.5,0), 위치 (0,28), 크기 (900,16), 색 (0.6,0.85,1,1), **Image Type = Filled, Fill Method Horizontal, Origin Left**, Source Image = 내장 `UISprite` (`AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd")`) |

- 자식 순서가 `Frame` → `Box` → `Label`이어야 노란 테두리가 박스 뒤에 테두리처럼 보인다.
- 코드는 `Label`의 **부모**(`SlotN`)를 켜고 끈다 → 구애 길이가 4면 Slot4~7은 숨는다.

`CourtshipQTE`(루트):
| 필드 | 값 |
|---|---|
| balance | `CourtshipBalance.asset` |
| panel | `CourtshipPanel` |
| titleText | `TitleText` |
| slots (8) | `Slot0/Label` … `Slot7/Label` |
| slotFrames (8) | `Slot0/Frame` … `Slot7/Frame` |
| infoText | `InfoText` |
| timerFill | `TimerFill` |
| 색 3개 | 기본값 |

`CourtshipFlow`: qte = 같은 루트의 `CourtshipQTE` (그대로). Canvas sortingOrder 12 그대로.

### 7-9. `SYS_Mate.prefab`

`Canvas/MateHint`(RawImage, 분홍):
- RectTransform 크기 (44, 38). 앵커 가운데 그대로.
- `MateFinderHint`: icon = `MateHint`의 RawImage, headHeight 0.9, maxDistance 16, size 44, bobPixels 6, fadeSpeed 8. worldCamera/player/mate/session은 비움.
- 옛 필드(showDelay, edgeMargin, sizeNearFar …)는 저장 시 사라진다.

### 7-10. `SYS_Rival.prefab` (신규)

```
SYS_Rival                         RivalBattle
└ Canvas                          Canvas(sortingOrder 13) + CanvasScaler + GraphicRaycaster
  └ RivalPanel  (비활성)           앵커 가운데, 크기 (1000,420), Image (0.14,0.05,0.05,0.88)
    ├ TitleText                    앵커 (0.5,1) 피벗 (0.5,1) 위치 (0,-24) 크기 (940,80) 글자 60 흰색 "オスと連打バトル！"
    ├ PromptText                   앵커 가운데 위치 (0,60) 크기 (940,100) 글자 84 색 (1,0.85,0.3,1) "Ⓐを連打！"
    ├ CountText                    앵커 가운데 위치 (0,-30) 크기 (940,70) 글자 56 흰색 "0／25"
    ├ Gauge                        앵커 가운데 위치 (0,-105) 크기 (800,44) Image (0,0,0,0.6)
    │ └ Fill                       앵커 (0,0)-(0,1), offsetMin (0,0), offsetMax (0,0), Image (1,0.75,0.2,1)
    └ Timer                        앵커 가운데 위치 (0,-160) 크기 (800,14) Image (0,0,0,0.6)
      └ Fill                       앵커 (0,0)-(1,1), offsetMin (0,0), offsetMax (0,0), Image (0.6,0.85,1,1)
```
- `Gauge/Fill`, `Timer/Fill`은 코드가 `anchorMax.x`(0~1)를 바꿔 길이를 정한다. offset은 0 그대로 둔다.

`RivalBattle`: balance = `RivalBalance.asset`, panel = `RivalPanel`, titleText, promptText, countText = 각 TMP, gaugeFill = `Gauge/Fill`(RectTransform), timerFill = `Timer/Fill`(RectTransform). session/player/followCamera는 비움.

### 7-11. `SYS_Menu.prefab` (신규) — 일시정지 + 결과

```
SYS_Menu                          PauseMenu, ResultPanel
├ EventSystem                     EventSystem + InputSystemUIInputModule (마우스 클릭용)
└ Canvas                          Canvas(sortingOrder 20) + CanvasScaler + GraphicRaycaster
  ├ PausePanel (비활성)            꽉 채움, Image (0,0,0,0.6), MenuNavigator
  │ └ Box                          앵커 가운데, 크기 (640,420), Image (0.95,0.95,0.92,1)
  │   ├ Title                      앵커 가운데 위치 (0,130) 크기 (600,90) 글자 64 색 (0.15,0.15,0.15,1), LocalizedText "pause.title"
  │   ├ ResumeButton               앵커 가운데 위치 (0,10) 크기 (420,84), Image + Button
  │   │ └ Label                    꽉 채움 글자 48, LocalizedText "pause.resume"
  │   └ TitleButton                앵커 가운데 위치 (0,-110) 크기 (420,84), Image + Button
  │     └ Label                    꽉 채움 글자 48, LocalizedText "menu.totitle"
  └ ResultPanel (비활성)           꽉 채움, Image (0,0,0,0.55), MenuNavigator
    └ Box                          앵커 가운데, 크기 (1300,860), Image (0.97,0.96,0.92,0.97)
      ├ TitleText                  앵커 가운데 위치 (0,330) 크기 (1200,130) 글자 96 색 (0.15,0.15,0.15,1)
      ├ SubText                    앵커 가운데 위치 (0,235) 크기 (1200,70) 글자 44 색 (0.3,0.3,0.3,1)
      ├ StatsText                  앵커 가운데 위치 (0,95) 크기 (1150,190) 글자 38 색 (0.2,0.2,0.2,1)
      ├ FactText                   앵커 가운데 위치 (0,-110) 크기 (1150,210) 글자 32 색 (0.25,0.3,0.35,1)
      ├ RetryButton                앵커 가운데 위치 (-240,-320) 크기 (400,90), Image + Button
      │ └ Label                    꽉 채움 글자 48, LocalizedText "result.retry"
      └ TitleButton                앵커 가운데 위치 (240,-320) 크기 (400,90), Image + Button
        └ Label                    꽉 채움 글자 48, LocalizedText "menu.totitle"
```
- 결과 패널의 TitleText·SubText·StatsText·FactText에는 `LocalizedText`를 붙이지 않는다(코드가 씀).
- 버튼 4개 공통: Button `Navigation` = **None**, `Transition` = **None**, `targetGraphic` = 자기 Image. 색은 `MenuNavigator`가 칠한다.
- OnClick(영구 리스너, `UnityEditor.Events.UnityEventTools.AddVoidPersistentListener`):
  - ResumeButton → `SYS_Menu`의 `PauseMenu.Resume`
  - PausePanel의 TitleButton → `PauseMenu.ToTitle`
  - RetryButton → `ResultPanel.Retry`
  - ResultPanel의 TitleButton → `ResultPanel.ToTitle`
- `MenuNavigator`: PausePanel = [ResumeButton, TitleButton], ResultPanel = [RetryButton, TitleButton]. 색·크기 필드는 기본값.
- `PauseMenu`: panel = `PausePanel`. `ResultPanel`: panel = `ResultPanel`, titleText/subText/statsText/factText = 위 TMP. session은 둘 다 비움.
- 이 프리팹의 EventSystem은 Game 씬에 하나뿐이어야 한다. Game 씬에 다른 EventSystem이 있으면 그쪽을 지운다.

### 7-12. `SYS_UI.prefab` (HUD)

- `TimerText` **삭제**.
- 신규 `Guide`: 앵커 (0.5,0)-(0.5,0), 피벗 (0.5,0), 위치 (0,40), 크기 (1000,80), Image (0,0,0,0.45).
  - └ `GuideText` (TMP): 꽉 채움, 글자 44, 흰색, 내용 "自動で進む　左スティックで曲がる", **LocalizedText 없음**.
- `GameHud`: guideRoot = `Guide`, guideText = `GuideText`, steerSecondsToHide 0.6, maxShowSeconds 12.

### 7-13. `SYS_Village.prefab`

`Village`의 `VillageGenerator` 항목 중 label `Fruit`의 **count 6 → 0**. (장식용 빨간 과일이 먹는 사과와 헷갈리지 않게. 항목은 지우지 않는다.)

### 7-14. `SYS_Fx.prefab`

`FxDirector`: 새 필드 appleBurst는 **비워 둔다**(비면 `FX_Perfect`를 씀). 나머지 그대로.

### 7-15. `Game.unity`

1. 루트의 `SYS_Backpedal` 인스턴스 **삭제**.
2. 루트에 `SYS_Items`, `SYS_Rival`, `SYS_Menu` 프리팹 인스턴스 추가(위치 0, 다른 `SYS_*`와 같은 높이).
3. 루트 `GameSession` 오브젝트의 `GameSession`: balance = `SessionBalance.asset`.
4. 저장. 결과로 `SYS_*`는 Health, Courtship, Sky, Village, Fx, Dodge, Mate, UI, Hazards, Items, Rival, Menu = **12개**.

### 7-16. `Title.unity`

현재: `Canvas`(TitleText, PressText, LanguageButton, FontCreditText), `TitleScreen`, `EventSystem`.

| 오브젝트 | 작업 | 값 |
|---|---|---|
| `PressText` | **삭제** | |
| `TitleText` | 그대로 | 위치 (0,160) 로 이동 |
| `GoalText` | 신규 TMP | 앵커 가운데, 위치 (0,30), 크기 (1400,70), 글자 44, 흰색, `LocalizedText` "title.goal" |
| `PlayButton` | 신규 Image + Button | 앵커 가운데, 위치 (0,-150), 크기 (420,96) |
| └ `Label` | TMP | 꽉 채움, 글자 56, `LocalizedText` "title.play" |
| `LanguageButton` | 수정 | Navigation None, Transition None. 나머지 그대로(OnClick → `TitleScreen.ToggleLanguage` 유지) |
| `FontCreditText` | 그대로 | |

- `PlayButton`: Navigation None, Transition None, OnClick → `TitleScreen.StartGame`.
- `TitleScreen` 오브젝트에 `MenuNavigator` 추가: buttons = [PlayButton, LanguageButton].

### 7-17. Build Settings

`EditorBuildSettings.scenes` = `[0] Assets/_Project/Scenes/Title.unity`, `[1] Assets/_Project/Scenes/Game.unity`. Result 제거 → 그다음 **4-3 (b)**로 `SYS_Backpedal.prefab`, `Result.unity` 파일 삭제 → `AssetDatabase.Refresh()`.

**자가 확인 3**: 모든 프리팹·씬 저장, Console 에러 0.

---

## 8. 단계 4 — 자가 검증

### 8-1. 참조 검사 스크립트

`Unity_RunCommand`로 실행. `MISSING`/`NOT FOUND` 줄이 하나도 없어야 한다.

```csharp
using UnityEditor;
using UnityEngine;

internal class CommandScript : IRunCommand
{
    const string P = "Assets/_Project/Prefabs/";

    public void Execute(ExecutionResult result)
    {
        int bad = 0;
        bad += Check(P + "Fly.prefab", "SphereWalker", "balance");
        bad += Check(P + "Mate.prefab", "MateAI", "balance");
        bad += Check(P + "Apple.prefab", "AppleVisual", "visual");
        bad += Check(P + "SYS_Health.prefab", "PlayerHealth", "balance");
        bad += Check(P + "SYS_Health.prefab", "HealthBar", "balance", "fill", "fillImage", "valueText", "deltaText", "lowHpMessage");
        bad += Check(P + "SYS_Items.prefab", "AppleSpawner", "balance", "applePrefab", "points");
        bad += Check(P + "SYS_Hazards.prefab", "ObstacleSpawner", "balance", "prefabs");
        bad += Check(P + "SYS_Hazards.prefab", "NearMissDirector", "balance", "promptUI", "promptText", "frameUI", "perfectText");
        bad += Check(P + "SYS_Dodge.prefab", "DodgeAbility", "balance");
        bad += Check(P + "SYS_Courtship.prefab", "CourtshipQTE", "balance", "panel", "titleText", "slots", "slotFrames", "infoText", "timerFill");
        bad += Check(P + "SYS_Courtship.prefab", "CourtshipFlow", "qte");
        bad += Check(P + "SYS_Mate.prefab", "MateFinderHint", "icon");
        bad += Check(P + "SYS_Rival.prefab", "RivalBattle", "balance", "panel", "titleText", "promptText", "countText", "gaugeFill", "timerFill");
        bad += Check(P + "SYS_Menu.prefab", "PauseMenu", "panel");
        bad += Check(P + "SYS_Menu.prefab", "ResultPanel", "panel", "titleText", "subText", "statsText", "factText");
        bad += Check(P + "SYS_Menu.prefab", "MenuNavigator", "buttons");
        bad += Check(P + "SYS_UI.prefab", "GameHud", "guideRoot", "guideText");
        Debug.Log(bad == 0 ? "[CHI05] reference check OK" : "[CHI05] reference check: " + bad + " problem(s)");
    }

    /// Every listed field of every component with this script name must be set (arrays: size > 0 and no empty slot).
    static int Check(string path, string script, params string[] fields)
    {
        var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (root == null) { Debug.LogError("[CHI05] NOT FOUND prefab " + path); return 1; }
        int bad = 0, found = 0;
        foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb == null) { Debug.LogError("[CHI05] MISSING script on " + path); bad++; continue; }
            var so = new SerializedObject(mb);
            var ms = so.FindProperty("m_Script").objectReferenceValue;
            if (ms == null || ms.name != script) continue;
            found++;
            foreach (var f in fields)
            {
                var p = so.FindProperty(f);
                if (p == null) { Debug.LogError($"[CHI05] NOT FOUND field {script}.{f} in {path}"); bad++; continue; }
                if (p.isArray && p.propertyType != SerializedPropertyType.String)
                {
                    if (p.arraySize == 0) { Debug.LogError($"[CHI05] MISSING {script}.{f} (empty) in {path}/{mb.name}"); bad++; }
                    for (int i = 0; i < p.arraySize; i++)
                    {
                        var e = p.GetArrayElementAtIndex(i);
                        if (e.propertyType == SerializedPropertyType.ObjectReference && e.objectReferenceValue == null)
                        { Debug.LogError($"[CHI05] MISSING {script}.{f}[{i}] in {path}/{mb.name}"); bad++; }
                        if (e.propertyType == SerializedPropertyType.Generic)
                        {
                            var pf = e.FindPropertyRelative("prefab");
                            if (pf != null && pf.objectReferenceValue == null)
                            { Debug.LogError($"[CHI05] MISSING {script}.{f}[{i}].prefab in {path}"); bad++; }
                        }
                    }
                }
                else if (p.propertyType == SerializedPropertyType.ObjectReference && p.objectReferenceValue == null)
                { Debug.LogError($"[CHI05] MISSING {script}.{f} in {path}/{mb.name}"); bad++; }
            }
        }
        if (found == 0) { Debug.LogError($"[CHI05] NOT FOUND component {script} in {path}"); bad++; }
        return bad;
    }
}
```

### 8-2. 눈으로 확인할 것 (에이전트)

| # | 확인 | 기대 |
|---|---|---|
| 1 | `Game.unity` 루트 | `SYS_Backpedal` 없음. `SYS_Items`, `SYS_Rival`, `SYS_Menu` 있음. `GameSession.balance` = SessionBalance |
| 2 | `Mate.prefab` `SphereWalker.balance` | None (Fly에서 물려받지 않음) |
| 3 | Build Settings | Title, Game 2개 |
| 4 | `TextTable.asset` | 42줄, 첫 줄 `title.name` |
| 5 | 프로젝트 검색 `ResultScreen`, `BackpedalFacing`, `GoToResult` | 코드·프리팹·씬 어디에도 없음 |
| 6 | 프로젝트 검색 `Time.timeScale =` | `TimeControl.cs`에만 있음 |
| 7 | 캔버스 정렬 | Health 1, Hazards 10, Mate 11, Courtship 12, Rival 13, Menu 20, UI(HUD) 0 |
| 8 | UI 캡처(AGENTS 6장 방법) | `SYS_Courtship` 패널, `SYS_Rival` 패널, `SYS_Menu` 두 패널, `Title` 화면을 임시로 켜서 캡처 → 보고서에 첨부. **저장하지 말고** 씬 다시 열기 |

Play 모드는 MCP로 켤 수 없으므로 실행 확인은 사람이 12장으로 한다.

---

## 9. AGENTS.md 수정

`FlyGame/AGENTS.md`에서 아래 부분을 바꾼다. 나머지는 그대로.

**2.1절** 본문을 교체:
```markdown
- `[0] Assets/_Project/Scenes/Title.unity` → `[1] Game.unity`. Result 씬은 없다. 결과는 Game 씬 안의 패널(`SYS_Menu`/`ResultPanel`: もういちど = 씬 재로드, タイトルに戻る).
- 시간 배율(`Time.timeScale`)은 `Core/Session/TimeControl`만 바꾼다(일시정지 0, 경고 슬로 0.5). 씬 전환 때 `SceneFlow`가 `TimeControl.ResetAll()`.
- 입력은 `Core/Session/InputCompat`만 읽는다(패드 우선, 키보드 대체, 기기별 버튼 표기).
```

**2.2절**의 Features 줄 두 개를 교체:
```markdown
- **Features** (10개): `Features/{Courtship, Dodge, Fx, Hazards, Health, Items, Mate, Rival, Sky, Village}/FlyJam.<Name>.asmdef`
  - 기본 참조: `FlyJam.Core` (`Hazards`·`Rival`은 `+ Unity.TextMeshPro`, `Courtship`·`Health`는 `+ Unity.TextMeshPro, UnityEngine.UI`, `Mate`는 `+ UnityEngine.UI`)
```
그리고 첫 문장의 "11개 어셈블리(런타임 10 + 에디터 전용 …)"를 "13개 어셈블리(런타임 12 + 에디터 전용 …)"로.

**2.3절** 제목과 목록을 교체:
```markdown
### 2.3 `Game.unity` 계층 & `SYS_*` 프리팹 (12개)
`Game.unity`는 `[Core]` + 12개 기능 프리팹(`Assets/_Project/Prefabs/SYS_*.prefab`)으로 구성된다. 루트 `GameSession`은 `SessionBalance`(하루 길이, 결과 대기)를 참조한다.
- **`SYS_Village.prefab`**: (기존 설명 유지) `VillageGenerator`의 `Fruit` 항목은 count 0(먹는 사과와 구분).
- **`SYS_Sky.prefab`**: `DayNight` (`DayNightCycle`, 하늘색 = `GameSession.DayProgress`, 하루 반복. 라운드를 끝내지 않음).
- **`SYS_Health.prefab`**: `PlayerHealth`(`HealthBalance`, HP → 속도), 자체 `Canvas`(`sortingOrder 1`: `HealthBar` = 막대·숫자·증감·저체력 문구).
- **`SYS_Items.prefab`**: `AppleSpawner`(`ItemBalance`, `Apple.prefab`, 고정 지점 `Points/P0~P7`).
- **`SYS_Hazards.prefab`**: `ObstacleSpawner`(`HazardBalance`, 종류별 고정 개체 수 `dragonfly/spider/male/acorn`, 행성을 계속 돎), `NearMissDirector`(경고 대상·테두리·안내·슬로·회피 성공), `ReactionTracker`, 자체 `Canvas`(`sortingOrder 10`: `WarningFrame`, `PerfectText`, `PromptRoot`). 적 = `RollingObstacle` 범용 컴포넌트, 종류별 숫자는 시트 `HazardTypes`.
- **`SYS_Dodge.prefab`**: `DodgeAbility`(`DodgeBalance`, 경고 중에만, `ExecutionOrder -10`).
- **`SYS_Mate.prefab`**: 중첩 `Mate.prefab`(`MateAI`+`MateBalance`, `MateGoal`, `MotionBlindVisibility`(motionBlind 끔) …), 자체 `Canvas`(`sortingOrder 11`: `MateHint` + `MateFinderHint` = 머리 위 발견 마크).
- **`SYS_Courtship.prefab`**: `CourtshipFlow`, `CourtshipQTE`(`CourtshipBalance`, 버튼 순서 입력), 자체 `Canvas`(`sortingOrder 12`: `CourtshipPanel`).
- **`SYS_Rival.prefab`**: `RivalBattle`(`RivalBalance`, 수컷 접촉 시 연타 배틀), 자체 `Canvas`(`sortingOrder 13`: `RivalPanel`).
- **`SYS_Fx.prefab`**: `FxDirector`(+ 사과 먹기), `AmbientParticles`, 파티클 7종.
- **`SYS_Menu.prefab`**: `PauseMenu`, `ResultPanel`, `EventSystem`, 자체 `Canvas`(`sortingOrder 20`: `PausePanel`·`ResultPanel`, 각각 `MenuNavigator`).
- **`SYS_UI.prefab`**: HUD `Canvas`(`sortingOrder 0`), `GameHud`(아래 가운데 이동 안내 `Guide`).
```

**3절 표** 맨 아래에 한 줄 추가:
```markdown
| **CHI/01~05 (dev)** | 01 Balance Sync(구글 시트 → `BalanceTable`), 02 EN/JA(`Loc`, `TextTable`), 03 체력, 04 적 3종, 05 기획서 M1~M4 전체(오토런, 패드, 사과, 고정 개체 적, 경고·회피, 연타 배틀, 버튼 순서 구애, 일시정지, 결과 패널. Backpedal·Result 씬 삭제). |
```

**4절 6번** 목록 끝에 추가:
```markdown
   - `Time.timeScale`은 `TimeControl`만 쓴다. 입력은 `InputCompat`만 읽는다. 대면 이벤트는 `GameSession.Pause(this)`로 세계를 멈추고, 입력 타이머는 `Time.deltaTime` + `TimeControl.InputBlocked` 확인.
```

---

## 10. 보고 형식 (에이전트)

```
## CHI/05 보고
1. 만든/바꾼/지운 파일 목록 (4장 표와 대조, 빠진 것 표시)
2. 스펙과 다르게 한 것: 파일·줄·무엇·왜
3. 미완료 항목과 이유
4. 로그: [CHI05] created/reset 9줄, TextTable rows 42, reference check OK
5. 8-2 표 1~8 결과 + UI 캡처
6. Console 최종 상태 (에러 0, 경고 목록)
```

---

## 11. 사람 — 시트 작업 (에이전트 작업 후, Play 전에)

새 탭이 많다. **Pull보다 Copy가 먼저**다. 순서가 바뀌면 옛 시트 값이 새 에셋을 덮는다(특히 `HazardTypes`는 열 이름이 바뀌어 적 수가 0이 된다).

1. Unity → `FlyJam → Balance Sync`.
2. 아래 표의 각 줄: 에셋 줄의 **Copy '탭' (TSV)** → 구글 시트에서 그 이름의 탭(없으면 새로 만들기) → **탭 내용을 전부 지우고** A1에 붙여넣기.

| 에셋 | 시트 탭 | 상태 |
|---|---|---|
| PlayerMoveBalance | `Player` | 새 탭 |
| SessionBalance | `Session` | 새 탭 |
| ItemBalance | `Items` | 새 탭 |
| MateBalance | `Mate` | 새 탭 |
| CourtshipBalance | `Courtship` | 새 탭 |
| RivalBalance | `Rival` | 새 탭 |
| DodgeBalance | `Dodge` | 새 탭 |
| HealthBalance | `Health` | **지우고 다시** (옛 `obstacleDamage` 등 제거) |
| HazardBalance | `Hazards` | **지우고 다시** |
| HazardBalance | `HazardTypes` | **지우고 다시** (열: id, count, speedRatio, damageRatio, warn, rival, chase, detectRadius, loseRadius, turnRate, stopOnHit) |
| TextTable | `Text` | **지우고 다시** (42줄) |

3. **Pull** → 로그에 값 변화가 없어야 한다("unknown key/column" 경고도 없어야 함). 변화가 있으면 그 탭을 다시 붙여넣는다.
4. 이후부터는 평소처럼 시트에서 고치고 Pull.

---

## 12. 체크포인트 (사람, 기획서 ID별)

패드(가능하면 Switch Pro 또는 Xbox 패드)를 연결하고 확인한다. 키보드 대체: 이동 A·D/←→, 액션 Space, 일시정지 Esc, 구애 →↓↑←.

### 12-1. 타이틀·이동 (S01, P01, P02, A06, C01)
| # | 할 일 | 성공이면 |
|---|---|---|
| 1 | Title 씬에서 Play | 제목, 목표 한 줄, `あそぶ`(선택 표시), 오른쪽 위 언어 버튼 |
| 2 | 스틱 위아래/좌우 | 커서가 あそぶ ↔ 언어 버튼 이동(색·크기 변화) |
| 3 | 언어 버튼에서 액션 | EN/JA 전환. 화면 전체 글자가 바뀜. 한 번만 바뀜 |
| 4 | あそぶ에서 액션 | Game 씬. 파리가 **입력 없이** 앞으로 감 |
| 5 | 스틱을 끝까지 좌/우로 계속 | 급하게 꺾이지 않고 큰 원을 그림. 중립이면 직진 |
| 6 | 행성 한 바퀴 | 카메라가 뒤에서 따라오고 위아래가 갑자기 뒤집히지 않음 |
| 7 | 시작 직후 화면 아래 | "自動で進む　左スティックで曲がる"(키보드면 A・Dキー). 잠시 조향하면 사라짐 |

### 12-2. 체력·속도·HUD (S02, S03, A03)
| # | 할 일 | 성공이면 |
|---|---|---|
| 1 | 시작 | 왼쪽 위 "体力" 막대 절반, 숫자 "5/10" |
| 2 | 10초 기다리기 | 숫자가 약 1 줄어듦(초당 0.1). 파리가 점점 느려짐 |
| 3 | 체력 3 이하 | 막대가 빨갛게 깜빡, 아래에 "りんごを探そう！" |
| 4 | 체력 3 아래로 더 | 속도는 최저 30%에서 더 느려지지 않음 |

### 12-3. 사과 (C02, A02, A01)
| # | 할 일 | 성공이면 |
|---|---|---|
| 1 | 시작해서 그대로 직진 | 바로 앞에 빨간 사과(둥실·회전) |
| 2 | 사과에 닿기 | 사과 사라짐, "+2"(초록) 팝업, 숫자 +2, 빨라짐, 반짝 파티클 |
| 3 | 체력 9.x에서 먹기 | 10에서 멈춤. 팝업은 실제 오른 양(예 "+0.6") |
| 4 | 먹은 자리에서 멀리(행성 반대편 쪽) 갔다 오기 | 같은 자리에 다시 1개(겹치지 않음) |
| 5 | 행성 곳곳 | 사과 지점 8곳. 장식용 빨간 과일은 더 이상 없음 |

### 12-4. 적 (C04~C08)
| # | 할 일 | 성공이면 |
|---|---|---|
| 1 | 2초 기다리기 | 1.5초 간격으로 적이 하나씩 나타나 총 6마리(잠자리 2, 거미 2, 수컷 1, 도토리 1). 사라지지 않고 계속 돎 |
| 2 | 잠자리/거미에 닿기 | 체력 −2(최대의 20%), "-2" 빨간 팝업, 깜빡임. 그 적은 2초 멈춤 |
| 3 | 닿은 채로 있기 | 매 프레임 줄지 않음(접촉 시작 1번만) |
| 4 | 도토리에 닿기 | 체력 −1(10%) |
| 5 | 체력 가득일 때 수컷이 쫓아옴 | 직진하면 떨쳐낼 수 있음(플레이어 6 > 수컷 4.2) |

### 12-5. 경고·회피 (E01, E02, P04, A04)
| # | 할 일 | 성공이면 |
|---|---|---|
| 1 | 적이 다가옴(거리 약 3) | 화면 네 변 빨간 테두리 + 위쪽 가운데 "Ⓐボタンで回避！"(패드 종류에 맞는 표기) + 슬로모션 |
| 2 | 적이 옆으로 지나가거나 멀어짐 | 경고가 바로 꺼짐 |
| 3 | 경고 없을 때 액션 버튼 | 아무 일도 없음 |
| 4 | 경고 중 스틱 오른쪽 + 액션 | 오른쪽으로 휙 이동 |
| 5 | 경고 중 중립 + 액션 | 적의 길에서 옆으로 비켜섬. 적이 닿지 않고 지나가면 "回避!" 1번 + 파티클 |
| 6 | 회피했는데 결국 맞음 | "回避!" 안 나옴(피격만) |
| 7 | 저체력 문구와 경고가 동시에 | 둘 다 읽힘(겹치지 않음) |
| 8 | 시트 `Dodge.neutralMode` = 1 → Pull → 5번 | 적이 가는 방향으로 이동(기획서 문구 그대로). 확인 후 0으로 |

### 12-6. 암컷·구애 (C03, P03, E03, E04, S06, A03, A05)
| # | 할 일 | 성공이면 |
|---|---|---|
| 1 | 암컷이 화면에 보이고 16 이내 | 머리 위에 분홍 하트 마크(둥실). 화면 밖이면 없음. 하트 방향 화살표 같은 안내는 없음 |
| 2 | 암컷에게 5 안으로 | 도망감. 약 8 밖이면 다시 배회(걷다 쉬다) |
| 3 | 체력 5로 쫓기 | 잘 안 잡힘(속도 같음) → 사과 먹고 쫓으면 잡힘 |
| 4 | 닿기 | 세상이 멈춤(적·체력·암컷). 경고 사라짐. 가운데 패널 "順番にボタンを押そう！" + 버튼 4칸 + "0／4　次はA" |
| 5 | 맞는 버튼 | 그 칸이 ♪로 바뀌고 노란 테두리가 다음 칸으로. 삑 소리(임시) |
| 6 | 틀린 버튼 | "求愛失敗" + "ボタンの順番が違った！" → 패널 닫힘 → 암컷이 도망 |
| 7 | 실패 직후 그대로 | 바로 다시 시작되지 않음. 2.5 이상 떨어졌다 다시 닿으면 재도전 |
| 8 | 전부 맞힘 | "求愛成功！" → 하트 파티클 → 결과 패널 "求愛成功！ / メスのハエに出会えた" |
| 9 | 패드 종류 바꾸기 | 칸의 글자가 기기 표기로(Switch A/B/X/Y, Xbox B/A/Y/X, PS ○/×/△/□, 키보드 화살표) |

### 12-7. 수컷·연타 (C07, P05, E05, E04, A05)
| # | 할 일 | 성공이면 |
|---|---|---|
| 1 | 수컷에 닿기 | 세상이 멈춤. 패널 "オスと連打バトル！", "Ⓐを連打！", "0／25", 게이지·시간 막대. 체력은 안 줄어듦 |
| 2 | 버튼을 길게 누르고 있기 | 1만 오름 |
| 3 | 6초 안에 25번 | "連打成功！" → 탐색 복귀. 수컷 2초 멈춤, 바로 다시 배틀 안 됨 |
| 4 | 6초 동안 조금만 | "時間切れ…" → 탐색 복귀 후 체력 −1 |
| 5 | 체력 1 이하에서 실패 | 게임 오버(결과 패널) |
| 6 | 배틀 중 Start/Esc | 일시정지 메뉴, 남은 시간 그대로 유지. 닫으면 이어서 |

### 12-8. 일시정지·패드 (S07, P06)
| # | 할 일 | 성공이면 |
|---|---|---|
| 1 | 탐색 중 Start(+) / Esc | 어두운 화면 + "一時停止", つづける / タイトルに戻る. 모든 것이 멈춤 |
| 2 | 10초 두었다가 つづける | 체력·위치·적·하늘이 그대로였다가 이어짐 |
| 3 | つづける를 액션 버튼으로 | 그 입력으로 회피가 나가지 않음 |
| 4 | 경고(슬로) 중 일시정지 → 재개 | 슬로모션으로 돌아옴(정상 속도로 튀지 않음) |
| 5 | タイトルに戻る | 타이틀. 타이틀이 정상 속도 |
| 6 | 패드 → 키보드 → 패드 번갈아 | 안내 문구·버튼 표기가 마지막 기기로 바뀜 |

### 12-9. 게임 오버·결과·재도전 (S04, S05, A06)
| # | 할 일 | 성공이면 |
|---|---|---|
| 1 | 체력이 자연 감소로 0 | 1.5초 뒤 결과 패널 "体力がなくなった！ / 赤りんごで体力を回復しよう" |
| 2 | 적에 맞아 0 | 같은 패널(1번만 뜸) |
| 3 | 결과 패널 | 회피를 했다면 "あなたの最速の回避反応: 0.xx秒", 연구 데이터 줄, "回避! × n", FLY FACT 한 개 |
| 4 | もういちど | 처음부터(체력 5, 위치, 사과 전부, 적 다시 등장) |
| 5 | タイトルに戻る | 타이틀 |

### 12-10. 전체
| # | 할 일 | 성공이면 |
|---|---|---|
| 1 | 일본어/영어 각각 한 판 | 빠진 글자(□) 없음 |
| 2 | 각 `SYS_*`를 하나씩 끄고 Play | 그 기능만 빠지고 에러 없음 |
| 3 | 전체 | Console 에러 0 |

### 12-11. 시트 연동
| # | 할 일 | 성공이면 |
|---|---|---|
| 1 | `Player.turnRate` 75 → 40, Pull, Play | 더 크게 돎 |
| 2 | `HazardTypes` spider count 2 → 5, Pull, Play | 거미 5마리 |
| 3 | `Courtship.length` 4 → 6, `timeLimit` 0 → 5, Pull | 버튼 6칸 + 시간 막대 |
| 4 | `Text` 탭 문구 하나 수정, Pull | 게임에 반영 |
| 5 | 확인 후 전부 되돌리고 Pull | |

---

## 13. 사운드 담당용 이벤트 표 (A07)

사운드는 이 브랜치에 없다. 담당자는 새 기능 폴더(예: `Features/Audio`, `SYS_Audio.prefab`)에서 아래 `GameEvents`를 구독하면 된다. 다른 기능 코드를 고칠 필요 없음.

| 기획서 SE | 이벤트 (`GameEvents.`) | 인자 | 비고 |
|---|---|---|---|
| 비행 루프 | `RoundStarted` 시작 / `RoundEnded` 정지 / `GamePaused(bool)` 일시정지 | | 속도에 맞춰 피치를 바꾸려면 `GameContext.Current.Player.CurrentSpeed` |
| 적 접근(경고) | `WarningChanged(IMovingObstacle)` | null이 아니게 **바뀔 때** 1번 | 매 프레임 오지 않음(바뀔 때만 발생) → "警戒音が毎フレーム重ならない" 충족 |
| 회피 동작 | `PlayerDodged(pos, dir)` | | |
| 회피 성공 | `DodgeSucceeded` | | `PerfectDodge`도 같은 순간 |
| 피격 | `PlayerHit` (또는 `PlayerHitBy(src)`) | | 수컷 접촉도 오므로 수컷은 `RivalBattleStarted`로 구분 |
| 체력 변화 | `HealthChanged(current, max)` | | 저체력 경고음이 필요하면 여기서 비율 확인 |
| 사과 먹기 | `AppleEaten(pos)` | | |
| 싸움(연타) | `RivalBattleStarted` / `RivalPressed(count, target)` / `RivalBattleEnded(won)` | | |
| 구애 | `CourtshipStarted` / `CourtshipInput(correct)` / `CourtshipFinished(success, pos)` | | `CourtshipQTE`에 임시 삑 소리가 있다(코드 생성). 정식 SE가 생기면 `CourtshipQTE`의 `audioSource.PlayOneShot` 두 줄을 지운다 |
| 구애 성공 | `CourtshipFinished(true, …)` 또는 `RoundEnded(true)` | | |
| 게임 오버 | `PlayerDied(reason)` | "Hit" / "Starved" | |
| 메뉴 이동/결정 | `MenuMoved` / `MenuSelected` | | |
| 일시정지 | `GamePaused(bool)` | | 일시정지 중엔 `Time.timeScale` = 0 → `AudioSource`는 `ignoreListenerPause` 또는 `AudioListener.pause`로 처리 |

---

## 14. 튜닝 값 전체 (시트 탭)

### `Player`
| key | 기본 | 의미 |
|---|---|---|
| maxSpeed | 6 | 체력 가득일 때 속도. 적·암컷 속도의 기준("플레이어 최대 속도") |
| turnRate | 75 | 스틱 끝까지 기울였을 때 회전 속도(°/초). 낮을수록 크게 돎 |
| steerDeadZone | 0.15 | 이보다 작은 스틱 기울기는 무시 |

### `Session`
| key | 기본 | 의미 |
|---|---|---|
| dayLength | 60 | 하늘 하루(아침→밤) 초. 반복. 라운드를 끝내지 않음 |
| resultDelay | 1.5 | 끝나고 결과 패널까지 초 |

### `Health`
| key | 기본 | 의미 |
|---|---|---|
| maxHp | 10 | 최대 체력 |
| startHp | 5 | 시작 체력 |
| drainPerSecond | 0.1 | 탐색 중 초당 감소(이벤트·일시정지 중 정지). 0 = 끔 |
| obstacleDamageRatio | 0.2 | 자기 값이 없는 장애물의 데미지(최대 체력 비율) |
| minSpeedRatio | 0.3 | 속도 하한(최대 속도 비율). 0이면 정확히 최대 × 체력 비율 |
| invulnerableTime | 1.5 | 맞은 뒤 무적(깜빡임) 초 |
| blinkInterval | 0.08 | 깜빡임 간격 |
| lowHpRatio | 0.3 | 이 비율 이하면 저체력 표시 |

### `Items`
| key | 기본 | 의미 |
|---|---|---|
| healRatio | 0.2 | 사과 1개 회복(최대 체력 비율). 최대에서 멈춤 |
| respawnDistance | 20 | 먹은 지점에서 이만큼(직선) 멀어지면 같은 곳에 다시 |
| pickupRadius | 1.0 | 먹는 거리 |
| hoverHeight | 0.6 | 지면 위 높이 |
| bobHeight / bobSpeed | 0.12 / 2.5 | 위아래 둥실 |
| spinSpeed | 90 | 회전(°/초) |

### `Mate`
| key | 기본 | 의미 |
|---|---|---|
| speedRatio | 0.5 | 암컷 속도 = 플레이어 최대 속도 × 이 값 |
| fleeRadius | 5 | 이 거리(직선) 안이면 도망 |
| releaseRadius | 8 | 이 거리 밖이면 배회로 복귀(fleeRadius보다 크게) |
| failFleeSeconds | 2 | 구애 실패 후 강제 도망 초 |
| walkTimeMin / Max | 1.5 / 3 | 배회: 걷는 시간 |
| pauseTimeMin / Max | 0.4 / 1.0 | 배회: 쉬는 시간 |
| wanderTurn | 70 | 배회: 걸을 때마다 무작위 회전(°) |
| leashAngle | 100 | 플레이어와 이 각도(행성 둘레) 이상 멀면 다음 걸음은 플레이어 쪽으로 |

### `Courtship`
| key | 기본 | 의미 |
|---|---|---|
| length | 4 | 순서대로 누를 버튼 수(최대 8칸 표시) |
| buttonKinds | 4 | 나올 수 있는 버튼 종류(1~4). 2면 오른쪽·아래 버튼만 |
| timeLimit | 0 | 전체 제한 시간 초. 0 = 없음 |
| inputDelay | 0.3 | 시작 직후 입력 무시 초 |
| resultShowSeconds | 1.2 | 성공·실패 문구 표시 초 |

### `Rival`
| key | 기본 | 의미 |
|---|---|---|
| targetPresses | 25 | 목표 횟수 |
| timeLimit | 6 | 제한 시간 초 |
| failDamageRatio | 0.1 | 실패 시 데미지(최대 체력 비율, 10의 0.1 = 1) |
| inputDelay | 0.2 | 시작 직후 입력 무시 초 |
| resultShowSeconds | 1.0 | 결과 문구 표시 초 |

### `Dodge`
| key | 기본 | 의미 |
|---|---|---|
| distance | 2.2 | 회피 이동 거리 |
| duration | 0.22 | 회피 시간(이 동안 무적) |
| cooldown | 0.4 | 다음 회피까지 초 |
| yawKick | 25 | 옆 회피 때 몸 회전(°) |
| steerDeadZone | 0.3 | 이보다 작은 스틱은 중립 |
| neutralMode | 0 | 중립 회피: 0 = 적의 길에서 옆으로(권장), 1 = 적이 가는 방향(기획서 문구) |

### `Hazards`
| key | 기본 | 의미 |
|---|---|---|
| firstDelay | 2 | 첫 적까지 초 |
| spawnInterval | 1.5 | 적이 하나씩 나오는 간격(종류별 count를 채울 때까지) |
| spawnAngleMin / Max | 50 / 170 | 플레이어에게서 몇 도 떨어진 곳에 나오는지(180 = 반대편) |
| warnRadius | 3 | 이 거리(직선) 안에서 다가오는 적 = 경고 |
| warnApproachDot | 0.5 | 얼마나 정면으로 와야 하는지(1 = 정확히, 0 = 옆) |
| slowScale | 0.5 | 경고 중 게임 속도. 1 = 슬로 끔 |
| successTimeout | 3 | 회피 후 적이 지나가길 기다리는 최대 초 |

### `HazardTypes` (한 줄 = 적 한 종류, 0/1은 끔/켬)
| id | count | speedRatio | damageRatio | warn | rival | chase | detectRadius | loseRadius | turnRate | stopOnHit |
|---|---|---|---|---|---|---|---|---|---|---|
| dragonfly | 2 | 0.7 | 0.2 | 1 | 0 | 0 | 0 | 0 | 0 | 2 |
| spider | 2 | 0.7 | 0.2 | 1 | 0 | 0 | 0 | 0 | 0 | 2 |
| male | 1 | 0.7 | 0 | 1 | 1 | 1 | 6 | 9 | 120 | 2 |
| acorn | 1 | 0.7 | 0.1 | 1 | 0 | 0 | 0 | 0 | 0 | 0 |

- `rival` = 1이면 접촉 시 데미지 대신 연타 배틀(`damageRatio` 무시).
- `stopOnHit` = 접촉 후 그 자리에 멈추는 초(그동안 무해). 0이면 그냥 지나감.
- 새 적 종류 = 프리팹 하나 + `SYS_Hazards`의 `ObstacleSpawner.prefabs`에 id 등록 + 이 표에 한 줄.

### `Text`
키·영어·일본어. 6-2의 TSV와 같다. `{0}` 자리는 코드가 채운다(버튼 이름, 숫자).

---

## 15. 커밋 (사람)

**작업 전**(에이전트에게 스펙을 넘기기 전에):
```bash
cd ~/Developer/sgj2026-flygame
git switch dev
git pull
git switch -c CHI/05-planner-spec-all
```

**작업 후**(12장 확인 끝나고):
```bash
cd ~/Developer/sgj2026-flygame
git add FlyGame/Assets/_Project FlyGame/ProjectSettings/EditorBuildSettings.asset FlyGame/AGENTS.md
git restore --staged "FlyGame/Assets/_Project/Art/Fonts/Silver SDF.asset" FlyGame/Assets/_Project/Materials/M_PixelArtPost.mat 2>/dev/null
git status      # 삭제(ResultScreen, Backpedal, SYS_Backpedal, Result.unity)가 "deleted"로 보여야 함.
                # Silver SDF.asset, M_PixelArtPost.mat, Assets/Test.meta, Claude 출력물은 없어야 함
git commit -m "Implement planner spec M1-M4: auto-run, HP, apples, enemies, dodge, courtship, mash battle, menus"
git push -u origin CHI/05-planner-spec-all
```
PR: `CHI/05-planner-spec-all` → `dev`.
