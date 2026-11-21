using System.Collections.Generic;
using UnityEngine;

namespace FlyJam
{
    /// Player HP (S02-S04). Starts at startHp, drains over time, drops on contact, refills from apples.
    /// Also sets the player's speed: SpeedMultiplier = max(minSpeedRatio, HP / maxHP) (S03).
    /// HP 0 -> GameEvents.PlayerDied("Hit" or "Starved") once. Frozen while the session is paused or ended.
    public class PlayerHealth : MonoBehaviour
    {
        public const string ReasonHit = "Hit";
        public const string ReasonStarved = "Starved";

        [SerializeField] HealthBalance balance;
        [SerializeField] SphereWalker player;   // Auto-filled from GameContext if null
        [SerializeField] GameSession session;   // Auto-filled from GameContext if null

        public float Current { get; private set; }
        public float Max => balance != null ? balance.MaxHp : 10f;
        public float Ratio => Max > 0f ? Current / Max : 0f;
        public bool IsDead { get; private set; }

        readonly List<Renderer> blinkRenderers = new List<Renderer>();
        float invulnerableLeft;
        bool hidden;

        void Awake()
        {
            var ctx = GameContext.Current;
            if (player == null && ctx != null) player = ctx.Player;
            if (session == null && ctx != null) session = ctx.Session;
            if (player != null)
                foreach (var r in player.GetComponentsInChildren<Renderer>())
                    if (r.enabled) blinkRenderers.Add(r);
        }

        void OnEnable()
        {
            GameEvents.PlayerHitBy += OnHitBy;
            GameEvents.HealRequested += OnHeal;
            GameEvents.DamageRequested += OnDamageRequested;
        }

        void OnDisable()
        {
            GameEvents.PlayerHitBy -= OnHitBy;
            GameEvents.HealRequested -= OnHeal;
            GameEvents.DamageRequested -= OnDamageRequested;
            EndInvulnerable();
        }

        void Start()
        {
            Current = balance != null ? balance.StartHp : 5f;
            GameEvents.RaiseHealthChanged(Current, Max);
            ApplySpeed();
        }

        void Update()
        {
            if (IsDead || session == null || session.Ended) return;
            if (session.IsPaused) { SetHidden(false); return; }

            if (invulnerableLeft > 0f)
            {
                invulnerableLeft -= Time.deltaTime;
                if (invulnerableLeft <= 0f) EndInvulnerable();
                else
                {
                    float step = balance != null ? balance.BlinkInterval : 0.08f;
                    SetHidden(Mathf.Repeat(invulnerableLeft, step * 2f) < step);
                }
            }

            float drain = balance != null ? balance.DrainPerSecond : 0f;
            if (drain > 0f) TakeDamage(drain * Time.deltaTime, ReasonStarved);
            ApplySpeed();
        }

        void ApplySpeed()
        {
            if (player == null) return;
            float min = balance != null ? balance.MinSpeedRatio : 0.3f;
            player.SpeedMultiplier = Mathf.Max(min, Ratio);
        }

        void OnHitBy(IObstacle source)
        {
            if (IsDead || invulnerableLeft > 0f) return;
            if (session != null && (session.Ended || session.IsPaused)) return;

            float fallback = balance != null ? balance.ObstacleDamageRatio : 0.2f;
            float ratio = source is IDamageSource ds && ds.DamageRatio >= 0f ? ds.DamageRatio : fallback;
            TakeDamage(ratio * Max, ReasonHit);
            if (IsDead) return;

            // Also after a 0-damage contact (male -> mash battle): avoids an instant second contact afterwards
            invulnerableLeft = balance != null ? balance.InvulnerableTime : 1.5f;
            if (invulnerableLeft > 0f && player != null) player.SetInvulnerable(this, true);
        }

        void OnHeal(float ratio)
        {
            if (IsDead || ratio <= 0f) return;
            Current = Mathf.Min(Max, Current + ratio * Max);
            GameEvents.RaiseHealthChanged(Current, Max);
            ApplySpeed();
        }

        /// Not blocked by pause: the mash battle requests damage right after it resumes the session.
        void OnDamageRequested(float ratio, string reason)
        {
            if (IsDead || ratio <= 0f) return;
            TakeDamage(ratio * Max, string.IsNullOrEmpty(reason) ? ReasonHit : reason);
        }

        void TakeDamage(float amount, string reason)
        {
            if (IsDead || amount <= 0f) return;
            Current = Mathf.Max(0f, Current - amount);
            GameEvents.RaiseHealthChanged(Current, Max);
            ApplySpeed();
            if (Current > 0f) return;

            IsDead = true;
            EndInvulnerable();
            GameEvents.RaisePlayerDied(reason);
        }

        void EndInvulnerable()
        {
            invulnerableLeft = 0f;
            SetHidden(false);
            if (player != null) player.SetInvulnerable(this, false);
        }

        void SetHidden(bool hide)
        {
            if (hidden == hide) return;
            hidden = hide;
            foreach (var r in blinkRenderers)
                if (r != null) r.enabled = !hide;
        }
    }
}
