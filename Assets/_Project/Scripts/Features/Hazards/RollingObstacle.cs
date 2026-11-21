using System.Collections.Generic;
using UnityEngine;

namespace FlyJam
{
    /// 행성 표면의 큰 원을 따라 굴러가는 장애물. 나무 등은 통과한다 (굴러 넘어가는 것으로 취급).
    public class RollingObstacle : MonoBehaviour, IMovingObstacle
    {
        static readonly List<RollingObstacle> active = new List<RollingObstacle>();
        public static IReadOnlyList<RollingObstacle> Active => active;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => active.Clear();

        [SerializeField] Transform visual;
        [SerializeField] float radius = 0.4f;

        Planet planet;
        Vector3 dir;
        float speed, travelled, maxTravel;

        public Vector3 Position => transform.position;
        public float HitRadius => radius;
        public bool IsMoving => true;
        public Vector3 Direction => dir;

        public void Launch(Planet p, Vector3 surfacePos, Vector3 direction, float spd, float maxDistance)
        {
            planet = p;
            speed = spd;
            maxTravel = maxDistance;
            transform.position = p.SnapToSurface(surfacePos, radius);
            Vector3 up = p.UpAt(transform.position);
            dir = Vector3.ProjectOnPlane(direction, up).normalized;
        }

        void OnEnable()
        {
            active.Add(this);
            ObstacleRegistry.Register(this);
        }

        void OnDisable()
        {
            active.Remove(this);
            ObstacleRegistry.Unregister(this);
        }

        void Update()
        {
            if (planet == null) return;
            float step = speed * Time.deltaTime;
            transform.position = planet.SnapToSurface(transform.position + dir * step, radius);
            Vector3 up = planet.UpAt(transform.position);
            dir = Vector3.ProjectOnPlane(dir, up).normalized;
            transform.rotation = Quaternion.LookRotation(dir, up);

            if (visual != null)
                visual.Rotate(Vector3.Cross(up, dir), step / radius * Mathf.Rad2Deg, Space.World);

            travelled += step;
            if (travelled >= maxTravel) Destroy(gameObject);
        }
    }
}
