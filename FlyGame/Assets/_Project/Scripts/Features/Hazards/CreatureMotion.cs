using UnityEngine;

namespace FlyJam
{
    /// Procedural motion for enemy creatures (they have no SphereWalker, so FlyVisual cannot drive them).
    /// Legs walk in two alternating groups while the enemy moves (6 legs = tripod gait like FlyVisual, 8 legs = spider),
    /// the body bobs with the steps, wings buzz (dragonfly: always), antennae sway.
    /// Legs rest while the enemy is stopped. Only a rival fight pose animates during a session pause.
    /// Pivot convention (same as FlyVisual): rest rotation = identity, x = side, y = up, z = forward.
    public class CreatureMotion : MonoBehaviour, IRivalFightVisual
    {
        [SerializeField] RollingObstacle body;              // empty = this object or a parent
        [Tooltip("Optional: moved up and down with the steps")]
        [SerializeField] Transform bodyRoot;
        [Tooltip("Front to back, left then right: L1, R1, L2, R2 ...")]
        [SerializeField] Transform[] legs = new Transform[0];
        [SerializeField] float stepsPerSecond = 5f;
        [SerializeField] float legSwingAngle = 20f;
        [SerializeField] float legLiftAngle = 12f;
        [SerializeField] float bodyBob = 0.015f;
        [Tooltip("Left then right, front pair first: L, R, L, R")]
        [SerializeField] Transform[] wings = new Transform[0];
        [Tooltip("Buzz even when not moving (dragonfly)")]
        [SerializeField] bool wingsAlways;
        [SerializeField] float wingFlapAngle = 28f;
        [SerializeField] float wingFlapFrequency = 14f;
        [SerializeField] Transform[] antennae = new Transform[0];
        [SerializeField] float antennaSway = 10f;
        [SerializeField] float blend = 12f;

        RivalFightPose fight;
        Quaternion[] legRest, wingRest, antRest;
        Vector3 bodyRest;
        float phase, clock;

        void Awake()
        {
            if (body == null) body = GetComponentInParent<RollingObstacle>();
            legRest = Rest(legs);
            wingRest = Rest(wings);
            antRest = Rest(antennae);
            if (bodyRoot != null) bodyRest = bodyRoot.localPosition;
            phase = Random.Range(0f, Mathf.PI * 2f);
            clock = Random.Range(0f, 10f);
        }

        static Quaternion[] Rest(Transform[] ts)
        {
            var r = new Quaternion[ts != null ? ts.Length : 0];
            for (int i = 0; i < r.Length; i++) r[i] = ts[i] != null ? ts[i].localRotation : Quaternion.identity;
            return r;
        }

        public void BeginFight(Planet planet, Transform opponent, float phaseOffset)
        {
            EndFight();
            if (body != null && body.Visual != null && body.Visual != body.transform && body.Visual.IsChildOf(body.transform))
                fight = new RivalFightPose(body.Visual, bodyRoot, legs, planet, opponent, phaseOffset);
        }

        public void EndFight()
        {
            fight?.Restore();
            fight = null;
        }

        void OnDisable() => EndFight();

        void LateUpdate()
        {
            if (fight != null)
            {
                ApplyWalkPose(fight.Advance(Time.deltaTime, stepsPerSecond), true, 1f);
                fight.Apply();
                return;
            }
            var ctx = GameContext.Current;
            if (ctx != null && ctx.Session != null && ctx.Session.IsPaused) return;   // frozen with the world (E04)

            float dt = Time.deltaTime;
            float k = 1f - Mathf.Exp(-blend * dt);
            bool moving = body == null || body.IsMoving;
            clock += dt;
            if (moving) phase += stepsPerSecond * dt * Mathf.PI * 2f;

            ApplyWalkPose(phase, moving, k);

            bool buzz = wingsAlways || moving;
            for (int i = 0; i < wings.Length; i++)
            {
                if (wings[i] == null) continue;
                if (!buzz)
                {
                    wings[i].localRotation = Quaternion.Slerp(wings[i].localRotation, wingRest[i], k);
                    continue;
                }
                float side = i % 2 == 0 ? 1f : -1f;
                float pair = (i / 2) % 2 == 0 ? 0f : Mathf.PI * 0.5f;   // back pair a quarter beat behind
                float flap = Mathf.Sin(clock * wingFlapFrequency * Mathf.PI * 2f + pair) * wingFlapAngle;
                wings[i].localRotation = wingRest[i] * Quaternion.Euler(0f, 0f, flap * side);
            }

            for (int i = 0; i < antennae.Length; i++)
            {
                if (antennae[i] == null) continue;
                float s = Mathf.Sin(clock * 3f + i * 1.7f) * antennaSway;
                antennae[i].localRotation = antRest[i] * Quaternion.Euler(s, 0f, s * 0.5f);
            }
        }

        void ApplyWalkPose(float walkPhase, bool moving, float k)
        {
            for (int i = 0; i < legs.Length; i++)
            {
                if (legs[i] == null) continue;
                if (!moving)
                {
                    legs[i].localRotation = Quaternion.Slerp(legs[i].localRotation, legRest[i], k);
                    continue;
                }
                bool groupA = ((i / 2) + (i % 2)) % 2 == 0;     // L1, R2, L3 (, R4) step together
                float p = walkPhase + (groupA ? 0f : Mathf.PI);
                float side = i % 2 == 0 ? 1f : -1f;             // left = +1 (same signs as FlyVisual)
                float swing = Mathf.Sin(p) * legSwingAngle * side;
                float lift = Mathf.Max(0f, Mathf.Cos(p)) * legLiftAngle * -side;
                legs[i].localRotation = legRest[i] * Quaternion.Euler(0f, swing, lift);
            }

            if (bodyRoot != null)
            {
                Vector3 target = moving ? bodyRest + Vector3.up * Mathf.Abs(Mathf.Sin(walkPhase)) * bodyBob : bodyRest;
                bodyRoot.localPosition = moving ? target : Vector3.Lerp(bodyRoot.localPosition, target, k);
            }
        }
    }
}
