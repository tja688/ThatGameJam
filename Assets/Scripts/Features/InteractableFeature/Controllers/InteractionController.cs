using QFramework;
using ThatGameJam.Features.InteractableFeature.Events;
using UnityEngine;

namespace ThatGameJam.Features.InteractableFeature.Controllers
{
    public class InteractionController : MonoBehaviour, IController
    {
        [SerializeField] private PlayerInteractSensor sensor;
        [SerializeField] private float interactCooldown = 0.1f;

        private float _nextAllowedTime;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void Awake()
        {
            if (sensor == null)
            {
                sensor = GetComponent<PlayerInteractSensor>();
            }
        }

        private void OnEnable()
        {
            this.RegisterEvent<InteractPressedEvent>(OnInteractPressed)
                .UnRegisterWhenDisabled(gameObject);
        }

        private void OnInteractPressed(InteractPressedEvent e)
        {
            if (Time.time < _nextAllowedTime)
            {
                return;
            }

            if (sensor == null)
            {
                return;
            }

            var candidate = sensor.GetCurrentCandidate();
            if (candidate == null)
            {
                return;
            }

            if (candidate.TryInteract(transform))
            {
                _nextAllowedTime = Time.time + interactCooldown;
            }
        }
    }
}
