namespace FlyJam
{
    public interface ISenseMode
    {
        void Enter();
        void Exit();
        void Tick(float deltaTime);
    }
}
