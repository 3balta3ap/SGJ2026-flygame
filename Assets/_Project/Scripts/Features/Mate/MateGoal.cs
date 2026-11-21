using UnityEngine;

namespace FlyJam
{
    /// Raises MateTouched when the player reaches the mate (counts even while invisible).
    public class MateGoal : MonoBehaviour
    {
        [SerializeField] Transform player;
        [SerializeField] float reachDistance = 0.9f;
        [SerializeField] float retryCooldown = 2f;

        float cooldown;
        bool armed = true;

        void Awake()
        {
            if (player == null && GameContext.Current != null && GameContext.Current.Player != null)
                player = GameContext.Current.Player.transform;
        }

        void Update()
        {
            if (cooldown > 0f) { cooldown -= Time.deltaTime; return; }
            if (!armed || player == null) return;
            if (Vector3.Distance(player.position, transform.position) <= reachDistance)
            {
                armed = false;
                GameEvents.RaiseMateTouched();
            }
        }

        public void Rearm()
        {
            cooldown = retryCooldown;
            armed = true;
        }
    }
}
