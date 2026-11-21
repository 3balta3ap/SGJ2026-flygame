#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine;

namespace FlyJam
{
    /// Face buttons by position, so the same physical button works on every pad. East = the "action" button.
    public enum FaceButton { East, South, North, West }

    /// Which button names to show on screen. Follows the device the player touched last.
    public enum PadLayout { Keyboard, Switch, Xbox, PlayStation }

    /// The only place that reads keyboard / gamepad. Gameplay code asks "what was pressed", never which device.
    /// Action button = right face button (Switch A / Xbox B / PS ○) or Space / Enter.
    /// Pause = Start (Switch +) or Esc. Steering = left stick / D-pad / A-D / arrow keys.
    public static class InputCompat
    {
        const float StickDead = 0.2f;
        const float MenuOn = 0.6f;
        const float MenuOff = 0.3f;

        static PadLayout layout = PadLayout.Keyboard;
        static bool layoutInit;
        static int layoutFrame = -1;
        static int menuFrame = -1;
        static Vector2Int menuMove;
        static bool menuStickHeld;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            layout = PadLayout.Keyboard;
            layoutInit = false;
            layoutFrame = -1;
            menuFrame = -1;
            menuMove = Vector2Int.zero;
            menuStickHeld = false;
        }

        // ───────── Gameplay ─────────

        /// Steering -1 (left) .. +1 (right). Keys give a full turn, the stick is proportional.
        public static float Steer()
        {
            float v = 0f;
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k != null)
                v = (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1f : 0f)
                  - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1f : 0f);
            var p = Gamepad.current;
            if (p != null)
            {
                float s = p.leftStick.ReadValue().x;
                s = Mathf.Abs(s) < StickDead ? 0f : Mathf.Sign(s) * Mathf.InverseLerp(StickDead, 1f, Mathf.Abs(s));
                float d = p.dpad.ReadValue().x;
                if (Mathf.Abs(d) > Mathf.Abs(s)) s = d;
                if (Mathf.Abs(s) > Mathf.Abs(v)) v = s;
            }
#else
            v = Input.GetAxisRaw("Horizontal");
#endif
            return Mathf.Clamp(v, -1f, 1f);
        }

        /// Action button pressed this frame. Rising edge only: holding it down counts once.
        public static bool ActionPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            var p = Gamepad.current;
            return (k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame))
                || (p != null && p.buttonEast.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
#endif
        }

        public static bool PausePressed()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            var p = Gamepad.current;
            return (k != null && k.escapeKey.wasPressedThisFrame) || (p != null && p.startButton.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        /// One of the four face buttons pressed this frame (courtship input).
        /// Keyboard: arrow keys or WASD by position (→/D = East, ↓/S = South, ↑/W = North, ←/A = West).
        public static bool FaceButtonPressed(out FaceButton button)
        {
            button = FaceButton.East;
#if ENABLE_INPUT_SYSTEM
            var p = Gamepad.current;
            if (p != null)
            {
                if (p.buttonEast.wasPressedThisFrame) { button = FaceButton.East; return true; }
                if (p.buttonSouth.wasPressedThisFrame) { button = FaceButton.South; return true; }
                if (p.buttonNorth.wasPressedThisFrame) { button = FaceButton.North; return true; }
                if (p.buttonWest.wasPressedThisFrame) { button = FaceButton.West; return true; }
            }
            var k = Keyboard.current;
            if (k != null)
            {
                if (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame) { button = FaceButton.East; return true; }
                if (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame) { button = FaceButton.South; return true; }
                if (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame) { button = FaceButton.North; return true; }
                if (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame) { button = FaceButton.West; return true; }
            }
#endif
            return false;
        }

        // ───────── Menus ─────────

        /// Menu cursor step this frame: x -1/0/+1, y +1 = up / -1 = down. Same answer for every caller in a frame.
        public static Vector2Int MenuMove()
        {
            if (menuFrame == Time.frameCount) return menuMove;
            menuFrame = Time.frameCount;
            var m = Vector2Int.zero;
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k != null)
            {
                if (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame) m.y = 1;
                else if (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame) m.y = -1;
                else if (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame) m.x = -1;
                else if (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame) m.x = 1;
            }
            var p = Gamepad.current;
            if (p != null && m == Vector2Int.zero)
            {
                if (p.dpad.up.wasPressedThisFrame) m.y = 1;
                else if (p.dpad.down.wasPressedThisFrame) m.y = -1;
                else if (p.dpad.left.wasPressedThisFrame) m.x = -1;
                else if (p.dpad.right.wasPressedThisFrame) m.x = 1;
                else
                {
                    Vector2 s = p.leftStick.ReadValue();
                    if (!menuStickHeld && (Mathf.Abs(s.x) > MenuOn || Mathf.Abs(s.y) > MenuOn))
                    {
                        menuStickHeld = true;
                        if (Mathf.Abs(s.y) >= Mathf.Abs(s.x)) m.y = s.y > 0f ? 1 : -1;
                        else m.x = s.x > 0f ? 1 : -1;
                    }
                    else if (Mathf.Abs(s.x) < MenuOff && Mathf.Abs(s.y) < MenuOff) menuStickHeld = false;
                }
            }
#endif
            menuMove = m;
            return m;
        }

        // ───────── Button names on screen ─────────

        public static PadLayout Layout
        {
            get
            {
                UpdateLayout();
                return layout;
            }
        }

        public static bool UsingPad => Layout != PadLayout.Keyboard;

        /// Name of a face button for the current device (courtship slots).
        public static string Label(FaceButton b)
        {
            switch (Layout)
            {
                case PadLayout.Switch: return b == FaceButton.East ? "A" : b == FaceButton.South ? "B" : b == FaceButton.North ? "X" : "Y";
                case PadLayout.Xbox: return b == FaceButton.East ? "B" : b == FaceButton.South ? "A" : b == FaceButton.North ? "Y" : "X";
                case PadLayout.PlayStation: return b == FaceButton.East ? "○" : b == FaceButton.South ? "×" : b == FaceButton.North ? "△" : "□";
                default: return b == FaceButton.East ? "→" : b == FaceButton.South ? "↓" : b == FaceButton.North ? "↑" : "←";
            }
        }

        /// Name of the action button for prompts ("Ⓐボタンで回避！").
        public static string ActionLabel
        {
            get
            {
                switch (Layout)
                {
                    case PadLayout.Switch: return "Ⓐ";
                    case PadLayout.Xbox: return "Ⓑ";
                    case PadLayout.PlayStation: return "○";
                    default: return "SPACE";
                }
            }
        }

        static void UpdateLayout()
        {
            if (layoutFrame == Time.frameCount) return;
            layoutFrame = Time.frameCount;
#if ENABLE_INPUT_SYSTEM
            var p = Gamepad.current;
            if (!layoutInit)
            {
                layoutInit = true;
                if (p != null) layout = KindOf(p);
            }
            var k = Keyboard.current;
            if (k != null && k.anyKey.wasPressedThisFrame) layout = PadLayout.Keyboard;
            else if (p != null && PadTouched(p)) layout = KindOf(p);
#endif
        }

#if ENABLE_INPUT_SYSTEM
        static bool PadTouched(Gamepad p) =>
            p.buttonEast.wasPressedThisFrame || p.buttonSouth.wasPressedThisFrame ||
            p.buttonNorth.wasPressedThisFrame || p.buttonWest.wasPressedThisFrame ||
            p.startButton.wasPressedThisFrame || p.leftStick.ReadValue().sqrMagnitude > 0.25f ||
            p.dpad.ReadValue().sqrMagnitude > 0.25f;

        static PadLayout KindOf(Gamepad p)
        {
            string n = (p.GetType().Name + " " + p.displayName + " " + p.description.product).ToLowerInvariant();
            if (n.Contains("switch") || n.Contains("pro controller") || n.Contains("joy-con")) return PadLayout.Switch;
            if (n.Contains("dualshock") || n.Contains("dualsense") || n.Contains("playstation") || n.Contains("wireless controller"))
                return PadLayout.PlayStation;
            return PadLayout.Xbox;
        }
#endif

        // ───────── Old names (kept so existing calls still compile) ─────────

        public static Vector2 Move() => new Vector2(Steer(), 0f);
        public static bool ConfirmPressed() => ActionPressed();
        public static bool BackPressed() => PausePressed();
        public static bool DodgePressed() => ActionPressed();

        /// Toggle pixel-art post-processing on/off for comparison (P key).
        public static bool TogglePixelPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            return k != null && k.pKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.P);
#endif
        }
    }
}
