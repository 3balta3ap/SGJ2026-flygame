using UnityEngine;

namespace FlyJam
{
    /// Rotates only the visual (FlyVisual transform) 180 degrees to face the camera while backing up.
    /// Leaves body root, movement, and camera untouched. Core (FlyVisual) has no knowledge of this component.
    public class BackpedalFacing : MonoBehaviour
    {
        [SerializeField] SphereWalker walker;     // Auto-filled from GameContext.Player if null
        [SerializeField] FlyVisual flyVisual;     // Found under walker if null
        [SerializeField] GameSession session;     // Auto-filled from GameContext if null
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
            // Stopped, Dodging: keep current facing

            float target = FacingCamera ? 180f : 0f;
            yaw = Mathf.MoveTowards(yaw, target, 180f / Mathf.Max(0.01f, turnTime) * Time.deltaTime);
            visualRoot.localRotation = baseRotation * Quaternion.Euler(0f, yaw, 0f);
            flyVisual.ReverseGait = FacingCamera;
        }
    }
}
