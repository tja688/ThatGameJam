using QFramework;
using ThatGameJam.Features.Darkness.Models;
using ThatGameJam.Features.Shared;

namespace ThatGameJam.Features.Darkness.Commands
{
    public class SetInDarknessCommand : AbstractCommand
    {
        private readonly bool _isInDarkness;

        public SetInDarknessCommand(bool isInDarkness)
        {
            _isInDarkness = isInDarkness;
        }

        protected override void OnExecute()
        {
            var model = (DarknessModel)this.GetModel<IDarknessModel>();
            if (model.CurrentValue == _isInDarkness)
            {
                return;
            }

            model.SetIsInDarkness(_isInDarkness);
            this.SendEvent(new DarknessStateChangedEvent
            {
                IsInDarkness = _isInDarkness
            });
        }
    }
}
