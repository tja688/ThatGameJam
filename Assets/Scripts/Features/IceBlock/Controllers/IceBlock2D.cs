using System.Collections;
using QFramework;
using ThatGameJam.Independents.Audio;
using ThatGameJam.Features.DeathRespawn.Commands; // 引入死亡命令
using ThatGameJam.Features.LightVitality.Commands;
using ThatGameJam.Features.LightVitality.Queries;
using ThatGameJam.Features.Shared;
using UnityEngine;

namespace ThatGameJam.Features.IceBlock.Controllers
{
    public class IceBlock2D : MonoBehaviour, IController
    {
        [Header("核心引用")]
        [SerializeField] private Collider2D triggerCollider;

        [SerializeField] private Collider2D solidCollider;

        [SerializeField] private SpriteRenderer visualRenderer;


        [Header("消融配置 (新)")]
        [SerializeField] private float meltLightAmount = 50f;     // 熔化所需的光亮值总额
        [SerializeField] private float meltSpeed = 25f;           // 每秒扣除光亮值的速度
        [SerializeField] private float recoveryDelay = 1f;        // 停止接触后等待多久开始恢复
        [SerializeField] private float recoverySpeed = 10f;       // 每秒恢复进度的速度 (光亮值单位)

        [Header("消融/凝结配置 (保持旧有)")]
        [SerializeField] private Color meltedColor = new Color(1, 1, 1, 0.2f); // 消融后的目标外观
        [SerializeField] private float waitDuration = 5f;                     // 熔化后的开启状态持续时间
        [SerializeField] private float transitionFadeInDuration = 2f;         // 被动凝结恢复时的渐变时间

        private Color _originalColor;
        private bool _isProcessing = false; // 是否处于熔化后的开启/重置等待阶段
        private float _currentProgress = 0f; // 当前已扣除的光亮值进度
        private float _idleTimer = 0f;       // 玩家离开后的计时器
        private int _playerInTriggerCount = 0;
        private bool _meltLoopPlaying = false;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        private void Awake()
        {
            if (visualRenderer != null) _originalColor = visualRenderer.color;
            if (triggerCollider == null) triggerCollider = GetComponent<Collider2D>();
        }

        private void OnEnable()
        {
            this.RegisterEvent<RunResetEvent>(_ => ResetToInitial())
                .UnRegisterWhenDisabled(gameObject);
            ResetToInitial();
        }

        private void Update()
        {
            if (_isProcessing) return;

            bool isTouching = _playerInTriggerCount > 0;

            if (isTouching)
            {
                _idleTimer = 0f;
                if (_currentProgress < meltLightAmount)
                {
                    HandleMelting();
                }
            }
            else if (_currentProgress > 0)
            {
                _idleTimer += Time.deltaTime;
                if (_idleTimer >= recoveryDelay)
                {
                    HandleRecovery();
                }
            }

            var shouldLoop = isTouching && _currentProgress < meltLightAmount && !_isProcessing;
            if (shouldLoop && !_meltLoopPlaying)
            {
                _meltLoopPlaying = true;
                AudioService.Play("SFX-INT-0005", new AudioContext
                {
                    Owner = transform
                });
            }
            else if (!shouldLoop && _meltLoopPlaying)
            {
                _meltLoopPlaying = false;
                AudioService.Stop("SFX-INT-0005", new AudioContext
                {
                    Owner = transform
                });
            }
        }

        private void HandleMelting()
        {
            float amountToDeduct = meltSpeed * Time.deltaTime;

            // 确保不扣除超过剩余需求的量

            float remaining = meltLightAmount - _currentProgress;
            if (amountToDeduct > remaining) amountToDeduct = remaining;

            _currentProgress += amountToDeduct;

            // 申请扣除玩家光亮值

            this.SendCommand(new ConsumeLightCommand(amountToDeduct, ELightConsumeReason.Script));

            UpdateVisuals();

            if (_currentProgress >= meltLightAmount)
            {
                StartCoroutine(CompleteMeltSequence());
            }
        }

        private void HandleRecovery()
        {
            _currentProgress -= recoverySpeed * Time.deltaTime;
            if (_currentProgress < 0) _currentProgress = 0;


            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (visualRenderer == null) return;
            float t = Mathf.Clamp01(_currentProgress / meltLightAmount);
            visualRenderer.color = Color.Lerp(_originalColor, meltedColor, t);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                _playerInTriggerCount++;
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                _playerInTriggerCount--;
                if (_playerInTriggerCount < 0) _playerInTriggerCount = 0;
            }
        }

        /// <summary>
        /// 当进度扣满后，进入老路子的“物理消失->等待->凝结恢复”逻辑
        /// </summary>
        private IEnumerator CompleteMeltSequence()
        {
            _isProcessing = true;
            if (_meltLoopPlaying)
            {
                _meltLoopPlaying = false;
                AudioService.Stop("SFX-INT-0005", new AudioContext
                {
                    Owner = transform
                });
            }

            AudioService.Play("SFX-INT-0006", new AudioContext
            {
                Position = transform.position,
                HasPosition = true
            });

            // 确保视觉达到熔化状态

            if (visualRenderer != null) visualRenderer.color = meltedColor;

            // 1. 物理消失
            if (solidCollider != null) solidCollider.enabled = false;

            // 2. 等待阶段
            yield return new WaitForSeconds(waitDuration);

            // 3. 凝结阶段 (老样子)：颜色渐变回初始值
            float elapsed = 0;
            while (elapsed < transitionFadeInDuration)
            {
                elapsed += Time.deltaTime;
                if (visualRenderer != null)
                {
                    visualRenderer.color = Color.Lerp(meltedColor, _originalColor, elapsed / transitionFadeInDuration);
                }
                yield return null;
            }
            if (visualRenderer != null) visualRenderer.color = _originalColor;
            AudioService.Play("SFX-INT-0006", new AudioContext
            {
                Position = transform.position,
                HasPosition = true
            });

            // 4. 物理恢复并检查死亡判定
            if (solidCollider != null)
            {
                solidCollider.enabled = true;
                CheckOverlapDeath();
            }

            _currentProgress = 0;
            _isProcessing = false;
        }

        private void CheckOverlapDeath()
        {
            if (solidCollider == null) return;

            ContactFilter2D filter = new ContactFilter2D().NoFilter();
            filter.useTriggers = false; // 死亡判定通常针对实体碰撞
            Collider2D[] results = new Collider2D[5];
            int count = solidCollider.Overlap(filter, results);

            for (int i = 0; i < count; i++)
            {
                if (results[i].CompareTag("Player"))
                {
                    this.SendCommand(new MarkPlayerDeadCommand(EDeathReason.Script, transform.position));
                    break;
                }
            }
        }

        private void ResetToInitial()
        {
            StopAllCoroutines();
            _isProcessing = false;
            _currentProgress = 0;
            _idleTimer = 0;
            _playerInTriggerCount = 0;
            if (_meltLoopPlaying)
            {
                _meltLoopPlaying = false;
                AudioService.Stop("SFX-INT-0005", new AudioContext
                {
                    Owner = transform
                });
            }
            if (solidCollider != null) solidCollider.enabled = true;
            if (visualRenderer != null) visualRenderer.color = _originalColor;
        }
    }
}
