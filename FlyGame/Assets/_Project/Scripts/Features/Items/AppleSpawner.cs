using System.Collections.Generic;
using UnityEngine;

namespace FlyJam
{
    /// Food at fixed points (C02, A02): apples, and bananas (rare, heal more, more EXP).
    /// Each child transform in 'points' is one spot (only its direction from the planet centre matters; it is snapped to the surface).
    /// Put a KeepClearZone on every point so the village generator leaves room for the item.
    /// Touch -> HealRequested + ExpRequested + ItemEaten, the item disappears. It comes back at the same spot once the player
    /// is respawnDistance away; what comes back is rolled again (rare chance grows with the level).
    /// (Class name kept from CHI-05 so the SYS_Items prefab link survives.)
    public class AppleSpawner : MonoBehaviour
    {
        [System.Serializable]
        public struct TypePrefab
        {
            public string id;
            public GameObject prefab;
        }

        [SerializeField] ItemBalance balance;
        [Tooltip("One prefab per ItemTypes id (apple -> Apple.prefab, banana -> Banana.prefab)")]
        [SerializeField] TypePrefab[] prefabs;
        [Tooltip("Old single-apple field. Used for id 'apple' only when 'prefabs' has no apple")]
        [SerializeField] GameObject applePrefab;
        [SerializeField] Transform[] points;
        [SerializeField] Planet planet;             // Auto-filled from GameContext if null
        [SerializeField] SphereWalker player;       // Auto-filled from GameContext if null
        [SerializeField] GameSession session;       // Auto-filled from GameContext if null

        class Spot
        {
            public Vector3 ground;
            public GameObject item;
            public ItemBalance.TypeRow row;
            public bool waiting;
        }

        readonly List<Spot> spots = new List<Spot>();

        public int ActiveCount
        {
            get
            {
                int n = 0;
                foreach (var s in spots) if (s.item != null && !s.waiting) n++;
                return n;
            }
        }

        void Start()
        {
            var ctx = GameContext.Current;
            if (ctx != null)
            {
                if (planet == null) planet = ctx.Planet;
                if (player == null) player = ctx.Player;
                if (session == null) session = ctx.Session;
            }
            if (planet == null || points == null || balance == null) return;

            foreach (var p in points)
            {
                if (p == null) continue;
                Vector3 dir = p.position - planet.transform.position;
                if (dir.sqrMagnitude < 1e-6f) continue;
                var s = new Spot { ground = planet.SurfacePoint(dir.normalized) };
                spots.Add(s);
                Fill(s);
            }
        }

        void Update()
        {
            if (player == null || session == null || session.Ended) return;
            if (session.IsPaused || player.State == FlyState.Disabled) return;

            float pick = balance.PickupRadius;
            float back = balance.RespawnDistance;
            Vector3 pp = player.transform.position;

            foreach (var s in spots)
            {
                if (!s.waiting)
                {
                    if (s.item != null && Vector3.Distance(pp, s.item.transform.position) <= pick) Take(s);
                }
                else if (Vector3.Distance(pp, s.ground) >= back) Fill(s);
            }
        }

        /// Rolls the item type for this spot and puts it there.
        void Fill(Spot s)
        {
            if (!Pick(out var row, out var prefab)) return;
            Vector3 up = planet.UpAt(s.ground);
            if (s.item != null) Destroy(s.item);
            s.item = Instantiate(prefab, s.ground + up * balance.HoverHeight, Quaternion.FromToRotation(Vector3.up, up), transform);
            var look = s.item.GetComponent<AppleVisual>();
            if (look != null) look.Init(balance);
            s.row = row;
            s.waiting = false;
        }

        bool Pick(out ItemBalance.TypeRow row, out GameObject prefab)
        {
            float chance = PlayerProgress.Active ? PlayerProgress.Info.rareItemChance : balance.RareChance;
            bool wantRare = Random.value < chance;
            if (PickFrom(wantRare, out row, out prefab)) return true;
            return PickFrom(!wantRare, out row, out prefab);   // no prefab for that group: use the other
        }

        bool PickFrom(bool rare, out ItemBalance.TypeRow row, out GameObject prefab)
        {
            row = default;
            prefab = null;
            int seen = 0;
            foreach (var t in balance.Types)
            {
                if (t.rare != rare) continue;
                var p = FindPrefab(t.id);
                if (p == null) continue;
                seen++;
                if (Random.Range(0, seen) == 0) { row = t; prefab = p; }   // uniform pick among matching rows
            }
            return prefab != null;
        }

        GameObject FindPrefab(string id)
        {
            if (prefabs != null)
                foreach (var p in prefabs)
                    if (p.prefab != null && string.Equals(p.id, id, System.StringComparison.OrdinalIgnoreCase)) return p.prefab;
            if (applePrefab != null && string.Equals(id, "apple", System.StringComparison.OrdinalIgnoreCase)) return applePrefab;
            return null;
        }

        void Take(Spot s)
        {
            s.waiting = true;
            Vector3 pos = s.item.transform.position;
            s.item.SetActive(false);
            if (s.row.healRatio > 0f) GameEvents.RaiseHealRequested(s.row.healRatio);
            if (s.row.exp > 0) GameEvents.RaiseExpRequested(s.row.exp);
            GameEvents.RaiseItemEaten(s.row.id, pos);
        }
    }
}
