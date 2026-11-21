using System;
using UnityEngine;

namespace FlyJam
{
    public static partial class GameEvents
    {
        public static event Action<bool, Vector3> CourtshipFinished; // 성공 여부, 짝 위치
        public static void RaiseCourtshipFinished(bool ok, Vector3 p) => CourtshipFinished?.Invoke(ok, p);
    }
}
