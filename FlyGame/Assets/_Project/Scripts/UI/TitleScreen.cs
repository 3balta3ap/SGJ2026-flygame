using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FlyJam
{
    /// Title (S01, A06). Buttons: あそぶ (StartGame) and the language button (ToggleLanguage), driven by MenuNavigator.
    /// Title image: drag a sprite into 'titleSprite' (this component). Set = the image shows and the text title hides.
    /// Empty = the text title (title.name) as before.
    public class TitleScreen : MonoBehaviour
    {
        [Tooltip("Title logo. Empty = text title")]
        [SerializeField] Sprite titleSprite;
        [SerializeField] Image titleImage;          // Canvas/TitleImage (Preserve Aspect on)
        [SerializeField] Graphic titleText;         // Canvas/TitleText (its TMP component)

        void Start()
        {
            TimeControl.ResetAll();
            ApplyTitle();
        }

        void OnValidate() => ApplyTitle();   // preview in the editor when the sprite is dropped in

        void ApplyTitle()
        {
            bool hasImage = titleSprite != null;
            if (titleImage != null)
            {
                titleImage.sprite = titleSprite;
                titleImage.preserveAspect = true;
                titleImage.enabled = hasImage;
            }
            if (titleText != null) titleText.enabled = !hasImage;
        }

        public void StartGame() => SceneFlow.GoToGame();

        public void ToggleLanguage()
        {
            Loc.Toggle();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
