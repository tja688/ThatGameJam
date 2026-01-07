using AutoGenJobs.Core;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace AutoGenJobs.Commands.Builtins
{
    /// <summary>
    /// Ping 对象命令（调试用）
    /// 在 Project 或 Hierarchy 窗口中高亮显示对象
    /// </summary>
    public class Cmd_PingObject : IJobCommand
    {
        public string Name => "PingObject";

        public CommandExecResult Execute(CommandContext ctx, JObject args)
        {
            if (args == null)
                return CommandExecResult.Fail("Missing args");

            var targetToken = args["target"];
            if (targetToken == null)
                return CommandExecResult.Fail("Missing 'target' argument");

            var targetRef = TargetRef.FromJToken(targetToken);
            if (targetRef == null)
                return CommandExecResult.Fail("Invalid 'target' format");

            var targetObj = ctx.ResolveTarget(targetRef);
            if (targetObj == null)
                return CommandExecResult.Fail($"Target not found: {targetRef.@ref ?? targetRef.scenePath ?? targetRef.assetPath}");

            ctx.Logger.Info($"Pinging object: {targetObj.name}");

            if (ctx.DryRun)
            {
                ctx.Logger.Info("[DryRun] Would ping object");
                return CommandExecResult.Ok("DryRun: would ping object");
            }

            EditorGUIUtility.PingObject(targetObj);

            return CommandExecResult.Ok($"Pinged {targetObj.name}");
        }
    }

    /// <summary>
    /// 选中对象命令（调试用）
    /// 在 Project 或 Hierarchy 窗口中选中对象
    /// </summary>
    public class Cmd_SelectObject : IJobCommand
    {
        public string Name => "SelectObject";

        public CommandExecResult Execute(CommandContext ctx, JObject args)
        {
            if (args == null)
                return CommandExecResult.Fail("Missing args");

            var targetToken = args["target"];
            if (targetToken == null)
                return CommandExecResult.Fail("Missing 'target' argument");

            var targetRef = TargetRef.FromJToken(targetToken);
            if (targetRef == null)
                return CommandExecResult.Fail("Invalid 'target' format");

            var targetObj = ctx.ResolveTarget(targetRef);
            if (targetObj == null)
                return CommandExecResult.Fail($"Target not found: {targetRef.@ref ?? targetRef.scenePath ?? targetRef.assetPath}");

            ctx.Logger.Info($"Selecting object: {targetObj.name}");

            if (ctx.DryRun)
            {
                ctx.Logger.Info("[DryRun] Would select object");
                return CommandExecResult.Ok("DryRun: would select object");
            }

            Selection.activeObject = targetObj;

            return CommandExecResult.Ok($"Selected {targetObj.name}");
        }
    }
}
