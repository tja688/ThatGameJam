using QFramework;
using ThatGameJam.Features.AreaSystem.Events;
using ThatGameJam.Features.AreaSystem.Models;

namespace ThatGameJam.Features.AreaSystem.Commands
{
    public class SetCurrentAreaCommand : AbstractCommand
    {
        private readonly string _areaId;

        public SetCurrentAreaCommand(string areaId)
        {
            _areaId = areaId ?? string.Empty;
        }

        protected override void OnExecute()
        {
            var model = (AreaModel)this.GetModel<IAreaModel>();
            if (model.CurrentAreaValue == _areaId)
            {
                return;
            }

            var previous = model.CurrentAreaValue;
            model.SetCurrentAreaId(_areaId);

            this.SendEvent(new AreaChangedEvent
            {
                PreviousAreaId = previous,
                CurrentAreaId = _areaId
            });
        }
    }
}
