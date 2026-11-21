using UnityEngine;

namespace FlyJam
{
    /// Spawns rolling obstacles 35-60 degrees away (usually beyond the horizon) aimed toward the player, faster over time.
    public class ObstacleSpawner : MonoBehaviour
    {
        [SerializeField] Planet planet;             // Auto-filled from GameContext if null
        [SerializeField] SphereWalker player;       // Auto-filled from GameContext if null
        [SerializeField] GameSession session;       // Auto-filled from GameContext if null
        [SerializeField] RollingObstacle prefab;
        [SerializeField] float firstDelay = 3f;
        [SerializeField] Vector2 intervalStartEnd = new Vector2(3.5f, 1.4f);
        [SerializeField] Vector2 speedRange = new Vector2(3f, 5f);
        [SerializeField] Vector2 spawnAngle = new Vector2(35f, 60f);
        [SerializeField] float aimJitter = 15f;
        [SerializeField] int maxActive = 6;

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
            timer = firstDelay;
        }

        void Update()
        {
            if (planet == null || player == null || session == null) return;
            if (session.Ended || session.IsPaused) return;
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = Mathf.Lerp(intervalStartEnd.x, intervalStartEnd.y, session.Normalized);
            if (RollingObstacle.Active.Count < maxActive) Spawn();
        }

        void Spawn()
        {
            Vector3 up = planet.UpAt(player.transform.position);
            Vector3 baseFwd = Vector3.ProjectOnPlane(player.transform.forward, up).normalized;
            Vector3 tangent = Quaternion.AngleAxis(Random.Range(0f, 360f), up) * baseFwd;
            Vector3 axis = Vector3.Cross(up, tangent);
            Vector3 spawnDir = Quaternion.AngleAxis(Random.Range(spawnAngle.x, spawnAngle.y), axis) * up;

            Vector3 spawnPos = planet.SurfacePoint(spawnDir);
            Vector3 sUp = planet.UpAt(spawnPos);
            Vector3 toPlayer = Vector3.ProjectOnPlane(player.transform.position - spawnPos, sUp).normalized;
            toPlayer = Quaternion.AngleAxis(Random.Range(-aimJitter, aimJitter), sUp) * toPlayer;

            var o = Instantiate(prefab);
            o.Launch(planet, spawnPos, toPlayer, Random.Range(speedRange.x, speedRange.y), planet.Radius * Mathf.PI * 0.6f);
        }
    }
}
