using UnityEngine;

namespace FlyJam
{
    /// Pollen by day, fireflies by night. Keeps both particle systems positioned in front of the camera
    /// (so they do not need to be children of Main Camera and can live inside SYS_Fx.prefab).
    public class AmbientParticles : MonoBehaviour
    {
        [SerializeField] ParticleSystem pollen;
        [SerializeField] ParticleSystem fireflies;
        [SerializeField] float pollenMax = 5f;
        [SerializeField] float firefliesMax = 6f;
        [SerializeField] Camera worldCamera;                       // Auto-filled from GameContext if null
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
