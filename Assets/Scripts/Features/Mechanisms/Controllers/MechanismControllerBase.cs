using QFramework;
using ThatGameJam.Features.AreaSystem.Events;
using ThatGameJam.Features.Shared;
using UnityEngine;

namespace ThatGameJam.Features.Mechanisms.Controllers
{
    /// <summary>
    /// 机关/环境交互基础基类
    /// 统一处理重置逻辑、区域进入/离开钩子
    /// </summary>
    public abstract class MechanismControllerBase : MonoBehaviour, IController
    {
        [Header("基础配置")]
        [SerializeField] private string areaId; // 所属区域ID，用于区域性能管理

        [Header("重置设置")]
        [Tooltip("是否在收到 RunResetEvent (如玩家死亡、手动重开) 时执行 OnHardReset 复位逻辑")]
        [SerializeField] private bool shouldResetOnRunReset = false; // 策划要求的开关

        public string AreaId => areaId;

        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        protected virtual void OnEnable()
        {
            // 只有开启了复位需求的机关，才注册重置事件监听
            if (shouldResetOnRunReset)
            {
                this.RegisterEvent<RunResetEvent>(_ => OnHardReset())
                    .UnRegisterWhenDisabled(gameObject);
            }

            // 区域切换事件始终注册，用于处理进出钩子
            this.RegisterEvent<AreaChangedEvent>(OnAreaChanged)
                .UnRegisterWhenDisabled(gameObject);
        }

        /// <summary>
        /// 当重置事件触发且 shouldResetOnRunReset 为 true 时调用
        /// 子类在此实现具体的复位逻辑（如藤蔓缩回、门关闭）
        /// </summary>
        protected virtual void OnHardReset()
        {
        }

        /// <summary>
        /// 当玩家进入该机关所属 areaId 区域时触发
        /// </summary>
        protected virtual void OnAreaEnter()
        {
        }

        /// <summary>
        /// 当玩家离开该机关所属 areaId 区域时触发
        /// </summary>
        protected virtual void OnAreaExit()
        {
        }

        /// <summary>
        /// 处理区域变更事件
        /// </summary>
        private void OnAreaChanged(AreaChangedEvent e)
        {
            if (string.IsNullOrEmpty(areaId))
            {
                return;
            }

            // 逻辑判定：离开当前区域
            if (e.PreviousAreaId == areaId && e.CurrentAreaId != areaId)
            {
                OnAreaExit();
            }

            // 逻辑判定：进入当前区域
            if (e.CurrentAreaId == areaId && e.PreviousAreaId != areaId)
            {
                OnAreaEnter();
            }
        }
    }
}