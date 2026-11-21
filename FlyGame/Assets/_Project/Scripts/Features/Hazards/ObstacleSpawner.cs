using UnityEngine;

namespace FlyJam
{
    /// Keeps a fixed population of enemies circling the planet (C04-C08).
    /// After firstDelay, one enemy appears every spawnInterval until each HazardTypes row reaches its 'count'.
    /// Each one starts 50-170 degrees away from the player with a random heading and never disappears.
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
            timer = balance != null ? balance.FirstDelay : 2f;

            if (balance != null)
                foreach (var t in balance.Types)
                    if (t.count > 0 && FindPrefab(t.id) == null)
                        Debug.LogWarning($"[ObstacleSpawner] no prefab for enemy type '{t.id}' - it will not spawn");
        }

        void Update()
        {
            if (planet == null || player == null || session == null || balance == null) return;
            if (session.Ended || session.IsPaused) return;
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = balance.SpawnInterval;

            if (PickShortType(out var row, out var prefab)) Spawn(row, prefab);
        }

        /// The type with the biggest shortage (count - alive). False when every type is full.
        bool PickShortType(out HazardBalance.TypeRow row, out RollingObstacle prefab)
        {
            row = default;
            prefab = null;
            int best = 0;
            foreach (var t in balance.Types)
            {
                var p = FindPrefab(t.id);
                if (p == null || t.count <= 0) continue;
                int shortage = t.count - AliveOf(t.id);
                if (shortage > best)
                {
                    best = shortage;
                    row = t;
                    prefab = p;
                }
            }
            return prefab != null;
        }

        static int AliveOf(string id)
        {
            int n = 0;
            var all = RollingObstacle.Alive;
            for (int i = 0; i < all.Count; i++)
                if (all[i] != null && string.Equals(all[i].TypeId, id, System.StringComparison.OrdinalIgnoreCase)) n++;
            return n;
        }

        void Spawn(HazardBalance.TypeRow row, RollingObstacle prefab)
        {
            Vector3 up = planet.UpAt(player.transform.position);
            Vector3 baseFwd = Vector3.ProjectOnPlane(player.transform.forward, up).normalized;
            Vector3 tangent = Quaternion.AngleAxis(Random.Range(0f, 360f), up) * baseFwd;
            Vector3 axis = Vector3.Cross(up, tangent);
            Vector3 spawnDir = Quaternion.AngleAxis(Random.Range(balance.SpawnAngleMin, balance.SpawnAngleMax), axis) * up;

            Vector3 spawnPos = planet.SurfacePoint(spawnDir);
            Vector3 sUp = planet.UpAt(spawnPos);
            Vector3 anyTangent = Vector3.ProjectOnPlane(Mathf.Abs(sUp.y) < 0.9f ? Vector3.up : Vector3.forward, sUp).normalized;
            Vector3 heading = Quaternion.AngleAxis(Random.Range(0f, 360f), sUp) * anyTangent;

            var o = Instantiate(prefab);
            o.Launch(planet, spawnPos, heading, player.MoveSpeed * row.speedRatio, 0f, row, player.transform);
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
