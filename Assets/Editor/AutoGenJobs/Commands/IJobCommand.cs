using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    /// Prefab 编辑上下文
    /// 用于隔离 Prefab 编辑时的对象查找，避免污染场景
    /// </summary>
    public class PrefabEditContext
    {
        /// <summary>
        /// Prefab 的根对象
        /// </summary>
        public GameObject PrefabRoot { get; set; }

        /// <summary>
        /// Prefab 内容所在的临时场景
        /// </summary>
        public Scene PrefabContentsScene { get; set; }

        /// <summary>
        /// 是否处于 Prefab 编辑模式
        /// </summary>
        public bool IsValid => PrefabRoot != null;
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

        /// <summary>
        /// Prefab 编辑上下文（当处于 CreateOrEditPrefab 的嵌套命令中时有效）
        /// </summary>
        public PrefabEditContext PrefabContext { get; private set; }

        /// <summary>
        /// 是否处于 Prefab 编辑模式
        /// </summary>
        public bool IsInPrefabEditMode => PrefabContext != null && PrefabContext.IsValid;

        public CommandContext(Core.JobData job, Core.JobLogger logger)
        {
            Job = job;
            Logger = logger;
            VarTable = new Dictionary<string, Object>();
        }

        /// <summary>
        /// 进入 Prefab 编辑模式
        /// </summary>
        public void EnterPrefabEditMode(GameObject prefabRoot, Scene prefabContentsScene)
        {
            PrefabContext = new PrefabEditContext
            {
                PrefabRoot = prefabRoot,
                PrefabContentsScene = prefabContentsScene
            };
            Logger.Debug($"Entered Prefab edit mode: {prefabRoot.name}");
        }

        /// <summary>
        /// 退出 Prefab 编辑模式
        /// </summary>
        public void ExitPrefabEditMode()
        {
            if (PrefabContext != null)
            {
                Logger.Debug($"Exited Prefab edit mode");
                PrefabContext = null;
            }
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
        /// 修复 B：在 Prefab 编辑模式下，限制查找范围到 Prefab 内部
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
        /// 修复 B：在 Prefab 编辑模式下，只在 Prefab 内容中查找
        /// </summary>
        public GameObject FindGameObjectByPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            // ============================================================
            // 修复 B: Prefab 编辑模式下，限制查找范围
            // ============================================================
            if (IsInPrefabEditMode)
            {
                return FindGameObjectInPrefab(path);
            }

            // 正常模式：在当前活动场景中查找
            return FindGameObjectInActiveScene(path);
        }

        /// <summary>
        /// 在 Prefab 内部查找 GameObject
        /// 路径被解释为相对于 PrefabRoot 的路径
        /// </summary>
        private GameObject FindGameObjectInPrefab(string path)
        {
            if (PrefabContext?.PrefabRoot == null)
            {
                Logger.Error("PrefabEditContext is invalid");
                return null;
            }

            var root = PrefabContext.PrefabRoot;

            // 如果路径为空或是根名称，返回根对象
            if (string.IsNullOrEmpty(path) || path == root.name)
            {
                return root;
            }

            // 分解路径
            var parts = path.Split('/');
            Transform current = root.transform;

            // 检查第一个部分是否是根名称，如果是则跳过
            int startIndex = 0;
            if (parts.Length > 0 && parts[0] == root.name)
            {
                startIndex = 1;
            }

            // 从 startIndex 开始在 Prefab 内部查找
            for (int i = startIndex; i < parts.Length; i++)
            {
                var child = current.Find(parts[i]);
                if (child == null)
                {
                    Logger.Warning($"[PrefabEdit] Child not found in prefab: {parts[i]} (full path: {path})");
                    return null;
                }
                current = child;
            }

            return current.gameObject;
        }

        /// <summary>
        /// 在活动场景中查找 GameObject
        /// </summary>
        private GameObject FindGameObjectInActiveScene(string path)
        {
            // 尝试直接查找
            var go = GameObject.Find(path);
            if (go != null)
                return go;

            // 分解路径逐级查找
            var parts = path.Split('/');
            if (parts.Length == 0)
                return null;

            // 查找根对象
            var rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
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
        /// 创建 GameObject（考虑 Prefab 编辑模式）
        /// 修复 B：在 Prefab 编辑模式下，新对象必须作为 Prefab 子对象创建
        /// </summary>
        public GameObject CreateGameObject(string name, string parentPath = null)
        {
            GameObject go = new GameObject(name);

            if (IsInPrefabEditMode)
            {
                // Prefab 编辑模式：默认父对象是 PrefabRoot
                Transform parent = PrefabContext.PrefabRoot.transform;

                if (!string.IsNullOrEmpty(parentPath))
                {
                    var parentGo = FindGameObjectInPrefab(parentPath);
                    if (parentGo != null)
                    {
                        parent = parentGo.transform;
                    }
                    else
                    {
                        Logger.Warning($"[PrefabEdit] Parent not found: {parentPath}, using prefab root");
                    }
                }

                go.transform.SetParent(parent, false);
                Logger.Debug($"[PrefabEdit] Created GameObject '{name}' under '{parent.name}'");
            }
            else
            {
                // 正常模式
                if (!string.IsNullOrEmpty(parentPath))
                {
                    var parent = FindGameObjectByPath(parentPath);
                    if (parent != null)
                    {
                        go.transform.SetParent(parent.transform, false);
                    }
                }
            }

            return go;
        }

        /// <summary>
        /// 验证对象是否在当前有效上下文中
        /// 用于防止跨上下文操作
        /// </summary>
        public bool ValidateObjectInContext(Object obj)
        {
            if (obj == null) return false;

            if (IsInPrefabEditMode)
            {
                // 在 Prefab 编辑模式下，只允许操作 Prefab 内的对象
                if (obj is GameObject go)
                {
                    return IsObjectInPrefab(go.transform);
                }
                else if (obj is Component comp)
                {
                    return IsObjectInPrefab(comp.transform);
                }
                // Asset 引用允许
                return true;
            }

            return true;
        }

        /// <summary>
        /// 检查对象是否在当前 Prefab 内
        /// </summary>
        private bool IsObjectInPrefab(Transform obj)
        {
            if (PrefabContext?.PrefabRoot == null) return false;

            var root = PrefabContext.PrefabRoot.transform;
            var current = obj;

            while (current != null)
            {
                if (current == root)
                    return true;
                current = current.parent;
            }

            return false;
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
