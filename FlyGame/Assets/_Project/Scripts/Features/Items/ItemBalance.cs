using UnityEngine;

namespace FlyJam
{
    /// Planner numbers for food items (C02). Sheet tabs "Items" (key/value) and "ItemTypes" (one row per item).
    /// Each fixed point holds one item. When it comes back, it is a rare item (banana) with the level's
    /// rareItemChance ("Level" table), otherwise a normal one (apple).
    public class ItemBalance : BalanceTable
    {
        public override string SheetName => "Items";

        [System.Serializable]
        public struct TypeRow
        {
            public string id;          // must match an id in AppleSpawner.prefabs (apple, banana)
            public bool rare;          // 1 = rare item: appears with the level's rareItemChance
            public float healRatio;    // HP restored, fraction of max HP (0.2 of 10 = +2). Capped at max HP
            public int exp;            // EXP gained
        }

        [Tooltip("An eaten item comes back at the same point once the player is this far (straight line) from that point")]
        [SerializeField] float respawnDistance = 20f;
        [Tooltip("Pick-up distance between the player and the item")]
        [SerializeField] float pickupRadius = 1.0f;
        [Tooltip("Item height above the ground")]
        [SerializeField] float hoverHeight = 0.75f;
        [Tooltip("Up/down float height")]
        [SerializeField] float bobHeight = 0.35f;
        [Tooltip("Up/down float speed")]
        [SerializeField] float bobSpeed = 4.5f;
        [Tooltip("Spin, degrees per second")]
        [SerializeField] float spinSpeed = 90f;
        [Tooltip("Rare item chance when the Level feature is off (with it: the Level table's rareItemChance)")]
        [SerializeField] float rareChance = 0.1f;

        [SheetRows("ItemTypes")]
        [SerializeField] TypeRow[] types =
        {
            new TypeRow { id = "apple",  rare = false, healRatio = 0.2f, exp = 1 },
            new TypeRow { id = "banana", rare = true,  healRatio = 0.4f, exp = 2 },
        };

        public float RespawnDistance => Mathf.Max(0f, respawnDistance);
        public float PickupRadius => Mathf.Max(0.1f, pickupRadius);
        public float HoverHeight => hoverHeight;
        public float BobHeight => Mathf.Max(0f, bobHeight);
        public float BobSpeed => bobSpeed;
        public float SpinSpeed => spinSpeed;
        public float RareChance => Mathf.Clamp01(rareChance);
        public TypeRow[] Types => types;
    }
}
