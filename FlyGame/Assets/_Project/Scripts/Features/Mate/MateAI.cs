using UnityEngine;

namespace FlyJam
{
    /// Alternates resting and short walks, and flees when the player crosses the approach threshold.
    [DefaultExecutionOrder(-5)] // Set the movement state before SphereWalker reads input.
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

        enum Mode { Idle, Wander, Flee, Frozen }
        Mode mode = Mode.Idle;
        float timer, forcedFlee;
        Vector3 heading;

        public bool IsFleeing => mode == Mode.Flee;
        /// Retains the existing bubble milestone and leash behavior when the Level feature is on.
        public bool IsShy => balance != null && PlayerProgress.Active && PlayerProgress.Level < balance.ShyUntilLevel;
        bool wasShy;

        void Awake()
        {
            if (self == null) self = GetComponent<SphereWalker>();
            var ctx = GameContext.Current;
            if (player == null && ctx != null && ctx.Player != null) player = ctx.Player.transform;
        }

        void Start()
        {
            wasShy = IsShy;
            heading = transform.forward;
            EnterIdle();
        }

        void Update() => UpdateBehavior(Time.deltaTime);

        void UpdateBehavior(float dt)
        {
            if (mode == Mode.Frozen || self == null || self.Planet == null || player == null || SessionPaused()) return;

            bool shy = IsShy;
            if (wasShy && !shy && !string.IsNullOrEmpty(notShyBubbleKey)) GameEvents.RaiseBubbleRequested(notShyBubbleKey, 3);
            wasShy = shy;

            Planet planet = self.Planet;
            float maxDistance = Mathf.PI * planet.Radius;
            int level = PlayerProgress.Level;
            Vector2 distances = balance != null ? balance.FleeDistancesAtLevel(level) : MateBalance.DefaultFleeDistancesAtLevel(level);
            float release = Mathf.Min(distances.y, maxDistance);
            float start = Mathf.Min(distances.x, Mathf.Max(0f, release - 0.5f));
            float distance = ArcDistance(planet, transform.position, player.position);
            const float distanceTolerance = 0.0001f;

            if (mode == Mode.Flee)
            {
                forcedFlee -= dt;
                if (forcedFlee <= 0f && distance >= release - distanceTolerance) EnterIdle();
                return;
            }

            if (distance <= start + distanceTolerance)
            {
                EnterFlee(0f);
                return;
            }

            timer -= dt;
            if (timer > 0f) return;
            if (mode == Mode.Idle) EnterWander();
            else EnterIdle();
        }

        static float ArcDistance(Planet planet, Vector3 from, Vector3 to)
        {
            Vector3 a = planet.UpAt(from);
            Vector3 b = planet.UpAt(to);
            return planet.Radius * Mathf.Atan2(Vector3.Cross(a, b).magnitude, Vector3.Dot(a, b));
        }

        void EnterIdle()
        {
            mode = Mode.Idle;
            forcedFlee = 0f;
            timer = Random.Range(balance != null ? balance.PauseTimeMin : 3f, balance != null ? balance.PauseTimeMax : 7f);
            StopMotion();
        }

        void EnterWander()
        {
            mode = Mode.Wander;
            timer = Random.Range(balance != null ? balance.WalkTimeMin : 0.5f, balance != null ? balance.WalkTimeMax : 1.5f);
            SetSpeed(Random.Range(balance != null ? balance.WalkSpeedMin : 1f, balance != null ? balance.WalkSpeedMax : 2f));

            Vector3 up = self.Planet.UpAt(transform.position);
            Vector3 baseDir = Vector3.ProjectOnPlane(transform.forward, up);
            float jitter = balance != null ? balance.WanderTurn : 70f;
            if (!IsShy && Vector3.Angle(up, self.Planet.UpAt(player.position)) > (balance != null ? balance.LeashAngle : 100f))
            {
                Vector3 towardPlayer = Vector3.ProjectOnPlane(player.position - transform.position, up);
                if (towardPlayer.sqrMagnitude >= 1e-6f)
                {
                    baseDir = towardPlayer;
                    jitter = leashJitter;
                }
            }
            heading = Quaternion.AngleAxis(Random.Range(-jitter, jitter), up) * baseDir.normalized;
        }

        void EnterFlee(float seconds)
        {
            mode = Mode.Flee;
            forcedFlee = seconds;
            SetSpeed(balance != null ? balance.Speed : 4.4f);
        }

        static bool SessionPaused()
        {
            var ctx = GameContext.Current;
            return ctx != null && ctx.Session != null && ctx.Session.IsPaused;
        }

        void SetSpeed(float speed)
        {
            if (self != null) self.SpeedMultiplier = speed / Mathf.Max(0.01f, self.MoveSpeed);
        }

        void StopMotion()
        {
            SetSpeed(0f);
            if (self != null) self.SetMotion(0f, 0f);
        }

        public Vector2 ReadMove()
        {
            if ((mode != Mode.Wander && mode != Mode.Flee) || self == null || self.Planet == null || player == null || SessionPaused())
                return Vector2.zero;

            Vector3 up = self.Planet.UpAt(transform.position);
            Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, up).normalized;
            Vector3 desired = mode == Mode.Flee ? transform.position - player.position : heading;
            desired = Vector3.ProjectOnPlane(desired, up);
            if (desired.sqrMagnitude < 1e-6f)
            {
                // At the same or opposite point there is no unique tangent toward or away from the player.
                desired = Vector3.ProjectOnPlane(heading, up);
                if (desired.sqrMagnitude < 1e-6f) desired = fwd;
            }
            heading = desired.normalized;
            Vector3 right = Vector3.Cross(up, fwd);
            return new Vector2(Vector3.Dot(heading, right), Vector3.Dot(heading, fwd));
        }

        public bool ReadDodge() => false;

        public void Freeze(bool on)
        {
            if (on)
            {
                mode = Mode.Frozen;
                StopMotion();
            }
            else EnterIdle();
        }

        /// Run away for failFleeSeconds even if the player is not close (failed courtship).
        public void Flee() => EnterFlee(balance != null ? balance.FailFleeSeconds : 2f);
    }
}
