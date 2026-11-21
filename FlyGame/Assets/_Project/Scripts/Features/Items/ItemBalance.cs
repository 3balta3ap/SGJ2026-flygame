using UnityEngine;

namespace FlyJam
{
    /// Planner numbers for red apples (C02). Sheet tab "Items".
    public class ItemBalance : BalanceTable
    {
        public override string SheetName => "Items";

        [Tooltip("HP restored per apple, as a fraction of max HP (0.2 of 10 = +2). Capped at max HP")]
        [SerializeField] float healRatio = 0.2f;
        [Tooltip("An eaten apple comes back at the same point once the player is this far (straight line) from that point")]
        [SerializeField] float respawnDistance = 20f;
        [Tooltip("Pick-up distance between the player and the apple")]
        [SerializeField] float pickupRadius = 1.0f;
        [Tooltip("Apple height above the ground")]
        [SerializeField] float hoverHeight = 0.6f;
        [Tooltip("Up/down float height")]
        [SerializeField] float bobHeight = 0.12f;
        [Tooltip("Up/down float speed")]
        [SerializeField] float bobSpeed = 2.5f;
        [Tooltip("Spin, degrees per second")]
        [SerializeField] float spinSpeed = 90f;

        public float HealRatio => Mathf.Max(0f, healRatio);
        public float RespawnDistance => Mathf.Max(0f, respawnDistance);
        public float PickupRadius => Mathf.Max(0.1f, pickupRadius);
        public float HoverHeight => hoverHeight;
        public float BobHeight => Mathf.Max(0f, bobHeight);
        public float BobSpeed => bobSpeed;
        public float SpinSpeed => spinSpeed;
    }
}
