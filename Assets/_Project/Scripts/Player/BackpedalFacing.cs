using UnityEngine;

namespace FlyJam
{
    /// 뒷걸음 중에는 겉모습(Visual)만 180° 돌려 카메라를 보게 한다.
    /// 몸(루트)·이동·카메라는 건드리지 않는다 → 카메라가 따라 돌며 빙글빙글 도는 문제가 생기지 않는다.
    public class BackpedalFacing : MonoBehaviour
    {
        [SerializeField] SphereWalker walker;
        [SerializeField] Transform visualRoot;      // Fly/Visual
        [SerializeField] GameSession session;
        [SerializeField] float turnTime = 0.25f;    // 180° 도는 데 걸리는 시간

        public bool FacingCamera { get; private set; }

        Quaternion baseRotation;
        float yaw;

        void Awake() => baseRotation = visualRoot.localRotation;

        void LateUpdate()
        {
            bool locked = session != null && (session.Courting || session.Ended);

            if (locked || walker.State == FlyState.Celebrating || walker.State == FlyState.Disabled)
            {
                FacingCamera = false;
            }
            else if (walker.State == FlyState.Walking)
            {
                // SignedSpeed < 0 = 뒷걸음. 전진·제자리 회전은 양수
                FacingCamera = walker.SignedSpeed < 0f;
            }
            // Stopped, Dodging: 지금 방향 유지

            float target = FacingCamera ? 180f : 0f;
            yaw = Mathf.MoveTowards(yaw, target, 180f / Mathf.Max(0.01f, turnTime) * Time.deltaTime);
            visualRoot.localRotation = baseRotation * Quaternion.Euler(0f, yaw, 0f);
        }
    }
}
