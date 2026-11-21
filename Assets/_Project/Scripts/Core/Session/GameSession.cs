using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FlyJam
{
    /// 한 판의 규칙 중 코어만: 타이머, 일시정지, 종료, 결과 화면 이동.
    /// 승리 조건(구애 등)은 기능이 End(true, 이유)를 호출해서 만든다.
    public class GameSession : MonoBehaviour
    {
        [SerializeField] SphereWalker player;          // 비우면 GameContext에서
        [SerializeField] float timeLimit = 45f;
        [SerializeField] float resultDelay = 2.0f;

        readonly HashSet<object> pausers = new HashSet<object>();

        public float Elapsed { get; private set; }
        public float Remaining => Mathf.Max(0f, timeLimit - Elapsed);
        public float Normalized => Mathf.Clamp01(Elapsed / timeLimit);
        public bool Ended { get; private set; }
        /// 누군가 Pause 중이면 true (구애 연출 등). 타이머·하늘·장애물 생성이 멈춘다.
        public bool IsPaused => pausers.Count > 0;
        public SphereWalker Player => player;

        void Awake()
        {
            if (player == null && GameContext.Current != null) player = GameContext.Current.Player;
        }

        void OnEnable() => GameEvents.PlayerHit += OnPlayerHit;
        void OnDisable() => GameEvents.PlayerHit -= OnPlayerHit;

        void Start()
        {
            Time.timeScale = 1f;
            GameEvents.RaiseRoundStarted();
        }

        void Update()
        {
            if (Ended || IsPaused) return;
            Elapsed += Time.deltaTime;
            if (Elapsed >= timeLimit) End(false, "TimeUp");
        }

        public void Pause(object by)
        {
            if (by != null) pausers.Add(by);
        }

        public void Resume(object by)
        {
            if (by != null) pausers.Remove(by);
        }

        void OnPlayerHit()
        {
            if (Ended || IsPaused) return;
            End(false, "Hit");
        }

        /// 판을 끝낸다. 이유(reason)는 결과 화면 제목에 쓰인다: "Love", "Hit", "TimeUp" 등.
        public void End(bool cleared, string reason)
        {
            if (Ended) return;
            Ended = true;
            pausers.Clear();
            if (player != null)
            {
                player.SetInputEnabled(false);
                if (!cleared) player.SetState(FlyState.Disabled);
            }

            SessionResult.Begin(cleared, Elapsed, reason);
            GameEvents.RaiseRoundEnded(cleared);   // 각 기능이 여기서 자기 통계를 SessionResult에 기록한다
            StartCoroutine(GoToResultAfterDelay());
        }

        IEnumerator GoToResultAfterDelay()
        {
            yield return new WaitForSeconds(resultDelay);
            SceneFlow.GoToResult();
        }
    }
}
