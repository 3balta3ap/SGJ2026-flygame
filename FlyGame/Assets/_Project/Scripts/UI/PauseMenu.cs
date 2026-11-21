using UnityEngine;

namespace FlyJam
{
    /// Pause (S07, P06): Start / + / Esc opens the menu. Everything stops (time scale 0 and session pause):
    /// HP, clock, position, enemies and the remaining time of a courtship or mash battle.
    /// Buttons: つづける (Resume), タイトルに戻る (ToTitle). Disabled after the round ends.
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] GameSession session;        // Auto-filled from GameContext if null
        [SerializeField] GameObject panel;

        public bool IsOpen { get; private set; }

        void Start()
        {
            if (session == null && GameContext.Current != null) session = GameContext.Current.Session;
            if (panel != null) panel.SetActive(false);
        }

        void Update()
        {
            if (session == null) return;
            if (session.Ended)
            {
                if (IsOpen) Resume();
                return;
            }
            if (!InputCompat.PausePressed()) return;
            if (IsOpen) Resume();
            else Open();
        }

        void Open()
        {
            IsOpen = true;
            if (panel != null) panel.SetActive(true);
            session.Pause(this);
            TimeControl.SetPaused(true);
            GameEvents.RaiseGamePaused(true);
        }

        public void Resume()
        {
            if (!IsOpen) return;
            IsOpen = false;
            if (panel != null) panel.SetActive(false);
            if (session != null) session.Resume(this);
            TimeControl.SetPaused(false);
            GameEvents.RaiseGamePaused(false);
        }

        public void ToTitle() => SceneFlow.GoToTitle();

        void OnDisable()
        {
            if (IsOpen) TimeControl.SetPaused(false);
        }
    }
}
