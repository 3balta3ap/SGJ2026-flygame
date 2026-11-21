using UnityEngine;

namespace FlyJam
{
    /// Abstracts movement input sources. Keyboard, Mate AI, and future gyro/gamepad inputs all implement this interface.
    public interface IMoveInput
    {
        Vector2 ReadMove();   // x = left/right, y = forward/back, magnitude 0..1
        bool ReadDodge();     // Whether the dodge button was pressed this frame
    }
}
