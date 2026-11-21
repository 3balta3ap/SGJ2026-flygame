using System.Collections.Generic;
using UnityEngine;

namespace FlyJam
{
    /// 다가오는 장애물이 warnDistance 안에 들어온 순간부터 플레이어가 회피 버튼을 누를 때까지의 시간(게임 시간)을 잰다.
    public class ReactionTracker : MonoBehaviour
    {
        [SerializeField] Transform player;
        [SerializeField] float warnDistance = 5f;
        [SerializeField] float expireAfter = 3f;

        readonly Dictionary<RollingObstacle, float> pending = new Dictionary<RollingObstacle, float>();
        readonly HashSet<RollingObstacle> done = new HashSet<RollingObstacle>();
        readonly List<RollingObstacle> scratch = new List<RollingObstacle>();
        float sum;

        public int Count { get; private set; }
        public float Best { get; private set; } = -1f;
        public float Average => Count > 0 ? sum / Count : -1f;

        void OnEnable() => GameEvents.PlayerDodged += OnDodged;
        void OnDisable() => GameEvents.PlayerDodged -= OnDodged;

        void Update()
        {
            var all = RollingObstacle.Active;
            for (int i = 0; i < all.Count; i++)
            {
                var o = all[i];
                if (pending.ContainsKey(o) || done.Contains(o)) continue;
                Vector3 to = player.position - o.Position;
                if (to.magnitude <= warnDistance && Vector3.Dot(o.Direction, to.normalized) > 0.5f)
                    pending[o] = Time.time;
            }

            scratch.Clear();
            foreach (var kv in pending)
                if (kv.Key == null || Time.time - kv.Value > expireAfter) scratch.Add(kv.Key);
            foreach (var k in scratch) { pending.Remove(k); done.Add(k); }
        }

        void OnDodged(Vector3 pos, Vector3 dir)
        {
            if (pending.Count == 0) return;
            float earliest = float.MaxValue;
            foreach (var kv in pending) if (kv.Value < earliest) earliest = kv.Value;

            float rt = Time.time - earliest;
            Count++;
            sum += rt;
            if (Best < 0f || rt < Best) Best = rt;

            foreach (var kv in pending) done.Add(kv.Key);
            pending.Clear();
            GameEvents.RaiseReactionMeasured(rt);
        }
    }
}
