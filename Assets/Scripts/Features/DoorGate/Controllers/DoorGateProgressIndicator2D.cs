using QFramework;
using ThatGameJam.Features.BellFlower.Events;
using ThatGameJam.Features.DoorGate.Events;
using ThatGameJam.Features.DoorGate.Queries;
using ThatGameJam.Features.Shared;
using UnityEngine;

namespace ThatGameJam.Features.DoorGate.Controllers
{
    public class DoorGateProgressIndicator2D : MonoBehaviour, IController
    {
        [SerializeField] private string doorId;
        [SerializeField] private SpriteRenderer[] indicators;
        [SerializeField] private Color inactiveColor = new Color(0.25f, 0.25f, 0.25f, 1f);
        [SerializeField] private Color activeColor = Color.white;
        [SerializeField] private Color completeColor = new Color(1f, 0.9f, 0.35f, 1f);

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void OnEnable()
        {
            this.RegisterEvent<FlowerActivatedEvent>(OnFlowerActivated)
                .UnRegisterWhenDisabled(gameObject);
            this.RegisterEvent<DoorStateChangedEvent>(OnDoorStateChanged)
                .UnRegisterWhenDisabled(gameObject);
            this.RegisterEvent<RunResetEvent>(_ => RefreshFromModel())
                .UnRegisterWhenDisabled(gameObject);
            RefreshFromModel();
        }

        private void OnFlowerActivated(FlowerActivatedEvent e)
        {
            if (!IsDoorMatch(e.DoorId))
            {
                return;
            }

            RefreshFromModel();
        }

        private void OnDoorStateChanged(DoorStateChangedEvent e)
        {
            if (!IsDoorMatch(e.DoorId))
            {
                return;
            }

            RefreshFromModel();
        }

        private void RefreshFromModel()
        {
            if (string.IsNullOrEmpty(doorId))
            {
                ApplyIndicatorColors(0, false);
                return;
            }

            if (indicators == null || indicators.Length == 0)
            {
                return;
            }

            var snapshot = this.SendQuery(new GetDoorGateStateQuery(doorId));
            if (!snapshot.HasDoor)
            {
                ApplyIndicatorColors(0, false);
                return;
            }

            var state = snapshot.State;
            var isComplete = state.IsOpen || state.RequiredFlowerCount <= 0
                || (state.RequiredFlowerCount > 0 && state.ActiveFlowerCount >= state.RequiredFlowerCount);

            ApplyIndicatorColors(state.ActiveFlowerCount, isComplete);
        }

        private void ApplyIndicatorColors(int activeCount, bool isComplete)
        {
            if (indicators == null || indicators.Length == 0)
            {
                return;
            }

            if (isComplete)
            {
                for (var i = 0; i < indicators.Length; i++)
                {
                    if (indicators[i] != null)
                    {
                        indicators[i].color = completeColor;
                    }
                }

                return;
            }

            var litCount = Mathf.Clamp(activeCount, 0, indicators.Length);
            for (var i = 0; i < indicators.Length; i++)
            {
                var indicator = indicators[i];
                if (indicator == null)
                {
                    continue;
                }

                indicator.color = i < litCount ? activeColor : inactiveColor;
            }
        }

        private bool IsDoorMatch(string eventDoorId)
        {
            return !string.IsNullOrEmpty(doorId) && eventDoorId == doorId;
        }
    }
}
