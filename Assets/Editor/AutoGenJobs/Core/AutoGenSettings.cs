using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AutoGenJobs.Core
{
    /// <summary>
    /// AutoGen Job System 的配置设置
    /// 使用 EditorPrefs 存储配置，提供全局访问
    /// </summary>
    public static class AutoGenSettings
    {
        // EditorPrefs 键名
        private const string KEY_JOB_ROOT = "AutoGenJobs_JobRoot";
        private const string KEY_ALLOWED_ROOTS = "AutoGenJobs_AllowedWriteRoots";
        private const string KEY_TICK_INTERVAL = "AutoGenJobs_TickIntervalMs";
        private const string KEY_MAX_JOBS_PER_TICK = "AutoGenJobs_MaxJobsPerTick";
        private const string KEY_MAX_MS_PER_TICK = "AutoGenJobs_MaxMsPerTick";
        private const string KEY_ENABLE_RUNNER = "AutoGenJobs_EnableRunner";
        private const string KEY_VERBOSE_LOGGING = "AutoGenJobs_VerboseLogging";

        // 默认值
        private static string DefaultJobRoot => Path.Combine(ProjectRoot, "AutoGenJobs");
        private const string DEFAULT_ALLOWED_ROOT = "Assets/AutoGen";
        private const int DEFAULT_TICK_INTERVAL_MS = 200;
        private const int DEFAULT_MAX_JOBS_PER_TICK = 1;
        private const int DEFAULT_MAX_MS_PER_TICK = 30;
        private const bool DEFAULT_ENABLE_RUNNER = true;
        private const bool DEFAULT_VERBOSE_LOGGING = false;

        /// <summary>
        /// Runner 版本号，用于 runnerMinVersion 检查
        /// </summary>
        public const int RUNNER_VERSION = 3;

        /// <summary>
        /// Unity 项目根目录（包含 Assets 的目录）
        /// </summary>
        public static string ProjectRoot => Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;

        /// <summary>
        /// Job 文件根目录（绝对路径）
        /// </summary>
        public static string JobRootAbsolute
        {
            get => EditorPrefs.GetString(KEY_JOB_ROOT, DefaultJobRoot);
            set => EditorPrefs.SetString(KEY_JOB_ROOT, value);
        }

        /// <summary>
        /// inbox 目录
        /// </summary>
        public static string InboxPath => Path.Combine(JobRootAbsolute, "inbox");

        /// <summary>
        /// working 目录
        /// </summary>
        public static string WorkingPath => Path.Combine(JobRootAbsolute, "working");

        /// <summary>
        /// done 目录
        /// </summary>
        public static string DonePath => Path.Combine(JobRootAbsolute, "done");

        /// <summary>
        /// results 目录
        /// </summary>
        public static string ResultsPath => Path.Combine(JobRootAbsolute, "results");

        /// <summary>
        /// dead 目录（死信）
        /// </summary>
        public static string DeadPath => Path.Combine(JobRootAbsolute, "dead");

        /// <summary>
        /// 允许写入的资产根目录列表
        /// </summary>
        public static List<string> AllowedWriteRoots
        {
            get
            {
                var json = EditorPrefs.GetString(KEY_ALLOWED_ROOTS, "");
                if (string.IsNullOrEmpty(json))
                {
                    return new List<string> { DEFAULT_ALLOWED_ROOT };
                }
                try
                {
                    return JsonUtility.FromJson<StringListWrapper>(json)?.items ?? new List<string> { DEFAULT_ALLOWED_ROOT };
                }
                catch
                {
                    return new List<string> { DEFAULT_ALLOWED_ROOT };
                }
            }
            set
            {
                var wrapper = new StringListWrapper { items = value };
                EditorPrefs.SetString(KEY_ALLOWED_ROOTS, JsonUtility.ToJson(wrapper));
            }
        }

        /// <summary>
        /// Tick 间隔（毫秒）
        /// </summary>
        public static int TickIntervalMs
        {
            get => EditorPrefs.GetInt(KEY_TICK_INTERVAL, DEFAULT_TICK_INTERVAL_MS);
            set => EditorPrefs.SetInt(KEY_TICK_INTERVAL, Mathf.Max(50, value));
        }

        /// <summary>
        /// 每次 Tick 最多处理的 Job 数量
        /// </summary>
        public static int MaxJobsPerTick
        {
            get => EditorPrefs.GetInt(KEY_MAX_JOBS_PER_TICK, DEFAULT_MAX_JOBS_PER_TICK);
            set => EditorPrefs.SetInt(KEY_MAX_JOBS_PER_TICK, Mathf.Max(1, value));
        }

        /// <summary>
        /// 每次 Tick 最大执行时间（毫秒）
        /// </summary>
        public static int MaxMsPerTick
        {
            get => EditorPrefs.GetInt(KEY_MAX_MS_PER_TICK, DEFAULT_MAX_MS_PER_TICK);
            set => EditorPrefs.SetInt(KEY_MAX_MS_PER_TICK, Mathf.Clamp(value, 10, 100));
        }

        /// <summary>
        /// 是否启用 Runner
        /// </summary>
        public static bool EnableRunner
        {
            get => EditorPrefs.GetBool(KEY_ENABLE_RUNNER, DEFAULT_ENABLE_RUNNER);
            set => EditorPrefs.SetBool(KEY_ENABLE_RUNNER, value);
        }

        /// <summary>
        /// 是否启用详细日志
        /// </summary>
        public static bool VerboseLogging
        {
            get => EditorPrefs.GetBool(KEY_VERBOSE_LOGGING, DEFAULT_VERBOSE_LOGGING);
            set => EditorPrefs.SetBool(KEY_VERBOSE_LOGGING, value);
        }

        /// <summary>
        /// 确保所有必需目录存在
        /// </summary>
        public static void EnsureDirectoriesExist()
        {
            var dirs = new[] { InboxPath, WorkingPath, DonePath, ResultsPath, DeadPath };
            foreach (var dir in dirs)
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
        }

        /// <summary>
        /// 重置为默认设置
        /// </summary>
        public static void ResetToDefaults()
        {
            EditorPrefs.DeleteKey(KEY_JOB_ROOT);
            EditorPrefs.DeleteKey(KEY_ALLOWED_ROOTS);
            EditorPrefs.DeleteKey(KEY_TICK_INTERVAL);
            EditorPrefs.DeleteKey(KEY_MAX_JOBS_PER_TICK);
            EditorPrefs.DeleteKey(KEY_MAX_MS_PER_TICK);
            EditorPrefs.DeleteKey(KEY_ENABLE_RUNNER);
            EditorPrefs.DeleteKey(KEY_VERBOSE_LOGGING);
        }

        /// <summary>
        /// 检查路径是否在允许的写入根目录下
        /// </summary>
        public static bool IsPathAllowed(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return false;

            var normalizedPath = PathUtil.NormalizeAssetPath(assetPath);
            foreach (var root in AllowedWriteRoots)
            {
                var normalizedRoot = PathUtil.NormalizeAssetPath(root);
                if (normalizedPath.StartsWith(normalizedRoot))
                {
                    return true;
                }
            }
            return false;
        }

        [System.Serializable]
        private class StringListWrapper
        {
            public List<string> items = new List<string>();
        }
    }
}
