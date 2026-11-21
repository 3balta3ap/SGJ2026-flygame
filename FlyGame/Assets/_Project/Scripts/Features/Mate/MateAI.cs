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
