using QFramework;
using ThatGameJam.Features.Shared;

namespace ThatGameJam.Features.RunFailReset.Systems
{
    public class RunFailResetSystem : AbstractSystem, IRunFailResetSystem
    {
        protected override void OnInit()
        {
        }

        public void RequestResetFromTest()
        {
            ExecuteReset();
        }

        private void ExecuteReset()
        {
            this.SendEvent(new RunResetEvent());
        }
    }
}
