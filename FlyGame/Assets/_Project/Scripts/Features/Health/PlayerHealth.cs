using System.Collections.Generic;
using UnityEngine;

namespace FlyJam
{
    /// Player HP. Drains over time, drops on obstacle hits, refills on GameEvents.HealRequested (food).
    /// After a hit: invulnerable + blinking for a while. HP 0 -> GameEvents.PlayerDied("Hit" or "Starved").
    /// Frozen while the session is paused (courtship) or ended.
    public class PlayerHealth : MonoBehaviour
    {
        public const string ReasonHit = "Hit";
        public const string ReasonStarved = "Starved";

        [SerializeField] HealthBalance balance;
        [SerializeField] SphereWalker player;   // Auto-filled from GameContext if null
        [SerializeField] GameSession session;   // Auto-filled from GameContext if null

        public float Current { get; private set; }
        public float Max => balance != null ? balance.MaxHp : 100f;
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
        }

        void OnDisable()
        {
            GameEvents.PlayerHitBy -= OnHitBy;
            GameEvents.HealRequested -= OnHeal;
            EndInvulnerable();
        }

        void Start()
        {
            Current = Max;
            GameEvents.RaiseHealthChanged(Current, Max);
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
        }

        void OnHitBy(IObstacle source)
        {
            if (IsDead || invulnerableLeft > 0f) return;
            if (session != null && (session.Ended || session.IsPaused)) return;

            float damage = source is IDamageSource ds ? ds.Damage : (balance != null ? balance.ObstacleDamage : 30f);
            TakeDamage(damage, ReasonHit);
            if (IsDead) return;

            invulnerableLeft = balance != null ? balance.InvulnerableTime : 1.5f;
            if (invulnerableLeft > 0f && player != null) player.SetInvulnerable(this, true);
        }

        void OnHeal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            Current = Mathf.Min(Max, Current + amount);
            GameEvents.RaiseHealthChanged(Current, Max);
        }

        void TakeDamage(float amount, string reason)
        {
            if (IsDead || amount <= 0f) return;
            Current = Mathf.Max(0f, Current - amount);
            GameEvents.RaiseHealthChanged(Current, Max);
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
