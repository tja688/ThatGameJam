using AutoGenJobs.Core;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace AutoGenJobs.Commands.Builtins
{
    /// <summary>
    /// 创建 GameObject 命令
    /// </summary>
    public class Cmd_CreateGameObject : IJobCommand
    {
        public string Name => "CreateGameObject";

        public CommandExecResult Execute(CommandContext ctx, JObject args)
        {
            if (args == null)
                return CommandExecResult.Fail("Missing args");

            var name = args["name"]?.ToString();
            if (string.IsNullOrEmpty(name))
                return CommandExecResult.Fail("Missing 'name' argument");

            var parentPath = args["parentPath"]?.ToString();

            ctx.Logger.Info($"Creating GameObject: {name}" + (string.IsNullOrEmpty(parentPath) ? "" : $" under {parentPath}"));

            if (ctx.DryRun)
            {
                ctx.Logger.Info("[DryRun] Would create GameObject");
                return CommandExecResult.Ok("DryRun: would create GameObject");
            }

            // 创建 GameObject
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");

            // 设置父对象
            if (!string.IsNullOrEmpty(parentPath))
            {
                var parent = ctx.FindGameObjectByPath(parentPath);
                if (parent == null)
                {
                    ctx.Logger.Warning($"Parent not found: {parentPath}, creating at root");
                }
                else
                {
                    go.transform.SetParent(parent.transform, false);
                }
            }

            // 设置 Transform（可选）
            ApplyTransform(go.transform, args, ctx);

            ctx.Logger.Info($"Created GameObject: {go.name}");

            return CommandExecResult.Ok(
                $"Created {name}",
                new System.Collections.Generic.Dictionary<string, Object> { { "go", go } }
            );
        }

        private void ApplyTransform(Transform transform, JObject args, CommandContext ctx)
        {
            // Position
            var posToken = args["position"];
            if (posToken != null)
            {
                var pos = ParseVector3(posToken);
                if (pos.HasValue)
                {
                    transform.localPosition = pos.Value;
                    ctx.Logger.Debug($"Set position: {pos.Value}");
                }
            }

            // Rotation
            var rotToken = args["rotation"];
            if (rotToken != null)
            {
                var rot = ParseVector3(rotToken);
                if (rot.HasValue)
                {
                    transform.localEulerAngles = rot.Value;
                    ctx.Logger.Debug($"Set rotation: {rot.Value}");
                }
            }

            // Scale
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
