using QFramework;

namespace ThatGameJam.Features.RunFailReset.Systems
{
    public interface IRunFailResetSystem : ISystem, ICanSendCommand
    {
        void RequestResetFromFail();
        void RequestResetFromTest();
    }
}
