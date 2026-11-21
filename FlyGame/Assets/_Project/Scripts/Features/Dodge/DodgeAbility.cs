using UnityEngine;

namespace FlyJam
{
    /// Dodge (P04). Only while a warning is on (GameEvents.WarningChanged); the action button does nothing otherwise.
    /// Stick left/right = dodge that way (relative to the fly). Neutral = DodgeBalance.neutralMode.
    /// Temporarily overrides SphereWalker movement (BeginOverride) and makes the fly invulnerable for the dodge.
    [DefaultExecutionOrder(-10)]   // Reads input before SphereWalker.Update
    public class DodgeAbility : MonoBehaviour
    {
        [SerializeField] SphereWalker walker;            // Auto-filled from GameContext.Player if null
        [SerializeField] DodgeBalance balance;

        IMovingObstacle threat;
        float timer, cd, yawSign;
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

            Begin(InputCompat.Steer());
        }

        bool HasThreat() => threat != null && !(threat is Object uo && uo == null);

        void Begin(float steer)
        {
            Transform t = walker.transform;
            Vector3 up = walker.Planet.UpAt(t.position);
            Vector3 fwd = Vector3.ProjectOnPlane(t.forward, up).normalized;
            Vector3 right = Vector3.Cross(up, fwd);
            float dead = balance != null ? balance.SteerDeadZone : 0.3f;

            if (Mathf.Abs(steer) > dead)
            {
                dir = right * Mathf.Sign(steer);
                yawSign = Mathf.Sign(steer);
            }
            else if (balance != null && balance.NeutralMode == 1)
            {
                Vector3 along = Vector3.ProjectOnPlane(threat.Direction, up);
                dir = along.sqrMagnitude > 1e-6f ? along.normalized : right;
                yawSign = 0f;
            }
            else
            {
                dir = AsideFromPath(t.position, up, right);
                yawSign = Mathf.Sign(Vector3.Dot(dir, right));
            }

            timer = Duration;
            cd = balance != null ? balance.Cooldown : 0.4f;
            walker.BeginOverride(this);
            walker.SetInvulnerable(this, true);
            walker.SetState(FlyState.Dodging);
            if (walker.IsPlayer) GameEvents.RaisePlayerDodged(t.position, dir);
            Tick();
        }

        float Duration => balance != null ? balance.Duration : 0.22f;

        void Tick()
        {
            float dt = Time.deltaTime;
            float spd = (balance != null ? balance.Distance : 2.2f) / Duration;
            walker.MoveBy(dir * spd * dt);
            walker.AlignToSurface();
            Vector3 up = walker.Planet.UpAt(walker.transform.position);
            dir = Vector3.ProjectOnPlane(dir, up).normalized;
            float kick = balance != null ? balance.YawKick : 25f;
            if (yawSign != 0f) walker.TurnAround(yawSign * kick * dt / Duration);
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

        /// Sideways, away from the line the enemy is travelling on.
        Vector3 AsideFromPath(Vector3 pos, Vector3 up, Vector3 fallbackRight)
        {
            Vector3 side = Vector3.Cross(up, threat.Direction);
            if (Vector3.Dot(side, pos - threat.Position) < 0f) side = -side;
            side = Vector3.ProjectOnPlane(side, up);
            return side.sqrMagnitude > 1e-6f ? side.normalized : fallbackRight;
        }
    }
}
