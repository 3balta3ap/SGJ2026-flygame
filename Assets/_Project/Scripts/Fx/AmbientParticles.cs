using UnityEngine;

namespace FlyJam
{
    /// 낮에는 꽃가루, 밤에는 반딧불.
    public class AmbientParticles : MonoBehaviour
    {
        [SerializeField] ParticleSystem pollen;
        [SerializeField] ParticleSystem fireflies;
        [SerializeField] float pollenMax = 18f;
        [SerializeField] float firefliesMax = 10f;

        void Update()
        {
            float n = DayNightCycle.Current != null ? DayNightCycle.Current.NightFactor : 0f;
            if (pollen != null) { var e = pollen.emission; e.rateOverTime = pollenMax * (1f - n); }
            if (fireflies != null) { var e = fireflies.emission; e.rateOverTime = firefliesMax * n; }
        }
    }
}
