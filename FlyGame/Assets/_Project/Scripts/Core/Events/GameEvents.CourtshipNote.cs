using System;

namespace FlyJam
{
    public static partial class GameEvents
    {
        public static event Action<FaceButton> CourtshipNote;

        public static void RaiseCourtshipNote(FaceButton button) => CourtshipNote?.Invoke(button);
    }
}
