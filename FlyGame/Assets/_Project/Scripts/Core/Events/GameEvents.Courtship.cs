using System;
using UnityEngine;

namespace FlyJam
{
    public static partial class GameEvents
    {
        public static event Action CourtshipStarted;
        public static event Action<bool> CourtshipInput;               // true = correct button, false = wrong
        public static event Action<bool, Vector3> CourtshipFinished;   // success, mate position

        public static void RaiseCourtshipStarted() => CourtshipStarted?.Invoke();
        public static void RaiseCourtshipInput(bool correct) => CourtshipInput?.Invoke(correct);
        public static void RaiseCourtshipFinished(bool ok, Vector3 p) => CourtshipFinished?.Invoke(ok, p);
    }
}
