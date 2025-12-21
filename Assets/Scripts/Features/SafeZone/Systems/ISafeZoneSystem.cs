using QFramework;

namespace ThatGameJam.Features.SafeZone.Systems
{
    public interface ISafeZoneSystem : ISystem, ICanSendCommand
    {
        void Tick(float deltaTime);
    }
}
