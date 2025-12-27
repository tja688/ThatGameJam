using QFramework;

namespace ThatGameJam.Features.RunFailReset.Systems
{
    public interface IRunFailResetSystem : ISystem
    {
        void RequestResetFromTest();
    }
}
