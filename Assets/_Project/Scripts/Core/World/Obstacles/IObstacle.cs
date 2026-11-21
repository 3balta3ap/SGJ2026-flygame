using UnityEngine;

namespace FlyJam
{
    /// 장애물. IsMoving은 운동맹 모드(움직이는 동안 안 보임)를 위한 정보.
    public interface IObstacle
    {
        Vector3 Position { get; }
        float HitRadius { get; }
        bool IsMoving { get; }
    }
}
