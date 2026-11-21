namespace FlyJam
{
    public interface IHaptics
    {
        void Pulse(float strength01, float seconds);
    }

    public class NullHaptics : IHaptics
    {
        public void Pulse(float strength01, float seconds) { }
    }
}
