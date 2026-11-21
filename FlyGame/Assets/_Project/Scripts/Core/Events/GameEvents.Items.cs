using System;
using UnityEngine;

namespace FlyJam
{
    public static partial class GameEvents
    {
        public static event Action<Vector3> AppleEaten;   // where it was

        public static void RaiseAppleEaten(Vector3 p) => AppleEaten?.Invoke(p);
    }
}
