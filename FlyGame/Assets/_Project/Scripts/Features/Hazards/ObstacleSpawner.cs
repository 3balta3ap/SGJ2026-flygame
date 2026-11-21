using System.Collections.Generic;
using UnityEngine;

namespace FlyJam
{
    /// Spawns enemies around the player: random direction, 35-60 degrees away, aimed near the player.
    /// Which enemy = weighted pick from HazardBalance "HazardTypes" rows. Each row id needs a prefab in 'prefabs'.
    public class ObstacleSpawner : MonoBehaviour
    {
        [System.Serializable]
        public struct TypePrefab
        {
            public string id;
            public RollingObstacle prefab;
        }

        [SerializeField] Planet planet;             // Auto-filled from GameContext if null
        [SerializeField] SphereWalker player;       // Auto-filled from GameContext if null
        [SerializeField] GameSession session;       // Auto-filled from GameContext if null
        [SerializeField] HazardBalance balance;
        [SerializeField] TypePrefab[] prefabs;

        float timer;

        void Start()
        {
            var ctx = GameContext.Current;
            if (ctx != null)
            {
                if (planet == null) planet = ctx.Planet;
                if (player == null) player = ctx.Player;
                if (session == null) session = ctx.Session;
            }
            timer = balance != null ? balance.FirstDelay : 3f;

            if (balance != null)
                foreach (var t in balance.Types)
                    if (t.weight > 0f && FindPrefab(t.id) == null)
                        Debug.LogWarning($"[ObstacleSpawner] no prefab for enemy type '{t.id}' - it will not spawn");
        }

        void Update()
        {
            if (planet == null || player == null || session == null || balance == null) return;
            if (session.Ended || session.IsPaused) return;
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = Mathf.Lerp(balance.IntervalStart, balance.IntervalEnd, session.Normalized);
            if (RollingObstacle.Alive.Count < balance.MaxActive) Spawn();
        }

        void Spawn()
        {
            if (!PickType(out var row, out var prefab)) return;

            Vector3 up = planet.UpAt(player.transform.position);
            Vector3 baseFwd = Vector3.ProjectOnPlane(player.transform.forward, up).normalized;
            Vector3 tangent = Quaternion.AngleAxis(Random.Range(0f, 360f), up) * baseFwd;
            Vector3 axis = Vector3.Cross(up, tangent);
            Vector3 spawnDir = Quaternion.AngleAxis(Random.Range(balance.SpawnAngleMin, balance.SpawnAngleMax), axis) * up;

            Vector3 spawnPos = planet.SurfacePoint(spawnDir);
            Vector3 sUp = planet.UpAt(spawnPos);
            Vector3 toPlayer = Vector3.ProjectOnPlane(player.transform.position - spawnPos, sUp).normalized;
            toPlayer = Quaternion.AngleAxis(Random.Range(-balance.AimJitter, balance.AimJitter), sUp) * toPlayer;

            var o = Instantiate(prefab);
            float speed = player.MoveSpeed * row.speedRatio;
            float travel = planet.Radius * Mathf.PI * balance.TravelRatio;
            o.Launch(planet, spawnPos, toPlayer, speed, travel, row, player.transform);
        }

        bool PickType(out HazardBalance.TypeRow row, out RollingObstacle prefab)
        {
            row = default;
            prefab = null;
            float total = 0f;
            foreach (var t in balance.Types)
                if (t.weight > 0f && FindPrefab(t.id) != null) total += t.weight;
            if (total <= 0f) return false;

            float r = Random.Range(0f, total);
            foreach (var t in balance.Types)
            {
                var p = t.weight > 0f ? FindPrefab(t.id) : null;
                if (p == null) continue;
                row = t;
                prefab = p;
                r -= t.weight;
                if (r <= 0f) break;
            }
            return prefab != null;
        }

        RollingObstacle FindPrefab(string id)
        {
            if (prefabs == null || string.IsNullOrEmpty(id)) return null;
            foreach (var p in prefabs)
                if (p.prefab != null && string.Equals(p.id, id, System.StringComparison.OrdinalIgnoreCase)) return p.prefab;
            return null;
        }
    }
}
