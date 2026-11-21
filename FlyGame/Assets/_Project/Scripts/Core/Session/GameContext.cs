using UnityEngine;

namespace FlyJam
{
    /// Central registry of core scene objects. Features auto-fill null inspector references from here.
    /// Allows features to live in standalone prefabs (SYS_*) without direct scene object references.
    [DefaultExecutionOrder(-1000)]
    public class GameContext : MonoBehaviour
    {
        public static GameContext Current { get; private set; }

        [SerializeField] Planet planet;
        [SerializeField] SphereWalker player;
        [SerializeField] GameSession session;
        [SerializeField] Camera worldCamera;
        [SerializeField] PlanetFollowCamera followCamera;

        public Planet Planet => planet;
        public SphereWalker Player => player;
        public GameSession Session => session;
        public Camera WorldCamera => worldCamera;
        public PlanetFollowCamera FollowCamera => followCamera;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Current = null;

        void Awake() => Current = this;

        void OnDestroy()
        {
            if (Current == this) Current = null;
        }
    }
}
