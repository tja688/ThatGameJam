using System.Collections;
using QFramework;
using ThatGameJam.Features.LightVitality.Commands;
using ThatGameJam.Features.LightVitality.Queries;
using ThatGameJam.Features.Shared;
using UnityEngine;

namespace ThatGameJam.Features.IceBlock.Controllers
{
    [RequireComponent(typeof(Collider2D))]
    public class IceBlock2D : MonoBehaviour, IController
    {
        [SerializeField] private Collider2D triggerCollider;
        [SerializeField] private Collider2D solidCollider;
        [SerializeField] private GameObject frozenVisual;
        [SerializeField] private GameObject meltedVisual;
        [SerializeField] private float lightCostRatio = 0.25f;
        [SerializeField] private float recoverSeconds = 5f;
        [SerializeField] private string playerTag = "Player";

        private bool _isFrozen = true;
        private Coroutine _recoverRoutine;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void Awake()
        {
            if (triggerCollider == null)
            {
                triggerCollider = GetComponent<Collider2D>();
            }

            if (triggerCollider != null && !triggerCollider.isTrigger)
            {
                LogKit.W("IceBlock2D expects triggerCollider.isTrigger = true.");
            }
        }

        private void OnEnable()
        {
            this.RegisterEvent<RunResetEvent>(_ => ForceFrozen())
                .UnRegisterWhenDisabled(gameObject);
            SetFrozen(true);
        }

        private void OnDisable()
        {
            if (_recoverRoutine != null)
            {
                StopCoroutine(_recoverRoutine);
                _recoverRoutine = null;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isFrozen || !IsPlayer(other))
            {
                return;
            }

            Melt();
        }

        private bool IsPlayer(Collider2D other)
        {
            if (string.IsNullOrEmpty(playerTag))
            {
                return true;
            }

            return other.CompareTag(playerTag);
        }

        private void Melt()
        {
            if (!_isFrozen)
            {
                return;
            }

            _isFrozen = false;
            UpdateVisuals();

            if (solidCollider != null)
            {
                solidCollider.enabled = false;
            }

            var maxLight = this.SendQuery(new GetMaxLightQuery());
            var amount = maxLight * Mathf.Clamp01(lightCostRatio);
            if (amount > 0f)
            {
                this.SendCommand(new ConsumeLightCommand(amount, ELightConsumeReason.Script));
            }

            if (_recoverRoutine != null)
            {
                StopCoroutine(_recoverRoutine);
            }

            _recoverRoutine = StartCoroutine(RecoverRoutine());
        }

        private IEnumerator RecoverRoutine()
        {
            if (recoverSeconds > 0f)
            {
                yield return new WaitForSeconds(recoverSeconds);
            }

            _recoverRoutine = null;
            SetFrozen(true);
        }

        private void ForceFrozen()
        {
            if (_recoverRoutine != null)
            {
                StopCoroutine(_recoverRoutine);
                _recoverRoutine = null;
            }

            SetFrozen(true);
        }

        private void SetFrozen(bool frozen)
        {
            _isFrozen = frozen;

            if (solidCollider != null)
            {
                solidCollider.enabled = frozen;
            }

            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (frozenVisual != null)
            {
                frozenVisual.SetActive(_isFrozen);
            }

            if (meltedVisual != null)
            {
                meltedVisual.SetActive(!_isFrozen);
            }
        }
    }
}
