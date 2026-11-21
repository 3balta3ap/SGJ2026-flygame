using System.Collections.Generic;
using UnityEngine;

namespace FlyJam
{
    /// Prevents village props from spawning within radius around this object (e.g., player/mate spawn points).
    /// Marker component so VillageGenerator does not need direct references to the player or mate.
    [ExecuteAlways]
    [DefaultExecutionOrder(-500)]
    public class KeepClearZone : MonoBehaviour
    {
        [SerializeField] float radius = 3f;

        static readonly List<KeepClearZone> all = new List<KeepClearZone>();
        public static IReadOnlyList<KeepClearZone> All => all;
        public float Radius => radius;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => all.Clear();

        void OnEnable()
        {
            if (!all.Contains(this)) all.Add(this);
        }

        void OnDisable() => all.Remove(this);
    }
}
