using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace AutoGenJobs.Commands
{
    /// <summary>
    /// Job 命令接口
    /// 所有内置和自定义命令都需要实现此接口
    /// </summary>
    public interface IJobCommand
    {
        /// <summary>
        /// 命令名称（用于 JSON 中的 cmd 字段匹配）
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="ctx">命令上下文，提供日志、变量表、设置等</param>
        /// <param name="args">命令参数（JObject）</param>
        /// <returns>执行结果</returns>
        Core.CommandExecResult Execute(CommandContext ctx, JObject args);
    }

    /// <summary>
    /// 命令执行上下文
    /// 提供命令执行所需的环境和工具
    /// </summary>
    public class CommandContext
    {
        /// <summary>
        /// 当前 Job 的配置
        /// </summary>
        public Core.JobData Job { get; private set; }

        /// <summary>
        /// 日志记录器
        /// </summary>
        public Core.JobLogger Logger { get; private set; }

        /// <summary>
        /// 变量表（存储命令输出的对象引用）
        /// </summary>
        public Dictionary<string, Object> VarTable { get; private set; }

        /// <summary>
        /// 是否为 DryRun 模式（只验证不执行）
        /// </summary>
        public bool DryRun => Job?.dryRun ?? false;

        /// <summary>
        /// 项目写入根目录
        /// </summary>
        public string ProjectWriteRoot => Job?.projectWriteRoot ?? "Assets/AutoGen";

        /// <summary>
        /// 当前命令索引
        /// </summary>
        public int CurrentCommandIndex { get; set; }

        public CommandContext(Core.JobData job, Core.JobLogger logger)
        {
            Job = job;
            Logger = logger;
            VarTable = new Dictionary<string, Object>();
        }

        /// <summary>
        /// 设置变量
        /// </summary>
        public void SetVar(string name, Object obj)
        {
            if (string.IsNullOrEmpty(name)) return;

            // 确保变量名以 $ 开头
            if (!name.StartsWith("$"))
                name = "$" + name;

            VarTable[name] = obj;
            Logger.Debug($"Set variable {name} = {obj}");
        }

        /// <summary>
        /// 获取变量
        /// </summary>
        public Object GetVar(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            if (!name.StartsWith("$"))
                name = "$" + name;

            VarTable.TryGetValue(name, out var obj);
            return obj;
        }

        /// <summary>
        /// 获取变量并转换为指定类型
        /// </summary>
        public T GetVar<T>(string name) where T : Object
        {
            return GetVar(name) as T;
        }

        /// <summary>
        /// 解析目标引用
        /// </summary>
        public Object ResolveTarget(Core.TargetRef target)
        {
            if (target == null) return null;

            // 变量引用
            if (target.IsVariableRef)
            {
                var obj = GetVar(target.@ref);
                if (obj == null)
                {
                    Logger.Warning($"Variable not found: {target.@ref}");
                }
                return obj;
            }

            // Asset 引用
            if (target.IsAssetRef)
            {
                return Core.AssetRef.Load<Object>(target.assetGuid, target.assetPath);
            }

            // 场景路径（查找 GameObject）
            if (target.IsScenePath)
            {
                return FindGameObjectByPath(target.scenePath);
            }

            return null;
        }

        /// <summary>
        /// 解析目标引用并转换为指定类型
        /// </summary>
        public T ResolveTarget<T>(Core.TargetRef target) where T : Object
        {
            var obj = ResolveTarget(target);

            if (obj == null)
                return null;

            // 直接类型匹配
            if (obj is T t)
                return t;

            // 如果目标是 Component 类型，尝试从 GameObject 获取
            if (typeof(T).IsSubclassOf(typeof(Component)) || typeof(T) == typeof(Component))
            {
                if (obj is GameObject go)
                {
                    return go.GetComponent<T>();
                }
            }

            // 如果目标是 GameObject，尝试从 Component 获取
            if (typeof(T) == typeof(GameObject))
            {
                if (obj is Component comp)
                {
                    return comp.gameObject as T;
                }
            }

            Logger.Warning($"Cannot convert {obj.GetType().Name} to {typeof(T).Name}");
            return null;
        }

        /// <summary>
        /// 通过层级路径查找 GameObject
        /// </summary>
        public GameObject FindGameObjectByPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            // 尝试直接查找
            var go = GameObject.Find(path);
            if (go != null)
                return go;

            // 分解路径逐级查找
            var parts = path.Split('/');
            if (parts.Length == 0)
                return null;

            // 查找根对象
            var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            GameObject current = null;

            foreach (var root in rootObjects)
            {
                if (root.name == parts[0])
                {
                    current = root;
                    break;
                }
            }

            if (current == null)
                return null;

            // 沿路径查找子对象
            for (int i = 1; i < parts.Length; i++)
            {
                var child = current.transform.Find(parts[i]);
                if (child == null)
                    return null;
                current = child.gameObject;
            }

            return current;
        }

        /// <summary>
        /// 检查资产路径是否允许写入
        /// </summary>
        public bool IsPathAllowed(string assetPath)
        {
            return Core.AutoGenSettings.IsPathAllowed(assetPath);
        }

        /// <summary>
        /// 验证路径在项目写入根目录下
        /// </summary>
        public bool ValidateWritePath(string assetPath)
        {
            var check = Core.Guards.CheckAssetPathAllowed(assetPath, ProjectWriteRoot);
            if (!check.CanProceed)
            {
                Logger.Error(check.FailReason);
                return false;
            }
            return true;
        }
    }
}
