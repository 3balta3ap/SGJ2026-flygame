using UnityEngine;

namespace FlyJam
{
    /// 씬의 코어 객체를 모아 두는 곳. 기능은 인스펙터 참조가 비어 있으면 여기서 채운다.
    /// 그래서 기능을 별도 프리팹(SYS_*)으로 떼어내도 씬 객체를 직접 연결할 필요가 없다.
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
