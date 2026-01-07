using AutoGenJobs.Core;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace AutoGenJobs.Commands.Builtins
{
    /// <summary>
    /// 添加组件命令
    /// </summary>
    public class Cmd_AddComponent : IJobCommand
    {
        public string Name => "AddComponent";

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

            // 解析类型
            var typeName = args["type"]?.ToString();
            if (string.IsNullOrEmpty(typeName))
                return CommandExecResult.Fail("Missing 'type' argument");

            var ifMissing = args["ifMissing"]?.ToObject<bool>() ?? true;

            // 获取目标 GameObject
            var targetObj = ctx.ResolveTarget(targetRef);
            if (targetObj == null)
                return CommandExecResult.Fail($"Target not found: {targetRef.@ref ?? targetRef.scenePath}");

            GameObject go;
            if (targetObj is GameObject gameObject)
            {
                go = gameObject;
            }
            else if (targetObj is Component comp)
            {
                go = comp.gameObject;
            }
            else
            {
                return CommandExecResult.Fail($"Target is not a GameObject or Component: {targetObj.GetType().Name}");
            }

            // 解析组件类型
            var componentType = TypeResolver.ResolveType(typeName);
            if (componentType == null)
                return CommandExecResult.Fail($"Component type not found: {typeName}");

            if (!TypeResolver.IsComponentType(componentType))
                return CommandExecResult.Fail($"Type is not a Component: {typeName}");

            ctx.Logger.Info($"Adding {componentType.Name} to {go.name}");

            if (ctx.DryRun)
            {
                ctx.Logger.Info("[DryRun] Would add component");
                return CommandExecResult.Ok("DryRun: would add component");
            }

            // 检查是否已存在（ifMissing 模式）
            Component existing = null;
            if (ifMissing)
            {
                existing = go.GetComponent(componentType);
                if (existing != null)
                {
                    ctx.Logger.Debug($"Component {componentType.Name} already exists on {go.name}");
                    return CommandExecResult.Ok(
                        "Component already exists",
                        new System.Collections.Generic.Dictionary<string, Object> { { "component", existing } }
                    );
                }
            }

            // 添加组件
            Undo.RecordObject(go, $"Add {componentType.Name}");
            var component = go.AddComponent(componentType);

            if (component == null)
                return CommandExecResult.Fail($"Failed to add component {componentType.Name}");

            Undo.RegisterCreatedObjectUndo(component, $"Add {componentType.Name}");
            EditorUtility.SetDirty(go);

            ctx.Logger.Info($"Added {componentType.Name} to {go.name}");

            return CommandExecResult.Ok(
                $"Added {componentType.Name}",
                new System.Collections.Generic.Dictionary<string, Object> { { "component", component } }
            );
        }
    }
}
