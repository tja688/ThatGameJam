using AutoGenJobs.Commands;
using AutoGenJobs.Core;
using UnityEditor;
using UnityEngine;

namespace AutoGenJobs.UI
{
    /// <summary>
    /// AutoGen Jobs 菜单项
    /// </summary>
    public static class MenuItems
    {
        private const string MENU_ROOT = "Tools/AutoGen Jobs/";

        /// <summary>
        /// 打开 Jobs 文件夹
        /// </summary>
        [MenuItem(MENU_ROOT + "Open Jobs Folder", priority = 100)]
        public static void OpenJobsFolder()
        {
            AutoGenSettings.EnsureDirectoriesExist();
            EditorUtility.RevealInFinder(AutoGenSettings.InboxPath);
        }

        /// <summary>
        /// 打开 Inbox 文件夹
        /// </summary>
        [MenuItem(MENU_ROOT + "Open Inbox", priority = 101)]
        public static void OpenInboxFolder()
        {
            AutoGenSettings.EnsureDirectoriesExist();
            EditorUtility.RevealInFinder(AutoGenSettings.InboxPath);
        }

        /// <summary>
        /// 打开 Results 文件夹
        /// </summary>
        [MenuItem(MENU_ROOT + "Open Results", priority = 102)]
        public static void OpenResultsFolder()
        {
            AutoGenSettings.EnsureDirectoriesExist();
            EditorUtility.RevealInFinder(AutoGenSettings.ResultsPath);
        }

        /// <summary>
        /// 切换 Runner 启用状态
        /// </summary>
        [MenuItem(MENU_ROOT + "Toggle Runner", priority = 200)]
        public static void ToggleRunner()
        {
            AutoGenSettings.EnableRunner = !AutoGenSettings.EnableRunner;
            
            if (AutoGenSettings.EnableRunner)
            {
                JobRunner.Initialize();
                Debug.Log("[AutoGenJobs] Runner enabled");
            }
            else
            {
                JobRunner.Shutdown();
                Debug.Log("[AutoGenJobs] Runner disabled");
            }
        }

        [MenuItem(MENU_ROOT + "Toggle Runner", true)]
        public static bool ToggleRunnerValidate()
        {
            Menu.SetChecked(MENU_ROOT + "Toggle Runner", AutoGenSettings.EnableRunner);
            return true;
        }

        /// <summary>
        /// 切换详细日志
        /// </summary>
        [MenuItem(MENU_ROOT + "Verbose Logging", priority = 201)]
        public static void ToggleVerboseLogging()
        {
            AutoGenSettings.VerboseLogging = !AutoGenSettings.VerboseLogging;
            Debug.Log($"[AutoGenJobs] Verbose logging: {AutoGenSettings.VerboseLogging}");
        }

        [MenuItem(MENU_ROOT + "Verbose Logging", true)]
        public static bool ToggleVerboseLoggingValidate()
        {
            Menu.SetChecked(MENU_ROOT + "Verbose Logging", AutoGenSettings.VerboseLogging);
            return true;
        }

        /// <summary>
        /// 显示 Runner 状态
        /// </summary>
        [MenuItem(MENU_ROOT + "Show Status", priority = 300)]
        public static void ShowStatus()
        {
            var status = JobRunner.GetStatus();
            var version = AutoGenSettings.RUNNER_VERSION;
            var pendingCount = new JobQueue().GetPendingJobs().Count;
            var workingCount = new JobQueue().GetWorkingJobs().Count;

            EditorUtility.DisplayDialog(
                "AutoGen Jobs Status",
                $"Runner Version: {version}\n" +
                $"Status: {status}\n" +
                $"Pending Jobs: {pendingCount}\n" +
                $"Working Jobs: {workingCount}\n" +
                $"\n" +
                $"Jobs Root: {AutoGenSettings.JobRootAbsolute}\n" +
                $"Allowed Roots: {string.Join(", ", AutoGenSettings.AllowedWriteRoots)}\n" +
                $"\n" +
                $"Registered Commands: {CommandRegistry.Commands.Count}",
                "OK"
            );
        }

        /// <summary>
        /// 列出已注册的命令
        /// </summary>
        [MenuItem(MENU_ROOT + "List Commands", priority = 301)]
        public static void ListCommands()
        {
            var commands = CommandRegistry.GetAllCommandNames();
            Debug.Log($"[AutoGenJobs] Registered commands ({commands.Length}):\n" + string.Join("\n", commands));
        }

        /// <summary>
        /// 手动处理下一个 Job
        /// </summary>
        [MenuItem(MENU_ROOT + "Process Next Job", priority = 400)]
        public static void ProcessNextJob()
        {
            JobRunner.ProcessNextJob();
        }

        /// <summary>
        /// 重置设置
        /// </summary>
        [MenuItem(MENU_ROOT + "Reset Settings", priority = 500)]
        public static void ResetSettings()
        {
            if (EditorUtility.DisplayDialog(
                "Reset AutoGen Jobs Settings",
                "This will reset all settings to default values. Continue?",
                "Reset", "Cancel"))
            {
                AutoGenSettings.ResetToDefaults();
                Debug.Log("[AutoGenJobs] Settings reset to defaults");
            }
        }

        /// <summary>
        /// 打开编辑器窗口
        /// </summary>
        [MenuItem(MENU_ROOT + "Show Window", priority = 0)]
        public static void ShowWindow()
        {
            AutoGenJobsWindow.ShowWindow();
        }
    }
}
