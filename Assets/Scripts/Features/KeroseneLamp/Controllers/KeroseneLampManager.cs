using System.Collections.Generic;
using QFramework;
using ThatGameJam.Features.KeroseneLamp.Commands;
using ThatGameJam.Features.KeroseneLamp.Models;
using ThatGameJam.Features.RunFailReset.Commands;
using ThatGameJam.Features.Shared;
using UnityEngine;

namespace ThatGameJam.Features.KeroseneLamp.Controllers
{
    public class KeroseneLampManager : MonoBehaviour, IController
    {
        [SerializeField] private GameObject lampPrefab;
        [SerializeField] private Transform lampParent;
        [SerializeField] private int maxLamps = 3;
        [SerializeField] private bool applyMaxOnEnable = true;

        private readonly List<GameObject> _lamps = new List<GameObject>();
        private int _nextLampId;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void OnEnable()
        {
            if (applyMaxOnEnable)
            {
                this.SendCommand(new SetLampMaxCommand(maxLamps));
            }

            this.RegisterEvent<PlayerDiedEvent>(OnPlayerDied)
                .UnRegisterWhenDisabled(gameObject);
            this.RegisterEvent<RunResetEvent>(OnRunReset)
                .UnRegisterWhenDisabled(gameObject);
        }

        private void OnPlayerDied(PlayerDiedEvent e)
        {
            var model = this.GetModel<IKeroseneLampModel>();
            if (model.LampCount.Value >= model.LampMax.Value)
            {
                this.SendCommand(new MarkRunFailedCommand());
                return;
            }

            SpawnLamp(e.WorldPos);
        }

        private void OnRunReset(RunResetEvent e)
        {
            ClearLamps();
            _nextLampId = 0;
            this.SendCommand(new ResetLampsCommand());
        }

        private void SpawnLamp(Vector3 worldPos)
        {
            var lampId = _nextLampId++;

            if (lampPrefab != null)
            {
                var instance = Instantiate(lampPrefab, worldPos, Quaternion.identity, lampParent);
                _lamps.Add(instance);
            }
            else
            {
                LogKit.W("KeroseneLampManager missing lampPrefab. Lamp will not be instantiated.");
            }

            this.SendCommand(new RecordLampSpawnedCommand(lampId, worldPos));
        }

        private void ClearLamps()
        {
            for (var i = 0; i < _lamps.Count; i++)
            {
                if (_lamps[i] != null)
                {
                    Destroy(_lamps[i]);
                }
            }

            _lamps.Clear();
        }
    }
}
