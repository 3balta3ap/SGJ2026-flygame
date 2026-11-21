using UnityEngine;

namespace FlyJam
{
    /// Player input for SphereWalker (keyboard + gamepad through InputCompat; the class name is kept for prefab links).
    /// x = steering. y = 1 (forward) - ignored by AutoRun, used only if the walker is switched back to Tank.
    public class KeyboardMoveInput : MonoBehaviour, IMoveInput
    {
        public Vector2 ReadMove() => new Vector2(InputCompat.Steer(), 1f);
        public bool ReadDodge() => InputCompat.ActionPressed();
    }
}
