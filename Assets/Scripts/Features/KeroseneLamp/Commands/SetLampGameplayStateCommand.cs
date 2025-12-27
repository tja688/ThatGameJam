using QFramework;
using ThatGameJam.Features.KeroseneLamp.Events;
using ThatGameJam.Features.KeroseneLamp.Models;

namespace ThatGameJam.Features.KeroseneLamp.Commands
{
    public class SetLampGameplayStateCommand : AbstractCommand
    {
        private readonly int _lampId;
        private readonly bool _gameplayEnabled;

        public SetLampGameplayStateCommand(int lampId, bool gameplayEnabled)
        {
            _lampId = lampId;
            _gameplayEnabled = gameplayEnabled;
        }

        protected override void OnExecute()
        {
            var model = (KeroseneLampModel)this.GetModel<IKeroseneLampModel>();
            if (!model.TryGetLamp(_lampId, out var record))
            {
                return;
            }

            if (record.Info.GameplayEnabled == _gameplayEnabled)
            {
                return;
            }

            model.SetLampGameplayEnabled(_lampId, _gameplayEnabled);
            this.SendEvent(new LampGameplayStateChangedEvent
            {
                LampId = _lampId,
                GameplayEnabled = _gameplayEnabled
            });
        }
    }
}
