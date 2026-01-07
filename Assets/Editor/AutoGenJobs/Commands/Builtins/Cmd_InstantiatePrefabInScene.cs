using AutoGenJobs.Core;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace AutoGenJobs.Commands.Builtins
{
    /// <summary>
    /// 在场景中实例化 Prefab 命令
    /// </summary>
    public class Cmd_InstantiatePrefabInScene : IJobCommand
    {
        public string Name => "InstantiatePrefabInScene";

        public CommandExecResult Execute(CommandContext ctx, JObject args)
        {
            if (args == null)
                return CommandExecResult.Fail("Missing args");

            // 获取 Prefab
            var prefabGuid = args["prefabGuid"]?.ToString();
            var prefabPath = args["prefabPath"]?.ToString();

            if (string.IsNullOrEmpty(prefabGuid) && string.IsNullOrEmpty(prefabPath))
                return CommandExecResult.Fail("Missing 'prefabGuid' or 'prefabPath' argument");

            var prefab = Core.AssetRef.Load<GameObject>(prefabGuid, prefabPath);
            if (prefab == null)
                return CommandExecResult.Fail($"Prefab not found: {prefabPath ?? prefabGuid}");

            var parentPath = args["parentPath"]?.ToString();
            var nameOverride = args["nameOverride"]?.ToString();

            ctx.Logger.Info($"Instantiating prefab: {prefab.name}" +
                           (string.IsNullOrEmpty(parentPath) ? "" : $" under {parentPath}") +
                           (string.IsNullOrEmpty(nameOverride) ? "" : $" as {nameOverride}"));

            if (ctx.DryRun)
            {
                ctx.Logger.Info("[DryRun] Would instantiate prefab");
                return CommandExecResult.Ok("DryRun: would instantiate prefab");
            }

            // 查找父对象
            Transform parent = null;
            if (!string.IsNullOrEmpty(parentPath))
            {
                var parentGo = ctx.FindGameObjectByPath(parentPath);
                if (parentGo != null)
                {
                    parent = parentGo.transform;
                }
                else
                {
                    ctx.Logger.Warning($"Parent not found: {parentPath}, instantiating at root");
                }
            }

            // 实例化
            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                // Fallback: 使用 Object.Instantiate
                instance = Object.Instantiate(prefab);
            }

            if (instance == null)
                return CommandExecResult.Fail("Failed to instantiate prefab");

            Undo.RegisterCreatedObjectUndo(instance, $"Instantiate {prefab.name}");

            // 设置父对象
            if (parent != null)
            {
                instance.transform.SetParent(parent, false);
            }

            // 重命名
            if (!string.IsNullOrEmpty(nameOverride))
            {
                instance.name = nameOverride;
            }

            // 设置位置（可选）
            var posToken = args["position"];
            if (posToken != null)
            {
                var pos = ParseVector3(posToken);
                if (pos.HasValue)
                {
                    instance.transform.localPosition = pos.Value;
                }
            }

            ctx.Logger.Info($"Instantiated {instance.name} in scene");

            return CommandExecResult.Ok(
                $"Instantiated {instance.name}",
                new System.Collections.Generic.Dictionary<string, Object> { { "instance", instance } }
            );
        }

        private Vector3? ParseVector3(JToken token)
        {
            if (token == null) return null;

            if (token.Type == JTokenType.Array)
            {
                var arr = token.ToObject<float[]>();
                if (arr != null && arr.Length >= 3)
                {
                    return new Vector3(arr[0], arr[1], arr[2]);
                }
            }

            if (token.Type == JTokenType.Object)
            {
                var obj = (JObject)token;
                return new Vector3(
                    obj["x"]?.ToObject<float>() ?? 0,
                    obj["y"]?.ToObject<float>() ?? 0,
                    obj["z"]?.ToObject<float>() ?? 0
                );
            }

            return null;
        }
    }
}
