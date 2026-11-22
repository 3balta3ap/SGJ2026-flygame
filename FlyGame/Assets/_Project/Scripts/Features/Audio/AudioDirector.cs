using UnityEngine;
using UnityEngine.SceneManagement;

namespace FlyJam
{
    /// All music and sound effects, set in the inspector only (drag clips in; an empty slot is silent).
    /// - Music: one looping track for title and game, kept playing across scene changes (DontDestroyOnLoad).
    ///   Pause menu: volume x pauseDuck. Courtship success: music fades out, the victory clip plays once;
    ///   the music comes back on the next scene load (retry / title).
    ///   Mash battle (male): battleMusic loops from the start while it lasts (main music muted), and is cut off
    ///   the moment the battle ends; the main music fades back in. Empty battleMusic = no change.
    /// - Effects: one slot per game event (see the field names). Put SYS_Audio in both Title and Game scenes:
    ///   the first one alive stays, later copies remove themselves.
    public class AudioDirector : MonoBehaviour
    {
        [System.Serializable]
        public class Sfx
        {
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1f;
        }

        public static AudioDirector Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;

        [Header("Music")]
        [SerializeField] AudioClip music;
        [Range(0f, 1f)] [SerializeField] float musicVolume = 0.6f;
        [Range(0f, 1f)] [SerializeField] float pauseDuck = 0.5f;
        [SerializeField] float musicFadeSeconds = 1f;
        [SerializeField] AudioClip victory;
        [Range(0f, 1f)] [SerializeField] float victoryVolume = 0.8f;
        [Tooltip("Loops during the mash battle with a male; stops as soon as the battle ends. Empty = keep the main music")]
        [SerializeField] AudioClip battleMusic;
        [Range(0f, 1f)] [SerializeField] float battleVolume = 0.7f;
        [Tooltip("Main music volume during the battle (0 = silent)")]
        [Range(0f, 1f)] [SerializeField] float battleMainDuck = 0f;

        [Header("Effects")]
        [Range(0f, 1f)] [SerializeField] float sfxVolume = 1f;
        [SerializeField] Sfx itemEaten = new Sfx();          // apple / banana (heal)
        [SerializeField] Sfx damage = new Sfx();             // hit by an enemy, mash battle lost
        [SerializeField] Sfx dodgeSuccess = new Sfx();       // 回避!
        [SerializeField] Sfx dash = new Sfx();               // dash pressed
        [SerializeField] Sfx warning = new Sfx();            // danger warning starts
        [SerializeField] Sfx courtshipCorrect = new Sfx();   // fallback clip and volume for accepted notes
        [SerializeField] Sfx courtshipWrong = new Sfx();     // fallback clip and volume for wrong notes
        [SerializeField] Sfx rivalPress = new Sfx();         // each press in the mash battle
        [SerializeField] Sfx rivalWin = new Sfx();           // mash battle won
        [SerializeField] Sfx menuMove = new Sfx();           // menu cursor moved
        [SerializeField] Sfx menuSelect = new Sfx();         // menu item chosen
        [SerializeField] Sfx pauseOpen = new Sfx();          // pause menu opened
        [SerializeField] Sfx levelUp = new Sfx();            // level up
        [SerializeField] Sfx fever = new Sfx();              // fever starts
        [SerializeField] Sfx bubble = new Sfx();             // speech bubble appears
        [SerializeField] Sfx gameOver = new Sfx();           // HP ran out

        [Header("Courtship Singing")]
        [SerializeField] AudioClip singDown;
        [SerializeField] AudioClip singLeft;
        [SerializeField] AudioClip singRight;
        [SerializeField] AudioClip singUp;
        [SerializeField] AudioClip singFailed;

        AudioSource musicSource, sfxSource, jingleSource, battleSource;
        float musicTarget, fadeSpeed;
        bool musicStopped, paused, inBattle;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (transform.parent != null) transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            jingleSource = gameObject.AddComponent<AudioSource>();
            jingleSource.playOnAwake = false;
            battleSource = gameObject.AddComponent<AudioSource>();
            battleSource.loop = true;
            battleSource.playOnAwake = false;
            StartMusic();
        }

        void OnEnable()
        {
            if (Instance != null && Instance != this) return;
            SceneManager.sceneLoaded += OnSceneLoaded;
            GameEvents.ItemEaten += OnItem;
            GameEvents.PlayerHitBy += OnHitBy;
            GameEvents.DamageRequested += OnDamageRequested;
            GameEvents.DodgeSucceeded += OnDodgeSucceeded;
            GameEvents.PlayerDodged += OnDash;
            GameEvents.WarningChanged += OnWarning;
            GameEvents.CourtshipInput += OnCourtInput;
            GameEvents.CourtshipNote += OnCourtNote;
            GameEvents.CourtshipFinished += OnCourtFinished;
            GameEvents.RivalPressed += OnRivalPressed;
            GameEvents.RivalBattleEnded += OnRivalEnded;
            GameEvents.RivalBattleStarted += OnRivalStarted;
            GameEvents.MenuMoved += OnMenuMoved;
            GameEvents.MenuSelected += OnMenuSelected;
            GameEvents.GamePaused += OnPaused;
            GameEvents.LevelChanged += OnLevel;
            GameEvents.FeverChanged += OnFever;
            GameEvents.BubbleRequested += OnBubble;
            GameEvents.PlayerDied += OnDied;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            GameEvents.ItemEaten -= OnItem;
            GameEvents.PlayerHitBy -= OnHitBy;
            GameEvents.DamageRequested -= OnDamageRequested;
            GameEvents.DodgeSucceeded -= OnDodgeSucceeded;
            GameEvents.PlayerDodged -= OnDash;
            GameEvents.WarningChanged -= OnWarning;
            GameEvents.CourtshipInput -= OnCourtInput;
            GameEvents.CourtshipNote -= OnCourtNote;
            GameEvents.CourtshipFinished -= OnCourtFinished;
            GameEvents.RivalPressed -= OnRivalPressed;
            GameEvents.RivalBattleEnded -= OnRivalEnded;
            GameEvents.RivalBattleStarted -= OnRivalStarted;
            GameEvents.MenuMoved -= OnMenuMoved;
            GameEvents.MenuSelected -= OnMenuSelected;
            GameEvents.GamePaused -= OnPaused;
            GameEvents.LevelChanged -= OnLevel;
            GameEvents.FeverChanged -= OnFever;
            GameEvents.BubbleRequested -= OnBubble;
            GameEvents.PlayerDied -= OnDied;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ---------- music ----------

        void StartMusic()
        {
            musicStopped = false;
            paused = false;
            if (music == null || musicSource == null) return;
            if (musicSource.clip != music) musicSource.clip = music;
            if (!musicSource.isPlaying)
            {
                musicSource.volume = 0f;
                musicSource.Play();
            }
            musicTarget = musicVolume;
            fadeSpeed = musicVolume / Mathf.Max(0.05f, musicFadeSeconds);
        }

        void OnSceneLoaded(Scene s, LoadSceneMode mode)
        {
            if (jingleSource != null) jingleSource.Stop();
            StopBattleMusic();
            StartMusic();
        }

        void OnPaused(bool on)
        {
            paused = on;
            if (on) Play(pauseOpen);
            if (inBattle && battleSource != null) battleSource.volume = battleVolume * (on ? pauseDuck : 1f);
            if (musicStopped) return;
            musicTarget = MainTarget();
            fadeSpeed = musicVolume * 4f;
        }

        void Update()
        {
            if (musicSource == null) return;
            musicSource.volume = Mathf.MoveTowards(musicSource.volume, musicTarget, fadeSpeed * Time.unscaledDeltaTime);
            if (musicStopped && musicSource.isPlaying && musicSource.volume <= 0.001f) musicSource.Stop();
        }

        float MainTarget() => musicVolume * (paused ? pauseDuck : 1f) * (inBattle ? battleMainDuck : 1f);

        void OnRivalStarted()
        {
            if (battleMusic == null || battleSource == null) return;
            inBattle = true;
            battleSource.clip = battleMusic;
            battleSource.volume = battleVolume * (paused ? pauseDuck : 1f);
            battleSource.time = 0f;
            battleSource.Play();
            if (musicStopped) return;
            musicTarget = MainTarget();
            fadeSpeed = musicVolume * 6f;   // main music gets out of the way fast
        }

        /// Cuts the battle music off at once (battle over, scene change, round end) and brings the main music back.
        void StopBattleMusic()
        {
            if (battleSource != null && battleSource.isPlaying) battleSource.Stop();
            if (!inBattle) return;
            inBattle = false;
            if (musicStopped) return;
            musicTarget = MainTarget();
            fadeSpeed = musicVolume / Mathf.Max(0.05f, musicFadeSeconds);
        }

        void OnCourtFinished(bool success, Vector3 pos)
        {
            if (!success) return;
            StopBattleMusic();
            musicStopped = true;
            musicTarget = 0f;
            fadeSpeed = musicVolume / Mathf.Max(0.05f, musicFadeSeconds);
            if (victory != null && jingleSource != null) jingleSource.PlayOneShot(victory, victoryVolume);
        }

        // ---------- effects ----------

        void Play(Sfx s)
        {
            if (s != null) Play(s.clip, s.volume);
        }

        void Play(AudioClip clip, float volume)
        {
            if (clip != null && sfxSource != null) sfxSource.PlayOneShot(clip, volume * sfxVolume);
        }

        void OnItem(string id, Vector3 pos) => Play(itemEaten);

        void OnHitBy(IObstacle src)
        {
            // The male's touch starts the mash battle and does no damage: no damage sound
            if (src is IDamageSource ds && ds.DamageRatio == 0f) return;
            Play(damage);
        }

        void OnDamageRequested(float ratio, string reason) => Play(damage);
        void OnDodgeSucceeded() => Play(dodgeSuccess);
        void OnDash(Vector3 pos, Vector3 dir) => Play(dash);

        IMovingObstacle lastWarn;
        void OnWarning(IMovingObstacle t)
        {
            if (t != null && lastWarn == null) Play(warning);   // only when a warning starts, never every frame
            lastWarn = t;
        }

        void OnCourtInput(bool correct)
        {
            if (correct) return;
            AudioClip clip = singFailed != null ? singFailed : courtshipWrong?.clip;
            Play(clip, courtshipWrong?.volume ?? 1f);
        }

        void OnCourtNote(FaceButton button) => Play(CourtshipClip(button), courtshipCorrect?.volume ?? 1f);

        AudioClip CourtshipClip(FaceButton button)
        {
            AudioClip clip = button switch
            {
                FaceButton.South => singDown,
                FaceButton.West => singLeft,
                FaceButton.East => singRight,
                FaceButton.North => singUp,
                _ => null
            };
            return clip != null ? clip : courtshipCorrect?.clip;
        }
        void OnRivalPressed(int count, int target) => Play(rivalPress);
        void OnRivalEnded(bool won)
        {
            StopBattleMusic();
            if (won) Play(rivalWin);
        }
        void OnMenuMoved() => Play(menuMove);
        void OnMenuSelected() => Play(menuSelect);
        void OnLevel(int level, int previous) { if (level > previous) Play(levelUp); }
        void OnFever(bool on) { if (on) Play(fever); }
        void OnBubble(string key, int priority) => Play(bubble);
        void OnDied(string reason)
        {
            StopBattleMusic();
            Play(gameOver);
        }
    }
}
