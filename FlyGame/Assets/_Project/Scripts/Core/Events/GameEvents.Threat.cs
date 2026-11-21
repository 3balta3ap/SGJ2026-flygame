using System;

namespace FlyJam
{
    public static partial class GameEvents
    {
        public static event Action<IMovingObstacle> WarningChanged;   // the enemy you are warned about; null = warning over
        public static event Action DodgeSucceeded;                    // dodged during a warning and the enemy passed without touching

        public static void RaiseWarningChanged(IMovingObstacle target) => WarningChanged?.Invoke(target);
        public static void RaiseDodgeSucceeded() => DodgeSucceeded?.Invoke();
    }
}
