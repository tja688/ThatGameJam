using QFramework;
using ThatGameJam.Features.StoryTasks.Models;

namespace ThatGameJam.Features.StoryTasks.Queries
{
    public class HasFlagQuery : AbstractQuery<bool>
    {
        private readonly string _flagId;

        public HasFlagQuery(string flagId)
        {
            _flagId = flagId;
        }

        protected override bool OnDo()
        {
            var model = (StoryFlagsModel)this.GetModel<IStoryFlagsModel>();
            return model.HasFlag(_flagId);
        }
    }
}
