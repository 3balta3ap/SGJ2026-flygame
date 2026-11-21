using System;

namespace FlyJam
{
    public static partial class GameEvents
    {
        public static event Action<IMovingObstacle> WarningChanged;   // the enemy you are warned about; null = warning over
        public static event Action DodgeSucceeded;                    // dodged during a warning and the enemy passed (or stopped) without touching
        public static event Action<string> EnemyAppeared;              // enemy type id, raised once per spawned enemy

        public static void RaiseWarningChanged(IMovingObstacle target) => WarningChanged?.Invoke(target);
        public static void RaiseDodgeSucceeded() => DodgeSucceeded?.Invoke();
        public static void RaiseEnemyAppeared(string id) => EnemyAppeared?.Invoke(id);
    }
}
