using System;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace AutoGenJobs.Core
{
    /// <summary>
    /// 类型解析工具类
    /// 用于从类型名称字符串解析 C# 类型
    /// </summary>
    public static class TypeResolver
    {
        /// <summary>
        /// 解析类型名称为 Type 对象
        /// 支持多种格式：
        /// - 完整程序集限定名：MyGame.Runtime.Health, Assembly-CSharp
        /// - 命名空间.类名：MyGame.Runtime.Health
        /// - Unity 内置类型：UnityEngine.SpriteRenderer
        /// </summary>
        public static Type ResolveType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return null;

            // 首先尝试直接获取类型（完整限定名）
            var type = Type.GetType(typeName);
            if (type != null)
                return type;

            // 尝试从所有已加载的程序集中查找
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                // 尝试直接获取
                type = assembly.GetType(typeName);
                if (type != null)
                    return type;

                // 如果类型名不包含逗号（不是完整限定名），尝试拼接程序集名
                if (!typeName.Contains(","))
                {
                    type = assembly.GetType(typeName);
                    if (type != null)
                        return type;
                }
            }

            // 特殊处理 Unity 类型
            if (typeName.StartsWith("UnityEngine."))
            {
                type = typeof(UnityEngine.Object).Assembly.GetType(typeName);
                if (type != null)
                    return type;

                // 尝试 UnityEngine.CoreModule
                foreach (var asm in assemblies.Where(a => a.FullName.Contains("UnityEngine")))
                {
                    type = asm.GetType(typeName);
                    if (type != null)
                        return type;
                }
            }

            // 尝试 UnityEditor 类型
            if (typeName.StartsWith("UnityEditor."))
            {
                type = typeof(Editor).Assembly.GetType(typeName);
                if (type != null)
                    return type;
            }

            return null;
        }

        /// <summary>
        /// 检查类型是否存在
        /// </summary>
        public static bool TypeExists(string typeName)
        {
            return ResolveType(typeName) != null;
        }

        /// <summary>
        /// 检查类型是否是 Component
        /// </summary>
        public static bool IsComponentType(Type type)
        {
            return type != null && typeof(UnityEngine.Component).IsAssignableFrom(type);
        }

        /// <summary>
        /// 检查类型是否是 ScriptableObject
        /// </summary>
        public static bool IsScriptableObjectType(Type type)
        {
            return type != null && typeof(UnityEngine.ScriptableObject).IsAssignableFrom(type);
        }

        /// <summary>
        /// 检查类型是否是 MonoBehaviour
        /// </summary>
        public static bool IsMonoBehaviourType(Type type)
        {
            return type != null && typeof(UnityEngine.MonoBehaviour).IsAssignableFrom(type);
        }

        /// <summary>
        /// 尝试使用 TypeCache 获取派生类型（Unity 2019.2+）
        /// </summary>
        public static Type[] GetTypesDerivedFrom<T>() where T : class
        {
#if UNITY_2019_2_OR_NEWER
            return TypeCache.GetTypesDerivedFrom<T>().ToArray();
#else
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => {
                    try { return a.GetTypes(); }
                    catch { return new Type[0]; }
                })
                .Where(t => typeof(T).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                .ToArray();
#endif
        }

        /// <summary>
        /// 解析枚举值
        /// </summary>
        public static bool TryParseEnum(Type enumType, string valueName, out object result)
        {
            result = null;
            if (enumType == null || !enumType.IsEnum || string.IsNullOrEmpty(valueName))
                return false;

            try
            {
                result = Enum.Parse(enumType, valueName, true);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
