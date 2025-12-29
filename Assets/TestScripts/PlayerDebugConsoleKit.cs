using System;
using QFramework;
using ThatGameJam.Features.PlayerCharacter2D.Controllers;
using ThatGameJam.Features.PlayerCharacter2D.Models;
using UnityEngine;

namespace Unity.Debug
{
    /// <summary>
    /// 为 PlayerCharacter2D 专门构建的调试控制台模块
    /// </summary>
    public class PlayerDebugConsoleModule : ConsoleModule
    {
        public override string Title => "Player Debug";

        public override void DrawGUI()
        {
            var model = Architecture<GameRootApp>.Interface.GetModel<IPlayerCharacter2DModel>();
            if (model == null)
            {
                GUILayout.Label("<color=red>Player Model not found. Make sure GameRootApp is initialized.</color>");
                return;
            }

            // 1. 跳跃系统状态
            DrawSection("Jump System", () =>
            {
                DrawLabel("Grounded", model.Grounded.Value);
                DrawLabel("Coyote Usable", model.CoyoteUsable);
                DrawLabel("Buffered Jump Usable", model.BufferedJumpUsable);
                DrawLabel("Jump To Consume", model.JumpToConsume);
                DrawLabel("Ended Jump Early", model.EndedJumpEarly);
                DrawLabel("Time Since Jump Press", $"{model.Time - model.TimeJumpWasPressed:F2} (Time: {model.Time:F2}, LastPress: {model.TimeJumpWasPressed:F2})");
            });

            GUILayout.Space(5);

            // 2. 触发器与检测信息
            DrawSection("Detection & Physics", () =>
            {
                DrawLabel("Velocity", model.Velocity.Value.ToString("F2"));
                DrawLabel("Wall Contact Timer", $"{model.WallContactTimer:F2}");
                DrawLabel("Climb Wall Side", model.ClimbWallSide == 0 ? "None" : (model.ClimbWallSide > 0 ? "Right (+1)" : "Left (-1)"));
                DrawLabel("Regrab Lockout", $"{model.RegrabLockoutTimer:F2}");
            });

            GUILayout.Space(5);

            // 3. 攀爬系统与按键响应
            DrawSection("Climbing & Input", () =>
            {
                DrawLabel("Is Climbing", model.IsClimbing.Value, model.IsClimbing.Value ? Color.green : Color.white);
                DrawLabel("Grab Held", model.FrameInput.GrabHeld);
                DrawLabel("Move X (Horizontal)", model.FrameInput.Move.x.ToString("F2"));
                DrawLabel("Move Y (Climb Up/Down)", model.FrameInput.Move.y.ToString("F2"),
                    Mathf.Abs(model.FrameInput.Move.y) > 0.1f ? Color.cyan : Color.white);

                if (model.IsClimbing.Value)
                {
                    string climbStatus = "Idle/Sliding";
                    if (model.FrameInput.Move.y > 0.1f) climbStatus = "Climbing UP";
                    else if (model.FrameInput.Move.y < -0.1f) climbStatus = "Climbing DOWN";
                    DrawLabel("Climb Status", climbStatus, Color.yellow);
                }
            });
        }

        private void DrawSection(string title, Action content)
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label($"<color=orange><b>[{title}]</b></color>");
            content?.Invoke();
            GUILayout.EndVertical();
        }

        private void DrawLabel(string label, object value, Color? valueColor = null)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label + ":", GUILayout.Width(150));
            var colorStr = valueColor.HasValue ? ColorUtility.ToHtmlStringRGB(valueColor.Value) : "FFFFFF";
            GUILayout.Label($"<color=#{colorStr}>{value}</color>");
            GUILayout.EndHorizontal();
        }
    }

    /// <summary>
    /// 自动注册调试模块
    /// </summary>
    public static class PlayerDebugConsoleKit
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Register()
        {
            // 移除已存在的同名模块防止重复（如果多次加载）
            ConsoleKit.AddModule(new PlayerDebugConsoleModule());

            // 如果场景中没有 ConsoleWindow，可以考虑创建一个，或者由用户手动开启
            ConsoleKit.CreateWindow(); 
        }

#if UNITY_EDITOR
        // 提供一个手动开启的方法，方便在编辑器或其他地方调用
        [UnityEditor.MenuItem("Tools/Debug/Open Console Kit")]
        public static void OpenConsole()
        {
            var window = GameObject.FindObjectOfType<ConsoleWindow>();
            if (window == null)
            {
                ConsoleKit.CreateWindow();
            }
        }
#endif
    }
}
