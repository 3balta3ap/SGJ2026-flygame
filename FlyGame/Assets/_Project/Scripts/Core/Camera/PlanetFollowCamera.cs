using UnityEngine;

namespace FlyJam
{
    [RequireComponent(typeof(Camera))]
    public class PlanetFollowCamera : MonoBehaviour
    {
        [SerializeField] SphereWalker target;
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

        void Awake() => cam = GetComponent<Camera>();

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

            bool lookOut = !SuppressLookOut && target.State == FlyState.Stopped && target.TimeInState > lookOutDelay;
            zoom = Mathf.Lerp(zoom, lookOut ? 1f : 0f, 1f - Mathf.Exp(-zoomSharpness * Time.deltaTime));
            float dist = Mathf.Lerp(distance, lookOutDistance, zoom);
            float hgt = Mathf.Lerp(height, lookOutHeight, zoom);

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
