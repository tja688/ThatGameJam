using QFramework;
using ThatGameJam.Features.LightVitality.Commands;
using ThatGameJam.Features.PlayerCharacter2D.Controllers;
using ThatGameJam.Features.PlayerCharacter2D.Events;
using ThatGameJam.Features.PlayerCharacter2D.Models;
using ThatGameJam.Features.Shared;
using UnityEngine;

namespace ThatGameJam.Features.LightVitality.Controllers
{
    [RequireComponent(typeof(PlatformerCharacterController))]
    public class FallLightDamageController : MonoBehaviour, IController
    {
        [Tooltip("Minimum vertical fall distance (world units) before any light is drained.")]
        [SerializeField] private float minHeightForDamage = 2f;

        [Tooltip("How much light is drained per meter beyond the safe fall height.")]
        [SerializeField] private float damagePerMeter = 5f;

        [Tooltip("Maximum light drained from a single fall; set to 0 to disable the cap.")]
        [SerializeField] private float maxDamagePerFall = 45f;

        private Transform _playerTransform;
        private bool _isGrounded;
        private bool _trackingFall;
        private bool _fallCancelled;
        private float _fallStartHeight;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void Awake()
        {
            _playerTransform = transform;
        }

        private void OnEnable()
        {
            var playerModel = this.GetModel<IPlayerCharacter2DModel>();
            _isGrounded = playerModel.Grounded.Value;
            if (_isGrounded)
            {
                ResetFallTracking();
            }
            else
            {
                StartFallTracking();
            }

            this.RegisterEvent<PlayerGroundedChangedEvent>(OnPlayerGroundedChanged)
                .UnRegisterWhenDisabled(gameObject);
            this.RegisterEvent<PlayerClimbStateChangedEvent>(OnPlayerClimbStateChanged)
                .UnRegisterWhenDisabled(gameObject);
        }

        private void OnPlayerGroundedChanged(PlayerGroundedChangedEvent e)
        {
            _isGrounded = e.Grounded;
            if (e.Grounded)
            {
                if (_trackingFall && !_fallCancelled)
                {
                    ApplyFallDamage();
                }
                ResetFallTracking();
            }
            else
            {
                StartFallTracking();
            }
        }

        private void OnPlayerClimbStateChanged(PlayerClimbStateChangedEvent e)
        {
            if (e.IsClimbing)
            {
                CancelFallTracking();
            }
            else if (!_isGrounded)
            {
                StartFallTracking();
            }
        }

        private void StartFallTracking()
        {
            if (_trackingFall)
            {
                return;
            }

            _trackingFall = true;
            _fallCancelled = false;
            _fallStartHeight = _playerTransform.position.y;
        }

        private void CancelFallTracking()
        {
            if (!_trackingFall)
            {
                return;
            }

            _trackingFall = false;
            _fallCancelled = true;
        }

        private void ResetFallTracking()
        {
            _trackingFall = false;
            _fallCancelled = false;
        }

        private void ApplyFallDamage()
        {
            if (minHeightForDamage <= 0f && damagePerMeter <= 0f)
            {
                return;
            }

            var landingHeight = _playerTransform.position.y;
            var fallHeight = Mathf.Max(0f, _fallStartHeight - landingHeight);
            var safeHeight = Mathf.Max(0f, minHeightForDamage);
            var rawDamage = Mathf.Max(0f, fallHeight - safeHeight) * Mathf.Max(0f, damagePerMeter);
            if (rawDamage <= 0f)
            {
                return;
            }

            var adjustedDamage = rawDamage;
            var maxDamage = Mathf.Max(0f, maxDamagePerFall);
            if (maxDamage > 0f)
            {
                adjustedDamage = Mathf.Min(rawDamage, maxDamage);
            }

            if (adjustedDamage <= 0f)
            {
                return;
            }

            this.SendCommand(new ConsumeLightCommand(adjustedDamage, ELightConsumeReason.Fall));
        }
    }
}
