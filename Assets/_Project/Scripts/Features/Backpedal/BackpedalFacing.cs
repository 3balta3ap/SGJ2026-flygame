using UnityEngine;

namespace FlyJam
{
    /// 뒷걸음 중에는 겉모습(FlyVisual이 붙은 Visual)만 180° 돌려 카메라를 보게 한다.
    /// 몸·이동·카메라는 건드리지 않는다. 코어(FlyVisual)는 이 컴포넌트를 모른다.
    public class BackpedalFacing : MonoBehaviour
    {
        [SerializeField] SphereWalker walker;     // 비우면 GameContext.Player
        [SerializeField] FlyVisual flyVisual;     // 비우면 walker 아래에서 찾음
        [SerializeField] GameSession session;     // 비우면 GameContext에서
        [SerializeField] float turnTime = 0.25f;

        public bool FacingCamera { get; private set; }

        Transform visualRoot;
        Quaternion baseRotation;
        float yaw;
        bool ready;

        void Start()
        {
            var ctx = GameContext.Current;
            if (ctx != null)
            {
                if (walker == null) walker = ctx.Player;
                if (session == null) session = ctx.Session;
            }
            if (flyVisual == null && walker != null) flyVisual = walker.GetComponentInChildren<FlyVisual>();
            if (flyVisual == null) return;
            visualRoot = flyVisual.transform;
            baseRotation = visualRoot.localRotation;
            ready = true;
        }

        void OnDisable()
        {
            if (!ready) return;
            FacingCamera = false;
            yaw = 0f;
            if (flyVisual != null) flyVisual.ReverseGait = false;
            if (visualRoot != null) visualRoot.localRotation = baseRotation;
        }

        void LateUpdate()
        {
            if (!ready || walker == null) return;
            bool locked = session != null && (session.IsPaused || session.Ended);

            if (locked || walker.State == FlyState.Celebrating || walker.State == FlyState.Disabled)
                FacingCamera = false;
            else if (walker.State == FlyState.Walking)
                FacingCamera = walker.SignedSpeed < 0f;
            // Stopped, Dodging: 지금 방향 유지

            float target = FacingCamera ? 180f : 0f;
            yaw = Mathf.MoveTowards(yaw, target, 180f / Mathf.Max(0.01f, turnTime) * Time.deltaTime);
            visualRoot.localRotation = baseRotation * Quaternion.Euler(0f, yaw, 0f);
            flyVisual.ReverseGait = FacingCamera;
        }
    }
}
