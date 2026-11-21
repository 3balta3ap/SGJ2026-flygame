using UnityEngine;

namespace FlyJam
{
    /// Raises PlayerHitBy(source) then PlayerHit when the player touches an obstacle.
    /// No hits while the walker is invulnerable (dodge, blinking after a hit).
    /// 'rehitDelay' prevents a hit every frame when nothing grants invulnerability (e.g., SYS_Health removed).
    public class ObstacleHitDetector : MonoBehaviour
    {
        [SerializeField] SphereWalker walker;
        [SerializeField] float selfRadius = 0.3f;
        [SerializeField] float rehitDelay = 0.5f;
        float nextHitTime;

        void Update()
        {
            if (walker == null || walker.IsInvulnerable || Time.time < nextHitTime) return;
            if (walker.State == FlyState.Disabled || walker.State == FlyState.Celebrating) return;

            var all = ObstacleRegistry.All;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] == null || (all[i] is Object uo && uo == null)) continue;
                float r = all[i].HitRadius + selfRadius;
                if ((all[i].Position - transform.position).sqrMagnitude <= r * r)
                {
                    nextHitTime = Time.time + rehitDelay;
                    GameEvents.RaisePlayerHitBy(all[i]);   // damage, knockback (Health, Enemy)
                    GameEvents.RaisePlayerHit();           // effects (Fx)
                    return;
                }
            }
        }
    }
}
