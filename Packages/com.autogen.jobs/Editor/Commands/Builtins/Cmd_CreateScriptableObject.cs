using System.IO;
using AutoGenJobs.Core;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace AutoGenJobs.Commands.Builtins
{
    /// <summary>
    /// 创建 ScriptableObject 命令
    /// </summary>
    public class Cmd_CreateScriptableObject : IJobCommand
    {
        public string Name => "CreateScriptableObject";

        public CommandExecResult Execute(CommandContext ctx, JObject args)
        {
            if (args == null)
                return CommandExecResult.Fail("Missing args");

            var typeName = args["type"]?.ToString();
            if (string.IsNullOrEmpty(typeName))
                return CommandExecResult.Fail("Missing 'type' argument");

            var assetPath = args["assetPath"]?.ToString();
            if (string.IsNullOrEmpty(assetPath))
                return CommandExecResult.Fail("Missing 'assetPath' argument");

            var overwrite = args["overwrite"]?.ToObject<bool>() ?? false;
            var init = args["init"] as JObject;

            // 验证路径
            if (!ctx.ValidateWritePath(assetPath))
                return CommandExecResult.Fail($"Path not allowed: {assetPath}");

            // 解析类型
            var type = TypeResolver.ResolveType(typeName);
            if (type == null)
                return CommandExecResult.Fail($"Type not found: {typeName}");

            if (!TypeResolver.IsScriptableObjectType(type))
                return CommandExecResult.Fail($"Type is not a ScriptableObject: {typeName}");

            ctx.Logger.Info($"Creating ScriptableObject: {type.Name} at {assetPath}");

            if (ctx.DryRun)
            {
                ctx.Logger.Info("[DryRun] Would create ScriptableObject");
                return CommandExecResult.Ok("DryRun: would create ScriptableObject");
            }

            // 检查是否已存在
            var existing = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            if (existing != null)
            {
                if (!overwrite)
                {
                    ctx.Logger.Warning($"Asset already exists at {assetPath}, returning existing");
                    return CommandExecResult.Ok(
                        "Asset already exists",
                        new System.Collections.Generic.Dictionary<string, Object> { { "asset", existing } }
                    );
                }
                else
                {
                    AssetDatabase.DeleteAsset(assetPath);
                }
            }

            // 确保目录存在
            var directory = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directory))
            {
                var normalizedDir = PathUtil.NormalizeAssetPath(directory);
                if (!AssetDatabase.IsValidFolder(normalizedDir))
                {
                    // 逐级创建文件夹
                    var parts = normalizedDir.Split('/');
                    var currentPath = parts[0]; // "Assets"
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

            // 创建 ScriptableObject
            var so = ScriptableObject.CreateInstance(type);
            if (so == null)
                return CommandExecResult.Fail($"Failed to create instance of {typeName}");

            // 保存为资产
            AssetDatabase.CreateAsset(so, assetPath);
            EditorUtility.SetDirty(so);

            ctx.Logger.Info($"Created ScriptableObject at {assetPath}");

            // 如果有初始化字段，使用 SetSerializedProperty 命令处理
            if (init != null && init.HasValues)
            {
                ctx.Logger.Debug("Applying init properties...");

                var serializedObj = new SerializedObject(so);

                foreach (var prop in init.Properties())
                {
                    var serializedProp = serializedObj.FindProperty(prop.Name);
                    if (serializedProp == null)
                    {
                        ctx.Logger.Warning($"Property not found: {prop.Name}");
                        continue;
                    }

                    if (!SerializedPropertyHelper.SetValue(serializedProp, new ValueUnion(prop.Value), ctx))
                    {
                        ctx.Logger.Warning($"Failed to set property: {prop.Name}");
                    }
                }

                serializedObj.ApplyModifiedProperties();
                EditorUtility.SetDirty(so);
            }

            return CommandExecResult.Ok(
                $"Created {type.Name}",
                new System.Collections.Generic.Dictionary<string, Object> { { "asset", so } }
            );
        }
    }
}
