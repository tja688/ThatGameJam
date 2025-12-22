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

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void OnEnable()
        {
            this.RegisterEvent<RunResetEvent>(OnRunReset)
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
            ResetState();
        }

        private void OnRunReset(RunResetEvent e)
        {
            ResetState();
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
