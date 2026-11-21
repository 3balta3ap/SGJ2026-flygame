using UnityEngine;

namespace FlyJam
{
    /// Planner numbers for the follow camera. Sheet tab "Camera".
    public class CameraBalance : BalanceTable
    {
        public override string SheetName => "Camera";

        [Tooltip("Seconds of neutral stick (going straight) before the camera pulls back for a wide view")]
        [SerializeField] float idleSeconds = 1f;
        [Tooltip("How fast it pulls back (bigger = faster). 2.5 = about 1.2 s")]
        [SerializeField] float zoomOutSharpness = 2.5f;
        [Tooltip("How fast it comes back when you steer (bigger = faster). 8 = about 0.4 s")]
        [SerializeField] float zoomInSharpness = 8f;
        [Tooltip("Wide view: distance behind the player")]
        [SerializeField] float lookOutDistance = 12f;
        [Tooltip("Wide view: height above the player")]
        [SerializeField] float lookOutHeight = 8.25f;

        public float IdleSeconds => Mathf.Max(0f, idleSeconds);
        public float ZoomOutSharpness => Mathf.Max(0.1f, zoomOutSharpness);
        public float ZoomInSharpness => Mathf.Max(0.1f, zoomInSharpness);
        public float LookOutDistance => lookOutDistance;
        public float LookOutHeight => lookOutHeight;
    }
}
