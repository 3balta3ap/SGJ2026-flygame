using UnityEngine;

namespace FlyJam
{
    /// 플레이어에게서 35~60° 떨어진 곳(대개 지평선 너머)에서 플레이어 쪽으로 굴려 보낸다. 시간이 갈수록 자주.
    public class ObstacleSpawner : MonoBehaviour
    {
        [SerializeField] Planet planet;
        [SerializeField] SphereWalker player;
        [SerializeField] GameSession session;
        [SerializeField] RollingObstacle prefab;
        [SerializeField] float firstDelay = 3f;
        [SerializeField] Vector2 intervalStartEnd = new Vector2(3.5f, 1.4f);
        [SerializeField] Vector2 speedRange = new Vector2(3f, 5f);
        [SerializeField] Vector2 spawnAngle = new Vector2(35f, 60f);
        [SerializeField] float aimJitter = 15f;
        [SerializeField] int maxActive = 6;

        float timer;

        void Start() => timer = firstDelay;

        void Update()
        {
            if (session.Ended || session.Courting) return;
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
