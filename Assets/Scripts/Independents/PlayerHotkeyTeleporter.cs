using System.Collections;
using System.Collections.Generic;
using QFramework;
using ThatGameJam.Features.LightVitality.Controllers;
using ThatGameJam.Features.PlayerCharacter2D.Commands;
using ThatGameJam.Features.PlayerCharacter2D.Controllers;
using UnityEngine;

namespace ThatGameJam.Independents
{
    [DisallowMultipleComponent]
    public class PlayerHotkeyTeleporter : MonoBehaviour, IController
    {
        [Header("Player")]
        [SerializeField] private Transform playerRoot;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private bool autoFindPlayer = true;

        [Header("Targets")]
        [SerializeField] private List<Transform> teleportTargets = new List<Transform>();
        [SerializeField] private bool loopTargets = true;
        [SerializeField] private int startIndex = 0;

        [Header("Input")]
        [SerializeField] private KeyCode triggerKey = KeyCode.T;

        [Header("Safety")]
        [SerializeField] private bool resetVelocity = true;
        [SerializeField] private bool resetClimbState = true;
        [SerializeField] private bool suppressFallDamage = true;
        [SerializeField] private float fallDamageReenableDelay = 0f;

        private int _currentIndex;
        private Rigidbody2D _playerRigidbody;
        private FallLightDamageController _fallDamageController;
        private Coroutine _fallDamageRoutine;
        private bool _fallDamageSuspended;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void Awake()
        {
            _currentIndex = startIndex;
            ResolvePlayer();
        }

        private void OnEnable()
        {
            ResolvePlayer();
        }

        private void OnDisable()
        {
            if (_fallDamageRoutine != null)
            {
                StopCoroutine(_fallDamageRoutine);
                _fallDamageRoutine = null;
            }

            if (_fallDamageSuspended && _fallDamageController != null)
            {
                _fallDamageController.enabled = true;
                _fallDamageSuspended = false;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(triggerKey))
            {
                TeleportNext();
            }
        }

        private void ResolvePlayer()
        {
            if (playerRoot == null && autoFindPlayer)
            {
                if (!string.IsNullOrEmpty(playerTag))
                {
                    var playerObj = GameObject.FindGameObjectWithTag(playerTag);
                    if (playerObj != null)
                    {
                        playerRoot = playerObj.transform;
                    }
                }

                if (playerRoot == null)
                {
                    var controller = FindObjectOfType<PlatformerCharacterController>();
                    if (controller != null)
                    {
                        playerRoot = controller.transform;
                    }
                }
            }

            if (playerRoot == null)
            {
                _playerRigidbody = null;
                _fallDamageController = null;
                return;
            }

            _playerRigidbody = playerRoot.GetComponent<Rigidbody2D>();
            if (_playerRigidbody == null)
            {
                _playerRigidbody = playerRoot.GetComponentInChildren<Rigidbody2D>();
            }

            _fallDamageController = playerRoot.GetComponent<FallLightDamageController>();
            if (_fallDamageController == null)
            {
                _fallDamageController = playerRoot.GetComponentInChildren<FallLightDamageController>();
            }
        }

        private void TeleportNext()
        {
            if (playerRoot == null)
            {
                ResolvePlayer();
                if (playerRoot == null)
                {
                    return;
                }
            }

            if (!TryGetNextTarget(out var target))
            {
                return;
            }

            if (suppressFallDamage)
            {
                SuspendFallDamage();
            }

            var targetPos = target.position;
            if (_playerRigidbody != null)
            {
                if (resetVelocity)
                {
                    _playerRigidbody.linearVelocity = Vector2.zero;
                    _playerRigidbody.angularVelocity = 0f;
                }

                _playerRigidbody.position = targetPos;
            }
            else
            {
                playerRoot.position = targetPos;
            }

            if (resetClimbState)
            {
                this.SendCommand(new ResetClimbStateCommand());
            }
        }

        private bool TryGetNextTarget(out Transform target)
        {
            target = null;

            if (teleportTargets == null || teleportTargets.Count == 0)
            {
                return false;
            }

            var count = teleportTargets.Count;
            if (!loopTargets && _currentIndex >= count)
            {
                return false;
            }

            if (_currentIndex < 0)
            {
                _currentIndex = 0;
            }
            else if (_currentIndex >= count)
            {
                _currentIndex = loopTargets ? 0 : count;
            }

            var attempts = 0;
            var index = _currentIndex;

            while (attempts < count)
            {
                var candidate = teleportTargets[index];
                if (candidate != null)
                {
                    target = candidate;
                    _currentIndex = index + 1;
                    if (loopTargets && _currentIndex >= count)
                    {
                        _currentIndex = 0;
                    }
                    return true;
                }

                index++;
                attempts++;
                if (index >= count)
                {
                    if (loopTargets)
                    {
                        index = 0;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return false;
        }

        private void SuspendFallDamage()
        {
            if (_fallDamageController == null || !_fallDamageController.enabled)
            {
                return;
            }

            _fallDamageController.enabled = false;
            _fallDamageSuspended = true;

            if (_fallDamageRoutine != null)
            {
                StopCoroutine(_fallDamageRoutine);
            }

            _fallDamageRoutine = StartCoroutine(ReenableFallDamage());
        }

        private IEnumerator ReenableFallDamage()
        {
            if (fallDamageReenableDelay <= 0f)
            {
                yield return null;
            }
            else
            {
                yield return new WaitForSeconds(fallDamageReenableDelay);
            }

            if (_fallDamageController != null)
            {
                _fallDamageController.enabled = true;
            }

            _fallDamageSuspended = false;
            _fallDamageRoutine = null;
        }

        private void OnValidate()
        {
            if (startIndex < 0)
            {
                startIndex = 0;
            }

            if (fallDamageReenableDelay < 0f)
            {
                fallDamageReenableDelay = 0f;
            }
        }
    }
}
