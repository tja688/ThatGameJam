using QFramework;
using ThatGameJam.Features.RunFailReset.Models;
using ThatGameJam.Features.RunFailReset.Systems;
using ThatGameJam.Features.Shared;
using UnityEngine;

namespace ThatGameJam.Features.RunFailReset.Controllers
{
    public class RunResetController : MonoBehaviour, IController
    {
        [SerializeField] private KeyCode resetKey = KeyCode.R;
        [SerializeField] private bool requireFailed = true;

        private bool _isFailed;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void OnEnable()
        {
            this.RegisterEvent<RunFailedEvent>(OnRunFailed)
                .UnRegisterWhenDisabled(gameObject);
            this.RegisterEvent<RunResetEvent>(OnRunReset)
                .UnRegisterWhenDisabled(gameObject);

            var model = this.GetModel<IRunFailResetModel>();
            _isFailed = model.IsFailed.Value;
        }

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!Input.GetKeyDown(resetKey))
            {
                return;
            }

            if (requireFailed && !_isFailed)
            {
                return;
            }

            this.GetSystem<IRunFailResetSystem>().RequestResetFromTest();
#endif
        }

        public void RequestReset()
        {
            this.GetSystem<IRunFailResetSystem>().RequestResetFromTest();
        }

        private void OnRunFailed(RunFailedEvent e)
        {
            _isFailed = true;
        }

        private void OnRunReset(RunResetEvent e)
        {
            _isFailed = false;
        }
    }
}
