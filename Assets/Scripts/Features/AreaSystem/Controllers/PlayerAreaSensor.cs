using System.Collections;
using System.Collections.Generic;
using QFramework;
using ThatGameJam.Features.AreaSystem.Commands;
using ThatGameJam.Features.Shared;
using UnityEngine;

namespace ThatGameJam.Features.AreaSystem.Controllers
{
    public class PlayerAreaSensor : MonoBehaviour, IController
    {
        private readonly HashSet<AreaVolume2D> _areas = new HashSet<AreaVolume2D>();
        private readonly List<Collider2D> _overlapResults = new List<Collider2D>(8);
        private ContactFilter2D _overlapFilter;
        private Collider2D[] _selfColliders;
        private Coroutine _refreshRoutine;
        private string _currentAreaId;

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

        public void NotifyAreaEnter(AreaVolume2D area)
        {
            if (area == null || !_areas.Add(area))
            {
                return;
            }

            RefreshCurrentArea();
        }

        public void NotifyAreaExit(AreaVolume2D area)
        {
            if (area == null || !_areas.Remove(area))
            {
                return;
            }

            RefreshCurrentArea();
        }

        private void OnDisable()
        {
            if (_refreshRoutine != null)
            {
                StopCoroutine(_refreshRoutine);
                _refreshRoutine = null;
            }

            _areas.Clear();
            SetCurrentArea(string.Empty);
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
            _areas.Clear();

            if (_selfColliders == null || _selfColliders.Length == 0)
            {
                RefreshCurrentArea();
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

                    var volume = hit.GetComponentInParent<AreaVolume2D>();
                    if (volume != null)
                    {
                        _areas.Add(volume);
                    }
                }
            }

            RefreshCurrentArea();
        }

        private void RefreshCurrentArea()
        {
            var best = SelectBestArea();
            var nextId = best != null ? best.AreaId : string.Empty;
            if (nextId == _currentAreaId)
            {
                return;
            }

            SetCurrentArea(nextId);
        }

        private void SetCurrentArea(string areaId)
        {
            _currentAreaId = areaId ?? string.Empty;
            this.SendCommand(new SetCurrentAreaCommand(_currentAreaId));
        }

        private AreaVolume2D SelectBestArea()
        {
            AreaVolume2D best = null;
            foreach (var area in _areas)
            {
                if (area == null)
                {
                    continue;
                }

                if (best == null)
                {
                    best = area;
                    continue;
                }

                if (area.Priority > best.Priority)
                {
                    best = area;
                    continue;
                }

                if (area.Priority < best.Priority)
                {
                    continue;
                }

                if (area.AreaSize > best.AreaSize)
                {
                    best = area;
                }
            }

            return best;
        }
    }
}
