using System.Collections;
using System.Collections.Generic;
using QFramework;
using ThatGameJam.Features.SafeZone.Commands;
using ThatGameJam.Features.Shared;
using UnityEngine;

namespace ThatGameJam.Features.SafeZone.Controllers
{
    public class PlayerSafeZoneSensor : MonoBehaviour, IController
    {
        private readonly HashSet<SafeZone2D> _zones = new HashSet<SafeZone2D>();
        private readonly List<Collider2D> _overlapResults = new List<Collider2D>(8);
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

        public void NotifyZoneEnter(SafeZone2D zone)
        {
            if (zone == null || !_zones.Add(zone))
            {
                return;
            }

            UpdateCount();
        }

        public void NotifyZoneExit(SafeZone2D zone)
        {
            if (zone == null || !_zones.Remove(zone))
            {
                return;
            }

            UpdateCount();
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

            if (_selfColliders == null || _selfColliders.Length == 0)
            {
                UpdateCount();
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

                    var zone = hit.GetComponentInParent<SafeZone2D>();
                    if (zone != null)
                    {
                        _zones.Add(zone);
                    }
                }
            }

            UpdateCount();
        }

        private void ResetState()
        {
            if (_zones.Count == 0)
            {
                this.SendCommand(new SetSafeZoneCountCommand(0));
                return;
            }

            _zones.Clear();
            UpdateCount();
        }

        private void UpdateCount()
        {
            this.SendCommand(new SetSafeZoneCountCommand(_zones.Count));
        }
    }
}
