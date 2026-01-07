using System;
using System.Collections.Generic;
using System.Linq;
using AutoGenJobs.Core;
using UnityEditor;

namespace AutoGenJobs.Commands
{
    /// <summary>
    /// 命令注册表
    /// 自动发现并注册所有实现 IJobCommand 的命令
    /// </summary>
    public static class CommandRegistry
    {
        private static Dictionary<string, IJobCommand> _commands;
        private static bool _initialized = false;

        /// <summary>
        /// 获取所有已注册的命令
        /// </summary>
        public static IReadOnlyDictionary<string, IJobCommand> Commands
        {
            get
            {
                EnsureInitialized();
                return _commands;
            }
        }

        /// <summary>
        /// 获取指定名称的命令
        /// </summary>
        public static IJobCommand GetCommand(string name)
        {
            EnsureInitialized();
            _commands.TryGetValue(name, out var cmd);
            return cmd;
        }

        /// <summary>
        /// 检查命令是否存在
        /// </summary>
        public static bool HasCommand(string name)
        {
            EnsureInitialized();
            return _commands.ContainsKey(name);
        }

        /// <summary>
        /// 重新扫描并注册命令
        /// </summary>
        public static void Refresh()
        {
            _initialized = false;
            EnsureInitialized();
        }

        /// <summary>
        /// 确保命令注册表已初始化
        /// </summary>
        private static void EnsureInitialized()
        {
            if (_initialized) return;

            _commands = new Dictionary<string, IJobCommand>(StringComparer.OrdinalIgnoreCase);

            // 使用 TypeCache 获取所有实现 IJobCommand 的类型（Unity 2019.2+）
            var commandTypes = GetCommandTypes();

            foreach (var type in commandTypes)
            {
                try
                {
                    if (type.IsAbstract || type.IsInterface)
                        continue;

                    // 确保有默认构造函数
                    var constructor = type.GetConstructor(Type.EmptyTypes);
                    if (constructor == null)
                    {
                        AutoGenLog.Warning($"Command type {type.Name} has no default constructor, skipping");
                        continue;
                    }

                    var instance = (IJobCommand)Activator.CreateInstance(type);
                    if (string.IsNullOrEmpty(instance.Name))
                    {
                        AutoGenLog.Warning($"Command type {type.Name} has empty name, skipping");
                        continue;
                    }

                    if (_commands.ContainsKey(instance.Name))
                    {
                        AutoGenLog.Warning($"Duplicate command name '{instance.Name}': {type.Name} conflicts with {_commands[instance.Name].GetType().Name}");
                        continue;
                    }

                    _commands[instance.Name] = instance;
                    AutoGenLog.Debug($"Registered command: {instance.Name} ({type.Name})");
                }
                catch (Exception e)
                {
                    AutoGenLog.Error($"Failed to register command type {type.Name}: {e.Message}");
                }
            }

            AutoGenLog.Info($"CommandRegistry initialized with {_commands.Count} commands");
            _initialized = true;
        }

        /// <summary>
        /// 获取所有命令类型
        /// </summary>
        private static Type[] GetCommandTypes()
        {
#if UNITY_2019_2_OR_NEWER
            return TypeCache.GetTypesDerivedFrom<IJobCommand>().ToArray();
#else
            var result = new List<Type>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var types = assembly.GetTypes()
                        .Where(t => typeof(IJobCommand).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);
                    result.AddRange(types);
                }
                catch
                {
                    // 忽略无法加载的程序集
                }
            }
            return result.ToArray();
#endif
        }

        /// <summary>
        /// 获取所有命令名称
        /// </summary>
        public static string[] GetAllCommandNames()
        {
            EnsureInitialized();
            return _commands.Keys.ToArray();
        }

        /// <summary>
        /// 手动注册命令（用于测试或特殊情况）
        /// </summary>
        public static void RegisterCommand(IJobCommand command)
        {
            EnsureInitialized();
            if (command == null || string.IsNullOrEmpty(command.Name))
            {
                AutoGenLog.Warning("Cannot register null command or command with empty name");
                return;
            }

            _commands[command.Name] = command;
            AutoGenLog.Debug($"Manually registered command: {command.Name}");
        }
    }
}
