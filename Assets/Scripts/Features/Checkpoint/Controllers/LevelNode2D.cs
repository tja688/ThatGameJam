using QFramework;
using ThatGameJam.Features.Checkpoint.Commands;
using ThatGameJam.Features.Checkpoint.Models;
using UnityEngine;

namespace ThatGameJam.Features.Checkpoint.Controllers
{
    public class LevelNode2D : MonoBehaviour, IController
    {
        [SerializeField] private string nodeId;
        [SerializeField] private string areaId;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private bool registerOnEnable = true;

        public string NodeId => nodeId;
        public string AreaId => areaId;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void OnEnable()
        {
            if (registerOnEnable)
            {
                RegisterNode();
            }
        }

        private void OnDisable()
        {
            if (registerOnEnable)
            {
                UnregisterNode();
            }
        }

        private void RegisterNode()
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                LogKit.W("[Checkpoint] LevelNode2D missing nodeId.");
                return;
            }

            var pos = spawnPoint != null ? spawnPoint.position : transform.position;
            var info = new CheckpointNodeInfo
            {
                NodeId = nodeId,
                AreaId = areaId ?? string.Empty,
                SpawnPoint = pos
            };

            this.SendCommand(new RegisterCheckpointNodeCommand(info));
        }

        private void UnregisterNode()
        {
            if (string.IsNullOrEmpty(nodeId))
            {
                return;
            }

            this.SendCommand(new UnregisterCheckpointNodeCommand(nodeId));
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            var pos = spawnPoint != null ? spawnPoint.position : transform.position;
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.6f);
            Gizmos.DrawWireSphere(pos, 0.2f);
        }
#endif
    }
}
