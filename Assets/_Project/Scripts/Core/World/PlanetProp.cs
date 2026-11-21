using System.Collections.Generic;
using UnityEngine;

namespace FlyJam
{
    public enum PropKind { Decor, Blocker, Hazard }

    /// 행성 위 오브젝트. Decor = 통과, Blocker = 막힘(미끄러져 돌아감), Hazard = 닿으면 실패.
    public class PlanetProp : MonoBehaviour, IObstacle
    {
        [SerializeField] PropKind kind = PropKind.Decor;
        [SerializeField] float radius = 0.5f;   // 표면에서의 충돌 반지름

        static readonly List<PlanetProp> all = new List<PlanetProp>();
        public static IReadOnlyList<PlanetProp> All => all;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => all.Clear();

        public PropKind Kind => kind;
        public float Radius => radius;
        public Vector3 Position => transform.position;
        public float HitRadius => radius;
        public bool IsMoving => false;

        public void Configure(PropKind k, float r)
        {
            kind = k;
            radius = r;
            if (!Application.isPlaying || !isActiveAndEnabled) return;
            ObstacleRegistry.Unregister(this);
            if (kind == PropKind.Hazard) ObstacleRegistry.Register(this);
        }

        void OnEnable()
        {
            all.Add(this);
            if (kind == PropKind.Hazard) ObstacleRegistry.Register(this);
        }

        void OnDisable()
        {
            all.Remove(this);
            ObstacleRegistry.Unregister(this);
        }
    }
}
