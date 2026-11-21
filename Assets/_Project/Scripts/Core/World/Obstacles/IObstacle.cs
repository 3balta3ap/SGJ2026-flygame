using UnityEngine;

namespace FlyJam
{
    /// Obstacle contract. IsMoving supports motion-blindness mechanics (invisible while moving).
    public interface IObstacle
    {
        Vector3 Position { get; }
        float HitRadius { get; }
        bool IsMoving { get; }
    }
}
