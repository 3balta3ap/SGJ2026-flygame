using System;
using UnityEngine;

namespace FlyJam
{
    public static partial class GameEvents
    {
        public static event Action<Vector3, Vector3> PlayerDodged;   // position, dodge direction
        public static void RaisePlayerDodged(Vector3 pos, Vector3 dir) => PlayerDodged?.Invoke(pos, dir);
    }
}
