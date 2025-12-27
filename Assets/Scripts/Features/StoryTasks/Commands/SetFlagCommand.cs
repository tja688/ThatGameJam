using QFramework;
using ThatGameJam.Features.StoryTasks.Events;
using ThatGameJam.Features.StoryTasks.Models;

namespace ThatGameJam.Features.StoryTasks.Commands
{
    public class SetFlagCommand : AbstractCommand
    {
        private readonly string _flagId;

        public SetFlagCommand(string flagId)
        {
            _flagId = flagId;
        }

        protected override void OnExecute()
        {
            if (string.IsNullOrEmpty(_flagId))
            {
                return;
            }

            var model = (StoryFlagsModel)this.GetModel<IStoryFlagsModel>();
            if (model.HasFlag(_flagId))
            {
                return;
            }

            model.SetFlag(_flagId);
            this.SendEvent(new StoryFlagChangedEvent
            {
                FlagId = _flagId
            });
        }
    }
}
