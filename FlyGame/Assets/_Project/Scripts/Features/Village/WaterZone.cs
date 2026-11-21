using System.Collections.Generic;
using UnityEngine;

namespace FlyJam
{
    /// Marks a pond: flies inside 'radius' (measured along the surface from this object) sink a little (PondSink).
    /// Put on each Pond object next to its SurfaceDisc (same radius).
    public class WaterZone : MonoBehaviour
    {
        [SerializeField] float radius = 2.5f;

        static readonly List<WaterZone> all = new List<WaterZone>();
        public static IReadOnlyList<WaterZone> All => all;
        public float Radius => radius;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => all.Clear();

        void OnEnable() { if (!all.Contains(this)) all.Add(this); }
        void OnDisable() => all.Remove(this);

        /// True when the point (on the surface) is inside this pond, shrunk by 'margin'.
        public bool Contains(Vector3 p, float margin) => Vector3.Distance(p, transform.position) <= Mathf.Max(0f, radius - margin);

        public static bool AnyContains(Vector3 p, float margin)
        {
            for (int i = 0; i < all.Count; i++)
                if (all[i] != null && all[i].Contains(p, margin)) return true;
            return false;
        }
    }
}
