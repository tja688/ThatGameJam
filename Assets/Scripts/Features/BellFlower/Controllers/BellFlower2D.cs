using QFramework;
using ThatGameJam.Independents.Audio;
using ThatGameJam.Features.BellFlower.Events;
using ThatGameJam.Features.KeroseneLamp.Queries;
using UnityEngine;

namespace ThatGameJam.Features.BellFlower.Controllers
{
    public class BellFlower2D : MonoBehaviour, IController, ICanSendEvent
    {
        [SerializeField] private string flowerId;
        [SerializeField] private string doorId;
        [SerializeField] private float lightAffectRadius = 3f;
        [SerializeField] private int minLampCount = 1;
        [SerializeField] private float activationSeconds = 1f;
        [SerializeField] private bool allowDeactivate = true;

        private float _lightTimer;
        private bool _isActive;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void OnEnable()
        {
            _lightTimer = 0f;
            _isActive = false;
        }

        private void Update()
        {
            var hasLight = CountLampsInRange() >= Mathf.Max(1, minLampCount);
            UpdateLightTimer(hasLight, Time.deltaTime);

            var shouldActivate = activationSeconds <= 0f ? hasLight : _lightTimer >= activationSeconds;
            if (shouldActivate && !_isActive)
            {
                SetActive(true);
            }
            else if (!shouldActivate && _isActive && allowDeactivate)
            {
                SetActive(false);
            }
        }

        private void UpdateLightTimer(bool hasLight, float deltaTime)
        {
            if (activationSeconds <= 0f)
            {
                _lightTimer = hasLight ? activationSeconds : 0f;
                return;
            }

            if (hasLight)
            {
                _lightTimer = Mathf.Min(activationSeconds, _lightTimer + deltaTime);
            }
            else
            {
                _lightTimer = Mathf.Max(0f, _lightTimer - deltaTime);
            }
        }

        private int CountLampsInRange()
        {
            if (lightAffectRadius <= 0f)
            {
                return 0;
            }

            var lamps = this.SendQuery(new GetGameplayEnabledLampsQuery());
            var pos = (Vector2)transform.position;
            var count = 0;

            for (var i = 0; i < lamps.Count; i++)
            {
                var lampPos = (Vector2)lamps[i].WorldPos;
                if (Vector2.Distance(pos, lampPos) <= lightAffectRadius)
                {
                    count++;
                }
            }

            return count;
        }

        private void SetActive(bool active)
        {
            _isActive = active;
            this.SendEvent(new FlowerActivatedEvent
            {
                DoorId = doorId,
                FlowerId = flowerId,
                IsActive = active
            });

            if (active)
            {
                AudioService.Play("SFX-INT-0001", new AudioContext
                {
                    Owner = transform
                });
            }
            else
            {
                AudioService.Stop("SFX-INT-0001", new AudioContext
                {
                    Owner = transform
                });
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0.2f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, lightAffectRadius);
        }
    }
}
