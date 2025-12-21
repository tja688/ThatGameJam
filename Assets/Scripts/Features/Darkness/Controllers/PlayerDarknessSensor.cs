using System.Collections.Generic;
using QFramework;
using ThatGameJam.Features.Darkness.Commands;
using UnityEngine;

namespace ThatGameJam.Features.Darkness.Controllers
{
    public class PlayerDarknessSensor : MonoBehaviour, IController
    {
        [SerializeField] private float enterDelay = 0.1f;
        [SerializeField] private float exitDelay = 0.1f;

        private readonly HashSet<DarknessZone2D> _zones = new HashSet<DarknessZone2D>();
        private bool _isInDarkness;
        private bool _enterPending;
        private bool _exitPending;
        private float _enterCountdown;
        private float _exitCountdown;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

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
            _zones.Clear();
            _enterPending = false;
            _exitPending = false;
            _enterCountdown = 0f;
            _exitCountdown = 0f;

            if (_isInDarkness)
            {
                _isInDarkness = false;
                this.SendCommand(new SetInDarknessCommand(false));
            }
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
