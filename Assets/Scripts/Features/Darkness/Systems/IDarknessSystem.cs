using QFramework;

namespace ThatGameJam.Features.Darkness.Systems
{
    public interface IDarknessSystem : ISystem, ICanSendCommand
    {
        void Tick(float deltaTime);
    }
}
