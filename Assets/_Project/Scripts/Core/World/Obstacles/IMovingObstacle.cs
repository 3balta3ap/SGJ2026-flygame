using UnityEngine;

namespace FlyJam
{
    /// Moving obstacle contract used by DodgeAbility to sidestep away from threats without knowing concrete obstacle types.
    public interface IMovingObstacle : IObstacle
    {
        Vector3 Direction { get; }
    }
}
