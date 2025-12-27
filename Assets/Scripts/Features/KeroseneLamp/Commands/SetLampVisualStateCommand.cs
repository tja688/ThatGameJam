using QFramework;
using ThatGameJam.Features.KeroseneLamp.Events;
using ThatGameJam.Features.KeroseneLamp.Models;

namespace ThatGameJam.Features.KeroseneLamp.Commands
{
    public class SetLampVisualStateCommand : AbstractCommand
    {
        private readonly int _lampId;
        private readonly bool _visualEnabled;

        public SetLampVisualStateCommand(int lampId, bool visualEnabled)
        {
            _lampId = lampId;
            _visualEnabled = visualEnabled;
        }

        protected override void OnExecute()
        {
            var model = (KeroseneLampModel)this.GetModel<IKeroseneLampModel>();
            if (!model.TryGetLamp(_lampId, out var record))
            {
                return;
            }

            if (record.Info.VisualEnabled == _visualEnabled)
            {
                return;
            }

            model.SetLampVisualEnabled(_lampId, _visualEnabled);
            this.SendEvent(new LampVisualStateChangedEvent
            {
                LampId = _lampId,
                VisualEnabled = _visualEnabled
            });
        }
    }
}
