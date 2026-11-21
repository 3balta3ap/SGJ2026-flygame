using System;

namespace FlyJam
{
    public static partial class GameEvents
    {
        public static event Action<IObstacle> RivalEncounter;     // a male touched the player -> mash battle
        public static event Action RivalBattleStarted;
        public static event Action<int, int> RivalPressed;        // count, target
        public static event Action<bool> RivalBattleEnded;        // true = escaped

        public static void RaiseRivalEncounter(IObstacle male) => RivalEncounter?.Invoke(male);
        public static void RaiseRivalBattleStarted() => RivalBattleStarted?.Invoke();
        public static void RaiseRivalPressed(int count, int target) => RivalPressed?.Invoke(count, target);
        public static void RaiseRivalBattleEnded(bool won) => RivalBattleEnded?.Invoke(won);
    }
}
