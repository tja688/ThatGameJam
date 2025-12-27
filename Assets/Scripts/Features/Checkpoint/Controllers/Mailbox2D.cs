using QFramework;
using ThatGameJam.Features.Checkpoint.Commands;
using UnityEngine;

namespace ThatGameJam.Features.Checkpoint.Controllers
{
    [RequireComponent(typeof(Collider2D))]
    public class Mailbox2D : MonoBehaviour, IController
    {
        [SerializeField] private LevelNode2D node;
        [SerializeField] private string nodeId;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private bool triggerOnce = true;

        private bool _triggered;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void Awake()
        {
            var collider2D = GetComponent<Collider2D>();
            if (collider2D != null && !collider2D.isTrigger)
            {
                LogKit.W("Mailbox2D expects Collider2D.isTrigger = true.");
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (triggerOnce && _triggered)
            {
                return;
            }

            if (!IsPlayer(other))
            {
                return;
            }

            var resolvedNodeId = node != null ? node.NodeId : nodeId;
            if (string.IsNullOrEmpty(resolvedNodeId))
            {
                LogKit.W("[Checkpoint] Mailbox2D missing nodeId.");
                return;
            }

            _triggered = true;
            this.SendCommand(new SetCurrentCheckpointCommand(resolvedNodeId));
        }

        private bool IsPlayer(Collider2D other)
        {
            if (string.IsNullOrEmpty(playerTag))
            {
                return true;
            }

            return other.CompareTag(playerTag);
        }
    }
}
