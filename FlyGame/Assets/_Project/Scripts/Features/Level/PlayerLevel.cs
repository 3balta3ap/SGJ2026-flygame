using UnityEngine;

namespace FlyJam
{
    /// Player level and EXP. EXP from items (ExpRequested) and successful dodges (LevelBalance.dodgeExp).
    /// Each level (LevelTable row) sets: top speed and turn rate (SphereWalker overrides), max HP (MaxHpRequested, the new part
    /// comes filled), rare item chance and courtship difficulty (read by other features through PlayerProgress).
    /// Level-up -> fever: invulnerable, flying (floats and swings up/down, wings flap), speed x feverSpeedMul,
    /// no warnings and no courtship start, for feverSeconds. Paused with the session.
    public class PlayerLevel : MonoBehaviour
    {
        public const string StatKey = "level";

        [SerializeField] LevelBalance balance;
        [SerializeField] SphereWalker player;     // Auto-filled from GameContext if null
        [SerializeField] GameSession session;     // Auto-filled from GameContext if null

        public int Level { get; private set; } = 1;
        public int Exp { get; private set; }
        public bool InFever => feverLeft > 0f;

        LevelInfo info;
        float feverLeft, feverTime;
        VisualOffset offset;
        FlyVisual visual;

        void OnEnable()
        {
            GameEvents.ExpRequested += AddExp;
            GameEvents.DodgeSucceeded += OnDodgeSucceeded;
            GameEvents.RoundEnded += OnRoundEnded;
        }

        void OnDisable()
        {
            GameEvents.ExpRequested -= AddExp;
            GameEvents.DodgeSucceeded -= OnDodgeSucceeded;
            GameEvents.RoundEnded -= OnRoundEnded;
            EndFever();
            PlayerProgress.Clear();
        }

        void Start()
        {
            var ctx = GameContext.Current;
            if (ctx != null)
            {
                if (player == null) player = ctx.Player;
                if (session == null) session = ctx.Session;
            }
            if (player != null)
            {
                offset = player.GetComponent<VisualOffset>();
                visual = player.GetComponentInChildren<FlyVisual>();
            }
            Level = 1;
            Exp = 0;
            Apply(true);
            GameEvents.RaiseLevelChanged(Level, Level);
        }

        void Apply(bool roundStart)
        {
            info = balance != null ? balance.Row(Level) : new LevelInfo { level = Level, maxSpeed = 3.6f, turnRate = 70f, maxHp = 10f, courtLength = 4 };
            PlayerProgress.Set(info);
            ApplySpeed();
            if (info.maxHp > 0f) GameEvents.RaiseMaxHpRequested(info.maxHp, !roundStart);
            GameEvents.RaiseExpChanged(Exp, info.expToNext, Level);
        }

        void ApplySpeed()
        {
            if (player == null) return;
            float mul = InFever && balance != null ? balance.FeverSpeedMul : 1f;
            player.MaxSpeedOverride = Mathf.Max(0.1f, info.maxSpeed) * mul;
            player.TurnRateOverride = Mathf.Max(1f, info.turnRate);
        }

        void OnDodgeSucceeded()
        {
            if (balance != null && balance.DodgeExp > 0) AddExp(balance.DodgeExp);
        }

        void AddExp(int amount)
        {
            if (amount <= 0 || (session != null && session.Ended)) return;
            Exp += amount;
            bool up = false;
            while (info.expToNext > 0 && Exp >= info.expToNext)
            {
                Exp -= info.expToNext;
                int prev = Level;
                Level++;
                Apply(false);
                GameEvents.RaiseLevelChanged(Level, prev);
                up = true;
            }
            if (info.expToNext <= 0) Exp = 0;   // max level: the bar stays full
            GameEvents.RaiseExpChanged(Exp, info.expToNext, Level);
            if (up) StartFever();
        }

        void StartFever()
        {
            if (balance == null || balance.FeverSeconds <= 0f) return;
            bool was = InFever;
            feverLeft = balance.FeverSeconds;
            if (!was) feverTime = 0f;
            PlayerProgress.SetFever(true);
            if (player != null) player.SetInvulnerable(this, true);
            if (visual != null) visual.Flying = true;
            ApplySpeed();
            if (!was) GameEvents.RaiseFeverChanged(true);
        }

        void EndFever()
        {
            bool was = feverLeft > 0f || PlayerProgress.Fever;
            feverLeft = 0f;
            PlayerProgress.SetFever(false);
            if (player != null) player.SetInvulnerable(this, false);
            if (visual != null) visual.Flying = false;
            if (offset != null) offset.Set("fever", 0f);
            ApplySpeed();
            if (was) GameEvents.RaiseFeverChanged(false);
        }

        void Update()
        {
            if (!InFever) return;
            if (session != null && session.IsPaused) return;   // keeps floating where it is

            float dt = Time.deltaTime;
            feverLeft -= dt;
            feverTime += dt;
            float ease = balance.FeverEaseSeconds;
            float k = Mathf.Min(Mathf.Clamp01(feverTime / ease), Mathf.Clamp01(Mathf.Max(0f, feverLeft) / ease));
            k = Mathf.SmoothStep(0f, 1f, k);
            float h = (balance.FeverLift + Mathf.Sin(feverTime * balance.FeverBobSpeed) * balance.FeverBobHeight) * k;
            if (offset != null) offset.Set("fever", h);
            if (feverLeft <= 0f) EndFever();
        }

        void OnRoundEnded(bool cleared)
        {
            SessionResult.SetStat(StatKey, Level);
            EndFever();
        }
    }
}
