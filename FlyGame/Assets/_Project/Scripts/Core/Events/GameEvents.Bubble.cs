using System;

namespace FlyJam
{
    public static partial class GameEvents
    {
        /// Speech bubble above the player: text key, priority (higher replaces a lower one on screen; equal waits in line).
        public static event Action<string, int> BubbleRequested;

        public static void RaiseBubbleRequested(string key, int priority) => BubbleRequested?.Invoke(key, priority);
    }
}
