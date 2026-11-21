using UnityEngine;

namespace FlyJam
{
    public class Planet : MonoBehaviour
    {
        [SerializeField] float radius = 12f;

        public float Radius => radius;
        public Vector3 Center => transform.position;

        /// Surface "up" direction at point p (pointing away from the planet center).
        public Vector3 UpAt(Vector3 p)
        {
            Vector3 d = p - Center;
            return d.sqrMagnitude > 1e-8f ? d.normalized : Vector3.up;
        }

        public Vector3 SnapToSurface(Vector3 p, float offset = 0f) => Center + UpAt(p) * (radius + offset);

        public Vector3 SurfacePoint(Vector3 dir, float offset = 0f) => Center + dir.normalized * (radius + offset);

        // Unity's default Sphere has radius 0.5 -> scale = radius * 2
        void OnValidate() => transform.localScale = Vector3.one * radius * 2f;
    }
}
