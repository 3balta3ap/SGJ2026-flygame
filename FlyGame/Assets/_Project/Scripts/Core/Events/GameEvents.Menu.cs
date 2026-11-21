using System;

namespace FlyJam
{
    public static partial class GameEvents
    {
        public static event Action MenuMoved;            // cursor moved (menu SE)
        public static event Action MenuSelected;         // item chosen (decide SE)
        public static event Action<bool> GamePaused;     // true = pause menu opened

        public static void RaiseMenuMoved() => MenuMoved?.Invoke();
        public static void RaiseMenuSelected() => MenuSelected?.Invoke();
        public static void RaiseGamePaused(bool paused) => GamePaused?.Invoke(paused);
    }
}
