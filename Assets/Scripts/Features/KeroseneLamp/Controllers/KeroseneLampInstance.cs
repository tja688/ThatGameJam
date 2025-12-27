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

            if (gameplayEnabledRoot != null)
            {
                gameplayEnabledRoot.SetActive(enabled);
            }

            if (gameplayDisabledRoot != null)
            {
                gameplayDisabledRoot.SetActive(!enabled);
            }

            if (!enabled && gameplayDisabledSfx != null)
            {
                gameplayDisabledSfx.Play();
            }
        }
    }
}
