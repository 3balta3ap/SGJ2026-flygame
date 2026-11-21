using UnityEngine;

namespace FlyJam
{
    /// 움직이는 장애물. 회피 능력이 '위협 반대쪽'을 찾을 때 쓴다 (장애물 구현을 몰라도 됨).
    public interface IMovingObstacle : IObstacle
    {
        Vector3 Direction { get; }
    }
}
