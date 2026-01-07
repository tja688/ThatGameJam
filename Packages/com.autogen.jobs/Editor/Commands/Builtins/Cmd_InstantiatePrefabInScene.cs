using AutoGenJobs.Core;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace AutoGenJobs.Commands.Builtins
{
    /// <summary>
    /// 在场景中实例化 Prefab 命令
    /// 修复 C：添加 ensure 语义，避免重复实例化
    /// </summary>
    public class Cmd_InstantiatePrefabInScene : IJobCommand
    {
        public string Name => "InstantiatePrefabInScene";

        public CommandExecResult Execute(CommandContext ctx, JObject args)
        {
            if (args == null)
                return CommandExecResult.Fail("Missing args");

            // 检查是否在 Prefab 编辑模式（不允许）
            if (ctx.IsInPrefabEditMode)
            {
                return CommandExecResult.Fail("InstantiatePrefabInScene is not allowed in Prefab edit mode");
            }

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

            // ensure 模式：如果对象已存在，复用而不是重复创建
            var ensure = args["ensure"]?.ToObject<bool>() ?? false;
            var ensureTag = args["ensureTag"]?.ToString();

            var finalName = nameOverride ?? prefab.name;

            ctx.Logger.Info($"Instantiating prefab: {prefab.name}" +
                           (string.IsNullOrEmpty(parentPath) ? "" : $" under {parentPath}") +
                           (string.IsNullOrEmpty(nameOverride) ? "" : $" as {nameOverride}") +
                           (ensure ? " (ensure mode)" : ""));

            if (ctx.DryRun)
            {
                ctx.Logger.Info("[DryRun] Would instantiate prefab");
                return CommandExecResult.Ok("DryRun: would instantiate prefab");
            }

            GameObject instance = null;

            // ============================================================
            // 修复 C: ensure 模式 - 尝试查找已存在的实例
            // ============================================================
            if (ensure)
            {
                instance = TryFindExisting(ctx, finalName, parentPath, ensureTag);
                if (instance != null)
                {
                    ctx.Logger.Info($"[Ensure] Found existing instance: {instance.name}");

                    // 更新位置（如果提供了新值）
                    var posToken = args["position"];
                    if (posToken != null)
                    {
                        var pos = ParseVector3(posToken);
                        if (pos.HasValue)
                        {
                            instance.transform.localPosition = pos.Value;
                        }
                    }

                    return CommandExecResult.Ok(
                        $"Reused existing instance {instance.name}",
                        new System.Collections.Generic.Dictionary<string, Object> { { "instance", instance } }
                    );
                }
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
            instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
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

            // 如果提供了 ensureTag，添加标记
            if (!string.IsNullOrEmpty(ensureTag))
            {
                var marker = instance.AddComponent<AutoGenMarker>();
                marker.tag = ensureTag;
                marker.hideFlags = HideFlags.HideInInspector;
            }

            // 设置位置（可选）
            var positionToken = args["position"];
            if (positionToken != null)
            {
                var pos = ParseVector3(positionToken);
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

        /// <summary>
        /// 尝试查找已存在的实例（ensure 模式）
        /// </summary>
        private GameObject TryFindExisting(CommandContext ctx, string name, string parentPath, string ensureTag)
        {
            // 优先通过 ensureTag 查找
            if (!string.IsNullOrEmpty(ensureTag))
            {
                var markers = Object.FindObjectsOfType<AutoGenMarker>();
                foreach (var marker in markers)
                {
                    if (marker.tag == ensureTag)
                    {
                        return marker.gameObject;
                    }
                }
            }

            // 通过路径查找
            string fullPath;
            if (string.IsNullOrEmpty(parentPath))
            {
                fullPath = name;
            }
            else
            {
                fullPath = $"{parentPath}/{name}";
            }

            return ctx.FindGameObjectByPath(fullPath);
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
