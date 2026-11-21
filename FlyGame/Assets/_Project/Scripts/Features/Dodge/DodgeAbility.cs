using UnityEngine;

namespace FlyJam
{
    /// Dash (P04). Only while a warning is on (GameEvents.WarningChanged); the action button does nothing otherwise.
    /// Always straight ahead (the way the fly faces). Invulnerable during the dash, so it may pass through the enemy:
    /// NearMissDirector counts it as a dodge once the enemy is behind and clear (or freezes, stopOnDodge).
    /// Temporarily overrides SphereWalker movement (BeginOverride).
    [DefaultExecutionOrder(-10)]   // Reads input before SphereWalker.Update
    public class DodgeAbility : MonoBehaviour
    {
        [SerializeField] SphereWalker walker;            // Auto-filled from GameContext.Player if null
        [SerializeField] DodgeBalance balance;

        IMovingObstacle threat;
        float timer, cd;
        Vector3 dir;

        public bool IsDodging => timer > 0f;

        void Start()
        {
            if (walker == null && GameContext.Current != null) walker = GameContext.Current.Player;
        }

        void OnEnable() => GameEvents.WarningChanged += OnWarning;

        void OnDisable()
        {
            GameEvents.WarningChanged -= OnWarning;
            Finish();
        }

        void OnWarning(IMovingObstacle t) => threat = t;

        void Update()
        {
            if (walker == null) return;
            if (cd > 0f) cd -= Time.deltaTime;
            if (timer > 0f) { Tick(); return; }

            if (!HasThreat() || cd > 0f || TimeControl.InputBlocked) return;
            if (!walker.InputEnabled || walker.Input == null || walker.IsOverridden) return;
            if (walker.State == FlyState.Celebrating || walker.State == FlyState.Disabled) return;
            if (!walker.Input.ReadDodge()) return;

            Begin();
        }

        bool HasThreat() => threat != null && !(threat is Object uo && uo == null);

        void Begin()
        {
            Transform t = walker.transform;
            Vector3 up = walker.Planet.UpAt(t.position);
            dir = Vector3.ProjectOnPlane(t.forward, up).normalized;

            timer = Duration;
            cd = balance != null ? balance.Cooldown : 0.4f;
            walker.BeginOverride(this);
            walker.SetInvulnerable(this, true);
            walker.SetState(FlyState.Dodging);
            if (walker.IsPlayer) GameEvents.RaisePlayerDodged(t.position, dir);
            Tick();
        }

        float Duration => balance != null ? balance.Duration : 0.3f;

        void Tick()
        {
            float dt = Time.deltaTime;
            float spd = (balance != null ? balance.Distance : 3f) / Duration;
            walker.MoveBy(dir * spd * dt);
            walker.AlignToSurface();
            Vector3 up = walker.Planet.UpAt(walker.transform.position);
            dir = Vector3.ProjectOnPlane(dir, up).normalized;
            walker.SetMotion(spd, spd);

            timer -= dt;
            if (timer <= 0f) Finish();
        }

        void Finish()
        {
            timer = 0f;
            if (walker == null) return;
            walker.EndOverride(this);
            walker.SetInvulnerable(this, false);
            if (walker.State == FlyState.Dodging) walker.SetState(FlyState.Stopped);
        }
    }
}
