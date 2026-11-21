using System;
using UnityEngine;

namespace FlyJam
{
    public static partial class GameEvents
    {
        public static event Action<bool, Vector3> CourtshipFinished; // success, mate position
        public static void RaiseCourtshipFinished(bool ok, Vector3 p) => CourtshipFinished?.Invoke(ok, p);
    }
}
