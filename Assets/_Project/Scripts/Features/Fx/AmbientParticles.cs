using UnityEngine;

namespace FlyJam
{
    /// 낮에는 꽃가루, 밤에는 반딧불. 두 파티클을 카메라 앞에 붙여 따라다니게 한다
    /// (그래서 Main Camera 자식이 아니어도 된다 → SYS_Fx 프리팹으로 뗄 수 있음).
    public class AmbientParticles : MonoBehaviour
    {
        [SerializeField] ParticleSystem pollen;
        [SerializeField] ParticleSystem fireflies;
        [SerializeField] float pollenMax = 5f;
        [SerializeField] float firefliesMax = 6f;
        [SerializeField] Camera worldCamera;                       // 비우면 GameContext에서
        [SerializeField] Vector3 cameraOffset = new Vector3(0f, 0f, 6f);

        void Start()
        {
            if (worldCamera == null && GameContext.Current != null) worldCamera = GameContext.Current.WorldCamera;
        }

        void LateUpdate()
        {
            float n = ToonLighting.Current != null ? ToonLighting.Current.NightFactor : 0f;
            if (pollen != null) { var e = pollen.emission; e.rateOverTime = pollenMax * (1f - n); }
            if (fireflies != null) { var e = fireflies.emission; e.rateOverTime = firefliesMax * n; }

            if (worldCamera == null) return;
            Transform cam = worldCamera.transform;
            Vector3 pos = cam.position + cam.rotation * cameraOffset;
            if (pollen != null) pollen.transform.SetPositionAndRotation(pos, cam.rotation);
            if (fireflies != null) fireflies.transform.SetPositionAndRotation(pos, cam.rotation);
        }
    }
}
