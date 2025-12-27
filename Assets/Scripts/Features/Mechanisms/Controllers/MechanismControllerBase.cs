using QFramework;
using ThatGameJam.Features.AreaSystem.Events;
using ThatGameJam.Features.Shared;
using UnityEngine;

namespace ThatGameJam.Features.Mechanisms.Controllers
{
    public abstract class MechanismControllerBase : MonoBehaviour, IController
    {
        [SerializeField] private string areaId;

        public string AreaId => areaId;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        protected virtual void OnEnable()
        {
            this.RegisterEvent<RunResetEvent>(_ => OnHardReset())
                .UnRegisterWhenDisabled(gameObject);
            this.RegisterEvent<AreaChangedEvent>(OnAreaChanged)
                .UnRegisterWhenDisabled(gameObject);
        }

        protected virtual void OnHardReset()
        {
        }

        protected virtual void OnAreaEnter()
        {
        }

        protected virtual void OnAreaExit()
        {
        }

        private void OnAreaChanged(AreaChangedEvent e)
        {
            if (string.IsNullOrEmpty(areaId))
            {
                return;
            }

            if (e.PreviousAreaId == areaId && e.CurrentAreaId != areaId)
            {
                OnAreaExit();
            }

            if (e.CurrentAreaId == areaId && e.PreviousAreaId != areaId)
            {
                OnAreaEnter();
            }
        }
    }
}
