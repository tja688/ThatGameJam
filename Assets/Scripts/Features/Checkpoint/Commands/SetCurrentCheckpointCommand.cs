using QFramework;
using ThatGameJam.Features.Checkpoint.Events;
using ThatGameJam.Features.Checkpoint.Models;
using UnityEngine;

namespace ThatGameJam.Features.Checkpoint.Commands
{
    public class SetCurrentCheckpointCommand : AbstractCommand
    {
        private readonly string _nodeId;

        public SetCurrentCheckpointCommand(string nodeId)
        {
            _nodeId = nodeId;
        }

        protected override void OnExecute()
        {
            if (string.IsNullOrEmpty(_nodeId))
            {
                return;
            }

            var model = (CheckpointModel)this.GetModel<ICheckpointModel>();
            if (model.CurrentNodeId.Value == _nodeId)
            {
                return;
            }

            model.SetCurrentNodeId(_nodeId);

            if (!model.TryGetNode(_nodeId, out var info))
            {
                LogKit.W($"[Checkpoint] NodeId not registered: {_nodeId}");
                this.SendEvent(new CheckpointChangedEvent
                {
                    NodeId = _nodeId,
                    AreaId = string.Empty,
                    SpawnPoint = Vector3.zero
                });
                return;
            }

            this.SendEvent(new CheckpointChangedEvent
            {
                NodeId = info.NodeId,
                AreaId = info.AreaId,
                SpawnPoint = info.SpawnPoint
            });
        }
    }
}
