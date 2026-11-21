using UnityEngine;

namespace FlyJam
{
    public class Planet : MonoBehaviour
    {
        [SerializeField] float radius = 12f;

        public float Radius => radius;
        public Vector3 Center => transform.position;

        /// p 지점에서의 '위' 방향 (행성 중심 반대쪽)
        public Vector3 UpAt(Vector3 p)
        {
            Vector3 d = p - Center;
            return d.sqrMagnitude > 1e-8f ? d.normalized : Vector3.up;
        }

        public Vector3 SnapToSurface(Vector3 p, float offset = 0f) => Center + UpAt(p) * (radius + offset);

        public Vector3 SurfacePoint(Vector3 dir, float offset = 0f) => Center + dir.normalized * (radius + offset);

        // Unity 기본 Sphere의 반지름은 0.5 → 스케일 = 반지름 × 2
        void OnValidate() => transform.localScale = Vector3.one * radius * 2f;
    }
}
