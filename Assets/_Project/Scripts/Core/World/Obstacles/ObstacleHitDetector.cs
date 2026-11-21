using UnityEngine;

namespace FlyJam
{
    public class ObstacleHitDetector : MonoBehaviour
    {
        [SerializeField] SphereWalker walker;
        [SerializeField] float selfRadius = 0.3f;
        bool hit;

        void Update()
        {
            if (hit || walker == null || walker.IsInvulnerable) return;
            if (walker.State == FlyState.Disabled || walker.State == FlyState.Celebrating) return;

            var all = ObstacleRegistry.All;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] == null || (all[i] is Object uo && uo == null)) continue;
                float r = all[i].HitRadius + selfRadius;
                if ((all[i].Position - transform.position).sqrMagnitude <= r * r)
                {
                    hit = true;
                    GameEvents.RaisePlayerHit();
                    return;
                }
            }
        }
    }
}
