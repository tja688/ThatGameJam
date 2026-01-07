using AutoGenJobs.Core;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace AutoGenJobs.Commands.Builtins
{
    /// <summary>
    /// 创建 GameObject 命令
    /// 修复 B：在 Prefab 编辑模式下使用上下文感知的创建
    /// 修复 C：添加 ensure 语义，避免重复创建
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

            // ensure 模式：如果对象已存在，复用而不是重复创建
            var ensure = args["ensure"]?.ToObject<bool>() ?? false;

            // 用于 ensure 查找的唯一标记
            var ensureTag = args["ensureTag"]?.ToString();

            ctx.Logger.Info($"Creating GameObject: {name}" +
                           (string.IsNullOrEmpty(parentPath) ? "" : $" under {parentPath}") +
                           (ensure ? " (ensure mode)" : ""));

            if (ctx.DryRun)
            {
                ctx.Logger.Info("[DryRun] Would create GameObject");
                return CommandExecResult.Ok("DryRun: would create GameObject");
            }

            GameObject go = null;

            // ============================================================
            // 修复 C: ensure 模式 - 尝试查找已存在的对象
            // ============================================================
            if (ensure)
            {
                go = TryFindExisting(ctx, name, parentPath, ensureTag);
                if (go != null)
                {
                    ctx.Logger.Info($"[Ensure] Found existing GameObject: {go.name}");
                    // 更新 Transform（如果提供了新值）
                    ApplyTransform(go.transform, args, ctx);

                    return CommandExecResult.Ok(
                        $"Reused existing {name}",
                        new System.Collections.Generic.Dictionary<string, Object> { { "go", go } }
                    );
                }
            }

            // ============================================================
            // 修复 B: 使用上下文感知的创建方法
            // ============================================================
            go = ctx.CreateGameObject(name, parentPath);

            if (go == null)
                return CommandExecResult.Fail("Failed to create GameObject");

            // 注册 Undo
            if (!ctx.IsInPrefabEditMode)
            {
                Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            }

            // 如果提供了 ensureTag，添加标记（用于后续 ensure 查找）
            if (!string.IsNullOrEmpty(ensureTag))
            {
                // 使用一个隐藏组件来标记
                var marker = go.AddComponent<AutoGenMarker>();
                marker.tag = ensureTag;
                marker.hideFlags = HideFlags.HideInInspector;
            }

            // 设置 Transform
            ApplyTransform(go.transform, args, ctx);

            ctx.Logger.Info($"Created GameObject: {go.name}");

            return CommandExecResult.Ok(
                $"Created {name}",
                new System.Collections.Generic.Dictionary<string, Object> { { "go", go } }
            );
        }

        /// <summary>
        /// 尝试查找已存在的对象（ensure 模式）
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

    /// <summary>
    /// 用于 ensure 模式的标记组件
    /// 允许通过唯一标签查找已创建的对象
    /// </summary>
    [AddComponentMenu("")] // 不在菜单中显示
    public class AutoGenMarker : MonoBehaviour
    {
        [HideInInspector]
        public string tag;
    }
}
