using System.Collections.Generic;
using UnityEngine;

namespace FlyJam
{
    /// Enemy / obstacle that circles the planet along its heading (a great circle), passing through props like trees (C04-C08).
    /// Per prefab: the look (rolling ball, or a creature facing its path with an optional hover bob).
    /// Per type row (HazardBalance.TypeRow from ObstacleSpawner.Launch): damage, warning, chase, stop-on-hit/dodge, rival,
    /// and day/night strength (male: faster and shorter chase cooldown as days pass and at night).
    /// Freezes while the session is paused (courtship, mash battle, pause menu) - E04.
    public class RollingObstacle : MonoBehaviour, IMovingObstacle, IObstacleDodgeMotion, IDamageSource
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

        internal Transform Visual => visual;
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
        public float TravelSpeed => speed * Strength;
        public float StopSecondsAfterDodge => hasRow && row.stopOnDodge > 0f ? row.stopOnDodge : Mathf.Max(0f, stopLeft);
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

            float step = TravelSpeed * Time.deltaTime;
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
