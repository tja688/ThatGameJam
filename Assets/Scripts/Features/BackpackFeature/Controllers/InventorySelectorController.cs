using QFramework;
using ThatGameJam.Features.BackpackFeature.Commands;
using ThatGameJam.Features.BackpackFeature.Events;
using ThatGameJam.Features.BackpackFeature.Queries;
using ThatGameJam.Features.InteractableFeature.Events;
using ThatGameJam.SaveSystem;
using UnityEngine;

namespace ThatGameJam.Features.BackpackFeature.Controllers
{
    public class InventorySelectorController : MonoBehaviour, IController
    {
        [SerializeField] private Transform holdPoint;
        [SerializeField] private bool selectUpdatesHeld = true;
        [SerializeField] private bool applyHeldOnEnable = true;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private SaveManager _saveManager;

        private void OnEnable()
        {
            this.RegisterEvent<ScrollUpEvent>(_ => CycleSelection(1))
                .UnRegisterWhenDisabled(gameObject);
            this.RegisterEvent<ScrollDownEvent>(_ => CycleSelection(-1))
                .UnRegisterWhenDisabled(gameObject);
            this.RegisterEvent<BackpackChangedEvent>(_ => ClampSelection())
                .UnRegisterWhenDisabled(gameObject);

            if (applyHeldOnEnable)
            {
                ApplyHeldFromModel();
                _saveManager = SaveManager.Instance;
                if (_saveManager != null)
                {
                    _saveManager.RestoreFinished += OnRestoreFinished;
                }
            }
        }

        private void OnDisable()
        {
            if (_saveManager != null)
            {
                _saveManager.RestoreFinished -= OnRestoreFinished;
                _saveManager = null;
            }
        }

        private void CycleSelection(int direction)
        {
            var items = this.SendQuery(new GetBackpackItemsQuery());
            if (items == null || items.Count == 0)
            {
                return;
            }

            var currentIndex = this.SendQuery(new GetSelectedIndexQuery());
            var nextIndex = currentIndex;
            if (currentIndex < 0)
            {
                nextIndex = direction > 0 ? 0 : items.Count - 1;
            }
            else
            {
                nextIndex = (currentIndex + direction) % items.Count;
                if (nextIndex < 0)
                {
                    nextIndex += items.Count;
                }
            }

            this.SendCommand(new SetSelectedIndexCommand(nextIndex));

            if (selectUpdatesHeld)
            {
                this.SendCommand(new SetHeldItemCommand(nextIndex, holdPoint));
            }
        }

        private void ClampSelection()
        {
            var items = this.SendQuery(new GetBackpackItemsQuery());
            var count = items != null ? items.Count : 0;
            var currentIndex = this.SendQuery(new GetSelectedIndexQuery());

            if (count == 0)
            {
                if (currentIndex != -1)
                {
                    this.SendCommand(new SetSelectedIndexCommand(-1));
                }

                if (selectUpdatesHeld)
                {
                    this.SendCommand(new SetHeldItemCommand(-1, holdPoint));
                }

                return;
            }

            if (currentIndex >= count)
            {
                this.SendCommand(new SetSelectedIndexCommand(count - 1));
            }
        }

        private void ApplyHeldFromModel()
        {
            if (holdPoint == null)
            {
                return;
            }

            var heldIndex = this.SendQuery(new GetHeldIndexQuery());
            if (heldIndex >= 0)
            {
                this.SendCommand(new SetHeldItemCommand(heldIndex, holdPoint, true, true));
            }
        }

        private void OnRestoreFinished()
        {
            if (applyHeldOnEnable)
            {
                ApplyHeldFromModel();
            }
        }
    }
}
