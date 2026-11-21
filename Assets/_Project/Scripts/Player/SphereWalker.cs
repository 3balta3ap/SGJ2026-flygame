using System;
using System.Collections;
using UnityEngine;

namespace FlyJam
{
    public enum FlyState { Stopped, Walking, Dodging, Celebrating, Disabled }

    /// Tank  = 플레이어: 입력 y = 전진(+)/뒷걸음(-), x = 제자리 회전
    /// Steer = 짝(AI):   입력 (x, y) = 자기 몸 기준으로 가고 싶은 방향, 몸이 그쪽으로 돈다
    public enum MoveMode { Tank, Steer }

    /// 같은 오브젝트에 IMoveInput 구현이 정확히 하나 있어야 한다 (플레이어 = KeyboardMoveInput, 짝 = MateAI).
    public class SphereWalker : MonoBehaviour
    {
        [SerializeField] Planet planet;
        [SerializeField] MoveMode moveMode = MoveMode.Tank;
        [SerializeField] bool isPlayer = true;
        [SerializeField] float moveSpeed = 4f;
        [SerializeField] float backwardSpeedRatio = 0.5f;   // 뒷걸음 = 앞 속도 × 이 값
        [SerializeField] float tankTurnSpeed = 150f;        // 도/초
        [SerializeField] float turnAnimSpeed = 1.5f;        // 제자리 회전 때 다리 움직임 빠르기
        [SerializeField] float steerTurnSpeed = 540f;
        [SerializeField] float inputDeadZone = 0.1f;
        [SerializeField] float surfaceOffset = 0f;
        [SerializeField] float bodyRadius = 0.3f;

        [Header("Dodge (급선회 회피)")]
        [SerializeField] float dodgeDistance = 2.2f;
        [SerializeField] float dodgeDuration = 0.22f;
        [SerializeField] float dodgeCooldown = 0.7f;
        [SerializeField] float dodgeYawKick = 25f;          // 옆걸음 회피 때 그쪽으로 몸을 트는 각도
        [SerializeField] float threatSearchRadius = 6f;

        public Planet Planet => planet;
        public bool IsPlayer => isPlayer;
        public FlyState State { get; private set; } = FlyState.Stopped;
        public float TimeInState { get; private set; }
        public float CurrentSpeed { get; private set; }     // 이동 속력 (항상 0 이상)
        public float SignedSpeed { get; private set; }      // 애니메이션용: 뒷걸음이면 음수, 제자리 회전이면 작은 양수
        public bool IsDodging => dodgeTimer > 0f;
        public float SpeedMultiplier { get; set; } = 1f;
        public event Action<FlyState> StateChanged;

        IMoveInput input;
        bool inputEnabled = true;
        bool scripted;
        float dodgeTimer, dodgeCd, dodgeYawSign;
        Vector3 dodgeDir;

        void Awake()
        {
            input = GetComponent<IMoveInput>();
            if (input == null) Debug.LogWarning("[SphereWalker] IMoveInput 컴포넌트가 없습니다.", this);
        }

        void Start()
        {
            transform.position = planet.SnapToSurface(transform.position, surfaceOffset);
            AlignUp();
        }

        void Update()
        {
            TimeInState += Time.deltaTime;
            if (dodgeCd > 0f) dodgeCd -= Time.deltaTime;
            if (scripted) return;

            Vector3 up = planet.UpAt(transform.position);
            bool canControl = inputEnabled && input != null;
            Vector2 raw = canControl ? Vector2.ClampMagnitude(input.ReadMove(), 1f) : Vector2.zero;

            if (canControl && !IsDodging && dodgeCd <= 0f && input.ReadDodge())
                StartDodge(raw, up);

            if (IsDodging) { UpdateDodge(); return; }

            if (moveMode == MoveMode.Tank) UpdateTank(raw, up);
            else UpdateSteer(raw, up);

            if (State == FlyState.Stopped || State == FlyState.Walking)
                SetState(Mathf.Abs(SignedSpeed) > 0.01f ? FlyState.Walking : FlyState.Stopped);
        }

        void UpdateTank(Vector2 raw, Vector3 up)
        {
            float turn = Mathf.Abs(raw.x) > inputDeadZone ? raw.x : 0f;
            float drive = Mathf.Abs(raw.y) > inputDeadZone ? raw.y : 0f;

            if (turn != 0f)
                transform.rotation = Quaternion.AngleAxis(turn * tankTurnSpeed * Time.deltaTime, up) * transform.rotation;

            float speed = drive * moveSpeed * (drive < 0f ? backwardSpeedRatio : 1f) * SpeedMultiplier;
            if (speed != 0f)
            {
                Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, up).normalized;
                MoveBy(fwd * speed * Time.deltaTime);
            }
            AlignUp();

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
            AlignUp();
            if (CurrentSpeed > 0f) TurnToward(move, steerTurnSpeed);
            SignedSpeed = CurrentSpeed;
        }

        void MoveBy(Vector3 delta)
        {
            Vector3 p = planet.SnapToSurface(transform.position + delta, surfaceOffset);
            transform.position = SurfaceCollision.ResolveBlockers(planet, p, bodyRadius, surfaceOffset);
        }

        void TurnToward(Vector3 dir, float degPerSec)
        {
            Vector3 up = planet.UpAt(transform.position);
            Vector3 d = Vector3.ProjectOnPlane(dir, up);
            if (d.sqrMagnitude < 1e-6f) return;
            Quaternion target = Quaternion.LookRotation(d.normalized, up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, degPerSec * Time.deltaTime);
        }

        /// Space+A/D = 옆걸음, Space+S = 뒤로, Space+W = 앞으로, Space 단독 = 위협 반대쪽 자동
        void StartDodge(Vector2 raw, Vector3 up)
        {
            Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, up).normalized;
            Vector3 right = Vector3.Cross(up, fwd);

            if (Mathf.Abs(raw.x) > inputDeadZone)
            {
                dodgeDir = right * Mathf.Sign(raw.x);
                dodgeYawSign = Mathf.Sign(raw.x);
            }
            else if (raw.y < -inputDeadZone) { dodgeDir = -fwd; dodgeYawSign = 0f; }
            else if (raw.y > inputDeadZone) { dodgeDir = fwd; dodgeYawSign = 0f; }
            else
            {
                dodgeDir = SidestepFromThreat(up);
                dodgeYawSign = Mathf.Sign(Vector3.Dot(dodgeDir, right));
            }

            dodgeTimer = dodgeDuration;
            dodgeCd = dodgeCooldown;
            SetState(FlyState.Dodging);
            if (isPlayer) GameEvents.RaisePlayerDodged(transform.position, dodgeDir);
        }

        void UpdateDodge()
        {
            float spd = dodgeDistance / dodgeDuration;
            MoveBy(dodgeDir * spd * Time.deltaTime);
            AlignUp();
            Vector3 up = planet.UpAt(transform.position);
            dodgeDir = Vector3.ProjectOnPlane(dodgeDir, up).normalized;
            if (dodgeYawSign != 0f)
                transform.rotation = Quaternion.AngleAxis(dodgeYawSign * dodgeYawKick * Time.deltaTime / dodgeDuration, up)
                                   * transform.rotation;

            CurrentSpeed = spd;
            SignedSpeed = spd;
            dodgeTimer -= Time.deltaTime;
            if (dodgeTimer <= 0f)
            {
                dodgeTimer = 0f;
                SetState(FlyState.Stopped);
            }
        }

        Vector3 SidestepFromThreat(Vector3 up)
        {
            RollingObstacle nearest = null;
            float best = threatSearchRadius;
            var all = RollingObstacle.Active;
            for (int i = 0; i < all.Count; i++)
            {
                float d = Vector3.Distance(all[i].Position, transform.position);
                if (d < best) { best = d; nearest = all[i]; }
            }
            if (nearest != null)
            {
                Vector3 side = Vector3.Cross(up, nearest.Direction);
                if (Vector3.Dot(side, transform.position - nearest.Position) < 0f) side = -side;
                side = Vector3.ProjectOnPlane(side, up);
                if (side.sqrMagnitude > 1e-6f) return side.normalized;
            }
            return Vector3.ProjectOnPlane(transform.right, up).normalized;
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

        void AlignUp()
        {
            Vector3 up = planet.UpAt(transform.position);
            transform.rotation = Quaternion.FromToRotation(transform.up, up) * transform.rotation;
        }
    }
}
