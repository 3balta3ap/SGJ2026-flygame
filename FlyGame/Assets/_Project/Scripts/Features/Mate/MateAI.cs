using UnityEngine;

namespace FlyJam
{
    /// Mate movement input: wanders the village alternating between walking and pausing.
    /// If farther than leashAngle from the player, biases the next walk heading back toward the player.
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
        [SerializeField] Vector2 walkTime = new Vector2(1f, 2f);
        [SerializeField] Vector2 pauseTime = new Vector2(2f, 3.5f);
        [SerializeField] float wanderTurn = 70f;
        [SerializeField] float leashAngle = 100f;
        [SerializeField] float leashJitter = 30f;
        [SerializeField] float fleeTime = 2f;
        [SerializeField] float fleeSpeedMultiplier = 2f;

        enum Mode { Pause, Walk, Flee, Frozen }
        Mode mode = Mode.Pause;
        float timer;
        Vector3 heading;

        void Awake()
        {
            if (self == null) self = GetComponent<SphereWalker>();
            if (player == null && GameContext.Current != null && GameContext.Current.Player != null)
                player = GameContext.Current.Player.transform;
        }

        void Start()
        {
            heading = transform.forward;
            timer = Random.Range(pauseTime.x, pauseTime.y);
        }

        void Update()
        {
            if (mode == Mode.Frozen) return;
            timer -= Time.deltaTime;
            if (timer > 0f) return;

            if (mode == Mode.Pause)
            {
                mode = Mode.Walk;
                timer = Random.Range(walkTime.x, walkTime.y);

                Vector3 up = self.Planet.UpAt(transform.position);
                Vector3 playerUp = self.Planet.UpAt(player.position);
                Vector3 baseDir;
                float jitter;
                if (Vector3.Angle(up, playerUp) > leashAngle)
                {
                    baseDir = Vector3.ProjectOnPlane(player.position - transform.position, up);
                    jitter = leashJitter;
                }
                else
                {
                    baseDir = Vector3.ProjectOnPlane(transform.forward, up);
                    jitter = wanderTurn;
                }
                heading = Quaternion.AngleAxis(Random.Range(-jitter, jitter), up) * baseDir;
            }
            else
            {
                mode = Mode.Pause;
                timer = Random.Range(pauseTime.x, pauseTime.y);
                self.SpeedMultiplier = 1f;
            }
        }

        public Vector2 ReadMove()
        {
            if (mode == Mode.Pause || mode == Mode.Frozen) return Vector2.zero;
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
            timer = Random.Range(pauseTime.x, pauseTime.y);
            self.SpeedMultiplier = 1f;
        }

        public void Flee()
        {
            mode = Mode.Flee;
            timer = fleeTime;
            self.SpeedMultiplier = fleeSpeedMultiplier;
        }
    }
}
