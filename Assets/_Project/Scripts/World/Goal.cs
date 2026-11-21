using UnityEngine;

namespace FlyJam
{
    public class Goal : MonoBehaviour
    {
        [SerializeField] Transform player;
        [SerializeField] Planet planet;
        [SerializeField] float reachDistance = 1.3f;
        [SerializeField] float landingHeight = 0.75f;  // 과일 반지름 = 착지 지점 높이

        bool reached;

        void Update()
        {
            if (reached || player == null) return;
            if (Vector3.Distance(player.position, transform.position) <= reachDistance)
            {
                reached = true;
                Vector3 landing = transform.position + planet.UpAt(transform.position) * landingHeight;
                GameEvents.RaiseGoalReached(landing);
            }
        }
    }
}
