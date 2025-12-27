using System.Collections.Generic;
using QFramework;

namespace ThatGameJam.Features.Checkpoint.Models
{
    public class CheckpointModel : AbstractModel, ICheckpointModel
    {
        private readonly BindableProperty<string> _currentNodeId = new BindableProperty<string>(string.Empty);
        private readonly Dictionary<string, CheckpointNodeInfo> _nodes = new Dictionary<string, CheckpointNodeInfo>();

        public IReadonlyBindableProperty<string> CurrentNodeId => _currentNodeId;

        internal bool TryGetNode(string nodeId, out CheckpointNodeInfo info) => _nodes.TryGetValue(nodeId, out info);
        internal void SetCurrentNodeId(string nodeId) => _currentNodeId.Value = nodeId ?? string.Empty;

        internal void RegisterNode(CheckpointNodeInfo info)
        {
            if (string.IsNullOrEmpty(info.NodeId))
            {
                return;
            }

            _nodes[info.NodeId] = info;
        }

        internal void UnregisterNode(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                return;
            }

            _nodes.Remove(nodeId);
        }

        protected override void OnInit()
        {
            _currentNodeId.Value = string.Empty;
            _nodes.Clear();
        }
    }
}
