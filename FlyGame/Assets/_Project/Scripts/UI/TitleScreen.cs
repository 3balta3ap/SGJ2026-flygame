using UnityEngine;
using UnityEngine.EventSystems;

namespace FlyJam
{
    /// Title (S01, A06). Buttons: あそぶ (StartGame) and the language button (ToggleLanguage), driven by MenuNavigator.
    public class TitleScreen : MonoBehaviour
    {
        void Start() => TimeControl.ResetAll();

        public void StartGame() => SceneFlow.GoToGame();

        public void ToggleLanguage()
        {
            Loc.Toggle();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
