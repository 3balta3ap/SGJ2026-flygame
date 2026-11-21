using UnityEngine;

namespace FlyJam
{
    /// Game 씬의 GameSession 오브젝트에 부착. 지금은 모드 없음(null) = 정상 시야.
    public class SenseModeController : MonoBehaviour
    {
        ISenseMode current;

        public void SetMode(ISenseMode mode)
        {
            current?.Exit();
            current = mode;
            current?.Enter();
        }

        void Update() => current?.Tick(Time.deltaTime);
    }
}
