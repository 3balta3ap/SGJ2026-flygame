using UnityEngine;

namespace FlyJam
{
    /// Quick-turn dodge (Space). Space+A/D = sidestep, Space+S = backstep, Space+W = dash forward, Space alone = sidestep away from nearest moving threat.
    /// Temporarily overrides SphereWalker movement (BeginOverride) and enables invulnerability (SetInvulnerable).
    /// Removing this component disables dodging cleanly without affecting any other system.
    [DefaultExecutionOrder(-10)]   // Reads input before SphereWalker.Update
    public class DodgeAbility : MonoBehaviour
    {
        [SerializeField] SphereWalker walker;            // Auto-filled from GameContext.Player if null
        [SerializeField] float distance = 2.2f;
        [SerializeField] float duration = 0.22f;
        [SerializeField] float cooldown = 0.7f;
        [SerializeField] float yawKick = 25f;
        [SerializeField] float threatSearchRadius = 6f;
        [SerializeField] float inputDeadZone = 0.1f;

        float timer, cd, yawSign;
        Vector3 dir;

        public bool IsDodging => timer > 0f;

        void Start()
        {
            if (walker == null && GameContext.Current != null) walker = GameContext.Current.Player;
        }

        void OnDisable() => Finish();

        void Update()
        {
            if (walker == null) return;
            if (cd > 0f) cd -= Time.deltaTime;
            if (timer > 0f) { Tick(); return; }

            if (cd > 0f || !walker.InputEnabled || walker.Input == null || walker.IsOverridden) return;
            if (walker.State == FlyState.Celebrating || walker.State == FlyState.Disabled) return;
            if (!walker.Input.ReadDodge()) return;

            Begin(Vector2.ClampMagnitude(walker.Input.ReadMove(), 1f));
        }

        void Begin(Vector2 raw)
        {
            Transform t = walker.transform;
            Vector3 up = walker.Planet.UpAt(t.position);
            Vector3 fwd = Vector3.ProjectOnPlane(t.forward, up).normalized;
            Vector3 right = Vector3.Cross(up, fwd);

            if (Mathf.Abs(raw.x) > inputDeadZone)
            {
                dir = right * Mathf.Sign(raw.x);
                yawSign = Mathf.Sign(raw.x);
            }
            else if (raw.y < -inputDeadZone) { dir = -fwd; yawSign = 0f; }
            else if (raw.y > inputDeadZone) { dir = fwd; yawSign = 0f; }
            else
            {
                dir = SidestepFromThreat(t.position, up, right);
                yawSign = Mathf.Sign(Vector3.Dot(dir, right));
            }

            timer = duration;
            cd = cooldown;
            walker.BeginOverride(this);
            walker.SetInvulnerable(this, true);
            walker.SetState(FlyState.Dodging);
            if (walker.IsPlayer) GameEvents.RaisePlayerDodged(t.position, dir);
            Tick();
        }

        void Tick()
        {
            float dt = Time.deltaTime;
            float spd = distance / duration;
            walker.MoveBy(dir * spd * dt);
            walker.AlignToSurface();
            Vector3 up = walker.Planet.UpAt(walker.transform.position);
            dir = Vector3.ProjectOnPlane(dir, up).normalized;
            if (yawSign != 0f) walker.TurnAround(yawSign * yawKick * dt / duration);
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

        Vector3 SidestepFromThreat(Vector3 pos, Vector3 up, Vector3 fallbackRight)
        {
            IMovingObstacle nearest = null;
            float best = threatSearchRadius;
            var all = ObstacleRegistry.All;
            for (int i = 0; i < all.Count; i++)
            {
                if (!(all[i] is IMovingObstacle m)) continue;
                if (all[i] is Object uo && uo == null) continue;
                float d = Vector3.Distance(m.Position, pos);
                if (d < best) { best = d; nearest = m; }
            }
            if (nearest != null)
            {
                Vector3 side = Vector3.Cross(up, nearest.Direction);
                if (Vector3.Dot(side, pos - nearest.Position) < 0f) side = -side;
                side = Vector3.ProjectOnPlane(side, up);
                if (side.sqrMagnitude > 1e-6f) return side.normalized;
            }
            return fallbackRight;
        }
    }
}
