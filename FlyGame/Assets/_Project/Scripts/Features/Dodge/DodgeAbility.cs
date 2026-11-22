using UnityEngine;

namespace FlyJam
{
    /// Dash (P04). Only while a warning is on (GameEvents.WarningChanged); the action button does nothing otherwise.
    /// Chooses a sidestep, forward dash, or rear feint from the threat's bearing at the moment of the press.
    /// Invulnerable during the dash, so it may pass through the enemy.
    /// Temporarily overrides SphereWalker movement (BeginOverride).
    [DefaultExecutionOrder(-10)]   // Reads input before SphereWalker.Update
    public class DodgeAbility : MonoBehaviour
    {
        [SerializeField] SphereWalker walker;            // Auto-filled from GameContext.Player if null
        [SerializeField] DodgeBalance balance;

        IMovingObstacle threat;
        const float StepSeconds = 1f / 120f;
        const float ExitLookahead = 0.65f;

        struct Phase
        {
            public readonly float Duration, StartSpeed, EndSpeed;
            public float Angle;

            public Phase(float duration, float startSpeed, float endSpeed, float angle = 0f)
            {
                Duration = duration;
                StartSpeed = startSpeed;
                EndSpeed = endSpeed;
                Angle = angle;
            }
        }

        readonly Phase[] phases = new Phase[2];
        int phaseCount, phaseIndex;
        float phaseTime, cd;
        Vector3 dir;

        public bool IsDodging => phaseIndex < phaseCount;

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
            if (IsDodging) { Tick(Time.deltaTime); return; }

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
            Vector3 forward = Vector3.ProjectOnPlane(t.forward, up).normalized;
            if (forward == Vector3.zero)
                forward = Vector3.Cross(t.right, up).normalized;
            Vector3 enemyStart = threat.Position;
            Vector3 toThreat = Vector3.ProjectOnPlane(enemyStart - t.position, up);
            ChoosePath(forward, up, toThreat, enemyStart);
            cd = balance != null ? balance.Cooldown : 0.4f;
            walker.BeginOverride(this);
            walker.SetInvulnerable(this, true);
            walker.SetState(FlyState.Dodging);
            if (walker.IsPlayer) GameEvents.RaisePlayerDodged(t.position, dir);
            Tick(Time.deltaTime);
        }

        void ChoosePath(Vector3 forward, Vector3 up, Vector3 toThreat, Vector3 enemyStart)
        {
            phaseIndex = 0;
            phaseTime = 0f;
            dir = forward;
            float duration = balance != null ? balance.Duration : 0.3f;
            float speed = (balance != null ? balance.Distance : 3f) / duration;
            phaseCount = 1;
            phases[0] = new Phase(duration, speed, speed);
            if (toThreat.sqrMagnitude <= 1e-6f) return;

            Vector3 right = Vector3.Cross(up, forward);
            float bearing = Mathf.Atan2(Vector3.Dot(toThreat, right), Vector3.Dot(toThreat, forward)) * Mathf.Rad2Deg;
            float angle = Mathf.Abs(bearing);
            float side = Mathf.Sign(bearing);
            float frontAngle = balance != null ? balance.FrontAngle : DodgeBalance.DefaultFrontAngle;
            float rearAngle = balance != null ? balance.RearAngle : DodgeBalance.DefaultRearAngle;
            float feintStart = balance != null ? balance.FeintStartAngle : DodgeBalance.DefaultFeintStartAngle;

            if (angle <= frontAngle)
                PlanSide(Random.Range(0, 2) == 0 ? -1f : 1f, enemyStart);
            else if (angle >= 180f - rearAngle)
                return;
            else if (angle <= feintStart)
                PlanSide(-side, enemyStart);
            else
                PlanFeint(side, angle, toThreat, enemyStart);
        }

        void PlanSide(float side, Vector3 enemyStart)
        {
            float turnTime = balance != null ? balance.SideTurnSeconds : DodgeBalance.DefaultSideTurnSeconds;
            float turnSpeed = balance != null ? balance.SideTurnSpeed : DodgeBalance.DefaultSideTurnSpeed;
            float exitTime = balance != null ? balance.SideAccelerateSeconds : DodgeBalance.DefaultSideAccelerateSeconds;
            float exitSpeed = balance != null ? balance.SideExitSpeed : DodgeBalance.DefaultSideExitSpeed;
            float angle = balance != null ? balance.SideTurnAngle : DodgeBalance.DefaultSideTurnAngle;

            phaseCount = 2;
            phases[0] = new Phase(turnTime, turnSpeed, turnSpeed, side * angle);
            phases[1] = new Phase(exitTime, turnSpeed, exitSpeed);
            SelectExit(0, 1f, 135f, enemyStart);
        }

        void PlanFeint(float side, float bearing, Vector3 toThreat, Vector3 enemyStart)
        {
            float escapeTime = balance != null ? balance.FeintEscapeSeconds : DodgeBalance.DefaultFeintEscapeSeconds;
            float radius = balance != null ? balance.FeintTurnRadius : DodgeBalance.DefaultFeintTurnRadius;
            float speed = balance != null ? balance.FeintSpeed : DodgeBalance.DefaultFeintSpeed;
            float endAngle = balance != null ? balance.FeintEndAngle : DodgeBalance.DefaultFeintEndAngle;

            dir = -toThreat.normalized;
            phaseCount = 2;
            phases[0] = new Phase(escapeTime, speed * 0.5f, speed);
            float minAngle = Mathf.Min(180f, 91f + 180f - bearing);
            float turnAngle = side * Mathf.Min(180f, endAngle + 180f - bearing);
            phases[1] = FeintArc(speed, radius, turnAngle);
            SelectFeintArc(speed, radius, minAngle, enemyStart);
        }

        static Phase FeintArc(float speed, float radius, float angle)
        {
            // At constant speed, arc length is radius times the heading change in radians.
            float duration = radius * Mathf.Abs(angle) * Mathf.Deg2Rad / speed;
            return new Phase(duration, speed, speed, angle);
        }

        struct EnemyMotion
        {
            public Vector3 Center, Radial, Axis;
            public float AngularSpeed, StoppedFor, Clearance;

            public Vector3 PositionAt(float time) => Center +
                Quaternion.AngleAxis(AngularSpeed * Mathf.Max(0f, time - StoppedFor), Axis) * Radial;
        }

        bool TryGetEnemyMotion(Vector3 enemyStart, out EnemyMotion enemy)
        {
            enemy = default;
            if (!(threat is IObstacleDodgeMotion motion)) return false;

            Vector3 center = walker.Planet.Center;
            Vector3 radial = enemyStart - center;
            Vector3 enemyDir = Vector3.ProjectOnPlane(threat.Direction, radial).normalized;
            float radius = radial.magnitude;
            if (radius < 0.001f || enemyDir == Vector3.zero) return false;

            enemy = new EnemyMotion
            {
                Center = center,
                Radial = radial,
                Axis = Vector3.Cross(radial / radius, enemyDir),
                AngularSpeed = motion.TravelSpeed / radius * Mathf.Rad2Deg,
                StoppedFor = motion.StopSecondsAfterDodge,
                Clearance = threat.HitRadius + 0.6f
            };
            return true;
        }

        void SelectFeintArc(float speed, float radius, float minAngle, Vector3 enemyStart)
        {
            if (!TryGetEnemyMotion(enemyStart, out EnemyMotion enemy)) return;

            Phase nominal = phases[1];
            Phase best = nominal;
            float bestExit = MeasureClearance(enemy, true, out float bestTurn);
            if (Mathf.Min(bestExit, bestTurn) >= enemy.Clearance) return;

            // Compare a bounded set of arcs. Keep the turn side and a rear-facing exit.
            for (int radiusOffset = 0; radiusOffset < 3; radiusOffset++)
            {
                float candidateRadius = Mathf.Max(0.5f, radius * (radiusOffset == 0 ? 1f : radiusOffset == 1 ? 0.85f : 1.15f));
                for (int offset = 0; offset <= 45; offset += 15)
                {
                    for (int sign = offset == 0 ? 1 : -1; sign <= 1; sign += 2)
                    {
                        float angle = Mathf.Sign(nominal.Angle) * Mathf.Clamp(Mathf.Abs(nominal.Angle) + sign * offset, minAngle, 180f);
                        if (radiusOffset == 0 && angle == nominal.Angle) continue;
                        phases[1] = FeintArc(speed, candidateRadius, angle);
                        float exit = MeasureClearance(enemy, true, out float turn);
                        if (Mathf.Min(exit, turn) >= enemy.Clearance) return;

                        // At fallback, prioritize safety once dodge invulnerability is gone.
                        if (Mathf.Min(exit, enemy.Clearance) > Mathf.Min(bestExit, enemy.Clearance) ||
                            (Mathf.Min(exit, enemy.Clearance) == Mathf.Min(bestExit, enemy.Clearance) && turn > bestTurn))
                        {
                            best = phases[1];
                            bestExit = exit;
                            bestTurn = turn;
                        }
                    }
                }
            }
            phases[1] = best;
        }

        void SelectExit(int turnIndex, float minAngle, float maxAngle, Vector3 enemyStart)
        {
            if (!TryGetEnemyMotion(enemyStart, out EnemyMotion enemy)) return;

            float nominal = phases[turnIndex].Angle;
            float bestAngle = nominal;
            float best = MeasureClearance(enemy, false, out _);
            if (best >= enemy.Clearance) return;

            // Prefer the closest viable angle to the requested heading, on the same turn side.
            for (int offset = 15; offset <= 45; offset += 15)
            {
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    float candidate = Mathf.Sign(nominal) * Mathf.Clamp(Mathf.Abs(nominal) + sign * offset, minAngle, maxAngle);
                    phases[turnIndex].Angle = candidate;
                    float score = MeasureClearance(enemy, false, out _);
                    if (score >= enemy.Clearance) return;
                    if (score > best)
                    {
                        best = score;
                        bestAngle = candidate;
                    }
                }
            }
            phases[turnIndex].Angle = bestAngle;
        }

        float MeasureClearance(EnemyMotion enemy, bool includeTurn, out float turnClearance)
        {
            Vector3 position = walker.transform.position;
            Vector3 heading = dir;
            int index = 0;
            float elapsed = 0f;
            float time = 0f;
            float turnDistanceSq = float.PositiveInfinity;
            if (includeTurn)
            {
                time = Advance(ref position, ref heading, ref index, ref elapsed, phases[0].Duration, false);
                while (index < phaseCount)
                {
                    Vector3 previous = position;
                    float step = Advance(ref position, ref heading, ref index, ref elapsed, StepSeconds, false);
                    turnDistanceSq = Mathf.Min(turnDistanceSq, SegmentClearance(enemy, previous, position, time, time + step));
                    time += step;
                }
            }
            else
                time = Advance(ref position, ref heading, ref index, ref elapsed, float.PositiveInfinity, false);

            turnClearance = Mathf.Sqrt(turnDistanceSq);
            float autoSpeed = walker.MoveSpeed * walker.SpeedMultiplier;
            float closest = float.PositiveInfinity;

            float end = time + ExitLookahead;
            while (time < end)
            {
                float next = Mathf.Min(time + 1f / 60f, end);
                if (time < enemy.StoppedFor) next = Mathf.Min(next, enemy.StoppedFor);
                Vector3 previous = position;
                AdvanceStep(ref position, ref heading, autoSpeed * (next - time), 0f);
                closest = Mathf.Min(closest, SegmentClearance(enemy, previous, position, time, next));
                time = next;
            }
            return Mathf.Sqrt(closest);
        }

        static float SegmentClearance(EnemyMotion enemy, Vector3 from, Vector3 to, float start, float end)
        {
            if (end <= enemy.StoppedFor) return float.PositiveInfinity;
            if (start < enemy.StoppedFor)
            {
                from = Vector3.Lerp(from, to, (enemy.StoppedFor - start) / (end - start));
                start = enemy.StoppedFor;
            }

            from -= enemy.PositionAt(start);
            to -= enemy.PositionAt(end);
            Vector3 relativeStep = to - from;
            float fraction = relativeStep.sqrMagnitude > 1e-8f
                ? Mathf.Clamp01(-Vector3.Dot(from, relativeStep) / relativeStep.sqrMagnitude)
                : 0f;
            return (from + fraction * relativeStep).sqrMagnitude;
        }

        void Tick(float dt)
        {
            if (dt <= 0f) return;
            Vector3 position = walker.transform.position;
            float used = Advance(ref position, ref dir, ref phaseIndex, ref phaseTime, dt, true);
            walker.ConsumeMovementTime(used);
            if (!IsDodging) Finish();
        }

        float Advance(ref Vector3 position, ref Vector3 heading, ref int index, ref float elapsed, float dt, bool apply)
        {
            float used = 0f;
            while (dt > 0f && index < phaseCount)
            {
                Phase phase = phases[index];
                float remaining = phase.Duration - elapsed;
                float maxStep = StepSeconds;
                if (phase.Angle != 0f) maxStep = Mathf.Min(maxStep, phase.Duration * 5f / Mathf.Abs(phase.Angle));
                float step = Mathf.Min(Mathf.Min(dt, remaining), maxStep);
                float speed = Mathf.Lerp(phase.StartSpeed, phase.EndSpeed, (elapsed + step * 0.5f) / phase.Duration);
                AdvanceStep(ref position, ref heading, speed * step, phase.Angle * (step / phase.Duration));
                if (apply)
                {
                    walker.transform.SetPositionAndRotation(position, Quaternion.LookRotation(heading, walker.Planet.UpAt(position)));
                    walker.SetMotion(speed, speed);
                }
                if (step >= remaining)
                {
                    index++;
                    elapsed = 0f;
                }
                else elapsed += step;
                dt -= step;
                used += step;
            }
            return used;
        }

        void AdvanceStep(ref Vector3 position, ref Vector3 heading, float distance, float turn)
        {
            Vector3 up = walker.Planet.UpAt(position);
            heading = Quaternion.AngleAxis(turn * 0.5f, up) * heading;
            if (distance > 0f) position = walker.PreviewMove(position, heading * distance);
            Vector3 nextUp = walker.Planet.UpAt(position);
            heading = Quaternion.FromToRotation(up, nextUp) * heading;
            heading = Quaternion.AngleAxis(turn * 0.5f, nextUp) * heading;
        }

        void Finish()
        {
            phaseIndex = phaseCount;
            if (walker == null) return;
            walker.EndOverride(this);
            walker.SetInvulnerable(this, false);
            if (walker.State == FlyState.Dodging)
                walker.SetState(FlyState.Stopped);
        }
    }
}
