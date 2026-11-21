using System.Collections.Generic;
using UnityEngine;

namespace FlyJam
{
    /// The only code that writes Time.timeScale.
    /// Pause menu -> 0. Slow motion -> the lowest requested scale. Otherwise 1.
    public static class TimeControl
    {
        static readonly Dictionary<object, float> slows = new Dictionary<object, float>();
        static bool paused;
        static int resumedFrame = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            slows.Clear();
            paused = false;
            resumedFrame = -1;
        }

        public static bool Paused => paused;

        /// True while paused and on the frame the pause was closed, so the button that closed it is not reused by gameplay.
        public static bool InputBlocked => paused || Time.frameCount == resumedFrame;

        public static void SetPaused(bool on)
        {
            if (paused && !on) resumedFrame = Time.frameCount;
            paused = on;
            Apply();
        }

        /// Ask for slow motion (scale below 1). Pass 1 to release your request.
        public static void SetSlow(object by, float scale)
        {
            if (by == null) return;
            if (scale >= 1f) slows.Remove(by);
            else slows[by] = Mathf.Max(0.05f, scale);
            Apply();
        }

        /// Normal speed, nothing paused. Called on every scene change.
        public static void ResetAll()
        {
            slows.Clear();
            paused = false;
            Apply();
        }

        static void Apply()
        {
            float s = 1f;
            foreach (var v in slows.Values) s = Mathf.Min(s, v);
            if (paused) s = 0f;
            Time.timeScale = s;
            Time.fixedDeltaTime = 0.02f * Mathf.Max(s, 0.05f);
        }
    }
}
