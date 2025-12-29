using System.Collections.Generic;
using QFramework;

namespace ThatGameJam.Features.StoryTasks.Models
{
    public class StoryFlagsModel : AbstractModel, IStoryFlagsModel
    {
        private readonly HashSet<string> _flags = new HashSet<string>();

        internal bool HasFlag(string flagId)
        {
            if (string.IsNullOrEmpty(flagId))
            {
                return false;
            }

            return _flags.Contains(flagId);
        }

        internal void SetFlag(string flagId)
        {
            if (string.IsNullOrEmpty(flagId))
            {
                return;
            }

            _flags.Add(flagId);
        }

        internal List<string> GetAllFlags()
        {
            return new List<string>(_flags);
        }

        internal void ClearFlags()
        {
            _flags.Clear();
        }

        protected override void OnInit()
        {
            _flags.Clear();
        }
    }
}
