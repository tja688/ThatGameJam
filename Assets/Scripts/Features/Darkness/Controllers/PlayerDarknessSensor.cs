using System.Collections;
using System.Collections.Generic;
using QFramework;
using ThatGameJam.Features.Darkness.Commands;
using ThatGameJam.Features.Shared;
using UnityEngine;

namespace ThatGameJam.Features.Darkness.Controllers
{
    public class PlayerDarknessSensor : MonoBehaviour, IController
    {
        [SerializeField] private float enterDelay = 0.1f;
        [SerializeField] private float exitDelay = 0.1f;

        private readonly HashSet<DarknessZone2D> _zones = new HashSet<DarknessZone2D>();
        private readonly List<Collider2D> _overlapResults = new List<Collider2D>(8);
        private bool _isInDarkness;
        private bool _enterPending;
        private bool _exitPending;
        private float _enterCountdown;
        private float _exitCountdown;
        private ContactFilter2D _overlapFilter;
        private Collider2D[] _selfColliders;
        private Coroutine _refreshRoutine;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void Awake()
        {
            _selfColliders = GetComponentsInChildren<Collider2D>();
            _overlapFilter = new ContactFilter2D
            {
                useTriggers = true,
                useLayerMask = false
            };
        }

        private void OnEnable()
        {
            this.RegisterEvent<RunResetEvent>(OnRunReset)
                .UnRegisterWhenDisabled(gameObject);
            this.RegisterEvent<PlayerRespawnedEvent>(OnPlayerRespawned)
                .UnRegisterWhenDisabled(gameObject);
        }

        public void NotifyZoneEnter(DarknessZone2D zone)
        {
            if (zone == null || !_zones.Add(zone))
            {
                return;
            }

            UpdatePendingState();
        }

        public void NotifyZoneExit(DarknessZone2D zone)
        {
            if (zone == null || !_zones.Remove(zone))
            {
                return;
            }

            UpdatePendingState();
        }

        private void Update()
        {
            if (_enterPending)
            {
                _enterCountdown -= Time.deltaTime;
                if (_enterCountdown <= 0f && _zones.Count > 0)
                {
                    ApplyState(true);
                }
            }

            if (_exitPending)
            {
                _exitCountdown -= Time.deltaTime;
                if (_exitCountdown <= 0f && _zones.Count == 0)
                {
                    ApplyState(false);
                }
            }
        }

        private void OnDisable()
        {
            if (_refreshRoutine != null)
            {
                StopCoroutine(_refreshRoutine);
                _refreshRoutine = null;
            }

            ResetState();
        }

        private void OnRunReset(RunResetEvent e)
        {
            RequestRefresh();
        }

        private void OnPlayerRespawned(PlayerRespawnedEvent e)
        {
            RefreshOverlap();
        }

        private void RequestRefresh()
        {
            if (_refreshRoutine != null)
            {
                StopCoroutine(_refreshRoutine);
            }

            _refreshRoutine = StartCoroutine(RefreshNextFrame());
        }

        private IEnumerator RefreshNextFrame()
        {
            yield return null;
            _refreshRoutine = null;
            RefreshOverlap();
        }

        private void RefreshOverlap()
        {
            _zones.Clear();
            _enterPending = false;
            _exitPending = false;
            _enterCountdown = 0f;
            _exitCountdown = 0f;

            if (_selfColliders == null || _selfColliders.Length == 0)
            {
                ApplyState(false);
                return;
            }

            for (var i = 0; i < _selfColliders.Length; i++)
            {
                var collider2D = _selfColliders[i];
                if (collider2D == null || !collider2D.enabled)
                {
                    continue;
                }

                _overlapResults.Clear();
                collider2D.Overlap(_overlapFilter, _overlapResults);
                for (var j = 0; j < _overlapResults.Count; j++)
                {
                    var hit = _overlapResults[j];
                    if (hit == null)
                    {
                        continue;
                    }

                    var zone = hit.GetComponentInParent<DarknessZone2D>();
                    if (zone != null)
                    {
                        _zones.Add(zone);
                    }
                }
            }

            ApplyState(_zones.Count > 0);
        }

        private void ResetState()
        {
            _zones.Clear();
            _enterPending = false;
            _exitPending = false;
            _enterCountdown = 0f;
            _exitCountdown = 0f;
            _isInDarkness = false;

            this.SendCommand(new SetInDarknessCommand(false));
        }

        private void UpdatePendingState()
        {
            if (_zones.Count > 0)
            {
                _exitPending = false;
                _exitCountdown = 0f;

                if (!_isInDarkness)
                {
                    if (enterDelay <= 0f)
                    {
                        ApplyState(true);
                    }
                    else
                    {
                        _enterPending = true;
                        _enterCountdown = enterDelay;
                    }
                }
            }
            else
            {
                _enterPending = false;
                _enterCountdown = 0f;

                if (_isInDarkness)
                {
                    if (exitDelay <= 0f)
                    {
                        ApplyState(false);
                    }
                    else
                    {
                        _exitPending = true;
                        _exitCountdown = exitDelay;
                    }
                }
            }
        }

        private void ApplyState(bool isInDarkness)
        {
            _enterPending = false;
            _exitPending = false;

            if (_isInDarkness == isInDarkness)
            {
                return;
            }

            _isInDarkness = isInDarkness;
            this.SendCommand(new SetInDarknessCommand(isInDarkness));
        }
    }
}
