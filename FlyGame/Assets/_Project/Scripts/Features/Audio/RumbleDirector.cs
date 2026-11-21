#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine;

namespace FlyJam
{
    /// Short gamepad rumble: hit (small), level-up (a bit longer). Silently does nothing where the pad or OS has no rumble
    /// (for example some pads over Bluetooth on macOS). Stops on pause, scene change and quit. Lives on SYS_Audio.
    public class RumbleDirector : MonoBehaviour
    {
        [SerializeField] bool rumbleOn = true;
        [SerializeField] Vector3 hit = new Vector3(0.25f, 0.5f, 0.15f);       // low motor, high motor, seconds
        [SerializeField] Vector3 levelUp = new Vector3(0.4f, 0.8f, 0.35f);

        float until = -1f;

        void OnEnable()
        {
            GameEvents.PlayerHitBy += OnHitBy;
            GameEvents.DamageRequested += OnDamageRequested;
            GameEvents.LevelChanged += OnLevel;
            GameEvents.GamePaused += OnPaused;
        }

        void OnDisable()
        {
            GameEvents.PlayerHitBy -= OnHitBy;
            GameEvents.DamageRequested -= OnDamageRequested;
            GameEvents.LevelChanged -= OnLevel;
            GameEvents.GamePaused -= OnPaused;
            Stop();
        }

        void OnApplicationQuit() => Stop();

        void OnHitBy(IObstacle src)
        {
            if (src is IDamageSource ds && ds.DamageRatio == 0f) return;
            Rumble(hit);
        }

        void OnDamageRequested(float ratio, string reason) => Rumble(hit);
        void OnLevel(int level, int previous) { if (level > previous) Rumble(levelUp); }
        void OnPaused(bool on) { if (on) Stop(); }

        void Rumble(Vector3 r)
        {
            if (!rumbleOn) return;
#if ENABLE_INPUT_SYSTEM
            var pad = Gamepad.current;
            if (pad == null) return;
            pad.SetMotorSpeeds(Mathf.Clamp01(r.x), Mathf.Clamp01(r.y));
            until = Time.unscaledTime + Mathf.Max(0.02f, r.z);
#endif
        }

        void Update()
        {
            if (until > 0f && Time.unscaledTime >= until) Stop();
        }

        void Stop()
        {
            until = -1f;
#if ENABLE_INPUT_SYSTEM
            var pad = Gamepad.current;
            if (pad != null) pad.ResetHaptics();
#endif
        }
    }
}
