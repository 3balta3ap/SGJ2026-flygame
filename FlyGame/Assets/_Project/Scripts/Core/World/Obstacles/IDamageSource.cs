namespace FlyJam
{
    /// Optional for an IObstacle: HP this obstacle takes when it hits the player.
    /// Obstacles without it (e.g., acorns) use HealthBalance.obstacleDamage.
    public interface IDamageSource
    {
        float Damage { get; }
    }
}
