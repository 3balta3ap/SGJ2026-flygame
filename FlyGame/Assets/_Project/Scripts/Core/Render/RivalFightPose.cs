using UnityEngine;

namespace FlyJam
{
    public interface IRivalFightVisual
    {
        void BeginFight(Planet planet, Transform opponent, float phaseOffset);
        void EndFight();
    }

    /// A temporary pose on render transforms only. Root position and rotation belong to gameplay.
    public sealed class RivalFightPose
    {
        const float TurnSeconds = 0.15f;
        const float GaitMultiplier = 2.5f;

        readonly Transform visual, body;
        readonly Transform[] legs;
        readonly Quaternion[] legRotations;
        readonly Quaternion visualLocalRotation, visualStart, facing, bodyRotation;
        readonly Vector3 bodyPosition;
        readonly float phaseOffset;
        float elapsed;

        public RivalFightPose(Transform visual, Transform body, Transform[] legs, Planet planet, Transform opponent, float phaseOffset)
        {
            this.visual = visual;
            this.body = body;
            this.legs = legs;
            this.phaseOffset = phaseOffset;
            visualLocalRotation = visual.localRotation;
            visualStart = visual.rotation;
            Vector3 origin = visual.parent.position;
            Vector3 up = planet.UpAt(origin);
            Vector3 direction = Vector3.ProjectOnPlane(opponent.position - origin, up);
            if (direction.sqrMagnitude < 1e-6f) direction = Vector3.ProjectOnPlane(visual.forward, up);
            facing = direction.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(direction, up) : visualStart;

            if (body != null)
            {
                bodyPosition = body.localPosition;
                bodyRotation = body.localRotation;
            }
            legRotations = new Quaternion[legs.Length];
            for (int i = 0; i < legs.Length; i++)
                if (legs[i] != null) legRotations[i] = legs[i].localRotation;
        }

        public float Advance(float dt, float stepsPerSecond)
        {
            elapsed += dt;
            return (elapsed * stepsPerSecond * GaitMultiplier + phaseOffset) * (2f * Mathf.PI);
        }

        // Runs after the normal gait pose so body travel adds to the walk's existing bob.
        public void Apply()
        {
            float blend = Mathf.SmoothStep(0f, 1f, elapsed / TurnSeconds);
            visual.rotation = Quaternion.Slerp(visualStart, facing, blend);
            if (body == null) return;

            float shove = Mathf.Sin((elapsed * 2.8f + phaseOffset) * (2f * Mathf.PI));
            float shake = Mathf.Sin((elapsed * 6f + phaseOffset) * (2f * Mathf.PI));
            body.localRotation = bodyRotation * Quaternion.Euler(
                (-20f + shove * 5f) * blend, shake * 7f * blend, shake * 3f * blend);
            Vector3 offset = (visual.up * 0.045f + visual.forward * (0.06f * shove)) * blend;
            body.localPosition += body.parent.InverseTransformVector(offset);

            // L1/R1 reach forward while L3/R3 keep most of their planted pose.
            for (int i = 0; i < legs.Length; i++)
            {
                Transform leg = legs[i];
                if (leg == null) continue;
                if (i < 2)
                {
                    float strike = Mathf.Sin((elapsed * 5f + phaseOffset + i * 0.25f) * (2f * Mathf.PI));
                    Vector3 side = leg.parent.InverseTransformDirection(visual.right);
                    leg.localRotation = Quaternion.AngleAxis((-32f + strike * 18f) * blend, side) * leg.localRotation;
                }
                else if (i >= 4)
                    leg.localRotation = Quaternion.Slerp(leg.localRotation, legRotations[i], 0.8f * blend);
            }
        }

        public void Restore()
        {
            if (visual != null) visual.localRotation = visualLocalRotation;
            if (body != null)
            {
                body.localRotation = bodyRotation;
                body.localPosition = bodyPosition;
            }
            for (int i = 0; i < legs.Length; i++)
                if (legs[i] != null) legs[i].localRotation = legRotations[i];
        }
    }
}
