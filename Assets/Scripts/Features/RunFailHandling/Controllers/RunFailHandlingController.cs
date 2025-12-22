using System.Collections;
using QFramework;
using ThatGameJam.Features.RunFailReset.Systems;
using ThatGameJam.Features.Shared;
using UnityEngine;

namespace ThatGameJam.Features.RunFailHandling.Controllers
{
    public class RunFailHandlingController : MonoBehaviour, IController
    {
        [SerializeField] private float resetDelaySeconds = 3f;

        private Coroutine _failRoutine;
        private bool _isHandlingFail;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void OnEnable()
        {
            this.RegisterEvent<RunFailedEvent>(OnRunFailed)
                .UnRegisterWhenDisabled(gameObject);
        }

        private void OnDisable()
        {
            if (_failRoutine != null)
            {
                StopCoroutine(_failRoutine);
                _failRoutine = null;
            }

            _isHandlingFail = false;
        }

        private void OnRunFailed(RunFailedEvent e)
        {
            if (_isHandlingFail)
            {
                return;
            }

            _isHandlingFail = true;
            _failRoutine = StartCoroutine(HandleRunFailedRoutine());
        }

        private IEnumerator HandleRunFailedRoutine()
        {
            // Placeholder fail handling; replace with real flow later.
            LogKit.I("[RunFailHandling] Run failed. Placeholder handling begins.");

            if (resetDelaySeconds > 0f)
            {
                yield return new WaitForSeconds(resetDelaySeconds);
            }

            RequestRunReset();

            _failRoutine = null;
            _isHandlingFail = false;
        }

        private void RequestRunReset()
        {
            this.GetSystem<IRunFailResetSystem>().RequestResetFromFail();
        }
    }
}
