namespace FlyJam
{
    /// Optional for an IObstacle: HP it takes on contact, as a fraction of max HP (0.2 = 20%).
    /// Return a negative value to use HealthBalance.obstacleDamageRatio instead. 0 = no damage (e.g., the male starts a mash battle).
    public interface IDamageSource
    {
        float DamageRatio { get; }
    }
}
