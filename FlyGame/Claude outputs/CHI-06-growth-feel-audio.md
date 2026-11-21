# CHI/06-growth-feel-audio: 레벨업·대시·바나나·말풍선·이펙트·사운드·패드

| 항목 | 값 |
|---|---|
| 브랜치 | `CHI/06-growth-feel-audio` (`dev`에서 생성. CHI/05가 머지된 `dev`) |
| 기준 | 2026-11-22 그릴 세션 결정 (이 문서 1장). 기획서 M1~M4 구현(CHI/05) 위에 덧붙인다 |
| 권장 커밋 메시지 | `Add levels, fever, forward dash, bananas, speech bubbles, wind and dash FX, audio and pad rumble` |
| 건드리지 않는 것 | `CourtshipFlow.cs`, `RivalBattle.cs`, `ObstacleHitDetector.cs`, `Loc.cs`, `TextTable.cs`, `LocalizedText.cs`, Balance Sync 툴, `VillageGenerator.cs`, 셰이더, 적 프리팹 모양 |

> ## ⚠️ 에이전트 실행 모드: **끝까지 무정지**
> 사람(지효재)은 자리를 비운다. **모든 결정과 승인은 이 문서로 이미 끝났다.**
> - AGENTS.md 4장 1번(체크포인트에서 멈추고 사람 확인 대기)과 3번(애매하면 질문)은 **이 스펙에서는 적용하지 않는다.** 멈추지 말고 1단계부터 4단계까지 연달아 진행한다.
> - 애매한 점은 이 문서의 의도(2장)에 가장 맞는 쪽으로 **직접 정하고**, 보고서(9장)의 "판단 기록"에 적는다.
> - 막힌 항목은 건너뛰고 "미완료"에 이유와 함께 적은 뒤, 상관없는 다음 항목을 계속한다.
> - 나머지 규칙은 그대로다: 스펙 코드는 원문 그대로(고쳤으면 무엇을 왜), **git 명령 금지**, 스펙 밖 파일·패키지 추가 금지, `FindObjectOfType` 류 금지, 끝나면 Console 에러 0 + 모든 씬·프리팹 저장.
> - Balance Sync의 **Pull 버튼은 누르지 않는다**(시트가 옛 형식이라 새 값을 덮는다. 10장).

---

## 0. 이 문서 읽는 법

| 장 | 내용 | 누가 |
|---|---|---|
| 0-1 | 실행 전 준비 (2분) | 사람 |
| 1 | 확정된 결정 | 모두 |
| 2 | 구현 전략 — 왜 이렇게 짰는지 | 모두 |
| 3 | 파일 변경 목록 | 에이전트 |
| 4 | **단계 1: 코드** | 에이전트 |
| 5 | **단계 2: 에셋** (밸런스, 텍스트, 머티리얼, 오디오) | 에이전트 |
| 6 | **단계 3: 프리팹·씬** | 에이전트 |
| 7 | **단계 4: 자가 검증** | 에이전트 |
| 8 | AGENTS.md 수정 | 에이전트 |
| 9 | 보고 형식 | 에이전트 |
| 10 | 시트 작업 | 사람 (돌아와서) |
| 11 | 체크포인트 | 사람 |
| 12 | 튜닝 값 전체 | 기획 |
| 13 | 3분 밸런스 시뮬레이션 | 기획 |
| 14 | 커밋 | 사람 |

### 0-1. 사람: 실행 전 준비

1. 브랜치:
   ```bash
   cd ~/Developer/sgj2026-flygame
   git switch dev && git pull
   git switch -c CHI/06-growth-feel-audio
   ```
2. **음악·효과음 파일**을 `FlyGame/Assets/_Project/Audio/` 폴더에 넣는다(폴더가 없으면 만든다). 파일 이름에 아래 단어 중 하나가 들어 있으면 에이전트가 알아서 연결한다(5-4). 이름이 달라도 괜찮다. 못 찾은 것은 보고서에 남고, 나중에 인스펙터에서 끌어다 놓으면 된다.

   | 파일 | 이름에 들어 있으면 되는 단어 (아무거나) |
   |---|---|
   | 배경음 (3분) | `bgm`, `music`, `배경`, `BGM` |
   | 승리 (30초) | `victory`, `win`, `clear`, `승리`, `勝利` |
   | 힐 | `heal`, `힐`, `回復` |
   | 데미지 | `damage`, `hit`, `데미지`, `ダメージ` |
   | 회피 | `dodge`, `회피`, `回避` |
   | 획득 | `get`, `pickup`, `item`, `획득`, `獲得` |
3. Antigravity CLI는 명령·파일 쓰기를 **매번 묻지 않도록**(자동 승인) 설정해서 실행한다. 그래야 자리를 비워도 끝까지 간다.
4. Unity 에디터를 열어 둔다(MCP 연결).

---

## 1. 확정된 결정 (2026-11-22 그릴 세션)

| 분류 | 결정 |
|---|---|
| 남성 파리 무한 경고 | 원인: 회피 후에도 계속 쫓아와서 "다가오는 적" 판정이 안 꺼짐. → 회피당한 수컷은 **1초 정지**(무해) 후 **3초 동안 직진만**(추적 쉼). 정지하는 순간 회피 성공(回避!) |
| 수컷 강해짐 | 날이 지날수록 +10% 속도·쉬는 시간 −20%, **밤에는** 속도 ×1.25·쉬는 시간 ×0.5. 밤에 수컷 눈이 **빨갛게 맥동**, 테두리도 붉게 |
| 수컷 등장 | 게임 시작 **20초 뒤** (종류별 `firstDelay`) |
| 대시 | 스페이스/액션 = **바라보는 방향(앞)으로 대시**. 경고 중에만. 거리 3, 0.3초 무적 → 적을 **뚫고 지나가면** 회피 성공. 좌우 회피·중립 모드 삭제 |
| 액션 버튼 | **아래 버튼**(Xbox Ⓐ / PS × / Switch Ⓑ) / Space. 엑스박스 패드 기준 "Ⓐボタンで回避！" |
| 바나나 | 사과와 같은 8지점에서 무작위. 다시 나올 때마다 추첨. 바나나 확률은 **레벨이 오를수록 증가**(5%→33%). 회복 40%·EXP 3 (사과 20%·EXP 1). 모델은 Unity 기본 도형으로 |
| 아이템 둥실 | 더 빠르고 크게: 높이 0.35, 속도 4.5 |
| 레벨 | 레벨 1~10. EXP = 사과 1, 바나나 3, 회피 성공 1. 필요 EXP는 레벨이 오를수록 증가. 레벨마다 **최대 속도·회전 속도·최대 체력** 증가(더 자유롭게 움직임). 체력 막대 아래 작은 EXP 막대 + 왼쪽 `Lv.3` + 숫자 `3/5` |
| 체력→속도 | **미세하게만**: 체력 0이어도 최대 15% 느려짐(85~100%). 최대 속도는 레벨이 정함 |
| 체력 막대 | 최대 체력 1당 30px → **레벨업마다 막대가 길어짐**. 데미지: 초록이 즉시 줄고 빨간 잔상이 0.4초 뒤 부드럽게 줄어듦. 회복: 0.3초에 걸쳐 차오름. 색: 50% 초과 초록, 50% 이하 노랑, 20% 이하 주황 깜빡임 + 「おなかがペコペコ…」 |
| 피버 | 레벨업 시 3초: 무적, 지면에서 떠서 위아래로 와리가리 + 날갯짓, 속도 ×1.2, 경고 없음, 구애 시작 안 됨. 화면 가장자리가 노랗게 은은히 밝아졌다 어두워짐. 「レベルアップ！」 팝업 |
| 암컷 | 처음부터 존재(행성 반대편). **Lv.3 전에는 수줍음**: 18 안으로 오면 멀리서부터 피함(속도 4.4 > 플레이어 3.6~3.9). Lv.3부터 평소(5 안이면 도망). 하트 마커는 **항상** 화면에 위치 표시(화면 밖이면 가장자리), 가까울수록 커짐 |
| 구애 난이도 | 레벨이 낮을수록 춤이 길고 빠름(Lv.1~3: 8개 2.5~3초 → 사실상 실패, Lv.7: 4개 6초, Lv.8+: 4개 제한 없음). 어려운 춤 실패 시 말풍선 「もっと強くならなきゃ…」 |
| 플레이 시간 | 목표 **3분**. 하루 **80초**(밤 1:02~1:20, 2:22~2:40) |
| 말풍선 | 주인공 머리 위 UI, 픽셀 말풍선, Silver 폰트. 밤: 「夜は、なんだかこわく見える…」→「オスが強くなってる…！」, 수컷 첫 등장 「オスが来た！」, 암컷 첫 발견 「メスだ！」, Lv.3 「今なら近づけそう…」, 어려운 춤 실패 「もっと強くならなきゃ…」 |
| 카메라 | 스틱 중립(직진) **1초** → 바로 넓은 시야로 멀어짐. 조향·경고·이벤트면 즉시 복귀 |
| 이펙트 | 바람: 4~8초마다 물결치는 줄기 1~2개가 화면을 스치고 꽃가루가 쓸림. 대시: 머리→꼬리로 링 4개가 빠르게 지나감(운석 진입처럼 주황→빨강, 진하지 않게, 픽셀 느낌). 연못: 물결 링 |
| 연못 | **2곳**. 들어가면 몸이 살짝 잠김(0.15). 지금은 막혀 있는 연못을 통과 가능하게 |
| 가시 | 기획서에 없음 → **삭제** |
| 아이템 겹침 | 사과 지점에 `KeepClearZone` → 소품이 그 근처에 안 생김 |
| 사운드 | 배경음 3분 하루종일 반복(타이틀·게임 이어서), 일시정지 시 50%. 승리 30초는 구애 성공 시. 힐=아이템, 데미지=피격·연타 실패, 회피=회피 성공, 획득=구애 정답·연타·메뉴. 레벨업·피버 등은 빈 칸(나중에 넣음). 설정은 인스펙터 |
| 패드 | 엑스박스 패드. 진동: 피격(짧게), 레벨업(조금 길게). 안 되는 환경은 조용히 무시 |
| 타이틀 | 제목 이미지 칸: `TitleScreen`의 `titleSprite`에 넣으면 이미지, 비우면 텍스트 |

---

## 2. 구현 전략

### 2-1. 큰 그림

```
 Core (FlyJam.Core)
   PlayerProgress (정적 스냅샷: 현재 레벨 한 줄 LevelInfo + Fever)   ← Level 기능이 쓰고, 모두가 읽음
   SphereWalker.MaxSpeedOverride / TurnRateOverride                 ← Level 기능이 레벨 속도·회전을 넣음
   VisualOffset (모델 높이 오프셋 합산: "fever" + "pond")            ← Level(떠오르기), Village(잠기기)
   FlyVisual.Flying (날갯짓 자세)                                    ← Level 피버
   GameSession.IsNight / DayIndex + NightChanged                     ← 수컷 강화, 밤 말풍선
   PlanetFollowCamera + CameraBalance (중립 1초 → 넓은 시야)
   GameEvents: Level(ExpRequested, ExpChanged, LevelChanged, FeverChanged), Bubble(BubbleRequested),
               Items(ItemEaten), Health(+MaxHpRequested), Threat(+EnemyAppeared), Day(NightChanged)

 Features (서로 모름, 이벤트·PlayerProgress로만)
   Level ── EXP·레벨·피버·EXP 막대·팝업·노란 테두리   (새 기능, SYS_Level)
   Bubble ─ 말풍선 큐 + 밤 트리거                      (새 기능, SYS_Bubble)
   Audio ── 음악·효과음·진동 (DontDestroyOnLoad)       (새 기능, SYS_Audio: Title·Game 둘 다)
   Health ─ MaxHpRequested로 최대 체력 증가, 미세한 속도, 새 막대
   Items ── ItemTypes(사과·바나나), 바나나 확률 = PlayerProgress
   Hazards  종류별 firstDelay·절대 속도·stopOnDodge·chaseCooldown·낮밤 강화, NightGlowEyes
   Dodge ── 앞으로 대시
   Mate ─── 절대 속도, 수줍음 모드, 항상 보이는 하트 마커
   Courtship 레벨별 길이·시간 (PlayerProgress)
   Fx ───── 대시 링, 바람 줄기
   Village ─ WaterZone(연못 2곳), PondSink(잠김·물결)
```

원칙(지금까지와 같음): 기능끼리는 `GameEvents`로만, 기획 숫자는 `BalanceTable`(→ 시트), `SYS_*` 안에 씬 참조 0, 새 기능 = 새 폴더 + asmdef + `SYS_*`.

### 2-2. 레벨 정보를 어떻게 나누나 — `PlayerProgress`

레벨 하나가 바꾸는 것이 여러 기능에 흩어져 있다(속도 → Core, 최대 체력 → Health, 바나나 확률 → Items, 구애 난이도 → Courtship, 피버 중 금지 → Hazards·Mate). 기능끼리 참조하면 의존이 엉키므로:
- Core에 `LevelInfo` 구조체(시트 `LevelTable` 한 줄)와 정적 `PlayerProgress`(현재 줄 + `Fever`)를 둔다.
- Level 기능만 쓴다(`Set`, `SetFever`). 다른 기능은 읽기만 한다.
- `SYS_Level`이 꺼져 있으면 `PlayerProgress.Active = false` → 모두 자기 시트 값(옛 방식)으로 동작한다. 기능 하나를 꺼도 에러 0 원칙 유지.
- 값이 "바뀌는 순간"이 필요한 곳(최대 체력, EXP 막대, 팝업, 진동)은 이벤트(`MaxHpRequested`, `ExpChanged`, `LevelChanged`)로 알린다.

### 2-3. 속도 = 레벨 × 체력(미세)

```
실제 속도 = LevelTable.maxSpeed(레벨) × (피버면 1.2) × [speedAtZeroHp + (1 − speedAtZeroHp) × 체력비율]
          = 3.6 ~ 6.3                              × (0.85 ~ 1.0)
```
- `SphereWalker.MaxSpeedOverride`(Level) × `SpeedMultiplier`(Health). 회전도 `TurnRateOverride` = 70°/s → 160°/s.
- 회전 반경 = 속도 ÷ 회전 속도: Lv.1 약 2.9 → Lv.10 약 2.3. **빨라지는데도 더 작게 꺾인다** = 레벨이 오를수록 "더 자유롭게" 움직인다.
- 적·암컷 속도는 이제 **절대값**(시트). 플레이어만 성장하므로 레벨업이 그대로 느껴진다.

### 2-4. 최대 체력 증가와 체력 막대

- `PlayerLevel`이 레벨업 때 `MaxHpRequested(새 최대, grow: true)` → `PlayerHealth`가 최대를 바꾸고 **늘어난 만큼 현재 체력도 더한다**(새 칸이 채워진 채로). 라운드 시작 때는 `grow: false`(최대만 설정).
- `HealthBar`가 `HealthChanged(현재, 최대)`의 최대로 막대 폭(`labelWidth + 최대 × 30px`)을 0.35초에 걸쳐 늘린다.
- 데미지 잔상: `Inner/Chip`(빨강)이 `Fill`(초록) 뒤에 있다. 큰 감소(≥0.05)면 초록은 바로, 빨강은 0.4초 기다렸다 부드럽게 따라온다. 매 프레임 자연 감소는 잔상 없이 바로 따라온다.

### 2-5. 피버

- `PlayerLevel.StartFever`: `PlayerProgress.SetFever(true)`, `SetInvulnerable`(→ `ObstacleHitDetector`가 접촉 무시 → 적 통과, 수컷 배틀도 안 걸림), `FlyVisual.Flying = true`(날갯짓 자세), 속도 ×1.2, `VisualOffset.Set("fever", 높이)`.
- 높이 = (feverLift + sin × feverBobHeight) × 이징(처음·끝 0.3초에 오르고 내림).
- `NearMissDirector`는 피버 중 경고를 끄고, `MateGoal`은 구애를 시작하지 않는다(떠 있는 채로 구애가 시작되면 어색하므로 착지 후).
- 세션 일시정지 중엔 피버 시간도 멈춘다. 라운드가 끝나면 즉시 종료.
- 노란 테두리: `FeverVignette`(RawImage, 코드로 만든 64×36 저해상도 텍스처 + Point 필터 + 4단계 알파 → 픽셀 느낌)가 `FeverChanged`를 듣고 숨 쉬듯 밝아졌다 어두워진다.

### 2-6. 대시와 회피 성공 판정

- `DodgeAbility`: 경고 중 액션 → 바라보는 방향으로 3을 0.3초에(초당 10), 그동안 무적.
- 정면에서 오는 적(속도 3.4)과의 상대 속도 ≈ 13.4/초 → 경고 시작 거리 3에서 눌러도 0.22초에 교차, 대시가 끝날 때 이미 1.07 떨어져 있다(접촉 반경 0.75) → 맞지 않는다.
- `NearMissDirector` 성공 조건: 대시 때의 대상이 **(a) 더 이상 플레이어 쪽으로 오지 않고 (b) 몸 반경 + 0.6 이상 떨어졌을 때**. (b)가 없으면 "뚫고 지나가는 중"에 성공이 먼저 뜨고, 무적이 끝난 뒤 겹쳐 맞는 경우가 생긴다(E02 위반).
- `stopOnDodge > 0`인 적(수컷)은 대시 순간 `RollingObstacle.OnDodged()`로 1초 정지(무해) → **그 자리에서 바로 성공**. 정지가 끝나면 `chaseCooldown` 동안 추적하지 않는다 → 경고가 끊긴다(무한 경고 해결).

### 2-7. 수컷 강화 (낮밤)

`RollingObstacle.Strength = (1 + daySpeedUp × 날짜) × (밤이면 1 + nightSpeedUp)` → 속도에 곱한다. 쉬는 시간 = `chaseCooldown × max(0.2, 1 − dayCooldownCut × 날짜) × (밤이면 1 − nightCooldownCut)`. 값은 `HazardTypes` 열이라 다른 적에게도 줄 수 있다(지금은 수컷만). 날짜·밤 판정은 `GameSession.DayIndex`/`IsNight`(시트 `Session.nightStart`), 하늘(`DayNightCycle`)도 같은 값을 쓰게 바꿔 화면과 판정이 맞는다.

| 시점 | 수컷 속도 | 쉬는 시간 |
|---|---|---|
| 1일 낮 (0:20~1:02) | 3.8 | 3.0초 |
| 1일 밤 (1:02~1:20) | 4.75 | 1.5초 |
| 2일 낮 | 4.18 | 2.4초 |
| 2일 밤 (2:22~2:40) | 5.23 | 1.2초 |
| 3일 낮 | 4.56 | 1.8초 |

### 2-8. 말풍선

- 아무 기능이나 `GameEvents.RaiseBubbleRequested(키, 우선순위)`. 화면의 것보다 높으면 교체, 아니면 줄을 선다(최대 4). 2.5초 표시, 0.25초 간격.
- 세션 일시정지(구애·연타·일시정지 메뉴) 중엔 숨고 시간도 멈춘다. 라운드 끝에 비운다.
- 말풍선 그림은 코드로 만든 12×12 9-slice(1px 테두리, 둥근 모서리) + 7×4 꼬리, 1 텍셀 = 4px. 위치도 4px 단위로 맞춰 픽셀이 흔들리지 않는다.
- 우선순위: 밤 1, 수컷·암컷 발견 2, Lv.3·어려운 춤 실패 3.

### 2-9. 사운드 — `AudioDirector` 하나

- `SYS_Audio`를 Title과 Game 두 씬에 둔다. 먼저 살아 있는 것이 `DontDestroyOnLoad`로 남고 나중 것은 스스로 지운다 → 타이틀→게임→재도전 동안 **배경음이 끊기지 않는다**.
- 슬롯 = 게임 이벤트 하나. 비어 있으면 무음. 효과음은 모두 1초 미만이라 `PlayOneShot` 한 소스로 충분.
- 수컷 접촉(데미지 0, 연타 배틀 시작)은 데미지음을 내지 않는다(`IDamageSource.DamageRatio == 0`).
- 경고음 슬롯은 경고가 **켜지는 순간 1번**만.
- 진동은 같은 프리팹의 `RumbleDirector`(Input System `SetMotorSpeeds`). 지원 안 되면 아무 일도 없다.
- `CourtshipQTE`의 임시 삑 소리(코드 생성)는 삭제.

### 2-10. 카메라

기존 "멈추면 멀리 보기"는 오토런에서 발동할 일이 없었다. 이제 `AutoRun` 중 스틱 중립이 `idleSeconds`(1초) 이어지면 같은 넓은 시야로 빠진다(멀어질 땐 약 1.2초, 돌아올 땐 약 0.4초). 경고 중·이벤트 중(`SuppressLookOut`)엔 멀어지지 않는다.

### 2-11. 이펙트를 픽셀처럼 보이게

대시 링·바람·물결은 `LineRenderer`로 **월드에** 그린다. 월드 카메라는 저해상도 렌더 텍스처로 그려지고 픽셀 후처리를 거치므로 자동으로 픽셀화된다. 머티리얼은 기존 `M_ParticleAdd`(빛나는 가산), `M_Particle`(반투명). 알파는 낮게(0.35~0.6) — "너무 진하지 않게".

### 2-12. 3분 밸런스

13장 시뮬레이션: 봇 200판(최적 경로, 회피 70%, 연타 승률 70%) → 클리어 95%, 중앙값 **169초**, 빠른 10% 113초, 90초 미만 2%. Lv.3(암컷이 다가갈 수 있게 됨) 약 22초, Lv.5 약 2분 10초. 사람은 봇보다 느리므로 3분 남짓 예상. 전부 시트로 조절.

---

## 3. 파일 변경 목록

경로는 `FlyGame/Assets/_Project/` 기준. **신규** / **교체**(파일 전체를 스펙 코드로) / **삭제**(파일과 `.meta` 함께).

### 3-1. 코드

| 파일 | 구분 | 요약 |
|---|---|---|
| `Scripts/Core/Events/GameEvents.Level.cs` | 신규 | `ExpRequested`, `ExpChanged`, `LevelChanged`, `FeverChanged` |
| `Scripts/Core/Events/GameEvents.Bubble.cs` | 신규 | `BubbleRequested` |
| `Scripts/Core/Events/GameEvents.Day.cs` | 신규 | `NightChanged` |
| `Scripts/Core/Events/GameEvents.Health.cs` | 교체 | + `MaxHpRequested(max, grow)` |
| `Scripts/Core/Events/GameEvents.Items.cs` | 교체 | `AppleEaten` → `ItemEaten(id, pos)` |
| `Scripts/Core/Events/GameEvents.Threat.cs` | 교체 | + `EnemyAppeared` |
| `Scripts/Core/Player/PlayerProgress.cs` | 신규 | `LevelInfo`, `PlayerProgress` |
| `Scripts/Core/Player/PlayerMoveBalance.cs` | 교체 | Level 없을 때의 대비값(3.6, 70) |
| `Scripts/Core/Player/SphereWalker.cs` | 교체 | + `MaxSpeedOverride`, `TurnRateOverride` |
| `Scripts/Core/Render/VisualOffset.cs` | 신규 | 모델 높이 오프셋 합산 |
| `Scripts/Core/Render/FlyVisual.cs` | 교체 | + `Flying` |
| `Scripts/Core/Camera/CameraBalance.cs` | 신규 | 시트 `Camera` |
| `Scripts/Core/Camera/PlanetFollowCamera.cs` | 교체 | 중립 1초 → 넓은 시야 |
| `Scripts/Core/Session/SessionBalance.cs` | 교체 | 하루 80초, `nightStart` |
| `Scripts/Core/Session/GameSession.cs` | 교체 | `NightStart`, `DayIndex`, `IsNight`, `NightChanged` |
| `Scripts/Core/Session/InputCompat.cs` | 교체 | 액션 = 아래 버튼, 표기 Ⓐ(Xbox)/×(PS)/Ⓑ(Switch) |
| `Scripts/Features/Level/*` (asmdef + 6개) | 신규 | `LevelBalance`, `PlayerLevel`, `ExpBar`, `LevelUpPopup`, `FeverVignette` |
| `Scripts/Features/Bubble/*` (asmdef + 3개) | 신규 | `PixelBubbleArt`, `SpeechBubble`, `BubbleTriggers` |
| `Scripts/Features/Audio/*` (asmdef + 2개) | 신규 | `AudioDirector`, `RumbleDirector` |
| `Scripts/Features/Health/HealthBalance.cs` | 교체 | `speedAtZeroHp`, `midHpRatio`, `lowHpRatio 0.2` |
| `Scripts/Features/Health/PlayerHealth.cs` | 교체 | 최대 체력 증가, 미세한 속도 |
| `Scripts/Features/Health/HealthBar.cs` | 교체 | 길이·잔상·회복 애니메이션·3색 |
| `Scripts/Features/Items/ItemBalance.cs` | 교체 | `ItemTypes` 표, 둥실 값 |
| `Scripts/Features/Items/AppleSpawner.cs` | 교체 | 사과·바나나 추첨 (클래스 이름 유지) |
| `Scripts/Features/Hazards/HazardBalance.cs` | 교체 | 절대 속도, `firstDelay`, `stopOnDodge`, `chaseCooldown`, 낮밤 강화 열 |
| `Scripts/Features/Hazards/RollingObstacle.cs` | 교체 | `OnDodged`, 쉬는 시간, `Strength` |
| `Scripts/Features/Hazards/ObstacleSpawner.cs` | 교체 | 종류별 등장 시각, 절대 속도, 수컷 첫 등장 말풍선 |
| `Scripts/Features/Hazards/NearMissDirector.cs` | 교체 | 피버 중 경고 없음, 뚫고 지나가기 성공 판정, 정지=즉시 성공 |
| `Scripts/Features/Hazards/NightGlowEyes.cs` | 신규 | 밤에 수컷 눈 빨강 맥동 |
| `Scripts/Features/Dodge/DodgeBalance.cs` | 교체 | 거리 3, 0.3초 |
| `Scripts/Features/Dodge/DodgeAbility.cs` | 교체 | 앞으로 대시 |
| `Scripts/Features/Mate/MateBalance.cs` | 교체 | 절대 속도 4.4, 수줍음 |
| `Scripts/Features/Mate/MateAI.cs` | 교체 | 수줍음 모드, 절대 속도 |
| `Scripts/Features/Mate/MateGoal.cs` | 교체 | 피버 중 구애 시작 안 함 |
| `Scripts/Features/Mate/MateFinderHint.cs` | 교체 | 항상 표시, 가장자리 고정, 거리별 크기, 첫 발견 말풍선 |
| `Scripts/Features/Courtship/CourtshipBalance.cs` | 교체 | 툴팁만 |
| `Scripts/Features/Courtship/CourtshipQTE.cs` | 교체 | 레벨별 길이·시간, 삑 소리 삭제, 실패 말풍선 |
| `Scripts/Features/Fx/FxDirector.cs` | 교체 | `ItemEaten` |
| `Scripts/Features/Fx/DashRingFx.cs` | 신규 | 대시 링 |
| `Scripts/Features/Fx/WindGusts.cs` | 신규 | 바람 줄기 + 꽃가루 밀기 |
| `Scripts/Features/Sky/DayNightCycle.cs` | 교체 | 밤 시작을 `GameSession.NightStart`에서 |
| `Scripts/Features/Village/WaterZone.cs` | 신규 | 연못 범위 |
| `Scripts/Features/Village/PondSink.cs` | 신규 | 잠기기 + 물결 |
| `Scripts/UI/TitleScreen.cs` | 교체 | 제목 이미지 칸 |
| `Scripts/UI/ResultPanel.cs` | 교체 | 결과에 "到達レベル" 줄 |

### 3-2. 에셋

| 에셋 | 구분 |
|---|---|
| `Data/Balance/LevelBalance.asset`, `CameraBalance.asset` | 신규 |
| `Data/Balance/PlayerMoveBalance`, `SessionBalance`, `HealthBalance`, `ItemBalance`, `MateBalance`, `DodgeBalance`, `HazardBalance` | 새 기본값으로 초기화 |
| `Data/Balance/Resources/TextTable.asset` | 52줄로 교체 |
| `Materials/M_Banana.mat`, `M_BananaStem.mat` | 신규 |
| `Prefabs/Banana.prefab` | 신규 |
| `Prefabs/SYS_Level.prefab`, `SYS_Bubble.prefab`, `SYS_Audio.prefab` | 신규 |
| `Prefabs/Fly`, `Enemy_Male`, `SYS_Items`, `SYS_Health`, `SYS_Hazards`, `SYS_Mate`, `SYS_Fx`, `SYS_Village` | 수정 |
| `Prefabs/Thorn.prefab` (+ 다른 곳에서 안 쓰면 `M_Thorn`, `M_ThornBase`, `M_HaloThorn`) | **삭제** |
| `Scenes/Game.unity`, `Scenes/Title.unity` | 수정 |
| `Audio/*` (사람이 넣은 파일) | 그대로 (슬롯에 연결만) |
| `AGENTS.md` | 8장 |

삭제는 AGENTS.md 6장대로: 참조를 먼저 끊고(6-10) → 셸로 파일과 `.meta`를 지우고 → `AssetDatabase.Refresh()`. 셸 삭제가 막히면 "미완료"에 적는다.

---

## 4. 단계 1 — 코드

**아래 파일을 모두 쓴 다음 한 번에 컴파일한다.** 중간 컴파일 에러(예: `FxDirector`가 아직 `AppleEaten`을 찾음)는 정상이다.
- `.cs`는 UTF-8. 교체 파일의 `.meta`는 건드리지 않는다(GUID 유지 → 프리팹 연결 유지).
- 클래스 이름을 바꾸지 않은 것(`AppleSpawner`, `NearMissDirector`, `RollingObstacle`, `MateFinderHint`, `CourtshipQTE`)은 이 GUID 때문이다.

**자가 확인 1**: 컴파일 후 Console 에러 0. 에러가 있으면 단계 2로 가지 않고 고친다(무엇을 왜 고쳤는지 기록).


### 4-1. Core — 이벤트

#### `Scripts/Core/Events/GameEvents.Level.cs` (신규)

```csharp
using System;

namespace FlyJam
{
    public static partial class GameEvents
    {
        public static event Action<int> ExpRequested;               // EXP to add (items, dodge success)
        public static event Action<int, int, int> ExpChanged;       // exp in this level, exp needed (0 = max level), level
        public static event Action<int, int> LevelChanged;          // new level, previous level (same value at round start)
        public static event Action<bool> FeverChanged;              // level-up fever on / off

        public static void RaiseExpRequested(int amount) => ExpRequested?.Invoke(amount);
        public static void RaiseExpChanged(int exp, int need, int level) => ExpChanged?.Invoke(exp, need, level);
        public static void RaiseLevelChanged(int level, int previous) => LevelChanged?.Invoke(level, previous);
        public static void RaiseFeverChanged(bool on) => FeverChanged?.Invoke(on);
    }
}
```

#### `Scripts/Core/Events/GameEvents.Bubble.cs` (신규)

```csharp
using System;

namespace FlyJam
{
    public static partial class GameEvents
    {
        /// Speech bubble above the player: text key, priority (higher replaces a lower one on screen; equal waits in line).
        public static event Action<string, int> BubbleRequested;

        public static void RaiseBubbleRequested(string key, int priority) => BubbleRequested?.Invoke(key, priority);
    }
}
```

#### `Scripts/Core/Events/GameEvents.Day.cs` (신규)

```csharp
using System;

namespace FlyJam
{
    public static partial class GameEvents
    {
        public static event Action<bool, int> NightChanged;   // isNight, day index (0 = first day). Raised by GameSession when it flips

        public static void RaiseNightChanged(bool night, int day) => NightChanged?.Invoke(night, day);
    }
}
```

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
        public static event Action<float, bool> MaxHpRequested;         // new max HP (Level feature), grow = also add the increase to current HP (false at round start)

        public static void RaisePlayerHitBy(IObstacle source) => PlayerHitBy?.Invoke(source);
        public static void RaiseHealthChanged(float current, float max) => HealthChanged?.Invoke(current, max);
        public static void RaisePlayerDied(string reason) => PlayerDied?.Invoke(reason);
        public static void RaiseHealRequested(float ratio) => HealRequested?.Invoke(ratio);
        public static void RaiseDamageRequested(float ratio, string reason) => DamageRequested?.Invoke(ratio, reason);
        public static void RaiseMaxHpRequested(float max, bool grow) => MaxHpRequested?.Invoke(max, grow);
    }
}
```

#### `Scripts/Core/Events/GameEvents.Items.cs` (교체)

```csharp
using System;
using UnityEngine;

namespace FlyJam
{
    public static partial class GameEvents
    {
        public static event Action<string, Vector3> ItemEaten;   // item id ("apple", "banana"), where it was

        public static void RaiseItemEaten(string id, Vector3 p) => ItemEaten?.Invoke(id, p);
    }
}
```

#### `Scripts/Core/Events/GameEvents.Threat.cs` (교체)

```csharp
using System;

namespace FlyJam
{
    public static partial class GameEvents
    {
        public static event Action<IMovingObstacle> WarningChanged;   // the enemy you are warned about; null = warning over
        public static event Action DodgeSucceeded;                    // dodged during a warning and the enemy passed (or stopped) without touching
        public static event Action<string> EnemyAppeared;              // enemy type id, raised once per spawned enemy

        public static void RaiseWarningChanged(IMovingObstacle target) => WarningChanged?.Invoke(target);
        public static void RaiseDodgeSucceeded() => DodgeSucceeded?.Invoke();
        public static void RaiseEnemyAppeared(string id) => EnemyAppeared?.Invoke(id);
    }
}
```

### 4-2. Core — 플레이어·렌더·카메라

#### `Scripts/Core/Player/PlayerProgress.cs` (신규)

```csharp
using UnityEngine;

namespace FlyJam
{
    /// Numbers the current level gives other features (one row of the "Level" sheet table).
    [System.Serializable]
    public struct LevelInfo
    {
        public int level;              // 1, 2, 3 ...
        public int expToNext;          // EXP needed for the next level. 0 or less = max level
        public float maxSpeed;         // player speed at full HP
        public float turnRate;         // degrees per second at full stick tilt
        public float maxHp;            // max HP at this level
        public float rareItemChance;   // 0-1: chance that a respawning item is a rare one (banana)
        public int courtLength;        // courtship buttons at this level
        public float courtTime;        // courtship time limit (seconds). 0 = none
    }

    /// Read-only snapshot of player growth, written by the Level feature.
    /// Other features read it instead of referencing the Level feature (no asmdef dependency).
    /// Without SYS_Level: Active = false and everyone uses their own sheet values.
    public static class PlayerProgress
    {
        public static bool Active { get; private set; }
        public static LevelInfo Info { get; private set; }
        public static int Level => Active ? Info.level : 1;
        /// Level-up fever: invulnerable, flying, no warnings, no courtship start.
        public static bool Fever { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Clear();

        public static void Set(LevelInfo info)
        {
            Info = info;
            Active = true;
        }

        public static void SetFever(bool on) => Fever = on;

        public static void Clear()
        {
            Active = false;
            Fever = false;
            Info = default;
        }
    }
}
```

#### `Scripts/Core/Player/PlayerMoveBalance.cs` (교체)

```csharp
using UnityEngine;

namespace FlyJam
{
    /// Planner numbers for player movement. Sheet tab "Player".
    /// With the Level feature (SYS_Level) on, speed and turn rate come from the "Level" table instead;
    /// these two are the fallback when it is off. Health scales speed slightly (Health.speedAtZeroHp).
    public class PlayerMoveBalance : BalanceTable
    {
        public override string SheetName => "Player";

        [Tooltip("Fallback speed when the Level feature is off (units per second)")]
        [SerializeField] float maxSpeed = 3.6f;
        [Tooltip("Fallback turn speed at full stick tilt when the Level feature is off, degrees per second")]
        [SerializeField] float turnRate = 70f;
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
        /// Speed before SpeedMultiplier. The Level feature sets MaxSpeedOverride (> 0) from the level table.
        public float MoveSpeed => MaxSpeedOverride > 0f ? MaxSpeedOverride : balance != null ? balance.MaxSpeed : moveSpeed;
        /// AutoRun turn speed at full tilt (degrees per second). The Level feature sets TurnRateOverride (> 0).
        public float TurnRate => TurnRateOverride > 0f ? TurnRateOverride : balance != null ? balance.TurnRate : tankTurnSpeed;
        /// > 0 replaces MoveSpeed (Level feature: level speed x fever bonus). 0 = off.
        public float MaxSpeedOverride { get; set; }
        /// > 0 replaces TurnRate (Level feature). 0 = off.
        public float TurnRateOverride { get; set; }
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

#### `Scripts/Core/Render/VisualOffset.cs` (신규)

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace FlyJam
{
    /// Adds up vertical offsets for the model child ("Visual") from several features without them fighting:
    /// Level fever lifts it ("fever"), ponds sink it ("pond"). Put on the Fly root (the Mate variant inherits it).
    [DefaultExecutionOrder(100)]
    public class VisualOffset : MonoBehaviour
    {
        [Tooltip("The model child to move. Empty = child named 'Visual'")]
        [SerializeField] Transform target;

        readonly Dictionary<string, float> offsets = new Dictionary<string, float>();
        Vector3 basePos;
        bool ready;

        void Awake()
        {
            if (target == null) target = transform.Find("Visual");
            if (target != null)
            {
                basePos = target.localPosition;
                ready = true;
            }
        }

        /// Sets one named offset (local up, units). 0 removes its effect.
        public void Set(string key, float y)
        {
            if (!string.IsNullOrEmpty(key)) offsets[key] = y;
        }

        public float Get(string key) => offsets.TryGetValue(key, out float y) ? y : 0f;

        void LateUpdate()
        {
            if (!ready) return;
            float sum = 0f;
            foreach (var kv in offsets) sum += kv.Value;
            target.localPosition = basePos + Vector3.up * sum;
        }
    }
}
```

#### `Scripts/Core/Render/FlyVisual.cs` (교체)

```csharp
using UnityEngine;

namespace FlyJam
{
    /// Procedurally animates the fly using only SphereWalker.State and CurrentSpeed.
    /// Replace only this component when switching to a Blender model + Animator later.
    public class FlyVisual : MonoBehaviour
    {
        [SerializeField] SphereWalker walker;
        /// Reverses leg gait phase when the visual faces backward (kept for compatibility, unused).
        public bool ReverseGait { get; set; }
        /// Flying pose: wings flap fast, legs tucked, body bobs (Level feature: level-up fever).
        public bool Flying { get; set; }
        [SerializeField] Transform bodyRoot;
        [Tooltip("Order: L1, R1, L2, R2, L3, R3 (1 = front legs)")]
        [SerializeField] Transform[] legs = new Transform[6];
        [SerializeField] Transform wingL, wingR;
        [SerializeField] Transform antennaL, antennaR;

        [Header("Walk")]
        [SerializeField] float stepsPerUnit = 1.6f;   // Step cycles per meter traveled
        [SerializeField] float legSwingAngle = 25f;
        [SerializeField] float legLiftAngle = 15f;
        [SerializeField] float bodyBob = 0.015f;
        [SerializeField] bool invertSwing = false;     // Check if legs swing in the wrong direction
        [SerializeField] bool invertLift = false;

        [Header("Idle")]
        [SerializeField] Vector2 groomInterval = new Vector2(2.5f, 5f);
        [SerializeField] float groomDuration = 1.2f;
        [SerializeField] Vector2 twitchInterval = new Vector2(0.4f, 1.2f);
        [SerializeField] float twitchAngle = 15f;

        [Header("Celebrate")]
        [SerializeField] float wingSpreadAngle = 60f;
        [SerializeField] float wingFlapAngle = 50f;
        [SerializeField] float wingFlapFrequency = 22f;

        [SerializeField] float blend = 12f;            // Pose blend/return speed

        Quaternion[] legRest;
        Quaternion wingLRest, wingRRest, antLRest, antRRest;
        Quaternion antLTarget, antRTarget;
        Vector3 bodyRest;
        float phase, groomTimer, groomT = -1f, twitchTimer;

        // Whether leg i is on the fly's left side (even index = L)
        static bool IsLeft(int i) => i % 2 == 0;
        // Tripod gait: Group A = L1, R2, L3 (0, 3, 4) / Group B = R1, L2, R3
        static bool IsTripodA(int i) => i == 0 || i == 3 || i == 4;

        void Awake()
        {
            legRest = new Quaternion[legs.Length];
            for (int i = 0; i < legs.Length; i++) legRest[i] = legs[i].localRotation;
            wingLRest = wingL.localRotation;
            wingRRest = wingR.localRotation;
            antLRest = antLTarget = antennaL.localRotation;
            antRRest = antRTarget = antennaR.localRotation;
            bodyRest = bodyRoot.localPosition;
            groomTimer = Random.Range(groomInterval.x, groomInterval.y);
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (Flying && (walker.State == FlyState.Walking || walker.State == FlyState.Stopped))
            {
                Celebrate(dt);
                return;
            }
            switch (walker.State)
            {
                case FlyState.Walking: Walk(dt); break;
                case FlyState.Stopped: Idle(dt); break;
                case FlyState.Dodging: Celebrate(dt); break;
                case FlyState.Celebrating: Celebrate(dt); break;
                default: ReturnToRest(dt); break;
            }
        }

        float K(float dt) => 1f - Mathf.Exp(-blend * dt);

        void Walk(float dt)
        {
            groomT = -1f;
            float s = ReverseGait ? -walker.SignedSpeed : walker.SignedSpeed;
            phase += s * stepsPerUnit * dt * Mathf.PI * 2f;
            float sSign = invertSwing ? -1f : 1f;
            float lSign = invertLift ? -1f : 1f;

            for (int i = 0; i < legs.Length; i++)
            {
                float p = phase + (IsTripodA(i) ? 0f : Mathf.PI);
                float side = IsLeft(i) ? 1f : -1f;
                float swing = Mathf.Sin(p) * legSwingAngle * side * sSign;
                float lift = Mathf.Max(0f, Mathf.Cos(p)) * legLiftAngle * -side * lSign;
                legs[i].localRotation = legRest[i] * Quaternion.Euler(0f, swing, lift);
            }

            bodyRoot.localPosition = bodyRest + Vector3.up * Mathf.Abs(Mathf.Sin(phase * 2f)) * bodyBob;
            FoldWings(dt);
            Antennae(dt);
        }

        void Idle(float dt)
        {
            bodyRoot.localPosition = Vector3.Lerp(bodyRoot.localPosition, bodyRest, K(dt));
            FoldWings(dt);
            Antennae(dt);

            // Grooming: occasionally rubs its face with both front legs
            if (groomT < 0f)
            {
                groomTimer -= dt;
                if (groomTimer <= 0f) groomT = 0f;
            }

            for (int i = 0; i < legs.Length; i++)
            {
                Quaternion target = legRest[i];
                bool frontLeg = i < 2;
                if (frontLeg && groomT >= 0f)
                {
                    float side = IsLeft(i) ? 1f : -1f;
                    float env = Mathf.Sin(groomT * Mathf.PI);                 // 0→1→0
                    float rub = Mathf.Sin(groomT * Mathf.PI * 8f) * 12f * env; // rubbing motion
                    float yaw = (70f * env + rub) * side * (invertSwing ? -1f : 1f);
                    float roll = 40f * env * -side * (invertLift ? -1f : 1f);
                    target = legRest[i] * Quaternion.Euler(0f, yaw, roll);
                    legs[i].localRotation = target;
                }
                else
                {
                    legs[i].localRotation = Quaternion.Slerp(legs[i].localRotation, target, K(dt));
                }
            }

            if (groomT >= 0f)
            {
                groomT += dt / groomDuration;
                if (groomT >= 1f)
                {
                    groomT = -1f;
                    groomTimer = Random.Range(groomInterval.x, groomInterval.y);
                }
            }
        }

        void Celebrate(float dt)
        {
            float t = Time.time * wingFlapFrequency * Mathf.PI * 2f;
            float flap = Mathf.Sin(t) * wingFlapAngle;
            wingL.localRotation = wingLRest * Quaternion.Euler(0f, -wingSpreadAngle, flap);
            wingR.localRotation = wingRRest * Quaternion.Euler(0f, wingSpreadAngle, -flap);

            bodyRoot.localPosition = bodyRest + Vector3.up * (0.03f * Mathf.Sin(Time.time * 8f));
            for (int i = 0; i < legs.Length; i++)
                legs[i].localRotation = Quaternion.Slerp(legs[i].localRotation, legRest[i], K(dt));
            Antennae(dt);
        }

        void ReturnToRest(float dt)
        {
            float k = K(dt);
            for (int i = 0; i < legs.Length; i++)
                legs[i].localRotation = Quaternion.Slerp(legs[i].localRotation, legRest[i], k);
            FoldWings(dt);
            bodyRoot.localPosition = Vector3.Lerp(bodyRoot.localPosition, bodyRest, k);
        }

        void FoldWings(float dt)
        {
            float k = K(dt);
            wingL.localRotation = Quaternion.Slerp(wingL.localRotation, wingLRest, k);
            wingR.localRotation = Quaternion.Slerp(wingR.localRotation, wingRRest, k);
        }

        void Antennae(float dt)
        {
            twitchTimer -= dt;
            if (twitchTimer <= 0f)
            {
                twitchTimer = Random.Range(twitchInterval.x, twitchInterval.y);
                antLTarget = antLRest * Quaternion.Euler(Random.Range(-twitchAngle, twitchAngle), Random.Range(-twitchAngle, twitchAngle), 0f);
                antRTarget = antRRest * Quaternion.Euler(Random.Range(-twitchAngle, twitchAngle), Random.Range(-twitchAngle, twitchAngle), 0f);
            }
            float k = 1f - Mathf.Exp(-20f * dt);
            antennaL.localRotation = Quaternion.Slerp(antennaL.localRotation, antLTarget, k);
            antennaR.localRotation = Quaternion.Slerp(antennaR.localRotation, antRTarget, k);
        }
    }
}
```

#### `Scripts/Core/Camera/CameraBalance.cs` (신규)

```csharp
using UnityEngine;

namespace FlyJam
{
    /// Planner numbers for the follow camera. Sheet tab "Camera".
    public class CameraBalance : BalanceTable
    {
        public override string SheetName => "Camera";

        [Tooltip("Seconds of neutral stick (going straight) before the camera pulls back for a wide view")]
        [SerializeField] float idleSeconds = 1f;
        [Tooltip("How fast it pulls back (bigger = faster). 2.5 = about 1.2 s")]
        [SerializeField] float zoomOutSharpness = 2.5f;
        [Tooltip("How fast it comes back when you steer (bigger = faster). 8 = about 0.4 s")]
        [SerializeField] float zoomInSharpness = 8f;
        [Tooltip("Wide view: distance behind the player")]
        [SerializeField] float lookOutDistance = 12f;
        [Tooltip("Wide view: height above the player")]
        [SerializeField] float lookOutHeight = 8.25f;

        public float IdleSeconds => Mathf.Max(0f, idleSeconds);
        public float ZoomOutSharpness => Mathf.Max(0.1f, zoomOutSharpness);
        public float ZoomInSharpness => Mathf.Max(0.1f, zoomInSharpness);
        public float LookOutDistance => lookOutDistance;
        public float LookOutHeight => lookOutHeight;
    }
}
```

#### `Scripts/Core/Camera/PlanetFollowCamera.cs` (교체)

```csharp
using UnityEngine;

namespace FlyJam
{
    /// Follows behind the player. Pulls back to a wide view when the player stops, or (auto-run) when the stick
    /// has been neutral for CameraBalance.idleSeconds. Comes back as soon as the player steers, warns or an event starts.
    [RequireComponent(typeof(Camera))]
    public class PlanetFollowCamera : MonoBehaviour
    {
        [SerializeField] SphereWalker target;
        [Tooltip("Sheet tab 'Camera'. Empty = the look-out values below, idle wide view after 1 s")]
        [SerializeField] CameraBalance balance;
        [SerializeField] float fieldOfView = 40f;
        [SerializeField] float distance = 6.75f;
        [SerializeField] float height = 3.3f;
        [SerializeField] float lookAhead = 2.2f;
        [SerializeField] float lookHeight = 0.5f;
        [SerializeField] float headingFollow = 5f;     // Speed of turning to follow the body heading
        [SerializeField] float positionFollow = 8f;

        [Header("Look-Out When Stopped")]
        [SerializeField] float lookOutDistance = 12f;
        [SerializeField] float lookOutHeight = 8.25f;
        [SerializeField] float lookOutDelay = 0.35f;
        [SerializeField] float zoomSharpness = 3f;

        public bool SuppressLookOut { get; set; }

        Camera cam;
        Vector3 heading;
        bool initialized;
        float zoom;
        float neutralTime;
        bool warned;

        void Awake() => cam = GetComponent<Camera>();

        void OnEnable() => GameEvents.WarningChanged += OnWarning;
        void OnDisable() => GameEvents.WarningChanged -= OnWarning;
        void OnWarning(IMovingObstacle t) => warned = t != null;

        void LateUpdate()
        {
            if (target == null) return;
            if (!Mathf.Approximately(cam.fieldOfView, fieldOfView)) cam.fieldOfView = fieldOfView;

            Planet planet = target.Planet;
            Vector3 tp = target.transform.position;
            Vector3 up = planet.UpAt(tp);

            if (!initialized) heading = target.transform.forward;
            heading = Vector3.ProjectOnPlane(heading, up);
            if (heading.sqrMagnitude < 1e-4f) heading = Vector3.ProjectOnPlane(target.transform.forward, up);
            heading.Normalize();

            // Body-relative controls: follow behind the body heading unless dodging (when backing up, body heading stays unchanged so camera stays steady)
            if (target.State != FlyState.Dodging)
            {
                Vector3 tf = Vector3.ProjectOnPlane(target.transform.forward, up).normalized;
                heading = Vector3.Slerp(heading, tf, 1f - Mathf.Exp(-headingFollow * Time.deltaTime)).normalized;
            }

            // Auto-run never "stops": a neutral stick for idleSeconds counts as idle instead
            bool running = target.Mode == MoveMode.AutoRun && target.State == FlyState.Walking && target.InputEnabled;
            if (running && Mathf.Abs(InputCompat.Steer()) < 0.2f) neutralTime += Time.deltaTime;
            else neutralTime = 0f;
            float idleSeconds = balance != null ? balance.IdleSeconds : 1f;
            bool idle = running && neutralTime >= idleSeconds;
            bool stopped = target.State == FlyState.Stopped && target.TimeInState > lookOutDelay;
            bool lookOut = !SuppressLookOut && !warned && (stopped || idle);

            float sharp = balance != null ? (lookOut ? balance.ZoomOutSharpness : balance.ZoomInSharpness) : zoomSharpness;
            zoom = Mathf.Lerp(zoom, lookOut ? 1f : 0f, 1f - Mathf.Exp(-sharp * Time.deltaTime));
            float outDist = balance != null ? balance.LookOutDistance : lookOutDistance;
            float outHeight = balance != null ? balance.LookOutHeight : lookOutHeight;
            float dist = Mathf.Lerp(distance, outDist, zoom);
            float hgt = Mathf.Lerp(height, outHeight, zoom);

            Vector3 desired = tp - heading * dist + up * hgt;
            transform.position = initialized
                ? Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-positionFollow * Time.deltaTime))
                : desired;

            Vector3 look = tp + heading * lookAhead + up * lookHeight;
            transform.rotation = Quaternion.LookRotation(look - transform.position, up);
            initialized = true;
        }
    }
}
```

### 4-3. Core — 세션·입력

#### `Scripts/Core/Session/SessionBalance.cs` (교체)

```csharp
using UnityEngine;

namespace FlyJam
{
    /// Planner numbers for the round. Sheet tab "Session".
    public class SessionBalance : BalanceTable
    {
        public override string SheetName => "Session";

        [Tooltip("Seconds for one sky cycle (sunrise -> night). Repeats. Does not end the round")]
        [SerializeField] float dayLength = 80f;
        [Tooltip("Night starts at this point of the day (0-1). 0.778 of 80 s = 62 s")]
        [SerializeField] float nightStart = 0.778f;
        [Tooltip("Seconds between the end of play and the result panel")]
        [SerializeField] float resultDelay = 1.5f;

        public float DayLength => Mathf.Max(5f, dayLength);
        public float NightStart => Mathf.Clamp(nightStart, 0.5f, 0.95f);
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
        [Tooltip("Used only when 'balance' is empty")]
        [SerializeField] float nightStart = 0.778f;

        readonly HashSet<object> pausers = new HashSet<object>();

        public float Elapsed { get; private set; }
        public float DayLength => balance != null ? balance.DayLength : Mathf.Max(5f, dayLength);
        public float ResultDelay => balance != null ? balance.ResultDelay : resultDelay;
        public float NightStart => balance != null ? balance.NightStart : nightStart;
        public float Remaining => Mathf.Max(0f, DayLength - Elapsed);
        /// 0 on the first day, 1 on the second ...
        public int DayIndex => Mathf.FloorToInt(Elapsed / DayLength);
        /// True from NightStart to the end of each day.
        public bool IsNight => DayProgress >= NightStart;
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

        bool wasNight;

        void Update()
        {
            if (Ended || IsPaused) return;
            Elapsed += Time.deltaTime;
            bool night = IsNight;
            if (night != wasNight)
            {
                wasNight = night;
                GameEvents.RaiseNightChanged(night, DayIndex);
            }
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

#### `Scripts/Core/Session/InputCompat.cs` (교체)

```csharp
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine;

namespace FlyJam
{
    /// Face buttons by position, so the same physical button works on every pad. South = the "action" button.
    public enum FaceButton { East, South, North, West }

    /// Which button names to show on screen. Follows the device the player touched last.
    public enum PadLayout { Keyboard, Switch, Xbox, PlayStation }

    /// The only place that reads keyboard / gamepad. Gameplay code asks "what was pressed", never which device.
    /// Action button = bottom face button (Xbox A / PS × / Switch B) or Space / Enter.
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
                || (p != null && p.buttonSouth.wasPressedThisFrame);
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
                    case PadLayout.Switch: return "Ⓑ";
                    case PadLayout.Xbox: return "Ⓐ";
                    case PadLayout.PlayStation: return "×";
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

### 4-4. Features/Level (신규 기능)

#### `Scripts/Features/Level/FlyJam.Level.asmdef` (신규)

```json
{
    "name": "FlyJam.Level",
    "rootNamespace": "FlyJam",
    "references": [
        "FlyJam.Core",
        "Unity.TextMeshPro",
        "UnityEngine.UI"
    ],
    "autoReferenced": true
}
```

#### `Scripts/Features/Level/LevelBalance.cs` (신규)

```csharp
using UnityEngine;

namespace FlyJam
{
    /// Planner numbers for levels. Sheet tabs "Level" (key/value: EXP and fever) and "LevelTable" (one row per level).
    /// Each row: level, expToNext (0 = max level), maxSpeed, turnRate, maxHp, rareItemChance, courtLength, courtTime.
    public class LevelBalance : BalanceTable
    {
        public override string SheetName => "Level";

        [Tooltip("EXP for a successful dodge (回避!). Items give the EXP written in ItemTypes")]
        [SerializeField] int dodgeExp = 1;
        [Tooltip("Level-up fever: seconds of flying + invulnerability")]
        [SerializeField] float feverSeconds = 3f;
        [Tooltip("Fever: speed x this")]
        [SerializeField] float feverSpeedMul = 1.2f;
        [Tooltip("Fever: how high the fly floats above the ground")]
        [SerializeField] float feverLift = 1.0f;
        [Tooltip("Fever: up/down swing while floating")]
        [SerializeField] float feverBobHeight = 0.25f;
        [Tooltip("Fever: up/down swing speed (radians per second; 9 = about 1.4 swings per second)")]
        [SerializeField] float feverBobSpeed = 9f;
        [Tooltip("Fever: seconds to rise at the start and land at the end")]
        [SerializeField] float feverEaseSeconds = 0.3f;

        [SheetRows("LevelTable")]
        [SerializeField] LevelInfo[] levels =
        {
            new LevelInfo { level = 1,  expToNext = 3,  maxSpeed = 3.6f, turnRate = 70f,  maxHp = 10f, rareItemChance = 0.05f, courtLength = 8, courtTime = 2.5f },
            new LevelInfo { level = 2,  expToNext = 4,  maxSpeed = 3.9f, turnRate = 80f,  maxHp = 11f, rareItemChance = 0.08f, courtLength = 8, courtTime = 2.5f },
            new LevelInfo { level = 3,  expToNext = 5,  maxSpeed = 4.2f, turnRate = 90f,  maxHp = 12f, rareItemChance = 0.12f, courtLength = 8, courtTime = 3f },
            new LevelInfo { level = 4,  expToNext = 6,  maxSpeed = 4.5f, turnRate = 100f, maxHp = 13f, rareItemChance = 0.15f, courtLength = 7, courtTime = 3.5f },
            new LevelInfo { level = 5,  expToNext = 7,  maxSpeed = 4.8f, turnRate = 110f, maxHp = 14f, rareItemChance = 0.18f, courtLength = 6, courtTime = 5f },
            new LevelInfo { level = 6,  expToNext = 8,  maxSpeed = 5.1f, turnRate = 120f, maxHp = 15f, rareItemChance = 0.21f, courtLength = 5, courtTime = 6f },
            new LevelInfo { level = 7,  expToNext = 10, maxSpeed = 5.4f, turnRate = 130f, maxHp = 16f, rareItemChance = 0.24f, courtLength = 4, courtTime = 6f },
            new LevelInfo { level = 8,  expToNext = 12, maxSpeed = 5.7f, turnRate = 140f, maxHp = 17f, rareItemChance = 0.27f, courtLength = 4, courtTime = 0f },
            new LevelInfo { level = 9,  expToNext = 14, maxSpeed = 6.0f, turnRate = 150f, maxHp = 18f, rareItemChance = 0.30f, courtLength = 4, courtTime = 0f },
            new LevelInfo { level = 10, expToNext = 0,  maxSpeed = 6.3f, turnRate = 160f, maxHp = 19f, rareItemChance = 0.33f, courtLength = 4, courtTime = 0f },
        };

        public int DodgeExp => Mathf.Max(0, dodgeExp);
        public float FeverSeconds => Mathf.Max(0f, feverSeconds);
        public float FeverSpeedMul => Mathf.Max(0.1f, feverSpeedMul);
        public float FeverLift => feverLift;
        public float FeverBobHeight => feverBobHeight;
        public float FeverBobSpeed => feverBobSpeed;
        public float FeverEaseSeconds => Mathf.Max(0.01f, feverEaseSeconds);
        public LevelInfo[] Levels => levels;

        /// Highest level in the table (at least 1).
        public int MaxLevel
        {
            get
            {
                int m = 1;
                if (levels != null) foreach (var r in levels) if (r.level > m) m = r.level;
                return m;
            }
        }

        /// The row for this level. Missing row = the closest lower level's row (so a short table still works).
        public LevelInfo Row(int level)
        {
            LevelInfo best = new LevelInfo { level = 1, expToNext = 0, maxSpeed = 3.6f, turnRate = 70f, maxHp = 10f, courtLength = 4 };
            int bestLevel = int.MinValue;
            if (levels != null)
                foreach (var r in levels)
                    if (r.level <= level && r.level > bestLevel) { best = r; bestLevel = r.level; }
            best.level = level;
            if (level >= MaxLevel) best.expToNext = 0;
            return best;
        }
    }
}
```

#### `Scripts/Features/Level/PlayerLevel.cs` (신규)

```csharp
using UnityEngine;

namespace FlyJam
{
    /// Player level and EXP. EXP from items (ExpRequested) and successful dodges (LevelBalance.dodgeExp).
    /// Each level (LevelTable row) sets: top speed and turn rate (SphereWalker overrides), max HP (MaxHpRequested, the new part
    /// comes filled), rare item chance and courtship difficulty (read by other features through PlayerProgress).
    /// Level-up -> fever: invulnerable, flying (floats and swings up/down, wings flap), speed x feverSpeedMul,
    /// no warnings and no courtship start, for feverSeconds. Paused with the session.
    public class PlayerLevel : MonoBehaviour
    {
        public const string StatKey = "level";

        [SerializeField] LevelBalance balance;
        [SerializeField] SphereWalker player;     // Auto-filled from GameContext if null
        [SerializeField] GameSession session;     // Auto-filled from GameContext if null

        public int Level { get; private set; } = 1;
        public int Exp { get; private set; }
        public bool InFever => feverLeft > 0f;

        LevelInfo info;
        float feverLeft, feverTime;
        VisualOffset offset;
        FlyVisual visual;

        void OnEnable()
        {
            GameEvents.ExpRequested += AddExp;
            GameEvents.DodgeSucceeded += OnDodgeSucceeded;
            GameEvents.RoundEnded += OnRoundEnded;
        }

        void OnDisable()
        {
            GameEvents.ExpRequested -= AddExp;
            GameEvents.DodgeSucceeded -= OnDodgeSucceeded;
            GameEvents.RoundEnded -= OnRoundEnded;
            EndFever();
            PlayerProgress.Clear();
        }

        void Start()
        {
            var ctx = GameContext.Current;
            if (ctx != null)
            {
                if (player == null) player = ctx.Player;
                if (session == null) session = ctx.Session;
            }
            if (player != null)
            {
                offset = player.GetComponent<VisualOffset>();
                visual = player.GetComponentInChildren<FlyVisual>();
            }
            Level = 1;
            Exp = 0;
            Apply(true);
            GameEvents.RaiseLevelChanged(Level, Level);
        }

        void Apply(bool roundStart)
        {
            info = balance != null ? balance.Row(Level) : new LevelInfo { level = Level, maxSpeed = 3.6f, turnRate = 70f, maxHp = 10f, courtLength = 4 };
            PlayerProgress.Set(info);
            ApplySpeed();
            if (info.maxHp > 0f) GameEvents.RaiseMaxHpRequested(info.maxHp, !roundStart);
            GameEvents.RaiseExpChanged(Exp, info.expToNext, Level);
        }

        void ApplySpeed()
        {
            if (player == null) return;
            float mul = InFever && balance != null ? balance.FeverSpeedMul : 1f;
            player.MaxSpeedOverride = Mathf.Max(0.1f, info.maxSpeed) * mul;
            player.TurnRateOverride = Mathf.Max(1f, info.turnRate);
        }

        void OnDodgeSucceeded()
        {
            if (balance != null && balance.DodgeExp > 0) AddExp(balance.DodgeExp);
        }

        void AddExp(int amount)
        {
            if (amount <= 0 || (session != null && session.Ended)) return;
            Exp += amount;
            bool up = false;
            while (info.expToNext > 0 && Exp >= info.expToNext)
            {
                Exp -= info.expToNext;
                int prev = Level;
                Level++;
                Apply(false);
                GameEvents.RaiseLevelChanged(Level, prev);
                up = true;
            }
            if (info.expToNext <= 0) Exp = 0;   // max level: the bar stays full
            GameEvents.RaiseExpChanged(Exp, info.expToNext, Level);
            if (up) StartFever();
        }

        void StartFever()
        {
            if (balance == null || balance.FeverSeconds <= 0f) return;
            bool was = InFever;
            feverLeft = balance.FeverSeconds;
            if (!was) feverTime = 0f;
            PlayerProgress.SetFever(true);
            if (player != null) player.SetInvulnerable(this, true);
            if (visual != null) visual.Flying = true;
            ApplySpeed();
            if (!was) GameEvents.RaiseFeverChanged(true);
        }

        void EndFever()
        {
            bool was = feverLeft > 0f || PlayerProgress.Fever;
            feverLeft = 0f;
            PlayerProgress.SetFever(false);
            if (player != null) player.SetInvulnerable(this, false);
            if (visual != null) visual.Flying = false;
            if (offset != null) offset.Set("fever", 0f);
            ApplySpeed();
            if (was) GameEvents.RaiseFeverChanged(false);
        }

        void Update()
        {
            if (!InFever) return;
            if (session != null && session.IsPaused) return;   // keeps floating where it is

            float dt = Time.deltaTime;
            feverLeft -= dt;
            feverTime += dt;
            float ease = balance.FeverEaseSeconds;
            float k = Mathf.Min(Mathf.Clamp01(feverTime / ease), Mathf.Clamp01(Mathf.Max(0f, feverLeft) / ease));
            k = Mathf.SmoothStep(0f, 1f, k);
            float h = (balance.FeverLift + Mathf.Sin(feverTime * balance.FeverBobSpeed) * balance.FeverBobHeight) * k;
            if (offset != null) offset.Set("fever", h);
            if (feverLeft <= 0f) EndFever();
        }

        void OnRoundEnded(bool cleared)
        {
            SessionResult.SetStat(StatKey, Level);
            EndFever();
        }
    }
}
```

#### `Scripts/Features/Level/ExpBar.cs` (신규)

```csharp
using TMPro;
using UnityEngine;

namespace FlyJam
{
    /// Small EXP bar under the HP bar (A03): "Lv.3" on the left, "3/5" numbers on the right ("MAX" at the top level).
    public class ExpBar : MonoBehaviour
    {
        [SerializeField] TMP_Text levelText;     // "Lv.3"
        [SerializeField] RectTransform fill;     // stretched by anchorMax.x
        [SerializeField] TMP_Text valueText;     // "3/5"
        [SerializeField] float fillSharpness = 10f;

        float target, shown;
        int lastLevel = -1;

        void OnEnable()
        {
            GameEvents.ExpChanged += OnExp;
            Loc.Changed += Redraw;
        }

        void OnDisable()
        {
            GameEvents.ExpChanged -= OnExp;
            Loc.Changed -= Redraw;
        }

        int exp, need, level = 1;

        void OnExp(int e, int n, int l)
        {
            exp = e;
            need = n;
            level = l;
            target = n > 0 ? Mathf.Clamp01((float)e / n) : 1f;
            if (l != lastLevel)
            {
                // New level: the bar restarts from empty (or full at max level)
                shown = n > 0 ? 0f : 1f;
                lastLevel = l;
            }
            Redraw();
        }

        void Redraw()
        {
            if (levelText != null) levelText.text = Loc.Format("hud.lv", level);
            if (valueText != null) valueText.text = need > 0 ? exp + "/" + need : Loc.Get("hud.lvmax");
        }

        void Update()
        {
            shown = Mathf.Lerp(shown, target, 1f - Mathf.Exp(-fillSharpness * Time.unscaledDeltaTime));
            if (fill != null) fill.anchorMax = new Vector2(shown, fill.anchorMax.y);
        }
    }
}
```

#### `Scripts/Features/Level/LevelUpPopup.cs` (신규)

```csharp
using TMPro;
using UnityEngine;

namespace FlyJam
{
    /// "レベルアップ！" above the player's head on level-up: pops in, rises and fades.
    public class LevelUpPopup : MonoBehaviour
    {
        [SerializeField] TMP_Text text;
        [SerializeField] Camera worldCamera;      // Auto-filled from GameContext if null
        [SerializeField] SphereWalker player;     // Auto-filled from GameContext if null
        [SerializeField] float headHeight = 1.6f;
        [SerializeField] float showSeconds = 1.2f;
        [SerializeField] float risePixels = 70f;
        [SerializeField] Color color = new Color(1f, 0.85f, 0.3f, 1f);

        float t = -1f;

        void Start()
        {
            var ctx = GameContext.Current;
            if (ctx != null)
            {
                if (worldCamera == null) worldCamera = ctx.WorldCamera;
                if (player == null) player = ctx.Player;
            }
            if (text != null) text.enabled = false;
        }

        void OnEnable() => GameEvents.LevelChanged += OnLevel;
        void OnDisable() => GameEvents.LevelChanged -= OnLevel;

        void OnLevel(int level, int previous)
        {
            if (level <= previous || text == null) return;
            text.text = Loc.Get("level.up");
            t = 0f;
            text.enabled = true;
        }

        void LateUpdate()
        {
            if (t < 0f || text == null || worldCamera == null || player == null) return;
            t += Time.unscaledDeltaTime / Mathf.Max(0.05f, showSeconds);
            if (t >= 1f)
            {
                t = -1f;
                text.enabled = false;
                return;
            }
            Vector3 up = player.Planet.UpAt(player.transform.position);
            Vector3 vp = worldCamera.WorldToViewportPoint(player.transform.position + up * headHeight);
            float pop = t < 0.15f ? Mathf.Lerp(0.6f, 1.15f, t / 0.15f) : Mathf.Lerp(1.15f, 1f, Mathf.Clamp01((t - 0.15f) / 0.15f));
            text.rectTransform.position = new Vector3(vp.x * Screen.width, vp.y * Screen.height + risePixels * t, 0f);
            text.rectTransform.localScale = Vector3.one * pop;
            var c = color;
            c.a = t < 0.7f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.7f) / 0.3f);
            text.color = c;
        }
    }
}
```

#### `Scripts/Features/Level/FeverVignette.cs` (신규)

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace FlyJam
{
    /// Soft yellow glow on the screen edges during the level-up fever: fades in, breathes brighter / darker, fades out.
    /// The texture is made in code at low resolution with point filtering and a few alpha steps, so it looks pixelated.
    [RequireComponent(typeof(RawImage))]
    public class FeverVignette : MonoBehaviour
    {
        [SerializeField] Color color = new Color(1f, 0.88f, 0.3f, 1f);
        [SerializeField] float alphaLow = 0.18f;
        [SerializeField] float alphaHigh = 0.45f;
        [SerializeField] float pulseSpeed = 6f;
        [SerializeField] float fadeSeconds = 0.3f;
        [SerializeField] int textureWidth = 64;
        [SerializeField] int textureHeight = 36;
        [Tooltip("Width of the glow band as a fraction of the screen")]
        [SerializeField] float band = 0.28f;
        [Tooltip("Alpha steps (pixel look). 4 = 4 visible rings")]
        [SerializeField] int steps = 4;

        RawImage image;
        bool on;
        float level;   // 0..1 fade

        void Awake()
        {
            image = GetComponent<RawImage>();
            image.raycastTarget = false;
            image.texture = Build();
            Apply(0f);
        }

        void OnEnable() => GameEvents.FeverChanged += OnFever;
        void OnDisable() => GameEvents.FeverChanged -= OnFever;
        void OnFever(bool v) => on = v;

        void Update()
        {
            level = Mathf.MoveTowards(level, on ? 1f : 0f, Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeSeconds));
            float pulse = Mathf.Lerp(alphaLow, alphaHigh, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pulseSpeed));
            Apply(level * pulse);
        }

        void Apply(float a)
        {
            var c = color;
            c.a = a;
            image.color = c;
            image.enabled = a > 0.001f;
        }

        Texture2D Build()
        {
            int w = Mathf.Max(8, textureWidth), h = Mathf.Max(8, textureHeight);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "FeverVignette"
            };
            int n = Mathf.Max(1, steps);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = (x + 0.5f) / w, v = (y + 0.5f) / h;
                    float edge = Mathf.Min(Mathf.Min(u, 1f - u) * w / h, Mathf.Min(v, 1f - v));   // distance to nearest edge, height units
                    float a = Mathf.Clamp01(1f - edge / Mathf.Max(0.01f, band));
                    a = Mathf.Ceil(a * a * n) / n;                                                   // stepped falloff
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            tex.Apply();
            return tex;
        }
    }
}
```

### 4-5. Features/Bubble (신규 기능)

#### `Scripts/Features/Bubble/FlyJam.Bubble.asmdef` (신규)

```json
{
    "name": "FlyJam.Bubble",
    "rootNamespace": "FlyJam",
    "references": [
        "FlyJam.Core",
        "Unity.TextMeshPro",
        "UnityEngine.UI"
    ],
    "autoReferenced": true
}
```

#### `Scripts/Features/Bubble/PixelBubbleArt.cs` (신규)

```csharp
using UnityEngine;

namespace FlyJam
{
    /// Speech bubble art made in code (no image files): a 12x12 rounded box with a 1-pixel dark outline (9-slice border 4)
    /// and a 7x4 tail. Point filtering keeps the pixels sharp.
    public static class PixelBubbleArt
    {
        static Sprite box, tail;

        public static Color Fill = new Color(0.98f, 0.97f, 0.92f, 1f);
        public static Color Line = new Color(0.16f, 0.15f, 0.2f, 1f);

        public static Sprite Box()
        {
            if (box != null) return box;
            string[] rows =
            {
                "  ########  ",
                " #........# ",
                "#..........#",
                "#..........#",
                "#..........#",
                "#..........#",
                "#..........#",
                "#..........#",
                "#..........#",
                "#..........#",
                " #........# ",
                "  ########  ",
            };
            box = Make(rows, "PixelBubbleBox", new Vector4(4, 4, 4, 4));
            return box;
        }

        public static Sprite Tail()
        {
            if (tail != null) return tail;
            string[] rows =
            {
                "#.....#",
                " #...# ",
                "  #.#  ",
                "   #   ",
            };
            tail = Make(rows, "PixelBubbleTail", Vector4.zero);
            return tail;
        }

        static Sprite Make(string[] rows, string name, Vector4 border)
        {
            int w = rows[0].Length, h = rows.Length;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = name
            };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    char c = rows[y][x];
                    tex.SetPixel(x, h - 1 - y, c == '#' ? Line : c == '.' ? Fill : Color.clear);
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }
    }
}
```

#### `Scripts/Features/Bubble/SpeechBubble.cs` (신규)

```csharp
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlyJam
{
    /// The player's speech bubble above its head (UI, pixel bubble, Silver font).
    /// GameEvents.BubbleRequested(key, priority): a higher priority replaces the one on screen; otherwise it waits in line.
    /// Hidden (and its timer paused) while the session is paused: courtship, mash battle, pause menu. Cleared at round end.
    public class SpeechBubble : MonoBehaviour
    {
        [SerializeField] RectTransform root;      // bubble root (moves above the head)
        [SerializeField] Image box;               // Image Type = Sliced (sprite made in code)
        [SerializeField] Image tail;              // below the box
        [SerializeField] TMP_Text text;
        [SerializeField] Camera worldCamera;      // Auto-filled from GameContext if null
        [SerializeField] SphereWalker player;     // Auto-filled from GameContext if null
        [SerializeField] GameSession session;     // Auto-filled from GameContext if null
        [SerializeField] float headHeight = 1.3f;
        [SerializeField] float showSeconds = 2.5f;
        [SerializeField] float gapSeconds = 0.25f;
        [SerializeField] float maxWidth = 560f;
        [SerializeField] Vector2 padding = new Vector2(40f, 26f);
        [Tooltip("Screen pixels per bubble texel (pixel size)")]
        [SerializeField] float pixelScale = 4f;
        [SerializeField] int maxQueue = 4;

        struct Item { public string key; public int priority; }

        readonly List<Item> queue = new List<Item>();
        Item current;
        float left, gap;
        bool showing;

        public bool Showing => showing;
        public string CurrentKey => showing ? current.key : null;

        void Awake()
        {
            if (box != null)
            {
                box.sprite = PixelBubbleArt.Box();
                box.type = Image.Type.Sliced;
                box.pixelsPerUnitMultiplier = 1f / Mathf.Max(0.01f, pixelScale);   // 1 texel = pixelScale canvas units
                box.raycastTarget = false;
            }
            if (tail != null)
            {
                tail.sprite = PixelBubbleArt.Tail();
                tail.raycastTarget = false;
                tail.rectTransform.sizeDelta = new Vector2(7f * pixelScale, 4f * pixelScale);
            }
            if (text != null) text.raycastTarget = false;
            SetVisible(false);
        }

        void Start()
        {
            var ctx = GameContext.Current;
            if (ctx == null) return;
            if (worldCamera == null) worldCamera = ctx.WorldCamera;
            if (player == null) player = ctx.Player;
            if (session == null) session = ctx.Session;
        }

        void OnEnable()
        {
            GameEvents.BubbleRequested += OnRequest;
            GameEvents.RoundEnded += OnRoundEnded;
            Loc.Changed += Relayout;
        }

        void OnDisable()
        {
            GameEvents.BubbleRequested -= OnRequest;
            GameEvents.RoundEnded -= OnRoundEnded;
            Loc.Changed -= Relayout;
        }

        void OnRoundEnded(bool cleared)
        {
            queue.Clear();
            showing = false;
            SetVisible(false);
        }

        void OnRequest(string key, int priority)
        {
            if (string.IsNullOrEmpty(key) || (session != null && session.Ended)) return;
            var item = new Item { key = key, priority = priority };
            if (!showing && gap <= 0f) { Show(item); return; }
            if (showing && priority > current.priority) { Show(item); return; }
            if (queue.Count >= maxQueue) return;
            // keep the line ordered: higher priority first, same priority in arrival order
            int at = queue.Count;
            for (int i = 0; i < queue.Count; i++) if (queue[i].priority < priority) { at = i; break; }
            queue.Insert(at, item);
        }

        void Show(Item item)
        {
            current = item;
            showing = true;
            left = showSeconds;
            Relayout();
            SetVisible(true);
        }

        void Relayout()
        {
            if (!showing || text == null || root == null) return;
            text.text = Loc.Get(current.key);
            Vector2 pref = text.GetPreferredValues(text.text, maxWidth - padding.x, 0f);
            float w = Mathf.Min(maxWidth, pref.x + padding.x);
            float h = pref.y + padding.y;
            root.sizeDelta = new Vector2(Snap(w), Snap(h));
        }

        float Snap(float v) => Mathf.Ceil(v / pixelScale) * pixelScale;

        void SetVisible(bool v)
        {
            if (box != null) box.enabled = v;
            if (tail != null) tail.enabled = v;
            if (text != null) text.enabled = v;
        }

        void LateUpdate()
        {
            bool paused = session != null && session.IsPaused;
            if (!showing)
            {
                if (gap > 0f && !paused) gap -= Time.deltaTime;
                if (gap <= 0f && queue.Count > 0 && !paused)
                {
                    var next = queue[0];
                    queue.RemoveAt(0);
                    Show(next);
                }
                return;
            }

            SetVisible(!paused);
            if (paused) return;

            left -= Time.deltaTime;
            if (left <= 0f)
            {
                showing = false;
                gap = gapSeconds;
                SetVisible(false);
                return;
            }

            if (worldCamera == null || player == null || root == null) return;
            Vector3 up = player.Planet.UpAt(player.transform.position);
            Vector3 vp = worldCamera.WorldToViewportPoint(player.transform.position + up * headHeight);
            if (vp.z <= 0f) { SetVisible(false); return; }
            float x = Mathf.Round(vp.x * Screen.width / pixelScale) * pixelScale;
            float y = Mathf.Round(vp.y * Screen.height / pixelScale) * pixelScale;
            root.position = new Vector3(x, y, 0f);
        }
    }
}
```

#### `Scripts/Features/Bubble/BubbleTriggers.cs` (신규)

```csharp
using UnityEngine;

namespace FlyJam
{
    /// Story lines that come from the world itself (other features raise their own bubbles with BubbleRequested):
    /// nightfall -> "夜は、なんだかこわく見える…" then "オスが強くなってる…！".
    public class BubbleTriggers : MonoBehaviour
    {
        [SerializeField] string[] nightKeys = { "bubble.night.1", "bubble.night.2" };
        [SerializeField] int nightPriority = 1;

        void OnEnable() => GameEvents.NightChanged += OnNight;
        void OnDisable() => GameEvents.NightChanged -= OnNight;

        void OnNight(bool night, int day)
        {
            if (!night || nightKeys == null) return;
            foreach (var k in nightKeys)
                if (!string.IsNullOrEmpty(k)) GameEvents.RaiseBubbleRequested(k, nightPriority);
        }
    }
}
```

### 4-6. Features/Audio (신규 기능)

#### `Scripts/Features/Audio/FlyJam.Audio.asmdef` (신규)

```json
{
    "name": "FlyJam.Audio",
    "rootNamespace": "FlyJam",
    "references": [
        "FlyJam.Core",
        "Unity.InputSystem"
    ],
    "autoReferenced": true
}
```

#### `Scripts/Features/Audio/AudioDirector.cs` (신규)

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FlyJam
{
    /// All music and sound effects, set in the inspector only (drag clips in; an empty slot is silent).
    /// - Music: one looping track for title and game, kept playing across scene changes (DontDestroyOnLoad).
    ///   Pause menu: volume x pauseDuck. Courtship success: music fades out, the victory clip plays once;
    ///   the music comes back on the next scene load (retry / title).
    /// - Effects: one slot per game event (see the field names). Put SYS_Audio in both Title and Game scenes:
    ///   the first one alive stays, later copies remove themselves.
    public class AudioDirector : MonoBehaviour
    {
        [System.Serializable]
        public class Sfx
        {
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1f;
        }

        public static AudioDirector Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;

        [Header("Music")]
        [SerializeField] AudioClip music;
        [Range(0f, 1f)] [SerializeField] float musicVolume = 0.6f;
        [Range(0f, 1f)] [SerializeField] float pauseDuck = 0.5f;
        [SerializeField] float musicFadeSeconds = 1f;
        [SerializeField] AudioClip victory;
        [Range(0f, 1f)] [SerializeField] float victoryVolume = 0.8f;

        [Header("Effects")]
        [Range(0f, 1f)] [SerializeField] float sfxVolume = 1f;
        [SerializeField] Sfx itemEaten = new Sfx();          // apple / banana (heal)
        [SerializeField] Sfx damage = new Sfx();             // hit by an enemy, mash battle lost
        [SerializeField] Sfx dodgeSuccess = new Sfx();       // 回避!
        [SerializeField] Sfx dash = new Sfx();               // dash pressed
        [SerializeField] Sfx warning = new Sfx();            // danger warning starts
        [SerializeField] Sfx courtshipCorrect = new Sfx();   // right button in the dance
        [SerializeField] Sfx courtshipWrong = new Sfx();     // wrong button / time up
        [SerializeField] Sfx rivalPress = new Sfx();         // each press in the mash battle
        [SerializeField] Sfx rivalWin = new Sfx();           // mash battle won
        [SerializeField] Sfx menuMove = new Sfx();           // menu cursor moved
        [SerializeField] Sfx menuSelect = new Sfx();         // menu item chosen
        [SerializeField] Sfx pauseOpen = new Sfx();          // pause menu opened
        [SerializeField] Sfx levelUp = new Sfx();            // level up
        [SerializeField] Sfx fever = new Sfx();              // fever starts
        [SerializeField] Sfx bubble = new Sfx();             // speech bubble appears
        [SerializeField] Sfx gameOver = new Sfx();           // HP ran out

        AudioSource musicSource, sfxSource, jingleSource;
        float musicTarget, fadeSpeed;
        bool musicStopped, paused;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (transform.parent != null) transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            jingleSource = gameObject.AddComponent<AudioSource>();
            jingleSource.playOnAwake = false;
            StartMusic();
        }

        void OnEnable()
        {
            if (Instance != null && Instance != this) return;
            SceneManager.sceneLoaded += OnSceneLoaded;
            GameEvents.ItemEaten += OnItem;
            GameEvents.PlayerHitBy += OnHitBy;
            GameEvents.DamageRequested += OnDamageRequested;
            GameEvents.DodgeSucceeded += OnDodgeSucceeded;
            GameEvents.PlayerDodged += OnDash;
            GameEvents.WarningChanged += OnWarning;
            GameEvents.CourtshipInput += OnCourtInput;
            GameEvents.CourtshipFinished += OnCourtFinished;
            GameEvents.RivalPressed += OnRivalPressed;
            GameEvents.RivalBattleEnded += OnRivalEnded;
            GameEvents.MenuMoved += OnMenuMoved;
            GameEvents.MenuSelected += OnMenuSelected;
            GameEvents.GamePaused += OnPaused;
            GameEvents.LevelChanged += OnLevel;
            GameEvents.FeverChanged += OnFever;
            GameEvents.BubbleRequested += OnBubble;
            GameEvents.PlayerDied += OnDied;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            GameEvents.ItemEaten -= OnItem;
            GameEvents.PlayerHitBy -= OnHitBy;
            GameEvents.DamageRequested -= OnDamageRequested;
            GameEvents.DodgeSucceeded -= OnDodgeSucceeded;
            GameEvents.PlayerDodged -= OnDash;
            GameEvents.WarningChanged -= OnWarning;
            GameEvents.CourtshipInput -= OnCourtInput;
            GameEvents.CourtshipFinished -= OnCourtFinished;
            GameEvents.RivalPressed -= OnRivalPressed;
            GameEvents.RivalBattleEnded -= OnRivalEnded;
            GameEvents.MenuMoved -= OnMenuMoved;
            GameEvents.MenuSelected -= OnMenuSelected;
            GameEvents.GamePaused -= OnPaused;
            GameEvents.LevelChanged -= OnLevel;
            GameEvents.FeverChanged -= OnFever;
            GameEvents.BubbleRequested -= OnBubble;
            GameEvents.PlayerDied -= OnDied;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ---------- music ----------

        void StartMusic()
        {
            musicStopped = false;
            paused = false;
            if (music == null || musicSource == null) return;
            if (musicSource.clip != music) musicSource.clip = music;
            if (!musicSource.isPlaying)
            {
                musicSource.volume = 0f;
                musicSource.Play();
            }
            musicTarget = musicVolume;
            fadeSpeed = musicVolume / Mathf.Max(0.05f, musicFadeSeconds);
        }

        void OnSceneLoaded(Scene s, LoadSceneMode mode)
        {
            if (jingleSource != null) jingleSource.Stop();
            StartMusic();
        }

        void OnPaused(bool on)
        {
            paused = on;
            if (on) Play(pauseOpen);
            if (musicStopped) return;
            musicTarget = musicVolume * (on ? pauseDuck : 1f);
            fadeSpeed = musicVolume * 4f;
        }

        void Update()
        {
            if (musicSource == null) return;
            musicSource.volume = Mathf.MoveTowards(musicSource.volume, musicTarget, fadeSpeed * Time.unscaledDeltaTime);
            if (musicStopped && musicSource.isPlaying && musicSource.volume <= 0.001f) musicSource.Stop();
        }

        void OnCourtFinished(bool success, Vector3 pos)
        {
            if (!success) return;
            musicStopped = true;
            musicTarget = 0f;
            fadeSpeed = musicVolume / Mathf.Max(0.05f, musicFadeSeconds);
            if (victory != null && jingleSource != null) jingleSource.PlayOneShot(victory, victoryVolume);
        }

        // ---------- effects ----------

        void Play(Sfx s)
        {
            if (s == null || s.clip == null || sfxSource == null) return;
            sfxSource.PlayOneShot(s.clip, s.volume * sfxVolume);
        }

        void OnItem(string id, Vector3 pos) => Play(itemEaten);

        void OnHitBy(IObstacle src)
        {
            // The male's touch starts the mash battle and does no damage: no damage sound
            if (src is IDamageSource ds && ds.DamageRatio == 0f) return;
            Play(damage);
        }

        void OnDamageRequested(float ratio, string reason) => Play(damage);
        void OnDodgeSucceeded() => Play(dodgeSuccess);
        void OnDash(Vector3 pos, Vector3 dir) => Play(dash);

        IMovingObstacle lastWarn;
        void OnWarning(IMovingObstacle t)
        {
            if (t != null && lastWarn == null) Play(warning);   // only when a warning starts, never every frame
            lastWarn = t;
        }

        void OnCourtInput(bool correct) => Play(correct ? courtshipCorrect : courtshipWrong);
        void OnRivalPressed(int count, int target) => Play(rivalPress);
        void OnRivalEnded(bool won) { if (won) Play(rivalWin); }
        void OnMenuMoved() => Play(menuMove);
        void OnMenuSelected() => Play(menuSelect);
        void OnLevel(int level, int previous) { if (level > previous) Play(levelUp); }
        void OnFever(bool on) { if (on) Play(fever); }
        void OnBubble(string key, int priority) => Play(bubble);
        void OnDied(string reason) => Play(gameOver);
    }
}
```

#### `Scripts/Features/Audio/RumbleDirector.cs` (신규)

```csharp
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine;

namespace FlyJam
{
    /// Short gamepad rumble: hit (small), level-up (a bit longer). Silently does nothing where the pad or OS has no rumble
    /// (for example some pads over Bluetooth on macOS). Stops on pause, scene change and quit. Lives on SYS_Audio.
    public class RumbleDirector : MonoBehaviour
    {
        [SerializeField] bool rumbleOn = true;
        [SerializeField] Vector3 hit = new Vector3(0.25f, 0.5f, 0.15f);       // low motor, high motor, seconds
        [SerializeField] Vector3 levelUp = new Vector3(0.4f, 0.8f, 0.35f);

        float until = -1f;

        void OnEnable()
        {
            GameEvents.PlayerHitBy += OnHitBy;
            GameEvents.DamageRequested += OnDamageRequested;
            GameEvents.LevelChanged += OnLevel;
            GameEvents.GamePaused += OnPaused;
        }

        void OnDisable()
        {
            GameEvents.PlayerHitBy -= OnHitBy;
            GameEvents.DamageRequested -= OnDamageRequested;
            GameEvents.LevelChanged -= OnLevel;
            GameEvents.GamePaused -= OnPaused;
            Stop();
        }

        void OnApplicationQuit() => Stop();

        void OnHitBy(IObstacle src)
        {
            if (src is IDamageSource ds && ds.DamageRatio == 0f) return;
            Rumble(hit);
        }

        void OnDamageRequested(float ratio, string reason) => Rumble(hit);
        void OnLevel(int level, int previous) { if (level > previous) Rumble(levelUp); }
        void OnPaused(bool on) { if (on) Stop(); }

        void Rumble(Vector3 r)
        {
            if (!rumbleOn) return;
#if ENABLE_INPUT_SYSTEM
            var pad = Gamepad.current;
            if (pad == null) return;
            pad.SetMotorSpeeds(Mathf.Clamp01(r.x), Mathf.Clamp01(r.y));
            until = Time.unscaledTime + Mathf.Max(0.02f, r.z);
#endif
        }

        void Update()
        {
            if (until > 0f && Time.unscaledTime >= until) Stop();
        }

        void Stop()
        {
            until = -1f;
#if ENABLE_INPUT_SYSTEM
            var pad = Gamepad.current;
            if (pad != null) pad.ResetHaptics();
#endif
        }
    }
}
```

### 4-7. Features/Health

#### `Scripts/Features/Health/HealthBalance.cs` (교체)

```csharp
using UnityEngine;

namespace FlyJam
{
    /// Planner numbers for HP (S02-S04). Sheet tab "Health".
    /// All damage and healing are fractions of the current max HP. Max HP itself comes from the "Level" table when
    /// the Level feature is on (it grows with each level); maxHp here is the fallback without it.
    public class HealthBalance : BalanceTable
    {
        public override string SheetName => "Health";

        [Tooltip("Max HP when the Level feature is off (with it: the Level table's maxHp)")]
        [SerializeField] float maxHp = 10f;
        [Tooltip("HP at the start of a round")]
        [SerializeField] float startHp = 5f;
        [Tooltip("HP lost per second while exploring (not during events or pause). 0 = off")]
        [SerializeField] float drainPerSecond = 0.1f;
        [Tooltip("Damage (fraction of max HP) for obstacles without their own value")]
        [SerializeField] float obstacleDamageRatio = 0.2f;
        [Tooltip("Speed at 0 HP as a fraction of full-HP speed (1 = HP never changes speed). 0.85 = up to 15% slower")]
        [SerializeField] float speedAtZeroHp = 0.85f;
        [Tooltip("Seconds of invulnerability (blinking) after a hit")]
        [SerializeField] float invulnerableTime = 1.5f;
        [Tooltip("Blink speed while invulnerable: seconds per on/off step")]
        [SerializeField] float blinkInterval = 0.08f;
        [Tooltip("At or below this HP ratio the bar turns yellow (0-1)")]
        [SerializeField] float midHpRatio = 0.5f;
        [Tooltip("At or below this HP ratio the bar turns orange and blinks, plus the 'hungry' message (0-1)")]
        [SerializeField] float lowHpRatio = 0.2f;

        public float MaxHp => Mathf.Max(1f, maxHp);
        public float StartHp => Mathf.Clamp(startHp, 0.1f, MaxHp);
        public float DrainPerSecond => Mathf.Max(0f, drainPerSecond);
        public float ObstacleDamageRatio => Mathf.Max(0f, obstacleDamageRatio);
        public float SpeedAtZeroHp => Mathf.Clamp01(speedAtZeroHp);
        public float InvulnerableTime => Mathf.Max(0f, invulnerableTime);
        public float BlinkInterval => Mathf.Max(0.02f, blinkInterval);
        public float MidHpRatio => Mathf.Clamp01(midHpRatio);
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
    /// Player HP (S02-S04). Starts at startHp, drains over time, drops on contact, refills from items.
    /// Max HP grows with the level (GameEvents.MaxHpRequested from the Level feature): the new part comes filled.
    /// Speed: SpeedMultiplier = speedAtZeroHp + (1 - speedAtZeroHp) x HP ratio (S03, small effect; the level sets the top speed).
    /// HP 0 -> GameEvents.PlayerDied("Hit" or "Starved") once. Frozen while the session is paused or ended.
    public class PlayerHealth : MonoBehaviour
    {
        public const string ReasonHit = "Hit";
        public const string ReasonStarved = "Starved";

        [SerializeField] HealthBalance balance;
        [SerializeField] SphereWalker player;   // Auto-filled from GameContext if null
        [SerializeField] GameSession session;   // Auto-filled from GameContext if null

        public float Current { get; private set; }
        public float Max => maxOverride > 0f ? maxOverride : balance != null ? balance.MaxHp : 10f;
        public float Ratio => Max > 0f ? Current / Max : 0f;
        public bool IsDead { get; private set; }

        readonly List<Renderer> blinkRenderers = new List<Renderer>();
        float invulnerableLeft, maxOverride;
        bool hidden, started;

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
            GameEvents.MaxHpRequested += OnMaxHp;
        }

        void OnDisable()
        {
            GameEvents.PlayerHitBy -= OnHitBy;
            GameEvents.HealRequested -= OnHeal;
            GameEvents.DamageRequested -= OnDamageRequested;
            GameEvents.MaxHpRequested -= OnMaxHp;
            EndInvulnerable();
        }

        void Start()
        {
            if (PlayerProgress.Active && PlayerProgress.Info.maxHp > 0f) maxOverride = PlayerProgress.Info.maxHp;
            Current = Mathf.Min(Max, balance != null ? balance.StartHp : 5f);
            started = true;
            GameEvents.RaiseHealthChanged(Current, Max);
            ApplySpeed();
        }

        /// grow = level-up: the added max HP comes filled. false = round start (just set the max).
        void OnMaxHp(float newMax, bool grow)
        {
            if (newMax <= 0f) return;
            float delta = newMax - Max;
            maxOverride = newMax;
            if (!started) return;   // Start() sets Current afterwards
            if (grow && delta > 0f && !IsDead) Current += delta;
            Current = Mathf.Clamp(Current, 0f, Max);
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
            float atZero = balance != null ? balance.SpeedAtZeroHp : 0.85f;
            player.SpeedMultiplier = atZero + (1f - atZero) * Mathf.Clamp01(Ratio);
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
    /// HUD top-left (A03): HP gauge, "7/12" numbers, "+2"/"-2" change popup, hungry message.
    /// - The gauge gets longer with max HP (pixelsPerHp per point): growth you can see at each level-up.
    /// - Damage: the green part drops at once, the lost part stays red (chip) for chipDelay, then shrinks smoothly.
    /// - Healing: the green part fills up over healSeconds.
    /// - Colour: green above midHpRatio, yellow at or below it, orange blinking at or below lowHpRatio (+ message).
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] HealthBalance balance;
        [Tooltip("The HealthBar root: its width = labelWidth + max HP x pixelsPerHp")]
        [SerializeField] RectTransform barRoot;
        [SerializeField] RectTransform fill;            // green part, stretched by anchorMax.x
        [SerializeField] Image fillImage;
        [SerializeField] RectTransform chip;            // red part behind fill, stretched by anchorMax.x
        [SerializeField] Image chipImage;
        [SerializeField] TMP_Text valueText;            // "5/10"
        [SerializeField] TMP_Text deltaText;            // "+2" / "-2"
        [SerializeField] GameObject lowHpMessage;       // LocalizedText "hud.lowhp" ("おなかがペコペコ…")

        [Header("Size")]
        [SerializeField] float pixelsPerHp = 30f;
        [SerializeField] float labelWidth = 104f;       // space for the "体力" label left of the frame
        [SerializeField] float growSeconds = 0.35f;

        [Header("Motion")]
        [SerializeField] float chipDelay = 0.4f;
        [SerializeField] float chipSharpness = 5f;
        [SerializeField] float healSeconds = 0.3f;
        [SerializeField] float deltaShowSeconds = 1.2f;
        [SerializeField] float deltaMin = 0.05f;        // smaller changes (per-frame drain) are not shown and do not leave a chip

        [Header("Colours")]
        [SerializeField] Color fullColor = new Color(0.40f, 0.78f, 0.36f, 1f);
        [SerializeField] Color midColor = new Color(1.00f, 0.84f, 0.25f, 1f);
        [SerializeField] Color lowColor = new Color(1.00f, 0.55f, 0.15f, 1f);
        [SerializeField] Color lowDimColor = new Color(0.62f, 0.30f, 0.08f, 1f);
        [SerializeField] Color chipColor = new Color(0.90f, 0.20f, 0.18f, 1f);
        [SerializeField] Color healColor = new Color(0.25f, 0.6f, 0.3f, 1f);
        [SerializeField] Color damageColor = new Color(0.8f, 0.2f, 0.2f, 1f);
        [SerializeField] float lowBlinkPeriod = 0.6f;

        float target = 1f, shown = 1f, chipRatio = 1f, chipHold, healRate;
        float maxHp = 10f, width = -1f, widthFrom, widthTo, growT = 1f;
        float last = -1f, deltaLeft;

        public float ShownRatio => shown;
        public float ChipRatio => chipRatio;

        void OnEnable() => GameEvents.HealthChanged += OnChanged;
        void OnDisable() => GameEvents.HealthChanged -= OnChanged;

        void Start()
        {
            if (deltaText != null) deltaText.gameObject.SetActive(false);
            if (chipImage != null) chipImage.color = chipColor;
        }

        void OnChanged(float current, float max)
        {
            float newTarget = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            bool first = last < 0f;
            float d = first ? 0f : current - last;

            if (first) { shown = chipRatio = newTarget; }
            else if (Mathf.Abs(max - maxHp) > 0.01f)
            {
                // Max HP changed (level-up): re-express the gauge in the new scale, no chip
                shown = chipRatio = newTarget;
            }
            else if (d <= -deltaMin)
            {
                chipRatio = Mathf.Max(chipRatio, shown);   // lost part stays red
                shown = newTarget;
                chipHold = chipDelay;
            }
            else if (d >= deltaMin)
            {
                healRate = Mathf.Max(0.01f, (newTarget - shown) / Mathf.Max(0.01f, healSeconds));
            }
            else if (newTarget < shown) shown = newTarget;   // slow drain: follow directly
            target = newTarget;

            if (Mathf.Abs(max - maxHp) > 0.01f || width < 0f) SetMax(max, first);
            if (valueText != null) valueText.text = Mathf.CeilToInt(current - 0.0001f) + "/" + Mathf.RoundToInt(max);

            if (!first && Mathf.Abs(d) >= deltaMin && deltaText != null)
            {
                string n = d.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
                deltaText.text = d > 0f ? "+" + n : n;
                deltaText.color = d > 0f ? healColor : damageColor;
                deltaText.gameObject.SetActive(true);
                deltaLeft = deltaShowSeconds;
            }
            last = current;
        }

        void SetMax(float max, bool instant)
        {
            maxHp = max;
            float w = labelWidth + max * pixelsPerHp;
            if (barRoot == null) return;
            if (instant || width < 0f)
            {
                width = w;
                growT = 1f;
                barRoot.sizeDelta = new Vector2(w, barRoot.sizeDelta.y);
                return;
            }
            widthFrom = barRoot.sizeDelta.x;
            widthTo = w;
            growT = 0f;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;

            if (shown < target) shown = Mathf.MoveTowards(shown, target, healRate * dt);
            else if (shown > target) shown = target;

            if (chipHold > 0f) chipHold -= dt;
            else chipRatio = Mathf.Lerp(chipRatio, shown, 1f - Mathf.Exp(-chipSharpness * dt));
            if (chipRatio < shown || chipRatio - shown < 0.002f) chipRatio = shown;

            if (fill != null) fill.anchorMax = new Vector2(shown, fill.anchorMax.y);
            if (chip != null) chip.anchorMax = new Vector2(chipRatio, chip.anchorMax.y);

            if (growT < 1f && barRoot != null)
            {
                growT = Mathf.Min(1f, growT + dt / Mathf.Max(0.01f, growSeconds));
                float w = Mathf.Lerp(widthFrom, widthTo, Mathf.SmoothStep(0f, 1f, growT));
                barRoot.sizeDelta = new Vector2(w, barRoot.sizeDelta.y);
                width = w;
            }

            float low = balance != null ? balance.LowHpRatio : 0.2f;
            float mid = balance != null ? balance.MidHpRatio : 0.5f;
            bool isLow = target <= low;
            if (lowHpMessage != null && lowHpMessage.activeSelf != isLow) lowHpMessage.SetActive(isLow);

            if (fillImage != null)
            {
                if (isLow) fillImage.color = Mathf.Repeat(Time.unscaledTime, lowBlinkPeriod) < lowBlinkPeriod * 0.5f ? lowColor : lowDimColor;
                else fillImage.color = target <= mid ? midColor : fullColor;
            }

            if (deltaLeft > 0f)
            {
                deltaLeft -= dt;
                if (deltaLeft <= 0f && deltaText != null) deltaText.gameObject.SetActive(false);
            }
        }
    }
}
```

### 4-8. Features/Items

#### `Scripts/Features/Items/ItemBalance.cs` (교체)

```csharp
using UnityEngine;

namespace FlyJam
{
    /// Planner numbers for food items (C02). Sheet tabs "Items" (key/value) and "ItemTypes" (one row per item).
    /// Each fixed point holds one item. When it comes back, it is a rare item (banana) with the level's
    /// rareItemChance ("Level" table), otherwise a normal one (apple).
    public class ItemBalance : BalanceTable
    {
        public override string SheetName => "Items";

        [System.Serializable]
        public struct TypeRow
        {
            public string id;          // must match an id in AppleSpawner.prefabs (apple, banana)
            public bool rare;          // 1 = rare item: appears with the level's rareItemChance
            public float healRatio;    // HP restored, fraction of max HP (0.2 of 10 = +2). Capped at max HP
            public int exp;            // EXP gained
        }

        [Tooltip("An eaten item comes back at the same point once the player is this far (straight line) from that point")]
        [SerializeField] float respawnDistance = 20f;
        [Tooltip("Pick-up distance between the player and the item")]
        [SerializeField] float pickupRadius = 1.0f;
        [Tooltip("Item height above the ground")]
        [SerializeField] float hoverHeight = 0.75f;
        [Tooltip("Up/down float height")]
        [SerializeField] float bobHeight = 0.35f;
        [Tooltip("Up/down float speed")]
        [SerializeField] float bobSpeed = 4.5f;
        [Tooltip("Spin, degrees per second")]
        [SerializeField] float spinSpeed = 90f;
        [Tooltip("Rare item chance when the Level feature is off (with it: the Level table's rareItemChance)")]
        [SerializeField] float rareChance = 0.1f;

        [SheetRows("ItemTypes")]
        [SerializeField] TypeRow[] types =
        {
            new TypeRow { id = "apple",  rare = false, healRatio = 0.2f, exp = 1 },
            new TypeRow { id = "banana", rare = true,  healRatio = 0.4f, exp = 3 },
        };

        public float RespawnDistance => Mathf.Max(0f, respawnDistance);
        public float PickupRadius => Mathf.Max(0.1f, pickupRadius);
        public float HoverHeight => hoverHeight;
        public float BobHeight => Mathf.Max(0f, bobHeight);
        public float BobSpeed => bobSpeed;
        public float SpinSpeed => spinSpeed;
        public float RareChance => Mathf.Clamp01(rareChance);
        public TypeRow[] Types => types;
    }
}
```

#### `Scripts/Features/Items/AppleSpawner.cs` (교체)

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace FlyJam
{
    /// Food at fixed points (C02, A02): apples, and bananas (rare, heal more, more EXP).
    /// Each child transform in 'points' is one spot (only its direction from the planet centre matters; it is snapped to the surface).
    /// Put a KeepClearZone on every point so the village generator leaves room for the item.
    /// Touch -> HealRequested + ExpRequested + ItemEaten, the item disappears. It comes back at the same spot once the player
    /// is respawnDistance away; what comes back is rolled again (rare chance grows with the level).
    /// (Class name kept from CHI-05 so the SYS_Items prefab link survives.)
    public class AppleSpawner : MonoBehaviour
    {
        [System.Serializable]
        public struct TypePrefab
        {
            public string id;
            public GameObject prefab;
        }

        [SerializeField] ItemBalance balance;
        [Tooltip("One prefab per ItemTypes id (apple -> Apple.prefab, banana -> Banana.prefab)")]
        [SerializeField] TypePrefab[] prefabs;
        [Tooltip("Old single-apple field. Used for id 'apple' only when 'prefabs' has no apple")]
        [SerializeField] GameObject applePrefab;
        [SerializeField] Transform[] points;
        [SerializeField] Planet planet;             // Auto-filled from GameContext if null
        [SerializeField] SphereWalker player;       // Auto-filled from GameContext if null
        [SerializeField] GameSession session;       // Auto-filled from GameContext if null

        class Spot
        {
            public Vector3 ground;
            public GameObject item;
            public ItemBalance.TypeRow row;
            public bool waiting;
        }

        readonly List<Spot> spots = new List<Spot>();

        public int ActiveCount
        {
            get
            {
                int n = 0;
                foreach (var s in spots) if (s.item != null && !s.waiting) n++;
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
            if (planet == null || points == null || balance == null) return;

            foreach (var p in points)
            {
                if (p == null) continue;
                Vector3 dir = p.position - planet.transform.position;
                if (dir.sqrMagnitude < 1e-6f) continue;
                var s = new Spot { ground = planet.SurfacePoint(dir.normalized) };
                spots.Add(s);
                Fill(s);
            }
        }

        void Update()
        {
            if (player == null || session == null || session.Ended) return;
            if (session.IsPaused || player.State == FlyState.Disabled) return;

            float pick = balance.PickupRadius;
            float back = balance.RespawnDistance;
            Vector3 pp = player.transform.position;

            foreach (var s in spots)
            {
                if (!s.waiting)
                {
                    if (s.item != null && Vector3.Distance(pp, s.item.transform.position) <= pick) Take(s);
                }
                else if (Vector3.Distance(pp, s.ground) >= back) Fill(s);
            }
        }

        /// Rolls the item type for this spot and puts it there.
        void Fill(Spot s)
        {
            if (!Pick(out var row, out var prefab)) return;
            Vector3 up = planet.UpAt(s.ground);
            if (s.item != null) Destroy(s.item);
            s.item = Instantiate(prefab, s.ground + up * balance.HoverHeight, Quaternion.FromToRotation(Vector3.up, up), transform);
            var look = s.item.GetComponent<AppleVisual>();
            if (look != null) look.Init(balance);
            s.row = row;
            s.waiting = false;
        }

        bool Pick(out ItemBalance.TypeRow row, out GameObject prefab)
        {
            float chance = PlayerProgress.Active ? PlayerProgress.Info.rareItemChance : balance.RareChance;
            bool wantRare = Random.value < chance;
            if (PickFrom(wantRare, out row, out prefab)) return true;
            return PickFrom(!wantRare, out row, out prefab);   // no prefab for that group: use the other
        }

        bool PickFrom(bool rare, out ItemBalance.TypeRow row, out GameObject prefab)
        {
            row = default;
            prefab = null;
            int seen = 0;
            foreach (var t in balance.Types)
            {
                if (t.rare != rare) continue;
                var p = FindPrefab(t.id);
                if (p == null) continue;
                seen++;
                if (Random.Range(0, seen) == 0) { row = t; prefab = p; }   // uniform pick among matching rows
            }
            return prefab != null;
        }

        GameObject FindPrefab(string id)
        {
            if (prefabs != null)
                foreach (var p in prefabs)
                    if (p.prefab != null && string.Equals(p.id, id, System.StringComparison.OrdinalIgnoreCase)) return p.prefab;
            if (applePrefab != null && string.Equals(id, "apple", System.StringComparison.OrdinalIgnoreCase)) return applePrefab;
            return null;
        }

        void Take(Spot s)
        {
            s.waiting = true;
            Vector3 pos = s.item.transform.position;
            s.item.SetActive(false);
            if (s.row.healRatio > 0f) GameEvents.RaiseHealRequested(s.row.healRatio);
            if (s.row.exp > 0) GameEvents.RaiseExpRequested(s.row.exp);
            GameEvents.RaiseItemEaten(s.row.id, pos);
        }
    }
}
```

### 4-9. Features/Hazards

#### `Scripts/Features/Hazards/HazardBalance.cs` (교체)

```csharp
using UnityEngine;

namespace FlyJam
{
    /// Planner numbers for enemies (C04-C08, E01, E02). Sheet tabs: "Hazards" (key/value) and "HazardTypes" (one row per enemy type).
    /// Speeds are absolute (units per second), not tied to the player: levelling up makes you faster than them.
    public class HazardBalance : BalanceTable
    {
        public override string SheetName => "Hazards";

        [System.Serializable]
        public struct TypeRow
        {
            public string id;               // must match an id in ObstacleSpawner.prefabs (dragonfly, spider, male, acorn)
            public int count;               // how many of this type circle the planet
            public float firstDelay;        // seconds after the round starts before this type appears
            public float speed;             // units per second (player Lv1 = 3.6)
            public float damageRatio;       // HP lost on contact, fraction of max HP (ignored when rival = 1)
            public bool warn;               // 1 = approaching triggers the warning (red frame, dash allowed)
            public bool rival;              // 1 = contact starts the mash battle instead of damage (male)
            public bool chase;              // 1 = turns toward the player when close
            public float detectRadius;      // chase starts within this straight-line distance
            public float loseRadius;        // chase stops beyond this straight-line distance
            public float turnRate;          // degrees per second while chasing
            public float stopOnHit;         // seconds it stays put after touching the player (harmless meanwhile). 0 = passes through
            public float stopOnDodge;       // seconds it stays put after the player dashes away from it (harmless). 0 = keeps going
            public float chaseCooldown;     // after stopping (hit or dodge): seconds it only goes straight before chasing again
            public float daySpeedUp;        // speed +this per day passed (0.1 = +10% on day 2, +20% on day 3)
            public float nightSpeedUp;      // extra speed at night (0.25 = x1.25)
            public float dayCooldownCut;    // chaseCooldown -this per day passed (0.2 = -20% per day, never below 20%)
            public float nightCooldownCut;  // chaseCooldown cut at night (0.5 = half)
        }

        [Tooltip("Enemies appear one by one, this many seconds apart, until every type reaches its count (each type waits for its firstDelay)")]
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
        [Tooltip("Seconds after a dash to wait for the enemy to pass before giving up on 'dodge success'")]
        [SerializeField] float successTimeout = 3f;

        [SheetRows("HazardTypes")]
        [SerializeField] TypeRow[] types =
        {
            new TypeRow { id = "dragonfly", count = 2, firstDelay = 2f, speed = 3.4f, damageRatio = 0.2f, warn = true, stopOnHit = 2f },
            new TypeRow { id = "spider",    count = 2, firstDelay = 2f, speed = 3.4f, damageRatio = 0.2f, warn = true, stopOnHit = 2f },
            new TypeRow { id = "male",      count = 1, firstDelay = 20f, speed = 3.8f, damageRatio = 0f, warn = true, rival = true,
                          chase = true, detectRadius = 6f, loseRadius = 9f, turnRate = 120f, stopOnHit = 2f,
                          stopOnDodge = 1f, chaseCooldown = 3f,
                          daySpeedUp = 0.1f, nightSpeedUp = 0.25f, dayCooldownCut = 0.2f, nightCooldownCut = 0.5f },
            new TypeRow { id = "acorn",     count = 1, firstDelay = 2f, speed = 3.2f, damageRatio = 0.1f, warn = true },
        };

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
    /// Per type row (HazardBalance.TypeRow from ObstacleSpawner.Launch): damage, warning, chase, stop-on-hit/dodge, rival,
    /// and day/night strength (male: faster and shorter chase cooldown as days pass and at night).
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
        float speed, travelled, maxTravel, stopLeft, bobPhase, cooldownLeft;
        bool chasing, dangerous;
        GameSession session;

        public Vector3 Position => transform.position;
        public float HitRadius => radius;
        public bool IsMoving => stopLeft <= 0f;
        public Vector3 Direction => dir;
        public bool IsChasing => chasing;
        public string TypeId => hasRow ? row.id : "";
        /// Approaching this one triggers the warning (E01).
        public bool Warns => !hasRow || row.warn;
        public bool IsRival => hasRow && row.rival;
        /// Day/night strength multiplier on speed (1 = base). Male: (1 + daySpeedUp x day) x (1 + nightSpeedUp at night).
        public float Strength { get; private set; } = 1f;
        public bool IsChaseCoolingDown => cooldownLeft > 0f;
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
            Stop(row.stopOnHit);
        }

        /// The player dashed while warned about this enemy (NearMissDirector). With stopOnDodge > 0 it freezes
        /// for that long (harmless), then goes straight for chaseCooldown before it may chase again.
        /// Returns true when it stopped (counts as a dodge success right away).
        public bool OnDodged()
        {
            if (!hasRow || row.stopOnDodge <= 0f) return false;
            Stop(row.stopOnDodge);
            return true;
        }

        void Stop(float seconds)
        {
            stopLeft = seconds;
            chasing = false;
            cooldownLeft = CooldownSeconds();
            SetDangerous(false);   // harmless while stopped: no hit, no warning
        }

        float CooldownSeconds()
        {
            if (!hasRow || row.chaseCooldown <= 0f) return 0f;
            int day = session != null ? session.DayIndex : 0;
            bool night = session != null && session.IsNight;
            float cut = Mathf.Max(0.2f, 1f - row.dayCooldownCut * day);
            if (night) cut *= Mathf.Clamp01(1f - row.nightCooldownCut);
            return row.chaseCooldown * cut;
        }

        void UpdateStrength()
        {
            if (!hasRow || session == null) { Strength = 1f; return; }
            float s = 1f + row.daySpeedUp * session.DayIndex;
            if (session.IsNight) s *= 1f + row.nightSpeedUp;
            Strength = Mathf.Max(0.1f, s);
        }

        void Update()
        {
            if (planet == null) return;
            if (session == null && GameContext.Current != null) session = GameContext.Current.Session;
            if (session != null && session.IsPaused) return;   // E04
            Bob();
            UpdateStrength();

            if (stopLeft > 0f)
            {
                stopLeft -= Time.deltaTime;
                if (stopLeft <= 0f) SetDangerous(true);
                return;
            }
            if (cooldownLeft > 0f) cooldownLeft -= Time.deltaTime;

            Vector3 up = planet.UpAt(transform.position);
            if (row.chase && target != null) Steer(up);

            float step = speed * Strength * Time.deltaTime;
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
            if (cooldownLeft > 0f) { chasing = false; return; }   // goes straight after being stopped
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

        [Tooltip("Speech bubble text key shown when a rival (male) appears for the first time. Empty = none")]
        [SerializeField] string rivalBubbleKey = "bubble.male";

        float timer;
        bool rivalAnnounced;

        void Start()
        {
            var ctx = GameContext.Current;
            if (ctx != null)
            {
                if (planet == null) planet = ctx.Planet;
                if (player == null) player = ctx.Player;
                if (session == null) session = ctx.Session;
            }
            timer = 0f;

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

            if (PickShortType(out var row, out var prefab))
            {
                Spawn(row, prefab);
                timer = balance.SpawnInterval;
            }
        }

        /// Among types whose firstDelay has passed: the one with the biggest shortage (count - alive). False when none.
        bool PickShortType(out HazardBalance.TypeRow row, out RollingObstacle prefab)
        {
            row = default;
            prefab = null;
            int best = 0;
            foreach (var t in balance.Types)
            {
                var p = FindPrefab(t.id);
                if (p == null || t.count <= 0 || session.Elapsed < t.firstDelay) continue;
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
            o.Launch(planet, spawnPos, heading, Mathf.Max(0.1f, row.speed), 0f, row, player.transform);
            GameEvents.RaiseEnemyAppeared(row.id);
            if (row.rival && !rivalAnnounced && !string.IsNullOrEmpty(rivalBubbleKey))
            {
                rivalAnnounced = true;
                GameEvents.RaiseBubbleRequested(rivalBubbleKey, 2);
            }
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
    /// Success = after a dash, that enemy leaves the danger range (or freezes: stopOnDodge) without touching the player -> "回避!" once.
    /// No warning during the level-up fever (the player is invulnerable then).
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
                        && player.InputEnabled && player.State != FlyState.Disabled && !PlayerProgress.Fever;

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
            if (dodged.OnDodged()) Succeed();   // stopOnDodge (male): it freezes, so the dodge counts right away
        }

        void OnHitBy(IObstacle src)
        {
            if (dodged != null && ReferenceEquals(src, dodged)) dodged = null;   // touched it: no success
        }

        void UpdateSuccess()
        {
            if (dodged == null) return;
            dodgedTimer -= Time.deltaTime;
            // Passed (not heading at the player any more) AND clear of its body: a forward dash can go through an enemy,
            // so "behind but still overlapping" must not count yet (E02: never success + hit for the same dash).
            float clear = dodged.HitRadius + 0.6f;
            bool apart = Vector3.Distance(player.transform.position, dodged.Position) > clear;
            if (!InDanger(dodged) && apart) Succeed();
            else if (dodgedTimer <= 0f) dodged = null;
        }

        void Succeed()
        {
            dodged = null;
            PerfectCount++;
            perfectUntil = Time.unscaledTime + successShowSeconds;
            GameEvents.RaiseDodgeSucceeded();
            GameEvents.RaisePerfectDodge();   // existing FX burst
        }

        void OnRoundEnded(bool cleared) => SessionResult.SetStat(PerfectKey, PerfectCount);
    }
}
```

#### `Scripts/Features/Hazards/NightGlowEyes.cs` (신규)

```csharp
using UnityEngine;

namespace FlyJam
{
    /// Male fly at night: eyes glow red and pulse, its halo turns red. Shows "the males got stronger" without words.
    /// Strength follows the night (ToonLighting.NightFactor). Put on Enemy_Male; eyes = EyeL/EyeR renderers, halo = Halo renderer.
    public class NightGlowEyes : MonoBehaviour
    {
        [SerializeField] Renderer[] eyes;
        [SerializeField] Renderer halo;
        [SerializeField] Color glowColor = new Color(1f, 0.12f, 0.08f, 1f);
        [SerializeField] Color haloNightColor = new Color(1f, 0.25f, 0.2f, 1f);
        [SerializeField] float pulseSpeed = 5f;
        [Range(0f, 1f)] [SerializeField] float pulseDepth = 0.35f;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int EmissiveId = Shader.PropertyToID("_Emissive");
        static readonly int HaloColorId = Shader.PropertyToID("_Color");

        MaterialPropertyBlock mpb;
        Color[] baseColors;
        Color haloBase = Color.white;
        float phase;

        void Start()
        {
            mpb = new MaterialPropertyBlock();
            phase = Random.Range(0f, 6.28f);
            baseColors = new Color[eyes != null ? eyes.Length : 0];
            for (int i = 0; i < baseColors.Length; i++)
            {
                var m = eyes[i] != null ? eyes[i].sharedMaterial : null;   // after ToonifyOnSpawn (Awake)
                baseColors[i] = m != null && m.HasProperty(BaseColorId) ? m.GetColor(BaseColorId) : Color.black;
            }
            if (halo != null && halo.sharedMaterial != null && halo.sharedMaterial.HasProperty(HaloColorId))
                haloBase = halo.sharedMaterial.GetColor(HaloColorId);
        }

        void LateUpdate()
        {
            if (mpb == null) return;
            float n = ToonLighting.Current != null ? ToonLighting.Current.NightFactor : 0f;
            float pulse = 1f - pulseDepth * (0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed + phase));
            float k = n * pulse;

            for (int i = 0; i < baseColors.Length; i++)
            {
                var r = eyes[i];
                if (r == null) continue;
                r.GetPropertyBlock(mpb);
                mpb.SetColor(BaseColorId, Color.Lerp(baseColors[i], glowColor, k));
                mpb.SetFloat(EmissiveId, k > 0.05f ? 1f : 0f);
                r.SetPropertyBlock(mpb);
            }

            if (halo != null)
            {
                halo.GetPropertyBlock(mpb);   // NightHalo sets _Intensity on the same block
                mpb.SetColor(HaloColorId, Color.Lerp(haloBase, haloNightColor, n));
                halo.SetPropertyBlock(mpb);
            }
        }
    }
}
```

### 4-10. Features/Dodge

#### `Scripts/Features/Dodge/DodgeBalance.cs` (교체)

```csharp
using UnityEngine;

namespace FlyJam
{
    /// Planner numbers for the dash (P04). Sheet tab "Dodge".
    public class DodgeBalance : BalanceTable
    {
        public override string SheetName => "Dodge";

        [Tooltip("Dash distance (straight ahead, the way the fly is facing)")]
        [SerializeField] float distance = 3f;
        [Tooltip("Dash duration (seconds). Invulnerable during it, so it can pass through an enemy")]
        [SerializeField] float duration = 0.3f;
        [Tooltip("Seconds before the next dash")]
        [SerializeField] float cooldown = 0.4f;

        public float Distance => Mathf.Max(0.1f, distance);
        public float Duration => Mathf.Max(0.05f, duration);
        public float Cooldown => Mathf.Max(0f, cooldown);
    }
}
```

#### `Scripts/Features/Dodge/DodgeAbility.cs` (교체)

```csharp
using UnityEngine;

namespace FlyJam
{
    /// Dash (P04). Only while a warning is on (GameEvents.WarningChanged); the action button does nothing otherwise.
    /// Always straight ahead (the way the fly faces). Invulnerable during the dash, so it may pass through the enemy:
    /// NearMissDirector counts it as a dodge once the enemy is behind and clear (or freezes, stopOnDodge).
    /// Temporarily overrides SphereWalker movement (BeginOverride).
    [DefaultExecutionOrder(-10)]   // Reads input before SphereWalker.Update
    public class DodgeAbility : MonoBehaviour
    {
        [SerializeField] SphereWalker walker;            // Auto-filled from GameContext.Player if null
        [SerializeField] DodgeBalance balance;

        IMovingObstacle threat;
        float timer, cd;
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

            Begin();
        }

        bool HasThreat() => threat != null && !(threat is Object uo && uo == null);

        void Begin()
        {
            Transform t = walker.transform;
            Vector3 up = walker.Planet.UpAt(t.position);
            dir = Vector3.ProjectOnPlane(t.forward, up).normalized;

            timer = Duration;
            cd = balance != null ? balance.Cooldown : 0.4f;
            walker.BeginOverride(this);
            walker.SetInvulnerable(this, true);
            walker.SetState(FlyState.Dodging);
            if (walker.IsPlayer) GameEvents.RaisePlayerDodged(t.position, dir);
            Tick();
        }

        float Duration => balance != null ? balance.Duration : 0.3f;

        void Tick()
        {
            float dt = Time.deltaTime;
            float spd = (balance != null ? balance.Distance : 3f) / Duration;
            walker.MoveBy(dir * spd * dt);
            walker.AlignToSurface();
            Vector3 up = walker.Planet.UpAt(walker.transform.position);
            dir = Vector3.ProjectOnPlane(dir, up).normalized;
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
    }
}
```

### 4-11. Features/Mate

#### `Scripts/Features/Mate/MateBalance.cs` (교체)

```csharp
using UnityEngine;

namespace FlyJam
{
    /// Planner numbers for the female (C03). Sheet tab "Mate".
    /// Before shyUntilLevel she is shy: she keeps far away (shyFleeRadius) so you cannot reach her early. The heart marker still shows where she is.
    public class MateBalance : BalanceTable
    {
        public override string SheetName => "Mate";

        [Tooltip("Female speed (units per second). Player: Lv4 4.5, Lv5 4.8 at full HP")]
        [SerializeField] float speed = 4.4f;
        [Tooltip("Below this player level she is shy (keeps far away). At this level she starts acting normally")]
        [SerializeField] int shyUntilLevel = 3;
        [Tooltip("Shy: she runs away when the player is this close (straight line). 18 = about a quarter of the planet")]
        [SerializeField] float shyFleeRadius = 18f;
        [Tooltip("Shy: she stops running when the player is this far (straight line)")]
        [SerializeField] float shyReleaseRadius = 20f;
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

        public float Speed => Mathf.Max(0f, speed);
        public int ShyUntilLevel => shyUntilLevel;
        public float ShyFleeRadius => Mathf.Max(0f, shyFleeRadius);
        public float ShyReleaseRadius => Mathf.Max(ShyFleeRadius + 0.5f, shyReleaseRadius);
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
    /// goes back to roaming beyond releaseRadius. Speed = MateBalance.speed (absolute).
    /// Shy below MateBalance.shyUntilLevel (Level feature): bigger flee range, no pull toward the player.
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
        [Tooltip("Speech bubble when she stops being shy (the player reached shyUntilLevel). Empty = none")]
        [SerializeField] string notShyBubbleKey = "bubble.mate.near";

        enum Mode { Pause, Walk, Flee, Frozen }
        Mode mode = Mode.Pause;
        float timer, forcedFlee;
        Vector3 heading;
        SphereWalker playerWalker;

        public bool IsFleeing => mode == Mode.Flee;
        /// Shy = keeps far away. Only with the Level feature on (PlayerProgress.Active).
        public bool IsShy => balance != null && PlayerProgress.Active && PlayerProgress.Level < balance.ShyUntilLevel;
        bool wasShy;

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
            wasShy = IsShy;
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
            bool shy = IsShy;
            if (wasShy && !shy && !string.IsNullOrEmpty(notShyBubbleKey)) GameEvents.RaiseBubbleRequested(notShyBubbleKey, 3);
            wasShy = shy;
            float fleeR = balance == null ? 5f : shy ? balance.ShyFleeRadius : balance.FleeRadius;
            float releaseR = balance == null ? 8f : shy ? balance.ShyReleaseRadius : balance.ReleaseRadius;
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
                if (!IsShy && Vector3.Angle(up, playerUp) > (balance != null ? balance.LeashAngle : 100f))
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
            float speed = balance != null ? balance.Speed : 4.4f;
            self.SpeedMultiplier = speed / Mathf.Max(0.01f, self.MoveSpeed);
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
            if (PlayerProgress.Fever) return;   // flying during the level-up fever: courtship starts after landing

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
    /// Heart marker that always shows where the female is (A03, C03).
    /// On screen: above her head. Off screen (or behind the camera): pinned to the screen edge in her direction.
    /// Size grows as she gets closer (sizeFar at distanceFar or more -> sizeNear at distanceNear or less).
    /// Hidden during courtship / mash battle / pause (session paused) and after the round ends.
    /// The first time she is on screen and within seenDistance -> speech bubble "メスだ！".
    /// (Class name kept from the prototype so the SYS_Mate prefab link survives.)
    public class MateFinderHint : MonoBehaviour
    {
        [SerializeField] Camera worldCamera;        // Auto-filled from GameContext if null
        [SerializeField] SphereWalker player;       // Auto-filled from GameContext if null
        [SerializeField] Transform mate;            // Auto-filled from MateAI.Current if null
        [SerializeField] GameSession session;       // Auto-filled from GameContext if null
        [SerializeField] RawImage icon;
        [SerializeField] float headHeight = 0.9f;
        [Tooltip("Screen-edge margin for the off-screen marker (fraction of the screen)")]
        [SerializeField] float edgeMargin = 0.06f;
        [SerializeField] float sizeNear = 72f;
        [SerializeField] float sizeFar = 36f;
        [SerializeField] float distanceNear = 3f;
        [SerializeField] float distanceFar = 24f;
        [SerializeField] float bobPixels = 6f;
        [SerializeField] float fadeSpeed = 8f;
        [Tooltip("First sighting bubble: on screen and within this distance")]
        [SerializeField] float seenDistance = 16f;
        [SerializeField] string seenBubbleKey = "bubble.mate.seen";

        float alpha;
        bool seen;

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
            float dist = Vector3.Distance(player.transform.position, mate.position);

            if (!seen && onScreen && dist <= seenDistance && !session.IsPaused && !session.Ended)
            {
                seen = true;
                if (!string.IsNullOrEmpty(seenBubbleKey)) GameEvents.RaiseBubbleRequested(seenBubbleKey, 2);
            }

            bool want = !session.Ended && !session.IsPaused;
            alpha = Mathf.MoveTowards(alpha, want ? 1f : 0f, fadeSpeed * Time.unscaledDeltaTime);
            SetAlpha(alpha);
            if (alpha <= 0f) return;

            Vector2 pos;
            if (onScreen)
            {
                float bob = Mathf.Sin(Time.unscaledTime * 5f) * bobPixels;
                pos = new Vector2(vp.x * Screen.width, vp.y * Screen.height + bob);
            }
            else pos = EdgePoint(vp);

            float t = Mathf.InverseLerp(distanceFar, distanceNear, dist);   // 0 far .. 1 near
            float s = Mathf.Lerp(sizeFar, sizeNear, t);
            icon.rectTransform.position = new Vector3(pos.x, pos.y, 0f);
            icon.rectTransform.sizeDelta = new Vector2(s, s * 6f / 7f);
        }

        /// Off-screen: from the screen centre toward her, clamped to the screen rectangle minus the margin.
        Vector2 EdgePoint(Vector3 vp)
        {
            Vector2 d = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
            if (vp.z < 0f) d = -d;                       // behind the camera: the projection is mirrored
            if (d.sqrMagnitude < 1e-6f) d = Vector2.down;
            float half = 0.5f - edgeMargin;
            float k = half / Mathf.Max(Mathf.Abs(d.x), Mathf.Abs(d.y));
            Vector2 v = new Vector2(0.5f, 0.5f) + d * k;
            return new Vector2(v.x * Screen.width, v.y * Screen.height);
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

### 4-12. Features/Courtship

#### `Scripts/Features/Courtship/CourtshipBalance.cs` (교체)

```csharp
using UnityEngine;

namespace FlyJam
{
    /// Planner numbers for the courtship button order (S06, P03, E03). Sheet tab "Courtship".
    public class CourtshipBalance : BalanceTable
    {
        public override string SheetName => "Courtship";

        [Tooltip("How many buttons to press in order. With the Level feature on, the Level table's courtLength is used instead")]
        [SerializeField] int length = 4;
        [Tooltip("How many different buttons can appear (1-4). 4 = A/B/X/Y")]
        [SerializeField] int buttonKinds = 4;
        [Tooltip("Seconds to finish the whole order. 0 = no limit. With the Level feature on, the Level table's courtTime is used instead")]
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
    /// Difficulty follows the player's level ("Level" table: courtLength, courtTime) when the Level feature is on:
    /// low levels get a long, fast dance that is nearly impossible ("もっと強くならなきゃ…" bubble on failure).
    /// Sounds: the Audio feature listens to CourtshipInput (no sound code here).
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

        [Tooltip("Speech bubble after failing a dance that was harder than the easiest one. Empty = none")]
        [SerializeField] string hardFailBubbleKey = "bubble.court.hard";

        const string DoneMark = "♪";

        public bool Running { get; private set; }

        void Awake()
        {
            if (panel != null) panel.SetActive(false);
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
            if (PlayerProgress.Active && PlayerProgress.Info.courtLength > 0)
            {
                length = PlayerProgress.Info.courtLength;
                limit = Mathf.Max(0f, PlayerProgress.Info.courtTime);
            }
            length = Mathf.Clamp(length, 1, slots != null && slots.Length > 0 ? slots.Length : 8);
            bool hard = length > 4 || limit > 0f;
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
                    failKey = "court.fail.wrong";
                    break;
                }
                GameEvents.RaiseCourtshipInput(true);
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
            if (!success && hard && !string.IsNullOrEmpty(hardFailBubbleKey)) GameEvents.RaiseBubbleRequested(hardFailBubbleKey, 3);
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
    }
}
```

### 4-13. Features/Fx

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
        [Tooltip("Item pickup burst (apple, banana). Empty = uses perfectBurst")]
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
            GameEvents.ItemEaten += OnItem;
        }

        void OnDisable()
        {
            GameEvents.PlayerDodged -= OnDodged;
            GameEvents.PlayerHit -= OnHit;
            GameEvents.CourtshipFinished -= OnCourtship;
            GameEvents.MateAppeared -= OnMateAppeared;
            GameEvents.PerfectDodge -= OnPerfect;
            GameEvents.ItemEaten -= OnItem;
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

        void OnItem(string id, Vector3 pos) => PlayUp(appleBurst != null ? appleBurst : perfectBurst, pos, 0.4f);

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

#### `Scripts/Features/Fx/DashRingFx.cs` (신규)

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace FlyJam
{
    /// Dash effect: glowing rings slide over the fly from head to tail, 4 times, fast - like the air around a falling meteor.
    /// Each ring is a circle around the body axis (the body is inside the ring), orange turning red as it fades.
    /// Drawn with LineRenderers in the world, so the pixel-art pass pixelates them like everything else.
    public class DashRingFx : MonoBehaviour
    {
        [SerializeField] Material material;                 // M_ParticleAdd (additive glow)
        [SerializeField] SphereWalker player;               // Auto-filled from GameContext if null
        [SerializeField] int rings = 4;
        [SerializeField] float ringInterval = 0.06f;        // seconds between ring starts
        [SerializeField] float ringTravelSeconds = 0.14f;   // head -> tail time of one ring
        [SerializeField] float headZ = 0.55f;               // local z (fly space) where a ring starts
        [SerializeField] float tailZ = -0.55f;              // local z where it ends
        [SerializeField] float centerY = 0.36f;             // local height of the body axis
        [SerializeField] float radius = 0.45f;
        [SerializeField] float endRadiusScale = 1.15f;
        [SerializeField] float width = 0.06f;
        [SerializeField] int segments = 20;
        [SerializeField] Color startColor = new Color(1f, 0.62f, 0.25f, 0.6f);
        [SerializeField] Color endColor = new Color(1f, 0.18f, 0.1f, 0f);

        class Ring { public LineRenderer line; public float t; public bool live; }

        readonly List<Ring> pool = new List<Ring>();
        float spawnTimer;
        int toSpawn;

        void Start()
        {
            if (player == null && GameContext.Current != null) player = GameContext.Current.Player;
        }

        void OnEnable() => GameEvents.PlayerDodged += OnDash;
        void OnDisable() => GameEvents.PlayerDodged -= OnDash;

        void OnDash(Vector3 pos, Vector3 dir)
        {
            toSpawn = Mathf.Max(1, rings);
            spawnTimer = 0f;
        }

        void Update()
        {
            if (player == null) return;
            float dt = Time.deltaTime;

            if (toSpawn > 0)
            {
                spawnTimer -= dt;
                if (spawnTimer <= 0f)
                {
                    Get().t = 0f;
                    toSpawn--;
                    spawnTimer = ringInterval;
                }
            }

            Transform pt = player.transform;
            foreach (var r in pool)
            {
                if (!r.live) continue;
                r.t += dt / Mathf.Max(0.01f, ringTravelSeconds);
                if (r.t >= 1f) { r.live = false; r.line.enabled = false; continue; }
                float z = Mathf.Lerp(headZ, tailZ, r.t);
                float rad = radius * Mathf.Lerp(1f, endRadiusScale, r.t);
                for (int i = 0; i < segments; i++)
                {
                    float a = Mathf.PI * 2f * i / segments;
                    Vector3 local = new Vector3(Mathf.Cos(a) * rad, centerY + Mathf.Sin(a) * rad, z);
                    r.line.SetPosition(i, pt.TransformPoint(local));
                }
                Color c = Color.Lerp(startColor, endColor, r.t);
                r.line.startColor = c;
                r.line.endColor = c;
            }
        }

        Ring Get()
        {
            foreach (var r in pool)
                if (!r.live) { r.live = true; r.line.enabled = true; return r; }
            var go = new GameObject("DashRing");
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = true;
            line.positionCount = Mathf.Max(6, segments);
            line.widthMultiplier = width;
            line.sharedMaterial = material;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            segments = line.positionCount;
            var ring = new Ring { line = line, live = true };
            pool.Add(ring);
            return ring;
        }
    }
}
```

#### `Scripts/Features/Fx/WindGusts.cs` (신규)

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace FlyJam
{
    /// Now and then one or two wavy wind lines sweep across the view, and the pollen drifts with them for a moment.
    /// The lines live in the world just in front of the camera, so the pixel-art pass pixelates them.
    /// Wind direction: one main direction (screen space) that slowly swings back and forth.
    public class WindGusts : MonoBehaviour
    {
        [SerializeField] Material material;                     // M_Particle (soft alpha)
        [SerializeField] Camera worldCamera;                    // Auto-filled from GameContext if null
        [SerializeField] ParticleSystem pollen;                 // SYS_Fx/FX_Pollen (pushed by the gust). Optional
        [SerializeField] Vector2 interval = new Vector2(4f, 8f);
        [SerializeField] Vector2Int linesPerGust = new Vector2Int(1, 2);
        [SerializeField] float lifetime = 1.6f;
        [SerializeField] float lineLength = 4f;
        [SerializeField] float sweepWidth = 13f;               // distance the line head travels across the view
        [SerializeField] Vector2 depth = new Vector2(4f, 7f);  // distance in front of the camera
        [SerializeField] float verticalSpread = 1.6f;
        [SerializeField] float waveAmplitude = 0.18f;
        [SerializeField] float waves = 2.5f;
        [SerializeField] float width = 0.05f;
        [SerializeField] Color color = new Color(1f, 1f, 1f, 0.35f);
        [SerializeField] int points = 24;
        [SerializeField] float baseAngle = 0f;                 // degrees: 0 = left -> right on screen
        [SerializeField] float angleSwing = 25f;
        [SerializeField] float pollenPush = 2f;

        class Streak { public LineRenderer line; public float t, phase, dep, y, delay; public bool live; }

        readonly List<Streak> pool = new List<Streak>();
        float timer, gustLeft;
        Vector3 gustWorldDir;

        void Start()
        {
            if (worldCamera == null && GameContext.Current != null) worldCamera = GameContext.Current.WorldCamera;
            timer = Random.Range(interval.x, interval.y);
        }

        float Angle => baseAngle + Mathf.Sin(Time.time * 0.07f) * angleSwing;

        void Update()
        {
            if (worldCamera == null) return;
            float dt = Time.deltaTime;

            timer -= dt;
            if (timer <= 0f)
            {
                timer = Random.Range(interval.x, interval.y);
                int n = Random.Range(linesPerGust.x, linesPerGust.y + 1);
                for (int i = 0; i < n; i++)
                {
                    var s = Get();
                    s.t = 0f;
                    s.delay = i * 0.25f;
                    s.phase = Random.Range(0f, 6.28f);
                    s.dep = Random.Range(depth.x, depth.y);
                    s.y = Random.Range(-verticalSpread, verticalSpread);
                }
                gustLeft = lifetime;
                float a = Angle * Mathf.Deg2Rad;
                Transform cam = worldCamera.transform;
                gustWorldDir = (cam.right * Mathf.Cos(a) + cam.up * Mathf.Sin(a)).normalized;
            }

            PushPollen(dt);

            Transform c = worldCamera.transform;
            float ang = Angle * Mathf.Deg2Rad;
            Vector3 along = (c.right * Mathf.Cos(ang) + c.up * Mathf.Sin(ang)).normalized;
            Vector3 side = Vector3.Cross(c.forward, along).normalized;
            foreach (var s in pool)
            {
                if (!s.live) continue;
                if (s.delay > 0f) { s.delay -= dt; s.line.enabled = false; continue; }
                s.line.enabled = true;
                s.t += dt / Mathf.Max(0.05f, lifetime);
                if (s.t >= 1f) { s.live = false; s.line.enabled = false; continue; }

                Vector3 center = c.position + c.forward * s.dep + side * s.y;
                float head = Mathf.Lerp(-sweepWidth * 0.5f, sweepWidth * 0.5f + lineLength, s.t);
                for (int i = 0; i < points; i++)
                {
                    float k = (float)i / (points - 1);                       // 0 tail .. 1 head
                    float x = head - lineLength * (1f - k);
                    float wave = Mathf.Sin(k * waves * Mathf.PI * 2f + s.phase + s.t * 8f) * waveAmplitude * Mathf.Sin(k * Mathf.PI);
                    s.line.SetPosition(i, center + along * x + side * wave);
                }
                float fade = Mathf.Sin(s.t * Mathf.PI);                       // in and out
                var col = color;
                col.a *= fade;
                var tailCol = col;
                tailCol.a = 0f;
                s.line.startColor = tailCol;                                  // tail end transparent
                s.line.endColor = col;
            }
        }

        void PushPollen(float dt)
        {
            if (pollen == null) return;
            var f = pollen.forceOverLifetime;
            if (gustLeft > 0f)
            {
                gustLeft -= dt;
                float k = Mathf.Sin(Mathf.Clamp01(gustLeft / Mathf.Max(0.05f, lifetime)) * Mathf.PI) * pollenPush;
                f.enabled = true;
                f.space = ParticleSystemSimulationSpace.World;
                f.x = new ParticleSystem.MinMaxCurve(gustWorldDir.x * k);
                f.y = new ParticleSystem.MinMaxCurve(gustWorldDir.y * k);
                f.z = new ParticleSystem.MinMaxCurve(gustWorldDir.z * k);
            }
            else if (f.enabled) f.enabled = false;
        }

        Streak Get()
        {
            foreach (var s in pool)
                if (!s.live) { s.live = true; return s; }
            var go = new GameObject("WindStreak");
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = Mathf.Max(4, points);
            line.widthMultiplier = width;
            line.sharedMaterial = material;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
            points = line.positionCount;
            var st = new Streak { line = line, live = true };
            pool.Add(st);
            return st;
        }
    }
}
```

### 4-14. Features/Sky

#### `Scripts/Features/Sky/DayNightCycle.cs` (교체)

```csharp
using UnityEngine;
using UnityEngine.Rendering;

namespace FlyJam
{
    /// Computes sky appearance (gradient, sun, moon, stars) and time-of-day lighting, pushing values to ToonLighting.
    /// Without this component, the game still runs using ToonLighting's clear daytime defaults.
    public class DayNightCycle : MonoBehaviour
    {
        public static DayNightCycle Current { get; private set; }

        [SerializeField] GameSession session;            // Auto-filled from GameContext if null; loops with idleCycleSeconds if absent
        [SerializeField] float idleCycleSeconds = 20f;
        [SerializeField] Camera worldCamera;             // Auto-filled from GameContext if null
        [SerializeField] Planet planet;                  // Auto-filled from GameContext if null
        [SerializeField] Material skyMaterial;
        [SerializeField] bool useCodeDefaults = true;
        [SerializeField] Gradient topColor, horizonColor, lightColor, ambientColor;
        [SerializeField] AnimationCurve lightIntensity;
        [SerializeField, Range(0.5f, 0.95f)] float nightStart = 35f / 45f;

        [Header("Sun/Moon Arc (Camera-Facing Sky, Degrees)")]
        [SerializeField] float arcAzimuth = 28f;
        [SerializeField] float arcLowElevation = -36f;
        [SerializeField] float arcPeakElevation = -14f;

        /// Night start (0-1 of the day): from GameSession (sheet "Session".nightStart) when present, else this inspector value.
        float NightStart => session != null ? session.NightStart : nightStart;

        public float T { get; private set; }
        public float NightFactor { get; private set; }

        Material skyInstance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Current = null;

        void Awake()
        {
            Current = this;
            var ctx = GameContext.Current;
            if (ctx != null)
            {
                if (session == null) session = ctx.Session;
                if (worldCamera == null) worldCamera = ctx.WorldCamera;
                if (planet == null) planet = ctx.Planet;
            }
            if (useCodeDefaults || lightIntensity == null || lightIntensity.length == 0) BuildDefaults();
            skyInstance = new Material(skyMaterial);
            RenderSettings.skybox = skyInstance;
            RenderSettings.ambientMode = AmbientMode.Flat;
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        void LateUpdate()
        {
            if (worldCamera == null || planet == null) return;

            T = session != null ? session.DayProgress : Mathf.Repeat(Time.time / idleCycleSeconds, 1f);
            NightFactor = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(NightStart - 0.08f, NightStart + 0.04f, T));

            Vector3 up = planet.UpAt(worldCamera.transform.position);
            Vector3 fwd = Vector3.ProjectOnPlane(worldCamera.transform.forward, up).normalized;

            Vector3 sunDir = Arc(up, fwd, Mathf.InverseLerp(0f, NightStart, T));
            Vector3 moonDir = Arc(up, fwd, Mathf.InverseLerp(NightStart - 0.05f, 1f, T) * 0.7f);

            Color horizon = horizonColor.Evaluate(T);
            skyInstance.SetVector("_SkyUp", up);
            skyInstance.SetColor("_TopColor", topColor.Evaluate(T));
            skyInstance.SetColor("_HorizonColor", horizon);
            skyInstance.SetColor("_BottomColor", horizon * 0.85f);
            skyInstance.SetVector("_SunDir", sunDir);
            skyInstance.SetVector("_MoonDir", moonDir);
            skyInstance.SetColor("_SunColor", new Color(1f, 0.92f, 0.75f, 1f - NightFactor));
            skyInstance.SetColor("_MoonColor", new Color(0.85f, 0.9f, 1f, NightFactor));
            skyInstance.SetFloat("_StarIntensity", NightFactor);

            RenderSettings.ambientLight = ambientColor.Evaluate(T);

            if (ToonLighting.Current != null)
                ToonLighting.Current.SetTimeOfDay(lightColor.Evaluate(T), lightIntensity.Evaluate(T), horizon, NightFactor);
        }

        Vector3 Arc(Vector3 up, Vector3 fwd, float t)
        {
            float az = Mathf.Lerp(arcAzimuth, -arcAzimuth, t);
            float elev = Mathf.Lerp(arcLowElevation, arcPeakElevation, Mathf.Sin(t * Mathf.PI)) * Mathf.Deg2Rad;
            Vector3 flat = Quaternion.AngleAxis(az, up) * fwd;
            return (flat * Mathf.Cos(elev) + up * Mathf.Sin(elev)).normalized;
        }

        void BuildDefaults()
        {
            float[] t = { 0f, 5f / 45f, 25f / 45f, 30f / 45f, NightStart, 1f };
            topColor = G(t, new[] { C(0.30f, 0.32f, 0.58f), C(0.35f, 0.60f, 0.95f), C(0.35f, 0.60f, 0.95f), C(0.45f, 0.38f, 0.70f), C(0.07f, 0.08f, 0.22f), C(0.03f, 0.04f, 0.12f) });
            horizonColor = G(t, new[] { C(1.00f, 0.62f, 0.48f), C(0.75f, 0.88f, 1.00f), C(0.75f, 0.88f, 1.00f), C(1.00f, 0.55f, 0.30f), C(0.25f, 0.16f, 0.32f), C(0.07f, 0.08f, 0.20f) });
            lightColor = G(t, new[] { C(1.00f, 0.72f, 0.55f), C(1.00f, 0.97f, 0.90f), C(1.00f, 0.97f, 0.90f), C(1.00f, 0.62f, 0.38f), C(0.60f, 0.68f, 1.00f), C(0.60f, 0.68f, 1.00f) });
            ambientColor = G(t, new[] { C(0.45f, 0.40f, 0.45f), C(0.60f, 0.62f, 0.66f), C(0.60f, 0.62f, 0.66f), C(0.55f, 0.42f, 0.40f), C(0.26f, 0.28f, 0.42f), C(0.24f, 0.27f, 0.40f) });
            lightIntensity = new AnimationCurve(
                new Keyframe(t[0], 0.46f), new Keyframe(t[1], 0.85f), new Keyframe(t[2], 0.85f),
                new Keyframe(t[3], 0.62f), new Keyframe(t[4], 0.35f), new Keyframe(t[5], 0.35f));
        }

        static Color C(float r, float g, float b) => new Color(r, g, b, 1f);

        static Gradient G(float[] times, Color[] colors)
        {
            var ck = new GradientColorKey[times.Length];
            for (int i = 0; i < times.Length; i++) ck[i] = new GradientColorKey(colors[i], times[i]);
            var g = new Gradient();
            g.SetKeys(ck, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return g;
        }
    }
}
```

### 4-15. Features/Village

#### `Scripts/Features/Village/WaterZone.cs` (신규)

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace FlyJam
{
    /// Marks a pond: flies inside 'radius' (measured along the surface from this object) sink a little (PondSink).
    /// Put on each Pond object next to its SurfaceDisc (same radius).
    public class WaterZone : MonoBehaviour
    {
        [SerializeField] float radius = 2.5f;

        static readonly List<WaterZone> all = new List<WaterZone>();
        public static IReadOnlyList<WaterZone> All => all;
        public float Radius => radius;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => all.Clear();

        void OnEnable() { if (!all.Contains(this)) all.Add(this); }
        void OnDisable() => all.Remove(this);

        /// True when the point (on the surface) is inside this pond, shrunk by 'margin'.
        public bool Contains(Vector3 p, float margin) => Vector3.Distance(p, transform.position) <= Mathf.Max(0f, radius - margin);

        public static bool AnyContains(Vector3 p, float margin)
        {
            for (int i = 0; i < all.Count; i++)
                if (all[i] != null && all[i].Contains(p, margin)) return true;
            return false;
        }
    }
}
```

#### `Scripts/Features/Village/PondSink.cs` (신규)

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace FlyJam
{
    /// In a pond the fly sinks a little (about a third of its body) and leaves faint pixel ripples while it moves.
    /// Put on the Fly root (the Mate variant inherits it). Needs VisualOffset on the same object. Speed is unchanged.
    [RequireComponent(typeof(VisualOffset))]
    public class PondSink : MonoBehaviour
    {
        [SerializeField] float sinkDepth = 0.15f;
        [SerializeField] float sinkSeconds = 0.15f;
        [SerializeField] float edgeMargin = 0.1f;
        [Header("Ripples")]
        [SerializeField] Material rippleMaterial;          // M_Particle. Empty = no ripples
        [SerializeField] float rippleInterval = 0.5f;
        [SerializeField] float rippleSeconds = 0.9f;
        [SerializeField] float rippleStartRadius = 0.25f;
        [SerializeField] float rippleEndRadius = 0.9f;
        [SerializeField] float rippleWidth = 0.035f;
        [SerializeField] Color rippleColor = new Color(0.85f, 0.95f, 1f, 0.35f);
        [SerializeField] int rippleSegments = 18;

        class Ripple { public LineRenderer line; public Vector3 center, up; public float t; public bool live; }

        VisualOffset offset;
        SphereWalker walker;
        float sink, rippleTimer;
        readonly List<Ripple> pool = new List<Ripple>();

        public bool InWater { get; private set; }

        void Awake()
        {
            offset = GetComponent<VisualOffset>();
            walker = GetComponent<SphereWalker>();
        }

        void Update()
        {
            InWater = WaterZone.AnyContains(transform.position, edgeMargin);
            float target = InWater ? -sinkDepth : 0f;
            sink = Mathf.MoveTowards(sink, target, sinkDepth / Mathf.Max(0.01f, sinkSeconds) * Time.deltaTime);
            offset.Set("pond", sink);

            bool moving = walker != null && walker.CurrentSpeed > 0.1f;
            if (InWater && moving && rippleMaterial != null && walker != null && walker.Planet != null)
            {
                rippleTimer -= Time.deltaTime;
                if (rippleTimer <= 0f)
                {
                    rippleTimer = rippleInterval;
                    var r = Get();
                    r.up = walker.Planet.UpAt(transform.position);
                    r.center = walker.Planet.SnapToSurface(transform.position, 0.03f);
                    r.t = 0f;
                }
            }
            UpdateRipples();
        }

        void UpdateRipples()
        {
            foreach (var r in pool)
            {
                if (!r.live) continue;
                r.t += Time.deltaTime / Mathf.Max(0.05f, rippleSeconds);
                if (r.t >= 1f) { r.live = false; r.line.enabled = false; continue; }
                float rad = Mathf.Lerp(rippleStartRadius, rippleEndRadius, r.t);
                Vector3 a = Vector3.Cross(r.up, Mathf.Abs(r.up.y) < 0.9f ? Vector3.up : Vector3.forward).normalized;
                Vector3 b = Vector3.Cross(r.up, a);
                for (int i = 0; i < rippleSegments; i++)
                {
                    float ang = Mathf.PI * 2f * i / rippleSegments;
                    r.line.SetPosition(i, r.center + (a * Mathf.Cos(ang) + b * Mathf.Sin(ang)) * rad);
                }
                var c = rippleColor;
                c.a *= 1f - r.t;
                r.line.startColor = c;
                r.line.endColor = c;
            }
        }

        Ripple Get()
        {
            foreach (var r in pool)
                if (!r.live) { r.live = true; r.line.enabled = true; return r; }
            var go = new GameObject("PondRipple");
            go.transform.SetParent(null, false);   // world space, not moving with the fly
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = true;
            line.positionCount = Mathf.Max(6, rippleSegments);
            line.widthMultiplier = rippleWidth;
            line.sharedMaterial = rippleMaterial;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            rippleSegments = line.positionCount;
            var rp = new Ripple { line = line, live = true };
            pool.Add(rp);
            return rp;
        }

        void OnDestroy()
        {
            foreach (var r in pool) if (r.line != null) Destroy(r.line.gameObject);
        }
    }
}
```

### 4-16. UI

#### `Scripts/UI/TitleScreen.cs` (교체)

```csharp
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FlyJam
{
    /// Title (S01, A06). Buttons: あそぶ (StartGame) and the language button (ToggleLanguage), driven by MenuNavigator.
    /// Title image: drag a sprite into 'titleSprite' (this component). Set = the image shows and the text title hides.
    /// Empty = the text title (title.name) as before.
    public class TitleScreen : MonoBehaviour
    {
        [Tooltip("Title logo. Empty = text title")]
        [SerializeField] Sprite titleSprite;
        [SerializeField] Image titleImage;          // Canvas/TitleImage (Preserve Aspect on)
        [SerializeField] Graphic titleText;         // Canvas/TitleText (its TMP component)

        void Start()
        {
            TimeControl.ResetAll();
            ApplyTitle();
        }

        void OnValidate() => ApplyTitle();   // preview in the editor when the sprite is dropped in

        void ApplyTitle()
        {
            bool hasImage = titleSprite != null;
            if (titleImage != null)
            {
                titleImage.sprite = titleSprite;
                titleImage.preserveAspect = true;
                titleImage.enabled = hasImage;
            }
            if (titleText != null) titleText.enabled = !hasImage;
        }

        public void StartGame() => SceneFlow.GoToGame();

        public void ToggleLanguage()
        {
            Loc.Toggle();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
```

#### `Scripts/UI/ResultPanel.cs` (교체)

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
                if (SessionResult.TryGetStat("level", out float level) && level > 0f)
                    sb.AppendLine(Loc.Format("result.level", level));
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

그다음 컴파일 → **자가 확인 1**.

---

## 5. 단계 2 — 에셋

### 5-1. 밸런스 에셋

`Unity_RunCommand`로 그대로 실행. 새 에셋 2개를 만들고, 구조가 바뀐 7개를 **코드 기본값으로 초기화**한다(`CourtshipBalance`, `RivalBalance`는 그대로).

```csharp
using UnityEditor;
using UnityEngine;

internal class CommandScript : IRunCommand
{
    const string Dir = "Assets/_Project/Data/Balance/";

    public void Execute(ExecutionResult result)
    {
        Create<FlyJam.LevelBalance>(Dir + "LevelBalance.asset");
        Create<FlyJam.CameraBalance>(Dir + "CameraBalance.asset");
        ResetToDefaults<FlyJam.PlayerMoveBalance>(Dir + "PlayerMoveBalance.asset");
        ResetToDefaults<FlyJam.SessionBalance>(Dir + "SessionBalance.asset");
        ResetToDefaults<FlyJam.HealthBalance>(Dir + "HealthBalance.asset");
        ResetToDefaults<FlyJam.ItemBalance>(Dir + "ItemBalance.asset");
        ResetToDefaults<FlyJam.MateBalance>(Dir + "MateBalance.asset");
        ResetToDefaults<FlyJam.DodgeBalance>(Dir + "DodgeBalance.asset");
        ResetToDefaults<FlyJam.HazardBalance>(Dir + "HazardBalance.asset");
        AssetDatabase.SaveAssets();
    }

    static void Create<T>(string path) where T : ScriptableObject
    {
        if (AssetDatabase.LoadAssetAtPath<T>(path) != null) { Debug.Log("[CHI06] already exists: " + path); return; }
        AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<T>(), path);
        Debug.Log("[CHI06] created: " + path);
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
        Debug.Log("[CHI06] reset to code defaults: " + path);
    }
}
```
확인: `LevelBalance` → `Levels` 10줄(Lv.1 expToNext 3, maxSpeed 3.6 … Lv.10 expToNext 0, 6.3). `HazardBalance` → `Types` 4줄(male firstDelay 20, speed 3.8, stopOnDodge 1, chaseCooldown 3). `SessionBalance` → dayLength 80.

### 5-2. 텍스트 표 (52줄)

1. 아래 TSV를 그대로 `FlyGame/Temp/CHI06-text.tsv`에 UTF-8로 저장(칸 구분 = **탭**, 큰 공백은 전각 U+3000). 확인: `awk -F'\t' 'NF!=3{print NR": "NF}' FlyGame/Temp/CHI06-text.tsv` → 출력 없음.
2. `Unity_RunCommand`:
```csharp
using UnityEditor;
using UnityEngine;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var table = AssetDatabase.LoadAssetAtPath<FlyJam.TextTable>("Assets/_Project/Data/Balance/Resources/TextTable.asset");
        string tsv = System.IO.File.ReadAllText("Temp/CHI06-text.tsv", System.Text.Encoding.UTF8);
        int n = table.EditorImportTsv(tsv);
        AssetDatabase.SaveAssets();
        FlyJam.Loc.Reload();
        Debug.Log("[CHI06] TextTable rows: " + n + " (expected 52)");
    }
}
```

```tsv
key	en	ja
title.name	LITTLE FLY PLANET	ちいさなハエのほし
title.goal	Find the female fly and win her heart!	メスのハエを見つけて、求愛しよう！
title.play	PLAY	あそぶ
title.language	日本語	English
hud.hp	HP	体力
hud.lowhp	So hungry...	おなかがペコペコ…
hud.guide.pad	Auto-run   Left stick: turn	自動で進む　左スティックで曲がる
hud.guide.kb	Auto-run   A / D: turn	自動で進む　A・Dキーで曲がる
hud.lv	Lv.{0}	Lv.{0}
hud.lvmax	MAX	MAX
level.up	LEVEL UP!	レベルアップ！
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
bubble.night.1	Night looks kind of scary...	夜は、なんだかこわく見える…
bubble.night.2	The males are getting stronger...!	オスが強くなってる…！
bubble.male	A male is coming!	オスが来た！
bubble.mate.seen	There she is!	メスだ！
bubble.mate.near	Maybe I can get close now...	今なら近づけそう…
bubble.court.hard	I need to get stronger...	もっと強くならなきゃ…
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
result.level	Level reached: Lv.{0:0}	到達レベル: Lv.{0:0}
fact.header	FLY FACT	ハエまめ知識
fact.1	Fruit flies dodge obstacles hundreds of times faster than today's robots.	ショウジョウバエは、今のロボットより何百倍も速く障害物をよける。
fact.2	Motion-blind fruit flies can't see things while they move. When an object stops, it seems to pop into view.	運動盲のショウジョウバエには、動いている物が見えない。物が止まると、急に見えるようになるらしい。
fact.3	Even without sight, a fruit fly stops when something is in front of it. Its antennae seem to feel the air flow.	目が見えなくても、ショウジョウバエは前に何かあると止まる。触角で空気の流れを感じているらしい。
fact.4	Blind fruit flies may also notice shadows with light receptors. (Research in progress)	目の見えないショウジョウバエも、光受容体で影に気づくのかもしれない。(研究中)
fact.5	A male fruit fly sings a courtship song to the female - sometimes hundreds of times.	オスのショウジョウバエは、メスに求愛の歌をうたう。ときには何百回も。
```
바뀐 줄: `hud.lowhp`(→ おなかがペコペコ…). 새 줄: `hud.lv`, `hud.lvmax`, `level.up`, `result.level`, `bubble.*` 6개. 모든 글자는 Silver 폰트에 있다(확인 완료).

### 5-3. 바나나 머티리얼

`Materials/M_FruitBody.mat`을 `AssetDatabase.CopyAsset`으로 두 번 복사해 `M_Banana.mat`, `M_BananaStem.mat`을 만들고 색만 바꾼다(`_BaseColor`가 있으면 그것, 없으면 `_Color`):
- `M_Banana`: (1.0, 0.85, 0.2, 1)
- `M_BananaStem`: (0.35, 0.22, 0.1, 1)

### 5-4. 오디오 파일 연결

1. `Assets/_Project/Audio/`가 있으면 안의 `AudioClip`을 모두 나열한다. 없거나 비었으면 이 절은 건너뛰고 "미완료: 오디오 파일 없음"이라고 보고한다(슬롯은 비워 둔다).
2. 파일 이름(대소문자 무시)에 아래 단어가 있으면 `SYS_Audio`(6-7)의 `AudioDirector` 슬롯에 넣는다. 한 파일이 여러 규칙에 걸리면 표의 **위쪽** 규칙이 이긴다.

| 순서 | 이름에 포함 | 넣을 슬롯 |
|---|---|---|
| 1 | `bgm`, `music`, `배경` | `music` |
| 2 | `victory`, `win`, `clear`, `승리`, `勝利` | `victory` |
| 3 | `heal`, `힐`, `回復` | `itemEaten.clip` |
| 4 | `damage`, `hit`, `데미지`, `ダメージ` | `damage.clip` |
| 5 | `dodge`, `회피`, `回避` | `dodgeSuccess.clip` |
| 6 | `get`, `pickup`, `item`, `획득`, `獲得` | `courtshipCorrect.clip`, `rivalPress.clip`, `menuMove.clip`, `menuSelect.clip` (같은 파일 4곳) |

3. 음악 파일(배경음)의 임포트 설정: Load Type = **Streaming**. 나머지 효과음은 기본값.
4. 못 찾은 슬롯·어느 규칙에도 안 걸린 파일은 보고서에 이름과 함께 적는다. `levelUp`, `fever`, `dash`, `warning`, `courtshipWrong`, `rivalWin`, `pauseOpen`, `bubble`, `gameOver`는 **비워 둔다**(나중에 받음).

**자가 확인 2**: Console 에러 0, 로그 `[CHI06] created/reset` 9줄, `TextTable rows: 52`.

---

## 6. 단계 3 — 프리팹·씬

### 6-0. 공통 규칙 (CHI/05와 같음)

- 프리팹 편집: `PrefabUtility.LoadPrefabContents` → 수정 → `SaveAsPrefabAsset` → `UnloadPrefabContents`.
- 새 UI: `new GameObject(name, typeof(RectTransform))` + `SetParent(parent, false)`. RunCommand 안에서는 `UnityEngine.UI.Image`처럼 풀네임.
- 텍스트: `TMPro.TextMeshProUGUI`, 폰트 `Assets/_Project/Art/Fonts/Silver SDF.asset`, Bold 끔, `raycastTarget` 끔. `LocalizedText`는 표에 "LocalizedText"라고 쓴 곳만(코드가 글자를 쓰는 곳엔 붙이지 않음).
- 장식 Image `raycastTarget` 끔.
- 표기: **앵커** (minX,minY)-(maxX,maxY) / **피벗** / **위치** = anchoredPosition / **크기** = sizeDelta. "꽉 채움" = 앵커 (0,0)-(1,1), offset 0.
- 새 Canvas: Screen Space - Overlay, `CanvasScaler` Scale With Screen Size 1920×1080, Match 0.5, `GraphicRaycaster`.
- 참조 연결은 `SerializedObject.FindProperty(...)` + `ApplyModifiedPropertiesWithoutUndo()`(리플렉션 금지).

### 6-1. `Fly.prefab` (Mate Variant가 물려받음)

루트에 컴포넌트 2개 추가:
| 컴포넌트 | 필드 |
|---|---|
| `FlyJam.VisualOffset` | target = 자식 `Visual` |
| `FlyJam.PondSink` | rippleMaterial = `Materials/M_Particle.mat`. 나머지 기본값 |

`SphereWalker.moveSpeed` = 3.6 (Level이 꺼졌을 때의 대비값). 다른 값은 그대로.
`Mate.prefab`: 위 두 컴포넌트에 오버라이드가 없는지 확인만 한다(있으면 Revert).

### 6-2. `Banana.prefab` (신규)

```
Banana                    AppleVisual(visual = Visual), ToonifyOnSpawn(설정은 Apple.prefab과 같게)
└ Visual                  위치 0, 스케일 (1.2, 1.2, 1.2)
  ├ Seg0 (Capsule)        위치 (-0.212, 0.088, 0), 회전 Z -135, 스케일 (0.16, 0.14, 0.16), M_Banana
  ├ Seg1 (Capsule)        위치 (-0.078, 0.010, 0), 회전 Z -105, 스케일 (0.16, 0.14, 0.16), M_Banana
  ├ Seg2 (Capsule)        위치 ( 0.078, 0.010, 0), 회전 Z  -75, 스케일 (0.16, 0.14, 0.16), M_Banana
  ├ Seg3 (Capsule)        위치 ( 0.212, 0.088, 0), 회전 Z  -45, 스케일 (0.16, 0.14, 0.16), M_Banana
  └ Stem (Cube)           위치 ( 0.285, 0.175, 0), 회전 Z   40, 스케일 (0.05, 0.09, 0.05), M_BananaStem
```
- 캡슐 4개가 아래로 휜 초승달 모양(바나나)이 되면 된다. 기본 도형의 **Collider는 모두 지운다**.
- `Prefabs/Banana.prefab`으로 저장. 크기는 사과와 비슷하게(가로 약 0.6). 어색하면 Seg 스케일만 조절해도 된다(판단 기록에 적기).

### 6-3. `SYS_Items.prefab`

- `AppleSpawner`: `prefabs` 2칸 = `apple` → `Apple.prefab`, `banana` → `Banana.prefab`. `applePrefab`은 그대로 둔다. balance = `ItemBalance.asset`(그대로).
- `Points/P0` ~ `P7` 각각에 `FlyJam.KeepClearZone` 추가, radius 1.2. (마을 생성기는 `max(자기 keepClearRadius 3, 1.2)`를 쓰므로 실제로는 반지름 3 안에 소품이 안 생긴다.)

### 6-4. `SYS_Health.prefab`

`Canvas/HealthBar`:
| 오브젝트 | 작업 | 값 |
|---|---|---|
| `Frame/Inner/Chip` | 신규 Image, `Fill`의 **앞 형제**(Inner의 첫 자식) | 앵커 (0,0)-(1,1), offset 0, 색 (0.9, 0.2, 0.18, 1) |
| `Frame/Inner/Fill` | 그대로 | (Chip 다음 순서 → 초록이 빨강 위에 그려짐) |
| `LowHpMessage` | 위치만 | (0, -44) — EXP 막대(6-5) 아래로 |
| `HealthBar` 루트 | 그대로 | 폭은 코드가 정한다(Lv.1 = 104 + 10×30 = 404) |

`HealthBar` 컴포넌트: barRoot = `HealthBar`(자기 RectTransform), fill = `Fill`, fillImage = `Fill` Image, chip = `Chip`, chipImage = `Chip` Image, valueText / deltaText / lowHpMessage = 그대로, balance = `HealthBalance.asset`. 나머지 기본값.

### 6-5. `SYS_Level.prefab` (신규)

```
SYS_Level                              PlayerLevel (balance = LevelBalance.asset)
└ Canvas                               Canvas(sortingOrder 2) + CanvasScaler + GraphicRaycaster
  ├ FeverVignette                      꽉 채움, RawImage(색 흰색, raycast 끔) + FlyJam.FeverVignette
  ├ ExpBar                             앵커 (0,1)-(0,1), 피벗 (0,1), 위치 (32,-74), 크기 (440,26), FlyJam.ExpBar
  │ ├ LevelText                        앵커 (0,0)-(0,1), 피벗 (0,0.5), 위치 (0,0), 크기 (96,0), TMP 글자 28, 흰색, 왼쪽 정렬, "Lv.1"
  │ ├ Frame                            앵커 (0,0.5)-(0,0.5), 피벗 (0,0.5), 위치 (104,0), 크기 (200,14), Image (0,0,0,0.6)
  │ │ └ Fill                           앵커 (0,0)-(0,1), offsetMin (2,2), offsetMax (0,-2), Image (0.45,0.8,1,1)
  │ └ ValueText                        앵커 (0,0.5)-(0,0.5), 피벗 (0,0.5), 위치 (314,0), 크기 (130,28), TMP 글자 26, 흰색, 왼쪽 정렬, "0/3"
  └ LevelUpPopup                       앵커 가운데, 크기 (640,100), TMP 글자 64, 색 (1,0.85,0.3,1), 가운데 정렬, "レベルアップ！", FlyJam.LevelUpPopup(text = 자기 TMP)
```
- `ExpBar` 컴포넌트: levelText = `LevelText`, fill = `Frame/Fill`(RectTransform), valueText = `ValueText`.
- `Fill`의 `anchorMax.x`를 코드가 0~1로 바꾼다(offset은 위 값 그대로).
- `FeverVignette`, `LevelUpPopup`의 다른 필드는 기본값. player/session/worldCamera는 비움.

### 6-6. `SYS_Bubble.prefab` (신규)

```
SYS_Bubble                             SpeechBubble, BubbleTriggers
└ Canvas                               Canvas(sortingOrder 9) + CanvasScaler + GraphicRaycaster
  └ Bubble                             앵커 가운데, 피벗 (0.5,0), 크기 (300,80)  ← 코드가 크기·위치를 바꿈
    ├ Box                              꽉 채움, Image (흰색, raycast 끔)       ← 스프라이트·Sliced는 코드가 넣음
    ├ Tail                             앵커 (0.5,0)-(0.5,0), 피벗 (0.5,1), 위치 (0,4), 크기 (28,16), Image (흰색, raycast 끔)
    └ Text                             앵커 (0,0)-(1,1), offsetMin (20,13), offsetMax (-20,-13), TMP 글자 34, 색 (0.16,0.15,0.2,1), 가운데 정렬, 줄바꿈 켬, "メスだ！"
```
`SpeechBubble`: root = `Bubble`, box = `Box`, tail = `Tail`, text = `Text`. 나머지 기본값(headHeight 1.3, showSeconds 2.5, pixelScale 4). `BubbleTriggers`는 기본값(nightKeys = bubble.night.1, bubble.night.2).

### 6-7. `SYS_Audio.prefab` (신규)

```
SYS_Audio                              AudioDirector, RumbleDirector
```
- 자식 없음. `AudioDirector` 슬롯은 5-4대로(없으면 비움). musicVolume 0.6, pauseDuck 0.5, 나머지 기본값.
- `RumbleDirector` 기본값(rumbleOn 켬).

### 6-8. `SYS_Hazards.prefab`, `Enemy_Male.prefab`

- `SYS_Hazards/Canvas/PromptRoot` 위치 (0,-150) → **(0,-170)** (EXP 막대·배고픔 문구와 안 겹치게).
- `Enemy_Male.prefab` 루트에 `FlyJam.NightGlowEyes` 추가: eyes = [`Visual/BodyRoot/EyeL`, `Visual/BodyRoot/EyeR`]의 MeshRenderer, halo = `Halo`의 Renderer. 나머지 기본값.
- `ObstacleSpawner`, `NearMissDirector` 필드는 그대로(balance = HazardBalance).

### 6-9. `SYS_Mate.prefab`

- 중첩 `Mate` 인스턴스의 위치를 **(2.04, -11.82, 0)** (시작 지점의 거의 반대편)로. 회전은 그대로.
- `Canvas/MateHint`: RectTransform 크기 (72,62). `MateFinderHint`: icon = 자기 RawImage, 나머지 **기본값**(edgeMargin 0.06, sizeNear 72, sizeFar 36, distanceNear 3, distanceFar 24, seenDistance 16, seenBubbleKey bubble.mate.seen). 옛 필드(maxDistance, size)는 저장 시 사라진다.
- `MateAI`: balance = `MateBalance.asset`(그대로). notShyBubbleKey 기본값.

### 6-10. `SYS_Village.prefab` — 가시 삭제, 연못 2곳

1. `Village`의 `VillageGenerator.rules`에서 label `Thorn` 항목을 **배열에서 제거**한다(다른 항목 순서·값 유지).
2. `Pond`:
   - `PlanetProp.kind` = **Decor (0)** (지금 Blocker라 들어갈 수 없음). radius 2.5 그대로.
   - `FlyJam.WaterZone` 추가, radius 2.5.
3. `Pond`를 복제해 **`Pond2`** (같은 부모):
   - `SurfacePlacer.direction` = **(0, 0.2, 0.98)**, offset 0. (사과 지점·시작 지점·암컷·BigTree·Pond와 모두 53° 이상 떨어진 곳. 시작해서 직진하면 몇 초 뒤 만난다)
   - `SurfaceDisc`, `PlanetProp`(Decor), `WaterZone`, `ToonifyOnSpawn`은 복제된 값 그대로.
4. `VillageGenerator.landmarks`에 `Pond2`의 `PlanetProp`을 추가(마을 소품이 연못 위에 안 생기게). 기존 2개(Pond, BigTree) 유지.

### 6-11. `SYS_Fx.prefab`

| 작업 | 값 |
|---|---|
| `FxDirector.dodgeTrail` | **None**으로 비우고, 자식 `FX_DodgeTrail` 오브젝트 삭제 |
| `FlyJam.DashRingFx` 추가 (루트) | material = `Materials/M_ParticleAdd.mat`, 나머지 기본값 |
| `FlyJam.WindGusts` 추가 (루트) | material = `Materials/M_Particle.mat`, pollen = 자식 `FX_Pollen`의 ParticleSystem, 나머지 기본값 |
| `FX_Pollen` ParticleSystem | Main 모듈 Simulation Space = **World** (이미 World면 그대로) |

### 6-12. `Game.unity`

1. 루트에 `SYS_Level`, `SYS_Bubble`, `SYS_Audio` 인스턴스 추가(위치 0, 다른 `SYS_*`와 같은 높이).
2. `GameContext`의 followCamera가 가리키는 카메라 오브젝트(`Main Camera`)의 `PlanetFollowCamera`: balance = `Data/Balance/CameraBalance.asset`.
3. 저장. `SYS_*` = Health, Courtship, Sky, Village, Fx, Dodge, Mate, UI, Hazards, Items, Rival, Menu, Level, Bubble, Audio = **15개**.

### 6-13. `Title.unity`

| 오브젝트 | 작업 | 값 |
|---|---|---|
| `Canvas/TitleImage` | 신규 Image, `TitleText`의 **바로 앞 형제** | 앵커 가운데, 위치 (0,170), 크기 (1400,420), Preserve Aspect 켬, raycast 끔, Image **enabled 끔**, 스프라이트 없음 |
| `TitleScreen` 컴포넌트 | 필드 | titleImage = `TitleImage`, titleText = `TitleText`의 TextMeshProUGUI, titleSprite = 비움 |
| 루트 | `SYS_Audio` 인스턴스 추가 | (타이틀부터 음악이 나옴) |

### 6-14. 가시 프리팹 삭제

1. 프로젝트 전체에서 `Thorn.prefab`의 GUID(`.meta`의 guid)를 검색해 6-10 이후 참조가 0인지 확인.
2. 셸로 `Prefabs/Thorn.prefab`(+`.meta`) 삭제. `M_Thorn.mat`, `M_ThornBase.mat`, `M_HaloThorn.mat`은 각각 GUID 검색 → 참조 0이면 삭제, 남은 참조가 있으면 두고 보고.
3. `AssetDatabase.Refresh()`.

**자가 확인 3**: 모든 프리팹·씬 저장, Console 에러 0.

---

## 7. 단계 4 — 자가 검증

### 7-1. 참조 검사 스크립트 (`Unity_RunCommand`)

`MISSING` / `NOT FOUND` 줄이 하나도 없어야 한다.

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
        bad += Check(P + "Fly.prefab", "VisualOffset", "target");
        bad += Check(P + "Fly.prefab", "PondSink", "rippleMaterial");
        bad += Check(P + "Banana.prefab", "AppleVisual", "visual");
        bad += Check(P + "Enemy_Male.prefab", "NightGlowEyes", "eyes", "halo");
        bad += Check(P + "SYS_Items.prefab", "AppleSpawner", "balance", "prefabs", "points");
        bad += Check(P + "SYS_Health.prefab", "HealthBar", "balance", "barRoot", "fill", "fillImage", "chip", "chipImage", "valueText", "deltaText", "lowHpMessage");
        bad += Check(P + "SYS_Level.prefab", "PlayerLevel", "balance");
        bad += Check(P + "SYS_Level.prefab", "ExpBar", "levelText", "fill", "valueText");
        bad += Check(P + "SYS_Level.prefab", "LevelUpPopup", "text");
        bad += Check(P + "SYS_Bubble.prefab", "SpeechBubble", "root", "box", "tail", "text");
        bad += Check(P + "SYS_Hazards.prefab", "NearMissDirector", "balance", "promptUI", "promptText", "frameUI", "perfectText");
        bad += Check(P + "SYS_Hazards.prefab", "ObstacleSpawner", "balance", "prefabs");
        bad += Check(P + "SYS_Dodge.prefab", "DodgeAbility", "balance");
        bad += Check(P + "SYS_Mate.prefab", "MateFinderHint", "icon");
        bad += Check(P + "SYS_Mate.prefab", "MateAI", "balance");
        bad += Check(P + "SYS_Fx.prefab", "DashRingFx", "material");
        bad += Check(P + "SYS_Fx.prefab", "WindGusts", "material", "pollen");
        bad += Check(P + "SYS_Village.prefab", "WaterZone");
        bad += Check(P + "SYS_Courtship.prefab", "CourtshipQTE", "balance", "panel", "titleText", "slots", "slotFrames", "infoText");
        bad += Check(P + "SYS_Menu.prefab", "ResultPanel", "panel", "titleText", "statsText");
        bad += Check(P + "SYS_Audio.prefab", "AudioDirector");
        bad += Check(P + "SYS_Audio.prefab", "RumbleDirector");
        int waters = 0;
        var village = AssetDatabase.LoadAssetAtPath<GameObject>(P + "SYS_Village.prefab");
        if (village != null) foreach (var mb in village.GetComponentsInChildren<MonoBehaviour>(true))
            if (mb != null && new SerializedObject(mb).FindProperty("m_Script").objectReferenceValue.name == "WaterZone") waters++;
        if (waters != 2) { Debug.LogError("[CHI06] NOT FOUND: expected 2 WaterZone in SYS_Village, found " + waters); bad++; }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(P + "Thorn.prefab") != null) { Debug.LogError("[CHI06] Thorn.prefab still exists"); bad++; }
        Debug.Log(bad == 0 ? "[CHI06] reference check OK" : "[CHI06] reference check: " + bad + " problem(s)");
    }

    /// Every listed field of every component with this script name must be set (arrays: size > 0 and no empty slot).
    static int Check(string path, string script, params string[] fields)
    {
        var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (root == null) { Debug.LogError("[CHI06] NOT FOUND prefab " + path); return 1; }
        int bad = 0, found = 0;
        foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb == null) { Debug.LogError("[CHI06] MISSING script on " + path); bad++; continue; }
            var so = new SerializedObject(mb);
            var ms = so.FindProperty("m_Script").objectReferenceValue;
            if (ms == null || ms.name != script) continue;
            found++;
            foreach (var f in fields)
            {
                var p = so.FindProperty(f);
                if (p == null) { Debug.LogError($"[CHI06] NOT FOUND field {script}.{f} in {path}"); bad++; continue; }
                if (p.isArray && p.propertyType != SerializedPropertyType.String)
                {
                    if (p.arraySize == 0) { Debug.LogError($"[CHI06] MISSING {script}.{f} (empty) in {path}/{mb.name}"); bad++; }
                    for (int i = 0; i < p.arraySize; i++)
                    {
                        var e = p.GetArrayElementAtIndex(i);
                        if (e.propertyType == SerializedPropertyType.ObjectReference && e.objectReferenceValue == null)
                        { Debug.LogError($"[CHI06] MISSING {script}.{f}[{i}] in {path}/{mb.name}"); bad++; }
                        if (e.propertyType == SerializedPropertyType.Generic)
                        {
                            var pf = e.FindPropertyRelative("prefab");
                            if (pf != null && pf.objectReferenceValue == null)
                            { Debug.LogError($"[CHI06] MISSING {script}.{f}[{i}].prefab in {path}"); bad++; }
                        }
                    }
                }
                else if (p.propertyType == SerializedPropertyType.ObjectReference && p.objectReferenceValue == null)
                { Debug.LogError($"[CHI06] MISSING {script}.{f} in {path}/{mb.name}"); bad++; }
            }
        }
        if (found == 0) { Debug.LogError($"[CHI06] NOT FOUND component {script} in {path}"); bad++; }
        return bad;
    }
}
```

### 7-2. 눈으로 확인 (에이전트)

| # | 확인 | 기대 |
|---|---|---|
| 1 | `Game.unity` 루트 | `SYS_Level`, `SYS_Bubble`, `SYS_Audio` 있음(SYS 15개). Main Camera `PlanetFollowCamera.balance` = CameraBalance |
| 2 | `Title.unity` | `TitleImage`(꺼짐) + `SYS_Audio` |
| 3 | 프로젝트 검색 `AppleEaten`, `neutralMode`, `speedRatio`, `MakeBeep` | 코드 어디에도 없음 |
| 4 | 프로젝트 검색 `Time.timeScale =` | `TimeControl.cs`에만 |
| 5 | `SYS_Village` | `Thorn` 규칙 없음, `Pond`·`Pond2` 둘 다 Decor + WaterZone, landmarks 3개 |
| 6 | 캔버스 정렬 | HUD 0, Health 1, Level 2, Bubble 9, Hazards 10, Mate 11, Courtship 12, Rival 13, Menu 20 |
| 7 | `TextTable` | 52줄, `bubble.night.1` 있음 |
| 8 | UI 캡처(AGENTS 6장 방법: 임시 ScreenSpaceCamera) | `SYS_Level`(EXP 막대·팝업), `SYS_Bubble`(말풍선을 임시로 켜서), `SYS_Health`(Chip 포함), `Title`(TitleImage 자리) → 보고서 첨부. **저장하지 말고** 다시 열기 |
| 9 | 오디오 슬롯 | 5-4 표대로 채워졌는지(또는 파일 없음 보고) |

Play는 MCP로 켤 수 없으므로 사람이 11장으로 확인한다.

---

## 8. AGENTS.md 수정

`FlyGame/AGENTS.md`에서:

**2.2절** 첫 문장의 "13개 어셈블리(런타임 12 + …)" → "**16개 어셈블리(런타임 15 + …)**". Features 두 줄을 교체:
```markdown
- **Features** (13개): `Features/{Audio, Bubble, Courtship, Dodge, Fx, Hazards, Health, Items, Level, Mate, Rival, Sky, Village}/FlyJam.<Name>.asmdef`
  - 기본 참조: `FlyJam.Core` (`Hazards`·`Rival`은 `+ Unity.TextMeshPro`, `Courtship`·`Health`·`Level`·`Bubble`은 `+ Unity.TextMeshPro, UnityEngine.UI`, `Mate`는 `+ UnityEngine.UI`, `Audio`는 `+ Unity.InputSystem`)
```

**2.3절** 제목을 "(15개)"로, 첫 문장의 "12개"를 "15개"로. 목록에서 아래 줄을 바꾸거나 추가:
```markdown
- **`SYS_Village.prefab`**: `Village` (`VillageGenerator`, `Generated`는 비어 있음, `Fruit` count 0, `Thorn` 없음, landmarks = Pond·BigTree·Pond2), `Pond`·`Pond2` (`SurfacePlacer`, `SurfaceDisc`, `PlanetProp`(Decor = 들어갈 수 있음), `WaterZone`, `ToonifyOnSpawn`), `BigTree`.
- **`SYS_Items.prefab`**: `AppleSpawner`(`ItemBalance` + 시트 `ItemTypes`, `Apple.prefab`·`Banana.prefab`, 고정 지점 `Points/P0~P7` 각각 `KeepClearZone`).
- **`SYS_Level.prefab`**: `PlayerLevel`(`LevelBalance` + 시트 `LevelTable`: 레벨별 속도·회전·최대 체력·바나나 확률·구애 난이도, 레벨업 피버), 자체 `Canvas`(`sortingOrder 2`: `FeverVignette`, `ExpBar`, `LevelUpPopup`).
- **`SYS_Bubble.prefab`**: `SpeechBubble`(주인공 머리 위 픽셀 말풍선, `BubbleRequested` 큐), `BubbleTriggers`(밤 대사), 자체 `Canvas`(`sortingOrder 9`).
- **`SYS_Audio.prefab`**: `AudioDirector`(음악·효과음 슬롯, DontDestroyOnLoad, Title·Game 두 씬에 배치), `RumbleDirector`(패드 진동).
- **`SYS_Fx.prefab`**: `FxDirector`(+ 아이템 먹기), `AmbientParticles`, `DashRingFx`(대시 링), `WindGusts`(바람 줄기), 파티클 6종.
```

**2.2절 "고정된 코어 계약"** 뒤에 한 줄 추가:
```markdown
**레벨 정보 공유**: 레벨이 정하는 값은 Core의 `PlayerProgress`(`LevelInfo` 한 줄 + `Fever`)로만 읽는다. 쓰는 곳은 Level 기능 하나. 모델 높이는 `VisualOffset.Set(키, 값)`(키: `fever`, `pond`)으로만 바꾼다.
```

**3절 표**의 CHI 줄 끝에 추가: `06 레벨(EXP·속도·회전·최대 체력 성장, 피버), 앞 대시, 수컷 회피 정지·낮밤 강화, 바나나, 체력 막대 개편, 말풍선, 연못 2곳·잠김, 가시 삭제, 바람·대시 링, 사운드·진동, 타이틀 이미지 칸.`

---

## 9. 보고 형식 (에이전트)

```
## CHI/06 보고
1. 만든/바꾼/지운 파일 (3장 표와 대조, 빠진 것 표시)
2. 스펙과 다르게 한 것: 파일·줄·무엇·왜
3. 판단 기록: 스펙이 애매해서 직접 정한 것과 이유
4. 미완료 항목과 이유 (예: 오디오 파일 없음)
5. 로그: [CHI06] created/reset 9줄, TextTable rows 52, reference check OK
6. 오디오 연결 결과: 슬롯 ← 파일 이름
7. 7-2 표 1~9 결과 + UI 캡처
8. Console 최종 상태 (에러 0, 경고 목록)
```

---

## 10. 사람 — 시트 작업 (돌아와서, Play 전에)

**Pull보다 Copy가 먼저.** `FlyJam → Balance Sync`에서 각 줄의 **Copy '탭' (TSV)** → 구글 시트의 그 탭(없으면 만들기)을 **전부 지우고** A1에 붙여넣기.

| 에셋 | 시트 탭 | 상태 |
|---|---|---|
| LevelBalance | `Level` | 새 탭 |
| LevelBalance | `LevelTable` | 새 탭 (10줄) |
| CameraBalance | `Camera` | 새 탭 |
| ItemBalance | `ItemTypes` | 새 탭 (사과·바나나) |
| ItemBalance | `Items` | 지우고 다시 |
| PlayerMoveBalance | `Player` | 지우고 다시 |
| SessionBalance | `Session` | 지우고 다시 (dayLength 80, nightStart) |
| HealthBalance | `Health` | 지우고 다시 |
| MateBalance | `Mate` | 지우고 다시 |
| DodgeBalance | `Dodge` | 지우고 다시 |
| HazardBalance | `Hazards` | 지우고 다시 |
| HazardBalance | `HazardTypes` | 지우고 다시 (열이 바뀜) |
| TextTable | `Text` | 지우고 다시 (52줄) |

그다음 **Pull** → 값 변화 없음, unknown 경고 없음이어야 한다. `Courtship`, `Rival` 탭은 그대로.

오디오 슬롯이 비어 있으면(보고서 4번): `SYS_Audio` 프리팹을 열고 파일을 슬롯에 끌어다 놓는다(5-4 표).

---

## 11. 체크포인트 (사람)

엑스박스 패드로 확인. 키보드 대체: A·D 조향, Space 액션, Esc 일시정지, 구애 →↓↑←.

### 11-1. 이동·카메라·레벨
| # | 할 일 | 성공이면 |
|---|---|---|
| 1 | 시작 | 왼쪽 위 "体力 5/10", 그 아래 작은 막대 + "Lv.1" + "0/3" |
| 2 | 스틱 중립으로 1초 직진 | 카메라가 멀어져 넓게 보임. 조향하면 바로 돌아옴 |
| 3 | 사과 먹기 | "+2", EXP 1 증가, 반짝 |
| 4 | EXP 3 모으기 | 「レベルアップ！」, 3초 동안 파리가 떠서 위아래로 흔들리며 날갯짓, 화면 가장자리가 노랗게 은은히 맥동, 빨라짐, 적에 닿아도 무사 |
| 5 | 레벨업 직후 체력 막대 | 막대가 한 칸만큼 길어지고 그 칸이 채워져 있음(5/10 → 6/11) |
| 6 | 레벨이 오를수록 | 조향 시 더 작게 꺾이고 더 빠름 |
| 7 | 체력이 낮을 때 속도 | 아주 조금만 느려짐(최대 15%) |

### 11-2. 체력 막대
| # | 할 일 | 성공이면 |
|---|---|---|
| 1 | 적에 맞기 | 초록이 즉시 줄고, 깎인 만큼 빨간 부분이 잠깐 남았다가 부드럽게 사라짐 |
| 2 | 사과 먹기 | 초록이 짧게 차오름 |
| 3 | 50% 이하 | 노랑. 20% 이하 주황 깜빡 + 「おなかがペコペコ…」 |

### 11-3. 대시·수컷
| # | 할 일 | 성공이면 |
|---|---|---|
| 1 | 경고 중 Ⓐ (화면 문구도 "Ⓐボタンで回避！") | **앞으로** 휙 대시. 머리→꼬리로 주황→빨강 링 4개가 빠르게 지나감(은은하게, 픽셀) |
| 2 | 정면에서 오는 잠자리에 대시 | 뚫고 지나가며 맞지 않음 → "回避!" |
| 3 | 20초 전 | 수컷 없음. 20초쯤 「オスが来た！」 |
| 4 | 수컷 경고 중 대시 | 수컷이 그 자리에서 1초 멈춤 + "回避!". 이후 3초 정도 쫓아오지 않음(경고가 끊김) |
| 5 | 밤 (1:02 무렵) | 말풍선 「夜は、なんだかこわく見える…」→「オスが強くなってる…！」. 수컷 눈이 빨갛게 맥동 |
| 6 | 밤의 수컷 | 낮보다 빠르고 다시 쫓아오는 간격이 짧음 |

### 11-4. 아이템·연못·가시
| # | 할 일 | 성공이면 |
|---|---|---|
| 1 | 아이템 | 둥실거림이 전보다 크고 빠름 |
| 2 | 레벨을 올려 가며 | 바나나(노란 초승달)가 점점 자주 나옴. 바나나 = +4(Lv.1 기준), EXP 3 |
| 3 | 아이템 주변 | 나무·바위가 겹쳐 있지 않음 |
| 4 | 시작해서 직진 | 연못에 들어가 몸이 살짝 잠기고 물결 링 |
| 5 | 연못 | 2곳 |
| 6 | 가시 | 행성에 없음 |

### 11-5. 암컷·구애
| # | 할 일 | 성공이면 |
|---|---|---|
| 1 | 시작부터 | 분홍 하트가 화면 가장자리에서 암컷 방향을 계속 가리킴. 가까울수록 커짐 |
| 2 | Lv.1~2로 쫓기 | 멀리서부터 도망가서 다가갈 수 없음 |
| 3 | Lv.3 | 「今なら近づけそう…」. 이제 가까이(5)까지 가야 도망 |
| 4 | 처음 화면에 보일 때 | 「メスだ！」 |
| 5 | 낮은 레벨로 닿기 | 버튼 7~8개 + 짧은 시간 막대 → 거의 실패 → 「もっと強くならなきゃ…」 |
| 6 | Lv.7~8로 닿기 | 4개(Lv.8은 시간 제한 없음) → 성공 가능 → 승리 음악 |
| 7 | 피버 중 암컷에 닿기 | 구애가 시작되지 않음(착지 후 가능) |
| 8 | 결과 패널 | "到達レベル: Lv.N" 줄 |

### 11-6. 사운드·패드·타이틀
| # | 할 일 | 성공이면 |
|---|---|---|
| 1 | 타이틀 | 배경음 시작. あそぶ → 게임에서도 끊기지 않고 이어짐 |
| 2 | 일시정지 | 음악이 절반 볼륨 |
| 3 | 아이템/피격/회피/메뉴·구애 정답·연타 | 힐/데미지/회피/획득 소리 |
| 4 | 수컷 접촉(연타 배틀 시작) | 데미지 소리 없음 |
| 5 | 구애 성공 | 배경음이 페이드아웃, 승리 음악. もういちど → 배경음 다시 |
| 6 | 패드 | 피격 시 짧은 진동, 레벨업 시 조금 긴 진동(Mac에서 안 느껴지면 정상일 수 있음) |
| 7 | `Title`의 `TitleScreen.titleSprite`에 아무 스프라이트 | 텍스트 제목 대신 이미지(비율 유지). 비우면 텍스트 |

### 11-7. 전체
| # | 할 일 | 성공이면 |
|---|---|---|
| 1 | 한 판 시간 | 처음 하는 사람이 약 3분 |
| 2 | `SYS_Level`·`SYS_Bubble`·`SYS_Audio`를 하나씩 끄고 Play | 그 기능만 빠지고 에러 없음(Level을 끄면 CHI-05처럼 동작) |
| 3 | EN/JA 각 한 판 | 빠진 글자 없음 |
| 4 | 전체 | Console 에러 0 |

---

## 12. 튜닝 값 (시트 탭)

### `LevelTable` (한 줄 = 한 레벨)
| level | expToNext | maxSpeed | turnRate | maxHp | rareItemChance | courtLength | courtTime |
|---|---|---|---|---|---|---|---|
| 1 | 3 | 3.6 | 70 | 10 | 0.05 | 8 | 2.5 |
| 2 | 4 | 3.9 | 80 | 11 | 0.08 | 8 | 2.5 |
| 3 | 5 | 4.2 | 90 | 12 | 0.12 | 8 | 3 |
| 4 | 6 | 4.5 | 100 | 13 | 0.15 | 7 | 3.5 |
| 5 | 7 | 4.8 | 110 | 14 | 0.18 | 6 | 5 |
| 6 | 8 | 5.1 | 120 | 15 | 0.21 | 5 | 6 |
| 7 | 10 | 5.4 | 130 | 16 | 0.24 | 4 | 6 |
| 8 | 12 | 5.7 | 140 | 17 | 0.27 | 4 | 0 |
| 9 | 14 | 6.0 | 150 | 18 | 0.30 | 4 | 0 |
| 10 | 0 | 6.3 | 160 | 19 | 0.33 | 4 | 0 |

- `expToNext` 0 = 최대 레벨. 줄을 늘리면 최대 레벨도 늘어난다. 줄이 빠진 레벨은 바로 아래 레벨 값을 쓴다.
- `courtTime` 0 = 시간 제한 없음. 구애 버튼은 최대 8칸.

### `Level`
| key | 기본 | 의미 |
|---|---|---|
| dodgeExp | 1 | 회피 성공 EXP |
| feverSeconds | 3 | 피버 시간 |
| feverSpeedMul | 1.2 | 피버 속도 배율 |
| feverLift | 1.0 | 떠오르는 높이 |
| feverBobHeight / feverBobSpeed | 0.25 / 9 | 위아래 흔들림 |
| feverEaseSeconds | 0.3 | 오르내리는 시간 |

### `ItemTypes` / `Items`
| id | rare | healRatio | exp |
|---|---|---|---|
| apple | 0 | 0.2 | 1 |
| banana | 1 | 0.4 | 3 |

`Items`: respawnDistance 20, pickupRadius 1.0, hoverHeight 0.75, bobHeight 0.35, bobSpeed 4.5, spinSpeed 90, rareChance 0.1(Level 꺼졌을 때만).

### `HazardTypes`
| id | count | firstDelay | speed | damageRatio | warn | rival | chase | detectRadius | loseRadius | turnRate | stopOnHit | stopOnDodge | chaseCooldown | daySpeedUp | nightSpeedUp | dayCooldownCut | nightCooldownCut |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| dragonfly | 2 | 2 | 3.4 | 0.2 | 1 | 0 | 0 | 0 | 0 | 0 | 2 | 0 | 0 | 0 | 0 | 0 | 0 |
| spider | 2 | 2 | 3.4 | 0.2 | 1 | 0 | 0 | 0 | 0 | 0 | 2 | 0 | 0 | 0 | 0 | 0 | 0 |
| male | 1 | 20 | 3.8 | 0 | 1 | 1 | 1 | 6 | 9 | 120 | 2 | 1 | 3 | 0.1 | 0.25 | 0.2 | 0.5 |
| acorn | 1 | 2 | 3.2 | 0.1 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |

`Hazards`: spawnInterval 1.5, spawnAngleMin/Max 50/170, warnRadius 3, warnApproachDot 0.5, slowScale 0.5, successTimeout 3. (옛 `firstDelay` 키는 없어짐 → 종류별 열)

### 기타 탭
| 탭 | key | 기본 | 의미 |
|---|---|---|---|
| `Session` | dayLength / nightStart / resultDelay | 80 / 0.778 / 1.5 | 하루 초, 밤 시작 비율(80초의 62초), 결과까지 초 |
| `Health` | maxHp / startHp / drainPerSecond | 10 / 5 / 0.1 | maxHp는 Level 꺼졌을 때만 |
| `Health` | obstacleDamageRatio / speedAtZeroHp | 0.2 / 0.85 | 체력 0일 때 속도 85% |
| `Health` | invulnerableTime / blinkInterval / midHpRatio / lowHpRatio | 1.5 / 0.08 / 0.5 / 0.2 | 노랑 50%, 주황 20% |
| `Mate` | speed / shyUntilLevel / shyFleeRadius / shyReleaseRadius | 4.4 / 3 / 18 / 20 | 절대 속도, 수줍음 |
| `Mate` | fleeRadius / releaseRadius / failFleeSeconds | 5 / 8 / 2 | 평소 |
| `Dodge` | distance / duration / cooldown | 3 / 0.3 / 0.4 | 앞 대시 |
| `Camera` | idleSeconds / zoomOutSharpness / zoomInSharpness / lookOutDistance / lookOutHeight | 1 / 2.5 / 8 / 12 / 8.25 | 중립 → 넓은 시야 |
| `Player` | maxSpeed / turnRate / steerDeadZone | 3.6 / 70 / 0.15 | 앞 두 개는 Level 꺼졌을 때만 |

---

## 13. 3분 밸런스 시뮬레이션

게임 규칙을 그대로 옮긴 봇으로 160판. 봇은 가장 가까운 아이템으로 가고, Lv.3부터 체력이 35% 넘으면 암컷을 쫓는다. 회피 성공률 70%, 연타 승률 70%, 구애 성공률은 레벨별 춤 난이도로 추정(Lv.3 3%, Lv.4 10%, Lv.5 40%, Lv.6 70%, Lv.7 90%, Lv.8+ 97%).

| 항목 | 결과 |
|---|---|
| 클리어 / 사망 | 95% / 5% |
| 클리어 시간 | 중앙값 **169초**, 빠른 10% 113초, 느린 10% 239초, 90초 미만 2% |
| 레벨 도달(중앙값) | Lv.2 11초, Lv.3 22초, Lv.4 79초, Lv.5 132초, Lv.6 190초 |
| 한 판 평균 | 사과 11, 바나나 1, 회피 8, 피격 1.2, 연타 0.6, 구애 시도 2.2, 클리어 레벨 5.4 |

사람은 봇보다 길 찾기가 느리므로 3분 남짓을 예상. 빨리 끝나면 `Mate.speed`↑ 또는 `LevelTable.expToNext`↑, 늦게 끝나면 반대.

---

## 14. 커밋 (사람)

```bash
cd ~/Developer/sgj2026-flygame
git add FlyGame/Assets/_Project FlyGame/AGENTS.md
git restore --staged "FlyGame/Assets/_Project/Art/Fonts/Silver SDF.asset" FlyGame/Assets/_Project/Materials/M_PixelArtPost.mat 2>/dev/null
git status      # Thorn 삭제가 deleted로 보이고, Silver SDF.asset·M_PixelArtPost.mat·Claude outputs는 없어야 함
git commit -m "Add levels, fever, forward dash, bananas, speech bubbles, wind and dash FX, audio and pad rumble"
git push -u origin CHI/06-growth-feel-audio
```
PR: `CHI/06-growth-feel-audio` → `dev`.
- 오디오 파일(`Assets/_Project/Audio/`)도 같이 커밋된다. 저장소에 올리지 않을 거라면 `git restore --staged FlyGame/Assets/_Project/Audio`.
