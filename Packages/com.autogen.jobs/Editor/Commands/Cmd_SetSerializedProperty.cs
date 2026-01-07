using AutoGenJobs.Core;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace AutoGenJobs.Commands.Builtins
{
    /// <summary>
    /// 设置 SerializedProperty 命令
    /// 核心通用命令，支持设置任意类型的序列化字段
    /// 支持 Prefab 编辑模式
    /// </summary>
    public class Cmd_SetSerializedProperty : IJobCommand
    {
        public string Name => "SetSerializedProperty";

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

            // 解析属性路径
            var propertyPath = args["propertyPath"]?.ToString();
            if (string.IsNullOrEmpty(propertyPath))
                return CommandExecResult.Fail("Missing 'propertyPath' argument");

            // 解析值
            var valueToken = args["value"];
            if (valueToken == null)
                return CommandExecResult.Fail("Missing 'value' argument");

            var ignoreMissing = args["ignoreMissing"]?.ToObject<bool>() ?? false;

            // 获取目标对象
            var targetObj = ctx.ResolveTarget(targetRef);
            if (targetObj == null)
                return CommandExecResult.Fail($"Target not found: {targetRef.@ref ?? targetRef.scenePath ?? targetRef.assetPath}");

            // 验证对象在当前上下文中（仅对场景对象验证）
            if (targetObj is GameObject go && !ctx.ValidateObjectInContext(go))
            {
                return CommandExecResult.Fail($"Object '{go.name}' is not in the current edit context");
            }
            else if (targetObj is Component comp && !ctx.ValidateObjectInContext(comp.gameObject))
            {
                return CommandExecResult.Fail($"Component on '{comp.gameObject.name}' is not in the current edit context");
            }

            var logPrefix = ctx.IsInPrefabEditMode ? "[PrefabEdit] " : "";
            ctx.Logger.Info($"{logPrefix}Setting {propertyPath} on {targetObj.name}");

            if (ctx.DryRun)
            {
                ctx.Logger.Info("[DryRun] Would set property");
                return CommandExecResult.Ok("DryRun: would set property");
            }

            // 创建 SerializedObject
            var serializedObject = new SerializedObject(targetObj);
            var property = serializedObject.FindProperty(propertyPath);

            if (property == null)
            {
                if (ignoreMissing)
                {
                    ctx.Logger.Warning($"{logPrefix}Property not found (ignored): {propertyPath}");
                    return CommandExecResult.Ok("Property not found (ignored)");
                }
                return CommandExecResult.Fail($"Property not found: {propertyPath}");
            }

            // 设置值
            var value = new ValueUnion(valueToken);
            if (!SerializedPropertyHelper.SetValue(property, value, ctx))
            {
                return CommandExecResult.Fail($"Failed to set property: {propertyPath}");
            }

            // 应用修改
            serializedObject.ApplyModifiedProperties();

            // Prefab 编辑模式下不调用 SetDirty（由 SaveAsPrefabAsset 处理）
            if (!ctx.IsInPrefabEditMode)
            {
                EditorUtility.SetDirty(targetObj);

                // 如果是 Prefab 中的对象，标记为修改
                if (PrefabUtility.IsPartOfPrefabAsset(targetObj))
                {
                    var assetPath = AssetDatabase.GetAssetPath(targetObj);
                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        AssetDatabase.SaveAssetIfDirty(targetObj);
                    }
                }
            }

            ctx.Logger.Info($"{logPrefix}Set {propertyPath} successfully");

            return CommandExecResult.Ok($"Set {propertyPath}");
        }
    }
}
