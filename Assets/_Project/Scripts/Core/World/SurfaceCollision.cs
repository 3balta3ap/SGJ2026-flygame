using UnityEngine;

namespace FlyJam
{
    /// Pushes out of Blocker objects without physics. Pushes along the tangent plane for smooth sliding.
    public static class SurfaceCollision
    {
        public static Vector3 ResolveBlockers(Planet planet, Vector3 pos, float selfRadius, float offset)
        {
            var props = PlanetProp.All;
            for (int i = 0; i < props.Count; i++)
            {
                var p = props[i];
                if (p.Kind != PropKind.Blocker) continue;

                float min = p.Radius + selfRadius;
                Vector3 delta = pos - p.transform.position;
                if (delta.sqrMagnitude > (min + 1f) * (min + 1f)) continue;   // Ignore distant objects (including those on the opposite side)

                Vector3 up = planet.UpAt(pos);
                Vector3 flat = Vector3.ProjectOnPlane(delta, up);
                float d = flat.magnitude;
                if (d >= min) continue;

                Vector3 n = d > 1e-4f ? flat / d : Vector3.ProjectOnPlane(Vector3.right, up).normalized;
                pos = planet.SnapToSurface(pos + n * (min - d), offset);
            }
            return pos;
        }
    }
}
