using UnityEngine;

namespace FlyJam
{
    public class KeyboardMoveInput : MonoBehaviour, IMoveInput
    {
        public Vector2 ReadMove() => InputCompat.Move();
        public bool ReadDodge() => InputCompat.DodgePressed();
    }
}
