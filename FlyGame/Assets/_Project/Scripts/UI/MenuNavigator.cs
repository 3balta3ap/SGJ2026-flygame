using UnityEngine;
using UnityEngine.UI;

namespace FlyJam
{
    /// Menu control for pad and keyboard (P02): left stick / D-pad / arrows / WASD move the cursor,
    /// the action button (Switch A / Xbox B / PS ○ / Space / Enter) runs the selected Button's OnClick.
    /// Works while the game is paused (no time used). Mouse clicks still work through the EventSystem if the scene has one.
    public class MenuNavigator : MonoBehaviour
    {
        [SerializeField] Button[] buttons;
        [SerializeField] float selectedScale = 1.08f;
        [SerializeField] Color selectedColor = new Color(0.12f, 0.29f, 0.33f, 1f);
        [SerializeField] Color normalColor = new Color(0.85f, 0.88f, 0.86f, 1f);
        [SerializeField] Color selectedTextColor = Color.white;
        [SerializeField] Color normalTextColor = new Color(0.2f, 0.2f, 0.2f, 1f);

        int index;
        int openedFrame;

        public int Index => index;

        void OnEnable()
        {
            openedFrame = Time.frameCount;
            index = 0;
            Apply();
        }

        void Update()
        {
            if (buttons == null || buttons.Length == 0) return;
            if (Time.frameCount == openedFrame) return;   // the press that opened this menu does not also choose an item

            var m = InputCompat.MenuMove();
            int step = m.y != 0 ? -m.y : m.x;              // up / left = previous, down / right = next
            if (step != 0)
            {
                index = (index + step + buttons.Length) % buttons.Length;
                Apply();
                GameEvents.RaiseMenuMoved();
            }

            if (InputCompat.ActionPressed() && buttons[index] != null && buttons[index].interactable)
            {
                GameEvents.RaiseMenuSelected();
                buttons[index].onClick.Invoke();
            }
        }

        /// Mouse hover can call this from an EventTrigger if wanted.
        public void Select(int i)
        {
            if (buttons == null || i < 0 || i >= buttons.Length) return;
            index = i;
            Apply();
        }

        void Apply()
        {
            if (buttons == null) return;
            for (int i = 0; i < buttons.Length; i++)
            {
                var b = buttons[i];
                if (b == null) continue;
                bool on = i == index;
                b.transform.localScale = Vector3.one * (on ? selectedScale : 1f);
                if (b.targetGraphic != null) b.targetGraphic.color = on ? selectedColor : normalColor;
                var label = b.GetComponentInChildren<TMPro.TMP_Text>();
                if (label != null) label.color = on ? selectedTextColor : normalTextColor;
            }
        }
    }
}
