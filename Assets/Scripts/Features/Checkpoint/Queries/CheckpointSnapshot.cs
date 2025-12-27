using UnityEngine;

namespace ThatGameJam.Features.Checkpoint.Queries
{
    public struct CheckpointSnapshot
    {
        public bool IsValid;
        public string NodeId;
        public string AreaId;
        public Vector3 SpawnPoint;
    }
}
