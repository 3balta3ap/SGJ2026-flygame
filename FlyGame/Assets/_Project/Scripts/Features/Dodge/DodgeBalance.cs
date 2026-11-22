using UnityEngine;

namespace FlyJam
{
    /// Planner numbers for the dash (P04). Sheet tab "Dodge".
    public class DodgeBalance : BalanceTable
    {
        public override string SheetName => "Dodge";

        internal const float DefaultFrontAngle = 15f;
        internal const float DefaultRearAngle = 20f;
        internal const float DefaultFeintStartAngle = 90f;
        internal const float DefaultSideTurnAngle = 90f;
        internal const float DefaultFeintEscapeSeconds = 0.20f;
        internal const float DefaultFeintTurnRadius = 1.2f;
        internal const float DefaultFeintSpeed = 8f;
        internal const float DefaultSideTurnSeconds = 0.06f;
        internal const float DefaultSideTurnSpeed = 1.5f;
        internal const float DefaultSideAccelerateSeconds = 0.24f;
        internal const float DefaultSideExitSpeed = 12f;
        internal const float DefaultFeintEndAngle = 135f;
        internal const float DefaultForwardDashSeconds = 0.3f;
        internal const float DefaultForwardDashStartSpeed = 4f;
        internal const float DefaultForwardDashEndSpeed = 12f;

        [Tooltip("Nominal distance along a straight forward dash")]
        [SerializeField] float distance = 3f;
        [Tooltip("Straight forward dash duration in seconds")]
        [SerializeField] float duration = 0.3f;
        [Tooltip("Shared cooldown in seconds, measured from the start of either action")]
        [SerializeField] float cooldown = 0.4f;
        [Tooltip("Duration of a forward dash when no warning is active")]
        [SerializeField, Min(0.01f)] float forwardDashSeconds = DefaultForwardDashSeconds;
        [SerializeField, Min(0f)] float forwardDashStartSpeed = DefaultForwardDashStartSpeed;
        [SerializeField, Min(0f)] float forwardDashEndSpeed = DefaultForwardDashEndSpeed;
        [Tooltip("Half-angle from straight ahead for a random sidestep")]
        [SerializeField, Range(0f, 90f)] float frontAngle = DefaultFrontAngle;
        [Tooltip("Half-angle from straight behind for a forward dash")]
        [SerializeField, Range(0f, 90f)] float rearAngle = DefaultRearAngle;
        [Tooltip("Bearings beyond this angle may trigger a rear feint, outside the straight-behind sector")]
        [SerializeField, Range(90f, 180f)] float feintStartAngle = DefaultFeintStartAngle;
        [Tooltip("Nominal heading change during a side dash")]
        [SerializeField, Range(1f, 135f)] float sideTurnAngle = DefaultSideTurnAngle;
        [SerializeField, Min(0.01f)] float sideTurnSeconds = DefaultSideTurnSeconds;
        [SerializeField, Min(0.1f)] float sideTurnSpeed = DefaultSideTurnSpeed;
        [SerializeField, Min(0.01f)] float sideAccelerateSeconds = DefaultSideAccelerateSeconds;
        [SerializeField, Min(0.1f)] float sideExitSpeed = DefaultSideExitSpeed;
        [Tooltip("Seconds spent moving away from a rear threat before turning")]
        [SerializeField, Min(0.01f)] float feintEscapeSeconds = DefaultFeintEscapeSeconds;
        [Tooltip("Nominal turning radius of the rear feint on the planet surface")]
        [SerializeField, Min(0.5f)] float feintTurnRadius = DefaultFeintTurnRadius;
        [Tooltip("Peak escape speed, maintained through the rear turn")]
        [SerializeField, Min(0.1f)] float feintSpeed = DefaultFeintSpeed;
        [Tooltip("Target heading from initial forward, on the threat's side. The turn is limited to a half-circle")]
        [SerializeField, Range(91f, 179f)] float feintEndAngle = DefaultFeintEndAngle;

        public float Distance => Mathf.Max(0.1f, distance);
        public float Duration => Mathf.Max(0.05f, duration);
        public float Cooldown => Mathf.Max(0f, cooldown);
        public float ForwardDashSeconds => Mathf.Max(0.01f, forwardDashSeconds);
        public float ForwardDashStartSpeed => Mathf.Max(0f, forwardDashStartSpeed);
        public float ForwardDashEndSpeed => Mathf.Max(ForwardDashStartSpeed, forwardDashEndSpeed);
        public float FrontAngle => Mathf.Clamp(frontAngle, 0f, 90f);
        public float RearAngle => Mathf.Clamp(rearAngle, 0f, 90f);
        public float FeintStartAngle => Mathf.Clamp(feintStartAngle, 90f, 180f - RearAngle);
        public float SideTurnAngle => Mathf.Clamp(sideTurnAngle, 1f, 135f);
        public float SideTurnSeconds => Mathf.Max(0.01f, sideTurnSeconds);
        public float SideTurnSpeed => Mathf.Max(0.1f, sideTurnSpeed);
        public float SideAccelerateSeconds => Mathf.Max(0.01f, sideAccelerateSeconds);
        public float SideExitSpeed => Mathf.Max(SideTurnSpeed, sideExitSpeed);
        public float FeintEscapeSeconds => Mathf.Max(0.01f, feintEscapeSeconds);
        public float FeintTurnRadius => Mathf.Max(0.5f, feintTurnRadius);
        public float FeintSpeed => Mathf.Max(0.1f, feintSpeed);
        public float FeintEndAngle => Mathf.Clamp(feintEndAngle, 91f, 179f);
    }
}
