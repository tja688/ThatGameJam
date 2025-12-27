using System.Collections;
using QFramework;
using ThatGameJam.Features.LightVitality.Commands;
using ThatGameJam.Features.LightVitality.Queries;
using ThatGameJam.Features.DeathRespawn.Commands; // 引入死亡命令
using ThatGameJam.Features.Shared;
using UnityEngine;

namespace ThatGameJam.Features.IceBlock.Controllers
{
    public class IceBlock2D : MonoBehaviour, IController
    {
        [Header("核心引用")]
        [SerializeField] private Collider2D triggerCollider; 
        [SerializeField] private Collider2D solidCollider;   
        [SerializeField] private SpriteRenderer visualRenderer; // 替换原有的GameObject

        [Header("消融/凝结配置")]
        [SerializeField] private Color meltedColor = new Color(1, 1, 1, 0.2f); // 消融后的颜色/透明度
        [SerializeField] private float transitionDuration = 2f;               // 渐变耗时
        [SerializeField] private float waitDuration = 5f;                     // 物理消失后的持续时间
        [SerializeField] private float lightCostRatio = 0.25f;

        private Color _originalColor;
        private bool _isProcessing = false; // 防止重复触发
        private Coroutine _activeRoutine;

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

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isProcessing || !other.CompareTag("Player")) return;
            StartCoroutine(MeltSequence());
        }

        private IEnumerator MeltSequence()
        {
            _isProcessing = true;

            // 1. 扣除光量
            var maxLight = this.SendQuery(new GetMaxLightQuery());
            this.SendCommand(new ConsumeLightCommand(maxLight * lightCostRatio, ELightConsumeReason.Script));

            // 2. 消融阶段：颜色渐变，此时物理碰撞还在
            yield return StartCoroutine(FadeRoutine(_originalColor, meltedColor, transitionDuration));

            // 3. 物理消失：玩家可通过
            if (solidCollider != null) solidCollider.enabled = false;

            // 4. 等待阶段
            yield return new WaitForSeconds(waitDuration);

            // 5. 凝结阶段：颜色恢复，此时物理碰撞还没回来
            yield return StartCoroutine(FadeRoutine(meltedColor, _originalColor, transitionDuration));

            // 6. 物理恢复并检查死亡判定
            if (solidCollider != null)
            {
                solidCollider.enabled = true;
                CheckOverlapDeath();
            }

            _isProcessing = false;
        }

        private IEnumerator FadeRoutine(Color from, Color to, float duration)
        {
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (visualRenderer != null)
                {
                    visualRenderer.color = Color.Lerp(from, to, elapsed / duration);
                }
                yield return null;
            }
            if (visualRenderer != null) visualRenderer.color = to;
        }

        /// <summary>
        /// 检查物理碰撞恢复瞬间是否卡住了玩家
        /// </summary>
        private void CheckOverlapDeath()
        {
            if (solidCollider == null) return;

            // 检查 solidCollider 内部是否有 Player 标签的对象
            ContactFilter2D filter = new ContactFilter2D().NoFilter();
            Collider2D[] results = new Collider2D[5];
            int count = solidCollider.Overlap(filter, results);

            for (int i = 0; i < count; i++)
            {
                if (results[i].CompareTag("Player"))
                {
                    // 触发死亡命令
                    this.SendCommand(new MarkPlayerDeadCommand(EDeathReason.Script, transform.position));
                    break;
                }
            }
        }

        private void ResetToInitial()
        {
            StopAllCoroutines();
            _isProcessing = false;
            if (solidCollider != null) solidCollider.enabled = true;
            if (visualRenderer != null) visualRenderer.color = _originalColor;
        }
    }
}