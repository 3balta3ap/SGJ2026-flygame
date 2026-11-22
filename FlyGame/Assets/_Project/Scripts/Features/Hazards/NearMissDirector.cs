using TMPro;
using UnityEngine;

namespace FlyJam
{
    /// Danger warning and dodge result (E01, E02, A04).
    /// Warning = the nearest enemy that is heading at the player within warnRadius. While warned:
    /// red frame on the screen edges, "Ⓐボタンで回避！" prompt, slow motion, and dodging is allowed (DodgeAbility listens to WarningChanged).
    /// Success = after a dash, that enemy leaves the danger range (or freezes: stopOnDodge) without touching the player -> "回避!" once.
    /// No warning during the level-up fever (the player is invulnerable then).
    /// (Class name kept from the prototype so the SYS_Hazards prefab link survives.)
    public class NearMissDirector : MonoBehaviour
    {
        public const string PerfectKey = "dodge.perfect";   // result stat: number of successful dodges

        [SerializeField] HazardBalance balance;
        [SerializeField] SphereWalker player;        // Auto-filled from GameContext if null
        [SerializeField] GameSession session;        // Auto-filled from GameContext if null
        [SerializeField] GameObject promptUI;        // top centre prompt root
        [SerializeField] TMP_Text promptText;        // "Ⓐボタンで回避！" (set from code: button name follows the device)
        [SerializeField] GameObject frameUI;         // red frame on the four screen edges
        [SerializeField] TMP_Text perfectText;       // "回避!" after a successful dodge
        [SerializeField] float successShowSeconds = 0.8f;

        RollingObstacle target, dodged;
        float dodgedTimer, perfectUntil;

        public int PerfectCount { get; private set; }
        public RollingObstacle Target => target;

        void OnEnable()
        {
            GameEvents.PlayerDodged += OnDodged;
            GameEvents.PlayerHitBy += OnHitBy;
            GameEvents.RoundEnded += OnRoundEnded;
        }

        void OnDisable()
        {
            GameEvents.PlayerDodged -= OnDodged;
            GameEvents.PlayerHitBy -= OnHitBy;
            GameEvents.RoundEnded -= OnRoundEnded;
            SetTarget(null);
            TimeControl.SetSlow(this, 1f);
        }

        void Start()
        {
            var ctx = GameContext.Current;
            if (ctx != null)
            {
                if (player == null) player = ctx.Player;
                if (session == null) session = ctx.Session;
            }
            if (promptUI != null) promptUI.SetActive(false);
            if (frameUI != null) frameUI.SetActive(false);
            if (perfectText != null) perfectText.enabled = false;
        }

        void Update()
        {
            bool live = player != null && session != null && balance != null && !session.Ended && !session.IsPaused
                        && player.InputEnabled && player.State != FlyState.Disabled && !PlayerProgress.Fever;

            var next = live ? FindThreat() : null;
            if (next != target) SetTarget(next);
            if (target != null && promptText != null)
                promptText.text = Loc.Format("hazard.prompt", InputCompat.ActionLabel);

            if (live) UpdateSuccess();
            if (perfectText != null) perfectText.enabled = Time.unscaledTime < perfectUntil;
        }

        RollingObstacle FindThreat()
        {
            RollingObstacle best = null;
            float bestDist = balance.WarnRadius;
            var all = RollingObstacle.Active;
            for (int i = 0; i < all.Count; i++)
            {
                var o = all[i];
                if (o == null || !o.Warns) continue;
                float d = Vector3.Distance(player.transform.position, o.Position);
                if (d > bestDist || !Approaching(o)) continue;
                best = o;
                bestDist = d;
            }
            return best;
        }

        bool Approaching(RollingObstacle o)
        {
            Vector3 to = player.transform.position - o.Position;
            return to.sqrMagnitude > 1e-6f && Vector3.Dot(o.Direction, to.normalized) > balance.WarnApproachDot;
        }

        bool InDanger(RollingObstacle o)
        {
            if (o == null) return false;
            bool activeNow = false;
            var all = RollingObstacle.Active;
            for (int i = 0; i < all.Count; i++) if (all[i] == o) { activeNow = true; break; }
            return activeNow && Vector3.Distance(player.transform.position, o.Position) <= balance.WarnRadius && Approaching(o);
        }

        void SetTarget(RollingObstacle t)
        {
            target = t;
            GameEvents.RaiseWarningChanged(t);
            if (promptUI != null) promptUI.SetActive(t != null);
            if (frameUI != null) frameUI.SetActive(t != null);
            TimeControl.SetSlow(this, t != null && balance != null ? balance.SlowScale : 1f);
        }

        void OnDodged(Vector3 pos, Vector3 dir)
        {
            // The cached warning may still refer to a stopped or passed threat for this frame.
            // A second press during the same pending approach must not restart its success window.
            if (target == null || target == dodged || !InDanger(target)) return;
            dodged = target;
            dodgedTimer = balance != null ? balance.SuccessTimeout : 3f;
            if (dodged.OnDodged()) Succeed();   // stopOnDodge (male): it freezes, so the dodge counts right away
        }

        void OnHitBy(IObstacle src)
        {
            if (dodged != null && ReferenceEquals(src, dodged)) dodged = null;   // touched it: no success
        }

        void UpdateSuccess()
        {
            if (dodged == null) return;
            dodgedTimer -= Time.deltaTime;
            // Passed (not heading at the player any more) AND clear of its body: a forward dash can go through an enemy,
            // so "behind but still overlapping" must not count yet (E02: never success + hit for the same dash).
            float clear = dodged.HitRadius + 0.6f;
            bool apart = Vector3.Distance(player.transform.position, dodged.Position) > clear;
            if (!InDanger(dodged) && apart) Succeed();
            else if (dodgedTimer <= 0f) dodged = null;
        }

        void Succeed()
        {
            dodged = null;
            PerfectCount++;
            perfectUntil = Time.unscaledTime + successShowSeconds;
            GameEvents.RaiseDodgeSucceeded();
            GameEvents.RaisePerfectDodge();   // existing FX burst
        }

        void OnRoundEnded(bool cleared) => SessionResult.SetStat(PerfectKey, PerfectCount);
    }
}
