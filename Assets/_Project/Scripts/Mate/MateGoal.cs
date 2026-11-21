using UnityEngine;

namespace FlyJam
{
    /// 플레이어가 짝에게 닿으면 MateTouched (보이지 않을 때 닿아도 인정 = '만져서 확인').
    public class MateGoal : MonoBehaviour
    {
        [SerializeField] Transform player;
        [SerializeField] float reachDistance = 0.9f;
        [SerializeField] float retryCooldown = 2f;

        float cooldown;
        bool armed = true;

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
