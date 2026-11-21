using System.Collections.Generic;

namespace FlyJam
{
    /// A button order to enter (plain logic, no Unity objects; reusable later for a rival command battle).
    public sealed class ButtonSequence
    {
        public enum Step { Correct, Wrong, Completed }

        readonly FaceButton[] buttons;

        public int Index { get; private set; }
        public int Length => buttons.Length;
        public IReadOnlyList<FaceButton> Buttons => buttons;
        public bool IsComplete => Index >= buttons.Length;
        public FaceButton Next => buttons[IsComplete ? buttons.Length - 1 : Index];

        public ButtonSequence(FaceButton[] order)
        {
            buttons = order != null && order.Length > 0 ? order : new[] { FaceButton.East };
        }

        /// Random order. With 2+ kinds the same button never appears twice in a row (easier to read).
        public static ButtonSequence Random(int length, int kinds)
        {
            if (length < 1) length = 1;
            if (kinds < 1) kinds = 1;
            if (kinds > 4) kinds = 4;
            var order = new FaceButton[length];
            for (int i = 0; i < length; i++)
            {
                FaceButton b;
                do b = (FaceButton)UnityEngine.Random.Range(0, kinds);
                while (kinds > 1 && i > 0 && b == order[i - 1]);
                order[i] = b;
            }
            return new ButtonSequence(order);
        }

        public Step Submit(FaceButton pressed)
        {
            if (IsComplete) return Step.Completed;
            if (pressed != buttons[Index]) return Step.Wrong;
            Index++;
            return IsComplete ? Step.Completed : Step.Correct;
        }
    }
}
