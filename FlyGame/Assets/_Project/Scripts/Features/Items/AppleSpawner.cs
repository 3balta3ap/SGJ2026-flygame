using System.Collections.Generic;
using UnityEngine;

namespace FlyJam
{
    /// Red apples at fixed points (C02, A02).
    /// Each child transform in 'points' is one apple spot (only its direction from the planet centre matters; it is snapped to the surface).
    /// Touch -> +HP (HealRequested), apple disappears. It comes back at the same spot once the player is respawnDistance away from it.
    /// Put the first point near the start so new players heal before meeting danger (A02).
    public class AppleSpawner : MonoBehaviour
    {
        [SerializeField] ItemBalance balance;
        [SerializeField] GameObject applePrefab;
        [SerializeField] Transform[] points;
        [SerializeField] Planet planet;             // Auto-filled from GameContext if null
        [SerializeField] SphereWalker player;       // Auto-filled from GameContext if null
        [SerializeField] GameSession session;       // Auto-filled from GameContext if null

        class Spot
        {
            public Vector3 ground;
            public GameObject apple;
            public bool waiting;
        }

        readonly List<Spot> spots = new List<Spot>();

        public int ActiveCount
        {
            get
            {
                int n = 0;
                foreach (var s in spots) if (s.apple != null && s.apple.activeSelf) n++;
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
            if (planet == null || applePrefab == null || points == null) return;

            foreach (var p in points)
            {
                if (p == null) continue;
                Vector3 dir = p.position - planet.transform.position;
                if (dir.sqrMagnitude < 1e-6f) continue;
                Vector3 ground = planet.SurfacePoint(dir.normalized);
                Vector3 up = planet.UpAt(ground);
                float hover = balance != null ? balance.HoverHeight : 0.6f;
                var apple = Instantiate(applePrefab, ground + up * hover, Quaternion.FromToRotation(Vector3.up, up), transform);
                var look = apple.GetComponent<AppleVisual>();
                if (look != null) look.Init(balance);
                spots.Add(new Spot { ground = ground, apple = apple });
            }
        }

        void Update()
        {
            if (player == null || session == null || session.Ended) return;
            if (session.IsPaused || player.State == FlyState.Disabled) return;

            float pick = balance != null ? balance.PickupRadius : 1f;
            float back = balance != null ? balance.RespawnDistance : 20f;
            Vector3 pp = player.transform.position;

            foreach (var s in spots)
            {
                if (s.apple == null) continue;
                if (!s.waiting)
                {
                    if (Vector3.Distance(pp, s.apple.transform.position) <= pick) Take(s);
                }
                else if (Vector3.Distance(pp, s.ground) >= back)
                {
                    s.waiting = false;
                    s.apple.SetActive(true);
                }
            }
        }

        void Take(Spot s)
        {
            s.waiting = true;
            s.apple.SetActive(false);
            GameEvents.RaiseHealRequested(balance != null ? balance.HealRatio : 0.2f);
            GameEvents.RaiseAppleEaten(s.apple.transform.position);
        }
    }
}
