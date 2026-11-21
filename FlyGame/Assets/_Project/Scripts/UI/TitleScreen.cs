using UnityEngine;
using UnityEngine.EventSystems;

namespace FlyJam
{
    public class TitleScreen : MonoBehaviour
    {
        void Start() => Time.timeScale = 1f;

        void Update()
        {
            if (InputCompat.ConfirmPressed()) StartGame();
        }

        public void StartGame() => SceneFlow.GoToGame();

        /// Language button OnClick. Deselects the button so Space/Enter only starts the game.
        public void ToggleLanguage()
        {
            Loc.Toggle();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
