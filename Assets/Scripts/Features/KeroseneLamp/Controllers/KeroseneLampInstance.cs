using UnityEngine;

namespace ThatGameJam.Features.KeroseneLamp.Controllers
{
    public class KeroseneLampInstance : MonoBehaviour
    {
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private Behaviour[] visualBehaviours;
        [SerializeField] private Renderer[] visualRenderers;
        [SerializeField] private GameObject gameplayEnabledRoot;
        [SerializeField] private GameObject gameplayDisabledRoot;
        [SerializeField] private AudioSource gameplayDisabledSfx;

        private bool _visualEnabled = true;
        private bool _gameplayEnabled = true;

        private void OnEnable()
        {
            SyncGameplayState();
        }

        public void SetVisualEnabled(bool enabled)
        {
            if (_visualEnabled == enabled)
            {
                return;
            }

            _visualEnabled = enabled;

            if (visualRoot != null)
            {
                visualRoot.SetActive(enabled);
            }

            if (visualBehaviours != null)
            {
                for (var i = 0; i < visualBehaviours.Length; i++)
                {
                    if (visualBehaviours[i] != null)
                    {
                        visualBehaviours[i].enabled = enabled;
                    }
                }
            }

            if (visualRenderers != null)
            {
                for (var i = 0; i < visualRenderers.Length; i++)
                {
                    if (visualRenderers[i] != null)
                    {
                        visualRenderers[i].enabled = enabled;
                    }
                }
            }
        }

        public void SetGameplayEnabled(bool enabled)
        {
            if (_gameplayEnabled == enabled)
            {
                return;
            }

            _gameplayEnabled = enabled;

            SyncGameplayState();
        }

        private void SyncGameplayState()
        {
            if (gameplayEnabledRoot != null)
            {
                gameplayEnabledRoot.SetActive(_gameplayEnabled);
            }

            if (gameplayDisabledRoot != null)
            {
                gameplayDisabledRoot.SetActive(!_gameplayEnabled);
            }

            if (!_gameplayEnabled && gameplayDisabledSfx != null)
            {
                gameplayDisabledSfx.Play();
            }
        }
    }
}
