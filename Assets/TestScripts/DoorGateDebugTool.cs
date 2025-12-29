using QFramework;
using ThatGameJam.Features.DoorGate.Models;
using ThatGameJam.Features.DoorGate.Events;
using UnityEngine;
using System.Collections.Generic;
    /// <summary>
    /// 门禁系统实时监控工具
    /// 挂载在场景中任意物体上即可运行
    /// </summary>
    public class DoorGateDebugTool : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => GameRootApp.Interface;

        [Header("运行时状态监控 (只读)")]
        [SerializeField] private List<string> activeDoorsStatus = new List<string>();

        private void Start()
        {
            // 监听事件，确认逻辑层是否发出了信号
            this.RegisterEvent<DoorStateChangedEvent>(e =>
            {
                Debug.Log($"<color=cyan>[Debug] 收到事件 - 门ID: {e.DoorId}, 状态: {(e.IsOpen ? "开启" : "关闭")}</color>");
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            this.RegisterEvent<DoorOpenEvent>(e =>
            {
                Debug.Log($"<color=green>[Debug] 收到开门广播 - 门ID: {e.DoorId}</color>");
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        private void Update()
        {
            // 每秒更新一次状态列表，避免消耗性能
            if (Time.frameCount % 60 == 0)
            {
                RefreshStatus();
            }
        }

        private void RefreshStatus()
        {
            activeDoorsStatus.Clear();
            var model = this.GetModel<IDoorGateModel>();
            if (model == null)
            {
                activeDoorsStatus.Add("错误: 找不到 IDoorGateModel!");
                return;
            }

            // 获取所有已注册的门状态
            var doors = (model as DoorGateModel)?.GetAllDoors();
            if (doors == null || doors.Count == 0)
            {
                activeDoorsStatus.Add("警告: 模型中没有任何已注册的门");
                return;
            }

            foreach (var state in doors)
            {
                string info = $"ID: {state.DoorId} | " +
                             $"花朵进度: {state.ActiveFlowerCount}/{state.RequiredFlowerCount} | " +
                             $"逻辑状态: {(state.IsOpen ? "已开启" : "未开启")}";
                activeDoorsStatus.Add(info);
            }
        }

        [ContextMenu("强制开启当前场景所有门")]
        public void ForceOpenAll()
        {
             var model = this.GetModel<IDoorGateModel>();
             var doors = (model as DoorGateModel)?.GetAllDoors();
             if (doors != null)
             {
                 foreach(var d in doors)
                 {
                     this.SendCommand(new ThatGameJam.Features.DoorGate.Commands.SetDoorStateCommand(d.DoorId, true));
                 }
             }
        }
    }