using System;
using UnityEngine;

namespace FlyJam
{
    public static partial class GameEvents
    {
        public static event Action<Vector3> MateAppeared;            // 운동맹 '팟'
        public static event Action MateTouched;

        public static void RaiseMateAppeared(Vector3 p) => MateAppeared?.Invoke(p);
        public static void RaiseMateTouched() => MateTouched?.Invoke();
    }
}
