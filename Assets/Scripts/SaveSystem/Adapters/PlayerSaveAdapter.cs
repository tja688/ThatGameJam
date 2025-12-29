using System;
using QFramework;
using ThatGameJam.Features.PlayerCharacter2D.Commands;
using ThatGameJam.Features.PlayerCharacter2D.Controllers;
using UnityEngine;

namespace ThatGameJam.SaveSystem.Adapters
{
    [Serializable]
    public class PlayerSaveData
    {
        public Vector3 position;
        public Vector2 velocity;
    }

    public class PlayerSaveAdapter : SaveParticipant<PlayerSaveData>, IController
    {
        [SerializeField] private string saveKey = "player.core";
        [SerializeField] private PlatformerCharacterController playerController;
        [SerializeField] private Rigidbody2D playerRigidbody;

        public override string SaveKey => saveKey;
        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void Reset()
        {
            saveKey = "player.core";
        }

        protected override PlayerSaveData Capture()
        {
            ResolveReferences();

            var transform = playerController != null ? playerController.transform : null;
            var position = transform != null ? transform.position : Vector3.zero;
            var velocity = playerRigidbody != null ? playerRigidbody.linearVelocity : Vector2.zero;

            return new PlayerSaveData
            {
                position = position,
                velocity = velocity
            };
        }

        protected override void Restore(PlayerSaveData data)
        {
            if (data == null)
            {
                return;
            }

            ResolveReferences();

            if (playerRigidbody != null)
            {
                playerRigidbody.position = data.position;
                playerRigidbody.linearVelocity = data.velocity;
            }
            else if (playerController != null)
            {
                playerController.transform.position = data.position;
            }

            this.SendCommand(new ResetClimbStateCommand());
        }

        private void ResolveReferences()
        {
            if (playerController == null)
            {
                playerController = FindObjectOfType<PlatformerCharacterController>();
            }

            if (playerRigidbody == null && playerController != null)
            {
                playerRigidbody = playerController.GetComponent<Rigidbody2D>();
            }
        }
    }
}
