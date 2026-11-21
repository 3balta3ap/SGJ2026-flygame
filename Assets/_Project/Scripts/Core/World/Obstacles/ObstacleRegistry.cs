using System.Collections.Generic;
using UnityEngine;

namespace FlyJam
{
    /// 장애물은 OnEnable에서 Register, OnDisable에서 Unregister 한다.
    public static class ObstacleRegistry
    {
        static readonly List<IObstacle> obstacles = new List<IObstacle>();
        public static IReadOnlyList<IObstacle> All => obstacles;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => obstacles.Clear();

        public static void Register(IObstacle o) { if (!obstacles.Contains(o)) obstacles.Add(o); }
        public static void Unregister(IObstacle o) => obstacles.Remove(o);
    }
}
