using System.Collections.Generic;
using System.IO;
using AutoGenJobs.Core;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace AutoGenJobs.Commands.Builtins
{
    /// <summary>
    /// 创建或编辑 Prefab 命令
    /// 支持嵌套命令在 Prefab 内部执行
    /// 修复 B：使用 PrefabEditContext 隔离 Prefab 编辑，防止污染场景
    /// </summary>
    public class Cmd_CreateOrEditPrefab : IJobCommand
    {
        public string Name => "CreateOrEditPrefab";

        public CommandExecResult Execute(CommandContext ctx, JObject args)
        {
            if (args == null)
                return CommandExecResult.Fail("Missing args");

            var prefabPath = args["prefabPath"]?.ToString();
            if (string.IsNullOrEmpty(prefabPath))
                return CommandExecResult.Fail("Missing 'prefabPath' argument");

            // 验证路径
            if (!ctx.ValidateWritePath(prefabPath))
                return CommandExecResult.Fail($"Path not allowed: {prefabPath}");

            var rootName = args["rootName"]?.ToString() ?? Path.GetFileNameWithoutExtension(prefabPath);
            var editsToken = args["edits"] as JArray;

            ctx.Logger.Info($"Creating/editing prefab: {prefabPath}");

            if (ctx.DryRun)
            {
                ctx.Logger.Info("[DryRun] Would create/edit prefab");
                return CommandExecResult.Ok("DryRun: would create/edit prefab");
            }

            // 确保目录存在
            EnsureDirectoryExists(prefabPath);

            GameObject prefabRoot = null;
            bool isNew = false;

            // 检查 Prefab 是否已存在
            var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existingPrefab != null)
            {
                // 编辑现有 Prefab
                ctx.Logger.Debug("Loading existing prefab for editing");
                prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            }
            else
            {
                // 创建新 Prefab
                ctx.Logger.Debug($"Creating new prefab with root: {rootName}");
                var tempGo = new GameObject(rootName);

                // 先保存为 Prefab
                PrefabUtility.SaveAsPrefabAsset(tempGo, prefabPath);
                Object.DestroyImmediate(tempGo);

                // 重新加载以进行编辑
                prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
                isNew = true;
            }

            if (prefabRoot == null)
                return CommandExecResult.Fail("Failed to load/create prefab");

            try
            {
                // ============================================================
                // 修复 B: 进入 Prefab 编辑模式
                // 这会改变 CommandContext 的行为，使所有对象查找限制在 Prefab 内
                // ============================================================
                var prefabScene = prefabRoot.scene;
                ctx.EnterPrefabEditMode(prefabRoot, prefabScene);

                // 将 prefabRoot 注入变量表
                ctx.SetVar("$prefabRoot", prefabRoot);

                // 执行嵌套编辑命令
                if (editsToken != null && editsToken.Count > 0)
                {
                    ctx.Logger.Debug($"Executing {editsToken.Count} nested edits in Prefab context");

                    for (int i = 0; i < editsToken.Count; i++)
                    {
                        var editToken = editsToken[i];
                        if (editToken.Type != JTokenType.Object)
                            continue;

                        var editObj = editToken as JObject;
                        var cmdName = editObj?["cmd"]?.ToString();
                        var cmdArgs = editObj?["args"] as JObject;
                        var cmdOut = editObj?["out"] as JObject;

                        if (string.IsNullOrEmpty(cmdName))
                        {
                            ctx.Logger.Warning($"Nested edit [{i}] missing 'cmd'");
                            continue;
                        }

                        // 禁止在 Prefab 编辑中调用某些危险命令
                        if (IsProhibitedInPrefabEdit(cmdName))
                        {
                            ctx.Logger.Warning($"Command '{cmdName}' is not allowed in Prefab edit mode");
                            continue;
                        }

                        var command = CommandRegistry.GetCommand(cmdName);
                        if (command == null)
                        {
                            ctx.Logger.Warning($"Unknown nested command: {cmdName}");
                            continue;
                        }

                        ctx.Logger.Debug($"[PrefabEdit] Executing nested [{i}]: {cmdName}");
                        var result = command.Execute(ctx, cmdArgs);

                        if (!result.Success)
                        {
                            // 退出 Prefab 编辑模式
                            ctx.ExitPrefabEditMode();

                            // 保存当前状态并报告错误
                            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                            PrefabUtility.UnloadPrefabContents(prefabRoot);
                            return CommandExecResult.Fail($"Nested command [{i}] {cmdName} failed: {result.Message}", result.Exception);
                        }

                        // 处理输出变量
                        if (cmdOut != null && result.Outputs != null)
                        {
                            foreach (var outProp in cmdOut.Properties())
                            {
                                var varName = outProp.Value.ToString();
                                if (result.Outputs.TryGetValue(outProp.Name, out var outputObj))
                                {
                                    ctx.SetVar(varName, outputObj);
                                }
                            }
                        }
                    }
                }

                // 保存 Prefab
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                ctx.Logger.Info($"Saved prefab: {prefabPath}");
            }
            finally
            {
                // ============================================================
                // 修复 B: 退出 Prefab 编辑模式
                // ============================================================
                ctx.ExitPrefabEditMode();

                // 卸载 Prefab 内容
                if (prefabRoot != null)
                {
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                }
            }

            // 重新加载以获取最终资产引用
            var finalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            return CommandExecResult.Ok(
                isNew ? $"Created prefab {prefabPath}" : $"Edited prefab {prefabPath}",
                new Dictionary<string, Object> { { "prefab", finalPrefab } }
            );
        }

        /// <summary>
        /// 确保目录存在
        /// </summary>
        private void EnsureDirectoryExists(string assetPath)
        {
            var directory = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directory))
            {
                var normalizedDir = PathUtil.NormalizeAssetPath(directory);
                if (!AssetDatabase.IsValidFolder(normalizedDir))
                {
                    var parts = normalizedDir.Split('/');
                    var currentPath = parts[0];
                    for (int i = 1; i < parts.Length; i++)
                    {
                        var nextPath = currentPath + "/" + parts[i];
                        if (!AssetDatabase.IsValidFolder(nextPath))
                        {
                            AssetDatabase.CreateFolder(currentPath, parts[i]);
                        }
                        currentPath = nextPath;
                    }
                }
            }
        }

        /// <summary>
        /// 检查命令是否在 Prefab 编辑模式中被禁止
        /// </summary>
        private bool IsProhibitedInPrefabEdit(string cmdName)
        {
            // 这些命令操作场景或外部资产，在 Prefab 编辑中禁止
            var prohibited = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
            {
                "InstantiatePrefabInScene",  // 会操作场景
                "CreateOrEditPrefab",         // 不允许嵌套 Prefab 编辑
                "SaveAssets",                 // 可能导致意外保存
                "ImportAssets",               // 与 Prefab 编辑无关
            };

            return prohibited.Contains(cmdName);
        }
    }
}
