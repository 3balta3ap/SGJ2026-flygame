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
        int movementBudgetFrame = -1;
        float movementTimeConsumed;
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
            UpdateMovement(Time.deltaTime);
        }

        void UpdateMovement(float dt)
        {
            if (scripted || IsOverridden) return;

            if (movementBudgetFrame == Time.frameCount) dt = Mathf.Max(0f, dt - movementTimeConsumed);
            if (dt <= 0f) return;

            Vector3 up = planet.UpAt(transform.position);
            bool canControl = inputEnabled && input != null;
            Vector2 raw = canControl ? Vector2.ClampMagnitude(input.ReadMove(), 1f) : Vector2.zero;

            if (moveMode == MoveMode.AutoRun) UpdateAutoRun(raw, canControl, up, dt);
            else if (moveMode == MoveMode.Tank) UpdateTank(raw, up, dt);
            else UpdateSteer(raw, up, dt);

            if (State == FlyState.Stopped || State == FlyState.Walking)
                SetState(Mathf.Abs(SignedSpeed) > 0.01f ? FlyState.Walking : FlyState.Stopped);
        }

        /// P01: always forward; x steers with a capped turn rate, so turns become wide arcs. Neutral = straight.
        void UpdateAutoRun(Vector2 raw, bool canControl, Vector3 up, float dt)
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
            if (steer != 0f) TurnAround(steer * TurnRate * dt);

            float speed = MoveSpeed * SpeedMultiplier;
            Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, up).normalized;
            MoveBy(fwd * speed * dt);
            AlignToSurface();

            CurrentSpeed = speed;
            SignedSpeed = speed;
        }

        void UpdateTank(Vector2 raw, Vector3 up, float dt)
        {
            float turn = Mathf.Abs(raw.x) > inputDeadZone ? raw.x : 0f;
            float drive = Mathf.Abs(raw.y) > inputDeadZone ? raw.y : 0f;

            if (turn != 0f) TurnAround(turn * tankTurnSpeed * dt);

            float speed = drive * MoveSpeed * (drive < 0f ? backwardSpeedRatio : 1f) * SpeedMultiplier;
            if (speed != 0f)
            {
                Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, up).normalized;
                MoveBy(fwd * speed * dt);
            }
            AlignToSurface();

            CurrentSpeed = Mathf.Abs(speed);
            SignedSpeed = speed != 0f ? speed : Mathf.Abs(turn) * turnAnimSpeed;
        }

        void UpdateSteer(Vector2 raw, Vector3 up, float dt)
        {
            Vector3 move = Vector3.zero;
            if (raw.magnitude > inputDeadZone)
            {
                Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, up).normalized;
                Vector3 right = Vector3.Cross(up, fwd);
                move = fwd * raw.y + right * raw.x;
            }

            CurrentSpeed = move.magnitude * MoveSpeed * SpeedMultiplier;
            if (CurrentSpeed > 0f) MoveBy(move * MoveSpeed * SpeedMultiplier * dt);
            AlignToSurface();
            if (CurrentSpeed > 0f) TurnToward(move, steerTurnSpeed, dt);
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

        /// Accounts for motion already performed by an ability before this frame's normal movement.
        public void ConsumeMovementTime(float seconds)
        {
            if (movementBudgetFrame != Time.frameCount)
            {
                movementBudgetFrame = Time.frameCount;
                movementTimeConsumed = 0f;
            }
            movementTimeConsumed += seconds;
        }

        /// Moves along the planet surface and resolves Blocker collisions.
        public void MoveBy(Vector3 delta) => transform.position = PreviewMove(transform.position, delta);

        /// Predicts the same step without moving the walker.
        public Vector3 PreviewMove(Vector3 position, Vector3 delta)
        {
            Vector3 p = planet.SnapToSurface(position + delta, surfaceOffset);
            return SurfaceCollision.ResolveBlockers(planet, p, bodyRadius, surfaceOffset);
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

        void TurnToward(Vector3 dir, float degPerSec, float dt)
        {
            Vector3 up = planet.UpAt(transform.position);
            Vector3 d = Vector3.ProjectOnPlane(dir, up);
            if (d.sqrMagnitude < 1e-6f) return;
            Quaternion target = Quaternion.LookRotation(d.normalized, up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, degPerSec * dt);
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
