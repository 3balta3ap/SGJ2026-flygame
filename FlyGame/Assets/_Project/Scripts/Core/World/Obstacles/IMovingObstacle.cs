using UnityEngine;

namespace FlyJam
{
    /// Surface heading of a moving obstacle.
    public interface IMovingObstacle : IObstacle
    {
        Vector3 Direction { get; }
    }

    /// Optional motion snapshot for predicting a warning target after the player starts a dodge.
    public interface IObstacleDodgeMotion
    {
        float TravelSpeed { get; }
        float StopSecondsAfterDodge { get; }
    }
}
