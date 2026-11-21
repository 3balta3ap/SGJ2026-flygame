using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FlyJam
{
    /// 코어 상태. 고정된 계약 — 새 기능이 값을 추가하지 않는다 (필요하면 코어 담당과 상의).
    public enum FlyState { Stopped, Walking, Dodging, Celebrating, Disabled }

    /// Tank  = 플레이어: 입력 y = 전진(+)/뒷걸음(-), x = 제자리 회전
    /// Steer = 짝(AI):   입력 (x, y) = 자기 몸 기준으로 가고 싶은 방향, 몸이 그쪽으로 돈다
    public enum MoveMode { Tank, Steer }

    /// 구체 표면 이동. 회피 같은 능력은 별도 컴포넌트가 BeginOverride로 이동을 잠깐 넘겨받는다.
    public class SphereWalker : MonoBehaviour
    {
        [SerializeField] Planet planet;                     // 비우면 GameContext에서
        [SerializeField] MoveMode moveMode = MoveMode.Tank;
        [SerializeField] bool isPlayer = true;
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
        public FlyState State { get; private set; } = FlyState.Stopped;
        public float TimeInState { get; private set; }
        public float CurrentSpeed { get; private set; }
        public float SignedSpeed { get; private set; }
        public float SpeedMultiplier { get; set; } = 1f;
        /// 어떤 능력이 이동을 넘겨받은 중인가 (회피 등)
        public bool IsOverridden => overrides.Count > 0;
        /// 어떤 능력이 무적을 켰는가 (회피 중 장애물 무시)
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
            if (input == null) Debug.LogWarning("[SphereWalker] IMoveInput 컴포넌트가 없습니다.", this);
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

            if (moveMode == MoveMode.Tank) UpdateTank(raw, up);
            else UpdateSteer(raw, up);

            if (State == FlyState.Stopped || State == FlyState.Walking)
                SetState(Mathf.Abs(SignedSpeed) > 0.01f ? FlyState.Walking : FlyState.Stopped);
        }

        void UpdateTank(Vector2 raw, Vector3 up)
        {
            float turn = Mathf.Abs(raw.x) > inputDeadZone ? raw.x : 0f;
            float drive = Mathf.Abs(raw.y) > inputDeadZone ? raw.y : 0f;

            if (turn != 0f) TurnAround(turn * tankTurnSpeed * Time.deltaTime);

            float speed = drive * moveSpeed * (drive < 0f ? backwardSpeedRatio : 1f) * SpeedMultiplier;
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

            CurrentSpeed = move.magnitude * moveSpeed * SpeedMultiplier;
            if (CurrentSpeed > 0f) MoveBy(move * moveSpeed * SpeedMultiplier * Time.deltaTime);
            AlignToSurface();
            if (CurrentSpeed > 0f) TurnToward(move, steerTurnSpeed);
            SignedSpeed = CurrentSpeed;
        }

        // ── 능력(회피 등)이 쓰는 API ──

        public void BeginOverride(object by) { if (by != null) overrides.Add(by); }
        public void EndOverride(object by) { if (by != null) overrides.Remove(by); }

        public void SetInvulnerable(object by, bool on)
        {
            if (by == null) return;
            if (on) invulnerable.Add(by); else invulnerable.Remove(by);
        }

        /// 표면을 따라 이동 + Blocker 충돌 처리
        public void MoveBy(Vector3 delta)
        {
            Vector3 p = planet.SnapToSurface(transform.position + delta, surfaceOffset);
            transform.position = SurfaceCollision.ResolveBlockers(planet, p, bodyRadius, surfaceOffset);
        }

        /// 현재 위치의 '위' 축으로 제자리 회전 (도)
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

        /// 애니메이션·카메라용 속도 값을 능력이 직접 넣을 때
        public void SetMotion(float currentSpeed, float signedSpeed)
        {
            CurrentSpeed = currentSpeed;
            SignedSpeed = signedSpeed;
        }

        // ── 기존 API ──

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
