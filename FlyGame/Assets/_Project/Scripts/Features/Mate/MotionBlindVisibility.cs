using UnityEngine;

namespace FlyJam
{
    /// Motion blindness: mate is invisible while walking and pops into view when stopped.
    /// Disable motionBlind to keep the mate always visible (for A/B comparison demos).
    public class MotionBlindVisibility : MonoBehaviour
    {
        [SerializeField] SphereWalker walker;
        [SerializeField] Transform visualRoot;
        [SerializeField] bool motionBlind = true;
        [SerializeField] float popScale = 1.35f;
        [SerializeField] float popTime = 0.18f;

        Renderer[] renderers;
        bool visible = true;
        float popT = 1f;
        Vector3 baseScale;

        public bool ForceVisible { get; set; }

        void Awake()
        {
            renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            baseScale = visualRoot.localScale;
        }

        void LateUpdate()
        {
            bool want = !motionBlind || ForceVisible || walker.State != FlyState.Walking;
            if (want != visible)
            {
                visible = want;
                foreach (var r in renderers) r.enabled = want;
                if (want)
                {
                    popT = 0f;
                    GameEvents.RaiseMateAppeared(transform.position);
                }
            }
            if (popT < 1f)
            {
                popT += Time.deltaTime / popTime;
                visualRoot.localScale = baseScale * Mathf.Lerp(popScale, 1f, Mathf.Clamp01(popT));
            }
        }
    }
}
