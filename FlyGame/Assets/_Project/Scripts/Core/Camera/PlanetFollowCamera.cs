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

        [Header("Rival Battle")]
        [SerializeField, Range(0f, 90f)] float fightAngle = 40f;
        [SerializeField, Min(0.01f)] float fightTransitionSeconds = 0.3f;
        [SerializeField, Min(0.1f)] float fightDistance = 4.5f;

        public bool SuppressLookOut { get; set; }

        Camera cam;
        Vector3 heading, normalPosition, fightPosition, fightLook;
        Transform rival;
        float fightBlend, fightSide;
        bool initialized;
        float zoom;
        float neutralTime;
        bool warned;

        void Awake() => cam = GetComponent<Camera>();

        void OnEnable() => GameEvents.WarningChanged += OnWarning;
        void OnDisable() => GameEvents.WarningChanged -= OnWarning;
        void OnWarning(IMovingObstacle t) => warned = t != null;

        public void BeginRivalFight(Transform opponent)
        {
            if (target == null || target.Planet == null || opponent == null) return;
            rival = opponent;
            Vector3 up = target.Planet.UpAt(target.transform.position);
            Vector3 forward = Vector3.ProjectOnPlane(opponent.position - target.transform.position, up).normalized;
            Vector3 cameraOffset = transform.position - target.transform.position;
            fightSide = Vector3.Dot(cameraOffset, Vector3.Cross(up, forward)) > 0f ? -1f : 1f;
        }

        public void EndRivalFight() => rival = null;

        void LateUpdate() => UpdateView(Time.deltaTime);

        void UpdateView(float dt)
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
                heading = Vector3.Slerp(heading, tf, 1f - Mathf.Exp(-headingFollow * dt)).normalized;
            }

            // Auto-run never "stops": a neutral stick for idleSeconds counts as idle instead
            bool running = target.Mode == MoveMode.AutoRun && target.State == FlyState.Walking && target.InputEnabled;
            if (running && Mathf.Abs(InputCompat.Steer()) < 0.2f) neutralTime += dt;
            else neutralTime = 0f;
            float idleSeconds = balance != null ? balance.IdleSeconds : 1f;
            bool idle = running && neutralTime >= idleSeconds;
            bool stopped = target.State == FlyState.Stopped && target.TimeInState > lookOutDelay;
            bool lookOut = !SuppressLookOut && !warned && (stopped || idle);

            float sharp = balance != null ? (lookOut ? balance.ZoomOutSharpness : balance.ZoomInSharpness) : zoomSharpness;
            zoom = Mathf.Lerp(zoom, lookOut ? 1f : 0f, 1f - Mathf.Exp(-sharp * dt));
            float outDist = balance != null ? balance.LookOutDistance : lookOutDistance;
            float outHeight = balance != null ? balance.LookOutHeight : lookOutHeight;
            float dist = Mathf.Lerp(distance, outDist, zoom);
            float hgt = Mathf.Lerp(height, outHeight, zoom);

            Vector3 desired = tp - heading * dist + up * hgt;
            normalPosition = initialized
                ? Vector3.Lerp(normalPosition, desired, 1f - Mathf.Exp(-positionFollow * dt))
                : desired;

            Vector3 look = tp + heading * lookAhead + up * lookHeight;
            Vector3 position = normalPosition;
            Quaternion rotation = Quaternion.LookRotation(look - normalPosition, up);
            bool fighting = rival != null && rival.gameObject.activeInHierarchy;
            if (fighting) UpdateFightFraming(planet, tp, hgt);
            fightBlend = Mathf.MoveTowards(fightBlend, fighting ? 1f : 0f, dt / Mathf.Max(0.01f, fightTransitionSeconds));
            if (fightBlend > 0f)
            {
                float weight = Mathf.SmoothStep(0f, 1f, fightBlend);
                // Orbit about the fight instead of moving along a chord across the planet.
                position = fightLook + Vector3.Slerp(normalPosition - fightLook, fightPosition - fightLook, weight);
                float clearance = planet.Radius + Mathf.Max(0.25f, cam.nearClipPlane);
                if ((position - planet.Center).sqrMagnitude < clearance * clearance)
                    position = planet.Center + planet.UpAt(position) * clearance;
                Quaternion fightRotation = Quaternion.LookRotation(fightLook - fightPosition, planet.UpAt(fightLook));
                rotation = Quaternion.Slerp(rotation, fightRotation, weight);
            }
            transform.SetPositionAndRotation(position, rotation);
            initialized = true;
        }

        void UpdateFightFraming(Planet planet, Vector3 playerPosition, float cameraHeight)
        {
            Vector3 middle = planet.SnapToSurface((playerPosition + rival.position) * 0.5f);
            Vector3 up = planet.UpAt(middle);
            Vector3 forward = Vector3.ProjectOnPlane(rival.position - playerPosition, up).normalized;
            if (forward == Vector3.zero) forward = Vector3.ProjectOnPlane(heading, up).normalized;
            Vector3 behind = Quaternion.AngleAxis(fightAngle * fightSide, up) * -forward;
            float halfFov = fieldOfView * 0.5f * Mathf.Deg2Rad;
            float frameDistance = (Vector3.Distance(playerPosition, rival.position) * 0.5f + 0.5f)
                / (Mathf.Tan(halfFov) * Mathf.Min(1f, cam.aspect));
            fightLook = middle + up * lookHeight;
            fightPosition = middle + behind * Mathf.Max(fightDistance, frameDistance) + up * Mathf.Max(0.25f, cameraHeight);
        }
    }
}
