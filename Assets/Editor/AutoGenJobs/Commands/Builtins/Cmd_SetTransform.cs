using AutoGenJobs.Core;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace AutoGenJobs.Commands.Builtins
{
    /// <summary>
    /// 设置 Transform 命令
    /// </summary>
    public class Cmd_SetTransform : IJobCommand
    {
        public string Name => "SetTransform";

        public CommandExecResult Execute(CommandContext ctx, JObject args)
        {
            if (args == null)
                return CommandExecResult.Fail("Missing args");

            // 解析目标
            var targetToken = args["target"];
            if (targetToken == null)
                return CommandExecResult.Fail("Missing 'target' argument");

            var targetRef = TargetRef.FromJToken(targetToken);
            if (targetRef == null)
                return CommandExecResult.Fail("Invalid 'target' format");

            // 获取目标 GameObject
            var targetObj = ctx.ResolveTarget(targetRef);
            if (targetObj == null)
                return CommandExecResult.Fail($"Target not found: {targetRef.@ref ?? targetRef.scenePath}");

            Transform transform;
            if (targetObj is GameObject go)
            {
                transform = go.transform;
            }
            else if (targetObj is Component comp)
            {
                transform = comp.transform;
            }
            else
            {
                return CommandExecResult.Fail($"Target is not a GameObject or Component: {targetObj.GetType().Name}");
            }

            var space = args["space"]?.ToString()?.ToLower() ?? "local";
            bool isWorld = space == "world";

            ctx.Logger.Info($"Setting transform for {transform.gameObject.name} (space={space})");

            if (ctx.DryRun)
            {
                ctx.Logger.Info("[DryRun] Would set transform");
                return CommandExecResult.Ok("DryRun: would set transform");
            }

            Undo.RecordObject(transform, "Set Transform");

            // Position
            var posToken = args["position"];
            if (posToken != null)
            {
                var pos = ParseVector3(posToken);
                if (pos.HasValue)
                {
                    if (isWorld)
                        transform.position = pos.Value;
                    else
                        transform.localPosition = pos.Value;

                    ctx.Logger.Debug($"Set position: {pos.Value} ({space})");
                }
            }

            // Rotation
            var rotToken = args["rotation"];
            if (rotToken != null)
            {
                var rot = ParseVector3(rotToken);
                if (rot.HasValue)
                {
                    if (isWorld)
                        transform.eulerAngles = rot.Value;
                    else
                        transform.localEulerAngles = rot.Value;

                    ctx.Logger.Debug($"Set rotation: {rot.Value} ({space})");
                }
            }

            // Scale（只支持 local）
            var scaleToken = args["scale"];
            if (scaleToken != null)
            {
                var scale = ParseVector3(scaleToken);
                if (scale.HasValue)
                {
                    transform.localScale = scale.Value;
                    ctx.Logger.Debug($"Set scale: {scale.Value}");
                }
            }

            EditorUtility.SetDirty(transform.gameObject);

            return CommandExecResult.Ok($"Set transform for {transform.gameObject.name}");
        }

        private Vector3? ParseVector3(JToken token)
        {
            if (token == null) return null;

            // 数组形式 [x, y, z]
            if (token.Type == JTokenType.Array)
            {
                var arr = token.ToObject<float[]>();
                if (arr != null && arr.Length >= 3)
                {
                    return new Vector3(arr[0], arr[1], arr[2]);
                }
                else if (arr != null && arr.Length == 2)
                {
                    return new Vector3(arr[0], arr[1], 0);
                }
            }

            // 对象形式 { "x": 0, "y": 0, "z": 0 }
            if (token.Type == JTokenType.Object)
            {
                var obj = (JObject)token;
                var x = obj["x"]?.ToObject<float>() ?? 0;
                var y = obj["y"]?.ToObject<float>() ?? 0;
                var z = obj["z"]?.ToObject<float>() ?? 0;
                return new Vector3(x, y, z);
            }

            return null;
        }
    }
}
