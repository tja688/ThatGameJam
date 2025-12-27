using UnityEngine;

namespace ThatGameJam.Features.Checkpoint.Events
{
    public struct CheckpointChangedEvent
    {
        public string NodeId;
        public string AreaId;
        public Vector3 SpawnPoint;
    }
}
